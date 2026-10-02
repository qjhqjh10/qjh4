#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_vfx_texture_mips.py —— 把**源包里**每张 `Texture2D` 的**尺寸与 mip 层数**落成一张旁挂表。

**为什么要它**（而不是在 C# 里枚举）：`battleprefabs_vfxandmisc_assets_all.bundle` 里
UnityPy 实读有 **346 张** `Texture2D`，而 Unity 侧 `AssetBundle.LoadAllAssets<Texture2D>()`
**只返回 5 张** —— 这正是本工程记过档的「**bundle 里非 addressable 的资产两条枚举路都拿不到**」
（判据 → `资料/已知的坑.md`）。所以走既定的**「旁挂数据 + 生成脚本」**路：
python 表 → TSV → C#（`EffectExporter.FixTextureImportSettings`）读表改 `.meta`。

🔴 **必须扫全部包，不是一个包**（2026-10-02 踩）：第一版只扫 `battleprefabs_vfxandmisc_assets_all`，
结果 `Shine trail`（**真正出问题的那张**）**根本没进表** —— 它在
**`battlesharedresources_assets_all.bundle`** 里。效果 prefab 引用的贴图**散在好几个包里**，
而工程里 `Assets/WarpforgeVFX/Textures/` 有 **452 张 PNG**（比单个包的 346 张多）。

产出：`数据/游戏数据/vfx_texture_mips.tsv`（列：`name  width  height  mipCount  bundle`）

用法：
  "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_vfx_texture_mips.py [--check]

⚠️ 同名贴图在**多个包**里出现时：mip 层数**一致**就合并（记第一个来源）；**不一致就大声报出来**
   （那说明「该听谁的」本身是个问题，不能静默挑一个）。
"""
import glob
import io
import os
import sys

import UnityPy

BUNDLE_DIR = (r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa"
              r"\StandaloneWindows64")
OUT = r"d:/4/Unity/数据/游戏数据/vfx_texture_mips.tsv"


def main():
    check = "--check" in sys.argv
    if not os.path.isdir(BUNDLE_DIR):
        print(f"!! 缺目录：{BUNDLE_DIR}")
        return 1

    bundles = sorted(glob.glob(os.path.join(BUNDLE_DIR, "*.bundle")))
    print(f"要扫 {len(bundles)} 个包")

    rows = {}          # name -> (w, h, mips, bundle)
    conflicts = []
    read_fail = 0
    for i, p in enumerate(bundles):
        bn = os.path.basename(p)
        try:
            env = UnityPy.load(p)
        except Exception as e:
            print(f"  !! 打不开 {bn}: {e}")
            continue
        for o in env.objects:
            if o.type.name != "Texture2D":
                continue
            try:
                d = o.read()
            except Exception:
                read_fail += 1
                continue
            name = getattr(d, "m_Name", None)
            w = getattr(d, "m_Width", None)
            m = getattr(d, "m_MipCount", None)
            if not name or w is None or m is None:
                read_fail += 1
                continue
            rec = (int(w), int(getattr(d, "m_Height", 0)), int(m), bn)
            old = rows.get(name)
            if old is None:
                rows[name] = rec
            elif old[2] != rec[2]:
                conflicts.append((name, old, rec))
        if (i + 1) % 20 == 0:
            print(f"  …扫到 {i+1}/{len(bundles)}，累计 {len(rows)} 张")

    out = sorted(rows.items())
    from collections import Counter
    dist = Counter(v[2] for _, v in out)
    print(f"扫完：**{len(out)} 张唯一 Texture2D**（读不出的对象 {read_fail}）")
    print(f"m_MipCount 分布 {dict(sorted(dist.items()))}")
    print(f"**无 mip（=1）的 {dist.get(1, 0)} 张** —— 这些在工程里必须是 `mipmapEnabled = false`")
    if conflicts:
        print(f"🔴 **{len(conflicts)} 张同名贴图在不同包里 mip 层数不一致**（不静默挑一个）：")
        for n, a, b in conflicts[:20]:
            print(f"    {n}: {a[3]} m{a[2]}  vs  {b[3]} m{b[2]}")
    else:
        print("同名贴图的 mip 层数在各包之间**无冲突**")

    if check:
        if os.path.exists(OUT):
            old = io.open(OUT, encoding="utf-8").read().splitlines()[1:]
            print(f"--check：现有表 {len(old)} 行，{'一致' if len(old) == len(out) else '**行数不一致，要重跑**'}")
        else:
            print(f"--check：{OUT} 不存在")
        return 0

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("name\twidth\theight\tmipCount\tbundle\n")
        for name, v in out:
            f.write(f"{name}\t{v[0]}\t{v[1]}\t{v[2]}\t{v[3]}\n")
    print(f"写出 {OUT}（{len(out)} 行）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
