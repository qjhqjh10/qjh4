// B99W2_Assert.cs — 执行代理 `B99_W2`（第九十九会话 · 波 2 · 界面 B+C）的断言集
//
// ============================ 它盯的是哪四格 ============================
//   A  界面 **#3(+#7)** · 收藏切页签扫光 —— 原版 `WildcardDisplay.OnEnable` / `__OnDisable`：
//      计数条 GO 藏在 **Cards 页树**里，换页 `SetActive` ⇒ 离开 `Signal.Unregister`、回来 `Register` + `Play`
//      ⇒ **每次切回 Cards 页都重播一次**。我们原来只在**建页**那一拍 Hook+Play（`ChangeTab` 不重建页）⇒ 永不重播。
//   B  界面 **#5** · 四格万能卡数字 = 该阵营四档**库存**（原版 `WildcardDisplay.SetCountersText`）；
//      以及 `Shell/Inventory.cs` 侧**发放源播种**（13 阵营 × 4 档条目，让 `ItemDrawer` 一族看得见万能卡）。
//   C  界面 **#1** · offer 底图 —— `Artwork/background` 走 `OfferContainer.Content.BgArt`
//      （原版 `PurchaseableOfferContainer.OnInitialize` 的 `background.sprite = LoadAsset(offer, BackgroundImage=1)`），
//      `BgArt == null` 才回 prefab 那张常量兜底 `40k_shop_popup_info_bg`。
//   D  界面 **#4** · 「结束于：…」那一行 —— 由 `Editor/SkirmishWindowAssertE15.cs` 的 A7/A7b/A7c 三条盯
//      （原版取不到目标时刻 = `return;` 静默，⛔ 不印 `--`），本文件**不重复**建窗。
//
// 判据（逐句施工单）→ `资料/完全复刻/判据/B99_R3b_施工单_其余按文件.md` §2·3 / §2·5 / §2·1 / §2·4。
// 结论 / 证据 / 没查清的部分 → `资料/完全复刻/报告/B99_W2.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么接（⚠️ 本代理不改宿主）============================
// 宿主建议 = **`Editor/CollectionScene.cs`**（那里已经有建好的 `CollectionWindow`（`:862` 的 `win`）与活的
//   `WindowsManager`，且同一文件的扫光那一节（`B9_W36SHINEA`）就在附近）：
//     1) 在那一节**之后**加一句：`B99W2Assert.Run(wm, win);`
//     2) 紧挨着加计数转发：`_sink.Pass += B99W2Assert.Pass; _sink.Fail += B99W2Assert.Fail;`
//   `win` 传 null 也能跑（自己 `Create` 一台，但那样会**重跑一次 Cards 页建页 ≈50 s** ⇒ 建议传）。
//   ⛔ 不要接在 `Editor/RuleEngineTest.cs`（那是调度台独占、且在引擎侧跑，没有壳的 `WindowsManager`）。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   A5/A6：把 `WildcardDisplayTabChanged` 里的 `UnhookWildcardUpdate()` 删掉 ⇒ 切走后信号仍能推动扫光 ⇒ A6 红。
//   A7b ：不接 `WildcardDisplayTabChanged`（= 改前的行为）⇒ 切回 Cards 页时 rate **停在 1** ⇒ A7b 红。
//         A3（切到 Cards 就 Play）与 A5b（离开就不亮）**并排** ⇒ 「恒播 / 恒不播」都只能对一半。
//   B1/B2：把四格数字改回写死 `"99"` ⇒ Rare=7 / Legendary=0 两条红（99 ≠ 7、99 ≠ 0）。
//   B4   ：把 `WildcardInventorySeed` 的 id 拼成小写英文档名（`…Ultramarinesrare`）⇒ `ItemDrawer.Spec`
//         认不出 `Wildcard` ⇒ B4 红（那正是「条目在、消费者看不见」的原始缺陷）。
//   C1   ：把 `Artwork/background` 改回常量（不看 `C.BgArt`）⇒ 贴图名仍是 `40k_shop_popup_info_bg` ⇒ C1 红。
//         C2（注入 null 仍回常量）与 C1 **并排** ⇒ 「恒用注入 / 恒用常量」都只能对一半。
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B99_W2`：界面 #3(+#7) 扫光重播 · #5 四格数字与发放源播种 · #1 offer 底图。
/// <para>计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。⛔ 期望值全是**字面量 / 注入值**，
/// 不从被测实现里读回来（唯一的例外是「库存」那一格，它读 `CardProgress` —— 那是**数量正本**，不是被测的显示层）。</para></summary>
public static class B99W2Assert
{
    static readonly CheckSink _sink = new CheckSink("[W2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Check<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>唯一入口。`mgr` 空 ⇒ 自己 `EnsureHost`；`existingCollection` 空 ⇒ 自己建一台（≈50 s，见文件头）。</summary>
    public static void Run(WindowsManager mgr = null, CollectionWindow existingCollection = null)
    {
        MenuCheck.Section(_sink, "B99_W2 界面 B+C：#3(+#7) 切页签扫光 · #5 四格数字与发放源 · #1 offer 底图");

        string keepPath = DeckStore.OverridePath;
        string tmp = "d:/4/_tmp_view/b99w2/_w2_decks.json";
        CollectionWindow win = existingCollection;
        bool ownWin = false;
        BaseOfferPopup pop = null;
        WindowTabType entryTab = win != null ? win.CurrentTab : WindowTabType.CollectionDecks;
        int rareWas = CardProgress.Wildcards("Ultramarines", "rare");
        int legWas = CardProgress.Wildcards("Ultramarines", "legendary");

        try
        {
            System.IO.Directory.CreateDirectory("d:/4/_tmp_view/b99w2");
            DeckStore.OverridePath = tmp;                       // ⛔ 不碰玩家真卡组（同 E15）
            try { System.IO.File.Delete(tmp); } catch { }
            var wm = mgr != null ? mgr : WindowsManager.EnsureHost();

            if (win == null)
            {
                win = CollectionWindow.Create(wm);
                wm.OpenWindow(win);
                ownWin = true;
            }
            Tr(win != null && win.CurrentState == WindowState.Open, "（前提 A0）收藏窗是开的（下面整组都按它读）");
            if (win == null) return;

            // ========================================================== A：切页签扫光（界面 #3+#7）
            win.ChangeTab(WindowTabType.CollectionCards);       // 首次切到 Cards ⇒ 建页（`BuildWildcardShine`）
            Tr(win.WildcardShineRate >= 0f,
               $"（前提 A1）Cards 页的扫光层建出来了（`WildcardShineRate` 现读 {win.WildcardShineRate:F3}；"
             + "-1 = `WildcardShineLayer.AttachNine` 返回 null ⇒ 下面几条都不许静默跳过）");
            Tr(win.WildcardSignalHookedForTest,
               "★ A2 在 Cards 页 ⇒ `WildcardInventory.Updated` **订着**（原版 `WildcardDisplay.OnEnable` 的 `Signal.Register`）");
            Near(win.WildcardShineRate, 0f, 1e-4f,
                 "★ A3 切到 Cards 页 ⇒ 扫光**从头播**（`Play(resetTime:true)` ⇒ rate = 0；原版 `OnEnable` 第二段）");
            Tr(win.WildcardShinePlayingForTest, "★ A3b …而且在播（`Rate` 单独看分不出「没播」与「播完」，两条并排）");

            win.StepWildcardShineForTest(1f);                   // 确定性推进（批处理没有帧循环）⇒ 播完
            Near(win.WildcardShineRate, 1f, 1e-4f, "（铺场 A4）推进 1 s ⇒ rate = 1（0.75 s 一次性播完）");

            win.ChangeTab(WindowTabType.CollectionDecks);       // 离开 Cards 页 = 原版 `OnDisable`
            Tr(!win.WildcardSignalHookedForTest,
               "★ A5 离开 Cards 页 ⇒ **退订**（原版 `WildcardDisplay.OnDisable` 的 `Signal.Unregister`）"
             + "｜🧨 `WildcardDisplayTabChanged` 里那句 `UnhookWildcardUpdate()` 删掉 ⇒ 红");
            Near(win.WildcardShineRate, 1f, 1e-4f, "★ A5b ……而且计数条**不亮**（`SetEnabled(false)` ⇒ rate 回 1）");
            Tr(!win.WildcardShinePlayingForTest, "★ A5c ……也没在播");

            win.StepWildcardShineForTest(0.3f);
            win.RaiseWildcardSignalForTest();                   // 库存信号响一次（生产入口 = `Wildcards.UpdateItemCollection`）
            Tr(!win.WildcardShinePlayingForTest,
               "★ A6 **退订真的生效**：切走后库存信号响了，扫光也**不动**（没被 `Play` 推起来）"
             + "｜🧨 只 `SetEnabled(false)` 不 `Unhook` ⇒ 信号仍把它推起来 ⇒ 红");
            Near(win.WildcardShineRate, 1f, 1e-4f, "★ A6b ……rate 仍是 1");

            win.ChangeTab(WindowTabType.CollectionCards);       // 切回 Cards 页 = 原版 `OnEnable`
            Tr(win.WildcardSignalHookedForTest, "★ A7 切回 Cards 页 ⇒ **重新订阅**");
            Near(win.WildcardShineRate, 0f, 1e-4f,
                 "★ A7b **切回 Cards 页 ⇒ 扫光重播**（rate 回落 0；原版 `OnEnable` 的 `shinyEffect.Play(resetTime:true)`）"
               + "｜🧨 不接 `WildcardDisplayTabChanged`（= 改前：只在建页那一拍播）⇒ rate 停在 1 ⇒ 红");
            Tr(win.WildcardShinePlayingForTest, "★ A7c ……而且在播（与 A5c 并排 ⇒ 「恒播 / 恒不播」都只能对一半）");

            // ========================================================== B：四格数字（界面 #5 · 收藏侧）
            //   注入两个**非出厂值**再重建页 —— 出厂值 99 与写死的 "99" 分不开，必须用 7 / 0 才抓得住。
            CardProgress.SetWildcardsForTest(CardProgress.WildKey("Ultramarines", "rare"), 7);
            CardProgress.SetWildcardsForTest(CardProgress.WildKey("Ultramarines", "legendary"), 0);
            win.RebuildCardsPageWithArmyForTest(ArmyIcons.Id("Ultramarines"));   // 重建页 ⇒ `RefreshWildcardCounters` 重读
            Check(win.WildcardCounterTextForTest(0), "99",
                  "★ B1 `Counters/Common` = **99**（这一档没注入过 ⇒ 出厂库存 `WildcardsPerRarity`）"
                + "｜🧨 读成别的桶 / 读成 0 ⇒ 红");
            Check(win.WildcardCounterTextForTest(1), "7",
                  "★ B2 `Counters/Rare` = **7**（注入的 `(Ultramarines, rare) = 7`；原版 `SetCountersText` = 库存直出）"
                + "｜🧨 写死 \"99\"（改前的行为）⇒ 红");
            Check(win.WildcardCounterTextForTest(2), "99", "★ B2b `Counters/Epic` = 99（未注入那一档照旧）");
            Check(win.WildcardCounterTextForTest(3), "0",
                  "★ B3 `Counters/Legendary` = **0** —— **0 也要显示**（同阵营资源那条口径），⛔ 不隐藏、⛔ 不退回 99"
                + "｜🧨 写死 \"99\" ⇒ 红");

            // 换阵营 ⇒ 数字跟着换（原版 `WildcardDisplay.Initialize(army)` 里那句 `SetCountersText`）
            CardProgress.SetWildcardsForTest(CardProgress.WildKey("Goff", "common"), 5);
            win.SetWildcardArmyForTest(ArmyIcons.Id("Goff"), "B99_W2 断言：换阵营后四格跟着换");
            Check(win.WildcardCounterTextForTest(0), "5",
                  "★ B3b 换成 `Goff` ⇒ `Counters/Common` = **5**（该阵营那一桶）"
                + "｜🧨 `SetWildcardArmy` 不刷数字（只换徽记）⇒ 仍读 99 ⇒ 红");
            Check(win.WildcardCounterTextForTest(1), "99",
                  "★ B3c …而 `Counters/Rare` 回到 99（Goff 那一桶没注入；Ultramarines 的 7 不该串过来）");

            // ========================================================== B4：发放源播种（`Shell/Inventory.cs` 侧）
            CardProgress.SetWildcardsForTest(CardProgress.WildKey("Ultramarines", "rare"), 7);
            Inventories.Wildcards.ClearForTest();               // 清空（不落盘）⇒ 下面从零播一遍
            WildcardInventorySeed.SeedAllArmies();
            Check(Inventories.Wildcards.Count, 52,
                  "★ B4 `WildcardInventory` 播了 **52 条**（13 阵营 × 4 档；改前是**空表** ⇒ `ItemDrawer` 一族看不见万能卡）");
            WildcardInventorySeed.SeedAllArmies();
            Check(Inventories.Wildcards.Count, 52, "★ B4b 播种**幂等**（再播一次仍是 52 条，⛔ 不叠数量）");
            var info = Inventories.Wildcards.Get("WildcardUltramarines2");   // = Ultramarines · 第 2 档（rare）
            Tr(info != null, "（前提 B5）条目 `WildcardUltramarines2` 在（id = 原版 SO 名 `Wildcard<阵营><1..4>`）");
            Check(info != null ? info.Amount : -1, 7,
                  "★ B5 条目数量 = **7**（取 `CardProgress` 那一份库存：`(Ultramarines, rare) = 7`；"
                + "⛔ 别在背包里再存第二份计数）");
            var spec = ItemDrawer.Spec("WildcardUltramarines2", null, null);
            Check((int)spec.Kind, (int)ItemKind.Wildcard,
                  "★ B6 `ItemDrawer.Spec(\"WildcardUltramarines2\")` 认得出这是**万能卡**"
                + "｜🧨 id 用**小写英文档名**（`…Ultramarinesrare`）⇒ 落到 `Unknown`/占位板 ⇒ 红（那正是本条要修的缺陷）");
            Check(spec.Rarity, 2, "★ B6b ……档位 = **2**（原版 `cardRarity` 1 基）");
            Check(spec.CardArmy, 10, "★ B6c ……阵营 = **10**（Ultramarines 的 `CardArmy` 值）");

            // ========================================================== C：offer 底图（界面 #1）
            var c1 = BaseOfferPopup.DefContent();
            c1.BgArt = "WF_Special_offer_Bck_tier1";            // 原版 4 份离线 SO 的 `BackgroundImage` 都指 tier1
            pop = BaseOfferPopup.Create(wm, null, c1);
            pop.Open();
            var bgQ = pop.ArtBgNode != null ? pop.ArtBgNode.GetComponent<ImageQuad>() : null;
            Tr(bgQ != null && bgQ.Texture != null,
               "（前提 C0）`Artwork/background` 画出来了（`ImageQuad.Texture` 非 null —— 取不到图时这一格**本来就不画**）");
            Check(bgQ != null && bgQ.Texture != null ? bgQ.Texture.name : null, "WF_Special_offer_Bck_tier1",
                  "★ C1 注入 `Content.BgArt = tier1` ⇒ `Artwork/background` 的贴图就是它"
                + "（= 原版 `background.sprite = LoadAsset(offer, BackgroundImage=1)`）"
                + "｜🧨 仍读常量 `40k_shop_popup_info_bg`（改前的行为）⇒ 红");

            var c0 = BaseOfferPopup.DefContent();               // `BgArt` 留 null
            pop.Show(null, c0);
            Tr(pop.C.BgArt == null,
               "（前提 C2）当下**没有在期 Premium 报价** ⇒ `FillDef` 没往 `BgArt` 里填东西（本机 13 份活动 SO 全过期）。"
             + "⚠️ 哪天有在期报价，这条前提会红 —— 那是对的：届时按新数据重核本节两条");
            var bgQ0 = pop.ArtBgNode != null ? pop.ArtBgNode.GetComponent<ImageQuad>() : null;
            Check(bgQ0 != null && bgQ0.Texture != null ? bgQ0.Texture.name : null, "40k_shop_popup_info_bg",
                  "★ C2 `BgArt == null` ⇒ **回常量兜底** `40k_shop_popup_info_bg`（与 C1 并排 ⇒ "
                + "「恒用注入 / 恒用常量」都只能对一半）");
        }
        catch (System.Exception ex)
        {
            // 🔴 不许让异常静默跳过一整节（同「不许静默失败」那条红线）
            Tr(false, "★ B99_W2 断言自身抛异常（这一条算**红**、⛔ 不是跳过）："
                    + ex.GetType().Name + " · " + ex.Message);
        }
        finally
        {
            // ---- 收尾：拆窗 + 复原注入 + 复原存档指向 + 复原页签（⛔ 不留窗、不碰玩家真存档）
            if (pop != null) RewardsWindow.DestroySafe(pop.gameObject);
            if (ownWin && win != null) RewardsWindow.DestroySafe(win.gameObject);
            else if (win != null && entryTab != WindowTabType.None) win.ChangeTab(entryTab);
            CardProgress.SetWildcardsForTest(CardProgress.WildKey("Ultramarines", "rare"), rareWas);
            CardProgress.SetWildcardsForTest(CardProgress.WildKey("Ultramarines", "legendary"), legWas);
            CardProgress.SetWildcardsForTest(CardProgress.WildKey("Goff", "common"), 99);
            WildcardInventorySeed.SeedAllArmies();              // 把被 `ClearForTest` 清掉的条目按出厂库存播回去
            DeckStore.OverridePath = keepPath;
            try { System.IO.File.Delete(tmp); } catch { }
            Debug.Log(_sink.P + "合计：通过 " + _sink.Pass + " / 失败 " + _sink.Fail
                      + (keepPath == null ? "" : "（存档指向已还原）"));
        }
    }
}
