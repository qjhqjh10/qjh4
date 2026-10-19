#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""analyze_render_cross.py —— **`RenderCross` 交叉图的「全空」普查**（`T08` 的度量，纯静态、不跑 Unity）。

背景 / 为什么有它
-----------------
`项目任务.md` §三 第 4 条 ⑦ 长期挂着一句 **「`RenderCross` 交叉图 136 张里 97 张全空」**，
而**原始出处找不到**（正本全文 0 命中；全仓只命中 `项目任务.md` 自己与两份二手盘点）。
⇒ 本工具把这件事**换成可复现的口径**：直接量盘上那批交叉图，并把「全空」与
`iso_stats_*.tsv` 里**同一渲染器**的隔离读数对齐，回答「全空是不是缺陷」。

它量的是什么
------------
`EffectIso.TestCrossMaterial`（`Assets/WarpforgeArena1/Editor/EffectIso.cs`）产出的
`_tmp_view/iso/<效果>_cross<NN>_<槽>__<kind>.png`，四种 kind：
  · `exp_with_ORIG_material` 导出 prefab + 原版材质（只拷属性，不换 shader）
  · `exp_with_ORIG_shader`   导出 prefab + 原版材质 **连 shader 一起换**
  · `orig_with_EXP_material` 原版 prefab + 导出材质
  · `orig_no_softparticles`  原版 prefab + 原版材质、关掉 `_SOFTPARTICLES`

判「全空」的定义（**唯一口径**）：整张图**每个通道的极差 ≤ 2/255**，且众数色 = 相机 clear color。
（`RenderCross` 的背景是**纯色** `(0.07,0.08,0.10)` ⇒ 无 backdrop；同一渲染器的「隔离图」
`<效果>_<NN>_<槽>#<槽>__orig.png` **有**棋盘底图，两者不可直接比。）

🔴 关键判据：**「全空」本身不是缺陷指标**。交叉图没有底图、只渲**一帧**、也不像
`RenderIsolated` 那样 `binder.Apply()` / 关 `SpriteMask` ⇒ 抓屏族、依赖深度的软粒子族、
只画拖尾的渲染器、以及导出 prefab 上需要 binder 才拿到材质的槽，**结构上就画不出东西**。
⇒ 本工具把每张全空图**配回同一渲染器的 `iso_stats`**：
   · `iso_lit == 0` ⇒ 隔离渲染本身也没有内容 ⇒ **全空是自洽的**（不是缺陷）
   · `iso_lit > 0`  ⇒ **候选**（需要人看放大图 / 重跑），但仍要按 kind 与兄弟 kind 比。

用法（在 `d:/4/Unity` 下）
--------------------------
    "D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 工具/analyze_render_cross.py
    ... 工具/analyze_render_cross.py --dir d:/4/_tmp_view/iso --tsv out.tsv

⚠️ 本脚本**只读 PNG + 两个 TSV**，不写工程、不跑 Unity。产物 TSV 只落在 `--tsv` 指定的地方。
⚠️ 它量的是**那一次跑**的产物；要换一批就重跑 `EffectIso`（Unity），本工具不能替代渲染。
"""
import argparse
import io
import os
import re
import sys

try:
    from PIL import Image
    import numpy as np
except Exception as e:  # pragma: no cover
    print(f"需要 PIL + numpy：{e}")
    sys.exit(2)

BG = (18, 20, 26)          # 相机 clear color：0.07/0.08/0.10 ×255（四舍五入）
KINDS = ("exp_with_ORIG_shader", "exp_with_ORIG_material",
         "orig_with_EXP_material", "orig_no_softparticles")
CROSS_RE = re.compile(
    r"^(.*)_cross(\d{2})_(.*?)__(" + "|".join(KINDS) + r")\.png$")


def img_stats(path):
    a = np.asarray(Image.open(path).convert("RGB")).astype(np.int16)
    flat = a.reshape(-1, 3)
    mn, mx = flat.min(0), flat.max(0)
    packed = (flat[:, 0].astype(np.int32) << 16) | (flat[:, 1].astype(np.int32) << 8) | flat[:, 2]
    vals, cnts = np.unique(packed, return_counts=True)
    mode = int(vals[cnts.argmax()])
    return dict(rng=int((mx - mn).max()),
                mode=(mode >> 16, (mode >> 8) & 255, mode & 255),
                npix=int(flat.shape[0]))


def load_stats(tsv):
    """image name（不含 .png / 带 __orig 后缀） → lit。"""
    out = {}
    if not os.path.exists(tsv):
        return out
    for line in io.open(tsv, encoding="utf-8"):
        p = line.rstrip("\n").split("\t")
        if len(p) < 6 or p[0] == "target":
            continue
        try:
            out[p[2]] = int(p[4])
        except ValueError:
            pass
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dir", default=r"d:/4/_tmp_view/iso")
    ap.add_argument("--stats", default=r"d:/4/Unity/资料/比对基线/iso_stats_orig.tsv")
    ap.add_argument("--tsv", default="")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass

    stats = load_stats(args.stats)
    if not os.path.isdir(args.dir):
        print(f"目录不存在：{args.dir}")
        return 1

    rows = []
    for fn in sorted(os.listdir(args.dir)):
        m = CROSS_RE.match(fn)
        if not m:
            continue
        tgt, idx, face, kind = m.group(1), int(m.group(2)), m.group(3), m.group(4)
        st = img_stats(os.path.join(args.dir, fn))
        iso_key = f"{tgt}_{idx:02d}_{face}#{idx}__orig"
        rows.append(dict(file=fn, target=tgt, idx=idx, face=face, kind=kind,
                         blank=(st["rng"] <= 2), rng=st["rng"], mode=st["mode"],
                         iso_lit=stats.get(iso_key)))

    if not rows:
        print(f"{args.dir} 下没有 `*_cross*.png`（先跑 EffectIso）。")
        return 1

    print(f"交叉图 {len(rows)} 张（{len(set((r['target'], r['idx'], r['face']) for r in rows))} 个渲染器槽 / "
          f"{len(set(r['target'] for r in rows))} 个目标）")
    print("按 kind：")
    for k in KINDS:
        s = [r for r in rows if r["kind"] == k]
        if not s:
            continue
        b = [r for r in s if r["blank"]]
        print(f"  {k:26s} {len(s):3d}  全空 {len(b):3d} ({len(b)/len(s):.0%})")

    blank = [r for r in rows if r["blank"]]
    print(f"\n全空合计 {len(blank)}/{len(rows)} ({len(blank)/len(rows):.1%})")
    j = [r for r in blank if r["iso_lit"] is not None]
    lit0 = [r for r in j if r["iso_lit"] == 0]
    litp = [r for r in j if (r["iso_lit"] or 0) > 0]
    print(f"  能与 iso_stats 对上的 {len(j)} 张：")
    print(f"    iso_lit == 0（隔离渲染本身没内容 ⇒ 全空自洽） {len(lit0)}"
          + (f" ({len(lit0)/len(j):.1%})" if j else ""))
    print(f"    iso_lit  > 0（候选，需要看放大图 / 重跑）        {len(litp)}")
    if litp:
        by = {}
        for r in litp:
            by.setdefault(r["target"], []).append(r)
        print("    按目标：" + "，".join(f"{t} {len(v)}" for t, v in sorted(by.items())))
        print("    按渲染器槽（同槽的四种 kind 要一起看）：")
        slots = {}
        for r in litp:
            slots.setdefault((r["target"], r["idx"], r["face"]), []).append(r)
        for k, v in sorted(slots.items()):
            kinds = ",".join(sorted(x["kind"] for x in v))
            print(f"      {k[0][:46]:46s} #{k[1]:02d} {k[2][:30]:30s} iso_lit={v[0]['iso_lit']:6d} 全空的 kind: {kinds}")

    if args.tsv:
        with io.open(args.tsv, "w", encoding="utf-8", newline="\n") as f:
            f.write("target\tslot\tface\tkind\tblank\trng\tiso_lit\tfile\n")
            for r in sorted(rows, key=lambda x: (x["target"], x["idx"], x["kind"])):
                f.write(f"{r['target']}\t{r['idx']}\t{r['face']}\t{r['kind']}\t"
                        f"{int(r['blank'])}\t{r['rng']}\t{'' if r['iso_lit'] is None else r['iso_lit']}\t{r['file']}\n")
        print(f"\n落盘：{args.tsv}")

    print("\n⚠️ 结论口径：「全空」**不是**缺陷指标 —— 交叉图没有底图、只渲一帧、也不 Apply binder；"
          "\n   抓屏族 / 深度软粒子 / 只画拖尾 的渲染器在它里面**结构上就画不出东西**。"
          "\n   判缺陷只看「同渲染器的 iso_lit > 0」的那一档，并且要配 `*_iso_orig.png` 一起看。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
