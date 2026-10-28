# -*- coding: utf-8 -*-
# B93_F2 账补丁生成器（只读主表、只写 账/_parts/B93_F2.tsv）
# 用法：python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B93_F2.py
# ⚠️ 裸 split("\t")，不引 csv（消费方 merge_ledger_parts.py 就是裸 split）。
import io, sys

BASE = "d:/4/Unity/资料/完全复刻/账/"
SRC = BASE + "战斗逻辑驱动.tsv"
OUT = BASE + "_parts/B93_F2.tsv"

src = io.open(SRC, "rb").read().decode("utf-8").split("\n")
hdr = src[0].split("\t")
assert len(hdr) == 11, len(hdr)

# 行号 => (新状态, 差异追加, 备注追加, 新落点 or None)
E = {}

E[74] = ("A",
 " ｜🆕 2026-10-28（B93_F2 · 文件所有者）：三条**全部落地** —— `RefCardOf`(partial:416) / `RefCardCostOf`(:428) / `ApplyRefCardValueOption`(:467) / 101 档 `RefCardBaseAttack`(partial:320，现读 = **无条件返回 minDamage**，旧「取 LastCreated 的 AttackBase」近似已按 2026-10-28 裁定删除) ⇒ 与 `AbilityLogic__GetValueFromCriteria.c:133-207` 逐句同形。休眠：100/110/120 池内 0 行、101 只 UM118 且原版 `referencedBaseCard` 恒 null ⇒ 机制齐、数据休眠（同 522 口径）⇒ A。",
 "", None)

E[131] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：本行 F2 侧**无代码可改** —— 原版 `displayFromHand(0x39)` 是队列动作 + 表现包装；我们借 `EvtKind.Draw` + 子标签 `display`，调度台 E4 族9 已裁「保持借用、⛔ 不新开 `EvtKind` 成员」⇒ 维持 B（口径层，不是实现缺口）。", None)

E[156] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：复核成立 —— 原版整条动作的唯一效果是清 `shouldCheckIfIsInCemetery(+0x24b)` 这道**异步重入闸**（`CardScript__RemoveCardFromCemetery.c` 全文 5 行）；我们同步结算没有「取出途中」状态 ⇒ 无方法级落点（不是漏做）。", None)

E[231] = ("B",
 " ｜🆕 2026-10-28（B93_F2）**已按调度台 E4 族10 裁定落地**：去掉 `ctx.SynapseBusy` 单层闸（原版无守卫 —— `BattleManagerSupport__BroadcastUnitSynapse.c:23-24` + `CardScript__TargetedSpellPlayed.c:49-72`），改成 `EnterSynapseGuard`/`ExitSynapseGuard` 的 **1000 层深度上限**（触顶**出声并停止**；🔶 自己设计·调度台 2026-10-28 裁定）。落点：`RepeatTacticOnAdjacent` = EffectResolver.cs:2885 · 守卫 = :18598 · `BroadcastKeywordEvent` = :10987。残差 = 池内递归可达性**未跑**（3 张带 synapse）。行为题 ⇒ 维持 B。",
 "", None)

E[254] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：`GetChoiceOptions` / `GetChoiceFullPool` 两档候选池（尤其 `matchType == 160` 的 `flag3` 支）**本轮未读** ⇒ 判据未闭合，维持 B（该项在 `BattleManager.cs`，不在本文件）。", None)

E[255] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：同 L254 —— 候选池两档 + 超时/取消回包两条**本轮未读** ⇒ 维持 B。落点 `DoChooseCard` = EffectResolver.cs:4792（现读未漂）。", None)

E[256] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：`BattleManager._ChooseCardMethod_d__449__MoveNext` 的等待/超时/取消三档**本轮未读** ⇒ 维持 B。落点 `ChooseCardCandidates` = EffectResolver.cs:4639（现读未漂）。", None)

E[436] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：落点 `IsLegalPlayTarget` 现读 = EffectResolver.cs:2211（未漂）；残差不变（教学局闸 `.c:30-35` 不可达 / `.c:61-65` 由 `AddSide`+`ApplyTargetFilters` 承担）⇒ 维持 B。", None)

E[493] = ("A",
 " ｜🆕 2026-10-28（B93_F2）**`.c` 全文（325 行）读完**：ResolveAddEffect 只有三句 —— ① `CardScript.AddEffect(target, effect, -1)`（addStats/addTrait 全族）② 若 `buffType(+0x38)==3` 且 `trait(+0x3c)==200 (poisoned)` ⇒ `BroadcastUnitPoisoned`（`.c:181` 判、`:268` 调，**落账之后**）③ `FinishResolvingAction`。我们的等价物 = `ApplyOneGain`（:11940 起）+ 唯一可达的毒生产者 `RuleCore.ResolveDamageDealtHook`（`poisonous` 关键词 → `ApplyTraitEffect(poisoned)` → `BroadcastUnitPoisoned`，RuleCore.cs:16152）；`cards_engine.json` 里 `poisoned` 载荷 **0 处** ⇒ 该广播今天只有那一个生产者，覆盖齐。③ 无对应物（原版是动作队列收尾，本版同步）。① 的 `ChallengeLogMgr.LogAddEffect` 计数面**未接**（跨面、落 `AchievementTally`）⇒ 记报告「顺手发现」，本行按可观察效果判 A。",
 "", None)

E[505] = ("A",
 " ｜🆕 2026-10-28（B93_F2）**`d__513`（131 行）读完**：三段 = ① `CardScript.CreateCardInGame`（表现幕）② `CardScript.MoveToDeck` ③ `BattleManager.AddCardInPlayToDeck` + `spellType==0x96(150)` ⇒ `BroadcastSecretCreated` + `FinishResolvingAction`。我们的等价物 = `DoCreate` 的 `todeck` 支（`InsertIntoDeckRandom` = 原版 `AddCardInPlayToDeck` 的「随机插入、不洗整副」，判据同 B92_B2b）+ `CreatePool.MatchesKind(pick,\"secret\")` ⇒ `CreatesSecret`（:5045 / :13306）。池内 `spellType 150` / `abilityTriggers` 含 150 的卡 **0 张** ⇒ 休眠 ⇒ A。",
 "", None)

E[506] = ("B",
 " ｜🆕 2026-10-28（B93_F2）**`d__512`（149 行）读完**：四段 = ① 造卡 ② `spellType==0xe6(230 sabotage)` ⇒ 信号 + `BroadcastSabotageCreated` + 中央展示 ③ `PlayerHand.AddCardNotDrawnToHand` ④ **无条件 `BroadcastCardCreated`**（外加一个可选 `AddEffect` 支）。我们已接 ② `CreatesSabotage`（:4219）与 ③ 手牌入区；**残差 = ④ 的通用 `BroadcastCardCreated`**（原版 trigger **210 CardCreatedInHand**）—— 本仓 `WhenEventKind` 里**没有**这一族、也没有发射口 ⇒ 真缺口（要动 `Core/WhenEvent.cs`，白名单外）。池内 `abilityTriggers` 含 210 的卡 **0 张** ⇒ 零可观测 ⇒ 维持 B。",
 "", None)

E[517] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：`EmitFollowUpResolution`（:7919）只发**表现层** `EvtKind.Trigger`；本版 410 正文不走 ability 结构 ⇒ 第二个参与方今天零可观测（与 B92_B8 一致）⇒ 维持 B（将来把 410 迁到 ability 结构路径时按 `FireTriggerAt` 第 6 形参传 `targetCard`）。", None)

E[521] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：复核 —— `CancelEffectsFromDisappearingUnit` 仍在 `ResolveJamUnit`（:16068）区块的 :16144；两处残差（`ActionPump` 的 ActionType 回填 / 两侧全量重算口）都在白名单外，池内 jam 生产者 0 ⇒ 维持 B。", None)

E[522] = ("A",
 " ｜🆕 2026-10-28（B93_F2）**逐档对表做完**（判据 `BattleManager__ProcessManaBuff.c:20-126` 全文）：原版四档 = `+0x1b spiritstone` ⇒ `DoFactionResource(kind=\"spirit\")`（:12838）· `+0x1c faith` ⇒ `kind=\"faith\"` · `+0x1d quest` ⇒ `kind=\"quest\"`（含「每 1 点广播一次」）· **否则能量** ⇒ `createMana(+0x16)` = `DoEnergy`（:12792，永久抬上限、当前能量不动）/ `refillMana(+0x17)` = `DoRefill`。**优先级同原版**（三档先判、能量最后）。两处有意保留的差异：① 原版 `amount < 0` 那一支会 `DestroyManaSlot`/`UseMana`（`.c:122-140`），我们**没有负量生产者**（卡面只有 `Gain`/`Refill`）⇒ 未做且无输入；② `amount == 0` 我们出声 + 记 `unresolved`（原版是 0 次循环、静默）。",
 "", "RuleEngine/Core/EffectResolver.cs:12838（DoFactionResource）· :12792（DoEnergy）· :12819（DoRefill）")

E[546] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：本轮未动 —— 残差三项（`UpdateFigures` 表现层 / 全残骸支逐跳复核 / `unstunnable` 池内 0）都不在本文件 ⇒ 维持 B。", None)

E[548] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：2,252 行**本轮仍未逐句核完** ⇒ 维持 B；落点 `DoBecome` 现读 :3407（未漂），`CheckStun` 等价位 = partial:527。", None)

E[637] = ("C", "",
 " ｜🆕 B93_F2 2026-10-28：维持 C —— 三样载具（`UnitState`/`PlayerState` 的状态位、移出区、表现层隐藏/碰撞）全在白名单外；本文件只能挂「广播等价物」，没有状态位就是半套。", None)

E[643] = ("A",
 " ｜🆕 2026-10-28（B93_F2）**已按原版改成两遍指名发射**：`EmitTurnStartBothPasses`（:18545）= 第 1 遍 `TurnStart(30)` **双方都发**、第 2 遍 `TurnStartPlayer(31)` **只发 owner == 行动方**（判据 `CardScript__OnTurnStart.c:137-146`：`:137` 无条件 30 → `:140` `isPlayerTurn != card.isPlayer ⇒ return` → `:144` 31）。改前「按卡二选一」⇒ 非行动方的 31 卡**也会**收到 31（比原版宽）。现读池内 30 = 5 张 / 31 = 8 张、交集 0。断言 = `RuleEngine/Editor/RuleEngineTest_EngineF2.cs` 的 `F2Case1TurnStartTwoPasses`（P0 收 1 次 / P1 收 0 次 / 30 双方都发）。",
 "", "RuleEngine/Core/EffectResolver.cs:8515（TurnTriggerNameOf）· :8601（EmitAtTurnTriggers）· :18545（EmitTurnStartBothPasses）")

E[647] = ("C", "",
 " ｜🆕 B93_F2 2026-10-28：维持 C —— 「攻击者必收一次」要 `DamageInfo.damageType == Attack(4)` 进广播载荷（`WhenEvent` 无此列，F4 文件）；`PreCallBoardListener` 现读 :10440（未漂）。", None)

E[655] = ("A",
 " ｜🆕 2026-10-28（B93_F2）**唯一差异已消除**：原版棋盘那一跳**没有 `IsInPlay`** —— `decomp_full/BattleManagerSupport__BroadcastUnitObtainedHuntMark.c` 全文 `IsInPlay` = **0**，循环体只有 `op_Inequality(u, param_2)` 自排除（同族的 `…ObtainedMarkerlight.c` / `…ObtainedMark.c` 也是 0 命中）⇒ 已把 `GetsHuntMark` / `GetsMarkerlight` 加进 `BoardJumpIncludesPendingDeath`（:16422，`GetsDarkPact` 早就在表里）。",
 "", "RuleEngine/Core/EffectResolver.cs:10987（BroadcastKeywordEvent）· :16422（BoardJumpIncludesPendingDeath）")

E[686] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：本轮未动（残差 3 张 `GOF56`/`GOF91`/`SW56` 的挂载端在 `RuleCore.PlayCard` / 在-play 触发；附魔模板 ability 派发在 `CardDatabase`/`Enchantment` —— 全在白名单外）⇒ 维持 B。", None)

E[740] = ("A",
 " ｜🆕 2026-10-28（B93_F2）**第 3/3 句核完**（全文 59 行）：`ExecuteSynapeEffect(this, actingCard)` = 取 `this.synapseCard(+0x288)`（空 ⇒ return）→ `GetAdjacentUnits(mgr, this)` → 逐邻过 **synapseCard 自己的**目标判定虚槽（`(*raw+0x318)`）→ 命中则 `OnCardPlayedWithTargetJustThisCardPlayed(synapseCard, mgr, actingCard, actingCard, 邻)`。我们的 `RepeatTacticOnAdjacent`（:2885）= 同一套：锚点 = 被选中那格、候选 = `BoardSpec.AdjacentSlots`、逐邻过 `IsLegalPick`（同一份目标规格）、命中者以它自己为 `chosen` 重跑该战术 ops ⇒ 筛出的邻与重放内容一致，只有**入口名**不同。",
 "", None)

E[830] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：发送侧调用点（幸存者写满血）在 `RuleCore.CleanupDeaths`（F1 文件）⇒ 本文件侧已备好广播器、无可改；池内 trigger 70 = 0 ⇒ 维持 B。", None)

E[832] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：`CancelEffectsFromDisappearingUnit` 已在 :16144 区块接上（判据 `.c:263`）；残差（ActionPump 豁免档 / 两侧全量重算口 / 池内 0）不变 ⇒ 维持 B。", None)

E[853] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：统一入口 `RemoveEffectRecordFrom` 现读 :18082（未漂）；残差（调用点切换 = 改动请求 · case 12 的 quest 支在 `Enchantment.cs` 且池内 `spellType==120` = 0）不变。", None)

E[947] = ("B",
 " ｜🆕 2026-10-28（B93_F2）复核四个调用点：`abilityFamilyCard:` 全仓只出现 **2 处**（EffectResolver.cs:2774 战术正文 / :2910 突触重放）⇒ 原版四跳里**只有 PlayAbility 那一跳**在本文件落地；`ApplyEnvEffect` 那一跳由 2026-10-28 `E25` 的 `Enchantments.RunGrantedEnvAbility` 新接（另一条路、不走族闸）；`ResolveSummonUnit` / `StartBattlePhase` 两跳**我们这边没有对应入口** ⇒ 维持 B。",
 "", None)

E[958] = ("A",
 " ｜🆕 2026-10-28（B93_F2）**那道专门闸已补**：`SpiritBroadcastGate`（:18570）= 原版 `CardScript__TriggerSpiritStone.c:21-25` 的 `HasAbilityType(600)`，落在广播**之前**（次序同原版：先发 600 那一拍、再判）。`AbilityTriggerPresence(600)`：`1` 放行 · `0` **不广播并出声** · `-1`（旧表）放行（不拿未知当「没有」）。现读池内 `abilityTriggers` 含 600 的 **29 张** ⊇ 卡面带 `[Spirit Stone]` 的 **28 张**（多的一张 `ASH52` 走免付、`SpiritOps` 为空）⇒ 今天这张闸不会拦任何一张（休眠、口径对齐）。",
 "", "RuleEngine/Core/EffectResolver.cs:14307（ConditionHolds）· :16478（BroadcastUnitSpiritStone）· :18570（SpiritBroadcastGate）")

E[1122] = ("C", "",
 " ｜🆕 B93_F2 2026-10-28：③ **仍未做** —— `DoDeploy` 在本文件（可改），但 `RuleCore.DeployFreeCore(requestSlot:)` 的形参在 `RuleCore.cs`（F1）⇒ 半套不能落。**施工单**：① `EffectResolver.DoDeploy` 读 `op.CreateAtPos`，只认 `{0 self, 1 cardTarget, 3 friendlyHero}` 三档（其余含池内 45 行 `14 nextFriendlyMinionSpot` ⇒ 出声 + 默认落点），判据 = `判据/B91_P3_战斗数据列缺口.md` §二·1；② 经 `NextSlotToCard`（partial:255）算格；③ 传给 `DeployFreeCore` 的 `requestSlot`。", None)

E[1278] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：落点 `RunAbilityOfAnotherCard` 现读 :17994（未漂）；2026-10-28 `E25` 已给它接上「正文解析不出来」那一支（`Enchantments.RunGrantedEnvAbility`，18002-18010 九行原地换九行）⇒ 原版 5 个调用点里 `ApplyEnvEffect` 那一跳已有消费者；其余 4 跳仍在 `RuleCore`/`BattleDriver` ⇒ 维持 B。", None)

E[1288] = ("B", "",
 " ｜🆕 B93_F2 2026-10-28：`FireTriggerOnce` 现读 :18442（未漂）；残差不变 —— 「触发数值 → ability → op」那张表要在生成器 + `CardDef` 的 ability 归位里补（白名单外）；池内 `abilityTriggers` 含 200 的卡 **0 张** ⇒ 今天恒返 false。", None)

NUM = [74,131,156,231,254,255,256,436,493,505,506,517,521,522,546,548,637,643,647,655,686,740,830,832,853,947,958,1122,1278,1288]
assert set(NUM) == set(E.keys()), (set(NUM) ^ set(E.keys()))

out = ["\t".join(hdr)]
checks = []
for n in NUM:
    cells = src[n-1].split("\t")
    assert len(cells) == 11, (n, len(cells))
    old = list(cells)
    st, diffAdd, noteAdd, landNew = E[n]
    cells[5] = st
    if landNew: cells[6] = landNew
    if diffAdd: cells[7] = cells[7] + diffAdd
    if "B93_F2" not in cells[9]: cells[9] = cells[9] + "｜B93_F2"
    cells[10] = cells[10] + noteAdd
    # 未动列必须逐字相同
    moved = [i for i in range(11) if cells[i] != old[i]]
    assert len(moved) > 0, (n, "一行都没改")
    checks.append((n, moved))
    out.append("\t".join(cells))

io.open(OUT, "wb").write(("\n".join(out) + "\n").encode("utf-8"))

# 回环比对：重读补丁，逐列 11 列，且第 0/1/2/3/4/8 列与主表逐字相同
back = io.open(OUT, "rb").read().decode("utf-8").split("\n")
while back and back[-1] == "": back.pop()
assert back[0].split("\t") == hdr, "表头不是 11 列"
assert len(back) - 1 == len(NUM), (len(back), len(NUM))
for i, n in enumerate(NUM):
    c = back[i+1].split("\t")
    assert len(c) == 11, (n, len(c))
    o = src[n-1].split("\t")
    for col in (0,1,2,3,4,8):
        assert c[col] == o[col], (n, col, c[col], o[col])
print("OK rows=%d  (改到的列: %s)" % (len(NUM), checks))
