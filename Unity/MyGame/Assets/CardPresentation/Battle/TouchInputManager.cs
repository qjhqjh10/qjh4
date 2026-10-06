// TouchInputManager.cs — 原版 `TouchInputManager`（`dump.cs` TypeDefIndex 2299）**整件**（A463，2026-10-13）
//
// ============================ 这件是干什么的 ============================
// 原版把「这一帧的指针/触摸状态」收成**一组静态属性**，谁要谁读：
//   · `BattleManager` 的 6 处「鼠标在世界哪儿」（`GetBoardMousePos` / `GetBoardCardMousePos` /
//     `GetMousePerspectivePos` / `GetTargetInPos` / `ChoiceOfCardPlayer` / `StopDisplayCardDescription`）
//   · `BattleSettingsWindow` 三处（`Open` 关掉它、`CloseWindow` / `ESCPressed` 开回来）
//   · `AttackTypesButtonsController.MoveButtons`（读 `TouchDragDeltaViewport` 当拖拽速度）
//   · `EnemyInfoTouch.Update`（读 `TouchPressed`）
//   · `CombatCameraZoom` 4 处（读 `ScrollDelta` / `TouchPressedSecondary` / `TouchDragDelta`）
//   · `BattleManager._CloseBattleDoors` 协程
// ⇒ 它是**输入层唯一的真相源**（全反编译里 `Instance`（静态字段 `+0x30`）命中 **37 个文件**，
//   其中 12 个是上面那些消费者、17 个是它自己、4 个是 `CombatCameraZoom`、剩下 4 个是 `Instance` 的读写）。
//
// ============================ 判据（全是实读） ============================
// ① **方法体** = `d:/2/tools/decomp_full/TouchInputManager__{Awake, Update, UpdateDrag, Toggle}.c`。
// ② **字段名 / 偏移 / 出厂值** = `d:/2/tools/il2cpp_out/dump.cs` 的 `TouchInputManager`：
//    实例：`forceMobileInput` `+0x20` · `lastTouchPosition` `+0x24` · `lastTwoFingerDistance` `+0x2C`；
//    静态：`TouchPosition` `+0x00`(Vector2) · `TouchPressed` `+0x08` · `TouchPressedSecondary` `+0x09` ·
//          `TouchDragDelta` `+0x0C`(Vector2) · `IsDragging` `+0x14` · `TouchDragDeltaViewport` `+0x18`(Vector2) ·
//          `ScrollDelta` `+0x20`(float) · `TwoFingerMidPoint` `+0x24`(Vector2) · `Instance` `+0x30`。
//    **场景序列化值**（`assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4011.json`，
//    挂在 GO `712` 上）：`forceMobileInput: 0`（= ctor 默认，两边同）。
// ③ 🔴 **两个被反编译器认错名字的调用**（本件按**指令流**读回来的，`.c` 里看不出来）：
//    `TouchInputManager__Update.c` 里以两个 `FUN_` 出现的，其实是**本类自己的属性访问器**（编译器把它们折叠到了
//    另一处地址，所以 il2cppdumper 的名字表按「名义地址」查不到）：
//    · `FUN_180789a80`（RVA `0x789A80`）= **`get_TwoFingerMidPoint`** —— 体 = 取类指针 → `[class+0xB8]`（静态字段块）
//      → 读 `+0x24`/`+0x28` 组一个 `Vector2` 返回（`movss xmm0,[rcx+0x28]` / `movss xmm1,[rcx+0x24]` / `unpcklps`）。
//    · `FUN_180789ad0`（RVA `0x789AD0`）= **`set_ScrollDelta`** —— 体 = 取类指针 → `[class+0xB8]`
//      → `movss [rcx+0x20], xmm0`（**收一个 float 实参**、**不返回值** ⇒ `.c` 里看着像「调了个 void 函数、结果丢了」）。
// ④ **`Update` 里那两处「同名但实为别的成员」**（同 `CombatCameraZoom` 文件头 ③ 那族，Ghidra 的**系统性**问题）：
//    · `WebSocketSharp…get_Count` = **`Touch.get_phase`**（拿返回值与 `0` 比 = `TouchPhase.Began`）；
//    · `Unity_Collections_NativeArray_Enumerator<Vector2>…get_Current` = **`Touch.get_deltaPosition`**
//      （按指令流：`call 0x183a330`，`all_methods.txt` 里那是 `UnityEngine.Touch$$get_deltaPosition`）
//      —— 而 `System_Nullable<Vector2>…GetValueOrDefault` = **`Touch.get_position`**。
//    ⇒ `touchCount == 1` 那一支**不是**「有没有触摸」，是「手指在动」。
// ⑤ **`Update` 的调用序列**（RVA `0x79BD20`，逐条 `call` 解析出来的）：
//    `Input.get_mouseScrollDelta` → `GetMouseButton(0)` → `GetMouseButton(1)` → `Input.get_mousePosition`
//    → `GetMouseButtonDown(0)` / `GetMouseButtonDown(1)` → `Input.get_touchCount` / `GetTouch` / `Touch.get_phase`
//    → `Application.get_isMobilePlatform` → `UpdateDrag` → `Application.get_isMobilePlatform` →
//      `get_touchCount` ×2 → `GetTouch` ×2 → …
// ⑥ **`ScrollDelta` 取的是 `.y` 不是 `.x`** —— `Update` 的 `0x79BDDC` 处是 `movss xmm0,[rbp+0x124]`
//    （`mouseScrollDelta` 的返回被吐到 `[rbp+0x120]`(x) / `[rbp+0x124]`(y)）⇒ **`.y`**。
//    （同 `CombatCameraZoom.cs` 文件头 ⑦ 那一句，两处互相独立地读到同一个结论。）
// ⑦ **`.rdata` 常量（`工具/read_literal.py` 直读，4 字节 float）**：
//    `0x1834b2bb4` = **0.5**（两指中点：`(p0 + p1) × 0.5`）· `0x1834b2bb8` = **1.0**（viewport 换算的分母）
//    · `0x1834b2bc8` = **−1.0**（捏合：`ScrollDelta = −(距离差)`）。
//
// ============================ 🔴 我们做的三处「等价物」（⛔ 不当原版） ============================
// A. **输入源换了**：原版读 legacy `UnityEngine.Input`（`Input.mousePosition` / `GetMouseButton` /
//    `mouseScrollDelta` / `GetTouch`）。本工程 `ProjectSettings.asset:932 activeInputHandler: 1`
//    ⇒ **只有新输入系统，legacy `Input` 不可用**（同 `CombatCameraZoom.PollPointerSource`）。
//    等价物 = `ReadRaw()`：`Mouse.current.{position,scroll,leftButton,rightButton}` +
//    `Touchscreen.current.touches`（活动手指）。**逐格对应关系写在那张表里，没有一处是靠猜的。**
//    ⚠️ 唯一的量纲换算：滚轮。legacy 一格 = **±1**，新输入系统一格 = **±120**
//    （同源口径 = `Shell/MenuScroll.cs:200`）⇒ 除 `ScrollUnitsPerNotch`。⛔ 只影响 `ScrollDelta` 一格的**数值大小**，
//    「取 `.y`」这条语义不变。
// B. **`Ensure()`（惰性自建）**：原版这东西是**摆在战场场景里的一件组件**
//    （`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4011.json`，挂 GO `712`）。
//    我们的战场是一棵**自己搭的树**，没有那件 ⇒ 第一次有人读它时把它建出来（幂等）。
//    ⛔ 这不是「另一条实现」：建出来的就是本类，走的还是 `Awake` + `Update`。
// C. **自检口**（批处理里没有真鼠标、也没有帧循环）：`RawOverride`（把这一帧的原始读数钉成给定的）·
//    `MobilePlatformOverride`（钉 `Application.isMobilePlatform`）· `Tick()`（显式驱动一帧，= `Update` 的体）·
//    `UpdateDragForTest` / `LastTouchPositionForTest`（钉住「上一帧指针在哪」，好把纯算式单独验）。
//    ⛔ 生产路径一律不碰它们。
//
// ============================ 🔴 我们没做 / 做不到的（如实，⛔ 不冒充原版） ============================
// D. **双指 / 单指那两路是移动端的，我们这台是桌面** —— 照原版写出来、`RawOverride` 也验得了，
//    但**没有真机**（`Application.isMobilePlatform` 在编辑器里恒 `false`）⇒ **如实记：没在设备上验过**。
// E. **`Toggle(bool)` 只改 `enabled`**（原版逐句 = `Behaviour.set_enabled`）—— 它**不**清零那些静态格。
//    原版 `BattleSettingsWindow.Open` 就是靠它把 `Update` 停掉（于是那 8 格**冻在上一帧的值**上）。
//    我们照抄这个语义：⛔ 别「顺手」在 `Toggle(false)` 里加清零 —— 那会把原版的行为改掉。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 指针/触摸的**唯一输入源**（原版 `TouchInputManager`，TypeDefIndex 2299）：每帧把
    /// 鼠标位置 / 按下 / 滚轮 / 拖拽增量收成一组静态属性，供战场各系统读。
    /// <para>⚠️ 原版的 12 个消费者里，我们**只有 `CombatCameraZoom` 一条真的接上了**（见文件头）；
    /// 其余各家我们都有自己的一条路（`BattleDriver.WorldPointer` / `PointerHeld` 等），
    /// 逐条对照写在 `资料/普查产出_1013/WB5_两件组件.md` §二。</para>
    /// </summary>
    public class TouchInputManager : MonoBehaviour
    {
        // ==================================================================
        //  原版字段（偏移 / 出厂值都有出处，见文件头 ②）
        // ==================================================================

        /// <summary>原版 `+0x20`。场景值 **0**（= 出厂）。真 = **桌面也走移动端那两路触摸分支**
        /// （`Update` 里 `if (Application.isMobilePlatform || forceMobileInput)`）。</summary>
        [SerializeField] bool forceMobileInput;

        /// <summary>原版 `+0x24`。上一帧指针在哪（`UpdateDrag` 的差分基准）。</summary>
        Vector2 lastTouchPosition;

        /// <summary>原版 `+0x2C`。上一帧两指距离（捏合缩放的差分基准；`< 0` / `== 0` 时不产 `ScrollDelta`）。</summary>
        float lastTwoFingerDistance;

        // ==================================================================
        //  原版 8 个静态属性（静态字段偏移见文件头 ② —— 与 `dump.cs` 的 `// 0x..` 逐格对上）
        // ==================================================================

        /// <summary>`+0x00`。本帧的指针位置（**屏幕像素、原点左下**，与 legacy `Input.mousePosition` 同口径）。</summary>
        public static Vector2 TouchPosition { get; private set; }
        /// <summary>`+0x08`。= legacy `Input.GetMouseButton(0)`（左键**按着**）。</summary>
        public static bool TouchPressed { get; private set; }
        /// <summary>`+0x09`。= legacy `Input.GetMouseButton(1)`（右键**按着**）—— `CombatCameraZoom` 用它判「右键拖拽」。</summary>
        public static bool TouchPressedSecondary { get; private set; }
        /// <summary>`+0x0C`。本帧的指针位移（**像素**）。桌面 = `TouchPosition − 上一帧`；移动端 = `Touch.deltaPosition`。</summary>
        public static Vector2 TouchDragDelta { get; private set; }
        /// <summary>`+0x14`。本帧有没有位移（`0 &lt; |TouchDragDelta|`）。</summary>
        public static bool IsDragging { get; private set; }
        /// <summary>`+0x18`。`TouchDragDelta` 的**视口**版（各轴 ÷ `Screen.width` / `Screen.height`，
        /// 分母那个 `1.0` = `DAT_1834b2bb8` ⇒ 屏宽 = 1）。`AttackTypesButtonsController.MoveButtons` 读它。</summary>
        public static Vector2 TouchDragDeltaViewport { get; private set; }
        /// <summary>`+0x20`。滚轮这一帧的读数 —— **legacy 量纲**（Windows 一格 = ±1；原版取的是 `mouseScrollDelta.y`，
        /// 见文件头 ⑥）。新输入系统那一格是 ±120 ⇒ 除 <see cref="ScrollUnitsPerNotch"/>（见文件头 A）。</summary>
        public static float ScrollDelta { get; private set; }
        /// <summary>`+0x24`。两指中点（屏像素）—— 双指时它同时是 `TouchPosition` 与 `UpdateDrag` 的输入。</summary>
        public static Vector2 TwoFingerMidPoint { get; private set; }
        /// <summary>`+0x30`。单例（`Awake` 里立的那个）。</summary>
        public static TouchInputManager Instance { get; private set; }

        // ==================================================================
        //  原版的方法（逐句；出处见文件头 ①）
        // ==================================================================

        /// <summary>= 原版 `Awake()`（`TouchInputManager__Awake.c` 逐句）：
        /// <code>
        /// if (Instance != null &amp;&amp; Instance != this) { Destroy(gameObject); return; }   // 已有单例 ⇒ 自杀
        /// Instance = this;
        /// </code>
        /// ⚠️ **原版用的是 `Object.Destroy`**（下一帧才真没），而本仓已知「批处理下没有帧循环 ⇒ `Destroy` 不生效」
        /// （`CLAUDE.md` §三）。这里**照原版用 `Destroy`** —— 因为语义上**结果一样**：
        /// 两条路都不会写 `Instance`（自杀那一支提前 `return`），单例始终是**先来的那一个**。
        /// ⇒ 自检断的是「`Instance` 没被后来者抢走」，⛔ 不断「那个 GO 真没了」（那要帧循环）。</summary>
        void Awake() { Bootstrap(); }

        /// <summary>= `Awake()` 的**函数体**（`Awake` 与自检共用这一份）。
        /// <para>🔴 **为什么拆出来**：批处理下 `AddComponent` 跑不跑 `Awake` **本工程没有定论**
        /// （`Editor/BattleScene.cs` 那条 A321 订正记着「不跑」与「会跑」两档、分界未坐实）
        /// ⇒ 一律**显式补一次**（同族先例 = 那条订正的「一律显式补一次 `Build()`」）。
        /// ⛔ 不是第二条实现：`Awake` 就这一句转发。</para></summary>
        public void Bootstrap()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>= 原版 `Update()`。原版这个体**很长**，本类把它整段收进 <see cref="Tick"/>，
        /// `Update` 只转发一句 —— 理由与 <see cref="Tick"/> 的注释同（批处理没有帧循环，
        /// 自检要能显式推一帧；⛔ 不是「换一条实现」：两处跑的是**同一个体**）。</summary>
        void Update() { Tick(); }

        /// <summary>= 原版 `Update()` 的**函数体本身**（`Update` 与自检共用这一份）。
        /// <para>每帧开头先把那 3 格清零（`TouchDragDelta` / `TouchDragDeltaViewport` / `IsDragging`），
        /// 再按「鼠标 → 触摸」两条路重填 —— 顺序逐句照原版（见文件头 ⑤ 的调用序列）。</para></summary>
        public void Tick()
        {
            RawInput raw = RawOverride ?? ReadRaw();

            // ---- ① 原版第一句：`TouchPressedSecondary = false` ----
            //  ⚠️ **它紧接着就被下面第 ⑦ 句覆盖掉**（`= GetMouseButton(1)`）—— 原版就是这样，
            //     照抄，⛔ 别「顺手」删掉（删了行为不变，但对不上原版；保留它也**不是**副作用）。
            TouchPressedSecondary = false;

            // ---- ② `ScrollDelta = Input.mouseScrollDelta.y`（**`.y`**，见文件头 ⑥）----
            ScrollDelta = raw.Scroll.y;

            // ---- ③④ `TouchDragDelta` / `TouchDragDeltaViewport` 先清零 ----
            //  （`.c` 里那两处 `***(undefined4**)(Vector2类 + 0xB8)` 就是 `Vector2.zero` 的两个分量 ——
            //    `Vector2` 的静态块 `+0x0` = `zero`、`+0x8` = `one`，与 `CombatCameraZoom` 文件头 ③ 同一条判据。）
            TouchDragDelta = Vector2.zero;
            TouchDragDeltaViewport = Vector2.zero;

            // ---- ⑤ `IsDragging = false` ----
            IsDragging = false;

            // ---- ⑥⑦ `TouchPressed` / `TouchPressedSecondary` ----
            TouchPressed = raw.LeftHeld;
            TouchPressedSecondary = raw.RightHeld;

            // ---- ⑧ `TouchPosition = Input.mousePosition` ----
            TouchPosition = raw.PointerPos;

            // ---- ⑨ 差分基准重置：鼠标**刚按下**、或**某一根手指刚** `Began` ⇒ `lastTouchPosition = TouchPosition` ----
            //  原版那句是一个 `goto` 汇合点（两条路都跳到同一个赋值），照抄。
            bool resetBase = raw.LeftDown || raw.RightDown;
            if (!resetBase && raw.TouchCount > 0 && raw.Touch0Phase == PhaseBegan)
                resetBase = true;
            if (resetBase) lastTouchPosition = TouchPosition;

            // ---- ⑩ 桌面路：`if (!forceMobileInput && !Application.isMobilePlatform && (TouchPressed || TouchPressedSecondary))`
            //                `UpdateDrag(TouchPosition);` ----
            //  ⚠️ 两层门**都要**（原版是先判 `forceMobileInput`、再判 `isMobilePlatform`）：
            //     `forceMobileInput` 为真时**整段桌面路被跳过**（改走下面的触摸路），不是「两条都走」。
            if (!forceMobileInput && !MobilePlatform)
            {
                if (TouchPressed || TouchPressedSecondary) UpdateDrag(TouchPosition);
            }

            // ---- ⑪ 触摸路：`if (Application.isMobilePlatform || forceMobileInput)` ----
            if (MobilePlatform || forceMobileInput)
            {
                if (raw.TouchCount == 1)
                {
                    // 单指（原版逐句）：
                    //   if (t.phase == Began) lastTouchPosition = t.position;
                    //   TouchPressed = true;
                    //   TouchPosition = t.position;
                    //   TouchDragDelta = t.deltaPosition;                       // ← 见文件头 ④
                    //   TouchDragDeltaViewport = (delta.x / Screen.width, delta.y / Screen.height);
                    //   lastTouchPosition = t.position;                          // ← 又来一次（原版就写了两遍）
                    if (raw.Touch0Phase == PhaseBegan) lastTouchPosition = raw.Touch0Pos;
                    TouchPressed = true;
                    TouchPosition = raw.Touch0Pos;
                    TouchDragDelta = raw.Touch0Delta;
                    TouchDragDeltaViewport = new Vector2(TouchDragDelta.x / Screen.width,
                                                         TouchDragDelta.y / Screen.height);
                    lastTouchPosition = raw.Touch0Pos;
                }
                else if (raw.TouchCount == 2)
                {
                    // 双指（原版逐句）：中点 → 距离 → 距离差进 `ScrollDelta`（**取负**）→ 中点当位置/拖拽输入。
                    TouchPressedSecondary = true;
                    Vector2 p0 = raw.Touch0Pos, p1 = raw.Touch1Pos;
                    TwoFingerMidPoint = new Vector2((p0.x + p1.x) * MidPointScale,
                                                    (p0.y + p1.y) * MidPointScale);

                    float dist = Mathf.Sqrt((p0.x - p1.x) * (p0.x - p1.x) + (p0.y - p1.y) * (p0.y - p1.y));

                    // 🔴 那两句在 `.c` 里长这样：`FUN_180789ad0(dist − lastTwoFingerDistance, 0);`
                    //    `FUN_180789ad0(ScrollDelta × DAT_1834b2bc8, 0);` —— **看着像「调了个 void、结果丢了」，
                    //    其实是 `set_ScrollDelta`**（文件头 ③）⇒ 正确语义是下面这两句赋值。
                    //    `DAT_1834b2bc8` = **−1.0** ⇒ 两指张开（距离变大）⇒ `ScrollDelta` 为**负** ⇒ 配合
                    //    `CombatCameraZoom.HandleManualControl` 的 `ZoomChange(scroll × sensitivity)` = **放大**。
                    if (0f < lastTwoFingerDistance)
                    {
                        ScrollDelta = dist - lastTwoFingerDistance;
                        ScrollDelta = ScrollDelta * PinchSign;
                    }
                    lastTwoFingerDistance = dist;

                    // 任一根手指刚要落 ⇒ 基准重置成**中点**（原版那两句是
                    // `FUN_180789a80()` = `get_TwoFingerMidPoint()`，见文件头 ③ ⇒ 不是 `Touch1Pos`）
                    if (raw.Touch0Phase == PhaseBegan || raw.Touch1Phase == PhaseBegan)
                        lastTouchPosition = TwoFingerMidPoint;

                    TouchPosition = TwoFingerMidPoint;
                    UpdateDrag(TwoFingerMidPoint);
                }
                else
                {
                    lastTwoFingerDistance = 0f;
                }
            }
        }

        /// <summary>= 原版 `UpdateDrag(Vector2 currentPos)`（逐句）：
        /// <code>
        /// TouchDragDelta = currentPos − lastTouchPosition;                       // 像素
        /// IsDragging     = 0 &lt; |TouchDragDelta|;                                 // 平方和开方
        /// TouchDragDeltaViewport = (delta.x / Screen.width, delta.y / Screen.height);   // 分母 1.0 那格
        /// lastTouchPosition = currentPos;
        /// </code></summary>
        void UpdateDrag(Vector2 currentPos)
        {
            TouchDragDelta = new Vector2(currentPos.x - lastTouchPosition.x,
                                         currentPos.y - lastTouchPosition.y);

            float sq = TouchDragDelta.x * TouchDragDelta.x + TouchDragDelta.y * TouchDragDelta.y;
            // 原版这里是 `if (d < 0) Nan()`（负数开方保护）后 `sqrt` —— `Mathf.Sqrt` 同语义。
            IsDragging = 0f < Mathf.Sqrt(sq);

            TouchDragDeltaViewport = new Vector2(TouchDragDelta.x / Screen.width,
                                                 TouchDragDelta.y / Screen.height);

            lastTouchPosition = currentPos;
        }

        /// <summary>= 原版 `Toggle(bool option)`（逐句一行）：`Behaviour.set_enabled(option)`。
        /// <para>🔴 **它的用途是「停摆」不是「清零」**（见文件头 E）：`BattleSettingsWindow.Open` 调 `Toggle(false)`
        /// 让 `Update` 停掉 ⇒ 那 8 个静态格**冻在上一帧的值上**（谁读谁拿到旧值）；
        /// `CloseWindow` / `ESCPressed` 调 `Toggle(true)` 还回来。
        /// `BattleManager.ChoiceOfCardPlayer` / `StopDisplayCardDescription` 调的时候传的是 **`true`**。</para></summary>
        public void Toggle(bool option) { enabled = option; }

        // ==================================================================
        //  🆕 等价物 B：惰性自建（原版是场景里的 GO 712，见文件头 B）
        // ==================================================================

        /// <summary>保证有一份实例（幂等）。原版那件组件摆在战场场景里（`MonoBehaviour_4011.json`，GO `712`）；
        /// 我们的战场是自己搭的树 ⇒ 由第一个消费者把它建出来，之后一路复用同一个。
        /// <para>⚠️ `Instance` 是**静态**的 ⇒ 一旦那个 GO 被销毁（换场景），`Instance != null` 那个判据
        /// 会因为 Unity 的「假 null」求值成 `false` ⇒ 这里会**再建一份**（正是我们想要的）。</para></summary>
        public static TouchInputManager Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("TouchInputManager");
            var t = go.AddComponent<TouchInputManager>();
            // ⚠️ 批处理下 `AddComponent` 跑不跑 `Awake` **本工程没有定论**（同 `CombatAutoZoom.AttachMinionEvent`
            //    那条注释）⇒ 显式补一次；⛔ 不赌生命周期。
            if (Instance == null) t.Bootstrap();
            return Instance;
        }

        /// <summary>单例的读口（`Instance` 是 `private set` 的静态属性，自检读不到「上一份是谁」时用它对比）。</summary>
        public static TouchInputManager Current { get { return Instance; } }

        // ==================================================================
        //  等价物 A：原始读数（legacy `Input` 不可用 ⇒ 走新输入系统，见文件头 A）
        // ==================================================================

        /// <summary>Windows 上一个滚轮刻度 = **120**（同源口径 = `Shell/MenuScroll.cs:200`；
        /// `CombatCameraZoom.ScrollUnitsPerNotch` 是同一个数的**另一处**用途 —— 那件组件现在改从本类读，
        /// 所以口径只此一处：**这里**）。</summary>
        public const float ScrollUnitsPerNotch = 120f;

        /// <summary>`TouchPhase.Began` 的枚举值（legacy)。原版两处判 `phase == 0`。</summary>
        const int PhaseBegan = 0;
        /// <summary>`DAT_1834b2bb4` = **0.5**（两指中点那一乘）。</summary>
        const float MidPointScale = 0.5f;
        /// <summary>`DAT_1834b2bc8` = **−1.0**（捏合距离差取负）。</summary>
        const float PinchSign = -1f;

        /// <summary>一帧的原始读数（= legacy `Input` 那几格；新输入系统读出来的东西先落进这里，
        /// `Tick` 只认这个结构 ⇒ 「哪一格来自哪个 API」只有 <see cref="ReadRaw"/> 一处判据）。</summary>
        public struct RawInput
        {
            public Vector2 PointerPos;          // ≡ Input.mousePosition
            public Vector2 Scroll;              // ≡ Input.mouseScrollDelta（**已换成 legacy 量纲**：一格 ±1）
            public bool LeftHeld, RightHeld;    // ≡ Input.GetMouseButton(0) / (1)
            public bool LeftDown, RightDown;    // ≡ Input.GetMouseButtonDown(0) / (1)
            public int TouchCount;              // ≡ Input.touchCount
            public Vector2 Touch0Pos, Touch0Delta; public int Touch0Phase;   // ≡ Input.GetTouch(0)
            public Vector2 Touch1Pos; public int Touch1Phase;                // ≡ Input.GetTouch(1)
        }

        /// <summary>🆕 **自检口**：把这一帧的原始读数钉成给定的（`null` = 读真的）。
        /// <para>存在的理由：原版这些读数由真鼠标/真触摸来，**批处理里两个都没有** ⇒ 不钉住的话
        /// `Update` 的每一条分支在自检里都走不到。**这不是「换一条实现」** —— `Tick` 只认这个结构，
        /// 钉住的是**输入**、不是**逻辑**。</para></summary>
        public static RawInput? RawOverride;

        /// <summary>🆕 **自检口**：把 `Application.isMobilePlatform` 钉成给定答案（`null` = 用真的）。
        /// 桌面编辑器里它恒 `false` ⇒ 不钉住的话触摸那两路（`touchCount == 1` / `== 2`）**一条都验不了**。</summary>
        public static bool? MobilePlatformOverride;

        static bool MobilePlatform
        {
            get { return MobilePlatformOverride.HasValue ? MobilePlatformOverride.Value : Application.isMobilePlatform; }
        }

        /// <summary>= 原版读的那几个 legacy `Input` API 在我们这一档的等价物（见文件头 A）。
        /// ⛔ 只有这一处知道新输入系统的名字 —— 别在别处再读一遍鼠标。</summary>
        RawInput ReadRaw()
        {
            var r = new RawInput();
            var m = UnityEngine.InputSystem.Mouse.current;
            if (m != null)
            {
                r.PointerPos = m.position.ReadValue();                 // 屏幕像素、原点左下（与 legacy 同）
                r.Scroll = new Vector2(0f, m.scroll.ReadValue().y / ScrollUnitsPerNotch);   // 一格 ±120 ⇒ ±1
                r.LeftHeld = m.leftButton.isPressed;
                r.RightHeld = m.rightButton.isPressed;
                r.LeftDown = m.leftButton.wasPressedThisFrame;
                r.RightDown = m.rightButton.wasPressedThisFrame;
            }
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null)
            {
                // legacy `Input.touchCount` = 「当前活跃的手指数」；新输入系统里 `touches` 是**定长 10 个**
                // ⇒ 等价物 = 数 `press.isPressed` 的那几个。
                var pos = new Vector2[2]; var delta = new Vector2[2]; var phase = new int[2];
                int n = 0;
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed) continue;
                    if (n < 2)
                    {
                        pos[n] = t.position.ReadValue();
                        delta[n] = t.delta.ReadValue();
                        // 🔴 **两套 `TouchPhase` 的枚举值【不同序】**，必须映射：
                        //    legacy `UnityEngine.TouchPhase` = Began **0** / Moved 1 / Stationary 2 / Ended 3 / Canceled 4；
                        //    新输入系统 `UnityEngine.InputSystem.TouchPhase` = None 0 / Began **1** / Moved 2 / Ended 3 /
                        //    Canceled 4 / Stationary 5。原版判的是 `== 0`（Began）⇒ 直接强转会把 `None` 当 `Began`
                        //    （而 `None` 永远不该出现在「活跃手指」上）⇒ 这里显式映射，`RawInput` 那几格才有资格叫「≡ legacy」。
                        phase[n] = t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began ? PhaseBegan : 1;
                    }
                    n++;
                }
                r.TouchCount = n;
                if (n >= 1) { r.Touch0Pos = pos[0]; r.Touch0Delta = delta[0]; r.Touch0Phase = phase[0]; }
                if (n >= 2) { r.Touch1Pos = pos[1]; r.Touch1Phase = phase[1]; }
            }
            return r;
        }

        // ==================================================================
        //  等价物 C：自检口（⛔ 生产路径一律不碰）
        // ==================================================================

        /// <summary>🆕 自检口：直接跑 <see cref="UpdateDrag"/>（那个方法原版是 `private`）。
        /// 走的是**同一个体**，⛔ 不是复制一份算式。</summary>
        public void UpdateDragForTest(Vector2 currentPos) { UpdateDrag(currentPos); }

        /// <summary>🆕 自检口：钉住「上一帧指针在哪」（`+0x24`）—— 不钉住的话 `UpdateDrag` 的差分基准
        /// 跟着真鼠标走，期望值算不出来。</summary>
        public Vector2 LastTouchPositionForTest
        {
            get { return lastTouchPosition; }
            set { lastTouchPosition = value; }
        }

        /// <summary>🆕 自检口：`+0x2C` 的读 / 写（捏合的差分基准；`0` 那一档**不产 `ScrollDelta`**）。</summary>
        public float LastTwoFingerDistanceForTest
        {
            get { return lastTwoFingerDistance; }
            set { lastTwoFingerDistance = value; }
        }

        /// <summary>🆕 自检口：`+0x20 forceMobileInput` 的读写（原版是 `[SerializeField] private`，自检改不到）。</summary>
        public bool ForceMobileInputForTest
        {
            get { return forceMobileInput; }
            set { forceMobileInput = value; }
        }

        /// <summary>🆕 自检口：把 8 个静态格放回出厂（全零 / 全 false）。
        /// ⚠️ **只在自检里用** —— 静态格的「出厂」= C# 的零初始化，原版没有任何一处主动清零它们。</summary>
        public static void ResetStaticsForTest()
        {
            TouchPosition = Vector2.zero;
            TouchPressed = false;
            TouchPressedSecondary = false;
            TouchDragDelta = Vector2.zero;
            IsDragging = false;
            TouchDragDeltaViewport = Vector2.zero;
            ScrollDelta = 0f;
            TwoFingerMidPoint = Vector2.zero;
            Instance = null;
        }
    }
}
