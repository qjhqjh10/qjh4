#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_guid_map_from_bundles.py —— 逐卡 VFX/美术/语音的 `AssetGUID → 资产` 对照表（**直接从本地束枚举**）

## 为什么要有这一份（2026-10-18 · 特效线 `T14` / 战斗线 `T21` 余账）

旧的 `资料/普查产出_第二十七会话/guid_map.tsv`（4,985 行 / 2,372 GUID）是
`工具/_guid_resolve_probe.py map` 建的，而**它是从 Addressables 目录出发**的：
目录（`catalog_main.json`，2025-07-30 快照）里没有的 GUID，**根本没被查过** ——
即使那个资产**就在本地束里**。

实测（2026-10-18）：当前 build 的 `allcards`（1,351 张卡）引用了 **2,647 个**不同 GUID，
其中 **276 个**在旧表里解不出；**把这些束的 `AssetBundle.m_Container` 直接枚举一遍，272 个当场解出**
（剩下 4 个是 GOF2 / EC57 / BL47 的语音，本地确实没有）。

⇒ 本工具**不查目录**，直接读束里的容器键。判据：容器键本身就是 **GUID（32 位十六进制）**，
老表里 4,704/4,985 行就是这个形态（出处：`资料/普查产出_第二十七会话/guid_map.tsv`）。

## 输出（⛔ 不覆盖旧表）

`资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv`
列 = `uniqueId / cardName / field / assetGuid / bundle / containerKey / type / className / name /
      internalId / exact / source`
· 前 3 列在新枚举行里为空（那是旧表的「这条引用出自哪张卡的哪个字段」注解，本工具**只按当前 build 重算**）；
  🆕 2026-10-19：回填完成后，用 `资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张）把
  **148 行 old-map 注解**重算一遍 —— 只改「当前 build 已经不这么引用」的行（实测 2 行：
  `47e38f…` / `261e3f…`）。找不到引用的行**保持原样**并计数，⛔ 不猜。
· `source = bundle-container`（本工具枚举）或 `old-map`（旧表回填，仅当新枚举没覆盖该 GUID）；
· 旧表回填**保留**：有些 GUID 只能走目录的「退键」逻辑才解得出来，直接枚举看不到。

## 🆕 2026-10-23（特效线 `A1·R28` 尾账 · `C3`）：**短键束**的 GUID 补集

**缺口**：`scan_bundle` 只枚举各束的 `m_Container`，而**有些束的容器键是短键**
（实测 `chaosspacemarinesemperorschildrencardassets` 是 `71/49/0a/2ce…`，`…cardanims` 也有一批）
—— 短键与 GUID 之间**只有 Addressables catalog 的 `internalId` 能对上** ⇒ 那一族 GUID 在本表里
要么缺失、要么只剩旧表退键的解。

**补法**：读**与本地束同 build 的二进制 catalog**（`aa/catalog.bin` / `catalog_main.bin`），
取 `internalId` **不是 32-hex** 的那些 location（= 短键），用 `工具/_guid_resolve_probe.py` 的
`lookup` 把「GUID → 束 → 短键 → 对象 `m_Name`」解出来，作为**增量行**追加（`source = catalog-internalid`）。
· ⛔ **不改已有行**：同一 GUID 已经有行且**名字相同** ⇒ 记 `duplicate`、不追加；
  名字**不同** ⇒ 记 `conflict`、**本次不写盘**（冲突要人来裁，⛔ 不猜哪个对）。
· 场景 location（`SceneInstance`）默认**跳过**（卡 SO 不会引用场景；且场景束加载代价大）——
  `--catalog-include-scenes` 可打开。
· `--catalog PATH` 指定 catalog（默认按「安装版 → LocalLow」顺序找）；`--no-catalog` 关闭本功能
  （= 复现 2026-10-23 之前的行为）。

用法：
    "D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_guid_map_from_bundles.py
    可选 `--cards-only`：只扫卡相关的束（cardassets / cardanims / vfxandmisc / soundcollection / audiocontrol）
    可选 `--dry`：只打印、不落盘；`--out <路径>` 可把干跑产物写到别的目录（⛔ 别默认覆盖正式表）
"""
import argparse
import collections
import csv
import io
import json
import os
import re
import sys

BUNDLES = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
OLD_MAP = r"d:/4/Unity/资料/普查产出_第二十七会话/guid_map.tsv"
OUT_DIR = r"d:/4/Unity/资料/普查产出_1018/cardanim_guid_map"
OUT_TSV = os.path.join(OUT_DIR, "guid_map_bundles.tsv")

# 🆕 2026-10-19（T15 残余）：**当前 build 的 1,351 张卡**快照。
# 旧表回填那 148 行的前 3 列（uniqueId / cardName / field）是 2025-07-30 快照的注解；
# 本 build 有两行归属变了（`47e38f…` / `261e3f…`，见 `W57_特效线_T15_V1_T02静态.md`）。
# 生成器收工时按这份快照**重算注解**；文件不在 ⇒ 保留旧注解并出声（不编理由）。
CARDS_RAW = r"d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"

# 卡相关束的前缀/关键字（`--cards-only` 用）
CARD_BUNDLE_HINTS = ("cardassets", "cardanims", "battleprefabs_vfxandmisc",
                     "soundcollection", "audiocontrol")

COLS = ["uniqueId", "cardName", "field", "assetGuid", "bundle", "containerKey",
        "type", "className", "name", "internalId", "exact", "source"]

# 🆕 2026-10-23（C3）：短键补集的 catalog（二进制，与本地束同 build；旧 JSON 目录**不用**）
CAT_BIN_CANDIDATES = [
    (r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/catalog.bin",
     "安装版 aa/catalog.bin（与本地束同 build）"),
    (r"C:/Users/qjh36/AppData/LocalLow/Everguild/Warpforge/com.unity.addressables/catalog_main.bin",
     "LocalLow catalog_main.bin"),
]
HEX32 = re.compile(r"^[0-9a-f]{32}$")
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
    except Exception as e:                      # noqa: BLE001
        print("⚠️ catalog 补集：加载 `_guid_resolve_probe.py` 失败（%s）⇒ 跳过本功能" % e)
    return _PROBE["mod"]


def pick_catalog(path=None):
    if path:
        return path, "显式指定"
    for p, why in CAT_BIN_CANDIDATES:
        if os.path.isfile(p):
            return p, why
    return None, None


def catalog_short_rows(cat_path, existing, skip_scenes=True):
    """catalog 的**短键**补集 → `(rows, stat, conflicts, dups)`。

    `existing` = 已枚举/回填出来的行（用来判「重复」与「冲突」）。
    · **只增不改**：同一 GUID 已有同名行 ⇒ `duplicate`（不追加）；名字不同 ⇒ `conflict`（不追加，交人裁）。
    · 短键判据 = catalog 的 `internalId` **不是 32-hex**（长键那族 `scan_bundle` 已经覆盖）。
    """
    mod = _probe_module()
    if mod is None:
        return [], {"skipped": "probe 不可用"}, [], []
    cat = mod.load_catalog(cat_path)
    stat = collections.Counter()
    per_bundle = collections.Counter()
    conflicts, dups, rows, no_hit = [], [], [], []
    by_guid = collections.defaultdict(list)
    for r in existing:
        by_guid[(r.get("assetGuid") or "").lower()].append(r)

    keys = cat.get("keys") or []
    for ki, key in enumerate(keys):
        if not isinstance(key, str) or not HEX32.match(key):
            continue
        entries = mod.resolve_guid(cat, ki)
        short = [e for e in entries if e.get("iid") and not HEX32.match(e["iid"])]
        if not short:
            continue
        stat["short_guids"] += 1
        rt_all = [t for e in short for t in (e.get("rtypes") or []) if t]
        if skip_scenes and rt_all and all("SceneInstance" in t for t in rt_all):
            stat["scene_skipped"] += 1
            continue
        try:
            hits = mod.lookup(cat, key)
        except Exception as e:                  # noqa: BLE001
            stat["lookup_failed"] += 1
            print("  !! lookup(%s) 失败: %s" % (key, e))
            continue
        hits = [h for h in hits if not h.get("note") and h.get("name") and h.get("type")]
        if not hits:
            stat["no_bundle_hit"] += 1
            no_hit.append({"guid": key.lower(), "iid": short[0].get("iid"),
                           "rtypes": rt_all, "bundles": short[0].get("bundles")})
            continue
        hits.sort(key=lambda h: (h.get("bundle") or "", str(h.get("key") or ""), h.get("pathid") or 0))
        seen_sig = set()
        for h in hits:
            sig = (h.get("bundle"), h.get("key"), h.get("name"), h.get("type"))
            if sig in seen_sig:
                continue
            seen_sig.add(sig)
            row = {
                "uniqueId": "", "cardName": "", "field": "",
                "assetGuid": key.lower(), "bundle": h.get("bundle") or "",
                "containerKey": h.get("key") or "", "type": h.get("type") or "",
                "className": h.get("cls") or "", "name": h.get("name") or "",
                "internalId": h.get("iid") or "", "exact": str(bool(h.get("exact"))),
                "source": "catalog-internalid",
            }
            old = by_guid.get(key.lower()) or []
            if old:
                # 「重复」= 名字 + 束 + 容器键**三者都对上**（类型可以不同：Sprite/Texture2D 那一对）；
                # 只对名字、束或键有一个对不上 ⇒ **冲突**（交人裁，⛔ 不静默当成重复）
                if any((o.get("name") or "") == row["name"]
                       and (o.get("bundle") or "") == row["bundle"]
                       and (o.get("containerKey") or "") == row["containerKey"] for o in old):
                    dups.append({"guid": key.lower(), "old": old, "new": row})
                    stat["duplicate"] += 1
                    continue
                conflicts.append({"guid": key.lower(), "old": old, "new": row})
                stat["conflict"] += 1
                continue
            rows.append(row)
            by_guid[key.lower()].append(row)
            per_bundle[row["bundle"]] += 1
            stat["added"] += 1
            stat["added_" + (row["type"] or "?")] += 1
    stat["added_bundles"] = len(per_bundle)
    return (rows, {"counts": dict(stat), "perBundle": dict(per_bundle),
                   "noHit": no_hit, "dups": [d["guid"] for d in dups]},
            conflicts, dups)


def script_classes():
    """monoscripts 束: MonoScript PathID -> 类名（MonoBehaviour 的 `className` 靠它）。"""
    import UnityPy
    p = os.path.join(BUNDLES, "Waprforge_monoscripts.bundle")
    out = {}
    if not os.path.exists(p):
        return out, "monoscripts 束不存在: %s" % p
    for o in UnityPy.load(p).objects:
        if o.type.name == "MonoScript":
            try:
                out[o.path_id] = o.read_typetree().get("m_ClassName")
            except Exception:
                pass
    return out, None


def scan_bundle(path, scripts):
    """一个束文件 → [(containerKey, type, className, name)]。

    只读 `m_Container` 里点到的那些 PathID 的类型树（**不遍历整包**）—— 大束（100 MB+）也快。
    """
    import UnityPy
    env = UnityPy.load(path)
    cont = {}
    for o in env.objects:
        if o.type.name == "AssetBundle":
            for k, v in o.read_typetree()["m_Container"]:
                cont[k] = v["asset"]["m_PathID"]
    want = set(cont.values())
    info = {}
    for o in env.objects:
        if o.path_id not in want:
            continue
        try:
            t = o.read_typetree()
        except Exception:
            continue
        if not isinstance(t, dict):
            continue
        cls = None
        if o.type.name == "MonoBehaviour" and isinstance(t.get("m_Script"), dict):
            cls = scripts.get(t["m_Script"].get("m_PathID"))
        info[o.path_id] = (o.type.name, cls, t.get("m_Name"))
    rows = []
    for k, pid in cont.items():
        ty, cls, nm = info.get(pid, (None, None, None))
        rows.append((k, ty, cls, nm))
    return rows


def load_card_refs(path=CARDS_RAW):
    """当前 build 的卡快照 → `{guid: [(uniqueId, cardName, field)]}`（按卡文件顺序、去重）。

    · 走 `full_raw`（完整 SO 记录；顶层是挖出来的子集，`summonAudio` 那类只在 full_raw 里）；
    · `field` 统一成旧表写法：`cardAbilities[0].abilityLogic[0].cardAnimations[1]`
      （去掉 `m_AssetGUID` 尾、`/` → `.`、去掉 `full_raw.` 前缀）；
    · 只收 32 位 hex 的 `m_AssetGUID`（空引用 / PPtr 不算）。

    实测（2026-10-19，1,351 张）：旧表 148 行**全部**能在这里找到引用；
    与旧注解逐字相同 **146** 行，过期 **2** 行（`47e38f…` / `261e3f…`）。
    """
    if not os.path.isfile(path):
        return {}, "卡快照不存在: %s" % path
    try:
        doc = json.load(io.open(path, encoding="utf-8"))
    except Exception as e:                  # noqa: BLE001
        return {}, "卡快照读不动（%s）: %s" % (path, e)
    cards = doc.get("cards") or []

    def walk(o, prefix=""):
        if isinstance(o, dict):
            for k, v in o.items():
                yield from walk(v, prefix + "/" + k)
        elif isinstance(o, list):
            for i, v in enumerate(o):
                yield from walk(v, prefix + "[%d]" % i)
        else:
            yield prefix, o

    def norm_field(p):
        p = p.lstrip("/")
        if p.startswith("full_raw/"):
            p = p[len("full_raw/"):]
        if p.endswith("/m_AssetGUID"):
            p = p[:-len("/m_AssetGUID")]
        return p.replace("/", ".")

    out = {}
    for c in cards:
        uid, cname = c.get("uniqueId") or "", c.get("cardName") or ""
        root = c.get("full_raw") or c
        seen = set()
        for p, v in walk(root):
            if not p.endswith("/m_AssetGUID"):
                continue
            g = (v or "").lower() if isinstance(v, str) else ""
            if len(g) != 32 or not all(ch in "0123456789abcdef" for ch in g):
                continue
            ref = (uid, cname, norm_field(p))
            if ref in seen:
                continue
            seen.add(ref)
            out.setdefault(g, []).append(ref)
    return out, None


def refresh_annotations(rows, refs):
    """把 `source=old-map` 那 148 行的前 3 列按**当前 build** 重算。

    规则（保守、只动过期的）：
      ① 旧注解仍然出现在当前引用里 ⇒ 一位不动；
      ② 同一 `uniqueId` 仍引用这个 GUID ⇒ 只更新 cardName / field（卡改名 / 字段挪位）；
      ③ 否则取**卡快照顺序里的第一条**引用（稳定：cards_raw.json 的卡顺序）。
    找不到任何引用的行**保持原样**并计数 —— ⛔ 不凭猜测改。

    返回 `(changed, no_ref)`：`changed = [(guid, old, new)]`。
    """
    changed = []
    no_ref = 0
    for r in rows:
        if r.get("source") != "old-map":
            continue
        rs = refs.get((r.get("assetGuid") or "").lower()) or []
        if not rs:
            no_ref += 1
            continue
        old = (r.get("uniqueId", ""), r.get("cardName", ""), r.get("field", ""))
        if old in rs:
            continue
        same = [x for x in rs if x[0] == old[0]]
        pick = (same or rs)[0]
        changed.append((r.get("assetGuid"), old, pick))
        r["uniqueId"], r["cardName"], r["field"] = pick
    return changed, no_ref


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--cards-only", action="store_true", help="只扫卡相关的束")
    ap.add_argument("--out", default=OUT_TSV)
    ap.add_argument("--cards-raw", default=CARDS_RAW,
                    help="当前 build 卡快照（重算旧表注解用；默认 %s）" % CARDS_RAW)
    ap.add_argument("--dry", action="store_true",
                    help="只打印统计，不落盘（`A1648` 同族）")
    # 🆕 2026-10-23（C3）：短键补集
    ap.add_argument("--catalog", default=None,
                    help="二进制 catalog（默认按「安装版 → LocalLow」找）；⛔ 别传旧 JSON 目录")
    ap.add_argument("--no-catalog", action="store_true", help="关闭短键补集（= 旧行为）")
    ap.add_argument("--catalog-include-scenes", action="store_true",
                    help="连 SceneInstance location 也查（默认跳过：卡 SO 不引用场景）")
    ap.add_argument("--allow-conflicts", action="store_true",
                    help="有冲突也写盘（默认**不写**，冲突交人裁）")
    args = ap.parse_args()

    if not os.path.isdir(BUNDLES):
        sys.exit("束目录不存在: %s" % BUNDLES)
    files = sorted(f for f in os.listdir(BUNDLES) if f.endswith(".bundle"))
    if args.cards_only:
        files = [f for f in files if any(h in f for h in CARD_BUNDLE_HINTS)]

    scripts, err = script_classes()
    if err:
        print("⚠️ " + err + "（MonoBehaviour 的 className 会是空）")
    print("扫 %d 个束 · monoscripts %d 个类" % (len(files), len(scripts)))

    seen = set()             # (guid, bundle, name)
    rows = []
    for i, f in enumerate(files, 1):
        path = os.path.join(BUNDLES, f)
        try:
            got = scan_bundle(path, scripts)
        except Exception as e:      # 坏包/不支持的格式：**出声**，不静默跳过
            print("  !! %-60s %s" % (f, e))
            continue
        n = 0
        for key, ty, cls, nm in got:
            if not key or not nm:
                continue
            sig = (key.lower(), f, nm)
            if sig in seen:
                continue
            seen.add(sig)
            rows.append({
                "uniqueId": "", "cardName": "", "field": "",
                "assetGuid": key.lower(), "bundle": f, "containerKey": key,
                "type": ty or "", "className": cls or "", "name": nm,
                "internalId": key, "exact": "True", "source": "bundle-container",
            })
            n += 1
        print("  [%2d/%2d] %-62s container=%-5d 收入=%d" % (i, len(files), f, len(got), n))

    # 旧表回填：新枚举没覆盖到的 GUID 才补（保留目录「退键」那一族的解）
    have = set(r["assetGuid"] for r in rows)
    backfill = 0
    if os.path.isfile(OLD_MAP):
        with io.open(OLD_MAP, encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                g = (r.get("assetGuid") or "").lower()
                if not g or not r.get("name") or g in have:
                    continue
                have.add(g)
                backfill += 1
                rows.append({
                    "uniqueId": r.get("uniqueId", ""), "cardName": r.get("cardName", ""),
                    "field": r.get("field", ""), "assetGuid": g,
                    "bundle": r.get("bundle", ""), "containerKey": r.get("containerKey", ""),
                    "type": r.get("type", ""), "className": r.get("className", ""),
                    "name": r.get("name", ""), "internalId": r.get("internalId", ""),
                    "exact": r.get("exact", ""), "source": "old-map",
                })
    else:
        print("⚠️ 旧表不存在，跳过回填: %s" % OLD_MAP)

    # 旧表注解按**当前 build** 重算（T15 残余：2 行归属过期）
    refs, refs_err = load_card_refs(args.cards_raw)
    if refs_err:
        print("⚠️ %s（旧表注解保持原样）" % refs_err)
        changed, no_ref = [], None
    else:
        changed, no_ref = refresh_annotations(rows, refs)
        print("旧表注解重算：改动 %d 行 / 无当前引用 %d 行（参考 %s，%d 个 GUID 有引用）"
              % (len(changed), no_ref, os.path.basename(args.cards_raw), len(refs)))
        for g, old, new in changed:
            print("   %s\n      旧: %s\n      新: %s" % (g, old, new))

    # ---- 🆕 2026-10-23（C3）：catalog 短键补集（只增不改；冲突 ⇒ 默认不写盘）
    cat_conflicts, cat_dups = [], []
    if not args.no_catalog:
        cat_path, cat_why = pick_catalog(args.catalog)
        if not cat_path:
            print("⚠️ 找不到二进制 catalog ⇒ 跳过短键补集（⛔ 不当成「没有短键缺口」）")
        else:
            print("\n[catalog 短键补集] catalog = %s（%s）" % (cat_path, cat_why))
            add_rows, cat_stat, cat_conflicts, cat_dups = catalog_short_rows(
                cat_path, rows, skip_scenes=not args.catalog_include_scenes)
            counts = (cat_stat or {}).get("counts") or {}
            print("  短键 GUID %d · 场景跳过 %d · **新增 %d 行** · 重复 %d · 冲突 %d · 无束命中 %d · lookup 失败 %d"
                  % (counts.get("short_guids", 0), counts.get("scene_skipped", 0),
                     counts.get("added", 0), counts.get("duplicate", 0),
                     counts.get("conflict", 0), counts.get("no_bundle_hit", 0),
                     counts.get("lookup_failed", 0)))
            print("  新增按类型：%s" % {k[6:]: v for k, v in counts.items() if k.startswith("added_") and k != "added_bundles"})
            for b, n in sorted(((cat_stat or {}).get("perBundle") or {}).items(), key=lambda kv: -kv[1]):
                print("    +%-4d %s" % (n, b))
            for c in cat_conflicts:
                print("  🔴 冲突 GUID %s\n      旧: %s\n      新: %s"
                      % (c["guid"], [(o.get("bundle"), o.get("name"), o.get("type")) for o in c["old"]],
                         [(c["new"].get("bundle"), c["new"].get("name"), c["new"].get("type"))]))
            for nh in (cat_stat or {}).get("noHit") or []:
                print("  ⚠️ 短键无束命中：GUID %s · internalId %s · 类型 %s · 目录束 %s"
                      % (nh["guid"], nh["iid"], nh["rtypes"],
                         [os.path.basename((b or "").replace("\\", "/")) for b in (nh["bundles"] or [])]))
            rows.extend(add_rows)
            if cat_conflicts and not args.allow_conflicts and not args.dry:
                print("\n🔴 发现 %d 条冲突 ⇒ **本次不写盘**（先裁冲突；或 `--allow-conflicts` 强写）" % len(cat_conflicts))
                sys.exit(2)

    if args.dry:
        print("\n干跑（--dry）：不写 %s" % args.out)
    else:
        if not os.path.isdir(OUT_DIR):
            os.makedirs(OUT_DIR)
        tmp = args.out + ".tmp"
        with io.open(tmp, "w", encoding="utf-8", newline="") as fh:
            w = csv.DictWriter(fh, fieldnames=COLS, delimiter="\t",
                               lineterminator="\n", quoting=csv.QUOTE_MINIMAL)
            w.writeheader()
            w.writerows(rows)
        os.replace(tmp, args.out)       # 先写临时件再改名：写一半崩掉不会毁掉旧产物
        print("")
        print("写出 %s" % args.out)

    guids = collections.Counter(r["assetGuid"] for r in rows)
    print("  行 %d · GUID %d · 其中新枚举 %d 行 / 旧表回填 %d 行"
          % (len(rows), len(guids), len(rows) - backfill, backfill))
    print("  多行 GUID %d 个（同一 GUID 解出同名的多个类型，如 Sprite+Texture2D）"
          % sum(1 for v in guids.values() if v > 1))


if __name__ == "__main__":
    main()
