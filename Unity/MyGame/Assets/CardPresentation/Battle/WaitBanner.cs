// WaitBanner.cs — 「等待提示」（原版 `WaitText`）
//
// 原版出处：运行时 dump `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive_0912.tsv:350-355`
//   `BackCanvas/Safe area BackCanvas/WaitText`  anchor (0.5,1) · anchoredPosition **(7.0, −175.1)**
//                                              size **1344.0 × 79.4** · activeSelf=**False**
//     ├── `Dark Shade`              size **3963.5 × 3366**（**无图，纯色**，`color = (0,0,0,0.6118)`）
//     └── `Generic Popup Background` size **1323 × 90**  图 `40k_popup`
//           ├── `Mask`                                图 `40k_popup`（拉伸填满）
//           │     └── `Background fill`               图 `40k_popup_texture`（128×128）
//           └── `Text`  anchor(0,1) pos(661.5,−45) size **1269.5 × 70**
//
// 🔴 **2026-10-24（执行代理 `W1` · 判据 `资料/普查产出_1024d/N1_敌方回合横幅与压暗.md` §2 — 铁律 5 就地订正）**：
//   **下面①「什么时机显示」那句「原版查不到」【不成立】，原版查得到，而且和我们做的完全不是一回事：**
//   · 这一件在原版叫 **`BattleManager.mulliganWaitText`**（组件字段名就是它，偏移 `+0x1F0`），
//     它是**「换牌等对手」那一下的过渡提示**，**不是「敌方回合横幅」**；
//   · 唯一活的显示点 = `BattleManager__ClickChosenCardDone.c:93-98`：**联网局**里本地玩家确认换牌
//     （`IsNetworkedGame`，`:55`）且 **`receivedEnemyMulligan == false`** ⇒ `SetActive(true)`；
//   · 关闭边沿 4 条：对手换牌包到达（`ReceiveEnemyMulliganAndEnvEffect:55-58`）· 环境卡阶段 ·
//     战斗阶段开始 · 初始化。**回合循环 `NextTurn` 一次都不碰它**；
//   · **单机局永不出现**：`_SetupEnviromentalEffectPhase:32-46` 在非联网那一支把 `receivedEnemyMulligan`
//     **强置 1** ⇒ 显示分支根本进不去（另一条更是死支：`ShouldFadeMulliganCards()` 恒 false）。
//   ⇒ 所以「整段敌方回合一直压暗」**是我们的缺陷**，判据（三态谓词）落在 `BattleDriver.ShowWaitBanner`。
//   视图件本身（尺寸 / α / 无图纯色 / 文案键）**仍然全对**，一个字都不用改。
//
// 🔴 **这份文档里剩下那一处「我们挑的」** 只有 ②（文案）—— 而它 **2026-10-18 也已经订正过了**
//   （原版 `mTerm` = `Battle/Mulligan/WaitEnemy`，见下），所以本件**今天一条「我们挑的」都不剩**。
//   ② **文字文案** —— 🔴 **2026-10-18（第十二轮 · W6）就地订正（铁律 5）**：这里原来写
//      「dump 里 `Text` 节点是**空的**（运行时才赋），原版的本地化 key 没解出来 ⇒ 这条是我们加的词条」
//      —— **整句不成立**。原版那颗节点的**字与键都在**：
//        · 父链 `BackCanvas < WaitText < Generic Popup Background < Text`（与上面 dump 逐级对得上）；
//        · TMP 原文 = **`Waiting for enemy`**；
//        · `mTerm` = **`Battle/Mulligan/WaitEnemy`**
//          （实据 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/
//            MonoBehaviour_4804.json` 的 `mTerm` + 同族那颗 TMP，2026-10-18 亲读；13 个 arena 每场 1 颗）。
//      **错因**：当年只看了运行时 dump（那一刻节点是空的），**没回 prefab 里找 `Localize.mTerm`**。
//      ⇒ 现在走 `Loc.T(WaitTerm)`（键名照原版），**不再是我们的词条**。
//      ⚠️ 中文那一列（`等待敌人`）**已换成官方 zh-CN 表的值**（2026-10-14；原来是我们的译法「等待对手」），出处写在 `Core/Loc.cs` 那一行。
//
// ⚠️ **底板：2026-09-17 已改回原版**（原来写「我们没九宫格 ⇒ 自建实底」，见 `Build()` 里那段更正）——
//    现在用 `MenuDraw.Nine`（`40k_popup`，Sliced）+ `MenuDraw.Tiled`（`40k_popup_texture`，Tiled）。
//    两件原版事实是从场景 JSON 里读出来的，不是猜的：
//    `Generic Popup Background.m_Type = 1`（Sliced）、`Background fill.m_Type = 2`（Tiled）。
//    🔴 **2026-10-04（A50③）两层的路都收口到 `MenuDraw` 了**（原来直调 `ImageQuad.Create*` = 绕开公共件、
//    拿不到 `clip`）：平铺那次是 A25⑤、框架这次。⚠️ 中心值给的是 `PopupRectPx()`（画布 px），
//    **z 由 `Build()` 里那两行 `localPosition` 显式补回来**（`MenuDraw.Local` 给的 z 恒为 `0 − 父件 z`）。
//    ⚠️ 2026-10-16（`T09` 块 6）：那两行现在在 **`BuildWorld()`** 里（世界档那一支，逐字未改）；
//       **画布档不靠 z 补** —— 那两层的落点与次序分别由 `HudCanvas.PosPx`/`PlaceAtBarCenter` 与
//       `HudCanvas.Add(rt, z)` 表达（见本节末那段）。
// ─────────────────────────────────────────────────────────────────────────────
// 🔴 **2026-10-16（第四十二会话 · `T09` 块 6 · 本件是那一波唯一有画面可验的）：迁进 uGUI 画布。**
//
//   【两层模型（判据 → `资料/普查产出_1016并行/S6S9_卡体与残件_施工方案.md` §2·1 / §3·2）】
//     原版 `WaitText` 挂在 `BackCanvas/Safe area BackCanvas` 上，而 `BackCanvas` 的
//     **`overrideSorting = false`** ⇒ 它的内容**并进根画布那一组**（不另立排序组）。
//     ⇒ 忠实做法 = **件直接挂根画布**（`HudCanvas.RootRect`）+ `HudCanvas.Add(rt, z)` 定兄弟序，
//       ⛔ **【不要】用 `EnsureSubCanvas` 造一块 `Default` 层的子画布** —— 那会落进一个
//       **本仓无先例、且 Unity 内部键判不了**的并列（根画布的 `sortingLayerID` 从未设过 ⇒ 也是 `Default`，
//       两者排序键逐位相同、还同平面；缺的判据 = 「三者全等时 Unity 拿什么排两个 canvas 组」，
//       坐实它要起一次 player，而现行口径不许）。
//     ⚠️ 因此本件**不进 `HudCanvas.SubCanvasRank`**（那张表只排「子画布彼此」的先后，本件没有子画布）。
//
//   【双模式】`Create(parent)` / `CreateWithArt(...)` 的 `uguiHost` **传了** ⇒ **画布档**（件挂它上面、
//     坐标/尺寸 = **设计 px**）；**不传 / 传 `null`（出厂）⇒ 世界档，与加本档之前逐位相同**
//     （先例 = `ErrorMessageBanner.Create(..., uguiHost)` · `Tooltip.UseHost` · `TutorialOverlay.Create(parent, uguiHost)`）。
//     ⚠️ 生产路的宿主由 `BattleDriver.BuildHud` 给（`HudCanvas.RootRect`）。
//
//   【画布档的形状】三层与框都**同心**（= 原版那条 `anchor (0.5,1)` + `ap (7.0, −175.1)`，见 `PlaceAtBarCenter`）：
//       root(原版 `WaitText`，1344×79.44) ── 只是本组件所在的容器（原版它也只是个容器）
//       ├ `wait_shade`   原版 `Dark Shade`（**无图、纯色** α0.6118，`sd 3963.5×3366`）
//       ├ `wait_popup`   原版 `Generic Popup Background`（`Image (Sliced)` 1323×90，border 169/160/169/160）
//       │   └ `wait_fillRoot` 原版 `Mask`（`sd 1323×90`）
//       │       └ `fill`      原版 `Background fill`（`Image (Tiled)`，`ppuMul 2` ⇒ 格宽 64 画布 px）
//       └ `wait_text`    原版 `Generic Popup Background/Text`（框 1270×70 · 自适应 [18, 55.3] · base 36）
//     ⚠️ 画布档与**世界档的树**只有一处不同：世界档里那三层/框挂在 `HudRoot` 下（本组件自己的节点下），
//        画布档里它们直接挂**根画布**、本组件节点退成不画东西的容器 —— 原因见上面那条「并进根画布那一组」。
//
//   ⚠️ **画布档与 HUD 的两套锚点约定**：`wait_shade` 走 `MakeImagePiece` 的**归一化锚点**
//     （与同一容器的 HUD 图族同一套）；框/字走**点锚**（`SetAnchorPx`，= 原版自己的五元组）。
//     两套在 16:9 下**逐位相同**，只在比 16:9 **高**的屏（4:3/3:2/16:10/竖屏）上差 135 屏 px
//     —— 那个差压在 3366 px 高的压暗层上**看不出来**（它盖满整屏这件事不受影响），
//     判据与通式 → `T09` §八·补（陷阱 1/3 与那条「误差通式」）。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;
using UnityEngine.UI;      // 🆕 画布档要读 `Image` 的两个字段：`type`（Tiled/Sliced）与 `pixelsPerUnitMultiplier`

namespace CardPresentation
{
    public class WaitBanner : MonoBehaviour
    {
        // ---- 原版字段（1080p 下 108 px = 1 世界单位，和 `SettingsPanel` 同一套换算）----
        const float BarDx = 7f, BarDy = -175.1f;     // `WaitText` 的锚 (0.5,1) 起算
        static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.6118f);   // 原版 `Dark Shade` 的色

        /// <summary>z —— 和 `SettingsPanel` 同一层（比结算面板靠前、比任何卡靠前）。
        /// <para>🔴 **2026-10-16（`T09` 块 6）**：画布档下它变成**兄弟序档**（`HudCanvas.Add` 的那个 `z`）。
        /// 两套的**方向相同**（`z` 大 ⇒ 画在底下）⇒ **值照搬**（口径 → `T09` §五·补⑩ 那张映射表）。</para>
        /// <para>🔴 **这一档为什么是它**（判据 = **原版事实**，不是成本）：原版 `WaitText` 在 `BackCanvas`
        /// 的子序是**最后一格（6/7）** ⇒ 它的压暗**盖住 `LeftArea`/`RightArea` 的名牌一族**、
        /// **盖不住** `FrontCanvas`（`OverHand`）与 `PopUps`。落到我们这边：「名牌一族」= HUD 的
        /// **图**那一族（`HudImageZ` 0.235~0.6）**和字**那一族（`HudLabelZ` = 0）
        /// ⇒ 本档必须**小于 `HudLabelZ`(0)** 才盖得住整整一族（字在图上，只压过图不够）。
        /// 而 `OverHand`/`PopUps` 那些扇窗各在自己的子画布上（**层先于 order 比**）⇒ 本来就在本件之上，
        /// **不受这个数影响** ✓。</para></summary>
        const float Z = -0.45f;

        static float U(float px) { return px / 108f; }

        /// <summary>🔴 **2026-10-16（`T09` 块 6）**：`ImageQuad` → **`HudPiece`**（适配层，两档共用同一个字段）。
        /// 世界档 = `HudPiece.Of(quad)`（用法一字不变）· 画布档 = `HudCanvas.MakeImagePiece(...)` 的返回值。</summary>
        HudPiece _shade;
        GameObject _popup;          // 原版 `Generic Popup Background`（九宫格框 + 平铺填充）
        GameObject _fillRoot;       // 填充那层的父节点（单独挂着，自检好分开数）
        Label _text;

        /// <summary>🔴 **画布档（uGUI）开关**（判据只此一处）—— `uguiHost != null` ⇒ true。
        /// ⚠️ **必须在 `Build()` 之前赋值**（`Build` 按它选档）。</summary>
        bool _ugui;

        /// <summary>原版 `Generic Popup Background` 的尺寸（**1323×90**）——
        /// ⚠️ 注意它**不是** 父节点 `WaitText` 的 1344×79.4：真正画出来的是这个子节点。</summary>
        const float PopupW = 1323f, PopupH = 90f;

        /// <summary>🔴 **2026-10-16（`T09` 块 6）：压暗层（原版 `Dark Shade`）的 `m_SizeDelta`**
        /// —— **3963.50 × 3366**，出处 = 运行时 dump `runtime_ui_dump_drive_0912.tsv:350-355`
        /// （与文件头那张子件表逐值相同；**无图、纯色**、`color = (0,0,0,0.6118)`）。
        /// <para>⚠️ 世界档那个尺寸（`LayoutSpace.DesignHeight × 1.05` 高、宽按可见宽的比例）是**我们自算的**，
        /// 两者在 16:9 下**都盖满整屏**（所以看不出差别）—— 画布档改取原版这个数。
        /// 它是个**以条心为中心**的巨大 quad：3:1 到 1:3 的任何常用宽高比都盖得住。</para></summary>
        const float ShadeW = 3963.5f, ShadeH = 3366f;

        /// <summary>🔴 **填充那层的 `m_PixelsPerUnitMultiplier`** = **2.0**（原版 `Background fill` 实读）——
        /// 它决定 uGUI `Image (Tiled)` 的格宽：`128 ÷ 2 = 64` 画布 px（推导见 `FillTilePx` 的注释）。
        /// ⚠️ 世界档靠 `MenuDraw.Tiled` 的 `tilePx` 形参表达同一个数（那边一格 = 一块 quad）。</summary>
        const float FillPpuMul = 2f;

        /// <summary>🔴 **根节点（原版 `WaitText`）自己的矩形** —— 2026-10-11（A218）为写 `sizeDelta` 立的常量，
        /// 值 = 运行时 dump 里 `WaitText` 的 `m_SizeDelta = (**1344.0, 79.44**)`
        /// （`runtime_ui_dump_drive_0912.tsv:350-355`；与 `bundle_scenes_scenes_battlearena1` 的
        /// `WaitText` 实读 `sizeDelta (1344.0000, 79.4400)` · `anchor (0.5,1)` · `ap (7.0, −175.1)` 逐位一致）。
        /// ⛔ 别拿 `PopupW/H` 顶替 —— 那两个是**子节点** `Generic Popup Background` 的。</summary>
        const float RootW = 1344f, RootH = 79.44f;

        /// <summary>这一层用的**渲染队列**。🔴 **必须显式给** —— 本件原来一次都没调过
        /// `SetRenderQueue`（= 材质默认档），而 `MenuDraw.Tiled` **会写**队列：
        /// 不传就等于在战斗现场**顺手换了一次排序**。3000 = `Sprites/Default` 的默认档
        /// （本件原本就是它），也是战斗侧 `CardDisplayWindow.QChrome` 那一档。</summary>
        const int BattleQChrome = 3000;

        /// <summary>提示条在**画布像素**里的矩形 —— 原版 `WaitText` 锚 `(0.5,1)` +
        /// `anchoredPosition (7.0, −175.1)`（`runtime_ui_dump_drive_0912.tsv:350-355`）
        /// ⇒ 中心 = (960 + 7, 175.1)（**自上而下量**），宽高取上面那对。
        /// ⚠️ 给 `MenuDraw.Nine` / `MenuDraw.Tiled` 用（它们吃画布 px、不吃世界坐标）。
        /// 🔴 **现在只有宽高是必需的**：两层的落位都由 `Build()` 里随后那行 `localPosition` 显式覆盖
        /// （九宫格/平铺的块都按**根节点的局部原点**铺，与矩形中心值无关 —— 那两行注释写了为什么）。</summary>
        static PxRect PopupRectPx()
        {
            float px = LayoutSpace.DesignPxW * 0.5f + BarDx, py = -BarDy;
            return new PxRect(px - PopupW * 0.5f, py - PopupH * 0.5f, px + PopupW * 0.5f, py + PopupH * 0.5f);
        }

        public bool Visible { get { return _popup != null && _popup.activeSelf; } }
        /// <summary>自检用：现在条上写的字</summary>
        public string ShownText { get { return _text != null ? _text.Text : "<无>"; } }
        /// <summary>自检用：提示条的**世界尺寸**（断言它等于原版 `Generic Popup Background` 的 1323×90 px）</summary>
        public float BarWorldW { get { return U(PopupW); } }
        public float BarWorldH { get { return U(PopupH); } }
        /// <summary>自检用：底板取到图了没有。
        /// 🔴 **必须数「框」自己的块，不能数 `_popup.transform.childCount`** ——
        ///    2026-09-17 第一版就是这么写的，结果**框没建起来（贴图取空了）它照样绿**，
        ///    因为填充那些块也挂在同一个父节点下（典型的「尺子自己会坏」）。</summary>
        public bool BarHasArt { get { return BarFramePieces == 9; } }
        /// <summary>自检用：九宫格框建了几块（原版 `Sliced` 应当是 **9**）
        /// <para>🔴 **2026-10-16（`T09` 块 6）**：本口**只对世界档有判别力** —— 画布档的框是**一张
        /// `Image (Sliced)`**（没有九块 quad）⇒ 恒 `0`。画布档照 `FrameIsSliced` / `FrameSizePx` 断
        /// （见本件报告 ③ 的接线清单）。</para></summary>
        public int BarFramePieces
        {
            get
            {
                if (_popup == null) return 0;
                int n = 0;
                foreach (Transform c in _popup.transform)
                    if (c.name.StartsWith("wait_popup_")) n++;
                return n;
            }
        }
        /// <summary>`40k_popup_texture` 的**平铺格宽**（**画布 px**）= **64**。
        /// 🔴 判据 = 原版那条 `Background fill` 的 `m_PixelsPerUnitMultiplier = **2.0**`：
        ///    uGUI 的格宽 = `(m_Rect.width − 左右 border) ÷ multipliedPixelsPerUnit`
        ///    （`Image.multipliedPixelsPerUnit` = `pixelsPerUnit × m_PixelsPerUnitMultiplier`；
        ///      `:1232`：`tileWidth = (spriteSize.x − border.x − border.z) ÷ multipliedPixelsPerUnit`），
        ///    参考分辨率 1920×1080 + Canvas `m_ReferencePixelsPerUnit = 100` + sprite `m_PixelsToUnits = 100`
        ///    ⇒ `pixelsPerUnit = 1` ⇒ **128 ÷ 2 = 64**。
        /// 本工程另有 5 处同一张图的先例都传这个数（`PromptPopup.FillTilePx` · `ImportDeckPopup.FillTilePx` ·
        /// `MissionRerollPopup` · `ProfileTab` · `DuelPopupWindow`，各自那条注释里都写着同一个推导）。
        /// ⚠️ **本件原来传的是 `128f`**（= 把 `m_Rect.width` 直接当节距、漏了 `ppuMul`）—— 它是**全工程唯一的例外**，
        /// 画面后果是**格子大一倍**（11 块 vs 42 块）。**2026-10-05 改回 64。**</summary>
        const float FillTilePx = 64f;

        /// <summary>自检用：填充层**建了几块**（原版 `Tiled`：1323÷64 向上取整 × 90÷64 向上取整 = **21×2 = 42**）。
        /// 🔴 **2026-10-04 修（A50④）**：原来数的是 `_fillRoot.transform.childCount` —— 那是 `MenuDraw.Tiled`
        ///    返回的**根节点**（`CreateTiled` 把块平铺挂在它下面）⇒ **恒等于 1**，
        ///    而 `Editor/BattleScene.cs` 那条断的是 `>= 1` ⇒ **改坏块数一条都不会红**（弱断言，等于没查）。
        ///    现在数**真正的 quad 块**（`ImageQuad` 子件）。
        /// 🔴 **2026-10-05 就地订正**：本段原来写「节距 = 贴图 **128** px ⇒ **11** 块」—— **那个节距是错的**
        ///    （漏了 `m_PixelsPerUnitMultiplier`，见 `FillTilePx` 的注释）。改成 64 之后：
        ///    · 1323 ÷ 64 = 20.67 → **21 列**；90 ÷ 64 = 1.41 → **2 行** ⇒ **42**。
        ///    ⚠️ 节距（画布 px）与 `ImageQuad.PixelsPerUnit = 108`（画布 px / 世界单位）**同一个口径**，
        ///      和本件 `U()` 那套换算是同一个换算。
        /// ⚠️ 别数 `_fillRoot` 自己（空节点、没有 `ImageQuad`）；也别回头去数 `childCount`。
        /// <para>🔴 **2026-10-16（`T09` 块 6）**：本口**只对世界档有判别力** —— 画布档的填充是**一张
        /// `Image (Tiled)`**（不是 42 块 quad）⇒ 恒 `0`。画布档照 `FillIsTiled` / `FillSizePx` / `FillPpuMulValue`
        /// 断（原版那三个值 → 本件报告 ③ 的接线清单）。</para></summary>
        public int BarFillPieces
        {
            get
            {
                if (_fillRoot == null) return 0;
                int n = 0;
                foreach (var q in _fillRoot.GetComponentsInChildren<ImageQuad>(true)) if (q != null) n++;
                return n;
            }
        }
        /// <summary>自检用：压暗层的颜色（断言 α = 原版 0.6118）</summary>
        public Color ShadeTint { get { return _shade != null ? _shade.Tint : Color.clear; } }
        /// <summary>自检用（**A276**）：框那一层写进去的**局部 z**（没建出来 ⇒ `NaN`）。
        /// 判据 = 它**必须只由本件的 `Z` 决定**，不许随父节点的世界 z 变 —— 同父的 `_shade`/`_text`
        /// 都是裸局部 z ⇒ 同一父节点下只该有一套口径（判据全文 → `资料/普查产出_1010/V4b_三件口径.md` §Q2）。</summary>
        public float PopupLocalZ { get { return _popup != null ? _popup.transform.localPosition.z : float.NaN; } }

        /// <summary>🔴 **2026-10-16（`T09` 块 6）：双模式的开关。**
        /// <para>`uguiHost` **非空 ⇒ 画布档**（件挂在它上面、坐标/尺寸 = **设计 px**）；
        /// **`null`（出厂）⇒ 世界档 —— 与加本档之前逐位相同**（先例 = `ErrorMessageBanner.Create(..., uguiHost)` ·
        /// `Tooltip.UseHost` · `TutorialOverlay.Create(parent, uguiHost)`）。</para>
        /// <param name="uguiHost">画布档的宿主 = **根画布**（`HudCanvas.RootRect`）。
        /// ⚠️ 本件**不建自己的子画布**、也**不进 `HudCanvas.SubCanvasRank`**（判据 → 文件头那条）。
        /// ⚠️ **生产路必须传它**（`BattleDriver.BuildHud` 传 `HudCanvas.RootRect`）—— 不传会**静默**走世界档，
        /// 而非 16:9 下位置与「压暗盖住名牌一族」正是本件要修的那件事（调用点那一侧有出声检查）。</param>
        /// <param name="parent">世界档的父节点（= `BattleDriver` 的 `HudRoot`）。
        /// ⚠️ **画布档下它是个死参**（件挂 `uguiHost`）—— 照 `EndPanel.Create` 那条先例：**改注释即可、⛔ 别改签名**。</param></summary>
        public static WaitBanner Create(Transform parent, RectTransform uguiHost = null)
        {
            return New(parent, CardArt.DeckUi("40k_popup"), CardArt.Ui("40k_popup_texture"), "WaitBanner", uguiHost);
        }

        /// <summary>🔴 **自检专用**（`Editor/BattleScene.cs` 的「美术取不到那一档」那条）：照**同一段 `Build`** 建一份，
        /// 只是两张图由调用方给 —— 传 `null` 就是「美术目录被删 / 图取不到」那一档（本工程明确支持的退路）。
        /// ⛔ 生产路（`Create`）别改走它：那边必须自己去 `CardArt` 取图（`Create` 就是它的唯一调用方）。
        /// <para>🔴 **2026-10-16（`T09` 块 6）**：加了 `uguiHost` 形参（默认 `null` ⇒ 世界档，既有 3 个实参的
        /// 调用点一字不动）—— 画布档的「图取不到」那一档要靠它才验得到。</para></summary>
        public static WaitBanner CreateWithArt(Transform parent, Texture2D popupTex, Texture2D fillTex,
                                               RectTransform uguiHost = null)
        {
            return New(parent, popupTex, fillTex, "WaitBannerNoArt", uguiHost);
        }

        /// <summary>建这条提示的根节点。🔴 **2026-10-07（A92）：根节点是 `RectTransform`**（原来是裸 `Transform`）。
        /// **判据 = 原版这一件本来就是 `RectTransform`**：`bundle_scenes_scenes_battlearena1` 里
        /// 同名件 `WaitText` 的组件实读就是 `RectTransform`（那个场景 988 `RectTransform` / 236 裸 `Transform`，
        /// 裸的那批只有卡框 3D 锚、`HandArea`/`Board Center` 一类 3D 挂点与粒子件）。
        /// 位置/尺寸不受影响：本件摆位走 `MenuDraw.Local`（= 世界坐标差），与节点类型无关。
        /// ⛔ 别写成 `AddComponent&lt;RectTransform&gt;()`。</summary>
        static WaitBanner New(Transform parent, Texture2D popupTex, Texture2D fillTex, string name,
                              RectTransform uguiHost = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            // ⛔ 画布档挂**根画布**（⛔ 不挂 `hudRoot` —— 那是**反向补震**用的，挂它下面会补偿两次；
            //    也不是子画布 —— 见 `Create` 的 doc 与文件头那条「并进根画布那一组」）
            bool ugui = uguiHost != null;
            go.transform.SetParent(ugui ? (Transform)uguiHost : parent, false);
            var b = go.AddComponent<WaitBanner>();
            b._ugui = ugui;                       // ⚠️ 必须在 `Build()` **之前**赋值（`Build` 按它选档）
            if (ugui)
            {
                // 🔴 **2026-10-11（A218）**：根节点写 `sizeDelta` —— 判据 = 原版 `WaitText` 自己那对
                //    `(1344, 79.44)`（见 `RootW/RootH` 的注释）。⚠️ 真正画出来的是子节点
                //    `Generic Popup Background` 的 1323×90（`PopupW/H`），两者**不是**同一个数，⛔ 别互相顶替。
                //    ⚠️ 画布档走 `HudCanvas.SetPxSize`（**真 px**）—— `MenuDraw.SetPxSize` 内部是 ÷108，
                //       用它会把这一件缩成 1/108（**而画面只是小得看不见、不报错**）。
                //    改坏法：删掉 ⇒ `Editor/BattleScene.cs` §A218「`WaitText` 根 = 1344×79.44」红。
                HudCanvas.SetPxSize((RectTransform)go.transform, RootW, RootH);
                // 位置 = **原版那一对五元组**：`anchor (0.5,1)` + `ap (7.0, −175.1)`
                PlaceAtBarCenter((RectTransform)go.transform);
                // 排进「根画布」那一组的兄弟序 —— ⛔ 不排就靠创建顺序，而创建顺序**不是判据**
                //   （`HudCanvas.Group.Add` 按 z 降序插：z 大 ⇒ 排前面 ⇒ **画在底下**）
                HudCanvas.Add((RectTransform)go.transform, Z);
            }
            else
            {
                MenuDraw.SetPxSize(go.transform, RootW, RootH);
            }
            b.Build(popupTex, fillTex, uguiHost);
            b.SetVisible(false);
            return b;
        }

        /// <summary>两档的分派（`T09` 块 6）。⛔ 别把两档合成一段 —— 它们的**建件口、单位、锚点**三样都不同。</summary>
        void Build(Texture2D popupTex, Texture2D fillTex, Transform uguiHost)
        {
            if (_ugui) { BuildCanvas(uguiHost, popupTex, fillTex); return; }
            BuildWorld(popupTex, fillTex);
        }

        /// <summary>🔴 世界档 —— **与加画布档之前逐位相同**（⛔ 这一支一个字都别改；
        /// 画布档要改什么就改 `BuildCanvas`，两档的产出口是同一组字段与读口）。</summary>
        void BuildWorld(Texture2D popupTex, Texture2D fillTex)
        {
            // ---- 整屏压暗（原版 `Dark Shade`：无图、纯色、α 0.6118）----
            _shade = HudPiece.Of(ImageQuad.Create(transform,
                                      SettingsPanel.SolidTex(LayoutSpace.VisibleWidth / LayoutSpace.DesignHeight),
                                      new Vector3(0f, 0f, Z + 0.02f),
                                      LayoutSpace.DesignHeight * 1.05f,
                                      new Vector2(0.5f, 0.5f), "wait_shade"));
            // ⚠️ 上面那句 2026-10-16（`T09` 块 6）只多套了一层 `HudPiece.Of(...)` —— 字段类型从
            //    `ImageQuad` 改成 `HudPiece` 了（两档共用），**建出来的东西与参数一字未改**；
            //    `HudPiece.Of(null)` 也回 null ⇒ 空值语义与改之前相同。
            if (_shade != null) _shade.SetTint(ShadeColor);

            // ---- 提示条：位置与尺寸照原版（`runtime_ui_dump_drive_0912.tsv:350-355`）----
            //   `WaitText`(1344×79.4, 只是个容器)
            //     └ `Generic Popup Background` **1323×90** = Image `40k_popup`
            //         · `m_Type = 1`（**Sliced**）、`m_Border = (169,160,169,160)`、`m_Rect = 359×336`
            //         └ `Mask` + `Background fill` = Image `40k_popup_texture`
            //             · `m_Type = 2`（**Tiled**）、`m_Rect = 128×128`、`m_Color` 纯白
            //         └ `Text` 1269.5×70（原版 `m_fontSize = 0` ⇒ 字号取不到，见文件头）
            // 🔴 **2026-09-17 更正**：原来这里写「我们**没有九宫格**，`40k_popup` 硬拉到 1344×79.4 会横向糊成
            //    一条 ⇒ 改用自建实底」，**那是我们挑的**。现在 `ImageQuad.CreateNineSlice / CreateTiled`
            //    补上了这两个原版的 `Image.Type`，就照原版画。
            float cx = U(BarDx);
            float cy = LayoutSpace.DesignHeight * 0.5f + U(BarDy);
            // ⚠️ **两张图在工程的**不同**目录里**：`40k_popup` 在 `Art/ui_deck/`（`CardArt.DeckUi`），
            //    `40k_popup_texture` 在 `Art/ui/`（`CardArt.Ui`）—— 2026-09-17 第一次取错目录，
            //    九宫格静默没建（断言当时也没抓住，见 `BarFramePieces` 那段注释）。
            // 🔴 **2026-10-04（A50③）：框架这一层也收口到公共件 `MenuDraw.Nine`** ——
            //    原来直调 `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，拿不到 `clip`：滚动视口里
            //    画九宫格会一直画到视口外）。两处**逐项等价**，别改：
            //    ① **尺寸 = `PopupRectPx()` 的宽高**（1323×90 px）—— 矩形中心是**占位**：九块是按
            //       **根节点的局部原点**铺的（尺寸从 `−尺寸/2` 起算），根节点摆哪由下面那行 `localPosition` 定。
            //       ⛔ 别删那一行：`MenuDraw.Local` 走的是「按可见宽拉伸」的画布映射，16:9 下与 `cx/cy`
            //       逐位相同、非 16:9 会差几个 px ⇒ 不覆盖就会**框和字对不上**（字这一层一直用 `cx/cy`）；
            //    ② **渲染队列 = 3000**（`BattleQChrome`）—— 原来一次都没显式设过（= 材质默认档，正是 3000）。
            _popup = MenuDraw.Nine(transform, popupTex, PopupRectPx(),
                                   new Vector4(169f, 160f, 169f, 160f), 359f, 336f,
                                   BattleQChrome, name: "wait_popup");
            if (_popup == null)
            {
                // 🔴 **2026-10-05 就地订正（A71①）：这一支必须连填充层一起跳过 —— 原来那条 NRE 路径就在下面。**
                //    改走公共件之前，框这一层直调 `ImageQuad.CreateNineSlice`，它 **从不返回 null**
                //    （`Battle/ImageQuad.cs` 的 `CreateNineSlice`：`tex == null` 只打警告、**仍返回 root**）
                //    ⇒ 下面那句 `_fillRoot.transform.SetParent(_popup.transform, …)` 是**死码、永远安全**。
                //    现在框走 `MenuDraw.Nine`，而它 **`tex == null` 时返回 null**（`Shell/MenuDraw.cs` 的 `Nine`）
                //    ⇒ 同一句变成一条真正的 NRE：「美术目录被删 / 图取不到」那一档从
                //    「打警告 + 退化」变成**抛 `NullReferenceException`**。
                //    ⛔ 别再把建填充那几行挪回 `if` 外面（同批另外三处都判空/早退：
                //      `Battle/SettingsPanel.cs` · `Battle/WfSlider.cs` · `Core/Tooltip.cs`）。
                Debug.LogWarning("[WaitBanner] 提示条底板没建起来（`40k_popup` 没取到？）—— "
                                 + "填充层一并跳过（它挂在框下面）；整条提示不显示（`Visible` 恒假）。"
                                 + "取图请跑 `工具/import_original_art.py`");
            }
            else
            {
                // x/y = `cx/cy`（本件原来那对值，与下面 `_text` 同一口径）；
                // z = `Z` —— **裸局部 z**，与**同父**的 `_shade`（`Z + 0.02`）· `_text`（`Z − 0.01`）
                // 同一套口径（本件 z 越负越靠前）。
                // 🔴 这一行**必须**覆盖 `MenuDraw.Nine` 给的那个局部坐标：它走 `MenuDraw.Local`
                //    （`Shell/MenuDraw.cs` 的 `Local`：`RectCenter − parent.position`），父件在世界原点时那个 z 是 `0`
                //    ⇒ 不覆盖的话框会落到 z = 0、**比压暗层还靠后**。
                // ⚠️ **A276（2026-10-11）把这里从 `Z - transform.position.z` 改成裸 `Z`** ——
                //    这是**收口径**（同一父节点下只留一套口径），**不是「对齐原版」**：这三层的
                //    **原版 z 关系没有判据**（`Z` 这个常量本身是我们挑的，见 `资料/普查产出_1010/V4b_三件口径.md` §Q2）。
                //    两种写法**今天逐位相同**（`transform.position.z ≡ 0`、`lossyScale.z ≡ 1`）⇒ 零观测风险；
                //    改法 = 与兄弟件同口径后，「父链被挪 z」时这一层不会再跟着漂。
                //    改坏法：写回 `Z - transform.position.z` ⇒ `Editor/BattleScene.cs` 那条 A276 探针断言红。
                _popup.transform.localPosition = new Vector3(cx, cy, Z);

                // 填充在**框后面**（我们这个坐标系的 z 越负越靠前）—— 原版它是被 `Mask` 裁在框里的。
                // 单独挂一个子节点，好让自检能分开数「框几块 / 填充几块」。
                // 🔴 **2026-10-04（A25⑤）**：原来在这里直调 `ImageQuad.CreateTiled` —— 那是**绕开公共件**
                //    的平铺路（`MenuDraw.Tiled` 才带 `clip`），已收口。两处**逐项等价**，别改：
                //    ① **渲染队列 = 3000** —— `MenuDraw.Tiled` 会**显式写**队列，不传就等于在战斗现场
                //       换一次排序。3000 = `Sprites/Default` 的默认档（本件原来就是它），也是战斗侧的
                //       `CardDisplayWindow.QChrome`；
                //    ② z 补回 **+0.01** —— ⚠️ **2026-10-04 就地订正（F13）：理由原来写反了**。`MenuDraw.Tiled`
                //       给的根节点 z 走 `MenuDraw.Local` = `RectCenter 的 z(恒 0) − 父件 z`，而**父件就是框**
                //       （`_popup`，世界 z = `Z` = −0.45）⇒ 它会把这一层摆到 **z = 0**；本件的坐标系
                //       **z 越负越靠前**（见上面 `_popup` 那一行）⇒ 0 是**更靠后**、不是「更前面」。
                //       这一行 `+0.01` 是**把填充压到框后面**（框 −0.45 → 填充 −0.44）；**不补**的话它落在
                //       **和框同一层** z 上 ⇒ 同队列同距离，谁先画由排序/枚举决定（不是「跑到框前面」）。
                //       （这一行本身是对的，只有理由那句话方向反了。）
                _fillRoot = new GameObject("wait_fillRoot", typeof(RectTransform));
                _fillRoot.transform.SetParent(_popup.transform, false);
                // 🔴 **2026-10-11（A218）**：这一层是 `RectTransform` + 写 `sizeDelta` ——
                //    它的原版对应件是 `Generic Popup Background/Mask`（`bundle_scenes_scenes_battlearena1`
                //    实读：`Mask` 与它下面的 `Background fill` **两件都是 `RectTransform`**；
                //    `Mask` 是 stretch 锚 + `sizeDelta (0,0)` ⇒ 它的矩形 = **父件那块 1323×90**）。
                //    ⚠️ 节点名 `wait_fillRoot` **是我们自己起的**（原版那两级叫 `Mask` / `Background fill`）——
                //    名字要不要照原版是**另一件**（A92 报告 §四·2a 已登记），本轮不动。
                //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「填充层根 = 1323×90」红。
                MenuDraw.SetPxSize(_fillRoot.transform, PopupW, PopupH);
                _fillRoot.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                var fillR = PopupRectPx();
                var fillGo = MenuDraw.Tiled(_fillRoot.transform, fillTex, fillR,
                                            FillTilePx, BattleQChrome, "fill");
                if (fillGo != null) fillGo.transform.localPosition = Vector3.zero;   // = `_fillRoot` 原位（见上②）
                else Debug.LogWarning("[WaitBanner] 填充层没建起来（`40k_popup_texture` 没取到？）");
            }

            _text = Label.Create(transform, Loc.T(WaitTerm),
                                 new Vector3(cx, cy, Z - 0.01f), 4,
                                 new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f), "wait_text");
            // 🔴 2026-10-17（A1024③ 逐行对账）：原版 `WaitText/Generic Popup Background/Text` = Pragati。
            //    `Label.Create` 无 face 形参 ⇒ 建完立刻换面（早于下面 `SetAutoFitBox` / 定字号）。
            if (_text != null) _text.SetFace(TmpFont.Face.LatinCardBody);
            // 🔴 **2026-10-16（`A2102⑤` / R6）：这颗文字【自己有框与字号】，我们原来一个都没接。**
            //   判据（`B5` 表一 R6 + 主对话现读那颗 TMP 的字段 `MonoBehaviour_4090.json`）：
            //   框 **1270 × 70**（画布 px）· `m_enableAutoSizing = 1` · `m_fontSize = 55.3` ·
            //   `m_fontSizeMin = 18` · `m_fontSizeMax = 55.3` · **`m_fontSizeBase = 36`**。
            //   ⚠️ `basePx` 这个形参就是 TMP 的 **`m_fontSizeBase`**（见 `SetAutoFitBox` 里那句
            //     「起点就是 base」）⇒ 填原版的 **36**，⛔ 别照 `A1104` 那次「base 取 max」的写法抄过来
            //     （那一次原版的 base 本来就是那个值）。
            //   ⚠️ 它原来落 `Label` 默认档 ⇒ 这条提示的字**多大与框多宽都没判据**（同族唯一没接框的一件）。
            _text.SetAutoFitBox(LayoutSpace.Px(WaitTextBoxW), LayoutSpace.Px(WaitTextBoxH),
                                WaitTextFsMin, WaitTextFsMax, WaitTextFsBase);
        }

        // ==================================================================
        //  🔴 2026-10-16（`T09` 块 6）：**画布档**
        // ==================================================================

        /// <summary>条心在**画布 px**里的坐标（左上原点）：`x` = 画面横中 + 原版 `ap.x`；
        /// `y` = 原版 `−ap.y`（`BarDx / BarDy` 就是原版 `WaitText` 的 `anchoredPosition (7.0, −175.1)`）。</summary>
        static float BarCenterXPx { get { return LayoutSpace.DesignPxW * 0.5f + BarDx; } }
        static float BarCenterYPx { get { return -BarDy; } }

        /// <summary>🔴 **把一件摆到「条心」上** —— 原版那一对五元组：**`anchor (0.5,1)` + `ap (7.0, −175.1)`**
        /// （出处 → `RootW` 的注释）。压暗 / 框 / 字三层共用它 ⇒ **三层同心**，原版也是同心
        /// （`Dark Shade` 与 `Generic Popup Background` 都是 `WaitText` 的子件、以它为中心）。
        /// <para>⛔ **必须写 `anchoredPosition`**（走 `HudCanvas.SetAnchorPx`）—— **不许写 `localPosition`**：
        /// 后者的原点是**父件枢轴**、不是父件中心（`T09` §八·补 陷阱 1；先例是 `Alliance Panel` 那扇偏
        /// `(301.566, −154.528)` px，**16:9 下也错**）。
        /// <para>⚠️ 前提 = 本件 `pivot == (0.5,0.5)` —— 三个建件口（`MakeImagePiece` / `MakeSlicedPiece` /
        /// `Label.CreateUgui`）都显式设了它 ⇒ 「枢轴点 = 矩形中心」= 条心 ✓。</para>
        /// <para>✅ **16:9 逐位**：`HudCanvas.AnchorApPx(967, 175.1, 0.5, 1) = (7, −175.1)` = **原版 `ap` 原文**
        /// （不依赖画布的实际尺寸 ⇒ 任何宽高比下都是原版那两个数）。</para></summary>
        static void PlaceAtBarCenter(RectTransform rt)
        {
            if (rt == null) return;
            HudCanvas.SetAnchorPx(rt, 0.5f, 1f, BarCenterXPx, BarCenterYPx);
        }

        /// <summary>🔴 **2026-10-16（`T09` 块 6）：画布档。** 件**直接挂根画布**（`host` = `HudCanvas.RootRect`）、
        /// 各自用 `HudCanvas.Add(rt, z)` 定兄弟序 —— 判据 → 文件头那条「并进根画布那一组」。
        /// <para>三层与框的相对次序**照世界档那三个 z 值**（`Z+0.02` → `Z` → `Z−0.01`；语义同向：
        /// `Add` 的 z 大 ⇒ 画在底下）⇒ 16:9 下与世界档**逐件对得上**。</para>
        /// <para>⚠️ **与世界档树的一处不同**：填充那三层/框在世界档里挂在**本组件节点**下，这里挂**根画布**；
        /// 本组件节点退成一个**不画东西的容器**（原版 `WaitText` 也只是个容器 ✓）。</para></summary>
        void BuildCanvas(Transform host, Texture2D popupTex, Texture2D fillTex)
        {
            // ---- 整屏压暗（原版 `Dark Shade`：**无图、纯色**、`sd 3963.5 × 3366`、α 0.6118）----
            //  🔴 尺寸取**原版那个数**（见 `ShadeW/ShadeH`）—— 世界档那个高 = `DesignHeight × 1.05` 是**我们自算的**。
            //     ⚠️ 两者在 16:9 下**都盖满整屏** ⇒ 看不出差别；它是个以条心为中心的巨大 quad，
            //     3:1 ~ 1:3 的任何常用宽高比都盖得住。
            //     🧨 改坏法：把它挤到 1920×1080 ⇒ 4:3 下条心偏下 175 px ⇒ 上下各露一条。
            //  ⚠️ `tex` 传 `null` = **原版那颗 Image 的 `m_Sprite = null`**（纯色）——
            //     `MakeImage` 这一档返回的是**没有 sprite 的 `Image`**（uGUI 用默认白底面片 + `color`）✓。
            //  🔴 **2026-10-24（执行代理 `W1` · 判据 `N1` §2.2 / §4-D4）**：原版这一颗是
            //     **`m_RaycastTarget = 1` + `m_RaycastPadding = (0, 1000, 0, 0)`**
            //     （`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4415.json` 现读；
            //      padding 的 uGUI 顺序 = X 左 / Y 下 / Z 右 / W 上）⇒ **吃点击**，但**下沿内缩 1000 px**
            //      （1080 高的屏上压暗的下边界从 1858 抬到 **858** ⇒ 最下面 222 px 的手牌那一带照旧能点）。
            //     ⚠️ 这里只做「**件自己吃不吃射线**」那一半；「世界侧输入也让它挡」在
            //        `BattleDriver` 的 `WaitShadeContains`（读本件的 `ShadeContains`）。
            _shade = HudCanvas.MakeImagePiece(host, null, "wait_shade",
                                              BarCenterXPx - ShadeW * 0.5f, BarCenterYPx - ShadeH * 0.5f,
                                              ShadeW, ShadeH, ShadeColor, raycast: true);
            if (_shade != null)
            {
                // padding 只能在拿到 `Image` 之后写（`HudCanvas` 那一层没有这个话题的形参 ——
                // 它只服务这一件，⛔ 别为一个调用点把 `HudCanvas` 的签名再撑大）
                var shadeImg = ImageOf(_shade.gameObject);
                if (shadeImg != null)
                    shadeImg.raycastPadding = new Vector4(0f, ShadeRaycastPadBottomPx, 0f, 0f);
                else
                    Debug.LogError("[WaitBanner] 压暗层的 `Image` 取不到 ⇒ 原版那条 `m_RaycastPadding` 写不进去"
                                 + "（它会变成「整屏都吃点击」或「整屏都不吃」，两种都与原版不符）");
                // ⚠️ `MakeImage*` **自己按 `DefaultZ` 登记过一次** ⇒ 想挪档**必须走 `Reorder`**
                //    （`Group.Add` 不去重：同一个件 `Add` 两次会留两条账、索引此后算偏，见 `T09` §七·11）
                HudCanvas.Reorder((RectTransform)_shade.transform, Z + 0.02f);
                PlaceAtBarCenter((RectTransform)_shade.transform);
            }

            // ---- 提示条：原版 `Generic Popup Background` = 1323×90、Image `40k_popup`
            //      （`m_Type = 1` ⇒ **`Image (Sliced)`**；`m_Border = (169,160,169,160)`）----
            //  锚点/枢轴全在中心（`MakeSlicedPiece` 定的）⇒ 落点交给 `PlaceAtBarCenter`。
            var popup = HudCanvas.MakeSlicedPiece(host, popupTex, "wait_popup",
                                                  PopupW, PopupH, new Vector4(169f, 160f, 169f, 160f),
                                                  Color.white);
            _popup = popup != null ? popup.gameObject : null;
            if (_popup == null)
            {
                // ⚠️ **与世界档同一支**（⛔ 别让两支的行为分家）：框没建起来 ⇒ **填充一并跳过**
                //    （它挂在框下面）、整条提示不显示（`Visible` 恒假，因为它的判据就是 `_popup`）。
                Debug.LogWarning("[WaitBanner] 提示条底板没建起来（`40k_popup` 没取到？）—— "
                                 + "填充层一并跳过（它挂在框下面）；整条提示不显示（`Visible` 恒假）。"
                                 + "取图请跑 `工具/import_original_art.py`");
            }
            else
            {
                PlaceAtBarCenter((RectTransform)_popup.transform);
                // 兄弟序 = `Z`（比压暗层的 `Z+0.02` 小 ⇒ **画在压暗之上**）
                HudCanvas.Add((RectTransform)_popup.transform, Z);

                // 填充：原版是 `Generic Popup Background/Mask > Background fill`（**框的子件**）⇒ 照原版嵌套建。
                // 单独挂一个子节点，好让自检能分开数「框 / 填充」。
                _fillRoot = new GameObject("wait_fillRoot", typeof(RectTransform));
                _fillRoot.transform.SetParent(_popup.transform, false);
                // 原版 `Mask` 是 stretch 锚 + `sd (0,0)` ⇒ 它的矩形 = 父件那块 **1323×90**（真 px）
                HudCanvas.SetPxSize((RectTransform)_fillRoot.transform, PopupW, PopupH);
                var fill = HudCanvas.MakeSlicedPiece(_fillRoot.transform, fillTex, "fill",
                                                     PopupW, PopupH, Vector4.zero, Color.white);
                if (fill == null)
                {
                    Debug.LogWarning("[WaitBanner] 填充层没建起来（`40k_popup_texture` 没取到？）");
                }
                else
                {
                    // 🔴 **平铺**（原版 `Background fill` 的 `m_Type = 2` **Tiled**）—— uGUI 的对应物 =
                    //    `Image.Type.Tiled` + `pixelsPerUnitMultiplier = 2`（格宽 = 128 ÷ 2 = **64** 画布 px，
                    //    推导见 `FillTilePx` 的注释）。
                    //    ⚠️🔴 **`HudCanvas` 今天【没有】平铺口** ⇒ 这两行是**临时权宜**（建完拿 `Image` 改两个字段）：
                    //      已记进本件报告 ③ 的接线清单，请主对话收编成一个 `MakeTiledPiece(..., ppuMul, ...)`
                    //      （与 `T09` §七·3 / §七·10 那两条「写手各自造了个权宜」**同族**，⛔ 别让它长出第二份）。
                    //    ⚠️ 这只改**填法**、**不改嵌套**：填充仍是框的子件（原版就是这么嵌的 ✓）。
                    //    ⚠️ 为什么不能拿 `MakeImageInParent` 顶：那个内部 `tex == null` 早退、且建的是 `Simple`（拉伸）
                    //      —— 拉伸会把 128² 的图案按 1323×90 揉开（横向 10 倍），与原版「64 px 一格」不是一回事。
                    var fim = fill.Rect != null ? fill.Rect.GetComponent<Image>() : null;
                    if (fim != null)
                    {
                        fim.type = Image.Type.Tiled;
                        fim.pixelsPerUnitMultiplier = FillPpuMul;
                    }
                    else
                    {
                        Debug.LogError("[WaitBanner] 填充层的 `Image` 取不到 ⇒ 它按 `Simple`（拉伸）画 —— "
                                     + "与「原版 `Tiled`、64 px 一格」不符（⛔ 出声，不静默）");
                    }
                    HudCanvas.Add(fill.Rect, Z + 0.01f);
                }
            }

            // ---- 字（原版 `Generic Popup Background/Text`：框 1270×70 · 自适应 [18, 55.3] · base 36）----
            _text = HudCanvas.MakeText(host, Loc.T(WaitTerm), 0f, 0f, 4,
                                       new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f),
                                       Z - 0.01f, "wait_text", face: TmpFont.Face.LatinCardBody);
            // 🔴 **2026-10-16（`A2102⑤` / R6）：这颗文字【自己有框与字号】，我们原来一个都没接。**
            //   判据（`B5` 表一 R6 + 主对话现读那颗 TMP 的字段 `MonoBehaviour_4090.json`）：
            //   框 **1270 × 70**（画布 px）· `m_enableAutoSizing = 1` · `m_fontSize = 55.3` ·
            //   `m_fontSizeMin = 18` · `m_fontSizeMax = 55.3` · **`m_fontSizeBase = 36`**。
            //   ⚠️ `basePx` 这个形参就是 TMP 的 **`m_fontSizeBase`**（见 `SetAutoFitBox` 里那句
            //     「起点就是 base」）⇒ 填原版的 **36**，⛔ 别照 `A1104` 那次「base 取 max」的写法抄过来。
            if (_text != null)
            {
                // ⚠️ `MakeText` 的 `px/py` 是「**相对父件中心**」⇒ 上面给的是 `(0,0)`（= 画布中心，只是过路）；
                //    **真正的落点在这一行**：与框、压暗层**同一个点**（原版的 `Text` 也是嵌在框里、以框心为中心）。
                //    🧨 改坏法：把这行换成 `PosPx(BarCenterXPx, BarCenterYPx)` ⇒ **16:9 下逐位相同**、
                //       **4:3 下字掉到框下面 135 屏 px**（两套锚点约定之差；口径 → `T09` §八·补 陷阱 1/3）。
                PlaceAtBarCenter((RectTransform)_text.transform);
                // ⚠️ **框与字号收的是【设计 px】**（世界档那一支才是 `LayoutSpace.Px(...)`）——
                //    喂世界单位**不报错、只是小到看不见**（`T09` §五·补⑩ 第 4 步）。
                _text.SetAutoFitBox(WaitTextBoxW, WaitTextBoxH, WaitTextFsMin, WaitTextFsMax, WaitTextFsBase);
            }
        }

        /// <summary>提示条走的那条原版词条。🔴 **只此一份**（`Build()` 与自检都取它）。
        /// 键名 = 原版那颗 `Localize.mTerm` 的原文，出处见文件头那条订正。</summary>
        public const string WaitTerm = "Battle/Mulligan/WaitEnemy";

        // 🔴 **2026-10-16（`A2102⑤` / R6）：那颗文字自己的框与字号** —— 出处 = `B5` 表一 R6（框 1270×70）
        //   + 主对话现读的 TMP 字段（`bundle_scenes_scenes_battlearena2/MonoBehaviour/MonoBehaviour_4090.json`，
        //   它是按 `m_text = "Waiting for enemy"` 找到的唯一一份）。**判据只此一份**（`Build()` 与断言都取它）。
        public const float WaitTextBoxW = 1270f, WaitTextBoxH = 70f;
        /// <summary>`m_fontSizeMin` = **18**（画布 px，与 `A1104` 那族同一个下限值 —— 原版就是这么填的）。</summary>
        public const float WaitTextFsMin = 18f;
        /// <summary>`m_fontSizeMax` = **55.3**（= `m_fontSize`，自适应开着时的上限）。</summary>
        public const float WaitTextFsMax = 55.3f;
        /// <summary>`m_fontSizeBase` = **36**（TMP 自适应的**起点**）—— ⚠️ 它**不是** 55.3，
        /// ⛔ 别把它写成 max（`SetAutoFitBox` 的第 5 个形参就是这一格）。</summary>
        public const float WaitTextFsBase = 36f;

        /// <summary>自检用：那颗文字的**渲染宽度/高度**（世界单位）；没建 → −1。
        /// 🔴 断「**渲染尺寸 ≤ 框**」而不是「字号 = 某个数」 —— 字号对而溢出自检照样全绿
        /// （`AutoFitBox` 那条教训，见 `资料/已知的坑.md`）。</summary>
        public float TextWorldW { get { return _text != null ? _text.WorldW : -1f; } }
        public float TextWorldH { get { return _text != null ? _text.WorldH : -1f; } }

        // ── 🔴 2026-10-16（`T09` 块 6）：**画布档的自检读口** ────────────────────────────────
        //  ⚠️ **一律读真 px**（`sizeDelta` / `HudCanvas.CenterPx`）—— ⛔ 别拿世界尺寸 ×108 折回来：
        //     画布从「设计 px」到「世界单位」的两轴系数**不同**、还随屏变（`T09` §七·8）。
        //  ⚠️ 世界档下这几个口读出来是**空 / 假**（世界档没有 `Image`、也不在画布上）—— 那是**正确的信号**，
        //     ⛔ 不是缺陷；`Editor/BattleScene.cs` 那几条按 `OnCanvas` **分档**断言（见本件报告 ③）。

        /// <summary>这一件是不是**画布档**（判据只此一处；世界档 = `false`）。</summary>
        public bool OnCanvas { get { return _ugui; } }

        /// <summary>条心的**画布 px**（左上原点）—— 判据 = 原版 `anchor (0.5,1)` + `ap (7.0, −175.1)`
        /// ⇒ **`(967, 175.1)`**。没建框 ⇒ `(0,0)`。
        /// ⚠️ 走 `HudCanvas.CenterPx`（= 读**枢轴那一点**）⇒ 只在 `pivot == (0.5,0.5)` 时等于矩形中心
        /// （本件三层都满足，见 `PlaceAtBarCenter`）。</summary>
        public Vector2 BarCenterPx
        {
            get { return _popup != null ? HudCanvas.CenterPx((RectTransform)_popup.transform) : Vector2.zero; }
        }

        /// <summary>本组件节点（原版 `WaitText`）的 `sizeDelta`（**真 px**）—— 原版 **1344 × 79.44**。</summary>
        public Vector2 RootSizePx { get { return HudCanvas.SizePx((RectTransform)transform); } }

        /// <summary>框那层（原版 `Generic Popup Background`）的 `sizeDelta`（**真 px**）—— 原版 **1323 × 90**。</summary>
        public Vector2 FrameSizePx
        {
            get { return _popup != null ? HudCanvas.SizePx((RectTransform)_popup.transform) : Vector2.zero; }
        }
        /// <summary>框那一层是不是 `Image.Type.Sliced`（对照原版 `m_Type = 1`）。</summary>
        public bool FrameIsSliced { get { var im = ImageOf(_popup); return im != null && im.type == Image.Type.Sliced; } }

        /// <summary>填充那层的 `sizeDelta`（**真 px**）—— 原版 `Background fill` 铺满 `Mask` 那块 **1323 × 90**。</summary>
        public Vector2 FillSizePx
        {
            get { var im = FillImage; return im != null ? HudCanvas.SizePx((RectTransform)im.transform) : Vector2.zero; }
        }
        /// <summary>填充那层是不是 `Image.Type.Tiled`（对照原版 `m_Type = 2`）。</summary>
        public bool FillIsTiled { get { var im = FillImage; return im != null && im.type == Image.Type.Tiled; } }
        /// <summary>填充那层写进去的 `pixelsPerUnitMultiplier`（对照原版 `m_PixelsPerUnitMultiplier = 2.0`；
        /// 常量那一份见 `FillPpuMul`）—— 它决定格宽（`128 ÷ 2 = 64` 画布 px，推导见 `FillTilePx`）。没建 ⇒ **−1**。
        /// ⚠️ 读口一律带 `Value` 后缀（先例 = `HudCanvas.PlaneDistanceValue` / `SortingOrderValue`），
        /// ⛔ 别与那个原版常量同名（`CS0102`）。</summary>
        public float FillPpuMulValue { get { var im = FillImage; return im != null ? im.pixelsPerUnitMultiplier : -1f; } }

        /// <summary>压暗层的 `sizeDelta`（**真 px**）—— 原版 `Dark Shade` 的 **3963.5 × 3366**。</summary>
        public Vector2 ShadeSizePx
        {
            get { var im = ImageOf(_shade != null ? _shade.gameObject : null);
                  return im != null ? HudCanvas.SizePx((RectTransform)im.transform) : Vector2.zero; }
        }

        /// <summary>🆕 **2026-10-19（`A1304`）：原版压暗层的 `m_RaycastPadding` 下沿内缩量**。
        /// 判据 = `bundle_scenes_scenes_battlearena1 ▸ …/WaitText/Dark Shade` 的
        /// `m_RaycastPadding = (0, 1000, 0, 0)`（`MonoBehaviour_4415.json` 现读；
        /// uGUI 的顺序是 **X=左 · Y=下 · Z=右 · W=上**，见 `Graphic.raycastPadding` 的 doc）。
        /// <para>🔴 **方向别写反**（`A1304` 的订正）：本仓约定「**负 = 外扩、正 = 内缩**」⇒ 下沿**内缩** 1000px
        /// ⇒ 屏幕 1080 下压暗的**下边界从 1858.1 抬到 858.1** ⇒ **最下面 221.9px（手牌那一带）照旧可用**。
        /// 原版语义 = 「挡上中部（连 tooltip hover 也挡）、底下手牌那一带不挡」。</para></summary>
        public const float ShadeRaycastPadBottomPx = 1000f;

        /// <summary>🆕 **2026-10-19（`A1304`）：这个**世界点**落不落在压暗层的【可吃输入区】里
        /// （= 压暗层矩形按原版 `m_RaycastPadding` 内缩之后的那块；见 <see cref="ShadeRaycastPadBottomPx"/>）。
        /// <para>⚠️ **本件只给几何** —— 消费方（`BattleDriver` 的指针 / tooltip 判定）不在本文件；
        /// 接线 = 在 `TickTooltipAt` 与点击入口之前先问一句本函数（接口需求 → 本批报告）。</para>
        /// <para>⚠️ 两档各读各的尺：画布档 = `sizeDelta`（设计 px = 局部单位）；世界档 = `WorldW/H × lossyScale`。</para></summary>
        public bool ShadeContains(Vector3 world)
        {
            if (_shade == null || !_shade.gameObject.activeSelf) return false;
            var tr = _shade.transform;
            float w, h, padB, lx = 1f, ly = 1f;
            if (_ugui)
            {
                var s = ShadeSizePx;
                w = s.x; h = s.y; padB = ShadeRaycastPadBottomPx;
            }
            else
            {
                lx = Mathf.Abs(tr.lossyScale.x); ly = Mathf.Abs(tr.lossyScale.y);
                w = _shade.WorldW * lx; h = _shade.WorldH * ly;
                padB = ShadeRaycastPadBottomPx / 108f * ly;      // 世界档 108 px = 1 世界单位
            }
            if (!(w > 0f) || !(h > 0f)) return false;
            Vector3 lp = tr.InverseTransformPoint(world);        // 中心原点、y 向上（与 `RectTransform` 同向）
            lp.x *= lx; lp.y *= ly;                              // 局部单位 → 与 w/h 同一把尺
            return Mathf.Abs(lp.x) <= w * 0.5f
                && lp.y <= h * 0.5f
                && lp.y >= -h * 0.5f + padB;
        }

        /// <summary>🆕 **2026-10-24（`W1` · 判据 `N1` §2.2）**：压暗层的 `Image.raycastTarget`
        /// —— 原版是 **`1`（吃点击）**；世界档（`ImageQuad`）没有这个概念 ⇒ 恒 `false`。
        /// <para>🧨 改坏法：把 `BuildCanvas` 那句 `raycast: true` 去掉 ⇒ `Editor/BattleScene.cs` 那条红。</para></summary>
        public bool ShadeRaycastTarget
        {
            get { var im = ImageOf(_shade != null ? _shade.gameObject : null); return im != null && im.raycastTarget; }
        }

        /// <summary>🆕 **2026-10-24（`W1`）**：压暗层 `Image.raycastPadding`（uGUI 顺序 X 左 / Y 下 / Z 右 / W 上）。
        /// 原版 = **`(0, 1000, 0, 0)`**（下沿内缩 1000 px）。没这颗 `Image` ⇒ 全 0。</summary>
        public Vector4 ShadeRaycastPaddingPx
        {
            get { var im = ImageOf(_shade != null ? _shade.gameObject : null);
                  return im != null ? im.raycastPadding : Vector4.zero; }
        }

        /// <summary>压暗层**有没有贴图** —— 原版 `Dark Shade` 是**无图、纯色**（`m_Sprite = null`）
        /// ⇒ 期望 **`false`**（`true` = 我们给它塞了一张图，与原版不符）。
        /// ⚠️ 世界档恒 `false`（那一档的是 `ImageQuad`、不是 `Image`）⇒ 只对画布档有判别力。</summary>
        public bool ShadeHasSprite
        {
            get { var im = ImageOf(_shade != null ? _shade.gameObject : null);
                  return im != null && im.sprite != null; }
        }

        /// <summary>本件在**根画布那一组**里占的 z 档（`HudCanvas.OrderZOf`；认不出 ⇒ `NaN`）。        /// 判据 = 它必须**小于 HUD 图族那一档**（`HudCanvas.DefaultZ` = 0.3）：`Add` 的 z 大 ⇒ 画在**底下**
        /// ⇒ 本件才盖得住名牌一族（图 0.235~0.6 **与字 0**）。</summary>
        public float RootOrderZ { get { return HudCanvas.OrderZOf((RectTransform)transform); } }

        /// <summary>压暗 / 框 / 字三层各自的 z 档（读口同 <see cref="RootOrderZ"/>；没建 ⇒ `NaN`）。
        /// 🔴 判据（**一条不变量的三段**，别只断一层）：`ShadeOrderZ > PopupOrderZ > TextOrderZ`
        /// —— `Add` 的 z 大 ⇒ 画在底下 ⇒ **压暗压在框下面、字在框上面**。
        /// 🧨 灭自证：三层里任意两个的 z 写反（例如把字也写成 `Z + 0.02`）⇒ 这一组红；
        /// 而「只断其中一层等于某个数」的写法**抓不住**这种反。</summary>
        public float ShadeOrderZ
        {
            get { return _shade != null ? HudCanvas.OrderZOf((RectTransform)_shade.transform) : float.NaN; }
        }
        /// <summary>见 <see cref="ShadeOrderZ"/>。</summary>
        public float PopupOrderZ
        {
            get { return _popup != null ? HudCanvas.OrderZOf((RectTransform)_popup.transform) : float.NaN; }
        }
        /// <summary>见 <see cref="ShadeOrderZ"/>。</summary>
        public float TextOrderZ
        {
            get { return _text != null ? HudCanvas.OrderZOf((RectTransform)_text.transform) : float.NaN; }
        }

        static Image ImageOf(GameObject go) { return go != null ? go.GetComponent<Image>() : null; }
        /// <summary>填充那层的那颗 `Image`（画布档才非 null；世界档是几块 quad ⇒ null）。</summary>
        Image FillImage
        {
            get { return _fillRoot != null ? _fillRoot.GetComponentInChildren<Image>(true) : null; }
        }

        /// <summary>自检用：**压暗层**此刻在不在画（`ShadeVisible`）/ 文字那层在不在画（`TextVisible`）。
        /// 🔴 **2026-10-16（`T09` 块 6）补这两个口的原因**：`Visible` 的判据**只有 `_popup`**
        /// ⇒ 「整条提示不显示」这件事**只验了框那一层**；`Editor/BattleScene.cs` §4.5b 那句括号
        /// （「不是半截挂着一块压暗」）在补这两个口之前**是没有判据的**（§六 顺手发现第 1 条）。</summary>
        public bool ShadeVisible { get { return _shade != null && _shade.gameObject.activeSelf; } }
        /// <summary>见 <see cref="ShadeVisible"/>。</summary>
        public bool TextVisible { get { return _text != null && _text.gameObject.activeSelf; } }

        /// <summary>开 / 关。**重复调用同一个值不会重复设置**（`UpdateHud` 每帧都会调它）。
        /// <para>🔴 **2026-10-16（`T09` 块 6）就地修：原来第一句是 `if (_popup == null) return;`** ——
        /// 那一档（**美术目录被删 / `40k_popup` 取不到**，本工程明确支持的退路）下，压暗层与文字
        /// 在 `Build` 里**建出来就是 active 的**，而 `Visible` 的判据只有 `_popup` ⇒
        /// **`Visible` 报 `false`、屏幕上却永久挂着一整屏 α0.6118 的黑罩 + 那行字**
        /// （`UpdateHud` 每帧调 `SetVisible(false)` 也会被这一句早退掉 ⇒ 盖到对局结束）。
        /// ⇒ 改成**三件一起开关**：框在 ⇒ 照旧按它早退（重复调用不重复设置）；框不在 ⇒ 也要把
        /// 另外两层关掉。⚠️ 这一档本来就该「出声（`Build` 已出）+ 整条不显示」，⛔ 不是「半截挂着」。</para></summary>
        public void SetVisible(bool on)
        {
            if (_popup != null)
            {
                if (_popup.activeSelf == on) return;
                _popup.SetActive(on);
            }
            if (_shade != null) _shade.gameObject.SetActive(on);
            if (_text != null) _text.gameObject.SetActive(on);
        }
    }
}
