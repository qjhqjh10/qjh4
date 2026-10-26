#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
B9 · 全池效果对拍驱动（P1 · 第八十三会话）

用途：把「每张卡的每个 `chosenAbility` 效果到底有没有实现 / 实现得对不对」
变成一张**可复跑、幂等**的差异表。

它【不】重新发明读数口 —— 读的是现成的 `ruleprobe play` 两条路：
  ① 主档（打出去那一拍）      `ruleprobe.sh play all pf <out>` 里 **不带 `E|` 前缀** 的行
     列（源码 `PlayProbe.FingerprintRow`）：id/type/warlord/playRc/played/faceOps/selfPlace/
     mech/other/expectedPay/energyDelta/handCostDelta/dmg/changes/crash/bucket/fixtureFit/
     injected/**changesFull**（逐条变化原文）
  ② 事件电池（打出去之后那一段）`play all pf` 里 **带 `E|` 前缀** 的行（只覆盖 `type=unit`）
     列（源码 `PlayProbe.FingerprintERow`）：id/type/bucket/cand/playRc/played/ctrlPlayed/
     steps/raw/ctrl/net/netDmg/note/**netFull**（净差逐条原文）

⚠️ 两条口都**不判对错**（工具自己的头注就这么写：「判据在卡面上」）。
   本脚本**只做一件事**：把卡面/真值推出的**期望形状**与这两条口的读数**逐行比**，
   并给一个**保守**的判定（见下 `§判定`）。判据仍是卡面英文原文 + `cards_truth.json` 的结构化真值。

============================ 判定（三档 + 两条铁律） ============================

* `对`     —— 该族要求的**可观察签名**在读数里出现了。
* `错`     —— **同时**满足：① 该行 `trigger == 0`（打出即结算，主档一定会走到）
                ② 卡真的打出去了（`playRc == 0`）③ 卡不是督军（督军走对照局，不是「打出」）
                ④ 没有任何「夹具看不到 / 条件不成立」的档（见下）⑤ 整张卡**一条变化都没有**。
* `未验到` —— 其余全部。**必须**在 `备注` 里写明是哪一档闸挡住的（不许只写「未验到」）。

🔴 两条防假缺陷的铁律（照 `B8_BE` 的教训）：
   ① **`unitsToSummon` 25% 是惰性遗留值** —— 只有 `chosenAbility ∈ {70,71,72,75,77,90,100,101,270}`
      时原版才读它。别把别的族身上的 `unitsToSummon` 当判据。
   ② **条件/目标类闸先判再判缺陷** —— `conditionsFilter` / `keywordFilter` / `traitsFilter` /
      `armyFilter` / `rarityFilter` / `filterSpecificCards` / `multiCountCriteria` /
      `chooseCardInPlay` / `selectFromChooseResolution` 任一在场 = **夹具不一定满足条件**
      ⇒ 落 `未验到`，⛔ 不落 `错`。

🔴 另外四道闸（每一条都是被实测的假缺陷逼出来的，完整清单见 `判据/B9_全池对拍_规格.md` §2·4）：
   ③ **`TargetsAffected` 看不见的那几档**（`50/51/52` 手牌 · `55/60` 墓地 · `65` 牌库 · `140/150`
      前一段目标 · `180` 选择 · `190` 上一张打出 · `200/201` 引用卡 · `210/220` 计数 · `245` 秘密）+
      **单位卡上的 `ta=30`**（夹具把它解析成「刚打出的这张卡自己」，见下 `§读数口径`）。
   ④ **`buffType ∈ {12,15,16}`（附魔注册）· `buffType=4`（剥关键词）· 负向加成** ——
      效果要么在后续事件才发生、要么在夹具目标上是 0。
   ⑤ **卡面文字闸**（`TEXT_GATES`）：`if you control no other …` · `for each …` · `died this game` ·
      `Choose …` · ` or ` · `your/the next … costs` · `Shuriken` · `All enemies lose …` · `up to …`。
   ⑥ **夹具结构不适用**（探针 `fixtureFit` 列非空 = `ephemeral`/`defence`，补进手牌才测到）
      ⇒ 只在「没观察到效果」时当闸，观察到了照样判对。

📐 读数口径（三条反直觉的，⛔ 别按 `other` 列想当然）：
   · 探针 `other` 把**所有**带自己 id 的 `✚ 落场` 算成 `selfPlace` ⇒ `Kroot Hound` 那种
     「召唤自己的复制体」在 `other` 上是 0 —— 本脚本用 `effective_changes` 只剔**第一行**。
   · `Rally: Gain +3 Attack` 这类**自身加成烘在「落地那一行」**里（行尾「落地时它自己身上与印的数不同」）。
   · **能量 / 手牌总价**要看探针自己加的 `⇒ 除付费外净 ±M` / `⇒ 除出牌本身外净 ±M` 标记，
     ⛔ 不能拿 `⚙ 能量` / `💰 手牌总价` 行本身当证据（出牌付费也会产生那一行）。


🆕 `P3`（2026-10-26）：**两套夹具并行**（默认那套**一个字没动**）
   `--run` 除了基准那两份读数，还会跑**第二遍全池扫描**：
       `ruleprobe.sh play all pf <cache>/all_pf_pd.txt dump <cache>/all_dump_pd.tsv preset dead=1,pick=1`
   · `dead=1` —— 往 P0 墓地预置 1 张友方部队（`reanimate` 那一族要残骸才验得到）。
   · `pick=1` —— 给**单位卡**一个「玩家点定的目标」：原版 `targeted == true` 的卡**没有目标时
     整段 `ThisCardPlayed` 直接跳过**（`RawCardScript.OnCardPlayedWithTarget`），而 `ta=30 (target)`
     那一支只认调用方传进来的目标（`AbilityLogic__GetTargets.c:192-207`）⇒ 默认夹具把
     「友方单位」解析成**刚打出去的这张卡自己**（满血）⇒ 治疗恒 0。
   ⛔ 第二套**只**用于 `USE_PRESET` 点名的那些行，其余行照旧读基准那一套
   —— 否则「换夹具」会污染全表、还会与 `play check` 的基线打架。
   **第三套** = 再多一个 `chipenemy=1`（P1 非督军单位各扣 2 血）：治「目标必须是**受伤**的单位」
   那一档（`a damaged enemy` / `a damaged friendly unit`）—— 基准夹具里**敌人全是满血** ⇒ 目标池空。
   判据 = 卡面英文原文里出现 `damaged`（现读 13 行 / 8 张卡）。


用法：
    python -I -X utf8 d:/4/Unity/工具/pool_effect_check.py                 # 用缓存读数出表
    python -I -X utf8 d:/4/Unity/工具/pool_effect_check.py --run           # 先跑 ruleprobe 刷新读数
    python -I -X utf8 d:/4/Unity/工具/pool_effect_check.py --only 70,75    # 只看某些族
    python -I -X utf8 d:/4/Unity/工具/pool_effect_check.py --out X.tsv

⛔ 本脚本不改任何 `Assets/**.cs` / `工具/ruleprobe/**` / 账表 —— 只读那两样、只写自己的三个产物。
"""

import argparse
import io
import json
import os
import re
import subprocess
import sys
from collections import Counter, defaultdict

ROOT = "d:/4/Unity"
DEF_TRUTH = ROOT + "/MyGame/Assets/RuleEngine/Resources/cards_truth.json"
DEF_ENGINE = ROOT + "/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
DEF_CACHE = ROOT + "/_tmp_view/p1_pool"
DEF_OUT = ROOT + "/资料/完全复刻/判据/B9_全池对拍_差异.tsv"
PROBE = ROOT + "/工具/ruleprobe.sh"

# ── 原版 `AbilityEffect` 枚举（`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AbilityEffect.cs`，83 个成员）
#    → 语义一句话 + 可观察签名。
#    「判据」列写的是取证来源：
#      · `dispatch`  = 在 `decomp_full/AbilityLogic__PlayAbility.c` 里读到该 case 与它调的原版方法
#      · `enum-name` = 只有枚举名（`.c` 里没逐字读到该分支，语义按枚举名 + 卡面用法推定）
#    ⚠️ 签名是**并集**：命中任一即算「有效果」；`[]` = 这一族**本来就不改局面**。
FAM = {
    0:   ("drawCard", "抽牌：从牌库抽 N 张进手", ["hand_gain"], "dispatch(:1431 AddDrawCard)"),
    3:   ("doOnlyVfx", "只播特效，不改任何局面字段", [], "dispatch(:1461 只打日志后 return)"),
    10:  ("doDamage", "对每个目标造成 min~max 伤害", ["damage", "hp_down"], "dispatch(:1504 DealMultiDamage)"),
    12:  ("doDamageMultiRandom", "随机多段伤害（每段独立随目标）", ["damage", "hp_down"], "dispatch(:1623 DealMultiDamageVariable)"),
    13:  ("doDamageFast", "快速伤害（damageType=fast 那一支）", ["damage", "hp_down"], "dispatch(:1504 同 10，末参 =fast)"),
    15:  ("doDamageAndDestroySelf", "造成伤害后自毁", ["damage", "board_leave"], "dispatch(:1671 DealMultiDamageAndDestroySelf)"),
    20:  ("doHeal", "治疗目标（满血时不产生可见变化）", ["hp_up", "heal"], "dispatch(:1295/:1303 HealOne/HealMultiple)"),
    30:  ("buffOthers", "给目标挂属性/关键词加成", ["board_mod"], "dispatch(:1218/:1369 AddEffect)"),
    31:  ("buffOthersNotResetPool", "给目标挂加成（不重置池）", ["board_mod"], "enum-name"),
    40:  ("buffSelf", "给自己挂属性/关键词加成", ["board_mod"], "dispatch(:1369 AddEffect，this=自己)"),
    50:  ("buffHand", "改手牌：加属性 / 减费 / 复制到手上", ["handcost_extra", "hand_gain", "hand_loss"], "dispatch(:1412/:1431 AddHandEffect)"),
    60:  ("buffMana", "加/改资源：能量 · 信仰 · 灵魂石 · 任务点", ["resource", "energy_extra"], "dispatch(:713 AddManaBuff)"),
    70:  ("summonUnit", "按 unitsToSummon 部署 N 个目标单位", ["board_add_match"], "dispatch(:857 AddSummonUnit)"),
    71:  ("summonUnitAndSetRef", "部署并把召唤物设为 reference", ["board_add_match"], "dispatch(:857 + 写 +0x280)"),
    72:  ("summonUnitAndTriggerEffect", "部署并立刻触发它的效果", ["board_add_match"], "dispatch(:857 case 0x48)"),
    75:  ("summonBuffedUnit", "部署一个带加成的单位", ["board_add_match"], "dispatch(:857 case 0x4b)"),
    77:  ("summonUnitDelayed", "延迟部署（pos 固定 3）", ["board_add_match"], "dispatch(:857 case 0x4d)"),
    80:  ("transformSelf", "把自己变成另一张卡", ["board_mod", "board_add"], "dispatch(:900 AddTransformCard)"),
    85:  ("transformSelfWithBuff", "把自己变成另一张卡并带加成", ["board_mod", "board_add"], "dispatch(:936 AddTransformCardWithBuff)"),
    90:  ("transformUnit", "把目标单位变成另一张卡", ["board_mod", "board_add"], "dispatch(:974 AddTransformCard)"),
    100: ("createCardInHand", "按 unitsToSummon 造 N 张牌进手", ["hand_gain_match"], "dispatch(:1023 AddNewCardToHand)"),
    101: ("createCardInHandAndKeepEffects", "造牌进手并保留其效果", ["hand_gain_match"], "dispatch(:1141 AddNewCardToHand + AddEffect)"),
    110: ("destroy", "消灭目标单位", ["board_leave"], "dispatch(:1097 DestroyUnit)"),
    120: ("jam", "把目标列为 jam（干扰）", ["marker_jam"], "dispatch(:634 JamUnit)"),
    130: ("stun", "眩晕目标", ["marker_stun"], "dispatch(:651 AddStun)"),
    135: ("stunMultiplier", "随机挑目标眩晕（stun 的倍率版）", ["marker_stun"], "dispatch(:676 Shuffle+AddStun)"),
    140: ("recallToOwnerHand", "把目标退回其拥有者手牌", ["board_leave", "hand_gain"], "dispatch(:556 AddRecallToHand)"),
    150: ("recallToCasterHand", "把目标退回施法者手牌", ["board_leave", "hand_gain"], "dispatch(:579 AddRecallToHand)"),
    160: ("discardFromHand", "弃掉目标手牌", ["hand_loss_other", "discard"], "dispatch(:597 AddDiscardFromHand)"),
    162: ("disappearFromHand", "让手牌消失（不进弃牌堆）", ["hand_loss_other"], "dispatch(:616 AddDisappearFromHand)"),
    165: ("discardWholeHand", "弃掉整手牌", ["hand_loss_other", "discard"], "dispatch(:338 GetCardsInHandList+Discard)"),
    170: ("stealFromHand", "从对方手里偷牌", ["hand_gain", "hand_loss_other"], "dispatch(:360 AddStealFromHand)"),
    175: ("stealFromDeck", "从对方牌库偷牌", ["deck_gain"], "dispatch(:377 AddStealFromDeck)"),
    180: ("stealMinion", "抢走对方一个单位", ["board_transfer"], "dispatch(:396 AddStealMinion)"),
    185: ("stealMinionSelf", "用自己换走对方一个单位", ["board_transfer"], "dispatch(:483-525)"),
    190: ("exchangeSelfForMinion", "用自己与目标单位交换", ["board_transfer"], "dispatch(:431 AddExchangeMinions)"),
    200: ("exchangeRandomFriendlyMinion", "与随机友军交换", ["board_transfer"], "enum-name"),
    210: ("copyUnitInCemetery", "复制墓地里一个单位", ["board_add"], "dispatch(:454 CopyUnitInCemetery)"),
    220: ("resetActions", "重置行动（refresh）", ["board_mod"], "enum-name"),
    230: ("forceAttack", "强制目标攻击", ["damage", "board_mod"], "dispatch(:2640 段)"),
    231: ("forceAttackDontCountAttack", "强制目标攻击（不计本次攻击）", ["damage"], "dispatch(:2641 case 0xe7)"),
    233: ("forceAttackFriendly", "强制友军互攻", ["damage"], "dispatch(:2679 case 0xe9)"),
    235: ("scriptedSelfAttack", "脚本化：自己打一发", ["damage"], "dispatch(:2695 case 0xeb)"),
    236: ("scriptedActingCardAttack", "脚本化：行动卡打一发", ["damage"], "dispatch(:2716 case 0xec)"),
    237: ("scriptedTargetCardAttackDontCount", "脚本化：目标卡打一发（不计）", ["damage"], "dispatch(:2748 case 0xed)"),
    238: ("scriptedSelfCardAttackDontCount", "脚本化：自己打一发（不计）", ["damage"], "dispatch(:2764 case 0xee)"),
    240: ("disappearUnit", "让目标消失（不留残骸）", ["board_leave"], "dispatch(:2780 case 0xf0)"),
    250: ("autoPlayFromHand", "从手牌/牌库自动打出一张", ["board_add", "hand_loss_other"], "enum-name"),
    260: ("triggerEffectInCard", "触发某张卡上的效果", ["board_mod", "damage", "hand_gain"], "dispatch(:1218 AddEffect)"),
    270: ("createCardInDeck", "按 unitsToSummon 造牌进牌库", ["deck_gain"], "enum-name"),
    280: ("revealOrder", "揭示牌序（不改局面字段）", [], "dispatch(:2492 AddRevealOrders)"),
    290: ("missAction", "使目标本次行动落空", ["board_mod"], "enum-name"),
    300: ("recallToDeck", "把目标退回牌库", ["board_leave", "deck_gain"], "enum-name"),
    301: ("recallToDeckToTop", "把目标退回牌库顶", ["board_leave", "deck_gain"], "enum-name"),
    310: ("displayFromHand", "展示手牌（UI 行为）", [], "enum-name"),
    320: ("followAfterResolution", "延后到本次结算之后再结算（当拍无变化）", [], "dispatch(:2455 AddFollowAfterResolution)"),
    325: ("followResolutionIfPreviousMeetsCriteria", "上一段满足条件才延后结算", [], "dispatch(:2380-2455)"),
    330: ("drawUntilEqual", "双方手牌拉平（抽到一样多）", ["hand_gain"], "dispatch(:2390 NumCardsInHand 差)"),
    340: ("removeThisEnchantment", "移除自己这个附魔", ["board_leave", "board_mod"], "enum-name"),
    345: ("removeEnchantment", "移除目标附魔", ["board_leave"], "enum-name"),
    350: ("removeFromCemetery", "从墓地移除", ["discard"], "dispatch(:1986 AddRemoveFromCemetery)"),
    360: ("playChosenCardAbility", "打出被选中那张卡的替代能力", ["board_mod", "board_add", "damage"], "dispatch(:2031 PayCardPlayedCostOath+OnTriggerAlt)"),
    370: ("triggerRelentless", "触发 Relentless（不屈）", ["board_mod", "board_add"], "dispatch(:2006 AddTriggerRelentless)"),
    375: ("triggerSacrifice", "触发 Sacrifice（献祭）", ["board_leave"], "enum-name"),
    380: ("setReferenceCard", "记录 reference（不改局面字段）", [], "enum-name"),
    390: ("triggerRequiem", "触发 Requiem（安魂）", ["board_mod"], "enum-name"),
    400: ("triggerCourtesy", "触发 Courtesy（礼节）", ["board_mod"], "enum-name"),
    410: ("triggerCodex", "触发 Codex（能量=卡费）", ["board_mod", "damage", "hand_gain"], "dispatch(:1694 AddTriggerCodex)"),
    411: ("triggerRegiment", "触发 Regiment（远程后）", ["board_mod", "damage"], "dispatch(:1713 AddTriggerRegiment)"),
    420: ("setCardDeckToTop", "把一张牌放到牌库顶（顺序变化）", [], "dispatch(:1730 AddSetCardToTop)"),
    430: ("reanimate", "复活一个残骸（Reanimate）", ["board_add"], "dispatch(:1753 AddTransformFromRemnant)"),
    440: ("triggerSpiritStone", "触发灵魂石能力", ["resource", "board_mod"], "dispatch(:1776 AddTriggerSpiritStone)"),
    450: ("triggerSynapse", "触发 Synapse（突触）", ["board_mod"], "dispatch(:1795 AddTriggerSynapse)"),
    460: ("triggerMob", "触发 Mob（近战后）", ["damage", "board_mod"], "dispatch(:1815 AddTriggerMob)"),
    461: ("triggerMobAdditional", "触发 Mob 追加段", ["damage"], "dispatch(:1815 同族)"),
    462: ("triggerPray", "触发 Pray（祈祷）", ["damage", "board_mod"], "dispatch(:1847 AddTriggerPray)"),
    465: ("triggerAmbush", "触发 Ambush（伏击）", ["damage", "board_mod"], "dispatch(:1871 AddTriggerAmbush)"),
    466: ("triggerUprising", "触发 Uprising（起义）", ["board_add", "board_mod"], "dispatch(:1887 AddTriggerUprising)"),
    467: ("triggerSlay", "触发 Slay（击杀）", ["damage", "board_mod"], "dispatch(:1903 AddTriggerSlay)"),
    468: ("triggerTeleport", "触发 Teleport（瞬移）", ["board_mod", "board_add"], "dispatch(:1919 AddTriggerTeleport)"),
    470: ("showCardsWindow", "弹一个看牌窗（UI 行为）", [], "dispatch(:1928 AddShowCardsWindowAction)"),
    475: ("resetDuty", "重置 Duty（职责）", ["board_mod"], "dispatch(:1949 AddResetDuty)"),
    480: ("highlightBloodThirst", "高亮 Blood Thirst（UI 行为）", [], "dispatch(:1970 SendHighlightBloodThirstAction)"),
}

# 原版**会读** `unitsToSummon` 的族（B8_BE §2·1 实测；其余族上挂的值是惰性遗留）
SUMMON_READERS = {70, 71, 72, 75, 77, 90, 100, 101, 270}
SUMMON_FAMILIES = {70, 71, 72, 75, 77}
CREATE_FAMILIES = {100, 101}

# 「本来就不改局面」的族：`other == 0` 是**正确**结果
NO_BOARD_FAMILIES = {3, 280, 310, 320, 325, 380, 420, 470, 480}

# 原版 `AbilityTrigger` 名（`Core/CardDef.cs:1917` 那张表；这里只抄本脚本要印出来的那批）
TRIG = {
    0: "ThisCardPlayed", 10: "OtherCardPlayed", 15: "PlayedThirdCardInTurn", 20: "WhileInPlay",
    30: "TurnStart", 31: "TurnStartPlayer", 40: "TurnEnd", 41: "TurnBeforeEnd", 43: "Courtesy",
    45: "Resolution", 46: "Talent", 50: "UnitAttack", 60: "UnitDamaged", 70: "UnitHealed",
    80: "ThisUnitDeath", 81: "Unstable", 90: "OtherUnitDeath", 100: "Growth",
    110: "HeroDefenseBonus", 120: "Slay", 130: "Rage", 145: "Codex", 155: "TurnSetup",
    160: "TurnStarted", 170: "UnitDeathByPoison", 175: "UnitDeathAttacking",
    180: "UnitDeathByAbility", 185: "UnitDamagedByAbility", 187: "UnitDamagedByAttack",
    190: "OtherUnitSummoned", 195: "ThisUnitSummoned", 200: "TriggerOnceIfBoardCriteria",
    210: "CardCreatedInHand", 220: "Sacrifice", 225: "OtherCardSacrifice", 230: "Relentless",
    240: "WhileInPlayVariable", 245: "Pack", 250: "ThisCardDrawnAsTrap", 260: "OtherCardDrawn",
    270: "TrapResolved", 280: "AboutToAttack", 290: "UnitPoisoned", 291: "UnitStunned",
    300: "Mob", 301: "Regiment", 400: "ThisUnitAbilityUsed", 410: "FollowUpResolution",
    420: "Survivor", 422: "SurvivorSpent", 430: "Requiem", 435: "OtherCardRequiem",
    440: "Landing", 450: "OtherCardLanding", 455: "OtherCardSpiritStone", 460: "Mutated",
    470: "CardReturnedToHand", 480: "CardReturnedToDeck", 490: "QuestTriggered",
    500: "TriggerEnchantment", 510: "UnitReceivesMarkOfChaos", 515: "UnitReceivesMarkerlight",
    520: "UnitReceivesHuntMark", 550: "Support", 560: "Artifice", 570: "Reanimated",
    580: "Strike", 590: "Combo", 600: "UseSpiritStone", 605: "UseFaith",
    610: "OnCollectSpiritStone", 620: "TriggeredSwarm", 630: "TriggeredSynapse",
    640: "EarnedSkull", 641: "SpentAllMana", 645: "OtherCardMob", 646: "OtherCardRegiment",
    647: "UnitLoseStealth", 650: "TriggeredCodex", 655: "Pray", 660: "TriggeredPray",
    661: "TriggeredDuty", 662: "Duty", 665: "OnCollectFaith", 670: "Ambush", 680: "Uprising",
    690: "Teleport", 695: "OnCollectQuestPoints", 700: "OnSecretCreated",
    701: "OnSabotageCreated", 705: "Agenda", 710: "Stimulation", 715: "Ecstasy",
    720: "Cruelty", 725: "Ferocity", 726: "TriggeredFerocity", 730: "Oath",
}

# 事件电池**能抬起**的 trigger（判据 = `PlayProbe.cs:1080-1130` 的步设计 + 电池自检那几对
#   「真/改」读数：Oath·selfChip(UnitDamaged)·软攻击·chip2·tactic(OtherCardPlayed)·
#   troop2(OtherUnitSummoned)·end0/end1(TurnEnd)·enemyTactic·friendDies(OtherUnitDeath)·
#   collect(OnCollectSpiritStone)·draw(OtherCardDrawn)·abil(ThisUnitAbilityUsed)·
#   atkM/atkR2(UnitAttack→Mob/Regiment)·kill(Slay)·V1 Codex·V2 Teleport·V3 Rally
#   ＋ 两个来回的 EndTurn/BeginTurn(TurnStart/TurnStartPlayer)）
BATTERY_TRIG = {0, 10, 15, 30, 31, 40, 46, 50, 60, 80, 90, 120, 145, 190, 195, 260, 280,
                300, 301, 400, 410, 420, 440, 460, 462, 466, 467, 468, 560, 580, 600, 610,
                640, 655, 661, 662, 670, 680, 690, 705, 710, 725, 730}
# ⚠️ **为什么是这批**（2026-10-26 P1 实测，⛔ 别按「步名猜」）：拿「**除这一条 trigger 之外没有
#   任何其它效果行**」的卡做隔离实验，看它们的电池净差是不是非 0 —— 非 0 ⇒ 电池**确实**抬得起来。
#   实测：46 Talent 15/15 · 560 Artifice 16/16 · 580 Strike 19/22 · 680 Uprising 7/7 ·
#   60 UnitDamaged 2/2 · 80 ThisUnitDeath 3/4 · 710 Stimulation 5/5 · 715 Ecstasy 2/7 ·
#   720 Cruelty 2/7 · **81 Unstable 0/5 · 510 MarkOfChaos 0/6 · 570 Reanimated 0/4**（后三个确实抬不到）。
#   ⇒ 只有 715/720 是**半覆盖**（暂不计入）；81/510/570 明确不计入。

# 「是条件句 / 要选 / 挑特定目标」的 criteria 键 —— 在场就落 `未验到`，⛔ 不落 `错`
COND_KEYS = ("conditionsFilter", "excludeCondition", "keywordFilter", "traitsFilter",
             "armyFilter", "rarityFilter", "filterSpecificCards", "multiCountCriteria",
             "chooseCardInPlay", "selectFromChooseResolution", "costValueOptions",
             "attackFilter", "currentHealthFilter", "manaCostFilter", "boardCriteria",
             "countCriteria", "excludeTrait", "excludeRace")

# 原版 `TargetsAffected`（`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TargetsAffected.cs`，41 个成员）
#   → **读数看不见**的那几档（判据见各条理由）。⛔ 不是「全都是闸」：`self(20)` / `allFiltered(10)`
#   / `target(30)`（仅限 tactic 且 `targeted=1`）/ `friendlyHero(120)` 这些**看得见**。
TA_UNOBS = {
    55: "目标在场面或濒死区（夹具没有濒死区）",
    60: "目标在墓地（夹具默认不铺坟场，需 `preset dead=N`）",
    65: "目标在牌库（牌库只读 id 增减，不看属性）",
    140: "目标 = 前一段能力的目标（本行单独跑不到）",
    150: "目标 = 前一段能力的过滤结果（同上）",
    180: "目标 = 选择结果（要玩家在弹窗里选）",
    190: "目标 = 上一张打出的卡（本行单独跑不到）",
    200: "目标 = 本卡内引用的那张卡（reference 未灌）",
    201: "目标 = 本卡内引用且在场的卡（同上）",
    210: "目标 = 按 trait 计数的结果（计数驱动）",
    220: "目标 = 目标上的 trait 计数（计数驱动）",
    245: "目标 = 已打出的秘密（夹具没有秘密）",
}

# 手牌类目标 = **软闸**：卡**属性**看不见，但「换了一张卡」这件事在读数里看得见（`✋ 手牌 ±「id」`）
TA_SOFT = {
    50: "目标在手牌里：手牌卡的**属性**不在读数里（id 与总价看得见）",
    51: "目标在手牌+牌库：属性不在读数里（id 看得见）",
    52: "目标在手牌+场面：属性不在读数里（id 看得见）",
}

# 卡面文字里**结构上让夹具看不到**的句子（判据 = 卡面英文原文，玩家看到的就是它）
#   每条 = (正则, 理由, 硬度)：
#     `hard` = 这一句在场 ⇒ **这块效果本来就不该在这一拍发生** ⇒ 未验到，不算缺陷
#     `soft` = 这一句只说明**其中一部分**是条件/计数（基础那一段仍应生效）⇒ 也算未验到，
#              但会额外标进 `候选缺陷` 列，供下一批复核（⛔ 不当场判错）
TEXT_GATES = [
    (r"if you control no other", "条件「你没有任何其它部队」（主档恒不成立；电池 V3 空棋盘变体可抬）", "hard"),
    (r"if you control (a|an|\d)", "条件「你若控制某种部队/武器」（夹具不一定有）", "hard"),
    (r"(your|the) next [^.]* costs?", "限定「下一张某种牌」的费用（夹具手牌里不一定有那种牌）", "hard"),
    (r"(your|all your) (troops?|vehicles?|infantry|beasts?|drones?|stratagems?|spells?)[^.]*costs?",
     "只给**某一类**牌减费（夹具手牌只有 1 张垫牌，且每 5 张才 1 张战术卡 ⇒ 手里不一定有那一类）", "hard"),
    (r"for each", "按计数结算（夹具计数常为 0；基础那一段仍应生效）", "soft"),
    (r"died (this game|this battle|since)|\bthat died\b|\bdealt? this game\b", "需要墓地里的阵亡单位", "hard"),
    (r"\bchoose\b", "需要玩家在弹窗里选", "hard"),
    (r"\bor\b", "卡面 `or`（二选一 / 条件换值）", "hard"),
    (r"shuriken", "取单位的 Shuriken 值（夹具单位没有该关键词）", "hard"),
    (r"all enemies lose", "敌人没有该关键词时无变化", "hard"),
    (r"\bup to\b", "上限可选（可以选 0）", "hard"),
]


# ---------------------------------------------------------------------------
#  🆕 `P3`（2026-10-26）：**分派表** —— 哪些行必须读「带 preset 的第二套读数」
# ---------------------------------------------------------------------------
#  每条都带判据，⛔ 别凭感觉加（多派一条 = 拿另一套夹具的读数冒充基准读数）。
def use_preset(row):
    """→ `(dead, pick)` 两个布尔（都 False = 读基准那一套）。

    · `dead` —— `reanimate(430)`：原版 `AddTransformFromRemnant` 要**墓地里有残骸**；
      默认夹具是空墓地 ⇒ 那一族的「零实得」是**夹具**造成的，不是引擎。
    · `pick` —— **触发=0 的单位卡 + `targetsAffected=30`**：原版 `targeted == true` 的卡
      **没有目标时整段 `ThisCardPlayed` 直接跳过**（`RawCardScript.OnCardPlayedWithTarget`：
      `if (!targeted || target != null)` 才 `TriggerAbilities`，否则只打一条
      `OnCardPlayedWith null Target` 警告）；而 `ta=30 (target)` 只认调用方传进来的目标
      （`AbilityLogic__GetTargets.c:192-207`）⇒ 默认夹具把「友方单位」解析成**刚打出去的这张卡自己**
      （满盘血）⇒ 治疗恒 0。⭐ 现读：**触发=0 且 `ta=30` 的单位卡 52 行 / 52 行全是 `targeted=1`**
      （2026-10-26 现算；⛔ 别把张数写死）。
    """
    tc = row.get("tc") or {}
    dead = (row["ca"] == 430)
    pick = (row["ctype"] == "unit" and row["trigger"] == 0 and tc.get("targetsAffected") == 30)
    return dead, pick


# 「目标必须是**受伤**的单位」—— 基准夹具里**敌人全是满血**（我方那三张扣过 2 血），
#   所以这一档只能靠第三套扫描（`chipenemy=1`）。判据 = **卡面英文原文**里有 `damaged` 这个词
#   （玩家看到的就是它；⛔ 不是按族猜）。现读 13 行 / 8 张卡（截 80 字后仍命中）。
DAMAGED_RE = re.compile(r"\bdamaged\b", re.I)


def _wake3(wake):
    """把 `wake` 补齐成 `(dead, pick, chip)` 三个布尔（`None` ⇒ 全 False = 基准夹具）。"""
    w = tuple(wake or ())
    return (w + (False, False, False))[:3]


def use_chip(row):
    """这一行要不要读**第三套**读数（`dead=1,pick=1,chipenemy=1`）。"""
    return bool(DAMAGED_RE.search(row.get("desc") or ""))


def observable(sigs):
    """把 `FAM` 的签名换成**读数里真会出现**的那几个桶（`Sig.tags` 里没有 `*_match`）。

    `board_add_match` / `hand_gain_match` 是「要**卡 id** 对上」的伪签名（走 `precise_match`），
    在 `Sig` 里对应的可观察桶分别是 `board_add` / `hand_gain`。
    """
    m = {"board_add_match": "board_add", "hand_gain_match": "hand_gain"}
    return [m.get(s, s) for s in sigs]


def follow_sigs_by_card(truth):
    """`followAfterResolution(320)` 那一族的**期望签名**：取**同卡 `trigger == 410`** 那几行。

    🔴 判据（现读 `AbilityLogic__PlayAbility.c:2466-2473`）：`case 320` 只调
    `BattleManager__AddFollowAfterResolution(manager, thisCard, actingCard, priority, …)`
    —— 它**登记**延后结算，本行自己的 `damage`/`buff`/`summon` 载荷**不在这一拍结算**；
    真正结算的是同卡 `trigger = 410 (FollowUpResolution)` 那几行。
    ⇒ 拿它们的 `chosenAbility` 去 `FAM` 查签名，才是这一行**应有的可观察量**。

    ⚠️ 现读 **27 行里 26 张有 410 兄弟行**；剩下 1 张（`No Mercy`）与几张 410 载荷本身没签名的
    （`Cloud of Smoke` 260 / `Hunta Rig` 235 / `Let Loose` 260）⇒ 返回空 ⇒ 仍按「本来就不改局面」处理。
    """
    out = {}
    for c in truth["cards"]:
        sigs = []
        for src in ("abilities", "activeAbility", "alternativeAbility"):
            v = c.get(src)
            items = v if isinstance(v, list) else ([v] if isinstance(v, dict) else [])
            for a in items:
                if a.get("trigger") != 410:
                    continue
                for lg in a.get("logic") or []:
                    ca = lg.get("chosenAbility")
                    fam = FAM.get(ca)
                    if not fam:
                        continue
                    for s in observable(fam[2]):
                        if s not in sigs:
                            sigs.append(s)
        if sigs:
            out[c["id"]] = sigs
    return out


def load_json(p):
    with io.open(p, encoding="utf-8") as f:
        return json.load(f)
def split_lines(s):
    return [x.strip() for x in (s or "").split(";") if x.strip()]


def board_mod_tokens(line):
    """`✎ 改动 P0[3] FIX_A 2/0/0/2 → FIX_A 3/1/0/3 {flank1}` → {flank1}"""
    i = line.find("{")
    if i < 0:
        return set()
    j = line.find("}", i)
    if j < 0:
        return set()
    return set(t.strip() for t in line[i + 1:j].split(",") if t.strip())


def delta_of(line):
    """`☠ 伤害累计 P0 10 → 13` → 3（读不到 → None）"""
    try:
        seg = line.split("→")
        if len(seg) < 2:
            return None
        left = seg[-2].strip().split()[-1]
        right = seg[-1].strip().split()[0]
        return int(right) - int(left)
    except Exception:
        return None


def stat_nums(seg):
    """从 `✎ 改动` 的一侧里取出「攻/远/甲/血」那四个整数（读不到 → None）。

    🔴 **2026-10-26 `P3` 修（原来这里恒失败、而且静默）**：行文形状是
    `✎ 改动 P0[3] FIX_A 2/0/0/2 → FIX_A 2/0/0/4` —— **`→` 右边也带卡名**，
    而旧代码写的是 `right.split()[0]`（取到 `FIX_A`）⇒ `len(rv) >= 4` 恒假
    ⇒ `hp_up` / `hp_down` 两个桶**从来没有被点亮过**（治疗族因此几乎全判「未验到」）。
    ⇒ 改成「从右往左找第一个 `a/b/c/d` 形状的 token」。
    """
    for tok in reversed(seg.split()):
        parts = tok.split("/")
        if len(parts) >= 4:
            try:
                return [int(x) for x in parts]
            except Exception:
                continue
    return None


def effective_changes(obs, card_id, keep=()):
    """把「出牌本身 / 付费」那几条机械变化剔掉，留下**实得**。

    🔴 为什么不直接用探针的 `other` 列：它把「✚ 落场」里**凡是带自己 id 的都算 `SelfPlace`**
       ⇒ `Kroot Hound`（`Rally: Deploy a Kroot Hound`，召的是自己的复制体）被算成 `selfPlace=2`
       / `other=0`，于是「召出了第二只」在 `other` 上**看不见**（2026-10-26 P1 实测，跑单卡读数确认）。
       这里的口径：同 id 的 `✚ 落场` **第一行**算机械（= 这张卡自己落地），**第二行起**算实得。
    """
    out = []
    seen_self = 0
    self_line = None
    for l in obs:
        if l.startswith("🎖 出兵数"):
            continue
        if l.startswith("⚙ 能量") and "energy" not in keep:
            continue
        if l.startswith("💰 手牌总价") and "handcost" not in keep:
            continue
        if l.startswith("✋ 手牌") and "-「" in l and ("「" + (card_id or "")) in l:
            continue
        if l.startswith("🗑 弃牌") and ("「" + (card_id or "")) in l:
            continue
        if l.startswith("✚ 落场") and ("→ " + (card_id or "") + " ") in l:
            seen_self += 1
            if seen_self == 1:
                # 🔴 这张卡自己落地那一行**不能扔**：`Rally: Gain +3 Attack` 这类**自身加成**
                #   是**在落地之前就算好**的 ⇒ 只在 `✚` 行里以「落地时与印的数不同」露出来
                #   （2026-10-26 P1 实测 `TAU71` 6→9、`SOR33` 多出 `invulnerable1`）。
                self_line = l
                continue
        out.append(l)
    return out, self_line


class Sig(object):
    """把一行变化归到签名桶里（并集；同一行可以命中多个桶）。"""

    def __init__(self, lines):
        self.lines = lines
        self.tags = set()
        self.hp_down = []
        self.hp_up = []
        for l in lines:
            if l.startswith("☠ 伤害累计"):
                d = delta_of(l)
                if d and d > 0:
                    self.tags.add("damage")
                    self.dmg = d
            elif l.startswith("✚ 落场"):
                self.tags.add("board_add")
            elif l.startswith("☠ 离场"):
                self.tags.add("board_leave")
            elif l.startswith("✎ 改动"):
                self.tags.add("board_mod")
                toks = board_mod_tokens(l)
                if "stun1" in toks:
                    self.tags.add("marker_stun")
                if "jam1" in toks:
                    self.tags.add("marker_jam")
                # 血量增减（`X a/b/c/d → X a2/b2/c2/d2`，第 4 个数是血）
                try:
                    left, right = l.split("→")
                    lv = stat_nums(left)
                    rv = stat_nums(right)
                    if lv and rv:
                        dh = rv[3] - lv[3]
                        if dh < 0:
                            self.hp_down.append(dh)
                            self.tags.add("hp_down")
                        elif dh > 0:
                            self.hp_up.append(dh)
                            self.tags.add("hp_up")
                except Exception:
                    pass
            elif l.startswith("✋ 手牌"):
                if "+「" in l:
                    self.tags.add("hand_gain")
                if "-「" in l:
                    self.tags.add("hand_loss")
            elif l.startswith("🂠 牌库"):
                if "+「" in l:
                    self.tags.add("deck_gain")
                if "-「" in l:
                    self.tags.add("deck_loss")
            elif l.startswith("🗑 弃牌"):
                self.tags.add("discard")
            elif l.startswith("⚙ 能量"):
                self.tags.add("energy")
                # 探针在「**除付费外**还有净额」的那一行上自己加了 `⇒ **照卡面应付 N · 除付费外净 ±M**`
                #   ⇒ 有这句话就是**实得**（`buffMana` 的判据；⛔ 别拿 `⚙ 能量` 行本身当证据 ——
                #   出牌付费也会产生一行，那样整族都会「看见变化」）
                if "除付费外净" in l:
                    self.tags.add("energy_extra")
            elif l.startswith("💰 手牌总价"):
                self.tags.add("handcost")
                if "除出牌本身外净" in l:      # 同上：探针自己抵掉了「这张牌离手」那一份
                    self.tags.add("handcost_extra")
            elif l.startswith("🔮"):
                self.tags.add("resource")
        # 牌被抢走 = 一边 +、一边 −（跨玩家）
        if any("手牌 P0 +「" in l for l in lines) and any("手牌 P1 -「" in l for l in lines):
            self.tags.add("hand_transfer")
        # 棋盘换边 = 两格里同一个 id 的「离场 + 落场」
        if "board_leave" in self.tags and "board_add" in self.tags:
            self.tags.add("board_transfer")

    def payload_ids(self, prefix):
        """`✚ 落场/生成 P0[3] . → TAU11 4/2/0/4 {..}` → `TAU11`"""
        out = []
        for l in self.lines:
            if not l.startswith(prefix):
                continue
            try:
                body = l.split("→", 1)[1].strip()
                tok = body.split()[0]
                if tok and tok != ".":
                    out.append(tok)
            except Exception:
                pass
        return out


def gate_notes(row, ctype, cardinfo, wake=None):
    """返回 `[(硬度, 理由)]`（空 = 没闸）。硬度 ∈ {hard, soft}，见 `TEXT_GATES` 的说明。

    三路判据（缺一个就会造出假缺陷）：
      ① **结构**：`targetCriteria` 里的条件键 · `TargetsAffected` 里读数看不见的那几档 ·
         `multiplier`/`furtherMultiplier`（计数驱动）· `buffType`（附魔注册 / 剥关键词）
      ② **卡面文字**：`TEXT_GATES`（条件句 / 要选 / 二选一 / 需要某个关键词）
      ③ **夹具**：墓地空 · 单位上的 `target` 被解析成「刚打出的这张卡自己」（满血）

    🆕 `P3`：`wake = (dead, pick)` —— **这一行读的是带 preset 的第二套读数**时，对应的那条夹具闸
    **不再成立**（`pick` 已经给了玩家点定的目标、`dead` 已经铺了墓地）⇒ ⛔ 别再把它们当闸。
    """
    why = []
    dead_ok, pick_ok, chip_ok = _wake3(wake)
    lg = row.get("lg") or {}
    tc = row.get("tc") or {}
    for k in COND_KEYS:
        if k in tc:
            why.append(("hard", "criteria." + k))
    ta = tc.get("targetsAffected")
    if ta in TA_SOFT:
        why.append(("soft", "ta=%s：%s" % (ta, TA_SOFT[ta])))
    if ta in TA_UNOBS:
        why.append(("hard", "ta=%s：%s" % (ta, TA_UNOBS[ta])))
    if ta == 30 and ctype == "unit" and not pick_ok:
        why.append(("hard", "ta=30(target) 落在单位上：夹具把它解析成「刚打出的这张卡自己」（满血 / 已是最强）"))
    # 🆕 `P3`：卡面要「**受伤**的单位」而基准夹具里**敌人满血**（我方扣过 2 血）⇒ 目标池可能为空。
    #   ⚠️ 只在**没读第三套扫描**时算闸；读了 `chipenemy=1` 就不再是闸。
    if DAMAGED_RE.search(row.get("desc") or "") and not chip_ok:
        why.append(("hard", "卡面要「damaged（受伤）」的目标，而基准夹具里**敌人满血** ⇒ 目标池可能是空的"))
    if (lg.get("multiplier") or {}).get("targetsAffected") is not None:
        why.append(("hard", "multiplier（按计数加减，夹具计数常为 0）"))
    if row.get("chanceOfHappening"):
        why.append(("hard", "chanceOfHappening（概率）"))
    if row.get("randomAbilityChoice"):
        why.append(("hard", "randomAbilityChoice（随机挑）"))
    if row.get("limitedUses"):
        why.append(("soft", "limitedUses（限次）"))
    b = lg.get("buff") or {}
    if b.get("buffType") in (12, 15, 16) or b.get("enchantment"):
        why.append(("hard", "附魔注册（buffType=%s）：效果在**后续事件**才发生，本拍看不到" % b.get("buffType")))
    if b.get("buffType") == 8:
        why.append(("soft", "buffType=8（标记/随机类加成）：读数里不一定现形"))
    if b.get("buffType") == 4:
        why.append(("hard", "剥关键词：夹具目标没有该关键词时无变化"))
    if any((b.get(k) or 0) < 0 for k in ("meleeAttackChange", "rangeAttackChange",
                                         "healthChange", "armourChange")):
        why.append(("hard", "负向加成：夹具目标的该项常为 0 / 会被夹到 0，读数看不到"))
    d = row.get("desc") or ""
    for pat, reason, hard in TEXT_GATES:
        if re.search(pat, d, re.I):
            why.append((hard, reason))
    return why


def expected_text(fid, row, name_of):
    """人读的期望一句话。"""
    lg = row["lg"]
    if fid in SUMMON_FAMILIES:
        us = (lg.get("summon") or {}).get("unitsToSummon") or []
        names = [u + ("(%s)" % name_of.get(u)) if name_of.get(u) else u for u in us]
        if names:
            return "落场：" + " / ".join(names[:4]) + ("…" if len(names) > 4 else "")
        return "落场：unitsToSummon 为空（原版按 targetCriteria 选，判不了具体卡）"
    if fid in CREATE_FAMILIES:
        us = (lg.get("summon") or {}).get("unitsToSummon") or []
        names = [u + ("(%s)" % name_of.get(u)) if name_of.get(u) else u for u in us]
        return "进手：" + (" / ".join(names[:4]) if names else "unitsToSummon 为空")
    if fid == 270:
        us = (lg.get("summon") or {}).get("unitsToSummon") or []
        return "进牌库：" + (" / ".join(u + ("(%s)" % name_of[u] if u in name_of else "") for u in us[:4]) if us else "unitsToSummon 为空")
    if fid in (10, 12, 13, 15):
        d = lg.get("damage") or {}
        n = (lg.get("targetCriteria") or {}).get("maxTargets")
        return "造成 %s~%s 伤害%s" % (d.get("minDamage"), d.get("maxDamage"),
                                    ("×%s 目标" % n) if n else "")
    if fid == 20:
        h = lg.get("heal") or {}
        return "治疗 %s~%s" % (h.get("minValue"), h.get("maxValue"))
    if fid in (30, 31, 40):
        b = lg.get("buff") or {}
        bits = []
        for k, lab in (("meleeAttackChange", "近战"), ("rangeAttackChange", "远程"),
                       ("healthChange", "生命"), ("armourChange", "护甲"), ("costChange", "费用")):
            if b.get(k):
                bits.append("%s%+d" % (lab, b[k]))
        if b.get("trait"):
            bits.append("trait=%s" % b["trait"])
        return ("加" + "/".join(bits)) if bits else "挂 %s 类加成" % b.get("buffType")
    if fid == 50:
        return "改手牌（加属性/减费/复制）"
    if fid == 60:
        return "加资源（能量/信仰/灵魂石/任务点）"
    if fid in (110, 240):
        return "目标离场"
    if fid in (130, 135):
        return "目标获得 stun"
    if fid == 120:
        return "目标获得 jam"
    if fid in (140, 150):
        return "目标离场并进手牌"
    if fid in (0, 330):
        return "抽牌"
    if fid in SUMMON_FAMILIES | {90, 80, 85}:
        return "目标变形/落场"
    return FAM.get(fid, ("?", "", [], ""))[1]



def same_card(actual, expected, name_of):
    """同名不同 id（`cards_truth.json` 里 70 组「同一张卡两个 SO」）也算命中。"""
    if actual in expected:
        return True
    an = name_of.get(actual)
    return bool(an) and any(name_of.get(e) == an for e in expected)


def ids_after(lines, prefix, marker):
    """从变化行里抽 `✚ 落场/生成 … → X` 或 `✋ 手牌 P0 +「X」` 的 X。"""
    out = []
    for l in lines:
        if not l.startswith(prefix):
            continue
        try:
            if marker == "arrow":
                out.append(l.split("→", 1)[1].strip().split()[0])
            else:
                out.append(l.split(marker)[1].split("」")[0])
        except Exception:
            pass
    return [x for x in out if x and x != "."]


def precise_match(fid, lg, lines, card_id, name_of):
    """召唤 / 造牌 / 进牌库 这三类要**卡对**（不是「有变化就算」）。

    → `(verdict, got)`；`verdict is None` = 这一族不走精确匹配，交给签名。
    ⚠️ 同 id 的落场第一行是**这张卡自己**（会被调用方先剔掉，见 `effective_changes`）。
    """
    us = (lg.get("summon") or {}).get("unitsToSummon") or []
    if fid in SUMMON_FAMILIES:
        adds = ids_after(lines, "✚ 落场", "arrow")
        if us:
            if any(same_card(a, us, name_of) for a in adds):
                return "对", "落场：" + ",".join(adds)
            if adds:
                return "错", "落场：" + ",".join(adds)
        elif adds:
            return "对", "落场：" + ",".join(adds)
        return None, ""
    if fid in CREATE_FAMILIES:
        ids = ids_after(lines, "✋ 手牌", "+「")
        if us:
            if any(same_card(i, us, name_of) for i in ids):
                return "对", "进手：" + ",".join(ids)
            if ids:
                return "错", "进手：" + ",".join(ids)
        elif ids:
            return "对", "进手：" + ",".join(ids)
        return None, ""
    if fid == 270:
        ids = ids_after(lines, "🂠 牌库", "+「")
        if not us:
            return None, ""
        if ids:
            if any(same_card(i, us, name_of) for i in ids):
                return "对", "进牌库：" + ",".join(ids)
            return "错", "进牌库：" + ",".join(ids)
        return None, ""
    return None, ""


def judge(row, main_row, bat_row, name_of, ctype, wake=None, picked=False, pick_none=False):
    """→ (判定, 期望, 实得, 备注)

    🆕 `P3`：`wake = (dead, pick)` —— 调用方**已经把读数换成带 preset 的那一套**时传它，
    两条夹具闸（墓地空 / `ta=30` 落在单位上）随之失效。`None` = 基准夹具（一字节不变）。
    `picked` = 这一行真的**点定了一个目标**（dump 的 `pickTarget` 列非空）；
    `pick_none` = 走了 `pick=1` 但**目标池是空的**（夹具里没有合法目标 ⇒ 仍算夹具闸）。
    """
    fid = row["ca"]
    trig = row["trigger"]
    fam = FAM.get(fid)
    if fam is None:
        return "未验到", "（枚举里没有这个值）", "", "ca=%s 不在 AbilityEffect 枚举内" % fid
    fname, fsem, sigs, src = fam
    dead_ok, pick_ok, chip_ok = _wake3(wake)
    if fid == 320 and row.get("fsigs"):
        # `followAfterResolution` 本行**不结算**（原版只 `AddFollowAfterResolution` 登记），
        # 可观察量落在同卡 `trigger = 410` 那几行 ⇒ 用它们的签名（见 `follow_sigs_by_card`）。
        sigs = row["fsigs"]

    exp = expected_text(fid, row, name_of)
    if fid == 320 and row.get("fsigs"):
        exp = "延后结算（同卡 410 行的载荷）：" + ",".join(row["fsigs"])

    # ── 通道 1：主档（trigger 0 = 打出即结算）
    if trig == 0:
        if main_row is None:
            return "未验到", exp, "", "闸：该卡不在池内（`cards_engine.json` 1128 张里没有它 ⇒ 无卡面文本、也没有读数）"
        if main_row[3] != "0" and main_row[6] != "hero":
            return "未验到", exp, "打不出去：rc=" + main_row[3] + " " + main_row[6], "闸：出牌失败"
        if main_row[6] == "hero":
            # 督军档 = 对照局净差，不是「打出」
            eff, _self = effective_changes(split_lines(main_row[18]), row["id"])
            s = Sig(eff)
            hit = [t for t in sigs if t in s.tags]
            if hit:
                return "对", exp, "督军对照局净差命中（%s）" % ",".join(hit), "通道=督军对照局"
            return "未验到", exp, "督军对照局净差 0 条", "闸：督军不走「打出」，见 MeasureHero"
        obs = split_lines(main_row[18])
        ta = (row.get("tc") or {}).get("targetsAffected")
        if ta in (50, 51, 52):
            # 目标在手牌 ⇒ 看得见的是「换了一张卡」与「手牌总价」；**卡属性看不见**（见 TA_SOFT）
            sigs = list(sigs) + ["hand_gain", "hand_loss", "handcost_extra"]
        eff, self_line = effective_changes(obs, row["id"])
        s = Sig(eff)
        # 🔴 能量 / 手牌总价那两行会被 `effective_changes` 当机械行剔掉，但**探针自己加过标记的**
        #   那两条（`⇒ 除付费外净 ±M` / `⇒ 除出牌本身外净 ±M`）是**实得** ⇒ 从原始行里补回签名。
        _all = Sig(obs)
        for _t in ("energy_extra", "handcost_extra"):
            if _t in _all.tags:
                s.tags.add(_t)
        self_modified = bool(self_line) and ("落地时它自己身上与印的数不同" in self_line)
        if self_modified:
            s.tags.add("self_modified")
            if fid in (20, 30, 31, 40, 50, 60, 130, 135):
                s.tags.add("board_mod")
        other = len(eff)
        hit = [t for t in sigs if t in s.tags]
        lg = row["lg"]
        gates = gate_notes(row, ctype, row, wake)
        fit = row.get("fit") or ""
        if fit:
            # `ephemeral` / `defence` 两族是**补进手牌**才测到的 ⇒ 探针自己标了「结构上不适用」。
            #   只在「**没观察到效果**」时才拿它当闸；观察到了（签名命中）照样判对。
            gates = gates + [("hard", "夹具结构上不适用（`%s`）⇒ `other=0` 不能单独当缺陷" % fit)]

        # 精确匹配：召唤 / 造牌 / 进牌库 要**卡对**（含「同名双 id」）
        pv, pgot = precise_match(fid, lg, eff, row["id"], name_of)
        if pv == "对":
            return "对", exp, pgot, ""
        if pv == "错":
            return "错", exp, pgot, "落/造的卡不在 unitsToSummon 里，且不同名"
        if fid == 270 and not ((lg.get("summon") or {}).get("unitsToSummon")):
            return "未验到", exp, "无牌库新增", "闸：createCardInDeck 的 unitsToSummon 为空，判不了具体卡"
        if fid == 430 and other == 0 and not dead_ok:
            return "未验到", exp, "墓地空 ⇒ 无可复活目标", "闸：Reanimate 需要残骸；夹具默认不铺坟场（可加 `preset dead=N`）"
        if fid == 430 and other == 0 and dead_ok:
            return ("未验到", exp, "墓地已预置 1 张、仍零实得",
                    "闸：`preset dead=1` 已开 ⇒ 这条**不是夹具口径**问题（残骸在墓地却翻不回来，待复核）")

        if hit:
            return "对", exp, "命中签名：" + ",".join(hit) + "（other=%d）" % other, ""

        if gates:
            hard = [r for h, r in gates if h == "hard"]
            soft = [r for h, r in gates if h == "soft"]
            # 条件型 Rally（「若你没有任何其它部队」）→ 电池的 V3 空棋盘变体就是为它准备的
            if any("没有任何其它部队" in r for r in hard) and bat_row is not None:
                net = split_lines(bat_row[13])
                if net:
                    return ("对", exp, "电池 V3（空棋盘变体）净差 %s 条" % bat_row[10],
                            "通道=电池 V3；主档因条件不成立看不到")
            note = "闸：" + "；".join(hard + soft)
            if soft and not hard:
                # `for each …` 只修饰**附加份数**，基础那一份仍应生效 ⇒ buff 三族零变化 = 缺陷
                if (fid in (30, 31, 40) and all("按计数结算" in s for s in soft) and other == 0):
                    return ("错", exp, "一条变化都没有（other=0）",
                            "基础那一份没生效（卡面的 `for each` 只影响附加份数）")
                # 只有「**什么都没发生** ＋ 真的可能有基础份数」才进候选；
                # 「本来就不改局面」的族（320/380/…）与「已经有变化」的行都不进（会灌水）
                if other == 0 and fid not in NO_BOARD_FAMILIES:
                    note = "⚠ 候选缺陷（软闸：" + "；".join(soft) + "）"
            return "未验到", exp, "无该族签名（other=%d）" % other, note
        if fid == 320 and row.get("fsigs"):
            # 🆕 `P3`：这一行的期望**不再为空**（见 `follow_sigs_by_card`）⇒ 不能走
            #   「该族本来就不改局面」那一支。⛔ 但也不判「错」：它是**间接归因**
            #   （可观察量属于同卡 410 那几行）⇒ 零实得只记未验到并写明，交给下一批复核。
            if other == 0:
                return ("未验到", exp, "零实得（other=0）",
                        "闸：延后结算的载荷**这一拍**没看到（本引擎在同一调用里结算 ⇒ 待复核）")
            return "未验到", exp, "有 %d 条变化但无该族签名" % other, "同卡其它效果产生变化，无法归因"
        if fid in NO_BOARD_FAMILIES:
            if other == 0:
                return "对", exp, "无局面变化（该族本来就不改局面）", "other=0 属正确"
            return "未验到", exp, "有 %d 条变化但不属该族签名" % other, "同卡其它效果产生变化，无法归因"
        if other == 0:
            # 🆕 `P3`：分派到 `pick=1` 的那两条 —— ⛔ 别把「夹具点不到目标 / 点了但引擎没采用」
            #   判成「打出成功却零实得」。目标池空 = 夹具闸；点了仍零实得 = 候选缺陷（待复核）。
            if pick_none:
                return ("未验到", exp, "一条变化都没有（other=0）",
                        "闸：`pick=1` 的目标池为空（夹具里没有这张卡要的合法目标）")
            if picked:
                return ("未验到", exp, "一条变化都没有（other=0）",
                        "⚠ 候选缺陷（待复核）：已点定目标 `%s`、仍零实得" % (row.get("pick_target") or "?"))
            return "错", exp, "一条变化都没有（other=0）", "打出成功却零实得"
        return "未验到", exp, "有 %d 条变化但无该族签名" % other, "同卡其它效果产生变化，无法归因"

    # ── 通道 2：事件电池（只覆盖 type=unit）
    if ctype == "?":
        return "未验到", exp, "", "闸：该卡不在池内（`cards_engine.json` 1128 张里没有它 ⇒ 无卡面文本、没有读数）"
    if ctype in ("None", ""):
        return "未验到", exp, "", "闸：卡类型未知（`cards_engine.json` 里这张卡 type 列为空）"
    if ctype != "unit":
        return "未验到", exp, "", "闸：电池只覆盖 unit（本卡 type=%s）" % ctype
    if bat_row is None:
        return "未验到", exp, "", "闸：该卡没有电池行"
    net = split_lines(bat_row[13])
    steps = bat_row[7] or ""
    if trig is None:
        # activeAbility / alternativeAbility 行
        if net:
            return "对", exp, "电池净差 %s 条（净伤 %s）" % (bat_row[10], bat_row[11]), "通道=电池（未逐族归因）"
        return "未验到", exp, "电池净差 0 条", "闸：电池没抬起这张卡的主动能力（steps: %s）" % steps[:80]
    if trig not in BATTERY_TRIG:
        # 被动光环那一族（`20 WhileInPlay` / `240 WhileInPlayVariable`）电池不「触发」，
        #   但它在**打出那一拍**就重算了 ⇒ 回主档看签名（有就算对，没有仍算未验到）
        if main_row is not None and main_row[3] == "0":
            s0 = Sig(effective_changes(split_lines(main_row[18]), row["id"])[0])
            hit0 = [t for t in sigs if t in s0.tags]
            if hit0:
                return ("对", exp, "主档命中被动签名（%s）" % ",".join(hit0),
                        "通道=主档（被动光环在打出那一拍生效）")
        return "未验到", exp, "", "闸：trigger %s(%s) 电池不触发" % (trig, TRIG.get(trig, "?"))
    pv, pgot = precise_match(fid, row["lg"], net, row["id"], name_of)
    if pv == "对":
        return "对", exp, "电池：" + pgot, "通道=电池"
    if pv == "错":
        return "错", exp, "电池：" + pgot, "电池里落/造的卡不在 unitsToSummon 里，且不同名"
    s = Sig(net)
    hit = [t for t in sigs if t in s.tags]
    if hit:
        return "对", exp, "电池净差命中（%s，共 %s 条）" % (",".join(hit), bat_row[10]), "通道=电池"
    if net:
        return "未验到", exp, "电池净差 %s 条但无该族签名" % bat_row[10], "同卡其它事件产生差异，无法归因"
    return "未验到", exp, "电池净差 0 条", "闸：电池未抬起该事件（steps: %s）" % steps[:80]


def run_probe(cache):
    os.makedirs(cache, exist_ok=True)
    dump = cache + "/all_dump.tsv"
    pf = cache + "/all_pf.txt"
    print("[run] ruleprobe play all dump …")
    r = subprocess.run(["bash", PROBE, "play", "all", "dump", dump],
                       cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        print("[run] dump rc=%s（读数可能不可信）" % r.returncode)
        sys.stderr.write((r.stdout or "")[-2000:])
    print("[run] ruleprobe play all pf …")
    r = subprocess.run(["bash", PROBE, "play", "all", "pf", pf],
                       cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        print("[run] pf rc=%s（读数可能不可信）" % r.returncode)
        sys.stderr.write((r.stdout or "")[-2000:])
    # 🆕 `P3`：**第二套**读数（带 preset，见文件头）—— 只服务分派表点名的那些行。
    dump_pd = cache + "/all_dump_pd.tsv"
    pf_pd = cache + "/all_pf_pd.txt"
    print("[run] ruleprobe play all pf/dump preset dead=1,pick=1（第二套夹具）…")
    r = subprocess.run(["bash", PROBE, "play", "all", "pf", pf_pd, "dump", dump_pd,
                        "preset", "dead=1,pick=1"],
                       cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        print("[run] preset 扫描 rc=%s（第二套读数可能不可信）" % r.returncode)
        sys.stderr.write((r.stdout or "")[-2000:])
    # 🆕 `P3`：**第三套** —— 多一个 `chipenemy=1`（把 P1 的非督军单位各扣 2 血）。
    #   治的是「`a damaged enemy` / `a damaged friendly unit`」那一档：目标池要**受伤**的单位，
    #   而基准夹具里**敌人全是满血**。`chipenemy` 这个预置 `A2001` 就写好了，只是从来没有扫描带过它。
    dump_pd3 = cache + "/all_dump_pd3.tsv"
    pf_pd3 = cache + "/all_pf_pd3.txt"
    print("[run] ruleprobe play all pf/dump preset dead=1,pick=1,chipenemy=1（第三套夹具）…")
    r = subprocess.run(["bash", PROBE, "play", "all", "pf", pf_pd3, "dump", dump_pd3,
                        "preset", "dead=1,pick=1,chipenemy=1"],
                       cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        print("[run] preset(chip) 扫描 rc=%s（第三套读数可能不可信）" % r.returncode)
        sys.stderr.write((r.stdout or "")[-2000:])
    return dump, pf


def load_probe(cache):
    dump = cache + "/all_dump.tsv"
    pf = cache + "/all_pf.txt"
    main, bat = {}, {}
    if os.path.exists(dump):
        rows = [l.rstrip("\n").split("\t") for l in io.open(dump, encoding="utf-8")]
        hdr = rows[0]
        idx = {k: i for i, k in enumerate(hdr)}
        for r in rows[1:]:
            if len(r) > idx["id"]:
                main[r[idx["id"]]] = {"dump": r, "hidx": idx}
    pfmain = {}
    if os.path.exists(pf):
        for l in io.open(pf, encoding="utf-8"):
            if l.startswith("#"):
                continue
            r = l.rstrip("\n").split("\t")
            if not r or not r[0]:
                continue
            if r[0].startswith("E|"):
                bat[r[0][2:]] = r
            else:
                pfmain[r[0]] = r
    for k, v in pfmain.items():
        main.setdefault(k, {})["pf"] = v
    return main, bat


def load_probe_preset(cache, suffix="pd"):
    """🆕 `P3`：读**带 preset 的**那几套读数 —— 结构与 `load_probe` 逐字一致。

    `suffix = "pd"` = `preset dead=1,pick=1`（第二套）· `suffix = "pd3"` = 再多一个
    `chipenemy=1`（第三套，见 `run_probe`）。
    返回 `(main_pd, bat_pd)`；文件不在 ⇒ 两个空 dict（调用方会**出声**并退回基准读数）。
    """
    dump = cache + "/all_dump_" + suffix + ".tsv"
    pf = cache + "/all_pf_" + suffix + ".txt"
    main, bat = {}, {}
    if os.path.exists(dump):
        rows = [l.rstrip("\n").split("\t") for l in io.open(dump, encoding="utf-8")]
        hdr = rows[0]
        idx = {k: i for i, k in enumerate(hdr)}
        for r in rows[1:]:
            if len(r) > idx["id"]:
                main[r[idx["id"]]] = {"dump": r, "hidx": idx}
    if os.path.exists(pf):
        for l in io.open(pf, encoding="utf-8"):
            if l.startswith("#"):
                continue
            r = l.rstrip("\n").split("\t")
            if not r or not r[0]:
                continue
            if r[0].startswith("E|"):
                bat[r[0][2:]] = r
            else:
                main.setdefault(r[0], {})["pf"] = r
    return main, bat


def iter_rows(truth, engine_by_id):
    """逐 (卡 × ability 行) 展开，去重（同卡同 trigger 同族同载荷的重复条目只留一条）。"""
    out = []
    for c in truth["cards"]:
        e = engine_by_id.get(c["id"]) or {}
        # `"?"` = **池外卡**（`cards_engine.json` 里根本没有它）· `""` = 池内但 `type` 列为空
        ctype = e.get("type") or ("?" if not e else "")
        seen = set()
        for src in ("abilities", "activeAbility", "alternativeAbility"):
            v = c.get(src)
            items = v if isinstance(v, list) else ([v] if isinstance(v, dict) else [])
            for a in items:
                trig = a.get("trigger")
                for lg in a.get("logic") or []:
                    ca = lg.get("chosenAbility")
                    if ca is None:
                        continue
                    payload = json.dumps({k: val for k, val in lg.items() if k != "chosenAbility"},
                                         ensure_ascii=False, sort_keys=True)
                    key = (trig, ca, payload, src)
                    if key in seen:
                        continue
                    seen.add(key)
                    out.append({
                        "id": c["id"], "name": c["name"], "army": c.get("army"),
                        "ctype": ctype, "ca": ca, "trigger": trig, "src": src,
                        "lg": lg, "tc": lg.get("targetCriteria") or {},
                        "desc": e.get("desc") or "", "descZh": e.get("descZh") or "",
                        "faction": e.get("faction") or "",
                    })
    return out


def main():
    ap = argparse.ArgumentParser(description="B9 全池效果对拍（只读 ruleprobe 读数，出差异表）")
    ap.add_argument("--truth", default=DEF_TRUTH)
    ap.add_argument("--engine", default=DEF_ENGINE)
    ap.add_argument("--cache", default=DEF_CACHE)
    # ⚠️ `--out` 默认按「有没有 `--only`」分开：`--only` 落 `<主表>_c<族号>.tsv`，
    #   ⛔ 绝不用过滤后的结果覆盖全池主表（2026-10-26 实测踩过：一次 `--only 20,430` 把 2000 行主表压成 83 行）
    ap.add_argument("--out", default=None)
    ap.add_argument("--only", default="", help="只跑这些 chosenAbility（逗号分隔）")
    ap.add_argument("--run", action="store_true", help="先跑 ruleprobe 刷新读数（约 40 秒）")
    args = ap.parse_args()

    if args.run:
        run_probe(args.cache)

    if args.out is None:
        args.out = DEF_OUT if not args.only else             DEF_OUT[:-4] + "_c" + args.only.replace(",", "_") + ".tsv"

    truth = load_json(args.truth)
    eng = load_json(args.engine)
    engine_by_id = {c["id"]: c for c in eng["cards"]}
    name_of = {c["id"]: c["name"] for c in truth["cards"]}
    name_of.update({c["id"]: c["name"] for c in eng["cards"]})

    mainr, batr = load_probe(args.cache)
    if not mainr:
        print("!! 没读到读数（%s）—— 先跑 `--run` 或 `ruleprobe.sh play all pf`" % args.cache)
        return 2
    if not any((v.get("dump") is not None) for v in mainr.values()):
        # 🔴 没有 `all_dump.tsv` ⇒ 拿不到 `fixtureFit` 列 ⇒ `ephemeral`/`defence` 那两族的
        #   「结构上不适用」闸失效，会多出假缺陷。⛔ 不许静默降级。
        print("!! 缺 all_dump.tsv（%s）—— `fixtureFit` 闸失效，`ephemeral`/`defence` 可能被误判成缺陷" % args.cache)

    # 🆕 `P3`：**第二套/第三套读数**（带 preset，见文件头）。⛔ 缺了不许静默退回 ——
    #   退回会把「夹具口径」重新算成「未验到」，数字会悄悄回到改前那一个。
    mainr_pd, batr_pd = load_probe_preset(args.cache, "pd")
    mainr_pd3, batr_pd3 = load_probe_preset(args.cache, "pd3")
    HAVE_PD = bool(mainr_pd)
    HAVE_PD3 = bool(mainr_pd3)

    only = set(int(x) for x in args.only.split(",") if x.strip()) if args.only else None

    fsigs = follow_sigs_by_card(truth)      # 🆕 `P3`：320 那一族的期望签名（同卡 410 行）

    rows = iter_rows(truth, engine_by_id)
    if only:
        rows = [r for r in rows if r["ca"] in only]

    out = []
    stat = Counter()
    pd_rows = 0
    pd3_rows = 0
    pd_missing = 0
    for row in rows:
        _entry = mainr.get(row["id"]) or {}
        _d = _entry.get("dump")
        row["fit"] = ""
        if _d is not None:
            _ix = _entry["hidx"].get("fixtureFit")
            if _ix is not None and _ix < len(_d):
                row["fit"] = _d[_ix]
        m = _entry.get("pf")
        b = batr.get(row["id"])
        row["fsigs"] = fsigs.get(row["id"]) if row["ca"] == 320 else None
        row["pick_target"] = ""
        # 🆕 `P3` 分派：这一行要不要读**带 preset 的第二/第三套读数**（判据见 `use_preset` / `use_chip`）
        wake = use_preset(row)
        chip = use_chip(row)
        picked = False
        pick_none = False
        if chip or any(wake):
            if chip:
                ok = HAVE_PD3
                _pe = (mainr_pd3.get(row["id"]) or {}) if ok else {}
                _bb = batr_pd3 if ok else {}
            else:
                ok = HAVE_PD
                _pe = (mainr_pd.get(row["id"]) or {}) if ok else {}
                _bb = batr_pd if ok else {}
            if ok:
                m = _pe.get("pf")
                b = _bb.get(row["id"])
                pd_rows += 1
                if chip:
                    pd3_rows += 1
                if wake[1]:
                    _pd = _pe.get("dump")
                    _pi = _pe.get("hidx") or {}
                    _ix = _pi.get("pickTarget")
                    row["pick_target"] = (_pd[_ix] if (_pd is not None and _ix is not None
                                                       and _ix < len(_pd)) else "")
                    picked = bool(row["pick_target"])
                    pick_none = not picked
                    if pick_none:
                        # 目标池空 ⇒ 「点不到目标」仍然是**夹具**的限制，`pick` 那一支的闸要留着。
                        wake = (wake[0], False)
            else:
                pd_missing += 1
                wake = (False, False)
                chip = False
        verdict, exp, got, note = judge(row, m, b, name_of, row["ctype"], tuple(wake) + (chip,),
                                        picked=picked, pick_none=pick_none)
        fname, fsem, sigs, src = FAM.get(row["ca"], ("?", "?", [], ""))
        out.append([
            row["id"], row["name"], row["faction"], row["ca"],
            (row["desc"] or "").replace("\t", " ")[:80],
            exp, got, verdict, note,
            "" if row["trigger"] is None else row["trigger"],
            "activeAbility" if row["trigger"] is None else TRIG.get(row["trigger"], "?"),
            fname, row["ctype"], src,
            "是" if "候选缺陷" in note else "",
        ])

    # 🔴 同卡同 trigger 同族、连「期望/实得/判定」都一样的行 = 真重复（载荷看不出差别）
    #   ⇒ 只留一条。⛔ 不同载荷（例：`+1 近战` vs `+1 生命`）的「期望」不同，不会被并掉。
    _seen, _dedup = set(), []
    for r in out:
        k = tuple(r)
        if k in _seen:
            continue
        _seen.add(k)
        _dedup.append(r)
    dup = len(out) - len(_dedup)
    out = _dedup
    stat = Counter(r[7] for r in out)       # 🔴 必须在**去重之后**统计（否则数字加起来不等于行数）

    out.sort(key=lambda r: (r[3], r[7] != "错", r[7] != "未验到", r[1]))
    hdr = ["卡id", "卡名", "阵营", "chosenAbility", "卡面英文效果原文(截80字)",
           "期望", "实得", "判定", "备注",
           "trigger", "trigger名", "族名", "卡类型", "族语义判据", "候选缺陷"]
    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    with io.open(args.out, "w", encoding="utf-8", newline="\n") as f:
        f.write("\t".join(hdr) + "\n")
        for r in out:
            f.write("\t".join(str(x).replace("\t", " ").replace("\n", " ") for x in r) + "\n")

    print("=== B9 全池效果对拍 ===")
    print("  行数（卡×族×触发，逐字重复已并掉 %d 行）: %d" % (dup, len(out)))
    print("  判定: " + " · ".join("%s=%d" % (k, stat[k]) for k in ("对", "错", "未验到")))
    print("  其中「候选缺陷」（软闸，未判错）: %d" % sum(1 for r in out if r[-1] == "是"))
    # 🆕 `P3`：第二/第三套夹具（`preset …`）覆盖了多少行 —— ⛔ 缺读数必须出声。
    print("  读第二套夹具（preset dead=1,pick=1）的行: %d%s"
          % (pd_rows, "" if HAVE_PD else "  🔴 **第二套读数缺失** ⇒ 分派表回退了 %d 行（见 `--run`）" % pd_missing))
    print("    其中读第三套夹具（多一个 chipenemy=1）的行: %d%s"
          % (pd3_rows, "" if HAVE_PD3 else "  🔴 **第三套读数缺失** ⇒ 「damaged 目标」那一档仍按夹具闸处理"))
    print("  族数（本表覆盖）: %d / 池内 %d" % (len(set(r[3] for r in out)),
                                            len(set(r["ca"] for r in iter_rows(truth, engine_by_id)))))
    byfam = Counter((r[3], r[7]) for r in out)
    print("  判「错」的族: " + (", ".join("%s×%d" % (k[0], v) for k, v in byfam.items() if k[1] == "错") or "（无）"))
    print("  写出: " + args.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
