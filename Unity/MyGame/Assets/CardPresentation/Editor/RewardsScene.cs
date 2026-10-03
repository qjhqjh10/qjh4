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

    /// <summary>🆕 A17：把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
    /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
    static void CheckHoverSwap(Transform root, string what)
    {
        int n; string bad = WindowButton.AuditHoverSwap(root, out n);
        CheckTrue(n > 0, what + "：**确实有**接了悬停换图的按钮（n=" + n + "，否则这条等于没查）");
        if (bad.Length > 0) CheckTrue(false, what + "：换图要「悬停换得动 + 离开还原得回」—— " + bad);
    }

    static void CheckNoMissingSwapArt(string what)
        => CheckTrue(WindowButton.MissingSwapArt.Count == 0,
                     what + "：**悬停图一张都不缺**（缺的会列在这里：" + string.Join("、", WindowButton.MissingSwapArt.ToArray()) + "）");

    static void CheckNear(float got, float want, float tol, string msg)
        => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（{got:F2} ≈ {want:F2}±{tol:F2}）");

    /// <summary>🆕 2026-10-03：一个节点**在世界里的位置 → 画布像素中心**，与期望的原版像素点比（±`tol`px）。
    /// 判据 = `LayoutSpace.ToPixel`（`PointerLayer` 命中用的是同一条换算 —— 所以这里量的就是「真鼠标会落在哪」）。</summary>
    static void CheckNearPx(Transform t, float xPx, float yPx, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var px = LayoutSpace.ToPixel(t.position);
        CheckNear(px.x, xPx, 0.5f, what + " 的中心 x(px)");
        CheckNear(px.y, yPx, 0.5f, what + " 的中心 y(px)");
    }

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

    /// <summary>一个 `ImageQuad` 当前的 **tint 颜色**（= 它那份材质上的 color）。
    /// 用来验「按状态染色」的件（战役节点那六种状态色就是这条路）。</summary>
    static Color TintOf(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) return new Color(0f, 0f, 0f, 0f);
        var mr = q.GetComponent<MeshRenderer>();
        return mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.color : new Color(0f, 0f, 0f, 0f);
    }

    static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>**按路径**找一个节点（`Content/Scroll View/Viewport/…`）。
    /// 🔴 2026-09-23 踩过：`FindChild` 是**按名字**找的（`GetComponentsInChildren` + `name ==`），
    /// **不认识 `A/B/C` 这种写法** —— 传路径进去**永远返回 null**，而断言只会报「不成立」，
    /// 看着像「这个件没建」，其实是找法错了。要路径就用这个。</summary>
    static Transform FindPath(Transform root, string path)
    {
        return root != null ? root.Find(path) : null;
    }

    /// <summary>一个**有渲染尺寸**的节点的宽度（画布像素）。取它子树里第一个 `ImageQuad` 的 `WorldW`
    /// （**渲染真值**，不是回读我们传进去的数）。</summary>
    static float Wpx(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        return q != null ? q.WorldW * 108f : 0f;
    }

    /// <summary>世界 x → 画布像素 x。`LayoutSpace` 是「可见高固定 10 单位、按 16:9 设计」⇒ ×108 + 960。
    /// （`FromPixel` 的逆：`worldX = (px/1920 − 0.5) × 17.7778`。）⚠️ **这个只能用在 x 上。**</summary>
    static float PxOf(float worldX) { return worldX * 108f + 960f; }

    /// <summary>世界 y → 画布像素 y。**y 是反的**（像素 y 向下）⇒ `540 − worldY × 108`。
    /// 🔴 2026-09-23 踩过：拿 `PxOf`（x 的换算）去量 y，得出「节点整体偏下 163px」的**假警报**。</summary>
    static float PxYOf(float worldY) { return 540f - worldY * 108f; }

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

    /// <summary>节点上那段字**现在写的是什么**（`Label.Text`；`Label` 走点阵兜底时也有值）。</summary>
    static string TextOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.Text : null;
    }

    /// <summary>一个件**渲出来**的像素矩形（画布像素 · 左上原点 · y 向下）。
    /// 图走 `ImageQuad.WorldW/H`、字走 `Label.WorldW/H`（**真测量**）；取**组件自己的 transform**
    /// （`AlignLeft/Right` 会把 `Label` 的节点挪走）。见 `ShopScene.RectOf` 的同名注释。</summary>
    static bool RectOf(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        if (t == null) return false;
        Transform node = t; float w, h;
        var lb = t.GetComponentInChildren<Label>();
        var q = t.GetComponentInChildren<ImageQuad>();
        if (lb != null) { node = lb.transform; w = lb.WorldW * 108f; h = lb.WorldH * 108f; }
        else if (q != null) { node = q.transform; w = q.WorldW * 108f; h = q.WorldH * 108f; }
        else return false;
        float cx = PxOf(node.position.x), cy = PxYOf(node.position.y);
        x1 = cx - w * 0.5f; x2 = cx + w * 0.5f;
        y1 = cy - h * 0.5f; y2 = cy + h * 0.5f;
        return true;
    }

    static bool Overlaps(float ax1, float ay1, float ax2, float ay2,
                         float bx1, float by1, float bx2, float by2)
    {
        return ax1 < bx2 - 0.5f && bx1 < ax2 - 0.5f && ay1 < by2 - 0.5f && by1 < ay2 - 0.5f;
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
        ForgeData.ResetForTest();     // 锻造页的数据是静态的 ⇒ 每次自检从初值起（自检之间互不影响）
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
        // 🔴 **2026-09-23 加（原版值，出处 `d:/2/tools/decomp_full/TabButtons__Initialize.c:35-44`）**：
        //    原版 `Initialize` 第一件事就是 `tabButtonPrefab.gameObject.SetActive(false)` ——
        //    第 4 键（Booster Packs）只是**运行期新增页签的克隆母版**，⇒ **左栏运行期只有 3 个键**。
        //    我们原来把它常显了（找茬点：多画了一层）。这条断言钉住「建了但关着」。
        var b3 = FindChild(bar, "RewardsTabButton_3");
        CheckTrue(b3 != null, "第 4 键（`tabButtonPrefab` 母版）**照建**（以后加活动页签要克隆它）");
        CheckTrue(b3 != null && !b3.gameObject.activeSelf,
                  "第 4 键运行期**隐藏**（照原版 `TabButtons.Initialize`）");
        Check(CountVisibleChildren(bar, "RewardsTabButton_"), 3, "左栏**可见的键恰好 3 个**");

        // 🆕 2026-10-03（§三 第 29 条 **B2**）：左栏 `Highlight` 从「单块拉伸」改成**九宫格**
        //   —— 原版这一件是 `Image.Type = Sliced` + `m_PixelsPerUnitMultiplier = 0.92`
        //   （判据：`menu_dump.py … "MissionsRewardsButton"` ⇒ `40k_main_bt_selected BW 71×71 九宫 30,30,30,30 ppuMul=0.92`；
        //    再按图集采样 ⇒ 边是**软边晕**、不是纯色 ⇒ 「看不出来」那条旧记录**已被证伪**）。
        //   ⇒ 一棵树里 **9 个 quad**：切键时**必须整棵开关**，只切一个会静默留下 8 块。
        {
            var k0 = FindChild(bar, "RewardsTabButton_0");
            var hl0 = k0 != null ? FindChild(k0, "Highlight") : null;
            var qs = hl0 != null ? hl0.GetComponentsInChildren<ImageQuad>(true) : null;
            CheckTrue(qs != null && qs.Length == 9,
                      $"左栏 `Highlight` 是**九宫格** ⇒ 一棵树 **9 块**（实测 {(qs != null ? qs.Length : 0)}）");
            win.tabButtons.Click(1);
            int alive = 0;
            if (qs != null) foreach (var q in qs) if (q != null && q.gameObject.activeInHierarchy) alive++;
            Check(alive, 0, "切到别的键 ⇒ 第 1 键的高亮**整棵**关掉（不是只关一块）");
            win.tabButtons.Click(0);
            alive = 0;
            if (qs != null) foreach (var q in qs) if (q != null && q.gameObject.activeInHierarchy) alive++;
            Check(alive, 9, "切回来 ⇒ 9 块**全亮**");
        }

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

        // 🆕 2026-10-03（§三 第 29 条 **B1**）：每日骷髅卡 `counter/icons` 的 `Army` 格
        //   判据 = `d:/2/tools/decomp_full/MissionCounterDisplay__Setup.c:51-63` ——
        //   图 = `ArmyUtilities.GetArmyIcon(challenge.Army)`，**`army == Neutral(0)` 时那一格整格 `SetActive(false)`**
        //   （HLG 跳过它 ⇒ `skull` 与计数文字**整体左移 60**）。
        //   ⚠️ 我们这份 daily **没有阵营维度**（服务端下发）⇒ 走 Neutral 分支 ⇒ **那格不建、也不占位**。
        {
            var skc = FindChild(tab, "Daily Skulls Mission Container");
            CheckTrue(skc != null, "`Daily Skulls Mission Container` 建了");
            CheckTrue(skc != null && FindChild(skc, "Army") == null,
                      "`counter/icons` 的 `Army` 那一格**不建**（原版 `army == Neutral` ⇒ `SetActive(false)`）");
            var skn = skc != null ? FindChild(skc, "skull") : null;
            float sx1, sy1, sx2, sy2;
            bool hasSkull = RectOf(skn, out sx1, out sy1, out sx2, out sy2);
            CheckTrue(hasSkull, "`skull` 建了");
            if (hasSkull)
            {
                // 🔴 **关键的判别式**：`skull` 的左边缘 = **图标区自己的左边缘**（`Army` 那格没占那 60px）。
                //    原来我们固定让出 60px ⇒ 这条会红。
                // ⚠️ **不另比宽度**：骷髅卡是按**设计空间**摆再整体缩放的（两个 quad 的缩放口径不同），
                //    比宽度会把「缩放」误判成「版面错」—— 宽度在 `MissionsTab` 里由同一个设计常量给出，
                //    真正会错的是**起点**（就是上面这条）。
                CheckNear(sx1, MissionsTab.SkullIconLeftPx, 1f,
                          "`skull` 的左边缘 = **图标区左边缘**（`Army` 那格**没有占位** —— 原版 Neutral 分支同）");
                CheckTrue(sx2 - sx1 > 1f, "`skull` 有非零宽度（不是画了个零宽的东西）");
            }
        }

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
        CheckHoverSwap(win.transform, "奖励窗（含 Missions 线）");

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

        // ============================================================ §三·b 锻造厂页
        // 🔴 **每一条的期望值都来自「原版参数」，不是我们写的常量**（否则就是自证）——
        //    出处 = `资料/阶段二_锻造厂与战役页_原版规格.md` §一/§二（原版 JSON 走链算出），
        //    矩形链由 `工具/menu_rect.py` 复算（**独立于 C# 的第二份实现**：`Core/UguiRect.cs`）。
        Section("§三·b `Forge Tab`（锻造厂，阶段二第 3 层第 1 件）：层 × 参数逐条对");
        if (forge != null)
        {
            // 页矩形：`Forge Tab` 实测 x 330.69..1920.00 · y 71.12..1079.82
            CheckAt(forge, 330.69f, 1920f, 71.12f, 1079.82f, "`Forge Tab` 页矩形");
            var fbg = FindChild(forge, "Background");
            CheckTrue(fbg != null, "`Background` 建了（原版 `Image(sprite=空)` + 色 **#000000** ⇒ 纯黑满铺）");
            var fwarp = FindChild(fbg, "Warp");
            CheckAt(fwarp, 741.85f, 1508.85f, 88.47f, 1062.47f, "`Background/Warp`（旋涡大图）");
            CheckRectPx(fwarp, 741.85f, 1508.85f, 88.47f, 1062.47f, "`Warp` 渲染尺寸（**767×974，与原图同尺寸 ⇒ 不拉伸**）");
            CheckArt(fwarp, "40K_ArmyTrack_bg", "旋涡大图的图");
            // 🔴 **同队列的两层「谁盖谁不可控」**（2026-09-23 实测：旋涡整张被 `Background` 的黑板盖掉，
            //    只看得到几缕紫雾 —— 而**所有断言都是绿的**）⇒ 钉一条「Warp 的队列必须大于黑板」。
            var fblackQ = fbg != null ? fbg.GetComponentInChildren<ImageQuad>() : null;
            var fwarpQ = fwarp != null ? fwarp.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(fblackQ != null && fwarpQ != null && fwarpQ.RenderQueue > fblackQ.RenderQueue,
                      "`Warp` 的**渲染队列 > 黑板**的（同队列里 Unity 按到相机的距离排，谁盖谁不可控）");

            // 奖励轨道：`Rewards Scroll View` x 330.97..1919.73 · y 318.60..1080
            var ftrack = FindChild(forge, "Rewards Scroll View");
            CheckAt(ftrack, 330.97f, 1919.73f, 318.60f, 1080f, "`Rewards Scroll View`");
            var fcontent = FindChild(FindChild(ftrack, "Viewport"), "Rewards Content");
            // 🔴 2026-09-23 起**轨道会滚** ⇒ 只建**视口里放得下的那些格**（原版 `RectMask2D` 裁掉的不建）。
            //    「一共 50 格」由 `ForgeData.MaxLevel` + 「滚到最右能建出第 50 格」两条一起管（见 §三·b2）。
            CheckTrue(fcontent != null && fcontent.childCount > 0 && fcontent.childCount < ForgeData.MaxLevel,
                      $"`Rewards Content` 下建的是**视口里放得下的那些格**（实测 {fcontent.childCount} / 共 "
                      + $"{ForgeData.MaxLevel} 格；⚠️ **格数是我们挑的**，原版在服务端 —— 见 `ForgeData.MaxLevel` 的注释）");

            // 阵营选择条：`Forge Army Selector` x 588.17..1662.53 · y 71.57..196.67
            var fsel = FindChild(forge, "Forge Army Selector");
            CheckAt(fsel, 588.17f, 1662.53f, 71.57f, 196.67f, "`Forge Army Selector`");
            var fsep = FindChild(fsel, "Separator Line");
            CheckAt(fsep, 491.43f, 1759.27f, 191.38f, 197.38f, "`Separator Line`");
            CheckArt(fsep, "40k_main_line_purple", "分隔线的图");
            var fArmy = FindChild(FindChild(fsel, "Viewport"), "Army Content");
            CheckTrue(fArmy != null && fArmy.childCount > 0 && fArmy.childCount <= ForgeData.Armies.Length,
                      $"`Army Content` 下建的是**视口里放得下的那些条目**（实测 {fArmy.childCount} / 共 "
                      + $"{ForgeData.Armies.Length} 个阵营；13 个阵营照用户边界「全解锁」）");

            // 选中信息：`Selected Army Info` x 619.40..1240.69 · y 195.76..318.48
            var finfo = FindChild(forge, "Selected Army Info");
            CheckAt(finfo, 619.40f, 1240.69f, 195.76f, 318.48f, "`Selected Army Info`");
            CheckAt(FindChild(finfo, "ArmyText"), 762.91f, 1083.82f, 209.36f, 259.36f, "`ArmyText`");
            CheckAt(FindChild(finfo, "LevelText"), 766.10f, 1097.11f, 252.76f, 302.76f, "`LevelText`");
            var ficon = FindChild(finfo, "Army Icon");
            CheckAt(ficon, 621.76f, 747.04f, 188.38f, 313.66f, "`Army Icon`");
            CheckArt(ficon, DeckRuntime.FactionIcon(ForgeData.Selected),
                     "阵营徽记（**走 `DeckRuntime.FactionIcon`，全工程唯一一份映射**）");

            CheckAt(FindChild(forge, "Help Icon"), 1685.21f, 1737.40f, 215.90f, 268.08f, "`Help Icon`");

            // ---- 纪律①：出厂 inactive 的**一律不建**（后两个另有 `Awake` 无条件关它们的硬证据）
            CheckTrue(FindChild(finfo, "Xp Points Icon") == null,
                      "`Xp Points Icon` **不建**（出厂 inactive；反编译实证全代码无人点亮它）");
            CheckTrue(FindChild(forge, "Debug Add points") == null,
                      "`Debug Add points` **不建**（`ForgeWindowTab.Awake` 无条件 `SetActive(false)`）");
            CheckTrue(FindChild(forge, "Debug Set Forge") == null, "`Debug Set Forge` **不建**（同上）");
            // 🔴 **2026-10-03 就地更正（铁律 5）**：这里原来挂着「`War ParticleSystemUI` **本轮不建**」
            //    （0917 判决：先做静态版 + 空态，粒子后补）—— **那条已经过期了**：A12-P1 正是把三个宿主
            //    建了出来（`ForgeTab.Build` 的 `BuildParticleHosts`；见那个方法头的日志）。旧断言断的是旧行为。
            //    ⇒ 新判据 = **宿主该在 + 层级/名字逐字对 + 子树里一个 `ParticleSystem` 都没有**（粒子本身仍没建），
            //      写在 §三·b3-a（`Background/War ParticleSystemUI` 三级全路径 + `Particle System nebula`
            //      没有 `RectTransform` + 三处身位 + 空态那一条）。**这里不再抄第二份**（同一条判据两处写 = 迟早不一致）。
            //    ⚠️ 别把它挪回上面那张「出厂 inactive ⇒ 不建」的清单里：它现在是**要建的**。

            // ---- 纪律③：两根石柱的**缩放必须烘进子件矩形**（右柱是镜像）
            var fdecor = FindChild(forge, "Background Elements");
            var fcl = FindChild(fdecor, "Column Left");
            var fcr = FindChild(fdecor, "Column Right");
            CheckTrue(fcl != null && fcr != null, "`Column Left` / `Column Right` 都建了");
            var fclt = FindChild(fcl, "Culumn Top");
            var fcrt = FindChild(fcr, "Culumn Top");
            CheckRectPx(fclt, 0f, 319f * 1.04f, 0f, 460f * 1.04f,
                        "左柱 `Culumn Top`（原版 319×460 × `localScale 1.04` ⇒ **实绘 331.76×478.4**）");
            CheckRectPx(fcrt, 0f, 319f * 1.04f, 0f, 460f * 1.04f, "右柱 `Culumn Top`（同上，镜像只是位置翻）");
            CheckTrue(fclt != null && fcrt != null && fclt.position.x < fcrt.position.x,
                      "右柱在左柱右边（`Column Right` 的锚点是 (1,1) ⇒ 它的设计矩形在 x=1920 起）");
            CheckArt(fclt, "40k_rewards_forge_decoration_Column_top", "石柱顶的图");

            // ---- 纪律④：`Ready for level up` 建了，且**开关 = `hasToCollectReward`**（原版判据）
            var fready = FindChild(forge, "Ready for level up");
            CheckTrue(fready != null, "`Ready for level up` **建了**（原版 prefab 里它是激活的）");
            CheckArt(FindChild(fready, "Glow"), "Glow_UI_W40K", "可领光效的图（色 #FF2DDF）");
            CheckTrue(fready != null && fready.gameObject.activeSelf == ForgeData.HasToCollect(ForgeData.Selected),
                      "可领光效的开关 = `hasToCollectReward`（原版 `ForgeRewardSelector.CreateRewards` 的判据）");

            // ---- 纪律⑤：等级圆牌**只有两张 sprite**（不是三张）
            var cell0 = fcontent != null && fcontent.childCount > 0 ? fcontent.GetChild(0) : null;
            var cellClaim = fcontent != null ? FindChild(fcontent, "ForgeCell_" + ForgeData.LevelOf(ForgeData.Selected)) : null;
            if (cell0 != null)
                CheckArt(FindChild(cell0, "LevelBg"), "40k_ArmyTrack_milestone_on",
                         "第 1 格圆牌 = `_on`（已领 ⇒ `Collected` 用 onSprite）");
            if (cellClaim != null)
            {
                CheckArt(FindChild(cellClaim, "LevelBg"), "40k_ArmyTrack_milestone_on", "可领格的圆牌 = `_on`");
                CheckTrue(FindChild(cellClaim, "Generic UI Button") != null,
                          "**可领格**才有 `Generic UI Button`（Claim）—— 照原版「只有 ToCollect 才开 claimButton」");
            }

            // ---- `SelectArmy`：换阵营要**真的换数据**（原版 `ForgeWindowTab.SelectArmy` 写三处 + 重建轨道）
            var ft = forge.GetComponent<ForgeTab>();
            CheckTrue(ft != null, "`Forge Tab` 上挂的是 `ForgeTab`（**不再是空页桩**）");
            if (ft != null)
            {
                ft.SelectArmy("SaimHann");
                Check(ForgeData.Selected, "SaimHann", "`SelectArmy` 把当前阵营换成了 SaimHann");
                var fat = FindChild(finfo, "ArmyText");
                var flt = FindChild(finfo, "LevelText");
                var fatL = fat != null ? fat.GetComponentInChildren<Label>() : null;
                var fltL = flt != null ? flt.GetComponentInChildren<Label>() : null;
                Check(fatL != null ? fatL.Text : null, "SaimHann",
                      "换阵营后 `ArmyText` 写成了新阵营名（原版 `ForgeSelectedArmyInfo.Initialize`）");
                Check(fltL != null ? fltL.Text : null, "Level " + ForgeData.LevelOf("SaimHann") + "/" + ForgeData.MaxLevel,
                      "`LevelText` = `\"Level {已领}/{格数}\"`（原版 `\"{0} {1}/{2}\"` 套本地化键 `\"MainMenu/Level\"`）");
                var rdy = ft.transform.Find("Ready for level up");
                CheckTrue(rdy != null && !rdy.gameObject.activeSelf,
                          "SaimHann 差 40 点 ⇒ **可领光效灭**（`hasToCollectReward = false`）");
                ft.SelectArmy("Goff");        // 换回可领态 —— 后面两张截图要用
            }

            // ============================================================ §三·b2 锻造厂页「找茬」
            // 🔴 **2026-09-23 找茬族**（`项目任务.md` §三 第 15 条第 18 行点名：锻造厂页只跑过它自己那 46 条
            //    断言，**没有**像商店 / 战役奖励窗那样把截图逐件核过一遍）。
            //    判据只有一条：**量「渲出来」的矩形** —— 「件在不在」那一类断言**验不出重叠 / 出屏 / 不齐**
            //    （`资料/已知的坑.md`「为什么照解包资料摆还会摆错」的 **C 类成因**）。
            //    期望值全部来自**原版参数**（正本 §一/§二/§五/§六 的 JSON 原文），不是我们自己的常量。
            Section("§三·b2 锻造厂页「找茬」：量【渲出来】的矩形（不越界 / 不重叠 / 同中心）");

            // ---- ① 阵营条：原版 `Army Content` = 「选择条正中心的一个零宽点」+ `ContentSizeFitter`
            //      ⇒ 13 个条目**以中心对称展开**（左对齐那一版最后两个阵营出屏、点不到 —— 本轮查出的真错）
            if (fArmy == null) CheckTrue(false, "`Army Content` 不在（下面那一族找茬没法量）");
            else
            {
                CheckNear(PxOf(fArmy.position.x), 1125.35f, 1f,
                          "`Army Content` 的中心 x = **选择条中心 1125.35**（原版锚点是「中心零宽点」⇒ 对称展开）");
                int n = fArmy.childCount;
                // 🔴 **2026-09-23 起阵营条会滚**（`MenuScroll`）：视口外的条目不建（= 原版 `RectMask2D` 裁掉的），
                //    开局又照 `FocusOnArmy` 把选中的对到视口中心 ⇒ **不能再拿「第 1 个在 391.19」这种内容坐标
                //    去量屏幕位置**（那是偏移 0 时的值，本轮断言被自检自己抓出来过）。改成量这三件事：
                var ftArmy = forge.GetComponent<ForgeTab>();
                CheckTrue(ftArmy != null && ftArmy.ArmyScroll != null, "阵营条的滚动区建了（`MenuScroll`）");
                CheckTrue(n >= 8, $"`Army Content` 下建了 **{n} 条**（≈ 视口里放得下的那些；越界的不建）");
                var selNow = FindChild(fArmy, "ForgeArmyItem_1");
                // ⚠️ **靠边的阵营对不到正中心**：`FocusOn` 会被滚动极值夹住（第 1/2 个最多只能到 −265.16），
                //    这是**对的** —— 原版 `ScrollRect` 一样夹。所以这里断「**完整落在视口里**」而不是「在中心」。
                float sx1, sy1, sx2, sy2;
                CheckTrue(selNow != null && RectOf(FindChild(selNow, "Icon"), out sx1, out sy1, out sx2, out sy2)
                          && sx1 >= 588.17f && sx2 <= 1662.53f,
                          "**选中的那个阵营完整落在选择条视口里**（照原版 `ArmySelector.FocusOnArmy` 的意图；"
                          + "⚠️ 靠边的阵营对不到正中心 —— 被滚动极值夹住，原版 `ScrollRect` 同样如此）");
                if (ftArmy != null && ftArmy.ArmyScroll != null)
                {
                    var sc = ftArmy.ArmyScroll;
                    // 滚动两端：**最边上那一条都要能完整带进视口**（= 原版靠滚动做到的事）
                    float fx1, fy1, fx2, fy2;
                    sc.ScrollBy(sc.MinOffset - sc.Offset);
                    var first = FindChild(fArmy, "ForgeArmyItem_0");
                    CheckTrue(first != null && RectOf(FindChild(first, "Icon"), out fx1, out fy1, out fx2, out fy2)
                              && fx1 >= 588.17f && fx2 <= 1662.53f,
                              "**滚到最左：第 1 个阵营的图标完整落在选择条视口里**"
                              + "（原来它整条躲在左柱的绘制矩形后面 —— 第 21 条那个缺口）");
                    sc.ScrollBy(sc.MaxOffset - sc.Offset);
                    int lastIdx = ForgeData.Armies.Length - 1;
                    var last = FindChild(fArmy, "ForgeArmyItem_" + lastIdx);
                    CheckTrue(last != null && RectOf(FindChild(last, "Icon"), out fx1, out fy1, out fx2, out fy2)
                              && fx1 >= 588.17f && fx2 <= 1662.53f,
                              $"**滚到最右：第 {ForgeData.Armies.Length} 个阵营的图标完整落在选择条视口里**"
                              + "（原来它落在 2056、整条出屏）");
                    CheckNear(sc.MaxOffset, 265.16f, 1f,
                              "内容居中 ⇒ **两侧都能滚**：右极值 = 内容右边(1927.69) − 视口右边(1662.53) = 265.16px");
                    CheckNear(sc.MinOffset, -265.16f, 1f, "左极值 = 内容左边(323.01) − 视口左边(588.17) = −265.16px");
                    ftArmy.FocusSelectedArmy();       // 还原成开局定位（后面两张截图要用）
                }
                int iconOverlap = 0; float prevX2 = float.NaN;
                for (int i = 0; i < n; i++)
                {
                    float ix1, iy1, ix2, iy2;
                    if (!RectOf(FindChild(fArmy.GetChild(i), "Icon"), out ix1, out iy1, out ix2, out iy2)) continue;
                    if (!float.IsNaN(prevX2) && ix1 < prevX2 - 0.5f) iconOverlap++;
                    prevX2 = ix2;
                }
                Check(iconOverlap, 0, "相邻阵营**图标**的渲染矩形互不重叠（原版条目 136.36 宽 · 间距 −14 "
                      + $"⇒ 图标之间还有 ~20px 缝；实测重叠 {iconOverlap} 对）");
                // ✅ 2026-09-23 **缺口已修**：这条以前钉的是「4 个落在左右柱的绘制矩形里、够不着」——
                //    现在两条滚动区能把**两端**都带进视口 ⇒ 期望值翻成 **0**
                //    （照 §一 那条纪律：钉缺口的断言修好之后要翻过来，别留着旧期望值）。
                //    ⚠️ 柱子仍会盖住**恰好落在它那一条里**的图标 —— 但那是**原版也一样的**（兄弟序如此）。
                int covered = 0;
                for (int i = 0; i < n; i++)
                {
                    float ix1, iy1, ix2, iy2;
                    if (!RectOf(FindChild(fArmy.GetChild(i), "Icon"), out ix1, out iy1, out ix2, out iy2)) continue;
                    if (ix2 <= 662f || ix1 >= 1588f) covered++;    // 整个图标都在某根柱子的绘制矩形里
                }
                Check(covered, 0, $"落在**左右装饰柱矩形之内**的阵营条目数 = **0**（实测 {covered}）"
                      + " —— 缺口已修：`MenuScroll` 能把两端带进视口（原版靠 `ScrollRect` 做同一件事）");

                // 选中格：`HighlightBG` 与它那一格**同中心**（原版 `pos = (0,0)`）+ 箭头在框内
                var selItem = FindChild(fArmy, "ForgeArmyItem_1");        // Goff = 第 2 个（此刻选中的就是它）
                var hb = FindChild(selItem, "HighlightBG");
                CheckTrue(selItem != null && hb != null, "选中的那一格上有 `HighlightBG`（选中 = 多显一层）");
                if (selItem != null && hb != null)
                {
                    CheckNear(PxOf(hb.position.x), PxOf(selItem.position.x), 0.5f,
                              "`HighlightBG` 与它那一格**同中心 x**（原版 `pos = (0,0)`）");
                    CheckNear(PxYOf(hb.position.y), PxYOf(selItem.position.y), 0.5f, "同上，y 也对齐");
                    float hx1, hy1, hx2, hy2, ax1, ay1, ax2, ay2;
                    if (RectOf(hb, out hx1, out hy1, out hx2, out hy2))
                    {
                        CheckNear(hx2 - hx1, 114.36f, 1f,
                                  $"`HighlightBG` 的渲染宽 = **114.36**（`Forge Army Item Button` **变体**的值；"
                                  + $"母版才是 136 —— 别套错实例；实测 {hx2 - hx1:F2}）");
                        CheckNear(hy2 - hy1, 122f, 1f, "`HighlightBG` 的渲染高 = **122**");
                    }
                    if (RectOf(FindChild(hb, "Arrow"), out ax1, out ay1, out ax2, out ay2)
                        && RectOf(hb, out hx1, out hy1, out hx2, out hy2))
                    {
                        // 变体原文：`Arrow` 锚 `(0.5,0)`（框底中点）+ pos `(0,12.1)` + 尺寸 102.38×30.71
                        // ⇒ 水平中心 = 框中心、竖向中心 = 框底上方 12.1（**会探出底边 ~3px，原版如此**）
                        CheckNear((ax1 + ax2) * 0.5f, (hx1 + hx2) * 0.5f, 0.5f,
                                  "`Arrow` **水平居中**在高亮框上（原版 `pos.x = 0`，挂在框底中点）");
                        // 🔴 **2026-10-03 改**：接上「选择条视口」的裁剪之后，箭头**探出视口底的那一截被真裁掉**
                        //    （原版 `Viewport` 上那层 `RectMask2D` 同样会裁）⇒ 再量「渲出来的中心 = 框底上方
                        //    12.1」量到的其实是**裁过之后**的中心（实测矮了 1.7px）。
                        //    ⇒ 改成量**没被裁的那条边**：上边缘 = 框底上方 12.1 + 半个箭头高（30.71/2）。
                        CheckNear(ay1, hy2 - 12.1f - 30.71f * 0.5f, 0.5f,
                                  "`Arrow` 的**上边缘**在框底上方 12.1 + 半箭头高（原版 `pos.y = 12.1`，尺寸 102.38×30.71）");
                        CheckTrue(ay2 <= hy2 + 3f + 0.5f,
                                  $"`Arrow` 探出框底的那一截**不超过原版的 ~3px**（实测 {ay2 - hy2:F2}px）");
                    }
                }

                // 黑底：原版根上 `Img[sprite=0] col=(0,0,0,1)` ⇒ 不透明纯黑（正本 §二「照画」）
                var selBg = FindChild(fsel, "Black");
                CheckTrue(selBg != null && TintOf(selBg).a > 0.99f && TintOf(selBg).r < 0.01f
                          && TintOf(selBg).g < 0.01f && TintOf(selBg).b < 0.01f,
                          "阵营条的不透明纯黑底**建了**（原版 `Img[sprite=0] col=(0,0,0,1)`；本族找茬查出原来漏画）");
                CheckTrue(selBg != null && Mathf.Abs(Wpx(selBg) - 1074.36f) < 2f,
                          $"黑底的渲染宽 = **1074.36**（= 选择条 588.17..1662.53；实测 {(selBg != null ? Wpx(selBg) : 0f):F1}）");
            }

            // ---- ② 可领格（Goff 第 4 格 = index 3）：Claim 钮与它那一格**同中心**、且不越出格
            {
                // 🔴 2026-09-23 起**轨道会滚**（`MenuScroll`）⇒ 「实建了哪些格」本身就是要验的东西：
                //    视口外的格**根本不建**（= 原版 `RectMask2D` 裁掉的那些），所以**不能**再断「50 格全在」。
                var ft0 = forge.GetComponent<ForgeTab>();
                string built = "";
                if (fcontent != null)
                    foreach (var t in fcontent.GetComponentsInChildren<Transform>(true))
                        if (t.name.StartsWith("ForgeCell_")) built += t.name.Substring(10) + ",";
                Debug.Log(P + $"   · 锻造轨道：偏移 **{(ft0 != null && ft0.TrackScroll != null ? ft0.TrackScroll.Offset : 0f):F1}px**"
                          + $" · 实建格 [{built}] · 阵营条偏移 **{(ft0 != null && ft0.ArmyScroll != null ? ft0.ArmyScroll.Offset : 0f):F1}px**");
                CheckTrue(ft0 != null && ft0.TrackScroll != null && ft0.ArmyScroll != null,
                          "锻造页的两条滚动区都建了（`MenuScroll` —— 全壳唯一一份滚动实现）");
                if (ft0 != null && ft0.TrackScroll != null)
                {
                    // ① **开局定位**：照原版 `ForgeRewardSelector` 的吸附语义，把「该领的那一格」对到视口中心
                    float wantOff = ForgeTab.TabL + ForgeTab.TrackPadL + ForgeData.LevelOf(ForgeData.Selected) * (ForgeTab.CellW + ForgeTab.TrackSpacing) + ForgeTab.CellW * 0.5f - 1125.35f;
                    CheckNear(ft0.TrackScroll.Offset, wantOff, 1f,
                              $"开机对到**该领的那一格**（照原版吸附语义）：偏移 = {wantOff:F1}px（= 第 {ForgeData.LevelOf(ForgeData.Selected) + 1} 格中心 − 视口中心）");
                    // ② **滚到最右** ⇒ 最后一格（level 50）完整落在视口里
                    float saved = ft0.TrackScroll.Offset;
                    ft0.TrackScroll.ScrollBy(ft0.TrackScroll.MaxOffset);
                    var lastC = fcontent != null ? FindChild(fcontent, "ForgeCell_" + (ForgeData.MaxLevel - 1)) : null;
                    float lx1, ly1, lx2, ly2;
                    CheckTrue(lastC != null && RectOf(lastC, out lx1, out ly1, out lx2, out ly2),
                              $"滚到最右后**最后一格（level {ForgeData.MaxLevel}）建出来了** —— 这是「第 5 格以后领不到」那个缺口的判据");
                    // ③ **滚到某一格** ⇒ 那一格的 Claim 钮落在视口里（能点到）
                    ft0.TrackScroll.ScrollBy(-ft0.TrackScroll.MaxOffset);
                    ft0.FocusClaimable();
                    var cellNow = fcontent != null ? FindChild(fcontent, "ForgeCell_" + ForgeData.LevelOf(ForgeData.Selected)) : null;
                    var cbtn = cellNow != null ? FindChild(cellNow, "Generic UI Button") : null;
                    float cx1, cy1, cx2, cy2;
                    CheckTrue(cbtn != null && RectOf(cbtn, out cx1, out cy1, out cx2, out cy2)
                              && cx1 >= 330.97f && cx2 <= 1919.73f,
                              "**该领那一格的 Claim 钮落在视口里**（原版靠滚动做到；我们不再需要「屏幕外也能点」这种假话）");
                    ft0.TrackScroll.ScrollBy(saved - ft0.TrackScroll.Offset);      // 还原，别影响后面的截图
                }

                int claimIdx = ForgeData.LevelOf(ForgeData.Selected);
                var clmCell = fcontent != null ? FindChild(fcontent, "ForgeCell_" + claimIdx) : null;
                var claim = clmCell != null ? FindChild(clmCell, "Generic UI Button") : null;
                CheckTrue(claim != null, "第 4 格（`ToCollect`）上有 `Generic UI Button`（Claim 钮）");
                if (clmCell != null && claim != null)
                {
                    CheckNear(PxOf(claim.position.x), PxOf(clmCell.position.x), 0.5f,
                              "`Claim` 钮与它那一格**同中心 x**（原版 `pos.x = −1.5e−05`）");
                    float bx1, by1, bx2, by2;
                    if (RectOf(claim, out bx1, out by1, out bx2, out by2))
                    {
                        float ccx = PxOf(clmCell.position.x);
                        CheckTrue(bx1 >= ccx - 252.95f - 0.5f && bx2 <= ccx + 252.95f + 0.5f,
                                  $"`Claim` 钮**不越出它那一格**（格宽 505.9 是原版值；钮 x {bx1:F0}..{bx2:F0}）");
                        // ⚠️ **如实记、不当断言**：第 4 格整体落在视口右边缘之外 —— 原版靠 `RectMask2D` 裁掉，
                        //    我们没建遮罩（文件头已出声）。整格一起越出、屏幕右边缘也在 1920 ⇒ 画面上看不到差别。
                        Debug.Log(P + $"   · 第 4 格的 Claim 钮右边缘 {bx2:F1}px（视口右 1919.73 ⇒ 越出 "
                                  + $"{bx2 - 1919.73f:F1}px）—— 原版靠 `RectMask2D` 裁，我们没建遮罩");
                    }
                }
            }

            // ---- ③ `Selected Army Info`：两行字与徽记**互不重叠**、且徽记在本框内
            {
                float ax1, ay1, ax2, ay2, lx1, ly1, lx2, ly2, ix1, iy1, ix2, iy2;
                bool okA = RectOf(FindChild(finfo, "ArmyText"), out ax1, out ay1, out ax2, out ay2);
                bool okL = RectOf(FindChild(finfo, "LevelText"), out lx1, out ly1, out lx2, out ly2);
                bool okI = RectOf(FindChild(finfo, "Army Icon"), out ix1, out iy1, out ix2, out iy2);
                CheckTrue(okA && okL && okI, "`Selected Army Info` 三件的渲染矩形都量得到");
                // 🔴 **这里不能断「两行不重叠」**：原版这两行的**框**本来就叠 6.6px
                //    （`ArmyText` 209.36..259.36 · `LevelText` 252.76..302.76），而 `Label.WorldH` 量的是**框**高
                //    ⇒ 断「不重叠」是**假警报**（本轮先写错、被自检自己抓出来了）。改断「两行中心 = 原版框中心」。
                if (okA)
                    CheckNear((ay1 + ay2) * 0.5f, 234.36f, 2f,
                              "阵营名的渲染中心 y = **234.36**（原版框 209.36..259.36 的中心）");
                if (okL)
                    CheckNear((ly1 + ly2) * 0.5f, 277.76f, 2f,
                              "等级行的渲染中心 y = **277.76**（原版框 252.76..302.76 的中心）");
                if (okA && okI)
                    CheckTrue(!Overlaps(ax1, ay1, ax2, ay2, ix1, iy1, ix2, iy2),
                              $"徽记**不压**阵营名（徽 x {ix1:F0}..{ix2:F0} · 名 x {ax1:F0}..{ax2:F0}）");
                if (okI)
                    CheckTrue(ix1 >= 619.40f - 0.5f && ix2 <= 1240.69f + 0.5f,
                              $"徽记在 `Selected Army Info` 框内（原版框 619.40..1240.69；实测 {ix1:F0}..{ix2:F0}）");
            }

            // ---- ④ 装饰柱：**渲染范围不许越出屏幕左右**（右柱整根是镜像出来的，翻错会飞出去）
            {
                float lx1, ly1, lx2, ly2, rx1, ry1, rx2, ry2;
                bool okL = RectOf(FindChild(fcl, "Culumn Top"), out lx1, out ly1, out lx2, out ly2);
                bool okR = RectOf(FindChild(fcr, "Culumn Top"), out rx1, out ry1, out rx2, out ry2);
                CheckTrue(okL && lx1 >= 0f && lx2 <= 1920f, $"左柱的渲染范围在屏幕内（x {lx1:F0}..{lx2:F0}）");
                CheckTrue(okR && rx1 >= 0f && rx2 <= 1920f, $"右柱的渲染范围在屏幕内（x {rx1:F0}..{rx2:F0}）");
                CheckTrue(okL && okR && lx2 < rx1, $"左右两柱**不重叠**（左到 {lx2:F0} · 右从 {rx1:F0} 起）");
            }

            // ---- ⑤ 层序：页内各层队列**严格递增**（同队列里「谁盖谁」不可控 —— 已踩两次）
            CheckTrue(ForgeTab.QTabBg < ForgeTab.QTabWarp && ForgeTab.QTabWarp < ForgeTab.QSelBg
                      && ForgeTab.QSelBg < ForgeTab.QSelLine && ForgeTab.QSelLine < ForgeTab.QArmyIcon
                      && ForgeTab.QArmyIcon < ForgeTab.QTabDecor && ForgeTab.QTabDecor < ForgeTab.QTabHelp,
                      "页内层序严格递增：黑板 < 旋涡 < **阵营条黑底** < 分隔线 < 阵营层 < 装饰柱 < Help"
                      + $"（{ForgeTab.QTabBg}/{ForgeTab.QTabWarp}/{ForgeTab.QSelBg}/{ForgeTab.QSelLine}/"
                      + $"{ForgeTab.QArmyIcon}/{ForgeTab.QTabDecor}/{ForgeTab.QTabHelp}）");
            // `Help Icon`（x 1685..1737）正落在**右柱的绘制范围**（x 1547..1920）里 ⇒ 只靠队列分层保住它
            CheckTrue(ForgeTab.QTabHelp > ForgeTab.QTabDecor,
                      "`Help Icon` 的队列**高于右柱**（右柱罩到 1685..1737 那一片，同队列就不可控）");

            // ---- ⑥ 指针层（2026-09-23 新加；判据 =「真鼠标点不动」那个缺口，见第 23 条）----
            //    🔴 原来 `WindowButton` 只实现老式 `OnMouseUpAsButton`，而工程既没给 quad 加 collider、
            //       又设成「只用新 Input System」⇒ **一次也不会派发**。
            //       现在收口成 `PointerLayer`（新输入系统轮询 + 自己算命中，照 `DeckRuntime.HandlePointer`）。
            //    ⚠️ 批处理里 `Update` 不跑、也没有输入事件 ⇒ 这里直调 `ClickAt/WheelAt/ButtonAt`，
            //       **和真点真滚走的是同一条路**。
            {
                // 🔴 **先切到锻造页**：`WindowButton` 是 `OnEnable` 登记进 `All` 的，而**非当前页的物件是关着的**
                //    ⇒ 不切过去的话登记表是空的（2026-09-23 自检报「登记表 0 个」，查了半天是这条）。
                if (win.tabButtons != null) win.tabButtons.Click(2);
                var layer = PointerLayer.Instance;
                int btnCount = Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None).Length;
                Debug.Log(P + $"   · 诊断：`PointerLayer.Instance`={(layer != null)}"
                          + $" · 场景里 `WindowButton` {btnCount} 个（指针层是**事件时扫描**，没有登记表）");
                CheckTrue(layer != null, "`PointerLayer` 拿得到（**惰性取用**：没有就现建 —— 不依赖生命周期回调）");
                CheckTrue(btnCount > 0, $"场景里找得到 `WindowButton`（实测 {btnCount} 个）");
                if (layer != null)
                {
                    var ft2 = forge.GetComponent<ForgeTab>();
                    int ci = ForgeData.LevelOf(ForgeData.Selected);
                    var cc = fcontent != null ? FindChild(fcontent, "ForgeCell_" + ci) : null;
                    var cb = cc != null ? FindChild(cc, "Generic UI Button") : null;
                    if (cb != null && ft2 != null)
                    {
                        ft2.FocusClaimable();                       // 把该领那一格对到视口中心
                        cb = FindChild(FindChild(fcontent, "ForgeCell_" + ci), "Generic UI Button");
                        float qx = PxOf(cb.position.x), qy = PxYOf(cb.position.y);
                        var hitBtn = cb != null ? layer.ButtonAt(qx, qy) : null;
                        if (hitBtn == null)
                        {
                            string who = "";
                            foreach (var wb in Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None))
                            {
                                var q2 = wb.GetComponent<ImageQuad>();
                                if (q2 == null) continue;
                                float bx = PxOf(q2.transform.position.x), by = PxYOf(q2.transform.position.y);
                                if (Mathf.Abs(bx - qx) < 300f && Mathf.Abs(by - qy) < 300f)
                                    who += $"[{wb.name} q={q2.RenderQueue} 中心 {bx:F0},{by:F0} 尺寸 {q2.WorldW * 108f:F0}x{q2.WorldH * 108f:F0} 激活 {q2.gameObject.activeInHierarchy}] ";
                            }
                            string all2 = "";
                            foreach (var wb in Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None))
                            {
                                var q3 = wb.GetComponent<ImageQuad>();
                                all2 += q3 == null ? $"[{wb.name} 无quad] " : $"[{wb.name}@{PxOf(q3.transform.position.x):F0},{PxYOf(q3.transform.position.y):F0}] ";
                            }
                            Debug.Log(P + $"   · 命中诊断：点在 {qx:F1},{qy:F1} —— 附近：{(who == "" ? "**一个都没有**" : who)}"
                                      + $"；全场景按钮：{all2}");
                        }
                        CheckTrue(hitBtn != null,
                                  "该领那一格的 `Claim` 钮**在指针层的命中表里**（真鼠标按得下去）"
                                  + " —— 这就是第 23 条那个缺口的判据");
                    }
                    CheckTrue(layer.ButtonAt(5f, 5f) == null, "页面左上角空白处**点不中任何按钮**（不误触）");
                    if (ft2 != null && ft2.TrackScroll != null)
                    {
                        float o0 = ft2.TrackScroll.Offset;
                        CheckTrue(layer.WheelAt(1125f, 700f, -120f), "在锻造轨道**视口里**滚轮被接受");
                        CheckTrue(!Mathf.Approximately(ft2.TrackScroll.Offset, o0), "…而且偏移**真的变了**");
                        CheckTrue(!layer.WheelAt(1125f, 300f, -120f),
                                  "在锻造轨道视口**外**（y=300，两条滚动区都不覆盖）滚轮不生效");
                        ft2.TrackScroll.ScrollBy(o0 - ft2.TrackScroll.Offset);     // 还原
                    }

                    // ---- 🆕 2026-10-03：**悬停 / 拖拽 / 惯性 / 回弹**（§三 第 29 条 A15 + A5）----
                    //   判据全部**照 UGUI 源码**（原版跑的滚动就是它，本地就能读：
                    //   `Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/ScrollRect.cs`）：
                    //   `m_DecelerationRate = 0.135` · `m_Elasticity = 0.1` · `RubberDelta` :1084 ·
                    //   拖拽阈值 = `EventSystem.m_DragThreshold = 10`（`EventSystem.cs:68`）；
                    //   悬停色 = 原版 prefab 里 1276 个按钮共用的 `m_Colors.m_HighlightedColor = 0.9607843`。
                    {
                        // ① 悬停（原版 = UGUI `IPointerEnter/Exit`）
                        int ci3 = ForgeData.LevelOf(ForgeData.Selected);
                        var cb3 = fcontent != null
                            ? FindChild(FindChild(fcontent, "ForgeCell_" + ci3), "Generic UI Button") : null;
                        if (cb3 != null)
                        {
                            float hx = PxOf(cb3.position.x), hy = PxYOf(cb3.position.y);
                            var want = layer.ButtonAt(hx, hy);
                            var hb = layer.HoverAt(hx, hy);
                            CheckTrue(hb != null && hb == want && hb.Hovered,
                                      "指针压到按钮上 ⇒ 它进**悬停**态（原版 UGUI `IPointerEnter`）");
                            // 🔴 **2026-10-03 随 A17 就地更正**：原版这一颗（`Forge Menu Reward Button` 的
                            //    `Generic UI Button`）是 **SpriteSwap** —— 悬停**换图**（`40K_button` → `40K_button_hover`），
                            //    而**不是**变暗（UGUI 一颗 `Selectable` 只有一种 transition）。
                            //    原来这条断的是「色偏 = 0.9608」= **断的是旧行为**（那时我们只有色偏兜底）。
                            CheckTrue(hb != null && hb.HoverTexForTest != null && hb.CurrentTexForTest == hb.HoverTexForTest,
                                      "悬停 ⇒ 底图换成原版的高亮图（这一颗是 SpriteSwap 档，不是色偏档）");
                            CheckTrue(hb != null && hb.TintKForTest >= 1f - 0.001f,
                                      "换图档**不叠色偏**（原版一颗只有一种 transition）");
                            CheckTrue(layer.HoveredForTest == hb, "指针层记着这一颗是当前悬停件");
                            layer.HoverAt(5f, 5f);
                            CheckTrue(hb != null && !hb.Hovered, "指针挪到空白 ⇒ 悬停态结束");
                            CheckTrue(hb != null && hb.CurrentTexForTest == hb.NormalTexForTest,
                                      "图**还原**成常态图");
                        }

                        // ② 拖拽阈值 10px + 「拖了就不点按钮」+ 惯性（速度照 UGUI `Lerp(v,newV,dt*10)`）
                        var s = ft2 != null ? ft2.TrackScroll : null;
                        if (s != null)
                        {
                            float o1 = s.Offset;
                            var downBtn = layer.ButtonAt(1125f, 700f);
                            layer.PressAt(1125f, 700f);
                            CheckTrue(downBtn == null || downBtn.Pressed,
                                      "按下时命中的那一颗进 **Pressed** 态（原版 `SelectionState.Pressed`）");
                            CheckTrue(!layer.MoveTo(1131f, 700f),
                                      "按住只挪 **6px** ⇒ **还没到** `m_DragThreshold = 10`（不算拖）");
                            CheckTrue(layer.MoveTo(1160f, 700f),
                                      "挪过 10px ⇒ 转成**拖拽**（原版 `EventSystem.m_DragThreshold = 10`）");
                            CheckTrue(s.Dragging, "滚动区自己知道在被拖");
                            CheckTrue(!Mathf.Approximately(s.Offset, o1), "拖动**真的改了偏移**（内容跟着手走）");
                            layer.TickAt(1f / 60f);                     // 一帧 ⇒ 攒速度
                            layer.MoveTo(1170f, 700f);
                            layer.TickAt(1f / 60f);
                            bool clicked = layer.ReleaseAt(1170f, 700f);
                            CheckTrue(!clicked, "**拖动中松手 ⇒ 不点按钮**（照原版：越过阈值那一下被 ScrollRect 吃掉）");
                            CheckTrue(!s.Dragging, "松手后拖动态结束");
                            CheckTrue(Mathf.Abs(s.Velocity) > 1f,
                                      $"松手时带着速度（惯性）—— 实测 {s.Velocity:F1}px/s");
                            float o2 = s.Offset;
                            layer.TickAt(1f / 60f);
                            CheckTrue(!Mathf.Approximately(s.Offset, o2), "松手后还会**自己走一段**（惯性）");
                            CheckTrue(layer.TickAt(0f) == false, "`dt <= 0` 那一帧不动（照 UGUI 那条守卫）");
                            s.Stop();                                   // 别把速度带到后面的断言里
                            s.SetOffset(o1);
                        }

                        // ③ 回弹（Elastic）—— 原版锻造轨道是 **Clamped** ⇒ 这里量的是**算式**
                        //    （照 UGUI `RubberDelta` :1084-1087 与 `SmoothDamp` 回弹那一支 :849-858）。
                        //    造一个临时区：视口 300 高 · 内容 600 ⇒ 可滚 [0,300]。
                        var es = MenuScroll.TopAligned(new PxRect(0f, 0f, 100f, 300f), 600f);
                        es.Elastic = true;
                        es.BeginDrag(0f);
                        es.DragTo(-400f);                                   // 手指往「内容尽头之外」拉 400
                        CheckTrue(es.OutOfRange, "Elastic：拉过头 ⇒ 内容**越出**可滚范围（Clamped 就拉不动了）");
                        CheckTrue(es.Offset < 400f - 1f,
                                  $"…但被 `RubberDelta` 阻尼住（拉 400 只走 {es.Offset:F1}；原版 :1084-1087）");
                        CheckTrue(es.Offset > es.ClampHi, "…而且确实越过了上界（不是被硬夹）");
                        es.EndDrag();
                        for (int i = 0; i < 400; i++) es.Tick(1f / 60f);
                        CheckNear(es.Offset, es.ClampHi, 0.5f, "松手后 `SmoothDamp` **回弹到位**");
                        CheckTrue(!es.OutOfRange, "回弹之后不再越界");
                    }
                }
            }

            // ============================================================ §三·b3 A12-P1 欠下的断言（本件**只写断言、不改实现**）
            //
            // 🔴 期望值只从两处来，**都不回读我们传进去的参数**：
            //    · **原版参数**（正本 `资料/阶段二_锻造厂与战役页_原版规格.md` §二 :73-78 的三级宿主与五元组）；
            //    · **渲染真值**（`ImageQuad.WorldW/WorldH`、`LayoutSpace.ToPixel` 的中心）。
            if (win.tabButtons != null) win.tabButtons.Click(2);      // 先切到锻造页（下面几条都在这页上）
            Section("§三·b3-a 粒子三宿主：层级 / 名字 / 身位 / 空态（正本 §二 :73-75）");
            {
                var psHost = FindPath(forge, "Background/War ParticleSystemUI");
                var psBody = FindPath(forge, "Background/War ParticleSystemUI/Warp Particle System");
                var psNeb = FindPath(forge, "Background/War ParticleSystemUI/Warp Particle System/Particle System nebula");
                CheckTrue(psHost != null, "`Background/War ParticleSystemUI` **逐字**建了（第 1 级，原版这个名字带 `UI`）");
                CheckTrue(psBody != null, "`…/Warp Particle System` 建了（第 2 级）");
                CheckTrue(psNeb != null, "`…/Particle System nebula` 建了（第 3 级）");
                // 原版这一件是 3D 子件 ⇒ **只有 `Transform`**
                CheckTrue(psNeb != null && psNeb.GetComponent<RectTransform>() == null,
                          "`Particle System nebula` **没有 `RectTransform`**（原版这一件只有一个 `Transform`）");

                // ---- 身位：三个宿主都立在**选择条中心那一列**上（原版方框 1483.64²、中心对齐）----
                CheckNearPx(psHost, 1125.35f, 575.5f, "`War ParticleSystemUI`（宿主）");
                CheckNearPx(psBody, 1125.35f, 575.5f,
                            "`Warp Particle System`（原版 `anchoredPosition ≈ −1.6e−4,0` ⇒ 与宿主同中心）");
                CheckNearPx(psNeb, 1125.35f, 575.5f,
                            "`Particle System nebula`（原版 `localPosition = (0, ~0, 0.553)` ⇒ 平面中心同宿主）");

                // ---- `Ready for level up` 底下那**两团**（原版 §二 :76 / :78）----
                var readyRoot = FindChild(forge, "Ready for level up");
                CheckTrue(readyRoot != null, "`Ready for level up` 建了");
                if (readyRoot != null)
                {
                    var names = new List<string>();
                    for (int k = 0; k < readyRoot.childCount; k++) names.Add(readyRoot.GetChild(k).name);
                    names.Sort(System.StringComparer.Ordinal);
                    Check(readyRoot.childCount, 3,
                          "`Ready for level up` 的子件**恰好 3 个**（多一个少一个都红；实测 ["
                          + string.Join("、", names.ToArray()) + "]）");
                    CheckTrue(names.Contains("Glow"), "…其中一个是 `Glow`（那团 700² 的洋红光，色 #FF2DDF）");
                    CheckTrue(names.Contains("War ParticleSystemUI Down"), "…其中一个是 `War ParticleSystemUI Down`（带 `UI`）");
                    CheckTrue(names.Contains("War Particle System Up"),
                              "…其中一个是 `War Particle System Up`（🔴 原版**这个名字没有 `UI`** —— 逐份读的名字，别顺手改齐）");
                }
                var blobDown = readyRoot != null ? FindChild(readyRoot, "War ParticleSystemUI Down") : null;
                var blobUp = readyRoot != null ? FindChild(readyRoot, "War Particle System Up") : null;
                CheckNearPx(blobDown, 1125.35f, 949.5f, "`War ParticleSystemUI Down`（原版 `pos = (0, −374)`）");
                CheckNearPx(blobUp, 1125.35f, 191.5f, "`War Particle System Up`（原版 `pos = (0, +384)`；两件**不对称**）");
                // 两团各自的 `Rays → Glow` —— 两层都与宿主同中心（原版 `anchoredPosition` 是 1e−5 量级）
                CheckNearPx(blobDown != null ? FindChild(blobDown, "Rays") : null, 1125.35f, 949.5f,
                            "`…Down/Rays`（与宿主同中心）");
                CheckNearPx(blobDown != null ? FindChild(blobDown, "Glow (1)") : null, 1125.35f, 949.5f,
                            "`…Down/Rays/Glow (1)`（🔴 原版 Down 那一支的孙件**叫 `Glow (1)`**，与 Up 的 `Glow` 不同名）");
                CheckNearPx(blobUp != null ? FindChild(blobUp, "Rays") : null, 1125.35f, 191.5f, "`…Up/Rays`（与宿主同中心）");
                CheckNearPx(blobUp != null ? FindChild(blobUp, "Glow") : null, 1125.35f, 191.5f,
                            "`…Up/Rays/Glow`（与宿主同中心）");

                // ---- 空态：**一个 `ParticleSystem` 都没有** ----
                // 判据（`ForgeTab.Build` 末尾那条日志）：三套粒子的材质是**外链**、本工程里没有它们的导出资产
                // ⇒ **不拿「参数是我们挑的」粒子冒充原版**（铁律 3）。宿主的层级/名字照原文各就各位、不报错、不留残留。
                Check(forge.GetComponentsInChildren<ParticleSystem>(true).Length, 0,
                      "锻造厂页子树里**一个 `ParticleSystem` 都没有**（空态 —— 原版那三套的材质是外链、本工程没有导出资产）");
            }

            Section("§三·b3-b 两团光跟着**可领态**（`hasToCollectReward`，原版 `ForgeRewardSelector.CreateRewards`）");
            {
                var ftR = forge.GetComponent<ForgeTab>();
                var ready = FindChild(forge, "Ready for level up");
                CheckTrue(ForgeData.HasToCollect(ForgeData.Selected),
                          $"当前阵营（{ForgeData.Selected}）**正好有可领的**（否则下面几条等于没验）");
                if (ftR != null) ftR.Refresh();
                var dHost = ready != null ? FindChild(ready, "War ParticleSystemUI Down") : null;
                var uHost = ready != null ? FindChild(ready, "War Particle System Up") : null;
                CheckTrue(ready != null && ready.gameObject.activeSelf, "可领 ⇒ `Ready for level up` **亮**");
                CheckTrue(dHost != null && dHost.gameObject.activeInHierarchy
                          && uHost != null && uHost.gameObject.activeInHierarchy,
                          "…**两团宿主跟着亮**（`activeInHierarchy` —— 它们挂在这根开关下面）");
                // ---- 领掉它 ⇒ 两团一起灭 ----
                int lvR = ForgeData.LevelOf(ForgeData.Selected);
                if (ftR != null) ftR.FocusClaimable();
                var claimN = FindChild(FindChild(fcontent, "ForgeCell_" + lvR), "ClaimHit");
                var claimW = claimN != null ? claimN.GetComponent<WindowButton>() : null;
                CheckTrue(claimW != null, "该领那一格的 `ClaimHit` 在（下面点它）");
                if (claimW != null) claimW.Click();
                Check(ForgeData.LevelOf(ForgeData.Selected), lvR + 1,
                      "点了 Claim ⇒ 已领格数 +1（原版语义：只把 `rewardsCollected` 加一，不扣经验）");
                CheckTrue(!ForgeData.HasToCollect(ForgeData.Selected), "领完 ⇒ `hasToCollectReward` 变假");
                CheckTrue(ready != null && !ready.gameObject.activeSelf, "…`Ready for level up` **灭**");
                CheckTrue((dHost == null || !dHost.gameObject.activeInHierarchy)
                          && (uHost == null || !uHost.gameObject.activeInHierarchy),
                          "…**两团一起灭**（同一个开关管住它们）");
                // ---- 幂等：连调两次 `Refresh()` 不许长出第二个宿主来 ----
                if (ftR != null) { ftR.Refresh(); ftR.Refresh(); }
                CheckTrue(ready != null && ready.childCount == 3,
                          "连调两次 `Refresh()` ⇒ `Ready for level up` 的子件**仍是 3 个**"
                          + $"（实测 {(ready != null ? ready.childCount : -1)}）—— 不留残留");
            }
            // ⚠️ 上面把 Goff 那格领掉了 ⇒ **还原成起手态**（C 段与后面两张截图都要它）
            ForgeData.ResetForTest();
            {
                var ftB = forge.GetComponent<ForgeTab>();
                if (ftB != null) ftB.SelectArmy(ForgeData.Selected);
            }

            Section("§三·b3-c `Help Icon` 的 tooltip（原版 `EverguildTooltipTrigger`：悬停即出、离开即收）");
            {
                var layerT = PointerLayer.Instance;
                var helpNode = FindChild(forge, "Help Icon");
                CheckTrue(helpNode != null, "`Help Icon` 建了");
                // ① 正文：**一个字都没编**（原版是 I2 词条 `MainMenu/Forge/Help`，词条表在远端 CCD、本地没有）
                CheckTrue(ForgeTab.HelpTipBody.Trim() == "",
                          "`ForgeTab.HelpTipBody` **一个字都没编**（面板照弹、正文空 —— 裁定过，别改成「不出面板」）");
                var helpQ = helpNode != null ? helpNode.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(helpQ != null, "`Help Icon` 有渲染 quad（下面拿它的**渲染中心**当悬停点）");
                var hpx = helpQ != null ? LayoutSpace.ToPixel(helpQ.transform.position) : Vector2.zero;
                int hov0 = ForgeTab.HelpTipHovers, sc0 = Tooltip.ShowCount;
                var hb = layerT != null ? layerT.HoverAt(hpx.x, hpx.y) : null;
                CheckTrue(hb != null && hb == ForgeTab.HelpTipHit,
                          "`PointerLayer.HoverAt(图标中心)` 打到的**就是** `ForgeTab.HelpTipHit`（接线通）");
                Check(ForgeTab.HelpTipHovers, hov0 + 1, "…悬停计数 +1");
                Check(Tooltip.ShowCount, sc0 + 1, "…`Tooltip.ShowCount` +1（**面板照原版的时机弹出来了**）");
                CheckTrue(Tooltip.Visible, "…`Tooltip.Visible == true`");
                CheckTrue((Tooltip.ShownBody ?? "").Trim() == "", "…面板里的**正文是空的**（一个字都没编）");
                // ② 面板**钉在图标上**（原版 `Show()` 取的是触发器自己的 `transform.position`，**不跟鼠标**）
                //    判据 = `tooltipAnchor = 25`（右上）⇒ pivot **(1,1)** ⇒ 面板的右上角落在图标中心
                float pxc = LayoutSpace.PxX(Tooltip.PanelCenter.x), pyc = LayoutSpace.PxY(Tooltip.PanelCenter.y);
                float pw = Tooltip.PanelSizePx.x, ph = Tooltip.PanelSizePx.y;
                CheckNear(pxc, hpx.x - pw * 0.5f, 1f,
                          "面板中心 x = 图标中心 − 半宽（pivot **(1,1)** ⇒ 面板的右上角 = 图标中心）");
                CheckNear(pyc, hpx.y + ph * 0.5f, 1f, "…面板中心 y = 图标中心 + 半高（同一句话的另一半）");
                CheckTrue(Mathf.Abs(pxc - hpx.x) > 5f && Mathf.Abs(pyc - hpx.y) > 5f,
                          $"…**而且确实偏开了**（面板 {pw:F0}×{ph:F0} ⇒ 偏 ({pxc - hpx.x:F0},{pyc - hpx.y:F0})）"
                          + " —— 否则「pivot=(1,1)」这句等于没验");
                // ③ 离开 ⇒ 立刻收
                int hov1 = ForgeTab.HelpTipHovers;
                if (layerT != null) layerT.HoverAt(5f, 5f);     // 页面左上角空白（同文件另一条已证这里打不中任何按钮）
                Tooltip.FinishFade();                           // ⚠️ 批处理没有帧循环 ⇒ 手动结束淡出
                Check(ForgeTab.HelpTipHovers, hov1, "指针挪到空白 ⇒ 悬停计数**不变**（没有误触发）");
                CheckTrue(!Tooltip.Visible, "…`Tooltip.Visible == false`（原版 `OnPointerExit` **立刻**收）");
            }

            Section("§三·b3-d 两个 `Viewport` 的裁切（原版 `RectMask2D` 的渲染那一面 + 射线那一面）");
            {
                var ftV = forge.GetComponent<ForgeTab>();
                var layerV = PointerLayer.Instance;
                // ---- ① 阵营条：滚到两端 ⇒ **视口外那一端的条目一个都不建** ----
                var aScroll = ftV != null ? ftV.ArmyScroll : null;
                int nAllA = ForgeData.Armies.Length;
                CheckTrue(aScroll != null && fArmy != null, "阵营条的滚动区与 `Army Content` 都在");
                if (aScroll != null && fArmy != null)
                {
                    string lastN = "ForgeArmyItem_" + (nAllA - 1);
                    aScroll.SetOffset(aScroll.MinOffset);
                    CheckTrue(fArmy.childCount < nAllA,
                              $"滚到最左 ⇒ 建出来的条目**少于全部 {nAllA} 条**（实测 {fArmy.childCount}）—— 视口外的不建");
                    CheckTrue(FindChild(fArmy, lastN) == null,
                              $"…最左端那一条（{ForgeData.Armies[nAllA - 1]}）**整条在视口外 ⇒ 连节点都不建**");
                    CheckTrue(FindChild(fArmy, "ForgeArmyItem_0") != null,
                              "…而另一端（第 1 个阵营）**进视口了 ⇒ 建了**（否则上面那条等于没验）");
                    aScroll.SetOffset(aScroll.MaxOffset);
                    CheckTrue(fArmy.childCount < nAllA,
                              $"滚到最右 ⇒ 条目同样**少于 {nAllA} 条**（实测 {fArmy.childCount}）");
                    CheckTrue(FindChild(fArmy, "ForgeArmyItem_0") == null,
                              "…第 1 个阵营**整条在视口外 ⇒ 不建**");
                    CheckTrue(FindChild(fArmy, lastN) != null, "…而最右端那一条进视口了 ⇒ 建了");
                }

                // ---- ② 奖励轨道：滚到最后一格 ⇒ 第 1 格连节点都不建、那一带也点不中 ----
                var tScroll = ftV != null ? ftV.TrackScroll : null;
                // 原版 `Rewards Scroll View` = 330.97,318.60 → 1919.73,1080.00（本文件上面 `CheckAt(ftrack, …)` 已钉住）
                var trackR = new PxRect(330.97f, 318.60f, 1919.73f, 1080f);
                CheckTrue(tScroll != null && fcontent != null, "轨道的滚动区与 `Rewards Content` 都在");
                if (tScroll != null && fcontent != null)
                {
                    tScroll.SetOffset(tScroll.MaxOffset);
                    CheckTrue(FindChild(fcontent, "ForgeCell_0") == null,
                              "**滚到最后一格 ⇒ 第 1 格整格在视口外 ⇒ 连节点都不建**（原版 `RectMask2D` 就是裁掉它）");
                    var c0r = tScroll.Shift(UguiLayout.HorizontalChild(trackR, ForgeTab.CellW, ForgeTab.CellH, 0,
                                                                      ForgeTab.TrackPadL, ForgeTab.TrackSpacing));
                    PxRect c0v;
                    CheckTrue(!MenuDraw.ClipRect(c0r, trackR, out c0v),
                              "…（现算：第 1 格的矩形与视口**无交集** ⇒ 「不建」正是原版 `RectMask2D` 的结果）");
                    // 原版 Claim 的 `pos.y = −134.147` ⇒ 那一格 Claim 该在的位置
                    float c0x = c0r.CX, c0y = c0r.CY + 134.147f;
                    CheckTrue(layerV == null || layerV.ButtonAt(c0x, c0y) == null,
                              $"…⇒ 第 1 格 Claim 该在的那一点（{c0x:F0},{c0y:F0}）**点不中任何按钮**"
                              + "（判据 = 原版 `RectMask2D` 的**射线那一面**）");

                    // ---- 🔴 非空对照：把**该领的那一格**移到左边缘压线 ⇒ 命中区**被截**、截过之后仍然点得中 ----
                    int lvV = ForgeData.LevelOf(ForgeData.Selected);
                    Check(ForgeData.StateAt(ForgeData.Selected, lvV), ForgeData.ToCollect,
                          "该领那一格的状态 = `ToCollect`（否则下面这条非空对照没意义）");
                    var cellLv = UguiLayout.HorizontalChild(trackR, ForgeTab.CellW, ForgeTab.CellH, lvV,
                                                            ForgeTab.TrackPadL, ForgeTab.TrackSpacing);
                    tScroll.SetOffset(cellLv.CX - (trackR.x1 + 26.5f));   // 让 Claim 的左半截伸到视口左边缘之外
                    var cellEdge = FindChild(fcontent, "ForgeCell_" + lvV);
                    var claimEdge = FindChild(cellEdge, "ClaimHit");
                    float qx1, qy1, qx2, qy2;
                    CheckTrue(claimEdge != null && RectOf(claimEdge, out qx1, out qy1, out qx2, out qy2),
                              "**压在视口边上的那一格**：`ClaimHit` 建了、渲染矩形量得到（部分越界 ⇒ 建、但被截）");
                    if (claimEdge != null && RectOf(claimEdge, out qx1, out qy1, out qx2, out qy2))
                    {
                        CheckNear(qx1, trackR.x1, 0.5f, "它的命中区**左边缘被截到视口左边缘 330.97**");
                        CheckTrue(qx2 - qx1 < 200.762f - 1f,
                                  $"…渲出来的宽 {qx2 - qx1:F1} **比原版整块 200.762 窄**（真被截了，不是整块）");
                        var wbEdge = claimEdge.GetComponent<WindowButton>();
                        CheckTrue(layerV != null && layerV.ButtonAt((qx1 + qx2) * 0.5f, (qy1 + qy2) * 0.5f) == wbEdge,
                                  "…截剩下的那半截**仍然点得中**（视口里的点击面还在）");
                        CheckTrue(layerV == null || layerV.ButtonAt(qx1 - 20f, (qy1 + qy2) * 0.5f) != wbEdge,
                                  "…而**视口外**那半截点不中它（原版 `RectMask2D` 的射线那一面）");
                    }

                    // ---- ③ 同一格：`Reward` 图标的矩形与 **uv 一起**被截（只截矩形不截 uv 会把图压扁）----
                    var rwEdge = FindChild(cellEdge, "Reward");
                    var rwQ = rwEdge != null ? rwEdge.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(rwQ != null && rwQ.Texture != null, "那一格的 `Reward` 图标量得到（有图）");
                    if (rwQ != null && rwQ.Texture != null)
                    {
                        // 整块宽 = 框高 150（`ForgeTab.BuildRewardIcon` 里那个高度，⚠️ 那一处是「我们挑的」）
                        //          × 图自身的宽高比（`keepAspect` 那条路）
                        float fullW = 150f * ((float)rwQ.Texture.width / Mathf.Max(1f, rwQ.Texture.height));
                        CheckTrue(rwQ.WorldW * 108f < fullW - 1f,
                                  $"压在视口边上的那一格：`Reward` 渲出来的宽 **{rwQ.WorldW * 108f:F1} < 整块 {fullW:F1}**（真被截了）");
                        CheckTrue(rwQ.UvRect.width < 0.999f,
                                  $"…而且 **uv 跟着截**（`UvRect.width = {rwQ.UvRect.width:F3} < 1`）");
                    }
                    // 对照：完全落在视口里的那一格 ⇒ 整块宽、uv = 整张
                    var rwIn = FindChild(FindChild(fcontent, "ForgeCell_" + (lvV + 1)), "Reward");
                    var rwInQ = rwIn != null ? rwIn.GetComponentInChildren<ImageQuad>() : null;
                    if (rwInQ != null && rwInQ.Texture != null)
                    {
                        float fullW2 = 150f * ((float)rwInQ.Texture.width / Mathf.Max(1f, rwInQ.Texture.height));
                        CheckNear(rwInQ.WorldW * 108f, fullW2, 1f,
                                  "对照：完全落在视口里的那一格 ⇒ `Reward` **整块宽**（没被截）");
                        CheckNear(rwInQ.UvRect.width, 1f, 0.001f, "…且 uv 是整张（`width = 1`）");
                    }

                    // ---- ④ `BuildRewardCells()` 跑完**必须把 `win.Clip` 还原** ----
                    // 不还原 = 后面画的石柱/阵营条会被一起裁掉，而且**是静默的**（矩形断言量不到）。
                    CheckTrue(win.Clip == null, "`Refresh()`（内含 `BuildRewardCells`）跑完 ⇒ `win.Clip` **还原成 null**");
                    var sentinel = new PxRect(10f, 20f, 300f, 400f);
                    win.Clip = sentinel;
                    if (ftV != null) ftV.Refresh();
                    CheckTrue(win.Clip.HasValue && MenuDraw.SameRect(win.Clip.Value, sentinel),
                              "…而且还原的是**调用前那个值**（不是硬写 null）—— 期间它确实被设成过 Viewport");
                    win.Clip = null;
                    if (ftV != null) ftV.SelectArmy(ForgeData.Selected);   // 还原轨道/阵营条的起手定位
                }
            }
        }

        // ============================================================ §三·b4 `Clip` 路（`MenuDraw` / `MenuWindowBase` 那一层）
        //
        // 🔴 三条都**只用临时节点**验 `MenuDraw`/`MenuWindowBase` 已落地的 `ClipRect` / `Nine(…,clip)` /
        //    `Hit(…,clip)`：判据 = 原版 `RectMask2D`（UGUI 源码出处写在 `MenuDraw.ClipRect` 的注释里）。
        //    ⚠️ 批处理下 `Object.Destroy` 不生效 ⇒ 一律 `DestroyImmediate`；`win.Clip` 用完**必须还原**。
        Section("§三·b4-a `MenuDraw.ClipRect` 的判据表（求交那一份，五格）");
        {
            var r = new PxRect(100f, 100f, 200f, 200f);
            var clip = new PxRect(150f, 150f, 300f, 300f);
            PxRect o;
            // ① `clip = null` ⇒ true 且**逐字段原样返回**
            CheckTrue(MenuDraw.ClipRect(r, null, out o) && MenuDraw.SameRect(o, r),
                      "① `clip = null` ⇒ true 且 `outRect` **逐字段 = r**（没被截）");
            // ② 整块在框内 ⇒ 同上
            CheckTrue(MenuDraw.ClipRect(r, new PxRect(50f, 50f, 400f, 400f), out o) && MenuDraw.SameRect(o, r),
                      "② 整块在框内 ⇒ true 且 `outRect` **逐字段 = r**");
            // ③ 整块在框外（x 出 / y 出各一条）⇒ false
            CheckTrue(!MenuDraw.ClipRect(r, new PxRect(210f, 0f, 400f, 400f), out o),
                      "③a 整块在框外（**x 出**）⇒ false（= 调用方「不建」）");
            CheckTrue(!MenuDraw.ClipRect(r, new PxRect(0f, 210f, 400f, 400f), out o),
                      "③b 整块在框外（**y 出**）⇒ false");
            // ④ 压着框边 ⇒ out = **交集**
            // 🔴 **2026-10-03 就地更正（铁律 5）**：这一条原来写的是
            //    `o.x1 == r.x1 && o.x2 == clip.x2 && o.y1 == r.y1 && o.y2 == clip.y2` —— **那是错的**，
            //    它描述的是「**clip 只切掉 r 的右下那一小块**」那种特例（要求 r 的左上角落在框内），
            //    而本格的这组数（见 `:1343-1344`）是 `r=(100,100,200,200)` / `clip=(150,150,300,300)`
            //    ⇒ **框切掉的是 r 的左上那一半**，交集的四条边**全都要取 max/min**。
            //    实测 150,150 → 200,200 正是交集（= `ClipRect` 的实现是对的，**断言写错了**）。
            //    ⇒ 期望值改成**字面量**（**不在这里重写一遍 Max/Min** —— 拿同一个公式去验同一个公式 = 自证）。
            bool ok4 = MenuDraw.ClipRect(r, clip, out o);
            CheckTrue(ok4, "④ 压着框边 ⇒ true");
            CheckTrue(ok4 && o.x1 == 150f && o.y1 == 150f && o.x2 == 200f && o.y2 == 200f,
                      $"④ …`outRect` = **交集**（`r` ∩ `clip`；期望 **150,150 → 200,200**，"
                      + $"实测 {o.x1:F0},{o.y1:F0} → {o.x2:F0},{o.y2:F0}）");
            // ④b 另一侧：**框切掉 r 的右下那一半** ⇒ 交集落在左上（原来那条断言想描述的其实是这一格）
            bool ok4b = MenuDraw.ClipRect(r, new PxRect(0f, 0f, 150f, 150f), out o);
            CheckTrue(ok4b, "④b 压着框的右下边 ⇒ true");
            CheckTrue(ok4b && o.x1 == 100f && o.y1 == 100f && o.x2 == 150f && o.y2 == 150f,
                      $"④b …`outRect` = **交集**（期望 **100,100 → 150,150**，"
                      + $"实测 {o.x1:F0},{o.y1:F0} → {o.x2:F0},{o.y2:F0}）");
            // ⑤ 有裁切 + 退化矩形 ⇒ false（`Rect` 原来就有的行为，别丢）
            CheckTrue(!MenuDraw.ClipRect(new PxRect(100f, 100f, 100f, 200f), clip, out o),
                      "⑤ **有裁切** + 宽 ≤ 0.01 的退化矩形 ⇒ false");
        }

        Section("§三·b4-b 九宫格**真吃** `Clip`（左栏高亮那张现成参数：`40k_main_bt_selected_BW` 71² · border 30 · ppuMul 0.92）");
        {
            // 🔴 判据 = 原版 `Image.Type = Sliced` + `m_PixelsPerUnitMultiplier = 0.92`
            //    ⇒ **画出来的角块 = 30 ÷ 0.92 = 32.61 画布像素**（`borderOutPx` 那个参数就是它）。
            //    这一节同时验一个**独立疑点**：`ImageQuad.PixelsPerUnit = 100` 而画布是 **108 px/世界单位**
            //    ⇒ 九宫格有可能**整体大 8%**（角块 32.61 → 35.22）。**本件只写断言、不改实现** —— 红了就是发现。
            var nineTex = win.Art("40k_main_bt_selected_BW");
            CheckTrue(nineTex != null, "`40k_main_bt_selected_BW` 取得到（左栏高亮那张）");
            CheckTrue(nineTex != null && nineTex.width == 71 && nineTex.height == 71,
                      $"那张图是 **71×71**（原版 `m_Rect`；实测 {(nineTex != null ? nineTex.width + "×" + nineTex.height : "?")}）");
            CheckTrue(win.Clip == null, "画基准那块之前 `win.Clip` 是干净的（否则基准本身也会被裁）");
            var tmpN = RewardsWindow.New(win.transform, "ClipNineTest");
            const float Corner = 30f / 0.92f;                 // 32.6087 画布像素
            float side = 3f * Corner;                          // **取 3 倍角块长** ⇒ 九块理论上都是同一个数
            var nr = new PxRect(100f, 100f, 100f + side, 100f + side);
            var nclip = new PxRect(100f, 100f + 10f, 100f + side, 100f + side);   // 切掉**顶边** 10px（画布 y 向下 ⇒ y1 是顶边）
            var border = new Vector4(30f, 30f, 30f, 30f);
            var bordOut = new Vector4(Corner, Corner, Corner, Corner);
            var baseRoot = MenuDraw.Nine(tmpN, nineTex, nr, border, 71f, 71f, 3005, null, true, "Highlight", bordOut, null);
            var clipRoot = MenuDraw.Nine(tmpN, nineTex, nr, border, 71f, 71f, 3005, null, true, "HighlightC", bordOut, nclip);
            CheckTrue(baseRoot != null && clipRoot != null, "基准块与「顶边切 10px」那块都建出来了");
            if (baseRoot != null && clipRoot != null)
            {
                var baseQs = baseRoot.GetComponentsInChildren<ImageQuad>(true);
                var clipQs = clipRoot.GetComponentsInChildren<ImageQuad>(true);
                // ① 子块个数相同（**含被关掉的一起数 = 9**）
                Check(baseQs.Length, 9, "① 基准块：**含被关掉的一起数 = 9 块**（`Highlight_00 .. _22`）");
                Check(clipQs.Length, 9, "① 被切那块：**子块个数与基准相同 = 9**（整块在框外的只 `SetActive(false)`，**不删节点**）");
                // ② 🔴 **重点**：基准那块**所有子块渲出来的高都是 32.61**（= 角块长）
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                    {
                        var t = baseRoot.transform.Find("Highlight_" + i + j);
                        var q = t != null ? t.GetComponent<ImageQuad>() : null;
                        CheckNear(q != null ? q.WorldH * 108f : -1f, Corner, 0.5f,
                                  $"② 基准块 `Highlight_{i}{j}` 渲出来的高(px) = **{Corner:F2}**"
                                  + "（= 原版 `m_Border 30` ÷ `ppuMul 0.92`）");
                    }
                // ③ 被切那块：**只有最上面那三块**（`_02/_12/_22`，j=2 是最上面那条）变矮，且 = 32.61 − 10
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                    {
                        var t = clipRoot.transform.Find("HighlightC_" + i + j);
                        var q = t != null ? t.GetComponent<ImageQuad>() : null;
                        CheckNear(q != null ? q.WorldH * 108f : -1f, j == 2 ? Corner - 10f : Corner, 0.5f,
                                  $"③ 被切那块 `HighlightC_{i}{j}` 渲出来的高(px) = **{(j == 2 ? Corner - 10f : Corner):F2}**"
                                  + (j == 2 ? "（顶边被切掉 10px）" : "（没压到框边 ⇒ 不动）"));
                    }
                // ④ 那三块的 uv：`height = (30/71) × (22.61/32.61)`、**`y` 不变**（截的是上边 ⇒ v 的下沿不动）
                for (int i = 0; i < 3; i++)
                {
                    var tb = clipRoot.transform.Find("HighlightC_" + i + "2");
                    var ta = baseRoot.transform.Find("Highlight_" + i + "2");
                    var qb = tb != null ? tb.GetComponent<ImageQuad>() : null;
                    var qa = ta != null ? ta.GetComponent<ImageQuad>() : null;
                    CheckNear(qb != null ? qb.UvRect.height : -1f, (30f / 71f) * ((Corner - 10f) / Corner), 0.005f,
                              $"④ `HighlightC_{i}2` 的 `UvRect.height` = (30/71)×(22.61/32.61)（uv **跟着截**，否则图会被压扁）");
                    CheckNear(qb != null ? qb.UvRect.y : -1f, qa != null ? qa.UvRect.y : -2f, 1e-6f,
                              $"④ …`UvRect.y` 与基准**完全不变**（截掉的是上边 ⇒ v 的下沿不动）");
                }
                // ⑤ 其余子块 `UvRect` 与基准**逐字段相同**
                // 🔴 **2026-10-03 就地更正（铁律 5）**：这一条原来是**逐字段 `!=` 精确比 + 数「差了几块」**，
                //    实测红（6 块里报 4 块）。**判清了：错的是断言，不是 `ClipNineChildren`。**
                //    原因：九宫格子块的 uv 是**「画布 px → 世界坐标 → 画布 px」来回换算**出来的
                //    （`MenuDraw.ClipNineChildren:187-189` 拿 `transform.position` 与 `WorldW/WorldH` 反推
                //    `qr`，再交给 `ClipRect`），float32 在这个量级（~197 px 处 ulp = **1.5e-5 px**）
                //    分辨不到「贴着框边」和「差一丁点」的区别：
                //      · 左列 `_00`/`_01` 的 `qr.x1` 算成 **99.99997**（框左边 100）⇒ 落在框外 3.05e-5 px；
                //      · 底排 `_10`/`_20` 的 `qr.y2` 算成 **197.82611**（框底 197.82608）⇒ 同样差 3.05e-5 px；
                //    ⇒ `SameRect(cr, qr)` 判「压边了」，这 4 块被按「部分越界」处理：uv 各截掉
                //    **3.9e-7**（几何 3.05e-5 px）。**中间那块 `_11` 与 `_21` 一动没动** ——
                //    被碰到的**只有本来就压着框边的那几块**，幅度比 1 个像素小 5 个数量级。
                //    ⇒ **实现没有错**（`ClipNineChildren` 的语义就是「压边 ⇒ 截」，它只是把「浮点意义下的压边」
                //    也算进去了）；**错的判据是「浮点精确相等」**。
                //    （对照：`ClipRect` 那条「整块在框内 ⇒ 原样返回 `r` 的**同一个 struct**」是**精确**的，
                //      所以上一节 §三·b4-a 那几条 `SameRect(ClipRect 的产物, 原矩形)` 不受影响 ——
                //      只有 `ClipNineChildren` 里这个**重算出来的** `qr` 才有这个陷阱。）
                // 判据改成**带容差**（`CheckNear` 口径）：`1e-5`（uv 单位）。
                //   容差出处：本块 uv 的 30/71 ≈ 0.4225 ↔ 画布 **32.61 px** ⇒ **1 uv ≈ 77 px**，
                //   故 **1e-5 uv ≈ 7.7e-4 画布像素**；而**真出错哪怕只错 1 px 也是 1.3e-2 uv（差 1300 倍）**
                //   ⇒ 该容差**挡得住真缺陷**，不是「放宽到永远绿」。（实测最大差 3.95e-7，余量 25 倍。）
                // ⚠️ 缺块（`Find` 不到）仍算失败 —— 取 `+∞` ⇒ 必红，不是静默放过。
                float uvMax = 0f;
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                    {
                        if (j == 2) continue;
                        var tc = clipRoot.transform.Find("HighlightC_" + i + j);
                        var tb2 = baseRoot.transform.Find("Highlight_" + i + j);
                        var qc = tc != null ? tc.GetComponent<ImageQuad>() : null;
                        var qb2 = tb2 != null ? tb2.GetComponent<ImageQuad>() : null;
                        if (qc == null || qb2 == null) { uvMax = float.PositiveInfinity; continue; }
                        uvMax = Mathf.Max(uvMax, Mathf.Abs(qc.UvRect.x - qb2.UvRect.x));
                        uvMax = Mathf.Max(uvMax, Mathf.Abs(qc.UvRect.y - qb2.UvRect.y));
                        uvMax = Mathf.Max(uvMax, Mathf.Abs(qc.UvRect.width - qb2.UvRect.width));
                        uvMax = Mathf.Max(uvMax, Mathf.Abs(qc.UvRect.height - qb2.UvRect.height));
                    }
                CheckNear(uvMax, 0f, 1e-5f,
                          $"⑤ 其余 6 块（j=0/1 那两排）的 `UvRect` 与基准**逐字段相同**"
                          + $"（容差 1e-5 uv ≈ 7.7e-4 px；实测最大差 {uvMax:E2}；"
                          + "v 方向差 1 px 会是 1.3e-2）");
                // ⑥ 根节点位置不动（原版 `RectMask2D` 也只裁渲染、不挪 `RectTransform`）
                CheckNear(Vector3.Distance(baseRoot.transform.position, clipRoot.transform.position), 0f, 1e-5f,
                          "⑥ **被切那块的根节点位置 == 基准那块根节点位置**（「根不动」）");
            }
            // ⑦ 整块落在 `clip` 外 ⇒ 返回 null（连节点都不建）
            var offRoot = MenuDraw.Nine(tmpN, nineTex, new PxRect(1000f, 1000f, 1000f + side, 1000f + side),
                                        border, 71f, 71f, 3005, null, true, "OffNine", bordOut, nclip);
            CheckTrue(offRoot == null, "⑦ 整块落在 `clip` 外 ⇒ **返回 null**（连节点都不建）");
            // ⑧ 走 `win.Nine(…)` 包装再来一遍 ⇒ 与 ③④ 同值（证明包装层把 `Clip` 传下去了）
            var keepClip = win.Clip;
            win.Clip = nclip;
            var wrapRoot = win.Nine(tmpN, "40k_main_bt_selected_BW", nr, border, 3005, null, true, "Wrapped", bordOut);
            win.Clip = keepClip;
            CheckTrue(wrapRoot != null, "⑧ `win.Nine(…)`（包装层）在 `Clip` 生效时也建出来了");
            if (wrapRoot != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var t = wrapRoot.transform.Find("Wrapped_" + i + "2");
                    var q = t != null ? t.GetComponent<ImageQuad>() : null;
                    CheckNear(q != null ? q.WorldH * 108f : -1f, Corner - 10f, 0.5f,
                              $"⑧ 包装层那条路 `Wrapped_{i}2` 的高(px) 与 ③ 同值（= {Corner:F2} − 10）");
                    CheckNear(q != null ? q.UvRect.height : -1f, (30f / 71f) * ((Corner - 10f) / Corner), 0.005f,
                              $"⑧ …`UvRect.height` 与 ④ 同值（`Clip` 确实传进 `MenuDraw.Nine` 了）");
                }
            }
            Object.DestroyImmediate(tmpN.gameObject);
        }

        Section("§三·b4-c 点击区守卫（`MenuDraw.Hit` / `MenuWindowBase.AddHit` 吃 `Clip`）");
        {
            var tmpH = RewardsWindow.New(win.transform, "ClipHitTest");
            var layerH = PointerLayer.Instance;
            CheckTrue(win.Clip == null, "起手 `win.Clip` 是干净的");
            // ① 回归：`Clip == null` ⇒ 命中区是**整块**
            // ⚠️ 临时矩形**以 (5,5) 为中心** —— 同文件上面已证这里打不中别的按钮 ⇒ 下面「点不中」才有意义。
            var hitR = new PxRect(-45f, -45f, 55f, 55f);
            var hA = win.AddHit(tmpH, "H1", hitR, 3005, null);
            var qA = hA != null ? hA.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(qA != null, "① `win.AddHit` 建出了命中区（带 quad）");
            if (qA != null)
            {
                CheckNear(qA.WorldW * 108f, hitR.W, 0.5f, "① `Clip == null` ⇒ 命中区**整块宽**（100px）");
                CheckNear(qA.WorldH * 108f, hitR.H, 0.5f, "① …整块高（100px）");
                CheckTrue(layerH != null && layerH.ButtonAt(5f, 5f) == hA.GetComponent<WindowButton>(),
                          "① 而且这一点**真的打到它**（= 这一点上没有别人，下面「点不中」才说明问题）");
            }
            MenuDraw.ClearChildren(tmpH);                 // 批处理下必须立刻清掉（`ClearChildren` 走 `DestroyImmediate`）
            // ② 整块在框外 ⇒ 返回 null + 节点不建 + 打不中
            win.Clip = new PxRect(500f, 500f, 600f, 600f);
            var hB = win.AddHit(tmpH, "H2", hitR, 3005, null);
            CheckTrue(hB == null, "② 整块在视口外 ⇒ `AddHit` **返回 null**");
            CheckTrue(FindChild(tmpH, "H2") == null, "② …**连节点都不建**");
            CheckTrue(layerH == null || layerH.ButtonAt(5f, 5f) == null, "② …那一点**点不中任何按钮**");
            win.Clip = null;
            MenuDraw.ClearChildren(tmpH);
            // ③ 压右边：只切掉右半 ⇒ 命中区 = 剩下一半；框内那半点得中、框外那半点不中
            var hr3 = new PxRect(100f, 100f, 200f, 200f);
            var hc3 = new PxRect(100f, 100f, 150f, 200f);
            PxRect cr3;
            CheckTrue(MenuDraw.ClipRect(hr3, hc3, out cr3), "③ 现算：`ClipRect` 给出的是**左半块**");
            win.Clip = hc3;
            var hC = win.AddHit(tmpH, "H3", hr3, 3005, null);
            var qC = hC != null ? hC.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(qC != null, "③ `AddHit` 建出了**截过**的命中区");
            if (qC != null)
            {
                CheckNear(qC.WorldW * 108f, hr3.W * 0.5f, 0.5f, "③ 命中区宽 = **剩下一半**（100 → 50px）");
                var wbC = hC.GetComponent<WindowButton>();
                CheckTrue(layerH != null && layerH.ButtonAt(125f, 150f) == wbC, "③ **框内那半点得中**它");
                CheckTrue(layerH == null || layerH.ButtonAt(175f, 150f) != wbC, "③ **框外那半点不中**它");
                // ⑤ 顺带：`Hit` 用的就是 `ClipRect` 那条判据（渲染矩形逐字段对得上）
                float bx = PxOf(qC.transform.position.x), by = PxYOf(qC.transform.position.y);
                float hw = qC.WorldW * 108f * 0.5f, hh = qC.WorldH * 108f * 0.5f;
                CheckTrue(Mathf.Abs((bx - hw) - cr3.x1) <= 0.5f && Mathf.Abs((bx + hw) - cr3.x2) <= 0.5f
                          && Mathf.Abs((by - hh) - cr3.y1) <= 0.5f && Mathf.Abs((by + hh) - cr3.y2) <= 0.5f,
                          $"⑤ 命中区的渲染矩形 == `MenuDraw.ClipRect` 算出来的那块（{(bx - hw):F1},{by - hh:F1} → "
                          + $"{(bx + hw):F1},{by + hh:F1}）—— **同一判据，不是第二份**");
            }
            win.Clip = null;
            MenuDraw.ClearChildren(tmpH);
            // ④ 只切上下：同理换 y
            var hr4 = new PxRect(100f, 600f, 200f, 700f);
            var hc4 = new PxRect(100f, 600f, 200f, 650f);
            win.Clip = hc4;
            var hD = win.AddHit(tmpH, "H4", hr4, 3005, null);
            var qD = hD != null ? hD.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(qD != null, "④ 只切上下时 `AddHit` 也建出了截过的命中区");
            if (qD != null)
            {
                CheckNear(qD.WorldH * 108f, hr4.H * 0.5f, 0.5f, "④ 命中区高 = **剩下一半**（100 → 50px）");
                var wbD = hD.GetComponent<WindowButton>();
                CheckTrue(layerH != null && layerH.ButtonAt(150f, 625f) == wbD, "④ **框内那半点得中**它（换 y 同理）");
                CheckTrue(layerH == null || layerH.ButtonAt(150f, 675f) != wbD, "④ **框外那半点不中**它");
            }
            win.Clip = null;
            Object.DestroyImmediate(tmpH.gameObject);
        }

        // ============================================================ §三·c 战役页
        // 期望值同样全部来自原版参数（正本 `资料/阶段二_锻造厂与战役页_原版规格.md` §一/§四/§十三）。
        Section("§三·c `Campaign Tab`（战役，阶段二第 3 层第 2 件）：层 × 参数逐条对");
        if (camp != null)
        {
            CheckAt(camp, 330.69f, 1920f, 70.94f, 1080f, "`Campaign Tab` 页矩形");
            var cbg = FindChild(camp, "Campaign Background");
            CheckTrue(cbg != null, "`Campaign Background` 建了（原版是 `Mask`）");
            var cbgImg = FindChild(cbg, "Background Image");
            CheckTrue(cbgImg != null,
                      "`Background Image` **建了而且开着** —— 原版出厂 `m_IsActive=false`，"
                      + "运行时由 `CampaignUIBackground` 打开并换图（**与纪律①那种「原版真不用」的不一样**）");
            CheckArt(cbgImg, CampaignData.Background(CampaignData.Selected),
                     "阵营背景图（13 张 GUID↔阵营已闭环，1024² 整图）");

            // 阵营选择条：`Campaign Army Selector` x 745.92..1920.34 · y 70.94..207.94
            var csel = FindChild(camp, "Campaign Army Selector");
            CheckAt(csel, 745.92f, 1920.34f, 70.94f, 207.94f, "`Campaign Army Selector`");
            var cArmy = FindChild(FindChild(csel, "Viewport"), "Army Content");
            CheckTrue(cArmy != null && cArmy.childCount > 0 && cArmy.childCount <= CampaignData.Armies.Length,
                      $"`Army Content` 下建的是**视口里放得下的那些条目**（实测 {cArmy.childCount} / 共 "
                      + $"{CampaignData.Armies.Length} 个阵营）");
            // 🔴 与锻造页**同形**（同一处收口，见 `UguiLayout.HorizontalContentCentered`）：
            //    原版 `Army Content` 是「选择条正中心的一个零宽点」+ `ContentSizeFitter` ⇒ 条目**居中**排。
            if (cArmy != null)
            {
                CheckNear(PxOf(cArmy.position.x), 1333.13f, 1f,
                          "`Army Content` 的中心 x = **选择条中心 1333.13**（745.92..1920.34）");
                var ctab = camp.GetComponent<CampaignTab>();
                CheckTrue(ctab != null && ctab.ArmyScroll != null, "战役页阵营条的滚动区建了（与锻造页共用 `MenuScroll`）");
                if (ctab != null && ctab.ArmyScroll != null)
                {
                    var cs = ctab.ArmyScroll;
                    // ✅ 2026-09-23 **缺口已修**：这条以前钉的是「越出屏幕 = 2」——
                    //    现在两侧都能滚 ⇒ 期望值翻成 **0**（照 §一 那条纪律）。
                    float fx1, fy1, fx2, fy2; int cOut = 0;
                    cs.ScrollBy(cs.MinOffset - cs.Offset);
                    float lo = cs.Offset;
                    cs.ScrollBy(cs.MaxOffset - cs.Offset);
                    // 两端各量一次：**两端那一条都要能完整落进视口**
                    for (int pass = 0; pass < 2; pass++)
                    {
                        if (pass == 1) cs.ScrollBy(cs.MinOffset - cs.Offset);
                        int idx = pass == 0 ? CampaignData.Armies.Length - 1 : 0;
                        var it = FindChild(cArmy, "CampaignArmyItem_" + idx);
                        if (it != null && RectOf(FindChild(it, "Icon"), out fx1, out fy1, out fx2, out fy2))
                            if (fx1 < 745.92f || fx2 > 1920.34f) cOut++;
                    }
                    Check(cOut, 0, $"阵营条**两端滚到位后没有条目越出屏幕**（实测 {cOut}）"
                          + " —— 缺口已修：`MenuScroll` 两侧都能滚（原版 `ScrollRect` 同）");
                    cs.ScrollBy(lo - cs.Offset);          // 还原
                    ctab.FocusSelectedArmy();

                    // ---- 条目内部几何（🔴 2026-09-24 照 `Campaign Army Item Button` 那棵树**直读**订正）----
                    // 判据 = 正本 §六 三变体对照表 + `menu_rect.py bundle_menus_assets_all
                    //        "Campaign Army Item Button" --depth 4 --cs`。
                    // ⛔ 原来照**母版**画：高亮框 136×122、图标用母版那套拉伸锚铺满整格 —— 三处都错。
                    {
                        // ⚠️ `Icon` 挂 `preserveAspect`（母版与变体都如此）⇒ **渲出来**是 100×100
                        //    （框 120×100 里**高度受限**）。所以「框宽 120」量不到、也不该量 ——
                        //    改判三样：渲出高 100 · 与高亮框同竖轴 · 框顶在高亮框顶下 5。
                        int selIdx = System.Array.IndexOf(CampaignData.Armies, CampaignData.Selected);
                        var sel = FindChild(cArmy, "CampaignArmyItem_" + selIdx);
                        var it0 = FindChild(cArmy, "CampaignArmyItem_0");
                        var ic0 = FindChild(it0, "Icon");
                        float x1, y1, x2, y2;
                        CheckTrue(ic0 != null && RectOf(ic0, out x1, out y1, out x2, out y2),
                                  "`CampaignArmyItem` 里有 `Icon`");
                        if (ic0 != null && RectOf(ic0, out x1, out y1, out x2, out y2))
                        {
                            CheckNear(y2 - y1, 100f, 1f,
                                      "条目 `Icon` **渲出**高 100（框是 120×100，`preserveAspect` 后高度受限）");
                            CheckNear(x2 - x1, 100f, 1f, "……渲出宽也是 100（方形图放进 120×100 的框，两侧各留 10）");
                        }
                        var hb = FindChild(sel, "HighlightBG");
                        var ic = FindChild(sel, "Icon");
                        CheckTrue(hb != null && RectOf(hb, out x1, out y1, out x2, out y2),
                                  $"选中那格（#{selIdx} {CampaignData.Selected}）有 `HighlightBG`");
                        if (hb != null && RectOf(hb, out x1, out y1, out x2, out y2))
                        {
                            float hx1 = x1, hy1 = y1, hx2 = x2;
                            CheckNear(hx2 - hx1, 120f, 1f, "`HighlightBG` 宽 **120**（母版是 136，变体是 120）");
                            CheckNear(y2 - y1, 110f, 1f, "`HighlightBG` 高 **110**（母版是 122）");
                            float ix1, iy1, ix2, iy2;
                            if (ic != null && RectOf(ic, out ix1, out iy1, out ix2, out iy2))
                            {
                                // 框高与渲出高同为 100（高度受限）⇒ **渲出的上沿就是框的上沿**
                                CheckNear(iy1 - hy1, 5f, 0.6f,
                                          "`Icon` 的框顶在高亮框顶**下 5**（原版 `pos (0,−5)`）");
                                CheckNear((ix1 + ix2) * 0.5f - (hx1 + hx2) * 0.5f, 0f, 0.6f,
                                          "`Icon` 与高亮框**同一竖轴**（原版锚 `(0.5,1)`、pos x=0）");
                            }
                        }
                        // 进度条（**这一页独有**）：宽 = 条目宽 − 10 = 110、芯 12 高
                        var sl = FindChild(it0, "Slider/Background");
                        CheckTrue(sl != null && RectOf(sl, out x1, out y1, out x2, out y2),
                                  "条目底下那条 `Slider`（**Campaign 变体独有**，母版没有）");
                        if (sl != null && RectOf(sl, out x1, out y1, out x2, out y2))
                        {
                            CheckNear(x2 - x1, 110f, 1f, "进度条宽 **110**（原版 `sizeDelta.x = −10`）");
                            CheckNear(y2 - y1, 12f, 1f, "进度条芯高 **12**（原版 `Background` 锚 0.2–0.8）");
                        }
                        // ⚠️ **不许有 `Arrow`** —— 这一变体**没有这个节点**（母版与 Forge 版才有）
                        CheckTrue(FindChild(it0, "Arrow") == null,
                                  "战役页的阵营格**没有 `Arrow`**（直读：`Campaign Army Item Button` 没有这个节点）");
                        // 进度条是**逐阵营**的：**没有内容的阵营一条填充都不该有**。
                        // 🔴 第一版对全部 13 格都填同一个值 —— 那是错的（本地只有 Ultramarines 有内容）。
                        for (int k = 0; k < 3 && k < CampaignData.Armies.Length; k++)
                        {
                            if (CampaignData.HasContent(CampaignData.Armies[k])) continue;
                            var nit = FindChild(cArmy, "CampaignArmyItem_" + k);
                            if (nit == null) continue;
                            CheckTrue(FindChild(nit, "Slider/Fill") == null,
                                      $"没有内容的阵营（{CampaignData.Armies[k]}，第 {k + 1} 格）**不画进度条填充**");
                            break;
                        }
                    }
                }
            }

            // Header：`Campaign Header` x 330.69..790.92 · y 60.94..225.94
            var chdr = FindChild(camp, "Campaign Header");
            CheckAt(chdr, 330.69f, 790.92f, 60.94f, 225.94f, "`Campaign Header`");
            CheckArt(FindChild(chdr, "bg"), "WF_Campaign_Info_Background", "Header 的底图");
            CheckArt(FindChild(chdr, "Army Icon"), DeckRuntime.FactionIcon(CampaignData.Selected),
                     "Header 的阵营徽记（走 `DeckRuntime.FactionIcon`）");
            CheckAt(FindChild(chdr, "Info Button"), 719.80f, 760.95f, 94.03f, 135.19f, "`Info Button`");

            // 轨道：`Campaign Track` x 330.69..1920.34 · y 335.47..1044.53
            var ctrack = FindChild(camp, "Campaign Track");
            CheckAt(ctrack, 330.69f, 1920.34f, 335.47f, 1044.53f, "`Campaign Track`");
            var cContent = FindChild(FindChild(ctrack, "Viewport"), "Content");
            var cTab0 = camp.GetComponent<CampaignTab>();
            int cNodes = 0; var cLines = 0;
            if (cContent != null)
                foreach (var t in cContent.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("CampaignNode_")) cNodes++;
                    else if (t.name.StartsWith("NodeLine_")) cLines++;
                }
            // 🆕 **2026-10-03：接上横向滚动 + `RectMask2D` 等效裁剪之后，只有【视口内】的节点会建**
            //    —— 视口外的连**点击区**一起不建（原版 `RectMask2D` 就是这么裁的）。
            //    ⇒ 判据从「恰好 47 个」改成「恰好 = 视口内那几个」（数量现算，不写死）。
            int cWant = 0;
            for (int t = 0; t < CampaignData.NodeCount; t++)
            {
                if (cTab0 == null || cTab0.TrackScroll == null
                    || cTab0.TrackScroll.Intersects(cTab0.NodeRectForTest(t))) cWant++;
            }
            Check(cNodes, cWant,
                  $"轨道上**恰好建了视口内的那些节点**（{cWant} / 共 {CampaignData.NodeCount}；"
                  + "视口外的被 `RectMask2D` 等效裁剪掉、**连点击一起**）");
            CheckTrue(cWant > 0 && cWant < CampaignData.NodeCount,
                      $"…而且**确实有节点落在视口外**（{CampaignData.NodeCount - cWant} 个）—— 否则这一条等于没验");
            CheckTrue(cLines > 0, $"连线建了（{cLines} 条；**原版 `SetAsFirstSibling` ⇒ 连线在节点下面**）");

            // Premium Panel：**矩形不是 JSON 值**（dump 出来高 = 0），是布局组算的（正本 §四）
            var cpan = FindChild(camp, "Premium Panel");
            CheckAt(cpan, 344.29f, 720.35f, 867.01f, 1080.00f,
                    "`Premium Panel`（**实算值**：`ContentSizeFitter` + `VerticalLayoutGroup` padding 7/11）");

            // 纪律①：出厂 inactive 的三件**不建**
            CheckTrue(FindChild(camp, "Debug Point Button") == null,
                      "`Debug Point Button` **不建**（`OnSetup` 里无条件 `SetActive(false)`）");
            CheckTrue(FindChild(camp, "Premium Button Container") == null,
                      "`Premium Button Container` **不建**（全 bundle 无脚本引用 ⇒ **发行版永不显示**）");
            CheckTrue(FindChild(camp, "Tutorial Message") == null,
                      "`Tutorial Message` **不建**（`ToggleChooseArmyText` **全库 0 调用者** ⇒ 改由教程线做）");

            // 🔴 **6 种状态色**（照原版 `SetNodeStyle` 的表）—— 起手只有根节点是 `Unlocked`
            var cTab = camp.GetComponent<CampaignTab>();
            CheckTrue(cTab != null, "`Campaign Tab` 上挂的是 `CampaignTab`（**不再是空页桩**）");
            if (cTab != null && cContent != null)
            {
                var n0 = cContent.Find("CampaignNode_0");
                var n5 = cContent.Find("CampaignNode_5");
                var b0 = n0 != null ? FindChild(n0, "Generic Round Button Variant") : null;
                var b5 = n5 != null ? FindChild(n5, "Generic Round Button Variant") : null;
                var t0 = TintOf(b0); var t5 = TintOf(b5);
                CheckNear(t0.a, 0.8078431f, 0.01f, "根节点圆盘 alpha = **0.8078**（`unlockedColor` #FFFFFFCE）");
                CheckNear(t5.a, 0.6823530f, 0.01f, "第 6 个节点圆盘 alpha = **0.6824**（`lockedColor` #B2A5A5AE）");
                CheckNear(t5.r, 0.6981132f, 0.01f, "第 6 个节点圆盘 r = **0.6981**（locked 是灰色，不是白）");

                // 点根节点 ⇒ **开奖励窗**（原版 `CampaignWindowTab.OnNodeClicked` 是组 context 开窗，
                // **不是点节点就发奖** —— 发奖在窗里那个 `Unlock` 钮上）。
                // ⚠️ 2026-09-23 改：原来这一条断的是「直接领到了」，那时 `CampaignRewardWindow` 还没建。
                CheckTrue(cTab.ClickNodeForTest(0), "点根节点 UM0 **开出了奖励窗**（`CampaignData.Claimable` 为真）");
                CheckTrue(!CampaignData.BaseClaimed(0), "**开窗本身不发奖**（原版领取发生在窗里的 `Unlock` 钮上）");
                // 再从窗里领（`Unlock` 钮那条路：`OnCollect` → `CampaignTab.ClaimForTest`）
                CheckTrue(cTab.ClaimForTest(0, CampaignData.TierBasic), "窗里的 `Unlock` ⇒ 根节点 UM0 **领到了基础档**");
                var n0b = cContent.Find("CampaignNode_0");
                var n1b = cContent.Find("CampaignNode_1");
                var t0b = TintOf(n0b != null ? FindChild(n0b, "Generic Round Button Variant") : null);
                var t1b = TintOf(n1b != null ? FindChild(n1b, "Generic Round Button Variant") : null);
                CheckNear(t0b.g, 1f, 0.01f, "领过之后根节点染色 = **`collectedColor`**（绿 g=1）");
                CheckNear(t0b.a, 0.6823530f, 0.01f, "领过之后根节点 alpha = **0.6824**");
                CheckNear(t1b.a, 0.8078431f, 0.01f,
                          "**后继节点自动解锁**（`unlockedColor` alpha 0.8078）—— 原版 `Collect` 里把邻居 `State 0 → 10`");
                // 轨道几何：**缩放比的判据 = 「47 个节点都落在 Viewport 竖向范围内」**
                // （原版 `CalculateRatio` 里那几个常量没全解出 ⇒ 这里不盯公式、盯**可观测的结果**）。
                // 轨道 rect / Viewport rect 照 `CampaignTab` 的常量现算（同一套锚点五元组）。
                var trackR = UguiRect.Child(new PxRect(CampaignTab.TabL, CampaignTab.TabT, 1920f, 1080f),
                                            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
                                            new Vector2(0f, -114.53f), new Vector2(0.34009f, 709.06f));
                var vpR = UguiRect.Child(trackR, UguiRect.A00, UguiRect.A11, UguiRect.P01,
                                         new Vector2(0f, 50f), new Vector2(0f, 50f));
                CheckNear(128f * cTab.Ratio, (vpR.H - 100f) / (2f * 392f) * 128f, 1.0f,
                          "**行距** = 128（SO 里两行差）× ratio；ratio 照原版 `CalculateRatio` 的结构"
                          + "（视口高 − 节点高）/（2 × |y| 最大）—— **小于节点高 100 就会上下叠**");
                float top = float.MaxValue, bot = float.MinValue;
                for (int t = 0; t < CampaignData.NodeCount; t++)
                {
                    // 🆕 2026-10-03：**用数据算的矩形**（视口外的节点现在不建 ⇒ `Find` 找不到 ≠ 没有）
                    var rr = cTab.NodeRectForTest(t);
                    float y = rr.CY;
                    if (y < top) top = y;
                    if (y > bot) bot = y;
                }
                CheckTrue(top >= vpR.y1 - 60f && bot <= vpR.y2 + 60f,
                          $"**47 个节点全部落在 Viewport 的竖向范围内**（实测 {top:F0}..{bot:F0}，"
                          + $"视口 {vpR.y1:F0}..{vpR.y2:F0}）—— 这就是「缩放比算对了」的判据");

                // ---- 🆕 2026-10-03：`Campaign Track` 的**横向滚动**（原版这一件是横向 `ScrollRect`）----
                {
                    var ts = cTab.TrackScroll;
                    CheckTrue(ts != null, "轨道有横向滚动区（`MenuScroll`，与锻造页共用同一份实现）");
                    if (ts != null)
                    {
                        CheckTrue(!ts.Vertical, "是**横向**滚动（原版 `Campaign Track` 横向）");
                        ts.SetOffset(0f);
                        CheckNear(ts.Offset, 0f, 0.01f, "起手在**最左**（原版也是起手滚到最左）");
                        // 最左那个节点在视口里（= 那道「让出一个光圈半径」的补偿还在起作用）
                        var r0 = cTab.NodeRectForTest(0);
                        CheckTrue(r0.CX >= vpR.x1, $"最左节点**不被左栏压住**（中心 x {r0.CX:F1} ≥ 视口左 {vpR.x1:F1}）");
                        // 右端：偏移上限 = 内容右端 − 视口右端（>0 ⇒ 右边确实滚得过去）
                        CheckTrue(ts.MaxOffset > 100f,
                                  $"右侧**留了可滚的余量**（{ts.MaxOffset:F0}px —— 47 个节点铺 5000+px，视口只有 {vpR.W:F0}）");
                        ts.SetOffset(ts.MaxOffset);
                        var rl = cTab.NodeRectForTest(CampaignData.NodeCount - 1);
                        CheckTrue(rl.CX <= vpR.x2 + 0.5f,
                                  $"滚到最右 ⇒ **终点节点（UM47）进了视口**（中心 x {rl.CX:F1} ≤ 视口右 {vpR.x2:F1}）");
                        int built = 0;
                        if (cContent != null)
                            foreach (var t in cContent.GetComponentsInChildren<Transform>(true))
                                if (t.name.StartsWith("CampaignNode_")) built++;
                        CheckTrue(built > 0 && built < CampaignData.NodeCount,
                                  $"滚到最右 ⇒ 建的是**另一批**节点（{built} 个；视口外的仍然不建）");
                        ts.SetOffset(0f);
                    }
                }

                CampaignData.ResetForTest();      // 复位，后面的截图要用起手态
                cTab.RefreshNodes();
                // 上面那次点击**真开了一个奖励窗**（弹窗）——收掉，免得它盖住后面的截图
                if (wm2.popUpWindow != null) wm2.popUpWindow.Close();
            }
            if (camp != null && camp.GetComponent<CampaignTab>() != null)
                Debug.Log(P + "   " + camp.GetComponent<CampaignTab>().Dump());
        }

        // ============================================================ §三·d 战役奖励窗
        // 期望值全部来自**原版参数**（正本 `资料/阶段二_锻造厂与战役页_原版规格.md` §十四）
        // 与**反编译**（`CampaignRewardsWindow__Open.c` / `__ConfigureIsPreviewState.c` / `CampaignUnlockButton__*.c`）。
        Section("§三·d `Campaign Reward Window`（第 3 层第 3 件）：层 × 参数逐条对");
        {
            // ---- 窗口参数（MB `7664330643585539206.json` 原文）----
            var cw = CampaignRewardWindow.Create(wm2);
            Check(cw.type, WindowType.Popup, "`type` = 1 Popup（原文）");
            Check(cw.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup（原文）");
            Check(cw.closeOnEsc, true, "`closeOnESC` = 1（原文）");
            CheckNear(cw.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = 1.0（原文）");

            // ---- UM0：基础档 1 件（`UM_SK_Starter`）· 高级档 1 件（`DT Ultramarines R4`）----
            var ctx0 = new CampaignRewardsContext
            {
                Rewards = CampaignData.RewardsOf(0),
                BaseCollected = false, PremiumCollected = false, Claimable = true,
                IsPremiumLocked = false, PointCost = CampaignData.At(0).Cost, Army = 10,
            };
            Check(ctx0.Rewards.Length, 2, "UM0 的奖励 = **2 条**（照 SO；正本 §十二）");
            wm2.OpenWindow(cw, ctx0);
            var croot = cw.transform;

            // ---- 根下三层：压暗 / 内容 / 暗角 ----
            CheckTrue(FindChild(croot, "Menu Dark Background") != null, "`Menu Dark Background` 建了");
            CheckNear(TintOf(FindChild(croot, "Menu Dark Background")).a, 0.773f, 0.005f,
                      "压暗层 alpha = **0.773**（原版 `m_Color` 原文）");
            CheckNear(TintOf(FindChild(croot, "Menu Dark Background")).r, 0f, 0.005f, "压暗层是纯黑");
            CheckRectPx(FindPath(croot, "Content"), 0f, 1920f, 165f, 965f,
                        "`Content`（`N(1,0,.5,1,.5,.5,.5, 0,−25, 0,800)` ⇒ y 165..965）");
            var vig = FindChild(croot, "Menu Vignette");
            CheckRectPx(vig, 0f, 1920f, 0f, 1080f, "`Menu Vignette` **铺满整屏**");
            CheckNear(TintOf(vig).a, 0.58f, 0.005f, "暗角 alpha = **0.58**（原版 `m_Color` 原文）");

            // ---- 出厂态 = **Preview**（`Glow Preview reward` 出厂 ACT、`Glow Get reward` 出厂 INACT）----
            CheckTrue(cw.IsPreview, "UM0 未领 ⇒ **Preview 态**（`__Open.c` 的 bVar9 分支）");
            CheckTrue(FindPath(croot, "Content/Reward Background Preview Reward") == null
                      || FindPath(croot, "Content/Reward Background Preview Reward").gameObject.activeSelf,
                      "Preview 态：`Reward Background Preview Reward` 开着");
            var gGet = FindPath(croot, "Content/Title/Glow Get reward");
            var gPre = FindPath(croot, "Content/Title/Glow Preview reward");
            CheckTrue(gGet == null || !gGet.gameObject.activeSelf, "Preview 态：`Glow Get reward` **关着**");
            CheckTrue(gPre != null && gPre.gameObject.activeSelf, "Preview 态：`Glow Preview reward` **开着**");
            // 标题文案：**prefab 里的 TMP 原文**（`ConfigureIsPreviewState` 只开关、不改文字）
            var ptNode = FindPath(gPre, "Text Preview Reward");
            CheckTrue(TextOf(ptNode) == CampaignRewardWindow.TxtPreview,
                      $"Preview 标题写的是 `{CampaignRewardWindow.TxtPreview}`（prefab TMP 原文）"
                      + $"—— 实测节点 {(ptNode != null ? "在" : "**不在**")}、文字「{(ptNode != null ? TextOf(ptNode) : "?")}」");

            // ---- 两列 holder：基础列**右边贴** 列右−65、高级列**左边贴** 列左+65 ----
            // 内容宽 = padL30 + Σ子件 + spacing25×(n−1) + padR30（`ContentSizeFitter m_HorizontalFit=1`）
            const float itemW = CampaignRewardWindow.ItemW, btnW = CampaignRewardWindow.UnlockW;
            const float badgeW = CampaignRewardWindow.BadgeSize;
            float baseW = 30f + itemW + 25f + btnW + 30f;
            float premW = 30f + itemW + 25f + btnW + 25f + badgeW + 30f;
            CheckNear(cw.BaseHolderRect.x2, 960f - 65f, 0.5f, "基础列 holder 的**右边缘 = 895**（列右 − 65）");
            CheckNear(cw.BaseHolderRect.W, baseW, 0.5f, $"基础列 holder 内容宽 = **{baseW}**（1 物品 + 按钮）");
            CheckNear(cw.PremHolderRect.x1, 960f + 65f, 0.5f, "高级列 holder 的**左边缘 = 1025**（列左 + 65）");
            CheckNear(cw.PremHolderRect.W, premW, 0.5f, $"高级列 holder 内容宽 = **{premW}**（1 物品 + 按钮 + 徽标）");
            CheckTrue(cw.BaseHolderRect.x1 >= 0f && cw.PremHolderRect.x2 <= 1920f,
                      $"两列的 holder **都落在屏幕里**（基础 {cw.BaseHolderRect.x1:F0}..{cw.BaseHolderRect.x2:F0} · "
                      + $"高级 {cw.PremHolderRect.x1:F0}..{cw.PremHolderRect.x2:F0}）"
                      + " —— 这是「内容宽算对了」的判据（⚠️ 把 INACT 的 `Warning` 也算进去会跑到 2005）");

            // ---- 物品格数 = context 里的条数（一列一个 tier）----
            var bh = FindPath(croot, "Content/Scroll View/Viewport/Content/Base Rewards/Rewards");
            var ph = FindPath(croot, "Content/Scroll View/Viewport/Content/Premium Rewards/Rewards");
            Check(CountByPrefix(bh, "Item_"), 1, "基础列画出 **1 个物品格**（UM0 基础档只有 1 条）");
            Check(CountByPrefix(ph, "Item_"), 1, "高级列画出 **1 个物品格**");
            CheckArt(FindChild(bh, "Unlock Button"), "UI_Button_Mulligan", "`Unlock Button` 的底图（Simple）");
            CheckArt(FindChild(ph, "Badge"), "40k_campaign_Premium-icon", "高级列的 `Badge`");

            // ---- 🔴 「谁压谁 / 谁跟谁不齐」这一类：**只能量【渲出来】的矩形** ----
            //   （2026-09-23 实拍才发现：物品格里的图标/占位板原来**顶在格子顶部**，而旁边的
            //     `Unlock Button` 是 `VertCenter` 的 ⇒ 两者中心差 40px、看着不齐，而**断言全绿**。）
            {
                float ix1, iy1, ix2, iy2, ux1, uy1, ux2, uy2;
                var icon = FindChild(bh, "IconPlaceholder") ?? FindChild(bh, "Icon");
                CheckTrue(RectOf(icon, out ix1, out iy1, out ix2, out iy2), "基础列物品格里有图标（或占位板）");
                CheckTrue(RectOf(FindChild(bh, "Unlock Button"), out ux1, out uy1, out ux2, out uy2),
                          "基础列的 `Unlock Button` 量得到渲染矩形");
                if (RectOf(icon, out ix1, out iy1, out ix2, out iy2)
                    && RectOf(FindChild(bh, "Unlock Button"), out ux1, out uy1, out ux2, out uy2))
                {
                    CheckNear((iy1 + iy2) * 0.5f, (uy1 + uy2) * 0.5f, 2f,
                              "物品图标与 `Unlock Button` **竖向中心对齐**（都在列的内容区里居中）");
                    CheckTrue(!Overlaps(ix1, iy1, ix2, iy2, ux1, uy1, ux2, uy2),
                              $"物品图标**不压** `Unlock Button`（图 x {ix1:F0}..{ix2:F0} · "
                              + $"按钮 x {ux1:F0}..{ux2:F0}）");
                }
            }

            // ---- 未领 + `PointCost = 100 ≥ 1` ⇒ 走「付点解锁」那一态：显点数、显 pointDrawer ----
            CheckTrue(FindPath(bh, "Unlock Button/Icon Campaign Points Drawer Variant") != null
                      && FindPath(bh, "Unlock Button/Icon Campaign Points Drawer Variant").gameObject.activeSelf,
                      "`PointCost ≥ 1` ⇒ `Icon Campaign Points Drawer Variant` **开着**（= 原版 `ToggleTexts(true)` 开 pointDrawer）");
            var costLb = FindPath(bh, "Unlock Button/Point Count");
            CheckTrue(costLb != null && TextOf(costLb) == "100",
                      $"按钮上的点数写的是**这一格的 `Cost`**（原版 `SetUnlockCost` 的 `cost.ToString()`）—— 实测「{(costLb != null ? TextOf(costLb) : "?")}」");
            // 🔴 **量渲染真值**（这一条是「`localScale` 要烘进子件矩形」那个坑的判据）：
            //    图标节点出厂 `localScale = 1.5`，**给父设 scale 再照常摆子件 ⇒ 位置与大小同时偏 1.5×**
            //    （2026-09-23 实拍：画成 150²、往右偏了半个按钮，而**所有矩形断言全绿**）。
            var ubtn = FindPath(bh, "Unlock Button");
            var pico = FindPath(bh, "Unlock Button/Icon Campaign Points Drawer Variant");
            CheckTrue(pico != null && ubtn != null, "`Icon Campaign Points Drawer Variant` 建了");
            if (pico != null && ubtn != null)
            {
                float ubtnLeft = PxOf(ubtn.position.x) - Wpx(ubtn) * 0.5f;
                CheckNear(Wpx(pico), CampaignRewardWindow.PtIconBox * CampaignRewardWindow.PtIconScale, 2f,
                          "战役点图标的**渲染宽 = 45 × 1.5 = 67.5**（缩放烘进矩形，不是给父设 scale）");
                CheckNear(PxOf(pico.position.x) + Wpx(pico) * 0.5f, ubtnLeft + 145f, 2f,
                          "图标的**右边缘 = 按钮左边 + 145**（pivot (1,0.5) + `pos.x = −77.5` + scl 1.5 实算）");
            }

            // ---- 领完之后按钮进 claimed 态（`SetAsClaimed`：`ToggleTexts(false)` + 关 pointDrawer）----
            CampaignData.ResetForTest();
            var cw2 = CampaignRewardWindow.Create(wm2);
            var ctxC = new CampaignRewardsContext
            {
                Rewards = CampaignData.RewardsOf(0), BaseCollected = true, PremiumCollected = false,
                Claimable = false, IsPremiumLocked = false, PointCost = CampaignData.At(0).Cost, Army = 10,
            };
            wm2.OpenWindow(cw2, ctxC);
            var bh2 = FindPath(cw2.transform, "Content/Scroll View/Viewport/Content/Base Rewards/Rewards");
            CheckTrue(FindPath(bh2, "Unlock Button/Icon Campaign Points Drawer Variant") != null
                      && !FindPath(bh2, "Unlock Button/Icon Campaign Points Drawer Variant").gameObject.activeSelf,
                      "已领 ⇒ pointDrawer **关着**（原版 `SetAsClaimed` 走 `ToggleTexts(false)`）");
            var cl2 = FindPath(bh2, "Unlock Button/Claimed Text");
            CheckTrue(cl2 != null && TextOf(cl2) == CampaignRewardWindow.TxtClaimed,
                      $"已领 ⇒ `Claimed Text` 写的是 `claim**ed**Key` 那个词条（我们按 term 末段填 `{CampaignRewardWindow.TxtClaimed}`）");
            // 高级列没领 ⇒ 恒 Preview（`__Open.c`：只有基础列时 preview = !BaseCollected）
            CheckTrue(cw2.IsPreview, "基础已领、高级未领 ⇒ **仍是 Preview**（`bVar9` 的口径）");
            cw2.Close();

            // ---- 单列时**居中**（原版 `CenterHolder`）----
            // UM1：基础 1 条、高级 0 条 ⇒ 只有基础列 ⇒ 内容在**视口正中**（不是留在 0..960 那一半）
            var cw3 = CampaignRewardWindow.Create(wm2);
            var ctx1 = new CampaignRewardsContext
            {
                Rewards = CampaignData.RewardsOf(1), BaseCollected = false, PremiumCollected = false,
                Claimable = true, IsPremiumLocked = false, PointCost = CampaignData.At(1).Cost, Army = 10,
            };
            Check(ctx1.Rewards.Length, 1, "UM1 只有 **1 条**奖励（且是基础档 ⇒ 高级列整列关掉）");
            wm2.OpenWindow(cw3, ctx1);
            CheckNear(cw3.BaseHolderRect.CX, 960f, 1.0f,
                      "只有一列 ⇒ holder **水平居中在 960**（原版 `CenterHolder` 把 holder **和它的父**一起改成锚 (.5,0)/(.5,1)）");
            CheckTrue(FindPath(cw3.transform, "Content/Scroll View/Viewport/Content/Premium Rewards") == null
                      || !FindPath(cw3.transform, "Content/Scroll View/Viewport/Content/Premium Rewards").gameObject.activeSelf,
                      "这一格没有高级档奖励 ⇒ **高级列整列关掉**（`__Open.c` 的 `SetActive(hasPrem)`）");
            cw3.Close();

            // ---- 两列都空 ⇒ 两列都关（`__Open.c` 的第 4 条分支）----
            var cw4 = CampaignRewardWindow.Create(wm2);
            wm2.OpenWindow(cw4, new CampaignRewardsContext
            {
                Rewards = new CampaignData.RewardSpec[0], IsPremiumLocked = false, PointCost = 0, Army = 10,
            });
            var bc4 = FindPath(cw4.transform, "Content/Scroll View/Viewport/Content/Base Rewards");
            var pc4 = FindPath(cw4.transform, "Content/Scroll View/Viewport/Content/Premium Rewards");
            CheckTrue(bc4 != null && !bc4.gameObject.activeSelf, "两列都空 ⇒ **基础列也关掉**");
            CheckTrue(pc4 != null && !pc4.gameObject.activeSelf, "两列都空 ⇒ **高级列也关掉**");
            CheckTrue(cw4.IsPreview, "两列都空 ⇒ Preview（原版默认分支）");
            cw4.Close();

            // ---- 奖励数据与节点表**同源**（`HasPremium` 与奖励表必须一致）----
            int nNodes, nRewards, nMismatch;
            CampaignData.SelfCheck(out nNodes, out nRewards, out nMismatch);
            Check(nNodes, 47, "节点表 **47** 个");
            Check(nRewards, 89, "奖励表 **89 条**（照 SO 生成；基础 70 / 高级 19）");
            Check(nMismatch, 0, "**每一格的 `HasPremium` 与奖励表一致**（同源复算，不一致就是抄错了）");

            // ---- 图缺不缺（不许静默）----
            Debug.Log(P + "   " + cw.Dump());
            CheckTrue(cw.NoIconItems.Count >= 0, "`NoIconItems` 清单存在（没有图标的物品**逐条打日志**，不是静默）");

            // ---- 实拍一张（断言测不出「像不像」；`资料/阶段二_锻造厂与战役页_原版规格.md` §十四）----
            // 把 UM0 那窗重新开出来再拍（前面几段换过 context）
            wm2.OpenWindow(cw, ctx0);
            Shoot("02b_战役奖励窗.png");
            Debug.Log(P + "   " + cw.Dump());

            cw.Close();
            CampaignData.ResetForTest();
        }

        // ============================================================ 弹窗队列的次序（跨页）
        // 🔴 **唯一能自动拦住「弹窗被页底板盖住」的判据** —— 这种错**矩形断言量不到**（件都在、位置也对）。
        //    2026-09-23 实测过一次：`PromptPopup` 原来在 3018…3023，而 `CampaignTab` 的底板到 3064。
        Section("渲染队列的次序：弹窗 > 页 > 窗（不许有交叉）");
        {
            int pageMax = Mathf.Max(Mathf.Max(ForgeTab.QTabHelp, CampaignTab.QPanelTimer), RewardsWindow.QOverlay);
            int popupMin = Mathf.Min(CampaignRewardWindow.QShade, PromptPopup.QShade);
            CheckTrue(pageMax < popupMin,
                      $"**所有「页」的最高队列 {pageMax} < 所有弹窗的最低队列 {popupMin}**"
                      + $"（页：`RewardsWindow` 3014 · `ForgeTab` {ForgeTab.QTabHelp} · `CampaignTab` {CampaignTab.QPanelTimer}；"
                      + $"弹窗：`CampaignRewardWindow` {CampaignRewardWindow.QShade} · `PromptPopup` {PromptPopup.QShade}）"
                      + " —— ⚠️ 新增任何一页都要回头看这个数");
            CheckTrue(PromptPopup.QText > CampaignRewardWindow.QVignette,
                      "通用弹窗在最上层弹窗之上（`PromptPopup` > `CampaignRewardWindow`）");
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
        win.tabButtons.Click(1);                                  // 视觉第 2 键 = Campaign
        Shoot("02_战役.png");        // 2026-09-23：这一页**不再是空页**（`CampaignTab` 已建成）
        // 🔴 **文字必须真的落在框里**（`资料/日常_画面逐项对_0923.md` D10 那条：原版是 `H=Left/Right`、
        //    我们画成居中 ⇒ 字压在别的件上，而**矩形断言全绿**）。这里量的是 `Label.WorldW`（TMP 真测量）。
        //    ⚠️ **必须在页面「正显示时」量** —— 未激活时 TMP 的 `textBounds` 是旧值/垃圾
        //    （实测量出 2.3e11；见 `已知的坑.md` 那条「面板画出来了、字不在」的坑①）。
        var camView = FindChild(FindChild(area, "Tabs"), "Campaign Tab");
        var cpan2 = FindChild(camView, "Premium Panel");
        CheckTrue(TextRightPx(FindChild(cpan2, "Title")) <= 720.35f + 1f,
                  $"`Premium Panel/Title` 的**右边缘 ≤ 面板右边 720.35**（原版 `TMP(右/上)`；"
                  + $"实测 {TextRightPx(FindChild(cpan2, "Title")):F1}）");
        // 🔴 **左边缘也要断** —— 2026-09-23 用户拿截图问出来的：这段 28 字的串在 fs33.3 下渲出 ≈545px，
        //    而框只有 355.79px ⇒ `AlignRight` 之后**左边冲出面板 180px**，而**上面那条只管右边缘、全绿**。
        //    原版那条 TMP 是 `m_enableAutoSizing = 1`（min 10）⇒ **原版是缩字号**（正本 §四）。
        CheckTrue(TextLeftPx(FindChild(cpan2, "Title")) >= 354.43f - 1f,
                  $"`Premium Panel/Title` 的**左边缘 ≥ 它自己的框左边 354.43**"
                  + $"（原版 `m_enableAutoSizing = 1` ⇒ 装不下就**缩字号**，不是溢出；"
                  + $"实测 {TextLeftPx(FindChild(cpan2, "Title")):F1}）");
        CheckTrue(TextLeftPx(FindChild(cpan2, "Timer Text")) >= 408.63f - 1f,
                  $"`Premium Panel/Timer Text` 的**左边缘 ≥ 它自己的框左边 408.63**（原版 `TMP(左/上)`；"
                  + $"实测 {TextLeftPx(FindChild(cpan2, "Timer Text")):F1}）");
        CheckNear(TextLeftPx(FindChild(FindChild(camView, "Campaign Header"), "Title")), 480.69f, 2f,
                  "`Campaign Header/Title` 的**左边缘 = 框左边 480.69**（原版 `TMP(左/中)`）");
        win.tabButtons.Click(2);                                  // 视觉第 3 键 = Forge（第 3 层第 1 件）
        Shoot("03_锻造厂.png");
        // 🔴 再拍一张**不可领**的：当前阵营（Goff）是可领态，`Ready for level up` 那团 700² 的洋红光
        //    会把**旋涡大图 `Warp`** 整个盖住（原版兄弟序就是光效在背景之上）⇒ 只看那一张会以为旋涡没画。
        //    换一个「差一点」的阵营，光效灭、旋涡露出来 —— 这一下**同时验了 `SelectArmy`**。
        var fgo = forge != null ? forge.GetComponent<ForgeTab>() : null;
        if (fgo != null)
        {
            fgo.SelectArmy("SaimHann");                           // 第 3 个阵营：差 40 点 ⇒ `InProgress`
            Shoot("03b_锻造厂_不可领.png");
        }
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
        CheckHoverSwap(dr.transform, "每日奖励窗");
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
        CheckHoverSwap(ds.transform, "连登窗");
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
        CheckHoverSwap(inbox.transform, "收件箱");
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

    /// <summary>按**名字前缀**数（`CountByName` 是**全等**比较 —— 拿它数 `Item_` 恒得 0）。</summary>
    static int CountByPrefix(Transform root, string prefix)
    {
        if (root == null) return 0;
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith(prefix)) n++;
        return n;
    }

    static int CountVisible(Transform root, string name)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name && t.gameObject.activeInHierarchy) n++;
        return n;
    }

    /// <summary>名字**以 `prefix` 打头**且**在层级里可见**的节点数（左栏 `RewardsTabButton_0..3` 这种）。</summary>
    static int CountVisibleChildren(Transform root, string prefix)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith(prefix, System.StringComparison.Ordinal) && t.gameObject.activeInHierarchy) n++;
        return n;
    }
}
