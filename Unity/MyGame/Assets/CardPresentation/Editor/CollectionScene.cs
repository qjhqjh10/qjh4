// CollectionScene.cs — 收藏线（`Collection Menu Variant`）的**场景 / 自检 / 截图**
//
// 用法：`Unity -batchmode -quit -executeMethod CollectionScene.Run`
// 判据全部来自 **原版参数**（`资料/阶段二_卡组线_原版规格.md` + `资料/普查产出_0923/A1~A4`），
// **不是我们自己的常量**（否则就是自证 —— 见 CLAUDE.md §二 那条）。
//
// ⚠️ 本文件的辅助函数（`Check*/FindChild/Shoot`）是**照 `ShopScene` / `RewardsScene` 又抄了一份**
//    —— 那三份各自有一整套，**这是一笔明账**（该收口成 `Editor/MenuCheck.cs`）。已记在
//    `项目任务.md` §三 第 15 条；**本轮先不动那三个绿着的文件**。
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RuleEngine;
using CardPresentation;     // ⚠️ 类本身**留在全局命名空间** —— 见下面那条注释

// 🔴 **`-executeMethod` 按类名找，带命名空间就找不到**（2026-09-23 踩：写成 `namespace CardPresentation`
//    之后跑出「executeMethod class 'CollectionScene' could not be found」，三次都是这一条）。
//    其余三个自检（`RewardsScene`/`ShopScene`/`DeckScene`）也都在**全局命名空间** ⇒ 照它们来。
public static class CollectionScene
{
        const string P = "[Collection] ";
        const string ShotDir = "d:/4/_tmp_view/collection";
        const string TestDeckFile = "d:/4/_tmp_view/collection/_test_decks.json";

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
        static void CheckText(string got, string want, string msg)
            => CheckTrue(got == want, $"{msg} —— 实测「{got}」，期望「{want}」");

        /// <summary>世界坐标比对（±0.01 世界单位 ≈ ±1px）。期望值必须来自原版像素矩形。</summary>
        static void CheckAt(Transform t, float x1, float x2, float y1, float y2, string what)
        {
            if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
            var want = LayoutSpace.RectCenter(x1, y1, x2, y2);
            float d = Vector3.Distance(t.position, want);
            CheckTrue(d <= 0.01f, $"{what} 在原版矩形中心（差 {d * 108f:F2}px）");
        }

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
            var nm = q != null && q.Texture != null ? q.Texture.name : null;
            CheckText(nm, want, what);
        }

        static Transform FindChild(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>一格卡的渲染队列（取卡内所有层里**最小的那个** —— 卡内层序靠 z 偏移、整格一起平移，
        /// 所以最小号就代表这一格；见 `CardFan.SetCardQueue`）。
        /// ⚠️ 读 `sharedMaterial`：`SetCardQueue` 走 `.material`（会把实例写回 `sharedMaterial`），
        /// 两边读到的是同一份，且**不会再实例化一次**。</summary>
        static int CardQueue(CardView v)
        {
            if (v == null) return -1;
            int q = int.MaxValue;
            foreach (var mr in v.GetComponentsInChildren<MeshRenderer>(true))
                if (mr.sharedMaterial != null) q = Mathf.Min(q, mr.sharedMaterial.renderQueue);
            return q == int.MaxValue ? -1 : q;
        }

        /// <summary>Deck 页那条 `Empty Collection Warning` 现在亮着没有。
        /// ⚠️ **必须限定在 Deck 页里找** —— 四个页各有一份**同名**节点（原版如此，矩形逐页不同）。</summary>
        static bool DeckEmptyShown(CollectionWindow win)
        {
            var ew = FindChild(win.PageRoot(0), "Empty Collection Warning");
            return ew != null && ew.gameObject.activeSelf;
        }

        static float PxOf(float worldX) { return worldX * 108f + 960f; }
        static float PxYOf(float worldY) { return 540f - worldY * 108f; }
        static float Wpx(Transform t)
        {
            var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
            return q != null ? q.WorldW * 108f : 0f;
        }
        static float Hpx(Transform t)
        {
            var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
            return q != null ? q.WorldH * 108f : 0f;
        }

        /// <summary>一个件**渲出来**的像素矩形（画布像素 · 左上原点 · y 向下）。
        /// 图走 `ImageQuad.WorldW/H`、字走 `Label.WorldW/H`（**都是真测量**，不是回读我们传进去的数）；
        /// 取的是**组件自己的 transform**（`AlignLeft/Right` 会把 `Label` 的节点挪走，拿外层容器算就会偏）。
        /// 🆕 2026-10-03：本文件原来只有 `Wpx/Hpx`（只给宽高）—— 要量「这块矩形落在哪」时不够用，
        /// 照 `RewardsScene.RectOf` / `ShopScene.RectOf` 的同名口子补一份
        /// （本文件开头那条注释已经明记：「四个自检各自一套辅助函数」是**一笔明账**，本轮不动那三个绿着的文件）。</summary>
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
        static string TextOf(Transform t)
        {
            var lb = t != null ? t.GetComponentInChildren<Label>() : null;
            return lb != null ? lb.Text : null;
        }

        // ============================================================ 🆕 2026-10-04：软边接线探针
        //
        // 判据 = `Shell/MenuDraw.cs` 的 `ApplySoftEdges`（原版 `RectMask2D.m_Softness` 的几何等效物）：
        //   非 0 时把一块**沿渐隐带的内沿切开**（原节点留含矩形中心的那一格、其余格建**子 quad**，
        //   命名 `…_soft<i><j>`）⇒ 父块与子块那条**共享边**就是带的内沿。
        // 🔴 **为什么这就是「接没接」的判据**：软边 = 0（硬边）时**一个子块都不会有** ⇒ 表空。
        //   于是「表空 = 没接」；而「切线该在哪」是**自己拿原版值 + 原版视口矩形算出来的**
        //   （⛔ 不是从被测实现里读常量）⇒ 改坏实现（删赋值 / 改数值 / 把两轴写反）都会真红。

        /// <summary>一个 `ImageQuad` **渲出来**的像素矩形。⚠️ 只用**这个组件自己**
        /// （不比 `RectOf`：那个会先找 `Label`、还会往子树里钻 —— 量切出来的每一块必须逐块量）。</summary>
        static bool QuadRectOf(ImageQuad q, out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = x2 = y2 = 0f;
            if (q == null) return false;
            float cx = PxOf(q.transform.position.x), cy = PxYOf(q.transform.position.y);
            float w = q.WorldW * 108f, h = q.WorldH * 108f;
            x1 = cx - w * 0.5f; x2 = cx + w * 0.5f;
            y1 = cy - h * 0.5f; y2 = cy + h * 0.5f;
            return true;
        }

        /// <summary>扫 `root` 子树，回传里面**所有软边切线**的位置（同 `RewardsScene.ScanSoftCuts`）。
        /// <param name="vertical">true = 只看**竖切线**（渐隐的是左右，= `m_Softness.x`）；false = 看横切线。</param></summary>
        static List<float> ScanSoftCuts(Transform root, bool vertical)
        {
            var cuts = new List<float>();
            if (root == null) return cuts;
            foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
            {
                if (q == null) continue;
                float hx1, hy1, hx2, hy2;
                if (!QuadRectOf(q, out hx1, out hy1, out hx2, out hy2)) continue;
                for (int i = 0; i < q.transform.childCount; i++)
                {
                    var c = q.transform.GetChild(i).GetComponent<ImageQuad>();
                    // ⚠️ **只认软边切出来的子块**（`ApplySoftEdges` 的命名 `baseName + "_soft" + i + j`）——
                    //    `ImageQuad.CreateNineSlice` 那 9 块是**兄弟**不是父子，但留一道名字闸更保险。
                    if (c == null || c.name.IndexOf("_soft") < 0) continue;
                    float cx1, cy1, cx2, cy2;
                    if (!QuadRectOf(c, out cx1, out cy1, out cx2, out cy2)) continue;
                    if (vertical)
                    {
                        if (Mathf.Abs(cx1 - hx2) < 0.5f) cuts.Add(cx1);         // 子块在**右** ⇒ 切线 = 子块左沿
                        else if (Mathf.Abs(cx2 - hx1) < 0.5f) cuts.Add(cx2);    // 子块在**左** ⇒ 切线 = 子块右沿
                    }
                    else
                    {
                        if (Mathf.Abs(cy1 - hy2) < 0.5f) cuts.Add(cy1);         // 子块在**下** ⇒ 切线 = 子块上沿
                        else if (Mathf.Abs(cy2 - hy1) < 0.5f) cuts.Add(cy2);    // 子块在**上** ⇒ 切线 = 子块下沿
                    }
                }
            }
            return cuts;
        }

        /// <summary>切线清单的**逐条**判据：每条都必须落在 `want` 里（±`tol`），且 `want` 每一项**都出现过**。
        /// `what` 里写清每一侧的算式（判据要能在失败信息里一眼看懂）。</summary>
        static void CheckSoftCuts(List<float> cuts, float[] want, float tol, string what)
        {
            var hit = new bool[want.Length];
            CheckTrue(cuts.Count > 0, what + "：**有层被软边切开**（切线实测 "
                + (cuts.Count > 0 ? string.Join("、", cuts.ConvertAll(v => v.ToString("F2")).ToArray()) : "一条都没有")
                + "）—— **空表 = 这条软边没接上**（`ClipSoftness` 留在 0）");
            for (int i = 0; i < cuts.Count; i++)
            {
                int k = -1;
                for (int j = 0; j < want.Length; j++)
                    if (Mathf.Abs(cuts[i] - want[j]) <= tol) { k = j; break; }
                CheckTrue(k >= 0, what + $"：切线 #{i + 1} 在 {cuts[i]:F2} ⇒ 必须是带的内沿"
                    + "（" + string.Join(" / ", System.Array.ConvertAll(want, v => v.ToString("F2"))) + "）");
                if (k >= 0) hit[k] = true;
            }
            for (int j = 0; j < want.Length; j++)
                CheckTrue(hit[j], what + $"：**{want[j]:F2} 这条切线确实出现**（少一条就说明那侧的软边没生效）");
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
            float lum = MeanBrightness(tex);
            if (allowBlank) Debug.Log(P + $"  截图 {file} 平均亮度 {lum:F1}（按已知情况放行）");
            else CheckTrue(lum > 3f, $"{file} **不是空图**（平均亮度 {lum:F1} > 3）");
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

        // ============================================================ 建场景

        static CollectionWindow Build(out Transform root)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.aspect = LayoutSpace.DesignAspect;
            LayoutSpace.Apply(cam);

            var anchors = new GameObject("Window Anchors").transform;
            MakeHolder(anchors, "1 - Below Upper Bar Holder", WindowsPlacement.World);
            MakeHolder(anchors, "2 - Canvas Holder Above upper bar", WindowsPlacement.Canvas);
            MakeHolder(anchors, "3 - PopUp Holder", WindowsPlacement.Popup);

            var wmGo = new GameObject("WindowsManager");
            var wm = wmGo.AddComponent<WindowsManager>();

            var win = CollectionWindow.Create(wm);
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

        // ============================================================ 自检

        public static void Run()
        {
            _pass = 0; _fail = 0; _failures.Clear();
            Directory.CreateDirectory(ShotDir);

            // 自检**不碰玩家的真存档**：`DeckStore.OverridePath` 指到临时文件，并**先造 14 套卡组**
            //（14 套 = 3 行 > 视口 2.4 行 ⇒ 列表**滚得动**，才验得到滚动那条）
            DeckStore.OverridePath = TestDeckFile;
            if (File.Exists(TestDeckFile)) File.Delete(TestDeckFile);
            CollectionData.ResetForTest();
            {
                var lib = DeckLibrary.Load();
                for (int i = 0; i < 14; i++) lib.Create("测试卡组 " + (i + 1));
                lib.Save();
            }
            CollectionData.ResetForTest();
            // 🔴 给第 1 套塞几张**真卡** —— `Deck info Popup` 的 `Deck List` 要有东西可画
            //   （`DeckLibrary.Create` 只建空壳，卡组里 0 张卡 ⇒ 第一版那条「画了 N 行」量到 0）
            {
                var lib = DeckLibrary.Load();
                if (lib.Decks.Count > 0)
                {
                    var pool = CardDatabase.Load();
                    var d0 = lib.Decks[0];
                    d0.CardIds.Clear();
                    foreach (var c in pool) if (c.Type == "hero") { d0.WarlordId = c.Id; break; }
                    int added = 0;
                    foreach (var c in pool) if (c.Type == "unit" && added < 3) { d0.CardIds.Add(c.Id); added++; }
                    lib.Save();
                }
            }
            CollectionData.ResetForTest();
            CollectionWindow.ResetStylesForTest();      // 异画那批卡的筛选状态（静态缓存）
            CardProgress.ResetForTest();                // 卡片详情窗的拥有数/等级（单机口径，静态缓存）
            Debug.Log(P + "=== 「收藏线」自检 开始 ===");

            var win = Build(out var root);

            // ---------------- 窗口参数（原版 MB `-6401214277658680619`）----------------
            Section("窗口参数（正本 §一；`Collection Menu Variant` 根 MB 原文）");
            Check(win.type, WindowType.Fullscreen, "`type` = **0 Fullscreen**（原文）");
            Check(win.placement, WindowsPlacement.Canvas,
                  "`windowsPlacement` = **5 Canvas**（⚠️ **商店是 10 World** —— 逐窗不同，别互推）");
            Check(win.closeOnEsc, true, "`closeOnESC` = **1**（⚠️ **奖励窗是 0**）");
            CheckNear(win.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = 1.0（原文）");

            // ---------------- 外壳（与奖励窗/商店**同一个壳**）----------------
            Section("外壳：`Content Area` / 左栏 / 四页签（正本 §二）");
            var area = FindChild(root, "Content Area");
            CheckAt(area, 167.17f, 1920.01f, 70.94f, 1080f, "`Content Area`");
            CheckRectPx(area, 167.17f, 1920.01f, 70.94f, 1080f, "`Content Area`");

            var bar = FindChild(root, "Tab Buttons");
            CheckAt(bar, 167.17f, 332.17f, 70.94f, 1080f, "`Tab Buttons`（左栏）");
            var keys = new List<Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("CollectionTabButton_")) keys.Add(t);
            Check(keys.Count, 4, $"左栏**四个键**（实测 {keys.Count}）");

            // 🔴 左栏第一个键的顶边 = **188.64**（条从 158.64 起 + VLG padTop 30）
            //    —— 这是本窗与奖励窗/商店**唯一的外壳差别**（那两个是 70.94 + 120 = 190.94）
            if (keys.Count >= 4)
            {
                var want = LayoutSpace.RectCenter(167.17f, 188.64f, 332.17f, 368.64f);
                float d = Vector3.Distance(keys[0].position, want);
                CheckTrue(d <= 0.01f,
                          $"**第一个键的矩形** = 167.17,188.64→332.17,368.64（原版；差 {d * 108f:F2}px）"
                          + " —— 本条是「`BarPadTop` 覆写成 117.7」的判据");
            }

            // ---------------- 四页签：**卡面文案不是页节点名** ----------------
            Section("四页签：文案 / 切页（正本 §二；A1 表 `:17-22`）");
            string[] wantLabels = { "DECKS", "CARDS", "COSMETICS", "STYLES" };
            for (int i = 0; i < 4 && i < keys.Count; i++)
            {
                // ⚠️ 键上的字是 `Text`；**原版 `m_fontStyle = UpperCase`**（基类已 `ToUpperInvariant`）
                var lab = FindChild(keys[i], "Text");
                CheckText(TextOf(lab), wantLabels[i],
                          $"左栏第 {i + 1} 键的文案 = **{wantLabels[i]}**"
                          + (i == 2 ? "（⚠️ 页节点叫 `Cardback Tab`，**卡面印的是 Cosmetics**）" : "")
                          + (i == 3 ? "（⚠️ 页节点叫 `Alternate Art Tab`，**卡面印的是 Styles**）" : ""));
            }
            var tabsRoot = FindChild(root, "Tabs");
            CheckTrue(FindChild(tabsRoot, "Select Deck Tab") != null, "页节点 `Select Deck Tab` 在（**不叫 `Deck Tab`**）");
            CheckTrue(FindChild(tabsRoot, "Cardback Tab") != null, "页节点 `Cardback Tab` 在");
            CheckTrue(FindChild(tabsRoot, "Alternate Art Tab") != null, "页节点 `Alternate Art Tab` 在");
            Check(win.CurrentTab, WindowTabType.CollectionDecks, "开窗默认落在第 1 页（Decks）");

            // ---------------- Deck 页：视口 / 格 / 6 列 ----------------
            Section("Deck 页：卡组列表的几何（正本 §三；A2 实测）");
            var listHolder = FindChild(tabsRoot, "Deck Scroll View");
            CheckAt(listHolder, 330.9f, 1920f, 155.9f, 1080f, "`Deck Scroll View`（视口）");
            int n = CollectionData.DeckCount();
            CheckTrue(n > 0, $"卡组列表里有 {n} 套卡组（自检用的临时存档）");

            var cells = win.DeckCells;
            CheckTrue(cells.Count > 0, $"画出了 {cells.Count} 格（视口外的不建 = `RectMask2D` 那套裁切）");
            if (cells.Count > 0)
            {
                var c0 = cells[0];
                CheckNear(PxOf(c0.position.x), 453.4f, 0.6f,
                          "第 1 格中心 x = **453.4**（视口左边 330.9 + padL 10 + 225/2）");
                CheckNear(PxYOf(c0.position.y), 338.15f, 0.6f,
                          "第 1 格中心 y = **338.15**（视口顶 155.9 + 364.5/2）");
                var frame = FindChild(c0, "Frame");
                CheckNear(Wpx(frame), 246f * 0.9f, 2f, "格内卡框宽 = **221.4**（原版 246 × 显示比例 0.9）");
                CheckNear(Hpx(frame), 368f * 0.9f, 2f, "格内卡框高 = **331.2**（原版 368 × 0.9）");
                CheckArt(frame, "40K_bt_deck", "卡框的图 = `40K_bt_deck`（原版）");
                CheckTrue(!string.IsNullOrEmpty(TextOf(FindChild(c0, "Deck Name"))), "格上有卡组名");
                // 6 列：第 6 格右边界 = 340.9 + 5×245 + 225 = 1790.9 ≤ 1920
                if (cells.Count >= 6)
                    CheckNear(PxOf(cells[5].position.x) + DeckCellWpx() / 2f, 1790.9f, 0.6f,
                              "第 **6** 格右边界 = **1790.9** ≤ 1920（**这是「6 列」的判据** —— 7 列要 2035.9）");
            }

            // ---- 左抽屉 `Deck Filters`（§三 第 15 条 **第 49 行**；2026-09-24 建）----
            // 出处：`menu_rect.py bundle_menus_assets_all "Deck Filters" --depth 6`（直读）。
            {
                var fh = FindChild(win.PageRoot(0), "FiltersHit");
                var fw = fh != null ? fh.GetComponent<WindowButton>() : null;
                CheckTrue(fw != null, "`Filters` 圆钮**有命中区**了（原来没有 ⇒ 玩家点它没反应）");
                var dflt = FindChild(win.PageRoot(0), "Deck Filters");
                CheckTrue(dflt != null, "左抽屉 `Deck Filters` 建了（原版这一页有抽屉）");
                if (dflt != null)
                {
                    // 🔴 **2026-10-04 订正（X3 审查的 R3）：这条位置断言原来是在「收着」的时候量的**，
                    //   A11 之后「收起」= 整栏**真的滑出去** —— 原版 `SetupFilters` 就是这么干的：
                    //   记下 `originalAnchorPosition` 之后立刻 `anchoredPosition = (hiddenPosition.x, …)`
                    //   （`资料/卡组编辑界面_查证_0920.md:433-434` 的 VA 反汇编）⇒ **关着时它已经不在原位**。
                    //   改法：**两态各量一条**（关着 = 真的滑出屏幕 · 开着 = 停在原版矩形），⛔ 不是把断言改软。
                    CheckNear(CollectionWindow.DeckFltView.W, 335.50f, 0.6f, "抽屉宽 **335.50**（原版）");
                    CheckTrue(!dflt.gameObject.activeSelf, "起手收起（**我们挑的**：与 Cards 页一致）");
                    CheckNear(win.DrawerSlide(3), 0f, 0.001f,
                              "🆕 A11：Deck 页这一列也走**同一套滑动**，收起 = 进度 **0**（不只在原位隐身）");
                    float dCx0 = PxOf(dflt.position.x);
                    CheckNear(dCx0, 167.81f - 385f, 1.5f,
                              "…收起 = 按**原版行程 −385px** 滑出去（`hiddenPosition.x − originalAnchorPosition.x`"
                              + " = −550 −(−165)；原位中心 167.81 → −217.19）—— 有洞的实现在这里就红");
                    CheckTrue(dCx0 + CollectionWindow.DeckFltView.W * 0.5f < 0f,
                              "★ …而且整栏**真的在屏幕左外**（右缘 "
                              + (dCx0 + CollectionWindow.DeckFltView.W * 0.5f).ToString("F1")
                              + " < 0）—— 这才叫「收起」");
                    // ★ **R4 的判据**：关着时那一列的命中区必须**已经是关的**。
                    //   ⚠️ 量的是 `WindowButton.enabled` 的**真值**、不是我们自己那个布尔（那个是自证）——
                    //   真命中路 `PointerLayer.CollectHits` 只挑 `isActiveAndEnabled`（`Shell/PointerLayer.cs:488`）。
                    //   原来 Deck 这一份**不走** `RebuildFilterRows`（格子自己 `AddHit` 建）⇒ 少了那次 `force: true`，
                    //   建出来的按钮停在默认 `enabled=true`，Play 里第一次滑出来的 0.3 秒真鼠标点得到
                    //   （被 R1 的 108 倍位移掩盖着，R1 修完才现形）。
                    var dwbs = dflt.GetComponentsInChildren<WindowButton>(true);
                    int dOn0 = 0; foreach (var b in dwbs) if (b != null && b.enabled) dOn0++;
                    CheckTrue(dwbs.Length > 0 && dOn0 == 0,
                              $"★ 起手收起 ⇒ 抽屉里 **{dwbs.Length}** 个命中区**全部 `enabled=false`**"
                              + $"（实测还开着 **{dOn0}** 个 —— 这一条是「第一次滑出来的 0.3 秒点得到」的判据）");
                    if (fw != null) fw.Click();
                    CheckTrue(dflt.gameObject.activeSelf, "点 `Filters` ⇒ 抽屉打开（原版这颗钮开的就是它）");
                    CheckNear(win.DrawerSlide(3), 1f, 0.001f, "🆕 A11：打开 = 进度 **1**（停在原位）");
                    // 「停在原版矩形」这一条原来在关着的时候量（见上面那段订正）⇒ 挪到**开着**的时候
                    CheckAt(dflt, 0.06f, 335.56f, 155.94f, 1080f,
                            "…打开后**停在原版矩形**中心（A11 起这条只能在开着时量）");
                    int dOn1 = 0; foreach (var b in dwbs) if (b != null && b.enabled) dOn1++;
                    CheckTrue(dwbs.Length > 0 && dOn1 == dwbs.Length,
                              $"★ …而到位后 **{dwbs.Length}** 个命中区**全回来**（实测 {dOn1} 个）—— "
                              + "与上面那条正好两态对比：写成恒开/恒关都过不了这两条");
                    var a0 = FindChild(dflt, "Cell_fac_" + CampaignData.Armies[0]);
                    CheckTrue(a0 != null,
                              $"`Army Filter` 里 **{CampaignData.Armies.Length}** 个阵营格建了"
                              + $"（第 1 个 = {CampaignData.Armies[0]}）");
                    // 正例：筛第 1 套卡组的阵营 ⇒ 只剩它那几套；反例：筛一个没有卡组的阵营 ⇒ 空 + 提示亮
                    string fac0 = CollectionData.DeckAt(0).Faction;
                    CheckTrue(!string.IsNullOrEmpty(fac0), "第 1 套卡组推得出阵营（筛选用）");
                    int nAll = win.DeckCellCount;
                    win.ToggleDeckFacFilter(fac0);
                    Check(win.DeckFacFilter, fac0, $"点阵营格 ⇒ 筛选条件 = {fac0}");
                    CheckTrue(win.DeckCellCount > 0 && win.DeckCellCount <= nAll,
                              $"筛「{fac0}」⇒ 卡组格 {nAll} → {win.DeckCellCount}（真筛得动）");
                    string other = "";
                    foreach (var a in CampaignData.Armies) if (a != fac0) { other = a; break; }
                    win.ToggleDeckFacFilter(fac0);          // 先取消
                    Check(win.DeckFacFilter, "", "再点同一格 ⇒ 取消（与 Cards 页 `$fac:` 同一条手感）");
                    win.ToggleDeckFacFilter(other);
                    Check(win.DeckCellCount, 0, $"筛一个**没有卡组**的阵营（{other}）⇒ 一格都不剩");
                    CheckTrue(DeckEmptyShown(win), "……而且 `Empty Collection Warning` **亮起来**（原版判据：过滤后为空）");
                    win.ClearDeckFilters();
                    Check(win.DeckFacFilter, "", "`Clear filters` ⇒ 筛选清掉");
                    Check(win.DeckCellCount, nAll, $"……卡组格回到 {nAll}");
                    CheckTrue(!DeckEmptyShown(win), "……而且那条提示关回去");
                    if (fw != null) fw.Click();
                    CheckTrue(!dflt.gameObject.activeSelf, "再点 `Filters` ⇒ 抽屉收起");
                    CheckNear(win.DrawerSlide(3), 0f, 0.001f, "…而且进度回到 **0**（A11：收 = 滑出去）");
                    int dOn2 = 0; foreach (var b in dwbs) if (b != null && b.enabled) dOn2++;
                    CheckTrue(dwbs.Length > 0 && dOn2 == 0,
                              $"…收起到位 ⇒ 命中区**又关回去**（实测还开着 {dOn2} 个）—— "
                              + "三态（收起 / 打开 / 再收起）走的是同一份实现");
                }
            }

            // `Empty Collection Warning`（**Deck 页那一份**）—— 原版**四个页各有一份、矩形各不相同**，
            // 逐页的实读值写在 `CollectionWindow.BuildDeckList` 那条注释里。
            {
                var ew = FindChild(win.PageRoot(0), "Empty Collection Warning");
                CheckTrue(ew != null, "Deck 页有 `Empty Collection Warning`（原版四页各一份）");
                if (ew != null)
                {
                    CheckAt(ew, 165.88f, 1970.01f, 70.94f, 1080f, "Deck 页 `Empty Collection Warning` 的位置");
                    CheckTrue(!ew.gameObject.activeSelf,
                              "有卡组 ⇒ 不显示（判据 = `CollectionData.DeckCount() <= 0`）");
                }
            }

            // ---------------- 交互：点一格 / 点 Create / 滚动 ----------------
            Section("交互（批处理直调，与真点同一条 `WindowButton.onClick`）");
            int before = CollectionData.CurrentIndex();
            int pick = (before + 1) % Mathf.Max(1, CollectionData.DeckCount());
            if (cells.Count > pick)
            {
                var hit = FindChild(cells[pick], "Hit");
                var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
                CheckTrue(wb != null && wb.onClick != null, "格子上的点击区挂到了 `WindowButton`");
                if (wb != null) wb.Click();
                Check(CollectionData.CurrentIndex(), pick, "点第 " + (pick + 1) + " 格 ⇒ 当前卡组换成它");
                // 🔴 **2026-09-23 换路**：原来这里是「**再点一下同一格** = 进编辑」—— 那是 `Deck info Popup`
                //    还没建时的**顶替路**（当时就出声了）。现在那扇窗建好了 ⇒ 改验**原版那条路**：
                //    点格 ⇒ 开窗 ⇒ 窗里点 `Edit Deck` ⇒ 才交接下标。
                var pop = CollectionWindow.LastOpened;
                CheckTrue(pop != null, "点一格卡组 ⇒ **开出了 `Deck info Popup`**（原版那条路）");
                Check(pop != null ? pop.DeckIndex : -1, pick, "弹窗收到的是**这一格**的下标");
                CheckTrue(pop == null || pop.CurrentState == WindowState.Open, "弹窗进了 `Open` 态");
                CollectionData.PendingEditDeck = -1;
                var eBtn = pop != null ? pop.Btn("Edit Deck") : null;
                var eWb = eBtn != null ? eBtn.GetComponent<WindowButton>() : null;
                CheckTrue(eWb != null, "窗里 `Edit Deck` 有点击区");
                if (eWb != null) eWb.Click();
                Check(CollectionData.PendingEditDeck, pick,
                      "窗里点 `Edit Deck` ⇒ 交接给 `DeckRuntime` 的下标 = " + pick
                      + "（「从收藏进编辑」的判据；真机上这一步会 `LoadScene(\"DeckEditor\")`）");
                if (pop != null) pop.Close();
            }
            var createHit = FindChild(tabsRoot, "CreateHit");
            var createBtn = createHit != null ? createHit.GetComponent<WindowButton>() : null;
            CheckTrue(createBtn != null, "`Create` 钮有点击区");
            if (createBtn != null)
            {
                int n0 = CollectionData.DeckCount();
                createBtn.Click();
                Check(CollectionData.DeckCount(), n0 + 1, "点 `Create` ⇒ 卡组数 +1（原版走 `Deck Editing Menu`，本轮只建卡组）");
            }
            // 🔴 **实拍抓的一条**：`Clear filters` 原来摆在 1218.6（**我们推错的**）⇒ 它和 `Import` 压掉 77.6px。
            //    真值 = A2 §161 的**实测落点 612.2**（容器宽 0 + 子件 `m_IgnoreLayout=1`）。
            {
                var cf = FindChild(tabsRoot, "Clear filters Text");
                var il = FindChild(tabsRoot, "Import Text");
                var lcf = cf != null ? cf.GetComponent<Label>() : null;
                var lil = il != null ? il.GetComponent<Label>() : null;
                if (lcf != null && lil != null)
                {
                    float cfR = PxOf(lcf.transform.position.x) + lcf.WorldW * 54f;
                    float iL = PxOf(lil.transform.position.x) - lil.WorldW * 54f;
                    CheckTrue(cfR < iL,
                              $"`Clear filters`（右缘 **{cfR:F0}**）与 `Import Deck`（左缘 **{iL:F0}**）**不叠**"
                              + " —— 判据是 A2 §161 的实测落点 612.2，**别自己按容器推**");
                }
                CheckText(TextOf(FindChild(tabsRoot, "Import Text")), "Import Deck", "`Import` 钮文案（原版 `Import Deck`）");
                CheckText(TextOf(FindChild(tabsRoot, "Create Text")), "Create Deck", "`Create` 钮文案（原版 `Create Deck`）");
            }
            var impHit = FindChild(tabsRoot, "ImportHit");
            CheckTrue(impHit != null && impHit.GetComponent<WindowButton>() != null,
                      "`Import` 钮有点击区（点了**如实说没实现**，不静默）");
            if (win.DeckScroll != null)
            {
                float max = win.DeckScroll.MaxOffset;
                CheckTrue(max >= 0f, $"卡组列表的滚动极值 = {max:F0}px（`MenuScroll` 纵向；格数少时可以为 0）");
                if (max > 0f)
                {
                    win.DeckScroll.ScrollBy(max);
                    var last = CollectionData.DeckCount() - 1;
                    var lastCell = FindChild(tabsRoot, "CollectionDeck_" + last);
                    CheckTrue(lastCell != null
                              && PxYOf(lastCell.position.y) + DeckCellHpx() / 2f <= 1080.5f,
                              "**滚到底 ⇒ 最后一格完整落进视口**（这是「够不着」那个缺口的判据）");
                    win.DeckScroll.ScrollBy(-max);
                }
            }

            // ---- 🆕 2026-10-03（A12 收口）：**滚动视口的裁切也管点击区** —— 真界面回归 ----
            //   判据 = 原版 `RectMask2D` 的**射线那一面**（`IsRaycastLocationValid` 就是一句
            //   `RectangleContainsScreenPoint` ⇒ 框外的点判不中任何东西）。判据正本 = 本工程唯一一份
            //   `MenuDraw.ClipRect`（它的注释里有 UGUI 源码出处）；这一段量的是**真界面**、不是临时节点。
            //   ⚠️ 这一段量的是**节点的位置**（= 交集块的中心），不是宽高。
            //      🔴 **2026-10-04 订正（A26）**：原来这里写着「`MenuDraw.DeckCell` 的 `Hit` 是**裸节点**（不带 quad）
            //      ⇒ 它进不了 `PointerLayer` 的命中表」—— **那条已经不成立**：裸节点已经补上了透明 quad
            //      （见 `MenuDraw.DeckCell` 的注释与新加的「真命中路」那一段断言）。
            {
                var ds = win.DeckScroll;
                if (ds != null)
                {
                    float savedD = ds.Offset;
                    ds.SetOffset(0f);                       // 起手态：第 3 行压在视口下边上
                    Transform edge = null; float edgeCy = 0f;
                    for (int i = 0; i < win.DeckCells.Count; i++)
                    {
                        var c = win.DeckCells[i];
                        if (c == null) continue;
                        float cy = PxYOf(c.position.y);
                        if (cy + DeckCellHpx() * 0.5f > CollectionWindow.DeckViewport.y2 + 0.5f)
                        { edge = c; edgeCy = cy; break; }
                    }
                    CheckTrue(edge != null, "起手有一格**压在视口下边上**（否则这一条等于没验）");
                    if (edge != null)
                    {
                        float ecx = PxOf(edge.position.x);
                        var full = new PxRect(ecx - DeckCellWpx() * 0.5f, edgeCy - DeckCellHpx() * 0.5f,
                                              ecx + DeckCellWpx() * 0.5f, edgeCy + DeckCellHpx() * 0.5f);
                        PxRect vis;
                        bool inView = MenuDraw.ClipRect(full, CollectionWindow.DeckViewport, out vis);
                        CheckTrue(inView && !MenuDraw.SameRect(vis, full),
                                  "现算：这一格与视口的**交集比整格小**（= 真被裁了，下面才有可验的）");
                        var eHit = FindChild(edge, "Hit");
                        CheckTrue(eHit != null, "压在边上的那一格**建了** `Hit`（部分越界 ⇒ 建、但位置被截）");
                        if (eHit != null)
                        {
                            CheckNear(PxOf(eHit.position.x), vis.CX, 0.5f,
                                      "命中区中心 x = **露出来那块的中心**（横向没裁 ⇒ 与格中心同）");
                            CheckNear(PxYOf(eHit.position.y), vis.CY, 0.5f,
                                      "命中区中心 y = **露出来那块的中心**（**不是**整格中心）");
                            CheckNear(Mathf.Abs(PxYOf(eHit.position.y) - edgeCy), (full.H - vis.H) * 0.5f, 0.5f,
                                      "…与整格中心的差 = 被裁掉那半截的一半（"
                                      + $"{Mathf.Abs(PxYOf(eHit.position.y) - edgeCy):F1} ≈ {(full.H - vis.H) * 0.5f:F1}）");
                        }
                    }
                    ds.SetOffset(savedD);
                }
            }

            // ---- 🆕 2026-10-04（A26）：**卡组格真鼠标点得到**（走真命中路，**不是**直调 `wb.Click()`）----
            //   判据 = `PointerLayer.CollectHits` 取的是「按钮下**第一个 `ImageQuad`**」
            //   （`Shell/PointerLayer.cs:492`）。`MenuDraw.DeckCell` 的 `Hit` 当年是 `Node()` 建的**裸节点**
            //   ⇒ 命中表里**恒没有它** ⇒ 真鼠标点不动；而 `WindowButton.onClick` 在着、自检直调 `wb.Click()`
            //   也过 ⇒ **只有这条路能验出来**（这正是 `真Play待验清单` A 鼠标那条的意义）。
            //   ⚠️ 这一段是**会红的**：把 `DeckCell` 改回裸节点它立刻红（不是自证 —— 期望值取自
            //      `PointerLayer` 那条与实现无关的路）。
            {
                var pl = PointerLayer.Instance;
                CheckTrue(pl != null, "`PointerLayer` 在（真鼠标 → 界面**唯一**那条路）");
                if (pl != null && win.DeckScroll != null)
                {
                    float savedD = win.DeckScroll.Offset;
                    win.DeckScroll.SetOffset(0f);               // 起手态：行 0 完整在视口里
                    var dc = win.DeckCells;                     // ⚠️ `SetOffset` 会重建 ⇒ 重建之后再取表
                    CheckTrue(dc.Count > 0, "有卡组格可点（否则这一段等于没验）");
                    if (dc.Count > 0)
                    {
                        var c0 = dc[0];
                        var h0 = FindChild(c0, "Hit");
                        CheckTrue(h0 != null, "第 1 格有 `Hit` 节点");
                        var hq = h0 != null ? h0.GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(hq != null,
                                  "★ 命中区节点下**真的挂着 `ImageQuad`** —— `PointerLayer` 只认它"
                                  + "（裸节点 = 进不了命中表 = 真鼠标点不动）");
                        if (hq != null)
                            Check(hq.RenderQueue, CollectionWindow.QPageRow,
                                  "命中 quad 的渲染队列 = `QPageRow`（与 `DeckCell` 的入参同档）");
                        var b0 = pl.ButtonAt(PxOf(c0.position.x), PxYOf(c0.position.y));
                        CheckTrue(b0 != null && b0.transform == h0,
                                  "★ **真命中路**（`PointerLayer.ButtonAt`）在格中心拿到的是**这一格**"
                                  + "（拿到 " + (b0 == null ? "**null** = 真鼠标点不动" : "`" + b0.name + "`") + "）");
                    }

                    // 压在视口下边上的那一格：**露出来那块的中心**也点得到，且命中 quad 的高**跟着裁切走**
                    Transform edge = null; float edgeCy = 0f;
                    for (int i = 0; i < win.DeckCells.Count; i++)
                    {
                        var c = win.DeckCells[i];
                        if (c == null) continue;
                        float cy = PxYOf(c.position.y);
                        if (cy + DeckCellHpx() * 0.5f > CollectionWindow.DeckViewport.y2 + 0.5f)
                        { edge = c; edgeCy = cy; break; }
                    }
                    CheckTrue(edge != null, "起手有一格**压在视口下边上**（否则下面那条等于没验）");
                    if (edge != null)
                    {
                        float ecx = PxOf(edge.position.x);
                        var full = new PxRect(ecx - DeckCellWpx() * 0.5f, edgeCy - DeckCellHpx() * 0.5f,
                                              ecx + DeckCellWpx() * 0.5f, edgeCy + DeckCellHpx() * 0.5f);
                        PxRect vis;
                        if (MenuDraw.ClipRect(full, CollectionWindow.DeckViewport, out vis)
                            && !MenuDraw.SameRect(vis, full))
                        {
                            var eh = FindChild(edge, "Hit");
                            var ehq = eh != null ? eh.GetComponentInChildren<ImageQuad>() : null;
                            CheckNear(ehq != null ? ehq.WorldH * 108f : -1f, vis.H, 1.5f,
                                      "★ 压在边上的那一格：命中 quad 的高 = **露出来那块**的高"
                                      + "（`Hit` 跟着 `RectMask2D` 的射线那一面裁）");
                            var be = pl.ButtonAt(vis.CX, vis.CY);
                            CheckTrue(be != null && be.transform == eh,
                                      "★ …而**露出来那块的中心**（不是整格中心）点得到它"
                                      + "（拿到 " + (be == null ? "**null**" : "`" + be.name + "`") + "）");
                        }
                    }
                    win.DeckScroll.SetOffset(savedD);
                }
            }

            // ---------------- `Deck info Popup`（A1 §2 逐节点表）----------------
            Section("`Deck info Popup`：版面（A1 §2）");
            {
                var pop = win.OpenDeckInfo(0);
                CheckTrue(pop != null, "开得出来");
                if (pop != null)
                {
                    var pr = pop.transform;
                    CheckHoverSwap(pop.transform, "Deck info Popup");   // 🆕 A17：三颗钮 + 关闭钮的圆底
                    Check(pop.type, WindowType.Popup, "`type` = **1 Popup**（原文）");
                    Check(pop.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
                    Check(pop.closeOnEsc, true, "`closeOnESC` = **1**（⚠️ 提示窗是 0）");
                    CheckAt(FindChild(pr, "Generic Window Red Background Big"),
                            134.50f, 1839.50f, 82f, 1032f, "红底 `UI_Deck_Information_Back`");
                    CheckAt(FindChild(pr, "Info Panel"), 659f, 1799f, 218.10f, 868.10f, "`Info Panel`");
                    var dn = CollectionData.DeckAt(0);
                    CheckText(TextOf(FindChild(pr, "Deck Name")), dn.Name, "`Deck Name` = 被点的那套卡组的名字");
                    var wlc = CollectionData.Warlord(0);
                    CheckText(TextOf(FindChild(pr, "Warlord Name")), wlc != null ? wlc.Name : "未选督军",
                              "`Warlord Name` = 该卡组的督军名");
                    CheckTrue(pop.Btn("Practice Deck") != null && pop.Btn("Edit Deck") != null
                              && pop.Btn("Select Deck") != null, "`Buttons` 三个钮**都建了**（在不在 ≠ 露不露，见下）");
                    // 🆕 2026-10-04（A31）：**这三颗按 state 显隐** —— 收藏窗这条 = 原版 `DeckCollectionTab.OnItemSelected`
                    //   = **state 0**（判据 → `Shell/DeckInfoPopup.cs` 文件头那张表）。state 0 下：
                    //   `Edit Deck` 露（`state < 2`）· `Practice Deck` 露（`isPlayerDeck && state ∈ {0,2}`）·
                    //   `Select Deck` **不露** —— 原版判据是 `context.SelectButton != null`，而**6 个调用点全传 null**
                    //   （逐处实读 `DeckInfoContext__ctor` 的第 3 个实参）⇒ 本 build 里这颗钮从不出现。
                    CheckTrue(pop.IsItemShown("Btn:Edit Deck"), "state 0 ⇒ `Edit Deck` **露着**（原版 `state < 2`）");
                    CheckTrue(pop.IsItemShown("Btn:Practice Deck"), "state 0 ⇒ `Practice Deck` **露着**（`isPlayerDeck && state∈{0,2}`）");
                    CheckTrue(!pop.IsItemShown("Btn:Select Deck"),
                              "state 0 ⇒ `Select Deck` **不露**（原版 `context.SelectButton != null`，而 6 个调用点全传 null）");
                    var pb = pop.Btn("Practice Deck");
                    CheckNear(pb != null ? PxOf(pb.position.x) : -1f, 887.45f, 1f,
                              "`Practice Deck` 中心 x = **887.45**（三个钮右缘到 1770.70、spacing 36）");
                    var sb = pop.Btn("Select Deck");
                    // ⚠️ 量的是**关着的**那颗（`Transform.Find` 找得到关着的节点）—— 位置不因显隐而变
                    CheckNear(sb != null ? PxOf(sb.position.x) : -1f, 1608.45f, 1f, "`Select Deck` 中心 x = **1608.45**");
                    CheckTrue(pop.Opt("Delete") != null && pop.Opt("Switch Deck Info") != null
                              && pop.Opt("Share") != null && pop.Opt("Share On Chat") != null
                              && pop.Opt("Duplicate") != null, "`Deck Options` 五个圆钮都在");
                    // 🆕 2026-10-04（A31）：这一扇是 **state 0**（原版 `DeckCollectionTab.OnItemSelected`）
                    //   ⇒ 四颗 `state == 0 && isPlayerDeck` 的**都该露**（两态对比的另一半 → 下面那节 A31）
                    CheckTrue(pop.IsItemShown("Opt:Share") && pop.IsItemShown("Opt:Share On Chat")
                              && pop.IsItemShown("Opt:Delete") && pop.IsItemShown("Opt:Duplicate"),
                              "state 0 ⇒ `Share`/`Share On Chat`/`Delete`/`Duplicate` **四颗都露着**");
                    var od = pop.Opt("Delete");
                    var os2 = pop.Opt("Switch Deck Info");
                    // 🔴 **2026-10-03 就地订正（A10）**：顺序原来反了。原版左→右 = **树序**：
                    //    `Switch Deck Info`(1611.7) → `Duplicate` → `Share` → `Share On Chat` → `Delete`(1709.2)
                    //    （实据 = fresh dump 的跑后 x；原来那句「`reverse=1` 把 GO 顺序倒过来」**实测不成立**）
                    CheckNear(os2 != null ? PxOf(os2.position.x) : -1f, 1648.89f, 1f,
                              "**最左**那个是 `Switch Deck Info`（原版左→右 = 树序，**不是**反序）");
                    CheckNear(od != null ? PxOf(od.position.x) : -1f, 1746.43f, 1f, "**最右**那个是 `Delete`");
                    Check(DeckInfoPopup.ListCols, 3,
                          "卡列表列数 = **3** = floor((1140 − 15 − 15 + 11) ÷ 371)（照 GridLayoutGroup 那套算）");
                    CheckTrue(pop.Rows.Count > 0, $"卡组内容画了 {pop.Rows.Count} 行");
                    var r0 = pop.Rows.Count > 0 ? pop.Rows[0] : null;
                    CheckNear(r0 != null ? PxOf(r0.position.x) : -1f, 854f, 1f,
                              "第 1 行中心 x = **854**（Info Panel 左 659 + pad 15 + 180）");
                    CheckNear(r0 != null ? PxYOf(r0.position.y) : -1f, 247.10f, 1f,
                              "第 1 行中心 y = **247.10**（218.10 + 58/2）");

                    // ---------------- 🆕 2026-10-03（§三第29条 A10）补的四件 + 两处订正 ----------------
                    //   逐值 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 8`
                    //   🔴 两个**新抓到的真缺陷**：`Deck Details` 整块原来用的是**布局组跑之前的模板位**、
                    //      `Deck Options` 顺序反了（上面那条已改）。这里把订正后的值钉住。
                    Section("`Deck info Popup` A10：游戏模式图标 / `Deck Info` 抽屉 / 两个抽屉切换 / 两处订正");
                    {
                        // ① `Deck Details` 跑后真值（原来那套是模板位 ⇒ Army Icon 画到容器外）
                        CheckAt(FindChild(pr, "Army Icon"), 767.9f, 867.9f, 106.7f, 216.7f,
                                "`Army Icon`（**跑后**真值 —— 原来那个 363.10 落在 `Deck Details` 容器之外）");
                        CheckAt(FindChild(pr, "Game Mode Separator"), 759.0f, 767.9f, 106.7f, 216.7f,
                                "`Game Mode Separator`（跑后真值）");
                        // ⚠️ `Deck Name` / `Warlord Name` 是 **`Align.Left`** ⇒ `AlignLeftOn` **会把 Label 的节点挪走**
                        //    ⇒ 拿节点中心去比 `CheckAt` 会差 100+px（第一版就是这么红的）。
                        //    **量它渲出来的左缘**（同 `已知的 MainMenuScene` 那两句的写法）。
                        {
                            var dnT = FindChild(pr, "Deck Name");
                            var dnl = dnT != null ? dnT.GetComponentInChildren<Label>() : null;
                            if (dnl != null)
                                CheckNear(PxOf(dnl.transform.position.x) - dnl.WorldW * 54f, 872.9f, 2f,
                                          "`Deck Name` 左缘 = **872.9**（跑后真值 · 原版 hAlign=Left）");
                            var wnT = FindChild(pr, "Warlord Name");
                            var wnl = wnT != null ? wnT.GetComponentInChildren<Label>() : null;
                            if (wnl != null)
                                CheckNear(PxOf(wnl.transform.position.x) - wnl.WorldW * 54f, 872.9f, 2f,
                                          "`Warlord Name` 左缘 = **872.9**（同上）");
                        }
                        // ② `Game Mode Icon`（A10 补的第一件）
                        var gm = FindChild(pr, "Game Mode Icon");
                        var gmq = gm != null ? gm.GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(gmq != null && gmq.Texture != null
                                  && gmq.Texture.name == "40k_gamemode_icon_classic",
                                  "`Game Mode Icon` = `40k_gamemode_icon_classic`（这副是经典模式；"
                                  + "原来记的「本地没图」是**过期**的 —— 两张图本来就在工程里）");
                        CheckAt(gm, 659.0f, 759.0f, 106.7f, 216.7f, "`Game Mode Icon` 的矩形（跑后真值）");
                        // ③ 两个抽屉：出厂只有 `Deck List` 开
                        var dlist = FindChild(pr, "Deck List");
                        var dinfo = FindChild(pr, "Deck Info");
                        CheckTrue(dlist != null && dlist.gameObject.activeSelf, "`Deck List` 出厂开着");
                        CheckTrue(dinfo != null && !dinfo.gameObject.activeSelf,
                                  "`Deck Info` **建成但关着**（原版出厂 `INACT`）");
                        CheckTrue(!pop.InfoDrawerShown, "自检读数：现在展示的是 `Deck List`");
                        // ④ `Switch Deck Info` ⇒ 真切（两个互斥）
                        var swHit = pop.Opt("Switch Deck Info");
                        var swWb = swHit != null ? swHit.GetComponent<WindowButton>() : null;
                        CheckTrue(swWb != null, "`Switch Deck Info` 有点击区");
                        if (swWb != null)
                        {
                            swWb.ClickForTest();
                            CheckTrue(pop.InfoDrawerShown, "点它 ⇒ 切到 `Deck Info`");
                            CheckTrue(!FindChild(pr, "Deck List").gameObject.activeSelf
                                      && FindChild(pr, "Deck Info").gameObject.activeSelf,
                                      "两个抽屉**互斥**（`Deck List` 关了、`Deck Info` 开了）");
                            // ⑤ `Deck Info` 里的三件：标题 / 费用曲线 9 行 / 卡背
                            var di = FindChild(pr, "Deck Info");
                            CheckText(TextOf(FindChild(di, "Deck Information Cost/balance text")), "Cards / Cost",
                                      "费用那条标题（⚠️ 原版 prefab 里是**葡语占位** `Cartas / Coste` ⇒ 这行文案是我们挑的）");
                            Check(pop.CostRowCounts.Count, 9, "费用曲线 **9 行**（费用 0..8 —— 原版序列化就是 9 行）");
                            int sum = 0; for (int k = 0; k < pop.CostRowCounts.Count; k++) sum += pop.CostRowCounts[k];
                            CheckTrue(sum > 0, $"…曲线里**真的有张数**（合计 {sum} 张；这副的卡都算进去了）");
                            var cbk = FindChild(di, "Cardback");
                            var cbq = cbk != null ? cbk.GetComponentInChildren<ImageQuad>() : null;
                            // ⚠️ 卡背图取不到是**已知缺口**（`Art/cards/back_*.png` 只有 4 个阵营 ⇒
                            //    多数阵营**没有「默认卡背」**，正本 §七 ③ 记着）⇒ 这条只断
                            //    「**有图就必须画出来**」；取不到时 `DeckInfoPopup` 会**出声**（日志里那条
                            //    「卡背取不到（`` / 阵营 X）⇒ 那一层不画」），**不是静默**。
                            var cbInfo = CollectionData.DeckAt(0);
                            var cbTex = CardArt.DeckCardback(cbInfo.CardbackId, cbInfo.Faction);
                            CheckTrue(cbTex == null || (cbq != null && cbq.Texture != null),
                                      "`Cardback`：**有图就必须画出来**（实测图 "
                                      + (cbTex != null ? "有" : "**没有**（已知缺口，已出声）") + "）");
                            CheckTrue(FindChild(di, "Lore Text") == null,
                                      "`Lore Text` **不建**（原版出厂 `act=N` + 我们引擎没有 lore 字段）");
                            swWb.ClickForTest();
                            CheckTrue(!pop.InfoDrawerShown, "再点 ⇒ 切回 `Deck List`");
                        }
                        // ⑥ 督军立绘可点（原版 `EverguildButton` ⇒ 开卡详情窗）
                        var wh = FindChild(pr, "WarlordHit");
                        CheckTrue(wh != null && wh.GetComponent<WindowButton>() != null,
                                  "`Warlord Image` 上有点击区（原版那层就是 `EverguildButton`）");
                        if (wh != null)
                        {
                            wh.GetComponent<WindowButton>().ClickForTest();
                            // ⚠️ 用 `DeckInfoPopup.LastWarlordDetail`，**不是** `CardDetailPopup.LastOpened`
                            //    （后者只在 `RebuildKeepingState()` 里赋值 ⇒ 这条路拿到的恒是 null）
                            var wd = DeckInfoPopup.LastWarlordDetail;
                            CheckTrue(wd != null && wlc != null && wd.Card != null && wd.Card.Name == wlc.Name,
                                      "点它 ⇒ 开督军的**卡片详情窗**（「" + (wlc != null ? wlc.Name : "?") + "」）");
                            if (wd != null) wd.Close();
                        }
                        // ⑦ `Share` / `Share On Chat`：给卡组串（原版走平台/服务端）
                        var shHit = pop.Opt("Share");
                        CheckTrue(shHit != null && shHit.GetComponent<WindowButton>() != null, "`Share` 有点击区");
                        Debug.Log(P + "   卡组串自检：Share 的实现在 `DeckInfoPopup.ShareDeck`");
                    }
                    Shoot("05_收藏_DeckInfo弹窗.png");
                    var popCloseHit = FindChild(pr, "CloseHit");
                    var popCloseWb = popCloseHit != null ? popCloseHit.GetComponent<WindowButton>() : null;
                    CheckTrue(popCloseWb != null, "关闭圆钮有点击区");
                    if (popCloseWb != null) popCloseWb.Click();
                    Check(pop.CurrentState, WindowState.Closed, "点关闭钮 ⇒ 窗进 `Closed` 态");
                }
            }

            // ---------------- 🆕 2026-10-04（§三第29条 A31）：8 颗钮**按 state 显隐** ----------------
            //   判据（唯一出处）= `DeckInfoControls__Initialize.c` 的 SetActive :60-112 + 「offset → 节点名」
            //   对照表（**在 `Shell/DeckInfoPopup.cs` 文件头**）。逐颗：
            //     `Edit Deck` ← `state < 2` · `Select Deck` ← `context.SelectButton != null`
            //     · `Share`/`Share On Chat`/`Delete`/`Duplicate` ← `state == 0 && isPlayerDeck`（**四颗同一条**）
            //     · `Practice Deck` ← `isPlayerDeck && state ∈ {0,2}` · `Switch Deck Info` ← **常显**
            //   `isPlayerDeck` = 原版 **`InventoryManager.HasItem(context.Deck)`**（`DeckInfoPopup__Open.c:43-48`）
            //     —— 我们三处调用点拿的都是玩家自己的卡组 ⇒ 恒 true。
            //   🔴 **真红法**（2026-10-04 逐条推演订正过 —— X3 审查的 **R6**：原来说「任何一行写成恒真/恒假
            //     都必有一条红」**不成立**）：下面 ①～③ 那三条**只覆盖 state 0 与 state 2**，而
            //       · `Practice Deck` 这一行**两态都该露** ⇒ 写成恒 `true` 全绿；
            //       · `isPlayerDeck`（那个 `mine`）默认 true、**没有任何断言把它置 false** ⇒ 丢掉这个合取项全绿。
            //     ⇒ ④ / ⑤ 两条就是给它们补「该藏」的那一态（判据里有、我们的调用点没有，
            //       但字段是 public、自检造得出）。**改坏法**：把 `mine` 换成恒 `true` ⇒ ④ 红；
            //       把 `Practice Deck` 那行的 `state ∈ {0,2}` 换成恒 `true` ⇒ ⑤ 红；
            //       把 `state < 2` 换成 `state < 3` / `<= 1` ⇒ ⑤ 那条「`Edit Deck` 仍露」红。
            Section("`Deck info Popup`：8 颗钮**按 state 显隐**（A31）");
            {
                var mi0 = win.Manager;
                // ① state 2（View）= 原版 `DeckGeneralInfoDemo.CardInDeckInfoButtonOnClick` 那一态
                //    （= 我们的 `PracticeModePopup.ShowDeckContent` **该给**的那一态 —— 见文件头「仍欠」①）
                var v2 = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.View);
                mi0.OpenWindow(v2);
                CheckTrue(v2 != null && v2.State == DeckInfoPopup.DeckInfoState.View, "开出一扇 `state = 2`（View）的");
                CheckTrue(!v2.IsItemShown("Btn:Edit Deck"), "state 2 ⇒ `Edit Deck` **藏起来**（原版 `state < 2`）");
                CheckTrue(!v2.IsItemShown("Opt:Share") && !v2.IsItemShown("Opt:Share On Chat")
                          && !v2.IsItemShown("Opt:Delete") && !v2.IsItemShown("Opt:Duplicate"),
                          "state 2 ⇒ **四颗同一条**（`Share` / `Share On Chat` / `Delete` / `Duplicate`）全藏");
                CheckTrue(v2.IsItemShown("Btn:Practice Deck"),
                          "state 2 ⇒ `Practice Deck` **仍露**（原版 `isPlayerDeck && state ∈ {0,2}`）");
                CheckTrue(v2.IsItemShown("Opt:Switch Deck Info"), "`Switch Deck Info` **常显**（原版那件没看到 SetActive）");
                CheckTrue(v2.Btn("Edit Deck") != null && v2.Opt("Delete") != null,
                          "…藏起来的那几颗**节点还在**（是 `SetActive(false)`，不是没建 —— 位置/命中区都还按原版摆着）");
                // ② **同一条路的两态**：只把 `State` 改回 0 再摆一次 ⇒ 那五颗必须回来
                v2.State = DeckInfoPopup.DeckInfoState.Edit;
                v2.ApplyStateVisibility();
                CheckTrue(v2.IsItemShown("Btn:Edit Deck") && v2.IsItemShown("Opt:Delete")
                          && v2.IsItemShown("Opt:Duplicate") && v2.IsItemShown("Opt:Share")
                          && v2.IsItemShown("Opt:Share On Chat"),
                          "把 `State` 改回 **0** ⇒ `Edit Deck` 与 4 颗圆钮**都回来了**"
                          + "（同一份实现的两态对比 —— 写成恒真/恒假这里就红）");
                // ③ `Select Deck`：**给了 `context.SelectButton` 才露**（原版 6 个调用点全传 null ⇒ 我们默认恒藏）
                var v3 = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.Edit, true);
                mi0.OpenWindow(v3);
                CheckTrue(v3.IsItemShown("Btn:Select Deck"),
                          "给了 `SelectButton` ⇒ `Select Deck` **露**（原版 `context.SelectButton != null`）");
                CheckTrue(!v2.IsItemShown("Btn:Select Deck"),
                          "…而没给的那一扇**不露**（两条一起才是判据；只断一条分不出「恒露」和「按判据露」）");
                // ④ 🔴 **`isPlayerDeck` 那一半**（2026-10-04 补，X3 审查的 **R6**）：原版四颗圆钮与
                //    `Practice Deck` 都**与**了 `isPlayerDeck`（= `InventoryManager.HasItem(context.Deck)`，
                //    `DeckInfoPopup__Open.c:43-48`）。我们的三处调用点恒 true ⇒ 上面 ①～③ **抓不到**
                //    「把这个合取项丢掉」。这里造一扇 `IsPlayerDeck = false` 的（字段是 public，
                //    ⚠️ 与铁律 5·c 同理：**一个值 ≠ 全部情况**）。
                var v4 = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.Edit);
                v4.IsPlayerDeck = false;                  // Create 之后、OpenWindow（会跑 Build→ApplyStateVisibility）之前
                mi0.OpenWindow(v4);
                CheckTrue(!v4.IsItemShown("Opt:Share") && !v4.IsItemShown("Opt:Share On Chat")
                          && !v4.IsItemShown("Opt:Delete") && !v4.IsItemShown("Opt:Duplicate"),
                          "`isPlayerDeck = false` ⇒ 四颗圆钮**全藏**（原版 `state == 0 && isPlayerDeck` 的**后半条**）"
                          + " —— 把 `mine` 写成恒 `true` 只有这一条会红");
                CheckTrue(!v4.IsItemShown("Btn:Practice Deck"),
                          "…而且 `Practice Deck` **也藏**（原版 `isPlayerDeck && state ∈ {0,2}`）");
                CheckTrue(v4.IsItemShown("Btn:Edit Deck"),
                          "…但 `Edit Deck` **照旧露**（原版只看 `state < 2`，与 `isPlayerDeck` 无关 —— 别把它也乘进去）");
                // ⑤ 🔴 **`Practice Deck` 那一行不能恒 true**（R6 的反例①）：state 0 与 state 2 **两态都该露**
                //    ⇒ 上面 ①～③ 一条都抓不到它。state 1（Import）是判据里「该藏」的那一态
                //    （原版 `DeckInfoControls__Initialize.c:84-97` 有这条；我们**没有**这个调用点，
                //     但字段是 public ⇒ 自检造得出来）。
                var v5 = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.Import);
                mi0.OpenWindow(v5);
                CheckTrue(!v5.IsItemShown("Btn:Practice Deck"),
                          "state 1（Import）⇒ `Practice Deck` **藏**（原版 `state ∈ {0,2}`）"
                          + " —— 只看 state 0/2 的话这一行写成恒 `true` 也全绿");
                CheckTrue(v5.IsItemShown("Btn:Edit Deck"),
                          "…而 `Edit Deck` **仍露**（`state < 2`）—— 与 state 2 正好两态对比"
                          + "（判据写成 `state < 3` 或 `<= 1` 都在这一条上红）");
                CheckTrue(!v5.IsItemShown("Opt:Delete") && !v5.IsItemShown("Opt:Share"),
                          "…四颗圆钮藏（`state == 0` 那一半）");
                CheckTrue(v5.IsItemShown("Opt:Switch Deck Info"),
                          "…`Switch Deck Info` **常显**（三个 state 都一样，原版那件没看到 SetActive）");
                // ⚠️ **R13（本轮没修，改点在 `Shell/DeckInfoPopup.cs:270-274`）**：`IsItemShown` 读的是
                //    **该节点的 `activeSelf`**、父链不参与 ⇒ 上面这些断言的前提是「窗开着」。
                //    谁要是关着窗口来断 A31，会**恒绿**（那是本工程第 N 次「弱断言分不出两态」）。
                v5.Close();
                v4.Close();
                // 🔴 **2026-10-04（A66）补的两句**：上面两扇关了，**这两扇一直没关** —— 它们会一路开着
                //   走到收工（层 3123 的 `Warlord Image` 命中区比屏还大，**顶掉后面所有的真命中路**）。
                //   实据：`_tmp_view/collection.log:6055` 那条「A11 前置：收掉 **2** 扇还开着的
                //   `Deck info Popup`」—— 全场只有这两扇从头到尾没有 `Close()`，正好 2。
                v3.Close();
                v2.Close();
            }

            // ---------------- 🆕 2026-10-03（A10 的尾巴）：`Practice Deck` ⇒ 挑对手 ⇒ 进练习赛 ----------------
            //   判据（唯一一处）= 原版 `Everguild.MatchMakerManager.StartMatch(PlayModes, PlayerBattleData,
            //   CardDeck **playerDeck**, CardDeck **enemyDeck**, …)` 的**形参名**
            //   （签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/Everguild/MatchMakerManager.cs:136`）：
            //     · 点 `Practice Deck` 的那一副 = `DeckInfoPopup.context.Deck` ⇒ **playerDeck（我的）**
            //     · 之后在 `DeckSelectionPopup` 里选的那一副 = 回调形参 ⇒ **enemyDeck（对手）**
            //     · 开打前 `GameStaticData.CheckHiddenCardsInDeck(我的那副)` ⇒ 有隐藏卡就弹提示、**不开打**
            //   ⚠️ 本工程 2026-10-03 之前把这两副写反了（`项目任务.md` §三第29条 A10 的提要）—— 已就地订正。
            //   🔴 **真红法（先说清把哪一行改坏它会红）**：
            //     · `DeckInfoPopup.SelectPracticeOpponentDeck` 里 `modeFilter: info.GameMode` 改成 `null` ⇒ 「按模式筛」红
            //     · `DeckInfoPopup.StartPracticeMatch` 里把「我的」与「对手」换个来源（例如 `mine := 选中的那副`、
            //       `OpponentDeck := 被点的那副`）⇒ 下面「我 / 对手」两条同时红
            //     · `PracticeModePopup.StartBotBattle` 里 `SetPendingOpponentDeck(OpponentDeck)` 那行删掉 ⇒ 「放进通道」红
            //     · `PracticeModePopup.HasHiddenCards` 里 `ForceHiddenCardsDeck` 那一支删掉 ⇒ 隐藏卡那两条红
            //     · `PracticeModePopup.StartPracticeMatch` 里去掉 `w.StartBattle()` ⇒ 「选定即开打」那两条红
            Section("`Practice Deck`：这一副 = 我的 · 挑**对手** · 选定即开打（A10 尾巴）");
            {
                int curSaved = CollectionData.CurrentIndex();
                var mi = CollectionData.DeckAt(0);                    // 被点的那一副（自检给第 1 套塞了督军+3 张卡）
                // 🔴 让「按模式筛」这一条**真有鉴别力**：14 套测试卡组默认**全是经典(0)** ⇒
                //    不区分模式的话「筛了」与「没筛」结果一样（筛了也红不了 = 等于没查）。
                //    把**最后一副**改成遭遇(13)，验完还原（只改内存，`DeckStore` 指向临时文件）。
                int flipIdx = CollectionData.DeckCount() - 1;
                int flipSaved = flipIdx > 0 ? CollectionData.Raw(flipIdx).GameMode : 0;
                if (flipIdx > 0) CollectionData.Raw(flipIdx).GameMode = (int)GameMode.Skirmish;
                DeckSelectionPopup.LastOpened = null;
                DeckInfoPopup.LastOpponentSelection = null;
                PracticeModePopup.LastOpened = null;
                PracticeModePopup.ClearPendingOpponentDeck();

                var dp = win.OpenDeckInfo(0);
                var dpb = dp != null ? dp.Btn("Practice Deck") : null;
                var dpw = dpb != null ? dpb.GetComponent<WindowButton>() : null;
                CheckTrue(dpw != null, "`Practice Deck` 有点击区");
                if (dpw != null)
                {
                    dpw.ClickForTest();
                    CheckTrue(dp != null && dp.CurrentState == WindowState.Closed,
                              "点它 ⇒ **把自己关掉**（原版开完那扇窗就 `Close` 自己）");
                    var sel = DeckSelectionPopup.LastOpened;
                    CheckTrue(sel != null && sel == DeckInfoPopup.LastOpponentSelection,
                              "…并开**选卡组窗**（原版 `SelectPracticeOpponentDeck` 开的就是这扇，**不另建**）");
                    Check(sel != null ? sel.ModeFilter : null, (int?)mi.GameMode,
                          "选卡组窗按**这一副的模式**筛（原版 lambda：候选.GameMode == `context.Deck.GameMode`）");
                    CheckTrue(sel != null && !sel.OwnDecks, "起手落在**预组卡组**页（`DeckSelectionTabController.Start`）—— 对手多半是预组");
                    if (sel != null)
                    {
                        sel.SwitchTab(true);                          // 换到「我的卡组」页才看得见模式筛选
                        int wantOwn = 0;
                        for (int i = 0; i < CollectionData.DeckCount(); i++)
                            if (CollectionData.DeckAt(i).GameMode == mi.GameMode) wantOwn++;
                        Check(sel.ShownCount, wantOwn,
                              $"「我的卡组」页列出 **{wantOwn}** 副同模式的（筛多 = 没传 `ModeFilter`；筛少 = 筛错档）");
                        CheckTrue(wantOwn < CollectionData.DeckCount(),
                                  "…而且这一条**真有鉴别力**：库里还有 "
                                  + (CollectionData.DeckCount() - wantOwn) + " 副别的模式的（上面刚把最后一副改成遭遇）被筛掉了");

                        // 🆕 2026-10-04（A26）：**第二扇窗**（选卡组窗）的卡格也要**真鼠标**点得到 ——
                        //    它与收藏窗共用 `MenuDraw.DeckCell`（同一处裸节点）⇒ 一起修好的，这里也钉一条。
                        //    判据 = `PointerLayer.CollectHits`（`Shell/PointerLayer.cs:492`），不是 `wb.Click()`。
                        CheckTrue(sel.Cells.Count > 0, "选卡组窗画出了格子（否则下面那条等于没验）");
                        if (sel.Cells.Count > 0)
                        {
                            var sc0 = sel.Cells[0];
                            var sh0 = FindChild(sc0, "Hit");
                            var shq = sh0 != null ? sh0.GetComponentInChildren<ImageQuad>() : null;
                            CheckTrue(shq != null,
                                      "★ 选卡组窗的格子：命中区节点下**真的挂着 `ImageQuad`**（A26）");
                            var pl2 = PointerLayer.Instance;
                            if (shq != null && pl2 != null)
                            {
                                var sb = pl2.ButtonAt(PxOf(sc0.position.x), PxYOf(sc0.position.y));
                                CheckTrue(sb != null && sb.transform == sh0,
                                          "★ …而**真命中路**在格中心拿到的就是这一格"
                                          + "（拿到 " + (sb == null ? "**null** = 真鼠标点不动" : "`" + sb.name + "`") + "）");
                            }
                        }
                    }
                    // 挑一副**不是我自己**的当对手（期望值由数据算，不写死名字）
                    int foeIdx = -1;
                    for (int i = 1; i < CollectionData.DeckCount(); i++)
                        if (CollectionData.DeckAt(i).GameMode == mi.GameMode) { foeIdx = i; break; }
                    CheckTrue(foeIdx > 0, "测试库里另有一副同模式的（没有它就没法验「谁是对手」）");
                    if (sel != null && foeIdx > 0)
                    {
                        var foe = CollectionData.Raw(foeIdx);
                        sel.Pick(new DeckSelectionPopup.DeckPick
                        {
                            Prebuilt = false,
                            Info = CollectionData.DeckAt(foeIdx),
                            OwnIndex = foeIdx,
                            PrebuiltDeck = null,
                        });
                        Check(sel.CurrentState, WindowState.Closed, "选完 ⇒ 选卡组窗自己关（原版 `Select` 的两步：关窗 + 回调）");
                        var prac = PracticeModePopup.LastOpened;
                        CheckTrue(prac != null, "回调 ⇒ **开练习窗并立刻开打**（原版 `StartPracticeMatch` 选完就 `StartMatch`）");
                        if (prac != null)
                        {
                            Check(prac.DeckIndex, 0,
                                  "**我** = 被点 `Practice Deck` 的那一副（原版 `playerDeck` = `DeckInfoPopup.context.Deck`）");
                            CheckTrue(prac.OpponentDeck != null && prac.OpponentDeck.Name == foe.Name,
                                      "**对手** = 刚在窗里选中的那一副（原版 `enemyDeck` = 回调回来那副）");
                            CheckTrue(prac.OpponentDeck != null && prac.OpponentDeck.Name != mi.Name,
                                      "两副**不是同一副** —— 放反了这条就红（这两副名字本来就不同）");
                            CheckTrue(prac.SearchingMatch, "选定 ⇒ 立刻进「等对手」那 12 秒（原版 `ShowPopUp(等待窗)` → `StartMatch`）");
                            CheckTrue(!prac.StartedBattle, "12 秒还没到 ⇒ 不抢跑");
                            prac.TickSearch(12f);
                            CheckTrue(prac.StartedBattle, "等满 12 秒 ⇒ **真开打**（批处理只记账；真机上这一步 `LoadScene(\"Battle\")`）");
                            Check(CollectionData.CurrentIndex(), prac.DeckIndex,
                                  "开战前把**我那一副**交给 `DeckLibrary`（`BattleDriver.PickSavedDeck` 读的就是它）");
                            var pend = PracticeModePopup.PendingOpponent;
                            CheckTrue(pend != null && pend.Name == foe.Name,
                                      "开战时把**对手那副**放进「本局对手」通道 —— `BattleDriver.BeginFromDeckLibrary` 该读的就是它");
                            CheckTrue(pend != null && pend.Name != mi.Name, "…通道里**不是**我自己那副");
                            var took = PracticeModePopup.TakePendingOpponentDeck();
                            CheckTrue(took != null && took.Name == foe.Name,
                                      "`TakePendingOpponentDeck()` 拿得到 —— 开局那条路读的就是它");
                            CheckTrue(PracticeModePopup.TakePendingOpponentDeck() == null,
                                      "**读一次就清**（下一局不会带着上一局的对手）");
                            prac.Close();       // 清理：别盖住后面的截图
                        }
                    }
                    // ② **隐藏卡**前置检查（原版 `GameStaticData.CheckHiddenCardsInDeck`）：注入 ⇒ 弹提示、**不开打**。
                    //   ⚠️ 为什么用注入：原版判据是 `PlayerItem.IsHidden()`，而这个 build 里 `RawCardScript` 没覆写它、
                    //      我们的卡数据也没有「隐藏」字段 ⇒ 不注入的话这条分支**永远走不到**（= 等于没查）。
                    foreach (var pp in Object.FindObjectsByType<PromptPopup>(FindObjectsSortMode.None))
                        if (pp != null) pp.Close();
                    var dp3 = win.OpenDeckInfo(0);
                    PracticeModePopup.LastOpened = null;
                    PracticeModePopup.ClearPendingOpponentDeck();
                    PracticeModePopup.ForceHiddenCardsDeck = CollectionData.Raw(0);   // = 「我这一副」那个对象
                    var d3 = dp3 != null ? dp3.Btn("Practice Deck") : null;
                    var d3w = d3 != null ? d3.GetComponent<WindowButton>() : null;
                    if (d3w != null) d3w.ClickForTest();
                    var sel3 = DeckSelectionPopup.LastOpened;
                    if (sel3 != null && foeIdx > 0)
                        sel3.Pick(new DeckSelectionPopup.DeckPick
                        {
                            Prebuilt = false,
                            Info = CollectionData.DeckAt(foeIdx),
                            OwnIndex = foeIdx,
                            PrebuiltDeck = null,
                        });
                    CheckTrue(PracticeModePopup.LastOpened == null,
                              "我自己那副带**隐藏卡** ⇒ **不开练习赛**（原版 `CheckHiddenCardsInDeck` 那一支：弹提示、不 `StartMatch`）");
                    CheckTrue(PracticeModePopup.PendingOpponent == null, "…连「本局对手」通道都不该被写上");
                    PromptPopup hp = null;
                    foreach (var pp in Object.FindObjectsByType<PromptPopup>(FindObjectsSortMode.None))
                        if (pp != null) hp = pp;
                    var hpTxt = hp != null ? TextOf(FindChild(hp.transform, "MessageText")) : null;
                    CheckTrue(hpTxt != null && hpTxt.Contains("隐藏卡"),
                              "…并且**弹出提示说清原因**（不许静默）—— 实测文案「" + (hpTxt ?? "<没有提示窗>") + "」");
                    PracticeModePopup.ForceHiddenCardsDeck = null;
                    if (hp != null) hp.Close();
                    // 🔴 **2026-10-04（A66）**：这一扇（`dp3`）走到这里必须**显式收掉**。
                    //   它的「开完自己关」那条路在 `DeckInfoPopup.SelectPracticeOpponentDeck():667` 里，
                    //   但**只在点击真的发生了**（`d3w != null`）时才走得到 ⇒ 补一句兜底，
                    //   别让一扇层 3123 的模态一路盖到收工（那会顶掉后面所有人的真命中路）。
                    if (dp3 != null) dp3.Close();
                    // ③ 预组也能当对手（原版那条链默认就落在预组页）—— 判据只一份：`PracticeModePopup.PlayerDeckOf`
                    if (PrebuiltDecks.Available && PrebuiltDecks.Tab.Count > 0)
                    {
                        var pk = PrebuiltDecks.Tab[0];
                        var pd = PracticeModePopup.PlayerDeckOf(new DeckSelectionPopup.DeckPick
                        {
                            Prebuilt = true,
                            Info = DeckSelectionPopup.InfoOf(pk),
                            OwnIndex = -1,
                            PrebuiltDeck = pk,
                        });
                        CheckTrue(pd != null && pd.WarlordId == pk.heroId,
                                  "选预组当对手 ⇒ 搓出来的 `PlayerDeck` 督军 = 那一副的督军（" + pk.deckId + " → " + pk.heroId + "）");
                    }
                    else Debug.LogWarning(P + "   预组数据读不到 ⇒ ③ 那一条跳过了（**不是通过**）");
                }
                if (flipIdx > 0) CollectionData.Raw(flipIdx).GameMode = flipSaved;   // 还原上面动过的那一副
                CollectionData.Select(curSaved);      // 上面开战那一步会改「当前卡组」⇒ 还原
            }

            // ---------------- `Import Deck Popup`（A1 §4）----------------
            Section("`Import Deck Popup`：版面 + **导入闭环**（A1 §4）");
            {
                var imp = win.OpenImportPopup();
                CheckTrue(imp != null, "开得出来");
                if (imp != null)
                {
                    var iroot = imp.transform;
                    // 🆕 A17：`Confirm`（九宫底 `40K_button`）与绿色关闭钮逐个悬停验一遍
                    CheckHoverSwap(imp.transform, "Import Deck Popup");
                    Check(imp.type, WindowType.Popup, "`type` = **1 Popup**（原文）");
                    Check(imp.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
                    CheckAt(FindChild(iroot, "Generic Popup Background"), 560f, 1360f, 234.07f, 685.93f,
                            "`Window`（560,234.07 → 1360,685.93）");
                    CheckText(TextOf(FindChild(iroot, "Main Search message")), "Paste your deck", "提示行文案");
                    var ph = FindChild(iroot, "Input Text");
                    CheckText(TextOf(ph), "Enter text...", "空输入时显示**占位符**");
                    // 🔴 实拍抓的：占位符第一版**跑到输入框上面去了**（`basis` 给了 root 而 parent 是 `Window`）
                    var phLb = ph != null ? ph.GetComponent<Label>() : null;
                    CheckTrue(phLb != null && Mathf.Abs(PxYOf(ph.transform.position.y) - 441.03f) <= 1.5f,
                              $"占位符**落在输入框里**（中心 y 实测 {(ph != null ? PxYOf(ph.transform.position.y) : -1f):F1}"
                              + "，原版 `Text Area` 377→505.06 ⇒ 中心 **441.03**）");
                    CheckText(TextOf(FindChild(iroot, "Confirm Text")), "Confirm", "确认钮文案");
                    Shoot("06_收藏_ImportDeck弹窗.png");   // ⚠️ **趁窗开着拍**（第一版拍在导入成功之后 ⇒ 窗已经关了）
                    // ⚠️ 别拿九宫格的**根**量宽 —— `GetComponentInChildren<ImageQuad>()` 取到的是**角块**
                    //    （第一版量出 190.76 = 一个角）。量**点击区那个单 quad**（= 整个按钮矩形）。
                    CheckNear(imp.OkHit != null ? Wpx(imp.OkHit) : -1f, 478.343f, 2f,
                              "确认钮宽 = **478.343**（原版；VLG 只有一个钮 ⇒ 在容器里居中）");
                    var okBtn = imp.OkHit != null ? imp.OkHit.GetComponent<WindowButton>() : null;
                    CheckTrue(okBtn != null, "`Confirm` 有点击区");
                    // ① 空串
                    if (okBtn != null) okBtn.Click();
                    CheckText(imp.ErrorText, "先粘贴卡组串",
                              "空串 ⇒ 给**人话**（**与卡组编辑那边逐字一致** —— 判据只有 `CollectionData.ImportDeck` 一份）");
                    Check(imp.CurrentState, WindowState.Open, "失败**不关窗**");
                    // ② 乱串
                    imp.SetTextForTest("这不是一条卡组串");
                    if (okBtn != null) okBtn.Click();
                    CheckText(imp.ErrorText, "这不是一条合法的卡组串", "乱串 ⇒ 另一句人话");
                    // ③ 真串（拿卡组 1 导出的串再导回来）
                    int n0 = CollectionData.DeckCount();
                    var src = DeckLibrary.ExportString(DeckLibrary.Load().Decks[0]);
                    imp.SetTextForTest(src);
                    if (okBtn != null) okBtn.Click();
                    Check(CollectionData.DeckCount(), n0 + 1, "合法串 ⇒ 卡组数 +1（**导入真的接上了**）");
                    Check(imp.CurrentState, WindowState.Closed, "成功 ⇒ 窗关上");
                    // ④ 点背景也关（原版 `backgroundCloseButton`）
                    var imp2 = win.OpenImportPopup();
                    var shade = imp2 != null && imp2.ShadeHit != null ? imp2.ShadeHit.GetComponent<WindowButton>() : null;
                    CheckTrue(shade != null, "背景有点击区（原版 `backgroundCloseButton`）");
                    // 🆕 2026-10-04（**A25⑥ 的断言模板**）：压暗层的命中区**档**必须落在压暗层自己那一档、
                    //   且**严格低于**本窗内容命中区档 —— 同档时 `ImageQuad` 的世界 z 恒 0，谁吃到退化成
                    //   「枚举顺序」，症状是**点不动的钮看着像正常工作**（A27 那批用真缺陷买来的）。
                    //   期望值 = 本窗自己的两个**原版档常量**（`QImp` / `QImpHit`），⛔ 不从被测实现里读。
                    if (imp2 != null)
                    {
                        string shadeWhy;
                        CheckTrue(MenuDraw.ShadeRuleOk(imp2.ShadeHit, ImportDeckPopup.QImp, ImportDeckPopup.QImpHit,
                                                       out shadeWhy),
                                  "压暗命中区档 = **压暗层自己那一档**、且**严格低于**内容命中区档"
                                  + (shadeWhy.Length > 0 ? "（" + shadeWhy + "）" : ""));
                    }
                    if (shade != null) shade.Click();
                    Check(imp2 != null ? imp2.CurrentState : WindowState.Open, WindowState.Closed, "点背景 ⇒ 关窗");
                }
            }

            // ---------------- 切页（`visualTypes` 必须整表替换） ----------------
            Section("切页（正本 §二；`visualTypes` 被本窗整表替换）");
            if (win.tabButtons != null)
            {
                win.tabButtons.Click(1);
                Check(win.CurrentTab, WindowTabType.CollectionCards, "点第 2 键 ⇒ 切到 **Cards** 页");
                var p2 = FindChild(tabsRoot, "Card Collection Tab");
                CheckTrue(p2 != null && p2.gameObject.activeSelf, "`Card Collection Tab` 开着");
                CheckTrue(!FindChild(tabsRoot, "Select Deck Tab").gameObject.activeSelf, "`Select Deck Tab` 关着");
                win.tabButtons.Click(0);
                Check(win.CurrentTab, WindowTabType.CollectionDecks, "点回第 1 键 ⇒ 回 Decks 页");
                // 🔴 选中态：**只亮当前那个键**（第一版漏了刷高亮 —— 截图里 DECKS 页却亮着 CARDS）
                var hi = new List<Transform>();
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Highlight" && t.parent != null && t.parent.name.StartsWith("CollectionTabButton_"))
                        hi.Add(t);
                Check(hi.Count, 4, $"左栏四个键的选中底图都建了（实测 {hi.Count}）");
                int on = 0;
                foreach (var h in hi) if (h.gameObject.activeSelf) on++;
                Check(on, 1, $"**恰好一个键亮着**（实测 {on} 个）—— 「切页要跟着刷高亮」的判据");
                if (hi.Count == 4)
                    CheckTrue(hi[0].gameObject.activeSelf && !hi[1].gameObject.activeSelf,
                              "亮的是**第 1 键**（当前页 = Decks）");
            }

            // ---------------- Cards 页：卡池网格（正本 §四；A3 实测）----------------
            Section("Cards 页：卡池网格（6 列 · 262.5×384 · **贴左但整体居中**）");
            win.tabButtons.Click(1);                       // 切到 Cards
            var cardsHolder = FindChild(tabsRoot, "Scroll View");
            CheckAt(cardsHolder, 330.2f, 1919.9f, 155.9f, 1079.9f,
                    "`Scroll View`（卡池视口；⚠️ 与 Deck 页的视口**差 0.7/0.1**，两份实测都记着）");
            CheckTrue(win.CardsVisibleCount > 1000, $"卡池读到 {win.CardsVisibleCount} 张（`CardDatabase.Load()`）");
            CheckTrue(win.CardsCells.Count > 0, $"画出了 {win.CardsCells.Count} 格（视口外的不建 = `RectMask2D` 那套）");
            if (win.CardsCells.Count > 0)
            {
                var cc0 = win.CardsCells[0];
                CheckNear(PxOf(cc0.position.x), 468.8f, 1.5f,
                          "第 1 格中心 x = **468.8**（贴左但整体居中：330.2 + 7.35 + 262.5/2）");
                CheckNear(PxYOf(cc0.position.y), 347.9f, 1.5f, "第 1 格中心 y = **347.9**（155.9 + 384/2）");
            }
            if (win.CardsScroll != null && win.CardsScroll.MaxOffset > 0f)
            {
                win.CardsScroll.ScrollBy(win.CardsScroll.MaxOffset);
                int lastIdx = win.CardsVisibleCount - 1;
                var lc = FindChild(tabsRoot, "CollectionCard_" + lastIdx);
                CheckTrue(lc != null && PxYOf(lc.position.y) + 192f <= 1080.5f,
                          $"**滚到底 ⇒ 第 {lastIdx + 1} 张（最后一张）完整落进视口**");
                win.CardsScroll.ScrollBy(-win.CardsScroll.MaxOffset);
            }

            // 🆕 2026-10-03（A12 收口）：Cards 页 —— 压在视口下边上的那一格，**命中区 == 露出来的那部分**
            //   （这一页的 `Hit` 走 `AddHit` ⇒ **有 quad** ⇒ 能直接量渲染矩形；判据同 `MenuDraw.ClipRect`）
            if (win.CardsScroll != null)
            {
                var cs = win.CardsScroll;
                float savedC = cs.Offset;
                cs.SetOffset(0f);
                int edgeIdx = -1;
                for (int i = 0; i < win.CardsVisibleCount; i++)
                {
                    var rr = CollectionWindow.CardsCellRect(i);
                    if (rr.y1 < CollectionWindow.CardsViewport.y2 - 0.5f
                        && rr.y2 > CollectionWindow.CardsViewport.y2 + 0.5f) { edgeIdx = i; break; }
                }
                CheckTrue(edgeIdx >= 0, "起手有一格**压在卡池视口下边上**（否则这一条等于没验）");
                if (edgeIdx >= 0)
                {
                    // 视口 330.2,155.9 → 1919.9,1079.9（本文件上面 `CheckAt(cardsHolder, …)` 已钉住）
                    var vp = CollectionWindow.CardsViewport;
                    var onScreen = cs.Shift(CollectionWindow.CardsCellRect(edgeIdx));
                    PxRect vis;
                    bool inView = MenuDraw.ClipRect(onScreen, vp, out vis);
                    var h = FindChild(tabsRoot, "CardHit_" + edgeIdx);
                    float x1, y1, x2, y2;
                    CheckTrue(h != null && RectOf(h, out x1, out y1, out x2, out y2),
                              $"第 {edgeIdx + 1} 格（压边那一格）的 `Hit` 建了、渲染矩形量得到");
                    if (h != null && RectOf(h, out x1, out y1, out x2, out y2))
                    {
                        CheckTrue(inView, "现算：这一格与视口**有交集**");
                        CheckNear(y2, vp.y2, 0.5f, "命中区的**下边缘 = 视口下边 1079.9**（被裁在那儿）");
                        CheckNear(y2 - y1, vis.H, 1.0f, $"命中区高 = **露出来的那部分**（{vis.H:F1}px）");
                        CheckTrue(y2 - y1 < CollectionWindow.CardsCellH - 1f,
                                  $"…而且确实**比整格矮**（{y2 - y1:F1} < {CollectionWindow.CardsCellH}）");
                        CheckNear(x2 - x1, CollectionWindow.CardsCellW, 1f, "…横向没裁 ⇒ 宽仍是格的宽 262.5");
                    }
                    // 对照：完全落在视口里的那一格 ⇒ 命中区是**整格**
                    var h0 = FindChild(tabsRoot, "CardHit_0");
                    float ax1, ay1, ax2, ay2;
                    if (h0 != null && RectOf(h0, out ax1, out ay1, out ax2, out ay2))
                    {
                        CheckNear(ax2 - ax1, CollectionWindow.CardsCellW, 1f, "对照：视口里的那一格 ⇒ 命中区**整格宽**");
                        CheckNear(ay2 - ay1, CollectionWindow.CardsCellH, 1f, "对照：…**整格高**（没被裁）");
                    }
                }
                cs.SetOffset(savedC);
            }
            // 筛选：**复用卡组编辑那套 `DeckEditorState.Filter`**（别写第二套）
            {
                int all = win.CardsVisibleCount;
                var f = DeckFilter.None; f.Rarity = "legendary";
                CollectionWindow.CardsState.SetFilter(f);
                win.RebuildCardsPage();
                CheckTrue(win.CardsVisibleCount > 0 && win.CardsVisibleCount < all,
                          $"筛 `legendary` ⇒ 可见卡 **{all} → {win.CardsVisibleCount}**（筛选真的接上了）");
                CollectionWindow.CardsState.SetFilter(DeckFilter.None);
                win.RebuildCardsPage();
                Check(win.CardsVisibleCount, all, "清空筛选 ⇒ 卡数回到原来的数");
            }

            // ---------------- Cards 页：**完整筛选面板**（正本 §五·1；A3 + `menu_rect.py` 实读）----------------
            Section("Cards 页：筛选面板（7 行 · 面板内坐标逐条比原版）");
            var fltPanel = FindChild(tabsRoot, "Card Filters");
            CheckTrue(fltPanel != null, "面板节点 `Card Filters` 建了");
            if (fltPanel != null)
            {
                // 原版**起手是收起的**（整栏滑到 `hiddenPosition=(-550,0)`，行程 −385px）
                // ⇒ 我们也是「收起 = 滑出去 + 滑完 SetActive(false)」（🆕 A11 起；原来只有后面那半截）
                CheckTrue(!fltPanel.gameObject.activeSelf, "**起手收起**（原版 `hiddenPosition = (-550, 0)`）");
                win.ToggleFilters();
                CheckTrue(fltPanel.gameObject.activeSelf, "`Filters` 圆钮 ⇒ 面板打开");

                CheckAt(fltPanel, 0.25f, 335.56f, 155.9f, 1079.99f,
                        "面板矩形 = **0.25,155.9 → 335.56,1080**（原版 x/w；高按**屏幕可见**的 924.1 裁）");
                CheckNear(FilterPanelModel.ContentHFor(CollectionWindow.CardsState), 1389.02f, 0.1f,
                          "内容高 = **1389.02** = 79.02+50+50 + **Army 550**（13 格 3 列 = 5 行）+ 280+230+150"
                          + "（⚠️ 2026-09-28 前写的是 989.02 —— 那是把 Army 行当成 150 算的，见 `FilterPanelModel.ArmyRowH`）");
                var fscr = win.FilterScroll;
                CheckTrue(fscr != null, "面板挂了滚动区（原版 `Scroll View` sens **50**、`Viewport` + `Mask showGraphic=0`）");
                if (fscr != null)
                    CheckNear(fscr.MaxOffset, 1389.02f - 924.1f, 0.6f,
                              "可滚量 = **约 464.9**（1389.02 − 924.1）—— **「Cost / Type 够得着」的判据**");

                // 🔴 「一个值 ≠ 全部情况」（铁律 5·c）：**选项表是从 MB 实读的**，不是按枚举直觉编
                //    （`Card*Filter.options` 的 `alternativeText`）：Army 13 / Rarity 5 / Cost **8** / Type 3
                //    + `Owned only` / `Upgradable only` 两格（原版 `filters[6]` 里的前两个）
                Check(win.FilterCellCount, 31,
                      "格子总数 = **31** = Owned + Upgradable + Army **13** + Rarity **5** + Cost **8** + Type **3**"
                      + "（⚠️ Cost 是**区间档**不是每费一格；⚠️ Type **没有防御卡那一档**）");

                // Army 第 1 格：Content 从行内 y+50 起、pad L14、cell 100×100
                //   ⚠️ 阵营名别写死 —— 按 `CardsState.Factions()`（**排序过**）的第 1 个取
                var facs = CollectionWindow.CardsState.Factions();
                CheckTrue(facs.Count > 0, $"卡池里有 {facs.Count} 个阵营（Army 行按它铺格）");
                var a0 = facs.Count > 0 ? FindChild(fltPanel, "Cell_fac_" + facs[0]) : null;
                CheckTrue(a0 != null,
                          "Army 那一格在（按**名字**找，不按序号 —— 视口外的格不建）；"
                          + "第 1 格 = 「" + (facs.Count > 0 ? facs[0] : "?") + "」");
                if (a0 != null)
                {
                    CheckNear(PxOf(a0.position.x), 64.25f, 0.6f, "Army 第 1 格中心 x = **64.25**（0.25+14+50）");
                    CheckNear(PxYOf(a0.position.y), 434.92f, 0.6f,
                              "Army 第 1 格中心 y = **434.92**（155.9+179.02+50+50）");
                    CheckArt(a0, DeckRuntime.FactionIcon(facs[0]), "Army 格的图 = 该阵营图标（原版运行时赋）");
                }
                // Rarity 第 1 格：Content 从行内 y+65 起；图**比格小**（格 100²、图 50² 居中）
                var r0 = FindChild(fltPanel, "Cell_rar_common");
                CheckTrue(r0 != null, "Rarity 的 Common 格在");
                if (r0 != null)
                {
                    CheckNear(PxYOf(r0.position.y), 999.92f, 0.6f,
                              "Rarity 第 1 格中心 y = **999.92**（155.9 + **729.02** + 65 + 50；729.02 = Army 行 550 之后）");
                    CheckArt(r0, "1_40k_cardframe_rarity_common", "Rarity Common 的图 = `1_40k_cardframe_rarity_common`");
                    var bg = FindChild(r0, "Background");
                    CheckNear(Wpx(bg), 50f, 2f, "Rarity 格里的图宽 = **50**（格 100 ⇒ 图只有一半，原版如此）");
                    // 🔴 实拍又抓一条：标签像**贴着面板左边界、被切掉**。
                    //    做法同 CLAUDE.md 的「量渲染真值」——**量 Label 自己的宽度算左边缘**，别只看节点位置。
                    //    ⚠️ 第一版这条断言**恒真**（宽度量出来 0 ⇒ 左边缘 = 右边界）⇒ 现在**同时要求宽度合理**。
                    //    ⚠️ 第二版又抓出 `Legendary` 105.9px > 原版格宽 100 ⇒ 补上原版的 `auto(10-27)`
                    //      （`SetAutoFitBox`），再断「缩完之后落进格子里」。
                    var lab0 = FindChild(r0, "Label");
                    var lb0 = lab0 != null ? lab0.GetComponent<Label>() : null;
                    float lw = lb0 != null ? lb0.WorldW * 108f : 0f;
                    float lleft = lab0 != null ? PxOf(lab0.position.x) - lw * 0.5f : -999f;
                    CheckTrue(lb0 != null && lw > 20f,
                              $"Rarity 格的标签**量得出宽度**（实测 {lw:F1}px）—— 量到 0 就是「TMP 在非激活对象上量不出尺寸」");
                    CheckTrue(lb0 != null && lw <= 100.5f,
                              $"标签**缩进原版的 100px 格宽里**（实测 {lw:F1}px ≤ 100）"
                              + " —— 判据是原版那几处标着 `auto(10-27)`；不开自适应会冲出格子");
                    CheckTrue(lb0 != null && lb0.FontPxNow >= 9.5f && lb0.FontPxNow <= 27.5f,
                              $"标签字号落在原版 `auto(10-27)` 区间里（实测 {lb0?.FontPxNow:F1}px）");
                    CheckTrue(lb0 != null && lleft >= 0f && lleft + lw <= 335.6f,
                              $"Rarity 格的标签**落在面板内**（左边缘 {lleft:F1}px · 文字宽 {lw:F1}px · "
                              + $"原版右对齐到格子右边 {114.25f:F1}px）");
                }
                // Cost 第 1 格 / Type 那一行：**2026-09-28 起都在视口外** ——
                //   Army 行的高度现在按**它自己的内容**算（13 格 · 3 格/行 = 5 行 = 550），Rarity 及以下整排往下挪
                //   ⇒ **不滚到底，Cost / Type 的格子根本不建**（原版 `RectMask2D` 那套裁切 + 我们的视口剔除）。
                //   判据与两处证据（A3 的 150 vs 卡组编辑那棵树的 `332x0`）→ `FilterPanelModel.ArmyRowH` 的注释。
                if (fscr != null)
                {
                    CheckTrue(FindChild(fltPanel, "Cell_cost_1") == null && FindChild(fltPanel, "Cell_type_hero") == null,
                              "**不滚时 Cost / Type 的格子不建**（都在视口下方）");
                    fscr.ScrollBy(fscr.MaxOffset);                       // 滚到底
                    var c0 = FindChild(fltPanel, "Cell_cost_1");
                    CheckTrue(c0 != null, "滚到底 ⇒ Cost 的 `1-` 格建出来了");
                    if (c0 != null)
                    {
                        CheckNear(PxOf(c0.position.x), 47.75f, 0.6f, "Cost 第 1 格中心 x = **47.75**（0.25+15+32.5）");
                        // 面板内 y = 609.02+65+32.5（旧值，见 `ArmyRowH`）→ 现在 = 1009.02+65+32.5，再减可滚量 464.92
                        CheckNear(PxYOf(c0.position.y), 155.9f + 1009.02f + 65f + 32.5f - 464.92f, 0.8f,
                                  "Cost 第 1 格中心 y（滚到底后）= **797.5**");
                        CheckNear(Wpx(c0), 65f, 2f, "Cost 格 = **65×65**（原版 `cell 65×65`）");
                        CheckArt(c0, "Card_Frame_Cost_Icon", "Cost 格的图 = `Card_Frame_Cost_Icon`");
                    }
                    var th = FindChild(fltPanel, "Cell_type_hero");
                    CheckTrue(th != null && PxYOf(th.position.y) + 50f <= 1080.5f,
                              "滚到底 ⇒ Type 那 3 格**完整落进视口**");
                    CheckArt(th, "40k_menu_search_icon_warlord", "Type 第 1 格（Warlord）的图 = `40k_menu_search_icon_warlord`");
                    CheckTrue(FindChild(fltPanel, "Cell_type_unit") != null
                              && FindChild(fltPanel, "Cell_type_tactic") != null,
                              "Type 另两格 `Troops` / `Stratagem` 也在（图 = `..._troop` / `..._stratagem`）");
                    fscr.ScrollBy(-fscr.MaxOffset);                  // ⚠️ 量完**滚回顶部** —— 下面几条断言
                    //    （四个小标题、搜索框）量的都是**顶部**那些件，不滚回去它们根本不建
                }

                // 四行的小标题（原版 `Title` TMP · **fs32 · hAlign=Center**）
                //   🔴 2026-09-23 **实拍补的缺口**：第一版只建了格子、**四个标题一个都没建**，
                //      96 条断言全绿 —— 因为它们不是「摆错位」而是「根本不在」，而当时没有盯这一条的断言。
                foreach (var ttl in new[] { "Army", "Rarity" })
                    CheckText(TextOf(FindChild(fltPanel, "Title " + ttl)), ttl,
                              $"小标题 `{ttl}` 在（原版 `Title` TMP fs32；它在**顶部视野内**）");
                // ⚠️ 2026-09-28：`Energy Cost` / `Type` 两个标题落在 Army 行（550 高）之后 ⇒ **要滚下去才建**
                if (fscr != null)
                {
                    fscr.ScrollBy(fscr.MaxOffset);
                    foreach (var ttl in new[] { "Energy Cost", "Type" })
                        CheckText(TextOf(FindChild(fltPanel, "Title " + ttl)), ttl,
                                  $"小标题 `{ttl}` 在（滚到底之后才够得着）");
                    fscr.ScrollBy(-fscr.MaxOffset);
                }

                // ---- 筛选**真的接上了**（每一条都拿 `DeckEditorState` 的结果数对照）----
                int all = win.CardsVisibleCount;
                System.Action<string> clickCell = key =>
                {
                    var cell = FindChild(tabsRoot, key);
                    var hit = cell != null ? FindChild(cell, "Hit") : null;
                    var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
                    if (wb != null) wb.Click();
                };
                clickCell("Cell_rar_legendary");
                var vis = CollectionWindow.CardsState.VisibleCards();
                CheckTrue(vis.Count > 0 && vis.Count < all, $"点 `Legendary` ⇒ 卡池 {all} → {vis.Count}");
                bool allLeg = true;
                foreach (var c in vis) if (c.Rarity != "legendary") { allLeg = false; break; }
                CheckTrue(allLeg, "筛出来的**每一张**都是 `legendary`");

                win.ClearCardFilters();
                Check(win.CardsVisibleCount, all, "`Clear filters` ⇒ 卡数回到 " + all);

                // ⚠️ Cost / Type 的格子**滚到底才建**（见上一段那条）⇒ 点它们之前先滚下去
                if (fscr != null) fscr.ScrollBy(fscr.MaxOffset);
                clickCell("Cell_cost_8");
                vis = CollectionWindow.CardsState.VisibleCards();
                bool allGe8 = vis.Count > 0;
                foreach (var c in vis) if (c.Cost < 8) { allGe8 = false; break; }
                CheckTrue(allGe8, $"点 `8+` ⇒ {vis.Count} 张**全部 cost ≥ 8**"
                                  + "（原版 Cost 是**区间档**：`1-`/2…7/`8+`，不是每费一格）");
                win.ClearCardFilters();

                clickCell("Cell_cost_1");
                vis = CollectionWindow.CardsState.VisibleCards();
                bool allLe1 = vis.Count > 0;
                foreach (var c in vis) if (c.Cost > 1) { allLe1 = false; break; }
                CheckTrue(allLe1, $"点 `1-` ⇒ {vis.Count} 张**全部 cost ≤ 1**（下界那档的效果）");
                win.ClearCardFilters();

                clickCell("Cell_type_hero");
                vis = CollectionWindow.CardsState.VisibleCards();
                bool allHero = vis.Count > 0;
                foreach (var c in vis) if (c.Type != "hero") { allHero = false; break; }
                CheckTrue(allHero, $"点 `Warlord` ⇒ {vis.Count} 张**全部是督军**（= `CardTypeOptions.Hero`）");
                win.ClearCardFilters();
                if (fscr != null) fscr.ScrollBy(-fscr.MaxOffset);        // 回顶部（搜索框在上面）

                // 搜索框：**外壳自己没有键盘**（`PointerLayer` 原来明写「键盘没实现」）⇒ 2026-09-23 补上
                var pl = PointerLayer.Instance;
                CheckTrue(pl != null, "`PointerLayer` 在（键盘走它）");
                if (pl != null)
                {
                    clickCell("Name Filter");
                    CheckTrue(pl.TextEditing, "点搜索框 ⇒ **进入文本编辑**");
                    foreach (var ch in "impe") pl.TypeChar(ch);
                    CheckText(pl.TextBuffer, "impe", "逐字输入 `impe` ⇒ 缓冲对得上");
                    pl.EndText(true);
                    vis = CollectionWindow.CardsState.VisibleCards();
                    CheckTrue(vis.Count > 0 && vis.Count < all,
                              $"卡名筛 `impe` ⇒ 卡池 {all} → {vis.Count}（**外壳的键盘真的接上了**）");
                    win.ClearCardFilters();
                    Check(win.CardsVisibleCount, all, "再清空 ⇒ 回到 " + all);
                    CheckText(CollectionWindow.CardsState.Filter.Name ?? "", "", "清空把卡名也一起清了");
                    // 🔴 2026-09-23 实拍抓的：**搜索框停在 `impe_` 没回到占位符**（断言当时一条都没报）
                    //    ⇒ 补这一条盯**画面上的字**（`Label.Text`），别只盯状态
                    var nf = FindChild(FindChild(fltPanel, "Name Filter"), "Input Text");
                    CheckText(TextOf(nf), "Search",
                              "清空后搜索框回到占位符 `Search`（**实拍抓出来的那条**：光看状态量不到）");
                }

                // 🔴 层序：筛选栏**必须盖在卡池之上**（原版兄弟序 `Collection Display` 在前、`Card Filters` 在后）
                var fbg = FindChild(fltPanel, "Panel");
                var fq = fbg != null ? fbg.GetComponentInChildren<ImageQuad>() : null;
                if (fq != null && win.CardsCells.Count > 0)
                {
                    var mrs = win.CardsCells[0].GetComponentsInChildren<MeshRenderer>(true);
                    int maxQ = int.MinValue;
                    foreach (var mr in mrs)
                    {
                        if (mr.sharedMaterial == null) continue;
                        maxQ = Mathf.Max(maxQ, mr.sharedMaterial.renderQueue);
                    }
                    CheckTrue(maxQ >= 0 && fq.RenderQueue > maxQ,
                              $"筛选栏底板队列 **{fq.RenderQueue}** > 卡池最高层 **{maxQ}**"
                              + " —— 「面板被卡池盖住」那类 bug 的判据（矩形断言量不到它）");
                }
                Shoot("03_收藏_Cards_筛选栏.png");
                win.ToggleFilters();
                CheckTrue(!fltPanel.gameObject.activeSelf, "再点一次 `Filters` ⇒ 面板收起");
            }

            // ---------------- 🆕 2026-10-04（§三第29条 A11）：筛选栏的**滑入/滑出** ----------------
            //   判据（原文 → `资料/待办判据_卡面卡池与双语.md` §四 那条操作链 + `卡组编辑界面_查证_0920.md:434`）：
            //     `Filter Toggle` → `CollectionDisplay.OnEnable → ToggleFilters(bool)`
            //     → `CollectionFilterController.Toggle(bool,bool)` → `DOTween.Kill` +
            //       **`DOAnchorPosX(rect, x, 0.3)`** + `SetActive`：收起 x = `hiddenPosition.x` = **−550**、
            //       展开 x = `originalAnchorPosition.x` = **−165**（**两个都是父系里的 `anchoredPosition`**）
            //       ⇒ **行程 = −385px**（🆕 2026-10-04 订正，X3 审查的 R5 —— 原来我们按 −550 走，多 43%）；
            //       ⚠️ **只动 x**（`anchoredPosition = (hiddenPosition.x, originalAnchorPosition.y)`）。
            //   ⚠️ 批处理**没有帧循环**（CLAUDE.md §二）⇒ `Update` 一次都不跑、`Toggle*` **直接到位**；
            //      动画本身由**确定性口**复验（`TickDrawers` / `SetDrawerProgressForTest`）——
            //      两条路走的是**同一个** `ApplyDrawerSlide`（不是「自检走一份、跑起来走另一份」）。
            //   🔴 **真红法（先说清把哪一行改坏它会红）** —— 2026-10-04 逐条推演订正过
            //     （X3 审查的 R12：原来 5 条里有 2 条说错）：
            //     · `ApplyDrawerSlide` 里 `lp.x += LayoutSpace.Px(FltHiddenDx) * (1 − p.Slide)` 那句**删掉**
            //       ⇒ 只红**两条**：「挪了半个行程」与「还差 10%」—— 而「回到原位」那条**照样绿**
            //       （它量的是「回到了 `BasePos`」，删掉那句它**恒**在 `BasePos`）。
            //     · 那句里把 **`LayoutSpace.Px(...)` 去掉**（= 同步点实跑抓到的那条 **R1**：px 裸进世界单位）
            //       ⇒ 上面那两条红（275→29700、55→5940）**而且**「★ 这一格此刻还在屏内」也红
            //       （实测量到 −5876 —— 那一条正是「点不到」的前提，前提塌了「点不到」就没意义）。
            //     · 行程改回 −550（行程偏 43%）⇒ 上面两条也红（它们把**轨迹**钉住了，不只是「回没回原位」）。
            //     · `StepDrawer` 里 `dt / FltAnimTime` 改成 `dt` ⇒ 「推进 0.15s ⇒ 进度 0.5」红。
            //     · `SetDrawerInteractive` 里 `WindowButton.enabled` 那一行删掉 ⇒ 红的是 **②「点不到」**
            //       （那一行是**唯一**关这些命中区的地方 —— 建的时候那次 `force: true` 也走它；
            //        位置断言照样全绿，正是「画面看着对、真鼠标还点得到」那类静默 bug）；
            //       而 ③「推到 1 **又点得到**」会**照样绿**（按钮从没被关过）—— 说清是哪一条才有用。
            //     · 「撤登记滚动区」那半份删掉 ⇒ ②的「滚轮也不该被这一列吃掉」红。
            //     · `DrawerSettled` 改成恒 `true` ⇒ 「不参与命中/滚轮」「起点不算到位」「不再吃命中/滚轮」三条红。
            //     · `ToggleFilters` 不调 `StartDrawerSlide`（只翻 `Open`）⇒ 「点一次 ⇒ 进度 1」与
            //       「到位了 ⇒ 命中/滚轮生效」红。
            //     · `BuildDeckFilterDrawer` 收尾那次 `force: true` 退回 `force: false`（= X3 审查的 **R4**）
            //       ⇒ Deck 页那三条数 `WindowButton.enabled` 的断言红（第 1 条最直接）。
            Section("筛选栏的滑入/滑出：位移 + 0.3 秒 + 位移期间命中/滚轮失效（A11）");
            {
                var pl3 = PointerLayer.Instance;
                CheckTrue(pl3 != null, "`PointerLayer` 在（下面两条要拿它问「真鼠标点不点得到」）");
                // ⚠️ **前置：把前几段留下的弹窗收干净** —— ② 那两条走的是**真命中路**（`ButtonAt`），
                //    而 `Deck info Popup` 的 `Warlord Image` 命中区盖着 x∈[−109, 999]、y∈[−34, 1074]（比屏还大），
                //    层又是 `QDIHit = 3123` > 筛选格的 `QFltHit = 3043` ⇒ 留着它，
                //    「点不到 / 点得到」两条量到的都是**它**顶掉的结果（不是筛选格的真值）。
                // 🔴 **2026-10-04（A66）：这笔债已经还清** —— 原来一路开到收工的是 **2 扇**
                //    （`_tmp_view/collection.log:6055`「收掉 2 扇」）：A31 那一段建的四扇里 **`v2`/`v3` 没关**
                //    （上面已补），加上 `Practice Deck` 那一段的 `dp3`（也补了兜底）。
                //    ⇒ 这一段从「只打日志」升成**断言**：走到这里还开着 = **新开的一笔债**，当场红，
                //      别再被下面这次清扫静默盖住（收紧的判据，不是放松）。
                int closedLeftovers = 0;
                foreach (var lw in Object.FindObjectsByType<DeckInfoPopup>(FindObjectsSortMode.None))
                    if (lw != null && lw.CurrentState != WindowState.Closed) { lw.Close(); closedLeftovers++; }
                Check(closedLeftovers, 0,
                      "★ A11 前置：这一路跑完，场上**没有**还开着的 `Deck info Popup`（A66 —— 有 = 又漏关了一扇"
                      + (closedLeftovers == 0 ? "）" : $"：实测 {closedLeftovers} 扇，它层 3123、"
                         + "`Warlord Image` 命中区比屏还大，会顶掉下面两条真命中路）"));
                // 起点：上面那一段刚把它收回去
                CheckNear(win.DrawerSlide(0), 0f, 0.001f, "起点进度 = **0**（整栏停在 `hiddenPosition` 那一头）");
                CheckTrue(!win.DrawerSettled(0), "…不参与命中/滚轮");

                win.ToggleFilters();                                   // 开（批处理里直接到位）
                CheckNear(win.DrawerSlide(0), 1f, 0.001f, "点一次 ⇒ 进度 **1**（原位）");
                CheckTrue(win.DrawerSettled(0), "…到位了 ⇒ 命中/滚轮生效");
                float baseCx = PxOf(fltPanel.position.x);

                // ① 时间推进：0.15 秒 = 半个 0.3 ⇒ 进度正好 0.5（这一步**只有真按 `animationTime` 走**才成立）
                win.SetDrawerProgressForTest(0, 0f);
                CheckTrue(fltPanel.gameObject.activeSelf,
                          "钉在**滑出来的起点**（进度 0 · 目标 1）⇒ 节点**还活着** —— 原版也是先 `SetActive(true)` 再动 tween，"
                          + "不然滑出来的那 0.3 秒根本看不见东西（**滑完**那一下才关，见本段末尾）");
                CheckTrue(!win.DrawerSettled(0), "…起点当然不算到位 ⇒ 命中/滚轮仍失效");
                win.TickDrawers(0.15f);
                CheckNear(win.DrawerSlide(0), 0.5f, 0.01f, "推进 **0.15 秒**（`animationTime` = 0.3）⇒ 进度 **0.5**");
                CheckTrue(fltPanel.gameObject.activeSelf, "…滑动途中整栏**活着**（要看得见它在滑）");
                float midCx = PxOf(fltPanel.position.x);
                CheckNear(baseCx - midCx, 385f * 0.5f, 1f,
                          "…而且**真的挪了半个行程**（原位 → 左移 **192.5px** = 385 × 0.5，"
                          + "行程 = 原版 `−550 −(−165)`）");

                // ② 位移没停稳 ⇒ 命中区与滚轮都失效（**这一条量的是「真鼠标路」，不是我们自己那个布尔**）
                win.SetDrawerProgressForTest(0, 0.9f);
                CheckNear(baseCx - PxOf(fltPanel.position.x), 385f * 0.1f, 1f,
                          "进度 0.9 ⇒ 离原位还差 **10%**（左移 **38.5px**；位移 = `行程 × (1 − 进度)`、只动 x）");
                var rc = FindChild(fltPanel, "Cell_rar_common");
                var rcq = rc != null ? rc.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(rcq != null, "拿得到一格（否则下面两条等于没验）");
                if (rcq != null)
                {
                    float cx = PxOf(rcq.transform.position.x), cy = PxYOf(rcq.transform.position.y);
                    // ★ 前置：这一格**现在还在屏内** —— 否则「点不到」是因为它滑出屏外、不是命中失效（等于没验）
                    CheckTrue(cx > 0f && cx < 1920f && cy > 0f && cy < 1080f,
                              $"★ 这一格此刻**还在屏内**（{cx:F0},{cy:F0}）—— 这条是下面「点不到」那个断言的前提");
                    var wbMid = pl3 != null ? pl3.ButtonAt(cx, cy) : null;
                    CheckTrue(wbMid == null,
                              "★ 位移没停稳 ⇒ **真命中路点不到它**（`PointerLayer.CollectHits` 只挑 "
                              + "`WindowButton.isActiveAndEnabled`，`Shell/PointerLayer.cs:488`）—— 拿到 "
                              + (wbMid == null ? "**null** ✓" : "`" + wbMid.name + "` = **还点得到**"));
                    var sMid = pl3 != null ? pl3.ScrollUnder(cx, cy) : null;
                    CheckTrue(sMid != win.FilterScroll,
                              "★ …滚轮也不该被这一列吃掉（`HitScroll` 那条 `Owner.activeInHierarchy` 的语义；"
                              + "拿到 " + (sMid == null ? "**null** ✓" : "`" + sMid.GetType().Name + "`"));
                }

                // ③ 推到目标 ⇒ 回原位、命中恢复（**同一格、同一条真命中路** —— 与 ② 正好两态）
                win.TickDrawers(0.2f);
                CheckNear(win.DrawerSlide(0), 1f, 0.001f, "再推 0.2 秒 ⇒ 收尾到 **1**（`MoveTowards` 夹住）");
                CheckNear(PxOf(fltPanel.position.x), baseCx, 0.5f, "…回到原位（位移是加在 `BasePos` 上的）");
                CheckTrue(win.DrawerSettled(0), "…到位 ⇒ 命中/滚轮**恢复**");
                if (rcq != null)
                {
                    var wbBack = pl3 != null
                        ? pl3.ButtonAt(PxOf(rcq.transform.position.x), PxYOf(rcq.transform.position.y)) : null;
                    CheckTrue(wbBack != null && rc != null && wbBack.transform == FindChild(rc, "Hit"),
                              "★ 同一格现在**又点得到了**（拿到 " + (wbBack == null ? "**null**" : "`" + wbBack.name + "`") + "）");
                }

                // 收尾：还它一个「收着」的状态（后面那些断言/截图按这个来）
                win.ToggleFilters();
                CheckNear(win.DrawerSlide(0), 0f, 0.001f, "再点一次 ⇒ 进度回 **0**（整栏滑出去）");
                CheckTrue(!fltPanel.gameObject.activeSelf, "…滑完就整块关掉（省渲染；原版那套也配 `SetActive`）");
                CheckTrue(!win.DrawerSettled(0), "…并且不再吃命中/滚轮");
                // 「收起」的语义 = **整栏真的在屏幕左外**（不是停在原位隐身）——
                //   判据要的是这一条，不是某个定点（原版那个 −550 是**父系里的绝对锚点值**；
                //   我们的**行程** = `hiddenPosition.x − originalAnchorPosition.x` = −385px，见 `ApplyDrawerSlide`）
                CheckTrue(PxOf(fltPanel.position.x) + CollectionWindow.FltView.W * 0.5f < 0f,
                          "…而且此刻**整栏都在屏幕左外**（右缘 "
                          + (PxOf(fltPanel.position.x) + CollectionWindow.FltView.W * 0.5f).ToString("F1")
                          + " < 0）—— 这才叫「滑出去」"
                          + "（行程 −385 时右缘 = −49.4；⚠️ 行程若再缩小到 < 335 这条就红）");
            }

            // 🔴 层序：**窗口底图必须在页内容之下** —— `CardView` 的各层都落在默认队列 **3000**
            //    （全工程只有 SDF 那层显式设过 3000），而基类给 `Background` 的是 **3005**
            //    ⇒ 实拍抓到过：**整片卡池被底图盖住、画面全空，而所有矩形断言全绿**。
            //    判据只有这一条（量队列，量不到「谁盖谁」）。
            {
                var bgN = FindChild(root, "Background");
                var bq = bgN != null ? bgN.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(bq != null && bq.RenderQueue < 3000,
                          $"窗口底图的队列 = **{bq?.RenderQueue}**，必须 **< 3000**（`CardView` 的层都在 3000）"
                          + " —— 「底图盖住卡池」那个 bug 的判据");
                if (bq != null && win.CardsCells.Count > 0)
                {
                    // ⚠️ **`CardView` 的层不是 `ImageQuad`**（那套是卡面渲染器，走**自定义 mesh + MeshRenderer**）
                    //    ⇒ 队列要量**材质**的 `renderQueue`（渲染真值），量 `ImageQuad` 会得到空数组。
                    var mrs = win.CardsCells[0].GetComponentsInChildren<MeshRenderer>(true);
                    int minQ = int.MaxValue, cnt = 0; string qlist = "";
                    foreach (var mr in mrs)
                    {
                        if (mr.sharedMaterial == null) continue;      // **没材质的渲染器不画东西**，不算层
                        cnt++; minQ = Mathf.Min(minQ, mr.sharedMaterial.renderQueue);
                        qlist += mr.sharedMaterial.renderQueue + ",";
                    }
                    CheckTrue(cnt > 0 && minQ > bq.RenderQueue,
                              $"卡池的格**每一层**都在底图之上（底图 {bq.RenderQueue} · 格内最低队列 {minQ}；"
                              + $"{cnt} 层，实测 [{qlist}]）");
                }
            }

            // ---- `Empty Collection Warning`（§三 第 15 条 **第 50 行**；2026-09-24 补建）----
            // 判据 = 原版 `CollectionDisplay.RefreshCollection`：**过滤后为空** ⇒ `SetActive(true)`
            //        （反汇编 `工具/disasm_va.py` 读 `0x1815ECC5B`）。
            // 坐标 = **135.22,70.94 → 1970.01,1080** —— **比父还宽、左右都溢出**，原版就这样，别"修正"。
            {
                // ⚠️ **必须限定在 Cards 页里找** —— 四个页**各有一份**同名 `Empty Collection Warning`
                //    （原版就这样，矩形逐页不同，见 `CollectionWindow.BuildDeckList` 那条注释）。
                //    用 `FindChild(tabsRoot, …)` 会先撞上 **Deck 页**那一份（第一版就栽在这儿，差 15.33px）。
                var ew = FindChild(win.PageRoot(1), "Empty Collection Warning");
                CheckTrue(ew != null, "Cards 页有 `Empty Collection Warning` 这一件（原版**四页各一份**）");
                if (ew != null)
                {
                    CheckAt(ew, 135.22f, 1970.01f, 70.94f, 1080f, "`Empty Collection Warning` 的位置");
                    CheckTrue(!ew.gameObject.activeSelf, "没筛选 ⇒ **不显示**（原版出厂 `act=F`）");
                    // 🔴 **正例与反例必须成对** —— 只验「不显示」的话，「永远不显示」也能过
                    var f = DeckFilter.None;
                    f.Name = "zzz_本地没有这张卡_zzz";
                    win.UiSetCardFilter(f);
                    CheckTrue(ew.gameObject.activeSelf,
                              "筛到一个**空结果** ⇒ **显示**（原版判据 `filteredCollection.Count <= 0`）");
                    win.ClearCardFilters();
                    CheckTrue(!ew.gameObject.activeSelf, "`Clear filters` ⇒ 又关回去（判据跟着数据走）");
                }
            }

            // 两张截图：当前还停在 Cards 页 ⇒ 先拍它，再切回 Decks 拍第一张
            Shoot("02_收藏_Cards.png");
            win.tabButtons.Click(0);
            Shoot("01_收藏_Decks.png");

            // ---------------- Cosmetics 页：233 张卡背（正本 §五；A4 §二 + §2·1）----------------
            Section("Cosmetics 页：卡背网格（列数**算出来的** · 250×405 · 233 张）");
            win.tabButtons.Click(2);
            Check(win.CurrentTab, WindowTabType.CollectionCosmetics, "点第 3 键 ⇒ 切到 **Cosmetics** 页");
            CheckTrue(win.PageRoot(2) != null, "页节点 `Cardback Tab` 在（**卡面文案是 `COSMETICS`**）");
            Check(CollectionWindow.CosmoTotal, 233,
                  "卡背读数 = **233**（`Resources/Art/cardbacks/`；全在**工程外**导进来的，见正本 §七 ③）");
            // 🔴 列数是**算出来的**：A4 §2·1 原来写「`_segments=5` 是定值」⇒ **已就地更正**
            //    （`_controlSegmentSize=1` ⇒ `ConfigureColumnNumber` 按宽度覆盖 `_segments`，证据同 A3 §3·5）
            // 🔴 **2026-09-24 整套订正**：本页原来那组坐标（`168.27,85→1920,1080`、7 列）是
            //    **在 `Content Area` 的局部系里**算的 ⇒ 整页偏 (167.17, 70.94)、列数**多算一列**。
            //    真值（从 `Collection Menu Variant` 根一路走下来）见 `CollectionWindow.CosmoView` 的注释；
            //    根因是 `menu_rect.py` 父链那个坑（`资料/已知的坑.md`）。
            Check(CollectionWindow.CosmoCols, 6,
                  "列数 = **6** = floor(1584.56 ÷ 250)（⚠️ **不是** `_segments=5` —— 那个是死值；"
                  + "也**不是**上一版算的 7 —— 那是拿偏了 167.17px 的视口宽算出来的）");
            var cpage = win.PageRoot(2);
            var cbh = cpage != null ? FindChild(cpage, "Scroll View") : null;
            CheckAt(cbh, 335.44f, 1920.01f, 155.94f, 1080f, "`Scroll View`（卡背视口 **1584.56 × 924.06**）");
            CheckTrue(win.CosmoCells.Count > 0, $"画出了 {win.CosmoCells.Count} 格（视口外的不建 = 那套裁切）");
            if (win.CosmoCells.Count > 0)
            {
                var k0 = win.CosmoCells[0];
                // 内容**整体居中**：pad = (1584.56 − 6×250) / 2 = **42.285** ⇒ 首格中心 335.44+42.285+125
                CheckNear(PxOf(k0.position.x), 502.73f, 0.6f, "第 1 格中心 x = **502.73**（335.44 + pad 42.285 + 250/2）");
                CheckNear(PxYOf(k0.position.y), 358.44f, 0.6f, "第 1 格中心 y = **358.44**（155.94 + 405/2）");
                var art = FindChild(k0, "Cardback");
                CheckNear(Wpx(art), 250f, 2f, "格里的卡背宽 = **250**（原版 `Cardback` 铺满 250×405）");
                CheckNear(Hpx(art), 405f, 2f, "格里的卡背高 = **405**");
                CheckTrue(art != null && art.GetComponentInChildren<ImageQuad>() != null
                          && art.GetComponentInChildren<ImageQuad>().Texture != null,
                          "卡背**真的有贴图**（不是空图 —— 导没导错就看这一条）");

                // ---- 🆕 2026-09-26：卡背底下那层 **SDF**（原版 `Cardback Shadow SDF`）----
                // 逐值出处：`资料/普查产出_0923/A4_装饰页与驱动链.md:91` ——
                //   rect **-42.5,-70.87,337.5,550.8**（格式是 `x,y,w,h`，y 向下相对格左上）·
                //   锚点 (-0.17,-0.185)-(1.18,1.175) + sizeDelta (0,0)（拉伸）· `act=T` ·
                //   组件 = Image + `UIImageMaterialColorChanger`（换色即悬停高亮）。
                var sdf = FindChild(k0, "Cardback Shadow SDF");
                CheckTrue(sdf != null, "卡背格里有 **`Cardback Shadow SDF`** 那一层（原版两层的底那层）");
                if (sdf != null)
                {
                    CheckNear(Wpx(sdf), 337.5f, 2f, "SDF 层宽 = **337.5**（比 250 的卡背大一圈 —— 露出来的就是落地感）");
                    CheckNear(Hpx(sdf), 550.8f, 2f, "SDF 层高 = **550.8**（`-42.5,-70.87,337.5,550.8`，格式是 x,y,w,h）");
                    var qi = sdf.GetComponentInChildren<ImageQuad>();
                    CheckTrue(qi != null && qi.Texture != null
                              && qi.Texture.name.EndsWith("_sdf"),
                              "SDF 层贴的是**这张卡背自己的 `_SDF` 掩码**"
                            + "（`CosmeticItemCardback.GetCardBackSprites()` 成对返回；100×130.5 是距离场的本意）");
                    CheckTrue(qi != null && qi.GetComponent<MeshRenderer>().sharedMaterial != null
                              && qi.GetComponent<MeshRenderer>().sharedMaterial.shader != null
                              && qi.GetComponent<MeshRenderer>().sharedMaterial.shader.name
                                 == "Everguild/FX/Card Highlight And Shadow",
                              "SDF 层用的是**原版 shader** `Everguild/FX/Card Highlight And Shadow`"
                            + "（材质值照原版 `Card Backs SDF`，不是 shader 默认值 —— 否则会多一圈白框）");
                    // 🔴 **SDF 必须在卡背底下**：两层给的是**两个渲染队列**（同一个队列里谁盖谁不可控，踩过三次）
                    var qArt = art != null ? art.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(qi != null && qArt != null && qi.RenderQueue < qArt.RenderQueue,
                              $"SDF 的渲染队列**低于**卡背（{CollectionWindow.QPageSdf} < {CollectionWindow.QPageRow}）"
                            + "—— 同队列排不出稳定次序");
                }
            }
            // 页头（A3 那条「每页自己的实例值」：本页 35/33，**异画页是 42**）
            CheckText(TextOf(FindChild(cpage, "Header Label")), "Your cosmetics collection",
                      "页头标题 = `Your cosmetics collection`（原版 fs38）");
            CheckText(TextOf(FindChild(cpage, "Filters Label")), "Filters",
                      "页头 `Filters` 文案在（原版 **fs35** auto 10-35 —— ⚠️ 异画页同名的是 42）");
            CheckText(TextOf(FindChild(cpage, "Clear filters Text")), "Clear filters",
                      "页头 `Clear filters` 文案在（原版 **fs33** auto 10-33）");
            // 🔴 **实拍抓的一条**：第一版照抄了 dump 的 `1405`（= **布局组跑之前的模板位**，
            //    父容器 `Header/Filters` 是 425..1385 的 VLG）⇒ **它和右对齐到 1821 的标题叠在一起**。
            //    ⇒ 现在断「按钮在容器内」**并且**「标题与按钮不叠」—— 这类错**矩形断言量不到**，
            //      得**量文字自己的宽度**算边缘（同筛选栏标签那两条）。
            // 🔴 2026-09-24：**x 也从 425 改成 612.2** —— 与 Deck/Cards/Styles 三页**同一个值**
            //    （四页共用同一条 `Header`；理由见 `CollectionWindow.BuildFilterHeader` 的注释）。
            {
                var bq = FindChild(cpage, "Clear filters");
                CheckTrue(bq != null && Mathf.Abs(PxOf(bq.position.x) - 737.17f) <= 1f,
                          "`Clear filters` 底图中心 x = **737.17**（= 612.17 + 250/2 —— 与另外三页同值）"
                          + " —— **别照抄 dump 里的 1405 / 1488.59**（那是布局组跑之前的模板位）");
                var ttlT = FindChild(cpage, "Header Label");
                var clrT = FindChild(cpage, "Clear filters Text");
                var lt = ttlT != null ? ttlT.GetComponent<Label>() : null;
                var lc2 = clrT != null ? clrT.GetComponent<Label>() : null;
                if (lt != null && lc2 != null)
                {
                    float tLeft = PxOf(lt.transform.position.x) - lt.WorldW * 54f;
                    float cRight = PxOf(lc2.transform.position.x) + lc2.WorldW * 54f;
                    CheckTrue(tLeft > cRight,
                              $"标题（左缘 **{tLeft:F0}**，右对齐到 1821）与 `Clear filters`（右缘 **{cRight:F0}**）**不叠**");
                }
            }
            // 左抽屉：**出厂 act=F**，靠 `Filter Toggle` 开合
            CheckTrue(!win.CosmoFiltersOpen, "筛选抽屉 `Cosmetic FIlter` **起手是收起的**（实证 act=F）");
            var cHit = FindChild(cpage, "FiltersHit");
            var cBtn = cHit != null ? cHit.GetComponent<WindowButton>() : null;
            CheckTrue(cBtn != null, "`Filter Toggle` 有点击区");
            if (cBtn != null)
            {
                cBtn.Click();
                CheckTrue(win.CosmoFiltersOpen, "点 `Filters` ⇒ 抽屉打开");
                cBtn.Click();
                CheckTrue(!win.CosmoFiltersOpen, "再点 ⇒ 收起");
            }
            // ---------------- 🆕 2026-10-03（§三 第 29 条 A11）：卡背页的筛选抽屉（**两行都建了**）----------------
            //   判据 = `FilterPanelModel.BuildCosmetics`（与卡组编辑那扇窗**同一份模型**）
            //        + `CardbackTable.NamesFor`（阵营 → 卡背，**判据只有一份**）。
            //   原来那句「A4 只给了容器 rect、没给格子尺寸 ⇒ 没建」**早就不成立**（2026-09-28 在卡组编辑实测补全）。
            Section("卡背页筛选抽屉：Army 13 格 + Owned（**行序 Army 在前**，与卡牌那套相反）");
            if (cBtn != null)
            {
                cBtn.Click();                       // 再开一次（上一组开合把它关回去了）
                CheckTrue(win.CosmoFiltersOpen, "抽屉开着才量得到格");
                var drw = FindChild(cpage, "Cosmetic FIlter");
                Check(win.CosmoFilterCellCount, 14,
                      "格子总数 = **14** = Army **13** + `Owned` **1**"
                      + "（⚠️ 这一页**没有**搜索框 / 稀有度 / 费用 / 类型 —— 原版那棵树里就没有）");

                var cf = CollectionWindow.CosmoState.Factions();
                CheckTrue(cf.Count > 0, $"阵营表有 {cf.Count} 个（按它铺 Army 格）");
                var ca0 = cf.Count > 0 ? FindChild(drw, "Cell_fac_" + cf[0]) : null;
                CheckTrue(ca0 != null, "Army 第 1 格在（按**名字**找，不按序号）");
                if (ca0 != null)
                {
                    // 面板内 (pad L14, 15+50) ⇒ `Abs` 加 (0.25,155.9) ⇒ 中心 (64.25, 270.9)
                    CheckNear(PxOf(ca0.position.x), 64.25f, 0.6f, "Army 第 1 格中心 x = **64.25**（0.25+14+50）");
                    CheckNear(PxYOf(ca0.position.y), 270.9f, 0.6f,
                              "Army 第 1 格中心 y = **270.9**（155.9+15+50+50）"
                              + " —— 这一页 **Army 在前**（卡牌那套是 Owned/Upgradable 在前）");
                    CheckArt(ca0, DeckRuntime.FactionIcon(cf[0]), "Army 格的图 = 该阵营图标（原版运行期赋）");
                }
                // `Owned` 行：`CosmoOwnedTop(13)` = 15 + 550 + 12.81 = 577.81 ⇒ 绝对行顶 733.71、中心 758.71
                var co = FindChild(drw, "Cell_owned");
                CheckTrue(co != null, "`Owned` 那一格在");
                if (co != null)
                {
                    CheckNear(PxYOf(co.position.y), 758.71f, 0.6f,
                              "`Owned` 行中心 y = **758.71**（155.9 + 577.81 + 25 —— Army 行 550 一高，它跟着往下走）");
                    CheckText(TextOf(FindChild(co, "Label")), "Owned only", "`Owned` 的标签文案");
                    CheckNear(Wpx(FindChild(co, "Background")), 70.59f, 1.5f,
                              "开关底图宽 = **70.59** = 0.3×335.31 − 30（原版那条锚点式子）");
                }

                // ---- **真的筛得动**（重画会重建格 ⇒ 每次点完要**重新找**那个节点）----
                System.Action<string> clickCell = key =>
                {
                    var c = FindChild(FindChild(cpage, "Cosmetic FIlter"),
                                      "Cell_" + key.Replace("$", "").Replace(":", "_"));
                    var h = c != null ? FindChild(c, "Hit") : null;
                    var b = h != null ? h.GetComponent<WindowButton>() : null;
                    CheckTrue(b != null, "格子 `" + key + "` 的点击区在");
                    if (b != null) b.ClickForTest();
                };
                int allN = CollectionWindow.CosmoTotal;
                if (cf.Count > 0)
                {
                    int wantFac = CardbackTable.NamesFor(cf[0], CardArt.CosmeticNames()).Length;
                    clickCell("$fac:" + cf[0]);
                    Check(CollectionWindow.FilteredCosmoNames().Length, wantFac,
                          $"点「{cf[0]}」⇒ 筛出 **{wantFac}** 张（判据 = `CardbackTable.NamesFor` 那一份）");
                    CheckTrue(wantFac > 0 && wantFac < allN,
                              $"…比全部 {allN} 张少 ⇒ **这是真筛**（不是摆设；`Army` 那半本来一直是空的）");
                    // 卡背格也跟着重画了（格数只能是**变少**）
                    CheckTrue(win.CosmoCells.Count > 0 && win.CosmoCells.Count <= wantFac,
                              $"卡背格重画了（{win.CosmoCells.Count} 格 ≤ {wantFac} 张）");
                    clickCell("$fac:" + cf[0]);        // 再点一次 = 取消（`FilterPanelModel.Click` 的语义）
                    Check(CollectionWindow.FilteredCosmoNames().Length, allN, "再点一次 ⇒ 取消阵营筛选，回到全部");
                }
                // `Owned only`：单机全解锁 ⇒ **切得动但不改变结果**（如实标的差异，不是静默失效）
                bool ownedBefore = CollectionWindow.CosmoState.Filter.Owned;
                clickCell("$owned");
                Check(CollectionWindow.CosmoState.Filter.Owned, !ownedBefore, "`Owned only` 那个开关切得动");
                Check(CollectionWindow.FilteredCosmoNames().Length, allN,
                      "…但**结果不变**（单机全解锁 —— 与卡组编辑那扇窗同一条如实标注）");
                clickCell("$owned");
                // `Clear filters`：回到全部
                if (cf.Count > 0) clickCell("$fac:" + cf[0]);
                win.ClearCosmoFilters();
                Check(CollectionWindow.FilteredCosmoNames().Length, allN, "`Clear filters` ⇒ 回到全部 233 张");
                CheckTrue(CollectionWindow.CosmoState.Filter.Faction == null
                          || CollectionWindow.CosmoState.Filter.Faction.Length == 0, "…阵营条件真的清掉了");

                Shoot("05_收藏_卡背筛选抽屉.png");
                cBtn.Click();                       // 收回去（下一张实拍要的是收起态）
                CheckTrue(!win.CosmoFiltersOpen, "量完收回去");
            }

            if (win.CosmoScroll != null && win.CosmoScroll.MaxOffset > 0f)
            {
                win.CosmoScroll.ScrollBy(win.CosmoScroll.MaxOffset);
                int last = CollectionWindow.CosmoTotal - 1;
                var lc = FindChild(cpage, "CollectionCosmetic_" + last);
                CheckTrue(lc != null && PxYOf(lc.position.y) + 405f * 0.5f <= 1080.5f,
                          $"**滚到底 ⇒ 第 {last + 1} 张（最后一张卡背）完整落进视口**");
                win.CosmoScroll.ScrollBy(-win.CosmoScroll.MaxOffset);
            }
            Shoot("04_收藏_Cosmetics.png");

            // `Empty Collection Warning`（**Cosmetics 页那一份**；矩形与别页又不同）
            {
                var ew = FindChild(win.PageRoot(2), "Empty Collection Warning");
                CheckTrue(ew != null, "Cosmetics 页有 `Empty Collection Warning`");
                if (ew != null)
                {
                    CheckAt(ew, 170.44f, 1970.00f, 70.94f, 1080f, "Cosmetics 页 `Empty Collection Warning` 的位置");
                    CheckTrue(!ew.gameObject.activeSelf,
                              $"233 张卡背 ⇒ 不显示（🆕 2026-10-03：抽屉两行齐了 ⇒ 这条判据**真能触发**；"
                              + "13 个阵营各 9~20 张 ⇒ 正常筛不空）");
                }
            }

            // ============================================================ 卡片详情窗（2026-09-24）
            // 出处：`资料/阶段二_卡片详情窗_原版规格.md`（逐节点表 + 三块面板 + 计数条）。
            Section("卡片详情窗（点一张卡开 · 原版菜单版 `CardDisplayWindow`）");
            win.tabButtons.Click(1);                       // 回 Cards 页
            {
                var cp = win.PageRoot(1);
                var hit0 = cp != null ? FindChild(cp, "CardHit_0") : null;
                var hb0 = hit0 != null ? hit0.GetComponent<WindowButton>() : null;
                CheckTrue(hb0 != null, "卡池第 1 格有点击区");
                if (hb0 != null)
                {
                    hb0.Click();
                    var cd = CollectionWindow.LastCardDetail;
                    CheckTrue(cd != null, "点第 1 格 ⇒ **开出了卡片详情窗**");
                    if (cd != null)
                    {
                        var disp = FindChild(cd.transform, "Card Display");
                        CheckAt(disp, 584f, 1336f, 106f, 974f, "`Card Display`（752 × 868）");
                        // 卡本体尺寸：量**渲出来的**网格包围盒（原版 = 523.25 × 832.75）
                        // 🔴 **只能量「卡框」那一层** —— 整棵子的包围盒会被**立绘抠图的溢出**撑大
                        //    （实测 1106.9px，是「角色越出卡框」那一层的功劳，第一版就这么误报的）。
                        //    卡框那层的贴图名以 **`frame_`** 开头（`CardArt.Frame(faction, rarity)` 给的，
                        //    落在 `Art/cards/frame_<阵营>[_strat]_tier<N>`）；
                        //    🔴 **别用 `_tier` 去找** —— SDF 那层叫 `<阵营>_tier<N>`（在 `Art/card_sdf/`），
                        //    也带 `_tier`，而且它是「软光/影」那张 **4.4281² = 1106.9px** 的方图（实测踩过）。
                        var mrs = disp != null ? disp.GetComponentsInChildren<MeshRenderer>(true) : null;
                        if (mrs != null && mrs.Length > 0)
                        {
                            Bounds? frameB = null;
                            foreach (var mr in mrs)
                            {
                                var tx = mr.sharedMaterial != null ? mr.sharedMaterial.mainTexture : null;
                                if (tx != null && tx.name.StartsWith("frame_")) { frameB = mr.bounds; break; }
                            }
                            CheckTrue(frameB.HasValue, $"找到了卡框那一层（{mrs.Length} 层网格里贴图名以 `frame_` 开头的那个）");
                            if (frameB.HasValue)
                            {
                                // 🔴 **2026-09-27 用户拍板：卡框改成【按固定矩形画】**（原来是「裁到 bbox 再等比内接」——
                                //    **那个设计是错的**）。硬证据：原版 `2DCard/CardFrame` 那个 Image 的
                                //    **`m_PreserveAspect = 0`** ⇒ 原版就是把框贴图**拉伸**进这个固定矩形的。
                                //    改之前：四档画形不同（bbox w/h：tier1/2 = 0.672、tier3/4 = 0.733，而矩形是 0.6893）
                                //    ⇒ 一律用 tier4 会让每张卡的框**矮 5.7%**（768.2）；改完**四档都是 814.25**（= 矩形本身）。
                                CheckNear(frameB.Value.size.y * 108f, 814.25f, 6f,
                                          $"**卡框渲出来的高 = {frameB.Value.size.y * 108f:F1}px**（原版 `CardFrame` = 561.25 × **814.25**；"
                                        + "按固定矩形画 ⇒ **与卡框档无关**）");
                                // ⚠️ **宽对不上，别拿它当判据**：我量到 535.19px、原版 `CardFrame` 是 561.25 —— 差 ~26px。
                                //    **还没查清**（疑 `CardView` 那层按贴图自身宽高比画、而原版 `CardFrame` 的 rect
                                //    比贴图比例宽）。已记进 `项目任务.md` §三 第 15 条，**不在本轮擅自改卡面**。
                                Debug.Log(P + $"   · 卡框宽实测 {frameB.Value.size.x * 108f:F1}px（原版 `CardFrame` 561.25 —— 差 "
                                          + $"{(frameB.Value.size.x * 108f - 561.25f):F1}px，**还没查清**，见 §三 第 15 条）");
                            }
                            CheckTrue(mrs.Length > 3, $"卡面上画了 {mrs.Length} 层网格（不是空卡位）");
                        }
                        // 三块面板的标题 —— 🔴 **2026-09-27：创建副本 / 升级两块【都不建】**（用户拍板，见 §三 第 21 条）
                        //   ⚠️ 原来这里断言的是这两块的**标题** —— 用户 2026-09-27 定了「不做升级、不做合成」
                        //     ⇒ 那两条判据**没有对象了**，改成「这两块根本不在」（`TitleOf` 查不到会返回 `(无)`）。
                        CheckText(cd.TitleOf("Crafting"), "(无)",
                                  "「创建副本」那块**不建**（本作不做合成 —— 用户 2026-09-27 拍板）");
                        CheckText(cd.TitleOf("Upgrade"), "(无)",
                                  "「升级」那块**不建**（本作不做升级 —— 用户 2026-09-27 拍板）");
                        CheckText(cd.TitleOf("AltArt"), "Alternate art",
                                  "异画面板标题 —— ⚠️ 原版这一格印的是**升级文案**（复制粘贴 bug），**我们不抄那个 bug**（出声）");
                        // 计数条：格式 = `x{min(拥有,卡组上限)}` + `"/ "` + `{拥有−该数}`
                        var cnt = cd.Counter;
                        CheckTrue(cnt != null, "`Card Counter` 在");
                        if (cnt != null)
                        {
                            var t1 = FindChild(cnt, "Counter");
                            string s1 = TextOf(t1);
                            CheckTrue(s1.StartsWith("x"),
                                      $"计数条左数 = **{s1}**（原版格式串 `\"x{{0}}\"`，= min(拥有, 卡组上限)）");
                            CheckText(TextOf(FindChild(cnt, "Slash")), "/ ",
                                      "中间那个 `/ ` **是写死的**（原版代码不改它）");
                            var t2 = FindChild(cnt, "Duplicates text");
                            CheckTrue(t2 != null && int.TryParse(TextOf(t2), out _),
                                      $"右数 = **{TextOf(t2)}**（= 多余副本数；> 0 ⇒ 走 `Duplicate Counter` 那一支）");
                        }
                        // 三块面板的动作：**创建副本 +1** / **升级 +1 级**（单机口径：不扣货币）
                        int cap = CardProgress.DeckCap(cd.Card.Rarity);
                        int owned0 = CardProgress.Owned(cd.Card.Id, cd.Card.Rarity);
                        var ch = cd.CraftHit; var chb = ch != null ? ch.GetComponent<WindowButton>() : null;
                        if (chb != null)
                        {
                            chb.Click();
                            Check(CardProgress.Owned(cd.Card.Id, cd.Card.Rarity), owned0 + 1,
                                  $"点 `Craft` ⇒ 拥有数 {owned0} → **{owned0 + 1}**（单机**不扣万能卡**，出声）");
                        }
                        int lv0 = CardProgress.Level(cd.Card.Id);
                        var uh = cd.UpgradeHit; var uhb = uh != null ? uh.GetComponent<WindowButton>() : null;
                        if (uhb != null)
                        {
                            uhb.Click();
                            Check(CardProgress.Level(cd.Card.Id), lv0 + 1, $"点 `Upgrade` ⇒ 等级 {lv0} → **{lv0 + 1}**");
                        }
                        // `Show Card Text` 切效果文字条；语音钮「没有就出声」
                        var ehit = cd.EyeHit; var ehb = ehit != null ? ehit.GetComponent<WindowButton>() : null;
                        if (ehb != null && cd.LoreVisible)
                        {
                            ehb.Click();
                            CheckTrue(!cd.LoreVisible, "点 `Show Card Text` ⇒ 效果文字条**藏起来**");
                            ehb.Click();
                            CheckTrue(cd.LoreVisible, "再点 ⇒ 显示回来");
                        }
                        // ================================================================
                        //  🆕 2026-09-27：「相关卡」那一块（**1 主卡 + 8 相关卡 = 9 格** · 扇形 · 换位）
                        //  判据 → 正本 **§8·7**（扇形真值 = 原版那条 legacy clip `Card Display Open`）
                        //  与 **§9·6**（换位：三个闸 · 0.25s 六条 tween · `SetSiblingIndex` 两两互换 ·
                        //  收尾重设 lore/语音）。用户口径 → `项目任务.md` §三 第 19 条。
                        //  ⚠️ **槽 0–4 是原版真值，槽 5–8 是我们外推的**（原版只有 5 格）—— 下面分开钉。
                        // ================================================================
                        {
                            // 量「卡框」那一层的渲染包围盒（同上面那条注释：整棵子会被立绘溢出撑大）
                            Bounds? FB(CardView v)
                            {
                                if (v == null) return null;
                                foreach (var mr in v.GetComponentsInChildren<MeshRenderer>(true))
                                {
                                    var tx = mr.sharedMaterial != null ? mr.sharedMaterial.mainTexture : null;
                                    if (tx != null && tx.name.StartsWith("frame_")) return mr.bounds;
                                }
                                return null;
                            }

                            CardDef poc = null, sgDef = null, moa = null;
                            foreach (var c in CardDatabase.Load())
                            {
                                if (c.Name == "Path of Command") poc = c;
                                if (c.Name == "Storm Guardian") sgDef = c;
                                if (c.Name == "Master of Arcana") moa = c;
                            }
                            CheckTrue(poc != null && sgDef != null,
                                      "（前提）卡池里有 `Path of Command` 与 `Storm Guardian`（用户举的那个例子）");
                            if (poc != null && sgDef != null)
                            {
                                cd.ShowCard(poc);      // 复用同一扇窗换一张卡（原版 `ShowCard` 就是这个意思）
                                CheckTrue(cd.SlotCount >= 2,
                                          $"`Path of Command` ⇒ 卡片那一叠 **{cd.SlotCount} 格**（主卡 + 相关卡）");
                                int sg = cd.SlotIndexOf(sgDef.Id);
                                CheckTrue(sg > 0,
                                          $"★ **`Storm Guardian` 就在这一叠里（第 {sg} 格）** —— "
                                        + "用户 2026-09-26 举的例子：卡面写 `Deploy a Storm Guardian`，"
                                        + "总不能让玩家不知道那张是什么");

                                // ① 前台那张 = 位姿槽 0：**屏心 (960,480) · 转角 0**（原版 clip 末帧）
                                var s0 = cd.SlotView(0);
                                var b0 = FB(s0);
                                CheckTrue(b0.HasValue, "位姿槽 0（前台）画出来了");
                                // 🔴 **位置断言要用「节点位置」，不能用渲染包围盒的中心**：
                                //    卡框那层（561.25×814.25）与卡本体（523.25×832.75）**本来就不同心**（差 ≈7.5px），
                                //    而且**旋转过的卡 AABB 还会再涨** —— 第一版就是这么误报的（实测 487.5 / 494.0）。
                                //    尺寸才用渲染盒（下面平台那条 `frame_` 高度就是渲染真值）。
                                if (b0.HasValue)
                                {
                                    var c0 = LayoutSpace.ToPixel(s0.transform.position);
                                    CheckNear(c0.x, 960f, 2f, "前台那张**中心 x = 960**（原版 clip 末帧 `anchoredPosition.x = 0`）");
                                    CheckNear(c0.y, 480f, 2f, "…**中心 y = 480**（原版 `anchoredPosition.y = 60` ⇒ 屏幕 y = 540−60）");
                                }

                                // ② 槽 1（第一张相关卡）：**原版真值 (−121,53) · 2.510° · scale 232.954**
                                var s1 = cd.SlotView(1);
                                if (s1 != null && s0 != null)
                                {
                                    var c1 = LayoutSpace.ToPixel(s1.transform.position);
                                    CheckNear(c1.x, 960f - 121f, 2.5f,
                                              "槽 1 中心 x = **839**（原版 clip：`anchoredPosition.x = −121` ⇒ 扇形**朝左开**）");
                                    CheckNear(c1.y, 480f + 7f, 2.5f,
                                              "槽 1 中心 y = **487**（原版 `y = 53` ⇒ 屏幕 y = 540−53，比前台**低 7**）");
                                    CheckNear(s1.transform.eulerAngles.z, 2.510f, 0.05f,
                                              "槽 1 转角 = **2.510°**（原版 clip 末帧的 z 旋转）");
                                    // 🔴 缩放比**别用渲染包围盒比** —— 旋转过的 AABB 会被撑大（实测给 0.96，真值 0.932）
                                    CheckNear(s1.transform.localScale.x / s0.transform.localScale.x, 232.954f / 250f, 0.005f,
                                              "槽 1 缩放 ÷ 前台缩放 = **232.954 / 250**（原版 clip 的 `localScale`）");
                                }

                                // ③ **外推的那几格**（槽 5–8）：只钉走向（更靠左 / 更斜 / 更小）——
                                //    原版没有第 6 格可比，⚠️ 这一段是**我们挑的**（正本 §8·7）。
                                //    ⚠️ 真实卡池里相关卡通常只有 1–2 张（全池 129 处点名摊在 100 来张卡上）
                                //    ⇒ **换一张相关卡够多的**再走这段：`Master of Arcana` 的天赋是
                                //    `Choose an Ultramarines Psychic Power…`（**池子 4 张**）⇒ 至少 5 格。
                                {
                                    CheckTrue(moa != null, "（前提）卡池里有 `Master of Arcana`");
                                    if (moa != null)
                                    {
                                        cd.ShowCard(moa);
                                        CheckTrue(cd.SlotCount >= 5,
                                                  $"`Master of Arcana`（天赋是个 **4 张的池子**）⇒ 卡片那一叠 {cd.SlotCount} 格");
                                        int inPool = 0;
                                        foreach (var c in CardDatabase.Load())
                                            if (c.Faction == "Ultramarines" && c.Subtype == "Psychic Power"
                                                && cd.SlotIndexOf(c.Id) > 0) inPool++;
                                        CheckTrue(inPool >= 3,
                                                  $"★ **池子里的卡真列进相关卡了**（{inPool} 张）—— "
                                                + "相关卡来源②：天赋是个池子 ⇒ 池里那几张跟出来（判据 → 正本 §九）");
                                        int pairs = 0;
                                        for (int i = 2; i < cd.SlotCount; i++)
                                        {
                                            var sa = cd.SlotView(i - 1); var sb = cd.SlotView(i);
                                            if (sa == null || sb == null) continue;
                                            pairs++;
                                            CheckTrue(LayoutSpace.ToPixel(sb.transform.position).x
                                                       < LayoutSpace.ToPixel(sa.transform.position).x - 10f,
                                                      $"槽 {i} 比槽 {i - 1} **更靠左**（扇形继续张开）");
                                            CheckTrue(sb.transform.localScale.x < sa.transform.localScale.x,
                                                      $"槽 {i} 比槽 {i - 1} **更小**（原版就是越远越小）");
                                            CheckTrue(sb.transform.eulerAngles.z > sa.transform.eulerAngles.z,
                                                      $"槽 {i} 比槽 {i - 1} **更斜**（原版就是越远越斜）");
                                        }
                                        CheckTrue(pairs >= 3, $"比得出至少 3 对相邻卡位（实得 {pairs} 对）");
                                        // 🔴 分层：**每格一个独立队列、越靠前台号越大** ——
                                        //    不然「谁盖谁」只剩「到相机的距离」在排，而那是**不可控**的
                                        //    （实测第一版：后面那张的**卡名画到了前面那张的立绘之上**）。
                                        {
                                            int prevQ = int.MaxValue; bool mono = true;
                                            for (int i = 0; i < cd.SlotCount; i++)
                                            {
                                                var sv = cd.SlotView(i);
                                                if (sv == null) continue;
                                                int q = -1;
                                                foreach (var mr in sv.GetComponentsInChildren<MeshRenderer>(true))
                                                    if (mr.sharedMaterial != null) { q = mr.sharedMaterial.renderQueue; break; }
                                                if (q >= prevQ) mono = false;
                                                prevQ = q;
                                            }
                                            CheckTrue(mono,
                                                      "★ **每格的渲染队列逐格递减**（前台最高）—— 分层靠队列，不靠距离"
                                                    + "（`资料/已知的坑.md`：同队列的两层谁盖谁不可控）");
                                        }
                                        cd.ShowCard(poc);        // 换回来 —— 下面第 ④ 段要在 `Path of Command` 这叠上验换位
                                    }
                                }

                                // ④ 换位：点相关卡 ⇒ 它和前台**两两互换**（原版 `ChangeCardPosition`）
                                if (sg > 0)
                                {
                                    var before0 = cd.FrontDef;
                                    var hit1 = FindChild(cd.transform, "CardHit " + sg);
                                    var hb1 = hit1 != null ? hit1.GetComponent<WindowButton>() : null;
                                    CheckTrue(hb1 != null, $"第 {sg} 格有点击区（原版 `AddCardsListeners`：5 个卡位各挂一个）");
                                    var posFront = cd.SlotView(0).transform.position;
                                    var posOther = cd.SlotView(sg).transform.position;
                                    // 🔴 批处理没有帧循环 ⇒ 补间要**手动推进**（同 `BattleScene` 那几处）
                                    CardTween.Mode = DG.Tweening.UpdateType.Manual;
                                    if (hb1 != null) hb1.Click();
                                    CheckTrue(cd.IsSwapping, "点了相关卡 ⇒ 换位在播（原版 `swappingCards` 闸置上）");
                                    // 播完之后再点一次 ⇒ **该被闸①挡掉**（原版：上一次没播完什么都不做）
                                    if (hb1 != null) hb1.Click();
                                    CheckTrue(cd.FrontDef == before0,
                                              "★ 换位播到一半再点 ⇒ **什么都不做**（原版闸① `swappingCards`）");
                                    CardTween.Advance(0.3f);          // 0.25s 那条 tween 走完
                                    CheckTrue(!cd.IsSwapping, "0.25s 之后换位收尾（开闸）");
                                    CheckTrue(cd.FrontDef != null && cd.FrontDef.Id == sgDef.Id,
                                              $"★ **被点那张换到了前台**（现在是「{cd.FrontDef.Name}」）—— 原版「点谁就把谁换到前面」");
                                    CheckNear(Vector3.Distance(cd.SlotView(0).transform.position, posFront), 0f, 0.01f,
                                              "★ 被点那张现在站在**原来的前台位**（两两互换，不是「把谁提到最前」）");
                                    CheckNear(Vector3.Distance(cd.SlotView(sg).transform.position, posOther), 0f, 0.01f,
                                              "★ 原来那张前台让到了**被点卡的槽位**（同上）");
                                    // 闸②：点前台自己 ⇒ 什么都不做（也别让这一下落到遮罩上把窗关掉）
                                    int hitFront = 0;
                                    var hf = FindChild(cd.transform, "CardHit " + hitFront);
                                    var hfb = hf != null ? hf.GetComponent<WindowButton>() : null;
                                    CheckTrue(hfb != null, "**前台那格也有点击区**（不然点它会落到遮罩上**把窗关掉**）");
                                    if (hfb != null) hfb.Click();
                                    CheckTrue(cd.CurrentState != WindowState.Closed,
                                              "★ 点前台那张 ⇒ **窗不关、也不换位**（原版闸②）");
                                    CheckTrue(cd.FrontDef.Id == sgDef.Id, "…而且前台还是刚换上去那张（没被点回去）");
                                    // 拍照前换回**格子最多**的那张（5 格）—— 截图是拿来**看扇形**的
                                    if (moa != null) cd.ShowCard(moa);
                                }
                            }
                        }
                        // ⑤ **着色 / 分层** —— 2026-09-28 按用户给的实拍（《点击卡片查看详情的参考.png》）
                        //    订正的两条（判据全文 → `资料/阶段二_卡片详情窗_原版规格.md` §十·2 / §十·5）
                        {
                            var s0 = cd.SlotView(0);
                            var sN = cd.SlotView(cd.SlotCount - 1);
                            CheckTrue(s0 != null, "（前提）前台那张在");
                            CheckNear(s0 != null ? s0.Tint.r : -1f, 1f, 0.01f,
                                      "★ 前台那张**不压暗**（原版前台色 = (1,1,1,1)）");
                            CheckTrue(cd.SlotCount < 2 || sN.Tint.r < 0.99f,
                                      "★ 相关卡**压暗**（原版 `cardInBackGroundColorTint` = 0.65）");
                            if (cd.SlotCount >= 2)
                                CheckNear(sN.Tint.r, 0.65f, 0.01f, "…而且是 **0.65**，不是我们随手取的");
                            // 🔴 遮罩必须在**卡格之下**：原来卡格 3009–3017、遮罩 3110 ⇒ **整叠卡被压暗一半**
                            var shadeNode = FindChild(cd.transform, "Menu Dark Background");
                            var shadeQuad = shadeNode != null ? shadeNode.GetComponentInChildren<ImageQuad>() : null;
                            int shadeQ = shadeQuad != null ? shadeQuad.RenderQueue : -1;
                            int cardQ = CardQueue(s0);
                            CheckTrue(shadeQ >= 0, $"（前提）遮罩在（队列 {shadeQ}）");
                            CheckTrue(cardQ > shadeQ,
                                      $"★ 卡格队列（{cardQ}）**高于遮罩**（{shadeQ}）—— 卡画在压暗层之上"
                                      + "（原版那棵树里 `Menu Dark Background` 是第一个孩子）");
                            // 风味底图：**按阵营**选图（2026-09-28 刚导进工程的那 13 张）
                            var loreBgNode = FindChild(cd.transform, "FlavourTextBG");
                            var loreImg = loreBgNode != null ? FindChild(loreBgNode, "Image") : null;
                            var loreQuad = loreImg != null ? loreImg.GetComponent<ImageQuad>() : null;
                            CheckTrue(loreQuad != null,
                                      "★ 风味底图建出来了（原版 `FlavourTextSO.GetClanFlavorBackground` 按阵营选）");
                            var frontDef = cd.FrontDef;
                            if (loreQuad != null && frontDef != null)
                                CheckTrue(loreQuad.Texture != null
                                          && loreQuad.Texture.name == "flavourbg_" + frontDef.Faction.ToLowerInvariant(),
                                          $"★ …而且取的是**这个阵营**那张：`{loreQuad.Texture.name}`"
                                          + $"（卡是 {frontDef.Faction}）—— 判据是原版那张 army→资产 表");
                        }
                        cd.PlayVoice();     // 有就播、没有就出声 —— 两种都接受（判据是它**不静默**）
                        Shoot("08_收藏_卡片详情窗.png");
                        var sh = cd.ShadeHit; var shb = sh != null ? sh.GetComponent<WindowButton>() : null;
                        if (shb != null) shb.Click();
                        Check(cd.CurrentState, WindowState.Closed, "点遮罩 ⇒ 窗关上（**原版全树没有关闭钮**，就这一条路 + ESC）");
                    }
                }
            }

            // ============================================================ Styles 页（异画，2026-09-24）
            //
            // 几何出处：`CollectionWindow.StyleView` 那段注释（**从 `Collection Menu Variant` 根走下来实读的**）。
            // 判据一律是**原版数**，不是我们自己的常量（否则是自证）。
            Section("Styles 页：换风格条 + 异画网格（6 列 · 262.5×384 · 本地 7 张 / 2 种风格）");
            win.tabButtons.Click(3);
            Check(win.CurrentTab, WindowTabType.CollectionStyles, "点第 4 键 ⇒ 切到 **Styles** 页");
            CheckTrue(win.PageRoot(3) != null, "页节点 `Alternate Art Tab` 在（**卡面文案是 `STYLES`**）");
            Check(CollectionWindow.AltArtCards.Length, 7,
                  "异画读数 = **7**（本地只有这 7 张督军异画；文件名 `AA_HB_…` / `…_v2`）");
            Check(CollectionWindow.AltStyles.Length, 2,
                  "风格数 = **2**（`AA_HB` 6 张 + `v2` 1 张；其余风格在远端 CCD 的 `alternateartstyles` 包）");
            Check(CollectionWindow.StyleCols, 6,
                  "列数 = **6** = floor(1589.78 ÷ 262.5)（⚠️ 不是 `_segments=4` —— 那是死值）");
            var spage = win.PageRoot(3);
            var svp = spage != null ? FindChild(spage, "Scroll View") : null;
            CheckAt(svp, 330.22f, 1920.01f, 287.67f, 1080f, "`Scroll View`（异画视口 **1589.78 × 792.33**）");
            // 两个换风格圆钮：**按位置**（脚本字段名反着：`leftStyleButton` 挂的是右边那颗）
            var aL = spage != null ? FindChild(spage, "Select Art Button Left") : null;
            var aR = spage != null ? FindChild(spage, "Select Art Button Right") : null;
            CheckNear(aL != null ? PxOf(aL.position.x) : -1f, 720.595f, 1f,
                      "`Select Art Button Left` 中心 x = **720.60**（683.40 + 74.39/2）");
            CheckNear(aR != null ? PxOf(aR.position.x) : -1f, 1357.59f, 1f,
                      "`Select Art Button Right` 中心 x = **1357.59**（1320.40 + 74.39/2）");
            CheckTrue(aL != null && aR != null && Mathf.Abs(PxYOf(aL.position.y) - 228.47f) <= 1f,
                      "两个圆钮中心 y = **228.47**（190.67 + 75.61/2）");
            // 🔴 **箭头那一层必须比黄底高一级队列** —— 两层摆在同一个矩形上，同队列时「谁盖谁不可控」
            //    （2026-09-24 实拍：箭头**整个没出现**、只看得见黄底，而矩形断言全绿）。
            //    判据照坑表那条：**比 `RenderQueue`，不比 z**。
            {
                var lIcon = aL != null ? aL.Find("Icon") : null;
                var lBg = aL != null ? aL.Find("Background") : null;
                var iq = lIcon != null ? lIcon.GetComponentInChildren<ImageQuad>() : null;
                var bq2 = lBg != null ? lBg.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(iq != null && iq.Texture != null, "左箭钮的箭头图**真的有贴图**（`40k_general_bt_arrow`）");
                CheckTrue(iq != null && bq2 != null && iq.RenderQueue > bq2.RenderQueue,
                          $"箭头那层队列 **{(iq != null ? iq.RenderQueue : -1)}** > 黄底那层 **{(bq2 != null ? bq2.RenderQueue : -1)}**"
                          + "（同队列时箭头会被盖掉 —— 实测过一次）");
            }
            // `Art Style Logo` 那一格：原版 `sprite=0`（运行时喂风格图 SO，**本地没有**）⇒ 我们画风格名（**我们挑的**）
            CheckText(TextOf(FindChild(spage, "Art Style Logo")), "Hammer and Bolter",
                      "风格名 = **Hammer and Bolter**（`AA_HB` 的显示名，出处 `解包资源使用地图.md:1174`）"
                      + " —— ⚠️ **原版这格是图不是字**，我们这里是**我们挑的做法**");
            // 🔴 **量渲染宽度**（不是比字号）：那一格是 **512×128**，56px 的 `Hammer and Bolter`
            //    实测宽 ≈1270px ⇒ **会压到右箭钮上**（第一版实拍一眼可见）。判据照 `AutoFitBox` 那条教训。
            CheckTrue(win.StyleLogoWidthPx <= 512f + 1f,
                      $"`Art Style Logo` 那行字的**渲染宽度 {win.StyleLogoWidthPx:F0}px ≤ 512**"
                      + "（超出就会压到右边那颗换风格钮上 —— 这条**矩形断言量不到**，得量 `Label.WorldW`）");
            // 网格：7 张里当前风格 6 张 ⇒ 2 行；首格中心
            Check(win.StyleVisibleCount, 6, "当前风格（`AA_HB`）下可见 **6** 张异画");
            Check(win.StyleCells.Count, 6, $"画出了 {win.StyleCells.Count} 格（视口外的不建 = 那套裁切）");
            if (win.StyleCells.Count > 0)
            {
                var s0 = win.StyleCells[0];
                // 内容**整体居中**：pad = (1589.78 − 6×262.5) / 2 = **7.39** ⇒ 首格中心 330.22+7.39+131.25
                CheckNear(PxOf(s0.position.x), 468.86f, 0.7f, "第 1 格中心 x = **468.86**（330.22 + pad 7.39 + 262.5/2）");
                CheckNear(PxYOf(s0.position.y), 479.67f, 0.7f, "第 1 格中心 y = **479.67**（287.67 + 384/2）");
                // 🔴 判「卡面用的是**异画**立绘」—— 扫这一格**所有** `MeshRenderer` 的材质贴图名字。
                //    ⚠️ **别只看第一个**：第一版取「第一个有贴图的」拿到的是**卡框**
                //    （`astramilitarum_tier3`），断言因此误报。`CardView` 的层走的是**自定义 mesh**
                //    （不是 `ImageQuad`）⇒ 只能按 `MeshRenderer.sharedMaterial.mainTexture` 判。
                int meshCount = 0; string altTex = null;
                foreach (var mr in s0.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (mr.sharedMaterial == null || mr.sharedMaterial.mainTexture == null) continue;
                    meshCount++;
                    if (mr.sharedMaterial.mainTexture.name.StartsWith("alt_") && altTex == null)
                        altTex = mr.sharedMaterial.mainTexture.name;
                }
                CheckTrue(meshCount > 0, $"第 1 格里画出了 `CardView` 的 {meshCount} 层网格");
                CheckTrue(altTex != null,
                          $"格里的立绘贴图 = **{altTex ?? "(一个 alt_* 都没有)"}**（**必须出现 `alt_*`** —— "
                          + "异画页画的就是它；一个都没有就说明 `CardData.artOverride` 没接上、退回了普通立绘）");
            }
            // 左抽屉：**出厂展开**（实证 act=T —— ⚠️ 与 Cosmetics 页相反）
            CheckTrue(win.StyleFiltersOpen, "异画页的左抽屉 `Card Filters` **起手是展开的**（实证 act=T）");
            Check(win.StyleVisibleCount, 6, "抽屉开着也不影响（起手没有筛选条件）");
            // 换风格：右箭钮 = 下一个（`v2` 只有 1 张）
            var rhit = FindChild(spage, "ArrowHit Right");
            var rbtn = rhit != null ? rhit.GetComponent<WindowButton>() : null;
            CheckTrue(rbtn != null, "右箭钮有点击区");
            if (rbtn != null)
            {
                rbtn.Click();
                Check(win.StyleIndex, 1, "点右箭 ⇒ 风格下标 = **1**（切到 `v2`）");
                CheckText(TextOf(FindChild(spage, "Art Style Logo")), "v2",
                          "风格名跟着变（`v2` 的**显示名查不到** ⇒ 直接印 token，如实记）");
                Check(win.StyleVisibleCount, 1, "`v2` 风格下只有 **1** 张异画（Azrael —— 本地就这么一张）");
                rbtn.Click();
                Check(win.StyleIndex, 0, "再点一下（`v2` 只有一格）⇒ **回绕**到第 0 种");
            }
            var lhit = spage != null ? FindChild(spage, "ArrowHit Left") : null;
            var lbtn = lhit != null ? lhit.GetComponent<WindowButton>() : null;
            if (lbtn != null)
            {
                lbtn.Click();
                Check(win.StyleIndex, 1, "点**左**箭 ⇒ 往回一个（`(0−1+2)%2` = 1）");
                lbtn.Click();
                Check(win.StyleIndex, 0, "再点左箭 ⇒ 回到 0");
            }
            // 筛选**真的筛得动**（复用同一套 `DeckEditorState`：筛 `legendary` 只剩传奇那几张）
            {
                int altBefore = win.StyleVisibleCount;
                win.ApplyStyleFilter("$rar:legendary");
                CheckTrue(win.StyleVisibleCount < altBefore,
                          $"筛 `Legendary` ⇒ 异画从 {altBefore} 张降到 **{win.StyleVisibleCount}** 张（真筛得动）");
                win.ClearStyleFilters();
                Check(win.StyleVisibleCount, altBefore, $"`Clear filters` ⇒ 回到 {altBefore} 张");
            }
            Shoot("07_收藏_Styles.png");
            win.tabButtons.Click(0);
            Debug.Log(P + "   " + win.Dump());
            SaveScene();

            // ---- 卡片详情窗 · 「创建副本」/「升级」两块面板**都不建**（用户 2026-09-27 拍板）----
            // 🔴 用户原话：「直接全部卡都是最高级别的卡框，这样就不用升级了。也不需要合成卡牌了。」
            //   · **升级**：`CardArt.TierOf` 已改成**所有卡一律最高档** ⇒ 没有可升的（而且我们这侧升级本就**不改卡面**）；
            //   · **合成**：本作全解锁（资源 9999、卡池全开、`Owned` 直接给足）⇒ 没有要合的。
            //   ⚠️ 上一轮还在这里验过「四档稀有度 → 四张万能卡图标」—— 面板停掉后那条判据**没有对象了**；
            //     `CardDetailPopup.Craftable` / `WildcardIconFor` **保留不删**，恢复那块面板时直接用。
            Section("卡片详情窗 · 「创建副本」/「升级」都不建");
            foreach (var want in new[] { "common", "rare", "epic", "legendary", "special" })
            {
                CardDef cd = null;
                var pool = CollectionWindow.CardsState.Pool;
                for (int i = 0; i < pool.Count; i++)
                    if (pool[i].Rarity == want) { cd = pool[i]; break; }
                if (cd == null) { Check(true, false, $"卡池里找不到稀有度 `{want}` 的卡"); continue; }
                var dw = win.OpenCardDetail(cd);
                if (dw == null) { Check(true, false, $"打不开卡片详情窗（{cd.Name}）"); continue; }
                CheckTrue(FindChild(dw.transform, "Craft Icon") == null,
                          $"★ `{want}` 的卡**没有**「创建副本」那一格（本作不做合成）—— 拿 `{cd.Name}` 试的");
                CheckTrue(FindChild(dw.transform, "Upgrade Title") == null,
                          $"★ `{want}` 的卡**没有**「升级」那一格（本作不做升级）—— 拿 `{cd.Name}` 试的");
                // 🆕 A17：卡片详情窗的两颗圆钮（语音 / 显示卡面文字）
                if (want == "common") CheckHoverSwap(dw.transform, "卡片详情窗");
                dw.Close();
            }

            // 🆕 A17：本窗的换图按钮（四页共用的 `Clear filters` / `Import` / `Create` / 换风格箭头 / 关闭钮「Back」）
            CheckHoverSwap(win.transform, "收藏窗");
            CheckNoMissingSwapArt("收藏窗这条链");

            // ============================================================ 🆕 2026-10-04：软边接线（三处）
            //
            // 🔴 **这一节量的是「接线」，不是机制** —— 机制（按带的内沿切开 + 逐角 alpha 斜坡）的逐条判据
            //    在 `Editor/RewardsScene.cs` §三·b4-d-2/d-4（对的是原版剖面手算值）。这里补的是另一半：
            //    **原版逐处不同的 `m_Softness` 真的被喂进那几处 `Clip` 了吗**（铁律 5·c：四处四个值）。
            // 判据（逐条实读，全量表 `d:/4/_tmp_view/q1_rm2d.txt`）：
            //   · 档案窗 `Avatar Tab/Item Display Panel/Scroll Rect` = **(0,50)**（:221-222）
            //   · 档案窗 `Title Tab/Item Display Panel/Scroll Rect` = **(0,50)**（:265-266）
            //   · 商店三页 `…/Packs Scroll View/Viewport` = **(0,25)**（`:177-178` / `:295-296` / `:59-60`）
            // 期望的**切线位置**全部由「原版值 + 原版视口矩形」现算（⛔ 不从被测实现里读常量）：
            //   带的内沿 = 视口该边的坐标 ± `m_Softness` 的那个分量。
            // ⚠️ 这两扇窗是**现场建的**（`PlayerProfileWindow` 是 Popup、`ShopWindow` 是 Fullscreen）
            //    —— 放在 `Run` 的**最后**，免得动到前面那些断言的现场。
            Section("软边接线（原版 `RectMask2D.m_Softness`）：档案窗 Avatar / Title 两页 + 商店");
            {
                // ---------------- ① 档案窗 `Avatar Tab`：(0,50) ----------------
                var pp = PlayerProfileWindow.Create(win.Manager);
                win.Manager.OpenWindow(pp);
                CheckTrue(pp.CurrentState == WindowState.Open, "`Player Profile Window` 开起来了（下面量它的两个页）");
                pp.tabButtons.Click(1);
                Check(pp.CurrentTab, WindowTabType.ProfileAvatar, "点第 2 个键 ⇒ 切到 `Avatar` 页");

                var avPage = FindChild(pp.transform, "Avatar Tab");
                var avGrid = FindChild(avPage, "Item Drawer");
                CheckTrue(avGrid != null, "`Avatar Tab/Item Display Panel/Scroll Rect/Item Drawer` 在");
                // 视口 = `AvatarTab` 的 `Scroll Rect`：654.16,210.69 → 1680.12,855.46（原版实测）
                // ⇒ 带内沿：上 210.69 + 50 = **260.69**、下 855.46 − 50 = **805.46**
                // 第 1 行的格（y 250.70..430.70）与第 3 行的格（710.70..890.70）各压在一条带上 ⇒ 两条都该出现
                CheckSoftCuts(ScanSoftCuts(avGrid, false), new[] { 260.69f, 805.46f }, 0.6f,
                              "`Avatar Tab` 的格子（原版 `m_Softness = (0,50)`）");
                Check(ScanSoftCuts(avGrid, true).Count, 0,
                      "`Avatar Tab` **一条竖切线都没有** —— 这一处只渐变上下（`(50,0)` 那种写反的实现这里会冒横竖两种）");

                // ---------------- ② 档案窗 `Title Tab`：(0,50)（同一个窗口的另一页）----------------
                pp.tabButtons.Click(2);
                Check(pp.CurrentTab, WindowTabType.ProfileTitle, "点第 3 个键 ⇒ 切到 `Title` 页");
                // ⚠️ 这一页的格子里**没有吃 `Clip` 的图件**：底板走 `ProfilePage.Solid`，而那个口子**不收 `Clip`**
                //    （`PlayerProfileWindow.cs` 的 `Solid` —— 与 `Rect/Nine/Text` 不同，**这是一条真缺口**，已写进报告）。
                //    ⇒ 这一处的可观测面是**压在带里的文字**：`MenuDraw.ClipText` 对带内顶点按同一剖面削 alpha。
                var ttGrid = FindChild(FindChild(pp.transform, "Title Tab"), "Item Drawer");
                var ttLb = FindChild(FindChild(ttGrid, "TitleDrawer_9"), "Name");
                var ttTmp = ttLb != null ? ttLb.GetComponentInChildren<TMPro.TextMeshPro>() : null;
                CheckTrue(ttTmp != null,
                          "第 4 行第 1 格（`TitleDrawer_9`：y 790.70..920.70，**压着视口底 855.46**）的名字有 TMP 网格");
                if (ttTmp != null)
                {
                    var ti = ttTmp.textInfo;
                    int nV = 0, nBand = 0, nBad = 0, nBelow = 0, nBadBelow = 0;
                    float minA = 255f, worst = 0f;
                    if (ti != null && ti.characterInfo != null && ti.meshInfo != null)
                    {
                        int cn = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
                        for (int ci = 0; ci < cn; ci++)
                        {
                            var ch = ti.characterInfo[ci];
                            if (!ch.isVisible) continue;
                            int mi = ch.materialReferenceIndex;
                            if (mi < 0 || mi >= ti.meshInfo.Length) continue;
                            var mesh = ti.meshInfo[mi];
                            if (mesh.vertices == null || mesh.colors32 == null) continue;
                            for (int k = 0; k < 4; k++)
                            {
                                int v = ch.vertexIndex + k;
                                if (v < 0 || v >= mesh.vertices.Length || v >= mesh.colors32.Length) continue;
                                float y = LayoutSpace.ToPixel(ttTmp.transform.TransformPoint(mesh.vertices[v])).y;
                                int a = mesh.colors32[v].a;
                                nV++;
                                minA = Mathf.Min(minA, a);
                                if (y > 805.46f)          // 带内：alpha 必须 = 255 × (视口底 855.46 − y) ÷ 50
                                {
                                    nBand++;
                                    float want = Mathf.Clamp01((855.46f - y) / 50f) * 255f;
                                    float err = Mathf.Abs(a - want);
                                    worst = Mathf.Max(worst, err);
                                    if (err > 3f) nBad++;
                                }
                                else                      // 带外：**一个顶点都不该被动**（硬裁那半句照旧）
                                {
                                    nBelow++;
                                    if (a < 250) nBadBelow++;
                                }
                            }
                        }
                    }
                    CheckTrue(nV > 0, $"量得到这一格的文字网格（{nV} 个顶点）");
                    CheckTrue(nBand > 0, $"**有 {nBand} 个顶点落在渐隐带里**（y > 805.46）—— 0 个 = 软边没接上"
                                       + "（顶点还会被硬裁夹到 855.46，但 alpha 一个都不动）");
                    CheckTrue(minA < 250f, $"带内的字**确实被削了 alpha**（最小 {minA:F0} < 250）—— 只夹顶点不削 alpha 是硬边");
                    Check(nBad, 0, $"带内每个顶点的 alpha = **255 × (855.46 − y) ÷ 50**（原版剖面；最差差 {worst:F2}）");
                    CheckTrue(nBadBelow == 0, $"带外（y ≤ 805.46）的顶点 {nBelow} 个**一个都没被动**"
                                            + "（软边只改带内；动的那些就是写错了带的位置）");
                }

                pp.Close();
                Check(pp.CurrentState, WindowState.Closed, "量完把档案窗关掉（别影响后面的现场）");

                // ---------------- ③ 商店：三页的 `Packs Scroll View` 都是 (0,25) ----------------
                // ⚠️ **ShopWindow 是 Fullscreen** ⇒ `OpenWindow` 会**把关着的当前主窗关掉**
                //    （`WindowsManager.OpenWindow` 那条原版判定）—— 所以这一段放在**最后**。
                var shop = ShopWindow.Create(win.Manager);
                win.Manager.OpenWindow(shop);
                shop.tabButtons.Click(0);
                var shopPage = FindChild(shop.transform, ShopData.Pages[0].Prefab);
                // ⚠️ 本文件没有 `FindPath`（`FindChild` 是**按名字**找的、不认识 `A/B/C`）⇒ 用 `Transform.Find`
                var shopContent = shopPage != null ? shopPage.Find("Packs Scroll View/Viewport/Content") : null;
                CheckTrue(shopContent != null, "商店 `Card Shop Tab/Packs Scroll View/Viewport/Content` 在");
                // 视口 = 329.76,127.62 → 1920.00,1080.00（原版实测）
                // ⇒ 带内沿：上 127.62 + 25 = **152.62**、下 1080.00 − 25 = **1055.00**
                // 第 1 行格底（133.62..610.62）与第 2 行格底（608.62..1080.00，硬裁到视口底）各压一条 ⇒ 两条都该出现
                CheckSoftCuts(ScanSoftCuts(shopContent, false), new[] { 152.62f, 1055.00f }, 0.6f,
                              "商店 `Packs Scroll View` 的格子（原版 `m_Softness = (0,25)`）");
                Check(ScanSoftCuts(shopContent, true).Count, 0,
                      "商店这一处**一条竖切线都没有** —— `m_Softness = (0,25)` 只渐变上下");
                shop.Close();
            }

            int total = _pass + _fail;
            if (_fail == 0) Debug.Log(P + $"=== 结束：{_pass}/{total} 全过 ✅ ===");
            else
            {
                var sb = new System.Text.StringBuilder(P + $"=== 结束：{_pass}/{total} 通过，**{_fail} 条失败** ❌ ===");
                foreach (var f in _failures) sb.Append("\n").Append(P).Append("   ✗ ").Append(f);
                Debug.LogError(sb.ToString());
            }
            if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
        }

        static float DeckCellWpx() { return CollectionWindow.DeckCellW; }
        static float DeckCellHpx() { return CollectionWindow.DeckCellH; }

        static void SaveScene()
        {
            // 收藏窗是**挂在壳锚点上的窗口**，不该有自己的场景 ⇒ 这里只存一张自检场景供人工看
            var path = "Assets/CardPresentation/Scenes/CollectionCheck.unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
            Debug.Log(P + $"  自检场景 → {path}");
        }
}
