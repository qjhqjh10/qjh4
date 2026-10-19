# -*- coding: utf-8 -*-
"""gen_card_inventory_options.py — 原版 `inventoryOptions` / `showInCollectionOptions` 真值 → 数据件

**为什么要有它**（`A1798`）：
  「哪张卡**不能进卡组**」在 `RuleEngine/Core/DeckRules.cs` 里原来是**我们自己定的口径**
  （`IsEffectOnly(subtype)`，命中 15 张、全是 GUID 卡）。原版不是这么判的 —— 判据是卡 SO 上的两个字段：

      `CardInventoryOptions`  : `InInventory = 0` · `CantAddToDeck = 5` · `NotInInventory = 10`
                                （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardInventoryOptions.cs`）
      `ShowInCollectionOptions`: `Hide = 0` · `Show = 5` · `OnlyIfOwned = 10`
                                （同目录 `ShowInCollectionOptions.cs`）

  原版用法（三个方法体逐句实读，`d:/2/tools/decomp_full/`）：
    · `DeckUtility__ValidateDeckCards.c:68-72`  `inventoryOptions != 0`        ⇒ `*param_3 = 4`，return 0
    · `DeckUtility__ValidateDeckCards.c:73-77`  `showInCollectionOptions != 5` ⇒ `*param_3 = 4`，return 0
    · `DeckUtility__TryRemoveInvalidCards.c:39-43` 上面两条 + 卡型 + 阵营，**任一成立就移除该卡**
    · `CardDeck__CanAddCard.c:40`  ⚠️ 它写的是 `A && B`（**与上三者不一致**，见下面「已知不一致」）

  ⚠️ **已知不一致（本脚本不替它选，如实记）**：
    `CanAddCard` 那一处把两条写成 `&&`（`A && B ⇒ InvalidCard`），而 `ValidateDeckCards` /
    `TryRemoveInvalidCards` 都是**各自独立成立即拦**（= `A || B`）。实测差集 = **77 张**
    （72 张 `CantAddToDeck` + 5 张 `NotInInventory & Show` 的暗黑天使秘仪卡）。
    ✅ **可自洽的解释**（写进 `资料/普查产出_第二十八会话/W9_卡组分流按原版.md`）：
      `NotInInventory` 的原意在原版里由**持有数**兜住（`CanAddCard.c:76-77` 的
      `InventoryManager.GetOwnedCount` 对这类卡返回 0 ⇒ 照样加不进），
      `&&` 那两条只是「完全内部的卡」的冗余兜底 ⇒ 原版看不出差别。
      **我们是单机全解锁（没有持有数这道闸）** ⇒ 必须按 `||` 那一版做，否则那 77 张会静默变得能进卡组。
    ⇒ 落进引擎的判定 = `inv != InInventory(0) || show != Show(5) ⇒ 不能进卡组`（`DeckRules.CannotAddToDeck`）。

**真值源（本机唯一一份，只读）**：
    `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`
    = 旧 Windows 用户缓存里抢救出的原版远端包 `allcards_assets_all`（1195 张卡 SO，全字段）。
    同源同批的兄弟件：`warlord_goes_second.json`（`A1070`）· `related_cards.json`（`A1796`）。
    ⚠️ 年代限制：该包是 Unity 2022.3.38f1 / 2025-07-30 的快照 ⇒ **比现池旧**
    （实测：整个 **SpaceWolves** 阵营 + 暗黑天使 `DA75..DA74` 等新卡**快照里没有**）。

**产出**：`d:/4/Unity/数据/游戏数据/card_inventory_options.json`（键 = **我们卡池的 id**）
    消费点 = `工具/gen_cards_engine.py` 的 `load_inventory_options()` → `cards_engine.json` 两列
    → `RuleEngine/Data/CardDatabase.cs` → `RuleEngine/Core/CardDef.cs` → `DeckRules.CannotAddToDeck()`。

**跑法**（⚠️ 顺序：**先本脚本、再 `gen_cards_engine.py --write`** —— 本脚本要读现卡池的 id/阵营；
   ⚠️ `gen_cards_engine.py` 不带 `--write` 是干跑（只打印、不落盘，`A1648`））：
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_card_inventory_options.py
    ... --check     只对账，不写文件
"""
import argparse
import collections
import io
import json
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8")

# 🔴 **2026-10-18（`K06`）换源**：原来是 2025-07-30 快照（1,195 张）；当前 build 的 `allcards`
#   已找到（1,351 张）⇒ 换过来。形状 `{bundle,count,cards:[{…,full_raw}]}`，`build()` 里展平。
TRUTH = r"d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"
POOL = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
DST = r"d:/4/Unity/数据/游戏数据/card_inventory_options.json"

# 原版 id 前缀 ↔ 我们的阵营名（与 `gen_cards_engine.py` 的 `ID_PREFIX` **同一份**；那边是权威，
# 这里复制一份只为「按阵营拆真值」。两处不一致时**以 gen_cards_engine.py 为准**并改这里。）
ID_PREFIX = {
    "AM": "AstraMilitarum", "ASH": "SaimHann", "BL": "BlackLegion", "DA": "DarkAngels",
    "EC": "EmperorsChildren", "GSC": "Genestealers", "GOF": "Goff", "TL": "Leviathan",
    "SAU": "Sautekh", "SOR": "Sororitas", "SW": "SpaceWolves", "TAU": "TauEmpire",
    "UM": "Ultramarines",
}
PREFIX_OF = {v: k for k, v in ID_PREFIX.items()}

IN_INVENTORY, CANT_ADD, NOT_IN_INVENTORY = 0, 5, 10
SHOW, HIDE, ONLY_IF_OWNED = 5, 0, 10


def load_json(p):
    with io.open(p, "r", encoding="utf-8") as f:
        return json.load(f)


def fac_to_army(truth, pool):
    """阵营名 → 原版 `cardArmy` 编号。

    判据 = **数字 id 的卡**：`UM*` 全是 10、`ASH*` 全是 30 …（实测 12/13 阵营，SW 在快照里没有）。
    ⚠️ 只认「前缀 → 阵营」与「前缀 → cardArmy」**都唯一**的那些；对不上就不给映射（不猜）。
    """
    pre_army = collections.defaultdict(collections.Counter)
    for t in truth:
        uid = (t.get("uniqueId") or "").strip()
        m = re.match(r"^([A-Za-z]+)", uid)
        if m and uid[len(m.group(1)):].isdigit():
            pre_army[m.group(1)][t.get("cardArmy")] += 1
    pre_fac = collections.defaultdict(collections.Counter)
    for c in pool:
        m = re.match(r"^([A-Za-z]+)", c.get("id") or "")
        if m:
            pre_fac[m.group(1)][c.get("faction")] += 1
    out = {}
    for p, facs in pre_fac.items():
        if p in pre_army and len(facs) == 1 and len(pre_army[p]) == 1:
            out[list(facs)[0]] = list(pre_army[p])[0]
    return out


def build():
    truth = load_json(TRUTH)
    # 🆕 **2026-10-18（`K06`）**：当前 build 的解析产物是 `{cards:[{…,full_raw}]}` ⇒ 展平。
    if isinstance(truth, dict) and isinstance(truth.get("cards"), list):
        truth = [c.get("full_raw") or c for c in truth["cards"]]
    pool = load_json(POOL)["cards"]
    fac2army = fac_to_army(truth, pool)

    by_id = {(t.get("uniqueId") or "").strip(): t for t in truth if (t.get("uniqueId") or "").strip()}
    by_name = collections.defaultdict(list)
    for t in truth:
        nm = (t.get("cardName") or "").strip().lower()
        if nm:
            by_name[nm].append(t)

    def hit(c):
        """四级定位，⛔ 全程不猜：① 原版 id 直命中 ② 卡名 + 阵营(army) ③ 卡名唯一
        ④ 卡名重名但**所有候选的两个值完全一致**（那样重名不影响结论）。"""
        cid, name, fac = c.get("id") or "", (c.get("name") or "").strip(), c.get("faction")
        t = by_id.get(cid)
        if t is not None:
            return t, "id"
        cands = by_name.get(name.lower(), [])
        army = fac2army.get(fac)
        if army is not None:
            same = [x for x in cands if x.get("cardArmy") == army]
            if len(same) == 1:
                return same[0], "name+army"
        if len(cands) == 1:
            return cands[0], "name"
        if len(cands) > 1 and len({(x.get("inventoryOptions"), x.get("showInCollectionOptions")) for x in cands}) == 1:
            return cands[0], "name-dup-agree"
        return None, ("name-ambiguous" if len(cands) > 1 else "no-truth-row")

    mp, unknown, used = {}, {}, set()
    via = collections.Counter()
    for c in pool:
        t, how = hit(c)
        if t is None:
            unknown[c["id"]] = {"name": c.get("name"), "type": c.get("type"), "why": how}
            continue
        mp[c["id"]] = {"inv": t.get("inventoryOptions"), "show": t.get("showInCollectionOptions"),
                       "type": c.get("type"),
                       "via": how, "ref": (t.get("uniqueId") or t.get("cardName"))}
        via[how] += 1
        used.add(id(t))

    # 真值侧**没落进我们卡池**的那些（如实列出 —— 卡池里没有的卡也有真值，别丢）
    unmatched = [{"ref": (t.get("uniqueId") or t.get("cardName")), "name": t.get("cardName"),
                  "army": t.get("cardArmy"), "inv": t.get("inventoryOptions"),
                  "show": t.get("showInCollectionOptions")}
                 for t in truth if id(t) not in used]

    iv = collections.Counter(v["inv"] for v in mp.values())
    sc = collections.Counter(v["show"] for v in mp.values())

    ct = collections.Counter((v["inv"], v["show"]) for v in mp.values())
    blocked = [k for k, v in mp.items()
               if v["type"] not in ("hero", "defence")
               and (v["inv"] != IN_INVENTORY or v["show"] != SHOW)]
    return dict(mp=mp, unknown=unknown, unmatched=unmatched, via=via, iv=iv, sc=sc, ct=ct,
                truth=truth, pool=pool, blocked=blocked, fac2army=fac2army)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只对账，不写文件")
    args = ap.parse_args()

    r = build()
    mp, truth, pool = r["mp"], r["truth"], r["pool"]
    print(f"真值源      {TRUTH}")
    print(f"             {len(truth)} 行（非 0 `inventoryOptions` "
          f"{sum(1 for t in truth if t.get('inventoryOptions') not in (None, 0))} 张）")
    print(f"我们的卡池  {len(pool)} 张")
    print(f"接上真值    {len(mp)}/{len(pool)}   四级定位："
          + " · ".join(f"{k}={v}" for k, v in sorted(r['via'].items())))
    print(f"全池 inv 分布 " + " · ".join(f"{k if k is not None else 'null'}={v}" for k, v in sorted(
        r["iv"].items(), key=lambda kv: -1 if kv[0] is None else kv[0])))
    print(f"全池 show 分布 " + " · ".join(f"{k if k is not None else 'null'}={v}" for k, v in sorted(
        r["sc"].items(), key=lambda kv: -1 if kv[0] is None else kv[0])))
    print("(inv, show) 交叉表：")
    for k in sorted(r["ct"], key=lambda t: (-1 if t[0] is None else t[0], -1 if t[1] is None else t[1])):
        print(f"    inv={str(k[0]):>5}  show={str(k[1]):>5}  ⇒ {r['ct'][k]} 张")

    print(f"\n落不上的真值  {len(r['unmatched'])} 行（我们卡池里没有这些卡 —— 如实列出，⛔ 没丢）")
    unm, guid = collections.Counter(), 0
    for u in r["unmatched"]:
        s = str(u["ref"])
        if re.match(r"^[0-9a-f]{32}$", s):
            guid += 1
        else:
            pre = re.match(r"^[A-Za-z]+", s)
            unm[pre.group(0) if pre else "?"] += 1
    print(f"    其中 32 位 hex（GUID 卡）{guid} 行；其余按 id 前缀："
          + " · ".join(f"{k}={v}" for k, v in sorted(unm.items(), key=lambda kv: -kv[1])))

    print(f"\n接不上的卡    {len(r['unknown'])} 张 —— **真值里查不到它们**，`inv`/`show` 记 `-1`（未知）：")
    bywhy = collections.Counter(v["why"] for v in r["unknown"].values())
    print("    原因：" + " · ".join(f"{k}={v}" for k, v in sorted(bywhy.items(), key=lambda kv: -kv[1])))
    f_of = {c["id"]: c.get("faction") for c in pool}
    print("    按阵营：" + " · ".join(f"{k}={v}" for k, v in sorted(
        collections.Counter(f_of[i] for i in r["unknown"]).items(), key=lambda kv: -kv[1])))

    print(f"\n🔴 新口径会拦下（非 hero/defence）{len(r['blocked'])} 张 —— "
          f"其中 `CantAddToDeck(5)` {sum(1 for i in r['blocked'] if mp[i]['inv'] == CANT_ADD)} 张 · "
          f"`NotInInventory(10)` {sum(1 for i in r['blocked'] if mp[i]['inv'] == NOT_IN_INVENTORY)} 张 · "
          f"`show != Show` {sum(1 for i in r['blocked'] if mp[i]['show'] != SHOW)} 张")
    for val in (CANT_ADD, NOT_IN_INVENTORY):
        ids = sorted(i for i, v in mp.items() if v["inv"] == val)
        print(f"     `inventoryOptions={val}` 落进卡池的 **{len(ids)}** 张："
              f" 卡型 " + " · ".join(f"{k}={v}" for k, v in sorted(
                  collections.Counter(mp[i]["type"] for i in ids).items())))
        print("        " + "、".join(f"{i}({mp[i]['type']})" for i in ids))

    doc = {
        "note": "原版 `RawCardScript.inventoryOptions` / `showInCollectionOptions` 真值，"
                "键 = **我们卡池的 id**。消费点 = `工具/gen_cards_engine.py` 的 `load_inventory_options()`。",
        "source": TRUTH + "（原版 allcards，**当前 build 1,351 张**卡 SO 全字段；2026-10-18 `K06` 换源）",
        "provenance": "抢救来源 = 旧 Windows 用户目录的 Unity 缓存（按哈希命名）里的原版远端包 "
                      "allcards_assets_all；原件与全部导出 → d:/2/远端缓存_旧profile_1013/（含 README）。"
                      f"⚠️ 年代限制：该包是 Unity 2022.3.38f1 / 2025-07-30 的快照（1195 张 vs 现池 {len(pool)} 张）"
                      "—— 整个 SpaceWolves 阵营与暗黑天使 DA75..DA74 等新卡快照里没有 ⇒ 那些 `inv`/`show` = -1（未知）。",
        "usage": "`DeckUtility__ValidateDeckCards.c:68-72`（`inventoryOptions != 0` ⇒ 非法）· "
                 ":73-77（`showInCollectionOptions != 5` ⇒ 非法）· `DeckUtility__TryRemoveInvalidCards.c:39-43` · "
                 "⚠️ `CardDeck__CanAddCard.c:40` 写的是 `&&`（与前三处不一致，差集 77 张）—— "
                 "解释见本文件生成器头部注释；我们按 `||` 那一版做（单机全解锁、没有持有数那道闸）。",
        "invariant": "① `inv` 非 -1 时取自真值行、一字不改；② `-1` = 真值里查不到（四级定位全落空），"
                     "**不是 0**（⛔ 别把 -1 当 InInventory）；③ 259 张 32 位 hex 键的卡**全部** `inv=10`"
                     "（本会话现核 259/259）；④ `type` = 我们卡池里那张卡的卡型，用来复现原版"
                     "「hero 走 CanAddHero / 防御卡走 CanAddDefensive、都不吃这两个字段」那两条路由。",
        "counts": {
            "truthRows": len(truth), "poolCards": len(pool), "resolved": len(mp),
            "unresolvedTruthRows": len(r["unmatched"]), "poolUnknown": len(r["unknown"]),
            "byVia": dict(r["via"]),
            "inv": {str(k): v for k, v in r["iv"].items()},
            "show": {str(k): v for k, v in r["sc"].items()},
            "blockedNonHeroDefence": len(r["blocked"]),
        },
        "map": mp,
        "unknown": r["unknown"],
        "unmatchedTruthRows": r["unmatched"],
    }

    if args.check:
        print("\n--check：只对账，没写文件。")
        return
    if not mp:
        raise SystemExit("❌ 一张都没接上 —— 真值源或卡池对不上了，⛔ 不许写空表盖掉旧文件")
    # 🔴 先算进变量再写（本仓踩过：`io.open(p,'wb').write(<表达式>)` 表达式一抛异常文件就永久空掉）
    body = json.dumps(doc, ensure_ascii=False, indent=1) + "\n"
    with io.open(DST, "w", encoding="utf-8", newline="\n") as f:
        f.write(body)
    print(f"\n写入 {DST}   {len(body)} B（map {len(mp)} · unknown {len(r['unknown'])} · "
          f"未落真值行 {len(r['unmatched'])}）")


if __name__ == "__main__":
    main()
