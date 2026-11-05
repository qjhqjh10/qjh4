# -*- coding: utf-8 -*-
"""gen_lore.py — 把官方 I2 词条表里的**两族风味文本（Lore）**抽成运行时数据件。

账：`A1802`（Lore 一条没接）+ `A1500①`（同一件事，两笔一起关）。

## 两族是什么（判据）

| 族 | 键 | 用在 |
|---|---|---|
| `Warlord_Lore/<卡 id>` | 键**就是**我们 `cards_engine.json` 的 `id`（例 `Warlord_Lore/GOF28`） | 卡牌详情窗 / 战斗展示窗的 `LoreText` 那一行 |
| `Cosmetic_Lore/<uniqueId>` | `uniqueId` = `Resources/Cardbacks.json` 每条的 `uniqueId` | 卡背（收藏页）—— **今天还没有消费点**，先落数据 |

- 原版行为（第一权威）：`SetCardLore` 取 `GetLocalizedFlavourText()`，**空则 `SetActive(loreObjectBG,false)`**。
  ⇒ 我们的两扇窗必须「没有 lore ⇒ 整块底板不亮」，**不许拿卡面效果文字回填**。
- 数据源：`d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json`
  （原版 I2 `I2Languages` 远端包，2026-10-14 由 `工具/_i2_decode_probe.py` 的算法解开；本脚本只做**抽取**）。

## 输出（两份，同一次运行产出 ⇒ 不会分叉）

1. `d:/4/Unity/数据/游戏数据/lore_zh+en.json` —— **全量台账**（与 `官方本地化_en+zh-CN.json` 同形：
   `_note/_source/_langs/_count/terms{ key: [en, zh] }`）。
2. `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/lore_zh+en.json` —— **运行时件**
   （`JsonUtility` 不支持字典 ⇒ 用 `entries` 数组；消费口 = `Core/Loc.cs` 的 `Loc.Lore` / `Loc.LoreByUniqueId`）。

## 覆盖范围（脚本每次都会现算并打印，⛔ 别把数字抄进别处）

`Warlord_Lore` 是 **2025-07-30 快照**：快照之后新增的卡**没有键** ⇒ 那些卡按原版「无 lore」处理（关底板），
脚本会把**逐类命中数 + 没命中的 id 样本**打出来。

用法：
  python -I -X utf8 d:/4/Unity/工具/gen_lore.py
  python -I -X utf8 d:/4/Unity/工具/gen_lore.py --report-only
"""

import argparse
import collections
import io
import json
import os
import sys

ROOT = r"d:/4/Unity"
SRC = os.path.join(ROOT, "数据", "游戏数据", "官方本地化_en+zh-CN.json")
CARDS = os.path.join(ROOT, "MyGame", "Assets", "RuleEngine", "Resources", "cards_engine.json")
CARDBACKS = os.path.join(ROOT, "MyGame", "Assets", "CardPresentation", "Resources", "Cardbacks.json")

OUT_FULL = os.path.join(ROOT, "数据", "游戏数据", "lore_zh+en.json")
OUT_RES = os.path.join(ROOT, "MyGame", "Assets", "CardPresentation", "Resources", "lore_zh+en.json")

FAMILIES = ("Warlord_Lore/", "Cosmetic_Lore/")

NOTE = (
    "官方 I2 词条表里的两族风味文本（Lore）。"
    "`Warlord_Lore/<卡 id>` 1042 条（键 = 我们 `cards_engine.json` 的 `id`）；"
    "`Cosmetic_Lore/<uniqueId>` 154 条（键 = `Resources/Cardbacks.json` 的 `uniqueId`）。"
    "⚠️ 年代 = 2025-07-30 的远端快照 ⇒ 快照之后新增的卡**没有键**；"
    "那些卡按原版「无 lore ⇒ 关掉 loreObjectBG」处理，⛔ 不拿效果文字回填。"
    "生成器 = `工具/gen_lore.py`；消费口 = `Core/Loc.cs` 的 `Loc.Lore` / `Loc.LoreByUniqueId`。"
)


def load_json(path):
    with io.open(path, encoding="utf-8") as f:
        return json.load(f)


def json_text(obj):
    """与 `dump_json()` 逐字节相同的文本（LF 结尾）。"""
    text = json.dumps(obj, ensure_ascii=False, indent=1)
    return text + "\n"


def dump_json(path, obj):
    """整篇写（新件；行尾固定 LF）。先算好字符串再写，避免半截文件。"""
    text = json_text(obj)
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def main():
    # 🔴 2026-11-05（`B145-E27`）：动词参数改成必填（CLAUDE.md §三「写盘脚本裸跑必须什么都不做」）。
    #   改前：裸跑 = 直接覆盖两份 `lore_zh+en.json`。现在裸跑只打印用法 + rc 2。
    #   `--report-only` 保留为 `--dry` 的旧别名（它原本就是「只打印、不写」）。
    ap = argparse.ArgumentParser(
        description="Lore 两族风味文本 → 数据件（🔴 必须显式选一个动词）")
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--write", action="store_true", help="重新生成并写盘")
    g.add_argument("--check", action="store_true",
                   help="只比对、不写盘：重算产物与盘上逐字节比（一致 rc=0 / 不一致 rc=1）")
    g.add_argument("--dry", action="store_true", help="只跑计算、打印读数，零写盘")
    g.add_argument("--report-only", action="store_true", help="（--dry 的旧别名）只打印覆盖情况，不写文件")
    args = ap.parse_args()

    src = load_json(SRC)
    terms = src["terms"]

    picked = collections.OrderedDict()
    for key in terms:
        if key.startswith(FAMILIES):
            picked[key] = terms[key]

    wl = {k[len("Warlord_Lore/"):]: v for k, v in picked.items() if k.startswith("Warlord_Lore/")}
    cl = {k[len("Cosmetic_Lore/"):]: v for k, v in picked.items() if k.startswith("Cosmetic_Lore/")}

    # ---- 覆盖普查（现算，⛔ 别把数字抄进别处）----
    cards = load_json(CARDS)["cards"]
    hit = collections.Counter()
    tot = collections.Counter()
    miss = collections.defaultdict(list)
    for c in cards:
        t = c.get("type") or "?"
        tot[t] += 1
        if c.get("id") in wl:
            hit[t] += 1
        else:
            miss[t].append(c.get("id"))
    print("== Warlord_Lore 覆盖（我们的卡池 %d 张）==" % len(cards))
    for t in sorted(tot):
        print("  %-8s %4d / %4d" % (t, hit[t], tot[t]))
    print("  合计      %4d / %4d   （缺 %d）" % (sum(hit.values()), len(cards), len(cards) - sum(hit.values())))
    for t in sorted(miss):
        print("  缺[%s] 例：%s" % (t, ", ".join(miss[t][:8])))
    print("  Warlord_Lore 表里多出（我们池子里没有的键）：%d" % (len(wl) - sum(hit.values())))

    backs = load_json(CARDBACKS)["items"]
    chit = sum(1 for b in backs if (b.get("uniqueId") or "") in cl)
    print("== Cosmetic_Lore 覆盖：卡背 %d 张里命中 %d（表里共 %d 条）==" % (len(backs), chit, len(cl)))
    missing_uid = [b.get("name") for b in backs if (b.get("uniqueId") or "") not in cl]
    print("  未命中样本：%s" % ", ".join(missing_uid[:8]))

    dry = args.dry or args.report_only

    # ---- 落盘 ①：全量台账（与官方表同形）----
    full = collections.OrderedDict()
    full["_note"] = NOTE
    full["_source"] = SRC
    full["_langs"] = ["en", "zh-CN"]
    full["_count"] = len(picked)
    full["terms"] = picked

    # ---- 落盘 ②：运行时件（entries 数组，JsonUtility 可读）----
    entries = [{"k": k, "en": v[0], "zh": v[1]} for k, v in picked.items()]
    res = collections.OrderedDict()
    res["version"] = 1
    res["note"] = NOTE
    res["source"] = SRC
    res["count"] = len(entries)
    res["entries"] = entries

    full_text = json_text(full)
    res_text = json_text(res)

    if dry:
        print("[dry] 未写盘；目标 = %s（%d 条）· %s（%d 条）"
              % (OUT_FULL, len(picked), OUT_RES, len(entries)))
        return 0

    if args.check:
        rc = 0
        for p, text in ((OUT_FULL, full_text), (OUT_RES, res_text)):
            old = io.open(p, "rb").read() if os.path.isfile(p) else b""
            same = old == text.encode("utf-8")
            print("[check] %s 与盘上逐字节一致 = %s（%d 字节）" % (p, same, len(text.encode("utf-8"))))
            if not same:
                rc = 1
        return rc

    with io.open(OUT_FULL, "w", encoding="utf-8", newline="\n") as f:
        f.write(full_text)
    print("written: %s (%d 条)" % (OUT_FULL, len(picked)))

    with io.open(OUT_RES, "w", encoding="utf-8", newline="\n") as f:
        f.write(res_text)
    print("written: %s (%d 条)" % (OUT_RES, len(entries)))

    meta = OUT_RES + ".meta"
    if not os.path.isfile(meta):
        guid = os.urandom(16).hex()
        with io.open(meta, "w", encoding="utf-8", newline="\n") as f:
            f.write("fileFormatVersion: 2\n")
            f.write("guid: %s\n" % guid)
            f.write("TextScriptImporter:\n")
            f.write("  externalObjects: {}\n")
            f.write("  userData: \n")
            f.write("  assetBundleName: \n")
            f.write("  assetBundleVariant: \n")
        print("written: %s (guid %s)" % (meta, guid))
    return 0


if __name__ == "__main__":
    sys.exit(main())
