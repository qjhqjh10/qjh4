#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_ability_anim_durations.py — 账 `A1767`：把「发动效果的时长」从一条常数换成**逐卡真值**。

产物（同一次运行写两份）：
  ① `d:/4/Unity/数据/游戏数据/ability_anim_durations.json`          —— 全量真值（给工具/人看，含逐条 CardAnim）
  ② `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/ability_anim_durations.json`
                                                                    —— **运行时件**（`EventTiming` 读的就是它）

判据链（逐跳可复核，**不是**按卡名/关键词推）：
  原版卡 SO `activeAbility.activeAbilityLogic[*].cardAnimations[*]`（`AssetReference`，存 GUID）
    → `数据/游戏数据/card_vfx_by_card.json` 的 `byCard[<我方 id>].ability[]`（`conf=authoritative`，
      `abilitySlot` 以 `activeAbility` 开头）—— 那份表已经走完了
      「GUID → 束 → 资产原名」那一跳（判据 → `资料/普查产出_第二十七会话/甲2_GUID到资源_调查.md`）
    → `d:/2/新解包资源/assets_full/bundle_<束>/MonoBehaviour/<CardAnim 名>.json` 的 `animInfo`
    → `AnimInfo.GetAnimDuration()` = `vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`

**为什么是 `activeAbility` 那一支**（原版方法体，不是猜的）：
  · `AbilityLogic__PlayAnim.c` 遍历**该 abilityLogic 自己的** `cardAnimations` 列表，逐条
    `BattleManager.AddCardAnim(...)` 排队（`d:/2/tools/decomp_full/`）；`PlayAnim` 的调用点是
    `AbilityLogic__PlayAbility.c:273`，而 `PlayAbility` 只被 `TriggerAbility` / `ResolveActiveAbility` /
    `ResolveAbilities` 调。
  · 每一拍各读**自己那份** `animInfo.waitAnimToApplyEffect`（`0x58`），然后由 `_ResolveAnim` 播：
    `BattleManager._ResolveAnim_d__466__MoveNext.c` 里先
    `WaitForCompletion(BuildSequence(tweenAnim))`（**仅当** `tweenAnim != null && waitAnimation != 0`）
    之后**一定**再 `WaitForSeconds(AnimInfo.GetAnimDuration())`。
    🔴 **2026-10-17 更正（铁律 5）**：本行原来抄的是「**排在末尾那一条**若 `waitAnimToApplyEffect != 0`」——
    **错**（那是把「目标列表的最后一拍」误当成了「步骤列表」）；判据 →
    `资料/普查产出_1017/BSC62/R4_waitForAnim语义.md`。求和不受影响。
  · 🔴 **2026-10-23（A11 · R18）**：`tweenAnim` 那一段**已经算了** ——
    `cards[*].*Anims[*].tweenAnim` 带 `name` / `waitAnimation` / `seconds`，
    卡片级在 `activeWithTween`。⚠️ **运行时没接**（`s` 仍是 `active`，不含 tween）。
  · `CardAbility.trigger` 那一族（`cardAbilities[*].abilityLogic[*]`，`ThisCardPlayed` / `Slay` / …）
    是**别的时机**播的（我们的 `EvtKind.Trigger` / `Play`），**不进这张表** —— 全量件里留在
    `cards[*].otherAbilityAnims` 里备查。

⚠️ **本表覆盖到当前 build 的卡 SO**：真值源 = `资料/普查产出_1018/allcards_1350/cards_raw.json`
   （**2026-10-18 换**；旧的是 2025-07-30 的 `cards_1195_2022时代.json` 快照，⛔ 别改回去）。
   仍有少量「会发 Ability 事件」的卡没有真值 —— 原因**不是快照年代**，而是它们的卡 SO
   `activeAbility` 本身**一条 CardAnim 都没有**（铁律 11 例外①）。逐张名单见 `uncovered`。

用法：
  python -I -X utf8 d:/4/Unity/工具/gen_ability_anim_durations.py            # 重出两份产物
  python -I -X utf8 d:/4/Unity/工具/gen_ability_anim_durations.py --check    # 只比不写（幂等自检）
"""

import io
import json
import os
import re
import sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n  python -I -X utf8 d:/4/Unity/工具/gen_ability_anim_durations.py            # 重出两份产物\n  python -I -X utf8 d:/4/Unity/工具/gen_ability_anim_durations.py --check    # 只比不写（幂等自检）\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

ROOT4 = "d:/4/Unity"
ROOT2 = "d:/2"

VFX_TABLE = ROOT4 + "/数据/游戏数据/card_vfx_by_card.json"
CARDS_ENGINE = ROOT4 + "/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
ASSETS_FULL = ROOT2 + "/新解包资源/assets_full"

# 🔴 **真值源 = 当前 build 的 allcards 卡 SO 语料**（2026-10-18 换）。
#   ⛔ 旧路径 `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json` 是 **2025-07-30 的旧快照**
#   （1,195 张，UM/SW 那几批不在里面）—— **别改回去**。
#   判据 → `资料/普查产出_1018/R_卡牌线_完整ID与SO后的调整评估.md` §4。
#   ⚠️ 它不是本表时长的来源（时长来自下面的 card_vfx_by_card → CardAnim dump 那条链）——
#      它只用来**如实判定**「这张卡在不在真值里」「SO 的 activeAbility 到底有没有 CardAnim」。
CARD_SO_TRUTH = ROOT4 + "/资料/普查产出_1018/allcards_1350/cards_raw.json"

# 🔴 2026-10-22（`W64 · B17`）：`alternativeAbility` 那一支的 GUID → CardAnim 解析索引。
#   原版 `RawCardScript.GetActiveAbility` = **二选一**：
#     `hasAlternativeAbility(+0x2aa) && BoardAnalysis.MeetsCriteria(+0x2b0)` ⇒ 返回**替代能力**(+0x2b8)，
#     否则返回普通 `activeAbility`(+0x260)（方法体 → `d:/2/tools/decomp_full/RawCardScript__GetActiveAbility.c`）。
#   `card_vfx_by_card.json` 的 `ability` 桶**只覆盖 `activeAbility`**（`abilitySlot` 以它开头），
#   所以替代那一支得单独解析一遍（现读全库只有 **1 张**卡真有这条：`SW21`）。
GUID_MAP_TSV = ROOT4 + "/资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv"

OUT_FULL = ROOT4 + "/数据/游戏数据/ability_anim_durations.json"
OUT_RT = ROOT4 + "/MyGame/Assets/CardPresentation/Resources/ability_anim_durations.json"

# 四字段之和的中位（旧常数）—— 生成器会把**本表的**中位算出来写进产物，供自检核对
FALLBACK = 1.0


def load(p):
    return json.load(io.open(p, encoding="utf-8"))


def anim_duration(ai):
    """`AnimInfo.GetAnimDuration`（`AnimInfo__GetAnimDuration.c`，偏移 0x24/0x2C/0x30/0x34 —— 逐项对应
    `vfxDelayTime` / `timeAtStartPos` / `timeMoving` / `timeAtEndPos`；**不含** `startDelay`）。"""
    return (float(ai.get("vfxDelayTime") or 0.0)
            + float(ai.get("timeAtStartPos") or 0.0)
            + float(ai.get("timeMoving") or 0.0)
            + float(ai.get("timeAtEndPos") or 0.0))


# =====================================================================================
#  R18（2026-10-23 · A11）：`AnimInfo.tweenAnim`（`UnitTweenSO`）那一段的**名字与长度**
# =====================================================================================
#  原版判据（`BattleManager._ResolveAnim_d__466__MoveNext.c` 的 `case 1`）：
#    `UnitTweenSO.BuildSequence(...)` → **仅当** `tweenAnim.waitAnimation(+0x18) != 0` 才
#    `WaitForCompletion(seq)`；播完**之后一定**再 `WaitForSeconds(AnimInfo.GetAnimDuration())`。
#  ⇒ 本表的 `cards[*].active`（= GetAnimDuration 之和）**不含** tween 那一段。带 `waitAnimation=1`
#    的那几张卡，原版真正等的 = `tween 序列长度 + animDuration`。**两段都出**（`activeWithTween`），
#    但 ⛔ **不替运行时决定用哪个** —— `Core/AbilityAnimDurations.cs` 读的是 `s`（= `active`）。
#
#  本件补的两个缺口：
#    ① **PathID → 名字**：`AnimInfo.tweenAnim` 只存 `{m_FileID, m_PathID}`；`UnitTweenSO` 全在
#       `bundle_tweenandshakes_assets_all`（74 个）。现成的 PathID 索引在
#       `数据/索引/anim_address_map.json` 的 `guid_to_asset`（按 `bundle` 过滤即可，**不用再解包**）。
#    ② **序列长度**：`Resources/UnitTweens.json` 只有按**名字**的键 ⇒ ① 补上就能算。
#
#  长度怎么算（判据 = 反编译 + DOTween 的数据结构）：
#    · 每一拍：`TweenInfoAnimationBase__GetTween.c` 对每条 tween 做
#      `SetDelay(delay@+0x3c)` + `SetLoops(loops@param_1[8], loopType@+0x44)`
#      ⇒ 一拍占的时长 = `delay + duration × (loops + 1)`（DOTween：`loops = -1` 才是无限，这里没有负数）。
#    · 排布：`UnitTweenSO__BuildSequence.c` 逐条 —— `muted != 0` 跳过（但记住「它本该是 Append 还是
#      Join」）；`appendType != 0` ⇒ `Join`，否则 `Append`；若上一条被跳过且本该是 Append ⇒ 这一条
#      **强制 Append**（原版 `bVar2 & bVar12` 那一对状态位）。
#    · `Append` 落在「序列当前末端」；`Join` 落在「上一次插入的起点」——
#      DOTween `Sequence.lastTweenInsertTime` 这个字段就是给 Join 用的
#      （`d:/2/Warpforge_code/Scripts/DOTween/DG/Tweening/Sequence.cs:13`）。
#      序列长度 = 所有拍 `起点 + delay + duration × (loops + 1)` 的最大值。
#    ⚠️ 本机**没有** DOTween 的 IL（`Scripts/DOTween/` 下是空桩），`Join` 的落点靠的是上面那个
#      字段名 + 与 `Core/UnitTweenTable.Build` 逐句对照。若 `Join` 其实是「落在当前末端」，
#      只有 `Tyranid_Deform` 一条会变（2.4 → 3.0）；另外两条 wait=1 的件（`Subtle Shake` /
#      `Mutation`）**全是 Append**，算出来的值与 Join 语义无关。
TWEEN_PATHID_INDEX = ROOT4 + "/数据/索引/anim_address_map.json"
UNIT_TWEENS_JSON = ROOT4 + "/MyGame/Assets/CardPresentation/Resources/UnitTweens.json"
TWEEN_BUNDLE = "bundle_tweenandshakes_assets_all"


def load_tween_names():
    """`{PathID: UnitTweenSO 名}` —— 只取 `bundle_tweenandshakes_assets_all` 里的 MonoBehaviour。

    同一 PathID 在不同 CAB 里可以是不同对象，所以**必须按束过滤**（`anim_address_map.json`
    的每条都带 `bundle` 字段）。"""
    names = {}
    if not os.path.isfile(TWEEN_PATHID_INDEX):
        print("⚠️ 读不到 %s ⇒ `tweenAnim` 的 PathID→名字 **判不了**（产物里如实标 resolved=false）"
              % TWEEN_PATHID_INDEX)
        return names
    for v in (load(TWEEN_PATHID_INDEX).get("guid_to_asset") or {}).values():
        if (v or {}).get("bundle") != TWEEN_BUNDLE or v.get("type") != "MonoBehaviour":
            continue
        pid, nm = v.get("pathId"), v.get("name")
        if pid is None or not nm:
            continue
        names[int(pid)] = nm
    return names


def load_unit_tweens():
    """`{名字: UnitTweens 条目}` —— 长度算式用的就是它（`工具/gen_unit_tweens.py` 的产物）。"""
    d = load(UNIT_TWEENS_JSON) if os.path.isfile(UNIT_TWEENS_JSON) else {}
    return {it.get("name"): it for it in (d.get("items") or []) if it.get("name")}


def tween_seconds(entry):
    """`UnitTweenSO` 建出来的 DOTween `Sequence` 总长（秒）。算式见上面那段注释。"""
    pos = 0.0          # 序列当前末端
    last_start = 0.0   # 上一次插入的起点（DOTween `lastTweenInsertTime`；Join 就落在这里）
    skipped = False
    skipped_was_append = False
    for s in (entry or {}).get("tweens") or []:
        if int(s.get("muted") or 0) != 0:
            if not skipped:
                skipped = True
                skipped_was_append = (int(s.get("appendType") or 0) == 0)
            continue
        is_join = (not (skipped and skipped_was_append)) and int(s.get("appendType") or 0) != 0
        skipped = False
        dur = float(s.get("duration") or 0.0)
        delay = float(s.get("delay") or 0.0)
        loops = int(s.get("loops") or 0)
        total = dur * (loops + 1) if loops >= 0 else float("inf")
        start = last_start if is_join else pos
        pos = max(pos, start + delay + total)
        last_start = start
    return round(pos, 4)


def tween_row(pptr, tween_names, unit_tweens):
    """`AnimInfo.tweenAnim` 的 PPtr → 产物里那一格（**PathID → 名字 → 长度** 三跳都落盘）。

    没有引用时返回 `None`（= 原版这一拍根本不等 tween）。"""
    pid = pptr.get("m_PathID")
    if not pid:
        return None
    name = tween_names.get(int(pid))
    entry = unit_tweens.get(name) if name else None
    if entry is None:
        return {"m_PathID": pid, "m_FileID": pptr.get("m_FileID"), "name": name,
                "waitAnimation": None, "seconds": None, "resolved": False,
                "why": ("PathID 在 `anim_address_map.json` 的 tween 束里查不到"
                        if name is None else
                        "名字 `%s` 在 `Resources/UnitTweens.json` 里没有" % name)}
    return {"m_PathID": pid, "m_FileID": pptr.get("m_FileID"), "name": name,
            "waitAnimation": int(entry.get("wait") or 0),
            "seconds": tween_seconds(entry), "steps": len(entry.get("tweens") or []),
            "resolved": True}


def waited_tween_seconds(row):
    """这一拍**真的会等**的 tween 秒数 —— 原版只在 `tweenAnim.waitAnimation != 0` 时 `WaitForCompletion`。"""
    t = (row or {}).get("tweenAnim")
    if not t or not t.get("waitAnimation"):
        return 0.0
    return float(t.get("seconds") or 0.0)


# =====================================================================================
#  R8（2026-10-23 · A11）：`normalAbilityBoardCriteria` 的语义（**第一权威 = 反编译方法体**）
# =====================================================================================
#  调用点：`RawCardScript__GetActiveAbility.c`
#    `if (hasAlternativeAbility(+0x2AA)) { if (!MeetsCriteria(..., *(this+0x2B0), ...)) return *(this+0x2B8); }`
#    ⇒ **满足 criteria 走【普通】`activeAbility`(+0x260)，不满足才落到替代能力**(+0x2B8)。
#  `BoardAnalysis__MeetsCriteria.c`（155 行，逐句读过）的完整语义：
#    · `criteria == null` ⇒ true；`criteria.countCriteria == null` ⇒ true
#    · `playerTurn`(`PlayersAffected` @0x10)：`Self(10)` ⇒ 要求 `IsPlayerTurn() == card.isPlayer(+0x40)`；
#      `Enemy(20)` ⇒ 要求不等；`Both(0)` ⇒ 不检查
#    · `triggerIf == RandomChance(8)` 且 `referenceValue > 0` 且 `param_6 == 0` ⇒
#      **先**掷 `RandomInt(0,100)`，返回 `rand < referenceValue`（这一支**不看目标数**）
#    · `countCriteria.targetsAffected`(@+0x10) == 0 ⇒ true（「Any 忽略其它过滤」）
#    · 否则 `targets = AbilityLogic.GetTargets(criteria=countCriteria, …)`，`n = targets.Count`；
#      `countOptions == EnemiesMinusAllies(10)` ⇒ `n = Σ(+1 敌方 / -1 我方)`
#    · 最后按 `triggerIf`(`StatFilterOptions` @0x24)：`EqualTo(1)` n==ref · `EqualOrHigher(2)` n>=ref ·
#      `EqualOrLower(3)` n<=ref；**其余值**（含 `NotApplicable(0)`、`*Alt(9/10/11)`、
#      `EqualOrLowerToThisUnitAttack(4)` 那一族）⇒ `LogError` 后**返回 true**
#    ⚠️ 枚举值出处：`PlayersAffected.cs` / `CountOptions.cs` / `StatFilterOptions.cs`（`d:/2/Warpforge_code/`）。
#    ⚠️ `param_6` 是第 6 个实参（我们读不出它的语义）⇒ 只有 `RandomChance` 那一支受影响，如实标。
#  数据侧现状：`cards_raw.json` 的 `cards[*].full_raw.normalAbilityBoardCriteria` **有**（现读
#    **1 张**卡带它：`SW21`），但 `cards_engine.json`（→ RuleEngine 的卡数据）**没有这个字段**
#    ⇒ 引擎侧要接得先 `gen_cards_engine.py` 把它带出来（那是卡牌线 A1 的文件，本件不动）。
PLAYERS_AFFECTED = {0: "Both", 10: "Self", 20: "Enemy"}
COUNT_OPTIONS = {0: "AllTargets", 10: "EnemiesMinusAllies"}
STAT_FILTER = {0: "NotApplicable", 1: "EqualTo", 2: "EqualOrHigher", 3: "EqualOrLower",
               4: "EqualOrLowerToThisUnitAttack", 5: "HigherToThisUnitAttack",
               6: "EqualOrLowerIncludingBastion", 7: "LowerToThisUnitAttack",
               8: "RandomChance", 9: "EqualToAlt", 10: "EqualOrHigherAlt", 11: "EqualOrLowerAlt"}
# `TargetsAffected.cs` 里 `countCriteria.targetsAffected` 用得到的那几个（0 之外的枚举全表在同一个文件）
TARGETS_AFFECTED = {0: "any", 10: "allFiltered", 20: "self", 30: "target", 40: "attacker",
                    50: "inHandFiltered", 55: "inPlayOrDyingFiltered", 60: "inCemeteryFiltered",
                    70: "abilityOwner", 80: "actingCard", 120: "friendlyHero", 130: "enemyHero"}
# 过滤位（`TargetCriteria` 的 bool 字段）—— 只列**为真**的那些，写进 summary
TARGET_FILTER_BITS = ("friendlyUnits", "enemyUnits", "includeHero", "includeMinions", "includeSpells",
                      "includeWhispers", "includeWeapons", "includeRemnants", "includeHeroParts",
                      "includeSelf", "excludeNotSelf", "excludeSimilarCards", "excludeTargetted",
                      "excludeFriendlyHero", "excludeEnemyHero")


def decode_board_criteria(c):
    """把 `normalAbilityBoardCriteria` 的**数字枚举**翻成人能读的名字 + 一句执行摘要。

    ⛔ 不做「这张卡在什么局面下走替代」的判定（那要棋盘）—— 只把判据原样翻明白。"""
    if not isinstance(c, dict):
        return None
    pt = int(c.get("playerTurn") or 0)
    co = int(c.get("countOptions") or 0)
    tf = int(c.get("triggerIf") or 0)
    ref = int(c.get("referenceValue") or 0)
    cc = c.get("countCriteria") or {}
    ta = int(cc.get("targetsAffected") or 0) if isinstance(cc, dict) else 0
    bits = [k for k in TARGET_FILTER_BITS if int(cc.get(k) or 0) != 0] if isinstance(cc, dict) else []
    parts = []
    if pt == 10:
        parts.append("必须轮到这张卡的主人行动")
    elif pt == 20:
        parts.append("必须是对方的回合")
    if ta == 0:
        parts.append("目标数不参与判断（`targetsAffected = any(0)` ⇒ 直接满足）")
    else:
        cnt = "目标数" if co != 10 else "（敌方目标数 − 我方目标数）"
        op = {1: "==", 2: ">=", 3: "<=", 8: "< 随机 0..99"}.get(tf)
        parts.append("%s %s %d" % (cnt, op, ref) if op else "未知比较方式(%d)" % tf)
        parts.append("目标集 = `%s`%s" % (TARGETS_AFFECTED.get(ta, "?(%d)" % ta),
                                         ("，过滤位 " + "/".join(bits)) if bits else ""))
    if tf == 8 and ref > 0:
        parts.append("（随机分支：`RandomInt(0,100) < %d`，不看目标数）" % ref)
    return {
        "playerTurn": {"value": pt, "name": PLAYERS_AFFECTED.get(pt, "?")},
        "countOptions": {"value": co, "name": COUNT_OPTIONS.get(co, "?")},
        "triggerIf": {"value": tf, "name": STAT_FILTER.get(tf, "?")},
        "referenceValue": ref,
        "countCriteriaTargetsAffected": {"value": ta, "name": TARGETS_AFFECTED.get(ta, "?")},
        "countCriteriaFilters": bits,
        "summary": "；".join(parts) if parts else "无约束",
        "method": ROOT2 + "/tools/decomp_full/BoardAnalysis__MeetsCriteria.c",
        "semantics": "满足 ⇒ 用普通 `activeAbility`；不满足 ⇒ 用 `alternativeAbility`"
                     "（`RawCardScript__GetActiveAbility.c`）",
        "caveat": "`MeetsCriteria` 的第 6 个实参（`param_6`）语义未解，只影响 RandomChance 那一支",
    }


def dump_path(bundle, name):
    if not bundle or not name:
        return None
    folder = "bundle_" + bundle[:-len(".bundle")] if bundle.endswith(".bundle") else "bundle_" + bundle
    return os.path.join(ASSETS_FULL, folder, "MonoBehaviour", name + ".json")


def load_guid_index():
    """`guid_map_bundles.tsv` → `{guid: (资产名, 束文件, 类名)}`（同 `gen_cards_engine.load_anim_guid_index`：
    同一 GUID 多行时**保留第一行**）。只给 `alternativeAbility` 那一支用（见 `GUID_MAP_TSV` 的注释）。"""
    idx = {}
    if not os.path.isfile(GUID_MAP_TSV):
        print("⚠️ 读不到 GUID 表 %s ⇒ `alternativeAbility` 那一支**判不了**（会在产物里如实标）"
              % GUID_MAP_TSV)
        return idx
    with io.open(GUID_MAP_TSV, encoding="utf-8") as f:
        for line in f:
            p = line.rstrip("\n").split("\t")
            if len(p) < 9 or not p[3] or not p[8]:
                continue
            idx.setdefault(p[3], (p[8], p[4], p[7]))
    return idx


def so_ability_anims(so_row, key):
    """卡 SO 里 `activeAbility` / `alternativeAbility` 的**非空** CardAnim GUID 清单（保序）。"""
    out = []
    for lg in ((so_row or {}).get(key) or {}).get("activeAbilityLogic") or []:
        for an in (lg.get("cardAnimations") or []):
            g = ((an or {}).get("m_AssetGUID") or "").strip()
            if g:
                out.append(g.lower())
    return out


def build():
    vfx = load(VFX_TABLE)
    ce = load(CARDS_ENGINE)["cards"]
    byid = {c["id"]: c for c in ce}
    name_of = {c["id"]: c.get("name") or "" for c in ce}
    tween_names = load_tween_names()      # R18：PathID → UnitTweenSO 名
    unit_tweens = load_unit_tweens()      # R18：名字 → 补间定义（算长度）

    cards = []
    unresolved = []
    not_ability = []           # 不是 `activeAbility` 链上的行（见下面 `if not slot` 那段）
    unknown_card = []          # byCard 里的键不在我们卡池（不该发生，发生就报）
    for cid, e in vfx["byCard"].items():
        act, other = [], []
        for it in (e.get("ability") or []):
            if it.get("conf") != "authoritative":
                continue
            slot = it.get("abilitySlot") or ""
            ca = it.get("cardanim")
            bundle = it.get("assetBundle")
            if not slot:
                # 🔴 2026-10-23（A11 就地修 `新账-72 ④`）：633 条 `inferred-keyword` 升
                #   `authoritative`（2026-10-23）之后，`card_vfx_by_card.json` 的 `ability` 桶里
                #   混进了 **142 条 `src == "cardtrait-so"`** 的**关键词演出**行
                #   （`FlyingTraitPlay` / `StealthTraitPlay` / `CamouflageTraitPlay` …：没有
                #   `abilitySlot`、没有 `assetBundle`）。它们**不是** `activeAbility` 链上的
                #   CardAnim（原版那条链只遍历 `activeAbilityLogic[*].cardAnimations[*]`）⇒
                #   既不进时长求和，也**不该**算「映射缺口」。
                #   如实列进 `notAbilityRows`（⛔ 不静默丢），`unresolved` 只留给真正的缺口。
                not_ability.append({"id": cid, "cardanim": ca, "src": it.get("src"),
                                    "prefab": it.get("prefab"),
                                    "why": "没有 `abilitySlot` ⇒ 不是 `activeAbility` 链上的 CardAnim"
                                           "（`src == \"cardtrait-so\"` 的关键词演出行，别混进本表）"})
                continue
            p = dump_path(bundle, ca)
            if not ca or not bundle or not os.path.exists(p):
                unresolved.append({"id": cid, "cardanim": ca, "soField": it.get("soField"),
                                   "why": "CardAnim 名或束查不到" if not ca or not bundle
                                          else "dump 文件不在盘上：" + str(p)})
                continue
            ai = (load(p).get("animInfo") or {})
            row = {
                "cardanim": ca,
                "seconds": round(anim_duration(ai), 4),
                "slot": slot,
                "trigger": it.get("abilityTriggerName"),
                "chosenAbility": it.get("chosenAbilityName"),
                "waitAnimToApplyEffect": int(ai.get("waitAnimToApplyEffect") or 0),
                "tweenAnim": tween_row(ai.get("tweenAnim") or {}, tween_names, unit_tweens),
            }
            (act if slot.startswith("activeAbility") else other).append(row)

        if not act and not other:
            continue
        if cid not in byid:
            unknown_card.append(cid)
        total = round(sum(r["seconds"] for r in act), 4)
        cards.append({
            "id": cid,
            "name": name_of.get(cid, ""),
            "active": total if act else None,     # None = 原版这张卡的主动能力**一条动画都没有**
            # 🔴 R18：原版 `_ResolveAnim` 在 `waitAnimation != 0` 时**先等** tween 序列，
            #   再等 `GetAnimDuration()` ⇒ 真正等的 = 这一列。`active` 保持不变（运行时的 `s`
            #   读的是它，改它 = 无人复核的行为变更；要接由 `Core/AbilityAnimDurations.cs` 那边定）。
            "activeWithTween": (round(total + sum(waited_tween_seconds(r) for r in act), 4)
                                if act else None),
            "activeAnims": act,
            "otherAbilityAnims": other,
            # 🔴 `alternativeAbility` 那一支（二选一，见 `GUID_MAP_TSV` 的注释）—— 下面统一填
            "alternative": None,
        })

    cards.sort(key=lambda c: c["id"])

    # ---- `alternativeAbility` 那一支（原版 `GetActiveAbility` 的二选一）------------------
    #
    # 🔴 2026-10-22（`W64 · B17`）：**一个值 ≠ 全部情况**（铁律 5·c）。
    #   原版 `RawCardScript__GetActiveAbility.c`：`hasAlternativeAbility(+0x2aa)` 且
    #   `BoardAnalysis.MeetsCriteria(+0x2b0 的 `normalAbilityBoardCriteria`)` ⇒ 返回**替代能力**(+0x2b8)，
    #   **否则**返回普通 `activeAbility`(+0x260)。⇒ 带替代能力的卡，时长**随棋盘状态变化**。
    #   现读全库只有 **1 张**（`SW21 Wulfen Pack Leader`：替代那支 1 条 0.2 s，普通那支 2 条 0.4 s）。
    #   ⚠️ 本表 `cards[*].active` 记的是**普通那一支**（= 现在的运行时行为）；
    #      替代那一支单独记在 `cards[*].alternative`，并进顶层 `conditional`（含 SO 的判据原件）。
    #   ⚠️ **判据原件没解码**：`normalAbilityBoardCriteria` 里的 `playerTurn: 10 / countOptions: 0 /
    #      triggerIf: 1 / referenceValue: 0` 要靠 `BoardAnalysis.MeetsCriteria` 的方法体才读得懂 ——
    #      那个方法体本件**没读到** ⇒ 不许猜，原样带出。
    so_all = {c["uniqueId"]: c for c in load(CARD_SO_TRUTH)["cards"]}
    guid_idx = load_guid_index()
    conditional = []
    for c in cards:
        srow = so_all.get(c["id"]) or {}
        alt_guids = so_ability_anims(srow, "alternativeAbility")
        if not alt_guids:
            continue
        rows, missing = [], []
        for g in alt_guids:
            rec = guid_idx.get(g)
            p = dump_path(rec[1], rec[0]) if rec else None
            if not p or not os.path.exists(p):
                missing.append({"guid": g, "why": "GUID 表里没有 / dump 不在盘上"})
                continue
            ai = load(p).get("animInfo") or {}
            rows.append({"cardanim": rec[0], "seconds": round(anim_duration(ai), 4), "guid": g})
        alt_raw = srow.get("full_raw") if isinstance(srow.get("full_raw"), dict) else {}
        crit_raw = alt_raw.get("normalAbilityBoardCriteria")
        c["alternative"] = {
            "seconds": round(sum(r["seconds"] for r in rows), 4) if rows else None,
            "anims": rows,
            "missing": missing,
            "hasAlternativeAbility": int(srow.get("hasAlternativeAbility") or 0),
            "selectCriteria": crit_raw,
            # 🔴 R8（2026-10-23 · A11）：原来这一格只有**原始枚举数字**，没人把它翻明白。
            #   语义 = `BoardAnalysis__MeetsCriteria.c`（逐句读过），见 `_conventions.boardCriteria`。
            "selectCriteriaDecoded": decode_board_criteria(crit_raw),
            "why": ("原版 `GetActiveAbility`（`RawCardScript__GetActiveAbility.c`）："
                    "`hasAlternativeAbility` 且 `MeetsCriteria(normalAbilityBoardCriteria)` **满足** ⇒ "
                    "播**普通** `activeAbility`；**不满足**才落到 `alternativeAbility`。"
                    "⛔ 本表 `active` 只记普通那一支"),
        }
        conditional.append({
            "id": c["id"], "name": c["name"],
            "normal": c["active"], "alternative": c["alternative"]["seconds"],
            "normalWithTween": c["activeWithTween"],
            "alternativeWithTween": (round(
                (c["alternative"]["seconds"] or 0.0)
                + sum(waited_tween_seconds(r) for r in rows), 4) if rows else None),
            "criteria": c["alternative"]["selectCriteriaDecoded"],
            "note": "同一张卡、不同棋盘状态 ⇒ 两个不同的时长（铁律 5·c）",
        })
    withtruth = [c for c in cards if c["active"] is not None]
    vals = sorted(c["active"] for c in withtruth)
    med = vals[len(vals) // 2] if len(vals) % 2 else round((vals[len(vals) // 2 - 1] + vals[len(vals) // 2]) / 2.0, 4)

    # 覆盖/未覆盖（**「我们引擎真能发 `EvtKind.Ability`」那一整族**点名）
    #
    # 🔴 2026-10-22（`W64 · B17`）**换过判据**：原来按 `keywords` 里有没有
    #   duty/ferocity/oath/pray/agenda 这五个词点名。那条**不是**引擎的判据，两处都会错：
    #   · **漏**：卡面印着 `Pray: …` 而 `keywords` 数组没写那个词的卡（`RuleCore.CanUseAlternative`
    #     用的是 `UnitState.HasFx`，不是 `Has`）⇒ 现读 3 张这样的卡；
    #   · **多**：`oath` 写在**战术卡**上的 10 张（`UM109` 等）—— 它们不在棋盘上，
    #     `UseOathAbility` 永远走不到（那个方法第一步就 `ctx.Players[p].Board[slot]`）。
    #
    # 现在按**引擎那三条产生 `EvtKind.Ability` 的路**点名（判据在 `RuleCore`）：
    #   ① `UseAbility`（`Ability: ` 正文）⇒ `CardDef.HasAbility` —— 原版卡恒 false（`CardDef.cs` 有实测口径）
    #   ② `UseOathAbility`  ⇒ 卡上要有 `Oath N:` 正文（`CollectOathOps`）
    #   ③ `UseAlternative`   ⇒ `Duty`/`Pray`/`Ferocity`/`Agenda` 之一（`RuleCore.IsAlternativeAction`，
    #                          闸是 `UnitState.HasFx` = 原生正文 / 挂上去的 / 封闭文法三合一）
    # 并且**必须能上棋盘**（`type` ∈ unit/hero）—— 战术卡在场上没有槽位。
    #
    # ⚠️ 如实标：② ③ 的静态代用品是「卡面正文里有没有那一段」(`desc` + `keywords` 两种写法)
    #   —— 那正是 `EffectText` 解析的输入，但我们**没有**它的输出表（那是 C# 现算的）
    #   ⇒ 这里是**代理判据**，不是引擎的运行期真相（挂上去的 `FxOps` 看不见）。
    ALT_KEYWORDS = ("duty", "pray", "ferocity", "agenda")
    BOARD_TYPES = ("unit", "hero")

    def can_emit_ability(c):
        """这张卡在**我们引擎**里有没有一条会 `Emit(EvtKind.Ability)` 的路（见上面那段）。"""
        if (c.get("type") or "") not in BOARD_TYPES:
            return False
        kws = [(k or "") for k in (c.get("keywords") or [])]
        low = [k.strip().lower() for k in kws]
        desc = c.get("desc") or ""
        # ③ 替代行动：裸关键词（正文就是整条 desc，`CollectBareKeywordBody`）
        if any(k in ALT_KEYWORDS for k in low):
            return True
        # ③ 替代行动：`Pray: …` 这种**带头**的写法（卡面正文或 `keywords` 条目里）
        if any(k.startswith(tuple(a + ":" for a in ALT_KEYWORDS)) for k in low):
            return True
        if any((("%s:" % a) in desc.lower()) for a in ALT_KEYWORDS):
            return True
        # ② 誓约：`Oath N: …`（`CollectOathOps` 认的是这条正文）
        if any(k.startswith("oath") for k in low):
            return True
        if re.search(r"(?i)(?<!\w)Oath\s*\d", desc):
            return True
        return False

    covered_ids = set(c["id"] for c in withtruth)
    # 「这张卡在当前 build 的卡 SO 里有没有 / 它的 activeAbility 有几条 CardAnim」——
    # 🔴 判据**直接读 SO**，不再拿 vfx 表反推（vfx 是下游，用下游反推上游 = 自证）。
    #   （`so_all` 在上面 `alternativeAbility` 那一段已经载过一次，这里复用，别载两遍）
    so = so_all

    def so_active_guids(cid):
        """SO 里这张卡 `activeAbility` 的 **CardAnim GUID 清单** —— 只数**非空**的。

        🔴 2026-10-22（`W64 · B17`）**修过一条假数**：原来数的是
        `len(cardAnimations)`，而现读有 **34 张卡**的 `cardAnimations` 里是**空引用**
        （`m_AssetGUID: ""`）⇒ 那些卡会被算成「有 2 条动画」，进而被错误地判成
        「映射缺口，该修」。真实口径 = 非空 GUID 的条数（= 原版真的会播几条）。"""
        s = so.get(cid)
        if s is None:
            return None
        out = []
        for lg in ((s.get("activeAbility") or {}).get("activeAbilityLogic") or []):
            for an in (lg.get("cardAnimations") or []):
                g = ((an or {}).get("m_AssetGUID") or "").strip()
                if g:
                    out.append(g)
        return out

    want = [c for c in ce if can_emit_ability(c)]
    uncovered = []
    for c in want:
        if c["id"] in covered_ids:
            continue
        guids = so_active_guids(c["id"])
        if guids is None:
            why = "**当前 build 的卡 SO 里根本没有这张卡** ⇒ 判据为空（铁律 11 例外①）"
        elif not guids:
            why = ("卡 SO 里有这张卡、`hasActiveAbility = %d`，但它的 `activeAbility` "
                   "**一条非空的 CardAnim 引用都没有**（`m_AssetGUID` 全空）⇒ 原版这条路上"
                   "**不放任何动画** ⇒ 判据不足以定「该等 0 还是等一拍」，暂退回兜底值并**出声**"
                   % int((so[c["id"]] or {}).get("hasActiveAbility") or 0))
        else:
            why = ("卡 SO 的 `activeAbility` 有 **%d 条** CardAnim（非空 GUID），但 "
                   "`card_vfx_by_card.json` 没覆盖到（映射缺口，**该修**）⇒ 暂退回兜底值并**出声**"
                   % len(guids))
        uncovered.append({
            "id": c["id"], "name": c.get("name"), "faction": c.get("faction"),
            "inTruthSnapshot": c["id"] in so,
            "soActiveAnims": None if guids is None else len(guids),
            "canEmitAbility": True,
            "why": why,
        })

    # ---- 第二条链交叉校验：`cards_engine.json` 的 `animTimes`（`gen_cards_engine.py` 那条独立链）
    #
    # 两链互不依赖：本表 = `card_vfx_by_card` → CardAnim dump；`animTimes` = 卡 SO 的 GUID →
    # `guid_map_bundles.tsv` → CardAnim dump。**都落在同一份 `AnimInfo` 上**，
    # 但 GUID 解析、束定位、聚合是各写各的 ⇒ 逐卡比一遍能抓「某一链静默丢了一条」。
    # `animTimes` 里 `tg == -1` 的就是 `activeAbility.activeAbilityLogic[*].cardAnimations[*]`，
    # 它的 `u` = `m + s + e + d`（与 `anim_duration` **逐字同一公式**）。
    an_reported = {c["id"]: c.get("animTimes") or [] for c in ce}
    cross = {"animTimesCards": 0, "agree": 0, "mismatch": [], "onlyInAnimTimes": [],
             "onlyInTable": []}
    for cid, rows in an_reported.items():
        act = [r for r in rows if int(r.get("tg", 0)) == -1]
        if not act:
            continue
        cross["animTimesCards"] += 1
        s2 = round(sum(float(r.get("u") or 0.0) for r in act), 4)
        if cid in covered_ids:
            s1 = round(next(c["active"] for c in withtruth if c["id"] == cid), 4)
            if abs(s1 - s2) <= 1e-6:
                cross["agree"] += 1
            else:
                cross["mismatch"].append({"id": cid, "table": s1, "animTimes": s2})
        else:
            cross["onlyInAnimTimes"].append({"id": cid, "animTimes": s2})
    for c in withtruth:
        if not any(int(r.get("tg", 0)) == -1 for r in an_reported.get(c["id"], [])):
            cross["onlyInTable"].append({"id": c["id"], "table": c["active"]})
    cross["allAgree"] = (not cross["mismatch"] and not cross["onlyInAnimTimes"]
                         and not cross["onlyInTable"])

    stats = {
        "cardsWithTruth": len(withtruth),
        "activeAnims": sum(len(c["activeAnims"]) for c in withtruth),
        "cardsMatchedButNoActiveAnim": len([c for c in cards if c["active"] is None]),
        "otherAbilityAnims": sum(len(c["otherAbilityAnims"]) for c in cards),
        "min": vals[0] if vals else None,
        "median": med,
        "max": vals[-1] if vals else None,
        "unresolved": len(unresolved),
        "notAbilityRows": len(not_ability),
        "tweenRows": sum(1 for c in cards for r in (c["activeAnims"] + c["otherAbilityAnims"])
                         if r.get("tweenAnim")),
        "tweenUnresolved": sum(1 for c in cards for r in (c["activeAnims"] + c["otherAbilityAnims"])
                               if r.get("tweenAnim") and not r["tweenAnim"].get("resolved")),
        # 🔴 真正会被 `WaitForCompletion` 等的那种（`waitAnimation != 0`）—— 两个桶都数
        "tweenWaitedRows": sum(1 for c in cards for r in (c["activeAnims"] + c["otherAbilityAnims"])
                               if (r.get("tweenAnim") or {}).get("waitAnimation")),
        # 其中落在 `activeAbility` 桶里的（= 会改 `activeWithTween` 的）—— 现读 **0**
        "cardsWhoseTweenIsWaited": sum(1 for c in withtruth
                                       if any(waited_tween_seconds(r) > 0 for r in c["activeAnims"])),
        "wantedCards": len(want),            # 引擎里**真能发 `EvtKind.Ability`** 的卡（池内、能上棋盘）
        "uncoveredCards": len(uncovered),
        # 两链交叉校验（`cards_engine.animTimes` 的 `tg == -1` 行）
        "animTimesCards": cross["animTimesCards"],
        "animTimesAgree": cross["agree"],
        "animTimesMismatch": len(cross["mismatch"]),
        "animTimesOnly": len(cross["onlyInAnimTimes"]),
        "tableOnly": len(cross["onlyInTable"]),
    }

    headline = (
        "🔴 `A1767`：`EventTiming.DurationOf(EvtKind.Ability)` 的那条常数 **1.0** 换成**逐卡真值**。"
        "判据 = 该卡 `activeAbility` 各 `cardAnimations` 的 `AnimInfo.GetAnimDuration()` 之和"
        "（`vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`；原版 `AnimInfo__GetAnimDuration.c`）。"
        f"覆盖 **{stats['cardsWithTruth']} 张卡 / {stats['activeAnims']} 条 CardAnim**，"
        f"min {stats['min']} / 中位 **{stats['median']}** / max {stats['max']}。"
        # 🔴 B17（2026-10-22）加的两段：第二条链交叉校验 + 「会发 Ability 事件」那一族的准确口径
        f"**第二条链交叉校验**（`cards_engine.json` 的 `animTimes`，`tg == -1` 行；那是一条独立的 "
        f"GUID→CardAnim 链）：{stats['animTimesCards']} 张卡，与本表**逐卡一致 {stats['animTimesAgree']}**、"
        f"不一致 {stats['animTimesMismatch']} · 只在 animTimes 有 {stats['animTimesOnly']} · "
        f"只在本表有 {stats['tableOnly']}"
        f"{'（**两链全同**）' if cross['allAgree'] else '（⚠️ 有分歧，见 `_crossCheck`）'}。"
        f"**「会发 Ability 事件」那一族 = {stats['wantedCards']} 张**（`type ∈ unit/hero` 且卡面有 "
        "`Duty/Pray/Ferocity/Agenda` 正文或 `Oath N:` —— 引擎那三条产生点的代理判据），"
        f"其中 **{stats['uncoveredCards']} 张没有真值**（逐张见 `uncovered`，理由逐条写明）。"
        f"**同一张卡、不同棋盘状态 = 不同时长**的那一族：**{len(conditional)} 张**（见 `conditional`）—— "
        "原版 `RawCardScript.GetActiveAbility` 在 `activeAbility` / `alternativeAbility` 之间按 "
        "`normalAbilityBoardCriteria` **二选一**；本表 `active` 只记**普通那一支**。"
        "⚠️ **四处如实标注**：① 那 " + str(len(uncovered)) + " 张没有真值 ——"
        " **不是快照年代**（2026-10-18 起真值 = 当前 build 的卡 SO），而是它们的卡 SO "
        "`activeAbility` 里**一条非空 CardAnim 引用都没有**（`m_AssetGUID` 全空）⇒ "
        "原版这条路上不放动画、「该等 0 还是等一拍」判据不足（铁律 11 例外①）；"
        f"② 原版 `_ResolveAnim` 里 tween 那一段（`animInfo.tweenAnim != null && waitAnimation != 0` 时 "
        "先 `WaitForCompletion(BuildSequence)`、**再** `WaitForSeconds(GetAnimDuration())`）—— "
        f"🔴 **2026-10-23（A11 · R18）已解**：{stats['tweenRows']} 条 CardAnim 带 tweenAnim，"
        "PathID→名字→长度**三跳全落盘**（`cards[*].*Anims[*].tweenAnim`；名字来自 "
        "`anim_address_map.json` 的 `bundle_tweenandshakes_assets_all`，长度按 "
        "`UnitTweenSO__BuildSequence.c` + `TweenInfoAnimationBase__GetTween.c` 的排布算）"
        f"；解不出 {stats['tweenUnresolved']} 条。真正会被等的（`waitAnimation != 0`）"
        f"**{stats['tweenWaitedRows']} 条，全部落在 `otherAbilityAnims`（触发链）** ⇒ "
        f"**本表 `active` 受影响的卡 = {stats['cardsWhoseTweenIsWaited']} 张**，"
        "`activeWithTween` 与 `active` 现读逐卡相等。"
        "⚠️ **运行时也没接**（`Core/AbilityAnimDurations.cs` 读的仍是 `active`）；"
        "要接由卡牌线定（会改对局节奏）；"
        "③ `cardAbilities[*]`（`ThisCardPlayed` / `Slay` / … 那一族）是**别的时机**播的，"
        "**不进这张表**（留在 `cards[*].otherAbilityAnims` 备查）；"
        f"④ `card_vfx_by_card.json` 的 `ability` 桶里有 **{len(not_ability)} 条**"
        "（`src == \"cardtrait-so\"` 的关键词演出行：`FlyingTraitPlay` / `StealthTraitPlay` …）"
        "**没有 `abilitySlot`** ⇒ 不是 `activeAbility` 链上的 CardAnim，单列在 `notAbilityRows`，"
        "⛔ 既不算时长、也不算「映射缺口」（2026-10-23 修 `新账-72 ④`）。"
    )

    conventions = {
        "scope": "只取 `activeAbility.activeAbilityLogic[*].cardAnimations[*]`（原版「单位主动发动能力」那一条路）；"
                 "`cardAbilities[*]` 归 `EvtKind.Trigger`/`Play`，不在本表",
        "formula": "AnimInfo.GetAnimDuration() = vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos（不含 startDelay）",
        "aggregate": "一张卡的值 = 它 `activeAbility` 全部 CardAnim 的时长**求和** —— 依据 "
                     "`AbilityLogic__PlayAnim.c` 会把这一整串逐条 `AddCardAnim` 排队播完",
        "fallback": f"表里没有这张卡 ⇒ 退回 {FALLBACK}（= 本表中位数；**能力事件那条路会出声**，见 `AbilityAnimDurations`）",
        # 🔴 2026-10-23（A11）**订正**：原来这一格写「tweenAnim 的长度；waitAnimToApplyEffect
        #   只如实带出不参与求和」—— tween 那一段**已经算了**（见上面 R18 的那段注释）。
        #   `waitAnimToApplyEffect` 的状态**不变**：只如实带出，仍不参与求和。
        "tween": "`animInfo.tweenAnim`（UnitTweenSO）：`m_PathID → 名字（`anim_address_map.json` 的 "
                 "tween 束）→ 长度（`Resources/UnitTweens.json`）`三跳都已落盘；长度 = BuildSequence "
                 "排布下的 DOTween 序列总长；**只在 `waitAnimation != 0` 时原版才等它**"
                 "（`_ResolveAnim` 先等 tween、再等 GetAnimDuration）。卡片级汇总 = `activeWithTween`。"
                 "⚠️ 运行时读的仍是 `active`（不含 tween）",
        "waitAnimToApplyEffect": "只如实带出，不参与求和（原版它是「这一拍带不带等待标记」，不是时长）",
        "boardCriteria": "`normalAbilityBoardCriteria` 的语义 = `BoardAnalysis__MeetsCriteria.c`（第一权威）；"
                         "**满足 ⇒ 走普通 `activeAbility`，不满足 ⇒ 走 `alternativeAbility`**"
                         "（`RawCardScript__GetActiveAbility.c`）。枚举值出处 `PlayersAffected.cs` / "
                         "`CountOptions.cs` / `StatFilterOptions.cs`。逐卡翻译在 `conditional[*].criteria`。"
                         "⚠️ 这个字段**不在** `cards_engine.json` 里（`cards_raw.json` 的 `full_raw` 才有）"
                         "⇒ 引擎要接得先让 `gen_cards_engine.py` 把它带出来（卡牌线的文件）",
        "whoEmitsAbility": "`EvtKind.Ability` 在引擎里只有三条产生点：`RuleCore.UseAbility`（`Ability:` 正文，"
                           "原版卡恒 false）· `UseOathAbility`（`Oath N:`）· `UseAlternative`"
                           "（`Duty/Pray/Ferocity/Agenda`）。前两条要 `Board[slot]` ⇒ **战术卡不可能发**",
        "secondChain": "`cards_engine.json` 的 `animTimes` 里 `tg == -1` 的行 = 同一份 "
                       "`activeAbility.activeAbilityLogic[*].cardAnimations[*]`；那条链走的是 "
                       "`guid_map_bundles.tsv`（与本表的 `card_vfx_by_card.json` 不同一条）⇒ 逐卡比一遍",
        "soAnimCount": "`uncovered[*].soActiveAnims` 数的是**非空 GUID** 的条数 —— "
                       "现读有 34 张卡的 `cardAnimations` 里是空引用（`m_AssetGUID: \"\"`），"
                       "数 `len()` 会得出假数（2026-10-22 修）",
    }

    full = {
        "schema": "ability_anim_durations/v1",
        "generatedBy": "d:/4/Unity/工具/gen_ability_anim_durations.py",
        "_sources": {
            "cardVfxByCard": VFX_TABLE,
            "cardsEngine": CARDS_ENGINE,
            "cardAnimDumps": ASSETS_FULL + "/bundle_*cardanims*/MonoBehaviour/*.json",
            # 当前 build 的卡 SO 语料（2026-10-18 换；旧的是 2025-07-30 的 cards_1195_2022时代.json）
            "cardSoTruth": CARD_SO_TRUTH,
            "splitter": ROOT2 + "/tools/decomp_full/AbilityLogic__PlayAnim.c",
            "resolver": ROOT2 + "/tools/decomp_full/BattleManager._ResolveAnim_d__466__MoveNext.c",
            "duration": ROOT2 + "/tools/decomp_full/AnimInfo__GetAnimDuration.c",
            # 🔴 R18 / R8（2026-10-23 · A11）新增的判据源
            "tweenSequencer": ROOT2 + "/tools/decomp_full/UnitTweenSO__BuildSequence.c",
            "tweenCommon": ROOT2 + "/tools/decomp_full/TweenInfoAnimationBase__GetTween.c",
            "tweenPathIds": TWEEN_PATHID_INDEX,
            "unitTweens": UNIT_TWEENS_JSON,
            "boardCriteria": ROOT2 + "/tools/decomp_full/BoardAnalysis__MeetsCriteria.c",
            "activeAbilityChoice": ROOT2 + "/tools/decomp_full/RawCardScript__GetActiveAbility.c",
        },
        "_headline": headline,
        "_conventions": conventions,
        "cards": cards,
        # 🔴 2026-10-22（`W64 · B17`）：**同一张卡、不同棋盘状态 = 不同时长**的那一族
        #   （原版 `GetActiveAbility` 在 `activeAbility` / `alternativeAbility` 之间二选一）。
        #   现读只有 `SW21` 一张；`cards[*].alternative` 里带 SO 的判据原件。
        "conditional": conditional,
        "uncovered": uncovered,
        "unresolved": unresolved,
        # 🔴 2026-10-23（A11）：`card_vfx_by_card.json` 的 `ability` 桶里那 142 条
        #   **没有 `abilitySlot`** 的关键词演出行（`src == "cardtrait-so"`）—— 不是本表的口径，
        #   单列备查，⛔ 不静默丢、也不算「映射缺口」。详见 `_headline` ④。
        "notAbilityRows": not_ability,
        "unknownCardIds": unknown_card,
        "_crossCheck": cross,
        "stats": stats,
    }

    rt_items = [{"id": c["id"], "name": c["name"], "s": c["active"], "n": len(c["activeAnims"])}
                for c in withtruth]
    rt = {
        "schema": "ability_anim_durations/v1+rt",
        "generatedBy": "d:/4/Unity/工具/gen_ability_anim_durations.py",
        "_headline": (
            f"`A1767`：逐卡「发动能力」时长 = 该卡 `activeAbility` 各 CardAnim 的 "
            f"`AnimInfo.GetAnimDuration()` 之和。{len(rt_items)} 张卡 · 中位 {med} · "
            "表里没有这张卡 ⇒ 退回 `fallbackSeconds` 并**出声**。"
            "判据长文 / 未覆盖名单 → `数据/游戏数据/ability_anim_durations.json` 的 `_headline` 与 `uncovered`。"),
        "fallbackSeconds": med,
        "count": len(rt_items),
        "items": rt_items,
    }
    return full, rt


def canon(o):
    return json.dumps(o, ensure_ascii=False, sort_keys=True, indent=1)


def main():
    check = "--check" in sys.argv
    full, rt = build()
    if check:
        ok = True
        for p, o in ((OUT_FULL, full), (OUT_RT, rt)):
            if not os.path.exists(p):
                print("✗ 缺文件: " + p)
                ok = False
                continue
            a = canon(load(p))
            b = canon(o)
            if "generatedAt" in a:
                pass
            if a == b:
                print("✅ 一致: " + p)
            else:
                # 忽略 generatedAt 的差异
                fa, fb = json.loads(a), json.loads(b)
                fa.pop("generatedAt", None)
                fb.pop("generatedAt", None)
                if fa == fb:
                    print("✅ 一致（仅 generatedAt 不同）: " + p)
                else:
                    print("✗ 不一致: " + p)
                    ok = False
        print("=== --check " + ("全过 ✅" if ok else "有差异 ✗"))
        return 0 if ok else 1
    io.open(OUT_FULL, "w", encoding="utf-8", newline="\n").write(canon(full))
    io.open(OUT_RT, "w", encoding="utf-8", newline="\n").write(canon(rt))
    s = full["stats"]
    print("[全量] %s  (%d 字节)" % (OUT_FULL, os.path.getsize(OUT_FULL)))
    print("[运行时] %s  (%d 字节)" % (OUT_RT, os.path.getsize(OUT_RT)))
    print("覆盖 %d 张卡 / %d 条 CardAnim · min %s / 中位 %s / max %s · 解不出 %d"
          % (s["cardsWithTruth"], s["activeAnims"], s["min"], s["median"], s["max"], s["unresolved"]))
    print("引擎里真能发 `EvtKind.Ability` 的卡 %d 张，其中没真值的 **%d 张**（逐张见 `uncovered`）"
          % (s["wantedCards"], s["uncoveredCards"]))
    for u in full["uncovered"]:
        print("   ! %s %s（%s）soActiveAnims=%s —— %s"
              % (u["id"], u["name"], u["faction"], u["soActiveAnims"], u["why"][:60]))
    print("另有 %d 张卡在真值里有键、但 `activeAbility` 一条动画都没有（原版就不放东西）"
          % s["cardsMatchedButNoActiveAnim"])
    print("tweenAnim（R18）：%d 条带引用 · 解不出 %d · 会被等的 %d 条（落在 active 桶 %d 张卡）"
          % (s["tweenRows"], s["tweenUnresolved"], s["tweenWaitedRows"],
             s["cardsWhoseTweenIsWaited"]))
    print("不是 `activeAbility` 链上的行（`src == \"cardtrait-so\"`）：%d 条 → `notAbilityRows`（不算缺口）"
          % s["notAbilityRows"])
    print("两链交叉校验（animTimes 的 tg=-1）：%d 张 · 一致 %d · 不一致 %d · 只在 animTimes %d · 只在本表 %d%s"
          % (s["animTimesCards"], s["animTimesAgree"], s["animTimesMismatch"],
             s["animTimesOnly"], s["tableOnly"],
             "" if full["_crossCheck"]["allAgree"] else "  ⚠️ 有分歧"))
    for c in full["conditional"]:
        print("   ~ 条件分支 %s %s：普通 %s s / 替代 %s s（⛔ 不是一个固定值）"
              % (c["id"], c["name"], c["normal"], c["alternative"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
