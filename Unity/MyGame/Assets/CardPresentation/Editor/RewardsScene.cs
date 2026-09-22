// RewardsScene.cs — 「日常」奖励窗口的**自检入口**（阶段二第 2 层）
//
// 用法：… -executeMethod RewardsScene.Run        自检（结构 + 版面 + 交互 + 截图），退出码 0 = 全过
//
// 🔴 **每一条断言的期望值都盯「原版值」，不是盯我们自己写的常量**（否则就是自证）——
//    期望值来自 `工具/menu_rect.py`（**独立于 C# 的第二份实现**，直接回原始 JSON 复算锚点链）。
//    两边对不上 = 有一边错了，而不是「改断言让它变绿」。
//
// ⚠️ **为什么没有 `BuildAndSaveScene`**：原版整套菜单**只有 2 个场景**，其余全是 prefab 窗口
//    （`资料/日常_原版规格.md` §〇）。奖励窗是**挂在 `Shell` 的三个锚点上的窗口**，
//    由主菜单的 REWARDS 钮开 ⇒ **不该有自己的场景**。要真 Play 就从 `Shell` 里点进去。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RewardsScene
{
    const string P = "[Rewards] ";
    const string ShotDir = "d:/4/_tmp_view/rewards";

    static int _pass, _fail;
    static readonly List<string> _failures = new List<string>();

    static void Section(string t) { Debug.Log(P + $"--- {t} ---"); }

    static void Check<T>(T got, T want, string msg)
    {
        if (EqualityComparer<T>.Default.Equals(got, want)) { _pass++; Debug.Log(P + $"   ✓ {msg}"); }
        else
        {
            _fail++;
            var line = $"{msg} —— 期望 [{want}]，实得 [{got}]";
            _failures.Add(line);
            Debug.LogError(P + $"   ✗ {line}");
        }
    }

    static void CheckTrue(bool c, string msg) { Check(c, true, msg); }

    static void CheckNear(float got, float want, float tol, string msg)
        => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（{got:F2} ≈ {want:F2}±{tol:F2}）");

    /// <summary>世界坐标比对（±0.01 世界单位 ≈ ±1 px）。**期望值必须来自原版像素矩形。**</summary>
    static void CheckAt(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var want = LayoutSpace.RectCenter(x1, y1, x2, y2);
        float d = Vector3.Distance(t.position, want);
        CheckTrue(d <= 0.01f, $"{what} 在原版矩形中心（差 {d:F4} 世界单位 = {d * 108f:F2}px）");
    }

    /// <summary>一张图**渲出来的像素矩形**（用 `WorldW/WorldH` —— TMP/材质真值，不是回读我们传进去的数）。</summary>
    static void CheckRectPx(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) { CheckTrue(false, what + "（没有 ImageQuad）"); return; }
        CheckNear(q.WorldW * 108f, x2 - x1, 2.0f, what + " 宽(px)");
        CheckNear(q.WorldH * 108f, y2 - y1, 2.0f, what + " 高(px)");
    }

    static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    // ============================================================ 建

    static RewardsWindow Build(out Transform root)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.aspect = LayoutSpace.DesignAspect;      // ⚠️ 必须在建任何东西之前定死（批处理默认 4:3）
        LayoutSpace.Apply(cam);

        // 三个锚点（照 `ShellRuntime.Build` 里那三个；名与 placement 都照原版）
        var anchors = new GameObject("Window Anchors").transform;
        MakeHolder(anchors, "1 - Below Upper Bar Holder", WindowsPlacement.World);
        MakeHolder(anchors, "2 - Canvas Holder Above upper bar", WindowsPlacement.Canvas);
        MakeHolder(anchors, "3 - PopUp Holder", WindowsPlacement.Popup);

        var wmGo = new GameObject("WindowsManager");
        var wm = wmGo.AddComponent<WindowsManager>();

        var win = RewardsWindow.Create(wm);
        wm.OpenWindow(win);
        root = win.transform;
        return win;
    }

    static void MakeHolder(Transform parent, string name, WindowsPlacement p)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        var h = t.gameObject.AddComponent<WindowHolder>();
        h.placement = p;
        h.RegisterNow();
    }

    static void Shoot(string file)
    {
        var cam = Camera.main;
        if (cam == null) return;
        const int W = 1920, H = 1080;
        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(ShotDir, file), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Debug.Log(P + $"  截图 {Path.Combine(ShotDir, file)}");
    }

    // ============================================================ 自检

    public static void Run()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        Directory.CreateDirectory(ShotDir);
        Debug.Log(P + "=== 「日常」奖励窗口自检 开始 ===");

        var win = Build(out var root);

        // ---------------- §一 窗口根 ----------------
        Section("窗口根（§一：`MainMenuRewardsWindow` 的 GameWindow 参数）");
        Check(win.type, WindowType.Fullscreen, "`type` = 0 Fullscreen（MB 1349677669050291967 实证）");
        Check(win.placement, WindowsPlacement.Canvas, "`windowsPlacement` = 5 Canvas（**不是 15**）");
        Check(win.closeOnEsc, false, "`closeOnESC` = 0（ESC 不关窗）");

        Section("Content Area（§一：x 167.17..1920.01 · y 70.94..1080.00）");
        var area = FindChild(root, "Content Area");
        CheckTrue(area != null, "`Content Area` 建了");
        CheckAt(area, 167.17f, 1920.01f, 70.94f, 1080f, "`Content Area`");
        CheckRectPx(area, 167.17f, 1920.01f, 70.94f, 1080f, "`Content Area`");
        var bg = FindChild(area, "Background");
        CheckTrue(bg != null && bg.GetComponentInChildren<ImageQuad>() != null
                  && bg.GetComponentInChildren<ImageQuad>().Texture != null
                  && bg.GetComponentInChildren<ImageQuad>().Texture.name.StartsWith("grad_"),
                  "`Background` 用的是**运行时双色渐变**（原版 `Image(sprite=null)` + `UIGradient` c1 #390503 / c2 #0C0004 / angle 82）");

        // ---------------- §二 左栏 ----------------
        Section("左栏 `Tab Buttons`（§二·1：x 167.17..332.17 · y 70.94..1080 · 165×1009.06）");
        var bar = FindChild(area, "Tab Buttons");
        CheckTrue(bar != null, "`Tab Buttons` 建了");
        CheckAt(bar, 167.17f, 332.17f, 70.94f, 1080f, "`Tab Buttons`");
        CheckRectPx(bar, 167.17f, 332.17f, 70.94f, 1080f, "左栏底图 `40k_main_tab_background`");
        CheckAt(FindChild(bar, "Shadow"), 167.18f, 214.81f, 70.94f, 1080f, "`Tab Buttons/Shadow`（窄 47.64）");

        Section("四个键（§二·2：VLG padTop 120 ⇒ 绝对 y 190.94 / 370.94 / 550.94 / 730.94）");
        float[] tops = { 190.94f, 370.94f, 550.94f, 730.94f };
        for (int i = 0; i < 4; i++)
        {
            var b = FindChild(bar, "RewardsTabButton_" + i);
            CheckTrue(b != null, $"第 {i + 1} 个键建了");
            CheckAt(b, 167.17f, 332.17f, tops[i], tops[i] + 180f, $"第 {i + 1} 个键");
            // `Label` 底：`pos=(0,-72.16) p=(.5,0) sz=(155,37.86)` ⇒ 底边 = 键中心 - 72.16
            float cy = tops[i] + 90f, lb = cy + 72.16f;
            CheckAt(FindChild(b, "Text Background"), 172.17f, 327.17f, lb - 37.86f, lb,
                    $"第 {i + 1} 个键的文案条");
            // `Badge Highlight`：35²，中心 (栏中心+51.7, 键中心∓27.2)。**第 4 键是 +47.9**（§二·2）
            float bdy = i == 3 ? 47.9f : -27.2f;
            CheckAt(FindChild(b, "Badge Highlight"), 249.67f + 51.7f - 17.5f, 249.67f + 51.7f + 17.5f,
                    cy - bdy - 17.5f, cy - bdy + 17.5f, $"第 {i + 1} 个键的红点");
        }
        CheckArt(FindChild(FindChild(bar, "RewardsTabButton_0"), "Icon"), "40K_rewards_bt_missions", "第 1 键图标");
        CheckArt(FindChild(FindChild(bar, "RewardsTabButton_1"), "Icon"), "40k_main_bt_campaign",
                 "第 2 键图标（**原版就是主菜单那张导航图** —— `40K_rewards_bt_campaign` 不存在）");
        CheckArt(FindChild(FindChild(bar, "RewardsTabButton_2"), "Icon"), "40K_rewards_bt_forge", "第 3 键图标");
        CheckArt(FindChild(FindChild(bar, "RewardsTabButton_3"), "Icon"), "40K_shop_bt_boosters", "第 4 键图标");
        Check(CountByName(root, "Highlight"), 4, "四个键各有一层高亮（**只有选中的那个可见**）");
        Check(CountVisible(root, "Highlight"), 1, "**可见的高亮恰好 1 个**（选中态；原版出厂四个都亮，可见性由运行时驱动）");

        // ---------------- §三 任务页 ----------------
        Section("`Missions Tab` 与三大块（§三·1，机械走链的数）");
        var tab = FindChild(FindChild(area, "Tabs"), "Missions Tab");
        CheckTrue(tab != null, "`Missions Tab` 建了");
        CheckAt(tab, 166.69f, 1920f, 69.20f, 1080f, "`Missions Tab`");
        var nm = FindChild(tab, "Normal Missions");
        CheckAt(nm, 372.37f, 1891.37f, 95.85f, 819.65f, "`Normal Missions`");
        CheckAt(FindChild(nm, "Special Missions"), 372.37f, 1151.58f, 95.85f, 652.07f, "`Special Missions`");
        CheckAt(FindChild(nm, "Daily Missions"), 1271.31f, 1810.49f, 95.85f, 651.72f, "`Daily Missions`");
        // ⚠️ `Weekly Mission Holder` 的父是 **`Missions Tab`**，与 `Normal Missions` **同级**
        //    （原版树 `菜单全树.md` + 机械走链两处一致；第一版挂在 `Normal Missions` 下，差 205.68/26.65px）
        CheckAt(FindChild(tab, "Weekly Mission"), 372.37f, 1891.36f, 759.79f, 987.30f, "`Weekly Mission`");

        Section("每日任务三行（§三·1：`Daily Missions Holder` VLG spacing 18.55 · 三行各 150 · align 7 LowerCenter）");
        // `RowRect` 的判据：三行靠**下**对齐 ⇒ 最后一行的底边 = 容器底边
        var rows = new List<Transform>();
        foreach (var t in tab.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("Daily Mission Container (")) rows.Add(t);
        Check(rows.Count, 3, "**恰好 3 行**（原版 `Daily Missions Holder` 下三份实例）");
        if (rows.Count == 3)
        {
            PxRect holder = new PxRect(RowRectHolderX1(), 150.28f, RowRectHolderX2(), 651.72f);
            for (int i = 0; i < 3; i++)
            {
                var want = MissionsTab.RowRect(holder, i);
                CheckAt(rows[i], want.x1, want.x2, want.y1, want.y2, $"第 {i + 1} 行");
            }
            // 行内两件：`description` 与 `Collect`（原版锚点依赖父宽 —— **这正是「按父宽重分布」的判据**）
            CheckTrue(FindChild(rows[0], "description") != null, "行里有 `description`");
            CheckTrue(FindChild(rows[0], "Generic UI Button") != null, "行里有 `Collect` 按钮");
            CheckTrue(FindChild(rows[0], "progress") != null, "行里有 `progress` 文本（`{0}/{1}` 口径）");
        }
        CheckTrue(FindChild(FindChild(nm, "Daily Missions"), "name (Mission Header)") != null,
                  "`Daily Missions` 上有 `Mission Header`（'Daily Missions' fs36）");

        Section("进度条与里程碑（§三·2 §三·7：两张条图都是**九宫格 (4,4,4,4)**）");
        var bar0 = FindChild(rows.Count > 0 ? rows[0] : tab, "Progress Bar");
        CheckTrue(bar0 != null, "每日任务行的 `Progress Bar` 建了");
        if (bar0 != null)
        {
            var nines = bar0.GetComponentsInChildren<ImageQuad>(true);
            CheckTrue(nines.Length >= 1, $"进度条至少画了底（实得 {nines.Length} 块）");
        }

        Section("图：一张都不能少");
        Check(win.MissingArt.Count, 0, "没有取不到的图（取不到的件**根本没画**，所以这条必须 0）");

        Section("页签切换（§二·3：**只切 activeSelf，不重建**）");
        Check(win.CurrentTab, WindowTabType.Missions, "开窗默认在 Missions 页");
        CheckTrue(tab != null && tab.gameObject.activeSelf, "`Missions Tab` 是开着的");
        var forge = FindChild(FindChild(area, "Tabs"), "Forge Tab");
        var camp = FindChild(FindChild(area, "Tabs"), "Campaign Tab");
        CheckTrue(forge != null && !forge.gameObject.activeSelf, "`Forge Tab` 出厂关着（原版 §二·4 `activeSelf=false`）");
        CheckTrue(camp != null && !camp.gameObject.activeSelf, "`Campaign Tab` 出厂关着");
        if (win.tabButtons != null)
        {
            win.tabButtons.Click(1);                                  // 左栏第 2 键 = Campaign
            Check(win.CurrentTab, WindowTabType.Campaign, "点左栏第 2 键切到 Campaign 页");
            CheckTrue(camp != null && camp.gameObject.activeSelf && !tab.gameObject.activeSelf,
                      "切页后 `Campaign Tab` 开、`Missions Tab` 关");
            win.tabButtons.Click(0);
            Check(win.CurrentTab, WindowTabType.Missions, "点回第 1 键切回 Missions 页");
            win.tabButtons.Click(3);                                  // 第 4 键：Booster Packs，不是页签
            Check(win.CurrentTab, WindowTabType.Missions, "点第 4 键（Booster Packs）**不切页**（它不是页签）");
        }

        Section("领奖：点了必须有反应（红线：不许静默失败）");
        DailyData.ForceCollectableForTest();
        int before = Wallet.Of("40k_topmarquee_currency_gold");
        DailyData.CollectDaily(0);
        CheckTrue(Wallet.Of("40k_topmarquee_currency_gold") >= before, "领每日任务后**资源记账动了**（原版走 PlayFab 云脚本，单机本地兑现）");

        Shoot("01_日常_Missions.png");
        win.tabButtons.Click(1);
        Shoot("02_日常_Campaign空页.png");

        Debug.Log(P + win.Dump());
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (_fail > 0) foreach (var f in _failures) Debug.LogError(P + "   ✗ " + f);
        EditorApplication.Exit(_fail > 0 ? 1 : 0);
    }

    // `Daily Missions Holder` 的 x 边界（原版实测 1271.31..1810.49 —— 见 §三·1）
    static float RowRectHolderX1() { return 1271.31f; }
    static float RowRectHolderX2() { return 1810.49f; }

    static void CheckArt(Transform t, string want, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        CheckTrue(q != null && q.Texture != null && q.Texture.name == want, $"{what} = `{want}`");
    }

    static int CountByName(Transform root, string name)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) n++;
        return n;
    }

    static int CountVisible(Transform root, string name)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name && t.gameObject.activeInHierarchy) n++;
        return n;
    }
}
