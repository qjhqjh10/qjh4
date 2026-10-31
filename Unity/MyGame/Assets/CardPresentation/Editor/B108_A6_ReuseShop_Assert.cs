// B108_A6_ReuseShop_Assert.cs — 执行代理 `B108_A6`（2026-10-31）落地的 AUD03 ❌ 断言
//
// ============================ 覆盖（逐条对上 `判据/AUD03_reuse_paths.tsv`）============================
//  · `RunCampaign(WindowsManager wm)` —— #51「战役·奖励弹窗开窗」：
//      `CampaignTab.ClickNodeForTest` 必须照原版 `CampaignWindowTab.OnNodeClicked` 走
//      `WindowsManager.OpenWindow<CampaignRewardsWindow>(ctx, 0, …)`（`decomp_full/CampaignWindowTab__OnNodeClicked.c` 末尾三句），
//      而那条路会**先命中 `automaticallyLoadedWindows` 复用同一扇**（`out_cpp2il_dev_ilspy/WindowsManager.cs:1298-1304`），
//      只有 `CloseWindowCO`（`:1623-1628`：`Hide` → 摘表 → **`Object.Destroy` + 释放缓存条目**）之后才新建。
//      ⇒ 断言：① 开着时再点节点 **不叠第二具**；② 关过的那一具在**下一次开窗**被收割（= 本仓的 `QueueDestroyOnClose`/`ReapClosedWindows`）。
//  · `Run()`（自包含、不需要壳）—— #61「`MainMenuRuntime` 的三个 static 记账」：
//      主菜单**树**被销毁（切战斗 / 场景卸载）之后，`Window` / `_pendingBaseMenu` / `_pendingRoot` 必须**真置空**。
//      原版 `MainMenuWindow` 就是主菜单树里的一个 GameObject（`MainMenuWindow.Initialize` 由那棵树调）⇒ 树没了就不该再指着它。
//      ⚠️ 编辑模式 `DestroyImmediate` **不派 `OnDestroy`**（本仓已知坑）⇒ 夹具用 `SendMessage("OnDestroy")` 显式派一次，
//         走的仍是生产同一个方法体。
//
// ============================ 口径（⛔ 别拿我们自己的常量证明我们自己的实现）============================
//  · 期望值全部来自**原版判据**（反编译方法体），逐条写在用例文案里。
//  · 前提断言与结论断言**分开写**：⛔ 不用 `if (x != null) Check(...)`（前提不成立会静默跳过 —— 铁律）。
//  · 每条结论句都带 🧨「改回旧写法时哪条会红」。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  自包含那组（#61）接进任一宿主的聚合行（同一物理行、`//` 之前）：
//      B108A6_Assert.Run(); _sink.Pass += B108A6_Assert.Pass; _sink.Fail += B108A6_Assert.Fail;
//  带窗那组（#51）要有 `WindowsManager` 的宿主（`ShellScene` / `RewardsScene` / `ShopScene` 都行）：
//      B108A6_Assert.RunCampaign(wm); _sink.Pass += B108A6_Assert.Pass; _sink.Fail += B108A6_Assert.Fail;
//  ⚠️ `RunCampaign` 会**开一扇真窗**并触发一次 `ReapClosedWindows`（宿主里已排队待收的尸体会被收掉 —— 那是正常生命周期）；
//     收尾时把 `wm.currentWindow` / `wm.popUpWindow` 还原成进去之前那一份。
using System;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B108_A6`：AUD03 #51（战役奖励窗复用）/ #61（`MainMenuRuntime` 三个 static 记账）的断言。</summary>
public static class B108A6_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B108A6] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口

    /// <summary>自包含（不需要壳）：#61 主菜单树销毁 ⇒ 三个 static 记账置空。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B108_A6 · AUD03 #61（`MainMenuRuntime` 的 static 记账随主菜单树一起清）");
        try { A_MainMenuStatics(); }
        catch (Exception e) { Tr(false, "「MainMenuRuntime static 记账」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B108A6 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    /// <summary>#51：要有 `WindowsManager` 的宿主。</summary>
    public static void RunCampaign(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "B108_A6 · AUD03 #51（战役奖励窗：开着时复用同一扇 / 关过的那一具在下次开窗被收割）");
        try { B_CampaignRewardReuse(wm); }
        catch (Exception e) { Tr(false, "「战役奖励窗复用」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B108A6（#51）合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ #61

    static void A_MainMenuStatics()
    {
        // 前提快照（跑完还原 —— 这三个 static 也可能被宿主用过）
        MainMenuWindow w0 = MainMenuRuntime.Window;
        object p0 = StaticField("_pendingBaseMenu");
        object r0 = StaticField("_pendingRoot");

        GameObject goA = null, goB = null, winA = null, winB = null;
        try
        {
            // ---- 第一组：本棵树自己登记的 ⇒ 销毁它（OnDestroy）之后三格必须真置空 ----
            goA = new GameObject("B108A6_MenuTreeA");
            var rt = goA.AddComponent<MainMenuRuntime>();
            InstanceField(rt, "_root", goA.transform);          // = `Build()` 的第一句（`MainMenuRuntime.cs:403`），这里不跑整棵树
            winA = new GameObject("MainMenuA");
            var winCompA = winA.AddComponent<MainMenuWindow>();

            // 直接摆出 `RegisterMainMenuWindow` 写的那三格（`MainMenuRuntime.cs:704-706`）——⛔ 不走真登记，
            // 因为真登记会碰宿主的 `WindowsManager.SetBaseMenu`（那是本组不该有的副作用）。
            SetWindow(winCompA);
            StaticFieldSet("_pendingBaseMenu", winCompA);
            StaticFieldSet("_pendingRoot", goA.transform);

            Tr((object)MainMenuRuntime.Window != null, "A0-前提①：夹具把 `Window` 摆上了（为真 ⇒ 下面两条不是空断）");
            Tr((object)StaticField("_pendingRoot") == (object)goA.transform,
               "A0-前提②：`_pendingRoot` 指向本棵树（`OnDestroy` 的守卫 `_pendingRoot == _root` 认的就是这一条）");

            goA.SendMessage("OnDestroy");                       // 编辑模式 `DestroyImmediate` 不派 `OnDestroy` ⇒ 显式派一次

            Tr((object)MainMenuRuntime.Window == null,
               "A1 ★★ #61 树销毁之后 `Window` **真置空**（`(object)Window == null`）。"
             + "🧨 删掉 `OnDestroy` 里 `if (_pendingRoot == _root) { … Window = null; }` 那半句 ⇒ 字段仍攥着已销毁的 `MainMenuWindow` ⇒ 本条红"
             + "｜⚠️ 这里必须 `(object)` 转一下：`Window == null` 在「已销毁」与「真置空」两种情况下**都为真**（Unity 假 null）⇒ 那样写是假绿");
            Tr((object)StaticField("_pendingBaseMenu") == null && (object)StaticField("_pendingRoot") == null,
               "A2 ★★ #61 `_pendingBaseMenu` / `_pendingRoot` 也真置空 —— `WindowsManager.EnsureHost` 回来认领时 "
             + "`OnWindowsManagerReady`（`MainMenuRuntime.cs:714-717`）读的就是这两个 ⇒ 不清就会拿一具死窗再去 `ApplyBaseMenu`");

            // ---- 第二组：守卫（别棵树的登记不许被抹掉）----
            goB = new GameObject("B108A6_MenuTreeB");
            var rtB = goB.AddComponent<MainMenuRuntime>();
            InstanceField(rtB, "_root", goB.transform);
            winB = new GameObject("MainMenuB");
            var winCompB = winB.AddComponent<MainMenuWindow>();
            SetWindow(winCompB);
            StaticFieldSet("_pendingBaseMenu", winCompB);
            StaticFieldSet("_pendingRoot", goB.transform);      // 现在静态记账归 B 树

            goA.SendMessage("OnDestroy");                        // 再销毁一次 A（它的 `_root` ≠ `_pendingRoot`）

            Tr((object)MainMenuRuntime.Window == winCompB && (object)StaticField("_pendingBaseMenu") == winCompB,
               "A3 ★ #61 守卫：销毁**别棵树**时，当前登记（B 树）原样保留（`_pendingRoot == _root` 那道闸）"
             + "🧨 去掉那道闸（无条件清）⇒ 自检夹具销毁自己那棵树时会把宿主真正在用的登记抹掉 ⇒ 本条红");
        }
        finally
        {
            if (goA != null) UnityEngine.Object.DestroyImmediate(goA);
            if (goB != null) UnityEngine.Object.DestroyImmediate(goB);
            if (winA != null) UnityEngine.Object.DestroyImmediate(winA);
            if (winB != null) UnityEngine.Object.DestroyImmediate(winB);
            // 还原宿主原先那一份记账
            SetWindow(w0);
            StaticFieldSet("_pendingBaseMenu", p0);
            StaticFieldSet("_pendingRoot", r0);
        }
    }

    // ============================================================ #51

    static void B_CampaignRewardReuse(WindowsManager wm)
    {
        Tr(wm != null, "B0-前提①：拿到 `WindowsManager`（宿主里已有则复用同一台）");
        if (wm == null) return;

        // 保存 / 还原管理器的那两格（`OpenWindow` 的弹窗支会写它们；⛔ 别把宿主的状态带跑）
        var saveCurrent = wm.currentWindow;
        var savePopUp = wm.popUpWindow;
        RewardWindowHostHolder hostHolder = new RewardWindowHostHolder();
        GameObject tabGo = null;
        CampaignRewardWindow first = null, second = null;
        var preexisting = new System.Collections.Generic.List<GameWindow>(wm.openWindows);   // 收尾只收**本次新开**的窗（⛔ 不动宿主的）

        try
        {
            // 🔴 夹具把 `currentWindow` 先腾空：`OpenWindow` 的弹窗支会对「当前那一扇」调 `ToBackground()`
            //    —— 那会把宿主正在用的窗打成 `Background` 态（只还原字段救不回来）。收尾还原。
            wm.currentWindow = null;

            hostHolder.Win = RewardsWindow.Create(wm);           // 只作 `CampaignTab._win`（`ClickNodeForTest` 读 `_win.Manager`）
            tabGo = new GameObject("B108A6_CampaignTab");
            var tab = tabGo.AddComponent<CampaignTab>();
            tab.SetHost(hostHolder.Win, tabGo.transform);

            int node = -1;
            for (int i = 0; i < CampaignData.NodeCount; i++) if (CampaignData.Claimable(i)) { node = i; break; }
            Tr(node >= 0, "B0-前提②：至少有一个**可领**的战役节点（`CampaignData.Claimable(i)`）；一个都没有 ⇒ 下面点不开窗、全是空断");
            if (node < 0) return;

            int live0 = LiveCampaignRewardWindows();

            Tr(tab.ClickNodeForTest(node), "B1-前提③：点节点 " + node + " ⇒ 开出了奖励窗");
            first = OpenCampaignRewardWindow(wm, preexisting);
            Tr(first != null, "B1-前提④：在 `openWindows` 里找得到那扇战役奖励窗");
            if (first == null) return;
            Tr(first.CurrentState == WindowState.Open,
               "B1-前提⑤：它当前是 `Open` 态（⚠️ `Close()` 只 `SetActive(false)` ⇒ 「开没开」不能拿 `activeSelf` 判，要读 `CurrentState`）");
            C(LiveCampaignRewardWindows() - live0, 1,
               "B2-前提⑥：第一次点节点之后，**非 `Closed`** 的战役奖励窗比基线多 1（基线 " + live0 + "）");

            Tr(tab.ClickNodeForTest(node), "B3 再点同一个节点（原版 `OnNodeClicked` 走 `OpenWindow<CampaignRewardsWindow>` ⇒ 先命中缓存复用）");
            C(LiveCampaignRewardWindows() - live0, 1,
               "B4 ★★ #51 第二次点节点**没有**再叠一具（非 `Closed` 的战役奖励窗仍只比基线多 1）"
             + "｜🧨 把 `Shell/CampaignTab.cs` 的 `ClickNodeForTest` 改回每次 `CampaignRewardWindow.Create` ⇒ "
             + "第一扇被打成 `Background`（仍不是 `Closed`）⇒ 变成 2 ⇒ 红。"
             + "判据 = 原版 `out_cpp2il_dev_ilspy/WindowsManager.cs:1298-1304`（`automaticallyLoadedWindows.TryGetValue` 命中 ⇒ 复用同一扇）");
            Tr(ReferenceEquals(OpenCampaignRewardWindow(wm, preexisting), first),
               "B5 ★ #51 复用的**就是第一次那一个实例**（`ReferenceEquals`；⛔ 不是另开一具碰巧同状态）");

            // ---- 关掉之后那一具要被收割（原版 `CloseWindowCO`：`Destroy` + 删缓存条目 ⇒ 再点节点是**新实例**）----
            first.Close();
            Tr(tab.ClickNodeForTest(node), "B6 关掉之后再点一次 ⇒ 又开出来一扇");
            Tr((object)first == null || !wm.openWindows.Contains(first),
               "B7 ★ #51 关掉的那一具在**下一次开窗**已不在册（销毁，或被登记表移出；⛔ 旧写法「还活着且仍在册」⇒ 红）"
             + "｜🧨 删掉 `GameWindow.Close` 尾句的 `QueueDestroyOnClose`（或 `WindowsManager.ReapClosedWindows`）⇒ 那具还在册 ⇒ 红"
             + "｜⚠️ 同样必须 `(object)` 转：`first == null` 在「已销毁」与「真置空」两种情况下都为真");
            second = OpenCampaignRewardWindow(wm, preexisting);
            Tr(second != null, "B8-前提⑦：收割之后场上有新的一扇（能读到它）");
            Tr(second != null && !ReferenceEquals(second, first),
               "B9 ★ #51 关过之后开出来的是**新的一扇**（原版 `CloseWindowCO:1623-1628` 把缓存条目一起删了 ⇒ 下一次是新建）");
            C(LiveCampaignRewardWindows() - live0, 1,
               "B10 ★ 收割 + 新建之后，非 `Closed` 的战役奖励窗仍只比基线多 1（**不随点节点次数增长**）");
        }
        finally
        {
            // 收掉**本次新开**的战役奖励窗（先 `Close()` 让它走正常记账，再销毁物体；⛔ 不动 `preexisting` 里宿主自己的窗）
            var leftovers = new System.Collections.Generic.List<GameWindow>(wm.openWindows);
            foreach (var gw in leftovers)
            {
                if (gw == null || preexisting.Contains(gw)) continue;
                var c = gw as CampaignRewardWindow;
                if (c == null) continue;
                try { c.Close(); } catch { }
                if (c != null) UnityEngine.Object.DestroyImmediate(c.gameObject);
            }
            foreach (var c in new CampaignRewardWindow[] { first, second })
                if (c != null) UnityEngine.Object.DestroyImmediate(c.gameObject);
            if (tabGo != null) UnityEngine.Object.DestroyImmediate(tabGo);
            if (hostHolder.Win != null) UnityEngine.Object.DestroyImmediate(hostHolder.Win.gameObject);

            wm.currentWindow = saveCurrent;
            wm.popUpWindow = savePopUp;

            // 左栏命中区那一档是被 `OpenWindow` 里的 `MainMenuRuntime.RefreshNavHitForTopWindow()` 改的
            // ⇒ 窗收完之后按**还原后的**状态重算一次（⛔ 别把宿主的 `NavHitQueueForTest` 留在 Blocked）。
            // ⚠️ 它是 `internal`、本仓没有 `InternalsVisibleTo`（同 `Editor/B97W34_Assert.cs` 那条记录）⇒ 只能反射调。
            var navRefresh = typeof(MainMenuRuntime).GetMethod("RefreshNavHitForTopWindow", BindingFlags.NonPublic | BindingFlags.Static);
            if (navRefresh != null) navRefresh.Invoke(null, null);
        }
    }

    /// <summary>只装一份 `RewardsWindow` 引用（`finally` 里要判 Unity 假 null，包一层最省事）。</summary>
    sealed class RewardWindowHostHolder { public RewardsWindow Win; }

    // ------------------------------------------------------------ 读口

    /// <summary>场上**非 `Closed`** 的战役奖励窗条数（`Close()` 只 `SetActive(false)`、对象还在 ⇒ 必须按状态数，⛔ 不能数实例）。</summary>
    static int LiveCampaignRewardWindows()
    {
        var all = UnityEngine.Object.FindObjectsByType<CampaignRewardWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int n = 0;
        for (int i = 0; i < all.Length; i++) if (all[i] != null && all[i].CurrentState != WindowState.Closed) n++;
        return n;
    }

    static CampaignRewardWindow OpenCampaignRewardWindow(WindowsManager wm, System.Collections.Generic.ICollection<GameWindow> exclude = null)
    {
        for (int i = wm.openWindows.Count - 1; i >= 0; i--)   // 🔴 2026-10-31（B108 收尾）：取**最新**那一扇（原来取第一扇 ⇒ 可能拿到宿主预存的旧窗，B7 关错对象）
        {
            var w = wm.openWindows[i] as CampaignRewardWindow;
            if (w != null && w.CurrentState != WindowState.Closed && (exclude == null || !exclude.Contains(w))) return w;
        }
        return null;
    }

    // ------------------------------------------------------------ 反射（只为了摆/还原 static 记账，⛔ 不碰任何生产逻辑）

    static object StaticField(string name)
    {
        var f = typeof(MainMenuRuntime).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        return f != null ? f.GetValue(null) : null;
    }

    static void StaticFieldSet(string name, object v)
    {
        var f = typeof(MainMenuRuntime).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        if (f != null) f.SetValue(null, v);
    }

    static void InstanceField(object o, string name, object v)
    {
        var f = o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(o, v);
    }

    /// <summary>`Window` 是 `public static … { get; private set; }` —— 它的 setter 非公开，只能走反射。</summary>
    static void SetWindow(MainMenuWindow v)
    {
        var p = typeof(MainMenuRuntime).GetProperty("Window", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        var set = p != null ? p.GetSetMethod(true) : null;
        if (set != null) set.Invoke(null, new object[] { v });
    }
}
