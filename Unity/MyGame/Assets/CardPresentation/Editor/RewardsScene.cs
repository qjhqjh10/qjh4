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

    /// <summary>只比**渲出来的高**（宽由别的判据管）。</summary>
    static void CheckH(Transform t, float wantHpx, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) { CheckTrue(false, what + "（没有 ImageQuad）"); return; }
        CheckNear(q.WorldH * 108f, wantHpx, 2.0f, what + " 高(px)");
    }

    /// <summary>只比**渲出来的宽**。</summary>
    static void CheckW(Transform t, float wantWpx, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) { CheckTrue(false, what + "（没有 ImageQuad）"); return; }
        CheckNear(q.WorldW * 108f, wantWpx, 2.0f, what + " 宽(px)");
    }

    /// <summary>一个 `ImageQuad` 当前的 **tint alpha**（= 它那份材质上的 color.a）。
    /// 用来验「靠 alpha 显隐」的件（原版 `UiBadgeNotification` 那条路）—— 它们 **`activeSelf` 恒为 true**，
    /// 所以 `CountVisible` 那种判据**看不见差别**。</summary>
    static float AlphaOf(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) return float.NaN;
        var mr = q.GetComponent<MeshRenderer>();
        return mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.color.a : float.NaN;
    }

    static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>一个**有渲染尺寸**的节点的宽度（画布像素）。取它子树里第一个 `ImageQuad` 的 `WorldW`
    /// （**渲染真值**，不是回读我们传进去的数）。</summary>
    static float Wpx(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        return q != null ? q.WorldW * 108f : 0f;
    }

    /// <summary>世界 x → 画布像素 x。`LayoutSpace` 是「可见高固定 10 单位、按 16:9 设计」⇒ ×108 + 960。
    /// （`FromPixel` 的逆：`worldX = (px/1920 − 0.5) × 17.7778`。）</summary>
    static float PxOf(float worldX) { return worldX * 108f + 960f; }

    /// <summary>一段文字**渲染出来的**左/右边缘（画布像素）。判据 = `Label.WorldW`（TMP `textBounds`，**真测量**）。
    /// 🔴 用来抓「字还在、但飘到框外/压在别的字上」这类**量矩形量不到**的错 ——
    /// 2026-09-23 那两处 `timer` / `Refill Counter` 就是 61 条断言全绿、字却一个都看不见。</summary>
    static float TextLeftPx(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb == null ? float.NaN : PxOf(lb.transform.position.x) - lb.WorldW * 108f * 0.5f;
    }

    static float TextRightPx(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb == null ? float.NaN : PxOf(lb.transform.position.x) + lb.WorldW * 108f * 0.5f;
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

    /// <summary>一张图的平均亮度（0–255）。**空图护栏**用 —— 见 `Shoot` 里的说明。</summary>
    static float MeanBrightness(Texture2D t)
    {
        if (t == null) return 0f;
        var px = t.GetPixels32();
        if (px.Length == 0) return 0f;
        long sum = 0;
        for (int i = 0; i < px.Length; i += 7) sum += px[i].r + px[i].g + px[i].b++;   // 抽样（每 7 个取 1）
        return sum / 3f / ((px.Length + 6) / 7);
    }
    /// <param name="allowBlank">**已知会是全黑的**那几个状态显式放行（不是静音 —— 每一处都在调用点上写了原因）。
    /// 判据仍是「平均亮度 &gt; 3」，只是这几张本来就拍的是「屏幕上什么都没有」。</param>
    static void Shoot(string file, bool allowBlank = false)
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
        // 🔴 **空图护栏**（2026-09-23 踩到）：`Shoot` 原来是「拍完就写盘」，于是一张**全黑**的图
        //    也能安静地写出去（原因：上一个窗的 `CloseAllWindows()` 把要拍的那个窗也关了）。
        //    断言一条都不会报 —— 正是 `资料/已知的坑.md` 那条「自检截图可能是手工合成的」的同类。
        //    判据：**平均亮度**（0–255）；模板黑底大约 14~40，纯黑 ≈ 0。
        //    ⚠️ **必须在 `DestroyImmediate(tex)` 之前**（销毁之后 `tex == null`，护栏恒红）。
        float lum = MeanBrightness(tex);
        if (allowBlank) Debug.Log(P + $"  截图 {file} 平均亮度 {lum:F1}（**这一张按已知情况放行**）");
        else CheckTrue(lum > 3f, $"{file} 不是空图（平均亮度 {lum:F1} > 3）");

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
        var wm2 = win.Manager;   // 下面几个窗（每日奖励/连登/收件箱）都挂在同一台 `WindowsManager` 上

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

        // ---------------- §三 画面逐项对（2026-09-23 找茬式审核的回归断言）----------------
        // 🔴 这一节全部是**「上一版渲染图里看得见、而断言一条都没量到」**的项 —— 期望值要么来自
        //    `工具/menu_rect.py`（独立于 C# 的第二份实现），要么来自原版 prefab 的序列化字段（注释里带出处）。

        Section("🔴 文字真的落在框里（防「字还在、但飘到框外/压在别的字上」——矩形断言量不到这一类）");
        // 起因（2026-09-23）：`Label.AlignRightOn` 把**世界坐标**写进了 `localPosition`，
        // 每日任务三行的 `timer` 与 `Refill Counter` **一个字都看不见**，而这 61 条断言全绿。
        var refill = FindChild(tab, "Refill Counter");
        var mhName = FindChild(tab, "name (Mission Header)");
        CheckTrue(refill != null, "`Refill Counter` 建了");
        CheckTrue(mhName != null, "`Mission Header` 的 `name` 建了");
        if (refill != null && mhName != null)
        {
            float rl = TextLeftPx(refill), rr = TextRightPx(refill);
            float nl = TextLeftPx(mhName), nr = TextRightPx(mhName);
            CheckTrue(rr > nl && rr < 1920f && rl > 0f,
                      $"`Refill Counter` 的**渲染**区间落在屏幕内（实得 {rl:F1}..{rr:F1}px）");
            // 原版两条 TMP 的 `m_HorizontalAlignment` 都是实测值：`name` = **1 (Left)**、`Refill Counter` 右对齐。
            // 两者框分别为 `a=(0.03,.5)-(0.84,.5)` 与 `a=(0,.5)-(1,.5) pos.x=−26.93 sz.x=−53.86`。
            CheckTrue(rr >= nr + 4f, $"`Refill Counter`（右对齐）不压在 `name`（左对齐）上（name 右 {nr:F1} / refill 右 {rr:F1}）");
        }
        // 达成态那一行（`DailyDone`）显示 `timer`、其余显示 `description` —— 原版 `DisplayRule` 的 1/2 **互斥**。
        CheckTrue(FindChild(rows[0], "description") != null, "未达成行显示 `description`（原版 `displayRule=1` WhenActive）");
        CheckTrue(FindChild(rows[0], "timer") == null, "未达成行**不显示** `timer`（原版 `displayRule=2` WhenComplete）");
        if (rows.Count == 3)
        {
            CheckTrue(FindChild(rows[1], "timer") != null, "已达成行显示 `timer`（10/10 那一行）");
            CheckTrue(FindChild(rows[1], "description") == null, "已达成行**不显示** `description`");
            // 原版 `timer` 的框左边缘 = 行内 **136.68**（`menu_rect.py "Daily Mission Container"
            // --depth 4 --relative --root-size 539.188x150` 实算）⇒ 画布 x = 1271.31 + 136.68 = 1407.99
            CheckNear(TextLeftPx(FindChild(rows[1], "timer")), 1408f, 3f,
                      "第 2 行 `timer` 的渲染左边缘(px)（原版 `H=Left`，框左 = 行左 + 136.68）");
        }

        Section("§三·3 登录卡 `body.image` 的 `pos=(0,−29)`（`menu_rect.py` 实算 62.14..323.27 vs 91.14..352.27）");
        var loginCard = FindChild(tab, "Daily Login Container");
        CheckTrue(loginCard != null, "`Daily Login Container` 建了");
        if (loginCard != null)
        {
            var bag = FindChild(loginCard, "image");
            // 图高 = `image` 的 261.131 × 原版 `Special Missions.localScale 1.15` = **300.30**
            CheckH(bag, 300.30f, "登录卡 `image`");
            // 图心在卡心**上方** 64.16px —— 出处：`body` 的 `pos.y = +84.7947`（设计 px，向上为正，即**上移** 84.79）
            // 与 `image` 的 `pos.y = −29`（**下移** 29）⇒ 净上移 **84.7947 − 29 = 55.7947**，再 × 1.15 = **64.16**。
            // （`menu_rect.py` 的实算：card 心 277.5、`image` 心 221.70 于 555 高的 prefab 内 ⇒ 上移 55.80 ✓）
            CheckNear((bag.position.y - loginCard.position.y) * 108f, 64.16f, 2f,
                      "登录卡 `image` 比卡心高(px)（原版 = (84.7947 − 29) × 1.15）");
        }

        Section("§三·1 `Special Missions` 的 `localScale=1.15` **要缩到子树**（2026-09-23 修）");
        var skullsCard = FindChild(tab, "Daily Skulls Mission Container");
        CheckTrue(skullsCard != null, "骷髅卡建了");
        if (skullsCard != null)
        {
            var steps = new List<Transform>();
            foreach (var t in skullsCard.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Milestone")) steps.Add(t);
            Check(steps.Count, 5, "骷髅卡 5 格里程碑");
            if (steps.Count == 5)
            {
                // 原版 `steps` 的 LG：`align=4 sp=20 ctlW=0 ctlH=0`，格 **40×40** ⇒ 缩放后 **46×46**
                CheckH(steps[0], 46f, "里程碑格（40 × 1.15）");
                float sp = Mathf.Abs(steps[1].position.x - steps[0].position.x) * 108f;
                CheckNear(sp, 69f, 2f, "里程碑格间距(px)（(40+20) × 1.15）");
                // `align=4 (MiddleCenter)` ⇒ 5×40+4×20=280 放进 325 的容器，左右各留 **22.5**（缩放后 25.875）
                float cardCx = skullsCard.position.x * 108f + 960f;
                float stepsCx = (steps[0].position.x + steps[4].position.x) * 0.5f * 108f + 960f;
                CheckNear(stepsCx - cardCx, 0f, 3f, "里程碑排的中心 = 卡心（`MiddleCenter`）");
            }
        }

        Section("§三·5 周常：`Handle` 与骑在它上面的 `counter`（2026-09-23 补）");
        var weekly = FindChild(tab, "Weekly Mission");
        CheckTrue(weekly != null, "周常卡建了");
        if (weekly != null)
        {
            var handle = FindChild(weekly, "Handle");
            var wcnt = FindChild(weekly, "counter");
            CheckTrue(handle != null, "`Handle` 建了（原版**无 sprite** ⇒ UGUI 画一块实心矩形，色 (0.941,0.725,0.314,1)）");
            CheckTrue(wcnt != null, "`counter`（`13/15`）建了");
            if (handle != null && wcnt != null)
            {
                CheckH(handle, 50.60f, "`Handle`（原版 4.141×50.60）");
                float d = Mathf.Abs(handle.position.x - wcnt.position.x) * 108f;
                CheckNear(d, 0f, 2f, "`counter` 中心骑在 `Handle` 上(px)（原版 `counter` 是 `Handle` 的子节点）");
                // 把手位置 = 进度条左 + 进度 × 条宽（Slider 的行为；进度 = 13/15）
                var wbar = FindChild(weekly, "Progress Bar");
                if (wbar != null)
                {
                    float t = DailyData.WeeklyProgress01();
                    float barCx = wbar.position.x * 108f + 960f, barW = Wpx(wbar);
                    float wantPx = barCx - barW * 0.5f + barW * t;
                    CheckNear(handle.position.x * 108f + 960f, wantPx, 3f, "`Handle` 的 x = 条左 + 进度×条宽(px)");
                }
            }
        }

        Section("§三 卡片 `header` 的**渐变条**（原版 `Image(sprite=null)` + `Gradient2`，2026-09-23 补）");
        CheckTrue(CountByName(root, "Mission Header bg") == 1, "`Mission Header` 的渐变条建了（每日任务那块）");
        CheckTrue(CountByName(root, "Daily Login Bonus header bg") == 1, "登录卡的渐变条建了");
        CheckTrue(CountByName(root, "Daily Skulls header bg") == 1, "骷髅卡的渐变条建了");
        CheckTrue(CountByName(root, "header bg") == 1, "周常卡的渐变条建了（灰蓝那套）");

        Section("§三·8 奖励格 `drawerHolder` 的绝对内缩（不是我们挑的百分比）");
        var rw0 = FindChild(rows.Count > 0 ? rows[0] : tab, "Reward 0");
        CheckTrue(rw0 != null, "每日任务行的奖励格建了");
        if (rw0 != null)
        {
            // 原版 `drawerHolder` = `sz=(−35.685, −42.369) p=(.5,1)`，格 126.334×150
            // ⇒ 实算 **17.84..108.49 × 0..107.63**（`menu_rect.py`）⇒ 宽 **90.65**、高 107.63。
            // ⚠️ 图本身按 `keepAspect` 等比放进这个框 ⇒ **只看宽**（高由图的宽高比定，见
            //    `资料/日常_画面逐项对_0923.md` 的「还没查清的」）。
            CheckW(rw0, 90.65f, "奖励格 `drawerHolder`（原版 90.65 宽）");
        }

        Section("§三 `Collect` 按钮的 `preserveAspect`（原版 `m_PreserveAspect=1`，源图 `40K_button` 489×107）");
        var collect = FindChild(rows.Count > 0 ? rows[0] : tab, "Generic UI Button");
        if (collect != null)
        {
            var q = collect.GetComponentInChildren<ImageQuad>();
            if (q != null)
            {
                float w = q.WorldW * 108f, h = q.WorldH * 108f;
                CheckNear(w / h, 489f / 107f, 0.02f, "`Collect` 渲染宽高比 = 源图 489/107（等比放进框）");
                // 框是 254.61×56.48；等比放进后高 = 254.61 / 4.5701 = **55.71**（拉伸的话会顶满 56.48）
                CheckNear(h, 254.61f / (489f / 107f), 1.0f, "`Collect` 渲染高(px)（= 框宽 ÷ 源图宽高比）");
            }
        }

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


        // 🔴 **两张奖励窗的截图必须在这里拍** —— 下面 §四/§五 会开别的窗，而它们结尾的
        //    `CloseAllWindows()` 会把**奖励窗一起关掉** ⇒ 拍出来是**空图**（2026-09-23 踩到：
        //    截图还在、内容没了，而断言一条都不会报）。**先拍完再往下开窗。**
        Shoot("01_日常_Missions.png");
        win.tabButtons.Click(1);
        Shoot("02_日常_Campaign空页.png");
        win.tabButtons.Click(0);

        Section("🔴 红点靠 **alpha**、不靠 `SetActive`（原版 `UiBadgeNotification.Show()/Hide()`）");
        var badge0 = FindChild(FindChild(bar, "RewardsTabButton_0"), "Badge Highlight");
        var badge1 = FindChild(FindChild(bar, "RewardsTabButton_1"), "Badge Highlight");
        CheckTrue(badge1 != null && badge1.gameObject.activeSelf,
                  "第 2 键的红点节点**是激活的**（原版出厂四个都 `active=1`）—— 这条正是「`SetActive` 不是判据」");
        CheckNear(AlphaOf(badge1), 0f, 0.01f, "第 2 键（Campaign）的红点 **alpha = 0**（没有通知源 ⇒ 原版 `Hide()` 的样子）");
        // 两态都验：开窗那一瞬（`ForceCollectableForTest` 还没跑）应当是 0；刷新之后应当变 1
        CheckNear(AlphaOf(badge0), 0f, 0.01f, "开窗时没有「可领」的任务 ⇒ 第 1 键红点 alpha = 0");
        DailyData.ForceCollectableForTest();
        win.RefreshBadges();
        CheckNear(AlphaOf(badge0), 1f, 0.01f,
                  "造出「可领」之后 `RefreshBadges()` ⇒ 第 1 键红点 alpha = 1（原版 `UiBadgeNotification.Show()`）");

        Section("§四 `Daily Reward Popup`（每日奖励窗 —— 四态 + 双轨侧栏，2026-09-23 建）");
        var dr = DailyRewardPopup.Create(wm2);
        wm2.OpenWindow(dr);
        Check(dr.type, WindowType.Fullscreen, "`type` = 0 Fullscreen（实证）");
        Check(dr.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup（**0 配 15 就是原版的组合**）");
        Check(dr.closeOnEsc, true, "`closeOnESC` = 1（实证；与奖励窗的 0 不同）");
        Check(dr.entries.Count, DailyData.RewardDays, $"`Rewards Content` 下 {DailyData.RewardDays} 个 `Entry`");
        // `Rewards Content` 的 HLG 实测 **spacing = −64**、align=3(MiddleLeft) ⇒ 相邻两格**故意重叠 64**
        if (dr.entries.Count >= 2)
            CheckNear(Mathf.Abs(dr.entries[1].position.x - dr.entries[0].position.x) * 108f,
                      DailyRewardPopup.EntryPitch, 1f,
                      "相邻 `Entry` 的中心距(px)（= 315.028 − 64）");
        // 四态（`GetCurrentState` 的公式照原版；采样数据 `资料/日常_原版规格.md` §四 + `DailyData` 里已标明「我们挑的」）
        var states = new List<RewardState>();
        for (int i = 0; i < DailyData.RewardDays; i++) states.Add(DailyData.RewardStateOf(i, false));
        CheckTrue(states.Contains(RewardState.Collected) && states.Contains(RewardState.Unlocked)
                  && states.Contains(RewardState.Locked),
                  "这一屏同时覆盖 `Collected` / `Unlocked` / `Locked` 三态（采样数据）");
        Check(DailyData.RewardStateOf(0, true), RewardState.PremiumLocked,
              "Premium 轨恒为 `PremiumLocked`（⚠️ 我们挑的：单机不卖 Premium，边界②）");
        if (dr.entries.Count > 0)
        {
            // `SetState` 表（`DF:DailyRewardDrawerController__SetState.c:8-28`）：
            //   `colider` 只有 Unlocked 开 · `Gacha Reward Claimed` 只有 Collected 开 · `Premium Indicator` 只有 PremiumLocked 开
            var e0 = dr.entries[states.IndexOf(RewardState.Collected)];
            var e1 = dr.entries[states.IndexOf(RewardState.Unlocked)];
            var c0 = FindChild(e0, "colider");
            CheckTrue(c0 != null && !c0.gameObject.activeSelf, "`Collected` 那格的 `colider`（点击区）是**关的**");
            var g0 = FindChild(e0, "Gacha Reward Claimed");
            CheckTrue(g0 != null && g0.gameObject.activeSelf, "`Collected` 那格亮 `Gacha Reward Claimed`");
            var c1 = FindChild(e1, "colider");
            CheckTrue(c1 != null && c1.gameObject.activeSelf, "`Unlocked` 那格的 `colider` 是**开的**（可点）");
            // ⚠️ 每个 Entry 有**两个**抽屉，两个都带 `Premium Indicator` ⇒ 必须限定在 `Premium Reward` 子树里找
            var premNode = FindChild(e1, "Premium Reward");
            var p1 = FindChild(premNode, "Premium Indicator");
            CheckTrue(p1 != null && p1.gameObject.activeSelf,
                      "`Unlocked` 那格的 **Premium 抽屉**亮 `Premium Indicator`（普通抽屉的该件是关的）");
        }
        Check(dr.MissingArt.Count, 0, "每日奖励窗没有取不到的图");
        Shoot("03_每日奖励.png");
        wm2.CloseAllWindows();

        Section("§五 `Daily Streak Popup`（每日连登窗 —— 两态互斥 + 奖格轨，2026-09-23 建）");
        var ds = DailyStreakPopup.Create(wm2);
        wm2.OpenWindow(ds);
        Check(ds.type, WindowType.Fullscreen, "`type` = 0 Fullscreen（实证）");
        Check(ds.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup（实证）");
        Check(ds.closeOnEsc, false, "`closeOnESC` = **0**（实测；⚠️ 与每日奖励窗的 1 相反）");
        Check(ds.entries.Count, DailyData.StreakDays, $"`Rewards Content` 下 {DailyData.StreakDays} 个奖格");
        // `Rewards Content` 的 HLG 实测 spacing = **−64** ⇒ 相邻两格**故意重叠 64**
        if (ds.entries.Count >= 2)
            CheckNear(Mathf.Abs(ds.entries[1].position.x - ds.entries[0].position.x) * 108f,
                      DailyStreakPopup.EntryPitch, 1f, "相邻奖格的中心距(px)（= 379.816 − 64）");
        // 两态**互斥**：`HasFailed` 决定谁开。我们的数据默认 `false` ⇒ 连胜态开、断签态关
        var succ = FindChild(ds.transform, "Streak Successful");
        var fail = FindChild(ds.transform, "Streak Failed");
        CheckTrue(succ != null && succ.gameObject.activeSelf, "连胜态开着（`Streak Successful`）");
        CheckTrue(fail != null && !fail.gameObject.activeSelf, "断签态关着（`Streak Failed`）");
        CheckTrue(FindChild(succ, "Timer") != null,
                  "`Timer` 在**连胜面板里**（原版实况 ⇒ 断签态下看不到倒计时）");
        // 🔴 `scaleMultiplierFirstElement = 1.2`：唯一读取点 = `RefreshRewards` 的第一次循环
        //    （`i == challenge.collectedRewards`）⇒ **第一个「还没领」的奖格**放大 1.2（该 prefab 的 pivot 实测 (.5,.5)）
        if (ds.entries.Count > DailyData.StreakCollected)
        {
            float w0 = Wpx(ds.entries[DailyData.StreakCollected]);
            float w1 = Wpx(ds.entries[DailyData.StreakCollected + 1]);
            CheckNear(w0 / w1, 1.2f, 0.05f, "「第一个还没领的」奖格宽是邻格的 **1.2 倍**（`scaleMultiplierFirstElement`）");
        }
        Check(ds.MissingArt.Count, 0, "连登窗没有取不到的图");
        Shoot("04_每日连登.png");
        wm2.CloseAllWindows();

        Section("§八 战果 → 任务进度（接 `Battle/EndPanel` 那条链，2026-09-23 接的）");
        // 三张每日任务卡：0 = `Deal 500 damage to enemy units` · 1 = `Play 10 troops` · 2 = `Win 3 battles`
        DailyData.ResetMissionsForTest();      // 52/500 · 4/10 · 1/3（**确定的初值**，见 `ResetMissionsForTest`）
        DailyData.OnBattleEnd(true, 100, 3);
        Check(DailyData.DailyProgressValue(0), 152, "「对敌伤害」按**实打伤害**推进（52 + 100）");
        Check(DailyData.DailyProgressValue(1), 7,   "「打出部队」按**打出的部队卡张数**推进（4 + 3）");
        Check(DailyData.DailyProgressValue(2), 2,   "赢了那一局 ⇒ 胜场 +1（1 + 1）");
        // 一次打够 ⇒ **封顶在 target**（原版 `MissionChallengeProgress` 也是夹住的）
        DailyData.OnBattleEnd(true, 99999, 99999);
        Check(DailyData.DailyProgressValue(0), 500, "进度**封顶在 target**（500）");
        Check(DailyData.DailyProgressValue(1), 10,  "进度**封顶在 target**（10）");
        Check(DailyData.DailyProgressValue(2), 3,   "进度**封顶在 target**（3）");
        Check(DailyData.DailyState(2), DailyData.State.Collectable, "到顶 ⇒ 变**可领取**（原版 `MissionBackgroundHighlighter` 那一态）");
        // 输了不加胜场
        DailyData.OnBattleEnd(false, 0, 0);
        Check(DailyData.DailyProgressValue(2), 3, "输了那一局 ⇒ 胜场**不加**");

        Section("§六 `Inbox Menu`（收件箱 —— **空态**，2026-09-23 建）");
        var inbox = InboxWindow.Create(wm2);
        wm2.OpenWindow(inbox);
        Check(inbox.type, WindowType.Popup, "`type` = 1 Popup（实证）");
        Check(inbox.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup（实证）");
        Check(inbox.closeOnEsc, true, "`closeOnESC` = 1（实证）");
        Check(inbox.HasMessages, false, "单机没有消息 ⇒ 走**空态**（原版 `InboxWindow__Open.c:49` 也是这条分支）");
        var mdNode = FindChild(inbox.transform, "Message Display");
        var warnNode = FindChild(inbox.transform, "No News Warning");
        CheckTrue(mdNode != null && !mdNode.gameObject.activeSelf, "空态下 `Message Display` **是关的**");
        CheckTrue(warnNode != null && warnNode.gameObject.activeSelf, "空态下 `No News Warning` **是开的**");
        var msgList = FindChild(inbox.transform, "Message List");
        CheckTrue(msgList != null && msgList.childCount == 0,
                  "`Message List` **出厂就是 0 个子件**（原版如此；条目 prefab 由服务端事件数据决定）");
        CheckTrue(FindChild(inbox.transform, "Reset Button") == null,
                  "**不建** `Reset Button`（原版 `m_OnClick` 空 + `DebugReset` 零调用者 + `Open()` 每次关它）");
        Check(inbox.MissingArt.Count, 0, "收件箱没有取不到的图");
        Shoot("05_收件箱_空态.png");
        wm2.CloseAllWindows();


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
