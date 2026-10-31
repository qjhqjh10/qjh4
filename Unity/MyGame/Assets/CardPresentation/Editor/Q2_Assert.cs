// Q2_Assert.cs — 第一百零九会话 · 执行代理 `Q2`（零散快活）
//
// ============================ 它盯的是哪两格 ============================
//  A  **卡背包变动 ⇒ 已打开的收藏窗要自己刷新**（原版 `CardCollectionUpdatedSignal` → `CardCollectionDisplay.OnCollectionUpdate`）。
//     判据 → `资料/完全复刻/判据/B109_R22_卡奖励收藏链.md` §5·5；实现 → `Shell/CollectionWindow.cs` 类尾
//     （`HookCardsInventoryUpdate` / `CardsInventoryTabChanged` / `OnCardsInventoryChanged`）。
//     原版三句（逐文件现读）：
//       · 发 = `CardInventory.UpdateItemCollection()`（`out_il2csharp/Assembly-CSharp/CardInventory.cs:17-20`）
//       · 收 = `CardCollectionDisplay.OnEnable` 的 `Signal.Register` / `OnDisable` 的 `Signal.Unregister`
//       · 回调 = 对**当前可见格**逐个 `UpdateCardVisuals`（`CardCollectionDisplay__UpdateCardVisuals.c`）
//  B  **`ItemDisplayWindow` 的「空上下文」= `LogError + return`，一个节点都不建**（原版第一句；
//     可读体 = `out_il2csharp/Assembly-CSharp/ItemDisplayWindow.cs` 的 `Open()`：`LogError("No item available")`）。
//     我们原来走 `DefContext()` 铺出 prefab 那三颗出厂文本（`报告/C2.md` §七·2 记的既有偏离）⇒ 本轮改掉。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//  · A2/A3/A5：把 `RefreshHighlights` 里那句 `CardsInventoryTabChanged(...)` 删掉 ⇒ 切页不再订/退 ⇒ A2/A3 红。
//  · A4/A6   ：把 `Close()` 里那句 `UnhookCardsInventoryUpdate()` 删掉 ⇒ 关窗后仍挂着 ⇒ A4 红（静态背包 = 泄漏）。
//  · B1      ：把 `OnCardsInventoryChanged` 里 `RebuildCardsCells` 删掉（只留计数）⇒ B1b（「`Counter 0` 是**新对象**」）红。
//  · B1b     ：订阅订到一条**不会响**的信号上（例如只订 `CardInventory.CollectionUpdated` —— 它今天 0 生产者）
//              ⇒ B1/B1b 一起红（这正是本轮要修的那条接线）。
//  · C1      ：离开 Cards 页后没退订 ⇒ 在 Deck 页加卡也会重建 ⇒ C1 红。
//  · D1      ：把 `OnCardsInventoryChanged` 第一句的 `this == null` 守卫删掉 ⇒ `DestroyImmediate` 之后背包再变
//              会抛 `MissingReferenceException` ⇒ D1 红（先例 → `项目任务.md` §三 `B103_F1`）。
//  · E1/E2   ：把 `Build()` 首句的空上下文闸删掉（= 改前的行为：铺 `Title`/`New Text`/`Type`）⇒ E1 红；
//              闸写过头（把**非空**上下文也拦掉）⇒ E2 红。两条并排 ⇒ 「恒建 / 恒不建」都只能对一半。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  自包含（自己建壳 + 自己建窗；A 段会**建一次 Cards 页**，批处理实测那次 ≈50 s —— 与 `B99_W2` 同量级）：
//      任一宿主的聚合行、`//` **之前**同一物理行追加：
//      Q2_Assert.Run(); pass += Q2_Assert.Pass; fail += Q2_Assert.Fail;
//  已有一个活的 `CollectionWindow` 时建议传进来（**省掉那次建页**）：
//      Q2_Assert.Run(wm, win);
//
// ============================ 副作用 / 恢复 ============================
//  · A 段会临时把一张卡的数量 +1 再 +1，**finally 里用 `SetAmount` 还原**（`SetAmount` 不发事件 ⇒ 不会反过来推动重建）。
//  · A 段用 `customData["CardTier"] = 现值-1` 发卡 ⇒ `CardProgress.SetLevel` 算出来 = 现值（**不动玩家等级**；同 `C9_Assert` B 组）。
//  · D 段只在「自己建窗」时跑（它会 `DestroyImmediate` 那扇窗）—— 传外部窗时不销毁别人的窗。
//  · B 段自建的两扇 `ItemDisplayWindow` 一律 `DestroyImmediate` 拆掉（`Create(null, …)` 不进窗口管理器）。
using System;
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `Q2`：① 卡背包变动 ⇒ 收藏窗自动刷新；② `ItemDisplayWindow` 的空上下文分支。</summary>
public static class Q2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[Q2] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>唯一入口。`mgr` 空 ⇒ 自己 `EnsureHost`；`existing` 空 ⇒ 自己建一台收藏窗（A 段会多花一次建页时间）。</summary>
    public static void Run(WindowsManager mgr = null, CollectionWindow existing = null)
    {
        MenuCheck.Section(_sink, "Q2 · ① 卡背包变动 ⇒ 收藏窗刷新（原版 `CardCollectionUpdatedSignal`）· ② 物品窗空上下文 = LogError + return");

        try { SectionA_CollectionRefresh(mgr, existing); }
        catch (Exception e) { Tr(false, "「A 收藏窗刷新」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }

        try { SectionB_ItemDisplayEmptyContext(); }
        catch (Exception e) { Tr(false, "「B 物品窗空上下文」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }

        Debug.Log(_sink.P + "Q2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 卡背包变动 ⇒ Cards 页重建
    static void SectionA_CollectionRefresh(WindowsManager mgr, CollectionWindow existing)
    {
        MenuCheck.Section(_sink, "A 卡背包变动 ⇒ Cards 页刷新（= 原版 `CardCollectionDisplay.OnCollectionUpdate`）");

        Tr(Inventories.Cards != null, "A0 **前提**：`Inventories.Cards` 在（卡的数量正本；见 `Shell/Inventory.cs:529`）");
        if (Inventories.Cards == null) return;

        CollectionWindow win = existing;
        bool ownWin = false;
        WindowTabType entryTab = win != null ? win.CurrentTab : WindowTabType.None;
        string cardId = null;
        int amountBefore = -1;

        try
        {
            var wm = mgr != null ? mgr : WindowsManager.EnsureHost();
            if (win == null)
            {
                win = CollectionWindow.Create(wm);
                wm.OpenWindow(win);
                ownWin = true;
            }
            if (win != null && win.CurrentState != WindowState.Open) win.TryOpen();   // 宿主可能把它压成 `Background`（同 `B99_W2` A0 的写法）
            Tr(win != null && win.CurrentState == WindowState.Open, "A0b **前提**：收藏窗是开的（下面整组都按它读）");
            if (win == null) return;

            // ---- A1：切到 Cards 页 = 原版 `CardCollectionDisplay.OnEnable`（`Signal.Register`）----
            win.ChangeTab(WindowTabType.CollectionCards);
            Tr(win.CardsCollectionSignalHookedForTest,
               "★ A1 在 Cards 页 ⇒ 卡背包那条信号**订着**（原版 `CardCollectionDisplay.OnEnable` 的 `Signal.Register`）"
             + "｜🧨 `RefreshHighlights` 里那句 `CardsInventoryTabChanged(...)` 删掉 ⇒ 红");

            // ---- A2：离开 Cards 页 = 原版 `OnDisable`（`Signal.Unregister`）----
            win.ChangeTab(WindowTabType.CollectionDecks);
            Tr(!win.CardsCollectionSignalHookedForTest,
               "★ A2 离开 Cards 页 ⇒ **退订**（原版 `CardCollectionDisplay.OnDisable` 的 `Signal.Unregister`）"
             + "｜🧨 只 `SetActive(false)` 不退订 ⇒ 红（那样在别的页加卡也会重建这一页）");

            // ---- A3：切回 ⇒ 重新订阅 ----
            win.ChangeTab(WindowTabType.CollectionCards);
            Tr(win.CardsCollectionSignalHookedForTest, "★ A3 切回 Cards 页 ⇒ **重新订阅**");

            // ---- B：事件真的把这一页推着重建成一遍 ----
            var vis = CollectionWindow.CardsState.VisibleCards();
            Tr(vis != null && vis.Count > 0, "B0 **前提**：Cards 页过滤后至少有一张卡（`CardsState.VisibleCards()`）");
            if (vis == null || vis.Count == 0) return;
            var def = vis[0];
            Tr(def != null && !string.IsNullOrEmpty(def.Id), "B0b **前提**：第一张卡有 id");
            if (def == null || string.IsNullOrEmpty(def.Id)) return;
            cardId = def.Id;

            Transform counter0Before = CounterNode(win, 0);
            bool hadCounter0 = counter0Before != null;   // ⚠️ 必须**在重建之前**取值：重建之后旧节点是 Unity 假 null（`== null` 恒真）
            Tr(hadCounter0,
               "B0c **前提**：第一格的「张数」节点 `Counter 0` 建出来了（`CardsCounterSprite` 在工程里、且首行在视口内）"
             + " —— 它是下面「视图真的重建了」那条的读数口");
            int counter0IdBefore = hadCounter0 ? counter0Before.GetInstanceID() : 0;
            int refreshBefore = win.CardsCollectionRefreshCountForTest;
            var info = Inventories.Cards.Get(def);
            amountBefore = info != null ? info.Amount : 0;

            AddOneCard(def);

            C(win.CardsCollectionRefreshCountForTest, refreshBefore + 1,
              "★ B1 背包里这张卡 +1 ⇒ 收藏窗**收到信号并重建了一次**（" + refreshBefore + " → "
              + win.CardsCollectionRefreshCountForTest + "）"
            + "｜🧨 订到不会响的信号上（例如只订 `CardInventory.CollectionUpdated` —— 它今天 0 生产者）⇒ 红");
            Transform counter0After = CounterNode(win, 0);
            Tr(counter0After != null, "B1a **前提**：重建之后 `Counter 0` 还在（否则下一条无从比）");
            // ⚠️ 用 `ReferenceEquals` 而不是 `!= null`：旧节点此刻已被销毁（Unity 假 null），
            //    但 C# 引用还在 —— 它俩是不是同一个对象**不该经过 Unity 的 `==` 重载**。
            Tr(hadCounter0 && counter0After != null && !ReferenceEquals(counter0Before, counter0After),
               "★ B1b **视图真的重建了**：`Counter 0` 是**新对象**（旧实例 id " + counter0IdBefore
             + " → 新 " + (counter0After != null ? counter0After.GetInstanceID() : 0) + "）"
             + "｜🧨 只累加计数、不真重建 ⇒ 红（挡住「计数器自己证明自己」）");

            // ---- C：离开 Cards 页之后再变 ⇒ **不该**再重建（灭自证：反方向）----
            win.ChangeTab(WindowTabType.CollectionDecks);
            int refreshOnDeck = win.CardsCollectionRefreshCountForTest;
            AddOneCard(def);
            C(win.CardsCollectionRefreshCountForTest, refreshOnDeck,
              "★ C1 在 Deck 页时背包再变 ⇒ Cards 页**不重建**（订阅已退：原版 `OnDisable` 之后信号收不到）"
            + "｜🧨 不退订 ⇒ 计数器 +1 ⇒ 红");

            // ---- D：窗被销毁后背包再变 ⇒ 不许抛（守卫当场退订）----
            if (ownWin)
            {
                win.ChangeTab(WindowTabType.CollectionCards);   // 先切回（订阅挂着）再销毁 ⇒ 走「已订阅却被销毁」那条最危险的路
                bool hookedBeforeDestroy = win.CardsCollectionSignalHookedForTest;
                Tr(hookedBeforeDestroy, "D0 **前提**：销毁之前订阅是挂着的（否则 D1 测不到那条路）");
                UnityEngine.Object.DestroyImmediate(win.gameObject);
                win = null;
                bool threw = false; string err = null;
                try { AddOneCard(def); }
                catch (Exception e) { threw = true; err = e.GetType().Name + "：" + e.Message; }
                Tr(!threw, "★ D1 窗被 `DestroyImmediate` 之后背包再变 ⇒ **不抛**、守卫当场退订"
                 + "（先例 → `项目任务.md` §三 `B103_F1`：三处宿主整条崩）"
                 + (threw ? "｜实测抛了 " + err : "")
                 + "｜🧨 删掉 `OnCardsInventoryChanged` 第一句 `this == null` 守卫 ⇒ 抛 `MissingReferenceException` ⇒ 红");
            }
            else
            {
                Debug.Log("[Q2] A 段用的是外部传进来的收藏窗 ⇒ 跳过 D 组（不销毁别人的窗；D 组请用自建窗那一档跑）");
            }
        }
        finally
        {
            // ---- 收尾：还原数量 / 窗 / 页签（⛔ 不留窗、不碰玩家真存档）----
            if (cardId != null && amountBefore >= 0 && Inventories.Cards != null)
            {
                var back = Inventories.Cards.Get(cardId);
                if (back != null) back.SetAmount(amountBefore);   // `SetAmount` 不发事件（不会反过来再推一次重建）
            }
            if (win != null)
            {
                if (ownWin) RewardsWindow.DestroySafe(win.gameObject);
                else if (entryTab != WindowTabType.None) win.ChangeTab(entryTab);
            }
        }
    }

    /// <summary>往卡背包加一张（= 原版 `CardInventory.Add`）。**带上当前档位**，否则 `UpdateCardTier` 会把等级打回 1
    /// （判据见 `Shell/RewardServicePartial_C9.cs` 文件头「必须带着的规避」）。</summary>
    static void AddOneCard(RuleEngine.CardDef def)
    {
        if (def == null) return;
        int tier = CardProgress.Level(def.Id) - 1;
        var cd = new Dictionary<string, string> { { "CardTier", tier.ToString() } };
        Inventories.Cards.Add(def, 1, null, cd, false);
    }

    /// <summary>Cards 页上第 <paramref name="i"/> 格的「张数」节点（名字 = `Counter i`，见 `BuildCardsCounter`）。</summary>
    static Transform CounterNode(CollectionWindow win, int i)
    {
        if (win == null) return null;
        var page = win.PageRoot(1);          // 只读：页已建过（A1 切到过 Cards）⇒ `SetupOnce` 空转
        if (page == null) return null;
        return FindDeep(page, "Counter " + i);
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            var ch = root.GetChild(i);
            if (ch == null) continue;
            if (ch.name == name) return ch;
            var hit = FindDeep(ch, name);
            if (hit != null) return hit;
        }
        return null;
    }

    // ============================================================ B · 物品窗：空上下文 = LogError + return
    static void SectionB_ItemDisplayEmptyContext()
    {
        MenuCheck.Section(_sink, "B 物品窗：空上下文 ⇒ `LogError(\"No item available\") + return`（一个节点都不建）");

        ItemDisplayWindow empty = null, full = null;
        bool logged = false;
        Application.LogCallback hook = null;
        try
        {
            // ---- E1：空上下文 ----
            hook = (cond, stack, type) =>
            {
                if (type == LogType.Error && cond != null && cond.Contains("No item available")) logged = true;
            };
            Application.logMessageReceived += hook;

            empty = ItemDisplayWindow.Create(null, null);     // `ctx == null` = 原版 `context.item == null` 的第一条来源
            Application.logMessageReceived -= hook; hook = null;

            Tr(empty != null, "E0 **前提**：`ItemDisplayWindow.Create(null, null)` 建得出来");
            Tr(logged, "★ E1 空上下文 ⇒ **`LogError`「No item available」**（原版原文，`out_il2csharp` 现读；⛔ 不静默）");
            if (empty != null)
            {
                C(empty.transform.childCount, 0,
                  "★ E1b 空上下文 ⇒ **一个节点都不建**（`transform.childCount == 0`）"
                + "｜🧨 退回 `DefContext()` 铺 `Title`/`New Text`/`Type`（= 改前的行为）⇒ 红");
                Tr(empty.TitleLabel == null && empty.PanelNode == null,
                   "★ E1c ……读数口一致：`TitleLabel` / `PanelNode` 都是 null（没建那三颗出厂文本、也没建面板）");
            }

            // ---- E2（对照 / 灭自证）：非空上下文照样建 ----
            full = ItemDisplayWindow.Create(null, new ItemDisplayWindow.Context
            {
                Title = "Q2", Type = "Q2", ArtName = "q2-probe", Amount = 1,
            });
            Tr(full != null, "E0b **前提**：`ItemDisplayWindow.Create(null, ctx)` 建得出来");
            if (full != null)
            {
                Tr(full.transform.childCount > 0, "★ E2 非空上下文 ⇒ 照常建（`childCount > 0`）"
                  + "｜🧨 空上下文的闸写过头（把有内容的也拦掉）⇒ 红（与 E1 并排 ⇒「恒建 / 恒不建」都只能对一半）");
                Tr(full.TitleLabel != null && full.PanelNode != null, "★ E2b ……而且标题/面板都在（`TitleLabel` / `PanelNode` 非 null）");
            }
        }
        finally
        {
            if (hook != null) Application.logMessageReceived -= hook;
            RewardsWindow.DestroySafe(empty != null ? empty.gameObject : null);
            RewardsWindow.DestroySafe(full != null ? full.gameObject : null);
        }
    }
}
