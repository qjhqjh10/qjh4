// SkillPanel.cs — 原版的「主动技能卡面板」（`ActiveSkillDesc`）
//
// 原版选完**主动技能**之后会弹一张卡，写清楚这个技能是什么、还能选几个目标。
// 我们以前把技能效果塞在选择器上方那行小字里（`AttackSelector` 的 `note`），现在按原版搬到这里。
//
// 出处：解包场景 `07_场景/battlearena1/`（**UI/3D 的东西不在 UI dump 里**）。
//
// ⚠️ **位置别照着 anchor 写。** 原版 RT 2911 的 anchor 是 x 0.6656–0.9658 / y 0–0.304，
//    看着在「右下」，但 `m_AnchoredPosition = (-606.11, 55.0)` 把它**拉回了水平正中**：
//      pivot 世界位 = (anchorMin + Δanchor×pivot) × 父尺寸 + anchoredPosition
//                   = (0.8156827×1920, 0.1520065×1080) + (-606.11, 55.0) = (960.0, 219.17)
//      尺寸        = (0.300212×1920, 0.304013×1080) + sizeDelta(-0.00037, -3.51) = 576.41 × 324.82
//    → 屏幕矩形 x **671.8–1248.2**、y **56.8–381.6**（1920×1080），即**底部居中**、不是右下。
//    交接文档第七节原来写的「右下 30%×30%」是错的（只看了一个字段）。
//
// 换算链（每一步都是 ×1.0，已逐级核过）：CanvasScaler `ScaleWithScreenSize` +
// `MatchWidthOrHeight=0` + 参考分辨率 1920×1080 → `scaleFactor = 屏宽/1920`；
// Canvas / Safe area FrontCanvas / FrontCanvas / BattleHud / BattlePrefab / ActiveSkillDesc
// 各级 `m_LocalScale` 全 1 → **1920 下 1 UGUI 单位 = 1 px**。
//
// 其它从原件量到的：
//   · `fadeTime = 0.2`，**纯代码插值、没有 Animator**（走 `CardTween` 的推进模式，
//     批处理自检里 `Step()` 才推得动）。`FadeOut` 用的是 `fadeTime × 0.5`（见 `FadeOutFactor` 的 doc）
//   · 四个文本（原版默认值 / 字号）：`NameText`(`"Fire Arrow"`/42.35)、`CostText`(`"10"`/52.4)、
//     `DescText`(37.35)、`TargetsAvailableText`(`"0 available"`/56.45)。
//     `DescText` **左对齐**（`m_margin.x = 7.4727 px`），其余居中。
//     四个都开着自动缩放 `[4,60]` —— **序列化字号是编辑器烘的「上次结果」**，
//     运行时会按各自矩形重算。这里走 `FitBox`（`Label.SetAutoFitBox`）开同一套自适应。
//   · `Lights` = **三层全铺的色片**（三个子节点 anchor 0,0–1,1 完全重叠），不是一排小灯
//   · `HideAbilityButton` = 一块**远大于面板的透明点击捕获区**（`m_Color.a = 0`、
//     `m_Type = Sliced`、`RaycastTarget = 1`），点它 → 面板收起。
//     我们这边等价的行为是「点面板外的任意地方 → `ClearSelection()` → 面板收起」
//
// ⚠️ **一处是我们挑的、不是原版的做法**：**底板**。
//    原版 `AbilityContainer` 上那个 Image **没有 sprite、颜色是纯白** ——
//    照搬会渲成一块白底压着白字，所以这里用深色板。
//    🔴 **2026-10-24（执行代理 `E17`）就地订正（铁律 5）**：本行原来接一句
//    「（多半靠运行时别的机制压暗）」—— 那句**猜测已降级**：原版这一整棵子树里**没有任何压暗组件**
//    （13 个 arena 逐份核过），`SetAlpha`/`FadePower` 是**唯一**的颜色通道；「白底白字」的三种可能
//    （序列化≠运行时真值 / 这个 build 根本不显示 / 另有不在本子树里的机制）**静态判据分不出来**。
//    判据 → `资料/普查产出_1024b/C4_战斗表现判据.md` §1·3。⛔ 别再把「多半靠运行时压暗」写成结论。
// ═════════════════════════════════════════════════════════════════════════════
//  🔴 **2026-10-16（`T09` 批 3 · `OverHand` 组）：图/字整批从【世界空间 quad】迁到【uGUI 子画布】**
//
//  · **原版宿主**（判据 = `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一 我这一行）：
//    `ActiveSkillDesc` @ `BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas`
//    —— **继承 `FrontCanvas`(rm 1) · `OverHand` 层 · order 0 · 兄弟序 1/14**（出处 `RT_3104` ·
//    `RT_3055.m_Children`）。⇒ 我们落成 `HudCanvas.EnsureSubCanvas(null, SubCanvasName, OverHand, 0)`。
//  · 🔴 **双模式（主对话裁定的硬要求）**：`UseHost(host)` **传了** ⇒ 画布档；**不传**（出厂）
//    ⇒ **世界档 —— 与改之前逐位相同**。先例 = `Tooltip.UseHost(host)` / `ErrorMessageBanner.Create(…, uguiHost)`。
//    ⚠️ 宿主由 **`BattleDriver`** 在建 HUD 时登记（组件是**场景挂的**，不像别的面板有 `Create`）
//    ⇒ 那一行在**白名单外**，见报告 §三 接线清单。**没登记 = 世界档**（⛔ 不是半迁移态）。
//  · **坐标**：两档共用同一组**面板内归一化**常量（`BgRect`/`NameRect`/…）——
//    世界档走 `InPanel`（世界单位）、画布档走 `CvPos`（设计 px，见它们各自的 doc）。
//    两档在 **16:9 下逐位相同**（画布那一侧的推导 → `Battle/HudCanvas.cs` 文件头）。
//  · **退场的口**（逐项 → `T09` §五·补④ 那张映射表 ＋ §五·补⑩ 关卡 1）：
//      `ImageQuad.Create(...)` → **`HudCanvas.MakeImageInParent(...)`**（⚠️ **中心锚**，**不是**
//        `MakeImage`/`QuadFromTopPx` 那套**归一化锚** —— 子画布 rect 在 `Expand` 档下比 1920×1080 高，
//        套归一化锚会把件**静默摆到别处**）；`Label.Create(...)` → **`HudCanvas.MakeText(...)`**（**设计 px**）；
//      `localPosition += (0,0,Z)` → 建件口那个 `z` 形参（画布上 z **只用来排兄弟序**，写进
//        `localPosition` 会把件推离画布平面）。**没有** `MenuDraw.SetPxSize` / 整屏覆盖层 ⇒ 用不上
//        `HudCanvas.SetPxSize` / `FillParent`（本面板的根**不是一个整屏矩形** —— 它在世界档里只有
//        「一个点 + 一组手算尺寸」，件才是载体）。
//  · 🔴🔴 **2026-10-16（`T09` 批 3 · 主对话裁定「按原版锚点补正」）：面板那一级从【固定设计 px】
//    改成【照原版锚点】** —— 见 `CvAnchorMinX` 那一组常量与 `PanelCvRect`。
//    判据 = **`T09` §五·补③ 一 的 2026-10-16 就地订正**（⛔ 不是旧那版「px 常量一律固定」）：
//    **「面板那一级与它的子件，锚点要照原版 `m_AnchorMin`/`m_AnchorMax` 逐级写；只有当原版那一级
//    是【双轴点锚】时，才等价于『固定设计 px』。」** 原版 `ActiveSkillDesc` 是**双轴部分拉伸**
//    （宽 30.02% × 高 30.40%）⇒ 必须真按锚点走，否则窄屏（画布被 `Expand` 撑高）下面板**不跟着变高**
//    （4:3 下只有原版的 **≈75%**）。
//  · 🔴🔴 **2026-10-20（W58 · `T09` 步骤 C4）：那一级现在是【真的节点】= `_panelRt`（`MakePanelNode`）** ——
//    双轴拉伸锚 + `pivot/ap/sd` 逐值照抄原版 `RT_3104`，**父画布一拉伸它自己就跟着变**；
//    `PanelCvRect` 读它（实测支 = Unity 的 `rect`，退档支 = 它自己的锚点字段 + `CanvasPx`），
//    ⛔ 不再用「我们那组常量 × 画布尺寸」现算第二遍（来源只有一份）。
//  · ⚠️ **如实记着的一条差异**（按 `T09` §五·补⑩ 二「差异如实记」）：
//    ① **件不挂在 `ActiveSkillDesc` 下面**：原版是「父画布 → `ActiveSkillDesc` → 7 个件」；
//       我们画布档是「子画布 → **`ActiveSkillDesc`（只承载几何、不装件）** + 7 个平级件」。
//       ⇒ 几何那一半（面板矩形/随父拉伸）已经与原版同构；**嵌套那一半仍不同**（⛔ 倒嵌套会动
//       4 颗自适应字吃的框宽/框高，另开一件再谈）。
//    ② ✅ **原来这里记的那条「尺寸口径 = 固定设计 px」已按上面的裁定做掉** —— 不再欠。
//  · ⚠️ **两档在 16:9 下逐位相同 —— 这句话 2026-10-16 一度只对 6 颗件成立，现已恢复为「对全部 7 颗」
//    都成立**：`_light`（原版 `Lights`）那格曾按「画布档 = 原版锚点盒 / 世界档 = `A2106③` 的冻结值
//    `(0,0,1,1)`」分档（那是两条互相咬着的要求的折中），**主对话当天裁定「按原版」⇒ 冻结值已删、
//    两档统一读 `LightRect`（原版锚点盒）**。⇒ 世界档在这一件上是**往原版方向修缺陷**
//    （原来多铺 6.78% 宽 / 15.32% 高），⛔ 不是回归；其余 6 颗件的位/尺寸两档**逐值相同**
//    （差 ≤ 0.02 px：残差全部来自 `PanelCvRect` 现算（锚点全精度）与旧那对 2 位小数常量 `PanelPxW/H` 的舍入）。
//  · ⚠️ **本件没有「整屏压暗层」**（`T09` §八·5 那个选择题在这扇上**不成立**）：原版 `HideAbilityButton`
//    是一块**远大于面板的透明点击捕获区**，我们等价的语义是「点面板外 ⇒ `ClearSelection()` ⇒ 收起」
//    （由 `BattleDriver` 那条通用路走，**不在这扇面板里**）。
//    🔴 **2026-10-20（W57 · `T09` 步骤 A3-e）就地订正（铁律 5）**：本行原来写「本扇**零射线件**，
//    `raycastTarget` 一条都不用改」—— **已过期**：那一天只做到「图与字迁画布」，**输入那一档押后**；
//    A3-e 已按原版把 `AbilityContainer` + `HideAbilityButton` 两口补上（判据与缺口 → 下一段）。
//
// ─────────────────────────────────────────────────────────────────────────────────────────────
//  🔴 **2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e）：输入层换 uGUI**（判据 = 原版 prefab 逐组件现读，
//   ⛔ 不是照迁移计划抄；⛔ 手搓路一个字没动）
//
//  · **原版面板那一级的组件**（`bundle_scenes_scenes_battlearena1` 与 `…arena2` 逐值相同，2026-10-20 现读）：
//    `ActiveSkillDesc`（GO 673）**只有** `RectTransform`(2911) + `CanvasRenderer`(2218) +
//    `ActiveSkillDisplay`(4368) —— **零 `Image`、零 `Button`** ⇒ 面板根自己不吃射线。
//  · **技能点击 / 按住 / 悬停 / 拖拽 = `AbilityContainer` 上的 `Image` + `EventTrigger`**
//    （GO `AbilityContainer` 组件 3128/2258/**4864**/**5084**）：
//      · `Image`（`MonoBehaviour_4864`）：`m_RaycastTarget = 1` · `m_RaycastPadding = (0,0,0,0)` ·
//        `m_Sprite = null`（纯白）· `m_Type = 0`（Simple）· `m_PreserveAspect = 0`
//      · `EventTrigger`（`MonoBehaviour_5084.m_Delegates`，**5 条，逐条照抄**）：
//        `eventID 2 PointerDown → ActiveSkillDisplay.ClickDown` ·
//        `eventID 3 PointerUp   → ClickUp` · `eventID 5 Drag → Drag` ·
//        `eventID 1 PointerExit → PointerExit` · `eventID 0 PointerEnter → **PointerExit**`（原版就这么接的）
//      · 反编译语义（`dump.cs` 字段偏移 × `decomp_full` 逐个方法体）：
//        `ClickDown` → `pressingLight`(0x40) `SetActive(true)` + 播 `AbilityBoxClick`；
//        `ClickUp` → `OnTouchUpAsButton` → 先熄 `pressingLight`，再 `BattleManager.TryUsingActiveAbility()`
//        （**面板本体 = 释放技能的那颗钮**）；`Drag` → `OnTouchDrag`（拖到合法目标也走释放）；
//        `PointerExit` → `pressingLight` `SetActive(false)`。
//  · **关闭 = `HideAbilityButton` 上的 `Image` + `EventTrigger{ PointerDown → StopDisplayingSkillFromManager }`**
//    （GO `HideAbilityButton` 组件 2860/**4207**/**4854**）：
//      · `Image`：`m_RaycastTarget = 1` · pad `(0,0,0,0)` · `m_Color.a = 0` · `m_Type = 1`（Sliced）·
//        `m_PreserveAspect = 0` · sprite PathID `1660267235368898380`
//      · `RectTransform`(2860)：`aMin = aMax = (1,0)` · `pivot = (1,0)` · `sd = (3707.830078125, 1985.3199462890625)` ·
//        `ap = (220.3900146484375, −146.0101776123047)` ⇒ **远大于屏幕的捕获区**（1920×1080 下 x[−1567.4, 2140.4]、
//        y[−146.0, 1839.3]），**按底右锚定** ⇒ 别的宽高比下仍铺满。
//      · 反编译：`StopDisplayingSkillFromManager` → `BattleManager.StopDisplayingActiveAbility` →
//        `ActiveSkillDisplay.FadeOut()` + `CardScript.StopDisplayingActiveAbility()`（= `CardHighlight.ChangeState(0)`）。
//  · 🔴 **我们这一侧怎么落**（`BuildInput` / `PlaceHideButton` / `SetInputEnabled`）：
//    `_bg` 挂**同形 `EventTrigger` + 同 5 个 eventID**；新建 `_hideBtn`（全屏透明）+ **同形 PointerDown**。
//    动作按「谁的状态归谁」拆：**pressingLight 是面板自己的** ⇒ `PointerDown/Up/Enter/Exit` 直接写 `_pressed`；
//    **释放技能 / 清选择是 `BattleDriver` 的** ⇒ 只发事件（`AbilityPressed/Clicked/Dragged` / `CloseRequested`）。
//    🔴 **2026-10-20（W57 `T09` WIRE）就地订正**：本行原写「驱动那一侧**没有订阅者**」—— **已过期**：
//    `BattleDriver.BuildHud` 已订阅 `AbilityClicked→SkillPanelConfirmRelease` /
//    `AbilityDragged→SkillPanelDragToTarget` / `CloseRequested→OnSkillPanelCloseRequested`。
//    ⚠️ **关闭那一条是例外**：原版 `FadeOut` 就是面板自己的事 ⇒ `OnHidePointerDown` **当场 `Hide()`**
//    （+ 发 `CloseRequested` 给驱动补 `ClearSelection`）。同一次物理点击的世界侧那条路仍会
//    `ClearSelection()`（`BattleDriver` 不经过 EventSystem）⇒ 状态照样收干净。
//    🔴 **2026-10-20（W57 · `T09-FIX3` · P0-1）**：捕获区按下时**先问宿主** `LeavePointerToWorld(屏幕点)` ——
//    点在**任何棋盘卡**上（目标 / 自己人 / 非目标 —— `T09-FIX5` 起按原版证据放宽）⇒ **不关面板、不发 `CloseRequested`**，
//    把这一下留给世界侧；原版等价物 = 棋盘卡自己的 `CardCollider` 先吃掉这一下（判据 → `LeavePointerToWorld` 的 doc）。
//  · ⚠️ **如实记的三条差异**（⛔ 本波没做、要等驱动侧接线）：
//    ① **「点面板 = 释放技能」只有事件、没有动作**（原版 `TryUsingActiveAbility`；我们驱动还没订阅 `AbilityClicked`）；
//    ② **拖拽释放**同理（原版 `OnTouchDrag` → `StartTargetedActiveAbility`）；
//    ③ **按下音效** `AbilityBoxClick`：本工程音效表里**查不到这个 cue**（`grep -rn AbilityBox` 全仓 0 命中，
//       `SoundAssetCollection.get_AbilityBoxClick` 的资产也不在本地 91 个包里）⇒ ⛔ 不接、不猜一个近似的。
//  · ⚠️ **两条结构性差异**（我们保持扁平层级、⛔ 不为此改框/加中间层 —— 那样会动 4 颗自适应字吃的框宽）：
//    🔴 **2026-10-20（W58 · `T09` C4 / `HideAbilityButton` 锚点补正）就地补注**：本件现在**有一颗 `ActiveSkillDesc` 节点**，
//    它承载几何（`PanelCvRect` 读它的锚点）+ **原版那一颗 `HideAbilityButton`**（父链照原版）；
//    **其余 7 件仍是子画布的平级子件** ⇒ 下面两条差异**照旧成立**
//    （⛔ 别把「有了同名节点」读成「嵌套已经倒过来了」）。
//    ① 原版 `NameText`/`DescText` 是 `AbilityContainer` 的**子件**（TMP `m_RaycastTarget = 1` ⇒ 点击沿父链冒泡到
//       容器那颗 `EventTrigger`）；我们三颗字是子画布的**平级子件**（`TmpFont.NewTextUgui` 默认 rt=0）。
//       等效性：`NameRect`/`DescRect` 的整块都落在 `BgRect` 内（逐格核过）⇒ 点在字上时射线直接落到 `_bg` 上，
//       **结果相同**（原版靠冒泡、我们靠下落）。
//    ② 原版 `TargetsAvailableText` 也是 `rt = 1`、但它的父链上**没有任何 handler**（`ActiveSkillDesc` 零组件）
//       ⇒ 那一小条在原版里是「**吃点击但什么都不做**」。我们为同构把 `_targets` 的 TMP `raycastTarget` 也置 true
//       （面板收起时必须关掉，见 `SetInputEnabled`）。
//  · ⚠️ **`Lights` 三层色片原版 `rt = 0`**（逐个核过）⇒ 我们的 `_light` 继续 `raycastTarget = false`，⛔ 别打开
//    （它盖在字上面，打开会把面板本体的点击全吞掉）。
// ─────────────────────────────────────────────────────────────────────────────────────────────
// ═════════════════════════════════════════════════════════════════════════════
using DG.Tweening;
using RuleEngine;
using UnityEngine;
using UnityEngine.EventSystems;   // 🆕 2026-10-20（`T09` A3-e）：`EventTrigger` / `BaseEventData` / `EventTriggerType`

namespace CardPresentation
{
    public class SkillPanel : MonoBehaviour
    {
        // ---- 面板在屏幕上的位置（归一化，y 从**底部**算，和 `LayoutSpace.ToWorld` 一致）----
        // 671.8 / 1248.2 ÷ 1920、56.8 / 381.6 ÷ 1080（算式见文件头）
        // ⚠️ **这是【世界档】的尺**，也是**16:9 下画布档锚点算出来的那一格**（自证 → `PanelPxW` 的 doc）。
        const float PanelX0 = 0.3499f, PanelX1 = 0.6501f;
        const float PanelY0 = 0.0526f, PanelY1 = 0.3533f;
        /// <summary>原版面板在**参考分辨率（1920×1080）**下的像素尺寸 —— 子节点矩形是按它折算的。
        /// 🔴 **2026-10-16（`T09` 批 3 · 锚点补正）**：它现在**只剩两个用途**：
        /// ① **归一化分母**（`DescMarginPx / PanelPxW` 那种「面板内比例」）；② **自证基准**
        /// （`PanelCvRect` 在 1920×1080 下必须算出这一对数 —— 算式见 §⑤ 报告）。
        /// ⛔ **画布档的尺不再是它**——是 `PanelCvRect` 从锚点节点读出来的 `size`（跟着画布高走）；
        /// 世界档的尺是 `PanelW/PanelH`（= 屏幕比例 × 30%，见它们）。两档只在这一处换算
        /// （`BoxW/BoxH`）—— ⛔ 别在调用点自己乘。</summary>
        const float PanelPxW = 576.41f, PanelPxH = 324.82f;

        // ── 🔴 2026-10-16（`T09` 批 3 · 主对话裁定「按原版锚点补正」）：面板那一级的**锚点五元组** ──
        //  出处 = **原版 `ActiveSkillDesc`（`RT_3104`）**，`bundle_scenes_scenes_battlearena2/
        //  RectTransform/RectTransform_3104.json` **逐字段现读**（2026-10-16）：
        //    `m_AnchorMin (0.665576696395874, 0.0)` · `m_AnchorMax (0.965788722038269, 0.3040129840373993)`
        //    `m_Pivot (0.5, 0.5)` · `m_AnchoredPosition (-606.110107421875, 55.0008544921875)`
        //    `m_SizeDelta (-0.0003662100061774254, -3.509999990463257)` · `m_LocalScale (1,1,1)`
        //  ⇒ **两轴都拉伸**（x 占画布宽 **30.02%** · y 占画布高 **30.40%**）。
        //  🔑 **`_cv`（子画布）就是原版那一级的父**（`Safe area FrontCanvas`）：它四边贴合根画布
        //     ⇒ `rect.min = −画布尺寸/2`、`rect` 的宽高 = 画布的设计 px 尺寸。公式见 `PanelCvRect`。
        //  ⛔ **逐值照抄、⛔ 别改数**（这是原版真值；改一个数 = 面板当场偏）。
        //  🔴 **2026-10-20（W58 · `T09` C4）**：这组常量现在**只用于【建】`_panelRt`**
        //     （`MakePanelNode` 把它们写进节点）；**运行时判据是那个节点**（`PanelCvRect` 读它，
        //     ⛔ 不再拿这组常量 × 画布尺寸现算第二遍 —— 那样「来源」就有两份）。
        //     ⇒ 改这里的数 = 同时改节点；改断言期望值请走**原版 JSON 字面量**（⛔ 别读常量反推）。
        const float CvAnchorMinX = 0.665576696395874f;
        const float CvAnchorMinY = 0f;
        const float CvAnchorMaxX = 0.965788722038269f;
        const float CvAnchorMaxY = 0.3040129840373993f;
        const float CvPivotX = 0.5f, CvPivotY = 0.5f;
        const float CvAnchoredX = -606.110107421875f;
        const float CvAnchoredY = 55.0008544921875f;
        const float CvSizeDeltaX = -0.0003662100061774254f;
        const float CvSizeDeltaY = -3.509999990463257f;

        /// <summary>面板所在的 z —— 越负越靠前。压在 HUD（z=0）前面、选择器（-2.0）后面。
        /// ⚠️ 画布档下它**只喂给那个世界空间的壳** `transform`（`BattleDriver` 拿壳的位置当
        /// 「指针按在面板上」那个点）—— 画布上的件**不读**它（z 只用来排兄弟序，见下一行）。</summary>
        const float ZPanel = -1.80f;
        /// <summary>面板内的 z 分层（相对面板）。色片要**盖在字上面** —— 原版就是这么叠的。
        /// 🔴 **画布档下这三个数只用来登记兄弟序**（`HudCanvas.Add`：**z 大的排前面 = 画在底下**，
        /// 语义与旧那套**同向**）⇒ `ZLight`(−0.02) 最小 ⇒ 排在最后 = **画在最上面** ✓、`ZBg`(+0.02) 最大
        /// ⇒ 画在最底下 ✓ —— 与旧那套「`_light` 最靠前、`_bg` 最靠后」逐件一致。
        /// ⛔ **别把它们写进画布件的 `localPosition.z`**（那会把件推离画布平面）。</summary>
        const float ZBg = 0.02f, ZText = 0f, ZLight = -0.02f;
        /// <summary>🆕 **2026-10-20（`T09` A3-e）：原版 `HideAbilityButton` 那块全屏透明捕获区的兄弟序**。
        /// **比 `_bg` 再大 0.02** ⇒ 排在它**后面**（`HudCanvas.Group.Add`：z 大的排前面 = 画在**底下**）。
        /// 🔴 **2026-10-20（W58）**：捕获区现在挂在**面板节点** `_panelRt` 下 ⇒ 真正决定它在子画布里
        /// 前后的是 **`ZPanelNode`**（见下面那颗）；本值现在只是它**在面板节点内部**的兄弟序（单子件，
        /// 无实际作用，保留以免 `MakeImageInParent` 的 z 语义变空）。
        /// 判据不变：捕获区必须被面板本体盖住（射线先给 `_bg`，面板外才轮到它）。</summary>
        const float ZHideBtn = ZBg + 0.02f;

        /// <summary>🆕 **2026-10-20（W58）：面板锚点节点在子画布里的兄弟序** —— **比所有件都靠前（画在最底下）**。
        /// <para>🔑 **为什么必须登记**：`_panelRt` 现在是 `HideAbilityButton` 的父 —— 不登记的话
        /// `HudCanvas.Group.Add` 每次给平级件 `SetSiblingIndex(i)` 都会把这个「没进账」的节点往**后**挤，
        /// 它会漂到最上层 ⇒ 整块透明捕获区盖住面板，**点面板本体先命中捕获区（面板当场关掉）**。
        /// 取 `ZHideBtn + 0.02` ⇒ 面板子树（含捕获区）永远排在 `_bg` 之前 = 画在底下、射线最后收。</para></summary>
        const float ZPanelNode = ZHideBtn + 0.02f;

        // ── 🆕 2026-10-20（`T09` A3-e）：原版 `HideAbilityButton`（arena1 `RT_2860` / arena2 `RT_3053`）──
        //  逐字段现读（13 个 `battlearena*` 包**逐值相同**；判据 =
        //  `bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_3053.json` +
        //  `…/GameObject/HideAbilityButton.json`，2026-10-20 现读）：
        //    `aMin = aMax = (1, 0)` · `pivot = (1, 0)` · `m_AnchoredPosition = (220.3900146484375, −146.0101776123047)`
        //    `m_SizeDelta = (3707.830078125, 1985.3199462890625)` · `m_LocalScale = (1,1,1)`
        //  🔴 **2026-10-20（W58 · `HideAbilityButton` 锚点补正）就地订正（铁律 5）**：
        //    本段原写「锚在**画布**右下角外」—— **错**。原版 `RT_3053.m_Father = 3104` =
        //    **`ActiveSkillDesc` 面板节点**（运行时 dump 的 path 也把它放在 `…/Safe area FrontCanvas/ActiveSkillDesc`
        //    之下）⇒ 这五元组是**相对面板**的：16:9 下面积 = 面板右下角（画布坐标 x≈1248.2 / y≈56.8）
        //    + ap ⇒ 矩形向左上铺开 3707.83 × 1985.32。
        //  ⛔ 别把 `sd` 改成「铺满画布」（`FillParent`）：原版底右锚定 + 固定尺寸在 21:9 / 4:3 下
        //    覆盖形状与 16:9 不同（y 轴在 16:9 下从 −89.3 到 1896.1，覆盖整屏且有余），改锚定会改变它。
        //  ⚠️ 如实记：按面板右下角锚定后，x 轴**右边缘**在 16:9 下到 1468.6（屏幕右界 1920）——
        //    **右侧约 451 px 不在捕获区内**。这就是原版自己的布局（⛔ 别为此擅自改 ap/sd）。
        const float HideBtnW = 3707.830078125f, HideBtnH = 1985.3199462890625f;
        const float HideBtnAnchoredX = 220.3900146484375f, HideBtnAnchoredY = -146.0101776123047f;

        /// <summary>本工程 1 世界单位 = 108 px（**世界档那一套尺**）—— 和 `AttackSelector` / `TargetReticle` 同口径。
        /// 🔴 画布档**不用它**（canvas 单位就是设计 px ⇒ 不除 108）—— 见 `BoxW/BoxH` / `MakeLabel`。</summary>
        const float PxPerUnit = 108f;
        static float WorldOfPx(float px) { return px / PxPerUnit; }

        // ---- 各子节点的矩形（**面板内**归一化：x 从左 0 到右 1，y 从**顶部** 0 到底部 1）----
        // 出处：`07_场景/battlearena1/RectTransform/`，按「pivot 世界位 = (anchorMin + Δanchor×pivot)
        // × 父尺寸 + anchoredPosition」逐级折算到面板坐标。7 个节点的 pivot 全是 (0.5,0.5)、
        // localScale 全是 1。
        // 🔴 **2026-10-16（`T09` 批 3 · 锚点补正）**：那一级现在是**锚点驱动**的（`PanelCvRect`）
        //   ⇒ 下面这些**面板内归一化矩形 = 原版子件的「锚点盒」**（`T09` §五·补③ 一 的口径：
        //   「面板那一级与它的子件，锚点要照原版 `m_AnchorMin`/`m_AnchorMax` 逐级写」）。
        //   4 个直接子件（`RT_3104.m_Children` = 3053/3322/3314/2818）里 **3 个是拉伸**，
        //   逐值（`bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_<pid>.json` 现读，
        //   `sd` 全 (0,0)、`ap` 全 (0,0)、`pivot` 全 (0.5,0.5)，只有 `TargetsAvailableText` 的
        //   `ap/sd` 是 1e−5 量级的浮点噪音 ⇒ 盒 = `aMin…aMax`）：
        //     · `AbilityContainer`(3322) `aMin (0.068690665, 0.083098173)` / `aMax (0.912609994, 0.764358878)`
        //       → 盒 = x[0.0686907, 0.9126100]、y(顶算)[0.2356411, 0.9169018] ⇒ **= `BgRect`**（差 ≤ 1e−4）
        //     · `TargetsAvailableText`(2818) `aMin (0.158, 0.764358878)` / `aMax (0.844548225, 0.929000020)`
        //       → 盒 = x[0.1580000, 0.8445482]、y(顶算)[0.0710000, 0.2356411] ⇒ **= `TargetsRect`** ✓
        //     · `Lights`(3314) `aMin (0.030818868, 0)` / `aMax (0.963017464, 0.846788108)`
        //       → 盒 = x[0.0308189, 0.9630175]、y(顶算)[0.1532119, 1.0] ⇒ **见 `LightRect`**（⚠️ 与旧值不同）
        //     · `HideAbilityButton`(3053) 是**点锚** ⇒ 我们**没建**（属 `T09` 第 4 步「输入」）
        //   ⚠️ 上面两颗（`BgRect`/`TargetsRect`）**保留 4 位小数的原值、不改成锚点的全精度** ——
        //     这是**有意**的：改全精度会让**世界档**的件动 ≤ 0.006 px（违「世界档逐位不变」那条硬要求），
        //     而 4 位小数的误差在画布档 ≤ 0.006 px（半像素以下，看不见）。判据 → 报告 §④·3。
        // ⚠️ 底板**不是铺满面板的** —— 它只占 84.4% × 68.1%，偏左上。
        static readonly Rect BgRect = new Rect(0.0687f, 0.2356f, 0.8439f, 0.6813f);
        static readonly Rect NameRect = new Rect(0.1004f, 0.2806f, 0.3295f, 0.2059f);
        // 🔴 **2026-10-10 更正（铁律 5 · `A1242①`）：这两颗原来归一化错了参考帧。**
        //    改前 `CostIconRect`/`CostTextRect` = (0.7409, 0.0829, 0.0837, 0.3057) /
        //    (0.8385, 0.0812, 0.1155, 0.3074)，而**上面那五颗**（`Bg` / `Name` / `Desc` / `Targets`）
        //    都是按**面板** `ActiveSkillDesc`(576.41×324.82) 归一化的 —— 那两颗却是按
        //    **`AbilityContainer`**(486.44×221.29，= 本文件 `BgRect` 那一颗) 归一化的
        //    （实据：`CostText` 的 x/w = `(1119.28−711.39)/486.44` = **0.83857**、`56.19/486.44` = **0.11549**，
        //     与改前那两个数**逐位相同**；`CostIcon` 的 x = `(1071.80−711.39)/486.44` = **0.74099** 同）。
        //    后果有两处，不是一处：① **位置**（`CenterOf` 摆的是错帧的心 ⇒ 屏上 x 1155.1 vs 原版 1119.3，差 35.8px）；
        //    ② **框**（`FitBox` 吃的宽高同源 ⇒ 66.58×99.85 vs 原版 56.20×68.40）。
        //    判据 = **现读**（`menu_dump.py bundle_scenes_scenes_battlearena1 "ActiveSkillDesc" --depth 4`，
        //    用 **`P-L` 修好之后**的尺子）：
        //      `CostText  1119.28,792.55→1175.47,860.95  56.20×68.40`（`'10'` 52.4 em，Center/Middle）
        //      `CostIcon  1071.80,792.92→1112.53,860.95  40.73×68.03`（无图 · `preserveAspect`）
        //    归一化（口径与那五颗**逐字相同**：x ÷ 面板宽、y 从**面板顶部**起算）：
        //      `(x−671.80)/576.41` · `(y−698.42)/324.82`
        //    ⚠️ `CostIcon` 自身在原版 prefab 里是 **`m_IsActive = 0`**（`menu_dump` 那一列的 `act=F`）——
        //      但它的**矩形是真的**，`_manaIcon` 就按它摆；改后 `min(w,h)` = 40.73 ⇒ 方图边长 = 原版图宽 ✓。
        static readonly Rect CostIconRect = new Rect(0.6939f, 0.2909f, 0.0707f, 0.2095f);
        static readonly Rect CostTextRect = new Rect(0.7763f, 0.2898f, 0.0975f, 0.2106f);
        static readonly Rect DescRect = new Rect(0.1009f, 0.5004f, 0.7140f, 0.3335f);
        /// <summary>= 原版 `TargetsAvailableText`(2818) 的锚点盒（见上面那一段逐值）。</summary>
        static readonly Rect TargetsRect = new Rect(0.1580f, 0.0710f, 0.6865f, 0.1646f);
        /// <summary>原版 `Lights`(RT **3314**) 那个**容器**的锚点盒 —— **两档都读它**（见下面那段更正痕）。
        /// 🔴🔴 **2026-10-16（`T09` 批 3）就地订正（铁律 5）—— `A2106③`（2026-10-15）改错了。**
        /// 它当时把这里改成 `(0, 0, 1, 1)`（全 stretch 面板），理由是
        /// 「原版三层是【全 stretch】…… 判据 → `C2`（引 `B2:74-76`）」。
        /// **错因 = 把 `Lights` 的【子件】当成了 `Lights` 自己**：
        ///   · `B2:74-76` 那三行写的是 `LightActing` / `LightAvailable` / `LightPressed` —— 它们是
        ///     `Lights` 的**三个子件**、各自 `anchor (0,0)-(1,1)`（相对 **`Lights`**，不是相对面板）；
        ///   · **`Lights` 自己那一行是 `B2:73`**，白纸黑字 `(0.031,0)/(0.963,0.847)` —— 与本节的值一致；
        ///   · `RT_3314` 现读：`aMin (0.030818868428468704, 0)` / `aMax (0.963017463684082, 0.8467881083488464)`
        ///     · `sd (0,0)` · `ap (0,0)` · `pivot (0.5,0.5)`。
        /// ⇒ 盒 = x[0.0308189, 0.9630175]、y(顶算)[0.1532119, 1.0]，**正是 `A2106③` 之前那个值**
        ///   （`(0.0308, 0.1532, 0.9322, 0.8468)`）—— 那次「订正」把一个**本来对的值改错了**
        ///   （6.8% 宽 / 15.3% 高的差，方向与它写的相反）。
        /// ⚠️ `Lights` 那个节点**自己没有任何 `Image`**（`B2:73` 组件列是 `—`），三层色片铺满它
        ///   ⇒ **画出来的范围 = 这个盒**（不可能更大）。</summary>
        static readonly Rect LightRect = new Rect(0.0308189f, 0.1532119f, 0.9321986f, 0.8467881f);
        /// <summary>⚠️ **2026-10-16（主对话裁定 · 本轮）**：这里**原来还有一颗 `LightRectWorldFrozen
        /// = (0,0,1,1)`**（「世界档冻结值」，`A2106③` 留下的），现**已删除 —— 两档统一读 `LightRect`**。
        /// <para>**为什么删**（裁定三条）：① `A2106③` 本身是把**一个本来对的值改错了**（它引的 `B2:74-76`
        /// 说的是 `Lights` 的三个**子件**，而 `Lights` 自己在 `B2:73` = `(0.031,0)/(0.963,0.847)`、
        /// 节点自身**没有 `Image`** ⇒ 画出来的范围就是那个盒）；② 「世界档逐位不变」那条规矩是
        /// **防回归**的、**不是冻结错误**的 —— 现状本身是缺陷（多铺 6.78% 宽 / 15.32% 高），
        /// 按**铁律 11**（与原版不符的全部要做）**必须改**；③ 同批先例 = `ChatPopupPanel` 世界档那 6 个
        /// z 常量的层序也是反的，裁定一模一样（**照原版改，⛔ 别拿「逐位相同」挡它**）。</para>
        /// <para>⇒ **世界档这一件也往原版方向变了**（从铺满面板 → 93.2%×84.7%）—— 这是**修缺陷**，
        /// ⛔ 不是回归。除此之外世界档仍然逐位不变（`B2` 那批常量与 `PanelW/H` 一个字没动）。</para></summary>

        /// <summary>原版 `DescText` 的 `m_margin.x = 7.4727 px` —— 左对齐的起笔位置</summary>
        const float DescMarginPx = 7.4727f;

        // ═══════════════════════════════════════════════════════════════════════════════════════════
        // ✅ **2026-10-19（`A1284` 收口）：7 颗件「逐级折算到面板坐标」已【逐颗回代】。**
        //   判据 = 原版绝对矩形（`python -I -X utf8 工具/menu_dump.py bundle_scenes_scenes_battlearena2
        //   "ActiveSkillDesc" --depth 6 --no-sprite` 现读，2026-10-19）+ 两条独立折算：
        //   `Name`/`Desc` 走 **`AbilityContainer`(RT3322) 的锚点盒**再折到面板，其余直接对面板。
        //   件       | 原版绝对矩形（1920×1080 · 左上原点） | 我们的常量 → 绝对（差）
        //   Bg       | 711.4, 775.0  486.44×221.29 | 711.40, 774.93  486.43×221.29（≤0.07px）
        //   Name     | 729.6, 789.6  189.95×66.88  | 729.67, 789.54  189.92×66.88（≤0.07px）
        //   CostIcon | 1071.8, 792.9 40.73×68.03   | 1071.74, 792.89 40.75×68.05（≤0.06px）
        //   CostText | 1119.3, 792.6 56.20×68.40   | 1119.27, 792.53 56.20×68.41（≤0.06px）
        //   Desc     | 730.0, 861.0  411.51×108.33 | 729.96, 860.94  411.56×108.33（≤0.06px）
        //   Targets  | 762.9, 721.5  395.73×53.48  | 762.87, 721.46  395.71×53.47（≤0.05px）
        //   Lights   | 689.6, 748.2  537.33×275.06 | 689.56, 748.17  537.33×275.06（≤0.04px）
        //   ⇒ **一个值都不用改**（残差全部来自 4 位小数舍入）。`NameRect` / `DescRect` 是**首次**回代；
        //   `Desc` 的 `m_margin.x = 7.4727` 另由 `MarginX01()` / `MarginW()` 在摆位与框宽里扣。
        //   ⛔ 断言在 `Editor/BattleScene.cs`，期望值取**上面那列原版绝对矩形**（不取我们的常量）。
        // ═══════════════════════════════════════════════════════════════════════════════════════════

        // 四个字号是 **em**（UGUI 的 fontSize 就是 em 大小），而我们的文案是**中文** ——
        // 一个汉字约占 1 em。所以按「汉字多高」定档（`Label.SetGlyphHeight`），
        // **不要**套 `SetCapHeight`（那是拉丁大写，只有约 0.72 em，会让字大 ~39%）。
        const float NamePx = 42.35f, CostPx = 52.4f, DescPx = 37.35f, TargetsPx = 56.45f;
        // 原版四个字的字体：Name/Cost/Desc 是 Asar-Regular SDF（PointSize 94 / CapLine 61），
        // Targets 是 Pragati-Regular SDF（95 / 60）。我们统一用中文那份 —— 反正要出汉字。

        // 🆕 **2026-10-10（A1212 块 7）**：原版那四颗 TMP 的自适应三格 —— **四颗逐值完全相同**。
        // 出处 = `资料/普查产出_第十会话/R6_包装层战斗与其余.md` §2·C（`:143/:144/:146/:147` 那四处）
        // 与 §3 的行 23–26：`bundle_scenes_scenes_battlearena1 ▸ FrontCanvas/Safe area FrontCanvas/
        // ActiveSkillDesc/AbilityContainer/{NameText,CostText,DescText,TargetsAvailableText}`
        // 四格 = `4 / 60 / 36 / 1`（`m_fontSizeMin 4` · `m_fontSizeMax 60` · `m_fontSizeBase 36` ·
        // `m_TextWrappingMode 1`）；归组 = `资料/普查产出_第十一会话/RA1212_切块表.md` 块 7。
        // ⚠️ **走 `Label.SetAutoFitBox` 那条路 ⇒ 没有 `MenuDraw.TextCore` 那三道闸**；但量纲要自己认：
        //   框宽/框高走**本档的尺寸单位**、`min/max/base` 走**原版画布 px**
        //   ⇒ 三个数**原样传、⛔ 不乘任何系数**。
        //   🔴 **2026-10-16（`T09` 批 3）就地订正（铁律 5）**：本行原来写「框宽/框高走**世界单位**
        //   （`W01/H01` 正好就是世界单位）」—— 那**只对世界档成立**：`SetAutoFitBox` 的第一个形参
        //   直接进 TMP 的 `sizeDelta`，而画布档的 `sizeDelta` 单位是**设计 px**
        //   （⇒ 世界档走 `W01/H01`、画布档走 `Wpx/Hpx`，调用点一律经 `BoxW/BoxH`）。
        //   喂错的后果**不报错**：画布档喂世界单位 ⇒ 框宽 5.34 px、字号收敛到一个小得看不见的值。
        // ⚠️ 折行 = **1** ⇒ 不用还原（`SetAutoFitBox` 内部那句 `SetWrapWidth` 顺带开的就是 `Normal(1)`）。
        const float AutoMinPx = 4f, AutoMaxPx = 60f, AutoBasePx = 36f;

        /// <summary>原版 `fadeTime`（**淡入**用的时长）</summary>
        public const float FadeTime = 0.2f;

        /// <summary>原版 `FadeOut` 的时长系数 —— `fadeTime × 0.5`。
        /// <para>判据（第一权威 = `d:/2/tools/decomp_full/`）：`ActiveSkillDisplay__FadeOut.c:26` 把
        /// `DAT_1834b2bb4`（= **0.5f**，`工具/read_literal.py` 现读）乘进 `fadeTime` 再喂 `FadePower`；
        /// 淡入那一侧 `ActiveSkillDisplay__FadeIn.c:55` 用的是 `DAT_1834b2bb8`（= 1.0f）×`fadeTime`。</para>
        /// <para>⇒ **淡出 0.1 s / 淡入 0.2 s**。🔴 **2026-10-24（执行代理 `E17`）就地订正（铁律 5）**：
        /// 改前两个方向都用 `FadeTime`（淡出慢一倍），文件头还写着「一个解析不出的常数」——
        /// 那个常数现在已经解出来了。判据 → `资料/普查产出_1024b/C4_战斗表现判据.md` §1·2。</para></summary>
        public const float FadeOutFactor = 0.5f;

        /// <summary>`Hide()` 那条淡出的时长 = `FadeTime × FadeOutFactor` = **0.1 s**（原版值，见上一个 const）。</summary>
        public const float FadeOutTime = FadeTime * FadeOutFactor;

        /// <summary>面板底色。原版那块 Image 是**纯白无 sprite**（见文件头）—— **这块深色板是我们挑的**</summary>
        static readonly Color BgColor = new Color(0.05f, 0.06f, 0.09f, 0.86f);
        /// <summary>🔴 **2026-10-15（账 `A2106`）就地订正：原版 Name / Cost 是【白】。**
        /// 原来这里是暖色 `(1,0.96,0.86)` / `(1,0.94,0.72)` —— **未标注的偏移**（既不是照原版、也没写「我们挑的」）。
        /// 判据 → `资料/普查产出_1015战斗/C2_逐格对账_弹窗与结算转场.md`（引 `B2:68,70`）。</summary>
        static readonly Color NameColor = new Color(1f, 1f, 1f);
        /// <inheritdoc cref="NameColor"/>
        static readonly Color CostColor = new Color(1f, 1f, 1f);
        static readonly Color DescColor = new Color(0.90f, 0.92f, 0.96f);
        static readonly Color TargetsColor = new Color(0.96f, 0.90f, 0.60f);

        // ---- 原版 `Lights`：三层**全铺**色片，靠开关切换（不是一排小灯）----
        // 颜色原样照抄，alpha 0.392 也是原版的。
        // ⚠️ 交接文档之前把颜色写反了 —— 原件里 `LightActing` 是白、`LightAvailable` 才是黄绿。
        static readonly Color LightActing = new Color(1.000f, 1.000f, 1.000f, 0.392f);
        static readonly Color LightAvailable = new Color(0.925f, 1.000f, 0.000f, 0.392f);
        static readonly Color LightPressed = new Color(0.006f, 0.000f, 1.000f, 0.392f);

        /// <summary>acting 白层**淡入**用时 = **0.4 s**。
        /// <para>判据（🆕 2026-10-25 · `N4`，账 `ActiveSkillDisplay.Drag` / `.OnTouchDrag`）：
        /// `ActiveSkillDisplay__ShowActingLight.c`（以及 `__OnTouchDrag.c` 里内联的那一段）——
        /// `actingLight.gameObject.SetActive(true)` 之后 `DOFade(actingLight, 1f, DAT_1834b3158)` +
        /// `SetEase(6)`（= DOTween `Ease.OutQuad`，取值见本工程 `DOTweenSettings.asset` 的 `defaultEaseType: 6`）。
        /// `DAT_1834b3158` 用 `工具/read_literal.py` 现读 = **0.4**（4 字节 float）。
        /// ⚠️ 起点 α = 序列化值 **0.392**（= `LightActing.a`）—— `DOFade` 从**当前** color.a 出发，不是从 0。</para></summary>
        public const float ActingFadeIn = 0.4f;

        /// <summary>acting 白层**淡出**用时 = **0.3 s**。
        /// <para>判据：`ActiveSkillDisplay__FinishActing.c:15-24` —— `DOFade(actingLight, 0f, DAT_1834b2dc8)` +
        /// `SetEase(6)`（`Ease.OutQuad`）。`DAT_1834b2dc8` 现读 = **0.3**（4 字节 float）。</para>
        /// ⚠️ 我们三盏灯收在**同一颗** `_light` 上（见 `Light` 枚举），所以 acting 用「α 倍率」表达：
        /// 淡入 = `_actingA` 0.392 → 1，淡出 = `_actingA` → 0（颜色本身仍是 `LightActing` 的白）。</summary>
        public const float ActingFadeOut = 0.3f;

        /// <summary>
        /// 面板上铺哪一层色片。三态的触发条件是**从 IL2CPP dump 里查出来的**：
        ///   · **蓝 `LightPressed`** —— 指针按在面板上（`ClickDown` 点亮、`PointerExit`/`Up` 熄灭）**确定**
        ///   · **白 `LightActing`** —— 技能**正在结算**（`TryUsingActiveAbility` 成功后点亮）**确定**
        ///   · **黄绿 `LightAvailable`** —— 可用性指示（`DisableUsableLight()` 把它瞬间淡到 0）。
        ///     这一条是**从函数名推的**，原码里没有一行显式把它设成 1
        /// </summary>
        public enum Light { None, Available, Pressed, Acting }

        // ==================================================================

        /// <summary>🔴 **2026-10-16（`T09` 批 3）：类型 `ImageQuad` → `HudPiece`** —— 画布档下这三颗是
        /// **uGUI 件**（`RectTransform + Image`）。⛔ **一个件只许在一边**（要么旧 quad、要么 uGUI）——
        /// 见 `HudPiece` 文件头那条铁律。</summary>
        HudPiece _bg, _light, _manaIcon;
        Label _name, _cost, _desc, _targets;
        Texture2D _solid;
        bool _built;

        // ---- 🆕 2026-10-16（`T09` 批 3）：画布那一侧 --------------------------------------------
        /// <summary>本面板那块 **uGUI 子画布**的 `RectTransform`（`OverHand` / order 0）。
        /// <para>`null`（出厂）= **世界档** —— 与改之前**逐位相同**；非 null = **画布档**
        /// （件建在它下面、坐标 = **设计 px**）。⛔ **绝不许两边都建**（同 `HudPiece`/`Label` 那条铁律）。</para>
        /// <para>它是 `EnsureSubCanvas` 的返回值 ⇒ **四边贴合根画布**（锚 0–1、`sd` 0、枢轴居中）
        /// ⇒ **它的中心 = 画布中心、局部单位 = 设计 px**（`T09` §五·补③ 一）。</para></summary>
        RectTransform _cv;

        /// <summary>🆕🔴 **2026-10-20（W58 · `T09` C4）：原版 `ActiveSkillDesc`（`RT_3104`）那一级的【锚点节点】。**
        /// <para>它**自己就是「写死的归一化锚点」**：双轴拉伸 `aMin (0.665576696395874, 0)` /
        /// `aMax (0.965788722038269, 0.3040129840373993)` + `pivot (0.5,0.5)` +
        /// `ap (−606.110107421875, 55.0008544921875)` + `sd (−0.0003662100061774254, −3.509999990463257)`
        /// （逐字段 = `bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_3104.json` 原版真值）
        /// ⇒ **父画布一拉伸它就跟着变**（4:3 / 21:9 不用任何人重算）。</para>
        /// <para>`PanelCvRect` 现在**从它读**（不再是「我们那组常量 × 画布尺寸」的第二次现算）。</para>
        /// <para>⚠️ 本节点**只承载几何 + 原版那一颗捕获区**：没有 `Image`、不吃射线；
        /// **唯一的子件** = `HideAbilityButton`（原版 `RT_3053.m_Father = 3104`，2026-10-20 W58 补上）。
        /// 其余 7 件仍是子画布的平级子件（那条「⛔ 不倒嵌套」的已知差异照旧，见文件头 §输入层 那两条结构性差异）。</para>
        /// <para>`null` = 世界档 / 没建起来（世界档没有画布坐标，`PanelCvRect` 恒不被读）。</para></summary>
        RectTransform _panelRt;

        // ── 🆕 2026-10-20（W57 · `T09` 步骤 A3-e）：uGUI 输入层（判据 → 文件头「输入层换 uGUI」那一段）──
        //  ⚠️ **只有画布档建这一组**（世界档用不了 uGUI 射线 —— 件是 `ImageQuad`，`SetRaycastTarget` 会出声拒绝）。
        /// <summary>原版 `HideAbilityButton`：一块**远大于屏幕的透明点击捕获区**（`m_Color.a = 0`、`rt = 1`）。
        /// 🔴 **2026-10-20（W58）**：父 = **面板锚点节点** `_panelRt`（原版 `RT_3053.m_Father = 3104`），
        /// ⛔ 不是子画布；五元组见 `PlaceHideButton`。
        /// `null` = 世界档 / 没建起来（见 `BuildInput` 的出声）。</summary>
        HudPiece _hideBtn;
        /// <summary>原版 `AbilityContainer` 上那颗 `EventTrigger`（自检 / 诊断用）。</summary>
        EventTrigger _abilityTrigger;
        /// <summary>原版 `HideAbilityButton` 上那颗 `EventTrigger`。</summary>
        EventTrigger _hideBtnTrigger;
        /// <summary>输入层现在是不是**开着**（跟着显隐走，见 `SetInputEnabled`）。</summary>
        bool _inputEnabled;

        /// <summary>🆕 **2026-10-20（`T09` A3-e）**：原版 `ClickDown`（按下 `AbilityContainer`）。
        /// ⚠️ `pressingLight`（蓝层）**已经由本面板自己点亮**（那是面板的视觉状态）；本事件只给宿主
        /// 做「按下」那一档的副作用（原版还播 `AbilityBoxClick`，本工程没有那个 cue，见文件头差异③）。</summary>
        public event System.Action AbilityPressed;
        /// <summary>🆕 **2026-10-20（`T09` A3-e）**：原版 `ClickUp` → `TryUsingActiveAbility`。
        /// <para>⚠️ **订阅者要做的是「用掉这个技能」**（驱动侧对应 `Resolve(AttackKind.Ability, …)` 那条链）。
        /// 🔴 **2026-10-20（`T09` WIRE）就地订正**：本行原写「本轮没有订阅者」—— **已过期**：
        /// 驱动已订阅成 `SkillPanelConfirmRelease`（只亮 acting 层、**不结算**；释放仍发生在「点目标」那一下）。</para></summary>
        public event System.Action AbilityClicked;
        /// <summary>🆕 **2026-10-20（`T09` A3-e）**：原版 `Drag` → `OnTouchDrag`（拖到合法目标也释放技能）。
        /// 🔴 **2026-10-20（`T09` WIRE）就地订正**：本行原写「没有订阅者」—— **已过期**：
        /// 驱动已订阅成 `SkillPanelDragToTarget`（重进选目标，**幂等、不结算**）。</summary>
        public event System.Action AbilityDragged;
        /// <summary>🆕 **2026-10-20（`T09` A3-e）**：原版 `HideAbilityButton` 的 `PointerDown` →
        /// `StopDisplayingSkillFromManager`。本面板**已经在 `PointerDown` 那一下 `Hide()`**（= 原版 `FadeOut`）；
        /// 本事件给宿主补另一半 —— `CardScript.StopDisplayingActiveAbility()` 的等价物 = `ClearSelection()`
        /// 里那几行（单位高亮 / 选择器 / 准星）。
        /// <para>🔴 **2026-10-20（`T09` WIRE / `T09-FIX3`）就地订正**：本行原写「本轮没有订阅者」—— **已过期**：
        /// 驱动已订阅成 `OnSkillPanelCloseRequested`（`ClearSelection`）。⚠️ **点任何棋盘卡那一下都不会发本事件**
        /// （P0-1 / `T09-FIX5`：`LeavePointerToWorld` 对任何棋盘卡放行 ⇒ `OnHidePointerDown` 直接 return）。</para></summary>
        public event System.Action CloseRequested;

        /// <summary>🔴 **2026-10-20（W57 · 战斗线 `T09-FIX5` · P0-1）：捕获区这一下**该不该让给世界侧**。
        /// <para>入参 = 屏幕点（`PointerEventData.position`）；返回 **true** ⇒ 这一下**不关面板**，
        /// 留给棋盘那条世界射线。`null`（没人登记）= 退回原行为「点哪都关」。</para>
        /// <para>🔑 **判据是「任何一张棋盘卡」，不是「目标侧那张」**（`T09-FIX3` 的「只放行目标侧」在这里修）：
        /// 原版棋盘卡自带 `CardCollider`（`IPointerDownHandler` / `IPointerClickHandler`），点**任何**一张
        /// 都先被那张卡吃掉；点亮目标时原版**只改高亮、不关非目标卡的 collider**
        /// （`BattleManager__ShowPotentialTargets.c` / `__StartChoosingAbilityTargetByClick.c`）⇒
        /// 点自己人 / 非目标也不会关面板。判据 → `资料/普查产出_1019/W57_战斗线_T09_审查.md` §三·1。</para>
        /// <para>🔴 **为什么需要它**：本面板那块 `HideAbilityButton` 是**远大于屏幕**的透明捕获区，
        /// 而我们的棋盘卡**仍是 3D 网格**（没有 uGUI 射线靶）⇒ `EventSystem` 整屏唯一能命中的就是它。
        /// 原版不会这样：原版棋盘卡自带 `CardCollider`
        /// + 相机上挂着 `EverguildPhysics2DRaycaster`（`Physics2DRaycaster` 子类，13/13 战场都有）
        /// ⇒ 点棋盘卡那一下**先被卡吃掉**（`CardCollider.OnPointerClick → CardScript.OnTouchUpAsButton`），
        /// 捕获区收不到。我们这一侧由驱动登记「这个屏幕点是不是落在棋盘卡上」把等价性补回来。</para></summary>
        public System.Func<Vector2, bool> LeavePointerToWorld;

        /// <summary>本面板那块子画布的名字（= 原版宿主名 + `" Canvas"`，见 `T09` §五·补⑩ 第 1 步）。
        /// 判据 = `T09c` §一 我这一行：`ActiveSkillDesc` @ `…/Safe area FrontCanvas`，
        /// **`OverHand` / order 0 / 兄弟序 1/14**。⛔ **只此一份**（建它、自检找它都取这里）。</summary>
        public const string SubCanvasName = "ActiveSkillDesc Canvas";

        /// <summary>本面板现在是不是**画布档**（诊断 / 断言 / 自检用；**判据只此一处**）。</summary>
        public bool UguiMode { get { return _cv != null; } }

        /// <summary>自检用：本面板那块**子画布**的 `RectTransform`（查它的层/order/`overrideSorting` 用）。</summary>
        public RectTransform CanvasRect { get { return _cv; } }

        // ── 🔴 2026-10-16（锚点补正）：画布档那块**面板矩形**的三个只读口（自检 / 断言用）──────────
        //  它们读的是 `PanelCvRect` —— **同一处判据**（`Contains` / 件位置 / 尺寸全读它），
        //  所以断言拿它去比「件的实绘矩形」是自洽的。
        //  ⚠️ **期望值要取【判据侧】**（原版 `RT_3104` 的锚点五元组 + `HudCanvas.RootRect.rect`），
        //     ⛔ 别拿本口算出来的数当期望值 —— 那是拿实现证明实现（同义反复）。

        /// <summary>画布档：面板矩形的**设计 px 尺寸**（= `(aMax−aMin) × 画布尺寸 + sizeDelta`）。
        /// ⛔ 世界档没有这一级 ⇒ 返回 `(0,0)`（别拿它当世界档的尺，那个是 `PanelW/PanelH`）。</summary>
        public Vector2 PanelCvSize { get { return UguiMode ? PanelCvRect.size : Vector2.zero; } }

        /// <summary>画布档：面板矩形中心在**子画布局部**的坐标（设计 px、中心原点、y 向上）。</summary>
        public Vector2 PanelCvCenter { get { return UguiMode ? PanelCvRect.center : Vector2.zero; } }

        /// <summary>画布档用的那把**父尺寸**（= `_cv.rect` 的宽高，设计 px；量不到时退参考分辨率
        /// 1920×1080，见 <see cref="CanvasPx"/>）。断言用它自己独立算一遍期望矩形。</summary>
        public Vector2 CanvasPxSize { get { return CanvasPx; } }

        /// <summary>🆕 **2026-10-20（W58 · `T09` C4）：面板锚点节点（原版 `ActiveSkillDesc` / `RT_3104`）的
        /// 五个字段读回** —— 自检 / 断言用（这是「来源」那一侧的独立读数）。
        /// <para>顺序：`aMin` · `aMax` · `pivot` · `anchoredPosition` · `sizeDelta`。
        /// 返回 `false` = 世界档 / 节点没建出来（**五个 `out` 都是 `NaN`** ——
        /// ⛔ 别折成 `(0,0)`：那与「锚点真是 0」分不开，会让断言假绿）。</para>
        /// <para>期望值取**原版 JSON 的字面量**（⛔ 不读这里反推；那就成了拿实现证明实现）。</para></summary>
        public bool PanelAnchorsForTest(out Vector2 aMin, out Vector2 aMax, out Vector2 pivot,
                                        out Vector2 anchoredPos, out Vector2 sizeDelta)
        {
            aMin = aMax = pivot = anchoredPos = sizeDelta = new Vector2(float.NaN, float.NaN);
            var rt = _panelRt;
            if (rt == null) return false;
            aMin = rt.anchorMin;
            aMax = rt.anchorMax;
            pivot = rt.pivot;
            anchoredPos = rt.anchoredPosition;
            sizeDelta = rt.sizeDelta;
            return true;
        }

        /// <summary>🆕 **2026-10-20（W58 · `T09` C4）灭自证用**：把面板锚点节点的 `anchoredPosition`
        /// 平移 <paramref name="delta"/>（px、y 向上）。调用方测完必须**平回去**。
        /// <para>用途：证明 `PanelCvRect` **真的从节点读** —— ap 动 ⇒ 面板中心必须跟着动、尺寸不变；
        /// 若它被改回「拿 `CvAnchor*` 常量 × 画布尺寸现算」（不看节点），中心**不会动** ⇒ 断言红。</para>
        /// <para>世界档 / 节点没建 ⇒ `false`（⛔ 不静默假装挪过）。</para></summary>
        public bool NudgePanelAnchoredPosForTest(Vector2 delta)
        {
            if (_panelRt == null) return false;
            _panelRt.anchoredPosition += delta;
            return true;
        }

        /// <summary>🔴 **2026-10-16（`T09` 批 3）：把本面板接到 uGUI 档。**
        /// <paramref name="host"/> = **本面板那块子画布的 `RectTransform`**
        /// （= `HudCanvas.EnsureSubCanvas(null, SubCanvasName, HudCanvas.SortingLayer.OverHand, 0)` 的返回值）。
        ///
        /// <para>🔴 **为什么必须是「建 UI 的那一方」来登记**（同 `Tooltip.UseHost` 那条）：组件是**场景挂的**
        /// （`AddComponent<SkillPanel>()`，没有 `Create`）⇒ 只有驱动层知道根画布建好了没。</para>
        ///
        /// <para>传 `null` = **子画布没建起来**：**留在世界档**（= 与改之前逐位相同）、并**出声**
        /// （⛔ 不静默 —— 那正是「一半在画布、一半在世界」那种 16:9 下看不见的半迁移态）。</para>
        ///
        /// <para>⚠️ **可以在 `Build()` 之后再调**：那时会**拆掉世界档的件、按画布档重建**
        /// （`Awake` 会先跑一次 `Build()`，而驱动层登记宿主的时刻由别人定 —— 两种次序都要能落地）。
        /// 拆旧建新是**同一个件只许在一边**那条铁律的落实，⛔ **不是**「两边都建」。</para></summary>
        public void UseHost(RectTransform host)
        {
            if (host == null)
            {
                Debug.LogError("[Battle] 技能面板：`UseHost(null)` —— 那块 `OverHand` 子画布没建起来"
                             + " ⇒ 本面板**留在世界档**（画面与改之前逐位相同，"
                             + "但非 16:9 下会与已迁到画布的同族面板分家）");
                return;
            }
            if (_cv == host) return;
            _cv = host;
            if (_built)
            {
                Debug.Log("[Battle] 技能面板：`UseHost` 排在 `Build()` 之后 ⇒ 拆掉世界档的件、按画布档重建"
                        + "（⛔ 不许两边都建）。把登记挪到第一次 `Build()` 之前可以省掉这一次重建。");
                Teardown();
                Build();
            }
        }

        /// <summary>拆掉本面板已经建出来的件 —— **只用于换档**（世界档 ↔ 画布档）。
        /// ⛔ 别拿它当「收起面板」用（收起只走 `HideImmediate`：显隐只靠 alpha，见它的 doc）。</summary>
        void Teardown()
        {
            KillGo(_bg != null ? _bg.gameObject : null);
            KillGo(_light != null ? _light.gameObject : null);
            KillGo(_manaIcon != null ? _manaIcon.gameObject : null);
            KillGo(_name != null ? _name.gameObject : null);
            KillGo(_cost != null ? _cost.gameObject : null);
            KillGo(_desc != null ? _desc.gameObject : null);
            KillGo(_targets != null ? _targets.gameObject : null);
            // 🆕 2026-10-20（`T09` A3-e）：输入层那两件也要拆（否则换档之后盘着一块**旧画布**的捕获区）
            KillGo(_hideBtn != null ? _hideBtn.gameObject : null);
            // 🆕 2026-10-20（W58 · `T09` C4）：面板锚点节点也要拆（否则换档之后盘着一颗**旧画布**的节点，
            //   而它正是 `PanelCvRect` 的几何来源 ⇒ 下次 `BuildCanvas` 会再建一颗、两颗并存）。
            KillGo(_panelRt != null ? _panelRt.gameObject : null);
            _panelRt = null;
            _bg = _light = _manaIcon = null;
            _name = _cost = _desc = _targets = null;
            _hideBtn = null;
            _abilityTrigger = _hideBtnTrigger = null;   // 它们是挂在上面那些 GO 上的，随 GO 一起销毁
            _inputEnabled = false;
            if (_fade != null) { _fade.Kill(); _fade = null; }
            if (_actingFade != null) { _actingFade.Kill(); _actingFade = null; }
            _pressed = _acting = false;
            _actingA = 0f;
            ShownName = null;
            ShownTargets = 0;
            CurrentLight = Light.None;
            Alpha = 0f;
            Visible = false;
            _built = false;                 // ⇒ 下一次 `Build()` 会用**当前**档重建
        }

        static void KillGo(GameObject o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);
        }

        /// <summary>面板现在开着吗</summary>
        public bool Visible { get; private set; }
        /// <summary>正在显示哪个技能（自检断言用）。没开着是 null</summary>
        public string ShownName { get; private set; }
        /// <summary>现在显示的可选目标数（自检断言用）</summary>
        public int ShownTargets { get; private set; }
        /// <summary>现在铺的是哪层色片（自检断言用）</summary>
        public Light CurrentLight { get; private set; }
        /// <summary>现在淡到多少（0~1，自检断言用）</summary>
        public float Alpha { get; private set; }

        /// <summary>🆕 **2026-10-17（`A1212` 块 7 的断言口）**：那四颗字（0=`NameText` · 1=`CostText` ·
        /// 2=`DescText` · 3=`TargetsAvailableText`）。**只给自检读** —— 生产路径不经过它。
        /// <para>⚠️ 为什么不按节点名找：画布档下它们挂在**共用子画布** `BattleSettingsPanel`/`ActiveSkillDesc`
        /// 那一层，`Find` 只能拿第一个同名件；本类自己持有引用，直接交出去最准。</para></summary>
        public Label TextForTest(int i)
        {
            switch (i)
            {
                case 0: return _name;
                case 1: return _cost;
                case 2: return _desc;
                case 3: return _targets;
            }
            return null;
        }

        /// <summary>🆕 **2026-10-19（`A1284` 的断言口）**：7 个件的**面板内归一化矩形**
        /// （x 从左、y 从**顶**、w/h 为比例 —— 与 `BgRect` 那一组同口径）。
        /// 序号 = 0 `Bg` · 1 `Name` · 2 `CostIcon` · 3 `CostText` · 4 `Desc` · 5 `Targets` · 6 `Lights`。
        /// <para>⚠️ 本口给的是**我们的常量**；断言的期望值必须取**原版绝对矩形**（⛔ 别拿它当判据侧 = 同义反复）。</para></summary>
        public static Rect RectForTest(int i)
        {
            switch (i)
            {
                case 0: return BgRect;
                case 1: return NameRect;
                case 2: return CostIconRect;
                case 3: return CostTextRect;
                case 4: return DescRect;
                case 5: return TargetsRect;
                case 6: return LightRect;
            }
            return new Rect();
        }

        /// <summary>🆕 **2026-10-19（`A1284`）**：原版面板在 1920×1080 下的像素尺寸
        /// （`PanelPxW/H` 的只读口）—— 只给自检把归一化矩形换算回绝对 px。</summary>
        public static Vector2 PanelPxForTest { get { return new Vector2(PanelPxW, PanelPxH); } }

        bool _pressed, _acting;
        /// <summary>acting 白层的**当前不透明度**（0~1）。原版那是 `actingLight` 这颗 `Image` 的 `color.a`：
        /// 序列化值 **0.392**（= `LightActing.a`），`ShowActingLight` 把它 `DOFade(1, 0.4)`、
        /// `FinishActing` 再 `DOFade(0, 0.3)`（判据 → `ActingFadeIn` / `ActingFadeOut` 那两个常量）。
        /// <para>🔴 **2026-10-25（`N4`）**：我们三盏灯收在**同一颗** `_light` 上（`Light` 枚举），
        /// ⇒ acting 这一档不能直接写 color.a，只能写成「α 倍率」：`_actingA &gt; 0` ⇒ 铺白层、α = `_actingA`。</para></summary>
        float _actingA;
        /// <summary>acting 白层那一条淡入/淡出补间（原版存在组件字段 `+0xc0` 里的那一条）。
        /// ⚠️ 必须经 `CardTween.Mode` + `SetLink`（同本文件 `_fade` 的理由：批处理 Manual 档 / 防销毁后还在跑）。</summary>
        Tween _actingFade;
        Tween _fade;

        void Awake() { Build(); }

        public void Build()
        {
            if (_built) return;
            _built = true;
            CardArt.Load();

            if (UguiMode) BuildCanvas();
            else BuildWorld();

            // 🔴 **2026-10-24（`E17`）：顺序是「先排版、再收起」**（改前是反的）。
            //   理由：`TargetsAvailableText` 收起时是**整个 GameObject 关掉**的（它不参与淡入淡出，
            //   见 `SetTargetsShown`），而 `RefreshLayout → FitBox → SetAutoFitBox` 里的
            //   `ForceMeshUpdate()` 在**未激活**对象上会早退（`TextMeshPro.cs:2119` 的 `IsActive()` 闸）
            //   ⇒ 反着写的话那一次自适应框是在「关着的」标签上算的（量出来是哨兵/旧值）。
            //   两段互不依赖（排版不看 `Alpha` / `Visible`，收起也不动几何）⇒ 换序不改变任何终态。
            RefreshLayout();
            HideImmediate();
        }

        /// <summary>世界档建件（**改之前那一版** —— ⛔ 不许在这里混进画布那一路）。
        /// ⚠️ **只有一处**与改之前不同：三颗图过一道 `HudPiece.Of(...)`（字段类型换成了适配层，
        /// 见 `HudPiece` 文件头）—— 建出来的**仍然是旧的世界空间 quad**，逐位不变。</summary>
        void BuildWorld()
        {
            _bg = HudPiece.Of(ImageQuad.Create(transform, Solid(), Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "AbilityContainer"));
            _light = HudPiece.Of(ImageQuad.Create(transform, Solid(), Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "Lights"));

            // 🔴 **2026-10-17（`A1024`③ 批 2–4）**：原版 `ActiveSkillDesc` 四颗字的面（现读 `battle1_tmp.tsv`）：
            //   · `AbilityContainer/NameText`   = **Asar**（fs42.35）  ⇒ `LatinUi`
            //   · `AbilityContainer/CostPanel/CostText` = **Asar**（fs52.4）⇒ `LatinUi`
            //   · `AbilityContainer/DescText`   = **Asar**（fs37.35）  ⇒ `LatinUi`
            //   · `TargetsAvailableText`        = **Pragati**（fs56.45）⇒ `LatinCardBody`
            //   ⚠️ 普查表 §二附录那一行只列了 `DescText` 一颗 Asar（NameText/CostText 写漏）——
            //     本批按它自己写明的**唯一判据**（TMP 的 `m_fontAsset`）落，差异记在 `W_A1024_战斗三窗.md`。
            _name = MakeLabel("NameText", NamePx, NameColor, face: TmpFont.Face.LatinUi);
            _cost = MakeLabel("CostText", CostPx, CostColor, face: TmpFont.Face.LatinUi);
            // 原版 `DescText` 是**左对齐**（H=1）+ `m_margin.x = 7.4727` → 锚点在左边
            _desc = MakeLabel("DescText", DescPx, DescColor, new Vector2(0f, 0.5f), face: TmpFont.Face.LatinUi);
            _targets = MakeLabel("TargetsAvailableText", TargetsPx, TargetsColor, face: TmpFont.Face.LatinCardBody);

            // 原版那格的图标是 `manaIcon`（原件里 `costSprite` 是 null，运行时才赋）——
            // 用 HUD 那颗能量宝石那张图占位，方图居中放进它的矩形
            // ⚠️ `ImageQuad.Create` 在贴图为空时**返回 null**（`HudPiece.Of(null)` 也返 null）——
            //    各读口都有空判，照旧。
            _manaIcon = HudPiece.Of(ImageQuad.Create(transform, CardArt.Ui("UI_Energy_Eldar"), Vector3.zero, 0.2f,
                                                     new Vector2(0.5f, 0.5f), "manaIcon"));
        }

        /// <summary>🔴 **2026-10-16（`T09` 批 3）画布档建件。**
        /// **位置/尺寸一概不在这里定** —— 全在 `RefreshLayout`（两档共用同一组面板内归一化常量，
        /// 判据只此一处）。这里只管「建出来 + 定层档（兄弟序）+ 定字号默认值」。
        /// <para>⚠️ **走 `MakeImageInParent`（中心锚、「相对父中心」）而不是 `MakeImage`/`QuadFromTopPx`**
        /// （那两个把 `x/y` 当**屏幕绝对 px**、锚点是归一化的）—— 见它自己的 doc：
        /// 子画布 rect 在 `Expand` 档下比 1920×1080 高 ⇒ 套错了会**静默摆到别处**（`T09` §七·10）。</para></summary>
        void BuildCanvas()
        {
            // 🆕 2026-10-20（W58 · `T09` C4）：先建原版 `ActiveSkillDesc` 那一级的锚点节点 ——
            //   `PanelCvRect` 的唯一几何来源（判据见字段 doc）。⛔ 除原版那一颗 `HideAbilityButton` 外，
            //   别把件挂到它下面（其余 7 件仍是子画布的平级子件）。
            _panelRt = MakePanelNode();
            _bg = MakeCv(Solid(), "AbilityContainer", ZBg);
            _light = MakeCv(Solid(), "Lights", ZLight);

            // 🔴 **2026-10-17（`A1024`③ 批 2–4）**：四颗字的面同 `BuildWorld` 那一块（判据 / 落点只此一份）——
            //   `NameText` · `CostText` · `DescText` = Asar ⇒ `LatinUi`；`TargetsAvailableText` = Pragati ⇒ `LatinCardBody`。
            _name = MakeLabel("NameText", NamePx, NameColor, face: TmpFont.Face.LatinUi);
            _cost = MakeLabel("CostText", CostPx, CostColor, face: TmpFont.Face.LatinUi);
            // 原版 `DescText` 是**左对齐**（H=1）+ `m_margin.x = 7.4727` → 锚点在左边
            _desc = MakeLabel("DescText", DescPx, DescColor, new Vector2(0f, 0.5f), face: TmpFont.Face.LatinUi);
            _targets = MakeLabel("TargetsAvailableText", TargetsPx, TargetsColor, face: TmpFont.Face.LatinCardBody);

            var gem = CardArt.Ui("UI_Energy_Eldar");
            _manaIcon = MakeCv(gem, "manaIcon", ZText);
            if (_manaIcon == null)
                Debug.LogWarning("[Battle] 技能面板：费用图标那张图 `UI_Energy_Eldar` 取不到"
                               + " ⇒ 那一格**不画**（照旧出声，不静默；⛔ 也不为此退回世界空间 quad）");

            // 🆕 2026-10-20（`T09` A3-e）：输入层（原版 `AbilityContainer` + `HideAbilityButton` 两口）。
            //   ⚠️ 必须排在件建完之后（它要用 `_bg` / `_targets`）；显隐由 `SetInputEnabled` 跟着走。
            BuildInput();
        }

        /// <summary>🆕🔴 **2026-10-20（W58 · `T09` C4）：建原版 `ActiveSkillDesc`（`RT_3104`）那一级的锚点节点。**
        /// <para>五个字段**逐字照抄**原版 JSON（出处 → <see cref="_panelRt"/> 的 doc 与 `CvAnchor*` 那一组常量）：
        /// 双轴拉伸 + `pivot (0.5,0.5)` + `ap (−606.11, 55.00)` + `sd (−0.00037, −3.51)`。
        /// ⇒ **父画布一拉伸它就跟着变** —— 这就是 C4 要的「锚点表达（随父级拉伸）」，
        /// ⛔ 不再是「我们那组常量 × 画布尺寸」的第二次现算。</para>
        /// <para>⛔ 别把它改成中心点锚 / 别删：`PanelCvRect` 的实测支与退档支都读它。</para></summary>
        RectTransform MakePanelNode()
        {
            var go = new GameObject("ActiveSkillDesc", typeof(RectTransform));
            go.transform.SetParent(_cv, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(CvAnchorMinX, CvAnchorMinY);
            rt.anchorMax = new Vector2(CvAnchorMaxX, CvAnchorMaxY);
            rt.pivot = new Vector2(CvPivotX, CvPivotY);
            rt.anchoredPosition = new Vector2(CvAnchoredX, CvAnchoredY);
            rt.sizeDelta = new Vector2(CvSizeDeltaX, CvSizeDeltaY);
            // 🔴 **2026-10-20（W58）**：登记进子画布的 z 账 —— 否则平级件每次 `SetSiblingIndex`
            //   都会把这颗「没进账」的节点挤到后面，它的子件 `HideAbilityButton`（透明捕获区）
            //   就会盖在面板本体上（点面板 = 关面板）。见 `ZPanelNode` 的 doc。
            HudCanvas.Add(rt, ZPanelNode);
            return rt;
        }

        /// <summary>画布档建一块**纯色/贴图**件（尺寸随后由 `RefreshLayout` 的 `PlaceQuad` 写 ——
        /// 两档各一份实现，见它）。贴图为空 ⇒ 返回 `null`（⛔ 不静默建一块白图）。</summary>
        HudPiece MakeCv(Texture2D tex, string name, float z)
        {
            if (tex == null) return null;
            // 出厂尺寸 = 贴图本身那一档（立刻会被 `PlaceQuad` 按面板内矩形覆盖）
            return HudCanvas.MakeImageInParent(_cv, tex, name, 0f, 0f, tex.width, tex.height, z, Color.white);
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-20（W57 · `T09` 步骤 A3-e）：uGUI 输入层
        //  判据逐条 → 文件头「输入层换 uGUI」那一段（原版 `MB_4864` / `MB_5084` / `MB_4207` / `MB_4854`）。
        //  ⛔ 只发事件、不做宿主那一半动作（释放技能 / 清选择归 `BattleDriver`）；
        //     **例外** = 捕获区按下那一下的 `Hide()`（原版 `FadeOut` 本来就是面板自己的事）。
        // ═══════════════════════════════════════════════════════════════════════════════════════════

        // 自检计数（灭自证：uGUI 各事件来一次 = 1；世界侧那条手搓路**不经过这里**）
        int _downCount, _upCount, _enterCount, _exitCount, _dragCount, _closeDownCount;
        /// <summary>🆕 2026-10-20（`T09-FIX3` · P0-1）：捕获区按下后被**让给世界侧**（没关面板）的次数。
        /// 与 `_closeDownCount` 配对 = 「来了几下 / 其中几下落给了目标射线」。</summary>
        int _closePassCount;

        /// <summary>建输入层（**只在画布档**；`BuildCanvas` 的最后一句调它）。</summary>
        void BuildInput()
        {
            if (_cv == null) return;                       // 世界档：没有 uGUI 件，整段不适用

            // ① 技能本体：原版 `AbilityContainer` = `Image(rt=1, pad=0)` + `EventTrigger`（5 条）
            _abilityTrigger = AttachSkillTrigger(_bg, "AbilityContainer");
            if (_abilityTrigger == null)
                Debug.LogError("[Battle] 技能面板：`AbilityContainer` 挂不上 `EventTrigger`"
                             + " ⇒ **点面板放不了技能 / 按压不亮灯**（不静默）");

            // ② 关闭捕获区：原版 `HideAbilityButton` —— 全屏、透明、`rt=1`、pad 0。
            //    🔴 **2026-10-20（W58 · 锚点补正）**：父 = **面板锚点节点** `_panelRt`
            //    （原版 `RT_3053.m_Father = 3104` = `ActiveSkillDesc`；⛔ 不是子画布）。
            //    五元组由 `PlaceHideButton` 逐字写原版字面量（届时 `RefreshLayout` 会跑一次）。
            if (_panelRt == null)
                Debug.LogError("[Battle] 技能面板：面板锚点节点 `ActiveSkillDesc` 没建起来"
                             + " ⇒ `HideAbilityButton` 只能挂到子画布上（锚点语义会偏；不静默）");
            _hideBtn = HudCanvas.MakeImageInParent(_panelRt != null ? (Transform)_panelRt : _cv,
                                                   Solid(), "HideAbilityButton",
                                                   0f, 0f, 1f, 1f, ZHideBtn, new Color(1f, 1f, 1f, 0f));
            if (_hideBtn == null)
            {
                Debug.LogError("[Battle] 技能面板：`HideAbilityButton`（全屏捕获区）没建起来"
                             + " ⇒ **点面板外关不掉技能面板**（不静默）");
            }
            else
            {
                _hideBtnTrigger = AttachHideTrigger(_hideBtn);
                if (_hideBtnTrigger == null)
                    Debug.LogError("[Battle] 技能面板：`HideAbilityButton` 挂不上 `EventTrigger`"
                                 + " ⇒ **点面板外关不掉技能面板**（不静默）");
            }

            // ③ 原版 `TargetsAvailableText` 的 TMP 是 `rt = 1`（父链上无 handler ⇒ 那一小条**吃点击但什么都不做**）。
            //    我们保持同构：目标数那一行也吃射线（收起时由 `SetInputEnabled(false)` 关掉）。
            if (_targets != null && !SetLabelRaycast(_targets, true))
                Debug.LogError("[Battle] 技能面板：`TargetsAvailableText` 的 `raycastTarget` 置不上"
                             + " ⇒ 与原版不同构（那条会漏到捕获区）");

            // 刚建出来时射线是**开**的（`AttachSkillTrigger` 已把它置 true）⇒ 记账要跟上。
            // ⚠️ 别省这一句：`Build()` 的收尾 `HideImmediate()` 靠它把整组关掉；账不对会**跳过关闭**
            //    （`SetInputEnabled` 的幂等短路）⇒ 一块看不见的全屏捕获区从第一帧起就吃点击。
            _inputEnabled = true;
        }

        /// <summary>原版 `AbilityContainer` 的五条 `EventTrigger`（`MonoBehaviour_5084.m_Delegates`）——
        /// **事件号逐条照抄**。⚠️ `PointerEnter(0)` 原版接的也是 `PointerExit`（同一个方法）—— 照抄，
        /// ⛔ 别「修」成空实现（那会让按下后滑出再滑回时蓝层不熄）。</summary>
        EventTrigger AttachSkillTrigger(HudPiece piece, string originalNode)
        {
            if (piece == null) return null;
            if (!piece.SetRaycastTarget(true)) return null;              // 世界空间件会在那边出声
            if (!piece.SetRaycastPadding(0f, 0f, 0f, 0f))
                Debug.LogError("[Battle] 技能面板：`" + originalNode + "` 的 `raycastPadding` 没写成（件不是 uGUI？）");
            var et = GetOrAddTrigger(piece.gameObject);
            if (et == null) return null;
            AddTrigger(et, EventTriggerType.PointerDown, OnSkillPointerDown);
            AddTrigger(et, EventTriggerType.PointerUp, OnSkillPointerUp);
            AddTrigger(et, EventTriggerType.Drag, OnSkillDrag);
            AddTrigger(et, EventTriggerType.PointerExit, OnSkillPointerExit);
            AddTrigger(et, EventTriggerType.PointerEnter, OnSkillPointerEnter);
            return et;
        }

        /// <summary>原版 `HideAbilityButton` 的那一条 `EventTrigger{ PointerDown }`（`MB_4854`）。</summary>
        EventTrigger AttachHideTrigger(HudPiece piece)
        {
            if (piece == null) return null;
            if (!piece.SetRaycastTarget(true)) return null;
            if (!piece.SetRaycastPadding(0f, 0f, 0f, 0f))
                Debug.LogError("[Battle] 技能面板：`HideAbilityButton` 的 `raycastPadding` 没写成（件不是 uGUI？）");
            var et = GetOrAddTrigger(piece.gameObject);
            if (et == null) return null;
            AddTrigger(et, EventTriggerType.PointerDown, OnHidePointerDown);
            return et;
        }

        static EventTrigger GetOrAddTrigger(GameObject go)
        {
            if (go == null) return null;
            var et = go.GetComponent<EventTrigger>();
            if (et == null) et = go.AddComponent<EventTrigger>();
            return et;
        }

        /// <summary>挂一条 `EventTrigger.Entry`（原版序列化里就是 `m_Delegates[]` 的 `eventID` + 持久回调）。</summary>
        static void AddTrigger(EventTrigger et, EventTriggerType type,
                               UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var e = new EventTrigger.Entry { eventID = type };
            e.callback.AddListener(action);
            et.triggers.Add(e);
        }

        /// <summary>原版 `ClickDown`：`pressingLight`(0x40) 亮 + 播 `AbilityBoxClick`。
        /// 我们只做**亮灯**（音效 cue 本地没有，见文件头差异③）+ 发事件。</summary>
        void OnSkillPointerDown(BaseEventData _)
        {
            _downCount++;
            if (!Visible) return;                     // 原版 `ClickDown` 先查 `beingDisplayed`
            if (!_pressed) { _pressed = true; ApplyColors(); }
            if (AbilityPressed != null) AbilityPressed();
        }

        /// <summary>原版 `ClickUp` → `OnTouchUpAsButton`：**先熄 `pressingLight`**，再 `TryUsingActiveAbility`。
        /// 我们熄灯 + 发事件（「释放」是宿主的事，见 `AbilityClicked` 的 doc）。</summary>
        void OnSkillPointerUp(BaseEventData _)
        {
            _upCount++;
            if (!Visible) return;
            ExtinguishPressed();
            if (AbilityClicked != null) AbilityClicked();
        }

        /// <summary>原版 `PointerExit`（`eventID 1`）：熄 `pressingLight`。</summary>
        void OnSkillPointerExit(BaseEventData _) { _exitCount++; if (Visible) ExtinguishPressed(); }

        /// <summary>原版 `PointerEnter`（`eventID 0`）接的**也是** `PointerExit` ⇒ 同样熄灯（逐条照抄）。</summary>
        void OnSkillPointerEnter(BaseEventData _) { _enterCount++; if (Visible) ExtinguishPressed(); }

        void ExtinguishPressed()
        {
            if (!_pressed) return;
            _pressed = false;
            ApplyColors();
        }

        /// <summary>原版 `Drag` → `OnTouchDrag`（拖到合法目标也走释放）。同 `AbilityClicked`：只发事件。
        /// <para>🔴 **2026-10-25（`N4` · 账 `ActiveSkillDisplay.Drag` / `.OnTouchDrag`）逐句对判据**：
        /// `ActiveSkillDisplay__OnTouchDrag.c`（ILSpy 同体 = `cpp2il/out_cpp2il_dev_ilspy/ActiveSkillDisplay.cs:456-505`）
        /// 在按下层还亮着时**直接 `return`**（`if (pressingLight.gameObject.activeSelf) return;`）——
        /// ⛔ **不是「先熄灯再继续」**（我们原来那句 `ExtinguishPressed()` 是多做的，已删）。
        /// 能继续之后做三件：`StartTargetedActiveAbility` · `SetupActiveAbility` · `waitingToResolve = true`，
        /// 再 `actingLight.SetActive(true)` + `DOFade(1, ActingFadeIn=0.4, OutQuad)`（`.c:496-504`）。
        /// 我们这边的等价：前两件由驱动承担（`AbilityDragged → SkillPanelDragToTarget` 幂等重进），
        /// 第三件 = <see cref="SetActing"/>；可用性闸取 <see cref="ShownTargets"/>（&gt;0 = 至少一个合法目标，
        /// 是我们这套里 `CanUseActiveAbility` 唯一可观测的那一面）。</para></summary>
        void OnSkillDrag(BaseEventData _)
        {
            _dragCount++;
            if (!Visible) return;
            if (_pressed) return;            // 原版 `pressingLight.activeSelf` ⇒ 直接 return（⛔ 别熄灯）
            if (ShownTargets <= 0) return;   // 原版 `CanUseActiveAbility(...) == false` ⇒ 直接 return
            SetActing();
            if (AbilityDragged != null) AbilityDragged();
        }

        /// <summary>原版 `HideAbilityButton` 的 `PointerDown` → `StopDisplayingSkillFromManager`：
        /// `ActiveSkillDisplay.FadeOut()`（面板自己收）+ `CardScript.StopDisplayingActiveAbility()`
        /// （宿主那半 = `ClearSelection()`；驱动已订阅 `CloseRequested`，见那个事件的 doc）。
        /// <para>🔴 **2026-10-20（W57 · `T09-FIX5` · P0-1）：先问「这一下是不是落在任何一张棋盘卡上」。**
        /// 是（目标 / 自己人 / 非目标都算）⇒ **不关面板、不发 `CloseRequested`**，把这一下留给世界侧
        /// （原版是卡自己的 `CardCollider.OnPointerClick` 先吃掉这一下 —— 判据见 `LeavePointerToWorld` 的 doc）。
        /// 不关的话，目标型主动技能在真机上**每一次点目标都会先把面板关掉**、松手那一帧
        /// `_selectedSlot` 已 −1 ⇒ 技能永远放不出来。</para></summary>
        void OnHidePointerDown(BaseEventData data)
        {
            _closeDownCount++;
            if (!Visible) return;
            var ped = data as PointerEventData;
            if (ped != null && LeavePointerToWorld != null && LeavePointerToWorld(ped.position))
            {
                _closePassCount++;
                return;                    // 🔴 P0-1：这一下归世界侧（点目标 / 释放技能），面板不关
            }
            Hide();                                   // 原版 `FadeOut`
            if (CloseRequested != null) CloseRequested();
        }

        /// <summary>把捕获区按**原版 `RT_3053` 的锚点五元组**摆到**面板节点**下（每次 `RefreshLayout` 重跑）。
        /// <para>🔴 **2026-10-20（W58 · `HideAbilityButton` 锚点补正）**：父 = `_panelRt`
        /// （原版 `ActiveSkillDesc` / `RT_3104` 那一级），⛔ **不是画布** ——
        /// 原版 `RT_3053.m_Father = 3104`（运行时 dump 的 path 也写着 `…/ActiveSkillDesc` 是父）。
        /// 旧实现按「画布底右锚」用 `CanvasPx` 换算到中心锚 ⇒ 与原版相差 **606.11 px**（面板 ap.x）。</para>
        /// <para>五元组**逐字照抄**原版：`aMin = aMax = (1,0)` · `pivot = (1,0)` ·
        /// `ap = (220.3900146484375, −146.0101776123047)` · `sd = (3707.830078125, 1985.3199462890625)`。
        /// ⇒ 「右下角」= **面板**右下角；矩形向左上铺开。</para>
        /// <para>⚠️ **不做 `ap/pivot` 换算**：报告 `C3C4` 里那条 `ap' = ap + (0.5 − pivot) × sd`
        /// 只在「把 pivot 改成 (0.5,0.5)」时才需要。本实现**保留原版 pivot `(1,0)`** ⇒ 直接写字面量
        /// （两种写法等价，但保留原版字段才能让断言逐字段对原版 JSON）。</para></summary>
        void PlaceHideButton()
        {
            if (_hideBtn == null || !UguiMode || _hideBtn.Rect == null) return;
            var rt = _hideBtn.Rect;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);   // 原版：**面板**右下角（点锚）
            rt.pivot = new Vector2(1f, 0f);                      // 原版 pivot（⛔ 别改成中心）
            rt.sizeDelta = new Vector2(HideBtnW, HideBtnH);
            rt.anchoredPosition = new Vector2(HideBtnAnchoredX, HideBtnAnchoredY);
        }

        /// <summary>输入层跟着显隐开关（**必须**：我们的件收起时**不 `SetActive(false)`**，只把 alpha 压到 0
        /// —— 见 `HideImmediate` 那段踩坑记录）。不关的话，一块**看不见**的全屏捕获区会一直吃点击
        /// （`Graphic.raycastTarget` 不看 alpha）。
        /// <para>原版同构：`FadeOut` / `InitialSetup` 把 `hideSkillBtn` `SetActive(false)`、
        /// `FadeIn` 再 `SetActive(true)`（`ActiveSkillDisplay__FadeIn.c:15-18`）。</para></summary>
        void SetInputEnabled(bool on)
        {
            if (!UguiMode) return;                    // 世界档没有射线件（`SetRaycastTarget` 会出声拒绝）
            if (_inputEnabled == on) return;
            _inputEnabled = on;
            if (_bg != null) _bg.SetRaycastTarget(on);
            if (_targets != null) SetLabelRaycast(_targets, on);
            if (_hideBtn != null && _hideBtn.gameObject != null) _hideBtn.gameObject.SetActive(on);
        }

        /// <summary>给一颗 `Label` 的 TMP 写 `raycastTarget`（`Label` 自己没有这个口 ——
        /// `TmpFont.NewTextUgui` 建出来默认 **false**）。
        /// <para>⚠️ **TMP 不在 `Label` 自己的 GO 上**：`Label.BuildTmp()` 把它建在**子节点 `text`**
        /// （`TmpFont.NewTextUgui` 是 `new GameObject(...)` 再 `SetParent`）⇒ 必须 `GetComponentInChildren`。
        /// 拿 `l.GetComponent<Graphic>()` 会永远 null（静默差一个件）。返回 false = 找不到那个 TMP。</para></summary>
        static bool SetLabelRaycast(Label l, bool on)
        {
            var g = LabelGraphic(l);
            if (g == null) return false;
            g.raycastTarget = on;
            return true;
        }

        /// <summary>`Label` 底下那颗 TMP 的 `Graphic`（见 `SetLabelRaycast` 的 doc）。没有 ⇒ `null`。</summary>
        static UnityEngine.UI.Graphic LabelGraphic(Label l)
        {
            if (l == null || l.transform == null) return null;
            return l.transform.GetComponentInChildren<UnityEngine.UI.Graphic>(true);
        }

        /// <summary>建一颗字。两档的字号**数不同**（世界档 = px ÷ 108、画布档 = px）——
        /// 口径 → `T09` §五·补⑨ 四（⛔ 喂错**不报错**、只是小到看不见）。
        /// <para>🆕 **2026-10-17（`A1024`③ 批 2–4）**：可选字体面，默认 `Cjk` ⇒ 旧调用点零行为变化。
        /// ⚠️ 必须排在 `SetGlyphHeight` **之前** —— 那个口按 `ActualFace` 的 em 比例折字号
        /// （判据 → `Label.TmpFontSize`）；两档都过这一处，⛔ 别在调用点各写一遍。</para></summary>
        Label MakeLabel(string name, float px, Color c, Vector2? anchor = null, TmpFont.Face face = TmpFont.Face.Cjk)
        {
            var a = anchor ?? new Vector2(0.5f, 0.5f);
            if (UguiMode)
            {
                // 位置由 `RefreshLayout` 写（建的时候先落在子画布中心）
                var u = HudCanvas.MakeText(_cv, "", 0f, 0f, 1, c, a, ZText, name, face);
                if (u != null) u.SetGlyphHeight(px);          // ⚠️ **设计 px**（⛔ 不除 108）
                return u;
            }
            var l = Label.Create(transform, "", Vector3.zero, 1, c, a, name);
            // 两档同一条口径：换面**在建 TMP 之后、定字号之前**（`MakeText` 内部已紧跟 `CreateUgui`）
            if (face != TmpFont.Face.Cjk) l.SetFace(face);
            l.SetGlyphHeight(WorldOfPx(px));
            return l;
        }

        /// <summary>1×1 纯色图。底板和色片都是纯色块 —— 原版那几个 sprite 本来就全是 null</summary>
        Texture2D Solid()
        {
            if (_solid == null)
            {
                _solid = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _solid.SetPixel(0, 0, Color.white);
                _solid.Apply();
                _solid.name = "solid_white";
            }
            return _solid;
        }

        static Color WithAlpha(Color c, float a) { c.a *= a; return c; }

        float PanelW { get { return LayoutSpace.VisibleWidth * (PanelX1 - PanelX0); } }
        float PanelH { get { return LayoutSpace.VisibleHeight * (PanelY1 - PanelY0); } }

        /// <summary>面板内归一化坐标（x 从左 0 到右 1，y 从**顶部** 0 到底部 1）→ 局部坐标</summary>
        Vector3 InPanel(float x01, float y01, float z)
        {
            return new Vector3((x01 - 0.5f) * PanelW, (0.5f - y01) * PanelH, z);
        }

        Vector3 CenterOf(Rect r, float z)
        {
            return InPanel(r.x + r.width * 0.5f, r.y + r.height * 0.5f, z);
        }

        /// <summary>
        /// 按当前分辨率摆一遍（幂等）。
        /// <para><b>世界档</b>：面板是**屏幕的固定比例**（30% 宽 × 30% 高），宽高比跟着屏幕走 ——
        /// 只摆一次的话切分辨率就错位（HUD 那几个 Label 踩过一模一样的坑）。</para>
        /// <para><b>画布档</b>：🔴 **2026-10-20（W58 · `T09` C4）后，面板那一级已经不再由本函数定位** ——
        /// 它是 `_panelRt`（原版 `ActiveSkillDesc` / `RT_3104` 的锚点五元组）**自己随父画布拉伸**，
        /// `PanelCvRect` 直接读它（见那个属性）。**本函数对画布档仍然必要**，但只剩一件事：
        /// **把「显式 `sizeDelta`」的那几件按新面板尺寸重写一遍** ——
        /// 4 颗自适应字的框宽/框高（`Label.SetAutoFitBox` 把框写进 `sizeDelta`）+
        /// `manaIcon` 的「方形 = min(w,h)」+ `Lights`/底板/字的位置（位置虽然可按锚点写，
        /// 但它们与框是同一次布局，合在这里一起落）。
        /// ⚠️ **删调用之前先解决上面那件事**（给 `Label` 一条「锚点框」的路）—— 否则 4:3 / 21:9 下
        /// 字框还停在 16:9 的 px 上（静默、只有非 16:9 才露）。换分辨率的既有入口 =
        /// `BattleDriver.ReanchorHud` → 本函数（那一句**照旧留着**，⛔ 别摘）。
        /// ⚠️ 它仍然幂等；同一宽高比下重跑 = 一个字节都不改。</para>
        /// </summary>
        public void RefreshLayout()
        {
            if (!_built) return;

            // 世界档：面板是**屏幕的固定比例**（30% 宽 × 30% 高），宽高比跟着屏幕走 ——
            //   只摆一次的话切分辨率就错位（HUD 那几个 Label 踩过一模一样的坑）。
            // 🔴 **2026-10-16（`T09` 批 3）**：画布档的件**不靠这一段定位**（它们跟 `PanelCvRect` 走）——
            // 🔴 **但这一句两边都留着**（⛔ 别按档跳过）—— 那个世界空间的**壳** `transform` 还要用：
            //   `BattleDriver` 拿 `SetPointer(sp.transform.position, …)` 当「指针按在面板上」那个点，
            //   而 `Contains` 画布档量的是**件真正画在哪**。两档下这个点都在命中框里：
            //   · 真正的 16:9 ⇒ **两档逐位重合**（面板中心 0.20293 vs 0.20295，差 2e−5 ≈ 0.0002 世界单位）；
            //   · 别的宽高比 ⇒ 差一个「面板随画布走」的量：**4:3 下壳在 −2.9705、面板中心在 −2.2280**
            //     （差 **0.74** 世界单位），而画布档那块面板的半高有 **1.51** 世界单位 ⇒ **仍落在框内**
            //     （`Editor/BattleScene.cs` 那条「指针按在面板上 → 铺蓝那层」靠的正是这一点）；
            //     **21:9 同理**（面板中心右移 1.84、半宽 3.56 ⇒ 壳仍在框内）。
            //   ⚠️ 如实记：**它不是「件画在哪」那个点**（画布档要那个点得走 `_cv.TransformPoint`，
            //   而批处理下画布还没排过版、那份反算不可信）—— 属「只换半边」那条已知代价。
            {
                var c = LayoutSpace.ToWorld((PanelX0 + PanelX1) * 0.5f, (PanelY0 + PanelY1) * 0.5f);
                transform.localPosition = new Vector3(c.x, c.y, ZPanel);
            }

            PlaceQuad(_bg, BgRect, ZBg);
            // ⚠️ `_light` **两档同值**（原版 `Lights` 的锚点盒）—— 2026-10-16 主对话裁定删掉了那颗
            //    「世界档冻结值」，见 `LightRect` 上面那段更正痕。
            PlaceQuad(_light, LightRect, ZLight);

            // 卡名 / 费用 / 目标数：居中放进各自的矩形
            Place(_name, NameRect, ZText);
            Place(_cost, CostTextRect, ZText);
            Place(_targets, TargetsRect, ZText);

            // 说明：**左对齐**，起笔位置 = 矩形左边 + 原版那个 margin
            // 🔴 **2026-10-16（锚点补正）**：那一格改用 `MarginX01()` —— 画布档的**面板宽**现在会随
            //   宽高比变（`Expand` ⇒ 21:9 下画布宽 2560、面板宽 768.54 px），而原版那个
            //   `m_margin.x = 7.4727` 是**文字自己的 px**（= canvas px，**不随面板缩放**）
            //   ⇒ 画布档必须按**现算的面板宽**折（用写死的 `PanelPxW` 会在 21:9 下差 2.5 px，
            //   而且会与下面 `FitBox(... - MarginW())` 那条框**对不上** —— `MarginW` 走的是固定 px）。
            Place(_desc, DescRect.x + MarginX01(),
                        DescRect.y + DescRect.height * 0.5f, ZText);

            // ⚠️ 原版四个字都开着 `m_enableAutoSizing`（min 4 / max 60），序列化字号是
            //    **编辑器烘进去的「上次结果」**。
            // 🆕 **2026-10-10（A1212 块 7）**：四颗各自按 `SetAutoFitBox` 开原版那套自适应（`4/60/36`，见 `AutoMinPx` 那一组常量）。
            // 🔴 **2026-10-19（`A1237①`）**：原来那句「再调一次 `Fit` 兜底」**已删**（它是反馈环，理由见本段末尾那四行）。
            // 🔴 **2026-10-16（`T09` 批 3）**：框的两个量纲**按档取**（`BoxW/BoxH`）——
            //    `SetAutoFitBox` 的第一个形参是「**本标签所在空间的单位**」（`Label.SetWrapWidth` 那句
            //    `sizeDelta` 直接吃它）⇒ 世界档喂世界单位、画布档喂**设计 px**。
            //    ⛔ 喂错**不报错**：画布档喂世界单位 ⇒ 框宽 5.34 px（而不是 576 px）、字收敛到一个小得看不见的值。
            FitBox(_name, BoxW(NameRect.width), BoxH(NameRect.height));
            // 🔴 **2026-10-17（`A1226` 表 B #24）：`CostText` 的框宽改成【原版有效框】56.20。**
            //    判据 = 原版 `…/AbilityContainer/CostText` 的 `RectTransform`（伸锚）⇒ **56.20 × 68.40**
            //    （我们原来的归一化矩形量出来 66.58 × 99.85 ⇒ 宽 +10.38 / 高 +31.45）。
            //    ⚠️ **只跟宽、框高【故意不跟】**：68.40 是 Pragati 行盒 0.9474em 下的数（52.4px 的字占 49.6 < 68.4，
            //    原版不缩）；我们全仓只有 CJK 那一份字体（行盒 **1.516em**）⇒ 喂 68.40 会压到 45.1px（原版渲 52.4）。
            //    ⇒ 等 `A1024③` 按用途换面之后，框高再一起改（已在 T07 报告登记）。
            FitBox(_cost, UguiMode ? 56.20f : WorldOfPx(56.20f), BoxH(CostTextRect.height));
            FitBox(_targets, BoxW(TargetsRect.width), BoxH(TargetsRect.height));
            FitBox(_desc, BoxW(DescRect.width) - MarginW(), BoxH(DescRect.height));

            // 🔴🔴 **2026-10-19（`A1237①`）：原来跟在这四句后面的 `Fit(...)` 已【删掉】—— 它不是兜底，是**反馈环**。**
            //    判据：`Fit` 读 `WorldW/H` 与 `BoxW/H`，装不下就 `SetGlyphHeight(GlyphHeightWorld × k)`；
            //    而 `SetGlyphHeight` 改的是**基号**（`_glyphHeight`），`TmpFontSize()` 下一趟就按它折
            //    `min/max/base` ⇒ **连调两次 `RefreshLayout` 会把字号越缩越小**（静默、无报错）。
            //    而它想兜的那件事（装不下 ⇒ 缩）**已经由 TMP 的自适应做了**（`SetAutoFitBox` 的
            //    `m_fontSizeMin/Max` 二分）⇒ 收敛成功时 `k >= 1`、它本来就该早退。
            //    ⛔ 别再把它加回来；「版面幂等」由 `Editor/BattleScene.cs` 那条新断言咬住
            //    （连调两次 `RefreshLayout` ⇒ 四颗的 `FontPxNow` 逐位不变）。

            PlaceManaIcon();
            // 🆕 2026-10-20（`T09` A3-e / W58）：捕获区五元组 = 原版 `RT_3053`（**相对面板节点**的底右点锚）
            PlaceHideButton();
            ApplyColors();
        }

        /// <summary>面板内归一化宽度 → **本档的尺寸单位**（世界档 = 世界单位 · 画布档 = 设计 px）。
        /// ⛔ **别在别处自己乘** —— 两档混一次就是「只换半边」（`T09` §五·补⑦ 那条）。</summary>
        float BoxW(float x01) { return UguiMode ? Wpx(x01) : W01(x01); }
        /// <inheritdoc cref="BoxW"/>
        float BoxH(float y01) { return UguiMode ? Hpx(y01) : H01(y01); }
        /// <summary>`DescText` 那个左对齐 margin，**本档的单位**（原版值是 7.4727 **px**）。</summary>
        float MarginW() { return UguiMode ? DescMarginPx : WorldOfPx(DescMarginPx); }
        /// <summary>`DescText` 那个 margin 折成**面板内归一化 x**（给 `Place(_desc, …)` 用）。
        /// <para>原版 `m_margin.x = 7.4727 px` 是**文字自己的 px**（= canvas px、**不随面板尺寸缩放**）⇒
        /// <b>画布档按现算的面板宽折</b>（⛔ 别用写死的 `PanelPxW` —— 21:9 下面板宽 768.54，
        /// 写死会让起笔位置偏 2.5 px，并与 `FitBox` 那条框对不上）。</para>
        /// <para>⚠️ **世界档那一支逐位不变**（就是原来的 `DescMarginPx / PanelPxW`）—— 它的尺是
        /// `PanelW`（屏幕比例 × 30%），改成 ÷`PanelW` 会让世界档在非 16:9 下位移（违「世界档逐位不变」）。</para></summary>
        float MarginX01()
        {
            if (!UguiMode) return DescMarginPx / PanelPxW;
            return DescMarginPx / Mathf.Max(1e-4f, PanelCvRect.width);
        }

        float W01(float x01) { return x01 * PanelW; }
        float H01(float y01) { return y01 * PanelH; }

        /// <summary>面板内归一化宽度 → **设计 px**（**画布档的尺** —— 尺本身 = 现算的面板宽）。
        /// ⛔ 别与 `W01`（世界档的尺）混。</summary>
        float Wpx(float x01) { return x01 * PanelCvRect.width; }
        /// <inheritdoc cref="Wpx"/>
        float Hpx(float y01) { return y01 * PanelCvRect.height; }

        // ── 🆕 2026-10-16（`T09` 批 3）：画布档的坐标/尺寸（**与建件那一侧成对的逆**）────────────
        /// <summary>🔴 **2026-10-16（`T09` 批 3 · 锚点补正）：画布档的【父尺寸】= 子画布自己的 rect**
        /// （设计 px = canvas 单位；`_cv` 四边贴合根画布 ⇒ 它的 `rect` 就是画布矩形）。
        /// <para>`Expand` 档下**画布宽恒 1920 单位**、**高随屏变**（4:3 ⇒ 1440、16:9 ⇒ 1080、
        /// 21:9 ⇒ 宽 2560）—— 这正是「x 轴的拉伸与固定 px 在 4:3/16:9 下数值相同、要到比 16:9
        /// 更宽的屏才分家，而 y 轴在 4:3 就立刻有差」那条的来源。</para>
        /// <para>⚠️ **退化（`rect` 量不到，批处理里画布没排过版时是 0×0）⇒ 退参考分辨率
        /// `HudCanvas.ReferenceResolution`（1920×1080）** —— 不退的话 `size = span×0 + sd`
        /// 会算出**负的/零的面板**、整块面板**当场塌掉**（静默、且只在批处理里现形）。
        /// 退档是**出声一次**（⛔ 不静默）。</para></summary>
        Vector2 CanvasPx
        {
            get
            {
                var r = _cv != null ? _cv.rect : new Rect(0f, 0f, 0f, 0f);
                if (r.width >= 1f && r.height >= 1f) return new Vector2(r.width, r.height);
                if (!_warnedDegenerateCanvas)
                {
                    _warnedDegenerateCanvas = true;
                    Debug.LogWarning("[Battle] 技能面板：子画布的 `rect` 还量不到（批处理下画布没排过版）"
                                   + " ⇒ 面板按**参考分辨率** " + HudCanvas.ReferenceResolution.x + "×"
                                   + HudCanvas.ReferenceResolution.y + " 排版（= 16:9、与改之前逐位相同）。"
                                   + "真机有渲染时不会走这一支。");
                }
                return HudCanvas.ReferenceResolution;
            }
        }
        bool _warnedDegenerateCanvas;

        /// <summary>🔴🔴 **2026-10-20（W58 · `T09` C4）：画布档的【面板矩形】现在读【锚点节点】** ——
        /// `_panelRt`（原版 `ActiveSkillDesc` / `RT_3104` 那一级）**自己随父画布拉伸**；
        /// 本属性只把它的结果换算成 `_cv` 局部坐标（中心原点、y 向上）。
        /// <para>**实测支**（正常路径）：`size = rt.rect.size`、矩形左下 = `rt.localPosition + rt.rect.min`
        /// —— 那是 **Unity 按锚点排出来的真值**（换分辨率/换父尺寸由布局系统自己更新，⛔ 不经过我们的算式）。
        /// ⚠️ **它要与锚点字段那支互校**（尺寸/原点差 ≤ 2 px 才采信）：防「整流排了一半」那一瞬
        /// （父画布尺寸刚变、子节点 `localPosition` 还没跟上）静默偏一整格（面板中心 y ≈ −320 px）。</para>
        /// <para>**退档支**（批处理里画布还没排过版 ⇒ `rect` 0×0，见 `CanvasPx` 的 doc）：按 `_panelRt`
        /// **自己的锚点字段** + `CanvasPx`（它再退参考分辨率）算同一格。两支在 16:9 下逐位相同
        /// （自证 → `PanelPxW/H` 的 doc 与 §④）。</para>
        /// <para>🔑 **灭自证（两个独立来源）**：实测支读 Unity 的布局结果，退档支读序列化的锚点五元组；
        /// 断言侧的期望值又是**从原版 JSON 抄来的字面量**（⛔ 不读本属性）⇒ 三处一起写错才会假绿。</para>
        /// <para>⚠️ **`RefreshLayout` 仍然要跑**（唯一原因）：4 颗自适应字的框宽 / 框高 + `manaIcon` 的方形
        /// 走的是**显式 `sizeDelta`**（`Label.SetAutoFitBox` 的语义），**不随锚点拉伸**
        /// ⇒ 换宽高比时仍要按新面板尺寸重写一遍。本属性只保证「面板那一格」是锚点真值、
        /// 不再在每次读的时候拿画布尺寸重算一遍。</para></summary>
        Rect PanelCvRect
        {
            get
            {
                var rt = _panelRt;
                if (rt != null)
                {
                    // ── 锚点字段 → 期望矩形（**面板节点自己的五个字段** + 画布尺寸；退档支与互校支共用）──
                    var c = CanvasPx;
                    float aMinX = rt.anchorMin.x, aMinY = rt.anchorMin.y;
                    float aMaxX = rt.anchorMax.x, aMaxY = rt.anchorMax.y;
                    float pvX = rt.pivot.x, pvY = rt.pivot.y;
                    float apX = rt.anchoredPosition.x, apY = rt.anchoredPosition.y;
                    float sdX = rt.sizeDelta.x, sdY = rt.sizeDelta.y;
                    float spanX = aMaxX - aMinX, spanY = aMaxY - aMinY;
                    var size = new Vector2(spanX * c.x + sdX, spanY * c.y + sdY);
                    var center = new Vector2(-c.x * 0.5f + (aMinX + spanX * pvX) * c.x + apX,
                                             -c.y * 0.5f + (aMinY + spanY * pvY) * c.y + apY);

                    // ── 实测支：Unity 按锚点排出来的 `rect` + `localPosition`（互校不过 ⇒ 不信它）──
                    //   ⚠️ 互校不是「两边差不多就行」——它是**防「整流排了一半」**：批处理/编辑模式下
                    //   父画布尺寸刚变、子节点的 `localPosition` 还没跟上的那一瞬，直接用会静默偏一整格
                    //   （面板中心 y ≈ −320 px）。偏差 > 2 px ⇒ 退回锚点字段那一格（与旧实现逐位相同）。
                    var rr = rt.rect;
                    if (rr.width >= 1f && rr.height >= 1f)
                    {
                        var p = rt.localPosition;
                        float dx = p.x + rr.x - (center.x - size.x * 0.5f);
                        float dy = p.y + rr.y - (center.y - size.y * 0.5f);
                        if (Mathf.Abs(rr.width - size.x) <= 2f && Mathf.Abs(rr.height - size.y) <= 2f
                            && Mathf.Abs(dx) <= 2f && Mathf.Abs(dy) <= 2f)
                            return new Rect(p.x + rr.x, p.y + rr.y, rr.width, rr.height);
                    }
                    // ── 退档支（`rect` 0×0 / 整流未完成）：锚点字段那一格 ──
                    return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
                }
                // 兜底（**不该发生**：画布档一定建了 `_panelRt`）—— 退到那组原始常量，并出声一次。
                if (!_warnedNoPanelNode)
                {
                    _warnedNoPanelNode = true;
                    Debug.LogWarning("[Battle] 技能面板：`PanelCvRect` 读不到锚点节点 `ActiveSkillDesc`"
                                   + " ⇒ 暂按 `CvAnchor*` 那组常量算（画布档不该走到这里；世界档不读本属性）");
                }
                var c2 = CanvasPx;
                float span2X = CvAnchorMaxX - CvAnchorMinX, span2Y = CvAnchorMaxY - CvAnchorMinY;
                var size2 = new Vector2(span2X * c2.x + CvSizeDeltaX, span2Y * c2.y + CvSizeDeltaY);
                var center2 = new Vector2(-c2.x * 0.5f + (CvAnchorMinX + span2X * CvPivotX) * c2.x + CvAnchoredX,
                                          -c2.y * 0.5f + (CvAnchorMinY + span2Y * CvPivotY) * c2.y + CvAnchoredY);
                return new Rect(center2.x - size2.x * 0.5f, center2.y - size2.y * 0.5f, size2.x, size2.y);
            }
        }
        /// <summary>上面最后那支兜底的「出声一次」闩（诊断用；正常路径恒 false）。</summary>
        bool _warnedNoPanelNode;

        /// <summary>面板内归一化（x 从左 0→右 1，y 从**面板顶** 0→底 1）→ **子画布局部**
        /// （中心原点、**y 向上**、设计 px）。给字与 `localPosition` 用。
        /// <para>⚠️ 与 <see cref="InPanel"/>（世界档那一份）**不是同一个坐标系** —— 两档在 16:9 下逐位相同，
        /// 别的宽高比下：**画布档跟着画布（= 原版口径）走**，世界档跟着屏幕比例走。</para></summary>
        Vector3 CvPos(float x01, float y01)
        {
            var r = PanelCvRect;                      // 🔴 现算（判据见它自己的 doc）
            return new Vector3(r.xMin + x01 * r.width,
                               r.yMax - y01 * r.height,   // `y01` 从**面板顶**算 ⇒ 从 `yMax` 往下减
                               0f);                       // 🔴 z **恒 0**：画布上写 z 会把件推离平面
        }

        /// <summary>画布档：直接定一个件的**设计 px** 尺寸（`sizeDelta`）。
        /// ⚠️ 世界档要走 `SetWorldHeight` + `localScale.x` 那一套（见 <see cref="PlaceQuad"/>）—— 两回事。</summary>
        static void SizePx(HudPiece q, float wPx, float hPx)
        {
            if (q == null || !q.IsUgui || q.Rect == null) return;
            q.Rect.sizeDelta = new Vector2(wPx, hPx);
        }

        /// <summary>画布档：按**高**定尺寸、**保持当前宽高比**（= 世界档 `SetWorldHeight` 的对应物）。</summary>
        static void SetHeightCv(HudPiece q, float hPx)
        {
            if (q == null || !q.IsUgui || q.Rect == null || !(hPx > 0f)) return;
            var d = q.Rect.sizeDelta;
            float a = d.y > 0f ? d.x / d.y : 1f;
            q.Rect.sizeDelta = new Vector2(hPx * a, hPx);
        }

        /// <summary>把一个件摆到面板内那个**归一化点**上（`x01` 从左、`y01` 从**面板顶**算）。
        /// 两档各走各的坐标系（世界档 `InPanel` / 画布档 `CvPos`）——
        /// ⛔ **别在调用点自己写 `localPosition`**（画布上写 z 会把件推离平面）。</summary>
        void Place(Transform t, float x01, float y01, float z)
        {
            if (t == null) return;
            t.localPosition = UguiMode ? CvPos(x01, y01) : InPanel(x01, y01, z);
        }
        void Place(HudPiece q, float x01, float y01, float z) { if (q != null) Place(q.transform, x01, y01, z); }
        void Place(Label l, float x01, float y01, float z) { if (l != null) Place(l.transform, x01, y01, z); }
        void Place(HudPiece q, Rect r, float z) { Place(q, r.x + r.width * 0.5f, r.y + r.height * 0.5f, z); }
        void Place(Label l, Rect r, float z) { Place(l, r.x + r.width * 0.5f, r.y + r.height * 0.5f, z); }

        /// <summary>费用图标那一格（原版是 `manaIcon`，`m_PreserveAspect = 1`）：
        /// **方图居中放进它的矩形** —— 边长 = `min(宽,高)`，宽再按图自己的比例给（同世界档）。
        /// 判据（那个 `side`）**只此一处**，两档各走各的尺寸口。</summary>
        void PlaceManaIcon()
        {
            if (_manaIcon == null) return;
            if (UguiMode)
            {
                float side = Mathf.Min(Wpx(CostIconRect.width), Hpx(CostIconRect.height));
                SetHeightCv(_manaIcon, side);
            }
            else
            {
                float side = Mathf.Min(W01(CostIconRect.width), H01(CostIconRect.height));
                _manaIcon.SetWorldHeight(side);
            }
            Place(_manaIcon, CostIconRect, ZText);
        }

        /// <summary>把一块纯色 quad 摆进面板内那个矩形。
        /// 世界档：`ImageQuad` 是按「高 + 图的比例」定尺寸的（1×1 的图出来是正方形），
        /// 所以横向再拉一下才成矩形（原版那几块也都是非等比的 Image）。
        /// 🔴 **画布档**：尺寸 = 设计 px 直接进 `sizeDelta`（`Image.preserveAspect = 0` ⇒ 拉满矩形，
        /// 见 `MakeImageInParent`）⇒ **不需要** `SetAspect` 那一步。</summary>
        void PlaceQuad(HudPiece q, Rect r, float z)
        {
            if (q == null) return;
            if (UguiMode)
            {
                SizePx(q, Wpx(r.width), Hpx(r.height));
                Place(q, r, z);
                return;
            }
            q.SetWorldHeight(H01(r.height));
            q.transform.localPosition = CenterOf(r, z);
            q.transform.localScale = new Vector3(W01(r.width) / Mathf.Max(1e-4f, q.WorldW), 1f, 1f);
        }

        /// <summary>
        /// 🆕 **2026-10-10（A1212 块 7）**：给这四颗字接上**原版那一套自适应**（`4/60/36`）。
        /// 框宽/框高走**本档的尺寸单位**（`Label.SetAutoFitBox` 的 doc：它写的是 `sizeDelta`，
        /// 单位随档变）—— 调用点一律走 `BoxW/BoxH`（世界档 = 世界单位 · 画布档 = 设计 px），
        /// ⛔ **别在这里自己乘系数**；`min/max/base` 恒走**原版画布 px**、三个数原样传
        /// （出处见 `AutoMinPx` 那一组常量的注释）。
        /// <para>⚠️ 它会**重排 mesh**（`ForceMeshUpdate`）⇒ 建件之后要再摆位/裁切的，排在本函数之后。</para>
        /// </summary>
        static void FitBox(Label l, float boxW, float boxH)
        {
            if (l == null) return;
            if (!(boxW > 0f) || !(boxH > 0f)) return;      // 退化框：不接（`SetAutoFitBox` 对 `worldH<=0` 也不写框高）
            l.SetAutoFitBox(boxW, boxH, AutoMinPx, AutoMaxPx, AutoBasePx);
        }

        // 🔴 **2026-10-19（`A1237①`）：`Fit(Label, maxW, maxH)` 已整块删除。**
        //    它不是「装不下就缩」的兜底，而是**反馈环**：`SetGlyphHeight` 改的是基号（`_glyphHeight`），
        //    `TmpFontSize()` 下一趟按它折 `min/max/base` ⇒ 连调两次 `RefreshLayout` 字号越缩越小，
        //    而且静默。原版那件事由 `SetAutoFitBox` 的 TMP 自适应完成（判据与断言 → `RefreshLayout` 里那段注释）。

        /// <summary>面板在**屏幕上**占的那块矩形（指针在不在面板上用它判）。
        /// 🔴 **2026-10-16（`T09` 批 3）：按档取坐标系** ——
        /// 世界档 = 壳 `transform` 的局部（世界单位、原点在面板中心）；
        /// **画布档 = 子画布 `_cv` 的局部**（设计 px、原点在**画布中心**）⇒ 与「件真正画在哪」同一个变换
        /// （跟着 `CanvasScaler` 走）⇒ **任何宽高比下都咬合**。
        /// ⛔ **画布档别再用 `transform`** —— 那是**世界空间那层壳**（`BattleDriver` 还在用它交指针点），
        /// 非 16:9 下两个帧差 1.333 倍 ⇒ 会变成「看得见的地方点不到」（`T09` §五·补⑦ 那条）。
        /// 🔴 **2026-10-16（锚点补正）**：画布档的框**不再是那对固定 px 常量**，而是
        /// <see cref="PanelCvRect"/>（跟着画布高走）—— 否则 4:3 下「画出来的」会跑到「命中框」外面去。</summary>
        public bool Contains(Vector3 world)
        {
            if (UguiMode)
            {
                var p = _cv.InverseTransformPoint(world);
                var r = PanelCvRect;
                return Mathf.Abs(p.x - r.center.x) <= r.width * 0.5f
                    && Mathf.Abs(p.y - r.center.y) <= r.height * 0.5f;
            }
            var l = transform.InverseTransformPoint(world);
            return Mathf.Abs(l.x) <= PanelW * 0.5f && Mathf.Abs(l.y) <= PanelH * 0.5f;
        }

        // ---- 🆕 2026-10-18（A964① ⑥）：面板的**实绘几何**只读口（⛔ 只给自检，不给生产用）----
        //  补它的理由：上面 `Contains` 用的是 `PanelW/PanelH`（**手算**：屏幕比例 × 30%），
        //  而**画出来的**是 `_bg`（只占面板 84.4%×68.1%）与 `_light`（93.2%×84.7%）两颗 quad
        //  ⇒ 没有实绘口就**无从比**「命中区 ⊇ 实绘」。
        //  🔴 **读法必须是 `WorldW/H × lossyScale`**：`PlaceQuad` 是「先按高定尺寸、再用
        //     `transform.localScale.x` 横向拉到目标宽」⇒ **`WorldW` 是拉伸【前】的值**
        //     （`_light` 的 `WorldW` 只有 0.8468×面板高，而它真正画出来是 0.9322×面板宽）。
        //     只读 `WorldW` 会把实绘宽量小 10% —— 那是「量错」不是「原版如此」。
        //  ⛔ 本件**不改** `Contains` 的口径（改命中＝改行为，要另开一件、另配断言）；
        //     它仍然只用于 `SetPointer` 的按下态着色。
        //  🔴 **2026-10-16（`T09` 批 3）：画布档那一支读 `sizeDelta` × `lossyScale`** ——
        //     画布上的件**没有** `localScale` 那次横向拉伸（尺寸本身就是 `sizeDelta`，
        //     见 `PlaceQuad` 的画布分支），而 `sizeDelta` 的单位是**设计 px**；
        //     乘 `lossyScale`（= 画布那一跳：设计 px → 世界单位）就回到**世界单位** ——
        //     **两个口因此量的是同一件事**（`E2⑥` 那条「命中区 ⊇ 实绘」照旧成立）。
        //     ⚠️ 本件这两口**契约恒为「世界单位」**（两档都是）—— 因为它们是**跨件**比较用的
        //     （比 `Contains` 那个世界框）。⛔ 别改成 px（那会让 `E2⑥` 拿两种尺比）。

        static Vector2 DrawnWorldSize(HudPiece q)
        {
            if (q == null) return Vector2.zero;
            var s = q.transform.lossyScale;
            if (q.IsUgui)
            {
                var d = q.Rect != null ? q.Rect.sizeDelta : Vector2.zero;
                return new Vector2(d.x * s.x, d.y * s.y);
            }
            return new Vector2(q.WorldW * s.x, q.WorldH * s.y);
        }

        /// <summary>底板 `AbilityContainer` 的**实绘**世界宽 × 高。</summary>
        public Vector2 BgDrawnSize { get { return DrawnWorldSize(_bg); } }

        /// <summary>底板 `AbilityContainer` 的世界中心（**不是**面板中心 —— 它偏左上，见 `BgRect`）。</summary>
        public Vector3 BgWorldPos
        {
            get { return _bg != null ? _bg.transform.position : Vector3.zero; }
        }

        /// <summary>`Lights` 色片的**实绘**世界宽 × 高 —— 它是**画得最大**的那一层
        /// （93.2% × 84.7% 的面板，见 `LightRect`），所以 `E2` 拿它当「实绘矩形」的尺子。
        /// ⚠️ **两档现在同值**（2026-10-16 主对话裁定删掉了那颗「世界档冻结值」）。</summary>
        public Vector2 LightDrawnSize { get { return DrawnWorldSize(_light); } }

        /// <summary>`Lights` 色片的世界中心。</summary>
        public Vector3 LightWorldPos
        {
            get { return _light != null ? _light.transform.position : Vector3.zero; }
        }

        /// <summary>命中区（<see cref="Contains"/>）自己的世界宽 × 高 —— 就是那对手算的
        /// `PanelW/PanelH`（**世界单位**；⛔ 不是那两颗实绘口量的东西 —— 它是 `Contains` 用的那个框）。
        /// 探针要拿它算「多远算面板之外」（`Contains` 是绕面板中心的半边长）。
        /// 🔴 **2026-10-16（`T09` 批 3）**：画布档下 `Contains` 量的框 = **面板的设计 px 矩形**
        /// （跟着 `CanvasScaler` 缩放）⇒ 这里按**子画布那一跳**折算成世界单位
        /// （乘 `_cv.lossyScale`）—— 这样它和 <see cref="LightDrawnSize"/> / <see cref="BgDrawnSize"/>
        /// **同一把尺**（两个口都恒为世界单位），`E2⑥` 那条「命中区 ⊇ 实绘」照旧可比。
        /// 🔴 **2026-10-16（锚点补正）**：那一对 px 现在是 <see cref="PanelCvRect"/> 的 `size`
        /// （**跟着画布高走**，不再写死 `PanelPxW/H`）—— 与 `Contains` 读的**同一个矩形**。</summary>
        public Vector2 HitBoxWorldSize
        {
            get
            {
                if (UguiMode)
                {
                    var s = _cv != null ? _cv.lossyScale : Vector3.one;
                    var r = PanelCvRect;
                    return new Vector2(r.width * s.x, r.height * s.y);
                }
                return new Vector2(PanelW, PanelH);
            }
        }

        // ==================================================================
        //  显示
        // ==================================================================

        /// <summary>
        /// 弹出来。`cardName` 当技能名（原版有独立的技能名 `"Fire Arrow"`，我们还没有那个字段）、
        /// `ability` 是效果、`targets` 是**现在有几个合法目标** —— 由 `BattleDriver` 数好传进来，
        /// 面板不自己判规则。
        /// </summary>
        public void Show(string cardName, EffectSpec ability, int targets)
        {
            Build();
            ShownName = cardName;
            ShownTargets = targets;
            // 🔴 2026-10-25（`N4`）：开面板 ⇒ acting 白层**归零并掐掉在飞的补间**（原版每次 `ShowActingLight`
            //   都是「SetActive(true) 后从当前 α 淡到 1」，而面板新开时那颗件是 0.392 的静态态）。
            _acting = false;
            _actingA = 0f;
            if (_actingFade != null) { _actingFade.Kill(); _actingFade = null; }

            if (_name != null) _name.SetText(cardName);
            if (_desc != null) _desc.SetText(CardText.EffectSentence(ability));
            // 🔴 2026-10-24（`E17`）：`TargetsAvailableText` 不参与淡入淡出 ⇒ 它靠「开关 GameObject」
            //   藏，**必须在写字/排版之前打开**（TMP 在未激活对象上排不出网格，见 `HideImmediate` 那段踩坑）。
            SetTargetsShown(true);
            if (_targets != null) _targets.SetText(CardText.TargetsAvailable(targets));
            // 我们的技能**不吃能量、吃一次行动** —— 原版这一格是能量费。照实填 0，
            // 不把行动费伪装成能量费（以后技能真收能量了，这格已经接好）
            if (_cost != null) _cost.SetText("0");

            Visible = true;
            // 🆕 2026-10-20（`T09` A3-e）：原版 `FadeIn` 会在显示时把捕获区 `SetActive(true)` —— 同构
            SetInputEnabled(true);
            RefreshLayout();
            FadeTo(1f, FadeTime);
        }

        /// <summary>合法目标数变了就更新那一行和色片（判据在 `BattleDriver`，这儿只显示）</summary>
        public void SetTargetsAvailable(int n)
        {
            ShownTargets = n;
            // 🔴 2026-10-24（`E17`）：这一件收起时是**整个 GameObject 关掉**的（它不参与淡入淡出），
            //   而 TMP 在未激活对象上排不出网格 ⇒ 关着的时候别写它（`Show()` 打开时会重设一次）。
            if (_targets != null && _targets.gameObject != null && _targets.gameObject.activeSelf)
                _targets.SetText(CardText.TargetsAvailable(n));
            ApplyColors();
        }

        /// <summary>🆕 **2026-10-18（第十三轮 · G2b）**：那一行**现在印出来的字**（自检用）。
        /// 权威 = 原版词条 `Battle/HUD/TargetsAvailable`（`CardText.TargetsAvailable` → `Loc.T` +
        /// `{0}` 替换；拼法与出处见 `Core/Loc.cs` 那一块）⇒ 切语档后重设一次再读它，两档分得出。</summary>
        public string TargetsText { get { return _targets != null ? _targets.Text : null; } }

        /// <summary>指针喂进来（`down` = 这一帧按着）—— 决定要不要铺蓝色那层</summary>
        public void SetPointer(Vector3 world, bool down)
        {
            bool p = Visible && down && Contains(world);
            if (p == _pressed) return;
            _pressed = p;
            ApplyColors();
        }

        /// <summary>技能开始结算 → 铺白那层（原版 `ShowActingLight()`）。
        /// <para>🔴 **2026-10-25（`N4`）**：原来是「瞬间铺满」，而原版是
        /// `actingLight.SetActive(true)` + `DOFade(1, 0.4, OutQuad)`（判据 → <see cref="ActingFadeIn"/>）。
        /// ⇒ 现在 α 从**序列化值 0.392** 补间到 **1**（`DOFade` 从当前 color.a 出发，⛔ 不是从 0）。
        /// ⚠️ 起始值**必须同步写进去**（补间第一次推进前不写 setter）—— 否则批处理 Manual 档下
        /// 同一帧读到的 `CurrentLight` 会退回 `Available`。</para></summary>
        public void SetActing()
        {
            _acting = true;
            // 🔴 起点取 `max(当前 α, 0.392)`：原版 `DOFade` 是「从当前 color.a 出发」⇒
            //   重复点亮（例如确认释放后又走执行那一跳）**不会先掉回 0.392 再涨回去**（那是可见的闪一下）。
            _actingA = Mathf.Max(_actingA, LightActing.a);
            ApplyColors();
            FadeActingTo(1f, ActingFadeIn, false);
        }

        /// <summary>技能结算完 / 取消 → 白层淡回 0（原版 `ActiveSkillDisplay.FinishActing`：
        /// `actingLight.DOFade(0, 0.3, OutQuad)`，判据 → <see cref="ActingFadeOut"/>）。
        /// <para>⚠️ **今天没有调用点**（2026-10-25 `N4` 如实记）：原版它挂在 `BattleManager.CancelActiveAbility`
        /// 那条链上，而我们那条链是 `BattleDriver.ClearSelection()`（它同时 `skillPanel.Hide()` 把整个面板
        /// 0.1 s 淡掉）⇒ 白层的 0.3 s 淡出会被面板的整体淡出**先盖掉**，可见性要先把「哪条链该调它」定下来
        /// （改动请求写在 `资料/完全复刻/报告/N4_HUD_C_第1轮.md` §③）。⛔ 别为了让它「有人调」去随便挂一处。</para></summary>
        public void FinishActing()
        {
            if (!_acting) return;               // 没在 acting（白层本来就 0）⇒ 别起一条 0→0 的空补间
            _acting = false;
            FadeActingTo(0f, ActingFadeOut, true);
        }

        /// <summary>acting 白层的 α 补间（淡入 / 淡出两档共用一条口，⛔ 别在调用处各写一份 DOTween）。</summary>
        void FadeActingTo(float target, float duration, bool clearWhenDone)
        {
            if (_actingFade != null) { _actingFade.Kill(); _actingFade = null; }
            _actingFade = DOTween.To(() => _actingA, v => { _actingA = v; ApplyColors(); }, target, duration)
                                 .SetEase(Ease.OutQuad)          // 原版 `SetEase(6)` = Ease.OutQuad
                                 .SetUpdate(CardTween.Mode)
                                 .SetLink(gameObject)
                                 .OnComplete(() =>
                                 {
                                     _actingFade = null;
                                     if (clearWhenDone) { _acting = false; _actingA = 0f; }
                                     ApplyColors();
                                 });
        }

        /// <summary>
        /// 三态归一到「现在铺哪一层」。**判据只有这一份** —— 别在调用处各自决定。
        /// 优先级照原版：结算 &gt; 按下 &gt; 可用。
        /// </summary>
        void ApplyColors()
        {
            // 🔴 2026-10-25（`N4`）：acting 那一档按**补间的当前 α**（`_actingA`）判，⛔ 不只看 `_acting` ——
            //   原版白层在 `FinishActing` 里是「0.3 s 淡到 0」，那 0.3 s 里灯**仍然是白的**（只是越来越淡）。
            CurrentLight = _actingA > 1e-4f ? Light.Acting
                         : _pressed ? Light.Pressed
                         : (ShownTargets > 0 ? Light.Available : Light.None);

            if (_light != null)
            {
                Color c;
                switch (CurrentLight)
                {
                    case Light.Acting:
                        c = LightActing;
                        c.a = _actingA;              // 白层的 α 由补间说了算（0.392 → 1 淡入 / → 0 淡出）
                        break;
                    case Light.Pressed: c = LightPressed; break;
                    case Light.Available: c = LightAvailable; break;
                    default: c = new Color(0, 0, 0, 0); break;
                }
                _light.SetTint(WithAlpha(c, Alpha));
            }

            if (_bg != null) _bg.SetTint(WithAlpha(BgColor, Alpha));
            if (_name != null) _name.SetColor(WithAlpha(NameColor, Alpha));
            if (_cost != null) _cost.SetColor(WithAlpha(CostColor, Alpha));
            if (_desc != null) _desc.SetColor(WithAlpha(DescColor, Alpha));
            // 🔴 2026-10-24（`E17`）：**这一件不乘 `Alpha`** —— 原版 `SetAlpha`/`FadePower` 动的 6 件里
            //   **没有** `targetsAvailableText`（`+0x78` 从来没被写过 α；`__SetAlpha.c:40-49` · `__FadePower.c:20-42`）。
            //   改前这里乘了 `Alpha` ⇒ 淡出比原版多淡一件。⛔ 别改回去（`BattleFxAssertE17` 有断言钉着）。
            if (_targets != null) _targets.SetColor(TargetsColor);
            if (_manaIcon != null) _manaIcon.SetTint(WithAlpha(Color.white, Alpha));
        }

        /// <summary>收起（带淡出）</summary>
        public void Hide()
        {
            if (!Visible && Alpha <= 0f) { HideImmediate(); return; }
            Visible = false;
            ShownName = null;
            // 🆕 2026-10-20（`T09` A3-e）：原版 `FadeOut` 一进来就把捕获区 `SetActive(false)`（`FadeOut.c` 0x20），
            //   而且 `beingDisplayed = false` ⇒ 淡出期间的点击**不算**。同构：这里就把输入层整组关掉。
            SetInputEnabled(false);
            FadeTo(0f, FadeOutTime);        // 原版 `fadeTime × 0.5` = 0.1 s（⛔ 不是 0.2）
        }

        /// <summary>
        /// 收起（不带淡出）。
        /// ⚠️ 显隐**只靠 alpha，不碰 `SetActive`** —— 踩过：一开始用 `SetActive(false)` 收起来，
        ///    结果 `Show()` 里在**还没激活**的对象上调 `SetText`，而 TMP 在未激活的 GameObject 上
        ///    `ForceMeshUpdate()` **生成不出网格**，`textBounds` 全是 0，
        ///    面板上的字全成零尺寸、一个字都看不见。
        ///    面板就这么点东西，一直开着只花几个全透明 quad，不值得为它冒这个险。
        /// </summary>
        void HideImmediate()
        {
            Visible = false;
            // 🆕 2026-10-20（`T09` A3-e）：收起 = 射线全关（件仍然 active，见上面那段踩坑记录）
            SetInputEnabled(false);
            // 🔴 2026-10-24（`E17`）：那一行**不参与淡入淡出**（原版 `SetAlpha`/`FadePower` 的 6 件里没它）
            //   ⇒ 收面板时只能单独把它关掉（见 `SetTargetsShown`）。
            SetTargetsShown(false);
            ShownName = null;
            ShownTargets = 0;
            _pressed = _acting = false;
            _actingA = 0f;
            CurrentLight = Light.None;
            if (_actingFade != null) { _actingFade.Kill(); _actingFade = null; }
            if (_fade != null) { _fade.Kill(); _fade = null; }
            Alpha = 0f;
            ApplyColors();
        }

        /// <summary>淡到某个 alpha。**不切 `SetActive`** —— 理由见 `HideImmediate`
        /// （唯一例外 = `TargetsAvailableText`，见 `SetTargetsShown`）。</summary>
        /// <param name="duration">补间时长。🔴 **两个方向不同**：淡入 = `FadeTime`(0.2)、
        /// 淡出 = `FadeOutTime`(0.1) —— 原版 `FadeIn` / `FadeOut` 各自喂的值，见 `FadeOutFactor`。</param>
        void FadeTo(float target, float duration)
        {
            if (!_built) return;
            if (_fade != null) _fade.Kill();
            _fade = DOTween.To(() => Alpha, SetAlpha, target, duration)
                           .SetUpdate(CardTween.Mode)
                           // 🔴 **绑生命周期**（2026-09-16 加）：面板被销毁时还在飞的淡入淡出
                           //    没人杀的话，DOTween 安全模式会每帧记一条
                           //    `Target or field is missing/null`（真包实测 22 条，编辑器看不见）。
                           //    全工程同一条规矩 —— 见 `CardTween.Use` 的注释。
                           .SetLink(gameObject)
                           // 🆕 2026-10-24（`E17`）：淡出**走完**才把 `TargetsAvailableText` 收起来
                           //   （它不参与淡出 ⇒ 只能靠「整棵树关掉」这一档藏，见 `SetTargetsShown`）。
                           //   淡入那一路 `Visible == true` ⇒ 这里是空操作。
                           .OnComplete(() => { if (!Visible) SetTargetsShown(false); });
        }

        /// <summary>🔴 **2026-10-24（执行代理 `E17`）**：`TargetsAvailableText` **不参与淡入淡出** ——
        /// 原版 `ActiveSkillDisplay.SetAlpha` / `FadePower` 动的恰 **6 件**
        /// （`cardFrame` / `bgLight` / `manaIcon` / `Name` / `Desc` / `Cost`），
        /// `targetsAvailableText`（字段 `+0x78`）**从来没被写过 α**
        /// （`dt/ActiveSkillDisplay__SetAlpha.c:40-49` · `__FadePower.c:20-42` · C4 §1·2）。
        /// <para>⇒ 它的 α 恒为自身基色。面板收起时不能靠 α 藏它 —— 照原版「关闭 = 那棵树不显示」
        /// 的等价做法**单独关它的 GameObject**。⚠️ 这是本面板**唯一**一件用 `SetActive` 收起的件
        /// （其余件一律只压 α，理由见 `HideImmediate` 那段踩坑记录）。</para>
        /// <para>⚠️ 关着的时候**别往里写字**（TMP 在未激活对象上排版会退化）—— 调用点见 `SetTargetsAvailable`。</para></summary>
        void SetTargetsShown(bool on)
        {
            if (_targets == null) return;
            var go = _targets.gameObject;
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        void SetAlpha(float a)
        {
            Alpha = a;
            ApplyColors();
        }

        /// <summary>🔴 **自检用（`T09` 批 3 · 灭自证）**：本面板的图与字**是不是都建在 uGUI 子画布上**
        /// （而不是有条漏网、退回世界空间 quad）。
        /// <para>判据 = 那几件**每一件**都是 uGUI 件、且**都挂在 `_cv`（子画布）的父链下**。
        /// 语义上它防的是本件**最坏的那一档**：一半在画布、一半在世界 —— 16:9 下逐位相同、全绿，
        /// 只有别的宽高比才露（`T09` §五·补⑦）。⛔ 别用「`HudPiece != null`」替它（那个两边都真）；
        /// ⛔ 也别用「`UguiMode`」替它（那个只看宿主登记了没，不看件建在哪）。</para>
        /// <para>⚠️ 任一**该有的**件没建出来（贴图/字体缺失）⇒ 也返 `false`，如实回答「不成立」
        /// —— `_manaIcon` 那格原版 `m_IsActive = 0`、我们本来就允许它缺，所以它**不在**这条断言里。</para></summary>
        public bool OnCanvas
        {
            get
            {
                if (_cv == null || _cv.GetComponentInParent<Canvas>() == null) return false;
                return IsUgui(_bg) && IsUgui(_light) && IsUgui(_name) && IsUgui(_cost)
                    && IsUgui(_desc) && IsUgui(_targets);
            }
        }
        static bool IsUgui(HudPiece q)
        {
            return q != null && q.IsUgui && q.Rect != null
                && q.Rect.GetComponentInParent<Canvas>() != null;
        }
        static bool IsUgui(Label l)
        {
            return l != null && l.IsUgui && l.transform is RectTransform rt
                && rt.GetComponentInParent<Canvas>() != null;
        }

        /// <summary>
        /// 自检用：面板各元素现在摆在哪、多大、有没有激活。
        /// **截图看不出「字是不是根本没生成」**，所以把数报出来。
        /// 🔴 **2026-10-16（`T09` 批 3）**：头一行加了**档位**（`世界档` / `画布档`）——
        /// 两档的件尺寸**单位不同**（世界单位 vs 设计 px），不带这一格的读数没法横向比。
        /// ⚠️ 「面板尺寸」那一格两档报的都是**本档的尺**（画布档 = 设计 px）。
        /// 🔴 **2026-10-16（锚点补正）**：画布档那一格报的是**现算的面板矩形**
        /// （<see cref="PanelCvRect"/> 的 `size`，跟着画布高走）—— ⛔ 不是那对写死的 `PanelPxW/H`
        /// （那对现在只是 1920×1080 下的期望值）。
        /// </summary>
        public string Describe()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("面板 ").Append(UguiMode ? "画布档" : "世界档")
              .Append(" 世界中心 ").Append(transform.position.ToString("F2"))
              .Append("　尺寸 ");
            if (UguiMode)
            {
                var r = PanelCvRect;
                sb.Append(r.width.ToString("F2")).Append("×").Append(r.height.ToString("F2"))
                  .Append(" px（画布 ").Append(CanvasPx.x.ToString("F0")).Append("×")
                  .Append(CanvasPx.y.ToString("F0")).Append(" · 锚点现算）");
            }
            else sb.Append(PanelW.ToString("F2")).Append("×").Append(PanelH.ToString("F2")).Append(" 世界单位");
            sb.Append("　alpha ").Append(Alpha.ToString("F2"))
              .Append("　灯 ").Append(CurrentLight);
            sb.Append("　name[").Append(One(_name)).Append("]");
            sb.Append("　cost[").Append(One(_cost)).Append("]");
            sb.Append("　desc[").Append(One(_desc)).Append("]");
            sb.Append("　targets[").Append(One(_targets)).Append("]");
            return sb.ToString();
        }

        static string One(Label l)
        {
            if (l == null) return "无";
            return "「" + l.Text + "」pos=" + l.transform.position.ToString("F2")
                 + " 尺寸=" + l.WorldW.ToString("F2") + "×" + l.WorldH.ToString("F2")
                 + " [" + l.DumpSizes() + "]"
                 + " active=" + l.gameObject.activeInHierarchy + "/" + l.gameObject.activeSelf;
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-20（W57 · `T09` 步骤 A3-e）：**Editor-only 只读探针**
        //  只读：不改状态、不派事件。播放器构建里**不存在**这些成员（`#if` 包住）⇒ 零运行时开销。
        //  ⚠️ 断言写在 `Editor/BattleScene.cs`（本波口径 = **只做不验** ⇒ 本件不写断言）。
        // ═══════════════════════════════════════════════════════════════════════════════════════════
#if UNITY_EDITOR
        /// <summary>输入层是不是**整组装好**了（画布档：容器 EventTrigger 5 条 + 捕获区 + 捕获区 EventTrigger）。
        /// 世界档 ⇒ `false`（那一档本来就不该有射线件）。</summary>
        public bool UguiInputReadyForTest
        {
            get
            {
                if (!UguiMode || _abilityTrigger == null || _hideBtn == null || _hideBtnTrigger == null) return false;
                return _abilityTrigger.triggers.Count == 5 && _hideBtnTrigger.triggers.Count == 1;
            }
        }

        /// <summary>输入层现在开着吗（跟着显隐走；收起时应为 `false`）。</summary>
        public bool InputEnabledForTest { get { return _inputEnabled; } }

        /// <summary>原版 `AbilityContainer` 上那颗 `EventTrigger`（`null` = 没建 / 世界档）。</summary>
        public EventTrigger AbilityTriggerForTest { get { return _abilityTrigger; } }
        /// <summary>原版 `HideAbilityButton` 上那颗 `EventTrigger`（`null` = 没建 / 世界档）。</summary>
        public EventTrigger HideButtonTriggerForTest { get { return _hideBtnTrigger; } }
        /// <summary>原版 `HideAbilityButton` 那一件（`null` = 世界档 / 没建起来）。</summary>
        public HudPiece HideButtonForTest { get { return _hideBtn; } }

        /// <summary>`AbilityContainer` 吃不吃射线。**原版判据 = `MB_4864.m_RaycastTarget = 1`** ⇒ 开着时应 `true`。</summary>
        public bool AbilityRaycastTargetForTest { get { return GraphicRaycastTarget(_bg); } }
        /// <summary>捕获区吃不吃射线。**原版判据 = `MB_4207.m_RaycastTarget = 1`** ⇒ 开着时应 `true`。</summary>
        public bool HideButtonRaycastTargetForTest { get { return GraphicRaycastTarget(_hideBtn); } }
        /// <summary>`TargetsAvailableText` 的 TMP 吃不吃射线（原版 `rt = 1`；收起时应 `false`）。
        /// ⚠️ TMP 在 `Label` 的**子节点**上（见 `SetLabelRaycast` 的 doc）。</summary>
        public bool TargetsRaycastTargetForTest
        {
            get
            {
                var g = LabelGraphic(_targets);
                return g != null && g.raycastTarget;
            }
        }

        /// <summary>`AbilityContainer` 的 `raycastPadding`（L,B,R,T；原版 `(0,0,0,0)`）。没有那件 ⇒ 四个 `NaN`。</summary>
        public Vector4 AbilityRaycastPaddingForTest { get { return GraphicRaycastPadding(_bg); } }
        /// <summary>捕获区的 `raycastPadding`（原版 `(0,0,0,0)`；没有那件 ⇒ 四个 `NaN`）。</summary>
        public Vector4 HideButtonRaycastPaddingForTest { get { return GraphicRaycastPadding(_hideBtn); } }

        /// <summary>捕获区**实读的矩形**（`RectTransform.rect`；原版 `sd` = 3707.8301 × 1985.3199，
        /// 任何画布尺寸下都不变 —— 变的是它**相对面板节点**的位置）。
        /// 没有那件 ⇒ `(NaN,NaN,NaN,NaN)`。</summary>
        public Rect HideButtonRectForTest
        {
            get
            {
                if (_hideBtn == null || _hideBtn.Rect == null) return new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
                return _hideBtn.Rect.rect;
            }
        }

        /// <summary>捕获区自己的 `anchoredPosition`（**相对它的父节点**）。
        /// 🔴 **2026-10-20（W58）就地订正**：本行原写「相对画布中心（底右锚 + `ap` 换算出来的那一格）」——
        /// **已过期**：父节点从子画布改成了面板节点 `_panelRt`，现在它**就是原版 `RT_3053` 的字面量**
        /// `(220.3900146484375, −146.0101776123047)`（⛔ 不再是换算值）。</summary>
        public Vector2 HideButtonAnchoredPosForTest
        {
            get
            {
                var rt = _hideBtn != null ? _hideBtn.Rect : null;
                return rt != null ? rt.anchoredPosition : new Vector2(float.NaN, float.NaN);
            }
        }

        /// <summary>🆕 **2026-10-20（W58 · `HideAbilityButton` 锚点补正）**：面板锚点节点
        /// （原版 `ActiveSkillDesc` / `RT_3104`）—— 父链断言用。`null` = 世界档 / 没建起来。</summary>
        public RectTransform PanelAnchorRectForTest { get { return _panelRt; } }

        /// <summary>🆕 **2026-10-20（W58）**：捕获区的父节点。**判据 = 面板锚点节点**（⛔ 不是子画布）；
        /// 没建 ⇒ `null`。</summary>
        public RectTransform HideButtonParentForTest
        {
            get
            {
                var rt = _hideBtn != null ? _hideBtn.Rect : null;
                return rt != null ? rt.parent as RectTransform : null;
            }
        }

        /// <summary>🆕 **2026-10-20（W58）**：捕获区 `RectTransform` 的五元组读回
        /// （顺序 `aMin` · `aMax` · `pivot` · `anchoredPosition` · `sizeDelta`）。
        /// <para>判据 = 原版 `bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_3053.json`
        /// 的**字面量**（13 个 `battlearena*` 包逐值相同）：
        /// `(1,0)` / `(1,0)` / `(1,0)` / `(220.3900146484375, −146.0101776123047)` /
        /// `(3707.830078125, 1985.3199462890625)`。⛔ 期望值别读本口反推（同义反复）。</para>
        /// <para>没有那件 ⇒ `false` 且五个 `out` 全是 `NaN`（⛔ 别折成 0 —— 与「锚点真是 0」分不开）。</para></summary>
        public bool HideButtonAnchorsForTest(out Vector2 aMin, out Vector2 aMax, out Vector2 pivot,
                                             out Vector2 anchoredPos, out Vector2 sizeDelta)
        {
            aMin = aMax = pivot = anchoredPos = sizeDelta = new Vector2(float.NaN, float.NaN);
            var rt = _hideBtn != null ? _hideBtn.Rect : null;
            if (rt == null) return false;
            aMin = rt.anchorMin;
            aMax = rt.anchorMax;
            pivot = rt.pivot;
            anchoredPos = rt.anchoredPosition;
            sizeDelta = rt.sizeDelta;
            return true;
        }

        /// <summary>🆕 **2026-10-20（W58）**：捕获区是否**画在面板本体之下**（射线顺序正确）。
        /// <para>判据 = 面板节点 `_panelRt` 与 `_bg`（`AbilityContainer`）**是同一父节点的兄弟**，
        /// 且 `_panelRt.GetSiblingIndex() < _bg.GetSiblingIndex()`（Unity：先画 = 在下 = 射线后收）。</para>
        /// <para>🔑 **为什么要断它**：`_panelRt` 是在 `BuildCanvas` 里手工 `new` 的节点，
        /// 若不登记进 `HudCanvas` 的 z 账，后续平级件的 `SetSiblingIndex` 会把它挤到**最上层**
        /// ⇒ 透明捕获区盖住面板，**点面板本体先命中捕获区（面板当场关掉）**（静默、不报错）。</para></summary>
        public bool HideButtonBehindAbilityContainerForTest
        {
            get
            {
                var prt = _panelRt;
                var bg = _bg != null ? _bg.Rect : null;
                if (prt == null || bg == null || prt.parent == null || bg.parent != prt.parent) return false;
                return prt.GetSiblingIndex() < bg.GetSiblingIndex();
            }
        }

        /// <summary>🆕 **2026-10-20（W58）**：捕获区在【子画布局部坐标】（设计 px、中心原点、y 向上）的
        /// **实测中心与尺寸** —— 断言 `CenterPx/SizePx` 用。
        /// <para>**实测支**：`panel.localPosition + hide.localPosition + hide.rect.center`
        /// （= Unity 按锚点排出来的真值；父尺寸由画布给）。⚠️ 这里**只读 Transform 的布局结果**，
        /// ⛔ 不复用 `PanelCvRect` / `CanvasPx`（那是被测实现自己的另一条路）。</para>
        /// <para>**退档支**（批处理里画布 `rect` 还没排过版 ⇒ 0×0）：按捕获区自己的锚点字段 +
        /// `PanelCvRect`（它自己也有同一档退路）算同一格，并**出声一次**（⛔ 不静默假装量到）。</para>
        /// <para>没有那件 / 没有父节点 ⇒ `false` 且两个 `out` 是 `NaN`。</para></summary>
        public bool HideButtonCenterSizeForTest(out Vector2 centerPx, out Vector2 sizePx)
        {
            centerPx = sizePx = new Vector2(float.NaN, float.NaN);
            var rt = _hideBtn != null ? _hideBtn.Rect : null;
            var parent = rt != null ? rt.parent as RectTransform : null;
            if (rt == null || parent == null) return false;

            // ── 实测支：Unity 布局结果 ──
            var rr = rt.rect;
            var pr = parent.rect;
            if (rr.width >= 1f && rr.height >= 1f && pr.width >= 1f && pr.height >= 1f)
            {
                sizePx = rr.size;
                centerPx = (Vector2)parent.localPosition + (Vector2)rt.localPosition + rr.center;
                return true;
            }

            // ── 退档支：锚点字段 + 面板矩形 ──
            if (!_warnedHideGeoDegenerate)
            {
                _warnedHideGeoDegenerate = true;
                Debug.LogWarning("[Battle] 技能面板：捕获区 / 面板的 `rect` 还量不到（批处理下画布没排过版）"
                               + " ⇒ `HideButtonCenterSizeForTest` 按锚点字段 + `PanelCvRect` 算同一格（不静默）");
            }
            var r = PanelCvRect;
            float spanX = rt.anchorMax.x - rt.anchorMin.x;
            float spanY = rt.anchorMax.y - rt.anchorMin.y;
            var size = new Vector2(spanX * r.width + rt.sizeDelta.x,
                                   spanY * r.height + rt.sizeDelta.y);
            var anchorRef = new Vector2((rt.anchorMin.x + spanX * rt.pivot.x) * r.width,
                                        (rt.anchorMin.y + spanY * rt.pivot.y) * r.height);
            var rel = anchorRef + rt.anchoredPosition
                    + new Vector2((0.5f - rt.pivot.x) * size.x, (0.5f - rt.pivot.y) * size.y);
            sizePx = size;
            centerPx = new Vector2(r.xMin + rel.x, r.yMin + rel.y);
            return true;
        }
        /// <summary>`HideButtonCenterSizeForTest` 退档支「出声一次」的闩（诊断用；正常路径恒 false）。</summary>
        bool _warnedHideGeoDegenerate;

        /// <summary>uGUI 各事件的到达次数（灭自证：喂一次 uGUI = 1；世界侧手搓路**不经过这里**）。</summary>
        public int SkillPointerDownCountForTest { get { return _downCount; } }
        /// <inheritdoc cref="SkillPointerDownCountForTest"/>
        public int SkillPointerUpCountForTest { get { return _upCount; } }
        /// <inheritdoc cref="SkillPointerDownCountForTest"/>
        public int SkillPointerEnterCountForTest { get { return _enterCount; } }
        /// <inheritdoc cref="SkillPointerDownCountForTest"/>
        public int SkillPointerExitCountForTest { get { return _exitCount; } }
        /// <inheritdoc cref="SkillPointerDownCountForTest"/>
        public int SkillDragCountForTest { get { return _dragCount; } }
        /// <inheritdoc cref="SkillPointerDownCountForTest"/>
        public int HidePointerDownCountForTest { get { return _closeDownCount; } }
        /// <summary>🆕 2026-10-20（`T09-FIX3` · P0-1）：捕获区按下后被**让给世界侧**的次数
        /// （= 点在棋盘卡上的那一下；与 `HidePointerDownCountForTest` 配对，证明「这一下没关面板」）。</summary>
        public int HidePointerPassCountForTest { get { return _closePassCount; } }

        /// <summary>🆕 2026-10-20（`T09-FIX3` · P0-1）：宿主有没有登记「让给世界侧」的判据。</summary>
        public bool LeavePointerToWorldWiredForTest { get { return LeavePointerToWorld != null; } }

        /// <summary>🆕 2026-10-20（`T09-FIX3` · P0-1）：**自检用** —— 喂一次捕获区的 `PointerDown`
        /// （屏幕点，与 `EventSystem` 派发时同一个字段）。返回 **true = 这一下把面板关掉了**。
        /// <para>⚠️ 走的是**真 handler**（`OnHidePointerDown`）⇒ 与生产同一条判据；
        /// ⛔ 别在自检里另写一份「是不是目标」的判定（那就是自证）。</para></summary>
        public bool SimulateHidePointerDownForTest(Vector2 screenPoint)
        {
            bool wasVisible = Visible;
            var ped = new PointerEventData(EventSystem.current);
            ped.position = screenPoint;
            OnHidePointerDown(ped);
            return wasVisible && !Visible;
        }

        static bool GraphicRaycastTarget(HudPiece p)
        {
            var g = GraphicOf(p);
            return g != null && g.raycastTarget;
        }
        static Vector4 GraphicRaycastPadding(HudPiece p)
        {
            var g = GraphicOf(p);
            return g != null ? g.raycastPadding : new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
        }
        static UnityEngine.UI.Graphic GraphicOf(HudPiece p)
        {
            if (p == null || p.Rect == null) return null;
            return p.Rect.GetComponent<UnityEngine.UI.Graphic>();
        }
#endif
    }
}
