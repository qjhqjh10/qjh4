// B111_R1WB_Assert.cs — 执行代理 `B111_R1WB`（第 111 会话第 1 波 · N124 收口）
//
// ============================ 它钉的是什么（2026-11-01）============================
//  原版 **`CardDeck.deckArmy`（字段，`+0x50`）** 的字段化：`PlayerDeck.DeckArmy` 取代「每次现推」的派生读口。
//
//  原版判据（全部现读）：
//    · 字段：`Warpforge_code/…/CardDeck.cs:26` · ILSpy `CardDeck.cs:105` · il2csharp `:27`(`// 0x50`) ·
//            `dump.cs:20479` · Ghidra `CardDeck__CanAddCard.c:91`（读 `param_1 + 0x50`）
//    · 写点：`DeckBasicSetup` `CardDeck.cs:291` · 拷贝构造 `:485` · `GetEmptyDeck` 那一支 `:610` ·
//            `AddCard` `:656/:709`（**加之前** `CardCount(hero:1)==0`）· `AddHero`/`TryAddHero` `:926/:938` ·
//            `RemoveCard` `:643`（**摘之后** `CardCount(hero:1)==0` ⇒ Neutral）· `DeserializeDeckString` `:1786`（只由督军定）
//    · 不碰：`AddDefensiveCard` / `RemoveDefensiveCard`（防御卡在 `+0x48`，不进 `cardLibrary`）
//
// ============================ 段落 ============================
//  A 前提：合成卡池（两阵营 + 中立）都能解析
//  B ★判别式 + 灭自证：`ImportString(无督军串)` ⇒ 读口 null；同一副卡表只翻 `DeckArmy` 字段 ⇒ 读口跟着翻
//  C 写点：GetEmptyDeck / AddCard(空卡组) / 之后再加不改 / 中立首卡 / AddHero / RemoveCard
//  D 拷贝点：`Clone()` 带上
//  E 旧档：缺键（null）⇒ 回落派生值（与加字段之前逐字节一致）
//
// ============================ 🔴 灭自证（怎么才能红）============================
//  · **B4 / B5【核心判别式】**：把 `DeckRules.DeckArmy` 的字段优先那一句删掉（改回纯派生）
//      ⇒ B4（`DeckArmy = "Neutral"` 那副）从 `null` 变成 `AlphaU1` 的阵营 ⇒ 红。**这是 N124 的修复判别式。**
//  · **B3**：`ImportString` 不写 `DeckArmy`（= 改前）⇒ 字段 null ⇒ 读口派生 ⇒ 给出卡表第 0 张阵营 ⇒ 红。
//  · **C7**：`ClearCards` 不写 Neutral ⇒ 读口派生 ⇒ 空卡组给 null（碰巧也对）—— ⚠️ **这一条不是判别式**，
//      它只钉「显式写」；真正的判别力在 B3/B4。如实记，不假装。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  本件**不接线**（与 `B105_W3_Assert` / `B105_W4_Assert` / `B106_W7_Assert` 同档，等调度台统一接）。建议宿主 =
//  `CardPresentation/Editor/DeckScene.cs` 的**方法尾聚合行**（`Debug.Log(P + $"=== 合计：…`）**同一物理行**、
//  在它**第一个 `//` 之前**追加（行数零变化）：
//      B111R1WB_Assert.Run(); <宿主那两个计数变量> += B111R1WB_Assert.Pass + B111R1WB_Assert.Fail;
//  ⚠️ 追加前先看那一行**第一个 `//` 在哪** —— 2026-10-30 一天踩 6 次「代码落进注释里、`typecheck 0/0` 全绿、断言一条没跑」。
//  ⚠️ 本件**不需要** Transform / Canvas / 窗口：只建 `DeckEditorState` + 合成卡池 + 纯 `PlayerDeck`。
//
// ============================ 判据 / 报告 ============================
//  · 报告：`资料/完全复刻/报告/B111_R1WB.md`
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B111_R1WB`：钉**原版 `CardDeck.deckArmy`** 的字段化（写点 / 读口 / 旧档回落）。</summary>
public static class B111R1WB_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B111R1WB] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ================================================================== 夹具
    //  🔶 自己设计（非原版）：**合成卡池**（自造阵营名 + 自造 id）—— 原版没有这两个阵营，这里只是把
    //     「同阵营 / 异阵营 / 中立」三档分开来测 · 夹具形状照 `B106_W7_Assert`（同一批）· 调度台 2026-11-01（本件夹具，不进产品数据）。
    const string FactionA = "B111WBAlpha";
    const string FactionB = "B111WBBeta";
    const string NeutralName = "Neutral";           // 工程既有约定（同 `PlayerDeck.DeckArmyNeutral`）

    const string HeroA = "B111WBHeroA";
    const string HeroB = "B111WBHeroB";
    const string AlphaU1 = "B111WBA1";
    const string AlphaU2 = "B111WBA2";
    const string NeutralU1 = "B111WBN1";
    const string DefA = "B111WBDefA";

    static CardDef Hero(string id, string faction)
    {
        return new CardDef(id, id, "hero", "", "rare", faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }
    static CardDef Unit(string id, string faction)
    {
        // 准入两字段显式取**真实原版取值**（`InInventory` / `ShowInCollection`）⇒ `CannotAddToDeck` 走正常放行分支。
        return new CardDef(id, id, "unit", "", "common", faction, 1, 1, 1, 0,
                           inventoryOptions: DeckRules.InInventory,
                           showInCollectionOptions: DeckRules.ShowInCollection);
    }
    static CardDef Defence(string id, string faction)
    {
        return new CardDef(id, id, "defence", "", "common", faction, 0, 0, 0, 0);
    }

    static List<CardDef> Pool()
    {
        return new List<CardDef>
        {
            Hero(HeroA, FactionA), Hero(HeroB, FactionB),
            Unit(AlphaU1, FactionA), Unit(AlphaU2, FactionA), Unit(NeutralU1, NeutralName),
            Defence(DefA, FactionA),
        };
    }

    static Func<string, CardDef> Lk(List<CardDef> pool)
    {
        return id =>
        {
            foreach (var c in pool) if (c != null && c.Id == id) return c;
            return null;
        };
    }

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B111_R1WB：`deckArmy` 字段化（原版 `CardDeck.deckArmy` / `+0x50`）—— 写点 · 读口 · 旧档回落");
        A_Fixture();
        B_ImportNoWarlord();
        C_Writers();
        D_Clone();
        E_OldSaveFallback();
    }

    // ==================================================================
    //  A：夹具前提
    // ==================================================================
    static void A_Fixture()
    {
        var pool = Pool();
        Tr(pool.Find(x => x.Id == HeroA) != null, "A1（前提）夹具能解析 `" + HeroA + "`");
        Tr(pool.Find(x => x.Id == AlphaU1) != null, "A2（前提）夹具能解析 `" + AlphaU1 + "`");
        var hA = pool.Find(x => x.Id == HeroA); var hB = pool.Find(x => x.Id == HeroB);
        Tr(hA != null && hB != null && !DeckRules.SameFaction(hA.Faction, hB.Faction),
           "A3（前提）两档阵营**确实不同**（否则整套「换阵营」断言等于什么都没测）");
        var nU = pool.Find(x => x.Id == NeutralU1);
        Tr(nU != null && DeckRules.SameFaction(nU.Faction, PlayerDeck.DeckArmyNeutral),
           "A4（前提）中立夹具的阵营串 == `PlayerDeck.DeckArmyNeutral`（否则 C7 那条测不到点子上）");
    }

    // ==================================================================
    //  B：★ 本笔的判别式 —— 导入一副**没有督军**的卡组串
    // ==================================================================
    static void B_ImportNoWarlord()
    {
        var pool = Pool(); var lk = Lk(pool);

        // B1/B2/B3：原版 `DeserializeDeckString` → `DeckBasicSetup`：无督军 ⇒ `deckArmy = Neutral`，
        //            **⛔ 不是**卡表第 0 张卡的阵营（今天改之前给的正是后者 —— 本笔要消掉的那一格）。
        string s = DeckLibrary.ExportString(new PlayerDeck("B111WB无督军", null, null, new[] { AlphaU1, AlphaU2 }, 0));
        var d = DeckLibrary.ImportString(s, lk);
        Tr(d != null, "B1（前提）无督军卡组串能导入");
        Tr(d != null && string.IsNullOrEmpty(d.WarlordId), "B2（前提）导入结果**确实没有督军**");
        C(d != null ? d.DeckArmy : "<null deck>", PlayerDeck.DeckArmyNeutral,
          "B3 ★ 导入无督军串 ⇒ 字段 = 原版 `CardArmy.Neutral`（把 `ImportString` 的写点删掉 ⇒ 字段变 null ⇒ 红）");
        C(DeckRules.DeckArmy(d, lk), (string)null,
          "B4 ★ 读口 ⇒ null（原版 `DeckBasicSetup`：无督军 ⇒ Neutral；⛔ 不是卡表第 0 张卡的阵营）");

        // B5/B6【灭自证】：**同一副卡表**，只翻 `DeckArmy` 字段 ⇒ 读口必须跟着翻。
        //   把 `DeckRules.DeckArmy` 的「字段优先」那一句删掉 ⇒ B6 从 null 变回 FactionA ⇒ 红。
        var g = new PlayerDeck("B111WB缺键", null, null, new[] { AlphaU1 }, 0);
        C(g.DeckArmy, (string)null, "B5（前提）手造的这副 `DeckArmy` 是 null（= 旧档缺键）");
        C(DeckRules.DeckArmy(g, lk), FactionA,
          "B6 缺键 ⇒ 回落**派生值** = 卡表第 0 张的阵营（= 加字段之前的行为，逐字节不变）");
        var h = new PlayerDeck("B111WB字段优先", null, null, new[] { AlphaU1 }, 0); h.SetDeckArmyNeutral();
        C(DeckRules.DeckArmy(h, lk), (string)null,
          "B7 ★灭自证：**同一副卡表**，只把 `DeckArmy` 置成 Neutral ⇒ 读口必须给 null"
        + "（把读口改回「纯派生」⇒ 这里变回 `" + FactionA + "` ⇒ 红）");
    }

    // ==================================================================
    //  C：写点（逐条对原版）
    // ==================================================================
    static void C_Writers()
    {
        var lk0 = Lk(Pool());

        // C1 原版 `GetEmptyDeck`（`CardDeck.cs:610`）⇒ Neutral
        {
            var s = new DeckEditorState(Pool()); s.NewDeck("B111WB新卡组");
            C(s.Deck.DeckArmy, PlayerDeck.DeckArmyNeutral, "C1 `NewDeck` ⇒ 字段 = Neutral（原版 `GetEmptyDeck`）");
        }

        // C2/C3 原版 `AddCard`（`:656-661`）：**加之前** `CardCount(hero:1)==0` ⇒ `deckArmy = card.cardArmy`
        {
            var s = new DeckEditorState(Pool()); s.NewDeck("B111WB首卡");
            Tr(s.TryAdd(s.Find(AlphaU1), out _), "C2（前提）空卡组加第一张普通卡成功");
            C(s.Deck.DeckArmy, FactionA, "C3 空卡组第一张普通卡 ⇒ 定阵营（原版 `AddCard` 的 `count==0` 那一支）");
            Tr(s.TryAdd(s.Find(AlphaU2), out _), "C4（前提）加入同阵营第二张普通卡成功");
            C(s.Deck.DeckArmy, FactionA, "C5 **之后**再加不改它（原版 `count==0` 只是那一支）");
        }

        // C6 中立首卡 ⇒ Neutral（原版 `AddCard` 也是写 `card.cardArmy` = Neutral）
        {
            var s = new DeckEditorState(Pool()); s.NewDeck("B111WB中立首卡");
            Tr(s.TryAdd(s.Find(NeutralU1), out _), "C6（前提）空卡组加第一张**中立**卡成功");
            C(s.Deck.DeckArmy, PlayerDeck.DeckArmyNeutral, "C7 中立首卡 ⇒ 字段 = Neutral（与原版 `card.cardArmy` 同值）");
        }

        // C8/C9 原版 `AddHero` / `TryAddHero`（`:926/:938`）⇒ `deckArmy = hero.cardArmy`
        {
            var s = new DeckEditorState(Pool()); s.NewDeck("B111WB加督军");
            Tr(s.TryAdd(s.Find(HeroB), out _), "C8（前提）空卡组加督军 `" + HeroB + "` 成功");
            C(s.Deck.DeckArmy, FactionB, "C9 加督军 ⇒ 字段 = 督军阵营（原版 `AddHero`）");
        }

        // C10/C11 原版 `RemoveCard`（`:643-647`）：**摘之后** `CardCount(hero:1)==0` ⇒ Neutral
        {
            var s = new DeckEditorState(Pool()); s.NewDeck("B111WB摘督军");
            Tr(s.TryAdd(s.Find(HeroA), out _), "C10（前提）加督军 `" + HeroA + "` 成功");
            Tr(s.TryAdd(s.Find(AlphaU1), out _), "C11（前提）加一张同阵营普通卡成功");
            C(s.Deck.DeckArmy, FactionA, "C12（前提）此时字段 = " + FactionA);
            Tr(s.ClearWarlord(), "C13（前提）摘掉督军成功");
            C(s.Deck.DeckArmy, FactionA,
              "C14 ★ 摘掉督军但**卡表非空** ⇒ 字段**不变**（原版 `RemoveCard` 只在 `CardCount(hero:1)==0` 时归 Neutral —— 这一格最容易写错）");
            s.ClearCards();
            C(s.Deck.DeckArmy, PlayerDeck.DeckArmyNeutral,
              "C15 清空到 `CardCount(hero:1)==0` ⇒ 字段 = Neutral");
        }

        // C16 防御卡**不碰**（原版 `AddDefensiveCard`/`RemoveDefensiveCard` 不写）
        {
            var s = new DeckEditorState(Pool()); s.NewDeck("B111WB防御卡");
            Tr(s.TryAdd(s.Find(HeroA), out _), "C16（前提）加督军成功");
            var before = s.Deck.DeckArmy;
            Tr(s.TryAdd(s.Find(DefA), out _), "C17（前提）加防御卡成功");
            C(s.Deck.DeckArmy, before, "C18 加防御卡**不改** `DeckArmy`（原版 `AddDefensiveCard` 不写它）");
        }
    }

    // ==================================================================
    //  D：拷贝点（原版拷贝构造 `CardDeck.cs:485` 照抄 `deckToCopy.deckArmy`）
    // ==================================================================
    static void D_Clone()
    {
        var pool = Pool(); var lk = Lk(pool);
        var src = new PlayerDeck("B111WB拷贝", HeroA, null, new[] { AlphaU1 }, 0);
        src.SetDeckArmyFromCard(pool.Find(x => x.Id == HeroA));
        C(src.Clone().DeckArmy, FactionA, "D1 `Clone()` 带上 `DeckArmy`（原版拷贝构造照抄 —— 漏了就会静默丢阵营）");

        var oldSrc = new PlayerDeck("B111WB拷贝旧档", null, null, new[] { AlphaU1 }, 0);   // DeckArmy == null
        C(oldSrc.Clone().DeckArmy, (string)null, "D2 旧档缺键的副本仍然是缺键（旧档语义不被拷贝破坏）");
        C(DeckRules.DeckArmy(oldSrc.Clone(), lk), FactionA, "D3 副本的读口仍回落派生 ⇒ 与原件一致");
    }

    // ==================================================================
    //  E：旧存档（缺键 ⇒ 沿用既有派生值，行为逐字节不变）
    // ==================================================================
    static void E_OldSaveFallback()
    {
        var pool = Pool(); var lk = Lk(pool);

        var old1 = new PlayerDeck("B111WB旧档-有督军", HeroA, null, new List<string>(), 0);
        C(old1.DeckArmy, (string)null, "E1（前提）旧档没有这个键 ⇒ `DeckArmy` 是 null");
        C(DeckRules.DeckArmy(old1, lk), FactionA, "E2 旧档有督军 ⇒ 派生 = 督军阵营（**与加字段之前逐字节一致**）");

        var old2 = new PlayerDeck("B111WB旧档-无督军", null, null, new[] { AlphaU1 }, 0);
        C(DeckRules.DeckArmy(old2, lk), FactionA, "E3 旧档无督军但有卡 ⇒ 派生 = 卡表第 0 张的阵营（**这是旧档的既有行为，刻意保留、不改**）");

        var old3 = new PlayerDeck("B111WB旧档-空", null, null, new List<string>(), 0);
        C(DeckRules.DeckArmy(old3, lk), (string)null, "E4 旧档空卡组 ⇒ 派生 = null（Neutral）");

        C(DeckRules.DeckArmy(null, lk), (string)null, "E5 `deck == null` ⇒ null（不抛、不猜）");
        C(DeckRules.DeckArmy(old3, null), (string)null, "E6 `lookup == null` 且缺键 ⇒ null（查不了卡就如实说 Neutral，⛔ 不猜）");
    }
}
