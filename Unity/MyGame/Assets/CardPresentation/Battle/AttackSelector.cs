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

        /// <summary>自检用：底板的**实绘尺寸**（px @1920×1080 —— 原版那条拍的是 **514.8 × 125.4**）。</summary>
        public Vector2 BgSizePx
        {
            get
            {
                if (_background == null) return Vector2.zero;
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

        /// <summary>外扩偏移（世界单位）—— 进度 1 时是零。</summary>
        Vector3 EnterOffset(AttackKind k)
        {
            if (_enterT >= 1f) return Vector3.zero;
            var d = DirectionOf(k);
            float len = d.magnitude;
            if (len <= 1e-5f) return Vector3.zero;                 // 原版退化支：不外扩
            d /= len;
            float dist = W(FurtherDist(k)) * LayoutSpace.Scale * (1f - _enterT);
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
            /// <see cref="IconName(AttackKind)"/> 的默认）。
            ///
            /// 为什么要有它：原版**主动技能那一格的 icon 是按卡运行时赋的**
            /// （`BattleCardUI` 的 `buttonIcon`），而「替代行动」那五个阵营**各有专属一张**
            /// ——`Attack type button {Pray, Duty, Ferocity, Agenda, Oath}`（128×128，
            /// 在 `assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Texture2D/`，
            /// 2026-09-25 导进 `Resources/Art/ui/Attack_type_button_<X>.png`）。
            /// ⇒ 有替代行动的卡**不该再共用那张占位图**（见 <see cref="IconName(AttackKind)"/> 的注释）。
            /// </summary>
            public string IconName;
        }

        readonly List<Option> _options = new List<Option>();
        readonly ImageQuad[] _icons = new ImageQuad[3];
        readonly ImageQuad[] _rings = new ImageQuad[3];
        readonly Label[] _captions = new Label[3];

        ImageQuad _background;
        Label _title, _note;
        string _titleText;
        string _noteText;
        bool _built;

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

            // 底板：原版那格 `Image` **没有 sprite**（`m_Sprite = null`）⇒ 画出来就是一块**纯色实心矩形**。
            // 🔴 **2026-09-29 照原版改（原来画错了：深色半透明 + 125.4² 方块，`BgW` 声明了没用）**：
            //    实读 `bundle_scenes_scenes_battlearena1/`（13 场都有同名节点）——
            //    `RectTransform_3061.json`：**sizeDelta 780×285 · localScale 0.44 · pivot (0.5,1)**；
            //    `MonoBehaviour_4492.json`（= 那个 Image）：**`m_Sprite` 空 · `m_Color = (1,1,1,1)`**（白、不透明）。
            //    满编时 `SelectAttackBackgroundController` 再 ×1.5 ⇒ **实绘 514.8 × 125.4**（`BgW` × `BgH`）。
            _background = ImageQuad.Create(transform, Solid(), Vector3.zero,
                                           W(BgH), new Vector2(0.5f, 1f), "SelectorBackground");
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
                _icons[i] = ImageQuad.Create(transform, CardArt.Ui(IconName(k)), Vector3.zero,
                                             W(ButtonSize), new Vector2(0.5f, 0.5f), "Icon_" + k);

                // 选中黄圈（原版里是按钮的子对象 `Highlight`）
                _rings[i] = ImageQuad.Create(transform, CardArt.Ui("Attack_type_button_highlight"),
                                             Vector3.zero, W(RingSize),
                                             new Vector2(0.5f, 0.5f), "Highlight_" + k);

                // 按钮上的数字角标（原版 `Select Active Skill Button/ValueText`）。
                // 原版字号很大（base 36 / max 89），配上它那个半透明底图，数字是按钮的主视觉
                _captions[i] = Label.Create(transform, "", Vector3.zero, 4,
                                            new Color(1f, 0.96f, 0.80f),
                                            new Vector2(0.5f, 0.5f), "Badge_" + k);
            }

            Hide();
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
                if (tex != null && !ReferenceEquals(_icons[i].Texture, tex)) _icons[i].SetTexture(tex);
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
            var center = _anchorAt - new Vector3(0f, W(BarH * 0.5f - BgDropY), 0f);

            if (_background != null)
            {
                _background.gameObject.SetActive(true);
                // 底板从**槽顶往下 18.4 px** 挂起（原版 anchor (0.5,1) + pivot (0.5,1)）
                _background.transform.localPosition =
                    center + new Vector3(0f, W(BarH * 0.5f - BgDropY), ZBg);
            }
            if (_title != null)
            {
                _title.gameObject.SetActive(!string.IsNullOrEmpty(_titleText));
                _title.SetText(_titleText);
                _title.transform.localPosition = center + new Vector3(0f, W(BarH * 0.5f + 26f), ZText);
            }
            if (_note != null)
            {
                _note.gameObject.SetActive(!string.IsNullOrEmpty(_noteText));
                _note.SetText(_noteText);
                _note.transform.localPosition = center + new Vector3(0f, W(BarH * 0.5f + 54f), ZText);
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
                float x = (n - (shown.Count - 1) * 0.5f) * W(ButtonSize + ButtonSpacing) * LayoutSpace.Scale;
                var p = center + new Vector3(x, 0f, 0f);

                SetSlotActive(i, opt);
                // 🆕 2026-09-29：**入场偏移**（原版 `MoveButton` 的 `lerp(外扩位, 归位位, t)`）——
                // 进度 1 时 `EnterOffset` 恒为 0，所以动画跑完这条等于没加。
                var off = EnterOffset(shown[n]);
                if (_icons[i] != null) _icons[i].transform.localPosition = p + off + new Vector3(0f, 0f, ZIcon);
                if (_rings[i] != null) _rings[i].transform.localPosition = p + off + new Vector3(0f, 0f, ZRing);
                if (_captions[i] != null)
                {
                    _captions[i].SetText(opt.Badge);
                    _captions[i].transform.localPosition =
                        p + off + new Vector3(W(BadgeDx), W(BadgeDy), ZText);
                }

                // 不能选的置灰 —— 「这个单位现在打不了这一种」要一眼看出来
                float dim = opt.Enabled ? 1f : 0.42f;
                if (_icons[i] != null)
                    _icons[i].SetTint(new Color(dim, dim, dim, opt.Enabled ? 1f : 0.8f));
            }

            RefreshPicked();
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

            var hit = AttackKind.None;
            for (int i = 0; i < Order.Length; i++)
            {
                var opt = Find(Order[i]);
                if (opt == null || !opt.Enabled) continue;
                if (_icons[i] == null || !_icons[i].gameObject.activeSelf) continue;

                // 命中按**圆形**判：图标是圆钮，用方形外接框会「还没碰上就选中」
                float half = W(ButtonSize) * 0.5f * LayoutSpace.Scale;
                var d = _icons[i].transform.localPosition - world;
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

        /// <summary>指针是不是在整条槽的范围里（点在槽外 = 取消）</summary>
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

        /// <summary>某个按钮在世界坐标的哪儿（没显示这一项就返回 null）</summary>
        public Vector3? ButtonWorld(AttackKind k)
        {
            int i = SlotOf(k);
            if (i < 0 || !Visible) return null;
            if (_icons[i] == null || !_icons[i].gameObject.activeSelf) return null;
            return _icons[i].transform.localPosition;
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
