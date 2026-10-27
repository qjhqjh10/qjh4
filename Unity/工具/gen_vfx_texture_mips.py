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

产出：`数据/游戏数据/vfx_texture_mips.tsv`（列：`name  width  height  mipCount  bundle`<br>
      ＋ 2026-10-19 T07 的 `wrapU wrapV filterMode aniso`<br>
      ＋ 🆕 2026-10-19 V1 的 `npotScale`：**0 = None（原版真值）**、`-1` = 该张是 POT / 无关（别动）。
      ⚠️ 判据：原版 **runtime `Texture2D.m_Width/m_Height` 是 NPOT**（`Glow UI W40K` 123×123 ·
      `trait_icon_*` 80×80 等）⇒ 原版导入**没有**做 POT 缩放（否则 runtime 会是 64/128）。
      player build 里没有 `TextureImporter` 字段（编辑器类不打包；`assets_full` 全库搜
      `nPOTScale` / `TextureImporter` = 0 命中）⇒ **runtime 尺寸就是唯一真值**。
      Unity 新导入默认 `nPOTScale: ToNearest(1)` ⇒ 80×80 会被补成 64×64、123×123 补成 128×128
      （UV/布局全变）。消费侧（`EffectExporter.FixTextureImportSettings`）**本波未接**（需下一次 Unity 导入）。

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

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n  "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_vfx_texture_mips.py [--check]\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

BUNDLE_DIR = (r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa"
              r"\StandaloneWindows64")
OUT = r"d:/4/Unity/数据/游戏数据/vfx_texture_mips.tsv"


def _wrap(d, field):
    """读 `Texture2D.m_TextureSettings.<field>`；读不到返回 -1（**未知**，调用方不许拿它覆盖）。

    🆕 2026-10-19（T07）：`m_WrapU/m_WrapV` = Unity `TextureWrapMode` 的数值
    （`Repeat=0 · Clamp=1 · Mirror=2 · MirrorOnce=3`）· `m_FilterMode`（0=Point 1=Bilinear 2=Trilinear）·
    `m_Aniso`。原版这四格我们**从来没照抄过** —— 导入器一律写死 `Repeat + Bilinear + aniso 1`。
    实测后果：`Data-mat-_MainTex-atlas-0`（Leviathan 背景那张 4096×2048）原版是 **Mirror(2)**、
    我们是 Repeat ⇒ 四边形采样到边缘时**把对侧像素卷回来** = 左右各 2px 的细边（能量比 1.011）。
    """
    try:
        st = getattr(d, "m_TextureSettings", None)
        if st is None:
            return -1
        v = getattr(st, field, None)
        return int(v) if v is not None else -1
    except Exception:
        return -1


def main():
    check = "--check" in sys.argv
    if not os.path.isdir(BUNDLE_DIR):
        print(f"!! 缺目录：{BUNDLE_DIR}")
        return 1

    bundles = sorted(glob.glob(os.path.join(BUNDLE_DIR, "*.bundle")))
    print(f"要扫 {len(bundles)} 个包")

    rows = {}          # name -> (w, h, mips, bundle, wrapU, wrapV, filterMode, aniso)
    conflicts = []
    setting_conflicts = []
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
            rec = (int(w), int(getattr(d, "m_Height", 0)), int(m), bn,
                   _wrap(d, "m_WrapU"), _wrap(d, "m_WrapV"),
                   _wrap(d, "m_FilterMode"), _wrap(d, "m_Aniso"))
            old = rows.get(name)
            if old is None:
                rows[name] = rec
            elif old[2] != rec[2]:
                conflicts.append((name, old, rec))
            elif (old[4:8] != rec[4:8]):
                # 🆕 2026-10-19：同名贴图在**不同包**里 wrap/filter/aniso 不一致 ⇒ 必须报，
                #   不能静默取第一个（「听谁的」本身是个问题）。
                setting_conflicts.append((name, old, rec))
        if (i + 1) % 20 == 0:
            print(f"  …扫到 {i+1}/{len(bundles)}，累计 {len(rows)} 张")

    out = sorted(rows.items())
    from collections import Counter
    dist = Counter(v[2] for _, v in out)
    print(f"扫完：**{len(out)} 张唯一 Texture2D**（读不出的对象 {read_fail}）")
    print(f"m_MipCount 分布 {dict(sorted(dist.items()))}")
    print(f"**无 mip（=1）的 {dist.get(1, 0)} 张** —— 这些在工程里必须是 `mipmapEnabled = false`")

    # 🆕 2026-10-19（V1 / nPOTScale）：原版 runtime 尺寸就是导入后的结果。
    #   NPOT ⇒ 原版导入没做 POT 缩放 ⇒ 我们必须写 `nPOTScale: 0`（None）。
    def _pot(n):
        return n > 0 and (n & (n - 1)) == 0
    npot = [(n, v[0], v[1]) for n, v in out if not (_pot(v[0]) and _pot(v[1]))]
    print(f"**NPOT（宽或高非 2 次幂）的 {len(npot)} 张** ⇒ 导入必须 `nPOTScale = None(0)`"
          f"（Unity 默认 ToNearest 会缩放）")
    for n, w, h in npot:
        print(f"    {n}: {w}x{h}")
    if conflicts:
        print(f"🔴 **{len(conflicts)} 张同名贴图在不同包里 mip 层数不一致**（不静默挑一个）：")
        for n, a, b in conflicts[:20]:
            print(f"    {n}: {a[3]} m{a[2]}  vs  {b[3]} m{b[2]}")
    else:
        print("同名贴图的 mip 层数在各包之间**无冲突**")
    if setting_conflicts:
        print(f"🔴 **{len(setting_conflicts)} 张同名贴图的 wrap/filter/aniso 在不同包里不一致**：")
        for n, a, b in setting_conflicts[:20]:
            print(f"    {n}: {a[3]} wrap({a[4]},{a[5]}) filter{a[6]} aniso{a[7]}  vs  "
                  f"{b[3]} wrap({b[4]},{b[5]}) filter{b[6]} aniso{b[7]}")

    # 产物文本**先算进变量**（`--check` 用它逐字节比对；写盘再原样写出 —— 两边同一份，不会分叉）
    lines = ["name\twidth\theight\tmipCount\tbundle\twrapU\twrapV\tfilterMode\taniso\tnpotScale"]
    for name, v in out:
        # npotScale：NPOT ⇒ 0 (None，原版真值)；POT ⇒ -1（无关，消费侧别动）
        npv = 0 if not (_pot(v[0]) and _pot(v[1])) else -1
        lines.append(f"{name}\t{v[0]}\t{v[1]}\t{v[2]}\t{v[3]}\t{v[4]}\t{v[5]}\t{v[6]}\t{v[7]}\t{npv}")
    text = "".join(l + "\n" for l in lines)

    if check:
        # 🔴 2026-10-27（B9_W45ARENA）：原来这里**只比行数、恒 rc0** —— 内容变了但行数不变时
        #   一个字都报不出来（「看着有 check、其实啥都没查」）。现在改成**逐字节比内容**，
        #   缺失/不一致一律点名 + rc=1（口径同 `工具/import_original_art.py:1714-1721`）。
        if not os.path.exists(OUT):
            print(f"--check：🔴 缺产物 {OUT}（重跑一次 `工具/gen_vfx_texture_mips.py`）")
            return 1
        old = io.open(OUT, encoding="utf-8").read()
        if old == text:
            print(f"--check：一致（{len(out)} 行） → {OUT}")
            return 0
        oldl, newl = old.splitlines(), text.splitlines()
        print(f"--check：🔴 产物过期 {OUT}（盘上 {len(oldl) - 1} 行 / 重算 {len(newl) - 1} 行）")
        for i in range(min(len(oldl), len(newl))):
            if oldl[i] != newl[i]:
                print(f"    第一处不同在第 {i + 1} 行：\n      盘上: {oldl[i]!r}\n      重算: {newl[i]!r}")
                break
        print("    ⇒ 重跑一次 `工具/gen_vfx_texture_mips.py` 生成（`--check` 只读、不写盘）。")
        return 1

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    print(f"写出 {OUT}（{len(out)} 行）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
