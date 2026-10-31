// B105_W3_Assert.cs — 执行代理 `B105_W3`（完全复刻 · 玩家主链 §一 #1：**跨阵营换战将不清旧卡**）
//
// ============================ 它钉的是什么（2026-10-31 第二版 · 按调度台裁定改向）============================
//  🔴 **结论：原版编辑期就是【不清】卡 —— 这不是缺陷，是原版形状。** 所以本件**不接线**，
//     断言改成钉**原版行为**，防止以后有人「顺手把清卡接到玩家路径上」（= 造出原版没有的行为）。
//
//  原版那条链（逐句现读，判据见下）：
//    ① 加/换督军 = `CardDeck.AddHero` 只写 `deckHero` + `deckArmy`（`CardDeck.cs:922-927`）；
//       玩家路径 = `DeckEditingPanel.AddCard`（`DeckEditingPanel.cs:305-357`）→ `CardDeck.TryAddCard`（`CardDeck.cs:680-744`）。
//    ② 异阵营能不能换：`CardDeck.CanAddHero`（`CardDeck.cs:960-1048`）**只在 `deckHero != null` 时比阵营**
//       ⇒ **槽里没人时直接放行** ⇒ 「先把督军拖出侧栏、再点另一个阵营的督军」**能成功**，旧卡原样留着。
//    ③ 于是卡组非法：`DeckUtility.ValidateDeck`（`DeckUtility.cs:172-284`）→ `:254` `ValidateDeckCards` 先跑
//       （逐张：卡型 / 入池 / 显示 / **阵营** / 同名上限；`decomp_full/DeckUtility__ValidateDeckCards.c:60-64`：
//       `cardArmy != deck.deckArmy` ⇒ `*param_3 = 4` = 阵营错）⇒ 张数那一关在**它之后**
//       （`decomp_full/DeckUtility__ValidateDeck.c:52,68-69`）⇒ `DeckEditingWindow.UpdateDoneButton`
//       （`DeckEditingWindow.cs:1470-1521`）拿返回值点/熄 Done。
//    ④ **清卡只发生在下一次开窗那一拍**：`DeckEditingWindow__TryOpen.c:41-47` —— 先 `new CardDeck(deckToCopy)` 拷一份，
//       `:47` `DeckUtility.RemoveInvalidCards(副本)`（`DeckUtility.cs:628` → `TryRemoveInvalidCards` `:798`）。
//       = 我们 `DeckEditorState.LoadDeck`（`Deck/DeckEditorState.cs:323` 的 `DeckRules.RemoveInvalidCards`）；**已实现**。
//    ⑤ `CardDeck.RemoveNoValidArmyCards`（`CardDeck.cs:1497-1541` / `decomp_full/CardDeck__RemoveNoValidArmyCards.c:21-61`）
//       规则确实存在（阵营不符就摘 + `LogWarning`），但**在原版本 build 里全库 0 调用点**
//       （`decomp_full/*.c` + `out_cpp2il_dev_ilspy` + `out_il2csharp` 三源 grep 只命中定义；
//        `assets_full` / AssetRipper 导出工程的 `m_MethodName` 也 0 命中）。成对的
//       `CardDeck.AreThereInvalidArmyCards`（`CardDeck.cs:1348`）同样 0 调用点 ⇒ 原版把这条设计**留在类里没人用**。
//
// ============================ 夹具口径（关键）============================
//  🔴 **必须把普通卡位填满（= `MaxDeckCount`）**，否则「张数不够」会先把 `Validate()` 短路掉，
//  测不到阵营那一条（⚠️ 我们 `DeckRules.Validate` 是 ④张数 → ⑤阵营；原版是**逐张（含阵营）→ 再比张数**，
//  顺序相反 —— 那是**另一条**已知偏离，见报告「顺手发现 ⑤」，本件不改 `DeckRules`）。
//  填满之后「张数」那关先过 ⇒ 两边**都**会报到阵营。
//
// ============================ 五段断言 ============================
//  A 前提：夹具（合成卡池，两阵营 + 20 张 Alpha 部队；不走真卡池 ⇒ `BatchMode` 能跑、不受数据换号影响）
//  B ① ★ 无督军 + 卡组满编全是 A 阵营时，选 **B 阵营**督军 ⇒ 换将**成功**、**旧卡一张不少**（防御卡也留着）
//  C ② 于是 `Validate()` 报 **`WrongFaction`** ⇒ `Done` 该被熄（原版 `UpdateDoneButton` 就是这一手），且校验只读
//  D ③ 同阵营换将 ⇒ 卡不动、防御卡不动、`Validate()` 与换之前**同一个码**（合法组换完仍合法）
//  E ④ 把 C 那副非法卡组**重新 `LoadDeck`**（= 原版开窗那一跳）⇒ `RemoveInvalidCards` 清掉那 30 张旧卡，
//        而督军位 / 防御卡位 / 传进去那一副**原封不动**
//  F 空卡组选督军（基本路径没被上一条带坏）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · **B5/B6【核心判别式】**：哪天有人又把「换将就清卡」接回 `TryAdd`（那就是原版没有的行为）⇒ 卡数 30→0、
//        防御卡被清 ⇒ B5 与 B6 同时红。**这正是本件要守的那条线。**
//  · C1：如果 `Validate` 漏了阵营那一支（或有人把 `CanAddCore` 的阵营闸门也拆了）⇒ 卡组不再非法 ⇒ C1 红。
//  · C3：`Validate` 哪天顺手清卡（原版它只补空名字）⇒ 卡数变 0 ⇒ 红。
//  · D3/D4：把换将写成「换了就清空」⇒ 同阵营那一段卡数/防御卡不对 ⇒ 红。
//  · E1/E2/E3/E7：`LoadDeck` 哪天不再跑 `RemoveInvalidCards`（= 原版开窗那一跳丢了）⇒ 卡数仍 30、`LastLoadRemoved` 空 ⇒ 一起红。
//  · E4/E5：把清卡做成「整副重置」（连督军/防御卡一起清）⇒ 红（原版只清 `cardLibrary`）。
//  · E6：`LoadDeck` 哪天不再拷副本（直接改传进来那一副）⇒ 外部那副被改 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/DeckScene.cs` 的**方法尾聚合行**（`Debug.Log(P + $"=== 合计：…`）**同一物理行**、
//  在它**第一个 `//` 之前**追加（行数零变化）：
//      B105W3_Assert.Run(); <宿主那两个计数变量> += B105W3_Assert.Pass + B105W3_Assert.Fail;
//  ⚠️ 追加前先看那一行**第一个 `//` 在哪** —— 2026-10-30 一天踩 6 次「代码落进注释里、`typecheck 0/0` 全绿、断言一条没跑」。
//  ⚠️ 本件**不需要** Transform / Canvas / 窗口：只建 `DeckEditorState` + 合成卡池。
//
// ============================ 判据 / 报告 ============================
//  · 原版：`CardDeck.cs:680-744`(TryAddCard) / `:922-927`(AddHero) / `:960-1048`(CanAddHero) / `:1348`(AreThereInvalidArmyCards)
//          / `:1497-1541`(RemoveNoValidArmyCards) · `DeckUtility.cs:172-284`(ValidateDeck) / `:628`(RemoveInvalidCards)
//          · `decomp_full/DeckEditingWindow__TryOpen.c:41-47` · `decomp_full/DeckUtility__ValidateDeckCards.c:60-64`
//  · 本件报告：`资料/完全复刻/报告/B105_W3.md`
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B105_W3`：钉**原版行为** —— 编辑期跨阵营换将**不清卡**，旧卡留到下一次开窗才被 `RemoveInvalidCards` 清。</summary>
public static class B105W3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B105W3] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ================================================================== 夹具
    //  🔶 自己设计（非原版）：**合成卡池**（两个自造阵营名 + 自造 id）· 原版没有这两个阵营，这里只是把
    //     「同阵营 / 异阵营」两档分开来测；卡型/稀有度取值照真实池子（`common` 的 `CopyLimit` = 2）·
    //     调度台 2026-10-31 裁定（本件夹具，不进产品数据）。
    const string FactionA = "B105W3Alpha";
    const string FactionB = "B105W3Beta";

    const string HeroA1 = "B105W3HeroA1";
    const string HeroA2 = "B105W3HeroA2";
    const string HeroB1 = "B105W3HeroB1";
    const string DefA = "B105W3DefA";
    const string UnitB = "B105W3UnitB";

    /// <summary>Alpha 阵营的 20 张部队（×2 份上限 = 够把普通卡位填满，`MaxDeckCount` 是多少都够）。</summary>
    const int AlphaUnitCount = 20;
    static string AlphaUnitId(int i) { return "B105W3UA" + i.ToString("00"); }

    static CardDef Hero(string id, string faction)
    {
        return new CardDef(id, id, "hero", "", "rare", faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }
    static CardDef Unit(string id, string faction)
    {
        return new CardDef(id, id, "unit", "", "common", faction, 1, 1, 1, 0);
    }
    static CardDef Defence(string id, string faction)
    {
        return new CardDef(id, id, "defence", "", "common", faction, 0, 0, 0, 0);
    }

    static DeckEditorState Fresh()
    {
        var pool = new List<CardDef>
        {
            Hero(HeroA1, FactionA), Hero(HeroA2, FactionA), Hero(HeroB1, FactionB),
            Unit(UnitB, FactionB), Defence(DefA, FactionA),
        };
        for (int i = 0; i < AlphaUnitCount; i++) pool.Add(Unit(AlphaUnitId(i), FactionA));
        return new DeckEditorState(pool);
    }

    /// <summary>A 阵营督军 + A 阵营防御卡 + **填满普通卡位**。
    /// ⚠️ 必须填满：否则「张数不够」会先把 `Validate()` 短路掉，测不到阵营那一条。</summary>
    static void FillA(DeckEditorState s, out string why)
    {
        s.TryAdd(s.Find(HeroA1), out why);
        s.TryAdd(s.Find(DefA), out why);
        for (int i = 0; i < AlphaUnitCount && s.DeckCount < s.MaxDeckCount; i++)
            for (int k = 0; k < 2 && s.DeckCount < s.MaxDeckCount; k++)
                s.TryAdd(s.Find(AlphaUnitId(i)), out why);
    }

    /// <summary>玩家真流程走完那一幕：把 A 阵营督军拖出侧栏 ⇒ 选 **B 阵营**督军（槽里没人 ⇒ 原版放行）。
    /// 返回**非法**的那副卡组（编辑期不清卡）。</summary>
    static DeckEditorState AfterCrossFactionSwap()
    {
        var s = Fresh();
        string why;
        FillA(s, out why);
        s.TryRemove(s.Find(HeroA1));
        s.TryAdd(s.Find(HeroB1), out why);
        return s;
    }

    /// <summary>唯一的入口（宿主 `DeckScene` 的聚合行里调）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B105_W3：**原版编辑期不清卡** —— 跨阵营换将后旧卡留着、`Validate` 报 WrongFaction、下次开窗才清");
        A_Fixture();
        B_EditTimeKeepsOldCards();
        C_DoneIsBlocked();
        D_SameFactionSwapIsInert();
        E_ReopenPurgesInvalidCards();
        F_EmptyDeckBaseline();
    }

    // ==================================================================
    //  A：夹具前提（不成立时下面全不值得看）
    // ==================================================================
    static void A_Fixture()
    {
        var s = Fresh();
        var hA = s.Find(HeroA1);
        var hB = s.Find(HeroB1);
        Tr(hA != null && hA.Type == "hero", "A1（前提）夹具能解析 `" + HeroA1 + "`，卡型 = hero");
        Tr(hB != null && hB.Type == "hero", "A2（前提）夹具能解析 `" + HeroB1 + "`，卡型 = hero");
        Tr(hA != null && hB != null && !DeckRules.SameFaction(hA.Faction, hB.Faction),
           "A3（前提）两位督军的阵营**确实不同**（否则 B 段测的其实是同阵营换将，等于什么都没测）");
        var dA = s.Find(DefA);
        Tr(dA != null && dA.Type == "defence", "A4（前提）`" + DefA + "` 卡型 = defence（防御卡槽夹具）");
        C(s.MaxDeckCount, DeckRules.CardCount(s.Skirmish),
          "A5（前提）`MaxDeckCount` = `DeckRules.CardCount(Skirmish)`（夹具要按这个数填满）");
    }

    // ==================================================================
    //  B ①：★ 编辑期跨阵营换将 —— **旧卡一张不少**（原版 `AddHero`/`TryAddCard` 不清卡）
    // ==================================================================
    static void B_EditTimeKeepsOldCards()
    {
        var s = Fresh();
        string why;

        FillA(s, out why);                 // 督军 + 防御卡 + 把普通卡位填满
        C(s.Deck.WarlordId, HeroA1, "B0（前提）A 阵营督军已就位");
        C(s.Deck.DefensiveId, DefA, "B0b（前提）防御卡位 = A 阵营那张");
        C(s.DeckCount, s.MaxDeckCount, "B1（前提）普通卡位**填满**（= `MaxDeckCount`，这样张数那关不会短路阵营判定）");
        C(s.Validate(), DeckError.None, "B1b（前提）填满且同阵营 ⇒ 此刻卡组**合法**（`Validate()` = `None`）");

        Tr(s.TryRemove(s.Find(HeroA1)), "B2（前提）把督军移除（= 玩家把督军行拖出侧栏，`DeckRuntime` 那条路）");
        C<string>(s.Deck.WarlordId, null, "B2b（前提）督军槽已空");
        C(s.DeckCount, s.MaxDeckCount,
          "B2c（前提）移除督军**不清卡**（原版 `CardDeck__RemoveCard.c`：hero 那一支只把 `+0x40` 清零）");

        Tr(s.TryAdd(s.Find(HeroB1), out why),
           "B3 ★ 跨阵营换将**成功** —— 原版 `CanAddHero` 在 `deckHero == null` 时**不比阵营**，直接放行 —— " + why);
        C(s.Deck.WarlordId, HeroB1, "B4 督军槽 = B 阵营那位");

        C(s.DeckCount, s.MaxDeckCount,
          "B5 ★★【核心判别式】**旧阵营那些卡一张不少**（仍是满编）—— 原版编辑期**不清卡**；"
        + "若哪天把「换将就清卡」接回 `TryAdd`（= 原版没有的行为）⇒ 这里是 0 ⇒ 红");
        C(s.Deck.DefensiveId, DefA,
          "B6 ★★ 防御卡**也留着**（原版 `AddHero` / `TryAddCard` 对防御卡位一个字都不碰）");
    }

    // ==================================================================
    //  C ②：于是卡组非法 —— `Validate()` 报 `WrongFaction`（`Done` 该被熄），且校验只读
    // ==================================================================
    static void C_DoneIsBlocked()
    {
        var s = AfterCrossFactionSwap();
        C(s.Deck.WarlordId, HeroB1, "C0（前提）重演到「跨阵营换将刚完成」，督军 = B 阵营那位");
        C(s.DeckCount, s.MaxDeckCount, "C0b（前提）旧阵营那些卡还在（编辑期不清）⇒ 张数仍是满编");

        int mismatched = 0;
        foreach (var id in s.Deck.CardIds)
        {
            var c = s.Find(id);
            if (c != null && !DeckRules.SameFaction(c.Faction, FactionB)) mismatched++;
        }
        C(mismatched, s.MaxDeckCount, "C0c（前提）那些卡**全部**与督军不同阵营（否则下面那条没有判别力）");

        C(s.Validate(), DeckError.WrongFaction,
          "C1 ★ `Validate()` = `WrongFaction` —— 原版 `ValidateDeckCards` 的阵营那一支（`DeckUtility__ValidateDeckCards.c:60-64`）");
        Tr(s.Validate() != DeckError.None,
           "C2 ★ `Done` 该被熄：原版 `UpdateDoneButton` 就是拿 `ValidateDeck` 的返回值开灯 —— 非 `None` 就是不许保存");
        C(s.DeckCount, s.MaxDeckCount, "C3 ★ 校验**只读**：`Validate()` 不许顺手清卡（原版 `ValidateDeck` 只补空名字，不改卡表）");
    }

    // ==================================================================
    //  D ③：同阵营换将 ⇒ 卡不动、防御卡不动、`Validate()` 同一个码
    // ==================================================================
    static void D_SameFactionSwapIsInert()
    {
        var s = Fresh();
        string why;

        FillA(s, out why);
        C(s.Deck.WarlordId, HeroA1, "D0（前提）A 阵营督军已就位");
        C(s.DeckCount, s.MaxDeckCount, "D0b（前提）普通卡位满编");
        var before = s.Validate();
        C(before, DeckError.None, "D0c（前提）换之前卡组**合法**（这样「换完仍合法」才有判别力）");

        Tr(s.TryAdd(s.Find(HeroA2), out why), "D1 同阵营换将**成功**（原版 `CanAddHero` 只挡异阵营）—— " + why);
        C(s.Deck.WarlordId, HeroA2, "D2 督军槽 = A 阵营另一位");
        C(s.DeckCount, s.MaxDeckCount, "D3 ★【判别式】同阵营换将**一张都不许少** —— 写成「换将就清空」⇒ 这里 0≠满编");
        C(s.Deck.DefensiveId, DefA, "D4 ★【判别式】同阵营换将**防御卡也留着**");
        C(s.Validate(), before, "D5 `Validate()` 与换之前**同一个码**（换将不改变卡组合法性）—— 换前 = " + before);
        Tr(s.Validate() != DeckError.WrongFaction, "D6 同阵营换将**不该**报阵营错（阵营没变）");
    }

    // ==================================================================
    //  E ④：原版**开窗那一跳**才清 —— 重新 `LoadDeck` 同一副非法卡组 ⇒ `RemoveInvalidCards` 清掉旧卡
    //      （判据 = `DeckEditingWindow__TryOpen.c:41-47`；我们 = `DeckEditorState.LoadDeck` → `DeckRules.RemoveInvalidCards`）
    // ==================================================================
    static void E_ReopenPurgesInvalidCards()
    {
        var dirty = AfterCrossFactionSwap();
        C(dirty.DeckCount, dirty.MaxDeckCount, "E0（前提）手里这副是**非法**卡组：旧阵营那些卡还在（满编）");
        C(dirty.Validate(), DeckError.WrongFaction, "E0b（前提）它确实非法（`WrongFaction`）");
        int dirtyCount = dirty.DeckCount;

        var s = Fresh();
        s.LoadDeck(dirty.Deck);            // = 原版 `TryOpen`：先拷一份副本，再 `RemoveInvalidCards`

        C(s.DeckCount, 0, "E1 ★ 开窗那一拍**把那批旧阵营卡全清掉了**（原版 `DeckUtility.RemoveInvalidCards` 的阵营那一支）");
        C(s.LastLoadRemoved.Count, dirtyCount, "E2 ★ `LastLoadRemoved` 条数 = 被清掉的张数（我们这条链的结果口，供调用方/日志用）");
        int wrong = 0;
        foreach (var r in s.LastLoadRemoved) if (r.Why == DeckError.WrongFaction) wrong++;
        C(wrong, dirtyCount, "E3 ★ 每一条的原因**都是** `WrongFaction`（不是「查不到卡」之类的别的支）");
        C(s.Deck.WarlordId, HeroB1,
          "E4 ★ 督军位**没被清**（原版 `TryRemoveInvalidCards` 只遍历 `cardLibrary`；督军位另有 `TryRemoveInvalidHero`，只摘「不是 hero」的）");
        C(s.Deck.DefensiveId, DefA,
          "E5 ★ 防御卡位**也没被清**（`DeckRules.RemoveInvalidCards` 的合同：不碰防御卡位 = 原版 `TryRemoveInvalidCards` 只扫 `cardLibrary`）");
        C(dirty.DeckCount, dirtyCount,
          "E6 ★ 传进去那一副**一个字节没动**（原版 `TryOpen` 先 `new CardDeck(deckToCopy)`；我们 `LoadDeck` 先 `Clone()`）");
        C(s.Validate(), DeckError.TooFewCards,
          "E7 ★ 清完之后**不再报 `WrongFaction`**，只剩「张数不够」（= 原版：开窗清完仍然不合法，但不合法在**别处**）");
    }

    // ==================================================================
    //  F：空卡组选督军（基本路径没被上一条带坏）
    // ==================================================================
    static void F_EmptyDeckBaseline()
    {
        var s = Fresh();
        string why;

        Tr(s.TryAdd(s.Find(HeroB1), out why), "F1 空卡组直接选 B 阵营督军 —— " + why);
        C(s.Deck.WarlordId, HeroB1, "F2 督军槽 = B 阵营那位");
        C(s.DeckCount, 0, "F3 空卡组 ⇒ 没有卡可清");
        C(s.Validate(), DeckError.TooFewCards, "F4 `Validate()` = 张数不够（没有阵营错可报）");
    }
}
