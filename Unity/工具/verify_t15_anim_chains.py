#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""verify_t15_anim_chains.py — 特效线 `T15` / `A2131` 的自检（**纯 python，不跑 Unity**）

用法：
    python -I -X utf8 d:/4/Unity/工具/verify_t15_anim_chains.py

它验四件事（每件都能独立失败，失败会打印「期望 / 实读」并以退出码 1 收尾）：

  ① **EC 27/27**：`资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` 里
     `source=old-map` 且字段是 `cardAnimations[…]` 的 27 个 GUID，必须
       · 在 `anim_address_map.guid_to_asset` 里 → CardAnim MonoBehaviour（存档 dump 存在）
       · 在 `anim_address_map.cardanim_guid_to_name` 里 → 与 tsv 的 CardAnim 名逐字相同
       · 该名在 `cardanim_to_asset` 里 → `targetName`（prefab）非空
     判据：`资料/普查产出_1018/R_K11_K12_报告.md` §五·1（EC29 全链）。

  ② **`cardanim_guid_to_name` 没有非 GUID 键**（第四十二会话 `W29` / `A7 ⑥·2`）：
     修前有 **47 个被截断的键**（长度 1–3 hex，全是 EC 族）；修后应为 **0**。
     ②b 再验 catalog 补跳的**安全闸**：旧表 `exact=True` 的行名字必须逐字相同，
     `exact=False` 的行**不许**被对到别的对象（那族是 31 行「目录键被截断、靠退键解」）。

  ③ **163 条的处置**（K11/K12 报告 §五·2 的「全池 1,629 / 缺 163」）：
     用**当前 build 的真值快照**（`allcards_1350/cards_raw.json`，1,351 张）重算：
       · 全快照 `cardAnimations` 引用 = 1,629
       · 我们卡池（`cards_engine.json`，按 id 全命中）= 1,466
       · 差 = **163 条 / 112 张卡** —— 这些卡**不在我们池子里**（80 张 `_enchantment*` 变体 +
         26 张其它 + 6 张 `Z-NC` 教程卡）
       · 163 条里 **162 条能解出 prefab**，唯一 1 条解不出的是 `Wait_1,5s`
         （纯计时占位、`animInfo.animAdressable` 为空，原版就没有 VFX）
     ⚠️ 所以「163 条缺」**不是缺陷**：它是「快照全量 vs 我们池子」的差集。

  ④ **池内逐卡覆盖**：
       · 池内 1,466 条 `cardAnimations` 引用 **逐字段**都能在 `card_vfx_by_card.json` 找到条目（0 缺）
       · 其中 `prefab` 为空的只允许是 `Wait_*` 纯计时占位 —— 基线 **12 条**

另有两条「报告里点名的卡」的仲裁：
  · `6b18f493…`（Duelist's Hubris）/ `e9d68c65…`（Coterie of the Conceited）是
    **`_enchantment` 变体 SO**（`m_Name` 后缀可证），**不是池内那张卡**；池内对应的是
    `6ed51d59…` / `EC51`，它们的 CardAnim 引用**都已进运行时表**。
"""

import collections
import csv
import io
import json
import os
import re
import sys

ROOT = r"d:/4/Unity"
AMAP = os.path.join(ROOT, "数据", "索引", "anim_address_map.json")
TSV = os.path.join(ROOT, "资料", "普查产出_1018", "cardanim_guid_map", "guid_map_bundles.tsv")
TRUTH = os.path.join(ROOT, "资料", "普查产出_1018", "allcards_1350", "cards_raw.json")
ENGINE = os.path.join(ROOT, "MyGame", "Assets", "RuleEngine", "Resources", "cards_engine.json")
VFX = os.path.join(ROOT, "数据", "游戏数据", "card_vfx_by_card.json")
ASSETS = r"d:/2/新解包资源/assets_full"

GUID_RE = re.compile(r"^[0-9a-f]{32}$")
# 快照口径（2026-10-19 现读）。快照/卡池变了就要更新这三个数 —— 这正是自检的意义。
EXP_TRUTH_REFS = 1629
EXP_POOL_REFS = 1466
EXP_GAP_REFS = 163
EXP_GAP_CARDS = 112
EXP_NULL_PREFAB = 12

FAILS = []


def check(cond, what, got=None, want=None):
    if cond:
        print("  [OK]   %s" % what)
        return True
    FAILS.append(what)
    extra = ""
    if got is not None or want is not None:
        extra = "（实读 %r / 期望 %r）" % (got, want)
    print("  [FAIL] %s%s" % (what, extra))
    return False


def read_json(p):
    return json.load(io.open(p, encoding="utf-8"))


def card_refs(t):
    """一张卡 SO → [(字段路径, GUID)]（`cardAnimations` 两路）。"""
    out = []
    for i, ab in enumerate(t.get("cardAbilities") or []):
        for j, lg in enumerate(ab.get("abilityLogic") or []):
            for k, a in enumerate(lg.get("cardAnimations") or []):
                g = ((a or {}).get("m_AssetGUID") or "").strip().lower()
                if g:
                    out.append(("cardAbilities[%d].abilityLogic[%d].cardAnimations[%d]" % (i, j, k), g))
    aab = t.get("activeAbility") or {}
    for j, lg in enumerate(aab.get("activeAbilityLogic") or []):
        for k, a in enumerate(lg.get("cardAnimations") or []):
            g = ((a or {}).get("m_AssetGUID") or "").strip().lower()
            if g:
                out.append(("activeAbility.activeAbilityLogic[%d].cardAnimations[%d]" % (j, k), g))
    return out


def main():
    print("=" * 78)
    print("verify_t15_anim_chains.py（特效线 T15 / A2131）")
    amap = read_json(AMAP)
    g2a = amap["guid_to_asset"]
    c2n = amap["cardanim_guid_to_name"]
    c2a = amap["cardanim_to_asset"]

    # ---------------- ① EC 27/27
    print("[1] EC 27 个 CardAnim GUID")
    rows = list(csv.DictReader(io.open(TSV, encoding="utf-8"), delimiter="\t"))
    old = [r for r in rows if r["source"] == "old-map" and "cardAnimations" in r["field"]]
    guids = {r["assetGuid"] for r in old}
    check(len(old) == 27 and len(guids) == 27,
          "old-map 的 cardAnimations 引用 = 27 条 / 27 个不同 GUID",
          (len(old), len(guids)), (27, 27))
    ok_ca = ok_name = ok_prefab = ok_dump = 0
    for r in old:
        g, nm = r["assetGuid"], r["name"]
        ent = g2a.get(g)
        if ent and ent.get("type") == "MonoBehaviour" and ent.get("name") == nm:
            ok_ca += 1
            d = ent.get("dump")
            if d and os.path.isfile(os.path.join(ASSETS, d.replace("/", os.sep))):
                ok_dump += 1
        if c2n.get(g) == nm:
            ok_name += 1
        tgt = c2a.get(nm) or {}
        if tgt.get("targetName"):
            ok_prefab += 1
    check(ok_ca == 27, "guid_to_asset：27/27 → 同名 CardAnim MonoBehaviour", ok_ca, 27)
    check(ok_dump == 27, "guid_to_asset：27/27 的 CardAnim dump 在磁盘上", ok_dump, 27)
    check(ok_name == 27, "cardanim_guid_to_name：27/27 GUID → 同名 CardAnim", ok_name, 27)
    check(ok_prefab == 27, "cardanim_to_asset：27/27 CardAnim → prefab 名", ok_prefab, 27)
    # 直链：dump 的 animAdressable GUID → guid_to_asset 必须就是同一个 prefab
    ok_chain = 0
    for r in old:
        nm = c2n.get(r["assetGuid"])
        rec = c2a.get(nm) or {}
        tgt = g2a.get(rec.get("guid") or "") or {}
        if tgt.get("type") == "GameObject" and tgt.get("name") == rec.get("targetName"):
            ok_chain += 1
    check(ok_chain == 27,
          "animAdressable GUID 直查 guid_to_asset：27/27 是同一个 GameObject prefab",
          ok_chain, 27)

    # ---------------- ② 反向索引没有截断键
    print("[2] cardanim_guid_to_name 的键形态（W29）")
    bad_keys = [k for k in c2n if not GUID_RE.match(k)]
    check(len(bad_keys) == 0,
          "非 GUID 键 = 0（修前 47 个截断键，全是 EC 族）", len(bad_keys), 0)
    check(len(c2n) == 1099, "反向索引条数 = 1099（键换了、条数不变）", len(c2n), 1099)
    check(amap["stats"].get("containerEntriesGuidViaCatalog", 0) > 0,
          "stats.containerEntriesGuidViaCatalog > 0（catalog 补跳确实跑过）",
          amap["stats"].get("containerEntriesGuidViaCatalog"))

    # ---------------- ②b catalog 补跳的**安全闸**（旧表 exact / 名字逐字校验）
    print("[2b] catalog 补跳的安全闸")
    old_tsv = os.path.join(ROOT, "资料", "普查产出_第二十七会话", "guid_map.tsv")
    if os.path.isfile(old_tsv):
        oldrows = list(csv.DictReader(io.open(old_tsv, encoding="utf-8"), delimiter="\t"))
        exact_ok = exact_bad = 0
        for r in oldrows:
            g = (r.get("assetGuid") or "").lower()
            if not g or r.get("exact") != "True":
                continue
            e = g2a.get(g)
            if e and e.get("name") == r.get("name"):
                exact_ok += 1
            else:
                exact_bad += 1
        check(exact_bad == 0,
              "旧表 exact=True 的 %d 行：guid_to_asset 名字逐字相同" % exact_ok,
              exact_bad, 0)
        # exact=False 那族**不许**被 catalog 升级成别的对象
        wrong = []
        for r in oldrows:
            g = (r.get("assetGuid") or "").lower()
            if not g or r.get("exact") != "False":
                continue
            e = g2a.get(g)
            if e and e.get("name") != r.get("name"):
                wrong.append((g, r.get("name"), e.get("name")))
        check(len(wrong) == 0,
              "旧表 exact=False 的行没有被 catalog 对到别的对象", len(wrong), 0)
        skipped = amap["stats"].get("containerEntriesGuidUpgradeSkipped") or {}
        check(skipped.get("旧表 exact=False（目录键被截断，catalog 的 iid 取到别的对象）", 0) > 0,
              "stats 记录了「exact=False 整族跳过」的计数", skipped)
    else:
        check(False, "旧表存在（%s）" % old_tsv, "missing")

    # ---------------- ③ 163 条
    print("[3] 163 条 cardAnimations（全快照 vs 我们池子）")
    truth_doc = read_json(TRUTH)
    truth = [c.get("full_raw") or c for c in truth_doc["cards"]]
    pool = read_json(ENGINE)["cards"]
    pool_ids = {c["id"] for c in pool}
    truth_refs = sum(len(card_refs(t)) for t in truth)
    pool_refs = sum(len(card_refs(t)) for t in truth if t.get("uniqueId") in pool_ids)
    gap = [(t, f, g) for t in truth if t.get("uniqueId") not in pool_ids
           for f, g in card_refs(t)]
    check(truth_refs == EXP_TRUTH_REFS, "全快照引用 = %d" % EXP_TRUTH_REFS, truth_refs, EXP_TRUTH_REFS)
    check(pool_refs == EXP_POOL_REFS, "池内（按 id）引用 = %d" % EXP_POOL_REFS, pool_refs, EXP_POOL_REFS)
    check(len(gap) == EXP_GAP_REFS, "差集引用 = %d" % EXP_GAP_REFS, len(gap), EXP_GAP_REFS)
    check(len({t["uniqueId"] for t, _, _ in gap}) == EXP_GAP_CARDS,
          "差集卡数 = %d" % EXP_GAP_CARDS, len({t["uniqueId"] for t, _, _ in gap}), EXP_GAP_CARDS)
    check(all(t.get("uniqueId") not in pool_ids for t, _, _ in gap),
          "差集 100% 是「不在池子里」的卡（不是解算失败）")
    # 分类
    gmap = collections.defaultdict(list)
    for r in rows:
        if r.get("assetGuid") and r.get("name"):
            gmap[r["assetGuid"].lower()].append(r)
    cls = collections.Counter()
    unresolved_names = []
    for t, f, g in gap:
        ca = [r for r in gmap.get(g) or [] if r["className"] == "CardAnim"]
        nm = ca[0]["name"] if ca else ""
        rec = c2a.get(nm) if nm else None
        if rec and rec.get("targetName"):
            cls["resolves"] += 1
        elif rec is not None:
            cls["unresolved"] += 1
            unresolved_names.append(nm)
        else:
            cls["no-entry"] += 1
    check(cls.get("resolves") == 162, "163 条里能解出 prefab 的 = 162", cls.get("resolves"), 162)
    check(unresolved_names == ["Wait_1,5s"],
          "唯一解不出的 = Wait_1,5s（纯计时占位，原版无 VFX）", unresolved_names, ["Wait_1,5s"])
    kinds = collections.Counter(
        "enchantment" if "_enchantment" in (t.get("m_Name") or "")
        else ("Z-NC" if (t.get("m_Name") or "").startswith("Z-NC") else "other")
        for t, _, _ in gap)
    print("       差集构成（按引用）：%s" % dict(kinds))
    # 两条点名卡
    for bad, good, why in [
            ("6b18f49379aca9742894111f7ed409c7", "6ed51d59b9f8ea24ab0dc7ceaef7956c", "Duelist's Hubris"),
            ("e9d68c658708e7149b6ddefa492e7357", "EC51", "Coterie of the Conceited")]:
        check(bad not in pool_ids and good in pool_ids,
              "%s：%s… 是变体（不在池）、%s 才是池内卡" % (why, bad[:8], good))

    # ---------------- ④ 池内逐字段覆盖
    print("[4] 池内逐卡覆盖")
    vfx = read_json(VFX)["byCard"]
    missing_fields = []
    null_prefab = []
    n = 0
    for c in pool:
        cid = c["id"]
        t = next((x for x in truth if x.get("uniqueId") == cid), None)
        if t is None:
            continue
        have = {}
        for ev, lst in (vfx.get(cid) or {}).items():
            if isinstance(lst, list):
                for it in lst:
                    if isinstance(it, dict) and str(it.get("soField") or "").startswith(
                            ("cardAbilities", "activeAbility")):
                        have[it["soField"]] = it
        for f, g in card_refs(t):
            n += 1
            it = have.get(f)
            if it is None:
                missing_fields.append((cid, f, g))
            elif not it.get("prefab"):
                null_prefab.append((cid, it.get("cardanim"), it.get("unresolved")))
    check(n == EXP_POOL_REFS, "池内引用逐条遍历 = %d" % EXP_POOL_REFS, n, EXP_POOL_REFS)
    check(len(missing_fields) == 0, "逐字段缺条目 = 0", len(missing_fields), 0)
    check(len(null_prefab) == EXP_NULL_PREFAB,
          "prefab 为空 = %d（基线）" % EXP_NULL_PREFAB, len(null_prefab), EXP_NULL_PREFAB)
    check(all((x[1] or "").startswith("Wait_") for x in null_prefab),
          "prefab 为空的全部是 Wait_* 纯计时占位",
          sorted({x[1] for x in null_prefab}))

    print("=" * 78)
    if FAILS:
        print("失败 %d 条：" % len(FAILS))
        for f in FAILS:
            print("  -", f)
        return 1
    print("全部通过 ✅")
    return 0


if __name__ == "__main__":
    sys.exit(main())
