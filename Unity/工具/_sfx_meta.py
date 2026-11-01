#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""_sfx_meta.py — 三个音频导入脚本**共用**的「补 Unity `.meta`」实现。

为什么单独一个模块（而不是在三个脚本里各抄一份）
--------------------------------------------------
`import_original_sfx.py` / `import_battle_sfx.py` / `import_remnant_sfx.py` 都会把原版音频
拷进 `Resources/Art/audio/sfx/`，而它们原来**只拷 wav、不写 `.meta`**。工程里没有
`AudioImporter` preset（`ProjectSettings/PresetManager.asset` 的 `m_DefaultPresets: {}`、
`Assets/**` 下 0 个 `.preset`）⇒ 手工补进来的那一条会被 Unity 用**内置默认**导入
（`loadType: 0` DecompressOnLoad / PCM / preload 1），与同目录既有的 **510 条 `.wav.meta`**
（`loadType: 1` CompressedInMemory · `compressionFormat: 2` · `preloadAudioData: 0`）**不一致** ——
同一条音在不同卡上会有不同的加载方式与内存占用，而且**没有任何报错**。

单独成模块的理由：① 不重复三份（改一次三处生效）；② 不依赖 `UnityPy`（三个脚本都在文件头
`import UnityPy`，把它当「共用件」会让只想补 meta 的一次性活也得先装 UnityPy）；
③ 与 `_guid_resolve_probe.py` 同一命名/用法惯例（下划线开头 = 工具内部共用件）。

口径（四条，别改）
------------------
1. **模板 = 既有兄弟文件的众数**（**同一目录 + 同一扩展名**），**逐字节照抄**，只换 `guid`。
   ⛔ 不手写模板、⛔ 不「按理解」拼字段。实测同目录 `.wav.meta` 与 `.ogg.meta` 本来就不是
   同一套设置（`sfx/*.ogg.meta` 是 `loadType: 2` Streaming / `compressionFormat: 1` / `preload 1`），
   而 `vo/*.ogg.meta` 又是 `loadType: 1 / 1 / 0` ⇒ **「一套模板走天下」会把 ogg 写错**。
2. **已存在就不覆盖**（`ensure_meta()` 直接返回 `exists`）—— 既有 guid 一旦被换掉，
   引用它的场景 / prefab / 资产全部断链，而且**不会有任何报错**。
3. **guid 全局唯一**：写前扫全工程 `*.meta`（`d:/4/Unity`，2026-10-24 实测 **24,074 份、0 重复**）
   的 `guid:`，撞了就重摇。格式恒为 **32 位小写 hex**（Unity 自己的写法）。
4. 🔴🆕 **那 4 项按【原版资产真值】写，不按众数**（活状态表 `B-92`，2026-11-01）：
   `loadType` / `compressionFormat` / `preloadAudioData` / `loadInBackground` ——
   原版真值来自 `soundcollection_assets_all.bundle` 的 **618 条 AudioClip typetree**
   （`m_LoadType` / `m_CompressionFormat` / `m_PreloadAudioData` / `m_LoadInBackground`，`B111_T2` 取得）。
   **理由**：兄弟众数只是「我们这条目录里大多数长什么样」，**不是原版设置**。
   实证：原版有 **21 条打击音**（`Hit Fire 1/2/3` · `Hit Rock 1/2/3` · `Hit Squishy 1` …）
   `m_LoadInBackground = false`，而 `.wav` 模板写死 `loadInBackground: 1` ⇒ 新加一条 `Hit X.wav`
   就又拿不到那条 `0`（`B111_T2` 手工修掉的 22 处会被重新造出来）。
   · **名单里没有的文件名** ⇒ ⛔ **不硬套 BASE**（那等于给没查过的音编数字），沿用兄弟模板并**出声**；
   · **取不到真值的那 3 项**（`normalize` / `quality` / `sampleRateOverride`）⇒ 保持模板值并**出声**。
     🔶 这三项是**我们自己定的**：原版 `AudioClip` typetree 里没有这些字段（`AudioImporter` 是编辑器侧
     设置、构建时被消费掉、不进包体），**本地任何数据源都给不出逐条真值**（`B111_T2` §B-25 已查三处）。
     （`sampleRateOverride: 44100` 还是**死值** —— 只有 `sampleRateSetting: 2` 时 Unity 才看它，
     我们全是 `0` = Preserve。）

用法
----
    # ① 三个导入脚本内部：拷完一条就 `ensure_meta(path)`（已存在的直接跳过，幂等）
    # ② **只补 meta、不重拷 wav**（幂等、可反复跑；这就是「已导入过的再过一遍」那条路）：
    python -I -X utf8 工具/_sfx_meta.py --backfill            # 真写
    python -I -X utf8 工具/_sfx_meta.py --backfill --dry-run  # 只数
    # ③ 一次性校验（只读）：配对 / guid 全局唯一 + 格式 / **4 项是否等于原版真值**
    python -I -X utf8 工具/_sfx_meta.py --verify
    # ④ 离线自证（只读，零写盘、不需要 UnityPy）：真值文件完整性 + 模板改写管线 + 覆盖率 + 兜底 normalize
    python -I -X utf8 工具/_sfx_meta.py --self-test   # 或单独跑 --check-fallback [--deep]（B-238：兜底↔众数↔冷重建）
    # ⑤ 看一眼「某条新音会被写成什么」（只读）
    python -I -X utf8 工具/_sfx_meta.py --probe "Hit Fire 1"
    # ⑥ 真值文件 ↔ 原版 bundle 本体 逐条比（只读，**需要 UnityPy**；`--apply` 才重写）
    python -I -X utf8 工具/_sfx_meta.py --scan-truth
    python -I -X utf8 工具/_sfx_meta.py --scan-truth --apply

真值文件 `_sfx_audio_truth.json` 是怎么来的
------------------------------------------
**机生成**，来源 = 原版 **30 个含 AudioClip 的 bundle** 的 typetree
（`m_LoadType` / `m_CompressionFormat` / `m_PreloadAudioData` / `m_LoadInBackground`）：
`{"clips": {"<clip 名>": [loadType, compressionFormat, preloadAudioData, loadInBackground]}, "_meta": {...}}`。
2026-11-01 实扫：**2,550 条对象 / 2,549 个去重名字 / 跨 bundle 冲突 0 条**（同名 clip 在多个包里 4 项一致，
所以「名字」可以当键）；我们自己的 **2,494 / 2,494** 条音频名都查得到真值
（靠 4 条桥接，均已逐条核过、不是规则：**2 条原版名带尾随空格** `'Hero of the Empire '` / `'Tau Summon '`，
**2 条命名风格不同** `main_theme` ↔ `Main Theme` / `menu_idle_theme` ↔ `Menu Idle Theme`）。
再生成 = `--scan-truth --apply`（先备份到 `_tmp_view/sfx_truth_backup/`；有冲突或扫到 0 条则**拒绝写**）。
⛔ 别手改那个 JSON —— 重跑会覆盖。

`_meta.constant_fields`（`{"3D": 1, "ambisonic": 0}`）= 原版 typetree 里 **`m_Legacy3D` / `m_Ambisonic`**，
全库 2,549 条**恒定**。🔴 就地订正 `B111_T2` §B-25 把 `3D` 归进「本地没有真值」那档 —— **不成立**。
⚠️ 但**只用于 `--verify` 报黄（只报不改）**，⛔ 不写进任何 `.meta`（口径是只做那 4 项）。
"""
import argparse
import collections
import io
import json
import os
import re
import shutil
import sys
import time
import uuid

PROJECT_ROOT = "d:/4/Unity"
SFX_DIR = PROJECT_ROOT + "/MyGame/Assets/CardPresentation/Resources/Art/audio/sfx"

# 认作「音频」的扩展名（小写比较）
AUDIO_EXTS = (".wav", ".ogg", ".mp3", ".aif", ".aiff", ".flac")

# `guid:` 那一行。`(?P<eol>\r?)` 把行尾吃掉再原样写回 ⇒ **CRLF 文件不会被翻成 LF**。
GUID_LINE_RE = re.compile(rb"^guid: (?P<guid>[0-9a-fA-F]{32})(?P<eol>\r?)$", re.M)

# 内置兜底模板（**只在**目标目录一条同扩展名兄弟 meta 都没有时才用；有新目录时请先让 Unity
# 生成一条、或手工补一条，别指望这里）。2026-10-24 抄自当时 `audio/sfx/` 的众数（`.wav` 510/511 · `.ogg` 5/5）；
# **那 4 项现在按原版真值改写**（口径 4）⇒ 这里只剩非真值字段（`normalize`/`quality`/`sampleRateOverride`/结构行）起作用。
# 🔴 `normalize` **必须 = 现读全树众数 `0`**（`B-238` 2026-11-01，属 `UNPROVABLE`、原版无真值）：写成 `1` ⇒「目录删空再冷重建」会把 2,494 份从 0 漂到 1；逐字段机检 = `--check-fallback`。
# ⚠️ 行尾 LF、`userData:` / `assetBundleName:` / `assetBundleVariant:` 后面**各有一个尾随空格**
#    （Unity 自己就这么写的，别「顺手删掉」）。
FALLBACK_TEMPLATES = {
    ".wav": (
        b"fileFormatVersion: 2\n"
        b"guid: 00000000000000000000000000000000\n"
        b"AudioImporter:\n"
        b"  externalObjects: {}\n"
        b"  serializedVersion: 8\n"
        b"  defaultSettings:\n"
        b"    serializedVersion: 2\n"
        b"    loadType: 1\n"
        b"    sampleRateSetting: 0\n"
        b"    sampleRateOverride: 44100\n"
        b"    compressionFormat: 2\n"
        b"    quality: 1\n"
        b"    conversionMode: 0\n"
        b"    preloadAudioData: 0\n"
        b"  platformSettingOverrides: {}\n"
        b"  forceToMono: 0\n"
        b"  normalize: 0\n"
        b"  loadInBackground: 1\n"
        b"  ambisonic: 0\n"
        b"  3D: 1\n"
        b"  userData: \n"
        b"  assetBundleName: \n"
        b"  assetBundleVariant: \n"
    ),
    ".ogg": (
        b"fileFormatVersion: 2\n"
        b"guid: 00000000000000000000000000000000\n"
        b"AudioImporter:\n"
        b"  externalObjects: {}\n"
        b"  serializedVersion: 8\n"
        b"  defaultSettings:\n"
        b"    serializedVersion: 2\n"
        b"    loadType: 2\n"
        b"    sampleRateSetting: 0\n"
        b"    sampleRateOverride: 44100\n"
        b"    compressionFormat: 1\n"
        b"    quality: 1\n"
        b"    conversionMode: 0\n"
        b"    preloadAudioData: 1\n"
        b"  platformSettingOverrides: {}\n"
        b"  forceToMono: 0\n"
        b"  normalize: 0\n"
        b"  loadInBackground: 1\n"
        b"  ambisonic: 0\n"
        b"  3D: 1\n"
        b"  userData: \n"
        b"  assetBundleName: \n"
        b"  assetBundleVariant: \n"
    ),
}

# ============================================================================
# 原版音频导入设置的**真值表**（活状态表 `B-92`）
# ============================================================================
# 来源 = 原版 `bundle_soundcollection_assets_all` 里 **618 条 AudioClip 的 typetree**
#        （`m_LoadType` / `m_CompressionFormat` / `m_PreloadAudioData` / `m_LoadInBackground`），
#        `B111_T2` 取的，落在 `_tmp_view/b111_t2/audioclip_asset_truth.json`。
# 结构 = 「618 个名字的总名单」+「三组例外」。三组之外的名单内名字一律用 BASE。
# ⚠️ 名单里**没有**的文件名 ⇒ 返回 None（⛔ 不拿 BASE 硬套：那等于给没查过的音编数字）。
# ⚠️ 本表只覆盖 `soundcollection` 这 1 个 bundle；原版另外 20 个带音频的 bundle 还没取过
#    （`B111_T2` 顺手发现 5）⇒ 那些名字落进「没有真值」那一档并出声，**不是**「原版就是 BASE」。
# 再生成：见模块 docstring「内嵌真值表的再生成」。
# ============================================================================
# 原版音频导入设置的**真值表**（活状态表 `B-92`）
# ============================================================================
# 真值文件 = `工具/_sfx_audio_truth.json`：
#     {"clips": {<clip 名>: [loadType, compressionFormat, preloadAudioData, loadInBackground]}, ...}
# 来源 = 原版 **30 个含 AudioClip 的 bundle** 的 typetree
#        （`m_LoadType` / `m_CompressionFormat` / `m_PreloadAudioData` / `m_LoadInBackground`），
#        2026-11-01 实扫：**2,550 条对象 / 2,549 个去重名字 / 跨 bundle 冲突 0 条**
#        （同名 clip 在多个包里 4 项完全一致，逐条核过才敢去重）。
#        我们自己的 2,494 / 2,494 条音频名都查得到真值（4 条桥接：2 条原版名带尾随空格 +
#        2 条命名风格不同，见 `NAME_ALIASES` / `_truth_index()`，均已逐条核过）。
# ⚠️ 名单里**没有**的文件名 ⇒ `original_prov()` 返回 `None`，⛔ **不拿多数值硬套**
#    （那等于给没查过的音编数字；`18·0` 只允许在服务器相关的场合自己设计）。
# 再生成 / 复核（**只读 d:/2，不写 d:/2**）：
#     python -I -X utf8 工具/_sfx_meta.py --scan-truth            # 只扫只比，不写
#     python -I -X utf8 工具/_sfx_meta.py --scan-truth --apply    # 重写真值文件（先备份）
TRUTH_JSON = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_sfx_audio_truth.json")
BUNDLE_DIR = "D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"

_TRUTH = None
_TRUTH_INDEX = None
_TRUTH_CONST = {}

# 🆕 **只报不改**的两项（2026-11-01 订正 `B111_T2` §B-25「`3D` 本地没有真值」→ **不成立**）：
#   typetree 里就有 `m_Legacy3D` / `m_Ambisonic`，全库 2,549 条**恒为 1 / 0**。
#   简报口径只做那 4 项 ⇒ 这两项**不参与写盘**，只在 `--verify` 里报黄（口径完整、又不越权改资产）。
VERIFY_ONLY_FIELDS = ("3D", "ambisonic")
_CONST_FIELD_RE = {f: re.compile(rb"(?m)^(?P<ind>[ \t]*" + f.encode("ascii")
                                 + rb":(?P<sp>[ \t]*))(?P<val>[0-9]+)(?P<eol>\r?)$")
                   for f in VERIFY_ONLY_FIELDS}

# 我们工程里的文件名 ↔ 原版 clip 名（命名风格不同，**只有这 2 条**）。
# 逐条核过：两份 `.meta` 的那 4 项与原版真值**完全一致**（2026-11-01，见 `报告/B112_C.md`）。
# ⛔ 别把它推广成「空格换下划线」之类的规则 —— 只登记核过的。
NAME_ALIASES = {
    "main_theme": "Main Theme",
    "menu_idle_theme": "Menu Idle Theme",
}


def load_truth(path=None):
    """读真值表（进程内缓存一次）→ `{clip 名（**原样，可能带尾随空格**）: {4 项}}`。

    顺带把 `_meta.constant_fields`（`{"3D": 1, "ambisonic": 0}`）读进 `_TRUTH_CONST`。
    文件缺失 / 坏 ⇒ 返回 `{}` 并**出声**（⛔ 不静默退回「照抄众数」还装作没事）。"""
    global _TRUTH, _TRUTH_CONST
    if path is None and _TRUTH is not None:
        return _TRUTH
    p = path or TRUTH_JSON
    out = {}
    const = {}
    try:
        doc = json.loads(io.open(p, "r", encoding="utf-8").read())
        clips = doc.get("clips") if isinstance(doc, dict) and "clips" in doc else doc
        for k, v in clips.items():
            if isinstance(v, dict):
                out[k] = {f: int(v[f]) for f in PROV_FIELDS}
            else:
                out[k] = dict(zip(PROV_FIELDS, (int(x) for x in v)))
        if isinstance(doc, dict):
            cf = (doc.get("_meta") or {}).get("constant_fields") or {}
            const = {f: int(cf[f]) for f in VERIFY_ONLY_FIELDS if f in cf}
    except (OSError, ValueError, KeyError, TypeError) as e:
        sys.stderr.write(u"[sfx_meta] ⚠️ 读不了原版真值表 %s：%s ⇒ 这 4 项只能沿用模板（**未被证明**）\n"
                         % (p, e))
        out = {}
        const = {}
    if path is None:
        _TRUTH = out
        _TRUTH_CONST = const
    return out


def truth_constants():
    """真值表里「全库恒定」的两项：`3D` ← `m_Legacy3D`、`ambisonic` ← `m_Ambisonic`。

    ⚠️ **只用于 `--verify` 报黄，⛔ 不写进任何 `.meta`**（简报口径只做那 4 项）。"""
    load_truth()
    return _TRUTH_CONST


def _truth_index():
    """`name.strip()` → 4 项（去掉首尾空白的索引）。

    🔴 **为什么需要**：原版有 **2 条 clip 名带尾随空格** —— `'Hero of the Empire '` / `'Tau Summon '`
    （2026-11-01 `--scan-truth` 实扫，全表只有这 2 条；去空白之后**没有任何碰撞**）。
    我们工程里的同名文件没有那个空格 ⇒ 精确匹配会**静默判成「没有真值」**。
    ⇒ 精确匹配（`original_prov` 先试原样）**, 再退到去空白索引**；两条值若打架就**出声**并取第一条。"""
    global _TRUTH_INDEX
    if _TRUTH_INDEX is None:
        idx = {}
        for k, v in load_truth().items():
            s = k.strip()
            if s in idx and idx[s] != v:
                sys.stderr.write(u"[sfx_meta] ⚠️ 真值表里去空白后撞名且值不同：%r ⇒ 取先出现的那条\n" % s)
                continue
            idx.setdefault(s, v)
        _TRUTH_INDEX = idx
    return _TRUTH_INDEX


_KNOWN_GUIDS = None          # 进程内缓存（全工程扫一次 ~1.8 s，别每条音频都扫）


def render_meta(template, guid):
    """把模板里的 `guid:` 那一行换成 `guid`；**其余逐字节保留**（含行尾 CR/LF 与尾随空格）。

    ⚠️ 用 `subn` 而不是字符串 `replace`：`guid` 只该出现一次，出现 0 次或多次都说明模板不认识
    ⇒ **报错**，别猜一个写法写下去。"""
    def _sub(m):
        return b"guid: " + guid.encode("ascii") + m.group("eol")

    out, n = GUID_LINE_RE.subn(_sub, template, count=1)
    if n != 1:
        raise ValueError("模板里认不出唯一的 `guid:` 行 —— 拒绝猜一个写法")
    return out


# 原版有声明的 4 项（顺序 = 真值文件里那 4 个数的顺序）
PROV_FIELDS = ("loadType", "compressionFormat", "preloadAudioData", "loadInBackground")

# 🔶 自己设计（非原版）：normalize / quality / sampleRateOverride 三项没有原版真值 ——
#    `AudioImporter` 是**编辑器侧**设置，构建时被消费掉、**不进包体**；原版 `AudioClip` typetree 里
#    根本没有这三个字段（`B111_T2` §B-25 查了 `assets_full` / AssetRipper 导出 / bundle typetree 三处）。
#    ⇒ 只能沿用兄弟模板里的值，**并每次新建时出声**。⛔ 别拿它当「原版就是这样」。
UNPROVABLE_FIELDS = ("normalize", "quality", "sampleRateOverride")

_PROV_FIELD_RE = {f: re.compile(rb"(?m)^(?P<ind>[ \t]*" + f.encode("ascii") + rb":(?P<sp>[ \t]*))(?P<val>[0-9]+)(?P<eol>\r?)$")
                  for f in PROV_FIELDS}


def original_prov(name):
    """→ 原版那 4 项的真值 dict，或 `None`（这个名字不在原版真值表里）。

    先精确匹配；再退到**去首尾空白**的索引（原版有 2 条 clip 名带尾随空格，见 `_truth_index()`)。
    ⚠️ `None` **不等于**「原版的默认值就是模板里那些」—— 是「**我们没查到**」
    （铁律 2：没查到就说没查到）。"""
    t = load_truth()
    if name in t:
        return t[name]
    alias = NAME_ALIASES.get(name)
    if alias is not None and alias in t:
        return t[alias]
    return _truth_index().get(name.strip())


def apply_original_prov(template, name):
    """把模板里那 4 项改写成**原版真值**。→ `(新字节, prov dict 或 None)`

    名字没真值 ⇒ **原样返回**模板（`prov=None`）；⛔ 不按 BASE 硬套。
    模板里某项的行认不出唯一一条 ⇒ 抛 `ValueError`（⛔ 别猜一个写法写下去）。"""
    prov = original_prov(name)
    if prov is None:
        return template, None
    out = template
    for f in PROV_FIELDS:
        val = str(prov[f]).encode("ascii")
        pat = _PROV_FIELD_RE[f]

        def _sub(m, _v=val):
            return m.group("ind") + _v + m.group("eol")

        out, n = pat.subn(_sub, out, count=1)
        if n != 1:
            raise ValueError("模板里 `%s:` 行出现 %d 次（应 1 次）—— 拒绝猜一个写法" % (f, n))
    return out, prov


def read_prov_fields(meta_bytes):
    """从一份 `.meta` 里读回那 4 项 → dict（缺项记 None）。用于校验，不写盘。"""
    out = {}
    for f in PROV_FIELDS:
        m = _PROV_FIELD_RE[f].search(meta_bytes)
        out[f] = int(m.group("val")) if m else None
    return out


def _prov_note(fname, prov):
    """新建一条 meta 时打到 stderr 的**出声**。"""
    if prov is None:
        return (u"[sfx_meta] ⚠️ %s.meta 新建：原版真值表里**没有**这个文件名 ⇒ "
                u"loadType/compressionFormat/preloadAudioData/loadInBackground 沿用兄弟模板"
                u"（**未被证明是原版值**）；" % fname)
    return (u"[sfx_meta] %s.meta 新建：4 项 = 原版真值（%s）；" % (
        fname, u" ".join(u"%s=%d" % (f, prov[f]) for f in PROV_FIELDS)))


def _unprov_note():
    return (u"🔶 自己设计（非原版）：%s 三项本地无原版真值（B111_T2 §B-25 已证），"
            u"沿用模板值" % u"/".join(UNPROVABLE_FIELDS))


def _iter_guid_lines(root):
    """→ `(guid, meta 路径)`；只读每个 meta 的前 4 KB（guid 在第 2 行，够用）。"""
    for dirpath, _dirnames, filenames in os.walk(root):
        for f in filenames:
            if not f.endswith(".meta"):
                continue
            p = os.path.join(dirpath, f)
            try:
                head = io.open(p, "rb").read(4096)
            except OSError:
                continue
            m = GUID_LINE_RE.search(head)
            if m:
                yield m.group("guid").decode("ascii").lower(), p


def known_guids(root=PROJECT_ROOT):
    """全工程既有 guid 集合（进程内缓存一次）。写 meta 前用它防碰撞。"""
    global _KNOWN_GUIDS
    if _KNOWN_GUIDS is None:
        _KNOWN_GUIDS = {g for g, _p in _iter_guid_lines(root)}
    return _KNOWN_GUIDS


def new_guid(existing=None):
    """摇一个**没被用过**的 guid（32 位小写 hex），并当场登记进 `existing`。"""
    if existing is None:
        existing = known_guids()
    while True:
        g = uuid.uuid4().hex
        if g not in existing:
            existing.add(g)
            return g


def pick_template(dirpath, ext):
    """→ `(模板字节, 来源 meta 路径 或 None, 票数)`。

    在同一目录里按 **`<ext>.meta`** 的「去掉 guid 后的整篇字节」投票，取众数。
    票数 0（目录里一条同扩展名兄弟都没有）⇒ 用 `FALLBACK_TEMPLATES[ext]`；再没有就报错。
    ⛔ 别跨目录借模板（`vo/*.ogg.meta` 与 `sfx/*.ogg.meta` 的设置本来就不同）。"""
    ext = ext.lower()
    if not ext.startswith("."):
        ext = "." + ext
    suffix = ext + ".meta"
    votes = collections.Counter()
    sample = {}
    if os.path.isdir(dirpath):
        for f in sorted(os.listdir(dirpath)):
            if not f.lower().endswith(suffix):
                continue
            p = os.path.join(dirpath, f)
            try:
                b = io.open(p, "rb").read()
            except OSError:
                continue
            if b"AudioImporter:" not in b:
                continue
            try:
                norm = render_meta(b, "0" * 32)
            except ValueError:
                continue
            votes[norm] += 1
            sample.setdefault(norm, p)
    if votes:
        norm, n = votes.most_common(1)[0]
        return norm, sample[norm], n
    fb = FALLBACK_TEMPLATES.get(ext)
    if fb is None:
        raise ValueError(f"{dirpath} 下没有 `{suffix}` 兄弟，且没有内置模板 ⇒ 拒绝凭空造一份")
    return fb, None, 0


def ensure_meta(path, guids=None, dry_run=False, quiet=False):
    """给音频文件 `path` 补 `<path>.meta`。→ `"exists"` / `"created"` / `"would-create"`

    🔴 **已存在 ⇒ 一个字节都不碰**（返回 `"exists"`）—— 这是幂等路径的核心。
    🆕 那 4 项按**原版真值**写（见模块 docstring 口径 4）；没有真值 ⇒ 沿用模板并出声。"""
    meta = path + ".meta"
    if os.path.exists(meta):
        return "exists"
    ext = os.path.splitext(path)[1].lower()
    stem = os.path.splitext(os.path.basename(path))[0]
    template, _src, _n = pick_template(os.path.dirname(path) or ".", ext)
    template, prov = apply_original_prov(template, stem)
    data = render_meta(template, new_guid(guids))
    if not quiet:
        # 出声：哪些字段是原版真值、哪些是我们没法证明的（⛔ 不静默）
        sys.stderr.write(_prov_note(os.path.basename(path), prov) + _unprov_note() + u"\n")
    if dry_run:
        return "would-create"
    with io.open(meta, "wb") as f:           # 二进制写：⛔ 别让文本模式把行尾翻掉
        f.write(data)
    return "created"


def find_audio_files(dirpath=SFX_DIR):
    """目录下（递归）所有音频文件，排序返回；**不带 `.meta`**。"""
    out = []
    for dirpath_, _dirnames, filenames in os.walk(dirpath):
        for f in filenames:
            if f.endswith(".meta"):
                continue
            if os.path.splitext(f)[1].lower() in AUDIO_EXTS:
                out.append(os.path.join(dirpath_, f))
    return sorted(out)


def backfill(dirpath=SFX_DIR, dry_run=False, quiet=False):
    """**只补 meta、不重拷 wav**。→ `(新补列表, 已存在列表, stats)`

    这就是「已导入过的再跑一遍」那条路：不会碰任何 wav，也不会覆盖任何既有 meta。
    `stats` = `{"with_truth": n, "without_truth": n}`（新建的里面有几个有原版真值）。"""
    guids = known_guids()
    created, existed = [], []
    stats = {"with_truth": 0, "without_truth": 0}
    for p in find_audio_files(dirpath):
        st = ensure_meta(p, guids=guids, dry_run=dry_run, quiet=quiet)
        if st == "exists":
            existed.append(p)
            continue
        created.append(p)
        stem = os.path.splitext(os.path.basename(p))[0]
        stats["with_truth" if original_prov(stem) is not None else "without_truth"] += 1
    return created, existed, stats


def verify(dirpath=SFX_DIR, root=PROJECT_ROOT):
    """一次性校验（**只读**）→ `(failures, warnings, stats)`。

    ① 每个音频文件都有配对 meta；② guid 32 位小写 hex 且**全工程唯一**；
    ③ 模板与「同目录同扩展名的众数」**逐字段一致**（例外点名，但不判红 —— 可能是历史遗留）。"""
    failures, warnings = [], []
    audio = find_audio_files(dirpath)

    # ① 配对
    for p in audio:
        if not os.path.exists(p + ".meta"):
            failures.append(f"缺 meta：{p}")

    # ② guid 格式 + 目录内重复 + 全工程唯一
    local = collections.defaultdict(list)
    for p in audio:
        meta = p + ".meta"
        if not os.path.exists(meta):
            continue
        b = io.open(meta, "rb").read()
        m = GUID_LINE_RE.search(b)
        if not m:
            failures.append(f"认不出 guid 行：{meta}")
            continue
        g = m.group("guid").decode("ascii")
        if not re.fullmatch(r"[0-9a-f]{32}", g):
            failures.append(f"guid 不是 32 位小写 hex：{meta} → {g!r}")
        local[g].append(meta)
    for g, ps in local.items():
        if len(ps) > 1:
            failures.append(f"本目录 guid 重复 {g}：" + " / ".join(ps))

    global_counts = collections.Counter(g for g, _p in _iter_guid_lines(root))
    for g in local:
        if global_counts.get(g, 0) > 1:
            failures.append(f"全工程 guid 重复（{global_counts[g]} 份）：{g}")

    # ③ 那 4 项 vs **原版真值**（有真值的逐条核 —— 这才是 `B-92` 的真闸）
    truth_hit = truth_miss = truth_mismatch = 0
    for p in audio:
        meta = p + ".meta"
        if not os.path.exists(meta):
            continue
        stem = os.path.splitext(os.path.basename(p))[0]
        prov = original_prov(stem)
        if prov is None:
            truth_miss += 1
            continue
        truth_hit += 1
        try:
            got = read_prov_fields(io.open(meta, "rb").read())
        except (OSError, ValueError):
            failures.append(f"读不出那 4 项：{meta}")
            continue
        diff = {f: (got.get(f), prov[f]) for f in PROV_FIELDS if got.get(f) != prov[f]}
        if diff:
            truth_mismatch += 1
            failures.append(f"4 项与原版真值不同：{meta} → {diff}")

    # ③b **只报不改**的两项：`3D` ← `m_Legacy3D` · `ambisonic` ← `m_Ambisonic`（全库恒定）
    #     ⚠️ 不写盘（简报口径只做那 4 项），所以这里报**黄**不报红。
    const = truth_constants()
    const_mismatch = 0
    if const:
        for p in audio:
            meta = p + ".meta"
            if not os.path.exists(meta):
                continue
            try:
                b = io.open(meta, "rb").read()
            except OSError:
                continue
            for f, want in const.items():
                m = _CONST_FIELD_RE[f].search(b)
                got = int(m.group("val")) if m else None
                if got != want:
                    const_mismatch += 1
                    warnings.append(f"「只看不改」项与原版不同（{f}）：{os.path.basename(meta)} → 我们 {got} / 原版 {want}")

    # ④ 模板一致（按扩展名分组；众数之外的点名）—— 但**已按原版真值写对的**不算异常
    by_ext = collections.defaultdict(list)
    for p in audio:
        by_ext[os.path.splitext(p)[1].lower()].append(p)
    stats = {"audio": len(audio), "guids": len(local), "global_metas": sum(global_counts.values()),
             "global_guid_dups": sum(1 for c in global_counts.values() if c > 1),
             "templates": {},
             "truth_metas": truth_hit, "truth_miss": truth_miss, "truth_mismatch": truth_mismatch,
             "truth_names_total": len(load_truth()),
             "const_fields": const, "const_mismatch": const_mismatch}
    for ext, paths in sorted(by_ext.items()):
        votes, sample = collections.Counter(), {}
        for p in paths:
            meta = p + ".meta"
            if not os.path.exists(meta):
                continue
            try:
                norm = render_meta(io.open(meta, "rb").read(), "0" * 32)
            except (OSError, ValueError):
                failures.append(f"模板认不出：{meta}")
                continue
            votes[norm] += 1
            sample.setdefault(norm, p)
        if not votes:
            continue
        canonical, n = votes.most_common(1)[0]
        stats["templates"][ext] = {"canonical": n, "total": len(paths),
                                   "source": sample[canonical],
                                   "canonical_bytes": canonical}
        for norm, c in votes.items():
            if norm == canonical:
                continue
            for p in paths:
                meta = p + ".meta"
                if not os.path.exists(meta):
                    continue
                try:
                    if render_meta(io.open(meta, "rb").read(), "0" * 32) != norm:
                        continue
                except (OSError, ValueError):
                    continue
                stem = os.path.splitext(os.path.basename(p))[0]
                prov = original_prov(stem)
                if prov is not None and _prov_matches(meta, prov):
                    # 4 项对得上 ⇒ 整篇与众数不同是**故意的**，不算异常。
                    # 🔴 但**非真值字段**（normalize / quality / sampleRateOverride / 结构行）若也漂了，
                    # 必须**报黄** —— 这就是 `B-238` 那 55 份 `normalize: 1` 挂了一轮没人发现的盲区
                    # （`B114_W21` 补）：原来这里是**无条件** `continue`，把整条差异吃掉了，连黄都不报。
                    for w in _nonprov_drift_notes(meta, canonical):
                        warnings.append(w)
                    continue
                warnings.append(f"模板与同目录众数不同（{c} 条这一款）：{os.path.basename(meta)}")
    return failures, warnings, stats


TRUTH_BACKUP_DIR = "d:/4/_tmp_view/sfx_truth_backup"


def self_test(dirpath=None):
    """离线自证（**只读、零写盘、不需要 UnityPy**）。→ 退出码

    ① 真值文件读得进 + 4 项组合分布；
    ② 「模板 → 改写那 4 项」的管线在**两套模板 × 每个名字**上跑一遍，
       逐条核「只有那 4 行变了、值等于真值」；
    ③ 我们自己的音频有多少条能对上真值（对不上的点名）。
    （「真值文件 ↔ 原版 bundle 本体」那一步在 `--scan-truth`，那一步才需要 UnityPy。）"""
    bad = []
    truth = load_truth()
    n_names = len(truth)
    print(u"原版真值表：%d 个名字（%s）" % (n_names, TRUTH_JSON))
    if not n_names:
        bad.append(u"真值表是空的 —— 文件不存在或读不了")
    dist = collections.Counter(tuple(v[f] for f in PROV_FIELDS) for v in truth.values())
    print(u"  4 项组合（loadType, compressionFormat, preloadAudioData, loadInBackground）：")
    for k, c in dist.most_common():
        print(u"    %-22r ×%d" % (k, c))

    # ② 管线：两套模板 × 每个名字
    for ext, tmpl in sorted(FALLBACK_TEMPLATES.items()):
        tl = tmpl.split(b"\n")
        checked = 0
        for name in truth:
            prov = truth[name]
            out, got_prov = apply_original_prov(tmpl, name)
            if got_prov != prov:
                bad.append(u"%s/%s：apply_original_prov 返回的 prov 不对" % (ext, name))
                break
            if out.count(b"\n") != tmpl.count(b"\n"):
                bad.append(u"%s/%s：改写后行数变了" % (ext, name))
                break
            ol = out.split(b"\n")
            broke = False
            for i, (a, b) in enumerate(zip(tl, ol)):
                fld = None
                for f in PROV_FIELDS:
                    if re.match(rb"^[ \t]*" + f.encode("ascii") + rb":", a):
                        fld = f
                        break
                if fld is None:
                    if a != b:
                        bad.append(u"%s/%s：第 %d 行（非那 4 项）被改了：%r → %r" % (ext, name, i + 1, a, b))
                        broke = True
                        break
                else:
                    want = a.split(b":")[0] + b": " + str(prov[fld]).encode("ascii")
                    if b.rstrip(b"\r") != want:
                        bad.append(u"%s/%s：第 %d 行期望 %r，得到 %r" % (ext, name, i + 1, want, b))
                        broke = True
                        break
            if broke:
                break
            # 再走一遍真实写盘前的那一步（render_meta 换 guid），并把 4 项读回来
            rendered = render_meta(out, "0" * 32)
            if read_prov_fields(rendered) != prov:
                bad.append(u"%s/%s：render_meta 之后读回的 4 项 != 真值" % (ext, name))
                break
            checked += 1
        print(u"  模板 %s：管线核过 %d/%d 个名字" % (ext, checked, n_names))

    # ③ 覆盖：我们自己的音频能不能对上真值
    if dirpath is None:
        dirpath = SFX_DIR
    audio = find_audio_files(dirpath)
    if audio:
        miss = [os.path.basename(p) for p in audio
                if original_prov(os.path.splitext(os.path.basename(p))[0]) is None]
        print(u"  覆盖（%s）：音频 %d 条 · 有真值 %d 条 · 无真值 %d 条"
              % (dirpath, len(audio), len(audio) - len(miss), len(miss)))
        if miss:
            print(u"    无真值的：" + u" · ".join(miss[:12]))
    else:
        print(u"  ⚠️ %s 下没有音频，跳过覆盖率" % dirpath)

    print(u"\n===== %s =====" % (u"全过" if not bad else u"%d 条不过" % len(bad)))
    for b in bad[:40]:
        print(u"  ✗ " + b)
    return 0 if not bad else 1


def scan_truth(apply_=False):
    """**只读**扫原版 30 个含 AudioClip 的 bundle typetree，与真值文件逐条比。→ 退出码

    `--apply` 才重写真值文件（先备份到 `TRUTH_BACKUP_DIR`）。⛔ **不写 d:/2 一个字节**。
    跨 bundle 同名 clip 的 4 项不一致 ⇒ 报冲突并**拒绝**重写（那说明名字不能当键）。"""
    try:
        import UnityPy
    except Exception as e:                                   # noqa: F841
        print(u"没有 UnityPy ⇒ 扫不了 bundle（只影响本模式；--verify / --self-test / --backfill 不需要它）")
        return 2
    if not os.path.isdir(BUNDLE_DIR):
        print(u"找不到 bundle 目录：%s" % BUNDLE_DIR)
        return 2

    files = sorted(f for f in os.listdir(BUNDLE_DIR) if f.endswith(".bundle"))
    live, conflicts, per_bundle, failed = {}, {}, {}, []
    const_pairs = collections.Counter()      # (m_Legacy3D, m_Ambisonic) —— 预期只有一个组合
    for i, fn in enumerate(files, 1):
        try:
            env = UnityPy.load(os.path.join(BUNDLE_DIR, fn))
            k = 0
            for o in env.objects:
                if o.type.name != "AudioClip":
                    continue
                try:
                    d = o.read()
                except Exception:
                    continue
                if getattr(d, "m_LoadType", None) is None:
                    continue
                nm = getattr(d, "m_Name", None)
                if not nm:
                    continue
                rec = dict(zip(PROV_FIELDS, (
                    int(d.m_LoadType), int(d.m_CompressionFormat),
                    int(bool(d.m_PreloadAudioData)), int(bool(d.m_LoadInBackground)))))
                if nm in live and live[nm] != rec:
                    conflicts.setdefault(nm, [live[nm]]).append(rec)
                live.setdefault(nm, rec)
                const_pairs[(int(bool(getattr(d, "m_Legacy3D", False))),
                             int(bool(getattr(d, "m_Ambisonic", False))))] += 1
                k += 1
            if k:
                per_bundle[fn] = k
        except Exception as e:
            failed.append((fn, str(e)))
        if i % 15 == 0:
            print(u"  … 扫到 %d/%d" % (i, len(files)))

    print(u"bundle %d 个 · 含 AudioClip 的 %d 个 · 对象 %d 条 · 去重名字 %d · 跨 bundle 冲突 %d"
          % (len(files), len(per_bundle), sum(per_bundle.values()), len(live), len(conflicts)))
    for fn, e in failed:
        print(u"  ✗ %s：%s" % (fn, e))

    cur = load_truth()
    added = sorted(set(live) - set(cur))
    removed = sorted(set(cur) - set(live))
    changed = sorted(n for n in set(live) & set(cur) if live[n] != cur[n])
    print(u"与 %s 相比：新增 %d · 消失 %d · 值变了 %d"
          % (os.path.basename(TRUTH_JSON), len(added), len(removed), len(changed)))
    for n in added[:10]:
        print(u"  + %s %r" % (n, live[n]))
    for n in removed[:10]:
        print(u"  - %s" % n)
    for n in changed[:10]:
        print(u"  ~ %s %r → %r" % (n, cur[n], live[n]))
    for n, v in list(conflicts.items())[:10]:
        print(u"  ⚠️ 跨 bundle 冲突 %s：%r" % (n, v))

    if not apply_:
        print(u"（只比对，没写盘；真要重写加 --apply）")
        return 0 if not conflicts else 1
    if conflicts:
        print(u"⛔ 有跨 bundle 冲突 ⇒ 拒绝重写（名字不能当键）")
        return 1
    if not live:
        print(u"⛔ 一条都没扫到 ⇒ 拒绝重写（不拿空表覆盖）")
        return 1

    const = {}
    if len(const_pairs) == 1:
        (l3d, amb), n = const_pairs.most_common(1)[0]
        const = {"3D": l3d, "ambisonic": amb}
        print(u"恒定项：3D=%d（m_Legacy3D）· ambisonic=%d（m_Ambisonic）· %d/%d 条一致 ⇒ 写入 `_meta.constant_fields`"
              % (l3d, amb, n, sum(const_pairs.values())))
    else:
        print(u"⚠️ 恒定项不是常量（%d 种组合：%r）⇒ 不写 `_meta.constant_fields`"
              % (len(const_pairs), dict(const_pairs)))

    os.makedirs(TRUTH_BACKUP_DIR, exist_ok=True)
    if os.path.exists(TRUTH_JSON):
        bak = os.path.join(TRUTH_BACKUP_DIR,
                           "sfx_audio_truth.%s.json" % time.strftime("%Y%m%d_%H%M%S"))
        shutil.copyfile(TRUTH_JSON, bak)
        print(u"备份 → %s" % bak.replace("\\", "/"))
    doc = {
        "_README": u"机生成 · 重生成命令 `python -I -X utf8 工具/_sfx_meta.py --scan-truth --apply` · ⛔ 别手改",
        "_meta": {
            "generated_by": "Unity/工具/_sfx_meta.py --scan-truth --apply",
            "source": BUNDLE_DIR,
            "date": time.strftime("%Y-%m-%d"),
            "fields": list(PROV_FIELDS),
            "constant_fields": const,
            "note": u"原版 AudioClip typetree 的 m_LoadType / m_CompressionFormat / "
                    u"m_PreloadAudioData / m_LoadInBackground；`constant_fields` 是 "
                    u"m_Legacy3D / m_Ambisonic（全库恒定，**只用于 `--verify` 报黄，不写盘**）；"
                    u"⛔ 别手改（重跑 --scan-truth 覆盖）",
        },
        "clips": {n: [live[n][f] for f in PROV_FIELDS] for n in sorted(live)},
    }
    with io.open(TRUTH_JSON, "w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(doc, ensure_ascii=False, indent=0) + "\n")
    print(u"已重写 %s（%d 个名字）" % (TRUTH_JSON, len(live)))
    return 0


def probe(name, ext=".wav"):
    """只读：打印「新建一条名叫 `name` 的音频会被写成什么」。→ 退出码"""
    ext = ext.lower()
    if not ext.startswith("."):
        ext = "." + ext
    tmpl = FALLBACK_TEMPLATES.get(ext)
    if tmpl is None:
        print(u"没有 %s 的内置模板（只支持 %s）" % (ext, sorted(FALLBACK_TEMPLATES)))
        return 2
    out, prov = apply_original_prov(tmpl, name)
    data = render_meta(out, "0" * 32)
    print(u"# %s%s  →  4 项 = %s" % (name, ext, u"原版真值 %r" % prov if prov else u"⚠️ 无原版真值（沿用模板）"))
    print(u"# " + _unprov_note())
    sys.stdout.write(data.decode("utf-8"))
    return 0


# ============================================================================
# `--check-fallback`（`B-238`，2026-11-01）：兜底模板 ↔ 现读众数 ↔「冷重建会写成什么」
# ============================================================================
# 为什么单开一个模式：`--verify` 的 ④ 已经算过「同目录同扩展名的众数」，但它对**有原版真值**的
# 文件，只要那 4 项对得上就整条 `continue`（见 `verify()` 里那句「差异是**故意的**，不是漂移」）
# ⇒ `normalize` 这类**非真值字段**的差异被一起吞掉，永远不会报黄。这个模式**不看那 4 项**
# （它们本来就该被逐条改写），只比**非真值字段 + 结构行**。
#
# 三件事，全部只读（唯一的写盘 = `tempfile.mkdtemp()` 里的探针目录，跑完 `shutil.rmtree`）：
#   ① **兜底模板的非真值行 == 现读全树众数的非真值行**（按扩展名；`pick_template` 是**按目录**投票，
#      但结构行 / `normalize` / `quality` / `sampleRateOverride` 在 4 个目录 2 种扩展名里本来就一致 ⇒
#      一个按扩展名的兜底模板够用；不一致时这里会点名是哪个目录）。
#   ② **`normalize` 现读 == 0**（`B-238` 的口径；写成 1 冷重建会把整棵树漂过去）。
#   ③ **「冷重建会写成什么」逐份算，与现状逐字节比**：冷重建的第一份没有兄弟 ⇒ 必走兜底模板；
#      之后所有兄弟的「众数」就等于第一份写出来的东西 ⇒ **逐份「兜底模板 + 真值改写」的产物
#      就是冷重建的稳态结果**（`apply_original_prov` 只改那 4 行、不改别的，所以这一步是精确的，
#      不是近似）。`--deep` 时再在临时空目录里**真跑一次 `ensure_meta`** 做端到端复核。
ZERO_GUID = "0" * 32


def _render_or_none(b):
    try:
        return render_meta(b, ZERO_GUID)
    except ValueError:
        return None


def _nonprov_lines(norm_bytes):
    """→ 去掉那 4 项（`PROV_FIELDS`）之后的非空行。`normalize`/`quality`/`sampleRateOverride`/结构行都在里面。"""
    out = []
    for ln in norm_bytes.split(b"\n"):
        s = ln.strip()
        if not s:
            continue
        if s.split(b":")[0].decode("utf-8", "replace") in PROV_FIELDS:
            continue
        out.append(ln)
    return out


def _field_of(line):
    s = line.strip()
    return s.split(b":")[0].decode("utf-8", "replace") if b":" in s else s.decode("utf-8", "replace")


def _line_diff(a, b, limit=4):
    """→ 逐行差的人话列表（`对我们 / 兜底` 的标法由调用方加）。"""
    out = []
    for i in range(max(len(a), len(b))):
        x = a[i] if i < len(a) else b"<MISSING>"
        y = b[i] if i < len(b) else b"<MISSING>"
        if x != y:
            out.append(u"%s: %r → %r" % (_field_of(x if x != b"<MISSING>" else y), x.strip(), y.strip()))
            if len(out) >= limit:
                break
    return out


def cold_rebuild_meta(audio_path, ext=None):
    """→ 「把 `audio_path` 放进一个**空目录**里冷重建，会写成什么样」（零写盘、纯内存）。

    冷重建时目录里一条兄弟 `.meta` 都没有 ⇒ `pick_template()` 必走 `FALLBACK_TEMPLATES`，
    再被 `apply_original_prov()` 按原版真值改那 4 行；第一份写出来之后，后面所有兄弟的众数
    就等于它 ⇒ **这个函数的结果就是该目录冷重建的稳态值**。"""
    if ext is None:
        ext = os.path.splitext(audio_path)[1].lower()
    tmpl = FALLBACK_TEMPLATES.get(ext)
    if tmpl is None:
        return None, None
    stem = os.path.splitext(os.path.basename(audio_path))[0]
    out, prov = apply_original_prov(tmpl, stem)
    return render_meta(out, ZERO_GUID), prov


def check_fallback(dirpath=None, quiet=False, deep=False):
    """只读自证「兜底档现在是 `normalize: 0`，且冷重建不会漂」。→ 退出码（0 = 全过）

    ① 兜底模板非真值行 ↔ 现读众数 · ② `normalize` 现读 == 0 · ③ 逐份冷重建 vs 现状 · ④（`deep`）端到端真跑。"""
    if dirpath is None:
        dirpath = SFX_DIR
    bad = []
    audio = find_audio_files(dirpath)
    if not audio:
        print(u"⚠️ %s 下没有音频 —— 这一步跳过（不判红）" % dirpath)
        return 0

    # 分组众数（按「相对目录 + 扩展名」，与 pick_template 的口径一致）
    groups, modal = collections.defaultdict(list), {}
    for p in audio:
        meta = p + ".meta"
        if not os.path.exists(meta):
            continue
        try:
            norm = render_meta(io.open(meta, "rb").read(), ZERO_GUID)
        except (OSError, ValueError):
            continue
        rel = os.path.relpath(os.path.dirname(p), dirpath).replace("\\", "/")
        groups[(rel, os.path.splitext(p)[1].lower())].append(norm)

    print(u"【① 兜底模板 非真值行 ↔ 现读众数】")
    for (rel, ext), norms in sorted(groups.items()):
        c = collections.Counter(tuple(_nonprov_lines(n)) for n in norms)
        want, n = c.most_common(1)[0]
        fb = FALLBACK_TEMPLATES.get(ext)
        if fb is None:
            print(u"  %-12s %-5s %4d 份 · 无内置兜底模板（不判）" % (rel or ".", ext, len(norms)))
            continue
        got = tuple(_nonprov_lines(render_meta(fb, ZERO_GUID)))
        if got == want:
            print(u"  %-12s %-5s %4d 份 · 众数 %d · ✅ 非真值行一致" % (rel or ".", ext, len(norms), n))
        else:
            print(u"  %-12s %-5s %4d 份 · 众数 %d · 🔴 非真值行**不一致**" % (rel or ".", ext, len(norms), n))
            for d in _line_diff(got, list(want), limit=6):
                print(u"      兜底 vs 众数 → " + d)
            bad.append(u"%s/%s：兜底模板的非真值行 != 现读众数（冷重建会漂）" % (rel or ".", ext))

    print(u"【② `normalize` 现读值】")
    nz = collections.Counter()
    for norms in groups.values():
        for norm in norms:
            for ln in _nonprov_lines(norm):
                if ln.strip().startswith(b"normalize:"):
                    nz[ln.strip().split(b":")[1].strip()] += 1
                    break
    for v, c in sorted(nz.items()):
        print(u"  normalize: %s × %d" % (v.decode("ascii", "replace"), c))
    fb_nz = {}
    for ext, fb in sorted(FALLBACK_TEMPLATES.items()):
        for ln in _nonprov_lines(render_meta(fb, ZERO_GUID)):
            if ln.strip().startswith(b"normalize:"):
                fb_nz[ext] = ln.strip().split(b":")[1].strip()
    print(u"  兜底模板：" + u" · ".join(u"%s normalize: %s" % (e, v.decode("ascii")) for e, v in sorted(fb_nz.items())))
    for e, v in sorted(fb_nz.items()):
        if v != b"0":
            bad.append(u"兜底模板 %s 的 normalize = %s（应为 0 —— `B-238`）" % (e, v.decode("ascii")))
    if nz and set(nz) != {b"0"}:
        # 现状里本来就有 55 份是 1（历史遗留，⛔ 不在本模式里判红 —— 只点名）
        print(u"  ⚠️ 现状里有 %d 份 normalize != 0（历史遗留，冷重建会写成 0；见报告 §一 的清单）"
              % sum(c for v, c in nz.items() if v != b"0"))

    # ③ 逐份「冷重建会写成什么」vs 现状
    print(u"【③ 冷重建（纯内存推算）vs 现状：%d 份】" % len(audio))
    same = diff = 0
    by_field = collections.Counter()
    samples = []
    cold_nz = collections.Counter()
    for p in audio:
        meta = p + ".meta"
        if not os.path.exists(meta):
            continue
        real = _render_or_none(io.open(meta, "rb").read())
        cold, prov = cold_rebuild_meta(p)
        if cold is None:
            continue
        for ln in _nonprov_lines(cold):
            if ln.strip().startswith(b"normalize:"):
                cold_nz[ln.strip().split(b":")[1].strip()] += 1
                break
        if real is None or cold == real:
            same += 1
            continue
        diff += 1
        a, b = _nonprov_lines(cold), _nonprov_lines(real or b"")
        fields = []
        for i in range(max(len(a), len(b))):
            x = a[i] if i < len(a) else b"<MISSING>"
            y = b[i] if i < len(b) else b"<MISSING>"
            if x != y:
                fields.append(_field_of(y if y != b"<MISSING>" else x))
        by_field[tuple(sorted(fields))] += 1
        if len(samples) < 8:
            samples.append(u"%s（%s）" % (os.path.relpath(p, dirpath).replace("\\", "/"),
                                          u", ".join(_line_diff(a, b))))
    print(u"  冷重建后 normalize 分布：" + u" · ".join(
        u"%s × %d" % (v.decode("ascii", "replace"), c) for v, c in sorted(cold_nz.items())))
    print(u"  与现状逐字节一致 %d 份 · 不一致 %d 份" % (same, diff))
    for fields, c in by_field.most_common():
        print(u"    差在 %s 的：%d 份" % (u"+".join(fields) or u"(?)", c))
    for s in samples:
        print(u"    " + s)

    # ④ --deep：临时空目录里真跑一次 ensure_meta（端到端；证明 `pick_template` 真走到了兜底）
    if deep:
        import tempfile
        print(u"【④ 端到端：临时空目录里真跑 ensure_meta】")
        root = tempfile.mkdtemp(prefix="wf_sfxmeta_cold_")
        try:
            for ext in sorted(FALLBACK_TEMPLATES):
                src = next((p for p in audio if p.lower().endswith(ext)), None)
                if src is None:
                    continue
                d = os.path.join(root, ext.strip("."))
                os.makedirs(d)
                guids = set()
                # 同一目录连放 3 份：第 1 份必走兜底，第 2/3 份走「众数」⇒ 两档都要对
                for i in range(3):
                    t = os.path.join(d, "probe%d%s" % (i, ext))
                    with io.open(t, "wb") as f:
                        f.write(b"")
                    ensure_meta(t, guids=guids, quiet=True)
                    got = _render_or_none(io.open(t + ".meta", "rb").read())
                    nzl = [ln.strip() for ln in _nonprov_lines(got) if ln.strip().startswith(b"normalize:")]
                    ok = bool(nzl) and nzl[0] == b"normalize: 0"
                    print(u"  %s 第 %d 份：%s %s" % (ext, i + 1, u"✅" if ok else u"🔴",
                                                    nzl[0].decode("ascii", "replace") if nzl else u"(没有 normalize 行)"))
                    if not ok:
                        bad.append(u"端到端 %s 第 %d 份冷重建 normalize 不是 0" % (ext, i + 1))
        finally:
            shutil.rmtree(root, ignore_errors=True)

    print(u"\n===== %s =====" % (u"兜底档 = normalize: 0，冷重建不漂" if not bad else u"%d 条不过" % len(bad)))
    for b in bad[:40]:
        print(u"  ✗ " + b)
    return 0 if not bad else 1


# ============================================================================
# `verify()` ④ 的两个小件（`B114_W21`，2026-11-01）：补「有真值就整条放行」的盲区
# ============================================================================
# 原来 ④ 对有原版真值的文件是**无条件** `continue`（只要 4 项对得上）⇒ `normalize` 这类
# **非真值字段**的漂移被整条吞掉、**连黄都不报**。`B-238` 那 55 份 `normalize: 1` 因此挂了
# 一整轮没人发现。下面两个函数把那一档拆开：**4 项对 ≠ 非真值字段也对**。
# ⚠️ 放在文件尾部（`self_test` 之后）是有意的：**别让 `verify:485` / `self_test:622` 漂**
#    （外部文档引的就是这几个行号，见 `判据/B114_R4_B93重建性.md:25,31`）。
def _prov_matches(meta, prov):
    """这份 `.meta` 的那 4 项是否 == 原版真值？读不出来 ⇒ False（走通配的「报黄」档，⛔ 不静默放行）。"""
    try:
        got = read_prov_fields(io.open(meta, "rb").read())
    except (OSError, ValueError):
        return False
    return all(got.get(f) == prov[f] for f in PROV_FIELDS)


def _nonprov_drift_notes(meta, canonical):
    """有原版真值的文件：**非真值行**（`_nonprov_lines`）与同扩展名众数 `canonical` 的差。→ 描述列表

    空列表 = 非真值行完全一致 ⇒ 整篇不同只是因为那 4 项（**故意的**，不报）。
    非空 = `normalize` / `quality` / `sampleRateOverride` / 结构行真的漂了 ⇒ **报黄**。"""
    try:
        real = render_meta(io.open(meta, "rb").read(), ZERO_GUID)
    except (OSError, ValueError):
        return []
    ours, want = _nonprov_lines(real), _nonprov_lines(canonical)
    if ours == want:
        return []
    return [u"非真值字段与同扩展名众数不同（那 4 项已对上真值）：%s → %s"
            % (os.path.basename(meta), u" · ".join(_line_diff(ours, want)))]


def main():
    ap = argparse.ArgumentParser(
        description="音频导入脚本共用的 .meta 补写/校验（模板 = 同目录同扩展名的众数，只换 guid；"
                    "loadType/compressionFormat/preloadAudioData/loadInBackground 按原版资产真值写）")
    ap.add_argument("--backfill", action="store_true",
                    help="给目录下所有缺 meta 的音频补 meta（幂等；已存在的跳过；不碰 wav）")
    ap.add_argument("--verify", action="store_true",
                    help="只读校验：配对 / guid 全局唯一与格式 / 那 4 项与原版真值 / 模板一致")
    ap.add_argument("--self-test", action="store_true",
                    help="只读离线自证：真值文件完整性 + 模板改写管线 + 覆盖率 + 兜底模板 normalize（不需要 UnityPy）")
    ap.add_argument("--check-fallback", action="store_true",
                    help="只读自证（B-238）：兜底模板的非真值行 ↔ 现读众数 · normalize 现读 == 0 · 冷重建 vs 现状")
    ap.add_argument("--deep", action="store_true",
                    help="配合 --check-fallback：再在临时空目录里真跑一次 ensure_meta（端到端）")
    ap.add_argument("--scan-truth", action="store_true",
                    help="只读扫原版 bundle 的 AudioClip typetree，与真值文件逐条比（需要 UnityPy）")
    ap.add_argument("--probe", metavar="NAME",
                    help="只读：打印「名为 NAME 的新音会被写成什么」（配合 --ext，默认 .wav）")
    ap.add_argument("--ext", default=".wav", help="配合 --probe 的扩展名（默认 .wav）")
    ap.add_argument("--apply", action="store_true",
                    help="配合 --scan-truth：真重写真值文件（先备份；有跨 bundle 冲突则拒绝）")
    ap.add_argument("--dir", default=SFX_DIR, help=f"音频目录（默认 {SFX_DIR}）")
    ap.add_argument("--dry-run", action="store_true", help="配合 --backfill：只报条数，不写盘")
    a = ap.parse_args()

    if not (a.backfill or a.verify or a.self_test or a.probe or a.scan_truth or a.check_fallback):
        ap.print_help()
        return 0
    if a.scan_truth:
        return scan_truth(a.apply)
    if a.check_fallback:
        return check_fallback(a.dir, deep=a.deep)
    if a.self_test:
        return self_test(a.dir) or check_fallback(a.dir)
    if a.probe:
        return probe(a.probe, a.ext)

    if a.backfill:
        created, existed, st = backfill(a.dir, dry_run=a.dry_run)
        print(f"音频 {len(created) + len(existed)} 条 · 补 meta {len(created)} 条"
              + ("（dry-run，未写盘）" if a.dry_run else "")
              + f" · 已存在 {len(existed)} 条")
        print(f"  新建的里面：4 项有原版真值 {st['with_truth']} 条 · 无真值（沿用模板）{st['without_truth']} 条")
        for p in created:
            print("  + " + os.path.basename(p) + ".meta")

    if a.verify:
        failures, warnings, stats = verify(a.dir)
        print(f"音频 {stats['audio']} 条 · 配对后 guid {stats['guids']} 个 · "
              f"全工程 meta {stats['global_metas']} 份（重复 guid {stats['global_guid_dups']} 组）")
        print(f"  原版真值：表内 {stats['truth_names_total']} 个名字 · 我们有 {stats['truth_metas']} 条对得上名字"
              f"（4 项不一致 {stats['truth_mismatch']} 条）· 无真值 {stats['truth_miss']} 条")
        if stats["const_fields"]:
            print("  「只看不改」两项（不写盘，只报黄）："
                  + " · ".join(f"{k}={v}" for k, v in stats["const_fields"].items())
                  + f" · 不一致 {stats['const_mismatch']} 条")
        for ext, t in stats["templates"].items():
            src = os.path.basename(t["source"]) if t["source"] else "（内置兜底）"
            print(f"  模板 {ext}: 众数 {t['canonical']}/{t['total']} · 取自 `{src}`")
        for w in warnings:
            print("  ⚠️ " + w)
        if failures:
            for f in failures:
                print("  🔴 " + f)
            print(f"=== {len(failures)} 红 / {len(warnings)} 黄 ===")
            return 1
        print(f"=== 0 红 / {len(warnings)} 黄 ===")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
