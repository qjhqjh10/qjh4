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

用法：
    "D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_guid_map_from_bundles.py
    可选 `--cards-only`：只扫卡相关的束（cardassets / cardanims / vfxandmisc / soundcollection / audiocontrol）
"""
import argparse
import collections
import csv
import io
import json
import os
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
