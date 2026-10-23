# -*- coding: utf-8 -*-
"""gen_warlord_goes_second.py — 原版 `RawCardScript.goSecondCardInHand` 真值 → 数据件

**为什么要有它**（引擎线 `P1·②`，2026-10-23）：
  中间表 `数据/游戏数据/warlord_goes_second.json` 原来是 **2025-07-30 旧快照**（55 位督军，
  **EmperorsChildren / SpaceWolves 两族整族缺席**）⇒ `gen_cards_engine.py` 的
  `resolve_warlord_defence()` 覆盖不到这两族 ⇒ 卡表上那 6 位督军的 `goesSecond` 是空的
  ⇒ 后手开局 `RuleCore.GoesSecondCard` 落到第 ③ 档「出声 + 不给卡」。
  判据 → `资料/普查产出_1023c/引擎深审C_数据吻合.md` §2·2 / §五·3。

**真值源（当前 build，只读）**：
  `资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张卡 SO · 每个 `full_raw` 全字段）。
  字段 = `full_raw.goSecondCardInHand`，形状 `{"m_FileID": 0, "m_PathID": <int>}` =
  **同包内 PPtr**（`m_FileID = 0` ⇒ 本包内）。⇒ 按 `m_PathID` 反查同文件的 `path_id` 解成 `uniqueId`。

**不变量（原版数据的实测形状，不是我们推的规则）**：
  **同一阵营的所有督军指向同一张** —— 13 个阵营各一张（现读 56 位督军 / 13 张卡）。
  本脚本每次生成都**重新断言**；不成立就抛（⛔ 不静默取一张）。

**产出**：`d:/4/Unity/数据/游戏数据/warlord_goes_second.json`
  ```json
  {"count": 56, "map": {"EC55": {"refId": "EC59", "refName": "Vial Tanks",
                                 "warlord": "Lucius the Eternal"}, …}}
  ```
  键 = 督军 SO 的 `uniqueId`（与旧表同形）；消费点 = `gen_cards_engine.py` 的
  `load_warlord_defence()` → `resolve_warlord_defence()`（**按阵营**落到我们卡池的防御卡上）。

**跑法**：
    python -I -X utf8 d:/4/Unity/工具/gen_warlord_goes_second.py            # 写文件
    python -I -X utf8 d:/4/Unity/工具/gen_warlord_goes_second.py --check    # 只对账，不写
  之后（本脚本不自动跑）：
    python -I -X utf8 d:/4/Unity/工具/gen_cards_engine.py --write
"""
import argparse
import collections
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

TRUTH = r"d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"
OUT = r"d:/4/Unity/数据/游戏数据/warlord_goes_second.json"

# 阵营号 → 名字（`cardArmy` 数值；只用于**读数/断言文案**，不参与落值）。
# ⚠️ 判据 = `cards_raw.json` 现读（同一 `cardArmy` 的全部卡 SO 同阵营）—— 别按 id 前缀猜。
ARMY_NAME = {
    10: "Ultramarines", 20: "Goff", 30: "SaimHann", 40: "Sautekh", 50: "BlackLegion",
    60: "Leviathan", 70: "TauEmpire", 80: "Sororitas", 90: "Genestealers",
    100: "AstraMilitarum", 110: "DarkAngels", 120: "EmperorsChildren", 130: "SpaceWolves",
}


def load_truth():
    raw = json.load(open(TRUTH, encoding="utf-8"))
    cards = raw.get("cards") or []
    if not cards:
        raise SystemExit(f"❌ {TRUTH} 里没有 `cards` —— 真值源不对，先查文件")
    return raw, cards


def build(cards):
    """→ `(map, report)`。`map` = `{督军 uniqueId: {refId, refName, warlord}}`。"""
    by_path = {c.get("path_id"): c for c in cards}

    def label(c):
        # 与旧表同口径：优先 `cardName`，没有就退回 `m_Name` 的尾巴（`33 EmperorsChildren-Heldrake`）。
        nm = (c.get("cardName") or "").strip()
        if nm:
            return nm
        mn = (c.get("m_Name") or "").strip()
        return mn.split("-", 1)[1] if "-" in mn else mn

    out, unresolved, no_field = {}, [], 0
    for c in cards:
        fr = c.get("full_raw") or {}
        pptr = fr.get("goSecondCardInHand")
        if not isinstance(pptr, dict) or not pptr.get("m_PathID"):
            no_field += 1
            continue
        if pptr.get("m_FileID", 0) != 0:
            # ⛔ 跨包引用我们不猜（本件实测全 0）
            unresolved.append((c.get("uniqueId"), "m_FileID!=0", pptr.get("m_FileID")))
            continue
        tgt = by_path.get(pptr["m_PathID"])
        if tgt is None:
            unresolved.append((c.get("uniqueId"), "m_PathID 在本文件里找不到", pptr["m_PathID"]))
            continue
        uid = c.get("uniqueId")
        if not uid:
            unresolved.append(("<无 uniqueId>", c.get("m_Name"), pptr["m_PathID"]))
            continue
        out[uid] = {"refId": tgt.get("uniqueId"), "refName": label(tgt), "warlord": label(c)}
    return out, {"no_field": no_field, "unresolved": unresolved}


def check_invariant(m, cards):
    """**同一阵营 → 同一张** 的不变量（不成立就抛，⛔ 不静默取一张）。"""
    by_army_card = {c.get("uniqueId"): c.get("cardArmy") for c in cards}
    per_army = collections.defaultdict(set)
    for uid, v in m.items():
        per_army[by_army_card.get(uid)].add(v["refId"])
    bad = {a: sorted(r) for a, r in per_army.items() if len(r) != 1}
    if bad:
        raise SystemExit(
            "❌ 「同一阵营督军同卡」不成立："
            + "；".join(f"{ARMY_NAME.get(a, a)} → {r}" for a, r in sorted(bad.items()))
            + " —— 要人判，不猜")
    return {a: next(iter(r)) for a, r in per_army.items()}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只对账、不写文件")
    args = ap.parse_args()

    raw, cards = load_truth()
    m, rep = build(cards)
    per_army = check_invariant(m, cards)

    print(f"真值源      {TRUTH}（count={raw.get('count')} · cards={len(cards)}）")
    print(f"带 PPtr 的卡 {len(m)} 张（没有这个字段的 {rep['no_field']} 张）")
    for uid, why, val in rep["unresolved"]:
        print(f"  🔴 解不出：{uid} · {why} = {val!r}")
    if rep["unresolved"]:
        raise SystemExit("❌ 有解不出的 PPtr —— 停下（⛔ 不许丢掉它们继续写）")

    print(f"不变量 ✅ 同一阵营督军同卡：{len(per_army)} 个阵营")
    for a in sorted(per_army):
        ref = per_army[a]
        who = sorted(u for u, v in m.items() if v["refId"] == ref)
        nm = m[who[0]]["refName"]
        print(f"  {ARMY_NAME.get(a, a):<16} → {ref:<6} {nm:<22}（{len(who)} 位督军：{'、'.join(who)}）")

    doc = {
        "note": "由 allcards 包解析：卡 SO 的 goSecondCardInHand 是【同包内 PPtr】，按 m_PathID 解成卡 id",
        "source": "d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json（**当前 build** · 1,351 张卡 SO）",
        "generator": "d:/4/Unity/工具/gen_warlord_goes_second.py",
        "provenance": (
            "⚠️ 2026-10-23 生成器换源（引擎线 P1·②）：旧表是 2025-07-30 旧 profile 快照的 55 位督军"
            "（`d:/2/远端缓存_旧profile_1013/…`），EmperorsChildren / SpaceWolves 两族整族缺席"
            "⇒ 那 6 位督军后手开局拿不到防御卡。现在改从**当前 build** 的卡 SO 快照重解，"
            "形状与字段语义不变。判据 → 资料/普查产出_1023c/引擎深审C_数据吻合.md §2·2。"),
        "invariant": (
            f"🔴 本表实测的不变量（{len(m)}/{len(m)} 全中，2026-10-23 现核）：**同一阵营的所有督军指向"
            f"【同一张】goSecondCardInHand** —— {len(per_army)} 个阵营各一张，没有例外。"
            "gen_warlord_goes_second.py 每次生成都会**重新断言这条不变量**，不成立就炸（不静默取一张）；"
            "gen_cards_engine.py 的 resolve_warlord_defence 再断言一次（按【阵营】落位）。"
            "⚠️ 这是**原版数据的实测形状**，不是我们对规则的推断。"),
        "count": len(m),
        "map": {k: m[k] for k in sorted(m)},
    }

    if args.check:
        old = json.load(open(OUT, encoding="utf-8")) if os.path.exists(OUT) else {}
        same = old.get("map") == doc["map"]
        print(f"\n--check：盘上那份与现算的 `map` {'**一致**' if same else '**不一致**（要重跑写文件）'}"
              f"（盘上 {len(old.get('map') or {})} 条 / 现算 {len(doc['map'])} 条）")
        return 0 if same else 1

    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, indent=2)
        f.write("\n")
    print(f"\nwrote {OUT}（{len(doc['map'])} 条）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
