// ShopFamilyAssertB93S1.cs — 商店族（`账/商店.tsv` 的 28 条 B/C）的断言（执行代理 `B93_S1`，2026-10-28）
//
// ============================ 它盯的是什么 ============================
// `账/商店.tsv` 里**批次 B93_S1** 那 28 条 B/C 中，本轮**新落地 / 新补齐**的几族：
//   A `GachaCratePanel.RefreshVisuals` / `Initialize`（账 `商店:97` / `:99` / `:100`）
//   B `CampaignPremiumPointsPanel.ChangeState` 的两件未建（底图 sprite / `backgroundShinyEffect`，账 `商店:36`）
//   C `CarouselOfferPopupDrawer.DrawItemReward` 三步（账 `商店:43`）
//   D `ShopStore.RemoveFinishedOffer()`（账 `商店:296`）
//   E `BaseOfferPopup` 的 `priceDisplayButton == null ⇒ LogError` 那道闸（账 `商店:12` / `:13`）
// 逐条的判据、做法、为什么某些格没落地 → `资料/完全复刻/报告/B93_S1.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
// `ShopFamilyAssertB93S1.Run();`
//   + 宿主里两行（照 `ShopFamilyAssertE13` 的先例）：
//     `_sink.Pass += ShopFamilyAssertB93S1.Pass; _sink.Fail += ShopFamilyAssertB93S1.Fail;`
//   · **不跑 Unity 批处理**、不建持久资产；临时结点在收尾 `DestroyImmediate`。
//   · 不依赖 `WindowsManager`（那几扇窗都只 `AddComponent` 裸组件来断言状态）。
//
// ============================ 🔴 灭自证（每条的两态/翻面法都写在文案里）============================
//   · A：把 `RefreshVisuals` 里 `RequiredOpeningsToSpecialItem == null` 那道早退删掉 ⇒ A4 红；
//        把 `>=` 改成 `>` ⇒ A7 红；把 `Initialize` 里 `openCrateHighlight.SetActive(interactable)` 写死成
//        常量 ⇒ A11/A12 两态必有一条红。
//   · B：把 `PremiumBgForState` 改成恒 Gold ⇒ B3 红；把 state 1 也切 Silver ⇒ B2/B7 红。
//   · C：把「工厂返回 false ⇒ 不画」改成先收后判 ⇒ C2/C5 红；把 null 工厂当成「画成功」⇒ C1 红。
//   · D：把 `!=` 改成 `==`（筛反）⇒ D1/D3 红；把 `null` 判据当成「全摘掉」⇒ D4 红。
//   · E：把那道闸删掉、`MenuDraw.Hit` 无条件挂 ⇒ E1 的读口就不再反映「按钮不在」这一态。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B93_S1`：商店族（28 条 B/C 里的 5 族）的断言。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。
/// <para>⛔ **期望值全是字面量**（`"WF_UI_Ranked_Background_Silver"` / 0 / 1 / 3 / true·false），
/// **不从被测实现里读** —— 否则就是「拿我们的实现证明我们的实现」。</para></summary>
public static class ShopFamilyAssertB93S1
{
    static readonly CheckSink _sink = new CheckSink("[S1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B93_S1 商店族：抽卡箱视觉 / 战役高级底图 / 轮播抽屉 / 商店摘报价 / 价签闸");

        SectionA_抽卡箱视觉();
        SectionB_战役高级底图();
        SectionC_轮播抽屉();
        SectionD_商店摘结束报价();
        SectionE_价签闸();

        Debug.Log(_sink.P + "B93_S1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A 抽卡箱（`GachaCratePanel.RefreshVisuals` / `Initialize`）
    static void SectionA_抽卡箱视觉()
    {
        MenuCheck.Section(_sink, "A `GachaCratePanel.RefreshVisuals`（高亮 / 抖动两态）+ `Initialize`（价格 / Interactable）");

        var go = new GameObject("S1_Gacha");
        var win = go.AddComponent<GachaEventWindow>();
        try
        {
            bool built = false;
            try { win.Build(); built = true; }
            catch (System.Exception ex)
            {
                Tr(false, "A1 前提：`GachaEventWindow.Build()` 跑得完（炸了：「" + ex.GetType().Name + " " + ex.Message + "」）");
            }
            if (!built) return;

            Tr(win.CrateHighlightActive,
               "A2 前提：`crateHighlight`（prefab `Highlight Crate`）**建出来且出厂开着**（`m_IsActive = 1`）");
            Tr(win.OpenCrateHighlightActive,
               "A3 前提：`openCrateHighlight`（prefab `Button Higlight border`）**建出来且出厂开着**");

            // ---- ① 没有容器 ⇒ 原版 `RefreshVisuals` 第一句就 return，出厂态一个字节都不动
            win.RefreshVisuals();
            Tr(win.CrateHighlightActive,
               "A4 ★★ `CurrentContainer == null`（`RequiredOpeningsToSpecialItem` 没给）⇒ 原版第 ① 句直接 return "
             + "⇒ 高亮**保持出厂开着**（⛔ 别在这里擅自定义「没数据就不亮」）");
            C(win.CrateShaking, false, "A5 ★ 同上一档：不抖（原版没走到 `DOShakePosition`）");

            // ---- ② 未达标 ⇒ 关（与 ① 两态对照）
            win.RequiredOpeningsToSpecialItem = 3;
            win.CratesSinceLastLegendary = 0;
            win.RefreshVisuals();
            C(win.CrateHighlightActive, false,
               "A6 ★ `crates(0) &lt; requiredOpeningsToSpecialItem(3)` ⇒ `crateHighlight.SetActive(false)`");

            // ---- ③ 刚好达标 ⇒ 开 + 该抖
            win.CratesSinceLastLegendary = 3;
            win.RefreshVisuals();
            Tr(win.CrateHighlightActive, "A7 ★★ `crates(3) &gt;= required(3)` ⇒ 高亮打开（把 `>=` 写成 `>` 这条就红）");
            Tr(win.CrateShaking, "A8 ★ 达标 ⇒ 原版起 `DOShakePosition(1,2,10,90)` 无限循环（批处理只记状态、不建补间）");

            // ---- ④ `Initialize` 的 ②③ 拍（价格 / `openCrateHighlight`）
            win.Initialize(1500, false);
            C(win.PriceText, "1500",
               "A9 ★ `Initialize` 第 ② 句 `openCrateButton.Setup(价格, 1)` ⇒ 价签印出 1500");
            C(win.OpenCrateHighlightActive, false,
               "A10 ★★ `Initialize` 第 ③ 句 `openCrateHighlight.SetActive(openCrateButton.Interactable = false)`");
            win.Initialize(null, true);
            C(win.OpenCrateHighlightActive, true,
               "A11 ★ 两态对照：`Interactable = true` ⇒ 高亮开（⛔ 别把那一句写死成常量）");
            C(win.PriceText, "1500",
               "A12 `price == null`（原版这一格没有）⇒ **不动**价签（⛔ 不清成空串、也不编一个 0）");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------ B 战役高级面板底图（`ChangeState` 的两件未建）
    static void SectionB_战役高级底图()
    {
        MenuCheck.Section(_sink, "B `CampaignPremiumPointsPanel.ChangeState` 的 `panelBackground.sprite`（Gold/Silver 两态）");

        // 判据 = `CampaignPicture.prefab` 的 `collectedSprite` = `WF_UI_Ranked_Background_Silver`、
        //        `notCollectedSprite` = `WF_UI_Ranked_Background_Gold`（guid 逐条落到 `Resources/Art/ui_menu/`）。
        C(CampaignTab.PremiumBgForState(0), "WF_UI_Ranked_Background_Gold",
           "B1 ★ state 0（`PremiumPurchased`）⇒ `notCollectedSprite` = Gold");
        C(CampaignTab.PremiumBgForState(1), "WF_UI_Ranked_Background_Gold",
           "B2 ★ state 1（`PremiumNotPurchased`）⇒ 同一张 Gold（原版 0/1 两档共用 `+0x90`）");
        C(CampaignTab.PremiumBgForState(2), "WF_UI_Ranked_Background_Silver",
           "B3 ★★ state 2（`AlreadyCollected`）⇒ `collectedSprite` = Silver —— **我们原来恒 Gold，是可见差**");

        Tr(CardArt.MenuUi("WF_UI_Ranked_Background_Silver") != null,
           "B4 前提：Silver 那张图在 `Resources/Art/ui_menu/` 里取得到（取不到 ⇒ B6 只是记账，不是画面）");
        Tr(CardArt.MenuUi("WF_UI_Ranked_Background_Gold") != null,
           "B5 前提：Gold 那张图取得到");

        var go = new GameObject("S1_Campaign");
        var tab = go.AddComponent<CampaignTab>();
        try
        {
            tab.ApplyPremiumState(2);
            C(tab.PremiumBackgroundArt, "WF_UI_Ranked_Background_Silver",
               "B6 ★★ `ApplyPremiumState(2)` ⇒ 面板底图切到 Silver（原版 `set_sprite(panelBackground, collectedSprite)`）");
            tab.ApplyPremiumState(1);
            C(tab.PremiumBackgroundArt, "WF_UI_Ranked_Background_Gold",
               "B7 ★ 两态对照：state 1 ⇒ 切回 Gold（把 state 1 也切成 Silver ⇒ 本条红）");
            C(tab.PremiumBackgroundShineEnabled == null, true,
               "B8 裸组件没有 `Background` 节点 ⇒ 扫光层建不出来 ⇒ 读口给 **null**（⛔ 不谎报 true/false）");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------ C 轮播抽屉（`DrawItemReward` 三步）
    static void SectionC_轮播抽屉()
    {
        MenuCheck.Section(_sink, "C `CarouselOfferPopupDrawer.DrawItemReward`：画不出来就不收 / 画出来才收");

        var st = new OfferContainer.Carousel.State();
        var reward = new CampaignData.RewardSpec("WildcardUltramarines2", 2, CampaignData.TierBasic);

        // ① 没有抽屉工厂（= 原版 `ItemDrawer.Draw` 不给）⇒ 第 ② 步「view == null ⇒ return」
        C(st.DrawItemReward(reward, 2, null), false,
           "C1 ★★ 没有抽屉工厂 ⇒ **返回 false**（原版第 ② 步 `if (view == null) return;`，⛔ 不假装画出来）");
        C(st.CurrentItemDrawers.Count, 0, "C2 ★ 同上一档：**一条都不收进 `currentItemDrawers`**");

        // ② 工厂被调到、但它返回 false（= `view == null`）⇒ 同样不画不收
        bool called = false;
        C(st.DrawItemReward(reward, 2, (r, q) => { called = true; return false; }), false,
           "C3 ★ 工厂返回 false ⇒ 不画（与 C6 两态对照）");
        C(called, true, "C4 前提：工厂**真的被调到了**（不是被 C1 那条早退挡掉）");
        C(st.CurrentItemDrawers.Count, 0, "C5 ★ 同上一档仍然 0 条（把「先收后判」写出来 ⇒ 本条红）");

        // ③ 画出来了 ⇒ 收进表，第 2 实参原样存
        C(st.DrawItemReward(reward, 2, (r, q) => true), true,
           "C6 ★★ 画出来了 ⇒ 返回 true（与 C1/C3 两态对照）");
        C(st.CurrentItemDrawers.Count, 1, "C7 ★ 收进 `currentItemDrawers`（原版第 ③ 步）");
        C(st.CurrentItemDrawers.Count == 1 ? st.CurrentItemDrawers[0].Quantity : -1, 2,
           "C8 ★ `quantity` 那一格原样存下来（`DrawItemReward(itemToDraw, quantity)` 的第 2 实参）");

        // ④ 真的走 `ItemDrawer.Draw` 那一档：没有父节点 ⇒ `Node == null` ⇒ 原版 `view == null` ⇒ false
        var style = ItemDrawerStyle.Default(1, 1, 1);
        C(st.DrawItemReward(null, new PxRect(0f, 0f, 100f, 100f), new ItemSpec { Id = "x" }, 1,
                            DrawerOverride.Default, style, reward), false,
           "C9 ★ 接 `ItemDrawer.Draw` 的重载：没有父节点 ⇒ 画不出来 ⇒ false（`res.Node == null`）");
        C(st.CurrentItemDrawers.Count, 1, "C10 同上一档不收（表里仍是 C7 那一条）");
    }

    // ------------------------------------------------------------ D 商店摘结束报价（`ShopStore.RemoveFinishedOffer`）
    static void SectionD_商店摘结束报价()
    {
        MenuCheck.Section(_sink, "D `ShopStore.RemoveFinishedOffer()`：摘掉 `!IsActive()` 的那条（倒序遍历）");

        var a = new ShopOffer("A", "Cosmetic", null, "1", 0, 0, 0, false, null);
        var b = new ShopOffer("B", "Cosmetic", null, "1", 0, 0, 0, false, null);
        var page = new[] { a, b };

        int removed;
        var kept = ShopData.PruneFinished(page, o => o.Name != "A", out removed);
        C(removed, 1, "D1 ★★ 原版 `RemoveFinishedOffer` 摘掉 `!IsActive()` 的那一条 ⇒ 1 条");
        C(kept != null ? kept.Length : -1, 1, "D2 ★ 剩 1 条");
        C(kept != null && kept.Length == 1 ? kept[0].Name : "<空>", "B",
           "D3 ★★ 留下的是**活着的那条**（把 `isActive` 判反 ⇒ 把 B 摘了、本条红）");

        int removed2;
        var same = ShopData.PruneFinished(page, null, out removed2);
        C(removed2, 0,
           "D4 ★ 没有活跃性判据（今天：静态表没有起止时刻）⇒ **一条都不摘**（与 D1 两态对照）");
        C(ReferenceEquals(same, page), true, "D5 同上一档：原数组原样返回（一个字节都不动）");

        C(ShopData.RemoveFinishedOffer(null), 0,
           "D6 ★ 生产入口 `RemoveFinishedOffer(null)` ⇒ 0：**没有判据就不许乱摘**（今天没有调用方，同原版）");
    }

    // ------------------------------------------------------------ E 价签闸（`BaseOfferPopup.OnEnable` 的 `priceDisplayButton == null`）
    static void SectionE_价签闸()
    {
        MenuCheck.Section(_sink, "E `BaseOfferPopup.OnEnable` 那道闸：`priceDisplayButton == null`（裸组件 = null 那一态）");

        var go = new GameObject("S1_BaseOffer");
        var pop = go.AddComponent<BaseOfferPopup>();
        try
        {
            C(pop.PriceDisplayButtonPresent, false,
               "E1 ★★ 还没建树 ⇒ 价签**不在**（= 原版 `priceDisplayButton == null` 那一支 ⇒ 我们打同一条 "
             + "`LogError(\"Offer popup missing priceDisplayButton\")` 并不挂监听）"
             + "｜🧨 把那道闸删掉、`MenuDraw.Hit` 无条件挂 ⇒ 这个读口不再反映「按钮不在」这一态");
        }
        finally { Object.DestroyImmediate(go); }
    }
}
