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

口径（三条，别改）
------------------
1. **模板 = 既有兄弟文件的众数**（**同一目录 + 同一扩展名**），**逐字节照抄**，只换 `guid`。
   ⛔ 不手写模板、⛔ 不「按理解」拼字段。实测同目录 `.wav.meta` 与 `.ogg.meta` 本来就不是
   同一套设置（`sfx/*.ogg.meta` 是 `loadType: 2` Streaming / `compressionFormat: 1` / `preload 1`），
   而 `vo/*.ogg.meta` 又是 `loadType: 1 / 1 / 0` ⇒ **「一套模板走天下」会把 ogg 写错**。
2. **已存在就不覆盖**（`ensure_meta()` 直接返回 `exists`）—— 既有 guid 一旦被换掉，
   引用它的场景 / prefab / 资产全部断链，而且**不会有任何报错**。
3. **guid 全局唯一**：写前扫全工程 `*.meta`（`d:/4/Unity`，2026-10-24 实测 **24,074 份、0 重复**）
   的 `guid:`，撞了就重摇。格式恒为 **32 位小写 hex**（Unity 自己的写法）。

用法
----
    # ① 三个导入脚本内部：拷完一条就 `ensure_meta(path)`（已存在的直接跳过，幂等）
    # ② **只补 meta、不重拷 wav**（幂等、可反复跑；这就是「已导入过的再过一遍」那条路）：
    python -I -X utf8 工具/_sfx_meta.py --backfill            # 真写
    python -I -X utf8 工具/_sfx_meta.py --backfill --dry-run  # 只数
    # ③ 一次性校验（只读）：配对 / guid 全局唯一 + 格式 / 模板逐字段一致
    python -I -X utf8 工具/_sfx_meta.py --verify
"""
import argparse
import collections
import io
import os
import re
import uuid

PROJECT_ROOT = "d:/4/Unity"
SFX_DIR = PROJECT_ROOT + "/MyGame/Assets/CardPresentation/Resources/Art/audio/sfx"

# 认作「音频」的扩展名（小写比较）
AUDIO_EXTS = (".wav", ".ogg", ".mp3", ".aif", ".aiff", ".flac")

# `guid:` 那一行。`(?P<eol>\r?)` 把行尾吃掉再原样写回 ⇒ **CRLF 文件不会被翻成 LF**。
GUID_LINE_RE = re.compile(rb"^guid: (?P<guid>[0-9a-fA-F]{32})(?P<eol>\r?)$", re.M)

# 内置兜底模板（**只在**目标目录一条同扩展名兄弟 meta 都没有时才用；有新目录时请先让 Unity
# 生成一条、或手工补一条，别指望这里）。两份都是 2026-10-24 从本工程实测的众数**逐字节**抄的：
#   · `.wav` ← `Resources/Art/audio/sfx/` 510/511 份的模板（`OvertimeStart.wav.meta` 是已知例外）
#   · `.ogg` ← 同目录 5/5 份的模板
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
        b"  normalize: 1\n"
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
        b"  normalize: 1\n"
        b"  loadInBackground: 1\n"
        b"  ambisonic: 0\n"
        b"  3D: 1\n"
        b"  userData: \n"
        b"  assetBundleName: \n"
        b"  assetBundleVariant: \n"
    ),
}

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


def ensure_meta(path, guids=None, dry_run=False):
    """给音频文件 `path` 补 `<path>.meta`。→ `"exists"` / `"created"` / `"would-create"`

    🔴 **已存在 ⇒ 一个字节都不碰**（返回 `"exists"`）—— 这是幂等路径的核心。"""
    meta = path + ".meta"
    if os.path.exists(meta):
        return "exists"
    ext = os.path.splitext(path)[1].lower()
    template, _src, _n = pick_template(os.path.dirname(path) or ".", ext)
    data = render_meta(template, new_guid(guids))
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


def backfill(dirpath=SFX_DIR, dry_run=False):
    """**只补 meta、不重拷 wav**。→ `(新补列表, 已存在列表)`

    这就是「已导入过的再跑一遍」那条路：不会碰任何 wav，也不会覆盖任何既有 meta。"""
    guids = known_guids()
    created, existed = [], []
    for p in find_audio_files(dirpath):
        st = ensure_meta(p, guids=guids, dry_run=dry_run)
        (existed if st == "exists" else created).append(p)
    return created, existed


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

    # ③ 模板一致（按扩展名分组；众数之外的点名）
    by_ext = collections.defaultdict(list)
    for p in audio:
        by_ext[os.path.splitext(p)[1].lower()].append(p)
    stats = {"audio": len(audio), "guids": len(local), "global_metas": sum(global_counts.values()),
             "global_guid_dups": sum(1 for c in global_counts.values() if c > 1),
             "templates": {}}
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
                    if render_meta(io.open(meta, "rb").read(), "0" * 32) == norm:
                        warnings.append(f"模板与同目录众数不同（{c} 条这一款）：{os.path.basename(meta)}")
                except (OSError, ValueError):
                    pass
    return failures, warnings, stats


def main():
    ap = argparse.ArgumentParser(
        description="音频导入脚本共用的 .meta 补写/校验（模板 = 同目录同扩展名的众数，只换 guid）")
    ap.add_argument("--backfill", action="store_true",
                    help="给目录下所有缺 meta 的音频补 meta（幂等；已存在的跳过；不碰 wav）")
    ap.add_argument("--verify", action="store_true",
                    help="只读校验：配对 / guid 全局唯一与格式 / 模板逐字段一致")
    ap.add_argument("--dir", default=SFX_DIR, help=f"音频目录（默认 {SFX_DIR}）")
    ap.add_argument("--dry-run", action="store_true", help="配合 --backfill：只报条数，不写盘")
    a = ap.parse_args()

    if not a.backfill and not a.verify:
        ap.print_help()
        return 0

    if a.backfill:
        created, existed = backfill(a.dir, dry_run=a.dry_run)
        print(f"音频 {len(created) + len(existed)} 条 · 补 meta {len(created)} 条"
              + ("（dry-run，未写盘）" if a.dry_run else "")
              + f" · 已存在 {len(existed)} 条")
        for p in created:
            print("  + " + os.path.basename(p) + ".meta")

    if a.verify:
        failures, warnings, stats = verify(a.dir)
        print(f"音频 {stats['audio']} 条 · 配对后 guid {stats['guids']} 个 · "
              f"全工程 meta {stats['global_metas']} 份（重复 guid {stats['global_guid_dups']} 组）")
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
