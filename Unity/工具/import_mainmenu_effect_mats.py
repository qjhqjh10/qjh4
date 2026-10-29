# -*- coding: utf-8 -*-
"""import_mainmenu_effect_mats.py —— 把「卡片展示窗」那 4 张粒子材质 + 2 张主菜单特效材质搬进 `Resources/`（运行时取得到）。

为什么需要它：
  原版的粒子与「升级边框特效」的材质**只在 bundle 里**，而 `Resources.Load` 只能读 `Resources/` 下的资产
  ⇒ 不搬这一趟，粒子只能画成白方块、升级动画是**静默 no-op**（写进 `Sprites/Default` = 没有 `_Reveal`）。

产物：`d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/vfx/<原版材质名>.mat`（6 个，重跑幂等）
  ⚠️ `.meta` 由 Unity 生成（第一次导入前不存在是正常的）。

两档来源（判据逐条可复查）：
  ① **字节拷贝**（4 张粒子材质）：`Assets/WarpforgeVFX/Materials/` 里特效线已经重建好的同名材质。
       · 原版名 = `bundle_duplicateassetisolation_assets_all/Material/Material_{…}.json` 的 `m_Name`
         （`-3062488692366390093` AirSharpSwirl · `4206261991850403090` Glow Rays UI ·
          `-6937851114935498264` Lightning Random UI · `-8031580538284717323` Spark Additive Extra Color）
       · `Lightning Random UI`：原版 `_Blend 2 / _SrcBlend 5 / _DstBlend 1` == 工程 `Lightning Random add.mat`
  ② **新生成**（2 张 UI 特效材质；工程里原来没有，按原版材质 JSON 逐字段建）：
       · `Border Effect For Level up` —— 原版
         `bundle_scenes_scenes_mainmenuwarpforge/Material/Material_4.json`
         shader = `Everguild/UI/UI Border Highlight Appear Pulsating Scale`（`Shader_-4463273156108355430`）
         ⇒ 工程等价 shader = `WarpforgeVFX/UI/UI Border Highlight`（`WFUIBorderHighlight.shader`，
            guid `8d5ed6ff6f6883649b60650e625a9160`；`_Reveal` / `_Noise_Strength_X_Speed_Y_Scale_Z` 同名）。
         🔴 **只搬【原版 shader 属性表里存在】的字段** —— 原版 shader 的 `m_PropInfo` 19 个属性里没有
            `_Add` / `_Range` / `_RangeStartOffset` / `_ColorMask` / `_NoiseStrength` / `_Length` /
            `_MaskScale` / `_ObjectScreenSpacePosition` / `_OffsetTest` / `_ScaleFromCenter` / `_TempOffset`
            ⇒ 它们是**内置 Standard shader 的残留（死值）**，⛔ **不搬**（CLAUDE.md 铁律 3 + §三那族坑）。
         `_Noise_R` → 原版 `m_TexEnvs._Noise_R`（pathID `-8214795764591836323`）= 工程里的
            `Art/ui_menu/UI_Dirt_And_Noise_skratches.png`（guid `69d5e963143e38e448fb8dca30bd8984`）。
       · `UI Additive` —— 原版 `bundle_menus_assets_all/Material/Material_-8464978790139598553.json`
         （`Rank Icon Glow` 的 `Image.m_Material`；⚠️ **文件名是 pid、`m_Name` 是 `UI Additive`**）。
         shader = `Everguild/UI/Additive`（`Shader_4921318991789134610`，属性表只有
            `_MainTex/_Color/_Stencil*`）—— **不是**边框 shader！
         🔴 **就地订正（铁律 5）**：该材质 `m_SavedProperties` 里的 `_ColorMain 337.79…` / `_Reveal 0.723` /
            `_Noise_Strength_X_Speed_Y_Scale_Z` 全是**上一颗 shader 的残留**（`Everguild/UI/Additive`
            属性表里没有它们）⇒ `Rank Icon Glow` **不是「靠 `_Reveal` 揭开的黄辉光」**，它是
            「白图 × 黄 `m_Color` × **加法混合**」的**加色块**，动画走 `Image.m_Color.a`（见
            `RankedDivisionChangeWindow.cs` 的 `alpha` 通道），**不是** `_Reveal`。
         工程等价 shader = `WarpforgeVFX/Sprites/Additive`（`WFSpritesAdditive.shader`，
            guid `c73bc767f6b789c43a14f857f298032d`；同为「加法、白图 × `_Color`」；
            🔶 原版 `Everguild/UI/Additive` 本机**没有单独重建**，这里用同为加法的 Sprite 版等价 —— 无 mask/stencil 的纯色块两者一致）。

用法（**默认只打印用法，不写盘**）：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/import_mainmenu_effect_mats.py --apply
  -h / --help   只打印用法并退出（退出码 0）
"""
import io
import os
import sys

SRC = r"d:/4/Unity/MyGame/Assets/WarpforgeVFX/Materials"
OUT = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/vfx"

# ① 字节拷贝：原版材质名 -> 工程里那份源文件（见文件头「判据」）
MAP = {
    "AirSharpSwirl": "AirSharpSwirl.mat",
    "Glow Rays UI": "Glow Rays UI.mat",
    "Spark Additive Extra Color": "Spark Additive Extra Color.mat",
    "Lightning Random UI": "Lightning Random add.mat",
    # 🆕 2026-10-29（B97_PROFILE · 账 `个人资料_成就_排行:221`）：段位变化窗两棵粒子树用到的两张。
    #   判据 = `Resources/Art/ranked_division_particles.json` 的 `material` 列现读（生成器
    #   `工具/gen_ranked_division_particles.py`）：`Up Rays Additive`（Wave Shine / Wave Shine R）·
    #   `Glow Additive Extra Color`（Trails R / Trails L）。⚠️ 后者在 `WarpforgeVFX/Materials/` 有**两个变体**
    #   （`__75594af2` / `__80c1155a`），实测 `diff` 只差 `m_Name` 一行 ⇒ 取名字干净的那份。
    "Up Rays Additive": "Up Rays Additive.mat",
    "Glow Additive Extra Color": "Glow Additive Extra Color__80c1155a.mat",
}

# shader GUID（.mat 按 GUID 引 shader；URL 见文件头）
GUID_BORDER = "8d5ed6ff6f6883649b60650e625a9160"   # WarpforgeVFX/UI/UI Border Highlight
GUID_SPRITE_ADD = "c73bc767f6b789c43a14f857f298032d"  # WarpforgeVFX/Sprites/Additive
GUID_NOISE = "69d5e963143e38e448fb8dca30bd8984"      # Art/ui_menu/UI_Dirt_And_Noise_skratches.png


def _header(name, guid, render_queue):
    return (
        "%%YAML 1.1\n"
        "%%TAG !u! tag:unity3d.com,2011:\n"
        "--- !u!21 &2100000\n"
        "Material:\n"
        "  serializedVersion: 8\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        "  m_Name: %s\n"
        "  m_Shader: {fileID: 4800000, guid: %s, type: 3}\n"
        "  m_Parent: {fileID: 0}\n"
        "  m_ModifiedSerializedProperties: 0\n"
        "  m_ValidKeywords: []\n"
        "  m_InvalidKeywords: []\n"
        "  m_LightmapFlags: 4\n"
        "  m_EnableInstancingVariants: 0\n"
        "  m_DoubleSidedGI: 0\n"
        "  m_CustomRenderQueue: %d\n"
        "  stringTagMap: {}\n"
        "  disabledShaderPasses: []\n"
        "  m_LockedProperties: \n"
        "  m_SavedProperties:\n"
        "    serializedVersion: 3\n"
    ) % (name, guid, render_queue)


def border_material_yaml():
    """`Border Effect For Level up` —— 逐字段照 `Material_4.json`，**只取原版 shader 属性表里有的**。"""
    s = _header("Border Effect For Level up", GUID_BORDER, -1)
    s += (
        "    m_TexEnvs:\n"
        "    - _Noise_R:\n"
        "        m_Texture: {fileID: 2800000, guid: %s, type: 3}\n"
        "        m_Scale: {x: 1, y: 1}\n"
        "        m_Offset: {x: 0, y: 0}\n"
        "    m_Ints: []\n"
        "    m_Floats:\n"
        "    - _AUTOROTATION: 0\n"
        "    - _AutoRotationSpeed: -180\n"
        "    - _DEBUG: 0\n"
        "    - _FLIPDIRECTION: 0\n"
        "    - _QueueControl: 0\n"
        "    - _QueueOffset: 0\n"
        "    - _Reveal: -0.5\n"
        "    - _Rotation: 0\n"
        "    - _Smoothness: 0.032\n"
        "    - _StartOffset: 0\n"
        "    m_Colors:\n"
        "    - _ColorMain: {r: 0.98929596, g: 0, b: 1, a: 1}\n"
        "    - _ColorSecondary: {r: 0.15395403, g: 0, b: 1, a: 1}\n"
        "    - _Color_offsets: {r: 0.8, g: 1, b: 0, a: 0}\n"
        "    - _Noise_Strength_X_Speed_Y_Scale_Z: {r: 0.02, g: -0.1, b: 8.17, a: 0}\n"
        "    - _Scale_X_Speed_Y: {r: 0.9, g: 3, b: 0, a: 0}\n"
    ) % GUID_NOISE
    s += "  m_BuildTextureStacks: []\n  m_AllowLocking: 1\n"
    return s


def additive_material_yaml():
    """`UI Additive` —— 原版 shader `Everguild/UI/Additive`（白图 × `_Color` 加法）；工程用 Sprites/Additive 等价。
    `_Color` 起始白（运行时 `ImageQuad.SetTint(GlowTint)` 会改黄）；`_ColorMain`/`_Reveal` 那批是原版残留，⛔ 不搬。"""
    s = _header("UI Additive", GUID_SPRITE_ADD, -1)
    s += (
        "    m_TexEnvs: []\n"
        "    m_Ints: []\n"
        "    m_Floats:\n"
        "    - PixelSnap: 0\n"
        "    - _EnableExternalAlpha: 0\n"
        "    m_Colors:\n"
        "    - _Color: {r: 1, g: 1, b: 1, a: 1}\n"
        "    - _RendererColor: {r: 1, g: 1, b: 1, a: 1}\n"
        "    - _Flip: {r: 1, g: 1, b: 1, a: 1}\n"
    )
    s += "  m_BuildTextureStacks: []\n  m_AllowLocking: 1\n"
    return s


# ② 新生成：原版材质名 -> 生成函数
GENERATED = {
    "Border Effect For Level up": border_material_yaml,
    "UI Additive": additive_material_yaml,
}

USAGE = """用法（默认不写盘）：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \\
      d:/4/Unity/工具/import_mainmenu_effect_mats.py --apply
  -h / --help   只打印本用法并退出（退出码 0）

做的事：
  ① 把 4 张原版粒子材质从 `Assets/WarpforgeVFX/Materials/` **字节拷贝**进
     `Assets/CardPresentation/Resources/Art/vfx/`（按**原版材质名**命名）。
  ② 按原版材质 JSON **新生成** 2 张 UI 特效材质（`Border Effect For Level up` · `UI Additive`）。
  产物落 `Assets/CardPresentation/Resources/Art/vfx/`，运行时 `Resources.Load<Material>` 取得到；重跑幂等。
"""


def main(apply):
    rows = []
    # ① 字节拷贝
    for name, src_name in MAP.items():
        src = os.path.join(SRC, src_name)
        dst = os.path.join(OUT, name + ".mat")
        if not os.path.isfile(src):
            rows.append((name, "copy<-" + src_name, "! 源不存在"))
            continue
        data = io.open(src, "rb").read()
        existed = os.path.isfile(dst)
        if existed and io.open(dst, "rb").read() == data:
            rows.append((name, "copy<-" + src_name, "same"))
            continue
        if apply:
            os.makedirs(OUT, exist_ok=True)
            io.open(dst, "wb").write(data)
            rows.append((name, "copy<-" + src_name, "diff" if existed else "new"))
        else:
            rows.append((name, "copy<-" + src_name, "would-copy"))
    # ② 新生成（UTF-8 + LF；幂等：字节相同就跳过）
    for name, gen in GENERATED.items():
        dst = os.path.join(OUT, name + ".mat")
        data = gen().encode("utf-8")
        existed = os.path.isfile(dst)
        if existed and io.open(dst, "rb").read() == data:
            rows.append((name, "gen", "same"))
            continue
        if apply:
            os.makedirs(OUT, exist_ok=True)
            io.open(dst, "wb").write(data)
            rows.append((name, "gen", "diff" if existed else "new"))
        else:
            rows.append((name, "gen", "would-gen"))
    for name, src_name, st in rows:
        print("  %-30s %-34s %s" % (name, src_name, st))
    if not apply:
        print("\n要真写盘请加 --apply")
    return 0


if __name__ == "__main__":
    argv = sys.argv[1:]
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    if any(a in ("-h", "--help") for a in argv):
        print(USAGE)
        sys.exit(0)
    if "--apply" not in argv:
        print(USAGE)
        main(False)
        print("（本次未带 --apply ⇒ 什么都没做）")
        sys.exit(0)
    sys.exit(main(True))
