#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_tmp_font_assets.py —— 把**原版那三份现成的** `TMP_FontAsset` 导进工程（`A1118①` + `A1300`）。

## 这一件是什么 / 不是什么（口径，别混）

  · **是**：把原版包里**已经烘好的**那三份字体资产（`Pragati-Regular SDF` / `Asar-Regular SDF` /
    `RobotoCondensed-Regular SDF`）
    **逐字段抄**成工程资产 —— 图集是**原版那张**（`m_GlyphTable` / `m_CharacterTable` /
    `m_UsedGlyphRects` / `m_FreeGlyphRects` 全抄，`m_AtlasPopulationMode = 0` = **Static**）。
  · **不是**：`A1097②③` 那条**「从原版 JSON + 图集 PNG 重建」**的路
    （那条走 `TMP_FontAsset.CreateFontAsset` **重新栅格化**，产出的是「我们自己画的字体」）。
    🔴 **主对话已裁：走本件（`A1118①`），`A1097②③` 的「重建」路作废**（⛔ 别各做一份）——
    见 `资料/交接_第八会话.md` §「已裁」那一行。

## 判据从哪来（全部实读，⛔ 没有一处是猜的）

  | 要抄什么 | 出处 |
  |---|---|
  | 42 个字段的值 | `d:/2/新解包资源/assets_full/bundle_fonts_assets_all/MonoBehaviour/<名字>.json`（原版包里的 `MonoBehaviour` 对象，TypeTree 导出） |
  | **图集贴图 / 材质的【字段值】** | 同包 `Material/Material_<pathID>.json`（脚本按 `<字体>.m_Material.m_PathID` 现取） |
  | **材质 `.mat` 的【骨架与键序】** | `Assets/WarpforgeVFX/Materials/Pragati-Regular Atlas Material.mat`（**本工程这台 Unity 自己写的** —— `_selftest()` 断言「照本脚本的规则从原版 JSON 重建 Pragati 那份 == 盘上那份**逐字节**」） |
  | **字段的【名字与顺序】** | `Assets/CardPresentation/Resources/Fonts/NotoSerifCJK-Regular SDF.asset` 的 `--- !u!114 &11400000` 块 —— 那是**本工程这台 Unity 自己写出来的** TMP_FontAsset，48 个顶层字段（= 10 个 MonoBehaviour 头 + **42 个正文字段**，与上面那份 JSON 的 42 个键**逐一相等**）。本脚本在 `_selftest()` 里**现读这份模板并断言两个集合相等**，所以模板漂了会当场报错 |
  | 图集贴图 | `Assets/WarpforgeVFX/Textures/<名字> Atlas.png`（**像素与原版逐位相同** —— 见下） |
  | 字体材质 | `Assets/WarpforgeVFX/Materials/<Pragati|Asar>-Regular Atlas Material.mat`（原版 `m_Material` 指的就是它） |

  🔴 **图集 PNG 的「像素相同」是量过的**（本脚本 `_selftest()` 里再量一次）：
  工程里那两份 PNG 与 `assets_full/.../Texture2D/<名字> Atlas.png` **文件字节不同**
  （`EffectExporter.ImportTexture` 用 `EncodeToPNG` 重编过），但**解码后的 RGBA 逐位相同**
  （实测 `Pragati 2048×1024` / `Asar 1024×1024`，最大通道差 `[0,0,0,0]`、非零像素 `0`）。
  数据在 **alpha** 通道（RGB 恒 0）⇒ 与 TMP 的 SDF shader 取 `.a` 一致。

## 为什么可以「不跑 Unity」就落盘

`TMP_FontAsset` 是普通 `ScriptableObject`，Unity 的 `.asset` 就是 **YAML**。
原版 JSON 的 42 个键与工程模板的 42 个正文字段**完全同名**（`_selftest()` 断言），
⇒ 这是一次**逐字段转写**，不是重建。本脚本把 `m_FileID/m_PathID` 那种引用改成
`{fileID: …, guid: …, type: …}` 的形式指向工程里那两份现成资产。

## 跑法

```bash
python -I d:/4/Unity/工具/gen_tmp_font_assets.py            # 生成（覆盖写；并补 `OUT_DIR` 里缺的图集/材质）
python -I d:/4/Unity/工具/gen_tmp_font_assets.py --verify    # 只校验已生成的三份（**只查不写**）
```

## 落到哪

`Assets/CardPresentation/Resources/Fonts/<名字>.asset` + `.meta`（与 `NotoSerifCJK-Regular SDF` 同目录
—— 那是本工程 TMP 字体资产的**既有落点**，且 `Resources.Load` 拿得到）。
`RobotoCondensed` 另加它的**图集 PNG** 与**材质 `.mat`**（各带 `.meta`）**也在这个目录下**（见上一节）。
⚠️ **`.meta` 必须一起写**：`BoosterPackExporter` 重导时会按 **`TMP_FontAsset` 的名字**
（`FindProjectAsset`）去找工程资产，而 prefab 里接的是 **guid** ⇒ guid 是**本脚本定死的**
（`hashlib.md5` 派生 + `_selftest()` 断言它在本工程 1 万多个 guid 里**唯一**）。

## 为什么会多写两样东西（`A1300`：第三份 `RobotoCondensed-Regular SDF`）

`A1097②③` 的口径是「**三份** Static `TMP_FontAsset`」，而本件 2026-10-10 之前只导了两份
（`Pragati` / `Asar`）⇒ 补第三份。**它与前两份有一处不同：原版包里有它，但【工程里没有】**——
`Pragati` / `Asar` 的图集 + 材质当年是被 `EffectExporter.ImportMaterial` 顺带导进来的
（那些 prefab 上有 TMP 组件引用了它们），而**全工程没有任何一处引用 `RobotoCondensed`**
（现读：`grep -rl` 三个原版 pathID 在本工程 **0 命中**）⇒ 那两样**从来没被导进来过**。

⇒ 本脚本因此多一段 `_ensure_side_assets()`：**只对「落在 `OUT_DIR` 里」的字体**，把缺的
**图集 PNG**（**逐字节照抄原版那份**）与 **材质 `.mat`** 一起落盘（各带 `.meta`）。
🔴 **它【不会】往 `OUT_DIR` 以外写任何东西**（本会话白名单 = `Resources/Fonts/**`）——
路径不在 `OUT_DIR` 里的（= `Pragati` / `Asar` 那两份）**必须在盘上已存在，否则停手报错**。

⚠️ **因此 `RobotoCondensed` 的图集/材质落在 `Resources/Fonts/` 下，而前两份在
`Assets/WarpforgeVFX/{Textures,Materials}/`**（那是导出器的落点）。这是**本会话白名单的边界**所致，
不是「原版就是这样」—— 见交接报告的「没查清」一节。

## 没做的事（如实记）

  · **源 `.ttf` 本地没有**：`Pragati` / `Asar` 的字体文件在 `d:/2/新解包资源/` 与 `d:/2/解包整理/`
    全库**零命中**（只有 `NotoSerifCJK-Regular.ttf`）⇒ `m_SourceFontFile` 只能留 `{fileID: 0}`，
    `m_SourceFontFileGUID` / `m_CreationSettings.sourceFontFileGUID` **照抄原版那个字符串**
    （它在本工程解析不到任何东西，但**是原版的真实值** —— 静默改掉它反而丢信息）。
    ⚠️ **Static（`m_AtlasPopulationMode = 0`）下 TMP 运行时不读源字体** ⇒ 缺它不影响渲染。
  · 字体图集贴图在 `Assets/WarpforgeVFX/**` 里，而那棵树**是 `.gitignore` 的**
    （`.gitignore:26`）⇒ 本 `.asset` 是入库的、它引的贴图不入库。若那棵树被
    `EffectExporter.ClearGenerated()` 清掉，本字体资产的贴图引用会断（**材质那份也一起断**，
    是这条链的既有状态，不是本件引入的）。
"""
import hashlib
import io
import json
import os
import re
import sys

# 本机控制台是 GBK：`✅` `⛔` 这类字符打不出来会直接抛 `UnicodeEncodeError`（把整条命令弄挂）。
# 保留控制台自己的编码、只把编不了的字符换成 `?` —— 中文照常显示。
try:
    sys.stdout.reconfigure(errors="replace")
    sys.stderr.reconfigure(errors="replace")
except Exception:
    pass

# ─────────────────────────── 常量（每一个都有出处） ───────────────────────────

FONTS_BUNDLE = r"d:/2/新解包资源/assets_full/bundle_fonts_assets_all"
ASSETS = r"d:/4/Unity/MyGame/Assets"
PROJ_TMP_ASSET = ASSETS + "/CardPresentation/Resources/Fonts/NotoSerifCJK-Regular SDF.asset"

#: 工程里 TMP_FontAsset 的 `m_Script` guid（TMP 随 `com.unity.ugui` 进工程）。
#: 出处：`Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset` 与
#: `Assets/CardPresentation/Resources/Fonts/NotoSerifCJK-Regular SDF.asset` **两份都写着它**；
#: `Library/PackageCache/com.unity.ugui@*/Editor/TMP/TMP_PackageUtilities.cs` 也引用它。
TMP_FONTASSET_SCRIPT_GUID = "71c1514a6bd24e1e882cebbe1904ce04"

#: 两份字体 → (原版 MonoBehaviour JSON 名, 工程里的图集贴图路径, 工程里的 Atlas Material 路径)
FONTS = [
    ("Pragati-Regular SDF",
     "WarpforgeVFX/Textures/Pragati-Regular SDF Atlas.png",
     "WarpforgeVFX/Materials/Pragati-Regular Atlas Material.mat"),
    ("Asar-Regular SDF",
     "WarpforgeVFX/Textures/Asar-Regular SDF Atlas.png",
     "WarpforgeVFX/Materials/Asar-Regular Atlas Material.mat"),
    # 🆕 2026-10-10（`A1300`）：第三份。它与前两份**不同**：工程里原本**没有**它的图集与材质
    # （全工程 0 处引用它，所以 `EffectExporter` 从没顺带导过）⇒ 由本脚本按 `_ensure_side_assets()`
    # **落到 `OUT_DIR` 下**（白名单边界，见文件头）。
    ("RobotoCondensed-Regular SDF",
     "CardPresentation/Resources/Fonts/RobotoCondensed-Regular SDF Atlas.png",
     "CardPresentation/Resources/Fonts/RobotoCondensed-Regular Atlas Material.mat"),
]

OUT_DIR = "CardPresentation/Resources/Fonts"
#: `_ensure_side_assets()` **只准往这里写**（= 本会话白名单）。别的路径必须在盘上已存在。
OUT_DIR_PREFIX = OUT_DIR + "/"

#: 材质 `.mat` 的**骨架 + 键序**来源（本工程 Unity 自己写的）。
#: 出处：`EffectExporter.ImportMaterial` 当年就是照原版 `Material_<pathID>.json` 的
#: `m_SavedProperties` 逐键转写成它的 —— `_selftest()` 把这一步**重做一遍并与它逐字节比**。
MAT_TEMPLATE = ASSETS + "/WarpforgeVFX/Materials/Pragati-Regular Atlas Material.mat"
#: 图集 `.meta` 的模板（同样是 `EffectExporter.ImportTexture` 写的那一份）。
ATLAS_META_TEMPLATE = ASSETS + "/WarpforgeVFX/Textures/Pragati-Regular SDF Atlas.png.meta"
#: 导入的 PNG 在工程里的主对象 fileID / `.mat` 的主对象 fileID（Unity 的固定值）。
TEX_MAIN_FILEID = 2800000
MAT_MAIN_FILEID = 2100000

#: **头 10 个**字段（`MonoBehaviour` 基类字段，值固定，只有 `m_Name` 随件变）。
#: **正文那 42 个的顺序【不在这里写死】** —— `_template_order()` 现读
#: `NotoSerifCJK-Regular SDF.asset` 拿顺序，`_selftest()` 断言「模板正文的键集 == 原版 JSON 的键集」。
#: 理由：那份是**本工程这台 Unity 自己写出来的**，比我们手抄一张表可靠（手抄一定会漂）。
HEADER = [
    "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
    "m_GameObject", "m_Enabled", "m_EditorHideFlags", "m_Script", "m_Name",
    "m_EditorClassIdentifier",
]

# ─────────────────────────── YAML 输出 ───────────────────────────


class Ref(object):
    """一个对象引用。`pathid` 为 0 且无 guid ⇒ 内联 `{fileID: 0}`。"""
    __slots__ = ("fileID", "guid", "type", "foreign")

    def __init__(self, fileID, guid=None, type=None, foreign=False):
        self.fileID = fileID
        self.guid = guid
        self.type = type
        #: `m_FileID != 0` ⇒ 指的是**别的文件里的对象**（原版 JSON 里的跨文件引用）。
        #: 本脚本没有实现这种改写 ⇒ 只在**它真的被输出**时才报错（被覆盖掉的那些不算）。
        self.foreign = foreign

    def text(self):
        if self.foreign:
            raise SystemExit("⛔ 有一处 `m_FileID != 0` 的跨文件引用（pathID %s）没被覆盖掉 —— "
                             "本脚本没实现它，停手。" % self.fileID)
        if self.guid is None:
            return "{fileID: %d}" % self.fileID
        return "{fileID: %d, guid: %s, type: %d}" % (self.fileID, self.guid, self.type)


_BARE_OK = re.compile(r"^[A-Za-z0-9_./+\-()\[\]{} ]+$")


def _fmt_str(s):
    if s == "":
        return ""            # Unity 写空串 = 冒号后面什么都没有（带一个尾空格）
    if _BARE_OK.match(s) and not s.endswith(" ") and not s.startswith(" "):
        return s
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


def _fmt_float(v):
    """float32 的**最短往返表示**（Unity 就是这么写的：`50` / `0.5` / `-14.3`）。"""
    import numpy as np
    f = float(np.float32(v))
    if f == int(f) and abs(f) < 1e15:
        return str(int(f))
    return np.format_float_positional(np.float32(v), unique=True, fractional=False, trim="-")


def _fmt_scalar(v):
    if v is None:
        return ""
    if isinstance(v, Ref):
        return v.text()
    if isinstance(v, bool):
        return "1" if v else "0"
    if isinstance(v, float):
        return _fmt_float(v)
    if isinstance(v, int):
        return str(v)
    return _fmt_str(v if isinstance(v, str) else str(v))


def _emit(key, val, ind, out):
    p = " " * ind
    if isinstance(val, dict) and val:
        out.append("%s%s:" % (p, key))
        for k, v in val.items():
            _emit(k, v, ind + 2, out)
    elif isinstance(val, list) and val:
        out.append("%s%s:" % (p, key))
        for it in val:
            if isinstance(it, dict) and it:
                # Unity 的块序列：`- ` 与父键**同缩进**，条目内的键与 `- ` 之后的那一格**对齐**
                #（= 父键缩进 + 2）。例：
                #   `  m_FontWeightTable:` / `  - regularTypeface: {fileID: 0}` /
                #   `    italicTypeface: {fileID: 0}`
                sub = []
                for k, v in it.items():
                    _emit(k, v, ind + 2, sub)
                first = sub[0]
                assert first.startswith(" " * (ind + 2)), first
                out.append("%s- %s" % (p, first[ind + 2:]))
                out.extend(sub[1:])
            else:
                out.append("%s- %s" % (p, _fmt_scalar(it)))
    elif isinstance(val, list):
        out.append("%s%s: []" % (p, key))
    elif isinstance(val, dict):
        out.append("%s%s: {}" % (p, key))
    else:
        s = _fmt_scalar(val)
        out.append("%s%s:%s" % (p, key, (" " + s) if s != "" else " " if isinstance(val, str) else ""))


# ─────────────────────────── 引用改写 ───────────────────────────

def _refs_to_Ref(v):
    """把 `{m_FileID, m_PathID}` 递归换成 `Ref`。`m_FileID != 0`（跨文件）标成 `foreign`，
    **只在它真被输出时**才报错 —— 那几处（`m_Script` / `m_Material` / `m_AtlasTextures`）本脚本
    一律显式覆盖掉，所以它们不会走到输出。"""
    if isinstance(v, dict):
        if set(v.keys()) == {"m_FileID", "m_PathID"}:
            return Ref(v["m_PathID"], foreign=(v["m_FileID"] != 0))
        return {k: _refs_to_Ref(x) for k, x in v.items()}
    if isinstance(v, list):
        return [_refs_to_Ref(x) for x in v]
    return v


# ─────────────────────────── 主流程 ───────────────────────────

def _guid_of(rel_path):
    """读工程里某个资产的 `.meta` guid（读不到就**停手**，⛔ 不猜一个）。"""
    mp = os.path.join(ASSETS, rel_path) + ".meta"
    if not os.path.isfile(mp):
        raise SystemExit("⛔ 找不到 `.meta`：%s" % mp)
    t = io.open(mp, encoding="utf-8", errors="replace").read()
    m = re.search(r"^guid: ([0-9a-f]{32})", t, re.M)
    if not m:
        raise SystemExit("⛔ `%s` 里没有 guid 行" % mp)
    return m.group(1)


def font_guid(name):
    h = hashlib.md5(("warpforge:tmp-font-asset:" + name).encode("utf-8")).hexdigest()
    assert len(h) == 32
    return h


def atlas_guid(name):
    h = hashlib.md5(("warpforge:tmp-font-atlas:" + name).encode("utf-8")).hexdigest()
    assert len(h) == 32
    return h


def mat_guid(name):
    h = hashlib.md5(("warpforge:tmp-font-material:" + name).encode("utf-8")).hexdigest()
    assert len(h) == 32
    return h


def _derived_guids():
    """本脚本**派生出来**的全部 guid（字体 + 落在 `OUT_DIR` 里的图集/材质），带中文标签便于报错。"""
    out = []
    for n, tex, mat in FONTS:
        out.append(("字体 " + n, font_guid(n)))
        for label, rel, g in (("图集", tex, atlas_guid(n)), ("材质", mat, mat_guid(n))):
            if rel.startswith(OUT_DIR_PREFIX):     # 只有**本脚本产的**那两个才用派生 guid
                out.append(("%s %s" % (label, n), g))
    return out


# ───────────────────────── 侧件（图集 PNG / 材质 `.mat`）─────────────────────────

def _orig_material(name):
    """按**字体自己指的** `m_Material.m_PathID` 取原版材质 JSON（⛔ 不按名字猜）。

    顺带强断言：该材质的 `_MainTex` 必须就是**这份字体自己的图集**（两个原版 pathID 相等）
    —— 判据是原版的引用关系本身，⛔ 不是名字、也不是条目次序。
    """
    src = json.load(io.open(os.path.join(FONTS_BUNDLE, "MonoBehaviour", name + ".json"),
                           encoding="utf-8"))
    pid = src["m_Material"]["m_PathID"]
    p = os.path.join(FONTS_BUNDLE, "Material", "Material_%d.json" % pid)
    if not os.path.isfile(p):
        raise SystemExit("⛔ %s 指的材质 `Material_%d.json` 在原版包里没有" % (name, pid))
    m = json.load(io.open(p, encoding="utf-8"))
    ote = {kv[0]: kv[1] for kv in m["m_SavedProperties"]["m_TexEnvs"]}
    if "_MainTex" not in ote:
        raise SystemExit("⛔ `%s` 的材质没有 `_MainTex` 条目" % name)
    atlas_pid = src["m_AtlasTextures"][0]["m_PathID"]
    if ote["_MainTex"]["m_Texture"]["m_PathID"] != atlas_pid:
        raise SystemExit("⛔ `%s` 的材质 `_MainTex`（%s）不是这份字体的图集（%s）⇒ 对不上，停手"
                         % (name, ote["_MainTex"]["m_Texture"]["m_PathID"], atlas_pid))
    return src, m, pid


def _mat_template_shader_pid():
    """模板那份 `.mat` 当初对应的**原版 shader pathID**（= Pragati 那份材质的）。"""
    return _orig_material("Pragati-Regular SDF")[1]["m_Shader"]["m_PathID"]


def _mat_keys(tmpl):
    """从模板里读 `m_TexEnvs` / `m_Floats` / `m_Colors` 的**键序**（判据：Unity 自己写的文件）。"""
    def span(start, end):
        i, j = tmpl.index(start) + len(start), tmpl.index(end)
        if j < i:
            raise SystemExit("⛔ 材质模板里 `%s` 在 `%s` 之后，读不出块" % (start, end))
        return [ln for ln in tmpl[i:j].split("\n") if ln.startswith("    - ")]
    tex = span("    m_TexEnvs:\n", "\n    m_Ints: []")
    flo = span("    m_Floats:\n", "\n    m_Colors:")
    col = span("    m_Colors:\n", "\n  m_BuildTextureStacks:")
    return ([ln[6:].rstrip(":") for ln in tex],
            [ln[6:].rsplit(": ", 1)[0] for ln in flo],
            [ln[6:].split(": ", 1)[0] for ln in col])


def _emit_colors(c):
    return "{r: %s, g: %s, b: %s, a: %s}" % (_fmt_float(c["r"]), _fmt_float(c["g"]),
                                             _fmt_float(c["b"]), _fmt_float(c["a"]))


def _build_mat_text(name, tmpl, tex_rel, tex_guid_override):
    """用**模板的骨架** + **原版 JSON 的值**拼出这份字体的 `.mat` 文本。

    `tex_guid_override` 为 `None` 时用**工程里那份图集 `.meta` 的真实 guid**（= 复现 Pragati，
    拿来跟盘上的模板逐字节比）；给字符串时用那个 guid（= 生成 `RobotoCondensed` 那份）。
    """
    src, m, mat_pid = _orig_material(name)
    if m["m_Shader"]["m_PathID"] != _mat_template_shader_pid():
        raise SystemExit("⛔ `%s` 的材质 shader（%s）与模板那份（%s）不是同一个 ⇒ 模板骨架不适用，停手"
                         % (name, m["m_Shader"]["m_PathID"], _mat_template_shader_pid()))
    tex_keys, flo_keys, col_keys = _mat_keys(tmpl)
    ote = {kv[0]: kv[1] for kv in m["m_SavedProperties"]["m_TexEnvs"]}
    ofl = dict(m["m_SavedProperties"]["m_Floats"])
    oco = {kv[0]: kv[1] for kv in m["m_SavedProperties"]["m_Colors"]}
    if set(tex_keys) != set(ote) or set(flo_keys) != set(ofl) or set(col_keys) != set(oco):
        raise SystemExit("⛔ `%s` 的材质键集与模板不一致（tex %r / float %r / color %r）"
                         % (name, sorted(set(ote) - set(tex_keys)),
                            sorted(set(ofl) - set(flo_keys)), sorted(set(oco) - set(col_keys))))
    tex_guid = tex_guid_override or _guid_of(tex_rel)
    atlas_pid = src["m_AtlasTextures"][0]["m_PathID"]

    out = []
    for line in tmpl.split("\n"):
        out.append(line)

    def replace_block(lines, header, keys, render):
        i = lines.index(header) + 1
        j = i
        while j < len(lines) and (lines[j].startswith("    - ") or lines[j].startswith("        ")):
            j += 1
        return lines[:i] + render(keys) + lines[j:]

    def r_tex(keys):
        r = []
        for k in keys:
            e = ote[k]
            pid = e["m_Texture"]["m_PathID"]
            tex = ("{fileID: %d, guid: %s, type: 3}" % (TEX_MAIN_FILEID, tex_guid)
                   if pid == atlas_pid else "{fileID: 0}")
            r += ["    - %s:" % k, "        m_Texture: %s" % tex,
                  "        m_Scale: {x: %s, y: %s}" % (_fmt_float(e["m_Scale"]["x"]),
                                                     _fmt_float(e["m_Scale"]["y"])),
                  "        m_Offset: {x: %s, y: %s}" % (_fmt_float(e["m_Offset"]["x"]),
                                                      _fmt_float(e["m_Offset"]["y"]))]
        return r

    def r_flo(keys):
        return ["    - %s: %s" % (k, _fmt_float(ofl[k])) for k in keys]

    def r_col(keys):
        return ["    - %s: %s" % (k, _emit_colors(oco[k])) for k in keys]

    out = replace_block(out, "    m_TexEnvs:", tex_keys, r_tex)
    out = replace_block(out, "    m_Floats:", flo_keys, r_flo)
    out = replace_block(out, "    m_Colors:", col_keys, r_col)
    text, n = re.subn(r"(?m)^  m_Name: .*$", lambda _: "  m_Name: " + m["m_Name"],
                      "\n".join(out), count=1)
    if n != 1:                      # ⛔ 换不到就停手 —— 静默留着模板的名字 = 材质名错而没人知道
        raise SystemExit("⛔ 材质模板里找不到 `m_Name:` 那一行，换不了名")
    return text, m["m_Name"]


def _ensure_side_assets(write=True):
    """把 `OUT_DIR` 里那两份字体的**图集 PNG / 材质 `.mat`** 补上（缺才写；**只往 `OUT_DIR` 写**）。

    `write=False`（`--verify` 用）⇒ **只查不写**：缺就停手。
    """
    tmpl = io.open(MAT_TEMPLATE, encoding="utf-8").read()
    made = []
    for name, tex_rel, mat_rel in FONTS:
        tex_p = os.path.join(ASSETS, tex_rel)
        mat_p = os.path.join(ASSETS, mat_rel)
        for rel, p in ((tex_rel, tex_p), (mat_rel, mat_p)):
            if rel.startswith(OUT_DIR_PREFIX):
                if not write and (not os.path.isfile(p) or not os.path.isfile(p + ".meta")):
                    raise SystemExit("⛔ `--verify`：`%s` 还没生成（先跑一次不带参数的）" % rel)
                continue
            if not os.path.isfile(p) or not os.path.isfile(p + ".meta"):
                raise SystemExit("⛔ `%s` 不在 `OUT_DIR` 里、本脚本又不准往那儿写 ⇒ 它必须在盘上已存在"
                                 "（缺 %s）" % (rel, p))
        if write and not os.path.isfile(tex_p):
            raw = io.open(os.path.join(FONTS_BUNDLE, "Texture2D", name + " Atlas.png"), "rb").read()
            meta = io.open(ATLAS_META_TEMPLATE, encoding="utf-8").read()
            meta = re.sub(r"(?m)^guid: [0-9a-f]{32}$", "guid: " + atlas_guid(name), meta, count=1)
            assert meta.count(atlas_guid(name)) == 1
            with io.open(tex_p, "wb") as f:
                f.write(raw)
            with io.open(tex_p + ".meta", "wb") as f:
                f.write(meta.encode("utf-8"))
            made.append(os.path.relpath(tex_p, ASSETS))
        if write and not os.path.isfile(mat_p):
            text, mat_name = _build_mat_text(name, tmpl, tex_rel, atlas_guid(name))
            with io.open(mat_p, "wb") as f:
                f.write(text.encode("utf-8"))
            with io.open(mat_p + ".meta", "wb") as f:
                f.write(("fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n"
                         "  externalObjects: {}\n  mainObjectFileID: %d\n"
                         "  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
                         % (mat_guid(name), MAT_MAIN_FILEID)).encode("utf-8"))
            made.append(os.path.relpath(mat_p, ASSETS))
    if made:
        print("[side] 补了 %d 样：%s" % (len(made), " · ".join(made)))
    return made


def _template_order():
    """现读工程那份 Unity 写的 TMP_FontAsset，取顶层字段顺序（= 判据，⛔ 不自己编）。"""
    t = io.open(PROJ_TMP_ASSET, encoding="utf-8").read()
    i = t.index("--- !u!114 &11400000")
    blk = t[i:]
    order = []
    for m in re.finditer(r"(?m)^  ([A-Za-z_][A-Za-z0-9_]*):", blk):
        if m.group(1) not in order:
            order.append(m.group(1))
    return order


def _selftest(write=True):
    """全部前置断言。任一条不过就**停手**（⛔ 不猜着往下走）。"""
    _ensure_side_assets(write)   # 先把缺的图集/材质补进 `OUT_DIR`（否则下面的 `_guid_of` 会停手）
    tmpl = _template_order()
    src = json.load(io.open(os.path.join(FONTS_BUNDLE, "MonoBehaviour",
                                        "Pragati-Regular SDF.json"), encoding="utf-8"))
    # 原版 JSON 里有 4 个键（`m_GameObject` / `m_Enabled` / `m_Script` / `m_Name`）**本身就是
    # MonoBehaviour 头字段** ⇒ 判据是「头的 10 个 ∪ JSON 的 42 个 == 模板的 48 个」，两个集合相等。
    if set(HEADER) | set(src.keys()) != set(tmpl):
        raise SystemExit("⛔ 模板字段集 != (头 10 ∪ 原版 JSON 键集)：\n  模板多 = %r\n  JSON 多 = %r"
                         % (sorted(set(tmpl) - set(HEADER) - set(src)),
                            sorted(set(HEADER) | set(src) - set(tmpl))))
    if tmpl[:10] != HEADER:
        raise SystemExit("⛔ 模板的 10 个头字段与 `HEADER` 不一致：%r" % (tmpl[:10],))
    print("[selftest] 模板字段 %d 个 = 头 %d + 原版 JSON 键 %d，**逐一相等** [OK]"
          % (len(tmpl), len(HEADER), len(src)))
    # 字体图集像素逐位相同（工程那份 vs 原版那份）
    import numpy as np
    from PIL import Image
    for name, tex, _ in FONTS:
        a = np.array(Image.open(os.path.join(ASSETS, tex)).convert("RGBA")).astype(int)
        b = np.array(Image.open(os.path.join(FONTS_BUNDLE, "Texture2D",
                                             name + " Atlas.png")).convert("RGBA")).astype(int)
        if a.shape != b.shape or np.abs(a - b).max() != 0:
            raise SystemExit("⛔ `%s` 的图集像素与工程那份不一致（%r vs %r）" % (name, a.shape, b.shape))
    # 材质转写规则**先自证**（拿 Unity 自己写的那份当期望值）：照本脚本的规则从 Pragati 的
    # 原版材质 JSON 重建，必须与盘上那份**逐字节**相同 —— 只有 `m_Name` 与 `_MainTex` 的 guid 会被换，
    # 而 Pragati 这两处本来就一样 ⇒ 应当**一字不差**。这条过了，`RobotoCondensed` 那份才敢照做。
    tmpl_txt = io.open(MAT_TEMPLATE, encoding="utf-8").read()
    rebuilt, _ = _build_mat_text("Pragati-Regular SDF", tmpl_txt,
                                 "WarpforgeVFX/Textures/Pragati-Regular SDF Atlas.png",
                                 _guid_of("WarpforgeVFX/Textures/Pragati-Regular SDF Atlas.png"))
    if rebuilt != tmpl_txt:
        raise SystemExit("⛔ 照本脚本规则重建的 Pragati 材质 **!=** 盘上模板（逐字节）⇒ 转写规则漂了，停手")
    print("[selftest] 材质转写规则：重建 Pragati 那份 == 盘上模板 **逐字节相同** [OK]")
    # guid 唯一：⚠️ **要排除本脚本自己的产物**（第二次跑时它们已经在盘上了 —— 那是「自己撞自己」，
    # 不是真冲突）。只跟**别的** .meta 比。
    own = set()
    for n, tex, mat in FONTS:
        own.add(os.path.normcase(os.path.join(ASSETS, OUT_DIR, n + ".asset.meta")))
        for rel in (tex, mat):                      # 落在 `OUT_DIR` 里的侧件也是**本脚本的产物**
            if rel.startswith(OUT_DIR_PREFIX):
                own.add(os.path.normcase(os.path.join(ASSETS, rel + ".meta")))
    have = set()
    for r, d, fs in os.walk(ASSETS):
        for f in fs:
            if not f.endswith(".meta"):
                continue
            p = os.path.join(r, f)
            if os.path.normcase(p) in own:
                continue
            try:
                t = io.open(p, encoding="utf-8", errors="replace").read(400)
            except Exception:
                continue
            m = re.search(r"^guid: ([0-9a-f]{32})", t, re.M)
            if m:
                have.add(m.group(1))
    for label, g in _derived_guids():
        if g in have:
            raise SystemExit("[STOP] 派生出来的 guid `%s`（%s）在本工程【别的 .meta】里**已存在** "
                             "⇒ 换一个派生串" % (g, label))
    print("[selftest] %d 份图集「工程 vs 原版」像素逐位相同 [OK] · 派生 guid（%d 个）与工程既有 %d 个 "
          "guid **不冲突** [OK]" % (len(FONTS), len(_derived_guids()), len(have)))
    return tmpl


def build_one(name, tex_rel, mat_rel, order):
    src = json.load(io.open(os.path.join(FONTS_BUNDLE, "MonoBehaviour", name + ".json"),
                           encoding="utf-8"))
    src = _refs_to_Ref(src)
    tex_guid = _guid_of(tex_rel)
    mat_g = _guid_of(mat_rel)          # ⚠️ 别起名 `mat_guid`：那是本模块的派生函数，会遮蔽它
    fam = name + ".asset"
    out = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:",
           "--- !u!114 &11400000", "MonoBehaviour:"]
    values = {
        "m_ObjectHideFlags": 0,
        "m_CorrespondingSourceObject": Ref(0),
        "m_PrefabInstance": Ref(0),
        "m_PrefabAsset": Ref(0),
        "m_GameObject": Ref(0),
        "m_Enabled": 1,
        "m_EditorHideFlags": 0,
        "m_Script": Ref(11500000, TMP_FONTASSET_SCRIPT_GUID, 3),
        "m_Name": name,
        "m_EditorClassIdentifier": "Unity.TextMeshPro::TMPro.TMP_FontAsset",
    }
    for k in order:
        if k in values:
            continue
        if k not in src:
            raise SystemExit("⛔ `%s` 在 `%s` 里没有" % (k, name))
        values[k] = src[k]
    # 三处引用改写（其余 `{m_FileID:0,m_PathID:0}` 已在 `_refs_to_Ref` 里变成 `Ref(0)`）
    values["m_Material"] = Ref(2100000, mat_g, 2)             # `.mat` 的主对象 = 2100000
    values["m_AtlasTextures"] = [Ref(2800000, tex_guid, 3)]   # 导入的 PNG 的主对象 = 2800000
    for w in values.get("m_FontWeightTable") or []:
        w["regularTypeface"] = Ref(0)
        w["italicTypeface"] = Ref(0)
    if values.get("atlas") is not None:
        values["atlas"] = Ref(0)
    # `m_FaceInfo` 等子字典里的引用（本件没有）也统一成 Ref(0)
    for k in order:
        if k in ("m_FontWeightTable",):
            continue
        values[k] = _refs_to_Ref(values[k])

    for k in order:
        _emit(k, values[k], 2, out)
    text = "\n".join(out) + "\n"
    dst = os.path.join(ASSETS, OUT_DIR, fam)
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    with io.open(dst, "wb") as f:                       # ⛔ 二进制写：行尾恒为 LF（见 CLAUDE.md §二）
        f.write(text.encode("utf-8"))
    mp = dst + ".meta"
    with io.open(mp, "wb") as f:
        f.write(("fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n"
                 "  externalObjects: {}\n  mainObjectFileID: 11400000\n"
                 "  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
                 % font_guid(name)).encode("utf-8"))
    return dst, len(src["m_GlyphTable"]), len(src["m_CharacterTable"]), len(out)


def verify(order):
    ok = True
    for name, tex_rel, mat_rel in FONTS:
        p = os.path.join(ASSETS, OUT_DIR, name + ".asset")
        if not os.path.isfile(p):
            print("✗ 缺 %s" % p); ok = False; continue
        t = io.open(p, encoding="utf-8").read()
        keys = [m.group(1) for m in re.finditer(r"(?m)^  ([A-Za-z_][A-Za-z0-9_]*):", t)]
        miss = [k for k in order if k not in keys]
        extra = [k for k in keys if k not in order]
        src = json.load(io.open(os.path.join(FONTS_BUNDLE, "MonoBehaviour", name + ".json"),
                               encoding="utf-8"))
        n_g = t.count("\n  - m_Index: ") if False else len(re.findall(r"(?m)^  - m_Index: ", t))
        crlf = t.count("\r\n")
        n0 = t.count("guid: " + "0" * 32)
        # 引用的两样（图集/材质）在工程里必须真存在、且 `.asset` 指的就是它们（重读产物、⛔ 不看生成器内部）
        tg = re.search(r"m_AtlasTextures:\n  - \{fileID: %d, guid: ([0-9a-f]{32}), type: 3\}"
                       % TEX_MAIN_FILEID, t)
        mg = re.search(r"m_Material: \{fileID: %d, guid: ([0-9a-f]{32}), type: 2\}" % MAT_MAIN_FILEID, t)
        refbad = []
        for what, got, rel in (("图集", tg, tex_rel), ("材质", mg, mat_rel)):
            if not got or got.group(1) != _guid_of(rel) or not os.path.isfile(os.path.join(ASSETS, rel)):
                refbad.append(what)
        pop = re.search(r"(?m)^  m_AtlasPopulationMode: (\d+)", t)
        src_pop = int(src["m_AtlasPopulationMode"])
        print("%s 顶层字段 %d/%d %s · 多余 %r · 字形条目 %d（原版 %d）· guid0 %d · CRLF %d"
              " · popMode %s（原版 %d）· 引用[图集/材质] %s"
              % (name, len(order) - len(miss), len(order), "OK" if not miss else "缺 " + str(miss),
                 extra, n_g, len(src["m_GlyphTable"]), n0, crlf,
                 pop.group(1) if pop else "?", src_pop,
                 "OK" if not refbad else "✗ " + "/".join(refbad)))
        if (miss or extra or n_g != len(src["m_GlyphTable"]) or crlf or n0 or refbad
                or not pop or int(pop.group(1)) != src_pop):
            ok = False
    return ok


def main():
    order = None
    if "--verify" in sys.argv:
        # verify 也要先用模板核对一遍字段集（⚠️ `write=False`：只查不写）
        order = _selftest(write=False)
        print("--- verify ---")
        sys.exit(0 if verify(order) else 1)
    order = _selftest()
    print()
    for name, tex_rel, mat_rel in FONTS:
        dst, ng, nc, nln = build_one(name, tex_rel, mat_rel, order)
        print("✓ %-22s → %s（%d 行 · 字形 %d · 字符 %d）" % (name, os.path.relpath(dst, ASSETS), nln, ng, nc))
    print()
    verify(order)


if __name__ == "__main__":
    main()
