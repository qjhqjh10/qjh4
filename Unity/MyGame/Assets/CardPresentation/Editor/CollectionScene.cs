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
        static string TextOf(Transform t)
        {
            var lb = t != null ? t.GetComponentInChildren<Label>() : null;
            return lb != null ? lb.Text : null;
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

            // ---------------- `Deck info Popup`（A1 §2 逐节点表）----------------
            Section("`Deck info Popup`：版面（A1 §2）");
            {
                var pop = win.OpenDeckInfo(0);
                CheckTrue(pop != null, "开得出来");
                if (pop != null)
                {
                    var pr = pop.transform;
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
                              && pop.Btn("Select Deck") != null, "`Buttons` 三个钮都在");
                    var pb = pop.Btn("Practice Deck");
                    CheckNear(pb != null ? PxOf(pb.position.x) : -1f, 887.45f, 1f,
                              "`Practice Deck` 中心 x = **887.45**（三个钮右缘到 1770.70、spacing 36）");
                    var sb = pop.Btn("Select Deck");
                    CheckNear(sb != null ? PxOf(sb.position.x) : -1f, 1608.45f, 1f, "`Select Deck` 中心 x = **1608.45**");
                    CheckTrue(pop.Opt("Delete") != null && pop.Opt("Switch Deck Info") != null
                              && pop.Opt("Share") != null && pop.Opt("Share On Chat") != null
                              && pop.Opt("Duplicate") != null, "`Deck Options` 五个圆钮都在");
                    var od = pop.Opt("Delete");
                    var os2 = pop.Opt("Switch Deck Info");
                    CheckNear(od != null ? PxOf(od.position.x) : -1f, 1648.89f, 1f,
                              "最左那个是 **`Delete`**（`reverse=1` 把 GO 顺序倒过来了）");
                    CheckNear(os2 != null ? PxOf(os2.position.x) : -1f, 1746.43f, 1f, "最右那个是 `Switch Deck Info`");
                    Check(DeckInfoPopup.ListCols, 3,
                          "卡列表列数 = **3** = floor((1140 − 15 − 15 + 11) ÷ 371)（照 GridLayoutGroup 那套算）");
                    CheckTrue(pop.Rows.Count > 0, $"卡组内容画了 {pop.Rows.Count} 行");
                    var r0 = pop.Rows.Count > 0 ? pop.Rows[0] : null;
                    CheckNear(r0 != null ? PxOf(r0.position.x) : -1f, 854f, 1f,
                              "第 1 行中心 x = **854**（Info Panel 左 659 + pad 15 + 180）");
                    CheckNear(r0 != null ? PxYOf(r0.position.y) : -1f, 247.10f, 1f,
                              "第 1 行中心 y = **247.10**（218.10 + 58/2）");
                    Shoot("05_收藏_DeckInfo弹窗.png");
                    var popCloseHit = FindChild(pr, "CloseHit");
                    var popCloseWb = popCloseHit != null ? popCloseHit.GetComponent<WindowButton>() : null;
                    CheckTrue(popCloseWb != null, "关闭圆钮有点击区");
                    if (popCloseWb != null) popCloseWb.Click();
                    Check(pop.CurrentState, WindowState.Closed, "点关闭钮 ⇒ 窗进 `Closed` 态");
                }
            }

            // ---------------- `Import Deck Popup`（A1 §4）----------------
            Section("`Import Deck Popup`：版面 + **导入闭环**（A1 §4）");
            {
                var imp = win.OpenImportPopup();
                CheckTrue(imp != null, "开得出来");
                if (imp != null)
                {
                    var iroot = imp.transform;
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
                // 原版**起手是收起的**（整栏滑到 `hiddenPosition=(-550,0)`）⇒ 我们整块 SetActive(false)
                CheckTrue(!fltPanel.gameObject.activeSelf, "**起手收起**（原版 `hiddenPosition = (-550, 0)`）");
                win.ToggleFilters();
                CheckTrue(fltPanel.gameObject.activeSelf, "`Filters` 圆钮 ⇒ 面板打开");

                CheckAt(fltPanel, 0.25f, 335.56f, 155.9f, 1079.99f,
                        "面板矩形 = **0.25,155.9 → 335.56,1080**（原版 x/w；高按**屏幕可见**的 924.1 裁）");
                CheckNear(CollectionWindow.FltContentH, 989.02f, 0.1f,
                          "内容高 = **989.02**（7 行之和，`menu_rect` 实读 —— **不是**模板位）");
                var fscr = win.FilterScroll;
                CheckTrue(fscr != null, "面板挂了滚动区（原版 `Scroll View` sens **50**、`Viewport` + `Mask showGraphic=0`）");
                if (fscr != null)
                    CheckNear(fscr.MaxOffset, 989.02f - 924.1f, 0.6f,
                              "可滚量 = **约 64.9**（989.02 − 924.1）—— **「Type 行够得着」的判据**");

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
                    CheckNear(PxYOf(r0.position.y), 599.92f, 0.6f, "Rarity 第 1 格中心 y = **599.92**（155.9+329.02+65+50）");
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
                // Cost 第 1 格：Content 从行内 y+65 起、pad L15、cell 65×65
                var c0 = FindChild(fltPanel, "Cell_cost_1");
                CheckTrue(c0 != null, "Cost 的 `1-` 格在");
                if (c0 != null)
                {
                    CheckNear(PxOf(c0.position.x), 47.75f, 0.6f, "Cost 第 1 格中心 x = **47.75**（0.25+15+32.5）");
                    CheckNear(PxYOf(c0.position.y), 862.42f, 0.6f, "Cost 第 1 格中心 y = **862.42**（155.9+609.02+65+32.5）");
                    CheckNear(Wpx(c0), 65f, 2f, "Cost 格 = **65×65**（原版 `cell 65×65`）");
                    CheckArt(c0, "Card_Frame_Cost_Icon", "Cost 格的图 = `Card_Frame_Cost_Icon`");
                }
                // Type 那一行：面板内 889.02→989.02，视口只到 924.1 ⇒ **不滚只露上面一截**，滚下去才完整
                if (fscr != null)
                {
                    var th0 = FindChild(fltPanel, "Cell_type_hero");
                    CheckTrue(th0 != null && PxYOf(th0.position.y) + 50f > 1080.5f,
                              "**不滚时 Type 那 3 格只露上半截**（下半截在视口外 —— 原版 `RectMask2D` 那套裁切）");
                    fscr.ScrollBy(fscr.MaxOffset);
                    var th = FindChild(fltPanel, "Cell_type_hero");
                    CheckTrue(th != null && PxYOf(th.position.y) + 50f <= 1080.5f,
                              "**滚到底 ⇒ Type 那 3 格完整落进视口**（这就是「可滚 64.9」那条的用处）");
                    CheckArt(th, "40k_menu_search_icon_warlord", "Type 第 1 格（Warlord）的图 = `40k_menu_search_icon_warlord`");
                    CheckTrue(FindChild(fltPanel, "Cell_type_unit") != null
                              && FindChild(fltPanel, "Cell_type_tactic") != null,
                              "Type 另两格 `Troops` / `Stratagem` 也在（图 = `..._troop` / `..._stratagem`）");
                    fscr.ScrollBy(-fscr.MaxOffset);
                }

                // 四行的小标题（原版 `Title` TMP · **fs32 · hAlign=Center**）
                //   🔴 2026-09-23 **实拍补的缺口**：第一版只建了格子、**四个标题一个都没建**，
                //      96 条断言全绿 —— 因为它们不是「摆错位」而是「根本不在」，而当时没有盯这一条的断言。
                foreach (var ttl in new[] { "Army", "Rarity", "Energy Cost", "Type" })
                    CheckText(TextOf(FindChild(fltPanel, "Title " + ttl)), ttl,
                              $"小标题 `{ttl}` 在（原版 `Title` TMP fs32）");

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
            Check(CollectionWindow.CosmoCols, 7,
                  "列数 = **7** = floor(1751.73 ÷ 250)（⚠️ **不是** `_segments=5` —— 那是死值）");
            var cpage = win.PageRoot(2);
            var cbh = cpage != null ? FindChild(cpage, "Scroll View") : null;
            CheckAt(cbh, 168.27f, 1920f, 85f, 1080f, "`Scroll View`（卡背视口 1751.73 × 995）");
            CheckTrue(win.CosmoCells.Count > 0, $"画出了 {win.CosmoCells.Count} 格（视口外的不建 = 那套裁切）");
            if (win.CosmoCells.Count > 0)
            {
                var k0 = win.CosmoCells[0];
                CheckNear(PxOf(k0.position.x), 293.27f, 0.6f, "第 1 格中心 x = **293.27**（168.27 + 250/2）");
                CheckNear(PxYOf(k0.position.y), 287.5f, 0.6f, "第 1 格中心 y = **287.5**（85 + 405/2）");
                var art = FindChild(k0, "Cardback");
                CheckNear(Wpx(art), 250f, 2f, "格里的卡背宽 = **250**（原版 `Cardback` 铺满 250×405）");
                CheckNear(Hpx(art), 405f, 2f, "格里的卡背高 = **405**");
                CheckTrue(art != null && art.GetComponentInChildren<ImageQuad>() != null
                          && art.GetComponentInChildren<ImageQuad>().Texture != null,
                          "卡背**真的有贴图**（不是空图 —— 导没导错就看这一条）");
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
            {
                var bq = FindChild(cpage, "Clear filters");
                CheckTrue(bq != null && Mathf.Abs(PxOf(bq.position.x) - 550f) <= 1f,
                          "`Clear filters` 底图中心 x = **550**（= 容器 425..1385 内、`align=3` 左对齐 ⇒ 425+125）"
                          + " —— **别照抄 dump 的 1405**（那是 VLG 跑之前的模板位）");
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
            win.tabButtons.Click(0);
            Debug.Log(P + "   " + win.Dump());
            SaveScene();

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
