# -*- coding: utf-8 -*-
"""gen_card_initiative.py — 原版 `RawCardScript.initiative` 真值 → 数据件

**为什么要有它**（账 `A1775`）：
  「谁先手」在 `CardPresentation/Battle/BattleDriver.cs` 里原来**只做掷硬币**，
  理由写在 `:2985-2993`：「所有督军 `initiative` 相同」——
  🔴 那是 **2026-09-26 的一次【用户拍板】**（「投硬币更公平、不需要为每个督军设先攻值」），
  不是原版事实。**用户 2026-10-14 改成「全部照原版复刻」** ⇒ 按真值判先后手。

原版链路（`d:/2/tools/decomp_full/`，方法体逐句实读）：
  · `BattleManager__GetPlayerGoesFirst.c` —— 顶层：① 模式特例 ② 督军 `initiative` 比大小
    ③ 平局缓存 ④ `MatchData.playerIdGoesFirst` ⑤ 都没有 ⇒ 掷硬币（`MatchData.GetRandomInt(0,2)`）。
  · `SupportMethods__GetPlayerGoesFirstWithInitiative.c` —— 比大小那一支的真身：
      `if (player.initiative (deck+0x58 → +0x18 → +0x12C) > enemy.initiative (deck+0x50 …)) return true;`
      `if (enemy > player) return false;` … 平局之后才轮到 `forceFirstPlayer` / 硬币。
  · `Initiative` 枚举（`Assembly-CSharp/Initiative.cs`）：
      `Undefined = 0` · `Low = 10` · `Medium = 20` · `High = 30` · `VeryHigh = 40`。

**真值源（本机唯一一份，只读）**：
  `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`
  （原版远端包 `allcards_assets_all`，1195 张卡 SO，全字段；快照 **2025-07-30** —— **比现池旧**）。
  实测分布：`20`(Medium) **1190 行** · `30`(High) **2 行**（`Marneus Calgar (tutorial)` /
  `Uriel Ventris (tutorial)` 两张教程督军）· 缺这个键 **3 行** ⇒ 共 1192 行有值。

**id 匹配：本脚本【不】自己写一套**
  —— 直接读兄弟件 `数据/游戏数据/card_inventory_options.json` 的 `map`
  （`{我们的卡id: {ref: 原版 uniqueId, via: …}}`，那是 `A1798` 那批**已经落地并自检过**的四级定位结果）。
  ⇒ 一处匹配、两处消费；⛔ 别在这儿再抄一份 `id → 真值行` 的匹配规则（两处必然分叉）。

**产出**：`d:/4/Unity/数据/游戏数据/card_initiative.json`（键 = **我们卡池的 id**）
  消费点 = `工具/gen_cards_engine.py` 的 `load_initiative()` → `cards_engine.json` 的 `initiative` 一列
  → `RuleEngine/Data/CardDatabase.cs` → `RuleEngine/Core/CardDef.cs`（`CardDef.Initiative`）
  → `RuleEngine/Core/FirstSeatRules.cs`（先后手判据）。

**跑法**（⚠️ 顺序：**先 `gen_card_inventory_options.py`、再本脚本、最后 `gen_cards_engine.py --write`** ——
   ⚠️ `gen_cards_engine.py` 不带 `--write` 是干跑（只打印、不落盘，`A1648`））：
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_card_initiative.py
    ... --check     只对账，不写文件
"""
import argparse
import collections
import io
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

# 🔴 **2026-10-18（`K06`）换源**：原来是 2022/2025-07-30 快照（1,195 张），池内 SW/UM/DA 一族查不到；
#   当前 build 的 `allcards` 已找到（1,351 张）⇒ 换过来。形状 `{bundle,count,cards:[{…,full_raw}]}`，
#   `load_truth()` 里展平。
TRUTH = r"d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"
POOL = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
INV = r"d:/4/Unity/数据/游戏数据/card_inventory_options.json"
DST = r"d:/4/Unity/数据/游戏数据/card_initiative.json"

NOTE = (
    "原版 `RawCardScript.initiative`（`Initiative` 枚举：Undefined=0 / Low=10 / Medium=20 / "
    "High=30 / VeryHigh=40）按【我们卡池的 id】落成的一张表，供先后手判据用（账 `A1775`）。"
    "真值源 = **当前 build** 的 `allcards`（1,351 张，2026-10-18 `K06` 换源；此前是 2025-07-30 的 1,195 张快照）；"
    "id 匹配直接复用 "
    "`card_inventory_options.json` 的 `map`（那里已经做过四级定位），本件不另写一套。"
)
INVARIANT = (
    "① `map` 的值只取 `{0,10,20,30,40}` 或 `-1`；② `-1` = **真值里查不到这张卡**"
    "（快照比现池旧），⛔ 别当 `0`(Undefined)；③ 判据用它的地方 = `FirstSeatRules.ByInitiative`，"
    "**缺值(`-1`)不比**（退回原版 ③④⑤，即掷硬币），⛔ 不许猜一个默认值顶上。"
)


def load_truth():
    rows = json.load(io.open(TRUTH, encoding="utf-8"))
    # 🆕 **2026-10-18（`K06`）**：当前 build 的解析产物是 `{cards:[{…,full_raw}]}` ⇒ 展平。
    if isinstance(rows, dict) and isinstance(rows.get("cards"), list):
        rows = [c.get("full_raw") or c for c in rows["cards"]]
    return {r.get("uniqueId"): r for r in rows if r.get("uniqueId")}


def load_pool():
    return json.load(io.open(POOL, encoding="utf-8"))["cards"]


def load_inv_map():
    d = json.load(io.open(INV, encoding="utf-8"))
    return d.get("map") or {}


def build():
    truth, pool, invm = load_truth(), load_pool(), load_inv_map()
    mp, unknown, dist = {}, [], collections.Counter()
    for c in pool:
        cid = c.get("id")
        e = invm.get(cid)
        val = None
        if e:
            r = truth.get(e["ref"])
            if r is not None:
                val = r.get("initiative")
        if val is None:
            unknown.append({"id": cid, "name": c.get("name")})
            mp[cid] = {"initiative": -1}
            dist[-1] += 1
        else:
            mp[cid] = {"initiative": int(val), "ref": e["ref"], "via": e["via"]}
            dist[int(val)] += 1
    return {
        "note": NOTE,
        "source": TRUTH,
        "provenance": (
            "原版远端包 `allcards_assets_all`（旧 Windows 用户缓存里抢救出来，只读）；"
            "id 匹配 = `数据/游戏数据/card_inventory_options.json` 的 `map`（`A1798` 那批的四级定位）"
        ),
        "invariant": INVARIANT,
        "counts": {
            "pool": len(pool),
            "mapped": len(pool) - len(unknown),
            "unknown": len(unknown),
            "byValue": {str(k): dist[k] for k in sorted(dist)},
        },
        "map": mp,
        "unknown": unknown,
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只对账，不写文件")
    a = ap.parse_args()

    doc = build()
    c = doc["counts"]
    print(f"卡池 {c['pool']} 张 · 接到真值 {c['mapped']} · 查不到 {c['unknown']}")
    print(f"取值分布：{c['byValue']}")
    if c["unknown"]:
        print("⚠️ 查不到的（记 -1，判据里**不比**）：")
        for u in doc["unknown"][:12]:
            print("   ", u["id"], u["name"])
        if len(doc["unknown"]) > 12:
            print(f"    … 另 {len(doc['unknown']) - 12} 张")

    text = json.dumps(doc, ensure_ascii=False, separators=(",", ":"), sort_keys=False)
    if a.check:
        if not os.path.exists(DST):
            print(f"❌ --check：{DST} 不存在")
            raise SystemExit(1)
        old = io.open(DST, encoding="utf-8").read()
        print("✅ --check：与盘上那份**逐字节相同**" if old == text
              else "❌ --check：与盘上那份**不同**（重跑一次不带 --check）")
        raise SystemExit(0 if old == text else 1)

    io.open(DST, "w", encoding="utf-8", newline="\n").write(text)
    print(f"✅ 写好 {DST}（{len(text)} B）")


if __name__ == "__main__":
    main()
