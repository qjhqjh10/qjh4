// RuleEngineTest_Diag.cs — **诊断入口**：一条用例抛异常不掐死整轮（2026-10-01 加）
//
// 🔴 **本文件由 `工具/gen_diag_runner.py` 生成，别手改** —— 重生成：
//    `PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py`
//    它按 `RuleEngineTest.Run()` 里**一模一样的顺序**重出这一串；`Run()` 的用例表一变就要重跑它。
//
// 为什么要它：`Run()` 是一串直接调用，**任一条抛异常（哪怕是测试自己解引用 null）整轮就断在那儿**，
// 后面几百条根本跑不到 —— 2026-10-01 改棋盘模型时踩到过（`TestAuraSettle` 里一句 `Board[2].Armor`
// 就吃掉了整轮，只看得到 4 条失败）。
//   ⚠️ **2026-10-17 起 `Step()` 也加了逐条 try/catch** ⇒ `Run()` 自己不会再断了；
//      本入口今天多出来的那一半用处 = **它自带「少没少跑」的对账**（见下面 `VerifyDiagCoverage`）。
//      两条都留着：`Step` 的兜底在正本入口上，本入口是**旁路诊断**（顺序、计数、名字都独立可比）。
//
// 每条包一层 try/catch：抛异常的记一条失败（**带用例名** + 异常类型 + 消息）继续往下跑。
// ⚠️ **它不是替代品**：验收仍然跑 `RuleEngineTest.Run`（那个才是正本）。
//
// 🆕 **2026-10-11 `A1394` 收口 —— 这个文件从今往后自己会报「静默少跑」**：
//   · `RunCaseNames` = 生成器从 `Run()` 读出来的**用例总数正本**（顺序同 `Run()`）；
//   · `RunSafe()` 跑完调 `VerifyDiagCoverage()`：
//       ① **实际跑的条数/顺序** ↔ `RunCaseNames`（会红、带首处差异）；
//       ② `RunCaseNames` ↔ **反射看到的用例方法集合**（判据 = `LooksLikeCaseMethod`，同样会红）；
//   · 另有 `[InitializeOnLoadMethod] DiagDriftAlarm()`：**每次 Unity 起来**（含每条批处理自检）
//     对一次，过期就 `LogError` —— 不用等谁想起来跑本入口。
//   由来：2026-10-01 出生时生成器正则只认裸调用 `Xxx();`，`Run()` 改成 `Step(Xxx);` 后**命中 0**，
//   脚本一 assert 就挂 ⇒ **没人再跑它** ⇒ 本文件**静默少跑 65 条**（停在 132 / 当时 `Run()` 197）。
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static partial class RuleEngineTest
{
    // ==================================================================
    //  `Run()` 的用例表 —— 生成器写进来的**正本**（顺序 = `Run()` 里的顺序）
    //  ⚠️ 别手改这一段，也别手改下面那串 `Safe(...)`：改 `Run()` 后重跑生成器。
    // ==================================================================
    static readonly string[] RunCaseNames = new string[]
    {
        "TestBoardMatchesPresentation", "TestBoardContiguousModel", "TestKeywordParsing", "TestCardDatabase", "TestImplementedKeywordSites", "TestOriginalCardPool",
        "TestCardFaceFixes", "TestCardIds", "TestChooseEffect", "TestA4Batch3", "TestA4Batch4", "TestA5Batch1",
        "TestTacticTextCoverage", "TestA4PaidPrefixAndAtoms", "TestA4Batch1Atoms", "TestA4Batch2ForceAttack", "ReportUnitDescCoverage", "ReportWhenCoverage",
        "ReportAdjacentGap", "TestAdjacent", "TestAuraParse", "TestAuraSettle", "TestA1458HeadlessCostAura", "TestSpiritStone",
        "TestOathAbility", "TestFaithAbility", "TestBatch0916", "TestConditionContractions", "TestAccursedHelbrute", "TestAlphaWarrior",
        "TestConditionKindFamily", "TestZhCrosscheckFindings", "ReportWillRunMechanism", "ReportSwallowedClauses", "ReportAuraMechanism", "TestBeastbossAndPayloadSegments",
        "TestZhAgreement", "TestFormerTerminalGaps", "TestPlayerChoice", "TestA6Batch1", "TestArtifice", "TestAlternativeActions",
        "TestSwarm", "TestSynapse", "TestUprising", "TestStimulation", "TestTide", "TestRemnant",
        "TestTeleport", "TestAmbush", "TestCemeteryEvents", "TestCreatedBy", "TestCompanion", "TestTacticPlay",
        "TestDefenceCards", "TestGraveyardTakeOne", "TestOvertime", "TestSkirmish", "TestW58GainEnergy", "TestW58HandLimitDraw",
        "TestCardInstanceStep1", "TestCardInstanceStep2", "TestCardInstanceNewVsMove", "TestInstanceAcceptanceTarget", "TestFactionResources", "TestWhenEventsWidened",
        "TestDeckRules", "TestDeckValidation", "TestCopyLimitWarlordTier", "TestDeckIntoBattle", "TestDeckStoreRoundTrip", "TestDeckLibrary",
        "TestDeckGameMode", "TestDefenceFallback", "TestNewBattle", "TestMulligan", "TestBeginTurn", "TestEnergyIsPerPlayer",
        "TestB01AiScorerHook", "TestPlayCostRejected", "TestPlaySlotRejected", "TestPlayOccupiedSlotInserts", "TestPlayOk", "TestMeleeTrade",
        "TestMissionCounters", "TestArmorMinimumOne", "TestShieldBlocks", "TestRangedEatsCounter", "TestRetaliationEvent", "TestLongRangeNoCounter",
        "TestVanguardRestriction", "TestStealthUntargetable", "TestFlyingMeleeBlocked", "TestEffectSpecParsing", "TestFlankInvulnVulnerable", "TestAttackKeywords",
        "TestBattleKeywords", "TestRally", "TestStrikeAndSlay", "TestW58BlastStompSlay", "TestW58SlayInterleave", "TestW58HuntMarkNoKillerGate",
        "TestW58NestedDeathInterleave", "TestW58SentrySlay", "TestA1279SlaySelfChain", "TestBacklash", "TestDeathAccountsAfterBacklash", "TestW58DeathOrderD5",
        "TestW58DeathOrderD4", "TestW58DamagedBroadcastOrderD8", "TestA20BlockedHitDoesNotBroadcastDamaged", "TestA20SiegeAndQuestCompleted", "TestA20AlternativeAbilityBranch", "TestW58AttackBroadcastOrder",
        "TestT03dAttackerKilledBySentryStillFiresOwnAttack", "TestW58DeployBroadcastOrder", "TestW58HuntMarkBroadcastOrder", "TestPenitence", "TestAbility", "TestAbilityTargetRules",
        "TestWillDiePreviewEntries", "TestDodgeKeyword", "TestShieldOneRepresentation", "TestHealAndDraw", "TestEffectChainGuard", "TestStarterCardEffects",
        "TestA1618TwoParsersAgree", "TestAiUsesAbility", "TestAiKillLine", "TestCreate", "TestDeploy", "TestLowerCost",
        "TestA5Batch3", "TestA5Batch4", "TestChooseCard", "TestChooseFamilyB14", "TestPlayedTableB19", "TestPlayedCountB24",
        "TestReturnAndRefs", "TestPersistentEffects", "TestDeployBuffAndCostMore", "TestUnitDescTriggers", "TestMobAndRegimentKeyword", "TestKeywordTriggeredEvents",
        "TestFactionResourceEvents", "TestQuestTriggeredSecretFidelity", "TestW57FactionMechanics", "TestW57Fix2FactionReview", "TestW57Double605", "TestListenerCardType",
        "TestUnknownPayloadAttr", "TestWeaponIsRanged", "TestKeywordImplementedList", "TestUnstableEcstasyCruelty", "TestOrAltIf", "TestDoubleAndCreateCost",
        "TestTalentKeyword", "TestMentionedCards", "TestDarkPactRelated", "TestRelatedCardsTruth", "TestW10EngineLine", "TestW15TalentLine",
        "TestW15PrayAndAltAbility", "TestWhenEvents", "TestEventLayerTail", "TestEphemeral", "TestSimultaneousDamage", "TestFactionMechanics",
        "TestWinnerByWarlord", "TestDrawIsDraw", "TestForfeit", "TestBattleVictoryCode", "TestFatigue", "TestDeterminism",
        "TestSignalDeterminism", "TestFullGameWithRealCards", "TestFactionBattle", "TestAiOriginal", "TestHandInstanceEffects", "TestHandEffectExpiryAndUses",
        "TestCoreLayerPurity", "TestPlayedCardsExcludesCompanionAndTide", "TestTutorialEngineHalf", "TestTutorialDontDrawAndTurnEndEnchantment", "TestNewUnitEntryList", "TestW5EngineBacklog",
        "TestG3EngineRealDiffs", "TestS7BroadcastBeforeBookkeeping", "TestS10CompanyMasterLowerCost", "TestA947NoncombatantAttackBan", "TestDiedThisTurnDuringBacklash", "TestA974NotePlayedAfterPlayBroadcast",
        "TestA976LastCreatedNotCarriedAcrossCards", "TestA963HandListenerSelfDoesNotSpread", "TestA993TriggerEntryClearsReferentSlots", "TestA992OathDeployExemption", "TestA992CannotAttackThisTurn", "TestA985TermKey",
        "TestA1098SummonSicknessReads", "TestA992HandSlotThreeWritePoints", "TestA992ReanimateDeployTurnState", "TestA985ErrSlotSplit", "TestA985TargetedAndAmbushFields", "TestS14SurvivorRescueAndDestroyBypass",
        "TestS14EcstasyPrintedVersusGranted", "TestS14StunWatcherGainsAttack", "TestS14ApplyDeployTurnState", "TestS14PoisonAtOwnTurnEnd", "TestS14PoisonBranchDoesNotSkipStunGate", "TestS14BastionAbsorbsWholeHit",
        "TestS14CounterIgnoresBastion", "TestS14DropPodPool", "TestS14DropPodTakesPrecedenceOverBastion", "TestS14VulnerableWithBastion", "TestS14SacrificeOnOwnTurnRescue", "TestS14SacrificeNotTriggeredOnEnemyTurn",
        "TestS14SacrificeNotTriggeredOnRealDeath", "TestS14SurvivorTriggerAlwaysFires", "TestS14WouldKillByEntries", "TestS14HandAndBoardInstancesDisjoint", "TestS14PronounTargetSide", "TestS14PronounSideUnknownStaysUnknown",
        "TestS14PronounSideChains", "TestS14ParseIsIdempotent", "TestS14ScoreGivingChargeSide", "TestS14KeywordNumberAndHasNumber", "TestS14PaidPrefixCarry", "TestS14OathActivationChargesOnce",
        "TestS14CostSharedChargesOnce", "TestS14FaithCheckCondition", "TestS14WhenBodyTargetIsSubjectless", "TestS14FaithCostKind", "TestS14WouldKillByEntriesDropPodLayer", "TestS14DropPodOpensAtTurnStart",
        "TestS14DropPodOpensAfterAttack", "TestS14SwarmTriggerBothHalves", "TestS14BloodThirstTriggerBothHalves", "TestA1564EcstasyHeadGates", "TestA1564Ec28SlayStopsAtEcstasy", "TestA1564DA25WhenBodyCarry",
        "TestA1564AboutToAttackCancel", "TestA1564StealthExpiryAfterDropPod", "TestA1564BastionFullAbsorbBroadcasts", "TestA1564InsteadTargetCarry", "TestA1562KeywordTriggerSelfExclusion", "TestA1562SelfExclusionNullPair",
        "TestA1574SwarmSubject", "TestA1576SacrificeOrder", "TestA1563VulnerableDamageSplit", "TestA1566LandingAndAboutToAttackRegistration", "TestA1462NonKeywordTargetFilter", "TestA1606TurnStartForceAttack",
        "TestA1631HalfUnderstoodTriggerBody", "TestA1632EachTurnAttackOnOpponentTurn", "TestA1455DeployNamedTroop", "TestA1600SelfFatalHitHeard", "TestA1468BatchAllEnemiesHitsStealth", "TestA1600DyingListenerExcluded",
        "TestA1468SingleTargetStealthGate", "TestS23ParseWhenPrefix", "TestS23ParseCostWhenInstead", "TestS23ParseCostWhenIconMarker", "TestS23AssertImplementedReadSites", "TestS23AssertA1413PayGroup",
        "TestS23ResolveDeployCopyOfThisTroop", "TestS23ResolveInsteadOnAbilityLayer", "TestS23ResolveInsteadConditionalNotPairedOnAbilityLayer", "TestS23ResolveLandingCancelsAttack", "TestS23ResolveA1426GiveToDyingOnBoard", "TestS23ResolveArmourTwoRepresentations",
        "TestS23ResolveA1692DeployFreeNoOverwrite", "TestS23ResolveA1690WhenBodyAdjacentAnchor", "TestS23ResolveA1312KeepStatusTurnState", "TestW59PlayOrderSameSideDeploy", "TestW59PlayOrderCrossPlayer", "TestW59PlayOrderTurnStartEnd",
        "TestW59PlayOrderReentry", "TestW59PlayOrderInvariant", "TestW59PlayOrderOnSide", "TestT21TurnBeforeEndBeforeTurnEnd", "TestA24EC58DeployPairAndFlank", "TestW61ActionQueue",
        "TestEngineP1", "TestT02OrConditionTargetDiesOrFriendly", "TestT02SacrificeBodyLandsOnSelf", "TestT02PlacementNoOverwriteFamily", "TestT02FirstFreeSlotHonoursAvailability", "TestT02NextDeploySlotHonoursAvailability",
        "TestT02ReturnSideIsAvailableSlot", "TestT02ResolveWarlordSlotHonoursAvailability", "TestT02ResolveSideBranchHonoursAvailability", "TestT02OrFamilyBuckets", "TestT02DuplicateTriggerBodyDedup", "TestW61bSpentAllMana",
        "TestW61bSpendInsufficientDrainsToZero", "TestW61bInitialModifiers", "TestW61bSkipCardDrawProducer", "TestW64UnitConditions", "TestW64StructuredConditions", "TestW64StructuredTargetFilters",
        "TestW64OpAbilityTargetConditions", "TestB16ConditionEval", "TestB17SubjectlessTargets", "TestEngineBatch1023", "TestA12FurtherMultiplier", "TestA30ListenerTriggerNames",
        "TestW62EntryPump", "TestW64ShowCardsWindow", "TestN2UnitConditionRoles", "TestA1EngineFive", "TestB3EngineFive", "TestB6EngineThird",
        "TestB7EnginePickAndCount", "TestF8EnginePlayOrder", "TestG1bSpiritStone", "TestG1bStatusApplied", "TestG2DeathRoutes", "TestG3AiTargetsTrim",
        "TestG5BareDealAndReferencedCards", "TestG6MultiCountRepeats", "TestE8PreviousAbilityDrawnCards", "TestG10HandCountScope", "TestG12ActiveAbilityLogic", "TestEngineE2",
    };

    // 本次诊断真正跑过的用例（顺序同 `RunCaseNames`）—— 只给对账用
    static readonly List<string> _diagRan = new List<string>();

    static void Safe(string name, Action a)
    {
        _diagRan.Add(name);
        try { a(); }
        catch (Exception e)
        {
            _fail++;   // ⚠️ 异常是从 `a()` 里冒出来的，没有 Check 帮它计数 ⇒ 自己记一条
            string line = $"[{name}] 抛异常：{e.GetType().Name} {e.Message}";
            _failures.Add(line);
            Debug.LogError(P + "   \u2717 " + line);
        }
    }

    // ==================================================================
    //  「少没少跑」的对账（`A1394`）—— 判据在**下面两个方法里**，本段注释只是说明
    // ==================================================================

    /// <summary>
    /// 「用例方法」的口径 —— ⚠️ **必须与 `工具/gen_diag_runner.py` 的 `case_method_names()` 同口径**：
    /// `RuleEngineTest` 里【`static` ＋ 无参 ＋ 返回 `void` ＋ 名字 `Test`/`Report` 打头且后接大写或 `_`】。
    /// 实测（2026-10-11）：这类方法 **213** 个，与 `Run()` 的用例表**逐个相等**（0 多 0 少）。
    /// ⛔ 新加**辅助方法**时别用 `Test`/`Report` 打头 —— 会被算成用例（改个名就行）。
    /// </summary>
    static bool LooksLikeCaseMethod(string n)
    {
        int k = n.StartsWith("Test") ? 4 : (n.StartsWith("Report") ? 6 : -1);
        return k > 0 && n.Length > k && (char.IsUpper(n[k]) || n[k] == '_');
    }

    /// <summary>
    /// **反射**看到的用例方法集合（排序后）—— 它跨 `RuleEngineTest` 的**所有 partial 分册**
    /// （`RuleEngineTest.cs` / `_BoardModel` / `_S14` / `_Diag` / `DeckRulesTest.cs`），
    /// 也就是「**编译出来的**应该是哪些用例」= 与 `Run()` 的正文无关的第二条腿。
    /// </summary>
    static List<string> DiagCompiledCaseMethods()
    {
        var names = new List<string>();
        foreach (var m in typeof(RuleEngineTest).GetMethods(
                     BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (m.ReturnType != typeof(void)) continue;
            if (m.GetParameters().Length != 0) continue;
            if (!LooksLikeCaseMethod(m.Name)) continue;
            names.Add(m.Name);
        }
        names.Sort(StringComparer.Ordinal);
        return names;
    }

    static string DiagFew(List<string> xs)
        => xs.Count == 0 ? "（无）"
         : string.Join("、", xs.GetRange(0, Math.Min(8, xs.Count)))
           + (xs.Count > 8 ? " …等 " + xs.Count + " 个" : "");

    /// <summary>
    /// 🔴 **本入口的「静默少跑」闸**（`A1394`）：`RunSafe()` **跑完必须调它**。
    /// 三条断言：
    ///   ① 实际跑的条数/顺序 == 生成器写进来的用例表（**多少条都对得上**）；
    ///   ② `Run()` 的用例表 == 反射看到的用例方法集合 ⇒ **「`Run()` 加了用例、产物没重生成」直接红**
    ///      （这一条管的是**两个入口都少跑**：`Run()` 那边也漏）；
    ///   ③ 反射清单非空 —— 防「判据自己坏了」变成**假绿**（`LooksLikeCaseMethod` 写错会返回 0 个，
    ///      那时 ② 的「缺」是空集、看着全对）。
    /// </summary>
    static void VerifyDiagCoverage()
    {
        // ---- ① 实际跑的 ↔ 生成器写进来的用例表（逐条同序）----
        var exp = new List<string>(RunCaseNames);
        int diff = 0;
        string first = "";
        for (int k = 0; k < Math.Max(_diagRan.Count, exp.Count); k++)
        {
            string ran = k < _diagRan.Count ? _diagRan[k] : "<无>";
            string want = k < exp.Count ? exp[k] : "<无>";
            if (ran == want) continue;
            diff++;
            if (first.Length == 0) first = $"第 {k + 1} 条：实跑 [{ran}] / 用例表 [{want}]";
        }
        Check(diff, 0, $"诊断入口实际跑了 {_diagRan.Count} 条，与用例表（{exp.Count} 条）逐条同序"
                     + (diff > 0 ? $" —— **{diff} 处不一致**，首处 {first}" : ""));

        // ---- ② `Run()` 的用例表 ↔ 反射看到的用例方法集合 ----
        var compiled = DiagCompiledCaseMethods();
        var missing = new List<string>();   // 类里有、`Run()` 的用例表里没有 ⇒ **两个入口都少跑它**
        var unknown = new List<string>();   // 用例表里有、类里没有 ⇒ 本该编译期就挡住（挡住了说明判据漏了）
        foreach (var n in compiled) if (!exp.Contains(n)) missing.Add(n);
        foreach (var n in exp) if (!compiled.Contains(n)) unknown.Add(n);
        Check(missing.Count, 0, $"用例方法（反射 {compiled.Count} 个）全在 `Run()` 的用例表里"
                              + (missing.Count > 0
                                 ? $" —— **缺 {missing.Count} 个**：{DiagFew(missing)}"
                                   + "（处置：若它本来就是用例 ⇒ 加进 `Run()` 再重生成；"
                                   + "若它是辅助方法 ⇒ 改个名，别用 `Test`/`Report` 打头）"
                                 : ""));
        Check(unknown.Count, 0, $"`Run()` 的用例表里没有「类里不存在的名字」"
                              + (unknown.Count > 0 ? $" —— **多 {unknown.Count} 个**：{DiagFew(unknown)}" : ""));
        Check(compiled.Count > 0, true, "反射清单非空（口径 `LooksLikeCaseMethod` 没坏）");
    }

    /// <summary>
    /// 🔴 **过期警报**：每次 Unity 起来（含 `-batchmode -executeMethod ...`）对一次，
    /// 产物比 `Run()` 旧就 `LogError`。
    /// **为什么非要有它**：`RunSafe` 是「人手想起来才跑」的入口
    /// （`工具/_run_8_checks.sh` 跑的是 `RuleEngineTest.Run`，**不含本入口**）——
    /// 光把断言放进 `RunSafe` 就还是「没人跑 = 没人知道」，正是 `A1394` 那个洞的形状。
    /// ⚠️ 这里**只报警、不碰任何计数**：`Run()` 开头就把 `_fail` / `_failures` 清零了，
    ///    写进去会被抹掉 ⇒ 只能靠日志（`LogError`，⛔ 不用 `Log`，否则又成静默）。
    /// </summary>
    [UnityEditor.InitializeOnLoadMethod]
    static void DiagDriftAlarm()
    {
        try
        {
            var compiled = DiagCompiledCaseMethods();
            var exp = new List<string>(RunCaseNames);
            var missing = new List<string>();
            int extra = 0;
            foreach (var n in compiled) if (!exp.Contains(n)) missing.Add(n);
            foreach (var n in exp) if (!compiled.Contains(n)) extra++;
            if (missing.Count == 0 && extra == 0) return;
            missing.Sort(StringComparer.Ordinal);
            Debug.LogError(P + "   \u2717 [诊断入口过期] `RuleEngineTest_Diag.cs` 跟不上 `Run()` 的用例表 —— "
                + $"反射看到 {compiled.Count} 个用例方法，产物只认 {exp.Count} 个（缺 {missing.Count} / 多 {extra}）"
                + (missing.Count > 0 ? $"；缺：{DiagFew(missing)}" : "")
                + "。重生成：`PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py`"
                + "（⛔ 别手改那个文件）");
        }
        catch (Exception e)
        {
            // 警报器自己坏了要说出来，但它不该影响别的东西 —— 所以只 Warning、不重抛
            Debug.LogWarning(P + "   [诊断入口过期检查] 自身出错（不影响别的）：" + e.Message);
        }
    }

    [UnityEditor.MenuItem("Tools/RuleEngine/规则引擎自检（诊断：单条异常不中断）")]
    public static void RunSafe()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        _diagRan.Clear();
        Debug.Log(P + "=== 规则引擎自检（诊断模式）开始 ===");

        Safe("TestBoardMatchesPresentation", () => TestBoardMatchesPresentation());
        Safe("TestBoardContiguousModel", () => TestBoardContiguousModel());
        Safe("TestKeywordParsing", () => TestKeywordParsing());
        Safe("TestCardDatabase", () => TestCardDatabase());
        Safe("TestImplementedKeywordSites", () => TestImplementedKeywordSites());
        Safe("TestOriginalCardPool", () => TestOriginalCardPool());
        Safe("TestCardFaceFixes", () => TestCardFaceFixes());
        Safe("TestCardIds", () => TestCardIds());
        Safe("TestChooseEffect", () => TestChooseEffect());
        Safe("TestA4Batch3", () => TestA4Batch3());
        Safe("TestA4Batch4", () => TestA4Batch4());
        Safe("TestA5Batch1", () => TestA5Batch1());
        Safe("TestTacticTextCoverage", () => TestTacticTextCoverage());
        Safe("TestA4PaidPrefixAndAtoms", () => TestA4PaidPrefixAndAtoms());
        Safe("TestA4Batch1Atoms", () => TestA4Batch1Atoms());
        Safe("TestA4Batch2ForceAttack", () => TestA4Batch2ForceAttack());
        Safe("ReportUnitDescCoverage", () => ReportUnitDescCoverage());
        Safe("ReportWhenCoverage", () => ReportWhenCoverage());
        Safe("ReportAdjacentGap", () => ReportAdjacentGap());
        Safe("TestAdjacent", () => TestAdjacent());
        Safe("TestAuraParse", () => TestAuraParse());
        Safe("TestAuraSettle", () => TestAuraSettle());
        Safe("TestA1458HeadlessCostAura", () => TestA1458HeadlessCostAura());
        Safe("TestSpiritStone", () => TestSpiritStone());
        Safe("TestOathAbility", () => TestOathAbility());
        Safe("TestFaithAbility", () => TestFaithAbility());
        Safe("TestBatch0916", () => TestBatch0916());
        Safe("TestConditionContractions", () => TestConditionContractions());
        Safe("TestAccursedHelbrute", () => TestAccursedHelbrute());
        Safe("TestAlphaWarrior", () => TestAlphaWarrior());
        Safe("TestConditionKindFamily", () => TestConditionKindFamily());
        Safe("TestZhCrosscheckFindings", () => TestZhCrosscheckFindings());
        Safe("ReportWillRunMechanism", () => ReportWillRunMechanism());
        Safe("ReportSwallowedClauses", () => ReportSwallowedClauses());
        Safe("ReportAuraMechanism", () => ReportAuraMechanism());
        Safe("TestBeastbossAndPayloadSegments", () => TestBeastbossAndPayloadSegments());
        Safe("TestZhAgreement", () => TestZhAgreement());
        Safe("TestFormerTerminalGaps", () => TestFormerTerminalGaps());
        Safe("TestPlayerChoice", () => TestPlayerChoice());
        Safe("TestA6Batch1", () => TestA6Batch1());
        Safe("TestArtifice", () => TestArtifice());
        Safe("TestAlternativeActions", () => TestAlternativeActions());
        Safe("TestSwarm", () => TestSwarm());
        Safe("TestSynapse", () => TestSynapse());
        Safe("TestUprising", () => TestUprising());
        Safe("TestStimulation", () => TestStimulation());
        Safe("TestTide", () => TestTide());
        Safe("TestRemnant", () => TestRemnant());
        Safe("TestTeleport", () => TestTeleport());
        Safe("TestAmbush", () => TestAmbush());
        Safe("TestCemeteryEvents", () => TestCemeteryEvents());
        Safe("TestCreatedBy", () => TestCreatedBy());
        Safe("TestCompanion", () => TestCompanion());
        Safe("TestTacticPlay", () => TestTacticPlay());
        Safe("TestDefenceCards", () => TestDefenceCards());
        Safe("TestGraveyardTakeOne", () => TestGraveyardTakeOne());
        Safe("TestOvertime", () => TestOvertime());
        Safe("TestSkirmish", () => TestSkirmish());
        Safe("TestW58GainEnergy", () => TestW58GainEnergy());
        Safe("TestW58HandLimitDraw", () => TestW58HandLimitDraw());
        Safe("TestCardInstanceStep1", () => TestCardInstanceStep1());
        Safe("TestCardInstanceStep2", () => TestCardInstanceStep2());
        Safe("TestCardInstanceNewVsMove", () => TestCardInstanceNewVsMove());
        Safe("TestInstanceAcceptanceTarget", () => TestInstanceAcceptanceTarget());
        Safe("TestFactionResources", () => TestFactionResources());
        Safe("TestWhenEventsWidened", () => TestWhenEventsWidened());
        Safe("TestDeckRules", () => TestDeckRules());
        Safe("TestDeckValidation", () => TestDeckValidation());
        Safe("TestCopyLimitWarlordTier", () => TestCopyLimitWarlordTier());
        Safe("TestDeckIntoBattle", () => TestDeckIntoBattle());
        Safe("TestDeckStoreRoundTrip", () => TestDeckStoreRoundTrip());
        Safe("TestDeckLibrary", () => TestDeckLibrary());
        Safe("TestDeckGameMode", () => TestDeckGameMode());
        Safe("TestDefenceFallback", () => TestDefenceFallback());
        Safe("TestNewBattle", () => TestNewBattle());
        Safe("TestMulligan", () => TestMulligan());
        Safe("TestBeginTurn", () => TestBeginTurn());
        Safe("TestEnergyIsPerPlayer", () => TestEnergyIsPerPlayer());
        Safe("TestB01AiScorerHook", () => TestB01AiScorerHook());
        Safe("TestPlayCostRejected", () => TestPlayCostRejected());
        Safe("TestPlaySlotRejected", () => TestPlaySlotRejected());
        Safe("TestPlayOccupiedSlotInserts", () => TestPlayOccupiedSlotInserts());
        Safe("TestPlayOk", () => TestPlayOk());
        Safe("TestMeleeTrade", () => TestMeleeTrade());
        Safe("TestMissionCounters", () => TestMissionCounters());
        Safe("TestArmorMinimumOne", () => TestArmorMinimumOne());
        Safe("TestShieldBlocks", () => TestShieldBlocks());
        Safe("TestRangedEatsCounter", () => TestRangedEatsCounter());
        Safe("TestRetaliationEvent", () => TestRetaliationEvent());
        Safe("TestLongRangeNoCounter", () => TestLongRangeNoCounter());
        Safe("TestVanguardRestriction", () => TestVanguardRestriction());
        Safe("TestStealthUntargetable", () => TestStealthUntargetable());
        Safe("TestFlyingMeleeBlocked", () => TestFlyingMeleeBlocked());
        Safe("TestEffectSpecParsing", () => TestEffectSpecParsing());
        Safe("TestFlankInvulnVulnerable", () => TestFlankInvulnVulnerable());
        Safe("TestAttackKeywords", () => TestAttackKeywords());
        Safe("TestBattleKeywords", () => TestBattleKeywords());
        Safe("TestRally", () => TestRally());
        Safe("TestStrikeAndSlay", () => TestStrikeAndSlay());
        Safe("TestW58BlastStompSlay", () => TestW58BlastStompSlay());
        Safe("TestW58SlayInterleave", () => TestW58SlayInterleave());
        Safe("TestW58HuntMarkNoKillerGate", () => TestW58HuntMarkNoKillerGate());
        Safe("TestW58NestedDeathInterleave", () => TestW58NestedDeathInterleave());
        Safe("TestW58SentrySlay", () => TestW58SentrySlay());
        Safe("TestA1279SlaySelfChain", () => TestA1279SlaySelfChain());
        Safe("TestBacklash", () => TestBacklash());
        Safe("TestDeathAccountsAfterBacklash", () => TestDeathAccountsAfterBacklash());
        Safe("TestW58DeathOrderD5", () => TestW58DeathOrderD5());
        Safe("TestW58DeathOrderD4", () => TestW58DeathOrderD4());
        Safe("TestW58DamagedBroadcastOrderD8", () => TestW58DamagedBroadcastOrderD8());
        Safe("TestA20BlockedHitDoesNotBroadcastDamaged", () => TestA20BlockedHitDoesNotBroadcastDamaged());
        Safe("TestA20SiegeAndQuestCompleted", () => TestA20SiegeAndQuestCompleted());
        Safe("TestA20AlternativeAbilityBranch", () => TestA20AlternativeAbilityBranch());
        Safe("TestW58AttackBroadcastOrder", () => TestW58AttackBroadcastOrder());
        Safe("TestT03dAttackerKilledBySentryStillFiresOwnAttack", () => TestT03dAttackerKilledBySentryStillFiresOwnAttack());
        Safe("TestW58DeployBroadcastOrder", () => TestW58DeployBroadcastOrder());
        Safe("TestW58HuntMarkBroadcastOrder", () => TestW58HuntMarkBroadcastOrder());
        Safe("TestPenitence", () => TestPenitence());
        Safe("TestAbility", () => TestAbility());
        Safe("TestAbilityTargetRules", () => TestAbilityTargetRules());
        Safe("TestWillDiePreviewEntries", () => TestWillDiePreviewEntries());
        Safe("TestDodgeKeyword", () => TestDodgeKeyword());
        Safe("TestShieldOneRepresentation", () => TestShieldOneRepresentation());
        Safe("TestHealAndDraw", () => TestHealAndDraw());
        Safe("TestEffectChainGuard", () => TestEffectChainGuard());
        Safe("TestStarterCardEffects", () => TestStarterCardEffects());
        Safe("TestA1618TwoParsersAgree", () => TestA1618TwoParsersAgree());
        Safe("TestAiUsesAbility", () => TestAiUsesAbility());
        Safe("TestAiKillLine", () => TestAiKillLine());
        Safe("TestCreate", () => TestCreate());
        Safe("TestDeploy", () => TestDeploy());
        Safe("TestLowerCost", () => TestLowerCost());
        Safe("TestA5Batch3", () => TestA5Batch3());
        Safe("TestA5Batch4", () => TestA5Batch4());
        Safe("TestChooseCard", () => TestChooseCard());
        Safe("TestChooseFamilyB14", () => TestChooseFamilyB14());
        Safe("TestPlayedTableB19", () => TestPlayedTableB19());
        Safe("TestPlayedCountB24", () => TestPlayedCountB24());
        Safe("TestReturnAndRefs", () => TestReturnAndRefs());
        Safe("TestPersistentEffects", () => TestPersistentEffects());
        Safe("TestDeployBuffAndCostMore", () => TestDeployBuffAndCostMore());
        Safe("TestUnitDescTriggers", () => TestUnitDescTriggers());
        Safe("TestMobAndRegimentKeyword", () => TestMobAndRegimentKeyword());
        Safe("TestKeywordTriggeredEvents", () => TestKeywordTriggeredEvents());
        Safe("TestFactionResourceEvents", () => TestFactionResourceEvents());
        Safe("TestQuestTriggeredSecretFidelity", () => TestQuestTriggeredSecretFidelity());
        Safe("TestW57FactionMechanics", () => TestW57FactionMechanics());
        Safe("TestW57Fix2FactionReview", () => TestW57Fix2FactionReview());
        Safe("TestW57Double605", () => TestW57Double605());
        Safe("TestListenerCardType", () => TestListenerCardType());
        Safe("TestUnknownPayloadAttr", () => TestUnknownPayloadAttr());
        Safe("TestWeaponIsRanged", () => TestWeaponIsRanged());
        Safe("TestKeywordImplementedList", () => TestKeywordImplementedList());
        Safe("TestUnstableEcstasyCruelty", () => TestUnstableEcstasyCruelty());
        Safe("TestOrAltIf", () => TestOrAltIf());
        Safe("TestDoubleAndCreateCost", () => TestDoubleAndCreateCost());
        Safe("TestTalentKeyword", () => TestTalentKeyword());
        Safe("TestMentionedCards", () => TestMentionedCards());
        Safe("TestDarkPactRelated", () => TestDarkPactRelated());
        Safe("TestRelatedCardsTruth", () => TestRelatedCardsTruth());
        Safe("TestW10EngineLine", () => TestW10EngineLine());
        Safe("TestW15TalentLine", () => TestW15TalentLine());
        Safe("TestW15PrayAndAltAbility", () => TestW15PrayAndAltAbility());
        Safe("TestWhenEvents", () => TestWhenEvents());
        Safe("TestEventLayerTail", () => TestEventLayerTail());
        Safe("TestEphemeral", () => TestEphemeral());
        Safe("TestSimultaneousDamage", () => TestSimultaneousDamage());
        Safe("TestFactionMechanics", () => TestFactionMechanics());
        Safe("TestWinnerByWarlord", () => TestWinnerByWarlord());
        Safe("TestDrawIsDraw", () => TestDrawIsDraw());
        Safe("TestForfeit", () => TestForfeit());
        Safe("TestBattleVictoryCode", () => TestBattleVictoryCode());
        Safe("TestFatigue", () => TestFatigue());
        Safe("TestDeterminism", () => TestDeterminism());
        Safe("TestSignalDeterminism", () => TestSignalDeterminism());
        Safe("TestFullGameWithRealCards", () => TestFullGameWithRealCards());
        Safe("TestFactionBattle", () => TestFactionBattle());
        Safe("TestAiOriginal", () => TestAiOriginal());
        Safe("TestHandInstanceEffects", () => TestHandInstanceEffects());
        Safe("TestHandEffectExpiryAndUses", () => TestHandEffectExpiryAndUses());
        Safe("TestCoreLayerPurity", () => TestCoreLayerPurity());
        Safe("TestPlayedCardsExcludesCompanionAndTide", () => TestPlayedCardsExcludesCompanionAndTide());
        Safe("TestTutorialEngineHalf", () => TestTutorialEngineHalf());
        Safe("TestTutorialDontDrawAndTurnEndEnchantment", () => TestTutorialDontDrawAndTurnEndEnchantment());
        Safe("TestNewUnitEntryList", () => TestNewUnitEntryList());
        Safe("TestW5EngineBacklog", () => TestW5EngineBacklog());
        Safe("TestG3EngineRealDiffs", () => TestG3EngineRealDiffs());
        Safe("TestS7BroadcastBeforeBookkeeping", () => TestS7BroadcastBeforeBookkeeping());
        Safe("TestS10CompanyMasterLowerCost", () => TestS10CompanyMasterLowerCost());
        Safe("TestA947NoncombatantAttackBan", () => TestA947NoncombatantAttackBan());
        Safe("TestDiedThisTurnDuringBacklash", () => TestDiedThisTurnDuringBacklash());
        Safe("TestA974NotePlayedAfterPlayBroadcast", () => TestA974NotePlayedAfterPlayBroadcast());
        Safe("TestA976LastCreatedNotCarriedAcrossCards", () => TestA976LastCreatedNotCarriedAcrossCards());
        Safe("TestA963HandListenerSelfDoesNotSpread", () => TestA963HandListenerSelfDoesNotSpread());
        Safe("TestA993TriggerEntryClearsReferentSlots", () => TestA993TriggerEntryClearsReferentSlots());
        Safe("TestA992OathDeployExemption", () => TestA992OathDeployExemption());
        Safe("TestA992CannotAttackThisTurn", () => TestA992CannotAttackThisTurn());
        Safe("TestA985TermKey", () => TestA985TermKey());
        Safe("TestA1098SummonSicknessReads", () => TestA1098SummonSicknessReads());
        Safe("TestA992HandSlotThreeWritePoints", () => TestA992HandSlotThreeWritePoints());
        Safe("TestA992ReanimateDeployTurnState", () => TestA992ReanimateDeployTurnState());
        Safe("TestA985ErrSlotSplit", () => TestA985ErrSlotSplit());
        Safe("TestA985TargetedAndAmbushFields", () => TestA985TargetedAndAmbushFields());
        Safe("TestS14SurvivorRescueAndDestroyBypass", () => TestS14SurvivorRescueAndDestroyBypass());
        Safe("TestS14EcstasyPrintedVersusGranted", () => TestS14EcstasyPrintedVersusGranted());
        Safe("TestS14StunWatcherGainsAttack", () => TestS14StunWatcherGainsAttack());
        Safe("TestS14ApplyDeployTurnState", () => TestS14ApplyDeployTurnState());
        Safe("TestS14PoisonAtOwnTurnEnd", () => TestS14PoisonAtOwnTurnEnd());
        Safe("TestS14PoisonBranchDoesNotSkipStunGate", () => TestS14PoisonBranchDoesNotSkipStunGate());
        Safe("TestS14BastionAbsorbsWholeHit", () => TestS14BastionAbsorbsWholeHit());
        Safe("TestS14CounterIgnoresBastion", () => TestS14CounterIgnoresBastion());
        Safe("TestS14DropPodPool", () => TestS14DropPodPool());
        Safe("TestS14DropPodTakesPrecedenceOverBastion", () => TestS14DropPodTakesPrecedenceOverBastion());
        Safe("TestS14VulnerableWithBastion", () => TestS14VulnerableWithBastion());
        Safe("TestS14SacrificeOnOwnTurnRescue", () => TestS14SacrificeOnOwnTurnRescue());
        Safe("TestS14SacrificeNotTriggeredOnEnemyTurn", () => TestS14SacrificeNotTriggeredOnEnemyTurn());
        Safe("TestS14SacrificeNotTriggeredOnRealDeath", () => TestS14SacrificeNotTriggeredOnRealDeath());
        Safe("TestS14SurvivorTriggerAlwaysFires", () => TestS14SurvivorTriggerAlwaysFires());
        Safe("TestS14WouldKillByEntries", () => TestS14WouldKillByEntries());
        Safe("TestS14HandAndBoardInstancesDisjoint", () => TestS14HandAndBoardInstancesDisjoint());
        Safe("TestS14PronounTargetSide", () => TestS14PronounTargetSide());
        Safe("TestS14PronounSideUnknownStaysUnknown", () => TestS14PronounSideUnknownStaysUnknown());
        Safe("TestS14PronounSideChains", () => TestS14PronounSideChains());
        Safe("TestS14ParseIsIdempotent", () => TestS14ParseIsIdempotent());
        Safe("TestS14ScoreGivingChargeSide", () => TestS14ScoreGivingChargeSide());
        Safe("TestS14KeywordNumberAndHasNumber", () => TestS14KeywordNumberAndHasNumber());
        Safe("TestS14PaidPrefixCarry", () => TestS14PaidPrefixCarry());
        Safe("TestS14OathActivationChargesOnce", () => TestS14OathActivationChargesOnce());
        Safe("TestS14CostSharedChargesOnce", () => TestS14CostSharedChargesOnce());
        Safe("TestS14FaithCheckCondition", () => TestS14FaithCheckCondition());
        Safe("TestS14WhenBodyTargetIsSubjectless", () => TestS14WhenBodyTargetIsSubjectless());
        Safe("TestS14FaithCostKind", () => TestS14FaithCostKind());
        Safe("TestS14WouldKillByEntriesDropPodLayer", () => TestS14WouldKillByEntriesDropPodLayer());
        Safe("TestS14DropPodOpensAtTurnStart", () => TestS14DropPodOpensAtTurnStart());
        Safe("TestS14DropPodOpensAfterAttack", () => TestS14DropPodOpensAfterAttack());
        Safe("TestS14SwarmTriggerBothHalves", () => TestS14SwarmTriggerBothHalves());
        Safe("TestS14BloodThirstTriggerBothHalves", () => TestS14BloodThirstTriggerBothHalves());
        Safe("TestA1564EcstasyHeadGates", () => TestA1564EcstasyHeadGates());
        Safe("TestA1564Ec28SlayStopsAtEcstasy", () => TestA1564Ec28SlayStopsAtEcstasy());
        Safe("TestA1564DA25WhenBodyCarry", () => TestA1564DA25WhenBodyCarry());
        Safe("TestA1564AboutToAttackCancel", () => TestA1564AboutToAttackCancel());
        Safe("TestA1564StealthExpiryAfterDropPod", () => TestA1564StealthExpiryAfterDropPod());
        Safe("TestA1564BastionFullAbsorbBroadcasts", () => TestA1564BastionFullAbsorbBroadcasts());
        Safe("TestA1564InsteadTargetCarry", () => TestA1564InsteadTargetCarry());
        Safe("TestA1562KeywordTriggerSelfExclusion", () => TestA1562KeywordTriggerSelfExclusion());
        Safe("TestA1562SelfExclusionNullPair", () => TestA1562SelfExclusionNullPair());
        Safe("TestA1574SwarmSubject", () => TestA1574SwarmSubject());
        Safe("TestA1576SacrificeOrder", () => TestA1576SacrificeOrder());
        Safe("TestA1563VulnerableDamageSplit", () => TestA1563VulnerableDamageSplit());
        Safe("TestA1566LandingAndAboutToAttackRegistration", () => TestA1566LandingAndAboutToAttackRegistration());
        Safe("TestA1462NonKeywordTargetFilter", () => TestA1462NonKeywordTargetFilter());
        Safe("TestA1606TurnStartForceAttack", () => TestA1606TurnStartForceAttack());
        Safe("TestA1631HalfUnderstoodTriggerBody", () => TestA1631HalfUnderstoodTriggerBody());
        Safe("TestA1632EachTurnAttackOnOpponentTurn", () => TestA1632EachTurnAttackOnOpponentTurn());
        Safe("TestA1455DeployNamedTroop", () => TestA1455DeployNamedTroop());
        Safe("TestA1600SelfFatalHitHeard", () => TestA1600SelfFatalHitHeard());
        Safe("TestA1468BatchAllEnemiesHitsStealth", () => TestA1468BatchAllEnemiesHitsStealth());
        Safe("TestA1600DyingListenerExcluded", () => TestA1600DyingListenerExcluded());
        Safe("TestA1468SingleTargetStealthGate", () => TestA1468SingleTargetStealthGate());
        Safe("TestS23ParseWhenPrefix", () => TestS23ParseWhenPrefix());
        Safe("TestS23ParseCostWhenInstead", () => TestS23ParseCostWhenInstead());
        Safe("TestS23ParseCostWhenIconMarker", () => TestS23ParseCostWhenIconMarker());
        Safe("TestS23AssertImplementedReadSites", () => TestS23AssertImplementedReadSites());
        Safe("TestS23AssertA1413PayGroup", () => TestS23AssertA1413PayGroup());
        Safe("TestS23ResolveDeployCopyOfThisTroop", () => TestS23ResolveDeployCopyOfThisTroop());
        Safe("TestS23ResolveInsteadOnAbilityLayer", () => TestS23ResolveInsteadOnAbilityLayer());
        Safe("TestS23ResolveInsteadConditionalNotPairedOnAbilityLayer", () => TestS23ResolveInsteadConditionalNotPairedOnAbilityLayer());
        Safe("TestS23ResolveLandingCancelsAttack", () => TestS23ResolveLandingCancelsAttack());
        Safe("TestS23ResolveA1426GiveToDyingOnBoard", () => TestS23ResolveA1426GiveToDyingOnBoard());
        Safe("TestS23ResolveArmourTwoRepresentations", () => TestS23ResolveArmourTwoRepresentations());
        Safe("TestS23ResolveA1692DeployFreeNoOverwrite", () => TestS23ResolveA1692DeployFreeNoOverwrite());
        Safe("TestS23ResolveA1690WhenBodyAdjacentAnchor", () => TestS23ResolveA1690WhenBodyAdjacentAnchor());
        Safe("TestS23ResolveA1312KeepStatusTurnState", () => TestS23ResolveA1312KeepStatusTurnState());
        Safe("TestW59PlayOrderSameSideDeploy", () => TestW59PlayOrderSameSideDeploy());
        Safe("TestW59PlayOrderCrossPlayer", () => TestW59PlayOrderCrossPlayer());
        Safe("TestW59PlayOrderTurnStartEnd", () => TestW59PlayOrderTurnStartEnd());
        Safe("TestW59PlayOrderReentry", () => TestW59PlayOrderReentry());
        Safe("TestW59PlayOrderInvariant", () => TestW59PlayOrderInvariant());
        Safe("TestW59PlayOrderOnSide", () => TestW59PlayOrderOnSide());
        Safe("TestT21TurnBeforeEndBeforeTurnEnd", () => TestT21TurnBeforeEndBeforeTurnEnd());
        Safe("TestA24EC58DeployPairAndFlank", () => TestA24EC58DeployPairAndFlank());
        Safe("TestW61ActionQueue", () => TestW61ActionQueue());
        Safe("TestEngineP1", () => TestEngineP1());
        Safe("TestT02OrConditionTargetDiesOrFriendly", () => TestT02OrConditionTargetDiesOrFriendly());
        Safe("TestT02SacrificeBodyLandsOnSelf", () => TestT02SacrificeBodyLandsOnSelf());
        Safe("TestT02PlacementNoOverwriteFamily", () => TestT02PlacementNoOverwriteFamily());
        Safe("TestT02FirstFreeSlotHonoursAvailability", () => TestT02FirstFreeSlotHonoursAvailability());
        Safe("TestT02NextDeploySlotHonoursAvailability", () => TestT02NextDeploySlotHonoursAvailability());
        Safe("TestT02ReturnSideIsAvailableSlot", () => TestT02ReturnSideIsAvailableSlot());
        Safe("TestT02ResolveWarlordSlotHonoursAvailability", () => TestT02ResolveWarlordSlotHonoursAvailability());
        Safe("TestT02ResolveSideBranchHonoursAvailability", () => TestT02ResolveSideBranchHonoursAvailability());
        Safe("TestT02OrFamilyBuckets", () => TestT02OrFamilyBuckets());
        Safe("TestT02DuplicateTriggerBodyDedup", () => TestT02DuplicateTriggerBodyDedup());
        Safe("TestW61bSpentAllMana", () => TestW61bSpentAllMana());
        Safe("TestW61bSpendInsufficientDrainsToZero", () => TestW61bSpendInsufficientDrainsToZero());
        Safe("TestW61bInitialModifiers", () => TestW61bInitialModifiers());
        Safe("TestW61bSkipCardDrawProducer", () => TestW61bSkipCardDrawProducer());
        Safe("TestW64UnitConditions", () => TestW64UnitConditions());
        Safe("TestW64StructuredConditions", () => TestW64StructuredConditions());
        Safe("TestW64StructuredTargetFilters", () => TestW64StructuredTargetFilters());
        Safe("TestW64OpAbilityTargetConditions", () => TestW64OpAbilityTargetConditions());
        Safe("TestB16ConditionEval", () => TestB16ConditionEval());
        Safe("TestB17SubjectlessTargets", () => TestB17SubjectlessTargets());
        Safe("TestEngineBatch1023", () => TestEngineBatch1023());
        Safe("TestA12FurtherMultiplier", () => TestA12FurtherMultiplier());
        Safe("TestA30ListenerTriggerNames", () => TestA30ListenerTriggerNames());
        Safe("TestW62EntryPump", () => TestW62EntryPump());
        Safe("TestW64ShowCardsWindow", () => TestW64ShowCardsWindow());
        Safe("TestN2UnitConditionRoles", () => TestN2UnitConditionRoles());
        Safe("TestA1EngineFive", () => TestA1EngineFive());
        Safe("TestB3EngineFive", () => TestB3EngineFive());
        Safe("TestB6EngineThird", () => TestB6EngineThird());
        Safe("TestB7EnginePickAndCount", () => TestB7EnginePickAndCount());
        Safe("TestF8EnginePlayOrder", () => TestF8EnginePlayOrder());
        Safe("TestG1bSpiritStone", () => TestG1bSpiritStone());
        Safe("TestG1bStatusApplied", () => TestG1bStatusApplied());
        Safe("TestG2DeathRoutes", () => TestG2DeathRoutes());
        Safe("TestG3AiTargetsTrim", () => TestG3AiTargetsTrim());
        Safe("TestG5BareDealAndReferencedCards", () => TestG5BareDealAndReferencedCards());
        Safe("TestG6MultiCountRepeats", () => TestG6MultiCountRepeats());
        Safe("TestE8PreviousAbilityDrawnCards", () => TestE8PreviousAbilityDrawnCards());
        Safe("TestG10HandCountScope", () => TestG10HandCountScope());
        Safe("TestG12ActiveAbilityLogic", () => TestG12ActiveAbilityLogic());
        Safe("TestEngineE2", () => TestEngineE2());

        VerifyDiagCoverage();

        Debug.Log(P + $"=== 结果：{_pass} 通过 / {_fail} 失败 ===");
        foreach (var f in _failures) Debug.Log(P + "   \u2717 " + f);
        if (Application.isBatchMode)
            UnityEditor.EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }
}
