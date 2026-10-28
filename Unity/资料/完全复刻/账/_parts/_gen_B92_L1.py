#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B92_L1：把「失败 / 漂移」的 A 行落点改对（只改 `我们的落点`；必要时降 B），产出 _parts/B92_L1.tsv。

只读主表，只写补丁。⛔ 不动 .cs、不动主表。
"""
import glob
import io
import os
import sys

LEDGER = r"D:/4/Unity/资料/完全复刻/账"
OUT = os.path.join(LEDGER, "_parts", "B92_L1.tsv")
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

# ---- 全仓共同的落点（可复用）----
TALLY = "RuleEngine/Core/BattleContext.cs:348（Tally）"

# key = (账文件名, who)，who = 「原版类.方法」（与 check_ledger_points 的口径完全一致）
# value = (新落点, 新状态 or None)
MAP = {
    # ===================== 其他_基础设施.tsv =====================
    ("其他_基础设施.tsv", "Extensions.Contains(int[] target, int nr)"): (
        "CardPresentation/Battle/HudCanvas.cs:627（IsRanked）· 本仓无同名扩展，等价实现 = 各调用点内联 "
        "`System.Array.IndexOf(...) >= 0`（`IsRanked` 体内是代表点）", None),
    ("其他_基础设施.tsv", "GameStaticData.MinionRaceToString(RawCardScript card, Keyword race)"): (
        "CardPresentation/Core/CardView.cs:5401（RaceTerm）· 本方法对应原版三支里的第 2 支"
        "（前缀 + 种族串，见 差异 列）", None),
    ("其他_基础设施.tsv", "GameStaticData.StratagemTypeToString(RawCardScript card)"): (
        "CardPresentation/Core/CardView.cs:5401（RaceTerm）· 本方法对应原版三支里的第 3 支；"
        "SpellType 六写法映射 = 同文件 RaceKeyBySubtype", None),
    ("其他_基础设施.tsv", "LoginManager.IsActivatedLanguage(string currentLanguage)"): (
        "Core/Loc.cs:188（HasOwnText）· 「这个语言有没有自己的文案」= 原版「在启用清单里吗」的成员测试；"
        "存档值合法性那道闸在 Core/Loc.cs 的 Load", None),
    ("其他_基础设施.tsv", "PlayerDataManager.IsTutorialIndexCompleted(int tutorialIndex)"): (
        "CardPresentation/Shell/MenuTutorialFlow.cs:72（IsCompleted）", None),

    # ===================== 战斗界面_HUD.tsv =====================
    ("战斗界面_HUD.tsv", "BattleManager.ShowPotentialTargets(CardScript actingCard, TargetingReason targetingReason)"): (
        "CardPresentation/Battle/BattleDriver.cs:15838（HighlightTargetsCore）· 合法目标 → "
        "`CardHighlightState.ValidTarget`（= 原版 `potentialTarget*` 档）高亮 + 准星", None),

    # ===================== 战斗逻辑驱动.tsv（失败） =====================
    ("战斗逻辑驱动.tsv", "BattleManager.BroadcastSacrificeResolved(CardScript unitSacrifice)"): (
        "RuleEngine/Core/RuleCore.cs:12906（FireTriggerAlways）· RuleEngine/Core/EffectResolver.cs"
        "（triggers:sacrifice → OtherCardSacrifice(225)）", None),
    ("战斗逻辑驱动.tsv", "BattleManager.ShouldActivatInitialEnchantments()"): (
        "RuleEngine/Core/RuleCore.cs:8204（SetupInitialEnchantments）· 门 = 同方法首句 "
        "`ctx.MatchType ∈ {PracticeOffline, EventAI}`", None),
    ("战斗逻辑驱动.tsv", "BattleManagerSupport.BroadcastSabotageCreated(BattleManager manager, CardScript unitCollected)"): (
        "RuleEngine/Core/EffectResolver.cs:18168（BroadcastCreatedCardWithWarlordPreCall）· 调用点 = "
        "CreatesSabotage 支 · 映射 = CreatesSabotage → OnSabotageCreated(701)", None),
    ("战斗逻辑驱动.tsv", "BattleManagerSupport.BroadcastSecretCreated(BattleManager manager, CardScript unitCollected)"): (
        "RuleEngine/Core/EffectResolver.cs:18168（BroadcastCreatedCardWithWarlordPreCall）· 映射 = "
        "CreatesSecret → OnSecretCreated(700) · Signal 那半的等价位 = RuleEngine/Core/AchievementTally.cs "
        "的 AddSecret", None),
    ("战斗逻辑驱动.tsv", "CardScript.CardWaitingToBePlayed()"): (
        "RuleEngine/Core/PlayerState.cs:20（Hand）· 我们没有 `cardState` 状态机，这一格由「这张牌在不在 "
        "`PlayerState.Hand` 列表里」结构性地表达", None),
    ("战斗逻辑驱动.tsv", "CardScript.SetupActiveAbility()"): (
        "RuleEngine/Core/RuleCore.cs:11587（UseAbilityCore）· `try/finally` 里 `MarkCardState(inPlayAttacking)` "
        "→ 复位（窗口 = 一次主动技能结算）", None),
    ("战斗逻辑驱动.tsv", "CardScript.SetupAttack(bool playSound, bool playerInitiated)"): (
        "RuleEngine/Core/RuleCore.cs:6282（DeclareAttackCore）· `try/finally` 里 "
        "`MarkCardState(inPlayAminingAttack)` → 复位；表现侧 = CardPresentation/Battle/AttackSelector.cs", None),
    ("战斗逻辑驱动.tsv", "CardScript.ShowDropPodFigures()"): (
        "RuleEngine/Core/RuleCore.cs:16311（ShowDropPodFigures）· 消费者 = 同文件回合开始自动开舱的逐卡循环", None),
    ("战斗逻辑驱动.tsv", "CardScript.TriggerCodex(CardScript targetCard)"): (
        "RuleEngine/Core/RuleCore.cs:13268（CheckCodex）· 体内 `FireTriggerAt(..., KeywordTable.Codex, "
        "chosen: u)` = 原版 `OnTrigger(0x91, …)` 的第 6 参", None),
    ("战斗逻辑驱动.tsv", "CardTraitCollection.GetTrait(DefinedTrait traitName)"): (
        "CardPresentation/Core/Badges.cs:570（For）· 我们按关键词名取 trait 徽标/图形（原版按 "
        "`DefinedTrait` 枚举取 `CardTrait` 资产；本仓无该枚举，见 AbilityIconAnim 文件头 ④）", None),
    ("战斗逻辑驱动.tsv", "DamageInfo.IsPlayer()"): (
        "RuleEngine/Core/RuleCore.cs:8453（ApplyDamage）· 我们没有 DamageInfo DTO：「这条伤害属于哪一方」"
        "由 ApplyDamage 的目标单位/发起侧显式携带", None),
    ("战斗逻辑驱动.tsv", "MinionManager.AreMinionsWithAmbush()"): (
        "CardPresentation/Battle/BattleDriver.cs:36412（AmbushHidesEnergy）· 读口 = 同文件 EnergyTextOf；"
        "类尾另有 NoteAmbushObserved / NoteAmbushEntered / NoteAmbushExited / AreMinionsWithAmbush / "
        "EnsureAmbushStateForBattle", None),
    ("战斗逻辑驱动.tsv", "RawCardScript.IsValidTarget(CardScript thisCard, CardScript targetCard)"): (
        "RuleEngine/Core/RuleCore.cs:11386（CanUseAbilityTarget）· RuleEngine/Core/RuleCore.cs"
        "（IsValidTarget，攻击侧同族闸）· RuleEngine/Core/EffectResolver.cs（camouflage 闸）", None),
    ("战斗逻辑驱动.tsv", "BattleManagerSupport.BroadcastSacrificeResolved(CardScript unitSacrifice)"): (
        "RuleEngine/Core/RuleCore.cs:12540（FireTriggerAt）· `includePendingDeath: keyword == "
        "KeywordTable.Sacrifice`；跳过待死的闸 = RuleEngine/Core/EffectResolver.cs 的 SnapshotBoardListeners "
        "（`!includePendingDeath && ctx.IsPendingDeath(u)`）", None),

    # ===================== 战斗逻辑驱动.tsv（漂移 / 歧义） =====================
    ("战斗逻辑驱动.tsv", "AI.CombatAttackWithType(CardScript selectedCard, AttackTypes attackType)"): (
        "RuleEngine/Core/RuleCore.cs:5009（FieldAttack）· 等价实现 = FieldAttack；原版第三档"
        "「其它 attackType ⇒ currentCombatAttack」落在 RuleEngine/Core/CounterAttack.cs 的 CurrentCombatAttack", None),
    ("战斗逻辑驱动.tsv", "AI.CombatAttackWithType(CardScript selectedCard, PlayerActions actionType)"): (
        "RuleEngine/Core/RuleCore.cs:5009（FieldAttack）· 与上一行同体（形参类型换成 PlayerActions，"
        "Ghidra 只落了一份体）", None),
    ("战斗逻辑驱动.tsv", "BattleManager.AddTriggerUprising(CardScript targetCard, CardScript actingCard, int priority, bool blockPlayerActions)"): (
        "RuleEngine/Core/RuleCore.cs:13420（FireTriggerOnSide，4 参真实现）· 入队 triggerUprising(84) → "
        "出队 CardScript__TriggerUprising；`requireDeployedThisTurn: true` 是这一族的那道闸", None),
    ("战斗逻辑驱动.tsv", "BattleManager.BroadcastTacticPlayed(CardScript tacticard, CardScript targetCard)"): (
        "RuleEngine/Core/RuleCore.cs:13420（FireTriggerOnSide，4 参真实现）· 等价传 KeywordTable.Artifice："
        "先按进场序快照带该词的场上单位，再逐个 FireTriggerAt", None),
    ("战斗逻辑驱动.tsv", "BattleManager.ResolveSummonUnit(BattleAction battleAction)"): (
        "RuleEngine/Core/RuleCore.cs:4759（DeployFree）· CardInstance 重载· 出队 summonUnit → SummonMinion"
        "（不付费）；实际体在 DeployFreeCore（同文件）", None),
    ("战斗逻辑驱动.tsv", "BattleManager.ResolveTriggerUprising(BattleAction battleAction)"): (
        "RuleEngine/Core/RuleCore.cs:13420（FireTriggerOnSide，4 参真实现）· "
        "`requireDeployedThisTurn: true` = 原版 `ResolveUnitSummoned.c:82-105` 的四条件", None),
    ("战斗逻辑驱动.tsv", "CardScript.CanUseAbilityNow()"): (
        "RuleEngine/Core/RuleCore.cs:11198（CanUseAbility）· 带 out targetOwner 的那一版· 同一组闸"
        "（CanStartAbility 三档 + targetSlot 另判技能目标）；带默认参的那个重载只是转发口", None),
    ("战斗逻辑驱动.tsv", "CardScript.CheckAndApplyCurrentPack()"): (
        "RuleEngine/Core/RuleCore.cs:5009（FieldAttack）· pack 支在同方法内：`if (!u.IsWarlord) n--;` ⇒ "
        "督军取 Count、部队取 Count-1", None),
    ("战斗逻辑驱动.tsv", "CardScript.HasPackBuffTrait()"): (
        "RuleEngine/Core/RuleCore.cs:5009（FieldAttack）· 原版该方法本身是死代码（三源合判）；语义在 "
        "FieldAttack 的 `Has(pack)` 闸 + 只在场单位调用", None),
    ("战斗逻辑驱动.tsv", "CardScript.TriggerUprising()"): (
        "RuleEngine/Core/RuleCore.cs:13420（FireTriggerOnSide，4 参真实现）· 调用点 = 同文件 "
        "`FireTriggerOnSide(..., KeywordTable.Uprising, requireDeployedThisTurn: true)`；单卡那一跳 = "
        "RuleEngine/Core/RuleCore.cs 的 FireTriggerAt", None),
    ("战斗逻辑驱动.tsv", "MinionManager.PlayMinionFromHand(CardScript card, int slotNum, float timeToMove, bool isFromTeleport)"): (
        "RuleEngine/Core/RuleCore.cs:3900（PlayCard）· 带 out landedSlot 的那一版· 扣费 → BoardSlots.Insert "
        "→ 部署广播一次；无 out 的两个重载只是转发口", None),
    ("战斗逻辑驱动.tsv", "MinionManager.SummonMinion(CardScript card, int summonSlot, float timeToMove, bool doScaleAnim)"): (
        "RuleEngine/Core/RuleCore.cs:4759（DeployFree）· CardInstance 重载· 不付费插入 + 部署触发 + 缩放动画"
        "（动画由表现层播）", None),
    ("战斗逻辑驱动.tsv", "RawCardScript.OnCardPlayedWithTarget(BattleManager manager, CardScript thisCard, CardScript cardPlayed, CardScript targetCard)"): (
        "RuleEngine/Core/RuleCore.cs:3900（PlayCard）· 带 out landedSlot 的那一版· "
        "RuleEngine/Core/EffectResolver.cs（PlayTacticBodyDeferred）· "
        "RuleEngine/Core/PlayedThirdCardInTurn.cs（FirePlayedThirdCardInTurn）· "
        "RuleEngine/Core/Enchantment.cs（OnCarrierPlayed）", None),
    ("战斗逻辑驱动.tsv", "SupportMethods.ShuffleCardList(List<CardScript> deck, MatchData matchData, bool checkStratagem)"): (
        "RuleEngine/Core/RuleCore.cs:15098（ShuffleByRandomOrder）· 泛型方法；我们是就地按名次重排，"
        "没有钉位支", None),
    ("战斗逻辑驱动.tsv", "SupportMethods.ShuffleRawCardList(List<RawCardScript> deck, MatchData matchData)"): (
        "RuleEngine/Core/RuleCore.cs:15098（ShuffleByRandomOrder）· 泛型方法；就地按名次重排", None),

    # ===================== 教程_引导.tsv =====================
    ("教程_引导.tsv", "BattleTutorialManager.Initialize()"): (
        "无对应物 —— 原版 `Initialize` 全文只有两个单例字段缓存（`+0x38` BattleManager / `+0x40` "
        "PlayerDataManager）、零行为；本仓 `BattleDriver` 本身即 BattleManager、`Ctx` 可直接用 ⇒ 不需要再缓存",
        "B"),

    # ===================== 日常_奖励.tsv =====================
    ("日常_奖励.tsv", "DailyLoginChallenge.Execute()"): (
        "CardPresentation/Shell/MissionEvent.cs:747（Execute）", None),
    ("日常_奖励.tsv", "DailyLoginChallenge.GetNextUpdate(ChallengeProgress progress)"): (
        "CardPresentation/Shell/MissionEvent.cs:939（GetNextUpdate）", None),
    ("日常_奖励.tsv", "DailyLoginChallenge.OnInitialize()"): (
        "CardPresentation/Shell/MissionEvent.cs:750（OnInitialize）", None),
    ("日常_奖励.tsv", "EventBadge.Awake()"): (
        "CardPresentation/Shell/EventBadge.cs:80（Awake）· 等价实现 = SetFill(0)（`ImageQuad.SetUvRect(0,0,0,1)`）", None),
    ("日常_奖励.tsv", "EventBadge.Initialize(ILiveOps liveOps)"): (
        "CardPresentation/Shell/EventBadge.cs:99（Initialize）· `ToastNotifications.RegisterNotificationHandler` "
        "+ 点击监听", None),
    ("日常_奖励.tsv", "MissionChallenge.GetID()"): (
        "CardPresentation/Shell/MissionEvent.cs:545（GetID）", None),
    ("日常_奖励.tsv", "MissionChallenge.GetLocalizedDescription()"): (
        "CardPresentation/Shell/MissionEvent.cs:913（GetLocalizedDescription）", None),
    ("日常_奖励.tsv", "MissionChallenge.Initialize()"): (
        "CardPresentation/Shell/MissionEvent.cs:70（Initialize）", None),
    ("日常_奖励.tsv", "MissionChallenge.UpdateProgress(Action<ChallengeProgress> updateFunction)"): (
        "CardPresentation/Shell/MissionEvent.cs:684（UpdateProgress）· Action 重载", None),
    ("日常_奖励.tsv", "MissionChallenge.UpdateProgress(Func<ChallengeProgress, bool> updateFunction)"): (
        "CardPresentation/Shell/MissionEvent.cs:660（UpdateProgress）· Func 重载", None),
    ("日常_奖励.tsv", "MissionChallenge.ValidateMatchConditions()"): (
        "CardPresentation/Shell/MissionEvent.cs:633（ValidateMatchConditions）", None),
    ("日常_奖励.tsv", "MissionChallengeProgress.GetLocalizedDescription()"): (
        "CardPresentation/Shell/MissionEvent.cs:1070（GetLocalizedDescription）", None),
    ("日常_奖励.tsv", "MissionChallengeProgress.IsComplete()"): (
        "CardPresentation/Shell/MissionEvent.cs:1048（IsCompleteCore）", None),
    ("日常_奖励.tsv", "MissionComplete.OnMissionComplete(MissionCompleteSignal obj)"): (
        "CardPresentation/Shell/MissionEvent.cs:875（OnMissionComplete）", None),
    ("日常_奖励.tsv", "MissionEvent.GetProgressFor(MissionChallenge challenge)"): (
        "CardPresentation/Shell/MissionEvent.cs:430（GetProgressFor）", None),
    ("日常_奖励.tsv", "MissionEvent.InitializeChallenge(MissionChallenge challenge)"): (
        "CardPresentation/Shell/MissionEvent.cs:459（InitializeChallenge）", None),
    ("日常_奖励.tsv", "MissionEvent.IsActive(MissionChallenge challenge)"): (
        "CardPresentation/Shell/MissionEvent.cs:404（IsActive）", None),
    ("日常_奖励.tsv", "MissionEvent.LocalizedDescription(MissionChallenge challenge)"): (
        "CardPresentation/Shell/MissionEvent.cs:486（LocalizedDescription）", None),
    ("日常_奖励.tsv", "MissionEvent.UpdateSaveGame(LiveOpsEventPersistence save)"): (
        "CardPresentation/Shell/MissionEvent.cs:274（UpdateSaveGame）", None),
    ("日常_奖励.tsv", "MissionPersistence.IsChallengeComplete(MissionChallenge challenge)"): (
        "CardPresentation/Shell/MissionEvent.cs:148（IsChallengeComplete）", None),
    ("日常_奖励.tsv", "RewardCollected.OnInitialize()"): (
        "CardPresentation/Shell/RewardCollected.cs:92（OnInitialize）", None),
    ("日常_奖励.tsv", "RewardCollected._003COnInitialize_003Eb__6_0(IIdentifiable x)"): (
        "CardPresentation/Shell/RewardCollected.cs:195（ArmyPasses）· 原版 `OnInitialize` 里那个 lambda 的等价物", None),
    ("日常_奖励.tsv", "RewardInfo.CanCollect()"): (
        "CardPresentation/Shell/RewardInfo.cs:83（CanCollect）", None),
    ("日常_奖励.tsv", "RewardService.Preview(IReadOnlyList<RewardInfo> rewards, Action<IReadOnlyList<RewardInfo>> onClose, IReadOnlyList<RewardInfo> collectedRewards, bool isPremiumLocked, string customTitle)"): (
        "CardPresentation/Shell/RewardWindow.cs:1577（Preview）", None),
    ("日常_奖励.tsv", "TimerDisplay.Awake()"): (
        "CardPresentation/Shell/TimerDisplay.cs:287（Awake）", None),
    ("日常_奖励.tsv", "TimerDisplay.Initialize(DateTime target, string timerDescription)"): (
        "CardPresentation/Shell/TimerDisplay.cs:319（Initialize）", None),
    ("日常_奖励.tsv", "TimerDisplay.LocalizationChange()"): (
        "CardPresentation/Shell/TimerDisplay.cs:280（LocalizationChange）", None),
    ("日常_奖励.tsv", "TimerDisplay.OnDestroy()"): (
        "CardPresentation/Shell/TimerDisplay.cs:288（OnDestroy）", None),
    ("日常_奖励.tsv", "TimerDisplay.SetTimerDescriptionText(string timerDescription)"): (
        "CardPresentation/Shell/TimerDisplay.cs:337（SetTimerDescriptionText）", None),
    ("日常_奖励.tsv", "TimerDisplay.UpdateTimer()"): (
        "CardPresentation/Shell/TimerDisplay.cs:366（UpdateTimer）", None),

    # ===================== 未分类_其他.tsv =====================
    ("未分类_其他.tsv", "CardPlayedCount.OnReset()"): (
        TALLY + " · Tally 每局一份 ⇒ 「重置」= 新局新 Tally（与 OnInitialize 的写/读点同一条）", None),
    ("未分类_其他.tsv", "CollectFaith.OnInitialize()"): (
        "CardPresentation/Shell/ProfileData.cs:635（ApplyAchievementTally）· `case CollectFaith → faithGained`，"
        "形参由结算口传", None),
    ("未分类_其他.tsv", "CollectFaith.OnReset()"): (TALLY, None),
    ("未分类_其他.tsv", "Decorator.Awake()"): (
        "CardPresentation/Shell/Decorator.cs:48（Awake）· 唯一子类行为的内联等价物 = "
        "CardPresentation/Shell/CampaignTab.cs 的 OnArmyAmountChanged", None),
    ("未分类_其他.tsv", "Decorator.OnDecorableUpdated(object sender, EventArgs e)"): (
        "CardPresentation/Shell/Decorator.cs:71（OnDecorableUpdated）· 唯一子类行为的内联等价物 = "
        "CardPresentation/Shell/CampaignTab.cs 的 OnArmyAmountChanged", None),
    ("未分类_其他.tsv", "Decorator.OnDisable()"): (
        "CardPresentation/Shell/Decorator.cs:61（OnDisable）· 唯一子类行为的内联等价物 = "
        "CardPresentation/Shell/CampaignTab.cs 的 OnArmyAmountChanged", None),
    ("未分类_其他.tsv", "Decorator.OnEnable()"): (
        "CardPresentation/Shell/Decorator.cs:54（OnEnable）· 唯一子类行为的内联等价物 = "
        "CardPresentation/Shell/CampaignTab.cs 的 OnArmyAmountChanged", None),
    ("未分类_其他.tsv", "GamesPlayed.OnInitialize()"): (
        "CardPresentation/Shell/ProfileData.cs:785（GamesPlayedDelta）· 读点 = 同文件 ApplyAchievementTally"
        "（case GamesPlayed）", None),
    ("未分类_其他.tsv", "GamesPlayed.OnReset()"): (TALLY, None),
    ("未分类_其他.tsv", "GiveHuntsMark.OnInitialize()"): (
        "RuleEngine/Core/AchievementTally.cs:237（AddHuntMark）· 写点 = RuleEngine/Core/EffectResolver.cs"
        "（ApplyOneGain 的 huntmark 授予点）· 读点 = Shell/ProfileData.cs 的 ApplyAchievementTally", None),
    ("未分类_其他.tsv", "GiveHuntsMark.OnReset()"): (TALLY, None),
    ("未分类_其他.tsv", "GiveMarkOfChaos.OnInitialize()"): (
        "RuleEngine/Core/AchievementTally.cs:243（AddMarkOfChaos）· 写点 = RuleEngine/Core/EffectResolver.cs"
        "（GrantDarkPactCore 的 AddKeyword(DarkPact,1)）· 读点 = Shell/ProfileData.cs 的 ApplyAchievementTally", None),
    ("未分类_其他.tsv", "GiveMarkOfChaos.OnReset()"): (TALLY, None),
    ("未分类_其他.tsv", "ListExtensions.RandomElement<T>(ICollection<T> coll)"): (
        "CardPresentation/Shell/BoosterPackOpenWindow.cs:1220（RollPack）· 各调用点自己的取样口"
        "（引擎侧走 ctx.Rng、非引擎侧 System.Random(seed)），无同名扩展", None),
    ("未分类_其他.tsv", "ListExtensions.Shuffle<T>(IList<T> list)"): (
        "RuleEngine/Core/RuleCore.cs:15098（ShuffleByRandomOrder）· 引擎侧洗牌的唯一实现"
        "（`RuleCore.Shuffle<T>` 只是转发口）；非引擎侧 = CardPresentation/Shell/BoosterPackOpenWindow.cs 的 RollPack", None),
    ("未分类_其他.tsv", "ListExtensions.SpliceWhere<T>(IList<T> list, Func<T, bool> predicate, int count)"): (
        "无对应物 —— 原版通用扩展；本仓等价 = 各调用点内联 `List.RemoveAll` / LINQ"
        "（`Shell/BaseOfferPopup` 的奖励绘制链等处），无单点方法级对应", "B"),
    ("未分类_其他.tsv", "ListExtensions.GetMax<T, TU>(IEnumerable<T> data, Func<T, TU> f)"): (
        "无对应物 —— 原版通用扩展；本仓等价 = 各调用点 `Select(...).Max()` / 手写比较"
        "（`Shell/DailyData` 的 Target 等处），无单点方法级对应", "B"),
    ("未分类_其他.tsv", "ListExtensions.GetMin<T, TU>(IEnumerable<T> data, Func<T, TU> f)"): (
        "无对应物 —— 原版通用扩展；本仓等价 = 各调用点 `Select(...).Min()` / 手写比较"
        "（`Shell/LiveOpsEventWindow` / `RankedRewardEventWindow` 的 boost 段），无单点方法级对应", "B"),
    ("未分类_其他.tsv", "NameGenerator.BuildMarkovChain()"): (
        "CardPresentation/Shell/NameGenerator.cs:1573（BuildMarkovChain）", None),
    ("未分类_其他.tsv", "NameGenerator.GenerateName(int minLength, int maxLength)"): (
        "CardPresentation/Shell/NameGenerator.cs:1632（GenerateName）", None),
    ("未分类_其他.tsv", "NameGenerator.GenerateNameOnce()"): (
        "CardPresentation/Shell/NameGenerator.cs:1591（GenerateNameOnce）", None),
    ("未分类_其他.tsv", "NameGenerator.Initialize()"): (
        "CardPresentation/Shell/NameGenerator.cs:1564（Initialize）", None),
    ("未分类_其他.tsv", "SingletonInitializableBehaviour.Awake()"): (
        "CardPresentation/Shell/SingletonInitializableBehaviour.cs:54（Awake）", None),
    ("未分类_其他.tsv", "SingletonInitializableBehaviour.OnDestroy()"): (
        "CardPresentation/Shell/SingletonInitializableBehaviour.cs:68（OnDestroy）", None),
    ("未分类_其他.tsv", "SingletonInitializableBehaviour.OnInitializationComplete()"): (
        "CardPresentation/Shell/SingletonInitializableBehaviour.cs:60（OnInitializationComplete）", None),
    ("未分类_其他.tsv", "SkullsCount.OnInitialize()"): (
        "CardPresentation/Shell/ProfileData.cs:635（ApplyAchievementTally）· `case SkullsCount → skullsCollected`；"
        "上游传参点 = 战斗文件的 AwardBattleAchievements / DailyData.OnBattleEnd", None),
    ("未分类_其他.tsv", "SkullsCount.OnReset()"): (TALLY, None),
    ("未分类_其他.tsv", "UnitReanimated.OnInitialize()"): (
        "RuleEngine/Core/AchievementTally.cs:249（AddReanimated）· 写点 = RuleEngine/Core/EffectResolver.cs"
        "（DoReanimate 两条支）· 读点 = Shell/ProfileData.cs 的 ApplyAchievementTally", None),
    ("未分类_其他.tsv", "UnitReanimated.OnReset()"): (TALLY, None),
    ("未分类_其他.tsv", "UnitSwarmCount.OnInitialize()"): (
        "RuleEngine/Core/AchievementTally.cs:255（AddSwarmMerge）· 写点 = RuleEngine/Core/RuleCore.cs 的 "
        "SwarmMergeNow（`ctx.Tally.AddSwarmMerge(p)`）· 读点 = Shell/ProfileData.cs 的 ApplyAchievementTally", None),
    ("未分类_其他.tsv", "UnitSwarmCount.OnReset()"): (TALLY, None),
    ("未分类_其他.tsv", "UpgradeCard.OnInitialize()"): (
        "CardPresentation/Shell/ProfileData.cs:813（ApplyAchievementUpgrade）· 判定点 = "
        "Shell/CardDetailPopup.cs 的 DoUpgrade 成功那一跳", None),
    ("未分类_其他.tsv", "UpgradeCard.OnReset()"): (TALLY, None),
    ("未分类_其他.tsv", "VarsDevice.GetCardInHandYOffsetTouchInput()"): (
        "CardPresentation/Hand/HandLayout.cs:1037（HoverLiftPxForDevice）· 桌面档 = 20；⚠️ 移动端 SO 那两档未建", None),

    # ===================== 泛型与启动链_补读.tsv =====================
    ("泛型与启动链_补读.tsv", "ShopStore<TSave,.GetBaseSave()"): (
        "无对应物 —— 原版 `GetBaseSave` = `return (TSave)this.<字段@0x38>`，纯取值器；我们的「本店存档」"
        "= 静态表 `Available` 列 + `InventoryStore` extras，没有那个 TSave 对象", "B"),
    ("泛型与启动链_补读.tsv", "MissionEvent<TS,.IsActive(MissionChallenge challenge)"): (
        "CardPresentation/Shell/MissionEvent.cs:404（IsActive）", None),
    ("泛型与启动链_补读.tsv", "SingletonInitializableBehaviour<T>.OnInitializationComplete()"): (
        "CardPresentation/Shell/SingletonInitializableBehaviour.cs:60（OnInitializationComplete）· "
        "`base.…(); instance = (T)(object)this;`", None),
    ("泛型与启动链_补读.tsv", "SingletonInitializableBehaviour<T>.Awake()"): (
        "CardPresentation/Shell/SingletonInitializableBehaviour.cs:54（Awake）· "
        "`if (Instance != null && Instance != this) Destroy(this);`", None),
    ("泛型与启动链_补读.tsv", "SingletonInitializableBehaviour<T>.OnDestroy()"): (
        "CardPresentation/Shell/SingletonInitializableBehaviour.cs:68（OnDestroy）· `== this` ⇒ "
        "`Destroy(this)` + `instance = null`", None),
    ("泛型与启动链_补读.tsv", "DraftEventBase<TS,.GetCardsFromPacks(DraftEventSaveGame desiredSaveGame)"): (
        "CardPresentation/Shell/DraftPacks.cs:405（TryBuildClosedDeck）· 摊平循环：包 → `p.cardIds` → "
        "逐张 append（顺序 = 选包序 + 包内原序）", None),

    # ===================== 社交_聊天.tsv =====================
    ("社交_聊天.tsv", "AllianceInvitationEntry.OnDestroy()"): (
        "无对应物 —— 原版 `OnDestroy` = 对两颗钮的 `UnityEvent` 各 `RemoveAllListeners`；本仓按钮走闭包、"
        "随节点销毁 ⇒ 没有要摘的东西，原版这条本身也没有可观察效果", "B"),

    # ===================== 菜单_外壳.tsv =====================
    ("菜单_外壳.tsv", "AdaptFlexibleGridLayoutByAspectRatio.OnDisable()"): (
        "无对应物 —— 原版只做 `Signal.Unregister`；我们的设计空间钉高 1080（`Core/LayoutSpace.cs`）"
        "⇒ 该适配器 `itemSize = 原值 × s` 恒等 ⇒ 既不需要注册也不需要退订", "B"),
    ("菜单_外壳.tsv", "AdaptFlexibleGridLayoutByAspectRatio.OnEnable()"): (
        "无对应物 —— 原版 = `Signal.Register` + `itemSize = 原值 × s`；我们钉高 1080 ⇒ `s` 恒 1、"
        "`CardH = 414.4` 已经是那一档 ⇒ 既不需要订阅也不需要每次重算", "B"),
    ("菜单_外壳.tsv", "TitleDrawer.GetItemName(CosmeticItemTitle item)"): (
        "CardPresentation/Shell/ProfileData.cs:372（TitleName）· 属性取值器 = 原版按 `TitleIndex` 取名字", None),
    ("菜单_外壳.tsv", "GameWindowWithTabs.CreateTab<T>(string tabName, Sprite icon, T tabPrefab = null)"): (
        "CardPresentation/Shell/GameWindowWithTabs.cs:360（CreateTab）· 原版那 6 步逐句；"
        "`tabPrefab` 母版字段与 `setupTabOnCreate` 闸在同一处类尾", None),

    # ===================== 轮抽.tsv =====================
    ("轮抽.tsv", "DraftEventBase.GetBaseData()"): (
        "无对应物 —— 原版 `GetBaseData` = `return (TData)this.<字段@0x220>`，纯取值器；我们的局数据 = "
        "`DraftModeWindow` 的静态 `_run`，没有同形的 `Data` 访问器", "B"),

    # ===================== 音频_语音.tsv =====================
    ("音频_语音.tsv", "AudioCue.OnEnable()"): (
        "Shell/WindowSounds.cs:184（Play）· 等价位 = `_lastAt`（按 cue 名的全局冷却表，进程内起始为空 "
        "= 原版 `AudioCue` 在资源载入时 `OnEnable` 把 `lastTimePlayed` 归零）", None),
}


def read_raw(path):
    b = io.open(path, "rb").read()
    bare = b.count(b"\r") - b.count(b"\r\n")
    text = b.decode("utf-8-sig")
    return text.replace("\r\n", "\n").split("\n"), bare


def main():
    files = {}
    for fn in sorted(os.listdir(LEDGER)):
        if not fn.endswith(".tsv") or fn.startswith("_"):
            continue
        lines, bare = read_raw(os.path.join(LEDGER, fn))
        if bare:
            print("!! %s 有 %d 个裸 CR" % (fn, bare))
        files[fn] = lines

    out_rows = []
    missing = []
    dup = []
    for (fn, who), (new_land, new_status) in MAP.items():
        if fn not in files:
            missing.append((fn, who, "账文件不存在"))
            continue
        lines = files[fn]
        hdr = lines[0].split("\t")
        assert hdr == COLS, (fn, hdr)
        hit = []
        for i, l in enumerate(lines):
            if not l.strip():
                continue
            c = l.split("\t")
            if len(c) != 11:
                continue
            if "%s.%s" % (c[1], c[2]) == who:
                hit.append((i, c))
        if not hit:
            missing.append((fn, who, "主表里找不到这一行"))
            continue
        if len(hit) > 1:
            dup.append((fn, who, [h[0] + 1 for h in hit]))
            continue
        i, c = hit[0]
        if c[5].strip() != "A":
            missing.append((fn, who, "状态不是 A（现 = %r）" % c[5]))
            continue
        nc = list(c)
        nc[6] = new_land
        if new_status:
            nc[5] = new_status
        out_rows.append(nc)

    if missing or dup:
        print("!! 未应用：")
        for m in missing:
            print("   MISSING", m)
        for d in dup:
            print("   DUP", d)
    print("将写入 %d 行补丁" % len(out_rows))

    body = "\t".join(COLS) + "\n" + "\n".join("\t".join(r) for r in out_rows) + "\n"
    tmp = OUT + ".tmp"
    io.open(tmp, "wb").write(body.encode("utf-8"))
    os.replace(tmp, OUT)
    print("写出 " + OUT)


main()
