#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_choice_option_cards.py — **选项 SO 旁表**（`Resources/choice_option_cards.json`）
执行写手 `B116_W32` · 2026-11-02 · 活状态表 `B-329`

================================================================================
为什么要有这张表（判据，⛔ 不是我们挑的）
================================================================================
`choose one:` / `Choose an effect …` 这一类卡（现读 21 张）在卡 SO 上带一张
`choicesDefinition.filterSpecificCards`（`TargetCriteria +0x70`，类型
`List<RawCardScript>`，`d:/2/tools/il2cpp_out/dump.cs:23870`）—— **直接指向选项卡 SO**。

原版怎么取（现读 `d:/2/tools/decomp_full/`）：
  · `BattleManager__GetChoiceFullPool.c:36-55`：`targetsAffected == 0xb4(180)` 且
    `filterSpecificCards` **非空** ⇒ 候选池**就是那张表**（不再查任何 collection）；
    只有那张表为空时才回落 `PlayerDataManager.get_fullCardCollection`（`:57` 起）。
  · `BattleManager__GetChoiceOptions.c:36-70`：从候选池随机抽 `min(池, maxTargets)` 张，
    **传的是 `uniqueId` 字符串**（`GetChoiceFullPool` 尾部 `Where(uniqueId != null)` + `Select(uniqueId)`，
    见 `BattleManager.__c___GetChoiceFullPool_b__488_0.c` / `_1.c`）。
  · `AbilityLogic__PlayAbility.c:2032-2040`（`chosenAbility 360 playChosenCardAbility`）：
    `RawCardScript.OnTriggerAlt(0 /* ThisCardPlayed */, …)` ⇒ **选项 SO 自己那张卡的
    `ThisCardPlayed` ability** 被跑，`thisCard` = 父卡所在的那个单位（`AbilityData+0x10`）。
  ⇒ **选项 SO 不是「可打出的卡」**：它没有正文（`desc` 空）、`cardType 20`、
    `inventoryOptions` 不是可收藏档，且原版**用 PPtr 直接点名**、不经可玩池。
  ⇒ 所以本表是**第二张表**（与 `enchantment_templates.json` 同一条既有口径：
     ⛔ 不进 `cards_engine.json`、⛔ 不改 `count`、⛔ 不进任何分母）。

================================================================================
收录闸（唯一）
================================================================================
`cards_engine.json` 里**每一张卡的 `choicesSpecificCardIds`**，减去 **可玩池自身的 id**。
现读 = 21 张父卡 / 57 个选项 id（`cards_engine.json` 1,129 张里 0 命中）。
（另外 112 个选项 id **本来就在池内** ⇒ 走可玩池那条查路，⛔ 不重复落进本表。）

================================================================================
产物形状
================================================================================
与 `enchantment_templates.json` **逐键同形**（`templates[]`：`id/name/cardType/spellType/
abilities[{trigger,triggerValue,logic[{ca,pa,ta,fm}]}]/abilityBuffs[]`）——
这样 C# 侧的 DTO / 三个映射 helper **一条都不用另写**
（`CardDatabase.EnchantmentTemplateFileDto` 等，见 `Data/CardDatabase.cs:3566` 那段）。
额外多一列 `parents[]`（谁引用它；排查用，⛔ 不参与任何判据）。

数据源 = `Resources/cards_truth.json`（1,351 张 SO 真值，本仓现成的第二权威旁路）。

================================================================================
用法（🔴 不带显式动作 = 干跑，一个字节都不写）
================================================================================
    python -I -X utf8 d:/4/Unity/工具/gen_choice_option_cards.py            # 干跑（打印读数）
    … --check                                                              # 与盘上逐字节比
    … --write                                                              # 落盘
"""
import argparse
import json
import os
import sys

RES = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources"
ENGINE = os.path.join(RES, "cards_engine.json")
TRUTH = os.path.join(RES, "cards_truth.json")
DST = os.path.join(RES, "choice_option_cards.json")

# 与 `工具/gen_cards_engine.py` 的 `AB_BUFF_FIELDS` **同一份口径**（26 格；
# ⚠️ 不 import 那个文件 —— 它有几千行顶层常量/全局副作用，import 一次要跑整份生成器的顶层）。
AB_BUFF_FIELDS = (
    "trait", "traitValue", "newRace",
    "meleeAttackChange", "meleeAttackValueOptions",
    "rangeAttackChange", "rangeAttackValueOptions",
    "armourChange", "armourValueOptions",
    "healthChange", "healthValueOptions",
    "costChange", "costValueOptions",
    "randomValueChange",
    "permanentBuff", "doNotStack",
    "markOfChaosType", "daemonHostType", "mutationType",
    "extrinsic", "extrinsicVariable",
    "untilEndOfTurn", "untilPlayerTurnStart", "untilEnemyTurnStart",
    "untilFollowingPlayerTurnStart", "untilNextAttack",
)


def _int(v):
    """bool → 0/1；认不出的值 → 0（= SO 默认值，与 `ability_buffs_of` 同口径）。"""
    if isinstance(v, bool):
        return 1 if v else 0
    return v if isinstance(v, int) else 0


def _logic_row(lg):
    """`abilities[].logic[]` 一格 → `{ca,pa,ta,fm}`（-1 = 该键缺失/坏值，⛔ 不猜 0）。"""
    if not isinstance(lg, dict):
        return {"ca": -1, "pa": -1, "ta": -1, "fm": -1}
    tc = lg.get("targetCriteria")
    ta = tc.get("targetsAffected") if isinstance(tc, dict) else None
    return {
        "ca": _int(lg["chosenAbility"]) if "chosenAbility" in lg else -1,
        "pa": _int(lg["playersAffected"]) if "playersAffected" in lg else -1,
        "ta": _int(ta) if isinstance(ta, int) and not isinstance(ta, bool) else -1,
        "fm": _int(lg["furtherMultiplier"]) if "furtherMultiplier" in lg else -1,
    }


def _buff_rows(oid, name, trow, report):
    """一个选项 SO → `abilityBuffs` 行（形状 = 卡表 v40 那一列，逐字同形）。"""
    out = []
    for i, ab in enumerate(trow.get("abilities") or []):
        if not isinstance(ab, dict):
            report["bad"].append((oid, name, "abilities[%d] 不是对象" % i))
            continue
        for j, lg in enumerate(ab.get("logic") or []):
            if not isinstance(lg, dict):
                continue
            ce = lg.get("buff")
            if not isinstance(ce, dict):
                continue
            bt = ce.get("buffType")
            if isinstance(bt, bool) or not isinstance(bt, int):
                report["bad"].append((oid, name, "buffType 缺失/不是整数"))
                continue
            if bt == 0:
                report["skipped_zero"] += 1      # 原版 `AddEffect.c:964-966` default 支什么都不做
                continue
            row = {"ab": i, "lg": j, "at": "buffEffect", "buffType": bt}
            for f in AB_BUFF_FIELDS:
                row[f] = _int(ce.get(f))
            # `buffEffect.enchantment.m_PathID`（PPtr 原值；⛔ 本表不解成模板 id ——
            # 那一跳要 guid→id 的映射，本表用不到它（唯一带这格的那一行不可达）。
            en = ce.get("enchantment")
            row["enchantmentPathId"] = _int(en.get("m_PathID")) if isinstance(en, dict) else 0
            out.append(row)
            report["buff_rows"] += 1
            report["by_buff"][bt] = report["by_buff"].get(bt, 0) + 1
    return out


def build():
    with open(ENGINE, encoding="utf-8") as f:
        eng = json.load(f)
    with open(TRUTH, encoding="utf-8") as f:
        truth = json.load(f)

    pool_ids = set(c["id"] for c in eng["cards"])
    byid = {c["id"]: c for c in truth["cards"]}

    report = {"bad": [], "skipped_zero": 0, "buff_rows": 0, "by_buff": {},
              "no_truth": [], "missing_id": []}

    parents = {}          # optionId -> [parentId, …]
    for c in eng["cards"]:
        for oid in (c.get("choicesSpecificCardIds") or []):
            if not oid or oid in pool_ids:
                continue
            parents.setdefault(oid, []).append(c["id"])

    templates = []
    for oid in sorted(parents):
        trow = byid.get(oid)
        if trow is None:
            report["missing_id"].append(oid)      # 池内指向了 truth 里没有的 id（= 真值缺口）
            continue
        ab_rows = []
        for i, ab in enumerate(trow.get("abilities") or []):
            if not isinstance(ab, dict):
                continue
            tg = ab.get("trigger")
            if isinstance(tg, bool) or not isinstance(tg, int):
                report["bad"].append((oid, trow.get("name"), "trigger 坏值"))
                continue
            tv = ab.get("triggerValue")
            ab_rows.append({
                "trigger": tg,
                "triggerValue": tv if isinstance(tv, int) and not isinstance(tv, bool) else -1,
                "logic": [_logic_row(lg) for lg in (ab.get("logic") or [])],
            })
        templates.append({
            "id": oid,
            "name": trow.get("name") or "",
            "cardType": _int(trow.get("cardType")) if "cardType" in trow else -1,
            "spellType": _int(trow.get("spellType")) if "spellType" in trow else -1,
            "parents": sorted(set(parents[oid])),
            "abilities": ab_rows,
            "abilityBuffs": _buff_rows(oid, trow.get("name"), trow, report),
        })

    out = {
        "version": 1,
        "count": len(templates),
        "source": "cards_engine.json 的 choicesSpecificCardIds − 可玩池；数据 = cards_truth.json",
        "note": ("选项 SO 旁表（`TargetCriteria.filterSpecificCards` 指向的卡）。"
                 "⛔ 不进 cards_engine.json / 不进 count / 不进任何分母；"
                 "形状与 enchantment_templates.json 逐键同形（复用同一套 DTO 与映射 helper）。"
                 "判据 → 资料/完全复刻/报告/B116_W32_B329选项SO进池.md"),
        "templates": templates,
    }
    return out, report, parents


def dumps(obj):
    # 与 `enchantment_templates.json` 同一种排版口径：紧凑、UTF-8、键序固定、末尾换行。
    return json.dumps(obj, ensure_ascii=False, separators=(",", ":")) + "\n"


def main():
    ap = argparse.ArgumentParser(add_help=True)
    ap.add_argument("--write", action="store_true", help="落盘")
    ap.add_argument("--check", action="store_true", help="与盘上逐字节比")
    args = ap.parse_args()
    if args.write and args.check:
        print("!! --write 与 --check 不能同时给")
        return 2

    out, report, parents = build()
    text = dumps(out)
    n_parents = len(set(p for v in parents.values() for p in v))
    print("=== gen_choice_option_cards ===")
    print("父卡（池内、带 choicesSpecificCardIds）引用到的池外选项 id = %d 个 / 来自 %d 张父卡"
          % (out["count"], n_parents))
    print("abilities 行 = %d · abilityBuffs 行 = %d · 跳过 buffType 0 = %d"
          % (sum(len(t["abilities"]) for t in out["templates"]), report["buff_rows"], report["skipped_zero"]))
    print("buffType 分布 = %s" % dict(sorted(report["by_buff"].items())))
    print("truth 里查不到的选项 id = %d %s" % (len(report["missing_id"]), report["missing_id"][:5]))
    print("坏行 = %d %s" % (len(report["bad"]), report["bad"][:5]))
    print("产物字节 = %d" % len(text.encode("utf-8")))

    if args.write:
        with open(DST, "wb") as f:
            f.write(text.encode("utf-8"))
        print("wrote %s" % DST)
        return 0
    if args.check:
        if not os.path.exists(DST):
            print("!! 盘上没有 %s" % DST)
            return 1
        with open(DST, "rb") as f:
            disk = f.read()
        if disk == text.encode("utf-8"):
            print("--check: 逐字节相同，无需重生成")
            return 0
        print("--check: **不一致** —— 需要 `--write` 重生成")
        return 1
    print("（干跑：⛔ 一个字节都没写。要落盘加 `--write`）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
