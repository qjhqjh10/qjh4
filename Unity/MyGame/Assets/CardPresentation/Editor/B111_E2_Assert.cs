// B111_E2_Assert.cs — 第一百一十一会话 · 执行代理 `B111_E2`（账 `B-69`：AI 的「平 / 誓二选一」按原版 bot 支【等概率随机】）
//
// ============================ 它盯的是什么 ============================
//  原版：`BattleManager.ChoiceOfCardEnemy` 在「非联网 ∧ 行动卡不是本机玩家的 ∧ 不是回放」时
//  （`decomp_full/BattleManager__ChoiceOfCardEnemy.c:18-31`）走 bot 支 ——
//  它拿的是 `GetChoiceOptions(..., isBotAndNotReplay: true)` 的 **`options[0]`**
//  （`out_il2csharp/Assembly-CSharp/BattleManager.cs:8951-8965`），而那个 options 是
//  `GetChoiceOptions` 从池里**按下标随机抽出来的前 N 张**（`decomp_full/...__GetChoiceOptions.c:48-70`）⇒
//  **平 / 誓 严格等概率**，且**每次净消耗 1 格预烧随机数**（池 2 项 + `choicesMaxTargets = 2`：
//  第 1 圈 `GetRandomInt(0,2)` 消耗、第 2 圈 `GetRandomInt(0,1)` 不消耗 —— 见 `BattleContextRandom.cs:65`）。
//  我们改前是「付得起就付」= **确定性**。
//
//  本件落地 = `RuleEngine/Data/SimpleAIPartial_B111E2.cs` 的
//    · `SimpleAI.OathPoolIndexIsOathVariant`（池下标 → 是不是誓约变体；池序 = `OrderBy(uniqueId)`）
//    · `SimpleAI.DecideOathForAi`（掷一次、写 `ctx` 答案 + `AiAction.OathChoice*`）
//    · `SimpleAI.PlayCardAsAi`（`ExecuteActionCore` 的 `PlayCard` 支，1→1 替换那一行）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · B 段：把掷骰改成恒 `true` / 恒 `false` ⇒ B2 / B3 中必有一条红（同一张卡、两个不同的预烧值）。
//  · B 段：把「一次 `GetRandomInt(0,2)`」写成**两圈都掷**（照 ILSpy 那个重复求值的错形状）⇒
//          `RandomNumbersIndex` 会是 2 ⇒ `B4` 红。
//  · B 段：改成 `ctx.AiRng`（AI 自己的骰子）⇒ 预烧表游标不动 ⇒ `B4` 红、且 B2/B3 不再跟着注进去的值走。
//  · C 段：只写 `AiAction.OathChoice` 不写 `ctx.SetOathAnswer` ⇒ C1（`TryPeekOathAnswer`）红。
//          只写 ctx 不写动作 ⇒ C2/C3 红。
//  · D 段：把三道闸任意一道删掉 ⇒ 对应的 D 段红（而且 `RandomNumbersIndex` 会动）。
//  · E 段：恒 `true` / 恒 `false` ⇒ 「两种结果都出现过」红。
//  · F 段：**生产端与消费端闸不同参**（比如 `NeedsToChooseFromPool` 少传/多传一个实参）⇒ 引擎那条
//          `DecideOathChoice` 会走「付得起就付」兜底 ⇒ F 红。这条是**跨文件**的，只改一侧不会一起变绿。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/BattleScene.cs` 的那条聚合行里，`B101E2_Assert.Run();` 之后**同一物理行**追加：
//      B111E2_Assert.Run(); pass += B111E2_Assert.Pass; fail += B111E2_Assert.Fail;
//  （⚠️ 追加时先看该行第一个 `//` 在哪 —— 文本必须落在它**之前**，否则代码会被行末注释吞掉、一条都不跑。）
//
// ============================ 判据 / 报告 ============================
//  · 判据正本：`资料/完全复刻/判据/B110_R3_N07_oath三处.md` §八·1（顺手发现）· 本批报告 `资料/完全复刻/报告/B111_E2.md`
//  · 数据真值（哪张变体是誓约版）逐张现读 `Assets/RuleEngine/Resources/cards_truth.json`：
//      每个变体 SO 的 `manaCost` / `abilities` —— 贵的那张 = 带誓约效果那张 = `choicesSpecificCardIds[1]`。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B111_E2`：账 `B-69` —— AI 的「平 / 誓二选一」= 原版 bot 支的等概率随机（消耗 1 格预烧随机数）。</summary>
public static class B111E2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B111E2] ");

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
            Debug.LogWarning("[B111E2] `CardDatabase.Load()` 抛了：" + e.Message);
            _pool = new List<CardDef>();
        }
        return _pool;
    }

    static CardDef ById(string id)
    {
        foreach (var c in Pool()) if (c != null && string.Equals(c.Id, id, StringComparison.Ordinal)) return c;
        return null;
    }

    /// <summary>
    /// **外部真值**：每张 oath 卡「誓约变体」在原版池里的下标（池序 = `OrderBy(uniqueId)`）。
    /// 逐条来自 `cards_truth.json` 现读（两个变体的 `manaCost`：贵的那张带誓约效果）。
    /// ⛔ 这不是从被测实现推出来的 —— 是**查数据**得的（改了实现不会自动跟着变）。
    /// </summary>
    static readonly string[,] ExpectedOathPoolIndex = new string[,]
    {
        // id                                                    誓约变体在池里的下标
        { "UM109",                                     "0" },   // Adaptive Strategy：b34ec917(1,平) vs a424ea66(2,誓) ⇒ a42 < b34
        { "4de4fcfb5d7e0554a88b9f419a2cd7c2",          "0" },   // Angel's Wrath：d0516815(1) vs 97a4b83a(3) ⇒ 97a < d05
        { "8482077f309b08943b190f18b5874871",          "0" },   // Catechism of Death：74480125(0) vs 0d2874f3(3) ⇒ 0d2 < 744
        { "UM112",                                     "0" },   // Champions of Humanity：a20b3a84(2) vs 2bee2e8f(6) ⇒ 2be < a20
        { "UM116",                                     "0" },   // No Mercy：7b703582(4) vs 6ea114bc(8) ⇒ 6ea < 7b7
        { "UM117",                                     "0" },   // No Respite：d32332a3(5) vs 79fe7580(8) ⇒ 79f < d32
        { "UM115",                                     "0" },   // Stormhawk Interception：fb248c0f(3) vs 57bfef85(6) ⇒ 57b < fb2
        { "UM110",                                     "1" },   // Fall Back：cd053fb3(1) vs fe6fde3d(3) ⇒ cd0 < fe6
        { "UM118",                                     "1" },   // Indomitus Crusade：6bce1717(6) vs 91992b40(12) ⇒ 6bc < 919
        { "UM111",                                     "1" },   // Shall Know No Fear：16c804b7(2) vs 481a554f(10) ⇒ 16c < 481
    };

    /// <summary>一局最小夹具：两个座位、指定能量、指定行动方。**不建棋盘**（本件只碰「选池闸 + 随机源」两处）。</summary>
    static BattleContext NewCtx(int seed, int energy, int active = 0)
    {
        var ctx = new BattleContext(seed);
        ctx.Players[0] = new PlayerState();
        ctx.Players[1] = new PlayerState();
        ctx.Active = active;
        ctx.Players[0].Energy = energy;
        ctx.Players[1].Energy = energy;
        return ctx;
    }

    static AiAction NewPlay() { return new AiAction { Kind = AiActionKind.PlayCard, HandIdx = 0, Slot = 0 }; }

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

        // ---------------------------------------------------------------- A 判据 / 数据前提
        MenuCheck.Section(_sink, "A · 数据前提与池序映射（外部真值 = cards_truth.json）");

        int n = 0;
        for (int r = 0; r < ExpectedOathPoolIndex.GetLength(0); r++)
        {
            string id = ExpectedOathPoolIndex[r, 0];
            int wantIdx = int.Parse(ExpectedOathPoolIndex[r, 1]);
            var card = ById(id);
            if (card == null)
            {
                Tr(false, $"（前提）卡池里有 `{id}` —— 不在了 ⇒ 池数据变了，下面的映射表要现读重核");
                continue;
            }
            n++;
            Tr(RuleCore.OathChoiceNeeded(card), $"（前提）`{id} {card.Name}` 是「平 / 誓二选一」战术卡");
            C(card.ChoicesMaxTargets, 2,
              $"★ `{card.Name}` 的 `choicesMaxTargets == 2` —— 「净消耗 1 格」的前提"
            + "（`min(池2, maxTargets)` ⇒ 第 1 圈消耗、第 2 圈 `GetRandomInt(0,1)` 不消耗）");
            C(card.ChoicesSpecificCardIds.Count, 2, $"★ `{card.Name}` 的 `filterSpecificCards` 正好 2 项");
            Tr(SimpleAI.OathPoolIndexIsOathVariant(card, wantIdx),
               $"★ `{card.Name}`：誓约变体在池里的下标 = {wantIdx}（池序 = `OrderBy(uniqueId)`，不是 `filterSpecificCards` 原序）");
            Tr(!SimpleAI.OathPoolIndexIsOathVariant(card, 1 - wantIdx),
               $"★ 反例：`{card.Name}` 的另一个下标 {1 - wantIdx} 是个平版（两格不能同为真）");
        }
        C(n, ExpectedOathPoolIndex.GetLength(0),
          "★ 池内 10 张 oath 二选一卡全部在场（`cards_engine.json` 现读 10 张；变了 ⇒ 映射表要重核）");

        // ---------------------------------------------------------------- B 随机源形状（确定性注入）
        MenuCheck.Section(_sink, "B · 随机源 = 预烧表按下标取，净消耗 1 格（注入预烧值，不靠运气）");

        {
            // UM109 的誓约变体在池下标 0 ⇒ `GetRandomInt(0,2)` 取到偶数（= 下标 0）⇒ 誓约版
            var ctx = NewCtx(0xB111_E2, 10);
            var inst = ctx.NewInstance(um109);
            var a = NewPlay();
            ctx.RandomNumbers[0] = 4; ctx.RandomNumbersIndex = 0;
            bool rolled;
            bool pay;
            rolled = SimpleAI.DecideOathForAi(ctx, 0, a, inst, out pay);
            Tr(rolled, "B1：闸全开时 AI **掷了**（原版这一次会问池）");
            C(ctx.RandomNumbersIndex, 1, "★ B4：**净消耗正好 1 格**预烧随机数（写成两圈都掷 ⇒ 2；改成 `ctx.AiRng` ⇒ 0）");
            C(pay, true, "★ B2：预烧值 4（`4 % 2 == 0` ⇒ 池下标 0）⇒ UM109 下标 0 是**誓约**变体 ⇒ 付誓约");
        }
        {
            var ctx = NewCtx(0xB111_E3, 10);
            var inst = ctx.NewInstance(um109);
            var a = NewPlay();
            ctx.RandomNumbers[0] = 5; ctx.RandomNumbersIndex = 0;
            bool pay;
            bool rolled = SimpleAI.DecideOathForAi(ctx, 0, a, inst, out pay);
            Tr(rolled, "B5：同一个夹具、换一个预烧值 ⇒ 照样掷");
            C(ctx.RandomNumbersIndex, 1, "★ B6：仍然只消耗 1 格");
            C(pay, false, "★ B3：预烧值 5（`5 % 2 == 1` ⇒ 池下标 1）⇒ UM109 下标 1 是**平版** ⇒ 不付誓约");
        }
        {
            // UM110 的誓约变体在池下标 **1** ⇒ 同一个预烧值 4（下标 0）这次应该是**平版**
            var um110 = ById("UM110");
            if (um110 == null) Tr(false, "（前提）池内有 `UM110 Fall Back`");
            else
            {
                var ctx = NewCtx(0xB111_E4, 10);
                var inst = ctx.NewInstance(um110);
                var a = NewPlay();
                ctx.RandomNumbers[0] = 4; ctx.RandomNumbersIndex = 0;
                bool pay;
                SimpleAI.DecideOathForAi(ctx, 0, a, inst, out pay);
                C(pay, false, "★ B7：**同一个预烧值、另一张卡** ⇒ 结果相反（UM110 的誓约变体在池下标 1）"
                            + "｜🧨 把映射写死成「下标 1 = 誓约」或「下标 0 = 誓约」⇒ B2 与 B7 必有一条红");
            }
        }

        // ---------------------------------------------------------------- C 答案落到两处
        MenuCheck.Section(_sink, "C · 答案同时写进 `ctx`（引擎读）与 `AiAction`（过网 / 录像）");

        {
            var ctx = NewCtx(0xB111_E5, 10);
            var inst = ctx.NewInstance(um109);
            var a = NewPlay();
            ctx.RandomNumbers[0] = 5; ctx.RandomNumbersIndex = 0;   // ⇒ pay = false
            bool pay;
            SimpleAI.DecideOathForAi(ctx, 0, a, inst, out pay);

            bool peekPay;
            Tr(ctx.TryPeekOathAnswer(0, out peekPay),
               "★ C1：答案进了 `ctx`（`TryPeekOathAnswer(0, …)` —— 引擎 `DecideOathChoice` 读的就是这一格）"
             + "｜🧨 只写 `AiAction.OathChoice` 不写 `ctx.SetOathAnswer` ⇒ 本条红");
            C(peekPay, pay, "★ C1b：ctx 里的值与本次掷出来的一致");
            Tr(a.OathChoiceRecorded,
               "★ C2：动作上带「记过」哨兵（`NetProtocol.ToWire` 只读 `AiAction` ⇒ 不记就过不了网）");
            C(a.OathChoice, pay ? 1 : 0,
               "★ C3：动作上的下标与 ctx 同值（0 = 平版 · 1 = 誓约版）"
             + "｜🧨 只写 ctx 不写动作 ⇒ C2/C3 红，联机录像里 AI 那一手会退回「付得起就付」");
            Tr(ctx.HasOathAnswer, "（前提）那格答案仍在 —— `DecideOathForAi` **不取**答案（取是引擎的活）");
        }

        // ---------------------------------------------------------------- D 原版不问的时候：一格都不许消耗
        MenuCheck.Section(_sink, "D · 原版这次根本不问池 ⇒ 不掷、不写答案");

        {
            // D1 不是 oath 二选一卡
            CardDef plain = null;
            foreach (var c in Pool()) if (c != null && !RuleCore.OathChoiceNeeded(c)) { plain = c; break; }
            if (plain == null) Tr(false, "（前提）卡池里能找一张**不是** oath 二选一的卡");
            else
            {
                var ctx = NewCtx(0xB111_E6, 10);
                var inst = ctx.NewInstance(plain);
                var a = NewPlay();
                ctx.RandomNumbersIndex = 0;
                Tr(!SimpleAI.DecideOathForAi(ctx, 0, a, inst), $"★ D1：`{plain.Name}` 不是二选一卡 ⇒ 不掷");
                C(ctx.RandomNumbersIndex, 0, "★ D1b：**一格随机都没消耗**");
                Tr(!ctx.HasOathAnswer, "★ D1c：也没摆答案");
                Tr(!a.OathChoiceRecorded, "★ D1d：动作上也没有「记过」哨兵");
            }
        }
        {
            // D2 付不起誓约（余额 = 卡费）
            var ctx = NewCtx(0xB111_E7, um109.Cost);
            var inst = ctx.NewInstance(um109);
            var a = NewPlay();
            ctx.RandomNumbersIndex = 0;
            Tr(!RuleCore.CanPayOathChoice(ctx, 0, um109, inst),
               "（前提）余额 = 卡费 ⇒ 付不起誓约（`CanPlayWithOathNowInHand` 那一句）");
            Tr(!SimpleAI.DecideOathForAi(ctx, 0, a, inst), "★ D2：付不起誓约 ⇒ **不掷**（原版这时根本不问）");
            C(ctx.RandomNumbersIndex, 0, "★ D2b：一格随机都没消耗");
            Tr(!ctx.HasOathAnswer, "★ D2c：没摆答案（引擎会照原版只结算平版）");
        }
        {
            // D3 不是这一侧的回合
            var ctx = NewCtx(0xB111_E8, 10, active: 1);
            var inst = ctx.NewInstance(um109);
            var a = NewPlay();
            ctx.RandomNumbersIndex = 0;
            Tr(!SimpleAI.DecideOathForAi(ctx, 0, a, inst), "★ D3：不是这张卡那一侧的回合 ⇒ 不掷");
            C(ctx.RandomNumbersIndex, 0, "★ D3b：一格随机都没消耗");
        }

        // ---------------------------------------------------------------- E 分布（灭「恒定」）
        MenuCheck.Section(_sink, "E · 换种子跑 64 局：平 / 誓**两种都出现**且大致各半");

        {
            int paid = 0, plainN = 0, failed = 0;
            // 种子**拉得很开**（`1009 + k*7919`）：`System.Random(seed)` 相邻种子的**首个**输出有已知相关性，
            // 用 1..64 这种连号种子会让「第一格预烧值的奇偶」偏成一边（假红/假绿都不该有）。
            for (int k = 0; k < 64; k++)
            {
                int seed = 1009 + k * 7919;
                var ctx = NewCtx(seed, 10);
                var inst = ctx.NewInstance(um109);
                var a = NewPlay();
                bool pay;
                if (!SimpleAI.DecideOathForAi(ctx, 0, a, inst, out pay)) { failed++; continue; }
                if (pay) paid++; else plainN++;
            }
            C(failed, 0, "★ E1：64 局每一局都掷得成（闸没误关）");
            Tr(paid > 0, $"★ E2：出现过**誓约版**（{paid} 局）｜🧨 恒 `false` ⇒ 本条红");
            Tr(plainN > 0, $"★ E3：出现过**平版**（{plainN} 局）｜🧨 恒 `true` ⇒ 本条红");
            float ratio = (paid + plainN) > 0 ? (float)paid / (paid + plainN) : 0f;
            MenuCheck.Near(_sink, ratio, 0.5f, 0.3f,
                           $"★ E4：64 局的誓约版占比应大致各半（实得 {paid}/{paid + plainN}）"
                         + "｜🧨 恒 `true`/恒 `false` ⇒ 0 或 1，超 ±0.3");
        }

        // ---------------------------------------------------------------- F 跨文件：引擎消费端真的按答案走
        MenuCheck.Section(_sink, "F · 引擎侧的 `DecideOathChoice` 照 AI 的答案结算（生产端与消费端同参）");

        for (int k = 0; k < 2; k++)
        {
            var ctx = NewCtx(0xB111_F0 + k, 10);
            var inst = ctx.NewInstance(um109);
            var a = NewPlay();
            ctx.RandomNumbers[0] = k == 0 ? 4 : 5; ctx.RandomNumbersIndex = 0;
            bool pay;
            bool rolled = SimpleAI.DecideOathForAi(ctx, 0, a, inst, out pay);
            bool engine = RuleCore.DecideOathChoice(ctx, 0, um109, inst);
            Tr(rolled, $"（前提 F{k + 1}）AI 掷了");
            C(engine, pay,
              $"★ F{k + 1}：引擎 `DecideOathChoice` 的裁决 == AI 掷出来的（{(pay ? "誓约" : "平")}版）"
            + "｜🧨 生产端/消费端闸不同参（如 `NeedsToChooseFromPool` 实参不一致）⇒ 引擎会走「付得起就付」兜底 ⇒ 本条红");
            Tr(!ctx.HasOathAnswer, $"★ F{k + 1}b：引擎取过一次之后那格答案被清掉（一次一取）");
        }
    }
}
