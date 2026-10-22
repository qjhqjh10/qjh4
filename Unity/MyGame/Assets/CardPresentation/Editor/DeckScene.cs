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

        // ⚠️ **2026-10-20 删件**：这里原来还有一组卡池格子的常量
        //   （`Cols=4 / Rows=3 / CardScale=0.62` + `GridCx=975 / GridCy=520 / GridStepX=250 / GridStepY=260`）
        //   —— **已作废**：卡格区现由运行时 `DeckRuntime.Pool` 按屏宽现算（见那族 `UiPool*Now`），
        //   全文件**零引用**（现核：整词搜这 7 个名字，只命中声明那三行）⇒ 删掉。
        //   ⚠️ 它当时记的那条坑**仍然成立、仍然别踩**：卡池是 **4 列 × 250 步进**，
        //   第一版把步进写成 330、起始 x 写成 980 ⇒ 第 4 列压到筛选栏上、第 3 行跑到屏幕外（截图看出来的）。

        /// <summary>🆕 2026-10-18（第四会话）：断言计数器 + 输出口径**收口到共用件 `Editor/MenuCheck.cs`**
        /// （唯一实现处；本文件只剩同名的一行转发 ⇒ 5,534 个调用点一个字没动）。
        /// 🔴 **逐宿主一份 `CheckSink`**（⛔ 不是全局 static）—— 「拿别处的 `Check` 去断，失败会
        /// **记进别人的合计**里 ⇒ 静默」，判据见 `Editor/RewardWindowFixture.cs`。</summary>
        static readonly CheckSink _sink = new CheckSink(P)
        {
            Near = MenuNearStyle.Labeled4,       // 本文件原来是 `（实测 {got:F4} ≈ 期望 {want:F4} ± {tol:F4}）`
        };

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

        static void Section(string t) => MenuCheck.Section(_sink, t);

        static void Check<T>(T got, T want, string msg) => MenuCheck.Check(_sink, got, want, msg);

        static void CheckTrue(bool c, string msg) => MenuCheck.True(_sink, c, msg);

        /// <summary>数值比较（±`tol`）—— 用来比**原版参数**那类量（颜色系数、像素尺寸）。</summary>
        static void CheckNear(float got, float want, float tol, string msg)
            => MenuCheck.Near(_sink, got, want, tol, msg);

        // ============================================================ 🆕 2026-10-04（A24）悬停 / 状态换图的断言
        //
        // 判据 = **原 prefab 的组件字段**（`m_Transition` / `m_SpriteState` / `EverguildToggle.onSprite|offSprite`），
        // 普查正本 = `资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md`；
        // 本轮又把 5 颗的 `m_TargetGraphic` + 三张图名、7 颗的状态图名**逐字段复读了一遍**。

        /// <summary>把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
        /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
        static void CheckHoverSwap(Transform root, string what) => MenuCheck.HoverSwap(_sink, root, what);

        /// <summary>**取不到的悬停图**一张都不许有（红线：不许静默画成没反应）。</summary>
        static void CheckNoMissingSwapArt(string what) => MenuCheck.NoMissingSwapArt(_sink, what);

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

        // ============================================================ 🆕 2026-10-11（A1497④）
        // **卡池 / 通配符计数条在哪些页签上出现** —— 判据（第一权威 = 反编译方法体）
        // = `d:/2/tools/decomp_full/DeckEditingWindow__ToggleDisplay.c`：**一个入参管三件** ——
        // `cardDisplay(0x78).SetActive(param)` · `cosmeticDisplay(0x80).SetActive(param ^ 1)` ·
        // `wildcards(0xd0).SetActive(param)`；而 `DeckEditingStateController__ToggleDeckInfo.c` /
        // `__ToggleDeckList.c` 传 **1**、`__ToggleDeckCosmetic.c` 传 **0**
        // ⇒ **Cards 页与 Deck info 页都显示卡池与整条通配符计数条、只有 Cosmetics 页关**（通配符与卡池同相）。
        //
        // 🔴 **为什么下面非三页都断不可**：只断「Cosmetics 页关着」的话，**旧的 `_tab == 0` 写法照样全绿**
        //    （它 Cosmetics 页也关）—— 那是**假绿**，抓不到本次要修的那一半（Info 页被误关）。
        //    ⚠️ 本宿主里**不许用 `UiQuadActive("pool_" + i)`** 量卡池格（理由见 `PoolCellGo` 的 doc）。

        /// <summary>卡池第 `i` 屏位那格**视图根**（`DeckRuntime.RefreshPool` 里名字 = `"pool_" + vi`、父 = `Root`）。
        /// <para>🔴 **⛔ 别拿 `UiQuadActive("pool_" + i)` 量这一格** —— `CardView` 的卡面每一层都是
        /// `MeshRenderer`（`CardView.AddLayer` 建的是 `MeshFilter + MeshRenderer`，**不是** `ImageQuad`），
        /// 全树唯一的 `ImageQuad` 是 `SetTargetGem` 那条路建的 `glow_melee` / `glow_ranged`
        /// （出厂 `SetActive(false)`，而且卡池的卡**永远不**调 `SetTargetGem`）
        /// ⇒ `UiQuadActive("pool_N")` 对卡池格**恒答 false**（拿它断「露着」= **恒红**，本仓最怕的那一族假读数）。</para>
        /// <para>本格用 `activeInHierarchy` —— `RefreshPool` 里 `!on` 那一支 `SetActive(false)` 的就是这个 GO。</para></summary>
        static GameObject PoolCellGo(int i)
        {
            var root = _rt != null ? _rt.Root : _root;
            if (root == null || i < 0) return null;
            var t = root.Find("pool_" + i);
            if (t == null) t = FindDeep(root, "pool_" + i);
            return t != null ? t.gameObject : null;
        }

        /// <summary>卡池现在**真显示着**几格：数 `Root` 下 `pool_*` **直接子件**里 `activeInHierarchy` 的那些。
        /// <para>⛔ 与 `UiPoolCardAt` 是**两条腿**、**都要有**：那条读 `_poolIndex`（状态），本条读视图（画面）
        /// —— 只更新状态忘了重新 `SetActive`（或反过来）时，单看一条**照样绿**。</para>
        /// <para>⚠️ 只认 `Root` 的**直接**子件（`CardView.Create(Root, …)` 就挂在那儿）——
        /// 顺带把 `poolcnt_*`（张数条，名字前缀 `poolc`）排除掉了，它们**不**吃这条判据。</para></summary>
        static int PoolCellsShown()
        {
            var root = _rt != null ? _rt.Root : _root;
            if (root == null) return 0;
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.parent != root) continue;
                if (!t.name.StartsWith("pool_", StringComparison.Ordinal)) continue;
                if (t.gameObject.activeInHierarchy) n++;
            }
            return n;
        }

        /// <summary>通配符计数条那 **10 件**的节点名（底板 1 + 稀有度图标 4 + 数字 4 + 阵营徽记 1）——
        /// 原版父链全在**同一个 GO**（`DeckEditingWindow.wildcards` 指的 `Content Area/Header/WIldcard Counter`）
        /// 底下：`…/WIldcard Counter/{Background, Counters/*, Army Icon}` ⇒ 一件都不能漏。</summary>
        static readonly string[] WcBarNodes = {
            "hdr_wcbg", "hdr_wc0", "hdr_wc1", "hdr_wc2", "hdr_wc3",
            "hdr_wct0", "hdr_wct1", "hdr_wct2", "hdr_wct3", "hdr_army" };

        /// <summary>上面那 10 件里属于 `ImageQuad` 的 **6 件**（`hdr_wcbg` / 4 图标 / `hdr_army`）——
        /// 这 6 件 `Img()` 建的时候登记进了 `_named` ⇒ `UiQuadActive` 量得到它们。
        /// ⚠️ 另 4 件（`hdr_wct*`）是 `Label`（`TxtPx` 建）—— `_named` **只装 `ImageQuad`**
        /// ⇒ `UiQuadActive` 对它们**恒答 false**（拿它断「露着」= 恒红），只能按名字取 GO 读 `activeInHierarchy`。</summary>
        static readonly string[] WcBarQuadNodes = {
            "hdr_wcbg", "hdr_wc0", "hdr_wc1", "hdr_wc2", "hdr_wc3", "hdr_army" };

        /// <summary>按名字取节点（`Root` 直接子件优先、再深查找）看它**现在显示着没有**。
        /// 与 `UiQuadActive` 的分工：那个只认 `ImageQuad`（`Label` 答不出、答就答 false），本条两种都认。</summary>
        static bool NodeShown(string name)
        {
            var root = _rt != null ? _rt.Root : _root;
            if (root == null || string.IsNullOrEmpty(name)) return false;
            var t = root.Find(name);
            if (t == null) t = FindDeep(root, name);
            return t != null && t.gameObject.activeInHierarchy;
        }

        /// <summary>🆕 **2026-10-11（`A1514`）**：按名字取一个 `Label` 的**墨迹框**（画布 px · 左上原点）：
        /// 左沿 / 右沿 / 中心 y。量不出来（节点不在 / 不是 `Label` / 宽度是哨兵值）⇒ `false`。
        /// <para>⚠️ 量的是 **`Label.WorldW`（TMP 真测量）**，**不是**我们传进去的框、也不是节点名 ——
        /// 判「左沿落在原版那个 x 上」只能用它（同 `RowNameInkPx` 的口径）。</para></summary>
        static bool NodeInkPx(string name, out float left, out float right, out float cy)
        {
            left = right = cy = -9999f;
            var root = _rt != null ? _rt.Root : _root;
            if (root == null || string.IsNullOrEmpty(name)) return false;
            var t = root.Find(name);
            if (t == null) t = FindDeep(root, name);
            var lb = t != null ? t.GetComponent<Label>() : null;
            if (lb == null) return false;
            float w = lb.WorldW * PxPerUnit;
            if (!(w > 0.5f && w < 4000f)) return false;      // 空串 / 哨兵值 ⇒ 量不出来
            var c = DeckRuntime.PxOfWorld(lb.transform.position);
            left = c.x - w * 0.5f; right = c.x + w * 0.5f; cy = c.y;
            return true;
        }

        /// <summary>通配符计数条 10 件里现在真显示着几件（`WcBarNodes` 逐件问 `NodeShown`）。
        /// ⛔ 读的是**画面**（`activeInHierarchy`），不是 `_cardsInfoOnly` 那张表 —— 读那张表就是自证。</summary>
        static int WcBarShownCount()
        {
            int n = 0;
            foreach (var k in WcBarNodes) if (NodeShown(k)) n++;
            return n;
        }

        /// <summary>🆕 **2026-10-18（A891 的续 · 续做 A）**：筛选栏一行小标题**该印什么字**（随语档）。
        /// <para>🔴 键**逐条写死在这里**，⛔ **不从 `FilterPanelModel.TitleText` / 产物读回来** ——
        /// 那是自证（实现与检测器用同一个口 ⇒ 两边一起改回去照样绿）。键本身 = 原版那颗 `Localize.mTerm`
        /// 的原文（判据见 `Core/FilterPanelModel.cs` 的 `TitleTerm`）：
        /// `Army` → `MenuDeck/Filters/Army` · `Rarity` → `MenuDeck/HUD/Rarity` · `Type` → `MenuDeck/Filters/Type` ·
        /// `Energy Cost` → `Battle/Tips/EnergyCost`（⚠️ 最后这条是 **2026-10-18 第三轮整改** 补的，见下）。</para>
        /// <para>🔴🔴 **2026-10-18（第三轮整改 · 审查 P1）就地订正（铁律 5）**：本函数原来写着
        /// 「`Energy Cost` 与任何别的名字都返回原样（英文）—— 原版那一行没有 `Localize`（全库 3 颗）」——
        /// **那是错的**（错因：**「全库」只扫了 `bundle_menus_assets_all` 一个包**）。
        /// 复跑全部 80 个 bundle：`m_text == "Energy Cost"` 的 TMP **17 颗 / 15 个 bundle**，
        /// **每颗都挂着 `Localize.mTerm = "Battle/Tips/EnergyCost"`** ⇒ **它是正经词条**，已按下面那条 `case` 接上。</para>
        /// <para>`default` 这一支**保留**（名字对不上任何键 ⇒ 照原样印英文；⛔ 不静默编词条），
        /// 但它现在**不再覆盖 `Energy Cost`**。</para>
        /// <para>「键在表里」由调用点各自那条 `Loc.HasEntry` 钉（键不在 ⇒ `Loc.T` 返回键名本身、
        /// 渲染那一侧同样返回键名 ⇒ 只比相等会**假绿**）。</para></summary>
        static string ExpectedTitleText(string title)
        {
            switch (title)
            {
                case "Army":        return Loc.T("MenuDeck/Filters/Army");
                case "Rarity":      return Loc.T("MenuDeck/HUD/Rarity");
                case "Type":        return Loc.T("MenuDeck/Filters/Type");
                case "Energy Cost": return Loc.T("Battle/Tips/EnergyCost");   // 🔴 第三轮整改（审查 P1）
                default:            return title;                             // 名字对不上任何键 ⇒ 照原样（英文）
            }
        }

        /// <summary>筛选栏里一行小标题**渲出来的左沿**（画布 px · 左上原点 · y 向下）。
        /// 🔴 量的东西：`Label.WorldW`（TMP `textBounds` 的**真测量**）反推的左缘 ——
        /// **不是**节点位置、更**不是**「对齐枚举 == Left」（那是同义反复：把渲染那句删掉照样绿）。
        /// ⛔ 期望值由**调用方**给（取自原版读数），本函数只负责量。
        /// 量不出来（那行小标题不在 / 名字对不上 / `WorldW` 是垃圾）⇒ 返回 **−9999** ⇒ 断言必红，
        /// **不静默放过**（宽度上下界那道守卫与 `Label.HasMeasuredWidth` 同一条：TMP 在未激活 / 空串时
        /// 给的是天文数字，实测 4.29e9）。
        /// ⚠️ 节点名 = `DeckRuntime.BuildFilterFixedParts()` 的 `"flt_title_" + tl.Text.Replace(" ", "_")`
        /// ⇒ 这里**同样要把空格换成下划线**（`Energy Cost` → `flt_title_Energy_Cost`）。
        /// 🔴 **2026-10-18（A891 的续 · 续做 A）**：认节点那一步**仍用英文原名**（节点名没变），
        /// 但**比字那一步换成 <see cref="ExpectedTitleText"/>** —— 三行标题的本批起走词条（`Loc.T(键)`），
        /// 原来那句 `lb.Text != title` 会把**中文档下印中文的那颗**判成「认错节点」⇒ 整族假红。
        /// ⛔ 期望值**不从 `FilterPanelModel` 读回来**（见 `ExpectedTitleText` 的注释）。</summary>
        static float TitleLeftPx(string title)
        {
            var root = _rt != null ? _rt.Root : _root;
            var t = FindDeep(root, "flt_title_" + title.Replace(" ", "_"));
            var lb = t != null ? t.GetComponent<Label>() : null;
            if (lb == null || lb.Text != ExpectedTitleText(title)) return -9999f;   // 找不到 / 认错节点 ⇒ 必红
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
        /// `Shell/WindowsManager.cs` 的 `GameWindow.TryOpen` 那一支，与 `Shell/TrophyInfoPopup.cs` 的 `GameWindow`
        /// 的「prefab 烤着那一颗」；其余命中全在 `Editor/*Scene.cs` 的自检探针里）
        /// —— **`DeckRuntime` 不是 `GameWindow`**、也不烤任何缩放器 ⇒ 走不到那两条路。
        /// ⇒ `PosInDesignSpace` 除的是 1（`DivByScale(v,1) == v`）⇒ 与改前**逐位相同**。
        /// ⚠️ **两态断言**（本文件「筛选抽屉：模态/量渲染真值」那一节里的 `A328①` 段）把这条
        /// 「开关一开也照样是设计 px」钉住：它给 `_root` 挂一颗 `menuScale = 1.2` 的缩放器再量同一格。</para></summary>
        static bool QuadRectPx(ImageQuad q, out float x1, out float y1, out float x2, out float y2)
        {
            // 🆕 2026-10-18（A1003）：算法收口到 `MenuDraw.QuadRectPx`（保留本名与本形参 ⇒ 调用点 0 改动）。
            // 🔴 改前这一份**已经是乙式**（`ToPixel(PosInDesignSpace(...))`），只差**收口 + 读口那一行**：
            //    `MenuDraw` 那份的位置项已改走 `PixelOfDesign`（`A1004`，16:9 下与 `ToPixel` 差 ≤2.5e-4px，
            //    远小于本文件 `LayTol`）⇒ 这里一并跟上，⛔ 别再在本地写第二份算式。
            return MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2);
        }

        /// <summary>🆕 **2026-10-12（A364）**：一个**节点子树里全部活着的 `ImageQuad`** 的渲染矩形**并集**（画布 px）。
        /// 量「一棵九宫格 / 一个容器」必须用并集 —— ⛔ 只取第一块会量成**某个子块**（`ShellScene` 那条
        /// 「弹窗底量成 182×173」的注释记的正是这个坑）。`node` 自己没有 `ImageQuad`（九宫格根就是空节点）也照样能量。</summary>
        static bool UnionQuadsPx(Transform node, out float x1, out float y1, out float x2, out float y2)
        {
            // 🆕 2026-10-18（A1003）：并集算法收口到 `MenuDraw.UnionQuadRectPx`（保留本名与本形参 ⇒ 调用点 0 改动）。
            // 🔴 两项原样保留：**激活闸 = `InHierarchy`** · **`searchInactive = true`**。
            return MenuDraw.UnionQuadRectPx(node, MenuDraw.QuadGate.InHierarchy, true,
                                            out x1, out y1, out x2, out y2);
        }

        /// <summary>🆕 **2026-10-19（A1170）**：拖影卡行**在「还没拖」那一态**下的渲染矩形（画布 px）。
        /// 与 <see cref="UnionQuadsPx"/> **同一条量法**（`MenuDraw.UnionQuadRectPx`），只把**激活闸换成
        /// `Self`**：拖影卡行出厂就是 `SetActive(false)`（原版 `Deck Selector Card Info button.m_IsActive: false`）
        /// ⇒ `InHierarchy` 那一档在**起拖之前一块都量不到**，而 `Self` 只看块自己（父链关着也算）。
        /// ⇒ 「静态版面没被搬动」那条断言**起拖前就能量**、且**不依赖测试顺序**。</summary>
        static bool GhostStaticUnionPx(out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = x2 = y2 = 0f;
            var rt = _rt;
            if (rt == null || rt.UiCardGhostGo == null) return false;
            return MenuDraw.UnionQuadRectPx(rt.UiCardGhostGo.transform, MenuDraw.QuadGate.Self, true,
                                            out x1, out y1, out x2, out y2);
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

        /// <summary>🆕 **2026-10-19（B6）**：一张**卡背**在 `boxW×boxH` 的框里**画出来**该多大 ——
        /// 原版 uGUI `Image` 的**两段式**（判据与算式全文 → `Core/CardbackFace.cs`）。
        ///
        /// <para>🔴 **为什么自检里要单独写一份**：被测那一份是 `CardbackFace.Fit`
        /// （`CosmeticPreview.PreserveAspectSize` 转调它，`DeckRuntime` 的三处都走那条路）
        /// —— **直接调它就是自证**（两边一起改错照样全绿）。本函数只吃三样**外部真值**：
        /// ① **贴图资产自己的 `width/height`**（`Resources/Art/cardbacks/` 那 233 张 PNG
        /// = 原版 `textureRect` 的裁片，实测比例 **0.6188~0.7652**）；
        /// ② 原版字面量的框（**132×198** = `cosm_preview_img` 的 220×330 × `m_LocalScale` 0.6 ·
        /// **250×405** = 卡背格 · **335.31×400** = 侧栏抽屉那张）；
        /// ③ 原版那张 sprite 的 **`m_Rect` = `707×1020`**（233/233 恒定，出处 → `Core/CardbackFace.cs` 文件头）。
        /// 算式照 uGUI 原文复述：**先按 `m_Rect` 的比例定框**（`Image.PreserveSpriteAspectRatio`），
        /// **再按 `textureRect/m_Rect` 缩**（`Image.GetDrawingDimensions` 里那四个 `v`）。</para>
        ///
        /// <para>⚠️ 拿 `tex.width/height` 顶 `textureRect` 的宽高（差 ≤0.5px 的图集取整 ⇒ 尺寸上 ≤0.18px，
        /// 容差 0.5px 挡得住）；**位置**那一半（按 `padding` 偏）由 <see cref="CardbackOffsetPx"/> 另走一路。</para>
        ///
        /// <para>⛔ **别只做第一段**（「按**贴图自己**的比例内接、居中」= 2026-10-18 `A994③` 那一版，
        /// 2026-10-19 `B2` 已订正 —— 判据全文 `资料/普查产出_第八会话/B2_卡背比例两段式.md`）。</para>
        ///
        /// <para>ℹ️ 本函数是 `Editor/CollectionScene.cs` **同名函数的副本**（⛔ 不是转调它）——
        /// 四个自检宿主各自一套辅助函数是本仓的**既有明账**（`CollectionScene.RectOf` 那段记着），
        /// 而且跨宿主引别家的辅助会变成**隐式耦合**（那边实参一改、这边静默跟着变）。</para></summary>
        static Vector2 CardbackDrawnPx(Texture2D tex, float boxW, float boxH)
        {
            const float RectW = 707f, RectH = 1020f;              // 原版 sprite 的 `m_Rect`（233/233 恒定）
            if (tex == null || tex.height <= 0 || boxW <= 0f || boxH <= 0f) return new Vector2(boxW, boxH);
            // ① 定框：比的是 **`m_Rect` 的比例**，⛔ 不是贴图自己的（这就是 B2 之前漏掉的那一段）
            float spriteRatio = RectW / RectH, boxRatio = boxW / Mathf.Max(1e-6f, boxH);
            float fw, fh;
            if (spriteRatio > boxRatio) { fw = boxW; fh = boxW / spriteRatio; }   // 宽定
            else { fh = boxH; fw = boxH * spriteRatio; }                          // 高定
            // ② 画心 = 框 × (textureRect / m_Rect)（两轴系数相等 ⇒ 等比）
            return new Vector2(fw * tex.width / RectW, fh * tex.height / RectH);
        }

        /// <summary>🆕 **2026-10-19（B6）**：画心相对**框中心**的偏移，**入参那个框的单位**（本文件三处都用
        /// **画布 px**）、**y 向下**（左上原点）—— 原版 uGUI **第二段的另一半**。
        ///
        /// <para>数据 = 原版那张 sprite 的 **`padding`**（`Resources/Cardbacks.json` 的 `padL/padR/padB/padT`
        /// 四列，由 `工具/gen_cardbacks.py` 从原版 `Sprite/&lt;名&gt;_Main.json` 的 `m_Rect − textureRect` 直读）。
        /// ⚠️ **这是「原版数据」、不是「被测算法」** —— 拿被测实现算期望值才是自证；读同一份**原版数据**不算
        /// （口径先例：`CardbackDrawnPx` 吃 `tex.width/height` 也是数据）。残余风险（**明账**）：表里某一行的
        /// `padX` 若被生成器抄错，这一条抓不到（它只验「实现有没有按这四列挪」）；独立复核那四列要走
        /// `d:/2/…/Sprite/*.json`，本文件**没读**（不引外部目录依赖）。</para>
        ///
        /// <para>算式照 uGUI 原文**独立复述**：`offset = ((padL−padR)/2, (padB−padT)/2) × (框宽 / m_Rect.w)`，
        /// 而 uGUI 的 y **向上** ⇒ 本仓画布 px（y 向下）要**反号**（**这是最容易抄反的一处** ——
        /// `DeckRuntime` 三处写的是 `Pos(cx + dox, cy − doy)`；预览那处走世界单位、`dy` 直接加）。</para>
        /// 查不到该贴图（不是卡背 / 表是旧版）⇒ 返回 `false`（调用点按「前提不成立」红出来，⛔ 不静默跳过）。</summary>
        static bool CardbackOffsetPx(Texture2D tex, float boxW, float boxH, out float dx, out float dyDown)
        {
            dx = dyDown = 0f;
            if (tex == null) return false;
            float rw, rh, pl, pr, pb, pt;
            if (!CardbackTable.TrySpriteRect(tex.name, out rw, out rh, out pl, out pr, out pb, out pt)) return false;
            float spriteRatio = rw / rh, boxRatio = boxW / Mathf.Max(1e-6f, boxH);
            float fw = spriteRatio > boxRatio ? boxW : boxH * spriteRatio;
            float k = fw / rw;                                    // == fh / rh
            dx = (pl - pr) * 0.5f * k;
            dyDown = -(pb - pt) * 0.5f * k;                       // uGUI 向上为正 ⇒ 本仓向下为正 = 反号
            return true;
        }

        /// <summary>🆕 **2026-10-17（B25①）**：某棵子树里第一颗 `WindowButton`（= 「这颗钮接了点击没有」）。
        /// ⚠️ **必须 `GetComponentInChildren`**：顶栏那几颗的挂载点在**子节点**上
        /// （`SettingsBtn/Image` · `InboxBtn/Image` · `Avatar Item Small/Border`），
        /// 在钮节点自己身上 `GetComponent` 会拿到 null ⇒ 那是**假红**（同 `Editor/MainMenuScene.cs` 那处订正）。
        /// ⚠️ 另外**只有 `Challenge button` 那一颗挂在节点本身**（它就是那个 quad）。</summary>
        static WindowButton HitOf(Transform node)
        {
            return node != null ? node.GetComponentInChildren<WindowButton>(true) : null;
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
        /// 节点名 = `"flt_title_" + 空格换下划线`（同 `TitleLeftPx` 那句）。
        /// 🔴 **2026-10-18（A891 的续 · 续做 A）**：比字那一步改用 <see cref="ExpectedTitleText"/>（随语档）。</summary>
        static Label FilterTitleLabel(string title)
        {
            var root = _rt != null ? _rt.Root : _root;
            var t = FindDeep(root, "flt_title_" + title.Replace(" ", "_"));
            var lb = t != null ? t.GetComponent<Label>() : null;
            return lb != null && lb.Text == ExpectedTitleText(title) ? lb : null;   // ⛔ 认错节点 = 当没找到（不静默放过）
        }

        /// <summary>🆕 **2026-10-11（A289）**：一段文字**渲染网格**的外接矩形（画布 px · 左上原点 · y 向下）。
        /// <para>🔴 **为什么不能拿 `transform.position` / `Label.WorldW/H` 代替**：`MenuDraw.ClipTextNow` 那一刀
        /// 只改 **TMP 网格的顶点**（夹到框边上 + 按同一仿射改 uv）—— **节点位置与 `WorldW/H` 一个字都不动**
        /// ⇒ 只有量**顶点**才看得出「有没有被裁住」（同 `ShellScene.TextMeshWidthPx` 那条的口径；
        /// 那边只要宽度，这里四条边都量 —— 本条的判据在**上沿**）。</para>
        /// <para>量的是 `textInfo`（TMP 的**模型**数组）—— 与 `MenuDraw.ClipTmpMesh` 写进去的是**同一份**；
        /// ⚠️ 上传那一半（`MeshFilter.sharedMesh`）在批处理下可能被跳过（`MenuDraw.TextClipUploadSkipped`），
        /// 那**不影响**这里读到的数（那一刀确实写进了模型数组）。量不出来（没字形 / `textInfo` 空）⇒ false。</para>
        /// <para>🆕 **2026-10-16（A844 · 跨文件那一半）**：本函数**只剩名字** —— 实现全部转调
        /// `ShellScene.TmpSpanPx(Label, …, includeInactive: true)`（那条量法**全仓唯一一份**实现）。
        /// 🔴 **读数逐位不变**（所以⛔**不是** A490 说的「量法一变」）：本函数原来的内层与那一份**逐句相同**
        /// —— 同一个 `isVisible` 过滤、同一个 `materialReferenceIndex` 取槽、同一套越界检查，
        /// 换算也是同一条式子（`DeckRuntime.PxOfWorld` = `world.x*108+960` / `540−world.y*108`，
        /// 与那一份走的 `LayoutSpace.ToPixel` 逐位同值 —— 两个常量各自写着 `1080/DesignHeight` 与 `1920/1080`）。
        /// ⚠️ 唯一那处**形参差别**就是 `includeInactive`：本函数历来取组件带 `(true)`（含 inactive，
        /// 判据是「抽屉里那些被 `ClipCellToBand` 关掉的格子也要量」）⇒ 收口时**原样传 `true`**，
        /// ⛔ 别顺手改成默认（那会把「关着的格子」整批变成量不到 = 假绿）。</para></summary>
        static bool TextMeshRectPx(Label lb, out float x1, out float y1, out float x2, out float y2)
        {
            return ShellScene.TmpSpanPx(lb, out x1, out y1, out x2, out y2, includeInactive: true);
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

        // ============================================================ 🆕 2026-10-21（A1527 / A1525 的断言看门）
        //
        // 名单上这一批（`Deck/DeckRuntime.cs` 里刚做完、此前**一条断言都没有**）：
        //   ① `A1527` 督军行**不印费用圆** —— `SetOn(_deckRowCost[i], on && !heroRow)`
        //      与 `SetOn(_deckRowCostText[i], on && !heroRow)`（**两颗一起**，本工程它们是两颗独立物体；
        //      ⚠️ `_deckRowCostText` 原名 `_deckRowCount` —— `A2007` 纯改名，它盛的是**费用**）；
        //   ② `A1525` 卡组列表三件 —— 按 `Id` 去重 / **费用 → 本地化名（当前文化比较）**排序 / 行名左对齐到 **68**。
        // 判据（⛔ 一律**不是我们自己的常量**）：
        //   · **行名左沿 68** = 参考图（`桌面/warpforge参考图/卡组编辑界面参考.png`）量出来的 ——
        //     参考图 px 56…61 × 1.21442（= 1920/1581）= 画布 **68.0…74.1**；
        //     ⛔ 不许写 `DeckRuntime.RowNameX`（那是被测实现里的数，改坏它断言跟着变 = 自证）；
        //   · 督军行那两颗的判据 = `资料/说明书/04_界面UI/菜单全树.md:9321-9329`（Hero 族**没有** `Cost Image`）
        //     与 `:9337` / `:9348`（Defensive / Card Info 两族**都有**）
        //     + `UICardInfoItem__Initialize.c:76`（值闸 `Coin >= 0`）；
        //   · 排序键 = **照原版：费用 → 本地化名的【当前文化】比较 → Id**（督军 / 防御卡钉在最前）。
        //     🔴 **2026-10-18（铁律 5 就地订正）**：这里原来写「原版 `Reorder` 第二把键的方法体本地读不到、
        //     只能拿参考图反推」—— **不成立**：两个 lambda 体在
        //     `d:/2/tools/decomp_full/DeckListDrawer.__c___Reorder_b__22_{0,1}.c`。现读 =
        //     键1 `*(… + 0x78)`（`RawCardScript.manaCost`）、键2 `RawCardScript__GetLocalizedCardName`；
        //     且 `OrderBy/ThenBy` **没传 `IComparer`** ⇒ 第二把键 = `Comparer<string>.Default`
        //     = **`StringComparison.CurrentCulture`**（⛔ **不是** `CompareOrdinal` —— 码点序是旧实现的一条偏离，
        //     用户 2026-10-18 拍板「照原版」）。记录 → `资料/普查产出_1018/R_卡组排序_照原版.md`
        //     与 `DeckRuntime.DeckEntries()` 的 doc。
        //     🔴 **2026-10-19（`A1929` · 铁律 5）把那句「参考图**三组**」的来路查清了**：
        //     旧注释里的三组（费用 2 `游/驭` · 费用 3 `女/帷/突` · 费用 4 `努/灵/狂/邪`）**取自参考图那副牌**，
        //     不是本节实际跑的演示牌 —— 2026-10-19 现读 `C:/Users/qjh36/Desktop/warpforge参考图/卡组编辑界面参考.png`：
        //     那副牌 = **「铺场流」**（督军 **安维尔·凯尔托克** · SaimHann · **30/30**），费用 2/3/4 三档逐行是
        //     `游侠 / 驭风者摩托`（**2** 张）· `女妖面具 / 帷幕奔行者 / 突击蝎`（**3** 张）·
        //     `努阿杜'火心' / 灵活机动 / 狂嚎女妖 / 邪术风暴`（**4** 张，每张 `x2`）—— **与旧注释的分组逐张吻合** ⇒ 来源坐实。
        //     而本节实际跑的 `DeckRuntime.PlayerDeckForDemo` 是**另一副**（只填单位、不填策略卡）：
        //     费用 2/3/4 只有 **1/2/3** 张不同卡（旧账记的「3 vs 2」「4 vs 3」即由此来 —— **不是抄错，是两副牌**）。
        //     ⇒ **组内张数本来就对不上**，⛔ 别拿参考图的组数当期望值；第 ⑥ 段现在是**逐段现算**、不依赖任一副牌的名字。

        // ============================================================ 🔴 2026-10-21（`A1145`）
        // **「自证结构」断言 —— 普查（本文件逐族扫过一遍；这一段是【结果】，不是待办）。**
        //
        // 家族定义（判据只有一条）：**期望值 / 判据取自【被测实现自己】** ⇒ 把实现和它的判据一起改回旧写法，
        //   断言照样全绿。三种形状：① 期望值 = 同一个函数/常量的输出；② 断「我们建了几颗节点」（我们的实现细节）；
        //   ③ 断「刚写进去的东西读得回来」（同一个口进出）。
        // ⛔ 反例（**不算**这一族）：期望值取自**另一条腿**（模型 / 参考图 / 原版 prefab 字面量），
        //   哪怕那条腿是同一批人写的 —— 那是「判据共用一份」，是铁律要的，不是自证。
        //
        // 逐条去向：
        //   ✅ **`Terms.Count == 0`**（`PopUpGameWindow.Terms` 那一节）：**2026-10-19（`A1012`）已收口**
        //      —— 今天那里剩下的两条 `Terms.Count` 是「**前提**」（那张表还是空的）与「**收尾**」（把自检塞的
        //      假词条删掉），而它们服务的 ③-c 那对（**选键不看 `Terms` / 取字先看 `Terms`**）是
        //      「同一个实现不可能同时满足」的**灭自证结构** ⇒ 已不是「拿我们自己的空表证明我们自己的函数」。
        //   ✅ **`hdr_army` 那一族**：期望值原本取自 `FactionIcon` **自己**的输出 ⇒ 只证「视图用了那条映射」、
        //      证不了映射本身。**本轮补了一条独立腿**（卡池里出现过的每个阵营：图名非空 / 不落 `default` /
        //      两两不同），见那一节里的 `A1145` 注释块。
        //   ✅ **`MenuDeckErrorKey` 那一族**（原 `numKey5` 那两处）：**2026-10-17（`T08`）已收掉自证的那一半** ——
        //      ③-a / ③-c 的期望值现在是**字面量** `"MenuDeck/Error/InvalidDeck"`（不再拿 `numKey5` 当期望值）；
        //      `numKey5` 只剩**一处**、且只是「**真前提**」（`!Loc.HasEntry(numKey5)` = 数字键今天不在表里）。
        //      那四个号（`MenuDeckErrorNumber`）仍由紧邻那六条 **字面量 2/5/5/4/4/4** 钉住 ⇒ ⛔ 不假装它不存在。
        //   ✅ 其余扫到的期望值来源（`PxOfWorld` 作**量尺** · `FilterPanelModel.*` · `CardText.*` ·
        //      `DeckRules.*` · `CardArt.*` · 纯字面量）：都是另一条腿，**不属这一族**。
        //   ⚠️ **本件没扫的**：① `DeckScene.cs` 之外别的自检宿主；② **结构冒烟**那类断言
        //      （「`name_bg`/`hdr_sep`/`tab_hi0` 建起来了」、节点名 / 层数）—— 它们**故意**钉实现细节，
        //      换实现就该红，定位是**工具**、不是判据，**不属这一族也不该删**。

        /// <summary>卡组行名（`row_n{i}`）**渲出来的墨迹左右缘**（画布 px · 左上原点）。
        /// <para>量的东西：`Label.WorldW`（TMP `textBounds` 的**真测量**，不是 `sizeDelta`）± 节点世界 x ——
        /// `RefreshBounds` 把 TMP 子节点摆在 `−anchor.x·WorldW − bounds.min.x`（`Battle/Label.cs` 里那一句）
        /// ⇒ 墨迹左缘恒 = 节点 x − `WorldW/2`，与本文件既有的 `TitleLeftPx` **同一条算式、同一份量纲**。</para>
        /// <para>量不出来（节点不在 / 没量出宽度 —— 空串与「没生成过版面」都答 4.29e9 那个哨兵）⇒ 返回 **false**，
        /// 调用点**必红**（不静默放过）。</para></summary>
        static bool RowNameInkPx(int i, out float left, out float right)
        {
            left = right = -9999f;
            var root = _rt != null ? _rt.Root : _root;
            var t = FindDeep(root, "row_n" + i);
            var lb = t != null ? t.GetComponent<Label>() : null;
            if (lb == null) return false;
            float w = lb.WorldW * PxPerUnit;
            if (!(w > 10f && w < 2000f)) return false;      // 空串 / 哨兵值 ⇒ 量不出来
            float cx = DeckRuntime.PxOfWorld(lb.transform.position).x;
            left = cx - w * 0.5f; right = cx + w * 0.5f;
            return true;
        }

        /// <summary>断言用：显示名的**当前文化**比较 —— 就是原版 `OrderBy/ThenBy` 没传 comparer 时
        /// 落到的那把键（`Comparer&lt;string&gt;.Default` = `string.CompareTo` = `CurrentCulture`）。
        /// <para>🔴 **它本身不是「独立腿」**（与实现同一个 API）—— 独立性来自 ⑥ 里与
        /// `CompareOrdinal` 的**对照**：同一费用段里找一对「两把键方向相反」的相邻名字，
        /// 实现若用回码点序，那一对必然不按文化序排（断言红）。⛔ 那一条**不是恒真**：
        /// 找不出这种对时它会**明说无鉴别力**（本机 zh-CN 下这副牌确实找得出）。</para>
        /// <para>⚠️ 名字取自 `CardText.Name(c)`（显示口径本身是规格的一部分）。</para></summary>
        static int NameCultureCmp(string a, string b)
        {
            // 2026-10-18：改走**生产那一份**比较器（`DeckRuntime.CompareDisplayName`），
            // ⛔ 不再在自检里另写一份 `string.Compare` —— 两份迟早不一致，而且判别式要量的正是生产那把键。
            return DeckRuntime.CompareDisplayName(a, b);
        }

        // ============================================================ 自检

        // ==================================================================
        //  🆕 2026-10-17（`A1212` 块 1）：**「实参 = 原版四格」那 15 处**的断言。
        //
        //  判据 = 原版 MB 的四个字段原文（`m_fontSizeMin` / `m_fontSizeMax` / `m_fontSizeBase` /
        //  `m_TextWrappingMode`），逐颗现读自 `bundle_menus_assets_all` —— 与
        //  `资料/普查产出_第十一会话/RA1212_切块表.md` 块 1 那 15 行逐值相同（那份表是 2026-10-10 快照，
        //  这里**按节点名现取**，⛔ 不按行号）。
        //  ⛔ 期望值**不许**读 `DeckRuntime` 里的常量 / 注释表（那是自证）。
        //  🔴 「折行 ≠ 1」那一半（`A1212` 要求逐块核 `SetWrapping` 有没有漏）：
        //   本块 15 处里 `0` 档 9 处、`3` 档 1 处（`name_t`）—— 接线侧已逐处 `SetWrappingMode(0/3)`，
        //   这里**逐颗读回**（`Label.WrappingMode` = TMP 的原文），漏一处就红。
        // ==================================================================

        /// <summary>`A1212` 的公用断言之「一颗字的四格 + 折行档」（判据见上面那一段）。</summary>
        static void CheckA1212(Transform root, string name, string origNode,
                               float mn, float mx, float bs, int wrap,
                               float boxW = -1f, float boxH = -1f)
        {
            var t = FindDeep(root, name);
            // 兜底：`DeckRuntime` 的视图不一定都挂在传进来的那棵根上 —— 两棵树都搜，⛔ 不猜是哪一棵。
            // （原来这一句举的例子是导入弹窗那几颗 `imp_*`；那棵树 2026-10-17 已按 T23 删件删掉，
            //  兜底本身对别的件仍然成立，故留着。）
            if (t == null && _rt != null && _rt.Root != null) t = FindDeep(_rt.Root, name);
            var lb = t != null ? t.GetComponent<Label>() : null;
            CheckTrue(lb != null, $"（前提）`A1212` `{name}` 建出来了（原版 `{origNode}`）");
            if (lb == null) return;
            string tag = $"`{name}`（原版 `{origNode}`）";
            CheckTrue(lb.AutoSizing, $"★ A1212 {tag}：**`m_enableAutoSizing = 1`**（自适应开着）");
            // ⚠️ 用**实例口** `FontSizeToPxNow(…, ActualFace)`：`SetAutoFitBox` 写进去的
            //   `fontSizeMin = cur × minPx / NominalPx()` 在 px 口径下恒等于原版字段原文（逐位），
            //   静态 `FontSizeToPx` 只在 `Face.Cjk` + 世界档那一档才同源。
            CheckNear(lb.FontSizeToPxNow(lb.FontSizeMin, lb.ActualFace), mn, 0.05f,
                      $"★ A1212 {tag}：下界 = 原版 `m_fontSizeMin` {mn}px");
            CheckNear(lb.FontSizeToPxNow(lb.FontSizeMax, lb.ActualFace), mx, 0.05f,
                      $"★ A1212 {tag}：上界 = 原版 `m_fontSizeMax` {mx}px");
            CheckNear(lb.FontSizeToPxNow(lb.FontSizeBase, lb.ActualFace), bs, 0.05f,
                      $"★ A1212 {tag}：二分起点 = 原版 `m_fontSizeBase` {bs}px"
                    + "（🧨 只开自适应、不给 base ⇒ 这一格红）");
            Check(lb.WrappingMode, wrap,
                  $"★ A1212 {tag}：折行档 = 原版 `m_TextWrappingMode` {wrap}"
                + "（`SetAutoFitBox` 内部会无条件开 `Normal(1)` ⇒ 这一格抓「接完忘了还原」）");
            var tmp = t.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp == null) { CheckTrue(false, $"（前提）A1212 {tag}：走 TMP 后端（点阵后端没有框/四格）"); return; }
            // 框：`SetWrapWidth` / `SetAutoFitBox` 写的是 TMP 子节点的 `sizeDelta`（本档单位）
            float k = lb.IsUgui ? 1f : (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight);
            if (boxW > 0f)
                CheckNear(Mathf.Abs(tmp.rectTransform.sizeDelta.x) * k, boxW, 0.05f,
                          $"★ A1212/A1226 {tag}：框宽 = 原版有效框 {boxW}px");
            if (boxH > 0f)
                CheckNear(Mathf.Abs(tmp.rectTransform.sizeDelta.y) * k, boxH, 0.05f,
                          $"★ A1212/A1226 {tag}：框高 = {boxH}px");
        }

        /// <summary>`A1212` 块 1（`DeckRuntime`）—— 逐颗对原版四格。
        /// 🔴 **2026-10-17（T23 删件）**：导入弹窗那 4 处（`imp_title/imp_input/imp_err/imp_ok_t`）随那棵树删掉。</summary>
        static void TestA1212()
        {
            Section("★ A1212 块 1：DeckRuntime 那批「实参 = 原版四格」");
            var r = _root;
            CheckTrue(r != null, "（前提）`_root` 在");
            if (r == null) return;

            CheckA1212(r, "hdr_back_t", "Deck Editing Menu/Content Area/Header/Close/Button Text", 10f, 40f, 12f, 0,
                       boxW: 133.22f);                       // A1226 表 B #35（框宽已改）
            CheckA1212(r, "hdr_fltlbl", "…/Sidebar/Filters/Filters", 10f, 40f, 32f, 0);
            // 🔴 **2026-10-22（`D1`）就地订正（铁律 5）**：`A1226` 表 B #37 的框宽**旧值 221.44 已改 68.69**。
            //    221.44 是拿 dump 的**模板位父宽 250** 算的（250 × 0.92575 − `m_SizeDelta.x 10`）；
            //    那颗钮布局完之后的父宽是 **85**（`Header/Filters` 的 `GridLayoutGroup` cellSize 85×70）
            //    ⇒ 原版有效框 = 85 × 0.92575 − 10 = **68.69**。⛔ 别再按 250 算。
            CheckA1212(r, "hdr_clear_t", "…/Header/Filters/…/Button Text", 10f, 42f, 12f, 0,
                       boxW: 68.69f);                       // A1226 表 B #37（框宽已按布局后父宽订正）
            for (int i = 0; i < 4; i++)
                CheckA1212(r, "hdr_wct" + i, "…/Header/Wildcard Counter/Counter", 10f, 38f, 45f, 1);
            CheckA1212(r, "name_t", "…/Sidebar/Deck Details/Name (Text)", 10f, 28f, 28f, 3);
            CheckA1212(r, "name_h", "…/Sidebar/Deck Details/Name (Hint)", 10f, 28f, 24f, 0);
            // 🔴 **2026-10-17（`A1226` 表 B #41/#52）：这两颗**原版没有固定框** —— 判据 = RM 自己那两列
            //   两种口径的对照：`m_SizeDelta` = **0 × 0**（`HorizontalLayoutGroup` 驱动 ⇒ 序列化里根本没有框）、
            //   「原版有效框」= **111.52 × 47.90** 是**布局组算出来的**（父格 × 锚跨度），随原版抽屉的格宽/行高走。
            //   ⇒ 与 `A1226` 其余站**不可比**：照 111.52 抄宽 = 把原版抽屉的布局结果抄成我们的固定框。
            //   本批的口径 = **断我们实际用的框 = 我们自己的父格**（`RowNameW 190 × RowH 55.7` /
            //   `CardGhostW − 70 = 217.9 × 55.7`），把「用的就是格、不是原版那个数」钉住；
            //   ⛔ 不为凑 `A1226` 的差值改几何（铁律 3 的反面：布局组驱动的件没有可抄的原版框）。
            CheckA1212(r, "row_n0", "…/Deck List drawer/…/Text fill/Card Name", 2f, 38f, 27.69f, 0,
                       boxW: 190f, boxH: 55.7f);             // A1226 表 B #41（不可比：原版 111.52 = 布局组算的）
            CheckA1212(r, "row_cnt0", "…/Deck List drawer/…/Cost Image/Cost", 18f, 50f, 32f, 1);
            CheckA1212(r, "foot_done_t", "…/Sidebar/Footer/Done/Button Text", 10f, 40f, 12f, 0,
                       boxW: 168.54f);                       // A1226 表 B #46
            CheckA1212(r, "card_ghost_n", "Card Drag Controller/…/Card Name", 2f, 38f, 27.69f, 0,
                       boxW: 217.9f, boxH: 55.7f);           // A1226 表 B #52（不可比：原版 111.52 = 布局组算的）
            CheckA1212(r, "card_ghost_c", "Card Drag Controller/…/Cost Image/Cost", 18f, 50f, 32f, 1);
            // 🔴 **2026-10-17（T23 删件）**：这里原来还有导入弹窗那 4 颗（`imp_title` / `imp_input` /
            //   `imp_err` / `imp_ok_t`，判据 = `Import Deck Popup/…`）—— 那棵树整棵删了
            //   （生产那一份是 `Shell/ImportDeckPopup.cs`，A891 那批词条断言在 `CollectionScene` 侧）。
        }

        public static void Run()
        {
            // 🔴 **2026-10-19（`A1534` 尾巴 · 铁律 11）**：批处理自检**不出声** —— `WFSoundPlayer.Enabled = false`
            //   时那个开关自己的契约是「**只记账不发声**：不建对象、不出声，但 `Played` 照样涨」
            //   ⇒「音效调度算对了没有」那一族断言**照旧有效**，而这一趟**不白建一堆 `AudioSource`、不白播**。
            //   先例 = `WarpforgeArena1/Editor/AnimFXCheck.cs`（同一个开关、同一个 `finally` 还原）。
            //   ⚠️ 它是**全局静态开关** ⇒ 必须在 `finally` 里还原；`RunCore` 本体保持原样（只换了名字）。
            bool prevSoundEnabled = WarpforgeVFX.WFSoundPlayer.Enabled;
            WarpforgeVFX.WFSoundPlayer.Enabled = false;
            try { RunCore(); }
            finally { WarpforgeVFX.WFSoundPlayer.Enabled = prevSoundEnabled; }
        }

        /// <summary>`Run()` 的**本体**（🆕 `A1534` 尾巴：`Run` 只负责「关音效 → 调它 → 还原」这一圈）。
        /// ⚠️ 入口仍是 `Run`（`-executeMethod DeckScene.Run` 一个字没变）。</summary>
        static void RunCore()
        {
            _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
            Directory.CreateDirectory(ShotDir);
            // 🆕 2026-10-18（`A1007`）：空图护栏的**基线**（结尾那条断言用它算增量，见收尾那一段）。
            int guardBase = MenuCheck.GuardedShots;
            Debug.Log(P + "=== 卡组编辑自检 开始 ===");

            Section("状态：卡池与筛选");
            TestFilters();
            TestWarlordGatedPool();

            // 🆕 2026-10-18（卡池排序 + 天赋卡显隐）：原版 4 个稳定 `OrderBy` 与 `showInCollectionOptions` 显示闸
            Section("状态：卡池排序与显示判据（原版 `DeckEditingWindow.GetCardCollection`）");
            TestPoolSortAndVisibility();

            Section("状态：加牌 / 删牌 / 督军");
            TestEditing();

            Section("状态：费用曲线与滚动窗口");
            TestCurveAndScroll();

            Section("状态：遭遇模式（模式**从正在编辑的那副卡组派生**）");
            TestSkirmishMode();

            // 🔴 **`A1896`（2026-10-19）**：打开编辑器时逐张摘除不合法卡（原版唯一调用点
            // `DeckEditingWindow__TryOpen.c:47`）；与 `BattleScene` 那条「开局不摘」成对。
            Section("状态：打开编辑器时逐张摘除不合法卡（`A1896`）");
            TestLoadDeckPrunesInvalidCards();

            Section("版面");
            var tmpPath = TempStorePath();
            RuleEngine.DeckStore.OverridePath = tmpPath;
            RuleEngine.DeckStore.DeleteFile();
            DeckEditorState state;
            try
            {
                // 🔴 **2026-10-22（`W62`）**：这一拍 `Build` 的前提是**库空、存档不存在** ——
                //   那正是本节要验的**新不变式**（原版 `SelectDecksTab__CreateDeck.c:17,26,28` =
                //   `CardDeck.GetEmptyDeck()` → 打模式 → 直接开 `DeckEditingWindow`，**全链无入库调用**）。
                //   ⛔ 别在这之前预建卡组：预建了，「空库时不写盘」那几条就没有鉴别力了。
                //   夹具在 `TestLibraryWiring()`（断空库那一拍）**之后**才建，见 `TestLibraryAfterDone()`。
                state = Build(DeckLibrary.Load(), out _root);
                // 🔴 2026-09-20：**根上必须挂着 `DeckRuntime`** —— 场景存盘后按 Play 就是靠它的
                //    `Start()` 建界面；忘了挂组件 = Play 出来一片黑，而这条断言能挡住。
                //    （铁律 10 第 5 条：`Build` 直调与 Play 的 `Start()` 是两个入口，**各配一条断言**。）
                CheckTrue(_root != null && _root.GetComponent<DeckRuntime>() != null,
                          "根对象上挂着 DeckRuntime（按 Play 的入口）");
                // ★ **2026-10-22（`W62`/`W63`）顺序有讲究 —— 这一块【必须在 `TestLayout` 之前】**：
                //   `TestLibraryWiring()` 断的是「**空库那一拍**」⇒ **必须赶在夹具建库之前**；
                //   `TestLibraryAfterDone()` 则负责显式建夹具那套并落盘（走生产那条 `Done` →
                //   `Library.Insert` 的路，= 原版 `UploadUnsyncedDeck`）。
                //   🔴 **2026-10-22（`W63`）挪位（铁律 5，原文留痕）**：这两次调用原来排在
                //   `TestLayout(state)` **之后** —— 那样 A363 那一节（在 `TestLayout` 里）先按
                //   `EscPressed()` 落盘、库里变成 1 套 ⇒ 本块「空库 0 套 / 一个字节都没写」那一族
                //   至少 5 条红；而 `DeckRuntime.Build` 已按原版删掉「空库替玩家建一套」那个副作用
                //   ⇒ A363 读 `stale.Current` 时直接 NRE。⇒ **整块上移到 `TestLayout` 之前**。
                //   ⛔ 别把这两次调用对调，也别让生产代码替夹具建库。
                TestLibraryWiring();
                TestLibraryAfterDone();
                // 🆕 2026-10-17（`A1212` 块 1）：15 处「实参 = 原版四格」的断言（必须在 `TestLayout` 之后 ——
                //   `_root` 是 `Build` 出来的那棵树；本组只读，不建任何东西）。
                //   🔴 **2026-10-22（`W63`）**：`TestLayout` 现在排在本块**之后**（见上）——
                //   「`TestA1212` 在 `TestLayout` 之后」这条既有顺序要求**没有被破坏**。
                TestLayout(state);
                TestA1212();
                // 🆕 **2026-10-20**：**开局那一套的坐标** —— 收尾复位要用（见本 `try` 末了那一节）。
                //   必须在这里取：**再往下就会被自检改掉** —— `UiCommitName("自检·改的名")` 那一节
                //   真改名并落盘 ⇒ 等到收尾再想读「原来叫什么 / 原来是第几套」已经读不到了。
                //   🔴 **2026-10-17（T23 删件）**：原来这里还举了「两处 `UiTryImport()` 往库里加副本」——
                //   导入弹窗删了，那两条路没有了；这条复位仍然需要（改名那一条还在）。
                //   🔴 **2026-10-22（`W62`）挪位**：这三行原来紧跟 `Build` —— 现在必须挪到
                //   `TestLibraryAfterDone()` **之后**：空库那一拍库里是 0 套，开局那套是**夹具**建的。
                string origDeckName = state.Deck != null ? state.Deck.Name : null;
                int origLibIndex = _rt.Library.CurrentIndex;
                int origLibCount = _rt.Library.Count;
                // 🔴 **2026-10-17（N16 撤回，铁律 5）**：原版 `RankedEventWindow ◂ Deck info` 那棵树属于
                //   **V1 旧窗**；本窗实现的是**现役 V2**（V2 prefab 没有 `deckInfo` 字段），而且原版事件类
                //   只有 `RankedV2Event` / `RankedV3Event`（无 V1 事件）⇒ 往 V2 里加那块会盖住三栏、且是死件。
                //   ⇒ 已撤回；判据与结论 → `资料/普查产出_1017/UI/U3_弹窗与小界面族.md` + 界面线总表 N16 行。
                // 🔴 **2026-10-22（`W62`）**：这里原来的 `TestLibraryWiring();` 已上移（见上面那一段）——
                //   它断言的是**空库那一拍**，必须赶在 `TestLibraryAfterDone()` 建夹具之前跑。
                // 🆕 2026-10-17：**拖拽那两条链**（卡背预览 / 松手投递 /「拖 vs 滚动」阈值）
                //    —— 必须在 `TestLayout` 之后（它才把 `_rt` 建出来）
                Section("拖拽：卡背预览 / 松手投递 / 「拖 vs 滚动」");
                TestCosmeticDrag();
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
                    // 🆕 2026-10-18（A855）：**离场那条路的回程意图** —— 批处理不切场景 ⇒ 这是唯一可观测面
                    //    （正因如此，意图的写入在 `Application.isBatchMode` 那道闸【之前】，同 `LeaveCount`）。
                    // 🔴 **2026-10-14（`A1132` 第 4 步）前提补记（⛔ 期望值一个字没动）**：下面的 `hrt` 是
                    //    `new GameObject` + `AddComponent<DeckRuntime>()` 建的、**没有宿主窗** ⇒
                    //    它走的是 `BackToMenu` 的**【独立场景】那条腿**（`HostWindow == null` ⇒
                    //    照旧写意图 + 批处理早退，**一个字未动**）。⇒ **这 5 条今天钉的是那条兜底腿**。
                    //    🔴 **另一条腿（有宿主窗 ⇒ 关窗、不切场景、意图当场消费）的断言在**
                    //    `TestLayout` 里「A1132 第 4 步」那一节 —— 两条腿**结构上不可能同时满足**，
                    //    只留一条、或把这条老腿顺手删掉，那一条必红（不是同义反复）。
                    Check((int)CardPresentation.CollectionData.PendingReturn.Source,
                          (int)CardPresentation.DeckExitSource.Collection,
                          "★ A855：离场写下了**回程意图**（来源 = 收藏）—— 回到主菜单据此重开收藏窗的卡组页。"
                          + "改坏法：把它挪到 `isBatchMode` 闸**之后** / 整句删掉 ⇒ 本文件这条红");
                    Check((int)CardPresentation.CollectionData.PendingReturn.Tab,
                          (int)CardPresentation.WindowTabType.CollectionDecks,
                          "★ A855：…而且**落点是卡组页**（`CollectionDecks` —— 不靠 `Open()` 那句 `Click(0)` 的巧合）");
                    // 🔴 **判别式（防「两边一起改回去」那种双向改错）**：入口已经写过来源 ⇒ 离场**不许覆盖**它。
                    //    若 `BackToMenu` 写成「无条件写 Collection」，「按来路回」就退化成「一律回收藏」——
                    //    那一版**上面两条照样全绿**，只有这一条会红。
                    CardPresentation.CollectionData.SetReturnIntent(
                        CardPresentation.DeckExitSource.LiveOpsEvent, CardPresentation.WindowTabType.None);
                    hrt.BackToMenu();
                    Check((int)CardPresentation.CollectionData.PendingReturn.Source,
                          (int)CardPresentation.DeckExitSource.LiveOpsEvent,
                          "★★ A855 判别式：入口先写了来路（事件窗那条路）⇒ 离场**不覆盖**它"
                          + "（覆盖了 = 「按来路回」是假的）");
                    // 读完即清（别污染本节后面的用例 —— 同上面 `PendingEditDeck` 那条「读完就清」的口径）
                    //   🔴 **2026-10-18 修（`DeckScene` 11 条红里的 R2）**：这条原来断 `ri.Source == None` ——
                    //     **断错了**。`TakePendingReturn()` 的契约是「**先取后清、返回取到的那份**」
                    //     （`Shell/CollectionData.cs`；同族先例 `PrebuiltDecks.cs` ·
                    //      `BattleDriver.cs`；而**生产代码 `MainMenuRuntime.cs` 正是靠这个返回值开窗**）
                    //     ⇒ 取回 `LiveOpsEvent(=2)` **才是对的**。正确的断法是**两条**：取回的是刚写的那份 + **静态口被清空**。
                    var ri = CardPresentation.CollectionData.TakePendingReturn();
                    Check((int)ri.Source, (int)CardPresentation.DeckExitSource.LiveOpsEvent,
                          "★ A855：取回的就是刚才写下的那份（`Take*` = 先取后清、返回取到的那份）");
                    Check((int)CardPresentation.CollectionData.PendingReturn.Source,
                          (int)CardPresentation.DeckExitSource.None,
                          "★ A855：回程意图**读完就清**（清的是静态口 `PendingReturn` —— 不清的话下次进主菜单还会再落一次）");
                    UnityEngine.Object.DestroyImmediate(hgo);
                }

                // 🔴 **2026-10-12（`A1697`）：「**新建卡组不入库**」那条链**（用户 2026-10-12 点名
                //    「没选战将的卡组…这按照原版复刻」）。判据（原版，逐行读过，全文 →
                //    `资料/普查产出_第二十三会话/R1_没选战将的卡组.md` §三）：
                //      · `SelectDecksTab__CreateDeck.c:17,26,28` = `CardDeck__GetEmptyDeck()` → 打模式
                //        → **直接 `WindowsManager__OpenWindow<DeckEditingWindow>(...)`**；全链**一个入库调用都没有**；
                //      · 入库的唯一路径 = `DeckEditingWindow__TrySaveDeck.c:100 UploadDeck`
                //        （先 `:80 ValidateDeck`，不合法 `:81-98` 弹错误窗后 `return`）；
                //      · `DeckUtility__ValidateDeck.c:88` 那句 `*param_2 = 2`（**没督军 = 错码 2 = 不合法**）就是它挡下来的。
                //    ⚠️ 原版不是靠灰按钮（`DeckEditingWindow__UpdateDoneButton.c:22` 只切高亮/音效）。
                //
                //    ⛔ 本节用自己那一份 `DeckLibrary.Load()`（同上面 `lib2`/`lib3` 两节的写法）
                //      —— **不碰 `_rt.Library`**（碰了会把库里多出一套带给后面所有节）。
                {
                    var libN = DeckLibrary.Load();
                    int beforeN = libN.Count;
                    string nmN = CardPresentation.CollectionData.CreateDeck(0);
                    Check(libN.Count, beforeN,
                          "★ A1697：`CollectionData.CreateDeck` **不入库**（库里套数不变）"
                        + " —— 改坏法：改回 `Lib.Create(...)` ⇒ 这条红");
                    CheckTrue(!string.IsNullOrEmpty(nmN), $"……而且照旧回一个名字（「{nmN}」）");
                    CheckTrue(CardPresentation.CollectionData.PendingNewDeck != null,
                              "★ A1697：交接的是**那份未入库的新卡组**（`PendingNewDeck`）"
                            + " —— 改坏法：只回名字、不写这个对象 ⇒ 红");
                    Check(CardPresentation.CollectionData.PendingEditDeck, -1,
                          "……而且**不交接旧下标**（两个交接口互斥：留着旧下标会让编辑器跳进**别的**卡组）");

                    var ngo = new GameObject("NewDeckHandoffSmoke");
                    var nrt = ngo.AddComponent<DeckRuntime>();
                    nrt.Build(libN);                       // 开局读掉交接
                    CheckTrue(CardPresentation.CollectionData.PendingNewDeck == null,
                              "交接**读完就清**（不清的话下次开还会再打开这一份）");
                    CheckTrue(nrt.State != null && nrt.State.Deck != null, "编辑器把那一份装进来了");
                    Check(nrt.State.Deck != null ? nrt.State.Deck.Name : "(null)", nmN,
                          "★ A1697：编辑器开局打开的就是**刚建的那一副**（不是库里当前选中那一套）"
                        + " —— 改坏法：`Build` 不读 `PendingNewDeck`（照旧落 `Library.Current`）⇒ 这条红");
                    Check(nrt.State.Deck != null ? nrt.State.Deck.CardIds.Count : -1, 0,
                          "★ ……而且是**空牌**（原版 `CardDeck__GetEmptyDeck.c:28-32`：督军/防御/模式都是 0）");
                    Check(libN.Count, beforeN, "……此刻库里仍然一套都没多（**Done 之前不入库**）");

                    // ① 没选战将 ⇒ `Done` 不合法 ⇒ **不入库**（= 用户报的那个症状的正面）
                    nrt.UiPressDone();
                    Check(libN.Count, beforeN,
                          "★ A1697：**没选战将就按 `Done` ⇒ 不入库**（原版 `ValidateDeck` 错码 2 ⇒ `return`，"
                        + "不走 `UploadDeck`）—— 改坏法：`CommitDeck` 不判 `_newDeck`、直接 `CommitCurrent` ⇒ 这条红");
                    CheckTrue(nrt.ModalPopupOpen, "★ ……而且照原版弹了那扇错误窗（`__TrySaveDeck.c:81-98`）");
                    ClickPopupButton(nrt.ModalPopup, "ButtonRight");     // 右钮 = 只 `HidePopUp()`（同 A364 节的收尾写法）
                    CheckTrue(!nrt.ModalPopupOpen, "（收尾）把误弹出来的那扇收掉（⛔ 别留给后面的节）");

                    // ② 改到一半退出 ⇒ **不留任何痕迹**
                    nrt.BackToMenu();
                    Check(libN.Count, beforeN,
                          "★ A1697：**改到一半退出 ⇒ 库里一套都没多**（原版没入库 ⇒ 本来就不该留）");
                    // ⛔ 防呆：`BackToMenu()` 会写下「回程意图」（上面那一节自己读完就清，这里照做）
                    CardPresentation.CollectionData.TakePendingReturn();

                    // ③ 合法 ⇒ `Done` **才**入库（原版 `__TrySaveDeck.c:80 → :100`）
                    var legalN = DeckRuntime.PlayerDeckForDemo(nrt.State);   // 演示卡组 = 合法满编 30 张
                    CheckTrue(legalN != null && legalN.CardIds.Count > 0,
                              $"（前提）夹具攒出了一副合法牌（{legalN?.CardIds.Count} 张）");
                    if (legalN != null)
                    {
                        nrt.State.Deck.WarlordId = legalN.WarlordId;
                        nrt.State.Deck.DefensiveId = legalN.DefensiveId;
                        nrt.State.Deck.CardIds.Clear();
                        nrt.State.Deck.CardIds.AddRange(legalN.CardIds);
                        nrt.UiPressDone();
                        Check(libN.Count, beforeN + 1,
                              "★ A1697：**合法（有督军 + 满编）⇒ `Done` 把它入库**（原版 `__TrySaveDeck.c:100 UploadDeck`）");
                        Check(libN.Count > 0 ? libN.Decks[libN.Count - 1].Name : "(null)", nmN,
                              "★ ……而且入库的就是这一副（名字对得上）");
                        CheckTrue(!nrt.DeckDirty, "★ ……且落盘成功 ⇒ 脏标记清了");
                        // ⛔ 别省这一步：`_newDeck` 不翻篇 ⇒ 第二次 `Done` **又插一遍**（库里两套同名、静默）
                        nrt.UiPressDone();
                        Check(libN.Count, beforeN + 1,
                              "★ A1697（判别式）：**再按一次 `Done` 不会重复入库**（`_newDeck` 在成功那一刻已翻篇，"
                            + "转而走 `CommitCurrent`）—— 改坏法：`CommitDeck` 里不清 `_newDeck` ⇒ 红");
                    }
                    UnityEngine.Object.DestroyImmediate(ngo);
                    CardPresentation.CollectionData.PendingNewDeck = null;   // ⛔ 防呆：别把未消费的交接留给后面的节
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

                // ============================================================
                //  🆕 2026-10-20：**收尾复位 —— 把「当前卡组」从自检自己造的那套还回开局那套**
                //
                //  🔴 **为什么非有这一节**：上面几节会**真的改当前卡组** ——
                //    `_rt.UiCommitName("自检·改的名")` 那一节**真改名**、按 Done 落盘（改名落盘在库里）。
                //    🔴 **2026-10-17（T23 删件）**：这里原来还举了「两处 `UiTryImport()` 往库里加副本、
                //    把编辑器切过去」—— 导入弹窗删了，那两条路没有了；`extra` 那个删除循环留着兜底。
                //    ⇒ 不还回去，`Shoot("deck_editor.png")` 那张**收尾截图**的页头上印的就是自检的名字
                //      （截图是给人当面看的 ⇒ 印着「自检·改的名 2 3」直接误导）。
                //
                //  🔴 **为什么写在 `finally` 【之前】**（⛔ **不能**挪到下面 `Shoot` 那两行前面）：
                //    `finally` 会把 `DeckStore.OverridePath` 还原成 `null`，那之后
                //    `Library.Delete()` / `CommitCurrent()` / `Save()` 落的是**玩家的真存档**
                //    （`RuleEngine/Data/DeckStore.cs`：`Path` = `OverridePath ?? persistentDataPath`）
                //    ⇒ 在 `finally` 之后删，**会毁掉玩家真卡组**。这里 `OverridePath` 还指着临时存档。
                //
                //  ⚠️ 复位只动**「当前是哪一套」+ 名字**：卡组**内容**（张数 / 督军 / 防御卡）由上面各节
                //    自己的「（收尾）」负责还，本节不重复做。
                //  ⚠️ 名字只能写回**开头抓下来的那一份**（`origDeckName`）—— 库里那一串在自检期间
                //    已经被改成「自检·改的名」**并落了盘**，收尾时读库读不回真名。
                // ============================================================
                {
                    Section("收尾复位：当前卡组还回开局那套（收尾截图里页头印的就是它）");
                    var lib = _rt.Library;
                    int extra = lib.Count - origLibCount;
                    for (int i = lib.Count - 1; i >= origLibCount; i--) lib.Delete(i);
                    // 🔴 **2026-10-17（T23 删件）**：自检里已经**没有**往库里加副本的节了（导入弹窗那一节删了）
                    //   ⇒ `extra` 今天恒 0；这个循环留着**兜底**（将来谁再加一节会加副本的，这里自动收拾）。
                    Check(lib.Count, origLibCount,
                          "★ 库回到开局那 " + origLibCount + " 套（自检期间多出来的 " + extra + " 套已删）");
                    lib.Select(origLibIndex);
                    _rt.State.LoadDeck(lib.Current);        // ⚠️ A397：装的是**副本**
                    _rt.State.Deck.Name = origDeckName;     // 名字被自检改过 ⇒ 就地写回
                    CheckTrue(_rt.CommitDeck(), "★ 把开局那个名字写回库里并落盘");
                    _rt.RefreshAll();                       // 批处理没有帧循环 ⇒ 视图要显式跟上（页头那行字）
                    Check(_rt.State.Deck.Name, origDeckName,
                          "★ ……复位之后编辑的这套名字 = 开局那套「" + origDeckName + "」"
                        + "（不复位 ⇒ `Shoot` 出去那张 `deck_editor.png` 页头印的是自检的名字）");
                    CheckTrue(!_rt.DeckDirty, "★ ……而且脏标记是干净的（复位这一下真的落了盘）");
                }
            }   // ← 这个大括号关的是 `try`（本节的复位必须留在 `try` 里，理由见上面那段）

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

            // ============================================================
            //  🆕 2026-10-18（`A1007`）：`MenuCheck.Shoot` 的 `Camera.main == null` 那一支
            //    —— **判别式探针**（自备 `CheckSink`，⛔ 不污染本宿主的合计）。
            //
            //  🔴 **为什么必须单开一条**：那一支原来是**裸 `return`**（不报错、也不写图）
            //     ⇒ 整条截图断言链可以「靠什么都没发生」假装通过。只把 `return` 改成出声**还不够**：
            //     那条路平时**走不到**（本宿主的相机是好的）⇒ 不造局面就永远验不了它。
            //     这里把 `MainCamera` 标签临时摘掉 + 关掉相机，**造出无相机上下文**再调 `Shoot`。
            //
            //  🧨 **灭自证**：`MenuCheck.Shoot` 那一支改回裸 `return` ⇒ 探针读到 **0 条 ✗** ⇒ 红；
            //     要让它重新变绿只能**连这条断言一起删/改**（那是看得见的改动）。
            //     「把实现改回去」与「把断言改回去」在结构上不可能同时满足。
            // ============================================================
            {
                var taggedCams = new List<Camera>();
                foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(
                                     FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (c.CompareTag("MainCamera")) { taggedCams.Add(c); c.tag = "Untagged"; c.enabled = false; }

                bool noCam = Camera.main == null;
                CheckTrue(taggedCams.Count > 0,
                          $"（前提）本宿主建场时挂了 `MainCamera`（摘到 {taggedCams.Count} 台）"
                        + " —— 一台都摘不到 ⇒ 下一条验不了");
                CheckTrue(noCam,
                          "（前提）摘标签 + 关相机 ⇒ `Camera.main == null`（**下一条的前提**，不是结论）"
                        + "｜⚠️ 本条若红 = 这台 Unity 的 `Camera.main` 不吃「摘标签/关相机」，"
                        + "先查这个再怀疑下面那条");

                if (noCam)
                {
                    string probeDir = Path.Combine(ShotDir, "A1007_无相机");
                    Directory.CreateDirectory(probeDir);
                    const string probeName = "no_camera.png";
                    string probeFull = Path.Combine(probeDir, probeName);
                    if (File.Exists(probeFull)) File.Delete(probeFull);

                    // ⚠️ 前缀特意与本宿主不同：本探针**故意**要让 `Shoot` 吼一条 `✗`，
                    //    那一行的前缀是 `>>> A1007 探针 `（**不是** `DK `）⇒
                    //    ⛔ 别把它当成「卡组编辑自检失败了一条」；本宿主的合计**不受影响**
                    //    （`probeSink` 是独立实例，`_sink` 一格没动）。
                    var probeSink = new CheckSink(">>> A1007 探针 ");
                    MenuCheck.Shoot(probeSink, probeDir, probeName,
                                    guardBlank: true, meanBrightness: MeanBrightness);
                    Check(probeSink.Fail, 1,
                          $"★ **无相机 ⇒ `MenuCheck.Shoot` 必须【出声】**：自己的 sink 上**恰好 1 条 ✗**"
                        + $"（实得 {probeSink.Fail}）"
                        + "｜🧨 把 `Editor/MenuCheck.cs` 里那一支改回裸 `return` ⇒ 这里 0 ⇒ 本条红"
                        + "（改前就正是裸 `return`：不报错、也不写图）");
                    Check(probeSink.Pass, 0,
                          "★ …而**一条 ✓ 都不许记**（没拍成 ≠ 通过 —— 连 `✓ … 不是空图` 那一类也不许有）");
                    CheckTrue(!File.Exists(probeFull),
                              "★ …而且**一张图都没落盘**（`cam == null` 时照旧不写图：那半句行为**没改**）");
                    if (probeSink.Failures.Count > 0)
                        Debug.Log(P + "  （探针子 sink 那条 ✗ 的原文）" + probeSink.Failures[0]);
                }

                foreach (var c in taggedCams) { c.tag = "MainCamera"; c.enabled = true; }
                CheckTrue(Camera.main != null,
                          "（收尾）相机还回去 ⇒ `Camera.main` 又有了（⛔ 别让本块废掉后面的截图）");
            }

            // ---- 🆕 `A1007`：本宿主的**空图护栏**（`guardBlank: true`）真的接上了没有 ----
            //  判据 = `MenuCheck.GuardedShots` 的增量（那是**唯一**一处真的走亮度那一跳的地方）。
            //  ⛔ 别拿「截图文件在盘上」当判据 —— 护栏关着的时候图**也照写**（那正是改前的形状）。
            //  🧨 把本文件 `Shoot` 的转发改回缺省（`guardBlank: false`）⇒ 增量 0 ⇒ 红。
            CheckTrue(MenuCheck.GuardedShots > guardBase,
                      $"★ `A1007` 本宿主的**空图护栏真的跑过**（开局 {guardBase} → 现 {MenuCheck.GuardedShots}）"
                    + " —— 改前本文件**没有**护栏（`guardBlank: false`）⇒ 这里恒为 0");

            // ============================================================ 🆕 2026-10-09（`A1025`）本批键全 `Loc.HasEntry` + 两语档取真文案
            // 判据（坑表 #18）：**「原版有词条」≠「我们表里有键」** —— 键不在 ⇒ `Loc.T` 返回**键名本身**、
            //   界面上就印 `MenuDeck/…` 这种键名（比印英文更难看、也更不容易被当成缺陷）。
            // 🔴 **与 `A1057(f)`（`Editor/NetSelfTest.cs` 那张**表级**扫描）不是同一条、方向相反**：
            //   那条是 **表 → 表**（表自身健康：条数 / EN 列无 CJK / 非空），本条是 **代码 → 表**
            //   （**本批代码引的键**有没有落进表）—— 表级扫描**永远看不见**「某文件引了键 X 而 X 不在表里」。
            // 数组 = 本批生产文件（`Deck/DeckRuntime.cs` · `Deck/DeckEditorState.cs` · `Core/FilterPanelModel.cs`）
            //   里出现的**活键字面量** ∩ `Core/Loc.cs` 的表键（超集无害、且更严）。
            // ⛔ 排除项（写了必红/空转）：① 前缀常量 `MenuDeck/Error/` 与格式串 `MenuDeck/Error/{0}`（不是词条键）；
            //   ② `MenuDeck/HUD/DefaultCardback`（**只在注释里**、`A1042③` 已裁定**不建**）。
            // ✅ **2026-10-09 就地订正（铁律 5）**：原来这里写着「`MenuDeck/Share/ExportSuccesful` 是一条
            //   『代码引了、表里没有』的**真现象**」—— **已作废**：第九会话（`A1153`）已把这条键**建进 `Loc` 表**
            //   （值 = **带 `{0}` 的模板**；调用点 `Shell/DeckInfoPopup.cs` 走 `Loc.HasEntry` 两态 + `string.Format`）。
            //   🔴 **2026-10-17 就地订正（`N35`）**：上一行的「带 `{0}` 的模板」**已过期** ——
            //   `N35` 按官方原文改成了 **无 `{0}` 的整句**（官方 `Deck copied to clipboard` 本身不带占位符），
            //   调用点也改回 `Loc.T(key)`（不再 `string.Format(..., s.Length)`）。
            //   ⛔ **它仍不进本数组** —— 本数组只收**本批生产文件**（`Deck/*.cs` + `Core/FilterPanelModel.cs`）里的活键，
            //   而它用在 `Shell/DeckInfoPopup.cs`。
            // 🧨 改坏法：① `Loc.cs` 删掉本批任一条键 ⇒ ①红（且 ② 也会红：`Loc.T(k) == k`）；
            //   ② 某条**英文列**填成中文/全角空格 ⇒ ③红；③ **中文列**清空 ⇒ ②红；④ 值改成键名本身 ⇒ ②红；
            //   ⑤ 表删掉一半 ⇒ ④红（`EntryCount` 掉到基线之下）；⑥「把实现与期望一起改回写死中文 **并** 把表里那条删掉」
            //   ⇒ ①红（数组里那条键仍在、`HasEntry` 假）—— **这正是本数组存在的唯一理由**。
            {
                string[] a1025Keys =
                {
                    "Battle/Tips/EnergyCost", "Card_Race/Warlord",
                    "Card_Rarity/Common", "Card_Rarity/Epic", "Card_Rarity/Legendary",
                    "Card_Rarity/Rare", "Card_Rarity/Special",
                    "MainMenu/General/Confirm", "MainMenu/MainButtons/ButtonLabel/Back",
                    "MenuCollection/NoCardsFound",
                    // 🔴 **2026-10-22（`W62`）如实注**：`MenuDeck/DefaultDeckName`（「我的卡组」）是**原版键，
                    //   但今天没有生产消费点** —— 它唯一的消费点（`DeckRuntime.Build` 空库时
                    //   `Library.Create(Loc.T(它))` 给那套起名）已随 W62 删掉（原版那一拍根本不入库）。
                    //   ⛔ 词条与本条断言都**留着**：A1025 钉的是「表里有这条键」，不是「有人在用」。
                    "MenuDeck/DefaultDeckName", "MenuDeck/DemoDeckName",
                    "MenuDeck/Error/ImportBadString", "MenuDeck/Error/ImportEmpty",
                    "MenuDeck/Error/ImportNotPersisted", "MenuDeck/Error/InvalidDeck",
                    "MenuDeck/Error/SaveFailed",
                    "MenuDeck/Filters/Army", "MenuDeck/Filters/ClearFilters", "MenuDeck/Filters/Filters",
                    "MenuDeck/Filters/ShowOwnedOnly", "MenuDeck/Filters/ShowUpgradableOnly",
                    "MenuDeck/Filters/Type",
                    "MenuDeck/HUD/DeckDescription/DeckInfo", "MenuDeck/HUD/DeckDescription/Minions",
                    "MenuDeck/HUD/DeckDescription/Spells", "MenuDeck/HUD/DiscardChanges",
                    "MenuDeck/HUD/DragCardsTip", "MenuDeck/HUD/EditDeckName", "MenuDeck/HUD/EnterText",
                    "MenuDeck/HUD/Rarity", "MenuDeck/HUD/SearchFilter",
                    "MenuDeck/MenuButtons/Done", "MenuDeck/NewDeckName", "MenuDeck/Share/PasteDeck",
                    "MenuShop/ShopItemType/Cards", "MenuShop/ShopItemType/Cosmetics",
                };
                foreach (var k in a1025Keys)
                    CheckTrue(Loc.HasEntry(k), $"★（A1025）本批键都在表里：`{k}`");
                // ② 两语档各取一次、都非空且 ≠ 键名（只断中文档 = 半边绿）
                var a1025LangWas = Loc.Current;
                foreach (var lang in new[] { AvailableLanguages.Chinese, AvailableLanguages.English })
                {
                    Loc.RestoreForTest(lang);
                    int bad = 0; string firstBad = null;
                    foreach (var k in a1025Keys)
                    {
                        string v = Loc.T(k);
                        if (string.IsNullOrEmpty(v) || v == k) { bad++; if (firstBad == null) firstBad = k; }
                    }
                    CheckTrue(bad == 0,
                              $"★（A1025）`{lang}` 档下 {a1025Keys.Length} 条**全部取到真文案**（缺 {bad} 条"
                            + (firstBad == null ? "" : $"，第一条 `{firstBad}`") + "）"
                            + " —— 取不到时会印**键名本身**，那就是静默失败");
                }
                Loc.RestoreForTest(a1025LangWas);      // ⛔ `RestoreForTest`（只改内存、不写 `PlayerPrefs`），不是 `SetLanguage`
                // ③ 灭自证 C1：英文列不许含汉字（`Loc.HasCjk` 的区间含 `0x3000-0x303F` 与 `0xFF00-0xFFEF`）
                {
                    int cjk = 0; string firstCjk = null;
                    foreach (var k in a1025Keys)
                    {
                        string en = Loc.EnOf(k);
                        if (!string.IsNullOrEmpty(en) && Loc.HasCjk(en)) { cjk++; if (firstCjk == null) firstCjk = k; }
                    }
                    CheckTrue(cjk == 0, $"★（A1025）本批 {a1025Keys.Length} 条的**英文列无 CJK**（坏 {cjk} 条"
                                        + (firstCjk == null ? "" : $"，第一条 `{firstCjk}`") + "）");
                }
                // ④ 灭自证 D（表基线）：开工前实测 `EntryCount == 429`
                CheckTrue(Loc.EntryCount >= 429,
                          $"★（A1025）表基线：`Loc.EntryCount` = {Loc.EntryCount} ≥ **429**（开工前实测）"
                        + "｜🧨 把键删掉、断言也一起删 ⇒ 这条红");
            }

            // ============================================================ 🆕 2026-10-17（界面线 T08 尾巴 · `A1852`）
            //  `MenuDeck/CantImportDeck` 的 `{0}` = **这副卡组督军的阵营名**（已查实）——
            //  判据 = `d:/2/tools/decomp_full/DeckInfoPopup__EditDeck.c`：
            //    `s = string.Format(GetTranslation("MenuDeck/CantImportDeck"),
            //                       GameStaticData.CardArmyToString(deck.Warlord.Army));`
            //  ⇒ 两列填官方原文（EN/ZH），调用点唯一拼装 = `Shell/DeckInfoPopup.CantImportDeckMessage`。
            //  ⛔ 期望值盯**官方文案字面量**，不是盯我们自己新写的常量（否则是自证）。
            {
                const string kImp = "MenuDeck/CantImportDeck";
                CheckTrue(Loc.HasEntry(kImp), $"★（A1852）`{kImp}` 在表里（不在 ⇒ `Loc.T` 返回键名 = 界面印键名）");
                Check(Loc.EnOf(kImp), "Can't import deck. You don't have enough {0} cards.",
                      "★（A1852）EN 列 = 官方原文（带 `{0}`）—— 判据 `Resources/official_loc_13lang.json`");
                Check(Loc.ZhOf(kImp), "无法导入卡组。你没有足够的{0}张牌。",
                      "★（A1852）ZH 列 = 官方原文（带 `{0}`）");
                // 拼出来的串：**含阵营名**、**不含督军卡名**（改前那版在后面拼了 `（<wl.Name>）`）
                var poolImp = CardDatabase.Load();
                var wlImp = CardDatabase.FindById(poolImp, "ASH83");      // Autarch · SaimHann（卡表实读）
                CheckTrue(wlImp != null, "（前提）探针督军 `ASH83` 在卡库里");
                if (wlImp != null)
                {
                    string armyImp = CardText.Faction(wlImp.Faction);
                    string msgImp = DeckInfoPopup.CantImportDeckMessage(wlImp);
                    CheckTrue(!string.IsNullOrEmpty(armyImp) && msgImp.Contains(armyImp),
                              $"★（A1852）拼出来的串**含阵营名** `{armyImp}`（`{{0}}` 的语义 = 督军阵营名）"
                            + "｜🧨 把 `CantImportDeckMessage` 改回拼 `wl.Name` ⇒ 本条红");
                    CheckTrue(!msgImp.Contains(wlImp.Name),
                              $"★（A1852）……且**不含督军卡名** `{wlImp.Name}`（改前那版在后面拼 `（<wl.Name>）`）");
                    // 逐语档各取一次：**两种语言下都**含该语档的阵营名（只断一档 = 半边绿）
                    var langWasImp = Loc.Current;
                    foreach (var lang in new[] { AvailableLanguages.Chinese, AvailableLanguages.English })
                    {
                        Loc.RestoreForTest(lang);
                        string armyL = CardText.Faction(wlImp.Faction);
                        string msgL = DeckInfoPopup.CantImportDeckMessage(wlImp);
                        CheckTrue(!string.IsNullOrEmpty(armyL) && msgL.Contains(armyL) && !msgL.Contains("{0}"),
                                  $"★（A1852）`{lang}` 档：拼出来的串含阵营名 `{armyL}`、且**不残留 `{{0}}`**（`{msgL}`）");
                    }
                    Loc.RestoreForTest(langWasImp);      // ⛔ `RestoreForTest`（只改内存、不写 `PlayerPrefs`）
                }
            }

            int total = _sink.Pass + _sink.Fail;
            if (_sink.Fail == 0) Debug.Log(P + $"=== 结束：{_sink.Pass}/{total} 全过 ✅ ===");
            else
            {
                var sb = new System.Text.StringBuilder(P + $"=== 结束：{_sink.Pass}/{total} 通过，**{_sink.Fail} 条失败** ❌ ===");
                foreach (var f in _sink.Failures) sb.Append("\n").Append(P).Append("   ✗ ").Append(f);
                Debug.LogError(sb.ToString());
            }
            if (Application.isBatchMode) EditorApplication.Exit(_sink.Fail == 0 ? 0 : 1);
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
            //   🔴 **2026-10-18（卡池排序与显隐）改口径 —— 走的是两条不同的判据，别混**：
            //     · 帝王之子那 6 张药剂 / 破坏卡：`showInCollectionOptions = 0` ⇒ 被**显示判据**剔掉
            //       （原版 `DeckEditingWindow.__c___GetCardCollection_b__32_2.c:8`）；
            //     · 暗黑天使那 5 张秘仪：`inv = 10` **但 `show = 5`** ⇒ 原版卡池里**看得见**，
            //       原版最后靠 `GetOwnedCount == 0` 拦住；我们单机**全解锁** ⇒ 主对话 2026-10-18 裁定
            //       **显示且可加**（下面另有逐张断言）。
            //   ⇒ 两家「卡池里没有**不能进卡组**的卡」这条结论不变，但理由各是各的。
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
                    if (DeckRules.CannotAddToDeck(c)) { eff++; if (firstEff.Length == 0) firstEff = c.Name; }
                Check(eff, 0, $"★ {probe} 的卡池里**没有不能进卡组的卡**"
                            + $"—— 现在有 {eff} 张" + (eff > 0 ? $"（如 `{firstEff}`）" : ""));
            }
            // 🔴 **2026-10-18（用户全解锁边界 · 判据 = `AUD_卡池排序与天赋卡显隐.md` §五.B.5）**：
            //   5 张暗黑天使秘仪（`inv=10` **`show=5`**）**显示且可加** —— 旧口径（`A1798` 的 `||`）
            //   把它们既藏出卡池、又挡在卡组外；那一版是「没有持有数这道闸」的保险，今天按用户边界放开。
            //   🧨 灭自证：把 `DeckRules.CannotAddToDeck` 改回 `||` ⇒ 第二条当场红（`addable` 变 0）；
            //      把显示判据改回 `CannotAddToDeck` ⇒ 第一条红（`shown` 变 0）。
            {
                var ph = s.Pool;
                CardDef hw = null;
                for (int i = 0; i < ph.Count; i++)
                    if (ph[i].Type == "hero" && ph[i].Faction == "DarkAngels") { hw = ph[i]; break; }
                if (hw == null) Check(true, false, "卡池里找不到 DarkAngels 的督军（这一节没法验）");
                else
                {
                    s.SetWarlord(hw.Id);
                    int shown = 0, addable = 0;
                    var names = new System.Text.StringBuilder();
                    foreach (var c in s.VisibleCards())
                        if (c.InventoryOptions == 10 && c.ShowInCollectionOptions == 5)
                        {
                            shown++;
                            if (s.CanAdd(c) == DeckError.None) addable++;
                            names.Append(" `").Append(c.Name).Append("`");
                        }
                    Check(shown, 5, "★ 5 张暗黑天使秘仪（`inv=10` 且 `show=5`）**都在卡池里显示**"
                                  + "（原版卡池也看得见它们 —— 挡住它们的是 `GetOwnedCount`，我们没有那道闸）"
                                  + (shown > 0 ? "：" + names : ""));
                    Check(addable, shown, "★ ……而且**这 5 张都能加进卡组**（用户 2026-10-18 全解锁边界；"
                                        + "🧨 把 `DeckRules.CannotAddToDeck` 改回 `||` ⇒ 本条 0/5 红）");
                }
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

            // 🔴🔴 **2026-10-14（`A1798`）：卡组准入改用【原版真值】`inventoryOptions` / `showInCollectionOptions`**
            //   判据 = `DeckRules.CannotAddToDeck`（原版 `DeckUtility__ValidateDeckCards.c:68-77`）。
            //   原来那条 `DeckRules.IsEffectOnly(c.Subtype)`（我们自己按兵种猜的三类）同批**已删**。
            //   ⚠️ 下面的期望值**全部来自原版真值**（`数据/游戏数据/card_inventory_options.json` 的 `map`，
            //     括号里是原版那两个字段的原始取值），⛔ **不是**从我们自己的常量反推的。
            {
                System.Func<string, CardDef> byId = id =>
                {
                    var ph2 = s.Pool;
                    for (int i = 0; i < ph2.Count; i++) if (ph2[i].Id == id) return ph2[i];
                    return null;
                };
                // ① 两列的取值只许是原版枚举那几档 + `-1`（未知）—— 挡「随手填个数」
                int badOpt = 0; string firstBadOpt = null;
                foreach (var c in s.Pool)
                {
                    bool okInv = c.InventoryOptions == -1 || c.InventoryOptions == 0
                              || c.InventoryOptions == 5 || c.InventoryOptions == 10;
                    bool okShow = c.ShowInCollectionOptions == -1 || c.ShowInCollectionOptions == 0
                               || c.ShowInCollectionOptions == 5 || c.ShowInCollectionOptions == 10;
                    if (!okInv || !okShow) { badOpt++; if (firstBadOpt == null) firstBadOpt = c.Id; }
                }
                CheckTrue(badOpt == 0, $"★（A1798）卡池两列取值全落在原版枚举里（坏 {badOpt} 张"
                                     + (firstBadOpt == null ? "" : $"，如 `{firstBadOpt}`") + "）");

                // ② **跟着原版真值走的锚点**（逐张钉住 id 与原版原始字段值）
                //   · `UM_Smite`                  真值 `inv=10 show=0`（`NotInInventory`）⇒ **不能进**
                //     🧨 **灭自证①**：旧口径按 `Subtype`（`Psychic Power`）判它是**能进**的 ——
                //        谁把实现改回「按兵种猜」，这一条立刻红。
                CheckTrue(byId("cf0b346f70e748c4bbfc4dc03ab39ead") != null && DeckRules.CannotAddToDeck(byId("cf0b346f70e748c4bbfc4dc03ab39ead")),
                          "★（A1798）`UM_Smite`（原版 `inventoryOptions=10`）⇒ **不能进卡组**"
                        + "｜🧨 旧口径（按兵种）放它过 ⇒ 改回旧口径这条红");
                //   · `EC72` Antrak Silk           真值 `inv=10 show=0`（药剂）
                CheckTrue(byId("5a8a11b2a89796f48b34fbec8db6f452") != null && DeckRules.CannotAddToDeck(byId("5a8a11b2a89796f48b34fbec8db6f452")),
                          "★（A1798）`EC72` Antrak Silk（原版 `inventoryOptions=10`）⇒ **不能进卡组**");
                //   · `DA_Convoke_the_Circle`      真值 `inv=10` **`show=5`**（秘仪）
                //     🔴 **2026-10-18 改判（卡池排序与显隐）**：主对话按用户全解锁边界裁定 ——
                //        这类「`inv=10` 但 `show=5`」的卡 **显示且可加**（原版靠 `GetOwnedCount==0` 拦，
                //        我们没有持有数那道闸）⇒ 本条期望值从「不能进」翻成「**能进**」。
                //     🧨 灭自证：`CannotAddToDeck` 改回 `||` ⇒ 本条红；而且它同时钉住
                //        「`inv=10 & show=0` 的 `UM_Smite` 仍然不能进」（上一条）—— 单改正负任一方向都会有一条红。
                CheckTrue(byId("3e99603a7b7b2c54c8f1ad40250ee2ae") != null
                          && !DeckRules.CannotAddToDeck(byId("3e99603a7b7b2c54c8f1ad40250ee2ae")),
                          "★（2026-10-18 改判）`DA_Convoke_the_Circle`（`inv=10` **`show=5`**）⇒ **能进卡组**"
                        + "（用户全解锁边界；原版这条被 `GetOwnedCount==0` 拦住，我们没有那道闸）"
                        + "｜🧨 改回 `||` ⇒ 本条红");
                //   · `AM60` Timely Reinforcements 真值 `inv=5`（`CantAddToDeck`）**但它是防御卡**
                //     ⇒ 原版把它路由给 `CanAddDefensive`、**不吃**这条判据 ⇒ **不拦**（玩家要挑防御卡）
                CheckTrue(byId("AM60") != null && !DeckRules.CannotAddToDeck(byId("AM60")),
                          "★（A1798）防御卡 `AM60`（原版 `inventoryOptions=5`）**不拦**"
                        + "—— 原版 `CanAddCard` 把它路由给 `CanAddDefensive`（用户 2026-09-27：防御卡要在卡池里）");
                //   · `ASH83` Autarch              真值 `inv=0 show=5`（`InInventory`）⇒ **能进**
                //     🧨 谁把判据改成「inv != 5」或「inv >= 0」之类，这一条红
                CheckTrue(byId("ASH83") != null && !DeckRules.CannotAddToDeck(byId("ASH83")),
                          "★（A1798）普通卡 `ASH83`（原版 `inv=0 show=5`）⇒ **能进**（判据不是「inv != 5」）");

                // ③ 交叉核：拦下的张数 == 直接从两列 + 卡型现算出来的张数（**不写死数字**）
                //   🔴 2026-10-18 改口径：判据是 `&&`（两条都坏才拦），⛔ 不是 `||`。
                int viaPred = 0, viaCols = 0;
                foreach (var c in s.Pool)
                {
                    if (DeckRules.CannotAddToDeck(c)) viaPred++;
                    if (c.Type != "hero" && c.Type != "defence" && c.InventoryOptions >= 0
                        && (c.InventoryOptions != 0 && c.ShowInCollectionOptions != 5)) viaCols++;
                }
                Check(viaPred, viaCols, $"★（A1798）拦下的张数两条路现算一致（判据 {viaPred} / 两列直算 {viaCols}）");

                // ④ 🧨 **灭自证③（新旧口径的差集）**：新口径必须**严格宽于**旧口径，
                //    且「旧放行、新拦下」的那些**必须是原版 `NotInInventory(10)`** ——
                //    ⛔ 下面这段是**旧口径的复刻副本**，只为当变更传感器，**不是判据**（判据只有 `DeckRules` 一处）。
                //
                //    🔴 **2026-10-18 改口径后**：反方向那一格（旧拦、新放）**不再要求为空** ——
                //    按用户全解锁边界放开的，**只许**是「`inv=10` **且** `show=5`」那 5 张暗黑天使秘仪
                //    （判据 = 原版 `CardDeck__CanAddCard.c:40` 的 `&&` 变体 + `GetOwnedCount==0` 那道我们**没有**的闸）。
                //    🧨 改坏法：把判据写成 `inv != 0` / `show != 5` 任一单边 ⇒ `legit` 之外会冒出越界卡 ⇒ 红；
                //       改回 `||` ⇒ `oldOnly` 变 0（下面第二条红）。
                {
                    int newOnly = 0, oldOnly = 0, legit = 0;
                    string firstNew = null, firstBad = null;
                    foreach (var c in s.Pool)
                    {
                        string st = c.Subtype ?? "";
                        bool oldRule = st == "Combat Elixir" || st == "Sabotage" || st == "Secret";
                        bool newRule = DeckRules.CannotAddToDeck(c);
                        if (!oldRule && newRule)
                        { newOnly++; if (firstNew == null) firstNew = c.Id; }
                        if (oldRule && !newRule)
                        {
                            oldOnly++;
                            // 「该放开的」= 原版那个 `&&` 变体会放行的那一类（`inv=10` **且** `show=5`；
                            //   hero/defence 早被路由走，不参与）—— **从两列原值现算，不看 `CannotAddToDeck`**（免得自证）。
                            bool isMystic = c.Type != "hero" && c.Type != "defence"
                                         && c.InventoryOptions == 10 && c.ShowInCollectionOptions == 5;
                            if (isMystic) legit++;
                            else if (firstBad == null) firstBad = c.Id + "/" + c.Name;
                        }
                    }
                    Check(oldOnly, legit, $"★（2026-10-18 改判）旧口径拦、新口径放的**只许是**「`inv=10` 且 `show=5`」的秘仪"
                                        + $"（实测 {oldOnly} 张 = 合格 {legit} 张"
                                        + (firstBad == null ? "" : $"；越界如 `{firstBad}`") + "）");
                    CheckTrue(oldOnly > 0, $"★ ……而且**非空**（真的放开了 {oldOnly} 张；"
                                         + "⛔ 变 0 ⇒ 那是退回 `||` 的旧口径，不是本口径）");
                    CheckTrue(newOnly > 0, $"★（A1798）新口径**严格宽于**旧口径（多拦 {newOnly} 张，"
                                         + $"如 `{firstNew}`）｜🧨 实现改回「按兵种猜三类」⇒ 这里变 0、红");
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

        /// <summary>🆕 **2026-10-18（卡池排序 + 天赋卡显隐）**：原版判据现读 `d:/2/tools/decomp_full/` ——
        /// · 排序 = `DeckEditingWindow__GetCardCollection.c:106,121,136,151` 那 4 个**稳定 `OrderBy`**
        ///   ⇒ 实际主序 **`cardArmy`(10→130) → `cardType != Hero`（战将整段先）→ `manaCost` → 本地化卡名**；
        /// · 显示 = 同链 `…b__32_2.c:8` 的 `showInCollectionOptions != Hide(0)`（+ 卡组编辑那层 `inv != 5`，防御卡例外）。
        /// 本节的每一条都**不盯我们自己的常量**：期望值要么是原版那条判据的**字面量**，要么是从卡表两列**现算**。
        /// ⚠️ 断言只读状态（`VisibleCards` / `CanAdd`），**不跑 Unity 版面**——「显示+变灰」的渲染那一半在
        /// `TestPoolSortAndVisibilityRender`（要 `_rt`，在版面那一节里跑）。</summary>
        static void TestPoolSortAndVisibility()
        {
            var s = new DeckEditorState(CardDatabase.Load());
            s.WarlordGatedPool = true;

            int facN = 0, badFirst = 0, badCost = 0, heroAfter = 0, badShow = 0;
            string firstBadFirst = null, firstBadCost = null;
            var defShown = new System.Collections.Generic.HashSet<string>();
            foreach (var fac in DeckEditorState.ArmyOrder)
            {
                CardDef hw = null;
                foreach (var c in s.Pool)
                    if (c.Type == "hero" && c.Faction == fac) { hw = c; break; }
                if (hw == null) continue;
                s.SetWarlord(hw.Id);
                var vis = s.VisibleCards();
                facN++;

                // ① 同一阵营内**第一张必须是战将**（原版排序键 `cardType != Hero`，false 先 ⇒ 战将整段排最前）
                if (vis.Count == 0 || vis[0].Type != "hero")
                {
                    badFirst++;
                    if (firstBadFirst == null)
                        firstBadFirst = fac + (vis.Count > 0 ? "/" + vis[0].Name + "(" + vis[0].Type + ")" : "/(空)");
                }

                // ② 战将段之后按 `manaCost` 非减（第三把键）；同时钉「战将段是连续的一段」（不许后面又冒出督军）
                bool seenNonHero = false;
                int lastNonHeroCost = int.MinValue;
                foreach (var c in vis)
                {
                    if (c.Type == "hero")
                    {
                        if (seenNonHero) heroAfter++;
                        continue;
                    }
                    seenNonHero = true;
                    if (c.Cost < lastNonHeroCost)
                    {
                        badCost++;
                        if (firstBadCost == null) firstBadCost = fac + "/" + c.Name + "(" + c.Cost + ")";
                    }
                    lastNonHeroCost = c.Cost;
                    if (c.Type == "defence") defShown.Add(c.Id);
                }

                // ③ 卡池里 `show == 0` 的卡数 = 0（原版 `…b__32_2.c:8`）
                foreach (var c in vis) if (c.ShowInCollectionOptions == 0) badShow++;

                // 期望张数 = 从卡表两列**现算**（原版卡池链：`show != 0`；卡组编辑再加
                //   `inv != CantAddToDeck(5)`，防御卡按用户 2026-09-27 口径例外）
                int want = 0;
                foreach (var c in s.Pool)
                    if (DeckRules.SameFaction(c.Faction, fac)
                        && c.ShowInCollectionOptions != 0
                        && (c.Type == "defence" || c.InventoryOptions != DeckRules.OptionCantAddToDeck)) want++;
                Check(vis.Count, want, $"★ {fac}：卡池张数 = 两列现算（该阵营 `show != 0` 共 {want} 张）");
            }
            Check(facN, 13, $"★ 逐阵营过了一遍（{facN}/13 —— 少一个阵营这一节就漏一块）");
            Check(badFirst, 0, "★ ① 每个阵营卡池的**第一张都是战将**（原版 `cardType != Hero` 排序键）"
                             + (firstBadFirst == null ? "" : $"｜第一个反例：{firstBadFirst}"));
            Check(heroAfter, 0, "★ ①b 战将段**连续**（战将之后不许再冒出战将 —— 说明排序键没生效）");
            Check(badCost, 0, "★ ② 战将段之后 `manaCost` **非减**"
                            + (firstBadCost == null ? "" : $"｜第一个反例：{firstBadCost}"));
            Check(badShow, 0, "★ ③ 卡组编辑卡池里 `showInCollectionOptions == 0` 的卡数 = **0**"
                            + "（原版 `DeckEditingWindow.__c___GetCardCollection_b__32_2.c:8`）");

            // ④ 防御卡仍在（用户 2026-09-27 口径：经典模式编辑卡池要列防御卡）
            int defTotal = 0;
            foreach (var c in s.Pool) if (c.Type == "defence") defTotal++;
            Check(defShown.Count, defTotal, $"★ ④ 全池 {defTotal} 张防御卡**一张不少**地出现在各自阵营的卡池里"
                                          + "（用户 2026-09-27；🧨 去掉 `PassesEditModeInventory` 的防御卡例外 ⇒ 本条掉到 0）");
            CheckTrue(defTotal > 0, $"……而且（前提）池子里真有防御卡（{defTotal} 张）");

            // ①c 没督军那种跨阵营状态：**每个阵营段的第一张也是战将**、阵营段按 `cardArmy` 升序
            s.ClearWarlord();
            var all = s.VisibleCards();
            int segN = 0, badSeg = 0, badArmyOrder = 0, lastRank = -1;
            string lastFac = null;
            foreach (var c in all)
            {
                var f = c.Faction ?? "";
                if (f == lastFac) continue;
                segN++;
                if (c.Type != "hero") badSeg++;
                int rank = System.Array.IndexOf(DeckEditorState.ArmyOrder, f);
                if (rank <= lastRank) badArmyOrder++;
                lastRank = rank;
                lastFac = f;
            }
            Check(segN, 13, $"★ ①c 没督军时跨阵营卡池**按阵营分段**（{segN}/13 段）");
            Check(badSeg, 0, "★ ①c 每个阵营段的**第一张都是战将**");
            Check(badArmyOrder, 0, "★ ①c 阵营段次序 = `cardArmy` 10→130（`DeckEditorState.ArmyOrder` 的下标）");

            // ---- ③b 收藏卡牌页那份显示闸（`ShowInCollectionOnly`，**不开** `WarlordGatedPool`）----
            var cs = new DeckEditorState(CardDatabase.Load());
            cs.ShowInCollectionOnly = true;
            var csList = cs.VisibleCards();
            int csShow0 = 0;
            foreach (var c in csList) if (c.ShowInCollectionOptions == 0) csShow0++;
            int csWant = 0;
            foreach (var c in cs.Pool) if (c.ShowInCollectionOptions != 0) csWant++;
            Check(csList.Count, csWant, $"★ ③b 收藏卡牌页 = `show != 0` 的那批（{csWant} 张，从两列现算）");
            Check(csShow0, 0, "★ ③b 收藏卡牌页 `show == 0` 的卡数 = **0**（92 天赋 + 30 `Z-NC` 全被剔掉）");

            // ---- ⑤ 收藏卡牌页的排序：阵营 → 战将 → `inventoryOptions` → 费用 → 名 ----
            var cs2 = new DeckEditorState(CardDatabase.Load());
            cs2.ShowInCollectionOnly = true;
            cs2.SortByInventoryOptions = true;
            var cl = cs2.VisibleCards();
            CheckTrue(cl.Count > 0, "（前提）收藏序非空");
            if (cl.Count > 0)
            {
                Check(cl[0].Type, "hero", "★ ⑤ 收藏序第 1 张是**战将**（`cardType != Hero` 那把键）");
                Check(cl[0].Faction, DeckEditorState.ArmyOrder[0],
                      "★ ⑤ ……而且属于**第一个阵营**（`cardArmy` 10 = Ultramarines，`ArmyOrder[0]`）");
            }
            // 同一阵营、同为战将的那一段里 `inventoryOptions` 必须非减（第二把键，只有收藏窗有）
            int invBad = 0, lastInv = int.MinValue, segStart = 0;
            for (int i = 1; i <= cl.Count; i++)
            {
                bool brk = i == cl.Count || cl[i].Faction != cl[segStart].Faction
                        || (cl[i].Type == "hero") != (cl[segStart].Type == "hero");
                if (!brk) continue;
                lastInv = int.MinValue;
                for (int j = segStart; j < i; j++)
                {
                    if (cl[j].InventoryOptions < lastInv) invBad++;
                    lastInv = cl[j].InventoryOptions;
                }
                segStart = i;
            }
            Check(invBad, 0, "★ ⑤ 收藏序里「同阵营 + 同战将档」那一段的 `inventoryOptions` **非减**"
                           + "（🧨 去掉 `SortByInventoryOptions` 那一层 ⇒ 本条的判别力落在 `CollectionScene` 的同名断言上）");
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

            // ---- 🆕 2026-10-14（账 `N10`）：费用档把**卡的费用**夹到 `[1,8]` ----
            //  判据 = `d:/2/tools/decomp_full/CardCostFilter__FilterItem.c:15-20`（现读，逐字）：
            //    `iVar2 = *(int *)(param_2 + 0x78);`   // 这张卡的 `manaCost`
            //    `if (iVar2 < 2) { iVar2 = 1; } else if (7 < iVar2) { iVar2 = 8; }`
            //  —— **夹完才**交给那条筛选谓词（`:22`）⇒ 落点 = `DeckEditorState.VisibleCards()` 里那句
            //    `int ccost = ClampPoolCost(c.Cost);`（`DeckEditorState.cs` 的 `int ccost = ClampPoolCost(c.Cost);`）。
            //  🔑 谓词 = 「**夹过的费用 ∈ 已选档位**」（`CardCostFilter : ToggleCollectionFilter<RawCardScript,int>`
            //     ⇒ `+0x38` 是已选中的 int 档位集）—— 我们那八档（`1-` / `2`…`7` / `8+`，
            //     `FilterPanelModel.CostBucketHi` 实读）的边界卡正是靠夹取才匹配得上。
            //  ⚠️ **夹的是【卡】那一侧**，档位（`Cost` / `CostMax`）不夹 —— 照原版。
            //  🔴 **判别式是一组**：②③ 方向相反（一个要收 0 费卡、一个不许收）⇒ 任何**单一**错实现
            //     不可能同时满足；① 是**字面量**锚（②③ 的期望值是用 `ClampPoolCost` 现算的，
            //     把夹取函数本身改坏时两边会一起动 ⇒ 只有 ① 会红）；⑤ 单独钉**上夹**。
            //  ⚠️ ④ 那一格**没有鉴别力**（裸值 ≥ 8 的本来就 ≥ 8）—— **如实标**，见那一行。

            // ① `ClampPoolCost` **逐值 8 格**（照原版那两行字面量 `2` / `7`）
            Check(DeckEditorState.ClampPoolCost(0), 1, "★ N10 ①：`0` 费 ⇒ **1**（原版 `iVar2 < 2 ⇒ 1`）");
            Check(DeckEditorState.ClampPoolCost(1), 1, "★ N10 ①：`1` ⇒ **1**");
            Check(DeckEditorState.ClampPoolCost(2), 2, "★ N10 ①：`2` ⇒ **2**（下夹的边界：`< 2` **不含** 2）");
            Check(DeckEditorState.ClampPoolCost(7), 7, "★ N10 ①：`7` ⇒ **7**（上夹的边界：`7 < iVar2` **不含** 7）");
            Check(DeckEditorState.ClampPoolCost(8), 8, "★ N10 ①：`8` ⇒ **8**");
            Check(DeckEditorState.ClampPoolCost(9), 8,
                  "★ N10 ①：`9` ⇒ **8**｜🧨 去掉上夹那一条（`cost > 7 ⇒ PoolCostMax`）⇒ 本行实得 **9**");
            Check(DeckEditorState.ClampPoolCost(14), 8, "★ N10 ①：`14` ⇒ **8**（我们池里最大的费用）");
            Check(DeckEditorState.ClampPoolCost(-3), 1, "★ N10 ①：`-3` ⇒ **1**（负数与 0 同一条路：`< 2`）");

            // ② `1-` 档（= `[1,1]`）：**夹过之后 == 1** 的都进来 ⇒ 收的是费用 `{0,1}` 那一批
            f = DeckFilter.None; f.Cost = 1; f.CostMax = 1;
            s.SetFilter(f);
            int got1 = s.VisibleCards().Count, raw1 = 0, want1 = 0;
            foreach (var c in s.Pool)
            {
                if (c.Cost == 1) raw1++;
                if (DeckEditorState.ClampPoolCost(c.Cost) == 1) want1++;
            }
            Check(got1, want1,
                  "★ N10 ②：`1-` 档 = **夹过 `[1,8]` 之后 == 1** 的张数（" + want1 + "）"
                + "｜🧨 用裸 `c.Cost` 比（= 去掉夹取）⇒ 本行实得 " + raw1);
            CheckTrue(want1 > raw1,
                  "★ N10 ② 灭自证（与 ③ **方向相反**）：池里**有 0 费卡** ⇒ 夹过之后严格多于裸值（裸 "
                  + raw1 + " < 夹后 " + want1 + "）—— 把 `ClampPoolCost` 改成恒等 / 删掉夹取 ⇒ 本行红");

            // ③ `2` 档（= `[2,2]`）：**一张 0/1 费卡都不许收**（它们被夹成 1、不进第 2 档）
            f = DeckFilter.None; f.Cost = 2; f.CostMax = 2;
            s.SetFilter(f);
            int got2 = s.VisibleCards().Count, raw2 = 0, want2 = 0;
            foreach (var c in s.Pool)
            {
                if (c.Cost == 2) raw2++;
                if (DeckEditorState.ClampPoolCost(c.Cost) == 2) want2++;
            }
            Check(got2, want2,
                  "★ N10 ③：`2` 档 = **夹过之后 == 2** 的张数（" + want2 + "）"
                + "｜🧨 把夹取写成「≤ 2 ⇒ 1」⇒ 本行实得 **0**（第 2 档一张都匹配不上）");
            CheckTrue(want2 == raw2,
                  "★ N10 ③（与 ② 配对）：**夹过 == 2 的张数正好等于裸 == 2 的张数（" + raw2 + "）** —— "
                  + "② 要收 0 费卡、③ 不许收 ⇒ 单一错实现不可能两格一起绿");

            // ④ `8+` 档（= `[8, int.MaxValue]`）—— ⚠️ **这一格没有鉴别力**：
            //    裸值 ≥ 8 的本来就 ≥ 8 ⇒ 去掉上夹照样绿。**如实标**：它钉的是「这一档不漏卡」。
            f = DeckFilter.None; f.Cost = 8; f.CostMax = int.MaxValue;
            s.SetFilter(f);
            int got8 = s.VisibleCards().Count, want8 = 0;
            foreach (var c in s.Pool)
                if (DeckEditorState.ClampPoolCost(c.Cost) >= 8) want8++;
            Check(got8, want8, "★ N10 ④：`8+` 档 = **夹过之后 ≥ 8** 的张数（" + want8 + "）"
                + "（费用 8…14 那一批；⚠️ **无鉴别力** —— 上夹在这一档看不出来）");

            // ⑤ **上夹单独钉一格**：`CostMax = 8`（我们 UI 到不了这一档 —— `8+` 给的是 `int.MaxValue`）
            //    ⇒ `[8,8]`：夹过 == 8 的都要收（裸 8/9/14 全被夹成 8）。
            f = DeckFilter.None; f.Cost = 8; f.CostMax = 8;
            s.SetFilter(f);
            int gotC = s.VisibleCards().Count, rawC = 0, wantC = 0;
            foreach (var c in s.Pool)
            {
                if (c.Cost == 8) rawC++;
                if (DeckEditorState.ClampPoolCost(c.Cost) == 8) wantC++;
            }
            Check(gotC, wantC,
                  "★ N10 ⑤：`[8,8]` = **夹过 == 8** 的张数（" + wantC + "）"
                + "｜🧨 去掉**上夹**（`cost > 7 ⇒ PoolCostMax` 那一条）⇒ 本行实得 " + rawC);
            CheckTrue(wantC > rawC,
                  "★ N10 ⑤ 判别式：池里**有费用 > 8 的卡** ⇒ 夹过 == 8 严格多于裸 == 8（裸 "
                  + rawC + " < 夹后 " + wantC + "）");

            // ⑥ 回归：`Cost = -1` = **不限**（`VisibleCards` 里那句 `if (f.Cost >= 0)`）⇒ 全池一张不少
            f = DeckFilter.None; f.Cost = DeckEditorState.AnyCost;
            s.SetFilter(f);
            Check(s.VisibleCards().Count, s.PoolCount,
                  "★ N10 ⑥ 回归：`Cost = -1`（不限）⇒ 命中全池 —— ⛔ 夹取只在 `f.Cost >= 0` 那一段里跑，"
                + "别把它提到那个 `if` 前面去");
            s.SetFilter(DeckFilter.None);

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

        // ============================================================ 🆕 2026-10-17：拖拽（卡背预览 / 投递 / 阈值）
        //
        // 这一节断的全是**原版参数**（⛔ 不是我们自己的常量）：
        //   · 节点 / 预览 / 落点三样矩形 = prefab 实读（`menu_rect.py bundle_menus_assets_all -1322417011089150172`
        //     与 `"Cosmetic Drag Controller" --depth 2`；PathID 见 `DeckRuntime` 那一段注释）
        //   · 行为那四拍（`SetDraggable` / `OnBeginDrag` / `OnDrag` / `OnEndDrag`）= `DraggableController<T>`
        //     的 **VA 反汇编**（`Core/DraggableController.cs` 文件头有逐条）
        //   · 「拖 vs 滚动」= `SupportMethods.IsScrollDragThreshold`，常量 `read_literal.py` 直读
        //     （`Vector2.right` · 60 · 120 · `Vector2.kEpsilonNormalSqrt`）
        // ⚠️ 批处理没有真鼠标 ⇒ 全程喂**合成坐标**（`UiCosmeticDragBegin/Move/End`，与鼠标那条路同一批函数）。

        /// <summary>🆕 **2026-10-19（B6）**：把第 `idx` 格卡背**起拖**并停到 `(toX,toY)`，量出**预览画心**
        /// 的渲染中心（画布 px）。⚠️ **不松手** —— 收尾由调用方自己 `UiCosmeticDragEnd(栏外的点, …)`
        /// （那样既量得到拖动中的几何，又不会顺手把装备换掉）。
        /// 取不到（那一格不在这屏 / 起不了拖 / 量不到渲染矩形）⇒ `false`（调用方按前提红出来，⛔ 不静默）。</summary>
        static bool PreviewDrawnCenterAt(int idx, float toX, float toY, out float cx, out float cy)
        {
            cx = cy = 0f;
            var rt = _rt;
            if (rt == null) return false;
            float px, py;
            if (!rt.UiCosmeticCellCenter(idx, out px, out py)) return false;
            if (!rt.UiCosmeticDragBegin(px, py, toX, toY)) return false;    // 起拖点 = 那一格中心（纯水平位移）
            rt.UiCosmeticDragMove(toX, toY);                                // 原版 `OnDrag`：预览贴到指针
            float x1, y1, x2, y2;
            if (!UnionQuadsPx(rt.UiCosmeticPreviewGo.transform, out x1, out y1, out x2, out y2)) return false;
            cx = (x1 + x2) * 0.5f;
            cy = (y1 + y2) * 0.5f;
            return true;
        }

        static void TestCosmeticDrag()
        {
            var rt = _rt;
            CheckTrue(rt != null, "（前提）`_rt` 在（这一节要它）");
            if (rt == null) return;

            // ⚠️ 期望的那张卡背**按运行时同一条规则取**（阵营筛选之后那张表）—— 别直接拿 `CardArt.CosmeticNames()`，
            //    否则一旦筛选不是空串，断言比的就不是同一张（2026-10-17 那一处「铺格筛了、命中没筛」的缺陷就是这么来的）。
            var names = CardbackTable.NamesFor(rt.CosmoFilterArmy, CardArt.CosmeticNames());
            CheckTrue(names.Length > 8, $"（前提）卡背够用（{names.Length} 张 · 阵营筛选「{rt.CosmoFilterArmy}」）");

            // ---- ① 原版参数：两个节点 + 落点那一栏 ----
            {
                var n = rt.DragNodeRectPx;
                CheckNear(n.x, 993.59f, 0.01f, "★ 拖拽节点 `Cosmetic Drag Controller` 左上 x = **993.59**（原版 RT −3820370437395452124）");
                CheckNear(n.y, 525.50f, 0.01f, "★ ……左上 y = **525.50**");
                CheckNear(n.z, 100f, 0.01f, "★ ……宽 = **100**");
                CheckNear(n.w, 100f, 0.01f, "★ ……高 = **100**");
                var p = rt.PreviewLayoutRectPx;
                CheckNear(p.x, 943.59f, 0.01f, "★ 预览 `Collection Cosmetic` 布局框左上 x = **943.59**（原版 RT 3595378309407108900：anchor(0,1) · pos(75,−121.5)）");
                CheckNear(p.y, 444.50f, 0.01f, "★ ……左上 y = **444.50**");
                CheckNear(p.z, 250f, 0.01f, "★ ……宽 = **250**");
                CheckNear(p.w, 405f, 0.01f, "★ ……高 = **405**");
                var d = rt.DropFieldRectPx;
                CheckNear(d.x, 0.25f, 0.01f, "★ 落点栏 `Sidebar/Deck Details` 左上 x = **0.25**（原版挂着 `IDropHandler<CosmeticItem>` 的 `DeckEditingPanel` · GO −1322417011089150172）");
                CheckNear(d.y, 360.97f, 0.01f, "★ ……左上 y = **360.97**");
                CheckNear(d.z, 335.31f, 0.01f, "★ ……宽 = **335.31**");
                CheckNear(d.w, 649.06f, 0.01f, "★ ……高 = **649.06**");
                Check(rt.DragCueName, "CardStartDrag",
                      "★ 起拖音 cue 名 = **CardStartDrag**（原版两个实例共用的那条 `AudioCue`，PathID −4308815958917459268 反查）");
            }

            // ---- ② 出厂：预览**关着**（原版 `Collection Cosmetic` 的 `m_IsActive: false`）----
            rt.UiSetTab(2);
            CheckTrue(!rt.UiCosmeticPreviewActive, "★ 出厂预览**关着**（原版那个子节点就是 `m_IsActive: false`）");

            // ---- ③ 「拖 vs 滚动」= 原版 `IsScrollDragThreshold`（`Vector2.Angle(d, Vector2.right) ∈ (60°,120°)`）----
            //  四条**边界**值钉住那个开区间（判别式：换成别的常量/量纲必红）
            CheckTrue(!rt.UiIsScrollDragThreshold(100f, 166.43f), "阈值：与水平轴 **59°** ⇒ 不是滚动（原版 `deg > 60`）");
            CheckTrue(rt.UiIsScrollDragThreshold(100f, 180.40f), "阈值：**61°** ⇒ 判成滚动");
            CheckTrue(rt.UiIsScrollDragThreshold(-100f, 180.40f), "阈值：**119°**（左上方）⇒ 仍判成滚动（`deg < 120`）");
            CheckTrue(!rt.UiIsScrollDragThreshold(-100f, 166.43f), "阈值：**121°** ⇒ 不是滚动");
            CheckTrue(!rt.UiIsScrollDragThreshold(100f, 0f), "阈值：纯水平（0°）⇒ **不是**滚动 ⇒ 这一条能起拖");
            CheckTrue(!rt.UiIsScrollDragThreshold(0f, 0f),
                      "阈值：零位移 ⇒ 走 `Vector2.Angle` 里那道 `kEpsilonNormalSqrt`（1e-15）守卫、返 0 ⇒ 不是滚动");

            // ---- ④ 链路：起拖 → 预览显形 + 跟指针 → 松手投递 + 关预览 ----
            //  ⚠️ 起拖点必须落在**第 2 行**（`cy ≈ 763.5`）：第 1 行 `cy ≈ 358.5` 差 2.5px 就进不了落点栏
            //     （y 360.97..1010.03）、第 3 行 `cy ≈ 1168.5` 已在屏幕外 ⇒ **只有第 2 行那六格**
            //     既能起拖、松手又落在栏里。
            //  🔴 **2026-10-19（B6）就地订正（铁律 5）**：探针的判据换成「**旧口径与新口径差最远**」——
            //     旧口径 = 「按**贴图自己**的比例内接、居中」（2026-10-18 `A994③` 那一版）；
            //     新口径 = **两段式**（① 按 sprite 的 **`m_Rect`（707×1020）定框**；② 贴图再按
            //     **`textureRect/m_Rect` 缩**、并按 `padding` 挪位 —— 判据全文 → `Core/CardbackFace.cs`）。
            //     ⇒ 「内接 vs 拉满」这个旧判据在两段式下已经**指不准**：**宽**现在也会缩
            //     （最坏缩到 86%），差得最远的那一格未必还是旧的判据挑出来的那一格。
            //     挑错探针 ⇒ 下面那两条**会空转**（看着绿，其实两种口径算出来一样）。
            //     ⛔ 别退回写死一格：写死哪一格 = 赌那张卡背的 `m_Rect`/`textureRect` 关系，赌输就是「假绿」。
            const float PrevBoxW = 132f, PrevBoxH = 198f;   // 外接框 = 原版 `Image` 的 220×330 × scale 0.6
            int Cell = -1;
            float expW = 0f, expH = 0f, probeAspect = 0f, probeD = 0f;
            for (int i = 6; i < 12 && i < names.Length; i++)
            {
                var tx = rt.UiCosmeticTex(names[i]);            // 与建格**同一条**会出声的取值路
                if (tx == null || tx.height <= 0) continue;
                float s = tx.width / (float)tx.height;
                // ⛔ 下面这两个期望值**都不许**调被测实现（`CardbackFace.Fit` / `PreserveAspectSize` = 自证）
                var neu = CardbackDrawnPx(tx, PrevBoxW, PrevBoxH);                  // 两段式（现在的口径）
                var old = s > PrevBoxW / PrevBoxH                                   // 「只做第一段」那一版
                        ? new Vector2(PrevBoxW, PrevBoxW / s) : new Vector2(PrevBoxH * s, PrevBoxH);
                float dd = Mathf.Max(Mathf.Abs(neu.x - old.x), Mathf.Abs(neu.y - old.y));
                if (dd > probeD) { probeD = dd; Cell = i; expW = neu.x; expH = neu.y; probeAspect = s; }
            }
            CheckTrue(Cell >= 0 && probeD > 3f,
                      $"（前提）第 2 行里挑得到一张「两段式 ≠ 只做第一段」的探针 —— 第 {Cell + 1} 格 · 贴图比例 "
                    + $"{probeAspect:0.####} · 两种口径差 {probeD:0.##}px"
                    + "（差 ≤ 3px 说明下面那两条会**空转** ⇒ 这一条先红，⛔ 别放它过去）");
            if (Cell < 0) return;
            float cx6, cy6;
            CheckTrue(rt.UiCosmeticCellCenter(Cell, out cx6, out cy6), $"（前提）第 {Cell + 1} 格在这一屏里");
            const float DropX = 240.1f;         // 落点栏（x 0.25..335.56）里的一点 —— ⛔ 别写成 `cx6 − 常量`
            float cx0 = 330.23f + (1589.78f - 6f * 250f) * 0.5f + 250f * 0.5f;   // 第 1 行第 1 格中心
            float cy0 = 155.97f + 405f * 0.5f;
            // 先把装备状态设到**另一张**，下面那条「换成了第 N 张」才不是碰巧
            CheckTrue(rt.UiClickCosmetic(cx0, cy0, true), "④ **右键装备那条路仍然好**（原版主力路 `OnCosmeticClick`：右键按下就装备）");
            Check(rt.EquippedCardback, names[0], "……右键点第 1 格 ⇒ 装备的是第 1 张");
            CheckTrue(!rt.UiCosmeticDragging, "（前提）此刻不在拖");

            CheckTrue(rt.UiCosmeticDragBegin(cx6, cy6, DropX, cy6),
                      "起拖：**纯水平**位移（180°）⇒ 起拖（竖向才会被判成滚动）");
            CheckTrue(rt.UiCosmeticDragging, "……`dragging` 立起来了（原版 `SetDraggable` 那句 `dragging = true`）");
            CheckTrue(rt.UiCosmeticPreviewActive, "★ 起拖那一拍预览**显形**（原版 `OnBeginDrag` 第一件事 = `gameObject.SetActive(true)`）");
            {
                // 预览**画出来**的框 = 原版 `Collection Cosmetic > content > Image_…` 那颗 `Image`
                //   的 `sizeDelta` **220×330** × `m_LocalScale` 0.6 = **外接框 132×198**；
                //   🔴 **画出来 ≠ 外接框** —— 那颗 `Image` 带 **`m_PreserveAspect = 1`**。
                // 🔴 **2026-10-18（`A994③`）就地订正**：`A944` 那轮把 `m_PreserveAspect = 1` 读成
                //   「比例就取 220/330」（= **拉满**）⇒ 那两条当时钉的是**旧错值 132×198**。
                // 🔴 **2026-10-19（B6）再订正（铁律 5）**：`A994③` 那一版只做了 uGUI 的**第一段**
                //   （「按**贴图自己**的比例内接」）—— 原版是**两段**：① 按 sprite 的 `m_Rect` 定框；
                //   ② 贴图再按 **`textureRect/m_Rect`** 缩（⇒ **宽也会缩**，最坏 86%）并按 `padding` 挪位。
                //   口径/判据全文 → `Core/CardbackFace.cs`（本仓**唯一**一份）。
                //   期望值现算走 `CardbackDrawnPx` / `CardbackOffsetPx`（⛔ **不调** `CardbackFace.Fit` /
                //   `CosmeticPreview.PreserveAspectSize` —— 那是被测实现本身，调它算期望值 = 自证）。
                float x1, y1, x2, y2;
                CheckTrue(UnionQuadsPx(rt.UiCosmeticPreviewGo.transform, out x1, out y1, out x2, out y2),
                          "（前提）预览那一棵能量出渲染矩形");
                CheckNear(x2 - x1, expW, 0.5f,
                          $"★ 预览**画出来**的宽 = **{expW:0.##}**（探针第 {Cell + 1} 格的贴图比例 "
                        + $"{probeAspect:0.####} ⇒ 两段式：**框宽 132 × `textureRect`/`m_Rect` 的宽那一半**；"
                        + $"贴图宽/707 = {expW / 132f:0.####}。"
                        + "⛔ 「宽恒 132」是 `A994③` 那一版（只做了第一段））"
                        + "｜🧨 改坏法：把 `Core/CardbackFace.cs` 的 `Fit` 里 `w = fw*sw` 改成 `w = fw`"
                        + "（= 只定框、不按 `textureRect` 缩）⇒ **这一条**红（高那条不红）");
                CheckNear(y2 - y1, expH, 0.5f,
                          $"★ ……高 = **{expH:0.##}**（= (132×1020/707) × `textureRect`高/1020；"
                        + "⛔ 「拉满那一档恒为 198」、「只做第一段」那一档都不等于它）"
                        + "｜🧨 改坏法：同上（这一条就是那处改动的**唯一**看门人）");
                // 🔴 **灭自证**：这一条与上面两条**结构上不可能同时被「拉伸」那种实现满足** ——
                //   拉伸时高恒等于外接框高 198 ⇒ 本条红。
                CheckTrue(y2 - y1 < 198f - 1f,
                          $"★ 灭自证：实绘高 {y2 - y1:0.##} **严格小于**外接框高 198（拉伸那种实现恒等于 198 ⇒ 红）");
                // 🧨 **偏移那一半**的改坏法（改坏的是 `padding` 那一截，**上面三条一条都不会红**：
                //   它们只量宽高、量不到位置）⇒ 抓它的在下面 ⑦ 那两条差分断言。
            }
            rt.UiCosmeticDragMove(DropX, cy6);
            {
                float px2, py2;
                CheckTrue(rt.UiCosmeticPreviewCenter(out px2, out py2), "（前提）能读预览中心");
                CheckNear(px2, DropX, 0.5f, "★ 预览**跟着指针**（x）—— 原版 `OnDrag` 那一跳");
                CheckNear(py2, cy6, 0.5f, "★ ……（y）");
            }
            CheckTrue(rt.UiCosmeticDragEnd(DropX, cy6),
                      "落点在那一栏**里** ⇒ **投递了**（原版 `OnEndDrag` 在 `hovered` 上找到 `IDropHandler<CosmeticItem>`）");
            Check(rt.EquippedCardback, names[Cell], $"……装备换成了第 {Cell + 1} 张（{names[Cell]}）");
            CheckTrue(!rt.UiCosmeticPreviewActive, "★ 松手那一拍预览**关回去**（原版 `OnEndDrag` 第一件事 = `SetActive(false)`）");
            CheckTrue(!rt.UiCosmeticDragging, "……`dragging` 落下来了");

            // ---- ⑤ 判别式：落点在栏**外** ⇒ 不投递 · 竖向位移 ⇒ 根本不起拖 ----
            //  这两条与上面那条一起构成「结构上不可能同时满足」：一个「不看落点、松手就装备」的实现红在第一条，
            //  一个「不看方向、位移就起拖」的实现红在第二条。
            CheckTrue(rt.UiCosmeticDragBegin(cx6, cy6, 1200f, cy6), "再起一次拖（水平）");
            CheckTrue(!rt.UiCosmeticDragEnd(1200f, cy6),
                      "★ **落点在那一栏之外 ⇒ 不投递**（原版：`hovered` 上没有 `IDropHandler<CosmeticItem>` —— 卡池那边松手不算）");
            Check(rt.EquippedCardback, names[Cell], "……装备**没变**");
            CheckTrue(!rt.UiCosmeticPreviewActive, "……预览照样关回去（关预览与投没投出去无关）");
            CheckTrue(!rt.UiCosmeticDragBegin(cx6, cy6, cx6, cy6 - 200f),
                      "★ **竖向位移（90°）⇒ 不起拖**（原版 `IsScrollDragThreshold`：那是滚动的手势）");
            CheckTrue(!rt.UiCosmeticDragging && !rt.UiCosmeticPreviewActive, "……既没起拖、预览也没显形");
            Check(rt.EquippedCardback, names[Cell], "……装备当然也没变");

            // ---- ⑦ 🆕 2026-10-19（B6）：卡背「两段式」的**偏移那一半**（预览件）----
            //  判据 = 原版那张 sprite 的 `padding`（`Resources/Cardbacks.json` 的四列）⇒ **换一张
            //  padding 不同的卡背，画心会挪**（`offset = ((padL−padR)/2, (padB−padT)/2) × 框宽/707`）。
            //  🔴 **为什么用差分、不写绝对期望值**：预览那颗 quad 的 `localPosition` 记的是**布局框中心
            //     的世界坐标**（`DeckRuntime.BuildCosmeticDrag` 那句 `Pos(pcx, pcy)`），而拖拽把**根**
            //     挪到指针上 ⇒ 画心的绝对位置 = 根 + 布局框中心 + 偏移。把那两个常量抄进期望值 = 把
            //     **实现细节**钉进断言（⛔ 另一族毛病）。**差分把「根 + 框中心」一起消掉**，
            //     左边只剩「原版 `padding` 算出来的那一截」——两根都停在同一落点上，那一项**逐位相消**。
            //  🧨 改坏法（三条，抓的都是**上面那三条尺寸断言抓不到**的那一半）：
            //     (a) 把 `Core/CardbackFace.cs` 的 `Fit` 里 `dx = …` / `dyUp = …` 两行删掉（偏移恒 0）
            //         ⇒ **两条都红**（差变 0）；(b) 把 `CosmeticPreview.Initialize` 里
            //         `+ new Vector3(dox, doyUp, 0f)` 删掉 ⇒ 同样两条都红；
            //     (c) 把那一句的 `doyUp` **反号** ⇒ **纵向那条红**（uGUI 的 y 向上、本处世界单位是 y 向上
            //         ⇒ ⛔ 这里**不许**反号 —— 反号那一处是 `DeckRuntime` 的画布 px 那两处）。
            {
                int offA = -1, offB = -1;
                float offD = 0f, oax = 0f, oay = 0f, obx = 0f, oby = 0f;
                for (int ia = 6; ia < 12 && ia < names.Length; ia++)
                {
                    var ta = rt.UiCosmeticTex(names[ia]);
                    float ax, ay;
                    if (ta == null || !CardbackOffsetPx(ta, PrevBoxW, PrevBoxH, out ax, out ay)) continue;
                    for (int ib = ia + 1; ib < 12 && ib < names.Length; ib++)
                    {
                        var tb = rt.UiCosmeticTex(names[ib]);
                        float bx, by;
                        if (tb == null || !CardbackOffsetPx(tb, PrevBoxW, PrevBoxH, out bx, out by)) continue;
                        float dd = Mathf.Max(Mathf.Abs(bx - ax), Mathf.Abs(by - ay));
                        if (dd > offD) { offD = dd; offA = ia; offB = ib; oax = ax; oay = ay; obx = bx; oby = by; }
                    }
                }
                string offPair = offA >= 0
                               ? $"第 {offA + 1} 格（{names[offA]}）与第 {offB + 1} 格（{names[offB]}）" : "（没挑到）";
                CheckTrue(offA >= 0 && offD > 1.5f,
                          $"（前提）第 2 行里挑得到一对「`padding` 偏移差 > 1.5px」的卡背 —— "
                        + offPair
                        + $" · 两对的偏移差 ({obx - oax:0.##}, {oby - oay:0.##})px · 最大 {offD:0.##}px"
                        + "（差 ≤ 1.5px ⇒ 下面两条量不出牙 ⇒ 这一条先红）"
                        + "；⚠️ 表是旧版（没有四列 `padding`）⇒ 本函数恒 false ⇒ 这里也会红（跑 "
                        + "`python d:/4/Unity/工具/gen_cardbacks.py` 重生成）");
                if (offA >= 0 && offD > 1.5f)
                {
                    float acx, acy, bcx, bcy;
                    bool okA = PreviewDrawnCenterAt(offA, DropX, cy6, out acx, out acy);
                    CheckTrue(okA, $"（前提）第 {offA + 1} 格起拖得起来、且量得到预览画心的渲染中心");
                    CheckTrue(!rt.UiCosmeticDragEnd(1200f, cy6),
                              "……落点选在栏**外** ⇒ 不投递（⛔ 别在这一节里顺手把装备换了）");
                    bool okB = PreviewDrawnCenterAt(offB, DropX, cy6, out bcx, out bcy);
                    CheckTrue(okB, $"（前提）第 {offB + 1} 格同样量得到");
                    CheckTrue(!rt.UiCosmeticDragEnd(1200f, cy6), "……同样不投递");
                    // 前提不成立 ⇒ 只留上面那两条红（⛔ 不级联出两条「量了 0」的假红）
                    if (okA && okB)
                    {
                        CheckNear(bcx - acx, obx - oax, 0.5f,
                                  $"★ 预览画心**横向**跟着 `padding` 挪：换一张卡背 ⇒ 横移 **{obx - oax:0.##}px**"
                                + "（= ((padL−padR)/2 × 132/707) 的差 —— 没做偏移那一半时**恒为 0**）");
                        CheckNear(bcy - acy, oby - oay, 0.5f,
                                  $"★ ……纵向 = **{oby - oay:0.##}px**（同上；⛔ 预览这一处是世界单位、"
                                + "`dy` 向上为正 ⇒ **不反号** —— 反了这条红）"
                                + "｜🧨 改坏法见本节开头那 (a)(b)(c) 三条");
                    }
                }
            }

            // ---- ⑥ 卡牌那一支（原版 `CardDraggingController` · 拖影卡行 287.9×55.7）----
            //  ⚠️ 挑一张**现在真加得进去**的卡：`CanAdd` 过不了的卡原版**就不起拖**（弹错误消息），
            //     那不是缺陷 ⇒ 夹具先自己筛一遍，否则这条会红在夹具上。
            //  ⚠️ 这一节会把拖进去那张**留在卡组里**（后面几节各用各的 `DeckRuntime` ⇒ 不受影响）。
            rt.UiSetTab(0);
            CheckTrue(!rt.UiCardGhostActive, "★ 出厂拖影卡行**关着**（原版 `Deck Selector Card Info button` 的 `m_IsActive: false`）");
            // 🔴 **2026-10-17（F2 · 修 (δ) 夹具前提不成立）**：本节那个「挑一张现在加得进去的卡」的**前置**
            //   在这套夹具下**恒不成立** —— `_rt` 的卡组来自 `PlayerDeckForDemo`（`Deck/DeckRuntime.cs`），
            //   它把 `CardIds` **填满到** `DeckRules.ClassicCards = 30`；而 `CanAdd` 的第一条卡位检查是
            //   `if (Deck.CardIds.Count >= MaxDeckCount) return DeckError.TooManyCards;`
            //   （`Deck/DeckEditorState.cs` 的 `MaxDeckCount`，`MaxDeckCount = CardCount(Skirmish) = 30`）⇒ **每一张普通卡**
            //   都 `TooManyCards`；hero / defence 又被 `CanAdd` 的前两支回 `WarlordAlreadySet` /
            //   `DefensiveAlreadySet`（演示卡组两者都已设）⇒ 找不到 ⇒ `view < 0` ⇒
            //   **⑥ 整节（起拖 / 跟指针 / 落点投递 / 拖影卡行 287.9×55.7）一条都没跑 = 零覆盖**。
            //   铁证（同一个演示卡组、同一次运行）：`deck.log` 里那句 `✓ 能加满 30 张`。
            //   ⇒ **开跑前腾一个空位**。⛔ 别改成「随便挑一张不问 `CanAdd`」—— 那正是原版 `CanAddCard`
            //     挡掉的那条路；也⛔ 别动 `PlayerDeckForDemo`（另有断言按它「满 30 张」的形态在跑）。
            //   ⚠️ 挑的是**普通卡**（`Type == "unit"`）：`TryRemove` 对 hero / defence 走的是
            //     `ClearWarlord` / `ClearDefensive`（`Deck/DeckEditorState.cs` 的 `ClearDefensive`）⇒ 那样会把
            //     阵营/督军一起清掉、卡池的筛选跟着变（本节后面全按卡池下标在跑）。
            // 🔴 **2026-10-14（`DIAG_deck七条红.md` ① · 铁律 5 订正）：上面那一版**腾错了卡**。**
            //   原来腾的是 `DeckEntries()` 里**第一张 unit** = 实测 **`ASH4` 守护者防御小队**，
            //   而它在卡池里的**可见下标 = 6**（现读 `cards_engine.json`，`SaimHann` 卡表原序：
            //   `ASH83 ASH77 ASH63 ASH62 ASH80 ASH22 ASH4 …`）—— **正好在被扫的 `0..5` 之外一格**
            //   ⇒ 「腾了等于没腾」，下面那条前提照样红。
            //   **错因**：`DeckEntries()` 按「费用 → 显示名」排（`DeckRuntime.cs` 的 `DeckEntries()`）、
            //   卡池按**卡表原序**排（`DeckEditorState.VisibleCards` 明写「不排序」）⇒
            //   两个「第一张」根本不是同一张。
            //   ⚠️ **2026-10-18（铁律 5 就地订正）**：上面那句「`DeckEntries()` 按费用 → 显示名排」**仍然成立**，
            //   但**第二把键的口径变了**：原版 `ThenBy` 没传 comparer ⇒ `Comparer<string>.Default`
            //   = `StringComparison.CurrentCulture`（旧实现用的是 `CompareOrdinal` 码点序，已按用户口径改回文化比较）。
            //   ⇒ 「第一张 unit 是谁」这个结论**要重新对**：本机 zh-CN 下汉字按拼音排，费用 1 段是
            //   `风暴守护者`(feng) &lt; `守护者防御小队`(shou) &lt; `织亡者次元蜘蛛`(zhi)
            //   ⇒ **第一张是 `ASH5`、不是 `ASH4`**。
            //   本段早已改成「按 `CanAdd` 挑」、不再依赖「第一张是谁」⇒ **行为不受影响**。
            //   ⇒ 改成：**从卡池可见前 18 格里挑那张 `CanAdd` 不过的**，腾掉它的**副本**
            //     （副本数 2→1、总数 30→29 ⇒ 这一张下一拍就 `CanAdd == None`）。
            //   🔴 **2026-10-18（卡池排序）**：扫的范围 **6 → 18**。原版排序落地后**战将整段排最前**
            //     （阵营 → 战将 → 费用 → 名），演示卡组那个阵营的督军段就占掉最前面几格
            //     （最多的一家 7 位督军 + 防御卡），前 6 格里已经不够挑「非 hero / 非 defence」的卡。
            //     18 = 前三行（视口 924 高、格高 384 ⇒ 第 4 行在屏外，⛔ 别扫到那里去起拖）。
            //   ⚠️ 仍**只挑 hero / defence 之外**的：`TryRemove` 对那两型走的是
            //     `ClearWarlord` / `ClearDefensive`（`Deck/DeckEditorState.cs` 的 `ClearDefensive`）⇒ 会把
            //     阵营/督军一起清掉、卡池的筛选跟着变（本节后面全按卡池下标在跑）。
            {
                bool freed = false;
                for (int i = 0; i < 18 && !freed; i++)
                {
                    var d = rt.UiPoolCardAt(i);
                    if (d == null || d.Type == "hero" || d.Type == "defence") continue;
                    if (rt.State.CanAdd(d) == DeckError.None) continue;   // 这一张本来就加得进去 ⇒ 不用动它
                    freed = rt.State.TryRemove(d);
                }
                CheckTrue(freed, "（夹具）腾掉「卡池前 18 格里**现在加不进去的那一张**」的副本 —— "
                               + "`PlayerDeckForDemo` 把卡组填满到 30，不腾就挑不出 `CanAdd` 过得去的卡"
                               + "（那样本节整段等于没跑）");
                // 张数变了 ⇒ 卡池那几格上的 `x N` 也要跟着刷（本文件后面那条移除也是这么做的：
                // `UiSetTab(1); UiSetTab(0);` —— 切页会重建列表）。
                rt.UiSetTab(1); rt.UiSetTab(0);
            }
            {
                int view = -1;
                for (int i = 0; i < 18; i++)
                {
                    var d = rt.UiPoolCardAt(i);
                    if (d != null && rt.State.CanAdd(d) == DeckError.None) { view = i; break; }
                }
                CheckTrue(view >= 0, "（前提）卡池前 18 格里有一张现在加得进去的卡（`CanAdd` 过了才起得了拖）");
                if (view >= 0)
                {
                    float sx, sy, sw, sh;
                    CheckTrue(rt.UiPoolCellRect(view, out sx, out sy, out sw, out sh), "（前提）那一格的矩形在");
                    // 🔴 **2026-10-19（A1170）**：**起拖之前**（= 静态、拖影关着）就把拖影卡行的版面位量下来。
                    //   判据（原版 `OnDrag` 那两句 → `Core/DraggableController.cs` 文件头；同一个 `OnDrag`
                    //   在同形的 `A1151` 已经落过地）：「布局框中心」那一跳必须记在**拖影根自己**的
                    //   `localPosition` 上、三个子件**相对根中心**（→ `Deck/DeckRuntime.cs` 的 `BuildCardDrag`）。
                    //   这一条是「**只搬偏置、不动静态版面**」的看门人：偏置整个删掉（子件对零、根也不加）
                    //   ⇒ 画心掉到屏幕中心 (960,540) ⇒ **红**；旧写法下它**绿**（偏置摊在子件上、根在原点
                    //   ⇒ 静态画出来正好也是这个位）—— 所以它管的是「别把静态版面搬歪」，不是消灭旧写法。
                    //   期望值 = **拖拽节点的中心**（`DragNodeRectPx`；① 那四条已把它逐值钉在原版
                    //   `[993.59,525.50] 100×100` 上 ⇒ ⛔ 不是拿我们自己的常量自证）。
                    //   ⚠️ 量法见 `GhostStaticUnionPx`（拖影出厂是关着的 ⇒ `InHierarchy` 那档量不到一块，
                    //   `Self` 那档才量得到）—— 这条因此**不依赖测试顺序**（不必是「本节第一拍拖动」）。
                    {
                        float gxL, gyT, gxR, gyB;
                        var dn = rt.DragNodeRectPx;
                        CheckTrue(!rt.UiCardGhostActive, "（前提）起拖前拖影卡行是**关着**的（原版 `m_IsActive: false`）");
                        CheckTrue(GhostStaticUnionPx(out gxL, out gyT, out gxR, out gyB),
                                  "（前提）静态下也能量出拖影卡行的渲染矩形（激活闸 = `Self`）");
                        CheckNear(gxR - gxL, 287.9f, 0.5f, "★ 静态：拖影卡行画出来的宽仍是 **287.9**");
                        CheckNear(gyB - gyT, 55.7f, 0.5f, "★ 静态：…… 高仍是 **55.7**");
                        CheckNear((gxL + gxR) * 0.5f, dn.x + dn.z * 0.5f, 0.5f,
                                  $"★ **静态画心 x 在版面位**（= 拖拽节点中心 **{dn.x + dn.z * 0.5f:0.##}**）"
                                    + "｜🧨 改坏法：把偏置从子件上删掉却不加到根上 ⇒ 画心掉到屏幕中心 ⇒ 红");
                        CheckNear((gyT + gyB) * 0.5f, dn.y + dn.w * 0.5f, 0.5f,
                                  $"★ …… y（= **{dn.y + dn.w * 0.5f:0.##}**）");
                    }
                    int before = rt.State.DeckCount;
                    CheckTrue(rt.UiCardDragBeginAt(view, 200f - sx, 600f - sy),
                              "卡牌那一支起拖（位移指向落点栏 ⇒ 不是滚动）");
                    CheckTrue(rt.UiCardGhostActive, "★ 起拖那一拍拖影卡行**显形**（原版 `OnBeginDrag`）");
                    {
                        float x1, y1, x2, y2;
                        CheckTrue(UnionQuadsPx(rt.UiCardGhostGo.transform, out x1, out y1, out x2, out y2),
                                  "（前提）拖影那一棵能量出渲染矩形");
                        CheckNear(x2 - x1, 287.9f, 1f, "★ 拖影卡行**画出来**的宽 = **287.9**（原版 `Deck Selector Card Info button`）");
                        CheckNear(y2 - y1, 55.7f, 1f, "★ ……高 = **55.7**");
                    }
                    rt.UiCardDragMove(200f, 600f);
                    {
                        // ★★ **A1170 的正主**：拖起来之后**画心贴着指针**（= 原版 `OnDrag` 那一跳的净效果）。
                        //   🔴 **量的是「子树里真渲染出来的矩形」**（`UnionQuadsPx` → 每颗 `ImageQuad` 的
                        //   **世界**矩形 → 画布 px），⛔ **不读 `rt.UiCardGhostGo.transform.position`** ——
                        //   `OnDrag` 每帧把它整句设成指针位置，拿它当检测器 = **拿被测实现自己的口验自己**
                        //   （自证：那样连「偏置挂错了件」都看不出来 —— 那个口恒等于指针）。
                        //   🧨 **灭自证（结构上不可能同时满足）**：偏置只要还留在**子件**上（= 旧写法），
                        //   画心 = 指针 **+(83.59 px 右, 35.5 px 下)** ⇒ 下面两条**必红**（容差 1px，
                        //   差两个数量级）；只有把它搬到**根**上才绿。
                        float mx1, my1, mx2, my2;
                        CheckTrue(UnionQuadsPx(rt.UiCardGhostGo.transform, out mx1, out my1, out mx2, out my2),
                                  "（前提）拖动中能量出拖影卡行的渲染矩形");
                        CheckNear((mx1 + mx2) * 0.5f, 200f, 1f,
                                  "★ **拖动中画心贴着指针（x）** —— 原版 `OnDrag` 把拖影**根**摆到指针上"
                                    + "（偏置在根上、被那一跳整句覆盖）｜🧨 旧写法（偏置摊在子件上）这里差 **+83.59**");
                        CheckNear((my1 + my2) * 0.5f, 600f, 1f,
                                  "★ ……（y）｜🧨 旧写法这里差 **+35.5**");
                    }
                    CheckTrue(rt.UiCardDragEnd(200f, 600f),
                              "落在栏里 ⇒ **投递了**（原版 `DeckEditingPanel.Drop(RawCardScript)` ⇒ 我们的 `TryAddCard`）");
                    Check(rt.State.DeckCount, before + 1, "……卡组多了**正好 1 张**（夹具挑的就是 `CanAdd` 过得了的那张）");
                    CheckTrue(!rt.UiCardGhostActive, "★ 松手后拖影卡行**关回去**（原版 `OnEndDrag`）");
                    // 落点在外面 ⇒ 不投递（同 ⑤ 那条判别式的卡牌版 —— 先把它从卡组拿掉，好再挑一张能加的）
                    rt.State.TryRemove(rt.UiPoolCardAt(view));
                    rt.UiSetTab(1); rt.UiSetTab(0);            // 刷新卡池（张数变了 ⇒ 那一格的 `x N` 也变了）
                    // 🔴 **2026-10-11（`A1533`）就地订正**：位移原来写 `780f - sy` —— 那是**竖向为主**的手势，
                    //   能不能起拖**取决于这一格在第几列**（`sx` 多大）：
                    //     第 0 列 `sx≈468.85` ⇒ 位移 `(431.15, 451.14)` ⇒ 与水平轴 **46.30°** ⇒ 不是滚动 ⇒ 起拖（绿）；
                    //     第 1 列 `sx≈731.35` ⇒ 位移 `(168.65, 451.14)` ⇒ **69.50°** ⇒ 落进滚动闸 `(60°,120°)`
                    //       ⇒ `Deck/DeckRuntime.cs` **静默 `return false`**（那一支不弹错、不打日志）⇒ 红。
                    //   ⇒ 这是**夹具判据不牢**，⛔ 不是实现缺陷、⛔ 也不是本批回归：
                    //     `T`（`DeckRuntime.DeckEntries` 去重+排序，`CardPresentation/Deck/DeckRuntime.cs`）把下面 `CardPresentation/Deck/DeckRuntime.cs` 移除的那张卡
                    //     从 `Autarch` 换成了别的 ⇒ 卡池第 0 格不再过 `CanAdd` ⇒ `view` 由第 0 列挪到第 1 列。
                    //   ⇒ 改成**纯水平位移**（`dy = 0`）：与水平轴 0° ⇒ **恒不进滚动闸、与挑到第几列无关**。
                    //   ⚠️ 同族先例 = 本节卡背那条 `本文件`（`UiCosmeticDragBegin(cx6, cy6, DropX, cy6)`，y 两端相同）。
                    //   ⚠️ 闸门本身**忠实照原版**（`本文件` = `Vector2.Angle(d, right) ∈ (60°,120°)`，
                    //     判据 = `SupportMethods__IsScrollDragThreshold.c`）⇒ ⛔ **别去改闸**、⛔ 别改 `CanAdd` 的次序。
                    //   落点 `(900, 780)` 的 x=900 仍在 `DropField`（0.25..335.56）之外 ⇒ 下面「不投递」的原意不变。
                    bool began2 = rt.UiCardDragBeginAt(view, 900f - sx, 0f);
                    CheckTrue(began2, "再起一次拖（**纯水平**位移 ⇒ 不被滚动闸吃掉）");
                    // 🔴 **同一条账的另一半**：下面两条**只在「真起了拖」时才有鉴别力** —— 没起拖时
                    //   `UiCardDragEnd` 当然返 false、拖影当然也是关的 ⇒ **两条一起空转绿**（2026-10-11 实测：
                    //   那条 ✗ 的下一行紧跟的就是 ✓）。⇒ 补一条**正向**断言把这个洞堵死：
                    //   起拖那一拍拖影**必须显形**（与本节第一次拖的 `本文件` 同形）——
                    //   它**结构上不可能与「没起拖」同时为真**。
                    CheckTrue(rt.UiCardGhostActive, "★ 第二次起拖那一拍拖影卡行**同样显形**（⛔ 别删：删了下面两条会空转）");
                    CheckTrue(!rt.UiCardDragEnd(900f, 780f), "★ **落在栏外 ⇒ 不投递**（`hovered` 上没有那个 handler）");
                    CheckTrue(!rt.UiCardGhostActive, "……拖影照样关回去");
                }
            }
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
                // ⚠️ `A1896` 起 `LoadDeck`（打开编辑器）会逐张摘掉「按原版不能进卡组」的牌 ——
                //    夹具里混进那种单位卡，「12 张正好装满」就会红。今天池里只有 1 张（SpaceWolves），
                //    碰巧不在这个阵营；加这道闸是防卡池顺序/阵营变了之后**静默变红**。
                if (ids.Count >= 12 || c == null || c.Type != "unit" || c.Faction != hero.Faction) continue;
                if (DeckRules.CannotAddToDeck(c)) continue;
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

        /// <summary>
        /// 🔴 **`A1896`（2026-10-19 · 用户拍板「方案 B」）：打开编辑器时**逐张摘除**不合法卡。**
        ///
        /// <para>判据 = 原版 `DeckEditingWindow__TryOpen`（`d:/2/tools/decomp_full/`，逐句实读）：
        /// `:41-45` 先 `new CardDeck(deckToCopy)` 拷副本 ⇒ `:47` `DeckUtility__RemoveInvalidCards`
        /// **对副本**逐张摘 ⇒ `:141` 把「摘过没有」写进 `CardDeck+0x60`（脏标记）。
        /// 全库**唯一**调用点就是这一处 —— 开局**不跑**（判据 `BattleManager__CreatePlayerDeck.c`）。</para>
        ///
        /// <para>⚠️ 与 `BattleScene` 那条「开局**不**摘除」是一对：两边都退回旧行为才可能同时绿
        /// （旧行为 = 开局摘、编辑器不摘 ⇒ 本函数 ① 红、那边 ② 红）。</para>
        /// </summary>
        static void TestLoadDeckPrunesInvalidCards()
        {
            var s = NewState();

            // 夹具：同一阵营里挑
            //   ① `bad` = **按原版不能进卡组**（`inventoryOptions`/`showInCollectionOptions` 那两道闸）
            //      且**引擎解析得了**（`TacticPlayable`）的战术卡 —— 后半个条件让「摘没摘」不会和
            //      「解析不了被 `FromDeck` 丢掉」混在一起（那是另一条判据）。
            //   ② `good` = 一张正常单位卡（证明摘除**只摘该摘的**）。
            CardDef hero = null, bad = null, good = null;
            foreach (var c in s.Pool)
                if (hero == null && c != null && c.Type == "hero") hero = c;
            if (hero == null) { CheckTrue(false, "（前提）池子里找得到一张督军"); return; }
            foreach (var c in s.Pool)
            {
                if (c == null || c.Faction != hero.Faction) continue;
                if (good == null && c.Type == "unit" && !DeckRules.CannotAddToDeck(c)) good = c;
                if (bad == null && c.Type == "tactic" && DeckRules.CannotAddToDeck(c)
                    && DeckBuilder.TacticPlayable(c)) bad = c;
            }
            CheckTrue(bad != null, "（前提）池里有一张「**按原版不能进卡组、但引擎解析得了**」的战术卡");
            CheckTrue(good != null, "（前提）同阵营有一张正常单位卡");
            // 🔴 **灭自证**：上面那两条前提**本身就是判别式** —— 谁把 `CannotAddToDeck` / `TacticPlayable`
            //   改成恒 false/true，这里先红；否则「摘掉了 1 张」会退化成同义反复（没东西可摘也照样绿）。
            if (bad != null)
                CheckTrue(DeckRules.CannotAddToDeck(bad) && DeckBuilder.TacticPlayable(bad),
                          $"🧨 灭自证：「{bad.Name}」确实**既不合法、又解析得了**（缺一条，这条用例就没有鉴别力）");
            if (bad == null || good == null) return;

            var src = new PlayerDeck("自检·带来路不明的牌", hero.Id, null,
                                     new List<string> { good.Id, bad.Id });
            int srcCount = src.CardIds.Count;

            s.LoadDeck(src);        // = 「打开编辑器」那一拍（`DeckRuntime.Build` 走的就是它）

            Check(s.LastLoadRemoved.Count, 1,
                  $"★ 打开编辑器按原版摘掉 **1** 张（实摘 {s.LastLoadRemoved.Count}）"
                + "｜🧨 改坏法：把 `LoadDeck` 里那句 `RemoveInvalidCards` 删掉 ⇒ 0 张 ⇒ 红");
            CheckTrue(s.Deck.CountOf(bad.Id) == 0,
                      $"★ 那张「{bad.Name}」（`{DeckRules.Describe(DeckError.EffectOnlyCard)}` 那一类）"
                    + "**不在编辑器副本里**（原版 `DeckEditingWindow__TryOpen.c:47`）");
            CheckTrue(s.Deck.CountOf(good.Id) == 1, "……其余合法的牌**一张不少**");

            // 🔴 **灭自证（第二条，结构上不同源）**：摘除只对**副本** —— 原版也是先 `new CardDeck(deckToCopy)`。
            //    改坏法：把 `RemoveInvalidCards(Deck, …)` 写成 `RemoveInvalidCards(deck, …)` ⇒ 源卡组被改 ⇒ 这两条红。
            Check(src.CardIds.Count, srcCount,
                  $"★ 灭自证：传进来那副**一个字节没动**（{src.CardIds.Count} 张，还是 {srcCount}）");
            CheckTrue(src.CountOf(bad.Id) == 1,
                      "……而且那张不合法牌**还在源卡组里**（摘的确实是副本，不是库/存档那份）");
        }

        /// <summary>卡组库接进场景之后，这几件事必须成立。
        ///
        /// <para>🔴 **2026-10-22（`W62`）口径改了**：空库时 `Build` **不再**替玩家建一套并落盘。
        /// 判据（原版）= `SelectDecksTab__CreateDeck.c:17`（`CardDeck.GetEmptyDeck()`）→ `:26`（打模式）→
        /// `:28`（`WindowsManager.OpenWindow&lt;DeckEditingWindow&gt;`）—— **全链没有一个入库调用**；
        /// 入库唯一路径 = `DeckEditingWindow__TrySaveDeck.c:100 UploadDeck`（先 `:80 ValidateDeck`，
        /// 不合法 `:81-98` 弹窗后 `return`）。⇒ 原来那三条（「空库时会建一套 / 有选中 / 建出来的不是空的」）
        /// 断的是**我们自己的旧副作用**，已按铁律 5 换成**新不变式**：
        /// 库仍然 0 套 · 没有选中 · **存档一个字节都没写** · 编辑器里**有东西可编辑**（演示牌只是内存载体）。
        /// ⚠️ 本方法**只在空库那一拍调用一次**（必须赶在夹具建库之前 —— 见 `RunCore` 的调用点）。</para>
        /// 🧨 改坏法：`DeckRuntime.Build` 里加回 `Library.Create` + `CommitCurrent` ⇒ 前三条红。</summary>
        static void TestLibraryWiring()
        {
            var lib = DeckLibrary.Load();
            Check(lib.Count, 0, $"★ W62：空库时**库里仍然是 0 套**（实际 {lib.Count} 套）"
                + "｜🧨 改坏法：`DeckRuntime.Build` 里加回「空库 `Library.Create` + `CommitCurrent`」⇒ 这条红");
            CheckTrue(lib.Current == null,
                      "★ W62：……也**没有「选中的卡组」**（空库 = 没有当前那套；原版这时走的是「新建卡组」）");
            CheckTrue(!File.Exists(RuleEngine.DeckStore.Path),
                      "★ W62：……而且**一个字节都没写存档**（`WarpforgeDecks.json` 不存在）"
                    + " —— 改前它一开编辑器就被静默写出来（Birth=00:09:26 那次实测）");
            Check(_rt.Library.Count, 0, "★ W62：……运行期那一刻的库也是 0 套（`_rt.Library` 与盘上一致）");
            CheckTrue(_rt.Library.Current == null, "★ W62：……`_rt.Library.Current` 也是 null（没有选中项）");
            // ★ 另一半（**缺了它，「不写盘」可以靠「干脆什么都不建」蒙过去**）：
            //   编辑器里仍然有东西可编辑 —— 演示牌只当**未入库**的内存载体。
            var d = _rt.State.Deck;
            CheckTrue(d != null && (d.CardIds.Count > 0 || !string.IsNullOrEmpty(d.WarlordId)),
                      "★ W62：……但编辑器里**有东西可编辑**（演示牌只当**尚未入库**的内存载体："
                    + (d == null ? "`State.Deck = null`"
                                 : $"{d.CardIds.Count} 张 / 督军 {d.WarlordId ?? "<无>"}") + "）");
            CheckTrue(d == null || !ReferenceEquals(d, _rt.Library.Current),
                      "★ W62：……而且那份载体**不是库里那个对象**（`State.Deck` 是 `LoadDeck` 装出来的副本；"
                    + "库空时 `Library.Current` 是 null）—— 否则任何库级 `Save()` 都会把它顺手写盘（A397 那条不变式）");
            // ⚠️ 断**满编**、不断「`DeckCount == Deck.CardIds.Count`」—— 那是同一个表达式的同义反复
            //   （`DeckEditorState.DeckCount` 就是 `Deck.CardIds.Count`），零鉴别力。
            Check(_rt.State.DeckCount, DeckRules.ClassicCards,
                  $"★ W62：……而且是**合法满编**那一副（{_rt.State.DeckCount}/{DeckRules.ClassicCards} 张，"
                + "不是一副空壳；空壳的话下面按 `Done` 会被 `ValidateDeck` 挡住，整条夹具链就断了）");
        }

        /// <summary>🔴 **2026-10-22（`W62`）**：验「**打开编辑器不写盘 · 按 `Done` 才入库落盘**」，
        /// 并**由它把夹具那套造出来 + 落盘**（从这一拍往下各节的前提：库里有一套）。
        ///
        /// <para>判据（原版）：打开 = `SelectDecksTab__CreateDeck.c:17,26,28`（**无入库调用**）；
        /// 入库 = `DeckEditingWindow__TrySaveDeck.c:80 ValidateDeck` → `:100 UploadDeck` →
        /// `DeckEditingWindow__UploadDeck.c:63 CardDeck.CopyDeck` + `:74 UploadUnsyncedDeck`。</para>
        ///
        /// <para>🧨 **灭自证**：「打开不写」与「`Done` 才写」这两条**必须成对** ——
        /// 只留后者 ⇒ 一个「打开就写」的实现照样绿；只留前者 ⇒ 一个「永远不写」的实现照样绿。</para></summary>
        static void TestLibraryAfterDone()
        {
            var path = RuleEngine.DeckStore.Path;
            CheckTrue(!File.Exists(path), "（前提）此刻存档仍不存在（`DeleteFile()` 之后一个字节都没写）");
            Check(_rt.Library.Count, 0, "（前提）此刻库里 0 套（空库那一拍）");
            Check(_rt.State.Validate(), DeckError.None,
                  "（前提）开场那副演示牌**合法** ⇒ 下面按 `Done` 会放行（原版 `__TrySaveDeck.c:80` 那道闸）");

            // ---- ① 按 `Done`（= 原版 `TrySaveDeck` 成功支）⇒ **这时才**入库 + 落盘 ----
            _rt.UiPressDone();
            Check(_rt.Library.Count, 1,
                  "★ W62：按 `Done` ⇒ **这时才**入库（`Library.Insert` = 原版 `UploadUnsyncedDeck`）");
            CheckTrue(File.Exists(path), "★ W62：……而且**这时才**出现 `WarpforgeDecks.json`");
            var disk = DeckLibrary.Load();
            Check(disk.Count, 1, "★ ……重读存档：库里恰好那 1 套（落盘往返）");
            Check(disk.Current != null ? disk.Current.Name : "(null)",
                  _rt.State.Deck != null ? _rt.State.Deck.Name : "(null)",
                  "★ ……盘上那套就是刚按 `Done` 存下去的这一副（名字一致）");

            // ---- ② 库里有卡组时，**再开一次编辑器也不写盘**（存档 mtime 一字未变）----
            DateTime mtime0 = File.GetLastWriteTimeUtc(path);
            var rgo = new GameObject("W62ReopenSmoke");
            var rrt = rgo.AddComponent<DeckRuntime>();
            rrt.Build(DeckLibrary.Load());
            CheckTrue(rrt.State != null && rrt.State.Deck != null, "（前提）重开也装进了那套（不是空的）");
            Check(File.GetLastWriteTimeUtc(path), mtime0,
                  "★ W62：**再开一次编辑器不写盘**（存档 mtime 一字未变）"
                + "｜🧨 让 `Build` 顺手 `Save()`（或重新建库）⇒ 这条红");
            Check(DeckLibrary.Load().Count, 1, "★ ……而且库也没多没少");
            UnityEngine.Object.DestroyImmediate(rgo);

            // ---- ③ 落盘往返（原来这一段是以「生产替你建的那套」为前提，现挪到**夹具这套**上）----
            var lib = _rt.Library;
            int n0 = lib.Count;
            string fixtureName = lib.Current != null ? lib.Current.Name : null;
            CheckTrue(!string.IsNullOrEmpty(fixtureName), "（前提）夹具那套有名字");
            lib.Create("第二套");
            Check(lib.Count, n0 + 1, "新建一套后库里多了一条");
            var lib2 = DeckLibrary.Load();
            Check(lib2.Count, n0 + 1, "**新建立刻落盘**（重新载入还在）");
            Check(lib2.Current != null ? lib2.Current.Name : "(null)", "第二套", "选中项也落盘了");
            // 删回夹具那套 —— ⚠️ 用 `_rt.Library` **自己**删（内存与盘一起回）；
            //   ⛔ 别拿 `lib2` 删：那只改盘，`_rt.Library` 会与盘长期不一致，后面各节全遭殃。
            lib.Delete(lib.Count - 1);
            Check(DeckLibrary.Load().Count, n0, "删除也落盘了");
            Check(_rt.Library.Current != null ? _rt.Library.Current.Name : "(null)", fixtureName,
                  "★ ……而且库里当前又回到夹具那套（后续各节的前提）");
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
            // 🔴 **2026-10-14（`DIAG_deck七条红.md` ②③）：费用圆那一族【原版本来就是互相压着的】。**
            //   圆 = **61.7** 见方（`DeckRuntime.cs` 的 `RowCostS`：原版 prefab `Cost Image` 锚
            //   `(0.03,0)/(0.03,1)` + `m_SizeDelta=(0,6)` ⇒ 高 = `RowH 55.7 + 6 = 61.7`，**垂直居中**
            //   ⇒ 上下各溢出 3px），而**行距 = 57.7**（`RowH 55.7 + m_Spacing 2.0`）
            //   ⇒ **相邻两行的圆重叠 `61.7 − 57.7 = 4.0px`** —— 这是原版设计，不是版面错
            //   （12 个槽 = 11 对相邻行；**非相邻对**间隙 `+53.7`，不重合）。
            //   ⇒ ① 给这一对**加「本来就是压着的」豁免**（照上面 `Resource Bar Background` 那条的写法）；
            //      ② **同时把这一族移出 `tightest`** —— 那条是「同层最紧的一对 = 相邻页签格、恰好相接」
            //         的**校准尺**（`tab_ic{i}` 宽 `TabCellW`、相邻格左缘 `TabsX + TabCellW*i` ⇒ 间隙恒 0），
            //         被 −4.0 的 `row_c*` 抢走之后尺子就废了。⛔ **别把期望值改成 −4.0**（那是把尺子废掉），
            //         也⛔ **别把 `RowCostS` 改小** —— `DeckScene.cs` 的 `A1735③` 正钉着它必须是 **61.7**
            //         （prefab 实读）⇒ 两条打架时按原版判据。
            //   ⚠️ 名字判据 =「`row_c` + **全数字**」，⛔ **不是** `StartsWith("row_c")` ——
            //     圆里那个数 `row_cnt*` 也吃那个前缀（今天它是 `Label`、不在本扫描里，但别留这个口子）。
            //   🧨 白名单只放这一族：别的任何同层重叠照旧报红。
            bool IsRowCostQuad(string n)
            {
                if (n == null || n.Length <= 5 || !n.StartsWith("row_c", StringComparison.Ordinal)) return false;
                for (int k = 5; k < n.Length; k++) if (n[k] < '0' || n[k] > '9') return false;
                return true;
            }
            for (int i = 0; i < quads.Length; i++)
                for (int j = i + 1; j < quads.Length; j++)
                {
                    var A = quads[i]; var B = quads[j];
                    if (A.RenderQueue != B.RenderQueue) continue;
                    // 🔴 **2026-10-11（`A1521③` · 代理 `N`/`O` 的判据）**：**货币条那两颗【原版就是重叠的】** ——
                    //   药丸格内 x `[20, 20+药丸宽]`、图标格内 x `[−8.5, 51.5]` ⇒ **重叠 31.5 px**，
                    //   而药丸的九宫 `pad(L29,…)` 那条左内衬**正是给图标留的位**（原版那颗 `Icon` 带
                    //   `LayoutElement.m_IgnoreLayout = 1`、**不进布局**）。⇒ 这一对**按原版就是压着的**，
                    //   ⛔ 不是版面错。实测差 = `−31.0001`，与算式吻合 ⇒ 加这条白名单。
                    //   🧨 **白名单只放这一对**（具名 + 前缀匹配）：别的任何同层重叠照旧报红。
                    bool byDesign = (A.name == "Image" || B.name == "Image")
                                 && (A.name.StartsWith("Resource Bar Background") || B.name.StartsWith("Resource Bar Background"));
                    // 费用圆那一族：**相邻两行原版就压着 4px**（见上面 `IsRowCostQuad` 那段）。
                    // ⚠️ 要求**两颗都是**费用圆 —— 圆与别的件真压住 = 版面错，照旧报红。
                    bool rowCostPair = IsRowCostQuad(A.name) && IsRowCostQuad(B.name);
                    if (byDesign || rowCostPair) continue;
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
            // 🔴 **2026-10-14（`DIAG_deck七条红.md` ③）**：这一族**必须把 `row_c*` 排除掉**
            //    （上面那段同名豁免已经排除）—— 它们**原版就压着 4px**（61.7 圆 ÷ 57.7 行距），
            //    一进 `tightest` 就把尺子抢走（实测 **−4.0000**）⇒ 这条会红在「尺子被抢」上、**不是版面错**。
            //    ⛔ **别把期望值改成 −4.0**（那是把尺子废掉）。
            CheckNear(tightest, 0f, LayTol,
                      $"同层最紧的一对（{tightestPair}）**恰好相接**、px 间隙 = 0"
                      + "（判据 = 原版相邻页签格边缘相接；这条同时是「判据的两个量都是 px」的校准"
                      + "｜⚠️ 费用圆那一族已排除 —— 它原版就压着 4px）");

            CheckTrue(state.VisibleCards().Count > 0, "版面用的状态里有卡可显示");

            // ============================================================ 新版面（2026-09-20 按权威坐标重建）
            // 🔴 素材齐不齐 —— **缺图会静默变白**，这条挡住它（`sync_battle_ui_art.py` 那批）
            var missing = _rt.MissingArt;
            Check(missing.Count, 0, "原版 UI 图**一张不缺**（缺了会画成纯白占位）" +
                  (missing.Count == 0 ? "" : "—— 缺 " + string.Join("、", missing)));

            // Header 四件 + 侧栏 + Footer：逐件点验「真的建了」
            foreach (var k in new[] { "hdr_sep", "hdr_back", "hdr_fltbtn", "hdr_clear", "hdr_wcbg", "hdr_army" })
                CheckTrue(_rt.UiHasQuad(k), $"Header 的 `{k}` 建起来了");

            // 🆕 **2026-10-17（A893）**：页头那颗 `hdr_army` 贴的必须是【**这副卡组所属阵营的徽记**】。
            //   改之前喂 `FactionIcon(null)` ⇒ 拿到的是**卡组图标** `40k_collection_bt_decks`，
            //   而**收藏窗同一位置**那颗（同属 `Wildcard Display` 的第三个孩子 `Army Icon`）是阵营徽记
            //   ⇒ **同一位置两个图标不一致**（A893 的开单理由）。
            //   判据（三层）→ `DeckRuntime.BuildHeader` 那一行 + `DeckArmyIcon` 的 doc：
            //   ① 原版 prefab 出厂值 = `40k_DeckSelection_icon_FactionBlackLegion`（`m_PreserveAspect = 1`）；
            //   ② 运行期 `DeckEditingWindow__FilterToDeck.c:24-28` 把它换成 `EditingDeck.deckArmy` 的徽记
            //      （`deckArmy == 0` ⇒ 原版那个字面量 **10 = Ultramarines**）；
            //   ③ ⇒ 「照原版」= **按卡组阵营换**，不是保留出厂那张。
            {
                string wlId0 = _rt.State.Deck.WarlordId;
                var wlDef0 = string.IsNullOrEmpty(wlId0) ? null : _rt.State.Find(wlId0);
                // ⚠️ 期望值从**卡组状态**独立算：阵营走**现有那道** `_rt.FactionOf`（id→阵营，全工程唯一一份），
                //   图走 `DeckRuntime.FactionIcon`（13 个阵营的**唯一一份**映射）——
                //   ⛔ 不从 `hdr_army` 自己读回来（那就是自证）。
                string fac0 = _rt.FactionOf(wlId0);
                if (string.IsNullOrEmpty(fac0)) fac0 = DeckRuntime.NoArmyFaction;   // 「还没有阵营」= 原版 `deckArmy == 0` 那一档
                string want = DeckRuntime.FactionIcon(fac0);
                string wlLabel = wlDef0 != null ? (wlDef0.Name + " / " + fac0)
                                                : (string.IsNullOrEmpty(wlId0) ? "还没选 ⇒ Ultramarines（原版 `FilterToDeck` 那个字面量 10）"
                                                                               : "查不到 `" + wlId0 + "` ⇒ 按原版那一档走");
                Check(_rt.UiTextureName("hdr_army"), want,
                      $"★ 页头 `hdr_army` 贴的是**卡组阵营的徽记** `{want}`（督军 = " + wlLabel + "）");
                // 🔴 **判别式（硬判据，不靠上面那条重算）**：这一格**不是**卡组图标 ——
                //   改回 `FactionIcon(null)`（= `40k_collection_bt_decks`）⇒ 本条必红。
                CheckTrue(_rt.UiTextureName("hdr_army") != "40k_collection_bt_decks",
                          "★ **判别式**：`hdr_army` 贴的**不是卡组图标** `40k_collection_bt_decks`"
                        + "（A893 改之前喂的正是它）");
                CheckTrue(want != DeckRuntime.FactionIcon(null),
                          "（前提）本卡组那个阵营的徽记 ≠ 卡组图标 ⇒ 上面那两条真的分得开");
                // ② **换阵营 ⇒ 徽记跟着换**（原版 `RefreshCards → FilterToDeck` 那条链）。
                //    白盒直接改 `WarlordId` 再刷新：⛔ **不走 `State.SetWarlord`** —— 它会**清掉**不合阵营的卡、
                //    把后面所有断言带偏；测完**原样放回**并断言放回了。
                string otherId = null, otherFac = null;
                foreach (var c in _rt.State.Pool)
                {
                    if (c == null || c.Type != "hero") continue;
                    if (wlDef0 != null && c.Faction == wlDef0.Faction) continue;   // 要**另一个阵营**的督军
                    otherId = c.Id; otherFac = c.Faction; break;
                }
                if (otherId != null)
                {
                    _rt.State.Deck.WarlordId = otherId;
                    _rt.RefreshAll();
                    Check(_rt.UiTextureName("hdr_army"), DeckRuntime.FactionIcon(otherFac),
                          $"★ 换成 `{otherFac}` 的督军 ⇒ `hdr_army` 跟着换成 **`{DeckRuntime.FactionIcon(otherFac)}`**"
                        + "（原版 `FilterToDeck` 就是拿 `EditingDeck.deckArmy` 喂 `WildcardDisplay.Initialize`）");
                    _rt.State.Deck.WarlordId = string.IsNullOrEmpty(wlId0) ? null : wlId0;
                    _rt.RefreshAll();
                    Check(_rt.State.Deck.WarlordId, wlId0, "（把督军原样放回 —— 后面的断言仍按原状态跑）");
                }
                else CheckTrue(true, "（本池子里只有一个阵营的督军 ⇒ 跳过「换阵营跟着换图」那一条）");

                // ---- 🔴 2026-10-21（`A1145`）：给那 13 条映射补一条【独立腿】----
                // 上面三条的期望值都取自 `DeckRuntime.FactionIcon` **自己** ⇒ 它们只证明
                // 「视图用了那条映射」，**证不了映射本身对不对**（`switch` 少一个 `case` 会**静默**掉进
                // `default` = 卡组图标 `40k_collection_bt_decks`，而那三种情况上面一条都抓不到：
                // 本卡组那个阵营、换过去那个阵营，恰好都不是漏的那个）。
                // 这里断的是**结构性质**（期望值不来自实现）：卡池里出现过的每个阵营
                //   ① 都得有**非空**的图名；② 都**不得**是那个 `default` 兜底；③ 两两**不同**。
                // ⚠️ 如实登记：它仍**不是**「图名与阵营一一对齐」的证明（那要拿原版 13 张
                //   `40k_DeckSelection_icon_*` 的名字逐条核，本件没做）—— 它挡的是「漏 case 静默兜底」。
                {
                    var seen = new System.Collections.Generic.Dictionary<string, string>();
                    string dup = null, miss = null, fallback = null;
                    foreach (var c in _rt.State.Pool)
                    {
                        if (c == null || c.Type != "hero" || string.IsNullOrEmpty(c.Faction)) continue;
                        string ic = DeckRuntime.FactionIcon(c.Faction);
                        if (string.IsNullOrEmpty(ic)) { if (miss == null) miss = c.Faction + " ⇒ 空"; continue; }
                        if (ic == "40k_collection_bt_decks") { if (fallback == null) fallback = c.Faction; continue; }
                        string prev;
                        if (seen.TryGetValue(ic, out prev) && prev != c.Faction && dup == null) dup = prev + " / " + c.Faction + " ⇒ " + ic;
                        seen[ic] = c.Faction;
                    }
                    CheckTrue(miss == null,
                              "★ `FactionIcon`：卡池里出现过的阵营**每个都有图名**（实测 " + seen.Count + " 个）"
                            + (miss == null ? "" : "｜🧨 " + miss));
                    CheckTrue(fallback == null,
                              "★ ……没有一个阵营掉进 `default`（= 卡组图标）兜底"
                            + (fallback == null ? "" : "｜🧨 漏了 `case \"" + fallback + "\"` ⇒ 静默贴卡组图标（原版 `hdr_army` 那一格是阵营徽记）"));
                    CheckTrue(dup == null,
                              "★ ……不同阵营拿到的徽记**两两不同**"
                            + (dup == null ? "" : "｜🧨 " + dup));
                }
            }

            foreach (var k in new[] { "side_bg", "tab_hi0", "tab_ic0", "name_bg", "name_clear", "foot_done", "foot_ic" })
                CheckTrue(_rt.UiHasQuad(k), $"`{k}` 建起来了");
            for (int i = 0; i < 11; i++)
                if (!_rt.UiHasQuad("row_" + i)) { Check(true, false, $"卡组列表第 {i} 行没建起来"); break; }

            // 关键 rect **逐值**对权威表（`_deck_editing_godot_rects.txt`）——
            // 这几个数是「按原版复刻」的硬判据，抄错了这条会红
            CheckRect("hdr_fltbtn", 367.2f, 88.5f, 50f, 50f);
            // 🔴 **2026-10-22（`D1`）就地订正（铁律 5）**：这里原来断的是 `1218.6, 83.5, 250×60`
            //    （「容器内右对齐」= 1468.6 − 250）—— **两个数都不是原版布局结果**，已按原版改。
            //    判据 = 父容器 `Header/Filters`（592.2,71.0→1468.6,156.0 · `GridLayoutGroup`
            //    cell **85×70** · pad 0 · `align=3` MiddleLeft · `constraint=2` FixedRowCount 1 ⇒ 1 行）
            //    ⇒ uGUI 跑完 = **85×70 @ (592.2, 78.5)**（x = 容器左沿 + 0；y = 71 + (85−70)/2）。
            //    ⚠️ dump 那行仍印 1488.6 / 250×60 —— 那是**布局前的模板位**（Grid 系 `menu_dump` 不套算）。
            //    ⛔ 别改回 1218.6（那会把钮摆到四个稀有度计数那一带）。
            CheckRect("hdr_clear", 592.2f, 78.5f, 85f, 70f);
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
            // 🔴 **2026-10-21（`A1720`）：这一段的前提必须先摆出来** —— 下面量的 `row_0` / `row_g0` 是
            //    **槽 0 的【督军档】那一套**，而自 `A1720` 起槽 0 有**两套件**、由「这一行装的是不是督军」二选一：
            //    **滚过一行之后**槽 0 装的是卡行 ⇒ 这一套会被关掉、换成 `row_0c` / `row_g0c`（卡行档几何）。
            //    ⇒ 先把滚动摆回顶（`firstRow == 0`），并把这条前提**单独断一条**（前提破时说得清是哪一半）。
            _rt.UiSetTab(0);                     // Cards 页（`SetTab` 会把 `_deckScroll` 归 0）
            _rt.UiScrollDeck(-9999f);            // 再显式滚回顶（`firstRow == 0`）
            CheckTrue(_rt.UiQuadActive("row_0"),
                      "（前提）槽 0 现在装的是**督军** ⇒ 量到的是**督军档**那一套（`row_0` 九宫格开着）");
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
                    // 🔴 **2026-10-21（`A1700`）就地订正（铁律 5）**：这一对期望值原来是 **128.05 / 261.38**
                    //   —— 那是**我们自己的两个常量**算出来的（`GradFrac 0.606 × 抽屉宽 325` 与抽屉右缘 325.4），
                    //   **既是自证、数值也不对**（右缘多 8px、`0.606` 还把卡行那个**相对 `Background` 的锚**
                    //   套到了抽屉宽上）。现改成**原版行 0（督军行）prefab** 的 `Rarity Gradient`：
                    //   锚 `0.611473560` + `sd.x −0.020004` ⇒ 绝对 **197.364 .. 317.399**（宽 **120.035**、中心 **257.38**）。
                    //   判据全文 → `DeckRuntime.RowGradHeroX` 那一段（含卡行那一档 201.927..317.400 / 宽 115.473）。
                    Check(Mathf.Abs(gw - 120.035f) < 0.6f, true,
                          $"色条宽 = **120.04**（原版行 0 `Rarity Gradient` 的锚算出来；"
                        + "原来钉的 128.05 是我们自己两个常量算的 —— 自证且右缘多 8px）");
                    Check(Mathf.Abs(gx - 257.38f) < 0.6f, true,
                          $"色条中心 x = **257.38**（右对齐：197.364 + 120.035/2）");
                    // 🔴 **2026-10-21（`A1700`）新加**：**卡行那一档**原来**一条断言都没有**
                    //   （A1700 之前我们所有行共用一个矩形，根本没有「两档」这回事）。
                    //   期望值 = 原版 `Deck Selector Card Info button` 的 `Rarity Gradient`
                    //   （锚 `0.605750263` + `sd.x 0.4528`、父 = `Background` 291.745 宽）⇒ 绝对
                    //   **201.927 .. 317.400**（宽 **115.473**、中心 **259.6635**）。判据全文 → `DeckRuntime.RowGradCardX`。
                    //   ⚠️ `row_g1` 是**普通 quad**（不是九宫格）⇒ `UiQuadRect` 的语义与 `row_g0` 同一条。
                    //   ⛔ 期望值写死（原版锚算出来的），⛔ 不从 `DeckRuntime.RowGradCardX` 读回来。
                    float g1x, g1y, g1w, g1h;
                    if (_rt.UiQuadRect("row_g1", out g1x, out g1y, out g1w, out g1h))
                    {
                        Check(Mathf.Abs(g1w - 115.473f) < 0.6f, true,
                              $"卡行色条宽 = **115.47**（原版卡行 `Rarity Gradient` 的锚算出来；实测 {g1w:F2}）");
                        Check(Mathf.Abs(g1x - 259.6635f) < 0.6f, true,
                              $"卡行色条中心 x = **259.66**（201.927 + 115.473/2；实测 {g1x:F2}）");
                    }
                }
            }

            // ============================================================ 🔴 2026-10-21（第二十四会话）
            // **卡组行几何四笔**（`A1722` 费用框 · `A1721` 行距 · `A1727` 视口裁切 · `A1720` 槽 0 两套件）。
            // 判据逐条 → `Deck/DeckRuntime.cs` 的 `RowCostX` / `RowSpacing` / `RowPitch` / `RowVisible` /
            //    `DeckListBand` / `RowBgRect` 那几段（原版 prefab 现读 + 参考图对账）。
            // ⛔ 期望值一律是**原版算式算出来的字面量**，不从我们自己的常量读回来（那是自证）。
            Section("卡组行几何四笔（`A1722` 费用框 · `A1721` 行距 · `A1727` 视口裁切 · `A1720` 槽 0 两套件）");
            {
                _rt.UiSetTab(0);                      // Cards 页（`SetTab` 顺带把 `_deckScroll` 归 0）
                _rt.UiScrollDeck(-9999f);             // 显式滚到顶（`firstRow == 0`）—— 后面每一笔都要这个基线
                var entries4 = _rt.DeckEntries();
                CheckTrue(entries4.Count > _rt.DeckRowVisible + 1,
                          $"（前提）卡组条目 {entries4.Count} 条 > 一屏槽数 {_rt.DeckRowVisible} + 1"
                        + "（才滚得动、且量得到**被裁的那一行**）");

                // ---------- ① `A1722`：费用图的【框】（不是墨迹）----------
                //   判据 = 原版卡行 `Cost Image`：父 `Background` 25.655..317.400（291.745 宽）· 锚 0.03 ·
                //   pos −0.1800079 · `AspectMode 2`（**高控宽**）· 高 = `RowH 55.7 + sizeDelta.y 6` = **61.7**
                //   ⇒ 中心 x = `25.655 + 0.03×291.745 − 0.1800079` = **34.227**、见方 **61.7**。
                //   ⛔ 不是我们那对旧常量 `12.1 / 46.2`（那是照着**实拍墨迹**量的 —— 墨迹左缘离框左缘还有
                //   约 3px 的透明发光边；右缘两法只差 0.12px ⇒ 是「框 vs 墨迹」、不是真偏离）。
                float c1x, c1y, c1w, c1h;
                CheckTrue(_rt.UiQuadRect("row_c1", out c1x, out c1y, out c1w, out c1h),
                          "（前提）行 1 的费用圆量得到矩形（它是普通 sprite，不是九宫格）");
                CheckNear(c1w, 61.7f, 0.05f,
                          $"★ 费用图的**框宽** = **61.7**（原版 `Cost Image` 高控宽、高 = 55.7 + 6；实测 {c1w:F2}）");
                CheckNear(c1h, 61.7f, 0.05f, $"★ ……**框高** = **61.7**（同上；实测 {c1h:F2}）");
                CheckNear(c1x, 34.227f, 0.05f,
                          $"★ ……框**中心 x** = **34.227**（25.655 + 0.03×291.745 − 0.1800079；实测 {c1x:F3}）"
                        + " —— 旧常量给的 35.2 是**墨迹**中心，框中心比它左 1px");

                // ---------- ② `A1721`：行距 57.7（55.7 + 2.0）· 且滚动上限是同一条式子 ----------
                float r0x, r0y, r0w, r0h, r1x, r1y, r1w, r1h;
                // ⚠️ 两次调用**分开写**（别写成 `a && b`）：短路求值下编译器认不出 `out` 一定赋过值。
                bool haveRow01 = _rt.UiRowRect(0, out r0x, out r0y, out r0w, out r0h);
                haveRow01 &= _rt.UiRowRect(1, out r1x, out r1y, out r1w, out r1h);
                CheckTrue(haveRow01, "（前提）前两行的**行矩形**量得到");
                CheckNear(r1y - r0y, 57.7f, 0.01f,
                          $"★ 行距（步长）= **57.7** = 行高 55.7 + 间距 **2.0**"
                        + "（原版 `Content` 的 `VerticalLayoutGroup.m_Spacing`；三行打在 366/423.7/481.4；"
                        + $"实测 {r1y - r0y:F2}）—— ⛔ 不是旧那个「我们挑的」56");
                CheckNear(r0h, 55.7f, 0.01f, $"……行高仍是 **55.7**（实测 {r0h:F2}）");
                // 🔴 上限那条**必须与行距一起断**：只改行距不改上限 ⇒ 多给（旧版是少给）2px，
                //   而上面那条断的是「步长」、这条断的是「能滚多远」—— 两条并存才挡得住「只改一处」。
                float vx1, vy1, vx2, vy2;
                _rt.UiDeckBand(out vx1, out vy1, out vx2, out vy2);   // 视口（自检口读，别在自检里另写 366/644.1）
                CheckNear(_rt.MaxDeckScrollPx, entries4.Count * 57.7f - 2f - (vy2 - vy1), 0.05f,
                          $"★ 滚动上限 = **内容高 − 视口高** = `n×57.7 − 2.0 − (视口高 {vy2 - vy1:F1})`"
                        + $"（n = {entries4.Count}；实测 {_rt.MaxDeckScrollPx:F2}）"
                        + " —— 上限只减一个 `m_Spacing`（`n−1` 个里的那一个），⛔ 别写成 `n×57.7 − 视口高`（多 2px）");

                // ---------- ③ `A1727`：一屏 12 个槽 + 视口裁切 ----------
                //   旧值 11 是一条**用户可见的真缺陷**：① 卷到 0 时第 12 行只露 9.4px、我们不画；
                //   ② `max scroll` 时最后一行**一个槽都没有**（`firstRow_max = Count−12`）。
                _rt.UiScrollDeck(-9999f);
                CheckNear(_rt.DeckRowVisible, 12f, 0.01f, "★ 一屏槽数 = **12**（`A1727` 之前是 11）");
                float q0x, q0y, q0w, q0h, q11x, q11y, q11w, q11h;
                CheckTrue(_rt.UiQuadRect("row_g0", out q0x, out q0y, out q0w, out q0h), "（前提）行 0 的色条量得到");
                CheckTrue(_rt.UiQuadRect("row_g11", out q11x, out q11y, out q11w, out q11h),
                          $"（前提）**第 12 行**（`row_g11`）量得到 —— 槽数还是 11 时这一句先红（现在是 {_rt.DeckRowVisible}）");
                CheckNear(q0h, 55.7f, 0.05f,
                          $"★ 滚到顶：**视口内**那一行（行 0）色条 = **整行高 55.7**（没被裁；实测 {q0h:F2}）");
                CheckNear(q11h, 9.4f, 0.05f,
                          $"★ 滚到顶：**视口外**那一行（行 11）被裁到**只露 9.4px**"
                        + $"（= 视口底 {vy2:F1} − 行 11 顶 {366f + 11f * 57.7f:F1}；实测 {q11h:F2}）"
                        + " —— 原版参考图上那颗「只高 6px 的蓝六边形」就是这一刀。"
                        + "🔑 **两条一起断才有牙**：只改常量不接裁切 ⇒ 这里 55.7；只接裁切不加槽 ⇒ 上一句先红");
                // 射线那一半：原版视口上那颗是 **`RectMask2D`（四边都裁：渲染走 `IClipper`、
                // 射线走 `IsRaycastLocationValid`）**，所以我们那条命中判定也要吃同一个框。
                // 滚 20px ⇒ 行 0 的**未裁切**矩形伸进页头 20px ⇒ 那一带必须**判不中任何行**。
                _rt.UiScrollDeck(20f);
                Check(_rt.UiRowHoverAt(100f, vy1 - 8f), -1,
                      "★ 视口**外**（页头那一带）的指针对任何行都**不命中**"
                    + "（判据 = 原版视口那颗 `RectMask2D` **也裁射线**；⛔ 删掉射线那道闸 ⇒ 这一条变 0）");
                Check(_rt.UiRowHoverAt(100f, vy1 + 8f), 0,
                      "★ ……而视口**内**同一列（行 0 顶带）命中**第 0 行**（两条成对 ⇒ 上一条不是「恒不命中」）");
                _rt.UiRowHoverAt(1700f, 500f);        // 指针移开（`_hoverRow` 归 -1）
                // 滚到底：最后那一项必须**真的画出来**（旧写法里它算出来的 y 在视口内，只是**没槽**）
                _rt.UiScrollDeck(9999f);
                Check(_rt.UiDeckRowAt(_rt.DeckRowVisible - 1), entries4[entries4.Count - 1],
                      "★ 滚到底：**最后一个槽**装的是卡组**最后一项**（`A1727` 之前它一个槽都没有 —— "
                    + "`firstRow_max = Count−12`、槽 0..10 只覆盖到 `Count−2`）");
                CheckTrue(_rt.UiQuadRect("row_g11", out q11x, out q11y, out q11w, out q11h),
                          "（前提）滚到底后 `row_g11` 仍量得到");
                // 🔴 **2026-10-14（`DIAG_deck七条红.md` ④ · (α) 断言自己用错了量）：**
                //   原来写的是 `q11y >= vy1 && q11y + q11h <= vy2` —— 把 `UiQuadRect` 的 **中心 y
                //   当成了上缘**（它的出参是 `(cx, cy, w, h)`，`cy` 是**中心**：`DeckRuntime.cs`
                //   `ToPx(q.transform.position)`；同文件上面那句「中心 x = 162.9」也是这个语义）
                //   ⇒ 后半条凭空多算了半行高、**几何本来是对的**却报红。**错因**：把「中心 + 高」
                //   当成了「上缘 + 下缘」两把尺子混用。
                //   ⇒ 上下缘一律 `cy ∓ h/2`。⚠️ 后半条余量只有 **0.05px**（行 18 的底 = 1010.1 = 视口底，
                //   正好装下）⇒ **容差别收紧**。
                float q11t = q11y - q11h * 0.5f, q11b = q11y + q11h * 0.5f;
                CheckTrue(q11t >= vy1 - 0.05f && q11b <= vy2 + 0.05f,
                          $"★ 滚到底：最后一行**整块落在视口内**（y {q11t:F1}..{q11b:F1} ⊂ [{vy1:F1}, {vy2:F1}]）"
                        + " —— 裁切把所有行都夹进了框里（⛔ 这一条与上面「只露 9.4px」是同一条裁切的**两个方向**）");
                CheckNear(q11h, 55.7f, 0.05f, $"……而且它是**整行高 55.7**（没被白裁；实测 {q11h:F2}）");
                _rt.UiScrollDeck(-9999f);             // 摆回顶（下一个前提要用）

                // ---------- ④ `A1720`：槽 0 两套件（几何跟**装进去的那一项**走，不是跟槽号走）----------
                float b0x, b0y, b0w, b0h;
                CheckTrue(_rt.UiQuadRect("row_0", out b0x, out b0y, out b0w, out b0h), "（前提）槽 0 的底板量得到");
                CheckNear(b0w, 309f, 0.6f,
                          $"（前提）滚到顶 ⇒ 槽 0 装的是**督军**：底板宽 = **309**（原版 `Deck Selector Hero Card"
                        + $" Info button` 的 `Background` 锚 `(0,0)-(1,1)`；实测 {b0w:F2}）");
                CheckNear(b0x, 162.9f, 0.6f, $"……中心 x = **162.9**（8.400 + 309/2；实测 {b0x:F2}）");
                _rt.UiScrollDeck(57.8f);              // 滚过一整行 ⇒ `firstRow == 1` ⇒ 槽 0 装的是**卡行**
                CheckTrue(_rt.UiHasQuad("row_0"),
                          "（前提）`row_0` **建出来了** —— 不然下面那条「关掉了」是拿「件不存在」蒙的"
                        + "（`UiQuadActive` 对不存在的 key 恒答 false，账 `A1532`）");
                CheckTrue(!_rt.UiQuadActive("row_0"),
                          "★ `A1720`：滚过一行后**督军档那一套关掉了**"
                        + "（按槽号取几何的旧写法里它恒开着 ⇒ 底板左缘多露 17.255px）");
                CheckTrue(_rt.UiQuadActive("row_0c"), "★ ……而**卡行档**那一套（`row_0c`）开着");
                float bcx, bcy, bcw, bch;
                CheckTrue(_rt.UiQuadRect("row_0c", out bcx, out bcy, out bcw, out bch), "（前提）`row_0c` 量得到");
                CheckNear(bcw, 291.745f, 0.6f,
                          $"★ ……槽 0 底板这时是**卡行档**：宽 = **291.745**（原版卡行 `Background` 锚 `0.055841297`；"
                        + $"实测 {bcw:F2}；旧写法这里仍是 309 —— 差的就是那 17.255px 左缘）");
                CheckNear(bcx, 171.5275f, 0.6f, $"★ ……中心 x = **171.53**（25.655 + 291.745/2；实测 {bcx:F2}）");
                float gc0x, gc0y, gc0w, gc0h;
                if (_rt.UiQuadRect("row_g0c", out gc0x, out gc0y, out gc0w, out gc0h))
                {
                    CheckNear(gc0w, 115.473f, 0.6f, $"★ ……稀有度色条也换成卡行档：宽 = **115.47**（实测 {gc0w:F2}）");
                    CheckNear(gc0x, 259.6635f, 0.6f, $"★ ……色条中心 x = **259.66**（201.927 + 115.473/2；实测 {gc0x:F2}）");
                }
                else CheckTrue(false, "`row_0c` 那一套里 `row_g0c`（卡行档色条）量得到");
                _rt.UiScrollDeck(-9999f);             // 摆回顶（后面几节按「顶」来的）
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
                    // 🔴 **2026-10-17（A894）**：这一格的上限要**带卡型**算 —— 原版 `GetMaxCopiesInDeck`
                    //   那句 `if (cardType == 10) return 1;`（督军）**排在稀有度判断【之前】**
                    //   ⇒ **非传说督军也是 1**（判据：`decomp_full/Everguild.LiveOps.GameplayVariablesData__GetMaxCopiesInDeck.c:7-9`；
                    //   `cardType` 表 = `dump.cs:45710` 的 `CardTypeOptions.Hero = 10`）。
                    //   ⚠️ 期望值**独立算**（`DeckRules` 那条唯一判据），⛔ 不从 `PoolCounterText` 读回来。
                    int cap = DeckRules.CopyLimit(poolDef.Rarity, poolDef.Type);
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
                        // 🔴 **2026-10-17（A894）**：摘掉督军 ⇒ **整池卡都换了**（`WarlordGatedPool`：
                        //   没督军时**只列督军**）⇒ 第 0 格**换人了**，上限必须**按新的那张**重算。
                        //   ⚠️ 原来这里沿用**摘之前**那张卡的 `cap` —— 只在「两张恰好同档」时才碰巧对。
                        var noWlDef = _rt.UiPoolCellDef(0);
                        CheckTrue(noWlDef != null && noWlDef.Type == "hero",
                                  "（前提）没督军时卡池第 0 格上是**督军**（`WarlordGatedPool` 那条规格）");
                        if (noWlDef != null)
                        {
                            // 🔴 **2026-10-17（A894）—— 这就是它在屏幕上的那一处**：
                            //   没督军 ⇒ 整池**全是督军** ⇒ 每格的「张数」分母都必须是 **1**
                            //   （原版 `GetMaxCopiesInDeck` 那句 `if (cardType == 10) return 1;`，
                            //   **在稀有度判断之前**）。
                            //   🔴 **期望值直接钉 `"x1"`** —— ⛔ 不写成 `"x" + DeckRules.CopyLimit(...)`
                            //   （那是拿实现证明实现 = 自证；1 出自原版那句 `return 1`）。
                            //   🔴 **判别式**：这一池里**一定有非传说督军**（池子实测 28 张）
                            //   —— 「只看稀有度」那版会给它们印 `x2` ⇒ 下面 `bad` 那条必红。
                            int heroCells = 0, nonLeg = 0, bad = 0;
                            for (int k = 0; k < 40; k++)
                            {
                                var d = _rt.UiPoolCellDef(k);
                                if (d == null) break;                  // 视口里的格子建完了
                                if (d.Type != "hero") continue;
                                heroCells++;
                                if (!DeckRules.IsLegendary(d.Rarity)) nonLeg++;
                                string got = _rt.UiLabelText("poolcnt_" + k);
                                if (got != "x1")
                                {
                                    bad++;
                                    if (bad <= 3)
                                        Check(got, "x1", $"没督军时第 {k + 1} 格（督军 `{d.Name}` / {d.Rarity}）的张数");
                                }
                            }
                            CheckTrue(heroCells > 0, $"（前提）没督军时视口里有督军格（{heroCells} 格）");
                            CheckTrue(nonLeg > 0, $"（前提）其中**非传说督军 {nonLeg} 格** —— 上面那条「全 `x1`」才有判别力"
                                                + "（全是传说的话新旧写法同值）");
                            Check(bad, 0, $"{heroCells} 格督军的「张数」**每一条**都印 `x1`"
                                        + $"（原版 `cardType == 10 ⇒ 1`；只看稀有度那版会给这 {nonLeg} 格非传说的印 `x2`）");
                        }
                        _rt.State.SetWarlord(wl);
                        _rt.RefreshAll();
                        Check(_rt.State.Deck.WarlordId, wl, "（把督军放回去 —— 后面的断言仍按原状态跑）");
                    }
                }

                // 🔴🆕 **2026-10-18（卡池排序与显隐 · 「显示 + 变灰」那一档）**
                //   判据 = 原版 `DeckEditorCollectionDisplay__DrawCell.c:75-92` 把 `CanAddCard` 的结果当 `canAdd`
                //   传给 `CollectionCard__UpdateCardVisuals`，后者 `:19-20` 的
                //   `UIImageGreyscaleController__ToggleGreyScale(controller, param_2 ^ 1, …)`
                //   （⚠️ 任务书写 `:17`，现读反编译：`:17` 是 `BasicCardUI.SetRawCardData`，
                //     灰化那一句在 `:19-20`）⇒ **在池里但加不进的卡 = 显示 + 变灰**，⛔ 不是隐藏。
                //   我们这一侧 2026-10-19（`A2139`）起走**真去饱和**：`CardView.SetGrey(1)` 写原版那个
                //   材质属性 `_GreyScale`（0 = 原色 / 1 = 全灰；判据 = 原版材质 `Sprite Greyscale`
                //   与 shader `Everguild/UI/Greyscale Add`，见 `Resources/Shaders/CardSprite.shader` 头部）。
                //   ⚠️ 原来借 `Unplayable` 的**乘法压暗**（tint 0.55/0.55/0.58）—— 那个口径**已作废**，
                //   但 `State` 仍保留 `Unplayable`（它是「加不进」的状态标记，别的自检也读它）。
                //   🧨 **灭自证 = 同一段 `RefreshPool` 上的两个方向**：
                //     ① 有督军 ⇒ 池里「当前督军」那格 `CanAdd == WarlordAlreadySet` ⇒ **必须灰**
                //        （读的是**渲染器上真正生效的** `_GreyScale` —— `SetGrey` 改成 no-op 必红）；
                //     ② 摘掉督军 ⇒ 池里全是督军且**每一张都加得进** ⇒ **一格都不许灰**。
                //   任何「恒灰」/「恒不灰」的写法必有一条红。
                {
                    string wlG = _rt.State.Deck.WarlordId;
                    CheckTrue(!string.IsNullOrEmpty(wlG), "（前提）此刻卡组有督军（灰卡那一条要有「加不进」的卡）");
                    _rt.RefreshAll();
                    int greySlot = -1; CardDef greyDef = null;
                    for (int i = 0; i < 18; i++)
                    {
                        var d = _rt.UiPoolCardAt(i);
                        if (d == null || _rt.State.CanAdd(d) == DeckError.None) continue;
                        greySlot = i; greyDef = d; break;
                    }
                    CheckTrue(greySlot >= 0, "（前提）有督军时卡池里找得到**加不进**的卡（当前督军那一格就是）");
                    if (greySlot >= 0)
                    {
                        var go = PoolCellGo(greySlot);
                        var cv = go != null ? go.GetComponent<CardView>() : null;
                        CheckTrue(cv != null, $"（前提）第 {greySlot} 格上有 `CardView`");
                        if (cv != null)
                        {
                            Check(cv.State, CardHighlightState.Unplayable,
                                  $"★ 加不进的卡（`{greyDef.Name}` · `{_rt.State.CanAdd(greyDef)}`）在卡池里**显示且灰**"
                                + "（原版 `ToggleGreyScale(param_2 ^ 1)`；🧨 改回 `Normal` ⇒ 本条红）");
                            // ①-a **真去饱和通道**：读两个图层的渲染器（MPB 优先）。
                            //     🧨 灭自证：把 `CardView.SetGrey` 改成 no-op ⇒ MPB 不写 ⇒ 读到材质默认 0 ⇒ 红。
                            float gArt = cv.LayerGrey("art");
                            float gFrame = cv.LayerGrey("frame");
                            CheckTrue(gArt > 0.99f && gFrame > 0.99f,
                                      "★ ……走的是**真去饱和**（`_GreyScale` = 1，不是乘法压暗）："
                                    + $"`art`={gArt:F2} / `frame`={gFrame:F2}"
                                    + "（🧨 `SetGrey` 改 no-op / 只留着色 ⇒ 本条红）");
                            // ①-b **不再是乘法压暗**：状态色必须是白（原版「灰」不靠变暗）。
                            CheckNear(cv.Tint.r, 1f, 0.01f,
                                      "★ ……整卡**不着色**：`Tint.r` = 1（乘法压暗 0.55 那一档已作废；原版是去饱和）");
                            CheckNear(cv.Tint.g, 1f, 0.01f, "★ ……`Tint.g` = 1");
                            CheckNear(cv.Tint.b, 1f, 0.01f, "★ ……`Tint.b` = 1");
                        }
                    }
                    // ② 反向控制：摘掉督军 ⇒ 整池全是督军、每一张都 `CanAdd == None` ⇒ 一格都不许灰
                    _rt.State.ClearWarlord();
                    _rt.RefreshAll();
                    int g2 = 0, seen2 = 0, checked2 = 0;
                    for (int i = 0; i < 18; i++)
                    {
                        var d = _rt.UiPoolCardAt(i);
                        if (d == null) continue;
                        seen2++;
                        if (d.Type != "hero" || _rt.State.CanAdd(d) != DeckError.None) continue;   // 只统计「该不灰」的
                        var cg = PoolCellGo(i);
                        var cv2 = cg != null ? cg.GetComponent<CardView>() : null;
                        if (cv2 == null) continue;
                        checked2++;
                        // 状态和**渲染器上的灰度**两条一起看 —— 只断状态的话「状态对、灰度没归零」会漏过去
                        if (cv2.State != CardHighlightState.Playable
                            || cv2.Grey > 0.01f
                            || cv2.LayerGrey("art") > 0.01f
                            || cv2.LayerGrey("frame") > 0.01f) g2++;
                    }
                    CheckTrue(seen2 > 0, $"（前提）没督军时卡池里有 {seen2} 格");
                    // 🧨 挡「探针没读到任何一格 ⇒ `g2` 恒 0」那种假绿
                    CheckTrue(checked2 > 0, $"（前提）反向控制真的读到 {checked2} 格 `CardView`（读不到 = 本条无意义）");
                    Check(g2, 0, "★ 反向控制：没督军 ⇒ 池里每一张都加得进 ⇒ **一格都不灰**"
                              + "（🧨 恒灰的写法 —— 比如把 `Playable` 那一支也改成 `Unplayable` / 灰度不复位 —— 本条红）");
                    _rt.State.SetWarlord(wlG);
                    _rt.RefreshAll();
                    Check(_rt.State.Deck.WarlordId, wlG, "（把督军放回去 —— 后面的断言仍按原状态跑）");
                }
                // 🧨 **本节的判别式（每格各挡一种改坏法，⛔ 只断「现在对」= 自证）**：
                //    ① 有督军 ⇒ 显示            —— 挡「探针落在带口外」/「这一格根本没建」（`null` 也算红）
                //    ② 没督军 ⇒ 不显示 + 点不到 —— 挡「改前那种恒显示」（本条的靶心）
                //    ② 反向控制 Army 族照旧活  —— 挡「整栏一起 SetActive(false)」那种改法
                //    ③ 督军放回 ⇒ 显示回来      —— 挡「关掉就再也点亮不回来」那种改法
                // 🔴 **2026-10-14（账 `N14`）：卡组没有督军 ⇒ 费用那一行（`Energy Cost` 小标题 + 8 档）整个不显示。**
                //    判据 = 原版 `CardCostFilter__SetFilterToDeck.c:13-23` 那句
                //    `SetActive(self.gameObject, deck.deckHero != null)`（整节点一关 ⇒ 其下
                //    `Title` 与 `Content` 一起不画）；收藏窗那两份恒 active（全库只有
                //    `DeckEditingWindow.FilterToDeck` 调 `SetFiltersToDeck`）⇒ 本条**只**该在卡组编辑这里成立。
                //    ⚠️ 断 **`activeSelf`**（我们自己 SetActive 的那一层）：抽屉是当场 `UiToggleFilters()`
                //       打开的（前提那条钉着），而 `ApplyDrawerSlide` 只开关**容器**、不碰格子树
                //       ⇒ 父链必定活着，不需要退到 `activeInHierarchy`。
                //        （本文件 `FilterTitleLabel` 走的是自己的 `FindDeep` = `GetComponentsInChildren(true)`
                //          ⇒ 关掉的 `flt_title_*` 仍取得到，`tOff != null` 这半个条件是成立的。）
                //    ⚠️ 探针**只能用 `CostBuckets[0]`（`$cost:1`）**，而且要**先把它滚进带口**
                //       （见下面 `UiScrollFilters(-1e6f)` + `UiScrollFilters(220f)` 那道算式）：
                //       滚动位 0 时 **Cost 整行**（两排 8 档）都在带口 `[156, 1080.1]` **之下**
                //       （`$cost:1` 的绝对 y = `FltY 156 + CostTop 1009.02 + CostContentTop 65` = **1230.02**）
                //       ⇒ 那些格子本来就 `SetActive(false)`（`ClipCellToBand`），拿它们断这条是**假红**。
                //       🔴 **2026-10-15 就地订正（铁律 5）**：原来写「滚动位 0 时第 2 排（档 5~8）整块在带口外」
                //          —— 实测是**整行**都在带口外（同文件 `本文件` 那条早就这么写了，两处曾互相打架）。
                {
                    string wlK = _rt.State.Deck.WarlordId;
                    bool wasOpen = _rt.FiltersOpen;
                    CheckTrue(!string.IsNullOrEmpty(wlK),
                              "（前提）此刻卡组**有督军**（下面两态都建立在「有督军」这个基线上）");
                    if (!wasOpen) _rt.UiToggleFilters();
                    // 🔴 **探针的滚动位（2026-10-15 · 诊断 `DK-1`/`DK-2`；`-1e6` 那句原来直接停在 0）**：
                    //    `UiScrollFilters(dy)` 是**增量**，所以先 `-1e6f` 归 0、再 `+220f` 到 220。
                    //    这个 220 是**算出来的**（期望值全是原版字面量，⛔ 不是试出来的）：
                    //      · `$cost:1` 滚到 0 时绝对 y = `FltY 156 + CostTop 1009.02 + CostContentTop 65`
                    //        = **1230.02**，格 65 高 ⇒ `[1230.02, 1295.02]`；减 220 ⇒ `[1010.02, 1075.02]`
                    //        —— **整格落进带口 [156, 1080.1]**（下沿还剩 5.08px）；
                    //      · `$fac:<第一阵营>`（Army 首格）滚到 0 时绝对 y =
                    //        `156 + RowArmy 179.02 + ArmyContentTop 50` = **385.02** ⇒ `[385.02, 485.02]`；
                    //        减 220 ⇒ `[165.02, 265.02]`，**也在带口内**（② 那条反向控制要拿它当对照）。
                    //    ⛔ **别改成「滚到底」（`1e6f` ⇒ `464.92`）**：那样 Army 首格变成 `[-79.9, 20.1]`
                    //        = 整块被推出带口 ⇒ `SetActive(false)` ⇒ ② 那条「Army 那一族照旧活着」**假红**。
                    //    ⚠️ 这两个数都依赖 `Factions()` = **13**（Army 行 550 高；见 `ContentHFor` 那段）；
                    //       阵营数一变，这儿要跟着重算。
                    _rt.UiScrollFilters(-1e6f);
                    _rt.UiScrollFilters(220f);
                    CheckTrue(_rt.FiltersOpen, "（前提）筛选栏打开（关着时 `_fltCells` 是空的，读不到格子）");
                    string ck = "$cost:" + FilterPanelModel.CostBuckets[0].Lo;      // `$cost:1` —— 由上面那一步推进带口
                    // ---- ① 有督军 ⇒ 显示（基线与「探针确实在带口内」一并钉住）----
                    var qOn = _rt.UiFilterCellQuad(ck);
                    var tOn = FilterTitleLabel("Energy Cost");
                    CheckTrue(qOn != null && qOn.gameObject.activeSelf,
                              "★ N14 ①：**有督军**时费用那一格**显示**（顺带证明探针不在带口外）");
                    CheckTrue(tOn != null && tOn.gameObject.activeSelf,
                              "★ N14 ① ……`Energy Cost` 小标题也显示");
                    // ---- ② 摘掉督军 ⇒ 整行不显示 ----
                    _rt.State.ClearWarlord();
                    _rt.RefreshAll();
                    var qOff = _rt.UiFilterCellQuad(ck);
                    var tOff = FilterTitleLabel("Energy Cost");
                    CheckTrue(qOff != null && !qOff.gameObject.activeSelf,
                              "★★ N14 ②：**没有督军** ⇒ 费用那一格**不显示**"
                            + "（改前恒显示 ⇒ 这条红；判据 `SetActive(deckHero != null)`）");
                    CheckTrue(!_rt.UiDrawerHitRect(ck, out _, out _, out _, out _),
                              "★★ N14 ② ……而且**点不到**（原版整节点一关 ⇒ 子树连射线一起没；"
                            + "⛔ 不许留「看不见却点得动」的空格）");
                    CheckTrue(tOff != null && !tOff.gameObject.activeSelf,
                              "★★ N14 ② ……`Energy Cost` 小标题也不显示（原版 `Cost Filter` 那两个子件一起不画）");
                    var qArmy = _rt.UiFilterCellQuad("$fac:" + _rt.State.Factions()[0]);
                    CheckTrue(qArmy != null && qArmy.gameObject.activeSelf,
                              "★ N14 ② 反向控制：**同一拍**里 `Army` 那一族照旧活着"
                            + "（「整栏一起关掉」那种改法会被这条抓住）");
                    // ---- ③ 放回督军 ⇒ 显示回来（免得「关掉就再也回不来」也绿）----
                    _rt.State.SetWarlord(wlK);
                    _rt.RefreshAll();
                    var qBack = _rt.UiFilterCellQuad(ck);
                    CheckTrue(qBack != null && qBack.gameObject.activeSelf,
                              "★ N14 ③：督军放回去 ⇒ 费用那一行**显示回来**");
                    Check(_rt.State.Deck.WarlordId, wlK, "（把督军原样放回 —— 后面的断言仍按原状态跑）");
                    // 🔴 **滚动位一并还原**（2026-10-15 · 诊断 `DK-1`/`DK-2`）：本段为了把 `$cost:1`
                    //    推进带口滚到了 220，而**下面那些节都在滚动位 0 上读抽屉**（`本文件` 那条更直接
                    //    写着「滚到顶时」）⇒ 不还回去就会把红挪到别人身上（本节的收尾纪律同上一条）。
                    _rt.UiScrollFilters(-1e6f);
                    if (!wasOpen) _rt.UiToggleFilters();     // 收回去：本节不许改后面用例的初态
                }
            }

            // 筛选栏：默认关着；打开后盖住侧栏（队列更大 = 更后画）
            Check(_rt.FiltersOpen, false, "刚建好时筛选栏是关着的");
            // 🔴 这条是踩出来的：底板建了却**没跟着开关隐藏** ⇒ 它（队列 3020，比侧栏大）
            //    会一直盖住整个侧栏 —— 画面上只剩一块底板色，卡组行/页签/Done 全看不见。
            CheckTrue(_rt.UiHasQuad("flt_bg"),
                      "（前提）筛选栏底板 `flt_bg` **建出来了** —— 不然下面那条「不显示」是拿「件不存在」蒙的"
                    + "（`UiQuadActive` 对不存在的 key 恒答 false，账 `A1532`）");
            CheckTrue(!_rt.UiQuadActive("flt_bg"), "关着的时候筛选栏底板**不显示**（不然会盖住整个侧栏）");
            // 🔴 **2026-10-05（A57 ①）：上面那条 `flt_bg` 与下面 `side_bg` 都是 `Img()` 建的【单块】**
            //    —— `UiQuadActive` 以前**只查 `_named`**，对**九宫格 / 子树**件**恒答 false**（静默）。
            //    这两条拿抽屉里的**搜索框底 `flt_input`** 做**两态对照**：它是
            //    `ImageQuad.CreateNineSlice` 建的九宫格、**不进 `_named`**，而且挂在容器 `flt_drawer` 底下。
            //    · 关着 ⇒ **不露** —— 这一条同时钉住「这个读数**不是恒 true**」；
            //    · 打开 ⇒ **露** —— 这一条**去掉兜底必红**（旧写法两种状态都答 false）。
            //    ⚠️ 「打开」那条**不能**拿常显件（如 `hdr_sep`）顶替两态：常显件在两种状态下同值，
            //    它只能验「不是恒 false」、验不出「不是恒 true」（同段 `hdr_sep` 那条就是只验前一半）。
            CheckTrue(_rt.UiHasQuad("flt_input"),
                      "（前提）九宫格件 `flt_input` **建出来了** —— 不然下面那条「不露」是拿「件不存在」蒙的"
                    + "（账 `A1532`）");
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

            // ============================================================ 🆕 2026-10-16（WB）
            // **卡组编辑与收藏窗的「列距 / 左内缩」必须【各是各的】**（铁律 5·c）—— 模型只有一份、被两扇窗共用，
            //   而原版那两棵树里 `Rarity/Cost/Type` 的 `Content` 是**两份不同的 prefab 实例、序列化值不同**：
            //     Rarity 列距  卡组编辑 `m_Spacing.x = 5`（步进 **105**）· 收藏窗 `= 7`（步进 107）
            //     Cost   列距  卡组编辑 `= 7`（步进 **72**）        · 收藏窗 `= 15`（步进 80）
            //     Type  左内缩 卡组编辑 `m_Padding.Left = 40`       · 收藏窗 `= 15`
            //   判据三条（`menu_dump.py` 打两棵树 · 全包逐实例枚举 · `普查产出_0923/A3_Cards页.md:118-119`）
            //   → `Core/FilterPanelModel.cs` ⑤ 那段；差异账 = `普查产出_1017/对账_卡组编辑部分_差异与待办.md` D36–D38。
            // ⛔ 期望值一律是**原版字面量**（105 / 72 / 40 / pad L14·L15），**不从 `FilterPanelModel.RaritySpXDeckEdit` 读**。
            // 🔴 改坏法：① 把 `Build` 的 `raritySpX/costSpX/typePadL` 三个形参删掉（退回只用共用常量）⇒ 第二组红；
            //            ② 把缺省 `ArmySpX` 改成 5 / `CostSpX` 改成 7 / `TypePadL` 改成 40 ⇒ 第一组红。
            {
                var probeA = new List<FilterPanelModel.Cell>();
                FilterPanelModel.Build(state, 331.7f, probeA);            // 缺省那一档 = **收藏窗**
                var probeB = new List<FilterPanelModel.Cell>();
                FilterPanelModel.Build(state, 331.7f, probeB,             // **卡组编辑**那一档（三个真值显式传）
                                       raritySpX: 5f, costSpX: 7f, typePadL: 40f);
                var rarA = probeA.Find(c => c.Key == "$rar:epic");        // Rarity 第 3 列（i=2）
                var rarB = probeB.Find(c => c.Key == "$rar:epic");
                var costA = probeA.Find(c => c.Key == "$cost:3");         // Cost 第 3 列（i=2）
                var costB = probeB.Find(c => c.Key == "$cost:3");
                var typA = probeA.Find(c => c.Key == "$type:unit");       // Type 第 2 格（i=1）
                var typB = probeB.Find(c => c.Key == "$type:unit");
                CheckTrue(rarA.Key != null && rarB.Key != null && costA.Key != null
                          && costB.Key != null && typA.Key != null && typB.Key != null,
                          "（前提）两组探针里 `$rar:epic` / `$cost:3` / `$type:unit` 都出得来");
                // 收藏窗：Rarity `pad L14 + 2×(100+7)` = **228** · Cost `pad L15 + 2×(65+15)` = **175** · Type `pad L15 + 80` = **95**
                CheckNear(rarA.R.x1, 228f, 0.6f,
                          "收藏窗 Rarity 第 3 列 x1 = **228**（`pad L14` + 2×步进 **107**，原版 `m_Spacing.x = 7`）");
                CheckNear(costA.R.x1, 175f, 0.6f,
                          "收藏窗 Cost 第 3 列 x1 = **175**（`pad L15` + 2×步进 **80**，原版 `m_Spacing.x = 15`）");
                CheckNear(typA.R.x1, 95f, 0.6f,
                          "收藏窗 Type 第 2 格 x1 = **95**（`pad L15` + 80，原版 HLG `m_Padding.Left = 15`）");
                // 卡组编辑：Rarity `14 + 2×(100+5)` = **224** · Cost `15 + 2×(65+7)` = **159** · Type `40 + 80` = **120**
                CheckNear(rarB.R.x1, 224f, 0.6f,
                          "卡组编辑 Rarity 第 3 列 x1 = **224**（`pad L14` + 2×步进 **105**，原版 `m_Spacing.x = 5`）");
                CheckNear(costB.R.x1, 159f, 0.6f,
                          "卡组编辑 Cost 第 3 列 x1 = **159**（`pad L15` + 2×步进 **72**，原版 `m_Spacing.x = 7`）");
                CheckNear(typB.R.x1, 120f, 0.6f,
                          "卡组编辑 Type 第 2 格 x1 = **120**（原版 HLG `m_Padding.Left = **40**` + 80）");
                // ★ **灭自证**（§三：防「两边一起改回去」）：两窗这三处**必须差出原版那个差值** ——
                //   若把缺省与实参改成同一套（或删掉形参），下面三条差都变 0 ⇒ 红；而上面那些绝对值也可能照样绿。
                CheckNear(rarA.R.x1 - rarB.R.x1, 4f, 0.6f,
                          "★ 两窗 Rarity 列距差 = 2×(7−5) = **4**（同一套常量时这个差是 0）");
                CheckNear(costA.R.x1 - costB.R.x1, 16f, 0.6f,
                          "★ 两窗 Cost 列距差 = 2×(15−7) = **16**（同一套常量时是 0）");
                CheckNear(typB.R.x1 - typA.R.x1, 25f, 0.6f,
                          "★ 两窗 Type 左内缩差 = 40−15 = **25**（同一套常量时是 0）");
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
            //   （`Shell/MenuDraw.cs`；A294 / A297 / A298 三轮把它收口到这一处）；
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
            // 🔴 **2026-10-18（A891 的续）改「随语档」**：占位符从本批起**走词条**
            //   （`Core/FilterPanelModel.InputPlaceholder` → `Loc.T("MenuDeck/HUD/SearchFilter")`），
            //   而本宿主跑在**出厂语言 = 中文**（`Core/Loc.cs` 的 `Default`）⇒ 原来写死的 `"Search"` 必红。
            //   期望值 = **同一个取词口**（`Loc.T`），但**不是**同义反复 —— 下面那条 `HasEntry` 钉住
            //   「键在表里」：键不在 ⇒ `Loc.T` 返回**键名本身**、渲染那一侧也返回键名 ⇒ 两边一起退化，
            //   只有靠 `HasEntry` 那条才红得出来（形状同 `Editor/CollectionScene.cs` ⑨ 那一节）。
            CheckTrue(Loc.HasEntry("MenuDeck/HUD/SearchFilter"),
                      "（前提）词条 `MenuDeck/HUD/SearchFilter` 在 `Loc` 表里 —— ⛔ 不在的话下面那条会两边同时退化成键名（假绿）");
            Check(_rt.UiFilterInputText, Loc.T("MenuDeck/HUD/SearchFilter"),
                  "空的时候搜索框画的是**占位符**（原版 `Placeholder` 挂 `Localize.mTerm = MenuDeck/HUD/SearchFilter` ⇒ 文案随语档）");

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
            //   · `Army` 那是**四行小标题**那一族（`FilterPanelModel.BuildTitles` 的 `Title.Text`）——
            //     🔴 **2026-10-18（A891 的续 · 续做 A）起它也走词条了**（`MenuDeck/Filters/Army`），
            //     期望值见下面那条（`Loc.T(键)`），⛔ 别写成中文或英文的字面量；
            //   · 搜索框那颗（`flt_input_t`）**本批起走词条**（`FilterPanelModel.InputPlaceholder` →
            //     `Loc.T("MenuDeck/HUD/SearchFilter")`）⇒ 期望值也改成那个取词口，⛔ 不再写死 `'Search'`
            //     （上一行 `UiFilterInputText` 刚量的是**同一颗**节点）。
            // 🔴 改坏法：把 `DeckRuntime.UiLabelText` 里 `FindDeep` 那一步删掉（退回只走 `Root.Find`）
            //   ⇒ 下面**两条字串断言都读成 `null`** ⇒ 两条红（`poolcnt_0` 那种直接子件不受影响，
            //   见本文件既有那两条 —— 本节顺带就是「直接子件那一路没被挤掉」的对照）。
            {
                var deepTitle = FindDeep(_root, "flt_title_Army");
                CheckTrue(deepTitle != null && _root.Find("flt_title_Army") == null,
                          "结构前提：`flt_title_Army`（`Army` 小标题那颗 `Label`）**在 Root 那棵树里、"
                          + "但不是直接子件**（父级 = 抽屉容器）—— 这正是缺 `FindDeep` 时看不见它的原因"
                          + "（这条若红：下面两条失去判别力，先修这里）");
                CheckTrue(Loc.HasEntry("MenuDeck/Filters/Army"),
                          "（前提）词条 `MenuDeck/Filters/Army` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名 = 假绿）");
                Check(_rt.UiLabelText("flt_title_Army"), Loc.T("MenuDeck/Filters/Army"),
                      "`UiLabelText(flt_title_Army)` 读得到**容器下**的 `Label`"
                      + "（A190：只走 `Root.Find` 的旧写法在这里**静默答 `null`**；"
                      + "**2026-10-18（A891 的续 · 续做 A）起期望值随语档** —— 原来是字面量 `Army`）");
                CheckTrue(Loc.HasEntry("MenuDeck/HUD/SearchFilter"),
                          "（前提）词条 `MenuDeck/HUD/SearchFilter` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名 = 假绿）");
                Check(_rt.UiLabelText("flt_input_t"), Loc.T("MenuDeck/HUD/SearchFilter"),
                      "……另一族容器下的 `Label`：搜索框那颗（与上一行同**一颗**节点，这条按**名字**走；"
                      + "旧写法同样答 `null`。**2026-10-18（A891 的续）起期望值随语档**）");
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
                CheckTrue(_rt.UiHasQuad("flt_bg"),
                          "（前提）`flt_bg` **建出来了** —— 不然下面那条「不露」是拿「件不存在」蒙的（账 `A1532`）");
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
                CheckTrue(_rt.UiHasQuad("flt_bg"),
                          "（前提）`flt_bg` **建出来了** —— 不然下面那条「滑到底关掉」是拿「件不存在」蒙的（账 `A1532`）");
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
            // 🔴 **2026-10-18（A891 的续 · 续做 A）**：本节的四条都过 `TitleLeftPx` → `ExpectedTitleText`，
            //   而四行小标题在**中文档**下印的都是中文 ⇒ 这里先钉住「键在表里」：
            //   键不在 ⇒ `Loc.T` 返回**键名本身**、渲染那一侧同样返回键名 ⇒ 比字两边一起退化 = **假绿**。
            // 🔴 **第三轮整改（审查 P1）**：`Energy Cost` 也在这一组里了（它原来被误判成「原版无词条」；
            //   复跑全部 80 个 bundle ⇒ 它挂 `Battle/Tips/EnergyCost`）。
            //   ⚠️ 这条**真的抓到过一次**：本批第一版 `TitleTerm` 给 `Rarity`/`Type` 返回了两个
            //   `Loc` 表里**当时还不存在**的键 ⇒ 界面上会印键名本身，就是靠这一条变红的。
            CheckTrue(Loc.HasEntry("MenuDeck/Filters/Army") && Loc.HasEntry("MenuDeck/HUD/Rarity")
                      && Loc.HasEntry("MenuDeck/Filters/Type") && Loc.HasEntry("Battle/Tips/EnergyCost"),
                      "（前提）四行小标题的词条都在 `Loc` 表里（⛔ 有一条不在 ⇒ 本节四条会两边一起退化成键名）");
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
            // 🔴 **2026-10-17（D35 删件）**：这一句原来是 `CheckTrue(_rt.UiInfoActionsVisible, …)`
            //   （「分享 / 导入」两颗钮显示出来）。那两颗钮**原版这扇窗里没有**、已删 ⇒ 换成**反向断言**：
            //   它们**一个都不许再被建出来**。判据 → `对账_卡组编辑部分_差异与待办.md` D35。
            //   ⛔ 「不在了」这半边必须**在 Deck info 页**上断一次（Cards 页断的话，一个「只在别的页建」
            //     的实现照样绿）—— 本行正处在 `_rt.UiSetTab(1)` 之后。
            foreach (var k in new[] { "info_share", "info_share_bg", "info_share_t",
                                      "info_import", "info_import_bg", "info_import_t" })
                CheckTrue(FindDeep(_rt.Root, k) == null, "★ D35：`" + k + "` **必须已经不存在**（原版这扇窗里没有分享/导入两颗圆钮）");
            CheckTrue(!_rt.UiBtnRegistered("info_share") && !_rt.UiBtnRegistered("info_import"),
                      "★ D35：那两个点击矩形**也不许留在 `_btns` 里**（否则又会出现「看不见却能点」）");
            CheckTrue(!_rt.UiCosmeticsVisible, "Cosmetics 那组东西在别的页签下是关的");
            CheckTrue(_rt.UiDeckRowAt(0) == null, "Deck info 页签下卡组行不显示");

            // ============================================================ 🆕 2026-10-17（WC）
            // **费用曲线抽屉：整体 `localScale = 1.48` 的验**（差异账 D30–D32；判据 = 原版预制体 JSON）。
            // 期望值**逐个来自原版字段 × 1.48**（`bundle_menus_assets_all`），⛔ 不是从 `DeckRuntime.CurveBar*`
            // 那族常量读回来的；量的是**真渲出来的矩形**（`UiQuadRect` 与 `DeckRuntime.PxOfWorld`）。
            //   · 抽屉 RT `7431712229497630500`：`m_LocalScale **1.48**` · pivot (0.5,**1**) ·
            //     `m_SizeDelta (158.15, 199.06)` ⇒ 缩放后 **x 50.8753..284.9373 × y 410.9700..705.5788**。
            //   · `Content` 的 VLG `m_Spacing **3.43**` + 行高 `18.91` ⇒ 行距 **22.34** ⇒ 缩放后步进 **33.0632**。
            //   · 行内三列（`Card Cost` / `Slider` / `Cards in deck`）的 `m_AnchoredPosition.x`（−95.64 / ≈0 / +95.23）。
            //   · 画出来的槽是 `Slider/Background`（151.34 × **16.57**，顶距行顶 **2.1349** 是缩放后的值）。
            // 🔴 **改坏法**：① 把 `CurveScale` 改回 **1.0**（= 本轮之前那版）⇒ 下面整块全红，
            //   其中「末行那根槽的底」会从 **702.134** 掉到约 **608.395**（= 410.97 + 8×22.34 + 2.1349 + 16.57）；
            //   ② 把两列的 x 改回旧写法（`行左 − 14` / `槽右 + 6`）⇒ 两条中心各红（+28.8 / −42.4）。
            {
                CheckTrue(_rt.UiQuadActive("curve_bg"),
                          "费用曲线抽屉的**底板** `curve_bg` 建出来了、且在本页签显示（D31：原来整件没画）");
                if (_rt.UiQuadRect("curve_bg", out float gx, out float gy, out float gw, out float gh))
                {
                    CheckNear(gx, 167.906f, 0.5f, "底板中心 x = (50.8753+284.9373)/2");
                    CheckNear(gy, 558.274f, 0.5f, "底板中心 y = (410.9700+705.5788)/2");
                    CheckNear(gw, 234.062f, 0.5f, "底板宽 = 158.15 × 1.48（⛔ 照 chain_rect 表抄会画成 158.15）");
                    CheckNear(gh, 294.609f, 0.5f, "底板高 = 199.06 × 1.48");
                }
                else CheckTrue(false, "量不到 `curve_bg` 的矩形（底板没建 / 没进 `_named`）");

                if (_rt.UiQuadRect("curve_b0", out float b0x, out float b0y, out float b0w, out float b0h)
                    && _rt.UiQuadRect("curve_b8", out float b8x, out float b8y, out float b8w, out float b8h))
                {
                    // 行距那一跳 = 8 × (18.91 + 3.43) × 1.48 —— ⛔ 别写成 8 × 18.91 × 1.48（漏了 VLG 的 spacing）
                    CheckNear(b8y - b0y, 264.5056f, 0.6f,
                              "费用 0→8 两根槽的中心相差 **264.5056** = 8 × (18.91+3.43) × 1.48（旧写法是 178.72）");
                    CheckNear(b0y, 425.367f, 0.5f, "首行（费用 0）槽中心 y = 410.970016 + 2.1349 + 24.5236/2");
                    CheckNear(b8y + b8h * 0.5f, 702.134f, 0.5f,
                              "**末行（费用 8）那根槽的底 = 702.134** —— 它落在那个人高 18.91×1.48 = **27.9868** 的行盒里，"
                              + "行盒的底 = 675.4756 + 27.9868 = **703.462**（槽底比盒底高 1.328；旧写法整根槽落到 608.4）");
                    CheckNear(b0x, 167.906f, 0.5f, "槽中心 x（原版 `Slider` 居中于抽屉 pivot 167.906）");
                    CheckNear(b0w, 223.983f, 0.5f, "槽宽 = 151.34 × 1.48（旧写法 151.3 ⇒ 少 72.7px）");
                }
                else CheckTrue(false, "量不到 `curve_b0` / `curve_b8` 的矩形");

                // 两列文字：量的是标签**渲染网格**的中心（`Label` 以 anchor 定位、居中画）——
                // 期望值 = 原版那两颗 TMP 的盒中心（`⛔ 不是我们传进去的 CurveLbX/CurveNumX 自己读回来`）。
                var lbT = FindDeep(_rt.Root, "curve_l0");
                var lbC = lbT != null ? lbT.GetComponent<Label>() : null;
                var nuT = FindDeep(_rt.Root, "curve_n0");
                var nuC = nuT != null ? nuT.GetComponent<Label>() : null;
                CheckTrue(lbC != null && nuC != null,
                          "两列数字的标签都找得到（`curve_l0` / `curve_n0`）");
                if (lbC != null)
                    CheckNear(DeckRuntime.PxOfWorld(lbC.transform.position).x, 26.359f, 0.5f,
                              "左列 `Card Cost` 中心 x = **26.359**（原版 `m_AnchoredPosition.x = −95.64`）；旧写法 55.1 偏右 28.8");
                if (nuC != null)
                    CheckNear(DeckRuntime.PxOfWorld(nuC.transform.position).x, 308.847f, 0.5f,
                              "右列 `Cards in deck` 中心 x = **308.847**（原版 `+95.23`）；旧写法 266.5 偏左 42.4");
            }
            // ============================================================ 🆕 2026-10-19（U4 · 界面线批次 6）
            // **原版那 9 颗 `Slider` 是【只画不点】的** —— 逐颗实读（`bundle_menus_assets_all` 的
            //   `Deck Editing Menu/.../Deck Information cost drawer/Content/Deck CostQuanityt Row Drawer (±i)/Slider`）：
            //   **9 颗值完全一致**：`m_Interactable = 0` · `m_Transition = 0 (None)` ·
            //   `m_TargetGraphic = null` · `m_HandleRect = null`（没有手柄）。
            //   ⇒ `m_Colors` 那四格（白 / 0.9608 / 0.7843 / α0.502）**一格都用不上**。
            // 🔴 **⛔ 别给这 9 颗接 `WfSlider` 反馈**：UGUI `Slider` 全族默认 `t=1 ColorTint`，
            //   **这一族是显式 `t=0` + 不可交互** —— 按全族默认值一刀切会与原版相反。
            // 我们的等价物 = 9 根**静态 `ImageQuad`**（`curve_b*` = 原版 `Slider/Background` ·
            //   `curve_f*` = `Slider/Fill`）；一根都不许挂 `Selectable` / `WindowButton`。
            // 🔴 **改坏法**：① 给任一根接 `WindowButton`（= 接反馈）⇒ 第二条红；
            //   ② 塞一颗真 `UnityEngine.UI.Slider` ⇒ 第三条红。
            {
                int bars = 0, wbs = 0, sels = 0;
                for (int c = 0; c <= 8; c++)
                {
                    var bt = FindDeep(_rt.Root, "curve_b" + c);
                    var ft = FindDeep(_rt.Root, "curve_f" + c);
                    if (bt != null && bt.GetComponent<ImageQuad>() != null
                        && ft != null && ft.GetComponent<ImageQuad>() != null) bars++;
                    if (bt != null)
                    {
                        wbs += bt.GetComponentsInChildren<WindowButton>(true).Length;
                        sels += bt.GetComponentsInChildren<UnityEngine.UI.Selectable>(true).Length;
                    }
                    if (ft != null)
                    {
                        wbs += ft.GetComponentsInChildren<WindowButton>(true).Length;
                        sels += ft.GetComponentsInChildren<UnityEngine.UI.Selectable>(true).Length;
                    }
                }
                Check(bars, 9,
                      "★ 费用曲线 9 根槽（`curve_b0..8` + `curve_f0..8`）**都画成了 `ImageQuad`**"
                      + "（= 原版 `Slider/Background`（`40k_CardAmount_bar_bg`）+ `Slider/Fill`（`40k_CardAmount_bar_fill`）的等价物）");
                CheckTrue(wbs == 0,
                          "★ 这 9 根槽**一个 `WindowButton` 都没有** —— 原版 `m_Interactable = 0` / `m_Transition = None`"
                          + " ⇒ 悬停/按下**什么都不发生**（⛔ 别接 `WfSlider` 反馈）"
                          + (wbs != 0 ? $"（现读 {wbs} 颗）" : ""));
                CheckTrue(sels == 0,
                          "★ 这 9 根槽子树里**没有任何 `UnityEngine.UI.Selectable`**"
                          + "（原版那 9 颗在 prefab 里就是 `m_Interactable = 0` 的 `Slider`；我们是纯绘制层）"
                          + (sels != 0 ? $"（现读 {sels} 颗）" : ""));
            }
            // 🔴 **2026-10-05（A57 ①）：同一条「行藏没藏」再过一遍【图】那一半** —— `UiDeckRowAt` 读的是
            //    状态（`_tab` + `shown`），答不出「那棵九宫格**树**真的关掉了吗」（`SetOn(_deckRowBg[i], …)`，
            //    `DeckRuntime.cs` 的 `RefreshDeckList`）。`row_0` 正是 A57 ① 点名的受影响件之一
            //    （九宫格、**不进 `_named`**）⇒ 下面这一对同时钉住「关得住」与「读得出关」。
            //    期望值出处：行属 Cards 页（原版 `Content Area/Sidebar` 的卡组列表只在 Cards 页签下），
            //    另由紧邻的 `UiDeckRowAt` 那两条独立佐证 —— ⛔ 不是从 `UiQuadActive` 自己读回来的。
            CheckTrue(_rt.UiHasQuad("row_0"),
                      "（前提）`row_0` 那棵九宫格行树**建出来了** —— 不然下面那条「真关了」是拿「件不存在」蒙的"
                    + "（账 `A1532`）");
            CheckTrue(!_rt.UiQuadActive("row_0"),
                      "Deck info 页签下**那棵九宫格行树真的关了**（`row_0` 不在 `_named` 里 ⇒ "
                      + "少了兜底这条会**碰巧**答 false，所以必须与下面那条成对看）");
            _rt.UiSetTab(2);
            CheckTrue(_rt.UiCosmeticsVisible, "Cosmetics 页签：**卡背那一页显示出来**（2026-09-24 起是真页面，不再是一句空态）");
            CheckTrue(!_rt.UiCurveVisible, "Cosmetics 页签下费用曲线关掉");
            // 🔴 **2026-10-17（D35 删件）**：这一句原来读 `_rt.UiInfoActionsVisible`（那颗 `info_import`）
            //   —— 钮已删、读数一并没了 ⇒ 换成**反向断言**（同 `Deck info 页` 那一条，两页各断一次）。
            CheckTrue(FindDeep(_rt.Root, "info_import") == null && !_rt.UiBtnRegistered("info_import"),
                      "★ D35：Cosmetics 页签上「导入」那颗钮也不存在（节点 + 点击矩形两样都没有）");
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
            // ⚠️ **`A1518③`（2026-10-11）**：这两条**只覆盖 `_infoOnly` / `_cosmOnly` 两张表** ——
            //   2026-10-11 新加的第三张表 `_cardsInfoOnly`（通配符计数条整条）**不在任一张里**
            //   ⇒ 它的「Cards / Cosmetics 页零泄露」那两格**各自另有断言**（下面 A1497④ 那一节里的
            //   `UiCardsInfoOnlyActive` 两条）。⛔ 别把这两条当成「整个窗口的泄露守卫」。
            Check(_rt.UiInfoOnlyActive, 0, "Cards 页签上**一件 Deck info 的东西都不许露**（图 + 文字都算）");
            Check(_rt.UiCosmOnlyActive, 0, "Cards 页签上不许露 Cosmetics 的东西");

            // ============================================================ 🆕 2026-10-11（A1497④）
            // **卡池 + 通配符计数条：Cards / Deck info 两页**都显示**，只有 Cosmetics 页关。**
            // 判据（第一权威 = 反编译方法体，逐行现核）= `d:/2/tools/decomp_full/DeckEditingWindow__ToggleDisplay.c`：
            //   一个入参管三件 —— `cardDisplay.SetActive(param)` · `cosmeticDisplay.SetActive(param ^ 1)` ·
            //   `wildcards.SetActive(param)`；而 `DeckEditingStateController__ToggleDeckInfo.c` /
            //   `__ToggleDeckList.c` 传 **1**、`__ToggleDeckCosmetic.c` 传 **0**
            //   ⇒ **卡池与整条通配符计数条是同相的两件**（Cards / Info 都开、Cosmetics 关）。
            // 🔴 **三页都得断**：只断「Cosmetics 页关着」的话，**旧的 `_tab == 0` 写法照样全绿** —— 那是**假绿**，
            //   本件要抓的正是「Deck info 页被误关」那一半（改之前那一页右侧整块是空的）。
            // 🧨 **每格摆回基线**：每一小格开头都 `UiSetTab(0)`（上一格切到 Cosmetics 的残留会漏到下一格）。
            // ⚠️ 「露着没有」一律读 **`activeInHierarchy`**（含父链）；⛔ **不许拿 `UiQuadActive("pool_" + i)` 量卡池格**
            //   —— 卡面层是 `MeshRenderer` 不是 `ImageQuad` ⇒ 那口对卡池格**恒答 false**（理由见 `PoolCellGo` 的 doc）。
            {
                Section("★ A1497④：卡池 + 通配符计数条 —— Cards / Info 两页都显示、只有 Cosmetics 页关");
                int nPool = 0;
                float bx = 0f, by = 0f, bw = 0f, bh = 0f;

                // ---- ① Cards 页（基线）----
                _rt.UiSetTab(0);
                {
                    var c0 = PoolCellGo(0);
                    CheckTrue(c0 != null, "（前提）卡池第 0 格的视图根 `pool_0` 在（`CardView.Create(Root, …)` 建的）");
                    CheckTrue(c0 != null && c0.gameObject.activeInHierarchy,
                              "★ Cards 页：`pool_0` **真画着**（视图根 `activeInHierarchy`；"
                            + "⛔ 不是 `UiQuadActive` —— 那一口对卡池格恒答 false）");
                    CheckTrue(_rt.UiPoolCardAt(0) != null, "★ ……状态里那一格也指着真卡（`_poolIndex[0] >= 0`）");
                    nPool = PoolCellsShown();
                    CheckTrue(nPool > 0, $"★ Cards 页卡池画出来 **{nPool}** 格（0 格 ⇒ 下面那几条全成空谈）");
                    CheckTrue(_rt.UiPoolCellRect(0, out bx, out by, out bw, out bh),
                              "★ ……以及那一格的矩形（② 要拿它比「Info 页摆的是不是同一处」）");
                }

                // ---- ② Deck info 页：**本次改动落在的这一格** ----
                //  改之前 `RefreshPool` 的判据是 `_tab == 0` ⇒ 这一页右侧整块是空的（`_poolIndex` 全 −1、
                //  视图全 `SetActive(false)`）；原版这一页**照样是卡池**（`Card Display` 那一棵与 Cards 页同一棵）。
                _rt.UiSetTab(1);
                {
                    var c0 = PoolCellGo(0);
                    CheckTrue(c0 != null && c0.gameObject.activeInHierarchy,
                              "★ Deck info 页：`pool_0` **也画着**（原版 `ToggleDisplay(1)` ⇒ `cardDisplay.SetActive(1)`）"
                            + "｜🧨 改回 `_tab == 0` ⇒ 本条红（这一页右侧整块空白）");
                    CheckTrue(_rt.UiPoolCardAt(0) != null,
                              "★ ……状态里那一格也指着真卡｜🧨 旧写法 `_poolIndex[0] == -1` ⇒ 本条红");
                    Check(PoolCellsShown(), nPool,
                          $"★ Deck info 页画出来的卡池格数 = **{nPool}**（= Cards 页那个数 —— 不是「恰好第 0 格活着」）"
                        + "｜🧨 旧写法这里是 **0** ⇒ 红");
                    float qx, qy, qw, qh;
                    CheckTrue(_rt.UiPoolCellRect(0, out qx, out qy, out qw, out qh),
                              "★ ……而且那一格的**矩形**量得出来（`UiPoolCellRect` 要求视图 `activeSelf` —— 旧写法恒 false）"
                            + $"（实测 {qx:F0},{qy:F0} {qw:F0}×{qh:F0}）");
                    // 「画出来了」还要「摆在**同一处**」—— 这三条（视图 active ✚ `_poolIndex[0] >= 0` ✚ 矩形同值）
                    // 正是 `HandlePoolClick` 命中这一格的前提（它拿**同一颗** `pool_0` transform 做 `Contains`）。
                    // 🔴 ⚠️ 「真点一下」那一步在批处理里**驱动不了**（`HandlePoolClick` 只从 `HandlePointer` 进、
                    //   而批处理没有鼠标 ⇒ `Mouse.current == null`），如实标注 —— 见本轮报告「没做到的」。
                    // 🆕 **2026-10-13 就地订正（铁律 5 · A1702 ③）**：上面那半句**已不再完全成立** ——
                    //   现在有 `DeckRuntime.UiPoolHit(px, py, right)` 这个自检口**直调** `HandlePoolClick`
                    //   （形状同 `UiClickCosmetic` 那条先例；**生产那条完整指针链照旧跑不了**，这一点没变）。
                    //   ⇒ 「卡池这一点吃不吃」现在验得到，见本文件 A1702③ 那一节（灭自证②/③）。
                    CheckTrue(Mathf.Abs(qx - bx) < 0.5f && Mathf.Abs(qy - by) < 0.5f
                              && Mathf.Abs(qw - bw) < 0.5f && Mathf.Abs(qh - bh) < 0.5f,
                              $"★ ……而且与 Cards 页摆的是**同一处**（{qx:F1},{qy:F1} {qw:F1}×{qh:F1} "
                            + $"vs {bx:F1},{by:F1} {bw:F1}×{bh:F1}）—— 命中这一格的三个前提由此齐了");
                }

                // ---- ③ Cosmetics 页：卡池 + **整条**通配符计数条一起关 ----
                _rt.UiSetTab(2);
                {
                    Check(PoolCellsShown(), 0,
                          "★ Cosmetics 页：卡池**一格都不显示**（原版 `cardDisplay.SetActive(0)`）"
                        + "｜🧨 把判据写成恒真（或把 `_tab != 2` 弄丢）⇒ 本条红");
                    CheckTrue(_rt.UiPoolCardAt(0) == null, "★ ……状态里那一格也空了（两条腿一起断）");
                    var c0 = PoolCellGo(0);
                    CheckTrue(c0 == null || !c0.gameObject.activeInHierarchy,
                              "★ ……`pool_0` 也在关着（同一条判据的第三个口）");

                    // 通配符计数条**整条**（底板 + 4 稀有度图标 + 4 数字 + 阵营徽记）——
                    // `wildcards.SetActive(param)` 与卡池是**同一次调用里的同相两件**，一件都不能漏
                    // （漏了就是「一条规则两种结果」）。
                    Check(WcBarShownCount(), 0,
                          $"★ Cosmetics 页：整条通配符计数条**一件都不显示**（实测 {WcBarShownCount()}/10 件："
                        + "底板 1 + 稀有度图标 4 + 数字 4 + 阵营徽记 1）");
                    // 🔴 **2026-10-11（`A1518③`）：上面那条读的是【画面】，这一条读【登记表】** ——
                    //   两张都读、缺一不可（只读画面：把 `_cardsInfoOnly` 那张表整个删掉照样绿；
                    //   只读表：表对了、树没跟着关也照样绿）。判据 → `UiCardsInfoOnlyActive` 的 doc。
                    Check(_rt.UiCardsInfoOnlyActive, 0,
                          "★ Cosmetics 页：登记表那一半（`_cardsInfoOnly`）也**一件都没露**（A1518③，两条腿一起断）");
                    foreach (var k in WcBarQuadNodes)
                    {
                        CheckTrue(_rt.UiHasQuad(k),
                                  "（前提）`" + k + "` **建出来了** —— 不然下面那条「不显示」是拿"
                                + "「件不存在」蒙的（`UiQuadActive` 对不存在的 key 恒答 false，账 `A1532`）");
                        CheckTrue(!_rt.UiQuadActive(k),
                                  "★ Cosmetics 页：`" + k + "` **不显示**（`ImageQuad`，走 `UiQuadActive`）");
                    }
                    for (int i = 0; i < 4; i++)
                        CheckTrue(!NodeShown("hdr_wct" + i),
                                  "★ Cosmetics 页：数字 `hdr_wct" + i + "` 也不显示（它是 `Label` —— "
                                + "`UiQuadActive` 对它**恒答 false**、量不到，只能按名字取 GO 读 `activeInHierarchy`）");
                }

                // ---- ④ 回到 Deck info 页：**逐件反向再断一次** ----
                //  只断「Cosmetics 页关着」会放过「Info 页也误关」（= 把 `_cardsInfoOnly` 那处写成 `_tab == 0`）
                //  ⇒ 这一格与 ① 那一格合起来才把「两页判据」钉死（Cards ✚ Info 都开、Cosmetics 关）。
                _rt.UiSetTab(1);
                {
                    Check(WcBarShownCount(), WcBarNodes.Length,
                          $"★ Deck info 页：整条通配符计数条 **10 件全显示**（实测 {WcBarShownCount()}/10）"
                        + "｜🧨 把 `RefreshTabVisibility` 里 `_cardsInfoOnly` 那一句改成 `_tab == 0`（或干脆不登记）⇒ 红");
                    // 🔴 **`A1518③`**：登记表那一半与画面**同数**（10 件）—— 两条腿交叉印证。
                    //   ⚠️ 「表里被误登记了别的件」也会在这里红（画面只数那 10 个名字，表会把多登记的算进来）。
                    Check(_rt.UiCardsInfoOnlyActive, WcBarShownCount(),
                          $"★ Deck info 页：登记表（`_cardsInfoOnly`，实测 {_rt.UiCardsInfoOnlyActive} 件）"
                        + $"与画面（`WcBarShownCount()`，实测 {WcBarShownCount()} 件）**同数**（A1518③）");
                    foreach (var k in WcBarQuadNodes)
                        CheckTrue(_rt.UiQuadActive(k), "★ Deck info 页：`" + k + "` 显示着（`UiQuadActive`）");
                    for (int i = 0; i < 4; i++)
                        CheckTrue(NodeShown("hdr_wct" + i),
                                  "★ Deck info 页：数字 `hdr_wct" + i + "` 也显示着（同上：它是 `Label`）");
                }

                // ---- ⑤ 切回 Cards：全部回来（收尾；= 下一节的初态）----
                _rt.UiSetTab(0);
                Check(PoolCellsShown(), nPool, "★ 切回 Cards：卡池格数回到基线");
                Check(WcBarShownCount(), WcBarNodes.Length,
                      "★ 切回 Cards：通配符计数条 10 件也全在（🧨 只登记一半、或写成 `_tab == 1` ⇒ 本条红）");
                CheckTrue(_rt.ActiveTab == 0, "（收尾）页签收回 Cards（下一节按 Cards 页在跑）");
            }

            // ============================================================ 🔴 2026-10-11（`A1514`）
            // **卡组类型计数抽屉**（原版 `Sidebar/Deck Details/Deck Counter Drawer`，类 `DeckTypeCounterDrawer`）
            //   —— 原版**有**、我们**没建**（缺件，铁律 11；⛔ 不是「原版没有」）。
            // 判据（逐条；几何/文案/计数/显隐的完整叙述 → `Deck/DeckRuntime.cs` 的类级 `Tc*` 常量那一段）：
            //   · 几何：RT `-4772579933788246236`（`anchorMin (0,1)` / `anchorMax (1,1)` / `pivot (0.5,1)` /
            //     `anchoredPosition (0,−400)` / `sizeDelta (0,100)` / **`scale 1`**），父 = `Deck Details`
            //     （绝对 `0.25,360.97 → 335.56,1010.03`）⇒ 抽屉绝对 **x 0.25..335.56 · y 760.97..860.97**；
            //     上格 = `stratagemsCount`（RT `2367829158540668708`，`anchors (0,0.5)-(1,1)`）·
            //     下格 = `troopCount`（RT `-3202665307648495836`，`anchors (0,0)-(1,0.5)`）——
            //     两颗都是 `apos.x 12.5` + `sd.x −25` ⇒ **x 25.25..335.56（宽 310.31）· 高 50**。
            //   · 文案 = `String.Format("{0}: {1}", 词条, 计数)`；格式串与两个词条键都是
            //     `d:/2/tools/il2cpp_out/stringliteral.json` 现查出来的（`DAT_18426e800` / `…cfdf0` / `…cfef0`）。
            //   · 计数 = 原版谓词 `card+0x94 == 0`（`Minion`）/ `== 0x14`（`Tactic`）
            //     = 我们的 `"unit"` / `"tactic"`，**每一份拷贝算一张**（原版数的是 `deck.cardLibrary`）。
            //   · 显隐：`DeckEditingPanel__ToggleDeckInfo.c` 才把它设 **1**、`__ToggleDeckList.c` /
            //     `__ToggleCosmetics.c` 都设 **0** ⇒ **只在 Deck info 页签**。
            // ⛔ 期望值一律**独立算**（从 `DeckEntries()` + `CountOf`、从上面这些原版读数），
            //   ⛔ 不是从 `DeckRuntime` 的 `Tc*` 常量读回来 —— 读了就是自证。
            {
                Section("★ A1514：卡组类型计数抽屉（原版 `Deck Counter Drawer` = 部队 / 策略两行）");

                // 期望的两个数：从**卡组**现算（督军 / 防御卡不进这两档 —— 原版谓词只认 Minion / Tactic）
                int wantTroops = 0, wantStrat = 0;
                foreach (var e in _rt.DeckEntries())
                {
                    if (e == null) continue;
                    int cn = (e.Type == "hero" || e.Type == "defence") ? 1 : state.Deck.CountOf(e.Id);
                    if (e.Type == "unit") wantTroops += cn;
                    else if (e.Type == "tactic") wantStrat += cn;
                }
                CheckTrue(Loc.HasEntry("MenuDeck/HUD/DeckDescription/Minions")
                          && Loc.HasEntry("MenuDeck/HUD/DeckDescription/Spells"),
                          "（前提）两个词条键都在表里（⛔ 不在 ⇒ `Loc.T` 返回**键名本身** "
                        + "⇒ 下面两条两边一起退化 ⇒ 假绿）");

                // ---- ① Deck info 页：两行都该在；文案 / 左沿 / 纵向位置 / 字号逐条断 ----
                _rt.UiSetTab(1);
                {
                    CheckTrue(NodeShown("typecnt_strat") && NodeShown("typecnt_troops"),
                              "★ Deck info 页：两行都**显示着**"
                            + "（原版 `DeckEditingPanel__ToggleDeckInfo.c`：`deckCounterDrawer(0x38) → 1`）");
                    Check(_rt.UiLabelText("typecnt_troops"),
                          Loc.T("MenuDeck/HUD/DeckDescription/Minions") + ": " + wantTroops,
                          "★ …下行 = `词条(部队) + \": \" + 部队数`（期望值 **独立算** = " + wantTroops + "）"
                        + "｜🧨 格式串换成别的（如只印数字）/ 把「策略」也数进「部队」⇒ 红");
                    Check(_rt.UiLabelText("typecnt_strat"),
                          Loc.T("MenuDeck/HUD/DeckDescription/Spells") + ": " + wantStrat,
                          "★ …上行 = `词条(策略) + \": \" + 策略数`（独立算 = " + wantStrat + "）"
                        + "｜🧨 两行的计数对调 ⇒ 红");

                    float lS, rS, cyS, lT, rT, cyT;
                    bool okS = NodeInkPx("typecnt_strat", out lS, out rS, out cyS);
                    bool okT = NodeInkPx("typecnt_troops", out lT, out rT, out cyT);
                    CheckTrue(okS && okT, "（前提）两行的**墨迹框**都量得出来（`Label.WorldW` 真测量）");
                    if (okS && okT)
                    {
                        CheckNear(lS, 25.25f, 1f,
                                  "★ 上行墨迹**左沿 = 25.25**（原版 `apos.x 12.5` + `sd.x −25` 解出来的框左沿；"
                                + "实测 " + lS.ToString("F1") + "）｜🧨 退回居中 ⇒ 依文字长短左右漂 ⇒ 红");
                        CheckNear(lT, 25.25f, 1f,
                                  "★ 下行墨迹左沿同样 = **25.25**（左对齐 ⇒ 与文字长短无关；实测 "
                                + lT.ToString("F1") + "）");
                        CheckNear(cyS, 785.97f, 0.8f,
                                  "★ 上行中心 y = **785.97**（= 抽屉顶 760.97 + 半格 25；实测 " + cyS.ToString("F1") + "）");
                        CheckNear(cyT, 835.97f, 0.8f,
                                  "★ 下行中心 y = **835.97**（= 760.97 + 50 + 25；实测 " + cyT.ToString("F1") + "）");
                        CheckNear(cyT - cyS, 50f, 0.5f,
                                  "★ 两行**间距 = 50**（原版两格各占抽屉一半、**无缝** —— 实测 "
                                + (cyT - cyS).ToString("F1") + "）｜🧨 两行叠在一起 / 拉开 ⇒ 红");
                    }

                    var sT = FindDeep(_rt.Root, "typecnt_strat");
                    var sLb = sT != null ? sT.GetComponent<Label>() : null;
                    CheckTrue(sLb != null, "（前提）`typecnt_strat` 那颗是 `Label`");
                    if (sLb != null)
                    {
                        CheckNear(sLb.FontPxNow, 36f, 0.6f,
                                  "★ 字号 = **36**（原版那颗 TMP 的 `m_fontSize = 36`；实测 "
                                + sLb.FontPxNow.ToString("F1") + "）");
                        CheckTrue(!sLb.AutoSizing,
                                  "★ 自适应**关**（原版 `m_enableAutoSizing = 0` —— 别顺手套一个 `SetAutoFitBox`）");
                        Check(sLb.WrappingMode, 1,
                              "★ 折行 = 原版 **`m_TextWrappingMode = 1`**（`Label` 出厂是 `NoWrap` ⇒ 要显式还原）");
                        CheckTrue(sLb.color.r > 0.99f && sLb.color.g > 0.99f && sLb.color.b > 0.99f
                                  && sLb.color.a > 0.99f,
                                  "★ 字色 = **白 `(1,1,1,1)`**（原版 `m_fontColor`；实测 "
                                + sLb.color + "）");
                        Check(sLb.VAlignTier, Label.VAlign.Middle,
                              "★ 垂直档 = 原版 `VA 512` = **Middle**");
                    }
                }

                // ---- ② 另两页：一颗都不许露（原版那两个 `Toggle*` 都把它设 0）----
                _rt.UiSetTab(0);
                CheckTrue(!NodeShown("typecnt_strat") && !NodeShown("typecnt_troops"),
                          "★ Cards 页：两行**都不显示**（`DeckEditingPanel__ToggleDeckList.c` 把 "
                        + "`deckCounterDrawer(0x38) → 0`）｜🧨 把它登记进 `_cardsInfoOnly`（Cards 也显示）⇒ 红");
                _rt.UiSetTab(2);
                CheckTrue(!NodeShown("typecnt_strat") && !NodeShown("typecnt_troops"),
                          "★ Cosmetics 页：同样**不显示**（`__ToggleCosmetics.c` 也是 0）");
                _rt.UiSetTab(0);
            }

            // ============================================================ 🔴 2026-10-11（`A1515`）
            // **卡牌那栏左抽屉也吃 `Card Display` 的页判据** —— 判据 = 原版那棵 `Card Filters` 挂在
            //   `Card Display` **底下**（`menu_dump.py bundle_menus_assets_all "Deck Editing Menu"` 的缩进：
            //   `2 Card Display` → `3 Card Filters`）⇒ `ToggleDisplay` 那一个入参一动整栏跟着不画
            //   （入参 → `DeckRuntime.CardDisplayPage` 的 doc）。
            // 改之前 `_fltSlide.PageOn` **全文件从没被赋过值**（`FilterDrawer` 的初值恒 `true`）⇒
            //   在 Cosmetics 页上整栏照画、而且那一条竖带会把左侧 331.7px **吃掉**（该由卡背筛选接管）。
            // 🧨 两格都断（「不画」+「不吃指针」）：只断前者会放过 `FltStripOn` 漏页判据那一半。
            {
                Section("★ A1515：卡牌筛选栏吃【页判据】—— Cosmetics 页整栏不画、那条竖带也不吃指针");
                _rt.UiSetTab(0);
                if (!_rt.FiltersOpen) _rt.UiToggleFilters();
                CheckTrue(_rt.FiltersOpen, "（前提）Cards 页：卡牌筛选栏开着（逻辑态）");
                CheckTrue(_rt.UiQuadActive("flt_bg"), "★ Cards 页：底板**露着**（整栏在位，与本节改前一致）");
                CheckTrue(_rt.FilterDrawerInteractive, "★ Cards 页：那条竖带**吃指针**（`FltStripOn` 为真）");
                _rt.UiSetTab(2);
                CheckTrue(_rt.FiltersOpen, "（前提）切页**不翻逻辑态**（`_filtersOpen` 跨页保留，翻它的是 Filters 键）");
                CheckTrue(_rt.UiHasQuad("flt_bg"),
                          "（前提）`flt_bg` **建出来了** —— 不然下面那条「整栏不画」是拿「件不存在」蒙的（账 `A1532`）");
                CheckTrue(!_rt.UiQuadActive("flt_bg"),
                          "★ Cosmetics 页：卡牌筛选栏**整栏不画**（原版它挂在 `Card Display` 底下）"
                        + "｜🧨 不给 `PageOn` 赋页判据（旧写法恒 `true`）⇒ 本条红");
                CheckTrue(!_rt.FilterDrawerInteractive,
                          "★ ……而且那条竖带**不吃指针**（否则 Cosmetics 页左侧那 331.7px 会被【卡牌筛选】"
                        + "吃掉，而那一刻该接管的是卡背筛选）｜🧨 `FltStripOn` 漏页判据 ⇒ 本条红");
                _rt.UiSetTab(0);
                CheckTrue(_rt.UiQuadActive("flt_bg"),
                          "★ 切回 Cards：整栏**回来**（逻辑态一直开着 ⇒ 不用再点一次 Filters）");
                if (_rt.FiltersOpen) _rt.UiToggleFilters();       // 收尾：关回去（本节不许改后面用例的初态）
                _rt.UiSetTab(0);
            }

            // ============================================================ 🆕 2026-10-20
            // **卡组行上的两颗数**：左圆（原版 `…/Background/Cost Image/Cost`）= **费用** ·
            //   行右缘（原版 TMP 名 `Count`）= **份数**。判据（逐条带出处；期望值一律**从卡表现算**，
            //   ⛔ 不是从 `DeckRuntime` 的常量读回来）：
            //   ① 左圆里印的是 `CardDef.Cost`（字段名现核：`RuleEngine/Core/CardDef.cs` 的 `Cost`）
            //      —— ⛔ **不是份数**。**改之前两颗喂的都是份数** ⇒ **费用哪都没印**（本件要修的就是这处）。
            //   ② 行右缘那颗 `row_x*` **只有份数 > 1 才显示**：原版 `UICardInfoItem__SetCount.c`
            //      里那一句 `gameObject.SetActive(1 < count)`。
            //   ③ 它的文本 = **`"x" + n`**（同函数那个 `String.Format` 的字面量 = **`x{0}`**，
            //      小写 x、**无空格**）。
            //   🧨 鉴别力（灭自证），两条缺一不可：
            //     · ① **必须至少有一行「费用 ≠ 份数」** —— 两数在整副牌里恒相等的话，
            //       一个「圆里印份数」的实现**照样全绿**（这条断言等于没断）；
            //     · ② 的**两种态要各见一行**（份数 > 1 的、份数 = 1 的）—— 只验一种的话
            //       「恒显示」「恒隐藏」两种实现都能混过去（督军 / 防御卡那两行天然是 1）。
            {
                Section("卡组行的两颗数：费用圆 = `Cost` · 行右缘 `row_x*` = 份数（>1 才显示）");

                // 🧨 鉴别力那一条（在**整副牌**上算 —— 比只看一屏 11 行稳，且不改变「量的是谁的」）
                int diffFromCopies = 0;
                foreach (var e in _rt.DeckEntries())
                {
                    int en = (e.Type == "hero" || e.Type == "defence") ? 1 : state.Deck.CountOf(e.Id);
                    if (e.Cost != en) diffFromCopies++;
                }

                int rows = 0, showN = 0, hideN = 0;
                string badCost = null, badX = null, badHide = null;
                string badCntVis = null;               // 🆕 2026-10-21：`row_cnt*` 的**可见性**反例（原来整片空白）
                for (int i = 0; i < 11; i++)            // 只查前 11 行（一屏现在是 12 行 —— 第 12 槽单独在
                                                        // `A1727` 那一节里断；⛔ 别把这里也写成 12：
                                                        // 这一段的目的是「逐行核对费用/份数」，多一行不加鉴别力）
                {
                    var d = _rt.UiDeckRowAt(i);
                    if (d == null) break;
                    rows++;
                    // 督军 / 防御卡各占一格 ⇒ 份数恒 1（与 `RefreshDeckList` 里那一句同口径）
                    int n = (d.Type == "hero" || d.Type == "defence") ? 1 : state.Deck.CountOf(d.Id);
                    // ---- ① 费用圆里那个数 ----
                    string costTxt = _rt.UiLabelText("row_cnt" + i);
                    if (costTxt != d.Cost.ToString() && badCost == null)
                        badCost = "第 " + i + " 行「" + CardText.Name(d.Name, d.NameZh) + "」左圆印的是「"
                                + costTxt + "」，而它的 `Cost` = **" + d.Cost + "**（份数 = " + n + "）";
                    // ⚠️ **2026-10-21 补（铁律 5·b）**：上一条**只验文案** ——
                    //   `Deck/DeckRuntime.cs` 的 `UiLabelText` **不看 `activeSelf`**
                    //   ⇒ 「督军行那颗 `0` 该藏没藏」**整片是空白、改前改后都绿**。
                    //   现在同一格顺手把**可见性**也断上：行可见时，`row_cnt*` 只该在**非督军行**上显示
                    //   （判据 = `RefreshDeckList` 里 `heroRow` 那两句；这里读的是 **`activeSelf`**、不是文案）。
                    var cntGo = FindDeep(_rt.Root, "row_cnt" + i);
                    bool cntShown = cntGo != null && cntGo.gameObject.activeSelf;
                    bool cntWant = d.Type != "hero";
                    if (cntShown != cntWant && badCntVis == null)
                        badCntVis = "第 " + i + " 行（`Type=" + d.Type + "`）该" + (cntWant ? "显示" : "不显示")
                                  + "，实得 " + (cntShown ? "显示着" : "藏着");
                    // ---- ② ③ 行右缘那颗 ----
                    var xGo = FindDeep(_rt.Root, "row_x" + i);
                    var xLb = xGo != null ? xGo.GetComponent<Label>() : null;
                    if (xLb == null)
                    { if (badX == null) badX = "第 " + i + " 行找不到节点 `row_x" + i + "`"; continue; }
                    if (n > 1)
                    {
                        if (xLb.gameObject.activeSelf && xLb.Text == "x" + n) showN++;
                        else if (badX == null)
                            badX = "第 " + i + " 行份数 = " + n + "（> 1）⇒ `row_x" + i
                                 + "` 该显示并印「x" + n + "」，实得 显示=" + xLb.gameObject.activeSelf
                                 + "、文本=「" + xLb.Text + "」";
                    }
                    else
                    {
                        if (!xLb.gameObject.activeSelf) hideN++;
                        else if (badHide == null)
                            badHide = "第 " + i + " 行份数 = 1 ⇒ `row_x" + i
                                    + "` 该**不显示**（原版 `SetActive(1 < count)`），实得**显示着**";
                    }
                }
                CheckTrue(rows >= 3, "（前提）一屏里量到了 " + rows + " 行（至少要 3 行才有鉴别力）");
                CheckTrue(badCost == null,
                          "★ ① 卡组行左圆里那个数 = 该卡的 `Cost`（**每一行**都对）"
                        + (badCost == null ? "；⛔ 不是份数" : "；反例：**" + badCost + "**"));
                CheckTrue(badCntVis == null,
                          "★ ①b 每一行 `row_cnt*` 的**可见性** = 「这一行不是督军行」"
                        + "（督军行不印费用圆、也不印圆里那个数）"
                        + "（⛔ 本条必须读 **`activeSelf`** —— 上一条走的 `UiLabelText` 只读 `Label.Text`、"
                        + "不看可见性）"
                        + (badCntVis == null ? "" : "；反例：**" + badCntVis + "**"));
                CheckTrue(diffFromCopies > 0,
                          "★ ①'（鉴别力）整副牌里**至少有一行「费用 ≠ 份数」**（实测 " + diffFromCopies + " 行）"
                        + " —— 两数恒相等的话，一个「圆里印份数」的实现**照样全绿**");
                CheckTrue(showN > 0 && badX == null,
                          "★ ② ③ 份数 > 1 的行：`row_x*` **显示**且文本 = **`x` + 份数**（实测 " + showN + " 行对）"
                        + (badX == null ? "" : "；反例：**" + badX + "**"));
                CheckTrue(hideN > 0 && badHide == null,
                          "★ ② 份数 = 1 的行：`row_x*` **不显示**（原版 `SetActive(1 < count)`；实测 " + hideN + " 行）"
                        + (badHide == null ? "" : "；反例：**" + badHide + "**"));
            }

            // ============================================================ 🆕 2026-10-21（`A1735`）
            // **费用圆里那颗数字的 TMP 文本框** = 原版 **48.82 × 47.88**（⛔ 不是费用图那个 61.7）。
            // 判据 = 原版 prefab 现读（`工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu"
            //   --depth 14`）：`…/Deck Selector Card Info button/Background/Cost Image/Cost` =
            //   **48.82 × 47.88**（`字号=50.0` · `基准=32.0` · `auto[18.0~50.0]` · `Center/Capline`），
            //   父 `Cost Image` = **61.70 × 61.70**。
            //   🔴 **2026-10-14（第二十九会话）就地订正**：这里原来还写「督军行那一族
            //   （Hero Card Info button）逐位同值」—— **错的**，督军行子树里**没有** `Cost Image`/`Cost`。
            //   现读全树只有 3 个 `Cost Image`：卡行 61.70（有 ARF）· 防御卡槽 61.70（有 ARF，与卡行逐位同值）·
            //   卡槽预览 **62.00（无 ARF，另一族）**。判据与订正痕 → `DeckRuntime.cs` 的 `RowCostTxW` 那一段。
            // 🔴 **为什么会漏**：我们把**图框**（`RowCostS 61.7`）直接当字框用了 —— 今天两边都停在
            //   自适应上限 50（`row_cnt` 是 1~2 位数字，两个框都塞得下）⇒ 画面同级、一直没露；
            //   **要缩字时我们会比原版【晚缩】**（原版框窄 12.88px）—— 那才是不符原版的那一半。
            // 🔴 **灭自证两条腿**：① 把字框改小、顺便把**费用图**也改小（两颗混成一颗）⇒ ③ 红；
            //   ② 只把框改小、没看**字号**（框一小自适应就可能把字压下去）⇒ ⑥ 红。
            {
                Section("★ A1735：费用数字的文本框 = 原版 48.82 × 47.88（费用图仍是 61.7 · 字仍停在上限）");
                int rk = -1; GameObject cntGo = null; Label cntLb = null;
                for (int i = 0; i < 11 && cntGo == null; i++)
                {
                    var g = FindDeep(_rt.Root, "row_cnt" + i);
                    if (g != null && g.gameObject.activeSelf) { rk = i; cntGo = g.gameObject; cntLb = g.GetComponent<Label>(); }
                }
                CheckTrue(cntGo != null && cntLb != null,
                          "（前提）量得到一颗**显示着**的 `row_cnt*`（= 非督军行的费用数字）");
                if (cntGo != null && cntLb != null)
                {
                    var tmp = cntGo.GetComponentInChildren<TMPro.TMP_Text>(true);
                    CheckTrue(tmp != null, "（前提）那颗 `row_cnt*` 底下有**真 TMP**（点阵后端没有框可量 ⇒ 会假绿）");
                    if (tmp != null)
                    {
                        // `SetAutoFitBox` 把框写进 TMP 自己的 `sizeDelta`（**世界单位**）⇒ × `PxPerUnit` 回设计 px。
                        // ⛔ 别拿 `Label.WorldW/WorldH` 量 —— 那是**墨迹**（字占多大），框换了它**不会变**。
                        var box = tmp.rectTransform.sizeDelta * DeckRuntime.PxPerUnit;
                        CheckNear(box.x, 48.82f, 0.05f,
                                  "★ A1735①：费用数字的**文本框宽** = 原版 **48.82**"
                                + "｜🧨 仍拿费用图那个 `RowCostS 61.7` 当字框 ⇒ 红");
                        CheckNear(box.y, 47.88f, 0.05f, "★ A1735②：…**框高** = 原版 **47.88**");
                        CheckTrue(_rt.UiQuadRect("row_c" + rk, out float icx, out float icy,
                                                 out float iw, out float ih),
                                  "（前提）同一行的费用图 `row_c" + rk + "` 量得到");
                        CheckNear(iw, 61.7f, 0.05f,
                                  "★ A1735③ 灭自证①：**费用图**仍是原版 **61.7**（宽与高同一颗常量）"
                                + "｜🧨 把 `RowCostS` 一起改小 ⇒ 红（原版这两颗是**两件东西**）");
                        CheckNear(ih, 61.7f, 0.05f, "★ A1735③b：…费用图的高也是 **61.7**");
                        // 与费用图的关系：原版四边等距内缩（6.44 / 6.91）⇒ **框心 = 图心**
                        // ⚠️ 读**世界坐标**（`transform.position`）—— 与 `UiQuadRect` 同一条口
                        var bc = _rt.ToPx(cntGo.transform.position);
                        CheckNear(bc.x - icx, 0f, 0.05f,
                                  "★ A1735④：字框**框心**与费用图同 x（`MoveRow` 的 `MoveLabel` 每刷新搬回图心）"
                                + "｜🧨 换尺寸却只挪左上角 ⇒ 数字整体偏 ⇒ 红");
                        CheckNear(bc.y - icy, 0f, 0.05f, "★ A1735⑤：…同 y");
                        // 🔴 **灭自证②：框换成原版尺寸后，字**没有被压小**。
                        //   ⚠️ **2026-10-17（R1 现核 · γ）就地改口径**：第四十八会话把 `TmpFont.EmPerFontSize`
                        //   改成与字体面无关（拉丁面 0.1039 → 0.0948，**+9.6%**）⇒ 该颗的 `fontSizeMax`
                        //   从 4.45 涨到 **4.88**，于是它从「撞上限」变成「被框轻压」**4.65 / 4.88**（差 4.7%）。
                        //   ⇒ 判据改成「**停在 max 的一个自适应步长以内**」—— 仍能咬住「框改小把字压下去」
                        //   （例如压到 2.0 ⇒ 红），但不再要求逐位撞墙。
                        //   🔴 **2026-10-18（`N55`）**：`EmPerFontSize` 再次改口径（拉丁面 0.0948 → 原版真 em 0.1）
                        //   ⇒ 这一颗（`LatinCardBody`）的 `fontSizeMax` 收到 **≈4.63**、实得 **≈4.41**（差 ≈4.7%）
                        //   —— 「一个自适应步长」这条关系式**含义不变**、余量 ≈0.03（**只剩这一点**，见报告 §3）。
                        //   判据 → `R4_Rewards残差.md` §五·B ＋ `D1_桶A字号换算链.md` §四 方案 B。
                        CheckTrue(tmp.fontSizeMax > 0f && tmp.fontSize >= tmp.fontSizeMax - 0.25f,
                                  $"★ A1735⑥ 灭自证②：字号**没有被框压小**（实得 {tmp.fontSize:F2} / 上限 {tmp.fontSizeMax:F2}；"
                                + "容差 0.25 = 一个自适应步长）"
                                + " —— 框换成原版尺寸后画面与原版同级"
                                + "｜🧨 框再小一点把字压下去 / 把上限改小 ⇒ 红");
                    }
                }
            }

            // ============================================================ 🔴 2026-10-11（`A1528`）
            // **行右缘那颗 `Count` 的【字色 + 自适应】。**
            // 判据 = 原版 prefab 实读，**按 RT pid 认人**（`bundle_menus_assets_all` 里名叫 `Count` 的 TMP
            //   共 15 颗、分 3 族 —— 铁律 4：各自都对 ⇒ **按用途取**，⛔ 不投票）：
            //   · 本窗左栏这一份属 **`Deck Selector Card Info button` 族**（`Count` RT
            //     **`-2738939269725491420`**，父 `Text fill`）⇒ **`m_fontColor = (0.9333333969116211, …, 1)`** ·
            //     **`m_enableAutoSizing = 1`** · `m_fontSizeMin = 2.0` · `m_fontSizeMax = 32.0` · `m_fontSizeBase = 36.0`。
            //   · 另一族是 `Deck Selector Hero Card Info button`（**督军行**那一颗，`(1,1,1,1)` + `auto = 0`）——
            //     原版**永远看不到它**（`UICardInfoItem__SetCount.c:41` 的 `SetActive(1 &lt; count)`，督军恒 1 张）
            //     ⇒ 拿它那套来断就是**断错对象**（这一条账原来的批注就是这么写错的）。
            //   出处 → `资料/普查产出_第十七会话/Z_两处悬案.md` §A ·
            //           `资料/历史/A表已收口_第十七会话.md` 的 `A1528` 那一行。
            //   ⛔ 下面四个期望值**写死在断言里**（原版读数），⛔ **不是**从 `DeckRuntime` 的
            //     `RowCountTint` / `RowCountFontPx` 读回来 —— 读了就是自证。
            //   🧨 灭自证：只断「开没开自适应」不够（把 `SetAutoFitBox` 删掉也答 false ⇒ 红）；
            //     所以**上下限与基准逐格断**（旧实现这四格里**一格都没有**）。
            {
                Section("★ A1528：行右缘 `row_x*` —— 字色 `(0.93333,0.93333,0.93333,1)` + 自适应开 `[2~32]` 基准 36"
                        + " · 墨迹右缘 309.48");
                Label x0 = null;
                for (int i = 0; i < 11; i++)
                {
                    var g = FindDeep(_rt.Root, "row_x" + i);
                    if (g == null) continue;
                    x0 = g.GetComponent<Label>();
                    if (x0 != null) break;
                }
                CheckTrue(x0 != null, "（前提）量得到一颗 `row_x*`（11 行里至少建出一颗）");
                if (x0 != null)
                {
                    CheckNear(x0.color.r, 0.93333f, 0.002f, "…字色 R = **0.93333**（原版 `m_fontColor.r`；⛔ 不是 1.0）");
                    CheckNear(x0.color.g, 0.93333f, 0.002f, "…字色 G = **0.93333**");
                    CheckNear(x0.color.b, 0.93333f, 0.002f,
                              "…字色 B = **0.93333**（⛔ 不是 `Ink` 的 0.95 —— 那一颗三通道**等值**）");
                    CheckTrue(x0.AutoSizing,
                              "…**`m_enableAutoSizing` 开着**（原版 Card Info 族是 **1**；"
                            + "🧨 旧的「固定 32 无自适应」实现这一格**恒红**）");
                    CheckNear(Label.FontSizeToPx(x0.FontSizeMin), 2f, 0.2f,
                              "…自适应**下界** = **2px**（原版 `m_fontSizeMin = 2.0` 按同一套比例折出来）");
                    CheckNear(Label.FontSizeToPx(x0.FontSizeMax), 32f, 0.2f,
                              "…自适应**上界** = **32px**（原版 `m_fontSizeMax = 32.0`）");
                    CheckNear(Label.FontSizeToPx(x0.FontSizeBase), 36f, 0.2f,
                              "…自适应**基准** = **36px**（原版 `m_fontSizeBase = 36.0`）"
                            + "｜🧨 只开自适应、不给 base（缺省 = 标称 32）⇒ 本条红");
                }

                // ---- 🆕 第二轮（`A1528`）：**位置**那一半（框沿 + 对齐档）----
                // 判据 = **原版 prefab 字段 + 布局式子**（逐条可复跑，完整推导 → `DeckRuntime.RowCountRightInset`）：
                //   · 行节点宽 309（抽屉 `Content` 的 `VerticalLayoutGroup` `pad 8,8`）·
                //     `Background`（`Text fill` 的父）行内 17.255..309.00 ·
                //     `Text fill` 行内 **55.182..301.083** ⇒ **右沿 = 行右缘 − 7.917**；
                //   · `Count` 是 `Text fill` 的**最后一个**子件 + `m_HorizontalAlignment = 4(Right)`
                //     + `m_ChildForceExpandWidth = 1` ⇒ **墨迹右缘 = 行右缘 − 7.917**。
                //   套到**我们自己的行**（**行节点** = `RowNodeX..RowNodeX+RowNodeW` = 8.4..317.4）⇒ **309.48**。
                //   **参考图**（2026-10-11 与 2026-10-21 两次在图上逐像素量过，k = 1.21442）：
                //   `x2` 墨迹右缘 ≈ ref **255** px ⇒ 画布 **309.7**，**行底板右缘** = ref **261** px ⇒ **317.0**。
                //   ⇒ **两条一致**（图上墨迹离底板右缘 ≈ 7.3 画布 px vs prefab 的 7.917；绝对值 309.7 vs 309.48）。
                //   🔴 **2026-10-21（`A1700`）就地订正（铁律 5）**：本段原来把行右缘写成 **325.4**
                //      （= **抽屉**右缘）⇒ 期望值写成 317.48，并把它挂成「另账」等以后处理。
                //      **那条另账已收口**：行节点右缘 = **317.4**（抽屉各缩 8，判据 → `DeckRuntime.RowNodeW`），
                //      ⇒ 期望值 = **309.48**，与参考图 309.7 同一个数（差 0.22 = 半个 ref px）。
                //   ⛔ 309.48 是**写死的绝对数**（原版规则 + 行节点右缘），⛔ 不是从
                //      `DeckRuntime.RowCountRight` 读回来 —— 读了就是自证。
                //   ⚠️ **鉴别力（如实登记）**：旧写法（50 宽贴行右缘 + 框内居中）对**两字**文本
                //      **恰好**落在 ≈317.9 ⇒ 本条只分得出 **0.4px**；它真正咬得住的是
                //      「**只改对齐、不改框右沿**」（A1700 之前那一版会顶到行节点右缘 **317.4** ⇒ 红 **7.9px**）。
                {
                    int xi = -1;
                    for (int i = 0; i < 11; i++)
                    {
                        var d = _rt.UiDeckRowAt(i);
                        if (d == null || d.Type == "hero" || d.Type == "defence") continue;
                        if (state.Deck.CountOf(d.Id) <= 1) continue;      // 份数 ≤ 1 ⇒ 那颗藏着（没有右沿可量）
                        var t = FindDeep(_rt.Root, "row_x" + i);
                        if (t != null && t.gameObject.activeInHierarchy) { xi = i; break; }
                    }
                    CheckTrue(xi >= 0, "（前提）找到一行**份数 > 1 且 `row_x*` 显示着**的行");
                    if (xi >= 0)
                    {
                        float lx, rx, cyy;
                        CheckTrue(NodeInkPx("row_x" + xi, out lx, out rx, out cyy),
                                  "（前提）第 " + xi + " 行那颗的**墨迹框**量得出来");
                        CheckNear(rx, 309.48f, 0.4f,
                                  "★ 墨迹**右缘 = 309.48**（原版规则「行节点右缘 − 7.917」套到我们的行节点 8.4..317.4 上；"
                                + "参考图独立核过：墨迹右缘 ref 255 ⇒ 画布 309.7，与 309.48 同一个数；"
                                + "实测 " + rx.ToString("F2") + "）"
                                + "｜🧨 **只改对齐、不改框右沿** ⇒ 顶到行节点右缘 317.4 ⇒ 红 7.9px"
                                + "｜🧨 退回「框内居中」⇒ 偏移随文字宽度漂（今天两字 ≈309.9 ⇒ 本条只分 0.4px，已如实登记）");
                    }
                }
            }

            // ============================================================ 🆕 2026-10-21（`A1527` + `A1525`）
            // 判据逐条列在 `RowNameInkPx` 上面那一段（⛔ 不在这里再写第二份）。
            // 🔴 本节**全部读「画面」或「刚量出来的数」**，不读 `DeckRuntime` 里那几个常量
            //   （`RowNameX` 那一类）也不读它自己的排序函数 —— 读了就是自证。
            {
                Section("★ A1527 / A1525：卡组列表 —— 督军行无费用圆 · 一卡一行 · 费用→本地化名（文化比较）· 行名左对齐 68");
                _rt.UiSetTab(0);
                _rt.UiScrollDeck(-9999f);          // 摆回基线：列表滚回顶（上一节量悬浮 / 页签，可能动过滚动）

                // ---- ① A1527：督军行**两颗一起**都不显示（圆 + 圆里的数）----
                {
                    var heroD = _rt.UiDeckRowAt(0);
                    CheckTrue(heroD != null && heroD.Type == "hero",
                              "（前提）列表第 0 行 = 督军（"
                            + (heroD == null ? "<空>" : CardText.Name(heroD.Name, heroD.NameZh)) + "）");
                    CheckTrue(_rt.UiHasQuad("row_c0"),
                              "（前提）`row_c0`（费用圆那颗）**真建出来了** —— 不然下面那条「不显示」是拿"
                            + "「件不存在」蒙的（`UiQuadActive` 对不存在的 key 恒答 false）");
                    CheckTrue(!_rt.UiQuadActive("row_c0"),
                              "★ **督军行没有费用圆**（`row_c0` 不显示）—— 判据 = Hero 族预制体**没有** `Cost Image` 子件"
                            + "（`菜单全树.md:9321-9329`）+ 参考图第 0 行无圆、无 `x2`"
                            + "（⛔ 不是「费用 < 0」那条 —— 我们表里督军 `cost` 恒 0、那一条在本数据上恒真）"
                            + "｜🧨 把 `RefreshDeckList` 的 `on && !heroRow` 改回 `on` ⇒ 本条红");
                    var cnt0 = FindDeep(_rt.Root, "row_cnt0");
                    CheckTrue(cnt0 != null && !cnt0.gameObject.activeSelf,
                              "★ ……**圆里那个数也不显示**（`row_cnt0`）—— 本工程这俩是**两颗独立物体**"
                            + "（`DeckRuntime` 里 `Img(\"row_c\"+i, …)` 与 `Txt(\"row_cnt\"+i, …)` 各建一颗）"
                            + "｜🧨 只把 `row_c*` 那句改回 `on`、`row_cnt*` 留着 ⇒ 督军行会留一颗**飘着的 `0`** ⇒ 本条红"
                            + "｜⚠️ 本条必须读 `activeSelf` —— `UiLabelText` **不看 `activeSelf`**，上面那块原来只验文案");
                }

                // ---- ② A1527 反向（鉴别力）：防御卡那一行**照样印** ----
                //  🔴 防御卡与督军**走两条不同的机制**：督军靠**预制体结构**（Hero 族压根没这件子件），
                //    防御卡靠**运行期值闸**（Defensive 族**有** `Cost Image`，开关是 `Coin >= 0`，
                //    而它费用是真值 `0/1/2/3` —— 卡面上真有蓝色六边形）⇒ 一起藏 = 造一条**新**偏离。
                {
                    int defRow = -1; CardDef defDef = null;
                    for (int i = 0; i < 11; i++)
                    {
                        var d = _rt.UiDeckRowAt(i);
                        if (d != null && d.Type == "defence") { defRow = i; defDef = d; break; }
                    }
                    CheckTrue(defRow >= 0, "（前提）一屏里量得到**防御卡那一行**（没有它 ⇒ 下面两条是空转）");
                    if (defRow >= 0)
                    {
                        CheckTrue(_rt.UiQuadActive("row_c" + defRow),
                                  "★ **防御卡行照样印费用圆**（第 " + defRow + " 行「" + CardText.Name(defDef.Name, defDef.NameZh)
                                + "」`cost=" + defDef.Cost + "`）—— 判据 = Defensive 族**有** `Cost Image`（`菜单全树.md:9337`）"
                                + "+ 卡面右上真有蓝色六边形（`Aeldari/5防御卡/Warpforge_67_Aspect-Shrine.png` = `1`）"
                                + "｜🧨 顺手连防御卡一起藏（`!(heroRow || rowDefence)`）⇒ 本条红");
                        var cntD = FindDeep(_rt.Root, "row_cnt" + defRow);
                        CheckTrue(cntD != null && cntD.gameObject.activeSelf,
                                  "★ ……圆里那个数也在（`row_cnt" + defRow + "`）⇒ 两颗同源，不是「圆在、数没了」");
                    }
                }

                var entries = _rt.DeckEntries();

                // ---- ③ A1525①：一卡一行（按 `Id` 去重）----
                {
                    var seenIds = new HashSet<string>();
                    string dupId = null;
                    foreach (var e in entries)
                        if (e != null && !seenIds.Add(e.Id)) { dupId = e.Id; break; }
                    CheckTrue(dupId == null,
                              "★ **一卡一行**：`DeckEntries()`（" + entries.Count + " 行）里 `Id` **两两不同**"
                            + "（↔ 判据 = `DeckListDrawer__Initialize.c` 遍历卡表时先 `Any(行.CurrentCard == 这张)`，"
                            + "命中就 `SetCount(行, 份数+1)`、**不建新行**）"
                            + "｜🧨 摘掉 `if (… || !seen.Add(id)) continue;` 里的 `!seen.Add(id)` ⇒ 同名卡各占一行 ⇒ 红"
                            + (dupId == null ? "" : "｜实得重复：" + dupId));
                }

                // ---- ④ A1525①：**一屏内**同名只出现一次 + 那一行的份数 = `CountOf` ----
                //  ⛔ 读的是**画面上那行字**（`row_n{i}` 的 `Label.Text`）—— 与 ③ 的状态腿是**两条腿**。
                {
                    var firstRowOfName = new Dictionary<string, int>();
                    string dupName = null; int dupA = -1, dupB = -1;
                    int cntRow = -1, cntWant = 0; string cntGot = "<没量到>";
                    for (int i = 0; i < 11; i++)
                    {
                        var d = _rt.UiDeckRowAt(i);
                        if (d == null) continue;                     // 行不可见（页签不对 / 滚出屏幕）⇒ 不算
                        var t = FindDeep(_rt.Root, "row_n" + i);
                        var lb = t != null ? t.GetComponent<Label>() : null;
                        string nm = lb != null ? lb.Text : null;
                        if (string.IsNullOrEmpty(nm)) continue;      // 没写过字的行（空槽）不算
                        int at;
                        if (firstRowOfName.TryGetValue(nm, out at)) { dupName = nm; dupA = at; dupB = i; }
                        else firstRowOfName[nm] = i;
                        if (cntRow < 0 && d.Type != "hero" && d.Type != "defence" && state.Deck.CountOf(d.Id) > 1)
                        {
                            cntRow = i; cntWant = state.Deck.CountOf(d.Id);
                            var xgo = FindDeep(_rt.Root, "row_x" + i);
                            var xlb = xgo != null ? xgo.GetComponent<Label>() : null;
                            cntGot = (xlb != null && xlb.gameObject.activeSelf) ? xlb.Text : "<没显示>";
                        }
                    }
                    CheckTrue(firstRowOfName.Count >= 5, "（前提）一屏量到 ≥5 个行名（实测 " + firstRowOfName.Count + "）");
                    CheckTrue(dupName == null,
                              "★ **一屏内同名只出现一次**（判据同 ③；这里量的是**画面**，不是状态）"
                            + "｜🧨 改回「逐条 `CardIds` 展开」⇒ 同一张卡占两行 ⇒ 红"
                            + (dupName == null ? "" : "｜实得「" + dupName + "」出现在第 " + dupA + " / " + dupB + " 行"));
                    CheckTrue(cntRow >= 0 && cntGot == "x" + cntWant,
                              "★ 份数 > 1 的那一行（第 " + cntRow + " 行）行右缘那颗 = **`x` + 份数**"
                            + "（该是 `x" + cntWant + "`，实得 `" + cntGot + "`）"
                            + "｜⚠️ 挑的是**份数 > 1** 的卡 —— 一屏里恒 `x1` 的话，「每行都藏」也蒙得过去"
                            + "｜🧨 去重那一刀被摘掉 ⇒ 份数印两遍 / 错位 ⇒ 红");
                }

                // ---- ⑤ 普通卡那一段**费用单调不减**（原版第一把键）----
                //  ⚠️ 排序口径 = **照原版**（用户 2026-10-18 拍板）：费用 → 本地化名的当前文化比较 → Id。
                int seg = 2;   // 前两行 = 督军 / 防御卡（与费用无关、钉在最前）⇒ 不进这一段
                {
                    string bad = null;
                    for (int i = seg + 1; i < entries.Count; i++)
                    {
                        if (entries[i] == null || entries[i - 1] == null) continue;
                        if (entries[i].Cost < entries[i - 1].Cost)
                        {
                            bad = "第 " + (i - 1) + " 行「" + CardText.Name(entries[i - 1]) + "」费用 " + entries[i - 1].Cost
                                + " > 第 " + i + " 行「" + CardText.Name(entries[i]) + "」费用 " + entries[i].Cost;
                            break;
                        }
                    }
                    CheckTrue(entries.Count > seg + 1, "（前提）排序段（第 2 行起）至少 2 行（实测 " + (entries.Count - seg) + "）");
                    CheckTrue(bad == null,
                              "★ 普通卡那一段**费用单调不减**（" + (entries.Count - seg) + " 行）—— 原版第一把键 = "
                            + "`RawCardScript.manaCost`（`DeckListDrawer.__c___Reorder_b__22_0.c`）"
                            + "｜🧨 摘掉 `cards.Sort(…)` ⇒ 顺序退回 `CardIds` 的插入序 ⇒ 红"
                            + (bad == null ? "" : "｜实得：" + bad));
                }

                // ---- ⑥ 同费用段：按**显示名的当前文化比较**升序 + 「文化序 vs 码点序」判别式 ----
                //  ⛔ 期望值不是从 `DeckRuntime` 的常量/函数读回来的：这里**逐段重算**（参考实现），
                //    用的是**规格里的那把键** —— `StringComparison.CurrentCulture`（原版 `ThenBy` 没传 comparer）
                //    + `Id` 序数兜底。数据 = `cards_engine.json`（`PlayerDeckForDemo` 那 30 张 SaimHann 单位）。
                //  ⚠️ **2026-10-19（`A1929`）**：历史注释里那「参考图三组」（= 参考图那副「铺场流」，2/3/4 张）
                //    与本节的演示牌（1/2/3 张）**不是同一副** ⇒ 本段不拿它们当期望值（来路与判据见文件头那条勘误）。
                {
                    // ① 参考实现：每个费用段各自按「名字文化比较 → Id」重排，逐张对 id。
                    string bad = null; int runs = 0;
                    for (int i = seg; i < entries.Count; )
                    {
                        if (entries[i] == null) { i++; continue; }
                        int j = i + 1;
                        while (j < entries.Count && entries[j] != null && entries[j].Cost == entries[i].Cost) j++;
                        runs++;
                        var got = new List<CardDef>();
                        for (int k = i; k < j; k++) got.Add(entries[k]);
                        var want = new List<CardDef>(got);
                        want.Sort((a, b) =>
                        {
                            int c = NameCultureCmp(CardText.Name(a), CardText.Name(b));
                            return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
                        });
                        for (int k = 0; k < got.Count; k++)
                            if (got[k].Id != want[k].Id)
                            {
                                bad = "第 " + (i + k) + " 行「" + CardText.Name(got[k]) + "」(" + got[k].Id + ")"
                                    + " 应为「" + CardText.Name(want[k]) + "」(" + want[k].Id + ")";
                                break;
                            }
                        if (bad != null) break;
                        i = j;
                    }
                    CheckTrue(runs >= 2, "（前提）排序段里至少 2 个费用档（实测 " + runs + "）");
                    CheckTrue(bad == null,
                              "★ 每个费用段内 = **显示名的【当前文化】比较升序**（" + runs + " 段）—— 原版 `ThenBy` "
                            + "没传 `IComparer` ⇒ `Comparer<string>.Default` ⇒ `string.CompareTo`"
                            + "｜🧨 换回 `CompareOrdinal`（码点序）⇒ 本段红、下一条判别式也红"
                            + (bad == null ? "" : "｜实得：" + bad));

                    // 🔴 **2026-10-18（第五十二会话）改法**：原来靠「这副牌同费用段里恰好有一对方向相反的名字」，
                    //    官方中文名落地后该前提不成立（假红）。改成**不依赖卡名**的固定探针 ——
                    //    它量的仍是**生产那一份**比较器（`DeckRuntime.CompareDisplayName`，
                    //    `NameCultureCmp` 已改成直接转调它），不是同义反复。
                    {
                        const string pa = "a", pb = "A";
                        int o = Math.Sign(string.CompareOrdinal(pa, pb));
                        int c2 = Math.Sign(DeckRuntime.CompareDisplayName(pa, pb));
                        CheckTrue(o != 0 && c2 != 0 && o != c2,
                                  "★ **文化序 vs 码点序的判别式**：`\"" + pa + "\"` vs `\"" + pb + "\"` —— "
                                + "文化序 " + c2 + " · 码点序 " + o + "（本机文化 = `"
                                + System.Globalization.CultureInfo.CurrentCulture.Name + "`）"
                                + "｜🧨 生产比较器换回 `CompareOrdinal` ⇒ 两把键同号 ⇒ 本条红（下一条也会红）");
                        CheckTrue(c2 < 0,
                                  "★ ……且生产比较器 `DeckRuntime.CompareDisplayName(\"a\",\"A\")` 用的是**文化序**"
                                + "（实得 " + c2 + "；码点序是 " + o + "）");
                    }
                }

                // ---- ⑦ A1525③：行名**左对齐**（左沿 = 68 · 与名字长短无关）----
                {
                    float minL = 1e9f, maxL = -1e9f, minW = 1e9f, maxW = -1e9f; int n = 0;
                    for (int i = 0; i < 11; i++)
                    {
                        if (_rt.UiDeckRowAt(i) == null) continue;
                        float l, r;
                        if (!RowNameInkPx(i, out l, out r)) continue;
                        n++;
                        if (l < minL) minL = l;
                        if (l > maxL) maxL = l;
                        if (r - l < minW) minW = r - l;
                        if (r - l > maxW) maxW = r - l;
                    }
                    CheckTrue(n >= 8, "（前提）一屏量到 ≥8 行行名的墨迹（实测 " + n + " 行）");
                    CheckNear(maxL, 68f, 1f,
                              "★ **每一行的墨迹左缘都落在 68**（实测最右的左缘 " + maxL.ToString("F1") + " px）"
                            + " —— 期望值 = **参考图**量出来的（px 56…61 × 1.21442 = 画布 68.0…74.1），"
                            + "⛔ **不是** `DeckRuntime.RowNameX`（那是被测实现里的数）"
                            + "｜🧨 只调 `SetAlignLeft()`、不调 `AlignLeftOn` ⇒ 整块还停在框心（第 0 行 ≈ 163 − 宽/2）⇒ 红"
                            + "｜🧨 `RowNameX` 改回 62 ⇒ 左缘恒 62 ⇒ 红");
                    CheckTrue(maxL - minL < 1f,
                              "★ 各行墨迹**左缘相等**（最左 " + minL.ToString("F1") + " · 最右 " + maxL.ToString("F1")
                            + "，差 " + (maxL - minL).ToString("F2") + " px）—— 这就是「左对齐」的定义（与名字长短无关）"
                            + "｜🧨 退回居中 ⇒ 名字长的行左缘小、短的左缘大（差几十 px）⇒ 红");
                    CheckTrue(maxW - minW > 20f,
                              "★ ……而各行**宽度不等**（最窄 " + minW.ToString("F1") + " · 最宽 " + maxW.ToString("F1")
                            + " px）—— 长短名并存，上一条才不是「所有行一样宽」蒙出来的");
                    CheckTrue(minW < 170f,
                              "★ 至少有一行的墨迹**明显窄于框 190**（最窄 " + minW.ToString("F1") + " px）—— 居中的话这一行的左缘"
                            + "会比 68 大 (190−宽度)/2 > 10 px ⇒ 上两条才有鉴别力（⛔ 本条若红 = 夹具变了，先看这一条）");
                }

                // ---- ⑧ 督军 / 防御卡**钉在最前**、与排序键无关 ----
                //  判据（三条）：① 参考图第 0 行 = 督军行（无费用圆、无 `x2`、字更大）② prefab `Content` 子件序
                //  Hero → Defensive → 一串 Card Info（`菜单全树.md:9320-9343`）③ `DeckListDrawer__Reorder.c` 排完序
                //  **又显式钉回去**：`SetSiblingIndex(hero,0)` / `(…,1)` / `(defensive, hero?1:0)`。
                //  🔴 **为什么这一格要造局面**：拿今天这副演示牌**直断前两行是什么，鉴别力是 0** ——
                //    开局督军 `ASH1`（费用 2）/ 防御卡 `ASH55`（费用 1）本来就排得很前
                //    ⇒ 「把这两行也丢进 `cards.Sort`」这个**错法**也能让前两行是它们，**绿**。
                //    所以白盒换成两张**贵**的卡：督军位放 `ASH83`（费用 6）、防御位放 `DA61`（费用 3）——
                //    钉住的话前两行仍是他俩、第 2 行起费用**又从小开始**（1 < 3 < 6）；
                //    丢进排序的话他们会被排到费用 3 / 6 各自的段里 ⇒ 前两行立刻变。
                //  ⚠️ `ASH83` **本来也在演示牌的 `CardIds` 里**（`PlayerDeckForDemo` 的第一张）⇒ 这一格顺带验去重：
                //     `DeckEntries` 必须先把 `WarlordId` / `DefensiveId` 记进 `seen`，否则同一张卡会占两行。
                //  ⚠️ 刷新**只走 `UiScrollDeck`**（它只调 `RefreshDeckList`），⛔ 不走 `RefreshAll`。
                {
                    string wlProbe = state.Deck.WarlordId, dfProbe = state.Deck.DefensiveId;
                    state.Deck.WarlordId = "ASH83";      // 费用 6（它同时也在 CardIds 里）
                    state.Deck.DefensiveId = "DA61";     // 费用 3
                    _rt.UiScrollDeck(-9999f);
                    var pr = _rt.DeckEntries();
                    string p0 = (pr.Count > 0 && pr[0] != null) ? pr[0].Id : "<空>";
                    string p1 = (pr.Count > 1 && pr[1] != null) ? pr[1].Id : "<空>";
                    Check(p0, "ASH83",
                          "★ 探针：**督军位那张（费用 6）钉在第 0 行**"
                        + "（`DeckEntries` 前两行按 `WarlordId` / `DefensiveId` 取卡、不进排序）"
                        + "｜🧨 把它也丢进 `cards.Sort` ⇒ 它被排到费用 6 那一段去 ⇒ 红");
                    Check(p1, "DA61",
                          "★ 探针：**防御位那张（费用 3）钉在第 1 行**｜🧨 同上 ⇒ 红");
                    bool tail = pr.Count > 2 && pr[2] != null;
                    int c2 = tail ? pr[2].Cost : -1;
                    CheckTrue(tail && pr[2].Cost < 3,
                              "★ ……而且第 2 行起**费用又从小开始**（第 2 行费用 " + c2 + " < 3 < 6）"
                            + "—— 这一段**反着**的费用序只可能来自「前两行被钉住」"
                            + "｜🧨 排序把三行一起吃掉 ⇒ 整列单调 ⇒ 红");
                    // 去重腿：ASH83 同时占着「督军位」和「CardIds 里的一张」⇒ 全列只许一行。
                    int n83 = 0;
                    foreach (var e in pr) if (e != null && e.Id == "ASH83") n83++;
                    CheckTrue(n83 == 1,
                              "★ ……`ASH83`（督军位 + `CardIds` 各有一条）在列表里**只出现 1 次**（实测 " + n83 + " 次）"
                            + "—— 判据 = 一卡一行（③ 那条）；去重 `seen` 必须**先把 `WarlordId` / `DefensiveId` 记进去**"
                            + "｜🧨 只去重 `CardIds` ⇒ 第 0 行与排序段各占一行 ⇒ 红");
                    // 还原（后面的节都按开局那副牌在跑）
                    state.Deck.WarlordId = wlProbe; state.Deck.DefensiveId = dfProbe;
                    _rt.UiScrollDeck(-9999f);
                    var bk = _rt.DeckEntries();
                    CheckTrue(bk.Count > 1 && bk[0] != null && bk[1] != null && bk[0].Id == wlProbe && bk[1].Id == dfProbe,
                              "（收尾）督军 / 防御卡**放回原样**（" + wlProbe + " / " + dfProbe + "）—— 后面的节按开局那副牌在跑");
                    CheckTrue(bk.Count > 1 && bk[0] != null && bk[1] != null && bk[0].Type == "hero" && bk[1].Type == "defence",
                              "★ 放回后**前两行 = 督军 → 防御卡**（参考图第 0 行是督军行 · prefab `Content` 子件序 Hero→Defensive→…）");
                }
            }

            // 🔴🔴 **2026-10-04（A41 ④）：「藏住了」还不够 —— 藏住的东西【不许吃点击】**。
            //   真缺陷（原来 `_btns` 里的矩形不跟着显隐走）：那两片空白（`info_share` / `info_import`，
            //     · (60..131, 636..707) = `info_share` ⇒ **一次点击静默把卡组串写进剪贴板**；
            //     · (200..271, 636..707) = `info_import` ⇒ **静默打开导入弹窗**）
            //   同一族还有第二处（**历史**）：导入弹窗**关着**时，它的输入框矩形 (610..1310, 370..511)
            //   正好落在**卡池**里 ⇒ 点在没卡的空白上会进入一个**看不见的文本编辑态**（`_editKind=3`）。
            //   🔴 2026-10-17（T23 删件）：导入弹窗整棵删了 ⇒ 这一处与 `_editKind=3` 都已不存在；
            //   上面那段留着是因为它是 `KeyLive` 这道闸的判据来源。
            //   判据 = 原版那套「`SetActive(false)` / `m_TargetGraphic.enabled=0` 之后就不再参与射线」
            //   （UGUI `Graphic.Raycast` 只对**激活且在画**的图形生效）⇒ 我们收口成 `DeckRuntime.KeyLive`。
            // 🔴 **2026-10-04 订正（R-W4 F1/F2）** —— 两句措辞原来都太宽：
            //   ① **「必现页」是 `Cosmetics`，不是 `Cards`**：真实指针链是
            //      `HandlePoolClick → HandleDeckRowClick → HandleCosmeticClick → HandleButtons`
            //      （上面那条链就在 `DeckRuntime.cs` 里，`HandleButtons` 是**最后一站** —— ⛔ 按【符号】认，别抄行号），而那两片矩形
            //      **整片落在卡组列表里**（x 0.4..325.4 · y 366..1010.1）⇒
            //        · **Cosmetics 页（必现）**：`_tab==2` ⇒ `HandleDeckRowClick`（`CardPresentation/Deck/DeckRuntime.cs` 要求 `_tab==0`）
            //          早退、`HandleCosmeticClick`（`CardPresentation/Deck/DeckRuntime.cs` 要求 `_tab==2`）只管 x ≥ `CosmoX` 那一片
            //          ⇒ **谁也拦不住**，直接落到 `HandleButtons`；
            //        · **Cards 页**：只要那一格真有行，`HandleDeckRowClick` 就先把它吃掉（开始行拖拽）
            //          ⇒ **点不到**；只有**那一格是空槽**时才漏得过去（`RefreshDeckList` 的
            //          `on = cards && (firstRow + i) < shown.Count`，`CardPresentation/Deck/DeckRuntime.cs`）。
            //   ② **下面这几条走的是 `KeyLive` 那条命中路**（`UiTopKeyAt` → `HitBtn`/`KeyLive`；
            //      `UiClickPx` 直调 `HandleButtons`），**不是**真实鼠标那条完整分派链
            //      ⇒ 只能说「**不被任何按钮**吃到」，不能说「谁都没吃到」（行处理器不在这一层）。
            {
                // 🔴 **2026-10-17（D35 删件）就地改写**：那两颗圆钮（`info_share` / `info_import`）
                //   **已经整颗删掉** ⇒ 本节原来靠它们的那四条（负例 + 正向控制）**没有对象了**。
                //   按「删件要配反向断言」那条口径换成下面这几条。
                //   判据 → `对账_卡组编辑部分_差异与待办.md` D35。
                // 🔴 **2026-10-17（T23 删件）**：本节原来还有一组「`imp_input` 那片在卡池里、弹窗关着时
                //   命中不到 / 不会点进看不见的输入态」（含一条正向控制）—— 导入弹窗整棵删了，
                //   **那一组随之删**（连同 `UiBtnRect("imp_input")` 那些读数）。
                CheckTrue(!_rt.UiBtnRegistered("info_share") && !_rt.UiBtnRegistered("info_import"),
                          "★ D35：两颗动作钮的点击矩形都已从 `_btns` 撤掉（不是「藏起来」—— **根上没有了**）");
                // 原那两片空白（60..131 / 200..271 × 636..707）现在**没有按钮**可命中 —— 走 `KeyLive`
                // 那条命中路各验一次（防止将来有人把矩形加回来、却忘了解除它「看得见才有」的前置）。
                Check(_rt.UiTopKeyAt(95.5f, 671.5f), null, "（反向）原 `info_share` 那片空白攒不到任何按钮 key");
                Check(_rt.UiTopKeyAt(235.5f, 671.5f), null, "（反向）原 `info_import` 那片空白攒不到任何按钮 key");
                CheckTrue(_rt.ActiveTab == 0, "（前提）现在在 Cards 页签");
            }
            Shoot("deck_cards.png");

            // ============================================================ 🆕 2026-10-04（A24）悬停换图 + 状态换图
            // 这一整段以前**一条断言都没有** ⇒ 屏幕上「悬停什么都不发生」也全绿（普查 A24 的结论）。
            // 判据逐条 = 原 prefab 的字段；出处写在 `CheckHoverOne` 上面那一段。
            // 🔴 **2026-10-20**：原版 `m_Transition=2` 的按钮**一共 5 颗**，本段现在只跑 **2 颗** ——
            //   `hdr_clear` 那一颗退出了本段，改由紧邻下面那段断它的**隐藏态**（原因写在那里）；
            //   🔴 **2026-10-17（T23 删件）**：另外两颗（`imp_ok` / `imp_close`）在导入弹窗那棵树里，
            //   那棵树整棵删了 ⇒ 本段只剩 `hdr_back` / `foot_done`。
            Section("悬停换图（原版 `m_Transition=2` 的按钮；本节 2 颗，`hdr_clear` 见下）");
            {
                CheckHoverOne("hdr_back", "hdr_back", "UI_Button_Mulligan", "UI_Button_Mulligan_hover",
                              "UI_Button_Mulligan_Pressed", "页头 `Close`（文本 'Back'）");
                // 🔴🔴 **2026-10-20 改向：`hdr_clear` 不在这里走 `CheckHoverOne`** —— 它此刻**是隐藏的**。
                //   本节跑到这里时，上面 `_rt.UiClearFilters()` 已经把筛选清空 ⇒ 按新判据这颗钮**隐藏**
                //   （判据 = `DeckRuntime.KeyLive` 的 `case "hdr_clear": return !State.Filter.IsEmpty;`
                //    与 `RefreshHeader` 里那两句 `SetOn(…, clearFiltersOn)`，两处**同一个谓词**）。
                //   而 `CheckHoverOne` 第 ③ 步走的是**鼠标那条路**（`UiHoverAt` → `HitBtn` → `KeyLive`）
                //   ⇒ 隐藏的钮打不到 ⇒ 它内部那两条（打不到 / 悬停图没换）**必红**。
                //   ⛔ **不许**改回「hover 之前先设个筛选、量完再清」那种迁移就写法 —— 那是把判据绕过去，
                //   「隐藏时是什么行为」一条都没验。**改断新的那条判据**（隐藏 + 吃不到输入）。
                //
                //   ⚠️ **隐藏态本身的判据强度（如实标注）**：原版那颗钮**不是 `SetActive` 隐藏，是平移到
                //   `hiddenPosition = (-550, 0)`**（`CollectionFilterController<T>.Toggle`，`animationTime 0.3`；
                //   逐实例实据见本文件上面 A67 那一节）。而**触发条件那个方法体本地缺失** ⇒
                //   「**筛选为空时隐藏**」这六个字是**推断**、不是逐句读出来的原文 —— 记在这里，别当已证。
                {
                    CheckTrue(state.Filter.IsEmpty, "（前提）此刻**一个筛选都没设**（上面 `UiClearFilters()` 清过）");
                    CheckTrue(_rt.UiHasQuad("hdr_clear"),
                              "（前提）`hdr_clear` **建出来了** —— 不然下面那条「不显示」是拿「件不存在」蒙的"
                            + "（`UiQuadActive` 对不存在的 key 恒答 false，账 `A1532`）");
                    CheckTrue(!_rt.UiQuadActive("hdr_clear"),
                              "★ 筛选为空 ⇒ **`Clear filters` 那颗钮不显示**"
                            + "（原版是平移到 `hiddenPosition`；「筛选为空时隐藏」是**推断**，见本段说明）");
                    // 命中那半边：拿**按钮自己登记的矩形**去问「这一点攒得到谁」
                    //   （`UiTopKeyAt` → `TopKeyAt` → `HitBtn` → `KeyLive`，与点击/悬停**同一条**路）。
                    CheckTrue(_rt.UiBtnRect("hdr_clear", out float chX, out float chY, out float chW, out float chH),
                              "（前提）`hdr_clear` 的**点击矩形还在 `_btns` 里**（撤掉矩形也能让它打不到 —— "
                            + "那是一种**不同的**绿，本条要验的是「矩形在、显隐闸拦住了」）");
                    Check(_rt.UiTopKeyAt(chX + chW * 0.5f, chY + chH * 0.5f), null,
                          "★ …而且**它那块点位吃不到输入**（藏起来了却还能点到 = 静默失败，本仓红线）"
                        + "｜🧨 把 `KeyLive` 里 `case \"hdr_clear\"` 那一支删掉（退回 `default: return true`）⇒ 本条红");
                    // 🧨 **反向控制**：同一条读数换到一颗**看得见**的钮上，必须**答得出 key** ——
                    //   否则上一条可能只是因为 `UiTopKeyAt` 恒答 null（那是另一种假绿）。
                    const float BackCx = 192.2f + 150f * 0.5f;      // `hdr_back` 正中心 = (267.2, 113.5)
                    const float BackCy = 83.5f + 60f * 0.5f;
                    Check(_rt.UiTopKeyAt(BackCx, BackCy), "hdr_back",
                          "（正向控制）同一条读数打在**看得见的** `hdr_back` 正中心 ⇒ 命中到它"
                        + "（⛔ 少了这条，「隐藏的钮打不到」可能只是「这条读数恒答 null」）");
                }
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
                // 🔴 **2026-10-17（T23 删件）**：这里原来还有导入弹窗那两颗的悬停换图
                //   （`imp_ok` = `40K_button` 那一组）与关闭图标 `imp_close_x` 的矩形四连
                //   （56.37×54.50 @ 1326.6/212.4，A37 ⑤）+ 两条模态遮挡断言 —— 那棵树整棵删了。
                //   ⚠️ 那颗 `Confirm` 的**词条**判据（`MainMenu/General/Confirm`）在外壳那份
                //   （`Shell/ImportDeckPopup.cs`，`CollectionScene` 有断言），本文件不再重复。
                // 兜底：整棵树里**每一颗**接了换图的都要「换得动 + 还原得回」，且图片一张不缺
                CheckHoverSwap(_root, "卡组编辑窗");
                CheckNoMissingSwapArt("卡组编辑窗");

                // 🔴 **2026-10-04（A37 ④）**：点击与悬停**共用同一张命中顺序表**（`DeckRuntime.ClickOrder`）。
                //   原来两套口径（点击走 `HandleButtons` 的 if 链 · 悬停走 `_btns` 的**登记顺序**），
                //   而 `HoverTargetUnder` 的注释却写着「命中口径 = 和点击同一条」—— **声明是错的**。
                //   这条断言盯的就是「两套再分叉」：漏写进表里的那颗会「亮得起来但点不到」。
                var order = DeckRuntime.UiClickOrder();
                var hkeys = _rt.UiHoverKeys();
                CheckTrue(hkeys.Count >= 3,
                          $"（前提）确实有接了悬停换图的按钮（实得 {hkeys.Count} 颗；少于 3 ⇒ 那 3 颗没接全）");
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
                // ⚠️ 本窗**没有** `PointerLayer` ⇒ 派发走 `HoverTargetUnder`（`ClickOrder` 那 9 个 key +
                //   `DrawerHoverUnder` 的抽屉那几格），`UiHoverAt` 走的正是这一条。
                //   （🆕 2026-10-17 F2 就地订正：这里原写「那 **14** 个 key」—— 当时实测 13 项；
                //    🔴 2026-10-17（T23 删件）：`imp_input/imp_ok/imp_close/imp_shade` 四项删掉之后 = **9** 项。）

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
                //   同工程早就做对过一次：`Shell/MenuWindowBase.cs`（同图 + `(1,0,0,1)`）。
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

                // ---- ★ 实绘尺寸：卡背格**画出来多大**（🆕 2026-10-18 · 补 `A994③` 的缺口）----
                // 🔴 **为什么单开这一组**：上面那六条只钉了**框**（250×405）与视口常量（1589.78…），
                //    **一条也量不到「真画出来多大」** ⇒ 上一批把 `SetAspect(250/405)`（= **拉伸**）
                //    换成「**按贴图自己的比例内接**」（原版那一格 `Cardback Container > Cardback` 的
                //    `Image` 带 `m_PreserveAspect = 1`）之后，**卡背格这一处在断言上无牙** ——
                //    改回拉伸照样全绿。这一组堵这个口。
                // 🔴 **2026-10-19（B6）就地订正（铁律 5）**：期望值原来是「**只做第一段**」那一版
                //    （= 按**贴图自己**的比例内接）—— 原版是**两段**：① 按 sprite 的 **`m_Rect`（707×1020）
                //    定框**；② 贴图再按 **`textureRect/m_Rect`** 缩、并按 **`padding`** 挪位。
                //    ⇒ 对已裁成 `textureRect` 的卡背：**宽也会缩**（⛔ 不是「宽恒 250」）+ **画心不居中**。
                //    口径/判据全文 → `Core/CardbackFace.cs`（本仓**唯一**一份）。
                // 🔴 **期望值怎么现算**（⛔ **不调** `CardbackFace.Fit` / `PreserveAspectSize`）：**直接调它就是自证**
                //    —— 它把被测的那条算式拿去当期望值，它自己算错时期望值跟着一起错 ⇒ 照样绿。
                //    这里只用三样**独立真值**：① **贴图自己的 `width/height`**（`Resources/Art/cardbacks/`
                //    那 233 张 PNG 的像素尺寸）② 框常量 250×405；③ 原版那张 sprite 的 **`m_Rect` = `707×1020`**
                //    （233/233 恒定，出处 → `Core/CardbackFace.cs` 文件头）—— 算式逐句照 uGUI 原文复述。
                {
                    _rt.UiScrollCosmetics(-1e6f);      // 回顶（本节下面那几条点击也是按「滚到顶」算坐标的）
                    Check(_rt.CosmoScrollPx, 0f, "（前提）卡背网格在顶 —— 探针格与后面的点击都按这一屏算");
                    // 选靶：**不写死格号**。🆕 **2026-10-19（B6）**：旧判据是「内接离**拉满**多远」——
                    //   两段式之后**宽也会缩**，旧判据挑出来的那一格未必是差得最远的 ⇒ 换成「**旧口径与
                    //   新口径逐维差的最大值**」（差 ≤3px 时下面那两条**空转**、看着绿其实什么都没查）。
                    //   同时**跳过被视口切过的格** —— 裁切之后量到的不是「它该画多大」。
                    int cbProbe = -1, cbOffProbe = -1;
                    float cbS = 0f, cbExpW = 0f, cbExpH = 0f, cbDev = 0f;
                    float cbOffX = 0f, cbOffY = 0f, cbOffSum = 0f;
                    for (int cbI = 0; cbI < names.Length; cbI++)
                    {
                        float cbPcx, cbPcy;
                        if (!_rt.UiCosmeticCellCenter(cbI, out cbPcx, out cbPcy)) continue;   // 不在这一屏里
                        var cbTx = _rt.UiCosmeticTex(names[cbI]);   // 与建格**同一条**会出声的取值路
                        if (cbTx == null || cbTx.height <= 0) continue;
                        float cbRatio = cbTx.width / (float)cbTx.height;                      // 贴图自己的比例
                        // ⛔ 下面这两个期望值**都不许**调被测实现（`CardbackFace.Fit` / `PreserveAspectSize` = 自证）
                        var cbNeu = CardbackDrawnPx(cbTx, 250f, 405f);                        // 两段式（现在的口径）
                        var cbOld = cbRatio > 250f / 405f                                     // 「只做第一段」那一版
                                  ? new Vector2(250f, 250f / cbRatio) : new Vector2(405f * cbRatio, 405f);
                        // 视口 = `CosmoView`（上六条刚核过：155.97 / 924.06）—— **画出来**的矩形露出去了就跳过
                        if (cbPcy - cbNeu.y * 0.5f < 155.97f || cbPcy + cbNeu.y * 0.5f > 155.97f + 924.06f) continue;
                        float cbD = Mathf.Max(Mathf.Abs(cbNeu.x - cbOld.x), Mathf.Abs(cbNeu.y - cbOld.y));
                        if (cbD > cbDev) { cbDev = cbD; cbProbe = cbI; cbS = cbRatio; cbExpW = cbNeu.x; cbExpH = cbNeu.y; }
                        // 🆕 位置那一半的靶：**两轴都要够大** —— 任一轴 ≈0 时那条断言与「恒居中」那种
                        //   实现**分不开**（弱断言）。⚠️ 本仓一屏（= 完全落在视口里的行 0~1 那 12 格）
                        //   满足「两轴都 > 1.5px」的**只有 2 格** ⇒ 挑不到就是数据变了，那要红出来。
                        float cbdX, cbdY;
                        if (CardbackOffsetPx(cbTx, 250f, 405f, out cbdX, out cbdY)
                            && Mathf.Abs(cbdX) > 1.5f && Mathf.Abs(cbdY) > 1.5f)
                        {
                            float cbSum = Mathf.Abs(cbdX) + Mathf.Abs(cbdY);
                            if (cbSum > cbOffSum) { cbOffSum = cbSum; cbOffProbe = cbI; cbOffX = cbdX; cbOffY = cbdY; }
                        }
                    }
                    CheckTrue(cbProbe >= 0 && cbDev > 3f,
                              $"（前提）挑得到一格「两段式 ≠ 只做第一段」的探针 —— 第 {cbProbe + 1} 格 · 贴图比例 "
                            + $"{cbS:0.####} · 两种口径差 {cbDev:0.##}px"
                            + "（差 ≤ 3px 说明下面两条**空转** ⇒ 这一条先红，⛔ 别放它过去）");
                    if (cbProbe >= 0
                        && _rt.UiQuadRect("cosm_cell" + cbProbe, out float cbMx, out float cbMy,
                                          out float cbMw, out float cbMh))
                    {
                        // 实测 233 张的 `m_Rect` 比例一律 707/1020 = 0.69314 > 框比例 250/405 = 0.61728
                        //   ⇒ **一律宽定**：框宽 250、框高 = 250 ÷ 0.69314 = **360.68**；
                        //   画心 = 框 × (`textureRect`/`m_Rect`) ⇒ 宽 = 250 × 贴图宽/707（⛔ 不是恒 250）。
                        CheckNear(cbMw, cbExpW, 0.5f,
                                  $"★ 卡背格**画出来**的宽 = **{cbExpW:0.##}**（第 {cbProbe + 1} 格 · 框 250×405 · "
                                + $"贴图比例 {cbS:0.####} ⇒ 两段式：**框宽 250 × `textureRect`/`m_Rect` 的宽那一半**；"
                                + $"贴图宽/707 = {cbExpW / 250f:0.####}。⛔ 「宽恒 250」是只做第一段那一版）"
                                + "｜🧨 改坏法：把 `Core/CardbackFace.cs` 的 `Fit` 里 `w = fw*sw` 改成 `w = fw`"
                                + "（= 只定框、不按 `textureRect` 缩）⇒ **这一条**红（高那条不红）");
                        CheckNear(cbMh, cbExpH, 0.5f,
                                  $"★ ……高 = **{cbExpH:0.##}** = (250×1020/707) × `textureRect`高/1020"
                                + $"（**拉满那一档恒为 405**、只做第一段那一档是 250 ÷ 贴图比例，与它差 "
                                + $"{cbDev:0.##}px）"
                                + "｜🧨 改坏法：把同一条 `Fit` 里 `h = fh*sh` 改成 `h = fh` ⇒ **这一条**红"
                                + "（宽那条不红；这一条就是那处改动的**唯一**看门人）");
                        // 🔴 **灭自证**：这一条与上一条**结构上不可能同时被「拉伸」那种实现满足** ——
                        //   拉伸时宽**确实**是 250（宽那条照样绿），但高恒等于 405 ⇒ 本条红。
                        CheckTrue(cbMh < 405f - 1f,
                                  $"★ 灭自证：实绘高 {cbMh:0.##} **严格小于**框高 405（拉伸那种实现恒等于 405 ⇒ 红）");
                    }
                    else CheckTrue(false, $"（前提）第 {cbProbe + 1} 格量得到渲染矩形（`cosm_cell{cbProbe}`）");

                    // ---- 🆕 2026-10-19（B6）★ 画心**不居中**（原版按 sprite 的 `padding` 偏）----
                    //  这一组是**新行为**：B2 之前「内接 + 居中」是全部，`padding` 那一半**一条断言都没有**。
                    //  期望值 = `((padL−padR)/2, −(padB−padT)/2) × 250/707`（`CardbackOffsetPx` 吃原版数据、独立复述）。
                    CheckTrue(cbOffProbe >= 0,
                              $"（前提）挑得到一格「`padding` 的**两轴**偏移都 > 1.5px」的卡背 —— 第 {cbOffProbe + 1} 格 · "
                            + $"该挪 ({cbOffX:0.##}, {cbOffY:0.##})px（⛔ 两轴都要够大：任一轴 ≈0 时那条断言"
                            + "与「恒居中」那种实现**分不开** = 弱断言）");
                    if (cbOffProbe >= 0)
                    {
                        // ⚠️ 先给初值：下面那两条读数用 `&&` 连起来（短路）⇒ 编译器没法证明第 2 个 `out` 一定跑过
                        float cboCx = 0f, cboCy = 0f, cboMx = 0f, cboMy = 0f, cboW = 0f, cboH = 0f;
                        bool cboOk = _rt.UiCosmeticCellCenter(cbOffProbe, out cboCx, out cboCy)
                                  && _rt.UiQuadRect("cosm_cell" + cbOffProbe, out cboMx, out cboMy, out cboW, out cboH);
                        CheckTrue(cboOk, $"（前提）第 {cbOffProbe + 1} 格量得到格中心与渲染矩形");
                        // 前提不成立 ⇒ 只留上面那条红（⛔ 不级联出两条「量了 0」的假红）
                        if (cboOk)
                        {
                            CheckNear(cboMx - cboCx, cbOffX, 0.5f,
                                      $"★ 卡背格里的画心**不居中**：渲染中心 − **格中心** 的横偏移 = **{cbOffX:0.##}**px"
                                    + "（= (padL−padR)/2 × 250/707 ⇒ 没做这一半时恒为 **0**）"
                                    + "｜🧨 改坏法：把 `Core/CardbackFace.cs` 的 `Fit` 里 `dx = …` 删掉，或把 "
                                    + "`Deck/DeckRuntime.cs` 的 `RefreshCosmetics` 里那句 `Pos(cx + cdox, cy − cdoy)`"
                                    + " 改回 `Pos(cx, cy)` ⇒ **这一条**红（下面那条不红）");
                            CheckNear(cboMy - cboCy, cbOffY, 0.5f,
                                      $"★ ……纵偏移 = **{cbOffY:0.##}**px（= −(padB−padT)/2 × 250/707 —— **反号那一处**："
                                    + "uGUI 的 y 向上、本仓画布 px y 向下 ⇒ 那句 `cy − cdoy` 的减号；"
                                    + "没做这一半时恒为 **0**）"
                                    + "｜🧨 改坏法：把上面那句的减号写成加号 ⇒ **这一条**红（横那条不红）");
                        }
                    }
                }

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

                // ---- ★ 实绘尺寸：侧栏抽屉那张**画出来多宽**（同上，这两处此前**一条都量不到实绘**）----
                //   口径与上面那组**逐条相同**（⛔ **不调** `PreserveAspectSize` —— 那是被测实现本身，
                //   直接调它 = 自证）：真值 = **贴图自己的 `width/height`** · 框 = 侧栏那张
                //   `Sidebar/Deck Details/Cosmetic Drawer` 的 **335.31×400**（`DeckRuntime` 的
                //   `DrawerW`/`DrawerH`）。实测 233 张比例 **0.6188~0.7652，全部 < 335.31/400 = 0.83828**
                //   ⇒ 一律走**高定**那一支（框高 400、框宽 = 400 × 707/1020 = **277.25**）。
                // 🔴 **2026-10-19（B6）就地订正（铁律 5）**：期望值原来是「**只做第一段**」那一版
                //   （按**贴图自己**的比例内接）—— 原版是**两段**（① 按 sprite 的 `m_Rect`（707×1020）
                //   定框；② 贴图再按 **`textureRect/m_Rect`** 缩、并按 **`padding`** 挪位）。
                //   口径/判据全文 → `Core/CardbackFace.cs`；期望值现算走 `CardbackDrawnPx` / `CardbackOffsetPx`。
                {
                    // 选靶：🆕 **2026-10-19（B6）**换成「**旧口径与新口径逐维差的最大值**」
                    //   （旧判据「内接离**拉满**多远」在两段式下指不准：**宽**现在也会缩 ⇒ 挑出来的
                    //   未必是差得最远的那一张）。⛔ 别写死一张 —— 写死 = 赌它的比例，赌输这两条就空转。
                    string drName = null, drOffName = null;
                    float drS = 0f, drExpW = 0f, drExpH = 0f, drDev = 0f;
                    float drOffX = 0f, drOffY = 0f, drOffSum = 0f;
                    foreach (var drTxName in names)
                    {
                        var drTx = _rt.UiCosmeticTex(drTxName);       // 与建格**同一条**会出声的取值路
                        if (drTx == null || drTx.height <= 0) continue;
                        float drRatio = drTx.width / (float)drTx.height;
                        var drNeu = CardbackDrawnPx(drTx, 335.31f, 400f);                     // 两段式（现在的口径）
                        var drOld = drRatio > 335.31f / 400f                                  // 「只做第一段」那一版
                                  ? new Vector2(335.31f, 335.31f / drRatio) : new Vector2(400f * drRatio, 400f);
                        float drD = Mathf.Max(Mathf.Abs(drNeu.x - drOld.x), Mathf.Abs(drNeu.y - drOld.y));
                        if (drD > drDev) { drDev = drD; drName = drTxName; drS = drRatio; drExpW = drNeu.x; drExpH = drNeu.y; }
                        // 🆕 位置那一半的靶：**两轴都要够大**（任一轴 ≈0 ⇒ 那条断言与「恒居中」分不开 = 弱断言）
                        float drdx, drdy;
                        if (CardbackOffsetPx(drTx, 335.31f, 400f, out drdx, out drdy)
                            && Mathf.Abs(drdx) > 1.5f && Mathf.Abs(drdy) > 1.5f)
                        {
                            float drSum = Mathf.Abs(drdx) + Mathf.Abs(drdy);
                            if (drSum > drOffSum) { drOffSum = drSum; drOffName = drTxName; drOffX = drdx; drOffY = drdy; }
                        }
                    }
                    CheckTrue(drName != null && drDev > 3f,
                              $"（前提）挑得到一张「两段式 ≠ 只做第一段」的探针卡背 —— `{drName}` · 比例 {drS:0.####} · "
                            + $"两种口径差 {drDev:0.##}px（差 ≤ 3px ⇒ 下面两条空转）");
                    // ⚠️ `EquipCardback` 对「**已经是这一张**」返回 false（那是它的正常语义，不是缺陷）⇒
                    //    这里先看抽屉现在画的**是不是**它，是就不重复写（⛔ 免得数据一变就假红在这条夹具上）
                    bool drEquipOk = _rt.CosmeticDrawerTex == drName || _rt.EquipCardback(drName);
                    CheckTrue(drEquipOk,
                              "（夹具）抽屉那张现在是探针卡背 —— 走**生产那条路** `EquipCardback`"
                            + "（与右键装备、拖拽投递是同一个口；它本来就是那一张时不必重复写）");
                    Check(_rt.CosmeticDrawerTex, drName,
                          $"……抽屉画的确实是它（`FitDrawerBack` 那一跳就是挂在换图之后的）｜实得 {_rt.CosmeticDrawerTex}");
                    // ⚠️ `drEquipOk` 不成立时上面那条已经红了 ⇒ 这里**不级联**（不然会再红两条「量了旧图」的假红）
                    if (drEquipOk && _rt.UiQuadRect("cosm_drawer", out float drCx, out float drCy,
                                                    out float drW, out float drH))
                    {
                        CheckNear(drW, drExpW, 0.5f,
                              $"★ 抽屉**画出来**的宽 = **{drExpW:0.##}**（框 335.31×400 · 贴图比例 {drS:0.####} "
                            + $"⇒ **高定** ⇒ 框宽 = 400 × 707/1020 = **277.25**；画心宽 = 框宽 × `textureRect`宽/707，"
                            + $"贴图宽/707 = {drExpW / 277.25f:0.####}。⛔ 「宽 = 400 × 贴图比例」是只做第一段那一版）"
                            + "｜🧨 改坏法：把 `Core/CardbackFace.cs` 的 `Fit` 里 `w = fw*sw` 改成 `w = fw`"
                            + "（= 只定框、不按 `textureRect` 缩）⇒ **这一条**红（高那条不红）");
                        CheckNear(drH, drExpH, 0.5f,
                              $"★ ……高 = **{drExpH:0.##}** = 400 × `textureRect`高/1020（高定 ⇒ 框高就是 400，"
                            + "⛔ 但**画心高 ≠ 400**：只有 `textureRect` 高 = 1020 那几张才等于 400）"
                            + "｜🧨 改坏法：把同一条 `Fit` 里 `h = fh*sh` 改成 `h = fh` ⇒ **这一条**红"
                            + "（宽那条不红；这一条就是那处改动的**唯一**看门人）");
                        // 🔴 **灭自证**：与上一条**结构上不可能同时被「拉伸」满足** —— 拉伸时宽恒等于
                        //    335.31，而比例那一维（这里 = 宽）压根没缩 ⇒ 本条红。
                        CheckTrue(drW < 335.31f - 1f,
                              $"★ 灭自证：实绘宽 {drW:0.##} **严格小于**框宽 335.31（拉伸那种实现恒等于 335.31 ⇒ 红）");
                    }
                    else CheckTrue(false, "（前提）侧栏那张量得到渲染矩形（且上面那条夹具过了）");

                    // ---- 🆕 2026-10-19（B6）★ 画心**不居中**（原版按 sprite 的 `padding` 偏）----
                    //  这一组是**新行为**：B2 之前「内接 + 居中」是全部，`padding` 那一半**一条断言都没有**。
                    //  框中心（画布 px）= `DrawerX + DrawerW/2`, `DrawerY + DrawerH/2`
                    //    = (0.25 + 335.31/2, 485.5 + 400/2) = **(167.905, 685.5)** —— 四个常量出自
                    //    `Deck/DeckRuntime.cs`（原版 `Sidebar/Deck Details/Cosmetic Drawer`；
                    //    本文件上面 `CheckNear(d.z, 335.31f, …)` 那条钉的是同一个数）。
                    //  期望值 = 框中心 + `CardbackOffsetPx`（吃原版 `padding` 数据、独立复述算式）。
                    const float DrBoxCx = 167.905f, DrBoxCy = 685.5f;
                    CheckTrue(drOffName != null,
                              $"（前提）挑得到一张「`padding` 的**两轴**偏移都 > 1.5px」的卡背（实得 `{drOffName}` · "
                            + $"({drOffX:0.##}, {drOffY:0.##})px）—— ⛔ 两轴都要够大：任一轴 ≈0 时那条断言"
                            + "与「恒居中」那种实现**分不开** = 弱断言");
                    bool drOffEquipOk = _rt.CosmeticDrawerTex == drOffName
                                     || (!string.IsNullOrEmpty(drOffName) && _rt.EquipCardback(drOffName));
                    CheckTrue(drOffName != null && drOffEquipOk,
                              $"（夹具）把抽屉那张换成**偏移靶** `{drOffName}` —— 同样走生产那条路"
                            + $"（它本来就是那一张时不必重复写）｜实得 {_rt.CosmeticDrawerTex}");
                    bool drOffDrawn = _rt.CosmeticDrawerTex == drOffName;
                    CheckTrue(drOffDrawn, $"……抽屉画的确实是它（实得 {_rt.CosmeticDrawerTex}）");
                    if (drOffDrawn && _rt.UiQuadRect("cosm_drawer", out float droCx, out float droCy,
                                                     out float droW, out float droH))
                    {
                        CheckNear(droCx, DrBoxCx + drOffX, 0.5f,
                                  $"★ 抽屉里的画心**不居中**：渲染中心 x = **{DrBoxCx + drOffX:0.##}**"
                                + $"（= 框中心 {DrBoxCx} + (padL−padR)/2 × 277.25/707 = {drOffX:0.##}；"
                                + "没做这一半时**恒等于框中心 167.9**）"
                                + "｜🧨 改坏法：把 `Core/CardbackFace.cs` 的 `Fit` 里 `dx = …` 删掉，或把 "
                                + "`Deck/DeckRuntime.cs` 的 `FitDrawerBack` 里那句 "
                                + "`Pos(DrawerX + DrawerW*0.5f + dox, …)` 改回不带 `+ dox` ⇒ **这一条**红（纵那条不红）");
                        CheckNear(droCy, DrBoxCy + drOffY, 0.5f,
                                  $"★ ……渲染中心 y = **{DrBoxCy + drOffY:0.##}**（= 框中心 {DrBoxCy} "
                                + $"− (padB−padT)/2 × 277.25/707 = {drOffY:0.##} —— **反号那一处**：uGUI 的 y 向上、"
                                + "本仓画布 px y 向下 ⇒ 那句 `DrawerY + DrawerH*0.5f − doyUp` 的减号；"
                                + "没做这一半时**恒等于框中心 685.5**）"
                                + "｜🧨 改坏法：把上面那句的减号写成加号 ⇒ **这一条**红（横那条不红）");
                    }
                    else CheckTrue(false, "（前提）换上偏移靶之后抽屉那张仍量得到渲染矩形");
                    // ⚠️ 下面「换一格（第 2 行第 3 列 = 第 9 格）」那一拍会**再右键换回 `names[8]`**
                    //   ⇒ 本节末尾 `Done` 后落盘的仍是 `names[8]`（那条断言靠它），本夹具（连同上面
                    //   那两次 `EquipCardback`）不改它的结论。
                }

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
                    // 🔴 **2026-10-22（`W63`）前提断言**：本节靠「盘上有一份基线」做对照 —— 基线由
                    //   `TestLibraryAfterDone()` 那套夹具提供（`RunCore` 里它已排在本节之前）。
                    //   ⛔ 别改成 `stale.Current == null || …`（恒真）或 null 容错跳过。
                    CheckTrue(stale.Current != null, "（前提）盘上有一份基线存档");
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

                // ================================ 🆕 2026-10-17（G2 · D39-D42）
                // **卡背抽屉也是「两扇窗各是各的」**（铁律 5·c）—— 与上面 WB 那批 D36-D38 **同一个形状**，
                //   这次差的是**卡组编辑窗**那一棵（`Deck Editing Menu > Content Area > Cosmetic Display >
                //   Cosmetic FIlter`），它比收藏窗那份（`Collection Menu Variant > Cardback Tab > …`）多三处：
                //     D39 Army 行 `Content` 的 `m_Spacing.y` = **20**（收藏窗 = 0）
                //         ⇒ 行高 = 50 + 5×100 + 4×20 = **630**（收藏窗 550）
                //     D40 由 D39 带出：`Owned` 行顶（面板内）= 15 + **630** + 12.81 = **657.81**
                //         ⇒ 绝对 = `FltY` 156 + 657.81 = **813.81**（我们原来 733.81，差 **80** = 4×20）
                //     D41 `Owned Toggle/Image` = 原版 `sd(**80**,0)` ⇒ 宽 **80**
                //         （收藏窗走锚点式 `0.3w−30`；右缘两棵都是 `w−25`）
                //     D42 `Owned Toggle/Label` = 原版 `sd(**230**,0)` ⇒ 25..**255**
                //         （收藏窗走锚点式 25..`0.7w`；左缘两棵都是 25）
                //   判据（2026-10-17 现读，可复跑）→ `Core/FilterPanelModel` 的 Cosmo 那段那三条：
                //     ① `menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 20 --md`（两棵树各打一次）
                //     ② 全包逐实例枚举：100×100 的 9 颗里 `spacing.y=20` 只有 1 颗（父链 = 卡组编辑那棵）·
                //        `sd(230,0)` 全包只有 1 颗 · `sd(80,0)` 有 3 颗（要连父链一起看）
                //     ③ `Owned` 行顶 / 两处宽度**自洽**（每条算式各自落到 813.81 / 80 / 255）
                // ⛔ 期望值一律是**原版字面量**（630 / 657.81 / 80 / 230 / 255 / 813.81 / 838.81），
                //   **不从 `CosmoArmySpYDeckEdit` / `CosmoIconWDeckEdit` / `CosmoLabWDeckEdit` 读**。
                // 🔴 改坏法：① `Deck/DeckRuntime.cs` 那处 `BuildCosmetics(…)` 后加的三个实参删掉
                //   ⇒ 下面两条**实拍读数**（最后一格 Army 退回 671.0 · `$owned` 行退回 758.81）⇒ 红；
                //   ② 把 `FilterPanelModel` 那三个**缺省**形参改成卡组编辑的值 ⇒ 「收藏窗档」那几条红。
                {
                    // ---- ① D39/D40 **实拍**：量的是**建出来**那两行（这一节抽屉正开着、`_tab == 2`）
                    //   —— 走的是真实调用点，所以「实参有没有传进去」也一并管住（模型层探针管不到那一格）。
                    var g2f = _rt.State.Factions();
                    Check(g2f.Count, 13, "（前提）本窗阵营表 **13** 档 —— Army 行 5 行才谈得上 630");
                    if (g2f.Count > 0)
                    {
                        int lrow = (g2f.Count - 1) / 3, lcol = (g2f.Count - 1) % 3;
                        string lastF = g2f[g2f.Count - 1];
                        if (_rt.UiCosmoFilterCell("$fac:" + lastF, out float lx, out float ly,
                                                  out float lw, out float lh, out bool _lOn))
                            // 面板内 x = `pad L14` + 列×107 + 半格 50 · y = 15 + 50 + 行×(100+**20**) + 半格 50
                            // ⇒ 最后一行（第 5 行）中心 = 156 + 595 = **751.0**（`m_Spacing.y` 退回 0 时是 **671.0**）
                            CheckRectPx("$fac:" + lastF, lx, ly, lw, lh,
                                        2.2f + 14f + lcol * 107f + 50f,
                                        156f + 15f + 50f + lrow * 120f + 50f, 100f, 100f);
                        else Check(true, false, $"量不到最后一行那格 Army（`$fac:{lastF}`）");

                        if (_rt.UiCosmoFilterCell("$owned", out float ox, out float oy,
                                                  out float ow, out float oh, out bool _oOn))
                            // 面板内行顶 = 15 + **630** + 12.81 = 657.81 ⇒ 中心 682.81 ⇒ 绝对 **838.81**
                            // （`m_Spacing.y` 退回 0 时 = 156 + 577.81 + 25 = **758.81**）
                            CheckRectPx("$owned", ox, oy, ow, oh,
                                        2.2f + 331.7f * 0.5f, 156f + 657.81f + 25f, 331.7f, 50f);
                        else Check(true, false, "量不到 `$owned`（Owned only）那一格");
                    }

                    // ---- ② D39-D42 **模型层**：两扇窗各跑一次，★ 那几条「差」就是**判别式**
                    //   （同一套常量 / 实参没传进去时，下面那些差全变 0 ⇒ 必红）。
                    //   ⚠️ 两次探针**共用同一个 `w` = 331.7** —— 只有把宽度钉死，差出来的才是**参数**差，
                    //     而不是「卡组编辑面板比收藏窗窄」那点几何差（真实宽 331.73 / 335.31 见 `FilterPanelModel`）。
                    var g2A = new List<FilterPanelModel.Cell>();     // 收藏窗档 = **缺省**形参
                    var g2B = new List<FilterPanelModel.Cell>();     // 卡组编辑档 = 四个原版字面量显式传
                    FilterPanelModel.BuildCosmetics(g2f, _rt.State.Filter, 331.7f, g2A);
                    FilterPanelModel.BuildCosmetics(g2f, _rt.State.Filter, 331.7f, g2B,
                                                    labelAutoMin: 26f, armySpY: 20f, iconW: 80f, labW: 230f);
                    var oA = g2A.Find(c => c.Key == "$owned");
                    var oB = g2B.Find(c => c.Key == "$owned");
                    CheckTrue(oA.Key != null && oB.Key != null,
                              "（前提）两组探针里 `$owned` 那一格都出得来");
                    if (oA.Key != null && oB.Key != null)
                    {
                        // 收藏窗（原版：`spacing.y = 0` · 两颗走**锚点**）
                        CheckNear(oA.R.y1, 577.81f, 0.6f,
                                  "收藏窗 `Owned` 行顶（面板内）= 15 + **550** + 12.81 = **577.81**（原版 `spacing.y = 0`）");
                        CheckNear(oA.Bg.W, 69.51f, 0.6f,
                                  "收藏窗 `Owned Toggle/Image` 宽 = `0.3×331.7 − 30` = **69.51**（原版锚 `a(0.7,0)-(1,1) sd(-30,0)`）");
                        CheckNear(oA.Bg.x2, 306.7f, 0.6f, "……右缘 = `w − 25` = **306.7**（两扇窗这一条相同）");
                        CheckNear(oA.Lab.x1, 25f, 0.6f, "收藏窗 `Label` 左缘 = **25**（两扇窗这一条相同）");
                        CheckNear(oA.Lab.x2, 232.19f, 0.6f,
                                  "收藏窗 `Label` 右缘 = `0.7×331.7` = **232.19**（原版锚 `a(0,0)-(0.7,1) sd(-25,0)`）");
                        // 卡组编辑（原版：`spacing.y = 20` · 两个 `sd` 固定宽）
                        CheckNear(oB.R.y1, 657.81f, 0.6f,
                                  "卡组编辑 `Owned` 行顶（面板内）= 15 + **630** + 12.81 = **657.81**"
                                + " ⇒ 绝对 **813.81**（原版 `Army Filter/Content` 的 `m_Spacing.y = 20`）");
                        CheckNear(oB.Bg.W, 80f, 0.6f,
                                  "卡组编辑 `Owned Toggle/Image` 宽 = **80**（原版 `sd(80,0)`）");
                        CheckNear(oB.Bg.x2, 306.7f, 0.6f, "……右缘仍是 `w − 25` = **306.7**");
                        CheckNear(oB.Lab.x1, 25f, 0.6f, "卡组编辑 `Label` 左缘 = **25**");
                        CheckNear(oB.Lab.x2, 255f, 0.6f,
                                  "卡组编辑 `Label` 右缘 = 25 + **230** = **255**（原版 `sd(230,0)`）");
                        // ★ **灭自证 / 判别式** —— 每条差都必须是原版那个差；同一套常量时全部变 0。
                        CheckNear(oB.R.y1 - oA.R.y1, 80f, 0.6f,
                                  "★ 两窗 `Owned` 行顶差 = 630−550 = **80**（= 4×20；同一套常量时是 0）");
                        CheckNear(oA.Bg.x1 - oB.Bg.x1, 10.49f, 0.6f,
                                  "★ 两窗 `Image` **左缘**差 = 80−69.51 = **10.49**"
                                + "（卡组编辑那颗往左多出 10.49，右缘不动；同一套常量时是 0）");
                        CheckNear(oB.Lab.W - oA.Lab.W, 22.81f, 0.6f,
                                  "★ 两窗 `Label` **宽**差 = 230−207.19 = **22.81**（同一套常量时是 0）");
                        CheckNear(FilterPanelModel.CosmoArmyRowH(g2f.Count, 20f)
                                  - FilterPanelModel.CosmoArmyRowH(g2f.Count), 80f, 0.6f,
                                  "★ 两窗 Army **行高**差 = 630−550 = **80**（⛔ 别把缺省 `armySpY` 改成 20）");
                    }
                }

                // ================================ 🆕 2026-10-18（A890）：那块 `Image` 的**锚法**到底怎么算
                //   账 = 「D41 的锚法只落到矩形上」：原版那两颗 `Owned Toggle/{Image,Label}` 的
                //   `m_AnchorMin/Max`、`m_Pivot` 当时**没搬进模型** ⇒ 判不出「面板宽可变时，卡组编辑那颗
                //   `Image` 的右缘该是**固定 308.9** 还是 `w−25`」（⚠️ 两棵树在各自真实宽度处**重合**）。
                //   🆕 **现读 RT 本体（序列化字段，不是 `menu_dump` 的布局模拟值）⇒ 答 `w − 25`**：
                //     · 卡组编辑 `…/Cosmetic FIlter/Filters/Owned Toggle/Image` =
                //       `d:/2/新解包资源/assets_full/bundle_menus_assets_all/RectTransform/RectTransform_-5843448329795342556.json`
                //       `m_AnchorMin (1,0)` · **`m_AnchorMax (1,1)`** · `m_Pivot (1,0.5)` · `m_AnchoredPosition (-25,0)` ·
                //       **`m_SizeDelta (80,0)`** ⇒ **右锚**：右缘恒 = 父级右缘 − 25（**面板一宽它跟着走**）；
                //       **固定的量是【宽 80】，不是右缘** —— `308.90` 只是 `w = 333.90` 那一刻的取值。
                //     · `…/Owned Toggle/Label` = `…/RectTransform_-6194547466839724252.json`：`AnchorMin (0,0)` ·
                //       `AnchorMax (0,1)` · `Pivot (0,0.5)` · `apos (25,0)` · `sd (230,0)` ⇒ **左锚 + 固定宽** ⇒
                //       25..**255**、**不随 `w` 变**（⚠️ 同一行里两半**不一样**，别一刀切）。
                //     · 收藏窗那棵（`…Cardback Tab/Cardback Display/Cosmetic FIlter/Filters/Owned Toggle (1)`）=
                //       `…/RectTransform_1643317952186213077.json`：`AnchorMin (0.7,0)` · **`AnchorMax (1,1)`** ·
                //       `Pivot (1,0.5)` · `apos (-25,0)` · `sd (-30,0)` ⇒ **同样右锚**、宽 `0.3w−30`
                //       ⇒ **两棵树差的只是【宽】，右缘规则相同**（= `ToggleRowRects` 的 `ix2 = w − ToggleIconRightIn`）。
                //   ★ 判别式（灭自证）：**宽度一改，右缘必须跟着改同样的量、宽必须一动不动** ——
                //     写成「固定右缘 308.9」⇒ 右缘差命 **0** ⇒ 红；退回锚点式 ⇒ 卡组编辑那档**宽差命 0** ⇒ 红。
                //   ⛔ 期望值全是**原版字面量**（25 / 80 / 230 / 255）与几何推论（`w−25`、`Δw`），
                //     **不从 `CosmoIconWDeckEdit` / `CosmoLabWDeckEdit` 读**（那是被测实现用的常量）。
                {
                    const float wSm = 331.73f;   // 本窗面板的真实宽（`DeckRuntime.FltW` 那棵树）
                    const float wBg = 400f;      // 另取一个**原版没有的宽** —— 就是为了把「跟着走 / 固定」分开
                    var zSm = new List<FilterPanelModel.Cell>();
                    var zBg = new List<FilterPanelModel.Cell>();
                    FilterPanelModel.BuildCosmetics(_rt.State.Factions(), _rt.State.Filter, wSm, zSm,
                                                    iconW: 80f, labW: 230f);
                    FilterPanelModel.BuildCosmetics(_rt.State.Factions(), _rt.State.Filter, wBg, zBg,
                                                    iconW: 80f, labW: 230f);
                    var iSm = zSm.Find(c => c.Key == "$owned");
                    var iBg = zBg.Find(c => c.Key == "$owned");
                    CheckTrue(iSm.Key != null && iBg.Key != null,
                              "（前提）`w = 331.73 / 400` 两档探针里 `$owned` 那一格都出得来");
                    if (iSm.Key != null && iBg.Key != null)
                    {
                        // ① 判据值：右缘 = `w − 25`（**两个宽度各自成立** ⇒ 不是固定的 308.9）
                        CheckNear(iSm.Bg.x2, wSm - 25f, 0.6f,
                                  "卡组编辑 `Image` 右缘（`w = 331.73`）= `w − 25` = **306.73**"
                                + "（原版右锚：`m_AnchorMax.x = 1` + `m_AnchoredPosition.x = -25` + `pivot.x = 1`）");
                        CheckNear(iBg.Bg.x2, wBg - 25f, 0.6f,
                                  "★ …换成 `w = 400` 右缘**还是** `w − 25` = **375** ⇒ **不是固定 308.9**");
                        CheckNear(iSm.Bg.W, 80f, 0.6f,
                                  "…而宽恒 **80**（原版 `m_SizeDelta = (80,0)` —— **固定的是宽**）");
                        CheckNear(iBg.Bg.W, 80f, 0.6f,
                                  "★ …换宽度它**一动不动**（面板宽只改右缘、不改宽）");
                        // ② `Label` 那一半**相反**：左锚 + 固定宽 ⇒ 两个宽度下都落在 25..255
                        CheckNear(iSm.Lab.x1, 25f, 0.6f, "`Label` 左缘 = **25**（原版 `apos(25,0)`）");
                        CheckNear(iBg.Lab.x1, 25f, 0.6f, "★ …换宽度**还是** 25（左锚）");
                        CheckNear(iSm.Lab.x2, 255f, 0.6f, "…右缘 = 25 + **230** = **255**（原版 `sd(230,0)`）");
                        CheckNear(iBg.Lab.x2, 255f, 0.6f,
                                  "★ …`w = 400` 时**仍是 255**（左锚固定宽 ⇒ **不**随 `w`；若按 `0.7w` 算这里会是 **280**）");
                        // ③ ★ 判别式：Δ右缘 = Δw（不是 0）· Δ宽 = 0（不是 `0.3Δw`）
                        CheckNear(iBg.Bg.x2 - iSm.Bg.x2, wBg - wSm, 0.6f,
                                  "★ **右缘差 = Δw = 68.27**（右锚；写成「固定右缘」时这里是 **0**）");
                        CheckNear(iBg.Bg.W - iSm.Bg.W, 0f, 0.6f,
                                  "★ **宽差 = 0**（`sd(80,0)` 固定；退回锚点式时这里是 `0.3Δw = 20.48`）");
                        CheckNear(iBg.Lab.x2 - iSm.Lab.x2, 0f, 0.6f,
                                  "★ `Label` 右缘差 = 0（左锚固定宽；按 `0.7w` 算这里会是 `0.7Δw = 47.79`）");
                    }
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
                CheckTrue(_rt.UiHasQuad("cosmoflt_bg"),
                          "（前提）`cosmoflt_bg` **建出来了** —— 不然下面那条「整栏不露」是拿「件不存在」蒙的"
                        + "（账 `A1532`）");
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

            // ================================ 🆕 2026-10-18（A889）：卡背那两行**不吃**卡牌抽屉的滚动量
            //   账原文：`RefreshCosmoFilters` 那三处（quad / 命中区 / `Label`）走 `FltAbs`，而 `FltAbs` 恒减
            //   **`_fltScroll`（卡牌抽屉的）**，卡背抽屉**自己没有滚动区**（`HandleScroll` 的卡背那一支只把
            //   滚轮吃掉）⇒ 只要 `_fltScroll ≠ 0`，卡背那两行就被**别人的滚动位**整体推走。
            //   `_fltScroll` 什么时候非 0：在 Cards 页滚卡牌抽屉（`HandleScroll` / 自检口 `UiScrollFilters`），
            //   而 **`SetTab` 不重置它** ⇒ 切到 Cosmetics 页时照样带着。
            //   **修法选 A**（卡背那一族有自己的滚动量 `_cosmoFltScroll`，**今天恒 0**，四处全走 `FltAbsCosmo`）：
            //   选 B（三个点直接不换算）会把「这一族有没有滚动量」**散在三个调用点**上 —— 将来真要加滚动得改三处、
            //   还得靠人记得，而且**只读口 `UiCosmoFilterCell` 必然漏一处**（读数与画面对不上）。
            //   A 的形状照收藏窗那一侧（`Shell/CollectionWindow.cs` 的
            //   `cosmo ? Abs(c.R) : _fltScroll.Shift(Abs(c.R))` —— 那边**本来就不借**）。
            //   ⚠️ `SetTab` **没动**：改完 `_fltScroll` 在卡背侧**零消费者**，剩下 7 个消费点（搜索框 3 · 格子 3 ·
            //   小标题 1）全在**卡牌那一族**；而卡背抽屉的**开合**本来就跨页签保留（A67）⇒「卡牌抽屉的滚动位
            //   也跨页签保留」是同一条口径（不是新偏离）。
            //   ★ 判别式（这一笔的关键）：**在 Cards 页把卡牌抽屉滚到底（464.92）再切到 Cosmetics 页**，
            //   卡背那两行的绝对 y 必须**逐值等于几何值**（751.0 / 838.81），且**不等于**「几何值 − 464.92」。
            //   ⛔ 期望值全是**几何量**（原版字面量算出来的绝对 y）—— 不是「相对基线不变」那种同义反复，
            //   更不是「`_fltScroll == 0`」。
            Section("卡背抽屉两行 × 卡牌抽屉的滚动量（A889）");
            {
                const float fltMax = 464.92f;   // 卡牌抽屉内容 1389.02 − 可见 924.1（判据 → `UiScrollFilters` 那段）
                var a889f = _rt.State.Factions();
                string a889Last = a889f[a889f.Count - 1];

                // ---- ① 「从没滚过」那一档：卡牌抽屉的滚动位先归 0，量卡背两行的**几何 y**
                _rt.UiSetTab(0);
                CheckNear(_rt.UiScrollFilters(-1e6f), 0f, 0.01f, "（夹具）**卡牌**抽屉先滚回顶（`_fltScroll = 0`）");
                _rt.UiSetTab(2);
                if (!_rt.CosmoFiltersOpen) _rt.UiToggleFilters();
                CheckTrue(_rt.CosmoFiltersOpen, "（前提）卡背抽屉开着（这一页）");
                float bFac, bOwn; bool g1, g2;
                g1 = _rt.UiCosmoFilterCell("$fac:" + a889Last, out _, out bFac, out _, out _, out _);
                g2 = _rt.UiCosmoFilterCell("$owned", out _, out bOwn, out _, out _, out _);
                CheckTrue(g1 && g2, "（前提）卡背那两行（最后一行 Army + `$owned`）都量得到");
                CheckNear(bFac, 751f, 0.6f,
                          "「从没滚过」：最后一行 Army 格中心 y = **751.0**（= 156 + 15 + 50 + 4×(100+20) + 50）");
                CheckNear(bOwn, 838.81f, 0.6f, "「从没滚过」：`$owned` 行中心 y = **838.81**（= 156 + 657.81 + 25）");

                // ---- ② 在 Cards 页把**卡牌**抽屉滚到底
                _rt.UiSetTab(0);
                if (!_rt.FiltersOpen) _rt.UiToggleFilters();
                CheckNear(_rt.UiScrollFilters(1e6f), fltMax, 0.6f,
                          "（夹具）**卡牌**抽屉滚到底 = 内容 1389.02 − 可见 924.1 = **464.92**");

                // ---- ③ 切到 Cosmetics 页 ⇒ 卡背那两行**逐值等于几何值**（不吃那 464.92）
                _rt.UiSetTab(2);
                float still = _rt.UiScrollFilters(0f);        // 只读回读（不改滚动位）
                CheckTrue(still > 400f,
                          $"（前提）切页后卡牌那段滚动量**还在**（{still:F2}）—— 本笔的前提就是「它跨页签保留」"
                        + "（`SetTab` 不重置）；若要改成归零，这条前提与下面那两条判别式**要一起重写**，别只改这里");
                float aFac, aOwn; bool h1, h2;
                h1 = _rt.UiCosmoFilterCell("$fac:" + a889Last, out _, out aFac, out _, out _, out _);
                h2 = _rt.UiCosmoFilterCell("$owned", out _, out aOwn, out _, out _, out _);
                if (h1 && h2 && g1 && g2)
                {
                    CheckNear(aFac, 751f, 0.6f, "★ 卡牌抽屉滚到底之后，最后一行 Army 格中心 y **还是 751.0**（几何值）");
                    CheckNear(aOwn, 838.81f, 0.6f, "★ ……`$owned` 行中心 y **还是 838.81**（几何值）");
                    CheckNear(aFac - bFac, 0f, 0.6f, "★ 与「从没滚过」**逐值相同**（Δ = 0）");
                    CheckNear(aOwn - bOwn, 0f, 0.6f, "★ ……同上（Δ = 0）");
                    // 判别式：改回吃 `_fltScroll` ⇒ 读数正好是「几何值 − 464.92」⇒ 这两条红
                    CheckTrue(Mathf.Abs(aFac - (751f - fltMax)) > 1f,
                              $"★ **判别式**：它**不是**「751.0 − 464.92 = **286.08**」——那正是「还在吃 `_fltScroll`」"
                            + $"时的读数（实 {aFac:F2}）");
                    CheckTrue(Mathf.Abs(aOwn - (838.81f - fltMax)) > 1f,
                              $"★ **判别式**：它**不是**「838.81 − 464.92 = **373.89**」（同上；实 {aOwn:F2}）");
                }
                else Check(true, false, "量不到卡背那两行 ⇒ 这一节**没验到**（别当成通过）");

                // ---- ④ 实拍那一腿：**只读口与建库两处都改了**才算修好
                //   `UiNodeRect("cosmoflt_cell")` = 树里**第一个** `cosmoflt_cell` 节点（= `Factions()[0]` 那格）
                //   的 quad 包围盒（`ImageQuad.Create(..., name)` 把 GameObject 就叫这个名，一格一个）⇒ 量的是
                //   **真建出来**的那个中心。只读口改对了、建库那一半没改 ⇒ 这条红（读数说 271.0、画面在 286.08 那一档）。
                //   几何值 = `FltY` 156 + `CosmoSpacing1` 15 + `Title` 50 + 半格 50 = **271.0**（与上一节第一条实拍同源）。
                if (_rt.UiNodeRect("cosmoflt_cell", out float qx, out float qy, out float qw, out float qh))
                    CheckNear(qy, 155.97f + 15f + 50f + 50f, 0.6f,
                              "★ **实拍**：滚过卡牌抽屉之后，卡背**建出来**那一格的图示中心 y 仍是 **271.0**"
                            + "（建库那一半没收口 ⇒ 这里是 271.0 − 464.92）");
                else Check(true, false, "`UiNodeRect(cosmoflt_cell)` 量不到 ⇒ 实拍那一腿没验到");

                // ---- 收尾：滚动位归 0 + 两个抽屉还原成**关**（下一节要 `_tab == 0` + 抽屉关着起）
                //   🔴 **2026-10-18 修（`DeckScene` 11 条红的根因 R1）**：`DeckRuntime.ToggleFilters()`
                //     **按 `_tab` 分派**（`_tab==2` ⇒ 关**卡背**抽屉；否则关**卡牌**抽屉 —— 这是 2026-10-01 起、
                //     有注释的既定契约）。原来这里**在 `_tab==0` 上调它去关卡背抽屉** ⇒ 实际翻的是**卡牌**抽屉
                //     ⇒ ① 卡背抽屉压根没关（本节的收尾断言红）② `_filtersOpen` 被**反翻成 true** 且此后无人关，
                //     而 `UiClickPx` 的路由是「`FltStripOn && px.x < FltX+FltW(=333.9)` ⇒ 整条改道 `HandleFilterClick`」
                //     （`DeckRuntime.cs`，**既有且共用的契约，⛔ 别动它**）⇒ 左侧与页脚那些点**全被筛选抽屉吞掉**
                //     （`UiTopKeyAt` 直调 `TopKeyAt`、不走这条路由 ⇒ 「**查得到、点不到**」）。
                //     ⇒ 修法 = **在和抽屉匹配的页签上关它**；这一改同时让 10 条连锁红回绿（⛔ 不单独去改那 10 条 —— 那是给假绿开口子）。
                _rt.UiSetTab(2);
                if (_rt.CosmoFiltersOpen) _rt.UiToggleFilters();     // `_tab==2` ⇒ 关的才是卡背抽屉
                Check(_rt.CosmoFiltersOpen, false, "（收尾）卡背抽屉也关回去");
                _rt.UiSetTab(0);
                CheckNear(_rt.UiScrollFilters(-1e6f), 0f, 0.01f, "（收尾）滚动位还给 0 —— 不留给后面的断言");
                if (_rt.FiltersOpen) _rt.UiToggleFilters();
                Check(_rt.FiltersOpen, false, "（收尾）卡牌抽屉关着（下一节点 `Filters` 钮要看它从关到开）");
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
            // 🔴 **2026-10-21（`A1721`）**：这里原来是 `UiScrollDeck(56f)` —— 行距还是 56 时它**正好**
            //   滚过一整行；行距改成原版真值 **57.7** 之后 56 < 57.7 ⇒ `firstRow` 仍是 0 ⇒
            //   「第一行换了卡」那条会**假红**。⇒ 改成**明确大于一个行距**的 58（滚过一整行）。
            _rt.UiScrollDeck(58f);
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
            //   顺序**逐级验**（每级的「另一种实现」都会让对应那条红 —— 见各条文案）。
            //   🔴 2026-10-17（T23 删件）：本窗的级数从三级降到**两级**（编辑中 / 保存）——
            //     中间那一级「导入弹窗开着 ⇒ 只关弹窗」随导入弹窗一起删了；模态消息窗那一级
            //     （`closeOnESC = 0`）由下面的 A502 那一节单独验。
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
                // 🔴 **2026-10-17（T23 删件）**：这里原来还有一级「② 导入弹窗开着 ⇒ ESC 只关弹窗、不落盘」
                //   （判据 = 原版 `ImportDeckPopup` 是独立窗、`closeOnESC = 1`）。导入弹窗那棵树整棵删了
                //   ⇒ 本窗已经没有那一级。**「`closeOnESC = 1` 的窗 ESC 归它自己」这条行为仍在外壳**
                //   （`Shell/ImportDeckPopup.cs` / `GameWindow` 那套），`CollectionScene` 有对应断言。
                // ② 文本编辑中 ⇒ **不抢**（ESC 归 `HandleTyping` 那条既有路：取消编辑）
                state.Deck.Name = "A223·编辑中不该落盘";
                _rt.UiBeginNameEdit();
                CheckTrue(_rt.NameEditing, "（前提）改名中输入态");
                _rt.EscPressed();
                CheckTrue(_rt.NameEditing, "★ ②…**编辑中 ESC 不归保存**（输入框先吃：`EscPressed()` 第一句 return）");
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
                // 🔴 **2026-10-17（D35 删件）就地改写**：这一句原来读 `UiLabelText("foot_verdict")`
                //   （页脚那行「合法 / <不合法原因>」判词）。**那行字原版没有、已删** ⇒ 按「删件配反向断言」
                //   换成「它不在」，而「不合法这件事说得出话」由下面 ③ 的 `UiLastSay` + `foot_hl` 两处接住。
                CheckTrue(FindDeep(_rt.Root, "foot_verdict") == null,
                          "★ D35：页脚那行「合法/不合法」判词**已经不存在**（原版侧栏没有它）");
                CheckTrue(_rt.UiHasQuad("foot_hl"),
                          "（前提）`foot_hl` **建出来了** —— 不然下面那条「灭掉」是拿「件不存在」蒙的（账 `A1532`）");
                CheckTrue(!_rt.UiQuadActive("foot_hl"),
                          "……`Done Highlight` **灭掉**（原版 `UpdateDoneButton` 用的是同一个 `ValidateDeck`）");
                Check(DeckLibrary.Load().Current.CardIds.Count, keepIds.Count,
                      "（前提）盘上仍是满编 —— 摘掉的只是**内存里那一份**");

                // ---- ③ `Done` ⇒ **不落盘** + **出声** ----
                live.Name = "A330·Done 不该落盘";
                // 🔴 **2026-10-17（D35 删件）**：原来这里读的是屏幕上那行 `notice`（`UiLabelText("notice")`）
                //   —— 那行字**原版没有、已删** ⇒ 改读**消息通道**（`UiLastSay`，`Say()` 那份状态；
                //   它同时仍 `Debug.Log`）。⛔ 判据没放宽：**内容必须还是同一份文案**（`DeckRules.Describe`）。
                string noticeBefore = _rt.UiLastSay;
                CheckTrue(FindDeep(_rt.Root, "notice") == null && FindDeep(_rt.Root, "store_err") == null
                          && FindDeep(_rt.Root, "side_title") == null,
                          "★ D35/D7/D10：底部 `notice` / `store_err` 两行与左栏 `side_title` **都不存在**"
                          + "（三件都是「我们多画的」）");
                _rt.UiBtnRect("foot_done", out float dnX, out float dnY, out float dnW, out float dnH);
                float dcx = dnX + dnW * 0.5f, dcy = dnY + dnH * 0.5f;
                CheckTrue(_rt.UiClickPx(dcx, dcy) && _rt.UiTopKeyAt(dcx, dcy) == "foot_done",
                          "点页脚 `Done` 的中心（走鼠标那条路，命中 `foot_done`）");
                Check(DeckLibrary.Load().Current.Name, keepName,
                      "★ A330：**卡组不合法 ⇒ 点 Done 不落盘**（原版 `__TrySaveDeck.c:81` 弹窗后 return，"
                      + "不 UploadDeck；把 `SaveAndSay()` 里那道闸删掉/改成 `if (false)` ⇒ 这条红）");
                Check(DeckLibrary.Load().Current.CardIds.Count, keepIds.Count, "★ ……而且盘上张数也没变");
                CheckTrue(_rt.UiLastSay != noticeBefore
                          && (_rt.UiLastSay ?? "").Contains("卡组张数不够"),
                          "★ ……而且**出声了**，印的是那条词条的**中文列**（`Say()` 那句：「" + _rt.UiLastSay
                        + "」—— 静默挡下 = 这条红）。"
                        + "｜🔴 **2026-10-18（`G9`）灭自证**：期望值原来是 `DeckRules.Describe(…)`"
                        + "（**取自实现本身**）⇒ 那时「让 `Describe` 返回空串」照样绿；现在钉的是**字面量**。");

                // ==== 🆕 2026-10-18（`G9`）：`DeckRules.Describe` 从「造中文句子」改成「出词条键」====
                //   判据链：① 键的**形状**（照原版具名族 `"MenuDeck/Error/" + 名字`）；
                //          ② **表里真有这条键**（⛔ 坑表 #18：「原版有词条」≠「我们表里有键」——
                //             键不在 ⇒ `Loc.T` 返回**键名本身**、界面上就印键名）；
                //          ③ **中英两档各印一句话**且**逐字不同**（这才是「显示层真的走词条了」）。
                {
                    string kFew = DeckRules.Describe(DeckError.TooFewCards);
                    Check(kFew, "MenuDeck/Error/TooFewCards",
                          "★ G9：`Describe(TooFewCards)` 出的是**词条键**、形状照原版具名族"
                        + "｜🧨 改回返回中文整句（或返回空串）⇒ 红");
                    Check(DeckRules.Describe(DeckError.None), "",
                          "★ G9：`None` ⇒ **空串**（原版 `ToRawLocalizationString` 的 `err==0` 那一支）");
                    CheckTrue(Loc.HasEntry(kFew),
                          $"★ G9：`{kFew}` **在 `Loc` 表里** —— 键不在 ⇒ 界面上印的是键名（不是中文、也不是英文）");
                    // 🔴 **2026-10-17（`T08`）就地订正（铁律 5）**：`Describe` 那族今天 = **12 条枚举具名键
                    //   + 1 条 catch-all `MenuDeck/Error/InvalidDeck`**（共 13 条；现读 `Core/Loc.cs` 的
                    //   `MenuDeck/Error/*` 段）—— `EffectOnlyCard` **原版没有这个键**，`A1868` 已从表里删掉，
                    //   `Describe` 那一档归 catch-all ⇒ 下面第二个 `HasEntry` 命中的是 `InvalidDeck`，
                    //   **不是** `EffectOnlyCard`。原文「13 条键抽查三条（`WrongFaction` / `EffectOnlyCard` 各一）」已过期。
                    CheckTrue(Loc.HasEntry(DeckRules.Describe(DeckError.WrongFaction))
                              && Loc.HasEntry(DeckRules.Describe(DeckError.EffectOnlyCard)),
                          "★ G9：`Describe` 那族 = **12 条枚举具名键 + 1 条 catch-all `InvalidDeck`**（共 13 条）都在表里"
                        + "（抽查 `TooFewCards` / `WrongFaction` 走自己的键；`EffectOnlyCard` **无原版键**、归 catch-all）"
                        + "｜🔴 2026-10-17（`A1868`/`A1872`）订正：`EffectOnlyCard` 那条键**已删**，"
                        + "这里 `Describe(EffectOnlyCard)` 返回的是 `MenuDeck/Error/InvalidDeck`");

                    // 🔴🔴 **2026-10-17（`T08`）新增两条【灭自证】**：上面那条只证明「`Describe(EffectOnlyCard)`
                    //   的返回值在表里」—— **两态都绿**（自造键还在表里时绿、归 catch-all 时也绿）
                    //   ⇒ 它测不出「自造键删干净了没有」。下面两条各钉住一半，合起来**不可能一起退化成旧写法**：
                    //     ① 表里**确实没有** `MenuDeck/Error/EffectOnlyCard`（`A1868` 删掉了）；
                    //     ② `Describe(EffectOnlyCard)` **逐字等于** catch-all 键（⛔ 不是 `TermPrefix + e`）。
                    //   🧨 **改坏法**：把 `Describe` 改回 `TermPrefix + e` ⇒ ② 红（① 仍绿 ⇒ 必须两条并存）；
                    //      把 `MenuDeck/Error/EffectOnlyCard` 加回 `Loc`（假装「原版有这个键」）⇒ ① 红。
                    CheckTrue(!Loc.HasEntry("MenuDeck/Error/EffectOnlyCard"),
                          "★ 灭自证①：`MenuDeck/Error/EffectOnlyCard` **不在** `Loc` 表里 ——"
                        + " 原版没有这个键（官方 `MenuDeck/Error/*` 只有 9 条具名键），我们那条自造键已删"
                        + "｜🧨 把它加回表里 ⇒ 红");
                    Check(DeckRules.Describe(DeckError.EffectOnlyCard), "MenuDeck/Error/InvalidDeck",
                          "★ 灭自证②：`Describe(EffectOnlyCard)` = **catch-all** `MenuDeck/Error/InvalidDeck`"
                        + "（⛔ 不再拼 `TermPrefix + e` —— 那样会印出**键名本身**）"
                        + "｜🧨 把它改回 `TermPrefix + e` ⇒ 红");

                    Loc.RestoreForTest(AvailableLanguages.Chinese);
                    string zhMsg = DeckRuntime.DeckErrorText(DeckError.TooFewCards);
                    Loc.RestoreForTest(AvailableLanguages.English);
                    string enMsg = DeckRuntime.DeckErrorText(DeckError.TooFewCards);
                    Loc.RestoreForTest(AvailableLanguages.Chinese);       // 收尾：还给本节的语档
                    Check(zhMsg, "卡组张数不够", "★ G9 **中文档**：键取到的是中文那一列（**字面量**，不是从实现里读）");
                    CheckTrue(enMsg.Length > 0 && enMsg != zhMsg && enMsg != kFew,
                          $"★ G9 **英文档**：同一把键取到**另一句话**（「{enMsg}」）"
                        + "｜🧨 那半边原来是引擎里写死的中文 ⇒ 英文档下**照样印中文** ⇒ 红");

                    // 🧨 **`Loc.HasEntry(null)` 不该抛**（`Dictionary.ContainsKey(null)` 会抛
                    //    `ArgumentNullException`）—— 上游只要有一处把「本该非空的键」算成 null，
                    //    整条自检就**崩掉**（没有「期望 X / 实得 Y」，还容易被当成环境问题放过去）。
                    bool threw = false, got = true;
                    try { got = Loc.HasEntry(null); }
                    catch (System.Exception ex)
                    { threw = true; Debug.LogWarning("[DeckScene] `Loc.HasEntry(null)` 抛了：" + ex.GetType().Name); }
                    CheckTrue(!threw && !got,
                          "★ G9：`Loc.HasEntry(null)` **不抛异常**、返回 `false`（并出声）"
                        + "｜🧨 把那道判空删掉（退回裸的 `Table.ContainsKey(key)`）⇒ 抛 "
                        + "`ArgumentNullException` ⇒ 红");
                    CheckTrue(!Loc.HasEntry("MenuDeck/Error/__没有这条键__"),
                          "★ G9 对照：**有键名的**查不到照样返回 `false`（不是「恒 true」那种假保护）");
                }

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

                // ---- ① 文案：原版号 → 术语键 → **显示的是词条**（纯函数先钉住，不依赖窗口）----
                //   🔴 **2026-10-19（A1012）就地订正（铁律 5）**：原来写的是「**显示的是键**」——
                //     `Shell/PopUpGameWindow.cs` 的 `Term()` 已改成「先查 `Terms`、再转 `Core/Loc.cs`」
                //     ⇒ 表里**有**的键印的是**词条**，**两处都没有**才印键名（旧句已不成立）。
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
                // 🔴 **2026-10-19（A1012）就地重写（铁律 5）**：这一段原来拿本类自己的 `Terms`
                //   （一张**恒空的**表）去证明 `MenuDeckErrorKey`（我们自己的函数）—— **自证结构**，
                //   而且 `A1012` 之后**必红**（判据已从 `Terms.ContainsKey` 换成 `Loc.HasEntry`）。
                //   现在拆成三条**各自独立**的东西：
                //     ① **兜底键在 `Loc` 表里** —— 它不在 ⇒ 那扇窗连兜底都印键名（`Term` 末跳就是 `Loc.T`）；
                //     ② **数字键今天不在 `Loc` 表里** —— 这是下面「落兜底键」那条期望值的**真前提**
                //        （原来那条 `Terms.Count == 0` 钉的是**我们的实现细节**，⛔ 不是原版判据）；
                //     ③ **两个口各看各的表**：选键那一侧看 `Loc`、取字那一侧**先**看 `Terms`
                //        ⇒ 往 `Terms` 塞一条 `MenuDeck/Error/5`，**选出来的键不动、字却取到了**
                //        —— **同一个实现不可能同时**「选键看 `Terms`」又「选键不看 `Terms`」
                //        （原来那两条是「两边一起改回去就全绿」的自证）。
                //   🔴 **2026-10-17（`T08`）就地订正（铁律 5）**：`A1133` 补进表的三条数字键
                //     `MenuDeck/Error/{2,4,5}` **已按 `A1872` 删掉**（官方 I2 表一条数字键都没有；
                //     原版 `ToRawLocalizationString` 的 `{0}` 装的是**枚举**、`ToString()` 印**名字**）
                //     ⇒ ② 今天**又回到「不在表里」**，③-a 的期望值 = **兜底键** `MenuDeck/Error/InvalidDeck`。
                //     ⚠️ **数字侧（`MenuDeckErrorNumber`）不动**：仍回原版号 2/5/4（那是原版枚举号，⛔ 别改成名字）。
                //     ⚠️ 那两句「2026-10-09 起数字键在表里」的旧话**已作废**，⛔ 别再照它改期望值。
                CheckTrue(Loc.HasEntry(DeckRuntime.MenuDeckErrorKeyFallback),
                          $"★ 兜底键 `{DeckRuntime.MenuDeckErrorKeyFallback}` **在 `Loc` 表里**"
                        + "（`Term` 真能把它取成词条）｜🧨 它不在 ⇒ 那扇窗印的是键名本身");
                string numKey5 = string.Format(DeckRuntime.MenuDeckErrorKeyFmt,
                                               DeckRuntime.MenuDeckErrorNumber(DeckError.TooFewCards));
                CheckTrue(!Loc.HasEntry(numKey5),
                          $"★ **真前提**（2026-10-17 起的实情）：数字键 `{numKey5}` **不在** `Loc` 表里 ——"
                        + " `A1872`：官方 I2 表一条数字键都没有（`{0}` 装的是**枚举**、`ToString()` 印名字），"
                        + " `A1133` 补进表的那三条已删 ⇒ 下面 ③-a 的期望值（**落兜底键**）就是照这个实情写的"
                        + "｜🧨 哪天把数字键**加回**表里 ⇒ **这一条自动红**，那时把 ③-a 的期望值一起改回**数字键**"
                        + "（两态跟着切，⛔ 别把它当缺陷去改实现）");
                CheckTrue(PopUpGameWindow.Terms.Count == 0,
                          "（前提）本类的 `Terms` 落点**仍是空的** —— 它与 `Loc` 是**两处**表"
                        + "（`Term()` 先查前者、再转后者；见 `Shell/PopUpGameWindow.cs` 的 `Term`）");
                // ---- ③-a 选键那一侧：看 `Loc` ----
                Check(DeckRuntime.MenuDeckErrorKey(DeckError.TooFewCards), "MenuDeck/Error/InvalidDeck",
                      "★ 选键：数字键**不在表里** ⇒ 落**兜底键** `MenuDeck/Error/InvalidDeck`"
                    + "（原版 `ToRawLocalizationString` 拼的是数字键、查不到词条时原版也回兜底**键**；"
                    + "我们那一跳现读 = `Loc.HasEntry(key) ? key : MenuDeckErrorKeyFallback`）"
                    + "｜🧨 判据改回 `Terms.ContainsKey`（那张**恒空**的表）⇒ ③-c 红");
                Check(DeckRuntime.MenuDeckErrorKey(DeckError.None), "", "★ ……`None` ⇒ 空串（原版 `:16` 那一支）");
                // ---- ③-b 取字那一侧：**先**看 `Terms`、再转 `Loc` ----
                //   🔴 **2026-10-17（`T08`）改测兜底键**：原来测的是 `MenuDeck/Error/5`（那时它在表里）。
                //   数字键删掉之后 `Loc.T("MenuDeck/Error/5")` 两边**都回键名本身** ⇒ 断言恒真、失去鉴别力。
                //   现在测**兜底键**（它确实在表里）：`Term` 的第二跳能取到词条、且**不是键名**。
                Check(PopUpGameWindow.Term("MenuDeck/Error/InvalidDeck"), Loc.T("MenuDeck/Error/InvalidDeck"),
                      "★ 取字：`Terms` 没有、**`Loc` 有** ⇒ `Term()` 的第二跳转到 `Loc`、取到**词条**"
                    + "（`Loc.T` 的实值；⛔ 不是键名本身）");
                CheckTrue(PopUpGameWindow.Term("MenuDeck/Error/InvalidDeck") != "MenuDeck/Error/InvalidDeck",
                      "★ 灭自证：兜底键取到的是**词条**、不是键名 —— 与上一条并排 ⇒「第二跳真的进了 `Loc`」"
                    + "｜🧨 把 `Term()` 末句改回 `return key ?? \"\"` ⇒ 上一条与这条一起红");
                // ---- ③-c **灭自证（结构）**：同一时刻，选出来的键不动、字却取到了塞进去的那条 ----
                PopUpGameWindow.Terms["MenuDeck/Error/5"] = "（自检塞的假词条）";
                Check(DeckRuntime.MenuDeckErrorKey(DeckError.TooFewCards), "MenuDeck/Error/InvalidDeck",
                      "★ 灭自证之一：往 `Terms` 塞了 `MenuDeck/Error/5`，**选出来的键仍是兜底键** ——"
                    + " 选键那一侧看的是 `Loc.HasEntry`（数字键**不在**表里），`Terms` **不参与选键**"
                    + "｜🧨 判据改回 `Terms.ContainsKey` ⇒ 选出来会变成 `MenuDeck/Error/5` ⇒ 红");
                Check(PopUpGameWindow.Term("MenuDeck/Error/5"), "（自检塞的假词条）",
                      "★ 灭自证之二：**同一时刻** `Term` 取到的却是塞进去的那条（`Terms` 仍**优先于** `Loc`）"
                    + " —— 与上一条并排 ⇒ 「两处表、两个口各看各的」被钉死"
                    + "｜🧨 删掉 `Term` 里 `Terms` 那一跳 ⇒ 红（这两条**不可能**被同一个实现同时满足）");
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
                    Check(pop.MessageKey, "MenuDeck/Error/InvalidDeck",
                          "★ 正文键 = **兜底键** `MenuDeck/Error/InvalidDeck`（`TooFewCards` 的原版号是 **5**，"
                        + "但数字键 `MenuDeck/Error/5` **不在** `Loc` 表里 ⇒ `MenuDeckErrorKey` 落兜底键；见 ① ③-a）");
                    // 🔴 **2026-10-19（A1012）就地改口径（铁律 5）**：下面这几条原来断的是
                    //   「**画出来的就是那个键**」—— `Term()` 转 `Loc` 之后**已经不对了**（印的是**词条**）。
                    //   现在按本文件既有写法（先例 = A891 那一节的 `Loc.HasEntry(键)` + `Loc.T(键)` 一对）拆成：
                    //     ① **画出来的是那条键的词条**（期望值从 `Loc` 表取，⛔ 不是从实现反推）；
                    //     ② **拿的是哪个键**（那半句原来挂在 `*Shown` 上，归 `*Key` —— 与 `本文件` 那组同形）；
                    //     ③ **灭自证**：印的**不是键名本身** —— 这正是本缺陷要防的那一半
                    //        （把 `Term()` 末句改回 `return key ?? ""` ⇒ ③ 红）。
                    //   ⚠️ 只有 ① 会「两边一起退化成键名」而假绿（键不在表里时）⇒ ①③ 缺一不可。
                    //   🔴 **2026-10-17（`T08`）**：期望值的键跟着上一条换成**兜底键**
                    //     `MenuDeck/Error/InvalidDeck`（它**在**表里 ⇒ ①②③ 三条都仍有鉴别力）。
                    string wantMsg = Loc.T("MenuDeck/Error/InvalidDeck");
                    Check(pop.MessageShown, wantMsg,
                          $"★ ……而且**画出来的是那条键的词条**（`A1012` 起 `Term()` 先查 `Terms`、再转 `Loc`；"
                        + $"当前语档下 =「{wantMsg}」）");
                    CheckTrue(!string.IsNullOrEmpty(pop.MessageShown) && pop.MessageShown != pop.MessageKey,
                          "★ 灭自证：正文印的**不是键名本身**（键名 = " + pop.MessageKey + "）"
                        + "｜🧨 把 `Term()` 末句改回 `return key ?? \"\"` ⇒ 这条红");
                    // 左钮：**拿哪个键**（原版 `LiveButtons[0]` = `ButtonLeft`）+ **画出来的是词条**
                    string wantLeft = Loc.T("MainMenu/General/Discard");
                    Check(pop.PrimaryKey, "MainMenu/General/Discard",
                          "★ **左**钮**拿的键** = `MainMenu/General/Discard`（原版 `LiveButtons[0]` = `ButtonLeft`）");
                    Check(pop.PrimaryShown, wantLeft,
                          $"★ ……画出来的是**那条键的词条**（当前语档下 =「{wantLeft}」）");
                    CheckTrue(!string.IsNullOrEmpty(pop.PrimaryShown) && pop.PrimaryShown != pop.PrimaryKey,
                          "★ 灭自证：左钮印的**不是键名本身**｜🧨 改回 `return key ?? \"\"` ⇒ 红");
                    // 右钮（对称）
                    string wantRight = Loc.T("MainMenu/General/Cancel");
                    Check(pop.SecondaryKey, "MainMenu/General/Cancel",
                          "★ **右**钮**拿的键** = `MainMenu/General/Cancel`");
                    Check(pop.SecondaryShown, wantRight,
                          $"★ ……画出来的是**那条键的词条**（当前语档下 =「{wantRight}」）");
                    CheckTrue(!string.IsNullOrEmpty(pop.SecondaryShown) && pop.SecondaryShown != pop.SecondaryKey,
                          "★ 灭自证：右钮印的**不是键名本身**｜🧨 改回 `return key ?? \"\"` ⇒ 红");

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
                    //   `ModalPopupOpen` 那道闸挡着）⇒ **它今天没有生产可达路径**。
                    //   🔴 **2026-10-15（A535）就地改向**（铁律 5）：这句原来接着写「**下面也不再钉它**（本件如实记账）」
                    //   —— **那半句已过期**：本件按调度台裁定「做」把覆盖补上了 ⇒ 新口 `DeckRuntime.UiPressDone()`
                    //   ＋ 下面那一节 **⑦·b**（同一入口两种状态：不合法 ⇒ 窗留着 / 合法 ⇒ 窗收掉）。
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

                    // ---- ⑦·b 🔴 **2026-10-15（A535）**：`SaveAndSay()` **成功支**那句 `HideDeckPopUp()` 的覆盖 ----
                    //   ⚖️ 调度台 2026-10-14 裁定「**做**」（`资料/历史/A表已收口_1014.md` §四）：用户的
                    //   「写盘失败族＝只出声」管的是**失败语义**，不管**成功支的覆盖** ⇒ 照同族先例补直调口。
                    //   为什么只能直调：`Done` 钮被 `HandlePointer` 的 `ModalPopupOpen` 闸挡着（`DeckRuntime.cs`
                    //   的 `if (ModalPopupOpen) return;`）、ESC 被 `EscPressed()` ③ 那一级吃掉 ⇒ 生产上**没有一条路**
                    //   能「窗开着 + 卡组合法」走到成功支（原版那一刻同样轮不到）。
                    //   🔴 **两种状态（缺一不可 —— 弱断言分不出两态就是不合格）**：**同一个入口**、只差卡组合不合法——
                    //     · **不合法** ⇒ 弹窗那一支 ⇒ **窗留着**（顺带证明这一按真的落到了 `SaveAndSay()` 上）
                    //     · **合法**   ⇒ 成功支   ⇒ **窗收掉**（= 原版 `__TrySaveDeck.c:103 HidePopUp`）
                    //   ⛔ 改坏法（两条都带电）：① 删掉 `SaveAndSay()` 成功支那句 `HideDeckPopUp()` ⇒ 态 B 红；
                    //     ② 把 `UiPressDone()` 写成「自己收窗」的假口（不调 `SaveAndSay()`）⇒ 态 A 红
                    //     （不合法那一按也会把窗收掉）**且**态 B 的「盘上换了名字」那条红（根本没写盘）。
                    string disk535 = DeckLibrary.ExportString(DeckLibrary.Load().Current);
                    CheckTrue(!_rt.ModalPopupOpen,
                              "（前提）⑦ 收尾之后**没有**模态窗（⛔ 这一条红先去看 ⑦ 那句 `if (pop3 != null)` "
                            + "是否进去过 —— 那不是 A535 的问题）");
                    string taken535 = live.CardIds[0];
                    live.CardIds.RemoveAt(0);                        // 30 → 29 ⇒ `TooFewCards`
                    _rt.UiScrollPool(0f);
                    Check(_rt.State.Validate(), DeckError.TooFewCards, "（前提）再摘一张 ⇒ 不合法");
                    _rt.UiPressDone();                               // ★ 直调 Done（绕过命中 + 两道模态闸）
                    CheckTrue(_rt.ModalPopupOpen,
                              "★ A535·态 A：**不合法**时直调 Done ⇒ 走弹窗那一支、**窗开着**"
                            + "（这一按要真的落到 `SaveAndSay()` 上；被那道闸挡掉 / 被挪去别的分支 ⇒ 这条红）");
                    live.CardIds.Insert(0, taken535);                // 把窗**开着**修回合法
                    _rt.UiScrollPool(0f);
                    Check(_rt.State.Validate(), DeckError.None, "（前提）补回一张 ⇒ 又合法（此刻窗还开着）");
                    live.Name = keepName + "·A535 成功支";           // 🔴 判别式：盘上没有这个名字
                    _rt.UiPressDone();                               // ★ 同一入口，这一次落**成功支**
                    Check(DeckLibrary.Load().Current.Name, keepName + "·A535 成功支",
                          "★ A535·态 B：直调 Done ⇒ 真的走了**成功支**（合法 ⇒ `CommitDeck()` 写盘）"
                        + " —— 没写盘 ⇒ 这条红（「只收窗」的假口也逃不掉）");
                    CheckTrue(!_rt.ModalPopupOpen && _rt.ModalPopup == null,
                              "★ A535·态 B：……而且成功支那句 `HideDeckPopUp()`（原版 `__TrySaveDeck.c:103`）"
                            + "**把窗收掉了** —— 删掉那一句 ⇒ 窗留着 ⇒ **这条红＝A535 要恢复的那条覆盖**");
                    live.Name = keepName;                            // 收尾：名字还回去（后面几节接着用这副牌）
                    _rt.UiPressDone();
                    Check(DeckLibrary.ExportString(DeckLibrary.Load().Current), disk535,
                          "（收尾）盘上逐字节复原（≡ A565 那节的收尾写法）");
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
                    // 🆕 **2026-10-15（A569·S1）**：本处是 5 个 `ShowPopUp` / `ShowMessagePopUp` 夹具站点里
                    //   **唯一拿返回值的**那个（另外 4 处从 `shell.Windows.popUpWindow` 重取 —— 判据与那两条★断言
                    //   → `Editor/ShellScene.cs` 的 A569·S1 那一节）。补这条前提的理由有两条：
                    //   ① 下面第一句就是 `one.name` —— 交回 `null` 的话那不是「红」而是**当场 NRE 崩**
                    //      （后面整片都不跑，比红更糟）；
                    //   ② 返回值 = 本题**唯一**的窗句柄 ⇒ ⛔ 别拿「上一拍那一扇」顶替：关过之后再开是**新建**
                    //      （原版 `CloseWindowCO` 是 `Destroy`），旧句柄还活着、却不是当前那一扇。
                    CheckTrue(one != null,
                              "（前提·A569·S1）`ShowMessagePopUp` **交回了一扇**"
                            + "（它的返回值就是本处唯一的窗句柄）");
                    if (one != null)
                    {
                        Check(one.name, "MessagePopupWindow",
                              "★ 只给一颗钮 ⇒ 建的是**1 按钮版** `MessagePopupWindow`（原版 `popUpWindowOneButton`）");
                        // 🔴 **2026-10-19（A1012）就地改口径（铁律 5）**：原来断「那一颗的字 = 给的那个键」
                        //   —— `Term()` 转 `Loc` 之后印的是**词条**。拆成「拿的键」+「画出来的词条」+「灭自证」。
                        string wantOne = Loc.T("MainMenu/General/Cancel");
                        Check(one.PrimaryKey, "MainMenu/General/Cancel",
                              "★ 那一颗**拿的键** = 调用点给的那个（原版那颗钮也是拿键去查表）");
                        Check(one.PrimaryShown, wantOne,
                              $"★ 那一颗**画出来的字** = 那条键的词条（当前语档下 =「{wantOne}」）");
                        CheckTrue(!string.IsNullOrEmpty(one.PrimaryShown) && one.PrimaryShown != one.PrimaryKey,
                              "★ 灭自证：印的**不是键名本身**｜🧨 把 `Term()` 末句改回 `return key ?? \"\"` ⇒ 红");
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
                // 🔴 **2026-10-18（`A1031③`）就地改「期望值随语档」**：这里原来写死字面量 `"新卡组"`
                //   —— 那**只是中文列的副本**（实现早就走词条了：`Deck/DeckRuntime.cs` 清名那一跳是
                //   `State.SetDeckName(Loc.T("MenuDeck/NewDeckName"))`）⇒ **换到英文档这条必红**；
                //   而中文档下期望值又**恰好等于**实现里那个默认串 ⇒ 两边一起看就是**自证**
                //   （期望值从实现的口里来，实现改坏了它跟着改）。
                //   ⇒ 期望值改成 `Loc.T(键)`（同形现成写法 = `Editor/CollectionScene.cs` 的
                //     `HasEntry` + `Loc.T(键)` 那一对）。判据 = `Core/Loc.cs` 那条
                //     `MenuDeck/NewDeckName`（EN `New deck` / ZH `新卡组`）。
                CheckTrue(Loc.HasEntry("MenuDeck/NewDeckName"),
                          "（前提）词条 `MenuDeck/NewDeckName` 在表里"
                        + "（⛔ 不在 ⇒ `Loc.T` 回的是**键名本身** ⇒ 实现印键名、期望也是键名 = **假绿**）");
                Check(live.Name, Loc.T("MenuDeck/NewDeckName"),
                      "……名字被写成**词条** `MenuDeck/NewDeckName`（期望值**随语档**，"
                    + $"当前语档下 =「{Loc.T("MenuDeck/NewDeckName")}」）");
                // 🧨 **灭自证**：同一条键在两档下**各是各的** ⇒ 旧断言那个字面量「新卡组」
                //   **只在中文档成立**（改前正是如此 = 单语档断言）。这里断的也不是「实现里那个字面量」，
                //   而是这一条**键**的英文列 —— 实现改成写死常量时它不会跟着变。
                {
                    var dkLangWas = Loc.Current;
                    Loc.RestoreForTest(AvailableLanguages.English);
                    string dkEn = Loc.T("MenuDeck/NewDeckName");
                    Loc.RestoreForTest(dkLangWas);                       // 收尾：还给本节剩下的语档
                    CheckTrue(!string.IsNullOrEmpty(dkEn) && dkEn != "新卡组"
                              && dkEn != "MenuDeck/NewDeckName",
                              $"★ **灭自证**：同一条键换到英文档是**另一句**（「{dkEn}」）"
                            + " ⇒ 期望值写成字面量「新卡组」只在中文档成立（改前 = **单语档断言**）；"
                            + " 而它**不是键名** ⇒ 词条真的在表里（上面那条 `HasEntry` 不是装饰）");
                }
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

            // 分享（后端 `DeckLibrary.ExportString`，原版格式）
            // 🔴 **2026-10-17（T23 删件）**：这里原来还有一整段「导入弹窗：开 → 贴串 → `TryImport` →
            //   关」+「乱串被挡」，以及一张 `deck_import.png` 截图 —— 导入弹窗那棵树整棵删了
            //   （它是 `Shell/ImportDeckPopup.cs` 的重复实现），那一段随之删。
            //   导入那条链的自检在 `CollectionScene`（`CollectionData.ImportDeck`，A503/A547 那一份）。
            var str = _rt.UiShareString();
            CheckTrue(str.Length > 0, $"分享：导出卡组串（{str.Length} 字符）");

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
            // 🔴 **2026-10-12（`A1698`）**：**开的必须是【菜单版】那一扇** —— 原版卡组编辑
            //   `DeckEditingWindow__OpenCardInformation.c:56-58` 硬编码传 `showOptions = 1`，
            //   而 `CardDisplayWindow__ShowCard.c:159` 的守卫过后会 `:163 SetActive(1)` 那个 `options` 子树
            //   （= 右侧那一列：创建副本 / 升级 / 异画 / 计数 / 通配符）。
            //   ⛔ **改坏法**：把 `DeckRuntime.OpenCardWindow` 改回 `CardDisplayWindow.Create(Root)`
            //   （战斗版摆放，`options = null`，13/13 竞技场都是）⇒ 右侧那一列一件都不建 ⇒ **这条红**。
            //   ⚠️ 数的是这三个节点：`Card Counter` / `Wildcard Count 0` / `AltArt Title`
            //     （前两个永远建；第三个“建了但可能关着”—— 原版 `AlternateArtPanel__Initialize.c:33`）。
            CheckTrue(_rt.UiCardWindowRightColumn >= 3,
                      "★ A1698：卡组编辑点卡开的是**菜单版**详情窗（原版 `showOptions=1`）⇒ 右侧那一列在"
                    + "（实得 " + _rt.UiCardWindowRightColumn + " 件）");
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
            //      实读都是 **0**（`Shell/PopUpGameWindow.cs`）⇒ 那一刻**什么都不做**。
            //  ⇒ 原版此刻 `DeckEditingWindow.ESCPressed`（= 保存）**根本轮不到**。
            //  ⚠️ **硬约束**（⛔ 别一刀切）：判别式必须让「ESC 什么都不做」与「ESC 落到保存」**分岔** ——
            //     只有在**卡组合法 + 脏**时才带电
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
            //
            // 🔴 **2026-10-19（批次 D · 红 #1–#3）夹具改成「找一格非战将」**：
            //   本批卡池改成原版显示序（`DeckEditorState.SortForDisplay`：`hero` 先）后，**每阵营第 0 格恒为战将**；
            //   而 `CardView.ShowsCost` = `cost >= 0 && type != "hero"`（判据 = 原版 `BasicCardUI__SetRawCardData.c:84`
            //   把费用控件 `SetActive(false)`）⇒ 战将的 `StatAt` 对费用恒答 `StatNone`、`TickTooltipAt` 返回 false。
            //   ⇒ **「战将没有费用 tooltip」是正确行为**，过期的是旧夹具「`pool_0` 必有费用」（改前那一版 `pool_0` 是 `Autarch`）。
            //   ⚠️ ⛔ 别把排序改回去迁就夹具；也⛔ 别退回固定 `pool_0`。
            Tooltip.Hide(); Tooltip.FinishFade();
            int tipSlot = -1;
            CardView tipView = null;
            CardDef tipDef = null;
            for (int i = 0; i < _rt.UiPoolSlotCount; i++)
            {
                var d = _rt.UiPoolCardAt(i);
                if (d == null || string.Equals(d.Type, "hero", StringComparison.Ordinal) || d.Cost < 0) continue;
                var g = PoolCellGo(i);
                if (g == null || !g.activeSelf) continue;          // `TickTooltipAt` 只问活着的格（见它那句 `activeSelf`）
                var cv = g.GetComponent<CardView>();
                if (cv == null) continue;
                tipSlot = i; tipView = cv; tipDef = d; break;
            }
            CheckTrue(tipView != null,
                      "卡池里找得到一格**非战将且有费用**的卡来做费用 tooltip 夹具"
                    + $"（每阵营第 0 格是战将、它没有费用 —— 见 `ShowsCost`；当前 {_rt.UiPoolSlotCount} 个可见槽）");
            if (tipView != null)
            {
                var pv = tipView;
                CheckTrue(_rt.TickTooltipAt(pv.StatWorld(CardView.StatCost)),
                          $"把指针放到卡池卡（第 {tipSlot} 格 `{tipDef.Name}`）的**费用**上 → tooltip 显示");
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
                    // 🔴 **2026-10-18（`N55`）**：口径由静态 Cjk 口改成按 `ActualFace` —— 本族（筛选单元格）
                    //   在 `Deck/DeckRuntime.cs` 的 `LatinCardBody` 显式 `SetFace(LatinCardBody)`；`EmPerFontSize`
                    //   自本日起按面分两支（CJK 0.0948 / 拉丁 0.1），静态口量它会恒差 5.2%。判据 → R4 §五·B。
                    CheckNear(typeLb.FontSizeToPxNow(typeLb.FontSizeMax, typeLb.ActualFace), 27f, 0.6f,
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
                    CheckNear(rarLb.FontSizeToPxNow(rarLb.FontSizeMax, rarLb.ActualFace), 27f, 0.6f,
                              "★ A407：稀有度族自适应**上限 = 原版 27**（`auto[10~27]`；旧写法 = 标称 23.2 ⇒ 矮 3.8px）");
                    CheckNear(rarLb.FontSizeToPxNow(rarLb.FontSizeBase, rarLb.ActualFace), 36f, 0.6f,
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
                    // 🔴 **2026-10-18（`N55`）**：口径按 `ActualFace`（卡背筛选格也是 `LatinCardBody`，判据同上）
                    CheckNear(cosmoLb.FontSizeToPxNow(cosmoLb.FontSizeMin, cosmoLb.ActualFace), 26f, 0.6f,
                              "★ 卡背页 `'Owned only'` 自适应**下界 = 原版 26**（`auto[26~32]`；"
                              + "⛔ 不是收藏窗那份 18 —— 退回共用常量这里必红）");
                    CheckNear(cosmoLb.FontSizeToPxNow(cosmoLb.FontSizeMax, cosmoLb.ActualFace), 32f, 0.6f,
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

            // ======== 🔴 **2026-10-17（T23 删件）：A547 那一节整段删掉了** ========
            //   它验的是 `DeckRuntime.TryImport` 的「落盘失败说得出话 + 不许清脏标记」—— 那个方法随导入弹窗
            //   整段删了（生产那份是外壳的 `Shell/CollectionData.ImportDeck`，A503/A547 的同一组断言在
            //   `CollectionScene` 里，判据同一条出口 `DeckLibrary.LastError`）。
            //   ⛔ 别照旧写回一份「给 `DeckRuntime` 再造一个导入口」的夹具 —— 那是把删掉的重复实现又请回来。

            // ============================================================ 🆕 **2026-10-18（`G8`）：卡组存档那一层
            //  **出【错误码】、不出「给人看的中文」** —— 两语档各断一次
            //
            //  背景（`G3` 交回的那半）：`DeckStore.LoadAll` 的三条出口原来是**写死的整句中文**
            //  （`DeckStore.cs` 的 `Contains("失败")`），`DeckLibrary` 只能拿 `Contains("失败")` 去猜类型
            //  ⇒ 换语言 / 改一次措辞就**静默失效**。本笔把那三条换成 `LoadNote` **错误码** +
            //  一句**诊断串**；给人看的文案走**词条**（`CustomErrors/ErrorLoadDeck` /
            //  `MenuDeck/Error/SaveFailed` —— 两条的载体与出处写在 `Core/Loc.cs` 上）。
            //
            //  🔴 **判据都是结构性的 / 原版的，不从实现里读回来**：
            //    · 错误码 = `DeckStore.LoadNote` 的枚举值（三条出口各一个）；
            //    · 「不产中文」= `Loc.HasCjk(detail)` 为假（`Loc.HasCjk` 是**唯一一份**汉字判据）；
            //    · 「换语言不出错」= **两档各跑一遍**：类型与诊断**逐字不变**、显示文案**逐字不同**；
            //    · 「显示层真的走词条了」= 那句人话里**含 `Loc.T(键)` 在当前语档下的值**。
            //  🧨 改坏法：把 `DeckStore` 那三条改回写死中文 ⇒ ① 那三条红（`Loc.HasCjk` 与 `StartsWith`）；
            //     把 `DeckRuntime.SaveFailReason()` 改回写死中文 ⇒ ③ 的**英文档**那条红。
            Section("G8：卡组存档出【错误码】不出「给人看的中文」（两语档）");
            {
                string keepPath = RuleEngine.DeckStore.OverridePath;
                string probeDir = System.IO.Path.GetDirectoryName(keepPath);
                CheckTrue(!string.IsNullOrEmpty(probeDir) && System.IO.Directory.Exists(probeDir),
                          $"（前提）探针目录存在（{probeDir}）—— 不存在 ⇒ 下面全是假绿");
                string p = System.IO.Path.Combine(probeDir, "_g8_deckload.json");
                var langWas = Loc.Current;
                try
                {
                    RuleEngine.DeckStore.OverridePath = p;

                    // ---- ① 三条出口各一个【错误码】；`detail` 是诊断串（不是中文整句） ----
                    //  ⚠️ `Empty` / `ReadFailed` 两条走的都是 `DeckStore` **自己拼的**那种串
                    //     ⇒ 可以逐条钉形状；`ReadFailed` 那一条**还缀着异常自己的话**（.NET/Unity 给的，
                    //     OS 是中文时也可能是中文）⇒ 对它只钉**前缀**（那正是我们写的那一段）。
                    try { if (System.IO.File.Exists(p)) System.IO.File.Delete(p); } catch { }
                    RuleEngine.DeckStore.LoadNote code; string detail; int cur;
                    RuleEngine.DeckStore.LoadAll(out code, out detail, out cur);
                    Check(code, RuleEngine.DeckStore.LoadNote.NoSaveFile, "①-a 没有存档 ⇒ 码 `NoSaveFile`");
                    CheckTrue(!string.IsNullOrEmpty(detail),
                              "①-a ……而且 `detail` **非空** —— 那是「`DeckStore` 有没有话要说」这个信号，"
                            + "改成 null 会把「第一次跑」**静默**判成「读到了」（`ClassifyLoad` 就是这么看的）");
                    CheckTrue(!Loc.HasCjk(detail),
                              $"★ ①-a ……而且它**一个汉字都没有**（实际「{detail}」）"
                            + "｜🧨 改回写死「还没有存档」⇒ 红");

                    System.IO.File.WriteAllText(p, "{ \"version\": 1, \"current\": 0 }");  // 合法 JSON、缺 `decks`
                    RuleEngine.DeckStore.LoadAll(out code, out detail, out cur);
                    Check(code, RuleEngine.DeckStore.LoadNote.Empty, "①-b 存档在、解析出来是空的 ⇒ 码 `Empty`");
                    CheckTrue(!Loc.HasCjk(detail),
                              $"★ ①-b ……诊断串也没有汉字（实际「{detail}」）"
                            + "｜🧨 改回写死「存档解析失败（内容为空）」⇒ 红");

                    //  ⚠️ **语法坏掉那段文本**不写死判哪一支：Unity 的 `JsonUtility.FromJson` 是**抛异常**
                    //     还是**回 null** 取决于它（两条都落 `Failed`，但**错误码不同**）⇒ 这里断言
                    //     「两个码里必居其一」+ 各自该有的形状（⛔ 别把一条**不一定成立**的写死）。
                    System.IO.File.WriteAllText(p, "{ 这不是 json ");
                    RuleEngine.DeckStore.LoadAll(out code, out detail, out cur);
                    CheckTrue(code == RuleEngine.DeckStore.LoadNote.Empty
                              || code == RuleEngine.DeckStore.LoadNote.ReadFailed,
                              $"①-c 语法坏掉的存档 ⇒ 码是 `Empty` 或 `ReadFailed`（实得 `{code}`）");
                    if (code == RuleEngine.DeckStore.LoadNote.ReadFailed)
                        CheckTrue(detail != null && detail.StartsWith("read failed:"),
                                  $"★ ①-c ……走 `ReadFailed` 那一支：诊断串前缀是我们写的那一段（实际「{detail}」）"
                                + "｜🧨 改回写死「存档读取失败：」⇒ 这条红（老前缀不是这个）");
                    else
                        CheckTrue(!Loc.HasCjk(detail),
                                  $"★ ①-c ……走 `Empty` 那一支：诊断串没有汉字（实际「{detail}」）");

                    // ---- ② 中英两档：**类型与诊断逐字不变**、**显示文案逐字不同** ----
                    //  盘上放一段**确定性**的坏存档（合法 JSON、缺 `decks` 键）—— 两档各 `Load()` 一次。
                    //  🔴 **2026-10-18 就地订正（铁律 5 · 独立审查 `REV_W_四写手.md` F7 抓的）**：
                    //    这里原来写「缺 `decks` 键 ⇒ **`JsonUtility` 给 null** ⇒ 必落 `Empty` 那一支」——
                    //    **那个理由是错的**（`DA_DeckLibrary分类13红.md` §1 证伪）：`JsonUtility` 对缺键的
                    //    集合字段给的是**空表、不是 `null`** ⇒ 旧判据 `dto.decks == null` **恒假**、
                    //    这一支**从没被走到过**（死代码）。`WD` 当天改成**按原文有没有 `decks"` 键**判（结构性）
                    //    ⇒ 这段坏存档才**真的**落 `Empty`。⚠️ **断言没错、错的是这句理由**，故只改注释。
                    System.IO.File.WriteAllText(p, "{ \"version\": 1, \"current\": 0 }");
                    Loc.RestoreForTest(AvailableLanguages.Chinese);
                    var libZh = RuleEngine.DeckLibrary.Load();
                    Check(libZh.LastLoadIssue, RuleEngine.DeckLibrary.DeckLoadIssue.Failed,
                          "②-a 中文档：坏存档 ⇒ 类型 `Failed`");
                    Check(libZh.LastLoadCode, RuleEngine.DeckStore.LoadNote.Empty,
                          "②-a ……错误码 = `Empty`（比类型细一档 —— 两个都得有，显示层挑词条用码）");
                    string zhTerm = libZh.LastLoadIssueTerm, zhDiag = libZh.LastError;
                    string zhText = Loc.T(zhTerm);

                    Loc.RestoreForTest(AvailableLanguages.English);
                    var libEn = RuleEngine.DeckLibrary.Load();
                    Check(libEn.LastLoadIssue, RuleEngine.DeckLibrary.DeckLoadIssue.Failed,
                          "★ ②-b **英文档**：同一件读不出的存档**照样** `Failed` —— 类型不随语言变"
                        + "｜🧨 判据换回 `note.Contains(\"失败\")` ⇒ 这里变 `None` ⇒ 红");
                    Check(libEn.LastLoadCode, RuleEngine.DeckStore.LoadNote.Empty,
                          "②-b ……错误码也不随语言变");
                    string enTerm = libEn.LastLoadIssueTerm, enDiag = libEn.LastError;
                    string enText = Loc.T(enTerm);

                    CheckTrue(Loc.HasEntry(zhTerm ?? ""),
                              $"②-c 词条键在表里（`{zhTerm}`）—— ⛔ 坑表 #18：「原版有词条」≠「我们表里有键」，"
                            + "两条都要断。键不在 ⇒ `Loc.T` 返回键名本身、界面上印键名");
                    CheckTrue(!string.IsNullOrEmpty(zhTerm) && zhTerm == enTerm,
                              $"②-c 词条键**两档同一个**（键不随语言变，只有值变）—— 实测「{zhTerm}」/「{enTerm}」");
                    CheckTrue(zhDiag != null && !Loc.HasCjk(zhDiag) && zhDiag == enDiag,
                              "★ ②-d **灭自证**：诊断串**非空**、**逐字不随语档变**、且不含汉字 —— "
                            + "证明「状态」没跟着语言走（它压根不该跟着走）。"
                            + "⛔ 少了「非空」那半，`null == null` 会让这条**假绿**");
                    CheckTrue(zhText != enText && zhText.Length > 0 && enText.Length > 0,
                              $"★ ②-e 显示文案**两档逐字不同**（中「{zhText}」/ 英「{enText}」）—— "
                            + "这一条把「键在表里且真有两列」钉住");

                    // ---- ③ 落盘失败那条**人话**随语档换（= 显示层真的改走词条了） ----
                    //  走**生产那条路**（`UiPressDone()` → `SaveAndSay()`），不另造出口。
                    if (_rt.State.Validate() != DeckError.None)
                    {
                        Check(true, false, $"（前提）③ 需要一副**合法**卡组（此刻是 `{_rt.State.Validate()}`）"
                                         + " ⇒ 这一段没法验（红 —— 别让它静默跳过）");
                    }
                    else
                    {
                        string badPath = System.IO.Path.Combine(probeDir, "__wf_g8_no_such_dir__", "x.json");
                        RuleEngine.DeckStore.OverridePath = badPath;
                        Loc.RestoreForTest(AvailableLanguages.Chinese);
                        string wantZh = Loc.T(DeckRuntime.TermSaveFailed);
                        _rt.UiPressDone();
                        string sayZh = _rt.UiLastSay ?? "";
                        Loc.RestoreForTest(AvailableLanguages.English);
                        string wantEn = Loc.T(DeckRuntime.TermSaveFailed);
                        _rt.UiPressDone();
                        string sayEn = _rt.UiLastSay ?? "";

                        CheckTrue(Loc.HasEntry(DeckRuntime.TermSaveFailed),
                                  $"③-a 词条键在表里（`{DeckRuntime.TermSaveFailed}`）");
                        CheckTrue(wantZh != wantEn && wantZh.Length > 0 && wantEn.Length > 0,
                                  $"③-a ……而且两档逐字不同（中「{wantZh}」/ 英「{wantEn}」）");
                        CheckTrue(sayZh.Contains(wantZh),
                                  $"★ ③ 中文档：落盘失败那句人话 = `Loc.T(键)`（实测「{sayZh}」）");
                        CheckTrue(sayEn.Contains(wantEn),
                                  $"★ ③ **英文档**：**同一处**跟着换成英文（实测「{sayEn}」）—— "
                                + "即 `SaveFailReason()` 的人话那一半真的走词条了"
                                + "｜🧨 把它改回写死「写不进存档文件」⇒ **只有这条红**（中文档那条照样绿）");
                    }
                }
                finally
                {
                    RuleEngine.DeckStore.OverridePath = keepPath;
                    try { if (System.IO.File.Exists(p)) System.IO.File.Delete(p); } catch { }
                    Loc.RestoreForTest(langWas);                 // ⛔ 自检一个字节都不写 `PlayerPrefs`
                    _rt.CommitDeck();                            // 把 ③ 留下的脏标记与 `LastError` 收回去
                    CheckTrue(!_rt.DeckDirty && _rt.Library.LastError == null,
                              "（收尾）路径恢复之后那一下真的落得下去（脏标记清、`LastError` 空）");
                }
            }

            // ============================================================ 🆕 2026-10-17（G1）
            // **卡组编辑整条线的差异账**（判据正本 → `资料/普查产出_1017/对账_卡组编辑部分_差异与待办.md` D 系列）。
            // 🔴 口径：每条期望值都是**原版判据**（预制体字段 / 反编译 / SO 字面量），
            //    ⛔ 一处也不从 `DeckRuntime` 自己的常量读回来（那是自证）；
            //    **删件那几条一律配反向断言**（「它不在了」—— 节点与点击矩形两样都查）。
            Section("G1：差异账（删件 / 参数 / 顶栏 / 小屏 / 配色）");
            {
                _rt.UiSetTab(0);

                // ---- ① D36–D38：**调用侧**用的是哪一套格距 ----
                //   上面那 10 条钉的是**模型**（两套形参各跑一遍）；这一组钉的是 `RefreshFilterCells`
                //   真的把「卡组编辑那一套」传进去了 —— 量的是它**真建出来**的那批格。
                //   期望值 = 原版那三个字面量算出来的**绝对 x1**（面板原点 **2.2** + 面板内 x1）：
                //     Rarity `2.2 + 14 + 2×(100+5) = 226.2` · Cost `2.2 + 15 + 2×(65+7) = 161.2`
                //     · Type `2.2 + 40 + 80 = 122.2`（⛔ 不是拿 `FilterPanelModel.RaritySpXDeckEdit` 算的）。
                {
                    bool wasOpen = _rt.FiltersOpen;
                    if (!wasOpen) _rt.UiToggleFilters();
                    CheckTrue(_rt.FiltersOpen, "（前提）筛选栏打开（关着时 `_fltCells` 是空的，读不到格子）");
                    float cx, cy, cw, ch; bool on;
                    CheckTrue(_rt.UiFilterCell("$rar:epic", out cx, out cy, out cw, out ch, out on),
                              "（前提）量得到 `$rar:epic`（Rarity 第 3 列）");
                    if (_rt.UiFilterCell("$rar:epic", out cx, out cy, out cw, out ch, out on))
                        CheckNear(cx - cw * 0.5f, 226.2f, 0.6f,
                                  "★ D36：**卡组编辑真建出来**的 Rarity 第 3 列 x1 = **226.2**"
                                + "（列距 5 ⇒ 步进 105；传成收藏窗那套会落在 230.2）");
                    if (_rt.UiFilterCell("$cost:3", out cx, out cy, out cw, out ch, out on))
                        CheckNear(cx - cw * 0.5f, 161.2f, 0.6f,
                                  "★ D37：Cost 第 3 列 x1 = **161.2**（列距 7 ⇒ 步进 72；收藏窗那套是 177.2）");
                    else Check(true, false, "量不到 `$cost:3`（Cost 第 3 桶）");
                    if (_rt.UiFilterCell("$type:unit", out cx, out cy, out cw, out ch, out on))
                        CheckNear(cx - cw * 0.5f, 122.2f, 0.6f,
                                  "★ D38：Type 第 2 格 x1 = **122.2**（`m_Padding.Left = 40`；收藏窗那套是 97.2）");
                    else Check(true, false, "量不到 `$type:unit`（Type 第 2 格）");
                    if (!wasOpen) _rt.UiToggleFilters();     // 收回去：本节不许改后面用例的初态
                }

                // ---- ② D48：费用曲线两列数字的**字色**（原版两颗 TMP 的 `m_fontColor` 不同）----
                {
                    _rt.UiSetTab(1);
                    var lbT = FindDeep(_rt.Root, "curve_l0");
                    var nuT = FindDeep(_rt.Root, "curve_n0");
                    var lbC = lbT != null ? lbT.GetComponent<Label>() : null;
                    var nuC = nuT != null ? nuT.GetComponent<Label>() : null;
                    CheckTrue(lbC != null && nuC != null, "（前提）两列数字都找得到（`curve_l0` / `curve_n0`）");
                    if (lbC != null)
                        CheckNear(lbC.color.g, 0.77358f, 0.002f,
                                  "★ D48：左列 `Card Cost` 的字色 = **绿 (0.29557, 0.77358, 0.49591)**"
                                + "（判据 = 那颗 TMP 的 `m_fontColor`，`MB 7891025530621652772`；"
                                + "两列同色 ⇒ 这条红）");
                    if (nuC != null)
                        CheckNear(nuC.color.r + nuC.color.g + nuC.color.b, 3f, 0.01f,
                                  "★ D48：右列 `Cards in deck` 的字色 = **白 (1,1,1)**"
                                + "（判据 = `MB 871731656867376932`）");
                    if (lbC != null && nuC != null)
                        CheckTrue(Mathf.Abs(lbC.color.g - nuC.color.g) > 0.1f,
                                  "★ D48 判别式：**两列的颜色必须不是同一个**（同一份常量 ⇒ 这条红）");
                    _rt.UiSetTab(0);
                }

                // ---- ③ D33：`Card Cost` 最后一格是 **`8+`**，而且那一格的计数 = 费用 ≥ 8 的张数 ----
                //   判据（反编译）：`DeckCostQuantityRowDrawer__Initialize.c:99-109` —— 后缀加在
                //   **`cardCost`（= `Card Cost`，左列）** 那颗 TMP 上、条件是 `== maxCostToDraw(=8)`；
                //   后缀串 `DAT_184275430` 按 RVA `0x4275430` 查 `stringliteral.json` = **`"+"`**。
                //   ⚠️ 施工单 D33 把后缀记在 `Cards in deck` 上 —— **那条是错的**（判据见上）。
                {
                    _rt.UiSetTab(1);
                    Check(_rt.UiLabelText("curve_l8"), "8+",
                          "★ D33：左列最后一格的字 = **`8+`**（后缀 `\"+\"`；写成 `8` ⇒ 红）");
                    Check(_rt.UiLabelText("curve_l7"), "7",
                          "★ D33 正向控制：`8` 那一格**才**带后缀（邻格是裸 `7`）");
                    // 计数那半边：最后一桶 = 费用 ≥ 8 全部（原版把超出的夹进最后一格）。
                    // 同一条判据的另一半 —— 用状态层算出来的期望值（⛔ 不是从界面读回来）。
                    var curve = _rt.State.CostCurve();
                    int n8 = 0;
                    for (int c = 8; c < curve.Length; c++) n8 += curve[c];
                    Check(_rt.UiLabelText("curve_n8"), n8.ToString(),
                          "★ D33：最后一格的计数 = 费用 **≥ 8** 的张数（" + n8 + "；只读 bucket[8] ⇒ 红）");
                    _rt.UiSetTab(0);
                }

                // ---- ④ D34：`Deck Name` 那颗占位符的字号档 / 对齐 ----
                //   判据 = `Placeholder_-4266719589130047708` 的 TMP（`MB -4659515947641016540`）：
                //   `HA 1`(Left) · **`VA 8192`(Capline)** —— ⚠️ 施工单写的「Left/Baseline」**标签是错的**
                //   （`Baseline` 是 `0x800`；`8192 = 0x2000 = Capline`）。
                //   量的是**渲染**：占位符那行字的左沿必须贴着框的左边（居中画会落在框中心）。
                {
                    var hint = FindDeep(_rt.Root, "name_h");
                    var hlb = hint != null ? hint.GetComponent<Label>() : null;
                    CheckTrue(hlb != null, "（前提）找得到 `name_h`（Deck Name 那颗占位符）");
                    if (hlb != null)
                    {
                        Check(hlb.VAlignTier, Label.VAlign.Capline,
                              "★ D34：占位符的垂直档 = 原版 `m_VerticalAlignment = 8192` = **Capline**");
                        // 🔴 **2026-10-18（第三轮整改 · 审查 P4）：这颗占位符的字也走词条了** ——
                        //   键 `MenuDeck/HUD/EditDeckName`（原版那颗 `Placeholder` 的 `mTerm` 原文，
                        //   全库只 1 颗）。期望值 `Loc.T(键)` + 单独钉「键在表里」。
                        //   ⚠️ 它左对齐 ⇒ 文案宽窄变了也不动左沿（下面那条 19.5 仍旧成立）。
                        CheckTrue(Loc.HasEntry("MenuDeck/HUD/EditDeckName"),
                                  "（前提）词条 `MenuDeck/HUD/EditDeckName` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名）");
                        Check(hlb.Text, Loc.T("MenuDeck/HUD/EditDeckName"),
                              "★ D34：占位符文案 = `Loc.T(\"MenuDeck/HUD/EditDeckName\")`（随语档；"
                            + "原版 TMP 原文 `Tap to edit deck name` / 中文「点击以编辑卡组名称」）");   // 2026-10-14：中文列换官方 zh-CN 表
                        float w = hlb.WorldW * PxPerUnit;
                        CheckTrue(w > 20f && w < 2000f, "（前提）占位符量到了真实宽度（" + w.ToString("F1") + "px）");
                        if (w > 20f && w < 2000f)
                        {
                            float left = DeckRuntime.PxOfWorld(hlb.transform.position).x - w * 0.5f;
                            // 帧的左上 = 19.5（`NameTxX`，原版那颗的框；⛔ 这是**几何字面量**、不是实现常量）
                            CheckNear(left, 19.5f, 2.5f,
                                      "★ D34：占位符**贴框的左边**（渲染左沿 " + left.ToString("F1")
                                    + " ≈ **19.5**）—— 还在居中画的话会落在 " + (163.35f - w * 0.5f).ToString("F1")
                                    + "（框中心 163.35 − 半宽）⇒ 红");
                        }
                    }
                }

                // ---- ⑤ 删件（D7 / D9 / D10 / D35 / D44）：**它们都不在了** ----
                {
                    foreach (var k in new[] { "side_title", "pool_info", "pool_info_bg", "store_err",
                                              "notice", "foot_verdict", "cosm_hint",
                                              "info_share", "info_share_bg", "info_share_t",
                                              "info_import", "info_import_bg", "info_import_t" })
                        CheckTrue(FindDeep(_rt.Root, k) == null,
                                  "★ 删件 D7/D9/D10/D35/D44：`" + k + "` **必须不存在**（原版这扇窗里没有它）");
                    foreach (var k in new[] { "info_share", "info_import" })
                        CheckTrue(!_rt.UiBtnRegistered(k),
                                  "★ 删件：`" + k + "` 的点击矩形也不许留在 `_btns` 里（否则「看不见却能点」）");
                }

                // ---- ⑥ 🔴 **2026-10-17（T23 删件）：D45/D46/D47 那一节整段删掉了** ----
                //   它验的全是**导入弹窗**那棵树：压暗层铺满整屏 / 点窗外关窗 / `Confirm` 按 VLG 跑完的位置 /
                //   输入框那行字的 `Left+Top`。那棵树是 `Shell/ImportDeckPopup.cs` 的重复实现、已整棵删掉
                //   （判据 → `资料/普查产出_1017/UI/R_界面线剩余条目现核.md` §二·13 `T23`）。
                //   ⛔ 别把这四条换个节点名又写回来 —— 它们没有被测对象了。

                // ---- ⑦ D23：卡背**缺图要出声** ----
                {
                    int calls0 = _rt.UiCosmeticTexCalls;
                    int warn0 = _rt.UiCardbackArtWarnCount;
                    CheckTrue(_rt.UiCosmeticTex("__g1_probe_no_such_cardback__") == null,
                              "（前提）探针那个名字取不到图");
                    Check(_rt.UiCardbackArtWarnCount, warn0 + 1,
                          "★ D23：卡背**缺图出声**（报过缺图的名字 +1）—— 把那句 `Debug.LogWarning` 删掉 ⇒ 这条红");
                    _rt.UiSetTab(2);
                    CheckTrue(_rt.UiCosmeticTexCalls > calls0,
                              "★ D23：卡背格走的就是**这一条**取图路（换页签铺格会调它）——"
                            + " 另写一条取值路、把告警挂在没人调的岔路上 ⇒ 这条红");
                    _rt.UiSetTab(0);
                }

                // ---- ⑧ D24：卡池的**两套参数**（桌面 6 列 262.5×384 ⇄ 小屏 4 列 393.75×576）----
                {
                    _rt.UiSetSmallScreenUIForTest(false);
                    Check(_rt.UiPoolColsNow, 6, "★ D24（桌面）：列数 = **6**（原版 `floor(1589.8 ÷ 262.5)`）");
                    CheckNear(_rt.UiPoolCellWNow, 262.5f, 0.01f, "★ D24（桌面）：卡位宽 = **262.5**");
                    CheckNear(_rt.UiPoolCellHNow, 384f, 0.01f, "★ D24（桌面）：卡位高 = **384**");
                    // 6 列 × 4 行 = **24 格**（下标 0..23）⇒ 下标 17（第 3 行第 6 列）在桌面上是活的
                    // 🔴 **2026-10-18（A870）**：行数**不再是写死的 3**（= 4 行，判据见 `DeckRuntime.PoolMinCoverage`）
                    //   ⇒ 这一段改的只是「一屏多少格」这个数；下面那两条**灭自证**的形状一个字节没变。
                    CheckTrue(_rt.UiPoolCellRect(17, out float p17x, out float p17y, out float p17w, out float p17h),
                              "（桌面侧）第 18 格（`pool_17`）建出来了（6 列 × 4 行 = 24 格）");
                    _rt.UiSetSmallScreenUIForTest(true);
                    Check(_rt.UiPoolColsNow, 4, "★ D24（小屏）：列数 = **4**（`floor(1589.8 ÷ 393.75)`）");
                    CheckNear(_rt.UiPoolCellWNow, 393.75f, 0.01f, "★ D24（小屏）：卡位宽 = 262.5 × 1.5 = **393.75**");
                    CheckNear(_rt.UiPoolCellHNow, 576f, 0.01f, "★ D24（小屏）：卡位高 = 384 × 1.5 = **576**");
                    // 🔴 **换档之后**：列数 6→4 **且**行数 4→5（A870 起行数也是现算的）⇒ 小屏 = **4 列 × 5 行 = 20 格**（下标 0..19）
                    //   ⇒ 下标 **20..23** 那 4 格必须**收起来**（它们在循环范围之外、自己不关就**留在画面上**）。
                    CheckTrue(!_rt.UiPoolCellRect(20, out float q20x, out float q20y, out float q20w, out float q20h),
                              "★ D24：换到小屏之后**多出来的格子收起来了**（`pool_20` 不再画；"
                            + " 只改列数/行数、不收拾多出来的那 4 格 ⇒ 这条红）");
                    CheckTrue(_rt.UiPoolCellRect(19, out float p11x, out float p11y, out float p11w, out float p11h),
                              "★ D24 正向控制：新的一屏**填满 20 格**（`pool_19` = 第 5 行第 4 列，仍在画）");
                    _rt.UiSetSmallScreenUIForTest(false);      // 收尾：换回桌面档
                    Check(_rt.UiPoolColsNow, 6, "（收尾）回到桌面档 6 列");
                }

                // ---- ⑨ D15：`Content Area/Background` —— 卡池那片双色渐变底（不是纯黑）----
                {
                    CheckTrue(_rt.UiQuadRect("area_bg", out float bx, out float by, out float bw2, out float bh2),
                              "★ D15：`Content Area/Background` 那层**建出来了**（我们原来是纯黑）");
                    if (_rt.UiQuadRect("area_bg", out bx, out by, out bw2, out bh2))
                    {
                        // 原版矩形 = 167.18,70.97 → 1920.00,1080.03（判据 = 预制体 RT 逐字段实读）
                        CheckNear(bx - bw2 * 0.5f, 167.18f, 0.6f, "★ D15：左沿 = **167.18**（原版 RT）");
                        CheckNear(by - bh2 * 0.5f, 70.97f, 0.6f, "★ D15：上沿 = **70.97**（原版 RT）");
                        CheckNear(bw2, 1752.83f, 0.6f, "★ D15：宽 = **1752.83**");
                        CheckNear(bh2, 1009.06f, 0.6f, "★ D15：高 = **1009.06**");
                    }
                }

                // ---- ⑩ D20：过滤器那颗图标 —— **原版就是 `40k_bt_icon_search`**（保持现状）----
                // 🔴 **2026-10-20 就地订正（铁律 5）**：这里原来写着「实拍里显得空是**那张图本身很细**」
                //   —— **与事实不符**。真因是**渲染队列反了**：按钮底 `hdr_fltbtn` 用 `QRow`（= 3007）、
                //   图标 `hdr_flticon` 用 `QBorder`（= 3005）⇒ **按钮底压在图标上面**，而按钮底那张
                //   `40k_menu_bt` 的**中心是不透明的** ⇒ 图标**被整块盖死**（屏上只剩一块底板色）。
                //   `DeckRuntime` 已按本仓血规（**分层要用渲染队列、不能用 z**）把图标改成 `QText - 1`（= 3008）。
                {
                    Check(_rt.UiQuadTex("hdr_flticon"), "40k_bt_icon_search",
                          "★ D20：图名 = **`40k_bt_icon_search`**（原版 `Header/Filters/Icon` 挂的就是它"
                        + " —— ⛔ 别换成「漏斗」）");
                    // 🧨 **抓得住上面那个真因的断言**：比的必须是**渲染队列**、⛔ 不是 z。
                    //   改坏法：把 `hdr_flticon` 的队列改回 `QBorder`（< 按钮底的 `QRow`）⇒ 本条红。
                    CheckTrue(_rt.UiQueueOf("hdr_flticon") > _rt.UiQueueOf("hdr_fltbtn"),
                              "★ D20：**图标的渲染队列 > 按钮底的渲染队列**（实测 "
                            + _rt.UiQueueOf("hdr_flticon") + " vs " + _rt.UiQueueOf("hdr_fltbtn") + "）"
                            + " —— 反了的话按钮底会把图标**整块盖住**（那张底图中心不透明）；"
                            + "⛔ 判据是**渲染队列**、不是 z（透明件按到相机的 3D 距离排序，z 靠不住）");
                }

                // ---- ⑪ D43：卡背抽屉那一行 `Army` 小标题 ----
                {
                    _rt.UiSetTab(2);
                    _rt.UiToggleFilters();
                    var at = FindDeep(_rt.Root, "cosmoflt_title");
                    var al = at != null ? at.GetComponent<Label>() : null;
                    CheckTrue(al != null, "★ D43：卡背抽屉的 `Army` 小标题**建出来了**"
                                          + "（`RefreshCosmoFilters` 那条链原来只调 `BuildCosmetics`）");
                    if (al != null)
                    {
                        // 🔴 **2026-10-18（A891 的续）改「随语档」**：这颗的字从本批起走词条
                        //   （`DeckRuntime.cs` 那处 `Txt("cosmoflt_title", Loc.T("MenuDeck/Filters/Army"), …)`，
                        //   词条键 = 原版那颗 `Localize.mTerm` 的原文）⇒ 宿主跑中文档时写死的 `"Army"` 必红。
                        //   期望值 = 同一个取词口；「键在表里」由上面那条 `HasEntry` 之外**这一条自己带**：
                        CheckTrue(Loc.HasEntry("MenuDeck/Filters/Army"),
                                  "（前提·D43）词条 `MenuDeck/Filters/Army` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名）");
                        Check(al.Text, Loc.T("MenuDeck/Filters/Army"),
                              "★ D43：……文案 = `Loc.T(\"MenuDeck/Filters/Army\")`（随语档；原版 `Army` / `zh_CN.csv:11`「军队」）");
                        Check(al.VAlignTier, Label.VAlign.Middle,
                              "★ D43：……垂直档 = 原版 `VA 512` = **Middle**");
                        float aw = al.WorldW * PxPerUnit;
                        if (aw > 20f && aw < 2000f)
                            CheckNear(DeckRuntime.PxOfWorld(al.transform.position).x - aw * 0.5f, 2.2f, 1f,
                                      "★ D43：……左沿 = **2.2**（原版 `Army Filter/Title` 面板内 x=0 + 面板原点 2.2）");
                        else CheckTrue(false, "（前提）`Army` 小标题量到了真实宽度（实得 " + aw.ToString("F1") + "px）");
                    }
                    _rt.UiToggleFilters();
                    _rt.UiSetTab(0);
                }

                // ---- ⑪·b 🆕 **2026-10-18（第三轮整改 · 审查 P4）：卡背抽屉那条空态** ----
                //   原版 `…/Cosmetic Display/Scroll View/Empty Collection Warning/Warning` 挂
                //   `MenuCollection/NoCardsFound`（与收藏窗 Cards 页**同一条键**，原版 4 颗之一）。
                //   ⚠️ 它出厂 `act=F`（判据 = 过滤后为空）⇒ 这里只断**字**，不断显隐（显隐由 `RefreshCosmoFilters` 管）。
                {
                    var zt = FindDeep(_rt.Root, "cosm_empty_t");
                    var zl = zt != null ? zt.GetComponent<Label>() : null;
                    CheckTrue(zl != null, "★ P4：卡背抽屉那条空态字 `cosm_empty_t` **建出来了**");
                    CheckTrue(Loc.HasEntry("MenuCollection/NoCardsFound"),
                              "（前提）词条 `MenuCollection/NoCardsFound` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名）");
                    if (zl != null)
                        Check(zl.Text, Loc.T("MenuCollection/NoCardsFound"),
                              "★ P4：那条字 = `Loc.T(\"MenuCollection/NoCardsFound\")`（随语档；"
                            + "原版 TMP 原文 `There are no cards in your collection for the selected filters`）");
                }

                // ---- ⑫ 附加条：行上那**两件**按 `CardUIDisplayColorsSO` 上色 ----
                //   判据 = `UICardInfoItem__Initialize.c:139-156`（`GetColor(rarity)` 后逐颗 `set_color`）+
                //   `imagesToChangeColorByRarity` 逐颗解出来是 `Rarity Gradient` / `Background Border`。
                //   🔴 **2026-10-18（`N102`）就地订正**：原来这里写死的是 **`CardRarityColorsSO`** 那六档，
                //     而 prefab 里 **15 颗** `UICardInfoItem` 的 `colorsByRarity` **逐颗**都指向
                //     **`CardUIDisplayColorsSO`**（pid `-2272266055686004801`）⇒ 期望值换成它。
                //     判据 → `资料/普查产出_1018/W3_稀有度色收口.md` §1。
                //   色值**直接写字面量**（= `CardUIDisplayColorsSO` 那六档），⛔ 不从 `RarityColors` 读回来。
                {
                    _rt.UiSetTab(0);
                    var g1RowDef = _rt.UiDeckRowAt(0);
                    CheckTrue(g1RowDef != null, "（前提）卡组列表第 0 行有卡（" + (g1RowDef != null ? g1RowDef.Name : "空") + "）");
                    if (g1RowDef != null && _rt.UiRowRarityTint(0, out Color grad, out Color border))
                    {
                        Color want;
                        switch ((g1RowDef.Rarity ?? "").ToLowerInvariant())
                        {
                            case "common": want = new Color(0.158419371f, 0.281238854f, 0.377358496f, 1f); break;
                            case "rare": want = new Color(0.188660726f, 0.349056602f, 0.074092217f, 1f); break;
                            case "epic": want = new Color(0.415094316f, 0.178177282f, 0.395884871f, 1f); break;
                            case "legendary": want = new Color(0.792452812f, 0.438179821f, 0f, 1f); break;
                            case "special": want = new Color(0.622641504f, 0.072625607f, 0.055802763f, 0.639215708f); break;
                            default: want = new Color(0.509433985f, 0.509433985f, 0.509433985f, 1f); break;
                        }
                        CheckNear(grad.r, want.r, 0.002f,
                                  "★ 附加条：第 0 行色条的 r = `CardUIDisplayColorsSO` 里 `" + g1RowDef.Rarity + "` 那一档（"
                                + want.r.ToString("F5") + "）—— 用错 SO（`CardRarityColorsSO`）⇒ 红");
                        CheckNear(grad.a, want.a, 0.002f, "★ 附加条：……alpha 也照 SO（`common` / `rare` / `epic` / `legendary` 那几档都是 **1**，只有 `special` 是 0.639）");
                        CheckNear(border.r, want.r, 0.002f,
                                  "★ 附加条：**边框那件也上了同一个色**"
                                + "（原版 `imagesToChangeColorByRarity` 那两颗数组元素 = 色条 + 边框；只染色条 ⇒ 红）");
                    }
                    else CheckTrue(false, "量不到第 0 行那两件的 tint（`UiRowRarityTint` 答 false）");
                }

                // ---- ⑬ D13/D14：整条 shell 顶栏 ----
                {
                    var tb = _rt.UiTopBar;
                    CheckTrue(tb != null && tb.Root != null,
                              "★ D13/D14：**顶栏整条建出来了**（我们原来一件都没有）");
                    if (tb != null && tb.Root != null)
                    {
                        // 原版逐件名（判据 = `bundle_scenes_scenes_mainmenuwarpforge` 那棵树）
                        CheckTrue(FindDeep(tb.Root, "Player Profile") != null, "★ D13：`Player Profile` 在");
                        CheckTrue(FindDeep(tb.Root, "Player Name") != null, "★ D13：`Player Name` 在（玩家名那行字）");
                        CheckTrue(FindDeep(tb.Root, "Avatar Item Small") != null, "★ D13：`Avatar Item Small` 在（头像盾框那一格）");
                        CheckTrue(FindDeep(tb.Root, "InboxBtn") != null, "★ D13：`InboxBtn` 在（信封）");
                        CheckTrue(FindDeep(tb.Root, "Challenge button") != null, "★ D13：`Challenge button` 在（人形图标 `40K_icon_duel`）");
                        CheckTrue(FindDeep(tb.Root, "SettingsBtn") != null, "★ D14：`SettingsBtn` 在（齿轮）");
                        CheckTrue(FindDeep(tb.Root, "Resources Bar") != null, "★ D14：`Resources Bar` 在");
                        // 🔴 **2026-10-17（B25①）就地改口径（铁律 5）**：这里原来断的是
                        //    `MainMenuRuntime.DefaultCurrencies.Length`（= **3**）—— 那是「哪几颗**恒显**」那一条判据，
                        //    ⛔ **不是「画出来几格」**。建树收口到 `TopBar.BuildResourcesBar`（与主菜单同一份）之后，
                        //    格数 = **恒显 3 颗 + 拥有 > 0 的**（活动点有记账口 ⇒ `Wallet` 给 9999 > 0 ⇒ 也在）⇒ **4**。
                        //    判据（与主菜单那条**逐字同源**的锚）：`Editor/MainMenuScene.cs` 的
                        //    「★ 生产态 = 4 颗：恒显 3 颗（gold/crystals/blackStones）+ 活动点」那一条。
                        //    ⚠️ 期望值写成**字面量 4**（⛔ 不从实现里算出来 —— 那是同义反复）。
                        int n = 0;
                        foreach (var t in tb.Root.GetComponentsInChildren<Transform>(true))
                            if (t.name == "Resource Counter Item") n++;
                        Check(n, 4, "★ D14：资源计数器 **4** 项（恒显 3 颗 + 活动点「拥有 > 0」）");

                        // 🔴 **🆕 2026-10-17（B25①）：次序 = 原版【建表序】** —— **本件原来就是错在这里**：
                        //    它当时按 `DefaultCurrencies`（= `{gold, crystals, blackStones}`）建 ⇒ 画出来
                        //    **gold·crystals·blackStones**，与原版**正好相反**。判据两条独立：
                        //      ① `CounterSpecs` 的 `List.Add` 序（`GeneralMenuController__Initialize.c:51-146`）；
                        //      ② 用户实拍 `原版参照图/用户实拍_1017/卡组编辑界面参考.png` 右上角
                        //         （左→右 = 水晶 41360 · 黑石 356 · 金币 1500）。
                        //    🔴 本条就是那处偏离的**判别式**：把 `TopBar.BuildResourcesBar` 换回
                        //       `DefaultCurrencies` 建 ⇒ **必红**（两套次序结构上不可能同时满足）。
                        {
                            var rc = FindDeep(tb.Root, "Resources Container");
                            var drawn = new List<string>();
                            if (rc != null)
                            {
                                var rows = new List<KeyValuePair<float, string>>();
                                for (int i = 0; i < rc.childCount; i++)
                                {
                                    var icon = FindDeep(rc.GetChild(i), "Icon");
                                    var q = icon != null ? icon.GetComponentInChildren<ImageQuad>(true) : null;
                                    // 认不出图（本地缺那张 ⇒ 那一格只有药丸 + 数字）⇒ 记 `<无>`，**照样参与次序**
                                    rows.Add(new KeyValuePair<float, string>(
                                        rc.GetChild(i).position.x,
                                        q != null && q.Texture != null ? q.Texture.name : "<无>"));
                                }
                                rows.Sort((a, b) => a.Key.CompareTo(b.Key));      // 按**画出来的 x** 排（⛔ 不按子件序）
                                foreach (var r in rows) drawn.Add(r.Value);
                            }
                            // 只看那三颗**恒显**币种的**相对次序**（其余币种本地可能缺图标 ⇒ 不参与）
                            var mine = new List<string>();
                            foreach (var s in drawn)
                                if (s == "40k_topmarquee_currency_crystal" || s == "40k_topmarquee_currency_blackstone"
                                    || s == "40k_topmarquee_currency_gold") mine.Add(s);
                            Check(string.Join("|", mine.ToArray()),
                                  "40k_topmarquee_currency_crystal|40k_topmarquee_currency_blackstone|40k_topmarquee_currency_gold",
                                  "★ B25①：三颗恒显币种**画出来的次序** = crystals · blackStones · gold（**原版建表序**）；"
                                + "按 `DefaultCurrencies` 那个次序建会画成**反的**（gold 在左）。"
                                + "实得（左→右全部）=" + string.Join("|", drawn.ToArray()));
                        }

                        // 整条带子在**所有窗之下**（复用 `MainMenuRuntime.QBar*`，判据 → 那边的长注释）：
                        // 顶栏那一档的**最高层**必须**严格低于**本窗自己最低的一层（`DeckRuntime.QLowest` = `QSep`）。
                        CheckTrue(MainMenuRuntime.QBarOverlay < DeckRuntime.QLowest,
                                  "（结构）顶栏整条带子**低于**本窗最低的一层（`QBarOverlay " + MainMenuRuntime.QBarOverlay
                                + "` < `QSep " + DeckRuntime.QLowest + "`）—— 反了的话窗口内容会被顶栏压住");
                        // 立绘：一张头像图都取不到时是 `null`（按出厂态处理）—— 这里只要求「有图时画得对」
                        if (tb.TopAvatar != null)
                            CheckNear(tb.TopAvatar.WorldW * PxPerUnit, 204.4f, 1.5f,
                                      "★ D13：顶栏立绘**保宽高比**后实绘 ≈ **204.4**（原版 512² 内接进 276.84×204.4）");
                        else
                            Debug.Log(P + "  ⚠️ 顶栏立绘没画（一张头像图都取不到）—— 按出厂态处理，不是失败");

                        // ---- 🆕 2026-10-17（B25①）：那 4 颗钮**接上点击了**（原来只画、点了没反应 = 静默失败）----
                        //   判据（逐颗「原版点它开哪扇窗 / 干什么」）→ `Shell/TopBar.cs` 文件头那 4 条。
                        var hitGear  = HitOf(FindDeep(tb.Root, "SettingsBtn"));
                        var hitInbox = HitOf(FindDeep(tb.Root, "InboxBtn"));
                        var hitAvat  = HitOf(FindDeep(tb.Root, "Avatar Item Small"));
                        var hitChal  = HitOf(FindDeep(tb.Root, "Challenge button"));
                        CheckTrue(hitGear  != null && hitGear.onClick  != null, "★ B25①：**齿轮**（`SettingsBtn`）接了点击");
                        CheckTrue(hitInbox != null && hitInbox.onClick != null, "★ B25①：**信封**（`InboxBtn`）接了点击");
                        CheckTrue(hitAvat  != null && hitAvat.onClick  != null, "★ B25①：**头像**（`Avatar Item Small`）接了点击");
                        CheckTrue(hitChal  != null && hitChal.onClick  != null,
                                  "★ B25①：**挑战**（`Challenge button`）接了点击 —— 它原版**不开窗**（`ChallengeButton`："
                                + "按挑战数显隐 + 弹「收挑战」确认框），本地没有挑战源 ⇒ 那一下**出声**（`Debug.Log`），"
                                + "⛔ 不是静默");

                        // ---- 🆕 B25①：点下去**开对了窗**（各自指名道姓；⛔ 不是「点了有反应就算过」）----
                        //   ⚠️ 宿主先备好（照 `Editor/MainMenuScene.cs` 那条先例 —— 单独打开某个界面场景时
                        //      `WindowsManager` / 指针层都不在；`EnsureHost()` 幂等，壳在的时候它直接返回那一台）。
                        WindowsManager.EnsureHost();
                        if (hitGear != null && hitGear.onClick != null)
                        {
                            hitGear.onClick();
                            CheckTrue(SettingsWindow.Instance != null
                                      && SettingsWindow.Instance.CurrentState == WindowState.Open,
                                      "★ B25①：点**齿轮** ⇒ 开的是**设置窗**（`Main Menu Settings Window`）且是 `Open` 态");
                            if (SettingsWindow.Instance != null) SettingsWindow.Instance.Close();
                        }
                        if (hitInbox != null && hitInbox.onClick != null)
                        {
                            hitInbox.onClick();
                            int nIn = 0; GameWindow inWin = null;
                            var wmI = WindowsManager.Instance;
                            if (wmI != null)
                                foreach (var w in wmI.openWindows)
                                    if (w is InboxWindow && w.CurrentState == WindowState.Open) { nIn++; inWin = w; }
                            Check(nIn, 1, "★ B25①：点**信封** ⇒ 开的是**收件箱**（`Inbox Menu`），而且只有一扇");
                            if (inWin != null) inWin.Close();
                        }
                        if (hitAvat != null && hitAvat.onClick != null)
                        {
                            hitAvat.onClick();
                            var prof = PlayerProfileWindow.LastOpened;
                            CheckTrue(prof != null && prof.CurrentState == WindowState.Open,
                                      "★ B25①：点**头像** ⇒ 开的是**玩家档案窗**（`Player Profile Window`）且是 `Open` 态"
                                    + "（⚠️ 另一条判别：这三个入口点出来的**必须是三扇不同的窗** —— 上面三条断的就是这件事）");
                            if (prof != null) prof.Close();
                        }

                        // ---- 🆕 B25①：**别处有窗开着 ⇒ 本窗让位**（`HandlePointer` / `HandleEscape` 那两道闸）----
                        //   为什么需要：见 `DeckRuntime.OtherWindowUp` 上面那一段（点击会穿透 + ESC 会被两家同时吃）。
                        //   ⚠️ 批处理里驱动不了真鼠标 / 真按键 ⇒ 这里断的是**那道闸读的同一个谓词** `UiWindowShield`。
                        {
                            bool shieldBefore = _rt.UiWindowShield;           // 起点（正常情况下 = false）
                            var wShield = MainMenuRuntime.OpenSettings();
                            CheckTrue(_rt.UiWindowShield,
                                      "★ B25①：**开着**顶栏那扇窗 ⇒ 本窗让位（闸真 —— 那两道闸读的就是它）");
                            wShield.Close();
                            CheckTrue(_rt.UiWindowShield == shieldBefore,
                                      "★ B25①（**判别式**）：关掉之后闸**回到起点**（" + shieldBefore
                                    + "）—— 一个恒真的实现过不了这一对（前提它就已经红了）");
                        }

                        // ---- 🆕 2026-10-13（`A1132` 第 2 步）：**本窗自我豁免**（`DeckRuntime.OtherWindowUp`）----
                        //   为什么需要它：第 3 步起卡组编辑**自己就是一扇窗**（`Shell/DeckEditingWindow.cs`）——
                        //   开出来那一刻，**它自己就是 `wm.TopWindow`** ⇒ 上面那道「别的窗开着 ⇒ 让位」的闸
                        //   **恒真** ⇒ `HandlePointer` 一进门就 `return` = **整个窗点不动**（`A1701` 那个症状）。
                        //   ⇒ 豁免只能**放过自己那一扇**、对**别的窗**照旧拦。
                        //   🔴 **宿主用真开一扇窗**（`DeckEditingWindow.OpenFrom`）而不是手搓对象：
                        //     豁免的判据是「顶窗**是不是**我的宿主窗」，只有真走一遍开窗那一刻的状态才对得上；
                        //     而且那句 `HostWindow` 的登记就写在 `Open()` 里，手搓会**绕过它** ⇒ 验了个假。
                        //   🔴🔴 **灭自证（两条必须并存）**：
                        //     · 只断「本窗自己开着 ⇒ 假」⇒ **把整条闸删掉**照样绿（恒假）；
                        //     · 只断「别的窗开着 ⇒ 真」⇒ 「豁免写成无条件 `return null`」也绿。
                        //     两条一起 ⇒ 闸被删 / 豁免写错 / 干脆没做豁免，各红一条，结构上不可能同时满足。
                        {
                            var wmSh2 = WindowsManager.Instance;
                            CheckTrue(wmSh2 != null, "（前提）壳在（上面 `EnsureHost()` 那一句）—— 本节要读 `TopWindow`");
                            if (wmSh2 != null)
                            {
                                // ⚠️ **两个交接口都存/还**：`DeckRuntime.Build` 开局就把它们**读掉并清掉**
                                //    ⇒ 本节这一开必须**不留副作用**给后面几节（照本文件别处「夹具态复原」那条规矩）。
                                int keepPending = CollectionData.PendingEditDeck;
                                var keepNew = CollectionData.PendingNewDeck;
                                var deWin2 = DeckEditingWindow.OpenFrom(wmSh2);
                                try
                                {
                                    CheckTrue(deWin2 != null && deWin2.gameObject.activeSelf,
                                              "★ A1132 第 2 步：宿主窗开出来了、物体是活的");
                                    CheckTrue(deWin2 != null && ReferenceEquals(wmSh2.TopWindow, deWin2),
                                              "（前提）这一刻**顶窗就是本窗自己那一扇** —— 不成立的话下面那条等于没验");
                                    var hostRt = deWin2 != null ? deWin2.Runtime : null;
                                    CheckTrue(hostRt != null, "（前提）宿主窗里那台 `DeckRuntime` 在（= `Open()` 建的那一台）");
                                    if (hostRt != null)
                                    {
                                        CheckTrue(hostRt.HostWindow == deWin2,
                                                  "★ A1132 第 2 步：**宿主登记对** —— `DeckRuntime.HostWindow` = 头上这一扇"
                                                + "（**豁免判据就是它**，⛔ 不是名字：名字在 Unity 里可以重）");
                                        CheckTrue(!hostRt.UiWindowShield,
                                                  "★ A1132 第 2 步（**正面**）：**本窗自己开着 ⇒ 闸为假**"
                                                + "（= 点击 / 拖拽 / ESC 不会自己给自己让位）"
                                                + " —— 本条红就是「窗一开整个点不动」那个 bug 本身");
                                        // 🔴 **第 2 步的必然配套**：ESC 那一半**方向相反** ——
                                        //   宿主窗自己就是「`PointerLayer.KeyCancel` 已把 ESC 打给它、
                                        //   它又转回 `DeckRuntime.EscPressed()`」的那一档 ⇒ 本类**必须**停手
                                        //   （否则同一下 ESC 被处理两遍；历史上判别式是「关导入弹窗」那一级，
                                        //    那一级 2026-10-17 已随 T23 删件删掉 —— 闸本身照旧必须有。）
                                        CheckTrue(hostRt.UiEscEatenByHost,
                                                  "★ A1132 第 2 步（**配套**）：**宿主窗在顶 ⇒ ESC 已被它吃**"
                                                + "（`HandleEscape` 第二道闸 `UiEscEatenByHost`）—— "
                                                + "两个谓词**方向相反**（指针放行 / ESC 拦下）：本条与上面那条**同时**为真才对");
                                        // 反面：**换一扇别的窗**压在头上 ⇒ 闸**必须**为真（⛔ 不许把整条闸删掉）
                                        var wSh2 = MainMenuRuntime.OpenSettings();
                                        CheckTrue(hostRt.UiWindowShield,
                                                  "★ A1132 第 2 步（**判别式**）：**别的窗**压在头上 ⇒ 闸**为真**"
                                                + " —— 豁免只准放过「自己那一扇」；把整条闸删掉、或豁免写成恒 `null`，本条必红");
                                        CheckTrue(!hostRt.UiEscEatenByHost,
                                                  "★ A1132 第 2 步（**判别式**）：那一刻 `UiEscEatenByHost` **为假**"
                                                + " —— 拦住这一下的是**另一条**（`UiWindowShield`）"
                                                + "（判别式：两个谓词若被合成一个，本条与上面那条必有一条红）");
                                        if (wSh2 != null) wSh2.Close();
                                        CheckTrue(!hostRt.UiWindowShield,
                                                  "★ A1132 第 2 步（**判别式**）：那扇别的窗关掉 ⇒ 闸**回到假**"
                                                + "（一个恒定实现过不了上面这一对）—— ⚠️ 若那扇窗是全屏档，"
                                                + "它会 `HideAllWindows()` 把本窗 `Hide()`；`ShowPreviousWindow` 会把它"
                                                + "`TryOpen` 回 `Open`（同一条豁免路径），故这里仍应为假");
                                    }
                                }
                                finally
                                {
                                    if (deWin2 != null)
                                    {
                                        deWin2.Close();
                                        UnityEngine.Object.DestroyImmediate(deWin2.gameObject);
                                    }
                                    CollectionData.PendingEditDeck = keepPending;   // 见上面那条 ⚠️
                                    CollectionData.PendingNewDeck = keepNew;
                                }
                            }
                        }

                        // ⚠️ **反面控制**：没有宿主窗那条路（`HostWindow == null`）⇒ 新加的谓词**不因它**改变 ——
                        //   本节这一台 `_rt` 就是那样（`DeckEditor.unity` 直起 / 批处理裸建，没人写 `HostWindow`）。
                        CheckTrue(!_rt.UiEscEatenByHost,
                                  "★ A1132 第 2 步（**反面控制**）：**没有宿主窗** ⇒ `UiEscEatenByHost` 恒假"
                                + "（= 独立场景那条路与加这一格之前**逐位一致**：ESC 照旧由本类自己吃）");

                        // ---- 🆕 2026-10-14（`A1132` 第 4 步）：离场的**另一条腿** = **有宿主窗** ⇒ 关窗、⛔ 不切场景 ----
                        //   判据 = 原版 `DeckEditingWindow__Close.c:19-20`（`NavigationPanelController.Toggle(1,1,0)`
                        //   放回导航栏 → `GameWindow.Close()`）：关掉之后底下那扇**一直开着的** `CollectionScreen`
                        //   由 `WindowsManager.ShowPreviousWindow` 带回来。原版**没有「回程」这回事** ——
                        //   我们这条腿的等价物（消费回程意图 + 作废库缓存）落在 `DeckRuntime.LeaveByHostWindow`。
                        //   🔴 **与 `Run()` 开头那 5 条 A855 断言【并存】**（灭自证）：那 5 条钉的是
                        //   `HostWindow == null` 那条兜底腿（照旧写意图 + 切场景，**一个字未动**）；
                        //   本节钉的是被托管这条腿（关窗 + **就地消费**）。**两条腿结构上不可能同时满足**
                        //   ⇒ 只做一条 / 把老腿顺手删掉，另一条必红。
                        {
                            var wm4 = WindowsManager.EnsureHost();      // 幂等（上面那节已经建过）
                            string sceneBefore4 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                            int pendKeep4 = CardPresentation.CollectionData.PendingEditDeck;
                            var newKeep4 = CardPresentation.CollectionData.PendingNewDeck;
                            var retKeep4 = CardPresentation.CollectionData.PendingReturn;
                            var deWin4 = DeckEditingWindow.OpenFrom(wm4);
                            try
                            {
                                var hrt4 = deWin4 != null ? deWin4.Runtime : null;
                                CheckTrue(hrt4 != null && hrt4.HostWindow == deWin4,
                                          "（前提）这一台 `DeckRuntime` **有宿主窗**（= 被托管那条腿），下面几条才有意义");
                                if (hrt4 != null)
                                {
                                    int leaveBefore = hrt4.LeaveCount;
                                    CardPresentation.CollectionData.SetReturnIntent(
                                        CardPresentation.DeckExitSource.Collection,
                                        CardPresentation.WindowTabType.CollectionDecks);
                                    hrt4.BackToMenu();
                                    Check(hrt4.LeaveCount, leaveBefore + 1,
                                          "★ A1132 第 4 步：离场那一下走到了 `BackToMenu`（`LeaveCount` +1）");
                                    CheckTrue(!deWin4.gameObject.activeSelf && !deWin4.IsOpen(),
                                              "★ A1132 第 4 步（**被托管那条腿**）：离场 ⇒ **关掉了宿主窗**"
                                            + "（`state = Closed` **且** 物体不活 —— 两条并存）—— 判据 = 原版"
                                            + " `DeckEditingWindow__Close.c:19-20`");
                                    Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, sceneBefore4,
                                          "★ ……而**场景没换**（第 4 步要替掉的就是那句 `LoadScene(\"MainMenu\")`）"
                                        + " —— ⚠️ 与 `Run()` 开头那 5 条 A855 断言**成对**：那 5 条的前提是"
                                        + "「**没**宿主窗 ⇒ 走独立场景那条腿」，本条的前提是「**有**宿主窗」");
                                    Check((int)CardPresentation.CollectionData.PendingReturn.Source,
                                          (int)CardPresentation.DeckExitSource.None,
                                          "★ ……而且回程意图**在关窗那一拍就地消费掉了**（`Take*` 读走即清）——"
                                        + " 原来这一跳只在 `MainMenuRuntime.Build` 里，而那条**只有真的换过场景才轮得到**"
                                        + "（判据 → `DeckRuntime.LeaveByHostWindow` 的 doc）");
                                }
                            }
                            finally
                            {
                                if (deWin4 != null)
                                {
                                    deWin4.Close();      // 已经关过 ⇒ 走 `Close()` 的首句守卫、零副作用
                                    UnityEngine.Object.DestroyImmediate(deWin4.gameObject);
                                }
                                CardPresentation.CollectionData.PendingEditDeck = pendKeep4;
                                CardPresentation.CollectionData.PendingNewDeck = newKeep4;
                                CardPresentation.CollectionData.PendingReturn = retKeep4;
                            }
                        }

                    }
                }

                // ---- ⑭ D2/D3/D4：四处文案走 `Loc.T`（中文档 = 原版实拍的那五个词）----
                //   判据两条，**都是原版的**：
                //     ① **词条键** = 原版 prefab 上那颗 `Localize` 的 `mTerm`（⛔ 不是自拟的键）——
                //        逐颗实读 `bundle_menus_assets_all` 的 `Deck Editing Menu` 全树：
                //        `…/Content Area/Header/Filters/Label` → `MenuDeck/Filters/Filters`（TMP "Filters" fs40）·
                //        `…/Sidebar/Window Options/Buttons/{Cards,Info,Cosmetics}/Label/Text` →
                //        `MenuShop/ShopItemType/Cards`（"Cards" fs34）/
                //        `MenuDeck/HUD/DeckDescription/DeckInfo`（"Deck info" fs31.5）/
                //        `MenuShop/ShopItemType/Cosmetics`（"Cosmetics" fs28.15）·
                //        `…/Sidebar/Footer/Done/Button Text` → `MenuDeck/MenuButtons/Done`（"Done" fs40；
                //        那颗上挂着**两颗** `Localize`，第二颗 `MenuLogin/Login/DoneButton` 是登录页共用的）。
                //        ⚠️ 键名分属 `MenuShop/*` 与 `MenuDeck/*` **两个前缀 —— 原版如此**（前两颗页签与
                //        美容品页签共用商城那一族的词条），⛔ 别「看着不齐」把它们统一成同一个前缀。
                //     ② **两列文案**：英文 = 上面那几颗 TMP 的 `m_text` **原文**；
                //        中文 = **原版实拍**（`资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png`：
                //        过滤器 / 张牌 / 卡组信息 / 美容品 / 完成 —— 本块开工时逐字复核过截图）。
                //   ⛔ 期望值一律**原版字面量**，一个都不从 `DeckRuntime` / `Loc` 里读回来（那是自证）。
                //   🧨 **判别式**：把 `DeckRuntime` 那三处 `Loc.T(...)` 换回写死的英文 ⇒
                //     ②的「实况」那五条**在中文档下必红**（② 自己先把语言切成中文再建窗）；
                //     反过来把词条的中文列改回英文 ⇒ ①那五条红。
                {
                    // 四张**同序**的表：键 · 它在 prefab 里的节点（报错时要能一眼找到判据）· 原版中文 · 原版英文
                    var lTerms = new[] { "MenuDeck/Filters/Filters",
                                         "MenuShop/ShopItemType/Cards",
                                         "MenuDeck/HUD/DeckDescription/DeckInfo",
                                         "MenuShop/ShopItemType/Cosmetics",
                                         "MenuDeck/MenuButtons/Done" };
                    var lWhere = new[] { "Header/Filters/Label",
                                         "Window Options/Buttons/Cards/Label/Text",
                                         "Window Options/Buttons/Info/Label/Text",
                                         "Window Options/Buttons/Cosmetics/Label/Text",
                                         "Footer/Done/Button Text" };
                    var lZh = new[] { "过滤器", "张牌", "卡组信息", "美容品", "完成" };
                    var lEn = new[] { "Filters", "Cards", "Deck info", "Cosmetics", "Done" };

                    var langBack2 = Loc.Current;
                    bool persistWas = Loc.PersistOverride;
                    Loc.PersistOverride = true;     // ⛔ 自检一个字节都不写 `PlayerPrefs["Language"]`
                    try
                    {
                        // ---- ① 词条层：两列各钉一次（**不建窗** ⇒ 与运行时的语言无关，恒定可跑）----
                        Loc.SetLanguage(AvailableLanguages.Chinese);
                        for (int i = 0; i < lTerms.Length; i++)
                            Check(Loc.T(lTerms[i]), lZh[i],
                                  "★ D2/D3/D4 词条层：`" + lTerms[i] + "`（" + lWhere[i] + "）的中文列 = **原版实拍「"
                                + lZh[i] + "」**");
                        Loc.SetLanguage(AvailableLanguages.English);
                        for (int i = 0; i < lTerms.Length; i++)
                            Check(Loc.T(lTerms[i]), lEn[i],
                                  "★ D2/D3/D4 词条层：`" + lTerms[i] + "` 的英文列 = 那颗 TMP 的 `m_text`「"
                                + lEn[i] + "」");

                        // ---- ③ 🆕 **2026-10-18（第三轮整改 · 审查 S1）：本批新接的词条【两列都钉死】** ----
                        //   🔴 **为什么需要这一段**：本批其余每一条断言都是 `Check(渲染值, Loc.T(同一把键))`
                        //     —— 两边都过 `Loc.T` ⇒ **把 `Core/Loc.cs` 里那条【中文值】改坏（例如
                        //     `MenuDeck/Filters/Army` 的「军队」改成「兵力」），全部断言照绿**
                        //     （`Loc.HasEntry` 只挡「键不在表里」，挡不了「值不对」）。
                        //   ⇒ 这里把**玩家最常看到 + 本批新接**的那一批的**中英两列都写成字面量**
                        //     （⛔ 没过 `Loc.T` 取期望值 —— 那就是自证）。中文来源逐条写在 `sSrc` 里。
                        //   ⚠️ **只钉这一批**（范围内）；其余仍走 `Loc.T`（维护成本，理由见交件报告 §9）。
                        //   ⚠️ 循环里来回切语言，**每轮末尾切回中文** —— 下面 ② 那一段假设当前是中文档。
                        {
                            var sTerms = new[]
                            {
                                "MenuDeck/Filters/Filters",           // 本批 B 组接的（原已存在，一并钉住）
                                "MenuDeck/Filters/ClearFilters",
                                "MenuDeck/HUD/SearchFilter",
                                "MenuDeck/Filters/Army",
                                "MenuDeck/HUD/Rarity",
                                "MenuDeck/Filters/Type",
                                "Battle/Tips/EnergyCost",
                                "MenuDeck/MenuButtons/CreateDeck",
                                "MenuDeck/MenuButtons/ImportDeck",
                                "MenuDeck/Button/Random",
                                "MainMenu/General/Confirm",
                                "MainMenu/Ranked/GoToCreateDeck",
                                "MenuDeck/HUD/EditDeckName",
                                "MenuCollection/NoCardsFound",
                                "MenuCollection/NoCardbackFound",
                                "MenuCollection/NoDecksFound",
                                "MenuCollection/Label/Cosmetics",
                                "MainMenu/MainButtons/ButtonLabel/Back",
                            };
                            var sWhere = new[]
                            {
                                "Deck Editing Menu/…/Header/Filters/Label",
                                "…/Header/Filters/Generic Simplified UI Button_updated/Button Text",
                                "…/Card Filters/…/Name FIlter/Input Field/Text Area/Placeholder",
                                "…/Card Filters/Filters/Army Filter/Title",
                                "…/Card Filters/Filters/Rarity FIlter/Title",
                                "…/Card Filters/Filters/Type Filter/Title",
                                "…/Card Filters/Filters/Cost Filter/Title（+ 另外 14 个 bundle）",
                                "Collection Menu Variant/…/Control Buttons/Create/Button Text",
                                "同上的兄弟 Import/Button Text",
                                "Deck Selection Popup with Tabs/…/Practice buttons/…/Button Text",
                                "Import Deck Popup/Window/Buttons/Generic UI Button/Button Text",
                                "Ranked Deck Selection/No Deck Text/Generic Simplified UI Button/Button Text",
                                "Deck Editing Menu/…/Deck Name/Text Area/Placeholder",
                                "…/Empty Collection Warning/Warning（Cards 页 + 异画页 + 卡背抽屉）",
                                "…/Empty Collection Warning/Warning（卡背页）",
                                "…/Empty Collection Warning/Warning（Deck 页）",
                                "Collection Menu Variant/…/Cardback Tab/Header/label",
                                "Collection Menu Variant/…/Tabs/Shared/Close Button/Button Text",
                            };
                            // 🔴 2026-10-14（第二十七会话）就地改期望值：下面 7 条中文列原是 `zh_CN.csv` 的我们自译版，
                            //   官方 zh-CN 表已到手 ⇒ 换成官方值（清洗/能量/编辑名/三条空收藏/装饰）。
                            var sZh = new[]
                            {
                                "过滤器", "清除过滤器", "搜索", "军队", "稀有度", "类型", "能量花费",
                                "创建卡组", "导入卡组", "随机", "确认", "创建卡组", "点击以编辑卡组名称",
                                "在您选择的过滤器中，您的收藏里没有任何张牌", "在您选择的过滤器中，您的收藏里没有任何卡背", "在您选择的过滤器中，您的收藏里没有卡组",
                                "你的化妆品收藏", "返回",
                            };
                            var sEn = new[]
                            {
                                "Filters", "Clear filters", "Search", "Army", "Rarity", "Type", "Energy Cost",
                                "Create Deck", "Import Deck", "Random", "Confirm", "Create deck", "Tap to edit deck name",
                                "There are no cards in your collection for the selected filters",
                                "There are no cardbacks in your collection for the selected filters",
                                // 🔴 2026-10-14（`A1803`）就地改期望值：原来是我们写的 `There are no deck…`（单数 `deck`）；
                                //    官方 zh-CN 表 = `There are no decks in your collection for the selected filters`（复数）⇒ 以官方为准。
                                "There are no decks in your collection for the selected filters",
                                "Your cosmetics collection", "Back",
                            };
                            var sSrc = new[]
                            {
                                "**原版实拍**（用户 1017 截图；⚠️ 与我们那份 `zh_CN.csv:8` 的「筛选」**不同**）",
                                "`zh_CN.csv:9`", "`zh_CN.csv:10`", "`zh_CN.csv:11`",
                                "`zh_CN.csv:150`", "`zh_CN.csv:182`", "`zh_CN.csv:98`（英文串索引 · 不确定性见 `Loc.cs`）",
                                "`zh_CN.csv:7`", "**我们自拟**（CSV 无此键）", "**我们自拟**（原版 TMP 是空串）",
                                "`zh_CN.csv:83`", "`zh_CN.csv:230`", "`zh_CN.csv:172`",
                                "`zh_CN.csv:177`", "**我们自拟**（CSV 无此键）", "`zh_CN.csv:178` 近邻（串差一个字母）",
                                "`zh_CN.csv:25`", "`zh_CN.csv:5`",
                            };
                            CheckTrue(sTerms.Length == sWhere.Length && sTerms.Length == sZh.Length
                                      && sTerms.Length == sEn.Length && sTerms.Length == sSrc.Length,
                                      "（前提）四张表的行数一致（" + sTerms.Length + "）");
                            for (int si = 0; si < sTerms.Length; si++)
                            {
                                Loc.SetLanguage(AvailableLanguages.Chinese);
                                Check(Loc.T(sTerms[si]), sZh[si],
                                      "★ S1 **中文列钉死**（⛔ 不过 `Loc.T` 取期望）：`" + sTerms[si] + "`（" + sWhere[si]
                                    + "）= 「" + sZh[si] + "」—— 来源 = " + sSrc[si]);
                                Loc.SetLanguage(AvailableLanguages.English);
                                Check(Loc.T(sTerms[si]), sEn[si],
                                      "★ S1 **英文列钉死**：`" + sTerms[si] + "` = `" + sEn[si]
                                    + "`（= 原版那颗 TMP 的 `m_text` 原文；`Random` 那条例外，见 `Core/Loc.cs`）");
                            }
                            Loc.SetLanguage(AvailableLanguages.Chinese);   // ⚠️ 收尾：② 那一段假设当前是中文档
                            Check(Loc.Current, AvailableLanguages.Chinese, "（收尾）语言放回中文档");
                        }

                        // ---- ④ 🆕 **2026-10-18（第四轮 · 审查 S2）：`Loc.T` 自身退化的两条守卫** ----
                        //   🔴 为什么需要：本批**所有**渲染断言都是 `Check(渲染值, Loc.T(键))` —— 两边都过同一个口
                        //     ⇒ 若 `Loc.T` **退化成「恒返回键名」**，两边一起退化 = **全绿**（`Loc.HasEntry` 也照绿，
                        //     因为它只问「键在不在表里」，不问「T 有没有真的查表」）。这两条把它挡住：
                        //       ① 一个**表里没有**的键 ⇒ 返回**键名本身** + **确实出声**（抓日志）；
                        //       ② **同一个键问两次 ⇒ 只出声一次**（去重 —— ⛔ 删掉那句 `_warnedMissing.Add` ⇒ 红）。
                        //   ⚠️ 打头先 `ResetMissingWarnedForTest()` ⇒ 本段**不依赖这个进程跑过几遍**。
                        //   ⚠️ 本段**不建窗、不碰盘**（只读 `Loc` 的静态记账 + 抓日志）。
                        {
                            const string ghostKey = "No/Such/Key/__W1_S2__";
                            Loc.ResetMissingWarnedForTest();          // ⛔ 只清去重表，不动 PlayerPrefs
                            CheckTrue(!Loc.HasEntry(ghostKey),
                                      $"（前提）`{ghostKey}` **不在**表里（⛔ 在的话下面两条失去意义）");
                            CheckTrue(!Loc.HasWarnedMissing(ghostKey), "（前提）清完去重表 ⇒ 这个键还没出过声");
                            int mc0 = Loc.MissingCount;
                            var logsS2 = new List<string>();
                            Application.LogCallback cbS2 = (cond, stack, type) =>
                            { if (type == LogType.Warning && cond != null) logsS2.Add(cond); };
                            Application.logMessageReceived += cbS2;
                            string got1, got2;
                            try
                            {
                                got1 = Loc.T(ghostKey);
                                got2 = Loc.T(ghostKey);
                            }
                            finally { Application.logMessageReceived -= cbS2; }
                            Check(got1, ghostKey, "★ S2①：表里没有的键 ⇒ 返回**键名本身**（⛔ 不是空串、不是英文）");
                            Check(got2, ghostKey, "★ S2①（第二次调用）：同样返回键名本身");
                            Check(Loc.MissingCount, mc0 + 2,
                                  "★ S2①：**记账**照旧每次 +1（去重只管日志，⛔ 别把 `MissingCount` 也去重了）");
                            int hits = 0;
                            for (int li = 0; li < logsS2.Count; li++) if (logsS2[li].Contains(ghostKey)) hits++;
                            Check(hits, 1, "★ S2②：**同一条键问两次 ⇒ 只出声一次**（判据 = 抓到含该键的 Warning 条数）"
                                         + " —— 🧨 删掉 `Loc.T` 里那句 `if (_warnedMissing.Add(key))`（或把去重表整个删掉）"
                                         + "⇒ 这里变 **2** ⇒ 红");
                            CheckTrue(Loc.HasWarnedMissing(ghostKey), "…并且这个键被记进了去重表（`HasWarnedMissing`）");
                        }

                        // ---- ② 实况层：**在中文档下真建一扇窗**，量那五颗标签到底印了什么 ----
                        //   ⚠️ 为什么不量 `_rt`：那扇窗是按**玩家当前语言**建的（自检不许假设它是中文）
                        //     ⇒ 临时建一扇探针窗、用完 `DestroyImmediate`（形状同 `Run()` 里那三处 `*Smoke`）。
                        //   ⚠️ 建窗**不改库**：`DeckRuntime.Build` 只 `State.LoadDeck(Library.Current)`（读副本）。
                        Loc.SetLanguage(AvailableLanguages.Chinese);
                        var lgo = new GameObject("LocTextSmoke");
                        var lrt = lgo.AddComponent<DeckRuntime>();
                        lrt.Build(DeckLibrary.Load());
                        CheckTrue(lrt.State != null, "（前提）中文档下的探针窗建起来了");
                        if (lrt.State != null)
                        {
                            var lKeys = new[] { "hdr_fltlbl", "tab_tx0", "tab_tx1", "tab_tx2", "foot_done_t" };
                            for (int i = 0; i < lKeys.Length; i++)
                                Check(lrt.UiLabelText(lKeys[i]), lZh[i],
                                      "★ D2/D3/D4 实况（中文档）：`" + lKeys[i] + "` 印的是 **「" + lZh[i]
                                    + "」**（判据 = 原版实拍；那三处写死回英文 ⇒ 这条红）");
                        }
                        UnityEngine.Object.DestroyImmediate(lgo);
                    }
                    finally { Loc.RestoreForTest(langBack2); Loc.PersistOverride = persistWas; }
                }
            }

            // ---- 🆕 2026-10-18（A870）：一屏铺**几行** = 运行时算（原来是我们挑的 `const int PoolRows = 3`）----
            //   判据 = 原版 `PolyAndCode.UI.VerticalRecyclingSystem.CreateCellPool` 那**两条阈值**（逐字读，
            //   出处见 `DeckRuntime.PoolMinCoverage` 那段 doc）：
            //     ① 覆盖：`ceil(视口高 × MinPoolCoverage(1.5) ÷ 格高)`（`:193` 的循环条件；
            //        网格档里总高**每换一行加一次** —— `CardPresentation/Deck/DeckRuntime.cs` + `CardPresentation/Deck/DeckRuntime.cs` 的 `goto`）；
            //     ② 池格下限：`ceil(MinPoolSize(20) ÷ 列数)`（`:186-192` 那个格下标阈值换成行）。
            //   ⇒ 桌面 `max(ceil(20÷6), ceil(924.06×1.5÷384))` = **4 行 / 24 格**；
            //      小屏 `max(ceil(20÷4)=5, ceil(924.06×1.5÷576)=3)` = **5 行 / 20 格**
            //      （⚠️ **小屏行数比桌面多** —— 格数下限恒定 20、列数变少 ⇒ 行变多）。
            {
                _rt.UiSetTab(0);                       // 卡池只在 Cards 页（`RefreshPool` 的显隐判据里有 `_tab`）
                _rt.UiScrollPool(-100000f);            // 回到池顶（下面几条要在 off = 0 处量）
                int visN = _rt.State.VisibleCards().Count;
                CheckTrue(visN >= 24, $"（前提）卡池至少 24 张（实得 {visN}）—— 桌面铺 4 行 = 24 格要靠它");

                // ① **换视口高 ⇒ 行数跟着变**（这条钉「不是常量」）
                Check(DeckRuntime.UiPoolRowsForViewport(924.1f, 384f, 6), 4,
                      "★ A870：视口 **924.1** / 格 384 / 6 列 ⇒ **4 行**"
                    + "（`max(ceil(20÷6), ceil(924.1×1.5÷384))`；1.5 = 原版 `MinPoolCoverage`、20 = `MinPoolSize`）");
                Check(DeckRuntime.UiPoolRowsForViewport(2000f, 384f, 6), 8,
                      "★ A870：**把视口高换成 2000** ⇒ **8 行**（`ceil(2000×1.5÷384)`）—— 行数写死那种写法 ⇒ 这条红");
                Check(DeckRuntime.UiPoolRowsForViewport(3000f, 384f, 6), 12,
                      "★ A870：再换 3000 ⇒ **12 行** —— 行数**跟着视口高走**（不是常量、也不是「每格加一次」）");

                // ② 现算出来的两档（桌面 4 ⇄ 小屏 5）—— 与列数那半是**同一个属性**（`Pool`）里的两行
                _rt.UiSetSmallScreenUIForTest(false);
                Check(_rt.UiPoolRowsNow, 4, "★ A870（桌面）：铺 **4 行**（24 格 = 6 × 4）—— 写回 `const = 3` ⇒ 红");
                _rt.UiSetSmallScreenUIForTest(true);
                Check(_rt.UiPoolRowsNow, 5,
                      "★ A870（小屏）：**5 行**（20 格 = 4 × 5）—— 覆盖那条只要 3 行，**池格下限 `ceil(20÷4) = 5` 占了上风**"
                    + "（格数下限恒定 20、列数变少 ⇒ **小屏行数比桌面多**，铁律 5·c 的「状态 → 参数」）");
                CheckTrue(_rt.UiPoolRowsNow != 4,
                          "★ A870 判别式：两档行数**必须不同**（桌面 4 / 小屏 5）—— 行数写成常量 ⇒ 两档相等 ⇒ 这条与上面两条一起红");

                // ③ **真铺出来了**（把行数算对 ≠ 循环里真按它铺）—— 桌面第 4 行：
                _rt.UiSetSmallScreenUIForTest(false);
                CheckTrue(_rt.UiPoolCellRect(23, out float r23x, out float r23y, out float r23w, out float r23h),
                          "★ A870（桌面）：`pool_23` = **第 4 行第 6 列**也铺出来了（4 × 6 = 24 格）"
                        + " —— 只把行数算对、循环里仍铺 3 行 ⇒ 这条红");
                //   🔴 **判别式（盖住视口底的硬要求）**：往下滚 300px（= 0.78 行）之后，池底那一段仍要被第 4 行盖住。
                //     只铺 3 行时 `pool_18` 这一格**根本不存在** ⇒ 池底露出 72px 空白（原版那个 1.5 倍覆盖正是挡这个）。
                _rt.UiScrollPool(300f);
                CheckTrue(_rt.UiPoolCellRect(18, out float r18x, out float r18y, out float r18w, out float r18h),
                          "★ A870 判别式：滚到半行（300px）之后**第 4 行仍在池里**（`pool_18` 活着、池底不露白）"
                        + " —— 行数写死 3 ⇒ 这一格不存在 ⇒ 红；改成 `ceil(视口高 ÷ 格高) = 3` ⇒ 同样红");
                _rt.UiScrollPool(-100000f);
                //   小屏那一档**也**要真收起来（20 格 vs 桌面 24 格那 4 格）—— 那两条在 ⑧ D24 那一节里
                //   （`pool_19` 在画 / `pool_20` 不画；A870 起它们的下标从 12 挪到 20），这里不重复。
                _rt.UiSetSmallScreenUIForTest(false);      // 收尾：换回桌面档
                _rt.UiScrollPool(-100000f);
                Check(_rt.UiPoolColsNow, 6, "（收尾）回到桌面档 6 列");
            }

            // ---- 🆕 2026-10-13（A1702 ③）：卡池的**视口裁切**（渲染 + 命中区**两半一起**）----
            //   原版：`Content Area/Card Display/Scroll View/Viewport` 上挂 **`RectMask2D`**（硬边、不内缩）
            //     ⇒ 滚出视口的卡**既不画、也点不到**。我们原来两半都缺：
            //       · **渲染**：`RefreshPool` 的 `SetPose` **不传第 4 参** ⇒ 部分滚出视口的卡**整张画出去**
            //         （压在页头那条空带上）；
            //       · **命中**：`HandlePoolClick` **只判 `px.x`**（左右）⇒ 滚到视口之外的卡**照样点得到**。
            //   判据矩形 = 视口 **(330.2,156)-(1920,1080.1)**（出处 = `DeckRuntime` 的
            //     `PoolX/PoolY/PoolW/PoolH` 那一段：原版 `Card Display` 视口 1589.8×924.1，R:167-170）。
            //   🔴 **灭自证**（这一节的三条连起来才有鉴别力，单看任何一条都能被某种错法骗过）：
            //     ① 裁切框**必须等于那个视口矩形** —— 传 `null`（不裁）/传整屏/传卡自己的框 ⇒ 红；
            //     ② 视口**外**那一点 ⇒ **没人吃**；③ 视口**内**那一点 ⇒ **吃**
            //     —— 只做渲染那半 ⇒ ② 红；只做命中那半 ⇒ ① 红；两半都做才全绿。
            {
                _rt.UiSetSmallScreenUIForTest(false);
                _rt.UiSetTab(0);                                 // 卡池只在 Cards 页画
                _rt.UiScrollPool(-100000f);                      // 回到池顶（下面几条要在 off = 0 处量）
                CheckTrue(_rt.State.VisibleCards().Count >= 7,
                          $"（前提）卡池至少 7 张（滚 300px 之后第 0 格与第 1 行第 0 格要同时在，实得 "
                        + $"{_rt.State.VisibleCards().Count}）");

                // ① **没滚的时候**：第 0 格带着那个视口矩形（`CardView.Clip` 有值）
                bool hasClip = PoolClipOf(0, out float a1, out float b1, out float c1, out float d1);
                CheckTrue(hasClip,
                          "★ A1702③：`pool_0` 这张卡**带上裁切了**（`CardView.Clip` 有值）"
                        + "｜🧨 `RefreshPool` 的 `SetPose` 不传第 4 参 ⇒ `Clip` 恒 null ⇒ 本条红"
                        + "（= 部分滚出视口的卡整张画出去）");
                CheckTrue(hasClip && Mathf.Abs(a1 - 330.2f) < 0.5f && Mathf.Abs(b1 - 156f) < 0.5f
                          && Mathf.Abs(c1 - 1920f) < 0.5f && Mathf.Abs(d1 - 1080.1f) < 0.5f,
                          $"★ A1702③ 灭自证①：裁切框 = **视口矩形** (330.2,156)-(1920,1080.1)，"
                        + $"实得 ({a1:F1},{b1:F1})-({c1:F1},{d1:F1})"
                        + "｜🧨 传整屏 `(0,0,1920,1080)` ⇒ `y1` 那半红；传这张卡自己的框 ⇒ 四边全红；传 null ⇒ 上一条红");

                // ② 滚到 300px（= 0.78 行）：第 0 格的中心**离开视口上沿**、第 1 行第 0 格（`pool_6`）仍在视口里
                _rt.UiScrollPool(300f);
                bool has0 = _rt.UiPoolCellRect(0, out float z0x, out float z0y, out float z0w, out float z0h);
                bool has6 = _rt.UiPoolCellRect(6, out float z6x, out float z6y, out float z6w, out float z6h);
                CheckTrue(has0 && has6, "（前提）滚动 300px 之后 `pool_0` 与 `pool_6` 两格都在");
                CheckTrue(has0 && z0y < 156f,
                          $"★ （前提）`pool_0` 的**中心**已滚到视口上沿之外（y={z0y:F1} < 156）"
                        + "—— 它是「部分露在视口里」的那一格（上半截压在页头空带上）");
                CheckTrue(has6 && z6y > 156f && z6y < 1080.1f,
                          $"★ （前提）`pool_6`（第 1 行第 0 格）的中心仍在视口里（y={z6y:F1}）");

                // ③ **命中区那半** —— 与①成对，**结构上不可能同时满足**：
                //    只把 clip 传进去（渲染那半），②/③ 这一对必红；只加视口框（命中那半）而不传 clip，① 必红。
                CheckTrue(!_rt.UiPoolHit(z0x, z0y),
                          $"★ A1702③ 灭自证②：点 `pool_0` 的**中心**（中心已滚到视口上沿之外，y={z0y:F1} < 156）"
                        + " ⇒ **没人吃这一下**"
                        + "｜🧨 `HandlePoolClick` 只判 `px.x` ⇒ 这里变 **true**（= 越界的卡照旧点得到）⇒ 红");
                CheckTrue(_rt.UiPoolHit(z6x, z6y),
                          $"★ A1702③ 正向控制：**同一次**点视口**内**那一格（`pool_6` 中心 {z6x:F0},{z6y:F0}）"
                        + " ⇒ **吃**了这一下"
                        + "｜🧨 把视口框收得比卡池还小 / 改成一律 `return false` ⇒ 本条红");

                // ④ 「越界的卡**不画**」那一半再钉一次读数（与 ① 同一个口，但量的是**滚出去之后**）：
                //    裁切框**不跟着滚动变**（它是视口的框，不是这张卡的框）—— 这正是「卡在动、框不动」的判据。
                CheckTrue(PoolClipOf(0, out float e1, out float f1, out float g1, out float h1)
                          && Mathf.Abs(e1 - 330.2f) < 0.5f && Mathf.Abs(f1 - 156f) < 0.5f
                          && Mathf.Abs(g1 - 1920f) < 0.5f && Mathf.Abs(h1 - 1080.1f) < 0.5f,
                          $"★ A1702③：滚出去之后那一格的裁切框**仍是视口矩形**（实得 ({e1:F1},{f1:F1})-({g1:F1},{h1:F1})）"
                        + "｜🧨 把 clip 传成「这张卡自己的框」⇒ 它跟着卡一起滚 ⇒ 这一条红");

                // ⑥ **裁切的结果**（与 ① 那条「读 `Clip` 字段」成对 —— 逐**顶点**量真画出来的范围）：
                //    此刻 `pool_0` 那张卡横跨 y ∈ [−144.0, 201.7]（中心 28.9、半高 172.86）
                //    ⇒ 裁对了的话 **没有任何一个顶点**画到视口上沿（156）以上。
                //    ⚠️ 容差 2px：网格顶点的浮点噪声 + `ToDesignPixel` 的斜率与 108 差 1e-5 那一档。
                bool drew0 = PoolDrawnExtentPx(0, out float v0x1, out float v0y1, out float v0x2, out float v0y2);
                CheckTrue(drew0, "（前提）滚出视口上沿那一格**还有东西在画**（它只露出一半 —— 全裁光了就没得量）");
                CheckTrue(drew0 && v0y1 >= 156f - 2f,
                          $"★ A1702③ 裁切结果：`pool_0` **真画出来的**东西一个顶点都没越过视口上沿"
                        + $"（最高顶点 y={v0y1:F1} ≥ 156；实画 ( {v0x1:F1},{v0y1:F1} )→( {v0x2:F1},{v0y2:F1} )，"
                        + "这张卡的**完整**矩形是 y ∈ [−144.0, 201.7]）"
                        + "｜🧨 `SetPose` 不传第 4 参（= 不裁）⇒ 这里读出 ≈ −144（整张卡画到页头空带上）⇒ 红"
                        + "；本条量**顶点**、① 读 `Clip` **字段** —— 两条腿缺一不可（同 A198③ 的口径）");

                _rt.UiScrollPool(-100000f);                      // 收尾：回池顶
            }

            // ---- 🆕 2026-10-17（`A1495` 第二半）：**卡池 → 卡组行 → 卡背 → 钮** 那条【完整】派发链 ----
            //   判据（账文）→ `资料/任务线/界面线/任务/T01_卡组编辑窗.md` §2·3。
            //   🔴 **结构原因**（那一段原文）：自检口 `UiClickPx` **只走 `HandleButtons`**，
            //     `UiPoolHit` **只走 `HandlePoolClick`** ⇒ 真实鼠标那条**按顺序**的派发
            //     **一个自检口都覆盖不到**。`A1495` 那道 NRE 恰恰出在「卡池这一站返回 false 之后、
            //     继续往下走」那条路上 ⇒ 只直调单站**永远抓不到**它（= 「16k+ 断言全绿、真机一进去就炸」）。
            //   ⚠️ 本节**不跑自检时的那条真鼠标路**（批处理 `Mouse.current == null`）—— 它走的是新自检口
            //     `UiClickPoolPx`，那个口的每一句**逐句照 `HandlePointer`**（闸 → 池 → 行 → 卡背 → 钮）。
            {
                _rt.UiSetSmallScreenUIForTest(false);            // 桌面档（6 列 × 4 行 = 24 格）
                _rt.UiSetTab(0);                                 // 卡池只在 Cards / Deck info 两页画
                _rt.UiClearFilters();                            // 清掉前面几节留下的筛选
                _rt.UiScrollPool(-100000f);                      // 先回池顶（把「槽 → 卡表下标」收成 0..23）
                int nPool = _rt.State.VisibleCards().Count;
                CheckTrue(nPool > 0, $"（前提）卡池有卡（{nPool} 张）");
                // 一次滚到底：**只刷新一趟** ⇒ 尾行那几格没有可复用的视图（见 `RefreshPool` 的复用判据）
                //   ⇒ 它们就是 `_poolIndex[i] < 0` 且 `_poolViews[i] == null` 的**真 null 槽**。
                _rt.UiScrollPool(100000f);

                int cols = _rt.UiPoolColsNow;
                int slots = _rt.UiPoolSlotCount;
                CheckTrue(slots > cols * 2, $"（前提）卡池槽数 {slots} 够多（列数 {cols}）");

                // ---- ② 不变式：空槽（`_poolIndex[i] < 0`）⇒ 视图 **null 或未激活** ----
                //   ⚠️ 遍历只到 `UiPoolSlotCount`（`_poolIndex.Count`）—— `_poolViews` 尾巴上还挂着
                //     「换档多出来、被 park 的那几格」（`_poolViews.Count` 可以比它大）⇒ 别拿 `_poolViews.Count` 当槽数。
                var badSlots = new List<string>();
                int emptySlots = 0, nullSlots = 0;
                for (int i = 0; i < slots; i++)
                {
                    if (_rt.UiPoolSlotCardIndex(i) >= 0) continue;
                    emptySlots++;
                    bool? act = _rt.UiPoolSlotViewActive(i);
                    if (act == null) nullSlots++;
                    if (act == true) badSlots.Add($"{i}(活着)");
                }
                CheckTrue(badSlots.Count == 0,
                          $"★ A1495 不变式：**空槽**（`_poolIndex[i] < 0`）的视图必须是 **null 或未激活**"
                        + $"（空槽 {emptySlots} 个：null {nullSlots} · 未激活 {emptySlots - nullSlots}；"
                        + $"违例 {badSlots.Count} 个{(badSlots.Count > 0 ? "：" + string.Join("、", badSlots) : "")}）"
                        + "｜🧨 `RefreshPool` 给空槽留下一个**还开着**的视图 ⇒ 这一条红");

                // ---- ③ **灭自证**：挑一个**真 null 槽**去点 ----
                //   为什么要挑 null 槽：`HandlePoolClick` 的守卫是
                //     `if (v == null || !v.gameObject.activeSelf || … ) return false;`
                //   ⇒ 若点的是「**建了但没激活**」的那种空槽，**把 `v == null` 那截删掉也照样返回 false**
                //     ⇒ ① 会变成**假绿**。只有 **null 槽**才真正踩到那道守卫（删掉它 ⇒ 下一句
                //     `v.gameObject` 直接 NRE）。所以「必须找到 null 槽」本身是 ① 的**鉴别力前提**。
                int probe = -1, above = -1, c0 = -1, c1 = -1;
                for (int i = cols; i < slots; i++)
                {
                    int up = i - cols;
                    if (up < cols) continue;                                // 还要能再往上取一格量行距
                    if (_rt.UiPoolSlotCardIndex(i) >= 0) continue;          // 要空槽
                    if (_rt.UiPoolSlotViewActive(i) != null) continue;      // 要**真 null** 槽
                    if (_rt.UiPoolSlotCardIndex(up) < 0) continue;          // 正上方那一格要装真卡（用来反推中心）
                    if (_rt.UiPoolSlotCardIndex(up - cols) < 0) continue;   // 再上面那一格也要装真卡（量行距）
                    c0 = up - cols; c1 = up;
                    probe = i; above = up; break;
                }
                CheckTrue(probe >= 0,
                          "★ A1495 灭自证（前提）：滚到底之后存在**真 null 空槽**（`_poolIndex < 0` 且视图为 null），"
                        + "而且它正上方那一格装着真卡"
                        + "｜🧨 若这一条红（只有「未激活视图」的空槽 / 卡池张数正好是格数整数倍）⇒ 下面 ① 的鉴别力就没了，"
                        + "先检查夹具或 `RefreshPool` 的空槽处置");
                CheckTrue(c0 >= 0 && c1 >= 0,
                          "★ A1495 灭自证（前提）：同一列有**两格连续的真卡**（用来量行距 —— 别拿我们自己的常量当尺子）");

                if (probe >= 0 && c0 >= 0 && c1 >= 0
                    && _rt.UiPoolCellRect(above, out float ax, out float ay, out _, out _)
                    && _rt.UiPoolCellRect(c0, out _, out float cy0, out _, out _)
                    && _rt.UiPoolCellRect(c1, out _, out float cy1, out _, out _))
                {
                    float pitch = cy1 - cy0;                 // 行距 = 相邻两行同列真实框心之差
                    float ex = ax, ey = ay + pitch;          // 空槽（正上方那一格）的中心
                    CheckTrue(pitch > 0f, $"（前提）行距量得出来（{pitch:F1}px > 0）");
                    // 闸（与 `HandlePointer` 同一对，`UiClickPoolPx` 逐句照抄）—— 它们为真时点击**进不了卡池那一站**
                    CheckTrue(!_rt.ModalPopupOpen && !_rt.UiWindowShield,
                              "（前提）两道闸都开着（没有模态消息窗 / 没有别的窗压着）"
                            + "—— 否则 `UiClickPoolPx` 在进卡池之前就返回 false（下面两条会变成假绿/假红）");

                    // ---- ① 点空槽格中心 ⇒ **没人吃**，而且 **这一下没有异常** ----
                    //   🔴 判据是「**该次调用有没有 Exception**」，⛔ 不是只看返回值 ——
                    //     `A1495` 那道 NRE 恰恰是**在返回 false 的路上**炸的（`!v.gameObject.activeSelf`）。
                    var clickLogs = new List<string>();
                    Application.LogCallback sink1 = (condition, stackTrace, type) =>
                        clickLogs.Add(type + ": " + (condition ?? ""));
                    Application.logMessageReceived += sink1;
                    bool ateEmpty;
                    try { ateEmpty = _rt.UiClickPoolPx(ex, ey); }
                    finally { Application.logMessageReceived -= sink1; }
                    string exceptions = null;
                    foreach (var l in clickLogs)
                        if (l != null && l.StartsWith("Exception: ")) { exceptions = l; break; }
                    CheckTrue(!ateEmpty,
                              $"★ A1495 ①：点**空槽**（第 {probe} 格）的中心 ({ex:F0},{ey:F0}) ⇒ **没人吃这一下**"
                            + "｜🧨 `HandlePoolClick` 把 `_poolIndex[i] < 0` 那一截判漏 ⇒ 这里变 true（点空格等于点到了卡）⇒ 红");
                    CheckTrue(exceptions == null,
                              "★ A1495 ①：……而且**这一下没有抛任何异常**"
                            + (exceptions != null ? $"（实得：{exceptions}）" : "")
                            + "｜🧨 删掉 `HandlePoolClick` 里 `v == null ||` 那截守卫 ⇒ 这条红（NRE），"
                            + "而**只看返回值**的上一条**照样绿** —— 这正是 `A1495` 当时漏掉的那一半");

                    // ---- ④ 正向控制：点**真卡**中心 ⇒ **吃** ----
                    //   与 ① 成对 ⇒ 「一律 return false」那种错法在这里必红。
                    CheckTrue(_rt.UiPoolCellRect(above, out float px0, out float py0, out _, out _),
                              "（前提）空槽正上方那一格量得出来");
                    CheckTrue(_rt.UiClickPoolPx(px0, py0),
                              $"★ A1495 ④ 正向控制：点**真卡**（第 {above} 格）的中心 ({px0:F0},{py0:F0}) ⇒ **吃**了这一下"
                            + "｜🧨 `HandlePoolClick` 改成「一律 return false」/ 视口框算错 ⇒ 本条红"
                            + "（① 与 ④ 结构上不可能同时被同一种错法满足）");

                    _rt.UiScrollPool(-100000f);              // 收尾：回池顶
                }
            }

            // ---- 🆕 2026-10-13（A1702 ②）：卡池滚一格**只建新进来的那一行**（原来重建 24 张）----
            //   改前：复用判据**按槽号**比（`_poolViewIds[vi] != def.Id`）。而槽 `vi` 装的是卡表里的
            //     `(firstRow + r) × cols + c` ⇒ **`firstRow` 一进 1，24 格的 `idx` 全变** ⇒ 24 格全落重建支
            //     （24 次 `Destroy` + 24 次 `CardView.Create`；每建一张 ≈2.2 秒的口径见账 `A1628`）。
            //   改后：判据**按【这张卡】找视图** —— 卡还在这一屏里 ⇒ 把它原来那颗 `CardView` **实例搬过来**
            //     （只换槽号），滚出这一屏的才销毁。**「卡变了就重建」那条语义原样保留。**
            //   🔴 **三条连起来才有鉴别力（灭自证）**：
            //     ① 留下来的那些格 = **同一批实例**（`==` 比 instance ID，两边都在世 ⇒ 等价引用相等）；
            //     ② 滚出去的那些格**真被销毁**（被 `Destroy` 过的 `UnityEngine.Object` 与 `null` 相等）；
            //     ③ 卡内容真变了的那一格**仍然是新建的实例**。
            //   🧨 「不销毁、只 `SetData`」那种无脑复用 ⇒ ② 红（而且卡池会渲成一排空卡位）；什么都不改 ⇒ ① ③ 红。
            {
                Section("★ A1702②：卡池滚一格只建新进来的那一行（不再重建 24 张）＋灭自证三条");
                _rt.UiSetSmallScreenUIForTest(false);
                _rt.UiSetTab(0);
                _rt.UiClearFilters();
                _rt.UiScrollPool(-100000f);                     // 回到池顶（下面按「正好一整行」算）
                int visN = _rt.State.VisibleCards().Count;
                CheckTrue(visN >= 24, $"（前提）卡池至少 24 张（实得 {visN}）—— 要 24 格全在才比得出「哪几颗留下来了」");
                CheckTrue(_rt.MaxPoolScrollPx >= 384f,
                          $"（前提）卡池至少滚得动一整行（上限 {_rt.MaxPoolScrollPx:F0} px ≥ 384 = 桌面档一个格高）");

                // 滚之前：24 格每一颗视图的**引用**（它是「还是不是同一批实例」唯一的尺子）
                // ⚠️ 名字别叫 `before` —— 本方法前面（`UiScrollPool` 那一段）已经有一个同名局部量。
                var poolBefore = new GameObject[24];
                int miss = 0;
                for (int i = 0; i < 24; i++) { poolBefore[i] = PoolCellGo(i); if (poolBefore[i] == null) miss++; }
                Check(miss, 0, "（前提）池顶 24 格**都建出来了**（下面按 24 颗比）");
                CheckTrue(_rt.UiPoolCardAt(0) != null && _rt.UiPoolCardAt(6) != null && _rt.UiPoolCardAt(23) != null,
                          "（前提）第 0 / 6 / 23 格都指着真卡（下标与此同 —— 滚完第 6 与第 23 格那两颗要搬到第 0 / 17 格）");

                _rt.UiScrollPool(384f);                          // **正好一整行**（桌面档格高 384）
                Check(_rt.PoolScrollPx, 384f,
                      "（前提）滚到 384px = 正好一整行（`firstRow` 0 → 1；没被滚动上限夹住）");

                // ① / ② —— 逐颗对：还在池里的算「留下来」，被销毁过的与 `null` 相等
                int kept = 0, destroyed = 0, orphan = 0;
                for (int i = 0; i < 24; i++)
                {
                    if (poolBefore[i] == null) { destroyed++; continue; }
                    bool inPool = false;
                    for (int j = 0; j < 24; j++)
                    { var g = PoolCellGo(j); if (g != null && g == poolBefore[i]) { inPool = true; break; } }
                    if (inPool) kept++; else orphan++;
                }
                Check(orphan, 0, "★ A1702② 灭自证②：**没有「还活着却不在卡池里」的幽灵**"
                               + "（滚出这一屏的必须销毁 —— 既不销毁、又没搬进新一屏 ⇒ 这一条 > 0 ⇒ 红）");
                Check(destroyed, 6, "★ ……滚出去的是**顶上那一整行 6 颗**，真被销毁了"
                                  + "｜🧨 「不销毁、只 `SetData`」那种无脑复用 ⇒ 这里是 **0** ⇒ 红");
                Check(kept, 18, "★ A1702② ①：**18 颗还是原来那批实例**（滚之前第 6…23 格那 18 颗 —— 换了槽号、没重建）"
                              + "｜🧨 照旧的「按槽号比」写法 24 格全重建 ⇒ 这里是 **0** ⇒ 红");
                CheckTrue(poolBefore[0] == null,
                          "★ A1702② ②（点名的这一颗）：滚之前**第 0 格**（卡表下标 0）那颗**真被销毁了**");
                CheckTrue(poolBefore[6] != null && poolBefore[6] == PoolCellGo(0),
                          "★ ……而滚之前**第 6 格**（下标 6）那一颗**搬到了第 0 格**（同一个实例、只换了槽号）"
                        + "｜🧨 全重建 ⇒ 第 0 格是一颗新实例 ⇒ 红");
                CheckTrue(poolBefore[23] != null && poolBefore[23] == PoolCellGo(17),
                          "★ ……第 23 格（下标 23）那一颗搬到了第 17 格（同一个实例）");

                // ③ **建了几张** —— 新滚进来的那一行（下标 24…29），⛔ 不是 24
                int expectNew = Mathf.Clamp(visN - 24, 0, 6);
                Check(_rt.UiPoolCreatedLastRefresh, expectNew,
                      $"★ A1702② ③：滚**整整一行**只建 **{expectNew}** 张（= 新滚进来的那一行；卡池共 {visN} 张）"
                    + "｜🧨 改前那条「按槽号比」的判据 ⇒ 这里是 **24** ⇒ 红");

                // ④ 同一条式子**内部**再滚（`firstRow` 不变）⇒ **一张都不建**
                _rt.UiScrollPool(144f);
                Check(_rt.UiPoolCreatedLastRefresh, 0,
                      "★ ……同一行内滚 144px（`firstRow` 仍是 1）⇒ **一张都不建**"
                    + "｜🧨 把复用判据改成「每次刷新都重建」⇒ 24 ⇒ 红");

                // ⑤ **往回滚**一整行：同样按「搬」走（只验往前搬对、往回搬错 ⇒ 这里红）
                var atRow2 = new GameObject[6];
                for (int i = 0; i < 6; i++) atRow2[i] = PoolCellGo(i);   // 此刻第 0…5 格装的是下标 6…11
                _rt.UiScrollPool(-144f);
                Check(_rt.PoolScrollPx, 384f, "（前提）先回到 384px");
                _rt.UiScrollPool(-384f);
                Check(_rt.PoolScrollPx, 0f, "（前提）再回到池顶（0px）");
                Check(_rt.UiPoolCreatedLastRefresh, 6,
                      "★ A1702② ⑤：**往回**滚一整行同样只建 **6** 张（重新露头的那一行）");
                CheckTrue(atRow2[0] != null && atRow2[0] == PoolCellGo(6),
                          "★ ……而且「下标 6」那一颗**搬回了第 6 格**（同一个实例）");

                // ⑥ **「卡内容真的变了」那一格仍然重建**（灭自证第三条）。
                //    造法 = 加一个**必定把第 0 格那张卡排除掉**的筛选：挑一个「稀有度/类型与它不同」的卡，
                //    按那个值筛 ⇒ 筛出来的第一张**必定不是**原来那张 ⇒ `Id` 对不上 ⇒ 该销毁重建。
                var allNow = _rt.State.VisibleCards();
                string exclKey = null;
                for (int i = 1; i < allNow.Count && exclKey == null; i++)
                {
                    var c2 = allNow[i];
                    if (!string.IsNullOrEmpty(c2.Rarity)
                        && !string.Equals(c2.Rarity, allNow[0].Rarity ?? "", StringComparison.OrdinalIgnoreCase))
                        exclKey = "$rar:" + c2.Rarity;
                    else if (!string.IsNullOrEmpty(c2.Type) && c2.Type != allNow[0].Type)
                        exclKey = "$type:" + c2.Type;
                }
                CheckTrue(exclKey != null,
                          $"（前提）卡池里有「另一个稀有度（或另一个类型）」的卡可以拿来筛"
                        + $"（第 0 格 `{allNow[0].Name}` 是 `{allNow[0].Rarity}`/`{allNow[0].Type}`）");
                if (exclKey != null)
                {
                    var goBefore = PoolCellGo(0);
                    var defBefore = _rt.UiPoolCardAt(0);
                    _rt.UiFilterRow(exclKey);
                    var defAfter = _rt.UiPoolCardAt(0);
                    CheckTrue(defAfter != null && defAfter != defBefore,
                              $"★ A1702② 灭自证③（前提）：筛 `{exclKey}` 之后第 0 格**换了另一张卡**"
                            + $"（{defBefore?.Name} → {defAfter?.Name}）—— 那个值把原来那张排除了 ⇒ 必定不同");
                    CheckTrue(goBefore == null,
                              "★ A1702② 灭自证③：**卡内容变了的那一格，旧实例被销毁**"
                            + "｜🧨 「不销毁、只 `SetData`」那种无脑复用 ⇒ 它还在 ⇒ 红");
                    CheckTrue(PoolCellGo(0) != null && PoolCellGo(0) != goBefore,
                              "★ ……而且第 0 格现在是一颗**新建的实例**（不是原来那颗）"
                            + "｜🧨 复用判据里把 `Id` 那一比删掉 ⇒ 还是老那颗 ⇒ 红");
                }
                _rt.UiClearFilters();
                _rt.UiScrollPool(-100000f);                     // 收尾：清筛选 + 回池顶
            }

            // ============================================================ 🆕 2026-10-21（`A1739`）
            // **换档（24 ⇄ 20）多出来的那几格 ⇒ 只关不销毁**（`A1702②` 那趟收口时被顺手改成了销毁）。
            //   判据 = 语义不同：「**滚出这一屏** ⇒ 销毁」管的是**滚动 / 换卡**（卡表几百张 ⇒ 必须有上界）；
            //   **换档不是滚出视口，是屏幕档位变了**（多出来的格数有上界 ≤ 4），拨回来时它们**正好又是
            //   那一屏要的那几张卡** ⇒ 现在 park 在 `_poolViews` 尾巴上、下一趟按【它绑的卡】搬回来。
            // 🔴 **灭自证**：④「一张都不建」与 ③「实例还活着」是**两条腿** ——
            //   · 改回「不在新一屏就销毁」⇒ ③ 红（`g20 == null`）、④ 也红（要重建 4 张）；
            //   · 改成「什么都不销毁、无脑复用」⇒ 上面 `A1702②` 那一节的 `destroyed == 6` 与
            //     `灭自证③`（换卡那一格仍新建）会红，两条合起来才把语义钉成「**只销毁真滚出去的**」。
            {
                Section("★ A1739：换档 24 ⇄ 20 那几格**只关不销毁** —— 拨回来是【搬】、一张都不建");
                _rt.UiSetSmallScreenUIForTest(false);
                _rt.UiSetTab(0);
                _rt.UiClearFilters();
                _rt.UiScrollPool(-100000f);
                int visN = _rt.State.VisibleCards().Count;
                CheckTrue(visN >= 24,
                          $"（前提）卡池 ≥ 24 张（实得 {visN}）—— 24 格全在才比得出「多出来的那 4 格」");
                var g20 = PoolCellGo(20);
                CheckTrue(g20 != null && _rt.UiPoolCellRect(20, out _, out _, out _, out _),
                          "（前提）桌面档第 21 格（`pool_20`）**有视图、在画**（滚到池顶时它装的是卡表下标 20）");

                _rt.UiSetSmallScreenUIForTest(true);             // 24 → 20
                Check(_rt.UiPoolCreatedLastRefresh, 0,
                      "★ A1739①：换到小屏档**一张都不建**（那一屏要的卡都在，是搬过去的）");
                CheckTrue(!_rt.UiPoolCellRect(20, out _, out _, out _, out _),
                          "★ A1739②：多出来的那一格**不画了**（`pool_20` 这个名字已让开）");
                CheckTrue(g20 != null,
                          "★ A1739③：…但**实例还活着**（只关不销毁）"
                        + "｜🧨 换档时把多出来的槽销毁 ⇒ 这一条红（`g20` 与 `null` 相等）");

                _rt.UiSetSmallScreenUIForTest(false);            // 20 → 24
                Check(_rt.UiPoolCreatedLastRefresh, 0,
                      "★ A1739④：拨回桌面档**也一张都不建**（那 4 格是 park 着的、按卡搬回来）"
                    + "｜🧨 换档时把多出来的槽销毁 ⇒ 这里是 4 ⇒ 红");
                CheckTrue(g20 != null && g20 == PoolCellGo(20),
                          "★ A1739⑤：换回来那一格 = **同一个实例**（原实例、没重建；🧨 销毁过的话它已与 `null` 相等）");
            }

            // ============================================================ 🆕 2026-10-21（`A1740`）
            // **卡池命中走 O(1) 反解格号**（原来 `HandlePoolClick` / `TickTooltipAt` 都是 **24 槽全遍历**，
            //   后者**每帧都跑**：`Update` → `TickTooltip` → `TickTooltipAt`，`Update` 里没有闸）。
            // 🔴 **判别式**：拿 `UiPoolCellRect(i)` 量出来的**每一格【真实的】框心**喂回反解，
            //   必须得到 **`i` 自己**（两档都跑、再滚半行跑一遍）—— 正反两条式子漂开一格就红。
            //   ⛔ 这里**不许**拿 `DeckRuntime` 的常量自己算期望值（那就是自证）。
            // 🔴 **灭自证**：③ 咬的是**边界闸**（最后一列右缘之外 ⇒ 不属于任何一格）——
            //   少了 `c < Cols` 那道闸，反解会**绕到下一行第 0 格**去命中一张不该命中的卡。
            {
                Section("★ A1740：卡池命中反解格号（两档逐格对 + 滚半行再对 + 边界闸）");
                _rt.UiSetTab(0);
                _rt.UiClearFilters();
                for (int s = 0; s < 2; s++)
                {
                    bool small = s == 1;
                    _rt.UiSetSmallScreenUIForTest(small);
                    _rt.UiScrollPool(-100000f);                  // 回池顶（`off = 0` 那一档）
                    int n = _rt.UiPoolColsNow * _rt.UiPoolRowsNow;
                    int bad = 0, live = 0; string firstBad = null;
                    for (int i = 0; i < n; i++)
                    {
                        if (!_rt.UiPoolCellRect(i, out float cx, out float cy, out _, out _)) continue;
                        live++;
                        int got = _rt.UiPoolCellAt(cx, cy);
                        if (got != i) { bad++; if (firstBad == null) firstBad = $"格 {i} 的框心 ({cx:F1},{cy:F1}) 反解成 {got}"; }
                    }
                    CheckTrue(live >= 4, $"（前提）{(small ? "小屏" : "桌面")}档量到了 {live} 格（≥ 4 才有鉴别力）");
                    Check(bad, 0,
                          $"★ A1740①（{(small ? "小屏" : "桌面")}档 {live} 格）：每一格的**真实框心**反解回来 = **它自己**"
                        + (bad == 0 ? "" : $"；反例：{firstBad}")
                        + "｜🧨 正反两条式子漂开一格（列数/`PadX`/`off` 任一处）⇒ 红");

                    // ③ 边界闸（咬 `c >= Cols` 与 `r < 0` 这两道）——
                    //    ⛔ **只断 `UiPoolCellAt`**：`UiPoolHit` 那一半在这里**判别不了** ——
                    //    少了闸时反解绕到的那张卡离这一点 **1400px 开外**，`CardView.Contains` 恒假
                    //    ⇒ 拿它当证据就是**假绿**（本项目最怕的那种绿）。
                    if (_rt.UiPoolCellRect(_rt.UiPoolColsNow - 1, out float lx, out float ly, out float lw, out _))
                    {
                        float ox = lx + lw * 0.5f + 2f;                  // 最后一列**右缘之外** 2px
                        Check(_rt.UiPoolCellAt(ox, ly), -1,
                              $"★ A1740③ 灭自证：最后一列**右缘之外** 2px（x={ox:F1}）**不属于任何一格**"
                            + $"｜🧨 少了 `c ≥ Cols` 这道闸 ⇒ 反解绕到**下一行第 0 格**（= {_rt.UiPoolColsNow}）⇒ 红");
                    }
                    if (_rt.UiPoolCellRect(0, out float bx0, out float by0, out _, out float bh0))
                    {
                        Check(_rt.UiPoolCellAt(bx0, by0 - bh0), -1,
                              $"★ A1740③b 灭自证：**第 0 行上沿再往上整整一格**（y={by0 - bh0:F1}）也不属于任何一格"
                            + "｜🧨 少了 `r ≥ 0` 这道闸 ⇒ 反解出一个**负格号** ⇒ 红");
                    }

                    // ② 滚**半行**（`off ≠ 0`）再对一遍 —— 咬的是反解里 `off` 那一半
                    _rt.UiScrollPool(_rt.UiPoolCellHNow * 0.5f);
                    int bad2 = 0; string firstBad2 = null;
                    for (int i = 0; i < n; i++)
                    {
                        if (!_rt.UiPoolCellRect(i, out float qx, out float qy, out _, out _)) continue;
                        int got = _rt.UiPoolCellAt(qx, qy);
                        if (got != i) { bad2++; if (firstBad2 == null) firstBad2 = $"格 {i} ({qx:F1},{qy:F1}) → {got}"; }
                    }
                    Check(bad2, 0,
                          $"★ A1740②（{(small ? "小屏" : "桌面")}档）：滚半行（`off = 半格`）之后**仍然逐格对得上**"
                        + (bad2 == 0 ? "" : $"；反例：{firstBad2}")
                        + "｜🧨 反解漏掉 `off`（当成恒 0）⇒ 红");
                }
                _rt.UiSetSmallScreenUIForTest(false);            // 收尾：换回桌面档
                _rt.UiScrollPool(-100000f);
                Check(_rt.UiPoolColsNow, 6, "（收尾）回到桌面档 6 列");
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

        /// <summary>🆕 2026-10-13（A1702 ③）：卡池第 `i` 格那张卡**现在的裁切框**（`CardView.Clip`，画布 px）。
        /// `false` = **没裁**（`Clip` 为 `null`）。与 `<see cref="PoolCellGo"/>` 是同一棵树的同一颗 GO
        /// （`CardView.Create(Root, …)` 就把 `CardView` 挂在这个 `pool_N` 上）。</summary>
        static bool PoolClipOf(int i, out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = x2 = y2 = 0f;
            var go = PoolCellGo(i);
            var cv = go != null ? go.GetComponent<CardView>() : null;
            var clip = cv != null ? cv.Clip : null;
            if (!clip.HasValue) return false;
            x1 = clip.Value.x1; y1 = clip.Value.y1; x2 = clip.Value.x2; y2 = clip.Value.y2;
            return true;
        }

        /// <summary>🆕 2026-10-13（A1702 ③）：卡池第 `i` 格那张卡**真画出来的范围**（画布 px · 左上原点）——
        /// 逐**顶点**量所有开着且在激活链上的 `MeshRenderer`。
        /// <para>🔴 **为什么要它、而不只读 `CardView.Clip`**：那是**字段**（赋上了、网格没裁照样是那个值）
        /// —— 同族先例 = `Editor/CollectionScene.cs` 的 A198③（原话「要断的是**裁切的结果**，
        /// 不是那个字段的瞬时值」）与它那份逐顶点量法（A181 的判据原话：裁切这件事
        /// **量节点位置量不出来**，只有逐顶点换算才看得见）。</para>
        /// <para>⚠️ **跳过两类**：① `ImageQuad`（自己会 `RebuildMesh`；本窗的卡上只有一颗出厂关着的
        /// `glow_*`）；② `TextMeshPro` 那颗（它的 `sharedMesh` 在批处理下**不一定**吃到
        /// `ClipTmpMesh` 那一刀 —— `MenuDraw.TextClipUploadSkipped` 那条；文字那一半由收藏窗 A250 块
        /// 那条腿验，本宿主不重复）。</para>
        /// 返回 `false` = 一个顶点都没量到（= 整张卡被裁光了 / 那格没建）。</summary>
        static bool PoolDrawnExtentPx(int i, out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = float.MaxValue; x2 = y2 = float.MinValue;
            var go = PoolCellGo(i);
            if (go == null) return false;
            int n = 0;
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null || !mr.enabled || !mr.gameObject.activeInHierarchy) continue;
                if (mr.GetComponent<ImageQuad>() != null) continue;
                if (mr.GetComponent<TMPro.TextMeshPro>() != null) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var vs = mf.sharedMesh.vertices;
                if (vs == null) continue;
                for (int k = 0; k < vs.Length; k++)
                {
                    var p = LayoutSpace.ToDesignPixel(mr.transform.TransformPoint(vs[k]));
                    n++;
                    if (p.x < x1) x1 = p.x; if (p.x > x2) x2 = p.x;
                    if (p.y < y1) y1 = p.y; if (p.y > y2) y2 = p.y;
                }
            }
            return n > 0;
        }

        // ============================================================ 建场景

        /// <summary>自检用的临时存档路径。</summary>
        static string TempStorePath()
        {
            return System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "../Temp/_deckscene_selftest.json"));
        }

        /// <summary>🆕 **2026-10-14（`A1132` 第 6 步）**：**空场 + 那台相机** —— 本文件**两条建场路共用**
        /// （`Run()` 那条走 `Build(...)`；`BuildAndSaveScene()` 那条走「壳 + 窗」）。
        /// 🔴 **抽出来是为了「一份实现」**（CLAUDE.md §三）：改前这一段只存在于 `Build(...)` 里，
        /// 第 6 步要再建一次场 ⇒ 不抽就是**两份迟早不一致**（本项目反复踩过的那个坑）。</summary>
        static Camera NewSceneWithCamera()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            // ⚠️ 宽高比必须在建任何东西之前定死 —— 位置/尺寸都是建的时候算一次，
            //    批处理下相机默认 4:3，建完再改 16:9 会整片错位（踩过）
            cam.aspect = LayoutSpace.DesignAspect;
            LayoutSpace.Apply(cam);
            return cam;
        }

        /// <param name="lib">卡组库。**自检必须传临时路径上的库** —— 不能动玩家的真存档。</param>

        // 🔴 **2026-10-17（N16 撤回，铁律 5）**：这里原来有 `TestRankedEmbeddedDeckInfo()` + `CheckNodeRectPx()`，
        //   断的是原版 **V1** `RankedEventWindow ◂ Deck info` 那棵树。撤回理由见 `Run()` 里那段注释：
        //   V1 是旧窗、原版事件只有 V2/V3、我们实现的是现役 V2 ⇒ 那些断言等于给死件钉钉子。

        static DeckEditorState Build(DeckLibrary lib, out Transform root)
        {
            NewSceneWithCamera();     // 🆕 第 6 步起与 `BuildAndSaveScene` **共用一份建场**（见那个方法）

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

        /// <summary>截图。🆕 **2026-10-18（`A1007`）起补上「空图护栏」** —— 本文件原来**没有**它
        /// （`guardBlank: false`、也没有 `MeanBrightness`）⇒ 一张**全黑**图也能安静地写出去
        /// （2026-09-23 在别的宿主上踩过：上一个窗的 `CloseAllWindows()` 把要拍的那个窗也关了）。
        /// 现在照另 5 份接上：`guardBlank: true` + 本文件自己一份 `MeanBrightness`。
        /// ⚠️ `MeanBrightness` 的口径照 **Collection / Rewards / Shell / Shop** 四份
        /// （`long` 累加 + 分母 `(px.Length + 6) / 7`）—— ⛔ **别抄 `SettingsScene` 那一份**：
        /// 它是 `double` + `px.Length / 7.0` 且**没有 null 护栏**，传 `null` 会 NRE。</summary>
        static void Shoot(string file, bool allowBlank = false)
            => MenuCheck.Shoot(_sink, ShotDir, file, allowBlank, guardBlank: true, meanBrightness: MeanBrightness);

        /// <summary>平均亮度（0–255）—— **逐宿主一份**（⛔ 没有合并成共享实现，理由见 `MenuCheck.cs` 的
        /// `Shoot` doc：合并会动 `{lum:F1}` 那一行、而本族的回归判据是 stdout 逐字节相同）。
        /// 口径与另四份逐字相同：`long` 累加 + **分母 = 采样数**（步长 7 ⇒ `(px.Length + 6) / 7`）。</summary>
        static float MeanBrightness(Texture2D t)
        {
            if (t == null) return 0f;
            var px = t.GetPixels32();
            if (px.Length == 0) return 0f;
            long sum = 0;
            for (int i = 0; i < px.Length; i += 7) sum += px[i].r + px[i].g + px[i].b;
            return sum / 3f / ((px.Length + 6) / 7);
        }

        static void SaveScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            SceneStubAudit.Run(ScenePath);   // 🆕 A1506：存盘后自证「不许有 !u!115 存根」（干净时零输出）
            Debug.Log(P + $"  场景 {ScenePath}");
        }

        /// <summary>把 `DeckEditor.unity` 加进 `EditorBuildSettings`（幂等）—— **直起那条路靠它**
        /// （`SceneManager.LoadScene("DeckEditor")` 没在 Build Settings 里会抛）。照 `MainMenuScene.AddToBuildSettings`。
        /// 可以单独跑：`-executeMethod DeckScene.AddToBuild`。
        /// ⚠️ **2026-10-14（`A1132` 第 6 步）就地订正（铁律 5）**：本行原写「**收藏窗的「进编辑」靠它**」——
        /// 第 3 步起「进编辑」**改开窗、不再切场景** ⇒ 那条 `LoadScene("DeckEditor")` 已经降级成
        /// **没壳时**的兜底（直起 / 批处理里没建壳那几条）。**场景仍然必须留在 Build Settings 里**，
        /// 判据 = `Core/PlayerBoot.cs` 的 `LoadSceneMatching` 按名字找场景、找不到就 `LogError`
        /// （`-wfscene DeckEditor` 走的就是它）。⛔ **别因为「生产不再用它」就把这一跳删掉。**</summary>
        public static void AddToBuild()
        {
            const string path = "Assets/CardPresentation/Scenes/DeckEditor.unity";
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in list)
                if (s.path == path) { Debug.Log(P + "  `DeckEditor.unity` 已在 Build Settings 里"); return; }
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log(P + "  已把 `DeckEditor.unity` 加进 Build Settings（直起 `-wfscene DeckEditor` 要靠它）");
        }

        /// <summary>建出场景存盘，给人**打开按 Play** 用（`-executeMethod DeckScene.BuildAndSaveScene`）。
        ///
        /// <para>🔴 **2026-10-14（`A1132` 第 6 步）改的是它的【定位】**：`DeckEditor.unity` **保留**，
        /// 从「**生产入口**」降级成「**直起 / 自检用的壳**」。
        /// 判据（`D10` §三）：`Core/PlayerBoot.cs` 的 `LoadSceneMatching` 按**名字含子串**在 Build Settings 里
        /// 找场景，**找不到就 `LogError`** ⇒ **删了它，`-wfscene DeckEditor`（自检与真机都走它）当场报错**。
        /// ⇒ 本方法现在建的是「**壳根 + `WindowsManager.EnsureHost()` + 直开那扇窗**」，
        /// 与 `Editor/CollectionScene.cs` 那条现成先例**同形**（单独打开某个界面场景 ⇒ 自己 `EnsureHost` 再开窗），
        /// ⛔ **不是**再造一棵裸 `DeckRuntime`（那是第 3 步之前的生产形状）。</para>
        ///
        /// <para>⚠️ **`Run()`（自检那条腿）一个字都不变**：它仍然走 `Build(...)` 的裸 `DeckRuntime`
        /// （= `HostWindow == null` 那条腿，第 4 步刚给它补了 5 条断言）。
        /// ⇒ **本文件现在有两条建场路**（见 <see cref="NewSceneWithCamera"/>）：`Run()` 走 `Build`、
        /// 本方法走「壳 + 窗」。⛔ **别把它们合并**（合了就把自检那条腿换成被托管那条，
        /// 那 5 条「独立场景」断言的**前提**当场不成立）。</para></summary>
        public static void BuildAndSaveScene()
        {
            Directory.CreateDirectory(ShotDir);
            AddToBuild();      // 🔴 2026-09-23：`DeckEditor` **原来不在 Build Settings 里** ⇒
                               //    收藏窗「进编辑」那一步 `SceneManager.LoadScene("DeckEditor")` 会抛
            NewSceneWithCamera();                       // 与 `Build(...)` 共用一份建场（第 6 步抽出来的）
            // ⚠️ 这条路用**玩家的真存档**（打开场景按 Play 时就是要编辑自己的卡组）——
            //    `DeckEditingWindow.Open()` 里面那句 `DeckLibrary.Load()` 也是读这里。
            var wm = WindowsManager.EnsureHost();       // 三颗锚点 + 管理器 + 指针层（幂等，同 `CollectionScene` 那条先例）
            var deWin = DeckEditingWindow.OpenFrom(wm); // 直开本窗（= 第 3 步那条生产落点）
            var root = deWin != null ? deWin.transform : null;
            _rt = deWin != null ? deWin.Runtime : null;
            _root = root;
            // 🔴 2026-09-20：**场景里必须挂着 `DeckRuntime`** —— 界面是运行时的
            //    `DeckRuntime.Start()` 建的（编辑器里 `Open()` 只是把绘制转发过去、直接调），
            //    忘了挂组件的话**按 Play 出来是一片黑**，而自检看不见这个错
            //    （它直调 `Build`，和 Play 那条路不是同一个入口 —— 本项目踩过同形的坑）。
            //    ⚠️ 第 6 步起 `root` = **那扇窗的窗根**（不再是场景根那颗 "DeckEditor"）——
            //    本条的判据不变（那颗 `DeckRuntime` 挂在窗根上），但**红的话含义变了**：
            //    先去查 `DeckEditingWindow.OpenFrom` 有没有真的开出来。
            CheckTrue(root != null && root.GetComponent<DeckRuntime>() != null,
                      "DeckEditor.unity 的**窗根**上挂着 DeckRuntime（不然按 Play 是一片黑）");
            CheckTrue(deWin != null && deWin.IsOpen() && deWin.Runtime != null && deWin.Runtime.HostWindow == deWin,
                      "★ A1132 第 6 步：`DeckEditor.unity` 里那扇窗**开着、且 `DeckRuntime` 认它当宿主**"
                    + "（= 直起 `-wfscene DeckEditor` 走的是**被窗托管**那条腿；`Run()` 那条自检腿仍是裸 `DeckRuntime`）");
            Shoot("deck_editor.png");
            SaveScene();
            Debug.Log(P + $"=== 场景已重建：{ScenePath}"
                        // ⚠️ 第 6 步起这里读的是**窗里那台** `DeckRuntime` 的 `State`
                        //    （改前是 `Build(...)` 的返回值；同一个语义，换了持有人）。
                        + (_rt != null && _rt.State != null
                           ? $"（{_rt.State.PoolCount} 张卡池，演示卡组 {_rt.State.Deck?.Name}）==="
                           : "（⚠️ 拿不到 `DeckRuntime.State` —— 窗没开出来，见上面那条 ★ 断言）"));
        }
}
