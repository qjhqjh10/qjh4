// B121_WC_Assert.cs — 执行代理 `B121_WC`（第一百二十一/二十二会话 · 2026-11-03）
//   · 账 `B120-E03g`：「卡池可见 + 不可加入卡组」的**界面侧**落点（`DeckError.InvalidCard` / `CanAdd` / `Validate`）
//   · `COLL-03`（行内拖动重排已关）· `COLL-04`（分享串可读头部）· `COLL-02`（卡背页小屏系数 1.2）
//   · `COLL-5①`（卡池按督军分流：**有督军/有牌 ⇒ 阵营筛选被锁成 `deckArmy`、整行隐藏**，玩家点不到也绕不过去）
//     🔴 **2026-11-04（`B141_W30` · W129 `3-02`）就地订正（铁律 5）**：原来这里写的是「玩家显式点了阵营
//     ⇒ 以筛选为准，不再恒空」—— 那条判据**已被原版早退句推翻**（`CardArmyFilter__SetFilterToDeck.c:20-25`：
//     `setted` 为真直接 `return`；`FilterToArmy.c` 末句 `SetActive(false)` 把整行关掉 ⇒ **没有切换入口**）。
//     `DeckEditorState` 的 `armyPicked` 已删，本文件的 `D2` 已**翻极性**（见 D 段）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 B2「`CanAdd` 回 `InvalidCard`」 vs B3「同一张卡**仍在 `VisibleCards()` 里**」：
//     把实现改成「从卡池隐藏」（那是另一条链、原版不这么干）⇒ B3 红；把标记丢掉 ⇒ B2 红。两边不能一起绿。
//  🧨 A5「`ErrorTextKey(InvalidCard) == ErrorTextKey(EffectOnlyCard)`（两档**同键** = `MenuDeck/Error/InvalidCard`）」：把 `EffectOnlyCard` 改回 `InvalidDeck` 兜底 ⇒ 两边键不同 ⇒ 红。
//  🧨 D1/D2 × D2b：**卡组编辑**下「没点阵营」与「点了别的阵营」**都看不到**别的阵营（锁 `deckArmy`）；
//     同一份筛选放到**收藏窗那一路**（不开 `WarlordGatedPool`）就看得见 ——
//     把 `armyPicked`（点阵营即越权）写回来 ⇒ D2 红；把 `FactionOk` 废掉 / 把锁做成全局 ⇒ D2b 红。两边不能一起绿。
//  🧨 F2「分享串**末行**是 base64 载荷」：导入端按最后一行取（`ImportDeckPopup__TryImport.c:14-18`）——
//     把 base64 挪到开头 ⇒ F2 红（而 F1「前面有可读段」仍绿）。
//
// ============================ 判据（逐条实读）============================
//  · `DeckError.InvalidCard` 的键 = 原版 `CardDeck__CanAddCard.c:40` 的 `MenuDeck/Error/InvalidCard`
//    （官方 13 语言表 EN「Trying to add an invalid card to the deck.」/ ZH「尝试向卡组添加无效的张牌。」）
//  · 卡池「显示」与「能不能进卡组」是**两条判据**（`DeckEditorCollectionDisplay__DrawCell.c:65,78` +
//    `CollectionCard__UpdateCardVisuals.c:19-20`）⇒ 显示链 = `PassesShowInCollection` / `PassesEditModeInventory`
//  · 分享串格式 = `DeckInfoPopup__MakeDeckString.c`（逐词表 → `资料/普查产出_1025/K3_收藏域复刻审查_执行报告.md` §P1-②）
//  · 卡背页小屏系数 = `_mobileSizeScale = 1.2`（`bundle_menus_assets_all` 的 `MonoBehaviour_-1923093106525475036.json`）
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不建场景、不读存档、不写 PlayerPrefs）。挂法（调度台决定）：
//  宿主聚合行**同一物理行**追加
//      B121_WC_Assert.Run(); _sink.Pass += B121_WC_Assert.Pass; _sink.Fail += B121_WC_Assert.Fail;
//  判绿红：`grep "^B121WC"` 的**合计行**；逐条按行首 `B121WC ✓` / `B121WC ✗` 数（⛔ 别 `grep -c ✗`）。

using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B121_WC`：`B120-E03g` 的界面侧 + COLL-02/03/04/5① 的验收点。</summary>
public static class B121_WC_Assert
{
    static int _pass, _fail;
    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B121WC ✓ " + msg); }
        else { _fail++; Debug.LogError("B121WC ✗ " + msg); }
    }

    // ---- 夹具 ------------------------------------------------------------
    const string FacA = "SpaceWolves", FacB = "Ultramarines";
    const string HeroA = "WC_HERO_A", HeroB = "WC_HERO_B", UnitA = "WC_UNIT_A", UnitB = "WC_UNIT_B";

    static CardDef Hero(string id, string fac) { return new CardDef(id, "WC " + id, "hero", "", "Common", fac, 0, 1, 30, 0, inventoryOptions: 0, showInCollectionOptions: 5); }
    static CardDef Unit(string id, string fac) { return new CardDef(id, "WC " + id, "unit", "", "Common", fac, 1, 1, 1, 0, inventoryOptions: 0, showInCollectionOptions: 5); }

    static DeckEditorState Fresh()
    {
        var pool = new List<CardDef>
        {
            Hero(HeroA, FacA), Hero(HeroB, FacB),
            Unit(UnitA, FacA), Unit(UnitB, FacB),
        };
        return new DeckEditorState(pool);
    }

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        A_ErrorCode();
        B_NotDeckableGate();
        C_ValidateGate();
        D_PoolSplitEdge();
        E_RowReorderOff();
        F_ShareString();
        G_CardbackScale();
    }

    // ==================================================================
    //  A：错误码本身 —— `InvalidCard` 是**原版具名键**，`EffectOnlyCard` 与它**同键**（原版那条准入闸只有一句话）
    // ==================================================================
    static void A_ErrorCode()
    {
        string key = DeckRules.ErrorTextKey(DeckError.InvalidCard);
        Check(key == DeckRules.TermPrefix + "InvalidCard",
              "A1 ★ `ErrorTextKey(InvalidCard)` = `MenuDeck/Error/InvalidCard`（原版 `CardDeck__CanAddCard.c:40` 的具名键），实读 = " + key);
        Check(DeckRules.HasOfficialNamedKey(DeckError.InvalidCard),
              "A2 ★ `HasOfficialNamedKey(InvalidCard)` = 真 ⇒ 不会被摊成 catch-all `InvalidDeck`");
        Check(Loc.HasAnyTerm(key), "A3 ★ 词条表里查得到 `" + key + "`（⛔ 查不到就会把键名印到屏幕上）");
        string txt = Loc.T(key);
        Check(!string.IsNullOrEmpty(txt) && txt != key, "A4 ★ 取到的是人话、不是键名：「" + txt + "」");
        Check(DeckRules.ErrorTextKey(DeckError.InvalidCard) == DeckRules.ErrorTextKey(DeckError.EffectOnlyCard),
              "A5 ★★ 两档**同键** = `MenuDeck/Error/InvalidCard`（原版 `CardDeck__CanAddCard.c:40-47` 的准入闸只有这一句；官方 13 语言表里 `MenuDeck/Error/EffectOnlyCard` 0 命中）"
            + " ｜🧨 把 `EffectOnlyCard` 改回 `InvalidDeck` 兜底 ⇒ 本句红");   // ✅ 已复刻（2026-11-04 · B141 · 判据 `CardDeck__CanAddCard.c:40-47`，同 W31 `DeckRules.Describe:596`）
        Check(DeckRuntime.DeckErrorText(DeckError.InvalidCard) == txt,
              "A6 `TryAdd` 的 `out why` 走 `DeckRuntime.DeckErrorText` ⇒ 与 A4 逐字节相同");
    }

    // ==================================================================
    //  B：`CanAdd` 回 `InvalidCard`，但**卡还在卡池里**（可见 + 不可加）
    // ==================================================================
    static void B_NotDeckableGate()
    {
        var s = Fresh();
        var u = s.Find(UnitA);
        Check(u != null, "B1（前提）夹具能解析 `" + UnitA + "`");
        if (u == null) return;
        Check(s.CanAdd(u) == DeckError.None, "B1b（前提）没打标之前可以加");

        u.AttachNotDeckable(true);
        Check(s.CanAdd(u) == DeckError.InvalidCard,
              "B2 ★★ 打了 `notDeckable` ⇒ `CanAdd` 回 **`InvalidCard`**（不是 `EffectOnlyCard`/`None`）");
        bool visible = false;
        var vis = s.VisibleCards();
        for (int i = 0; i < vis.Count; i++) if (vis[i] != null && vis[i].Id == UnitA) { visible = true; break; }
        Check(visible, "B3 ★★ 这张卡**仍然在 `VisibleCards()` 里**（原版对这类卡是「显示 + 变灰」，⛔ 不是从卡池隐藏）"
                     + " ｜🧨 改成隐藏 ⇒ 本句红");
        string why;
        Check(!s.TryAdd(u, out why) && !string.IsNullOrEmpty(why),
              "B4 ★ 真的加不进去，而且给出了人话原因：「" + why + "」");
    }

    // ==================================================================
    //  C：`Validate` 也认这一档（老存档里混进这类卡 ⇒ 报 `InvalidCard`，⛔ 不是笼统的 `InvalidDeck`）
    // ==================================================================
    static void C_ValidateGate()
    {
        var s = Fresh();
        var u = s.Find(UnitA);
        if (u == null) { Check(false, "C0（前提）夹具解析失败"); return; }
        s.Deck.WarlordId = HeroA;
        // 2026-11-03（B124-W1 · δ）：原来只放 1 张 ⇒ **旧闸序**里 ④ 张数档先回 `TooFewCards`，走不到 ⑥b（`IsNotDeckable`）。
        //   🔴 2026-11-04 就地订正（B141 · W38 现核）：④ 张数闸已挪到 ⑧ 之后（`DeckRules.cs:524-526`）；⑥b 仍在 ⑦ 同名上限之前 ⇒ 30 张同名不会先撞 `CopyLimitExceeded`，要测的那张 `notDeckable` 卡仍在。
        for (int i = 0; i < DeckRules.CardCount(s.Skirmish); i++) s.Deck.CardIds.Add(UnitA);
        u.AttachNotDeckable(true);
        var e = s.Validate();
        Check(e == DeckError.InvalidCard,
              "C1 ★ `Validate` 对这种卡回 **`InvalidCard`**（实读 = " + e + "）—— 与 `CanAdd` 同一档（判据只此一处 = `DeckRules.IsNotDeckable`）");
    }

    // ==================================================================
    //  D：卡池按督军分流的边角（COLL-5①）
    // ✅ 已复刻（2026-11-04 · B141 · 原版 `CardArmyFilter__SetFilterToDeck.c:16-25` + `FilterToArmy.c` 末句）
    //
    //  🔴 **2026-11-04（`B141_W30b` · W129 `3-02`）翻极性**：原版「有督军 / 有牌」那一支 =
    //     `FilterToArmy(deckArmy)`（把阵营筛选**强制**成 `deckArmy`）+ `SetActive(false)`（整行隐藏），
    //     而且 `.c:20` 那句 `if (param_1[8] != '\0') return;` 说明锁过就不再重算。
    //     ⇒ **玩家没有「点阵营就绕过督军闸」这条路**（行都是隐的）。
    //     旧口径（`B121_WC · COLL-5①`「显式点了阵营 ⇒ 以筛选为准」）已被上面那句早退句推翻，
    //     `DeckEditorState` 的 `armyPicked` 同批删除（落点 → `Deck/DeckEditorState.cs:585/595/608`）。
    // ==================================================================
    static void D_PoolSplitEdge()
    {
        var s = Fresh();
        s.WarlordGatedPool = true;
        s.Deck.WarlordId = HeroA;

        bool seesB = false;
        var vis1 = s.VisibleCards();
        for (int i = 0; i < vis1.Count; i++) if (vis1[i] != null && vis1[i].Id == UnitB) { seesB = true; break; }
        Check(!seesB, "D1（前提）有督军 A、且**没点阵营筛选** ⇒ 别的阵营那几张看不见（督军硬闸，用户 2026-09-27 口径）");

        var f = s.Filter;
        f.Factions = new HashSet<string> { FacB };
        s.Filter = f;
        bool seesB2 = false, seesA2 = false;
        var vis2 = s.VisibleCards();
        for (int i = 0; i < vis2.Count; i++)
        {
            if (vis2[i] == null) continue;
            if (vis2[i].Id == UnitB) seesB2 = true;
            if (vis2[i].Id == UnitA) seesA2 = true;
        }
        Check(!seesB2,
              "D2 ★★ **有督军 + 筛选里残留阵营 B ⇒ 别的阵营照旧看不见**（卡池被锁在 `deckArmy`）"
            + " —— 判据 `CardArmyFilter__SetFilterToDeck.c:20-25`（`setted` 早退 + `FilterToArmy(deckArmy)`）"
            + " ｜🧨 把 `armyPicked`（2026-11-03 那条「点阵营即越权」）写回来 ⇒ 本句红");
        Check(seesA2, "D2a ……而 A 阵营自己的卡**在**（锁的是阵营，不是把池子清空）");
        // 🧨 灭自证：同一份筛选换成**收藏窗那一路**（不开 `WarlordGatedPool`）就看得见 B
        //   ⇒ 「看不见」是**卡组编辑那把锁**造成的，不是 `FactionOk` 坏了 / 池子恒空。
        var c = Fresh();
        c.Deck.WarlordId = HeroA;
        var fc = c.Filter; fc.Factions = new HashSet<string> { FacB }; c.Filter = fc;
        bool seesBC = false;
        var visC = c.VisibleCards();
        for (int i = 0; i < visC.Count; i++) if (visC[i] != null && visC[i].Id == UnitB) { seesBC = true; break; }
        Check(seesBC,
              "D2b ★★ 同一份筛选放到**收藏窗那一路**（不开 `WarlordGatedPool`）⇒ B **看得见**"
            + " ｜🧨 与 D2 成对：把 `FactionOk` 废掉 / 把锁做成全局 ⇒ 本句红");
        Check(s.CanAdd(s.Find(UnitB)) == DeckError.InvalidArmy,
              "D3 ★ **看不见 ≠ 加得进**：那张卡直接加仍被 `InvalidArmy` 挡（与原版同一条路）");
    }

    // ==================================================================
    //  E：COLL-03 —— 行内拖动重排**已关**（数据层能力保留）
    // ==================================================================
    static void E_RowReorderOff()
    {
        Check(DeckRuntime.RowReorderDisabled,
              "E1 ★★ 卡组行「拖动 = 重排」的总闸是 **关**的（原版这扇窗全树没有 `DragAndDropItem`）");
        string why;
        var moved = DeckEditorState.TryMoveInList(new List<string> { "A", "B", "C", "D" }, null, null, 2, 4, out why);
        Check(moved != null, "E2 纯逻辑那半边（`TryMoveInList`）**保留**（界面接线摘掉、数据层能力不动）");
    }

    // ==================================================================
    //  F：COLL-04 —— 分享串 = 可读四段 + **末行** base64
    // ==================================================================
    static void F_ShareString()
    {
        var deck = new PlayerDeck("我的卡组", HeroA, null, new List<string> { UnitA, UnitA, UnitB }, 0);
        string s = DeckRuntime.DeckShareString(deck, "BASE64PAYLOAD");
        string[] lines = s.Split('\n');

        Check(lines.Length >= 6, "F0（前提）分享串是多行的（实读 " + lines.Length + " 行）");
        Check(lines[0] == "* 我的卡组", "F1 ★ 第 1 行 = `* <卡组名>`（原版 `\"* \" + deck.Name`），实读 = " + lines[0]);
        Check(lines[1] == "*", "F2 第 2 行 = `*`（原版分隔行）");
        Check(lines[2].StartsWith("* ") && lines[2].Contains(": "),
              "F3 ★ 第 3 行 = `* <Card_Race/Warlord>: <督军名>`，实读 = " + lines[2]);
        Check(lines[lines.Length - 1] == "BASE64PAYLOAD",
              "F4 ★★ **末行**才是 base64 载荷（导入端 `TryImport.c:14-18` 取最后一行）"
            + " ｜🧨 把载荷挪到开头 ⇒ 本句红");
        Check(lines[lines.Length - 2] == "*", "F5 载荷**前面**还有一行 `*`（原版 `\"*\\n\"` 那一段）");
        int cardLines = 0;
        for (int i = 0; i < lines.Length; i++)
            if (lines[i].StartsWith("* ") && lines[i].Contains("x ")) cardLines++;
        Check(cardLines == 2, "F6 ★ 每张**唯一**卡一行（`* Nx 卡名`）：夹具 2 张唯一卡 ⇒ " + cardLines + " 行（重复的 `"
                             + UnitA + "` 计成 `2x`，⛔ 不是两行）");
    }

    // ==================================================================
    //  G：COLL-02 —— 卡背页小屏系数 = 1.2（不是 1.5）
    // ==================================================================
    static void G_CardbackScale()
    {
        float expectW = SmallScreenUI.Enabled ? 300f : 250f;
        float expectH = SmallScreenUI.Enabled ? 486f : 405f;
        Check(Mathf.Abs(DeckRuntime.CosmoCellW - expectW) < 0.01f,
              "G1 ★ 卡背格宽 = " + DeckRuntime.CosmoCellW + "（期望 " + expectW + "：小屏 250×1.2 = 300，桌面 250）");
        Check(Mathf.Abs(DeckRuntime.CosmoCellH - expectH) < 0.01f,
              "G2 ★ 卡背格高 = " + DeckRuntime.CosmoCellH + "（期望 " + expectH + "：小屏 405×1.2 = 486，桌面 405）");
        if (SmallScreenUI.Enabled)
            Check(DeckRuntime.CosmoCols == 5, "G3 ★★ 小屏档列数 = **5**（`floor(1589.78 / 300)`；1.5 那一版是 4 ⇒ 本句为那条订正的判别式）");
        else
            Check(DeckRuntime.CosmoCols == 6, "G3 桌面档列数 = 6（`floor(1589.78 / 250)`；与本次订正无关的对照）");
    }
}
