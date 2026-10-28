// RuleEngineTest_EngineB94B8.cs — 第九十四会话 · 波 2 · 执行代理 `B94_B8`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆）。
// 本件只放本批新增判据/取数口的断言；宿主 `RuleEngineTest.cs` 里只加一个
//   `{ CheckB94B8Engine(); }` 块（⚠️ 方法名**不用 `Test`/`Report` 打头**，也**不进 8 空格缩进的
//   顶层语句** ⇒ 不会被 `VerifyRunCaseCoverage` / `gen_diag_runner` 认成用例）。
//
// 本批覆盖的三件（判据 = `d:/2/tools/decomp_full/`，逐句亲读 2026-10-29）：
//   ① `BoardAnalysis.DeckMeetsCriteria` —— 原版 `BoardAnalysis__DeckMeetsCriteria.c`（账 248 依赖）
//   ② `TutorialScriptPredicatesB94B8.IsPremulliganScriptAvailable` / `PremulliganScriptedActions`
//      / `IsScriptedStageAvailable` —— 账 422 / 372 / 424（休眠，判据齐）
//   ③ `RuleCore.ThisCardPlayedDamageEntries` / `ThisCardPlayedDamage` —— 原版
//      `EntityScript__GetThisCardPlayedDamage.c` 的结构等价物（账 1037）
//
// 🔴 每条都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB94B8Engine()
    {
        Section("🆕 B94_B8：DeckMeetsCriteria（账 248）· preMulligan/脚本关谓词（账 372/422/424）· ThisCardPlayedDamage 取数（账 1037）");
        B94B8T1DeckMeetsCriteria();
        B94B8T2TutorialPredicates();
        B94B8T3ThisCardPlayedDamage();
    }

    /// <summary>`B94B8` ①：`DeckMeetsCriteria` —— 纯查询谓词，四档 + 空判据 + 空牌表。
    /// 判据 = `BoardAnalysis__DeckMeetsCriteria.c:1-101`。</summary>
    static void B94B8T1DeckMeetsCriteria()
    {
        var a = Unit("OnlyA", 1, 1, 1);
        var b = Unit("OnlyB", 1, 1, 1);
        var c = Unit("OnlyC", 1, 1, 1);
        var deck = new List<CardDef> { a, b, c };
        Func<CardDef, bool> isA = x => x != null && x.Name == "OnlyA";   // 牌表里恰好 1 张满足

        // ---- 前提：筛选结果真的是 1 张（⛔ 别假定）----
        int hit = 0; for (int i = 0; i < deck.Count; i++) if (isA(deck[i])) hit++;
        Check(hit, 1, "前提：`meets` 在 3 张牌库里恰好命中 1 张");
        if (hit != 1) return;

        CheckTrue(BoardAnalysis.DeckMeetsCriteria(deck, isA, BoardAnalysis.ModeEqual, 1),
                  "`ModeEqual(1)` 目标 1：命中 1 ⇒ 满足");
        // 🧨 把 Equal 档实现改成 `count >= target` ⇒ 这条会变 true（本该 false）。
        CheckTrue(!BoardAnalysis.DeckMeetsCriteria(deck, isA, BoardAnalysis.ModeEqual, 2),
                  "🧨 `ModeEqual(1)` 目标 2：命中 1 ≠ 2 ⇒ **不满足**");
        CheckTrue(BoardAnalysis.DeckMeetsCriteria(deck, isA, BoardAnalysis.ModeAtLeast, 1),
                  "`ModeAtLeast(2)` 目标 1：命中 1 ≥ 1 ⇒ 满足");
        CheckTrue(!BoardAnalysis.DeckMeetsCriteria(deck, isA, BoardAnalysis.ModeAtLeast, 2),
                  "🧨 `ModeAtLeast(2)` 目标 2：命中 1 < 2 ⇒ **不满足**");
        CheckTrue(!BoardAnalysis.DeckMeetsCriteria(deck, isA, BoardAnalysis.ModeAtMost, 0),
                  "🧨 `ModeAtMost(3)` 目标 0：命中 1 > 0 ⇒ **不满足**");
        CheckTrue(BoardAnalysis.DeckMeetsCriteria(deck, isA, BoardAnalysis.ModeAtMost, 1),
                  "`ModeAtMost(3)` 目标 1：命中 1 ≤ 1 ⇒ 满足");

        // 原版「子判据 count == 0 ⇒ return 1」= 调用方传 `meets == null`。
        CheckTrue(BoardAnalysis.DeckMeetsCriteria(deck, null, BoardAnalysis.ModeEqual, 99),
                  "`meets == null`（原版无条件即满足）⇒ 恒 true，与 targetCount 无关");
        // 牌表 null ⇒ false（原版对 null 牌表没有可用结果）。
        CheckTrue(!BoardAnalysis.DeckMeetsCriteria(null, isA, BoardAnalysis.ModeEqual, 1),
                  "`deck == null` ⇒ false");
    }

    /// <summary>`B94B8` ②：preMulligan / 脚本关谓词。判据 = `BattleManager__IsPremulliganScriptAvailable.c:1-33`
    /// / `BattleManager__GetPremulliganScriptedData.c:1-45` / `BattleManager__IsScriptedStageAvailable.c:1-15`。</summary>
    static void B94B8T2TutorialPredicates()
    {
        // ---- 休眠档（= 现网 6 关实测形状）：scriptedTurn=false ⇒ 恒假 ----
        var off = new TutorialStageData
        {
            stage = 1,
            preMulligan = new TutorialTurn { turnName = "pre", scriptedTurn = false,
                                             actions = new TutorialAction[0] },
        };
        CheckTrue(!TutorialScriptPredicatesB94B8.IsPremulliganScriptAvailable(off),
                  "🧨 `scriptedTurn=false` ⇒ `IsPremulliganScriptAvailable` = false（现网 6 关即此档）");
        CheckTrue(TutorialScriptPredicatesB94B8.PremulliganScriptedActions(off) == null,
                  "`scriptedTurn=false` ⇒ 取数口返回 null");

        // ---- 可达档（数据一变即可达）：scriptedTurn=true + actions≥1 ⇒ 真 ----
        var on = new TutorialStageData
        {
            stage = 1,
            preMulligan = new TutorialTurn { turnName = "pre", scriptedTurn = true,
                                             actions = new[] { new TutorialAction() } },
        };
        CheckTrue(TutorialScriptPredicatesB94B8.IsPremulliganScriptAvailable(on),
                  "`scriptedTurn=true ∧ actions.Length≥1` ⇒ `IsPremulliganScriptAvailable` = true");
        CheckTrue(ReferenceEquals(TutorialScriptPredicatesB94B8.PremulliganScriptedActions(on), on.preMulligan),
                  "过闸后取数口返回**同一个** `preMulligan` 对象");

        // ---- 脚本关：教程/战役都没有 ⇒ false；战役那半边非空 ⇒ true ----
        var ctx = Battle(new[] { Unit("TutDummy", 1, 1, 1) }, new[] { Unit("FoeDummy", 1, 0, 9) });
        CheckTrue(ctx.Tutorial == null, "前提：夹具对局不是教程局（`ctx.Tutorial == null`）");
        if (ctx.Tutorial == null)
        {
            CheckTrue(ctx.CampaignStage == null, "前提：夹具对局没有战役关卡数据（`ctx.CampaignStage == null`）");
            if (ctx.CampaignStage == null)
            {
                CheckTrue(!TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(ctx),
                          "🧨 教程/战役都空 ⇒ `IsScriptedStageAvailable` = false");
                ctx.CampaignStage = new TutorialStageData { stage = 1 };
                CheckTrue(TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(ctx),
                          "战役半非空 ⇒ `IsScriptedStageAvailable` = true（原版 `IsCampaignStageAvailable()` 那一跳）");
            }
        }
    }

    /// <summary>`B94B8` ③：`ThisCardPlayedDamage` 取数口 —— 用挂上去的 `rally` 正文验证
    /// 「只收打在点选目标上的 deal」。判据 = `EntityScript__GetThisCardPlayedDamage.c:1-106`。
    /// ⚠️ 我们的模型没有非主动能力的 chosenAbility/damageCriteria ⇒ 结构等价物是
    /// 「`FxOps(keyword)` 里 `DealsToPickedTarget` 的 deal 逐条求和」。</summary>
    static void B94B8T3ThisCardPlayedDamage()
    {
        var ctx = Battle(new[] { Unit("AtkDummy", 1, 1, 1) }, new[] { Unit("FoeDummy", 1, 0, 9) });
        ToP1Turn(ctx, 1);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "AtkDummy"), 1), RuleCodes.OK,
                  "前提：把 `AtkDummy` 部署到 P0 棋盘");

        int slot = B94B8SlotByName(ctx, 0, "AtkDummy");
        CheckTrue(slot >= 0, "前提：在 P0 棋盘上找到 `AtkDummy`");
        if (slot < 0) return;
        var atk = ctx.Players[0].Board[slot];

        // ---- 目标型伤害：`Deal 3 damage to an enemy` ⇒ 收 3 ----
        var opsTarget = EffectText.Parse("Deal 3 damage to an enemy", out _, out _, card: atk.Card);
        CheckTrue(opsTarget != null && opsTarget.Count > 0, "前提：`Deal 3 damage to an enemy` 解析出 ≥1 条 op");
        if (opsTarget == null || opsTarget.Count == 0) return;
        atk.GrantOps("rally", opsTarget, "Deal 3 damage to an enemy");

        // 🧨 把 `ThisCardPlayedDamageEntries` 里 `DealsToPickedTarget` 那道筛去掉 ⇒ 下一条
        //    (all enemies 那一档) 会变成 3 而不是 0。
        Check(RuleCore.ThisCardPlayedDamage(ctx, 0, slot, "rally"), 3,
              "🧨 `rally: Deal 3 damage to an enemy` ⇒ 取数和 = 3");

        // ---- 非目标型（all enemies，`Count != 1`）⇒ 不计入 ----
        var opsAll = EffectText.Parse("Deal 3 damage to all enemies", out _, out _, card: atk.Card);
        CheckTrue(opsAll != null && opsAll.Count > 0, "前提：`Deal 3 damage to all enemies` 解析出 ≥1 条 op");
        if (opsAll == null || opsAll.Count == 0) return;
        atk.GrantOps("rally", opsAll, "Deal 3 damage to all enemies");
        Check(RuleCore.ThisCardPlayedDamage(ctx, 0, slot, "rally"), 0,
              "🧨 `rally: Deal 3 damage to all enemies` ⇒ **不计入**（原版只收 `targetsAffected == target`）");
    }

    // ---------------- 本件私有小工具（与宿主里的同名助手不冲突）----------------

    /// <summary>按卡名在某一侧棋盘上找格号（找不到返回 -1）。</summary>
    static int B94B8SlotByName(BattleContext ctx, int p, string name)
    {
        var board = ctx.Players[p].Board;
        for (int s = 0; s < board.Length; s++)
        {
            var u = board[s];
            if (u != null && u.Card != null && u.Card.Name == name) return s;
        }
        return -1;
    }
}
