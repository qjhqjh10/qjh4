#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""import_original_3dcard.py — 把原版**场上那张 3D 卡体**要的资产导进工程

**为什么要它**：原版场上的卡不是一块平面立绘，是一张**贴了立绘的厚 3D 卡**（薄板 + 滚圆底边，
靠 **matcap 假光照**出立体感，材质 Unlit）。资源全在本地、也早导进 `Assets/WarpforgeVFX/` 了，
但**那一份在 gitignore 里、而且运行时读不到**（不在 `Resources/` 下）。
卡面其它美术走的是 `CardPresentation/Resources/Art/` + `CardArt` 那条路 ⇒ 这里照同一条路搬一份。

规格与出处：`资料/3DBody_原版场上卡体规格.md`（mesh 881 顶点 / 材质 14 属性 / UV1 那套 mask）。

导出到哪
--------
`Assets/CardPresentation/Resources/Art/card3d/`（**gitignore**，本地件，和 `cards/`、`card_sdf/` 同规矩）：
  · `Card 3D WH40k.asset`            网格（**UV1 是立绘那套、UV2 是计数器面板开关** —— 三个通道都在，实读确认过）
  · `Card3D_BaseColor.png`           材质 `_BaseMap`：512² 底板图集（正/背/侧/滚边/计数器面板）
  · `MatCap_Card_Level1.png`         材质 `_MatCap`（**只有 Level 1 有**，其它 tier 的 matcap 本地没解出来）

用法
----
  PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/import_original_3dcard.py
  # 加 --check 只体检、不写文件
"""
import argparse
import os
import shutil
import sys

VFX = "d:/4/Unity/MyGame/Assets/WarpforgeVFX"
DEST = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/card3d"

# (源, 目标名, 说明)
JOBS = [
    (f"{VFX}/Meshes/Card 3D WH40k.asset",        "Card 3D WH40k.asset", "网格（881 顶点 / UV0+UV1+UV2）"),
    (f"{VFX}/Textures/WF 3D Card_Card 3D_BaseColor.png", "Card3D_BaseColor.png", "`_BaseMap` 底板图集 512²"),
    (f"{VFX}/Textures/MatCap Card Level 1.png",  "MatCap_Card_Level1.png", "`_MatCap`（只有 tier1 这一张）"),
    # 🆕 2026-09-25：**卡底那枚软阴影**（原版 `BlobShadowController` 用的 sprite）。
    #   · 原版 sprite：`Card blob shadow`（`bundle_battleprefabs_vfxandmisc_assets_all/Sprite/Card blob shadow.json`）
    #     矩形 128×128、`m_PixelsToUnits = 100`、pivot (0.5,0.5)；`textureRect` = 124.848² @ (2.076,1.076)
    #     ⇒ 导出的这张是**裁掉透明边**的 125×125 内容。材质 `Everguild_Cards_BlobShadow`（`_MainTex` 那张）。
    #   · 为什么走这条路：`WarpforgeVFX/` 在 gitignore 里、**运行时读不到**（不在 `Resources/` 下），
    #     和上面三件的理由一样。见 `项目任务.md` §三 第 12 条 第 3 项。
    (f"{VFX}/Textures/Card blob shadow_sprite.png", "CardBlobShadow.png", "卡底软阴影 sprite（125² / 原版 PPU 100）"),
    # 🆕 2026-09-26：**「未行动」绿光**那两颗粒子系统（原版 `CardPrefab/…/3DBody/CanActParticles` +
    #   它的子节点 `RotatingRing`）要的两张贴图。规格 → `项目任务.md` §三 第 12 条第 3 项。
    #   · `CanActParticles` 的材质 = **`Circle_Hoop Additive`**，`_BaseMap` → 贴图 **`Circle_Hoop`**
    #     （原版那份在 `bundle_duplicateassetisolation_assets_all`；shader 就是 **URP 自带的
    #     `Universal Render Pipeline/Particles/Unlit`**，所以这张是唯一要搬的外部件）。
    #   · `RotatingRing` 的材质 = `Sparks UI Additive Scroll`，`_MainTex` → 贴图 **`Spark UI`**
    #     （在原版 `battleprefabs_vfxandmisc` 包里；shader = `Everguild/FX/Halo UV scroll`，运行时从随包 bundle 取）。
    #   · ⚠️ 走这条路的理由同上：`WarpforgeVFX/` 在 gitignore 里、**运行时读不到**（不在 `Resources/` 下）。
    (f"{VFX}/Textures/Circle_Hoop.png", "CanAct_CircleHoop.png", "未行动绿光 `_BaseMap`（原版 `Circle_Hoop`）"),
    (f"{VFX}/Textures/Spark UI.png",   "CanAct_SparkUI.png",    "未行动绿光 `RotatingRing._MainTex`（原版 `Spark UI`）"),
    #   · ⚠️ `RotatingRing` 的**网格**是 `FxObject_cylinder_short`（**不是 `Cylinder_Ring`**）——
    #     实据 = UnityPy 直读 bundle，`ParticleSystemRenderer_1479589607930043328.m_Mesh`
    #     pathID `4959531874643241410` ⇒ `Mesh.m_Name`。两者都在同一个包里，别按名字猜。
    (f"{VFX}/Meshes/FxObject_cylinder_short.asset", "FxObject_cylinder_short.asset", "未行动绿光 `RotatingRing` 的网格"),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只体检，不写文件")
    args = ap.parse_args()

    ok = True
    for src, dst, note in JOBS:
        exist = os.path.exists(src)
        size = os.path.getsize(src) // 1024 if exist else -1
        print(f"  [{'✓' if exist else '✗'}] {dst:<26} {note:<34} ← {src}  ({size} KB)")
        if not exist:
            ok = False
    if not ok:
        print("\n!! 有源文件缺失 —— 那些是导出器产物，先跑一次 `EffectExporter`/`export_full` 把它们生成出来")
        return 1

    if args.check:
        print("\n--check：只体检，未写文件")
        return 0

    os.makedirs(DEST, exist_ok=True)
    for src, dst, _ in JOBS:
        shutil.copyfile(src, os.path.join(DEST, dst))
    total = sum(os.path.getsize(os.path.join(DEST, f)) for f in os.listdir(DEST) if not f.endswith(".meta"))
    print(f"\n已写入 {len(JOBS)} 个 → {DEST}（{total/1024:.0f} KB）")
    print("⚠️ 导完在 Unity 里跑一次 `ArtBaker.ApplyImportSettings`（贴图的 mipmap/alpha 那套统一设置）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
