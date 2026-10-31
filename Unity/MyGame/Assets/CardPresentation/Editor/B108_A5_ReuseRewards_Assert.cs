// B108_A5_ReuseRewards_Assert.cs — 执行代理 `B108_A5`（2026-10-31）的断言：`AUD03`「复用 / 生命周期」里
//   落在**奖励·收件箱·每日两弹窗 · `GameWindow` / `GameWindowWithTabs` / `WindowsManager`** 那几行。
//
// 覆盖的三条（其余行是「现读证据证明不成立 / 已由前批修掉」，逐条写在 `报告/B108_A5.md` 里，这里不给空断言）：
//   A1  判据表 **#54 / #55 / #93**：`GameWindowWithTabs._cachedTabs` 在「整窗重建页对象」之后**仍指已销毁的旧页**；
//       `CurrentTabView` 也停在死对象上。
//   A2  判据表 **#60**：`WindowsManager.SetBaseMenu` 只清表 ⇒ 上一屏还开着的窗变成「activeSelf 真、却不在册」的孤儿。
//   A3  判据表 **#102**：`RewardWindow.Close` 的回调**每次 `Close()` 都发**（原版契约 —— 见下面 A3 那段反证；
//       **本轮零改动**，这条断言是用来**钉住「别按『只回调一次』去改」**的）。
//
// ============================ 期望值从哪来（⛔ 不许拿我们自己的常量证明我们自己的实现）============================
//  · A1 的「为什么缓存必须重建」= **我们的窗每次 `Open` 都重建页对象**（`RewardsWindow.BuildTabContents` :
//    `tabs.Clear()` + 新 `AddComponent`；`CollectionWindow` 同一路）—— 这是**我们的同源差**（原版 `tabs` 只建一次），
//    不是原版机制；判据 → `GameWindowWithTabs.cs` 文件头差异②。
//  · A2 的两份动作**都是原版的**：`GameWindow.Hide()`（原版 `CloseWindowCO` 里 Slot 9）+
//    `Object.Destroy(win.gameObject)`（原版 `CloseWindowCO` 非 baseMenu 支末句，本仓走「记账 → 下次开窗收割」）。
//    原版 `WindowsManager__SetBaseMenu.c` 只有「日志 → 写 0x48 → `List.Clear`」三句 —— 它不需要收窗，
//    因为原版那一刻场上**还没有任何窗**（`MainMenuWindow.Initialize` 是整场第一扇）。
//  · A3 的期望 = `d:/2/tools/decomp_full/RewardWindow__Close.c` **全文**（2026-10-31 亲读，18 行）：
//      `ctx = get_context(this); if (ctx != 0) { cb = *(ctx+0x30); if (cb != 0) { invoke(cb, *(ctx+0x38)); }
//        GameWindow__Close(this); return; }`
//    ⇒ **回调在 `GameWindow.Close` 之前、且没有任何状态守卫** ⇒ 原版**每调一次 `Close()` 就回调一次**
//      （第二次 `Close` 只是被基类的 `!gameObject.activeSelf ⇒ return` 吃掉）。判据表那一行原来写「？判不准」
//      + 建议「先判 `Closed` 直接 return」—— **与 `.c` 冲突** ⇒ 按「判据与 `.c` 冲突时以 `.c` 为准、宁可拒做」处理：
//      **不改实现**，改用 A3 把原版契约钉住。
//
// ============================ 灭自证（每条 ★ 的 🧨 在文案里写死）============================
//  · A1 ★1：🧨 删掉 `EnsureTabsSetup` 里那句 `RebuildTabCache()` ⇒ `CachedTabDeadCountForTest` 变 2 ⇒ 红。
//    ★2（`GetTab` 那条）**仍绿** —— 它由 `GetTab(Type)` 自己的「死条目回落」兜住 ⇒ **两条修法结构上分得开**：
//    ★1 盯「缓存真的被重建」、★2 盯「读口不返回死对象」。
//  · A1 ★3：🧨 把 `RebuildTabCache` 改成「只要 `CurrentTabView` 不在 `tabs` 里就置 null」⇒ **A1b 会红**
//    （活着的当前页被清掉）—— 那是「照原版保留当前页」的反面。
//  · A2 ★1：🧨 把 `RetireOpenWindowsOnBaseMenuChange` 整个删掉（回到只 `Clear()`）⇒ 两条红
//    （窗仍 active、仍不在册）。
//  · A2 ★2：🧨 把收窗写成「连新 `baseMenu` 一起收」⇒ `marker` 那条红。
//  · A3 ★：🧨 在 `RewardWindow.Close` 里加「`CurrentState == Closed` 就直接 return」⇒ 第二次回调变 0 ⇒ 红
//    （= 有人按判据表那条**建议**去改的话，这条会当场拦住它，并把 `.c` 的出处指回来）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主任选其一（本件**不需要宿主已经开着任何窗**，它自己建/自己拆）：
//      B108_A5_ReuseRewards_Assert.Run(); _sink.Pass += B108_A5_ReuseRewards_Assert.Pass; _sink.Fail += B108_A5_ReuseRewards_Assert.Fail;
//  接在**合计行之前**、**同一行追加**（⇒ 行数零变化）。也可以单独跑：`-executeMethod B108_A5_ReuseRewards_Assert.Run`。
//  ⚠️ A2 用的是**自建的独立 `WindowsManager`**（不是 `EnsureHost()`）—— 它要调 `SetBaseMenu`，
//    而宿主里那台身上挂着真主菜单；动它会把宿主的 `baseMenu` 收掉、后面几十条断言跟着失效。
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B108_A5`：`AUD03` 里奖励/收件箱/每日两弹窗 + `GameWindowWithTabs` 页缓存那几行。</summary>
public static class B108_A5_ReuseRewards_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B108_A5] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B108_A5 · AUD03 复用/生命周期（① 页对象缓存随重建重建 ② SetBaseMenu 收窗 ③ RewardWindow.Close 原版契约）");
        try { A1_TabCacheRebuild(); }
        catch (Exception e) { Tr(false, "①「页对象缓存」这一组抛异常（后面的条目没跑完）：" + e.GetType().Name + "：" + e.Message); }
        try { A2_SetBaseMenuRetiresWindows(); }
        catch (Exception e) { Tr(false, "②「`SetBaseMenu` 收窗」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { A3_RewardWindowCloseCallback(); }
        catch (Exception e) { Tr(false, "③「`RewardWindow.Close` 回调」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B108_A5 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ ① 页对象缓存随重建而重建（#54 / #55 / #93）
    static void A1_TabCacheRebuild()
    {
        MenuCheck.Section(_sink, "① `GameWindowWithTabs._cachedTabs` / `CurrentTabView` 在「整窗重建页对象」之后必须跟着重建（判据表 #54/#55/#93）");

        var wgo = new GameObject("B108A5 TabsWin", typeof(RectTransform));
        var win = wgo.AddComponent<GameWindowWithTabs>();
        Tr(win != null, "A0-前提①：探针 `GameWindowWithTabs` 建出来了（`AddComponent` 成功 ⇒ 它有自己的脚本资产）");
        if (win == null) return;

        var a = NewTab(win, "TabA", WindowTabType.Missions);
        win.tabs.Add(a);

        win.Open();                                       // = 生产第一次开窗：`EnsureTabsSetup()` → `SetupTabs()` + `OpenTabs()`
        Tr(win.TabsInitializedForTest, "A0-前提②：`SetupTabs` 走过（`_tabsInitialized = true`）");
        C(win.CachedTabDeadCountForTest, 0, "A0-前提③：刚建好时缓存里没有死条目");
        Tr(ReferenceEquals(win.GetTab(typeof(ProbeTab)), a), "A0-前提④：`GetTab(Type)` 拿到的是 `tabs[0]`（活的那一页）");

        // —— 模拟生产第二次开窗：`BuildTabContents` 把页对象**整批重建**（旧页随 `DestroyChildren` 一起销毁）
        UnityEngine.Object.DestroyImmediate(a.gameObject);
        win.tabs.Clear();
        var a2 = NewTab(win, "TabA2", WindowTabType.Missions);
        win.tabs.Add(a2);

        C(win.CachedTabDeadCountForTest, 1,
          "A0-前提⑤：重建之后缓存里**确实**躺着一条指向已销毁旧页的条目（= 缺陷现场本身，不是期望值；同一类型只有一个键）");
        Tr(win.CurrentTabView == null,
           "A0-前提⑥：`CurrentTabView` 此刻是 Unity 假 null（旧页被销毁了 —— 缺陷现场，不是期望值）");

        win.Open();                                       // = 生产第二次开窗（`EnsureTabsSetup` 已在真跑）

        C(win.CachedTabDeadCountForTest, 0,
          "A1 ★ 第二次 `Open()` 之后 `_cachedTabs` 里**一条死条目都没有**（缓存按活表 `tabs` 重建了）"
        + " —— 🧨 删掉 `EnsureTabsSetup` 里那句 `RebuildTabCache()` ⇒ 本条变 1 ⇒ 红");
        Tr(ReferenceEquals(win.GetTab(typeof(ProbeTab)), a2),
          "A1b ★ `GetTab(Type)` 返回的是**这一代**的活页（不是上一代那具尸体、也不是假 null）"
        + " —— 🧨 把 `RebuildTabCache` 改成「`CurrentTabView` 不在 `tabs` 就置 null」时本条仍绿，"
        + "但 A1c 会红；🧨 只删 `RebuildTabCache` 时本条**也仍绿**（`GetTab` 自己会回落扫 `tabs`）⇒ 与 A1 ★ 结构上分得开");
        Tr(ReferenceEquals(win.CurrentTabView, a2),
          "A1c ★ `CurrentTabView` 跟着落到**活页**上（起始页 = `tabs[0]`）—— 🧨 见 A1b 的反证说明");

        UnityEngine.Object.DestroyImmediate(win.gameObject);
    }

    /// <summary>建一条 `ProbeTab` 页（= `ShellScene` ④ 段夹具页的同一份；它只记账、不碰美术）。</summary>
    static ProbeTab NewTab(GameWindowWithTabs win, string name, WindowTabType type)
    {
        var t = new GameObject(name).AddComponent<ProbeTab>();
        t.type = type;
        t.transform.SetParent(win.transform, false);
        t.gameObject.SetActive(false);
        return t;
    }

    // ============================================================ ② SetBaseMenu 收窗（#60）
    static void A2_SetBaseMenuRetiresWindows()
    {
        MenuCheck.Section(_sink, "② `WindowsManager.SetBaseMenu` 换主菜单那一刻，把上一屏还开着的窗**收起 + 记账销毁**（判据表 #60）");

        if (Application.isPlaying)
        {
            Tr(false, "A0-前提⓪：本组在 Play 模式下**不成立**（自建的探针 `WindowsManager` 会被 `Awake` 的「一山不容二虎」自毁）"
                    + " ⇒ 只应在编辑模式（批处理宿主）里跑");
            return;
        }

        var wgo = new GameObject("B108A5 WM Probe");
        var wm = wgo.AddComponent<WindowsManager>();
        Tr(wm != null, "A0-前提①：**独立**探针 `WindowsManager` 建出来了（⛔ 不用 `EnsureHost()` —— 宿主那台身上挂着真主菜单）");
        if (wm == null) return;

        try
        {
            var probe = BattleLogPopup.Create(wm);
            wm.OpenWindow(probe);
            Tr(probe.CurrentState == WindowState.Open, "A0-前提②：探针窗真的开起来了（`CurrentState == Open`）");
            Tr(wm.openWindows.Contains(probe), "A0-前提③：它在 `openWindows` 里（有「孤儿」可言）");

            var marker = new GameObject("B108A5 BaseMenu Probe").AddComponent<MainMenuWindow>();   // 2026-10-31 收尾：`RetireOpenWindowsOnBaseMenuChange` 只对**真主菜单基座**生效（生产口径），探针必须用 MainMenuWindow
            var mgo = marker.gameObject;

            wm.SetBaseMenu(marker);   // ← 本笔的修法就在这一跳里

            Tr(probe != null && probe.gameObject != null && !probe.gameObject.activeSelf,
              "A1 ★ 换主菜单之后，上一屏那扇窗**被收起**（`activeSelf == false`）"
            + " —— 🧨 删掉 `RetireOpenWindowsOnBaseMenuChange`（回到只 `Clear()`）⇒ 本条红（它仍 active 地叠在新主菜单上）");
            C(wm.openWindows.Count, 0,
              "A1b ★ 登记表清空 —— 🧨 同上 ⇒ 变 1（「活着却不在册」的孤儿，`ESC`/`CloseAllWindows` 全够不到）");
            C(probe.CurrentState, WindowState.Closed,
              "A1c ★ 状态是 `Closed`（不是 `Background`）—— 只有这样它才会被「关过的窗」那条收割路真销毁");
            Tr(WindowsManager.PendingDestroyCountForTest(wm) >= 1,
              "A1d ★ 它被记进「待收」表（= 原版 `CloseWindowCO` 末句 `Object.Destroy` 的等价物；本仓下一次开窗收割）");
            Tr(wm.BaseMenu == marker && !wm.openWindows.Contains(marker),
              "A2 ★ 灭自证：**刚登记的那扇主菜单没被误收**（🧨 把收窗写成「连 `menu` 一起收」⇒ 本条红）");
            Tr(mgo != null && mgo.activeSelf,
              "A2b ★ 灭自证（同上）：主菜单的物体仍然活着（我们没收它、也没藏它）");

            UnityEngine.Object.DestroyImmediate(mgo);
        }
        finally
        {
            if (wm != null) UnityEngine.Object.DestroyImmediate(wm.gameObject);
        }
    }

    // ============================================================ ③ RewardWindow.Close 的原版契约（#102）
    static void A3_RewardWindowCloseCallback()
    {
        MenuCheck.Section(_sink, "③ `RewardWindow.Close` 的回调**每次 `Close()` 都发**（原版 `RewardWindow__Close.c` 逐句；本轮**零改动**，这条用来钉住「别按判据表那条建议去改成只回调一次」）");

        var win = RewardWindow.Create(null);              // Manager = null ⇒ 不碰任何 `openWindows` 登记
        Tr(win != null, "A0-前提①：`RewardWindow` 建出来了");
        if (win == null) return;

        try
        {
            int calls = 0;
            var ctx = new RewardWindowContext
            {
                Rewards = new CampaignData.RewardSpec[0],
                IsPremiumLocked = true,
                IsPreview = false,
                OnClose = delegate { calls++; },
            };
            Tr(win.TryOpen(ctx), "A0-前提②：`TryOpen(ctx)` 走完了（= `SetupData` + `Open()` 建树）");
            Tr(win.CurrentState == WindowState.Open, "A0-前提③：它现在是 `Open` 态");
            C(calls, 0, "A0-前提④：还没关过 ⇒ 回调 0 次");

            win.Close();
            C(calls, 1, "A3 ★ 第一次 `Close()` ⇒ 回调 1 次（原版：回调写在 `GameWindow__Close` **之前**）");

            win.Close();   // 第二次：基类首句 `!gameObject.activeSelf ⇒ return` 吃掉，但**回调已经发过了**
            C(calls, 2,
              "A3b ★ 第二次 `Close()` ⇒ 回调**再发一次**（= 原版契约：回调在基类守卫**之前**、无状态判断）"
            + " —— 🧨 在 `RewardWindow.Close` 里加「`CurrentState == Closed` 就直接 return」⇒ 本条变 1 ⇒ 红"
            + "（判据 = `d:/2/tools/decomp_full/RewardWindow__Close.c` 全文：`if (cb != 0) invoke(cb, *(ctx+0x38)); GameWindow__Close(this);`）");
        }
        finally
        {
            if (win != null) UnityEngine.Object.DestroyImmediate(win.gameObject);
        }
    }
}
