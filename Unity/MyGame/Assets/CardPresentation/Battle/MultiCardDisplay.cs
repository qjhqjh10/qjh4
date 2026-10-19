// MultiCardDisplay.cs — **一次摊开多张**的展示窗（原版 `UIMultiCardDisplay` / `BattleHud.DisplayMultiCardView`）
//
// 原版出处（**全是解包/反编译，不是照截图猜的**）：
//   · 2D 全树 `资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:309`
//       `Generic Multi Card Display Combat (inactive) [0,671 0x818]`
//         ├ Header Text [-596,671 1192x63]
//         ├ Viewport [0,671 0x818] └ Content [959,671 -1918x818] └ CardUI Reference
//         └ BattleContinueButton [-857,1492 833x50]（Button 578x64 · Text 'Continue' · CircleButton 80x80）
//   · **逐字段权威表** `资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md` §二：
//       根 `anchor(0,0.5)-(1,0.5)` `size(0, **818.04**)` → 绝对 **y[131,949]**、横向铺满；
//       根上**自带 `ScrollRect`**：`m_Horizontal=1 m_Vertical=0 m_MovementType=1(Elastic)` ⇒ **横向滚动**；
//       `Header Text` `anchor(0.5,1) pivot(0.5,1) size(1192.37,63.204)` · 白 · **fs 38**（auto 18..38）· 居中；
//       `Menu Dark Background` 4574.6×2572.4 · `color(0,0,0,**0.7725**)` · 吃 raycast；
//       Continue 条 577.5×63.84（贴图 `40k_bt_underbutton`，按钮色 `(0.369,0.894,0.587)` 绿）+
//       圆钮 80.47×79.64（贴图 `40k_UI_bt_play`，**纵向凸出横条**：条 y945–1009、钮 y937–1017）；
//       `'Continue'` 文字 **右对齐**、fs 38、白。
//   · 反编译 `BattleHud__DisplayMultiCardView.c`：`UIMultiCardDisplay.Initialize(cards, header)`
//     + `WindowsManager.OpenWindow(...)`；调用点在 `BattleManager__ResolveAction.c`（带一个**本地化过的标题**）。
//
// 🔴 **2026-10-16（`T09` 批 3 · `OverHand` 组）：本扇已迁到 uGUI 子画布** ——
//    配方与全部口径 → `资料/任务线/战斗线/任务/T09_整层换uGUI.md` **§五·补⑨ / §五·补⑩**；
//    映射那一行 → `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一（`MultiCardDisplay` 那一行）。
//   · **子画布** = `HudCanvas.EnsureSubCanvas(null, "Generic Multi Card Display Combat Canvas",
//     HudCanvas.SortingLayer.OverHand, 0)` —— 层/order **照原版那一行**（原版这一件**自身没有 `Canvas` 组件**，
//     它随 `Safe area FrontCanvas` 的 `FrontCanvas`(rm2/ovr… / **`OverHand`**/order 0) 渲染 ⇒
//     我们给它一块**同层同序、`overrideSorting`** 的子画布，继承根 canvas 的 `CanvasScaler`）。
//   · 🔴 **本扇因此分成两个节点**（同 `SettingsPanel` 那件「**世界壳 + 画布本体**」的形状）：
//       ① **世界壳** = `MultiCardDisplay`（`Create` 建的那个节点，**仍是**「世界空间 1920×818」）——
//          装**卡片**：`CardView` 是**世界物件**（`T09` §五·补⑧ 明写「手牌与场上卡」还没迁，
//          第 6 步走 `Physics2D` 那条），它的局部单位是**世界单位**、`SetPose` 收的也是世界坐标
//          ⇒ 一搬上画布（局部单位变**设计 px**）那张卡就会**贴着画布中心摆**（差 108 倍、**不报错**；
//          同 `BattleLogPanel` 留下那个「悬停卡」父节点的理由，逐字同一条）。
//          它**不是死件**：`Editor/BattleScene.cs` §A218 量「多卡展示窗根 = 1920×818」量的就是它。
//       ② **画布本体** = `Generic Multi Card Display Combat`（挂在子画布下、**1920×818.04 设计 px**、
//          摆在设计中心 (960,540)）—— 装这一整组「图与字」：**标题 / 继续条 / 圆钮 / 「继续」两个字 /
//          底部提示行**。
//          🔴 **2026-10-16（修波 `FB` · 账 `F11`）**：这一组里**「继续」那三件的锚法换了** ——
//          条 / 圆钮 / 两个字从「**相对面板中心的固定 px**」改成**原版 `BattleContinueButton` 那一级的
//          「右-下点锚」**（`aMin = aMax = (1,0)`，判据 = `RT_3251`/`RT_3058` 五元组，见
//          `BtnBarCx` 那一串常量）。旧写法在 21:9 下整组**左偏 320 px**（= `(画布宽 − 1920)/2`）。
//          标题与底部提示行**不在那一组里**（原版也是）⇒ 仍是中心锚。
//   · 🔴🔴 **本文件里唯一【有意留在世界空间】的图 = `mcd_mask`（整屏压暗）** —— ⛔ 别顺手把它迁上去，
//     三条理由（同批 `CardChoicePanel` 那层 `ChoiceShade` 是**同一条**，两处注释互相指）：
//       ① **它必须画在卡片【后面】**：原版那张 `Menu Dark Background`（4574.6×2572.4 · α0.7725）就是
//          「卡片底下的暗底」。挪到卡片前面 ⇒ 卡被压到 **23% 亮度**、这个窗的用途就没了。
//       ② **卡片是 `MeshRenderer`（`CardView`），不归画布管** —— 它落在 `Default` 排序层、order 0
//          （`CardView` 全族 `renderQueue 3000`）；而我们的子画布是 **`OverHand`**（更高）
//          ⇒ **画布上的任何件都画在它【上面】**（判据 → `T09` §五·补⑤ ⑤：「给了 `sortingOrder`
//          ⇒ 那一扇**盖住所有还没迁的件**」）。
//       ③ ⇒ **在卡片迁上画布之前，「必须画在卡片后面」的整屏件一个都不能上画布。**
//     ⚠️ 这是**临时边界**（随第 6 步「手牌 / 场上卡」那批一起消失），已写进 T09 批 3 的报告。
//     ⚠️ **`_mask` 的字段类型保持 `ImageQuad`**（有意：它**没迁**，类型上就不该装成 `HudPiece`）。
//
// 🔴 **我们挑的（解包里查不到 / 我们没那套设施，如实标着，别当成原版行为）**：
//   ① **卡与卡的间距 24 px** —— 解包里查不到（原版 `Content` 的宽由 `ContentSizeFitter` 运行时算，
//      静态表里是塌缩值 −1917.7）。
//   ② **卡片高度 743 px** —— 🔴 **2026-09-28 查实：这不是「我们挑的」，是原版值**
//      （原来写「原版 `CardUI Reference` 的 scale 在预制体里，本地没读到」—— 那个字段一直在，
//       只是当时没找到节点）。它 = `RectTransform_3421.json` 的 `m_LocalScale = **223.14**`
//      × 卡体 `3.3313` = **743.35 px**。⚠️ **它属于【多卡展示窗】这棵树**，
//      与「卡片详情窗」（`CardDisplayWindow`，卡体 **832.825 px**）**不是同一个尺寸**
//      —— 2026-09-28 订正，判据 → `资料/阶段二_卡片详情窗_原版规格.md` §十·4。
//   ③ **滚动**：原版是 uGUI `ScrollRect` + `RectMask2D` 裁剪；我们这套 HUD 是**世界空间四边形**、
//      没有 uGUI 遮罩 ⇒ 卡放不下时**先把卡等比缩到装得下**（缩到 45% 仍装不下才让它溢出去）。
//      ⚠️ 迁到画布之后**仍没做**（卡片那一支还是世界空间）——「继续不缩、改成横向滚动」是**另开一件**的活。
//   ④ **「继续」文字右对齐**：我们这块 `Label` 只有居中 ⇒ 用「中心点右移」近似。
//      🔴 **2026-10-16（修波 `FB` · 账 `F11`）就地订正：右移的【基准】原来是错的** ——
//      旧写法 `BarCx + BarW*0.5 − 60` 把基准取成了**条的右沿**，而原版那个 `Text` **框**的右沿
//      离条的右沿还有 **227.77 px**（框 `287.33 × 44.856`；TMP `m_HorizontalAlignment = 4(Right)` +
//      `m_margin = 0` ⇒ **字形右沿 = 框右沿**）⇒ 我们那个字一直**压在圆钮上**（`R1` ②-1 与 `A11` §⑥·b
//      量到的那 ≈168 px 就是这处，它量的是另一个参照点）。
//      ✅ 现在基准 = **原版 `Text` 框的右沿**（判据 = `RT_3020` 五元组现算，见 `BtnTextCx`）。
//      ⚠️ **仍留的误差** = 「真实半字宽 − 60 px」（真值要真机量 TMP 字宽 —— 那一笔仍开着，见本件报告 §⑤）。
//   ⑤ **入口**：原版从 `BattleManager.ResolveAction` 里调（环境卡 / 战绩卡组），我们没有那两个流程
//      ⇒ 接到**点我方牌堆**（摊开牌库）—— **入口是我们挑的**。
//
// ⚠️ 遮罩与标题**建一次就留着**，开关只切 `activeSelf`（照 `CardDisplayWindow` 的教训：
//    每次重建、`Hide` 又销毁的话，第二次打开只剩一张光卡）。
// ═════════════════════════════════════════════════════════════════════════════
//  🔴 **2026-10-20（W57 · `T09` 步骤 A3-e2）：点击/关闭整批换成 uGUI**（判据 = 原版 prefab 现读）
//  · 原版 `UIMultiCardDisplay`（`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4182`）的
//    序列化字段：`closeButton` = `MB_5115`（`EverguildButton`，挂在 `BattleContinueButton/Button`）·
//    `backgroundCloseButton` = `MB_4604`（`BackgroundCloseButton`，挂在 `Menu Dark Background`）·
//    `headerText` = `MB_3753` · `mask` = **null** · `closeOnESC = 1` · `enableCardTooltips = 1`。
//  · `UIMultiCardDisplay__Open.c`：`GameWindow.Open()` 之后把 `CloseButtonOnClick` **同时**挂到
//    `closeButton.onClick` 与 `backgroundCloseButton.onClick` 上（`__Close.c` 关窗时 `RemoveAllListeners`）
//    ⇒ **两个入口都关窗**。
//  · `BackgroundCloseButton__…OnPointerClick.c`：`window != null` ⇒ 调它的虚方法（关窗），随后 `onClick.Invoke()`。
//  · Continue 条那颗 `Image`（`MB_4202`）：`m_RaycastTarget = 1` · `m_RaycastPadding = (0,−40,0,−40)`
//    （**上下各外扩 40 px、横向不动**，与 `ContinuePad` 逐值同）；圆钮 `CircleButton` 的 `Image`
//    `m_RaycastTarget = **0**`（不吃射线）。
//  · `Header Text` 的 TMP `m_RaycastTarget = 1`（零处理器 ⇒ 挡射线但不响应）。
//  · **翻页 = `ScrollRect`**（根上 `MB_4823`：`m_Horizontal=1` · `m_Vertical=0` ·
//    `m_MovementType=1(Elastic)` · `Elasticity=0.1` · `Inertia=1` · `DecelerationRate=0.135` ·
//    `m_ScrollSensitivity=1` · viewport `RT_3176` / content `RT_3174`）+ `RectMask2D`；
//    卡片 prefab 的 `CardCollider.sendEventsToScrollRect` 把拖拽转给 ScrollRect。
//    🔴 **我们这一档没做滚动**（文件头 ③：卡片是世界物件、装不下时等比缩）⇒ **没有可迁的拖拽口**；
//    ⛔ 本波不新造「假滚动」（那会改展示顺序/行为）。
//  · 🔴 `_mask` **仍然留在世界空间**（必须画在卡片【后面】，见上面三条）—— 世界 `ImageQuad` 没有
//    `raycastTarget`、uGUI 打不到它 ⇒ 背景关闭的输入由一块**全屏透明 uGUI 件**（`_bgClose`）承担：
//    `color = clear`、不画像素、只吃射线；⛔ `_mask` 的 z / 渲染队列 / 排序**一个字节不动**。
//  · ⚠️ **没有**改 `Show`/`Hide` 的次序、卡片顺序、`TickFeedback` 的悬停反馈判据 —— 本波只加输入口。
//  · ⏭ **仍欠**（如实记）：`closeOnESC = 1`（原版 ESC 关窗；我们的 `BattleDriver` 只在结算屏处理 ESC）·
//    `enableCardTooltips = 1`（卡片 tooltip）—— 两条都要动 `BattleDriver` 或卡片，**本波白名单外**。
// ═════════════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    public class MultiCardDisplay : MonoBehaviour
    {
        // ---- 版面（px @1920×1080，**逐条来自上面那张权威表**）----
        /// <summary>窗口带的上/下缘（绝对 px，左上原点）。原版 `y[131,949]`、高 818.04</summary>
        public const float BandTopPx = 131f, BandBottomPx = 949f;
        /// <summary>标题：宽 1192.37、高 63.204（锚在窗口带**顶边**，`anchor(0.5,1) pivot(0.5,1)`）</summary>
        const float HeaderW = 1192.37f, HeaderH = 63.204f;
        /// <summary>遮罩不透明度。原版 `Menu Dark Background` 的 `color.a`（和放大窗**同一个值**）</summary>
        public const float MaskAlpha = 0.7725f;
        /// <summary>标题与「继续」两个字的字号（px）。**两颗都是原版 TMP `m_fontSize = 38`**
        /// （2026-10-18 现读 `bundle_scenes_scenes_battlearena1`）：`…/Header Text` = 38
        /// （`m_fontSizeBase 31.9`、auto 18..38）；`…/BattleContinueButton/Text`（MB 3886）
        /// `m_fontSize = 38` · `m_fontSizeMax = 38` · `m_text = "Continue"` · `m_HorizontalAlignment = 4(Right)`。
        /// ⚠️ **这是 px（em 的像素值），⛔ 不是 `Label.Create` 的第 4 参** —— 那个是整数 scale 档，
        /// 见 <see cref="ChromeFontScale"/>。</summary>
        const float ChromeFontPx = 38f;
        /// <summary>`Label.Create` 第 4 参要的**整数 scale 档**（⛔ 不是字号；同目录 20 处调用全是 1~8）。
        /// 档位换算：点阵/TMP 每档 = **7 px 大写字高**（`Battle/Label.cs:207` `CapHeightWorldOfScale = 7f/100`），
        /// 而拉丁大写 ≈ **0.72 em**（`Battle/Label.cs:268`）⇒ 原版 fs 38 要的大写字高 = `38 × 0.72 ≈ 27.4 px`
        /// ⇒ `27.4 / 7 ≈ 3.9` ⇒ 最近整数档 = **4**（3 档 → 21 px，小 23%；5 档 → 35 px，大 28%）。
        /// 真正的字号**一律走 `Label.SetScriptHeight(text, px, pxPerUnit)`** —— 那是「按语种二选一」的
        /// **唯一一份**口径（汉字 `SetGlyphHeight(≈1em)` / 拉丁 `SetCapHeight(≈0.72em)`，`Battle/Label.cs:266`）；
        /// 兄弟件同一个形状：`Battle/MulliganPanel.cs:238` · `Battle/CardChoicePanel.cs:423`
        /// （⚠️ 2026-10-16 现读：后者的行号随本批改动挪过 —— 原来这里是 `:156`）。
        /// ⚠️ 原来这两处把 **38 当 scale 传**（= 原版的 12.7 倍），实测症状 = 画面右下角两个巨大白字。
        /// 🔴 **2026-10-16（`T09` 批 3）**：`pxPerUnit` 那一格**两档不同** —— 世界档传 `EndPanel.PxPerUnit`(108)、
        /// **uGUI 档必须传 `1`**（canvas 单位**就是**设计 px，`Label.PxPerUnit` 的 doc 逐字如是）
        /// 传 108 ⇒ 字**小 108 倍**、而**一个字都不报**。</summary>
        const int ChromeFontScale = 4;
        /// <summary>Continue 条在 **16:9 画布**上的绝对位置：`x[1322.7,1900.2] y[945.2,1009]`。
        /// 🔴 **2026-10-16（修波 `FB` · 账 `F11`）：`BarCx/BarCy` 现在【只剩兜底支在用】**
        /// （`HitContinue` 里「条不在画布上」那一支）—— 摆件**不再**用它（见 `BtnBarCx`）。
        /// 留着 = **16:9 自证**：按组锚点算出来的条心 `= 960 + BtnBarCx = **1611.439**`
        /// ⇒ 与本值差 **0.011 px**（那 0.011 就是原版导出里 `−24.2256` vs `−24.2258` 的取整）。
        /// `BarW/BarH` 仍是**尺寸真值**（= 原版 `m_SizeDelta`），两个分支都在用。</summary>
        const float BarCx = 1611.45f, BarCy = 977.1f, BarW = 577.5f, BarH = 63.84f;
        /// <summary>🔴 **Continue 条那颗 `Image` 自己的 `m_RaycastPadding`**（UGUI 分量序 **L,B,R,T**）——
        /// **负 = 外扩**。判据（2026-10-18 · 第六会话现读）=
        /// `python -I d:/tmp/wf_hit/rcunion.py bundle_scenes_scenes_battlearena1 "BattleContinueButton" --depth 3`：
        /// `/BattleContinueButton/Button` 的 `Image`(`40k_bt_underbutton`) = `RT=1` ·
        /// `pad={'x':0.0,'y':-40.0,'z':0.0,'w':-40.0}` ⇒ **上下各外扩 40 px、横向一个像素都不动**。
        /// ⚠️ **不必再乘缩放**：`RectTransform_3348`（那颗钮自己）的 `m_LocalScale = (1,1,1)`，
        /// 且父链 `BattleContinueButton`(3058) → 根(3175) 的 scale 也都是 1（`Viewport`(3176) 的 0.7 在**卡片那一支**上）。
        /// ⛔ 外扩一律走 `MenuDraw.PaddedRect`（「正值缩小、负值扩大」的唯一一份算式），别在本地另写一遍。</summary>
        static readonly Vector4 ContinuePad = new Vector4(0f, -40f, 0f, -40f);
        /// <summary>圆钮：`x[1723.2,1803.7] y[937.3,1016.9]`（**纵向凸出横条**）。
        /// `CircleCx/CircleCy` = 它在 **16:9 画布**上的绝对位置（🔴 **2026-10-16（修波 `FB`）**：与
        /// `BarCx` 同源、**代码里已一处都不引用** —— 留着只为留一条「原版在 16:9 下的绝对位置」的记录，
        /// 用来核上面的角偏移：`960 + BtnCircleCx = **1763.459**`，与本值差 0.009 px）；
        /// `CircleD` = 原版 rect 的**宽**、`CircleH` = 它的**高** —— 原版那一格**不是正方**（80.47 × 79.64）。
        /// ⚠️ **2026-10-16 就地订正（铁律 5）**：本行原写「`CircleD` 也是 `HitContinue` 认的那一份」——
        /// **已过期**：`HitContinue` 的命中区**只**由**条**那一颗的矩形 + 它自己的 `m_RaycastPadding` 算
        /// （见那里的注释），从来不含圆钮（原版圆钮 `m_RaycastTarget = 0`）。
        /// ⚠️ 旧那套 `ImageQuad.Create` 只吃**高**（宽 = 高 × 显示比例）⇒ 那时得喂 `CircleH` + `SetAspect`；
        /// 迁到 uGUI 之后走 `HudCanvas.MakeImageInParent(…, w, h, …)`，**宽高各给一个数** ⇒ 直接喂这一对，
        /// ⛔ 不再需要 `SetAspect`（`MakeImageInParent` 恒 `preserveAspect = false` = 原版 `PA=0` 拉满）。</summary>
        const float CircleCx = 1763.45f, CircleCy = 977.1f, CircleD = 80.47f, CircleH = 79.64f;

        // ---- 🔴 **2026-10-16（修波 `FB` · 账 `F11`）**：`BattleContinueButton` 那一级的锚点 -----------
        //  **判据（2026-10-16 两场现读，逐值同）** = 原版 `bundle_scenes_scenes_battlearena2/RectTransform/
        //  RectTransform_3251.json` 五元组（arena1 的 `RT_3058` 是同一件：
        //  `ap(-24.2256,-53)` vs arena2 的 `(-24.2258,-53)` —— 那 0.0002 是导出的取整）：
        //    `m_AnchorMin = m_AnchorMax = (1, 0)` · `m_Pivot = (1, 0)` · `m_AnchoredPosition = (-24.2258, -53)`
        //    · `m_SizeDelta = (832.89, 49.84)` · `m_LocalScale = (1,1,1)`
        //  ⇒ **右-下【点锚】**：组的**右下角**钉在【面板右下角】往左上各 24.2258 / 53 处。
        //     （旧写法「三件相对**面板中心**固定 px」在 16:9 下是同一处，但 21:9 下整组偏
        //      `(画布宽 − 1920)/2` = **320 px** —— 那正是本件修的。）
        //  `m_Children` 实读 = `[3543 Button] · [3020 Text] · [3579 CircleButton]`（层序判据，见 `ZBtnBar` 那组）。
        //  ⚠️ 组自己**没有任何图形组件**（那个 GO 的 `m_Component` 只有 `RectTransform` 一条 —— 2026-10-16 现读
        //     `…/GameObject/BattleContinueButton.json`）⇒ 我们**不为它另建空节点**：把它那一级的几何
        //     **折进下面这三个角偏移**（结果逐位相同 —— 组内子件的锚点都挂在「组 rect 的中心 + 固定偏移」上）。
        /// <summary>组那一级的五元组（见上面那一段判据）。</summary>
        const float BtnApX = -24.2258f, BtnApY = -53f, BtnBoxW = 832.89f, BtnBoxH = 49.84f;
        /// <summary>组内两件的 x —— 原版 `Button`(RT_3543) `ap.x = 132.11` · `CircleButton`(RT_3579)
        /// `ap.x = 284.1295`；两件都是 `anchor(0.5,0.5) + pivot(0.5,0.5) + ap.y = 0`
        /// （⇒ 与**组心**同一 y）。</summary>
        const float BtnBarDx = 132.11f, BtnCircleDx = 284.1295f;
        /// <summary>组心（组内坐标）—— `pivot = (1,0)` ⇒ 组的 rect 在组内是 `x[-W,0] y[0,H]`
        /// ⇒ 组心 = 枢轴 + `(−W/2, +H/2)`。</summary>
        const float BtnGroupCx = BtnApX - BtnBoxW * 0.5f;      // −440.6708
        const float BtnGroupCy = BtnApY + BtnBoxH * 0.5f;      //  −28.08
        /// <summary>原版 `Text`(RT_3020) 那个**框**在组内的**右沿** —— 由它的五元组现算
        /// （`aMin(0,0)` · `aMax(0.7702433466911316, 0.9)` · `piv(0.5,0.5)` · `ap(145.1,0)` · `sd(-354.2,0)`）：
        /// `框右沿 = (aMax.x − 1) × 组宽 + ap.x + sd.x × (1 − piv.x) = **−223.362**`
        /// （框 = `287.33 × 44.856` · TMP `m_text = "Continue"` · `m_fontSize = 38` ·
        ///  **`m_HorizontalAlignment = 4 (Right)`** · `m_margin = 0` ⇒ **字形右沿 = 框右沿**）。</summary>
        const float BtnTextFrameRx = (0.7702433466911316f - 1f) * BtnBoxW + 145.1f - 354.2f * 0.5f;
        /// <summary>三件的角偏移 = **件心相对【面板右下角】**（y 向上、往左/下为负）——
        /// 三件一律 `anchorMin = anchorMax = (1,0)` · `pivot = (0.5,0.5)`（= 原版那一级的**右-下点锚**）。
        /// · 条心 = 组心 + `BtnBarDx` = **−308.5608** ⇒ 16:9 下 `+960` = **1611.439**（原版 1611.44 ✓）
        /// · 圆钮心 = 组心 + `BtnCircleDx` = **−156.5413** ⇒ 16:9 下 = **1763.459**（原版 1763.46 ✓）
        /// · 字心 = **框右沿** − 60 = **−307.588** —— 那个 60 是「半字宽」的**近似**（见文件头 ④）；
        ///   ⚠️ 旧值是「条的右沿 − 60」= **−79.81** ⇒ 比原版**偏右 227.77 px**（`R1` ②-1 / `A11` §⑥·b）。
        ///     若将来把字宽量出来了，**只改 `BtnTextCx` 这一处**（`−60` → 真实半宽）即可。</summary>
        const float BtnBarCx = BtnGroupCx + BtnBarDx;          // −308.5608
        const float BtnCircleCx = BtnGroupCx + BtnCircleDx;    // −156.5413
        const float BtnTextCx = BtnApX + (BtnTextFrameRx - 60f);   // −307.588
        /// <summary>条 / 圆钮的角偏移 y（与**组心**同 y —— 原版两件的 `ap.y` 都是 0）。</summary>
        const float BtnChromeCy = BtnGroupCy;                  //  −28.08
        /// <summary>「继续」两字的角偏移 y = `Text` **框心**的 y（`aMin.y = 0`、`aMax.y = 0.9`
        /// ⇒ 框在组内 `y[0, 0.9×49.84]`）⇒ `= BtnApY + 0.9 × 组高 / 2`。
        /// ⚠️ 比条心**低 2.492 px**（原版的框心本来就不与条心同高）—— 这是原版值，别拉平。</summary>
        const float BtnTextCy = BtnApY + 0.9f * BtnBoxH * 0.5f;    // −30.572
        /// <summary>Continue 的绿色（原版 Button `colors.normal`）</summary>
        static readonly Color BarColor = new Color(0.369f, 0.894f, 0.587f);
        /// <summary>本扇**子画布**的名字（= 原版宿主 GO 名 + ` Canvas`，同批 5 扇同一个命名法）。</summary>
        const string SubCanvasName = "Generic Multi Card Display Combat Canvas";
        /// <summary>本扇**画布本体**那个节点的名字（**逐字 = 原版宿主 GO 名**）。</summary>
        const string PanelNodeName = "Generic Multi Card Display Combat";

        /// <summary>卡片高度（px）。
        ///
        /// 🔴🔴 **2026-10-15（账 `A2105`）就地订正：原来是 `743f` —— 那个数【漏乘了父链的 0.7】，卡片高了 43%。**
        /// **743 的来历**：`223.14 × 3.3313`（`CardUI Reference` 自己的 scale × 卡体高）。
        /// **漏的是这一跳**：`Generic Multi Card Display Combat`(RT_3370, 1.0) → **`Viewport`(RT_3371,
        /// `m_LocalScale = 0.7`)** → `Content`(RT_3369) → `CardUI Reference`(RT_3616)
        /// ⇒ **lossyScale = 223.14 × 0.7 = 156.198** ⇒ 卡体实绘高 = `3.3313 × 156.198` = **520.3 px**。
        /// ⚠️ **本文件 `:75` 那句注释早就写着这个 0.7**（当时只用它论证「Continue 钮不必乘」）
        /// —— 也就是说**是同一个文件里自己跟自己对不上**（铁律 5 那一类「听着对、其实错」的注释）。
        /// 判据 → `资料/普查产出_1015战斗/C2_逐格对账_弹窗与结算转场.md`（那一条是它查出**最大**的一处）。
        /// ⚠️ **本值是 `public const`、而且被拿去算「装得下几张」**（`:242` 当屏幕高用）⇒
        /// 改成真值之后**同屏能放下的张数会变多**，那是**应有的**（原版就是这样）——
        /// ⛔ 别为了保住旧张数再把它改回去。</summary>
        public const float CardHeightPx = 520.3f;
        /// <summary>卡间距（px）。**我们挑的**（见文件头 ①）</summary>
        public const float SpacingPx = 24f;
        /// <summary>两侧留白（px）—— 判断「装不装得下」时按它算可用宽度</summary>
        const float SideMarginPx = 60f;
        /// <summary>缩放下限：再小就不缩了（溢出去也比糊成一团好认）</summary>
        const float MinShrink = 0.45f;

        const float ZMask = -2.2f, ZContent = -2.3f;   // 比放大窗（−2.0/−2.1）还靠前一点
        /// <summary>画布本体那个节点在**子画布**里的 z 档（它是子画布的**独子** ⇒ 只用于登记兄弟序）。</summary>
        const float ZBody = ZContent;
        /// <summary>「继续」三件在**画布本体**里的 z 档 —— `HudCanvas.Add` 的 z **大 ⇒ 画在底下**
        /// （`HudCanvas.Group.Add` 按 z **降序**排兄弟序）。
        /// 🔴 **判据 = 原版 `RT_3251.m_Children` 实读**（2026-10-16 现读）= **`[3543 Button] · [3020 Text]
        /// · [3579 CircleButton]`** ⇒ **条在最下 · 字居中 · 圆钮在最上**（arena1 的 `RT_3058` 子件序逐位同）。
        /// 🔴🔴 **2026-10-16（修波 `FB` · 账 `F1`）：这里原来是「三件同 `ZContent`」** —— 同 z 的先后由
        /// **建序**定（`Group.Add` 把同 z 的插在**末尾**），而建序是 `_bar` → `_circle` → `_barText`
        /// ⇒ **文字画在圆钮之上**，与原版相反（`R1` ②-1；而当时我们的字又正好压在圆钮上 ⇒ 这一条有可见后果）。
        /// ⇒ 改成**显式档**（⛔ 别只改建件顺序 —— 那是**隐式**的，下一次谁调整建序就又反了）。
        /// ⚠️ 三件都**严格小于** `ZContent`（标题那一档）⇒ 整组画在**标题之上**，与原版
        /// `RT_3370.m_Children = [Menu Dark Background] · [Header Text] · [Viewport] · [BattleContinueButton]`
        /// 的先后一致 ✓ —— **不靠建序**（标题比条先建这件事只是巧合，⛔ 别拿它当判据）。
        /// （底部提示行 `mcd_hint` 仍是 `ZContent`，在整组**之下**；两者不重叠，无画面后果。）</summary>
        const float ZBtnBar = ZContent - 0.01f, ZBtnText = ZContent - 0.02f, ZBtnCircle = ZContent - 0.03f;
        /// <summary>🆕 2026-10-20（A3-e2）：背景关闭件（`mcd_background_close`）在**子画布**里的 z 档。
        /// 必须**大于 `ZBody`** —— `HudCanvas.Add` 按 z **降序**排兄弟序（z 大 ⇒ 前面 ⇒ 画在底下）
        /// ⇒ 它排在面板本体**之后**（= 面板里的标题 / 继续条 / 圆钮 / 字都盖在它上面）。
        /// ⚠️ 只登记这一块的兄弟序；⛔ **不碰 `_mask` 的 z / 渲染队列 / 排序**（它在世界空间、见文件头）。
        /// </summary>
        const float ZBgClose = ZBody + 0.1f;

        Transform _root;          // 世界壳：卡片 + 整屏压暗（见文件头那张「两个节点」的说明）
        Transform _panel;         // 画布本体：标题 / 继续条 / 圆钮 / 两个字 / 提示
        ImageQuad _mask;          // 🔴 有意**留在世界空间**（必须压在卡片后面，见文件头那三条）
        HudPiece _bar, _circle;
        /// <summary>🆕 2026-10-20（W57 · `T09` A3-e2）：`_bar` 的 uGUI 点击 —— 原版
        /// `UIMultiCardDisplay.closeButton`（那颗 `EverguildButton`，`Open()` 时挂 `CloseButtonOnClick`）。
        /// `null` = 没挂上（建件缺贴图 / 不是 uGUI 件）—— 调用点已出声。</summary>
        BattleHudButton _continueBtn;
        /// <summary>🆕 2026-10-20（A3-e2）：原版 `backgroundCloseButton` 的 uGUI 承载件 ——
        /// **全屏透明**、只吃射线、点它关窗。⚠️ 我们的压暗层 `_mask` **有意留在世界空间**
        /// （必须画在卡片【后面】，见文件头）⇒ 世界 `ImageQuad` 不能当 uGUI 射线靶 ⇒ 输入走这一块。
        /// ⛔ 这**不是**第二张压暗图：`color = clear`、一个像素都不画，只影响命中。</summary>
        HudPiece _bgClose;
        /// <summary>🆕 2026-10-20（A3-e2）：`_bgClose` 上的点击（= 原版 `BackgroundCloseButton`）。</summary>
        BattleHudButton _bgCloseBtn;
        /// <summary>🔴 **2026-10-20（W57 · `T09-FIX3` · P1-3）：uGUI 关窗只记「待关」**，
        /// 由 <see cref="LateUpdate"/> 在本帧末尾落账（同 `ChatPopupPanel` / `BattleLogPanel` / `AlliancePanelWindow`）。
        /// <para>为什么不当场 `Hide()`：`EventSystem.Update` 与 `BattleDriver.Update` 都是默认执行顺序 ——
        /// 当场关掉会让驱动那一帧看到「窗本来就关着」（`TickMultiCards` 的 `if (!Visible) return false`）
        /// ⇒ **同一次松手继续落到 `DrivePlayerTurn`**（Continue 条与牌堆命中区大面积重叠 ⇒ 点「继续」反而
        /// 又把牌库窗打开）。挂起到 `LateUpdate`（所有 `Update` 跑完之后）⇒ 两种顺序都安全。</para></summary>
        bool _closePending;
        /// <summary>uGUI 关闭被**请求**了几次 / 真**落账**了几次（自检用；落账幂等）。</summary>
        int _uguiCloseClicks, _uguiCloseApplied;
        // 🆕 2026-10-19（B4 · `N70` P0-2）：继续钮的反馈（原版那一族 `Continue Button` t=1 ColorTint +
        //   `CircleButton` t=2 SpriteSwap `40k_UI_bt_play_hover/_pressed`；本地缺那两张 ⇒ 圆钮退回 tint 并出声）。
        BattleButtonFx _fxBar, _fxCircle;
        Label _header, _barText, _hint;
        readonly List<CardView> _cards = new List<CardView>();

        /// <summary>窗是不是开着（自检用）</summary>
        public bool Visible { get; private set; }
        /// <summary>摊开了几张（自检用）</summary>
        public int CardCount { get { return _cards.Count; } }
        /// <summary>标题那行字（自检用）</summary>
        public string HeaderShown { get; private set; }
        /// <summary>这一排的实际宽度（px；自检用 —— 用来验「装得下 / 缩过」）</summary>
        public float ContentWidthPx { get; private set; }
        /// <summary>画这一排用的缩放（自检用；和「无缩」相比小就是缩过）</summary>
        public float UsedScale { get; private set; }
        /// <summary>窗口带里第一个卡片的中心（px；自检用）</summary>
        public float FirstCardXpx { get; private set; }
        /// <summary>卡片中心 y（px；自检用）</summary>
        public float CardCypx { get; private set; }

        /// <summary>🆕 **2026-10-16（`T09` 批 3）**：本扇**画布本体**那个节点的 rect（自检用）。
        /// 判据 = **原版那一对锚点逐字复刻**：`Generic Multi Card Display Combat` 的
        /// `anchor (0,0.5)-(1,0.5)`（横向 stretch）+ `sizeDelta (0, **818.04**)`
        /// ⇒ 我们写 `sizeDelta (0, BandBottomPx − BandTopPx = **818**)`、横向由 stretch 拿父宽
        /// ⇒ `rect = (**画布宽** × 818)`（16:9 下画布宽就是 1920）。
        /// ⚠️ 拿不到（子画布没立起来 ⇒ 本窗整个没建）时返回 `null`。</summary>
        public RectTransform PanelRect { get { return _panel as RectTransform; } }

        public static MultiCardDisplay Create(Transform parent)
        {
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`。
            //    判据 = 原版同名件 `Generic Multi Card Display Combat` 实读（`bundle_scenes_scenes_battlearena1`，
            //    2026-10-11 现读）：`RectTransform` · `anchor (0,0.5)-(1,0.5)`（**横向 stretch**）·
            //    `sizeDelta (0, **818.04**)` ⇒ 绝对矩形 **x 铺满 1920 · y 818.04**（与本文件头引的
            //    「2D 全树 `[0,671 0x818]` / `size(0,818.04)` → y[131,949]」逐位一致）。
            //    ⚠️ 原版靠横向 stretch 拿父宽 ⇒ 我们用「重合锚点 + 1920×818.04」表达同一个矩形（锚点不复刻）。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「多卡展示窗根 = 1920×818.04」红。
            //    ⚠️ 高度取本文件那对上下缘（`BandBottomPx − BandTopPx` = 818，原版精确值 818.04 ——
            //       那 0.04px 的取整在本文件全部几何里已经这样用了，⛔ 别在这儿另立一个数）。
            // 🔴🔴 **2026-10-16（`T09` 批 3）：这个节点【有意不迁】—— 它是「世界壳」**（只装卡片与整屏压暗，
            //    两者都**必须留在世界空间**，理由三条写在文件头）。⇒ `MenuDraw.SetPxSize` 那对**世界单位**
            //    写法**照旧**（⛔ 别换成 `HudCanvas.SetPxSize` —— 那会把 818 解释成 818 **设计 px**＝世界单位的
            //    1/108，卡片那一支的 `Pos()` 是按**世界单位**算的，换了两边就对不上）。
            var go = new GameObject("MultiCardDisplay", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, BandBottomPx - BandTopPx);
            var w = go.AddComponent<MultiCardDisplay>();

            // ---- 子画布：`OverHand` / order 0（判据 = 文件头那一行，⛔ 别自己编）------------------------
            // 🔴 **子 Canvas 一个 `CanvasScaler` 都不许加**（原版全场景只有一个、在根上 ⇒ 加了会二次缩放）。
            HudCanvas.Ensure(LayoutSpace.Cam);      // 幂等（`HudAbs` 那一族会先建；这里兜底）
            // 🔴 **2026-10-16（修波 `FB` · 账 `F10`）就地订正（铁律 5）：这里原来包了一层【幂等包装】** ——
            //   `var cv = SubCanvas(name); var cvRt = cv != null ? … : EnsureSubCanvas(…)`，理由是
            //   **当时**「`EnsureSubCanvas` 每调一次就 `new` 一块、**不会去重**」。**那句已过期**：
            //   它现在**按名字幂等**（`HudCanvas.cs` 的 `EnsureSubCanvas` 体内先查 `SubCanvas(name)`），
            //   它的 doc 逐字写着「⛔ 调用点**不要再自己包一层**（那是同一个规矩的第二份写法）」
            //   ⇒ 直接调它。（同族另外 3 处仍在包：`BattleDoors` · `CardChoicePanel` · `SettingsPanel`。）
            //   ⚠️ **行为差一处、且是本波要的**：旧写法「已有同名画布 ⇒ 原样返回、层/order 一个字不动」，
            //   新写法会把已有的那块**对齐到本次传的 (层, order)** —— 传的就是原版那一组（`OverHand`/0）
            //   ⇒ 只在「已有那块本来设错了层」时才不同，而那一档旧写法是**静默错**（不报错、画到别的层）。
            var cvRt = HudCanvas.EnsureSubCanvas(null, SubCanvasName,
                                                HudCanvas.SortingLayer.OverHand, 0);
            if (cvRt == null)
            {
                // ⛔ **不许静默退回世界空间 quad** —— 半迁移态（一半跟画布、一半跟世界）在 16:9 下看不出来
                //    （同 `HudCanvas.Ensure` / `Label.CreateUgui` 那两条红线）。
                UnityEngine.Object.DestroyImmediate(go);
                Debug.LogError("[多卡展示窗] `OverHand` 子画布建不起来（根 canvas 没有？）⇒ **本窗不建**");
                return null;
            }
            // 画布本体：🔴 **逐字照原版那对锚点**（`T09c` §一 那一行 / 本文件头引的逐字段表）——
            //   `anchor (0,0.5)-(1,0.5)`（**横向 stretch**、竖直居中）+ `sizeDelta (0, **818.04**)`
            //   ⇒ 我们写 `anchorMin(0,0.5)/anchorMax(1,0.5)` + `sizeDelta (0, BandBottomPx−BandTopPx)`。
            //   ⛔ **别退回「重合锚 + 写死 1920×818」**（那是**世界壳**那两个节点的写法、它在 21:9 下会窄 640 px）；
            //      也别用 `FillParent` —— 那会把高变成**画布高**（4:3 下 1440）而不是原版那个 818.04。
            // ✅ 这样锚点/枢轴与父链**与原版逐字一致**，而本节点的中心仍然 = 画布中心 = 屏心
            //    ⇒ `HudCanvas.PosPx(x, y从顶)` 交出来的那一对数**照旧**是本节点的局部位移（子件全是中心锚）。
            var pgo = new GameObject(PanelNodeName, typeof(RectTransform));
            pgo.transform.SetParent(cvRt, false);
            var prt = (RectTransform)pgo.transform;
            prt.anchorMin = new Vector2(0f, 0.5f);
            prt.anchorMax = new Vector2(1f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(0f, BandBottomPx - BandTopPx);
            w._panel = prt;
            HudCanvas.Add(prt, ZBody);                 // 登记兄弟序（子画布下只有它一个）

            w.Build();
            w.Hide();
            return w;
        }

        void Build()
        {
            _root = transform;                 // 世界壳（卡片 + 整屏压暗）
            // ⛔ 这里**不设**「`_panel` 为空就退回 `_root`」那种兜底 —— 画布件挂在世界节点下**根本不渲染**
            //    （`CanvasRenderer` 没有祖先 `Canvas`）⇒ 那是个「看不见、也不报错」的假兜底。
            //    `_panel` 由 `Create` 保证非空（建不起来时本窗整个不建、已出声）。

            // 整屏压暗：🔴 **有意留在世界空间**（见文件头那三条 —— 它必须画在卡片**后面**，
            // 而卡片是 `MeshRenderer`、不归画布管 ⇒ 迁到 `OverHand` 画布上会把卡压到 23% 亮度）。
            // 原版那张是 4574.6×2572.4（两倍屏还多）⇒ 我们按「可见高 ×1.2」开，两个轴都盖满。
            _mask = ImageQuad.Create(_root, CardDisplayWindow.SolidTexFor(LayoutSpace.VisibleWidth / LayoutSpace.DesignHeight),
                                     Vector3.zero, LayoutSpace.DesignHeight * 1.2f,
                                     new Vector2(0.5f, 0.5f), "mcd_mask");
            if (_mask != null)
            {
                _mask.transform.localPosition = new Vector3(0f, 0f, ZMask);
                _mask.SetTint(new Color(0f, 0f, 0f, MaskAlpha));
            }
            else Debug.LogError("[多卡展示窗] 整屏压暗（`mcd_mask`）没建起来");

            // ---- 以下五件 = 「画布本体」那一组（图与字，全部在设计 px 上）----
            // 🔴 **2026-10-16（`T09` 批 3）的换口**：`Label.Create(...) + Pos(px,py,z)` → `HudCanvas.MakeText(...)`
            //    （**设计 px**、**相对父件中心**、y 向上）；`ImageQuad.Create(...)` → `HudCanvas.MakeImageInParent(...)`
            //    （**相对父件中心**、y 从顶向下、**宽高各给一个数**）。⛔ **不能**用 `QuadFromTopPx` /
            //    `MakeImage` —— 那两个的 `x/y` 是**屏幕绝对 px**（锚点 = `x/1920`、`1−y/1080`），
            //    **只在 `parent` 本身是整屏矩形时才等于「相对父中心」**；本节点是 1920×**818** 的横带
            //    ⇒ 用那两个会**静默摆到别处**（`HudCanvas.MakeImageInParent` 的 doc 逐字写着这条）。

            // 标题：锚在窗口带**顶边**（原版 `anchor(0.5,1) pivot(0.5,1)`）
            // 第 4 参是**整数 scale 档**（⛔ 不是原版那个 fs 38 —— 那个走 `SetScriptHeight`，见常量处的注释）。
            // 出厂这一下只是给个尺寸：标题文字是 `Show()` 里才定的（语种跟着标题走）⇒ 字号在那边再设一遍。
            // ⚠️ `SetScriptHeight` 的 `pxPerUnit` 在 uGUI 档 = **1**（canvas 单位就是设计 px）——
            //    传 `EndPanel.PxPerUnit`(108) 会让字**小 108 倍**而**不报错**。
            var tp = HudCanvas.PosPx(960f, BandTopPx + HeaderH * 0.5f);
            _header = HudCanvas.MakeText(_panel, "", tp.x, tp.y, ChromeFontScale, Color.white,
                                         new Vector2(0.5f, 0.5f), ZContent, "mcd_header", face: TmpFont.Face.LatinCardBody);
            if (_header != null) _header.SetScriptHeight("", ChromeFontPx, 1f);
            // 🆕 2026-10-20（A3-e2）：原版 `Header Text` 的 TMP `m_RaycastTarget = 1`（**零处理器**）
            //   ⇒ 它挡射线但**不响应**（点标题不关窗，点标题以外的暗底才关）。
            //   我们的 `Label` 出厂 `raycastTarget = false`（`TmpFont.NewText` 那条装修约定）⇒ 这里显式打开。
            //   ⚠️ 如实记：原版那个节点的 rect 是 1192.37×63.204（整条），我们的 TMP 只占**文字自身**的
            //   bbox（`Label.RefreshBounds` 写的是文字尺寸）⇒ 挡射线的那块比原版小；无处理器、无视觉差。
            var headerTmp = _header != null ? _header.GetComponentInChildren<TMP_Text>(true) : null;
            if (headerTmp != null) headerTmp.raycastTarget = true;
            else Debug.LogWarning("[多卡展示窗] 标题的 TMP 取不到 ⇒ 它不吃射线（原版 RT=1；无处理器、无视觉差）");

            // Continue：**贴图用原版那两张**（`40k_bt_underbutton` / `40k_UI_bt_play`），
            // 图不在工程里时退回纯色（颜色仍是原版那个绿 —— 不静默、也不假装）
            //
            // 🔴 **2026-10-18：这一块原来两处都错，本件修的就是它们**
            //  ① **量纲**：`ImageQuad.Create` 第 4 参是**世界高度**，原来喂的是 px 原值
            //     （`BarH` / `CircleD`）⇒ 实绘 = 原版 rect 的 **108 倍**（条 40,285 × 6,894 px，
            //      整屏被 `40k_bt_underbutton` 铺满 —— 实据 `d:/4/_tmp_view/battle/11b_多张展示窗.png`）。
            //     改用那个世界档的 `U(...)` —— 🔴 **2026-10-16（`T09` 批 3）之后这个助手【已从本文件删掉】**
            //     （本件只剩 `_mask` 一件在世界空间、而它不开 px）：现在这两颗**直接给设计 px**
            //     （见下面那两句 `MakeImageInParent` 的注释），⛔ 别再除 108。
            //  ② **显示比例**：`ImageQuad` 默认按**贴图比例**定宽（`ImageQuad.cs:124` `_aspect = tex.w/tex.h`），
            //     而原版这两格都是 `m_PreserveAspect = 0` + `m_Type = 0(Simple)` ⇒ **拉满 rect**。
            //     判据 = **2026-10-18 现读** `bundle_scenes_scenes_battlearena1`：
            //       `Generic Multi Card Display Combat/BattleContinueButton/Button`（RT 3348）
            //         `m_SizeDelta = (577.50, **63.84**)` · 它的 Image（MB 4202）`m_PreserveAspect = 0` · `m_Type = 0`；
            //       `…/CircleButton`（RT 3384）`m_SizeDelta = (**80.47**, **79.64**)` · Image（MB 4285）同样 PA=0 / Type=0。
            //     贴图实际尺寸：`40k_bt_underbutton` = **485×83**（比例 5.8434，≠ 9.046）
            //       ⇒ 不补 `SetAspect` 只画出 **372.9 × 63.84**（**窄 204.6 px**）；
            //       `40k_UI_bt_play` = **128×128**（正方，≠ 1.0104）⇒ 差 1%（0.83 px），照旧补上。
            //     兄弟件同一条：`Battle/CardChoicePanel.cs` 的 `MakeImg`（**已经在画布上了** ⇒ 它给的也是
            //       宽高各一个数，见那里的注释）· `Battle/MulliganPanel.cs:265`（**还没迁** ⇒ 仍是
            //       `if (… != null) ….SetAspect(BarW / BarH);` 那句世界档写法）。
            // 🔴 **2026-10-16（`T09` 批 3）**：uGUI 档改走 `MakeImageInParent(…, wPx, hPx, …)` —— **宽高各给一个数**、
            //    `preserveAspect` 恒 `false` ⇒ **原版那对 `m_SizeDelta` 直接落进 `sizeDelta`**，
            //    `SetAspect` 那一步**不再需要**（原来那两句算出来就是同一对数：条 577.5×63.84、圆钮 80.47×79.64）。
            // 🔴 **2026-10-16（修波 `FB` · 账 `F11`）：这一块三件的【位置】不在这里给** ——
            //    `MakeImageInParent` / `MakeText` 建出来的是**中心锚**（位置相对**父件中心**），而原版这一组
            //    是**右-下点锚**（`aMin = aMax = (1,0)`、钉在**面板右下角**）⇒ 三件建完之后由
            //    `AnchorBottomRight(…)` 统一改锚（坐标 = `BtnBarCx` 那一串常量）。这里传的 `0f, 0f` 是**占位**、
            //    随后就被改掉 —— ⛔ **别把位置写在这儿**（两处写同一个位置 = 迟早不一致）。
            // 🔴 **层序也显式给了**（`ZBtnBar/ZBtnText/ZBtnCircle`，判据 = 原版子件序）—— 原来是三件同 z、
            //    先后靠**建序**定 ⇒ 文字压在圆钮上（账 `F1`）。
            var barTex = CardArt.Ui("40k_bt_underbutton");
            _bar = HudCanvas.MakeImageInParent(_panel,
                                               barTex != null ? barTex : CardDisplayWindow.SolidTexFor(BarW / BarH),
                                               "mcd_bar", 0f, 0f, BarW, BarH, ZBtnBar, Color.white);
            if (_bar == null) Debug.LogError("[多卡展示窗] 「继续」那条横条没建起来（贴图与纯色兜底都取不到？）");
            if (_bar != null && barTex == null) _bar.SetTint(BarColor);

            var circleTex = CardArt.Ui("40k_UI_bt_play");
            _circle = HudCanvas.MakeImageInParent(_panel,
                                                  circleTex != null ? circleTex : CardDisplayWindow.SolidTexFor(1f),
                                                  "mcd_circle", 0f, 0f,
                                                  CircleD, CircleH, ZBtnCircle, Color.white);
            if (_circle == null) Debug.LogError("[多卡展示窗] 「继续」那颗圆钮没建起来");

            // ── 🆕 2026-10-20（W57 · `T09` A3-e2）：背景关闭的 uGUI 输入 ─────────────────────────
            // 原版 = `Menu Dark Background` 上的 `BackgroundCloseButton`（`MB_4604`；
            //   `IPointerClickHandler.OnPointerClick` → `window.Close()` + `onClick.Invoke()`），
            //   `UIMultiCardDisplay.Open()` 把 `CloseButtonOnClick` 挂进它的 `onClick`。
            // 🔴 我们那颗压暗层 `_mask` **有意留在世界空间**（见文件头三条：必须画在卡片【后面】）——
            //   世界 `ImageQuad` 没有 `raycastTarget`、uGUI 打不到它 ⇒ 用**一块全屏透明件**承载同一条输入路径。
            //   ⛔ `_mask` 的 z / 渲染队列 / 排序**一个字节不动**（那正是本步的硬约束）。
            var cvRt = _panel != null ? _panel.parent as RectTransform : null;
            if (cvRt == null)
            {
                Debug.LogError("[多卡展示窗] 拿不到子画布节点 ⇒ **背景关闭**建不出来（点空白不会关）");
            }
            else
            {
                _bgClose = HudCanvas.MakeImageInParent(cvRt, CardDisplayWindow.SolidTexFor(1f),
                                                       "mcd_background_close", 0f, 0f, 1f, 1f,
                                                       ZBgClose, Color.clear);
                if (_bgClose == null)
                {
                    Debug.LogError("[多卡展示窗] 背景关闭件（`mcd_background_close`）没建起来（纯色图取不到？）");
                }
                else
                {
                    HudCanvas.FillParent(_bgClose.Rect);   // 铺满子画布 = 全屏；⛔ 别写死 1920×1080
                    _bgCloseBtn = BattleHudButton.Attach(_bgClose, RequestCloseByUgui);
                    if (_bgCloseBtn == null) Debug.LogError("[多卡展示窗] 背景关闭件没挂上 uGUI 点击");
                }
            }

            // 文字**右对齐**在条内右侧（原版 `H=4(Right)`）—— 我们这块 `Label` 只有居中
            // ⇒ 用「中心点右移」近似（**我们挑的**，见文件头 ④）
            // 🔴 **2026-10-16（修波 `FB` · 账 `F11`）就地订正**：那个「右移 60」的**基准**原来是**条的右沿**
            //    （`BarCx + BarW*0.5 − 60`），而原版 `Text` **框**的右沿在条的右沿往左 **227.77 px** 处
            //    （框 `287.33 × 44.856`、右对齐）⇒ 我们的字一直**压在圆钮上**（`R1` ②-1 / `A11` §⑥·b）。
            //    ✅ 现在基准 = **框的右沿**（`BtnTextCx` 那一串由原版五元组现算）。
            // 🔴 **2026-10-18（第十二轮 · W6）**：那颗钮上的字走**原版词条** `Battle/Mulligan/ButtonDone`
            //    （原版 `Generic Multi Card Display Combat < BattleContinueButton < Text` 挂的就是它，
            //     TMP 原文 `Continue`）—— 原来写死的是中文 `"继续"`（与换牌面板那条是**同一条**词条）。
            string continueText = Loc.T(MulliganPanel.DoneTerm);
            _barText = HudCanvas.MakeText(_panel, continueText, 0f, 0f, ChromeFontScale,
                                          Color.white, new Vector2(0.5f, 0.5f), ZBtnText, "mcd_continue", face: TmpFont.Face.LatinCardBody);
            if (_barText != null) _barText.SetScriptHeight(continueText, ChromeFontPx, 1f);

            // 🔴 **三件【同批】改走原版那一级的右-下点锚**（账 `F11`）—— ⛔ 只改一两个会造出
            //    「两件在锚点上、一件在面板中心」的**半迁移态**（16:9 下还看不出来 ⇒ 同 `T09` §五·补⑦ 那条红线）。
            //    ⚠️ `SetScriptHeight` 排在改锚**之前**是有意的：它只改字号/版面，不碰本节点自己的锚点
            //        （文字块动的是它的 **TMP 子节点** —— `Label.RefreshBounds` 写的是
            //        `_tmp.rectTransform.localPosition`）⇒ 顺序无妨，但把改锚放最后更稳。
            AnchorBottomRight(_bar != null ? _bar.Rect : null, BtnBarCx, BtnChromeCy);
            AnchorBottomRight(_circle != null ? _circle.Rect : null, BtnCircleCx, BtnChromeCy);
            AnchorBottomRight(_barText != null ? (RectTransform)_barText.transform : null,
                              BtnTextCx, BtnTextCy);

            // 🆕 2026-10-20（A3-e2）：`_bar` = 原版 `UIMultiCardDisplay.closeButton`（`EverguildButton` `MB_5115`，
            //   `m_TargetGraphic` = `MB_4202` 那颗 `40k_bt_underbutton`）—— `Open()` 时把 `CloseButtonOnClick`
            //   挂上 ⇒ **点它关窗**（与背景同一个回调，判据 → 文件头「点击/关闭」那段）。
            //   它的 `Image`：`m_RaycastTarget = 1` · `m_RaycastPadding = (0,−40,0,−40)`
            //   ⇒ **上下各外扩 40 px、横向不动**；两条都在这里照抄（rect 本来就是原版节点 rect 577.5×63.84）。
            _continueBtn = BattleHudButton.Attach(_bar, RequestCloseByUgui);
            if (_continueBtn == null)
            {
                Debug.LogError("[多卡展示窗] 「继续」条没挂上 uGUI 点击（点它不会关）");
            }
            else if (!_bar.SetRaycastPadding(ContinuePad.x, ContinuePad.y, ContinuePad.z, ContinuePad.w))
            {
                Debug.LogWarning("[多卡展示窗] 「继续」条的 `raycastPadding` 没写进去 ⇒ 命中框少了上下各 40 px");
            }

            // ⚠️ **这行提示是我们自加的**：原版 `Generic Multi Card Display Combat` 下只有
            //    `Header Text`（TMP = `Header Text`、**无 `Localize`**）· `BattleContinueButton` ·
            //    `CardUI Reference` · `Viewport` ⇒ **原版这一格零词条**（铁律 11 例外①）。
            //    它里面那个「继续」是**夹在中文句子里**的 ⇒ 英文档下这行会露中文 ——
            //    **这是已知缺口**（没有原版键可接，要修得先自拟一条两条语言都编的键），已记进交件报告。
            var hntp = HudCanvas.PosPx(960f, BandBottomPx + 34f);
            _hint = HudCanvas.MakeText(_panel, "点「继续」或再点一下牌堆关闭", hntp.x, hntp.y, 2,
                                       new Color(0.7f, 0.7f, 0.75f), new Vector2(0.5f, 0.5f), ZContent, "mcd_hint");
            // A1024③ 判不出（原版 `Generic Multi Card Display Combat` 无这一行，我们自加）⇒ 保持 `Face.Cjk`
            if (_hint == null) Debug.LogWarning("[多卡展示窗] 底部提示行没建起来（`MakeText` 返回 null —— 拿不到 TMP 字体？）");

            SetChrome(false);
            // 🆕 2026-10-19（B4 · `N70` P0-2）
            _fxBar = BattleButtonFx.Tint(_bar, "Continue Button");
            var ph = CardArt.MenuUi("40k_UI_bt_play_hover");
            var pp = CardArt.MenuUi("40k_UI_bt_play_pressed");
            _fxCircle = (ph != null && pp != null)
                       ? BattleButtonFx.Swap(_circle, "CircleButton", _circle != null ? _circle.Texture as Texture2D : null, ph, pp)
                       : BattleButtonFx.Tint(_circle, "CircleButton(tint-fallback)");
        }

        /// <summary>每帧喂状态（驱动层 `TickMultiCards` 里调）。</summary>
        public void TickFeedback(Vector3 world, bool held)
        {
            if (_fxBar == null) return;
            bool over = HitContinue(world), down = over && held;
            _fxBar.SetHover(over); _fxBar.SetPressed(down); _fxBar.Tick();
            if (_fxCircle != null) { _fxCircle.SetHover(over); _fxCircle.SetPressed(down); _fxCircle.Tick(); }
        }
        /// <summary>自检口。</summary>
        public void SimulateFeedback(bool hover, bool pressed)
        {
            if (_fxBar == null) return;
            _fxBar.SetHover(hover); _fxBar.SetPressed(pressed);
            if (_fxCircle != null) { _fxCircle.SetHover(hover); _fxCircle.SetPressed(pressed); }
        }
        public Color FeedbackKeyForTest { get { return _fxBar != null ? _fxBar.KeyForTest : Color.clear; } }

        void SetChrome(bool on)
        {
            if (_mask != null) _mask.gameObject.SetActive(on);
            if (_bar != null) _bar.gameObject.SetActive(on);
            if (_circle != null) _circle.gameObject.SetActive(on);
            if (_barText != null) _barText.gameObject.SetActive(on);
            if (_hint != null) _hint.gameObject.SetActive(on);
            if (_header != null) _header.gameObject.SetActive(on);
            // 🆕 2026-10-20（A3-e2）：背景关闭件**必须跟着开关** —— 漏了它 = 一块全屏射线靶
            //   在窗口关着时继续挡着整个屏幕（`SetActive(false)` 会把它移出 `GraphicRegistry`）。
            if (_bgClose != null) _bgClose.gameObject.SetActive(on);
        }

        /// <summary>把一件**已经建好**的画布件改成原版 `BattleContinueButton` 那一级的**右-下点锚**：
        /// `anchorMin = anchorMax = (1,0)` · `pivot = (0.5,0.5)`（**尺寸一个字节都不动**）。
        /// <para>`dx/dy` = **件心相对【父件右下角】**（`RectTransform` 口径：**y 向上**、往左/下为负）。</para>
        /// <para>🔴 **为什么要用锚点、不能继续写「相对父件中心的固定 px」**（账 `F11`）：两者只在
        /// **画布 1920 宽**（16:9 / 4:3 两档，`Expand` 下同宽）时同值；画布一宽（21:9 ⇒ 2560）就分家，
        /// 差 `(画布宽 − 1920)/2` = **320 px** —— 本扇面板本体是**横向 stretch**（`anchor (0,0.5)-(1,0.5)`），
        /// 挂在它右下的那一组必须跟着**右沿**走。判据 = 原版 `RT_3251`/`RT_3058` 五元组（见本文件
        /// `BtnApX` 那一串常量）＋ `A11`/`R1` 的 16:9 vs 21:9 算式表。</para>
        /// <para>⚠️ 调用点（`Build`）里那三件是**先按中心锚建、再调本函数改锚** —— `HudCanvas.Make*InParent`
        /// 那两个口只出中心锚（它们的多数调用点就是中心锚），故那三处的位置参数传 `0f,0f` 占位。</para>
        /// <para>🔴 **为什么不直接用 `HudCanvas.SetAnchorPx` / `AnchorApPx`（2026-10-16 现读它们的 doc 后判定）**：
        /// 那两个口是同一件事的**共用式**，但它们的式子 `ap = (xPx − ax·1920, 1080(1−ay) − yPxFromTop)`
        /// **把父矩形当成整块 1920×1080**（doc 逐字：「点锚 `(ax,ay)` 指的是**父矩形（= 画布）**的那个角」，
        /// 且 `xPx/yPxFromTop` 是**屏幕绝对**设计 px）⇒ 只在**父件铺满画布**时才成立。
        /// 本件的父件是**画布本体那个 1920×818 的横带**（`_panel`）⇒ 拿它套会**静默摆错**
        /// （要么多算 818 高那一半、要么把角偏移当成屏幕坐标）—— 所以这里**只写「相对父件右下角」的那一步**，
        /// ⛔ 不是「同一条规矩的第二份写法」（本函数**不含**任何 1920/1080 硬编码，锚点完全交给 Unity 的父矩形算）。
        /// </para></summary>
        static void AnchorBottomRight(RectTransform rt, float dx, float dy)
        {
            if (rt == null) return;          // 件没建起来时**建件那一处已经出声**（⛔ 这里不重复报）
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(dx, dy);
        }

        /// <summary>开/关一次（和放大窗一个形状）。</summary>
        public void Toggle(IList<CardData> cards, string header)
        {
            if (Visible) Hide(); else Show(cards, header);
        }

        public void Show(IList<CardData> cards, string header)
        {
            Hide();
            ContentWidthPx = 0f;
            UsedScale = 0f;
            FirstCardXpx = 0f;
            CardCypx = 0f;
            if (cards == null || cards.Count == 0) { HeaderShown = null; return; }

            // 标题的文字与字号**不在这里设** —— 挪到本方法末尾（`SetChrome(true)` 之后）
            // 🔴 理由：TMP **在对象没激活时量不出尺寸**（`ForceMeshUpdate()` 要在 `SetActive(true)`
            //    之后调，否则 `textBounds` 是垃圾 —— CLAUDE.md §三那条坑）；而本方法开头刚跑过
            //    `Hide()` ⇒ 到这里 chrome 整排是关着的。
            HeaderShown = header ?? "";

            // 卡宽（px）：和放大窗同源 —— 卡本体按「高 743 px」反算缩放
            float baseScale = CardHeightPx / EndPanel.PxPerUnit / CardView.Height;
            float cardWpx = CardHeightPx * (CardView.Width / CardView.Height);
            float gapPx = SpacingPx;
            float scale = baseScale;

            float totalPx = cards.Count * cardWpx + (cards.Count - 1) * gapPx;
            // 🔴🔴 **2026-10-16（`T09` 批 3 · 写手 C 现核、主对话裁定并就地修）：这里原来是【量纲错】。**
            //   原式 `LayoutSpace.VisibleWidth - SideMarginPx * 2f` 拿**世界单位**（16:9 下 17.78）去减 **px**（120）
            //   ⇒ `availPx = −102.2` ⇒ **永远判「装不下」**、`k` 永远被 `Max` 夹到 `MinShrink`
            //   ⇒ **无论几张卡都缩到 45%**（卡高 520.3 → 234 px），与张数无关。
            //   ✅ 正解 = 用**同一量纲的屏宽**：`LayoutSpace.DesignPxW`（1920 px）。
            //   ⚠️ 这条与迁 uGUI **无关**（老代码就有），是写手 C 顺手查出来的 ⇒ 按铁律 11「与原版不符的全部要做」。
            //   ⚠️ **它盖住了真实行为**：`:8151` 那条断言的第三支 `UsedScale > 0` **恒真** ⇒ 旧的绿不是判据。
            float availPx = LayoutSpace.DesignPxW - SideMarginPx * 2f;
            if (totalPx > availPx && cards.Count > 1)
            {
                // 装不下 ⇒ **等比缩**（原版这里是横向滚动，见文件头 ③）
                float k = Mathf.Max(MinShrink, availPx / totalPx);
                scale = baseScale * k;
                cardWpx *= k;
                gapPx *= k;
                totalPx = cards.Count * cardWpx + (cards.Count - 1) * gapPx;
            }
            UsedScale = scale;
            ContentWidthPx = totalPx;

            // 竖：标题下面那一条的中点；横：整排居中（原版是 ScrollRect，我们居中 —— 见文件头 ③）
            float cy = (BandTopPx + HeaderH + BandBottomPx) * 0.5f;
            float x0 = 960f - totalPx * 0.5f + cardWpx * 0.5f;
            CardCypx = cy;
            FirstCardXpx = x0;
            for (int i = 0; i < cards.Count; i++)
            {
                // 🔴 **2026-10-16（`T09` 批 3）**：这一段**一个字没改** —— 卡是**世界物件**（`CardView` 是
                //    `MeshRenderer` 那一族），它的父节点是**世界壳** `_root`、`SetPose` 收的是**世界坐标**
                //    ⇒ 仍走 `Pos(px, py, z)`（= `EndPanel.Pos`，**世界单位**）。⛔ 别顺手把它换成 `PosPx`：
                //    那是**画布**口径（设计 px），卡会贴到画布中心去（差 108 倍、**不报错**）。
                //    ⚠️ 代价如实记：卡片不跟 `CanvasScaler` 缩放 ⇒ **非 16:9 下与画布那一组图/字会分家**
                //    （这条随第 6 步「手牌/场上卡迁过去」一起消失）。
                var v = CardView.Create(_root, cards[i], "mcd_card" + i);
                if (v == null) continue;
                v.SetPose(Pos(x0 + i * (cardWpx + gapPx), cy, ZContent), 0f, scale);
                v.SetHighlight(CardHighlightState.Normal);   // 展示窗里的卡不吃「可出牌」那套状态色
                _cards.Add(v);
            }

            SetChrome(true);
            Visible = true;
            gameObject.SetActive(true);
            _closePending = false;      // 🔴 P1-3：重开 ⇒ 丢掉上一帧还没落的待关（否则本帧末尾会把新窗关掉）

            // 标题的文字 + 字号（见本方法开头那条：必须在 chrome **点亮之后**做）
            if (_header != null)
            {
                _header.SetText(HeaderShown);
                // 🔴 字号要在**文字定下来之后**设：`SetScriptHeight` 按 `Loc.HasCjk(text)` 二选一
                //    （汉字 `SetGlyphHeight(≈1em)` / 拉丁 `SetCapHeight(≈0.72em)`）—— 标题是本地化串，
                //    语种得看这一串本身。与 `MulliganPanel.SetTurnText` 同一个形状。
                //    ⚠️ uGUI 档 `pxPerUnit` = **1**（见建件处那条）。
                _header.SetScriptHeight(HeaderShown, ChromeFontPx, 1f);
            }
        }

        public void Hide()
        {
            Visible = false;
            HeaderShown = null;
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i] != null) Kill(_cards[i].gameObject);
            _cards.Clear();
            SetChrome(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════════
        //  🔴 2026-10-20（W57 · 战斗线 `T09-FIX3` · P1-3）：uGUI 关闭**挂起到 `LateUpdate`**
        //  判据与理由逐字同 `ChatPopupPanel.OnClosePointerDown` / `BattleLogPanel.ApplyPendingClose` /
        //  `AlliancePanelWindow.OnUguiCloseClick`（三处先例）：`EventSystem.Update` 与 `BattleDriver.Update`
        //  都是默认执行顺序，当场 `Hide()` 会让驱动那一帧看到「窗已经关着」⇒ 同一次松手继续落到
        //  `DrivePlayerTurn`（Continue 条 x[1322.7,1900.2] y[937.3,1016.9] 与牌堆命中区
        //  x[1615,1845] y[850,1080] **大面积重叠**）⇒ 点「继续」反而开回牌库窗。
        // ═══════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>uGUI（`Continue` 条 / 全屏背景关闭件）被点 —— **只记「待关」**，⛔ 不当场 `Hide()`。</summary>
        void RequestCloseByUgui()
        {
            if (!Visible) return;              // 窗关着 ⇒ 这一下不算（件本来也收不到射线，双保险）
            _uguiCloseClicks++;
            _closePending = true;
        }

        /// <summary>把挂起的「关窗」落账（**幂等**）。批处理没有帧循环 ⇒ 自检手动调；
        /// 真机由 <see cref="LateUpdate"/> 每帧调。
        /// <returns>true = 这一次确实落了一次挂起的关闭。</returns></summary>
        public bool ApplyPendingClose()
        {
            if (!_closePending) return false;
            _closePending = false;
            _uguiCloseApplied++;
            if (Visible) Hide();               // 驱动那条手搓路可能已经在同一帧关过 ⇒ 不再重复
            return true;
        }

        void LateUpdate()
        {
            ApplyPendingClose();
        }

        /// <summary>「继续」那一块（横条）被点到了没有 —— **判据只此一处**。
        /// <para>🔴 **2026-10-18（`A1061`）按订正后的口径重算** —— 判据（第一权威 = 原版 prefab 实读）=
        /// `python -I d:/tmp/wf_hit/rcunion.py bundle_scenes_scenes_battlearena1 "BattleContinueButton" --depth 3`：
        /// 子树里**只有一颗**可射线件 = `/BattleContinueButton/Button` 的 `Image`（`40k_bt_underbutton`，
        /// 577.50 × 63.84，`m_RaycastTarget = 1`），它自己的 `m_RaycastPadding = (0,-40,0,-40)`
        /// （**负 = 外扩**）⇒ **上下各外扩 40**、横向不变；`Text`（TMP）与 `CircleButton`
        /// （`40k_UI_bt_play`）的 `m_RaycastTarget` **都是 0** ⇒ **它们一颗都不吃射线**
        /// （`CircleButton` 上那颗 `EveguildButtonStateFollower` 只跟随状态、**不是** `Button`，
        /// 它 `m_Transition=2` 换的也是**圆钮自己那张图**，与点击无关）。
        /// ⇒ 原版可点区 = **条矩形横向原样、纵向 ±40**。
        /// ⚠️ **2026-10-18 更正（铁律 5）**：本函数原来写
        /// `px &gt;= BarCx − BarW/2 − CircleD/2`（**向左多出圆钮半径 40.235 px**）且**纵向没有那 ±40 外扩**
        /// ⇒ **两处都不对**。错因 = 只见「圆钮 `RT=0`、条 `RT=1`」就**推定「圆钮凸出去的那半圈要算进来」**，
        /// **既没读条自己的 `m_RaycastPadding`、也没核圆钮往哪边凸** —— 原版与我们都把圆钮画在条的
        /// **内部右端**（圆钮 x[1723.2,1803.7] ⊂ 条 x[1322.7,1900.2]，只有**纵向**凸出 ~8 px，
        /// 而那 8 px 已被 ±40 的外扩整个盖住）。</para>
        /// <para>🔴🔴 **2026-10-16（`T09` 批 3）：世界点 → 设计 px 的那一步换了算法。**
        /// 原来写的是 `px = world.x × 108 + 960`（两个轴都按 `108 px/世界单位`）—— 那只在
        /// **条自己也是世界空间 quad** 时成立。条现在在**画布**上，而「世界单位 ↔ 画布单位」的两个轴
        /// **系数可以不同**（`CanvasScaler(Expand)` 在「屏 4:3、相机 `aspect` 被钉成 16:9」那种档里
        /// ⇒ x 轴 `1/108`、y 轴 `1/144`；判据 → `T09` **§七·8**）⇒ 老式子读出来会**偏 0.75 倍**
        /// （而画面是对的）。
        /// ✅ 正解 = **用条自己的局部系反算**（`RectTransform.InverseTransformPoint`，单位 = 设计 px、
        /// 原点 = 条中心、y 向上）—— 与 `SettingsPanel.RectContains` **同一条口径**（⛔ 别在这儿另写一份）。
        /// 取不到条（贴图与兜底都缺失）时才退回老那个「按 108 折」的算式（保持今天的行为）。</para>
        /// <para>🔴🔴 **2026-10-16（修波 `FB` · 账 `F11`）：命中框改成【从条自己的实绘矩形现算】。**
        /// 原来那份是**硬写的 px 矩形**（`BarCx − BarW/2 …`）**再加**「局部点 → 屏幕设计 px（`BarCx + l.x`）
        /// 再比」—— 那两步里的 `BarCx` 恰好**互相抵消**，所以它在 16:9、以及「条挂面板中心且尺寸恒等于
        /// `BarW/BarH`」的旧形态下与正解同值；但条改走**右下点锚**（见 `BtnBarCx`）之后，`BarCx` 这个
        /// 「16:9 下的绝对位置」就不再等于画布变宽时条的实位 ⇒ 留着它 = **新造**一处
        /// 「画到哪 / 判到哪不一致」。✅ 现在直接用条自己那个 `RectTransform`：`sizeDelta` = 设计 px
        /// （= 原版 `m_SizeDelta`，与画布宽无关）、`InverseTransformPoint` = 条自己的局部系
        /// ⇒ **画到哪就判到哪**；且在 16:9 下与改之前**逐点同结论**（`BarW/BarH` 本来就是那两颗 quad 的
        /// `sizeDelta`）。</para>
        /// <para>⚠️ **顺手现读（不是本函数的口径问题）**：生产路径 `BattleDriver.TickMultiCards`
        /// **不拿本函数当点击判据** —— 那一支按「多卡窗开着时**松手即关**」处理（原版对应的是整屏
        /// `Menu Dark Background` 上的 `BackgroundCloseButton`）。
        /// 🔴 **2026-10-20（A3-e2）就地订正（铁律 5）**：本行原写「本函数今天**只有自检在用**」——
        /// **不成立**：`TickFeedback`（每帧由驱动层调）用它对条/圆钮做**悬停/按下反馈**。
        /// 本波起**点击**由 uGUI 派发（`_bar` 与 `_bgClose` 上各一颗 `BattleHudButton`，见 `Build`），
        /// 本函数只留作**反馈几何**与自检（`Editor/BattleScene.cs` 的 `A964/E2④`）。</para></summary>
        public bool HitContinue(Vector3 world)
        {
            if (_bar != null && _bar.IsUgui && _bar.Rect != null)
            {
                // 条那颗 `Image` 的矩形（**条自己的局部系**：原点 = 条中心、单位 = 设计 px、y 向上）
                // → 折成 y 向下（`PxRect` 那一套是左上原点）→ 按**它自己的** `m_RaycastPadding` 外扩
                //   （**共用那一份算式** `MenuDraw.PaddedRect`：正值缩小、负值扩大）
                var size = HudCanvas.SizePx(_bar.Rect);
                var hit = MenuDraw.PaddedRect(
                    new PxRect(-size.x * 0.5f, -size.y * 0.5f, size.x * 0.5f, size.y * 0.5f), ContinuePad);
                var l = _bar.Rect.InverseTransformPoint(world);
                float lx = l.x, ly = -l.y;
                return lx >= hit.x1 && lx <= hit.x2 && ly >= hit.y1 && ly <= hit.y2;
            }
            // ⛔ 兜底：只有「条不在画布上」时才对（**老口径，一字未改** —— `BarCx/BarCy` 现在只剩这一支用）
            var hitFb = MenuDraw.PaddedRect(
                new PxRect(BarCx - BarW * 0.5f, BarCy - BarH * 0.5f, BarCx + BarW * 0.5f, BarCy + BarH * 0.5f),
                ContinuePad);
            float px = world.x * EndPanel.PxPerUnit + 960f;
            float py = 540f - world.y * EndPanel.PxPerUnit;
            return px >= hitFb.x1 && px <= hitFb.x2 && py >= hitFb.y1 && py <= hitFb.y2;
        }

        // ---- 🆕 2026-10-18（A964① ④）：「继续」那一块的**实绘几何**只读口（⛔ 只给自检）----
        //  补它的理由：命中区与实绘**可能不是同一份数据**（命中区一度是硬写的 px 矩形，而画出来的是
        //  `_bar` / `_circle` 两颗 quad，宽曾经由**贴图比例**定）⇒ 没有实绘口就**无从比**「命中区 ⊇ 实绘」。
        //  ⚠️ **2026-10-18 更正（铁律 5）**：本行原写「⛔ 本件**不改** `HitContinue` 的口径
        //  （改命中＝改行为，要另开一件、另配断言）」—— 那一句**已被 `A1061` 兑现**：
        //  口径**改了**（按订正后的原版判据重算，见 `HitContinue` 的注释），
        //  配套断言 = `Editor/BattleScene.cs` 的 `A964/E2④` 那两条**口径判别式**（`A1061①②`）
        //  + 两条原有的（内侧中 / 外侧不中）。
        //  🔴 **2026-10-16（修波 `FB` · 账 `F11`）再进一步**：命中区**不再**用那几个 px 常量拼，而是
        //  直接读**条自己的 `RectTransform`**（`HudCanvas.SizePx` + `InverseTransformPoint`）⇒
        //  「判到哪」与「画到哪」**同源**（那几个常量现在只剩「条不在画布上」那一支在用）。

        /// <summary>「继续」横条的**实绘**世界中心（= 那颗 quad 的中心）。</summary>
        public Vector3 ContinueBarWorldPos
        {
            get { return _bar != null ? _bar.transform.position : Vector3.zero; }
        }

        /// <summary>「继续」横条**实绘**的 px 尺寸。🔴 **2026-10-16（`T09` 批 3）换了量法** ——
        /// 原来量的是「`ImageQuad.WorldW/H` × 108」（**世界尺寸**），那条路在画布件上**会偏 0.75 倍**：
        /// 画布把 `屏/scaleFactor` 个画布单位摊到 `VisibleWidth × DesignHeight` 个世界单位上，
        /// **两个轴的系数可以不同**（判据 → `T09` §七·8，实测 `FrameWorldH × 108` 读出 47.88 而真值 63.84）。
        /// ✅ 现在读 **`HudCanvas.SizePx`（= `RectTransform.sizeDelta`，单位就是设计 px）** —— 不含任何
        /// 分辨率换算、逐位等于原版 `m_SizeDelta`（条 **577.50 × 63.84**）。
        /// ⚠️ 断言那一侧：`Editor/BattleScene.cs` `E2④` 原来把这两个数当「画出来的 px」用（配
        /// `PxPerUnit964` 折成世界偏移），换口径后它们**更精确了**，那两条的取值不用改。</summary>
        public Vector2 ContinueBarDrawnPx
        {
            get { return _bar != null && _bar.Rect != null ? HudCanvas.SizePx(_bar.Rect) : Vector2.zero; }
        }

        /// <summary>圆钮的**实绘**世界中心。</summary>
        public Vector3 ContinueCircleWorldPos
        {
            get { return _circle != null ? _circle.transform.position : Vector3.zero; }
        }

        /// <summary>圆钮**实绘**的 px 尺寸（读数口径同 <see cref="ContinueBarDrawnPx"/>；原版 **80.47 × 79.64**）。</summary>
        public Vector2 ContinueCircleDrawnPx
        {
            get { return _circle != null && _circle.Rect != null ? HudCanvas.SizePx(_circle.Rect) : Vector2.zero; }
        }

        /// <summary>点在我方牌堆那一块没有（**入口是我们挑的**，见文件头 ⑤）。</summary>
        public static bool HitDeckPile(Vector3 world)
        {
            return BattleDriver.HitMyDeckPile(world);
        }

        /// <summary>px（**左上原点**）→ **世界坐标** —— 只给**卡片那一支**用（它们在世界壳上）。
        /// 🔴 **2026-10-16（`T09` 批 3）**：画布那一组（标题/条/圆钮/字/提示）**不再走它**，
        /// 走 `HudCanvas.PosPx`（设计 px、相对画布本体的中心）；本函数**保留**给卡片
        /// （`CardView.SetPose` 收世界坐标）。</summary>
        static Vector3 Pos(float px, float py, float z) { return EndPanel.Pos(px, py, z); }

        // ---- 🔴 **2026-10-16（`T09` 批 3）：本文件那个私有 `U(px) = px / 108` 助手【已删】** ----
        //  它当年存在的唯一理由是「`ImageQuad.Create` 的第 4 参要**世界高度**」——
        //  而本件里**只剩 `_mask` 还在世界空间**，它的高给的是 `LayoutSpace.DesignHeight * 1.2f`
        //  （本来就是世界单位，不经 px）；**其余五件全在画布上、尺寸一律直接给设计 px**。
        //  ⇒ 留着它就是留一个「喂进去会缩成 1/108、且**不报错**」的口（`T09` §五·补⑩ 那道硬关卡一）。
        //  ⚠️ 兄弟件的同款助手仍各自留着（`Battle/CardChoicePanel.cs` 的 `U` · `Battle/MulliganPanel.cs`
        //     的 `U` · `Battle/EndPanel.cs` 那个私有的）—— 它们各自还有世界档的调用点，**别一起删**。

        // ── 🆕 2026-10-20（W57 · `T09` A3-e2）：uGUI 输入口的 Editor-only 只读探针 ─────────────────
        //  ⚠️ 只读、且**播放器构建里不存在**（`#if UNITY_EDITOR`）。断言写在 `Editor/BattleScene.cs`
        //     （本波口径 = 只做不验 ⇒ 本件不写断言）。
        //  ⚠️ 期望值（断言侧独立写死，⛔ 别读我们自己的常量）：Continue 条 `RT=1` · pad `(0,−40,0,−40)`；
        //     背景关闭件 `RT=1`；Header TMP `RT=1`（判据 → 文件头「点击/关闭」那段）。
#if UNITY_EDITOR
        /// <summary>「继续」横条那一件（`null` = 没建出来）。Editor-only 只读探针。</summary>
        public HudPiece ContinuePieceForTest { get { return _bar; } }
        /// <summary>「继续」条上的 uGUI 点击器（`null` = 没挂上）。</summary>
        public BattleHudButton ContinueButtonForTest { get { return _continueBtn; } }
        /// <summary>`_bar` 的 `Image.raycastTarget`（原版 = 1；读不到 ⇒ false）。</summary>
        public bool ContinueRaycastTargetForTest { get { return RaycastTargetOf(_bar); } }
        /// <summary>`_bar` 的 `Image.raycastPadding`（原版 `(0,−40,0,−40)`；读不到 ⇒ zero）。</summary>
        public Vector4 ContinueRaycastPaddingForTest { get { return RaycastPaddingOf(_bar); } }

        /// <summary>背景关闭件（全屏透明、uGUI 射线靶；原版的 `BackgroundCloseButton` 承载）。`null` = 没建出来。</summary>
        public HudPiece BackgroundClosePieceForTest { get { return _bgClose; } }
        /// <summary>背景关闭件上的点击器（`null` = 没挂上）。</summary>
        public BattleHudButton BackgroundCloseButtonForTest { get { return _bgCloseBtn; } }
        /// <summary>背景关闭件的 `Image.raycastTarget`（原版 = 1）。</summary>
        public bool BackgroundCloseRaycastTargetForTest { get { return RaycastTargetOf(_bgClose); } }

        /// <summary>🆕 2026-10-20（`T09-FIX3` · P1-3）：有没有「挂起的关闭」还没落（真机由 `LateUpdate` 落）。</summary>
        public bool ClosePendingForTest { get { return _closePending; } }
        /// <summary>uGUI 关闭被**请求**了几次（点一次 = 1；`Hide()` 直接调不走这里）。</summary>
        public int UguiCloseClickCountForTest { get { return _uguiCloseClicks; } }
        /// <summary>挂起的关闭**真落账**了几次（灭自证：点一次 = 1，⛔ 不是「点了就关」）。</summary>
        public int UguiCloseAppliedCountForTest { get { return _uguiCloseApplied; } }
        /// <summary>🆕 `T09-FIX3` P1-3：**批处理**落账口（那里没有帧循环 ⇒ `LateUpdate` 不跑）。</summary>
        public bool ApplyPendingCloseForTest() { return ApplyPendingClose(); }

        /// <summary>标题 TMP 的 `raycastTarget`（原版 = 1；TMP 取不到 ⇒ false）。</summary>
        public bool HeaderRaycastTargetForTest
        {
            get
            {
                var t = _header != null ? _header.GetComponentInChildren<TMP_Text>(true) : null;
                return t != null && t.raycastTarget;
            }
        }

        static bool RaycastTargetOf(HudPiece p)
        {
            var img = p != null && p.IsUgui && p.gameObject != null ? p.gameObject.GetComponent<Image>() : null;
            return img != null && img.raycastTarget;
        }
        static Vector4 RaycastPaddingOf(HudPiece p)
        {
            var img = p != null && p.IsUgui && p.gameObject != null ? p.gameObject.GetComponent<Image>() : null;
            return img != null ? img.raycastPadding : Vector4.zero;
        }
#endif

        static void Kill(GameObject o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}
