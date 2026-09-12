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
import difflib
import json
import os
import re
import sys

SRC = r"d:/4/Unity/数据/游戏数据/card_stats.json"
DST = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
# 稀有度权威来源：vision 逐张看卡面宝石颜色判的，1118/1118 全覆盖
RARITY_SRC = r"d:/4/Unity/资料/卡牌数据表/卡牌宝石稀有度_0824.md"

# 引擎认识的稀有度取值（`special` = 橙红宝石，防御/药剂/特殊卡）
RARITIES = ("common", "rare", "epic", "legendary", "special")

# 引擎需要的字段。`art`/`voice`/`ocrSrc`/`face`/`factionId`/`decks`/`tier` 全部丢掉。
KEEP = ("name", "type", "cost", "attack", "health", "ranged_attack",
        "keywords", "desc", "faction", "rarity")

# 卡牌类型白名单 —— 引擎认识这四种（见 CardDef.Type）
TYPES = ("unit", "tactic", "hero", "defence")


def _norm_name(s):
    """卡名归一化：去掉大小写、空格、标点和各种连字符（含 U+2011 不换行连字符）。"""
    return re.sub(r"[^a-z0-9]", "", (s or "").lower())


def load_rarity():
    """读「卡牌宝石稀有度_0824.md」→ {卡名: 稀有度}。

    **为什么必须覆盖**：`card_stats.json` 的 `rarity` 字段有两类毛病 ——
      ① 239 张是空串（OCR 没读出来）
      ② 102 张写成了 `defence`（那是**卡牌类型**串到稀有度字段里了，不是稀有度）
    而这张表是逐张看卡面宝石颜色判的，1118/1118 全覆盖，是唯一的权威来源。
    卡组编辑要用稀有度做筛选和卡表行底色，所以这一层不能省。
    """
    tbl = {}
    if not os.path.exists(RARITY_SRC):
        return tbl
    with open(RARITY_SRC, encoding="utf-8") as f:
        for line in f:
            if not line.startswith("|"):
                continue
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if len(cells) < 5 or cells[4] not in RARITIES:
                continue
            tbl[cells[2]] = cells[4]
    return tbl


def make_rarity_lookup(tbl):
    """两级查表：①原名 ②归一化名（去大小写/空格/标点）③编辑距离兜底。

    ②③ 是必要的 —— OCR 出来的卡名和宝石表里的写法常有出入：
    `Helfire Pit` vs `Hellfire Pit`、`Land Rider` vs `Land Raider`、
    `Tyrannic War Veteran` vs `Tyranic War Veteran`。没有这两级的话有 341 张补不上。
    ③ 的阈值卡在 0.88，宁可漏也不要错配（错配会让筛选和底色看起来对、其实错）。
    """
    by_name = dict(tbl)
    by_norm = {}
    for k, v in tbl.items():
        by_norm.setdefault(_norm_name(k), v)

    def lookup(name):
        """返回 (稀有度, 匹配方式)；查不到返回 (None, None)。"""
        if name in by_name:
            return by_name[name], "原名"
        n = _norm_name(name)
        if n in by_norm:
            return by_norm[n], "归一化"
        cand = difflib.get_close_matches(n, list(by_norm), n=1, cutoff=0.88)
        if cand:
            return by_norm[cand[0]], f"模糊~{cand[0]}"
        return None, None

    return lookup


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

    rarity_lookup = make_rarity_lookup(load_rarity())
    cards, skipped = [], []
    filled, still_missing = [], []
    for c in raw:
        ctype = norm_str(c.get("type"))
        if not c.get("hasStats"):
            skipped.append((norm_str(c.get("name")), "无数值(hasStats=false)"))
            continue
        if ctype not in TYPES:
            skipped.append((norm_str(c.get("name")), "未知类型 " + ctype))
            continue
        name = norm_str(c.get("name"))
        rarity = norm_str(c.get("rarity"))
        if rarity not in RARITIES:
            # 空串或 `defence` 这类错值 —— 拿宝石扫描表顶上
            got, how = rarity_lookup(name)
            if got:
                filled.append((name, rarity, got, how))
                rarity = got
            else:
                still_missing.append(name)
                rarity = ""
        cards.append({
            "name":     name,
            "type":     ctype,
            "cost":     norm_int(c.get("cost")),
            "attack":   norm_int(c.get("attack")),
            "health":   norm_int(c.get("health")),
            "ranged":   norm_int(c.get("ranged_attack")),
            "keywords": norm_list(c.get("keywords")),
            "desc":     norm_str(c.get("desc")),
            "faction":  norm_str(c.get("faction")),
            "rarity":   rarity,
        })

    return {
        "version": 2,
        "source": "Unity/数据/游戏数据/card_stats.json（稀有度另取 资料/卡牌数据表/卡牌宝石稀有度_0824.md）",
        "note": "由 工具/gen_cards_engine.py 生成，不要手改。改数据请改 card_stats.json 后重跑。",
        "count": len(cards),
        "cards": cards,
    }, skipped, len(raw), filled, still_missing


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只对账，不写文件")
    args = ap.parse_args()

    doc, skipped, total, filled, still_missing = build()

    by_type = {}
    for c in doc["cards"]:
        by_type[c["type"]] = by_type.get(c["type"], 0) + 1
    by_rarity = {}
    for c in doc["cards"]:
        k = c["rarity"] or "(空)"
        by_rarity[k] = by_rarity.get(k, 0) + 1

    print(f"源卡数      {total}")
    print(f"收录        {doc['count']}    " + "  ".join(f"{k}={v}" for k, v in sorted(by_type.items())))
    print(f"跳过        {len(skipped)}")
    for name, why in skipped[:5]:
        print(f"            · {name}: {why}")
    if len(skipped) > 5:
        print(f"            … 其余 {len(skipped) - 5} 张同理")

    print(f"\n稀有度       " + "  ".join(f"{k}={v}" for k, v in sorted(by_rarity.items())))
    print(f"  用宝石表补正 {len(filled)} 张（原值是空串或 defence 这类错值）")
    for name, old, new, how in filled[:4]:
        print(f"            · {name}: {old or '(空)'} → {new}  [{how}]")
    if len(filled) > 4:
        print(f"            … 其余 {len(filled) - 4} 张同理")
    fuzzy = [f for f in filled if f[3].startswith("模糊")]
    if fuzzy:
        print(f"  其中靠**模糊匹配**的 {len(fuzzy)} 张（卡名拼写有出入，建议抽查）：")
        for name, old, new, how in fuzzy:
            print(f"            · {name} → {new}  [{how}]")
    if still_missing:
        print(f"  ⚠️ 宝石表里也没有的 {len(still_missing)} 张：" + "、".join(still_missing[:6]))

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
