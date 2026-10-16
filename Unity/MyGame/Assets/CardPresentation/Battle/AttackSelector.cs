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
//    三个按钮  118.4 × 118.8，间距 37，居中排开
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
using System.Collections.Generic;
using UnityEngine;

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
        const float ButtonSize = 118.8f;
        const float ButtonSpacing = 37f;
        const float HighlightScale = 1.3f;              // scaleMultiplierWhenSelected
        /// <summary>选中黄圈（原版按钮的子对象 `Highlight`）的实测尺寸</summary>
        const float RingSize = 176.2f;

        // ⚠️ **`40K_melee_glow` / `40K_ranged_glow` 不在这里用**。
        //    我一开始以为它们是按钮的外发光 —— **错了**。它们真正的用途是
        //    **场上单位卡身上、按攻击方式点亮的高亮**：
        //    `d:/2/解包整理/08_预制体特效/战斗预制体/GameObject/Highlight_*.json`（近战，红）和
        //    `Highlight ranged_*.json`（远程，紫），挂在 `CardPrefab/Board Elements/3DBody/
        //    Base Attack Counters/Melee Attack Container`（和 `Range Attack Container`）下面。
        //    那是「**这个单位能被选中**」的反馈，**还没做** ——
        //    锚点到底在「卡面的近战/远程数值格上」还是「卡脚下」还没钉死，
        //    证据和待办写在 `资料/规则引擎_进度与交接.md` 第七节，别照猜着做。
        //    按钮本身自带底盘和金属环（`Attack type button Melee/Ranged`），不用再垫一层光。

        // `Select Attack Background`：RectTransform 的 sizeDelta 是 780×285，**但它的 localScale = 0.44**，
        // 显示时 `backgroundController`（`inputScaleModifier`/`elementsHorizontalScaleModifier` 都是 1.5）
        // 再横向放大 1.5 倍 → **实际 514.8 × 125.4**，正好罩住那排 429.2 宽的按钮。
        // ⚠️ 只看 sizeDelta 会以为是 780×285，做出来一大块（这块差点做错，别再用 sizeDelta）
        const float BgW = 780f * 0.44f * 1.5f;          // 514.8
        const float BgH = 285f * 0.44f;                 // 125.4
        const float BgDropY = 18.375f;                  // 从槽顶往下挂多少
        /// <summary>三颗钮那一行比**锚点**低多少 px —— 就是原来 `center = _anchorAt − (0, W(BarH*0.5 − BgDropY))`
        /// 里那个量（42.7485）。⚠️ **这是我们自己的摆法，不是原版的**：原版三颗钮的 `ap` 都是 `(0,0)`
        /// （`RT_3563` / `RT_2819` / `RT_2997` 三份 `RectTransform`），而 `Select Attack Background` 是
        /// `ap (0,−18.375)` + pivot `(0.5,1)`（`RT_3254.json`）⇒ 「三颗钮在**节点中心**」才对得上。
        /// ⛔ 本件**只换渲染层与单位、不动排布模型**（差异已记进报告 §⑤，⛔ 别在本件里顺手改）。</summary>
        const float ButtonRowDownPx = BarH * 0.5f - BgDropY;   // 42.7485

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
        /// ⚠️ **我们仍然把它当弹出阈值用**（`BattleDriver` 的拖拽判据）—— 这是**如实标注的偏离**：
        /// 原版「真正的弹出阈值」是哪个常量**没定死**（下一行的 `DAT_1834b2bb0` 那条更正讲了原因）。
        /// ⇒ **要坐实只能进原版实机量**，已挂 `资料/真Play待验清单.md`。
        /// </summary>
        public const float DragThreshold01 = 0.085f;

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
                return new Vector2(_background.WorldH * _background.transform.localScale.x, _background.WorldH)
                     * PxPerUnit;
            }
        }

        /// <summary>自检用：底板的颜色（原版 `(1,1,1,1)` = 白实心）。</summary>
        public Color BgTint { get { return _background != null ? _background.Tint : Color.clear; } }

        /// <summary>拖拽阈值换算成世界单位（屏高恒 10 世界单位）</summary>
        public static float DragThresholdWorld
        {
            get { return DragThreshold01 * LayoutSpace.VisibleHeight; }
        }

        /// <summary>设计分辨率下 1 世界单位 = 108 px（可见高恒定 10 单位 = 1080 px）</summary>
        const float PxPerUnit = 108f;
        static float W(float px) { return px / PxPerUnit; }

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
            TickHover(dt);
            return PushDrag(TouchInputManager.TouchDragDeltaViewport);
        }

        /// <summary>🆕 A658 自检口：**自己喂**「这一帧的视口拖拽位移」。
        /// 批处理里没有输入层（`TouchInputManager.Update` 不会跑）⇒ 不喂就永远推不动进度。
        /// ⚠️ 走的是**和真实那一路完全相同**的 `TickHover` + `PushDrag`，不另写一份判据。</summary>
        public bool TickForTest(float dt, Vector2 dragDeltaViewport)
        {
            if (!Visible) return false;
            TickHover(dt);
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
        public void MarkHoverPicked() { _hoverSent = Hovered; }

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
        }

        readonly List<Option> _options = new List<Option>();
        // 🔴 **2026-10-16（`T09` 批 3）：类型从 `ImageQuad` 换成 `HudPiece`**（适配层；先例 = HUD 图族那 39 个字段）。
        //   「一个件要么是世界空间 quad、要么是 uGUI 的 `RectTransform + Image`，二选一」的规矩 → `Battle/HudPiece.cs` 文件头。
        //   `Label` **不换**：它自己就有 uGUI 后端（`Label.CreateUgui` / `IsUgui`），见 `Battle/Label.cs` 文件头。
        readonly HudPiece[] _icons = new HudPiece[3];
        readonly HudPiece[] _rings = new HudPiece[3];
        readonly Label[] _captions = new Label[3];

        HudPiece _background;
        Label _title, _note;
        string _titleText;
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
            string title = _titleText, note = _noteText;
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
            if (_title != null) doomed.Add(_title.gameObject);
            if (_note != null) doomed.Add(_note.gameObject);
            for (int i = 0; i < Order.Length; i++)
            {
                if (_icons[i] != null) doomed.Add(_icons[i].gameObject);
                if (_rings[i] != null) doomed.Add(_rings[i].gameObject);
                if (_captions[i] != null) doomed.Add(_captions[i].gameObject);
            }
            if (_frame != null) doomed.Add(_frame.gameObject);       // ⛔ 只到帧这一级（宿主画布是共享的）
            foreach (var go in doomed) if (go != null) Object.DestroyImmediate(go);

            _background = null;
            _title = null;
            _note = null;
            for (int i = 0; i < Order.Length; i++) { _icons[i] = null; _rings[i] = null; _captions[i] = null; }
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
        /// 帧的 rect **逐字照原版**（`RectTransform_3166.json`：中心锚 + 中心枢轴 +
        /// `sizeDelta 621.7249755859375 × 122.24700164794922`，我们的常量取 621.7 / 122.2）；
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
                                                      0f, 0f, BgW, BgH, ZBg, new Color(1f, 1f, 1f, 1f));

            // 标题上面那行小字（技能效果）**不能塞在中场的提示行上** —— 提示行 y=532 正好是按钮的位置，
            // 会被按钮压住（踩过）。这条与两档无关，照旧。
            _title = HudCanvas.MakeText(P, "", 0f, 0f, 3,
                                        new Color(0.86f, 0.89f, 0.94f), new Vector2(0.5f, 0.5f),
                                        ZText, "SelectorTitle");
            _note = HudCanvas.MakeText(P, "", 0f, 0f, 2,
                                       new Color(0.98f, 0.93f, 0.72f), new Vector2(0.5f, 0.5f),
                                       ZText, "SelectorNote");

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
                                                            ButtonSize * AspectOf(itex), ButtonSize,
                                                            ZIcon, Color.white);

                // 选中黄圈（原版里是按钮的子对象 `Highlight`；我们这边是**平铺的兄弟件** ——
                // 画出来同一处，差别只在父链，见报告 §⑤）
                var rtex = CardArt.Ui("Attack_type_button_highlight");
                if (rtex != null)
                    _rings[i] = HudCanvas.MakeImageInParent(P, rtex, "Highlight_" + k, 0f, 0f,
                                                            RingSize * AspectOf(rtex), RingSize,
                                                            ZRing, Color.white);

                // 按钮上的数字角标（原版 `Select Active Skill Button/ValueText`）。
                // 原版字号很大（base 36 / max 89），配上它那个半透明底图，数字是按钮的主视觉
                _captions[i] = HudCanvas.MakeText(P, "", 0f, 0f, 4, new Color(1f, 0.96f, 0.80f),
                                                  new Vector2(0.5f, 0.5f), ZText, "Badge_" + k);
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
            //    满编时 `SelectAttackBackgroundController` 再 ×1.5 ⇒ **实绘 514.8 × 125.4**（`BgW` × `BgH`）。
            _background = HudPiece.Of(ImageQuad.Create(transform, Solid(), Vector3.zero,
                                           W(BgH), new Vector2(0.5f, 1f), "SelectorBackground"));
            if (_background != null)
            {
                // `ImageQuad` 是按**高** + 贴图宽高比摆的，纯色图是 1:1 ⇒ 横向自己拉到 `BgW/BgH`
                _background.transform.localScale = new Vector3(BgW / BgH, 1f, 1f);
                _background.SetTint(new Color(1f, 1f, 1f, 1f));
            }

            _title = Label.Create(transform, "", Vector3.zero, 3,
                                  new Color(0.86f, 0.89f, 0.94f), new Vector2(0.5f, 0.5f), "SelectorTitle");
            // 标题上面那行小字（技能效果）。**不能塞在中场的提示行上** ——
            // 提示行 y=532 正好是按钮的位置，会被按钮压住（踩过）
            _note = Label.Create(transform, "", Vector3.zero, 2,
                                 new Color(0.98f, 0.93f, 0.72f), new Vector2(0.5f, 0.5f), "SelectorNote");

            for (int i = 0; i < Order.Length; i++)
            {
                var k = Order[i];

                // 按钮本体（原版 Attack type button *，自带底盘和金属环）
                _icons[i] = HudPiece.Of(ImageQuad.Create(transform, CardArt.Ui(IconName(k)), Vector3.zero,
                                             W(ButtonSize), new Vector2(0.5f, 0.5f), "Icon_" + k));

                // 选中黄圈（原版里是按钮的子对象 `Highlight`）
                _rings[i] = HudPiece.Of(ImageQuad.Create(transform, CardArt.Ui("Attack_type_button_highlight"),
                                             Vector3.zero, W(RingSize),
                                             new Vector2(0.5f, 0.5f), "Highlight_" + k));

                // 按钮上的数字角标（原版 `Select Active Skill Button/ValueText`）。
                // 原版字号很大（base 36 / max 89），配上它那个半透明底图，数字是按钮的主视觉
                _captions[i] = Label.Create(transform, "", Vector3.zero, 4,
                                            new Color(1f, 0.96f, 0.80f),
                                            new Vector2(0.5f, 0.5f), "Badge_" + k);
            }
        }

        /// <summary>贴图宽高比（`ImageQuad.Create` 那条「按高定、宽由比例来」的算法，uGUI 档自己算一次
        /// —— 因为 `HudCanvas.QuadFromTopPx` 的 x/y 是屏幕绝对 px，本件用不了它，见 `BuildOnCanvas` 里那条）。</summary>
        static float AspectOf(Texture2D t)
        {
            return t != null && t.height > 0 ? (float)t.width / t.height : 1f;
        }

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

            if (_background != null) _background.gameObject.SetActive(false);
            if (_title != null) _title.gameObject.SetActive(false);
            if (_note != null) _note.gameObject.SetActive(false);
            for (int i = 0; i < Order.Length; i++) SetSlotActive(i, null);
        }

        /// <summary>
        /// 弹出并给选项。**按 `Kind` 对应按钮，不靠下标** ——
        /// 调用方给哪几项、给不给全，都不会串位。
        /// </summary>
        /// <param name="note">标题上面再加一行小字（技能效果之类）。空就不显示</param>
        /// <param name="atWorld">🆕 2026-09-29：**被拖的那个单位在哪**（世界坐标 = 指针/布局空间）。
        /// 原版展开时把整条 `set_position(单位位置)`（`UnitOnBoardAttackTypeSelector__Toggle.c:112-126`）
        /// —— 传 null 就退回「屏幕中心」（那是我们原来的固定摆法）。</param>
        public void Show(List<Option> options, string title = null, string note = null,
                         Vector3? atWorld = null, int selectedType = 0)
        {
            Build();
            _anchorAt = atWorld ?? Vector3.zero;
            // 🆕 2026-09-29：**已选打法那一格**（原版 `unit.attackType`；0 = 这个单位还没打过 ⇒ 不亮圈）
            _selected = KindOf(selectedType);
            _options.Clear();
            if (options != null) _options.AddRange(options);
            ApplyOptionIcons();
            _titleText = title;
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
            RefreshLayout();
        }

        /// <summary>
        /// 🆕 2026-09-25 把每一格的底图刷成**这一次该用的那张**。
        /// 为什么不建的时候一次定死：**同一个按钮格子在不同单位上要换图** ——
        /// 带 `Duty` 的兵那一格是修女/星军…那张专属图，普通的兵是占位图。
        /// ⚠️ 取不到图就**保持上一次的**（别 `SetTexture(null)` 把按钮刷没）。
        /// </summary>
        void ApplyOptionIcons()
        {
            for (int i = 0; i < Order.Length; i++)
            {
                if (_icons[i] == null) continue;
                var o = Find(Order[i]);
                string want = (o != null && !string.IsNullOrEmpty(o.IconName)) ? o.IconName : IconName(Order[i]);
                var tex = CardArt.Ui(want);
                if (tex != null && !ReferenceEquals(_icons[i].Texture, tex))
                {
                    _icons[i].SetTexture(tex);
                    // 🔴 **2026-10-16（`T09` 批 3）：uGUI 档要自己把宽补上** ——
                    //   世界档的 `ImageQuad.SetTexture` 会**重算宽高比**、uGUI 的 `Image` 换 sprite
                    //   **不会动 `sizeDelta`**（`HudPiece.SetTexture` 只重建 `Sprite`）⇒ 不补这一句，
                    //   换图之后那一格会**留着上一张的宽度**（替代行动那五张 128×128 与占位图尺寸不同时看得出来）。
                    if (_icons[i].IsUgui && _icons[i].Rect != null)
                        _icons[i].Rect.sizeDelta = new Vector2(ButtonSize * AspectOf(tex), ButtonSize);
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
            //    `set_position(Get2DWorldPosFromBoardPos(被拖单位.position))` —— 也就是把**那格的 pivot**
            //    放到单位的位置上；那条的 `pivot = (0.5, 1)`（实读 `RectTransform_3061.json`）
            //    ⇒ 条子从单位那儿**往下垂**。
            //    我们的底板 pivot 同样是 `(0.5,1)`、位置 = `center + (0, BarH/2 − BgDropY)`，
            //    所以要让**底板那一格的 pivot** 落在单位上，`center` 得先减掉那个偏移。
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

            var center = origin - new Vector3(0f, U(ButtonRowDownPx), 0f);

            if (_background != null)
            {
                _background.gameObject.SetActive(true);
                // 底板从**槽顶往下 18.4 px** 挂起（原版 anchor (0.5,1) + pivot (0.5,1)）
                _background.transform.localPosition =
                    center + new Vector3(0f, U(ButtonRowDownPx), Z(ZBg));
            }
            if (_title != null)
            {
                _title.gameObject.SetActive(!string.IsNullOrEmpty(_titleText));
                _title.SetText(_titleText);
                _title.transform.localPosition = center + new Vector3(0f, U(BarH * 0.5f + 26f), Z(ZText));
            }
            if (_note != null)
            {
                _note.gameObject.SetActive(!string.IsNullOrEmpty(_noteText));
                _note.SetText(_noteText);
                _note.transform.localPosition = center + new Vector3(0f, U(BarH * 0.5f + 54f), Z(ZText));
            }

            // 只排**这次真的要显示**的那几项，居中 —— 原版是 HorizontalLayoutGroup(MiddleCenter)，
            // 所以只有两个按钮时它们会往中间收，不会留着技能那个空档
            var shown = new List<AttackKind>();
            for (int i = 0; i < Order.Length; i++)
                if (Find(Order[i]) != null) shown.Add(Order[i]);

            for (int i = 0; i < Order.Length; i++) SetSlotActive(i, null);

            for (int n = 0; n < shown.Count; n++)
            {
                int i = SlotOf(shown[n]);
                var opt = Find(shown[n]);
                // ⚠️ **间距那一项过 `US`**（不是 `U`）：世界档原来乘了 `LayoutSpace.Scale`（窄屏整体缩），
                //    uGUI 档由 `CanvasScaler` 负责 ⇒ 不乘。
                float x = (n - (shown.Count - 1) * 0.5f) * US(ButtonSize + ButtonSpacing);
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

        /// <summary>`opt` 传 null = 关掉这一格（三个按钮节点是**一次建满**的，之后只切显隐）</summary>
        void SetSlotActive(int i, Option opt)
        {
            bool on = opt != null;
            if (_icons[i] != null) _icons[i].gameObject.SetActive(on);
            if (_rings[i] != null) _rings[i].gameObject.SetActive(on && opt.Enabled);
            if (_captions[i] != null)
                _captions[i].gameObject.SetActive(on && !string.IsNullOrEmpty(opt.Badge));
        }

        // ==================================================================
        //  指针
        // ==================================================================

        /// <summary>喂指针世界坐标（每帧）。返回压着的那个按钮。</summary>
        public AttackKind UpdatePointer(Vector3 world)
        {
            if (!Visible) return AttackKind.None;

            // 🆕 2026-09-29：**先推进入场动画，再判命中** —— 反过来的话这一帧用的是上一帧的位置
            // （原版也是这个顺序：`Update` 里先 `MoveButtons`，输入事件是另外进的）。
            Tick(Time.deltaTime);

            // 🔴 **2026-10-16（`T09` 批 3）：命中一律按【世界点】算**（两档都对，见 `T09` §五·补④）：
            //   · 世界档：`transform.position` 与原来的 `localPosition` **逐位相同**（选择器根在世界原点，
            //     `Editor/BattleScene.cs` 的 A964 那条前提钉着）⇒ 这一支的行为一个字没变；
            //   · uGUI 档：`localPosition` 是**画布单位**、不是世界点 ⇒ **只能用 `position`**。
            //   半径：世界档 = `W(ButtonSize)/2 × Scale`（原样）；uGUI 档 = **画布单位 → 世界单位**的系数
            //   （`_frame.lossyScale`）× 按钮半径。⛔ 拿 px 去比世界点 = 命中框**大 108 倍**、而画面完全正常。
            //   ⚠️ 取两个轴的 **min**：批处理下画布两轴系数不同（1/108 vs 1/144），取小的那一档是**保守**方向
            //     （「还没碰上就选中」正是圆判据要躲的那个错）；真机两轴相等 ⇒ 与旧值逐位一致。
            float half = _frame != null
                       ? Mathf.Min(_frame.lossyScale.x, _frame.lossyScale.y) * (ButtonSize * 0.5f)
                       : W(ButtonSize) * 0.5f * LayoutSpace.Scale;

            var hit = AttackKind.None;
            for (int i = 0; i < Order.Length; i++)
            {
                var opt = Find(Order[i]);
                if (opt == null || !opt.Enabled) continue;
                if (_icons[i] == null || !_icons[i].gameObject.activeSelf) continue;

                // 命中按**圆形**判：图标是圆钮，用方形外接框会「还没碰上就选中」
                var d = _icons[i].transform.position - world;
                if (d.x * d.x + d.y * d.y <= half * half) { hit = opt.Kind; break; }
            }

            if (hit != Hovered) { Hovered = hit; RefreshPicked(); }
            return Hovered;
        }

        /// <summary>把「已选打法」那一格画出来：**黄圈 + 放大 1.3 倍**（原版 `scaleMultiplierWhenSelected`）。
        /// 🔴 **2026-09-29 照原版改**：原来这个高亮挂在**指针悬停**上（`Hovered`），而原版挂的是
        /// `unit.attackType`（= 这个单位上一次用的打法，`HighlightSelectedAttackTypeButtons`）。
        /// ⚠️ **悬停现在不再单独给视觉**（原版那一圈只有"已选"这一档）—— 实机觉得别扭再单记，别自己发明。</summary>
        void RefreshPicked()
        {
            for (int i = 0; i < Order.Length; i++)
            {
                var opt = Find(Order[i]);
                if (opt == null) continue;

                bool hot = _selected != AttackKind.None && opt.Kind == _selected && opt.Enabled;
                float s = hot ? HighlightScale : 1f;
                if (_icons[i] != null) _icons[i].transform.localScale = new Vector3(s, s, 1f);
                if (_rings[i] != null && _rings[i].gameObject.activeSelf)
                {
                    _rings[i].transform.localScale = new Vector3(s, s, 1f);
                    _rings[i].SetTint(hot ? Color.white : new Color(1f, 1f, 1f, 0.28f));
                }
            }
        }

        /// <summary>指针是不是在整条槽的范围里（点在槽外 = 取消）。
        /// ⚠️ **两档都不用改**：这里比的全是**世界量**（喂进来的点在世界上、`hw/hh` 由 px 常量折成世界单位）
        /// ⇒ 与「渲染层在哪一档」无关。
        /// 🔴 **顺手发现（本件【没】改，已记报告 §⑤）**：这个盒子**以世界原点（屏心）为中心**，
        /// 而 2026-09-29 起整条挂在**被拖那个单位**的位置上（`_anchorAt`）⇒ 两者只在「单位恰好在屏心」
        /// 时重合。改它 = 改**输入行为**（「点槽外取消」的判定域），不属于本次「只换渲染层与单位」
        /// ⇒ 留给主对话裁（铁律 11：要修，但不该在本件里顺手修）。</summary>
        public bool ContainsBar(Vector3 world)
        {
            if (!Visible) return false;
            float hw = W(BarW) * 0.5f * LayoutSpace.Scale;
            float hh = W(BarH + BgH * 0.5f) * LayoutSpace.Scale;   // 连底板一起算进去
            return Mathf.Abs(world.x) <= hw && world.y <= hh && world.y >= -hh * 2f;
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
    }
}
