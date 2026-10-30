// B103R1_Assert.cs — 只读代理 `B103_R1`（2026-10-30）落地的 AUD03 剩余 ❌ 的断言
//
// ============================ 覆盖（逐条对上 `判据/B103_R1_AUD03施工单.md`）============================
//  · `Run()`（自包含、只要一个空 GameObject）：
//      #5  `DeckRuntime.Build()` 尾要把**四路滚动位 + 两个筛选抽屉逻辑位**归零（原版每次进编辑都是新实例）；
//      #14 `_cosmCells` 只补不长（两次 Build 之后条数不变）；
//      #7  `_dragSlotView` 每次刷行按 `_dragRow − firstRow` 重算（不重算 ⇒ 停在旧槽 / 恒 -1）；
//      #8  库存信号响一次 ⇒ 扫光要播（原版 `WildcardDisplay__UpdateCounters` 无条件 Play）。
//  · `RunTypeOverrides()`（纯反射）：#94 五个窗都 override 了 `OnLanguageChanged`。
//  · `RunShell(WindowsManager wm)`（要有壳的宿主，如 `ShellScene`）：
//      #120 四扇活动窗的 `Create` **不再自开**（唯一调用方 `OpenByRef` 会开）；
//      #74  `_pendingPlayMode` 通道 = 读一次就清。
//  · `RunCollection(CollectionWindow win)`（宿主已有建好的收藏窗时调；**不重建、不关窗**）：
//      #29 四个主页滚动区的 `Owner` = 各自**页节点**；#23 键表无重复键。
//  · `RunCollectionRebuild(CollectionWindow win)`：#23 的正手（`Build()` 两次，会重建整窗 —— 调度台挑安全时机调）。
//  · `RunCollectionClose(CollectionWindow win)`：#28 关窗把三份 static 筛选重置（**调用后这扇窗会被排进「待销毁」**，只在收尾调）。
//  · `RunShop(ShopWindow win)`：#38 `Open()` 之后 `Context` 已经消费掉（须**紧接一次真实开窗**调）。
//
// ============================ 口径（⛔ 别拿我们自己的常量证明我们自己的实现）============================
//  · 每条的期望值都来自**原版判据**（方法体/约定），逐条写在用例文案里；⛔ 没有一条是「读我们的实现再断我们自己」。
//  · 前提断言与结论断言**分开写**：⛔ 不用 `if (x != null) Check(...)`（前提不成立会静默跳过 —— 铁律）。
//  · 每组至少一条**灭自证**（🧨 写在文案里：改回旧写法时哪条会红）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  自包含那部分（`Run()` / `RunTypeOverrides()`）可以接进任一宿主的聚合行（同一物理行、`//` 之前）：
//      B103R1_Assert.Run(); B103R1_Assert.RunTypeOverrides(); _sink.Pass += B103R1_Assert.Pass; _sink.Fail += B103R1_Assert.Fail;
//  带窗的那几组由调度台在**对应宿主**里按上面「怎么调」的时机接。
using System;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B103_R1`：AUD03 剩余 ❌ 的断言（#5 #7 #8 #14 #23 #28 #29 #38 #74 #94 #120）。</summary>
public static class B103R1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B103R1] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口

    /// <summary>自包含那几组（#5 / #7 / #8 / #14）。⛔ 不需要壳、不建窗、不写盘。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B103R1 · AUD03 #5/#7/#8/#14（DeckRuntime 复用与生命周期）");
        try { A_DeckRuntime(); }
        catch (Exception e) { Tr(false, "「DeckRuntime」这一组抛异常（后面的条目没跑完）：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B103R1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    /// <summary>#94：五个窗都必须自己覆写 `OnLanguageChanged`（原版每颗 TMP 挂着本地化组件，换语言时由本地化系统重取）。</summary>
    public static void RunTypeOverrides()
    {
        MenuCheck.Section(_sink, "B103R1 · AUD03 #94（换语言后窗内文案要重取：五个窗各自 override `OnLanguageChanged`）");
        try { A1_LanguageOverrides(); }
        catch (Exception e) { Tr(false, "「#94 覆写」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
    }

    /// <summary>#120 / #74 —— 需要壳（`WindowsManager.EnsureHost()` 那台）。</summary>
    public static void RunShell(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "B103R1 · AUD03 #120（活动窗 Create 不自开）/ #74（`_pendingPlayMode` 读一次就清）");
        try { B1_EventWindowCreateDoesNotOpen(wm); }
        catch (Exception e) { Tr(false, "「#120 活动窗不自开」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { B2_PendingPlayModeConsumeOnce(); }
        catch (Exception e) { Tr(false, "「#74 通道语义」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
    }

    // ============================================================ #5 / #7 / #8 / #14
    static void A_DeckRuntime()
    {
        var go = new GameObject("B103R1_DeckRuntime");
        try
        {
            var rt = go.AddComponent<DeckRuntime>();
            rt.Build(new DeckLibrary());

            // ---- 前提（先立前提，再断结论：前提不成立时下面的红要指向「没建出来」，不是「修错了」）----
            Tr(rt.UiWcText(0) != null, "A0-前提①：第 1 次 `Build()` 之后计数条在（`UiWcText(0) != null`）；不在 ⇒ 下面几条全无意义");
            C(rt.ActiveTab, 0, "A0-前提②：第 1 次建完落在 Cards 页（`ActiveTab == 0`）");

            // ---- #14：两次 Build 之后 `_cosmCells` 条数不增长 ----
            int cosm1 = rt.UiCosmCellCount;
            Tr(cosm1 > 0, "A1-前提③：卡背格子表第 1 次建完非空（`UiCosmCellCount == " + cosm1 + "`）；为 0 ⇒ 下面那条是空断");

            // ---- #5：先摆出「上一趟留下的残渣」（直接写字段 —— 用 `UiScrollDeck` 那些会被内容高夹取、造不出脏态）----
            rt.UiSetTab(1);                    // 换到 Deck info 页（离开 Cards）
            rt.UiSetBuildResidueForTest(57.7f * 2f, 30f, 20f, 10f, true, true);
            Tr(rt.DeckScrollPx > 0f && rt.FiltersOpen && rt.UiCosmoFiltersOpen,
               "A2-前提④：残渣摆好了（`DeckScrollPx == " + rt.DeckScrollPx.ToString("F2") + "` · `FiltersOpen` · `UiCosmoFiltersOpen`）；"
             + "没摆上 ⇒ 下面几条复位断言是空断");

            rt.Build(new DeckLibrary());       // ← 第二次开窗（原版这一拍是**新实例**）

            C(rt.ActiveTab, 0, "A3 ★ #5 第二次 `Build()` 之后回 Cards 页（原版 `DeckEditingWindow__TryOpen:123` 把 tab0 的 toggle 置 1）");
            Tr(Mathf.Abs(rt.DeckScrollPx) < 1e-3f,
               "A4 ★ #5 卡组列表滚动位归零（`DeckScrollPx == " + rt.DeckScrollPx.ToString("F2")
             + "`）—— 🧨 删掉 `Build()` 尾那句 `_deckScroll = 0f;` ⇒ 本条红（原版每次开窗是新实例 ⇒ 恒出厂态）");
            Tr(!rt.FiltersOpen,
               "A5 ★ #5 筛选抽屉逻辑位归零（`FiltersOpen == false`）—— 🧨 删掉 `_filtersOpen = false;` ⇒ 本条红（原版新实例抽屉是收起的）");
            Tr(Mathf.Abs(rt.UiPoolScrollPx) < 1e-3f && Mathf.Abs(rt.UiFilterScrollPx) < 1e-3f && Mathf.Abs(rt.UiCosmScrollPx) < 1e-3f,
               "A6 ★ #5 另外三路滚动位也归零（卡池 " + rt.UiPoolScrollPx.ToString("F2") + " / 筛选抽屉 " + rt.UiFilterScrollPx.ToString("F2")
             + " / 卡背 " + rt.UiCosmScrollPx.ToString("F2") + "）");
            Tr(!rt.UiCosmoFiltersOpen, "A7 ★ #5 卡背抽屉逻辑位归零（`_cosmoFltOpen == false`）");

            int cosm2 = rt.UiCosmCellCount;
            C(cosm2, cosm1,
               "A8 ★ #14 两次 `Build()` 之后卡背格子表**条数不变**（第 1 次 " + cosm1 + " / 第 2 次 " + cosm2
             + "）—— 🧨 把 `while (count < N) Add` 改回 `for (i<N) Add` ⇒ 第 2 次变成两倍 ⇒ 红");

            // ---- #7：保护槽必须跟着**数据行**走（不依赖真实拖拽：只置 `_dragging/_dragRow` 两格）----
            rt.UiSetDragStateForTest(true, 2);
            rt.UiSetTab(0);                    // 走一次 RefreshDeckList（`_deckScroll` 也被 SetTab 归零 ⇒ firstRow == 0）
            C(rt.UiDragSlotViewForTest, 2,
               "A9 ★ #7 拖第 2 行时「受保护槽」== 2（= `_dragRow − firstRow`）—— 🧨 删掉 `RefreshDeckList` 里那句重算 ⇒ 它停在 -1 ⇒ 红");
            rt.UiSetDragStateForTest(true, 0);
            rt.UiSetTab(0);
            C(rt.UiDragSlotViewForTest, 0,
               "A10 ★ #7 灭自证：换成拖第 0 行 ⇒ 保护槽跟着变 0（🧨 若把它写成常量/只赋一次，A9 绿而本条红）");
            rt.UiSetDragStateForTest(false, -1);

            // ---- #8：库存信号响一次 ⇒ 扫光要播（原版 `WildcardDisplay__UpdateCounters.c` 无条件 Play）----
            rt.UiSetTab(0);                    // 保证在 Cards 页（计数条可见）
            Tr(rt.WcSignalHookedForTest, "A11-前提⑥：信号已经挂着（`WcSignalHookedForTest`）；没挂 ⇒ 下面那条空断");
            Inventories.Wildcards.UpdateItemCollection();   // = 原版 `Signal.Raise(WildcardUpdateSignal)`
            Tr(rt.WcShineRate >= 0f,
               "A12 ★ #8 库存信号响一次之后扫光真的播了（`WcShineRate == " + rt.WcShineRate.ToString("F3")
             + "`，播起来后落在 [0,1]；今天改前恒 -1）—— 🧨 把 `OnWildcardsUpdated` 改回 `if (_wcShine == null) return;` ⇒ 开窗直落 Cards 页时层还没挂 ⇒ 本条红");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);   // `DeckRuntime.OnDestroy` 会 `UnhookWildcardUpdate()`，不留静态订阅
            // ⛔ 这里**不调** `CardProgress.ResetForTest()`：那会把共享库存清零，污染宿主里后面的断言。
            //   本组只读库存（`RefreshHeader` 读 `CardProgress.Wildcards`）、只摆内存里的滚动位，不写盘。
        }
    }

    // ============================================================ #94（反射）
    static void A1_LanguageOverrides()
    {
        CheckOverride(typeof(RewardsWindow), "RewardsWindow");
        CheckOverride(typeof(InboxWindow), "InboxWindow");
        CheckOverride(typeof(DailyRewardPopup), "DailyRewardPopup");
        CheckOverride(typeof(DailyStreakPopup), "DailyStreakPopup");
        CheckOverride(typeof(RewardWindow), "RewardWindow");
        CheckOverride(typeof(ShopWindow), "ShopWindow（原有那一份）");
    }

    static void CheckOverride(Type t, string name)
    {
        var m = t.GetMethod("OnLanguageChanged", BindingFlags.Public | BindingFlags.Instance);
        bool own = m != null && m.DeclaringType == t;
        Tr(own, "B1 ★ #94 `" + name + "` 自己 override 了 `OnLanguageChanged`（🧨 删掉那个覆写 ⇒ 它退回基类空实现 ⇒ 换语言后整窗停在旧语言 ⇒ 红）");
    }

    // ============================================================ #120 / #74
    static void B1_EventWindowCreateDoesNotOpen(WindowsManager wm)
    {
        Tr(wm != null, "B0-前提①：拿到 `WindowsManager`（宿主里已有则复用同一台）");
        if (wm == null) return;

        CheckCreateDoesNotOpen(wm, "GachaEventWindow", m => (GameWindow)GachaEventWindow.Create(m));
        CheckCreateDoesNotOpen(wm, "RaidEventWindow", m => (GameWindow)RaidEventWindow.Create(m));
        CheckCreateDoesNotOpen(wm, "ExpansionPassEventWindow", m => (GameWindow)ExpansionPassEventWindow.Create(m));
        CheckCreateDoesNotOpen(wm, "ParentEventWindow", m => (GameWindow)ParentEventWindow.Create(m));
    }

    static void CheckCreateDoesNotOpen(WindowsManager wm, string name, Func<WindowsManager, GameWindow> create)
    {
        GameWindow w = null;
        try { w = create(wm); }
        catch (Exception e) { Tr(false, "B1-前提②：" + name + ".Create 抛异常：" + e.GetType().Name + "：" + e.Message); return; }
        Tr(w != null, "B1-前提②：" + name + ".Create 建出来了");
        if (w == null) return;
        Tr(!wm.openWindows.Contains(w),
           "B2 ★ #120 `" + name + "` 的 `Create` **不再自己开窗**（原版 `Create` 不自开；唯一调用方 `OpenByRef` 会在 :940 开一次）"
         + " —— 🧨 把 `mgr.OpenWindow(win)` 加回去 ⇒ 本条红（首开会被开两遍、第二次把本窗打回 Closed 再重建）");
        Tr(w.CurrentState != WindowState.Open, "B3 #120 `" + name + "` 建出来时状态不是 `Open`（没进过开窗流程）");
        UnityEngine.Object.DestroyImmediate(w.gameObject);
    }

    static void B2_PendingPlayModeConsumeOnce()
    {
        var prev = BattleDriver.PendingPlayMode;
        BattleDriver.SetPendingPlayMode(GameMode.Tutorial);
        C(BattleDriver.PendingPlayMode, (GameMode?)GameMode.Tutorial, "B4-前提③：`SetPendingPlayMode(Tutorial)` 之后通道里读得到它");
        var got = BattleDriver.TakePendingPlayMode();
        C(got, (GameMode?)GameMode.Tutorial, "B5 #74 `TakePendingPlayMode()` 取出 Tutorial");
        Tr(BattleDriver.PendingPlayMode == null,
           "B6 ★ #74 读完即清（`PendingPlayMode == null`）—— 这是「读一次就清」那条口径的基础；"
         + "B7（教程/联机两支也要消化掉）只能在本局真跑一遍 `BeginFromDeckLibrary` 时断，落点 = `Editor/BattleScene.cs`（调度台补）。");
        BattleDriver.SetPendingPlayMode(prev ?? GameMode.Classic);
        if (prev == null) BattleDriver.TakePendingPlayMode();   // 还原成「空」而不是 Classic
    }

    // ============================================================ #29 / #23 / #28
    /// <summary>宿主已建好的收藏窗（**不重建、不关窗**）。</summary>
    public static void RunCollection(CollectionWindow win)
    {
        MenuCheck.Section(_sink, "B103R1 · AUD03 #29（滚动区 Owner = 页节点）/ #23（池键表无重复键）");
        try { C1_ScrollOwners(win); }
        catch (Exception e) { Tr(false, "「#29 滚动区归属」抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { C2_CellKeyDup(win); }
        catch (Exception e) { Tr(false, "「#23 键表去重」抛异常：" + e.GetType().Name + "：" + e.Message); }
    }

    static void C1_ScrollOwners(CollectionWindow win)
    {
        Tr(win != null, "C0-前提①：收藏窗在（为 null ⇒ 下面的归属断言无意义）");
        if (win == null) return;
        for (int p = 0; p <= 3; p++)
        {
            var page = win.PageRoot(p);
            Tr(page != null, "C0-前提②：第 " + p + " 页建出来了（`PageRoot(" + p + ")`）");
            if (page == null) continue;
            var owner = win.UiScrollOwnerForTest(p);
            Tr(owner != null, "C0-前提③：第 " + p + " 页的滚动区登记着 `Owner`");
            Tr(owner == page.gameObject,
               "C1 ★ #29 第 " + p + " 页滚动区的 `Owner` == **该页节点**（不是窗根）—— 🧨 改回 `Owner = gameObject` ⇒ 本条红"
             + "（指针层靠 `Owner.activeInHierarchy` 跳过切走的页；Owner 是窗根时隐藏页仍吃滚轮）");
        }
    }

    static void C2_CellKeyDup(CollectionWindow win)
    {
        if (win == null) return;
        C(win.UiCellKeyDuplicateCountForTest, 0,
          "C2 ★ #23 卡池视图池的**键表没有重复键**（🧨 把 `if (!Contains) Add` 改回裸 `Add` 且 Build 不清池 ⇒ 重建过就有重复 ⇒ 红）");
    }

    /// <summary>#23 的正手：`Build()` 两次之后键表规模不膨胀、无重复键。⚠️ 会重建整窗，调度台挑安全时机调。</summary>
    public static void RunCollectionRebuild(CollectionWindow win)
    {
        MenuCheck.Section(_sink, "B103R1 · AUD03 #23（两次 Build 之后池键表不膨胀）");
        Tr(win != null, "C3-前提④：收藏窗在");
        if (win == null) return;
        win.Build();
        int k1 = win.UiCellKeyCountForTest;
        Tr(k1 > 0, "C3-前提⑤：第 1 次 Build 之后键表非空（" + k1 + " 条）");
        win.Build();
        int k2 = win.UiCellKeyCountForTest;
        C(k2, k1, "C4 ★ #23 第二次 Build 之后键表条数不变（" + k1 + " → " + k2
                + "）—— 🧨 删掉 `Build()` 开头那六句 `.Clear()` ⇒ 键表翻倍且复用恒 miss ⇒ 红");
        C(win.UiCellKeyDuplicateCountForTest, 0, "C5 ★ #23 第二次 Build 之后仍无重复键");
    }

    /// <summary>#28：关窗之后三份 static 筛选回到出厂态。⚠️ 调用后这扇窗会被排进「待销毁」—— 只在收尾调。</summary>
    public static void RunCollectionClose(CollectionWindow win)
    {
        MenuCheck.Section(_sink, "B103R1 · AUD03 #28（关窗把三份 static 筛选重置）");
        Tr(win != null, "C6-前提⑥：收藏窗在");
        if (win == null) return;
        var dirty = DeckFilter.None; dirty.Faction = "SpaceMarines";
        CollectionWindow.CardsState.Filter = dirty;
        Tr(CollectionWindow.CardsState.Filter.Faction == "SpaceMarines", "C6-前提⑦：先把筛选弄脏（卡牌页阵营 = SpaceMarines）");
        win.Close();
        Tr(string.IsNullOrEmpty(CollectionWindow.CardsState.Filter.Faction)
            && string.IsNullOrEmpty(CollectionWindow.CosmoState.Filter.Faction)
            && string.IsNullOrEmpty(CollectionWindow.StylesState.Filter.Faction),
           "C7 ★ #28 关窗之后三份筛选都回到「不限」（原版关窗即 Destroy ⇒ 新实例是出厂态）—— 🧨 删掉 `Close()` 里那三句重置 ⇒ 本条红");
    }

    // ============================================================ #38
    /// <summary>#38：`Open()` 之后 `OpenData` 已消费。⚠️ 必须在**一次真实开窗之后**调（`Context` 那时才应清空）。</summary>
    public static void RunShop(ShopWindow win)
    {
        MenuCheck.Section(_sink, "B103R1 · AUD03 #38（`OpenData` 一次 Open 只消费一次）");
        Tr(win != null, "D0-前提①：商店窗在（为 null ⇒ 本条无意义）");
        if (win == null) return;
        Tr(win.Context == null,
           "D1 ★ #38 开过一窗之后 `Context` 已经清掉（原版每次开窗重新喂 `data`，旧的那份不跨开窗存活；"
         + "不清 ⇒ `StartVisualIndex` 每次都被旧 army 强制落 Crates 页）—— 🧨 删掉 `Open()` 尾那句 `_openData = null;` ⇒ 本条红");
    }
}
