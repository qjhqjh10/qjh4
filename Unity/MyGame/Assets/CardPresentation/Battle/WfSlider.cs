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
//     · `m_TargetGraphic` = Handle 的 Image
//       🔴 **2026-10-19（A1301①）就地订正**：这行原来接着写「**不是**轨道 —— 点轨道不改值，只有拖手柄才算」
//       —— **不成立**。`targetGraphic` 只服务过渡美术、**不管输入**；输入那条是
//       `Slider.OnPointerDown`：指针**不在** `m_HandleRect` 里就走 `else` 支
//       `UpdateDrag(...)`（源码注释原文 `// Outside the slider handle - jump to this point instead`）
//       ⇒ **点轨道 = 值跳到指针那里**；事件能到 `Slider` 上靠的是子件 `Background`(MB_4355) /
//       `Fill`(MB_4553) **`m_RaycastTarget` 都是 1、`m_RaycastPadding` 都是 0**（本包逐颗实读）。
//       ⇒ 我们「点轨道也认」**本来就是照原版做的**（原来那句标成「我们挑的」，已订正）。
//
// ⚠️ **两处我们挑的（如实标着）**：
//   1. **手柄的绘制尺寸 = 原版 `preserveAspect` 取短边**（110×110 方图塞进 46.811×22.406 的非方框
//      ⇒ 画成正方形）。我们照这个画。**没有逐帧跟原版比对过**。
//      🔴 **2026-10-07（A169）**：那个 22.406 **不再写死在本件里** —— 原版逐实例不同
//      （FPS 那一行是 35.406）⇒ 由调用方按自己那根的框高给（`Create(handlePx:)`）。
//      🔴🔴 **2026-10-07（波 8）复核：22.406 是【序列化的】`m_SizeDelta.y`，不是运行时的框高** ——
//      `Slider.UpdateVisuals` 运行时把手柄的锚写成 `y 0 → 1` ⇒
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
//
// ═══════════════════════════════════════════════════════════════════════════════════════
// 🔴 **2026-10-16（`T09` 块 6 · 第二件）：本件加【双模式】—— `Create(..., uguiHost)`。**
//   起因：`Battle/SettingsPanel.cs` 那一整扇 2026-10-16 已迁到 `PopUps`/0 的子画布上，而**这三根滑块
//   留在世界空间** ⇒ 当时正处**半迁移态**（子画布在 `PopUps` 层 ⇒ 面板底盖住那三根；它们仍跟 `HudRoot` 震屏）。
//   判据与落点 → `资料/普查产出_1016并行/S6S9_卡体与残件_施工方案.md` §2·2 与 §3·2（`WfSlider` 那一行）。
//
//   · **落点：它是【面板内部件】、不是子画布** ⇒ **不新增排序层、⛔ 不进 `HudCanvas.SubCanvasRank`**，
//     挂**它所属面板的画布容器**（战斗侧 = `SettingsPanel._panel`，那个 743.2×758.6 px 的面板帧）。
//   · **双模式**：`uguiHost == null`（出厂）⇒ **世界档 —— 与加本档之前逐位相同**
//     （`Shell/SettingsWindow.cs` 那三根走的就是它）；**非空 ⇒ 画布档**。
//     先例 = `ErrorMessageBanner.Create(..., uguiHost)` · `WaitBanner.Create(parent, uguiHost)` ·
//     `Tooltip.UseHost`（本会话块 6 那两件）。
//     ⚠️ **⛔ 不许做成全局开关** —— 本件被 `Battle/SettingsPanel.cs`（3 处）与
//     `Shell/SettingsWindow.cs`（3 处，搜 `WfSlider.Create`）**共用**，全局开关会把壳侧那三根一起翻掉。
//
//   · 🔴 **单位（本件的全部模式差异就在这一个 `u` 上）**：
//       世界档：局部单位 = **世界单位**（`U = 108f` px / 世界单位）⇒ `_w = trackW / U` …
//       画布档：局部单位 = **设计 px**（canvas 单位就是设计 px，推导 → `Battle/HudCanvas.cs` 文件头）
//               ⇒ `u = 1` ⇒ `_w = trackW` …
//     四个字段（`_w/_h` · `_handleH` · `_handleOffsetU` · `_slideInsetU`）与命中口那个 `ppu` 各取一次 `u`；
//     **`Contains` / `SetFromPointer` / `Layout` 的算式一个字都没改**（三个 doc 里各加了「换算在哪」那一句）。
//     🔑 为什么这样够：`RectTransform.InverseTransformPoint(world)` 交出来的**就是本件的局部单位**
//     （画布那条锚点链上 `localScale` 恒 1，唯一那个根缩放已经折进单位里）⇒ 画布档下它**就是设计 px**、
//     且**任何宽高比下都成立**（`CanvasScaler` 已经把全局缩放做了：4:3 下 `scaleFactor 0.75`
//     ⇒ 1 世界单位 = **144** 画布单位，而轨道仍是 561.08 **设计 px**）。
//     ⇒ 「设计 px 进来、设计 px 出去」——**不是**在算式里乘系数（那会踩 `T09` §七·8 那条「两轴系数不同」的坑）。
//
//   · **三层先后**：世界档靠 `renderQueue`（`queue` / `+1` / `+2`）；画布档**只能靠兄弟序**
//     ⇒ `HudCanvas.Add(rt, −queue / −queue−1 / −queue−2)` —— **取负**（`queue` 大 = 画在上面，
//     而 `Group.Add` 的 z 大 = **画在底下**；判据 → `T09` §五·补⑩ 那条「照搬值会把熄灯层画到最上面」的订正）。
//
//   · **自检读口**（原有四个 `TrackWorldH` / `FillWorldW` / `FillWorldLeftX` / `FillCapWorldW`，
//     外加本档新加的 `TrackSizePx` / `FillSizePx` / `HandleSizePx` / `Node`+`FillNode`+`HandleNode` /
//     `OnCanvas` / 三层 `*OrderZ`）：**原那四个口的「世界单位」契约保持不变**（画布档读 `sizeDelta`
//     （真 px）之后**除以 `U`** 交出去）⇒ 调用点那句 `× 108f` **两种模式同一个算式**（⛔ 别在调用点分档）。
//     🔴 **画布档一律读 `sizeDelta`** —— ⛔ 别读世界尺寸再折：`T09` §七·8（画布→世界的两轴系数不同、还随屏变）。
//   ⚠️ **一处如实标的权宜**：`HudCanvas.MakeSlicedPiece` 的 `pixelsPerUnitMultiplier` 恒 1，而本件两张条图的
//     端帽 = `m_Border ÷ ppuMul`（2）⇒ 建完自己把那一个字段补上（`ApplyPpuMul`；先例 =
//     `WaitBanner.BuildCanvas` 给 `Image (Tiled)` 补 `ppuMul`）—— 同一条「`HudCanvas` 缺这个口」的账。
// ═══════════════════════════════════════════════════════════════════════════════════════
using System;
using UnityEngine;
using UnityEngine.EventSystems;   // 🆕 2026-10-20（`T09` A3-b）：画布档的输入走 uGUI（`EventTrigger` + `PointerEventData`）
using UnityEngine.UI;     // 🆕 画布档要 `Image`（`ppuMul` / `sprite` 那两个读口）；世界档一个字都不用

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
        /// （本机 `…/com.unity.ugui/Runtime/UGUI/UI/Core/Slider.cs` 的 `Slider.UpdateVisuals`，只改**轴**那一维的锚值）
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
        /// <summary>🔴 **2026-10-19（A1301①）**：手柄那一颗 `Image` 的 **`m_RaycastPadding`**
        /// （分量序 **L,B,R,T**；**负值 = 外扩、正值 = 内缩** —— 判据见 `Shell/MenuDraw.cs` 的
        /// `PaddedRect` / `PaddedHitRect` 那两段 uGUI 出处，本工程已经连修两次、方向反过一次）。
        /// <para>实据 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/`
        /// 的 `MonoBehaviour_4502`（Music `Handle`）/ `…_4077`（FX）/ `…_4984`（Voiceover）
        /// —— **三颗四分量全是 −25**（逐颗实读；`go` 反查的名字分别是
        /// `Handle` / `Handle_584` / `Handle_858`）。设置窗那根 FPS 滑块的
        /// `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3648122393879609434.json`
        /// **也是同一个 −25**。</para>
        /// <para>⇒ 原版那三颗手柄的命中框 **96.811 × (滑区高 + 22.406 + 50)**（战斗 **96.811×84.406**），
        /// 比**画出来**的那个 34.406 正方**大一圈**（宽那一维尤其大 —— 原版框宽 46.811 是序列化值、
        /// 手柄那颗是 `preserveAspect` 取短边画成正方）。</para>
        /// <para>⛔ **别拿序列化的 `m_SizeDelta.y`(22.406) 当框高** —— 运行期框高 = 滑区高 + 22.406
        /// （`Slider.UpdateVisuals` 把非轴那一维的锚写成 0/1、`OnEnable` 就调它；判据见 `HandlePx` 那条）。</para></summary>
        public const float HandlePadPx = -25f;
        /// <summary>九宫格的 `m_Border`（**贴图 px**）—— `Volume_bar_inactive`(400×31) = (184,0,184,0)、
        /// `Volume_bar_active`(64×31) = (30,0,30,0)（`sharedassets0/Sprite/*.json` 的 `m_Border` 实读）。
        /// ⚠️ 它**只是切 uv 用的**；**画出来的**端帽 = `border ÷ ppuMul`（见下一条）。</summary>
        public const float BarBorderPx = 184f, FillBorderPx = 30f;
        /// <summary>两张图的 `m_PixelsPerUnitMultiplier` = **2.0**（Image 字段实读，两处宿主逐颗核过：
        /// 设置窗音频页 `MonoBehaviour_-3335051800813797466`(bg) / `-3703242373742624858`(fill)、
        /// 战斗 `…_4032`/`…_4228` 等三对 —— 九根**全是 2.0**）。
        /// ⇒ 端帽 = `m_Border ÷ 2`（uGUI `Image.GenerateSlicedSprite` → `GetAdjustedBorders(border / multipliedPixelsPerUnit)` —— 就是 `Image.multipliedPixelsPerUnit`）。
        /// 🔴 **2026-10-07（A169）修的就是这一条**：本件原来把 184 / 30 **直接当画出来的端帽**（= 贴图 px 原样）
        /// ⇒ 端帽**宽 2.2 倍、中段短一半**（A168 那根已按 92 / 15 画，本件照它）。</summary>
        public const float PpuMul = 2f;
        /// <summary>滑区**右**端的让位 = 原版 `Handle Slide Area` 的 `m_SizeDelta.x` 字面量 **−10**。
        /// 🔴 **单位 = 设计 px**（⛔ 不是画布 px）。A169 起这个数叫 `TravelRightPx` 且**按画布 px 用** ⇒
        /// 设置窗那条路上少了 1 画布 px（原版 674.195 × 0.9 = 606.7755，我们 605.7755）。
        /// **左端让位 = 0**（滑区左沿与轨道左沿重合 —— 判据见文件头那条「+17 是算错的」更正）
        /// ⇒ 滑区 = [轨道左, 轨道右 − 10 设计 px]，与 uGUI `Slider.UpdateDrag` 拿 `m_HandleContainerRect`
        /// 当归一化矩形这件事逐句对应（见 `Slider.UpdateDrag`）。
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
        /// <summary>填条那棵九宫格的**根**（节点名 `slider_fill` —— ⚠️ 两个自检宿主按**这个名字**取它，
        /// ⛔ 别改名）。🔴 **2026-10-10（`A1359`）起它自己【不带 `localScale`】了** ——
        /// 填条的横向变化改成打在下面三块**子 quad** 上（见 `LayoutFill`）。
        /// ⇒ 它**曾经**是 `Shell/MenuDraw.QuadRectPx` 那条「自带 `localScale` 的节点」名单里的一员，
        /// 现在仍是「量它的几何要自己乘回缩放」的一处，只是**缩放搬到了子件上**：
        /// 子件的 `WorldW` 是**建件时的整宽**、不含它自己的缩放 ⇒ **⛔ 别对填条这棵子树取并集当渲染矩形**，
        /// 要量就走 `FillWorldW` / `FillWorldLeftX`。</summary>
        GameObject _fillRoot;
        GameObject _bgRoot;        // 轨道（Background）九宫格的根 —— 只给 `TrackWorldH` 量几何用
        /// <summary>填条那三块子 quad（左端帽 / 中段 / 右端帽）—— 照 `ImageQuad.CreateNineSlice` 的命名取
        /// （`slider_fill_01` / `_11` / `_21`；上下 `border` 都是 0 ⇒ 行 0 / 2 的高为 0、创建时就被跳过）。
        /// ⚠️ 取不到 ⇒ `_fillAnchorMode = false`、退回旧路（整体横向缩放）＋ 出声，见 `Create`。</summary>
        ImageQuad _fillL, _fillC, _fillR;
        /// <summary>端帽 / 中段的**建件宽**（世界单位）= 建的那一刻 `MenuDraw.Nine` 实际给出去的宽。
        /// 🔴 从子件自己的 `WorldW` **读回来**（⛔ 不是拿 `FillBorderPx ÷ PpuMul × capScale ÷ U` 再算一遍 ——
        /// 那样「建件那一步被改了」这边不会跟着变，见 `CLAUDE.md` 铁律 5·c）。</summary>
        float _fillCapU, _fillMidU;
        /// <summary>填条走不走「锚点式」（`A1359` 起正常为 `true`）。`false` = 那三块子 quad 没按名字找到
        /// ⇒ 退回旧的整体横向缩放（`Create` 里已经出声，⛔ 不是静默失败）。</summary>
        bool _fillAnchorMode;
        /// <summary>手柄那一层。🔴 **2026-10-16（`T09` 块 6）**：类型从 `ImageQuad` 换成 **`HudPiece`**
        /// （那层适配件，两档共用同一个字段）—— 世界档 = `HudPiece.Of(ImageQuad.Create(…))`、
        /// 画布档 = `HudCanvas.MakeImageInParent(…)` 的返回值。用法（`.transform` / `.Texture`）一字未改。
        /// ⚠️ **`SetRenderQueue` 只对世界档那把 quad 有意义**（画布档的先后归兄弟序）⇒ 那边走 `.Quad` 判空。</summary>
        HudPiece _handle;
        /// <summary>🔴 **2026-10-16（`T09` 块 6）：画布档（uGUI）开关** —— `uguiHost != null` ⇒ `true`。
        /// ⚠️ **必须在 `Layout()` 之前定**（它按本档选填条那一支的摆法）。</summary>
        bool _ugui;
        /// <summary>两张条图在**画布档**下的那颗 `Image`（九宫格 = 一张 `Image (Sliced)`）——
        /// 世界档恒 `null`（那边是九块 quad、没有 `Image`）。给读口量 `sizeDelta` 与 `ppuMul` 用。</summary>
        Image _bgImg, _fillImg;
        /// <summary>两层自己的**局部 z**：世界档 = 建件时那两行字面量（−0.001 / −0.002，**只有世界档有意义**）、
        /// 画布档 = **0**（画布上的 z 会把件推离画布平面 ⇒ `SettingsPanel` 那条「⛔ 别再写进 `localPosition.z`」）。</summary>
        float _fillZ, _handleZ;
        float _w, _h;
        /// <summary>手柄边长 / 手柄自己的 `m_AnchoredPosition.x`（**世界单位**，= 传进来的画布 px ÷ 108）。
        /// 🔴 逐实例的（A169 起由调用方给）⇒ 不能再当常量用（`Contains` 的命中区、`Layout` 的起点都要它）。</summary>
        float _handleH, _handleOffsetU;
        /// <summary>手柄**实画边长**（**画布 px**，`Create` 时由调用方给）。
        /// 🔴 **2026-10-19（A1301①）**：它同时就是手柄那颗 `Image` 的**运行期框高** ——
        /// `Slider.UpdateVisuals` 把非轴那一维的锚写成 `0/1` ⇒ 框高 = 滑区高 + `HandleFrameH`，
        /// 而 `HandleFrameW`(46.811) 恒大于它 ⇒ `preserveAspect` 取的短边**就是框高**。
        /// 命中框（`HitHandlePadded`）要拿它当那颗 `Image` 的 `rect` 高（⛔ 不是新开一个口径：
        /// 两者在九个实例上逐位相同，理由见上一句）。</summary>
        float _handlePx;
        /// <summary>滑区**右**端的让位（**世界单位** = `SlideRightInsetPx × capScale ÷ 108`）。
        /// 🔴 也是逐实例的（设置窗要 × 0.9）⇒ 不能当常量用 —— `Layout` 的行程与 `SetFromPointer`
        /// 的分母都要它（A169 起那两处写的是「画布 px 的 10」，设置窗因此少 1 画布 px，波 8 修）。
        /// ⛔ 别退回 `SlideRightInsetPx / U`（那就又变成「拿战斗那个数当全部情况」）。</summary>
        float _slideInsetU;

        // ═══════════════════════════════════════════════════════════════════════════════════════
        // 🔴 **2026-10-20（W57 · 战斗线 `T09` 步骤 A3-b）：画布档的输入改走 uGUI 事件** ——
        //   判据 → `资料/普查产出_1019/W57_战斗线_T09迁移计划.md` §2 步骤 A3-b
        //   （「`WfSlider` 双模式，需要 `IPointerDown/Drag` 走 uGUI（拖动是持续状态，不能只靠 onClick）」）。
        //
        //   · **为什么拖动不能只靠 onClick**：拖动是**持续状态** —— 按下 → 每一帧移动 → 抬起。
        //     原版 `Slider` 就是 `IPointerDownHandler / IDragHandler / IPointerUpHandler` 走 EventSystem 送达。
        //   · **为什么用 `EventTrigger` 而不是自定义 `MonoBehaviour`**：本波是**单文件白名单**
        //     （只许改 `WfSlider.cs`），而本仓有一条硬规矩 = **`MonoBehaviour` 的类名必须与文件名相同**
        //     （2026-10-13 那轮 `!u115` 根治的结论；现读全仓「一文件多 `MonoBehaviour`」= **0**）。
        //     在 `WfSlider.cs` 里再定义一个 `WfSliderPointerInput` 会破坏那条规矩。
        //     而 `UnityEngine.EventSystems.EventTrigger` 是 uGUI **自带**的组件，它实现了
        //     `IPointerDownHandler / IDragHandler / IPointerUpHandler / IPointerEnter/ExitHandler` 整族
        //     （本机源码 `…/com.unity.ugui/Runtime/UGUI/EventSystem/EventTrigger.cs:143-160`），
        //     正是「把事件转给委托」的那个口。
        //   · **挂哪儿**：挂在**滑块根**（`slider_<name>`）上。射线打到三张图里的任意一张 ⇒
        //     `ExecuteEvents.GetEventHandler<T>` 从命中件**沿父链冒泡** ⇒ 落到根上的 `EventTrigger`
        //     （所以根自己不需要 `Graphic`）。⛔ 不要在三张图上各挂一个（那就是同一件事三份）。
        //   · **命中几何照原版**：原版 `Slider` 点得到靠的是子件
        //     `Background`(MB_4355) / `Fill`(MB_4553) 的 `m_RaycastTarget = 1` + `m_RaycastPadding = 0`，
        //     以及手柄那颗 `Image`（MB_4502 等）的 `m_RaycastPadding = (−25,−25,−25,−25)`（外扩）。
        //     我们照抄（见 `SetupUguiInput`）；**手柄那一条要把「节点 rect 差」补进 padding**，
        //     因为我们的手柄 rect 是「画出来的正方」而不是原版的 `HandleFrameW × 运行期框高`。
        //   · **数值行为逐位不变**：`PointerDown / Drag` 都走 `SetFromLocal(...)`，
        //     它与 `SetFromPointer(world)` **共用同一个算式**（`SetFromPointer` 现在只是
        //     「世界 → 局部」再转调它）⇒ 值域 / 步进（`1e-4` 阈值）/ `OnChanged` 与改之前完全一致。
        //   · ⚠️ **如实记一条与原版的差异（本步【不改】）**：原版 `Slider.OnPointerDown` 按在**手柄上**时
        //     记 `m_Offset`（拖起来保持抓取偏移、值**不跳**）；本件一直就是「按下即跳到指针」
        //     （`SetFromPointer` 的既有语义，`SettingsPanel.PointerFrame` 也按那条注释在做）。
        //     本步的口径是**保持数值行为逐位不变** ⇒ 不引入抓取偏移。要改成原版那一条是**另一步**
        //     （那会改数值行为，得单独拍板）。
        //   · ⚠️ **过渡期两套路径并存是安全的**：宿主（`SettingsPanel.PointerFrame`）现在仍每帧喂
        //     `Contains` / `SetFromPointer`。两条路对**同一个指针位置**算出**同一个值** ⇒
        //     `SetFromLocal` 的 `1e-4` 阈值让第二次调用**不会重复触发 `OnChanged`**。
        //     等 `A2`/`A3` 那批把宿主的手搓口删掉，这里就是唯一入口。⛔ 本步**不改**宿主（不在白名单）。
        // ═══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>🔴 **2026-10-20（`T09` A3-b）：画布档的 uGUI 输入组件**（`EventTrigger`）。
        /// **世界档恒 `null`** —— 那一档仍由宿主（`Shell/SettingsWindow.cs`）喂 `Contains`/`SetFromPointer`。</summary>
        EventTrigger _pointerTrigger;
        /// <summary>uGUI 拖动状态（`PointerDown → PointerUp` 之间为 true；**仅画布档会变**，世界档恒 false）。</summary>
        bool _dragging;
        /// <summary>uGUI 悬停状态（`PointerEnter/Exit` 维护；**仅画布档会变** —— 世界档的悬停由宿主喂）。</summary>
        bool _pointerHover;
        /// <summary>指针换算失败那条警告的限流计数（不许静默失败，但也别每帧刷屏）。</summary>
        int _pointerConvertWarns;

        // ============================ 悬停 / 按下 / 禁用 反馈（`N70` P0-4） ============================
        // 判据 = 原版 uGUI `Slider`（`m_Transition = 1 ColorTint`）：
        //   `m_TargetGraphic` = **手柄**那颗 `Image`（文件头「Slider 字段」那一节逐字实读），
        //   `m_Colors`：NOR 白 · **HL 0.9607843** · **PR 0.7843137** · **DIS (0.7843, α 0.502)** ·
        //   `m_FadeDuration = 0.1`。⇒ 只有**手柄**变色，轨道/填条不变（UGUI 的 ColorTint 只打 target）。
        // 🔴 **2026-10-19（B4）**：内核放在本件（⛔ 不在两个宿主各写一套）。
        //   宿主只需每帧喂 `SetHover/SetPressed`（并在每帧末 `TickFeedback()`）；
        //   在 Play 里按 0.1s 逐通道 lerp（与 `WindowButton` 同形），批处理里**立刻写**（自检读目标色）。
        static readonly Color FbHighlight = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
        static readonly Color FbPressed = new Color(0.7843137f, 0.7843137f, 0.7843137f, 1f);
        static readonly Color FbDisabled = new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.502f);
        /// <summary>原版 `m_Colors.m_FadeDuration`（秒）。</summary>
        public const float FbFadeSeconds = 0.1f;

        bool _fbHover, _fbPressed, _fbInteractable = true;
        Color _fbK = Color.white, _fbTarget = Color.white;
        Color _fbBase = Color.white;      // 手柄图形自己的颜色（通常是白；别把状态色乘进基准色）

        /// <summary>悬停态（宿主每帧喂）。</summary>
        public void SetHover(bool on)
        {
            if (_fbHover == on) return;
            _fbHover = on;
            FbSetTarget();
        }
        /// <summary>按下态（拖手柄期间 = true，原版 `Selectable.Pressed`）。</summary>
        public void SetPressed(bool on)
        {
            if (_fbPressed == on) return;
            _fbPressed = on;
            FbSetTarget();
        }
        /// <summary>可交互（`false` ⇒ 原版 `DisabledColor`：0.7843 / α0.502）。默认 true、现有调用点零变化。</summary>
        public void SetInteractable(bool on)
        {
            if (_fbInteractable == on) return;
            _fbInteractable = on;
            FbSetTarget();
        }
        /// <summary>当前状态键（`Color.white` = Normal）。自检读**目标值**（批处理没有帧循环）。</summary>
        public Color FeedbackKeyForTest { get { return _fbTarget; } }
        /// <summary>当前**实际**色偏（Play 里会落后目标最多 0.1s）。</summary>
        public Color FeedbackCurrentForTest { get { return _fbK; } }

        void FbSetTarget()
        {
            _fbTarget = !_fbInteractable ? FbDisabled
                      : _fbPressed ? FbPressed
                      : _fbHover ? FbHighlight
                      : Color.white;   // m_NormalColor = 纯白（Slider 那一族，实读）
            if (!Application.isPlaying)
            {
                _fbK = _fbTarget;
                FbApply();
            }
        }

        /// <summary>每帧推一次（宿主在 `PointerFrame` 末尾调）。Play 里按 0.1s 逐通道 lerp；
        /// 批处理里 `FbSetTarget` 已经即时写过了，这里再调也是幂等。</summary>
        public void TickFeedback()
        {
            if (!Application.isPlaying || _fbK == _fbTarget) return;
            float k = Mathf.Clamp01(Time.unscaledDeltaTime / Mathf.Max(0.0001f, FbFadeSeconds));
            _fbK = Color.Lerp(_fbK, _fbTarget, k);
            if (Mathf.Abs(_fbK.r - _fbTarget.r) < 0.001f && Mathf.Abs(_fbK.g - _fbTarget.g) < 0.001f
                && Mathf.Abs(_fbK.b - _fbTarget.b) < 0.001f && Mathf.Abs(_fbK.a - _fbTarget.a) < 0.001f)
                _fbK = _fbTarget;
            FbApply();
        }

        void FbApply()
        {
            if (_handle == null) return;
            if (_fbBase == default(Color)) _fbBase = Color.white;
            // 原版 `CrossFadeColor` 是**乘**在图形自己的颜色上
            _handle.SetTint(new Color(_fbBase.r * _fbK.r, _fbBase.g * _fbK.g,
                                      _fbBase.b * _fbK.b, _fbBase.a * _fbK.a));
        }

        /// <summary>建件收尾（`Create` 末尾）：记下基准色并立刻写一次 Normal（同 `WindowButton.ApplyTintInstant`）。</summary>
        void FbInit()
        {
            _fbBase = _handle != null ? _handle.Tint : Color.white;
            _fbK = _fbTarget = Color.white;
            FbApply();
        }

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
        /// <param name="uguiHost">🔴 **2026-10-16（`T09` 块 6）：双模式的开关。**
        /// **非空 ⇒ 画布档**（三层都建成 uGUI 件、挂 `uguiHost` 下、坐标/尺寸 = **设计 px**）；
        /// **`null`（出厂）⇒ 世界档 —— 与加本档之前逐位相同**（先例 = `ErrorMessageBanner.Create(..., uguiHost)` ·
        /// `WaitBanner.Create(parent, uguiHost)` · `Tooltip.UseHost`；本件被壳侧共用 ⇒ ⛔ 不许做成全局开关）。
        /// <para>宿主 = **本件所属面板的画布容器**（战斗侧传 `SettingsPanel._panel`，那个 743.2×758.6 px 的面板帧）
        /// —— **本件不建自己的子画布、也不进 `HudCanvas.SubCanvasRank`**（判据 → 文件头那条「落点」）。</para>
        /// <para>⚠️ 这一档下 `center` 的 **x/y 是【设计 px】**（世界档是**世界单位**）、
        /// 而它的 **z 不进 `localPosition`** —— 只当 `HudCanvas.Add` 的**兄弟序档**（语义同旧：z 大 = 画在底下）。
        /// ⛔ 传了 `uguiHost` 却还按世界单位给 x/y ⇒ 三根会挤到面板中心那一小撮（偏 108 倍，**而画面不报错**）。</para></param>
        public static WfSlider Create(Transform parent, string name, Vector3 center, float value,
                                      Action<float> onChanged, int queue, float handlePx, float handleOffset,
                                      float capScale, float trackW = TrackW, float trackH = TrackH,
                                      Transform uguiHost = null)
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
            // 🔴 **2026-10-16（`T09` 块 6）**：档位在这一行定死，**下面所有单位都由它派生**（见文件头那条 `u`）。
            s._ugui = uguiHost != null;
            float u = s._ugui ? 1f : U;                // 局部单位/px：世界档 = 108、画布档 = 1（局部单位**就是**设计 px）
            if (s._ugui && capScale != 1f)
                // ⚠️ 画布档的 `capScale` 语义不成立（那一档的「设计 px → 画布 px」恒 = 1，全局缩放归 `CanvasScaler`）
                //    ⇒ 出声，别让「端帽/让位各差一点」静默过去。战斗侧那条路传的就是 1f。
                Debug.LogWarning($"[WfSlider] `{name}` 在**画布档**下拿了 capScale = {capScale}（应当传 1）"
                    + " —— 端帽与滑区右端让位会按它缩一遍（画布档的全局缩放由 `CanvasScaler` 负责）");

            s._w = trackW / u; s._h = trackH / u;
            s._handleH = handlePx / u;
            s._handlePx = handlePx;                    // A1301①：手柄那颗的原版 `rect` 高 = 实画边长（见字段的 doc）
            s._handleOffsetU = handleOffset / u;
            // 滑区右端的让位 = 原版那条 `m_SizeDelta.x = −10`（**设计 px**）× 本实例的设计→画布倍数。
            // 🔴 复用一个已有形参（`capScale`）而不是新开一个：两者本来就是**同一个倍数**
            //    （战斗 1、设置窗 0.9），多传一个只是多一处能写错的地方；
            //    ⛔ 也**不许**从 `trackH` / `handlePx` 反推（那几个数两边不同源，铁律 5·c）。
            s._slideInsetU = SlideRightInsetPx * capScale / u;
            // 两层自己的局部 z：世界档 = 原字面量；画布档 = 0（画布上的 z 会把件推离画布平面）。
            s._fillZ = s._ugui ? 0f : -0.001f;
            s._handleZ = s._ugui ? 0f : -0.002f;

            s._root = new GameObject("slider_" + name, typeof(RectTransform));
            if (s._ugui)
            {
                // ---- 🔴 2026-10-16（`T09` 块 6）：**画布档**的根 ----
                //  挂**宿主容器**（战斗侧 = `SettingsPanel._panel`）、`sizeDelta` 吃**真 px**（`HudCanvas.SetPxSize`，
                //  ⛔ 别用 `MenuDraw.SetPxSize` —— 那个内部是 ÷108，会把这一件缩成 1/108**而不报错**）。
                //  `center.z` **不进 `localPosition`**（画布上的 z 会把件推离画布平面）—— 它只当兄弟序档。
                s._root.transform.SetParent(uguiHost, false);
                HudCanvas.SetPxSize((RectTransform)s._root.transform, trackW, trackH);
                s._root.transform.localPosition = new Vector3(center.x, center.y, 0f);
                HudCanvas.Add((RectTransform)s._root.transform, center.z);
            }
            else
            {
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
            }

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
            else if (s._ugui)
            {
                // ═══════════════════════════════════════════════════════════════════════════════════
                // 🔴 **2026-10-16（`T09` 块 6）：画布档三层** —— 三张 uGUI 件（两张 `Image (Sliced)` + 一张方 `Image`）。
                //   判据 → 文件头那段。四样别改：
                //   ① **两层条图 = `Image (Sliced)`**（原版那三根 `… Slider` 的子件本来就是 `Image`，`Background`
                //      / `Fill` 在 uGUI `Slider` 的默认 prefab 里就是 Sliced）—— uGUI 里「九宫格」就是这一件事；
                //   ② **端帽 = `m_Border ÷ (pixelsPerUnit × ppuMul)`**：`184 ÷ 2 = 92` / `30 ÷ 2 = 15` 画布 px
                //      —— 与**世界档**那两处 `borderOutPx`（`BarBorderPx / PpuMul * capScale`，画布档 capScale = 1）
                //      **同一个数**。`HudCanvas.MakeSlicedPiece` 的 `ppuMul` 恒 1 ⇒ 建完把那一个字段补上
                //      （`ApplyPpuMul`；⚠️ 那是「`HudCanvas` 缺这个口」的**临时权宜**，先例 =
                //       `WaitBanner.BuildCanvas` 给 `Image (Tiled)` 补 `ppuMul`）。
                //      ⛔ **别改成「把 border 预 ÷ ppuMul」** —— 那种写法端帽**画出来**的宽对、但**切到的贴图列错**
                //      （92 vs 184 那一列），肉眼看是「端帽的花纹不对」。
                //   ③ **三层先后只能靠兄弟序**（画布上同队列、同平面）⇒ `HudCanvas.Add(rt, …)` 的 z **取负**：
                //      轨道 `−queue`（最大 z = 画在**最底下**）→ 填条 `−queue−1` → 手柄 `−queue−2`（画在最上面）。
                //      判据 = `queue` 大 = 画在上面，而 `Group.Add` 的 z 大 = 画在底下（`T09` §五·补⑩ 那条订正）。
                //   ④ **填条的宽由 `Layout` 那一支给**（`LayoutFillCanvas`：改矩形宽 = 原版 `UpdateVisuals` 的
                //      `anchorMax.x = value` 的等效物）—— 这里只建**满宽**，随后 `s.Layout()` 会摆第一次。
                // ═══════════════════════════════════════════════════════════════════════════════════
                var bgPiece = HudCanvas.MakeSlicedPiece(s._root.transform, bgTex, "slider_bg",
                                                        trackW, trackH,
                                                        new Vector4(BarBorderPx, 0f, BarBorderPx, 0f), Color.white);
                if (bgPiece == null)
                {
                    // 不许静默失败：轨道建不出来 = 一根什么都没有的滑块，而调用点会以为建成功了
                    Debug.LogWarning($"[WfSlider] `{name}` 的**轨道**在画布档下没建起来（`Volume_bar_inactive` 没取到？）");
                }
                else
                {
                    ApplyPpuMul(bgPiece, PpuMul);
                    bgPiece.Rect.localPosition = new Vector3(0f, 0f, 0f);
                    HudCanvas.Add(bgPiece.Rect, -queue);
                    s._bgRoot = bgPiece.gameObject;          // 自检量轨道几何用（`TrackWorldH`）
                    s._bgImg = bgPiece.Rect.GetComponent<Image>();
                }
                var fillPiece = HudCanvas.MakeSlicedPiece(s._root.transform, fillTex, "slider_fill",
                                                          trackW, trackH,
                                                          new Vector4(FillBorderPx, 0f, FillBorderPx, 0f), Color.white);
                if (fillPiece == null)
                {
                    // ⛔ 不许静默：填条建不出来 = 拖动时**没有任何可见反馈**（值其实一直在变），而画面上只像「少了那条」
                    Debug.LogWarning($"[WfSlider] `{name}` 的**填条**在画布档下没建起来（`Volume_bar_active` 没取到？）");
                }
                else
                {
                    ApplyPpuMul(fillPiece, PpuMul);
                    fillPiece.Rect.localPosition = new Vector3(0f, 0f, s._fillZ);
                    HudCanvas.Add(fillPiece.Rect, -queue - 1f);
                    s._fillRoot = fillPiece.gameObject;
                    s._fillImg = fillPiece.Rect.GetComponent<Image>();
                }
                // 手柄 = 一张**方** `Image`（`HudCanvas.MakeImageInParent` = 中心锚 + 中心枢轴 + `PA = 0` 拉满）。
                //  ⚠️ 世界档那支是 `ImageQuad.Create`（**按高定、宽由贴图比例来**）；这里给的是 `(handlePx, handlePx)`
                //     的**正方** —— 与那张 110×110 方图 + `preserveAspect` 取短边的**实画结果**同一个形（见 `HandlePx`）。
                //  x/y 先给 0：真正的落点由紧随的 `s.Layout()` 给（与本档填条同一处口径）。
                s._handle = HudCanvas.MakeImageInParent(s._root.transform, handleTex, "slider_handle",
                                                        0f, 0f, s._handlePx, s._handlePx, -queue - 2f, Color.white);
                if (s._handle == null)
                    Debug.LogWarning($"[WfSlider] `{name}` 的**手柄**在画布档下没建起来（`Volume_button` 没取到？）");
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
                // 🔴🔴 **2026-10-10（`A1359`）就地订正（铁律 5）：这一段的实现换了，原注释已过期。**
                //    原写的是「实现上用**整体横向缩放**而不是按目标宽重建九宫格 …… **我们挑的** ……
                //    **未逐帧比对过**」—— 那不是原版：**它把两个端帽一起缩**（值越小端帽越窄）。
                //    **原版是改锚点**：uGUI `Slider.UpdateVisuals` 把 `Fill` 的 `anchorMax` 写成 `(value, 1)`
                //    （判据抄在 `Editor/BattleScene.cs` 的 A130 那一段；本机源码
                //     `…/com.unity.ugui/Runtime/UGUI/UI/Core/Slider.cs` 的 `UpdateVisuals`：
                //     `anchorMax[(int)axis] = normalizedValue`）⇒ **矩形变窄、两端端帽照旧画
                //    `30 ÷ 2 × capScale`**（sliced 的角块是**固定像素**、中段拉伸）。
                //    现读 = `LayoutFill` 逐值摆那三块子 quad 的**位置 + 横向 `localScale`** ——
                //    它与「按目标宽重建九宫格」**像素等价**（每块都是「一块 quad + 一段固定 uv 被拉伸」，
                //    把这块 quad 横向缩到 s 倍 ≡ 把它重建窄 s 倍），又不用每次拖动销毁/重建节点。
                float fillCap = FillBorderPx / PpuMul * capScale;
                s._fillRoot = MenuDraw.Nine(s._root.transform, fillTex, TrackRectPx(trackW, trackH),
                                            new Vector4(FillBorderPx, 0f, FillBorderPx, 0f), fillTex.width, fillTex.height,
                                            queue + 1, name: "slider_fill",
                                            borderOutPx: new Vector4(fillCap, 0f, fillCap, 0f));
                if (s._fillRoot != null) s._fillRoot.transform.localPosition = new Vector3(0f, 0f, s._fillZ);
                // 三块子 quad 按 `ImageQuad.CreateNineSlice` 的命名取回（`{name}_{i}{j}`，列 0/1/2 × 行**只能**是 1
                //   —— 上下 `border` 都是 0 ⇒ 行 0 / 2 的高为 0、创建时就被 `continue` 跳过）。
                //   🔴 取不到 = 九宫格的结构变了（命名或 border 被改）⇒ **出声并退回旧路**
                //   （`_fillAnchorMode = false`，画出来仍是今天的滑块）—— ⛔ 不静默、也不画一根坏滑块。
                if (s._fillRoot != null)
                {
                    s._fillL = FindFillQuad(s._fillRoot, "slider_fill_01");
                    s._fillC = FindFillQuad(s._fillRoot, "slider_fill_11");
                    s._fillR = FindFillQuad(s._fillRoot, "slider_fill_21");
                    s._fillAnchorMode = s._fillL != null && s._fillC != null && s._fillR != null;
                    if (!s._fillAnchorMode)
                        Debug.LogWarning($"[WfSlider] `{name}` 的填条三块子 quad 没按名字取到"
                            + "（`slider_fill_01` / `_11` / `_21`）—— 填条退回【整体横向缩放】那条旧路，"
                            + "端帽会跟着值一起缩（与原版不符）。是不是改了九宫格的命名 / border？");
                    else
                    {
                        // 建件宽**从子件自己的 `WorldW` 读回来**（见字段的 doc：⛔ 别拿常量再算一遍）
                        s._fillCapU = Mathf.Max(0f, s._fillL.WorldW);
                        s._fillMidU = Mathf.Max(1e-6f, s._fillC.WorldW);
                    }
                }
                // ③ Handle：正方形，边长 = 调用方给的**实画边长**（A169 起不是常量）
                //  🔴 **2026-10-16（`T09` 块 6）**：只多套一层 `HudPiece.Of(...)` —— 字段类型从 `ImageQuad`
                //     改成 `HudPiece` 了（两档共用），**建出来的东西与三个参数一字未改**；
                //     `HudPiece.Of(null)` 也回 `null` ⇒ 空值语义与改之前相同。
                s._handle = HudPiece.Of(ImageQuad.Create(s._root.transform, handleTex,
                                             new Vector3(0f, 0f, s._handleZ),
                                             s._handleH, new Vector2(0.5f, 0.5f), "slider_handle"));
                // `ImageQuad.Create` 自己不设队列（= 材质默认档 3000）⇒ 三层里最高的那一档显式写上。
                // ⚠️ **只对世界档**（画布档的手柄是 `Image`、先后由 `HudCanvas.Add` 的兄弟序定 ⇒ 没有队列可设）。
                if (s._handle != null && s._handle.Quad != null) s._handle.Quad.SetRenderQueue(queue + 2);
            }
            s.Layout();
            s.FbInit();          // 🆕 2026-10-19（B4）：记基准色 + 立刻写一次 Normal（同 `WindowButton.ApplyTintInstant`）
            // 🆕 2026-10-20（`T09` A3-b）：**画布档**的 uGUI 输入出口 —— 三张图设为可射线件 + 根上挂 `EventTrigger`。
            //   世界档**一个字都不做**（那一档的输入仍由宿主的手搓口喂；判据见上面那整段）。
            if (s._ugui) s.SetupUguiInput();
            return s;
        }

        /// <summary>按当前 `Value` 摆 Fill 与 Handle。
        /// <para>🔴 **2026-10-16（`T09` 块 6）：这一条「交互口」的【算式一个字没改】—— 加的是【分派与换算】。**
        /// 两处的单位已经在本件内部换好了（世界档 `_w/_h/_handleOffsetU/_slideInsetU` = **世界单位**、
        /// 画布档 = **设计 px**，见 <see cref="Create"/> 里那个 `u`），而 `localPosition` / `sizeDelta`
        /// 在**两种模式下都是「本件局部单位」**（画布档下它就是设计 px）⇒ 同一个算式两边都对。
        /// **分派** = 只多一支：填条画布档走 <see cref="LayoutFillCanvas"/>（一张 `Image (Sliced)` 改矩形宽）
        /// vs 世界档走 <see cref="LayoutFill"/>（三块子 quad 改位置+`localScale`）—— 两者**语义同一**
        /// （= 原版 `UpdateVisuals` 的 `anchorMax.x = value`），⛔ 不是两条并存的口径。</para></summary>
        void Layout()
        {
            float t = Mathf.InverseLerp(Min, Max, Mathf.Clamp(Value, Min, Max));
            if (_fillRoot != null)
            {
                if (_ugui) LayoutFillCanvas(t);
                else if (_fillAnchorMode) LayoutFill(t);
                else
                {
                    // 🔴 **旧路（`A1359` 之前）**：整体横向缩放九宫格的根 —— 端帽会跟着值一起缩。
                    //    只有「那三块子 quad 没按名字找到」时才走到这儿（那一刻 `Create` 已经出声、不静默）；
                    //    留着它只为「结构变了也不会画出一根坏滑块」，⛔ 不是两条并存的口径。
                    //    ⚠️ 画布档**不走这里**（那边没有「根缩放」那回事，见上面那条分派）。
                    float w0 = Mathf.Max(0.0001f, t * _w);
                    _fillRoot.transform.localScale = new Vector3(w0 / _w, 1f, 1f);
                    _fillRoot.transform.localPosition = new Vector3(-_w * 0.5f + w0 * 0.5f, 0f, _fillZ);
                }
            }
            if (_handle != null)
            {
                // 🔴 手柄中心 = **滑区左沿**（= 轨道左端，左端不让位，判据见文件头那条更正）
                //    + 手柄自己的 `m_AnchoredPosition.x` + 值 × 滑区宽（滑区 = 轨道宽 − 右端 10 **设计 px**）。
                //    A169 前这里**连中间那一项都没有** ⇒ 整体偏左 12 设计 px。
                // ⚠️ 值 0 时手柄左缘只离轨道左端 0.8 画布 px（看着就是"贴着左端"）、值 1 时中心探出
                //    轨道右端 **2** 画布 px —— 原版就是这样，别「顺手」夹回来（夹了手柄就与原版对不上；
                //    `Contains` 那边已让出手柄探出去的那一块）。
                // 🔴 **2026-10-16（`T09` 块 6）**：z 从字面量 `-0.002f` 改成字段 `_handleZ`
                //    （世界档那个字面量原样、画布档 = 0 —— 画布上的 z 会把件推离画布平面）。**算式本体未改**。
                float x = -_w * 0.5f + _handleOffsetU + t * (_w - _slideInsetU);
                _handle.transform.localPosition = new Vector3(x, 0f, _handleZ);
            }
        }

        /// <summary>🔴 **2026-10-16（`T09` 块 6）：填条的【画布档】摆法。**
        /// <para>与 <see cref="LayoutFill"/> **语义同一、实现不同**：画布档下就是原版那件事本身 ——
        /// 一张 `Image (Sliced)` **改矩形宽**（左沿不动、右沿随值走 = `Slider.UpdateVisuals` 把 `Fill` 的
        /// `anchorMax` 写成 `(value, 1)`），端帽由切片**固定像素** ⇒ 恒宽、中段拉伸。
        /// 世界档那份「三块子 quad 改位置 + `localScale`」是它的**等效做法**（理由见那个函数）。</para>
        /// <para>⚠️ 原版那颗 `Fill` 的锚是 `anchorMin (0,0)` / `anchorMax (value,1)`；本件走的是
        /// `HudCanvas.MakeSlicedPiece` 的**中心锚**（那个口只建中心锚）⇒ 这里把「矩形」显式算出来
        /// （中心 = `轨道左 + 总宽/2`、`sizeDelta.x = 总宽`）—— **结果矩形与原版逐位相同**，
        /// 差的是「用锚点表达」还是「用 anchoredPosition 表达」（同一块矩形）。</para>
        /// <para>⚠️ 退化（`total < 两端端帽之和`）**不用在这里管** —— uGUI `Image.GetAdjustedBorders`
        /// 自己会按 `rect.width ÷ (bL+bR)` 把两端缩掉（世界档那份才要手写这一条，见 `LayoutFill`）。</para></summary>
        void LayoutFillCanvas(float t)
        {
            if (_fillRoot == null) return;
            float total = Mathf.Max(0f, t * _w);          // 本值下填条的总宽（画布档 = 设计 px）
            var rt = (RectTransform)_fillRoot.transform;
            rt.sizeDelta = new Vector2(total, _h);
            rt.localPosition = new Vector3(-_w * 0.5f + total * 0.5f, 0f, _fillZ);
        }

        /// <summary>🔴 **2026-10-10（`A1359`）**：填条按 `t`（0..1）摆位 —— **照原版 uGUI 的【锚点】语义**，
        /// ⛔ 不是把整棵九宫格横向缩放（那样值越小端帽越窄，与原版不符）。
        /// <para>**原版判据**：`Slider.UpdateVisuals` 把 `Fill`（sliced `Image`）的 `anchorMin/anchorMax`
        /// 写成 `(0,0)` / **`(value, 1)`**（本机 `…/com.unity.ugui/Runtime/UGUI/UI/Core/Slider.cs`；
        /// 同一段判据抄在 `Editor/BattleScene.cs` 的 A130 那一段）⇒ 锚在**左边**、右沿随值走，
        /// 而 sliced 的角块是**固定像素** ⇒ **两端端帽恒宽、中段拉伸**。</para>
        /// <para>**本仓的等效做法**：三块子 quad 的**横向 `localScale`**。为什么恰好等价 ——
        /// 九宫格的角块与中段都是「**一块 quad + 一段固定 uv 被拉伸**」（`ImageQuad.CreateNineSlice`
        /// 建完就 `SetUvRect`，之后由网格把它铺满整块）⇒ 「把这块 quad 横向缩到 s 倍」≡
        /// 「把它按 `s × WorldW` 重建」**像素一致**，而且不重算 uv / 网格、不销毁重建节点
        /// （⛔ 坐标与 `WorldW` 都**不动** ⇒ 既有那几条量 `WorldH` / `WorldW` 的断言读数**逐位不变**）。</para>
        /// <para>**退化照原版 `Image.GetAdjustedBorders`**（uGUI 的 sliced：**逐轴**、只在
        /// `border.x + border.z > rect.width` 时把两端按 `rect.width ÷ (bL + bR)` 缩；中段宽 ≤ 0 的格子
        /// `GenerateSlicedSprite` 直接跳过）⇒ 本函数同样：**总宽不足两端端帽之和**时两端各缩成 `总宽/2`、
        /// 中段宽归 0（`localScale.x = 0` ⇒ 零面积、画不出来。⚠️ **不是** `SetAspect(0)` ——
        /// 那个会造出**镜像 quad**，见 `Shell/MenuDraw.cs` 那条）。</para>
        /// <para>⚠️ 三块子 quad 是**按名字**取的（见 `_fillL` 的 doc）；⚠️ 本件**不裁切**
        /// （`MenuDraw.Nine` 那条 `clip` 形参没传）⇒ 这里直接摆绝对位置，不与任何裁切结果打架。</para></summary>
        void LayoutFill(float t)
        {
            float total = Mathf.Max(0f, t * _w);          // 本值下填条的**总宽**（世界单位）
            float cap = CapOf(_fillCapU, total);           // 同 `Image.GetAdjustedBorders`（只有横轴有角块）
            float capS = cap == _fillCapU ? 1f : (_fillCapU > 1e-9f ? cap / _fillCapU : 0f);
            float mid = Mathf.Max(0f, total - cap * 2f);
            float midS = mid / _fillMidU;                  // `_fillMidU` 已保证 > 0（见 `Create`）
            float left = -_w * 0.5f;                       // 左沿恒不动（原版 `Fill` 锚在左边的锚点上）
            FillPlace(_fillL, left + cap * 0.5f, capS);
            FillPlace(_fillC, left + cap + mid * 0.5f, midS);
            FillPlace(_fillR, left + total - cap * 0.5f, capS);
        }

        /// <summary>把一块填条子 quad 摆到 `x`（本件局部世界单位）、横向缩到 `sx` 倍。
        /// ⚠️ **纵轴 / z 原样保留**（三块同高、z 都是建件时那个 0；本件三层靠 `queue` 排，不靠 z）。
        /// ⚠️ `sx == 0` 是**合法输入**（值 0、或总宽小于两端端帽）⇒ 零面积、画不出来，⛔ 别去 `SetActive(false)`
        /// —— 那样这块的几何会**停在上一帧**（`FillWorldW` 之类的读者就会量到过期的那一块）。</summary>
        static void FillPlace(ImageQuad q, float x, float sx)
        {
            if (q == null) return;
            var p = q.transform.localPosition;
            q.transform.localPosition = new Vector3(x, p.y, p.z);
            var s = q.transform.localScale;
            q.transform.localScale = new Vector3(sx, s.y, s.z);
        }

        /// <summary>按名字取填条那一块子 quad（`ImageQuad.CreateNineSlice` 建的是「根 + 每格一颗」；
        /// ⚠️ `transform.Find` **只找直接子件** —— 那几颗正是直接子件，这里对得上）。</summary>
        static ImageQuad FindFillQuad(GameObject root, string childName)
        {
            if (root == null) return null;
            var t = root.transform.Find(childName);
            return t != null ? t.GetComponent<ImageQuad>() : null;
        }

        /// <summary>🔴 **2026-10-16（`T09` 块 6）：填条端帽那一格的退化规则 —— 全仓只此一份。**
        /// 判据 = uGUI `Image.GetAdjustedBorders`：**总宽不足两端端帽之和**时，两端各缩成 `总宽/2`
        /// （两只一起，所以是 `total * 0.5f` 而不是各缩一半）。
        /// <para>两个用户（**同一条规则、两种量纲**）：<see cref="LayoutFill"/>（世界档，单位 = 世界单位）
        /// 与 <see cref="FillCapWorldW"/> 的画布档那一支（读口，单位 = 设计 px ⇒ 调用点自己再除以 `U`）；
        /// **画布档的渲染不用它** —— 那边是 uGUI `Image (Sliced)` 自己按这条规则画（`LayoutFillCanvas` 的 doc）。</para></summary>
        static float CapOf(float nominalCap, float total)
        {
            return nominalCap * 2f > total ? total * 0.5f : nominalCap;
        }

        /// <summary>🔴 **2026-10-16（`T09` 块 6）：把画布档两张条图的 `m_PixelsPerUnitMultiplier` 补上。**
        /// <para>`HudCanvas.MakeSlicedPiece` 建出来的 `Image` 这个字段恒 = **1**，而原版那两张图的端帽是
        /// `m_Border ÷ (pixelsPerUnit × ppuMul)` = `184 ÷ 2 = **92**` / `30 ÷ 2 = **15**` 画布 px
        /// ⇒ 不补就**画成两倍宽**（中段短一半，世界档 `A169` 当年修的就是同一条）。</para>
        /// <para>⚠️ 这是「`HudCanvas` 缺一个 `ppuMul` 形参」的**临时权宜**（先例 = `WaitBanner.BuildCanvas`
        /// 给 `Image (Tiled)` 补 `ppuMul`，同一族的账已记在 `T09` §七·14）—— 收集齐之后收编进那个口。</para>
        /// <para>⛔ **别改成「把 border 预 ÷ ppuMul 再建」**：那种写法端帽**画出来**的宽对、但**切到的贴图列错**
        /// （92 vs 184 那一列）⇒ 肉眼看是「端帽的花纹不对」，而且它只在 uGUI 那一档出现。</para></summary>
        static void ApplyPpuMul(HudPiece p, float ppuMul)
        {
            if (p == null || p.Rect == null) return;
            var im = p.Rect.GetComponent<Image>();
            if (im == null)
            {
                Debug.LogError($"[WfSlider] 画布档的九宫格件 `{p.name}` 上取不到 `Image` ⇒ "
                             + $"端帽按 1 倍画（原版 = `m_Border ÷ {ppuMul}`）—— 出声，不静默");
                return;
            }
            im.pixelsPerUnitMultiplier = ppuMul;
        }

        // 🔴 **N41（2026-10-17）：这里原来有个 `HitBand(…)`（「轨道 ∪ 手柄」写成**一块矩形**的近似），
        //    已删 —— 全仓 0 调用点（战斗侧 `Contains` 在 A1301① 起、外壳侧 FPS 滑块在 A1326 起，
        //    都改走 `InUnion` / `HandleHitRect` 的**真并集**）。
        //    ⚠️ 历史结论别丢（它记录的是「为什么不能用单矩形」）：两块**不是同一个纵向带**
        //    （轨道 13 · 手柄 35.406 设计 px）⇒ 取 `max(半轨道, 半手柄)` 会**多认**轨道上下那两圈、
        //    同时**漏认**手柄按 `m_RaycastPadding = −25` 外扩出来的那一圈 —— 两个方向都错。
        //    判据与算式见 `InUnion` 的 doc，⛔ 别再把这个近似写回来。

        /// <summary>🆕 **2026-10-17（A1326）**：手柄那颗 `Image` 的**原版命中框** —— 入参 = 它**未外扩**的
        /// 布局框（= 原版 `m_SizeDelta` 那一块：`HandleFrameW × 运行期框高`），返回**四边按 `m_RaycastPadding`
        /// 收/放之后**的框（`HandlePadPx = −25` ⇒ 每边外扩 25）。
        /// <para>🔴 **算式只此一份**：转发 `MenuDraw.PaddedRect`（符号口径「正 = 缩小、负 = 扩大」与退化守卫都在那边，
        /// 本件一个字都不写 `±pad`）。战斗侧 <see cref="HitHandlePadded"/> 与外壳侧 FPS 滑块
        /// （`Shell/SettingsWindow.cs`）用的是**同一条**。</para>
        /// <para>⚠️ 坐标帧随调用方（本件世界单位 / 外壳设计 px 都行）—— 与 <see cref="InUnion"/> 同一条规矩。</para></summary>
        public static PxRect HandleHitRect(PxRect frame)
        {
            return MenuDraw.PaddedRect(frame,
                new Vector4(HandlePadPx, HandlePadPx, HandlePadPx, HandlePadPx));
        }

        /// <summary>🆕 **2026-10-17（A1326）**：**真并集**判据 —— 点落在「轨道」或「手柄命中框」**任一块**里。
        /// ⛔ **不是它们的外接矩形**：两块纵向带不同（轨道 13 设计 px、手柄外扩后 85.406）⇒ 外接矩形会把
        /// 轨道上下各多认 ~36 px（原版那两处都没有可射线件）。单位随调用方。</summary>
        public static bool InUnion(PxRect track, PxRect handleHit, float x, float y)
        {
            if (x >= track.x1 && x <= track.x2 && y >= track.y1 && y <= track.y2) return true;
            return x >= handleHit.x1 && x <= handleHit.x2 && y >= handleHit.y1 && y <= handleHit.y2;
        }

        /// <summary>世界坐标是不是落在这根滑块的**轨道**或**手柄**上。
        /// <para>🔴 **2026-10-19（A1301①）就地订正**：上一版这里写着「点轨道也算命中是**我们挑的**」——
        /// **不成立**，那就是原版的做法。判据（本机 uGUI 源码
        /// `…/com.unity.ugui/Runtime/UGUI/UI/Core/Slider.cs` 的 `OnPointerDown`）：
        /// 指针**不在** `m_HandleRect` 里时它走 `else` 分支 `UpdateDrag(...)`（注释原文
        /// `// Outside the slider handle - jump to this point instead`）⇒ **点轨道 = 值跳到那里**；
        /// 而事件能送到 `Slider` 上，靠的是子件里那两颗可射线件 ——
        /// `Background`（`MonoBehaviour_4355`）与 `Fill`（`…_4553`）**`m_RaycastTarget` 都是 1、
        /// `m_RaycastPadding` 都是 0**（`bundle_scenes_scenes_battlearena1` 逐颗实读）。
        /// ⛔ 那本第 31 行那条「`m_TargetGraphic` = 手柄 ⇒ 点轨道不改值」是**读错了对象**
        /// （`targetGraphic` 只服务过渡美术，不管输入）。</para>
        /// <para>🔴 **手柄那一块也要算命中**（A169）：手柄中心带上 `m_AnchoredPosition.x` 后在两端会
        /// **探出轨道**（值 0 时左缘贴轨道左端、值 1 时中心探出右端 2 画布 px —— 判据见文件头那条更正）。
        /// 🔴 **2026-10-19（A1301①）**：手柄那一块**换成原版那颗 `Image` 的命中框** ——
        /// 它的 `m_RaycastPadding` 是 `(−25,−25,−25,−25)`（**外扩**，见 `HandlePadPx`）⇒ 比画出来的
        /// 手柄大一圈。原来拿「画出来的那个正方」当命中框 **少了那一圈**
        /// （旧口径的量：纵向半高 17.203 → 现在 42.203、横向 17.203 → 48.406）。</para></summary>
        public bool Contains(Vector3 world)
        {
            if (_root == null) return false;
            // 🔴 **2026-10-16（`T09` 块 6）**：这一句是**两种模式共用的**，而且**它自己不需要分档** ——
            //    `InverseTransformPoint` 交出来的是**本件的局部单位**：世界档 = 世界单位、画布档 = 设计 px
            //    ⇒ 与 `_w/_h`（在 `Create` 里按同一把尺折好）**同域**。本函数的**唯一**模式差异在
            //    <see cref="HitHandlePadded"/> 那个 `ppu`（世界档 108 / 画布档 1）。
            var l = _root.transform.InverseTransformPoint(world);
            // ① 轨道那一块 = 本实例的 `_w × _h`（原版 `Background` 561.08×12 —— 调用方给的
            //    `trackW/trackH` 就是它，见 `Create`）。两颗（`Background` / `Fill`）的
            //    `m_RaycastPadding` 都是 0 ⇒ 不做任何收放（⛔ 不是「顺手也过一遍 pad」）。
            if (Mathf.Abs(l.y) <= _h * 0.5f && l.x >= -_w * 0.5f && l.x <= _w * 0.5f) return true;
            // ② 手柄那一块（原版那颗的 `m_RaycastPadding` = `HandlePadPx`）
            float handleCx = _handle != null ? _handle.transform.localPosition.x : 0f;
            return HitHandlePadded(l.x, l.y, handleCx);
        }

        /// <summary>🆕 **2026-10-19（A1301①）**：本件局部点（`l.x/l.y`，**本件的局部单位**）是不是落在
        /// **手柄那颗 `Image` 的原版命中框**里（`m_SizeDelta` 46.811 × `_handlePx`，四边按 `HandlePadPx` 外扩）。
        /// <para>🔴 算式**只此一份** —— 转发 `MenuDraw.PaddedHitRect`（它自己再转发 `PaddedRect`；
        /// **符号口径与退化守卫都在那一边**）。⛔ 本件一个字都不写 `±pad`（`CLAUDE.md` §三）。
        /// 函数名是 `Hit…Padded`、和 `BattleDriver.HitPaddedRect` 同一个形状（那一颗是先例）。</para>
        /// <para>⚠️ 坐标系要跳一下：本件局部 **y 向上**，而 `PxRect` 是**左上原点、y 向下**。</para>
        /// <para>🔴 **2026-10-16（`T09` 块 6）：`ppu` 按模式换**（本函数是三条交互口里**唯一**需要换算式的一处）——
        /// 世界档 = `U`(108)「px / 世界单位」、**画布档 = 1**（那一档的局部单位**就是** px，见文件头那条 `u`）。
        /// 判据/先例 = `BattleDriver.HitPaddedRect` 的 `q.IsUgui ? 1f : 1f / Px(1f)` 与
        /// `CameraResetButtonHit` 的按模式阈值换乘（同一批翻件时踩出来的：**不换就是命中框大 108 倍、
        /// 而画面完全正常**）。⚠️ 上面那句「同批三处只有本件符号反了」的更正仍然成立、与本次无关。</para></summary>
        bool HitHandlePadded(float lx, float ly, float handleCx)
        {
            if (_handle == null) return false;
            float ppu = _ugui ? 1f : U;               // 画布档：局部单位 = px ⇒ ppu = 1；世界档 = 108（px / 世界单位）
            // 🔴 **2026-10-11 就地订正（`A1301①` · 铁律 5）**：本行原写 `1f / U` —— 而 `U = 108f` 本来就是
            //   **px / 世界单位**（`:173`）⇒ `1f / U` 得 **0.00926**、**比正确值小 108² 倍**。
            //   后果：`px = lx * ppu` 把**点**缩小 108 倍、而 `PxRect` 那两半是**照 px 宽写死的**
            //   ⇒ 两个域不匹配 ⇒ 命中框退化成「面板里任何一点都中」⇒ 连带 `PointerFrame` 恒 true，
            //   把「谁被拖/能不能松手」整条链带红（`BattleScene` 8 条）。
            //   ⚠️ **对照**：`SettingsPanel.CloseIconHit` 写的是 `1f / U(1f)` —— 那边 `U(px) = px/108`
            //   ⇒ 得 **108**、**那是对的** ⇒ 这就是「同批三处只有 `WfSlider` 这一处符号反了」的原因。
            //   ⚠️ 别再写成 `1f / U`：`U` 不是「世界单位/px」那个方向。
            var hit = MenuDraw.PaddedHitRect(
                new PxRect(handleCx * ppu - HandleFrameW * 0.5f, -_handlePx * 0.5f,
                           handleCx * ppu + HandleFrameW * 0.5f,  _handlePx * 0.5f),
                new Vector4(HandlePadPx, HandlePadPx, HandlePadPx, HandlePadPx));
            float px = lx * ppu, py = -ly * ppu;      // 本件局部 px（左上原点、y 向下，与 `PxRect` 同帧）
            return px >= hit.x1 && px <= hit.x2 && py >= hit.y1 && py <= hit.y2;
        }

        /// <summary>🆕 **2026-10-19（A1301①）自检用**：手柄那颗 `Image` 的**原版命中框**尺寸（**画布 px**）
        /// = `m_SizeDelta`(46.811 × `handlePx`) 四边按 `HandlePadPx` 收/放 —— 战斗那三根 = **96.811 × 84.406**
        /// （`handlePx` = 12 + 22.406 = 34.406）。
        /// <para>⛔ **别写死 34.406**：设置窗那根是 31.87（逐实例，`CLAUDE.md` 铁律 5·c）。</para>
        /// <para>算式不在这里 —— 转发 `MenuDraw.PaddedRect`（唯一一份）。</para></summary>
        public static Vector2 HandleHitPx(float handlePx)
        {
            var r = MenuDraw.PaddedRect(new PxRect(0f, 0f, HandleFrameW, handlePx),
                                        new Vector4(HandlePadPx, HandlePadPx, HandlePadPx, HandlePadPx));
            return new Vector2(r.W, r.H);
        }

        /// <summary>按指针的世界坐标取值。返回**值变了没有**。
        /// 🔴 **逐句照原版 `Slider.UpdateDrag`**（本机 `…/com.unity.ugui/Runtime/UGUI/UI/Core/Slider.cs` 的 `Slider.UpdateDrag`）：
        /// `clickRect = m_HandleContainerRect`（= **`Handle Slide Area`**，⛔ 不是轨道）、
        /// `localCursor -= clickRect.rect.position;`、`val = Clamp01(localCursor[axis] / clickRect.rect.size[axis])`
        /// ⇒ **值的 0 在滑区左沿**（= 轨道左端 —— 左端不让位）、**1 在滑区右沿**（= 轨道右端 − 10 设计 px）、
        /// **分母 = 滑区宽**（战斗 551.08；设置窗 674.195 × 0.9 = **606.7755** 画布 px）。
        /// ⚠️ A169 起这里写的是「轨道宽 − `TravelRightPx`(**画布** px)」⇒ 设置窗那条路少了 1 画布 px；
        /// 波 8 改成 `_slideInsetU`（逐实例）后两边都对。⛔ 别改回「拿整根轨道当分母」，也⛔ 别把起点挪成 +5。
        /// <para>🔴 **2026-10-16（`T09` 块 6）：算式一个字没改、也不需要改** —— 它只用 `l`（本件局部坐标，
        /// 与 `_w`/`_slideInsetU` **同域**：世界档 = 世界单位、画布档 = 设计 px）。换算全在 `Create` 那个 `u` 上，
        /// 判据与 `Contains` 同一份（那两条 doc 都写了「换算在哪」）。</para></summary>
        public bool SetFromPointer(Vector3 world)
        {
            if (_root == null) return false;
            var l = _root.transform.InverseTransformPoint(world);
            // 🔴 **2026-10-20（`T09` A3-b）**：算式搬进 `SetFromLocal`（**唯一一份**）——
            //    本句只负责「世界 → 本件局部」，转调之后**逐位不变**（`l.x` 同一个数、同一个 `t` 算式）。
            //    uGUI 那条路（`ApplyUguiPointer`）也是转调它 ⇒ 两条输入口不可能算出不同的值。
            return SetFromLocal(new Vector2(l.x, l.y));
        }

        /// <summary>🆕 **2026-10-20（`T09` A3-b）**：按**本件局部坐标**取值（世界档 = 世界单位、
        /// 画布档 = 设计 px —— 与 `_w/_slideInsetU` 同域，见 <see cref="Create"/> 那个 `u`）。
        /// 返回**值变了没有**。
        /// <para>🔴 **算式是唯一一份**：<see cref="SetFromPointer"/>（宿主手搓口）与
        /// <see cref="ApplyUguiPointer"/>（uGUI 事件口）都调它 —— ⛔ 别在任何一边再写一份
        /// （两处写同一条规则 = 迟早不一致，`CLAUDE.md` §三）。</para>
        /// <para>判据 = 原版 `Slider.UpdateDrag`（逐句见 `SetFromPointer` 的 doc）：
        /// 值的 0 在滑区左沿、1 在滑区右沿，分母 = 滑区宽。</para></summary>
        bool SetFromLocal(Vector2 l)
        {
            float t = Mathf.Clamp01((l.x + _w * 0.5f) / (_w - _slideInsetU));
            float v = Mathf.Lerp(Min, Max, t);
            if (Mathf.Abs(v - Value) < 1e-4f) return false;
            Value = v;
            Layout();
            if (OnChanged != null) OnChanged(Value);
            return true;
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════
        // 🔴 **2026-10-20（`T09` A3-b）：画布档的 uGUI 输入**（判据/为什么这么写 → 文件头 `_pointerTrigger`
        //   上面那一整段）。本段**只在 `_ugui` 档生效**；世界档的输入仍由宿主喂，一个字都没动。
        // ═══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>画布档建完收尾：① 三张图 = **可射线件**（原版 `m_RaycastTarget = 1`；我们的建件口默认 false）；
        /// ② 根上挂 `EventTrigger`，把 `PointerDown/Drag/PointerUp/PointerEnter/PointerExit` 接到本件。
        /// <para>幂等（已有 `EventTrigger` 就复用、表清空重建）—— `Create` 每根只调一次，但重入也不会叠两份委托。</para>
        /// <para>⛔ 世界档调用它是**空操作**（`_ugui == false` 直接返回）。</para></summary>
        void SetupUguiInput()
        {
            if (!_ugui || _root == null) return;

            // ① 轨道 / 填条：**原版 `Background`(MB_4355) / `Fill`(MB_4553) 的 `m_RaycastTarget = 1`、padding 0**
            //    （`bundle_scenes_scenes_battlearena1` 逐颗实读，判据 → `Contains` 的 doc）。
            //    ⚠️ 图没建起来的那一档 `Create` 已经出声（不在这里重复刷同一条），这里只跳过着色。
            if (_bgImg != null) { _bgImg.raycastTarget = true; _bgImg.raycastPadding = Vector4.zero; }
            if (_fillImg != null) { _fillImg.raycastTarget = true; _fillImg.raycastPadding = Vector4.zero; }

            // ② 手柄：原版那颗 `Image` 的 `m_RaycastPadding = (−25,−25,−25,−25)`（外扩，判据 → `HandlePadPx`）。
            //    🔴 **不能照抄四个 −25**：原版命中框的基准是**节点 rect** `HandleFrameW(46.811) × 运行期框高`，
            //    而我们这颗节点的 rect 是**画出来的正方** `_handlePx × _handlePx`（见 `HandlePx` 那条）
            //    ⇒ 横向差的 `(HandleFrameW − _handlePx)/2` 必须补进 padding，才能得到**同一块**命中框
            //    `96.811 × 84.406`（= `Contains` 的 `HitHandlePadded` 那一块，逐位相同）。
            //    推导：目标半宽 = `HandleFrameW/2 − HandlePadPx`；我们的半宽 = `_handlePx/2`
            //    ⇒ 每边外扩 = `(HandleFrameW − _handlePx)/2 − HandlePadPx`（本例 = 6.2025 + 25 = **31.2025**）。
            //    纵向我们的框高本来就 = 原版运行期框高 ⇒ 直接就是 `HandlePadPx`（本例 −25）。
            //    ⛔ 别把这条「横向补偿」删掉当成「两个值一样」—— 少补就是每边少 6.2 px，而画面完全正常。
            var handleImg = _handle != null && _handle.Rect != null ? _handle.Rect.GetComponent<Image>() : null;
            if (handleImg != null)
            {
                handleImg.raycastTarget = true;
                float padX = HandlePadPx + (_handlePx - HandleFrameW) * 0.5f;
                handleImg.raycastPadding = new Vector4(padX, HandlePadPx, padX, HandlePadPx);
            }

            // ③ 根上的事件入口。判据/为什么不是自定义 MonoBehaviour → 文件头 `_pointerTrigger` 那一段。
            var trig = _root.GetComponent<EventTrigger>();
            if (trig == null) trig = _root.AddComponent<EventTrigger>();
            if (trig == null)
            {
                // 不许静默失败：加不上 = 画布档**永远收不到按下/拖动**（而画面完全正常）
                Debug.LogError($"[WfSlider] `{SliderName}` 的 `EventTrigger` 加不上 ⇒ 画布档的拖动/点击**收不到**"
                             + "（uGUI 输入层没接上；世界档不受影响）");
                return;
            }
            trig.triggers.Clear();
            AddPointerEntry(trig, EventTriggerType.PointerDown, OnUguiPointerDown);
            AddPointerEntry(trig, EventTriggerType.Drag, OnUguiDrag);
            AddPointerEntry(trig, EventTriggerType.PointerUp, OnUguiPointerUp);
            AddPointerEntry(trig, EventTriggerType.PointerEnter, OnUguiPointerEnter);
            AddPointerEntry(trig, EventTriggerType.PointerExit, OnUguiPointerExit);
            _pointerTrigger = trig;
        }

        /// <summary>往 `EventTrigger` 上挂一条 —— 只认 `PointerEventData`（这五类事件本来就是它）。</summary>
        static void AddPointerEntry(EventTrigger trig, EventTriggerType type, Action<PointerEventData> handler)
        {
            var e = new EventTrigger.Entry { eventID = type };
            e.callback.AddListener(d =>
            {
                var p = d as PointerEventData;
                if (p != null) handler(p);
            });
            trig.triggers.Add(e);
        }

        /// <summary>画布档：按下。**照原版 `Slider.OnPointerDown` 的两条闸**（`MayDrag`：可交互 + 只认左键），
        /// 然后 **按下即定位**（值跳到指针 = `SetFromPointer` 的既有语义，见文件头那条差异说明）。</summary>
        void OnUguiPointerDown(PointerEventData e)
        {
            if (e == null || !_fbInteractable) return;
            if (e.button != PointerEventData.InputButton.Left) return;
            _dragging = true;
            SetPressed(true);
            ApplyUguiPointer(e);
        }

        /// <summary>画布档：拖动（**持续状态**）。只在 `PointerDown` 之后、`PointerUp` 之前生效
        /// —— 与 EventSystem 的 `pointerDrag` 语义一致（指针离开矩形后仍继续送）。</summary>
        void OnUguiDrag(PointerEventData e)
        {
            if (e == null || !_dragging || !_fbInteractable) return;
            if (e.button != PointerEventData.InputButton.Left) return;
            ApplyUguiPointer(e);
        }

        /// <summary>画布档：抬起 ⇒ 退出拖动/按下态。**不再改值**（照原版 `Selectable.OnPointerUp` ——
        /// 原版 `Slider` 没有自己的 `OnPointerUp`，抬起那一帧不重算值）。</summary>
        void OnUguiPointerUp(PointerEventData e)
        {
            if (e == null || !_dragging) return;
            if (e.button != PointerEventData.InputButton.Left) return;
            _dragging = false;
            SetPressed(false);
        }

        /// <summary>画布档：悬停进/出 ⇒ 喂本件既有的反馈口（`SetHover`，原版 `Selectable` 的
        /// `Highlighted`）。⛔ 别在这里直接改色 —— 状态优先级（禁用 &gt; 按下 &gt; 悬停）只在 `FbSetTarget` 一份。</summary>
        void OnUguiPointerEnter(PointerEventData e) { _pointerHover = true; SetHover(true); }
        void OnUguiPointerExit(PointerEventData e) { _pointerHover = false; SetHover(false); }

        /// <summary>把 uGUI 事件的屏幕点换来本件局部坐标，再走 <see cref="SetFromLocal"/>。
        /// <para>🔴 换算**只走** `RectTransformUtility.ScreenPointToLocalPointInRectangle`（`T09` A3-b 的硬要求）
        /// —— ⛔ 不许混用 `LayoutSpace.ToPixel`：那是「世界 px ↔ 世界单位」的旧尺，画布档的局部单位是**设计 px**。
        /// 相机取 `eventData.pressEventCamera`（= 按下那次射线的模块相机；`ScreenSpaceCamera` 画布 = 该相机、
        /// Overlay = `null`，判据 → 本机 `PointerEventData.cs:315-323` + `Slider.UpdateDrag` 同一句）。</para>
        /// <para>⚠️ 与 `Slider.UpdateDrag` 的**一处已知差异（如实记）**：原版先用
        /// `MultipleDisplayUtilities.GetRelativeMousePositionForDrag` 处理多显示器相对坐标；我们直接吃
        /// `eventData.position`（单显示器下**逐位同值**）。多显示器不在本项目的验收范围（`资料/真Play待验清单.md`）。</para></summary>
        void ApplyUguiPointer(PointerEventData e)
        {
            if (e == null || _root == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)_root.transform, e.position, e.pressEventCamera, out local))
            {
                // 不许静默失败，但限流（拖动时可能每帧都失败）
                if (_pointerConvertWarns++ < 3)
                    Debug.LogWarning($"[WfSlider] `{SliderName}` 的 uGUI 指针换算失败"
                        + $"（screen={e.position}, pressEventCamera={(e.pressEventCamera == null ? "null" : e.pressEventCamera.name)}）"
                        + " —— 这次按下/拖动**没改值**（⛔ 不猜一个值顶上）");
                return;
            }
            SetFromLocal(local);
        }

        public void SetValue(float v, bool fire)
        {
            v = Mathf.Clamp(v, Min, Max);
            bool changed = Mathf.Abs(v - Value) > 1e-4f;
            Value = v;
            Layout();
            if (fire && changed && OnChanged != null) OnChanged(Value);
        }

        public void SetVisible(bool on)
        {
            if (_root != null) _root.SetActive(on);
            // 关着的时候不该留着悬停/按下（否则再打开会先亮一帧旧状态）
            // 🔴 2026-10-20（`T09` A3-b）：uGUI 那两个状态也要一并清 —— 窗在拖动中途被关掉时
            //    `PointerUp` 可能根本到不了（件已失活 ⇒ 不派事件）⇒ 不清就会「下次打开仍是拖动中」。
            if (!on) { _dragging = false; _pointerHover = false; SetHover(false); SetPressed(false); }
        }

        // ---- 自检用 ----
        /// <summary>三张图都取到了没有（缺图 = 玩家什么都看不见）。
        /// <para>🔴 **2026-10-16（`T09` 块 6）**：画布档走一条**等价**的分支（那边没有「子节点数」这回事，
        /// 判据换成「那张 `Image` 上有 `sprite`」）。
        /// ⚠️ **如实记一处既有缺口（两档都有）**：本口**只查了填条与手柄**，
        /// **没有查轨道那一张**（`_bgRoot`）—— 世界档那句 `childCount > 0` 数的是**填条**的子块。
        /// ⇒ 断言文案写的「三张图」实际上只覆盖两张（报告里已记，⛔ 别照它当「三张都查过了」）。</para></summary>
        public bool HasArt
        {
            get
            {
                if (_ugui)
                    return _fillImg != null && _fillImg.sprite != null
                        && _handle != null && _handle.Texture != null;
                bool bg = _fillRoot != null && _fillRoot.transform.childCount > 0;
                return bg && _handle != null && _handle.Texture != null;
            }
        }
        /// <summary>轨道**渲出来的世界高**（自检用）。🔴 量的是**几何**，不是把 `trackH` 参数念一遍 ——
        /// 取 Background 那几块 quad 的**并集高**（九宫格上下 border 都是 0 ⇒ 三块同高）。
        /// 「常量改了、`TrackRectPx` 忘了跟着 `trackH` 走」那种改法，这里会**当场露出来**。
        /// ⚠️ 单位是**世界单位**（×108 = 画布 px）；⛔ **不含手柄**（手柄比轨道高，混进来就量错）。
        /// <para>🔴 **2026-10-16（`T09` 块 6）**：画布档读**那张 `Image (Sliced)` 的 `sizeDelta.y`（真 px）
        /// ÷ `U`** —— 交出去的仍是**世界单位**（本口的契约不变 ⇒ 调用点那句 `× 108f` 两档同一个算式）。
        /// ⛔ **别改成「读世界尺寸再折」**：`T09` §七·8（画布→世界的两轴系数不同、还随屏变）。</para></summary>
        public float TrackWorldH
        {
            get
            {
                if (_ugui)
                {
                    var b = _bgImg;
                    return b != null ? b.rectTransform.sizeDelta.y / U : 0f;
                }
                if (_bgRoot == null) return 0f;
                var qs = _bgRoot.GetComponentsInChildren<ImageQuad>(true);
                float h = 0f;
                for (int i = 0; i < qs.Length; i++)
                    if (qs[i] != null) h = Mathf.Max(h, qs[i].WorldH);
                return h;
            }
        }

        /// <summary>🆕 **2026-10-10（`A1359` / `A1351`②）：填条【画出来】的那块矩形**（**本件局部帧**、
        /// **世界单位**，×108 = 画布 px）—— 与 `TrackWorldH` 同一形状的「自检用」读口。
        /// <para>🔴 **为什么必须有这个读口**：`A1359` 起填条的横向变化打在**三块子 quad 自己的
        /// `localScale.x`** 上（见 `LayoutFill`）⇒ 子件的 `WorldW` 是**建件时的整宽**、
        /// **不含**它自己的缩放 ⇒ **对填条那棵子树取并集（`QuadRectPx` / 那个口径的任何并集）
        /// 会得到【整根轨道】那么宽**，与屏幕上那块不是一回事（同 `Shell/MenuDraw.QuadRectPx` 那条
        /// ⚠️ **2026-10-10（第十四会话）量准 —— 别把「整根轨道」当期望值**：上面那个并集**在 `v < 1` 时是 `531.08`**（= 轨道 `561.08` 两支各去掉端帽 `15`），**只有 `v = 1` 时才等于整根轨道** —— 因为中段那块会被挪到「本值总宽」的中心、而并集是按各块**自己的位置**算的。⇒ **写期望值时按 `v = 1` / `v < 1` 分开写**。旁证：`Editor/SettingsScene.cs:3459-3460` 自己算过「中段建件宽 = `615.77 − 2×13.5 = 588.77`」。出处 → `资料/普查产出_第十四会话/W_断言_战斗与探针.md`。
        /// 「节点自带 `localScale` 时恒等式不成立，由调用点自己乘回」的规范句）。
        /// 本读口就是「调用点自己乘回」的那一份 —— ⛔ 别在别处再写一份。
        /// 🔴 **量的是几何、不是把输入念一遍**：它把填条那棵子树里**每一块 `ImageQuad`** 的
        /// **有效宽**（`WorldW × |子件自己的 localScale.x|`）连同**根那一级**的 `localScale`/`localPosition`
        /// 一起折进滑块根的局部帧再取并集 ⇒ 「哪一块没跟着值摆」「缩放又被打回根上」这类改法
        /// **都会当场露出来**（⛔ 不是 `Value × 轨道宽` 的回读 —— 那种读口改坏 `LayoutFill` 也不会红）。
        /// ✅ **对两种实现都成立**（`A1359` 的锚点式 · 那条旧的整体缩放路）⇒ 哪一天结构又变了，
        /// 这个读口照样报屏幕上那块。</para>
        /// <para>⚠️ 值 0 时**宽 = 0、左沿仍 = `−轨道宽/2`**（零宽的那一块位置照写，不 `SetActive(false)`）。</para>
        /// <returns>`false` = 一块都量不到（填条没建出来）⇒ 两个 `out` 归 0。</returns></summary>
        bool FillSpanW(out float leftX, out float rightX)
        {
            leftX = rightX = 0f;
            if (_fillRoot == null) return false;
            if (_ugui)
            {
                // 🔴 **2026-10-16（`T09` 块 6）：画布档那一支** —— 填条是**一张** `Image (Sliced)`
                //    ⇒ 它画出来的矩形就是 `sizeDelta` + `localPosition`（中心锚、中心枢轴，父件 = 滑块根
                //    ⇒ `localPosition.x` **就是**「相对轨道中心 px」）。
                //    ⚠️ **除以 `U` 是为了保住本口原有的「世界单位」契约**（不是「拿世界尺寸折回」——
                //    这里读的就是 `sizeDelta` 那份**真 px**，见 `TrackWorldH` 的 doc）。
                var im = _fillImg;
                if (im == null) return false;
                var rt2 = im.rectTransform;
                float hw2 = rt2.sizeDelta.x * Mathf.Abs(rt2.localScale.x) * 0.5f;
                float cx2 = rt2.localPosition.x;
                leftX = (cx2 - hw2) / U; rightX = (cx2 + hw2) / U;
                return true;
            }
            // 根那一级（`A1359` 之后恒 = 恒等变换 —— 留着它，是为了「谁又把它缩回去」也量得出来）
            float rs = _fillRoot.transform.localScale.x;
            float rp = _fillRoot.transform.localPosition.x;
            var qs = _fillRoot.GetComponentsInChildren<ImageQuad>(true);
            bool any = false;
            for (int i = 0; i < qs.Length; i++)
            {
                var q = qs[i];
                if (q == null) continue;
                float hw = q.WorldW * Mathf.Abs(q.transform.localScale.x) * Mathf.Abs(rs) * 0.5f;
                float cx = rp + rs * q.transform.localPosition.x;
                if (!any) { leftX = cx - hw; rightX = cx + hw; any = true; continue; }
                if (cx - hw < leftX) leftX = cx - hw;
                if (cx + hw > rightX) rightX = cx + hw;
            }
            if (!any) { leftX = rightX = 0f; return false; }
            return true;
        }

        /// <summary>填条**画出来**的宽（**世界单位**，×108 = 画布 px）—— 自检用。见 <see cref="FillSpanW"/>。
        /// 🔴 **期望值要写原版算式 `值 × 轨道宽`**（⛔ 不是引用本件别的常量）。
        /// ⚠️ **这一条单独分不出「改锚点」与「整体横向缩放」** —— 两种做法下整条填条的宽都 = 值 × 轨道宽
        /// （旧路是靠「根缩放 × 整宽」凑出来的，与锚点式同值）⇒ **判别式是 `FillCapWorldW`**
        /// （端帽**恒宽** vs **跟着值缩**），那一条才分得开（判据见 `LayoutFill`）。
        /// <para>🔴 **2026-10-16（`T09` 块 6）**：画布档下 = 那张 `Image (Sliced)` 的 `sizeDelta.x ÷ U`
        /// （仍是**世界单位**契约；见 `FillSpanW` 那一支）。</para></summary>
        public float FillWorldW
        {
            get { float a, b; return FillSpanW(out a, out b) ? b - a : 0f; }
        }

        /// <summary>填条**画出来**的左沿（相对轨道中心的世界 x）—— 自检用。原版 `Fill` 锚在**左边**的锚点上
        /// ⇒ 它**恒 = `−轨道宽/2`**（值怎么变都不动）。
        /// ⚠️ **这一条本身【不是】`A1359` 的判别式** —— 旧的整体缩放路**也**给同一个值（它是靠把根
        /// 右移 `w/2` 补回来的，算式上恒等，别拿它当「锚点式已经生效」的证据）；它挡的是
        /// 「谁把填条左沿挪走了」这一类改法（左沿一走 ⇒ 填条与轨道左端脱开）。
        /// <para>🔴 **2026-10-16（`T09` 块 6）**：画布档下 = `localPosition.x − sizeDelta.x/2`，再 ÷ `U`
        /// （同一个**世界单位**契约、同一个物理量：相对轨道中心的左沿）。</para></summary>
        public float FillWorldLeftX
        {
            get { float a, b; return FillSpanW(out a, out b) ? a : 0f; }
        }

        /// <summary>填条三块子 quad 里**端帽那块【画出来】的宽**（**世界单位**）—— 自检用。
        /// 🔴 **这一格才是 `A1359` 的判别式**：原版（改锚点）下它**恒 = `m_Border 30 ÷ ppuMul 2 × capScale`**
        /// （连同左沿一起把「端帽没跟着缩」这件事钉死）；改回整体缩放 ⇒ 值 0.5 时它**掉一半**。
        /// ⚠️ 因此**根那一级的 `localScale` 也要乘进去**（旧路就是缩在根上的那一档）——
        /// 漏了它这条就恒等于建件宽、什么也验不出来。
        /// ⚠️ 端帽被压缩的那一档（**总宽不足两端端帽之和**，即值小到 **1/18 以下**）**本来就该跟着缩**
        /// （同 uGUI `Image.GetAdjustedBorders`）⇒ 拿它断「恒宽」要在**值够大**的那几格断。
        /// 量不到（三块子 quad 没按名字取到）⇒ 返回 `0`。
        /// <para>🔴 **2026-10-16（`T09` 块 6）：画布档那一支** —— 端帽的**画出来**宽度由切片定
        /// （`Image` 拿 `m_Border ÷ (pixelsPerUnit × ppuMul)` = `30 ÷ 2` = **15 画布 px**、
        /// uGUI 不给量它的 API）⇒ 这里**把同一条规则再算一遍**：`CapOf`（`Image.GetAdjustedBorders`
        /// 那格的**唯一一份**实现，`LayoutFill` 用的也是它）÷ `U`（仍是**世界单位**契约）。</para></summary>
        public float FillCapWorldW
        {
            get
            {
                if (_ugui)
                {
                    var im = _fillImg;
                    if (im == null) return 0f;
                    float total = im.rectTransform.sizeDelta.x * Mathf.Abs(im.rectTransform.localScale.x);
                    return CapOf(FillBorderPx / PpuMul, total) / U;
                }
                if (_fillL == null || _fillRoot == null) return 0f;
                return _fillL.WorldW * Mathf.Abs(_fillL.transform.localScale.x)
                     * Mathf.Abs(_fillRoot.transform.localScale.x);
            }
        }

        // ── 🔴 2026-10-16（`T09` 块 6）：**画布档的自检读口** ───────────────────────────────
        //  ⚠️ **一律读真 px**（`sizeDelta` / `HudCanvas.SizePx`）—— ⛔ 别拿世界尺寸 ×108 折回来
        //     （画布从「设计 px」到「世界单位」的两轴系数**不同**、还随屏变，`T09` §七·8）。
        //  ⚠️ 上面那几个「世界单位」口径的读口（`TrackWorldH` / `FillWorldW` / `FillWorldLeftX` /
        //     `FillCapWorldW`）**在画布档下仍然交世界单位**（内部除以 `U`）⇒ 调用点那句 `× 108f`
        //     **两档同一个算式**。下面这几个口则是**两档都交 px**（新断言用它们更直接）。

        /// <summary>这一件是不是**画布档**（判据只此一处；世界档 = `false`）。</summary>
        public bool OnCanvas { get { return _ugui; } }

        /// <summary>滑块**根**那一级（原版那颗 `Slider` 节点）。两档都有 ⇒ 自检**跨档找子件**都从它出发
        /// （⛔ 别再用 `sp.transform.Find("slider_…")` —— 画布档下它挂在**面板帧**上、不在世界壳下）。</summary>
        public Transform Node { get { return _root != null ? _root.transform : null; } }
        /// <summary>`slider_fill` 那一级（世界档 = 九宫格的根、画布档 = 那张 `Image (Sliced)`）。</summary>
        public Transform FillNode { get { return _fillRoot != null ? _fillRoot.transform : null; } }
        /// <summary>`slider_handle` 那一级（两档同名）。</summary>
        public Transform HandleNode { get { return _handle != null ? _handle.transform : null; } }

        /// <summary>轨道**画出来**的矩形（**px**）。世界档 = 量 `slider_bg` 那几块 quad 的并集
        /// （宽取本件的 `_w`、高取 `TrackWorldH`）；画布档 = 那张 `Image (Sliced)` 的 `sizeDelta`。
        /// 判据 = 原版那三根 **561.08 × 12.00**（战斗）。</summary>
        public Vector2 TrackSizePx
        {
            get
            {
                if (_ugui) { var b = _bgImg; return b != null ? HudCanvas.SizePx(b.rectTransform) : Vector2.zero; }
                return new Vector2(_w * U, TrackWorldH * U);
            }
        }

        /// <summary>填条**画出来**的矩形（**px**）。世界档 = `(FillWorldW, 填条那层的并集高) × U`；
        /// 画布档 = 那张 `Image (Sliced)` 的 `sizeDelta`（= `值 × 轨道宽` × 轨道高）。
        /// 判据 = 「与 `Background` 同高」（原版 `Fill` 与 `Background` 同父同高 —— 见 `A130`）。</summary>
        public Vector2 FillSizePx
        {
            get
            {
                if (_ugui)
                {
                    var im = _fillImg;
                    return im != null ? HudCanvas.SizePx(im.rectTransform) : Vector2.zero;
                }
                float h = 0f;
                if (_fillRoot != null)
                    foreach (var q in _fillRoot.GetComponentsInChildren<ImageQuad>(true))
                        if (q != null) h = Mathf.Max(h, q.WorldH);
                return new Vector2(FillWorldW * U, h * U);
            }
        }

        /// <summary>手柄**画出来**的矩形（**px**）—— 原版那九根 = **34.406 × 34.406**
        /// （110×110 方图 + `preserveAspect` 取短边；判据 → `HandlePx`）。
        /// ⚠️ 它与 `HandleHitPx`（= **原版命中框** 96.811×84.406）**是两个不同的框**，⛔ 别互相顶替。</summary>
        public Vector2 HandleSizePx
        {
            get
            {
                if (_handle == null) return Vector2.zero;
                var rt = _handle.Rect;
                if (rt != null) return HudCanvas.SizePx(rt);       // 画布档：`sizeDelta` = (handlePx, handlePx)
                return new Vector2(_handle.WorldW * U, _handle.WorldH * U);
            }
        }

        /// <summary>三层各自的**兄弟序档**（`HudCanvas.OrderZOf`；认不出 ⇒ `NaN`）—— 自检用。
        /// 🔴 **只对画布档有判别力**（世界档的先后归 `renderQueue`、这里恒 `NaN`）。
        /// 判据 = 「`queue` 大 = 画在上面」⇒ 兄弟序 z **取负** ⇒ 不变量是
        /// **`BgOrderZ > FillOrderZ > HandleOrderZ`**（`Add` 的 z 大 ⇒ 画在底下 ⇒ 轨道压在底下、手柄在最上面）。
        /// 🧨 灭自证：三层里任意两个的 z 写反（例如三处都写 `-queue`）⇒ 这一组红；
        /// 「只断其中一层等于某个数」的写法**抓不住**这种反。
        /// <para>⚠️ **世界档直接返回 `NaN`、不去问 `HudCanvas`**（那些件根本没登记过）——
        /// 免得那三个只读口在**不需要画布的场景**（外壳侧）里白白建出一堆空的 z 账条目。</para></summary>
        public float BgOrderZ
        {
            get
            {
                if (!_ugui || _bgRoot == null) return float.NaN;
                return HudCanvas.OrderZOf((RectTransform)_bgRoot.transform);
            }
        }
        /// <summary>见 <see cref="BgOrderZ"/>。</summary>
        public float FillOrderZ
        {
            get
            {
                if (!_ugui || _fillRoot == null) return float.NaN;
                return HudCanvas.OrderZOf((RectTransform)_fillRoot.transform);
            }
        }
        /// <summary>见 <see cref="BgOrderZ"/>。</summary>
        public float HandleOrderZ
        {
            get
            {
                var rt = _ugui && _handle != null ? _handle.Rect : null;
                return rt != null ? HudCanvas.OrderZOf(rt) : float.NaN;
            }
        }

        // ── 🆕 2026-10-20（W57 · `T09` A3-b）：**Editor-only 只读探针**（给后续断言用；播放器里不存在）──
        //   ⛔ 本段**只读** —— 不提供「模拟一次指针事件」的写口（事件要走 `EventSystem`
        //      / `ExecuteEvents`，见 `T09` 迁移计划 §2 A1·断言③ 的写法；自造写口 = 两条路径）。
#if UNITY_EDITOR
        /// <summary>当前值（= <see cref="Value"/> 的探针版）。</summary>
        public float ValueForTest { get { return Value; } }
        /// <summary>uGUI 拖动状态（`PointerDown → PointerUp`；**世界档恒 false**）。</summary>
        public bool DraggingForTest { get { return _dragging; } }
        /// <summary>uGUI 悬停状态（`PointerEnter/Exit`；**世界档恒 false** —— 那边悬停由宿主喂）。</summary>
        public bool PointerHoverForTest { get { return _pointerHover; } }
        /// <summary>反馈层的「按下」状态（= 原版 `Selectable.Pressed`；两种模式同一个值）。</summary>
        public bool PressedForTest { get { return _fbPressed; } }
        /// <summary>画布档的 uGUI 输入组件（`EventTrigger`）；**世界档 / 建失败 ⇒ `null`**。</summary>
        public Component PointerInputForTest { get { return _pointerTrigger; } }
        /// <summary>轨道那颗 `Image` 的 `raycastTarget`（判据 = 原版 `Background` 的 `m_RaycastTarget = 1`）。</summary>
        public bool TrackRaycastTargetForTest { get { return _bgImg != null && _bgImg.raycastTarget; } }
        /// <summary>填条那颗 `Image` 的 `raycastTarget`（判据 = 原版 `Fill` 的 `m_RaycastTarget = 1`）。</summary>
        public bool FillRaycastTargetForTest { get { return _fillImg != null && _fillImg.raycastTarget; } }
        /// <summary>手柄那颗 `Image` 的 `raycastTarget`（判据 = 原版 `Handle` 的 `m_RaycastTarget = 1`）。</summary>
        public bool HandleRaycastTargetForTest
        {
            get
            {
                var im = _handle != null && _handle.Rect != null ? _handle.Rect.GetComponent<Image>() : null;
                return im != null && im.raycastTarget;
            }
        }
        /// <summary>手柄那颗 `Image` 实际的 `raycastPadding`（画布档；世界档 / 没建 ⇒ `zero`）。
        /// 战斗那三根的期望 = **(−31.2025, −25, −31.2025, −25)**（推导见 `SetupUguiInput` 的 ②）。</summary>
        public Vector4 HandleRaycastPaddingForTest
        {
            get
            {
                var im = _handle != null && _handle.Rect != null ? _handle.Rect.GetComponent<Image>() : null;
                return im != null ? im.raycastPadding : Vector4.zero;
            }
        }
#endif

        public Vector3 WorldPos { get { return _root != null ? _root.transform.position : Vector3.zero; } }
        public Vector3 HandleWorldPos { get { return _handle != null ? _handle.transform.position : WorldPos; } }
        /// <summary>轨道左端 / 右端的世界坐标（自检拿它喂指针，验「点最左 = 0、点最右 = 1」）。</summary>
        public Vector3 LeftWorld { get { return _root.transform.TransformPoint(new Vector3(-_w * 0.5f, 0f, 0f)); } }
        public Vector3 RightWorld { get { return _root.transform.TransformPoint(new Vector3(_w * 0.5f, 0f, 0f)); } }
    }
}
