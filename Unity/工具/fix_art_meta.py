#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""fix_art_meta.py —— 美术 / 音频 `.meta` 的【只读普查 + 显式写盘】工具。

立项：账 `B-24`（NPOT 存量）· `B-25`（音频导入设置系统级不一致）；
判据：`资料/完全复刻/判据/B110_V4_真缺陷复核D.md` §G / §E · `判据/B110_R2_小账时效复核.md` §四⑩；
本次实现与结论：`资料/完全复刻/报告/B111_T2.md`。

用法（**不带参 = 只打印本段并退出 0**；`-h/--help` 同）：
  python -I -X utf8 d:/4/Unity/工具/fix_art_meta.py
  python -I -X utf8 d:/4/Unity/工具/fix_art_meta.py --scan-npot
  python -I -X utf8 d:/4/Unity/工具/fix_art_meta.py --fix-npot            # 只演算、不写盘（默认）
  python -I -X utf8 d:/4/Unity/工具/fix_art_meta.py --fix-npot --apply    # 真写（先备份）
  python -I -X utf8 d:/4/Unity/工具/fix_art_meta.py --scan-audio
  python -I -X utf8 d:/4/Unity/工具/fix_art_meta.py --fix-audio-prov       # 只演算
  python -I -X utf8 d:/4/Unity/工具/fix_art_meta.py --fix-audio-prov --apply
  python -I -X utf8 d:/4/Unity/工具/fix_art_meta.py --revert --apply       # 从备份恢复（只恢复验过 md5 的）
  可选：`--assets <根>`（默认 `d:/4/Unity/MyGame/Assets`）· `--backup-dir <目录>` ·
        `--include-plugins`（连 `Plugins/` 一起改；默认**跳过**第三方插件资产）· `--top N`

🔴 **写盘纪律（本项目踩过「`--help` 其实是真合并」）**：
  · 任何 `--fix-*` **不带 `--apply` 时只演算**；
  · `--apply` 前**先把要改的原始字节按相对路径备份**到 `--backup-dir`，并写一份 `manifest.tsv`；
  · 只改**确实要改的那一个数字**，其余字节原样（**二进制读、二进制写**，绝不翻行尾）。

── 一、NPOT（账 `B-24`）────────────────────────────────────────────────────────
判据：PNG 的 `width`/`height` 任一不是 2 的幂，且 `.meta` 里 `nPOTScale != 0` ⇒ Unity 导入时
会把它缩放成最近的 2 的幂（`TextureImporterNPOTScale.ToNearest = 1`），**像素被重采样、尺寸变了**。
（枚举：None=0 · ToNearest=1 · ToLarger=2 · ToSmaller=3。）
修法：把 `nPOTScale` 改成 `0`。⚠️ 这个数字对**本身是 POT 的贴图是惰性的**，
所以两个口径要分清：
  · `nPOTScale: 1` 的 `.meta` 总数（`grep` 口径，`B110_R2` 记的 **427**）
  · 其中**真的 NPOT、会被缩放**的（**行动集**，见 `--scan-npot` 现读）
⛔ `Plugins/` 是第三方插件自带资产，默认不动（`--include-plugins` 才动）。

── 二、音频导入设置（账 `B-25` / `B-138`）─────────────────────────────────────
**先判死「判据能不能拿到」**（铁律 18·0：拿不到就不许自己编）：
  · `AudioImporter` 的 `sampleRateOverride` / `normalize` / `3D` / `quality` / `forceToMono` /
    `ambisonic` 是**编辑器侧的导入设置**，**构建时被消费掉、不进包体** ⇒ 本地**没有任何真值源**。
  · 🔴 **2026-11-01（`B-138`）换源**：原指 `assetripper_out/.../AudioClip/*.meta`（**651 份**）；
    现指 `工具/_sfx_audio_truth.json`（**2,549 条去重名字**，来自原版 **30 个含 AudioClip 的 bundle** 的 typetree）。
    **换的理由是「覆盖」、不是「AssetRipper 的值不可信」** —— 两源**值上 0 差异**（643 条同名 clip
    逐字段相同，另 8 条是 `_0` 后缀去歧义名）；但 AssetRipper **完全够不到 VO**（`vo_*` = 0），
    而 `diff` 只在 `common` 上算 ⇒ 旧源下 **`vo/` 那 1,893 条永远进不了名单**。
    读法**复用共用件 `_sfx_meta`**（`original_prov`：精确 → `NAME_ALIASES` → 去首尾空白索引），
    ⛔ 不在这里另抄一份真值/别名（同一条规则两处写 = 迟早不一致）。
  ⇒ 所以本工具**只对齐那 4 项**（`--fix-audio-prov`），**其余一项都不碰**。
⛔ 另注意：我们 2,494 份全是 `sampleRateSetting: 0`（PreserveSampleRate）+
   `sampleRateOverride: 44100` —— **override 是死值**（`sampleRateSetting != 2` 时 Unity 不看你这个词），
   即「我们把音频全重采样到 44.1k」这个说法**不成立**。本工具**不改它**（改了也没有任何行为变化，
   只会把 2,494 份 `.meta` 全刷一遍），只在 `--scan-audio` 里点出来。

只读：⛔ 不写 `d:/2` · ⛔ 不跑 Unity · ⛔ 不动 git。
"""
import argparse
import hashlib
import io
import os
import re
import shutil
import struct
import sys

DEFAULT_ASSETS = r"d:/4/Unity/MyGame/Assets"
DEFAULT_BACKUP = r"d:/4/_tmp_view/b111_t2_backup"
# 原版「能进包」的 4 项真值源 = `_sfx_meta` 的真值表（30 个 bundle 的 typetree，2,549 条）
ORIG_AUDIO_TRUTH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_sfx_audio_truth.json")

AUDIO_DIR_REL = "CardPresentation/Resources/Art/audio/"
# 能证明的原版字段（进包体，AssetRipper 从资产读出来）→ 逐条不同
PROVABLE = ("loadType", "compressionFormat", "preloadAudioData", "loadInBackground")
# 拿不到真值的字段（651 份里恒为同一个值 = 填充）→ ⛔ 不碰
UNPROVABLE = ("sampleRateSetting", "sampleRateOverride", "quality", "conversionMode",
              "forceToMono", "normalize", "ambisonic", "3D")

RE_NPOT = re.compile(rb"(nPOTScale: )(-?\d+)")
RE_DEFBLK = re.compile(rb"(defaultSettings:)(.*?)(platformSettingOverrides)", re.S)


def log(s=""):
    print(s, flush=True)


# ────────────────────────────────────────────────────────── 文件读写（二进制，绝不翻行尾）
def rb(p):
    with io.open(p, "rb") as f:
        return f.read()


def wb(p, b):
    d = os.path.dirname(p)
    if d and not os.path.isdir(d):
        os.makedirs(d)
    with io.open(p, "wb") as f:
        f.write(b)


def relpath(p, root):
    return os.path.relpath(p, root).replace(os.sep, "/")


def walk(assets):
    for root, _dirs, files in os.walk(assets):
        for f in files:
            yield os.path.join(root, f)


# ────────────────────────────────────────────────────────── PNG 头
def png_size(p):
    b = rb(p)[:33]
    if len(b) < 24 or b[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    return struct.unpack(">II", b[16:24])


def is_pot(n):
    return n > 0 and (n & (n - 1)) == 0


# ────────────────────────────────────────────────────────── 备份
def backup(paths, assets, backup_dir):
    """把 paths 按相对路径备份到 backup_dir，并追加 manifest.tsv（相对路径 \\t md5）。"""
    if not paths:
        return
    man = os.path.join(backup_dir, "manifest.tsv")
    if not os.path.isdir(backup_dir):
        os.makedirs(backup_dir)
    lines = []
    for p in paths:
        rel = relpath(p, assets)
        dst = os.path.join(backup_dir, rel)
        b = rb(p)
        wb(dst, b)
        lines.append("%s\t%s" % (rel, hashlib.md5(b).hexdigest()))
    with io.open(man, "ab") as f:
        f.write(("\n" + "\n".join(lines) + "\n").encode("utf-8"))
    log("已备份 %d 份 → %s（manifest: %s）" % (len(paths), backup_dir, man))


# ────────────────────────────────────────────────────────── ① NPOT
def scan_npot(assets, include_plugins=False):
    rows = []          # (abs_png, rel, w, h, cur_value_or_None, reason)
    n_png = 0
    n_npot = 0
    n_meta1 = 0
    for p in walk(assets):
        if p.lower().endswith(".meta"):
            b = rb(p)
            if RE_NPOT.search(b):
                m = RE_NPOT.search(b)
                if m.group(2) == b"1":
                    n_meta1 += 1
            continue
        if not p.lower().endswith(".png"):
            continue
        n_png += 1
        s = png_size(p)
        if not s:
            continue
        if is_pot(s[0]) and is_pot(s[1]):
            continue
        n_npot += 1
        rel = relpath(p, assets)
        if not include_plugins and rel.startswith("Plugins/"):
            continue
        mp = p + ".meta"
        cur = None
        if os.path.isfile(mp):
            m = RE_NPOT.search(rb(mp))
            cur = m.group(2).decode() if m else None
        if cur == "0":
            continue
        rows.append((p, rel, s[0], s[1], cur, "meta 缺失 nPOTScale" if cur is None else "nPOTScale=%s" % cur))
    return rows, dict(png_total=n_png, npot_total=n_npot, meta_npot1=n_meta1)


def print_npot(rows, stat, assets):
    log("── NPOT 普查（%s）──" % assets)
    log("PNG %d 张 · 其中 NPOT %d 张 · `nPOTScale: 1` 的 .meta **%d** 份（grep 口径）"
        % (stat["png_total"], stat["npot_total"], stat["meta_npot1"]))
    log("⚠️ 两个口径别混：grep 口径含「本身是 POT、设置惰性」的那些；"
        "**行动集 = 真 NPOT 且 nPOTScale != 0 = %d**" % len(rows))
    agg = {}
    for _p, rel, _w, _h, _cur, _r in rows:
        k = "/".join(rel.split("/")[:2]) if rel.count("/") >= 1 else rel
        agg[k] = agg.get(k, 0) + 1
    for k in sorted(agg, key=lambda x: -agg[x]):
        log("  %5d  %s" % (agg[k], k))
    return rows


def fix_npot(rows, assets, backup_dir, apply_):
    todo = []
    for p, rel, w, h, cur, reason in rows:
        mp = p + ".meta"
        if not os.path.isfile(mp):
            log("  ⛔ 跳过（没有 .meta，本工具不生成）: %s" % rel)
            continue
        b = rb(mp)
        m = RE_NPOT.search(b)
        if not m:
            log("  ⛔ 跳过（.meta 里没有 nPOTScale 键，非 TextureImporter？）: %s" % rel)
            continue
        todo.append((mp, m.group(0), b[:m.start()] + b"nPOTScale: 0" + b[m.end():]))
    log("── NPOT 待改 %d 份（%dx%d 例：%s）──"
        % (len(todo), rows[0][2], rows[0][3], rows[0][1]) if rows else "── NPOT 待改 0 份 ──")
    if not apply_:
        log("（演算模式：未写盘。加 --apply 才真改）")
        return 0
    backup([t[0] for t in todo], assets, backup_dir)
    n = 0
    for mp, _old, new in todo:
        wb(mp, new)
        n += 1
    log("已改 %d 份 .meta：nPOTScale → 0" % n)
    return n


# ────────────────────────────────────────────────────────── ② 音频
def parse_audio_meta(b):
    """返回 dict；取不到 AudioImporter 返回 None。"""
    if b"AudioImporter" not in b:
        return None
    d = {}
    m = RE_DEFBLK.search(b)
    if m:
        for k in (b"loadType", b"sampleRateSetting", b"sampleRateOverride", b"compressionFormat",
                  b"quality", b"conversionMode", b"preloadAudioData"):
            mm = re.search(rb"^\s+" + k + rb": (-?[\d.]+)$", m.group(2), re.M)
            if mm:
                d[k.decode()] = mm.group(1).decode()
    for k in (b"forceToMono", b"normalize", b"loadInBackground", b"ambisonic", b"3D"):
        mm = re.search(rb"^  " + k + rb": (-?\d+)$", b, re.M)
        if mm:
            d[k.decode()] = mm.group(1).decode()
    return d


def _sfxmod():
    """懒加载共用件 `工具/_sfx_meta.py` —— 真值表 / `NAME_ALIASES` / 去首尾空白索引
    **只有那一处正本**（⛔ 不在这里另抄一份）。读不到 ⇒ 返回 None 并**出声**（⛔ 不静默退回旧源）。"""
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    try:
        import _sfx_meta
    except Exception as e:                              # noqa: BLE001
        log("⛔ 读不到 `工具/_sfx_meta.py`（%s）⇒ 原版侧 0 条，逐条比全部跳过" % e)
        return None
    if not _sfx_meta.load_truth(ORIG_AUDIO_TRUTH):
        log("⛔ 原版真值表空/读不动：%s ⇒ 原版侧 0 条，逐条比全部跳过" % ORIG_AUDIO_TRUTH)
        return None
    return _sfx_meta


def scan_audio(assets, top=25):
    our_dir = os.path.join(assets, AUDIO_DIR_REL.replace("/", os.sep))
    ours = {}
    noimp = 0
    for p in walk(our_dir):
        if not p.endswith(".meta"):
            continue
        d = parse_audio_meta(rb(p))
        if d is None:
            noimp += 1
            continue
        ours[os.path.basename(p)[:-5]] = (p, d)     # 键 = `<clip 名>.<ext>`（对齐旧口径）
    mod = _sfxmod()
    orig = mod.load_truth(ORIG_AUDIO_TRUTH) if mod is not None else {}
    log("── 音频导入设置普查 ──")
    log("我们：%d 份 .meta 带 AudioImporter（无 importer %d）· 目录 `%s`"
        % (len(ours), noimp, AUDIO_DIR_REL))
    log("原版真值源：%s（%d 条去重 clip 名）" % (ORIG_AUDIO_TRUTH, len(orig)))

    ALL_FIELDS = PROVABLE + UNPROVABLE

    def dist(src, name, fields=ALL_FIELDS):
        from collections import Counter
        c = Counter()
        for _k, d in src.items():
            for f in fields:
                c[(f, d.get(f))] += 1
        log("  %s 分布：" % name)
        for f in fields:
            vals = sorted([(k[1], v) for k, v in c.items() if k[0] == f],
                          key=lambda x: -x[1])
            log("    %-20s %s" % (f, " · ".join("%s×%d" % (a, b) for a, b in vals[:6])))

    dist({k: v[1] for k, v in ours.items()}, "我们")
    dist(orig, "原版(真值表)", PROVABLE)          # 真值表只有这 4 项（另 2 项只给 --verify 用）

    log("  ✅ 原版那 %d 条是 **AudioClip typetree 的逐条真值**（不是填充）；"
        "`%s` 这几项在本地**没有真值源**（编辑器侧设置、不进包体），⛔ 本工具不碰。"
        % (len(orig), "/".join(UNPROVABLE)))
    log("  ⚠️ 我们 2,494 份都是 sampleRateSetting: 0（PreserveSampleRate）+ sampleRateOverride: 44100 "
        "⇒ **override 是死值**（只有 sampleRateSetting=2 才生效），实际没有重采样。")

    diff, missing_truth, n_common = [], [], 0
    for k in sorted(ours):
        d_r = mod.original_prov(os.path.splitext(k)[0]) if mod is not None else None
        if not d_r:                                  # 真值表里查不到 ⇒ ⛔ 不拿多数值硬套
            missing_truth.append(k)
            continue
        n_common += 1
        d_o = ours[k][1]
        bad = [(f, d_o.get(f), str(d_r[f])) for f in PROVABLE if d_o.get(f) != str(d_r[f])]
        if bad:
            diff.append((k, ours[k][0], bad))
    log("  逐条比（有真值 %d 份 · **查不到真值 %d 份**，只比那 %d 个能进包的字段）："
        % (n_common, len(missing_truth), len(PROVABLE)))
    if missing_truth:
        log("    ⚠️ 查不到真值（⛔ 不猜、不写）：%s%s"
            % (", ".join(missing_truth[:top]), " …" if len(missing_truth) > top else ""))
    from collections import Counter
    byfield = Counter(f for _k, _p, bad in diff for f, _a, _b in bad)
    log("    不一致 %d 份：%s" % (len(diff), dict(byfield)))
    for k, _p, bad in diff[:top]:
        log("      %-40s %s" % (k, " · ".join("%s 我们=%s 原版=%s" % t for t in bad)))
    return ours, orig, diff


def fix_audio_prov(diff, assets, backup_dir, apply_):
    log("── 音频「能证明的字段」待改 %d 份 ──" % len(diff))
    if not diff:
        return 0
    if not apply_:
        log("（演算模式：未写盘。加 --apply 才真改）")
        return 0
    backup([p for _k, p, _b in diff], assets, backup_dir)
    n = 0
    for _k, p, bad in diff:
        b = rb(p)
        for f, _a, want in bad:
            key = f.encode()
            w = want.encode()
            m = RE_DEFBLK.search(b)
            if m and f in ("loadType", "compressionFormat", "preloadAudioData"):
                blk = m.group(2)
                # ⚠️ 不能用 rb"\1" + want —— want 以数字开头时会被拼成 \10（= 第 10 组），本仓踩过。
                nb = re.sub(rb"(\n\s+" + key + rb": )(-?[\d.]+)",
                            lambda mm, w=w: mm.group(1) + w, blk, count=1)
                b = b[:m.start(2)] + nb + b[m.end(2):]
            else:
                b = re.sub(rb"(^  " + key + rb": )(-?\d+)$",
                           lambda mm, w=w: mm.group(1) + w, b, count=1, flags=re.M)
        wb(p, b)
        n += 1
    log("已改 %d 份 .meta" % n)
    return n


# ────────────────────────────────────────────────────────── 回滚
def do_revert(assets, backup_dir, apply_):
    man = os.path.join(backup_dir, "manifest.tsv")
    if not os.path.isfile(man):
        log("⛔ 找不到备份清单：%s" % man)
        return 1
    seen = set()
    n = 0
    for line in io.open(man, encoding="utf-8").read().splitlines():
        if not line.strip():
            continue
        rel, md5 = line.split("\t")
        if rel in seen:
            continue
        seen.add(rel)
        src = os.path.join(backup_dir, rel)
        dst = os.path.join(assets, rel)
        if not os.path.isfile(src):
            log("  ⛔ 备份里没有：%s" % rel)
            continue
        b = rb(src)
        if hashlib.md5(b).hexdigest() != md5:
            log("  ⛔ 备份 md5 不符，跳过：%s" % rel)
            continue
        if apply_:
            wb(dst, b)
        n += 1
    log("%s %d 份（%s）" % ("已恢复" if apply_ else "将恢复", n, backup_dir if apply_ else "演算模式"))
    return 0


# ────────────────────────────────────────────────────────── main
def main():
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--scan-npot", action="store_true")
    ap.add_argument("--fix-npot", action="store_true")
    ap.add_argument("--scan-audio", action="store_true")
    ap.add_argument("--fix-audio-prov", action="store_true")
    ap.add_argument("--revert", action="store_true")
    ap.add_argument("--apply", action="store_true", help="真的写盘（缺省 = 只演算）")
    ap.add_argument("--assets", default=DEFAULT_ASSETS)
    ap.add_argument("--backup-dir", default=DEFAULT_BACKUP)
    ap.add_argument("--include-plugins", action="store_true")
    ap.add_argument("--top", type=int, default=25)
    ap.add_argument("-h", "--help", action="store_true")
    args = ap.parse_args()
    if args.help or not (args.scan_npot or args.fix_npot or args.scan_audio
                         or args.fix_audio_prov or args.revert):
        print(__doc__)
        return 0

    rc = 0
    if args.revert:
        rc = do_revert(args.assets, args.backup_dir, args.apply)
    if args.scan_npot or args.fix_npot:
        rows, stat = scan_npot(args.assets, args.include_plugins)
        print_npot(rows, stat, args.assets)
        if args.fix_npot:
            fix_npot(rows, args.assets, args.backup_dir, args.apply)
    if args.scan_audio or args.fix_audio_prov:
        _ours, _orig, diff = scan_audio(args.assets, args.top)
        if args.fix_audio_prov:
            fix_audio_prov(diff, args.assets, args.backup_dir, args.apply)
    return rc


if __name__ == "__main__":
    sys.exit(main())
