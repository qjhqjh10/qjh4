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
# 中文卡名/效果文字：键 = 英文卡名。**只有这一份**（原来是 7 个 `_tmp_zhcards_g*.json`，
# 2026-09-12 合并成 zh_cards.json —— `_tmp_` 那个名字有被当临时文件清掉的风险）。
ZH_SRC = r"d:/4/Unity/数据/卡牌翻译/zh_cards.json"

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

    **为什么必须覆盖**：`card_stats.json` 的 `rarity` 字段有三类毛病 ——
      ① 239 张是空串（OCR 没读出来）
      ② 102 张写成了 `defence`（那是**卡牌类型**串到稀有度字段里了，不是稀有度）
      ③ **140 张把非传说卡写成了 `legendary`**（2026-09-12 发现的，见调用处的更正注释）
    而这张表是逐张看卡面宝石颜色判的，1118/1118 全覆盖，是唯一的权威来源。
    卡组编辑要用稀有度做筛选和卡表行底色，**卡框还要按它取 tier1–4**，所以这一层不能省。
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


def load_zh():
    """读中文卡名/效果文字表 → {英文卡名: {"nameZh":..., "descZh":...}}。

    ⚠️ 原表里有 **12 处同名冲突**：大多是 `Normal Conditions` 这类**关键词术语**被各分组
    译得略有出入；真正的卡名冲突只有 4 张已知重名卡（Aggressor / Terminator /
    Terminator Champion / Maulerfiend）。合并时**后写的赢**，冲突原样记在
    zh_cards.json 的 `conflicts` 字段里，要修去那儿看。
    （这也顺带说明「卡名当 id」这个设计迟早要改。）
    """
    if not os.path.exists(ZH_SRC):
        print(f"⚠️ 找不到中文表 {ZH_SRC} —— 生成出来的卡表没有中文（卡面会显示英文）")
        return {}
    doc = json.load(open(ZH_SRC, encoding="utf-8"))
    out = {}
    for name, v in doc.get("cards", {}).items():
        nzh, dzh = (v.get("n") or "").strip(), (v.get("d") or "").strip()
        e = {}
        if nzh:
            e["nameZh"] = nzh
        if dzh:
            e["descZh"] = dzh
        if e:
            out[name] = e
    print(f"中文表       {len(out)} 条（源 {doc.get('count')}，冲突 {len(doc.get('conflicts') or [])} 处）")
    return out


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
    zh = load_zh()
    cards, skipped = [], []
    _seen_names = set()   # (阵营, 归一化卡名) —— 判重只在这个粒度上做
    filled, still_missing = [], []
    for c in raw:
        ctype = norm_str(c.get("type"))
        # ⚠️ **`noise=true` 的整条丢掉**（2026-09-12 加）：那批是**根本不是卡**的索引噪音 ——
        #    `Normal Conditions` / `Normal` / `Default Conditions` / `v2` 这类占位条目（无卡面、
        #    无数值、任何卡组都不引用），加上两张重复卡（`HB` = Imotekh 的异画版、
        #    `Threnodic Choir Flawless` = 与另一张同图）。原来它们**混在卡池里**，
        #    卡组编辑器和自动凑牌都可能抽到「一张没有卡面的假卡」。
        #    判定和理由都在源表里（`noise_reason`），这里只负责过滤。
        if c.get("noise"):
            skipped.append((norm_str(c.get("name")), "噪音卡(" + norm_str(c.get("noise_reason"))[:40] + ")"))
            continue
        if not c.get("hasStats"):
            skipped.append((norm_str(c.get("name")), "无数值(hasStats=false)"))
            continue
        if ctype not in TYPES:
            skipped.append((norm_str(c.get("name")), "未知类型 " + ctype))
            continue
        name = norm_str(c.get("name")).strip()
        name = re.sub(r"\s+", " ", name)          # 空白归一：源表里有 ` Iron Priest`（前导空格）这种
        # ⚠️ **同阵营同名的丢掉后一条**（2026-09-12 加）：源表里有一对
        #    ` Iron Priest` / `Iron Priest`（前导空格造成的重复），两条数值一模一样。
        #    按 **(阵营, 归一化名字)** 判重 —— 不能只按名字，原版本来就有跨阵营同名卡
        #    （Aggressor / Terminator 那几个），那些是**不同的卡**，不能误删。
        if (norm_str(c.get("faction")), name) in _seen_names:
            skipped.append((name, "同阵营重名（前一条已收）"))
            continue
        _seen_names.add((norm_str(c.get("faction")), name))
        rarity = norm_str(c.get("rarity"))
        # ⚠️ 2026-09-12 更正：这里原来写的是 `if rarity not in RARITIES:` —— 只把宝石表当**补丁**用
        #    （仅在原值是空串 / `defence` 这类**非法值**时才查），于是 OCR 写错但**看起来合法**的值
        #    被原样放行。实测：**151 张与宝石表冲突，其中 140 张是我们写成 `legendary`、
        #    宝石实测是 common/rare/epic/special**（Autarch / Night Spinner / Shining Spear / Ursula Creed…）。
        #    拿卡面核过实：Night Spinner 与 Shining Spear 卡面底部那颗菱形宝石都是**浅蓝 = common**，
        #    OCR 记的 `legendary` 是错的 —— 那句「宝石表是唯一的权威来源」原来只写在文档字符串里，
        #    代码没照做。**现在只要宝石表里有这张卡就用它**。
        #    影响面：稀有度 → 卡框取 tier1–4（`CardArt.TierOf`）+ 底部宝石颜色，这 151 张一直挂错档。
        got, how = rarity_lookup(name)
        if got:
            if got != rarity:
                filled.append((name, rarity, got, how))
            rarity = got
        elif rarity not in RARITIES:
            # 宝石表里也没有这张（卡名对不上）—— 原值本身也不合法，才算缺失
            still_missing.append(name)
            rarity = ""
        entry = {
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
        }
        # 中文（有才写：没翻译的卡面自动回英文，不写空串进来白占体积）
        for k, v in zh.get(name, {}).items():
            entry[k] = v
        cards.append(entry)

    return {
        "version": 3,
        "source": "Unity/数据/游戏数据/card_stats.json（稀有度另取 资料/卡牌数据表/卡牌宝石稀有度_0824.md；"
                  "中文另取 数据/卡牌翻译/zh_cards.json）",
        "note": "由 工具/gen_cards_engine.py 生成，不要手改。"
                "改数据请改 card_stats.json / 数据/卡牌翻译/zh_cards.json 后重跑。"
                "nameZh / descZh 是可选字段 —— 没有的卡面回英文。",
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
    print(f"  用宝石表定档 {len(filled)} 张（宝石表是权威来源，只要它在表里就用它）")
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

    zh_n = sum(1 for c in doc["cards"] if c.get("nameZh"))
    zh_d = sum(1 for c in doc["cards"] if c.get("descZh"))
    missing = doc["count"] - zh_n
    print(f"\n中文名       {zh_n}/{doc['count']}   中文效果 {zh_d}/{doc['count']}"
          f"   （缺 {missing} 张，卡面回英文、不静默）")

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
