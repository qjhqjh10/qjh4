// RuleEngineTest_T03b.cs — 引擎线 `T03·b`「回合流程残余」那一批的断言（2026-10-21）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_BoardModel` / `_S14` / `_S23_*` / `_Order` / `_T02` / `_Priority` / `_Diag` 都在），
// 而**同一时刻只能有一个写手改一个文件** ⇒ 本批一律写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里那几行 `Step(...)`**。
//
// 本批补 **T03·b** 名下最后三块**缺失的机制**（不是修 bug，是照原版补机制）：
//   ① **`SpentAllMana`（原版 `AbilityTrigger.SpentAllMana = 641`）** —— 「花光法力」触发。
//      判据 = `PlayerManager__UseNormalEnergy.c:100-124`（**任何**正常法力支出之后，
//      `unusedMana` 恰好变成 0 ⇒ **双方督军**各触发一次，`actingCard` = 花钱那一方）。
//      落在唯一那个扣能量口 `RuleCore.SpendNormalMana` 上（出牌 / 战术 / 付费前缀 / 维护费共用）。
//   ② **`initialHandModifier` / `initialManaModifier` / `initialSpiritStoneModifier`**
//      （原版 `RawCardScript +0x118 / +0x11C / +0x128`）—— 判据见 `CardDef` 那三个属性的 doc。
//   ③ **`skipCardDraw` 的【生产者】**（原版 `HandEffect.cardEffect.buffType == 7`：
//      `AbilityLogic__PlayAbility.c:1377-1414` → `PlayerHand__AddHandEffect.c:88-139`）。
//      消费端（`RuleCore.ConsumeSkipCardDrawHandEffect`）本来就有，缺的是「谁写这条效果」。
//
// 🔴 每条都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// ⚠️ 本文件里凡**直接调 `RuleCore.*`** 的，都是**夹具直调**（断言不是对局），
//    不走 `BattleDriver.LocalAct` / `SimpleAI.ExecuteAction` 两个记账口 ⇒ 不会被录进本地录像。
// ⚠️ **今天全池 0 张卡**会走到这三条机制上（`cards_raw.json` 1,351 张现读：
//    `trigger == 641` 只有战役附魔 `Factories Overdrive`、三个 modifier 全 0、
//    `handBuff.buffType` 没有 7）⇒ 这里全部用**按原版文法造出来的夹具卡**端到端钉住。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==================================================================
    //  ① `SpentAllMana`（花光法力）
    // ==================================================================

    /// <summary>
    /// 夹具：双方督军都写 `SpentAllMana: Gain +2 Attack`，牌库塞满**不会抽空**的填充卡。
    /// 督军近战 2 ⇒ 触发一次变 4（这个增量就是「触发了几次」的读数）。
    /// </summary>
    static BattleContext SpentAllManaFixture(string heroDesc)
    {
        var h0 = new CardDef("T03b_W0", "Spender", "hero", heroDesc, "common", "Test",
                             0, 2, 30, 0, null, subtype: "Warlord");
        var h1 = new CardDef("T03b_W1", "Watcher", "hero", heroDesc, "common", "Test",
                             0, 2, 30, 0, null, subtype: "Warlord");
        var d0 = new List<CardDef> { h0 };
        var d1 = new List<CardDef> { h1 };
        for (int i = 0; i < 12; i++)
        {
            d0.Add(Unit("T03b_F0_" + i, 1, 1, 1));
            d1.Add(Unit("T03b_F1_" + i, 1, 1, 1));
        }
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    static void TestW61bSpentAllMana()
    {
        var ctx = SpentAllManaFixture("SpentAllMana: Gain +2 Attack");
        RuleCore.BeginTurn(ctx);                       // 第 1 回合：经典能量 = 1 + 1 = 2
        int me = ctx.Active, foe = 1 - ctx.Active;
        int startEnergy = ctx.Players[me].Energy;
        Check(startEnergy, 2, "（前提）第 1 回合的能量 = 2");
        Check(ctx.Players[me].Warlord.Attack, 2, "（前提）己方督军近战 2");
        Check(ctx.Players[foe].Warlord.Attack, 2, "（前提）对方督军近战 2");

        // ---- ① 只花一部分 ⇒ **不**触发（原版那道闸是「扣完 == 0」）----
        using (RuleCore.BeginAction(ctx))
            RuleCore.SpendNormalMana(ctx, me, startEnergy - 1, "断言：只花一部分");
        Check(ctx.Players[me].Energy, 1, "花掉 1 点后剩 1");
        Check(ctx.Players[me].Warlord.Attack, 2,
              "★ 没花光 ⇒ `SpentAllMana` **不触发**（己方督军仍是 2 攻）"
            + "｜🧨 把 `SpendNormalMana` 里那句 `if (ps.Energy == 0)` 去掉，本行实得 4");

        // ---- ② 花光剩下的 1 点 ⇒ **双方督军都触发** ----
        //  判据 = `PlayerManager__UseNormalEnergy.c:100-124` 那两次 `TriggerSpentAllMana`
        //    （`this` 换、`actingCard` 都是花钱那一方）⇒ 两侧督军的 `SpentAllMana:` 正文都结算。
        using (RuleCore.BeginAction(ctx))
            RuleCore.SpendNormalMana(ctx, me, 1, "断言：花光");
        Check(ctx.Players[me].Energy, 0, "花光 ⇒ 余额 0");
        Check(ctx.Players[me].Warlord.Attack, 4,
              "★ 花钱那一方的督军触发了（2 → 4）"
            + "｜🧨 把 `FireSpentAllMana` 里那个 `for seat` 改成只发一次 ⇒ 本行仍是 2");
        Check(ctx.Players[foe].Warlord.Attack, 4,
              "★ **对方督军也触发**（原版两遍 `TriggerSpentAllMana` 是两侧督军各一次，不是只发自己那侧）"
            + "｜🧨 只在 `spender` 那一侧发（`seat == spender` 才发）⇒ 本行实得 2");

        // ---- ③ `amount == 0` **不触发**（原版 `0 < amount` 那条闸）----
        using (RuleCore.BeginAction(ctx))
            RuleCore.SpendNormalMana(ctx, me, 0, "断言：0 费");
        Check(ctx.Players[me].Warlord.Attack, 4,
              "★ 支出 0 点 ⇒ **不触发**（免费牌 / 0 费效果不该响）"
            + "｜🧨 把 `amount <= 0` 那个早退删掉 ⇒ 本行实得 6");

        // ---- ④ 余额已经是 0 时「再想花」⇒ 仍然触发（原版判的是【扣完的余额】）----
        //  ⚠️ 这一档是**原版机器码的字面行为**，不是我们挑的：反汇编 `0x18063F31C xor edx, edx`
        //     之后同样落到 `test eax,eax / jne` 那条闸上（余额 0 + 要扣 &gt; 0 ⇒ 触发）。
        using (RuleCore.BeginAction(ctx))
            RuleCore.SpendNormalMana(ctx, me, 3, "断言：余额为 0 还想花 3");
        Check(ctx.Players[me].Energy, 0, "余额保持 0（余额不足 ⇒ 原版把余额**写成 0**，本来就是 0）");
        Check(ctx.Players[me].Warlord.Attack, 6,
              "★ 余额 0 + 要扣 3 ⇒ **仍然触发**（原版判据是「扣完的余额 == 0」，不是「这次付成功了」）"
            + "｜🧨 把触发条件改成「本次付费成功」⇒ 本行实得 4");
    }

    /// <summary>
    /// **余额不足 ⇒ 清零**（不是「整笔不扣」）。
    /// 判据 = 反汇编 `0x18063F2DC cmp edi,eax / jg 错误支`、`0x18063F31C xor edx,edx`、
    ///        `0x18063F381` 用 `edx` 编码写回 `[rbx+0x4c]`（`unusedMana`）。
    /// ⚠️ 本工程 2026-10-20 那一版把它读成「一点不扣」（把 `iVar3` 当成「要扣的额度」）——
    ///    2026-10-21 已就地订正（`RuleCore.SpendNormalMana` 的 doc 里留着订正痕迹）。
    /// </summary>
    static void TestW61bSpendInsufficientDrainsToZero()
    {
        var ctx = SpentAllManaFixture("SpentAllMana: Gain +2 Attack");
        RuleCore.BeginTurn(ctx);
        int me = ctx.Active;
        int have = ctx.Players[me].Energy;
        Check(have, 2, "（前提）第 1 回合的能量 = 2");

        int paid;
        using (RuleCore.BeginAction(ctx))
            paid = RuleCore.SpendNormalMana(ctx, me, have + 3, "断言：付不起");
        Check(paid, have, "★ 返回值 = **原余额**（原版把余额清零 ⇒ 实际扣掉的就是原来那些）"
                        + "｜🧨 改回「付不起就 paid = 0」⇒ 本行实得 0");
        Check(ctx.Players[me].Energy, 0,
              "★ 付不起 ⇒ 余额被**写成 0**（原版 `0x18063F31C xor edx,edx` 之后写回 `+0x4c`）"
            + "｜🧨 改回「整笔不扣」⇒ 本行实得 2");
    }

    // ==================================================================
    //  ② 开局加成三列（`initialHandModifier` / `initialManaModifier` / `initialSpiritStoneModifier`）
    // ==================================================================

    static BattleContext InitialModifierFixture(int handMod, int manaMod, int stoneMod)
    {
        var h0 = new CardDef("T03b_M0", "Modded", "hero", "", "common", "Test",
                             0, 2, 30, 0, null, subtype: "Warlord",
                             initialHandModifier: handMod,
                             initialManaModifier: manaMod,
                             initialSpiritStoneModifier: stoneMod);
        var h1 = new CardDef("T03b_M1", "Plain", "hero", "", "common", "Test",
                             0, 2, 30, 0, null, subtype: "Warlord");
        var d0 = new List<CardDef> { h0 };
        var d1 = new List<CardDef> { h1 };
        for (int i = 0; i < 12; i++)
        {
            d0.Add(Unit("T03b_MF0_" + i, 1, 1, 1));
            d1.Add(Unit("T03b_MF1_" + i, 1, 1, 1));
        }
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    static void TestW61bInitialModifiers()
    {
        // ---- 对照组（三个 modifier 全 0）----
        var baseCtx = InitialModifierFixture(0, 0, 0);
        int baseHand0 = baseCtx.Players[0].Hand.Count;
        int baseHand1 = baseCtx.Players[1].Hand.Count;
        int baseStones = baseCtx.Players[0].SpiritStones;
        RuleCore.BeginTurn(baseCtx);
        int baseMax = baseCtx.Players[baseCtx.Active].MaxEnergy;

        // ---- 实验组：+1 张起手 / +1 点法力上限 / +2 灵魂石 ----
        var ctx = InitialModifierFixture(1, 1, 2);
        Check(ctx.Players[0].Hand.Count, baseHand0 + 1,
              "★ `initialHandModifier = +1` ⇒ 起手多 1 张（原版 `_AddCardsToMulligan…:35-76`）"
            + "｜🧨 把 `NewBattle` 里 `seatHand[seat] = startHand + mod` 改回 `startHand` ⇒ 本行实得基准张数");
        Check(ctx.Players[1].Hand.Count, baseHand1,
              "★ **只影响自己那一侧**（对方督军没写这个 modifier ⇒ 起手张数不变）");
        Check(ctx.Players[0].SpiritStones, baseStones + 2,
              "★ `initialSpiritStoneModifier = +2` ⇒ 开局灵魂石 +2（原版 `SetupInitialMana.c:46` 的 "
            + "`ResetMana(…, initialSpiritStone)`）"
            + "｜🧨 删掉 `NewBattle` 里那一段 `SpiritStones += sp` ⇒ 本行实得基准值");

        RuleCore.BeginTurn(ctx);
        int act = ctx.Active;
        if (act == 0)
            Check(ctx.Players[0].MaxEnergy, baseMax + 1,
                  "★ `initialManaModifier = +1` ⇒ 第 1 回合能量上限 +1（原版 "
                + "`ResetMana(模式基数 + initialManaModifier, …)`）"
                + "｜🧨 删掉 `BeginTurn` 里 `MaxEnergy` 式子的那一项 ⇒ 本行实得基准值");
        else
            CheckTrue(false, "夹具前提不成立：第 1 回合的行动方不是座位 0");
    }

    // ==================================================================
    //  ③ `skipCardDraw` 的生产者
    // ==================================================================

    /// <summary>
    /// 夹具：一张带 `Rally: Gain +1 Attack` 的部队，**外加一条 `skipCardDraw` 规格**
    /// （`Keyword = "rally"`、`Uses = 1`、`Limited = true`）。
    /// 数据列（`cards_engine.json` 的 `skipDraws`）今天 0 张卡有，所以这里直接按
    /// `CardDef.SkipDrawSpec` 造 —— 语义与原版 `handBuff.cardEffect.buffType == 7` 一一对应。
    /// </summary>
    static BattleContext SkipDrawFixture()
    {
        var blocker = new CardDef("T03b_SD", "DrawBlocker", "unit", "Rally: Gain +1 Attack",
                                  "common", "Test", 1, 1, 5, 0, new[] { "Rally" },
                                  skipDraws: new[]
                                  {
                                      new CardDef.SkipDrawSpec
                                      {
                                          Keyword = "rally", Uses = 1, Limited = true,
                                          Source = "DrawBlocker（本回合不抽牌）",
                                      },
                                  });
        var h0 = new CardDef("T03b_SDW", "Host", "hero", "", "common", "Test",
                             0, 2, 30, 0, null, subtype: "Warlord");
        var h1 = new CardDef("T03b_SDE", "Foe", "hero", "", "common", "Test",
                             0, 2, 30, 0, null, subtype: "Warlord");
        // 🔴 **2026-10-22（`W62` · R7）就地订正（铁律 5）：`blocker` 必须放在**牌库顶**（= `d0` 末尾）。
        //   抽牌取的是 `Deck` **末尾**（`RuleCore.MoveDeckTopToHand`：`int last = ps.Deck.Count - 1`）。
        //   原来 `d0 = { h0, blocker } + 12 张垫牌` ⇒ blocker 在**列表头 = 牌库底**，
        //   起手 3 张只拿到 F11/F10/F9 ⇒ 下面「`DrawBlocker` 在起手牌里」这个前提不成立
        //   （先手只发 3 张，见 `_AddCardsToMulligan…:62-66` 的 `count`）。
        //   ⚠️ 这与 2026-10-21「起手张数」那批改动**无关** —— 改前改后都是 3 张。
        var d0 = new List<CardDef> { h0 };
        var d1 = new List<CardDef> { h1 };
        for (int i = 0; i < 12; i++)
        {
            d0.Add(Unit("T03b_SF0_" + i, 1, 1, 1));
            d1.Add(Unit("T03b_SF1_" + i, 1, 1, 1));
        }
        d0.Add(blocker);                          // ← 牌库顶（抽牌取 `Deck` 末尾）
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    static void TestW61bSkipCardDrawProducer()
    {
        var ctx = SkipDrawFixture();
        RuleCore.BeginTurn(ctx);
        int me = ctx.Active;
        if (me != 0) { CheckTrue(false, "夹具前提不成立：座位 0 不是先手"); return; }

        Check(ctx.Players[0].HandEffectRecords.Count, 0, "（前提）开局没有任何手牌效果记录");

        // ---- ① 打出这张牌 ⇒ `Rally` 触发时**挂上** `skipCardDraw` 记录 ----
        int idx = HandIdx(ctx, 0, "DrawBlocker");
        CheckTrue(idx >= 0, "（前提）`DrawBlocker` 在起手牌里");
        if (idx < 0) return;
        int before = ctx.Players[0].Hand.Count;
        RuleCore.PlayCard(ctx, 0, idx, 3);
        Check(ctx.Players[0].Hand.Count, before - 1, "打出去之后手牌 −1");
        Check(ctx.Players[0].HandEffectRecords.Count, 1,
              "★ `Rally` 触发 ⇒ 登记了 1 条 `skipCardDraw` 手牌效果（原版 `AbilityLogic__PlayAbility.c:1412` "
            + "→ `PlayerHand.AddHandEffect`）"
            + "｜🧨 删掉 `FireTriggerDeferred` 开头那段 `GrantSkipCardDraw` ⇒ 本行实得 0");
        var rec = ctx.Players[0].HandEffectRecords.Count > 0
                ? ctx.Players[0].HandEffectRecords[0] : null;
        CheckTrue(rec != null && rec.Op != null && rec.Op.Verb == RuleCore.SkipCardDrawVerb,
                  "★ 那条记录的载荷动词是 `" + RuleCore.SkipCardDrawVerb + "`（消费端只认这个动词）");
        CheckTrue(rec != null && rec.Uses != null && rec.Uses.Limited && rec.Uses.Left == 1,
                  "★ `limitedUses = true`、`numberOfUses = 1`（原版 `HandEffect.limitedUses/+0x28`、"
                + "`numberOfUses/+0x2C`）");

        // ---- ② 下一次**自己**的回合开始 ⇒ **不抽牌**，而且次数用尽后整条摘 ----
        int handBeforeTurn = ctx.Players[0].Hand.Count;
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);      // 对方回合（不该动我方手牌）
        Check(ctx.Players[0].Hand.Count, handBeforeTurn, "（前提）对方回合不改我方手牌数");
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);      // 我方回合 ⇒ 应当被跳过抽牌
        Check(ctx.Players[0].Hand.Count, handBeforeTurn,
              "★ 回合开始**不抽牌**（原版 `PlayerTurnStartCardDraw.c` → `CanDrawTurnStartCard` 命中 "
            + "`BuffType.skipCardDraw(7)`）"
            + "｜🧨 让 `ConsumeSkipCardDrawHandEffect` 返回 false ⇒ 本行实得手牌 +1");
        Check(ctx.Players[0].HandEffectRecords.Count, 0,
              "★ 次数用尽 ⇒ **整条摘掉**（原版 `numberOfUses--` 到 &lt;1 就 `RemoveHandEffectAt`）"
            + "｜🧨 去掉 `ConsumeSkipCardDrawHandEffect` 里那个 `Left < 1 ⇒ Remove` ⇒ 本行实得 1");

        // ---- ③ 再下一个自己的回合 ⇒ 恢复抽牌 ----
        int beforeNext = ctx.Players[0].Hand.Count;
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);
        Check(ctx.Players[0].Hand.Count, beforeNext + 1,
              "★ 效果摘掉之后**恢复抽牌**（手牌 +1）"
            + "｜🧨 让那条记录不被摘掉 ⇒ 本行实得手牌不变");
    }
}
