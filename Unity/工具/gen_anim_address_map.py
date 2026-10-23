#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_anim_address_map.py — 「CardAnim → AssetGUID → 本地 prefab/资产」地址表生成器

用法（本项目惯例）：
    PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_anim_address_map.py
    # 可选 --quiet          只打统计，不打未解出明细
    # 可选 --limit-bundles N 调试用，只处理前 N 个 bundle

只读（绝不写 d:/2/）：
  ① d:/2/新解包资源/assets_full/<bundle>/AssetBundle/AssetBundle_*.json
        —— 每个 bundle 一个 Unity `AssetBundle` 对象的 typetree dump。
           **`m_Container` 就是 Addressables 的「地址表」**：
           键 = 资产 GUID 字符串，值 = {preloadIndex, preloadSize, asset:{m_FileID, m_PathID}}。
  ② d:/2/新解包资源/assets_full/<bundle>/MonoBehaviour/*.json
        —— CardAnim 侧：`animInfo.animAdressable.m_AssetGUID` 指向 ① 里的某个 GUID。
  ③ <bundle 源目录>/<name>.bundle
        —— 原 AssetBundle 二进制（UnityPy 重新读），用来把 m_PathID 反解成 (类型, 名字)。

输出（只写这一个文件）：
    d:/4/Unity/数据/索引/anim_address_map.json

join 办法（三选一里选了「UnityPy 重读原 bundle」+「文件名后缀」并用）：
  · **m_PathID → (类型, 名字)**：只能走 UnityPy。
    `m_Container[..].asset.m_PathID` 就是该 bundle 内对象的 path_id（实测 13448 条全部
    m_FileID==0，即全部是本包内引用，不需要跨包解依赖）。用 `env.objects` 建
    `{path_id: obj}` 即可反解。
  · **m_PathID → dump 文件名**：走文件名后缀，规则可证：
    解包脚本 `d:/2/Warpforge_tools/scripts/extract_full.py:183-198` 是
        fname = safe_name(m_Name, f"{Type}_{path_id}")      # 非法字符→_ ，先 strip 再截断到 120
        jpath = f"{fname}.json"；**若该文件"当时已存在"**才退成 f"{fname}_{path_id}.json"
    ⇒ ① 若 `{safe}_{pathId}.json` 存在 → 就是它；
       ② 否则该对象就是**同名里的第一个**，文件是 `{safe}.json`。
    ⚠️ **2026-10-07 措辞更正（铁律 5；A161 顺手 ④）**：① 后面原来写的是「（**path_id 唯一，无歧义**）」
       —— **这句不成立**：pid 是**分包局部**的（全 84 包「同 pid 不同名」的 GameObject 有 1,291 条，
       见 `资料/已知的坑.md`）。① 今天成立**另有原因**（对着 `extract_full.py:183-198` 读的 + 实测）：
       · 后缀写的就是**那个对象自己的** `obj.path_id`，而且**只在同名相撞时才加**（`os.path.exists` 那一跳）；
       · 只有「(类型, 名字, pid) 完全相同」的第二件才会**覆盖**它 —— 🔴 **实测（2026-10-07，我量的）：
         15 个双 CAB 包里这种对象 = 0 条**（单 CAB 包内 pid 本来就唯一，不可能撞）。
         ⚠️ 这条是**数据性质、不是结构保证** ⇒ 哪天真撞了，症状是**静默少一份 dump**（后写覆盖前写）。
       ⛔ 别把「path_id 唯一」当结论抄到别的工具去；要跨包反查就按 `(包, pid)`。
    实测 `a5834a7ae2dd92745a7d0e2637239c47` → pathId `-1939833645471234905`
    → `battleprefabs_vfxandmisc_assets_all/GameObject/Swarm_Trigger_OnTarget.json`
    （文件名里**没有**这个 path_id，正是走 ② 的那一支）。
  · 🆕 **短 internalId 容器键（2026-10-19 `T15` / `A2131`）**：EC 的 cardassets / cardanims 两个束，
    `m_Container` 的键是**短 internalId**（`bc` / `0d7` / `eac` …，= 真 GUID 的前缀截断），不是 GUID。
    只枚举容器项时这些资产**没有 GUID 身份**（实测 EC 卡引用的 27 个 CardAnim GUID 全缺）。
    ⇒ 本脚本额外读 Addressables catalog 的「GUID → (束, internalId)」补这一跳
    （判据：`资料/普查产出_1018/R_K11_K12_报告.md` §五·1；EC29 全链
    `bc43540f…` → `Rally EC Heldrake` → `67eb257e…` → prefab `EC Heldrake Rally`）。
    🔴 **2026-10-23（工具线 `T14`）：catalog 优先用【二进制】**（安装版 `aa/catalog.bin`，
    `buildResultHash ae516dc0…`，与本地束同 build）—— 旧 `catalog_main.json` 是 2025-07-30
    快照、**不是同一 build**，EC 短键束里 10 个资产的 `internalId` 被截断到**别的键**
    ⇒ 会配到另一个对象（实测 12 条真错，逐条见 `资料/普查产出_1023/C5_语音错配修复.md` §5·1）。
    旧 JSON 只在两份二进制都不在时兜底。
    找不到任何 catalog 时**如实降级**（stats 里记 `catalogJsonError`），不编理由。
  · 🆕 **旧表 `exact=False` 的「退键族」（2026-10-19 T15 残余）**：catalog 给这些 GUID 的
    internalId 是**截断键**（`d`），拿它去束里取会命中别的对象 ⇒ 上面那条升级闸门整族跳过。
    但它们**不是解不出**：旧表已经用「退键」找到了真正的容器键（`db9`）。本脚本现在补一条
    **三元组唯一命中**的升级路：按旧表那一行的 `(bundle, containerKey, type, name)` 回到
    `m_Container` 里找，**恰好 1 个对象**同名同类型才升级（stats:
    `containerEntriesGuidViaVerifiedRetreat`）。实测 31/31 命中；逐条卡名旁证见
    `资料/普查产出_1019/W57_特效线_T15_V1_T02静态.md`。
"""

import argparse
import io
import json
import os
import re
import sys
import time
from collections import Counter, defaultdict

# ---------------------------------------------------------------- 常量 / 路径

ASSETS_FULL = r"d:/2/新解包资源/assets_full"

# 原 bundle 源目录，按顺序找第一个存在的（两份都在，内容同源）
BUNDLE_SRC_CANDIDATES = [
    r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64",
    r"d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64",
]

# Addressables 目录（JSON 形态）—— **GUID → (束, internalId)** 的唯一权威来源。
# 有些束（EC 的 cardassets / cardanims）的 `m_Container` 键是**短 internalId**（`bc` / `0d7`），
# 不是 GUID；只枚举容器项时这些资产就**没有 GUID 身份**。catalog 给出这一跳。
# 判据：`资料/普查产出_1018/R_K11_K12_报告.md` §五·1（EC29 全链例子）。
CATALOG_JSON_CANDIDATES = [
    os.path.join(os.path.expanduser("~"), "AppData", "LocalLow", "Everguild",
                 "Warpforge", "com.unity.addressables", "catalog_main.json"),
    r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/catalog_main.json",
]

# 🆕 **2026-10-23（工具线 `T14`）：二进制 catalog 优先**。
# 为什么：上面的 `catalog_main.json` 是 **2025-07-30 快照**，与本地束**不是同一 build** ——
#   EC 短键束里 10 个资产的 `internalId` 被**截断到别的键**（例 `8c3458c0…` 它给 `8c`，
#   而本 build 的真键是 `8`）⇒ 那一族 GUID 会配到**另一个对象**（实测 12 条真错：
#   6 音频 + 4 美术/卡框，逐条见 `资料/普查产出_1023/C5_语音错配修复.md` §5·1）。
#   与本地束同 build 的二进制 catalog（安装版 `aa/catalog.bin`，`buildResultHash ae516dc0…`）
#   给的是**真键**；安卓 1.35.0 的 catalog（`0dd7cbfa…`）与之逐条同键（互证见 `C3`）。
# ⚠️ 旧 JSON 仍保留为**最后兜底**（二进制两份都不在时），并在输出里出声。
CATALOG_BIN_CANDIDATES = [
    (r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/catalog.bin",
     "安装版 aa/catalog.bin（与本地束同 build）"),
    (os.path.join(os.path.expanduser("~"), "AppData", "LocalLow", "Everguild",
                  "Warpforge", "com.unity.addressables", "catalog_main.bin"),
     "LocalLow catalog_main.bin"),
]

DATA_DIR = r"d:/4/Unity/数据/游戏数据"
INDEX_DIR = r"d:/4/Unity/数据/索引"
OUT_JSON = os.path.join(INDEX_DIR, "anim_address_map.json")

# assets_full 里非 bundle 的那几个目录（内置文件 / 场景），没有 AssetBundle 容器
NON_BUNDLE_DIRS = {"globalgamemanagers", "level0", "level1", "resources",
                   "sharedassets0", "sharedassets1"}

# 与 extract_full.py 一致的正则（extract_full.py:19）
SAFE_RE = re.compile(r'[\\/:*?"<>|\x00-\x1f]')

# 容器键：32 位 hex = 资产 GUID；否则多半是场景 bundle 的资产路径
GUID_RE = re.compile(r"^[0-9a-fA-F]{32}$")

# extract_full.py 里"不写 .json"的类型（交给 fix_exports.py 写别的扩展名）。
# 只用于报「这一条为什么没有 .json dump」，不参与 join。
NON_JSON_TYPES = ["Texture2D", "Mesh", "Font", "TextAsset", "VideoClip", "AudioClip"]


def safe_name(name, fallback):
    """抄自 d:/2/Warpforge_tools/scripts/extract_full.py:19-21（逐字一致，别改）"""
    name = SAFE_RE.sub("_", (name or "").strip())
    return name[:120] or fallback


def find_bundle_src():
    for d in BUNDLE_SRC_CANDIDATES:
        if os.path.isdir(d):
            return d
    return None


def rel(p):
    return p.replace("\\", "/")


def bundle_fail_counts(assets_root):
    """读 extract_full.py 自己写的 _stats.json，看有没有"没 dump 出来"的对象。

    这条很要紧：本脚本的 dump 路径是**按命名规则反查磁盘**，如果某对象当初 dump 失败
    （没写文件），反查就可能落到同名的兄弟身上 ⇒ 静默错。实测 84 个 bundle 全 0 fail，
    所以这条规则对 bundle 对象是安全的；一旦不为 0，本脚本会在报告里点名。
    """
    p = os.path.join(assets_root, "_stats.json")
    if not os.path.isfile(p):
        return None, None
    try:
        with io.open(p, encoding="utf-8") as fh:
            st = json.load(fh)
    except Exception as e:
        return None, "读不动 _stats.json: %s" % e
    bad = {k: v.get("fail") for k, v in st.items()
           if v.get("fail") and k.endswith(".bundle")}
    return bad, None


def catalog_hex_set(bundle_src):
    """Addressables 的 catalog.bin 里出现的所有 32 位 hex 串。

    只当**旁证**用：判"这个 GUID 根本不在任何 catalog 里"（= 该资产没随包发，
    是悬空引用），或"在 catalog 里但 bundle 没随包"。找不到 catalog.bin 就返回 None，
    调用方如实降级，不编理由。
    """
    p = os.path.join(os.path.dirname(bundle_src), "catalog.bin")
    if not os.path.isfile(p):
        return None, None
    try:
        with io.open(p, "rb") as fh:
            raw = fh.read()
    except OSError as e:
        return None, "读不动 catalog.bin: %s" % e
    return set(m.decode("ascii").lower()
               for m in re.findall(rb"[0-9a-f]{32}", raw)), rel(p)


# ---------------------------------------------------------------- ①·b catalog 补一跳


def decode_catalog_json(path):
    """Addressables `catalog_main.json` → (keys, ents, internalIds, buckets)。

    格式与 `工具/_guid_resolve_probe.py:56-79` 相同（同一份 Unity Addressables 目录序列化）：
      · `m_KeyDataString`    = [int32 n] + n × (u8 type=0 + int32 len + utf8)
      · `m_EntryDataString`  = [int32 n] + n × 7 × int32
      · `m_BucketDataString` = [int32 ?] 后接 (count, ...indices) 的桶表
    """
    import base64
    import struct
    d = json.load(io.open(path, encoding="utf-8"))
    kb = base64.b64decode(d["m_KeyDataString"])
    n = struct.unpack_from("<i", kb, 0)[0]
    keys, o = [], 4
    for _ in range(n):
        t = kb[o]
        o += 1
        if t == 0:
            l = struct.unpack_from("<i", kb, o)[0]
            o += 4
            keys.append(kb[o:o + l].decode("utf-8"))
            o += l
        else:
            raise RuntimeError("m_KeyDataString 里出现未知键类型 %d" % t)
    eb = base64.b64decode(d["m_EntryDataString"])
    ne = struct.unpack_from("<i", eb, 0)[0]
    ents = [struct.unpack_from("<7i", eb, 4 + 28 * i) for i in range(ne)]
    bb = base64.b64decode(d["m_BucketDataString"])
    ni = (len(bb) - 4) // 4
    ints = struct.unpack_from("<%di" % ni, bb, 4)
    buckets, i = [], 0
    while i < len(ints):
        cnt = ints[i + 1]
        buckets.append(ints[i + 2:i + 2 + cnt])
        i += 2 + cnt
    return keys, ents, d["m_InternalIds"], buckets


_PROBE = {"tried": False, "mod": None}


def _probe_module():
    """按文件路径加载同目录 `_guid_resolve_probe.py`（`python -I` 不会把脚本目录放进 sys.path）。"""
    if _PROBE["tried"]:
        return _PROBE["mod"]
    _PROBE["tried"] = True
    try:
        import importlib.util
        p = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_guid_resolve_probe.py")
        spec = importlib.util.spec_from_file_location("wf_guid_resolve_probe", p)
        mod = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(mod)
        _PROBE["mod"] = mod
    except Exception as e:                                  # noqa: BLE001
        print("⚠️ 加载 `_guid_resolve_probe.py` 失败（%s）⇒ 二进制 catalog 用不了" % e)
    return _PROBE["mod"]


def iter_catalog_locations(path):
    """catalog（`.bin` 或旧 `.json`）→ 逐条 `(guid, internalId, [候补束...], 类名)`。

    · **`.bin`（首选）**：复用 `工具/_guid_resolve_probe.py` 的 `_load_catalog_bin`
      （同一份解析实现，⛔ 不写第二份）。它的 `buckets[ki]` 是 location id、
      `ents[lid] = (internalId, 0, 依赖束 lid 列表, …, rtypeIdx)`、`ids[lid]` = internalId。
    · **`.json`（兜底）**：用本文件的 `decode_catalog_json`（与 probe 同一套字段布局）。

    两个分支产出**同一种**四元组，调用方不必知道来源。
    """
    if path.lower().endswith(".json"):
        keys, ents, ids, buckets = decode_catalog_json(path)
        rtypes = json.load(io.open(path, encoding="utf-8"))["m_resourceTypes"]
        for ki, k in enumerate(keys):
            if not GUID_RE.match(k):
                continue
            for ei in buckets[ki]:
                e = ents[ei]
                iid = ids[e[0]]
                depkey = keys[e[2]] if 0 <= e[2] < len(keys) else None
                cand = []
                if isinstance(depkey, str) and depkey.lstrip("-").isdigit():
                    for be in buckets[e[2]]:
                        cand.append(ids[ents[be][0]])
                elif isinstance(depkey, str) and depkey.endswith(".bundle"):
                    cand.append(depkey)
                rt = rtypes[e[6]] if 0 <= e[6] < len(rtypes) else None
                cls = rt.get("m_ClassName") if isinstance(rt, dict) else str(rt)
                yield k.lower(), iid, cand, (cls or "").split(".")[-1]
        return

    mod = _probe_module()
    if mod is None:
        raise RuntimeError("`_guid_resolve_probe.py` 不可用")
    cat = mod._load_catalog_bin(path)
    keys, buckets = cat["keys"], cat["buckets"]
    ents, ids, rtypes = cat["ents"], cat["ids"], cat["rtypes"]
    for ki, k in enumerate(keys):
        if not isinstance(k, str) or not GUID_RE.match(k):
            continue
        for lid in buckets[ki]:
            e = ents.get(lid)
            if not e:
                continue
            cand = [ids[d] for d in (e[2] or []) if isinstance(d, int) and 0 <= d < len(ids)]
            rt = rtypes[e[6]] if e[6] is not None and 0 <= e[6] < len(rtypes) else None
            cls = rt.get("m_ClassName") if isinstance(rt, dict) else str(rt)
            yield k.lower(), ids[lid], cand, (cls or "").split(".")[-1]


def load_catalog_internal_ids(bundle_dirs):
    """catalog → `{(束文件名, internalId): (GUID, 类名)}`（只收 GUID 键、只留本地束）。

    **为什么需要这一跳（2026-10-19 `T15` / `A2131`）**：
      EC（帝皇之子）的 cardassets / cardanims 两个束，`m_Container` 的键是**短 internalId**
      （`bc` / `0d7` / `eac` …，= 真 GUID 的前缀截断），**不是 GUID**。只枚举容器项时，
      这些资产就**没有 GUID 身份** —— `guid_to_asset` / `cardanim_guid_to_name` 里查不到
      （实测 EC 卡引用的 **27 个 CardAnim GUID 全缺**）。
      catalog 正好给出「GUID → (束, internalId)」，补上这一跳就能把短键换回真 GUID。
      判据：`资料/普查产出_1018/R_K11_K12_报告.md` §五·1（EC29 全链例子：
      `bc43540f…` → CardAnim `Rally EC Heldrake` → `67eb257e…` → prefab `EC Heldrake Rally`）。

    🔴 **只取依赖桶里的【第一个】束**（2026-10-19 实测）：catalog 的 `dependencies` 桶
    把**资产所在的主束排在第一**，后面是依赖束（`tweenandshakes` 等）。依赖束里也可能
    有同名的短键（`a` / `23` …）指向**完全不同的对象** —— 全收会把 Texture2D 对到 AudioClip。
    实测：只取第一束后，旧表 148 行里 **exact=True 的 117 行全部「束名 + 容器键」逐字对上**；
    exact=False 的 31 行（目录的键被截断、旧表靠退键解出来的那一族）则对不上 ⇒ 调用方跳过它们。

    ⚠️ 目录里的束名有**已知字符串损坏**（`...emperirschildren...`；
      `_guid_resolve_probe.py:127-145` 记过）⇒ 精确匹配失败时用 difflib 兜底。

    返回 `(pairs, catalogPath|None, error|None)`；找不到 catalog 时**如实降级**，不编理由。
    同一 `(束, internalId)` 对到两个不同 GUID ⇒ 该键记 `None`（歧义，不升级）。

    🆕 **2026-10-23（工具线 `T14`）**：catalog **优先用二进制**（安装版 `aa/catalog.bin` →
    LocalLow `catalog_main.bin`），旧 `catalog_main.json` 只作最后兜底 —— 理由见
    `CATALOG_BIN_CANDIDATES` 上方那段（旧 JSON 与本地束不是同一 build，EC 短键被截断成
    另一个键 ⇒ 10 条真错）。两条路走**同一个** `iter_catalog_locations()`。
    """
    import difflib
    bin_path, bin_why = next(((p, w) for p, w in CATALOG_BIN_CANDIDATES
                              if os.path.isfile(p)), (None, None))
    json_path = next((p for p in CATALOG_JSON_CANDIDATES if os.path.isfile(p)), None)
    path = bin_path or json_path
    if path is None:
        return {}, None, ("catalog 找不到（二进制试过 %d 个、JSON 试过 %d 个）" %
                          (len(CATALOG_BIN_CANDIDATES), len(CATALOG_JSON_CANDIDATES)))
    try:
        locs = list(iter_catalog_locations(path))
    except Exception as e:                                  # noqa: BLE001
        return {}, rel(path), "catalog 解不开（%s）: %s" % (os.path.basename(path), e)

    local = [d[len("bundle_"):] + ".bundle" for d in bundle_dirs
             if d.startswith("bundle_")]
    local_set = set(local)
    _match_cache = {}

    def norm_bundle(b):
        b = os.path.basename((b or "").replace("\\", "/"))
        if b.endswith(".bundle"):
            head, _, tail = b[:-7].rpartition("_")
            if len(tail) == 32 and all(c in "0123456789abcdef" for c in tail.lower()):
                b = head + ".bundle"          # 去掉构建哈希
        return b

    def match_local(b):
        if b in _match_cache:
            return _match_cache[b]
        r = b if b in local_set else None
        if r is None:
            m = difflib.get_close_matches(b, local, n=1, cutoff=0.85)
            r = m[0] if m else None
        _match_cache[b] = r
        return r

    pairs = {}
    for g, iid, cand, cls in locs:
        if not cand:
            continue
        lb = match_local(norm_bundle(cand[0]))     # 第一束 = 资产主束
        if not lb:
            continue
        key2 = (lb, iid)
        if key2 in pairs:
            if pairs[key2] is not None and pairs[key2][0] != g:
                pairs[key2] = None            # 同 (束, 短键) 对到两个 GUID ⇒ 歧义
        else:
            pairs[key2] = (g, (cls or "").split(".")[-1])
    return {k: v for k, v in pairs.items() if v}, rel(path), None


# 旧表（2025-07-30 目录快照建的那份）—— 只用来**校验** catalog 补跳，不参与正向 join。
# 它有 `exact` 列：`False` = 目录给的 internalId 在束里取不到、靠退键解出来的那一族。
OLD_GUID_MAP = r"d:/4/Unity/资料/普查产出_第二十七会话/guid_map.tsv"


def load_old_guid_map(path=OLD_GUID_MAP):
    """旧 `guid_map.tsv` → `{guid: row}`；读不到如实返回错误串（调用方降级）。"""
    import csv
    if not os.path.isfile(path):
        return {}, "旧表不存在: %s" % rel(path)
    out = {}
    try:
        with io.open(path, encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                g = (r.get("assetGuid") or "").lower()
                if g:
                    out[g] = r
    except Exception as e:
        return {}, "旧表读不动（%s）: %s" % (rel(path), e)
    return out, None


def load_old_guid_map_multi(path=OLD_GUID_MAP):
    """旧 `guid_map.tsv` → `{guid: [row, ...]}`（**保留同 GUID 的多行**）。

    为什么需要：`exact=False` 的 56 行里，22 个 GUID 各有 2 行
    （同一资产的 **Texture2D + Sprite** 两个容器项），8 个音频各 1 行，1 个有 4 行。
    `load_old_guid_map` 是「后写覆盖」，只留最后一行 —— 退键族升级要**按行逐条核对**，
    ⛔ 不能只拿最后一条（否则会把 Texture2D 丢掉、只剩 Sprite）。
    """
    import csv
    if not os.path.isfile(path):
        return {}, "旧表不存在: %s" % rel(path)
    out = defaultdict(list)
    try:
        with io.open(path, encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                g = (r.get("assetGuid") or "").lower()
                if g:
                    out[g].append(r)
    except Exception as e:
        return {}, "旧表读不动（%s）: %s" % (rel(path), e)
    return out, None


# `m_resourceTypes` 里的引擎类名（其余一律当 MonoBehaviour 子类）
_ENGINE_CLASSES = {
    "Sprite", "Texture2D", "Texture", "Texture3D", "Cubemap", "RenderTexture",
    "AudioClip", "Mesh", "Material", "Shader", "ComputeShader", "Font",
    "TextAsset", "VideoClip", "GameObject", "AnimationClip", "AnimatorController",
    "LightmapSettings", "NavMeshData", "SceneAsset", "DefaultAsset", "ScriptableObject",
}


def type_compatible(cat_cls, obj_type):
    """catalog 的 `m_ClassName` 与束里对象的类型是否相容。

    · 完全相同 → 相容；
    · `Sprite` 与 `Texture2D` 是同一张图的两种取法（容器项指哪个都可能）→ 互认；
    · catalog 写的是**具体 MonoBehaviour 子类**（`CardAnim` / `ScenarioEnvironmentConditionSO` …），
      束里的 `type` 只会是 `MonoBehaviour` → 认。
    """
    if not cat_cls or not obj_type:
        return False
    if cat_cls == obj_type:
        return True
    if cat_cls in ("Sprite", "Texture2D") and obj_type in ("Sprite", "Texture2D"):
        return True
    if obj_type == "MonoBehaviour" and cat_cls not in _ENGINE_CLASSES:
        return True
    return False


# ---------------------------------------------------------------- ① 容器项


def read_containers(assets_root):
    """扫 assets_full 下所有 bundle 的 AssetBundle dump，收 m_Container。

    返回 (entries, bundles, problems)
    """
    entries = []
    bundles = []
    problems = []
    for d in sorted(os.listdir(assets_root)):
        bdir = os.path.join(assets_root, d)
        if not os.path.isdir(bdir):
            continue
        ab_dir = os.path.join(bdir, "AssetBundle")
        if not os.path.isdir(ab_dir):
            if d not in NON_BUNDLE_DIRS:
                problems.append("无 AssetBundle 目录（非 bundle 包？）: %s" % d)
            continue
        jsons = sorted(f for f in os.listdir(ab_dir) if f.endswith(".json"))
        if not jsons:
            problems.append("AssetBundle 目录为空: %s" % d)
            continue
        if len(jsons) > 1:
            problems.append("AssetBundle 目录有 %d 个 json（本脚本只认第一个）: %s"
                            % (len(jsons), d))
        jf = os.path.join(ab_dir, jsons[0])
        try:
            with io.open(jf, encoding="utf-8") as fh:
                j = json.load(fh)
        except Exception as e:
            problems.append("读不动 %s: %s" % (rel(jf), e))
            continue
        cont = j.get("m_Container")
        if not isinstance(cont, list):
            problems.append("m_Container 不是 list: %s" % rel(jf))
            continue
        n0 = len(entries)
        for item in cont:
            if not (isinstance(item, list) and len(item) == 2):
                problems.append("容器项形状不对 %s: %r" % (rel(jf), item))
                continue
            guid, val = item
            asset = (val or {}).get("asset") or {}
            pid = asset.get("m_PathID")
            if guid is None or pid is None:
                problems.append("容器项缺 guid/pathId %s: %r" % (rel(jf), item))
                continue
            entries.append({
                "guid": str(guid),
                # 场景 bundle 的键是**资产路径**（`Assets/Data/Scenes/….unity`），不是 GUID。
                # 本脚本"全都收"，但标出来，免得下游把路径当 GUID 使。
                "keyKind": "guid" if GUID_RE.match(str(guid)) else "assetPath",
                "bundleDir": d,
                "bundleFile": j.get("m_Name") or "",
                "pathId": int(pid),
                "fileId": int(asset.get("m_FileID", 0)),
                "preloadIndex": (val or {}).get("preloadIndex"),
                "preloadSize": (val or {}).get("preloadSize"),
            })
        bundles.append({
            "dir": d,
            "assetBundleJson": rel(os.path.relpath(jf, assets_root)),
            "cabName": j.get("m_Name") or "",
            "containerCount": len(entries) - n0,
        })
    return entries, bundles, problems


# ---------------------------------------------------------------- ② path_id → (类型, 名字)


def object_name(obj):
    """拿对象的 m_Name。先走 typetree（不触发贴图解码/音频解码），失败再退 read()。"""
    try:
        tt = obj.read_typetree()
        if isinstance(tt, dict):
            nm = tt.get("m_Name")
            if nm:
                return nm
    except Exception:
        pass
    try:
        nm = getattr(obj.read(), "m_Name", None)
        if nm:
            return nm
    except Exception:
        pass
    return None


def resolve_bundle(src_path, wanted_ids):
    """UnityPy 重读原 bundle → ({path_id: {type,name}}, missing_ids, err)"""
    import UnityPy
    try:
        env = UnityPy.load(src_path)
        objs = list(env.objects)
    except Exception as e:
        return {}, sorted(wanted_ids), "UnityPy load 失败: %s" % e
    byid = {o.path_id: o for o in objs}
    resolved = {}
    missing = []
    for pid in sorted(wanted_ids):
        o = byid.get(pid)
        if o is None:
            missing.append(pid)
            continue
        resolved[pid] = {"type": o.type.name, "name": object_name(o)}
    return resolved, missing, None


# ---------------------------------------------------------------- dump 文件名解析（磁盘侧，精确）


class DumpIndex:
    """bundle 目录 → 类型目录 → {文件名(去扩展): 扩展名}，用来把 (类型, 名字, pathId) 落成
    磁盘上的实际 dump 文件。

    命名规则要按**两个**导出脚本分别记（它们用名字的类型集合不一样）：

      · `extract_full.py:183-198`（写 .json）—— 只有这 6 类读 `m_Name`：
            ("MonoBehaviour","GameObject","Sprite","Texture2D","AudioClip","Mesh")
        规则：`{safeName}_{pathId}.json`（同名重复才有后缀）→ `{safeName}.json`（同名第一个）
      · `fix_exports.py:119-120`（写 .png/.obj/.ttf/.txt/.mp4）—— 这 6 类读 `m_Name`：
            ("Texture2D","AudioClip","Mesh","Font","VideoClip","TextAsset")
        规则：`{safeName}.<ext>` —— **没有 pathId 消歧后缀**，同名直接覆盖（最后一个赢）
      · **两边都不在的类型一律用兜底名** `{Type}_{path_id}`：
        Material / SpriteAtlas / AnimationClip / ComputeShader / AudioMixer* /
        Cubemap / LightProbes / VisualEffectAsset / Shader / Transform …
        实测 `battleprefabs…/Material/` 下 90 个文件**全部**叫 `Material_<pid>.json`，
        一个材质名都没有 —— 别拿名字去猜。
      · 全都查不到 ⇒ 返回 None，如实记进 unresolved，不猜。
    """

    # extract_full.py 主循环里会去读 m_Name 的类型
    EXTRACT_NAME_TYPES = {"MonoBehaviour", "GameObject", "Sprite", "Texture2D",
                          "AudioClip", "Mesh"}
    # fix_exports.py 里会去读 m_Name 的类型
    FIX_NAME_TYPES = {"Texture2D", "AudioClip", "Mesh", "Font", "VideoClip",
                      "TextAsset"}

    def __init__(self, assets_root, bundle_dirs):
        self.by_name = defaultdict(dict)    # (bundle, 类型目录) -> {文件名(无扩展): 扩展名}
        for b in bundle_dirs:
            bdir = os.path.join(assets_root, b)
            if not os.path.isdir(bdir):
                continue
            for tn in sorted(os.listdir(bdir)):
                tdir = os.path.join(bdir, tn)
                if not os.path.isdir(tdir):
                    continue
                files = {}
                for f in os.listdir(tdir):
                    stem, ext = os.path.splitext(f)
                    files.setdefault(stem, ext)
                if files:
                    self.by_name[(b, tn)] = files

    def _candidates(self, type_name, name, path_id):
        """按优先级排出候选文件名（不带扩展），与两个导出脚本的写法一一对应。"""
        cands = []
        if name and (type_name in self.EXTRACT_NAME_TYPES
                     or type_name in self.FIX_NAME_TYPES):
            base = safe_name(name, None)
            if type_name in self.EXTRACT_NAME_TYPES:
                cands.append("%s_%d" % (base, path_id))   # 同名重复 → 带 pathId
            cands.append(base)                            # 同名第一个 / fix_exports 的写法
        cands.append("%s_%d" % (type_name, path_id))      # 兜底名（非名字类型必走这里）
        return cands

    def path_for(self, bundle, type_name, name, path_id):
        files = self.by_name.get((bundle, type_name))
        if not files:
            return None
        for cand in self._candidates(type_name, name, path_id):
            if cand in files:
                return "%s/%s/%s%s" % (bundle, type_name, cand, files[cand])
        return None

    def name_hit(self, bundle, type_name, name, path_id):
        """诊断用：区分命中方式，别把几种"没定位到"混成一种原因。"""
        files = self.by_name.get((bundle, type_name))
        if not files:
            return "磁盘上无该类型目录"
        cands = self._candidates(type_name, name, path_id)
        for i, cand in enumerate(cands):
            if cand in files:
                if i == 0 and cand != cands[-1]:
                    return "命中: 名+pathId 后缀（同名重复）"
                if cand != cands[-1]:
                    return "命中: 裸名（同名里的第一个 / fix_exports 的写法）"
                return "命中: 兜底名 {Type}_{pathId}"
        return "磁盘上没有对应 dump 文件"


# ---------------------------------------------------------------- ③ CardAnim 侧


def scan_cardanims(assets_root, bundle_dirs):
    """扫给定 bundle 的 MonoBehaviour dump，收 animInfo.animAdressable。

    返回 (hits, no_guid, problems)
    """
    hits = []
    no_guid = []
    problems = []
    needle = b'"animAdressable"'
    n_scan = 0
    for d in bundle_dirs:
        mono_dir = os.path.join(assets_root, d, "MonoBehaviour")
        if not os.path.isdir(mono_dir):
            continue
        for fn in sorted(os.listdir(mono_dir)):
            if not fn.endswith(".json"):
                continue
            fp = os.path.join(mono_dir, fn)
            try:
                with io.open(fp, "rb") as fh:
                    raw = fh.read()
            except OSError as e:
                problems.append("读不动 %s: %s" % (rel(fp), e))
                continue
            if needle not in raw:          # 快筛，省掉 6 万次 json.load
                continue
            n_scan += 1
            try:
                j = json.loads(raw.decode("utf-8"))
            except Exception as e:
                problems.append("解析不动 %s: %s" % (rel(fp), e))
                continue
            ai = j.get("animInfo")
            if not isinstance(ai, dict) or "animAdressable" not in ai:
                continue
            ad = ai.get("animAdressable") or {}
            guid = (ad.get("m_AssetGUID") or "").strip()
            stem = fn[:-5]
            item = {
                "cardAnim": j.get("m_Name") or stem,
                "monoStem": stem,
                "bundleDir": d,
                "monoFile": "%s/MonoBehaviour/%s" % (d, fn),
                "subObjName": ad.get("m_SubObjectName") or "",
                "subObjType": ad.get("m_SubObjectType") or "",
            }
            if guid:
                item["guid"] = guid
                hits.append(item)
            else:
                no_guid.append(item)
    return hits, no_guid, problems, n_scan


# ---------------------------------------------------------------- 输入表覆盖率


def _read_json(path):
    with io.open(path, encoding="utf-8") as fh:
        return json.load(fh)


def load_input_tables(cardanim_to_asset=None, guid_to_asset=None):
    """三类输入表：**只用来交叉校验**（不参与建表）。

    每张表抽成 (用于对齐的名字, guid) 二元组：
      · animinfo_lookup.json  —— 键是"逻辑名"，值里 `m` = MonoBehaviour 文件名、`g` = guid 前 8 位
      · animinfo_0824.json    —— 键 = MonoBehaviour 文件名，值 = 完整 guid
      · card_anim_map.json    —— 阵营 → VFX 名 → 完整 guid
    对齐后逐条比"我这表里这个名字的 guid == 它写的 guid"，**0 条不符才算对**。
    """
    out = {}
    all_guids = set((guid_to_asset or {}).keys())

    def crosscheck(pairs):
        res = {"rows": len(pairs), "nameFound": 0, "nameMiss": 0,
               "guidMatch": 0, "guidBothBlank": 0, "guidMismatch": 0,
               "mismatchExamples": []}
        for alias, guid in pairs:
            e = (cardanim_to_asset or {}).get(alias)
            if e is None:
                res["nameMiss"] += 1
                continue
            res["nameFound"] += 1
            g = (guid or "").strip().lower()
            mine = (e.get("guid") or "").lower()
            if not g and not mine:
                res["guidBothBlank"] += 1
            elif g and (mine == g or (len(g) < 32 and mine.startswith(g))):
                res["guidMatch"] += 1
            else:
                res["guidMismatch"] += 1
                if len(res["mismatchExamples"]) < 5:
                    res["mismatchExamples"].append(
                        {"name": alias, "table": g or "(空)", "mine": mine or "(空)"})
        return res

    def add(name, path, pairs, note):
        rec = {"path": rel(path), "count": len(pairs), "note": note}
        rec["guids"] = len(set(g for _, g in pairs if g))
        rec["guidHits"] = sum(1 for _, g in pairs
                              if g and (g in all_guids
                                        or any(k.startswith(g) for k in all_guids)))
        rec.update(crosscheck(pairs))
        out[name] = rec

    def try_load(name, path, builder, note):
        if not os.path.isfile(path):
            out[name] = {"path": rel(path), "error": "文件不存在"}
            return
        try:
            add(name, path, builder(_read_json(path)), note)
        except Exception as e:
            out[name] = {"path": rel(path), "error": str(e)}

    def b_lookup(j):
        # 键只是逻辑名，真正能对齐的是 m（MonoBehaviour 文件名）
        return [(str((v or {}).get("m") or k), (v or {}).get("g") or "")
                for k, v in j.items()]

    def b_0824(j):
        return [(k, (((v or {}).get("animAdressable") or {})
                     .get("m_AssetGUID") or "")) for k, v in j.items()]

    def b_camap(j):
        pairs = []
        for fac, entries in j.items():
            if str(fac).startswith("_") or not isinstance(entries, dict):
                continue
            for nm, e in entries.items():
                pairs.append((nm, (e or {}).get("guid") or ""))
        return pairs

    try_load("animinfo_lookup.json", os.path.join(DATA_DIR, "animinfo_lookup.json"),
             b_lookup, "对齐用 `m`（MonoBehaviour 文件名）；`g` 只有 8 位，按前缀比")
    try_load("animinfo_0824.json", os.path.join(INDEX_DIR, "animinfo_0824.json"),
             b_0824, "键 = MonoBehaviour 文件名")
    try_load("card_anim_map.json", os.path.join(DATA_DIR, "card_anim_map.json"),
             b_camap, "阵营 → VFX 名 → 完整 guid")
    return out


# ---------------------------------------------------------------- 挑"主"资产


def pick_primary(recs):
    """同一 GUID 命中多个对象时挑一个"主"的。

    实测 Addressables 会把「主资产 + 它的子资产」挂在同一个 GUID 下
    （例：`008b2df23284546f29acb83a7d8ea785` → Texture2D
    `40k_Cardframe_stratagem_SaimHann_SDF_tier4` + 同名 Sprite）。
    挑法：能直接当"特效/prefab"用的类型优先。
    """
    def score(r):
        t = r.get("type") or ""
        s = 0
        if t == "GameObject":
            s += 40
        elif t in ("VisualEffectAsset", "AnimationClip", "AnimatorController",
                   "Material", "Sprite", "Texture2D", "MonoBehaviour"):
            s += 10
        if r.get("name"):
            s += 3
        if r.get("dump"):
            s += 1
        if "unresolved" in r:
            s -= 100
        return s
    return sorted(recs, key=lambda r: (-score(r), r["bundle"], r["pathId"]))[0]


# ---------------------------------------------------------------- 主流程


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--quiet", action="store_true", help="只打统计，不打未解出明细")
    ap.add_argument("--limit-bundles", type=int, default=0,
                    help="调试用：只处理前 N 个 bundle")
    ap.add_argument("--dry", action="store_true",
                    help="只打印统计，不落盘（`A1648` 同族：只想看时不许写产物）")
    ap.add_argument("--stamp", default=None,
                    help="覆盖 generatedAt（复跑 md5 比对用；默认 = 当前时间）")
    # 🆕 2026-10-23（T14）：干跑落点（⛔ 默认仍写正式产物；干跑请显式给 `_tmp_view/...`）
    ap.add_argument("--out", default=OUT_JSON,
                    help="输出路径（默认 %s；干跑时指到 _tmp_view/）" % OUT_JSON)
    args = ap.parse_args()

    t0 = time.time()
    if not os.path.isdir(ASSETS_FULL):
        print("!! 找不到解包资源目录: %s" % ASSETS_FULL)
        return 2
    bundle_src = find_bundle_src()
    if not bundle_src:
        print("!! 找不到原 bundle 源目录，试过:")
        for d in BUNDLE_SRC_CANDIDATES:
            print("   -", d)
        return 2

    print("=" * 78)
    print("gen_anim_address_map.py")
    print("  assets_full = %s" % rel(ASSETS_FULL))
    print("  bundle_src  = %s" % rel(bundle_src))

    # ---- ① 容器项
    entries, bundles, problems = read_containers(ASSETS_FULL)
    print("[1/5] 读容器项：%d 个 bundle，%d 条 m_Container，%d 条结构问题"
          % (len(bundles), len(entries), len(problems)))
    for p in problems[:5]:
        print("       ! %s" % p)

    if args.limit_bundles:
        keep = set(b["dir"] for b in bundles[:args.limit_bundles])
        entries = [e for e in entries if e["bundleDir"] in keep]
        bundles = [b for b in bundles if b["dir"] in keep]

    # ---- ①·b catalog 读一跳（真正升级要等 ② 的 `resolved` 出来：要按对象类型/名字校验）
    catalog_pairs, catalog_json, catalog_err = load_catalog_internal_ids(
        [b["dir"] for b in bundles])

    want = defaultdict(set)
    for e in entries:
        want[e["bundleDir"]].add(e["pathId"])

    # ---- ② UnityPy 反解 path_id
    resolved = {}
    missing = defaultdict(list)
    load_errors = []
    for i, b in enumerate(bundles):
        d = b["dir"]
        src = os.path.join(bundle_src, d[len("bundle_"):] + ".bundle")
        if not os.path.isfile(src):
            load_errors.append("源 bundle 不存在: %s" % rel(src))
            missing[d] = sorted(want[d])
            continue
        res, miss, err = resolve_bundle(src, want[d])
        if err:
            load_errors.append("%s: %s" % (d, err))
            missing[d] = sorted(want[d])
            continue
        for pid, v in res.items():
            resolved[(d, pid)] = v
        if miss:
            missing[d] = miss
        if (i + 1) % 10 == 0 or (i + 1) == len(bundles):
            print("       [2/5] %d/%d 包已反解（%.0fs）"
                  % (i + 1, len(bundles), time.time() - t0))

    n_missing = sum(len(v) for v in missing.values())
    print("[2/5] path_id 反解：命中 %d / 缺 %d（其中 pathId==0 的 %d 条）；"
          "源 bundle load 问题 %d 条"
          % (len(resolved), n_missing, sum(1 for v in missing.values()
                                           for x in v if x == 0), len(load_errors)))
    for e in load_errors[:5]:
        print("       ! %s" % e)
    fails, ferr = bundle_fail_counts(ASSETS_FULL)
    if ferr:
        print("       ! %s" % ferr)
    elif fails:
        print("       ! ⚠️ 有 bundle 的 dump 失败过（会让「按名字反查」落错人）: %s" % fails)
    else:
        print("       ✓ _stats.json: 所有 bundle 的 dump fail 全为 0"
              "（按名字反查 dump 文件这条路对 bundle 对象是安全的）")

    # ---- ①·b catalog 补一跳（第二步：校验后把「短 internalId」换回真 GUID）
    #
    # 三条闸门（缺一不可）：
    #   ① 旧表（`资料/普查产出_第二十七会话/guid_map.tsv`）**有**这个 GUID ⇒ 必须
    #      `exact=True` 且**资产名逐字相同**。`exact=False` 是「目录的键被截断、旧表靠退键
    #      解出来」的那一族 —— 2026-10-19 实测：拿 catalog 的 internalId 去束里取，
    #      **31/31 全取到别的对象**（Texture2D 对到 AudioClip 等）。必须整族跳过。
    #   ② 旧表**没有**这个 GUID ⇒ 按**类型**兜底：catalog 的 `m_ClassName` 与束里对象的
    #      类型必须相容（`Sprite`/`Texture2D` 互认；非引擎类名 = MonoBehaviour 子类）。
    #   ③ 同一 `(束, 短键)` 对到两个 GUID ⇒ 已在 `load_catalog_internal_ids` 里记歧义、跳过。
    old_map, old_map_err = load_old_guid_map()
    upgraded = []
    up_why = Counter()
    for e in entries:
        if e["keyKind"] == "guid" or e["pathId"] == 0:
            continue
        bf = e["bundleDir"][len("bundle_"):] + ".bundle"
        pv = catalog_pairs.get((bf, e["guid"]))
        if not pv:
            continue
        g, cls = pv
        r = resolved.get((e["bundleDir"], e["pathId"])) or {}
        obj, nm = r.get("type"), r.get("name")
        if g in old_map:
            o = old_map[g]
            if o.get("exact") != "True":
                up_why["旧表 exact=False（目录键被截断，catalog 的 iid 取到别的对象）"] += 1
                continue
            if o.get("name") != nm:
                up_why["旧表名字与束里对象不符"] += 1
                continue
        elif not type_compatible(cls, obj):
            up_why["非旧表条目 && 类型不符"] += 1
            continue
        ne = dict(e)
        ne["shortKey"] = e["guid"]          # 保留短键，便于追溯
        ne["guid"] = g
        ne["keyKind"] = "guid"
        ne["viaCatalog"] = True
        ne["catalogClass"] = cls
        upgraded.append(ne)
    entries.extend(upgraded)
    if catalog_err:
        print("       catalog: %s ⇒ 短键容器项**不做 GUID 升级**" % catalog_err)
    else:
        print("       catalog: %s%s；短键容器项校验后升级 **%d 条**"
              "（跳过 %d 条：%s）"
              % (catalog_json, "（二进制，与本地束同 build）"
                 if str(catalog_json).lower().endswith(".bin") else "（JSON 兜底）",
                 len(upgraded), sum(up_why.values()), dict(up_why)))
    if old_map_err:
        print("       ! 旧表校验降级：%s（非旧表条目只走类型兜底）" % old_map_err)

    # ---- ①·b·2 旧表 `exact=False`（catalog 键被截断、旧表靠「退键」解出的那一族）
    #
    # 背景：EC 的 cardassets / cardanims 束里，catalog 给这些 GUID 的 internalId 是**截断键**
    # （例 `d`），拿它去束里取会命中**别的对象**（Texture2D 对到 AudioClip）。旧表当时靠
    # 「退键」到 `db9` 才解出正确资产，因此把 `exact` 记成 False；catalog 升级闸门整族跳过。
    #
    # 2026-10-19（T15 残余）实测：这 31 个 GUID / 56 行**有足够判据升级**，判据是**逐行三元组唯一命中** ——
    #   旧表一行给了 `(bundle, containerKey, type, name)`；在 `m_Container` 里按
    #   `(bundleDir, containerKey)` 取到该键下的对象，按 `(type, name)` 过滤，**每行必须恰好 1 个**。
    #   56/56 行命中（Texture2D + Sprite 同名时按 type 分开；AudioClip 独立）。
    # 独立旁证（一次性逐条，详见 `资料/普查产出_1019/W57_特效线_T15_V1_T02静态.md`）：
    #   31/31 的资产名与「引用该 GUID 的卡名」逐条相符（30 个全名、2 个关键 token）。
    # ⇒ 这不是「拿 catalog 的坏 iid 硬升」，而是「按束里真正命中的容器键升」。
    retreat_entries = []
    retreat_why = Counter()
    retreat_guids = set()
    retreat_rows = 0
    if old_map_err:
        print("       ! 退键族升级降级：旧表读不到 ⇒ 0 条")
    else:
        old_multi, old_multi_err = load_old_guid_map_multi()
        if old_multi_err:
            print("       ! 退键族升级降级：%s ⇒ 0 条" % old_multi_err)
            old_multi = {}
        by_bundle_file = {}
        for b in bundles:
            # 两种键都收：① AssetBundle 的 m_Name（可能是哈希名）
            #            ② 目录名反推的友好名 `bundle_<stem>` → `<stem>.bundle`
            # 旧表的 `bundle` 列是**友好名**（= 本地束文件名），所以要按 ② 匹配。
            for bf in (os.path.basename(b.get("bundleFile") or ""),
                       b["dir"][len("bundle_"):] + ".bundle"
                       if b["dir"].startswith("bundle_") else ""):
                if bf:
                    by_bundle_file.setdefault(bf, b["dir"])
        keyed = defaultdict(list)
        for e in entries:
            if e["keyKind"] == "guid":
                continue
            keyed[(e["bundleDir"], str(e["guid"]))].append(e)
        have_guid = set(e["guid"].lower() for e in entries
                        if GUID_RE.match(str(e["guid"])))
        for g, o_rows in old_multi.items():
            rows = [o for o in o_rows if o.get("exact") == "False"]
            if not rows:
                continue
            retreat_rows += len(rows)
            if g in have_guid:
                retreat_why["GUID 已有正式容器键（不需要退键）"] += 1
                continue
            got, fail = [], None
            for o in rows:
                bdir = by_bundle_file.get(os.path.basename(o.get("bundle") or ""))
                if not bdir:
                    fail = "束文件不在本地（或 bundleFile 对不上）"
                    break
                hits = []
                for e in keyed.get((bdir, o.get("containerKey") or ""), []):
                    r = resolved.get((e["bundleDir"], e["pathId"])) or {}
                    if r.get("type") == o.get("type") and r.get("name") == o.get("name"):
                        hits.append(e)
                if len(hits) != 1:
                    fail = "按 (containerKey, type, name) 命中 %d 个（要恰好 1 个）" % len(hits)
                    break
                got.append(hits[0])
            if fail:
                retreat_why[fail] += 1
                continue
            seen_pid = set()
            for e in got:
                sig = (e["bundleDir"], e["pathId"])
                if sig in seen_pid:
                    continue
                seen_pid.add(sig)
                ne = dict(e)
                ne["shortKey"] = e["guid"]
                ne["guid"] = g
                ne["keyKind"] = "guid"
                ne["viaVerifiedRetreat"] = True
                retreat_entries.append(ne)
            have_guid.add(g)
            retreat_guids.add(g)
        entries.extend(retreat_entries)
        print("       退键族（旧表 exact=False）：升级 **%d 个 GUID**"
              "（%d 行 old-map 逐行三元组唯一命中 ⇒ %d 条容器项，同对象去重；"
              "跳过 %d 个 GUID：%s）"
              % (len(retreat_guids), retreat_rows, len(retreat_entries),
                 sum(retreat_why.values()), dict(retreat_why)))

    # ---- ③ 正向表 + dump 路径
    dump_idx = DumpIndex(ASSETS_FULL, [b["dir"] for b in bundles])
    per_guid = defaultdict(list)
    n_no_type = 0
    n_pathid_zero = 0
    for e in entries:
        r = resolved.get((e["bundleDir"], e["pathId"]))
        rec = {
            "bundle": e["bundleDir"],
            "bundleFile": e["bundleFile"],
            "pathId": e["pathId"],
            "type": (r or {}).get("type"),
            "name": (r or {}).get("name"),
        }
        if e["keyKind"] != "guid":
            rec["keyKind"] = e["keyKind"]
        if r is None:
            if e["pathId"] == 0:
                n_pathid_zero += 1
                rec["unresolved"] = (
                    "m_PathID == 0：容器项没带本地对象指针"
                    "（场景 bundle 里这是场景自身；跨包引用这里也解不了）")
            else:
                rec["unresolved"] = "pathId 在原 bundle 里找不到"
        per_guid[e["guid"]].append(rec)

    guid_to_asset = {}
    dup_guids = 0
    dump_hit_kind = Counter()
    n_dump_ok = 0
    for guid, recs in per_guid.items():
        if len(recs) > 1:
            dup_guids += 1
        for rec in recs:
            if rec.get("type") is None:
                rec["dump"] = None
                n_no_type += 1
                continue
            rec["dump"] = dump_idx.path_for(rec["bundle"], rec["type"],
                                            rec["name"], rec["pathId"])
            kind = dump_idx.name_hit(rec["bundle"], rec["type"],
                                     rec["name"], rec["pathId"])
            dump_hit_kind[kind] += 1
            if rec["dump"]:
                n_dump_ok += 1
        primary = pick_primary(recs)
        out = dict(primary)
        others = [r for r in recs if r is not primary]
        if others:
            out["also"] = others
        guid_to_asset[guid] = out

    print("[3/5] 正向表：%d 个唯一容器键（其中 GUID 键 %d 个、非 GUID 键 %d 个）；"
          "%d 个键对应 ≥2 个对象；dump 路径命中 %d 条"
          % (len(guid_to_asset),
             sum(1 for r in per_guid.values() if not r[0].get("keyKind")),
             sum(1 for r in per_guid.values() if r[0].get("keyKind")),
             dup_guids, n_dump_ok))
    for k, v in dump_hit_kind.most_common():
        print("       dump: %-32s %d" % (k, v))

    # Addressables catalog 旁证（判"没解出"的原因用）
    cat_hex, cat_path = catalog_hex_set(bundle_src)
    if cat_hex is None:
        print("       catalog.bin: 没找到（未解出原因只报到 m_Container 那一层）")
        cat_overlap = None
    else:
        cat_overlap = sum(1 for g in guid_to_asset if g.lower() in cat_hex)
        print("       catalog.bin: %s，%d 个 32 位 hex 串；"
              "正向表 %d 个 GUID 里有 %d 个能在 catalog 里找到"
              % (cat_path, len(cat_hex), len(guid_to_asset), cat_overlap))

    # ---- ④ CardAnim 反向表
    anim_bundles = [b["dir"] for b in bundles if "cardanims" in b["dir"]]
    other_bundles = [b["dir"] for b in bundles if "cardanims" not in b["dir"]]
    hits, no_guid, cproblems, n_scan1 = scan_cardanims(ASSETS_FULL, anim_bundles)
    extra_hits, extra_noguid, extra_problems, n_scan2 = scan_cardanims(
        ASSETS_FULL, other_bundles)
    all_hits = hits + extra_hits
    no_guid = no_guid + extra_noguid
    cproblems = cproblems + extra_problems
    print("[4/5] CardAnim 侧：cardanims 包 %d 条 + 别的包 %d 条 = %d 条；"
          "其中 guid 空白 %d 条" % (len(hits), len(extra_hits), len(all_hits),
                                    len(no_guid)))

    cardanim_to_asset = {}
    anim_unresolved = []
    for h in all_hits:
        g = h["guid"]
        tgt = guid_to_asset.get(g)
        rec = {
            "guid": g,
            "targetName": None,
            "targetType": None,
            "targetBundle": None,
            "targetPathId": None,
            "dump": None,
            "sourceBundle": h["bundleDir"],
            "sourceMono": h["monoFile"],
            "subObjName": h["subObjName"],
            "subObjType": h["subObjType"],
        }
        if tgt is None:
            # 分开两条原因：在不在 Addressables catalog 里，指向的是两种不同的"没解出"
            if cat_hex is None:
                rec["unresolved"] = "GUID 不在任何 bundle 的 m_Container 里（catalog.bin 读不到，没做旁证）"
            elif g.lower() in cat_hex:
                rec["unresolved"] = ("GUID 在 catalog.bin 里，但不在任何已随包 bundle 的 "
                                     "m_Container 里（catalog 条目与本地包对不上）")
            else:
                rec["unresolved"] = ("GUID 在 catalog.bin 和所有 bundle 的 m_Container 里都搜不到"
                                     "（该资产根本没随包发，是悬空引用）")
            anim_unresolved.append(dict(rec, cardAnim=h["cardAnim"]))
        else:
            for k_src, k_dst in (("name", "targetName"), ("type", "targetType"),
                                 ("bundle", "targetBundle"),
                                 ("pathId", "targetPathId"), ("dump", "dump")):
                rec[k_dst] = tgt.get(k_src)
            if tgt.get("also"):
                rec["candidates"] = [
                    {"name": tgt.get("name"), "type": tgt.get("type"),
                     "bundle": tgt.get("bundle"), "pathId": tgt.get("pathId"),
                     "dump": tgt.get("dump")}] + [
                    {"name": c.get("name"), "type": c.get("type"),
                     "bundle": c.get("bundle"), "pathId": c.get("pathId"),
                     "dump": c.get("dump")} for c in tgt["also"]]
            if rec["targetName"] is None:
                rec["unresolved"] = "GUID 命中容器项，但 pathId 反解不出 (类型, 名字)"
                anim_unresolved.append(dict(rec, cardAnim=h["cardAnim"]))
            elif rec["dump"] is None:
                rec["dumpMissing"] = dump_idx.name_hit(
                    rec["targetBundle"], rec["targetType"],
                    rec["targetName"], rec["targetPathId"])
        cardanim_to_asset[h["cardAnim"]] = rec

    # guid 空白的也进表 —— 查询任何一个 CardAnim 名都得有回话，不许静默缺
    for h in no_guid:
        cardanim_to_asset[h["cardAnim"]] = {
            "guid": None,
            "targetName": None,
            "targetType": None,
            "targetBundle": None,
            "targetPathId": None,
            "dump": None,
            "sourceBundle": h["bundleDir"],
            "sourceMono": h["monoFile"],
            "subObjName": h["subObjName"],
            "subObjType": h["subObjType"],
            "unresolved": "animInfo.animAdressable.m_AssetGUID 为空 —— 这个 CardAnim 没指向任何资产",
        }

    # 🆕 2026-10-01：**CardAnim 自己的容器 GUID → CardAnim 名字**
    #
    # 为什么必须补这一跳：`cardanim_to_asset` 是**按 CardAnim 名字做键**的，而 AnimFX 数据里存的
    # 是 **`cardAnim.m_AssetGUID`**（= CardAnim 在它自己那个 bundle 的 `m_Container` 里的键）。
    # 少了这一跳，`WFModuleInstanceParticleAdjacent` 就接不上（原先记成「要建导入路」，其实是缺索引）。
    # 材料都是现成的：`entries` 给了 (bundleDir, pathId, guid)，`resolved` 给了 (type, name)。
    _name_to_guid = defaultdict(dict)          # bundleDir → {对象名: 容器 GUID}（容器项指 MonoBehaviour）
    # 🔴 2026-10-05 修：原来这一格**只要 `type == "MonoBehaviour"`**（`!= 就 continue`），
    #   结果把 **2 条 CardAnim 静默筛掉**（`Invoke Minion Card Fade Default` / `… Legendary`
    #   —— 生成器每次都自己报 `cardAnimGuidIndexMissing`，所以没静默失败，但也没修）。
    #   实测根因：这两条的**容器项指的是同名的 `GameObject`**（不是 MonoBehaviour），
    #   而 CardAnim MB 的 `animInfo.animAdressable.m_AssetGUID` 正是那个 GUID
    #   （`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/Invoke Minion Card Fade Default.json`
    #   读出来 = `5e977521119914de5b08bba7f4d2eed6`，与 `cardanim_to_asset` 里那条逐位相同）。
    #   ⇒ 改成**两档**：MonoBehaviour 优先、其余类型进兜底（查不到再回落），
    #   这样既修好这两条，又不会让非 MonoBehaviour 的同名项抢在真身份前面。
    _name_to_guid_fb = defaultdict(dict)       # 同一张表的兜底档：容器项指的不是 MonoBehaviour
    for e in entries:
        r = resolved.get((e["bundleDir"], e["pathId"]))
        if not r or not r.get("name"):
            continue
        tbl = (_name_to_guid if r["type"] == "MonoBehaviour" else _name_to_guid_fb)
        prev = tbl[e["bundleDir"]].get(r["name"])
        # **真 GUID 优先于短 internalId**：①·b 的升级项与短键项同指一个对象时，
        # 选 GUID（短键不是 Addressables 的可寻址身份，查表查不到）。
        if prev is None or (not GUID_RE.match(prev) and GUID_RE.match(e["guid"])):
            tbl[e["bundleDir"]][r["name"]] = e["guid"]
    cardanim_guid_index = {}                   # CardAnim 容器 GUID → CardAnim 名字
    _cg_miss = []
    for h in all_hits + no_guid:
        nm = h["cardAnim"]
        d_mb = _name_to_guid.get(h["bundleDir"], {})
        d_fb = _name_to_guid_fb.get(h["bundleDir"], {})
        # 先按 CardAnim 名找（MonoBehaviour 档优先、兜底档次之），
        # 再退一步按 dump 文件名找（m_Name 与文件名不同名时）。
        g = (d_mb.get(nm) or d_fb.get(nm)
             or d_mb.get(h["monoStem"]) or d_fb.get(h["monoStem"]))
        if g is None:
            _cg_miss.append({"cardAnim": nm, "bundle": h["bundleDir"],
                             "monoFile": h["monoFile"]})
            continue
        cardanim_guid_index[g] = nm
    print("       🆕 CardAnim 容器 GUID 索引：%d 条（收 %d 个 CardAnim，缺 %d 条）"
          % (len(cardanim_guid_index), len(all_hits) + len(no_guid), len(_cg_miss)))
    for m in _cg_miss[:5]:
        print("          ! 没找到容器项: %s（%s）" % (m["cardAnim"], m["monoFile"]))

    n_anim_ok = sum(1 for v in cardanim_to_asset.values() if v.get("targetName"))
    n_anim_dump = sum(1 for v in cardanim_to_asset.values() if v.get("dump"))
    n_anim_noguid = sum(1 for v in cardanim_to_asset.values()
                        if v.get("guid") is None)
    print("       CardAnim 表共 %d 个名字：解出 %d（都能定位到 dump）、"
          "guid 空白 %d、guid 解不出 %d"
          % (len(cardanim_to_asset), n_anim_ok, n_anim_noguid,
             len(anim_unresolved)))

    # ---- ⑤ 跟三张老输入表交叉校验
    table_cov = load_input_tables(cardanim_to_asset, guid_to_asset)
    n_bad = 0
    for name, t in table_cov.items():
        if "error" in t:
            print("       %-24s 读失败: %s" % (name, t["error"]))
            continue
        n_bad += t["guidMismatch"]
        print("       %-22s %4d 条 | 名字对上 %d / 找不到 %d | "
              "guid 相同 %d · 两边都空 %d · **不符 %d**"
              % (name, t["count"], t["nameFound"], t["nameMiss"],
                 t["guidMatch"], t["guidBothBlank"], t["guidMismatch"]))
        for ex in t["mismatchExamples"]:
            print("          ! 不符: %s  表里=%s  本表=%s"
                  % (ex["name"], ex["table"], ex["mine"]))
    print("[5/5] 交叉校验报完：三张老表 guid 不符共 %d 条" % n_bad)

    # ---- 未解出原因归类
    reasons = Counter()
    examples = {}
    for u in anim_unresolved:
        r = u.get("unresolved") or "?"
        reasons[r] += 1
        examples.setdefault(r, u.get("cardAnim"))
    no_name = [v for v in cardanim_to_asset.values()
               if v.get("targetName") and v.get("dump") is None]
    for v in no_name:
        r = "dump 文件定位不到: " + (v.get("dumpMissing") or "?")
        reasons[r] += 1
        examples.setdefault(r, None)
    if reasons:
        print("   未解出归类:")
        for r, c in reasons.most_common():
            print("     %-56s %4d" % (r, c))

    # ---- 写盘
    out = {
        "schema": 1,
        "generatedBy": "d:/4/Unity/工具/gen_anim_address_map.py",
        "generatedAt": args.stamp or time.strftime("%Y-%m-%d %H:%M:%S"),
        "sources": {
            "assetsFull": rel(ASSETS_FULL),
            "bundleSrc": rel(bundle_src),
            "addressablesCatalog": cat_path,
            "cardAnimBundles": anim_bundles,
            "otherBundlesWithCardAnim": [h["bundleDir"] for h in extra_hits],
            "inputTables": table_cov,
        },
        "howToJoin": (
            "m_Container[guid].asset.m_PathID 就是该 bundle 内对象的 path_id"
            "（实测 13448 条 m_FileID 全为 0）；UnityPy 重读原 .bundle 建 "
            "{path_id: obj} 反解出 (type, name)。dump 路径按 extract_full.py "
            "命名规则在磁盘上反查：先 `{safeName}_{pathId}.<ext>`，再 `{safeName}.<ext>`。"
        ),
        "stats": {
            "bundles": len(bundles),
            "containerEntries": len(entries),
            "containerEntriesGuidKey": sum(1 for e in entries
                                           if e["keyKind"] == "guid"),
            "containerEntriesNonGuidKey": sum(1 for e in entries
                                              if e["keyKind"] != "guid"),
            # 🆕 2026-10-19（T15 / A2131）：由 catalog 的「GUID → 短 internalId」补出来的子集
            "containerEntriesGuidViaCatalog": len(upgraded),
            "containerEntriesGuidUpgradeSkipped": dict(up_why),
            # 🆕 2026-10-19（T15 残余）：旧表 exact=False 的「退键族」——
            #   按 (bundle, containerKey, type, name) 三元组唯一命中后升级的子集。
            "containerEntriesGuidViaVerifiedRetreat": len(retreat_entries),
            "containerEntriesGuidViaVerifiedRetreatGuids": len(retreat_guids),
            "containerEntriesVerifiedRetreatSkipped": dict(retreat_why),
            "catalogJson": catalog_json,
            # 🆕 2026-10-23（T14）：catalog 的实际形态（`bin` = 与本地束同 build；`json` = 旧快照兜底）
            "catalogKind": ("bin" if str(catalog_json or "").lower().endswith(".bin")
                            else ("json" if catalog_json else None)),
            "catalogJsonError": catalog_err,
            "oldGuidMapError": old_map_err,
            "nonGuidUniqueKeys": sum(1 for r in per_guid.values()
                                     if r[0].get("keyKind")),
            "uniqueGuids": len(guid_to_asset) - sum(1 for r in per_guid.values()
                                                    if r[0].get("keyKind")),
            "guidsWithMultipleObjects": dup_guids,
            "pathIdResolved": len(resolved),
            "pathIdMissing": n_missing,
            "pathIdZero": n_pathid_zero,
            "pathIdMissingBundles": {k: len(v) for k, v in sorted(missing.items())
                                     if v},
            "dumpPathComputed": n_dump_ok,
            "dumpHitKind": dict(dump_hit_kind),
            "catalogHexStrings": None if cat_hex is None else len(cat_hex),
            "catalogGuidOverlap": cat_overlap,
            "extractStatsBundleFailures": fails,
            "extractStatsReadError": ferr,
            "bundleLoadErrors": load_errors,
            "containerReadProblems": problems,
            "cardAnimEntries": len(all_hits),
            "cardAnimResolved": n_anim_ok,
            "cardAnimDumpLocated": n_anim_dump,
            "cardAnimUnresolved": len(anim_unresolved),
            "cardAnimBlankGuid": len(no_guid),
            "cardAnimScanProblems": cproblems,
            # 🆕 2026-10-01：CardAnim 容器 GUID → 名字（`cardanim_to_asset` 是按名字做键的）
            "cardAnimGuidIndex": len(cardanim_guid_index),
            "cardAnimGuidIndexMissing": _cg_miss,
        },
        "guid_to_asset": guid_to_asset,
        "cardanim_to_asset": cardanim_to_asset,
        # 🆕 2026-10-01：**给 AnimFX 数据用** —— 数据里存的是 `cardAnim.m_AssetGUID`，
        #   拿这个索引换成名字，再去 `cardanim_to_asset` 拿 `targetName`（= 工程里的 prefab 名）。
        #   ⚠️ 与 `cardanim_to_asset` 是**两份**：那份按名字、这份按 GUID，别只改一份。
        "cardanim_guid_to_name": cardanim_guid_index,
        "unresolved": {
            "cardAnim": anim_unresolved,
            "cardAnimBlankGuid": no_guid,
            "cardAnimDumpMissing": [{"cardAnim": v.get("cardAnim"),
                                     "monoFile": v.get("sourceMono"),
                                     "targetType": v.get("targetType"),
                                     "targetName": v.get("targetName")}
                                    for k, v in cardanim_to_asset.items()
                                    if v.get("targetName") and not v.get("dump")],
            "pathIdByBundle": {k: v for k, v in sorted(missing.items()) if v},
            "guidDumpMissing": [
                {"key": g, "bundle": c["bundle"], "pathId": c["pathId"],
                 "type": c["type"], "name": c["name"],
                 "why": "磁盘上没有对应 dump 文件（对应类型的导出脚本没写出来）"}
                for g, r in guid_to_asset.items()
                for c in [r] + (r.get("also") or [])
                if c.get("type") and not c.get("dump")],
        },
    }
    if args.dry:
        print("   干跑（--dry）：不写 %s（总耗时 %.0fs）" % (rel(args.out), time.time() - t0))
    else:
        out_dir = os.path.dirname(args.out)
        if out_dir and not os.path.isdir(out_dir):
            os.makedirs(out_dir)
        with io.open(args.out, "w", encoding="utf-8", newline="\n") as fh:
            json.dump(out, fh, ensure_ascii=False, indent=1)
        sz = os.path.getsize(args.out)
        print("   写盘 %s（%.2f MB，总耗时 %.0fs）"
              % (rel(args.out), sz / 1048576.0, time.time() - t0))

    if not args.quiet and anim_unresolved:
        print("   -- 未解出明细（前 30 条）--")
        for u in anim_unresolved[:30]:
            print("      %-40s %s" % (u.get("cardAnim"), u.get("unresolved")))
    print("=" * 78)
    return 0


if __name__ == "__main__":
    sys.exit(main())
