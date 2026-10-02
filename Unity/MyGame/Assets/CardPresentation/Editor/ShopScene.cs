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
