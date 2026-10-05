// WfSlider.cs — 原版音量滑块（Unity `UI.Slider`）的替身
//
// 为什么自己写：我们的 UI 是**世界空间**的（`ImageQuad` + `Label`），没有 uGUI 那套
// `Canvas` / `Slider` 组件。原版那三根是标准 uGUI `Slider`，这里按**它的实测字段**复刻。
//
// 🔴 逐字段出处（`资料/普查产出_0918/第18行_UI三小条_规格.md` §② + 2026-09-19 逐级解父链复核）：
//   · 根尺寸 **561.08 × 12.00 px**（anchorMin(0.10,0.33)/anchorMax(0.90,0.45)，高被容器撑出来
//     —— (0.45−0.33) × 容器高 **100** = **12.00**）
//     🔴 **2026-10-05 更正（A96）：这一行原来写「561.08 × 14.00」—— 两边都不是。**
//        · **战斗**（= 本文件的默认值）：解析高 **12.00**。出处 `bundle_scenes_scenes_battlearena1` /
//          `BattleSettingsPanel/Volume Sliders/{Music,FX,Voiceover} Container`（RT `3417`/`2865`/`3297`：
//          `m_SizeDelta=(0,100)`、`m_LocalScale=(1,1)`）＋ 滑块 RT（`2943`/`2631`/`3329`）锚 y `0.33→0.45`；
//          父链**无缩放** ⇒ 屏幕上就是 12.00。复核：`python 工具/menu_dump.py bundle_scenes_scenes_battlearena1
//          "Volume Sliders" --depth 2 --no-sprite` → `Music Slider … 561.08 **12.00**`（三根同值）。
//        · **主菜单设置窗**那三根：`m_SizeDelta.y = ` **13**（**未缩放**设计值；`bundle_menus_assets_all` /
//          `RectTransform_-5607048960844890` 等三根实读）—— 但那条父链的根 `Main Menu Settings Window`
//          （`RectTransform_-7066813013973172314`）`m_LocalScale = 0.9` ⇒ **屏幕上只有 13 × 0.9 = 11.7**。
//        · **14 没有出处** —— 文件头原来只写「被容器撑出来」；`0.12 × 116.667` 是事后反推、**未证实**。
//     ⇒ 轨道高**逐场景给**：`Create(..., trackH:)`，默认 = 战斗那份 **12**（见 `TrackH`）。
//   · 子物体三件，**绘制序 Background → Fill → Handle**：
//       Background  铺满 561.08×12.00，sprite **`Volume_bar_inactive`**，`Image.Type = Sliced`，border **(184,0,184,0)**
//       Fill        左对齐、宽 = value × 轨道宽，sprite **`Volume_bar_active`**，Sliced，border **(30,0,30,0)**
//       Handle Slide Area  宽 **551.08**（= 轨道宽 561.08 − 右端让 10 **设计 px**）——
//                   🔴 **左沿与轨道左沿重合、只有右边让 10**（`m_SizeDelta.x = −10` +
//                   `m_AnchoredPosition.x = −4.99988` ⇒ rect = [轨道左 + 0, 轨道右 − 10]；
//                   ⛔ 不是「左右各让 5」—— 判据见文件头那条「+17 是算错的」更正）
//       Handle      中心 x = 滑区左沿 + `m_AnchoredPosition.x`(11.99988) + 值 × 滑区宽
//                   ⇒ value 0 时 = 轨道左 + **12**、value 1 时 = 轨道右 **+2**（两端各探出 0.8 / 2），
//                   sprite **`Volume_button`**（110×110）
//   · Slider 字段：`m_Direction = LeftToRight` · `Min 0` / `Max 1` · `WholeNumbers = 0` · `Value = 1.0`
//     · `m_TargetGraphic` = Handle 的 Image（**不是**轨道 —— 点轨道不改值，只有拖手柄才算）
//
// ⚠️ **两处我们挑的（如实标着）**：
//   1. **手柄的绘制尺寸 = 原版 `preserveAspect` 取短边**（110×110 方图塞进 46.811×22.406 的非方框
//      ⇒ 画成正方形）。我们照这个画。**没有逐帧跟原版比对过**。
//      🔴 **2026-10-07（A169）**：那个 22.406 **不再写死在本件里** —— 原版逐实例不同
//      （FPS 那一行是 35.406）⇒ 由调用方按自己那根的框高给（`Create(handlePx:)`）。
//      🔴🔴 **2026-10-07（波 8）复核：22.406 是【序列化的】`m_SizeDelta.y`，不是运行时的框高** ——
//      `Slider.UpdateVisuals` 运行时把手柄的锚写成 `y 0 → 1`（`Slider.cs:616-623`）⇒
//      **框高 = 滑区高 + 22.406**（战斗 12 + 22.406 = **34.406**、设置窗 13 + 22.406 = **35.406**），
//      再经 `preserveAspect` 取短边 ⇒ 实画边长应分别是 34.406 / 35.406（设置窗再 × 0.9 = 31.87）。
//      🔑 **A168 那根 FPS 滑块已经按 35.406 画**（同一个 `m_SizeDelta`、同一个 13 高的滑区）⇒
//      同一扇窗里两族现在**不一致**（音频九根小 35~37%）。
//      ✅ **2026-10-07（波 8 · A197）已落地**：两个调用点都改成 `滑区高 + 22.406` —— 战斗三根
//      `handlePx: 34.406f`（`Battle/SettingsPanel.cs`）、设置窗三根 `handlePx: 35.406f * RootScale`
//      （`Shell/SettingsWindow.cs`）；自检期望值跟着改成 34.406 / 31.87。
//      ⚠️ 仍未**并排渲过**（本批不跑 Unity）⇒ 铁律 10⑥ 那条「抽一帧与 FPS 那根并排比」归同步点 / 真 Play。
//   2. **手柄的纵向位置**：原版 RT 里读到的 y 是 −7.00（贴根底），看起来是编辑器残留；
//      我们按**竖直居中**画。规格文档里也没给定论。
//
// 🔴 **2026-10-07（A169）：三处「逐实例不同」的值从内部常量改成调用方必传**（原来是**真缺陷**）：
//   · `queue`   —— 原来硬编码 3000（那是**战斗内**面板的档），而主菜单设置窗的压暗层 3130 / 面板 3131 /
//                  填色 3132 / 内容 3133 全在它上面 ⇒ 音频页那三根**被压暗一层**。现在三层按
//                  `queue` / `+1` / `+2` 递进（同 A168 那根 FPS 滑块的 `QContent/QText/QOverlay`）。
//   · `handlePx`—— 原来是 22.406（音频页框高）⇒ FPS 那一行会**画小 37%**（22.406 / 35.406 = 0.633）。现在必传。
//   · `handleOffset` —— 原来**一次都没加** `m_AnchoredPosition.x`（11.99988）⇒ 手柄整体偏左 12 设计 px。
//   · 另修**端帽**：原来把 `m_Border`（184 / 30，**贴图 px**）直接当**画出来的**端帽
//     ⇒ 比原版宽 2.2 倍、中段短一半。现在端帽 = `m_Border ÷ m_PixelsPerUnitMultiplier(2) × capScale`
//     （`capScale` = 该实例的设计 px→画布 px 倍数：战斗 1、设置窗 0.9）。
// 🔴🔴 **2026-10-07（波 8）更正：A169 报告附录那条「手柄起点真值 = 轨道左 + 17」是【算错的】——
//    真值就是 +12，本件（A169 之后）本来就对，这一项一个字没改。**
//    · A169 的算法：「`m_SizeDelta.x = −10` ⇒ 左右各让 5 ⇒ 滑区左沿 = 轨道左 + 5」，它把那个
//      `m_AnchoredPosition.x = −4.99988` 当成了「居中的表现」。**它不是** —— 拉伸轴上的
//      `anchoredPosition` 是从**锚矩形的中心**量起的（uGUI 那几个量的关系：
//      `offsetMin = anchoredPosition − sizeDelta × pivot`、`offsetMax = anchoredPosition +
//      sizeDelta × (1 − pivot)`，而 `rect.min = anchorMin 角 + offsetMin`、`rect.max = anchorMax 角 + offsetMax`）
//      ⇒ 对本件这组字段（锚 (0,0)-(1,1)、pivot (.5,.5)、`sizeDelta.x = −10`、`anchoredPosition.x = −5`）：
//        rect = [轨道左 + (−5) − (−10)×0.5, 轨道右 + (−5) + (−10)×0.5] = [**轨道左 + 0**, 轨道右 − 10]
//      —— **左沿与轨道左沿重合**、只有右边让 10。「左右各让 5」要的是 `anchoredPosition.x = 0`。
//    · **两条独立核**（都不是我们的常量、也不是同一份推导）：
//      ① Unity 自带 `DefaultControls.CreateSlider`（本机
//         `…/com.unity.ugui/Runtime/UGUI/UI/Core/DefaultControls.cs`）：默认滑块的 `Handle Slide Area`
//         = 锚 (0,0)-(1,1) + `sizeDelta (−20,0)` + **`anchoredPosition (0,0)`**；`Fill Area` = 同锚 +
//         `sizeDelta (−20,0)` + **`anchoredPosition (−5,0)`**。按上式：滑区 = [左+10, 右−10]（**对称**——
//         20 宽的手柄正好卡在轨道两端之内）、填区 = [左+5, 右−15]；再叠加填条自己的 `sizeDelta.x = 10`
//         ⇒ **填条左沿 = 轨道左沿、值 1 时填条右沿 = 手柄中心** ⇒ 四个字段互相自洽。
//         ⚠️ 反过来若把 `anchoredPosition` 当「从最小角量起」，默认滑块的填条会落到**轨道左端之外**
//         10px、又永远够不到右端 ⇒ 那个默认滑块就是坏的，显然不成立。
//      ② 原版运行时 dump 里 **FPS 那一行**的解析矩形：`FPS Slider` [266,84.2]–[757.2,97.2]、
//         **`Handle Slide Area` [266,84.2]–[747.2,97.2]**（A168 报告 §步骤 0 ③）—— **左沿相同**、
//         只在右边少 10 ⇒ 与本件这组字段（同值）算出来的形状一致。
//    ⇒ **手柄中心 = 轨道左 + 12 + 值 × 滑区宽**；滑区宽 = 轨道宽 − 10 **设计 px**（⛔ 不是画布 px，见
//      `SlideRightInsetPx`）。⛔ 别再往那个 12 上补 5：补了手柄就比原版偏右 5 设计 px（设置窗屏上
//      4.5 画布 px），而且会和 `Contains` 的命中区、两个宿主 + 两条自检的期望值一起失配。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一根原版音量滑块。**不是 MonoBehaviour** —— 它只是三张图 + 一个值。</summary>
    public class WfSlider
    {
        /// <summary>**战斗内**那三根的轨道尺寸（px）。🔴 **2026-10-05（A96）**：高从 `14f` 改成 **12f**
        /// —— 原版解析高 = 0.12 × 容器高 100（判据见文件头）。**这是 `Create(trackH:)` 的默认值**；
        /// 主菜单设置窗那三根**不是**这个数：那边传 `13 × SettingsWindow.RootScale`（= 11.7，见 `AuTrackH`）。</summary>
        public const float TrackW = 561.08f, TrackH = 12f;      // px
        /// <summary>手柄**序列化**框 = 原版 `Handle` 的 `m_SizeDelta`（**逐实例相同**）：设置窗音频页那三根
        /// （`RectTransform_-6029089631055872090` 等）与战斗那三根（`RectTransform_2597/2979/3354`）
        /// **全是 46.811 × 22.406**。uGUI 那边是 `preserveAspect = 1` + `Volume_button` 110×110 **方图**
        /// ⇒ 实画 = **运行时的框短边**（`Image.GetDrawingDimensions`；而运行时的框高是
        /// **滑区高 + `m_SizeDelta.y`**，见 `HandlePx` 那条）。
        /// ⚠️ **FPS 那一行的手柄实画边长是另一个数（35.406）** —— 但那是**同一条算式**的结果：
        /// 序列化框与这九根**是同一个** 46.811 × 22.406，只是那边滑区高 **13**（本面板 12）
        /// ⇒ 运行时框高 13 + 22.406 = 35.406（A168 报告 §步骤 0 ③ 逐字就是这条）。⇒ 边长**不是常量**，
        /// 由调用方按自己那根框的高给（`Create(handlePx:)`）。
        /// 🔴 **2026-10-07（波 8 · A197）三条 RT 逐字段复核**（都是 `m_SizeDelta.y = 22.406`、
        /// `m_AnchoredPosition = (11.99988, 6.1e-05)`）：FPS 那根
        /// `bundle_menus_assets_all/RectTransform/RectTransform_4475875889173987238.json`
        /// **锚 y 已经是 (0,0)-(0,1)**（编辑器里就撑着 ⇒ 它那个 35.406 连运行时都不用等）、
        /// 设置窗音频页那根 `…_-6029089631055872090.json` 与战斗那根
        /// `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_2979.json` **锚 y 是 (0,0)-(0,0)**
        /// （= 值 0 那一帧的编辑器状态）⇒ 那两处靠运行时 `Slider.UpdateVisuals` 才撑成 0→1。</summary>
        public const float HandleFrameW = 46.811f, HandleFrameH = 22.406f;
        /// <summary>⚠️ **这是【序列化】的框短边（= `HandleFrameH`），⛔ 不是实画边长** —— 名字是 A169 之前
        /// 留下的（那时以为九根都画 22.406）。**实画边长 = `滑区高 + HandleFrameH`**：
        /// uGUI `Slider.UpdateVisuals` 把手柄的 `anchorMin.y/anchorMax.y` 写成 **0 / 1**
        /// （本机 `…/com.unity.ugui/Runtime/UGUI/UI/Core/Slider.cs:616-623`，只改**轴**那一维的锚值）
        /// ⇒ 运行时框高 = 容器（`Handle Slide Area`，与滑块根同高）× 1 + `m_SizeDelta.y`。
        /// 🔴 **2026-10-07（波 8 · A197）前，两个调用点都拿它当实画边长用**（战斗画 22.406、应为 34.406；
        /// 设置窗画 20.17、应为 31.87）⇒ 小 35~37%，而**同一扇窗**里 A168 那根 FPS 滑块已经画对了
        /// （35.406 = 13 + 22.406）—— 那才是「同一条判据」。
        /// 现在**只剩 `Create` 里那条告警文案**引用它（判据在调用点，逐实例给）。</summary>
        public const float HandlePx = HandleFrameH;
        public const float HandleSpritePx = 110f;
        /// <summary>原版 `Handle.m_AnchoredPosition.x` 的**字面量**（九根音频手柄 + FPS 那根全是它）。
        /// 🔴 `Slider.UpdateVisuals` 只驱动手柄的**锚点**（`m_HandleRect.anchorMin/anchorMax`），
        /// **不动这个偏移** ⇒ 手柄中心 = 滑区左沿 + 12 + 值 × 滑区宽。出处同 `HandleFrameW` 那两条 RT。</summary>
        public const float HandleOffsetPx = 11.99988f;
        /// <summary>九宫格的 `m_Border`（**贴图 px**）—— `Volume_bar_inactive`(400×31) = (184,0,184,0)、
        /// `Volume_bar_active`(64×31) = (30,0,30,0)（`sharedassets0/Sprite/*.json` 的 `m_Border` 实读）。
        /// ⚠️ 它**只是切 uv 用的**；**画出来的**端帽 = `border ÷ ppuMul`（见下一条）。</summary>
        public const float BarBorderPx = 184f, FillBorderPx = 30f;
        /// <summary>两张图的 `m_PixelsPerUnitMultiplier` = **2.0**（Image 字段实读，两处宿主逐颗核过：
        /// 设置窗音频页 `MonoBehaviour_-3335051800813797466`(bg) / `-3703242373742624858`(fill)、
        /// 战斗 `…_4032`/`…_4228` 等三对 —— 九根**全是 2.0**）。
        /// ⇒ 端帽 = `m_Border ÷ 2`（uGUI `Image.GenerateSlicedSprite` → `GetAdjustedBorders(border / multipliedPixelsPerUnit)`，
        /// `Image.cs:1157`）。
        /// 🔴 **2026-10-07（A169）修的就是这一条**：本件原来把 184 / 30 **直接当画出来的端帽**（= 贴图 px 原样）
        /// ⇒ 端帽**宽 2.2 倍、中段短一半**（A168 那根已按 92 / 15 画，本件照它）。</summary>
        public const float PpuMul = 2f;
        /// <summary>滑区**右**端的让位 = 原版 `Handle Slide Area` 的 `m_SizeDelta.x` 字面量 **−10**。
        /// 🔴 **单位 = 设计 px**（⛔ 不是画布 px）。A169 起这个数叫 `TravelRightPx` 且**按画布 px 用** ⇒
        /// 设置窗那条路上少了 1 画布 px（原版 674.195 × 0.9 = 606.7755，我们 605.7755）。
        /// **左端让位 = 0**（滑区左沿与轨道左沿重合 —— 判据见文件头那条「+17 是算错的」更正）
        /// ⇒ 滑区 = [轨道左, 轨道右 − 10 设计 px]，与 uGUI `Slider.UpdateDrag` 拿 `m_HandleContainerRect`
        /// 当归一化矩形这件事逐句对应（`Slider.cs:630-642`）。
        /// ⚠️ 本件**逐实例**用它：`_slideInsetU = 本常量 × capScale ÷ 108`
        /// （战斗 `capScale = 1` ⇒ 屏幕上让 10；设置窗 `0.9` ⇒ 让 9 —— 见 `Create` 的 `capScale`）。
        /// 出处（两条 RT 都实读得到 −10）：`bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_2978.json` ·
        /// `bundle_menus_assets_all/RectTransform/RectTransform_452611598607941542.json`。</summary>
        public const float SlideRightInsetPx = 10f;
        const float U = 108f;                                   // px → 世界单位（和 `SettingsPanel` 同一套）

        /// <summary>**战斗内**那三根的渲染队列档（= `SettingsPanel.QPanel` 的同一个数 **3000**）。
        /// 🔴 **2026-10-07（A169）**：本件原来把队列**硬编码进内部**（= 这个数），而主菜单设置窗那一页的
        /// 压暗层 **3130** / 面板 3131 / 填色 3132 / 内容 3133 全在它上面 ⇒ 音频页那三根被压暗一层（**真缺陷**）。
        /// 现在队列由调用方按自己窗口的分层给（`Create(queue:)`，**必传**）。
        /// ⛔ 别退回「不传就默认这个数」—— 那正是这次要修掉的静默顶替。
        /// ⚠️ 三层的先后**只能靠队列**（透明物体同队列时按「到相机的距离」排，而这三张图在屏幕上位置不同
        /// ⇒ 填条会被轨道盖住，`CLAUDE.md` §三 那条）⇒ 本件按 `queue` / `+1` / `+2` 三层递进，
        /// ⛔ 别改回「三层同一个队列 + 靠 z」。</summary>
        public const int QSlider = 3000;

        /// <summary>`MenuDraw.Nine` 要的**画布 px 矩形**。🔴 **只用到它的宽高**（`trackPx` / `trackHpx`
        /// 都由调用方给 —— ⛔ **别把高换回常量 `TrackH`**：那样两种场景的轨道会被画成同一个高）。
        /// 九宫格那九块是按**根节点的局部原点**铺的（尺寸从 `−尺寸/2` 起算）⇒ 根节点摆在哪由调用方
        /// 紧接着那行 `localPosition` 定，这里的**中心值只是占位**（取画布中心，好读而已）。
        /// ⛔ 别把中心改成「世界坐标反推的画布 px」—— 那会让这一层在非 16:9 下跟着可见宽挪走。</summary>
        static PxRect TrackRectPx(float trackPx, float trackHpx)
        {
            float cx = LayoutSpace.DesignPxW * 0.5f, cy = LayoutSpace.DesignPxH * 0.5f;
            return new PxRect(cx - trackPx * 0.5f, cy - trackHpx * 0.5f, cx + trackPx * 0.5f, cy + trackHpx * 0.5f);
        }

        public string SliderName;
        public float Min = 0f, Max = 1f;
        public float Value = 1f;
        public Action<float> OnChanged;

        GameObject _root;
        GameObject _fillRoot;      // 九宫格的根（填条要整体缩放/移动）
        GameObject _bgRoot;        // 轨道（Background）九宫格的根 —— 只给 `TrackWorldH` 量几何用
        ImageQuad _handle;
        float _w, _h;
        /// <summary>手柄边长 / 手柄自己的 `m_AnchoredPosition.x`（**世界单位**，= 传进来的画布 px ÷ 108）。
        /// 🔴 逐实例的（A169 起由调用方给）⇒ 不能再当常量用（`Contains` 的命中区、`Layout` 的起点都要它）。</summary>
        float _handleH, _handleOffsetU;
        /// <summary>滑区**右**端的让位（**世界单位** = `SlideRightInsetPx × capScale ÷ 108`）。
        /// 🔴 也是逐实例的（设置窗要 × 0.9）⇒ 不能当常量用 —— `Layout` 的行程与 `SetFromPointer`
        /// 的分母都要它（A169 起那两处写的是「画布 px 的 10」，设置窗因此少 1 画布 px，波 8 修）。
        /// ⛔ 别退回 `SlideRightInsetPx / U`（那就又变成「拿战斗那个数当全部情况」）。</summary>
        float _slideInsetU;

        public bool Visible { get { return _root != null && _root.activeSelf; } }

        /// <param name="trackW">轨道宽（px）。⚠️ **2026-09-26 加**：设置窗的音频页那三根取的是
        /// **该页自己的容器宽**（684.19，`资料/联机P2P_设计与交接.md` §3·5），
        /// 和战斗内那根的 561.08 不是同一个数。**默认值 = 老值 ⇒ 战斗那条路一字未改。**</param>
        /// <param name="trackH">轨道高（px，**画布 px** —— 与本件 `U = 108` 是同一个坐标系）。
        /// 🔴 **2026-10-05 加（A96）**：原版两边**不是一个数**，所以它必须由调用方给：
        ///   · **战斗**（默认）= **12**（= 0.12 × 容器高 100，父链无缩放，屏幕上就是 12）；
        ///   · **主菜单设置窗** = `13 × SettingsWindow.RootScale` = **11.7**
        ///     —— 原版 `m_SizeDelta.y = 13` 是**未缩放**的设计值，而那条父链的根 `m_LocalScale = 0.9`，
        ///     本工程把这 0.9 **烘进矩形**（`SettingsWindow.Screen()`）⇒ 传进来的必须也是缩放后的值。
        ///     ⛔ **别传裸 13** —— 那比原版**大 11%**（`SettingsWindow` 文件头第 13-14 行那条坑）。
        /// ⚠️ 传的是**屏幕上的高**：战斗 12 → 世界 `12/108`；设置窗 11.7 → 世界 `11.7/108`。</param>
        /// <param name="queue">本实例三层的**基档**：轨道 = `queue`、填条 = `queue+1`、手柄 = `queue+2`。
        /// 🔴 **2026-10-07（A169）新加，必传**（⛔ 没有默认值 —— 默认值就是这次要修掉的静默顶替）：
        /// 三层的先后**只能靠队列**（透明物体同队列时按「到相机的距离」排，而这三张图在屏幕上位置不同
        /// ⇒ 填条会被轨道盖住，`CLAUDE.md` §三 那条）。
        /// 调用方按**自己那一层的分层**给：战斗 = `SettingsPanel.QPanel`(3000)；
        /// 设置窗音频页 = `SettingsWindow.QContent`(3133)（那扇窗的压暗层 3130 / 面板 3131 / 填色 3132
        /// **都在它下面** —— 再低一档就会被压暗层盖住；轨道取「内容」这一档 = 同属页面内容，
        /// A168 那根 FPS 滑块的轨道也取同一档）。</param>
        /// <param name="handlePx">手柄**实画边长**（**画布 px**）= 运行时手柄框的**短边**
        /// （= **滑区高 + `m_SizeDelta.y`(22.406)**，见 `HandlePx` 的那条判据）：
        /// 战斗/音频那九根 = 12 + 22.406 = **34.406**、设置窗音频三根 = (13 + 22.406) × 0.9 = **31.87**、
        /// FPS 那一行 = (13 + 22.406) × 0.9 = 31.87（同一个 35.406）。
        /// 🔴 **2026-10-07（A169）新加，必传** —— 不许当常量（⛔ 更不许再拿 `HandlePx`(22.406) 顶替，
        /// 那是 **序列化**值、不是实画值；A197 前就是这么错的）。</param>
        /// <param name="handleOffset">手柄自己的 `m_AnchoredPosition.x`（**画布 px**）。
        /// 🔴 **2026-10-07（A169）新加，必传**：本件原来**一次都没加它** ⇒ 手柄整体偏左 12 设计 px
        /// （原版字面量 `HandleOffsetPx` = 11.99988；设置窗传 `× RootScale` = 10.7999）。
        /// 判据：`Slider` 只写手柄的**锚点**，这个偏移原样留在 `anchoredPosition` 里
        /// ⇒ 手柄中心 = **滑区左沿（= 轨道左端）** + 它 + 值 × 滑区宽。
        /// ⛔⛔ **别把这个值改成 +17** —— A169 报告附录那条「还要再加 5」是算错的，
        /// 判据（含两条独立核）见文件头；滑区的**左**端不让位。</param>
        /// <param name="capScale">**设计 px → 画布 px** 的倍数，本实例的：
        /// · 九宫格**端帽** = `m_Border ÷ PpuMul × capScale`；
        /// · **滑区右端的让位** = `SlideRightInsetPx(10 设计 px) × capScale`（波 8 起 —— 之前它写死成
        ///   10 **画布 px**，设置窗那条路上少了 1 画布 px）。
        /// · **战斗**那条父链无缩放 ⇒ 传 **1**（端帽 92 / 15 画布 px = 原版设计值；滑区让 10）；
        /// · **主菜单设置窗**根那层 0.9 是烘进矩形的（`SettingsWindow.Screen()`）⇒ 传 `RootScale`
        ///   （端帽 **82.8 / 13.5**、滑区让 **9** —— 与 A168 那根 FPS 滑块的自检期望同一个数）。
        /// ⚠️ 手柄那两样（`handlePx` / `handleOffset`）**不吃**这个倍数 —— 它们由调用方自己烘好
        /// （同 `trackW` / `trackH` 的口径）。🔴 为什么不从 `trackH`/`handlePx` 反推这个倍数：
        /// 两边的轨道高**本来就不同源**（12 vs 13），FPS 那根的手柄框高又是 35.406 ⇒ 反推就是
        /// 「拿一个值当全部情况」（`CLAUDE.md` 铁律 5·c）。
        /// ⚠️ **名字**：A169 起叫 `capScale`（那时它只管端帽）；波 8 起它还管滑区让位 ⇒ 名字偏窄。
        /// 正名（`pxScale` 之类）要**同时**改 `Battle/SettingsPanel.cs` 与 `Shell/SettingsWindow.cs`
        /// 两个调用点 ⇒ 本轮**没改**（改了会让那个文件的编译悬在别人手里），照实标在这儿。</param>
        public static WfSlider Create(Transform parent, string name, Vector3 center, float value,
                                      Action<float> onChanged, int queue, float handlePx, float handleOffset,
                                      float capScale, float trackW = TrackW, float trackH = TrackH)
        {
            // 🔴 **不许静默失败**（A169）：三样入参任一样拿不到 / 越界，都当场出声 ——
            //    画出来会是「看不见」或「压不住底板」，而屏幕上只会显示成一根怪滑块。
            if (queue < 1)
                Debug.LogWarning($"[WfSlider] `{name}` 的渲染队列给了 {queue} —— 三层会落在 3000 以下，"
                    + "会被自己那扇窗的底板/压暗层盖住（战斗传 SettingsPanel.QPanel、设置窗传 SettingsWindow.QContent）");
            if (handlePx <= 0f)
                Debug.LogWarning($"[WfSlider] `{name}` 的手柄边长给了 {handlePx} —— 手柄画不出来"
                    + $"（原版 = 滑区高 + 序列化框短边 {HandleFrameH}：战斗/音频页 12 + 22.406 = 34.406、"
                    + "设置窗 (13 + 22.406) × 0.9 = 31.87）");
            if (capScale <= 0f)
                Debug.LogWarning($"[WfSlider] `{name}` 的端帽倍率给了 {capScale} —— 九宫格端帽会画成 0"
                    + "（战斗传 1、设置窗传 SettingsWindow.RootScale）");

            var s = new WfSlider();
            s.SliderName = name;
            s.OnChanged = onChanged;
            s.Value = value;

            s._w = trackW / U; s._h = trackH / U;
            s._handleH = handlePx / U;
            s._handleOffsetU = handleOffset / U;
            // 滑区右端的让位 = 原版那条 `m_SizeDelta.x = −10`（**设计 px**）× 本实例的设计→画布倍数。
            // 🔴 复用一个已有形参（`capScale`）而不是新开一个：两者本来就是**同一个倍数**
            //    （战斗 1、设置窗 0.9），多传一个只是多一处能写错的地方；
            //    ⛔ 也**不许**从 `trackH` / `handlePx` 反推（那几个数两边不同源，铁律 5·c）。
            s._slideInsetU = SlideRightInsetPx * capScale / U;

            s._root = new GameObject("slider_" + name, typeof(RectTransform));
            s._root.transform.SetParent(parent, false);
            // 🔴 **2026-10-11（A218）**：滑块根也是 `RectTransform` + 写 `sizeDelta` ——
            //    判据 = **原版那一颗 `Slider` 节点自己的矩形**：战斗那三根实读 **561.08 × 12.00**
            //    （`bundle_scenes_scenes_battlearena1`：`BattleSettingsPanel/Volume Sliders/{Music,FX,Voiceover}
            //     Container` 的 `m_SizeDelta=(0,100)` + 滑块锚 y `0.33→0.45` ⇒ `(0.45−0.33)×100 = 12`；
            //    宽 561.08 见本文件头那一段），设置窗那几根由调用方按同一份口径给 `trackW/trackH`。
            //    ⚠️ **尺寸取形参**（`trackW/trackH` 就是本实例的轨道 px）—— 与本件 `_w/_h` 那两行**同一个数**
            //    （`s._w = trackW / U`）。改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218
            //    「滑块根 = 轨道矩形」红（设置窗那半边在 `Editor/SettingsScene.cs` 同一节核）。
            MenuDraw.SetPxSize(s._root.transform, trackW, trackH);
            s._root.transform.localPosition = center;

            var bgTex = CardArt.Ui("Volume_bar_inactive");
            var fillTex = CardArt.Ui("Volume_bar_active");
            var handleTex = CardArt.Ui("Volume_button");
            if (bgTex == null || fillTex == null || handleTex == null)
            {
                // 不许静默失败：图不在 = 滑块画不出来，而玩家只会看到「这里什么都没有」
                Debug.LogWarning($"[WfSlider] 音量滑块的图缺了："
                    + $"bar_inactive={(bgTex != null)} bar_active={(fillTex != null)} button={(handleTex != null)}"
                    + "（跑 `工具/` 那套取图脚本把三张图放进 Resources/Art/ui/）");
            }
            else
            {
                // 🔴 **2026-10-04（A50③）：两层都收口到公共件 `MenuDraw.Nine`** —— 原来直调
                //    `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，拿不到 `clip`）。
                //    与旧代码**逐项等价**，三样都别改：
                //    ① **尺寸** = 轨道 `trackW × trackH`（px，**两个都是调用方给的** —— 见 `Create` 的
                //       `<param name="trackH">`）：`MenuDraw.Nine` 内部按 `LayoutSpace.Px`
                //       折世界尺寸，与本件那个 `U = 108f` 是**同一个换算**；
                //    ② **落位** = 每层紧跟的 `localPosition` 显式给回 `(0, 0, z)`（旧代码传的 center 就是它）
                //       —— 九块是按**根节点的局部原点**铺的（尺寸从 `−尺寸/2` 起算），与矩形中心值无关，
                //       ⛔ **别删那两行**：删了这一层会跟着 `MenuDraw.Local` 的映射跑（非 16:9 会挪位）；
                //    ③ **渲染队列** = `queue` / `queue+1` / `queue+2`（A169 起**由调用方给**，见 `Create`
                //       的 `<param name="queue">`）—— 三层**只能靠队列**排（同队列时按到相机的距离排，
                //       填条中心偏左 ⇒ 离相机更远 ⇒ 会被轨道盖住）。
                // ① Background：Sliced，铺满。端帽 = `m_Border 184 ÷ ppuMul 2 × capScale`（A169 修：
                //    原来把 184 直接当画出来的端帽 ⇒ 宽 2.2 倍）
                float bgCap = BarBorderPx / PpuMul * capScale;
                var bg = MenuDraw.Nine(s._root.transform, bgTex, TrackRectPx(trackW, trackH),
                                       new Vector4(BarBorderPx, 0f, BarBorderPx, 0f), bgTex.width, bgTex.height,
                                       queue, name: "slider_bg",
                                       borderOutPx: new Vector4(bgCap, 0f, bgCap, 0f));
                if (bg != null) bg.transform.localPosition = new Vector3(0f, 0f, 0f);
                s._bgRoot = bg;      // 自检量轨道几何用（`TrackWorldH`）
                // ② Fill：Sliced，左对齐，宽随值。端帽 = `30 ÷ 2 × capScale`（同上）
                // ⚠️ **实现上用的是「整体横向缩放」而不是「按目标宽重建九宫格」** ——
                //    代价是左右那两个端帽会跟着缩（拖到很小的时候端帽变窄）。
                //    我们挑的：重建九宫格要每次 drop_value 都销毁/新建 9 个 quad，拖动时太吵；
                //    而这条填充条只有横端帽（border 上下都是 0），缩放的观感差别很小。**未逐帧比对过**。
                float fillCap = FillBorderPx / PpuMul * capScale;
                s._fillRoot = MenuDraw.Nine(s._root.transform, fillTex, TrackRectPx(trackW, trackH),
                                            new Vector4(FillBorderPx, 0f, FillBorderPx, 0f), fillTex.width, fillTex.height,
                                            queue + 1, name: "slider_fill",
                                            borderOutPx: new Vector4(fillCap, 0f, fillCap, 0f));
                if (s._fillRoot != null) s._fillRoot.transform.localPosition = new Vector3(0f, 0f, -0.001f);
                // ③ Handle：正方形，边长 = 调用方给的**实画边长**（A169 起不是常量）
                s._handle = ImageQuad.Create(s._root.transform, handleTex, new Vector3(0f, 0f, -0.002f),
                                             s._handleH, new Vector2(0.5f, 0.5f), "slider_handle");
                // `ImageQuad.Create` 自己不设队列（= 材质默认档 3000）⇒ 三层里最高的那一档显式写上
                if (s._handle != null) s._handle.SetRenderQueue(queue + 2);
            }
            s.Layout();
            return s;
        }

        /// <summary>按当前 `Value` 摆 Fill 与 Handle。</summary>
        void Layout()
        {
            float t = Mathf.InverseLerp(Min, Max, Mathf.Clamp(Value, Min, Max));
            if (_fillRoot != null)
            {
                float w = Mathf.Max(0.0001f, t * _w);
                _fillRoot.transform.localScale = new Vector3(w / _w, 1f, 1f);
                _fillRoot.transform.localPosition = new Vector3(-_w * 0.5f + w * 0.5f, 0f, -0.001f);
            }
            if (_handle != null)
            {
                // 🔴 手柄中心 = **滑区左沿**（= 轨道左端，左端不让位，判据见文件头那条更正）
                //    + 手柄自己的 `m_AnchoredPosition.x` + 值 × 滑区宽（滑区 = 轨道宽 − 右端 10 **设计 px**）。
                //    A169 前这里**连中间那一项都没有** ⇒ 整体偏左 12 设计 px。
                // ⚠️ 值 0 时手柄左缘只离轨道左端 0.8 画布 px（看着就是"贴着左端"）、值 1 时中心探出
                //    轨道右端 **2** 画布 px —— 原版就是这样，别「顺手」夹回来（夹了手柄就与原版对不上；
                //    `Contains` 那边已让出手柄探出去的那一块）。
                float x = -_w * 0.5f + _handleOffsetU + t * (_w - _slideInsetU);
                _handle.transform.localPosition = new Vector3(x, 0f, -0.002f);
            }
        }

        /// <summary>🔴 **命中判据（整个工程只此一份）：轨道 ∪ 手柄**，写成一块矩形。
        /// 两个宿主都走它 —— 本件的 <see cref="Contains"/>（世界坐标）与
        /// `Shell/SettingsWindow.cs` 那根 **FPS 滑块**的命中区（画布 px）。⛔ **别在任何一侧另写一份**
        /// （「同一份判据写两处 = 迟早不一致」，`CLAUDE.md` §三；2026-10-07 A193 前两处口径就不一致）。
        ///
        /// <para>**为什么并集本身就是一块矩形**：两块共用同一个纵向带 —— 半高都是
        /// `max(轨道半高, 手柄半高)`（手柄比轨道高，点在**手柄上**也该算命中）⇒ 横向只要把两端取 min/max。</para>
        ///
        /// <para>**单位随调用方**（本件传世界单位、设置窗传设计 px）—— 它是**纯几何**，不碰任何全局换算。</para>
        ///
        /// <para>判据出处：原版那两颗 Image（`Background` 轨道 / `Handle` 手柄）的 `m_RaycastTarget` 都是 **1**
        /// （九根 + FPS 那根逐颗实读）⇒ 两块的射线都冒泡到父件的 `Slider`。而手柄中心带上
        /// `m_AnchoredPosition.x`(12) 之后在两端会**探出轨道**（值 0 时左缘探出 0.8 设计 px、值 1 时右缘探出
        /// 19.7 —— 见文件头那条「+17 是算错的」更正）⇒ 只按轨道判会让那一块**静默点不到**。</para></summary>
        /// <param name="halfH">纵向半高（= `max(halfTrackH, halfHandle)`；没有手柄时 = `halfTrackH`）。</param>
        public static void HitBand(float trackL, float trackR, float halfTrackH,
                                   float handleCx, float halfHandle, bool hasHandle,
                                   out float lx, out float rx, out float halfH)
        {
            halfH = hasHandle ? Mathf.Max(halfTrackH, halfHandle) : halfTrackH;
            lx = trackL; rx = trackR;
            if (!hasHandle) return;
            lx = Mathf.Min(lx, handleCx - halfHandle);
            rx = Mathf.Max(rx, handleCx + halfHandle);
        }

        /// <summary>世界坐标是不是落在这根滑块的**轨道**或**手柄**上（点轨道 = 直接跳值，原版 `m_TargetGraphic`
        /// 是手柄，但单机点轨道不改值会很难用；这里点轨道也认 —— **这一条我们挑的**）。
        /// 🔴 **2026-10-07（A169）**：**手柄那一块也要算命中** —— 手柄中心带上 `m_AnchoredPosition.x` 之后
        /// 在右端会**探出轨道**（原版：值 0 时中心在轨道左端内 +12、值 1 时在右端**外** +2 —— 判据见
        /// 文件头那条更正），而那两颗手柄 Image 的 `m_RaycastTarget` 都是 **1**（九根逐颗实读）
        /// ⇒ 原版点得到。原来只按轨道判 ⇒ 「指针落在手柄上」这一条在值接近 1 时会**静默判不中**
        /// （`Editor/BattleScene.cs` 那条断言正是这么写的）。
        /// 🔴 **2026-10-07（波 8 · A193）**：几何**收口到 `HitBand`**（判据只此一份）—— 设置窗那根
        /// FPS 滑块走的是同一条，别在这里改口径。</summary>
        public bool Contains(Vector3 world)
        {
            if (_root == null) return false;
            var l = _root.transform.InverseTransformPoint(world);
            float handleCx = _handle != null ? _handle.transform.localPosition.x : 0f;
            float lx, rx, halfH;
            HitBand(-_w * 0.5f, _w * 0.5f, _h * 0.5f, handleCx, _handleH * 0.5f, _handle != null,
                    out lx, out rx, out halfH);
            return Mathf.Abs(l.y) <= halfH && l.x >= lx && l.x <= rx;
        }

        /// <summary>按指针的世界坐标取值。返回**值变了没有**。
        /// 🔴 **逐句照原版 `Slider.UpdateDrag`**（本机 `…/com.unity.ugui/Runtime/UGUI/UI/Core/Slider.cs:630-642`）：
        /// `clickRect = m_HandleContainerRect`（= **`Handle Slide Area`**，⛔ 不是轨道）、
        /// `localCursor -= clickRect.rect.position;`、`val = Clamp01(localCursor[axis] / clickRect.rect.size[axis])`
        /// ⇒ **值的 0 在滑区左沿**（= 轨道左端 —— 左端不让位）、**1 在滑区右沿**（= 轨道右端 − 10 设计 px）、
        /// **分母 = 滑区宽**（战斗 551.08；设置窗 674.195 × 0.9 = **606.7755** 画布 px）。
        /// ⚠️ A169 起这里写的是「轨道宽 − `TravelRightPx`(**画布** px)」⇒ 设置窗那条路少了 1 画布 px；
        /// 波 8 改成 `_slideInsetU`（逐实例）后两边都对。⛔ 别改回「拿整根轨道当分母」，也⛔ 别把起点挪成 +5。</summary>
        public bool SetFromPointer(Vector3 world)
        {
            if (_root == null) return false;
            var l = _root.transform.InverseTransformPoint(world);
            float t = Mathf.Clamp01((l.x + _w * 0.5f) / (_w - _slideInsetU));
            float v = Mathf.Lerp(Min, Max, t);
            if (Mathf.Abs(v - Value) < 1e-4f) return false;
            Value = v;
            Layout();
            if (OnChanged != null) OnChanged(Value);
            return true;
        }

        public void SetValue(float v, bool fire)
        {
            v = Mathf.Clamp(v, Min, Max);
            bool changed = Mathf.Abs(v - Value) > 1e-4f;
            Value = v;
            Layout();
            if (fire && changed && OnChanged != null) OnChanged(Value);
        }

        public void SetVisible(bool on) { if (_root != null) _root.SetActive(on); }

        // ---- 自检用 ----
        /// <summary>三张图都取到了没有（缺图 = 玩家什么都看不见）。</summary>
        public bool HasArt
        {
            get
            {
                bool bg = _fillRoot != null && _fillRoot.transform.childCount > 0;
                return bg && _handle != null && _handle.Texture != null;
            }
        }
        /// <summary>轨道**渲出来的世界高**（自检用）。🔴 量的是**几何**，不是把 `trackH` 参数念一遍 ——
        /// 取 Background 那几块 quad 的**并集高**（九宫格上下 border 都是 0 ⇒ 三块同高）。
        /// 「常量改了、`TrackRectPx` 忘了跟着 `trackH` 走」那种改法，这里会**当场露出来**。
        /// ⚠️ 单位是**世界单位**（×108 = 画布 px）；⛔ **不含手柄**（手柄比轨道高，混进来就量错）。</summary>
        public float TrackWorldH
        {
            get
            {
                if (_bgRoot == null) return 0f;
                var qs = _bgRoot.GetComponentsInChildren<ImageQuad>(true);
                float h = 0f;
                for (int i = 0; i < qs.Length; i++)
                    if (qs[i] != null) h = Mathf.Max(h, qs[i].WorldH);
                return h;
            }
        }
        public Vector3 WorldPos { get { return _root != null ? _root.transform.position : Vector3.zero; } }
        public Vector3 HandleWorldPos { get { return _handle != null ? _handle.transform.position : WorldPos; } }
        /// <summary>轨道左端 / 右端的世界坐标（自检拿它喂指针，验「点最左 = 0、点最右 = 1」）。</summary>
        public Vector3 LeftWorld { get { return _root.transform.TransformPoint(new Vector3(-_w * 0.5f, 0f, 0f)); } }
        public Vector3 RightWorld { get { return _root.transform.TransformPoint(new Vector3(_w * 0.5f, 0f, 0f)); } }
    }
}
