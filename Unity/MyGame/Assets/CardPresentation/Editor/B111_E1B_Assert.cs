// B111_E1B_Assert.cs — 第一百一十一会话 · 执行代理 `B111_E1B`（账 `B-85` + `B-86`）
//
// ============================ 它盯的是什么 ============================
//  **「平 / 誓二选一」的掷骰挪进引擎，且无条件掷** —— 原版只有一个抽取点
//  `BattleManager.GetChoiceOptions`（`decomp_full/BattleManager__GetChoiceOptions.c:40-70`），
//  它被 **`ChoiceOfCardEnemy`（bot 支）** 与 **`ChoiceOfCardPlayer`（面板支）** 各调一次
//  （`out_il2csharp/Assembly-CSharp/BattleManager.cs:8941` / `:8951-8965`；`isBotAndNotReplay` **只用于排序**，
//  抽数那半照跑）⇒ **玩家手 / AI 手 / 回放落地 三条路各消耗正好 1 格预烧随机数**。
//
//  改前：AI 在 `SimpleAI` 掷（1 格）、回放不走 `SimpleAI`（0 格）⇒ 此后每次 `ctx.GetRandomInt` **错开 1 格**；
//        玩家那一手更是 **0 格**。改后：三条路都走 `RuleCore.DecideOathChoice` 里那**一个**掷骰点。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · **A/B 段**：把掷骰写成「有答案就不掷」⇒ 玩家手/AI 手游标差 1 ⇒ A2/A3/B1 红。
//  · **C 段**：把「有答案用答案」改成「一律用掷出来的」⇒ C1/C2 必有一条红（同一份答案、两个相反的注入值）。
//  · **D 段**：把「没答案」改回旧的 `return true`（付得起就付）⇒ D1 红。
//  · **E 段**：把三道闸任意一道删掉 ⇒ 对应 E 段红**且游标会动**。
//  · **F 段**：把 `NoteOathDecision` 撤掉 ⇒ F1 红（`SimpleAI` 就没有可 peek 的「最后用的答案」）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/BattleScene.cs` 那条聚合行里，`B111E2_Assert.Run();` 之后**同一物理行**追加：
//      B111E1B_Assert.Run(); pass += B111E1B_Assert.Pass; fail += B111E1B_Assert.Fail;
//  （⚠️ 追加时先看该行第一个 `//` 在哪 —— 文本必须落在它**之前**，否则代码会被行末注释吞掉、一条都不跑。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B111_E1B`：账 `B-85` + `B-86` —— 誓约二选一的掷骰挪进引擎、三条路各消耗正好 1 格。</summary>
public static class B111E1B_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B111E1B] ");

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具
    static List<CardDef> _pool;

    static List<CardDef> Pool()
    {
        if (_pool != null && _pool.Count > 0) return _pool;
        try { _pool = CardDatabase.Load(); }
        catch (Exception e)
        {
            Debug.LogWarning("[B111E1B] `CardDatabase.Load()` 抛了：" + e.Message);
            _pool = new List<CardDef>();
        }
        return _pool;
    }

    static CardDef ById(string id)
    {
        foreach (var c in Pool()) if (c != null && string.Equals(c.Id, id, StringComparison.Ordinal)) return c;
        return null;
    }

    /// <summary>一局最小夹具（与 `B111_E2_Assert.NewCtx` 同形）：两个座位、指定能量、指定行动方。
    /// **不建棋盘**（本件只碰「选池闸 + 随机源 + 答案」三处）。
    /// <para>⚠️ `seed` 收 `long`：本件各段用 `0xB111E1B_xx` 这种**超过 `int` 范围**的字标做互不相干的种子，
    /// 进来再截成 `int`（`BattleContext(int seed)` 就是这个类型）。</para></summary>
    static BattleContext NewCtx(long seed, int energy, int active = 0)
    {
        var ctx = new BattleContext(unchecked((int)seed));
        ctx.Players[0] = new PlayerState();
        ctx.Players[1] = new PlayerState();
        ctx.Active = active;
        ctx.Players[0].Energy = energy;
        ctx.Players[1].Energy = energy;
        return ctx;
    }

    // ============================================================ 主体
    public static void Run()
    {
        var um109 = ById("UM109");
        if (Pool().Count == 0 || um109 == null)
        {
            Tr(false, "（前提）`CardDatabase.Load()` 拿到卡池、且池内有 `UM109 Adaptive Strategy` —— "
                    + "拿不到 ⇒ 本件下面的断言全部无从谈起");
            return;
        }

        // ---------------------------------------------------------------- A 三条路各消耗正好 1 格
        MenuCheck.Section(_sink, "A · 玩家手 / AI 手 / 回放落地 三条路**各消耗正好 1 格**（读 `RandomNumbersIndex` 前后差）");

        {
            // A1 玩家手（面板答案 = 誓约版）
            var ctx = NewCtx(0xB111E1B_A1, 10);
            var inst = ctx.NewInstance(um109);
            ctx.RandomNumbers[0] = 4; ctx.RandomNumbersIndex = 0;
            ctx.SetOathAnswer(0, true);                       // = `BattleDriver` 面板 `SetOathAnswer` 那一跳
            bool r = RuleCore.DecideOathChoice(ctx, 0, um109, inst);
            Tr(r, "A1 ★ 玩家手：面板选了**誓约版** ⇒ 裁决 = 誓约版");
            C(ctx.RandomNumbersIndex, 1,
              "★ A2：玩家手也**消耗正好 1 格**（原版 `ChoiceOfCardPlayer` 调 `GetChoiceOptions`，抽数那半照跑）"
            + "｜🧨 改成「有答案就不掷」⇒ 本条 0 ⇒ 红（这正是玩家侧原来一格都不烧的缺陷）");
            bool pay; Tr(ctx.TryPeekLastOathDecision(0, out pay) && pay,
                         "★ A2b：引擎把「最后用了誓约版」记了下来（`SimpleAI` peek 的就是这一格）");
        }
        {
            // A3 玩家手（面板答案 = 平版）
            var ctx = NewCtx(0xB111E1B_A3, 10);
            var inst = ctx.NewInstance(um109);
            ctx.RandomNumbers[0] = 4; ctx.RandomNumbersIndex = 0;   // 注入值 4 ⇒ 若真用随机会是「誓约版」
            ctx.SetOathAnswer(0, false);
            bool r = RuleCore.DecideOathChoice(ctx, 0, um109, inst);
            Tr(!r, "★ A3：面板选**平版** ⇒ 裁决 = 平版（注入的随机值会得到相反结论，说明答案优先）");
            C(ctx.RandomNumbersIndex, 1, "★ A3b：仍然**正好 1 格**");
            bool pay; Tr(ctx.TryPeekLastOathDecision(0, out pay) && !pay, "★ A3c：记录里也是「平版」");
        }
        {
            // A4 AI 手（没有答案 ⇒ 用掷出来的那张）
            var ctx = NewCtx(0xB111E1B_A4, 10);
            var inst = ctx.NewInstance(um109);
            ctx.RandomNumbers[0] = 5; ctx.RandomNumbersIndex = 0;   // 5 % 2 == 1 ⇒ 池下标 1 ⇒ UM109 = 平版
            bool r = RuleCore.DecideOathChoice(ctx, 0, um109, inst);
            Tr(!r, "★ A4：AI 手没有答案 ⇒ 用**掷出来的那一张**（注入值 5 ⇒ 池下标 1 ⇒ UM109 的平版）");
            C(ctx.RandomNumbersIndex, 1, "★ A4b：AI 手同样**正好 1 格**");
        }
        {
            // A5 回放落地（报文把答案带过来 —— `NetApply` 落地前 `SetOathAnswer`）
            var ctx = NewCtx(0xB111E1B_A5, 10);
            var inst = ctx.NewInstance(um109);
            ctx.RandomNumbers[0] = 5; ctx.RandomNumbersIndex = 0;
            ctx.SetOathAnswer(0, true);                       // 报文里 `oathChoice == 1`
            bool r = RuleCore.DecideOathChoice(ctx, 0, um109, inst);
            Tr(r, "★ A5：回放落地：报文答案 = 誓约版 ⇒ 裁决 = 誓约版（与注入的随机值相反）");
            C(ctx.RandomNumbersIndex, 1,
              "★★ A5b：回放落地也**正好 1 格** —— 改前回放不走 `SimpleAI` ⇒ 0 格 ⇒ 此后每次取数错开 1 格");
        }

        // ---------------------------------------------------------------- B 三条路游标一致
        MenuCheck.Section(_sink, "B · 同种子 + 同注入值：三条路走完后的 `RandomNumbersIndex` **完全相同**");

        {
            int iPlayer, iAi, iReplay;
            {
                var ctx = NewCtx(0xB111E1B_B, 10); var inst = ctx.NewInstance(um109);
                ctx.RandomNumbers[0] = 4; ctx.RandomNumbersIndex = 0; ctx.SetOathAnswer(0, false);
                RuleCore.DecideOathChoice(ctx, 0, um109, inst); iPlayer = ctx.RandomNumbersIndex;
            }
            {
                var ctx = NewCtx(0xB111E1B_B, 10); var inst = ctx.NewInstance(um109);
                ctx.RandomNumbers[0] = 4; ctx.RandomNumbersIndex = 0;
                RuleCore.DecideOathChoice(ctx, 0, um109, inst); iAi = ctx.RandomNumbersIndex;
            }
            {
                var ctx = NewCtx(0xB111E1B_B, 10); var inst = ctx.NewInstance(um109);
                ctx.RandomNumbers[0] = 4; ctx.RandomNumbersIndex = 0; ctx.SetOathAnswer(0, true);
                RuleCore.DecideOathChoice(ctx, 0, um109, inst); iReplay = ctx.RandomNumbersIndex;
            }
            C(iPlayer, iAi, "★ B1：玩家手 与 AI 手 的游标**相同**（改前玩家 0 / AI 1 ⇒ 差 1 格）");
            C(iAi, iReplay, "★ B1b：AI 手 与 回放落地 的游标**相同**（改前 AI 1 / 回放 0 ⇒ 差 1 格，残局可能分叉）");
            C(iPlayer, 1, "★ B1c：三条路都是 **1**（不是 0、也不是 2）");
        }

        // ---------------------------------------------------------------- C 答案优先于随机（灭自证）
        MenuCheck.Section(_sink, "C · 有答案 ⇒ 答案说了算；掷出来的值**不影响**结论（两个相反的注入值各跑一遍）");

        {
            // 注入值 4 ⇒ 池下标 0（UM109 = 誓约版）；注入值 5 ⇒ 池下标 1（平版）。
            // 答案固定 = 平版 ⇒ 两次都必须得到「平版」。
            foreach (int burn in new[] { 4, 5 })
            {
                var ctx = NewCtx(0xB111E1B_C + burn, 10); var inst = ctx.NewInstance(um109);
                ctx.RandomNumbers[0] = burn; ctx.RandomNumbersIndex = 0;
                ctx.SetOathAnswer(0, false);
                bool r = RuleCore.DecideOathChoice(ctx, 0, um109, inst);
                Tr(!r, $"★ C1（注入 {burn}）：答案 = 平版 ⇒ 裁决 = 平版（掷出来的值**不采**）"
                     + "｜🧨 改成「一律用掷出来的」⇒ 注入 4 那一趟会变誓约版 ⇒ 本条红");
                C(ctx.RandomNumbersIndex, 1, $"★ C1b（注入 {burn}）：仍然正好 1 格（**有没有答案都掷**）");
            }
            foreach (int burn in new[] { 4, 5 })
            {
                var ctx = NewCtx(0xB111E1B_D + burn, 10); var inst = ctx.NewInstance(um109);
                ctx.RandomNumbers[0] = burn; ctx.RandomNumbersIndex = 0;
                ctx.SetOathAnswer(0, true);
                bool r = RuleCore.DecideOathChoice(ctx, 0, um109, inst);
                Tr(r, $"★ C2（注入 {burn}）：答案 = 誓约版 ⇒ 裁决 = 誓约版"
                    + "｜🧨 同上，注入 5 那一趟会变平版 ⇒ 本条红");
            }
        }

        // ---------------------------------------------------------------- D 没有答案 ⇒ 用掷出来的（不再恒「付得起就付」）
        MenuCheck.Section(_sink, "D · AI 手：没有答案 ⇒ 结论跟着预烧表走（旧形状是恒「付得起就付」= 恒誓约版）");

        {
            var ctx = NewCtx(0xB111E1B_D1, 10); var inst = ctx.NewInstance(um109);
            ctx.RandomNumbers[0] = 5; ctx.RandomNumbersIndex = 0;
            bool r = RuleCore.DecideOathChoice(ctx, 0, um109, inst);
            Tr(!r, "★ D1：注入值 5 ⇒ 池下标 1 ⇒ 平版。⛔ 不许回到「没答案就 return true」"
                 + "｜🧨 改回 `return true` ⇒ 本条红（AI 又变成永远付誓约、B-85 白做）");

            var ctx2 = NewCtx(0xB111E1B_D2, 10); var inst2 = ctx2.NewInstance(um109);
            ctx2.RandomNumbers[0] = 4; ctx2.RandomNumbersIndex = 0;
            bool r2 = RuleCore.DecideOathChoice(ctx2, 0, um109, inst2);
            Tr(r2, "★ D1b：换一个注入值（4 ⇒ 池下标 0 ⇒ 誓约版）⇒ 结论跟着翻（不是恒平版）");
        }

        // ---------------------------------------------------------------- E 原版不问的时候：一格都不许消耗
        MenuCheck.Section(_sink, "E · 原版这次根本不问池 ⇒ 不掷、一格都不消耗");

        {
            // E1 不是二选一卡
            CardDef plain = null;
            foreach (var c in Pool()) if (c != null && !RuleCore.OathChoiceNeeded(c)) { plain = c; break; }
            if (plain == null) Tr(false, "（前提）卡池里能找一张**不是** oath 二选一的卡");
            else
            {
                var ctx = NewCtx(0xB111E1B_E1, 10); var inst = ctx.NewInstance(plain);
                ctx.RandomNumbersIndex = 0;
                bool r = RuleCore.DecideOathChoice(ctx, 0, plain, inst);
                Tr(r, $"★ E1：`{plain.Name}` 不是二选一卡 ⇒ 裁决 = 誓约版（= 行为逐字不变的那一档）");
                C(ctx.RandomNumbersIndex, 0, "★ E1b：**一格随机都没消耗**");
            }
        }
        {
            // E2 付不起誓约（余额 = 卡费）
            var ctx = NewCtx(0xB111E1B_E2, um109.Cost);
            var inst = ctx.NewInstance(um109);
            ctx.RandomNumbersIndex = 0;
            Tr(!RuleCore.CanPayOathChoice(ctx, 0, um109, inst), "（前提 E2）余额 = 卡费 ⇒ 付不起誓约");
            bool r = RuleCore.DecideOathChoice(ctx, 0, um109, inst);
            Tr(!r, "★ E2：付不起誓约 ⇒ 只结算平版");
            C(ctx.RandomNumbersIndex, 0, "★ E2b：**一格随机都没消耗**（原版这时根本不问池）");
        }
        {
            // E3 不是这一侧的回合
            var ctx = NewCtx(0xB111E1B_E3, 10, active: 1);
            var inst = ctx.NewInstance(um109);
            ctx.RandomNumbersIndex = 0;
            bool r = RuleCore.DecideOathChoice(ctx, 0, um109, inst);
            Tr(!r, "★ E3：不是这张卡那一侧的回合 ⇒ 按平版结算");
            C(ctx.RandomNumbersIndex, 0, "★ E3b：**一格随机都没消耗**");
        }

        // ---------------------------------------------------------------- F 结论记录那一格
        MenuCheck.Section(_sink, "F · `NoteOathDecision` / `TryPeekLastOathDecision` / `ClearOathDecisionRecord`（`SimpleAI` peek 的接口）");

        {
            var ctx = NewCtx(0xB111E1B_F, 10); var inst = ctx.NewInstance(um109);
            ctx.RandomNumbers[0] = 4; ctx.RandomNumbersIndex = 0;
            ctx.SetOathAnswer(0, false);
            RuleCore.DecideOathChoice(ctx, 0, um109, inst);

            bool pay;
            Tr(ctx.TryPeekLastOathDecision(0, out pay) && !pay,
               "★ F1：结算完那格记着「这次用了平版」（`SimpleAI.PlayCardAsAi` 出牌后 peek 的就是它）"
             + "｜🧨 撤掉 `ctx.NoteOathDecision(...)` ⇒ 本条红（动作上就再也回填不出答案）");
            Tr(!ctx.TryPeekLastOathDecision(1, out pay), "★ F2：座位不符 ⇒ 当作「没有」（一条结论只属于出它的座位）");
            ctx.ClearOathDecisionRecord();
            Tr(!ctx.TryPeekLastOathDecision(0, out pay),
               "★ F3：清掉之后 peek 不到（出牌前清一次 = 残答不许报到下一手）");
        }
    }
}
