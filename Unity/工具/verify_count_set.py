#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""独立校验：`cards_engine.json` 的 `unitConditions[role=count].countSet` ↔ `cards_raw.json`。

> 🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16` 的明确缺口 · v16）。
> **本脚本【不 import】`工具/gen_cards_engine.py` 的任何东西** —— 它存在的意义就是
> 「另一个人按原版字段表再数一遍」：生成器那边写错一格，这里必须报出来。
> 字段清单**从 `cards_raw.json` 自己推**（`countCriteria` 的键），不抄生成器的 `COUNT_SET_*`。

口径（判据 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{BoardCriteria,TargetCriteria}.cs`
＋ `d:/2/tools/decomp_full/FilterMethods__CheckIfMeetsCriteria.c`）：
  · 被数集合 = `BoardCriteria.countCriteria`（`TargetCriteria`）的**全部字段**
    ＋ `BoardCriteria.playerTurn`；
  · `conditionsFilter` / `excludeCondition` **不算集合字段** —— 它们在行上是 `cond` / `ex`；
  · `filterSpecificCards` / `excludeEnchantments` 是 `List<RawCardScript>` ⇒ 比对的是
    `m_PathID → uniqueId` 解出来的 id 列表；
  · 布尔在 JSON 里是 0/1 整数（`True/False` 归一成 1/0）；缺键/坏值的哨兵 = `-1`。

用法：`python -I -X utf8 工具/verify_count_set.py [--raw <path>] [--engine <path>]`
退出码：0 = 差异 0；1 = 有差异（逐条打印）；2 = 结构问题（表版本 / 行形状）。
"""
import argparse
import json
import os
import sys

RAW_DEFAULT = r"d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"
ENGINE_DEFAULT = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"

# 这两个格子**不在** `countSet` 里（它们已经是这一行的 `cond` / `ex`）。
NON_SET_CR_FIELDS = ("conditionsFilter", "excludeCondition")
# 引用列表（`List<RawCardScript>`）—— 比的是解引用后的 id 列表。
REF_FIELDS = ("filterSpecificCards", "excludeEnchantments")
# `count` 行的 `lg` 恒为 -1（`countCriteria` 直接挂在 `boardCriteria` 上）。
COUNT_LG = -1


def load_json(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def raw_count_rows(card):
    """一张真值卡 → `[(ab, boardCriteria, countCriteria), …]`（**只数 `conditionsFilter != 0` 的**）。

    位置与生成器同一套原版位置编码（`ab`：`cardAbilities` 下标 / `-1` active / `-2` alternative）——
    但**代码是另写的**（不 import 生成器）。`count` 只可能挂在 `boardCriteria.countCriteria`。
    """
    out = []

    def scan(ab, obj):
        if not isinstance(obj, dict):
            return
        board = obj.get("boardCriteria")
        if not isinstance(board, dict):
            return
        cr = board.get("countCriteria")
        if not isinstance(cr, dict):
            return
        v = cr.get("conditionsFilter", 0)
        if isinstance(v, bool) or not isinstance(v, int) or v == 0:
            return
        out.append((ab, board, cr))

    for i, ab in enumerate(card.get("cardAbilities") or []):
        scan(i, ab)
    scan(-1, card.get("activeAbility"))
    scan(-2, card.get("alternativeAbility"))
    return out


def norm_bool(v):
    return 1 if v is True else 0 if v is False else v


def resolve_refs(lst, path_to_uid):
    """`[{"m_PathID":…}, …]` → `[uniqueId, …]`；解不出的**留一个哨兵** `"<unresolved:pid>"`（一定会报差异）。"""
    if not isinstance(lst, list):
        return lst
    out = []
    for ref in lst:
        pid = ref.get("m_PathID") if isinstance(ref, dict) else None
        uid = path_to_uid.get(pid)
        out.append(uid if uid is not None else "<unresolved:%r>" % (pid,))
    return out


def expected_set(board, cr, path_to_uid):
    """真值 → `{字段: 期望值}`（含 `playerTurn`；不含 `conditionsFilter`/`excludeCondition`）。"""
    exp = {}
    for k, v in cr.items():
        if k in NON_SET_CR_FIELDS:
            continue
        if k in REF_FIELDS:
            exp[k] = resolve_refs(v, path_to_uid)
        else:
            exp[k] = norm_bool(v)
    # `BoardCriteria.playerTurn` 是被数集合的一部分（谁的场面）。
    exp["playerTurn"] = norm_bool(board.get("playerTurn"))
    return exp


def actual_set(row):
    cs = row.get("countSet")
    return cs if isinstance(cs, dict) else None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--raw", default=RAW_DEFAULT)
    ap.add_argument("--engine", default=ENGINE_DEFAULT)
    a = ap.parse_args()

    raw = load_json(a.raw)
    eng = load_json(a.engine)

    raw_cards = raw["cards"] if isinstance(raw, dict) else raw
    raw_by_id = {}
    path_to_uid = {}
    for c in raw_cards:
        uid = c.get("uniqueId")
        if uid:
            raw_by_id[uid] = c
        pid = c.get("path_id")
        if pid is not None and uid:
            path_to_uid[pid] = uid

    version = eng.get("version")
    cards = eng.get("cards") or []
    print(f"卡池 version = {version} · 卡 {len(cards)} 张 · 真值 {len(raw_cards)} 张")
    if version != 16:
        print(f"❌ 卡池 version = {version!r}，本校验针对 **16**（v16 才落 `countSet`）")
        return 2

    diffs = []
    n_cards_with_rows = 0
    n_rows = 0
    n_set_rows = 0
    n_no_truth_rows = 0
    n_raw_not_in_pool = 0
    pool_ids = {c.get("id") for c in cards}

    for rc in raw_cards:
        uid = rc.get("uniqueId")
        if uid and uid not in pool_ids and raw_count_rows(rc):
            n_raw_not_in_pool += 1

    for card in cards:
        cid = card.get("id")
        ut = card.get("name")
        rows = [r for r in (card.get("unitConditions") or []) if r.get("role") == "count"]
        rc = raw_by_id.get(cid)
        if rc is None:
            if rows:
                n_no_truth_rows += len(rows)
                diffs.append((cid, ut, "真值缺失", f"引擎有 {len(rows)} 条 count 行，但真值快照里没有这张卡"))
            continue
        exp_rows = raw_count_rows(rc)
        if not rows and not exp_rows:
            continue
        n_cards_with_rows += 1
        # 按 `ab` 配对（`count` 的 `lg` 恒 -1；同一 `ab` 下原版只能有一条 countCriteria）。
        eng_by_ab = {}
        for r in rows:
            eng_by_ab.setdefault(r.get("ab"), r)
        exp_by_ab = {b[0]: b for b in exp_rows}
        for ab in sorted(set(eng_by_ab) | set(exp_by_ab), key=lambda x: (x is None, x)):
            er = eng_by_ab.get(ab)
            xr = exp_by_ab.get(ab)
            if er is None:
                diffs.append((cid, ut, f"ab={ab}", "真值有 count 行，引擎没落"))
                continue
            if xr is None:
                diffs.append((cid, ut, f"ab={ab}", "引擎有 count 行，真值里没有"))
                continue
            n_rows += 1
            _, board, cr = xr
            if er.get("lg") != COUNT_LG:
                diffs.append((cid, ut, f"ab={ab}.lg", f"引擎 {er.get('lg')!r} ≠ {COUNT_LG}"))
            # 三参数（v15 起的既有列，顺带复核 —— 它们和 countSet 同源、同一次比对里看更省事）。
            for col, key in (("opt", "countOptions"), ("op", "triggerIf"), ("refValue", "referenceValue")):
                want = norm_bool(board.get(key))
                got = er.get(col, "<missing>")
                if got != want:
                    diffs.append((cid, ut, f"ab={ab}.{col}", f"引擎 {got!r} ≠ 真值 {want!r}（board.{key}）"))
            # ---- 被数集合 ----
            act = actual_set(er)
            if act is None:
                diffs.append((cid, ut, f"ab={ab}.countSet", "引擎没有 `countSet`（应为对象）"))
                continue
            n_set_rows += 1
            exp = expected_set(board, cr, path_to_uid)
            for k in sorted(set(exp) | set(act)):
                if k not in exp:
                    diffs.append((cid, ut, f"ab={ab}.countSet.{k}", f"引擎多出这一格（真值没有）：{act[k]!r}"))
                elif k not in act:
                    diffs.append((cid, ut, f"ab={ab}.countSet.{k}", f"引擎缺这一格（真值 {exp[k]!r}）"))
                elif norm_bool(act[k]) != norm_bool(exp[k]):
                    diffs.append((cid, ut, f"ab={ab}.countSet.{k}", f"引擎 {act[k]!r} ≠ 真值 {exp[k]!r}"))

    print(f"池内带 count 行的卡 = {n_cards_with_rows} 张 · 比对条 = {n_rows} 条"
          f"（其中带 countSet 的 = {n_set_rows} 条）")
    if n_no_truth_rows:
        print(f"⚠️ 真值快照里没有、但引擎落过 count 行的：{n_no_truth_rows} 条（上面逐条列了）")
    if n_raw_not_in_pool:
        print(f"ℹ️ 真值里有 count 行、但不在我们卡池的卡：{n_raw_not_in_pool} 张（不参与比对 —— 卡池之外）")
    if "gen_cards_engine" in sys.modules:
        print("❌ 独立性破了：本进程 import 过生成器 `gen_cards_engine`")
        return 2
    if diffs:
        print(f"\n❌ 差异 **{len(diffs)}** 处：")
        for d in diffs[:200]:
            print("  · " + " | ".join(str(x) for x in d))
        if len(diffs) > 200:
            print(f"  …（还有 {len(diffs) - 200} 处，省略）")
        return 1
    print("\n✅ 差异 0（被数集合逐格与 cards_raw.json 的 countCriteria 一致）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
