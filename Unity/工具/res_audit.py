#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""res_audit.py —— 资源「三层查」的第 ②层（可用）与第 ③层（正确）。

判据与设计 = `Unity/资料/完全复刻/判据/B109_R9_资源侧剩余.md` §8；
前身 = `Unity/资料/完全复刻/报告/B100_X11.md`（四支临时脚本的固化版）。
第 ① 层（覆盖账 247,612 对象）由 `资源/R0_资源覆盖账.tsv` 承担，本工具**不重扫**。

用法（不带参 = 只打印本段并退出 0）：
  python -I -X utf8 d:/4/Unity/工具/res_audit.py
  python -I -X utf8 d:/4/Unity/工具/res_audit.py --dry                  # 只算不写
  python -I -X utf8 d:/4/Unity/工具/res_audit.py --scan                 # 真跑 + 写盘
  python -I -X utf8 d:/4/Unity/工具/res_audit.py --scan --only 2        # 只跑第 ② 层
  python -I -X utf8 d:/4/Unity/工具/res_audit.py --scan --only 3        # 只跑第 ③ 层
  python -I -X utf8 d:/4/Unity/工具/res_audit.py --scan --no-content    # 跳过逐像素比对（快）
  python -I -X utf8 d:/4/Unity/工具/res_audit.py --scan --top 40        # 摘要里每类列几条

只读：⛔ 不写 `Assets/**` · ⛔ 不写 `d:/2` · ⛔ 不跑 Unity · ⛔ 不动 git。
唯一写盘动作 = `--scan` 时写 `--out`（默认 `资料/完全复刻/资源/B109_资源可用性.tsv`）。

② 可用（7 项）：EXISTS · GUID_OK · TYPE_OK · DEPS_OK · LOADABLE · SIZE_OK · READABLE_OK
   → 状态机 OK / PRESENT_NOT_LOADABLE / DEPS_MISSING / TYPE_MISMATCH / MISSING / UNKNOWN
③ 正确（4 层）：
   P1 参数级 —— 贴图尺寸 · 导入参数(平台块 maxTextureSize/format) · 材质属性值 vs 原版 m_SavedProperties
   P2 内容级 —— 同名同尺寸贴图逐像素比对（对标 `d:/2/新解包资源/assets_full/*/Texture2D/*.png`）；
                音频按字节比对（`assets_full/*/AudioClip/*`）
   P3 绑定级 —— material→shader 是不是原版那一支；贴图尺寸是否 = 原版 sprite 的 textureRect
   P4 行为级 —— 材质声明的属性/关键字在目标 shader 里有没有被消费；原版材质有没有被我们消费（R4-07 类）

输出（`--scan` 写的 TSV，15 列；前 14 列 = 判据 §8 的设计，末列「层」是本工具的补充）：
  层 · 域 · 原版对象 · 类型 · 原版资产名 · 我们路径 · ②状态 · ②失败项 ·
  ③P1 · ③P2 · ③P3 · ③P4 · 缺陷类 · 处置 · 证据(文件:行)
只写「有问题的行」；② 层的全量清点看控制台日志。

已知边界（⛔ 别把没覆盖的当成已核）：
  · 贴图的 GPU 格式 / sRGB 只能读解包 bundle 头，本轮不比（`判据/B108_AUD09_纹理格式.md` 已对 105 张 SDF 做过）；
  · 「prefab 节点 → 用哪张 sprite/材质」的 P3 需要对原版 prefab YAML（AssetRipper/Unity），本轮只做材质侧；
  · 同名不同图：只对「同名且**尺寸相同**」的才逐像素比（裁片尺寸不同跳过，那部分靠 P3 的 textureRect 判）；
  · 平台块**只有 `overridden: 1` 才生效**，`overridden: 0` ⇒ Unity 回落 `DefaultTexturePlatform`
    （🔴 2026-10-31 `B110_F3` 修正；构建实证见 `判据/B110_R1_资源缺陷分流.md` §2）；
  · P2 判「内容不同」前先过两档：**alpha=0 掩码**（不可见像素的 RGB 差不算）与 **gamma 复检**
    （`srgb2lin` 后等价 ⇒ 同一张图两种编码）；两档都不过才算真差异（同上，`B110_F3`）；
  · P4 的「关键字未声明」收**三处**声明形态（`#pragma multi_compile*` / `#pragma shader_feature*` /
    `Properties` 里的 `[Toggle(KEY)]`），且只判 `m_ValidKeywords`；`m_InvalidKeywords` 的死值
    单独计入信息项（同上，`B110_F3`）。
  · 🆕 2026-10-31（`B110_R5`）三处分类修正（判据 → `判据/B110_R5_资源侧放回11条.md`）：
    ① `WarpforgeVFX/ArenaOriginalPrefabs/**` = **源树**（运行期不直接加载、构建期由 ArenaBuilder/清单消费）
       ⇒ 该树里的缺陷一律**转信息项**（行保留、不进行动清单）；
    ② 「运行时接管」的悬空引用（`arena_shader_guid_map.tsv` 的 18 个 shader guid + 5 个 AnimatorController
       + `AnimFXModuleScreenShake` 的 script guid）不算缺陷（`RUNTIME_BOUND_EXTRA` / `runtime_bound_guids()`）；
    ③ P1 遇到**同名多份原版材质**时**逐份比**、取未裁定差异最少的那份；都不匹配才报「同名多源、需人工判」
       （原来只留第一份 ⇒ `SandParticle` 那种会被判错）。
  · 🆕 2026-11-01（`B111_T2` / 账 `B-75`）：**`BUILD_PLACEHOLDER`（构建期占位材质）** ——
    `WarpforgeArena1/arenas/<场>/Materials/*.mat` 是 `ArenaBuilder.GetOrCreateMaterial`/`SaveOrReuse`
    **按贴图名共享生成的占位件**（每个 ArenaBuilder 批次重写），运行期由 `ArenaOriginalMaterial.Rebuild()`
    按清单 props 重建真值 ⇒ **它们的 P1 参数漂移转信息项**（⛔ 只摘 P1，P4「材质未消费」照常判）。
    判据 → `判据/B110_R5_资源侧放回11条.md` §B-41。
  · 🆕 2026-11-01（`B111_T2B` / 账 `B-08`）：**「原版有、我们没有同名实例」这一类的四条等价/结构性判据**
    —— ① `<名>__<8 位 hex>.mat` 等价件 · ② TMP 逐文本字体材质变体 · ③ ShaderGraph 无 `.shader` 源 ·
    ④ 菜单 UI 族的自研等价件 `Resources/Art/vfx/WFUI_*`（`MENU_UI_EQUIV` 逐条 1:1）。
    命中任一 ⇒ 转信息项（⛔ 只降级**已有等价物**的，没有等价物的照旧算缺陷）。判据 → `报告/B111_T2B.md`。
"""
import argparse
import collections
import hashlib
import io
import json
import os
import re
import sys

# ---------------------------------------------------------------- 配置
ASSETS = r"d:/4/Unity/MyGame/Assets"
PROJ = r"d:/4/Unity/MyGame"
PKG_ROOTS = [os.path.join(PROJ, "Library", "PackageCache"), os.path.join(PROJ, "Packages")]
ORIG = r"d:/2/新解包资源/assets_full"
RES_DIR = r"d:/4/Unity/资料/完全复刻/资源"
R4_TSV = os.path.join(RES_DIR, "R4_Shader_VFX_清单.tsv")
R0_TSV = os.path.join(RES_DIR, "R0_资源覆盖账.tsv")
DEFAULT_OUT = os.path.join(RES_DIR, "B109_资源可用性.tsv")

COLS = ["层", "域", "原版对象", "类型", "原版资产名", "我们路径",
        "②状态", "②失败项", "③P1", "③P2", "③P3", "③P4",
        "缺陷类", "处置", "证据(文件:行)"]

ASSET_EXTS = {
    ".png", ".jpg", ".mat", ".prefab", ".asset", ".ogg", ".wav", ".shader", ".obj",
    ".anim", ".unity", ".json", ".bundle", ".webm", ".mp4", ".ttf", ".mixer",
    ".controller", ".cubemap", ".shadergraph", ".hlsl", ".cginc", ".txt", ".tsv",
    ".inputactions", ".dll", ".xml",
}
YAML_EXTS = {".mat", ".prefab", ".asset", ".unity", ".anim", ".controller", ".mixer", ".cubemap"}

IMPORTER_OK = {
    ".png": "TextureImporter", ".jpg": "TextureImporter",
    ".ogg": "AudioImporter", ".wav": "AudioImporter",
    ".shader": "ShaderImporter", ".cginc": "ShaderImporter", ".hlsl": "ShaderImporter",
    ".obj": "ModelImporter",
    ".ttf": "TrueTypeFontImporter",
    ".webm": "VideoClipImporter", ".mp4": "VideoClipImporter",
    ".prefab": "PrefabImporter", ".unity": "DefaultImporter",
    ".asset": "NativeFormatImporter", ".mat": "NativeFormatImporter",
    ".anim": "NativeFormatImporter", ".controller": "NativeFormatImporter",
    ".mixer": "NativeFormatImporter", ".cubemap": "NativeFormatImporter",
    ".json": "TextScriptImporter", ".txt": "TextScriptImporter",
    ".bundle": "DefaultImporter", ".dll": "PluginImporter",
    ".shadergraph": "ScriptedImporter", ".inputactions": "ScriptedImporter",
}

TYPE_OF = {".png": "Texture2D", ".jpg": "Texture2D", ".mat": "Material", ".prefab": "GameObject",
           ".asset": "ScriptableObject", ".ogg": "AudioClip", ".wav": "AudioClip",
           ".shader": "Shader", ".obj": "Mesh", ".anim": "AnimationClip", ".unity": "Scene",
           ".json": "TextAsset", ".bundle": "AssetBundle", ".webm": "VideoClip",
           ".mp4": "VideoClip", ".ttf": "Font", ".mixer": "AudioMixer",
           ".controller": "AnimatorController", ".cubemap": "Cubemap", ".shadergraph": "Shader",
           ".inputactions": "InputActionAsset", ".txt": "TextAsset", ".tsv": "TextAsset",
           ".dll": "Dll", ".hlsl": "ShaderInclude", ".cginc": "ShaderInclude",
           ".xml": "TextAsset", ".md": "TextAsset", ".rsp": "TextAsset"}

RE_GUID = re.compile(rb"^guid:\s*([0-9a-fA-F]{32})", re.M)
RE_IMPORTER = re.compile(rb"^([A-Za-z]+Importer):", re.M)
RE_REF = re.compile(rb"\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*(\d+)\}")
RE_NAME = re.compile(rb"^  m_Name:[ \t]*(.*)$", re.M)
RE_SHADER_REF = re.compile(rb"m_Shader:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*(\d+)\}")
RE_SHADER_DECL = re.compile(rb'^\s*Shader\s+"([^"]+)"', re.M)
RE_ISREADABLE = re.compile(rb"^  isReadable:\s*(\d+)", re.M)
RE_KEYWORDS = re.compile(rb"^  m_ValidKeywords:(.*)$", re.M)
RE_KEYWORD_ITEM = re.compile(rb"^  - (\S+)$", re.M)

BUILTIN_PREFIX = "0000000000000000"
ZERO_GUID = "0" * 32

# ---------------------------------------------------------------- 🆕 2026-10-31（B110_R5）分类修正
# 三处口径（判据全文 → `资料/完全复刻/判据/B110_R5_资源侧放回11条.md` §B-37 / §B-38 / §B-41）：
#   ① **源树** `WarpforgeVFX/ArenaOriginalPrefabs/**`：运行期**不直接加载**（构建期由 `ArenaBuilder` / 清单消费），
#      这棵树里的「有件取不到 / 依赖悬空 / 关键字未声明 / 内容不同」**不是玩家可见缺陷** ⇒ 单列成信息项，
#      ⛔ 不删行（保留可复查的证据），只是不进行动清单。
#   ② **运行时接管**的悬空引用：静态引用是坏的，但运行期有明确绑定路径 ⇒ 不补静态。
#   ③ P1 遇到**同名多份原版材质**时，先逐份比；都不匹配才报「需人工判」，⛔ 不拿第一份当唯一真值。
SOURCE_TREE_PREFIXES = ("WarpforgeVFX/ArenaOriginalPrefabs/",)
ARENA_SHADER_TSV = os.path.join(ASSETS, "WarpforgeVFX", "ArenaOriginalPrefabs", "_shared",
                                "arena_shader_guid_map.tsv")
RUNTIME_BOUND_EXTRA = {
    # AnimatorController：原版 `Cannon 1`(×2) / `Pump 1` / `Battle Arena 2` / `Dynamic Skulls` / `Battle Arena Leviathan`
    #   → `CardPresentation/Battle/ArenaSceneAnims.cs` 按 `Resources/arena_scene_anims.json` 的 `controllerPid`
    #     从 `wf_arena_anims_*.bundle` 取。
    "25a9b4140ac7ace48b389f6a9b26dbe2", "cc7c8c74587419f4f8b54c70f3f14c92",
    "e2df0361947e12c4f981bca4fb05fb4f", "e24ebf6983a5c0149ae850e564ecaa55",
    "247aee8d740eb0b42b22bf56f789592e",
    # MonoScript `AnimFXModuleScreenShake`（`Railgun BIG.anim` 的脚本绑定；原版 clip 从 bundle 取，缺绑定只会被忽略）
    "2ffcb2959b3f164290a624a862212364",
}
_runtime_bound = None


def runtime_bound_guids():
    """「运行时接管」的 guid 集合 = `arena_shader_guid_map.tsv` 第 1 列（18 行）+ `RUNTIME_BOUND_EXTRA`。
    ⚠️ **两个 Texture3D 的原版 guid（`97873f43…` / `0ca68a91…`）不在这里** —— 它们 2026-10-31 已按原版 guid
    改回 `.meta`（`B110_R5`），应当**直接解析**，⛔ 别拿这张表去掩盖。"""
    global _runtime_bound
    if _runtime_bound is not None:
        return _runtime_bound
    s = set(RUNTIME_BOUND_EXTRA)
    try:
        lines = io.open(ARENA_SHADER_TSV, encoding="utf-8").read().splitlines()
        for line in lines[1:]:
            c = line.split("\t")
            if c and len(c[0].strip()) == 32:
                s.add(c[0].strip().lower())
    except Exception:
        pass
    _runtime_bound = s
    return s


def in_source_tree(relpath):
    return relpath.startswith(SOURCE_TREE_PREFIXES)


# 🆕 2026-11-01（`B111_T2` / 账 `B-75`）：**BUILD_PLACEHOLDER = 构建期占位 `.mat`**。
#   `WarpforgeArena1/arenas/<场>/Materials/*.mat` 不是手写资产，是
#   `ArenaBuilder.GetOrCreateMaterial` → `SaveOrReuse` **按【贴图名】共享**生成的占位件
#   （`WarpforgeArena1/Editor/ArenaBuilder.cs:5495` 起那一整段）。三个事实决定它不该按 P1 判：
#     ① **生成物**：每个 `ArenaBuilder` 批次都会把它们整批重写（`SaveOrReuse`，guid 不变）；
#     ② **运行期不直接用它**：网格上挂的是 `ArenaOriginalMaterial`，`Rebuild()` 按清单 props
#        重建渲染状态（`ArenaBuilder.cs` 那两处长注释明写「占位 `.mat` 只是 `Rebuild()` 失败时的退路」）；
#     ③ 逐属性实测：占位件对应的 manifest props **53 vs 原版 53**、**47 vs 原版 47，差 0**。
#   ⇒ 占位件的「P1 参数值 vs 原版 `m_SavedProperties`」**不是缺陷**，单列成信息项。
#   判据全文 → `资料/完全复刻/判据/B110_R5_资源侧放回11条.md` §B-41（`B-41` 三条里的前两条）。
#   ⚠️ **只摘 P1 参数漂移**：P4「材质未消费（属性/关键字未声明）」照常判 —— 那是 shader 绑定的问题，
#      占位件躲不掉（虽然它们多半走的是 `URP/Unlit` 包内 shader，本来就不进 P4）。
BUILD_PLACEHOLDER_RE = re.compile(r"^WarpforgeArena1/arenas/[^/]+/Materials/[^/]+\.mat$")


def in_build_placeholder(relpath):
    return BUILD_PLACEHOLDER_RE.match(relpath) is not None


# ---------------------------------------------------------------- 🆕 2026-11-01（`B111_T2B` / 账 `B-08`）
# 「原版材质我们没有同名实例」这一类的**四条等价/结构性判据**。改前它把 216 条全算缺陷，
# 其中 110 条是**假阳性或结构性不可能**（逐条核过，见 `报告/B111_T2B.md` §①）：
#   ① `<名>__<8 位 hex>.mat` —— `EffectExporter` 的去重命名，**等价件就在工程里**（47 条）。
#   ② TMP 逐文本字体材质变体（`TextMeshPro/Distance Field` / `Mobile/Distance Field` / `TextMeshPro/Sprite`）——
#      我们统一走 `Label`（TMP 顶点色 + `Asar-Regular SDF`），**不需要这些 per-text 变体**（46 条）。
#   ③ ShaderGraph 生成体（`Everguild/Cards/Shader Graphs/Booster Pack`）—— **没有 `.shader` 源**，
#      `.mat` 结构性建不出来；原版那 16 张在 `wf_boosters_extra.bundle` 包内可解、0 悬空（46 → 16 条）。
#   ④ 菜单 UI 族的**自研等价件** `Resources/Art/vfx/WFUI_*`（见下面 `MENU_UI_EQUIV` 逐条 1:1，13 条）。
# ⚠️ 只对**已有等价物**的降级；**没有等价物的照旧算缺陷**（⛔ 别把这一类整体抹掉 —— 铁律 11）。
MENU_UI_EQUIV_DIR = "CardPresentation/Resources/Art/vfx/"
MENU_UI_EQUIV = {
    # 原版材质名 → 我们的自研等价件（`Resources/Art/vfx/<值>.mat`）
    "Border Chest": "WFUI_GlowSdf_BorderChest",
    "Border Chest Collect": "WFUI_GlowSdf_BorderChestCollect",
    "UI Highlight Alliance trophy": "WFUI_GlowSdf_AllianceTrophy",
    "UI Highlight Red SDF": "WFUI_GlowSdf_HighlightRed",
    "UI Highlight Smooth Red SDF": "WFUI_GlowSdf_HighlightSmoothRed",
    "Everguild Campaing Premium node": "WFUI_Pulsating_Campaign",
    "Forge UI Light": "WFUI_Pulsating_Forge",
    "Forge Candle": "WFUI_Distort_ForgeCandle",
    "UI Distort in Atlas Small objects": "WFUI_Distort_AtlasSmall",
    "UI Clock": "WFUI_Clock",
    "Rank Change": "WFUI_DivisionChange",
    "UI Greyscale Add": "WFUI_GreyscaleAdd",
    "UI Scan Lines Alpha Mask": "WFUI_ScanLines",
}
TMP_FONT_SHADERS = ("TextMeshPro/Distance Field", "TextMeshPro/Mobile/Distance Field",
                    "TextMeshPro/Sprite")
SHADERGRAPH_NO_SOURCE = ("Everguild/Cards/Shader Graphs/Booster Pack",)


def hash_equiv_map(all_mat_names):
    """`<名>__<8 位 hex>` → 名字前缀。返回 `{原名: 等价件名}`（取第一份）。"""
    out = {}
    hexs = set("0123456789abcdef")
    for m in all_mat_names:
        i = m.rfind("__")
        if i <= 0:
            continue
        suf = m[i + 2:]
        if len(suf) == 8 and all(c in hexs for c in suf):
            out.setdefault(m[:i], m)
    return out

# 判据 `B109_R9 §6` 的 13 条原版菜单 UI 材质动画 shader（P4 的固定标尺，⛔ 别改名单）
R4_07 = {
    "Everguild/UI/Division Change", "Everguild/UI/Greyscale Add",
    "Everguild/FX/FX Shine For Animation Normal", "Everguild/UI/Pulsating",
    "Everguild/UI/UI Glow SDF Noise", "Everguild/UI/UI Distort", "Everguild/UI/Additive",
    "Shader Graphs/UI Container Main Menu", "Everguild/UI/UI Clock",
    "Everguild/UI/UI Border Highlight Pulse Alpha", "Everguild/UI/Scan Lines Alpha Mask",
    "Everguild/FX/Genestealers Flood", "Everguild/FX/Two passes blur",
}

# 已裁定「我们与原版序列化值不同、但这是有意为之」的属性：原版那侧是死值。
# 判据 → `Unity/资料/任务线/特效线/任务总表.md:164`（2026-10-23 翻案）。
RULED_DIFFS = {
    "_SrcBlendAlpha": "原版 `rtSeparateBlend=False` ⇒ 原版那侧是死值；2026-10-23 裁定取 5",
    "_DstBlendAlpha": "同上（原版 alpha 侧不寻址）",
}

PIXEL_APIS = ("GetPixels(", "GetPixels32(", "SetPixels(", "SetPixels32(",
              "GetPixelData(", "SetPixelData(", "EncodeToPNG(", "GetRawTextureData(")


def rb(p):
    with io.open(p, "rb") as f:
        return f.read()


def txt(p):
    return rb(p).decode("utf-8", "replace")


def rel(p, base):
    return os.path.relpath(p, base).replace(os.sep, "/")


def image_size(p):
    """按内容判图（⛔ 不按扩展名：本工程实测有 .png 里装 JPEG 的）。"""
    try:
        b = rb(p)[:32]
    except Exception:
        return None, None
    if b[:8] == b"\x89PNG\r\n\x1a\n" and len(b) >= 24:
        return "png", (int.from_bytes(b[16:20], "big"), int.from_bytes(b[20:24], "big"))
    if b[:3] == b"\xff\xd8\xff":
        try:
            from PIL import Image
            with Image.open(p) as im:
                return "jpg", im.size
        except Exception:
            return "jpg", None
    if b[:4] == b"RIFF" and b[8:12] == b"WEBP":
        return "webp", None
    if b[:3] == b"GIF":
        return "gif", (int.from_bytes(b[6:8], "little"), int.from_bytes(b[8:10], "little"))
    if b[:2] == b"BM":
        return "bmp", (int.from_bytes(b[18:22], "little"), int.from_bytes(b[22:26], "little"))
    if b[:4] == b"OggS":
        return "ogg", None
    if b[:4] == b"RIFF" and b[8:12] == b"WAVE":
        return "wav", None
    if b[:4] == b"fLaC":
        return "flac", None
    return None, None


def png_size(p):
    kind, size = image_size(p)
    return size if kind == "png" else None


def domain_of(r):
    pairs = [
        ("CardPresentation/Resources/Art/cards/clan", "卡框"),
        ("CardPresentation/Resources/Art/cards", "卡面"),
        ("CardPresentation/Resources/Art/cardbacks", "卡背"),
        ("CardPresentation/Resources/Art/avatars", "头像"),
        ("CardPresentation/Resources/Art/traits", "关键词图标"),
        ("CardPresentation/Resources/Art/ui", "菜单UI"),
        ("CardPresentation/Resources/Art/vfx", "特效材质"),
        ("CardPresentation/Resources/Art/audio", "音频"),
        ("CardPresentation/Resources/Art/videos", "视频"),
        ("CardPresentation/Resources/Art/altarts", "异画"),
        ("CardPresentation/Resources/Art/boosterpack", "卡包"),
        ("CardPresentation/Resources/Art", "其它美术"),
        ("CardPresentation/Resources/ArenaPrefabs", "战场预制体"),
        ("CardPresentation/Resources", "其它Resources"),
        ("WarpforgeVFX", "特效"),
        ("WarpforgeArena1", "战场"),
        ("WarpforgeBooster", "卡包"),
        ("RuleEngine", "规则数据"),
        ("TextMesh Pro", "TMP"),
        ("StreamingAssets", "流式"),
        ("Plugins", "插件"),
    ]
    for pre, d in pairs:
        if r.startswith(pre):
            return d
    return "其它"


def parse_platform_settings(b):
    """{buildTarget: {maxTextureSize, format, textureCompression, overridden}} —— Unity 生效的是平台块。

    ⚠️ 关键：`overridden: 0` 的平台块**不生效**，Unity 回落到 `DefaultTexturePlatform`。
    （只读平台块的 maxTextureSize 会误报降采样 —— `B108_AUD09_meta.md` §二 记过同族坑。）
    """
    out = {}
    if not b:
        return out
    lines = b.split(b"\n")
    i = None
    for k, ln in enumerate(lines):
        if ln.strip() == b"platformSettings:":
            i = k
            break
    if i is None:
        return out
    cur = None
    for ln in lines[i + 1:]:
        if not ln.strip():
            continue
        if ln.startswith(b"  ") and not ln.startswith(b"   ") and not ln.startswith(b"  - "):
            break  # 下一个同级键
        s = ln.strip()
        if s.startswith(b"- serializedVersion"):
            cur = {}
            continue
        if s.startswith(b"buildTarget:"):
            cur = cur if cur is not None else {}
            cur["target"] = s.split(b":", 1)[1].strip().decode()
            out[cur["target"]] = cur
            continue
        m = re.match(rb"(maxTextureSize|format|textureCompression|resizeAlgorithm|overridden):\s*(\S+)", s)
        if m and cur is not None:
            try:
                cur[m.group(1).decode()] = int(m.group(2))
            except ValueError:
                pass
    return out


def effective_max_size(ps, top_level_max):
    """本机目标 = Standalone；**只有 `overridden: 1` 的平台块才生效**，否则 Unity 回落 `DefaultTexturePlatform`。

    🔴 2026-10-31 `B110_F3` 修正（旧实现与 `parse_platform_settings` 的 docstring 自相矛盾，是
    B109 那 9 条「尺寸格式错」全是假阳性的根因）：
      旧写法「有 Standalone 块就按它算」**被构建产物证伪** ——
      `_tmp_view/player/WarpforgePlayer_Data/resources.assets` 里那 9 张全是源尺寸（8192/4096 =
      `DefaultTexturePlatform` 的值），而 Standalone 块写着 2048 ⇒ 那个块并没有参与导入。
    ⇒ `overridden: 0` = 「未覆盖该平台」⇒ 该块的一组值不生效。
    ⚠️ `DefaultTexturePlatform` 块的 `overridden` 恒为 0，但**它就是默认值**本身，照取不误。
    """
    blk = ps.get("Standalone")
    if blk and blk.get("overridden") == 1 and blk.get("maxTextureSize"):
        return blk["maxTextureSize"], "Standalone"
    d = ps.get("DefaultTexturePlatform") or {}
    if d.get("maxTextureSize"):
        return d["maxTextureSize"], "DefaultTexturePlatform"
    return top_level_max, "top-level"


def _mat_keyword_section(text, key):
    """读 .mat 的 `m_ValidKeywords:` / `m_InvalidKeywords:` 一节。

    ⚠️ 2026-10-31 `B110_F3`：旧版从 `m_ValidKeywords:` 往后把**两节一起**扫（`text[m.end():]`
    后面所有 `  - ` 行），valid/invalid 从来没分开过 ⇒ 无法区分「Unity 说 shader 认识它」与
    「Unity 说它是死值」。现在按节截断。
    """
    m = re.search(r"^  %s:(.*)$" % re.escape(key), text, re.M)
    if not m:
        return []
    if m.group(1).strip() == "[]":
        return []
    out = []
    for ln in text[m.end():].split("\n"):
        if ln.startswith("  - "):
            out.append(ln[4:].strip())
        elif not ln.strip():
            continue
        elif ln.startswith("  ") and not ln.startswith("    "):
            break                   # 下一个二级键（如 m_InvalidKeywords / m_LightmapFlags）
    return out


def parse_mat_props(b):
    """返回 (colors{name:(r,g,b,a)}, floats{name:v}, texenvs[names],
           kws_valid[names], kws_invalid[names])

    形状以本工程 .mat 源码为准（Unity 6）：
        m_TexEnvs:
        - _MainTex:
            m_Texture: {fileID: ...}
        m_Floats:
        - _Cull: 2
        m_Colors:
        - _Color: {r: 4, g: 4, b: 4, a: 1}
    """
    colors, floats, texenvs, kws = {}, {}, [], []
    text = b.decode("utf-8", "replace")
    i = text.find("  m_SavedProperties:")
    if i >= 0:
        seg = text[i:].split("\n")
        state = None
        for ln in seg[1:]:
            if not ln.startswith("    "):
                if ln.startswith("  ") and ln.strip():
                    state = None            # 回到二级键（如 m_BuildTextureStacks）
                continue
            s = ln[4:]
            if s.startswith("m_TexEnvs:"):
                state = "tex"
                continue
            if s.startswith("m_Floats:"):
                state = "float"
                continue
            if s.startswith("m_Colors:"):
                state = "color"
                continue
            if s.startswith("m_Ints:"):
                state = "int"
                continue
            if not s.startswith("- "):
                continue
            item = s[2:]
            if state == "tex":
                texenvs.append(item.split(":", 1)[0].strip())
            elif state == "float":
                k, _, v = item.partition(":")
                try:
                    floats[k.strip()] = round(float(v.strip()), 6)
                except ValueError:
                    pass
            elif state == "color":
                k, _, v = item.partition(":")
                m = re.search(r"r:\s*([-\d.eE+]+),\s*g:\s*([-\d.eE+]+),\s*b:\s*([-\d.eE+]+),\s*a:\s*([-\d.eE+]+)", v)
                if m:
                    colors[k.strip()] = tuple(round(float(m.group(x)), 4) for x in (1, 2, 3, 4))
    return colors, floats, texenvs, _mat_keyword_section(text, "m_ValidKeywords"), \
        _mat_keyword_section(text, "m_InvalidKeywords")


RE_SHADER_PROPS = re.compile(rb"^[ \t]{1,12}(?:\[[^\]]*\]\s*)*([A-Za-z_]\w*)\s*\(", re.M)
# 🔴 2026-10-31 `B110_F3`：关键字声明有**三处**，旧版只认第 ① 处 ⇒ 把「我们明明声明了」的关键字
#   整批判成「未声明」（`B110_R1 §7` / 顺手发现 #3）。现在全收：
#   ① `#pragma multi_compile*` / `#pragma shader_feature*`
#      （含 `_local` / `_fragment` / `_vertex` / `_local_fragment` 等全组合 —— 旧正则只列了其中几种）
#   ② `Properties` 里的 `[Toggle(KEY)]` / `[ToggleOff(KEY)]` / `[ToggleUI(KEY)]` 装饰器
#      （Unity 会把括号里那个 `_KEY` 注册成 shader 关键字）
RE_PRAGMA = re.compile(rb"#pragma\s+(?:multi_compile\w*|shader_feature\w*)\s+([^\n]+)")
RE_TOGGLE_KW = re.compile(rb"\[(?:Toggle|ToggleOff|ToggleUI)\s*\(\s*(_\w+)\s*\)\s*\]")


def parse_shader(path, cache):
    if path in cache:
        return cache[path]
    b = rb(path)
    blob = b
    # 跟一层 #include（TMP 的属性写在 TMPro_Properties.cginc 里，只看 .shader 正文会漏）
    for m in re.finditer(rb'#include\s+"([^"]+)"', b):
        inc = os.path.join(os.path.dirname(path), m.group(1).decode("utf-8", "replace"))
        if os.path.isfile(inc):
            try:
                blob += b"\n" + rb(inc)
            except Exception:
                pass
    props = set(m.group(1).decode() for m in RE_SHADER_PROPS.finditer(blob))
    # HLSL 里直接声明的 uniform 也算「被消费」（URP 里大量状态量不走 Properties 块）
    for m in re.finditer(rb"(?:uniform[ \t]+)?(?:float|half|fixed|int|uint|float2|float3|float4|half2|half3|half4"
                         rb"|fixed2|fixed3|fixed4|sampler2D|sampler3D|samplerCUBE|sampler2D_float|Texture2D|Texture3D|TextureCube)"
                         rb"[ \t]+(_\w+)\s*[;=]", blob):
        props.add(m.group(1).decode())
    for m in re.finditer(rb"TEXTURE2D\s*\(\s*(_\w+)\s*\)", blob):
        props.add(m.group(1).decode())
    kws = set()
    for m in RE_PRAGMA.finditer(blob):
        for tok in m.group(1).split():
            t = tok.decode("utf-8", "replace")
            if t == "_":            # `multi_compile _ _FOO` 里的空位不是关键字
                continue
            if re.match(r"^[A-Za-z_][A-Za-z0-9_]*$", t):
                kws.add(t)
    for m in RE_TOGGLE_KW.finditer(blob):
        kws.add(m.group(1).decode("utf-8", "replace"))
    decl = RE_SHADER_DECL.search(b)
    name = decl.group(1).decode("utf-8", "replace") if decl else ""
    cache[path] = (props, kws, name)
    return cache[path]


def pixel_md5(path):
    from PIL import Image
    with Image.open(path) as im:
        im = im.convert("RGBA")
        return hashlib.md5(im.tobytes()).hexdigest(), im.size


def _srgb_to_linear(v):
    """sRGB(0..255) → 线性(0..255)。用于「我们存 sRGB、原版存 linear」这条编码差的复检。"""
    import numpy as np
    c = np.asarray(v, dtype=np.float32) / np.float32(255.0)
    return np.where(c <= 0.04045, c / np.float32(12.92),
                    ((c + np.float32(0.055)) / np.float32(1.055)) ** np.float32(2.4)) * np.float32(255.0)


def pixel_diff(p1, p2, max_px=4000000):
    """返回 dict: all / rgb / rgb_mask / gamma / mask_frac。

    🔴 2026-10-31 `B110_F3` 修两处判据（旧版只回 `(mean_all, mean_rgb)`，把两类「不是内容差」
    的图判成了缺陷）：
      ① **只在「至少一侧 alpha>0」的可见像素上比 RGB**。我们的导出把透明区的 RGB 写成 0、原版保留
         白色/原色；alpha=0 的像素不可见（alpha blend 与 additive 都乘 alpha）⇒ 那部分 RGB 差
         不算内容差（`B110_R1 §4-a`：18 条假阳性就是这么来的）。
      ② **gamma 复检**：我们存 sRGB、原版存 linear ⇒ `srgb2lin(我们的) ≈ 原版`，导入
         `sRGBTexture: 1` 后 GPU 取到的就是同一组值（21 条假阳性）。正反两个方向取最小值，
         免得把方向押反。⚠️ 法线图的排除理由也是**这个**（不是旧文档写的「压缩通道重排」）。
    """
    import numpy as np
    from PIL import Image
    with Image.open(p1) as a, Image.open(p2) as b:
        a = a.convert("RGBA")
        b = b.convert("RGBA")
        if a.size != b.size:
            return None
        if a.size[0] * a.size[1] > max_px:
            a = a.resize((256, 256))
            b = b.resize((256, 256))
        x = np.asarray(a, dtype=np.float32)
        y = np.asarray(b, dtype=np.float32)
    d = np.abs(x - y)
    mask = (x[:, :, 3] > 0) | (y[:, :, 3] > 0)
    n = int(mask.sum())
    if n == 0:                      # 整图全透明：退回全像素（否则除零）
        mask = np.ones(mask.shape, dtype=bool)
        n = int(mask.size)
    g1 = np.abs(_srgb_to_linear(x[:, :, :3]) - y[:, :, :3])[mask].mean()
    g2 = np.abs(x[:, :, :3] - _srgb_to_linear(y[:, :, :3]))[mask].mean()
    return {
        "all": float(d.mean()),
        "rgb": float(d[:, :, :3].mean()),
        "rgb_mask": float(d[:, :, :3][mask].mean()),
        "gamma": float(min(g1, g2)),
        "mask_frac": n / float(mask.size),
    }


def mat_name(b):
    """取第一个非空的 `m_Name:`（.mat 前面常先有一段 AssetVersion 的 MonoBehaviour，m_Name 为空）。"""
    for m in RE_NAME.finditer(b):
        s = m.group(1).decode("utf-8", "replace").strip()
        if len(s) >= 2 and s[0] == s[-1] and s[0] in "'\"":
            s = s[1:-1]
        if s:
            return s
    return ""


# ---------------------------------------------------------------- 索引
class Ctx(object):
    def __init__(self, log):
        self.log = log
        self.files = []            # (abs, rel, ext, size)
        self.guid2path = {}
        self.guid_dups = set()
        self.meta_missing = []
        self.meta_orphan = []
        self.resources_roots = []
        self.load_sites = []
        self.load_literals = set()
        self.load_prefixes = set()
        self.pixel_src = []        # (rel, text)
        self.orig_tex = collections.defaultdict(list)
        self.orig_mat = {}
        # 🆕 2026-10-31（B110_R5）：**同名多份**原版材质（`d:/2` 里真的存在，例 `SandParticle` 两份值不同）
        #   原版对象 → [(json, shader_pid), …]。P1 逐份比，⛔ 不拿第一份当唯一真值。
        self.orig_mat_all = collections.defaultdict(list)
        self.orig_sprite = {}
        self.orig_audio = {}
        self.shader_pid2name = {}
        self.shader_orig_refs = {}
        self.shader_cache = {}
        self.mat_cache = {}
        self.code_all = ""
        self.code_runtime = ""

    def load(self):
        A = ASSETS
        for dp, dn, fn in os.walk(A):
            for f in fn:
                p = os.path.join(dp, f)
                r = rel(p, A)
                if f.endswith(".meta"):
                    b = rb(p)
                    m = RE_GUID.search(b)
                    if m:
                        g = m.group(1).decode().lower()
                        if g in self.guid2path:
                            self.guid_dups.add(g)
                        else:
                            self.guid2path[g] = p[:-5]
                    else:
                        self.meta_orphan.append(r)
                    if not os.path.exists(p[:-5]):
                        self.meta_orphan.append(r)
                    continue
                ext = os.path.splitext(f)[1].lower()
                try:
                    sz = os.path.getsize(p)
                except OSError:
                    sz = -1
                self.files.append((p, r, ext, sz))
                if not os.path.exists(p + ".meta"):
                    self.meta_missing.append(r)
        for base in PKG_ROOTS:
            if not os.path.isdir(base):
                continue
            for dp, dn, fn in os.walk(base):
                for f in fn:
                    if not f.endswith(".meta"):
                        continue
                    p = os.path.join(dp, f)
                    try:
                        m = RE_GUID.search(rb(p))
                    except Exception:
                        continue
                    if m:
                        self.guid2path.setdefault(m.group(1).decode().lower(), p[:-5])
        for dp, dn, fn in os.walk(A):
            if "Resources" in dn:
                self.resources_roots.append(os.path.join(dp, "Resources"))
        self.log("① 索引：工程资产 %d · guid %d（重复 %d）· 缺 meta %d · 孤儿 meta %d · Resources 根 %d"
                 % (len(self.files), len(self.guid2path), len(self.guid_dups),
                    len(self.meta_missing), len(self.meta_orphan), len(self.resources_roots)))
        if self.meta_missing:
            self.log("   缺 meta 清单（前 10）：" + " · ".join(self.meta_missing[:10]))
        self.scan_code()
        self.scan_original()
        self.build_reverse()

    # ---- 代码侧
    def scan_code(self):
        A = ASSETS
        srcs, consts_file, allconst = {}, {}, collections.defaultdict(set)
        for p, r, ext, sz in self.files:
            if ext != ".cs":
                continue
            s = txt(p)
            srcs[p] = s
            d = {}
            for m in re.finditer(r'(?:const|static\s+readonly)\s+string\s+(\w+)\s*=\s*"((?:[^"\\]|\\.)*)"', s):
                d[m.group(1)] = m.group(2)
            consts_file[p] = d
            for k, v in d.items():
                allconst[k].add(v)
        uniq = {k: next(iter(v)) for k, v in allconst.items() if len(v) == 1}
        self.code_all = "\n".join(srcs.values())
        full_qual = {}
        for p, d in consts_file.items():
            cls = os.path.splitext(os.path.basename(p))[0]
            for k, v in d.items():
                full_qual[cls + "." + k] = v

        def lookup(tok, cf):
            short = tok.split(".")[-1]
            if tok in full_qual:
                return full_qual[tok]
            if short in consts_file.get(cf, {}):
                return consts_file[cf][short]
            if short in uniq:
                return uniq[short]
            return None

        def strip_comments(s):
            out = []
            for line in s.split("\n"):
                q = False
                i = 0
                while i < len(line):
                    ch = line[i]
                    if ch == '"':
                        q = not q
                    elif ch == "/" and not q and i + 1 < len(line) and line[i + 1] == "/":
                        line = line[:i]
                        break
                    i += 1
                out.append(line)
            return "\n".join(out)

        def split_plus(a):
            parts, depth, cur, q, esc = [], 0, "", False, False
            for ch in a:
                if q:
                    cur += ch
                    if esc:
                        esc = False
                    elif ch == "\\":
                        esc = True
                    elif ch == '"':
                        q = False
                    continue
                if ch == '"':
                    q = True
                    cur += ch
                    continue
                if ch == "(":
                    depth += 1
                elif ch == ")":
                    depth -= 1
                if ch == "+" and depth == 0:
                    parts.append(cur.strip())
                    cur = ""
                    continue
                cur += ch
            if cur.strip():
                parts.append(cur.strip())
            return parts

        def resolve(arg, cf):
            a = arg.strip()
            m = re.fullmatch(r'"((?:[^"\\]|\\.)*)"', a)
            if m:
                return "literal", m.group(1)
            val, unknown = "", False
            for p in split_plus(a):
                mm = re.fullmatch(r'"((?:[^"\\]|\\.)*)"', p)
                if mm:
                    val += mm.group(1)
                    continue
                c = lookup(p, cf)
                if c is not None:
                    val += c
                    continue
                lits = re.findall(r'"((?:[^"\\]|\\.)*)"', p)
                if lits:
                    val += lits[0]
                unknown = True
            if not unknown and val:
                return "literal", val
            if val:
                return "prefix", val
            return "dynamic", None

        runtime_clean = []
        for p, s in srcs.items():
            r = rel(p, A)
            is_editor = "/Editor/" in p.replace(os.sep, "/")
            clean = strip_comments(s)
            for m in re.finditer(r"Resources\.(Load|LoadAll)\s*(?:<[^>]*>)?\s*\(", clean):
                i = m.end()
                depth, j, q, esc = 1, i, False, False
                while j < len(clean) and depth > 0:
                    ch = clean[j]
                    if q:
                        if esc:
                            esc = False
                        elif ch == "\\":
                            esc = True
                        elif ch == '"':
                            q = False
                    else:
                        if ch == '"':
                            q = True
                        elif ch == "(":
                            depth += 1
                        elif ch == ")":
                            depth -= 1
                    j += 1
                arg = clean[i:j - 1].strip()
                ln = clean.count("\n", 0, m.start()) + 1
                kind, val = resolve(arg, p)
                self.load_sites.append({"file": r, "line": ln, "arg": arg, "kind": kind,
                                        "value": val, "editor": is_editor,
                                        "all": m.group(1) == "LoadAll"})
                if not is_editor and kind == "literal":
                    self.load_literals.add(val)
                elif not is_editor and kind == "prefix":
                    self.load_prefixes.add(val)
            if not is_editor and any(api in s for api in PIXEL_APIS):
                self.pixel_src.append((r, s))
            if not is_editor:
                runtime_clean.append(clean)
        self.code_runtime = "\n".join(runtime_clean)
        c = collections.Counter(x["kind"] for x in self.load_sites)
        self.log("代码面：Resources.Load 调用点 %d（literal %d / prefix %d / dynamic %d）· 运行期读像素文件 %d"
                 % (len(self.load_sites), c["literal"], c["prefix"], c["dynamic"], len(self.pixel_src)))

    def scan_original(self):
        if not os.path.isdir(ORIG):
            self.log("⚠️ 原版镜像 %s 不在 ⇒ ③ 层全部 UNKNOWN" % ORIG)
            return
        for d in sorted(os.listdir(ORIG)):
            base = os.path.join(ORIG, d)
            if not os.path.isdir(base):
                continue
            td = os.path.join(base, "Texture2D")
            if os.path.isdir(td):
                for f in os.listdir(td):
                    if f.lower().endswith(".png"):
                        self.orig_tex[os.path.splitext(f)[0]].append(os.path.join(td, f))
            md = os.path.join(base, "Material")
            if os.path.isdir(md):
                for f in os.listdir(md):
                    if not f.endswith(".json"):
                        continue
                    try:
                        o = json.load(io.open(os.path.join(md, f), encoding="utf-8"))
                    except Exception:
                        continue
                    nm = o.get("m_Name")
                    if nm:
                        sh = (o.get("m_Shader") or {}).get("m_PathID")
                        self.orig_mat_all[nm].append((os.path.join(md, f), str(sh) if sh else ""))
                        if nm not in self.orig_mat:
                            self.orig_mat[nm] = (os.path.join(md, f), str(sh) if sh else "")
            sd = os.path.join(base, "Sprite")
            if os.path.isdir(sd):
                for f in os.listdir(sd):
                    if f.endswith(".json") and f[:-5] not in self.orig_sprite:
                        self.orig_sprite[f[:-5]] = os.path.join(sd, f)
            ad = os.path.join(base, "AudioClip")
            if os.path.isdir(ad):
                for f in os.listdir(ad):
                    self.orig_audio.setdefault(os.path.splitext(f)[0], os.path.join(ad, f))
        if os.path.isfile(R4_TSV):
            L = io.open(R4_TSV, "rb").read().decode("utf-8-sig").split("\n")
            h = L[0].split("\t")
            ip, iname, icnt, itype = h.index("PathID"), h.index("真实名字"), h.index("引用数"), h.index("类型")
            for l in L[1:]:
                if not l.strip():
                    continue
                f = l.split("\t")
                if len(f) <= icnt or f[itype] != "Shader":
                    continue
                self.shader_pid2name[f[ip]] = f[iname]
                try:
                    self.shader_orig_refs[f[iname]] = int(f[icnt])
                except ValueError:
                    pass
        self.log("原版索引：Texture2D %d · Material %d · Sprite %d · AudioClip %d · Shader %d"
                 % (len(self.orig_tex), len(self.orig_mat), len(self.orig_sprite),
                    len(self.orig_audio), len(self.shader_pid2name)))

    def build_reverse(self):
        self.reverse = collections.defaultdict(set)
        for p, r, ext, sz in self.files:
            if ext not in YAML_EXTS:
                continue
            b = rb(p)
            for m in RE_REF.finditer(b):
                g = m.group(2).decode().lower()
                if g == ZERO_GUID or g.startswith(BUILTIN_PREFIX):
                    continue
                self.reverse[g].add(r)

    # ---- 工具
    def meta_of(self, p):
        mp = p + ".meta"
        return rb(mp) if os.path.exists(mp) else None

    def res_json_text(self):
        """Resources 下所有 .json 的正文（数据表按名引用资产的证据面）。"""
        out = []
        for p, r, ext, sz in self.files:
            if ext == ".json" and self.resources_key(p) is not None:
                try:
                    out.append(txt(p))
                except Exception:
                    pass
        return "\n".join(out)

    def guid_of(self, p):
        b = self.meta_of(p)
        if not b:
            return None
        m = RE_GUID.search(b)
        return m.group(1).decode().lower() if m else None

    def resources_key(self, p):
        for R in self.resources_roots:
            if p.startswith(R + os.sep):
                return rel(p, R).rsplit(".", 1)[0]
        return None


# ---------------------------------------------------------------- 主流程
def run(args):
    logs = []

    def log(s):
        logs.append(s)
        print(s)
        sys.stdout.flush()

    ctx = Ctx(log)
    ctx.load()

    rows = []
    stats = collections.Counter()
    cls_counts = collections.Counter()
    cls_examples = collections.defaultdict(list)

    # ---- 可加载闭包
    seed_guids = set()
    for p, r, ext, sz in ctx.files:
        if ctx.resources_key(p) is not None or r.startswith("StreamingAssets/") or ext == ".unity":
            g = ctx.guid_of(p)
            if g:
                seed_guids.add(g)
    reach = set(seed_guids)
    for _ in range(3):
        nxt = set()
        for p, r, ext, sz in ctx.files:
            if ext not in YAML_EXTS:
                continue
            g = ctx.guid_of(p)
            if not g or g not in reach:
                continue
            for m in RE_REF.finditer(rb(p)):
                gg = m.group(2).decode().lower()
                if gg == ZERO_GUID or gg.startswith(BUILTIN_PREFIX):
                    continue
                nxt.add(gg)
        add = nxt - reach
        if not add:
            break
        reach |= add
    log("② 可加载闭包：种子 %d → 闭包 %d 个 guid" % (len(seed_guids), len(reach)))

    pixel_text = "\n".join(s for _, s in ctx.pixel_src)
    code_text = ctx.code_all
    res_json_text = ctx.res_json_text()
    misc_counts = collections.Counter()

    def tokset(s):
        """正文 → 可 O(1) 查的串集合（引号内字面量 + 非空白 token）。"""
        t = set(re.findall(r'"([^"\n]{3,160})"', s))
        t |= set(re.findall(r"[^\s\"',;(){}\[\]]{4,160}", s))
        return t

    code_toks = tokset(code_text)
    code_lit = set(re.findall(r'"([^"\n]{3,160})"', ctx.code_runtime))
    data_toks = tokset(res_json_text)
    dir_hit_memo = {}
    # Resources 下已存在的「名字」集合：同名同图/异图都可能 —— 名字可加载性按名字判
    res_stems = set()
    for p, r, ext, sz in ctx.files:
        if ctx.resources_key(p) is not None:
            res_stems.add(os.path.splitext(os.path.basename(p))[0])
    # 「读像素通道」真正摸到的贴图名 = 每个 GetPixels*/SetPixelData* 调用点周围 600 字符里的字面量
    pixel_near = set()
    for _, s in ctx.pixel_src:
        for m in re.finditer(r"GetPixels|GetPixelData|SetPixelData|SetPixels|EncodeToPNG|GetRawTextureData", s):
            win = s[max(0, m.start() - 600):m.start() + 600]
            for lit in re.findall(r'"([^"\n]{2,160})"', win):
                pixel_near.add(lit)
    log("② 命名面：Resources 名字 %d · 读像素邻域字面量 %d" % (len(res_stems), len(pixel_near)))

    def is_loadable(p, r, ext):
        key = ctx.resources_key(p)
        if key is not None:
            return "RESOURCES", key
        if r.startswith("StreamingAssets/"):
            return "STREAMING", r[len("StreamingAssets/"):]
        if ext == ".unity":
            return "SCENE", ""
        g = ctx.guid_of(p)
        if g and g in reach:
            return "VIA_REF", ""
        stem = os.path.splitext(os.path.basename(p))[0]
        for k in ctx.load_literals:
            if k.rstrip("/").split("/")[-1] == stem:
                return "LOAD_LITERAL", k
        if stem in res_stems:
            return "NAME_IN_RESOURCES", ""
        return None, ""

    # ================================================ ② 层
    if args.only in ("2", "all"):
        log("—— ② 层：逐工程资产（%d）——" % len([1 for f in ctx.files if f[2] in ASSET_EXTS]))
        st_counts = collections.Counter()
        for p, r, ext, sz in ctx.files:
            if ext not in ASSET_EXTS:
                continue
            dom = domain_of(r)
            typ = TYPE_OF.get(ext, ext)
            stem = os.path.splitext(os.path.basename(p))[0]
            orig_name = ""
            if ext in (".png", ".jpg") and stem in ctx.orig_tex:
                orig_name = stem
            elif ext == ".mat" and stem in ctx.orig_mat:
                orig_name = stem
            elif ext in (".ogg", ".wav") and stem in ctx.orig_audio:
                orig_name = stem
            fails = []
            meta_b = ctx.meta_of(p)
            guid = None
            if meta_b is None:
                fails.append("META_MISSING")
            else:
                m = RE_GUID.search(meta_b)
                if not m:
                    fails.append("GUID_INVALID")
                else:
                    guid = m.group(1).decode().lower()
                    if guid in ctx.guid_dups:
                        fails.append("GUID_DUP")
            exp = IMPORTER_OK.get(ext)
            if meta_b is not None and exp:
                m = RE_IMPORTER.search(meta_b)
                imp = m.group(1).decode() if m else None
                if imp and imp != exp:
                    fails.append("TYPE_IMPORTER(%s!=%s)" % (imp, exp))
                elif not imp:
                    misc_counts["IMPORTER_DEFAULT(meta 无 importer 块，吃 Unity 默认)"] += 1
            dep_bad = []
            if ext in YAML_EXTS:
                for m in RE_REF.finditer(rb(p)):
                    g = m.group(2).decode().lower()
                    if g == ZERO_GUID or g.startswith(BUILTIN_PREFIX):
                        continue
                    if g not in ctx.guid2path:
                        dep_bad.append(g)
                if dep_bad:
                    fails.append("DEPS_MISSING(%d)" % len(dep_bad))
            lkind, lkey = is_loadable(p, r, ext)
            if not lkind:
                fails.append("NOT_LOADABLE")
            p1 = ""
            if ext in (".png", ".jpg"):
                kind, wh = image_size(p)
                expect_kind = "png" if ext == ".png" else "jpg"
                if kind and kind != expect_kind:
                    fails.append("CONTENT_EXT_MISMATCH(实际=%s)" % kind)
                if not wh:
                    if not kind:
                        fails.append("SIZE_INVALID_IMAGE")
                else:
                    w, h = wh
                    p1 = "%dx%d" % (w, h)
                    ps = parse_platform_settings(meta_b)
                    mt = re.search(rb"^  maxTextureSize:\s*(\d+)", meta_b, re.M) if meta_b else None
                    top_max = int(mt.group(1)) if mt else None
                    mx, src = effective_max_size(ps, top_max)
                    if mx and (w > mx or h > mx):
                        fails.append("SIZE_DOWNSAMPLED(%dx%d>%d@%s)" % (w, h, mx, src))
                    if w > 8192 or h > 8192:
                        fails.append("SIZE_TOO_BIG(%dx%d)" % (w, h))
                if meta_b is not None and ctx.resources_key(p) is not None:
                    m = RE_ISREADABLE.search(meta_b)
                    if m and m.group(1) == b"0":
                        # 只有「会进读像素通道」的才必须 isReadable=1（B100_X11 §2-1 判据）
                        consumer = (r.startswith("CardPresentation/Resources/Art/cards/clan/")
                                    or "/frame_" in os.path.basename(p)
                                    or (stem in pixel_near))
                        if consumer:
                            fails.append("READABLE_BAD(isReadable=0)")
            order = [("META_MISSING", "MISSING"), ("GUID_INVALID", "UNKNOWN"), ("GUID_DUP", "UNKNOWN"),
                     ("TYPE_IMPORTER", "TYPE_MISMATCH"), ("DEPS_MISSING", "DEPS_MISSING"),
                     ("CONTENT_EXT_MISMATCH", "CONTENT_MISMATCH"),
                     ("SIZE_INVALID_IMAGE", "SIZE_BAD"), ("SIZE_DOWNSAMPLED", "SIZE_BAD"),
                     ("SIZE_TOO_BIG", "SIZE_BAD"), ("READABLE_BAD", "READABLE_BAD")]
            st = "OK"
            for pre, name in order:
                if any(f.startswith(pre) for f in fails):
                    st = name
                    break
            if st == "OK" and "NOT_LOADABLE" in fails:
                st = "PRESENT_NOT_LOADABLE"
            st_counts[st] += 1
            if fails:
                cls, fix = classify(st, fails)
                if st == "PRESENT_NOT_LOADABLE":
                    vendor = r.startswith(("Plugins/", "ThirdParty/", "TextMesh Pro/", "Settings/"))
                    artish = ext in (".png", ".jpg", ".ogg", ".wav", ".webm", ".mp4")
                    ev_hit = (artish and not vendor and len(stem) >= 6
                              and (stem in code_lit or stem in data_toks))
                    if ev_hit:
                        cls = "有件取不到"
                        fix = "核：代码/数据表引用了它，但运行期取不到（搬进 Resources 或改引用）"
                    else:
                        cls = ""
                        fix = "非运行期资产（信息项，不计缺陷）"
                # ---- 🆕 2026-10-31（B110_R5）分类修正（判据 → `资料/完全复刻/判据/B110_R5_资源侧放回11条.md`）----
                # ② 运行时接管的悬空：静态引用是坏的、运行期有明确绑定路径 ⇒ 不补静态（混着真悬空的仍按缺陷）
                if st == "DEPS_MISSING" and dep_bad and all(g in runtime_bound_guids() for g in dep_bad):
                    cls = ""
                    fix = ("依赖悬空但**运行时已接管**（%d 处全在 RUNTIME_BOUND_GUIDS："
                           "ArenaOriginalShaderRebind / ArenaSceneAnims 按原版名绑定）—— 不补静态引用" % len(dep_bad))
                    misc_counts["依赖悬空（运行时接管）"] += 1
                # ① 源树：运行期不直接加载 ⇒ 一律转信息项（**保留行**，不进行动清单）
                if in_source_tree(r):
                    if cls:
                        misc_counts["源树（ArenaOriginalPrefabs）· " + cls] += 1
                        fix = ("源树 `WarpforgeVFX/ArenaOriginalPrefabs/**`：运行期不直接加载（构建期由 "
                               "ArenaBuilder / 清单消费）—— " + fix)
                    cls = ""
                if cls:
                    cls_counts[cls] += 1
                    if len(cls_examples[cls]) < args.top:
                        cls_examples[cls].append(r)
                fl = [f for f in fails if f != "NOT_LOADABLE"] or ["NOT_LOADABLE"]
                ev = r
                if lkind:
                    ev += "  [%s:%s]" % (lkind, lkey) if lkey else "  [%s]" % lkind
                rows.append(["2", dom, orig_name or "查不到", typ, orig_name or "查不到",
                             r, st, ";".join(fl), p1, "", "", "", cls, fix, ev])
        log("② 结果：" + " ".join("%s=%d" % kv for kv in st_counts.most_common()))
        log("② 缺陷：" + (" ".join("%s=%d" % kv for kv in cls_counts.most_common()) or "无"))

    # ================================================ ③ 层
    if args.only in ("3", "all"):
        log("—— ③ 层：与原版镜像逐项比 ——")
        p3_counts = collections.Counter()
        content = {"EXACT": 0, "NEAR": 0, "DIFF": 0, "SKIP_DIM": 0, "SKIP_NO_PIL": 0}
        tex_pairs = 0
        audio_pairs = 0
        audio_same = 0
        audio_diff = 0
        mat_pairs = 0
        mat_missing_rows = 0
        mat_equiv_rows = 0            # 🆕 B111_T2B：其中**有等价件 / 结构性不可能**、已转信息项的
        miss_by_shader = collections.Counter()
        drift_props = collections.Counter()
        orig_mat_seen = set()
        try:
            import PIL  # noqa: F401
            have_pil = True
        except Exception:
            have_pil = False
        for p, r, ext, sz in ctx.files:
            stem = os.path.splitext(os.path.basename(p))[0]
            dom = domain_of(r)
            if ext in (".png", ".jpg"):
                if stem not in ctx.orig_tex:
                    continue
                tex_pairs += 1
                ow = png_size(ctx.orig_tex[stem][0])
                wh = png_size(p)
                if not ow or not wh:
                    p1 = "尺寸读不出（%s）" % os.path.splitext(p)[1]
                    rows.append(["3", dom, stem, TYPE_OF.get(ext, ext), stem, r, "OK", "",
                                 p1, "跳过", "", "", "", "", p])
                    continue
                p1 = "尺寸 %dx%d vs 原版 %dx%d" % (wh[0], wh[1], ow[0], ow[1])
                p2 = p3 = ""
                cls = ""
                fix = ""
                if wh == ow:
                    if args.no_content or not have_pil:
                        content["SKIP_NO_PIL"] += 1
                        p2 = "跳过（--no-content）" if args.no_content else "跳过（无 PIL）"
                    else:
                        try:
                            h1, s1 = pixel_md5(p)
                            h2, s2 = pixel_md5(ctx.orig_tex[stem][0])
                            if h1 == h2:
                                content["EXACT"] += 1
                                p2 = "像素逐字节相同"
                            else:
                                d = pixel_diff(p, ctx.orig_tex[stem][0])
                                if d is None:
                                    p2 = "尺寸读取不一致"
                                elif d["all"] <= 3.0:
                                    content["NEAR"] += 1
                                    p2 = "几乎相同(RGBA mean|Δ|=%.2f，压缩/重编码差)" % d["all"]
                                elif d["rgb_mask"] <= 3.0 and d["rgb_mask"] < d["rgb"]:
                                    content["NEAR"] += 1
                                    p2 = ("仅透明区 RGB 不同：可见像素 mean|Δ|=%.2f / 全图 %.2f"
                                          "（alpha=0 的像素不可见，不算内容差）"
                                          % (d["rgb_mask"], d["rgb"]))
                                elif d["gamma"] <= 3.0:
                                    content["NEAR"] += 1
                                    p2 = ("gamma 编码等价：srgb2lin(一方) vs 另一方 mean|Δ|=%.2f"
                                          "（我们存 sRGB / 原版存 linear；导入 sRGBTexture=1 后 GPU 取值一致）"
                                          % d["gamma"])
                                elif d["rgb"] <= 1.0:
                                    content["NEAR"] += 1
                                    p2 = ("RGB 相同、仅 alpha 不同(RGB mean|Δ|=%.2f / RGBA=%.2f)"
                                          % (d["rgb"], d["all"]))
                                else:
                                    content["DIFF"] += 1
                                    hint = ""
                                    if re.search(r"(Normal|NRM|nrm)", stem):
                                        hint = ("（法线图：旧说法『压缩通道重排』已作废 —— 真因是 gamma 编码；"
                                                "本行是连 gamma 复检也不等价）")
                                    p2 = "内容不同(RGB mean|Δ|=%.1f / RGBA=%.1f / 可见区 RGB=%.1f)%s" % (
                                        d["rgb"], d["all"], d["rgb_mask"], hint)
                                    cls, fix = "内容不同（需核）", \
                                        ("核：是否拿错图/换过异画（判据 B110_R1 §4：先过 alpha=0 掩码与 "
                                         "gamma 复检，两档都不等价才算真差异）")
                        except Exception as e:
                            content["SKIP_NO_PIL"] += 1
                            p2 = "比对失败:%s" % type(e).__name__
                else:
                    content["SKIP_DIM"] += 1
                    p2 = "跳过（尺寸不同，多为裁片）"
                sj = ctx.orig_sprite.get(stem)
                if sj:
                    try:
                        d = json.load(io.open(sj, encoding="utf-8"))
                        tr = (d.get("m_RD") or {}).get("textureRect") or {}
                        if tr:
                            tw = round(tr["x"] + tr["width"]) - round(tr["x"])
                            th = round(tr["y"] + tr["height"]) - round(tr["y"])
                            p3 = "sprite rect %dx%d vs 我们 %dx%d%s" % (
                                tw, th, wh[0], wh[1], "" if (tw, th) == wh else "  ⚠差")
                            if (tw, th) != wh and cls == "":
                                p3 += "（裁片允许 ±1）"
                    except Exception:
                        p3 = "sprite json 读失败"
                else:
                    p3 = "查不到原版 sprite"
                # 🆕 2026-10-31（B110_R5）：源树里的贴图缺陷一律转信息项（同 ② 层；判据 §B-39/§B-40）
                if in_source_tree(r) and cls:
                    misc_counts["源树（ArenaOriginalPrefabs）· " + cls] += 1
                    fix = "源树 `WarpforgeVFX/ArenaOriginalPrefabs/**`：运行期不直接加载 —— " + fix
                    cls = ""
                if cls:
                    cls_counts[cls] += 1
                    if len(cls_examples[cls]) < args.top:
                        cls_examples[cls].append(r)
                rows.append(["3", dom, stem, TYPE_OF.get(ext, ext), stem, r, "OK", "",
                             p1, p2, p3, "", cls, fix, ctx.orig_tex[stem][0]])
            elif ext == ".mat":
                b = rb(p)
                nm = mat_name(b)
                m = RE_SHADER_REF.search(b)
                our_shader_path = ctx.guid2path.get(m.group(2).decode().lower()) if m else None
                our_name = ""
                our_props, our_kws = set(), set()
                if our_shader_path and os.path.isfile(our_shader_path):
                    our_props, our_kws, our_name = parse_shader(our_shader_path, ctx.shader_cache)
                colors, floats, texenvs, kws_valid, kws_invalid = parse_mat_props(b)
                p1 = p3 = p4 = ""
                cls = ""
                fix = ""
                if nm in ctx.orig_mat:
                    mat_pairs += 1
                    # 🆕 2026-10-31（B110_R5）：`d:/2` 里**真的存在同名多份原版材质**（例 `SandParticle`
                    #   两份：`battleprefabs_vfxandmisc` = `_QueueOffset 0` / `battlesharedresources` = `1`，
                    #   而原版 `Battle Arena 2.unity` 引的是后者）。原来 `ctx.orig_mat` 只留第一份 ⇒
                    #   会拿**错的那份**当唯一真值、把正确的我们判成「漂移」。
                    #   ⇒ 逐份比、取「**未裁定差异最少**」的那份；**都不匹配且有多份**才报「需人工判」。
                    cands = ctx.orig_mat_all.get(nm) or [ctx.orig_mat[nm]]
                    best = None      # (未裁定差异数, diffc, difff, oc, of, shader_name, json)
                    for oj, opid in cands:
                        oname = ctx.shader_pid2name.get(opid, "")
                        try:
                            o = json.load(io.open(oj, encoding="utf-8"))
                            sp = o.get("m_SavedProperties") or {}
                            oc = {}
                            for item in sp.get("m_Colors") or []:
                                try:
                                    oc[item[0]] = tuple(round(float(item[1][k]), 4) for k in ("r", "g", "b", "a"))
                                except Exception:
                                    pass
                            of = {}
                            for item in sp.get("m_Floats") or []:
                                try:
                                    of[item[0]] = round(float(item[1]), 6)
                                except Exception:
                                    pass
                        except Exception:
                            continue
                        diffc = [k for k in oc if k in colors and oc[k] != colors[k]]
                        difff = [k for k in of if k in floats and abs(of[k] - floats[k]) > 1e-4]
                        nunruled = len([k for k in (diffc + difff) if k not in RULED_DIFFS])
                        cand = (nunruled, diffc, difff, oc, of, oname, oj)
                        if best is None or cand[0] < best[0]:
                            best = cand
                    if best is None:
                        p1 = "原版材质读失败"
                    else:
                        nunruled, diffc, difff, oc, of, oname, oj = best
                        p3 = "shader 我们=%s / 原版=%s" % (our_name or "(未解析)", oname or "(未解析)")
                        if oname and our_name and oname != our_name:
                            p3 += "  ⚠不同"
                        if len(cands) > 1:
                            p3 += "  ⚠同名原版 %d 份（逐份比、取最接近的一份）" % len(cands)
                        common = set(oc) & set(colors)
                        p1 = "共有属性 颜色%d/浮点%d，差 颜色%d/浮点%d" % (
                            len(common), len(set(of) & set(floats)), len(diffc), len(difff))
                        if diffc or difff:
                            allk = diffc + difff
                            ruled = [k for k in allk if k in RULED_DIFFS]
                            unruled = [k for k in allk if k not in RULED_DIFFS]
                            p1 += " 例:%s" % ",".join(allk[:3])
                            if ruled:
                                p1 += "  ⚑已裁定有意:%s" % ",".join(sorted(set(ruled)))
                            if unruled:
                                if len(cands) > 1:
                                    cls, fix = ("参数漂移（同名多源、需人工判）",
                                                "人工判：`d:/2` 里有 %d 份同名原版材质，先定哪一份是本 prefab/本场用的"
                                                % len(cands))
                                else:
                                    cls, fix = "参数漂移", "核：颜色/浮点是否照原版 m_SavedProperties"
                            else:
                                cls, fix = "参数漂移（已裁定有意）", "不修（判据 RULED_DIFFS → 特效线任务总表:164）"
                            for k in diffc:
                                drift_props["颜色 " + k] += 1
                            for k in difff:
                                drift_props["浮点 " + k] += 1
                else:
                    p1 = "查不到同名原版材质"
                if our_props and our_shader_path and our_shader_path.startswith(ASSETS) \
                        and not our_shader_path.endswith(".shadergraph"):
                    # ⚠️ 只对我们**自己写的 .shader** 判「属性有没有被消费」：
                    #    包内 shader（URP Lit 等）属性写在 include 里、ShaderGraph 的属性在 .shadergraph 里，
                    #    按 .shader 正文判会整批误报（实测 1,164 → 只留自写 shader 后大幅收敛）。
                    undeclared = [x for x in (texenvs + list(colors) + list(floats))
                                  if x not in our_props and not x.startswith("unity_") and not x.endswith("_ST")]
                    unk = [k for k in kws_valid if k not in our_kws]
                    # 🔴 2026-10-31 `B110_F3`：`m_InvalidKeywords` 不再计入缺陷。
                    #   Unity 把「目标 shader 不认识」的关键字放在这一节（原版材质从 URP/Mobile 粒子
                    #   shader 切到 Everguild shader 时的残留），运行时开着也不起作用 = 纯死值
                    #   （`B110_R1 §7` ①）⇒ 单独进信息项，⛔ 别当「未声明」缺陷报。
                    dead = [k for k in kws_invalid if k not in our_kws]
                    if dead:
                        misc_counts["关键字死值(在 m_InvalidKeywords、目标 shader 不认)"] += len(dead)
                    p4 = "属性未声明 %d · 关键字未声明 %d" % (len(undeclared), len(unk))
                    if undeclared:
                        p4 += " 例:" + ",".join(sorted(set(undeclared))[:3])
                    if undeclared or unk:
                        tag = "材质未消费（属性未声明）" if undeclared else "材质未消费（关键字未声明）"
                        if not cls:
                            cls = tag
                            fix = "核：这些属性/关键字目标 shader 里不存在（多数无害；若原版靠它驱动则缺功能）"
                        elif tag not in cls:
                            cls += "＋" + tag
                elif our_props:
                    p4 = "目标 shader 非自写（包内 / ShaderGraph）⇒ 不判消费"
                else:
                    p4 = "shader 未解析，无法判消费"
                # 🆕 2026-11-01（`B111_T2` / 账 `B-75`）：构建期占位 `.mat` 的 P1 参数漂移转信息项。
                #   见 `BUILD_PLACEHOLDER_RE` 上面那段注释（判据 §B-41）。⛔ 只摘 P1，不摘 P4。
                if in_build_placeholder(r) and cls.startswith("参数漂移"):
                    misc_counts["BUILD_PLACEHOLDER（构建期占位 .mat · 运行期 Rebuild）· " + cls] += 1
                    fix = ("占位 `.mat`：`ArenaBuilder.GetOrCreateMaterial`/`SaveOrReuse` 按【贴图名】生成、"
                           "每个 ArenaBuilder 批次重写；运行期 `ArenaOriginalMaterial.Rebuild()` 按清单 props "
                           "重建真值（53 vs 53 / 47 vs 47 差 0）⇒ P1 参数漂移不是缺陷 "
                           "（判据 `判据/B110_R5_资源侧放回11条.md` §B-41）")
                    cls = ""
                # 🆕 2026-10-31（B110_R5）：源树里的材质缺陷一律转信息项（同 ② 层；判据 §B-38）
                if in_source_tree(r) and cls:
                    misc_counts["源树（ArenaOriginalPrefabs）· " + cls] += 1
                    fix = "源树 `WarpforgeVFX/ArenaOriginalPrefabs/**`：运行期不直接加载 —— " + fix
                    cls = ""
                if cls:
                    for cc in [x for x in cls.split("＋") if x]:
                        cls_counts[cc] += 1
                        if len(cls_examples[cc]) < args.top:
                            cls_examples[cc].append(r)
                rows.append(["3", dom, nm, "Material", nm, r, "OK", "", p1, "", p3, p4,
                             cls, fix, r])
            elif ext in (".ogg", ".wav"):
                if stem not in ctx.orig_audio:
                    continue
                audio_pairs += 1
                op = ctx.orig_audio[stem]
                so = os.path.getsize(op)
                p1 = "字节 %d vs 原版 %d%s" % (sz, so, "" if sz == so else "  ⚠大小不同")
                p2 = ""
                cls = ""
                fix = ""
                if sz == so:
                    try:
                        same = hashlib.md5(rb(p)).hexdigest() == hashlib.md5(rb(op)).hexdigest()
                    except Exception:
                        same = False
                    if same:
                        p2 = "字节逐字节相同"
                        audio_same += 1
                    else:
                        p2 = "同大小但内容不同"
                        audio_diff += 1
                        cls, fix = "内容不同（需核）", "核：音频是否被转码/替换"
                else:
                    audio_diff += 1
                    cls, fix = "内容不同（需核）", "核：音频是否被转码/替换"
                if cls:
                    for cc in [x for x in cls.split("＋") if x]:
                        cls_counts[cc] += 1
                        if len(cls_examples[cc]) < args.top:
                            cls_examples[cc].append(r)
                rows.append(["3", dom, stem, "AudioClip", stem, r, "OK", "", p1, p2, "", "",
                             cls, fix, op])
        # 原版材质我们没有的
        all_mat_names = set()
        mat_file_stems = set()       # 🆕 B111_T2B：`.mat` 的**文件名**（`Resources.Load` 按路径取，路径才是功能身份）
        for p, r, ext, sz in ctx.files:
            if ext == ".mat":
                all_mat_names.add(mat_name(rb(p)))
                mat_file_stems.add(os.path.basename(r)[:-4])
        hsh = hash_equiv_map(all_mat_names)          # 🆕 B111_T2B：`<名>__<hash>` 等价件
        for nm, (oj, opid) in sorted(ctx.orig_mat.items()):
            if nm in all_mat_names:
                continue
            oname = ctx.shader_pid2name.get(opid, "")
            # 🆕 2026-11-01（`B111_T2B` / 账 `B-08`）：四条等价/结构性判据 → 转信息项（见上面那段注释）
            eq = None
            if oname in SHADERGRAPH_NO_SOURCE:
                eq = ("ShaderGraph 无 .shader 源、结构性建不出",
                      "不是缺陷：shader 是 ShaderGraph 生成体、没有 `.shader` 源 ⇒ `.mat` 结构性建不出来"
                      "（`Core/BoosterPackAssets.cs:29-33`）；原版那批在 `wf_boosters_extra.bundle` 包内可解、0 悬空")
            elif oname in TMP_FONT_SHADERS:
                eq = ("TMP 逐文本字体材质变体",
                      "不是缺陷：这是 TMP 给**逐文本**生成的字体材质变体；我们统一走 `Label`"
                      "（TMP 顶点色 + `Asar-Regular SDF`），不需要这些变体")
            elif nm in MENU_UI_EQUIV and os.path.isfile(
                    os.path.join(ASSETS, MENU_UI_EQUIV_DIR + MENU_UI_EQUIV[nm] + ".mat")):
                eq = ("有等价件（自研 WFUI_*）",
                      "不是缺陷：我们的等价件 = `Resources/Art/vfx/%s.mat`（1:1，已接线，"
                      "经 `Shell/MenuFxMaterials` 加载）" % MENU_UI_EQUIV[nm])
            elif nm in hsh:
                eq = ("有等价件（去重后缀 __hash）",
                      "不是缺陷：工程里有等价件 `%s.mat`（`EffectExporter` 的去重命名）" % hsh[nm])
            elif nm in mat_file_stems:
                eq = ("有同名路径（.mat 文件名 = 原版名，仅 m_Name 不同）",
                      "不是缺陷：`Resources.Load` 按**路径**取材质，`%s.mat` 就在；它内部的 `m_Name` "
                      "是别的（改名拷贝进来的），**路径身份成立**" % nm)
            if eq:
                misc_counts["材质未消费（%s）" % eq[0]] += 1
                cls, fix = "", eq[1]
                p4 = "原版 shader=%s（引用数 %s）%s  等价件=%s" % (
                    oname or "?", ctx.shader_orig_refs.get(oname, "?"),
                    "  ⚑R4-07" if oname in R4_07 else "",
                    MENU_UI_EQUIV.get(nm) or hsh.get(nm) or "-")
                rows.append(["3", "菜单UI" if "UI/" in oname else domain_of(""), nm, "Material", nm,
                             "", "", "", "", "", "", p4, cls, fix, oj])
                if oname:
                    orig_mat_seen.add(oname)
                mat_missing_rows += 1
                mat_equiv_rows += 1
                continue
            cls = "材质未消费（原版有、我们没有同名实例）"
            fix = "核：是否已被等价材质替代（R4-07 那 13 条 UI 材质动画就在这一类）"
            p4 = "原版 shader=%s（引用数 %s）%s" % (
                oname or "?", ctx.shader_orig_refs.get(oname, "?"),
                "  ⚑R4-07" if oname in R4_07 else "")
            rows.append(["3", "菜单UI" if "UI/" in oname else domain_of(""), nm, "Material", nm, "",
                         "", "", "", "", "", p4, cls, fix, oj])
            cls_counts[cls] += 1
            if len(cls_examples[cls]) < args.top:
                cls_examples[cls].append(nm)
            if oname:
                orig_mat_seen.add(oname)
            miss_by_shader[oname or "?"] += 1
            mat_missing_rows += 1
        log("③ 结果：贴图配对 %d（逐像素 相同=%d 近似=%d 不同=%d 尺寸不同跳过=%d 未比=%d）· 音频配对 %d（字节相同=%d 不同=%d）· 材质配对 %d · 原版材质我们没有 %d（其中**有等价件/结构性不可能 %d**，已转信息项 · **真缺 %d**）"
            % (tex_pairs, content["EXACT"], content["NEAR"], content["DIFF"],
               content["SKIP_DIM"], content["SKIP_NO_PIL"], audio_pairs, audio_same, audio_diff,
               mat_pairs, mat_missing_rows, mat_equiv_rows, mat_missing_rows - mat_equiv_rows))
        log("③ 原版材质缺口按 shader 分组（前 15）：")
        for sh, n in miss_by_shader.most_common(15):
            log("   %5d  %s%s" % (n, sh, "  ⚑R4-07" if sh in R4_07 else ""))
        if drift_props:
            log("③ 参数漂移按属性名分组（前 12）：")
            for k, n in drift_props.most_common(12):
                log("   %5d  %s" % (n, k))

    # ================================================ 加载实参落不到盘
    if args.only in ("2", "all"):
        miss = 0
        for s in ctx.load_sites:
            if s["kind"] != "literal" or s["editor"]:
                continue
            val = s["value"]
            found = False
            for R in ctx.resources_roots:
                base = os.path.join(R, val.replace("/", os.sep))
                if os.path.isdir(base):
                    found = True
                    break
                for e in ("", ".png", ".jpg", ".asset", ".mat", ".shader", ".anim", ".json",
                          ".ogg", ".wav", ".webm", ".mp4", ".mixer", ".controller", ".prefab",
                          ".ttf", ".otf", ".txt", ".bytes"):
                    if os.path.isfile(base + e):
                        found = True
                        break
                if found:
                    break
            if not found:
                miss += 1
                rows.append(["2", "加载面", val, "加载实参", val, "%s:%d" % (s["file"], s["line"]),
                             "MISSING", "LOAD_TARGET_ABSENT", "", "", "", "",
                             "有件取不到", "核：实参拼错 / 图没导 / 只在该探针里故意不存在",
                             "%s:%d  \"%s\"" % (s["file"], s["line"], s["arg"])])
                cls_counts["有件取不到"] += 1
                if len(cls_examples["有件取不到"]) < args.top:
                    cls_examples["有件取不到"].append("%s:%d %s" % (s["file"], s["line"], val))
        log("② 加载面：运行期 literal 落不到盘 = %d" % miss)

    # ================================================ 摘要
    log("—— 缺陷类汇总 ——")
    for k, v in cls_counts.most_common():
        log("  %-28s %6d  例: %s" % (k, v, " · ".join(str(x) for x in cls_examples[k][:3])))
    if misc_counts:
        log("—— 信息项（不计缺陷）——")
        for k, v in misc_counts.most_common():
            log("  %-56s %6d" % (k, v))

    # 写盘
    if not args.dry:
        out = args.out
        os.makedirs(os.path.dirname(out), exist_ok=True)
        with io.open(out, "w", encoding="utf-8", newline="\n") as f:
            f.write("\t".join(COLS) + "\n")
            for x in rows:
                f.write("\t".join(str(y) for y in x) + "\n")
        log("已写 %s（%d 行）" % (out, len(rows)))
    else:
        log("--dry：未写盘（应有 %d 行）" % len(rows))
    return rows, cls_counts, logs


def classify(st, fails):
    if st == "MISSING":
        return "缺件", "补 .meta"
    if st == "UNKNOWN" and any(f.startswith("GUID") for f in fails):
        return "GUID 异常", "去重 / 补有效 guid"
    if st == "TYPE_MISMATCH":
        return "类型错", "改导入器（见 .meta 的 importer 块）"
    if st == "DEPS_MISSING":
        return "依赖悬空", "补依赖或改引用"
    if st == "CONTENT_MISMATCH":
        return "文件内容与扩展名不符", "按真实格式改名 / 重新导出（Unity 按扩展名选导入器）"
    if st == "SIZE_BAD":
        return "尺寸格式错", "改 maxTextureSize / 重导"
    if st == "READABLE_BAD":
        return "有件取不到", "isReadable 改 1"
    if st == "PRESENT_NOT_LOADABLE":
        return "有件取不到", "核：运行期是否真的不需要（需要则搬进 Resources 或加引用）"
    return "", ""


def main():
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--scan", action="store_true")
    ap.add_argument("--dry", action="store_true")
    ap.add_argument("--only", default="all", choices=["2", "3", "all"])
    ap.add_argument("--out", default=DEFAULT_OUT)
    ap.add_argument("--no-content", action="store_true")
    ap.add_argument("--top", type=int, default=25)
    ap.add_argument("-h", "--help", action="store_true")
    args = ap.parse_args()
    if args.help or not (args.scan or args.dry):
        print(__doc__)
        return 0
    run(args)
    return 0


if __name__ == "__main__":
    sys.exit(main())
