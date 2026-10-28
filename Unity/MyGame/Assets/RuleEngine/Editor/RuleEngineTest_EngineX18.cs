// RuleEngineTest_EngineX18.cs — 执行代理 `X18`（2026-10-25 · 界面线）的回归断言：
//   **成就计数源补完**（隐秘/破坏 · 开包 · 轮抽战绩 · 卡池收集 · 「按 Ach_N 写」不串台）
//
// 判据（⛔ 不要重新论证）= `资料/待办判据_成就计数器_1025b.md`：
//   · §B-3 #14 隐秘/破坏（`Ach_78` / `Ach_65`）—— 落点 = `EffectResolver` 里**三处造隐秘**
//     （`DoCreate` 手牌支 · `DA44` 的 `todeck` 支 · 任务点满阈值入牌库）＋ **一处造破坏**（`enemyhand` 支）；
//   · §B-3 #7 开包（`Ach_15`）= `BoosterPackOpenWindow.Open`（UI 窗，本宿主跑不了 ⇒ 只断「写点 id 指向谁」，
//     端到端由 `ShopScene.Run` 验）；
//   · §B-3 #9 轮抽战绩（`Ach_20`）= `DraftModeWindow.UpdateState` 的收尾分支（同上，端到端宿主 = `MainMenuScene.Run`
//     里的 `DraftAssertD3`）；本文件断「`highestValue` 语义 + 阈值表」；
//   · §五 `Ach_24`（收集牌）—— 全解锁口径：**首次加载档案时把卡池张数记一次**（幂等、只涨不落）；
//   · §F5 三组 challenge 原文重名 ⇒ **必须按 `Ach_N` 写**，各自独立涨；
//   · §F4 `Ach_97` 数据自相矛盾（`anyTrait=1` 但 `withTrait=[610]`）—— 已按 `withTrait` 意图实现，这里确认它在跑。
//
// 🔴 **灭自证**：每一对「该涨 / 不该涨」都**两条一起断** —— 把实现改回旧写法（共用一个键 / 累加实现）
//    不可能同时绿。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批**唯一入口**（`RuleEngineTest.Run()` 里一行 `Step(TestEngineX18);`）。</summary>
    public static void TestEngineX18()
    {
        Section("🆕 执行代理 `X18`：成就计数源 —— 隐秘/破坏 · 开包 · 轮抽战绩 · 卡池收集 · 按 Ach_N 不串台");
        // 🔴 不碰玩家真档：批处理本来就不落盘（`ProfileData.EnsureSave` 的 `Application.isBatchMode` 门），
        //    这里再把路径改到临时文件 —— 双保险（同 `RuleEngineTest_EngineX2` 的做法）。
        string tmp = Path.Combine(Path.GetTempPath(), "wf_x18_" + Guid.NewGuid().ToString("N"));
        string oldPath = ProfileData.SavePathOverrideForTest;
        try
        {
            Directory.CreateDirectory(tmp);
            ProfileData.SavePathOverrideForTest = Path.Combine(tmp, "profile.json");

            X18T1SecretAndSabotage();
            X18T2ContainersOpenAndDraftWins();
            X18T3CardPoolCollected();
            X18T4AchievementIdIsolation();
            X18T5Ach89AndAch97();
            X18T6HuntMarkAndDarkPact();
            X18T7Reanimated();
            X18T8Swarm();
            X18T9CompleteCampaign();
        }
        catch (Exception e)
        {
            CheckTrue(false, "X18 断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            ProfileData.SavePathOverrideForTest = oldPath;
            try { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); } catch { /* 临时目录删不掉不影响结论 */ }
        }
    }

    // ==================================================================================================
    //  ① 隐秘 / 破坏（判据 §B-3 #14）：`Tally.SecretsMade` / `SabotageMade` → `Ach_78` / `Ach_65`
    // ==================================================================================================

    /// <summary>**隐秘那一支**走「任务点满阈值 → 造隐秘入牌库」（`EffectResolver` 的第三处写点）。
    /// ⚠️ 另两处（`DoCreate` 的手牌支 / `DA44` 的 `todeck` 支）与本处**同一个判据**
    /// （原版 `BroadcastSecretCreated` 的同一批调用点），都写了 `AddSecret`；这里只验一跳。</summary>
    static void X18T1SecretAndSabotage()
    {
        var secretDa = new CardDef("X18SecretDA", "X18 Secret DA", "tactic", "x", "common", "DarkAngels",
                                   2, 0, 0, 0, null, subtype: "Secret");
        var pool = new List<CardDef> { secretDa };
        var gain3 = Tactic("T_X18Quest3", 0, "Gain 3 任务点");
        var ctx = QuestFixture(pool, "X18Warlord", "DarkAngels", 20261025, gain3);
        ToP1Turn(ctx, 1);
        ctx.Players[0].Energy = 12;

        CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_X18Quest3"), -1), RuleCodes.OK,
                  "X18-1a 打出「Gain 3 任务点」（满阈值 ⇒ 造 1 张隐秘入牌库）");
        Check(ctx.Tally.SecretsOf(0), 1, "X18-1b ★ 造出 1 张隐秘 ⇒ `Tally.SecretsOf(我方) == 1`");
        Check(ctx.Tally.SabotageOf(0), 0, "X18-1c ★ 同一跳 **不**记破坏（两条成就各认各的格）");

        int b78 = ProfileData.AchievementValue("Ach_78");
        int b65 = ProfileData.AchievementValue("Ach_65");
        ProfileData.ApplyAchievementTally(ctx.Tally, 0, 0, 0, 0, GameMode.OfflinePractice, null, false, false);
        Check(ProfileData.AchievementValue("Ach_78"), b78 + 1, "X18-1d `Ach_78`（`Create Secrets`）+1");
        Check(ProfileData.AchievementValue("Ach_65"), b65, "X18-1e ★ `Ach_65`（`Add Sabotage cards`）**不动**");

        // ---- 破坏：`Create a random Sabotage in the enemy hand`（`DoCreate` 的 `enemyhand` 支）----
        var tac = Tactic("T_X18Sab", 0, "Create a random Sabotage in the enemy hand");
        var sab = new CardDef("X18Sabotage", "X18Sabotage", "tactic", "", "common", "Test",
                              0, 0, 0, 0, null, subtype: "Sabotage", spellType: 230);
        // 🔴 2026-10-29（`B94_FX8` · DG1 γ3）：`B94_B14` 把「破坏」的判据从 **subtype 词**改成**数据面**
        //   `rawCardData.spellType == 0xe6(230)`（`EffectResolver.cs:4216`，判据 `_ResolveCreateHandCard_d__512__MoveNext.c:90`）。
        //   夹具只写 `subtype: "Sabotage"` 时 `CardDef` ctor 的 `spellType` 缺省是 **-1**（`CardDef.cs:912`）⇒ 闸不成立
        //   ⇒ `AddSabotage` 根本没被调到 ⇒ 下面两条「1 / +1」一起红。**先硬检查前提，别让它静默跳过**。
        Check(sab.SpellType, 230, "夹具前提：破坏卡的数据面 `spellType = 230`（⛔ 缺了它 `AddSabotage` 不会被调到）");
        var ctx2 = BattlePool(new[] { tac }, new[] { Unit("X18FoeSab", 1, 1, 9) },
                              new List<CardDef> { tac, sab });
        ToP1Turn(ctx2, 1);
        CheckCode(RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_X18Sab"), -1), RuleCodes.OK,
                  "X18-1f 打出「Create a random Sabotage in the enemy hand」");
        Check(ctx2.Tally.SabotageOf(0), 1,
              "X18-1g ★ 造出 1 张破坏 ⇒ `Tally.SabotageOf(我方) == 1`（归属 = **造牌者**，⛔ 不是收牌的敌方）");
        Check(ctx2.Tally.SecretsOf(0), 0, "X18-1h ★ 同一跳 **不**记隐秘");

        int b65b = ProfileData.AchievementValue("Ach_65");
        int b78b = ProfileData.AchievementValue("Ach_78");
        ProfileData.ApplyAchievementTally(ctx2.Tally, 0, 0, 0, 0, GameMode.OfflinePractice, null, false, false);
        Check(ProfileData.AchievementValue("Ach_65"), b65b + 1, "X18-1i `Ach_65`（`Add Sabotage cards`）+1");
        Check(ProfileData.AchievementValue("Ach_78"), b78b,
              "X18-1j ★ `Ach_78` **不动**（灭自证：两条若共用一格计数，这一对不可能同时绿）");
    }

    // ==================================================================================================
    //  ② 开包（`Ach_15`）/ 轮抽战绩（`Ach_20`）—— 两个 UI 落点的**数据面**与 `highestValue` 语义
    //
    //  ⚠️ 端到端：`Ach_15` = `BoosterPackOpenWindow.Open`（宿主人 `ShopScene.Run`）；
    //            `Ach_20` = `DraftModeWindow.UpdateState` 收尾分支（宿主 `MainMenuScene.Run` 的 `DraftAssertD3`）。
    //            本文件跑在**引擎侧**、建不出那两个窗 ⇒ 这里只断「写点 id 指向谁 / 阈值 / 只涨不落」。
    // ==================================================================================================
    static void X18T2ContainersOpenAndDraftWins()
    {
        var a15 = ProfileData.AchievementById(ProfileData.IdAchContainersOpen);
        CheckTrue(a15 != null && a15.Class == "ContainersOpen",
                  "X18-2a ★ 开包写点指向 `Ach_15`，且它的原版触发源类名是 `ContainersOpen`（`containerType=1`）");
        Check(ProfileData.IdAchContainersOpen, "Ach_15", "X18-2b 常量与成就 id 一致（改 id 时这一条会红）");

        // ---- `Ach_15` 走「对局外」写点：+1 就是 +1（不是 +2/不加）----
        int b15 = ProfileData.AchievementValue(ProfileData.IdAchContainersOpen);
        CheckTrue(ProfileData.GainAchievementOutsideMatch(ProfileData.IdAchContainersOpen, 1),
                  "X18-2c 开一个包 ⇒ 写点返回 `true`");
        Check(ProfileData.AchievementValue(ProfileData.IdAchContainersOpen), b15 + 1,
              "X18-2d ★ 开一个包 ⇒ `Ach_15` **恰好 +1**");

        var a20 = ProfileData.AchievementById(ProfileData.IdAchDraftWins);
        CheckTrue(a20 != null && a20.Class == "DraftWins",
                  "X18-2e ★ 轮抽战绩写点指向 `Ach_20`，类名 `DraftWins`");
        CheckTrue(a20 != null && a20.Thresholds != null && a20.Thresholds.Length == 5
                  && a20.Thresholds[0] == 3 && a20.Thresholds[4] == 12,
                  "X18-2f `Ach_20` 阈值 = 3 / 6 / 8 / 10 / 12（判据 §B-3 #9：**一次 run 内的胜场**）");

        // ---- `highestValue` 语义：只涨不落（同一天/同一个 run 反复收尾不会累加）----
        int cur = ProfileData.AchievementValue(ProfileData.IdAchDraftWins);
        ProfileData.SetAchievementValueAtLeast(ProfileData.IdAchDraftWins, cur + 2);
        Check(ProfileData.AchievementValue(ProfileData.IdAchDraftWins), cur + 2, "X18-2g 抬到 cur+2 ⇒ 就是 cur+2");
        ProfileData.SetAchievementValueAtLeast(ProfileData.IdAchDraftWins, cur + 1);
        Check(ProfileData.AchievementValue(ProfileData.IdAchDraftWins), cur + 2,
              "X18-2h ★ 再喂一个更小的值（cur+1）⇒ **不动**（⛔ 不许把「只涨不落」实现成累加/覆盖）");
        ProfileData.SetAchievementValueAtLeast(ProfileData.IdAchDraftWins, cur + 3);
        Check(ProfileData.AchievementValue(ProfileData.IdAchDraftWins), cur + 3, "X18-2i 更大的值 ⇒ 抬上去");
    }

    // ==================================================================================================
    //  ③ `Ach_24`（收集牌 · 全解锁口径）：进度 == 卡表 `count` 的**现读值**，且幂等
    //
    //  ★ 灭自证：期望值从 `cards_engine.json` **自己读一遍**（⛔ 不从 `CardDatabase.Load()` 反算 ——
    //    那会和被测实现同源）；写死 1127 也不行（卡池会变）。
    // ==================================================================================================
    static void X18T3CardPoolCollected()
    {
        int assetCount = -1;
        try
        {
            string p = Path.Combine(Application.dataPath, "RuleEngine/Resources/cards_engine.json");
            string json = File.ReadAllText(p);
            var m = Regex.Match(json, "\"count\"\\s*:\\s*(\\d+)");
            if (m.Success) assetCount = int.Parse(m.Groups[1].Value);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[X18] 读 `cards_engine.json` 的 `count` 失败：" + e.Message);
        }
        CheckTrue(assetCount > 0, "X18-3a（前提）从卡表原文读到了 `count`（现读 " + assetCount + "）");
        if (assetCount <= 0) return;

        int delta = ProfileData.RecordCardPoolCollectedOnce();
        Check(ProfileData.AchievementValue(ProfileData.IdAchCardPool), assetCount,
              "X18-3b ★ `Ach_24` 进度 == 卡表 `count` 的现读值（全解锁口径；⛔ 不写死张数）");
        Check(delta, assetCount, "X18-3c ★ 首次记 ⇒ 差值 == 卡池张数（" + assetCount + "）");

        int delta2 = ProfileData.RecordCardPoolCollectedOnce();
        Check(delta2, 0, "X18-3d ★ 幂等：再调一次 ⇒ 差值 0");
        Check(ProfileData.AchievementValue(ProfileData.IdAchCardPool), assetCount,
              "X18-3e ★ …进度**仍是**卡池张数（灭自证：若写成 `AddAchievementValue` ⇒ 第二次会翻倍）");
    }

    // ==================================================================================================
    //  ④ 判据 §F5：三组 challenge 原文**重名** ⇒ 只能按 `Ach_N` 写，各自独立涨
    //
    //  ★ 灭自证：这三组若改回 `FindIdByChallenge(原文)`，一组里的两条会**一起涨** ⇒ 每组的
    //    「另一条不动」断言立刻红。
    // ==================================================================================================
    static void X18T4AchievementIdIsolation()
    {
        // ---- ① `Upgrade Sautekh Cards T2` = `Ach_27`(army 40) + `Ach_29`(army 30) ----
        var a27 = ProfileData.AchievementById("Ach_27");
        var a29 = ProfileData.AchievementById("Ach_29");
        CheckTrue(a27 != null && a29 != null && string.Equals(a27.ChallengeId, a29.ChallengeId, StringComparison.Ordinal),
                  "X18-4a ★ `Ach_27` / `Ach_29` 的 challenge 原文**同名**（重名前提成立，实为 `"
                  + (a27 != null ? a27.ChallengeId : "?") + "`）");
        int b27 = ProfileData.AchievementValue("Ach_27");
        int b29 = ProfileData.AchievementValue("Ach_29");
        ProfileData.ApplyAchievementUpgrade("Sautekh", 2);
        Check(ProfileData.AchievementValue("Ach_27"), b27 + 1, "X18-4b 索泰克升 Lv2 ⇒ `Ach_27`（army=40）+1");
        Check(ProfileData.AchievementValue("Ach_29"), b29, "X18-4c ★ …而 `Ach_29`（army=30 = SaimHann）**不动**");
        ProfileData.ApplyAchievementUpgrade("SaimHann", 2);
        Check(ProfileData.AchievementValue("Ach_29"), b29 + 1, "X18-4d 萨姆-罕升 Lv2 ⇒ `Ach_29` +1");
        Check(ProfileData.AchievementValue("Ach_27"), b27 + 1, "X18-4e ★ …而 `Ach_27` **仍只有第一跳那一次**");

        // ---- ② `Win ranked matches as EC` = `Ach_94`(EC 120) + `Ach_101`(SW 130) ----
        var a94 = ProfileData.AchievementById("Ach_94");
        var a101 = ProfileData.AchievementById("Ach_101");
        CheckTrue(a94 != null && a101 != null && string.Equals(a94.ChallengeId, a101.ChallengeId, StringComparison.Ordinal),
                  "X18-4f ★ `Ach_94` / `Ach_101` 的 challenge 原文**同名**（实为 `"
                  + (a94 != null ? a94.ChallengeId : "?") + "`）");
        var t = new AchievementTally();
        int b94 = ProfileData.AchievementValue("Ach_94");
        int b101 = ProfileData.AchievementValue("Ach_101");
        ProfileData.ApplyAchievementTally(t, 0, 0, 0, 0, GameMode.Classic, "EmperorsChildren", true, false);
        Check(ProfileData.AchievementValue("Ach_94"), b94 + 1, "X18-4g 帝皇之子赢排位 ⇒ `Ach_94` +1");
        Check(ProfileData.AchievementValue("Ach_101"), b101, "X18-4h ★ …而 `Ach_101`（太空野狼）**不动**");
        ProfileData.ApplyAchievementTally(t, 0, 0, 0, 0, GameMode.Classic, "SpaceWolves", true, false);
        Check(ProfileData.AchievementValue("Ach_101"), b101 + 1, "X18-4i 太空野狼赢排位 ⇒ `Ach_101` +1");
        Check(ProfileData.AchievementValue("Ach_94"), b94 + 1, "X18-4j ★ …而 `Ach_94` **仍只有第一跳那一次**");

        // ---- ③ `Win draft matches as EC` = `Ach_95`(EC 120) + `Ach_102`(SW 130) ----
        var a95 = ProfileData.AchievementById("Ach_95");
        var a102 = ProfileData.AchievementById("Ach_102");
        CheckTrue(a95 != null && a102 != null && string.Equals(a95.ChallengeId, a102.ChallengeId, StringComparison.Ordinal),
                  "X18-4k ★ `Ach_95` / `Ach_102` 的 challenge 原文**同名**（实为 `"
                  + (a95 != null ? a95.ChallengeId : "?") + "`）");
        int b95 = ProfileData.AchievementValue("Ach_95");
        int b102 = ProfileData.AchievementValue("Ach_102");
        ProfileData.ApplyAchievementTally(t, 0, 0, 0, 0, GameMode.ClosedDeck, "EmperorsChildren", true, false);
        Check(ProfileData.AchievementValue("Ach_95"), b95 + 1, "X18-4l 帝皇之子赢轮抽 ⇒ `Ach_95` +1");
        Check(ProfileData.AchievementValue("Ach_102"), b102, "X18-4m ★ …而 `Ach_102`（太空野狼）**不动**");
        ProfileData.ApplyAchievementTally(t, 0, 0, 0, 0, GameMode.ClosedDeck, "SpaceWolves", true, false);
        Check(ProfileData.AchievementValue("Ach_102"), b102 + 1, "X18-4n 太空野狼赢轮抽 ⇒ `Ach_102` +1");
        Check(ProfileData.AchievementValue("Ach_95"), b95 + 1, "X18-4o ★ …而 `Ach_95` **仍只有第一跳那一次**");
    }

    // ==================================================================================================
    //  ⑤ `Ach_89`（战斗药剂）/ `Ach_97`（带 pack 的部队）—— 确认**真在跑**，并把「判据不闭合」钉在断言上
    //
    //  · `Ach_89`：`anyCardType=0` 却**没有卡型值字段** ⇒ 已按 `spellType=260 ∧ type=="tactic"` 合取实现
    //    （判据 §B-3 #3 / `RuleCore.CardPlayedTagMask`）；
    //  · `Ach_97`：`anyTrait=1` 但 `withTrait=[610]` **自相矛盾** ⇒ 已按 `withTrait` 的**意图**（pack）实现
    //    （判据 §F4，如实标为「按意图、不是按原版那道闸」）。
    // ==================================================================================================
    static void X18T5Ach89AndAch97()
    {
        var f89 = ProfileData.FiltersOf(ProfileData.AchievementById("Ach_89"));
        CheckTrue(f89 != null && f89.anySpellType == 0 && f89.spellType == 260 && f89.anyCardType == 0,
                  "X18-5a ★ `Ach_89` 的判据：`spellType=260` + `anyCardType=0`（**没有卡型值字段** ⇒ 数据本身不闭合）");
        var f97 = ProfileData.FiltersOf(ProfileData.AchievementById("Ach_97"));
        bool has610 = false;
        if (f97 != null && f97.withTrait != null)
            for (int i = 0; i < f97.withTrait.Length; i++) if (f97.withTrait[i] == 610) has610 = true;
        CheckTrue(f97 != null && f97.anyTrait == 1 && has610,
                  "X18-5b ★ `Ach_97` 的判据**自相矛盾**（`anyTrait=1` 但 `withTrait=[610]`）—— 已按 `withTrait` 意图实现");

        // ---- `Ach_89`：一张 `spellType=260` 的**计谋**打出去 ⇒ 只涨它自己 ----
        var elixir = new CardDef("X18Elixir", "X18Elixir", "tactic", "Deal 1 damage to an enemy troop",
                                 "common", "Test", 1, 0, 0, 0, null, spellType: 260);
        var ctx = Battle(new[] { elixir }, new CardDef[0]);
        ToP1Turn(ctx, 1);
        Place(ctx, 1, 5, Unit("X18FoeElixir", 1, 1, 9));
        CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "X18Elixir"), 5), RuleCodes.OK,
                  "X18-5c 打出一张 `spellType=260` 的计谋（战斗药剂）");
        Check(ctx.Tally.CardsPlayedOf(0, CardPlayedTag.ElixirTactic), 1,
              "X18-5d ★ 合取标签 `ElixirTactic`（260 ∧ tactic）== 1");
        Check(ctx.Tally.CardsPlayedOf(0, CardPlayedTag.Pack), 0, "X18-5e ★ …而 `Pack` 那格 0（各落各的标签）");

        int b89 = ProfileData.AchievementValue("Ach_89");
        int b97 = ProfileData.AchievementValue("Ach_97");
        ProfileData.ApplyAchievementTally(ctx.Tally, 0, 0, 0, 0, GameMode.OfflinePractice, null, false, false);
        Check(ProfileData.AchievementValue("Ach_89"), b89 + 1, "X18-5f `Ach_89`（`Play Combat Elixirs`）+1");
        Check(ProfileData.AchievementValue("Ach_97"), b97, "X18-5g ★ …而 `Ach_97`（带 pack 的部队）**不动**");

        // ---- `Ach_97`：一张带 `pack` 的**部队**打出去 ⇒ 只涨它自己 ----
        var packUnit = new CardDef("X18PackUnit", "X18PackUnit", "unit", "", "common", "Test",
                                   1, 1, 5, 0, new[] { "Pack" });
        var ctx2 = Battle(new[] { packUnit }, new CardDef[0]);
        ToP1Turn(ctx2, 1);
        CheckCode(RuleCore.PlayCard(ctx2, 0, HandIdx(ctx2, 0, "X18PackUnit"), 1), RuleCodes.OK,
                  "X18-5h 打出一张带 `pack` 的部队");
        Check(ctx2.Tally.CardsPlayedOf(0, CardPlayedTag.Pack), 1, "X18-5i ★ `Pack` 标签 == 1");
        Check(ctx2.Tally.CardsPlayedOf(0, CardPlayedTag.ElixirTactic), 0, "X18-5j ★ …而 `ElixirTactic` 那格 0");

        int b97b = ProfileData.AchievementValue("Ach_97");
        int b89b = ProfileData.AchievementValue("Ach_89");
        ProfileData.ApplyAchievementTally(ctx2.Tally, 0, 0, 0, 0, GameMode.OfflinePractice, null, false, false);
        Check(ProfileData.AchievementValue("Ach_97"), b97b + 1, "X18-5k `Ach_97`（`Deploy troops with Pack`）+1");
        Check(ProfileData.AchievementValue("Ach_89"), b89b, "X18-5l ★ …而 `Ach_89` **不动**");
    }

    // ==================================================================================================
    //  ⑥ 猎杀标记（`Ach_96`）/ 黑暗契约（`Ach_6`）—— 判据 §B-3 #12 / #13
    //
    //  🔴 判据 §八 N3 原来写「授予点拿不到施放者座位」—— **不成立**（本轮已就地更正）：
    //     `EffectResolver.ApplyOneGain(ctx, owner, …)` 与 `GrantDarkPactCore(ctx, owner, …)` 的
    //     `owner` 就是施放者（调用点 = `DoGive` 的目标循环）。
    //  ★ 灭自证：两条**成对**断「自己涨 / 另一条不动」；删掉任一计数 ⇒ 自己那条红。
    // ==================================================================================================
    static void X18T6HuntMarkAndDarkPact()
    {
        // ---- ① 猎杀标记：`Give Hunt Mark to an enemy troop` ----
        var hm = Tactic("T_X18HM", 0, "Give Hunt Mark to an enemy troop");
        var ctx = ProbeBattle(new[] { hm }, new CardDef[0]);
        ToP1Turn(ctx, 2);
        Place(ctx, 1, 5, Unit("X18HMVictim", 1, 0, 9), exhausted: true);
        CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_X18HM"), 5), RuleCodes.OK,
                  "X18-6a 打出 `Give Hunt Mark to an enemy troop`");
        Check(ctx.Tally.HuntMarksOf(0), 1, "X18-6b ★ 授予 1 份猎杀标记 ⇒ `HuntMarksGiven[我方] == 1`");
        Check(ctx.Tally.MarksOfChaosOf(0), 0, "X18-6c ★ 同一跳 **不**记黑暗契约");
        Check(ctx.Tally.ReanimatedOf(0), 0, "X18-6d ★ …也不记再造 / 暴群（防串台）");
        Check(ctx.Tally.SwarmMergesOf(0), 0, "X18-6d ★ …暴群那格同样 0");

        int b96 = ProfileData.AchievementValue("Ach_96");
        int b6 = ProfileData.AchievementValue("Ach_6");
        int b14 = ProfileData.AchievementValue("Ach_14");
        int b10 = ProfileData.AchievementValue("Ach_10");
        ProfileData.ApplyAchievementTally(ctx.Tally, 0, 0, 0, 0, GameMode.OfflinePractice, null, false, false);
        Check(ProfileData.AchievementValue("Ach_96"), b96 + 1, "X18-6e `Ach_96`（`Give Hunt Marks`）+1");
        Check(ProfileData.AchievementValue("Ach_6"), b6, "X18-6f ★ `Ach_6`（Dark Pact）**不动**");
        Check(ProfileData.AchievementValue("Ach_14"), b14, "X18-6g ★ `Ach_14`（Reanimated）**不动**");
        Check(ProfileData.AchievementValue("Ach_10"), b10, "X18-6g ★ `Ach_10`（Swarm）**不动**");

        // ---- ② 黑暗契约：`Give a Dark Pact to a friendly troop` ----
        var dp = Tactic("T_X18DP", 0, "Give a Dark Pact to a friendly troop");
        var ctx2 = ProbeBattle(new[] { dp }, new CardDef[0]);
        ToP1Turn(ctx2, 2);
        Place(ctx2, 0, 3, Unit("X18DPFriendly", 1, 1, 5), exhausted: true);
        CheckCode(RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_X18DP"), 3), RuleCodes.OK,
                  "X18-6h 打出 `Give a Dark Pact to a friendly troop`");
        Check(ctx2.Tally.MarksOfChaosOf(0), 1, "X18-6i ★ 授予 1 份黑暗契约 ⇒ `MarksOfChaosGiven[我方] == 1`");
        Check(ctx2.Tally.HuntMarksOf(0), 0,
              "X18-6j ★ 同一跳 **不**记猎杀标记（灭自证：两条若共用一格计数，6c 与 6j 不可能同时绿）");

        int b6b = ProfileData.AchievementValue("Ach_6");
        int b96b = ProfileData.AchievementValue("Ach_96");
        ProfileData.ApplyAchievementTally(ctx2.Tally, 0, 0, 0, 0, GameMode.OfflinePractice, null, false, false);
        Check(ProfileData.AchievementValue("Ach_6"), b6b + 1, "X18-6k `Ach_6`（`Give a Dark Pact`）+1");
        Check(ProfileData.AchievementValue("Ach_96"), b96b, "X18-6l ★ `Ach_96` **不动**");
    }

    // ==================================================================================================
    //  ⑦ 再造（`Ach_14` `UnitReanimated`）—— 判据 §B-3 #15
    //
    //  ★ 灭自证：①「死成残骸」**不算**再造（0）；② 真翻回来才算（1）；③ 删掉计数 ⇒ ② 红。
    //  ⚠️ 原版那一格 `unit+0x40 != 0` 的过滤语义没解出来（反编译撞名）⇒ 按「翻了就算」实现，**近似**。
    // ==================================================================================================
    static void X18T7Reanimated()
    {
        var rem = new CardDef("X18Remnant", "X18Remnant", "unit", "", "common", "Test",
                              1, 2, 5, 0, new[] { KeywordTable.Remnant }, subtype: "Infantry");
        var kill = Tactic("T_X18RemKill", 0, "Deal 99 damage to a friendly unit");
        var raise = Tactic("T_X18Raise", 0, "Reanimate a friendly Remnant");
        var ctx = ProbeBattle(new[] { rem, kill, raise }, new[] { Unit("X18FoeRem", 1, 0, 9) });
        ToP1Turn(ctx, 2);
        Place(ctx, 0, 3, rem, exhausted: true);
        CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_X18RemKill"), 3), RuleCodes.OK,
                  "X18-7a 把带 `Remnant` 的单位打死（翻面成残骸）");
        CheckTrue(Board(ctx, 0, 3) != null && Board(ctx, 0, 3).IsRemnant, "X18-7b（前提）3 号格上是残骸");
        Check(ctx.Tally.ReanimatedOf(0), 0, "X18-7c ★ **死成残骸不算再造**（0）");

        CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_X18Raise"), -1), RuleCodes.OK,
                  "X18-7d 打出 `Reanimate a friendly Remnant`");
        Check(ctx.Tally.ReanimatedOf(0), 1, "X18-7e ★ 翻回 1 个 ⇒ `ReanimatedCount[我方] == 1`");
        Check(ctx.Tally.SwarmMergesOf(0), 0, "X18-7f ★ …暴群那格 0（防串台）");

        int b14 = ProfileData.AchievementValue("Ach_14");
        int b10 = ProfileData.AchievementValue("Ach_10");
        ProfileData.ApplyAchievementTally(ctx.Tally, 0, 0, 0, 0, GameMode.OfflinePractice, null, false, false);
        Check(ProfileData.AchievementValue("Ach_14"), b14 + 1, "X18-7g `Ach_14`（`Reanimate Remnant`）+1");
        Check(ProfileData.AchievementValue("Ach_10"), b10, "X18-7h ★ `Ach_10`（Swarm）**不动**");
    }

    // ==================================================================================================
    //  ⑧ 暴群（`Ach_10` `UnitSwarmCount`）—— 判据 §B-3 #15
    //
    //  🔴 判据为什么不闭合 / 为什么放 `SwarmMergeNow`：见 `AchievementTally.SwarmMerges` 的 doc
    //     （原版 `UnitSwarmCount__OnUnitSwarm.c` 方法体撞名丢失 ⇒ 取**「真合并了才算」**）。
    //  ★ 灭自证：右邻**不带 `swarm`** ⇒ 不合并（X12 刚补的那一半判据）⇒ 计数**不涨**。
    // ==================================================================================================
    static void X18T8Swarm()
    {
        CardDef SwarmCard(string n)
        {
            return new CardDef(n, n, "unit", "", "common", "Test", 1, 2, 3, 0,
                               new[] { KeywordTable.Swarm }, subtype: "Infantry");
        }
        CardDef PlainCard(string n)
        {
            return new CardDef(n, n, "unit", "", "common", "Test", 1, 2, 3, 0,
                               null, subtype: "Infantry");
        }

        // ---- ① 真合并 ⇒ +1 ----
        {
            var host = SwarmCard("X18Guy");
            var ctx = ProbeBattle(new[] { SwarmCard("X18Guy") }, new[] { Unit("X18Foe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, host, exhausted: true);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "X18Guy"), 2), RuleCodes.OK,
                      "X18-8a 把带 `swarm` 的同名部队打到右邻也带 `swarm` 的那张左边（会合并）");
            Check(Board(ctx, 0, 3).Attack, 4, "X18-8b（前提）合并真的发生了（2 + 2 = 4 攻）");
            Check(ctx.Tally.SwarmMergesOf(0), 1, "X18-8c ★ 合并 1 次 ⇒ `SwarmMerges[我方] == 1`");
            Check(ctx.Tally.ReanimatedOf(0), 0, "X18-8d ★ …再造那格 0（防串台）");

            int b10 = ProfileData.AchievementValue("Ach_10");
            int b14 = ProfileData.AchievementValue("Ach_14");
            ProfileData.ApplyAchievementTally(ctx.Tally, 0, 0, 0, 0, GameMode.OfflinePractice, null, false, false);
            Check(ProfileData.AchievementValue("Ach_10"), b10 + 1, "X18-8e `Ach_10`（`Trigger Swarm`）+1");
            Check(ProfileData.AchievementValue("Ach_14"), b14, "X18-8f ★ `Ach_14`（Reanimated）**不动**");
        }

        // ---- ② ★ 灭自证：右邻不带 `swarm` ⇒ 不合并 ⇒ **不涨** ----
        {
            var plain = PlainCard("X18Guy2");
            var ctx = ProbeBattle(new[] { SwarmCard("X18Guy2") }, new[] { Unit("X18Foe2", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, plain, exhausted: true);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "X18Guy2"), 2), RuleCodes.OK,
                      "X18-8g 把带 `swarm` 的那张打到**右邻不带 `swarm`** 的同名部队左边");
            Check(ctx.Tally.SwarmMergesOf(0), 0,
                  "X18-8h ★★ **没合并 ⇒ 不计数**（若把计数挪到 `TrySwarmMerge` 入队那一跳 ⇒ 这里会变 1 ⇒ 红）");
        }
    }

    // ==================================================================================================
    //  ⑨ 战役全通（`Ach_16` `CompleteCampaign`）—— 判据 §B-3 #8
    //
    //  口径（调度台 2026-10-25 定）：**47 个节点的基础档全领** ⇒ `SetAtLeast("Ach_16", 1)`
    //  （`checkAlreadyCompleted=1` = 只记一次；天然幂等，**不依赖** F6「战役进度不落盘」）。
    //  ★ 灭自证：
    //    · 9b = 只领 1 个节点 ⇒ **不动**（防「一开局就满」/「领一个涨一次」）；
    //    · 9g = 把进度重置（模拟重开游戏）后再全通一次 ⇒ **仍 +1**（改成 `AddAchievementValue` ⇒ 变 +2 ⇒ 红）。
    // ==================================================================================================
    static void X18T9CompleteCampaign()
    {
        CampaignData.ResetForTest();
        int b16 = ProfileData.AchievementValue(ProfileData.IdAchCompleteCampaign);

        // ---- ① 没领全 ⇒ 不涨（★ 最重要的一条）----
        string why;
        CheckTrue(CampaignData.Claim(0, CampaignData.TierBasic, out why),
                  "X18-9a 领 1 个节点（根 `UM0`）");
        Check(ProfileData.AchievementValue(ProfileData.IdAchCompleteCampaign), b16,
              "X18-9b ★ 只领了 1/" + CampaignData.NodeCount + " 个节点 ⇒ `Ach_16` **不动**"
            + "｜🧨 把「全领」判据改成无条件 ⇒ 这里 +1 ⇒ 红；改成「每领一个 +1」⇒ 这里也会 +1 ⇒ 红");

        // ---- ② 全领 ⇒ +1 ----
        int claimed = ClaimAllCampaignBase();
        Check(claimed, CampaignData.NodeCount,
              "X18-9c（前提）基础档**全部**领完（" + CampaignData.NodeCount + " 个）");
        Check(ProfileData.AchievementValue(ProfileData.IdAchCompleteCampaign), b16 + 1,
              "X18-9d ★ 战役全通 ⇒ `Ach_16` +1（`SetAchievementValueAtLeast(…,1)`）");

        // ---- ③ 幂等：模拟「重开游戏」（战役进度归零）再全通一次 ⇒ 仍是 +1 ----
        CampaignData.ResetForTest();
        Check(ProfileData.AchievementValue(ProfileData.IdAchCompleteCampaign), b16 + 1,
              "X18-9e（前提）重置战役进度**不动**成就（成就自己落在 `profile.json` 里，不依赖战役存档）");
        int claimed2 = ClaimAllCampaignBase();
        Check(claimed2, CampaignData.NodeCount, "X18-9f（前提）第二次也全领完");
        Check(ProfileData.AchievementValue(ProfileData.IdAchCompleteCampaign), b16 + 1,
              "X18-9g ★★ 再全通一次 ⇒ **仍是 +1**（`checkAlreadyCompleted=1` / `highestValue` 语义）"
            + "｜🧨 改成 `AddAchievementValue` ⇒ 这里变 +2 ⇒ 红");

        // ---- ④ 纯函数那一半：没全领时如实答 `false`，且不写进度 ----
        CampaignData.ResetForTest();
        Check(CampaignData.NotifyCampaignCompletionIfAllClaimed(), false,
              "X18-9h ★ 没全领 ⇒ `NotifyCampaignCompletionIfAllClaimed()` 返回 `false`");
        Check(ProfileData.AchievementValue(ProfileData.IdAchCompleteCampaign), b16 + 1,
              "X18-9i ★ …而且进度**没有**被写（仍 `+1`）");

        CampaignData.ResetForTest();     // 收尾：别把 47 个节点的进度留给别的自检
    }

    /// <summary>X18-9 的辅助：按节点表顺序把**基础档**全领完。
    /// `Nodes` 是拓扑序（下标即前置序）⇒ 一趟就够；这里仍跑两趟兜底，返回已领格数。</summary>
    static int ClaimAllCampaignBase()
    {
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < CampaignData.NodeCount; i++)
                if (!CampaignData.BaseClaimed(i))
                {
                    string why;
                    CampaignData.Claim(i, CampaignData.TierBasic, out why);
                }
        return CampaignData.ClaimedCount;
    }
}
