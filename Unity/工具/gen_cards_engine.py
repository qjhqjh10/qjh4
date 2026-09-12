#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_cards_engine.py — 卡牌原始数据 → 规则引擎用的精简卡表

为什么要有这一道：
  1. `card_stats.json` 有 978 KB，大头是 `art` / `voice` / `ocrSrc` / `face` 这些
     **引擎完全用不到的路径字符串**。精简后只剩 273 KB，能进构建、能进仓库。
  2. 原始数据里数值字段**大量为 null**（`"cost": null` 有 124 处）。
     C# 侧 `JsonUtility` 遇到显式 null 不可靠 —— 在这里一次性归一化掉，
     生成的 JSON 里每个字段都有确定类型，`CardDatabase.cs` 就不用写防御代码了。

输出：`Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`
（放 `Resources/` 是为了运行时 `Resources.Load<TextAsset>("cards_engine")` 能取到 ——
  `.json` 进 Assets 默认就按 TextAsset 导入，232 KB 进构建可以接受）

用法：
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_cards_engine.py
    ... --check     只对账，不写文件
"""
import argparse
import json
import os
import sys

SRC = r"d:/4/Unity/数据/游戏数据/card_stats.json"
DST = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"

# 引擎需要的字段。`art`/`voice`/`ocrSrc`/`face`/`factionId`/`decks`/`tier` 全部丢掉。
KEEP = ("name", "type", "cost", "attack", "health", "ranged_attack",
        "keywords", "desc", "faction", "rarity")

# 卡牌类型白名单 —— 引擎认识这四种（见 CardDef.Type）
TYPES = ("unit", "tactic", "hero", "defence")


def norm_int(v):
    """None/非数字 → 0。原始数据里 null 很常见（59 张卡无数值）"""
    if v is None:
        return 0
    if isinstance(v, bool):
        return 0
    if isinstance(v, (int, float)):
        return int(v)
    return 0


def norm_str(v):
    return "" if v is None else str(v)


def norm_list(v):
    if v is None:
        return []
    if isinstance(v, list):
        return [str(x) for x in v if x is not None and str(x).strip()]
    return [str(v)]


def build():
    with open(SRC, encoding="utf-8") as f:
        raw = json.load(f)["cards"]

    cards, skipped = [], []
    for c in raw:
        ctype = norm_str(c.get("type"))
        if not c.get("hasStats"):
            skipped.append((norm_str(c.get("name")), "无数值(hasStats=false)"))
            continue
        if ctype not in TYPES:
            skipped.append((norm_str(c.get("name")), "未知类型 " + ctype))
            continue
        cards.append({
            "name":     norm_str(c.get("name")),
            "type":     ctype,
            "cost":     norm_int(c.get("cost")),
            "attack":   norm_int(c.get("attack")),
            "health":   norm_int(c.get("health")),
            "ranged":   norm_int(c.get("ranged_attack")),
            "keywords": norm_list(c.get("keywords")),
            "desc":     norm_str(c.get("desc")),
            "faction":  norm_str(c.get("faction")),
            "rarity":   norm_str(c.get("rarity")),
        })

    return {
        "version": 1,
        "source": "Unity/数据/游戏数据/card_stats.json",
        "note": "由 工具/gen_cards_engine.py 生成，不要手改。改数据请改 card_stats.json 后重跑。",
        "count": len(cards),
        "cards": cards,
    }, skipped, len(raw)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只对账，不写文件")
    args = ap.parse_args()

    doc, skipped, total = build()

    by_type = {}
    for c in doc["cards"]:
        by_type[c["type"]] = by_type.get(c["type"], 0) + 1

    print(f"源卡数      {total}")
    print(f"收录        {doc['count']}    " + "  ".join(f"{k}={v}" for k, v in sorted(by_type.items())))
    print(f"跳过        {len(skipped)}")
    for name, why in skipped[:5]:
        print(f"            · {name}: {why}")
    if len(skipped) > 5:
        print(f"            … 其余 {len(skipped) - 5} 张同理")

    if args.check:
        if os.path.exists(DST):
            old = json.load(open(DST, encoding="utf-8"))
            same = old.get("count") == doc["count"] and old.get("cards") == doc["cards"]
            print(f"\n--check：现有 {DST} {'一致 ✅' if same else '**已过期** ❌（重跑不带 --check）'}")
            return 0 if same else 1
        print(f"\n--check：{DST} 不存在")
        return 1

    os.makedirs(os.path.dirname(DST), exist_ok=True)
    with open(DST, "w", encoding="utf-8") as f:
        json.dump(doc, f, ensure_ascii=False, separators=(",", ":"))
    print(f"\n写出 {DST}  ({os.path.getsize(DST) / 1024:.0f} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
