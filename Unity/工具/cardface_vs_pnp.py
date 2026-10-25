#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""cardface_vs_pnp.py —— 把「我们渲出来的卡面」与「原版 PnP 成品卡图」**按卡配对、拼成并排图**。

为什么要有它
------------
`X1`/`B2` 那几轮里「并排比 PnP」一直是**手工**做的（打开两张图、缩放、对眼），
而这正是 `CLAUDE.md` 铁律 10 第 6 条要求的**最后一步验收**。手工 ⇒ 不可复现、
换个人做法不一样、也没法一眼看出「哪几张没配上」。本脚本把它变成一条命令：

    python -I -X utf8 d:/4/Unity/工具/cardface_vs_pnp.py

输入 / 输出
-----------
· 输入 A（我们的渲图）：`d:/4/_tmp_view/cardface/*.png`
  —— `CardFaceProbe.Run` 出的那一批（文件名 = 英文卡名，下划线连接）。
· 输入 B（原版成品卡图）：`d:/2/Warpforge部队卡片/<阵营>/<类别>/*.png|jpg`
  —— 900×1200 的 PnP 印刷卡（文件名形如 `Warpforge_20_Deathwing-Terminator.png`）。
· 输出：`d:/4/_tmp_view/cardface_vs_pnp/<卡名>.png`（左 = 我们的，右 = PnP）
  外加 `manifest.tsv`（卡名 / 我们图 / PnP 图 / 配对方式）。

**配对靠卡名**（下划线/短横/空格归一后比）。配不上的一律**如实列出来**
（`manifest.tsv` 里 `matched=no`，并在终端打一段「没配上」清单）—— ⛔ 绝不硬凑。

⚠️ 幂等：每次跑都**重写**同名输出，并删掉本脚本上一轮留下的、这一轮不再产出的 png
（判据 = 上一次的 `manifest.tsv`），所以连跑两次不会长出重复文件。

⚠️ 本脚本**只读** `d:/2`（原版档案库），只写 `d:/4/_tmp_view/cardface_vs_pnp/`。
"""
import io
import os
import re
import sys

from PIL import Image, ImageDraw

# ---------------------------------------------------------------- 路径（可被 argv 覆盖）
HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_OURS = "d:/4/_tmp_view/cardface"
DEFAULT_PNP = "d:/2/Warpforge部队卡片"
DEFAULT_OUT = "d:/4/_tmp_view/cardface_vs_pnp"

IMG_EXT = (".png", ".jpg", ".jpeg")

# 我们的渲图里那些**不是「一张卡」**的后缀（同一张卡的姿态变体）——只取主图
OUR_VARIANT_SUFFIXES = (
    "_playable", "_selected", "_unplayable", "_nofront", "_tex",
    "_hand", "_inplay", "_board", "_tilt",
)


def norm(name):
    """卡名归一：去掉一切分隔与大小写，只留字母数字。"""
    return re.sub(r"[^0-9a-z]+", "", name.lower())


PNP_PREFIX_RE = re.compile(r"^(?:\d+[_-])*(?:updated_)?(?:warpforge[_-]?\d*[_-])?", re.I)
PNP_TRAIL_RE = re.compile(r"[-_ ]\d+$")          # `Heavy-Intercessor-2` 那种变体序号


def pnp_key(stem):
    s = stem
    s = PNP_PREFIX_RE.sub("", s)
    s = PNP_TRAIL_RE.sub("", s)
    return norm(s)


def ours_key(stem):
    s = stem
    for suf in OUR_VARIANT_SUFFIXES:
        if s.lower().endswith(suf):
            s = s[: -len(suf)]
            break
    return norm(s)


def list_ours(root):
    """{归一卡名: 路径}（同一张卡的多个变体只留主图）"""
    out = {}
    if not os.path.isdir(root):
        return out
    for fn in sorted(os.listdir(root)):
        if not fn.lower().endswith(IMG_EXT):
            continue
        stem, _ = os.path.splitext(fn)
        if stem.startswith("_") or stem.lower().startswith("cmp_"):
            continue                       # `_sbs_banshee` / `cmp_disk_vs_runtime` 不是「一张卡」
        low = stem.lower()
        if any(low.endswith(s) for s in OUR_VARIANT_SUFFIXES):
            continue
        out.setdefault(ours_key(stem), os.path.join(root, fn))
    return out


def list_pnp(root):
    """{归一卡名: [路径…]} —— 12/13 个阵营目录都扫，同类多份变体都留着（记数量）"""
    out = {}
    if not os.path.isdir(root):
        return out
    for faction in sorted(os.listdir(root)):
        fp = os.path.join(root, faction)
        if not os.path.isdir(fp):
            continue
        for dirpath, _dirnames, filenames in os.walk(fp):
            for fn in filenames:
                if not fn.lower().endswith(IMG_EXT):
                    continue
                stem, _ = os.path.splitext(fn)
                k = pnp_key(stem)
                if not k:
                    continue
                out.setdefault(k, []).append(os.path.join(dirpath, fn))
    for k in out:
        out[k].sort()
    return out


# 前缀/后缀兜底：`pariah` ↔ `Pariah_Vanguard`、`stormlord` ↔ `Imotekh_the_Stormlord`
# 这种「PnP 只印了半截名」的。⚠️ 只在**唯一命中**且两边都 ≥ 6 个字符时接受，
# 并在 manifest 里标 `matched=prefix` / `matched=suffix` —— 让复核的人一眼知道「这一对是机器猜的」。
def affix_candidates(okey, pnp_keys, mode):
    hits = []
    for k in pnp_keys:
        if len(k) < 6 or len(okey) < 6 or k == okey:
            continue
        if mode == "prefix" and (okey.startswith(k) or k.startswith(okey)):
            hits.append(k)
        elif mode == "suffix" and (okey.endswith(k) or k.endswith(okey)):
            hits.append(k)
    return hits if len(hits) == 1 else []


def load_scaled(path, height):
    im = Image.open(path).convert("RGB")
    w, h = im.size
    if h <= 0:
        return im
    nw = max(1, int(round(w * height / float(h))))
    return im.resize((nw, height), Image.LANCZOS)


def compose(ours_path, pnp_path, card_label, height=900, gap=24, label_h=26):
    left = load_scaled(ours_path, height)
    right = load_scaled(pnp_path, height)
    W = left.size[0] + gap + right.size[0]
    H = height + label_h
    canvas = Image.new("RGB", (W, H), (24, 24, 28))
    canvas.paste(left, (0, label_h))
    canvas.paste(right, (left.size[0] + gap, label_h))
    try:
        d = ImageDraw.Draw(canvas)
        d.text((4, 4), (card_label + "   |   left = ours / right = PnP")[:160], fill=(230, 230, 210))
    except Exception:
        pass                                       # 没字体就算了：标签只是方便人眼，不是判据
    return canvas


def previous_outputs(manifest_path):
    """上一次跑写了哪些 png（用于「这一轮不再产出就删掉」的幂等收尾）"""
    old = set()
    if not os.path.isfile(manifest_path):
        return old
    with io.open(manifest_path, "r", encoding="utf-8") as f:
        for line in f:
            parts = line.rstrip("\n").split("\t")
            if len(parts) >= 5 and parts[4].endswith(".png"):
                old.add(parts[4])
    return old


def main(argv):
    ours_root = argv[1] if len(argv) > 1 else DEFAULT_OURS
    pnp_root = argv[2] if len(argv) > 2 else DEFAULT_PNP
    out_dir = argv[3] if len(argv) > 3 else DEFAULT_OUT

    ours = list_ours(ours_root)
    pnp = list_pnp(pnp_root)
    if not ours:
        sys.stderr.write("[cardface_vs_pnp] 我们的渲图目录是空的：%s\n" % ours_root)
        return 2
    if not pnp:
        sys.stderr.write("[cardface_vs_pnp] PnP 目录没扫到图：%s\n" % pnp_root)
        return 2

    os.makedirs(out_dir, exist_ok=True)
    manifest_path = os.path.join(out_dir, "manifest.tsv")
    stale = previous_outputs(manifest_path)

    rows = []
    produced = set()
    matched_n = 0
    for okey in sorted(ours):
        our_path = ours[okey]
        how = "no"
        pnp_path = ""
        if okey in pnp:
            pnp_path = pnp[okey][0]
            how = "exact"
        else:
            cand = affix_candidates(okey, list(pnp.keys()), "prefix")
            if cand:
                pnp_path = pnp[cand[0]][0]
                how = "prefix"
            else:
                cand = affix_candidates(okey, list(pnp.keys()), "suffix")
                if cand:
                    pnp_path = pnp[cand[0]][0]
                    how = "suffix"
        if not pnp_path:
            rows.append((os.path.basename(our_path), our_path, "", "no", ""))
            continue

        matched_n += 1
        out_name = okey + ".png"
        produced.add(out_name)
        label = os.path.splitext(os.path.basename(our_path))[0]
        compose(our_path, pnp_path, label).save(os.path.join(out_dir, out_name))
        rows.append((os.path.basename(our_path), our_path, pnp_path, how, out_name))

    # ---- 幂等收尾：删掉上一轮产、这一轮不再产出的 png ----
    for fn in sorted(stale - produced):
        p = os.path.join(out_dir, fn)
        if os.path.isfile(p):
            os.remove(p)

    with io.open(manifest_path, "w", encoding="utf-8", newline="\n") as f:
        f.write("card\tours\tpnp\tmatched\tout\n")
        for r in rows:
            f.write("\t".join(r) + "\n")

    # ---- 覆盖率（本批渲图覆盖了哪些阵营；够不够「12 阵营 × ≥3 张」）----
    fac_of = {}
    for k, paths in pnp.items():
        for p in paths:
            rel = os.path.relpath(p, pnp_root).replace("\\", "/")
            fac_of.setdefault(k, rel.split("/")[0])
    cover = {}
    for our_base, _our, pnp_p, how, _out in rows:
        if how == "no":
            continue
        rel = os.path.relpath(pnp_p, pnp_root).replace("\\", "/")
        fac = rel.split("/")[0]
        cover[fac] = cover.get(fac, 0) + 1

    print("[cardface_vs_pnp] 我们的渲图 %d 张 · PnP 去重后 %d 张 · 配上 %d 对 ⇒ %s"
          % (len(ours), len(pnp), matched_n, out_dir))
    print("[cardface_vs_pnp] 覆盖到的阵营：")
    for fac in sorted(cover):
        print("    %-22s %d 对" % (fac, cover[fac]))
    missing = sorted(set(v for v in fac_of.values()) - set(cover))
    if missing:
        print("[cardface_vs_pnp] ⚠️ 这些阵营**一对都没配上**（我们的渲图里没有它们的卡）：")
        for fac in missing:
            print("    %s" % fac)
    unmatched = [(r[0], r[1]) for r in rows if r[3] == "no"]
    if unmatched:
        print("[cardface_vs_pnp] ⚠️ 没配上的我们的图 %d 张（如实列出，不硬凑）：" % len(unmatched))
        for base, _p in unmatched:
            print("    %s" % base)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
