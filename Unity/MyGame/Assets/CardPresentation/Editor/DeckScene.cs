// DeckScene.cs — 卡组编辑界面的构建 + 自检
//
// 两个入口（和 BattleScene 一个套路）：
//   DeckScene.BuildAndSaveScene   建出 `Assets/CardPresentation/Scenes/DeckEditor.unity`，打开按 Play 就能用
//   DeckScene.Run                 批处理自检：状态断言 + 建场景 + 截图（grep "^DK "）
//
// ---- 坐标怎么来的 ----
// 这套布局「可见高恒 10 世界单位」，1080p 下 **108 px = 1 世界单位**；
// 原版界面按 1920×1080 设计的，所以 `Pos(px, py)` 把原版像素直接换算过来。
//
// 🔴 2026-09-20：**版面与绘制的唯一出处已经挪进 `CardPresentation/Deck/DeckRuntime.cs`**
//    （这个文件只做「建场景 + 自检」，绘制全部转发给它 —— 见 `Build`）。
//    权威坐标 = `D:/2/Warpforge_tools/data/ui_layout/_deck_editing_godot_rects.txt`，
//    查证正本 = `资料/卡组编辑界面_查证_0920.md`。**改版面改那一份，别在这里写第二份。**
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RuleEngine;
using CardPresentation;

// ⚠️ 放在**全局命名空间**（不加 namespace）—— `-executeMethod DeckScene.Run` 认的就是这个名字。
//    和 `BattleScene` / `CardBaseDemo` 一致；放进命名空间的话 Unity 会说「class could not be found」。
public static class DeckScene
{
        const string P = "DK ";
        const string ScenePath = "Assets/CardPresentation/Scenes/DeckEditor.unity";
        const string ShotDir = @"d:/4/_tmp_view/deck";

        /// <summary>原版界面按 1920×1080 设计；这套布局可见高 10 单位 → 108 px/单位</summary>
        const float PxPerUnit = 1080f / LayoutSpace.DesignHeight;   // = 108
        const float ScreenW = 1920f, ScreenH = 1080f;

        // ⚠️ 我们挑的（原版是回收滚动列表，节点树给不出一屏几列）
        const int Cols = 4, Rows = 3;
        const float CardScale = 0.62f;
        // 卡格区域：侧栏占 0..350，筛选栏占 1600..1900，中间 350..1600 才是卡池的地盘。
        // 4 列 × 260 步进、3 行 × 260 步进 —— 第一版把步进写成 330、起始 x 写成 980，
        // 结果第 4 列压到筛选栏上、第 3 行跑到屏幕外（截图看出来的）
        const float GridCx = 975f, GridCy = 520f, GridStepX = 250f, GridStepY = 260f;

        static int _pass, _fail;
        static readonly List<string> _failures = new List<string>();

        static Transform _root;

        // ============================================================ 工具

        /// <summary>原版像素坐标 → 世界坐标（原点在屏幕中心，y 向上）</summary>
        static Vector3 Pos(float px, float py)
        {
            return new Vector3((px - ScreenW * 0.5f) / PxPerUnit, (ScreenH * 0.5f - py) / PxPerUnit, 0f);
        }

        /// <summary>原版像素长度 → 世界单位</summary>
        static float U(float px) { return px / PxPerUnit; }

        /// <param name="z">越**大**离相机越远（相机在 z=-20 朝 +z 看）。
        /// 同 z 的两个 quad 谁压谁看渲染顺序、不确定 —— HUD 那边也踩过这条，所以显式分层。</param>
        static ImageQuad Quad(Texture2D tex, float cx, float cy, float w, float h, string name, float z = 0f)
        {
            var q = ImageQuad.Create(_root, tex, Pos(cx, cy) + new Vector3(0f, 0f, z), U(h),
                                     new Vector2(0.5f, 0.5f), name);
            return q;
        }
        const float ZPanel = 0.30f, ZRow = 0.10f, ZBar = 0.10f;

        static Label Text(string s, float cx, float cy, int scale, Color c, string name = null)
        {
            return Label.Create(_root, s, Pos(cx, cy), scale, c, new Vector2(0.5f, 0.5f), name);
        }

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

        /// <summary>数值比较（±`tol`）—— 用来比**原版参数**那类量（颜色系数、像素尺寸）。</summary>
        static void CheckNear(float got, float want, float tol, string msg)
            => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（实测 {got:F4} ≈ 期望 {want:F4} ± {tol:F4}）");

        // ============================================================ 🆕 2026-10-04（A24）悬停 / 状态换图的断言
        //
        // 判据 = **原 prefab 的组件字段**（`m_Transition` / `m_SpriteState` / `EverguildToggle.onSprite|offSprite`），
        // 普查正本 = `资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md`；
        // 本轮又把 5 颗的 `m_TargetGraphic` + 三张图名、7 颗的状态图名**逐字段复读了一遍**。

        /// <summary>把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
        /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
        static void CheckHoverSwap(Transform root, string what)
        {
            int n; string bad = WindowButton.AuditHoverSwap(root, out n);
            CheckTrue(n > 0, what + "：**确实有**接了悬停换图的按钮（n=" + n + "，否则这条等于没查）");
            if (bad.Length > 0) CheckTrue(false, what + "：换图要「悬停换得动 + 离开还原得回」—— " + bad);
        }

        /// <summary>**取不到的悬停图**一张都不许有（红线：不许静默画成没反应）。</summary>
        static void CheckNoMissingSwapArt(string what)
            => CheckTrue(WindowButton.MissingSwapArt.Count == 0,
                         what + "：**悬停图一张都不缺**（缺的会列在这里："
                         + string.Join("、", WindowButton.MissingSwapArt.ToArray()) + "）");

        static string TexName(Texture t) { return t != null ? t.name : "<无>"; }

        /// <summary>🆕 2026-10-05（A32②）**变色那一档**（原版 `m_Transition = 1(ColorTint)`）的逐颗断言：
        /// 悬停 ⇒ 系数 = 原版 `m_Colors.m_HighlightedColor`（**0.9607843**）· 移开 ⇒ 回到 **1**。
        /// ⚠️ 批处理没有帧循环，但 `WindowButton.SetTarget` 在 `Application.isPlaying == false` 时**直接落目标值**
        ///   ⇒ 这里读到的是真值（不需要推进帧，与 `AuditHoverSwap` 直调 `Enter/Exit` 同一个道理）。
        /// ⚠️ **期望值写的是原版 prefab 里 `m_Colors` 的字面量**，不是我们代码里的常量（否则就是自证）。</summary>
        static void CheckHoverTint(WindowButton wb, string what)
        {
            if (wb == null) { CheckTrue(false, what + "：**没接**悬停（`UiHoverAt` 一颗都没打到）"); return; }
            CheckTrue(Mathf.Abs(wb.TintKForTest - 0.9607843f) < 1e-4f,
                      what + "：悬停 ⇒ 原色 × **0.9607843**（原版 `m_Colors.m_HighlightedColor`；实得 "
                      + wb.TintKForTest.ToString("F4") + "）");
            _rt.UiHoverAt(1700f, 500f);                 // 移开（卡池那边，一件悬停件都没有）
            CheckTrue(Mathf.Abs(wb.TintKForTest - 1f) < 1e-4f,
                      what + "：移开 ⇒ 还原 **1.0**（实得 " + wb.TintKForTest.ToString("F4") + "）");
        }

        /// <summary>**逐颗**验一颗按钮的悬停换图（判据 = 原版那颗 `m_SpriteState` 的三张图名）。
        /// 三步都要过：① 组件层面三张图对得上 ② 直调 `Enter/Exit` 换得动 + 还原得回 + **不改矩形**
        /// （`ImageQuad.SetTexture` 会把宽高比冲成贴图自己的 —— A17 买过的教训）
        /// ③ **走鼠标那条路**（`UiHoverAt`，就是 `HandlePointer` 每帧调的那条）也能打到它。</summary>
        static void CheckHoverOne(string key, string node, string normal, string hover, string pressed, string what)
        {
            var t = _root != null ? _root.Find(node) : null;
            var wb = t != null ? t.GetComponent<WindowButton>() : null;
            if (wb == null)
            {
                CheckTrue(false, $"{what}：`{node}` 上**接了 `WindowButton`**（不然悬停时屏幕上什么都不发生）");
                return;
            }
            CheckTrue(wb.NormalTexForTest != null && wb.NormalTexForTest.name == normal,
                      $"{what}：常态图 = 原版 `{normal}`（实得 `{TexName(wb.NormalTexForTest)}`）");
            CheckTrue(wb.HoverTexForTest != null && wb.HoverTexForTest.name == hover,
                      $"{what}：高亮图 = 原版 `{hover}`（实得 `{TexName(wb.HoverTexForTest)}`）");
            CheckTrue(wb.PressedTexForTest != null && wb.PressedTexForTest.name == pressed,
                      $"{what}：按下图 = 原版 `{pressed}`（实得 `{TexName(wb.PressedTexForTest)}`）");
            var q = wb.target;
            if (q == null) { CheckTrue(false, $"{what}：换图落在**看得见的那一层**上（`target` 是空的）"); return; }
            float w0 = q.WorldW, h0 = q.WorldH;
            wb.Enter();
            CheckTrue(wb.CurrentTexForTest == wb.HoverTexForTest,
                      $"{what}：**悬停换得动**（实得 `{TexName(wb.CurrentTexForTest)}`）");
            wb.Exit();
            CheckTrue(wb.CurrentTexForTest == wb.NormalTexForTest,
                      $"{what}：**离开还原得回**（实得 `{TexName(wb.CurrentTexForTest)}`）");
            CheckTrue(Mathf.Abs(q.WorldW - w0) < 1e-4f && Mathf.Abs(q.WorldH - h0) < 1e-4f,
                      $"{what}：换图**不改矩形**（`SetTexture` 会冲掉宽高比 ⇒ 必须 `SetAspect` 拉回；实测 "
                      + $"{w0 * 108f:F1}×{h0 * 108f:F1} → {q.WorldW * 108f:F1}×{q.WorldH * 108f:F1} px）");
            // ③ 派发：走**鼠标那条路**（`UiHoverAt` = `HandlePointer` 里那条），命中口径 = 点击同一条
            if (_rt.UiBtnRect(key, out float bx, out float by, out float bw, out float bh))
            {
                CheckTrue(_rt.UiHoverAt(bx + bw * 0.5f, by + bh * 0.5f) == wb,
                          $"{what}：**走鼠标那条路**（`UiHoverAt`）打到的就是它（悬停派发接线通）");
                CheckTrue(wb.CurrentTexForTest == wb.HoverTexForTest,
                          $"{what}：派发过来的悬停也换成了 HL 图");
                _rt.UiHoverAt(1700f, 500f);                      // 移开（卡池那边，没有按钮）
                CheckTrue(wb.CurrentTexForTest == wb.NormalTexForTest,
                          $"{what}：移开 ⇒ 判据还原（`UiHoverAt` 那条路）");
            }
            else Check(true, false, $"{what}：`{key}` 在 `_btns` 里量不到（悬停派发比不了）");
        }

        // ============================================================ 🆕 2026-10-05（A93②）筛选栏小标题的左对齐
        //
        // 判据 = **原 prefab 的字段 + 权威矩形表**（⛔ 不是我们自己的常量）：
        //   · **对齐**：四行 `Title` 的 TMP 是 `m_HorizontalAlignment = 1`(Left) · `m_VerticalAlignment = 512`(Middle)
        //     —— 逐行实读见 `Core/FilterPanelModel.cs` 的 `TitleFontPx` 那段（旧注释里的 `Center` 是读错了）；
        //   · **矩形**：`D:/2/Warpforge_tools/data/ui_layout/_deck_editing_godot_rects.txt`
        //     —— 面板 `Card Filters :: pos(2.2,156.0)`（**该文件 :174**）= 面板原点 **2.2**；
        //     `Title` 两处 x = **2.2**（Army 行，`:192`）与 **27.2**（另三行，`:198` / `:205` / `:212`）
        //     ⇒ 面板内 **0 / 25 / 25 / 25**、画布绝对 x = **2.2 / 27.2 ×3**。
        //   ⚠️ **收藏窗那套 `Card Filters :: pos(0.25,…)` 不是本窗的尺子** —— 本窗面板原点就是 **2.2**
        //     （`DeckRuntime.FltX`）⇒ 画布值是 2.2 / 27.2，**不是** 0.25 / 25.25。
        //   ⛔ 期望值**不许**拿 `DeckRuntime.FltX` / `FilterPanelModel.TitleArmyX` 去算 —— 那是自证
        //     （常量被改坏时断言跟着变，永远绿）。

        /// <summary>按名字**往深处**找一个节点。`Transform.Find` 只认直接子物体，而这四个小标题挂在
        /// 抽屉容器 `flt_drawer` 底下（`DeckRuntime.BuildFilterFixedParts()` 里 `Txt(…, FltParent)`）
        /// ⇒ `DeckRuntime.UiLabelText` 那种只认直接子物体的口子**在这四个字上恒答 `null`**（别拿它量）。</summary>
        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>筛选栏里一行小标题**渲出来的左沿**（画布 px · 左上原点 · y 向下）。
        /// 🔴 量的东西：`Label.WorldW`（TMP `textBounds` 的**真测量**）反推的左缘 ——
        /// **不是**节点位置、更**不是**「对齐枚举 == Left」（那是同义反复：把渲染那句删掉照样绿）。
        /// ⛔ 期望值由**调用方**给（取自原版读数），本函数只负责量。
        /// 量不出来（那行小标题不在 / 名字对不上 / `WorldW` 是垃圾）⇒ 返回 **−9999** ⇒ 断言必红，
        /// **不静默放过**（宽度上下界那道守卫与 `Label.HasMeasuredWidth` 同一条：TMP 在未激活 / 空串时
        /// 给的是天文数字，实测 4.29e9）。
        /// ⚠️ 节点名 = `DeckRuntime.BuildFilterFixedParts()` 的 `"flt_title_" + tl.Text.Replace(" ", "_")`
        /// ⇒ 这里**同样要把空格换成下划线**（`Energy Cost` → `flt_title_Energy_Cost`）。</summary>
        static float TitleLeftPx(string title)
        {
            var root = _rt != null ? _rt.Root : _root;
            var t = FindDeep(root, "flt_title_" + title.Replace(" ", "_"));
            var lb = t != null ? t.GetComponent<Label>() : null;
            if (lb == null || lb.Text != title) return -9999f;     // 找不到 / 认错节点 ⇒ 必红
            float w = lb.WorldW * PxPerUnit;
            if (!(w > 20f && w < 2000f)) return -9999f;
            return DeckRuntime.PxOfWorld(lb.transform.position).x - w * 0.5f;
        }

        /// <summary>🆕 **2026-10-10（A224②）**：一张 `ImageQuad` 的**渲染矩形**（画布 px · 左上原点）——
        /// 网格以 `transform` 为中心、锚 (.5,.5)（`ImageQuad.RebuildMesh`）⇒ 由世界坐标 ± `WorldW/H` 反推。
        /// ⛔ **不是** `RectTransform.rect`、更**不是**模型算出来的那个矩形：本条要量的是「**真渲出来的几何**」
        /// （裁切只动几何 ⇒ 只有量渲染矩形才测得到）。同族写法 → `Editor/RewardsScene.cs` 的 `QuadRectOf`。
        /// <para>🔴 **2026-10-12（A328①）位置项改成设计空间口径**（= `Shell/MenuDraw.QuadRectPx` 那一份，
        /// A298 修的那条）：原来是裸 `DeckRuntime.PxOfWorld(q.transform.position)` —— `Transform.position`
        /// 是**已缩放**的视觉世界坐标，而本文件拿它比的期望值**全是原版设计 px 字面量**
        /// （`BandX1/BandTop/BandW/BandBottom` 等）⇒ 只要树上有非 1 的 `lossyScale` 就**不是同量纲**。
        /// 现在位置项先过 `MenuDraw.PosInDesignSpace()`（= **全工程唯一一份**「世界 → 设计空间」换算），
        /// **尺寸项一个字没动**（`q.WorldW/H` 是 `ImageQuad.Create/SetWorldHeight` 收进去的**设计长度** ⇒
        /// 乘 `K` 就是设计 px —— 与 A298 那句「尺寸项本来就在设计量纲上」同一条）。</para>
        /// <para>✅ **今天逐位不变（有实据，不是推理）**：`localScale ≠ 1` 的写入点全工程只有 5 处
        /// （清单 → `资料/普查产出_1010/A297_MenuWindowBase副本.md`），**没有一处落在本场景的树上** ——
        /// 本窗的根 = `Build()` 里 `new GameObject("DeckEditor")`（**无父、位置原点**），
        /// 全树唯一的 `localScale` 写入是 `DeckRuntime.NewDrawer()` 那句 `= Vector3.one`；
        /// 而 `TransformScalerBySmallScreenUI` **只由窗根挂**（全仓 grep 该类，`AddComponent` 的**生产**调用点只有两处：
        /// `Shell/WindowsManager.cs:381` 的 `GameWindow.TryOpen` 那一支，与 `Shell/TrophyInfoPopup.cs:217`
        /// 的「prefab 烤着那一颗」；其余命中全在 `Editor/*Scene.cs` 的自检探针里）
        /// —— **`DeckRuntime` 不是 `GameWindow`**、也不烤任何缩放器 ⇒ 走不到那两条路。
        /// ⇒ `PosInDesignSpace` 除的是 1（`DivByScale(v,1) == v`）⇒ 与改前**逐位相同**。
        /// ⚠️ **两态断言**（本文件「筛选抽屉：模态/量渲染真值」那一节里的 `A328①` 段）把这条
        /// 「开关一开也照样是设计 px」钉住：它给 `_root` 挂一颗 `menuScale = 1.2` 的缩放器再量同一格。</para></summary>
        static bool QuadRectPx(ImageQuad q, out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = x2 = y2 = 0f;
            if (q == null) return false;
            var c = LayoutSpace.ToPixel(MenuDraw.PosInDesignSpace(q.transform));
            float w = q.WorldW * PxPerUnit, h = q.WorldH * PxPerUnit;
            x1 = c.x - w * 0.5f; x2 = c.x + w * 0.5f;
            y1 = c.y - h * 0.5f; y2 = c.y + h * 0.5f;
            return true;
        }

        /// <summary>🆕 **2026-10-12（A364）**：一个**节点子树里全部活着的 `ImageQuad`** 的渲染矩形**并集**（画布 px）。
        /// 量「一棵九宫格 / 一个容器」必须用并集 —— ⛔ 只取第一块会量成**某个子块**（`ShellScene` 那条
        /// 「弹窗底量成 182×173」的注释记的正是这个坑）。`node` 自己没有 `ImageQuad`（九宫格根就是空节点）也照样能量。</summary>
        static bool UnionQuadsPx(Transform node, out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = x2 = y2 = 0f;
            if (node == null) return false;
            var qs = node.GetComponentsInChildren<ImageQuad>(true);
            if (qs == null || qs.Length == 0) return false;
            float a1 = float.MaxValue, b1 = float.MaxValue, a2 = float.MinValue, b2 = float.MinValue;
            bool any = false;
            foreach (var q in qs)
            {
                if (q == null || !q.gameObject.activeInHierarchy) continue;
                if (!QuadRectPx(q, out float qx1, out float qy1, out float qx2, out float qy2)) continue;
                a1 = Mathf.Min(a1, qx1); b1 = Mathf.Min(b1, qy1);
                a2 = Mathf.Max(a2, qx2); b2 = Mathf.Max(b2, qy2);
                any = true;
            }
            if (!any) return false;
            x1 = a1; y1 = b1; x2 = a2; y2 = b2;
            return true;
        }

        /// <summary>🆕 **2026-10-12（A364）**：一个节点子树里**活着的第一块 `ImageQuad`**（量它的渲染队列/图名用）。
        /// 找不到 ⇒ null（调用点自己判，⛔ 不静默）。</summary>
        static ImageQuad FirstQuad(Transform node)
        {
            if (node == null) return null;
            foreach (var q in node.GetComponentsInChildren<ImageQuad>(true))
                if (q != null && q.gameObject.activeInHierarchy) return q;
            return null;
        }

        /// <summary>🆕 **2026-10-12（A364）**：拆掉「模态消息窗」那条链建出来的**场景对象**——
        /// `WindowsManager.EnsureHost()` 建的那三样（`Window Anchors` / `WindowsManager` / `Pointer Layer`）
        /// ＋ 挂在弹窗锚点下的那两扇窗（它们随 `Window Anchors` 一起走）。
        /// <para>🔴 **为什么必须拆**：`Run()` 末尾会 `Shoot` + `SaveScene()` **重写** `DeckEditor.unity`，
        /// 而 Unity 的 `SaveScene` **连非激活的 GameObject 一起存** —— 运行期拼出来的窗（贴图/材质都是
        /// 运行期造的）一旦落进场景，下次打开工程就是一堆**指不回原始资产**的壳
        /// （同族事故：`BattleScene` 那条「34 个粒子的材质没贴图」；本文件里那几处
        /// `DestroyImmediate(sgo)` 也是同一个理由）。</para>
        /// <para>幂等：没建过 ⇒ 什么都不做。返回拆掉的**根对象**个数（自检口）。</para></summary>
        static int DestroyPopupScaffold()
        {
            int n = 0;
            var wm = WindowsManager.Instance;
            if (wm != null) { wm.CloseAllWindows(); UnityEngine.Object.DestroyImmediate(wm.gameObject); n++; }
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(
                         UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
            {
                if (t == null) continue;
                if (t.name == "Window Anchors" || t.name == "Pointer Layer" || t.name == "WindowsManager")
                { UnityEngine.Object.DestroyImmediate(t.gameObject); n++; }
            }
            int left = 0;
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(
                         UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
                if (t != null && (t.name == "MessagePopupWindow" || t.name == "MessagePopupWindow2Buttons")) left++;
            if (left > 0)
                Debug.LogWarning($"[DK] `DestroyPopupScaffold` 之后还有 {left} 扇模态窗留在场景里 —— "
                               + "它们会被 `SaveScene()` 写进 `DeckEditor.unity`（不该发生的组合，报出来）。");
            return n;
        }

        /// <summary>🆕 **2026-10-12（A364）**：点模态窗上那颗钮（**走生产那条路**：`WindowButton.ClickForTest()`
        /// → `Click()` → `onClick`，= `PointerLayer` 派发时会调的那一个）。找不到节点 ⇒ 打一条红（⛔ 不静默）。</summary>
        static void ClickPopupButton(Component popup, string btnName)
        {
            var node = popup != null ? FindDeep(popup.transform, btnName) : null;
            var wb = node != null ? node.GetComponentInChildren<WindowButton>(true) : null;
            CheckTrue(wb != null, $"（点钮）`{btnName}` 上挂着 `WindowButton`（挂不上 ⇒ 这扇窗的钮点不动）");
            if (wb != null) wb.ClickForTest();
        }

        /// <summary>🆕 **2026-10-11（A289）**：筛选栏里那一行小标题的 `Label`（按**名字**取，找不到 / 认错 ⇒ null）。
        /// 节点名 = `"flt_title_" + 空格换下划线`（同 `TitleLeftPx` 那句）。</summary>
        static Label FilterTitleLabel(string title)
        {
            var root = _rt != null ? _rt.Root : _root;
            var t = FindDeep(root, "flt_title_" + title.Replace(" ", "_"));
            var lb = t != null ? t.GetComponent<Label>() : null;
            return lb != null && lb.Text == title ? lb : null;      // ⛔ 认错节点 = 当没找到（不静默放过）
        }

        /// <summary>🆕 **2026-10-11（A289）**：一段文字**渲染网格**的外接矩形（画布 px · 左上原点 · y 向下）。
        /// <para>🔴 **为什么不能拿 `transform.position` / `Label.WorldW/H` 代替**：`MenuDraw.ClipTextNow` 那一刀
        /// 只改 **TMP 网格的顶点**（夹到框边上 + 按同一仿射改 uv）—— **节点位置与 `WorldW/H` 一个字都不动**
        /// ⇒ 只有量**顶点**才看得出「有没有被裁住」（同 `ShellScene.TextMeshWidthPx` 那条的口径；
        /// 那边只要宽度，这里四条边都量 —— 本条的判据在**上沿**）。</para>
        /// <para>量的是 `textInfo`（TMP 的**模型**数组）—— 与 `MenuDraw.ClipTmpMesh` 写进去的是**同一份**；
        /// ⚠️ 上传那一半（`MeshFilter.sharedMesh`）在批处理下可能被跳过（`MenuDraw.TextClipUploadSkipped`），
        /// 那**不影响**这里读到的数（那一刀确实写进了模型数组）。量不出来（没字形 / `textInfo` 空）⇒ false。</para></summary>
        static bool TextMeshRectPx(Label lb, out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = float.MaxValue; x2 = y2 = float.MinValue;
            var tmp = lb != null ? lb.GetComponentInChildren<TMPro.TextMeshPro>(true) : null;
            if (tmp == null) return false;
            var ti = tmp.textInfo;
            if (ti == null || ti.characterInfo == null || ti.meshInfo == null) return false;
            int n = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
            bool any = false;
            for (int ci = 0; ci < n; ci++)
            {
                var ch = ti.characterInfo[ci];
                if (!ch.isVisible) continue;
                int mi = ch.materialReferenceIndex;
                if (mi < 0 || mi >= ti.meshInfo.Length) continue;
                var vm = ti.meshInfo[mi].vertices;
                if (vm == null) continue;
                int v = ch.vertexIndex;
                if (v < 0 || v + 3 >= vm.Length) continue;
                for (int k = 0; k < 4; k++)
                {
                    var p = DeckRuntime.PxOfWorld(tmp.transform.TransformPoint(vm[v + k]));
                    if (p.x < x1) x1 = p.x;
                    if (p.x > x2) x2 = p.x;
                    if (p.y < y1) y1 = p.y;
                    if (p.y > y2) y2 = p.y;
                    any = true;
                }
            }
            return any;
        }

        /// <summary>🆕 **2026-10-10（A224②）**：抽屉里**现在建着几个格子节点**（按节点名数 `flt_cell`）。
        /// <para>🔴 数的是**树里的节点**，⛔ 不是 `DeckRuntime` 那本账（`UiFilterCellObjects`）——
        /// 本条的判据是「节点**真建出来了**」，账本对不对不是它要盯的东西。</para>
        /// <para>⚠️ `includeInactive: true`：整块滚出带口的那些格子是**建着但不画**（`SetActive(false)`，
        /// 见 `DeckRuntime.ClipCellToBand`）—— 它们**要算进来**（原版全量 `Instantiate` 也不管它在不在视口里）。</para>
        /// </summary>
        static int CountDrawerCellQuads()
        {
            int n = 0;
            if (_root == null) return 0;
            foreach (var q in _root.GetComponentsInChildren<ImageQuad>(true))
                if (q != null && q.name == "flt_cell") n++;
            return n;
        }

        // ============================================================ 自检

        public static void Run()
        {
            _pass = 0; _fail = 0; _failures.Clear();
            Directory.CreateDirectory(ShotDir);
            Debug.Log(P + "=== 卡组编辑自检 开始 ===");

            Section("状态：卡池与筛选");
            TestFilters();
            TestWarlordGatedPool();

            Section("状态：加牌 / 删牌 / 督军");
            TestEditing();

            Section("状态：费用曲线与滚动窗口");
            TestCurveAndScroll();

            Section("状态：遭遇模式（模式**从正在编辑的那副卡组派生**）");
            TestSkirmishMode();

            Section("版面");
            var tmpPath = TempStorePath();
            RuleEngine.DeckStore.OverridePath = tmpPath;
            RuleEngine.DeckStore.DeleteFile();
            DeckEditorState state;
            try
            {
                state = Build(DeckLibrary.Load(), out _root);
                // 🔴 2026-09-20：**根上必须挂着 `DeckRuntime`** —— 场景存盘后按 Play 就是靠它的
                //    `Start()` 建界面；忘了挂组件 = Play 出来一片黑，而这条断言能挡住。
                //    （铁律 10 第 5 条：`Build` 直调与 Play 的 `Start()` 是两个入口，**各配一条断言**。）
                CheckTrue(_root != null && _root.GetComponent<DeckRuntime>() != null,
                          "根对象上挂着 DeckRuntime（按 Play 的入口）");
                TestLayout(state);
                TestLibraryWiring();
                // 🔴 2026-09-20：**Play 那条入口也验一次** —— 按 Play 时是 `DeckRuntime.Start()`
                //    调 `Build`，而自检走的是 `DeckScene.Build()` 直调（**两条路**；铁律 10 第 5 条
                //    要求每个入口各配一条断言）。这里 new 一个 `DeckRuntime` 调同一个 `Build`，
                //    把「Play 那条路建得起来」验掉（`Start()` 里就一行 `if (State == null) Build(...)`）。
                var smokeGo = new GameObject("PlayEntrySmoke");
                var smoke = smokeGo.AddComponent<DeckRuntime>();
                smoke.Build(DeckLibrary.Load());
                CheckTrue(smoke.State != null && smoke.State.PoolCount > 1000,
                          $"Play 入口（DeckRuntime.Build）也建得起来（卡池 {smoke.State?.PoolCount} 张）");
                UnityEngine.Object.DestroyImmediate(smokeGo);

                // 🔴 2026-09-23：**「从收藏进编辑」的交接**（`CollectionData.PendingEditDeck`）——
                //    收藏窗点「编辑」时写它，`DeckRuntime.Build` 开局读掉并清掉
                //    （正本 `资料/阶段二_卡组线_原版规格.md` §七；原版是同窗换页，我们是两个场景）。
                {
                    var lib2 = DeckLibrary.Load();
                    while (lib2.Count < 2) lib2.Create("交接测试 " + (lib2.Count + 1));
                    lib2.Save();
                    const int want = 1;
                    CardPresentation.CollectionData.PendingEditDeck = want;
                    var hgo = new GameObject("HandoffSmoke");
                    var hrt = hgo.AddComponent<DeckRuntime>();
                    hrt.Build(lib2);
                    Check(lib2.CurrentIndex, want,
                          $"交接下标 {want} ⇒ 编辑器**开局就打开第 {want + 1} 套**（「从收藏进编辑」的判据）");
                    Check(CardPresentation.CollectionData.PendingEditDeck, -1,
                          "交接**读完就清**（不清的话下次开还会跳过去）");
                    hrt.BackToMenu();
                    CheckTrue(true, "`BackToMenu()` 在批处理下**不切场景**（切了会把后面的断言全带走）");
                    UnityEngine.Object.DestroyImmediate(hgo);
                }

                // 🆕 2026-09-26：**遭遇模式在界面那一层也走对了**。
                //    状态层那条（`TestSkirmishMode`）只证明「`MaxDeckCount` 会变成 12」；
                //    这一条证明**界面上真的按模式画** —— 脚注计数画的是 `State.MaxDeckCount + 2`
                //    （+2 = 督军 + 防御卡各占一格），这副牌故意只放 12 张普通卡 ⇒ 应显示 `12/14`。
                //    出处（模式为什么从这副牌来）→ `资料/加时与冲突模式_原版规格.md` §2.7。
                {
                    var lib3 = DeckLibrary.Load();
                    var pool3 = RuleEngine.CardDatabase.Load();
                    // ⚠️ **挑阵营要挑得动**：得有督军 + 有防御卡 + **凑得出 12 张同阵营部队**
                    //    （第一版只取「池里第一个督军」，结果那个阵营部队不够 12 张、也没防御卡 ⇒ 夹具自己错了）
                    var heroByFac = new Dictionary<string, RuleEngine.CardDef>();
                    var defByFac = new Dictionary<string, RuleEngine.CardDef>();
                    foreach (var c in pool3)
                    {
                        if (c == null || string.IsNullOrEmpty(c.Faction)) continue;
                        if (c.Type == "hero" && !heroByFac.ContainsKey(c.Faction)) heroByFac[c.Faction] = c;
                        if (c.Type == "defence" && !defByFac.ContainsKey(c.Faction)) defByFac[c.Faction] = c;
                    }
                    string fac3 = null;
                    var ids3 = new List<string>();
                    foreach (var kv in heroByFac)
                    {
                        if (!defByFac.ContainsKey(kv.Key)) continue;
                        var tryIds = new List<string>();
                        foreach (var c in pool3)
                        {
                            if (tryIds.Count >= 12 || c == null || c.Type != "unit" || c.Faction != kv.Key) continue;
                            for (int k = 0; k < RuleEngine.DeckRules.CopyLimit(c.Rarity) && tryIds.Count < 12; k++)
                                tryIds.Add(c.Id);
                        }
                        if (tryIds.Count == 12) { fac3 = kv.Key; ids3 = tryIds; break; }
                    }
                    CheckTrue(fac3 != null,
                              "池子里有一个阵营凑得出 12 张的遭遇牌（督军 + 防御卡 + 12 张部队）—— 有它才验得了界面");

                    var made3 = lib3.Create("遭遇规则测试", (int)RuleEngine.GameMode.Skirmish);
                    if (fac3 != null)
                    {
                        made3.WarlordId = heroByFac[fac3].Id;
                        made3.DefensiveId = defByFac[fac3].Id;
                        made3.CardIds.AddRange(ids3);
                        lib3.Save();
                        lib3.Select(lib3.Count - 1);
                        Check(made3.CardIds.Count, 12, $"夹具凑到的就是 12 张（阵营 {fac3}）");
                    }

                    var sgo = new GameObject("SkirmishModeSmoke");
                    var srt = sgo.AddComponent<DeckRuntime>();
                    srt.Build(lib3);
                    CheckTrue(srt.State != null && srt.State.Skirmish,
                              "★ 编辑器打开一副**遭遇**牌 ⇒ 状态按遭遇规则（`DeckEditorState.Skirmish` 是**从这副牌派生**的）");
                    Check(srt.State.MaxDeckCount, 12, "上限 12 张");
                    var cnt3 = srt.transform.Find("foot_cnt");
                    CheckTrue(cnt3 != null, "脚注计数节点（`foot_cnt`）在");
                    if (cnt3 != null)
                    {
                        var lbl3 = cnt3.GetComponent<Label>();
                        string txt3 = lbl3 != null ? lbl3.Text : null;
                        // 🔴 **2026-09-28 改判据：脚注只数【卡组里那 N 张】，不再把督军/防御卡算进去。**
                        //   判据 = 原版实拍（`卡组编辑界面参考.png`）右下角印 **`30/30`**，而那一屏的卡表里
                        //   **只有卡组的 30 张**（督军在标题行、防御卡在自己的格子里）⇒ 原来那个
                        //   `(卡数 + 督军 + 防御卡) / (MaxDeckCount + 2)` 会印成 **32/32**，**多 2**。
                        //   出处：`资料/历史/五张参考图_逐件核对_0922.md` 第 10 条（挂着「要核」，已核完）。
                        //   ⇒ 遭遇模式这副满牌 = **12/12**（经典满编会画 **30/30**）—— **分母仍是模式的证据**。
                        Check(txt3, "12/12",
                              "★ 界面按遭遇画：**12/12** = 卡组那 12 张 / 遭遇上限 12"
                            + " —— 经典会画成 32/32 ⇒ 分母就是「模式真的传到了界面」的证据；"
                            + "这一条挡住「状态层改对了、界面还写死 30」那种半截活");
                    }
                    UnityEngine.Object.DestroyImmediate(sgo);
                }
            }
            finally
            {
                RuleEngine.DeckStore.OverridePath = null;
                try { if (System.IO.File.Exists(tmpPath)) System.IO.File.Delete(tmpPath); } catch { }
            }

            // 🔴 **2026-10-12（A364）**：**先拆窗口层**再拍图 + 存场景 —— `SaveScene()` 会把**非激活**的
            //    GameObject 一起写进 `DeckEditor.unity`，而那扇窗是运行期拼的（理由见 `DestroyPopupScaffold`）。
            //    ⚠️ 这一步也得在那张收尾截图**之前**：留一扇模态窗在画面上会让 `deck_editor.png` 变成误导。
            int torn = DestroyPopupScaffold();
            Debug.Log(P + $"  拆掉模态窗那条链留下的场景对象 **{torn}** 件（A364）");
            Shoot("deck_editor.png");
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

        static DeckEditorState NewState()
        {
            var pool = CardDatabase.Load();
            return new DeckEditorState(pool);
        }

        /// <summary>🆕 **2026-09-27（用户给的规格）**：卡组编辑的卡池**按督军分流** ——
        /// 「卡组没有督军的时候，右边卡库显示的是各个阵营的督军，放入督军后，右边卡库显示该阵营的卡牌」。
        /// ⚠️ **只有卡组编辑开这个开关**（`WarlordGatedPool`）；收藏窗与上面那些纯逻辑用例都走 false。</summary>
        static void TestWarlordGatedPool()
        {
            var s = new DeckEditorState(CardDatabase.Load());
            s.WarlordGatedPool = true;

            var noWl = s.VisibleCards();
            CheckTrue(noWl.Count > 0, $"**没督军**时卡池里有东西（{noWl.Count} 个）");
            var facs = new System.Collections.Generic.HashSet<string>();
            bool allHero = true;
            foreach (var c in noWl)
            {
                if (c.Type != "hero")
                { Check(true, false, $"没督军时卡池里混进了非督军：`{c.Name}`（{c.Type}）"); allHero = false; break; }
                facs.Add(c.Faction ?? "");
            }
            if (allHero) CheckTrue(true, $"没督军时**全是督军**（{noWl.Count} 张）");
            CheckTrue(facs.Count >= 10, $"**各阵营的督军都在**（覆盖 {facs.Count} 个阵营 —— 不是只列某一个）");

            CardDef w = noWl[0];
            s.SetWarlord(w.Id);
            var seen = s.VisibleCards();
            CheckTrue(seen.Count > 0, $"定了督军（`{w.Name}` / {w.Faction}）后卡池里有东西（{seen.Count} 个）");
            bool hasHero = false, hasDef = false, okFac = true;
            foreach (var c in seen)
            {
                if (!DeckRules.SameFaction(c.Faction, w.Faction))
                { Check(true, false, $"定了督军后混进了别的阵营：`{c.Name}`（{c.Faction} ≠ {w.Faction}）"); okFac = false; break; }
                if (c.Type == "hero") hasHero = true;
                if (c.Type == "defence") hasDef = true;
            }
            if (okFac) CheckTrue(true, $"定了督军后卡池**只剩该阵营**（{seen.Count} 张）");
            CheckTrue(hasHero, "该阵营**其它督军还在**（玩家可以换督军）");
            CheckTrue(hasDef, "该阵营的**防御卡在卡池里**（玩家要从这儿挑防御卡 —— 用户 2026-09-27）");

            // 🆕 **2026-09-27（用户拍板）：效果生成的卡（药剂/破坏/秘仪）不进卡池**
            //   帝王之子有 6 张药剂、暗黑天使有 5 张秘仪 —— 选这两家的督军，池子里一张都不该有。
            foreach (var probe in new[] { "EmperorsChildren", "DarkAngels" })
            {
                CardDef hw = null;
                var ph = s.Pool;
                for (int i = 0; i < ph.Count; i++)
                    if (ph[i].Type == "hero" && ph[i].Faction == probe) { hw = ph[i]; break; }
                if (hw == null) { Check(true, false, $"卡池里找不到 {probe} 的督军"); continue; }
                s.SetWarlord(hw.Id);
                int eff = 0; string firstEff = "";
                foreach (var c in s.VisibleCards())
                    if (DeckRules.IsEffectOnly(c.Subtype)) { eff++; if (firstEff.Length == 0) firstEff = c.Name; }
                Check(eff, 0, $"★ {probe} 的卡池里**没有效果生成的卡**（药剂/破坏/秘仪）"
                            + $"—— 现在有 {eff} 张" + (eff > 0 ? $"（如 `{firstEff}`）" : ""));
            }
            // 而且**加不进去**（负例：绕开卡池直接 TryAdd 一张药剂）
            {
                CardDef elix = null;
                var pe = s.Pool;
                for (int i = 0; i < pe.Count; i++) if (pe[i].Subtype == "Combat Elixir") { elix = pe[i]; break; }
                if (elix == null) Check(true, false, "卡池里找不到药剂卡（这一条没法验）");
                else
                {
                    string why;
                    bool added = s.TryAdd(elix, out why);
                    Check(added, false, $"★ 直接 `TryAdd` 一张药剂 `{elix.Name}` ⇒ **被拒**（{why}）"
                                      + " —— 用户 2026-09-27：药剂/破坏卡不能加入卡组");
                }
            }

            s.ClearWarlord();
            Check(s.VisibleCards().Count, noWl.Count, "清掉督军 ⇒ 卡池回到「只列各阵营督军」");

            // 🆕 **督军按阵营分组**（用户 2026-09-27 问「总不能混乱地排吧」）——
            //   判据：**同一个阵营的督军必须连成一段**，不许 A→B→A 这种交错。
            {
                var list = s.VisibleCards();
                int breaks = 0; string last = null;
                var closed = new System.Collections.Generic.HashSet<string>();
                foreach (var c in list)
                {
                    var f = c.Faction ?? "";
                    if (f != last) { if (!closed.Add(f)) breaks++; last = f; }
                }
                Check(breaks, 0, $"★ 督军**按阵营分组**、没有交错（重复出现的阵营段 = {breaks}；共 {list.Count} 位督军）");
            }
        }

        static void TestFilters()
        {
            var s = NewState();
            CheckTrue(s.PoolCount > 1000, $"卡池加载到 {s.PoolCount} 张");

            var all = s.VisibleCards().Count;
            Check(all, s.PoolCount, "不设筛选时命中全部");

            var f = DeckFilter.None; f.Faction = "Ultramarines";
            s.SetFilter(f);
            int ult = s.VisibleCards().Count;
            CheckTrue(ult > 0 && ult < all, $"按阵营筛 → {ult} 张（少于全部）");
            foreach (var c in s.VisibleCards())
                if (!DeckRules.SameFaction(c.Faction, "Ultramarines")) { Check(true, false, "筛出来的卡阵营不对"); break; }

            f = DeckFilter.None; f.Rarity = "legendary"; f.Faction = "Ultramarines";
            s.SetFilter(f);
            int leg = s.VisibleCards().Count;
            CheckTrue(leg > 0 && leg < ult, $"再加稀有度筛 → {leg} 张（更少）");

            f = DeckFilter.None; f.Cost = 3; f.Faction = "Ultramarines";
            s.SetFilter(f);
            foreach (var c in s.VisibleCards())
                if (c.Cost != 3) { Check(true, false, "费用筛选没生效"); break; }
            CheckTrue(s.VisibleCards().Count > 0, "按费用 3 筛能得到卡");

            f = DeckFilter.None; f.Name = "autarch";
            s.SetFilter(f);
            CheckTrue(s.VisibleCards().Count >= 1, "按卡名子串筛（大小写不敏感）能命中 Autarch");

            f = DeckFilter.None; f.Type = "hero";
            s.SetFilter(f);
            // ⚠️ 这个数是**卡池里 hero 的总数**（不是我们挑了 57 个）。2026-09-12 从 57 变 56：
            //    卡表过滤器把噪音卡剔掉了，其中 `HB`（= Imotekh the Stormlord 的异画重复条目）是 hero。
            CheckTrue(s.VisibleCards().Count == 56, $"按类型筛出 56 个督军（实际 {s.VisibleCards().Count}）");

            s.SetFilter(DeckFilter.None);
            Check(s.VisibleCards().Count, s.PoolCount, "清掉筛选又回到全部");

            CheckTrue(s.Factions().Count == 13, $"阵营下拉有 13 项（实际 {s.Factions().Count}）");
            CheckTrue(s.Costs().Count > 0, "费用下拉非空");
        }

        static void TestEditing()
        {
            var s = NewState();
            CardDef warlord = null, unitA = null, unitB = null, tactic = null, def = null;
            foreach (var c in s.Pool)
            {
                if (warlord == null && c.Type == "hero") warlord = c;
            }
            foreach (var c in s.Pool)
            {
                if (!DeckRules.SameFaction(c.Faction, warlord.Faction)) continue;
                // ⚠️ 专门挑**非传说**的单位卡来测「同名 2 张」—— 传说卡上限是 1，拿它测必然失败
                if (unitA == null && c.Type == "unit" && !DeckRules.IsLegendary(c.Rarity)) unitA = c;
                else if (unitB == null && c.Type == "unit" && c.Id != unitA?.Id) unitB = c;
                if (tactic == null && c.Type == "tactic") tactic = c;
                if (def == null && c.Type == "defence") def = c;
            }
            CheckTrue(warlord != null && unitA != null && unitB != null && def != null,
                      $"找到测试用的卡（督军 {warlord?.Name} / 单位 {unitA?.Name} / 防御 {def?.Name}）");

            string why;
            CheckTrue(s.TryAdd(warlord, out why), $"加督军（{why}）");
            Check(s.Deck.WarlordId, warlord.Id, "督军进了督军位，不是普通卡位");
            Check(s.DeckCount, 0, "督军不占 30 张的名额");

            s.TryAdd(def, out why);
            Check(s.Deck.DefensiveId, def.Id, "防御卡进了防御位，也不占名额");

            CheckTrue(s.TryAdd(unitA, out why), $"加单位卡（{why}）");
            CheckTrue(s.TryAdd(unitA, out why), $"同名第 2 张能加（{why}）");
            Check(s.DeckCount, 2, "卡组里 2 张");
            bool okThird = s.TryAdd(unitA, out why);
            CheckTrue(!okThird, $"同名第 3 张被挡（{why}）");
            CheckTrue(!string.IsNullOrEmpty(why), "被挡时给出了人话原因");

            // 传说卡：第 2 张就该被挡
            CardDef legendary = null;
            foreach (var c in s.Pool)
                if (c.Type == "unit" && DeckRules.SameFaction(c.Faction, warlord.Faction)
                    && DeckRules.IsLegendary(c.Rarity) && c.Id != unitA.Id) { legendary = c; break; }
            if (legendary != null)
            {
                s.TryAdd(legendary, out why);
                CheckTrue(!s.TryAdd(legendary, out why), $"传说卡第 2 张被挡（{why}）");
            }

            // 别阵营的卡
            CardDef other = null;
            foreach (var c in s.Pool)
                if (c.Type == "unit" && !DeckRules.SameFaction(c.Faction, warlord.Faction)) { other = c; break; }
            if (other != null)
                CheckTrue(!s.TryAdd(other, out why), $"别阵营的卡被挡（{why}）");

            // 换督军会清掉不合阵营的卡
            CardDef otherWarlord = null;
            foreach (var c in s.Pool)
                if (c.Type == "hero" && !DeckRules.SameFaction(c.Faction, warlord.Faction)) { otherWarlord = c; break; }
            if (otherWarlord != null)
            {
                int before = s.DeckCount;
                int removed = s.SetWarlord(otherWarlord.Id);
                Check(s.Deck.WarlordId, otherWarlord.Id, "换督军成功");
                Check(s.DeckCount, 0, "换督军把不合阵营的卡清空了");
                CheckTrue(removed > 0, $"并报告清掉了几张（{removed} 张，原来是 {before} 张）");
                CheckTrue(string.IsNullOrEmpty(s.Deck.DefensiveId), "防御卡也被清掉了（阵营不合）");
            }

            // 删
            s.SetWarlord(warlord.Id);
            s.TryAdd(unitA, out why);
            s.TryAdd(unitA, out why);
            Check(s.Deck.CountOf(unitA.Id), 2, "加回 2 张");
            CheckTrue(s.TryRemove(unitA), "删掉 1 张");
            Check(s.Deck.CountOf(unitA.Id), 1, "还剩 1 张");
            CheckTrue(s.TryRemove(unitA), "再删 1 张");
            CheckTrue(!s.TryRemove(unitA), "已经没有了 → 删不动");

            // 正常路径下加满 30 张 → 第 31 张被挡
            s.ClearCards();
            s.SetWarlord(warlord.Id);
            s.TryAdd(def, out why);
            int guard = 0;
            foreach (var c in s.Pool)
            {
                if (s.DeckCount >= s.MaxDeckCount || guard > 5000) break;
                for (int i = 0; i < DeckRules.CopyLimit(c.Rarity); i++) { s.TryAdd(c, out why); guard++; }
            }
            Check(s.DeckCount, s.MaxDeckCount, $"能加满 {s.MaxDeckCount} 张");
            var overflow = s.Find(unitA.Id);
            CheckTrue(!s.TryAdd(overflow, out why), $"满 30 张后第 31 张被挡（{why}）");
        }

        static void TestCurveAndScroll()
        {
            var s = NewState();
            var curve = s.CostCurve();
            Check(curve.Length, 21, "费用曲线数组是 0..20");
            int sum = 0; foreach (var n in curve) sum += n;
            Check(sum, 0, "空卡组曲线全 0");

            CardDef unit = null;
            foreach (var c in s.Pool) if (c.Type == "unit" && c.Cost <= 20) { unit = c; break; }
            string why;
            for (int i = 0; i < DeckRules.CopyLimit(unit.Rarity); i++) s.TryAdd(unit, out why);
            curve = s.CostCurve();
            Check(curve[unit.Cost], DeckRules.CopyLimit(unit.Rarity),
                  $"曲线在费用 {unit.Cost} 上记到 {DeckRules.CopyLimit(unit.Rarity)} 张");

            // 🔴 卡池是**滚动窗口**不是翻页（原版 `RecyclableScrollRect`；我们自加的分页 2026-09-20 已删）
            CheckTrue(s.VisibleAt(0) != null, "按序号能取到第 0 张");
            CheckTrue(s.VisibleAt(-1) == null, "序号越界给 null（不是抛异常）");
            CheckTrue(s.VisibleAt(s.PoolCount) == null, "越过末尾也给 null");

            var f = DeckFilter.None; f.Type = "hero";
            s.SetFilter(f);
            CheckTrue(s.VisibleAt(0) != null && s.VisibleAt(0).Type == "hero",
                      "换了筛选，窗口的第 0 张就是筛出来的那张");
            s.SetFilter(DeckFilter.None);
            Check(s.VisibleCards().Count, s.PoolCount, "清空筛选回到全部");
        }

        /// <summary>🆕 2026-09-26：**编辑器的「模式」是从正在编辑的那副卡组派生出来的**。
        ///
        /// 为什么单独一节：`DeckEditorState.Skirmish` 原来是**全仓没有任何赋值点的裸字段**
        /// ⇒「玩家自建遭遇卡组」那条路**静默走不通**（编辑器恒按经典 30 张算）。
        /// 原版也是这样：卡组编辑器不认识「当前模式」，它读 `EditingDeck.GameMode`
        /// （`DeckEditingWindow._GetCardCollection` 那条链，判据 → `资料/加时与冲突模式_原版规格.md` §2.7）。
        ///
        /// 🔴 这一节**只验「派生」**（换一副牌规则就跟着换）—— 那是它跟裸字段最本质的区别；
        /// 「模式怎么进到卡组里」另有断言（`RuleEngineTest.TestDeckGameMode` / `BattleScene` 第 9c 节）。</summary>
        static void TestSkirmishMode()
        {
            var s = NewState();

            s.NewDeck("经典牌");
            CheckTrue(!s.Skirmish, "新建一副**不传模式**的牌 ⇒ 经典");
            Check(s.MaxDeckCount, 30, "经典上限 30 张");
            Check(s.SlotsLeft, 30, "空牌差 30 张");

            s.NewDeck("遭遇牌", (int)RuleEngine.GameMode.Skirmish);
            CheckTrue(s.Skirmish, "★ 新建一副**遭遇牌** ⇒ 编辑器自己就是遭遇规则（没有谁去给它赋值）");
            Check(s.MaxDeckCount, 12, "★ 遭遇上限 **12** 张（`GameplayVariables` 那套值）");
            Check(s.SlotsLeft, 12, "空牌差 12 张");

            // ★ **派生的证据**：把另一副牌装进来，规则立刻跟着换
            s.LoadDeck(new RuleEngine.PlayerDeck("切回经典", null, null, null));
            CheckTrue(!s.Skirmish, "★ 装上另一副**经典**牌 ⇒ 立刻回到经典规则（说明是派生，不是「谁记得赋值」）");
            Check(s.MaxDeckCount, 30, "上限跟着回到 30");

            // 12 张这条线真的**卡得住**：加满 12 张之后第 13 张要被挡
            var pool = s.Pool;
            CardDef hero = null;
            foreach (var c in pool) if (c.Type == "hero") { hero = c; break; }
            CheckTrue(hero != null, "池子里找得到一张督军");
            if (hero == null) return;

            var ids = new List<string>();
            foreach (var c in pool)
            {
                if (ids.Count >= 12 || c == null || c.Type != "unit" || c.Faction != hero.Faction) continue;
                for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && ids.Count < 12; i++) ids.Add(c.Id);
            }
            Check(ids.Count, 12, $"凑得出一套 12 张的遭遇牌（阵营 {hero.Faction}）");

            s.LoadDeck(new RuleEngine.PlayerDeck("满的遭遇牌", hero.Id, null, ids,
                                                 (int)RuleEngine.GameMode.Skirmish));
            CheckTrue(s.Skirmish, "装进来还是遭遇");
            Check(s.SlotsLeft, 0, "★ **12 张正好装满**（差 0 张）");
            Check(s.MaxDeckCount, 12, "上限还是 12（没被 30 那套盖掉）");

            CardDef extra = null;
            foreach (var c in pool)
                if (c != null && c.Type == "unit" && c.Faction == hero.Faction && s.Deck.CountOf(c.Id) == 0)
                { extra = c; break; }
            if (extra != null)
                Check(s.CanAdd(extra), DeckError.TooManyCards,
                      "★ 遭遇牌满 12 张之后**再加一张会被挡**（`TooManyCards`）—— 上限真的生效了");

            // 校验走**遭遇那套账**：上限 12（不是 30）。
            // 🔴 **2026-09-26 改口径**：防御卡改成**可选**（照原版 `DeckUtility.ValidateDeck` 不读那一格），
            //    ⇒ 这副「12 张 + 没带防御卡」**合法**。原来这条断的是 `DefensiveMissing`（我们比原版严）。
            //    为了**仍然钉住「按模式算张数」**，下面两头都断：正好 12 张 ⇒ 合法；多一张 ⇒ `TooManyCards`。
            //    判据 → `资料/加时与冲突模式_原版规格.md` §2.7c。
            Check(s.Validate(), DeckError.None,
                  "★ 遭遇：**正好 12 张 + 没带防御卡 ⇒ 合法**（防御卡可选，照原版）");
            var over = new PlayerDeck(s.Deck.Name, s.Deck.WarlordId, null,
                                      new List<string>(s.Deck.CardIds), (int)GameMode.Skirmish);
            if (extra != null) over.CardIds.Add(extra.Id);
            Check(DeckRules.Validate(over, s.Find, true), DeckError.TooManyCards,
                  "★ ……而**多一张就超**（`TooManyCards`）⇒ 上限确实是**遭遇那套 12**，不是经典的 30");
        }

        /// <summary>卡组库接进场景之后，这几件事必须成立。</summary>
        static void TestLibraryWiring()
        {
            var lib = DeckLibrary.Load();
            CheckTrue(lib.Count >= 1, $"空库时场景会替玩家建一套（现在 {lib.Count} 套）");
            CheckTrue(lib.Current != null, "有选中的卡组");
            CheckTrue(lib.Current.CardIds.Count > 0 || lib.Current.WarlordId != null,
                      "建出来的演示卡组不是空的");

            var before = lib.Current.Name;
            int n0 = lib.Count;
            lib.Create("第二套");
            Check(lib.Count, n0 + 1, "新建一套后库里多了一条");
            var lib2 = DeckLibrary.Load();
            Check(lib2.Count, n0 + 1, "**新建立刻落盘**（重新载入还在）");
            Check(lib2.Current.Name, "第二套", "选中项也落盘了");

            lib2.Select(0);
            lib2.Delete(0);
            Check(DeckLibrary.Load().Count, n0, "删除也落盘了");
            CheckTrue(!string.IsNullOrEmpty(before), "（原卡组名非空，便于下面对账）");
        }

        static DeckRuntime _rt;

        static void TestLayout(DeckEditorState state)
        {
            // ⚠️ 只数**可见**的图（`false` = 不含 inactive）—— 卡组行与费用曲线是互斥视图
            //    （Cards 页签 / Deck info 页签），同时算进来会误报「重叠」。
            var quads = _root.GetComponentsInChildren<ImageQuad>(false);
            CheckTrue(quads.Length >= 20, $"画出来的可见图至少有 20 张（实际 {quads.Length}）");

            // 版面：所有可见的图都在可见区里
            // 🔴 **2026-10-05（A76③）把这条「提醒」核成了明确结论：`localPosition` 在这里【仍然对】。**
            //   理由（三句，都能就地复核）：
            //   ① 所有 quad 都是 `Pos(px,py)` 摆的，而 `Pos` 给的就是**相对 `Root` 的世界单位**
            //      （`(px − 960)/108`）；`halfW/halfH` 也是世界单位（`VisibleWidth*0.5` / `DesignHeight*0.5`）
            //      ⇒ 两边同量纲（`A41 ③` 那次**单位混了**的错在下面那条「同层重叠」上，这里没有）。
            //   ② `Root` = 场景根 `DeckEditor`（`new GameObject`，位置原点、无父）⇒ 根的子物体
            //      `localPosition == 它相对 Root 的位置`。
            //   ③ **抽屉有了容器之后也不受影响**：两个抽屉容器由 `NewDrawer` 建，**出场 `localPosition` 就是
            //      `Vector3.zero`**，而滑动只改**容器自己**的 x（子物体的 `localPosition` 一个都不动）
            //      ⇒ 这条检查量的是「这一件在版面里的位置」，连「滑出去那一头」都不会把它算成屏外。
            // ⚠️ **两条已知的窄口（今天都是空集，写在这里是因为它们会随新写法重新出现）**：
            //   · **九宫格的 9 块**：它们的父是九宫格**根**（在 `Pos(...)` 上），自己的 `localPosition` 是
            //     根内的**小偏移**（±半个宽高）⇒ 恒在可见区内，这条对它们**等于空转**
            //     （根自己身上**没有 `ImageQuad`** ⇒ 也不在扫描范围内）。判据：`ImageQuad.CreateNineSlice`
            //     只给 9 个子块 `Create`，根是个空的 `GameObject`。
            //   · **将来若有人建一个 `localPosition != 0` 的容器**，它子树里那些件的 `localPosition`
            //     就**不再是版面坐标**了 —— 那时这条会**静默地量错东西**（不报错、也不红）。
            //     ⇒ 判据：新建容器时照 `NewDrawer` 那样**出场放在原点**（要挪就挪它的子件）。
            //     ⛔ 本条**不改成世界坐标**：世界坐标版在抽屉滑动期间会跟着父级一起动（同一件东西会有两个读数），
            //       而且今天**没有任何一件**能靠它多抓出来（九宫格根没有 quad、9 块又恒在内）。
            float halfW = LayoutSpace.VisibleWidth * 0.5f, halfH = LayoutSpace.DesignHeight * 0.5f;
            int off = 0; string firstOff = null;
            foreach (var q in quads)
            {
                // ⚠️ **卡池那一列是可滚动内容** ⇒ 排除掉：第 3 行只露头，它底下那条张数（`poolbar_*`）
                //    本来就在屏幕外（滚上来才看得到）。卡池的**卡**是 `CardView`（不是 `ImageQuad`），
                //    这条从来没扫到过它们；2026-09-28 新加的张数条是 `ImageQuad` ⇒ 不排除就会一直误报
                //    （实测第一处 `poolbar_12` = 第 3 行那张）。
                if (q.name != null && q.name.StartsWith("poolbar_")) continue;
                var p = q.transform.localPosition;
                if (Mathf.Abs(p.x) > halfW + 0.01f || Mathf.Abs(p.y) > halfH + 0.01f)
                { off++; if (firstOff == null) firstOff = q.name; }
            }
            Check(off, 0, "所有可见 UI 图都落在可见区内（没有跑到屏幕外）" +
                  (firstOff == null ? "" : "—— 第一处 " + firstOff));

            // ⚠️ 版面回归用的通用检查：**同一层的两个图不许压在一起**。
            //    分层 = `SetRenderQueue`（**不是 z** —— 透明队列按到相机的 3D 距离排序，
            //    铺满屏的图会互相盖错，2026-09-20 实测：侧栏底板盖住了整个卡组列表）。
            // 🔴🔴 **2026-10-04（A41 ③）：这条判据以前【单位混了 ⇒ 恒绿】** ——
            //    左边 `DeckRuntime.PxOfWorld(...)` 给的是 **px**，右边 `A.WorldW/B.WorldW` 是**世界单位**
            //    （1 世界单位 = 108px）⇒ 阈值实际只有 ~1–3px ⇒ **正常尺寸的重叠一个都抓不到**。
            //    修法 = 右边一律 `* PxPerUnit`；容差同时从 `1e-3` 放大到 `LayTol`（px）——
            //    本批新加宽的页签格**边缘恰好相接**（余量本来就只有 1e-3 px 那一档），太紧会误报。
            const float LayTol = 0.05f;      // px：小于它的「重叠」不报（浮点噪声），真重叠照报
            int overlaps = 0; var pairs = new List<string>();
            int sameQueue = 0;
            float tightest = float.MaxValue; string tightestPair = null;
            for (int i = 0; i < quads.Length; i++)
                for (int j = i + 1; j < quads.Length; j++)
                {
                    var A = quads[i]; var B = quads[j];
                    if (A.RenderQueue != B.RenderQueue) continue;
                    // 🔴 用**世界坐标**（`PxOfWorld`）而不是 `localPosition` ——
                    //    九宫格的 9 块是**根的子物体**，它们的 localPosition 都在根附近
                    //    ⇒ 用 local 会把每一行的块都判成「同一处」，660 处假重叠（2026-09-23 踩）。
                    var pa = DeckRuntime.PxOfWorld(A.transform.position);
                    var pb = DeckRuntime.PxOfWorld(B.transform.position);
                    float aw = A.WorldW * PxPerUnit, ah = A.WorldH * PxPerUnit;
                    float bw = B.WorldW * PxPerUnit, bh = B.WorldH * PxPerUnit;
                    float gx = Mathf.Abs(pa.x - pb.x) - (aw + bw) * 0.5f;   // > 0 = 两轴还要再靠近这么多才碰上
                    float gy = Mathf.Abs(pa.y - pb.y) - (ah + bh) * 0.5f;
                    sameQueue++;
                    float gap = Mathf.Max(gx, gy);                          // < 0 ⇒ 真的重叠了这么多 px
                    if (gap < tightest) { tightest = gap; tightestPair = A.name + " × " + B.name; }
                    if (gx < -LayTol && gy < -LayTol)
                    { overlaps++; if (pairs.Count < 5) pairs.Add(A.name + " × " + B.name); }
                }
            // （前提）真的比了足够多对 —— 防「循环静默地一对都没比」
            CheckTrue(sameQueue >= 100, $"同层组合真的比了 {sameQueue} 对（少于此数 ⇒ 这条检查其实没在跑）");
            CheckTrue(overlaps == 0, $"同一层的 UI 图没有互相压住（实测 {overlaps} 处" +
                      (pairs.Count == 0 ? "）" : "：" + string.Join(" · ", pairs) + "）"));
            // 🔴 这条是**上面那条判据的尺子**（A41 ③ 的「改坏会红」）：同层挨得最近的一对是
            //    **相邻两个页签格的边缘**，原版把它们做成**恰好相接**（`[0.3,109.2][109.2,218.2]`）
            //    ⇒ px 间隙 = **0**。若右边漏乘 `PxPerUnit`（老写法），这个数会变成 ~109 —— **这里会红**。
            //    期望值 0 出自**原版 rect 相接**，不是从我们的实现里读回来的。
            CheckNear(tightest, 0f, LayTol,
                      $"同层最紧的一对（{tightestPair}）**恰好相接**、px 间隙 = 0"
                      + "（判据 = 原版相邻页签格边缘相接；这条同时是「判据的两个量都是 px」的校准）");

            CheckTrue(state.VisibleCards().Count > 0, "版面用的状态里有卡可显示");

            // ============================================================ 新版面（2026-09-20 按权威坐标重建）
            // 🔴 素材齐不齐 —— **缺图会静默变白**，这条挡住它（`sync_battle_ui_art.py` 那批）
            var missing = _rt.MissingArt;
            Check(missing.Count, 0, "原版 UI 图**一张不缺**（缺了会画成纯白占位）" +
                  (missing.Count == 0 ? "" : "—— 缺 " + string.Join("、", missing)));

            // Header 四件 + 侧栏 + Footer：逐件点验「真的建了」
            foreach (var k in new[] { "hdr_sep", "hdr_back", "hdr_fltbtn", "hdr_clear", "hdr_wcbg", "hdr_army" })
                CheckTrue(_rt.UiHasQuad(k), $"Header 的 `{k}` 建起来了");
            foreach (var k in new[] { "side_bg", "tab_hi0", "tab_ic0", "name_bg", "name_clear", "foot_done", "foot_ic" })
                CheckTrue(_rt.UiHasQuad(k), $"`{k}` 建起来了");
            for (int i = 0; i < 11; i++)
                if (!_rt.UiHasQuad("row_" + i)) { Check(true, false, $"卡组列表第 {i} 行没建起来"); break; }

            // 关键 rect **逐值**对权威表（`_deck_editing_godot_rects.txt`）——
            // 这几个数是「按原版复刻」的硬判据，抄错了这条会红
            CheckRect("hdr_fltbtn", 367.2f, 88.5f, 50f, 50f);
            // ⚠️ `hdr_clear` 的 x **不是** dump 里的 1488.6（那是 VLG 布局前的模板位，落在父容器外）
            //    —— 按「容器内右对齐」= 1468.6 − 250 = 1218.6，理由写在 `DeckRuntime` 那条常量上
            CheckRect("hdr_clear", 1218.6f, 83.5f, 250f, 60f);
            CheckRect("hdr_sep", 167.2f, 151f, 1752.8f, 10f);
            // 🔴 **2026-10-04（A51 F10）：页头分隔线原版也是【九宫格】，我们原来是单块拉伸。**
            //   判据 = dump 那行 `Separator Line [167.2,151]–[1920,161] 1752.83×10 | 40k_main_line 171×6
            //   九宫80,0,80,0 | Sliced ppuMul=0.75`（`Sprite/40k_main_line.json` 同值）。
            //   端帽 = `m_Border 80 ÷ ppuMul 0.75` = **106.6667px**；上下 `m_Border` 都是 **0**
            //   ⇒ 只有**中间那一行** ⇒ 一棵树 **3 块**（不是 9 块）。
            //   ⚠️ 拉伸的代价：1752.8 ÷ 171 = **10.25 倍**横向拉伸 ⇒ 两端 ~80px 的端帽被拉成 ~146px。
            Check(_rt.UiQuadCount("hdr_sep"), 3,
                  "页头分隔线是**九宫格**（原版 `m_Type=Sliced`；上下 border 是 0 ⇒ 只有中间一行、共 3 块）");
            Check(_rt.UiTextureName("hdr_sep"), "40k_main_line", "……图还是那张 `40k_main_line`");
            // 🔴 **2026-10-05（A57 ①）**：`hdr_sep` 是**直接挂在 `Root` 下的九宫格**（`NineSlice` 建，
            //    **不进 `_named`**）⇒ 它正是 `UiQuadActive` 缺兜底时**必答 false** 的那一类、而且**常显**。
            //    期望值 `true` 的出处 = 页头分隔线在卡组编辑界面上**一直画着**（原版 `Separator Line`
            //    `[167.2,151]–[1920,161]`；上面那条 `CheckRect("hdr_sep", …)` 钉着它摆在哪）——
            //    ⛔ 不是从 `UiQuadActive` 自己读回来的。
            //    ⚠️ 这条只验「**不是恒 false**」；「不是恒 true」那一半由上面 `flt_input` 的**关着那条**钉。
            CheckTrue(_rt.UiQuadActive("hdr_sep"),
                      "九宫格件 `hdr_sep`（页头分隔线，3 块、不在 `_named` 里）**读得出「露着」**"
                      + "（A57 ①：只走 `Lookup` 的旧写法在这里恒答 false）");
            // 块名 = `{根}_{i}{j}`（i = 横序号、j = 纵序号）；j=0/2 那两行高为 0 不建 ⇒ 左边那块是 `_01`
            if (_rt.UiQuadRect("hdr_sep/hdr_sep_01", out float spx, out float spy, out float spw, out float sph))
            {
                CheckNear(spw, 106.6667f, 0.6f,
                          "端帽宽 = 原版 `m_Border 80 ÷ ppuMul 0.75` = **106.6667**（不是 80 —— ppuMul 会缩放端帽）");
                CheckNear(sph, 10f, 0.6f, "……高 = 整条高 10（上下 border = 0 ⇒ 那一行就是整条）");
            }
            else Check(true, false, "`hdr_sep_01` 量得到（量不到 ⇒ 那层不是九宫格、退回单块拉伸了）");
            // 🔴 **2026-10-04（A41 ③ 修好单位之后抓出来的真重叠）**：页头分隔线与侧栏底板 / 页签高亮
            //   在 **y 156..161 那 5px** 上是**真重叠**的（逐对算：与 `Info [109.2,218.2]` 重叠 **51×5px**、
            //   与 `Cosmetics [218.2,327.1]` 重叠 **108.9×5px**、与 `side_bg` 168×5px —— 原版 rect 也一样重叠）。
            //   ⚠️ **2026-10-04 订正（R-W4 F7a）**：这句原来把 `Cosmetics` 写成 **160×5px** —— 160 是
            //   **整块 `Buttons`**（`327.1 − 167.2`）与分隔线的重叠，不是 `Cosmetics` 那一格。
            //   原版靠**兄弟序**分先后：`Content Area` 的孩子是 `Background → Header → Sidebar → …`
            //   ⇒ **侧栏及其页签压在分隔线之上**。我们原来三者（含分隔线）分别在 `QPanel`/`QSide`，
            //   方向还反着（分隔线压在侧栏上）⇒ 现在分隔线单开最底层（`QSep`）。
            //   这条断言盯的就是「层序不许再翻回去」。
            CheckTrue(_rt.UiQueueOf("hdr_sep") < _rt.UiQueueOf("side_bg")
                      && _rt.UiQueueOf("side_bg") < _rt.UiQueueOf("tab_hi0"),
                      "层序 = 分隔线 < 侧栏底板 < 页签高亮（= 原版兄弟序 `Header → Sidebar → 页签`；"
                      + $"实测 {_rt.UiQueueOf("hdr_sep")}/{_rt.UiQueueOf("side_bg")}/{_rt.UiQueueOf("tab_hi0")}）");
            CheckRect("side_bg", -203f, 156f, 538.5f, 924.1f);
            CheckRect("name_bg", 9.5f, 311f, 307.7f, 50f);
            // 🔴 **2026-10-04（A24）换图**：卡组名输入框的底图原来用的是 `40K_dropdown_bg`
            //   —— 那张是**导入弹窗**的输入框图（`Import Deck Popup/Window/Input Field`）。
            //   原版这颗的 Image 实测 = **`InputFieldBackground`**（Unity 内置 32×32 · 九宫格 (10,10,10,10)
            //   · ppu=200 · `m_Color=(0.0627,0,0,1)`），同窗 `Card Filters/…/Input Field` 也是这一张。
            //   判据 = prefab 的 Image 组件 + `FilterPanelModel.InputSprite`（那一份是共用的唯一出处）。
            Check(_rt.UiTextureName("name_bg"), "InputFieldBackground",
                  "卡组名输入框的底图 = 原版那张 `InputFieldBackground`（**不是** `40K_dropdown_bg`）");
            CheckTrue(_rt.UiQuadCount("name_bg") >= 9,
                      $"它是**九宫格**（原版 `m_Type=Sliced` + border 10；实测 {_rt.UiQuadCount("name_bg")} 块）");
            CheckRect("foot_done", 13f, 1020.5f, 188.5f, 50.2f);
            // 🔴 **2026-09-27 更正：原来写的是 `50f, 40f`（拉伸），那是错值。**
            //    PA 普查实读：原版 `Content Area/Sidebar/Footer/Image` 是 **PA=1**，
            //    贴图 `40k_general_icon_card_amount` **64×64** 塞进 50×40 ⇒ 原版实绘 **40×40**（**居中**）。
            //    框左 201.6、宽 50 ⇒ 内接后左 206.6、宽 40（中心 226.6 不动）。
            CheckRect("foot_ic", 206.6f, 1025f, 40f, 40f);

            // ---- 通配符计数条（`WIldcard Counter`，注意原版拼写就是 `WIldcard`）----
            // 🔴 **这一段以前一条断言都没有** ⇒ 「断言全绿、画面全错」（图标抄了**外层容器顶边 71**、
            //    数字被摆在图标**正下方**、字号只有原版的 1/3）。下面所有期望值来自
            //    正本 `资料/卡组编辑界面_查证_0920.md` §③ 的原版绝对 px。
            // ⚠️ 数字比的是**渲染矩形**（`Label.WorldW/H` = TMP 真测量），不是几何中心 ——
            //    原版那条 TMP 是 `VerticalAlignment = Capline`，数字相对几何中心略偏上。
            for (int i = 0; i < 4; i++)
            {
                float cx, cy, w, h;
                CheckTrue(_rt.UiQuadRect("hdr_wc" + i, out cx, out cy, out w, out h),
                          $"通配符第 {i + 1} 个稀有度图标量得到矩形");
                if (_rt.UiQuadRect("hdr_wc" + i, out cx, out cy, out w, out h))
                {
                    Check(Mathf.Abs(cx - (1580f + 75f * i)) < 0.6f, true,
                          $"第 {i + 1} 个图标中心 x = **{1580 + 75 * i}**（槽 x = 1565+75i、图标 30 宽在左）");
                    Check(Mathf.Abs(cy - 113.5f) < 0.6f, true,
                          $"第 {i + 1} 个图标中心 y = **113.5**（行带 91.5..135.5 —— 原来抄了 71 ⇒ 高 20.5px）");
                    // 🔴 **2026-09-27 更正：原来断言的是 `30×44`（拉伸），那是错值。**
                    //    PA 普查实读：原版 `…/WIldcard Counter/Counters/*/Icon` 是 **PA=1 + Simple**，
                    //    4 张图分别是 42×51（Common）与 41×51（Rare/Epic/Legendary）塞进 30×44 的框
                    //    ⇒ 原版实绘 **30×36.4**（第 1 个）/ **30×37.3**（后三个）（**按框居中**）。
                    float wantH = (i == 0) ? 30f * 51f / 42f : 30f * 51f / 41f;
                    Check(Mathf.Abs(w - 30f) < 0.6f && Mathf.Abs(h - wantH) < 0.6f, true,
                          $"第 {i + 1} 个图标 = **30×{wantH:F0}**（PA=1 内接；实测 {w:F0}×{h:F0}）");
                }
                float tx, ty, tw, th;
                CheckTrue(_rt.UiWcCounterRect(i, out tx, out ty, out tw, out th),
                          $"通配符第 {i + 1} 个**数字**量得到渲染矩形");
                if (_rt.UiWcCounterRect(i, out tx, out ty, out tw, out th))
                {
                    // 数字盒 = 41 宽、左边缘贴图标右边缘（图标右 = 1580+75i+15）⇒ 盒中心 = 1615.5+75i
                    Check(Mathf.Abs(tx - (1615.5f + 75f * i)) < 1.0f, true,
                          $"第 {i + 1} 个数字盒中心 x = **{1615.5f + 75f * i}**（在图标**右侧** —— "
                          + $"原来在图标正下方、还左移 45.5px；实测 {tx:F1}）");
                    Check(ty > 91.5f && ty < 135.5f, true,
                          $"第 {i + 1} 个数字落在**行带 91.5..135.5** 里（实测中心 y = {ty:F1}）");
                    Check(th >= 18f, true,
                          $"第 {i + 1} 个数字的**渲染高 ≥ 18px**（字号 32.6 ⇒ cap 约 23px；"
                          + $"原来只有 1/3、cap ≈7.56。实测 {th:F1}）");
                }
                Check(_rt.UiWcText(i), "99",
                      $"第 {i + 1} 个数字 = **恒定 `99`**（用户 2026-09-28 拍板；原版那 4 个数是"
                      + " `WildcardDisplay` 库存直出、跟着指针悬停那张卡的阵营走，我们没有 hover）");
            }

            // ---- 卡组行的四层（正本 `卡组编辑界面_查证_0920.md` §① · `项目任务.md` §三 第 12 条 **第 5 项**）----
            // 🔴 这一段以前也**一条断言都没有** ⇒ 「断言全绿、画面全错」：行底用了一张**原版全透明**的图、
            //    描边 11×11 被**拉满**整行、稀有度色条**铺满整行**、层序还反了（行底盖住色条）。
            Check(_rt.UiTextureName("row_0"), "40k_deck_cardlist_bg",
                  "行底用的是原版那张 `40k_deck_cardlist_bg`"
                  + "（**不是** `UI_Card_name_background_normal_BW` —— 那张原版只当两处 `alpha=0` 的按钮根图）");
            // ⚠️ 行底是 **3 块**不是 9 块 —— 它的九宫格 border 只有**左右** `(150,0,150,0)`
            //    ⇒ 中间那一行三段（左端 150 原尺寸 + 中段拉伸 + 右端 150）就是全部；上下无边。
            CheckTrue(_rt.UiQuadCount("row_0") >= 3,
                      $"行底是**九宫格**（实测 {_rt.UiQuadCount("row_0")} 块 = 左右各 150 的端块 + 中段，"
                      + "border 只有左右 ⇒ 3 块**是对的**）");
            CheckTrue(_rt.UiQuadCount("row_b0") >= 9,
                      $"行描边是**九宫格**（实测 {_rt.UiQuadCount("row_b0")} 块；原来 11×11 的图被拉满整行）");
            CheckTrue(_rt.UiQueueOf("row_0") < _rt.UiQueueOf("row_g0")
                      && _rt.UiQueueOf("row_g0") < _rt.UiQueueOf("row_b0"),
                      "行内层序 = **行底 → 稀有度色条 → 描边**（原版兄弟序 Background→Rarity Gradient→Border；"
                      + $"实测 {_rt.UiQueueOf("row_0")}/{_rt.UiQueueOf("row_g0")}/{_rt.UiQueueOf("row_b0")}）");
            {
                float gx, gy, gw, gh;
                CheckTrue(_rt.UiQuadRect("row_g0", out gx, out gy, out gw, out gh), "稀有度色条量得到矩形");
                if (_rt.UiQuadRect("row_g0", out gx, out gy, out gw, out gh))
                {
                    Check(Mathf.Abs(gw - 128.05f) < 0.6f, true,
                          $"色条宽 = **128.05**（原版只占右侧 0.606→1.0 —— 原来铺满整行 325）");
                    Check(Mathf.Abs(gx - 261.38f) < 0.6f, true,
                          $"色条中心 x = **261.38**（右对齐：197.35 + 128.05/2）");
                }
            }

            // 卡池那一格：原版**默认那一套** = 卡位 262.5×384 · 6 列 · 间距 0 · 贴左起排
            // （定案与硬证据见 `项目任务.md` §三 第 12 条 第 6 项；另一套「小屏 UI」我们没实现，已出声）
            // 🔴 **2026-09-28 订正**：卡位 384 里**底下 38.272 是那条「张数」**（原版
            //    `Collection Card/Content/Counter` 的锚点算出来的，见 `DeckRuntime.PoolCounterH`）
            //    ⇒ **卡只占上面 345.728**（原来按 384 画 = 卡顶满格子、张数条压住卡底；实拍不是这样）。
            float wantScale = 345.728f / (CardView.Height * 108f);
            CheckTrue(Mathf.Abs(CardViewScaleOf(0) - wantScale) < 1e-4f,
                      $"卡池第一张卡的缩放 = {wantScale:F4}（按**卡位高 384 − 张数条 38.272** 反解）");
            // 🔴 **贴左但整体居中 + 6 列**（原版 `RecyclableScrollRect` 的居中量常量 `0.5`）：
            //    内容宽 6×262.5 = 1575 < 视口 1589.8 ⇒ 两侧各留 **7.4** ⇒
            //    第 0 格中心 x = 330.2 + 7.4 + 131.25 = **468.85**；第 5 格 = **1781.35**（右边界 1912.6 ≤ 1920 ✓）
            //    ⚠️ 这条**纠过一次**：先写成「贴左起排」（461.45/1773.95）—— 差 7.4px，来自没查「内容是否居中」。
            {
                float cx, cy, w, h;
                CheckTrue(_rt.UiPoolCellRect(0, out cx, out cy, out w, out h), "卡池第 0 格在（比版面的前提）");
                if (_rt.UiPoolCellRect(0, out cx, out cy, out w, out h))
                {
                    Check(Mathf.Abs(cx - 468.85f) < 0.6f, true,
                          $"卡池第 0 格中心 x = **468.85**（贴左但整体居中：330.2 + 7.4 + 262.5/2 —— 原来是 37.96 的居中留白 ✗）");
                    Check(Mathf.Abs(cy - 328.86f) < 0.6f, true,
                          $"卡池第 0 格**那张卡**的中心 y = **328.86**（156 + 345.728/2 —— 卡在张数条上方那一段里居中；"
                          + "原来是 348 = 整格中心，那是把卡画满整格的老口径 ✗）");
                }
                CheckTrue(_rt.UiPoolCellRect(5, out cx, out cy, out w, out h), "卡池第 6 格（最后一列）在");
                if (_rt.UiPoolCellRect(5, out cx, out cy, out w, out h))
                    Check(Mathf.Abs(cx - 1781.35f) < 0.6f, true,
                          $"卡池第 6 格中心 x = **1781.35** ⇒ 右边界 1912.6 落在 1920 里（**这是「6 列」的判据**）");
                // 🆕 2026-09-28：每格底下那条「张数」（原版 `Collection Card/Content/Counter`）
                CheckTrue(_rt.UiHasQuad("poolbar_0"), "卡池第 0 格**底下那条张数**的底图建出来了");
                var poolDef = _rt.UiPoolCellDef(0);
                CheckTrue(poolDef != null, "（前提）卡池第 0 格上有卡");
                if (poolDef != null)
                {
                    int cap = DeckRules.CopyLimit(poolDef.Rarity);
                    string wl = _rt.State.Deck.WarlordId;
                    // ① **有督军** ⇒ `{已在卡组}/{min(拥有, 上限)}` —— 原版 `DeckEditorCollectionDisplay__DrawCell`
                    //    的格式串**实测就是 `"{0}/{1}"`**（`stringliteral.json@0x426DE28`）。
                    if (!string.IsNullOrEmpty(wl))
                        Check(_rt.UiLabelText("poolcnt_0"), _rt.State.Deck.CountOf(poolDef.Id) + "/" + cap,
                              "★ 有督军时那条写 **`{已在卡组}/{能放的张数}`**（原版 `\"{0}/{1}\"`）");
                    // ② **还没有督军**（新建卡组 / 正在挑督军）⇒ `x{能放进卡组的张数}` ——
                    //    原版 `CardCollectionDisplay__SetCell.c:72` 的格式串是 **`x{0}`**（⚠️ 它喂的是**拥有数**）；
                    //    我们这版资源全解锁 ⇒ 照原式会显示 `x11` 那种没意义的数，按**用户 2026-09-28 的口径**
                    //    喂「能放进卡组的张数」。两个分支都要验 —— 就地把督军摘掉再放回去。
                    if (!string.IsNullOrEmpty(wl))
                    {
                        _rt.State.ClearWarlord();
                        _rt.RefreshAll();
                        Check(_rt.UiLabelText("poolcnt_0"), "x" + cap,
                              "★ 没督军时那条写 **`x{能放进卡组的张数}`**（原版 `x{0}`；见 `PoolCounterText` 的注释）");
                        _rt.State.SetWarlord(wl);
                        _rt.RefreshAll();
                        Check(_rt.State.Deck.WarlordId, wl, "（把督军放回去 —— 后面的断言仍按原状态跑）");
                    }
                }
            }

            // 筛选栏：默认关着；打开后盖住侧栏（队列更大 = 更后画）
            Check(_rt.FiltersOpen, false, "刚建好时筛选栏是关着的");
            // 🔴 这条是踩出来的：底板建了却**没跟着开关隐藏** ⇒ 它（队列 3020，比侧栏大）
            //    会一直盖住整个侧栏 —— 画面上只剩一块底板色，卡组行/页签/Done 全看不见。
            CheckTrue(!_rt.UiQuadActive("flt_bg"), "关着的时候筛选栏底板**不显示**（不然会盖住整个侧栏）");
            // 🔴 **2026-10-05（A57 ①）：上面那条 `flt_bg` 与下面 `side_bg` 都是 `Img()` 建的【单块】**
            //    —— `UiQuadActive` 以前**只查 `_named`**，对**九宫格 / 子树**件**恒答 false**（静默）。
            //    这两条拿抽屉里的**搜索框底 `flt_input`** 做**两态对照**：它是
            //    `ImageQuad.CreateNineSlice` 建的九宫格、**不进 `_named`**，而且挂在容器 `flt_drawer` 底下。
            //    · 关着 ⇒ **不露** —— 这一条同时钉住「这个读数**不是恒 true**」；
            //    · 打开 ⇒ **露** —— 这一条**去掉兜底必红**（旧写法两种状态都答 false）。
            //    ⚠️ 「打开」那条**不能**拿常显件（如 `hdr_sep`）顶替两态：常显件在两种状态下同值，
            //    它只能验「不是恒 false」、验不出「不是恒 true」（同段 `hdr_sep` 那条就是只验前一半）。
            CheckTrue(!_rt.UiQuadActive("flt_input"),
                      "九宫格件（搜索框底 `flt_input`）**关着时不露** —— 同时钉「这条读数不是恒 true」");
            _rt.UiToggleFilters();
            Check(_rt.FiltersOpen, true, "点一下 Filters 键 → 筛选栏打开");
            CheckTrue(_rt.UiQuadActive("flt_bg"), "打开后筛选栏底板显示出来");
            CheckTrue(_rt.UiQuadActive("flt_input"),
                      "……同一时刻**九宫格件**（搜索框底 `flt_input`，不在 `_named` 里、又挂在容器 "
                      + "`flt_drawer` 底下）也读得出「露着」（A57 ①：少了兜底这里会**谎报 false**）");
            CheckTrue(_rt.UiQuadActive("side_bg"), "筛选栏开着时侧栏底板还在（它被盖住，不是被删掉）");

            // ============================================================ 🆕 2026-10-07（A77 ⑭）
            // **同族另外四条读数**：`UiHasQuad` / `UiTextureName` / `UiQuadCount` / `UiQueueOf`
            // —— 它们以前**只有两步**（`Lookup` → `Root.Find`），容器下的件（`flt_input` 这类
            // **不在 `_named` 里**、又挂在 `flt_drawer` 底下的九宫格）会被**静默**答成 false / null / 0 / −1。
            // 🔴 判据 = `资料/待办判据_审查发现_1005.md` §⑭（裁定：四条**统一到三步找法**、
            //   与 `UiNodeRect`/`UiQuadActive` 一致；⚠️ 且「**别只改一行**」—— 四条都得改）。
            // 🔴 期望值**不是**从这四条自己读回来的（⛔ 自证）：
            //   · 「它在 `Root` 那棵树里」由**本文件自己的** `FindDeep`（`GetComponentsInChildren<Transform>`）
            //     作证 —— 与 `DeckRuntime.FindDeep` 是**两份**实现；
            //   · 「它**不在** `Root` 的直接子件里」由 `_root.Find` 作证 = 缺第三步时看不见它的**原因**；
            //   · 图名 / 块数 / 队列取自**建法本身**：`FilterPanelModel.InputSprite` 那张图（同上 `name_bg`
            //     那条的 `InputFieldBackground`）· border 10 的九宫格 = **3×3 九块** · 队列 = `QFltRow`（3021）。
            //   ⚠️ 这四条读数用 `true`（含未激活）取件 ⇒ **与抽屉开没开无关**（这里是开着量的，与 A57 那组同时刻）。
            {
                var deepIn = FindDeep(_root, "flt_input");
                CheckTrue(deepIn != null && _root.Find("flt_input") == null,
                          "结构前提：`flt_input`（搜索框底）**在 Root 那棵树里、但不是直接子件**"
                          + "（它挂在容器 `flt_drawer` 底下）—— 这正是四条读数缺 `FindDeep` 时**看不见它**的原因"
                          + "（这条若红：下面四条失去判别力，先修这里）");
                CheckTrue(_rt.UiHasQuad("flt_input"),
                          "`UiHasQuad(flt_input)` = **真**（A77 ⑭：只走两步的旧写法在这里**静默答 false**）");
                Check(_rt.UiTextureName("flt_input"), "InputFieldBackground",
                      "`UiTextureName(flt_input)` = 原版那张 `InputFieldBackground`（同 `name_bg` 那条的判据）"
                      + " —— 九宫格 9 块里取第一块，即「一棵树取第一块」那一半也在");
                CheckTrue(_rt.UiQuadCount("flt_input") >= 9,
                          "`UiQuadCount(flt_input)` = **九宫格那 9 块**（border 10 ⇒ 3×3；旧写法在这里答 **0**；"
                          + $"实测 {_rt.UiQuadCount("flt_input")} 块）");
                Check(_rt.UiQueueOf("flt_input"), 3021,
                      "`UiQueueOf(flt_input)` = `QFltRow` **3021**（旧写法在这里答 **−1**；"
                      + "−1 会被「层序比大小」那种断言当成「队列最小」）");
                // 负例：**哪儿都没有**的名字 —— 四条必须照旧答「没有」（钉住兜底**不是恒真**）
                CheckTrue(!_rt.UiHasQuad("flt_input_zzz") && _rt.UiQuadCount("flt_input_zzz") == 0
                          && _rt.UiTextureName("flt_input_zzz") == null && _rt.UiQueueOf("flt_input_zzz") == -1,
                          "负例：查一个**哪儿都没有**的名字 ⇒ 四条一律 `false` / `0` / `null` / `−1`（实得 "
                          + $"{_rt.UiHasQuad("flt_input_zzz")} / {_rt.UiQuadCount("flt_input_zzz")} / "
                          + $"{_rt.UiTextureName("flt_input_zzz") ?? "<null>"} / {_rt.UiQueueOf("flt_input_zzz")}）");
                // 回归钉：`_named` 里那件（同一容器下的**单块**）加了深查找之后**仍是 1 块**。
                // ⚠️ 它钉的是**答案**（`_named` 那一支没被深查找挤掉），⛔ 钉不了「优先序」——
                //    今天没有任何 `_named` 的 key 包着 **>1** 块（`Img()` 只建单块）⇒
                //    「把 `Lookup` 提到最前」这种改法**本工程今天没有断言能分辨**（如实记在报告里）。
                Check(_rt.UiQuadCount("flt_bg"), 1,
                      "`UiQuadCount(flt_bg)` 仍是 **1**（`_named` 单块；深查找插在 `Root.Find` 之后 ⇒ 树那一支数的还是同一个节点）");
            }

            if (_rt.UiQuadRect("flt_bg", out float fx, out float fy, out float fw, out float fh))
            {
                // 权威表是「左上 + 宽高」= [2.2,156] 331.7×924.1 ⇒ 中心 (168.05, 618.05)
                CheckRectPx("flt_bg", fx, fy, fw, fh, 2.2f + 331.7f * 0.5f, 156f + 924.1f * 0.5f, 331.7f, 924.1f);
                CheckTrue(fx < 336f, $"筛选栏在**左**（中心 x = {fx:F1}，原版权威坐标说左、与侧栏同格）");
            }

            // 🆕 2026-09-28：**照原版补的三件**（搜索框 + Owned + Upgradable）+ 七行的行高 / 格尺寸。
            //   判据 = `资料/普查产出_0923/A3_Cards页.md` §5·1 实读（模型 = `Core/FilterPanelModel.cs`，与收藏窗同一份）；
            //   出厂态 = `资料/卡组编辑界面_查证_0920.md` 末节（2026-09-28 三路实读）。
            Check(_rt.UiFilterCellCount, 31, "抽屉里 31 个格子（2 + Army 13 + Rarity 5 + Cost 8 + Type 3）");
            if (_rt.UiFilterCell("$owned", out float ownCx, out float ownCy, out float ownW, out float ownH, out bool ownOn))
            {
                // 面板内 79.02 起、高 50（**整行可点**，原版就是那一行的 `EverguildToggle`）
                CheckRectPx("flt_owned", ownCx, ownCy, ownW, ownH, 2.2f + 331.7f * 0.5f, 156f + 79.02f + 25f, 331.7f, 50f);
                CheckTrue(ownOn, "`Owned` **出厂就是【开】**（prefab `m_IsOn=1` + `showOnlyOwnedCards` 初值 true）");
            }
            else Check(true, false, "找不到 `$owned` 那一格");
            if (_rt.UiFilterCell("$upgradable", out float upgCx, out float upgCy, out float upgW, out float upgH, out bool upgOn))
            {
                CheckRectPx("flt_upgradable", upgCx, upgCy, upgW, upgH, 2.2f + 331.7f * 0.5f, 156f + 129.02f + 25f, 331.7f, 50f);
                CheckTrue(!upgOn, "`Upgradable` **运行期是【关】**（`UpgradableCardFilter__SetupFilter.c:16` 强制置 false）");
            }
            else Check(true, false, "找不到 `$upgradable` 那一格");

            // ============================================================ 🆕 2026-10-09（ArmyRowH）
            // **Army 那一行的高度 = 它的【内容高】550，不是 150。** 真身是 2026-09-28 修掉的一处真缺陷：
            //   原来把 Army 行写死 150（A3 §5·1 那两个数），而这一行的网格是 **13 格 · 3 格/行 = 5 行 · 格 100**
            //   ⇒ 内容高 = `Content` 顶内缩 50 + 5×100 = **550** ⇒ 溢出 350px，**压在 Rarity / Cost 两行上**
            //   （截图里阵营徽记与稀有度框叠在一起；更坏的是**点 Rarity 实际改的是阵营** —— 命中按登记顺序先到先得）。
            //   判据 → `资料/卡组编辑界面_查证_0920.md` §四（含那条裁定与两处证据）；
            //   实现（两处证据写在它的注释里）→ `FilterPanelModel.ArmyRowH`。
            // ⚠️ 量的是**建出来那两行格子的渲染矩形**，⛔ **不是** `FilterPanelModel.ArmyRowH(13)` 的返回值
            //   —— 读被测函数自己 = 自证（那个函数被改回 150 时返回值跟着变小、断言照样绿）。
            //   期望值由「面板原点 `FltY` 156 + 面板内 179.02 + **550** + Rarity 行 `Content` 顶内缩 65 + 半格 50」
            //   算出来 ⇒ `$rar:common` 中心 y = **1000.02**（面板内 844.02）。
            // 🔴 改坏法：把 `FilterPanelModel.ArmyRowH` 改成写死 `150f` ⇒ `$rar:common` 中心读成 **600.02**
            //   ⇒ 下面第一条红；同时最后一行 Army 格与 `$rar:common` 在 y 上**压在一起**（间隙 −335，
            //   而不是 +65）⇒ 下面那条几何断言也红。**两条一起红**，不会出现「改坏了还绿」。
            {
                int facN = state.Factions().Count;
                Check(facN, 13, "（前提）本窗阵营表 **13** 档 —— Army 行 = 5 行（13 ÷ 3 向上取整）才谈得上 550");
                if (facN > 0
                    && _rt.UiFilterCell("$fac:" + state.Factions()[facN - 1],
                                        out float arX, out float arY, out float arW, out float arH, out bool arOn))
                {
                    int lastRow = (facN - 1) / 3, lastCol = (facN - 1) % 3;
                    // 最后一格：面板内 x = 14 + 列×107、y = 179.02 + 50 + 行×100；格 100×100
                    CheckRectPx("$fac:" + state.Factions()[facN - 1], arX, arY, arW, arH,
                                2.2f + 14f + lastCol * 107f + 50f,
                                156f + 179.02f + 50f + lastRow * 100f + 50f, 100f, 100f);
                    if (_rt.UiFilterCell("$rar:common", out float rcX, out float rcY, out float rcW, out float rcH, out bool rcOn))
                    {
                        // 🔴 **这一条就是「Army 行 = 550（不是 150）」的判别点**（1 行 = 5 行网格撑出来的内容高）
                        CheckRectPx("$rar:common", rcX, rcY, rcW, rcH,
                                    2.2f + 14f + 50f, 156f + 179.02f + 550f + 65f + 50f, 100f, 100f);
                        // 几何判别（不靠「550」这个数本身）：最后一行 Army 格的**底**到 Rarity 格的**顶**，
                        // 期望间隙 = Rarity 行 `Content` 的**顶内缩 65**；写死 150 时是 **−335**（压在一起）。
                        float armyBottom = arY + arH * 0.5f, rarTop = rcY - rcH * 0.5f;
                        CheckNear(rarTop - armyBottom, 65f, 0.6f,
                                  "★ Army 行跟着**内容高**走（13 格 5 行 ⇒ 550）：最后一行 Army 格与 Rarity 格"
                                  + "**不相斥**，y 向间隙 = **65**（= Rarity 行 `Content` 顶内缩；写死 150 时是 −335）");
                    }
                    else Check(true, false, "量不到 `$rar:common`（Rarity 首格）");
                }
                else Check(true, false, "量不到最后一行那格 Army（`$fac:…`）");
            }

            // ============================================================ 🆕 2026-10-10（A224②）
            // **抽屉里的格子：① 全量建（数量不随滚动变）· ② 压在带口的被裁住**
            //
            // 判据（**原版第一权威** = `d:/2/tools/decomp_full/`）：
            //   · `CardRarityFilter__FillToggleList.c` / `CardTypeFilter__FillToggleList.c`（同一个泛型的
            //     两次实例化、逐句同形）的循环体里**没有任何视口 / 滚动位置 / 可见性判据**
            //     （唯一那个 bool 是 option 自己的标志位）；`CollectionFilterToggle__Initialize.c` 只
            //     `set_sprite` + 赋值 ⇒ **每个 option 都 `Instantiate`** —— 裁切只发生在**渲染层**。
            //   · 原版那层裁切 = `Card Filters > Scroll View > Viewport` 上的 **`Image,Mask`**
            //     （`showGraphic = 0` ⇒ 那层 mask 底图不画）。证据链全文 →
            //     `资料/普查产出_1009/查证V1_原版prefab四件.md` §一。
            //
            // ⛔ 期望值**不从被测实现里读**：
            //   · 条数 = **原版算式** 2（`Owned` / `Upgradable` 两个开关）+ Army 阵营数 + 5（Rarity）
            //     + 8（Cost）+ 3（Type）—— 后三个是原版那三个 filter 的 `get_options()` 全表长度；
            //   · 带口 = **原版字面量** `Card Filters :: pos(2.2,156.0)` **331.7×924.1** ⇒
            //     上沿 **156** · 下沿 **156 + 924.1 = 1080.1**。
            // 🔴 改坏法（两条各盯一件事，不重叠）：
            //   · 把 `RefreshFilterCells` 里那句「滚出面板的不建」加回去 ⇒ ① 的**两个位置数量不同**（20 → 23）⇒ 红；
            //   · 只把裁切那一刀去掉（`ClipCellToBand` 不调）⇒ ② 的上沿读成 120.1 ⇒ 红，②′ 报出越界格数。
            {
                CheckTrue(_rt.FiltersOpen, "（前提）抽屉开着（下面两条量的都是它里面的格子）");
                int wantCells = 2 + state.Factions().Count + 5 + 8 + 3;
                int nTop = CountDrawerCellQuads();
                Check(nTop, wantCells,
                      "★ ① **滚到顶**时抽屉里的格子节点数 = 应有条数（原版是**全量 `Instantiate`**；"
                      + "那句早退还在的话这里只有 2 + 13 + 5 = **20** 个 —— Cost/Type 两族全在带口外）");
                float fltBot = _rt.UiScrollFilters(1e6f);
                CheckTrue(fltBot > 0f, $"（前提）抽屉滚得动（滚到底 = {fltBot:F2}px > 0）");
                int nBot = CountDrawerCellQuads();
                CheckTrue(nBot == nTop,
                          $"★ ① **滚到底**时格子数**一个不差**（{nTop} → {nBot}）—— 数量不随滚动位置变"
                          + "（早退还在的话这里是 **20 → 23**：滚到底时 Cost/Type 两族进了带，"
                          + "而 Owned/Upgradable + Army 前两排滚了出去 —— 一进一出、数量变了）");

                // ② ★ 压在带口的格子被裁住了 —— 量**渲染并集**（几何真值），⛔ 不是模型矩形
                //   滚到底（`464.92`）时 Army 行第 3 排（0 起）的绝对 y = `156 + 179.02 + 50 + 200 − 464.92`
                //   = **[120.1, 220.1]** ⇒ **上沿压在带口 156 上** —— 未裁时它会画到 120.1，
                //   高出的 **35.9px 全压在 `Header` 上**（V1 §一 说的那处越界）。
                //   ⚠️ 挑哪一格**不写死下标**（阵营数变了照样挑得中）：扫一遍 Army 各族，取**上沿压在带口上**
                //     的那一格；而**期望值全是原版字面量**（`156` / `1080.1`）。
                const float BandX1 = 2.2f, BandTop = 156f, BandW = 331.7f, BandBottom = 156f + 924.1f;
                string crossKey = null; float modelTop = 0f;
                foreach (var fk in state.Factions())
                {
                    var key = "$fac:" + fk;
                    if (!_rt.UiFilterCell(key, out _, out float mcy, out _, out float mch, out _)) continue;
                    if (mcy - mch * 0.5f < BandTop && mcy + mch * 0.5f > BandTop) { crossKey = key; modelTop = mcy - mch * 0.5f; break; }
                }
                CheckTrue(crossKey != null,
                          "（前提）滚到底时**有**一格的上沿压在带口上（没有 ⇒ 下面那条等于没查）");
                if (crossKey != null)
                {
                    CheckTrue(modelTop < BandTop,
                              $"（前提）`{crossKey}` 的**未裁**上沿 {modelTop:F1} 确实在带口 {BandTop:F1} 之上"
                              + "（= 这一格真的越界了；不越界的话「裁住了」是白说）");
                    var cq = _rt.UiFilterCellQuad(crossKey);
                    CheckTrue(cq != null && cq.gameObject.activeSelf,
                              $"（前提）`{crossKey}` 那一格的图示在、而且**活着**（只有整块在带外的才会被关掉）");
                    if (cq != null && QuadRectPx(cq, out float qx1, out float qy1, out float qx2, out float qy2))
                    {
                        CheckNear(qy1, BandTop, 0.6f,
                                  $"★ ② `{crossKey}` 的**渲染上沿 = 带口 {BandTop:F1}**（被裁到边上了；"
                                  + $"不裁时它是 {modelTop:F1} ⇒ 那 {BandTop - modelTop:F1}px 全压在 `Header` 上）");
                        CheckTrue(qy2 <= BandBottom + 0.6f && qx1 >= BandX1 - 0.6f && qx2 <= BandX1 + BandW + 0.6f,
                                  $"★ ② ……另外三边也都在带口内（渲染矩形 = ({qx1:F1},{qy1:F1})-({qx2:F1},{qy2:F1})；"
                                  + $"带口 x [{BandX1:F1}, {BandX1 + BandW:F1}] · y [{BandTop:F1}, {BandBottom:F1}]）");
                    }
                    else Check(true, false, $"`{crossKey}` 那一格的渲染矩形量不出来");
                }

                // ②′ 逐格扫一遍：**所有活着的格子**都不许画出带口 ——
                //    单挑一格容易被「只裁了那一格」蒙过去（改坏法：只裁 Army 那一排 ⇒ 这条会报出别的格子越界）。
                int outN = 0; string outWho = "";
                foreach (var q in _root.GetComponentsInChildren<ImageQuad>(true))
                {
                    if (q == null || q.name != "flt_cell" || !q.gameObject.activeSelf) continue;
                    if (!QuadRectPx(q, out float sx1, out float sy1, out float sx2, out float sy2)) continue;
                    if (sy1 < BandTop - 0.6f || sy2 > BandBottom + 0.6f
                        || sx1 < BandX1 - 0.6f || sx2 > BandX1 + BandW + 0.6f)
                    { outN++; if (outWho.Length == 0) outWho = $"({sx1:F1},{sy1:F1})-({sx2:F1},{sy2:F1})"; }
                }
                Check(outN, 0,
                      "★ ②′ 滚到底时**一格都没画出带口**（实测 " + outN + " 格越界"
                      + (outWho.Length == 0 ? "）" : $"；例：{outWho}）"));
                // 收尾：滚回顶 —— **下面的断言都在滚动位 0 上读抽屉**（同 A62 那一段的收尾写法）
                Check(_rt.UiScrollFilters(-1e6f), 0f, "（收尾）滚回 0（不把滚动位留给后面的断言）");
            }

            // ============================================================ 🆕 2026-10-12（A328①）
            // **`QuadRectPx` 的量纲：树上被乘了缩放之后，它量出来的必须还是【设计 px】**
            //
            // 判据（A298 口径，逐条）：① **期望值全是原版设计 px 字面量**（带口 = 原版 `Card Filters` 的
            //   `pos(2.2,156.0)` 331.7×924.1 —— 就在上面那一节里）⇒ 量出来的必须是**同一个量纲**；
            //   ② 「已缩放的世界坐标 → 设计空间」在全工程**只有一份**换算 = `MenuDraw.PosInDesignSpace`
            //   （`Shell/MenuDraw.cs:71`；A294 / A297 / A298 三轮把它收口到这一处）；
            //   ③ 尺寸项**不除**（`q.WorldW/H` 收进去的就是**设计长度** —— 同 A298 那句
            //   「尺寸项本来就在设计量纲上」）。
            //
            // 🔴 **两态**：态一 = 出厂（`k == 1`，本窗**今天唯一**的真实态）· 态二 = 给 `_root` 挂一颗
            //   `TransformScalerBySmallScreenUI(menuScale = 1.2)` 再 `Tick()`（= 小屏开关打开时**窗根**被乘
            //   的那种情形）。两态量**同一格**，断「设计 px 矩形**相同**」。
            // ⚠️ **为什么必须两态**（不是啰嗦）：`k == 1` 时新旧两式**逐位相同** ⇒ 只跑态一等于没查
            //   （A298 那份报告的原话：「今天【一条现有断言都不会红】」）。
            {
                Section("A328①：`QuadRectPx` 的量纲（世界 → 设计 px）—— 两态");
                const float BandTop2 = 156f, BandX12 = 2.2f, BandW2 = 331.7f, BandB2 = 156f + 924.1f;
                _rt.UiScrollFilters(1e6f);                       // 滚到底：下面要的那一格只有在这里才压在带口上
                ImageQuad cell = null;
                foreach (var q in _root.GetComponentsInChildren<ImageQuad>(true))
                {
                    if (q == null || q.name != "flt_cell" || !q.gameObject.activeSelf) continue;
                    if (!QuadRectPx(q, out float a1, out float b1, out float a2, out float b2)) continue;
                    // 挑「上沿压在带口上」的那一格（与上一节同一个挑法）——**别写死下标**（阵营数会变）
                    if (b1 < BandTop2 - 0.6f || b2 <= BandTop2) continue;
                    if (a1 < BandX12 - 0.6f || a2 > BandX12 + BandW2 + 0.6f) continue;
                    cell = q; break;
                }
                CheckTrue(cell != null, "（前提）滚到底时抓得到一格「上沿压在带口上」的 `flt_cell`"
                                      + "（抓不到 ⇒ 下面两态量的不是同一件东西）");
                if (cell != null)
                {
                    CheckTrue(QuadRectPx(cell, out float p1x1, out float p1y1, out float p1x2, out float p1y2),
                              "（前提）态一量得出渲染矩形");
                    // 态一：**逐位等于**那两处老读数（`DeckRuntime.PxOfWorld(世界)` —— 树不带缩放时两者必同）
                    var raw1 = DeckRuntime.PxOfWorld(cell.transform.position);
                    CheckNear(p1x1 + (p1x2 - p1x1) * 0.5f, raw1.x, 0.01f,
                              "态一（`k == 1`）：中心 x 与 `PxOfWorld(世界)` **逐位同值**"
                            + "（= 这次换口径在出厂态下零可观测差异）");

                    // ---- 态二：给**本场景的根**挂一颗缩放器（`menuScale = 1.2`）再 `Tick()` ----
                    SmallScreenUI.PersistOverride = true;         // 自检不许动玩家的真设置（同 CollectionScene 的写法）
                    SmallScreenUI.Set(true);
                    var sc = _root.gameObject.AddComponent<TransformScalerBySmallScreenUI>();
                    sc.SetScale(1.2f);
                    sc.Tick();                                    // 批处理没有帧循环 ⇒ 手动推一次（同族先例满地都是）
                    CheckNear(_root.localScale.x, 1.2f, 0.001f, "（夹具）态二：根真的被乘到 1.2（不然下面那条是空转）");

                    CheckTrue(QuadRectPx(cell, out float p2x1, out float p2y1, out float p2x2, out float p2y2),
                              "（前提）态二量得出渲染矩形");
                    var raw2 = DeckRuntime.PxOfWorld(cell.transform.position);
                    // 🔴 **夹具非退化**：态二下「旧式（裸世界坐标）算出来的中心」与「设计 px 中心」**必须差得出来**
                    //    —— 否则两态相同是因为两式本来同值，而不是因为换算对。
                    float dcx = (p2x1 + p2x2) * 0.5f, dcy = (p2y1 + p2y2) * 0.5f;
                    float gap = Mathf.Max(Mathf.Abs(raw2.x - dcx), Mathf.Abs(raw2.y - dcy));
                    CheckTrue(gap > 10f,
                              $"（前提）态二下**旧式读数与设计 px 已经分家**（中心差 {gap:F1}px > 10）—— 这一格离画布中心够远，"
                            + "量纲病照得出来（夹具若摆在画布中心附近，两式恒等 ⇒ 这条会红）；实得设计中心 ({dcx:F1},{dcy:F1})");
                    CheckNear(p2x1, p1x1, 0.05f, "★ A328①：态二的**左沿**（设计 px）= 态一的左沿");
                    CheckNear(p2y1, p1y1, 0.05f, "★ A328①：态二的**上沿**= 态一的上沿"
                                               + "（改回裸 `PxOfWorld(q.transform.position)` ⇒ 这两条按 (1−1/1.2)×离原点距离 偏掉 ⇒ 红）");
                    CheckNear(p2x2, p1x2, 0.05f, "★ A328①：态二的**右沿** = 态一的右沿");
                    CheckNear(p2y2, p1y2, 0.05f, "★ A328①：态二的**下沿** = 态一的下沿"
                                               + "（尺寸项不除缩放 —— 与位置项同一条判据的两半）");

                    // ---- 还原（后面几节接着用同一场景；截图也在后面）----
                    UnityEngine.Object.DestroyImmediate(sc);      // 批处理下 `Destroy` 不生效（本项目铁律）
                    _root.localScale = Vector3.one;
                    SmallScreenUI.ResetForTest();
                    SmallScreenUI.PersistOverride = false;
                    CheckNear(_root.localScale.x, 1f, 1e-6f, "（收尾）根缩放还回 1（不留脏给后面的断言/截图）");
                    CheckTrue(QuadRectPx(cell, out float r1x, out float r1y, out float r2x, out float r2y),
                              "（收尾）还原后还量得出来");
                    CheckNear(r1x, p1x1, 0.05f, "（收尾）还原后与态一逐值一致");
                    CheckTrue(!SmallScreenUI.Enabled, "（收尾）小屏开关还回**出厂关**（没把玩家的设置改掉）");
                }
                _rt.UiScrollFilters(-1e6f);                       // 收尾：滚动位还给 0（下一节读抽屉时按 0 算）
            }

            // ============================================================ 🆕 2026-10-11（A289）
            // **四行小标题（`flt_title_*`）也吃同一道带口** —— 它们与格子住在**同一个滚动内容**里
            //
            // 判据（**原版第一权威**）：
            //   · `Deck Editing Menu > Card Display > Card Filters > Scroll View > Viewport` 上那颗
            //     **`Image,Mask`** 对**文字与图一视同仁**（掩码在 shader 里按像素裁）—— 与上面 A224② 同一处判据；
            //   · 四行小标题也在这个滚动内容里（`RefreshFilterTitles` 用 `FltAbs`：减 `_fltScroll` 摆位）
            //     ⇒ 滚到中间位置时**会压进 `Header`**（算例：`_fltScroll = 200` ⇒ `Army` 那一行绝对 y =
            //     `156 + 179.02 − 200 = 135.02`）。
            //
            // ⛔ **期望值全是原版字面量**：带口 = `Card Filters :: pos(2.2,156.0)` **331.7×924.1**；
            //    小标题那一行的**框** = 行顶 `179.02`（Army 行）+ 行高 `50`（原版 `Title` TMP 实读）。
            //    ⛔ 不从 `DeckRuntime` / `FilterPanelModel` 读常量。
            // 🔴 两条断言的靶子**不同**（不重叠）：
            //   · ① 盯「压在带口上的那一行**真被裁住了**」—— 判据用**相对量**（拿同一屏里**没被裁**的
            //     `Energy Cost` 那一行推出「框顶 → 字形顶」的偏置）⇒ ⛔ 不靠估字体度量、也不抄现成结论；
            //   · ②′ 逐行普查「**没有一行**画出带口」—— 改坏法：只裁某一行 ⇒ 别的行会被报出来。
            //   改坏法（①）：把 `RefreshFilterTitles` 末句 `MenuDraw.ClipTextNow(lb, band, Vector2.zero)` 删掉
            //     ⇒ ① 的字顶读成「未裁的那个值」⇒ 红；把 `ForceRelayout()` 那一句删掉（在**已被夹过的**
            //     网格上再夹一次）⇒ 几何越滚越坏 ⇒ 同一条也会红（这条正是「复用件必须重排」的判别点）。
            {
                CheckTrue(_rt.FiltersOpen, "（前提）抽屉开着（小标题与格子同住那一个滚动内容里）");
                Check(_rt.UiScrollFilters(220f), 220f, "（前提）滚到中间位置 `_fltScroll = 220`");
                const float BandX1 = 2.2f, BandTop = 156f, BandW = 331.7f, BandBottom = 156f + 924.1f;
                var lbArmy = FilterTitleLabel("Army");
                var lbCost = FilterTitleLabel("Energy Cost");
                // ③ 整块在带外的那一行（`Type`）：滚到 220 时框 = `[1180.02, 1230.02]` ⇒ 整块在带口**下沿**外
                var lbType = FilterTitleLabel("Type");
                CheckTrue(lbArmy != null && lbCost != null && lbType != null,
                          "（前提）三行小标题都在树里（`Army` / `Energy Cost` / `Type` —— 按名字找得到）");
                if (lbArmy != null && lbCost != null && lbType != null)
                {
                    // 行的**框**由**节点位置**反推（中心 = 节点世界 y；框 = 中心 ± 25 = `TitleH/2`）
                    float armyCY = DeckRuntime.PxOfWorld(lbArmy.transform.position).y;
                    float costCY = DeckRuntime.PxOfWorld(lbCost.transform.position).y;
                    float armyTop = armyCY - 25f, armyBot = armyCY + 25f;
                    CheckNear(armyCY, 156f + 179.02f + 25f - 220f, 0.6f,
                              "（前提）`Army` 那一行摆在原版算出来的位置上（中心 y = `156 + 179.02 + 25 − 220` = 140.02）");
                    CheckTrue(armyTop < BandTop && armyBot > BandTop,
                              $"（前提）它的**框** y = [{armyTop:F1}, {armyBot:F1}] 真的**压在带口上沿 {BandTop:F1}** 上"
                            + "（不压 ⇒ 下面那条没有鉴别力）");
                    CheckTrue(lbCost.gameObject.activeInHierarchy,
                              "（前提）参考行 `Energy Cost`（框 `[960.02, 1010.02]`）**整块在带口内** ⇒ 它没被裁");
                    if (TextMeshRectPx(lbCost, out _, out float costY1, out _, out _)
                        && TextMeshRectPx(lbArmy, out _, out float armyY1, out _, out _))
                    {
                        // ---- 相对判据：拿**没被裁**的参考行推出「框顶 → 字形顶」的偏置 ----
                        float glyphInset = costY1 - (costCY - 25f);
                        CheckTrue(glyphInset > -5f && glyphInset < 25f,
                                  $"（前提）参考行的字形顶比框顶低 **{glyphInset:F1}px**（这个数由同屏另一行现推，"
                                + "⛔ 不是估的字体度量）");
                        float armyIfUnclipped = armyTop + glyphInset;
                        CheckTrue(armyIfUnclipped < BandTop - 1f,
                                  $"（前提 · **相对判据**）若**不裁**，`Army` 的字形顶会画到 **{armyIfUnclipped:F1}**，"
                                + $"比带口上沿 {BandTop:F1} 还高 {BandTop - armyIfUnclipped:F1}px ⇒ **这一刀必须存在**");
                        CheckNear(armyY1, BandTop, 0.6f,
                                  $"★★ A289 压在带口上的小标题**渲出来的字顶 = 带口 {BandTop:F1}**（被夹在边上）。"
                                + $"改坏法：`RefreshFilterTitles` 末句 `MenuDraw.ClipTextNow(lb, band, Vector2.zero)` 删掉"
                                + $"⇒ 这里读成 {armyIfUnclipped:F1}；删掉那句 `ForceRelayout()`（在已夹过的网格上"
                                + "二次夹）⇒ 几何越滚越坏、同一条也红");
                    }
                    else Check(true, false, "小标题的渲染网格量不出来（`textInfo` 里没有字形 / 对不上）");

                    // ②′ 逐行普查：**所有活着的小标题**都不许画出带口
                    //   （改坏法：只裁 `Army` 那一行、别的行照旧 ⇒ 这条会报出越界的行数与坐标）
                    int outN = 0; string outWho = "";
                    foreach (var ttl in new[] { "Army", "Rarity", "Energy Cost", "Type" })
                    {
                        var lb = FilterTitleLabel(ttl);
                        if (lb == null || !lb.gameObject.activeInHierarchy) continue;
                        if (!TextMeshRectPx(lb, out float tx1, out float ty1, out float tx2, out float ty2)) continue;
                        if (ty1 < BandTop - 0.6f || ty2 > BandBottom + 0.6f
                            || tx1 < BandX1 - 0.6f || tx2 > BandX1 + BandW + 0.6f)
                        { outN++; if (outWho.Length == 0) outWho = $"`{ttl}` ({tx1:F1},{ty1:F1})-({tx2:F1},{ty2:F1})"; }
                    }
                    Check(outN, 0,
                          "★ A289 ②′ 滚到 220 时**没有一行小标题画出带口**（实测 " + outN + " 行越界"
                          + (outWho.Length == 0 ? "）" : $"；例：{outWho}）"));

                    // ③ 整块在带口**下沿外**的那一行（`Type`：框 `[1180.02, 1230.02]` —— 整块在带外）：
                    //    它的字形**全部**落在带外 ⇒ 夹完之后是**零高**（退化 ⇒ 看不见）。
                    //    ⚠️ 这里**不是**「把节点关掉」那种写法 —— 见 `RefreshFilterTitles` 里那段
                    //    「为什么小标题**不**走格子那条 `只关不删`」（复用件 + `SetOn` 会在收起途中
                    //    重新点亮 ⇒ 会带着旧网格重新出现）；改坏法：删掉 `ClipTextNow` 那一句 ⇒ 红。
                    if (TextMeshRectPx(lbType, out _, out float tyY1, out _, out float tyY2))
                    {
                        CheckTrue(tyY2 <= BandBottom + 0.6f,
                                  $"★ A289 ③ 整块在带外的那一行（`Type`）也被夹在带口内（渲出来 y = "
                                + $"[{tyY1:F1}, {tyY2:F1}] ≤ 带口下沿 {BandBottom:F1}）");
                        CheckNear(tyY2 - tyY1, 0f, 0.6f,
                                  "★ …而且是**零高**（四个角全被夹到带口下沿 ⇒ 退化、本来就看不见）");
                    }
                    else Check(true, false, "`Type` 那一行的渲染网格量不出来");
                }
                Check(_rt.UiScrollFilters(-1e6f), 0f, "（收尾）滚回 0 —— 不把滚动位留给后面的断言");
            }
            // 搜索框三件：`Input Field` 281.28×40 **居中** + `Text Area` + 尾图标 `40k_icon_search` 35×30
            _rt.UiFilterNameRects(out float inX, out float inY, out float inW, out float inH,
                                  out float taX, out float taY, out float taW, out float taH,
                                  out float icnX, out float icnY, out float icnW, out float icnH);
            CheckRectPx("flt_input", inX + inW * 0.5f, inY + inH * 0.5f, inW, inH,
                        2.2f + 25.21f + 281.28f * 0.5f, 156f + 19.51f + 20f, 281.28f, 40f);
            CheckRectPx("flt_searchicon", icnX + icnW * 0.5f, icnY + icnH * 0.5f, icnW, icnH,
                        2.2f + 266.55f + 17.5f, 156f + 24.51f + 15f, 35f, 30f);
            Check(_rt.UiFilterInputText, "Search", "空的时候搜索框画的是**占位符**（原版 `Placeholder` 原文）");

            // ============================================================ 🆕 2026-10-09（A190）
            // **同族第五条读数 `UiLabelText`** —— 它是 `DeckRuntime` 里最后一个「按名字找节点」的自检口。
            //   A77 ⑭ 那轮把四条（`UiHasQuad` / `UiTextureName` / `UiQuadCount` / `UiQueueOf`）统一到
            //   `Lookup` → **`FindDeep`** 的找法，**漏了这一条**：它当时**只有一步 `Root.Find`**
            //   （`Transform.Find` **只认直接子件**）⇒ 对挂在**容器**（`flt_drawer` / `cosmoflt_drawer`）
            //   底下的 `Label`（本节这两颗都**不在 `_named` 里**）**静默答 `null`** = 谎报「这颗标签不存在」。
            //   判据 → `资料/待办判据_1007.md` §A190；同族形状 → `DeckRuntime.UiQuadActive` / `UiNodeRect`。
            // 🔴 期望值**不是**从这条读数自己读回来的（⛔ 自证）：
            //   · 「它在 `Root` 那棵树里、但**不是**直接子件」由**本文件自己的** `FindDeep`
            //     （另一份实现：`GetComponentsInChildren<Transform>`）作证 = 缺第二步时看不见它的**原因**；
            //   · `'Army'` 取自**原版行小标题的文案**（`FilterPanelModel.BuildTitles` 的 `Title.Text`）；
            //   · `'Search'` 取自**原版 `Placeholder` 原文**（上一行 `UiFilterInputText` 刚量的是**同一颗**节点）。
            // 🔴 改坏法：把 `DeckRuntime.UiLabelText` 里 `FindDeep` 那一步删掉（退回只走 `Root.Find`）
            //   ⇒ 下面**两条字串断言都读成 `null`** ⇒ 两条红（`poolcnt_0` 那种直接子件不受影响，
            //   见本文件既有那两条 —— 本节顺带就是「直接子件那一路没被挤掉」的对照）。
            {
                var deepTitle = FindDeep(_root, "flt_title_Army");
                CheckTrue(deepTitle != null && _root.Find("flt_title_Army") == null,
                          "结构前提：`flt_title_Army`（`Army` 小标题那颗 `Label`）**在 Root 那棵树里、"
                          + "但不是直接子件**（父级 = 抽屉容器）—— 这正是缺 `FindDeep` 时看不见它的原因"
                          + "（这条若红：下面两条失去判别力，先修这里）");
                Check(_rt.UiLabelText("flt_title_Army"), "Army",
                      "`UiLabelText(flt_title_Army)` 读得到**容器下**的 `Label`"
                      + "（A190：只走 `Root.Find` 的旧写法在这里**静默答 `null`**）");
                Check(_rt.UiLabelText("flt_input_t"), "Search",
                      "……另一族容器下的 `Label`：搜索框那颗（与上一行同**一颗**节点，这条按**名字**走；"
                      + "旧写法同样答 `null`）");
                // 负例：**哪儿都没有**的名字 —— 照旧答 `null`（钉住这一步**不是恒真**）
                CheckTrue(_rt.UiLabelText("flt_title_zzz") == null,
                          "负例：查一个**哪儿都没有**的名字 ⇒ 照旧答 `null`（钉住兜底不是恒真）");
            }
            // 🔴 2026-09-28（审核抓到）：前面那条「同一层不许压住」是在**抽屉还关着**时量的
            //    （`quads` 那会儿还没有这 31 格）⇒ 抽屉里的重叠一条都测不到。**开着再量一次。**
            {
                var draw = new List<ImageQuad>();
                foreach (var q in _root.GetComponentsInChildren<ImageQuad>(false))
                    if (q.name != null && q.name.StartsWith("flt_")) draw.Add(q);
                int bad = 0; var pair = new List<string>();
                for (int i = 0; i < draw.Count; i++)
                    for (int j = i + 1; j < draw.Count; j++)
                    {
                        var A = draw[i]; var B = draw[j];
                        if (A.RenderQueue != B.RenderQueue) continue;
                        var pa = DeckRuntime.PxOfWorld(A.transform.position);
                        var pb = DeckRuntime.PxOfWorld(B.transform.position);
                        // 🔴 2026-10-04（A41 ③）：**右边同样要乘 `PxPerUnit`** —— 这里是上面那条判据的
                        //    第二份拷贝（同一个「单位混了」的毛病，不改的话两条行为不一致）。
                        float bx = Mathf.Abs(pa.x - pb.x) - (A.WorldW + B.WorldW) * 0.5f * PxPerUnit;
                        float by = Mathf.Abs(pa.y - pb.y) - (A.WorldH + B.WorldH) * 0.5f * PxPerUnit;
                        if (bx < -LayTol && by < -LayTol) { bad++; if (pair.Count < 5) pair.Add(A.name + " × " + B.name); }
                    }
                CheckTrue(draw.Count >= 30, $"抽屉里量到 {draw.Count} 张可见图（31 格 + 底 + 输入框那几件）");
                CheckTrue(bad == 0, $"抽屉**开着**时同层的图也没有互相压住（实测 {bad} 处"
                          + (pair.Count == 0 ? "）" : "：" + string.Join(" · ", pair) + "）"));
            }
            Shoot("deck_filters.png");   // 改版面要看截图（断言测不出「字压住了 / 图标没出来」）

            // 两个开关点了真的改状态（`Upgradable` 在单机必然筛成空 —— 界面上会出声说明，不许静默）
            int visBefore = state.VisibleCards().Count;
            _rt.UiFilterRow("$upgradable");
            Check(state.Filter.Upgradable, true, "点 `Upgradable` ⇒ 开关打开");
            Check(state.VisibleCards().Count, 0, "打开后**一张不剩**（单机没有升级系统 ⇒ 恒空是预期的）");
            _rt.UiFilterRow("$upgradable");
            Check(state.VisibleCards().Count, visBefore, "再点一下 ⇒ 回到原来的张数");

            // 筛选动作（走的是和鼠标同一条路）
            _rt.UiFilterRow("$rar:legendary");
            Check(state.Filter.Rarity, "legendary", "点稀有度 → 筛选条件变了");
            CheckTrue(state.VisibleCards().Count < state.PoolCount, "筛出来的确实变少了");
            _rt.UiFilterRow("$rar:legendary");
            Check(state.Filter.Rarity, "", "再点一次 → 取消该筛选");
            _rt.UiFilterRow("$fac:Ultramarines");
            Check(state.Filter.Faction, "Ultramarines", "点阵营 → 筛选条件变了");
            _rt.UiClearFilters();
            CheckTrue(state.Filter.IsEmpty, "Clear filters → 条件清空");
            _rt.UiToggleFilters();
            Check(_rt.FiltersOpen, false, "再点 Filters → 关闭");

            // 🆕 2026-10-04（§三第29条 **A67**）：**左抽屉的滑进/滑出**（原来是整块 `SetOn` 硬切）
            {
                Section("筛选抽屉滑入/滑出：行程 −385px + 0.3 秒 + 位移期间不吃点击（A67）");
                // 判据（唯一一处）= `CollectionFilterController<T>.Toggle(bool, bool)`：
                //   `DOTween.Kill` + **`DOAnchorPosX(rect, x, 0.3)`** + `SetActive`
                //   —— 收起 x = `hiddenPosition.x` = **−550**、展开 x = `originalAnchorPosition.x` = **−165**
                //   ⇒ **行程 = −385px**；`animationTime` = **0.3**。
                //   逐实例实据：menus 包里 6 个带 `hiddenPosition` 的实例**全是 (−550,0) / 0.3**，
                //   其中就有本窗这两棵（`Card Filters [2,156 332x924]` / `Cosmetic FIlter (inactive)`）。
                // ⚠️ **量的是渲染出来的东西**（`UiNodeRect("flt_drawer")` = 整栏那棵子树的包围盒，走世界坐标），
                //   **不是**读我们自己的常量：把 `LayoutSpace.Px(...)` 去掉（px 裸进世界单位）时这里量到 −59400。
                Check(_rt.FiltersOpen, false, "接着上一段：抽屉现在是关着的");
                CheckNear(_rt.FilterDrawerSlide, 0f, 0.001f, "关 ⇒ 进度 **0**（整栏停在 `hiddenPosition` 那一头）");
                CheckTrue(!_rt.FilterDrawerInteractive, "…不参与命中（滑出去那一头同样不吃）");
                CheckTrue(!_rt.UiQuadActive("flt_bg"), "…底板**不露**（关的是整栏那个容器，不是逐件 SetActive）");

                _rt.UiToggleFilters();
                CheckNear(_rt.FilterDrawerSlide, 1f, 0.001f, "点一次 ⇒ 进度 **1**（原位）—— 批处理没有帧循环，直接到位");
                CheckTrue(_rt.FilterDrawerInteractive, "…到位了 ⇒ 命中生效");
                CheckTrue(_rt.UiQuadActive("flt_bg"), "…底板露出来");

                float openCx, openCy, openW, openH;
                CheckTrue(_rt.UiNodeRect("flt_drawer", out openCx, out openCy, out openW, out openH),
                          $"量得到整栏那棵子树（{openW:F1}×{openH:F1}px）—— 它就是这次位移的**唯一对象**");
                CheckNear(openCx, 168.05f, 6f, "原位时整栏中心 x ≈ **168.05**（权威表 [2.2,156] 331.7×924.1 的中心）");

                // ① 时间：`animationTime` = 0.3 ⇒ 推进 0.15 秒正好走半个行程
                _rt.SetDrawerProgressForTest(false, 0f);
                CheckTrue(_rt.UiQuadActive("flt_bg"),
                          "钉在**滑出去的起点**但目标是开 ⇒ 节点**还活着**（原版先 SetActive 再动 tween；"
                          + "不然滑出来那 0.3 秒根本看不见东西）");
                CheckTrue(!_rt.FilterDrawerInteractive, "…起点当然不算到位 ⇒ 命中仍失效");
                _rt.TickDrawersForTest(0.15f);
                CheckNear(_rt.FilterDrawerSlide, 0.5f, 0.01f,
                          "推进 **0.15 秒**（`animationTime` = 0.3）⇒ 进度 **0.5**（**只有真按 0.3 秒走**才成立）");
                _rt.TickDrawersForTest(0.15f);
                CheckNear(_rt.FilterDrawerSlide, 1f, 0.001f, "再推 0.15 秒 ⇒ 到位（一共 0.3 秒）");
                CheckTrue(_rt.FilterDrawerInteractive, "…到位 ⇒ 命中生效");

                // ② 位移量：进度 0 与进度 1 两处量同一棵子树的位置差 = 原版那段行程
                _rt.SetDrawerProgressForTest(false, 0f);
                float shutCx, shutCy, shutW, shutH;
                CheckTrue(_rt.UiNodeRect("flt_drawer", out shutCx, out shutCy, out shutW, out shutH),
                          "滑出去那一头也量得到（容器关掉时 `GetComponentsInChildren(true)` 照样进）");
                CheckNear(shutCx - openCx, -385f, 1.5f,
                          $"行程 = **−385px**（收起 −550 / 展开 −165 之差；实测 {shutCx - openCx:F1}）"
                          + " —— 单位换算写错时这里是 −59,400（收藏窗真踩过那个 bug）");
                CheckTrue(shutCx + shutW * 0.5f < 0f,
                          $"滑出去之后**整栏右缘在屏外**（x = {shutCx + shutW * 0.5f:F1} < 0）");

                // ③ **两态对比**：同一个点，在「位移途中」与「完全到位」两种状态下结果必须不同
                //    （只断一半 = 弱断言：写成恒「不吃」/ 恒「吃」都会有一条绿）
                if (_rt.UiFilterCell("$owned", out float ocx, out float ocy, out float ocw, out float och,
                                     out bool oon))
                {
                    CheckTrue(oon, "（前提：`Owned` 出厂就是开的 —— 与上面那条一致）");
                    // ⚠️ 点在这一格的**右缘内侧**（中心 + 半个宽 − 4）：位移期间这一栏不吃的话，
                    //   点击会**往下落**到它盖住的东西上；本窗那三颗页签的右缘是 327.2 ⇒ 特意挑 ≈329.9，
                    //   免得「开关没被翻」是**点到了别处**、而不是「这一栏不吃」。
                    float clickX = ocx + ocw * 0.5f - 4f;
                    CheckTrue(clickX > 327.2f, $"（这一下落在 x = {clickX:F1} > 页签右缘 327.2 —— 落下去也点不到页签）");
                    _rt.SetDrawerProgressForTest(false, 0.5f);
                    bool own0 = state.Filter.Owned;
                    CheckTrue(!_rt.UiClickPx(clickX, ocy), "位移途中（进度 0.5）点这一格 ⇒ **没人吃这一下**");
                    Check(state.Filter.Owned, own0, "…`Owned` 开关**没被翻**（这一栏此刻不参与命中）");
                    _rt.SetDrawerProgressForTest(false, 1f);
                    CheckTrue(_rt.FilterDrawerInteractive, "推到 1 ⇒ 命中生效");
                    CheckTrue(_rt.UiClickPx(clickX, ocy), "★ **同一点** ⇒ 这次**吃**了（两态对比的另一半）");
                    Check(state.Filter.Owned, !own0, "…`Owned` **真被翻了**（写成恒不吃 / 恒吃，这里必红一条）");
                    _rt.UiClickPx(clickX, ocy);                       // 翻回来，别影响后面几段
                    Check(state.Filter.Owned, own0, "（还原成原来的样子）");
                }
                else Check(true, false, "找不到 `$owned` 那一格");

                // ④ 收起：逻辑态**立刻**翻假，但整栏要滑出去 0.3 秒 —— 那 0.3 秒里格子得跟着一起走
                //    （拆早了画面上只剩一块空底板在滑；原版是整栏连格子一起滑走）
                _rt.UiToggleFilters();
                Check(_rt.FiltersOpen, false, "点 Filters ⇒ 逻辑态**立刻**翻假（UI / 自检读的都是它）");
                CheckTrue(_rt.UiFilterCellObjects > 0,
                          $"…但格子**还留着**（{_rt.UiFilterCellObjects} 件）—— 收起途中要看见它们一起滑走");
                _rt.SetDrawerProgressForTest(false, 0.5f);
                CheckTrue(_rt.UiQuadActive("flt_bg"), "收起途中（进度 0.5）底板**还活着**（要画）");
                CheckTrue(!_rt.FilterDrawerInteractive, "…但**已经不吃**点击/滚轮");
                _rt.SetDrawerProgressForTest(false, 0f);
                CheckTrue(!_rt.UiQuadActive("flt_bg"), "滑到底 ⇒ 整栏关掉（`activeInHierarchy` 假 ⇒ 不再盖住侧栏）");
            }

            // ============================================================ 🆕 2026-10-05（A93②）
            // 筛选栏**四行小标题的左对齐** —— 补断言（以前这四个字**一条断言都没有**：把对齐那半句删掉，
            //   11 条自检**全绿** = 实现改了没人会红）。判据 · 量什么 · 为什么这样量 → 见本文件上面
            //   `TitleLeftPx` 那一段（原版 `Title` 是 `Left/Middle`；面板原点 2.2 ⇒ 画布 **2.2 / 27.2 ×3**）。
            //
            // 🔴 改坏会红在哪（两种改法各给一个读数，都在下面四条上）：
            //   ① 删掉 `DeckRuntime.RefreshFilterTitles` 里那句 `MenuDraw.AlignLeft`（或把
            //      `FilterPanelModel.BuildTitles` 的 `Left` 退回 `false`）⇒ 小标题停在**矩形中心**
            //      （Army **168.05** = 2.2 + 331.7/2 · 另三行 **180.55** = 2.2 + (25+331.7)/2）
            //      ⇒ 量到「中心 − 文字宽/2」≈ Army **126** / 另三行 **135**（离期望 2.2 / 27.2 差 120+px，
            //      远超 ±1 容差）⇒ **四条逐条红**；
            //   ② 只摆中心、不做对齐（= 原来那版）—— 与 ① 是同一个读数，同样四条全红。
            //   ⚠️ 「量不出来」也不放过：`TitleLeftPx` 给 **−9999** ⇒ 照样红（不静默）。
            // ⚠️ 本节**自己开、自己关** —— 量完把抽屉收回 false，不改后续用例的初态。
            Section("筛选栏四行小标题的**渲染左沿**（原版 `Title` = Left/Middle；⛔ 不是 Center）");
            Check(_rt.FiltersOpen, false, "（前提）进本节时抽屉是关着的（上一节收尾如此）");
            _rt.UiToggleFilters();                    // 摆位 + 对齐只在逻辑态开着时做 ⇒ 必须先开
            Check(_rt.FiltersOpen, true, "点 `Filters` ⇒ 抽屉打开（这时才摆小标题）");
            CheckNear(_rt.FilterDrawerSlide, 1f, 0.001f,
                      "…而且**一步到位**（批处理没有帧循环 ⇒ 容器已回原位、位移量 0）");
            CheckNear(TitleLeftPx("Army"), 2.2f, 1f,
                      "小标题 `Army` 的**渲染左沿** = **2.2**（原版面板内 0 + 面板原点 2.2；⛔ 不是 0.25 —— "
                      + "那是收藏窗的原点；居中画的话会落在 ~126）。量不出来时这里给 −9999");
            CheckNear(TitleLeftPx("Rarity"), 27.2f, 1f,
                      "小标题 `Rarity` 的**渲染左沿** = **27.2**（原版那三行都从面板内 x=25 起；居中会落在 ~135）");
            CheckNear(TitleLeftPx("Energy Cost"), 27.2f, 1f,
                      "小标题 `Energy Cost` 的**渲染左沿** = **27.2**（名字带空格 ⇒ 节点是 `flt_title_Energy_Cost`）");
            CheckNear(TitleLeftPx("Type"), 27.2f, 1f,
                      "小标题 `Type` 的**渲染左沿** = **27.2**（四行里最后一行；判据与上面三条同一份）");
            _rt.UiToggleFilters();                    // 收回去：本节开关成对，后续用例的初态不变
            Check(_rt.FiltersOpen, false, "量完收起 ⇒ 回到本节开工时的样子（本节不许影响后面的断言）");

            // 页签：Cards / Deck info / Cosmetics —— 费用曲线只在 info 页显示
            // 页签：Cards / Deck info / Cosmetics —— 三页各有各的东西，**不能是空白页**
            _rt.UiSetTab(1);
            Check(_rt.ActiveTab, 1, "切到 Deck info 页签");
            CheckTrue(_rt.UiCurveVisible, "Deck info 页签里费用曲线可见（Cards 页签下它是关的）");
            CheckTrue(_rt.UiInfoActionsVisible, "Deck info 页签里「分享 / 导入」两颗钮显示出来");
            CheckTrue(!_rt.UiCosmeticsVisible, "Cosmetics 那组东西在别的页签下是关的");
            CheckTrue(_rt.UiDeckRowAt(0) == null, "Deck info 页签下卡组行不显示");
            // 🔴 **2026-10-05（A57 ①）：同一条「行藏没藏」再过一遍【图】那一半** —— `UiDeckRowAt` 读的是
            //    状态（`_tab` + `shown`），答不出「那棵九宫格**树**真的关掉了吗」（`SetOn(_deckRowBg[i], …)`，
            //    `DeckRuntime.cs` 的 `RefreshDeckList`）。`row_0` 正是 A57 ① 点名的受影响件之一
            //    （九宫格、**不进 `_named`**）⇒ 下面这一对同时钉住「关得住」与「读得出关」。
            //    期望值出处：行属 Cards 页（原版 `Content Area/Sidebar` 的卡组列表只在 Cards 页签下），
            //    另由紧邻的 `UiDeckRowAt` 那两条独立佐证 —— ⛔ 不是从 `UiQuadActive` 自己读回来的。
            CheckTrue(!_rt.UiQuadActive("row_0"),
                      "Deck info 页签下**那棵九宫格行树真的关了**（`row_0` 不在 `_named` 里 ⇒ "
                      + "少了兜底这条会**碰巧**答 false，所以必须与下面那条成对看）");
            _rt.UiSetTab(2);
            CheckTrue(_rt.UiCosmeticsVisible, "Cosmetics 页签：**卡背那一页显示出来**（2026-09-24 起是真页面，不再是一句空态）");
            CheckTrue(!_rt.UiCurveVisible, "Cosmetics 页签下费用曲线关掉");
            CheckTrue(!_rt.UiInfoActionsVisible, "Cosmetics 页签下动作钮关掉");
            _rt.UiSetTab(0);
            CheckTrue(!_rt.UiCurveVisible, "切回 Cards → 费用曲线隐藏");
            CheckTrue(!_rt.UiCosmeticsVisible, "切回 Cards → Cosmetics 空态隐藏");
            CheckTrue(_rt.UiDeckRowAt(0) != null, "切回 Cards → 卡组行回来");
            // 🔴 **2026-10-05（A57 ①）**：与上面 `row_0` 那条**成对** —— 同一条九宫格行树，切回 Cards 页
            //    必须**露出来**。前面那条 `UiDeckRowAt(0) != null` 已经钉住「此刻确实有行」（非空卡组 +
            //    `_tab==0`，`DeckRuntime.cs` 的 `RefreshDeckList` 的 `on = cards && firstRow+i < shown.Count`）
            //    ⇒ 这里期望 `true` 有独立出处，**去掉兜底这条必红**（旧写法对九宫格件恒答 false）。
            CheckTrue(_rt.UiQuadActive("row_0"),
                      "切回 Cards → **那棵九宫格行树也真的露出来了**（A57 ① 点名受影响的 `row_*` 那一族）");
            // 🔴 「该藏的藏住了吗」——**图 + 文字一起查**（只查图会漏：`Lookup` 不认 Label）
            Check(_rt.UiInfoOnlyActive, 0, "Cards 页签上**一件 Deck info 的东西都不许露**（图 + 文字都算）");
            Check(_rt.UiCosmOnlyActive, 0, "Cards 页签上不许露 Cosmetics 的东西");

            // 🔴🔴 **2026-10-04（A41 ④）：「藏住了」还不够 —— 藏住的东西【不许吃点击】**。
            //   真缺陷（原来 `_btns` 里的矩形不跟着显隐走）：那两片空白（`info_share` / `info_import`，
            //     · (60..131, 636..707) = `info_share` ⇒ **一次点击静默把卡组串写进剪贴板**；
            //     · (200..271, 636..707) = `info_import` ⇒ **静默打开导入弹窗**）
            //   同一族还有第二处：导入弹窗**关着**时，它的输入框矩形 (610..1310, 370..511)
            //   正好落在**卡池**里 ⇒ 点在没卡的空白上会进入一个**看不见的文本编辑态**（`_editKind=3`）。
            //   判据 = 原版那套「`SetActive(false)` / `m_TargetGraphic.enabled=0` 之后就不再参与射线」
            //   （UGUI `Graphic.Raycast` 只对**激活且在画**的图形生效）⇒ 我们收口成 `DeckRuntime.KeyLive`。
            // 🔴 **2026-10-04 订正（R-W4 F1/F2）** —— 两句措辞原来都太宽：
            //   ① **「必现页」是 `Cosmetics`，不是 `Cards`**：真实指针链是
            //      `HandlePoolClick → HandleDeckRowClick → HandleCosmeticClick → HandleButtons`
            //      （`DeckRuntime.cs:1606-1610`，`HandleButtons` 是**最后一站**），而那两片矩形
            //      **整片落在卡组列表里**（x 0.4..325.4 · y 366..1010.1）⇒
            //        · **Cosmetics 页（必现）**：`_tab==2` ⇒ `HandleDeckRowClick`（`:1634` 要求 `_tab==0`）
            //          早退、`HandleCosmeticClick`（`:1159` 要求 `_tab==2`）只管 x ≥ `CosmoX` 那一片
            //          ⇒ **谁也拦不住**，直接落到 `HandleButtons`；
            //        · **Cards 页**：只要那一格真有行，`HandleDeckRowClick` 就先把它吃掉（开始行拖拽）
            //          ⇒ **点不到**；只有**那一格是空槽**时才漏得过去（`RefreshDeckList` 的
            //          `on = cards && (firstRow + i) < shown.Count`，`:1263`）。
            //   ② **下面这几条走的是 `KeyLive` 那条命中路**（`UiTopKeyAt` → `HitBtn`/`KeyLive`；
            //      `UiClickPx` 直调 `HandleButtons`），**不是**真实鼠标那条完整分派链
            //      ⇒ 只能说「**不被任何按钮**吃到」，不能说「谁都没吃到」（行处理器不在这一层）。
            {
                _rt.UiBtnRect("info_share", out float shX, out float shY, out float shW, out float shH);
                _rt.UiBtnRect("info_import", out float imX, out float imY, out float imW, out float imH);
                _rt.UiBtnRect("imp_input", out float ipX, out float ipY, out float ipW, out float ipH);
                float shCx = shX + shW * 0.5f, shCy = shY + shH * 0.5f;
                float imCx = imX + imW * 0.5f, imCy = imY + imH * 0.5f;
                float ipCx = ipX + ipW * 0.5f, ipCy = ipY + ipH * 0.5f;

                CheckTrue(_rt.ActiveTab == 0 && !_rt.UiInfoActionsVisible,
                          "（前提）现在在 Cards 页签、两颗动作钮是**隐藏**的");
                Check(_rt.UiTopKeyAt(shCx, shCy), null,
                      "分享钮那片空白**攒不到任何按钮 key**（`KeyLive` 那条命中路；隐藏时原来会静默分享到剪贴板）");
                CheckTrue(!_rt.UiClickPx(shCx, shCy), "……点下去也没有**任何按钮**吃到这一下（走 `HandleButtons`）");
                Check(_rt.UiTopKeyAt(imCx, imCy), null, "导入钮那片空白**同样攒不到任何按钮 key**");
                _rt.UiClickPx(imCx, imCy);
                Check(_rt.ImportOpen, false, "……点下去也不会**静默打开导入弹窗**");
                CheckTrue(!_rt.NameEditing, "（前提）现在不在文本编辑态");
                Check(_rt.UiTopKeyAt(ipCx, ipCy), null,
                      "导入弹窗**关着**时，`imp_input` 那片（在卡池里）命中不到任何东西");
                _rt.UiClickPx(ipCx, ipCy);
                CheckTrue(!_rt.NameEditing, "……也不会点进一个**看不见的输入态**（静默失败那一类）");

                // ---- 正向控制：**同一批点位**，在「显示出来」的时候必须照常命中 ----
                // （不然上一条可能是因为「这些 key 压根没注册」，那是另一种绿）
                _rt.UiOpenImport();
                Check(_rt.UiTopKeyAt(ipCx, ipCy), "imp_input",
                      "（正向控制）**弹窗开着**时同一点 = `imp_input` ⇒ 上一条不是「这个 key 没注册」");
                _rt.UiCloseImport();
                _rt.UiSetTab(1);
                Check(_rt.UiTopKeyAt(shCx, shCy), "info_share",
                      "（正向控制）切到 Deck info 页 ⇒ 同一点命中 `info_share`（只有藏起来时才不许命中）");
                Check(_rt.UiTopKeyAt(imCx, imCy), "info_import", "（正向控制）导入钮那位命中 `info_import`");
                _rt.UiClickPx(imCx, imCy);
                Check(_rt.ImportOpen, true, "……而且真的打开了导入弹窗（显示时照常、隐藏时才挡）");
                _rt.UiCloseImport();
                _rt.UiSetTab(0);
                CheckTrue(!_rt.ImportOpen && !_rt.NameEditing, "（收尾）弹窗关着、回到 Cards 页");
            }
            Shoot("deck_cards.png");

            // ============================================================ 🆕 2026-10-04（A24）悬停换图 + 状态换图
            // 这一整段以前**一条断言都没有** ⇒ 屏幕上「悬停什么都不发生」也全绿（普查 A24 的结论）。
            // 判据逐条 = 原 prefab 的字段；出处写在 `CheckHoverOne` 上面那一段。
            Section("悬停换图（原版 `m_Transition=2` 的那 5 颗）");
            {
                CheckHoverOne("hdr_back", "hdr_back", "UI_Button_Mulligan", "UI_Button_Mulligan_hover",
                              "UI_Button_Mulligan_Pressed", "页头 `Close`（文本 'Back'）");
                CheckHoverOne("hdr_clear", "hdr_clear", "UI_Button_Mulligan", "UI_Button_Mulligan_hover",
                              "UI_Button_Mulligan_Pressed", "页头 `Clear filters`");
                CheckHoverOne("foot_done", "foot_done", "UI_Button_Mulligan", "UI_Button_Mulligan_hover",
                              "UI_Button_Mulligan_Pressed", "页脚 `Done`");
                // 按下态（原版 `m_PressedSprite`）：按着的时候画按下图，抬起还原
                if (_rt.UiBtnRect("foot_done", out float dX, out float dY, out float dW, out float dH))
                {
                    var done = _root.Find("foot_done") != null ? _root.Find("foot_done").GetComponent<WindowButton>() : null;
                    CheckTrue(_rt.UiPressAt(dX + dW * 0.5f, dY + dH * 0.5f, true, false) == done,
                              "页脚 `Done`：左键按下 ⇒ 进**按下态**（原版那颗有 `m_PressedSprite`）");
                    if (done != null)
                        CheckTrue(done.CurrentTexForTest == done.PressedTexForTest,
                                  "按下时贴图 = 原版 `UI_Button_Mulligan_Pressed`");
                    _rt.UiPressAt(dX + dW * 0.5f, dY + dH * 0.5f, false, true);
                    if (done != null)
                        CheckTrue(done.CurrentTexForTest == done.NormalTexForTest, "抬起 ⇒ 还原成常态图");
                }
                // 导入弹窗那两颗是**模态件**（`_modalOnly` 出厂 `SetActive(false)`）⇒ 先打开再验
                _rt.UiOpenImport();
                CheckHoverOne("imp_ok", "imp_ok", "40K_button", "40K_button_hover", "40K_button_pressed",
                              "导入弹窗 `Confirm`");
                // ⚠️ 这颗的 `m_TargetGraphic` 实测 = **子件 `Icon`**（= 我们那颗 `imp_close_x`），
                //    不是按钮节点的圆底（普查 §二 块 B 写的是按钮节点的常态图 —— 本轮已就地订正）
                CheckHoverOne("imp_close", "imp_close_x", "40k_bt_close", "40k_bt_close_hover",
                              "40k_bt_close_pressed", "导入弹窗关闭钮（原版目标是**子件 `Icon`**）");
                // 🔴 **2026-10-04（A37 ⑤）**：那颗图标我们原来画 **75×75**（跟着圆底走）。
                //   判据（`python 工具/menu_dump.py bundle_menus_assets_all "Import Deck Popup" --depth 8`）：
                //   `Generic Close Button Green` = `[1317.3,202.1]–[1392.3,277.1]`（**75×75 圆底，我们那层是对的**）·
                //   子件 `Icon` = `[1326.6,212.4]–[1383.0,266.8]` = **56.37×54.50**，图 `40k_bt_close` 175×174
                //   ·`Simple (1,1,1,1)` **无 preserveAspect**（原版是拉进这个矩形）。
                if (_rt.UiQuadRect("imp_close_x", out float ix, out float iy, out float iw, out float ih))
                {
                    CheckNear(iw, 56.37f, 0.02f, "导入弹窗关闭图标的宽 = 原版子件 `Icon` 的 **56.37**（原来 75）");
                    CheckNear(ih, 54.5f, 0.02f, "...高 = **54.50**（原来 75）");
                    CheckNear(ix, 1354.785f, 0.02f, "...中心 x = 1326.6 + 56.37/2");
                    CheckNear(iy, 239.65f, 0.02f, "...中心 y = 212.4 + 54.5/2");
                }
                else CheckTrue(false, "`imp_close_x` 量得到矩形（量不到 = 那颗关闭图标根本没画）");
                // 模态遮挡：弹窗开着时**背后**那颗不该亮（原版靠弹窗的全屏暗底吃射线，我们没建那块暗底）
                CheckTrue(_rt.UiHoverAt(267.2f, 113.5f) == null,
                          "导入弹窗开着时，**弹窗背后**的 `Back` 不亮（`HoverTargetUnder` 的模态口径）");
                _rt.UiCloseImport();
                Check(_rt.ImportOpen, false, "（收尾）导入弹窗关掉");
                // 反向：**关着**时那两颗看不见的钮不许悬停得上（否则会在看不见的件上亮起来 —— 静默的那种错）
                CheckTrue(_rt.UiHoverAt(960f, 648.5f) == null,
                          "导入弹窗**关着**时，`Confirm` 那一片没有悬停反应（`imp_*` 只在弹窗开着时算）");
                // 兜底：整棵树里**每一颗**接了换图的都要「换得动 + 还原得回」，且图片一张不缺
                CheckHoverSwap(_root, "卡组编辑窗");
                CheckNoMissingSwapArt("卡组编辑窗");

                // 🔴 **2026-10-04（A37 ④）**：点击与悬停**共用同一张命中顺序表**（`DeckRuntime.ClickOrder`）。
                //   原来两套口径（点击走 `HandleButtons` 的 if 链 · 悬停走 `_btns` 的**登记顺序**），
                //   而 `HoverTargetUnder` 的注释却写着「命中口径 = 和点击同一条」—— **声明是错的**。
                //   这条断言盯的就是「两套再分叉」：漏写进表里的那颗会「亮得起来但点不到」。
                var order = DeckRuntime.UiClickOrder();
                var hkeys = _rt.UiHoverKeys();
                CheckTrue(hkeys.Count >= 5,
                          $"（前提）确实有接了悬停换图的按钮（实得 {hkeys.Count} 颗；少于 5 ⇒ 本批那 5 颗没接全）");
                var notInOrder = new List<string>();
                foreach (var k in hkeys) if (System.Array.IndexOf(order, k) < 0) notInOrder.Add(k);
                CheckTrue(notInOrder.Count == 0,
                          "接了悬停换图的 key **全都在 `ClickOrder` 里**（漏的会「亮得起来但点不到」；漏的："
                          + string.Join("、", notInOrder.ToArray()) + "）");
                var noRect = new List<string>();
                foreach (var k in order)
                    if (!_rt.UiBtnRect(k, out _, out _, out _, out _)) noRect.Add(k);
                CheckTrue(noRect.Count == 0,
                          "`ClickOrder` 里每个 key 都真的注册了矩形（写错 key / 按钮被删 ⇒ 红；坏的："
                          + string.Join("、", noRect.ToArray()) + "）");
            }

            Section("悬停**变色**（原版 `m_Transition = 1(ColorTint)` 那批 —— A32②）");
            {
                // 判据 = **逐颗读 prefab**：`m_Transition = 1` + `m_Colors` = UGUI 默认那组
                //   （HL `0.9607843` / P `0.7843137`）+ **目标件是看得见的**（`m_Color.a > 0` 且 `m_Enabled = 1`）。
                //   「看不看得见」的唯一判据是**目标件**，不是「有没有 `m_Transition`」。
                // ⚠️ 本窗**没有** `PointerLayer` ⇒ 派发走 `HoverTargetUnder`（`ClickOrder` 那 14 个 key +
                //   `DrawerHoverUnder` 的抽屉那几格），`UiHoverAt` 走的正是这一条。

                // ① 页头 `Filters`（`EverguildToggle`，MB `-3758886955019145436`）—— 它**两种行为都有**：
                //    状态换图（上面那一节验过）+ 悬停变色（这里验）。换的是**纹理**、变的是**顶点色**，互不覆盖。
                _rt.UiBtnRect("hdr_filters", out float hfx, out float hfy, out float hfw, out float hfh);
                CheckHoverTint(_rt.UiHoverAt(hfx + hfw * 0.5f, hfy + hfh * 0.5f), "页头 `Filters`");

                // ② `Deck Name` 输入框（`EverguildInputField`）—— 目标件 = 它自己那张 `InputFieldBackground`
                //    （`m_Color = (0.0627,0,0,1)` 不透明）⇒ 看得见。九宫格那颗**9 块要一起变**
                //    （`WindowButton.Collect` 收的是自己子树里全部 `ImageQuad`）。
                _rt.UiBtnRect("name_box", out float nbx, out float nby, out float nbw, out float nbh);
                CheckHoverTint(_rt.UiHoverAt(nbx + nbw * 0.5f, nby + nbh * 0.5f), "`Deck Name` 输入框");

                // ③ 抽屉里那几格（搜索框 + 两个开关）：**先把抽屉打开**（批处理下 `UiToggleFilters` 一步到位）
                if (!_rt.FiltersOpen) _rt.UiToggleFilters();      // ⚠️ 写成「没开才开」—— 不依赖上一条留的状态
                CheckTrue(_rt.FiltersOpen, "（前提）筛选栏打开了（下面几条才有格子可打）");
                foreach (var kk in new[] { "$name", "$owned", "$upgradable" })
                {
                    string who = kk == "$name" ? "筛选栏的搜索框" : ("开关格 `" + kk + "`");
                    if (!_rt.UiDrawerHitRect(kk, out float dx, out float dy, out float dw, out float dh))
                    { Check(true, false, who + "：这一格量不到（抽屉没开？）"); continue; }
                    CheckHoverTint(_rt.UiHoverAt(dx + dw * 0.5f, dy + dh * 0.5f), who);
                }

                // ④ 🔴 **反向断言（A32② 里「原版本身就没有」的那半）**：卡池四个**选项行**的格子
                //    `m_Transition = 0(None)` ⇒ 原版悬停什么都不变 ⇒ 我们**也不许**亮。
                //    这条同时是上面那三颗的**对照**：不给它加，是因为判据说没有，不是漏了。
                if (_rt.UiFilterCell("$rar:legendary", out float rx, out float ry, out float rw, out float rh, out _))
                    CheckTrue(_rt.UiHoverAt(rx + rw * 0.5f, ry + rh * 0.5f) == null,
                              "Rarity 那一行的格子**不接悬停**（原版 `m_Transition = 0` ⇒ 悬停什么都不变）");
                else Check(true, false, "（对照那一条要的 `$rar:legendary` 格子没量到）");

                // ⑤ 抽屉**收起**后不许再亮（`FltStripOn` 那道门 = 与点击同一条判据）。
                //    ⚠️ 写法上比的是「**同一个坐标**前后打到的对象」+「那一颗的色偏还原没」
                //    —— **不是**比 `== null`：抽屉底下压着侧栏页签（矩形重叠），关掉之后那个坐标
                //    本来就可能命中别的 key（今天页签没接悬停 ⇒ 仍返回 null，但那不是这条要盯的东西）。
                if (_rt.UiDrawerHitRect("$owned", out float ox, out float oy, out float ow, out float oh))
                {
                    float mx = ox + ow * 0.5f, my = oy + oh * 0.5f;
                    var wbOpen = _rt.UiHoverAt(mx, my);
                    CheckTrue(wbOpen != null, "（前提）抽屉开着时，`$owned` 那一格真的被打到了");
                    _rt.UiToggleFilters();
                    CheckTrue(!_rt.FiltersOpen, "（前提）筛选栏收起来了");
                    CheckTrue(_rt.UiHoverAt(mx, my) != wbOpen,
                              "抽屉收着时，原来那一格**不再**被悬停派发打到（闸门 = `FltStripOn`）");
                    CheckTrue(wbOpen == null || Mathf.Abs(wbOpen.TintKForTest - 1f) < 1e-4f,
                              "……而且那一颗的色偏**还原成 1**（不许停在悬停态：`UpdateButtonHover` 每帧重算）");
                }
                else Check(true, false, "（收起那条要的 `$owned` 矩形没量到）");

                // 收尾：抽屉**必须是关的** —— 下一节（`状态换图`）的 ① 走的是「点一下 ⇒ 变按下态」，
                //   它假设进来时抽屉是关的（`UiToggleFilters` 是**翻转**，不是「设成开」）。
                if (_rt.FiltersOpen) _rt.UiToggleFilters();
                CheckTrue(!_rt.FiltersOpen, "（收尾）筛选栏回到关着（不把状态留给下一节）");
            }

            Section("状态换图（原版 `trans=1` + `onSprite/offSprite` 的那 7 颗）");
            {
                // ① 页头 `Filters`（`EverguildToggle` · `changeSpriteOnValueChange=1` · `m_IsOn=0`）
                Check(_rt.UiTextureName("hdr_fltbtn"), "40k_menu_bt",
                      "Filters 键**关着**时 = `40k_menu_bt`（原版 `offSprite`）");
                _rt.UiBtnRect("hdr_filters", out float fbX, out float fbY, out float fbW, out float fbH);
                _rt.UiClickPx(fbX + fbW * 0.5f, fbY + fbH * 0.5f);          // 走鼠标那条路点它
                Check(_rt.FiltersOpen, true, "（前提）点一下 Filters 键 ⇒ 筛选面板打开");
                Check(_rt.UiTextureName("hdr_fltbtn"), "40k_menu_bt_pressed",
                      "面板开着 ⇒ 换成 `40k_menu_bt_pressed`（原版 `onSprite`；原来**开着也不换图**）");
                _rt.UiClickPx(fbX + fbW * 0.5f, fbY + fbH * 0.5f);
                Check(_rt.UiTextureName("hdr_fltbtn"), "40k_menu_bt", "关回去 ⇒ 换回 `40k_menu_bt`");

                // 🆕 2026-10-05（**A76②**）：**逻辑态 vs 动画态** —— 判据链在 `RefreshHeader` 那段长注释里
                //   （`EverguildToggle.OnPointerClick` → UGUI `Toggle.Set(!m_IsOn,true)` ⇒ `m_IsOn` 点下去就翻、
                //     `onValueChanged` 同步换图；那 0.3 秒属于**抽屉**的 `DOAnchorPosX`）。
                //   ⇒ 抽屉**滑到一半**时页头**已经**是按下图。这条就是它的「改坏会红」：
                //     谁把 `RefreshHeader` 那颗改成跟 `Slide`/`SlideTarget` 走，这里立刻报 `40k_menu_bt`。
                _rt.UiToggleFilters();                                        // 打开（批处理：一步到位 + 逻辑态翻真）
                CheckTrue(_rt.FiltersOpen, "（前提）抽屉开着");
                _rt.SetDrawerProgressForTest(false, 0.5f);                    // 把它**钉在滑动中途**（不动 SlideTarget）
                _rt.UiScrollPool(0f);                                         // 逼一次 `RefreshHeader`（会重算这颗图的就它）
                Check(_rt.UiTextureName("hdr_fltbtn"), "40k_menu_bt_pressed",
                      "抽屉**滑到一半**（`Slide=0.5`）时页头**已经**是按下图 —— 原版按下态跟的是**逻辑态**"
                      + "（`m_IsOn`），那 0.3 秒是**抽屉**在滑；改成跟动画进度走就红");
                _rt.SetDrawerProgressForTest(false, 1f);                      // 摆回到位
                _rt.UiToggleFilters();                                        // 关回去（把状态留给下一节）
                CheckTrue(!_rt.FiltersOpen, "（收尾）抽屉关着");

                // ② 侧栏三页签 —— 判据 = UGUI `Toggle.PlayEffect`：`graphic`（= 子件 `Highlight`）的 alpha 0/1
                CheckTrue(Mathf.Abs(_rt.UiTabHighlightAlpha(0) - 1f) < 1e-4f,
                          "Cards 页签（选中）的 `Highlight` alpha = 1");
                CheckTrue(Mathf.Abs(_rt.UiTabHighlightAlpha(1) - 0f) < 1e-4f,
                          "Deck info（未选中）的 alpha = **0**（原来打 0.15 ⇒ 未选中的页签上压着一层 15% 幽灵高亮）");
                CheckTrue(Mathf.Abs(_rt.UiTabHighlightAlpha(2) - 0f) < 1e-4f,
                          "Cosmetics（未选中）的 alpha = 0");
                _rt.UiSetTab(1);
                CheckTrue(Mathf.Abs(_rt.UiTabHighlightAlpha(1) - 1f) < 1e-4f,
                          "切到 Deck info ⇒ 它的高亮亮起来");
                CheckTrue(Mathf.Abs(_rt.UiTabHighlightAlpha(0) - 0f) < 1e-4f,
                          "Cards 的高亮同时灭掉（三选一）");
                _rt.UiSetTab(0);

                // 🔴 **2026-10-04（A37 ①）**：上面那几条**只验 alpha ⇒ 对颜色是瞎的** ——
                //   把 tint 打成白色（我们原来就是）照样全绿。原版那颗高亮 = **灰度图 + 红 tint**：
                //   判据 ① prefab 实读：子件 `Highlight` 的 Image `m_Color = (1,0,0,1)`
                //     （`python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 6`
                //       那行里的 `Sliced (1,0,0,1)`，左边写着 `40k_main_bt_selected BW` = 灰度图）。
                //   判据 ② 通道语义：`Toggle.PlayEffect` → `CrossFadeAlpha` → `useRGB:false`
                //     ⇒ **只动 alpha、两个态的 RGB 都是红**。
                //   同工程早就做对过一次：`Shell/MenuWindowBase.cs:420-424`（同图 + `(1,0,0,1)`）。
                for (int i = 0; i < 3; i++)
                {
                    string who = i == 0 ? "Cards" : (i == 1 ? "Deck info" : "Cosmetics");
                    var c = _rt.UiTabHighlightTint(i);
                    CheckTrue(Mathf.Abs(c.r - 1f) < 1e-4f && Mathf.Abs(c.g) < 1e-4f && Mathf.Abs(c.b) < 1e-4f,
                              $"`{who}` 高亮 tint 的 **RGB = (1,0,0)**（原版 `m_Color` 是纯红；我们原来打的是白）"
                              + $"（实测 {c.r:F3},{c.g:F3},{c.b:F3}）");
                }
                // 顺带（A37 ①）：原版那颗 `Highlight` 的 rect = **整格 108.96×150**（与父节点同矩形），
                //   我们原来画 100×100（只有图标那一方块）。
                //   判据 = dump 的 `Cards [0.3,156]–[109.2,306]` 与子件 `Highlight` 同串。
                if (_rt.UiQuadRect("tab_hi0", out float hx, out float hy, out float hw, out float hh))
                {
                    CheckNear(hw, 108.9667f, 0.02f, "`Cards` 高亮的宽 = **整格 108.96**（原版 `[0.3,109.2]`）");
                    CheckNear(hh, 150f, 0.02f, "`Cards` 高亮的高 = **整格 150**（原版 `[156,306]`）");
                    CheckNear(hx, 54.7833f, 0.02f, "高亮中心 x = 0.3 + 108.9667/2");
                    CheckNear(hy, 231f, 0.02f, "高亮中心 y = 156 + 150/2");
                }
                else CheckTrue(false, "`tab_hi0` 量得到矩形（量不到 = 那颗高亮根本没画）");

                // 🔴 **2026-10-04（A41 ⑥）**：上面那条只验了**矩形** —— 原版那颗 `Image` 的
                //   `m_Type = Sliced` + `m_Border (30,30,30,30)` + **`m_PixelsPerUnitMultiplier 0.92`**
                //   ⇒ 画出来是**九宫格**、角块 = `30 ÷ 0.92` = **32.6087px**；我们原来**单块拉伸**
                //   （71² 的图拉到 108.97×150 ⇒ 那条 ~30px 的软边被拉成 ~46px）。
                //   判据 = dump 那行的 `九宫30,30,30,30 | Sliced (1,0,0,1) ppuMul=0.9200000166893005`。
                Check(_rt.UiQuadCount("tab_hi0"), 9, "页签高亮是**九宫格**（原版 `Sliced`）—— 一棵树 9 块，不是单块拉伸");
                Check(_rt.UiTextureName("tab_hi0"), "40k_main_bt_selected_BW",
                      "……图还是那张 `40k_main_bt_selected BW`");
                Check(_rt.UiTabHighlightBlocks(0), 9, "……而且**整棵树**都在（`UiTabHighlightBlocks`；只建一块 = 漏了九宫格）");
                // 角块的大小只有量**单独那一块**才看得出来（`tab_hi0` 自己的包围盒永远是整格）
                if (_rt.UiQuadRect("tab_hi0/tab_hi0_00", out float cnx, out float cny, out float cnw, out float cnh))
                {
                    CheckNear(cnw, 32.6087f, 0.6f,
                              "角块宽 = 原版 `m_Border 30 ÷ ppuMul 0.92` = **32.6087**（不是 30 —— ppuMul 会缩放角块）");
                    CheckNear(cnh, 32.6087f, 0.6f, "……角块高 = 32.6087");
                }
                else Check(true, false, "`tab_hi0_00` 量得到（量不到 ⇒ 那层不是九宫格，退回单块了）");

                // 🔴 **2026-10-04（A41 ①）**：页签图标 `Icon` 的 rect = **整格 108.96×150** +
                //   `m_PreserveAspect = 1`，源图 126×126 ⇒ **实绘 108.9667²、在格子里居中**
                //   （我们原来画 100×100 ⇒ **小 8.97px ≈ 9%**）。
                //   判据 = dump 的 `Cards/Icon [0.3,156]–[109.2,306] 108.96×150 …… preserveAspect`。
                string[] tabWho = { "Cards", "Deck info", "Cosmetics" };
                string[] tabIc = { "40k_collection_bt_cards", "40k_collection_bt_decks", "40k_collection_bt_cosmetics" };
                for (int i = 0; i < 3; i++)
                {
                    Check(_rt.UiTextureName("tab_ic" + i), tabIc[i], $"`{tabWho[i]}` 图标用的是原版那张 `{tabIc[i]}`");
                    if (_rt.UiQuadRect("tab_ic" + i, out float icx, out float icy, out float icw, out float ich))
                    {
                        CheckNear(icw, 108.9667f, 0.6f,
                                  $"`{tabWho[i]}` 图标实绘宽 = 整格 **108.9667**（PA 内接 126² 的图；原来 100）");
                        CheckNear(ich, 108.9667f, 0.6f, $"`{tabWho[i]}` 图标实绘高 = **108.9667**（同上）");
                        CheckNear(icx, 0.3f + 108.9667f * i + 54.4833f, 0.6f, $"……中心 x = 格左缘 + 108.9667/2");
                        CheckNear(icy, 231f, 0.6f, "……中心 y = 156 + 150/2（在整格里居中）");
                    }
                    else Check(true, false, $"`tab_ic{i}` 量得到矩形（量不到 = 图标根本没画）");
                }

                // 🔴 **2026-10-04（A41 ②）**：页签**少了一块名牌底板**。原版 `Cards/Label`：
                //   图 `40k_main_bt_nametag`（109×41 · `Simple` · **无 PA** ⇒ 拉进矩形）、
                //   rect `[5.3,261]–[104.2,301]` = **98.96×40**（三个页签各 +108.97：`114.2` / `223.2` 逐条对上）；
                //   子件 TMP `Text` **与底板同一矩形**、`Center/Middle`、`m_enableAutoSizing=1` + `[10,34]`。
                //   我们原来只画 TMP 文字（100×26 @ y271）⇒ **没有底板、字框小 14px、锚点也不同**。
                //   页面左缘 = `Cards [0.3]` + 5.0 = 5.3。
                for (int i = 0; i < 3; i++)
                {
                    float x1 = 5.3f + 108.9667f * i;
                    Check(_rt.UiTextureName("tab_nm" + i), "40k_main_bt_nametag",
                          $"`{tabWho[i]}` 名牌底板 = 原版那张 `40k_main_bt_nametag`");
                    if (!_rt.UiQuadRect("tab_nm" + i, out float ncx, out float ncy, out float nw, out float nh))
                    { Check(true, false, $"`tab_nm{i}` 量得到矩形（量不到 = 名牌根本没画）"); continue; }
                    CheckNear(nw, 98.96f, 0.6f, $"`{tabWho[i]}` 名牌宽 = 原版 **98.96**（`[5.3,104.2]`）");
                    CheckNear(nh, 40f, 0.6f, $"……高 = **40**（`[261,301]`）");
                    CheckNear(ncx, x1 + 98.96f * 0.5f, 0.6f, $"……中心 x = {x1:F1} + 98.96/2");
                    CheckNear(ncy, 281f, 0.6f, "……中心 y = 261 + 40/2");
                    // 那行字：**不许画出名牌框**（原版就是靠 `m_enableAutoSizing` 把它缩进去的）
                    float fpx = _rt.UiTabLabelFontPx(i);
                    CheckTrue(fpx > 0f,
                              $"`{tabWho[i]}` 名牌字号读得到（≤0 = TMP 没起来、走的是点阵兜底；实测 {fpx:F2}）");
                    // 🔴 **2026-10-04（A46）：字色 = 原版 `m_fontColor`，恒白。**
                    //   判据 = prefab 实读三颗 `Text`（Cards / Deck info / Cosmetics）的 `m_fontColor`
                    //   **都是 `(1,1,1,1)`**（`menu_dump.py … "Deck Editing Menu"` 那三行的 `色=(1,1,1,1)`）
                    //   ⇒ 选中与未选中**同色**，区分只靠身后那块红高亮。
                    //   ⚠️ 我们原来打的是 `Gold : Ink`（两个都是**自己挑的**）⇒ 这条会红。
                    var lc = _rt.UiTabLabelColor(i);
                    CheckTrue(Mathf.Abs(lc.r - 1f) < 1e-4f && Mathf.Abs(lc.g - 1f) < 1e-4f
                              && Mathf.Abs(lc.b - 1f) < 1e-4f && Mathf.Abs(lc.a - 1f) < 1e-4f,
                              $"`{tabWho[i]}` 名牌字色 = 原版 `m_fontColor` **(1,1,1,1) 纯白**"
                              + $"（实测 {lc.r:F3},{lc.g:F3},{lc.b:F3},{lc.a:F3}；"
                              + "原来打的是我们自己挑的 `Gold : Ink`）");
                    if (_rt.UiTabLabelRect(i, out float tcx, out float tcy, out float tw, out float th))
                    {
                        CheckTrue(tw <= 98.96f + 0.6f && th <= 40f + 0.6f,
                                  $"`{tabWho[i]}` 那行字**没画名牌框**：渲染 {tw:F1}×{th:F1} ≤ 框 98.96×40"
                                  + "（画出去 = 漏了自适应）");
                        // 🔴 **2026-10-04（A51 F3）：这条是把原来那条【同义反复】的区间断言换掉的。**
                        //   原来断的是 `10 ≤ fpx ≤ 34` —— 那两个数**正是我们从 `TabNameMinPx/MaxPx`
                        //   传进去的**，而 TMP 的自适应**构造上**就落在 `[fontSizeMin, fontSizeMax]` 内
                        //   （`TextMeshPro.cs:3567-3580` 缩、`:4139-4149` 涨，两头都夹在这个区间）
                        //   ⇒ 把 max 改成 20、甚至把 `SetAutoFitBox` 整句删掉，那条**照样绿**。
                        //   ⇒ 换成**有区分力**的判据：TMP 只在「大一号就装不下」时才缩
                        //   （二分收敛到 **0.05 步长**，`TextMeshPro.cs:4149`；本工程 1 fontSize ≈ 10.24px
                        //     ⇒ 步长 ≈ **0.51px**）⇒ 结果只有两种可能：
                        //     ① 停在**原版上限 34**（判据 = 原版 `m_fontSizeMax = 34`；= 框根本没压它），或
                        //     ② 被压到**框的边界**上：宽贴住 **98.96** 或高贴住 **40**（= 原版 `Label` 的框）。
                        //   把 max 改成 20（或任何「小于框容得下的值」）⇒ 两个分支都不成立 ⇒ **真红**。
                        //   ⚠️ 容差取**比例**（94%）而不是像素等号：收敛点最多比边界低「一步」——
                        //      宽 ∝ 字号 ⇒ 低 0.51/fpx（fpx≈20 时 ≈ 2.5px）；高 ∝ 字号×行数 ⇒ 低 ≈ 0.51×行数
                        //      ⇒ 1~2.5px 的不确定度；而「字号被设成 20」那类错会让文本只占框的 ~50%(宽)/~72%(高)，
                        //      94% 这条线离两种情形都远。
                        const float FitFrac = 0.94f;
                        bool atMax = fpx >= 34f - 0.1f;
                        bool fillsW = tw >= 98.96f * FitFrac;
                        bool fillsH = th >= 40f * FitFrac;
                        CheckTrue(atMax || fillsW || fillsH,
                                  $"`{tabWho[i]}` 字号 = **原版上限 34**（框没压住它）**或**被压到**框的边界**上"
                                  + "（宽 98.96 / 高 40 的 ≥94%）—— 二者必居其一（TMP 只在『大一号就装不下』时才缩）；"
                                  + $"实测 字号 {fpx:F2} · 渲染 {tw:F1}×{th:F1}");
                        CheckTrue(Mathf.Abs(tcx - ncx) < 2f && Mathf.Abs(tcy - ncy) < 2f,
                                  $"……而且**居中**在名牌上（原版 `m_HorizontalAlignment=2` / `Middle`；"
                                  + $"字心 {tcx:F1},{tcy:F1} vs 名牌心 {ncx:F1},{ncy:F1}）");
                    }
                    else Check(true, false, $"`tab_tx{i}` 量得到渲染矩形");
                }
                // 🔴 **2026-10-04（A46）**：再加一条**「选中态与未选中态字色完全一样」** ——
                //   原版三颗 `Text` 的 `m_fontColor` 是**同一个值**（`(1,1,1,1)`），选中与否只改身后那块红高亮。
                //   此刻选中的是 `Cards`（上面那一段收尾 `UiSetTab(0)`）⇒ 正好一比一。
                //   （原来 `Gold : Ink` 那种「用字色区分选中」的写法会让这条红。）
                {
                    var cSel = _rt.UiTabLabelColor(0);      // 选中
                    var cUn = _rt.UiTabLabelColor(1);       // 未选中
                    CheckTrue(Mathf.Abs(cSel.r - cUn.r) < 1e-4f && Mathf.Abs(cSel.g - cUn.g) < 1e-4f
                              && Mathf.Abs(cSel.b - cUn.b) < 1e-4f && Mathf.Abs(cSel.a - cUn.a) < 1e-4f,
                              "选中的 `Cards` 与未选中的 `Deck info` **字色一模一样**"
                              + "（原版三颗 `m_fontColor` 同值、区分只靠红高亮）"
                              + $"（实测 选中 {cSel.r:F3},{cSel.g:F3},{cSel.b:F3} vs 未选中 {cUn.r:F3},{cUn.g:F3},{cUn.b:F3}）");
                }

                // ③ 筛选栏那三个开关：`40_main_bt_toggle_on` ↔ `40_main_bt_toggle_off`
                _rt.UiToggleFilters();
                Check(_rt.UiFilterCellTex("$owned"), "40_main_bt_toggle_on",
                      "`Owned only`（出厂开）画的是 `40_main_bt_toggle_on`");
                Check(_rt.UiFilterCellTex("$upgradable"), "40_main_bt_toggle_off",
                      "`Upgradable only`（出厂关）画的是 `40_main_bt_toggle_off`（原来**恒画 on 那张**）");
                bool gotOwn = _rt.UiFilterCell("$owned", out float oX, out float oY,
                                               out float oW, out float oH, out bool oOn);
                CheckTrue(gotOwn, "（前提）`$owned` 那一格量得到");
                CheckTrue(oOn, "（前提）`$owned` 出厂是【开】（原版 `m_IsOn=1`）");
                if (gotOwn)
                {
                    _rt.UiClickPx(oX, oY);                                   // 走鼠标那条路点这一格
                    Check(_rt.State.Filter.Owned, false, "（前提）点一下 ⇒ `Owned` 关掉");
                    Check(_rt.UiFilterCellTex("$owned"), "40_main_bt_toggle_off",
                          "关掉 ⇒ **换成 off 那张**（原来「从不换出来」，关掉的与打开的长得一模一样）");
                    _rt.UiClickPx(oX, oY);
                    Check(_rt.State.Filter.Owned, true, "（收尾）再点一下 ⇒ 回到出厂的【开】");
                    Check(_rt.UiFilterCellTex("$owned"), "40_main_bt_toggle_on", "开回去 ⇒ 换回 on 那张");
                }
                // 🆕 2026-10-05（A32③④）同一批开关行的两件事：**逐行 off 色** + **标签左对齐**。
                //   判据都是原 prefab 的字段（`EverguildToggle.offColor` · `Label.m_HorizontalAlignment`）。
                {
                    // ③ 逐行 off 色 —— 上面 `UiClearFilters()` 之后四个筛选格**全是关的**（下面两条各带一条前提）
                    var armyKey = _rt.State.Factions().Count > 0 ? ("$fac:" + _rt.State.Factions()[0]) : null;
                    if (armyKey != null && _rt.UiFilterCell(armyKey, out _, out _, out _, out _, out bool aOn))
                    {
                        CheckTrue(!aOn, "（前提）Army 某一格现在是【关】的 —— 下面断的是 off 色");
                        var ca = _rt.UiFilterCellTint(armyKey);
                        CheckTrue(Mathf.Abs(ca.r - 0.5f) < 2f / 255f && Mathf.Abs(ca.b - 0.5f) < 2f / 255f
                                  && Mathf.Abs(ca.a - 1f) < 2f / 255f,
                                  "Army 格 off 色 = 原版 `offColor (0.5,0.5,0.5,1)`"
                                  + $"（实得 {ca.r:F3},{ca.g:F3},{ca.b:F3},{ca.a:F3}；原来是共用的 0.349 ⇒ 偏深）");
                    }
                    else Check(true, false, "Army 某一格量不到（这条断的是 off 色）");
                    if (_rt.UiFilterCell("$rar:common", out _, out _, out _, out _, out bool rOn))
                    {
                        CheckTrue(!rOn, "（前提）Rarity 那一格现在是【关】的");
                        var cr = _rt.UiFilterCellTint("$rar:common");
                        CheckTrue(Mathf.Abs(cr.r - 0.5f) < 2f / 255f && Mathf.Abs(cr.a - 0.749f) < 2f / 255f,
                                  "Rarity 格 off 色 = 原版 `offColor (0.5,0.5,0.5,`**`0.749`**`)`"
                                  + $"（实得 {cr.r:F3},{cr.g:F3},{cr.b:F3},{cr.a:F3}）"
                                  + " —— Rarity 是四行里**唯一带 alpha** 的那个（191/255）");
                    }
                    else Check(true, false, "`$rar:common` 那一格量不到");

                    // ④ 标签**左对齐**（原版 `m_HorizontalAlignment = 1`）：期望值 = 原版那颗 `Label` 的
                    //    **rect 左缘**（`27.18,234.99→234.38,284.99`）⇒ 面板原点 2.2 + 25 = **27.2**。
                    //    居中的话文字左缘会落在 ~97px ⇒ 这条红。
                    if (_rt.UiFilterCellLabelLeft("$owned", out float labL))
                        CheckNear(labL, 27.2f, 1.2f,
                                  "`Owned only` 的标签**左对齐**在 x = **27.18**（原版 `Label` rect 左缘）");
                    else Check(true, false, "`$owned` 的标签量不到（这条量的是**文字左缘**，量到 0 会拒答）");
                }

                // 卡背页那颗（`Cosmetic FIlter > Filters > Owned Toggle`，出厂也是开）
                _rt.UiToggleFilters();                                       // 收起卡牌那套
                _rt.UiSetTab(2);
                _rt.UiToggleFilters();                                       // 开卡背那套（按 `_tab` 分派）
                Check(_rt.UiTextureName("hdr_fltbtn"), "40k_menu_bt_pressed",
                      "卡背页开抽屉 ⇒ 页头 `Filters` **也**变成按下的图"
                      + "（两页的 `filterToggle` 在 prefab 里就是同一颗）");
                Check(_rt.UiCosmoFilterCellTex("$owned"), "40_main_bt_toggle_on",
                      "卡背页 `Owned only`（出厂开）画的是 `40_main_bt_toggle_on`");
                bool gotCos = _rt.UiCosmoFilterCell("$owned", out float cX, out float cY,
                                                    out float cW, out float cH, out bool cOn);
                CheckTrue(gotCos, "（前提）卡背抽屉里那一格量得到");
                CheckTrue(cOn, "（前提）卡背页那个 `Owned only` 出厂也是【开】");
                if (gotCos)
                {
                    _rt.UiClickPx(cX, cY);
                    Check(_rt.UiCosmoFilterCellTex("$owned"), "40_main_bt_toggle_off",
                          "卡背页关掉它 ⇒ 也换成 `40_main_bt_toggle_off`");
                    _rt.UiClickPx(cX, cY);
                    Check(_rt.UiCosmoFilterCellTex("$owned"), "40_main_bt_toggle_on", "（收尾）换回 on 那张");
                }
                _rt.UiToggleFilters();                                       // 收起卡背抽屉
                Check(_rt.UiTextureName("hdr_fltbtn"), "40k_menu_bt",
                      "卡背抽屉收起来 ⇒ 页头那颗换回 `40k_menu_bt`");
                _rt.UiSetTab(0);
                Check(_rt.FiltersOpen, false, "（收尾）抽屉关着、回到 Cards 页签");
            }

            Section("卡组行的悬停色（原版 `m_HighlightedColor = 0.6887`）");
            {
                // 判据 = 原 prefab：`Deck Selector {Card Info button, Defensive Card Slot}` 的
                //   `m_Transition=1` + `m_TargetGraphic` = 子件 `Background`（图 `40k_deck_cardlist_bg`）
                //   + `m_HighlightedColor = (0.6886792,…)`；而 `Deck Selector Hero Card Info button`
                //   的目标图 `m_Color.a = 0`（全透明）⇒ **督军行悬停看不到变化**。
                var entries = _rt.DeckEntries();
                int rowCard = -1;
                for (int i = 0; i < entries.Count && i < 11; i++)
                    if (entries[i].Type != "hero") { rowCard = i; break; }
                CheckTrue(rowCard >= 0, "（前提）卡组里有一行**非督军**（才验得到「悬停变暗」）");
                if (rowCard >= 0 && _rt.UiRowRect(rowCard, out float rx, out float ry, out float rw, out float rh))
                {
                    // 悬停前先记下**别的几件**的颜色，验「只动目标那一件」（UGUI `ColorTint` 的语义）
                    var gradT = _root.Find("row_g" + rowCard);
                    var grad = gradT != null ? gradT.GetComponent<ImageQuad>() : null;
                    Color gradBefore = grad != null ? grad.Tint : Color.white;
                    var borderT = _root.Find("row_b" + rowCard);
                    var border = borderT != null ? borderT.GetComponentInChildren<ImageQuad>(true) : null;
                    Color borderBefore = border != null ? border.Tint : Color.white;

                    Check(_rt.UiRowHoverAt(rx + rw * 0.5f, ry + rh * 0.5f), rowCard,
                          $"指针压在第 {rowCard} 行（非督军）上 —— `UiRowHoverAt` 走的是鼠标那条路");
                    // 🔴 **2026-10-04（A37 ③）改**：这里原来写的是 `DeckRuntime.RowHoverK` —— **自证**
                    //   （期望值取我们自己的常量 ⇒ 把那常量改成错值 `0.9608` 照样绿）。
                    //   现在写死**原版字面量**：prefab `Deck Selector Defensive Card Slot` /
                    //   `Deck Selector Card Info button` 的 `m_Colors.m_HighlightedColor = 0.6886792182922363`
                    //   （实读 `python 工具/_probe_deckinfo.py bundle_menus_assets_all "Deck Selector Defensive Card Slot"`）。
                    CheckNear(_rt.UiRowBgTint(rowCard), 0.6886792f, 1e-3f,
                              "非督军行悬停 ⇒ 行底 **×0.6886792**（原版 `m_HighlightedColor` 字面量；"
                              + "不是全库默认的 0.9608）");
                    if (grad != null)
                        CheckTrue(grad.Tint == gradBefore,
                                  "悬停**只动行底那一件**：稀有度色条不变色（原版 `targetGraphic` = 子件 `Background`）");
                    if (border != null)
                        CheckTrue(border.Tint == borderBefore,
                                  "悬停**只动行底那一件**：行描边不变色（同上）");
                    _rt.UiRowHoverAt(1700f, 500f);                            // 指针移开
                    CheckNear(_rt.UiRowBgTint(rowCard), 1f, 1e-4f, "指针移开 ⇒ 行底还原成白（×1）");
                }
                if (_rt.UiRowRect(0, out float zx, out float zy, out float zw, out float zh)
                    && entries.Count > 0 && entries[0].Type == "hero")
                {
                    _rt.UiRowHoverAt(zx + zw * 0.5f, zy + zh * 0.5f);
                    CheckNear(_rt.UiRowBgTint(0), 1f, 1e-4f,
                              "**督军行**悬停**不上色**（原版那颗 `Deck Selector Hero Card Info button` 的"
                              + "目标图 `m_Color.a = 0` ⇒ 原版自己也看不见变化）");
                    _rt.UiRowHoverAt(1700f, 500f);
                }
            }

            // ============================================================ Cosmetics 页 = 换卡背（2026-09-24）
            // 判据、出处、以及「为什么列数不是字段里的 `_segments=4`」都写在 `DeckRuntime` 的 Cosmetics 那一段。
            // 原版：`Deck Editing Menu > Content Area > Cosmetic Display`（`CardbackCollectionDisplay`）。
            _rt.UiSetTab(1); Shoot("deck_info.png");
            _rt.UiSetTab(2);
            {
                var names = CardArt.CosmeticNames();
                Check(_rt.CosmoColsPx, 6,
                      "卡背网格 **6 列** = floor(1589.78 ÷ 250)（原版字段 `_segments=4` 是**死值**，照抄会少两列）");
                Check(_rt.CosmoCellWpx, 250f, "卡背格宽 250（原版 `_cellWidth`）");
                Check(_rt.CosmoCellHpx, 405f, "卡背格高 405（原版 `_cellHeight`）");
                Check(_rt.CosmoView.x, 330.23f, "网格视口左缘 330.23（原版 `Cosmetic Display/Scroll View`）");
                Check(_rt.CosmoView.y, 155.97f, "网格视口上缘 155.97");
                Check(_rt.CosmoView.z, 1589.78f, "网格视口宽 1589.78");
                Check(_rt.CosmoView.w, 924.06f, "网格视口高 924.06");
                Check(names.Length, 233, "卡背总数 233（2026-09-23 导进工程的那批）");
                CheckTrue(_rt.CosmoCellShown >= 24, $"一屏至少铺 24 格（实铺 {_rt.CosmoCellShown}）");
                Check(_rt.CosmoCellTex(0), names[0], "第 1 格 = 字典序第一张卡背");

                // ---- 右键装备 / 左键不做事（原版 `DeckEditingWindow__OnCosmeticClick.c:26`）----
                CheckTrue(string.IsNullOrEmpty(_rt.EquippedCardback),
                          $"起手**没装备过**（原版 `CardDeck.cardbackId` 出厂是空串，实得「{_rt.EquippedCardback}」）");
                // ---- 判据本身（`CardArt.DeckCardback` = 原版 `CardDeck.GetDeckCardback()`）----
                //  选了 → 那张 · 没选 → **该阵营的默认卡背** · 选了张取不到的 → 退回默认（**不静默画空白**）
                Check(CardArt.DeckCardback(names[3], "Ultramarines"), CardArt.Cosmetic(names[3]),
                      "判据：选了卡背 ⇒ 用**选的那张**");
                CheckTrue(CardArt.DeckCardback(null, "Ultramarines") == CardArt.CardBack("Ultramarines"),
                      "判据：没选过 ⇒ **该阵营的默认卡背**（原版 `ArmyUtilities.GetDefaultCardback(army)`）");
                CheckTrue(CardArt.DeckCardback("不存在的卡背_zzz", "Ultramarines") == CardArt.CardBack("Ultramarines"),
                      "判据：选了张**取不到的** ⇒ 退回默认（存档跨版本/手改过时不许静默变空白）");

                // ---- 🆕 2026-10-03（A20）：**13 个阵营的默认卡背全都在** ----
                //  判据 = 原版 `DefaultCarbackByArmySO`（**13 条、没有 Neutral**）—— 它不在任何 bundle/导出里，
                //  读法 → `工具/read_default_cardbacks.py`（原始字节手工切）；表 → `Resources/Cardbacks.json` 的 `defaults`。
                Check(CardbackTable.DefaultCount, 13, "默认卡背表 **13 个阵营**（原版那 SO 就是 13 条、没有 Neutral）");
                {
                    var facs = new[] { "Ultramarines", "Goff", "SaimHann", "Sautekh", "BlackLegion", "Leviathan",
                                       "TauEmpire", "Sororitas", "Genestealers", "AstraMilitarum", "DarkAngels",
                                       "EmperorsChildren", "SpaceWolves" };
                    int ok = 0;
                    var bad = new System.Text.StringBuilder();
                    foreach (var f in facs)
                    {
                        var nm = CardbackTable.DefaultFor(f);
                        var t = CardArt.CardBack(f);
                        bool sdf = CardArt.CardBackSdf(f) != null;
                        if (!string.IsNullOrEmpty(nm) && t != null && sdf) ok++;
                        else bad.Append(f).Append("（").Append(string.IsNullOrEmpty(nm) ? "无表项" : nm)
                            .Append(t == null ? " · 图取不到" : "").Append(sdf ? "" : " · SDF 取不到").Append("）");
                    }
                    Check(ok, facs.Length,
                          "13 个阵营**都能取到默认卡背的图 + SDF**（缺的：" + (bad.Length == 0 ? "无" : bad.ToString()) + "）");
                    CheckTrue(CardbackTable.DefaultFor("Neutral") == null, "`Neutral` **没有**默认卡背（原版那表就没有它）");
                }
                // 🔴 **2026-10-03 就地更正（A20）**：这里原来断「本卡组没有督军 ⇒ 抽屉不画卡背」——
                //    **那句的前提是错的**：跑到这里卡组**已经有督军了**（上面 `SetWarlord` 那段），
                //    原来之所以量到「<无>」，是因为那个阵营的 `Art/cards/back_*.png` **压根没有那张图**
                //    （只有 4 个阵营有）⇒ **它断的其实是「图缺」、不是「不该画」**。
                //    A20 起 13 个阵营都有默认卡背 ⇒ **该画**，而且画的必须是**这副牌阵营**那张。
                {
                    var wlFac = _rt.FactionOf(_rt.State.Deck.WarlordId);
                    Check(_rt.CosmeticDrawerTex, CardbackTable.DefaultFor(wlFac),
                          "抽屉画的是**这副牌阵营的默认卡背**（原版 `ArmyUtilities.GetDefaultCardback(army)`；阵营 = "
                          + (string.IsNullOrEmpty(wlFac) ? "<空>" : wlFac) + "）");
                }
                float cx0 = 330.23f + (1589.78f - 6f * 250f) * 0.5f + 250f * 0.5f;   // 第 1 格中心
                float cy0 = 155.97f + 405f * 0.5f;
                CheckTrue(!_rt.DeckDirty, "（前提）这一节起手是干净的（没落盘过任何东西）");
                CheckTrue(_rt.UiClickCosmetic(cx0, cy0, false), "左键点第 1 格：**命中了**（原版左键「什么都不做」，但不是点不到）");
                CheckTrue(string.IsNullOrEmpty(_rt.EquippedCardback), "……而且**没装备**（原版只有右键才装备）");
                CheckTrue(_rt.UiClickCosmetic(cx0, cy0, true), "右键点第 1 格：命中");
                Check(_rt.EquippedCardback, names[0], "……而且**装备上了**（原版 `editingDeck.cardbackId = item.GetID()`）");
                Check(_rt.CosmeticDrawerTex, names[0], "侧栏抽屉那张图跟着换成**装备的那张**");

                // 换一格（第 2 行第 3 列 = 第 9 格）—— 验「行列反算」不是碰巧对
                float cx8 = 330.23f + (1589.78f - 6f * 250f) * 0.5f + 250f * 2.5f;
                float cy8 = 155.97f + 405f * 1.5f;
                CheckTrue(_rt.UiClickCosmetic(cx8, cy8, true), "右键点第 2 行第 3 列：命中");
                Check(_rt.EquippedCardback, names[8], $"……装备的是第 9 张（{names[8]}）—— 行列反算对");

                // ---- 滚到底：最后一格 = 最后一张 ----
                _rt.UiScrollCosmetics(1e6f);
                Check(_rt.CosmoScrollPx, _rt.MaxCosmoScrollPx, $"滚到底 = MaxCosmoScrollPx（{_rt.MaxCosmoScrollPx:F1}）");
                {
                    int firstRow = Mathf.FloorToInt(_rt.CosmoScrollPx / 405f);
                    int vi = (names.Length - 1) - firstRow * 6;
                    Check(_rt.CosmoCellTex(vi), names[names.Length - 1], "滚到底：最后一格 = 字典序最后一张卡背");
                }
                _rt.UiScrollCosmetics(-1e6f);
                Check(_rt.CosmoScrollPx, 0f, "滚回顶");
                // ---- 存档（🔴 **2026-10-12（A363）改向**）----
                //   原版换完卡背是**标脏**、不是立刻上传：`DeckEditingPanel__Drop.c:26` 在写完
                //   `deck.cardbackId` 之后紧跟一句 `deck[+0x60] = 0`；真上传只在
                //   `DeckEditingWindow__TrySaveDeck.c:80` 校验通过之后的 `:100 UploadDeck`。
                //   （原来这条断的是「换一下**就**落盘」—— 那是我们的偏离，已按原版改掉。）
                CheckTrue(_rt.DeckDirty, "★ A363：换完卡背 ⇒ **标脏**（`Drop.c:26` 那句 `[+0x60] = 0`）");
                {
                    var stale = DeckLibrary.Load();
                    CheckTrue(stale.Current.CardbackId != names[8],
                              "★ A363：……而且**盘上还是旧的**（「" + (stale.Current.CardbackId ?? "<空>")
                              + "」）—— 改一下就写盘 = 这条红");
                }
                // Done 那一拍（ESC 与 Done 是**同一个函数**：原版 `__ESCPressed.c:5` 就一句 `TrySaveDeck`）
                Check(_rt.State.Validate(), DeckError.None,
                      "（前提）这副牌此刻合法 ⇒ 下面按 Done 会真的落盘（不合法时那道闸会挡住）");
                _rt.EscPressed();
                CheckTrue(!_rt.DeckDirty, "★ A363：按 Done ⇒ 脏标记清掉（= 写成功了）");
                var reread = DeckLibrary.Load();
                Check(reread.Current.CardbackId, names[8],
                      "★ A363：……**Done 之后才**落盘，重读存档就是那张"
                    + "（`CommitCurrent` 是逐字段拷的，漏一个字段就静默丢）");
                // ⚠️ **不复位** —— 后面那张 Cosmetics 截图就拍「装备了第 9 张」的样子（正好当实拍证据）；
                //    卡组数据在临时存档里（`DeckStore.OverridePath`），跑完就删，不碰玩家的真存档。
                _rt.UiSetTab(2); Shoot("deck_cosmetics.png");
            }

            // ============================================================ 🆕 2026-10-01
            //  卡背页那个左抽屉（原版 `Cosmetic FIlter` → `Army Filter` + `Owned Toggle`）。
            //  判据 = **实读那棵树**（`python 工具/menu_rect.py …bundle_menus_assets_all "Cosmetic FIlter" --depth 5`，
            //  命中两个同名节点、取 `-372539790263455964` 那个 = 卡组编辑这棵树）
            //  ＋ 原版 SO 的 `cardArmy`（表 → `Core/CardbackTable.cs`，生成 → `工具/gen_cardbacks.py`）。
            //  ⚠️ 在这之前，点这一页的 `Filters` 只会**如实出声「还没建」**。
            {
                Check(_rt.CosmoFiltersOpen, false, "卡背抽屉**出厂是关的**（原版 `Cosmetic FIlter` 是 INACT）");
                _rt.UiSetTab(2);
                _rt.UiClickPx(392.2f, 113.5f);                    // 页头那颗 `Filters`
                Check(_rt.CosmoFiltersOpen, true, "……点 `Filters` ⇒ **开的是卡背自己那个抽屉**（不再是「还没建」）");
                Check(_rt.FiltersOpen, false, "……而且**没有**把卡牌那七行端上来（两套是两棵 prefab）");
                Check(_rt.UiCosmoFilterCellCount, 14,
                      $"抽屉里 **14 格** = 13 个阵营 + 1 个 `Owned only`（实 {_rt.UiCosmoFilterCellCount}）");
                Shoot("deck_cosmo_filters.png");        // 实拍证据：抽屉开着的样子（改版面必看截图）
                {
                    // ⚠️ 用**第一个阵营**（`State.Factions()` 是**字典序**的）—— 第一版这里写死了 `Ultramarines`，
                    //    而它排第 13 ⇒ 落在第 5 行（+400 px）⇒ **断言自己错了**（自检当场抓到，已改）。
                    string fac0 = _rt.State.Factions()[0];
                    float x, y, w, h, wy; bool on, won;
                    CheckTrue(_rt.UiCosmoFilterCell("$fac:" + fac0, out x, out y, out w, out h, out on),
                              $"找得到阵营格 `$fac:{fac0}`");
                    CheckTrue(_rt.UiCosmoFilterCell("$owned", out w, out wy, out w, out h, out won),
                              "找得到 `$owned`（Owned only）那一格");
                    CheckTrue(wy > y + 100f,
                              $"`Owned` 在 Army 行**下面**（y {y:F0} → {wy:F0}）—— 原版这两行的**行序与卡牌那套相反**");
                    CheckTrue(won, "`Owned only` **出厂就是开的**（原版 `showOnlyOwnedCards` 初值 true + prefab `m_IsOn=1`）");

                    float cx, cy, cw, ch; bool con;
                    _rt.UiCosmoFilterCell("$fac:" + fac0, out cx, out cy, out cw, out ch, out con);
                    Check(cw, 100f, "阵营格 **100×100**（原版那棵树里 `Toggle` 模板的实测值）");
                    CheckTrue(Mathf.Abs(cy - (155.97f + 15f + 50f + 50f)) < 0.5f,
                          $"**第一个**阵营格中心 y = 抽屉顶 + 15 + 50 + 50 = {155.97f + 15f + 50f + 50f:F2}（实 {cy:F2}）"
                        + "—— Spacing 15 → Army `Title` 50 → `Content` 起");
                }

                // ---- 真的筛了没有（判据与铺格**同一份** `CardbackTable`）----
                string fac1 = _rt.State.Factions()[1];
                int allN = _rt.UiCosmoShownCount;
                _rt.UiCosmoFilterRow("$fac:" + fac1);
                int umN = _rt.UiCosmoShownCount;
                Check(_rt.CosmoFilterArmy, fac1, "点一个阵营 ⇒ 条件记下来了");
                CheckTrue(umN > 0 && umN < allN, $"…铺出来的张数真的变了（{allN} → {umN}）");
                Check(CardbackTable.ArmyOf(_rt.CosmoCellTex(0)), fac1,
                      $"……筛完第 1 格确实是那个阵营的卡背（`{_rt.CosmoCellTex(0)}`）");
                Check(_rt.State.Filter.Faction, "", "……而且**没污染卡池**那套筛选条件（两套分开，原版是两棵 prefab）");
                _rt.UiCosmoFilterRow("$owned");
                Check(_rt.CosmoFilterOwned, false, "`Owned only` 能切换（原版 `ToggleShowOwnedCards`）");
                Check(_rt.UiCosmoShownCount, umN,
                      "……但**张数不变** —— 单机全解锁，这个开关不改变结果（**如实标**，不是静默失效）");
                _rt.UiCosmoFilterRow("$owned");
                _rt.UiCosmoFilterRow("$fac:" + fac1);            // 再点一次 = 取消
                Check(_rt.CosmoFilterArmy, "", "阵营格**再点一次 = 取消**（与卡牌那套同一条手感）");
                Check(_rt.UiCosmoShownCount, allN, "……张数回到全量");

                // ---- 表本身：**每一张本地卡背都查得到阵营**（生成脚本对账过 243 SO → 233 图名；这条防手改坏）----
                var allCb = CardArt.CosmeticNames();
                int noArmy = 0;
                for (int i = 0; i < allCb.Length; i++)
                    if (string.IsNullOrEmpty(CardbackTable.ArmyOf(allCb[i]))) noArmy++;
                Check(noArmy, 0, $"**张张卡背都查得到阵营**（查不到 {noArmy} 张；表 = `Resources/Cardbacks.json`）");
                Check(CardbackTable.Count, allCb.Length,
                      $"表里条数 = 本地卡背张数（{CardbackTable.Count} vs {allCb.Length}）");
                // 🆕 2026-10-04（A67）：卡背抽屉走**同一套滑动引擎**；而且它多了「**页在不在**」那一半 ——
                //   切走再回来：**逻辑态保留**、画面跟着页瞬时开关、**不重放那 0.3 秒**。
                //   ⚠️ 「切页签不放动画」是**我们挑的**口径（原版 `OnEnable → ToggleFilters(bool)` 回来时
                //   会不会重跑 tween 判不出来 —— 泛型方法体缺失，见 `DeckRuntime` 的 §A67 那段）。
                CheckNear(_rt.CosmoFilterDrawerSlide, 1f, 0.001f, "卡背抽屉：点开后进度 = **1**（也是滑进来的）");
                CheckTrue(_rt.CosmoFilterDrawerInteractive, "…到位 ⇒ 参与命中");
                _rt.UiSetTab(0);
                Check(_rt.CosmoFiltersOpen, true, "切到 Cards 页 ⇒ 卡背抽屉的**逻辑态保留**");
                CheckTrue(!_rt.UiQuadActive("cosmoflt_bg"), "…但整栏**不露**（原版它挂在那张页底下）");
                CheckTrue(!_rt.CosmoFilterDrawerInteractive, "…也不参与命中");
                _rt.UiSetTab(2);
                CheckTrue(_rt.UiQuadActive("cosmoflt_bg"), "切回 Cosmetics ⇒ 又露出来了");
                CheckNear(_rt.CosmoFilterDrawerSlide, 1f, 0.001f, "…进度还是 **1**（页的开关不重放那 0.3 秒）");
                _rt.UiToggleFilters();                            // 关掉，别影响后面
                Check(_rt.CosmoFiltersOpen, false, "（抽屉关回去）");
                CheckNear(_rt.CosmoFilterDrawerSlide, 0f, 0.001f, "…也是**滑出去**的（A67）");
                CheckTrue(!_rt.CosmoFilterDrawerInteractive, "…关着就不参与命中");
            }
            _rt.UiSetTab(0);

            // 🔴 命中矩形：用**合成坐标**走鼠标那条路（`Ui*()` 只驱动状态，验不到「点在哪儿」）
            CheckTrue(_rt.UiClickPx(392.2f, 113.5f), "点 `Filters` 钮的**中心**（392,113.5）能命中");
            Check(_rt.FiltersOpen, true, "……而且筛选栏真的开了");
            _rt.UiToggleFilters();
            Check(_rt.FiltersOpen, false, "（关回去）");
            CheckTrue(!_rt.UiClickPx(900f, 700f), "点空白处不命中任何钮");
            Check(_rt.FiltersOpen, false, "……也不会误开面板");

            // 滚动（原版是滚动列表，不是翻页）—— 滚过一整行之后，第 0 格该换一张卡
            var before = _rt.UiPoolCardAt(0);
            CheckTrue(_rt.MaxPoolScrollPx > 0f, $"卡池滚得动（上限 {_rt.MaxPoolScrollPx:F0} px）");
            _rt.UiScrollPool(600f);
            CheckTrue(_rt.UiPoolCardAt(0) != before, $"卡池滚动后第 0 格换了卡（{before?.Name} → {_rt.UiPoolCardAt(0)?.Name}）");
            _rt.UiScrollPool(-9999f);
            Check(_rt.PoolScrollPx, 0f, "往回滚到顶夹在 0（不会滚成负的）");

            // 卡组列表滚动：先把卡组填满（演示卡组就有 30 张 + 督军 + 防御）
            CheckTrue(_rt.MaxDeckScrollPx > 0f, $"卡组列表滚得动（{_rt.MaxDeckScrollPx:F0} px = 条目数超过一屏）");
            var row0 = _rt.UiDeckRowAt(0);
            _rt.UiScrollDeck(56f);
            CheckTrue(_rt.UiDeckRowAt(0) != row0, "卡组列表滚动后第一行换了卡");
            _rt.UiScrollDeck(-9999f);
            Check(_rt.DeckScrollPx, 0f, "卡组列表滚回顶部夹在 0");

            // 改名（🔴 **2026-10-12（A363 + A365）改向**）
            //   · A363：原版改名**只标脏**（`DeckEditingPanel__ChangeName.c:6` 写名字、`:9` 标脏），
            //     落盘等 `Done`（`DeckEditingWindow__TrySaveDeck.c:80` → `:100 UploadDeck`）；
            //     ⇒ 原来这条断的「改完**就**落盘」是我们自己的偏离。
            //   · A365：空名字**照收**（同 `:6`，方法体里没有任何空值判断）；
            //     空名怎么补在**校验**那一拍 —— 合法就用**督军卡名**补上
            //     （`DeckUtility__ValidateDeck.c:70-76`）。
            string diskNameBefore = DeckLibrary.Load().Current.Name;
            _rt.UiCommitName("自检·改的名");
            Check(state.Deck.Name, "自检·改的名", "改名进了当前卡组");
            CheckTrue(_rt.DeckDirty, "★ A363：改名 ⇒ **标脏**");
            Check(DeckLibrary.Load().Current.Name, diskNameBefore,
                  "★ A363：……而且**盘上还是旧名字**（「" + diskNameBefore + "」）—— 改一下就写盘 = 这条红");
            _rt.EscPressed();                                        // = Done
            Check(DeckLibrary.Load().Current.Name, "自检·改的名", "★ A363：……按 Done **才**落盘");
            CheckTrue(!_rt.DeckDirty, "★ ……而且脏标记清掉了（= 那一下真的写成功）");

            // ---- A365：空名 ⇒ **不再挡**，合法时自动补成督军卡名 ----
            //  ① 先在**状态层**直判「到底收没收」：界面那条路会**立刻**被 `Validate()` 补名
            //     ⇒ 光看界面分不出「收下了空名」还是「挡掉了、然后被补名」（那两条都会绿）。
            CheckTrue(state.SetDeckName("   "),
                      "★ A365：`SetDeckName(\"   \")` ⇒ **返回 true（照原版收下）**"
                    + "（改回去那句 `if (IsNullOrWhiteSpace) return false;` ⇒ 这条红）");
            Check(state.Deck.Name, "", "★ ……而且名字**真的是空的**（不是「挡掉、保持原样」）");
            state.Deck.Name = "自检·改的名";                  // 还回去，让下面那条界面路「真的变了」

            //  ② 再走**界面**那条路（回车提交）：卡组合法 ⇒ 补成督军卡名
            var wl0 = state.Find(state.Deck.WarlordId);
            CheckTrue(wl0 != null, "（前提）这副牌有督军 —— 补名用的就是它");
            _rt.UiCommitName("   ");
            CheckTrue(_rt.DeckDirty, "★ A363：清空名字走界面的那一下 ⇒ **标脏**");
            Check(state.Deck.Name, CardText.Name(wl0.Name, wl0.NameZh),
                  "★ A365：合法卡组 + 空名 ⇒ 自动补成**督军卡名**（原版 `DeckUtility__ValidateDeck.c:73-75`："
                + "`deckName = GetLocalizedCardName(deckHero)`）");

            // ---- A365 的反向：**不合法 ⇒ 不补**（原版 `:69` 那道「张数 == deckSize」是补名的前置）----
            //   ⛔ 只验正向的话，一个「不管合不合法都拿督军名去补」的实现照样绿 —— 那会把玩家
            //      特意清空的名字又填回来，与原版不符。
            {
                string back0 = state.Deck.CardIds[0];
                state.Deck.CardIds.RemoveAt(0);                        // 30 → 29 ⇒ 不合法
                _rt.UiCommitName("   ");                               // 清空名字
                Check(_rt.State.Validate(), DeckError.TooFewCards,
                      $"（前提）摘掉一张 ⇒ 不合法（{_rt.State.DeckCount}/{_rt.State.MaxDeckCount}）");
                CheckTrue(string.IsNullOrEmpty(state.Deck.Name),
                          "★ A365：**不合法时不补** —— 名字就空着（谁把补名写成无条件 ⇒ 这条红）");
                // 收尾：卡组与名字都还回去（下面几节接着用这副牌）
                state.Deck.CardIds.Insert(0, back0);
                state.Deck.Name = "自检·改的名";
                Check(_rt.State.Validate(), DeckError.None, "（收尾）补回那张 ⇒ 又合法");
            }

            // ======== 🆕 2026-10-11（A223）：**ESC = 保存**（原版 `DeckEditingWindow__ESCPressed`）========
            //   判据 = `decomp_full/DeckEditingWindow__ESCPressed.c:5` —— 那一行就是 `TrySaveDeck(param_1, 0)`；
            //   ⛔ **不是关窗**（关窗走 `DeckEditingWindow__TryClose.c`，只由关闭钮走）。
            //   `DeckRuntime.EscPressed()` = 运行时 `Update()` → `HandleEscape()` 调的**同一个函数**
            //   （批处理没有键盘 ⇒ 这里直接调它，不走键）。
            //   三级顺序**逐级验**（每级的「另一种实现」都会让对应那条红 —— 见各条文案）：
            {
                // ① 普通态 ⇒ 保存。把卡组**改脏**：直接改 `State.Deck`（⛔ 不走 `UiCommitName` ——
                //   虽然 A363 起它**也只标脏、不再自己提交**了，但直接改更干净：这一节要验的
                //   就是「ESC 这一下把内存里那份写进盘」，⛔ 别让 `Ui*()` 那条路也掺进来）。
                state.Deck.Name = "A223·ESC 落盘";
                Check(DeckLibrary.Load().Current.Name, "自检·改的名", "（前提）改完**还没**提交 ⇒ 盘上仍是旧名字");
                _rt.EscPressed();
                Check(DeckLibrary.Load().Current.Name, "A223·ESC 落盘",
                      "★ A223：**ESC = 保存**（原版 `DeckEditingWindow__ESCPressed.c:5` → `TrySaveDeck`）"
                      + " —— 从**盘上重读**卡组库，名字真的变了（删掉 `Update()` 那句 `HandleEscape()` ⇒ 红）");
                // ② 导入弹窗开着 ⇒ **只关弹窗、不落盘**。判据：原版那扇窗是**独立的窗**
                //   （`Assembly-CSharp/ImportDeckPopup.cs:6` = `: GameWindow`），它的 prefab 实例实读
                //   **`closeOnESC = 1`** ⇒ ESC 归**压在最上面那扇窗**。
                state.Deck.Name = "A223·不该落盘";
                _rt.UiOpenImport();
                CheckTrue(_rt.ImportOpen, "（前提）导入弹窗开着");
                _rt.EscPressed();
                CheckTrue(!_rt.ImportOpen,
                          "★ A223：**ESC 先把最上面那扇窗关掉**（原版 `ImportDeckPopup` 是独立窗、`closeOnESC = 1`）");
                Check(DeckLibrary.Load().Current.Name, "A223·ESC 落盘",
                      "★ …而且这一下**没有落盘**（把「保存」排到弹窗之前 ⇒ 这条红）");
                // 🔴 **2026-10-13（A502）对照**：上面这一级 = **`closeOnESC = 1`** 的窗 ⇒ ESC **关得掉**；
                //   而 A502 新加的那一级（`_popup`，`closeOnESC = 0`）⇒ ESC **什么都不做**。
                //   两道门的差别**就在那扇窗自己的 `closeOnEsc`** 上 —— ⛔ 别把这两级合并成「有弹窗就不做」
                //   （合并 = 这条反例断言当场红）。A502 那一节有它的正例断言。
                _rt.UiCloseImport();
                // ③ 文本编辑中 ⇒ **不抢**（ESC 归 `HandleTyping` 那条既有路：取消编辑）
                state.Deck.Name = "A223·编辑中不该落盘";
                _rt.UiBeginNameEdit();
                CheckTrue(_rt.NameEditing, "（前提）改名中输入态");
                _rt.EscPressed();
                CheckTrue(_rt.NameEditing, "★ …**编辑中 ESC 不归保存**（输入框先吃：`EscPressed()` 第一句 return）");
                Check(DeckLibrary.Load().Current.Name, "A223·ESC 落盘", "★ …而且这一下**没有落盘**");
                _rt.UiCancelEdit();
                // 收尾：名字改回去（下面的用例接着用同一副牌），顺带**再验一次保存**
                state.Deck.Name = "自检·改的名";
                _rt.EscPressed();
                Check(DeckLibrary.Load().Current.Name, "自检·改的名", "（收尾）再按一次 ESC ⇒ 名字写回盘上");
            }

            // ======== 🆕 2026-10-11（A330）：**卡组不合法 ⇒ `Done` / `ESC` 【都不落盘】** ========
            //   判据 = `decomp_full/DeckEditingWindow__TrySaveDeck.c:80` —— `TrySaveDeck` 的**第一件事**
            //   就是 `cVar3 = DeckUtility__ValidateDeck(deck, out err, /*validateOwnership*/1, 0);`，
            //   `:81-98` 不合法 ⇒ 弹错误窗后 **return**（**不走** `:100` 的 `UploadDeck`）。
            //   而 `ESC` 调的就是这个函数（`__ESCPressed.c:5`）、`Done` 那颗钮的**点亮**用的是**同一个**
            //   `ValidateDeck`（`__UpdateDoneButton.c:24`）⇒ **「灯亮」与「放行」是同一条判据**。
            //   ⚠️ 本段**只动内存**（直接改 `State.Deck`，⛔ 不走 `CommitDeck()` / `Ui*()`）——
            //     那些路**自己会落盘**，拿它们当夹具就分不出「是 Done 存的」还是「那条路存的」＝同义反复。
            //     （`DeckLibrary.Load()` 是**从盘上重读**，与内存里那份是两个对象 ⇒ 读它才分得出。）
            {
                var live = _rt.State.Deck;   // 🔴 A397 起 = 编辑器那份**副本**（不再是 `Library.Current` 那个对象）
                var keepIds = new List<string>(live.CardIds);                    // 收尾复原（后面几节还用这副牌）
                const string keepName = "自检·改的名";

                // ---- ① 前提：此刻这副牌是**合法的**（灯亮 + `Validate()` 过）----
                //    ⛔ 这条不给的话，「不合法 ⇒ 不落盘」在一个「本来就存不进去」的实现下也照样绿。
                Check(_rt.State.Validate(), DeckError.None,
                      $"（前提）演示卡组此刻合法（{_rt.State.DeckCount}/{_rt.State.MaxDeckCount}）");
                CheckTrue(_rt.UiQuadActive("foot_hl"), "（前提）`Done Highlight` 亮着（= 同一判据的另一半）");

                // ---- ② 弄成不合法：摘掉一张（30 → 29）----
                live.CardIds.RemoveAt(0);
                _rt.UiScrollPool(0f);            // 逼一次 `RefreshHeader()`（批处理没有帧循环，同 `A76②` 那条的写法）
                Check(_rt.State.Validate(), DeckError.TooFewCards,
                      $"（前提）摘掉一张 ⇒ 不合法（{_rt.State.DeckCount}/{_rt.State.MaxDeckCount}，`TooFewCards`）");
                Check(_rt.UiLabelText("foot_verdict"), DeckRules.Describe(DeckError.TooFewCards),
                      "……页脚也如实说出了原因（`DeckRules.Describe`，与闸门同一份文案）");
                CheckTrue(!_rt.UiQuadActive("foot_hl"),
                          "……`Done Highlight` **灭掉**（原版 `UpdateDoneButton` 用的是同一个 `ValidateDeck`）");
                Check(DeckLibrary.Load().Current.CardIds.Count, keepIds.Count,
                      "（前提）盘上仍是满编 —— 摘掉的只是**内存里那一份**");

                // ---- ③ `Done` ⇒ **不落盘** + **出声** ----
                live.Name = "A330·Done 不该落盘";
                string noticeBefore = _rt.UiLabelText("notice");
                _rt.UiBtnRect("foot_done", out float dnX, out float dnY, out float dnW, out float dnH);
                float dcx = dnX + dnW * 0.5f, dcy = dnY + dnH * 0.5f;
                CheckTrue(_rt.UiClickPx(dcx, dcy) && _rt.UiTopKeyAt(dcx, dcy) == "foot_done",
                          "点页脚 `Done` 的中心（走鼠标那条路，命中 `foot_done`）");
                Check(DeckLibrary.Load().Current.Name, keepName,
                      "★ A330：**卡组不合法 ⇒ 点 Done 不落盘**（原版 `__TrySaveDeck.c:81` 弹窗后 return，"
                      + "不 UploadDeck；把 `SaveAndSay()` 里那道闸删掉/改成 `if (false)` ⇒ 这条红）");
                Check(DeckLibrary.Load().Current.CardIds.Count, keepIds.Count, "★ ……而且盘上张数也没变");
                CheckTrue(_rt.UiLabelText("notice") != noticeBefore
                          && (_rt.UiLabelText("notice") ?? "").Contains(DeckRules.Describe(DeckError.TooFewCards)),
                          "★ ……而且**出声了**（页脚那行提示：「" + _rt.UiLabelText("notice")
                          + "」—— 静默挡下 = 这条红）");

                // ---- ④ `ESC` = **同一个函数** ⇒ 也不落盘 ----
                //    谁是「只给 Done 那一支加闸、ESC 那支绕过」⇒ 这条红（两处写同一条规则的老毛病）。
                live.Name = "A330·ESC 不该落盘";
                _rt.EscPressed();
                Check(DeckLibrary.Load().Current.Name, keepName,
                      "★ A330：**不合法时 ESC 也不落盘**（原版 `__ESCPressed.c:5` 调的就是 `TrySaveDeck`）");

                // 🔴 **2026-10-13（A502）补一步「收窗」** —— 前提变了：③ 那次 Done / ④ 这次 ESC 走的都是
                //   「不合法 ⇒ `ShowPopUp` + `return`」那一支 ⇒ **那扇模态窗此刻还开着**。
                //   A502 之后 ESC 照原版**归最上面那扇窗**（它 `closeOnESC = 0` ⇒ 什么都不做）
                //   ⇒ 不收掉它，下面 ⑤ 与收尾那两次 ESC **一个字节都写不进去**（「闸不是恒关」当场红），
                //   而且这扇窗会留在场上污染后面几节（A364 起手那条「没有模态窗」的前提也靠它）。
                //   ⇒ 走**真钮**那条路收掉（右钮 = 原版那颗只调 `HidePopUp()` 的 `Cancel`），同 A415 收尾的写法。
                var popA330 = _rt.ModalPopup;
                CheckTrue(popA330 != null, "（前提）④ 之后那扇模态窗开着（A364 起手那条前提就靠这一句收干净）");
                if (popA330 != null) ClickPopupButton(popA330, "ButtonRight");
                CheckTrue(!_rt.ModalPopupOpen, "（收尾）点右钮 ⇒ 窗收掉");

                // ---- ⑤ 复原 + 反证「闸不是恒关」----
                live.CardIds.Insert(0, keepIds[0]);
                _rt.UiScrollPool(0f);
                Check(_rt.State.Validate(), DeckError.None, "（收尾）把那 1 张补回来 ⇒ 又合法");
                CheckTrue(_rt.UiQuadActive("foot_hl"), "（收尾）灯重新亮起来");
                live.Name = "A330·闸不是恒关";
                _rt.EscPressed();
                Check(DeckLibrary.Load().Current.Name, "A330·闸不是恒关",
                      "★ （收尾）**合法时照样存得下去** —— 谁把闸写成恒真（`if (true)` 挡死）⇒ 这条红");
                live.Name = keepName;                                     // 名字复位（后面几节还用同一副牌）
                _rt.EscPressed();
                Check(DeckLibrary.Load().Current.Name, keepName, "（收尾）名字复位并落盘");
                Check(DeckLibrary.Load().Current.CardIds.Count, keepIds.Count, "（收尾）盘上仍是满编");
            }

            // ======== 🆕 2026-10-12（A364）：卡组不合法 ⇒ **模态消息窗**（原版 `PopUpGameWindow`）========
            //   判据（逐句 → `d:/2/tools/decomp_full/DeckEditingWindow__TrySaveDeck.c`）：
            //     `:81-98` 不合法 ⇒ 建两颗 `GameWindowButton`（左 `MainMenu/General/Discard` / 右
            //     `MainMenu/General/Cancel`）→ `:94 WindowsManager.ShowPopUp(ToRawLocalizationString(err), 1, 0, 左, 右)`
            //     → `:95 return`；`:103` 合法那一支反而调 `WindowsManager.HidePopUp`。
            //   窗 = `PopUpGameWindow`，两扇 prefab（`MessagePopupWindow{,_2Buttons}`）**本地有**，
            //   逐节点参数见 `Shell/PopUpGameWindow.cs` 文件头（`menu_dump.py` 实读 + 原始 JSON 复核）。
            //   ⚠️ 与 `Shell/PromptPopup.cs`（原版 `GenericPromptWindow`）**是两扇窗**，⛔ 别混。
            {
                Section("A364：不合法 ⇒ 模态消息窗（原版 `PopUpGameWindow`）");

                // ---- ① 文案：原版号 → 术语键 → **显示的是键**（纯函数先钉住，不依赖窗口）----
                // 原版枚举（`global-metadata.dat` 实读）：`None=0 · CardsNotOwned=1 · MissingHero=2 ·
                // InvalidDeckBannedCards=3 · InvalidDeck=4 · IncompleteDeck=5`
                Check(DeckRuntime.MenuDeckErrorNumber(DeckError.NoWarlord), 2,
                      "★ A364：`NoWarlord` ⇒ 原版号 **2**(`MissingHero`)");
                Check(DeckRuntime.MenuDeckErrorNumber(DeckError.TooFewCards), 5,
                      "★ ……`TooFewCards` ⇒ **5**(`IncompleteDeck`)");
                Check(DeckRuntime.MenuDeckErrorNumber(DeckError.TooManyCards), 5,
                      "★ ……`TooManyCards` 也是 **5**（原版那条判据是「**恰好等于** deckSize」，多与少同号）");
                Check(DeckRuntime.MenuDeckErrorNumber(DeckError.WrongFaction), 4,
                      "★ ……`WrongFaction` ⇒ **4**(`InvalidDeck` —— 原版的 catch-all)");
                Check(DeckRuntime.MenuDeckErrorNumber(DeckError.CopyLimitExceeded), 4, "★ ……`CopyLimitExceeded` ⇒ **4**");
                Check(DeckRuntime.MenuDeckErrorNumber(DeckError.WarlordNotHero), 4, "★ ……`WarlordNotHero` ⇒ **4**");
                CheckTrue(PopUpGameWindow.Terms.Count == 0,
                          "（前提）I2 词条表**本地是空的**（表在远端 CCD）—— 下面「显示的是键」才有意义");
                Check(DeckRuntime.MenuDeckErrorKey(DeckError.TooFewCards), "MenuDeck/Error/InvalidDeck",
                      "★ ……表空 ⇒ 原版 `ToRawLocalizationString` 落到**兜底键**（**兜的也是键、不是明文**）");
                Check(DeckRuntime.MenuDeckErrorKey(DeckError.None), "", "★ ……`None` ⇒ 空串（原版 `:16` 那一支）");
                Check(PopUpGameWindow.Term("MenuDeck/Error/5"), "MenuDeck/Error/5",
                      "★ ……`Term` 查不到就**返回键本身**（⛔ 不自己编文案）");
                // 🔴 **把「将来填表」那条路也钉住**：不然「表空 ⇒ 印键」会被一个**根本没用表**的实现蒙对。
                PopUpGameWindow.Terms["MenuDeck/Error/5"] = "（自检塞的假词条）";
                Check(DeckRuntime.MenuDeckErrorKey(DeckError.TooFewCards), "MenuDeck/Error/5",
                      "★ ……表里真有 `MenuDeck/Error/5` ⇒ 键就用它（拿到真表只往 `Terms` 里填、⛔ 不改调用点）");
                Check(PopUpGameWindow.Term("MenuDeck/Error/5"), "（自检塞的假词条）", "★ ……而且 `Term` 取到了那条文字");
                PopUpGameWindow.Terms.Remove("MenuDeck/Error/5");
                Check(PopUpGameWindow.Terms.Count, 0, "（收尾）词条表还回空的（假词条不许漏进后面的断言）");

                // ---- ② 开窗：不合法 ⇒ `Done`（= `ESC`，同一个 `SaveAndSay()`）----
                //  🔴 **A397**：`live` = 编辑器那份**副本**（`DeckEditorState.LoadDeck` 装的是副本）——
                //     本节全部只改它（⛔ 不走 `CommitDeck()`），库那份**内存里也没被动过**；
                //     「盘上没动」到底靠的是「没人调 `Save()`」，不是「靠同一个对象」。
                var live = _rt.State.Deck;
                var keepIds = new List<string>(live.CardIds);
                string keepName = live.Name;
                Check(_rt.State.Validate(), DeckError.None, "（前提）这一节起手这副牌合法");
                CheckTrue(!_rt.ModalPopupOpen && _rt.ModalPopup == null, "（前提）起手**没有**模态消息窗");
                Check(_rt.LeaveCount, 0, "（前提）起手没离场过");
                bool fltWas = _rt.FiltersOpen;                       // 下面验「模态挡住指针」要用的基线

                live.CardIds.RemoveAt(0);                            // 30 → 29 ⇒ `TooFewCards`
                live.Name = "A364·不该落盘";
                _rt.UiScrollPool(0f);                                // 逼一次 `RefreshHeader`（批处理没有帧循环）
                Check(_rt.State.Validate(), DeckError.TooFewCards, "（前提）摘一张 ⇒ 不合法");
                _rt.EscPressed();                                    // = Done = `TrySaveDeck`
                var pop = _rt.ModalPopup;
                CheckTrue(pop != null,
                          "★ A364：**不合法 ⇒ 弹出模态消息窗**（原版 `__TrySaveDeck.c:94`；把 `SaveAndSay` 里那句"
                        + " `ShowInvalidDeckPopUp(err)` 删掉 ⇒ 这条红）");
                if (pop != null)
                {
                    int popId1 = pop.GetInstanceID();
                    Check(pop.name, "MessagePopupWindow2Buttons",
                          "★ 建的是**2 按钮版**（原版按「按钮数 > 1」挑 prefab；给两颗却挑 1 按钮版 ⇒ 红）");
                    Check(pop.type, WindowType.Popup, "★ `type = 1(Popup)`");
                    Check(pop.placement, WindowsPlacement.Popup, "★ `windowsPlacement = 15(Popup)`");
                    CheckTrue(!pop.closeOnEsc, "★ `closeOnESC = 0` —— **ESC 关不掉这扇窗**（原版 MB 实读）");
                    CheckNear(pop.extraScaleSmallScreen, 1f, 1e-6f, "★ `extraScaleSmallScreen = 1.0`（原版 MB 实读）");
                    Check(pop.transform.parent != null ? pop.transform.parent.name : "(空)",
                          "3 - PopUp Holder", "★ 挂在 **Popup(15)** 那档锚点下（原版同一个 Holder）");
                    Check(pop.MessageKey, "MenuDeck/Error/InvalidDeck", "★ 正文键 = 兜底键（表空）");
                    Check(pop.MessageShown, "MenuDeck/Error/InvalidDeck",
                          "★ ……而且**画出来的就是那个键**（⛔ 不是我们编的一句人话）");
                    Check(pop.PrimaryShown, "MainMenu/General/Discard",
                          "★ **左**钮 = `MainMenu/General/Discard`（原版 `LiveButtons[0]` = `ButtonLeft`）");
                    Check(pop.SecondaryShown, "MainMenu/General/Cancel", "★ **右**钮 = `MainMenu/General/Cancel`");

                    // ---- ③ 版面：量**渲染矩形**，期望值全是原版字面量（`menu_dump` 实读）----
                    //   ⛔ 不比我们自己的常量（那是自证）；下面是原版 prefab 的绝对矩形。
                    const float WinL = 535f, WinT = 245f, WinR2 = 1385f, WinB = 675f;          // `Window` 850×430
                    const float ShadeW = 4574.6f, ShadeH = 2572.36f;                            // `Menu Dark Background`
                    const float BtnW = 350f, BtnH = 76f;                                        // 两颗钮各 350×76
                    const float BtnLoL = 591.15f, BtnLoR = 941.15f, BtnHiL = 978.85f, BtnHiR = 1328.85f;
                    const float BtnT = 567f, BtnB = 643f;
                    var panelN = FindDeep(pop.transform, "Generic Popup Background");
                    CheckTrue(UnionQuadsPx(panelN, out float qx1, out float qy1, out float qx2, out float qy2),
                              "★ 面板九宫格**建出来了**（`MenuDraw.Nine` 那九块；建不出 ⇒ 这里就红）");
                    if (panelN != null)
                    {
                        CheckNear(qx1, WinL, 0.6f, $"★ 面板左沿 = 原版 `Window` 的 {WinL:F0}（实得 {qx1:F1}）");
                        CheckNear(qy1, WinT, 0.6f, $"★ 面板上沿 = {WinT:F0}（实得 {qy1:F1}）");
                        CheckNear(qx2, WinR2, 0.6f, $"★ 面板右沿 = {WinR2:F0}（实得 {qx2:F1}）");
                        CheckNear(qy2, WinB, 0.6f, $"★ 面板下沿 = {WinB:F0}（实得 {qy2:F1}）");
                    }
                    var shadeN = FindDeep(pop.transform, "Menu Dark Background");
                    if (shadeN != null && shadeN.GetComponent<ImageQuad>() != null)
                    {
                        var sq = shadeN.GetComponent<ImageQuad>();
                        CheckNear(sq.WorldW * PxPerUnit, ShadeW, 0.6f, $"★ 压暗层宽 = {ShadeW}（实得 {sq.WorldW * PxPerUnit:F1}）");
                        CheckNear(sq.WorldH * PxPerUnit, ShadeH, 0.6f, $"★ 压暗层高 = {ShadeH}（实得 {sq.WorldH * PxPerUnit:F1}）");
                    }
                    else Check(true, false, "压暗层 `Menu Dark Background` 没建出来");
                    // 左钮：中心 + 高按原版；**宽不许撑满 350**（原版那颗 `m_PreserveAspect = 1`
                    // ⇒ 489×107 的图等比放进 350×76 ⇒ 宽 = 76×(489/107) = 347.33，左右各让 1.33）
                    var bl = FindDeep(pop.transform, "ButtonLeft");
                    CheckTrue(UnionQuadsPx(bl, out float l1, out float lt, out float l2, out float lb2),
                              "★ 左钮的底图建出来了");
                    if (bl != null)
                    {
                        CheckNear((l1 + l2) * 0.5f, (BtnLoL + BtnLoR) * 0.5f, 0.6f,
                                  $"★ 左钮中心 x = {(BtnLoL + BtnLoR) * 0.5f:F2}（原版 `ButtonLeft` 中点）");
                        CheckNear(lt, BtnT, 0.6f, $"★ ……上沿 = {BtnT:F0} · 下沿 = {BtnB:F0}（= `Buttons` 行里居中）");
                        CheckNear(lb2, BtnB, 0.6f, "★ ……下沿");
                        // 宽：原版那一版是 `m_PreserveAspect = 1` ⇒ **等比放进 350×76**，宽 = 76 × 图的长宽比
                        // （489×107 ⇒ 347.33，左右各让 1.33）⛔ 不是撑满 350。期望值取**图自己**的比例
                        // （图 = 原版 `40K_button`；这样也不怕导入器改了尺寸）。
                        var bq = FirstQuad(bl);
                        float sprAspect = (bq != null && bq.Texture != null && bq.Texture.height > 0)
                                          ? (float)bq.Texture.width / bq.Texture.height : 489f / 107f;
                        CheckTrue(l2 - l1 <= BtnW + 0.1f,
                                  $"★ ……而**宽 ≤ {BtnW:F0}**（实得 {l2 - l1:F2}）—— 那一版是 `m_PreserveAspect = 1`，"
                                + "画出来比框**窄**；改成 `keepAspect: false` 撑满 350 ⇒ 这条红");
                        CheckNear(l2 - l1, BtnH * sprAspect, 0.6f,
                                  $"★ ……宽 = 76 × 图的长宽比 = {BtnH * sprAspect:F2}（原版 `m_PreserveAspect = 1`）");
                    }
                    var br = FindDeep(pop.transform, "ButtonRight");
                    CheckTrue(UnionQuadsPx(br, out float r1, out float rt2, out float r2, out float rb2),
                              "★ 右钮的底图建出来了");
                    if (br != null)
                    {
                        CheckNear((r1 + r2) * 0.5f, (BtnHiL + BtnHiR) * 0.5f, 0.6f,
                                  $"★ 右钮中心 x = {(BtnHiL + BtnHiR) * 0.5f:F2}（原版 `ButtonRight` 中点）");
                        CheckNear(rt2, BtnT, 0.6f, "★ ……上沿与左钮同高（原版 HLG `align=4` 两格同高）");
                        CheckNear(rb2, BtnB, 0.6f, "★ ……下沿");
                    }
                    // 层带：压暗 < 面板 < 填充 < 按钮 < 文字（**这个顺序**才是判据；⛔ 不比具体号）。
                    // ⚠️ `MessageText` 是 **`Label`（TMP）不是 quad** ⇒ 它那一档读 `Label.RenderQueue`。
                    var qShade = FirstQuad(shadeN);
                    var qPanel = FirstQuad(panelN);
                    var qFill = FirstQuad(FindDeep(pop.transform, "Background fill"));
                    var qBtn = FirstQuad(bl);
                    var msgNode = FindDeep(pop.transform, "MessageText");
                    var msgLb2 = msgNode != null ? msgNode.GetComponent<Label>() : null;
                    CheckTrue(qShade != null && qPanel != null && qFill != null && qBtn != null && msgLb2 != null,
                              "（前提）五层都量得到队列（量不到 ⇒ 下面那条是空转）");
                    if (qShade != null && qPanel != null && qFill != null && qBtn != null && msgLb2 != null)
                    {
                        CheckTrue(qShade.RenderQueue < qPanel.RenderQueue && qPanel.RenderQueue < qFill.RenderQueue
                                  && qFill.RenderQueue < qBtn.RenderQueue && qBtn.RenderQueue < msgLb2.RenderQueue,
                                  $"★ 队列严格递增 = 原版兄弟序（压暗 {qShade.RenderQueue} < 面板 {qPanel.RenderQueue}"
                                + $" < 填充 {qFill.RenderQueue} < 按钮 {qBtn.RenderQueue} < 文字 {msgLb2.RenderQueue}）");
                        CheckTrue(qShade.RenderQueue >= 3103,
                                  $"★ ……而且整条压在**卡组编辑窗自己最高那档之上**（{qShade.RenderQueue} ≥ 3103；"
                                + "本窗模态档 `QModalText` = 3102）");
                    }
                    // 压暗层那颗**吸收**命中区（原版 `Menu Dark Background` 的 `m_RaycastTarget = 1`）
                    CheckTrue(pop.ShadeHitNode != null && MenuDraw.WasAbsorb(pop.ShadeHitNode),
                              "★ 压暗层那颗**吃射线**的命中区建出来了（`MenuDraw.Absorb`）——"
                            + "**模态**靠它 + `DeckRuntime` 那道闸（见下面 ④）");
                    if (pop.ShadeHitNode != null)
                    {
                        string why;
                        CheckTrue(!MenuDraw.AbsorbTierWarned(pop.ShadeHitNode, out why),
                                  "★ ……而且它的档**合法**（吸收层的档必须严格高于压暗层、低于内容层）" + why);
                        var ahb = pop.ShadeHitNode.GetComponent<WindowButton>();
                        CheckTrue(ahb != null && ahb.absorbOnly,
                                  "★ ……它是一颗 `absorbOnly` 的 `WindowButton`（点了**什么都不做** —— 原版那颗"
                                + " `BackgroundCloseButton.window` 是**空引用** ⇒ 这扇窗点背景不关窗）");
                        if (ahb != null) ahb.ClickForTest();
                        CheckTrue(_rt.ModalPopupOpen, "★ ……点压暗层**不会**把窗关掉（`absorbOnly` 那条早退）");
                    }
                    CheckHoverSwap(pop.transform, "模态消息窗（原版两颗钮都是 `trans=2 SpriteSwap`）");

                    // ---- ④ 模态：本窗**不吃点击**（原版靠压暗层吞射线；我们靠 `DeckRuntime` 那道闸）----
                    bool ate = _rt.UiClickPx(392.2f, 113.5f);         // 页头 `Filters` 那颗钮的正中心
                    CheckTrue(!ate, "★ A364：弹窗开着 ⇒ 点下面的 `Filters` **没人吃这一下**（模态）");
                    Check(_rt.FiltersOpen, fltWas,
                          "★ ……而且抽屉**没有被翻动**（删掉 `HandlePointer` / `UiClickPx` 里那句 `if (ModalPopupOpen) return;`"
                        + " ⇒ 这一下会被 `hdr_filters` 吃到 ⇒ 红）");
                    // 不合法时 ESC **也走不出这条路**。
                    // 🔴 **2026-10-13（A502）改了前提**：这一段原来记的是**一处如实标注的偏离** ——
                    //    那时 `DeckRuntime.EscPressed()` **不认弹窗**，ESC 会一路走到 `SaveAndSay()`
                    //    （= 把同一扇窗又配了一遍，同一个实例、看不出来）。A502 已照原版把那一级补上：
                    //    原版 ESC **只打给最上面那扇窗、没有第二跳**（`WindowsManager__Update.c` 取 `+0x58`
                    //    = `currentWindow` → `IsOpen()` → 它自己的 `ESCPressed()` → return），而弹窗一开
                    //    `currentWindow` 就是它（`OpenWindowCO.c` 的 `set_CurrentWindow` 写在 if/else 之外）
                    //    ⇒ 那扇窗 `closeOnESC = 0`（prefab 实读）⇒ **什么都不做**。
                    //    ⚠️ 旧注释里那句「照原版做就造出一个只能点钮的死局」**不成立**：原版本来就是点钮才关得掉
                    //    —— 下面 ⑤ 点右钮 / ⑥ 点左钮那两条就是这个「唯一出口」的验证。
                    // 🔴 **诚实标注**：下面这两条**分不出**「ESC 什么都不做」与「ESC 又走到 `SaveAndSay()`
                    //    把那扇窗重配了一遍」—— 因为不合法那一支最终**也是**这扇窗、也**不落盘**（同实例、同一个键）。
                    //    带电的那条在 A502 那一节（合法 + 脏 + 窗开着：一旦落到保存就**写盘**）。
                    _rt.EscPressed();
                    CheckTrue(_rt.ModalPopupOpen, "★ ……不合法时再按 ESC：窗**还开着**（不会偷偷把自己收掉）");
                    Check(DeckLibrary.Load().Current.Name, keepName, "★ ……而且盘上仍然**没有**这次的名字（没落盘）");

                    // ---- ⑤ 复用：**还开着**时再弹 ⇒ 原版是「重配 + 重开同一实例」（`*(this+0x60)`）----
                    // 🔴 **2026-10-13（A502）如实记**：这一条原来是靠 ④ 那次 ESC（当时它**不认弹窗**、
                    //   又走了一遍保存 ⇒ 把窗重弹一次）**带电**的；A502 之后 ④ 的 ESC 什么都不做
                    //   ⇒ 这里成了**平凡真**。带电的那条挪到 A502 那一节（那里显式再 `TryClose()` 一次，
                    //   断言「同一个实例」）；本节这条留着不删（④ 一旦被改回旧写法，它仍然是对的）。
                    Check(pop.GetInstanceID(), popId1,
                          "★ ……而且**还是同一扇实例**（原版 `ShowPopUp` 打的是字段 `popUpWindow`；"
                        + "每次都新建 ⇒ 这条红）");
                    // 点**右钮**（`Cancel`）= 原版那颗回调体里只有 `HidePopUp()`
                    // ⚠️ 走**真钮**那条路（`WindowButton.ClickForTest` → `Click` → `onClick`），
                    //    ⛔ 不另开一个「关窗」自检口 —— 那样就验不到两颗钮的接线了。
                    ClickPopupButton(pop, "ButtonRight");
                    CheckTrue(!_rt.ModalPopupOpen, "★ 点**右钮**（`Cancel`）⇒ 窗关掉");
                    Check(_rt.LeaveCount, 0, "★ ……而且**没离场**（原版右钮那颗回调体里只有 `HidePopUp()`）");
                    Check(DeckLibrary.Load().Current.Name, keepName, "★ ……盘上仍然没变");
                    CheckTrue(_rt.UiClickPx(392.2f, 113.5f), "★ ……窗关了之后本窗**又吃得进点击了**（模态解除）");
                    _rt.UiClickPx(392.2f, 113.5f);                   // 翻回去（别把抽屉留在开着）
                    Check(_rt.FiltersOpen, fltWas, "（收尾）抽屉状态还回基线");

                    // ---- ⑥ 关过之后重开 ⇒ 原版关窗**销毁**实例（我们只 `SetActive(false)`，靠 `StillOpen` 判）----
                    _rt.EscPressed();                                // 仍不合法 ⇒ 再弹
                    var pop2 = _rt.ModalPopup;
                    CheckTrue(pop2 != null, "★ 再弹一次 ⇒ 又开出来了");
                    if (pop2 != null)
                        CheckTrue(pop2.GetInstanceID() != popId1,
                                  "★ ……而且是**新的一扇**（原版 `CloseWindowCO` 关窗即销毁 + 清字段 ⇒ 关过就不复用；"
                                + "同 `WindowsManager.OpenByRef` 那条「关过就不再复用」口径）");
                    if (pop2 != null) ClickPopupButton(pop2, "ButtonLeft");   // = 点左钮（`Discard`）
                    Check(_rt.LeaveCount, 1,
                          "★ ……点**左钮**（`Discard`）⇒ **离场那一步真的走了**（原版那颗回调 = `HidePopUp()` + "
                        + "`GameWindow.Close()`(虚槽 0x1b8)；改成一个什么都不做的钮 ⇒ 这条红）");
                    CheckTrue(!_rt.ModalPopupOpen, "★ ……窗也关掉了");
                    Check(DeckLibrary.Load().Current.Name, keepName, "★ ……盘上仍然没变（Discard 不是「存一半」）");
                    // 🔴 **2026-10-14（A565）新增**：这颗左钮（`MainMenu/General/Discard`）在原版是
                    //   `.<TrySaveDeck>b__42_1` = `HidePopUp()` + 虚槽 `0x1b8`（`GameWindow.Close()`）
                    //   ⇒ **关窗把那份 `EditingDeck` 副本一起销毁**；我们批处理/不切场景时编辑器还活着
                    //   ⇒ 必须**显式回滚内存**（不回滚 ⇒ 按一次 Done 会把刚被丢弃的内容写回库）。
                    //   ⚠️ 回滚**换对象**（`State.LoadDeck()` 装的是新副本）⇒ 别名 `live` 要**重新捕获**，
                    //      否则下面 ⑦ 与本节收尾全在改一个**已经悬空**的对象（那正是这条账「必须连夹具一起改」的原因）。
                    live = _rt.State.Deck;
                    Check(DeckLibrary.ExportString(live), DeckLibrary.ExportString(_rt.Library.Current),
                          "★ A565：点 Discard ⇒ 编辑器那份**回滚成库里那份**（原版关窗销毁副本；"
                        + "删掉 `RollBackEditingCopy()` 那一句 ⇒ 这里还是被丢弃那份（" + (keepIds.Count - 1) + " 张）⇒ 这条红）");
                    Check(_rt.State.DeckCount, keepIds.Count,
                          "★ A565：……而且张数回到满编（被丢弃那份是 " + (keepIds.Count - 1) + " 张）");
                    Check(live.Name, keepName,
                          "★ A565：……名字也回到库里那个（被丢弃那份叫「" + keepName + "·不该落盘」）");
                    Check(_rt.State.Validate(), DeckError.None,
                          "★ A565：……而且这份**又合法**了（被丢弃那份是 `TooFewCards`；"
                        + "「Discard 之后仍不合法」那种状态从此不再存在）");

                    // ---- ⑦ 🔴 **2026-10-13（A502）改向**：原来的期待是「合法保存之后那扇窗要收掉」
                    //   （原版 `__TrySaveDeck.c:103 HidePopUp`）—— 那时 ESC **不认弹窗**，一路打到 `SaveAndSay()`
                    //   才碰得到那一句。照原版补上「弹窗开着 ⇒ ESC 什么都不做」之后，ESC **再也到不了**
                    //   `SaveAndSay()` 了 ⇒ 这一节改成**两条**：① 卡组已合法、窗还开着时按 ESC ⇒ **仍然什么都不做**
                    //   （带电的一条：一旦退回旧写法，`SaveAndSay()` 成功支就会 `HideDeckPopUp()` ⇒ 窗被收掉 ⇒ 红）；
                    //   ② 把窗关掉（点钮 = 原版那扇窗的唯一出口）之后再按 ESC ⇒ 才落盘。
                    //   ⚠️ **如实记**：`SaveAndSay()` 成功支那句 `HideDeckPopUp()` 从此刻起在**两边**都成了
                    //   防御性代码（原版那一刻同样轮不到 `DeckEditingWindow.ESCPressed`，我们的 Done 钮又被
                    //   `ModalPopupOpen` 那道闸挡着）⇒ **它今天没有生产可达路径、下面也不再钉它**（本件如实记账）。
                    //   ⚠️ **2026-10-14（A565）**：⑥ 那一按 Discard 之后**内存已经回滚**（卡组又合法、不脏）
                    //   ⇒ 想重演「不合法 ⇒ 弹第 3 扇窗」，得**先原地再造一次脏**（⛔ 不能换对象，否则 `live` 悬空）。
                    live.CardIds.RemoveAt(0);
                    _rt.UiScrollPool(0f);                            // 逼一次 `RefreshHeader`（批处理没有帧循环）
                    Check(_rt.State.Validate(), DeckError.TooFewCards, "（前提）再摘一张 ⇒ 又不合法");
                    _rt.EscPressed();                                // 仍不合法 ⇒ 窗又开（第 3 扇）
                    CheckTrue(_rt.ModalPopupOpen, "（前提）此刻窗开着（不合法）");
                    var pop3 = _rt.ModalPopup;
                    live.CardIds.Insert(0, keepIds[0]);              // 补回那一张 ⇒ 又合法
                    _rt.UiScrollPool(0f);
                    Check(_rt.State.Validate(), DeckError.None, "（前提）补回一张 ⇒ 合法");
                    live.Name = keepName + "·不该落盘";              // 🔴 **判别式**：与盘上那个名字**不同**
                    _rt.EscPressed();
                    CheckTrue(_rt.ModalPopupOpen,
                              "★ A502：卡组**已经合法**、窗还开着 ⇒ ESC **仍然什么都不做**"
                            + "（删掉 `DeckRuntime.EscPressed()` 里 ③ 那一级 ⇒ ESC 落回 `SaveAndSay()`"
                            + " ⇒ 合法 ⇒ 提交 + `HideDeckPopUp()` ⇒ 窗被收掉 ⇒ 这条红）");
                    CheckTrue(DeckLibrary.Load().Current.Name != keepName + "·不该落盘",
                              "★ ……而且这一下**没有落盘**（落到保存那一路，盘上就会出现「…·不该落盘」这个名字）");
                    if (pop3 != null)
                    {
                        ClickPopupButton(pop3, "ButtonRight");       // 右钮 = **只** `HidePopUp()`（窗的唯一出口）
                        CheckTrue(!_rt.ModalPopupOpen, "（收尾）点右钮把窗关掉");
                    }
                    live.Name = keepName;                            // 名字还回原样
                    _rt.EscPressed();                                // 此刻**没有**弹窗 ⇒ 回到「ESC = 保存」
                    Check(DeckLibrary.Load().Current.Name, keepName, "★ ……这一下才真的落盘（删掉 ③ 之后这一条仍绿，"
                          + "两支合起来才说明分岔的是**弹窗状态**、不是「ESC 坏了」）");
                    Check(DeckLibrary.Load().Current.CardIds.Count, keepIds.Count, "★ ……盘上仍是满编");
                }
                else
                {
                    Check(true, false, "（跳过）模态窗没建出来 ⇒ 上面那一整组版面/行为断言这一轮没跑到");
                }

                // ---- ⑧ 1 按钮版（`MessagePopupWindow`）：今天**没有生产消费者**，但它是原版另一半，
                //        照铁律 11 一起建/一起验（`ShowPopUp` 在「只给一颗」时走的就是它）----
                {
                    var wm = WindowsManager.EnsureHost();
                    var one = wm.ShowMessagePopUp("MenuDeck/Error/InvalidDeck", "MainMenu/General/Cancel", null);
                    Check(one.name, "MessagePopupWindow",
                          "★ 只给一颗钮 ⇒ 建的是**1 按钮版** `MessagePopupWindow`（原版 `popUpWindowOneButton`）");
                    Check(one.PrimaryShown, "MainMenu/General/Cancel", "★ 那一颗的字 = 给的那个键");
                    CheckTrue(one.SecondaryShown == null || one.SecondaryShown == "",
                              "★ ……而且**没有**第二颗（1 按钮版只有 `Generic UI Button` 一个）");
                    var b1 = FindDeep(one.transform, "Generic UI Button");
                    CheckTrue(UnionQuadsPx(b1, out float oneL, out float oneT, out float oneR2, out float oneB),
                              "★ 那一颗钮的底图建出来了");
                    if (b1 != null)
                    {
                        // 原版 1 按钮版：`Generic UI Button` = 760,562.5→1160,637.5（400×75）——
                        // 那一版的 `m_PreserveAspect = **0**` ⇒ **拉伸撑满**（与 2 按钮版相反，逐扇实读）
                        CheckNear(oneL, 760f, 0.6f, "★ 1 按钮版那颗钮左沿 = 原版的 760");
                        CheckNear(oneR2, 1160f, 0.6f, "★ ……右沿 = 1160（**撑满** 400 ⇒ 那一版 `m_PreserveAspect = 0`）");
                        CheckNear(oneT, 562.5f, 0.6f, "★ ……上沿 = 562.5");
                        CheckNear(oneB, 637.5f, 0.6f, "★ ……下沿 = 637.5（高 75）");
                    }
                    // 🔴 **2026-10-14（A533）加强**：这里原来是一条**三条件命一即可**的弱断言
                    //   （`wm2 == null || !one.gameObject.activeSelf || wm2.popUpWindow != one`）——
                    //   首项在**收尾信号丢失**时（`WindowsManager.Instance` 拿不到）也恒真 ⇒
                    //   「窗根本没被收掉」这种最该报的情况**反而是绿的**。拆成三条**各断一件事**，
                    //   并补上「它确实是 `popUpWindow`」这条前提 —— ⛔ 不是删掉（铁律：不许弱化断言）。
                    var wm2 = WindowsManager.Instance;
                    CheckTrue(wm2 != null,
                              "（前提）窗口管理器还在（`EnsureHost()` 刚**显式登记**过 `Instance`，见它里面那段注释）"
                            + " —— 拿不到 ⇒ 下面两条就没有鉴别力（旧写法正是「`wm2 == null` 也算通过」）");
                    if (wm2 != null)
                    {
                        CheckTrue(ReferenceEquals(wm2.popUpWindow, one),
                                  "（前提）刚才那一扇确实登记成了 `WindowsManager.popUpWindow` —— 否则 "
                                + "`HidePopUp()` 按原版先比 `currentWindow == popUpWindow`、**直接早退**"
                                + " ⇒ 下面那条「收掉了」会变成假绿（原版 `WindowsManager__HidePopUp.c` 第一句）");
                        wm2.HidePopUp();                          // 收掉（别留给后面的截图/断言）
                        CheckTrue(wm2.popUpWindow != one,
                                  "（收尾）1 按钮版那一扇收掉了：`popUpWindow` **不再**指着它"
                                + "（原版 `HidePopUp` → `CloseWindow` → 摘表 + 清字段；把那句收窗删掉 ⇒ 这条红）");
                    }
                    CheckTrue(!one.gameObject.activeSelf,
                              "（收尾）……而且那一扇真的**不激活**了（收尾信号丢失 / 收窗被早退 ⇒ 这条红）");
                }

                // ---- 收尾：卡组**原样**还回去（后面几节接着用这副牌）----
                live.CardIds.Clear(); live.CardIds.AddRange(keepIds);
                live.Name = keepName;
                _rt.UiScrollPool(0f);
                Check(_rt.State.Validate(), DeckError.None, "（收尾）张数与名字都还回去 ⇒ 这副牌又合法");
                _rt.EscPressed();                                    // 落盘 ⇒ 盘上与内存又同一份（A363 那一节的前提）
                CheckTrue(!_rt.ModalPopupOpen, "（收尾）没有模态窗留在场上（后面的截图/点击不受影响）");
            }


            // ======== 🆕 2026-10-12（A363）：**剩下两个突变点也只标脏** ========
            //   `DeckRuntime` 的 5 个突变调用点里，「换卡背」「拖出删除」「改名」三条已在上面
            //   各自那一节改向断言；这里补最后两条：**卡池加牌**（`TryAddCard`）与
            //   **页头清空名字**（`name_clear`）。
            //   ⛔ 这 5 处任何一处被改回 `CommitDeck()`，本节或上面三节必红。
            {
                // 🔴 **A397**：`live` = 编辑器那份**副本**；本节只改内存（⛔ 不走 `CommitDeck()`），
                //    所以下面「盘上没动」那几条断的是「没人调 `Save()`」——不是「内存==盘上的对象」。
                var live = _rt.State.Deck;
                var snapIds = new List<string>(live.CardIds);
                const string snapName = "自检·改的名";
                Check(_rt.State.Validate(), DeckError.None, "（前提）这一节起手这副牌合法");
                Check(DeckLibrary.ExportString(live), DeckLibrary.ExportString(DeckLibrary.Load().Current),
                      "（前提）起手**内存与盘上是同一份** —— 不然下面「盘上没动」那两条没有鉴别力");

                // ---- ① `TryAddCard`（卡池点一张加进卡组）----
                // 「盘上那份」的指纹 = `ExportString`（**原版卡组串**：名字 + 防御卡 + 督军 + 全部卡 + 模式）
                // ⇒ 一个字符串就够判「盘上到底动没动」，比逐字段比更省也更严。
                string removedId = live.CardIds[0];
                live.CardIds.RemoveAt(0);              // 腾一格（只动内存，同上面 A330 那节的写法）
                CardDef add = null;
                foreach (var c in _rt.State.VisibleCards())
                    if (c != null && c.Id != removedId && _rt.State.CanAdd(c) == DeckError.None) { add = c; break; }
                CheckTrue(add != null,
                          $"（前提）卡池里找得到一张加得进去、且**不是刚摘掉那张**的牌（{add?.Name}）");
                string disk0 = DeckLibrary.ExportString(DeckLibrary.Load().Current);
                _rt.UiAddCard(add);
                Check(_rt.State.DeckCount, snapIds.Count, "……加回去 ⇒ 又是满编（内存那 30 张）");
                CheckTrue(_rt.DeckDirty, "★ A363：加牌 ⇒ **标脏**");
                Check(DeckLibrary.ExportString(DeckLibrary.Load().Current), disk0,
                      "★ A363：……盘上那份**一个字节都没动**（加牌自己写盘 = 这条红）");
                CheckTrue(DeckLibrary.ExportString(live) != disk0,
                          "（前提）内存这份**确实已经和盘上不一样了**（换过牌）⇒ 上一条才有鉴别力");
                _rt.EscPressed();                                        // = Done
                CheckTrue(!_rt.DeckDirty, "★ ……按 Done ⇒ 脏标记清掉");
                CheckTrue(DeckLibrary.ExportString(DeckLibrary.Load().Current) != disk0,
                          "★ ……**Done 之后才落盘**（盘上那份变了）");

                // ---- ② `name_clear`（页头那颗清空卡组名的钮）----
                //  🔴 **2026-10-12（A396）改向**：这颗钮原来**用鼠标点不到** —— 它的矩形（277.2,316,35×40）
                //     整个落在 `name_box`（9.5,311,307.7×50）里面，而 `ClickOrder` 里 `name_box` **排在它前面**
                //     ⇒ `TopKeyAt(清空图标中心)` 恒是 `name_box`（点一下 = 进改名态，不是清空）。
                //     修法 = 照**可见层**把 `name_clear` 挪到 `name_box` **前面**（清空图标 `QBorder = 3005`
                //     画在输入框底 `QPanel = 3004` 之上 —— 判据在 `DeckRuntime.ClickOrder` 那段注释里）。
                //     ⇒ 下面这几条**走真鼠标那条路**（`UiClickPx` 点图标正中心），⛔ 不再用 `UiNameClear()`
                //     （那个自检口**正好绕开**了要验的这件事 —— 拿它当证据就是自证）。
                string disk1 = DeckLibrary.ExportString(DeckLibrary.Load().Current);
                const float ClrCx = (277.2f + 312.2f) * 0.5f;     // = 294.7：清空图标正中心
                const float ClrCy = (316f + 356f) * 0.5f;         // = 336
                Check(_rt.UiTopKeyAt(ClrCx, ClrCy), "name_clear",
                      "★ A396：清空图标正中心命中的是 **`name_clear`**（`name_box` 排前面时这里是 `name_box`）");
                CheckTrue(_rt.UiClickPx(ClrCx, ClrCy),
                          "★ A396：点它 ⇒ **有人吃这一下**（原来被 `name_box` 吃掉 = 「亮得起来但点不到」）");
                Check(live.Name, "新卡组", "……名字被写成「新卡组」");
                CheckTrue(_rt.DeckDirty, "★ A363：`name_clear` ⇒ **标脏**");
                Check(DeckLibrary.ExportString(DeckLibrary.Load().Current), disk1,
                      "★ A363：……盘上那份**还是没动**（清名字自己写盘 = 这条红）");

                // 🔴 **A396 的反向对照**：顺序挪对之后，`name_box` **自己那块地**照样进改名态 ——
                //   只让出图标那 35×40，⛔ 不是整颗都不认了。**少了这一条，一个「把 `name_box` 从表里删掉」
                //   的实现照样能过上面三条**（那会把「点名字框改名」整条功能弄丢）。
                const float BoxCx = 60f;                          // 名字框里**远离图标**的一处
                const float BoxCy = 336f;
                Check(_rt.UiTopKeyAt(BoxCx, BoxCy), "name_box",
                      "★ A396（反向）：名字框左侧那一点仍然命中 `name_box`（把整颗挪没 = 这条红）");
                CheckTrue(_rt.UiClickPx(BoxCx, BoxCy), "★ ……有人吃这一下");
                Check(_rt.UiEditKind, 1, "★ ……进的是**改名态**（`name_box` 那条路没被挪坏）");
                _rt.UiCancelEdit();                              // 退出改名态（收尾那句 `EscPressed()` 要它先退出）
                Check(_rt.UiEditKind, 0, "（收尾）退出改名态");

                // ---- 收尾：把卡组**原样**还回去（后面几节还用这副牌）----
                live.CardIds.Clear(); live.CardIds.AddRange(snapIds);
                live.Name = snapName;
                _rt.EscPressed();
                Check(DeckLibrary.ExportString(DeckLibrary.Load().Current), disk0,
                      "（收尾）盘上逐字节复原成这一节起手那份（名字 + 卡序都一样）");
            }

            // ======== 🆕 2026-10-13（A397）：**编辑器编辑的是【副本】** ========
            //  判据 = 原版 `DeckEditingWindow__TryOpen`
            //  （`d:/2/tools/decomp_full/DeckEditingWindow__TryOpen.c:41-45`）：
            //   `lVar5 = new CardDeck(...)` → `CardDeck___ctor(lVar5, plVar8 /* 开窗时传进来那副 */, 0)`
            //   （**拷贝构造**）→ `*(window + 0x118) = lVar5`（`EditingDeck` 存的是**副本**；
            //   `get_EditingDeck` 读的就是 `+0x118`）。真写回库里那份只在一处 =
            //   `DeckEditingWindow__UploadDeck.c` 的 `CardDeck__CopyDeck(库那份 ← 编辑中那份)`
            //   （= 我们 `Done`/`ESC` 那一拍的 `CommitDeck`）。
            //
            //  ⚠️ **我们原来是直接赋引用**（`LoadDeck` 里 `Deck = deck;`）⇒ `State.Deck` 就是
            //     `DeckLibrary.Current` **那个对象本身**，后果：**任何库级 `Save()`**
            //     （`Create` / `Delete` / `Add` / `Duplicate` / `Rename` —— 它们序列化**整份库**）
            //     会把编辑器里**还没 Done 的改动顺手写盘**。原版不会：那些改动只活在副本里。
            //
            //  ⛔ 下面三条**各断一件事**，缺一条就留下一种假绿（判据/改坏法逐条写在文案里）：
            //     ① 结构 = 两者**不是同一个对象**；② 行为 = **库级 `Save()` 不写未 Done 的改动**；
            //     ③ 正向对照 = `Done` 那一拍**照样**写回库里（挡住「把写回整条拆掉」的假绿）。
            {
                Section("A397：编辑器编辑的是**副本**（库级 `Save()` 不会把未 Done 的改动顺手写盘）");
                int idx397 = _rt.Library.CurrentIndex;
                var libDeck397 = _rt.Library.Current;                 // 库里那个对象（**内存**里那个）
                CheckTrue(idx397 >= 0 && libDeck397 != null, "（前提）库里有一套选中的卡组");
                var keepIds397 = new List<string>(_rt.State.Deck.CardIds);
                string keepName397 = _rt.State.Deck.Name;
                Check(DeckLibrary.ExportString(_rt.State.Deck), DeckLibrary.ExportString(libDeck397),
                      "（前提）起手**编辑器那份与库里那份内容一致**（不然下面「盘上没动」那几条没有鉴别力）");

                // ---- ① 结构：编辑的是**副本**，不是库那个对象 ----
                CheckTrue(!ReferenceEquals(_rt.State.Deck, libDeck397),
                          "★ A397：`State.Deck` **不是** `Library.Current` 那个对象 —— 编辑的是**副本**"
                        + "（原版 `__TryOpen.c:41-44`：`new CardDeck` + 拷贝构造 ⇒ `EditingDeck` 是副本；"
                        + "`DeckEditorState.LoadDeck` 改回「直接赋引用」⇒ 这条红）");

                // ---- ② 行为：**库级 `Save()` 不会**把未 Done 的改动顺手写盘 ----
                //  造局：只动**内存**（⛔ 不按 `EscPressed()` —— 那会真提交）：摘掉一张 + 标脏。
                //  库里那个对象 / 盘上那份在这一拍都必须**原封不动**。
                _rt.State.Deck.CardIds.RemoveAt(0);
                _rt.MarkDeckDirty();
                CheckTrue(_rt.DeckDirty, "（前提）摘下这一张 ⇒ 标脏了（还没有 Done）");
                Check(_rt.State.DeckCount, keepIds397.Count - 1, "（前提）编辑器那份少了 1 张");
                Check(libDeck397.CardIds.Count, keepIds397.Count,
                      "★ A397：……而且**库里那个对象在内存里也没被改**（摘牌只落在副本上；"
                    + "两者是同一个对象时这里会是 " + (keepIds397.Count - 1) + " 张）");
                //  ⛔ **不拿** `Create` / `Duplicate` / `Add` 造这次「库级 `Save()`」—— 那三个**会挪选中项**
                //     （`_current` 跑到新那套去），而 `Done` 是写**当前选中**那一套的
                //     （`DeckLibrary.CommitCurrent`）⇒ 夹具自己会把库搅乱、还会让 ③ 写到别人身上。
                //     `Rename` 是那条清单里**唯一不动选中项**的（只写名字 + `Save()` 整份库）。
                string libName397 = libDeck397.Name;
                CheckTrue(_rt.Library.Rename(idx397, libName397 + "·A397 库级"),
                          "（造局）走一次**库级**写盘路：`DeckLibrary.Rename`（内部 `Save()` 序列化**整份库**）");
                try
                {
                    var disk397 = DeckLibrary.Load();
                    CheckTrue(idx397 < disk397.Count, "（前提）盘上读得回那一套");
                    if (idx397 < disk397.Count)
                        Check(disk397.Decks[idx397].CardIds.Count, keepIds397.Count,
                              "★ A397：库级 `Save()` 落的是**库那份**（" + keepIds397.Count + " 张）—— "
                            + "编辑器与它若是同一个对象，这种**没 Done 的摘牌**会被顺手写进盘（"
                            + (keepIds397.Count - 1) + " 张）");
                    Check(disk397.Decks[idx397].Name, libName397 + "·A397 库级",
                          "（前提）它确实写盘了（不然上一条没有鉴别力：写盘失败时「盘上没变」是假绿）");
                }
                finally
                {
                    _rt.Library.Rename(idx397, libName397);          // 名字还回去（再 `Save()` 一次）
                }
                Check(DeckLibrary.Load().Decks[idx397].Name, libName397, "（收尾）库里的名字复原");

                // ---- ③ 正向对照：`Done` 那一拍**照样**把副本写回库里 ----
                //  少了这一条，一个「干脆不写回」的实现（比如把 `CommitDeck` 拆掉）照样能过 ①②。
                _rt.State.Deck.CardIds.Insert(0, keepIds397[0]);          // 补回来 ⇒ 又合法（`Done` 那道闸才放行）
                _rt.UiScrollPool(0f);                                     // 逼一次 `RefreshHeader`（批处理没有帧循环）
                Check(_rt.State.Validate(), DeckError.None, "（前提）补回来 ⇒ 又合法");
                _rt.State.Deck.Name = keepName397 + "·A397 Done 写回";
                _rt.EscPressed();                                         // = Done = 原版 `__TrySaveDeck` → `__UploadDeck`
                CheckTrue(!_rt.DeckDirty, "★ A397：`Done` 之后脏标记清掉（= 那一下真的写成功了）");
                Check(libDeck397.Name, keepName397 + "·A397 Done 写回",
                      "★ A397：……副本被**整份拷回库里那个对象**（原版 `__UploadDeck.c` 的 "
                    + "`CardDeck__CopyDeck(库那份 ← 编辑中那份)`；把 `CommitDeck` 拆掉 ⇒ 这条红）");
                Check(DeckLibrary.Load().Decks[idx397].Name, keepName397 + "·A397 Done 写回",
                      "★ ……而且落了盘（从盘上重读就是它）");
                // 🔴 **提交之后两边仍然不共享卡表** —— `CommitCurrent` 是**整份拷**
                //   （`dst.CardIds = new List<string>(deck.CardIds …)`）。少了这条：「副本隔离」在
                //   **第一次 Done 之后**就失效了（两边又共用一个 `List`，后面未 Done 的改动照样漏进库）。
                //   改坏法：`DeckLibrary.CommitCurrent` 里那句的 `new List<string>(…)` 去掉 `new` ⇒ 这条红。
                _rt.State.Deck.CardIds.RemoveAt(0);
                Check(libDeck397.CardIds.Count, keepIds397.Count,
                      "★ A397：……提交之后编辑器与库里那份**仍然不共享卡表**"
                    + "（再摘一张，库那份还是 " + keepIds397.Count + " 张；共用同一个 `List` ⇒ 这里会是 "
                    + (keepIds397.Count - 1) + " 张）");
                _rt.State.Deck.CardIds.Insert(0, keepIds397[0]);              // 还回去（还没 Done ⇒ 与库又一致）
                // ---- 收尾：名字复原 + 再存一次（后面几节接着用这副牌）----
                _rt.State.Deck.Name = keepName397;
                _rt.EscPressed();
                Check(DeckLibrary.Load().Decks[idx397].Name, keepName397, "（收尾）名字复原并落盘");
                Check(_rt.State.DeckCount, keepIds397.Count, "（收尾）仍是满编");
                CheckTrue(!_rt.DeckDirty, "（收尾）干净离场（后面几节的前提）");
            }

            // 卡名筛选的输入框（原版 `CardNameFilter`）：点进去 → 输入 → 回车
            _rt.UiToggleFilters();
            _rt.UiFilterRow("$name");
            Check(_rt.UiEditKind, 2, "点 Name 那一行 → 进了「改卡名筛选」的输入态");
            _rt.UiCommitEdit("autarch");
            Check(_rt.UiEditKind, 0, "回车之后退出输入态");
            Check(state.Filter.Name, "autarch", "卡名筛选生效");
            CheckTrue(state.VisibleCards().Count >= 1 && state.VisibleCards().Count < state.PoolCount,
                      $"筛出 {state.VisibleCards().Count} 张（少于全部）");
            _rt.UiFilterRow("$name");
            _rt.UiCommitEdit("");
            Check(state.Filter.Name, "", "再输入空串 → 清空卡名筛选");
            _rt.UiToggleFilters();

            // 分享 / 导入（后端 `DeckLibrary.ExportString`/`ImportString`，原版格式）
            var str = _rt.UiShareString();
            CheckTrue(str.Length > 0, $"分享：导出卡组串（{str.Length} 字符）");
            int n1 = DeckLibrary.Load().Count;
            Check(_rt.UiModalActive, 0, "没打开时，导入弹窗**一件都不显示**（图 + 文字都算）");
            _rt.UiOpenImport();
            CheckTrue(_rt.UiModalVisible, "导入弹窗打开了");
            CheckTrue(_rt.UiModalActive >= 8, $"弹窗开着一共 {_rt.UiModalActive} 件（图 + 文字）显示出来");
            Shoot("deck_import.png");
            _rt.UiSetImportText(str);
            CheckTrue(_rt.UiTryImport(), "把刚导出的串导回来 → 成功");
            CheckTrue(!_rt.UiModalVisible, "导入成功后弹窗自动关掉");
            Check(DeckLibrary.Load().Count, n1 + 1, "卡组库里多了一套（导入是**新增**不是覆盖）");
            _rt.UiOpenImport();
            _rt.UiSetImportText("这不是一条卡组串");
            CheckTrue(!_rt.UiTryImport(), "乱串被挡");
            CheckTrue(_rt.ImportError.Length > 0, $"被挡时给了人话错误（「{_rt.ImportError}」）");
            _rt.UiCloseImport();
            CheckTrue(!_rt.UiModalVisible, "点关闭 → 弹窗收起");

            // 🔴 「拖出侧栏 = 删除」与「点一下 = 弹大图」——**删牌只有拖出这一条路**
            //    （原版 `DeckEditingPanel__CheckCardSlotDrag.c:59,66`），批处理没鼠标，
            //    所以走 `UiDragDeckRow` / `UiClickDeckRow`（**调的是鼠标那条路的同一段代码**）。
            _rt.UiSetTab(0);
            int n2 = _rt.State.DeckCount;
            var rowDef = _rt.UiDeckRowAt(0);
            CheckTrue(rowDef != null, $"第 0 行有牌可操作（{rowDef?.Name}）");
            CheckTrue(_rt.UiClickDeckRow(0), "模拟「点一下卡组第 0 行」");
            CheckTrue(_rt.UiCardWindowVisible, "……弹出放大窗（原版：**点卡组里的牌是弹大图、不是删除**）");
            Check(_rt.UiCardWindowTitle, CardText.Name(rowDef.Name, rowDef.NameZh), "放大窗里就是那一张");
            _rt.UiCloseCardWindow();
            Check(_rt.State.DeckCount, n2, "只点不开删 —— 卡组张数没变");
            // ⚠️ 行 0/1 是**督军 / 防御卡**（不占 30 张的名额）⇒ 拿行 2（第一张普通卡）验删除
            var delDef = _rt.UiDeckRowAt(2);
            CheckTrue(delDef != null && delDef.Type == "unit", $"第 2 行是普通单位卡（{delDef?.Name} / {delDef?.Type}）");
            // 往**侧栏里面**拖：不算拖出 ⇒ 不删
            CheckTrue(_rt.UiDragDeckRow(2, 150f), "模拟「把第 2 行拖到侧栏里面（x=150）松开」");
            Check(_rt.State.DeckCount, n2, "拖到侧栏里松开 → **不删**（判据是「拖出侧栏」）");
            // 往**侧栏外面**拖：算拖出 ⇒ 删掉一张
            CheckTrue(_rt.UiDragDeckRow(2, 1200f), "模拟「把第 2 行拖到侧栏外（x=1200）松开」");
            Check(_rt.State.DeckCount, n2 - 1, "拖出侧栏松开 → **卡组里少一张**（照原版删牌）");
            // 🔴 **2026-10-12（A363）改向**：删牌 = `DeckEditingPanel__RemoveCard.c:13` 那一句
            //    `deck[+0x60] = 0` ⇒ **只标脏**；落盘在 `Done`（`__TrySaveDeck` → `:100 UploadDeck`）。
            //    （原来这条断的是「删一下就落盘」—— 我们的偏离，已按原版改掉。）
            CheckTrue(_rt.DeckDirty, "★ A363：拖出一张 ⇒ **标脏**");
            Check(DeckLibrary.Load().Current.CardIds.Count, n2,
                  "★ A363：……而且**盘上还是 " + n2 + " 张**（删一下就写盘 = 这条红）");

            // ---- 🔴 **2026-10-12（A415）改向**：删到 29 张 ⇒ **不合法** ⇒ `Done` **不放行** ----
            //  这一节原来接着断「按 Done ⇒ 脏标记清掉 **且** 盘上 = n2−1」—— **那两条与本实现的闸直接冲突**
            //  （= D2 §六·3 记下的那条红）：`n2` 是**满编**（30），拖出删一张 ⇒ **29** ⇒ `State.Validate()` =
            //  `TooFewCards` ⇒ `SaveAndSay()` 那道闸（A330）**不放行** ⇒ 脏标记清不掉、盘上也不会变成 29。
            //  ⚖️ **先定口径：哪一条才是原版语义**（判据 = `d:/2/tools/decomp_full/DeckEditingWindow__TrySaveDeck.c` 逐句）：
            //    `:80 cVar3 = DeckUtility__ValidateDeck(deck, out err, /*validateOwnership*/1, 0);`
            //    `:81 if (cVar3 == '\0')` → `:94 WindowsManager__ShowPopUp(ToRawLocalizationString(err), 1, 0, 左, 右)`
            //                             → `:95 return`（**绝不走到 `:100` 的 `UploadDeck`**）
            //    `:99 else`              → `:100 DeckEditingWindow__UploadDeck(...)` → `:103 WindowsManager__HidePopUp(...)`
            //  ⇒ 原版就是「**非法卡组不进库**」（`ESC` 走的是同一个函数：`DeckEditingWindow__ESCPressed.c:5`）
            //    ⇒ ✅ **改夹具的期望**：盘上**不变** + **出声**（那扇模态窗；页脚那行另有 A330 段盯着）。
            //    ⛔ **不是**放宽那道闸、⛔ 更不是改 `Validate()` 的规则去迁就夹具 —— 那两样都是「为了让断言变绿而造假」。
            //  ⚠️ 上面那两条「拖出 ⇒ 标脏 / 盘上没动」**照旧成立**（它们不依赖 `Done`），一个字没动。
            Check(_rt.State.Validate(), DeckError.TooFewCards,
                  $"（前提）删到 {_rt.State.DeckCount}/{_rt.State.MaxDeckCount} ⇒ 不合法（`TooFewCards`）"
                  + " —— 下面三条判的就是「不合法 ⇒ 不放行」");
            _rt.EscPressed();                                        // = Done = `TrySaveDeck`
            CheckTrue(_rt.DeckDirty, "★ A415：**不合法 ⇒ 按 Done 也不清脏标记**"
                  + "（把 `SaveAndSay()` 那道闸删掉 / 改成 `if (false)` ⇒ 这条红）");
            Check(DeckLibrary.Load().Current.CardIds.Count, n2,
                  "★ A415：……盘上**照旧 " + n2 + " 张**（原版 `:95 return`，不走 `:100 UploadDeck`；"
                  + "水位放宽 / 绕过那道闸 ⇒ 这条红）");
            CheckTrue(_rt.ModalPopupOpen,
                  "★ A415：……而且**出声**了 —— 原版 `:94` 那扇模态消息窗此刻开着（静默挡下 = 这条红）");

            // 收尾：**先收窗、再把牌补回去** —— 后面几节接着用这副牌，⛔ 别把模态窗留在场上：
            //     它的闸（`HandlePointer` / `UiClickPx` 各一处）会挡住后面**所有**指针动作 ⇒
            //     那些断言会**静默退化成空断**（这是本工程最怕的那种「绿得没道理」）。
            //  ⚠️ 顺序不能反：窗开着时**指针那条路进不去**（`UiClickPx` / `HandlePointer` 各一道模态闸），
            //    而 ESC 也到不了保存 —— 🔴 **2026-10-13（A502）改口径**：原来是「ESC 会把**同一扇窗**
            //    再配一遍（D2 §五·5）、不落盘」；照原版补上那一级之后 ESC 索性**什么都不做**
            //    （判据 = 那扇窗自己的 `closeOnEsc = 0`）⇒ 结论没变（**不会落盘**），
            //    所以**必须先收窗**（下面那句）再按 Done/ESC。
            var popA415 = _rt.ModalPopup;
            CheckTrue(popA415 != null, "（收尾）拿得到刚才那扇窗（拿不到 ⇒ 下面那句收窗是空做）");
            if (popA415 != null) ClickPopupButton(popA415, "ButtonRight");   // = 原版那颗只调 `HidePopUp()` 的 Cancel
            CheckTrue(!_rt.ModalPopupOpen, "（收尾）点右钮 ⇒ 窗收掉（后面的截图/点击不受影响）");
            if (delDef != null) _rt.UiAddCard(delDef);               // 补回那一张（走生产那条路 ⇒ 视图与状态一起跟上）
            Check(_rt.State.DeckCount, n2, "（收尾）补回一张 ⇒ 又是满编");
            Check(_rt.State.Validate(), DeckError.None, "（收尾）又合法了");
            _rt.EscPressed();                                        // 这一次合法 ⇒ **才**落盘
            CheckTrue(!_rt.DeckDirty, "★ A415（反证）：**合法之后**同一颗 Done 照样落得下去"
                  + " —— 谁把闸写成恒关（挡死一切保存）⇒ 这条红");
            Check(DeckLibrary.Load().Current.CardIds.Count, n2, "（收尾）盘上也是满编（这一下真落盘了）");

            // ======== 🆕 2026-10-12（A398）：`DeckLibrary` 不再吞 `Save()` 的返回值 ========
            //  D1 §五·b·2 那笔账：`CommitCurrent` 已在 A363 修成 `return Save();`，同族**还有 5 处**
            //  （`Rename` / `Delete` / `Create` / `Duplicate` / `Add`）。
            //  🔴 **怎么验才不是自证**：把存档路径换成一个**必然写不进去**的地方（父目录不存在 ⇒
            //     `File.WriteAllText` 抛 `DirectoryNotFoundException` ⇒ `DeckStore.SaveAll` 返 false），
            //     再看「写失败」这件事**说没说得出话**；最后用一条**控制组**（路径正常 ⇒ 回 true）挡住
            //     「恒返回 false」那种假绿。⛔ 不碰玩家的真存档（`OverridePath` 用完还回去）。
            {
                Section("A398：`DeckLibrary` 的落盘失败**说得出话**（`return Save();` / `SaveOrWarn`）");
                string keepPath = RuleEngine.DeckStore.OverridePath;
                string probeDir = System.IO.Path.GetDirectoryName(keepPath);
                string goodPath = System.IO.Path.Combine(probeDir, "_a398_probe.json");
                string badPath = System.IO.Path.Combine(probeDir, "__wf_a398_no_such_dir__", "x.json");
                CheckTrue(!string.IsNullOrEmpty(probeDir) && System.IO.Directory.Exists(probeDir),
                          $"（前提）探针要用的目录存在（{probeDir}）—— 不存在 ⇒ 下面全是假绿");
                try
                {
                    // ---- ① 写不进去的路径 ----
                    RuleEngine.DeckStore.OverridePath = badPath;
                    var lib = DeckLibrary.Load();        // 读不到 ⇒ 空库；`LastError` 是 null（那是「还没有存档」，不是失败）
                    CheckTrue(lib.LastError == null, "（前提）起手 `LastError` 是空的");
                    CheckTrue(!lib.Save(), "（前提）这条路径**确实写不进去**（写得进去 ⇒ 下面几条全是假绿）");

                    var mk = lib.Create("A398·Create");
                    CheckTrue(mk != null, "★ A398：`Create` 照旧把那一套造出来（内存里）");
                    CheckTrue(lib.LastError != null,
                              "★ A398：……而落盘失败**说得出话**（`LastError` 里有人话；把 `SaveOrWarn` 换成裸 `Save();` ⇒ 这条红）");
                    var dup = lib.Duplicate(0);
                    CheckTrue(dup != null && lib.LastError != null, "★ A398：`Duplicate` 同上");
                    var added = lib.Add(new PlayerDeck("A398·Add", null, null, null, 0));
                    CheckTrue(added != null && lib.LastError != null, "★ A398：`Add` 同上");
                    CheckTrue(!lib.Rename(0, "A398·改名"),
                              "★ A398：`Rename` **如实回传落盘失败**（改回「`Save(); return true;`」⇒ 这条红）");
                    CheckTrue(lib.LastError != null, "★ ……而且原因还在（它只回 bool、不带原因）");
                    CheckTrue(!lib.Delete(0),
                              "★ A398：`Delete` 同上（改回「`Save(); return true;`」⇒ 这条红）");

                    // ---- ② 控制组：同几条路，路径正常 ⇒ 回 true ----
                    RuleEngine.DeckStore.OverridePath = goodPath;
                    var lib2 = DeckLibrary.Load();
                    var mk2 = lib2.Create("A398·控制");
                    CheckTrue(mk2 != null && lib2.LastError == null,
                              "（控制组）路径正常 ⇒ `Create` 落盘成功、`LastError` 空（**没有这一条，一个恒报失败的实现照样绿**）");
                    CheckTrue(lib2.Rename(0, "A398·控制改名"), "（控制组）……`Rename` 回 true（`return Save();` 两边都对）");
                    CheckTrue(lib2.Delete(0), "（控制组）……`Delete` 回 true");
                }
                finally
                {
                    RuleEngine.DeckStore.OverridePath = keepPath;          // 玩家的/夹具的路径还回去
                    try { if (System.IO.File.Exists(goodPath)) System.IO.File.Delete(goodPath); } catch { }
                }
            }

            // ======== 🆕 2026-10-12（A399）：关窗链 = `TryClose`（脏了才 `ConfirmDiscard`）========
            //  A363 只把**前置条件**（`DeckDirty`）补上了，UI 出口一直没接 —— 这一节接上。
            //  判据逐句抄在 `DeckRuntime.TryClose` 那段注释里（`__TryClose.c` / `__ConfirmDiscard.c` /
            //  `.<ConfirmDiscard>b__45_0.c` / `…b__45_1.c` + 正文键的**地址表实读**）。
            //  ⛔ 这一节**不许**改成「点关闭钮 = 保存」—— 原版关闭钮那条链**从不保存**（保存归 Done/ESC）。
            {
                Section("A399：关窗 = `TryClose`（脏了才问「丢弃」；关闭钮**从不保存**）");
                if (_rt.FiltersOpen) _rt.UiToggleFilters();     // 抽屉开着时 `UiClickPx` 会先把它那一带吃掉
                CheckTrue(!_rt.FiltersOpen, "（前提）筛选抽屉关着（开着时 x < FltX+FltW 的点击归它）");
                CheckTrue(!_rt.ModalPopupOpen, "（前提）没有模态窗");
                const float BackCx = 192.2f + 150f * 0.5f;      // `hdr_back` 正中心 = (267.2, 113.5)
                const float BackCy = 83.5f + 60f * 0.5f;
                Check(_rt.UiTopKeyAt(BackCx, BackCy), "hdr_back", "（前提）关闭钮正中心命中的是 `hdr_back`");
                int leave0 = _rt.LeaveCount;
                string diskA = DeckLibrary.ExportString(DeckLibrary.Load().Current);
                int fullA = _rt.State.DeckCount;
                var snapA = new List<string>(_rt.State.Deck.CardIds);
                CheckTrue(!_rt.DeckDirty, "（前提）起手**干净**（上一节收尾按过 Done）");

                // ---- ① 干净 ⇒ **直接离场、不弹窗**（原版 `__TryClose.c:15` 那个虚槽 `0x1b8` 那一支）----
                CheckTrue(_rt.UiClickPx(BackCx, BackCy), "点关闭钮（中心）⇒ 有人吃了这一下");
                Check(_rt.LeaveCount, leave0 + 1, "★ A399：**干净**时点关闭钮 ⇒ 直接离场（原版 `TryClose` 的干净支）");
                CheckTrue(!_rt.ModalPopupOpen, "★ ……而且**没弹窗**（干净也弹 ⇒ 这条红）");
                Check(DeckLibrary.ExportString(DeckLibrary.Load().Current), diskA, "★ ……盘上没动");

                // ---- ② 脏 ⇒ **先问**（原版 `:9` 那句判 `deck[+0x60]` ⇒ `:10 ConfirmDiscard`）----
                var rowA = _rt.UiDeckRowAt(2);
                CheckTrue(rowA != null && _rt.UiDragDeckRow(2, 1200f), "（造脏）把第 2 行拖出侧栏 ⇒ 删掉一张");
                CheckTrue(_rt.DeckDirty, "（前提）这一下**标脏了**");
                CheckTrue(_rt.UiClickPx(BackCx, BackCy), "脏了再点关闭钮");
                Check(_rt.LeaveCount, leave0 + 1,
                      "★ A399：脏了 ⇒ **不离场**（先问；直接 `BackToMenu()` ⇒ 这条红）");
                var popAsk = _rt.ModalPopup;
                CheckTrue(popAsk != null, "★ ……而是弹出「丢改动」那扇模态窗（原版 `ConfirmDiscard`）");
                if (popAsk != null)
                {
                    Check(popAsk.MessageKey, DeckRuntime.DiscardChangesKey,
                          "★ 正文键 = `MenuDeck/HUD/DiscardChanges`（原版 `DAT_1842d00e8` 的地址表实读；"
                        + "自己编一句人话 ⇒ 这条红）");
                    Check(popAsk.PrimaryKey, "MainMenu/General/Discard",
                          "★ 左钮 = `MainMenu/General/Discard`（原版 `DAT_1842be418` → `.<ConfirmDiscard>b__45_1`）");
                    Check(popAsk.SecondaryKey, "MainMenu/General/Cancel",
                          "★ 右钮 = `MainMenu/General/Cancel`（原版 `DAT_1842be120` → `.<ConfirmDiscard>b__45_0`）");
                    CheckTrue(_rt.DeckDirty, "★ ……此刻脏标记还在（还没丢）");
                    Check(DeckLibrary.ExportString(DeckLibrary.Load().Current), diskA, "★ ……盘上也还没变");

                    // 右钮 = **只** `HidePopUp()`（留在编辑器里）
                    ClickPopupButton(popAsk, "ButtonRight");
                    CheckTrue(!_rt.ModalPopupOpen, "★ 点**右钮**（`Cancel`）⇒ 窗关掉");
                    Check(_rt.LeaveCount, leave0 + 1, "★ ……而且**没离场**（原版那颗回调体里只有 `HidePopUp()`）");
                    CheckTrue(_rt.DeckDirty, "★ ……脏标记也**留着**（取消 = 什么都没丢）");
                    Check(DeckLibrary.ExportString(DeckLibrary.Load().Current), diskA, "★ ……盘上仍然没动");
                }

                // ---- ③ 再来一次 ⇒ 这回点**左钮**（`Discard`）= `HidePopUp()` + 关窗 ----
                CheckTrue(_rt.UiClickPx(BackCx, BackCy), "再点关闭钮");
                var popGo = _rt.ModalPopup;
                CheckTrue(popGo != null, "（前提）又问了一次");
                if (popGo != null) ClickPopupButton(popGo, "ButtonLeft");
                Check(_rt.LeaveCount, leave0 + 2,
                      "★ A399：点**左钮**（`Discard`）⇒ **离场**（原版 `b__45_1` = `HidePopUp()` + 虚槽 `0x1b8`；"
                    + "把那颗回调接成一个什么都不做的钮 ⇒ 这条红）");
                CheckTrue(!_rt.ModalPopupOpen, "★ ……窗也关掉了");
                CheckTrue(!_rt.DeckDirty, "★ ……待写的改动**作废**（脏标记清掉；不清 ⇒ 下次 Done 还会把它写进去）");
                Check(DeckLibrary.ExportString(DeckLibrary.Load().Current), diskA,
                      "★ ……而且盘上**一个字节都没动** —— 关闭钮这条链**从不保存**（原版只有 `Done`/`ESC` 才 `UploadDeck`；"
                    + "把 `DiscardChangesAndLeave` 里的 `BackToMenu()` 换成 `SaveAndSay()` ⇒ 这条红）");
                // 🔴 **2026-10-14（A565）新增**：同一颗左钮还要**把内存那份副本回滚成库里那份**
                //   （原版那一刻是**关窗** ⇒ 窗上那份 `EditingDeck` 随窗销毁；不回滚的话，批处理/不切场景
                //   路径上按一次 `Done` 会把**刚被丢弃的那份**写回库）。两条期望值都是**绝对量**
                //   （`fullA` / `diskA` 都是 Discard **之前**取的）⇒ 与「回滚成谁」无关，指哪打哪。
                Check(_rt.State.DeckCount, fullA,
                      $"★ A565：……而且**内存那份也回滚了**（张数从被丢弃那份的 {fullA - 1} 回到满编 {fullA}）"
                    + " —— 删掉 `DiscardChangesAndLeave` 里那句 `RollBackEditingCopy()` ⇒ 这条红");
                Check(DeckLibrary.ExportString(_rt.State.Deck), diskA,
                      "★ A565：……编辑器那份与**盘上 / 库里那份逐字节一致**（= 回滚过；不回滚 ⇒ 比出少一张）");

                // ---- 收尾：把那一张还回去（后面几节还用这副满编牌）----
                //  ⚠️ 用**状态层**逐字节还原（不是 `UiAddCard`）—— 补回去的位置会变，盘上就对不上 `diskA` 了
                //     （同 A363 那一节收尾的写法）。
                _rt.State.Deck.CardIds.Clear();
                _rt.State.Deck.CardIds.AddRange(snapA);
                Check(_rt.State.DeckCount, fullA, "（收尾）张数还回去");
                _rt.EscPressed();                                       // 合法 ⇒ 落盘
                CheckTrue(!_rt.DeckDirty, "（收尾）落一次盘（脏标记清掉）");
                Check(DeckLibrary.ExportString(DeckLibrary.Load().Current), diskA, "（收尾）盘上逐字节复原");
            }

            // ======== 🆕 2026-10-13（A502）：**弹窗开着时按 ESC ⇒ 什么都不做** ========
            //  判据（两条，都是原版反编译亲读；本件现读复核过）：
            //   ① ESC **只打给最上面那扇窗、没有第二跳** —— `d:/2/tools/decomp_full/WindowsManager__Update.c`：
            //      `GetKeyDown(0x1b)` → 取 `+0x58`（= `currentWindow`）→ 过 `IsOpen()`（虚表 `0x1f8`，
            //      为假直接 return）→ 调那扇窗的 `ESCPressed()`（虚表 `0x1e8`）→ **走完就 return**；
            //      而 `WindowsManager__OpenWindowCO.c` 的 `set_CurrentWindow` 写在 if/else **之外**
            //      ⇒ **弹窗一开、`currentWindow` 就是它**（旁证：`HidePopUp` 第一句比的就是这两个字段）。
            //   ② `GameWindow__ESCPressed.c` 第二道门槛 = `+0x39`（`closeOnESC`）；`PopUpGameWindow` 那两扇 prefab
            //      实读都是 **0**（`Shell/PopUpGameWindow.cs:238`）⇒ 那一刻**什么都不做**。
            //  ⇒ 原版此刻 `DeckEditingWindow.ESCPressed`（= 保存）**根本轮不到**。
            //  ⚠️ **两条硬约束**（⛔ 别一刀切）：① 「导入窗」（`closeOnESC = 1`）照旧**关得掉** ——
            //     那一级的反例断言在 A223 那一节（`_rt.UiOpenImport()` → ESC ⇒ `!ImportOpen` 且**不落盘**）；
            //     ② 判别式必须让「ESC 什么都不做」与「ESC 落到保存」**分岔** —— 只有在**卡组合法 + 脏**时才带电
            //     （不合法时两支最终都是同一扇窗、都不落盘 ⇒ 分不出，见 A364 ④ 那两条的诚实标注）。
            {
                Section("A502：丢改动窗开着时 ESC = 什么都不做（判据 = 那扇窗自己的 `closeOnEsc`）");
                CheckTrue(!_rt.ModalPopupOpen, "（前提）起手没有模态窗");
                string disk502 = DeckLibrary.ExportString(DeckLibrary.Load().Current);
                string name502 = _rt.State.Deck.Name;                // = 盘上那个名字（A399 收尾刚落过盘）
                CheckTrue(!_rt.DeckDirty, "（前提）起手**干净**");
                Check(_rt.State.Validate(), DeckError.None,
                      "（前提）这一节这副牌**合法** —— ⛔ 不合法的话两条支路最终都是同一扇窗、都不落盘 ⇒ 判别式失效");
                _rt.MarkDeckDirty();                                 // 只标脏（不动内容）⇒ `TryClose()` 才会「先问」
                CheckTrue(_rt.DeckDirty, "（前提）标脏了");
                _rt.State.Deck.Name = "A502·ESC 不该落盘";            // 🔴 **只改内存**、与盘上**不同** —— 这就是判别式
                _rt.TryClose();                                      // 脏 ⇒ 弹「丢改动」那扇（= 生产那条路，同 A399）
                var pop502 = _rt.ModalPopup;
                CheckTrue(pop502 != null, "（前提）「丢改动」窗开着（`TryClose` → `ConfirmDiscard`）");
                int leave502 = _rt.LeaveCount;
                if (pop502 != null)
                {
                    Check(pop502.MessageKey, DeckRuntime.DiscardChangesKey, "（前提）正文键 = `MenuDeck/HUD/DiscardChanges`");
                    CheckTrue(!pop502.closeOnEsc, "（前提）那扇窗 **`closeOnESC = 0`**（原版 prefab 实读 —— 下面那条的判据就是它）");
                    _rt.EscPressed();
                    CheckTrue(_rt.ModalPopupOpen,
                              "★ A502：**弹窗开着时按 ESC ⇒ 什么都不做**（原版 ESC 只打给最上面那扇窗 + 它 `closeOnESC = 0`；"
                            + "删掉 `DeckRuntime.EscPressed()` 里 ③ 那一级 ⇒ ESC 落回 `SaveAndSay()` ⇒ 这条红）");
                    Check(_rt.ModalPopup != null ? _rt.ModalPopup.MessageKey : "(窗没了)", DeckRuntime.DiscardChangesKey,
                          "★ ……而且那扇窗**没有被换内容**（同上：落到保存那一路，窗会被 `HideDeckPopUp()` 收掉/重配）");
                    CheckTrue(_rt.DeckDirty, "★ ……脏标记**还在**（落到 `CommitDeck()` 就会把它清掉）");
                    Check(_rt.LeaveCount, leave502, "★ ……也**没离场**");
                    // 🔴 **本条是真判别式**：这一节卡组是**合法**的 ⇒ 万一 ESC 落到 `SaveAndSay()`，
                    //    它会走 `CommitDeck()` 把内存里那个新名字 **写进盘** ⇒ 下面两条必红。
                    //    （A364 ④ 那两条分不出这件事 —— 那里卡组不合法，两支都不落盘，见那边的注释。）
                    Check(DeckLibrary.ExportString(DeckLibrary.Load().Current), disk502,
                          "★ ……盘上**一个字节都没动**（⛔ 这一下绝不能走到 `SaveAndSay()`）");
                    Check(DeckLibrary.Load().Current.Name, name502, "★ ……盘上那个名字还是旧的（同上，双保险）");
                    // 🔴 **「还开着时再弹 ⇒ 复用同一扇」**（原版 `ShowPopUp` 打的是字段 `popUpWindow`）——
                    //   这一条从 A364 ⑤ 接过来：那边原来是靠 ESC 重弹一次带电的，A502 之后 ESC 不再重弹
                    //   ⇒ 这里显式再走一次「脏了先问」（`TryClose` → `ConfirmDiscard` ⇒ 第二次 `ShowMessagePopUp`）。
                    //   改坏法：把 `WindowsManager.ShowMessagePopUp` 里 `StillOpen` 那一支删掉（每次都新建）⇒ 这条红。
                    int popId502 = pop502.GetInstanceID();
                    _rt.TryClose();                                      // 窗还开着 + 仍脏 ⇒ 再弹一次
                    Check(_rt.ModalPopup != null ? _rt.ModalPopup.GetInstanceID() : 0, popId502,
                          "★ 还开着时再弹 ⇒ **复用同一扇实例**（每次都新建 ⇒ 这条红）");
                    // 收尾：右钮（`Cancel`）关窗 ⇒ 名字改回盘上那个 ⇒ 再按一次 ESC（此刻**没有**弹窗）落盘收口。
                    ClickPopupButton(pop502, "ButtonRight");
                    CheckTrue(!_rt.ModalPopupOpen, "（收尾）右钮关窗");
                    CheckTrue(_rt.DeckDirty, "（收尾）窗关了、脏标记还在（原版右钮那颗回调体里只有 `HidePopUp()`）");
                }
                _rt.State.Deck.Name = name502;
                _rt.EscPressed();                                    // **没有弹窗** ⇒ 回到「ESC = 保存」
                CheckTrue(!_rt.DeckDirty, "（对照）同一个函数、**没有弹窗**时 ⇒ 真的落盘了"
                      + " —— 与上面那条合起来才说明分岔的是**弹窗状态**，不是「ESC 坏了」");
                Check(DeckLibrary.ExportString(DeckLibrary.Load().Current), disk502, "（收尾）盘上逐字节复原");
                CheckTrue(!_rt.ModalPopupOpen, "（收尾）没有留下开着的弹窗");
            }

            // 悬停 tooltip（原版触发器挂在**卡面数值容器**上，卡池里的卡是同一批 prefab）
            Tooltip.Hide(); Tooltip.FinishFade();
            var pool0 = _root.Find("pool_0");
            CheckTrue(pool0 != null, "卡池第 0 格有视图");
            if (pool0 != null)
            {
                var pv = pool0.GetComponent<CardView>();
                CheckTrue(_rt.TickTooltipAt(pv.StatWorld(CardView.StatCost)),
                          "把指针放到卡池卡的**费用**上 → tooltip 显示");
                CheckTrue(Tooltip.ShownBody == TipText.Cost, "显示的是费用那一条");
                Tooltip.FinishFade();
                CheckTrue(Tooltip.Visible && Tooltip.PanelSizePx.x > 0f,
                          $"面板量得出来（{Tooltip.PanelSizePx.x:F0}×{Tooltip.PanelSizePx.y:F0} px）"
                          + " —— 量不出来就是「先排版后激活」那个坑");
                Tooltip.Hide(); Tooltip.FinishFade();
                _rt.TickTooltipAt(pv.StatWorld(CardView.StatArmour));
                CheckTrue(!Tooltip.Visible, "**护甲上没有 tooltip**（原版那个容器就没有触发器 —— 照原版）");
                _rt.TickTooltipAt(new Vector3(0f, -4.5f, 0f));
                CheckTrue(!Tooltip.Visible, "挪到空白处 → 不显示");
            }

            // ============================================================ 🆕 2026-10-07（波 8 · Label 折行族）
            //  A205（`Label.ForceRelayout`）· A62 主表 #2/#4/#5/#6 · A77 ①⑩（第三档 + 按窗分参数）。
            //  判据全文 → `资料/普查产出_1007/波8_Label折行族.md`；⛔ 本节是**新开的一节**，上面那些断言一条没动。
            {
                // ---- A205：`SetWrapping` 之后 **mesh 真的重排了吗**（批处理没有帧循环，字段变了画面可能没变）----
                var probe = new GameObject("a205_probe");
                var plb = Label.Create(probe.transform, "", Vector3.zero, 3, Color.white,
                                       new Vector2(0.5f, 0.5f), "a205");
                CheckTrue(plb != null && plb.CanRenderChinese,
                          "（前提）A205 探针建出了**真 TMP** —— 点阵后端没有折行这回事（`WrappingMode` 恒 −1），不前置会假绿");
                if (plb != null && plb.CanRenderChinese)
                {
                    // 长到必然折：8 个词 + 80px 宽的框
                    const string Long = "AAA BBB CCC DDD EEE FFF GGG HHH";
                    plb.SetText(Long);
                    plb.SetGlyphHeight(20f / 108f);        // 20px 高的字
                    plb.SetWrapWidth(80f / 108f);          // 框 80px 宽
                    plb.ForceRelayout();                   // 传当前值 ⇒ 只推一次重排（不早退的那一步就是它）
                    int wrapped = plb.LineCount;
                    float wrappedW = plb.WorldW;
                    CheckTrue(wrapped > 1,
                              $"（前提）80px 宽的框里这句真的折了（{wrapped} 行）—— 只有它 > 1，下面那条才有鉴别力");

                    plb.SetWrapping(false);                // ← A205：这一步**自己**要把版面推下去（不是调用方补的）
                    Check(plb.WrappingMode, 0, "★ 折行字段 = **0（`NoWrap`）**");
                    Check(plb.LineCount, 1,
                          $"★ 而且**mesh 真的重排了**：行数从 {wrapped} → **1**（旧写法只改字段、不推版面 ⇒ 这里仍是 {wrapped} 行 = 红）");
                    CheckTrue(plb.WorldW > wrappedW * 1.05f,
                              $"★ 块宽跟着变宽（{wrappedW * 108f:F1}px → {plb.WorldW * 108f:F1}px；只改字段的话宽度纹丝不动）");

                    // ---- A77①：第三档 `3`（`PreserveWhitespaceNoWrap`）**能被表达**，⛔ 不许拿 `0` 顶替 ----
                    plb.SetWrappingMode(3);
                    Check(plb.WrappingMode, 3,
                          "★ 第三档 `折行=3`（`PreserveWhitespaceNoWrap`）**表达得出来** —— 旧口（`SetWrapping(bool)`）表达不了它，"
                          + "用 `false` 顶替 = 把原版的 `3` 静默降级成 `0`");
                    CheckTrue(!plb.Wrapping, "★ `Wrapping`（= 是不是 `Normal`）为**假**（3 不是 1）");
                    Check(plb.LineCount, 1, "★ 第三档也**不折行**（TMP 源码：`NoWrap` 与 `PreserveWhitespaceNoWrap` 在换行判定上同档）");
                    // 负例：认不出的档 ⇒ **出声且不设**（不静默降级成 0）
                    plb.SetWrappingMode(7);
                    Check(plb.WrappingMode, 3, "负例：传一个不存在的档（7）⇒ **不改**（只出声；旧口那种「猜一个档顶上」在这里会红）");
                }
                UnityEngine.Object.DestroyImmediate(probe);

                // ---- A62 #4 / ⑩：搜索框的字号与模式（按窗分参数：卡组编辑窗 ≠ 收藏窗）----
                // ⚠️ 写成「**没开才开**」—— `UiToggleFilters` 是**翻转**，不依赖上一节留的状态（同本文件既有的写法）。
                if (!_rt.FiltersOpen) _rt.UiToggleFilters();
                CheckTrue(_rt.FiltersOpen, "（前提）筛选抽屉开着（格子的标签才是这一轮建的）");
                var inLb = _rt.UiFilterInputLabel;
                CheckTrue(inLb != null, "（前提）搜索框那个 `Label` 在");
                CheckTrue(inLb != null && inLb.CanRenderChinese, "（前提）它是真 TMP（点阵后端读不出模式）");
                if (inLb != null && inLb.CanRenderChinese)
                {
                    Check(inLb.WrappingMode, FilterPanelModel.DeckEditInputWrap,
                          "★ 搜索框 `Text` = 原版 **`折行=3`**（`Deck Editing Menu` 的 dump 原始行；⛔ 不是 0）");
                    CheckTrue(!inLb.AutoSizing,
                              "★ 搜索框**不开自适应** —— 原版这一对搜索框 `m_enableAutoSizing = 0`"
                              + "（`min18/max72` 是不生效的残留值）；旧写法套的是**收藏窗**那对共用值（auto[18~30] ⇒ 会开）");
                    CheckNear(inLb.FontPxNow, FilterPanelModel.InputFontPxDeckEdit, 0.6f,
                              "★ 搜索框字号 = 原版 **26**（不是收藏窗那份 30）");
                }

                // ---- A62 #5：四族筛选格标签的折行（**只有费用桶折行**）----
                // 🔴 **2026-10-07（A92）：必须先【滚到底】才读得到费用桶/类型这两族** ——
                //   `RefreshFilterCells` 里那句「滚出面板的不建」（`if (b.y2 < FltY || b.y1 > FltY + FltH) continue;`）
                //   把**没进可见带**的格子连标签一起跳过（标签的登记在同一个 `continue` 之后）。
                //   本窗 `Factions()` 恒 13 ⇒ Army 行 550 高 ⇒ `CostTop 1009.02 / TypeTop 1239.02`
                //   ⇒ 滚到 0 时这两族首格的绝对 y = **1230.02 / 1470.02**，都在可见带
                //   `[FltY, FltY+FltH] = [156, 1080.1]` **外**（`$owned` 在 235 · `$rar:common` 在 975 ⇒ 那两条过得去）
                //   ⇒ 旧写法在这里拿到的**恒是 null** —— 那两条真判据（类型 `0` / 费用桶 `1`）**一次都没执行过**，
                //   所以「今天实际设成了几档」此前**没有读数**。
                //   做法 = 照本文件既有的 `UiScrollCosmetics(1e6f) → 读 → (−1e6f)` 那个形状：
                //   **滚到底读 Cost/Type → 滚回 0 读开关/Rarity**；四族的 `WrappingMode` 判据与期望值**一个字没改**。
                //   ⚠️ 自检口见 `DeckRuntime.UiScrollFilters`（**它是自检口，不是生产路径**）。
                //   🔴 **2026-10-10（A224②）订正**：上面那句「`RefreshFilterCells` 里那句早退……」**已经不成立**
                //     —— **那句早退删了**（原版全量 `Instantiate`，见 `RefreshFilterCells` 与本节 ① 那条断言）。
                //     今天 Cost/Type 两族**在滚动位 0 也建着**，只是整块滚出带口 ⇒ 那两颗标签被
                //     `SetActive(false)`（**建着、不画**，同 `MenuDraw.ClipNineChildren` 的口径）。
                //     ⚠️ **这一段仍然【先滚到底再读】**，两条理由：① 与 A92 那一轮的读数保持同一口径
                //     （**改口径就得重新验**，本轮不掺这件事）；② 带口内的标签才是「玩家真看得见的那一颗」。
                //     ⛔ 但别再拿「不滚就读不到」当理由 —— 那个理由是旧的。
                float fltMax = _rt.UiScrollFilters(1e6f);      // 滚到底（口子里按 `HandleScroll` 那份 max 夹住）
                CheckTrue(fltMax > 0f, $"（前提）筛选抽屉**滚得动**（滚到底 = {fltMax:F2}px > 0）");
                var typeLb = _rt.UiFilterCellLabel("$type:" + FilterPanelModel.TypeKeys[0]);
                CheckTrue(typeLb != null && typeLb.CanRenderChinese, "（前提）类型那族的标签在且是真 TMP");
                if (typeLb != null && typeLb.CanRenderChinese)
                {
                    Check(typeLb.WrappingMode, 0, "★ 类型族 = 原版 **`折行=0`**");
                    // 🔴 **2026-10-13（A407）**：这一族的**字号窗口上限**（原版 `'Warlord'` =
                    //   `fs 23.2 · auto[10~27] · base 36`，出处 → `FilterPanelModel.Cell.LabelAutoMax` 那条）。
                    //   量的是 TMP **自己**的 `fontSizeMax` 折回**画布 px**（`Label.FontSizeToPx`；与下面 A247
                    //   那条同一条口径）—— ⛔ **不是**读我们传进去的常量（那是自证：常量被改坏时断言跟着变、恒绿）。
                    //   改坏法：把 `Deck/DeckRuntime.cs` 那处 `SetAutoFitBox` 改回只传 4 参
                    //   ⇒ 上限 = `c.LabelPx` = **23.2**（= 标称）⇒ 这条红。
                    CheckNear(Label.FontSizeToPx(typeLb.FontSizeMax), 27f, 0.6f,
                              "★ A407：类型族自适应**上限 = 原版 27**（`auto[10~27]`）—— 拿标称 23.2 当上限 = 矮 3.8px");
                }
                var costLb = _rt.UiFilterCellLabel("$cost:" + FilterPanelModel.CostBuckets[0].Lo);
                CheckTrue(costLb != null && costLb.CanRenderChinese, "（前提）费用桶那族的标签在且是真 TMP");
                if (costLb != null && costLb.CanRenderChinese)
                    Check(costLb.WrappingMode, 1,
                          "★ 费用桶 = 原版 **`折行=1`**（四族里**唯一**折行的那一族 —— 一刀切成 false 会把它改错）");
                // 读完整回 0 —— **这不只是收尾，是后面同一宿主里所有断言的前提**（`_fltScroll` 会留在库里）。
                Check(_rt.UiScrollFilters(-1e6f), 0f,
                      "★ 读完**滚回 0**（末态滚动位 = 0；不回 0 会把后面读抽屉的断言全污染）");
                var ownedLb = _rt.UiFilterCellLabel("$owned");
                CheckTrue(ownedLb != null && ownedLb.CanRenderChinese, "（前提）`$owned` 那格的标签在且是真 TMP");
                if (ownedLb != null && ownedLb.CanRenderChinese)
                    Check(ownedLb.WrappingMode, 0, "★ 开关行 `'Owned only'` = 原版 **`折行=0`**（关着）");
                var rarLb = _rt.UiFilterCellLabel("$rar:" + FilterPanelModel.RarityKeys[0]);
                CheckTrue(rarLb != null && rarLb.CanRenderChinese, "（前提）稀有度那族的标签在且是真 TMP");
                if (rarLb != null && rarLb.CanRenderChinese)
                {
                    Check(rarLb.WrappingMode, 0,
                          "★ 稀有度 `'Common'`（= `RarityKeys[0]`，该族 **5 档同值**）= 原版 **`折行=0`** —— ⛔ 别按 `LabelCenter` 反推："
                          + "这一族是 `LabelRight`，照中心取反会**静默漏掉它**（旧口径就是这么漏的）");
                    // 🔴 **2026-10-13（A407）**：同一格的**字号窗口**（上限 27 + base 36）—— 判据与出处
                    //   → `FilterPanelModel.Cell.LabelAutoMax` / `LabelBase`（原版 `Rarity FIlter/…/Toggle/Label`
                    //   实读 `fs 23.2 · auto[10~27] · base 36`；这一族正是 A333 记的**真偏离**那两族之一）。
                    //   ⛔ 期望值取自**原版读数**（27 / 36），不是我们传进去的那个常量。
                    //   改坏法：`Deck/DeckRuntime.cs` 那处回到只传 4 参 ⇒ 上限读成 **23.2**、base 读成
                    //   「调用方那一档」（也 = 23.2：`basePx = 0` ⇒ `baseCur = cur`）⇒ **下面两条都红**。
                    CheckNear(Label.FontSizeToPx(rarLb.FontSizeMax), 27f, 0.6f,
                              "★ A407：稀有度族自适应**上限 = 原版 27**（`auto[10~27]`；旧写法 = 标称 23.2 ⇒ 矮 3.8px）");
                    CheckNear(Label.FontSizeToPx(rarLb.FontSizeBase), 36f, 0.6f,
                              "★ A407：……而且 `m_fontSizeBase` = **原版 36**（不是标称 23.2）—— 反射读的真字段"
                            + "（删掉 `SetAutoFitBox` 第 5 个实参 `c.LabelBase` ⇒ 起点退回标称 ⇒ 这条红）");
                }

                // ---- A62 #2：三个页签名牌（`Cards` 1 / `Deck info` 1 / **`Cosmetics` 0**）----
                for (int i = 0; i < 3; i++)
                {
                    var tabLb = _rt.UiTabLabelAt(i);
                    int want = i == 2 ? 0 : 1;
                    CheckTrue(tabLb != null && tabLb.CanRenderChinese, $"（前提）第 {i} 颗页签名牌是真 TMP");
                    if (tabLb != null && tabLb.CanRenderChinese)
                        Check(tabLb.WrappingMode, want,
                              $"★ 页签 {i} 的折行 = 原版 **{want}**（三颗共用同一句 `SetAutoFitBox`，"
                              + "而原版 `Cards=1 · Deck info=1 · **Cosmetics=0**` —— 只有第 3 颗要关）");
                }

                // ---- A62 #6：卡背页那颗 `'Owned only'`（原版 `折行=0`）----
                _rt.UiSetTab(2);                           // 切到 Cosmetics 页（卡背抽屉，与卡牌那栏**不是一套**）
                if (!_rt.CosmoFiltersOpen) _rt.UiToggleFilters();
                CheckTrue(_rt.CosmoFiltersOpen, "（前提）卡背页那份抽屉开着（`ToggleFilters` 按 `_tab` 分派）");
                var cosmoLb = _rt.UiCosmoFilterCellLabel("$owned");
                CheckTrue(cosmoLb != null && cosmoLb.CanRenderChinese,
                          "（前提）卡背页 `$owned` 那格的标签在且是真 TMP");
                if (cosmoLb != null && cosmoLb.CanRenderChinese)
                {
                    Check(cosmoLb.WrappingMode, 0,
                          "★ 卡背页 `'Owned only'` = 原版 **`折行=0`**（`md \"Deck Editing Menu\" --depth 18 --md`）");
                    // 🔴 **2026-10-09（A247）**：**同一颗标签的字号窗口** —— 原版是 **`auto[26~32]`**
                    //   （`Deck Editing Menu > … > Cosmetic Display > Cosmetic FIlter > Filters > Owned Toggle > Label`
                    //    = `字号 32 · auto[26~32] · 折行 0`；**收藏窗**卡背页那颗才是 `auto[18~32]`）
                    //   ⇒ 按 A77⑩「按窗分参数」，**本窗**那个调用点显式传 26。
                    //   量什么：TMP **自己**的 `fontSizeMin`/`fontSizeMax` 折回**画布 px**（`Label.FontSizeToPx`，
                    //   与 `MainMenuScene` / `RewardsScene` 那两处同族助手同一条口径）——
                    //   ⛔ **不是**读我们传进去的那个常量（那是自证：常量被改坏时断言跟着变、永远绿）。
                    //   🔴 改坏法：把 `Deck/DeckRuntime.cs` 那处 `BuildCosmetics(…)` 的第 5 个实参删掉
                    //   （退回缺省 = 共用常量 `ToggleFontAutoMin` = 18）⇒ **下界读成 18** ⇒ 下面第一条红。
                    CheckNear(Label.FontSizeToPx(cosmoLb.FontSizeMin), 26f, 0.6f,
                              "★ 卡背页 `'Owned only'` 自适应**下界 = 原版 26**（`auto[26~32]`；"
                              + "⛔ 不是收藏窗那份 18 —— 退回共用常量这里必红）");
                    CheckNear(Label.FontSizeToPx(cosmoLb.FontSizeMax), 32f, 0.6f,
                              "★ ……**上界 = 原版 32**（= `ToggleFontPx`）⇒ 窗口是 `[26,32]`，不是 `[18,32]`");
                }
                // 🔴 **2026-10-09（A247）对照**：**同一份模型的另一扇窗** —— `BuildCosmetics` 的**缺省**
                //   形参（= 收藏窗 `CollectionWindow.cs` 那一路）必须**仍是 18**。
                //   ⛔ 本批只许在卡组编辑那个调用点传 26；谁去改共用常量 `ToggleFontAutoMin`，这一条会红
                //   （收藏窗那颗原版就是 `auto[18~32]`，改它 = 把收藏窗改歪）。
                //   ⚠️ 期望值 18 **不是**从被测实现读的：它取自**原版收藏窗**那份 dump
                //   （`Collection Menu Variant` 的卡背抽屉，见 `FilterPanelModel` 那条 A247 注释）。
                //   ⚠️ `w` 与 `LabelAutoMin` 无关（那条只喂 `ToggleRowRects`）⇒ 探针给 331.73（抽屉原版宽）即可。
                {
                    var cosmoProbe = new List<FilterPanelModel.Cell>();
                    FilterPanelModel.BuildCosmetics(state.Factions(), state.Filter, 331.73f, cosmoProbe);
                    int pi = cosmoProbe.FindIndex(c => c.Key == "$owned");
                    CheckTrue(pi >= 0, "（前提）对照探针里量得到 `$owned` 那一格");
                    if (pi >= 0)
                        CheckNear(cosmoProbe[pi].LabelAutoMin, 18f, 0.01f,
                                  "★ 对照：`BuildCosmetics` 的**缺省**下界仍是 **18**（= 收藏窗那颗的原版值）"
                                  + " —— 「两扇窗不同、只改本窗」的判别点（⛔ 改共用常量会在这里红）");
                }
                // 收尾：把两栏抽屉都关回去、页签回 Cards —— **本节不许影响后面的断言**（本文件自己的纪律）。
                if (_rt.CosmoFiltersOpen) _rt.UiToggleFilters();
                _rt.UiSetTab(0);
                if (_rt.FiltersOpen) _rt.UiToggleFilters();
                Check(_rt.FiltersOpen, false, "收尾：卡牌筛选栏关回去了");
                Check(_rt.CosmoFiltersOpen, false, "收尾：卡背抽屉关回去了（两句的初态都不依赖上一节）");
            }

            // ======== 🆕 2026-10-13（A547）：导入的**落盘失败**要说得出话、而且**不许清脏标记** ========
            //  缺陷：`DeckRuntime.TryImport` 在 `Library.Add(deck)` 之后**不看落盘结果**，直接
            //  `Say("已导入「…」")` + `DeckDirty = false` ⇒ 页脚说「已导入」、脏标记被清、下次 `Done`
            //  也不会再试 ⇒ **玩家关掉编辑器就永久丢**。它正是 A503 修好的
            //  `Shell/CollectionData.ImportDeck` 的**另一半**（那边文档写着「与卡组编辑那边逐字一致」）。
            //  判据 = **同一条出口** `DeckLibrary.LastError`（⛔ 不另造一套出声机制）。
            //  🔴 **怎么验才不是自证**：把存档路径换成**必然写不进去**的地方（父目录不存在 ⇒
            //  `File.WriteAllText` 抛 `DirectoryNotFoundException` ⇒ `DeckStore.SaveAll` 回 false），
            //  再看「写盘失败」这件事**说没说得出话**；同一节里配**控制组**（同一条链、路径正常）
            //  挡住「恒报失败 / 脏标记恒留着」那种假绿。⛔ 不碰玩家的真存档（`OverridePath` 用完还回去）。
            //
            //  ⚠️ **2026-10-14（A603，只记不改）—— 一条会打脸的耦合**：本节（以及本文件**所有**
            //   「盘上没动 / 盘上是这个名字」那类断言）全靠一件事：**`DeckLibrary.Load()` 每次 `new`、
            //   没有静态缓存**（`RuleEngine/Data/DeckLibrary.cs` 的 `Load()`；判据 →
            //   `资料/普查产出_1013/WSmall1_Deck两尾巴.md` §四·3）。
            //   ⇒ ① 本节探针只动 `_rt.Library` 那一份，另一份 `Load()` 读的是**盘**（两条独立通道）；
            //     ② `TestLibraryWiring()`（本文件 `:890` 那一节）那几条「**新建立刻落盘** / **删除也落盘**」
            //        也靠它 —— 改成缓存单例，那几条就退化成**自证**（`lib2` 与 `lib` 变成同一个实例）；
            //     ③ 本节收尾那句 `_rt.CommitDeck()` 同样依赖「`_rt.Library` 与别处 `Load()` 不是同一个对象」。
            //   🔴 **谁把 `Load()` 改成单例 / 加缓存，先回来看这一节与 `TestLibraryWiring`** ——
            //   那不是性能优化，是**换语义**（`Load()` 的契约 = 「从盘重读一份」）。
            {
                Section("A547：`TryImport` 的落盘失败**说得出话**，而且**不许清脏标记**");
                string keepPath = RuleEngine.DeckStore.OverridePath;
                string probeDir = System.IO.Path.GetDirectoryName(keepPath);
                string goodPath = System.IO.Path.Combine(probeDir, "_a547_probe.json");
                string badPath = System.IO.Path.Combine(probeDir, "__wf_a547_no_such_dir__", "x.json");
                CheckTrue(!string.IsNullOrEmpty(probeDir) && System.IO.Directory.Exists(probeDir),
                          $"（前提）探针要用的目录存在（{probeDir}）—— 不存在 ⇒ 下面全是假绿");
                // 导入串走生产那条路（`UiShareString` = `DeckLibrary.ExportString(State.Deck)`）：
                // ⛔ 不自己手拼格式 —— 手拼的串一旦格式漂了，这一节验的东西会**静默变成「串不合法」**。
                string impStr = _rt.UiShareString();
                CheckTrue(impStr.Length > 0, $"（前提）拿得到一条合法的卡组串（{impStr.Length} 字符）");
                try
                {
                    // ---- ① 控制组：路径正常 ⇒ 导入成、脏标记清掉、`LastError` 空 ----
                    RuleEngine.DeckStore.OverridePath = goodPath;
                    int c0 = _rt.Library.Count;
                    _rt.UiOpenImport(); _rt.UiSetImportText(impStr);
                    CheckTrue(_rt.UiTryImport(), "（控制组）路径正常 ⇒ 导入**成功**（`TryImport` 回 true）");
                    Check(_rt.Library.Count, c0 + 1, "（控制组）……库里多了一套");
                    CheckTrue(_rt.Library.LastError == null, "（控制组）……`LastError` 是空的（真写进去了）");
                    CheckTrue(!_rt.DeckDirty,
                              "（控制组）……而且脏标记**清掉了**（刚落过盘）—— **没有这一条，"
                            + "一个「恒报失败 + 脏标记恒留着」的实现照样绿**");

                    // ---- ② 探针：写不进去的路径 ----
                    RuleEngine.DeckStore.OverridePath = badPath;
                    var lib = _rt.Library;
                    CheckTrue(!lib.Save(), "（前提）这条路径**确实写不进去**（写得进去 ⇒ 下面几条全是假绿）");
                    CheckTrue(!string.IsNullOrEmpty(lib.LastError), "（前提）……失败时**带了原因**（`LastError` 非空）");
                    CheckTrue(!_rt.DeckDirty, "（前提）起手脏标记是干净的（否则下面「留着」那条是假绿）");
                    int p0 = lib.Count;
                    var logs = new List<string>();
                    // 出声断言走本仓**现成**的那条范式（`Application.logMessageReceived`；先例
                    // `Editor/CollectionScene.cs` 的 A229 那段 —— 那里断的就是「几条警告」）。
                    // 🔴 A547：`Application.LogCallback` 的签名是 `(string condition, string stackTrace, LogType type)`
                    // —— **第 1 个才是消息正文**，第 2 个是**堆栈**（这里原来取的是第 2 个 ⇒ 下面三条
                    // `Contains` 全在比堆栈）。参数名照签名写死，免得下一个人再把第 2 个当消息用。
                    Application.LogCallback sink = (condition, stackTrace, type) => logs.Add(condition);
                    Application.logMessageReceived += sink;
                    try
                    {
                        _rt.UiOpenImport(); _rt.UiSetImportText(impStr);
                        CheckTrue(!_rt.UiTryImport(),
                                  "★ A547：写盘失败 ⇒ 导入回 **false**（退回「不看落盘结果」⇒ 这条红）");
                        // 🔴 **2026-10-14（A602）**：`TryImport` 回 `false` 有**三种**成因
                        //   （**串空** / **串不合法** / **没落盘**），而 `ImportError` 原来只回填前两种
                        //   ⇒ 调用方按它**分不出**这一种。第三种是本节的探针唯一走到的那一支。
                        CheckTrue(_rt.ImportError.Length > 0,
                                  $"★ A602：……而且 `ImportError` 也说得出话（「{_rt.ImportError}」）—— "
                                + "这一支原来**只出声、不回填** `ImportError` ⇒ 调用方拿不到原因（这条红）");
                    }
                    finally { Application.logMessageReceived -= sink; }
                    Check(lib.Count, p0 + 1, "★ A547：……而那套**真的在内存里**了（「失败」专指**没落盘**"
                          + " —— A398/A503 定过的语义：内存改动已生效、盘上没有；⛔ 不做回滚）");
                    CheckTrue(_rt.DeckDirty,
                              "★ A547：……而且**脏标记留着**（下次 `Done` 还会再试）—— 退回 `DeckDirty = false`"
                            + " ⇒ **这条红**，那正是「关掉编辑器就永久丢」的成因");
                    string spoken = string.Join("\n", logs);
                    CheckTrue(spoken.Contains("导入失败"),
                              "★ A547：……而且**出声**了（日志里有一句「导入失败」）—— 静默失败 = 红线");
                    CheckTrue(!spoken.Contains("已导入"),
                              "★ A547：……⛔ **不许**再说「已导入」—— 页脚把失败说成成功正是原来的缺陷");
                    CheckTrue(spoken.Contains(lib.LastError),
                              "★ A547：……而且报的是**真原因**（`DeckLibrary.LastError` 那句原话出现在日志里）"
                            + " —— 这一条把「随便说一句话」与「把原因说出来」分开");
                }
                finally
                {
                    RuleEngine.DeckStore.OverridePath = keepPath;          // 夹具那条路径还回去
                    try { if (System.IO.File.Exists(goodPath)) System.IO.File.Delete(goodPath); } catch { }
                    // 收尾：把脏标记与 `LastError` 收回去 —— 页头印的就是 `Library.LastError`（A330），
                    // 留着会污染后面那张 `deck_editor.png` 截图；走生产那条路（`SaveAndSay` 里那一下）。
                    _rt.CommitDeck();
                    CheckTrue(!_rt.DeckDirty && _rt.Library.LastError == null,
                              "（收尾）路径恢复之后那一下真的落得下去（脏标记清、`LastError` 空）");
                }
            }
        }

        /// <summary>比一张具名图的 px 中心/尺寸（容差 0.6px —— 坐标是我们自己换算的，不该有误差）。</summary>
        static void CheckRect(string key, float x, float y, float w, float h)
        {
            if (!_rt.UiQuadRect(key, out float cx, out float cy, out float gw, out float gh))
            { Check(true, false, $"`{key}` 没建起来（比不了 rect）"); return; }
            CheckRectPx(key, cx, cy, gw, gh, x + w * 0.5f, y + h * 0.5f, w, h);
        }

        static void CheckRectPx(string key, float cx, float cy, float gw, float gh,
                                float wx, float wy, float ww, float wh)
        {
            bool ok = Mathf.Abs(cx - wx) < 0.6f && Mathf.Abs(cy - wy) < 0.6f
                   && Mathf.Abs(gw - ww) < 0.6f && Mathf.Abs(gh - wh) < 0.6f;
            Check(ok, true, $"`{key}` 的 rect = 权威值（中心 {wx:F1},{wy:F1} 尺寸 {ww:F1}×{wh:F1}）" +
                            (ok ? "" : $" —— 实得 中心 {cx:F1},{cy:F1} 尺寸 {gw:F1}×{gh:F1}"));
        }

        static float CardViewScaleOf(int i)
        {
            var go = _root.Find("pool_" + i);
            return go == null ? -1f : go.localScale.x;
        }

        // ============================================================ 建场景

        /// <summary>自检用的临时存档路径。</summary>
        static string TempStorePath()
        {
            return System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "../Temp/_deckscene_selftest.json"));
        }

        /// <param name="lib">卡组库。**自检必须传临时路径上的库** —— 不能动玩家的真存档。</param>
        static DeckEditorState Build(DeckLibrary lib, out Transform root)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            // ⚠️ 宽高比必须在建任何东西之前定死 —— 位置/尺寸都是建的时候算一次，
            //    批处理下相机默认 4:3，建完再改 16:9 会整片错位（踩过）
            cam.aspect = LayoutSpace.DesignAspect;
            LayoutSpace.Apply(cam);

            // 🔴 2026-09-20：**建界面这件事只有一处实现** —— `DeckRuntime.Build()`（运行时程序集）。
            //    这里原来有 ~110 行绘制代码，和 `DeckRuntime` 那份是同一个东西的两个副本；
            //    两份迟早不一致（本项目反复踩过这个坑），所以改成转发。
            var rootGo = new GameObject("DeckEditor");
            var rt = rootGo.AddComponent<DeckRuntime>();
            rt.Build(lib);
            _rt = rt;
            _root = rootGo.transform;
            root = _root;
            return rt.State;
        }

        // 演示卡组 `PlayerDeckForDemo` 2026-09-20 **挪进 `DeckRuntime`**（和建界面同一处）——
        // 这里不再留第二份（两份迟早不一致）。要用就 `DeckRuntime.PlayerDeckForDemo(state)`。

        /// <summary>卡面数据 —— **转发到唯一的正路** `BattleDriver.ToCardData`。
        ///
        /// 🔴 2026-09-15 改：这里原来是**第二份 `ToCardData`**（本项目反复强调「卡面数据只有这一条路」），
        /// 而它跟正路差了三件、件件都在卡面上看得见：
        ///   · `title = c.Name` —— **丢掉中文名**（正路走 `CardText.Name(c.Name, c.NameZh)`）
        ///   · `keywords = string.Join(" · ", c.Keywords.Keys)` —— **把内部 canonical 键直接印上卡面**
        ///     （会印出 `longrange · cantattack` 这种机器味串）
        ///   · 没有 `subtype`（兵种行整条不画）、没有 `artId`（立绘取不到）、没有 `badges`
        /// ⇒ 卡组编辑器里的卡面和战斗里的**不是同一张脸**。改成转发之后两边自然一致。
        /// ⚠️ 顺带统一了阵营色（原来这里有一份**哈希取色的 `FactionColor`**，正路那份是查表的）。</summary>
        static CardData ToCardData(CardDef c) => BattleDriver.ToCardData(c, c.Faction);

        static readonly Color[] Palette =
        {
            new Color(0.72f, 0.24f, 0.22f), new Color(0.24f, 0.48f, 0.72f),
            new Color(0.30f, 0.60f, 0.34f), new Color(0.62f, 0.50f, 0.20f),
            new Color(0.52f, 0.30f, 0.66f), new Color(0.24f, 0.58f, 0.58f),
        };

        static Color FactionColor(string faction)
        {
            if (string.IsNullOrEmpty(faction)) return Palette[0];
            int h = 0;
            foreach (char ch in faction) h = (h * 31 + ch) & 0x7fffffff;
            return Palette[h % Palette.Length];
        }

        // ============================================================ 截图 / 存场景

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
            UnityEngine.Object.DestroyImmediate(tex);
            RenderTexture.ReleaseTemporary(rt);
            Debug.Log(P + $"  截图 {Path.Combine(ShotDir, file)}");
        }

        static void SaveScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            Debug.Log(P + $"  场景 {ScenePath}");
        }

        /// <summary>建出场景存盘，给人打开按 Play 用。</summary>
        /// <summary>把 `DeckEditor.unity` 加进 `EditorBuildSettings`（幂等）—— **收藏窗的「进编辑」靠它**
        /// （`SceneManager.LoadScene("DeckEditor")` 没在 Build Settings 里会抛）。照 `MainMenuScene.AddToBuildSettings`。
        /// 可以单独跑：`-executeMethod DeckScene.AddToBuild`。</summary>
        public static void AddToBuild()
        {
            const string path = "Assets/CardPresentation/Scenes/DeckEditor.unity";
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in list)
                if (s.path == path) { Debug.Log(P + "  `DeckEditor.unity` 已在 Build Settings 里"); return; }
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log(P + "  已把 `DeckEditor.unity` 加进 Build Settings（收藏窗的「进编辑」要靠它 `LoadScene`）");
        }

        public static void BuildAndSaveScene()
        {
            Directory.CreateDirectory(ShotDir);
            AddToBuild();      // 🔴 2026-09-23：`DeckEditor` **原来不在 Build Settings 里** ⇒
                               //    收藏窗「进编辑」那一步 `SceneManager.LoadScene("DeckEditor")` 会抛
            Transform root;
            // ⚠️ 这条路用**玩家的真存档**（打开场景按 Play 时就是要编辑自己的卡组）
            var state = Build(DeckLibrary.Load(), out root);
            // 🔴 2026-09-20：**场景里必须挂着 `DeckRuntime`** —— 界面现在是运行时的
            //    `DeckRuntime.Start()` 建的（编辑器里 `Build` 只是把绘制转发过去、直接调），
            //    忘了挂组件的话**按 Play 出来是一片黑**，而自检看不见这个错
            //    （它直调 `Build`，和 Play 那条路不是同一个入口 —— 本项目踩过同形的坑）。
            CheckTrue(root != null && root.GetComponent<DeckRuntime>() != null,
                      "DeckEditor.unity 的根上挂着 DeckRuntime（不然按 Play 是一片黑）");
            Shoot("deck_editor.png");
            SaveScene();
            Debug.Log(P + $"=== 场景已重建：{ScenePath}"

                        + $"（{state.PoolCount} 张卡池，演示卡组 {state.Deck.Name}）===");
        }
}
