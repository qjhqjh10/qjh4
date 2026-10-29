// AttackSelector.cs — 原版的「攻击方式选择器」（近战 / 远程 / 主动技能）
//
// ⚠️ **本文件里的每个数值都来自解包资源实测**，不是拍的。出处：
//    · 场景 JSON：`d:/2/解包整理/07_场景/battlearena1/GameObject/Drag Attack Selector_172.json`
//      和它三个子按钮（`Select Melee Button_867` / `Select Range Button_1005` /
//      `Select Active Skill Button_760`）上的 MonoBehaviour
//    · 运行时实况：`资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv`
//
//    对象路径  Canvas/FrontCanvas/Safe area FrontCanvas/Drag Attack Selector
//    整条槽    621.7 × 122.2，**屏幕正中央**（两排棋盘之间那条带，y=479..601 @1080p）
//    三个按钮  118.372 × 118.806（节点 rect；Image `m_PreserveAspect = 0` ⇒ 拉满这张固定矩形），间距 37，居中排开
//    attackType 是个位标志：1 = 近战、2 = 远程、4 = 主动技能（所以枚举值照抄）
//    选中放大  scaleMultiplierWhenSelected = 1.3
//    拖拽阈值  accumulatedDragForMinDistance = 0.085（屏高的 8.5%，1080p ≈ 92 px）
//    方向      directionToPivotPoint —— 近战 (-0.5,-1) 左下 / 技能 (0,-1.1) 正下 /
//              远程 (0.5,-1) 右下。**这就是横排的左右顺序**：近战在前、技能居中、远程在后
//    底板      Select Attack Background —— ⚠️ sizeDelta 是 780×285，但 **localScale = 0.44**、
//              显示时再横向 ×1.5 → **实际 514.8 × 125.4**。只读 sizeDelta 会做出一大块
//
// ⚠️ **别照抄 `d:/warpforge/scripts/battle.gd` 那份重实现**：它把四个按钮**叠在同一坐标**、
//    用 `visible = (_attack_type == ...)` 做互斥高亮 —— Godot 里不可见的 Button 收不到输入，
//    结果玩家根本切不到远程/技能。**以解包的原版为准**（三个独立按钮 + 黄圈 + 1.3 倍放大）。
//
// 图（`Resources/Art/ui/`，都是原版战斗 UI 图集的切片）：
//    Attack_type_button_Melee / _Ranged（按钮本体，自带底盘和金属环）
//    Attack_type_button_highlight（选中黄圈）
//    Attack_type_Generic_Foreground（**主动技能那格的占位底图** —— 原版那格是空的、运行时才赋图，
//    这张图在原版里查不到任何引用，是我们挑的。见 `IconName`）
//
// **没有这些图也能跑** —— 每条都判空，退化成纯文字（和 `CardArt` 一个路子）。
//
// ─────────────────────────────────────────────────────────────────────────────
// 🔴 **2026-10-16（`T09` 批 3）：整条迁到 uGUI 子画布**（判据 → `任务/T09_整层换uGUI.md` §八·3）
//
// **原版对应物** = GO **`Drag Attack Selector`** @ `BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas`，
//   **兄弟序 0/14** · 排序层 `Default`(0) · order 0 · **`overrideSorting = false`**
//   （出处 `bundle_scenes_scenes_battlearena2/Canvas/Canvas_2759.json` ·
//    `RectTransform/RectTransform_3166.json`；表 → `普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一 ·
//    `T09b` §A·2#5 / §A·3）。
//   ⇒ 🔴 它**不另立排序组**、**随父画布按兄弟序渲染** = 该组（`OverHand`）里**最底下一件**
//     ⇒ **本件【不建】自己的画布**：并进**该组那块画布**，并在**宿主容器里拿最低的兄弟序**（见 `ZFrameBottom`）。
//
// 🔴 **双模式（硬要求）**：宿主为 `null` ⇒ **世界档** —— 与改之前**逐位相同**（`U`/`US`/`Z` 三个换算口
//    在两档下分别退化成原式）。宿主由**建 UI 的那一方**显式登记（`UseHost`，先例 = `Tooltip.UseHost` ·
//    `ErrorMessageBanner.Create(..., uguiHost)` · 同批同组的 `SkillPanel.UseHost`）。
//   ⚠️ **本文件【不】自己建画布、也不自己挑画布** —— 「并进哪一块」的口径由主对话裁（见报告 §③/§⑤）。
//   ⚠️ 组件是**场景挂的**（`AddComponent<AttackSelector>()`，没有 `Create`）⇒ 只有驱动层知道根画布建好了没；
//      而 `Awake()` 会先跑一次 `Build()`（那一刻宿主还没登记 ⇒ 走世界档）⇒ `UseHost` 排在 `Build()` 之后
//      **是正常次序**，那时会**拆掉世界档的件、按画布档重建**（⛔ 不是「两边都建」）。
//
// ⚠️ **本件有命中（拖拽起手）** ⇒ 两档都必须对。那条反复踩过的量纲坑
//    （**旧 quad 的局部单位 = 世界单位、uGUI = 设计 px，差 108；也见 `T09` §五·补⑥ 末**）
//    在本件里有四处：`RefreshLayout` 的摆位、`EnterOffset`、`UpdatePointer` 的圆半径、
//    `ButtonWorld` 的读回 —— 逐处都按模式换过（见各自的 doc）。
// ─────────────────────────────────────────────────────────────────────────────
// ═══════════════════════════════════════════════════════════════════════════════════════
// 🔴 **2026-10-20（`W57` · 战斗线 `T09` 步骤 A3-e）：本件的【输入层】换成 uGUI**
//   判据（本波现读 `bundle_scenes_scenes_battlearena1` 的 `Drag Attack Selector` = GO **172** /
//   `RT 2973`，子树逐组件）：
//     · 根上**自带 `Canvas`（`MB_2566`）+ `EverguildGraphicRaycaster`（`MB_4391`）** ⇒ 原版这条选择器
//       本来就是一块**能收射线的画布**；
//     · 三颗钮（`Select Melee / Range / Active Skill Button`，GO **867 / 1005 / 760** ·
//       `RT 3368 / 2804 / 2626`）：**节点 rect 118.372 × 118.806** · `Image.m_RaycastTarget = 1` ·
//       `m_RaycastPadding = (0,0,0,0)` · `m_PreserveAspect = 0`；上面挂
//       **`EverguildButton`（`m_Transition = 1` ColorTint）+ `CardDisplayAttackTypeButton`**；
//     · 🔴 **选中是「悬停」，不是点击**：`CardDisplayAttackTypeButton` 只实现 `IPointerEnter/ExitHandler`
//       （`__…OnPointerEnter.c`：`+0x92 = 1`；`__…OnPointerExit.c`：清 `+0x91` 并给兄弟钮 `DisableTemporary`），
//       `AttackTypesButtonsController__Update.c` 拿「`inputOverButton && sendInput && !isInputOverSent`」
//       发指令 —— 这一套我们 2026-10-13（`A462`）已按原版实现，**本波不改语义**；
//     · 每颗钮的子件 **`Highlight`**（GO **112 / 396 / 180** · `RT 3028 / 2776 / 3512` · 176.23 × 176.875）
//       的 `Image.m_RaycastTarget` **也是 1** ⇒ **原版悬停区 = 钮 ∪ 高亮圈**（圈更大 ⇒ 实际就是那一圈）；
//     · `Select Attack Background`（GO 951 · `MB_4492`）`m_RaycastTarget = 1`（无 sprite、纯色、无动作）。
//   ⇒ 本波落地（**只换输入路径，不动排布 / 跟随 / `RefreshLayout`**）：
//     · uGUI 档把 **`Highlight` 那件**置 `raycastTarget = true` 并挂 `EventTrigger` 的
//       `PointerEnter / PointerExit`（= 原版 `IPointerEnter/ExitHandler` 的等价送达）⇒ 驱动 `Hovered`；
//     · `UpdatePointer` 在 uGUI 档**不再做手搓圆判定**（否则两条路抢同一个 `Hovered` = 半迁移），
//       只保留 `Tick()`（入场动画 / 安全窗）+ 返回 `Hovered`；**世界档逐位不变**（`_frame == null` 走原式）。
//     · `ContainsBar`（「点槽外取消」）**原版没有等价物**（`A462` §四·3 已记）⇒ 本波不动它 ——
//       它仍在 `BattleDriver` 那条链上，归 `T09` A4 与手搓沿一起收。
//   ⚠️ **悬停区与今天不同（如实记，⛔ 不是回归）**：今天 = 以图标中心为圆心、直径 118.8 px 的**圆**；
//     本波起 = `Highlight` 的 **176.2 × 176.2 矩形**（含圆外四角、相邻两格会重叠 —— 原版同样重叠）。
//     `HoverPickReady` / 安全窗 / `_hoverSent` 的语义**一个字没动**（`TickHover` 仍是唯一判据）。
// ═══════════════════════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using DG.Tweening;               // 🆕 2026-10-25：选中/取消选中那一拍的 0.3 s `DOScale`（原版是补间，⛔ 不是瞬切）
using UnityEngine;
using UnityEngine.EventSystems;   // 🆕 2026-10-20（A3-e）：`EventTrigger` 的 `PointerEnter/Exit`

namespace CardPresentation
{
    /// <summary>
    /// 一次行动可以选哪几种「打法」。
    /// 值**照抄原版的 `attackType` 位标志**（1/2/4），这样对着场景 JSON 排查时能直接对上。
    /// </summary>
    public enum AttackKind
    {
        None = 0,
        Melee = 1,
        Ranged = 2,
        Ability = 4,
    }

    public class AttackSelector : MonoBehaviour
    {
        // ==================================================================
        //  原版实测数值（1920×1080 像素）
        // ==================================================================

        const float BarW = 621.7f, BarH = 122.2f;
        /// <summary>原版三颗钮的**节点 rect**（`RectTransform.m_SizeDelta`）—— **固定值，⛔ 不按贴图比例**。
        /// <para>🔴 **2026-10-24（`A28`）就地订正（铁律 5）**：原来只有一个 `ButtonSize = 118.8`、
        /// 宽度写成 `ButtonSize × AspectOf(tex)`（按贴图宽高比）。判据（**逐件现读**，arena1/arena2 各三份）：
        /// · `RT_3368 / 2804 / 2626`（arena1；arena2 = `2819 / 2997 / 3563`）全是
        ///   **`m_SizeDelta (118.37200164794922, 118.80599975585938)`** · `m_LocalScale 1`；
        /// · 它们身上的 `Image`（`MB_4206 / 4121 / 4054`）= **`m_PreserveAspect = 0`** · `m_Type 0`（Simple）
        ///   ⇒ **拉满这个矩形**、与贴图宽高比无关。
        /// （出处：`bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_{3368,2804,2626}.json`
        ///  · `…/MonoBehaviour/MonoBehaviour_{4206,4121,4054}.json`。）</para>
        /// <para>⚠️ **原版不同状态下这一对宽度不变**（2026-10-24 全量反编译现核）：
        /// `CardDisplayAttackTypeButton` / `AttackTypesButtonsController` 只写 `localPosition` 与
        /// `localScale`（选中 1.3 倍挂的是 **`localScale`**，见 `HighlightScale`）——
        /// **没有任何 `sizeDelta` / `SetSizeWithCurrentAnchors` 写入点** ⇒ 节点矩形恒定。</para>
        /// <para>🧨 改坏法：宽再写成 `ButtonW × AspectOf(tex)` ⇒ 我们这批贴图恰好都是正方形 ⇒ 实测 118.8
        /// ⇒ 与 118.372 差 0.428 px &gt; 容差 ⇒ `Editor/BattleScene.cs` 的 `A28` 那条红
        /// （换成非正方形贴图会差更多，而且**画面看着还挺正常**）。</para></summary>
        const float ButtonW = 118.372f, ButtonH = 118.806f;
        const float ButtonSpacing = 37f;
        const float HighlightScale = 1.3f;              // scaleMultiplierWhenSelected
        /// <summary>「已选打法」那一格**放大 / 回落**的补间时长 = `DAT_1834b2dc8`（`工具/read_literal.py` 实读 **0.3**）。
        /// <para>判据：`CardDisplayAttackTypeButton__HighlightIfNeeded.c` 与
        /// `AttackTypesButtonsController__HighlightSelectedAttackTypeButtons.c` 里都是
        /// `DOScale(target, DAT_1834b2dc8)`；**选中那一格再 `SetEase(0x1b)`** =
        /// DOTween `Ease.OutBack`（枚举逐个实读：0 Unset · … · 26 `InBack` · **27 `OutBack`** · 28 `InOutBack`），
        /// **回落那一格不带 `SetEase`** ⇒ 走 `DOTween.defaultEaseType`（本工程 = 6 `OutQuad`）。两级都是 0.3 s。</para>
        /// <para>🧨 改坏法：把这个补间删掉、直接写 `localScale` ⇒ 原版那一下「弹出来」的手感没了
        /// （画面仍然「看着对」，因为终值一样）。</para></summary>
        const float PickTweenSeconds = 0.3f;
        /// <summary>选中黄圈（原版按钮的子件 `Highlight`）的**节点 rect** = **176.23 × 176.875**。
        /// 判据：`RT_3028 / 2776 / 3512`（`m_SizeDelta (176.22999572753906, 176.875)` · `m_LocalScale 1`）
        /// ＋ 它们的 `Image`（`MB_5267 / 4108 / 4478`）**`m_PreserveAspect = 0`** ⇒ 拉满。
        /// 🔴 **2026-10-24（`A28`）就地订正**：原来按 `RingSize × AspectOf(tex)` 摆（我们这张贴图 227×227
        /// 是正方形 ⇒ 176.2²），**高差 0.675 px**；而且与非正方形贴图一配就错。⛔ 别改回按比例。</summary>
        const float RingW = 176.23f, RingH = 176.875f;

        // ⚠️ **`40K_melee_glow` / `40K_ranged_glow` 不在这里用**。
        //    我一开始以为它们是按钮的外发光 —— **错了**。它们真正的用途是
        //    **场上单位卡身上、按攻击方式点亮的高亮**：
        //    `d:/2/解包整理/08_预制体特效/战斗预制体/GameObject/Highlight_*.json`（近战，红）和
        //    `Highlight ranged_*.json`（远程，紫），挂在 `CardPrefab/Board Elements/3DBody/
        //    Base Attack Counters/Melee Attack Container`（和 `Range Attack Container`）下面。
        //    那是「**这个单位能被选中**」的反馈。
        //    🔴 **2026-10-23（`W64`·`D-12` 就地订正 · 铁律 5）**：本段原来写「**还没做** / 锚点还没钉死」
        //    —— **已过期**：那一层已经建好，落在 `Core/CardView.cs`（`MakeGlow("40K_melee_glow", …)`
        //    与 `MakeGlow("40K_ranged_glow", …)`，见 `CardView.cs:7143-7145`；挂载/显隐由 `CardView`
        //    自己的高亮那一段管，不在本文件）。⛔ 本文件仍然**不**用这两张图（它只管按钮本身的底盘/金属环）。
        //    按钮本身自带底盘和金属环（`Attack type button Melee/Ranged`），不用再垫一层光。

        // ── 底板 `Select Attack Background` ──────────────────────────────────────────
        // 节点真身 = **`RT_3061`**（`m_SizeDelta 780×285` · `m_LocalScale 0.44` · `m_Pivot (0.5,1)` ·
        // `m_AnchorMin/Max (0.5,1)` · `m_AnchoredPosition (0,−18.375)`）。
        // 🔴 **2026-10-25 就地订正（铁律 5）**：本段原来写「显示时再横向 ×1.5 ⇒ 实际 514.8 × 125.4」，
        //   把一帧当成了常量 —— **实绘宽是 `count` 与入场进度 `t` 两个量的函数**：
        //   · `SelectAttackBackgroundController.ChangeSizeByElements(count)`（产物在
        //     `SelectAttackBackgroundController__ChangeSizeByElements.c`，同一段还被**内联**进
        //     `AttackTypesButtonsController__ToggleAttackTypesSelector.c` 尾部）：
        //     目标 = 原始 0.44 × `1 + (elementsHorizontalScaleModifier − 1) × clamp01(count − 2)`
        //     ⇒ **2 颗钮** 0.44 · **3 颗钮** 0.66（`DAT_1834b2bbc` 实读 = **2.0**）；
        //   · `AttackTypesButtonsController__MoveButtons.c` 每帧（= `ChangeSizeByInput` 的同一算式）：
        //     `localScale = lerp(目标 × inputScaleModifier(1.5), 原始 0.44, t)`。
        //   ⇒ **t=0（刚顶上来的那一帧）**：2 颗 **514.8** · 3 颗 **772.2**；
        //     **t=1（归位后平常看到的那一帧）**：**343.2 × 125.4**（与颗数无关）。
        //   判据值：`MonoBehaviour_5200.json`（GO 951 上那颗控制器）= `inputScaleModifier 1.5` /
        //   `elementsHorizontalScaleModifier 1.5`；`DAT_1834b2bb8` = 1.0（插值上界）。
        //   ⚠️ 只看 sizeDelta 会以为是 780×285，做出来一大块（这块差点做错，别再用 sizeDelta）
        const float BgBaseW = 780f * 0.44f;             // 343.2 = 原始 scale 下的宽（t=1 实绘；t=0 起点另算）
        const float BgH = 285f * 0.44f;                 // 125.4（高恒定，不参与补间）
        /// <summary>`SelectAttackBackgroundController.inputScaleModifier`（`MB_5200` 实读 1.5）——
        /// **入场补间起点那一侧的倍率**（`MoveButtons` / `ChangeSizeByInput` 里乘在「目标 scale」上）。</summary>
        const float InputScaleModifier = 1.5f;
        /// <summary>`SelectAttackBackgroundController.elementsHorizontalScaleModifier`（`MB_5200` 实读 1.5）——
        /// **3 颗钮**相对 2 颗钮的额外横向倍率（`ChangeSizeByElements` 里乘在「原始 scale」上）。</summary>
        const float ElementsHorizontalScaleModifier = 1.5f;
        /// <summary>`Select Attack Background` 的**顶边**在父 rect **顶边**下方多少 px
        /// （`RT_3061.json`：`m_AnchorMin/Max = (0.5, **1**)`、`m_AnchoredPosition = (0, −18.375)`、
        /// `m_Pivot = (0.5, **1**)` ⇒ 它的**顶边** = 父 rect 顶边 − 18.375）。
        /// ⚠️ **2026-10-25 就地订正（铁律 5）**：原来这里引的是 `RT_3254` —— 那是**另一颗节点**
        /// （`sizeDelta −26×0`、stretch 锚、pivot 0.5），值与本件对不上。真身 = `RT_3061`。</summary>
        const float BgDropY = 18.375f;
        /// <summary>底板**顶边**在**锚点**（= 父 rect 中心 = 三颗钮那一行的中心）**上方**多少 px
        /// = `BarH/2 − BgDropY` = **42.7485**。
        ///
        /// 🔴🔴 **2026-10-24（`B-21`）就地订正（铁律 5）—— 下面这段是本件最重要的一条口径，别看错符号：**
        /// 原版 `Select Attack Background` 的锚点是 **`(0.5, 1)`（父 rect 的顶边）**，**不是中心** ——
        /// 所以 `ap (0, −18.375)` 是「**父 rect 顶边**往下 18.375」。换算到「**以父中心为原点、y 向上**」
        /// 的坐标里就是 `+BarH/2 − 18.375` = **+42.7485** ⇒ **底板顶边在钮心【上方】42.7485**。
        /// 三颗钮则被父件上的 `HorizontalOrVerticalLayoutGroup`（`MB_4653`：`m_Spacing 37` ·
        /// `m_ChildAlignment 4 = MiddleCenter` · padding 0）**居中**在父 rect 里 ⇒ **钮心 = 中心 y = 0**。
        /// （三份 `RectTransform` 的 `ap (0,0)` / `anchor (0,0)` 是**布局组跑之前的模板位**，⛔ 别直接照抄 ——
        ///  运行时实况 dump `runtime_ui_dump_Battle_Arena_1.tsv:363,366` 里技能/远程两颗已被布局组写到
        ///  `(310.9, 61.1)` = 父 rect 左下角 +(310.9,61.1) = **中心 + (0,0)**，与这里一致。）
        ///
        /// ⚠️ **原来我们写的是 `ButtonRowDownPx = BarH/2 − BgDropY = 42.7485`，并把三颗钮压在锚点【下方】**
        /// 42.7485**（`center = origin − (0, 42.7485)`）—— 代价是**整条选择器比原版低 42.7485 px**。
        /// 现在 = `center = origin`（钮心落在锚点上）+ 底板顶边抬到锚点上方 42.7485 ⇒ 两者之间仍是
        /// **42.7485**（原版的相对关系一直是对的，错的只是**整条相对锚点**的位置）。
        /// 📌 判据出处：`RectTransform_3061/2973/3563/2819/2997.json` · `MonoBehaviour_4653.json`（布局组）·
        /// `资料/普查产出_1016并行/T09_AttackSelector_报告.md:251-256` · 上面的运行时 dump。
        /// 🧨 改坏法：`center` 再减掉一个 42.7485 ⇒ `Editor/BattleScene.cs` 的 `B-21` 两条断言红。</summary>
        const float BgTopAboveAnchorPx = BarH * 0.5f - BgDropY;   // 42.7485

        // 按钮上的数字角标（原版 `Select Active Skill Button/ValueText`）：
        // 只有**主动技能**那一格有，字号大（base 36 / max 89），贴在中下部
        const float BadgeDx = 0f, BadgeDy = -8f;

        /// <summary>拖多远才弹出来：屏高的 8.5%（1080p ≈ 92 px）。
        ///
        /// 🔴 **2026-09-29 更正（Q7 第 4 条）：这个数在原版里【不是】弹出阈值。**
        /// 原版那个字段叫 `AttackTypesButtonsController.accumulatedDragForMinDistance`（`dump.cs` 偏移 **`+0x30`**），
        /// 它的真正用途 = **三钮入场动画的进度分母**：
        /// `AttackTypesButtonsController__MoveButtons.c` 每帧算 `t = −accumulatedDrag.y ÷ 它`、
        /// 夹到 `[0,1]`（夹的上界 `DAT_1834b2bb8` 从 DLL 浮点池读出来 = **1.0f**），
        /// 再把它交给每个按钮的 `CardDisplayAttackTypeButton.MoveButton(t)`。
        ///
        /// ⚠️ **我们原来把它当弹出阈值用**（`BattleDriver.cs:13464` 的拖拽判据）—— 那是**自造的替代**。
        /// 🔴 **2026-10-25 判据已闭合（`DAT_1834b2bb0` 那条不再「没定死」）**：
        ///   · 真入口 = `CardScript__OnTouchDrag.c:46` → `CardScript__TryDraggingFromBoard.c`；
        ///     那里第一道闸是 `|pointer.position(+0x144) − 本次位置|² &lt;= DAT_1834b2bb0`
        ///     （`read_literal.py` 实读 = **0.2**）⇒ 它的单位是**屏幕 px 的平方**，即「**挪动超过 ≈0.45 px**」
        ///     —— 那是个**去抖死区**，不是手感阈值；
        ///   · 真正的「拖拽开始」门在 **uGUI 自己**：`IDragHandler`（也就是上面那句所在的方法）
        ///     **要拖过 `EventSystem.m_DragThreshold` 之后才会被派**（`pixelDragThreshold` 只是它的属性包装）。
        ///     🔴 **2026-10-25（`X8`）就地订正（铁律 5）：这个值【查到了】，不是 Unity 默认 10 px。**
        ///     判据 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity:5429`
        ///     → `m_DragThreshold: 5`。为什么这一份对战场有效：**全工程只有这一个 `EventSystem`**
        ///     （13 个 battlearena 场景包里一份都没有 ⇒ 战斗沿用它；`level0` / `menus` 三个包搜
        ///     `m_DragThreshold` 仍然 0 命中），而 `Intro` 是常驻的引导/管理器场景。
        ///     反编译侧再核一遍：`pixelDragThreshold` / `m_DragThreshold` 在 `decomp_full` 里
        ///     **0 处读写**（没有代码改过它）⇒ **原版奔跑值就是序列化的 5 px**。
        ///     ⚠️ 原来这里写「序列化值没搜到 ⇒ 按 Unity 默认 10 px 记」—— 那是**只搜了 bundle、
        ///     没搜 AssetRipper 导出的场景**（`Intro.unity` 在导出工程里，不在 `assets_full` 的
        ///     bundle 目录树里）⇒ 结论偏了 1 倍，已改。
        ///   ⇒ 手感口径 = **5 px**，不是 **92 px**（8.5% 屏高）。见 <see cref="PopDragThresholdPx"/>。
        /// </summary>
        public const float DragThreshold01 = 0.085f;

        /// <summary>🆕 **2026-10-25（`X8` 订正为 5）**：**真正的手感门 = 拖出 5 px 才弹选择器**
        /// （判据 → `DragThreshold01` 的 doc：原版 uGUI 的 `EventSystem.m_DragThreshold = 5`，
        /// 出处 `Intro.unity:5429`；全工程唯一那份 EventSystem，战场沿用它）。
        /// <para>⛔ **别再拿 `DragThreshold01`（0.085 屏高 ≈ 92 px）当弹出阈值** —— 它只是**入场动画的进度分母**，
        /// 两个量在原版里是两件事，混用会让我们「必须拖 18 倍的距离才弹出」（量级错，不是微调）。</para>
        /// <para>⚠️ 调用点在 `BattleDriver`（那边判 `dragged`），本文件只给**唯一的常量出处**。</para></summary>
        public const float PopDragThresholdPx = 5f;

        /// <summary>`PopDragThresholdPx` 的世界单位版（= 5/108 ≈ 0.0463；同 `W()` 的口径）。</summary>
        public static float PopDragThresholdWorld { get { return W(PopDragThresholdPx); } }

        // 🔴 **2026-10-13（A658）就地更正：入场动画的驱动量【不是】时间，是「累计的视口拖拽位移」。**
        //    原来这里有一个 `const float EnterAnimSeconds = 0.085f`，注释写着「原版每一帧往
        //    `accumulatedDrag`（`+0x50`）里加一个每帧量，**判不出那到底是 `−deltaTime` 还是拖拽速度** ⇒ 按时间驱动」。
        //    判据现在有了 —— `AttackTypesButtonsController__MoveButtons.c` 逐句读出来是：
        //        accumulatedDrag(+0x50/+0x54) += TouchInputManager.TouchDragDeltaViewport(+0x18/+0x1c)
        //        t = clamp01( −accumulatedDrag.y ÷ accumulatedDragForMinDistance(+0x30) )
        //    ⇒ 驱动量 = **累计的视口拖拽位移**（`TouchInputManager` 每帧刷的那一格）。
        //    推论（原版如此，⛔ 别「顺手」补个按时间的收尾）：**玩家停住不拖，三钮就停在半路**；
        //    拖过 0.085 屏高之后一律夹在 1（归位）。见 `Tick` / `PushDrag` / `_accumulatedDrag`。

        /// <summary>🆕 2026-09-29：整条要挂在哪（世界坐标）。**默认 `Vector3.zero` = 屏幕中心**（我们原来的固定摆法）。
        /// 由 `Show(..., atWorld)` 设；`RefreshLayout` 每次都用它 ⇒ 分辨率重排也不会丢。</summary>
        Vector3 _anchorAt = Vector3.zero;

        /// <summary>自检用：整条现在挂在哪个世界坐标上。</summary>
        public Vector3 AnchorWorld { get { return _anchorAt; } }

        /// <summary>自检用：底板的**实绘尺寸**（px @1920×1080 —— 原版那条拍的是 **514.8 × 125.4**）。
        /// 🔴 **uGUI 档读 `sizeDelta`**（= 设计 px，精确）—— ⛔ 不走「世界尺寸 ×108」那条：画布单位↔世界单位
        /// 的**两个轴系数不同、且随屏变**（批处理下 x 1/108 · y 1/144）⇒ 那条读法偏 0.75 倍
        /// （**画面没错、尺子错**，判据 → `T09` §七·8）。</summary>
        public Vector2 BgSizePx
        {
            get
            {
                if (_background == null) return Vector2.zero;
                if (_background.IsUgui) return HudCanvas.SizePx(_background.Rect);
                // 🆕 2026-10-25：宽不再靠 `localScale.x` 拉伸（那是把 1:1 纯色板拉成 780×285 的老办法）——
                //   现在宽与高都由 `SetAspect`（宽 = 高 × aspect）定，`WorldW` 就是**画出来的宽**。
                return new Vector2(_background.WorldW, _background.WorldH) * PxPerUnit;
            }
        }

        /// <summary>自检用：底板的颜色（原版 `(1,1,1,1)` = 白实心）。</summary>
        public Color BgTint { get { return _background != null ? _background.Tint : Color.clear; } }

        /// <summary>把一件的**局部 y**（本档单位）换成 **px**。
        /// 世界档的局部单位 = 世界单位 = 108 px（`W()` 的倒数）；uGUI 档的局部单位**本来就是设计 px**。</summary>
        float PxOfLocalY(float localY) { return _frame != null ? localY : localY * PxPerUnit; }

        /// <summary>🔴 **自检用（`B-21`）：三颗钮那一行的【中心】相对锚点的 y**（px、**向上为正**）。
        /// <para>原版判据 = **0**：那一行由父件上的 `HorizontalOrVerticalLayoutGroup`
        /// （`MB_4653`：`m_ChildAlignment = 4 (MiddleCenter)`、padding 0）居中在 `Drag Attack Selector`
        /// 的 rect 里，而整条被 `set_position(单位位置)`、它自己的 pivot = 中心
        /// ⇒ **行心 = 锚点**（运行时实况：`runtime_ui_dump_Battle_Arena_1.tsv:363,366` 的
        /// `ap (310.9, 61.1)` + `anchor (0,0)` = 父 rect 左下角 + 该偏移 = 父中心 (+0,+0)）。</para>
        /// <para>⚠️ 读到的是**真几何**（图标节点的局部位置），⛔ 不是本件的布局常量；
        /// 入场动画期间会带 `EnterOffset` ⇒ 自检要先 `TickForTest` 把进度推到 1。</para></summary>
        public float ButtonRowCenterPxFromAnchor
        {
            get
            {
                for (int i = 0; i < Order.Length; i++)
                {
                    var p = _icons[i];
                    if (p == null || !p.gameObject.activeSelf) continue;
                    return PxOfLocalY(p.transform.localPosition.y);
                }
                return float.NaN;          // 一件都没显示 ⇒ 量不到（⛔ 别返回 0 冒充「对上了」）
            }
        }

        /// <summary>🔴 **自检用（`B-21`）：底板【顶边】相对锚点的 y**（px、**向上为正**）。
        /// <para>原版判据 = `BarH/2 − 18.375` = **+42.7485**：`RectTransform_3254` 的
        /// `m_AnchorMin/Max = (0.5, **1**)`（父 rect 顶边）+ `m_AnchoredPosition = (0, −18.375)` +
        /// `m_Pivot = (0.5, **1**)` ⇒ 顶边 = 父顶边 − 18.375 = 父中心 + `BarH/2` − 18.375。</para>
        /// <para>⚠️ 按**真几何**读：uGUI 档读 `anchor`（父中心）+ `pivot` 补出顶边；世界档读
        /// `ImageQuad.anchor`。⛔ 不读 `BgTopAboveAnchorPx` 这个常量（那是被测实现 = 自证）。</para></summary>
        public float BackgroundTopPxFromAnchor
        {
            get
            {
                if (_background == null) return float.NaN;
                if (_background.IsUgui)
                {
                    var rt = _background.Rect;
                    if (rt == null) return float.NaN;
                    // 顶部 = 「枢轴那一点」+ (1 − pivot.y)·高（父 pivot 居中 ⇒ localPosition.y == anchoredPosition.y）
                    return rt.anchoredPosition.y + (1f - rt.pivot.y) * rt.sizeDelta.y;
                }
                var q = _background.Quad;
                if (q == null) return float.NaN;
                return PxOfLocalY(_background.transform.localPosition.y + (1f - q.anchor.y) * _background.WorldH);
            }
        }

        /// <summary>拖拽阈值换算成世界单位（屏高恒 10 世界单位）</summary>
        public static float DragThresholdWorld
        {
            get { return DragThreshold01 * LayoutSpace.VisibleHeight; }
        }

        /// <summary>设计分辨率下 1 世界单位 = 108 px（可见高恒定 10 单位 = 1080 px）</summary>
        const float PxPerUnit = 108f;
        static float W(float px) { return px / PxPerUnit; }

        /// <summary>**矩形命中**（只给世界档用）：`d` = 指针 − 件中心（世界单位），`wPx/hPx` = 原版节点 rect。
        /// 两轴各自按屏宽整体缩放折过来（同旧那行 `W(ButtonH) * 0.5 * LayoutSpace.Scale` 的口径）。
        /// <para>判据 = 原版受体就是这两颗 `Image` 的矩形（见 `UpdatePointer` 里 2026-10-25 那段），
        /// 与「圆」无关。uGUI 档不用它（那里由 `EventSystem` 按真实矩形判）。</para></summary>
        static bool HitRect(Vector3 d, float wPx, float hPx)
        {
            float hw = W(wPx) * 0.5f * LayoutSpace.Scale;
            float hh = W(hPx) * 0.5f * LayoutSpace.Scale;
            return Mathf.Abs(d.x) <= hw && Mathf.Abs(d.y) <= hh;
        }

        // z 分层：**越负越靠前**（相机看 -Z）。整块选择器都排在卡牌前面，见 `RefreshLayout`
        const float ZBg = -2.00f, ZIcon = -2.10f, ZRing = -2.15f, ZText = -2.20f;
        /// <summary>帧节点在**宿主容器**里的排位 z（只有 uGUI 档用）。
        /// <para>🔴 **必须拿最低的兄弟序**：原版 `Drag Attack Selector` 是 `Safe area FrontCanvas` 的
        /// `m_Children[0]`（兄弟序 **0/14**）⇒ 它是该组**最底下一件**（`T09` §八·3 · `T09b` §A·3）。</para>
        /// <para>🔑 **怎么落到「最低兄弟序」**：`HudCanvas.Add` 按 z **降序**排兄弟序
        /// （z 大的排前面 = 画在底下，见 `HudCanvas.Group.Add`）⇒ 取一个**比同容器任何件都大**的值即可。
        /// 宿主画布里那些件的 z 都在 **−2.3 … 0.6** 这一带（`MulliganPanel` −0.9 · `MultiCardDisplay` −2.3 ·
        /// `CardDisplayWindow` −2.0 …）⇒ 取 100 留足余量（本件进宿主后 `siblingIndex = 0`）。
        /// ⚠️ 它是**哨兵值、不是量出来的数**；⛔ 别拿它当「本件的世界 z」用（画布上早没有那个东西了）。</para></summary>
        const float ZFrameBottom = 100f;

        /// <summary>横排顺序 —— 按原版的 `directionToPivotPoint`：近战左、技能中、远程右</summary>
        static readonly AttackKind[] Order = { AttackKind.Melee, AttackKind.Ability, AttackKind.Ranged };

        // ==================================================================
        //  入场动画（原版 `AttackTypesButtonsController.Update → MoveButtons`
        //  → `CardDisplayAttackTypeButton.MoveButton`）—— 2026-09-29 补
        // ==================================================================

        /// <summary>原版 `CardDisplayAttackTypeButton.furtherPointDistance`（字段 `+0x4c`）：
        /// 按钮入场时从**槽位**沿 `directionToPivotPoint` **外扩**多少 px。原版三个值 **45 / 45 / 80**
        /// （近战 / 远程 / 技能 —— 技能那格远一些，因为它正下方、离底板边最近）。</summary>
        static float FurtherDist(AttackKind k) { return k == AttackKind.Ability ? 80f : 45f; }

        /// <summary>原版 `CardDisplayAttackTypeButton.directionToPivotPoint`（字段 `+0x44`）——
        /// **外扩方向**：近战 `(-0.5,-1)` 左下 · 技能 `(0,-1.1)` 正下 · 远程 `(0.5,-1)` 右下。
        /// `MoveButton` 里**归一化之后**才乘 `furtherPointDistance`（归一化阈值 `DAT_1834b2f44` = `1e-5f`；
        /// 退化时退回 `Vector2.zero` ⇒ 不外扩）。</summary>
        static Vector2 DirectionOf(AttackKind k)
        {
            switch (k)
            {
                case AttackKind.Melee:  return new Vector2(-0.5f, -1f);
                case AttackKind.Ranged: return new Vector2( 0.5f, -1f);
                default:                return new Vector2( 0f, -1.1f);
            }
        }

        /// <summary>入场进度：`0` = 还在**外扩位**（刚弹出那一帧）· `1` = 已经**归位**。
        /// 由 <see cref="Tick"/> 按**累计的视口拖拽位移**推进（原版口径，见上面那段更正）；
        /// `RefreshLayout` 每帧拿它算按钮位置。</summary>
        float _enterT = 1f;

        /// <summary>原版 `AttackTypesButtonsController.accumulatedDrag`（`+0x50`/`+0x54`）——
        /// **累计的视口拖拽位移**。`OnEnable` 把它清零（`AttackTypesButtonsController__OnEnable.c`
        /// 从静态零向量写 `+0x50`/`+0x54`）⇒ 我们每次 `Show()` 也清。</summary>
        Vector2 _accumulatedDrag;

        /// <summary>自检用：那个累计量（判「停住不拖就不动」）。</summary>
        public Vector2 AccumulatedDrag { get { return _accumulatedDrag; } }

        /// <summary>自检用：入场进度（0..1）。</summary>
        public float EnterProgress { get { return _enterT; } }

        /// <summary>
        /// 推进入场动画（每帧一次）。**返回 true = 这一帧位置变了**（调用方要重排才看得见）。
        ///
        /// 🔴 **照原版**：`MoveButton(t)` 里是 `pos = lerp(归位位 + 方向×外扩距, 归位位, t)`，
        /// `t` 由控制器的 `MoveButtons` 每帧算（见 <see cref="PushDrag"/>）。
        /// `dt` 只喂**安全窗**（悬停那条，见 <see cref="TickHover"/>），**不再驱动动画**。
        /// </summary>
        public bool Tick(float dt)
        {
            if (!Visible) return false;
            TickHover(dt); AdvanceRingRotation(dt);
            return PushDrag(TouchInputManager.TouchDragDeltaViewport);
        }

        /// <summary>🆕 A658 自检口：**自己喂**「这一帧的视口拖拽位移」。
        /// 批处理里没有输入层（`TouchInputManager.Update` 不会跑）⇒ 不喂就永远推不动进度。
        /// ⚠️ 走的是**和真实那一路完全相同**的 `TickHover` + `PushDrag`，不另写一份判据。</summary>
        public bool TickForTest(float dt, Vector2 dragDeltaViewport)
        {
            if (!Visible) return false;
            TickHover(dt); AdvanceRingRotation(dt);
            return PushDrag(dragDeltaViewport);
        }

        /// <summary>原版 `MoveButtons` 那两句（累计 + 求 t）—— **判据只此一处**。</summary>
        bool PushDrag(Vector2 dragDeltaViewport)
        {
            // 这一帧没拖 ⇒ 累计量不变 ⇒ 进度不变（原版：加个零向量等于没加）。返回 false = 不用重排。
            if (dragDeltaViewport.x == 0f && dragDeltaViewport.y == 0f) return false;
            _accumulatedDrag += dragDeltaViewport;
            float t = Mathf.Clamp01(-_accumulatedDrag.y / DragThreshold01);
            if (t == _enterT) return false;
            _enterT = t;
            RefreshLayout();
            return true;
        }

        // ==================================================================
        //  悬停即选中（🆕 2026-10-13 · A462）
        //
        //  原版这一处**根本不是点击**：三颗钮的组件 `CardDisplayAttackTypeButton` 只实现
        //  `IPointerEnter/ExitHandler`（`CardDisplayAttackTypeButton.cs:12`），
        //  `__Update.c:20-30` 三格一齐为真就发 + 把指令发出去：
        //      `inputOverButton(+0x92)` && `sendInput(+0x90)` && `!isInputOverSent(+0x91)`
        //  · `+0x92`：`OnPointerEnter` 置 1 / `OnPointerExit` 置 0（指针在不在这一格上）
        //  · `+0x90`：`Toggle(true)` 与「兄弟钮 `OnPointerExit`」都走 `DisableTemporary` →
        //             `StartSafeTouch` 协程：**先置 0、等 `disableTimeAfterPointerExit` 再置 1**
        //             （`<StartSafeTouch>d__37__MoveNext.c:17,31`）—— 三颗的序列化值都是 **0.1**
        //             （`MonoBehaviour_{4956,4361,4449}.json`）⇒ **0.1s 安全窗**
        //  · `+0x91`：发过就置 1，`OnPointerExit` 才清 ⇒ **同一次进入只发一次**
        //  ⇒ 我们这一侧逐格复刻这三格（见 `HoverPickReady` / `MarkHoverPicked` / `TickHover`）。
        // ==================================================================

        /// <summary>原版 `CardDisplayAttackTypeButton.disableTimeAfterPointerExit`（字段 `+0x50`）：
        /// **0.1 秒**（三颗序列化值实测相同）。语义见上面那段。</summary>
        public const float SafeTouchSeconds = 0.1f;

        float _safeLeft;                            // 安全窗剩余（秒）
        AttackKind _hoverWas = AttackKind.None;     // 上一帧压着哪一格（用来判「换格」）
        AttackKind _hoverSent = AttackKind.None;    // 本次进入已经发过的那一格（原版 `+0x91`）

        /// <summary>指针**压着某一格、安全窗走完、且本次进入还没发过** ⇒ 可以定下来。
        /// （原版那三格一齐为真。⛔ 别把它降级成「压着就算」—— 安全窗是原版有的那一格。）</summary>
        public bool HoverPickReady
        {
            get
            {
                return Visible && Hovered != AttackKind.None
                    && _safeLeft <= 0f && _hoverSent != Hovered;
            }
        }

        /// <summary>自检用：安全窗还剩多少秒（`0` = 已经可以发）。</summary>
        public float HoverSafeLeft { get { return _safeLeft; } }

        /// <summary>记「这一格已经发过了」—— 同一次进入只发一次（原版 `+0x91 = 1`）。
        /// ⚠️ 由**调用方**在真的把指令发出去之后调（`BattleDriver.DrivePlayerTurn` ①）。</summary>
        public void MarkHoverPicked()
        {
            _hoverSent = Hovered;
            // 🆕 2026-10-24（`W10`）：**只有技能那颗有 `soundOnSelect`**（原版三份序列化值里
            //   近战/远程是空 GUID、技能那颗才是 `f83d6aab…`）⇒ 只在 `Ability` 这一格响。
            //   播的时机 = 原版 `DoPointerOverButtonActions`（发指令那一拍，跟这里同义）。
            if (Hovered == AttackKind.Ability) PlaySelectSound();
        }

        /// <summary>🆕 2026-10-24（`W10`）：技能钮选中音 —— cue `ButtonClick` → clip `UI Select Toggle`
        /// （GUID → cue → clip 的整条解析见 <see cref="SelectSoundCue"/>）。**2D 播**
        /// （原版 `Play2D`，`MixerType = FX`，`WFSoundPlayer` 那边已经是 FX 组）。</summary>
        void PlaySelectSound()
        {
            WarpforgeVFX.WFSoundCue cue;
            if (!WarpforgeVFX.WFSoundBank.TryGetCue(SelectSoundCue, out cue) || cue == null)
            {
                SelectSoundMisses++;
                if (!_selectSoundWarned)
                {
                    _selectSoundWarned = true;
                    Debug.LogWarning("[Battle] 技能钮选中音：`Resources/animfx_sounds.json` 里查不到 cue `"
                                   + SelectSoundCue + "`（clip `UI Select Toggle`）—— 这一声不会响。"
                                   + " 它本来是**原版就有的**（`soundOnSelect` GUID f83d6aab…），不是「原版没声音」。");
                }
                return;
            }
            // 🔴 **2026-10-24（执行代理 `W24` · `D2_BattleScene11红诊断.md` §2）**：「走到播放口」的判据
            //   改看 `WFSoundPlayer.Played` 的**增量** —— 那是这个开关自己的契约口
            //   （`WFSoundPlayer.cs:17` 的 doc：「`Enabled = false` 时**只记账不发声**……**但 `Played` 照样涨**」）。
            //   原来写的是 `Play(...) != null`，而 `Play` 是 **先 `Played++` 再 `if (!Enabled) return null`**
            //   ⇒ 批处理静音档下恒为 null ⇒ `SelectSoundPlays` 不动；cue 又查得到（上面那支没走）⇒
            //   `SelectSoundMisses` 也不动 ⇒ **两个计数器都被同一个 0 掩盖**（自检必红，且分不清
            //   「接线没生效」与「cue 不在」）。⚠️ 灭自证照旧：删掉 `MarkHoverPicked` 里那句 ⇒ 2 个都不动 ⇒ 红。
            int playedBefore = WarpforgeVFX.WFSoundPlayer.Played;
            WarpforgeVFX.WFSoundPlayer.Play(cue, Vector3.zero, true);
            if (WarpforgeVFX.WFSoundPlayer.Played > playedBefore) SelectSoundPlays++;
            else SelectSoundMisses++;
        }

        /// <summary>安全窗 + 「一次进入只发一次」那两格（原版 `Update` 的对应物）。`Tick` 每帧调。</summary>
        void TickHover(float dt)
        {
            if (_safeLeft > 0f)
            {
                _safeLeft -= dt;
                if (_safeLeft < 0f) _safeLeft = 0f;
            }
            if (Hovered != _hoverWas)
            {
                // 换到另一格（或离开整条）⇒ 原版那条 `OnPointerExit` → `DisableTemporary(其它钮)` 的等价物：
                // 重开安全窗 + 把「已经发过」清掉（所以「离开再回来」会**再发一次**，与原版一致）。
                _hoverWas = Hovered;
                _hoverSent = AttackKind.None;
                _safeLeft = SafeTouchSeconds;
            }
        }

        /// <summary>外扩偏移 —— 进度 1 时是零。**本档的局部单位**（世界档 = 世界单位 / uGUI = 设计 px）。
        /// ⚠️ 这里原来只有 `W(…)*Scale` 那一档；`US()` 就是它、并在 uGUI 档**不乘 `Scale`**
        /// （窄屏缩放归 `CanvasScaler`）—— 乘两次的话外扩距会小 33%（4:3）。</summary>
        Vector3 EnterOffset(AttackKind k)
        {
            if (_enterT >= 1f) return Vector3.zero;
            var d = DirectionOf(k);
            float len = d.magnitude;
            if (len <= 1e-5f) return Vector3.zero;                 // 原版退化支：不外扩
            d /= len;
            float dist = US(FurtherDist(k)) * (1f - _enterT);
            return new Vector3(d.x * dist, d.y * dist, 0f);
        }

        // ==================================================================
        //  状态
        // ==================================================================

        public class Option
        {
            public AttackKind Kind;
            /// <summary>能不能选（没这个攻击力/技能在冷却 → false，按钮置灰且点不动）</summary>
            public bool Enabled;
            /// <summary>
            /// 按钮上的**数字角标**（原版 `Select Active Skill Button/ValueText`：字号很大、贴在中下部）。
            /// 原版只有主动技能那一格有；近战/远程的 `buttonIconValue` 是 null。
            /// </summary>
            public string Badge;
            /// <summary>
            /// 🆕 2026-09-25 **这一格按钮用哪张底图**（`Resources/Art/ui/` 下的文件名；null = 按
            /// <see cref="AttackSelector.IconName(AttackKind)"/> 的默认）。
            ///
            /// 为什么要有它：原版**主动技能那一格的 icon 是按卡运行时赋的**
            /// （`BattleCardUI` 的 `buttonIcon`），而「替代行动」那五个阵营**各有专属一张**
            /// ——`Attack type button {Pray, Duty, Ferocity, Agenda, Oath}`（128×128，
            /// 在 `assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Texture2D/`，
            /// 2026-09-25 导进 `Resources/Art/ui/Attack_type_button_&lt;X>.png`）。
            /// ⇒ 有替代行动的卡**不该再共用那张占位图**（见 <see cref="AttackSelector.IconName(AttackKind)"/> 的注释）。
            /// </summary>
            public string IconName;

            /// <summary>🆕 **2026-10-24（`W10`）**：这张卡的**阵营值**（`CardArmy`，见 `ArmyIcons.Id(faction)`）。
            /// `> 0` 时技能钮的图标**按军队**查（原版 `ArmyUtilities.GetActiveAbilityIconForArmy` →
            /// `activeAbilityIcons`，只有 5 条）而不是按关键词 —— 判据链与 5 条真值见 `Core/ArmyIcons.cs`。
            /// <para>⚠️ 默认 `0` = 调用方没给 ⇒ 保持旧口径（`IconName` / 占位图）。原版在
            /// 「阵营查不到图标」时**整颗技能钮不显示**（`AttackTypesButtonsSelector.c:54-56`），
            /// 这一档只有给了 `Army` 才做得到（见 <see cref="Hidden"/>）。</para></summary>
            public int Army;

            /// <summary>🆕 **2026-10-24（`W10`）**：这一格的替代行动关键词（`duty`/`pray`/`ferocity`/`agenda`/`oath`），
            /// **只给 tooltip 兜底文案用**（`TipText.Trait(key)`）。空 = 没有。</summary>
            public string AltKeyword;

            /// <summary>🆕 **2026-10-24（`W10`）**：技能钮 tooltip 的**标题 / 正文**（原版
            /// `CardDisplayAttackTypeButton` 的 `+0x68` / `+0xa8`，见 `HighlightIfNeeded.c:50-70`）。
            /// 都空 ⇒ 用选择器那条 `note`（技能效果本来就写在那儿，见 `Show` 的 `note` 形参）。
            /// ⚠️ 只有**主动技能那一颗**会弹（原版 `if (param_2 != 4) return;`）。</summary>
            public string TooltipTitle, TooltipBody;

            /// <summary>🆕 **2026-10-24（`W10`）**：整颗钮**不显示**。原版唯一的成因 =
            /// 「按军队查 `activeAbilityIcons` 取不到图标」⇒ `CardDisplayAttackTypeButton__Toggle(btn, false)`
            /// （`AttackTypesButtonsSelector.c:54-56`）。⛔ **不是**「不可选」（那是 `Enabled = false`：钮还在、只是灰的）。</summary>
            public bool Hidden;
        }

        readonly List<Option> _options = new List<Option>();
        // 🔴 **2026-10-16（`T09` 批 3）：类型从 `ImageQuad` 换成 `HudPiece`**（适配层；先例 = HUD 图族那 39 个字段）。
        //   「一个件要么是世界空间 quad、要么是 uGUI 的 `RectTransform + Image`，二选一」的规矩 → `Battle/HudPiece.cs` 文件头。
        //   `Label` **不换**：它自己就有 uGUI 后端（`Label.CreateUgui` / `IsUgui`），见 `Battle/Label.cs` 文件头。
        readonly HudPiece[] _icons = new HudPiece[3];
        readonly HudPiece[] _rings = new HudPiece[3];
        readonly Label[] _captions = new Label[3];

        HudPiece _background;
        // 🔴 **2026-10-25（`W6`）**：自造的标题 `_title` / `_titleText` **已删** —— 原版 `Drag Attack Selector`
        //   的 subtree 里没有这一件（判据见 `BuildOnCanvas` 里那段红字：逐颗列了原版 4 个子节点）；
        //   官方本地化表（10,854 条的 `en+zh-CN`，I2 `I2Languages` 解码产物）也没有 `ChooseAction` 这个键
        //   （`terms/Battle/**` 156 条里 0 命中，`ChooseAction` / `Choose action` 全 0）。
        //   `Show(title:)` 的形参**保留**（不改正本外的调用点 `BattleDriver.cs:13756`），但不再显示任何东西。
        //   ⛔ 别把标题建回来。
        /// <summary>技能正文那一串（`Show(note: …)` 传进来的）。🔴 **2026-10-25（`W4`）起不再画成副标题行**
        /// —— 原版 `Drag Attack Selector` 的 subtree 里没有这一件
        /// （判据 → `资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:233-241`：只有底板 / 三颗钮 / 三圈 /
        /// `ValueText`）。这串仍然留着当**技能钮 tooltip 正文的兜底**（`RefreshAbilityTooltip`）——
        /// 原版那串正是 `RawCardScript.GetLocalizedAbilityDesc` → `CardDisplayAttackTypeButton+0xa8`
        /// （`AttackTypesButtonsController__ToggleAttackTypesSelector.c` 末段）⇒ 数据来源不变，只是不再是可见标签。
        /// ⛔ 别把它加回一行 `Label`。</summary>
        string _noteText;
        bool _built;

        // ==================================================================
        //  uGUI 档（`T09` 批 3）—— 宿主 / 帧 / 双模式
        // ==================================================================

        /// <summary>画布上那个帧节点的名字 = **原版 GO 名**（与 `Mulligan` / `Card Display Window` 那批同规矩）。</summary>
        public const string FrameName = "Drag Attack Selector";

        /// <summary>本件那块 **uGUI 子画布**的名字 = **原版宿主名 + `" Canvas"`**（`T09` §五·补⑩ 第 1 步）。
        /// 判据 = `T09c` §一 我这一行：`Drag Attack Selector` 落 `OverHand` / order **0**、原版子件序 **0/14**。
        /// ⛔ **只此一份**（建它、`HudCanvas.SubCanvasRank` 找它都引用这里）。
        /// <para>🔴 **2026-10-16（`G1-1` · 修波）新增** —— 与主对话裁定（`T09` §八·3）配套的**单一来源**：
        /// 裁定前这串名字在**两处各写一遍**（`Battle/BattleDriver.cs` 的登记点 + `Battle/HudCanvas.cs`
        /// 的 `SubCanvasRank` 那张表里的**字面量**）⇒ 补上本常量，与 `SkillPanel.SubCanvasName` /
        /// `UnitChatPanel.SubCanvasName` / `ChatPopupPanel.SubCanvasName` /
        /// `AlliancePanelWindow.SubCanvasName` **同形**。
        /// ⚠️ 它的值与 `FrameName + " Canvas"` **逐字节相同**（**27 字符**，`G1` 现核过全等）
        /// ⇒ 本常量的价值是**单一来源**，⛔ **不是**「原先会拼错」（那一说已被现核推翻）。</para></summary>
        public const string SubCanvasName = "Drag Attack Selector Canvas";

        /// <summary>本件的 uGUI 宿主（= **本件那块子画布的 `RectTransform`**）。`null` ⇒ **世界档**。
        /// <para>🔴 **2026-10-16（`G1-2` · 修波）就地订正（铁律 5）**：本条原来写着「原版那个节点是
        /// `overrideSorting = false` 的嵌套 Canvas ⇒ 它**不另立排序组**、随父画布按兄弟序渲染（`T09` §八·3）
        /// ⇒ 我们**不许**给它起一块自己的画布；『并进哪一块』是**待主对话裁**的口径」——
        /// **这两句都已过期**（都是**裁定之前**写的；留痕：原文见本文件 `HEAD` 版的 `:391-393`）。</para>
        /// <para>✅ **现行裁定（主对话 · `T09` §八·3，该节自己已就地订正）**：**另起一块
        /// `"Drag Attack Selector Canvas"`**（`OverHand` / order 0），排在 `HudCanvas.SubCanvasRank`
        /// 的 `OverHand` 段**最前**（= 原版子件序 **0/14**）。**原来那两句的错因**：**兄弟序只在一份
        /// 「画布之内」生效；跨画布的先后只由 `SubCanvasRank` 定** ⇒ 「并进别人的画布」会把它连带
        /// **降到那块画布那一档**（并进 `Mulligan Canvas` = 3/14 ⇒ 被 `ActiveSkillDesc`(1/14) 与
        /// `Unit Chat`(2/14) **一起盖住**），**与原版恰好相反**。
        /// ⚠️ **如实记的一处差异**：我们这块子画布 `overrideSorting = true`，而原版那个节点是 `false`
        /// —— 复刻的是**渲染次序**（结果一致），⛔ **不是那个开关本身**（`BattleDriver` 登记点已自陈）。</para>
        /// <para>⚠️ 宿主由**驱动层**登记、本文件自己不建画布（本件是场景挂的组件、没有 `Create`）——
        /// 见 <see cref="UseHost"/>。</para></summary>
        RectTransform _host;

        /// <summary>画布上的帧节点（= 原版 `Drag Attack Selector`）。**世界档恒 `null`**。</summary>
        RectTransform _frame;

        /// <summary>🆕 **2026-10-20（`T09` A3-e）**：三颗 `Highlight` 上的 `EventTrigger`
        /// （`PointerEnter / PointerExit` = 原版 `CardDisplayAttackTypeButton` 那两个接口）。
        /// 下标 = `Order` 下标（同 `SlotOf`）；世界档恒 `null`；`null` = 那一格没挂上。</summary>
        readonly EventTrigger[] _hoverTriggers = new EventTrigger[3];
        /// <summary>🆕 **2026-10-25**：三颗**钮本体**（`Image`，118.372×118.806）上的 `EventTrigger`。
        /// <para>为什么原来没有：那时拿圈子当唯一受体；但原版 **`Highlight` 子件只在「已选打法」那一格
        /// `SetActive(true)`**（`CardDisplayAttackTypeButton__HighlightIfNeeded.c` 第一句）⇒ 没选中的格子
        /// 只有**钮本体**吃射线（`m_RaycastTarget = 1`）。圈一改成「只在选中时亮」（本次同批改），
        /// 只挂圈就会让另外两格**整天悬停不中**。</para>
        /// <para>⚠️ 下标 = `Order` 下标（同 `SlotOf`）；世界档恒 `null`。</para></summary>
        readonly EventTrigger[] _iconTriggers = new EventTrigger[3];
        /// <summary>自检计数：`PointerEnter` 各来过几次（只增）。</summary>
        readonly int[] _hoverEnterCount = new int[3];
        /// <summary>自检计数：`PointerExit` 各来过几次（只增）。</summary>
        readonly int[] _hoverExitCount = new int[3];

        /// <summary>诊断/断言用：本件现在是不是**画布档**。**判据只此一处** = 「帧节点在不在」。
        /// ⚠️ 不另设一个 bool —— 两份状态迟早不一致（本工程血规：两处写同一条规则 = 迟早不一致）。
        /// 用 `!= null` 是 Unity 的重载比较（销毁过的对象也算 null）。
        /// ⚠️ 名字照同批同组的 `SkillPanel.UguiMode`（同一件事别长出两个名字）。</summary>
        public bool UguiMode { get { return _frame != null; } }

        /// <summary>自检用：本件并进的那块**宿主画布**的 `RectTransform`（查它的层 / `overrideSorting` 用）。</summary>
        public RectTransform CanvasRect { get { return _frame != null ? _host : null; } }

        /// <summary>自检用：画布上那个**帧节点**（世界档 = `null`；读它的 `localPosition` / `sizeDelta`）。</summary>
        public RectTransform FrameRect { get { return _frame; } }

        /// <summary>🔴 **把本件接到 uGUI 档**（先例 = `Tooltip.UseHost` · `ErrorMessageBanner.Create(..., uguiHost)`
        /// · 同批同组的 `SkillPanel.UseHost`）。
        /// <paramref name="host"/> = **本件并进去的那块画布的 `RectTransform`**。
        /// <para>🔴 **为什么必须是「建 UI 的那一方」来登记**：组件是**场景挂的**（`AddComponent<AttackSelector>()`，
        /// 没有 `Create`）⇒ 只有驱动层知道根画布建好了没（`Awake` 还早于 `LayoutSpace.Apply`）。</para>
        /// <para>传 `null` = **宿主没建起来**：**留在世界档**（= 与改之前逐位相同）、并**出声**
        /// （⛔ 不静默 —— 那正是「一半在画布、一半在世界」那种 16:9 下看不见的半迁移态）。</para>
        /// <para>⚠️ **可以在 `Build()` 之后再调**：那时会**拆掉世界档的件、按画布档重建**
        /// （`Awake()` 会先跑一次 `Build()`，而驱动层登记宿主的时刻由别人定 —— 两种次序都要能落地）。
        /// 拆旧建新是「一个件只许在一边」那条铁律的落实，⛔ **不是**「两边都建」。</para></summary>
        public void UseHost(RectTransform host)
        {
            if (host == null)
            {
                Debug.LogError("[Battle] 攻击方式选择器：`UseHost(null)` —— 宿主画布没建起来"
                             + " ⇒ 本件**留在世界档**（画面与改之前逐位相同，"
                             + "但非 16:9 下会与已迁到画布的同组件分家）");
                return;
            }
            if (_host == host) return;
            _host = host;
            if (_built)
            {
                Debug.Log("[Battle] 攻击方式选择器：`UseHost` 排在 `Build()` 之后 ⇒ 拆掉世界档的件、"
                        + "按画布档重建（⛔ 不许两边都建）。把登记挪到第一次 `Build()` 之前可以省掉这一次重建。");
                Rebuild();
            }
        }

        /// <summary>换档：把本件建出来的东西**全拆掉**、再按新宿主重建（保留「正在显示」这一档状态）。
        /// ⚠️ **只拆本件自己的节点**（含帧）—— ⛔ 不碰宿主画布：它是**别人**建的、共享的。</summary>
        void Rebuild()
        {
            bool wasVisible = Visible;
            var opts = new List<Option>(_options);
            string title = null, note = _noteText;   // ⚠️ `title` 2026-10-25 起不显示（自造件已删），这里恒 null
            int sel = (int)_selected;                 // 枚举值 = 原版 `attackType` 位标志（0/1/2/4）⇒ 直接喂回去
            Vector3 at = _anchorAt;

            ClearPieces();
            _built = false;
            Visible = false;
            Hovered = AttackKind.None;
            _options.Clear();

            Build();
            if (wasVisible && _built) Show(opts, title, note, at, sel);
        }

        /// <summary>销毁本件建出来的全部节点（`DestroyImmediate` —— 批处理下没有帧循环，`Destroy` 不生效）。</summary>
        void ClearPieces()
        {
            var doomed = new List<GameObject>();
            if (_background != null) doomed.Add(_background.gameObject);
            for (int i = 0; i < Order.Length; i++)
            {
                if (_icons[i] != null) doomed.Add(_icons[i].gameObject);
                if (_rings[i] != null) doomed.Add(_rings[i].gameObject);
                if (_captions[i] != null) doomed.Add(_captions[i].gameObject);
            }
            if (_frame != null) doomed.Add(_frame.gameObject);       // ⛔ 只到帧这一级（宿主画布是共享的）
            foreach (var go in doomed) if (go != null) Object.DestroyImmediate(go);

            _background = null;
            for (int i = 0; i < Order.Length; i++) { _icons[i] = null; _rings[i] = null; _captions[i] = null; _hoverTriggers[i] = null; _iconTriggers[i] = null; }
            _frame = null;
        }

        // ── 三个**单位换算口**（双模式的全部机关都在这里）─────────────────────────────
        //  ⚠️ 规矩（`T09` §五·补⑥ 末）：**旧 quad 的局部单位 = 世界单位 · uGUI = 设计 px（差 108）**
        //    ⇒ 凡「拿 px 常量去摆一件 / 量一件」的地方**一律过这三个口**，⛔ 别在调用点写 `_frame != null ? … : …`
        //    （那是同一条规矩的第二份写法）。**16:9 下两档逐位相同** ⇒ 写错了断言不会红（只有真机看得出来）。

        /// <summary>px → **本档的单位**（**不含**窄屏整体缩放）。世界档 = `px/108`；uGUI 档 = 原样（设计 px）。</summary>
        float U(float px) { return _frame != null ? px : W(px); }

        /// <summary>px → 本档单位 **并乘窄屏整体缩放**（`LayoutSpace.Scale`）—— 用在「按屏宽铺开的量」上
        /// （按钮间距 / 入场外扩距），逐处对应原来那个 `W(…) * LayoutSpace.Scale`。
        /// ⚠️ uGUI 档**不再乘**：窄屏缩放由画布的 `CanvasScaler(Expand)` 负责（乘了就是缩两次）。</summary>
        float US(float px) { return _frame != null ? px : W(px) * LayoutSpace.Scale; }

        /// <summary>z 那个形参的对应物：世界档按原值分层（**越负越靠前**）；uGUI 档**恒 0**
        /// —— 画布上谁压谁靠**兄弟序**，建件时已经登记过了（`HudCanvas.Add`）。</summary>
        float Z(float zWorld) { return _frame != null ? 0f : zWorld; }

        public bool Visible { get; private set; }
        /// <summary>指针现在压着哪个按钮（压着才放大 1.3 + 亮黄圈）</summary>
        public AttackKind Hovered { get; private set; }
        public IReadOnlyList<Option> Options { get { return _options; } }

        static int SlotOf(AttackKind k)
        {
            for (int i = 0; i < Order.Length; i++) if (Order[i] == k) return i;
            return -1;
        }

        Option Find(AttackKind k)
        {
            foreach (var o in _options) if (o.Kind == k) return o;
            return null;
        }

        // ==================================================================
        //  建 + 摆
        // ==================================================================

        void Awake() { Build(); }

        public void Build()
        {
            if (_built) return;
            _built = true;

            CardArt.Load();

            // 宿主 = **别人登记进来的那一块既有画布**（见 `UseHost`；本件自己不建画布、也不挑画布）。
            // 没登记 ⇒ **世界档**（与改之前逐位相同）—— 这是 `Awake()` 那一趟的常态
            // （那一刻连 HUD 相机都还没立起来）；驱动层登记时会走 `UseHost` 的「拆旧建新」。
            if (_host != null) BuildOnCanvas(_host);
            else BuildInWorld();

            Hide();
        }

        /// <summary>uGUI 档：件挂在**并进去的那块宿主画布**上；帧 = 原版 GO `Drag Attack Selector`。
        /// 帧的 rect **逐字照原版**（`RectTransform_2973.json`：中心锚 + 中心枢轴 +
        /// `sizeDelta 621.7249755859375 × 122.24700164794922`，我们的常量取 621.7 / 122.2）；
        /// ⚠️ **2026-10-25 就地订正（铁律 5）**：原来这里引的是 `RT_3166` —— 那是另一颗 `722.66×403.13` 的节点，
        /// 值与本件对不上（同批 `RT_2819/2997` 是它在 arena2 的兄弟件，别混）。
        /// 它的**位置**每次 `RefreshLayout` 按 `_anchorAt` 摆（原版运行期也是 `set_position(单位位置)`，
        /// `UnitOnBoardAttackTypeSelector__Toggle.c:112-126`）。</summary>
        void BuildOnCanvas(RectTransform host)
        {
            var fgo = new GameObject(FrameName, typeof(RectTransform));
            fgo.transform.SetParent(host, false);
            _frame = (RectTransform)fgo.transform;
            _frame.anchorMin = _frame.anchorMax = _frame.pivot = new Vector2(0.5f, 0.5f);
            _frame.anchoredPosition = Vector2.zero;
            _frame.sizeDelta = new Vector2(BarW, BarH);
            HudCanvas.Add(_frame, ZFrameBottom);     // ⇒ 宿主容器里 siblingIndex = 0（原版兄弟序 0/14）

            var P = (Transform)_frame;

            // 底板：原版那格 `Image` **没有 sprite**（`m_Sprite = null`）⇒ 画出来就是一块**纯色实心矩形**；
            // 实绘 514.8 × 125.4（= `m_SizeDelta 780×285` × `localScale 0.44` × 运行期横向 ×1.5）。
            // 中心 = **帧原点**（= 锚点）：原版那格是 pivot `(0.5,1)` + `ap (0,−18.375)` ⇒ 顶端挂在锚点那一级。
            _background = HudCanvas.MakeImageInParent(P, Solid(), "SelectorBackground",
                                                      0f, 0f, BgBaseW, BgH, ZBg, new Color(1f, 1f, 1f, 1f));
            //   ⚠️ 初值给 `BgBaseW`（= t=1 归位后的实绘宽 343.2）；真正的宽由 `RefreshLayout` 每帧按
            //      「颗数 × 入场进度」重算（见 `BgBaseW` 的 doc）。⛔ 别在这里写死 514.8。
            // 🆕 2026-10-20（`T09` A3-e）：原版 `Select Attack Background` 的 `Image.m_RaycastTarget = 1`
            //   （`MB_4492`；无 sprite、纯色）⇒ 照抄成射线靶。**不挂处理器** —— 原版那块也没有动作。
            if (_background != null && !_background.SetRaycastTarget(true))
                Debug.LogWarning("[Battle] 攻击方式选择器：底板没设成射线靶（不静默）");

            // 🔴 **2026-10-25（`W4` / `W6`）：这里原来建了两行自造文字**（标题 `SelectorTitle` + 技能小字 `SelectorNote`）
            //   —— **两行都删了**。判据（两级，都查不到）：
            //   ① 原版 `Drag Attack Selector` 的 subtree（解包原始资产逐颗核，`bundle_scenes_scenes_battlearena1`：
            //      `RectTransform_2973.m_Children` = **恰好 4 个** → `Select Attack Background`(3061，无子件) ·
            //      `Select Melee Button`(3368→`Highlight`) · `Select Active Skill Button`(2626→`Highlight`+`ValueText`) ·
            //      `Select Range Button`(2804→`Highlight`)；同一结论见
            //      `资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:233-241`（13 场全树同构）
            //   ② 官方本地化表（`Unity/数据/游戏数据/官方本地化_en+zh-CN.json`，10,854 条 = I2 `I2Languages` 解码产物）
            //      里 `ChooseAction` **0 命中**（`terms/Battle/**` 共 156 条，也没有 `Phrase/*` 这一类键）
            //   ⇒ 原版没有这块标题；那句 `CardText.Phrase("CHOOSE ACTION")` 只是我们手写 `Core/Loc.cs:2771` 的兜底短语。
            //   ⚠️ 技能正文那串**没丢**：它现在是技能钮 tooltip 的正文兜底（`_noteText` / `RefreshAbilityTooltip`），
            //   与原版 `CardDisplayAttackTypeButton +0xa8` 同源。⛔ 别把这两行建回来。

            for (int i = 0; i < Order.Length; i++)
            {
                var k = Order[i];

                // 按钮本体（原版 Attack type button *，自带底盘和金属环）
                // ⛔ **别用 `HudCanvas.QuadFromTopPx`**：它的 `x/y` 是**屏幕绝对 px**（锚点 = `x/1920`），
                //    只有「父件本身就是整屏矩形」时才等于「相对父中心」——本件的父件是 621.7×122.2 的帧
                //    ⇒ 拿它会把三颗钮**静默摆到屏幕另一头**（同一个坑：`T09` §七·3）。
                var itex = CardArt.Ui(IconName(k));
                if (itex != null)
                    _icons[i] = HudCanvas.MakeImageInParent(P, itex, "Icon_" + k, 0f, 0f,
                                                            ButtonW, ButtonH,          // 原版固定 rect（PA=0）—— ⛔ 不乘贴图比例
                                                            ZIcon, Color.white);

                // 选中黄圈（原版里是按钮的子对象 `Highlight`；我们这边是**平铺的兄弟件** ——
                // 画出来同一处，差别只在父链，见报告 §⑤）
                var rtex = CardArt.Ui("Attack_type_button_highlight");
                if (rtex != null)
                    _rings[i] = HudCanvas.MakeImageInParent(P, rtex, "Highlight_" + k, 0f, 0f,
                                                            RingW, RingH,               // 原版固定 rect（PA=0）
                                                            ZRing, Color.white);
                // 🆕 2026-10-20（`T09` A3-e）· 🔴 **2026-10-25 补第二受体**：**悬停即选中**的 uGUI 送达 ——
                //   原版三颗钮各挂 `CardDisplayAttackTypeButton`（`IPointerEnter/ExitHandler`），受体是
                //   **钮自己的 `Image`**（`RT_3368/2804/2626` 118.372×118.806，`m_RaycastTarget = 1`）
                //   ∪ 它的子件 `Highlight`（176.23×176.875，`m_RaycastTarget = 1`）。
                //   🔴 **子件 `Highlight` 只在「这一格是已选打法」时才 `SetActive(true)`**
                //      （`CardDisplayAttackTypeButton__HighlightIfNeeded.c` / `…HighlightSelectedAttackTypeButtons.c`
                //       的第一句 `SetActive(highlightImage, param_2 == attackType)`）⇒ 原版**没选中**的格子
                //      只有钮本体吃射线（118.372×118.806），**选中**那一格才是 钮 ∪ 圈。
                //      ⇒ 我们两件都挂 `EventTrigger`；圈由 `RefreshPicked` 按「是不是已选」开关（关着的圈
                //      收不到 uGUI 射线，正好等价于原版）。
                //   ⚠️ 件没建出来（贴图缺）⇒ 出声，⛔ 不让这一格静默变成点不中。
                if (_icons[i] != null)
                {
                    _iconTriggers[i] = AddHoverTrigger(_icons[i], k);
                    if (_iconTriggers[i] == null)
                        Debug.LogError("[Battle] 攻击方式选择器：`" + k + "` 那一格的**钮本体**没挂上悬停"
                                     + " ⇒ 未选中时这一格悬停选不中（不静默）");
                }
                if (_rings[i] != null)
                {
                    _hoverTriggers[i] = AddHoverTrigger(_rings[i], k);
                    if (_hoverTriggers[i] == null)
                        Debug.LogError("[Battle] 攻击方式选择器：`" + k + "` 那一格的 `Highlight` 圈没挂上悬停"
                                     + " ⇒ 已选中时圈外那一圈选不中（不静默）");
                }
                if (_icons[i] == null && _rings[i] == null)
                {
                    Debug.LogWarning("[Battle] 攻击方式选择器：`" + k + "` 那一格**既没有圈也没有图标**"
                                   + " ⇒ 这一格悬停选不中（贴图缺；不静默）");
                }

                // 按钮上的数字角标（原版 `Select Active Skill Button/ValueText`）。
                // 原版字号很大（base 36 / max 89），配上它那个半透明底图，数字是按钮的主视觉
                _captions[i] = HudCanvas.MakeText(P, "", 0f, 0f, 4, new Color(1f, 0.96f, 0.80f),
                                                  new Vector2(0.5f, 0.5f), ZText, "Badge_" + k, face: TmpFont.Face.LatinCardBody);
            }
            // 位置一律交给 `RefreshLayout`（建在这儿先落 (0,0)，紧随其后的 `Hide()`/`Show()` 会立刻重排一次）
        }

        /// <summary>世界档：**与改之前逐位相同**（`ImageQuad` + `Label`，挂在选择器根下）。</summary>
        void BuildInWorld()
        {
            // 底板：原版那格 `Image` **没有 sprite**（`m_Sprite = null`）⇒ 画出来就是一块**纯色实心矩形**。
            // 🔴 **2026-09-29 照原版改（原来画错了：深色半透明 + 125.4² 方块，`BgW` 声明了没用）**：
            //    实读 `bundle_scenes_scenes_battlearena1/`（13 场都有同名节点）——
            //    `RectTransform_3061.json`：**sizeDelta 780×285 · localScale 0.44 · pivot (0.5,1)**；
            //    `MonoBehaviour_4492.json`（= 那个 Image）：**`m_Sprite` 空 · `m_Color = (1,1,1,1)`**（白、不透明）。
            //    🔴 2026-10-25 订正：实绘宽**不是常量**（原版那是一条按「颗数 × 入场进度」插值的补间，
            //    峰值 514.8（2 颗）/ 772.2（3 颗）、归位后恒 343.2）—— 判据与算式 → `BgBaseW` 的 doc。
            _background = HudPiece.Of(ImageQuad.Create(transform, Solid(), Vector3.zero,
                                           W(BgH), new Vector2(0.5f, 1f), "SelectorBackground"));
            if (_background != null)
            {
                // `ImageQuad` 按**高** + 贴图宽高比摆，纯色图 1:1 ⇒ 用 `SetAspect` 把宽定成 `BgBaseW`
                //   （宽 = 高 × aspect，与 uGUI 档同一个口）。🔴 **2026-10-25**：原来写的是
                //   `transform.localScale = (BgW/BgH, 1, 1)` —— 那条路与「宽由 `RefreshLayout` 按
                //   颗数/入场进度重算」冲突（两处都在定宽），已收成一个口。
                _background.SetAspect(BgBaseW / BgH);
                _background.SetTint(new Color(1f, 1f, 1f, 1f));
            }

            // 🔴 **2026-10-25（`W4` / `W6`）**：标题 `SelectorTitle` 与技能小字 `SelectorNote` **两行都已删** ——
            //   原版 `Drag Attack Selector` 的 subtree 里没有这两件（判据 → 上面 `BuildOnCanvas` 那条红字的① ②，
            //   解包原始资产的子件逐颗核过 + 官方本地化表 `ChooseAction` 0 命中）。
            //   技能正文那串改当**技能钮 tooltip 正文**（`_noteText`）用，⛔ 别再把两行 `Label` 建回来。

            for (int i = 0; i < Order.Length; i++)
            {
                var k = Order[i];

                // 按钮本体（原版 Attack type button *，自带底盘和金属环）
                // 🔴 **2026-10-24（`A28`）**：原版那颗 `Image` 是 **`m_PreserveAspect = 0`（拉满固定 rect）**
                //   ⇒ 宽 **118.372**、高 **118.806**。世界档的 `ImageQuad` 默认「按高定、宽由贴图比例推」
                //   ⇒ 建完立刻用 `SetAspect` 盖掉（同一招见 `EndPanel` 的骷髅底板那段）。
                //   ⛔ 只改高度、把宽留给贴图 = 换一张非正方形贴图就静默错。
                _icons[i] = HudPiece.Of(ImageQuad.Create(transform, CardArt.Ui(IconName(k)), Vector3.zero,
                                             W(ButtonH), new Vector2(0.5f, 0.5f), "Icon_" + k));
                if (_icons[i] != null) _icons[i].SetAspect(ButtonW / ButtonH);

                // 选中黄圈（原版里是按钮的子对象 `Highlight`）—— 同样按原版固定 rect 176.23 × 176.875
                _rings[i] = HudPiece.Of(ImageQuad.Create(transform, CardArt.Ui("Attack_type_button_highlight"),
                                             Vector3.zero, W(RingH),
                                             new Vector2(0.5f, 0.5f), "Highlight_" + k));
                if (_rings[i] != null) _rings[i].SetAspect(RingW / RingH);

                // 按钮上的数字角标（原版 `Select Active Skill Button/ValueText`）。
                // 原版字号很大（base 36 / max 89），配上它那个半透明底图，数字是按钮的主视觉
                _captions[i] = Label.Create(transform, "", Vector3.zero, 4,
                                            new Color(1f, 0.96f, 0.80f),
                                            new Vector2(0.5f, 0.5f), "Badge_" + k);
                // 🔴 2026-10-17（A1024③ 逐行对账）：`Label.Create` 没有 face 形参 ⇒ 建完立刻换面
                //    （原版 `Drag Attack Selector/Select Active Skill Button/ValueText` = Pragati）。
                //    本函数之后才调 `SetGlyphHeight` ⇒ 换面早于定字号，口径同 `MenuDraw.TextCore`。
                if (_captions[i] != null) _captions[i].SetFace(TmpFont.Face.LatinCardBody);
                // 🔴 2026-10-17（A1024③ 第 2 批）：`Label.Create` 没有 face 形参 ⇒ 建完立刻换面
                //    （原版 `Select Active Skill Button/ValueText` = Pragati）。`SetGlyphHeight` 在本函数之后才调，
                //    ⇒ 这里换面**早于**定字号，口径与 `MenuDraw.TextCore` 同一条。
                if (_captions[i] != null) _captions[i].SetFace(TmpFont.Face.LatinCardBody);
            }
        }

        // 🔴 **2026-10-24（`A28`）：原来的私有助手 `AspectOf(tex)` 已删** —— 它只服务于
        //   「按贴图比例定宽」那两处（按钮 / `Highlight`），而那两处的原版判据都是
        //   **固定 rect + `m_PreserveAspect = 0`**（见 `ButtonW` / `RingW` 的 doc）。
        //   ⛔ 别为了「贴图换得勤」把它加回来：原版换图（`SetButtonIcon`）只写 `Image.sprite`，
        //   **不动 `sizeDelta`**（`CardDisplayAttackTypeButton__SetButtonIcon.c:14` 只有 `set_sprite`）。

        static string IconName(AttackKind k)
        {
            switch (k)
            {
                case AttackKind.Melee: return "Attack_type_button_Melee";
                case AttackKind.Ranged: return "Attack_type_button_Ranged";
                // ⚠️ 主动技能那格**原版没有静态底图** —— 场景里 `m_Sprite` 是空的，
                //    运行时由 `buttonIcon` 赋（多半是每张卡自己的技能图标）。
                //    `Attack_type_Generic_Foreground` 那张图虽然名字像，但**在 battlearena1/2 里零引用**，
                //    所以下面这只是**我们挑的占位**，不是原版的做法。
                //    我们自己的卡没有专属技能图标，先用它 + 按钮上的数字角标（`Badge`）。
                default: return "Attack_type_Generic_Foreground";
            }
        }

        /// <summary>收起来。**不销毁节点** —— 一局里要反复弹，每次重建会掉帧</summary>
        /// <summary>«已选打法»那一档（原版 `AttackTypesButtonsController.HighlightSelectedAttackTypeButtons(unit.attackType)`）——
        /// 那圈黄圈 + 1.3 倍**挂的是它、不是指针悬停**（🆕 2026-09-29 照原版改）。</summary>
        AttackKind _selected = AttackKind.None;

        /// <summary>🆕 **2026-10-25**：上一次**真把缩放/圈落下去**的那个打法 —— 只在它与 `_selected`
        /// 不同时才起补间（原版 `HighlightSelectedAttackTypeButtons` 本来也是「被调用一次就补一次」；
        /// 我们这边它每次 `RefreshLayout` 都会被叫到，所以必须先判「变没变」）。</summary>
        AttackKind _pickedApplied = AttackKind.None;

        // ══════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-24（`W10`）· 原版 `UnitOnBoardAttackTypeSelector` / `CardDisplayAttackTypeButton`
        //  剩下那几件**表现层**行为。判据全部是反编译现读（每处自己的注释里带 `文件:行号`）。
        // ══════════════════════════════════════════════════════════════════════════════════

        /// <summary>🆕 **`pindown` ⇒ 近战那颗选了没反应**（原版 `UnitOnBoardAttackTypeSelector__AttackButtonClick.c:27-33`：
        /// `HasCurrentTrait(0x3ca /*970 = pindown*/) && param_2 == 1 ⇒ return`）。
        /// <para>⚠️ 原版那颗钮**照样亮、照样能悬停**（不置灰），只是**指令被吞掉** ⇒ 我们这里也只
        /// **不吃悬停**（= 选不中、也不会发出 commit），⛔ 不改 `Enabled`（那会让它变灰，原版不灰）。</para>
        /// <para>✅ **调用点已接（2026-10-25 现核）**：`BattleDriver.cs:13664` 每次开选择器都显式
        /// `selector.LockMelee = u.Has("pindown")`（置 false 那一半也在那儿）。原先本条写「调用点待接、
        /// 在那之前这条闸恒不生效」—— **已过期**（铁律 5）。</para></summary>
        public bool LockMelee { get; set; }

        /// <summary>🆕 **`ToggleJustOne` 那一档**（原版 `UnitOnBoardAttackTypeSelector__ToggleJustOne.c`：
        /// 整条 `SetActive(true)`，把**三颗钮全部关掉**，再只 `Toggle(true)` 那一颗）。
        /// 触发面（`UnitOnBoardAttackTypeSelector__Toggle.c:200-240` 现读）：
        /// **`displaySummonSickness` 且（`ferocity(0x4f1)` 或 `oath(0x4fb) + CanUseOathAbility`）**。
        /// ⚠️ U3 报告只记了 oath 那一支，**ferocity 那一支是本次现读补上的**。
        /// 非 `None` ⇒ `RefreshLayout` 只排这一颗。</summary>
        AttackKind _justOne = AttackKind.None;

        /// <summary>🆕 **原版 `BattleManager.StartAttackFrom` 的「起手就点亮合法目标 + 准星」**
        /// （`BattleManager__StartAttackFrom.c:20-40`：`Toggle(true)` 之后立刻 `ShowPotentialTargets` +
        /// `ToggleCrosshair(true)`，**此刻玩家还没选打法**）。
        /// <para>本件只提供**幂等入口**：`Show()` 结束时若本回调非空就打一次（`RequestPreviewTargets`）。
        /// 回调由驱动层登记 —— 它要做的两件事是 `HighlightTargets()` + `ShowReticleNow()`
        /// （⚠️ 后者现在要求 `_command != None`，得放宽成「用单位当前 `attackType`，可为 0」）。</para>
        /// <para>✅ **调用点已接（2026-10-25 现核）**：`BattleDriver.cs:25542` 把 `PreviewTargetsFromDrag`
        /// 挂进来（`W20` 落地）。原先本条写「调用点待接」—— **已过期**（铁律 5）。</para></summary>
        public System.Action<AttackKind> PreviewTargets;

        /// <summary>这一次 `Show()` 里有没有已经请求过「预亮目标」（原版是一次性的：起手那一下）。</summary>
        bool _previewSent;
        /// <summary>自检用：`RequestPreviewTargets` 真把回调打出去的次数。</summary>
        public static int PreviewRequests { get; private set; }

        /// <summary>🆕 **技能钮悬停音**（原版只有技能那颗有 `soundOnSelect`，
        /// `S:MonoBehaviour_4449.json` 的值 = GUID `f83d6aabae538004383a3072f12d6ff5`）。
        /// <para>🔴 **GUID 已解出（2026-10-24 现读，U3 §六·1 那条「没解出来」结案）**：
        /// `资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv:4536` 与
        /// `普查产出_1019/catalog_dump/*/catalog_locations.tsv:13905` 都指向
        /// `soundcollection_assets_all.bundle` 的 **`AudioCue`「ButtonClick」**；该 cue 的 `clipList`
        /// （`bundle_soundcollection_assets_all/MonoBehaviour/ButtonClick.json`）= PathID
        /// `487309658559691530` → 束里那个 AudioClip 的 `m_Name` = **`UI Select Toggle`**。
        /// 而 `Resources/animfx_sounds.json` **已经有** cue `ButtonClick` → clip `UI Select Toggle`
        /// （`minPitch/maxPitch = 1` · `timeToPlayAgain = 0.1`）⇒ **不用改任何数据**。</para>
        /// <para>播放时机 = 原版 `CardDisplayAttackTypeButton__DoPointerOverButtonActions.c`（发指令那一拍，
        /// 即 `+0x92 && +0x90 && !+0x91` 三格齐 ⇒ `callback` + `Play(+0x58)`）⇒ 我们的对应口 =
        /// <see cref="MarkHoverPicked"/>（调用方真把指令发出去之后调）。</para></summary>
        public const string SelectSoundCue = "ButtonClick";
        /// <summary>自检用：技能钮选中音真**走到了播放口**的次数
        /// （判据 = `WFSoundPlayer.Played` 的增量 —— 静音档下照样涨；⛔ 不是「真的出声了」）。</summary>
        public static int SelectSoundPlays { get; private set; }
        /// <summary>自检用：**没走到播放口**的次数 —— 两档：① cue 查不到（音效表缺件）
        /// ② 走到 `Play` 但 `Played` 没涨（不该发生，真发生了就是调度链断了）。</summary>
        public static int SelectSoundMisses { get; private set; }
        static bool _selectSoundWarned;

        /// <summary>🆕 **2026-10-25（`W2`）** 自检用：因「按军队查 `activeAbilityIcons` 取不到图标」而
        /// **把整颗技能钮关掉**的次数（只增）。判据 = 原版 `AttackTypesButtonsController__ToggleAttackTypesSelector.c:54-56`
        /// 的 `Toggle(btn, iconSprite != null & param_2)`。
        /// <para>⚠️ 每一次 `Show()` 都会重刷一遍（调用方每次新建 `Option`、`Hidden` 归零）⇒ 断言要比**增量**。</para></summary>
        public static int ArmyIconMissingHides { get; private set; }

        /// <summary>🆕 技能钮 tooltip —— 原版 `CardDisplayAttackTypeButton__HighlightIfNeeded.c:50-70`：
        /// **只有高亮的是 `attackType == 4`（主动技能）时才弹**，而且 `+0xb0`（当前那一条）非空就不再弹
        /// （= **一颗钮一辈子只弹一次**）。挂点 = 按钮自己的 `transform`（`SetParent(button)`），
        /// 位置 = 设置对象那个向量 × `tooltipVerticalOffset`（`+0x60`，技能那颗序列化值 = **−100**）。
        /// <para>⚠️ **−100 乘的那个基准向量本地取不到**（它在 `DAT_1842da2a8+0xb8` 那个设置对象里）
        /// 🔶 自己设计（非原版）：技能钮 tooltip 的落点距离 `TooltipDropPx = 70f` · 原版 −100 乘的那个基准向量在 `DAT_1842da2a8+0xb8` 设置对象里、本地取不到（原版判据拿不到） · TODO(谁定的)｜原注：⇒ 我们只能定「弹在钮**下方**」，落点距离是**我们挑的**（`TooltipDropPx`），如实标着。</para></summary>
        const float TooltipDropPx = 70f;
        bool _tooltipShown;
        /// <summary>自检用：这一次展开里技能钮 tooltip 弹过没有。</summary>
        public bool AbilityTooltipShown { get { return _tooltipShown; } }


        /// <summary>原版 `AttackTypes` 数值 → 我们这格的 `Kind`（**0 没打过 / 1 近战 / 2 远程 / 4 主动技能**）。</summary>
        static AttackKind KindOf(int attackType)
        {
            switch (attackType)
            {
                case 1: return AttackKind.Melee;
                case 2: return AttackKind.Ranged;
                case 4: return AttackKind.Ability;
                default: return AttackKind.None;
            }
        }

        public void Hide()
        {
            Visible = false;
            Hovered = AttackKind.None;
            _options.Clear();
            // 🆕 A462：悬停那两格的状态一起收干净（免得下一次弹出带着上一次的「已发」/安全窗）
            _hoverWas = AttackKind.None;
            _hoverSent = AttackKind.None;
            _safeLeft = 0f;
            // 🆕 2026-10-24（`W10`）：一次展开的那两件一次性状态 + 收掉技能钮 tooltip
            //   ⚠️ **只收自己弹的那条**（`Tooltip` 是全局单例，别人可能正拿它显示词条提示）。
            _justOne = AttackKind.None;
            _previewSent = false;
            // 🆕 2026-10-25：这一趟结束 ⇒ 「已选」落点标记清掉、三件缩放**直接**归位（⛔ 不补间：
            //   节点紧接着就 `SetActive(false)`）。下一趟 `Show` 会按当时的 `attackType` 重新补一次。
            _pickedApplied = AttackKind.None;
            ResetPickScales();
            if (_tooltipShown) Tooltip.Hide();
            _tooltipShown = false;

            if (_background != null) _background.gameObject.SetActive(false);
            for (int i = 0; i < Order.Length; i++) SetSlotActive(i, null);
        }

        /// <summary>
        /// 弹出并给选项。**按 `Kind` 对应按钮，不靠下标** ——
        /// 调用方给哪几项、给不给全，都不会串位。
        /// </summary>
        /// <param name="title">🔴 **2026-10-25（`W6`）起完全不用**（连数据都不存）—— 原版 `Drag Attack Selector`
        /// 的 subtree 里没有标题这一件（判据 → `BuildOnCanvas` 那段红字的① ②：解包原始资产子件逐颗核过 +
        /// 官方本地化表 `ChooseAction` 0 命中）。形参保留只是为了**不动调用点**
        /// （`BattleDriver.cs:13756` 的第一个字符串实参）。</param>
        /// <param name="note">🔴 **2026-10-25（`W4`）起不再画成可见标签** —— 原版 `Drag Attack Selector`
        /// 的 subtree 里没有那一行（判据 → `BuildOnCanvas` 那段红字）。这串现在只当**技能钮 tooltip 的正文兜底**
        /// （`_noteText` → `RefreshAbilityTooltip`）；形参保留是为了**不动调用点**
        /// （`BattleDriver.cs:13756` 那个 `CardText.Phrase(...)` 旁的第二实参）。</param>
        /// <param name="atWorld">🆕 2026-09-29：**被拖的那个单位在哪**（世界坐标 = 指针/布局空间）。
        /// 原版展开时把整条 `set_position(单位位置)`（`UnitOnBoardAttackTypeSelector__Toggle.c:112-126`）
        /// —— 传 null 就退回「屏幕中心」（那是我们原来的固定摆法）。</param>
        public void Show(List<Option> options, string title = null, string note = null,
                         Vector3? atWorld = null, int selectedType = 0, AttackKind justOne = AttackKind.None)
        {
            Build();
            _anchorAt = atWorld ?? Vector3.zero;
            // 🆕 2026-09-29：**已选打法那一格**（原版 `unit.attackType`；0 = 这个单位还没打过 ⇒ 不亮圈）
            _selected = KindOf(selectedType);
            _options.Clear();
            if (options != null) _options.AddRange(options);
            _justOne = justOne;
            // 🔴 **2026-10-24（`W10`）：三颗钮里近战/远程【恒显】** —— 原版
            //    `AttackTypesButtonsController__ToggleAttackTypesSelector.c:47-96` 对 `attackTypeButtons`
            //    那**恰好 3 颗**逐个 `SetActive(true)`，只有**技能那颗**可能被
            //    `CardDisplayAttackTypeButton__Toggle(btn, iconSprite != null && 有主动能力)` 关掉；
            //    近战/远程**没有任何使能判据**（哪怕攻击值 0 也在）。
            //    ⇒ 调用方没给的那两颗，这里补上（`Enabled = false` = 灰的、选不中）。
            //    ⚠️ 「可点性」原版静态读是**可点**（`AttackButtonClick` 没有攻击值判据），但那是
            //    `U3 §六·6` 明确标「未实跑」的一条 ⇒ 本件**不擅自把它变成可点**（见执行报告「没做到」）。
            //    `ToggleJustOne` 那一档**不补**（原版那时就是把另外两颗关掉）。
            if (justOne == AttackKind.None) EnsureAttackTypeInvariant();
            ApplyOptionIcons();
            // ⚠️ `title` 形参**收下但不用**（2026-10-25 `W6` 删了那块自造标题）；保留形参只为不动调用点。
            _noteText = note;
            Visible = true;
            Hovered = AttackKind.None;
            // 每次弹出都从「外扩位」飞回 —— 原版 `OnEnable` 把累计量清零（`__OnEnable.c` 从静态零向量
            // 写 `+0x50`/`+0x54`）；🆕 A462：顺带把**安全窗**也打满（原版 `Toggle(true)` →
            // `StartSafeTouch` ⇒ 弹出后头 0.1s 不吃悬停）`_hoverWas`/`_hoverSent` 一起归零。
            _enterT = 0f;
            _accumulatedDrag = Vector2.zero;
            _safeLeft = SafeTouchSeconds;
            _hoverWas = AttackKind.None;
            _hoverSent = AttackKind.None;
            // 🆕 2026-10-24：起手预亮 / tooltip 这两件都是「一次展开各一次」
            _previewSent = false;
            _tooltipShown = false;
            RefreshLayout();

            // 🔴 **原版 `BattleManager.StartAttackFrom` 的「拖拽起手就点亮合法目标 + 准星」**
            //    （`BattleManager__StartAttackFrom.c:20-40`）—— 整条链是
            //    `selector.Toggle(true, card)` → `ToggleCrosshair(true)` → `ShowPotentialTargets(card, type)`，
            //    三步在**同一个方法里**、**玩家还没选打法**。
            //    ⇒ 这里在展开完成的最后一刻请求一次（回调没挂 = 什么都不发生 ⇒ 与改前逐位相同）。
            //    `ToggleJustOne` 那条路**不预亮**（原版那条由 `ShowAttackTypeMark` 触发，不走 StartAttackFrom）。
            if (justOne == AttackKind.None) RequestPreviewTargets(_selected);
        }

        /// <summary>🆕 2026-10-24（`W10`）：**近战/远程恒显** —— 把调用方没给的那两颗补成「灰的」。
        /// 判据 → <see cref="Show"/> 里那段（原版 `ToggleAttackTypesSelector` 对 3 颗逐个 `SetActive(true)`）。</summary>
        void EnsureAttackTypeInvariant()
        {
            AddIfMissing(AttackKind.Melee);
            AddIfMissing(AttackKind.Ranged);
        }

        void AddIfMissing(AttackKind k)
        {
            if (Find(k) != null) return;
            _options.Add(new Option { Kind = k, Enabled = false });
        }

        /// <summary>🆕 2026-10-24（`W10`）：**只显示一颗**（原版 `UnitOnBoardAttackTypeSelector.ToggleJustOne`）。
        /// 判据 → 上面 `_justOne` 那条注释（含触发条件）。
        /// <para>✅ **调用点已接（2026-10-25 现核）**：`BattleDriver.cs:13654`（`displaySummonSickness` &&
        /// (`ferocity` || `oath`+`CanUseOathAbility`) 那一支）。原先本条写「调用点待接」—— **已过期**（铁律 5）。
        /// ⚠️ 那一处目前**没传 `army`**（用默认 0 ⇒ 技能钮仍是占位图）—— 见报告 AU3·未接项。</para>
        /// <para>🆕 **2026-10-25**：补 `army` 形参（默认 0 = 不给）—— 原来这里造的 `Option` **不带 `Army`**
        /// ⇒ 只显一颗时技能钮拿不到「按军队查的 `activeAbilityIcons`」那张图，落回占位图
        /// （`ApplyOptionIcons` 里「给了 `Army` 才走按军队那条」）。调用方照原版
        /// `UnitOnBoardAttackTypeSelector__Toggle.c` 把 `card.rawCard.army(+0x2c)` 传进来即可。</para></summary>
        public void ShowJustOne(AttackKind kind, Vector3? atWorld = null, int selectedType = 0, int army = 0)
        {
            var opts = new List<Option> { new Option { Kind = kind, Enabled = true, Army = army } };
            Show(opts, null, null, atWorld, selectedType, kind);
        }

        /// <summary>🆕 2026-10-24（`W10`）：**幂等**地请求一次「点亮合法目标 + 准星」。
        /// 原版只在拖拽起手那一拍做一次（`BattleManager__StartAttackFrom`），所以一次展开只发一次
        /// （`Show()` 会把 <see cref="_previewSent"/> 清零）。
        /// <returns>true = 这次真把回调打出去了；false = 已经在这次展开里发过 / 没挂回调 / 没显示。</returns></summary>
        public bool RequestPreviewTargets(AttackKind attackType = AttackKind.None)
        {
            if (!Visible || _previewSent) return false;
            _previewSent = true;
            var hook = PreviewTargets;
            if (hook == null) return false;
            PreviewRequests++;
            hook(attackType);
            return true;
        }

        /// <summary>🆕 2026-10-24（`W10`）：**立刻改「已选打法」那一格**。
        /// 原版 `AttackButtonClick` 在**选定时**就 `ChangeAttackType`（写 `card.attackType` + 高亮那一圈），
        /// 而我们驱动层只在**结算时**写 `u.LastAttackType` ⇒ 同一回合内改选后再开选择器，高亮的还是
        /// 「上次打出去的那一档」（`U3 §七·4` 那条时序差的修法口）。
        /// <para>⚠️ **2026-10-25 订正（铁律 5）**：原来这里写「全仓 `selector.SetSelected(` **0 命中**、
        /// 时序差仍然存在」—— **已过期**。实际 `BattleDriver.CommitCommand` 在**选定时**就写
        /// `LastAttackType` 并调 `SetSelected`（`BattleDriver.cs:13932`；
        /// `if (selector != null) { selector.SetSelected(kind); selector.Hide(); }`）
        /// ⇒ 「同一回合内改选后再开选择器，高亮的还是上次打出去的那一档」这条时序差**已经不存在**。
        /// 引擎侧「结算那一刻」那次写（`RuleCore.cs:5834/10157/10345/10556`）照旧，两处不冲突
        /// （前者是表现层的即时反馈，后者是规则层的落账）。</para></summary>
        public void SetSelected(AttackKind kind)
        {
            if (_selected == kind) return;
            _selected = kind;
            RefreshPicked();
        }


        /// <summary>
        /// 🆕 2026-09-25 把每一格的底图刷成**这一次该用的那张**。
        /// 为什么不建的时候一次定死：**同一个按钮格子在不同单位上要换图** ——
        /// 带 `Duty` 的兵那一格是修女/星军…那张专属图，普通的兵是占位图。
        /// ⚠️ 取不到图就**保持上一次的**（别 `SetTexture(null)` 把按钮刷没）。
        /// <para>🔴 **2026-10-25（`W2` · `AU1-B` 残差）**：技能钮的图标**原版只按军队查**
        /// （`UnitOnBoardAttackTypeSelector__Toggle.c:74-80` → `ArmyUtilities.GetActiveAbilityIconForArmy`
        /// → `IconsByArmySO.activeAbilityIcons`，**只有 5 条** ⇒ 真值表只在 `Core/ArmyIcons.cs` 一处）。
        /// 查不到（`iconSprite == null`）时原版是
        /// **`CardDisplayAttackTypeButton__Toggle(btn, bVar4 & param_2)` 把整颗钮关掉**
        /// （`AttackTypesButtonsController__ToggleAttackTypesSelector.c:54-56`），
        /// ⛔ **不是**「给一张占位图」。本方法照此：**`Army > 0`**（= 合法阵营值）时**只认军队那条**，
        /// 命中不了就 `Hidden = true` + 出声，并 `continue`（不再落回下面的 `IconName` 占位兜底）。
        /// ⚠️ **`Army <= 0` 一律走旧口径（`IconName` / 占位图），不关钮、不报警**，两档：
        ///   · `0` = 调用方没给（`BattleDriver` 在 `u.Card == null` 时给 0）；
        ///   · `-1` = `ArmyIcons.Id(faction)` **认不出阵营**（我们自研的 `Ember` / `Tide` 两阵营）
        ///     ⇒ 按 `BattleDriver.cs:13815-13817` 的既定口径落回占位图。
        /// 🔴 **2026-10-25（`W2` 修，真缺陷留痕）**：这里一度写成 `Army != 0` ⇒ `-1` 被送进「按军队查」⇒
        ///   `tex` 恒 null ⇒ **整颗技能钮被关掉** ⇒ 自研两阵营的主动技能**打不出来**（`BattleScene.Run` 连红 9 条）。
        ///   根因 = 把「认不出阵营」误当成「阵营有但没有图标」。⛔ 别再把闸放宽到 `!= 0`。
        /// ✅ **现核（2026-10-25，`W2`）**：`ArmyIcons.ActiveAbilityIconName` 返回的**已经是**我们
        /// `Resources/Art/ui/` 的文件名（它内部走 `ResourceNameOf` 把原版名里的空格换成下划线）
        /// ⇒ 直接喂 `CardArt.Ui` 取得到图，**不需要在调用点再做一次名字转换**。</para>
        /// </summary>
        void ApplyOptionIcons()
        {
            for (int i = 0; i < Order.Length; i++)
            {
                if (_icons[i] == null) continue;
                var o = Find(Order[i]);
                // 🆕 **2026-10-24（`W10`）：技能钮的图标原版是【按军队】查的**（见本方法 doc）。
                //   我们原来按**关键词**挑图（五阵营 1:1 时结果相同 ⇒ 看不出来）。
                // 🔴 **2026-10-25（`W2` 修・真缺陷）**：闸必须是 **`Army > 0`**，⛔ **不是 `Army != 0`** ——
                //   `ArmyIcons.Id(faction)` 认不出阵营时回 **-1**（我们自研的 `Ember` / `Tide` 两阵营），
                //   那一档**必须落回旧口径**（`IconName` / 占位图），见 `BattleDriver.cs:13815-13817` 的注释。
                //   写成 `!= 0` 会把 -1 也送进「按军队查」⇒ 查不到 ⇒ 整颗技能钮消失 ⇒ **主动技能打不出来**。
                bool abilityByArmy = o != null && o.Kind == AttackKind.Ability && o.Army > 0;
                string want;
                Texture2D tex;
                if (abilityByArmy)
                {
                    // 🔴 原版这一格**只认军队** ⇒ 即使调用方同时给了 `IconName`（关键词那条），也按军队查
                    //   （两条判据在本仓 5 个阵营上 1:1 吻合；不一致时以原版那条为准 —— 铁律 11）。
                    //   ⚠️ `ArmyIcons.ActiveAbilityIconName` **已经**做过「原版 sprite 名（带空格）→ 我们
                    //   `Resources/Art/ui/` 文件名（下划线）」那一跳（`ArmyIcons.ResourceNameOf`）⇒ 直接喂 `CardArt.Ui`。
                    //   走到这里 `Army > 0` 是**合法阵营值**，取不到只能是「那 5 条里没有它」或「资源名取不到」
                    //   ⇒ 才是原版要关钮的那一档（`iconSprite == null`）。
                    want = ArmyIcons.ActiveAbilityIconName(o.Army);
                    tex = string.IsNullOrEmpty(want) ? null : CardArt.Ui(want);
                    if (tex == null)
                    {
                        // 原版：`iconSprite == null` ⇒ 整颗钮关掉。我们同档 + **出声**（⛔ 不静默给占位图）。
                        // 只报一次：`Hidden` 已经是 true 就不再刷这条（`Show` 每次会重建 Option，故仍需 `!o.Hidden` 闸）。
                        if (!o.Hidden)
                        {
                            o.Hidden = true;
                            o.Enabled = false;
                            ArmyIconMissingHides++;
                            Debug.LogWarning("[Battle] 攻击方式选择器：这张卡的阵营（`" + o.Army + "`）在"
                                           + " `activeAbilityIcons` 那 5 条里查不到图标 ⇒ **整颗技能钮关掉**"
                                           + "（原版 `Toggle(btn, iconSprite != null & …)`；⛔ 不拿占位图顶）");
                        }
                        continue;      // ⛔ 不落回 `IconName` 的占位图
                    }
                }
                else
                {
                    want = (o != null && !string.IsNullOrEmpty(o.IconName)) ? o.IconName : null;
                    if (string.IsNullOrEmpty(want)) want = IconName(Order[i]);
                    tex = CardArt.Ui(want);
                }
                if (tex != null && !ReferenceEquals(_icons[i].Texture, tex))
                {
                    _icons[i].SetTexture(tex);
                    // 🔴 **2026-10-24（`A28`）就地订正（铁律 5）**：原来这里只在 uGUI 档「按新贴图比例
                    //   重算宽度」—— **方向错了**：原版那颗 `Image` 是 `m_PreserveAspect = 0`、
                    //   节点 rect 是固定 118.372 × 118.806（判据见 `ButtonW` 的 doc）⇒ **换图不动矩形**。
                    //   ⇒ 两档走同一个口 `SetAspect`（uGUI = 只改 `sizeDelta.x`；世界档 = 改 quad 的 aspect），
                    //   把矩形重新钉回原版那一对值（世界档的 `SetTexture` 会把 aspect 重算回贴图比例）。
                    _icons[i].SetAspect(ButtonW / ButtonH);
                }
            }
        }

        /// <summary>
        /// **替代行动 / 誓约 → 它那颗专属按钮图的文件名**（`Resources/Art/ui/` 下；没有 = null）。
        ///
        /// 判据：**名字一一对应，不是猜的** —— 原版那五张就叫
        /// `Attack type button {Pray, Duty, Ferocity, Agenda, Oath}`，我们导进来时只换成下划线命名
        /// （和已有的 `Attack_type_button_Melee/Ranged/highlight` 保持同一个写法）。
        /// 阵营 ↔ 关键词的映射见 `资料/特殊行动_五件_原版规格.md`。
        /// </summary>
        public static string AltIconFor(string altKeyword, bool oath)
        {
            if (oath) return "Attack_type_button_Oath";
            if (altKeyword == RuleEngine.KeywordTable.Duty)     return "Attack_type_button_Duty";
            if (altKeyword == RuleEngine.KeywordTable.Pray)     return "Attack_type_button_Pray";
            if (altKeyword == RuleEngine.KeywordTable.Ferocity) return "Attack_type_button_Ferocity";
            if (altKeyword == RuleEngine.KeywordTable.Agenda)   return "Attack_type_button_Agenda";
            return null;
        }

        /// <summary>
        /// 按当前分辨率把按钮摆一遍。
        /// ⚠️ 位置只在建的时候算一次的话，切到 4:3 就全错位 —— HUD 那几个 Label 踩过一模一样的坑
        /// （见 `BattleDriver.ReanchorHud`）。所以摆位独立成这一个函数。
        /// </summary>
        public void RefreshLayout()
        {
            if (!Visible) return;

            // 条子**挂在被拖的那个单位身上**，不是屏幕中心。
            // 🔴 **2026-09-29 照原版改**：`UnitOnBoardAttackTypeSelector__Toggle.c:112-126` 展开时做的是
            //    `set_position(Get2DWorldPosFromBoardPos(被拖单位.position))` —— 被 set_position 的是
            //    **整条 `Drag Attack Selector` 自己的 Transform**（组件就挂在 GO 158 上）。
            //    🔴 **2026-10-24（`B-21`）就地订正（铁律 5）**：本段原来写「那条的 `pivot = (0.5, 1)`
            //    （实读 `RectTransform_3061.json`）」—— **引错了节点**：`RT_3061` 是它的子件
            //    `Select Attack Background`；**整条自己的** `RT_2973` 是 `pivot (0.5, 0.5)`
            //    （中心）⇒ `set_position` 落在锚点上的是**条子的中心**（= 三颗钮那一行），不是底板顶边。
            //    ⚠️ 2026-10-25 再订正：那句里原来还并列了 `RT_3166`（`722.66×403.13`、锚点 (0,0)）——
            //      那也是**另一颗**节点，不是本条子的 rect；本条子的 rect 只有 `RT_2973`（`621.72×122.25`）。
            //
            // 🔴 **2026-10-16（`T09` 批 3）：原点在这一步按档取** ——
            //    · uGUI 档：帧节点摆到锚点上（画布单位），件全挂在帧下 ⇒ 下面的数**全部相对帧中心**；
            //    · 世界档：件直接挂在选择器根下、根在世界原点 ⇒ 原点就是 `_anchorAt` 这个局部位置。
            //    两者下面**共用同一串式子**（靠 `U`/`US`/`Z` 三个换算口分叉）—— 这样两档的几何是**同一份判据**，
            //    不会是「两处各写一遍」（本工程血规）。
            Vector3 origin;
            if (_frame != null)
            {
                var l = AnchorLocal();
                _frame.localPosition = new Vector3(l.x, l.y, 0f);   // ⛔ z 恒 0（画布上分层靠兄弟序）
                origin = Vector3.zero;
            }
            else origin = _anchorAt;

            // 🔴 **2026-10-24（`B-21`）**：三颗钮那一行的中心 = **锚点本身**（原版那行由
            //    `HorizontalLayoutGroup(MiddleCenter)` 居中在 `Drag Attack Selector`(RT_3166) 的 rect 里，
            //    而整条被 `set_position(单位位置)`、pivot = 中心 ⇒ 行心 = 锚点）。
            //    ⛔ 原来这里减了 `ButtonRowDownPx`(=42.7485)，整条比原版低 42.7485 px —— 见常量上的长注释。
            var center = origin;

            // 这一次真要显示的那几颗 —— **背景宽**与**按钮排布**共用这一份（判据只此一处）
            var shown = ShownKinds();

            if (_background != null)
            {
                _background.gameObject.SetActive(true);
                // 底板**顶边**在锚点**上方** `BarH/2 − 18.375` = 42.7485 px（原版 `RT_3254`：
                // `anchor (0.5,1)` + `ap (0,−18.375)` + `pivot (0.5,1)`）。
                // ⚠️ 两档的**枢轴不同**：世界档 `ImageQuad` 的 pivot 是 `(0.5,1)`（局部位置 = 顶边），
                //    uGUI 档 `HudCanvas.MakeImageInParent` 的 pivot 恒 `(0.5,0.5)`（局部位置 = 中心）
                //    ⇒ 后者要再减半个板高。（这一处也是**原先两档差 62.7 px** 的病根 —— 一起修掉。）
                float bgTop = U(BgTopAboveAnchorPx);
                float bgY = _background.IsUgui ? bgTop - U(BgH * 0.5f) : bgTop;
                _background.transform.localPosition = center + new Vector3(0f, bgY, Z(ZBg));

                // 🔴 **2026-10-25（判据见 `BgBaseW` 的长注释）**：实绘宽 = 343.2 × lerp(目标倍率×1.5, 1, t)
                //   —— `count` 与入场进度**两个量都在里面**，⛔ 别写成一个常量。
                //   · `countT` = `clamp01(颗数 − 2)`（`DAT_1834b2bbc` = 2.0）；
                //   · `targetMul` = `1 + (1.5 − 1) × countT`（`elementsHorizontalScaleModifier`）；
                //   · 起点那一侧再乘 `inputScaleModifier`（`MoveButtons` 就是这么乘的）。
                //   `SetAspect` 是「宽 = 高 × aspect」的唯一口（两档同一行）——
                //   ⛔ 别在 uGUI 档写 `sizeDelta.x`、世界档写 `localScale.x`（那是同一条规矩的两份写法）。
                float countT = Mathf.Clamp01(shown.Count - 2f);
                float targetMul = 1f + (ElementsHorizontalScaleModifier - 1f) * countT;
                float bgMul = Mathf.Lerp(targetMul * InputScaleModifier, 1f, Mathf.Clamp01(_enterT));
                _background.SetAspect((BgBaseW * bgMul) / BgH);
            }
            // 🔴 **2026-10-25（`W6`）删**：这里原来是自造标题（`SelectorTitle`）的显隐 + 摆位
            //   （`center + (0, BarH/2 + 26px)`）。原版 subtree 里没有这块标题，判据 → `BuildOnCanvas` 那条红字。
            // ⚠️ 这里原来是第二行「技能效果」小字（`SelectorNote`）的摆位 —— **2026-10-25（`W4`）删**：
            //   原版 subtree 里没有这一件（判据 → `BuildOnCanvas` 那段红字）。`_noteText` 只当 tooltip 正文用。

            // 只排**这次真的要显示**的那几项，居中 —— 原版是 HorizontalLayoutGroup(MiddleCenter)，
            // 所以只有两个按钮时它们会往中间收，不会留着技能那个空档
            // ⚠️ 2026-10-25：这份清单已提到上面（`var shown = ShownKinds();`）—— 背景宽也要按它算，
            //    这里 ⛔ 别再算第二份（两处写同一条规则 = 迟早不一致）。

            for (int i = 0; i < Order.Length; i++) SetSlotActive(i, null);

            for (int n = 0; n < shown.Count; n++)
            {
                int i = SlotOf(shown[n]);
                var opt = Find(shown[n]);
                // ⚠️ **间距那一项过 `US`**（不是 `U`）：世界档原来乘了 `LayoutSpace.Scale`（窄屏整体缩），
                //    uGUI 档由 `CanvasScaler` 负责 ⇒ 不乘。
                //    🔴 **2026-10-24（`A28`）**：步距用**原版按钮宽 118.372**（不是旧常量 118.8）——
                //    原版那一行是 `HorizontalLayoutGroup(m_Spacing 37)` 排的 ⇒ 步距 = **子件宽 + 37**。
                float x = (n - (shown.Count - 1) * 0.5f) * US(ButtonW + ButtonSpacing);
                var p = center + new Vector3(x, 0f, 0f);

                SetSlotActive(i, opt);
                // 🆕 2026-09-29：**入场偏移**（原版 `MoveButton` 的 `lerp(外扩位, 归位位, t)`）——
                // 进度 1 时 `EnterOffset` 恒为 0，所以动画跑完这条等于没加。
                var off = EnterOffset(shown[n]);
                if (_icons[i] != null) _icons[i].transform.localPosition = p + off + new Vector3(0f, 0f, Z(ZIcon));
                if (_rings[i] != null) _rings[i].transform.localPosition = p + off + new Vector3(0f, 0f, Z(ZRing));
                if (_captions[i] != null)
                {
                    _captions[i].SetText(opt.Badge);
                    _captions[i].transform.localPosition =
                        p + off + new Vector3(U(BadgeDx), U(BadgeDy), Z(ZText));
                }

                // 不能选的置灰 —— 「这个单位现在打不了这一种」要一眼看出来
                float dim = opt.Enabled ? 1f : 0.42f;
                if (_icons[i] != null)
                    _icons[i].SetTint(new Color(dim, dim, dim, opt.Enabled ? 1f : 0.8f));
            }

            RefreshPicked();
        }

        /// <summary>锚点（**世界坐标**）→ **帧的局部坐标**（画布单位）。
        /// <para>🔴 **⛔ 别用 `LayoutSpace.ToDesignPixel` 的 y** —— 它那一半写死 108，只在 16:9 对；
        /// 画布单位↔世界单位的**两个轴系数不同且随屏变**（批处理下 x 1/108 · y 1/144，见 `T09` §七·8）。
        /// 只有**画布自己的变换**是精确的 ⇒ 走 `InverseTransformPoint`（它把父链的缩放/宽高比一起吃掉）。</para>
        /// <para>⚠️ 本件只改**轴对齐的平移**（无旋转/切变）⇒ 逆变换出来的点可以直接当局部坐标用。</para></summary>
        Vector3 AnchorLocal()
        {
            if (_frame == null || _frame.parent == null) return Vector3.zero;
            var p = _frame.parent.InverseTransformPoint(_anchorAt);
            return new Vector3(p.x, p.y, 0f);
        }

        /// <summary>这一次真要显示的那几颗（`Hidden` / `_justOne` 都在这里收）。**判据只此一处** ——
        /// 背景宽（按颗数）与按钮排布（按颗数居中）共用它。</summary>
        List<AttackKind> ShownKinds()
        {
            var shown = new List<AttackKind>();
            for (int i = 0; i < Order.Length; i++)
            {
                var o = Find(Order[i]);
                if (o == null || o.Hidden) continue;
                if (_justOne != AttackKind.None && o.Kind != _justOne) continue;
                shown.Add(o.Kind);
            }
            return shown;
        }

        /// <summary>🔴 **2026-10-25**：这一格是不是「**已选打法**」那一格 —— 原版那颗 `Highlight` 圈的显隐
        /// **只由它决定**：`CardDisplayAttackTypeButton__HighlightIfNeeded.c` 第一句
        /// `SetActive(highlightImage(+0x38), param_2 == attackType(+0x20))`，`…HighlightSelectedAttackTypeButtons.c`
        /// 同一句 ⇒ **没选中的格子那圈是关着的**，⛔ 不是「全都亮、只是压暗」。
        /// <para>判据只此一处：`SetSlotActive`（切显隐）与 `RefreshPicked`（切缩放/补间）都走它。</para></summary>
        bool IsPickedSlot(Option opt)
        {
            return opt != null && opt.Enabled && !opt.Hidden
                && _selected != AttackKind.None && opt.Kind == _selected;
        }

        /// <summary>`opt` 传 null = 关掉这一格（三个按钮节点是**一次建满**的，之后只切显隐）</summary>
        void SetSlotActive(int i, Option opt)
        {
            bool on = opt != null;
            if (_icons[i] != null) _icons[i].gameObject.SetActive(on);
            // 圈只在「已选打法」那一格亮（判据 → `IsPickedSlot`）；关着的圈同时收不到 uGUI 射线
            if (_rings[i] != null) _rings[i].gameObject.SetActive(on && IsPickedSlot(opt));
            if (_captions[i] != null)
                _captions[i].gameObject.SetActive(on && !string.IsNullOrEmpty(opt.Badge));
        }

        // ==================================================================
        //  🆕 2026-10-20（`T09` A3-e）：uGUI 悬停（原版 `CardDisplayAttackTypeButton` 那两个接口）
        // ==================================================================

        /// <summary>给一件 uGUI 件挂 `PointerEnter / PointerExit`（形状同 `BattleLogPanel.AddPointerDown`；
        /// 原版这两处就是「`Image` 射线靶 + 指针接口」）。**同批置 `raycastTarget = true`** ——
        /// 原版 `Highlight` 那颗 `Image` 就是 `m_RaycastTarget = 1`。
        /// <returns>`null` = 没挂上（件 null / 世界空间件 / 拿不到 GameObject）—— 调用点已出声。</returns></summary>
        EventTrigger AddHoverTrigger(HudPiece piece, AttackKind kind)
        {
            if (piece == null) return null;
            if (!piece.SetRaycastTarget(true)) return null;      // 世界空间件会在那边 `LogError`
            var go = piece.gameObject;
            if (go == null) return null;
            var et = go.GetComponent<EventTrigger>();
            if (et == null) et = go.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => OnHoverEnter(kind));
            et.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => OnHoverExit(kind));
            et.triggers.Add(exit);
            return et;
        }

        /// <summary>指针进入某一格 —— 原版 `CardDisplayAttackTypeButton.OnPointerEnter`（置 `inputOverButton`）。
        /// ⚠️ 只写 `Hovered`；「换格 ⇒ 重开安全窗 / 清 `_hoverSent`」仍由 <see cref="TickHover"/> 按
        /// `Hovered` 的变化统一做（⛔ 不在这里另写一份 —— 判据只此一处）。</summary>
        void OnHoverEnter(AttackKind kind)
        {
            int i = SlotOf(kind);
            if (i >= 0) _hoverEnterCount[i]++;
            if (!Visible) return;                  // 关着时圈已 `SetActive(false)`（收不到）；这里是双保险
            // 🔴 **可选中性判据只此一处**（与原 `UpdatePointer` 的 `if (opt == null || !opt.Enabled) continue;`
            //    同义）：灰掉/没这一项的格子一律不吃悬停 —— 圈虽然会 `SetActive(false)`，
            //    但**退回图标当受体**那一档（圈贴图缺）图标仍在 ⇒ 不写这句就会「灰格也能选中」。
            var opt = Find(kind);
            if (!Selectable(opt)) return;
            if (Hovered == kind) return;
            Hovered = kind;
            RefreshPicked();
        }

        /// <summary>🆕 2026-10-24（`W10`）· **可选中性判据只此一处**（`OnHoverEnter` 与 `UpdatePointer` 共用一个口）：
        /// ① 有这一项；② `Enabled`（调用方给的「现在能不能用」）；③ 没被 `Hidden`（原版按军队查不到图标）；
        /// ④ **近战那格在 `LockMelee`（= 卡上有 `pindown`）时不选** —— 原版
        /// `AttackButtonClick.c:27-33` 是「指令被吞掉」，效果上等于选不中（⚠️ 原版那颗钮**不置灰**，
        /// 所以这里也没动 `Enabled`）。</summary>
        bool Selectable(Option opt)
        {
            if (opt == null || !opt.Enabled || opt.Hidden) return false;
            if (opt.Kind == AttackKind.Melee && LockMelee) return false;
            return true;
        }

        /// <summary>指针离开某一格 —— 原版 `OnPointerExit`（清 `isInputOverSent`；给兄弟钮 `DisableTemporary`）。
        /// ⚠️ **只在 `Hovered` 还停在这一格时才清**：相邻两圈的矩形会重叠，A→B 的切换里
        /// 若「先 Enter B、后 Exit A」，无脑清会把刚进入的 B 抹掉。</summary>
        void OnHoverExit(AttackKind kind)
        {
            int i = SlotOf(kind);
            if (i >= 0) _hoverExitCount[i]++;
            if (Hovered != kind) return;
            Hovered = AttackKind.None;
            RefreshPicked();
        }

        // ==================================================================
        //  指针
        // ==================================================================

        /// <summary>喂指针世界坐标（每帧）。返回压着的那个按钮。
        /// <para>🔴 **2026-10-20（`T09` A3-e）**：uGUI 档的 `Hovered` 由
        /// `EventSystem` 的 `PointerEnter/Exit` 驱动（原版就是那两个接口，见文件头）——
        /// 本方法在那一档**只推进 `Tick()` 并回读 `Hovered`**，⛔ 不再做手搓圆判定
        /// （两条路抢同一个 `Hovered` = 半迁移；面板挡住射线时两边还会给出不同答案）。
        /// **世界档（`_frame == null`）逐位不变。**</para></summary>
        public AttackKind UpdatePointer(Vector3 world)
        {
            if (!Visible) return AttackKind.None;

            // 🆕 2026-09-29：**先推进入场动画，再判命中** —— 反过来的话这一帧用的是上一帧的位置
            // （原版也是这个顺序：`Update` 里先 `MoveButtons`，输入事件是另外进的）。
            Tick(Time.deltaTime);

            // 🆕 2026-10-20（`T09` A3-e）：uGUI 档 —— 命中已由 `Highlight` 上的 `EventTrigger` 送达，
            //   这里**只回读**（`Tick` 仍要跑：入场动画与 0.1s 安全窗都靠它）。
            if (_frame != null) return Hovered;

            // 🔴 **2026-10-16（`T09` 批 3）：世界档的命中按【世界点】算** ——
            //   `transform.position` 与原来的 `localPosition` **逐位相同**（选择器根在世界原点，
            //   `Editor/BattleScene.cs` 的 A964 那条前提钉着）⇒ 这一支的行为一个字没变。
            //   半径 = `W(ButtonH)/2 × Scale`（原样）；⛔ 拿 px 去比世界点 = 命中框**大 108 倍**、画面却完全正常。
            // 🔴 **2026-10-20（`T09` A3-e）**：uGUI 档那条「用 `_frame.lossyScale` 把 px 折成世界单位的圆判据」
            //   **已整段退场**（命中改由 `EventSystem` 送达，见上面的早退）—— ⛔ 别把它加回来（= 两条路抢 `Hovered`）。
            // 🔴 **2026-10-25 改判（判据充分 —— 顺手闭掉 `T16` §1·2 #17「模型③判据不足」那条）**：
            //   原版**没有「圆」这个模型**：受体 = **两个矩形的并集** ——
            //   钮本体 `Image`（`RT_3368/2804/2626` = **118.372 × 118.806**，`m_RaycastTarget = 1`）
            //   ∪ 子件 `Highlight`（`RT_3028/2776/3512` = **176.23 × 176.875**，**只在「已选打法」那一格
            //   `SetActive(true)`**，见 `IsPickedSlot`）。
            //   ⇒ 世界档（= uGUI 宿主没建起来时的兜底）照同一条口径判**矩形包含**，圈只在 hot 那格参与。
            //   ⛔ 别再回到「以图标中心为圆心、直径 118.8」：那个圆比原版小一圈（方角上点不到），
            //     也判不出「已选那格因为圈变大 ⇒ 命中区跟着变大」这一档。
            // 🔴 **2026-10-25（`X8` · ③）：这段循环已提成 `KindAtPoint`** —— 与「槽外取消」
            //   （`ContainsBar`）**共用同一个判定口**（⛔ 别在这儿再留一份矩形）。
            var hit = KindAtPoint(world);

            if (hit != Hovered) { Hovered = hit; RefreshPicked(); }
            return Hovered;
        }

        /// <summary>指针落在**哪一颗钮的受体矩形**上（`None` = 不在任何一格）—— 悬停与「槽外取消」的**唯一判定口**。
        /// <para>🔴 **2026-10-25（`X8` · ③）为什么要提出来**：原来 `ContainsBar` 另写了一个
        /// `BarW × (BarH + BgH/2)` 的手搓大矩形（还带一段 `−hh*2` 的非对称下沿），与悬停的
        /// 「钮矩形 ∪ 已选那格的圈」**不是同一个区域** ⇒ 底板边缘那一点会出现
        /// 「悬停档说不在、取消档说在」（玩家看到的是：点一下底板边缘，指挥被取消了）。
        /// ⛔ 别再给 `ContainsBar` 单写一份几何（两处写同一条规则 = 迟早不一致）。</para>
        /// <para>⚠️ **两档的单位不同、在这里一次换好**：
        /// · uGUI 档 ⇒ 指针先折进**帧局部 px**（`_frame.InverseTransformPoint`，与件自己的
        ///   `localPosition` 同一空间；⛔ 别再乘/除画布缩放 —— `InverseTransformPoint` 已经算进去了）；
        /// · 世界档 ⇒ 直接用世界点，`HitRect` 按 `W(px) × LayoutSpace.Scale` 折。</para>
        /// <para>⚠️ **不可选的格子不算受体**（`Selectable`）—— 与 `OnHoverEnter` 同一个口；
        /// 代价是「`pindown` 那颗钮 / 教程关掉的那颗」点上去会走「槽外取消」那一支（见 §五 记录）。</para></summary>
        public AttackKind KindAtPoint(Vector3 world)
        {
            if (!Visible) return AttackKind.None;
            Vector3 p = _frame != null ? _frame.InverseTransformPoint(world) : world;
            for (int i = 0; i < Order.Length; i++)
            {
                var opt = Find(Order[i]);
                if (!Selectable(opt)) continue;      // 与 `OnHoverEnter` 同一个口（含 `pindown` 那条）
                if (_icons[i] == null || !_icons[i].gameObject.activeSelf) continue;

                if (HitPiece(_icons[i].transform, ButtonW, ButtonH, p)) return opt.Kind;
                if (IsPickedSlot(opt) && _rings[i] != null && _rings[i].gameObject.activeSelf
                    && HitPiece(_rings[i].transform, RingW, RingH, p)) return opt.Kind;
            }
            return AttackKind.None;
        }

        /// <summary>`KindAtPoint` 的单件判定（**两档共用**）。`p` = uGUI 档的**帧局部 px** / 世界档的**世界点**。</summary>
        bool HitPiece(Transform t, float wPx, float hPx, Vector3 p)
        {
            if (_frame != null)
            {
                // 件都是 `_frame` 的直接子件（`BuildOnCanvas`）⇒ `localPosition` 就是帧局部 px。
                Vector3 dl = p - t.localPosition;
                return Mathf.Abs(dl.x) <= wPx * 0.5f && Mathf.Abs(dl.y) <= hPx * 0.5f;
            }
            var d = t.position - p;
            d.z = 0f;
            return HitRect(d, wPx, hPx);
        }


        /// <summary>把「已选打法」那一格画出来：**黄圈 + 放大 1.3 倍 + 0.3 s 补间**（原版 `scaleMultiplierWhenSelected`）。
        /// 🔴 **2026-09-29 照原版改**：原来这个高亮挂在**指针悬停**上（`Hovered`），而原版挂的是
        /// `unit.attackType`（= 这个单位上一次用的打法，`HighlightSelectedAttackTypeButtons`）。
        /// ⚠️ **悬停现在不再单独给视觉**（原版那一圈只有"已选"这一档）—— 实机觉得别扭再单记，别自己发明。
        /// 🔴 **2026-10-25 按判据补三处**（原来都与原版不符，逐条见下）：
        ///   ① 圈**只在已选那一格亮**（原来三格都亮、非选的压到 α0.28；原版第一句就是 `SetActive(false)`）；
        ///   ② 缩放是 **0.3 s 补间**（选中 `Ease.OutBack` / 回落 `OutQuad`），原来是**瞬间赋值**；
        ///   ③ `ValueText`（我们的 `Badge_*`）是**按钮的子件** ⇒ 原版一起 ×1.3，原来我们漏了它。</summary>
        void RefreshPicked()
        {
            bool changed = _pickedApplied != _selected;
            for (int i = 0; i < Order.Length; i++)
            {
                var opt = Find(Order[i]);
                if (opt == null) continue;
                bool hot = IsPickedSlot(opt);

                if (_rings[i] != null)
                {
                    // 圈显隐与 `SetSlotActive` 同一个判据（`IsPickedSlot`）—— 这里再做一遍是因为
                    //   「只改 `_selected`、不重排」的调用点（`SetSelected`）也要把圈换过去。
                    bool shown = _icons[i] != null && _icons[i].gameObject.activeSelf;
                    _rings[i].gameObject.SetActive(shown && hot);
                    if (shown && hot) _rings[i].SetTint(Color.white);   // 原版那颗 `Image` 不改色（贴图自带黄圈）
                }

                // 只在「已选打法」真的换了的那一拍起补间 —— 否则每次 `RefreshLayout`（每帧都可能来）
                //   都会把补间重置回起点，看起来就是「永远弹不到位」。
                if (!changed) continue;
                ApplyPickScale(_icons[i], hot);
                ApplyPickScale(_rings[i], hot);
                ApplyPickScale(_captions[i], hot);     // `ValueText` 是钮的子件 ⇒ 一起缩放（原版整体 DOScale）
            }
            if (changed) _pickedApplied = _selected;
            RefreshAbilityTooltip();
        }

        /// <summary>原版那一拍 `DOScale(原始 scale × (hot ? 1.3 : 1), 0.3 s)`（判据 → <see cref="PickTweenSeconds"/>）。
        /// ⚠️ 走 `CardTween.Mode`（批处理 = `Manual`，由 `BattleScene.Step` 推）
        /// —— ⛔ 直写 `localScale` 等于把原版那条补间吞掉，而且会被下一次 `RefreshLayout` 覆盖回来。
        /// <para>⚠️ 原版放大的是**按钮整条 transform**（钮 + 子件 `Highlight` + `ValueText`），
        /// 我们这三件是平铺的兄弟件（见报告 §⑤ 的历史偏离）⇒ 逐个调同一份算式。</para></summary>
        void ApplyPickScale(Transform t, bool hot)
        {
            if (t == null) return;
            float s = hot ? HighlightScale : 1f;
            var tw = t.DOScale(new Vector3(s, s, 1f), PickTweenSeconds)
                      .SetEase(hot ? Ease.OutBack : DOTween.defaultEaseType)
                      .SetUpdate(CardTween.Mode);
            if (CardTween.LinkEnabled) tw.SetLink(t.gameObject);   // 拆旧建新 / 换场景时别留野补间
        }
        void ApplyPickScale(HudPiece piece, bool hot) { if (piece != null) ApplyPickScale(piece.transform, hot); }
        void ApplyPickScale(Label cap, bool hot) { if (cap != null) ApplyPickScale(cap.transform, hot); }

        /// <summary>收起时把三件的缩放**直接**拉回 1（⛔ 不补间 —— 节点马上就不可见了）。
        /// <para>为什么要有：`Hide()` 之后节点是 `SetActive(false)`，留着上一趟的 1.3 会在下一趟
        /// 「没选中那一格」上闪一下；原版 `OnDisable` 只归位 `localPosition`（它的按钮出生态就是 1）。</para></summary>
        void ResetPickScales()
        {
            for (int i = 0; i < Order.Length; i++)
            {
                ResetPickScale(_icons[i] != null ? _icons[i].transform : null);
                ResetPickScale(_rings[i] != null ? _rings[i].transform : null);
                ResetPickScale(_captions[i] != null ? _captions[i].transform : null);
            }
        }
        static void ResetPickScale(Transform t)
        {
            if (t == null) return;
            t.DOKill();
            t.localScale = Vector3.one;
        }

        /// <summary>🆕 2026-10-24（`W10`）：技能钮 tooltip —— 原版 `CardDisplayAttackTypeButton__HighlightIfNeeded.c`。
        /// 判据逐句：① **高亮的必须是 `attackType == 4`**（`if (param_2 != 4) return;`）② `+0xb0`（当前那条）
        /// **非空就不再弹**（一颗钮一辈子一次）③ `SetParent(按钮)` ⇒ 挂在**按钮**上
        /// ④ `localPosition = 设置向量 × tooltipVerticalOffset`（技能那颗 = **−100**）。
        /// <para>⚠️ ④ 里那个「设置向量」本地取不到（在 `DAT_1842da2a8+0xb8` 那个 SO 里，无实例）
        /// ⇒ 我们只能定「弹在钮**下方**」+ 一个**我们挑的**距离 <see cref="TooltipDropPx"/>（铁律 3 如实标注）。</para>
        /// <para>文案优先级：`Option.TooltipTitle/Body` → 选择器那条 `note`（原版正文就是卡上那条技能描述）
        /// → `TipText.Trait(AltKeyword)`。三处都空 ⇒ 不弹（并**不静默**：见下面那句 warning）。</para></summary>
        void RefreshAbilityTooltip()
        {
            if (!Visible) return;
            var opt = Find(AttackKind.Ability);
            bool hot = opt != null && !opt.Hidden && _selected == AttackKind.Ability;
            if (!hot)
            {
                if (_tooltipShown) { Tooltip.Hide(); _tooltipShown = false; }
                return;
            }
            if (_tooltipShown) return;                       // 原版：`+0xb0 != null` ⇒ 不再弹
            var at = ButtonWorld(AttackKind.Ability);
            if (at == null) return;

            string body = opt != null && !string.IsNullOrEmpty(opt.TooltipBody) ? opt.TooltipBody : _noteText;
            string title = opt != null ? opt.TooltipTitle : null;
            if (string.IsNullOrEmpty(body) && opt != null && !string.IsNullOrEmpty(opt.AltKeyword))
                body = TipText.Trait(opt.AltKeyword);
            if (string.IsNullOrEmpty(body))
            {
                // 原版这一格一定有正文（`+0xa8` 是卡上那条技能描述）⇒ 我们取不到就是**少一件**，出声。
                Debug.LogWarning("[Battle] 技能钮 tooltip：没有正文（`Option.TooltipBody` / `note` / `AltKeyword` 都空）"
                               + " ⇒ 这一下不弹（原版 `HighlightIfNeeded` 是有的）。");
                return;
            }
            string text = string.IsNullOrEmpty(title) ? body : title + "\n" + body;
            // `anchor: 27` = **下中**（`Tooltip.Show` 那张锚点表）；再往下压 `TooltipDropPx`。
            Tooltip.Show(text, at.Value, 27, new Vector3(0f, -U(TooltipDropPx), 0f));
            _tooltipShown = true;
        }

        /// <summary>指针是不是在整条槽的范围里（点在槽外 = 取消）。
        /// ⚠️ **两档都不用改**：这里比的全是**世界量**（喂进来的点在世界上、`hw/hh` 由 px 常量折成世界单位）
        /// ⇒ 与「渲染层在哪一档」无关。
        /// 🔴 **2026-10-23（`W64` `B-22` 收口 · 铁律 5 就地订正）**：本段原来写着「这个盒子以**世界原点（屏心）**
        /// 为中心…留给主对话裁」—— **已修**。判据：整条 2026-09-29 起就挂在**被拖那个单位**的位置上
        /// （`_anchorAt` = `Show(..., atWorld)` 传进来的世界锚，由 `BattleDriver` 喂
        /// `playerBoard.DropTargetWorld(slot)`）⇒ 「点槽外取消」的判定域**必须跟着条子走**，
        /// 否则单位不在屏心时判定域与画面脱节（画在左、点右边才取消）。
        /// ⇒ 现在把喂进来的点**先减掉 `_anchorAt`** 再比（盒子的形状/尺寸一个字没变）。</summary>
        /// <summary>这个点算不算「在选择器里面」（**只给「点槽外取消」那一条用**）。
        /// <para>🔴 **2026-10-25（`X8` · ③）：改成转发 `KindAtPoint`** —— 原来这里是一份**手搓的大矩形**
        /// `BarW × (BarH + BgH/2)`，还带一段非对称的下沿（`rel.y >= −hh*2`）；它和悬停实际用的
        /// 「钮矩形 ∪ 已选那格的圈」不是同一个区域 ⇒ **底板边缘那一点：悬停档说「不在」、取消档说「在」**
        /// （玩家看到的是「点一下底板边，指挥被取消」，而原版那块 `Select Attack Background` 是
        /// `m_RaycastTarget = 1` 的纯色射线靶、**点了什么也不发生**）。</para>
        /// <para>⚠️ **行为变化（如实记）**：现在「点在选择器**底板**上但不在任何一颗钮上」会走取消
        /// （旧实现不会）。这是「与悬停同源」的必然结果 —— 原版那一带本来就没有动作，
        /// 我们这条取消本来就是**自造的**（`A462` §四·3）；判据只有一条：不要让两条路给出不同答案。</para></summary>
        public bool ContainsBar(Vector3 world)
        {
            if (!Visible) return false;
            return KindAtPoint(world) != AttackKind.None;
        }

        /// <summary>诊断用：这一次给了哪些选项</summary>
        public string Describe()
        {
            if (!Visible) return "（收起）";
            var parts = new List<string>();
            foreach (var o in _options)
                parts.Add($"{o.Kind}{(o.Enabled ? "" : "(灰)")}"
                        + (string.IsNullOrEmpty(o.Badge) ? "" : " [" + o.Badge + "]"));
            return "[" + string.Join(" ", parts.ToArray()) + "]"
                 + (Hovered != AttackKind.None ? " 压着 " + Hovered : "");
        }

        // ---- 给自检用的 ----

        /// <summary>某个按钮在**世界坐标**的哪儿（没显示这一项就返回 null）。
        /// 🔴 **2026-10-16（`T09` 批 3）：一律读 `transform.position`** —— quad 与 `RectTransform` 都是
        /// `Transform`，两档拿到的都是**世界点**。⛔ uGUI 档的 `localPosition` 是**画布单位**、不是世界点
        /// （`Editor/BattleScene.cs` 的 A964 那条把这个读回口喂给 `UpdatePointer` ⇒ 读错档 = 静默错）。
        /// ⚠️ 世界档下与原来那句 `localPosition` **逐位相同**（选择器根在世界原点，A964 的前提钉着）。</summary>
        public Vector3? ButtonWorld(AttackKind k)
        {
            int i = SlotOf(k);
            if (i < 0 || !Visible) return null;
            if (_icons[i] == null || !_icons[i].gameObject.activeSelf) return null;
            return _icons[i].transform.position;
        }

        /// <summary>某个按钮当前的缩放（**「已选打法」那一格**应当是 1.3 —— 🆕 2026-09-29 起挂在
        /// `unit.attackType` 上，**不是指针悬停**；见 `RefreshPicked`）。</summary>
        public float ButtonScale(AttackKind k)
        {
            int i = SlotOf(k);
            if (i < 0 || _icons[i] == null) return 0f;
            return _icons[i].transform.localScale.x;
        }

        /// <summary>
        /// 1×1 白贴图 —— 原版底板那格没有 sprite（纯色），我们也用纯色板。
        /// ⚠️ 做成**实例字段**而不是 static：static 的 UnityEngine.Object 会跨场景存活，
        ///    场景一重载它就成了野指针（这工程在「共享材质」上踩过同一类坑）。
        /// </summary>
        Texture2D _solid;
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

        // ============================================================ 🔬 Editor-only 只读探针（A3-e）
        //
        //  ⚠️ **只读**（无 setter、不触发副作用）；播放器构建里整段不存在。
        //  判据（原版真值）→ 文件头：三颗 `Highlight` 的 `Image.m_RaycastTarget = 1`；底板 `MB_4492` 也是 1；
        //  `Highlight` 节点 rect 176.23 × 176.875（我们 = `RingW/RingH`，A28 起逐值相等）。
#if UNITY_EDITOR
        /// <summary>🆕 **2026-10-24（`A28`）** 自检用：某一格**按钮节点**的 rect（**设计 px**）。
        /// 原版 = **118.372 × 118.806**（`RT_3368/2804/2626`；`Image.m_PreserveAspect = 0` ⇒ 拉满）。
        /// <para>读的是**真几何** —— uGUI 走 `sizeDelta`、世界档走 `WorldW/H × 108`（= `HudPiece.SizePx` 那个口），
        /// ⛔ **不读本件常量**（那是拿被测实现自证）。没建出来 ⇒ `(-1,-1)`（⛔ 不拿 0 冒充「量到了」）。</para></summary>
        public Vector2 ButtonNodeSizePx(AttackKind k)
        {
            int i = SlotOf(k);
            if (i < 0 || _icons[i] == null) return new Vector2(-1f, -1f);
            return _icons[i].SizePx;
        }

        /// <summary>🆕 **2026-10-25（`X8` · ③）** 自检用：把「原版节点 rect 的 px」折成**世界单位**
        /// （= 造点时加在世界坐标上的那个长度）。三种口径都对齐到世界：
        /// · 世界档 = `W(px) × LayoutSpace.Scale`（与 `HitRect` 同一式）；
        /// · uGUI 档 = `px × _frame.lossyScale.x`（画布把设计 px 折成世界的那一级）。
        /// ⛔ 别拿它当命中判据（那只在 `KindAtPoint` 一处）。</summary>
        public float WorldOfPxForTest(float px)
        {
            if (_frame != null)
            {
                float s = _frame.lossyScale.x;
                return s > 1e-9f ? px * s : px;
            }
            return W(px) * LayoutSpace.Scale;
        }

        /// <summary>🆕 **2026-10-24（`A28`）** 自检用：某一格 `Highlight` 的节点 rect（**设计 px**）。
        /// 原版 = **176.23 × 176.875**（`RT_3028/2776/3512`；PA=0）。没建出来 ⇒ `(-1,-1)`。</summary>
        public Vector2 HighlightNodeSizePx(AttackKind k)
        {
            int i = SlotOf(k);
            if (i < 0 || _rings[i] == null) return new Vector2(-1f, -1f);
            return _rings[i].SizePx;
        }

        /// <summary>自检用：第 `i` 格（`Order` 下标）的 `Highlight` 件（世界档 / 没建出来 = `null`）。</summary>
        public HudPiece HighlightPieceForTest(int i) { return (i >= 0 && i < _rings.Length) ? _rings[i] : null; }
        /// <summary>自检用：第 `i` 格 `Highlight` 的 `Image.raycastTarget`（原版 = **1**）。</summary>
        public bool HighlightRaycastTargetForTest(int i)
        {
            var im = ImageOf(HighlightPieceForTest(i));
            return im != null && im.raycastTarget;
        }
        /// <summary>自检用：第 `i` 格的 `EventTrigger`（`PointerEnter/Exit`；`null` = 没挂上）。</summary>
        public EventTrigger HighlightTriggerForTest(int i) { return (i >= 0 && i < _hoverTriggers.Length) ? _hoverTriggers[i] : null; }
        /// <summary>🆕 **2026-10-25** 自检用：第 `i` 格**钮本体**（`Image` 118.372×118.806）上的 `EventTrigger`。
        /// 圈改成「只在已选那一格亮」之后，**没选中那两格的唯一受体就是它**（原版同样如此）。</summary>
        public EventTrigger IconTriggerForTest(int i) { return (i >= 0 && i < _iconTriggers.Length) ? _iconTriggers[i] : null; }
        /// <summary>🆕 **2026-10-25** 自检用：第 `i` 格的 `Highlight` 圈现在**亮着吗**
        /// （原版 = 只有「已选打法」那一格 `SetActive(true)`）。没建出来 ⇒ `false`。</summary>
        public bool HighlightActiveForTest(int i)
        {
            var p = HighlightPieceForTest(i);
            return p != null && p.gameObject != null && p.gameObject.activeSelf;
        }
        /// <summary>自检用：第 `i` 格 `PointerEnter` 来过几次（只增）。</summary>
        public int HighlightEnterCountForTest(int i) { return (i >= 0 && i < _hoverEnterCount.Length) ? _hoverEnterCount[i] : -1; }
        /// <summary>自检用：第 `i` 格 `PointerExit` 来过几次（只增）。</summary>
        public int HighlightExitCountForTest(int i) { return (i >= 0 && i < _hoverExitCount.Length) ? _hoverExitCount[i] : -1; }
        /// <summary>自检用：第 `i` 格 `Highlight` 的**节点 rect**（原版 176.23 × 176.875；我们 176.2²）。</summary>
        public Vector2 HighlightSizeForTest(int i)
        {
            var p = HighlightPieceForTest(i);
            return p != null && p.Rect != null ? HudCanvas.SizePx(p.Rect) : Vector2.zero;
        }
        /// <summary>自检用：底板 `Image.raycastTarget`（原版 `Select Attack Background` = **1**）。</summary>
        public bool BackgroundRaycastTargetForTest
        {
            get { var im = ImageOf(_background); return im != null && im.raycastTarget; }
        }
        static UnityEngine.UI.Image ImageOf(HudPiece p)
        {
            return p != null && p.IsUgui && p.Rect != null ? p.Rect.GetComponent<UnityEngine.UI.Image>() : null;
        }
#endif

        // ============================================================ 原版 `SimpleRotation`（`B97_HUDROT` · 2026-10-29）
        //
        // **缺口**：账 `未分类_其他.tsv:584` 原来记成「战场 prefab 没挂驱动物」—— 方向错了（`B97_MISC2` 复核时改的）。
        //   现读 `m_Script = -6857224376968460714`（= `SimpleRotation`；类名出处
        //   `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-6857224376968460714.json`）的全库 **42 个实例**：
        //   **13 场 × 3 + `Cog` × 3**。那 39 个**不是 3D 景物**，是**战斗 HUD 的 UI 图** —— 每颗都是
        //   `RectTransform + CanvasRenderer + Image + SimpleRotation + EverguildButtonMaterialModifier`
        //   （`bundle_scenes_scenes_battlearena1/GameObject/Highlight.json` = GO 112、
        //    同目录 `Highlight_396.json` / `Highlight_180.json` —— 三份的 `m_Name` 都是 `Highlight`）。
        //   ⇒ 真缺的是**这三颗圈的驱动**（圈本身与它的 rect 早已建好，见 `RingW`/`RingH`）。
        //
        // **逐场取证（39 个实例逐个现读，**几何 / 图 / speed 全同 ⇒ 写死，不按场分档**）**：
        //   · 父链（arena1 现读，其余 12 场同构、只有 pathID 不同）：
        //       `Highlight` 的**父**分别是 `Select Melee Button`(RT_3368) / `Select Active Skill Button`(RT_2626)
        //       / `Select Range Button`(RT_2804) —— 与 `Order = {Melee, Ability, Ranged}` 一一对应；
        //       再上：`Drag Attack Selector`(RT_2973) → `Safe area FrontCanvas`(2862) → `FrontCanvas`(2900)
        //       → `Canvas`(2759) → `BattleHud`(1395) → `BattlePrefab`(1300)。
        //       ⚠️ 我们这边圈是**平铺的兄弟件**（父链差一级，见报告 §⑤），几何等价。
        //   · `m_IsActive = true`（三颗序列化里都亮）；运行时的显隐由
        //     `CardDisplayAttackTypeButton__HighlightIfNeeded.c` 第一句
        //     `SetActive(highlightImage(+0x38), param_2 == attackType(+0x20))` 按「是不是已选打法」开关
        //     —— 我们已按这条实现（`IsPickedSlot` / `SetSlotActive` / `RefreshPicked`）。
        //   · `RectTransform`（RT_2776 / RT_3028 / RT_3512 等 39 份）：`m_AnchorMin = m_AnchorMax = (0.5, 0.5)` ·
        //     `m_AnchoredPosition = (0, 0)`（个别是 `-3.8e-5` 的浮点噪声）· `m_Pivot = (0.5, 0.5)` ·
        //     `m_LocalScale = 1` · `m_LocalRotation = identity`（= 起始角 0）·
        //     **`m_SizeDelta = (176.22999572753906, 176.875)`** ⇒ 与钮同心，尺寸 = `RingW × RingH`。
        //   · `Image`（MB_4108 / 4478 / …）：`m_Sprite` = **`Attack type button highlight`**
        //     （`d:/2/解包整理/03_界面UI/图集/battleatlasui/Sprite/Attack type button highlight_-7315808829623560242.json`
        //      · 227×227 · `m_PixelsToUnits 100` · pivot (0.5,0.5)）；`m_Color = (1,1,1,1)` ·
        //     `m_Type = 0`(Simple) · `m_PreserveAspect = 0` · `m_RaycastTarget = 1`。
        //     ⇒ 同名图我们已在用（`Resources/Art/ui/Attack_type_button_highlight.png`，`Build*` 里那张）。
        //   · `SimpleRotation.speed` = **`(0, 0, 100)`** —— 39 个实例逐个现读，**没有一个例外**
        //     （另 3 个 `Cog` 是 `(0, 0, -20)`，宿主在 `menus_assets_all` / `mainmenuwarpforge`，**不属本件**）。
        //     `SimpleRotation.cs:7` 的 `Update` = `transform.eulerAngles += speed * Time.deltaTime`
        //     ⇒ **100°/s，3.6 s 一圈**；数学那一半 = `Shell/LoadingMenu.cs` 的 `CardPresentation.SimpleRotation`
        //     （`Step` 已带 `Repeat(.., 360)`，与「下一次读 `eulerAngles` 拿到 0..360」等价）。
        //   · 第 5 个组件 `EverguildButtonMaterialModifier`（MB_4693）**序列化里一个字段值都没有**
        //     ⇒ `ToogleGreyScale(+0x20)` 为假 ⇒ `GetModifiedMaterial` 原样返回 ⇒ **无视觉作用，故不建**。
        //
        // ⚠️ **如实记：每颗圈各有一个角度累加器，而且只有【activeSelf 为真】的那颗才推进**。
        //   原版是 3 个独立 `SimpleRotation`（各转各的），**未被选中的那颗 GameObject 是关的 ⇒ Unity 不给它跑
        //   `Update` ⇒ 角度冻住**。这里逐字照那条（`!activeSelf` 就 `continue`），所以「切选」那一刻新亮起
        //   的那颗从它上次冻住的角度续上 —— 与原版同一条语义。⛔ 别图省事改成「三颗共用一个累加器 / 每帧
        //   无条件写三颗」：那会让关着的那颗也跟着走（原版不走），而且这条差异**只能在自检里看出来**。

        /// <summary>原版 `SimpleRotation.speed.z`：13 场 × 3 颗 `Highlight` **全部 = 100**（度/秒）。
        /// 判据 → 本节头「逐场取证」。</summary>
        const float RingSpinDegPerSec = 100f;

        /// <summary>三颗 `Highlight` 圈各自的自转角（度，0..360）—— 按 `Order` 下标。
        /// `0` = 还没转过（= 原版序列化里的 `m_LocalRotation = identity`）。</summary>
        readonly float[] _ringSpinDeg = new float[3];

        /// <summary>自检用：第 `i` 格（`Order` 下标）那个累加器当前的角度（度，0..360）。</summary>
        public float RingSpinDegForTest(int i)
        {
            return (i >= 0 && i < _ringSpinDeg.Length) ? _ringSpinDeg[i] : float.NaN;
        }

        /// <summary>自检用：某一格 `Highlight` 节点 **`localRotation` 的 z 欧拉角**（原版由 `SimpleRotation`
        /// 每帧写它）。件没建出来 ⇒ `NaN`（⛔ 不是 0 —— 「量不到」和「没转」必须分得开）。</summary>
        public float RingNodeRotationZForTest(AttackKind k)
        {
            for (int i = 0; i < Order.Length; i++)
                if (Order[i] == k)
                    return _rings[i] != null ? _rings[i].transform.localRotation.eulerAngles.z : float.NaN;
            return float.NaN;
        }

        /// <summary>原版 `SimpleRotation.Update` 的落地：把**亮着的那颗**圈按 `100°/s` 转一格。
        /// <para>⚠️ 入口只有 `Tick` / `TickForTest` 两处（都在 `Visible` 早退**之后**）—— 与「选择器收起时
        /// 三颗 GameObject 全是关的、`Update` 不跑 ⇒ 角度冻住」同义。</para>
        /// <para>⚠️ 写的是 **`localRotation`**：原版写 `transform.eulerAngles`（世界欧拉角），
        /// 但三颗圈的世界旋转只有 Z 这一份（父链全是恒等旋转 + `localScale 1`）⇒ 两者写出来的数相等。
        /// ⛔ 世界档的 `ImageQuad` / uGUI 档的 `RectTransform` 都没有别的旋转要保住，别改成 `rotation`
        /// （那会被父级旋转带偏）。</para></summary>
        void AdvanceRingRotation(float dt)
        {
            for (int i = 0; i < _rings.Length; i++)
            {
                if (_rings[i] == null || !_rings[i].gameObject.activeSelf) continue;
                _ringSpinDeg[i] = SimpleRotation.Step(_ringSpinDeg[i], RingSpinDegPerSec, dt);
                _rings[i].transform.localRotation = Quaternion.Euler(0f, 0f, _ringSpinDeg[i]);
            }
        }
    }
}
