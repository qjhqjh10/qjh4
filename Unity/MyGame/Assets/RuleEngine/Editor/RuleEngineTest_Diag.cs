// RuleEngineTest_Diag.cs — **诊断入口**：一条用例抛异常不掐死整轮（2026-10-01 加）
//
// 🔴 **本文件由 `工具/gen_diag_runner.py` 生成，别手改** —— 它按 `RuleEngineTest.Run()` 里的
//    **一模一样的顺序**重生成；`Run()` 的用例表一变就要重跑那个脚本。
//
// 为什么要它：`Run()` 是一串直接调用，**任一条抛异常（哪怕是测试自己解引用 null）整轮就断在那儿**，
// 后面几百条根本跑不到 —— 2026-10-01 改棋盘模型时踩到过（`TestAuraSettle` 里一句 `Board[2].Armor`
// 就吃掉了整轮，只看得到 4 条失败）。
//
// 每条包一层 try/catch：抛异常的记一条失败（**带用例名** + 异常类型 + 消息）继续往下跑。
// ⚠️ **它不是替代品**：验收仍然跑 `RuleEngineTest.Run`（那个才是正本）。
using System;
using UnityEngine;

public static partial class RuleEngineTest
{
    static void Safe(string name, Action a)
    {
        try { a(); }
        catch (Exception e)
        {
            _fail++;   // ⚠️ 异常是从 `a()` 里冒出来的，没有 Check 帮它计数 ⇒ 自己记一条
            string line = $"[{name}] 抛异常：{e.GetType().Name} {e.Message}";
            _failures.Add(line);
            Debug.LogError(P + "   \u2717 " + line);
        }
    }

    [UnityEditor.MenuItem("Tools/RuleEngine/规则引擎自检（诊断：单条异常不中断）")]
    public static void RunSafe()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        Debug.Log(P + "=== 规则引擎自检（诊断模式）开始 ===");

        Safe("TestBoardMatchesPresentation", () => TestBoardMatchesPresentation());
        Safe("TestBoardContiguousModel", () => TestBoardContiguousModel());
        Safe("TestKeywordParsing", () => TestKeywordParsing());
        Safe("TestCardDatabase", () => TestCardDatabase());
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
        Safe("TestSpiritStone", () => TestSpiritStone());
        Safe("TestOathAbility", () => TestOathAbility());
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
        Safe("TestCompanion", () => TestCompanion());
        Safe("TestTacticPlay", () => TestTacticPlay());
        Safe("TestDefenceCards", () => TestDefenceCards());
        Safe("TestGraveyardTakeOne", () => TestGraveyardTakeOne());
        Safe("TestOvertime", () => TestOvertime());
        Safe("TestSkirmish", () => TestSkirmish());
        Safe("TestCardInstanceStep1", () => TestCardInstanceStep1());
        Safe("TestCardInstanceStep2", () => TestCardInstanceStep2());
        Safe("TestCardInstanceNewVsMove", () => TestCardInstanceNewVsMove());
        Safe("TestInstanceAcceptanceTarget", () => TestInstanceAcceptanceTarget());
        Safe("TestFactionResources", () => TestFactionResources());
        Safe("TestWhenEventsWidened", () => TestWhenEventsWidened());
        Safe("TestDeckRules", () => TestDeckRules());
        Safe("TestDeckValidation", () => TestDeckValidation());
        Safe("TestDeckIntoBattle", () => TestDeckIntoBattle());
        Safe("TestDeckStoreRoundTrip", () => TestDeckStoreRoundTrip());
        Safe("TestDeckLibrary", () => TestDeckLibrary());
        Safe("TestNewBattle", () => TestNewBattle());
        Safe("TestMulligan", () => TestMulligan());
        Safe("TestBeginTurn", () => TestBeginTurn());
        Safe("TestEnergyIsPerPlayer", () => TestEnergyIsPerPlayer());
        Safe("TestPlayCostRejected", () => TestPlayCostRejected());
        Safe("TestPlaySlotRejected", () => TestPlaySlotRejected());
        Safe("TestPlayOccupiedSlotInserts", () => TestPlayOccupiedSlotInserts());
        Safe("TestPlayOk", () => TestPlayOk());
        Safe("TestMeleeTrade", () => TestMeleeTrade());
        Safe("TestArmorMinimumOne", () => TestArmorMinimumOne());
        Safe("TestShieldBlocks", () => TestShieldBlocks());
        Safe("TestRangedEatsCounter", () => TestRangedEatsCounter());
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
        Safe("TestBacklash", () => TestBacklash());
        Safe("TestPenitence", () => TestPenitence());
        Safe("TestAbility", () => TestAbility());
        Safe("TestAbilityTargetRules", () => TestAbilityTargetRules());
        Safe("TestHealAndDraw", () => TestHealAndDraw());
        Safe("TestEffectChainGuard", () => TestEffectChainGuard());
        Safe("TestStarterCardEffects", () => TestStarterCardEffects());
        Safe("TestAiUsesAbility", () => TestAiUsesAbility());
        Safe("TestAiKillLine", () => TestAiKillLine());
        Safe("TestCreate", () => TestCreate());
        Safe("TestDeploy", () => TestDeploy());
        Safe("TestLowerCost", () => TestLowerCost());
        Safe("TestA5Batch3", () => TestA5Batch3());
        Safe("TestA5Batch4", () => TestA5Batch4());
        Safe("TestChooseCard", () => TestChooseCard());
        Safe("TestReturnAndRefs", () => TestReturnAndRefs());
        Safe("TestPersistentEffects", () => TestPersistentEffects());
        Safe("TestDeployBuffAndCostMore", () => TestDeployBuffAndCostMore());
        Safe("TestUnitDescTriggers", () => TestUnitDescTriggers());
        Safe("TestMobAndRegimentKeyword", () => TestMobAndRegimentKeyword());
        Safe("TestKeywordTriggeredEvents", () => TestKeywordTriggeredEvents());
        Safe("TestFactionResourceEvents", () => TestFactionResourceEvents());
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
        Safe("TestWhenEvents", () => TestWhenEvents());
        Safe("TestEventLayerTail", () => TestEventLayerTail());
        Safe("TestEphemeral", () => TestEphemeral());
        Safe("TestSimultaneousDamage", () => TestSimultaneousDamage());
        Safe("TestFactionMechanics", () => TestFactionMechanics());
        Safe("TestWinnerByWarlord", () => TestWinnerByWarlord());
        Safe("TestDrawIsDraw", () => TestDrawIsDraw());
        Safe("TestForfeit", () => TestForfeit());
        Safe("TestFatigue", () => TestFatigue());
        Safe("TestDeterminism", () => TestDeterminism());
        Safe("TestSignalDeterminism", () => TestSignalDeterminism());
        Safe("TestFullGameWithRealCards", () => TestFullGameWithRealCards());
        Safe("TestFactionBattle", () => TestFactionBattle());
        Safe("TestAiOriginal", () => TestAiOriginal());

        Debug.Log(P + $"=== 结果：{_pass} 通过 / {_fail} 失败 ===");
        foreach (var f in _failures) Debug.Log(P + "   \u2717 " + f);
        if (Application.isBatchMode)
            UnityEditor.EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }
}
