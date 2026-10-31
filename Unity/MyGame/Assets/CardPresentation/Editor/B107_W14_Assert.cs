// B107_W14_Assert.cs — 执行代理 `B107_W14`（2026-10-31）落地的断言：**卡组/收藏 5 条新账**
//
// ============================ 覆盖 ============================
//  · `RunStatic()`         —— 账 `N83`（两份卡池视口常量收口到原版派生值 1589.78 / 924.06）
//                             ＋ 账 `N84`（异画页出厂筛选 `Owned = false`，另三页仍为 `DeckFilter.None`）。
//                             无实例依赖，任何宿主都能跑。
//  · `RunDeckScene(rt)`    —— 账 `N83` 的**活体**（真 `DeckRuntime.Pool` 的列数/居中量）
//                             ＋ 账 `N120`（`DeckRuntime.ArmyFaction()` 实例读口 = 原版 `deckArmy`）。
//  · `RunCollection(win)`  —— 账 `N84` 的**关窗重开回初值**那一跳（`ResetPageFiltersToFactory`）。
//  · `RunDraftSlider(win)` —— 账 `N136`：轮抽费用抽屉 `Slider/Fill` 的**填充比例几何**（宽 = clamp(张数/10) × 槽宽）。
//
// ============================ 判据（⛔ 期望值一律**不**从被断属性读回来）============================
//  · **N83**：原版 prefab 是两个**不同的** prefab（`Deck Editing Menu` / `Collection Menu Variant`），
//    两边 `Content Area` 与 `Scroll View` 都是**拉伸锚点** ⇒ 序列化字段是 `m_SizeDelta`（负数内缩量），
//    **不是 1589.x**。真宽 = 1920 − ContentArea(167.17…) − ScrollView(163.05…)：
//      · 卡组编辑：`1920 − 167.17498779296875 − 163.0500030517578 = 1589.7750091552734`
//      · 收藏窗：  `1920 − 167.169921875    − 163.0500030517578 = 1589.7800750732422`
//    高：`1080 − 70.94 − 85 = 924.06` ⇒ 两处都取 **1589.78 × 924.06**（差 0.005px，四舍五入到两位一致）。
//    出处 = `d:/2/新解包资源/assets_full/bundle_menus_assets_all/RectTransform/`：
//      `RectTransform_3034975885635478229.json`（Scroll View，sizeDelta −163.05/−85）
//      · `RectTransform_675656078588768036.json`（卡组编辑 Content Area，−167.17498779296875/−70.94）
//      · `RectTransform_-2248377500096666923.json`（收藏窗 Content Area，−167.169921875/−70.94）
//    ⚠️ AUD05 记的「1589.75」**无出处**（≈1589.775 截断），别按它改回去。
//  · **N84**：`AlternateArtCardCollectionTab.showOnlyOwned` **没有字段初值**（C# 默认 false）；
//    `CardCollectionTab` / `CardbackCollectionTab` 同名字段都写 `= true`。反证 = 三个 `.ctor` 现读：
//    只有另两份写 `*(undefined1*)(param_1+0xNN) = 1`，AltArt 的 `.ctor` **不写这个偏移**。
//    出处 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AlternateArtCardCollectionTab.cs:99,179`
//    ＋ `d:/2/tools/decomp_full/{AlternateArtCardCollectionTab,CardCollectionTab,CardbackCollectionTab}__.ctor.c`。
//  · **N120**：原版 `DeckEditingWindow__FilterToDeck.c:24` 读 `editingDeck.deckArmy`（`deck+0x50`），
//    默认档 `uVar3 = 10`（`CardArmy.Ultramarines`）⇒ 消费方 `WildcardDisplay__Initialize(army)`；
//    同族 `CardCollectionFilterController__SetFiltersToDeck.c` 把 deck 发给每个 filter。
//    `deckArmy` 的派生口径 = `RuleEngine/DeckRules.DeckArmy`（督军 → 卡表第 0 张 → Neutral/null）。
//  · **N136**：`DeckEnergyCostDrawer.NUMBER_OF_CARDS_FOR_NORMALIZATION = 10`（`dump.cs` 现读）
//    ⇒ 填充宽 = `clamp(张数 / 10, 0, 1) × 槽宽`；槽宽 = prefab 现读 `212.30 − 40.90 = 171.40`。
//    `d:/2/tools/decomp_full/DeckEnergyCostDrawer__Initialize.c` 的分桶 = `cost < 8 ⇒ 第 cost 行`、否则最后一行。
//
// ============================ 灭自证 ============================
//  · N83：常量期望写**派生式**（不是从 `CardsViewport.W` 读回），实现改回 1589.7/1589.8 ⇒ 红。
//  · N84：先断言「`DeckFilter.None.Owned == true` 而 `AltArtInitialFilter.Owned == false`」两个**必须不同**；
//    再把 Styles 筛选改成 `None`、跑复位、断它变回 false —— 三条连起来才证明「按页初值」这一跳真的存在。
//  · N120：同一个 `ArmyFaction()` 在「有督军 / 无督军有卡 / 空卡组」三态各断一次，
//    并同时断「= `DeckEditorState.DeckArmyFaction()`」（两个读口同一把尺）。只断一态会被「只看督军」那种错法骗过。
//  · N136：除了逐行比填充宽，还要求**至少一行 `0 < 张数 < 10`**（否则「永远满槽」和「按比例」分不开）
//    并断言那一行**严格短于满槽**（把 `ratio` 写成 1 / 写成 0 都红）。
//
// ============================ 用法（⚠️ 接线归调度台）============================
//  · `Editor/DeckScene.cs`：`Run()` 里 `_rt` 建好之后 —— `B107_W14_Assert.RunStatic(); B107_W14_Assert.RunDeckScene(_rt);`
//  · `Editor/CollectionScene.cs`：拿到收藏窗实例之后 —— `B107_W14_Assert.RunCollection(win);`
//  · `Editor/DraftAssertD3.cs` 的 ④-bis（`win.SetDeckTab(0)` 之后、`SelectedCards` 非空时）——
//      `B107_W14_Assert.RunDraftSlider(win);`
//  三项各自累加 `Pass` / `Fail`；要并进宿主合计就 `pass += B107_W14_Assert.Pass; fail += B107_W14_Assert.Fail;`
//  `Reset()` 只在**第一项之前**调一次（同一次宿主运行里连调多项时别中途 Reset）。
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class B107_W14_Assert
{
    const string P = "[W14] ";

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Reset() { Pass = 0; Fail = 0; }

    // ------------------------------------------------------------------ 断言小工具（与 DraftAssertD3 同形）
    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + $"   ✓ {msg}"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}"); return false;
    }

    static bool Eq<T>(T got, T want, string msg)
    {
        return Ok(EqualityComparer<T>.Default.Equals(got, want), msg + $"（实得 {got} / 期望 {want}）");
    }

    static bool Near(float got, float want, float tol, string msg)
    {
        return Ok(Mathf.Abs(got - want) <= tol, msg + $"（实得 {got:F3} / 期望 {want:F3} ± {tol:F3}）");
    }

    // ==================================================================
    //  N83 + N84：纯静态那一段（无窗口 / 无实例）
    // ==================================================================
    public static void RunStatic()
    {
        // ---- N83：两份卡池视口常量收口 ----
        // 期望值 = 上面的派生式（⛔ 不写 1589.7 / 1589.8，也不从 `CardsViewport.W` 读回来）
        const float OrigWDeck = 1920f - 167.17498779296875f - 163.0500030517578f;   // 1589.775…
        const float OrigWCollection = 1920f - 167.169921875f - 163.0500030517578f;   // 1589.780…
        const float OrigH = 1080f - 70.94f - 85f;                                    // 924.06

        Near(DeckRuntime.UiPoolViewportWidthForTest, OrigWDeck, 0.01f,
             "N83 卡组编辑卡池视口宽 = **1589.78**（prefab 派生 1920 − 167.17499 − 163.05 = 1589.775；"
           + "⚠️ 序列化字段本身是 sizeDelta −163.05、不是 1589.x）");
        Near(DeckRuntime.UiPoolViewportHeightForTest, OrigH, 0.01f,
             "N83 卡组编辑卡池视口高 = **924.06**（1080 − 70.94 − 85）");

        Near(CollectionWindow.CardsViewport.W, OrigWCollection, 0.01f,
             "N83 收藏窗卡池视口宽 = **1589.78**（prefab 派生 1920 − 167.16992 − 163.05 = 1589.780）");
        Near(CollectionWindow.CardsViewport.H, OrigH, 0.01f,
             "N83 收藏窗卡池视口高 = **924.06**");
        Near(CollectionWindow.CardsPadX, (OrigWCollection - 6f * 262.5f) * 0.5f, 0.01f,
             "N83 收藏窗居中量 = (1589.78 − 6×262.5) / 2 = **7.39**（原版 `CreateCellPool` 那条居中式子）");
        Eq(CollectionWindow.CardsCols, 6,
           "N83 收藏窗桌面列数 = floor(1589.78 ÷ 262.5) = **6**（⛔ 不是写死 6；改回 1589.7 也不翻列数）");

        // ---- N84：异画页出厂筛选 `Owned = false` ----
        var none = DeckFilter.None;
        var alt = CollectionWindow.AltArtInitialFilter;
        Eq(none.Owned, true,
           "N84 前提：共用出厂态 `DeckFilter.None.Owned` 仍是 **true**（Cards / Cosmetics / Decks 三页的原版值）");
        Eq(alt.Owned, false,
           "N84 异画页出厂 `Owned` = **false**（原版 `AlternateArtCardCollectionTab.showOnlyOwned` 无字段初值）");
        Eq(alt.Name ?? "", none.Name ?? "", "N84 异画出厂态只有 `Owned` 与非空筛选不同 ⇒ `Name` 同 `None`");
        Eq(alt.Faction ?? "", none.Faction ?? "", "N84 异画出厂态 ⇒ `Faction` 同 `None`");
        Eq(alt.Rarity ?? "", none.Rarity ?? "", "N84 异画出厂态 ⇒ `Rarity` 同 `None`");
        Eq(alt.Type ?? "", none.Type ?? "", "N84 异画出厂态 ⇒ `Type` 同 `None`");
        Eq(alt.Cost, none.Cost, "N84 异画出厂态 ⇒ `Cost` 同 `None`（−1 = 不限）");
        Eq(alt.CostMax, none.CostMax, "N84 异画出厂态 ⇒ `CostMax` 同 `None`");
        Eq(alt.Upgradable, false, "N84 异画出厂态 ⇒ `Upgradable` 仍关（原版 `UpgradableCardFilter__SetupFilter` 运行期关）");
        Ok(none.Owned != alt.Owned,
           "N84 灭自证：两页的 `Owned` 初值**必须不同** —— 把 `AltArtInitialFilter` 写成 `DeckFilter.None` ⇒ 上面两条直接红");
    }

    // ==================================================================
    //  N83（活体）+ N120：需要一座真 `DeckRuntime`
    // ==================================================================
    public static void RunDeckScene(DeckRuntime rt)
    {
        if (!Ok(rt != null && rt.State != null,
                "（前提）`DeckScene` 里拿得到一座真 `DeckRuntime`（`rt.State` 非 null）—— 本节 N83 活体 / N120 都靠它"))
            return;

        // ---- N83 活体：列数 / 居中量（`Pool` 是按开关现算的，桌面档断 6 列 7.39）----
        rt.UiSetSmallScreenUIForTest(false);
        Eq(rt.UiPoolColsNow, 6,
           "N83 卡组编辑桌面列数 = floor(1589.78 ÷ 262.5) = **6**");
        Near(rt.UiPoolPadXNowForTest, (1589.78f - 6f * 262.5f) * 0.5f, 0.01f,
             "N83 卡组编辑居中量 = (1589.78 − 6×262.5) / 2 = **7.39**（小屏档 4 列时也应是同一个数）");
        rt.UiSetSmallScreenUIForTest(false);      // 收尾：还回桌面档（与 DeckScene 其余节的前提一致）

        // ---- N120：实例读口 `ArmyFaction()` = 原版 `deckArmy` ----
        var st = rt.State;
        var deck = st.Deck;
        if (!Ok(deck != null, "N120（前提）`rt.State.Deck` 非 null")) return;

        // 现场保存（本段会改这副卡组的三格，断完原样还回去）
        string saveWl = deck.WarlordId;
        var saveCards = deck.CardIds != null ? new List<string>(deck.CardIds) : new List<string>();
        try
        {
            if (deck.CardIds == null) deck.CardIds = new List<string>();

            // ① 有督军 ⇒ 督军阵营（挑一个非中立阵营的督军，避免落到 Neutral 那一档）
            CardDef hero = null;
            for (int i = 0; i < st.Pool.Count; i++)
            {
                var c = st.Pool[i];
                if (c != null && c.Type == "hero" && !string.IsNullOrEmpty(c.Faction)
                    && !DeckRules.IsNeutralFaction(c.Faction)) { hero = c; break; }
            }
            if (!Ok(hero != null,
                    "N120（前提）卡池里找得到一个**非中立阵营**的督军（否则「有督军」那一态退化成 Neutral）"))
                return;

            deck.WarlordId = hero.Id;
            deck.CardIds.Clear();
            Eq(rt.ArmyFaction(), hero.Faction,
               "N120 ① 有督军 `" + hero.Id + "` ⇒ `ArmyFaction()` = **" + hero.Faction + "**（原版 `deckArmy`）");
            Eq(rt.ArmyFaction(), st.DeckArmyFaction(),
               "N120 ① 两个读口同一把尺：`DeckRuntime.ArmyFaction()` = `DeckEditorState.DeckArmyFaction()`");

            // ② 没督军但有卡 ⇒ 卡表第 0 张的阵营（原版 `AddCard` 首张定 deckArmy）
            CardDef unit = null;
            for (int i = 0; i < st.Pool.Count; i++)
            {
                var c = st.Pool[i];
                if (c != null && c.Type != "hero" && c.Type != "defence" && !string.IsNullOrEmpty(c.Faction)
                    && !DeckRules.IsNeutralFaction(c.Faction)) { unit = c; break; }
            }
            if (!Ok(unit != null, "N120（前提）卡池里找得到一张**非中立**普通卡"))
                return;

            deck.WarlordId = null;
            deck.CardIds.Clear();
            deck.CardIds.Add(unit.Id);
            Eq(rt.ArmyFaction(), unit.Faction,
               "N120 ② 无督军但卡表第 0 张 = `" + unit.Id + "` ⇒ `ArmyFaction()` = **" + unit.Faction
             + "**（⛔ 只按督军取会得 null ⇒ 本条红）");

            // ③ 空卡组 ⇒ null（= 原版 `CardArmy.Neutral`）
            deck.WarlordId = null;
            deck.CardIds.Clear();
            Ok(rt.ArmyFaction() == null,
               "N120 ③ 空卡组 ⇒ `ArmyFaction()` = **null**（= 原版 `CardArmy.Neutral`；`DeckArmyName` 再把它映成 `Ultramarines`）");
        }
        finally
        {
            deck.WarlordId = saveWl;
            if (deck.CardIds == null) deck.CardIds = new List<string>();
            deck.CardIds.Clear();
            deck.CardIds.AddRange(saveCards);
        }
    }

    // ==================================================================
    //  N84（复位那一跳）：需要收藏窗实例
    // ==================================================================
    public static void RunCollection(CollectionWindow win)
    {
        if (!Ok(win != null, "（前提）拿得到一具 `CollectionWindow` 实例")) return;

        // 先把异画页改成「Owned 开」（= 另三页的出厂态），再走**生产那条复位**（`Close()` 里同一句）
        CollectionWindow.StylesState.Filter = DeckFilter.None;
        Eq(CollectionWindow.StylesState.Filter.Owned, true,
           "N84（前置）刚把异画页筛选手动改成 `DeckFilter.None`（Owned 开）");

        CollectionWindow.ResetPageFiltersToFactory();

        Eq(CollectionWindow.StylesState.Filter.Owned, false,
           "N84 关窗重开回初值：复位后异画页 `Owned` = **false**（`ResetPageFiltersToFactory` 必须按页给初值）"
         + "｜🧨 三页一起写 `DeckFilter.None` ⇒ 本条红");
        Eq(CollectionWindow.CardsState.Filter.Owned, true,
           "N84 复位后 Cards 页 `Owned` 仍是 **true**（原版 `CardCollectionTab.showOnlyOwned = true`）");
        Eq(CollectionWindow.CosmoState.Filter.Owned, true,
           "N84 复位后 Cosmetics（卡背）页 `Owned` 仍是 **true**（原版 `CardbackCollectionTab.showOnlyOwned = true`）");
        Eq(CollectionWindow.CardsState.Filter.Cost, -1,
           "N84 复位把**非 Owned** 的玩家筛选也清回出厂（`Cost = -1`）—— ⛔ 别只复位 Owned 一格");
    }

    // ==================================================================
    //  N136：轮抽费用抽屉 `Slider/Fill` 的填充比例几何
    // ==================================================================
    public static void RunDraftSlider(DraftModeWindow win)
    {
        if (!Ok(win != null, "N136（前提）拿得到一具 `DraftModeWindow` 实例")) return;
        int nCards = win.SelectedCards != null ? win.SelectedCards.Count : -1;
        if (!Ok(nCards > 0,
                "N136（前提）手里已有卡（本节要在「有张数」时量填充宽；`SelectedCards.Count = " + nCards + "`）"))
            return;

        win.SetDeckTab(0);                        // ⇒ `RefreshDeckPanel` → 重建 9 行
        var content = win.DeckInfoHolder != null ? win.DeckInfoHolder.Find("Content") : null;
        if (!Ok(content != null && content.childCount == 9,
                "N136（前提）费用抽屉 `Content` 有 9 行（实得 " + (content != null ? content.childCount : -1) + "）"))
            return;

        // 期望张数 = 卡表独立算（⛔ 不读实现里的 `buckets`）
        var pool = CardDatabase.Load();
        var want = new int[9];
        int noCost = 0;
        for (int i = 0; i < nCards; i++)
        {
            var d = CardDatabase.FindById(pool, win.SelectedCards[i]);
            if (d == null || d.Cost < 0) { noCost++; continue; }
            want[Mathf.Min(d.Cost, 8)]++;
        }
        int sum = 0;
        for (int i = 0; i < 9; i++) sum += want[i];
        if (!Ok(sum > 0,
                "N136（前提）至少一行张数 > 0（有费用卡 " + sum + " 张；无费用/查不到 " + noCost + " 张）"))
            return;

        int mid = -1;                             // 0 < 张数 < 10 的那一行（判别力所在）
        for (int i = 0; i < 9; i++) if (want[i] > 0 && want[i] < 10) { mid = i; break; }
        if (!Ok(mid >= 0,
                "N136（前提）至少一行 `0 < 张数 < 10` —— 否则「永远满槽」和「按比例」分不开（本节会退化成空验）"))
            return;

        // 原版字面量（⛔ 不从 `DeckCostSliderOff` 读回来）
        const float OrigSliderW = 212.30f - 40.90f;   // prefab 现读（行内 `Slider`）
        const float OrigNormalize = 10f;              // `DeckEnergyCostDrawer.NUMBER_OF_CARDS_FOR_NORMALIZATION`
        float k = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;

        float midObserved = -1f;
        for (int i = 0; i < 9; i++)
        {
            var row = content.GetChild(i);
            var slider = row != null ? row.Find("Slider") : null;
            if (!Ok(slider != null, "N136 第 " + i + " 行 `Slider` 在（节点名逐字照 prefab）")) continue;

            var srt = slider as RectTransform;
            float sliderW = srt != null ? srt.sizeDelta.x * k : -1f;
            Near(sliderW, OrigSliderW, 1.5f,
                 "N136 第 " + i + " 行 `Slider` 宽 = prefab 现读 " + OrigSliderW.ToString("F2") + "px（几何前提）");

            var fillT = slider.Find("Fill");
            float wantW = OrigSliderW * Mathf.Clamp01(want[i] / OrigNormalize);

            if (want[i] <= 0)
            {
                var fq0 = fillT != null ? fillT.GetComponent<ImageQuad>() : null;
                float w0 = 0f;
                if (MenuDraw.QuadRectPx(fq0, out float ax, out float ay, out float bx, out float by)) w0 = bx - ax;
                Ok(fillT == null || w0 <= 0.5f,
                   "N136 第 " + i + " 行 张数 0 ⇒ 填充宽 = 0（无 `Fill` 节点或宽 ≈ 0；实得 "
                 + (fillT == null ? "无节点" : w0.ToString("F2") + "px") + "）");
                continue;
            }

            if (!Ok(fillT != null, "N136 第 " + i + " 行 张数 " + want[i] + " > 0 ⇒ 应有 `Slider/Fill` 节点"))
                continue;
            var q = fillT.GetComponent<ImageQuad>();
            if (!Ok(q != null, "N136 第 " + i + " 行 `Fill` 上挂着 `ImageQuad`（量几何的前提 —— 裸节点量不到宽）"))
                continue;
            if (!MenuDraw.QuadRectPx(q, out float x1, out float y1, out float x2, out float y2))
            {
                Ok(false, "N136 第 " + i + " 行 读得到 `Fill` 的渲染矩形");
                continue;
            }

            Near(x2 - x1, wantW, 1.0f,
                 "N136 第 " + i + " 行 填充宽 = clamp(" + want[i] + " / 10) × " + OrigSliderW.ToString("F2")
               + " = **" + wantW.ToString("F2") + "px**（分母 10 = `DeckEnergyCostDrawer` 现读常量）"
               + "｜🧨 把比例写成「永远满槽 / 永远 0 / 按别的分母」⇒ 本条红");
            if (i == mid) midObserved = x2 - x1;

            // 左端贴槽左：`MenuDraw.Rect` 的桩锚点在中心 ⇒ 只改宽会**左右一起长**（原来踩过）
            var sc = MenuDraw.PosInDesignSpace(slider);
            float sliderLeft = LayoutSpace.PxX(sc.x) - sliderW * 0.5f;
            Near(x1, sliderLeft, 1.0f,
                 "N136 第 " + i + " 行 填充**左端贴槽左**（宽是绕中心长的 ⇒ 只调宽会左边跑到槽外）");
        }

        Ok(midObserved >= 0f && midObserved < OrigSliderW - 2f,
           "N136 灭自证：第 " + mid + " 行（张数 " + want[mid] + "，`0 < n < 10`）的填充宽 **严格短于满槽**"
         + "（实得 " + (midObserved < 0f ? "没量到" : midObserved.ToString("F2") + "px")
         + " / 满槽 " + OrigSliderW.ToString("F2") + "px）—— 把 `ratio` 写成 1 或 0 ⇒ 本条红");
    }
}
