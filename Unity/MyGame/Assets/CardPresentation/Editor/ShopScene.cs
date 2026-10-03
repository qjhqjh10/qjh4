// ShopScene.cs — 阶段二第 3 层「商店」的**自检入口**
//
// 用法：… -executeMethod ShopScene.Run        自检（结构 + 版面 + 交互 + 截图），退出码 0 = 全过
//
// 🔴 **每一条断言的期望值都盯「原版值」，不是盯我们自己写的常量**（否则就是自证）。
//    期望值来自三个页签 prefab 的**原始 JSON 走链**：
//      `工具/menu_rect.py bundle_menus_assets_all "<名>" --size 1752.83x1009.06 --relative` **再加 (167.17, 70.94)**
//      （`Card/Daily/Item Shop Tab` 的 `m_Father = 0`，是**独立 prefab 根**、运行期才挂进 `Tabs`）；
//      以及 `工具/menu_rect.py … "Catalog Item Shop Container" --root-size 335.6x475 --relative`（格内几何）。
//    交叉验证：这样算出来的 `Packs Scroll View` 与直接量 `Shop Menu Variant` 的同一节点**逐位相同**。
//
// ⚠️ **为什么没有 `BuildAndSaveScene`**：同 `RewardsScene` —— 商店是**挂在 `Shell` 锚点上的窗口**，
//    由主菜单左竖导航的 SHOP 钮开（原版 `OpenWindowButton.closeOtherMenus = 1` ⇒ `closeAll: true`）。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShopScene
{
    const string P = "[Shop] ";
    const string ShotDir = "d:/4/_tmp_view/shop";

    static int _pass, _fail;
    static readonly List<string> _failures = new List<string>();
    /// <summary>🆕 A8：19 个商品条目容器建在这棵（摆在屏外）—— 实拍那一段要把它挪进画面再拍一张。</summary>
    static Transform _offerScratch;

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

    /// <summary>节点**在世界里的位置**要落在原版像素矩形的中心。</summary>
    static void CheckAt(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var want = LayoutSpace.RectCenter(x1, y1, x2, y2);
        float d = Vector3.Distance(t.position, want);
        CheckTrue(d <= 0.01f, $"{what} 在原版矩形中心（差 {d:F4} 世界单位 = {d * 108f:F2}px）");
    }

    /// <summary>一张图**渲出来的像素矩形**（`WorldW/H` = 渲染真值，不是回读我们传进去的数）。</summary>
    static void CheckRectPx(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) { CheckTrue(false, what + "（没有 ImageQuad）"); return; }
        CheckNear(q.WorldW * 108f, x2 - x1, 2.0f, what + " 宽(px)");
        CheckNear(q.WorldH * 108f, y2 - y1, 2.0f, what + " 高(px)");
    }

    static void CheckArt(Transform t, string want, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        CheckTrue(q != null && q.Texture != null && q.Texture.name == want, $"{what} = `{want}`");
    }

    static Color TintOf(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) return new Color(0f, 0f, 0f, 0f);
        var mr = q.GetComponent<MeshRenderer>();
        return mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.color : new Color(0f, 0f, 0f, 0f);
    }

    static string TextOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.Text : null;
    }

    /// <summary>一个件**渲出来**的像素矩形（画布像素 · 左上原点 · y 向下）。
    /// 图走 `ImageQuad.WorldW/H`、字走 `Label.WorldW/H`（**都是 TMP/材质的真测量**，不是回读常量）。
    /// 🔴 取的是**组件自己的 transform** —— `AlignLeft/Right` 会把 `Label` 的节点挪走，
    ///    拿外层容器的位置去算就会偏（本工程踩过「字飘走了而矩形断言全绿」）。</summary>
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

    /// <summary>两个矩形**有没有重叠**（像素矩形的标准 AABB 判据）。</summary>
    static bool Overlaps(float ax1, float ay1, float ax2, float ay2,
                         float bx1, float by1, float bx2, float by2)
    {
        return ax1 < bx2 - 0.5f && bx1 < ax2 - 0.5f && ay1 < by2 - 0.5f && by1 < ay2 - 0.5f;
    }

    static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>**测试编排**（不是断言）：A7 之后「买一件卡包」会弹出**全屏**的
    /// `Booster Pack Open Window`（`type = 0 Fullscreen` ⇒ `OpenWindowCO` 会把商店关掉），
    /// 后面那些断言/实拍要的是**商店开着**的状态 ⇒ 这里把它关掉、把商店重新开起来。</summary>
    static void ClosePackAndReopenShop(ShopWindow win)
    {
        var pg = win.PageOf(0);
        var bp = pg != null ? pg.LastBoosterPack : null;
        if (bp != null && bp.CurrentState != WindowState.Closed)
        {
            Debug.Log(P + "  （编排）买完弹出的开包窗先关掉，把商店重新开起来");
            bp.Close();
        }
        if (win.CurrentState == WindowState.Closed && win.Manager != null) win.Manager.OpenWindow(win);
    }

    /// <summary>世界 x → 画布像素 x（×108 + 960）。⚠️ **只能用在 x 上**。</summary>
    static float PxOf(float worldX) { return worldX * 108f + 960f; }
    /// <summary>世界 y → 画布像素 y。**y 是反的**（像素 y 向下）⇒ `540 − worldY × 108`。
    /// 🔴 拿 `PxOf` 去量 y 会得到**假警报**（本工程踩过，见 `RewardsScene` 的 `PxYOf`）。</summary>
    static float PxYOf(float worldY) { return 540f - worldY * 108f; }

    /// <summary>**按路径**找（`FindChild` 是按名字找的、不认识 `A/B/C` —— 见 `RewardsScene` 里那条注释）。</summary>
    static Transform FindPath(Transform root, string path) { return root != null ? root.Find(path) : null; }

    static int CountByPrefix(Transform root, string prefix)
    {
        if (root == null) return 0;
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith(prefix)) n++;
        return n;
    }

    static int CountVisible(Transform root, string prefix)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith(prefix) && t.gameObject.activeInHierarchy) n++;
        return n;
    }

    // ============================================================ 场景

    static ShopWindow Build(out Transform root)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.aspect = LayoutSpace.DesignAspect;      // ⚠️ 必须在建任何东西之前定死（批处理默认 4:3）
        LayoutSpace.Apply(cam);

        var anchors = new GameObject("Window Anchors").transform;
        // 商店的 `windowsPlacement = 10 (World)`（**与奖励窗的 5 Canvas 不同**）
        MakeHolder(anchors, "1 - Below Upper Bar Holder", WindowsPlacement.World);
        // 🆕 A7：`Booster Pack Open Window` 是 `windowsPlacement = **5 (Canvas)**`
        //（MB `MonoBehaviour_9012570135841684515.json` 原文）⇒ 本场景也得有那个锚点，
        // 否则 `GetWindowAnchor(Canvas)` 会报「找不到锚点」、窗口建在场景根上（能跑，但那不是原版的挂法）。
        MakeHolder(anchors, "2 - Canvas Holder Above upper bar", WindowsPlacement.Canvas);

        var wmGo = new GameObject("WindowsManager");
        var wm = wmGo.AddComponent<WindowsManager>();

        var win = ShopWindow.Create(wm);
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
        // 🔴 **空图护栏**（同 `RewardsScene`）：全黑的图不拦就白拍
        float lum = MeanBrightness(tex);
        if (allowBlank) Debug.Log(P + $"  截图 {file} 平均亮度 {lum:F1}（**这一张按已知情况放行**）");
        else CheckTrue(lum > 3f, $"{file} 不是空图（平均亮度 {lum:F1} > 3）");
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Debug.Log(P + $"  截图 {Path.Combine(ShotDir, file)}");
    }

    static float MeanBrightness(Texture2D t)
    {
        if (t == null) return 0f;
        var px = t.GetPixels32();
        if (px.Length == 0) return 0f;
        long sum = 0;
        for (int i = 0; i < px.Length; i += 7) sum += px[i].r + px[i].g + px[i].b;
        return sum / 3f / ((px.Length + 6) / 7);
    }

    // ============================================================ 自检

    public static void Run()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        Directory.CreateDirectory(ShotDir);
        ShopData.ResetForTest();
        Debug.Log(P + "=== 「商店」自检 开始 ===");

        var win = Build(out var root);

        // ---------------- 窗口参数（`MonoBehaviour_6184803956894681212.json` 原文）----------------
        Section("窗口参数（`Shop Menu Variant` 的 MB 原文）");
        Check(win.type, WindowType.Fullscreen, "`type` = 0 Fullscreen（原文）");
        Check(win.placement, WindowsPlacement.World,
              "`windowsPlacement` = **10 World**（⚠️ **奖励窗是 5 Canvas** —— 两个窗不同，别互推）");
        Check(win.closeOnEsc, true, "`closeOnESC` = 1（⚠️ 奖励窗是 0）");
        CheckNear(win.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = 1.0（原文）");

        // ---------------- 外壳（与奖励窗**同一套**，常量已收口到 `MainMenuSubmenuWindow`）----------------
        Section("外壳 `Content Area` / 左栏 / `Tabs`（原版实测，与奖励窗同值）");
        var area = FindChild(root, "Content Area");
        CheckAt(area, 167.17f, 1920.01f, 70.94f, 1080f, "`Content Area`");
        var bar = FindChild(root, "Tab Buttons");
        CheckAt(bar, 167.17f, 332.17f, 70.94f, 1080f, "`Tab Buttons`（左栏，165 宽）");
        CheckAt(FindChild(bar, "Shadow"), 167.18f, 214.81f, 70.94f, 1080f, "`Tab Buttons/Shadow`（47.64 宽）");
        CheckAt(FindChild(area, "Tabs"), 166.69f, 1920f, 69.20f, 1080f, "`Tabs`");

        Section("左栏键（3 个页 + 1 个母版；**母版照建但关着**）");
        Check(CountByPrefix(bar, "ShopTabButton_"), 4, "建了 **4** 个键（3 页 + `tabButtonPrefab` 母版）");
        Check(CountVisible(bar, "ShopTabButton_"), 3, "**可见的键恰好 3 个**（母版照原版 `TabButtons.Initialize` 关掉）");
        CheckTrue(FindChild(bar, "ShopTabButton_3") != null && !FindChild(bar, "ShopTabButton_3").gameObject.activeSelf,
                  "第 4 键（母版）**建成但关着**");
        for (int i = 0; i < ShopData.Pages.Length; i++)
        {
            var b = FindChild(bar, "ShopTabButton_" + i);
            CheckArt(FindChild(b, "Icon"), ShopData.Pages[i].Icon,
                     $"第 {i + 1} 键的图标 = `{ShopData.Pages[i].Icon}`");
            CheckTrue(TextOf(FindChild(b, "Text")) == ShopData.Pages[i].Label.ToUpperInvariant(),
                      $"第 {i + 1} 键的文案 = `{ShopData.Pages[i].Label.ToUpperInvariant()}`");
        }

        // ---------------- 页签：切页 ----------------
        Section("页签切换（三个页各自的层 × 参数）");
        for (int p = 0; p < ShopData.Pages.Length; p++)
        {
            win.tabButtons.Click(p);
            Check(win.CurrentTab, (WindowTabType)(10 + p), $"点第 {p + 1} 键 ⇒ 切到 `{ShopData.Pages[p].Label}` 页");

            var pg = FindChild(root, ShopData.Pages[p].Prefab);
            CheckTrue(pg != null && pg.gameObject.activeSelf, $"`{ShopData.Pages[p].Prefab}` 开着");
            // 页根 = `Tabs` 整矩形（三页的根都是 aMin(0,0)/aMax(1,1)/pos(0,0)/sizeDelta(0,0) ⇒ 撑满父）
            CheckAt(pg, 167.17f, 1920.00f, 70.94f, 1080.00f, "页根矩形 = `Tabs` 的整矩形");

            // `daily shop header`（高 85）／`TimeCounter`／`Packs Scroll View`
            var hdr = FindChild(pg, "daily shop header");
            CheckAt(hdr, 167.17f, 1920.00f, 70.94f, 155.94f, "`daily shop header`（高 85）");
            CheckTrue(FindChild(pg, "Line") == null,
                      "`Line` **不建**（出厂 `m_IsActive = false`）");
            var tc = FindChild(hdr, "TimeCounter");
            CheckAt(tc, 367.47f, 678.87f, 70.94f, 150.94f, "`TimeCounter`（311.40 × 80）");
            var sv = FindChild(pg, "Packs Scroll View");
            CheckAt(sv, 329.76f, 1920.00f, 127.62f, 1080.00f, "`Packs Scroll View`");
            // ⚠️ **别拿 `CheckRectPx(sv, …)`** —— `sv` 自己**没有 Graphic**（原版那个 `Image` 是 `UIMask`、
            //    `m_Color.a = 0` ⇒ 我们照纪律没画），所以 `GetComponentInChildren<ImageQuad>()` 会
            //    一路找到**子树里第一格**（337.6 × 477），量出来的是格的尺寸、不是 Scroll View 的。
            //    实测踩过：那两条断言报「宽 337.60 ≈ 1590.24」，**看着像版面错，其实是量错了节点**。
            //    `sv` 的矩形由上面那条 `CheckAt` 钉住；「渲出来的尺寸」留给下面的格去量。

            // 🔴 **三页唯一的结构差**：`TimeCounter` 的第一个子件 —— 文字 vs 时钟图标
            bool asText = ShopData.Pages[p].TimerAsText;
            CheckTrue(FindChild(tc, "RefreshText") != null || FindChild(tc, "Clock Icon") != null,
                      "`TimeCounter` 的第一个子件建了（文字 或 时钟图标）");
            CheckTrue((FindChild(tc, "RefreshText") != null) == asText,
                      asText ? "本页时间条是**文字**（原版 `Refreshes in:`）"
                             : "本页时间条是**时钟图标**（原版 `Clock Icon` = `WF_icon_clock`，preferredWidth 26）");
            if (!asText)
            {
                CheckArt(FindChild(tc, "Clock Icon"), "WF_icon_clock", "时钟图标");
                // 🔴 **量渲染真值**：只断「节点在不在」拦不住「画到屏外去了」
                //    （第一版就是这么错的：只改了 x、y 留在错初值，而断言全绿）。
                var ic = FindChild(tc, "Clock Icon");
                var iq = ic != null ? ic.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(iq != null, "时钟图标有 `ImageQuad`");
                if (iq != null)
                {
                    CheckNear(iq.WorldH * 108f, 26f, 2f, "时钟图标**渲出来的高 = 26**（原版 `preferredWidth 26` + `preserveAspect`）");
                    float pxc = PxOf(iq.transform.position.x), pyc = PxYOf(iq.transform.position.y);
                    CheckNear(pyc, 110.94f, 3f, "时钟图标**在 `TimeCounter` 的竖向中心上**（y ≈ 110.94）");
                    CheckTrue(pxc > 367.47f && pxc < 678.87f,
                              $"时钟图标**落在 `TimeCounter` 的横向范围内**（实测 x {pxc:F1}，容器 367.47..678.87）");
                }
            }
            CheckTrue(TextOf(FindChild(tc, "Time")) == ShopData.RefreshTime,
                      $"倒计时文本 = `{ShopData.RefreshTime}`（原版是 `TimerDisplay` 给的真时间；**值是我们挑的**）");

            // ---- 栅格：`GridLayoutGroup cell 335.6×475 · spacing 0 · padding.top 7 · 2 列 · UpperLeft` ----
            var offers = ShopData.Offers(p);
            var content = FindPath(pg, "Packs Scroll View/Viewport/Content");
            CheckTrue(content != null, "`Content`（栅格挂点）建了");
            CheckByPrefix(content, "CatalogItemShopContainer_", offers.Length,
                          $"格数 = 商品数 {offers.Length}（照 `ShopTab.CreateOffers` 逐 offer 建）");
            for (int i = 0; i < offers.Length; i++)
            {
                int col = i % ShopTabPage.GridCols, row = i / ShopTabPage.GridCols;
                float x1 = 329.76f + col * ShopTabPage.CellW;
                float y1 = 127.62f + ShopTabPage.GridPadT + row * ShopTabPage.CellH;
                var cell = FindChild(content, "CatalogItemShopContainer_" + i);
                CheckAt(cell, x1, x1 + ShopTabPage.CellW, y1, y1 + ShopTabPage.CellH,
                        $"第 {i + 1} 格的位置（col {col} / row {row} ⇒ 原版栅格算式）");
                CheckArt(FindChild(cell, "background"), "UI_Deck_Selection_Back_simple", "格底");
                // 格底**渲出来**的尺寸 = 格 + 2（原版 `background` 的 `sizeDelta = (2,2)`，锚点拉满）
                // 🆕 **2026-10-03**：接上纵向滚动 + 裁切之后，**照原版 `RectMask2D` 先按
                //   `Packs Scroll View` 的视口裁一刀** —— 最后一行会压在视口底边上（内容高 957 > 视口 952.38）。
                float bx1 = Mathf.Max(x1 - 1f, ShopTabPage.ScrollView.x1);
                float bx2 = Mathf.Min(x1 + ShopTabPage.CellW + 1f, ShopTabPage.ScrollView.x2);
                float by1 = Mathf.Max(y1 - 1f, ShopTabPage.ScrollView.y1);
                float by2 = Mathf.Min(y1 + ShopTabPage.CellH + 1f, ShopTabPage.ScrollView.y2);
                CheckRectPx(FindChild(cell, "background"), bx1, bx2, by1, by2,
                            "格底渲出来的矩形（= 格 + 2，**已按 `Packs Scroll View` 视口裁过**）");
                CheckArt(FindChild(cell, "Generic UI Button"), "40K_button", "价格钮底图");
                CheckNear(TintOf(FindChild(cell, "Generic UI Button")).g, 0.637f, 0.01f,
                          "价格钮的色 = **(0.902,0.637,0.18)**（原版 `m_Color` 原文）");
                CheckTrue(TextOf(FindChild(cell, "Button Text")) == offers[i].Price,
                          $"格 {i + 1} 的价格文本 = `{offers[i].Price}`");
                CheckArt(FindChild(cell, "Counter"), "40K_main_deck_card_counter", "拥有数角标底");
                CheckTrue(TextOf(FindChild(cell, "Text (TMP)")) == "x" + ShopData.OwnedOf(p, i),
                          $"格 {i + 1} 的拥有数 = `x{ShopData.OwnedOf(p, i)}`");
                // `Available Counter`：**有限购信息才画**
                bool wantAvail = offers[i].AvailableMax > 0;
                CheckTrue((FindChild(cell, "Available Counter") != null) == wantAvail,
                          wantAvail ? $"格 {i + 1} 有 `Available Counter`（限购 {offers[i].AvailableMax}）"
                                    : $"格 {i + 1} 没有限购信息 ⇒ **不画** `Available Counter`");
                // 两个出厂 INACT 的角标**不建**
                CheckTrue(FindChild(cell, "TimedOffer") == null && FindChild(cell, "New") == null,
                          $"格 {i + 1}：`TimedOffer` / `New` **不建**（出厂 INACT，且原版锚点算出来落在格外面）");

                // ---- 🔴 「谁压谁 / 谁出界」这一类：**只能量【渲出来】的矩形**，量节点位置量不到 ----
                //     （2026-09-23 实拍才发现的：名字/类型两行**压在商品图上**、`Available` **跨到下一行**，
                //      而当时的 178 条断言**全绿** —— 它们只管「件在不在」「节点在不在原版矩形中心」。）
                var artNode = FindChild(cell, "Art") ?? FindChild(cell, "ArtPlaceholder");
                float ax1, ay1, ax2, ay2, tx1, ty1, tx2, ty2, nx1, ny1, nx2, ny2, vx1, vy1, vx2, vy2;
                bool hasArt = RectOf(artNode, out ax1, out ay1, out ax2, out ay2);
                bool hasTy = RectOf(FindChild(cell, "Type"), out tx1, out ty1, out tx2, out ty2);
                bool hasNm = RectOf(FindChild(cell, "Name"), out nx1, out ny1, out nx2, out ny2);
                CheckTrue(hasArt, $"格 {i + 1} 有主图（真图或占位板）");
                if (hasArt && hasTy)
                    CheckTrue(!Overlaps(ax1, ay1, ax2, ay2, tx1, ty1, tx2, ty2),
                              $"格 {i + 1}：**主图不压类型行**（图 y {ay1:F0}..{ay2:F0} · 类型 y {ty1:F0}..{ty2:F0}）");
                if (hasTy && hasNm)
                    CheckTrue(!Overlaps(tx1, ty1, tx2, ty2, nx1, ny1, nx2, ny2),
                              $"格 {i + 1}：**类型行不压名字行**（类型 y {ty1:F0}..{ty2:F0} · 名字 y {ny1:F0}..{ny2:F0}）");
                // `Available Counter`：**整条要在格子里面**（第一版它中心在格底边上 ⇒ 半截跨到下一行）
                if (RectOf(FindChild(cell, "Available Counter"), out vx1, out vy1, out vx2, out vy2))
                {
                    CheckTrue(vy1 >= y1 - 0.5f && vy2 <= y1 + ShopTabPage.CellH + 0.5f,
                              $"格 {i + 1}：`Available Counter` **竖向不越出格子**"
                              + $"（实测 y {vy1:F0}..{vy2:F0}，格 {y1:F0}..{y1 + ShopTabPage.CellH:F0}）");
                    CheckTrue(vx1 >= x1 - 0.5f && vx2 <= x1 + ShopTabPage.CellW + 0.5f,
                              $"格 {i + 1}：`Available Counter` **横向不越出格子**");
                }
                // 格内所有件都别横着越界（价格钮 / 名字 / 类型 / 主图 一起过一遍）
                string[] insideNames = { "Name", "Type", "Art", "ArtPlaceholder", "Generic UI Button" };
                for (int k = 0; k < insideNames.Length; k++)
                {
                    float ix1, iy1, ix2, iy2;
                    if (!RectOf(FindChild(cell, insideNames[k]), out ix1, out iy1, out ix2, out iy2)) continue;
                    CheckTrue(ix1 >= x1 - 2f && ix2 <= x1 + ShopTabPage.CellW + 2f,
                              $"格 {i + 1}：`{insideNames[k]}` **横向在格内**（实测 x {ix1:F0}..{ix2:F0}）");
                    CheckTrue(iy1 >= y1 - 2f && iy2 <= y1 + ShopTabPage.CellH + 2f,
                              $"格 {i + 1}：`{insideNames[k]}` **竖向在格内**（实测 y {iy1:F0}..{iy2:F0}）");
                }
            }

            // ---- 🆕 2026-10-03：`Packs Scroll View` 的纵向滚动 + 视口裁剪 ----
            //   判据 = 原版实读 `menu_dump.py bundle_menus_assets_all "Card Shop Tab"`：
            //          `ScrollRect h=0 v=1 mode=1(Clamped) inertia=1 elasticity=0.1 decel=0.135` +
            //          `Viewport` 上的 `RectMask2D`；`Content` 由 `ContentSizeFitter(V=Preferred)` 撑到 `行数×475+7`。
            {
                var gs = win.GridScrollOf(p);
                CheckTrue(gs != null, "本页的栅格滚动区建了（`MenuScroll.TopAligned`）");
                if (gs != null)
                {
                    CheckTrue(gs.Vertical, "是**纵向**滚动（原版 `m_Vertical = 1` / `m_Horizontal = 0`）");
                    CheckTrue(!gs.Elastic, "是 **Clamped**（原版这一件的 `m_MovementType = 1`）");
                    float wantH = (offers.Length + ShopTabPage.GridCols - 1) / ShopTabPage.GridCols
                                  * ShopTabPage.CellH + ShopTabPage.GridPadT;
                    CheckNear(gs.ContentX2 - gs.ContentX1, wantH, 0.5f,
                              "`Content` 高 = 行数×475+7（原版 `ContentSizeFitter(Vertical = Preferred)`）");
                    CheckNear(gs.MaxOffset, wantH - ShopTabPage.ScrollView.H, 0.5f,
                              "可滚范围 = 内容高 − 视口高（`Packs Scroll View` 127.62..1080）");

                    // 顶：最后一格**压在视口底边上** ⇒ 原版 `RectMask2D` 把它裁掉一截
                    var lastCell = FindPath(pg, "Packs Scroll View/Viewport/Content/CatalogItemShopContainer_"
                                                + (offers.Length - 1));
                    float cx1, cy1, cx2, cy2;
                    bool has0 = RectOf(FindChild(lastCell, "background"), out cx1, out cy1, out cx2, out cy2);
                    CheckTrue(has0 && cy2 <= ShopTabPage.ScrollView.y2 + 0.5f,
                              $"在顶时最后一格的格底**被视口裁住**（实测底边 {cy2:F2} ≤ 视口底 {ShopTabPage.ScrollView.y2:F2}）");

                    // 滚到底：内容末尾对齐视口底 ⇒ 最后一格**完整**了
                    gs.SetOffset(gs.MaxOffset);
                    var lastCell2 = FindPath(pg, "Packs Scroll View/Viewport/Content/CatalogItemShopContainer_"
                                                 + (offers.Length - 1));
                    CheckTrue(lastCell2 != null, "滚到底之后最后一格**还在**（没被裁掉）");
                    if (lastCell2 != null)
                    {
                        // ⚠️ 最后一格的列号**按页不同**（4 件 ⇒ 最后在 col 1；3 件 ⇒ col 0）—— 别写死
                        int lc = (offers.Length - 1) % ShopTabPage.GridCols;
                        CheckAt(lastCell2, ShopTabPage.ScrollView.x1 + lc * ShopTabPage.CellW,
                                ShopTabPage.ScrollView.x1 + (lc + 1) * ShopTabPage.CellW,
                                ShopTabPage.ScrollView.y2 - ShopTabPage.CellH, ShopTabPage.ScrollView.y2,
                                "滚到底 ⇒ 最后一格**贴着视口底**（内容末尾 = 视口底）");
                        float hx1, hy1, hx2, hy2;
                        if (RectOf(FindChild(lastCell2, "background"), out hx1, out hy1, out hx2, out hy2))
                            CheckNear(hy2 - hy1, ShopTabPage.CellH + 2f, 2f,
                                      "滚到底 ⇒ 格底**不再被裁**（高回到 格高 + 2）");
                    }
                    // 回顶必须精确回到 0（`SetOffset` 是**绝对**设值 —— 2026-09-23 那条重入教训）
                    gs.SetOffset(0f);
                    CheckNear(gs.Offset, 0f, 0.01f, "回顶后偏移精确 = 0");
                    // 滚轮一格 = 48px（照卡组编辑那条已验过的路：`dy * 0.4f`，新输入系统一格 ±120）
                    gs.Wheel(-120f);
                    CheckNear(gs.Offset, 4.62f, 0.5f, "往下滚一格 ⇒ 偏移 +48px（被 Clamped 夹在 4.62）");
                    gs.SetOffset(0f);
                }
            }

            // 空态遮罩：出厂 INACT，我们每页都有商品 ⇒ 恒不显示
            var ew = FindChild(pg, "Empty Collection Warning");
            CheckTrue(ew != null && !ew.gameObject.activeSelf,
                      "`Empty Collection Warning` **建成但不显示**（原版在列表为空时才开）");
        }

        // ---------------- 买一件（红线：点了必须有反应）----------------
        Section("买一件：**点了必须有反应**（红线：不许静默失败；边界②：不做真实经济 ⇒ 不扣钱）");
        win.tabButtons.Click(0);
        var pg0 = FindChild(root, ShopData.Pages[0].Prefab);
        int before = ShopData.OwnedOf(0, 0);
        var hit = FindPath(pg0, "Packs Scroll View/Viewport/Content/CatalogItemShopContainer_0/Hit");
        CheckTrue(hit != null, "第 1 格的点击区建了");
        if (hit != null)
        {
            var wb = hit.GetComponent<WindowButton>();
            CheckTrue(wb != null, "点击区挂着 `WindowButton`");
            if (wb != null) wb.ClickForTest();
            Check(ShopData.OwnedOf(0, 0), before + 1, "买了之后**拥有数 +1**");
            CheckTrue(TextOf(FindChild(FindChild(root, ShopData.Pages[0].Prefab),
                                       "Text (TMP)")) == "x" + (before + 1),
                      "画面上的拥有数也跟上了（**卡变了就重建视图**）");
        }
        ClosePackAndReopenShop(win);   // A7：买卡包会弹全屏的开包窗 ⇒ 还原成「商店开着」再往下做

        // ---------------- 🆕 2026-10-03：传奇重复购买确认（§三 第 29 条 A16）----------------
        //   判据 = `d:/2/tools/decomp_full/CatalogItemContainer__TryPurchase.c`：
        //   `Item.Rarity == 4 && GetOwnedCount(Item) == 1` ⇒ 先弹 `MenuShop/ExtraLegendaryWarning`，
        //   **确认回调才走真正的购买**；其余情况直接买。
        Section("传奇重复购买确认（原版 `CatalogItemContainer__TryPurchase`）");
        {
            win.tabButtons.Click(0);
            CheckTrue(ShopData.NeedsLegendaryConfirm(0, 2),
                      "第 3 件（`Space Wolves Booster`：Rarity 4 + 已拥有 **1**）⇒ **要弹确认**");
            CheckTrue(!ShopData.NeedsLegendaryConfirm(0, 0),
                      "第 1 件（已拥有 3）⇒ **不弹**（判据盯的是 `GetOwnedCount == 1`）");
            CheckTrue(!ShopData.NeedsLegendaryConfirm(0, 1),
                      "第 2 件（Rarity 0）⇒ **不弹**");

            int before2 = ShopData.OwnedOf(0, 2);
            var pg = win.PageOf(0);
            CheckTrue(pg != null, "第 1 页的 `ShopTabPage` 拿得到");
            string got = pg != null ? pg.Buy(2) : "没有页";
            Check(got, "", "点了传奇那一件 ⇒ **这一次没买**（原版先弹确认框）");
            Check(ShopData.OwnedOf(0, 2), before2, "…拥有数**没变**（取消分支 = 什么都不做）");
            var wm = win.Manager;
            CheckTrue(wm != null && wm.popUpWindow != null, "确认框**真弹出来了**");
            if (wm != null && wm.popUpWindow != null) wm.popUpWindow.Close();
            // 非传奇那件照旧直接买（单机行为一字不改）
            int before0 = ShopData.OwnedOf(0, 0);
            if (pg != null) pg.Buy(0);
            Check(ShopData.OwnedOf(0, 0), before0 + 1, "非传奇那一件**照旧直接买**（不弹框）");
        }
        ClosePackAndReopenShop(win);   // A7：同上（这一件也是卡包 ⇒ 也会弹开包窗）

        // ---------------- 🆕 2026-10-03：`Booster Info Popup`（§三 第 29 条 A6）----------------
        //   判据 = `资料/阶段二_商店_原版规格.md` **§五·二 / §五·二·一**（19 行逐节点几何表）
        //        + MB `MonoBehaviour_7872967592023106223.json` 的窗口字段（type/placement/closeOnESC/scale）。
        //   🔴 **入口是我们定的**：点卡包格的**商品主图**（原版走 `ShopOfferContainer.OnClick → OpenContainer`，
        //      那一族我们没有）⇒ 见 `Shell/BoosterInfoPopup.cs` 文件头。只在 `Booster Pack` 上接。
        Section("`Booster Info Popup`（A6；⚠️ 入口是我们定的，不是复刻）");
        {
            win.tabButtons.Click(0);
            var pc0 = FindChild(root, ShopData.Pages[0].Prefab);
            CheckTrue(CountByPrefix(pc0, "InfoHit") == 4,
                      "Cards 页 4 件**都是卡包** ⇒ 4 个 `InfoHit`（实测 " + CountByPrefix(pc0, "InfoHit") + "）");
            win.tabButtons.Click(1);
            var pc1 = FindChild(root, ShopData.Pages[1].Prefab);
            CheckTrue(CountByPrefix(pc1, "InfoHit") == 0, "Daily 页（非卡包）**没有**这个入口");
            win.tabButtons.Click(0);

            var pgB = win.PageOf(0);
            var pop = pgB != null ? pgB.OpenBoosterInfo(0) : null;
            CheckTrue(pop != null, "`Booster Info Popup` 开出来了（入口 `ShopTabPage.OpenBoosterInfo`）");
            if (pop != null)
            {
                var t = pop.transform;
                // ---- 窗口字段（MB 原文，逐个抄的）----
                Check(pop.type, WindowType.Popup, "`type` = 1 (**Popup**)（MB 原文）");
                Check(pop.placement, WindowsPlacement.World,
                      "`windowsPlacement` = 10 (**World**，不是弹窗那档 15)（MB 原文）");
                CheckTrue(pop.closeOnEsc, "`closeOnESC` = 1（MB 原文）");
                CheckNear(pop.extraScaleSmallScreen, 1.2f, 1e-4f, "`extraScaleSmallScreen` = 1.2（MB 原文）");
                CheckTrue(pop.Host == win, "宿主商店窗挂上了（购买那条链要走它的传奇确认闸门）");

                // ---- 层一：压暗层 + 窗底 ----
                CheckAt(FindChild(t, "Menu Dark Background"), -1327.30f, 3247.30f, -746.18f, 1826.18f,
                        "压暗层 `Menu Dark Background`");
                CheckNear(TintOf(FindChild(t, "Menu Dark Background")).a, 0.773f, 0.005f, "压暗层 α = 0.773");
                var wn = FindChild(t, "window");
                CheckAt(wn, 395.72f, 1524.28f, 188.35f, 851.65f, "`window`");
                CheckArt(FindChild(wn, "Generic Window Red Background Big"), "UI_Deck_Information_Back",
                         "窗底 `Generic Window Red Background Big`");

                // ---- 关闭钮（**这一件自己的底图是画出来的**，与对局历史那扇不同）----
                var cb2 = FindChild(wn, "Generic Close Button Orange");
                CheckAt(cb2, 1487.06f, 1561.45f, 159.83f, 235.44f, "`Generic Close Button Orange`");
                CheckArt(FindChild(cb2, "Background"), "40k_general_bt_yellow", "关闭钮 `Background`");
                CheckArt(FindChild(cb2, "Icon"), "40k_general_bt_yellow_close", "关闭钮 `Icon`");

                // ---- 主图：`background` 画商品图、`foreground` 只建节点（原版两处都空）----
                CheckArt(FindPath(wn, "Artwork/background"), ShopData.Offers(0)[0].Art, "主图 = 商品表的 `Art`");
                var fg = FindPath(wn, "Artwork/foreground");
                CheckTrue(fg != null && fg.GetComponentInChildren<ImageQuad>() == null,
                          "`foreground` **只建节点、不画**（原版 `m_Sprite` 也是空，判据不足 ⇒ 留白不猜）");

                // ---- 四段字：文本 + 字号 + 字距（盯**原版参数**，不是盯我们自己的常量）----
                var title = FindPath(wn, "Text/Title");
                Check(TextOf(title), ShopData.Offers(0)[0].Name, "`Title` = 商品名");
                // ⚠️ 原版这三条 TMP 都是 **`auto(min-max)`**（Title 3–40 · Category 3–39 · Descr 3–35）
                //    ⇒ **字号是被框缩过的**（实测 Title 34.29 / Category 29.69），**不能断「≈ 40」**。
                //    判据改成「落在原版的 auto 区间里」+「渲染宽不超出框」（同 `已知的坑.md` 那条 AutoFitBox 教训）。
                CheckTrue(title.GetComponentInChildren<Label>().FontPxNow <= 40.01f
                          && title.GetComponentInChildren<Label>().FontPxNow >= 3f,
                          "`Title` 字号落在原版 `auto(3-40)` 区间（实测 "
                          + title.GetComponentInChildren<Label>().FontPxNow.ToString("F2") + "）");
                var cat = FindPath(wn, "Text/Category");
                Check(TextOf(cat), "Booster Pack", "`Category`（Rarity 0 ⇒ 用商品表的 `Type`）");
                CheckTrue(cat.GetComponentInChildren<Label>().FontPxNow <= 39.01f
                          && cat.GetComponentInChildren<Label>().FontPxNow >= 3f,
                          "`Category` 字号落在原版 `auto(3-39)` 区间（实测 "
                          + cat.GetComponentInChildren<Label>().FontPxNow.ToString("F2") + "）");
                CheckNear(cat.GetComponentInChildren<Label>().CharSpacing, -1.8f, 0.01f,
                          "`Category` **字距 −1.8**（原版 `m_characterSpacing`）");
                var desc = FindPath(wn, "Text/Descripton");
                Check(TextOf(desc), BoosterInfoPopup.DescSample,
                      "`Descripton` = **prefab 出厂文本**（原版运行期由服务端 item 覆盖）");
                var cc2 = FindPath(wn, "Text/CrateCounter");
                Check(TextOf(cc2), BoosterInfoPopup.CrateCounterText, "`CrateCounter` 出厂文本");
                CheckNear(cc2.GetComponentInChildren<Label>().CharSpacing, -2f, 0.01f,
                          "`CrateCounter` **字距 −2**（原版）");

                // ---- 保底进度条：底 / 填充 / 描边 / 计数 / 说明图标 ----
                var sl = FindPath(wn, "Text/Booster pack guarantee Slider");
                CheckArt(FindChild(sl, "Background"), "40k_campaign_bar_bg", "进度条底");
                CheckArt(FindChild(sl, "Outline"), "40k_campaign_bar_outline", "进度条描边");
                CheckArt(FindPath(sl, "Fill Area/Fill"), "40k_campaign_bar_fill", "进度条填充");
                CheckArt(FindPath(sl, "Fill Area/Fill/end"), "40k_campaign_bar_end", "填充端帽 `end`");
                // 填充宽 = 值比例 × 406.58（原版 `Fill Area` 的宽）
                CheckNear(pop.SliderFillW, 203.29f, 0.5f,
                          "填充宽 = 100/200 × 406.58（原版 `Slider.m_FillRect` 的语义）");
                Check(TextOf(FindChild(sl, "counter")), BoosterInfoPopup.CounterSample, "`counter` 出厂占位值");
                CheckArt(FindChild(sl, "Tooltip"), "40k_generic_bt_info", "`Tooltip` 图标");

                // ---- 两个购买钮 ----
                var pd2 = FindPath(wn, "Text/Purchase buttons/Price Display/Generic UI Button");
                CheckArt(pd2, "40K_button", "`Price Display` 的 `40K_button`");
                Check(TextOf(FindChild(pd2, "Button Text")), ShopData.Offers(0)[0].Price, "价格文本 = 商品表的价格");
                var wsB = FindPath(wn, "Text/Purchase buttons/WebShop Button");
                CheckArt(FindChild(wsB, "Highlight"), "OctagonUI_Filled_Fade_SDF", "`WebShop` 的 `Highlight`");
                CheckArt(FindChild(wsB, "Button Image"), "40K_button", "`WebShop` 的 `Button Image`");
                CheckArt(FindChild(wsB, "Icon"), "40K_Icon_Discount_Gold", "`WebShop` 的 `Icon`");
                float ix1, iy1, ix2, iy2;
                if (RectOf(FindChild(wsB, "Icon"), out ix1, out iy1, out ix2, out iy2))
                    CheckNear(iy2 - iy1, 51.88f * 1.2f, 1.5f,
                              "`Icon` **画出来 = 51.88 × `localScale 1.2` = 62.26**（原版就带这个缩放）");
                Check(TextOf(FindChild(wsB, "Button Text")), BoosterInfoPopup.WebShopText, "`Save More!`");

                // ---- 换一件传奇的：`Category` 走原文那档 ----
                pop.Show(0, 2);
                // ⚠️ 路径要**从弹窗根算起**（`Text` 是 `window` 的子节点）—— 第一版漏了 `window/`
                //    ⇒ `FindPath` 返回 null、`TextOf` 什么都读不到（自检报「实得 []」）。
                Check(TextOf(FindPath(pop.transform, "window/Text/Category")), "Legendary Booster Pack",
                      "Rarity 4 ⇒ `Category` = `Legendary Booster Pack`（原文那档）");

                // ---- 🆕 2026-10-03（A12-P1 欠下的断言）：`Tooltip` 图标的悬停 tooltip ----
                //   判据 = 原版 `EverguildTooltipTrigger`（`text = "MenuShop/BoosterInfo/LegendaryTooltip"`，
                //   词条表在远端 CCD ⇒ 本地没有）+ **`m_RaycastPadding = (−15,−15,−15,−15)`**
                //   （**负值 = 外扩**，反证写在 `BoosterInfoPopup.BuildTooltipHit` 的注释里）。
                Section("`Booster Info Popup` 的 `Tooltip` 图标：悬停出面板 + 命中区外扩 15px");
                {
                    CheckTrue(BoosterInfoPopup.TipBody.Trim() == "",
                              "`BoosterInfoPopup.TipBody` **一个字都没编**（面板照弹、正文空）");
                    var layerS = PointerLayer.Instance;
                    var tipNode = FindPath(pop.transform, "window/Text/Booster pack guarantee Slider/Tooltip");
                    var tipQ = tipNode != null ? tipNode.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(tipQ != null, "`Tooltip` 图标在（下面拿它的**渲染矩形**当判据）");
                    float tx1, ty1, tx2, ty2;
                    bool hasTip = RectOf(tipNode, out tx1, out ty1, out tx2, out ty2);
                    CheckTrue(hasTip, "`Tooltip` 图标的渲染矩形量得到");
                    if (hasTip)
                    {
                        // ⚠️ 先钉**图标自己的位置**（原版 `TooltipR` = 1411.00,667.18 → 1455.88,712.09）
                        //    —— 这样下面「外扩 15」那条就不是自证
                        CheckNear((tx1 + tx2) * 0.5f, 1433.44f, 1f, "图标中心 x = **1433.44**（原版 `TooltipR` 的中心）");
                        CheckNear((ty1 + ty2) * 0.5f, 689.635f, 1f, "图标中心 y = **689.635**（同上）");
                        CheckNear(tx2 - tx1, 44.88f, 1f, "图标宽 = **44.88**（原版 `TooltipR` 的 sizeDelta）");
                    }
                    int scS = Tooltip.ShowCount;
                    float tipCx = (tx1 + tx2) * 0.5f, tipCy = (ty1 + ty2) * 0.5f;
                    var hbS = layerS != null ? layerS.HoverAt(tipCx, tipCy) : null;
                    // 🔴 这一条要的是**真鼠标**那条路（`PointerLayer.HoverAt`）。
                    //    ⚠️ **若它红了，先看下面这句诊断**：两条命中区**同队列**时，`PointerLayer.HitButton`
                    //    那句「同队列再比 z」就退化成「`FindObjectsByType` 的枚举顺序」—— 因为
                    //    `ImageQuad` 的世界 z **恒为 0**（`LayoutSpace.ToWorld` 就给 0）⇒ **谁吃到命中不可控**
                    //    （正是 CLAUDE.md §三 那条「同队列谁盖谁不可控」）。
                    // 🔴 **2026-10-03 就地更正（铁律 5）**：这里原来写着「本件**只写断言、不改实现**」——
                    //    **当天就改了实现**：压暗层的命中区从 `QHit` 降到压暗层自己那一档
                    //    （`BoosterInfoPopup.QShadeHit`），窗内四个命中区不再被抢。
                    //    分档的三条断言在**下一节**（断的是「谁高谁低」这个关系，不是某一个点的巧合）。
                    CheckTrue(hbS != null && hbS == BoosterInfoPopup.TipHit,
                              "`PointerLayer.HoverAt(图标中心)` 打到的**就是** `BoosterInfoPopup.TipHit`"
                              + (hbS != null && hbS != BoosterInfoPopup.TipHit
                                 ? $" —— ⚠️ 实得 `{hbS.name}`；若是压暗层的 `CloseHit`，说明"
                                   + "「压暗层命中区低于窗内命中区」这条分档被改回去了（见下一节）"
                                 : ""));
                    // ② 悬停**接线**本身：直调 `Enter/Exit`（**同 `WindowButton.AuditHoverSwap` 的口径**，
                    //    批处理里没有帧循环 ⇒ 这是唯一入口）—— 这一条**不依赖**上面那次命中，
                    //    所以「同队列不可控」不会把它一起拖红。
                    // 🔴 **2026-10-03 就地更正（铁律 5）**：这里原来少了「**先把指针挪开**」这一步 ——
                    //    上面 `HoverAt(图标中心)`（`:645`）那次**真悬停**已经派发过一次 `Enter()` 了
                    //    （`TipHovers` 0→1，面板也正是那一次弹出来的，所以 `:664/:666/:667` 都是绿的），
                    //    而 `WindowButton.Enter()` 是**幂等**的（`PromptPopup.cs:435` 的 `if (Hovered) return;`，
                    //    等价原版 `IPointerEnterHandler`「每次进入只发一次」）⇒ 这里再叫一次计数不涨，
                    //    断言报「期望 2 实得 1」。**实现是对的、是断言少了前提**（不是漏触发）。
                    //    ⇒ 挪到空白清掉悬停态、再量增量；顺带把「幂等」也钉一条（免得下次又把 `repeat` 当漏触发）。
                    var wbTip = BoosterInfoPopup.TipHit;
                    if (layerS != null) layerS.HoverAt(5f, 5f);     // 挪开 ⇒ `Exit()`（`onExit = Tooltip.Hide`）
                    int tip1 = BoosterInfoPopup.TipHovers;
                    if (wbTip != null) wbTip.Enter();
                    Check(BoosterInfoPopup.TipHovers, tip1 + 1,
                          "悬停 ⇒ `TipHovers` **+1**（原版 `EverguildTooltipTrigger.OnPointerEnter`）");
                    if (wbTip != null) wbTip.Enter();                // 已悬停时再来一次
                    Check(BoosterInfoPopup.TipHovers, tip1 + 1,
                          "…**已悬停**时再叫一次 `Enter()` **不重复计**（原版 `IPointerEnterHandler` 每次进入只发一次；"
                          + "`WindowButton.Enter` 开头那句 `if (Hovered) return;`）");
                    CheckTrue(Tooltip.ShowCount >= scS + 1,
                              "…`Tooltip.ShowCount` 也涨了（面板**照原版的时机弹出来了**）");
                    CheckTrue(Tooltip.Visible, "…`Tooltip.Visible == true`");
                    CheckTrue((Tooltip.ShownBody ?? "").Trim() == "", "…面板里的**正文是空的**（一个字都没编）");
                    // 面板**钉在图标上**：原版这一件 `tooltipAnchor = 0`（None）⇒ pivot (.5,.5)
                    //   ⇒ 面板中心 == 图标中心（**不跟鼠标**）
                    CheckNear(LayoutSpace.PxX(Tooltip.PanelCenter.x), tipCx, 0.5f,
                              "面板中心 x = 图标中心 x（`tooltipAnchor = 0` ⇒ pivot (.5,.5)）");
                    CheckNear(LayoutSpace.PxY(Tooltip.PanelCenter.y), tipCy, 0.5f,
                              "…y 同（`offset = (0,0,0)`）—— 与锻造页那件的 anchor 25 是**两个不同的值**，别互抄");
                    // ③ 离开 ⇒ 收
                    if (wbTip != null) wbTip.Exit();
                    Tooltip.FinishFade();                           // ⚠️ 批处理没有帧循环 ⇒ 手动结束淡出
                    CheckTrue(!Tooltip.Visible, "指针离开 ⇒ `Tooltip.Visible == false`（原版 `OnPointerExit` 立刻收）");
                    // 🔴 命中区**外扩 15px**（原版 `m_RaycastPadding = (−15,−15,−15,−15)`）
                    // ⚠️ `RectOf` 那句要在 `if` 的**条件里**再调一次（不是偷懒）：C# 的确定赋值分析
                    //    只认「同一个布尔表达式里 `out` 出来」的变量，存进 `bool hasHit` 之后就不认了（CS0165）。
                    //    多调一次是**纯读**、无副作用。
                    float hx1, hy1, hx2, hy2;
                    var hitNodeS = BoosterInfoPopup.TipHit != null ? BoosterInfoPopup.TipHit.transform : null;
                    bool hasHit = hitNodeS != null && RectOf(hitNodeS, out hx1, out hy1, out hx2, out hy2);
                    CheckTrue(hasHit, "命中区的渲染矩形量得到");
                    if (hasHit && hasTip && RectOf(hitNodeS, out hx1, out hy1, out hx2, out hy2))
                    {
                        CheckNear(hx1, 1396.00f, 1f, "命中区左边缘 = **1396.00**（= 原版 `TooltipR.x1 1411.00 − 15`）");
                        CheckNear(hy1, 652.18f, 1f, "命中区上边缘 = **652.18**（= 667.18 − 15）");
                        CheckNear(hx2, 1470.88f, 1f, "命中区右边缘 = **1470.88**（= 1455.88 + 15）");
                        CheckNear(hy2, 727.09f, 1f, "命中区下边缘 = **727.09**（= 712.09 + 15）");
                        CheckNear(hx2 - hx1, (tx2 - tx1) + 30f, 1f,
                                  $"…宽 = 图标宽 + 30（外扩，**不是内缩**：实测 {hx2 - hx1:F2} vs 图标 {tx2 - tx1:F2}）");
                    }
                    // 收尾：把指针层的悬停态清掉（离开 ⇒ 收；批处理没有帧循环 ⇒ 手动结束淡出）
                    if (layerS != null) layerS.HoverAt(5f, 5f);
                    Tooltip.FinishFade();
                    CheckTrue(!Tooltip.Visible, "指针挪到空白 ⇒ `Tooltip.Visible == false`");
                }

                // ---- 🆕 2026-10-03：**分档** —— 压暗层不许抢走窗内命中 ----
                //   判据 = `CLAUDE.md` §三「**分层要用渲染队列，不能用 z**」。
                //   🔴 机制：`ImageQuad.Create` 造出来的 quad **世界 z 恒为 0**（`LayoutSpace.ToWorld` 就给 0）
                //      ⇒ 两条命中区**同队列**时 `PointerLayer.HitButton` 的「再比 z」退化成
                //      `FindObjectsByType` 的**枚举顺序** ⇒ 谁吃到命中不可控。
                //   🔴 修前实测（`_tmp_view/shop.log:11896`）：压暗层 `Menu Dark Background/CloseHit`
                //      与窗内**四个**命中区同档（都是 `QHit = 3079`）⇒ 它把 `Tooltip` 图标、**价签**、
                //      `WebShop Button` 全抢走了 —— 那两颗钮**点不动、点下去只会关窗**。
                //   ⚠️ 这一节断的是**分档关系**（队列谁高谁低）+ **真鼠标那条路上命中的是谁**，
                //      不是某一个点的巧合 ⇒ 下次谁再把压暗层的命中区提回内容那一档，这里会红。
                Section("`Booster Info Popup`：压暗层命中区**严格低于**窗内命中区（分档 ⇒ 命中唯一）");
                {
                    var layerQ = PointerLayer.Instance;
                    var darkHitN = FindPath(t, "Menu Dark Background/CloseHit");
                    var darkHitQ = darkHitN != null ? darkHitN.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(darkHitQ != null,
                              "压暗层 `CloseHit` 带 `ImageQuad`（`PointerLayer` 靠它量矩形 + 读队列）");
                    // ⚠️ 矩形的中心一律量 **`ImageQuad` 自己**的位置 + `WorldW/H`（`RectOf` 就是这条口径）——
                    //    `MenuDraw.Hit` 把命中区那个**节点**摆在父原点，拿节点 `position` 当中心会量歪。
                    string[] innerHits =
                    {
                        "window/Generic Close Button Orange/Hit",                  // 关闭钮
                        "window/Text/Purchase buttons/Price Display/Hit",          // 价签（购买）
                        "window/Text/Purchase buttons/WebShop Button/Hit",         // `WebShop`（只出声、不跳转）
                        "window/Text/Booster pack guarantee Slider/Tooltip/Hit",   // 说明图标（悬停出 tooltip）
                    };
                    for (int i = 0; i < innerHits.Length; i++)
                    {
                        var hn = FindPath(t, innerHits[i]);
                        var wbH = hn != null ? hn.GetComponent<WindowButton>() : null;
                        var hq = hn != null ? hn.GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(wbH != null && hq != null,
                                  "`" + innerHits[i] + "` 在（`WindowButton` + `ImageQuad` 都有）");
                        if (wbH == null || hq == null) continue;
                        float x1, y1, x2, y2;
                        if (!RectOf(hn, out x1, out y1, out x2, out y2))
                        { CheckTrue(false, "…它的渲染矩形量得到"); continue; }
                        float hcx = (x1 + x2) * 0.5f, hcy = (y1 + y2) * 0.5f;
                        var gotH = layerQ != null ? layerQ.ButtonAt(hcx, hcy) : null;
                        CheckTrue(gotH == wbH,
                                  "…`PointerLayer.ButtonAt` 打在它的中心 ⇒ 命中的**就是它自己**"
                                  + "（实得 `" + (gotH != null ? gotH.name : "<null>") + "`）");
                        CheckTrue(darkHitQ == null || hq.RenderQueue > darkHitQ.RenderQueue,
                                  "…它的命中队列 **>** 压暗层的（" + hq.RenderQueue + " > "
                                  + (darkHitQ != null ? darkHitQ.RenderQueue.ToString() : "?")
                                  + "）—— 唯一命中，不靠枚举顺序");
                    }
                    // 反方向：**点窗外仍然关窗**（压暗层的命中区不许低到商店页那一档 —— 那会穿透到商品格上）。
                    //   取 `(360,1010)`：在 `DarkR` 内、在 `window` 之外（窗底到 851.65）、离顶栏（y ≤ 148）很远；
                    //   ⚠️ 这个点上压着的商店页命中区最高 `ShopWindow.QCellInfoHit = 3031` ⇒ 正是要证明「压暗层在它之上」。
                    if (layerQ != null && darkHitN != null)
                    {
                        var gotD = layerQ.ButtonAt(360f, 1010f);
                        CheckTrue(gotD != null && gotD == darkHitN.GetComponent<WindowButton>(),
                                  "点**窗外**（360,1010）打到的仍是压暗层 `CloseHit`"
                                  + "（原版 `BackgroundCloseButton` ⇒ 关窗）"
                                  + "（实得 `" + (gotD != null ? gotD.name : "<null>") + "`）");
                    }
                }

                // ---- 实拍（开着的状态）----
                pop.Show(0, 0);
                Shoot("04_商店_卡包详情窗.png");

                // 🆕 A17：这一扇的换图按钮（关闭钮的**圆底** + 价签 + `WebShop`）逐个悬停验一遍
                CheckHoverSwap(pop.transform, "Booster Info Popup");

                // ---- 点窗外/关闭钮 ⇒ 关窗 ----
                var darkHit = FindPath(pop.transform, "Menu Dark Background/CloseHit");
                CheckTrue(darkHit != null, "压暗层上有 `CloseHit`（原版 `BackgroundCloseButton`）");
                if (darkHit != null) darkHit.GetComponent<WindowButton>().ClickForTest();
                Check(pop.CurrentState, WindowState.Closed, "点窗外 ⇒ 关窗");
                CheckTrue(!pop.gameObject.activeSelf, "…且节点也关了");

                Debug.Log(P + "   " + pop.Dump());
            }
        }

        ClosePackAndReopenShop(win);   // A7 的购买会把商店关掉 ⇒ 实拍前先还原

        // ---------------- 🆕 2026-10-03：`Booster Pack Open Window`（§三 第 29 条 A7）----------------
        //   判据 = `资料/阶段二_商店_原版规格.md` **§五·三**（几何/依赖）+
        //          MB `MonoBehaviour_9012570135841684515.json`（窗口字段）+
        //          `Booster Window Open` clip 的**末帧关键帧**（5 张卡的位姿）+
        //          `d:/2/tools/decomp_full/BoosterPackOpenWindow__*.c`（行为）。
        //   🔴 **期望值全部盯原版**：卡位 x/scale 来自 clip（−730/−360/0/360/730 · 151），
        //      父级缩放 0.876259982585907 来自 RT 实读 ⇒ `CardK = 151 × 0.87626 = 132.3153`。
        Section("`Booster Pack Open Window`（A7；5 张卡的位姿来自原版 clip，不是我们挑的）");
        {
            win.tabButtons.Click(0);
            var pgA = win.PageOf(0);
            CheckTrue(pgA != null, "第 1 页拿得到");
            CheckTrue(pgA != null && ShopData.Offers(0)[3].Type == "Booster Pack",
                      "第 4 件是卡包（`Type == \"Booster Pack\"`）");
            int beforeA = ShopData.OwnedOf(0, 3);
            if (pgA != null) pgA.Buy(3);
            Check(ShopData.OwnedOf(0, 3), beforeA + 1, "买了第 4 件 ⇒ 拥有数 +1");

            var bp = pgA != null ? pgA.LastBoosterPack : null;
            CheckTrue(bp != null, "**买完自动开包**（入口是我们定的：`ShopTabPage.DoBuy` → `OpenBoosterPack`）");
            if (bp == null) { }
            else
            {
                var t = bp.transform;

                // ---- 窗口字段（MB 原文，逐个抄的）----
                Check(bp.type, WindowType.Fullscreen, "`type` = 0 (**Fullscreen**)（MB 原文）");
                Check(bp.placement, WindowsPlacement.Canvas,
                      "`windowsPlacement` = **5 (Canvas)**（MB 原文；⚠️ 与商店的 10 / 详情窗的 10 / 弹窗 15 **都不同**）");
                Check(bp.closeOnEsc, false, "`closeOnESC` = **0**（MB 原文）");
                CheckNear(bp.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = 1.0（MB 原文）");
                CheckTrue(t.parent != null && t.parent.name == "2 - Canvas Holder Above upper bar",
                          "挂在 **`Canvas` 锚点**下（`windowsPlacement = 5` ⇒ `2 - Canvas Holder Above upper bar`；实得 `"
                          + (t.parent != null ? t.parent.name : "<null>") + "`）");

                // ---- ① 背景：**原版那一件 `Image` 的 `m_Sprite` 是 0** ⇒ 纯色块 ----
                var bg = FindChild(t, "Booster pack Background");
                CheckAt(bg, -100f, 2020f, -100f, 1180f, "背景 `Booster pack Background`（−100,−100 → 2020,1180）");
                CheckRectPx(bg, -100f, 2020f, -100f, 1180f, "背景**渲出来**的矩形（2120×1280，四周出血 100）");
                bool hasLeg = false;
                if (bp.Cards != null)
                    for (int i = 0; i < bp.Cards.Length; i++)
                        if (BoosterPackOpenWindow.RarityInt(bp.Cards[i]) == 4) hasLeg = true;
                var bgc = TintOf(bg);
                if (hasLeg)
                {
                    // `BoosterPackBackground.thereIsALegendaryCardbackgroundColor`（实读，**红分量 >1 是原版值**）
                    CheckNear(bgc.r, 1.513579f, 0.02f, "有传奇 ⇒ 背景色 R = **1.513579**（`thereIsALegendaryCardbackgroundColor`）");
                    CheckNear(bgc.g, 0.459480f, 0.02f, "…G = **0.459480**（同上）");
                    CheckNear(bgc.b, 0f, 0.02f, "…B = **0**（同上）");
                }
                else
                {
                    CheckNear(bgc.r, 1f, 0.02f, "无传奇 ⇒ 背景色 = 那张 `Image` 的 `m_Color` **(1,1,1,1)** · R");
                    CheckNear(bgc.g, 1f, 0.02f, "…G");
                    CheckNear(bgc.b, 1f, 0.02f, "…B");
                }
                CheckNear(bgc.a, 1f, 0.02f, "…α = 1");

                // ---- ② 卡位容器 + ⑤ 5 张卡（**位姿逐个对 clip 末帧**）----
                CheckAt(FindChild(t, "Booster Animation Parent"), 910f, 1010f, 490f, 590f,
                        "`Booster Animation Parent`（100×100，中心 = 屏心）");
                CheckAt(FindChild(t, "Cards"), 910f, 1010f, 490f, 590f, "`Cards`（与父同矩形）");
                float[] wantX = { -730f, -360f, 0f, 360f, 730f };
                const float AncS = 0.876259982585907f;      // `Booster Animation Parent` 的 RT 实测
                const float CardK = 151f * AncS;            // = 132.3153（clip 末帧 scale 151 × 父级缩放）
                for (int i = 0; i < 5; i++)
                {
                    var s = FindChild(t, "CardInBoosterPack UI " + (i + 1));
                    float cx = 960f + wantX[i] * AncS;
                    CheckTrue(s != null, "第 " + (i + 1) + " 格 `CardInBoosterPack UI " + (i + 1) + "` 建了");
                    if (s == null) continue;
                    CheckNear(PxOf(s.position.x), cx, 0.6f,
                              "第 " + (i + 1) + " 格 x = **" + cx.ToString("F2") + "**（clip 末帧 x=" + wantX[i]
                              + " × 父级 scl 0.87626 + 960）");
                    CheckNear(PxYOf(s.position.y), 540f, 0.6f, "第 " + (i + 1) + " 格 y = 540（容器中心）");
                }
                // 卡背**渲出来**的尺寸 = 2.17×3.14 卡单位 × `CardK` = 287.12 × 415.47
                CheckRectPx(FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 3/Cardback Container/Cardback"),
                            960f - 2.17f * CardK * 0.5f, 960f + 2.17f * CardK * 0.5f,
                            540f - 3.14f * CardK * 0.5f, 540f + 3.14f * CardK * 0.5f,
                            "第 3 格卡背渲出来的矩形（2.17×3.14 卡单位 × CardK=132.3153）");

                // ---- 出场态：**卡背开着、卡面关着、三个角标全关、两段提示字全关** ----
                for (int i = 1; i <= 5; i++)
                {
                    var bc = FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI " + i + "/Cardback Container");
                    var fd = FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI " + i + "/2DCard");
                    CheckTrue(bc != null && bc.gameObject.activeSelf, "第 " + i + " 格**卡背**开着（原版 `ChangeState(1)`）");
                    CheckTrue(fd != null && !fd.gameObject.activeSelf, "第 " + i + " 格**卡面**关着（翻之前）");
                }
                var up1 = FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/Card Ready for level up");
                var nb1 = FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/New Card Badge");
                var ban1 = FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/Ban Icon");
                CheckTrue(up1 != null && !up1.gameObject.activeSelf,
                          "`Card Ready for level up` 建成、出厂 **INACT**（翻牌后才按判据决定）");
                CheckTrue(nb1 != null && !nb1.gameObject.activeSelf,
                          "`New Card Badge` 建成、关着（`BasicCardUI.SetRawCardData` 一进来就 `SetActive(false)`）");
                CheckTrue(ban1 != null && !ban1.gameObject.activeSelf,
                          "`Ban Icon` 建成、**恒不显示**（`SetRawCardData` 末尾关它，翻牌链从不 `ToggleBanned`）");
                CheckArt(FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/Card Ready for level up/Image"),
                         "Card_Ready_For_Level_Up", "`Card Ready for level up` 的图");
                CheckArt(FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/Ban Icon/Image"),
                         "40k_Cross_icon_cross_big_Banned_card", "`Ban Icon` 的图");
                CheckTrue(bp.NewBadges[0] != null && bp.BanIcons[0] != null && bp.UpBadges[0] != null,
                          "三件角标都建出来了（不是没做）");
                Check(TextOf(FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/New Card Badge/Text")),
                      BoosterPackOpenWindow.NewBadgeText, "`New Card Badge/Text` 的文案");
                Check(TextOf(FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/Ban Icon/Banned Text")),
                      BoosterPackOpenWindow.BannedText, "`Ban Icon/Banned Text` 的文案");

                // ---- ⑤⑥ 两段提示字 ----
                var disc = FindChild(t, "Tap to discover");
                var clos = FindChild(t, "Tap to close");
                CheckTrue(disc != null && !disc.gameObject.activeSelf, "`Tap to discover` 出场**关着**（原版 `OnEnable` 里 `SetActive(false)`）");
                CheckTrue(clos != null && !clos.gameObject.activeSelf, "`Tap to close` 出场**关着**（5 张全翻开才出现）");
                Check(TextOf(FindChild(disc, "Text")), BoosterPackOpenWindow.DiscoverText, "`Tap to discover` 的文本（原版出厂英文）");
                Check(TextOf(FindChild(clos, "Text")), BoosterPackOpenWindow.CloseText, "`Tap to close` 的文本（原版出厂英文）");
                CheckAt(disc, 1156.67f, 1865f, 977f, 1080f, "`Tap to discover` 的矩形（708.33×103）");
                CheckAt(clos, 1156.67f, 1865f, 977f, 1080f, "`Tap to close` 的矩形（同上）");
                var dl = FindChild(disc, "Text") != null ? FindChild(disc, "Text").GetComponentInChildren<Label>() : null;
                if (dl != null)
                {
                    CheckNear(dl.FontPxNow, 49.82f, 0.6f, "提示字字号 = **49.82**（原版 `m_fontSize`）");
                    CheckNear(dl.color.r, 0.5566f, 0.01f, "提示字色 R = **0.5566**（原版 `m_fontColor`）");
                }
                // `Tap to close/Collider`：盖满整屏那块（原版 `NonDrawingGraphic`）
                var col = FindPath(t, "Tap to close/Collider");
                CheckAt(col, -534.74f, 3318.35f, -204.55f, 2027.98f, "`Tap to close/Collider`（3853.09×2232.53）");
                CheckTrue(bp.CloseSurfaceHit != null, "整屏那块**带 `ImageQuad` + `WindowButton`**"
                          + "（裸节点 `PointerLayer` 收不到 —— 卡组格那颗就是这么点不动的）");

                // ---- `timeToShowHelpText = 10`（MB 原文）：静止 10 s ⇒ `Tap to discover` 淡入 ----
                bp.AddTime(9f);
                CheckTrue(!disc.gameObject.activeSelf, "静止 **9 s** ⇒ `Tap to discover` **还没出来**（< 10 s）");
                bp.AddTime(1.5f);
                CheckTrue(disc.gameObject.activeSelf, "静止 **10.5 s** ⇒ `Tap to discover` 出现（原版 `timeToShowHelpText = 10`）");

                // ---- 稀有度 → 粒子名的映射（判据 = `CardInBoosterPack.contentByRarities[]` 的实测分组；
                //      **这一条与「导出器跑没跑」无关**，所以卡包窗一建出来就该绿）----
                Check(BoosterPackOpenWindow.CardFxFor(0), "Boosterpack Open Card Rarity 1",
                      "稀有度 0 ⇒ 粒子 `…Rarity 1`（`contentByRarities[0]` 实读）");
                Check(BoosterPackOpenWindow.CardFxFor(1), "Boosterpack Open Card Rarity 1",
                      "稀有度 1 ⇒ 同上（`contentByRarities[1]` 与 `[0]` **同一个 pid**）");
                Check(BoosterPackOpenWindow.CardFxFor(2), "Boosterpack Open Card Rarity 2",
                      "稀有度 2 ⇒ `…Rarity 2`（`contentByRarities[2]`）");
                Check(BoosterPackOpenWindow.CardFxFor(3), "Boosterpack Open Card Rarity 3",
                      "稀有度 3 ⇒ `…Rarity 3`（`contentByRarities[3]`）");
                Check(BoosterPackOpenWindow.CardFxFor(4), "Boosterpack Open Card Rarity 4",
                      "稀有度 4 ⇒ `…Rarity 4`（`contentByRarities[4]`）");
                // 角标判据的两条纯函数（**与拥有数口径无关的那部分**）
                CheckTrue(bp.Cards != null && bp.Cards.Length == 5, "这一包正好 5 张 `CardDef`");
                CheckTrue(BoosterPackOpenWindow.RarityInt(null) == 0, "`RarityInt(null)` = 0（不抛）");

                // ---- 翻牌（原版 `CardInBoosterPack.UiColliderOnClick` → `ChangeState(3)`）----
                int left0 = bp.CardsLeft;
                Check(left0, 5, "出场时 5 张都还没翻");
                CheckTrue(bp.SlotHits[0] != null, "第 1 格的命中区建了");
                if (bp.SlotHits[0] != null) bp.SlotHits[0].ClickForTest();
                CheckTrue(bp.Opened[0], "点第 1 格 ⇒ **翻了**（`Opened[0]`）");
                Check(bp.CardsLeft, 4, "…`cardsLeftToOpen` 自减（原版 `CardOpened`）");
                CheckTrue(!FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/Cardback Container").gameObject.activeSelf,
                          "…卡背关掉");
                CheckTrue(FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/2DCard").gameObject.activeSelf,
                          "…卡面开出来");
                CheckTrue(bp.SlotHits[0] == null || !bp.SlotHits[0].gameObject.activeSelf,
                          "…翻开的卡**不再吃点击**（原版 `ChangeState` 只在该卡 state==2 时可交互）");
                CheckTrue(!disc.gameObject.activeSelf, "…`Tap to discover` 收掉（原版 `CardOpened`）");
                // 播粒子（原版 `Instantiate(contentByRarities[rarity].particles, card.transform)`）
                CheckTrue(bp.PlayedFx.Count >= 1,
                          "翻牌**播了粒子**（" + bp.PlayedFx.Count + " 次 · `" + string.Join("`,`", bp.PlayedFx.ToArray())
                          + "`）—— ⚠️ 为 0 说明 `BoosterPackExporter.Run` + `EffectLibraryBuilder.Run` 还没跑");

                // 剩下 4 张全翻 ⇒ `Tap to close` 出现（原版 `cardsLeftToOpen` 归零那条）
                for (int i = 1; i < 5; i++)
                    if (bp.SlotHits[i] != null) bp.SlotHits[i].ClickForTest();
                Check(bp.CardsLeft, 0, "5 张全翻开");
                CheckTrue(clos.gameObject.activeSelf, "全翻开 ⇒ `Tap to close` 出现");

                // 点整屏 ⇒ 关（原版 `Tap to close/Collider` 的 `NonDrawingGraphic` 盖满整屏）
                if (bp.CloseSurfaceHit != null) bp.CloseSurfaceHit.ClickForTest();
                Check(bp.CurrentState, WindowState.Closed, "点整屏 ⇒ 关窗");

                // ---- 规则与缺口 ----
                bool anyRare = false;
                if (bp.Cards != null)
                    for (int i = 0; i < bp.Cards.Length; i++)
                        if (BoosterPackOpenWindow.RarityInt(bp.Cards[i]) >= 2) anyRare = true;
                CheckTrue(anyRare, "**至少一张 Rare or better**（原版出厂文案："
                          + "`At least one of the cards is guaranteed to be Rare or better`）");
                Check(bp.Cards != null ? bp.Cards.Length : 0, 5,
                      "**5 张**（原版出厂文案 `Contains 5 cards …` + prefab 里正好 5 个 `CardInBoosterPack`）");
                Check(bp.Army, "Leviathan", "第 4 件（`40K_shop_offer_booster_leviathan`）⇒ 阵营 Leviathan");
                CheckTrue(bp.MissingArt.Count == 0,
                          "这一扇用到的图**一张都不缺**（缺的会列在这里：" + string.Join("、", bp.MissingArt.ToArray()) + "）");
                Debug.Log(P + "   " + bp.Dump());
                // 收尾：这一扇已经关掉了，把商店开回来给实拍用
                ClosePackAndReopenShop(win);
            }
        }
        // ---------------- 🆕 2026-10-03（§三 第 29 条 A8）：商品条目族（19 个 `General Basic Offer Container *`）----------------
        //   判据 = `资料/阶段二_商店_原版规格.md` **§五·一**（19 件清单 / 11 种抽屉 / 三条硬限制 / 两个别照抄的坑）
        //        + 逐份实测 `python 工具/menu_dump.py bundle_menus_assets_all "<prefab 名>" --depth 3 --relative`（19 次）
        //   🔴 **期望值全部盯原版**：根尺寸 / `Dynamic Content` 矩形 / 抽屉节点名与兄弟序 / 出厂 INACT
        //      —— 一格都不是我们编的；那张表在 `Shell/OfferContainer.cs` 的 `Variants`（那边逐条写了出处）。
        //   ⚠️ 这一族**没有入口**（原版是服务端 LiveOps 报价的载体，本地一个报价数据都没有 —— 见 `OfferContainer.cs` 文件头）
        //      ⇒ 这里只断**骨架本身**，不断任何「点了会怎样」。
        Section("商品条目族（A8）：19 个变体的骨架 + 抽屉槽（期望值 = 原版逐份实读）");
        {
            Debug.Log(P + "   " + OfferContainer.Dump());
            Check(OfferContainer.Variants.Length, 19,
                  "变体表 **19 条**（18 个 `General Basic Offer Container *` + 1 个 `Small …`）");
            CheckTrue(!OfferContainer.Find("General Basic Offer Container", out _),
                      "**没有裸的母版文件名**（`GameObject/` 与真包里都没有 —— 正本 §五·一）");

            // 19 份全建出来，摆到**屏外**（不吃后面的实拍；断言量的是世界坐标，与在不在屏内无关）
            _offerScratch = new GameObject("OfferContainers_A8").transform;
            int slots = 0, offSlots = 0;
            for (int i = 0; i < OfferContainer.Variants.Length; i++)
            {
                var v = OfferContainer.Variants[i];
                float bx = -8000f - (i % 5) * 500f, by = -6000f - (i / 5) * 1000f;
                var c = OfferContainer.Content.Def();
                c.Item = ItemDrawer.Spec("WildcardUltramarines1", null, "Ultramarines");  // 喂**真物品**，抽屉才画得出来
                c.Price = "1 800";
                var b = OfferContainer.Build(_offerScratch, v, bx, by, c, 3000, null);

                CheckTrue(b.Root != null && b.Root.name == v.Prefab,
                          $"[{i + 1}] 根节点建出来了（名字 = 原 prefab 名 `{v.Prefab}`）");
                // ---- ① 根尺寸（原版实读）----
                CheckAt(b.Root, bx, bx + v.G.W, by, by + v.G.H, $"[{i + 1}] 根矩形 = **{v.G.W}×{v.G.H}**");
                CheckRectPx(FindChild(b.Root, OfferContainer.NRaycast), bx, bx + v.G.W, by, by + v.G.H,
                            $"[{i + 1}] `raycast target` **渲出来** = 整根那么大");

                // ---- ② `Dynamic Content` 矩形（原版实读 · 19 份恒 100×100）----
                var dyn = FindChild(b.Root, OfferContainer.NDynamic);
                CheckAt(dyn, bx + v.Dyn.x1, bx + v.Dyn.x2, by + v.Dyn.y1, by + v.Dyn.y2,
                        $"[{i + 1}] `Dynamic Content` = **({v.Dyn.x1},{v.Dyn.y1})→({v.Dyn.x2},{v.Dyn.y2})**");
                CheckTrue(dyn != null && Mathf.Abs(v.Dyn.W - 100f) < 0.01f && Mathf.Abs(v.Dyn.H - 100f) < 0.01f,
                          $"[{i + 1}] …它是个 **100×100 的锚框**（原版 19 份恒为 100×100 · 无脚本无 Graphic）");

                // ---- ③ 抽屉名配对：**逐个按名字 + 兄弟序**核（含 `" (1)"` 这类重名后缀）----
                var names = new System.Text.StringBuilder();
                if (dyn != null)
                    for (int k = 0; k < dyn.childCount; k++)
                    { if (k > 0) names.Append("|"); names.Append(dyn.GetChild(k).name); }
                Check(names.ToString(), string.Join("|", v.Drawers),
                      $"[{i + 1}] `Dynamic Content` 下那 {v.Drawers.Length} 个抽屉槽**逐个对上**（名字 + 兄弟序）");

                // ---- ④ 出厂 INACT 的槽**建成但关着**（原版 `m_IsActive`）----
                int off = 0, on = 0;
                if (dyn != null)
                    for (int k = 0; k < dyn.childCount; k++)
                        if (dyn.GetChild(k).gameObject.activeSelf) on++; else off++;
                slots += v.Drawers.Length;
                offSlots += v.Off != null ? v.Off.Length : 0;
                Check(off, v.Off != null ? v.Off.Length : 0,
                      $"[{i + 1}] 出厂 **INACT** 的槽 {off} 个（原版实读；其余 {on} 个开着）");

                // ---- ⑤ 「哪个槽被填」：**一律走已有的 `ItemDrawer.Draw`** ----
                int fi = OfferContainer.FirstActiveIndex(v);
                var filled = fi >= 0 && dyn != null ? dyn.GetChild(fi) : null;
                CheckTrue(filled != null && filled.Find("Item Drawer") != null,
                          $"[{i + 1}] 第 {fi + 1} 个槽 `{(fi >= 0 ? v.Drawers[fi] : "?")}` 里"
                          + "**真有一个 `ItemDrawer` 铺的抽屉**（`…/Item Drawer/Icon`）");
                Check(b.Filled != null ? b.Filled.name : null, fi >= 0 ? v.Drawers[fi] : null,
                      $"[{i + 1}] 填的正是 `FirstActiveIndex` 那一个"
                      + "（🔴 **我们挑的**：原版 `ShopOfferContainer.GeneralOfferPopupDrawer.DrawRewards` 是空 stub）");
                Check(b.Drawer, ItemDrawer.DrawerWildcard,
                      $"[{i + 1}] 喂野牌 ⇒ 抽屉 = `WildcardDrawer`（`ItemDrawer.PickDrawer`）");
                int extra = 0;
                if (dyn != null)
                    for (int k = 0; k < dyn.childCount; k++)
                        if (k != fi && dyn.GetChild(k).Find("Item Drawer") != null) extra++;
                Check(extra, 0, $"[{i + 1}] 其余 {v.Drawers.Length - 1} 个槽**都是空的**（一个都没被填）");
            }
            // 这两个总数**由 19 份 dump 逐份数出来**（3+6+6+2+3+4+3+3+3+3+5+7+4+3+5+12+3+6+7 = 88；
            // INACT 1+1+8+6 = 16）—— 钉在这里，改表时会被强制看见。
            Check(slots, 88, "19 个变体的**抽屉槽合计 88 个**（原版逐份数出来）");
            Check(offSlots, 16, "其中**出厂 INACT 合计 16 个**（原版逐份数出来）");

            // ---- ⑥ 分档：`WebShop` 那颗命中区**必须排在整卡命中区之上**（否则它点不动）----
            //   判据 = `CLAUDE.md` §三「分层要用渲染队列，不能用 z」+ `PointerLayer` 取队列最高的那条。
            //   （2026-10-03 在 `BoosterInfoPopup` 上刚栽过一次：压暗层与窗内四颗同档 ⇒ 那四颗点不动。）
            {
                var root0 = _offerScratch.GetChild(0);
                var cardsHit = FindChild(root0, OfferContainer.NRaycast);
                var wsHit = FindPath(root0, OfferContainer.NBackground + "/" + OfferContainer.NNameBg + "/"
                                          + OfferContainer.NWebShop + "/Hit");
                var qA = cardsHit != null ? cardsHit.GetComponentInChildren<ImageQuad>() : null;
                var qC = wsHit != null ? wsHit.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(qA != null && qC != null,
                          "整卡那句 `raycast target` 与 `WebShop` 那颗**两条命中区都建了**（各带 `ImageQuad`）");
                if (qA != null && qC != null)
                    CheckTrue(qC.RenderQueue > qA.RenderQueue,
                              "`WebShop` 命中队列 **>** 整卡 `raycast target` 的"
                              + $"（{qC.RenderQueue} > {qA.RenderQueue}）—— 不然那颗被整卡盖住、**点不动**");
            }
        }

        // ---------------- 🆕 2026-10-03（A8）：商店格**那条链**（原版 `CatalogItemContainer.OnInitialize`）----------------
        //   判据 = `d:/2/tools/decomp_full/CatalogItemContainer__OnInitialize.c`：
        //     `SupportMethods.DestroyAllChildren(drawerHolder);`
        //     `this.drawer = ItemDrawer.Draw(this.drawerHolder, item, 1, DrawerOverride.Shop(0x14), 0);`
        //   ⇒ 原版那一格的图是**运行时由抽屉铺的**，不是自己填的。
        //   🔴 **这条链是加法**：`HasArt` 真 ⇒ 走抽屉；假 ⇒ 老路（`ShopOffer.Art` + 占位板）一字不动。
        Section("商店格（A8）：**走抽屉 / 走兜底 两条路各走一次**");
        {
            win.tabButtons.Click(0);
            var pgA = win.PageOf(0);
            CheckTrue(pgA != null && pgA.DrawerCells == 4 && pgA.FallbackCells == 0,
                      "Cards 页 **4 件都走抽屉**（`ItemDrawer.HasArt(spec, Shop)` 为真）"
                      + $"（实测 抽屉 {pgA?.DrawerCells} / 兜底 {pgA?.FallbackCells}）");

            var cell0 = FindPath(FindChild(root, ShopData.Pages[0].Prefab),
                                 "Packs Scroll View/Viewport/Content/CatalogItemShopContainer_0");
            CheckTrue(cell0 != null, "第一格找得到");
            var art0 = FindChild(cell0, "Art");
            // ⚠️ 节点名仍是 `Art`（老路留下的名字）—— 既有四条断言按它找主图，改名 = **把断言改软**，见 `DrawerStyle` 注释
            CheckTrue(art0 != null && art0.Find(ItemDrawer.NodeIcon) != null,
                      "第一格的主图**是抽屉铺的**（`Art/Icon` ⇒ `ItemDrawer.Icon()` 建的）");
            CheckArt(FindPath(cell0, "Art/" + ItemDrawer.NodeIcon), ShopData.Offers(0)[0].Art,
                     "…画的还是商品表那张图（`spec.Art` = `ShopOffer.Art`）");

            // 🔴 **换路之后渲出来的矩形一字未变** —— 判据 = `DrawerStyle.IconFill = 1f` 的算式：
            //   老路 `MenuDraw.Rect(314.6×208, keepAspect)` ⇒ 内接成 `208×sprAspect`；
            //   抽屉 `Square(box, 1) = min(314.6,208) = 208²` + `keepAspect` ⇒ **同一个矩形**。
            float ax1, ay1, ax2, ay2;
            CheckTrue(RectOf(art0, out ax1, out ay1, out ax2, out ay2), "第一格主图的**渲染矩形**量得到");
            var tex0 = CardArt.MenuUi(ShopData.Offers(0)[0].Art);
            CheckTrue(tex0 != null, "商品表那张图在本地取得到（判据下面两条要用它的宽高比）");
            float wantArt = tex0 != null ? 208f * (float)tex0.width / tex0.height : 0f;
            CheckNear(ay2 - ay1, 208f, 1.5f, "主图**渲出来的高 = 208**（= `CellArtBox` 短边 × `IconFill 1`）");
            CheckNear(ax2 - ax1, wantArt, 1.5f,
                      $"主图**渲出来的宽 = 208 × 图宽高比（{tex0?.width}×{tex0?.height}）** —— 与老路 `keepAspect` 同值");
            CheckNear((ax1 + ax2) * 0.5f, 329.76f + 168.3f, 1f,
                      "主图中心 x = 格左 329.76 + `CellArtBox` 的中心 168.3（**位置也没动**）");
            CheckNear((ay1 + ay2) * 0.5f, 127.62f + 7f + 168f, 1f,
                      "主图中心 y = `Packs Scroll View` 顶 127.62 + 栅格 pad 7 + `CellArtBox` 中心 168（同上）");

            win.tabButtons.Click(1);
            var pgD = win.PageOf(1);
            CheckTrue(pgD != null && pgD.DrawerCells == 0 && pgD.FallbackCells == 3,
                      "Daily 页 **3 件全走兜底**（`Art = null` ⇒ `ItemDrawer.Spec` 判 `Unknown` ⇒ `HasArt` 假）"
                      + $"（实测 抽屉 {pgD?.DrawerCells} / 兜底 {pgD?.FallbackCells}）");
            var dcell0 = FindPath(FindChild(root, ShopData.Pages[1].Prefab),
                                  "Packs Scroll View/Viewport/Content/CatalogItemShopContainer_0");
            CheckTrue(FindChild(dcell0, "ArtPlaceholder") != null,
                      "…画的是**中性灰占位板 + 短名**（老路**一字未动**）");
            CheckTrue(FindChild(dcell0, "Art") == null, "…**没有** `Art` 节点（这一件本来就没图）");
            CheckTrue(pgD != null && pgD.NoArtOffers.Count == 3,
                      "…`NoArtOffers` 照旧记了 3 件（「不许静默失败」那条出声链没断）");
            win.tabButtons.Click(0);
        }

        ClosePackAndReopenShop(win);   // 上面若没走到（`bp == null`）也保证商店是开着的

        // ---------------- 实拍 ----------------
        CheckHoverSwap(win.transform, "商店窗");   // 🆕 A17：格内价签（`40K_button` → `_hover`）
        Section("实拍");        win.tabButtons.Click(0);
        Shoot("01_商店_Cards.png");
        win.tabButtons.Click(1);
        Shoot("02_商店_Daily.png");
        win.tabButtons.Click(2);
        Shoot("03_商店_Items.png");
        win.tabButtons.Click(0);
        Debug.Log(P + "   " + win.Dump());
        Debug.Log(P + "   " + ShopData.Dump());

        // ---------------- 🆕 2026-10-03（A8）实拍：商品条目族的骨架（抽两份并排）----------------
        //   为什么单拍一张：骨架里有**九宫格 `Badge`**、**半透明 `name-bg`**、**抽屉里那张野牌图** 三处
        //   是「断言绿了但画歪」的典型场合（本工程踩过好几次）⇒ 至少留一张**能看的**。
        //   抽的这两份：`…Variant Booster_avatar_cardback_title`（正本 §五·一 拿来当「代表骨架」的那一份，
        //   339×778）+ `Small …`（339×390，唯一一份 `background` 的 `Image` 是 enabled 的）。
        Section("商品条目族（A8）实拍");
        {
            win.Close();                                   // 商店是全屏窗，不关就什么都看不见
            if (_offerScratch != null) Object.DestroyImmediate(_offerScratch.gameObject);
            var shot = new GameObject("OfferContainer_Shot").transform;
            var vA = OfferContainer.Variants[5];           // `…Variant Booster_avatar_cardback_title`
            var vB = OfferContainer.Variants[18];          // `Small … Single Item Type`
            Check(vA.Prefab, "General Basic Offer Container Variant Booster_avatar_cardback_title",
                  "抽的是正本 §五·一 当「代表骨架」用的那一份");
            Check(vB.Prefab, "Small General Basic Offer Container Variant Single Item Type",
                  "另一份是唯一那个 `Small`（339×390）");
            for (int k = 0; k < 2; k++)
            {
                var v = k == 0 ? vA : vB;
                var c = OfferContainer.Content.Def();
                c.Item = ItemDrawer.Spec("WildcardUltramarines" + (k + 1), null, "Ultramarines");
                c.Price = k == 0 ? "1 800" : "2 000";
                var b = OfferContainer.Build(shot, v, k == 0 ? 420f : 1161f, 151f, c, 3000, null);
                CheckTrue(b.Filled != null && b.Drawer == ItemDrawer.DrawerWildcard,
                          $"并排第 {k + 1} 份的抽屉真填上了（`{b.Drawer}`）");
            }
            Shoot("05_商店_商品条目族骨架.png");
        }

        // ---------------- 收尾 ----------------
        ShopData.ResetForTest();
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        for (int i = 0; i < _failures.Count; i++) Debug.LogError(P + "失败 " + (i + 1) + "：" + _failures[i]);
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    static void CheckByPrefix(Transform root, string prefix, int want, string msg)
    {
        int n = CountByPrefix(root, prefix);
        CheckTrue(n == want, $"{msg}（实测 {n}）");
    }
}
