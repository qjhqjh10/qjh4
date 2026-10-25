// RuleEngineTest_EngineX2.cs — 执行代理 `X2`（2026-10-25）的回归断言：**成就计数器**
//
// 判据（⛔ 不要重新论证）= `资料/待办判据_成就计数器_1025b.md`：
//   · §B-0 数据管线四列（`k` 类名 / `f` 过滤位 / `tut` / `duel`）—— 没有它们，
//     27 条升级 / 30 条对局 / 17 条触发在运行时**分不开**（challenge 原文有重名，§F5）；
//   · §B-2/§B-3 计数源（伤害分类 / 能力触发 / 出牌 / 升级 / 对局 / 登录）；
//   · §B-4 结算统一一处（`ProfileData.ApplyAchievementTally`）；
//   · §二① 教程闸（`MissionChallenge__ValidateMatchConditions.c`：回放一律不计 · 教程按
//     `allowOnTutorials`（42 计 / 60 不计）· 决斗按 `allowOnDuels`（全 0））；
//   · §F2 口径裁断（`GamesPlayed__OnTrigger.c`：判**模式**，不判「对手是不是真人」）。
//
// 🔴 期望值一律写死（或从**资产真数**读：`profile_cosmetics.json` 的 `k/f/tut/duel`），
//    ⛔ 不从被测实现反算。
// 🔴 **灭自证**：每类计数都配一条「**另一类不该涨**」的读数 —— 把两处一起改回旧写法不可能同时绿。
// ✅ 接线：`RuleEngineTest.Run()` 里一行 `Step(TestEngineX2);`（本文件不碰 `Run()` 的用例表）。
using System;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批**唯一入口**（`Run()` 里一行 `Step(TestEngineX2)`）。</summary>
    public static void TestEngineX2()
    {
        Section("🆕 执行代理 `X2`：成就计数器（数据四列 · 伤害分类 · 触发单口 · 出牌 · 升级 · 对局 · 教程闸 · F2）");
        // 🔴 不碰玩家真档：批处理本来就不落盘（`ProfileData.EnsureSave` 的 `Application.isBatchMode` 门），
        //    这里再把路径改到临时文件 —— 双保险（同 `ProfileDataAssertE13` 的做法）。
        string tmp = Path.Combine(Path.GetTempPath(), "wf_x2_" + Guid.NewGuid().ToString("N"));
        string oldPath = ProfileData.SavePathOverrideForTest;
        try
        {
            Directory.CreateDirectory(tmp);
            ProfileData.SavePathOverrideForTest = Path.Combine(tmp, "profile.json");

            X2T0DataPipeline();
            X2T1DamageKinds();
            X2T2TriggerSinglePort();
            X2T3CardPlayed();
            X2T4Upgrade();
            X2T5GamesPlayed();
            X2T6TutorialGate();
            X2T7Login();
            X2T8MissingIdIsLoud();
        }
        catch (Exception e)
        {
            CheckTrue(false, "X2 断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            ProfileData.SavePathOverrideForTest = oldPath;
            try { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); } catch { /* 临时目录删不掉不影响结论 */ }
        }
    }

    // ==================================================================================================
    //  ① 数据管线四列（判据 §B-0）
    // ==================================================================================================
    static void X2T0DataPipeline()
    {
        var all = ProfileData.Achievements;
        CheckTrue(ProfileData.Loaded, "X2-0a（前提）成就清单读进来了（`Resources/profile_cosmetics.json`）");
        Check(all.Count, 102, "X2-0b 成就条数 = 资产真数 102");

        int noClass = 0, tutYes = 0, duelYes = 0;
        for (int i = 0; i < all.Count; i++)
        {
            if (string.IsNullOrEmpty(all[i].Class)) noClass++;
            if (all[i].TutorialOk) tutYes++;
            if (all[i].DuelOk) duelYes++;
        }
        Check(noClass, 0, "X2-0c ★ 每条都有**触发源类名**（`k`）—— 少一条就有一条成就在结算时永远不涨");
        // 判据 §二① 实测分布：**42 条 allowOnTutorials=1 / 60 条缺省 0**；`allowOnDuels` **102 条全 0**。
        Check(tutYes, 42, "X2-0d ★ `allowOnTutorials=1` 的恰好 42 条（判据 §二① 现读）");
        Check(duelYes, 0, "X2-0e ★ `allowOnDuels=1` 的 0 条（判据 §二①：决斗一条都不计）");

        // 过滤位真的解出来了：三条代表性成就（升级 / 对局 / 触发 / 伤害）
        var a25 = ProfileData.AchievementById("Ach_25");
        var f25 = ProfileData.FiltersOf(a25);
        CheckTrue(f25 != null && f25.army == 10 && f25.tier == 1,
                  "X2-0f `Ach_25`（UM 升到 Tier2）= `army=10` + `tier=1`（原版 `CardTier` 0 基）");

        var f37 = ProfileData.FiltersOf(ProfileData.AchievementById("Ach_37"));
        CheckTrue(f37 != null && f37.gameModes != null && f37.gameModes.Length == 1
                  && f37.gameModes[0]._typeNameAndAssembly != null
                  && f37.gameModes[0]._typeNameAndAssembly.IndexOf("IRankedEvent", StringComparison.Ordinal) >= 0,
                  "X2-0g `Ach_37` 的模式是 `IRankedEvent`（原版判的就是这个运行时类型）");

        var f81 = ProfileData.FiltersOf(ProfileData.AchievementById("Ach_81"));
        CheckTrue(f81 != null && f81.triggerAbility == 120, "X2-0h `Ach_81` 的 `triggerAbility = 120`（`AbilityTrigger.Slay`）");

        // ★ 重名那三组：**按 id 写**才分得开（判据 §F5）
        var a27 = ProfileData.AchievementById("Ach_27");
        var a29 = ProfileData.AchievementById("Ach_29");
        CheckTrue(a27 != null && a29 != null && a27.ChallengeId == a29.ChallengeId,
                  "X2-0i ★ `Ach_27` / `Ach_29` 的 challenge 原文**同名**（`" + (a27 != null ? a27.ChallengeId : "?") + "`）");
        var f27 = ProfileData.FiltersOf(a27);
        var f29 = ProfileData.FiltersOf(a29);
        CheckTrue(f27 != null && f29 != null, "X2-0j（前提）这两条的过滤位都解出来了");
        if (f27 != null && f29 != null)
        {
            Check(f27.army, 40, "X2-0j ★ …但 `army` 不同：`Ach_27` = 40（Sautekh）");
            Check(f29.army, 30, "X2-0j ★ …`Ach_29` = 30（SaimHann）⇒ 只能按 `Ach_N` 写，按原文会串台");
        }
    }

    // ==================================================================================================
    //  ② 伤害分类（判据 §B-3 #1）：收口 = `RuleCore.ApplyDamageCore`，按 `actual` 累加
    // ==================================================================================================
    static void X2T1DamageKinds()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var victim = Place(ctx, 1, 5, Unit("X2 Victim", 1, 0, 40));
        var own = Place(ctx, 0, 5, Unit("X2 Own", 1, 0, 40));

        // ---- ① 近战 7 点 ⇒ `Melee` 恰好 7 ----
        RuleCore.HurtWithSourceForTest(ctx, victim, 7, "X2 测试", kind: DamageKind.Melee);
        Check(ctx.Tally.DamageOf(0, DamageKind.Melee), 7, "X2-1a 近战 7 点 ⇒ `DamageByKind[我方][Melee] == 7`（打的是敌方单位）");

        // ---- ② ★ 灭自证：远程 5 点**只进** `Ranged`，`Melee` 一个点都不许涨 ----
        RuleCore.HurtWithSourceForTest(ctx, victim, 5, "X2 测试", kind: DamageKind.Ranged);
        Check(ctx.Tally.DamageOf(0, DamageKind.Ranged), 5, "X2-1b ★ 远程 5 点 ⇒ `Ranged == 5`");
        Check(ctx.Tally.DamageOf(0, DamageKind.Melee), 7, "X2-1b ★ …而且 `Melee` **仍是 7**（两条一起断：两边一起改回旧写法不可能同时绿）");

        // ---- ③ ★ 灭自证：类别未知 ⇒ **不计**（⛔ 不许用「猜一个」兜底）----
        int beforeAll = ctx.Tally.DamageOf(0, DamageKind.Melee) + ctx.Tally.DamageOf(0, DamageKind.Ranged)
                      + ctx.Tally.DamageOf(0, DamageKind.Spell) + ctx.Tally.DamageOf(0, DamageKind.Shuriken)
                      + ctx.Tally.DamageOf(0, DamageKind.Markerlight) + ctx.Tally.DamageOf(0, DamageKind.Stomp)
                      + ctx.Tally.DamageOf(0, DamageKind.Blast) + ctx.Tally.DamageOf(0, DamageKind.Sentry);
        RuleCore.HurtWithSourceForTest(ctx, victim, 4, "X2 测试（Unknown）");
        int afterAll = ctx.Tally.DamageOf(0, DamageKind.Melee) + ctx.Tally.DamageOf(0, DamageKind.Ranged)
                     + ctx.Tally.DamageOf(0, DamageKind.Spell) + ctx.Tally.DamageOf(0, DamageKind.Shuriken)
                     + ctx.Tally.DamageOf(0, DamageKind.Markerlight) + ctx.Tally.DamageOf(0, DamageKind.Stomp)
                     + ctx.Tally.DamageOf(0, DamageKind.Blast) + ctx.Tally.DamageOf(0, DamageKind.Sentry);
        Check(afterAll, beforeAll, "X2-1c ★ 类别缺省（`Unknown`）的伤害 ⇒ **一格都不涨**（⛔ 不拿某一类兜底）");

        // ---- ④ 「打自己人」不算我方战果（归属口径与 `DamageToEnemy` 逐字相同：记给**对面**）----
        RuleCore.HurtWithSourceForTest(ctx, own, 4, "X2 测试（打自己人）", kind: DamageKind.Melee);
        Check(ctx.Tally.DamageOf(0, DamageKind.Melee), 7, "X2-1d ★ 打自己人 ⇒ **我方**那一格不动（仍 7）");
        Check(ctx.Tally.DamageOf(1, DamageKind.Melee), 4, "X2-1d ★ …而是记给**对面**（与 `BattleContext.DamageToEnemy` 同一条归属规则）");

        // ---- ⑤ 从计数 → 成就（`Ach_2` 近战涨、`Ach_3` 远程不涨）----
        int b2 = ProfileData.AchievementValue("Ach_2");
        int b3 = ProfileData.AchievementValue("Ach_3");
        ProfileData.ApplyAchievementTally(ctx.Tally, 0, 0, 0, 0, GameMode.OfflinePractice, null, false, false);
        Check(ProfileData.AchievementValue("Ach_2"), b2 + 7, "X2-1e 近战 7 点 ⇒ `Ach_2`（`Deal Melee Damage`）+7");
        Check(ProfileData.AchievementValue("Ach_3"), b3 + 5, "X2-1f ★ 同一次结算里 `Ach_3`（远程）**只 +5**（= 远程那 5 点，不是全部 12）");
    }

    // ==================================================================================================
    //  ③ 能力触发**单口**（判据 §B-3 #2）：`BattleContext.Emit` 是唯一入口
    // ==================================================================================================
    static void X2T2TriggerSinglePort()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);

        // ---- ① 原版 trigger 名（`EmitCardTrigger` 发的形态）----
        ctx.Emit(EvtKind.Trigger, 0, 5, "X2", keyword: "Slay");
        Check(ctx.Tally.TriggerOf(0, 120), 1, "X2-2a `Slay` 触发一次 ⇒ `TriggerByAbility[0][120] == 1`");

        // ---- ② ★ `#<能力序号>` 后缀要剥掉（`KeywordWithAbility` 的传输形态）----
        ctx.Emit(EvtKind.Trigger, 0, 5, "X2", keyword: "Slay#2");
        Check(ctx.Tally.TriggerOf(0, 120), 2, "X2-2b ★ `Slay#2`（带能力序号后缀）也只算 `Slay` 一次 ⇒ 累计 2");

        // ---- ③ 引擎关键词那一形态（`SceneSig(EvtKind.Trigger, "landing")`）也认 ----
        ctx.Emit(EvtKind.Trigger, 1, 5, "X2", keyword: "landing");
        Check(ctx.Tally.TriggerOf(1, 440), 1, "X2-2c 引擎关键词 `landing` 也认（⇒ `Landing(440)`，按 `e.Player` 记给座位 1）");

        // ---- ④ ★ 灭自证：认不出的关键词 ⇒ 不记 ----
        int sum = 0;
        for (int s = 0; s < 2; s++)
            for (int a = 0; a < 800; a++) sum += ctx.Tally.TriggerOf(s, a);
        ctx.Emit(EvtKind.Trigger, 0, 5, "X2", keyword: "这不是任何 trigger");
        int sum2 = 0;
        for (int s = 0; s < 2; s++)
            for (int a = 0; a < 800; a++) sum2 += ctx.Tally.TriggerOf(s, a);
        Check(sum2, sum, "X2-2d ★ 认不出的关键词 ⇒ **一格都不涨**（⛔ 不猜成 `ThisCardPlayed(0)`）");

        // ---- ⑤ 从计数 → 成就（`Ach_81` Slay 涨、`Ach_5` Codex 不涨）----
        int b81 = ProfileData.AchievementValue("Ach_81");
        int b5 = ProfileData.AchievementValue("Ach_5");
        ProfileData.ApplyAchievementTally(ctx.Tally, 0, 0, 0, 0, GameMode.OfflinePractice, null, false, false);
        Check(ProfileData.AchievementValue("Ach_81"), b81 + 2, "X2-2e `Slay` 两次 ⇒ `Ach_81`（`Trigger Slay`）+2");
        Check(ProfileData.AchievementValue("Ach_5"), b5, "X2-2f ★ `Ach_5`（`Trigger Codex`）**不动**（17 条触发各认各的 id）");
    }

    // ==================================================================================================
    //  ④ 出牌（判据 §B-3 #3）：单位走 `PlayCard`、战术走 `PlayTactic`，**两条都要**
    // ==================================================================================================
    static void X2T3CardPlayed()
    {
        // ---- 单位卡 + `Ephemeral`（`Ach_12`）----
        var eph = new CardDef("X2Eph", "X2Eph", "unit", "", null, "Test", 1, 1, 5, 0,
                              keywords: new[] { "Ephemeral" });
        var ctx = Battle(new[] { eph }, new CardDef[0]);
        ToP1Turn(ctx, 1);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "X2Eph"), 1), RuleCodes.OK, "X2-3a 打出一张 `Ephemeral` 部队");
        Check(ctx.Tally.CardsPlayedOf(0, CardPlayedTag.Ephemeral), 1, "X2-3a 单位支：`CardsPlayedByTag[Ephemeral] == 1`");

        // ---- 战术卡（`Tactic` 标签）+ ★ 灭自证：`Ephemeral` 那格不许跟着涨 ----
        var tac = Tactic("X2Tac", 1, "Deal 1 damage to an enemy troop");
        var ctx2 = Battle(new[] { tac }, new CardDef[0]);
        ToP1Turn(ctx2, 1);
        Place(ctx2, 1, 5, Unit("X2 Foe", 1, 0, 9));       // 战术要有个合法目标（`enemy troop`）
        CheckCode(RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "X2Tac"), 5), RuleCodes.OK, "X2-3b 打出一张战术卡");
        Check(ctx2.Tally.CardsPlayedOf(0, CardPlayedTag.Tactic), 1, "X2-3b 战术支：`CardsPlayedByTag[Tactic] == 1`");
        Check(ctx2.Tally.CardsPlayedOf(0, CardPlayedTag.Ephemeral), 0, "X2-3c ★ …而 `Ephemeral` 那格 **0**（两张卡各落各的标签）");

        // ---- `Ach_12`（`Play Card With Ephemeral`）只认那一条 ----
        int b12 = ProfileData.AchievementValue("Ach_12");
        ProfileData.ApplyAchievementTally(ctx.Tally, 0, 0, 0, 0, GameMode.OfflinePractice, null, false, false);
        Check(ProfileData.AchievementValue("Ach_12"), b12 + 1, "X2-3d 打一张临时卡 ⇒ `Ach_12`（`Play Card With Ephemeral`）+1");
    }

    // ==================================================================================================
    //  ⑤ 升级卡（判据 §B-3 #4 · 27 条）：按 `(army, level-after)` 分流
    // ==================================================================================================
    static void X2T4Upgrade()
    {
        int b23 = ProfileData.AchievementValue("Ach_23");   // 任意档
        int b25 = ProfileData.AchievementValue("Ach_25");   // UM → Tier2
        int b31 = ProfileData.AchievementValue("Ach_31");   // UM → Tier3
        int b26 = ProfileData.AchievementValue("Ach_26");   // Goff → Tier2

        ProfileData.ApplyAchievementUpgrade("Ultramarines", 2);
        Check(ProfileData.AchievementValue("Ach_23"), b23 + 1, "X2-4a 升一级 ⇒ `Ach_23`（任意档）+1");
        Check(ProfileData.AchievementValue("Ach_25"), b25 + 1, "X2-4b UM 升到 **Lv2** ⇒ `Ach_25`（UM T2）+1");
        Check(ProfileData.AchievementValue("Ach_31"), b31, "X2-4c ★ `Ach_31`（UM T3）**不动** —— 档位闸真的在");
        Check(ProfileData.AchievementValue("Ach_26"), b26, "X2-4d ★ `Ach_26`（Goff T2）**不动** —— 阵营闸真的在");

        ProfileData.ApplyAchievementUpgrade("Ultramarines", 3);
        Check(ProfileData.AchievementValue("Ach_31"), b31 + 1, "X2-4e UM 升到 **Lv3** ⇒ `Ach_31`（UM T3）+1");
        Check(ProfileData.AchievementValue("Ach_25"), b25 + 1, "X2-4f ★ 这一跳 `Ach_25`（T2）**不再涨**（只认自己那一档）");
        Check(ProfileData.AchievementValue("Ach_23"), b23 + 2, "X2-4g `Ach_23` 累计 2（每成功一次 +1）");
    }

    // ==================================================================================================
    //  ⑥ 对局（判据 §B-3 #5 · 30 条 + §F2 裁断）
    // ==================================================================================================
    static void X2T5GamesPlayed()
    {
        var t = new AchievementTally();
        int b37 = ProfileData.AchievementValue("Ach_37");    // Play Ranked（任意阵营）
        int b38 = ProfileData.AchievementValue("Ach_38");    // Win Ranked
        int b41 = ProfileData.AchievementValue("Ach_41");    // Win Ranked as UM
        int b47 = ProfileData.AchievementValue("Ach_47");    // Win Draft as UM
        int b39 = ProfileData.AchievementValue("Ach_39");    // Play Draft
        int b40 = ProfileData.AchievementValue("Ach_40");    // Win Draft
        int b42 = ProfileData.AchievementValue("Ach_42");    // Win Ranked as Goff

        // ---- ★ F2：**单机**排位（`Classic`）也算（原版判模式、不判对手是不是真人）----
        ProfileData.ApplyAchievementTally(t, 0, 0, 0, 0, GameMode.Classic, "Ultramarines", true, false);
        Check(ProfileData.AchievementValue("Ach_37"), b37 + 1, "X2-5a ★ 排位一局 ⇒ `Ach_37`（Play Ranked）+1（⛔ 不再要求「真联机」）");
        Check(ProfileData.AchievementValue("Ach_38"), b38 + 1, "X2-5b ★ 赢了 ⇒ `Ach_38`（Win Ranked）+1");
        Check(ProfileData.AchievementValue("Ach_41"), b41 + 1, "X2-5c 我方是极限战士 ⇒ `Ach_41`（Win Ranked as UM）+1");
        Check(ProfileData.AchievementValue("Ach_42"), b42, "X2-5d ★ `Ach_42`（Goff）**不动** —— 阵营分流真的在");
        Check(ProfileData.AchievementValue("Ach_47"), b47, "X2-5e ★ `Ach_47`（Draft as UM）**不动** —— 模式分流真的在");

        // ---- 轮抽（`ClosedDeck`）：只涨轮抽那两条 ----
        ProfileData.ApplyAchievementTally(t, 0, 0, 0, 0, GameMode.ClosedDeck, "Ultramarines", true, false);
        Check(ProfileData.AchievementValue("Ach_39"), b39 + 1, "X2-5f 轮抽一局 ⇒ `Ach_39`（Play Draft）+1");
        Check(ProfileData.AchievementValue("Ach_40"), b40 + 1, "X2-5g 赢了 ⇒ `Ach_40`（Win Draft）+1");
        Check(ProfileData.AchievementValue("Ach_47"), b47 + 1, "X2-5h 我方是极限战士 ⇒ `Ach_47`（Win Draft as UM）+1");
        Check(ProfileData.AchievementValue("Ach_37"), b37 + 1, "X2-5i ★ 轮抽**不算**排位 ⇒ `Ach_37` 仍 +1（不是 +2）");

        // ---- 输了一局排位 ⇒ 只涨「玩过」，不涨「赢过」----
        ProfileData.ApplyAchievementTally(t, 0, 0, 0, 0, GameMode.Classic, "Ultramarines", false, false);
        Check(ProfileData.AchievementValue("Ach_37"), b37 + 2, "X2-5j 排位输了 ⇒ `Ach_37` +1（玩过）");
        Check(ProfileData.AchievementValue("Ach_38"), b38 + 1, "X2-5k ★ …而 `Ach_38`（Win）**不动**（`onlyAccountWins=1`）");

        // ---- ★ 灭自证：练习局（`OfflinePractice`）两条都不算 ----
        ProfileData.ApplyAchievementTally(t, 0, 0, 0, 0, GameMode.OfflinePractice, "Ultramarines", true, false);
        Check(ProfileData.AchievementValue("Ach_37"), b37 + 2, "X2-5l ★ 练习局 ⇒ `Ach_37` **不动**（只有排位/轮抽那两种模式算）");
    }

    // ==================================================================================================
    //  ⑦ 教程 / 决斗 / 回放闸（判据 §二① · §F1）
    // ==================================================================================================
    static void X2T6TutorialGate()
    {
        var t = new AchievementTally();

        // ---- 教程局：`Ach_1`（tut=1）计 · `Ach_63`（tut=0）**不计** ----
        int b1 = ProfileData.AchievementValue("Ach_1");
        int b63 = ProfileData.AchievementValue("Ach_63");
        ProfileData.ApplyAchievementTally(t, 0, 10, 0, 5, GameMode.Tutorial, null, true, false);
        Check(ProfileData.AchievementValue("Ach_1"), b1 + 10, "X2-6a 教程局：`Ach_1`（`allowOnTutorials=1`）**照计**");
        Check(ProfileData.AchievementValue("Ach_63"), b63, "X2-6b ★ 教程局：`Ach_63`（`allowOnTutorials=0`）**不涨** —— 这就是 §F1 那条误涨的修复");

        // ---- 决斗局：`allowOnDuels` 全 0 ⇒ 一条都不计 ----
        int b1b = ProfileData.AchievementValue("Ach_1");
        ProfileData.ApplyAchievementTally(t, 0, 10, 0, 5, GameMode.Duel, null, true, false);
        Check(ProfileData.AchievementValue("Ach_1"), b1b, "X2-6c ★ 决斗局：`Ach_1` **也不涨**（102 条 `allowOnDuels` 全 0）");

        // ---- 回放：一律不计（`replay` 位 + `GameMode.Replay` 两条路都堵）----
        ProfileData.ApplyAchievementTally(t, 0, 10, 0, 5, GameMode.Classic, null, true, replay: true);
        Check(ProfileData.AchievementValue("Ach_1"), b1b, "X2-6d ★ 回放（`replay: true`）⇒ **不计**");
        ProfileData.ApplyAchievementTally(t, 0, 10, 0, 5, GameMode.Replay, null, true, replay: false);
        Check(ProfileData.AchievementValue("Ach_1"), b1b, "X2-6e ★ 回放模式（`GameMode.Replay`）⇒ **也不计**（即使 `replay` 位没传）");

        // ---- 普通模式：`Ach_63` 照计（证明 6b 不是「这一条永远不涨」）----
        ProfileData.ApplyAchievementTally(t, 0, 0, 0, 5, GameMode.OfflinePractice, null, true, false);
        Check(ProfileData.AchievementValue("Ach_63"), b63 + 5, "X2-6f ★ 非教程局 ⇒ `Ach_63` +5（6b 的灭自证）");
    }

    // ==================================================================================================
    //  ⑧ 登录（判据 §B-3 #6 · `Ach_17` 累计 / `Ach_18` 连续）
    //
    //  ⚠️ `RecordLogin` **会落盘**（末尾那句 `Persist`）⇒ 自检必须把 `OverrideDir` 指到临时目录
    //     （`DailyData.StoreOn` 那道门的口径），跑完把**三格状态**逐格还原。
    //  ⚠️ `Ach_21` / `Ach_22`（注册账号 / 订阅新闻）**结构上做不到**（服务端事件，本地零信号，判据 §五）。
    // ==================================================================================================
    static void X2T7Login()
    {
        int oldDays = DailyData.StreakLoggedDays;
        DateTime oldLast = DailyData.StreakLastDayForTest();
        int oldTotal = DailyData.TotalLoginDays;
        string oldDir = DailyData.OverrideDir;
        string tmp = Path.Combine(Path.GetTempPath(), "wf_x2_daily_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tmp);
            DailyData.OverrideDir = tmp;             // 走临时档（⛔ 不碰玩家真档）
            var now = DateTime.Now;
            DailyData.ForceStreakForTest(2, now.Date.AddDays(-1));
            DailyData.ForceTotalLoginDaysForTest(2);
            DailyData.RecordLoginForTest(now);       // 走**同一条实现**（⛔ 不另写一份规则）
            Check(DailyData.StreakLoggedDays, 3, "X2-7a 昨天登过（连登 2）⇒ 今天再登 = 连登 3");
            Check(DailyData.TotalLoginDays, 3, "X2-7b ★ 累计登录 = 3（**跨一天 +1**，与连登是两个数）");
            CheckTrue(ProfileData.AchievementValue(ProfileData.IdAchLoginStreakDays) >= 3,
                      "X2-7c `Ach_18`（`Log in consecutively`）进度已写进去（≥3）");
            CheckTrue(ProfileData.AchievementValue(ProfileData.IdAchLoginTotalDays) >= 3,
                      "X2-7d `Ach_17`（`Log in counter`）进度已写进去（≥3）");

            // ★ 灭自证：同一天再进壳 ⇒ 累计**不动**（幂等，不是「进一次加一次」）
            DailyData.RecordLoginForTest(now);
            Check(DailyData.TotalLoginDays, 3, "X2-7e ★ 同一天再进一次壳 ⇒ 累计**仍 3**（幂等）");
        }
        catch (Exception e)
        {
            CheckTrue(false, "X2-7 登录断言抛异常：" + e);
        }
        finally
        {
            DailyData.ForceStreakForTest(oldDays, oldLast);
            DailyData.ForceTotalLoginDaysForTest(oldTotal);
            DailyData.OverrideDir = oldDir;
            try { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); } catch { /* 临时目录删不掉不影响结论 */ }
        }
    }

    // ==================================================================================================
    //  ⑨ 不静默：找不到 id ⇒ 出声（返回 false）
    // ==================================================================================================
    static void X2T8MissingIdIsLoud()
    {
        Check(ProfileData.GainAchievementOutsideMatch("Ach_999", 1), false,
              "X2-7a ★ 不存在的成就 id ⇒ **不写**（并且 `Debug.LogWarning` 一条 —— 红线：不许静默失败）");
        Check(ProfileData.GainAchievement("Ach_1", 0, GameMode.Classic, false), false,
              "X2-7b delta = 0 ⇒ **不写**（这一项这局是 0 不该产生一次写盘）");
    }
}
