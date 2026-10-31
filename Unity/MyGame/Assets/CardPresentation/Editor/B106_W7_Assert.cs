// B106_W7_Assert.cs — 执行代理 `B106_W7`（完全复刻 · 第一百零六会话 · W4 那 4 条顺手发现的收口）
//
// ============================ 它钉的是什么（2026-10-31）============================
//  本笔把「阵营」的三处读口统一到原版的 **`CardDeck.deckArmy`**（`deck+0x50`），不再只看督军：
//    · W4①：`DeckRuntime.DeckArmyName` / `ArmyFaction` —— 「没督军但有卡」时原版显示**卡表第 0 张卡的阵营**；
//    · W4③：`DeckRules.Validate` / `RemoveInvalidCards` 的阵营支 —— 原版比 `deckArmy`，不是督军的阵营；
//    · W4⑤：`DeckEditorState.CanAddHero` / `CanAddDefensive` 的阵营支 —— 原版返回具名键
//      `MenuDeck/Error/InvalidArmy`，⛔ 不是 `WrongFaction`（后者上屏会落到 catch-all `InvalidDeck`）。
//
//  原版判据（全部现读 `d:/2/tools/decomp_full/`）：
//    · `CardDeck__CanAddCard.c:86-97`  —— `IsNeutralArmy` 跳过 + `deckArmy != cardArmy` + `CardCount(hero:1) > 0`
//    · `CardDeck__CanAddHero.c:74-80` / `CardDeck__CanAddDefensive.c`（阵营那两支）—— `DAT_1842cf3f0` = `InvalidArmy`
//    · `DeckUtility__ValidateDeckCards.c:58`（`card+0x2c != deck+0x50`）
//    · `DeckUtility__ValidateDeck.c:38-40`（`hero.cardArmy == deckArmy` 才往下走）
//    · `DeckUtility__TryRemoveInvalidCards.c:32-36`（同一句 `card+0x2c != deck+0x50`，**没有**中立例外）
//    · `CardDeck__AddCard.c` / `CardDeck__DeckBasicSetup.c`（`deckArmy` 的写点；见 `DeckRules.DeckArmy` 的 doc）
//  `deckArmy` 的**派生**（督军 > 卡表第 0 张 > Neutral）只一处 = `RuleEngine/Core/DeckRules.cs` 类尾 `DeckArmy`。
//
// ============================ 五段断言 ============================
//  A 前提：合成卡池（两阵营 + 一张中立卡）+ 两两不同阵营
//  B W4①：`DeckRuntime.DeckArmyName` —— 有督军 / **无督军但有卡（★ 本笔修的那格）** / 空卡组 / null state
//  C W4⑤：`CanAdd(HeroB)` / `CanAdd(DefB)` ⇒ `InvalidArmy`；同阵营换 ⇒ `None`；同一张 ⇒ `CopyLimitExceeded`
//  D W4③：`RemoveInvalidCards` 无督军时按**卡表第 0 张**的阵营摘（★ 本笔修的那格）；有督军时行为不回归
//  E W4③：`Validate` —— 有督军时仍报 `WrongFaction`（不回归）；无督军时报 `NoWarlord`（**已知不等价**，见报告 §施工单）
//        合法前提 = **15 张不同的同阵营 unit × 2 份 = 30 张**（⛔ 不能「同一张塞 30 张」：⑦ 同名上限 = 2 ⇒ 必红）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · **B2【核心判别式】**：把 `DeckArmyName` 改回旧写法（只看督军、查不到就 `NoArmyFaction`）
//        ⇒ 无督军那格退回 `Ultramarines` ⇒ 红。本笔 W4① 的修复就靠它判别。
//  · **D1【核心判别式】**：把 `RemoveInvalidCards` 的阵营支改回 `warlord != null && …` 旧写法
//        ⇒ 无督军那副**一张都不摘** ⇒ `rm.Count` 从 1 变 0 ⇒ 红。
//  · **C1/C3**：把 `CanAddHero` / `CanAddDefensive` 的返回码改回 `WrongFaction` ⇒ 两条一起红。
//  · B3/B4：把 `DeckArmyName` 的 null/空串兜底删掉 ⇒ 红；B4 同时钉「两个读口同一把尺」。
//  · E1/E2：把 `Validate` 的阵营支删掉 ⇒ E1 变 `None` ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  本件**不接线**（与 `B105_W3_Assert` / `B105_W4_Assert` 同档，等调度台统一接）。建议宿主 =
//  `CardPresentation/Editor/DeckScene.cs` 的**方法尾聚合行**（`Debug.Log(P + $"=== 合计：…`）**同一物理行**、
//  在它**第一个 `//` 之前**追加（行数零变化）：
//      B106W7_Assert.Run(); <宿主那两个计数变量> += B106W7_Assert.Pass + B106W7_Assert.Fail;
//  ⚠️ 追加前先看那一行**第一个 `//` 在哪** —— 2026-10-30 一天踩 6 次「代码落进注释里、`typecheck 0/0` 全绿、断言一条没跑」。
//  ⚠️ 本件**不需要** Transform / Canvas / 窗口：只建 `DeckEditorState` + 合成卡池。
//  ⚠️ `DeckRuntime.ArmyFaction()`（实例属性）**没有单独断言**：它需要一台真的 `DeckRuntime`（`State` 是
//     `private set`，只能从 `Build()` 来）；它的函数体与本件 B 段断言的 `DeckArmyName` 是**同一个表达式**
//     （`State.DeckArmyFaction()`）⇒ 逻辑由 B 段覆盖，此处如实标明这一格没直测。
//
// ============================ 判据 / 报告 ============================
//  · 报告：`资料/完全复刻/报告/B106_W7.md`
//  · 上游：`资料/完全复刻/报告/B105_W4.md` §6 顺①②③⑤ · `判据/B104_本会话待办与裁定.md` §七
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B106_W7`：钉**原版口径** —— 阵营读口一律是 `deckArmy`（督军 > 卡表第 0 张 > Neutral），
/// 且督军/防御卡槽那一支的错误码是 `InvalidArmy`（不是 `WrongFaction`）。</summary>
public static class B106W7_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B106W7] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ================================================================== 夹具
    //  🔶 自己设计（非原版）：**合成卡池**（自造阵营名 + 自造 id）。原版没有这两个阵营，这里只是把
    //     「同阵营 / 异阵营 / 中立」三档分开来测；卡型/稀有度取值照真实池子 · 调度台 2026-10-31 裁定（本件夹具，不进产品数据）。
    const string FactionA = "B106W7Alpha";
    const string FactionB = "B106W7Beta";
    const string NeutralName = "Neutral";          // 工程既有约定（同 `TargetCriteriaEval.cs:1156`）

    const string HeroA = "B106W7HeroA";
    const string HeroA2 = "B106W7HeroA2";
    const string HeroB = "B106W7HeroB";
    const string AlphaU1 = "B106W7A1";
    const string BetaU1 = "B106W7B1";
    const string NeutralU1 = "B106W7N1";
    const string DefA = "B106W7DefA";
    const string DefB = "B106W7DefB";

    static CardDef Hero(string id, string faction)
    {
        return new CardDef(id, id, "hero", "", "rare", faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }
    static CardDef Unit(string id, string faction)
    {
        // 准入两字段显式取**真实原版取值**（`InInventory = 0` / `Show = 5`）⇒ `CannotAddToDeck` 走正常放行分支；
        // 留默认 `-1` 会落进「真值缺失」兜底，虽然结论一样是「不拦」，但每条夹具都会打一条 `WarnUnknown`。
        return new CardDef(id, id, "unit", "", "common", faction, 1, 1, 1, 0,
                           inventoryOptions: DeckRules.InInventory,
                           showInCollectionOptions: DeckRules.ShowInCollection);
    }
    static CardDef Defence(string id, string faction)
    {
        return new CardDef(id, id, "defence", "", "common", faction, 0, 0, 0, 0);
    }

    static DeckEditorState Fresh()
    {
        var pool = new List<CardDef>
        {
            Hero(HeroA, FactionA), Hero(HeroA2, FactionA), Hero(HeroB, FactionB),
            Unit(AlphaU1, FactionA), Unit(BetaU1, FactionB), Unit(NeutralU1, NeutralName),
            Defence(DefA, FactionA), Defence(DefB, FactionB),
        };
        return new DeckEditorState(pool);
    }

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B106_W7：阵营读口 = 原版 `deckArmy`（督军 > 卡表第 0 张 > Neutral）· 槽位错误码 = `InvalidArmy`");
        A_Fixture();
        B_DeckArmyName();
        C_WarlordSlotErrorCode();
        D_RemoveInvalidCardsUsesDeckArmy();
        E_ValidateStillReportsWrongFaction();
    }

    // ==================================================================
    //  A：夹具与判据前提
    // ==================================================================
    static void A_Fixture()
    {
        var s = Fresh();
        var hA = s.Find(HeroA);
        var hB = s.Find(HeroB);
        var uA = s.Find(AlphaU1);
        var uB = s.Find(BetaU1);
        Tr(hA != null && hA.Type == "hero", "A1（前提）夹具能解析 `" + HeroA + "`，卡型 = hero");
        Tr(uA != null && uA.Type == "unit", "A2（前提）夹具能解析 `" + AlphaU1 + "`，卡型 = unit");
        Tr(uB != null && uB.Type == "unit", "A3（前提）夹具能解析 `" + BetaU1 + "`，卡型 = unit");
        Tr(hA != null && hB != null && !DeckRules.SameFaction(hA.Faction, hB.Faction),
           "A4（前提）Alpha / Beta 两档阵营**确实不同**（否则整套「异阵营」断言等于什么都没测）");
        Tr(s.Find(DefB) != null && s.Find(DefB).Type == "defence", "A5（前提）夹具能解析 `" + DefB + "`，卡型 = defence");
        C(DeckRuntime.NoArmyFaction, "Ultramarines",
          "A6（前提）`NoArmyFaction` = `Ultramarines`（原版 `CardArmy` 那个字面量默认档 `10`）—— 它**不等于**夹具任一真阵营，B 段才能判别");
    }

    // ==================================================================
    //  B：W4① `DeckRuntime.DeckArmyName` —— 原版 `deckArmy`
    // ==================================================================
    static void B_DeckArmyName()
    {
        // B1 有督军 ⇒ 督军阵营
        {
            var s = Fresh();
            s.LoadDeck(new PlayerDeck("B1", HeroB, null, null, 0));
            C(DeckRuntime.DeckArmyName(s), FactionB,
              "B1 ★ 有督军 ⇒ 页头/四格阵营名 = 督军阵营（原版 `DeckBasicSetup` / `AddHero` 写 `deckArmy = hero.cardArmy`）");
            C(s.DeckArmyFaction(), FactionB, "B1-a 同一把尺：`DeckArmyFaction()` 与 `DeckArmyName` 一致");
        }
        // B2 ★ 本笔修的那格：没督军、有卡 ⇒ 卡表第 0 张卡的阵营（旧写法会给 `NoArmyFaction`）
        {
            var s = Fresh();
            s.LoadDeck(new PlayerDeck("B2", null, null, new[] { AlphaU1 }, 0));   // ⚠️ 只放同阵营一张：`LoadDeck` 会跑 `RemoveInvalidCards`，混阵营会打 LogWarning 噪声；混阵营那格在 D1 直接测
            C(DeckRuntime.DeckArmyName(s), FactionA,
              "B2 ★★ **无督军但有卡** ⇒ 原版 `deckArmy` = 卡表第 0 张卡的阵营（`AddCard` 的 `count==0` 那支）"
              + " ⇒ 页头徽记/四格读的是 `" + FactionA + "`，**不是** `Ultramarines`");
            C(s.DeckArmyFaction(), FactionA, "B2-a 同一把尺：`DeckArmyFaction()` 也是第 0 张卡的阵营");
        }
        // B3 空卡组 ⇒ 原版默认档
        {
            var s = Fresh();
            s.LoadDeck(new PlayerDeck());
            C(DeckRuntime.DeckArmyName(s), DeckRuntime.NoArmyFaction,
              "B3 ★ 空卡组（没督军、没卡）⇒ `NoArmyFaction`（= 原版 `deckArmy == 0` 时 `FilterToDeck` 那个字面量 10）");
        }
        // B4 null state 不抛
        {
            string got = null; bool threw = false;
            try { got = DeckRuntime.DeckArmyName(null); } catch (System.Exception) { threw = true; }
            Tr(!threw && got == DeckRuntime.NoArmyFaction,
               "B4 ★ `state == null` ⇒ 不抛、返回 `NoArmyFaction`（读口对空输入的兜底，⛔ 不许 NRE）");
        }
    }

    // ==================================================================
    //  C：W4⑤ 督军 / 防御卡槽的阵营支 ⇒ `InvalidArmy`（原版 `DAT_1842cf3f0`）
    // ==================================================================
    static void C_WarlordSlotErrorCode()
    {
        {
            var s = Fresh();
            s.LoadDeck(new PlayerDeck("C1", HeroA, null, null, 0));
            C(s.CanAdd(s.Find(HeroB)), DeckError.InvalidArmy,
              "C1 ★★ 槽里是**同型异阵营**督军 ⇒ `InvalidArmy` —— 原版 `CardDeck__CanAddHero.c:74-80` 给的是具名键 "
              + "`MenuDeck/Error/InvalidArmy`（`DAT_1842cf3f0`），改回 `WrongFaction` 这条就红");
        }
        {
            var s = Fresh();
            s.LoadDeck(new PlayerDeck("C2", HeroA, null, null, 0));
            C(s.CanAdd(s.Find(HeroA2)), DeckError.None,
              "C2 同阵营换督军 ⇒ 放行（原版 `CanAddHero` 只挡异阵营，不变量：换的同阵营卡组仍合法）");
            C(s.CanAdd(s.Find(HeroA)), DeckError.CopyLimitExceeded,
              "C2-b 槽里**就是这一张** ⇒ `CopyLimitExceeded`（= 原版 `MenuDeck/Error/MaxCopiesReached`；不回归）");
        }
        {
            var s = Fresh();
            s.LoadDeck(new PlayerDeck("C3", null, DefA, null, 0));
            C(s.CanAdd(s.Find(DefB)), DeckError.InvalidArmy,
              "C3 ★★ 槽里是**异阵营**防御卡 ⇒ `InvalidArmy` —— 原版 `CardDeck__CanAddDefensive.c` 阵营那支也是 "
              + "`DAT_1842cf3f0`；改回 `WrongFaction` 这条就红");
        }
        {
            var s = Fresh();
            s.LoadDeck(new PlayerDeck("C4", null, DefA, null, 0));
            C(s.CanAdd(s.Find(DefA)), DeckError.CopyLimitExceeded,
              "C4 防御卡槽里就是这一张 ⇒ `CopyLimitExceeded`（不回归）");
        }
    }

    // ==================================================================
    //  D：W4③ `RemoveInvalidCards` 比 `deckArmy`
    // ==================================================================
    static void D_RemoveInvalidCardsUsesDeckArmy()
    {
        // D1 ★ 无督军 ⇒ 原版 `deckArmy` = 卡表第 0 张卡的阵营 ⇒ 异阵营那张要摘掉
        //     （旧写法 `warlord != null && …` 在这一格里**一张都不摘** ⇒ 恒 0 条）
        {
            var s = Fresh();
            var deck = new PlayerDeck("D1", null, null, new[] { AlphaU1, BetaU1 }, 0);
            var rm = DeckRules.RemoveInvalidCards(deck, s.Find);
            C(rm.Count, 1,
              "D1 ★★ **无督军**的卡组：`deckArmy` = 卡表第 0 张（`" + FactionA + "`）⇒ 异阵营那张被摘 —— "
              + "旧写法（只在 `warlord != null` 时比）在这一格恒 0 条");
            Tr(rm.Count == 1 && rm[0].Id == BetaU1 && rm[0].Why == DeckError.WrongFaction,
               "D1-a 摘掉的是 `" + BetaU1 + "`、原因码 = `WrongFaction`（原版同样是错阵营那一支）");
            C(deck.CardIds.Count, 1, "D1-b 只剩 1 张");
            Tr(deck.CardIds.Count == 1 && deck.CardIds[0] == AlphaU1, "D1-c 留下的正是第 0 张那张（同阵营）");
        }
        // D2 有督军 ⇒ 行为不回归（异阵营仍被摘）
        {
            var s = Fresh();
            var deck = new PlayerDeck("D2", HeroA, null, new[] { AlphaU1, BetaU1 }, 0);
            var rm = DeckRules.RemoveInvalidCards(deck, s.Find);
            Tr(rm.Count == 1 && rm[0].Why == DeckError.WrongFaction,
               "D2 ★ 有督军时行为**不回归**：异阵营那张照旧被摘（原版 `TryRemoveInvalidCards.c:32-36`）");
            C(deck.CardIds.Count, 1, "D2-a 只剩同阵营那张");
        }
        // D3 中立卡不算法外开恩：`TryRemoveInvalidCards` 那一支原版**没有** `IsNeutralArmy` 例外 ⇒
        //    中立卡在「有督军」时要被摘（与 `CanAddCard` 那支的差异是原版自己的，⛔ 别顺手统一）
        {
            var s = Fresh();
            var deck = new PlayerDeck("D3", HeroA, null, new[] { NeutralU1 }, 0);
            var rm = DeckRules.RemoveInvalidCards(deck, s.Find);
            Tr(rm.Count == 1 && rm[0].Why == DeckError.WrongFaction,
               "D3 ★ 中立卡在**校验/清理**这一侧被摘（原版这一支没有中立例外 —— 与 `CanAddCard` 的差异是原版自己的，照抄别修）");
        }
    }

    // ==================================================================
    //  E：W4③ `Validate` 的阵营支（不回归 + 已知不等价如实钉住）
    // ==================================================================
    static void E_ValidateStillReportsWrongFaction()
    {
        // 30 张（经典档）才过得了 ④ 张数那一关，才轮得到 ⑤ 阵营。
        // 🔴 **必须 15 张不同的同阵营 unit × 2 份** —— ⑦ 同名上限 = `CopyLimit("common","unit")`
        //    = `GameplayVariables.Classic.numberOfCopiesOtherRarities` = **2**（`DeckRules.cs:516`/`:162-168`）。
        //    原来「同一张 `AlphaU1` 塞 30 张」在 ④⑤⑥ 全过之后**必然**撞 ⑦ ⇒ 这条前提在任何输入下都不成立。
        //    E1 仍可用第 30 位换异阵营（⑤ `:489` 先于 ⑦ `:516`）；E2 无督军（② `:467` 先返回）。
        var pool = new List<CardDef> { Hero(HeroA, FactionA), Unit(BetaU1, FactionB) };
        var ok = new List<string>();
        for (int i = 0; i < 15; i++)
        {
            var id = "B106W7EU" + i;            // 🔶 自己设计（非原版）：合成池自造 id，同本文件既有夹具口径
            pool.Add(Unit(id, FactionA));
            ok.Add(id);
            ok.Add(id);                          // 每张 2 份 ⇒ 30 张，恰好等于上限（不是超过）
        }
        var s = new DeckEditorState(pool);

        C(DeckRules.Validate(new PlayerDeck("E0", HeroA, null, ok, 0), s.Find), DeckError.None,
          "E0（前提）30 张同阵营（15 张不同 id × 2 份）⇒ 合法（否则下面两条可能只是撞在张数上）");

        var mixed = new List<string>(ok);
        mixed[29] = BetaU1;                     // 换成一张异阵营（张数不变 ⇒ 先过 ④；⑤ 先于 ⑦）
        C(DeckRules.Validate(new PlayerDeck("E1", HeroA, null, mixed, 0), s.Find), DeckError.WrongFaction,
          "E1 ★ 有督军 + 一张异阵营 ⇒ `WrongFaction`（`ValidateDeckCards.c:58` 的 `cardArmy != deckArmy`；不回归）");

        // 无督军：原版会先跑 `ValidateDeckCards`，但 `ValidateDeck.c` 的 `pDeckHero == null` 那一支**先**判 2
        //   ⇒ 两边都是「没督军」这条错；⛔ 这一格是**已知不等价**的另一半（见报告 §施工单），此处只钉现状。
        C(DeckRules.Validate(new PlayerDeck("E2", null, null, ok, 0), s.Find), DeckError.NoWarlord,
          "E2 无督军 ⇒ `NoWarlord`（= 原版 `ValidateDeck.c` 的 `MissingHero` 那一档；**已知不等价的边界**："
          + "原版 `deckArmy` 在导入无督军串时恒 Neutral，见报告 §施工单）");
    }
}
