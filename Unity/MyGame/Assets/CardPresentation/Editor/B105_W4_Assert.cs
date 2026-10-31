// B105_W4_Assert.cs — 执行代理 `B105_W4`（完全复刻 · 账 `卡组编辑:8`：**`CanAddCore` 的阵营闸口径 = 原版 `deckArmy`**）
//
// ============================ 它钉的是什么（2026-10-31）============================
//  原版 `CardDeck.CanAddCard` 判「这张卡能不能加」时，比的是 **`CardDeck.deckArmy` 这个字段**，
//  **不是督军的阵营**；而且有两条容易被漏掉的边：
//    · **中立阵营卡不挡**（`GameStaticData.IsNeutralArmy(card.cardArmy)` ⇒ 整条闸跳过）；
//    · **`CardCount(countHero:true) > 0` 才挡**（空卡组一律放行）。
//
//  原版那一支（`d:/2/tools/decomp_full/CardDeck__CanAddCard.c:86-97`，逐句）：
//      uVar1 = *(undefined4 *)(param_2 + 0x2c);            // card.cardArmy
//      cVar2 = GameStaticData__IsNeutralArmy(uVar1,0);      // 中立 ⇒ '\0' 以外
//      if (((cVar2 == '\0') && (*(int *)(param_1 + 0x50) != *(int *)(param_2 + 0x2c))) &&
//         (iVar3 = CardDeck__CardCount(param_1,1,0), 0 < iVar3)) {
//          uVar5 = DAT_1842cf3f0;                           // = 字面量 "MenuDeck/Error/InvalidArmy"
//        }
//    · `param_1 + 0x50` = `CardDeck.deckArmy`（字段序 → 偏移：`deckHero +0x40` / `defensiveCard +0x48` /
//      `deckArmy +0x50`，见 `Assembly-CSharp/CardDeck.cs:22-28` 的字段序 + `RemoveCard` 里 `this+0x40/0x48` 那两处）；
//    · `param_2 + 0x2c` = 卡的 `cardArmy`；`param_3` = `out string error`（原版直接给它**本地化好的人话**）。
//  `DAT_1842cf3f0` = `MenuDeck/Error/InvalidArmy`（地址表 → `RuleEngine/Core/DeckRules.cs` 的 `Describe` doc；
//  官方表：EN `Cannot add a card from other army` / ZH「不能添加其他军队的牌」）。
//
//  `CardCount(bool countHero)`（`decomp_full/CardDeck__CardCount.c`，本仓另有同一份判据）=
//      countHero && deckHero != null ⇒ cardLibrary.Count + 1 ; 否则 ⇒ cardLibrary.Count（**防御卡不算**）。
//  ⇒ 我们这边的等价物 = `DeckEditorState.DeckCardCount(true)`。
//
// `deckArmy` 的**全部写点**与「为什么这里用派生」→ `RuleEngine/Core/DeckRules.cs` 类尾 `DeckArmy` 的 doc。
//
// ============================ 六段断言 ============================
//  A 前提：夹具（合成卡池；两阵营 + 一张中立卡）+ `IsNeutralFaction` 的口径
//  B `DeckRules.DeckArmy` 的派生（督军 > 卡表第 0 张 > Neutral）
//  C 验收①：**有督军 + 卡组非空**，加异阵营卡 ⇒ 被挡、码 = `InvalidArmy`、上屏键 = `MenuDeck/Error/InvalidArmy`
//  D 验收②：**空卡组**（`CardCount(countHero:true) == 0`）⇒ 一律放行（含只有防御卡的那种「空」）
//  E 验收③：**中立阵营卡** ⇒ 不挡（有督军 / 没督军两种状态各一条）
//  F 验收④：同阵营 ⇒ 放行（不回归）+ ★★ **无督军但卡组非空** 这一格（= 本笔修的那条）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · **F3【核心判别式】**：把 `CanAddCore` 的阵营闸改回旧写法（「比督军的阵营」+「没有督军就完全放行」）
//        ⇒ 这一条从 `InvalidArmy` 变成 `None` ⇒ 红。**本笔的整条修复就靠它判别。**
//  · F4：把 `DeckArmy` 改成「只看督军」（即旧口径的另一种写法）⇒ 无督军那两格退回 `null` ⇒ F3/F4 一起红。
//  · E1/E2：把 `IsNeutralFaction` 那一支去掉（或改成只认 `""`）⇒ 中立卡被挡 ⇒ 红。
//  · D1/D2：把 `DeckCardCount(true) > 0` 那半句去掉 ⇒ 空卡组也挡 ⇒ 红。
//  · C2/C4/C5：把返回码改回 `WrongFaction`（= 上屏落到 catch-all `InvalidDeck`）⇒ 三条一起红。
//  · C3：把 `ErrorTextKey` 里的 `InvalidArmy` 从 `HasOfficialNamedKey` 拿掉 ⇒ 键退回 `InvalidDeck` ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  本件**不接线**（与 `B105_W3_Assert` 同档，等调度台统一接）。将来接的时候：
//  `CardPresentation/Editor/DeckScene.cs` 的**方法尾聚合行**（`Debug.Log(P + $"=== 合计：…`）**同一物理行**、
//  在它**第一个 `//` 之前**追加（行数零变化）：
//      B105W4_Assert.Run(); <宿主那两个计数变量> += B105W4_Assert.Pass + B105W4_Assert.Fail;
//  ⚠️ 追加前先看那一行**第一个 `//` 在哪** —— 2026-10-30 一天踩 6 次「代码落进注释里、`typecheck 0/0` 全绿、断言一条没跑」。
//  ⚠️ 本件**不需要** Transform / Canvas / 窗口：只建 `DeckEditorState` + 合成卡池。
//
// ============================ 判据 / 报告 ============================
//  · 原版：`decomp_full/CardDeck__CanAddCard.c:86-97`（阵营闸）· `decomp_full/CardDeck__CardCount.c`（张数定义）
//          · `decomp_full/GameStaticData__IsNeutralArmy.c`（中立只有一个值 `CardArmy.Neutral = 0`）
//          · `out_cpp2il_dev_ilspy/CardDeck.cs:105`（字段）/`:812-819`（同形 ILSpy 版）/`:1220-1240`（`CardCount`）
//  · 本件报告：`资料/完全复刻/报告/B105_W4.md`
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B105_W4`：钉**原版口径** —— `CanAddCore` 的阵营闸比的是 `deckArmy`（不是督军阵营），
/// 且「中立卡不挡」「空卡组不挡」两条都在。</summary>
public static class B105W4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B105W4] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ================================================================== 夹具
    //  🔶 自己设计（非原版）：**合成卡池**（自造阵营名 + 自造 id）。原版没有这两个阵营，这里只是把
    //     「同阵营 / 异阵营 / 中立」三档分开来测；卡型/稀有度取值照真实池子（`common` 的 `CopyLimit` = 2，
    //     所以每个 id 只用一次）· 调度台 2026-10-31 裁定（本件夹具，不进产品数据）。
    const string FactionA = "B105W4Alpha";
    const string FactionB = "B105W4Beta";
    const string NeutralName = "Neutral";          // 工程既有约定（同 `TargetCriteriaEval.cs:1156`）

    const string HeroA = "B105W4HeroA";
    const string HeroB = "B105W4HeroB";
    const string AlphaU1 = "B105W4A1";
    const string AlphaU2 = "B105W4A2";
    const string BetaU1 = "B105W4B1";
    const string NeutralU1 = "B105W4N1";
    const string DefA = "B105W4DefA";

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
            Hero(HeroA, FactionA), Hero(HeroB, FactionB),
            Unit(AlphaU1, FactionA), Unit(AlphaU2, FactionA),
            Unit(BetaU1, FactionB), Unit(NeutralU1, NeutralName),
            Defence(DefA, FactionA),
        };
        return new DeckEditorState(pool);
    }

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B105_W4：`CanAddCore` 的阵营闸 = 原版 `deckArmy`（中立不挡 · 空卡组不挡）");
        A_Fixture();
        B_DeckArmyDerivation();
        C_CrossFactionBlocked();
        D_EmptyDeckAllowsEverything();
        E_NeutralCardNeverBlocked();
        F_SameFactionAllows();
    }

    // ==================================================================
    //  A：夹具与判据前提（不成立时下面全不值得看）
    // ==================================================================
    static void A_Fixture()
    {
        var s = Fresh();
        var hA = s.Find(HeroA);
        var uA = s.Find(AlphaU1);
        var uB = s.Find(BetaU1);
        var uN = s.Find(NeutralU1);
        Tr(hA != null && hA.Type == "hero", "A1（前提）夹具能解析 `" + HeroA + "`，卡型 = hero");
        Tr(uA != null && uA.Type == "unit", "A2（前提）夹具能解析 `" + AlphaU1 + "`，卡型 = unit");
        Tr(uB != null && uB.Type == "unit", "A3（前提）夹具能解析 `" + BetaU1 + "`，卡型 = unit");
        Tr(uN != null && uN.Type == "unit", "A4（前提）夹具能解析 `" + NeutralU1 + "`，卡型 = unit");
        Tr(uA != null && uB != null && !DeckRules.SameFaction(uA.Faction, uB.Faction),
           "A5（前提）Alpha / Beta 两档阵营**确实不同**（否则整套「异阵营」断言等于什么都没测）");

        // 中立判据本身（原版 = `GameStaticData.IsNeutralArmy`：只有一个值 `CardArmy.Neutral = 0`）
        Tr(DeckRules.IsNeutralFaction(NeutralName), "A6 ★ `IsNeutralFaction(\"Neutral\")` = 真（= 原版 `CardArmy.Neutral`）");
        Tr(!DeckRules.IsNeutralFaction(FactionA), "A7 ★ 真阵营**不是**中立");
        Tr(!DeckRules.IsNeutralFaction(null), "A8 ★ `null` **不算**中立（原版 `CardArmy` 表达不出「没有阵营」⇒ 不给 `\"\"` 发明语义）");
        Tr(!DeckRules.IsNeutralFaction(""), "A9 ★ 空串**不算**中立（同上；口径写在 `DeckRules.IsNeutralFaction` 的 doc）");
        // 🔴 2026-10-31（调度台 · 首跑红诊断）：原来写成 `Tr(!…)`，与自己的文案（「大小写不敏感 ⇒ 也算」）相反。
        //   `IsNeutralFaction` 走 `SameFaction`（`OrdinalIgnoreCase`）⇒ 本行应为真。改布尔，不改实现。
        Tr(DeckRules.IsNeutralFaction("neutral"), "A10 `IsNeutralFaction` 大小写不敏感 ⇒ `\"neutral\"` 也算（走 `SameFaction`）");
    }

    // ==================================================================
    //  B：`deckArmy` 的派生 —— 督军 > 卡表第 0 张 > Neutral
    //      （原版写点：`DeckBasicSetup` / `AddHero` / `AddCard` 的 `count==0` 那一支 / `RemoveCard` 归 Neutral）
    // ==================================================================
    static void B_DeckArmyDerivation()
    {
        var s = Fresh();
        System.Func<string, CardDef> lk = s.Find;

        C<string>(DeckRules.DeckArmy(s.Deck, lk), null,
          "B1 ★ 空卡组（没督军、没卡）⇒ `deckArmy` = **Neutral**（`null`）= 原版 `DeckBasicSetup` 的 `pDeckHero == null` 那一支");

        s.Deck.WarlordId = HeroA;
        C(DeckRules.DeckArmy(s.Deck, lk), FactionA,
          "B2 ★ 有督军 ⇒ 督军阵营（原版 `AddHero` / `TryAddHero` 都写 `deckArmy = hero.cardArmy`）");

        s.Deck.CardIds.Add(AlphaU1);
        C(DeckRules.DeckArmy(s.Deck, lk), FactionA, "B3 有督军 + 有卡 ⇒ 仍是督军阵营（卡表不参与）");

        s.Deck.WarlordId = null;
        C(DeckRules.DeckArmy(s.Deck, lk), FactionA,
          "B4 ★★ 没督军但卡表非空 ⇒ = **卡表第 0 张**的阵营（原版 `AddCard` 的 `CardCount(countHero:true)==0` 那一支）"
        + " —— 这就是旧写法漏掉的那一格（旧写法这里给 null ⇒ 后面 F3 会放行）");

        s.Deck.CardIds.Insert(0, NeutralU1);
        C(DeckRules.DeckArmy(s.Deck, lk), FactionA,
          "B5 ★ 中立卡排在第 0 位 ⇒ **不会**把 `deckArmy` 定成中立（原版中立 = 0 = 「没定过」同值 ⇒ 跳过继续找）");

        C<string>(DeckRules.DeckArmy(null, lk), null, "B6 `deck == null` ⇒ `null`（不抛、不猜）");
        C<string>(DeckRules.DeckArmy(s.Deck, null), null, "B7 `lookup == null` ⇒ `null`（查不了卡就如实说 Neutral，⛔ 不猜）");
    }

    // ==================================================================
    //  C 验收①：有督军 + 卡组非空 ⇒ 加异阵营卡**被挡**，码/文案 = 原版那一档
    // ==================================================================
    static void C_CrossFactionBlocked()
    {
        var s = Fresh();
        string why;

        // ⓐ 只有督军（`CardCount(countHero:true) == 1` > 0）就该挡
        Tr(s.TryAdd(s.Find(HeroA), out why), "C0（前提）先放一位督军进去 —— " + why);
        C(s.DeckCount, 0, "C0b（前提）普通卡位还是 0 张（卡组非空是靠督军那张）");
        C(s.DeckCardCount(true), 1, "C0c（前提）`CardCount(countHero:true)` = 1 —— 原版 `CardCount` 把督军算进去");

        C(s.CanAdd(s.Find(BetaU1)), DeckError.InvalidArmy,
          "C1 ★★【核心判别式】**异阵营卡 ⇒ `InvalidArmy`**（原版 `CardDeck__CanAddCard.c:91-93` 那一支）"
        + " ｜🧨 把闸改回「比督军阵营 + 无督军放行」这一格仍红？不 —— **C1 钉的是码**："
        + "若返回 `WrongFaction` 或 `None` ⇒ 红");
        Tr(!s.TryAdd(s.Find(BetaU1), out why), "C2 ★ 真的加不进去（`TryAdd` 返回 false）—— 原因：" + why);

        // ⓑ 上屏那一句 = 原版具名键（不是 catch-all `InvalidDeck`）
        C(DeckRules.ErrorTextKey(DeckError.InvalidArmy), DeckRules.TermPrefix + "InvalidArmy",
          "C3 ★ `ErrorTextKey(InvalidArmy)` = **`MenuDeck/Error/InvalidArmy`** —— 原版 `CanAddCard` 直接给的是这条具名字面量"
        + "（`DAT_1842cf3f0`），**不是** `DeckUtility.ToRawLocalizationString` 那条 catch-all");
        Tr(DeckRules.HasOfficialNamedKey(DeckError.InvalidArmy),
           "C4 ★ `HasOfficialNamedKey(InvalidArmy)` = 真（官方 I2 表真有这条键）⇒ `ErrorTextKey` 不会把它摊成 `InvalidDeck`");
        Tr(DeckRules.ErrorTextKey(DeckError.InvalidArmy) != DeckRules.ErrorTextKey(DeckError.WrongFaction),
           "C5 ★ 与 `Validate` 那条 `WrongFaction`（→ `InvalidDeck`）**不是同一个键**（两处判据别混：原版这两处真的不同键）");

        string key = DeckRules.ErrorTextKey(DeckError.InvalidArmy);
        Tr(Loc.HasAnyTerm(key), "C6 ★ 词条表里**真的查得到** `" + key + "`（查不到就会把键名印到屏幕上）");
        string txt = Loc.T(key);
        Tr(!string.IsNullOrEmpty(txt) && txt != key,
           "C7 ★ 取到的是**人话**、不是键名：\"" + txt + "\"（官方 EN `Cannot add a card from other army` / ZH「不能添加其他军队的牌」）");
        C(DeckRuntime.DeckErrorText(DeckError.InvalidArmy), txt,
          "C8 ★ `TryAdd` 的 `out why` 走的就是 `DeckRuntime.DeckErrorText` —— 与上面那句逐字节相同");
    }

    // ==================================================================
    //  D 验收②：空卡组 ⇒ 一律放行（原版 `CardCount(countHero:true) > 0` 才挡）
    // ==================================================================
    static void D_EmptyDeckAllowsEverything()
    {
        var s = Fresh();
        C(s.DeckCardCount(true), 0, "D0（前提）空卡组：`CardCount(countHero:true)` = 0");
        C(s.CanAdd(s.Find(BetaU1)), DeckError.None,
          "D1 ★★ **空卡组 ⇒ 异阵营卡也放行**（原版 `CardCount(countHero:true) > 0` 才挡）"
        + " ｜🧨 把 `DeckCardCount(true) > 0` 那半句去掉 ⇒ 这里变 `InvalidArmy` ⇒ 红");

        // 防御卡**不进 `cardLibrary`** ⇒ 只带一张防御卡仍然算「空」（原版 `CardDeck__CardCount.c`）
        var s2 = Fresh();
        string why;
        Tr(s2.TryAdd(s2.Find(DefA), out why), "D2（前提）先放一张防御卡进防御卡槽 —— " + why);
        C(s2.Deck.DefensiveId, DefA, "D2b（前提）防御卡槽 = 那张");
        C(s2.DeckCount, 0, "D2c（前提）普通卡位仍是 0 张");
        C(s2.DeckCardCount(true), 0,
          "D2d ★★ 只带防御卡 ⇒ `CardCount(countHero:true)` **仍是 0**（原版 `CardCount` 只数 `deckHero` + `cardLibrary`，"
        + "防御卡住在 `+0x48` 不在 `cardLibrary` 里）");
        C(s2.CanAdd(s2.Find(BetaU1)), DeckError.None,
          "D3 ★★ 于是异阵营卡**照样放行**（「空卡组」= 按 `CardCount(countHero:true)` 算，不是「卡组里啥都没有」）");
    }

    // ==================================================================
    //  E 验收③：中立阵营卡 ⇒ **不挡**（本池 0 张中立卡 ⇒ 这条是「判据先落」的伏笔）
    // ==================================================================
    static void E_NeutralCardNeverBlocked()
    {
        var s = Fresh();
        string why;
        Tr(s.TryAdd(s.Find(HeroA), out why), "E0（前提）先放一位 Alpha 督军 —— " + why);
        C(s.DeckCardCount(true), 1, "E0b（前提）卡组非空（阵营闸此时对**非中立**卡是生效的）");
        C(s.CanAdd(s.Find(BetaU1)), DeckError.InvalidArmy, "E0c（前提）对照：异阵营**非中立**卡被挡");
        C(s.CanAdd(s.Find(NeutralU1)), DeckError.None,
          "E1 ★★ **中立阵营卡不挡**（原版 `GameStaticData.IsNeutralArmy(card.cardArmy)` 为真 ⇒ 整条阵营闸跳过）"
        + " ｜🧨 把 `IsNeutralFaction` 那一支去掉 ⇒ 这里变 `InvalidArmy` ⇒ 红");
        Tr(s.TryAdd(s.Find(NeutralU1), out why), "E1b ★ 而且真加得进去 —— " + why);

        // 没督军、卡组非空（deckArmy = 第 0 张卡的阵营 = Alpha）⇒ 中立卡同样不挡
        var s2 = Fresh();
        Tr(s2.TryAdd(s2.Find(AlphaU1), out why), "E2（前提）没督军、先加一张 Alpha 卡 —— " + why);
        C(s2.DeckCardCount(true), 1, "E2b（前提）卡组非空、`deckArmy` = Alpha");
        C(s2.CanAdd(s2.Find(BetaU1)), DeckError.InvalidArmy, "E2c（前提）对照：此时异阵营**非中立**卡被挡");
        C(s2.CanAdd(s2.Find(NeutralU1)), DeckError.None,
          "E3 ★★ 没有督军那一态里，中立卡**同样不挡**（原版那一句 `IsNeutralArmy` 在 `deckArmy` 比较**之前**、与督军无关）");
    }

    // ==================================================================
    //  F 验收④：同阵营放行（不回归）+ ★★「无督军但卡组非空」这一格
    // ==================================================================
    static void F_SameFactionAllows()
    {
        var s = Fresh();
        string why;
        Tr(s.TryAdd(s.Find(HeroA), out why), "F0（前提）先放一位 Alpha 督军 —— " + why);
        C(s.CanAdd(s.Find(AlphaU1)), DeckError.None,
          "F1 ★ 同阵营（与督军）⇒ 放行（不因为本笔新增的闸而误挡）");
        C(s.CanAdd(s.Find(AlphaU2)), DeckError.None, "F2 ★ 同阵营另一张 ⇒ 也放行");

        var s2 = Fresh();
        Tr(s2.TryAdd(s2.Find(AlphaU1), out why), "F2b（前提）**没有督军**、先加一张 Alpha 卡 —— " + why);
        C<string>(s2.Deck.WarlordId, null, "F2c（前提）督军槽是空的（这一格就是旧写法漏掉的）");
        C(s2.DeckCardCount(true), 1, "F2d（前提）卡组非空 ⇒ 原版会挡异阵营");
        C(s2.CanAdd(s2.Find(AlphaU2)), DeckError.None,
          "F3 ★★【核心判别式】没督军但卡组非空，**同阵营**（与卡表第 0 张同阵营）⇒ 放行。"
        + "旧写法这里也放行（它「没督军就全放」）⇒ 单看这一条**不判别**，配对的是 F4");
        C(s2.CanAdd(s2.Find(BetaU1)), DeckError.InvalidArmy,
          "F4 ★★【核心判别式】**没督军但卡组非空，异阵营 ⇒ 必须被挡**（原版 `deckArmy` = 卡表第 0 张的阵营，"
        + "`CardCount(countHero:true) = 1 > 0`）"
        + " ｜🧨 把闸改回旧写法（「没有督军 ⇒ 完全放行」）⇒ 这里变 `None` ⇒ 红。**本笔整条修复靠这条判别。**");
    }
}
