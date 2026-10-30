// B102AUD03R_Assert.cs — 执行代理 `B102_AUD03R`（2026-10-30）的断言：`AUD03` 普查的**三个跨窗根因**
//
//   ① `GameWindow.Close()` 只 `SetActive(false)`、**不销毁**（老账 `A123`）⇒ 每「关掉再开」一轮多一具 inactive 尸体；
//   ② `WindowsManager.OpenByRef` 走「新建」支时**不销毁**缓存里那具旧实例；
//   ③ `TabButtons.OnTabChanged` 在「整窗重建」时**每次都 +1 条死 handler**（收藏 / 奖励 / 设置三族）。
//
// ============================ 期望值从哪来（⛔ 不许拿我们自己的常量证明我们自己的实现）============================
//  · ① ② 的期望 = **原版 `WindowsManager.CloseWindowCO`**（`资料/完全复刻/判据/AUD03_reuse_paths.md` §「跨窗根因」1/2）：
//    非 baseMenu 支 = `Hide()` → `List.Remove` → **`Object.Destroy(win.gameObject)`** + 释放 + 删缓存条目
//    ⇒ 「关掉再开一轮」之后**旧对象必须不存在**，而不是「还在、只是不活」。
//  · ③ 的期望 = **原版 `TabButtons.Initialize` 只有一条订阅**（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TabButtons.cs:83-267`：
//    `window.OnTabChanged += UpdateTab` 一次；原版没有「整窗重建还留着旧容器」这回事 —— 它是 `Destroy` + 新建窗口）。
//
// ============================ 🔴 本批的一处【如实标注】：收割的**时机**与原版不同 ============================
//  ① 修法不是「`Close()` 当拍 `Destroy`」，而是「**记账 → 下一次开窗时统一收割**」（理由与清单 →
//  `Shell/WindowsManager.cs` 的 `QueueDestroyOnClose` 那段 doc：3 处生产点 + 十余段夹具靠「关掉后同一实例再开」，
//  那些文件都不在本批白名单内）。⇒ **A1 的前提⑥ 钉住了这个差别**（关掉当拍「还活着、只是不活」），
//  **A1 ★** 钉的是**玩家可见的那条语义**（「关掉再开一轮」之后旧对象真的没了）。若将来改成当拍销毁，前提⑥ 会红，
//  那是**预期**的（改的人把那条期望值一起改掉即可）。
//
// ============================ 灭自证（每条 ★ 的 🧨 在文案里写死）============================
//  · ① A1 / A1b：🧨 删掉 `GameWindow.Close()` 尾句的 `Manager.QueueDestroyOnClose(this)` ⇒ 两红（`go1` 仍非 null、具数 +1）。
//  · ① A2：🧨 把收割写成「开窗时把场上的窗都销毁」⇒ 红（新开的那一扇被误杀）。
//  · ② A1：🧨 删掉 `OpenByRef` 里那句 `DestroyWindowObject(cached.gameObject)` ⇒ 红（旧对象要等 `OpenWindow` 收割才没，
//    「新建之前」仍是活的）—— 与 ①**结构上分得开**（① 那条仍旧绿），这正是「两处要分别改」的锁。
//  · ③ A1：🧨 删掉 `TabButtons.Initialize` 里那句 `DropDeadHandlers` ⇒ 红（1 → 2）。
//  · ③ A2：🧨 把 `DropDeadHandlers` 改成「只留自己」⇒ 红（2 → 1；两个容器**都还活着**，不该被谁清掉）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `CardPresentation/Editor/ShellScene.cs` 的 `ShellScene.Run`（同 `B102AUD04_Assert` 那条代接线）。
//  接在**合计行之前**、**同一行追加**（⇒ 行数零变化）：
//      B102AUD03R_Assert.Run(); _sink.Pass += B102AUD03R_Assert.Pass; _sink.Fail += B102AUD03R_Assert.Fail;
//  也可以单独跑：`-executeMethod B102AUD03R_Assert.Run`。
//  ⚠️ 本件**不需要宿主已开着任何窗**：它自己 `WindowsManager.EnsureHost()` 拿管理器（宿主里已有 ⇒ 复用同一台），
//    用完把自建的窗关掉 + `DestroyImmediate`，并 `ClearReuseCacheForTest()` 收干净缓存条目。
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B102_AUD03R`：`AUD03` 三个跨窗根因（关窗销毁 / `OpenByRef` 换实例 / `TabButtons` 死 handler）。</summary>
public static class B102AUD03R_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102AUD03R] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B102_AUD03R · AUD03 三个跨窗根因（① 关窗=销毁 ② OpenByRef 新建前销毁旧实例 ③ 每次重建 +1 死 handler）");
        try { A1_CloseIsDestroy(); }
        catch (Exception e) { Tr(false, "①「关窗=销毁」这一组抛异常（后面的条目没跑完）：" + e.GetType().Name + "：" + e.Message); }
        try { A2_OpenByRefReplacesInstance(); }
        catch (Exception e) { Tr(false, "②「OpenByRef 换实例」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { A3_TabButtonsDeadHandler(); }
        catch (Exception e) { Tr(false, "③「TabButtons 死 handler」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B102AUD03R 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    /// <summary>场上某类对象有几具（**含 inactive** —— 尸体就是 inactive 的，⛔ 别用只找活物的那个重载）。</summary>
    static int CountType<T>() where T : UnityEngine.Object
    {
        return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
    }

    // ============================================================ ① 关窗 = 销毁
    static void A1_CloseIsDestroy()
    {
        MenuCheck.Section(_sink, "① `GameWindow.Close()` ⇒ **销毁**（原版 `CloseWindowCO` 的非 baseMenu 支：`Hide()` → `Remove` → `Object.Destroy`）");

        var wm = WindowsManager.EnsureHost();
        Tr(wm != null, "A0-前提①：拿到 `WindowsManager`（宿主里已有则复用同一台；拿不到 ⇒ 本组下面全无效）");
        if (wm == null) return;

        var w1 = BattleLogPopup.Create(wm);
        wm.OpenWindow(w1);                                  // ← 这一拍会先「收割」掉场上关过的旧窗
        var go1 = w1.gameObject;
        C(w1.CurrentState, WindowState.Open, "A0-前提②：第一扇真的开起来了（`CurrentState == Open`）");
        Tr(go1 != null && go1.activeSelf, "A0-前提③：它的 GameObject 活着且 active（不然下面「关掉」那一拍没意义）");
        Tr(wm.openWindows.Contains(w1), "A0-前提④：它在 `openWindows` 里（管理过 = 才有「尸体」可收）");

        int nAfterOpen1 = CountType<BattleLogPopup>();       // 此时已收割过一轮 ⇒ 这是「干净」的基数

        w1.Close();
        Tr(go1 != null && !go1.activeSelf,
           "A0-前提⑥：关掉**当拍**它「还活着、只是不活」（= 本仓「延期收割」的形态 —— 原版这一拍已经 `Destroy` 了，"
         + "差别与理由见 `WindowsManager.QueueDestroyOnClose` 那段 doc；改成当拍销毁的人要把这条期望值一起改）");
        Tr(WindowsManager.PendingDestroyCountForTest(wm) >= 1,
           "A0-前提⑦：它被记进「待收」表（= `Close()` 尾句那句 `QueueDestroyOnClose` 真的跑了；这条不成立 ⇒ 下面的 A1 是空断）");

        var w2 = BattleLogPopup.Create(wm);
        wm.OpenWindow(w2);                                  // ← 这一拍收割

        Tr(go1 == null,
           "A1 ★ 关掉再开一轮之后，**旧的那一具真的没了**（`gameObject == null` = 销毁，而不是 inactive）"
         + " —— 🧨 删掉 `GameWindow.Close()` 尾句的 `Manager.QueueDestroyOnClose(this)` ⇒ 本条红（`go1` 仍非 null）");
        C(CountType<BattleLogPopup>(), nAfterOpen1,
           "A1b ★ 场景里本类窗的**具数不变**（旧没了、新顶上 ⇒ 尸体不累积）—— 🧨 同上 ⇒ 变成 基数+1");
        Tr(w2 != null && w2.gameObject != null && w2.CurrentState == WindowState.Open,
           "A2 ★ 灭自证：新开的那一扇**没被误杀**（🧨 把收割写成「开窗时把场上的窗都销毁」⇒ 本条红）");

        if (w2 != null) { w2.Close(); UnityEngine.Object.DestroyImmediate(w2.gameObject); }
    }

    // ============================================================ ② OpenByRef 新建前销毁旧实例
    static void A2_OpenByRefReplacesInstance()
    {
        MenuCheck.Section(_sink, "② `WindowsManager.OpenByRef` 走「新建」支**之前**销毁缓存里那具旧实例（原版 `CloseWindowCO` 的三件套）");

        var wm = WindowsManager.EnsureHost();
        Tr(wm != null, "A0-前提①：拿到 `WindowsManager`");
        if (wm == null) return;

        WindowsManager.ClearReuseCacheForTest();
        const string Key = "B102AUD03R Probe";

        var p1 = WindowsManager.OpenByRef(Key, m => GenericOptionsPanel.Create(m));
        Tr(p1 != null, "A0-前提②：第一扇经 `OpenByRef` 建出来了");
        if (p1 == null) return;
        var go1 = p1.gameObject;
        int id1 = p1.GetInstanceID();                       // ⚠️ 必须在 `Close()` **之前**取（销毁之后取到的是假 null 那一套）
        Tr(wm.openWindows.Contains(p1), "A0-前提③：它在 `openWindows` 里（下一次 `OpenByRef` 才会先命中缓存条目）");

        int nAfterOpen1 = CountType<GenericOptionsPanel>();  // 收割过一轮之后的基数（含 p1）

        p1.Close();
        Tr(!WindowsManager.StillOpen(wm, p1),
           "A0-前提④：关掉之后 `StillOpen` 为假（不成立 ⇒ 下一次 `OpenByRef` 会走**复用**支，测不到「新建前销毁」）");

        bool oldGoneDuringCreate = false;
        var p2 = WindowsManager.OpenByRef(Key, m =>
        {
            oldGoneDuringCreate = (go1 == null);            // ← 测的正是「**开始建新实例那一刻**旧对象还在不在」
            return GenericOptionsPanel.Create(m);
        });

        Tr(oldGoneDuringCreate,
           "A1 ★ 「新建」那一支**开始建新实例之前**，旧的那一具就已经销毁了"
         + " —— 🧨 删掉 `OpenByRef` 里那句 `DestroyWindowObject(cached.gameObject)` ⇒ 本条红（旧的要等 `OpenWindow` 收割才没，"
         + "「建新之前」仍是活的）· 与 ① 那条**结构上分得开**（① 仍绿）");
        Tr(p2 != null && p2.GetInstanceID() != id1, "A1b ★ 拿到的是一扇**新实例**（不是那具尸体、也不是假 null）");
        Tr(go1 == null, "A1c ★ 旧的那一具现在确实没了");
        C(CountType<GenericOptionsPanel>(), nAfterOpen1, "A1d ★ 场景里本类窗只有那么多（连开两次不累积尸体）");
        Tr(p2 != null && p2.CurrentState == WindowState.Open, "A2 新开的那一扇是开着的（没收错对象）");

        if (p2 != null) { p2.Close(); UnityEngine.Object.DestroyImmediate(p2.gameObject); }
        WindowsManager.ClearReuseCacheForTest();
    }

    // ============================================================ ③ TabButtons 死 handler
    static void A3_TabButtonsDeadHandler()
    {
        MenuCheck.Section(_sink, "③ `TabButtons.OnTabChanged` 不随「整窗重建」累积死 handler（收藏 / 奖励 / 设置三族）");

        var wgo = new GameObject("B102AUD03R TabWin", typeof(RectTransform));
        var win = wgo.AddComponent<GameWindowWithTabs>();
        Tr(win != null, "A0-前提①：探针 `GameWindowWithTabs` 建出来了（`AddComponent` 成功 —— 它有自己的脚本资产）");
        if (win == null) return;
        C(TabButtons.HandlerCountForTest(win), 0, "A0-前提②：新窗的 `OnTabChanged` 一开始一条 handler 都没有");

        var tb1 = NewTabButtons(win);
        C(TabButtons.HandlerCountForTest(win), 1, "A0-前提③：第一次 `Initialize` 之后正好 1 条");
        C(TabButtons.DeadHandlerCountForTest(win), 0, "A0-前提④：而且没有死的");

        // = 整窗重建（`MainMenuSubmenuWindow.BuildBar` 每次 `Build()` 都新建一个 `Tab Buttons` 节点 + 新 `TabButtons`）
        UnityEngine.Object.DestroyImmediate(tb1.gameObject);
        C(TabButtons.HandlerCountForTest(win), 1,
           "A0-前提⑤：旧容器销毁之后它那条订阅**还挂在事件上**（= 缺陷现场，不是期望值）");
        C(TabButtons.DeadHandlerCountForTest(win), 1, "A0-前提⑥：而且那一条确实被判成「死的」（`Target` 是 Unity 假 null）");

        NewTabButtons(win);
        C(TabButtons.HandlerCountForTest(win), 1,
           "A1 ★ 重建之后再 `Initialize`，事件上仍然只有 **1** 条 —— 🧨 删掉 `Initialize` 里那句 `DropDeadHandlers` ⇒ 本条变成 2 ⇒ 红");
        C(TabButtons.DeadHandlerCountForTest(win), 0, "A1b ★ 而且**没有死 handler** 了（旧容器那条被摘掉）");

        var tb3 = NewTabButtons(win);
        C(TabButtons.HandlerCountForTest(win), 2,
           "A2 ★ 灭自证：**还活着的**两个容器都在 —— 🧨 把 `DropDeadHandlers` 改成「只留自己」⇒ 本条变 1 ⇒ 红"
         + "（说明正确修法是「只摘死的」，不是「全清再挂」）");
        C(TabButtons.DeadHandlerCountForTest(win), 0, "A2b 这两条都是活的");

        tb3.Initialize(win);
        C(TabButtons.HandlerCountForTest(win), 2, "A2c 同一个容器重复 `Initialize` 不重复订阅（先摘后挂）");

        UnityEngine.Object.DestroyImmediate(win.gameObject);
    }

    /// <summary>建一个 `Tab Buttons` 节点 + `TabButtons` 容器并 `Initialize`（= `MainMenuSubmenuWindow.BuildBar` 那一跳的等价物）。</summary>
    static TabButtons NewTabButtons(GameWindowWithTabs win)
    {
        var go = new GameObject("Tab Buttons", typeof(RectTransform));
        go.transform.SetParent(win.transform, false);
        var tb = go.AddComponent<TabButtons>();
        tb.Initialize(win);
        return tb;
    }
}
