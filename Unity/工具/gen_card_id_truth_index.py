# -*- coding: utf-8 -*-
"""`card_id_truth_index.json` 生成器 —— 给「我们卡池 ↔ 原版真值卡 id」做 join 的名字索引。

**为什么要它**（台账 `A1782` / `A1778` / `A1783` / `A1800`）：
我们池里有一批卡拿不到原版 id —— 原因是 id 的判据是「按卡名查 `card_ids.json`」，而
  ① 我们池里相当一部分卡名是**美术/VO 资产名**（Godot 期 `d:/warpforge/data/cards.json` 带过来的），
     与原版 `cardName` 不一致（`Commissar Elan` ↔ `Commissar Denkler` · `Veteran Flyboy` ↔ `Veteran Stormboy`）；
  ② 原版 259 张卡的 `uniqueId` 是 **32 位 GUID**，`card_ids.json` 里一条都没有
     （`gen_cards_engine.pick_id` 只认「前缀 + 纯数字」）；
  ③ 同名组里「基准卡」与「附魔/教程副本」同名，按名取会把副本当成本体。
⇒ 这三件事**都只有真值侧的数据能判**，所以在这里**一次性算成索引**，由
`gen_cards_engine.py` 在分 id 时查 —— 判据原文 → `资料/普查产出_第三十会话/乙档六笔_判据查证.md` §`A1800`。

**级联 join 规则**（照 `乙档六笔_判据查证.md` §A1800 那张表，⛔ 别自己发明）：
  ① 官方本地化 `Card_Name/*` 的英文值 → 它的键（键 = 真值 `uniqueId`）
  ② 真值 `m_Name` 尾巴
  ③ 真值 `cardName`
取名字时**先用我们池里的 `name`、查不到再退 `card_stats.json` 的 `ocrName`**（在消费侧做）。

**「基准卡」判据**（本文件算出、`gen_cards_engine.py` 用它消歧）：
  `m_Name` 归一化后**以 `cardName` 归一化结尾** ⇒ 基准卡。
  实测：1,194 条真值里，88 个「同名多行」组中 **83 组恰好 1 个基准**；5 个例外都**没有池内卡**会命中
  （见本文件 `counts['base_exceptions']`）。

来源（都是本地只读档案，与 `gen_cards_engine.py` 同一份）：
  · 真值 = `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`
    （原版 `allcards` 包的 1,195 个 `RawCardScript`；Unity 2022.3.38f1 / 缓存 2025-07-30 老快照）
  · 官方本地化 = `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`（`Card_Name/*` 那一族）

用法：python -I -X utf8 d:/4/Unity/工具/gen_card_id_truth_index.py
      python -I -X utf8 d:/4/Unity/工具/gen_card_id_truth_index.py --dry   # 只打印统计、不落盘
"""
import argparse
import io
import json
import os
import re
import sys
import collections

TRUTH = "d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json"
OFFICIAL = "d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json"
OUT = "d:/4/Unity/数据/游戏数据/card_id_truth_index.json"


def norm(s):
    """卡名归一化：去掉大小写、空格、标点、连字符（与 `gen_cards_engine._norm_name` 同口径）。"""
    return re.sub(r'[^a-z0-9]', '', (s or '').lower())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry", action="store_true", help="只打印统计，不落盘")
    args = ap.parse_args()

    for p in (TRUTH, OFFICIAL):
        if not os.path.exists(p):
            sys.exit("❌ 找不到判据文件：%s" % p)
    truth = json.load(io.open(TRUTH, 'r', encoding='utf-8'))
    off = json.load(io.open(OFFICIAL, 'r', encoding='utf-8'))['terms']

    rows = []
    for t in truth:
        u = t.get('uniqueId') or ''
        if not u:
            continue
        rows.append(t)
    if len(rows) != len(set(t.get('uniqueId') for t in rows)):
        sys.exit("❌ 真值里有重复 uniqueId —— 索引会静默丢行，先查")

    uid2row = {t['uniqueId']: t for t in rows}

    def mname(t):
        return t.get('m_Name') or ''

    def is_base(t):
        """基准卡 = `m_Name` 归一化后以 `cardName` 归一化结尾（且 cardName 非空）。"""
        cn = norm(t.get('cardName'))
        return bool(cn) and norm(mname(t)).endswith(cn)

    by_official_en = collections.defaultdict(list)
    n_off_in, n_off_out = 0, 0
    for k, v in off.items():
        if not k.startswith('Card_Name/'):
            continue
        key = k.split('/', 1)[1]
        if key not in uid2row:
            n_off_out += 1          # 官方表比真值快照新 ⇒ 键不在快照里，**不收**（收了会指向不存在的行）
            continue
        n_off_in += 1
        by_official_en[norm(v[0])].append(key)

    by_name = collections.defaultdict(list)
    by_tail = collections.defaultdict(list)
    for t in rows:
        by_name[norm(t.get('cardName'))].append(t['uniqueId'])
        mn = mname(t)
        if mn:
            # 尾巴 = 最后一个 `-` 或 `_` 之后那段（两种分隔符真值里都用：`Z-DA-Grim Effigy` / `Goff_war_GrukkFace-Rippa`）
            by_tail[norm(re.split(r'[-_]', mn)[-1])].append(t['uniqueId'])

    base = sorted(t['uniqueId'] for t in rows if is_base(t))
    base_set = set(base)
    army = {t['uniqueId']: t.get('cardArmy') for t in rows}

    # 统计（写进 note，便于现核自检）
    groups = collections.defaultdict(list)
    for t in rows:
        groups[norm(t.get('cardName'))].append(t)
    multi = [v for v in groups.values() if len(v) > 1]
    exc = [(k, len(v), sum(1 for t in v if is_base(t)))
           for k, v in groups.items() if len(v) > 1 and sum(1 for t in v if is_base(t)) != 1]
    stats = {
        "truth_rows": len(rows),
        "official_keys": len(by_official_en),
        "official_keys_in_truth": n_off_in,
        "official_keys_not_in_truth": n_off_out,
        "base_rows": len(base),
        "multi_name_groups": len(multi),
        "base_exceptions": ["%s(rows=%d,bases=%d)" % e for e in sorted(exc)],
    }

    note = (
        "【我们卡池 ↔ 原版真值卡 id 的 join 索引】2026-10-14（第三十一会话 · P1 id 置换批）生成。"
        "生成器 = `工具/gen_card_id_truth_index.py`，消费者 = `工具/gen_cards_engine.py`。"
        "真值 = `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（原版 `allcards` 包 1,195 个 `RawCardScript`，"
        "Unity 2022.3.38f1 / 缓存 2025-07-30 老快照）+ 官方本地化 `Card_Name/*`（`Unity/数据/游戏数据/官方本地化_en+zh-CN.json`）。"
        "级联 join = ①官方英文名 → 键（键 = 真值 `uniqueId`）②真值 `m_Name` 尾巴 ③真值 `cardName`；"
        "取名字时先我方 `name`、再退 `card_stats.json` 的 `ocrName`（消费侧做）。"
        "`base` = **基准卡**（`m_Name` 归一化以 `cardName` 归一化结尾）—— 同名组里只有它允许被池内卡取用，"
        "这是为了不把「附魔/教程副本」当成本体（例：`Uge Choppa` 有本体 + `_enchantment` 副本两条）。"
        "`army` = 真值 `cardArmy`（与 `card_stats.json` 的 `factionId` 同口径）—— 消费侧按它过滤阵营。"
        "判据原文 → `Unity/资料/普查产出_第三十会话/乙档六笔_判据查证.md` §`A1800`；"
        "涉及账 `A1782` / `A1778` / `A1783` / `A1800`。"
        "⚠️ 年代限制：快照里没有 2025-07-30 之后新增的内容（SpaceWolves / Ultramarines 等）⇒ 那些卡查不到，属预期。"
    )

    out = {
        "note": note,
        "source": {"truth": TRUTH, "official": OFFICIAL},
        "counts": stats,
        "army": army,
        "base": base,
        "by_official_en": {k: v for k, v in sorted(by_official_en.items())},
        "by_tail": {k: v for k, v in sorted(by_tail.items())},
        "by_name": {k: v for k, v in sorted(by_name.items())},
    }

    print("真值行 %d · 官方键命中真值 %d / 不在真值 %d" % (stats['truth_rows'], n_off_in, n_off_out))
    print("基准卡 %d · 同名多行组 %d · 基准数 != 1 的组 %d" % (stats['base_rows'], stats['multi_name_groups'], len(exc)))
    for e in stats['base_exceptions']:
        print("   ⚠️ 例外：", e)
    print("by_official_en %d · by_tail %d · by_name %d" % (len(by_official_en), len(by_tail), len(by_name)))
    if args.dry:
        print("（--dry：不落盘）")
        return
    # 先算进变量、确认没错再写（铁律：`io.open(p,'wb').write(<表达式>)` 抛异常会把文件截成 0 字节）
    text = json.dumps(out, ensure_ascii=False, indent=1)
    io.open(OUT, 'w', encoding='utf-8', newline='\n').write(text)
    print("→ %s（%d 字节）" % (OUT, len(text.encode('utf-8'))))


if __name__ == '__main__':
    main()
