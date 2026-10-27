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
  · `MatCap_Card_Level1.png`         材质 `_MatCap` 的 Tier1；🆕 2026-10-21 起还有 `MatCap_Bronze/Silver/Gold`
                                     （原版 `CardMaterialHelper.matcapTexturesByCardTier` 四档 —— 原来这里写
                                     「其它 tier 本地没解出来」**是错的**，见下面 JOBS 里那段订正）

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
# 🆕 2026-10-21（W61 第 2 批 · 第 6 条）：`Matcap Gold` 导出器**没导**（`WarpforgeVFX/Textures/` 里没有），
#   直接从解包原图拿 —— 判据见下面对应那一行的注释。
ASSETS = "d:/2/新解包资源/assets_full"

# (源, 目标名, 说明)
JOBS = [
    (f"{VFX}/Meshes/Card 3D WH40k.asset",        "Card 3D WH40k.asset", "网格（881 顶点 / UV0+UV1+UV2）"),
    (f"{VFX}/Textures/WF 3D Card_Card 3D_BaseColor.png", "Card3D_BaseColor.png", "`_BaseMap` 底板图集 512²"),
    (f"{VFX}/Textures/MatCap Card Level 1.png",  "MatCap_Card_Level1.png", "`_MatCap`（只有 tier1 这一张）"),
    # 🆕 2026-10-21（W61 第 2 批 · 第 6 条）：**matcap 四档** —— 🔴 原来这一行上面写着「只有 tier1 这一张 /
    #   其它 tier 的 matcap 本地没解出来」，**那是错的**（铁律 5 就地订正）。
    #   判据 = `CardMaterialHelper.matcapTexturesByCardTier`（`bundle_scenes_scenes_battlearena1` 的
    #   `MonoBehaviour_4274`）是 4 个 Texture2D 引用，UnityPy 逐 PathID 落到
    #   `battlesharedresources_assets_all.bundle`：
    #     [0] Tier1 `MatCap Card Level 1`（上一行）· [1] Tier2 `Matcap Bronze` ·
    #     [2] Tier3 `Matcap Silver` · [3] Tier4 `Matcap Gold`（`CardTier.cs` = Tier1=0 … Tier4=3）。
    #   ⚠️ 前两张 `WarpforgeVFX/Textures/` 里有（导出器产物）；**Gold 那份没有** ⇒ 取解包原图。
    (f"{VFX}/Textures/Matcap Bronze.png",        "MatCap_Bronze.png",       "`_MatCap` Tier2（原版 `Matcap Bronze`）"),
    (f"{VFX}/Textures/Matcap Silver.png",        "MatCap_Silver.png",       "`_MatCap` Tier3（原版 `Matcap Silver`）"),
    (f"{ASSETS}/bundle_battlesharedresources_assets_all/Texture2D/Matcap Gold.png", "MatCap_Gold.png",
     "`_MatCap` Tier4（原版 `Matcap Gold`；导出器没导，取解包原图）"),
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
        # 🔴 2026-10-27（执行代理 B9_W35CHECK）：`--check` 必须**连产物一起查** ——
        #   原来只查「源在不在」（上面那个 `exist` 循环），产物缺失/过期一律静默绿。
        #   判据同 工具/import_original_art.py `--only-altart`（`:1714-1721`）。
        lack = [dst for _s, dst, _n in JOBS if not os.path.exists(os.path.join(DEST, dst))]
        if lack:
            print(f"\n🔴 产物缺失 {len(lack)}/{len(JOBS)}（{DEST}）—— 重跑不带 --check 落盘：")
            for d in lack:
                print("   缺:", d)
        else:
            print(f"\n✅ 产物齐全 {len(JOBS)}/{len(JOBS)}（{DEST}）")
        print("--check：只体检，未写文件")
        return 0 if not lack else 1

    os.makedirs(DEST, exist_ok=True)
    for src, dst, _ in JOBS:
        shutil.copyfile(src, os.path.join(DEST, dst))
    total = sum(os.path.getsize(os.path.join(DEST, f)) for f in os.listdir(DEST) if not f.endswith(".meta"))
    print(f"\n已写入 {len(JOBS)} 个 → {DEST}（{total/1024:.0f} KB）")
    print("⚠️ 导完在 Unity 里跑一次 `ArtBaker.ApplyImportSettings`（贴图的 mipmap/alpha 那套统一设置）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
