// CombatCameraZoom.cs — 原版 `CombatCameraZoom`（`dump.cs` TypeDefIndex 670）**整件**（A422，2026-10-12）
//
// ============================ 这件是干什么的 ============================
// 原版战场相机那三件是一组：
//   · `CameraVerticalFramer`（TypeDefIndex 668）—— 按 zoom 算「传感器尺寸 + 透镜位移」（数学）
//   · `CombatAutoZoom`（669）—— 按场上人数定 zoom 目标（自动）
//   · **`CombatCameraZoom`（670）—— 每帧把 zoom/位移/边界落到相机上，并吃手动输入**（本件）
// 判据链：`BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c:229` 先 `CombatCameraZoom.Initialize(allowManual)`
//   再 `CombatAutoZoom.Initialize()`（两句**挨着**，教学那条 `_TutorialStartSequence_d__592__MoveNext.c:145` 同）；
//   `CombatAutoZoom.SetZoomLevel` 把值写给 `combatCameraZoom.TargetZoomLevel`（setter）；
//   `BattleCameraSreenSize.Initialize` 只读它一格 `GetMaxZoomLevel(1.0f)`（`BattleCameraSreenSize__Initialize.c:25`）。
//
// ============================ 判据（全是实读，没有一个是推的） ============================
// ① **方法体（第一权威）** = `d:/2/tools/decomp_full/CombatCameraZoom__{ctor, Awake, OnDestroy, LateUpdate,
//    ToggleAllowManualControl, Initialize, SetZoomLevel, GetMaxZoomLevel, DetectPlayerInput,
//    ToggleManualCameraControl, HandleManualControl, HandleAutomaticControl, ZoomChange, HandleDrag,
//    GetWorldPositionUnderMouse, GetWorldPositionViewportSpace, ApplyZoom, ClampShiftLimits, CorrectLensShift,
//    GetCameraFrustumWorldBoundsWithShift, OnCameraShiftChanged, OnCameraSensorSizeChanged, OnDrawGizmos,
//    get_TargetZoomLevel, set_TargetZoomLevel, get_ZoomSensitivity}.c`
// ② **字段名 / 偏移 / 出厂值（ctor）** = `d:/2/tools/il2cpp_out/dump.cs` 的 `CombatCameraZoom`（`// 0x..` 那几行）
//    + `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CombatCameraZoom.cs`（签名桩）
//    + `CombatCameraZoom___ctor.c`（`0x3dcccccd`=0.1 · `0x3ba3d70a`=0.005 · `0x40a00000`=5.0 · `+0x80`=1）
// ③ 🔴 **反编译器认错的操作数**（本件**逐条按 VA 读指令流**取回，别照 `.c` 抄）：
//    · `ApplyZoom` VA `0x18060CF60`：
//      - `0x18060D00F` 那两处平滑 —— `Mathf.SmoothDamp(current, target, ref vel, **Time.deltaTime × zoomSpeed**,
//        maxSpeed = `[xmm15]` = `DAT_1834b2ea0` = **+∞**, deltaTime)`（位置 3 的浮点在 **xmm3**、
//        4/5 两个在**栈** `[rsp+0x20]/[rsp+0x28]` —— Microsoft x64 的**位置制**，`ref` 占了 r8）。
//      - `0x18060D2EC/0x18060D314` 同理是 `Vector2.SmoothDamp(vcam.m_Lens.LensShift, targetLensShift,
//        ref currentLensShiftVelocity, Time.deltaTime × zoomSpeed, +∞, Time.deltaTime)`。
//      - 🔴🔴 `0x18060D22A/D232`：那句「贴近就吸附」**Ghidra 读反了** —— `.c` 写的是
//        `targetLensShift = vcamLensShift − desiredLensShift`，**指令流里是 `[rbx+0x88]=xmm3 / [rbx+0x8c]=xmm4`，
//        而 xmm3/xmm4 = `desiredLensShift`** ⇒ 正确语义 = 「|vcam − desired|² < 1e-7 就 **`targetLensShift = desiredLensShift`**」。
//        （照 `.c` 抄会让镜头每帧被置成「差值」≈ 0 —— 一个**静默**的画面错误，本件踩过它的影子。）
//      - `0x18060D42D-D4B8` 尾段：`referenceA`（`[rsp+0x118]`）− `referenceB`（`[rsp+0x100]`），
//        **两者同除 `targetCamera.sensorSize`**（`0x18060D481` 除 `.x`、`0x18060D492` 除 `.y`，
//        两次 `get_sensorSize` 之间没有任何写 ⇒ 同一个值）⇒ 加上平滑值 → `ClampShiftLimits` → 写 vcam。
//    · `ClampShiftLimits` VA `0x18060D720`：尾段 `0x18060D8ED-D91E` 证明它**就是调 `CorrectLensShift`**
//      （`desired + correction / ((sensorSize × |camPos.z|) / focalLength)`，x 用 `.x`、y 用 `.y`）。
//      两个 `if` **不是 `else if`**（`0x18060D7A2` 与 `0x18060D7C2` 两条独立分支，后者可覆盖前者）。
//    · `GetWorldPositionUnderMouse` VA `0x18060E0B0` / `GetWorldPositionViewportSpace` VA `0x18060E280`：
//      `.c` 里那两个「`Vector2.one`」实为 **`Vector2.zero` 与 `Vector2.one`** 这两个静态字段
//      （`Vector2` 类静态块 `+0x0` = `zero`、`+0x8` = `one`，`0x18060E4A3` 读的是 `[rcx+4]`/`[rcx+0]`）；
//      那一句是 `vp − one × 0.5`（`DAT_1834b2bb4` = **0.5**），不是「zero × 0.5」。
//    · `HandleDrag` VA `0x18060E440`：`0x18060E4F8/0x18060E500` 两条 rip 相对常量**同一个地址**
//      `0x1834B2BC8` = **−1.0**（不是 +1）⇒ 拖拽方向是**反**的：`TouchDragDelta × dragSensitivity × (−1f)`。
//    · `GetCameraFrustumWorldBoundsWithShift` VA `0x18060DB00`：`0x18060DB00+…` 里那个「静态 `Vector3` +0x48」
//      = **`Vector3.forward`**（`zero@0` `one@0xC` `up@0x18` `down@0x24` `left@0x30` `right@0x3C` `forward@0x48`）
//      ⇒ 那一跳是 `Mathf.Abs(Vector3.Dot(forward, Vector3.forward)) < DAT_1834b2db8(=1e-06)` ⇒ 打警告 + 返回**空 Rect**；
//      接着 `t = (0 − camPos.z) / forward.z`。
// ④ **常量（`.rdata` 直读，`工具/read_literal.py`；4 字节 float）**：
//    `0x1834b2bb8` = **1.0**（zoom 上界 / `Clamp01` 上界 · 同一格）· `0x1834b2bb4` = **0.5** ·
//    `0x1834b2da8` = **10.0**（`originalLensShift` 每秒追多少）· `0x1834b2ea0` = **+∞**（`SmoothDamp` 的 `maxSpeed`）·
//    `0x1834b2bc8` = **−1.0** · `0x1834b2c80` = **−0.0**（= `0x80000000`，取符号位翻转 = 求负）·
//    `0x1834b2e60` = `0x7FFFFFFF`（float 绝对值的**掩码**）· `0x1834b2e08` = **100.0**（原版场地根 x）·
//    `0x1834b3174` = **1e-07**（`|vcam−desired|²` 的吸附阈值）· `0x1834b2ba8` = **1e-10**（边界修正/缓存重抓的阈值）·
//    `0x1834b2db8` = **1e-06**（相机的 forward 与 `Vector3.forward` 的夹角判断）。
// ⑤ **序列化值 = 13/13 个战场逐字节相同**（只差对象引用）：`worldBounds = (89.95, −8.0, 20.115, 15.75)` ·
//    `zoomSensitivity 0.1` · `zoomSensitivityMobile 0.005` · **`zoomSpeed 12.5`** · **`invertMouseScrollWheel 1`** ·
//    **`dragSensitivity 0.01`** · `snapBackSpeed 5.0` · `manualCamera 0`。
//    🔴 **三处 ctor 默认 ≠ 场景值**（铁律 5·c「一个值 ≠ 全部情况」）：`zoomSpeed`（ctor 0.1 / 场景 12.5）·
//    `invertMouseScrollWheel`（ctor false / 场景 **true**）· `dragSensitivity`（ctor 0.005 / 场景 0.01）。
//    **我们取场景值**（那才是玩家在实况里遇到的），并在字段上把两档都写出来。
//    判据 = `assets_full/bundle_scenes_scenes_battlearena*/MonoBehaviour/MonoBehaviour_4404.json`（arena1；
//    其余 12 场各自的副本逐值相同，2026-10-12 逐份亲读）。
// ⑥ **`virtualCamera` 那一格**：原版是 `CinemachineVirtualCamera`（`+0x28`），而本文件只碰它的
//    **一格** = `m_Lens.LensShift`（`m_Lens` 在 `CinemachineVirtualCamera+0xB8`，`LensSettings.LensShift` 在
//    struct 内 `+0x18` ⇒ 对象偏移 **`+0xD0`/`+0xD4`**，`dump.cs` 的 `CinemachineVirtualCamera`/`LensSettings` 实读）。
//    我们**没有 Cinemachine** ⇒ 这一格落成 <see cref="virtualCameraLensShift"/>，
//    并把 Cinemachine 每帧「把 `m_Lens` 推给真相机」的那一下显式写成 <see cref="ApplyVirtualCameraLensShift"/>。
//    判据（原版那一格的初始值）= 同场景 `MonoBehaviour_4328.json` 的 `m_Lens.LensShift = (0.0, −0.205)`。
// ⑦ **输入源**：原版读的是另一件组件的**静态属性** `TouchInputManager.{ScrollDelta, TouchPressedSecondary,
//    TouchDragDelta}`（`dump.cs` TypeDefIndex 2299；静态字段 `+0x20` / `+0x9` / `+0xC`，逐处实读）。
//    🔴 **我们也没有那件组件** ⇒ 见 <see cref="PollPointerSource"/>：那三格的等价物从**新输入系统**现取
//    （`ProjectSettings.asset:932 activeInputHandler: 1` ⇒ **只有** Input System 包，legacy `UnityEngine.Input` 不可用）。
//
// ============================ 🔴 我们做的三处「等价物」（⛔ 不当原版） ============================
// A. **`settle` 那一路（<see cref="SettleFraming"/>）**：原版靠 `LateUpdate` 每帧收敛；**批处理里没有帧循环**
//    ⇒ 自检与「换牌结束那一刻」都收不到那个收敛。settle = 把 `currentZoomLevel` 直接置成 `targetZoomLevel`
//    （**这正是原版 `CombatCameraZoom.Initialize` 末尾那一手的做法**）再走**同一条** `ApplyZoomAt` 的落点，
//    并把两处插值当成「一步到位」。**生产路径（`LateUpdate`）不传 settle = 逐句原版**。
// B. **指针闸门换了源**：原版 `EventSystemController.Instance.IsPointerOverUIObject(fingerId)` +
//    `EventSystem.current.m_HasFocus`(+0x48)。🔴 **本仓没有 UGUI `EventSystem` / `EventSystemController`**
//    （`Editor/ShellScene.cs:1884` 记过），`m_HasFocus` 的等价物是 **`Application.isFocused`**（那一格就是
//    `EventSystem.OnApplicationFocus` 写的）；「指针在不在 UI 上」**没有等价物** ⇒ 见 <see cref="PointerOverUi"/>（**待接线**）。
// C. **双指那一路（移动端）**：原版 `Input.GetTouch(0)`；我们这台是桌面 ⇒ 那一段**照原版写出来**，
//    但**没有真机验过**（如实记，不进断言）。滚轮/右键拖拽那两条是桌面主路。
//
// ============================ 🔴 我们没做 / 触发源缺的（如实，⛔ 不冒充原版） ============================
// D. **`BattleCameraSreenSize`（TypeDefIndex 493）整件没做** —— `CombatCameraZoom.Awake`/`OnDestroy` 的**全部内容**
//    就是往它那两个 `public Action<Vector2>`（`+0x58 OnCameraSensorSizeChanged` / `+0x50 OnCameraShiftChanged`）
//    上 `+=` / `-=`。那件组件没做 ⇒ 我们把这**一对事件挪到本组件上**
//    （<see cref="OnCameraSensorSizeChangedSource"/> / <see cref="OnCameraShiftChangedSource"/>），
//    `Awake`/`OnDestroy` 的 `+=`/`-=` **机制照原版**，但**没有生产者**（触发源缺）。
//    ⛔ 别读成「没关系」：`targetOriginalLensShift`（镜头平移的**基准**）只有 `OnCameraShiftChanged` 会写。
//    （那件组件的序列化值同 ⑥：`sensorSizeXSmall 37.0` / `sensorSizeXBigScreen 41.0` / `animTime 3.0`。）
// E. ✅ **`BattleHud` 那颗「重置自动镜头」钮 —— 2026-10-12（A423）做了**（原版 `resetCameraZoomButton` `+0xa8`）。
//    ⚠️ **2026-10-12 更正**：这里原来写「全仓 `ResetCamera` 0 命中 ⇒ 我们 HUD 里没有那颗钮」——
//    那半句说的是**链**不在（驱动里既没引用也没点击判定），**不是「图不在」**：那张图与它的位置
//    （`CenterCameraButton`，`40k_UI_bt_center_camera`，rect 64.44×61.85 @17.9,568.2）`BuildHud` 里一直有。
//    现在 `LateUpdate` / `SetZoomLevel(force)` / `ToggleManualCameraControl` 里那三句
//    `BattleHud.Instance.ToggleResetAutoCameraZoom(...)` 真的落到 **`BattleDriver.ToggleCameraResetButton`** 上
//    （经 <see cref="ToggleResetCameraZoomUi"/> 转发），点击走 `BattleDriver.ResetCameraZoomClick`。
//    ⛔ 钩子为空那一支仍留（「不在战斗里」那一档才走到），它会**出声一次**。
// F. **死代码照留**：`DetectPlayerInput` / `ZoomChange` / `ToggleManualCameraControl` / `HandleAutomaticControl`
//    在全反编译里**一个调用点都没有**（它们的函数体被内联进了 `LateUpdate` / `HandleManualControl`）。
//    照原版留成方法 + 标注「本 build 无调用点」，⛔ **不删**（删了就再也对不上原版）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 战场相机的**手动 + 每帧落点**（原版 `CombatCameraZoom`）：滚轮/双指缩放、右键拖拽平移、
    /// 世界边界夹取、`SmoothDamp` 平滑、贴近回弹，最后把 zoom 与透镜位移写进相机。
    /// <para>自动那一半（按人数定 zoom）在 <see cref="CombatAutoZoom"/>；取景曲线也在那边（= 我们的
    /// `CameraVerticalFramer`）。本件只管「把它算出来的东西每帧落到相机上」，外加玩家的手动输入。</para>
    /// </summary>
    public class CombatCameraZoom : MonoBehaviour
    {
        // ==================================================================
        //  原版序列化字段（偏移 / 出厂值 / 场景值都有出处，见文件头 ②⑤）
        // ==================================================================

        /// <summary>原版 `+0x20`。场景里指的是 `BoardCamera`（`Camera_1461`，透视 · `focalLength 28` ·
        /// `m_SensorSize (41.5, 24.0)` · `m_GateFitMode 2`(Horizontal)）。</summary>
        public Camera targetCamera;

        /// <summary>🔴 原版 `+0x28` 是 `CinemachineVirtualCamera`，而我们**没有 Cinemachine**（见文件头 ⑥）：
        /// 只保留它被真正读到的那一格 —— `m_Lens.LensShift`（对象偏移 `+0xD0`/`+0xD4`）。
        /// <para>语义 = 「虚拟镜头**当前**的位移」，`ApplyZoom` 从头到尾读它/写它；写完之后由
        /// <see cref="ApplyVirtualCameraLensShift"/> 推给真相机（原版那一下在 Cinemachine 的 pipeline 里）。</para></summary>
        public Vector2 virtualCameraLensShift;

        /// <summary>= 原版 `+0x30 manualCamera`：真 = 玩家手动控着镜头，自动缩放**不碰**它。
        /// ⚠️ 原版是 **private 字段**；这里照旧 private，读写口是 <see cref="ManualCamera"/>（给 `CombatAutoZoom` 用）。</summary>
        [SerializeField] bool manualCamera;

        /// <summary>= 原版 `+0x38 [SerializeField] BattleCameraSreenSize battleCameraScreenSize`。
        /// 🔴 **我们那件组件没做**（文件头 D）⇒ 保留字段只为对齐原版的字段表，**代码里一个字节都不读**。
        /// 它那两个 `Action<Vector2>` 挪到了 <see cref="OnCameraSensorSizeChangedSource"/> /
        /// <see cref="OnCameraShiftChangedSource"/>。</summary>
        [System.NonSerialized] public MonoBehaviour battleCameraScreenSize;

        // ---- Zoom config ----

        /// <summary>原版 `+0x40`。ctor `0x3dcccccd` = **0.1**；场景 `0.10000000149011612`（同一个数）。</summary>
        [SerializeField] float zoomSensitivity = 0.1f;

        /// <summary>原版 `+0x44`。ctor `0x3ba3d70a` = **0.005**；场景 `0.004999999888241291`。</summary>
        [SerializeField] float zoomSensitivityMobile = 0.005f;

        /// <summary>原版 `+0x48`。🔴 **ctor 默认 0.1（`0x3dcccccd`），而 13/13 个战场里序列化的是 `12.5`**
        /// —— 我们取**场景值**（玩家实况那一档）。它是 `SmoothDamp` 的 **smoothTime 系数**
        /// （`smoothTime = Time.deltaTime × zoomSpeed`，见文件头 ③）⇒ 越大越慢。</summary>
        [SerializeField] float zoomSpeed = 12.5f;

        /// <summary>= 原版 `+0x50 [SerializeField] CameraVerticalFramer cameraVerticalFramer`。
        /// 🔴 我们**没有**那件组件：它的数学（三条曲线 + `sensorSize.x` 那半）全在 <see cref="CombatAutoZoom"/>，
        /// 判据收成一处 ⇒ 这一格指向 <see cref="CombatAutoZoom"/>（它公开
        /// <see cref="CombatAutoZoom.CalculateFraming"/>，就是原版 `CameraVerticalFramer.CalculateFraming` 的等价物）。</summary>
        [System.NonSerialized] public CombatAutoZoom framer;

        /// <summary>= 原版 `+0x58`（`maxZoomForSmallScreenDevicesByAspectRatio`，5 键，13/13 场逐字节相同）。
        /// ⚠️ 曲线数据不在这里再抄一份 —— 只有 `CombatAutoZoom.MaxZoomForSmallScreen(aspect)` 一处判据。
        /// 这一格留着 = 原版那个序列化槽位（自检钉「开关关着时它不参与」）。</summary>
        [SerializeField] AnimationCurve maxZoomForSmallScreenDevicesByAspectRatio;

        /// <summary>原版 `+0x60`。🔴 **ctor 默认 false（零初始化），场景里 13/13 都是 1** ⇒ 我们取 **true**。
        /// 作用：桌面平台上把滚轮读数**翻符号**（`0x18060E781 xorps xmm6, DAT_1834b2c80`，那个常量 = −0.0 = 符号位）。</summary>
        [SerializeField] bool invertMouseScrollWheel = true;

        // ---- Pan ----

        /// <summary>原版 `+0x64`。🔴 **ctor 默认 0.005（`0x3ba3d70a`），场景里 13/13 都是 `0.01`** ⇒ 我们取 0.01。</summary>
        [SerializeField] float dragSensitivity = 0.01f;

        /// <summary>原版 `+0x68`。ctor `0x40a00000` = **5.0**；场景 `5.0`。它就是「镜头位移每秒追多少比例」
        /// （`t = Clamp01(Time.deltaTime × snapBackSpeed)`，再 `Lerp`）。</summary>
        [SerializeField] float snapBackSpeed = 5f;

        // ---- World Bounds ----

        /// <summary>原版 `+0x6C`（`Rect`：`x`@0x6c · `y`@0x70 · `width`@0x74 · `height`@0x78）。
        /// 序列化值 **13/13 场逐字节相同** = `(89.95, −8.0, 20.115, 15.75)`，那是**原版的世界系**
        /// （原版场地根在 `x = 100`）。我们整体平移了 `−100`（`Editor/BattleScene.cs:10590 const ArenaOriginX = 100f`、
        /// 场地根 `localPosition = (−100, 0, 0)`、相机同减）⇒ **等价的 x = 89.95 − 100 = −10.05**；
        /// `y/width/height` 不受平移影响，逐值照抄。（平移只动 x ⇒ 边界盒与新世界系对得上。）</summary>
        [SerializeField] Rect worldBounds = new Rect(-10.05f, -8f, 20.115f, 15.75f);

        // ==================================================================
        //  原版私有状态（`dump.cs` 的 `// 0x..`）
        // ==================================================================

        /// <summary>`+0x7C`。零初始化（ctor 不写它）—— 开局那个值由 <see cref="Initialize"/> **反解**出来。</summary>
        float currentZoomLevel;

        /// <summary>`+0x80`。ctor 写 **1**（`CombatCameraZoom___ctor.c` 最后第二句）⇒ 出厂 = 允许手动控制。</summary>
        bool allowManualControl = true;

        /// <summary>`+0x84`。零初始化；`Initialize` 反解之后写它。</summary>
        float targetZoomLevel;

        /// <summary>`+0x88`。= 我们想达到的透镜位移。</summary>
        Vector2 targetLensShift;

        /// <summary>`+0x90`。= 「屏幕尺寸那一套」给的基准位移（本件按 `dt × 10` 追 <see cref="targetOriginalLensShift"/>）。</summary>
        Vector2 originalLensShift;

        /// <summary>`+0x98`。= 上面那个目标；**只有** `OnCameraShiftChanged` 会写它（= `BattleCameraSreenSize` 在动）。</summary>
        Vector2 targetOriginalLensShift;

        /// <summary>`+0xA0`。`Mathf.SmoothDamp` 的 `ref currentVelocity`。</summary>
        float currentZoomVelocity;

        /// <summary>`+0xA4`。`Vector2.SmoothDamp` 的 `ref currentVelocity`。</summary>
        Vector2 currentLensShiftVelocity;

        // ==================================================================
        //  🆕 我们补的挂钩 / 状态（⛔ 都有出处，见各自的注释）
        // ==================================================================

        /// <summary>= 原版 `BattleCameraSreenSize.OnCameraSensorSizeChanged`（那件组件的 `+0x58`）。
        /// 🔴 **我们那件组件没做**（文件头 D）⇒ 先挂在本组件上；`Awake`/`OnDestroy` 的 `+=`/`-=` **照原版**。
        /// 消费者 = <see cref="OnCameraSensorSizeChanged"/>（原版**空方法**，`RVA 0x4B33B0` 是个共享桩）⇒ 订了也不做事。</summary>
        public System.Action<Vector2> OnCameraSensorSizeChangedSource;

        /// <summary>= 原版 `BattleCameraSreenSize.OnCameraShiftChanged`（那件组件的 `+0x50`）。
        /// 消费者 = <see cref="OnCameraShiftChanged"/> ⇒ 写 `targetOriginalLensShift`（镜头平移的**基准**）。</summary>
        public System.Action<Vector2> OnCameraShiftChangedSource;

        /// <summary>抬 <see cref="OnCameraShiftChangedSource"/>（🔴 **我们加的**：原版抬它的是 `BattleCameraSreenSize`
        /// 那条 DOTween 的收尾回调 —— `BattleCameraSreenSize__Initialize.c` 里 `DG_Tweening_DOTween__To` 的
        /// `Action&lt;Vector2&gt;` 就是它。那件组件没做 ⇒ 留一个显式抬法给自检/将来那件组件，⛔ 别当成第二条生产链）。</summary>
        public void RaiseCameraShiftChanged(Vector2 newLensShift)
        {
            var h = OnCameraShiftChangedSource;
            if (h != null) h(newLensShift);
        }

        /// <summary>= 原版 `BattleHud.Instance.ToggleResetAutoCameraZoom(bool)` 的落点（三处调它：
        /// `LateUpdate` / `SetZoomLevel(force:true)` / `ToggleManualCameraControl`）。
        /// 🔴 **我们 HUD 里没有那颗钮**（文件头 E）⇒ 由 `CombatAutoZoom` 接上它那条「钩子或出声」。</summary>
        [System.NonSerialized] public System.Action<bool> ToggleResetCameraZoomUi;

        /// <summary>「指针现在压在我们自己的 UI 上吗」—— 原版这一问是
        /// `EventSystemController.Instance.IsPointerOverUIObject(fingerId)`（拿 UGUI 的 EventSystem 做射线）。
        /// 🔴 **本仓没有 UGUI `EventSystem` / `EventSystemController`**（`Editor/ShellScene.cs:1884` 记过；
        /// `ProjectSettings` 的 `activeInputHandler: 1` = 只用新输入系统）⇒ **由驱动层接一个**（**待接线**）。
        /// ⛔ 没人接时按 `false`（= 不在 UI 上）**并出声一次** —— 默认成 `true` 就等于「手动缩放永远不生效」，
        /// 那是**静默失败**（本仓红线）。</summary>
        [System.NonSerialized] public System.Func<bool> PointerOverUi;

        bool _pointerGateNoted;
        bool _cameraPlaneWarned;
        bool _screenSizeSourceNoted;
        bool _refsMissingNoted;

        // ==================================================================
        //  输入源：原版 `TouchInputManager` 那三格的等价物（见文件头 ⑦ / A）
        // ==================================================================

        /// <summary>Windows 上一个滚轮刻度 = **120**（`Shell/MenuScroll.cs:200` 那条同源口径：
        /// 「`Mouse.current.scroll.ReadValue().y`，Windows 上一格 ±120」）。
        /// 原版读的是 legacy `Input.mouseScrollDelta.y`（`TouchInputManager.Update` 里
        /// `0x18079BDDC movss xmm0,[rbp+0x124]` —— **.y**，不是 .x）＝ **一格 ±1** ⇒ 除 120 才是同一量纲。</summary>
        public const float ScrollUnitsPerNotch = 120f;

        /// <summary>`dt × 10.0` 那一格（`DAT_1834b2da8`，文件头 ④）。</summary>
        public const float OriginalLensShiftSpeed = 10f;

        float _scrollDelta;
        bool _rightButtonHeld;
        Vector2 _pointerDragDelta;
        Vector2 _lastPointerPosition;
        bool _pointerInited;

        /// <summary>= 原版 `TouchInputManager.Update` 里那三格（`ScrollDelta` / `TouchPressedSecondary` /
        /// `TouchDragDelta`）的等价物，**每帧重算**（原版也是每帧在第一句就把 `TouchDragDelta` 清零、
        /// 再在 `UpdateDrag` 里写）。
        /// <para>⚠️ 顺序：Unity 里组件 `Update` 跑在 `LateUpdate` **之前**，原版 `TouchInputManager` 是独立组件
        /// ⇒ 我们把这一跳放在 <see cref="Tick"/> 的**最前面**，等价。</para>
        /// <para>`TouchDragDelta` 只在「左手键或右键按着」时非零（原版 `Update` 的
        /// `if (TouchPressed == 0) { if (TouchPressedSecondary == 0) 跳过 UpdateDrag; }`）。</para></summary>
        public void PollPointerSource()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null)
            {
                _scrollDelta = 0f; _rightButtonHeld = false; _pointerDragDelta = Vector2.zero;
                return;
            }
            _scrollDelta = mouse.scroll.ReadValue().y / ScrollUnitsPerNotch;   // 原版取的是 .y
            _rightButtonHeld = mouse.rightButton.isPressed;                     // = Input.GetMouseButton(1)
            Vector2 pos = mouse.position.ReadValue();                           // 屏幕像素，原点左下（同 legacy）
            if (!_pointerInited) { _lastPointerPosition = pos; _pointerInited = true; }
            _pointerDragDelta = Vector2.zero;
            if (mouse.leftButton.isPressed || _rightButtonHeld)
                _pointerDragDelta = pos - _lastPointerPosition;
            _lastPointerPosition = pos;
        }

        /// <summary>= 原版 `TouchInputManager.get_ScrollDelta()`（静态 `+0x20`）。</summary>
        public float ScrollDelta { get { return _scrollDelta; } }
        /// <summary>= 原版 `TouchInputManager.TouchPressedSecondary`（静态 `+0x9`）= 右键按着。</summary>
        public bool TouchPressedSecondary { get { return _rightButtonHeld; } }
        /// <summary>= 原版 `TouchInputManager.TouchDragDelta`（静态 `+0xC`）= 指针的**像素**位移。</summary>
        public Vector2 TouchDragDelta { get { return _pointerDragDelta; } }

        /// <summary>🆕 **自检口**：直接喂那三格（见 <see cref="TickInjected"/> 里的理由）。
        /// ⛔ 生产路径**别调** —— 它会让那一帧的摄像机听一个不存在的鼠标。</summary>
        public void InjectPointerSource(float scrollDelta, bool secondaryHeld, Vector2 dragDelta)
        {
            _scrollDelta = scrollDelta;
            _rightButtonHeld = secondaryHeld;
            _pointerDragDelta = dragDelta;
        }

        /// <summary>🆕 **自检口**：把「窗口有没有焦点」那一问钉成给定答案（`null` = 用真的 `Application.isFocused`）。
        /// <para>🔴 原版那一跳读 `EventSystem.current.m_HasFocus`（`+0x48`，由 `EventSystem.OnApplicationFocus` 写）；
        /// 我们的等价物 = `Application.isFocused`，而**批处理里它恒为 `false`**（没有窗口）
        /// ⇒ 不钉住的话「手动缩放/平移」那一路在自检里**全被这道门挡掉**（而那是本件的主体）。</para></summary>
        public bool? FocusedOverride;

        // ---- 自检口：私有序列化字段的只读出口（原版那几个字段是 `[SerializeField] private`，自检断不到）----
        /// <summary>`zoomSpeed`（原版 `+0x48`）—— 场景值 **12.5**（ctor 默认 0.1，见字段注释）。</summary>
        public float ZoomSpeedTest { get { return zoomSpeed; } }
        /// <summary>`dragSensitivity`（原版 `+0x64`）—— 场景值 **0.01**（ctor 默认 0.005）。</summary>
        public float DragSensitivityTest { get { return dragSensitivity; } }
        /// <summary>`snapBackSpeed`（原版 `+0x68`）= **5.0**。</summary>
        public float SnapBackSpeedTest { get { return snapBackSpeed; } }
        /// <summary>`zoomSensitivity`（原版 `+0x40`）= **0.1**。</summary>
        public float ZoomSensitivityTest { get { return zoomSensitivity; } }
        /// <summary>`zoomSensitivityMobile`（原版 `+0x44`）= **0.005**。</summary>
        public float ZoomSensitivityMobileTest { get { return zoomSensitivityMobile; } }
        /// <summary>`invertMouseScrollWheel`（原版 `+0x60`）—— 场景值 **true**（ctor 默认 false）。</summary>
        public bool InvertMouseScrollWheelTest { get { return invertMouseScrollWheel; } }
        /// <summary>`worldBounds`（原版 `+0x6C`）—— 场景值 `(89.95, −8, 20.115, 15.75)` **再减 100 的 x 平移**。</summary>
        public Rect WorldBoundsTest { get { return worldBounds; } }

        // ==================================================================
        //  原版的方法（逐句；出处见文件头 ①）
        // ==================================================================

        void Awake()
        {
            // 原版第一句：`UnityEngine_Behaviour__set_enabled(this, 0)` —— **组件出厂是关的**，
            // `Initialize()` 才把它打开（所以 `LateUpdate` 在那之前不会跑）。
            enabled = false;
            SubscribeScreenSizeSource();
        }

        void OnDestroy() { UnsubscribeScreenSizeSource(); }

        void SubscribeScreenSizeSource()
        {
            // 原版 `Awake` 的后半段：往 `battleCameraScreenSize` 的两个 `Action<Vector2>` 上 `+=`
            // （`+0x58` 那条配 `OnCameraSensorSizeChanged`、`+0x50` 那条配 `OnCameraShiftChanged`）。
            // ⚠️ `Delegate.Combine` **不幂等**，原版靠「一局一份场景、`Awake` 只跑一次」；我们加幂等守卫
            //（同 `CombatAutoZoom.AttachMinionEvent` 的理由：批处理下 `AddComponent` 跑不跑 `Awake` 本工程未定论）。
            OnCameraSensorSizeChangedSource -= OnCameraSensorSizeChanged;
            OnCameraSensorSizeChangedSource += OnCameraSensorSizeChanged;
            OnCameraShiftChangedSource -= OnCameraShiftChanged;
            OnCameraShiftChangedSource += OnCameraShiftChanged;
            if (!_screenSizeSourceNoted)
            {
                _screenSizeSourceNoted = true;
                // 🔴 出声一次：原版的**生产者**是 `BattleCameraSreenSize`，它按屏幕尺寸/小屏档去动
                // `sensorSize` 与 `lensShift.y`（DOTween，`animTime`）；那件组件我们没做 ⇒
                // 这两个回调**现在永远不会被抬**，`targetOriginalLensShift` 恒等于 vcam 那一格。
                Debug.Log("[CombatCameraZoom] 🔴 `BattleCameraSreenSize`（原版 `+0x38` 那件组件，TypeDefIndex 493）"
                        + "**我们没做** ⇒ `Awake`/`OnDestroy` 那两句 `+=`/`-=` 订到的这一对回调**没有生产者**："
                        + "`OnCameraShiftChanged` 永远收不到（`targetOriginalLensShift` 不会动）。**这是触发源缺，不是省了这一格。**");
            }
        }

        void UnsubscribeScreenSizeSource()
        {
            // 原版 `OnDestroy` 就是上面那两句的 `Delegate.Remove`（逐句与 `Awake` 对称）。
            OnCameraSensorSizeChangedSource -= OnCameraSensorSizeChanged;
            OnCameraShiftChangedSource -= OnCameraShiftChanged;
        }

        /// <summary>= 原版 `LateUpdate()`。
        /// <code>
        /// if (manualCamera) { HandleManualControl(); return; }          // 手动档每帧走这条
        /// if (TouchInputManager.ScrollDelta != 0 || TouchInputManager.TouchPressedSecondary) {
        ///     BattleHud.Instance.ToggleResetAutoCameraZoom(allowManualControl);
        ///     manualCamera = allowManualControl;                        // ← 先「进手动档」，本帧不再落点
        /// }
        /// if (manualCamera) return;
        /// ApplyZoom(Vector2.zero);
        /// </code>
        /// <para>🔴 **为什么滚一下就把开关拨成 `allowManualControl`**：原版就是「玩家一动 ⇒ 自动缩放退位」，
        /// 手动档一直保持到 `SetZoomLevel(force:true)`（HUD 那颗「重置自动镜头」）把它清掉。</para></summary>
        void LateUpdate() { Tick(Time.deltaTime); }

        /// <summary>框架那一步（= 原版 `LateUpdate`，`deltaTime` 提成参数只为自检能直调，见 `Shell/PointerLayer.TickAt` 的先例）。
        /// ⚠️ 每帧第一步 = <see cref="PollPointerSource"/>（原版是 `TouchInputManager.Update`，见它的注释）。</summary>
        public void Tick(float dt)
        {
            PollPointerSource();
            TickBody(dt);
        }

        /// <summary>🆕 **自检口**：不重新采样鼠标地跑一帧 —— 配合 <see cref="InjectPointerSource"/>。
        /// <para>存在的理由：原版那三格（滚轮/右键/拖拽）由 `TouchInputManager.Update` 每帧从**真鼠标**取，
        /// 而批处理里没有真鼠标，且 <see cref="Tick"/> 的第一步就会把注入的值覆盖掉
        /// ⇒ 手动那一路在自检里根本走不到。**这不是「换一条实现」**：它跑的是同一个 <see cref="TickBody"/>。</para></summary>
        public void TickInjected(float dt) { TickBody(dt); }

        /// <summary>= 原版 `LateUpdate` 的**函数体本身**（`Tick` / `TickInjected` 共用同一份）。</summary>
        void TickBody(float dt)
        {
            TickCount++;

            if (manualCamera)
            {
                HandleManualControl(dt);
                return;
            }
            if (ScrollDelta != 0f || TouchPressedSecondary)
            {
                if (ToggleResetCameraZoomUi != null) ToggleResetCameraZoomUi(allowManualControl);
                else if (!_resetUiNoted) { _resetUiNoted = true; Debug.Log(ResetUiMissingLog); }
                manualCamera = allowManualControl;
            }
            if (manualCamera) return;
            ApplyZoomAt(Vector2.zero, dt, false);
        }

        bool _resetUiNoted;
        const string ResetUiMissingLog =
            "[CombatCameraZoom] 原版这三处会 `BattleHud.Instance.ToggleResetAutoCameraZoom(active)`"
          + "（显隐 HUD 那颗「重置自动镜头」+ `DOPunchScale`）—— ⚠️ **本局没人接这条钩子**"
          + "（正常战斗里 `BattleDriver.SetupAutoZoom` 会把它接成 `ToggleCameraResetButton`）" +
            "⇒ 这一句只出声、不产生画面效果（钩子 = `ToggleResetCameraZoomUi`）";

        /// <summary>= 原版 `ToggleAllowManualControl(bool option)`：一句赋值（`+0x80 = option`）。</summary>
        public void ToggleAllowManualControl(bool option) { allowManualControl = option; }

        /// <summary>= 原版 `Initialize(bool allowManualCameraControl)`（`CombatCameraZoom__Initialize.c` 逐句）。
        /// 调用点 = `BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c:229`（换牌结束、真开打那一刻；
        /// 教学那条 `_TutorialStartSequence_d__592__MoveNext.c:145` 同），实参 = **`matchType != 100`**
        /// （`dump.cs:27379` = `MatchType.Tutorial = 100` ⇒ **教学里不许玩家手动控镜头**；我们没有教学模式 ⇒ 传 `true`）。
        /// <para>它做两件事：① 打开组件（`enabled = true`）；② **反解开局那个 zoom** —— 拿
        /// `CalculateFraming(0)` / `CalculateFraming(1)` 两个 `sensorSize.x` 把相机**现在**那个值换算成 `t`，
        /// 再写进 `targetZoomLevel` 与 `currentZoomLevel`（⇒ **开局不跳画面**）。
        /// 然后把 vcam 那三格（`originalLensShift` / `targetLensShift` / `targetOriginalLensShift`）都置成
        /// **vcam 当前**的位移。</para>
        /// <para>⚠️ 它**不落点**（不写相机）—— 落点是 `ApplyZoom` 的事。</para></summary>
        public void Initialize(bool allowManualCameraControl)
        {
            allowManualControl = allowManualCameraControl;
            enabled = true;
            SubscribeScreenSizeSource();     // 原版这一句在 `Awake` 里；见下面那段注释（批处理下 `Awake` 跑不跑本工程未定论）

            if (framer == null || framer.boardCamera == null)
            {
                Debug.LogError("[CombatCameraZoom] `Initialize` 拿不到取景器（`framer` / 它的 `boardCamera`）"
                             + " —— 原版这里就是空引用（`FUN_1803f47a0`）⇒ 取景落不下去，如实报错，**不静默**");
                return;
            }
            Vector2 ss0, ss1, shift;
            framer.CalculateFraming(0f, out ss0, out shift);
            framer.CalculateFraming(OffZoom, out ss1, out shift);

            Vector2 camSS = framer.boardCamera.sensorSize;
            float t = 0f;
            if (ss0.x != ss1.x)
            {
                float raw = (camSS.x - ss0.x) / (ss1.x - ss0.x);
                t = Mathf.Clamp01(raw);            // 原版那两条 `comiss`（下界 0 · 上界 DAT_1834b2bb8 = 1.0）
            }
            TargetZoomLevel = t;                    // 走 setter（含 `smallScreenUI` 夹取）
            currentZoomLevel = t;                   // 原版：`*(param_1 + 0x7c) = fVar5`

            Vector2 v = virtualCameraLensShift;
            originalLensShift       = v;            // `+0x90/0x94`
            targetLensShift         = v;            // `+0x88/0x8c`
            targetOriginalLensShift = v;            // `+0x98/0x9c`
        }

        /// <summary>= 原版 `SetZoomLevel(float zoomLevel, bool instant = false, bool force = false)`（逐句）：
        /// <code>
        /// if (force) { BattleHud.Instance.ToggleResetAutoCameraZoom(false); manualCamera = false; }
        /// if (!manualCamera) {
        ///     TargetZoomLevel = Mathf.Clamp01(zoomLevel);
        ///     if (instant) currentZoomLevel = Mathf.Clamp01(zoomLevel);
        /// }
        /// </code>
        /// <para>🔴 **`instant` 就是原版自带的「不插值、直接到位」那一档**（`*(param_1 + 0x7c) = Clamp01(x)`）——
        /// 我们无帧循环那一档也正需要它（见 <see cref="SettleFraming"/>）。</para>
        /// <para>⚠️ 它**不落点**（不写相机）—— 落点仍是 `ApplyZoom`。
        /// 调用点：教学 `_TutorialStartSequence_d__592__MoveNext.c:149`（`SetZoomLevel(useCombatAutoZoom ? 0 : 1)`）。</para></summary>
        public void SetZoomLevel(float zoomLevel, bool instant = false, bool force = false)
        {
            if (force)
            {
                if (ToggleResetCameraZoomUi != null) ToggleResetCameraZoomUi(false);
                else if (!_resetUiNoted) { _resetUiNoted = true; Debug.Log(ResetUiMissingLog); }
                manualCamera = false;
            }
            if (!manualCamera)
            {
                TargetZoomLevel = Mathf.Clamp01(zoomLevel);
                if (instant) currentZoomLevel = Mathf.Clamp01(zoomLevel);
            }
        }

        /// <summary>= 原版 `GetMaxZoomLevel(float desiredZoomLevel)`（逐句）：
        /// <code>
        /// float max = 1.0f;                                  // DAT_1834b2bb8
        /// if (GameStaticData.smallScreenUI /* +0x11c */)
        ///     max = maxZoomForSmallScreenDevicesByAspectRatio.Evaluate(targetCamera.aspect);
        /// return Mathf.Min(desiredZoomLevel, max);
        /// </code>
        /// <para>调用点 = `BattleCameraSreenSize__Initialize.c:25`（`GetMaxZoomLevel(1.0f)`）。</para></summary>
        public float GetMaxZoomLevel(float desiredZoomLevel)
        {
            float max = OffZoom;
            if (SmallScreenUI.Enabled && targetCamera != null)
                max = maxZoomForSmallScreenDevicesByAspectRatio != null
                    ? maxZoomForSmallScreenDevicesByAspectRatio.Evaluate(targetCamera.aspect)
                    : CombatAutoZoom.MaxZoomForSmallScreen(targetCamera.aspect);
            return Mathf.Min(desiredZoomLevel, max);
        }

        /// <summary>= 原版 `get_TargetZoomLevel` / `set_TargetZoomLevel`（`+0x84`）。
        /// 🔴 setter **只上夹**（`Mathf.Min(value, GetMaxZoomLevel)`），**不下夹** —— 原版就长这样。</summary>
        public float TargetZoomLevel
        {
            get { return targetZoomLevel; }
            set { targetZoomLevel = GetMaxZoomLevel(value); }
        }

        /// <summary>= 原版 `get_TargetZoomLevel` 的读口（自检用；同 `CombatAutoZoom.ZoomLevel` 那条口径）。</summary>
        public float CurrentZoomLevel { get { return currentZoomLevel; } }
        /// <summary>= 原版 `get_ZoomSensitivity()`：`Application.isMobilePlatform ? zoomSensitivityMobile : zoomSensitivity`。</summary>
        public float ZoomSensitivity
        {
            get { return Application.isMobilePlatform ? zoomSensitivityMobile : zoomSensitivity; }
        }
        /// <summary>原版 `manualCamera`（`+0x30`）的读写口 —— 给 `CombatAutoZoom` 转发用。</summary>
        public bool ManualCamera { get { return manualCamera; } set { manualCamera = value; } }
        /// <summary>原版 `allowManualControl`（`+0x80`）。</summary>
        public bool AllowManualControl { get { return allowManualControl; } }
        /// <summary>`Tick` 跑了几次（自检用：钉「帧循环那一步真的在跑」）。</summary>
        public int TickCount { get; private set; }
        /// <summary>这种「settle」落点真的写进相机几次（自检用）。</summary>
        public int FramingApplyCount { get; private set; }

        /// <summary>= 原版 `DetectPlayerInput()`（**本 build 无调用点**，见文件头 F —— 它的体被内联进了 `LateUpdate`）。
        /// 语义 = 「滚了或按了右键 ⇒ 每帧把 `manualCamera` 对齐到 `allowManualControl` 并显示那颗重置钮」。</summary>
        public void DetectPlayerInput()
        {
            if (ScrollDelta == 0f && !TouchPressedSecondary) return;
            if (ToggleResetCameraZoomUi != null) ToggleResetCameraZoomUi(allowManualControl);
            else if (!_resetUiNoted) { _resetUiNoted = true; Debug.Log(ResetUiMissingLog); }
            manualCamera = allowManualControl;
        }

        /// <summary>= 原版 `ToggleManualCameraControl(bool option)`（**本 build 无调用点**，见文件头 F）：
        /// `BattleHud.ToggleResetAutoCameraZoom(option &amp;&amp; allowManualControl)`，然后
        /// `manualCamera = option &amp;&amp; allowManualControl`。
        /// <para>⚠️ 原版那几行的实际取值（逐句读）：`cVar2 = option ? allowManualControl : false` 传给 HUD；
        /// `cVar3 = option ? allowManualControl : false` 写 `manualCamera`。</para></summary>
        public void ToggleManualCameraControl(bool option)
        {
            bool v = option && allowManualControl;
            if (ToggleResetCameraZoomUi != null) ToggleResetCameraZoomUi(v);
            else if (!_resetUiNoted) { _resetUiNoted = true; Debug.Log(ResetUiRememberMissingLog); }
            manualCamera = v;
        }

        const string ResetUiRememberMissingLog =
            "[CombatCameraZoom] `ToggleManualCameraControl` 里那句 `BattleHud.ToggleResetAutoCameraZoom(...)`"
          + " 没地方落（我们 HUD 没有那颗钮，见文件头 E）";

        /// <summary>= 原版 `ZoomChange(float delta)`（**本 build 无调用点**，见文件头 F；`HandleManualControl` 里有一份内联拷贝）：
        /// <code>
        /// TargetZoomLevel = targetZoomLevel + delta;     // setter 内含 `smallScreenUI` 上夹
        /// float v = targetZoomLevel;                     // 再读回来
        /// TargetZoomLevel = Mathf.Clamp(v, 0f, 1f);      // 原版是「>1 ⇒ 写 1；否则写 v」
        /// </code></summary>
        public void ZoomChange(float delta)
        {
            TargetZoomLevel = targetZoomLevel + delta;
            float v = targetZoomLevel;
            if (v < 0f) v = 0f;
            else if (v > OffZoom) v = OffZoom;
            TargetZoomLevel = v;
        }

        /// <summary>= 原版 `HandleDrag()`：右键按着时 = `TouchDragDelta × dragSensitivity × (−1f)`，否则 `Vector2.zero`。
        /// 🔴 那个 **−1** 是 `.rdata` 直读（`0x1834B2BC8`，文件头 ③）—— 少了它拖拽方向就反了。</summary>
        public Vector2 HandleDrag()
        {
            if (!TouchPressedSecondary) return Vector2.zero;
            return new Vector2(TouchDragDelta.x * dragSensitivity * DragSign,
                               TouchDragDelta.y * dragSensitivity * DragSign);
        }

        /// <summary>原版那个乘数 = `DAT_1834b2bc8` = **−1.0**。</summary>
        public const float DragSign = -1f;

        /// <summary>= 原版 `HandleManualControl()`（逐句）。
        /// <para>三道门（顺序原版就是这样）：① 双指（移动端，见文件头 C）⇒ 取 `fingerId`；
        /// ② `EventSystemController.Instance != null &amp;&amp; EventSystem.current != null
        /// &amp;&amp; !IsPointerOverUIObject(fingerId)`；③ `EventSystem.current.m_HasFocus`。
        /// 🔴 我们的等价物见文件头 B：② 的 `IsPointerOverUIObject` 走 <see cref="PointerOverUi"/>（待接线），
        /// ③ 走 `Application.isFocused`。</para>
        /// <para>门内两件事：**滚轮改 zoom**（`ZoomSensitivity` 分桌面/移动两档 · 桌面且
        /// `invertMouseScrollWheel` 时**翻符号**）→ `ZoomChange` 那两句；然后 `ApplyZoom(HandleDrag())`。</para>
        /// <para>⚠️ 真机没验过（桌面批处理调不动双指那一路）—— 如实记。</para></summary>
        public void HandleManualControl() { HandleManualControl(Time.deltaTime); }

        /// <summary><see cref="HandleManualControl()"/> 的 `deltaTime` 版本（自检直调用）。</summary>
        public void HandleManualControl(float dt)
        {
            int pointerId = -1;
            if (Application.isMobilePlatform &&
                UnityEngine.InputSystem.Touchscreen.current != null &&
                UnityEngine.InputSystem.Touchscreen.current.touches.Count > 1)
            {
                // 原版：`if (Application.isMobilePlatform && Input.touchCount > 1)`
                //        `{ Touch t = Input.GetTouch(0); if (t.phase == TouchPhase.Began) pointerId = Input.GetTouch(0).fingerId; }`
                // 新输入系统里 TouchPhase.Began ≈ `touch.phase == TouchPhase.Began`（枚举值 0，与 legacy 同）；
                // 我们桌面这一档走不到这里 —— 照原版写出来，但**没验过**（文件头 C）。
                var t0 = UnityEngine.InputSystem.Touchscreen.current.touches[0];
                if (t0.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    pointerId = t0.touchId.ReadValue();
            }

            if (!PointerOverUiObject(pointerId)) return;
            if (!Focused()) return;                     // ← 原版 `EventSystem.current.m_HasFocus`(+0x48)

            float scroll = ScrollDelta;
            if (scroll != 0f)
            {
                if (!Application.isMobilePlatform && invertMouseScrollWheel)
                    scroll = -scroll;                   // 原版：`xmm6 ^= DAT_1834b2c80`（符号位掩码 −0.0）= 求负
                ZoomChange(scroll * ZoomSensitivity);   // 桌面 `zoomSensitivity` / 移动 `zoomSensitivityMobile`
            }
            ApplyZoomAt(HandleDrag(), dt, false);
        }

        /// <summary>= 原版 `HandleAutomaticControl()`（**本 build 无调用点**，见文件头 F）：
        /// 一句 `ApplyZoom(Vector2.zero)`。</summary>
        public void HandleAutomaticControl() { ApplyZoomAt(Vector2.zero, Time.deltaTime, false); }

        /// <summary>原版那两道「指针在不在本游戏上」的门的等价物（见文件头 B）：
        /// `PointerOverUi` 没人接 ⇒ 按「不在 UI 上」放行，**并出声一次**（⛔ 默认成「在 UI 上」= 手动缩放永远失效 = 静默）。</summary>
        bool PointerOverUiObject(int pointerId)
        {
            if (PointerOverUi == null)
            {
                if (!_pointerGateNoted)
                {
                    _pointerGateNoted = true;
                    Debug.Log("[CombatCameraZoom] 🔴 「指针在不在 UI 上」那一问**没人接**"
                            + "（原版 = `EventSystemController.Instance.IsPointerOverUIObject(fingerId)`；"
                            + "本仓没有 UGUI `EventSystem`/`EventSystemController`，见 `Editor/ShellScene.cs:1884`）"
                            + " ⇒ 这一档按**不在 UI 上**放行。要挡住面板/窗口上方的滚轮：给 `PointerOverUi` 接一个判据。");
                }
                return true;
            }
            return !PointerOverUi();
        }

        /// <summary>= 原版那一句 `EventSystem.current.m_HasFocus`（`EventSystem` 的 `+0x48`，
        /// 由 `EventSystem.OnApplicationFocus` 写）在我们这一档的等价物 —— 即 `Application.isFocused`；
        /// <see cref="FocusedOverride"/> 非空时以它为准（自检口，理由见那个字段）。</summary>
        bool Focused() { return FocusedOverride.HasValue ? FocusedOverride.Value : Application.isFocused; }

        /// <summary>🆕 **自检口**：把「鼠标在哪」钉成给定的**视口**坐标（`null` = 用真的 `Mouse.current.position`）。
        /// 批处理里没有真鼠标 ⇒ 不钉住的话 <see cref="GetWorldPositionUnderMouse"/> 会跟着一个假坐标走
        /// （而它在 `ApplyZoom` 的手动档里进「参考点」那一项）。</summary>
        public Vector2? PointerViewportOverride;

        /// <summary>= 原版 `GetWorldPositionUnderMouse()`（逐句；`Vector2.zero`/`Vector2.one` 两格见文件头 ③）：
        /// <code>
        /// Vector3 vp = targetCamera.ScreenToViewportPoint(Input.mousePosition);
        /// float dx = vp.x − Vector2.one.x × 0.5f,  dy = vp.y − Vector2.one.y × 0.5f;
        /// Vector2 ss = targetCamera.sensorSize;
        /// return ( ss.x × dx + ss.x × vcamLensShift.x,
        ///         (ss.y / targetCamera.aspect) × dy + ss.y × vcamLensShift.y );
        /// </code>
        /// <para>⚠️ 名字叫「世界坐标」，其实量纲是「半个 sensor」—— 判据照它，⛔ 别按名字改。</para></summary>
        public Vector2 GetWorldPositionUnderMouse()
        {
            if (targetCamera == null) return Vector2.zero;
            if (PointerViewportOverride.HasValue)
                return GetWorldPositionViewportSpace(PointerViewportOverride.Value);
            var m = UnityEngine.InputSystem.Mouse.current;
            Vector3 mp = m != null ? (Vector3)m.position.ReadValue() : Vector3.zero;
            return GetWorldPositionViewportSpace(targetCamera.ScreenToViewportPoint(mp));
        }

        /// <summary>= 原版 `GetWorldPositionViewportSpace(Vector2 viewportPoint)`（逐句）。</summary>
        public Vector2 GetWorldPositionViewportSpace(Vector2 viewportPoint)
        {
            float dx = viewportPoint.x - 0.5f;                       // Vector2.one.x * DAT_1834b2bb4(=0.5)
            float dy = viewportPoint.y - 0.5f;
            Vector2 ss = targetCamera.sensorSize;
            Vector2 ls = virtualCameraLensShift;
            return new Vector2(ss.x * dx + ss.x * ls.x,
                               (ss.y / targetCamera.aspect) * dy + ss.y * ls.y);
        }

        /// <summary>= 原版 `get_ZoomSensitivity` 用的那个「不缩放档」= 常量 `DAT_1834b2bb8` = **1.0**。
        /// ⛔ 别在这儿另写一个 `1f` —— 同一个常量的字面量只留一处（<see cref="CombatAutoZoom.OffZoomLevel"/>）。</summary>
        public const float OffZoom = CombatAutoZoom.OffZoomLevel;

        /// <summary>= 原版 `ApplyZoom(Vector2 drag)`（`CombatCameraZoom__ApplyZoom.c` + 指令流 `0x18060CF60..0x18060D538`）。
        /// 生产路径就这一条（`LateUpdate` / `HandleManualControl` 调它）。
        /// <para>🆕 `dt` / `settle` 两个参数是为**无帧循环**那一档加的（见文件头 A）；⛔ 生产路径
        /// **一律** `settle: false`（= 逐句原版，`dt` 取 `Time.deltaTime`）。</para></summary>
        void ApplyZoomAt(Vector2 drag, float dt, bool settle)
        {
            // 原版这几处是**空引用抛点**（`FUN_1803f47a0` = il2cpp 抛 `NullReferenceException`）。
            // 我们改成「出声一次 + 早退」：每帧抛会刷屏，而这条只有「引用没接上」才会走到
            // （`BattleDriver.SetupAutoZoom` 建它的时候是接齐的）。⛔ 不是语义差异：原版走到那儿已经崩了。
            if (framer == null || targetCamera == null || framer.boardCamera == null)
            {
                if (!_refsMissingNoted)
                {
                    _refsMissingNoted = true;
                    Debug.LogError("[CombatCameraZoom] `ApplyZoom` 的前置引用没接齐："
                                 + $"framer={(framer != null)} · targetCamera={(targetCamera != null)} · "
                                 + $"framer.boardCamera={(framer != null && framer.boardCamera != null)}"
                                 + " ⇒ **取景这一帧没落下去**（原版这里是空引用抛点）。");
                }
                return;
            }

            // ---- ① zoom 平滑（原版 `Mathf.SmoothDamp(currentZoomLevel, targetZoomLevel, ref currentZoomVelocity,
            //         Time.deltaTime * zoomSpeed, +∞, Time.deltaTime)`；位置 3/4/5 的实参见文件头 ③）----
            currentZoomLevel = settle
                ? targetZoomLevel                     // = 原版 `Initialize` 末尾那一手（直接置，不插值）
                : Mathf.SmoothDamp(currentZoomLevel, targetZoomLevel, ref currentZoomVelocity,
                                   dt * zoomSpeed, float.PositiveInfinity, dt);

            // ---- ② 把 `originalLensShift` 以 `dt × 10` 的速度追向 `targetOriginalLensShift`；
            //         追得到（或本来就相等）就**一步贴上去**（原版那两条 `comiss` 判的就是「够不够一步走完」）----
            {
                Vector2 cur = originalLensShift;
                Vector2 tgt = targetOriginalLensShift;
                Vector2 d = tgt - cur;
                float sq = d.x * d.x + d.y * d.y;
                float step = settle ? float.PositiveInfinity : dt * OriginalLensShiftSpeed;
                Vector2 moved = tgt;                                  // 默认（= 贴上去那一支）
                if (sq != 0f && (step < 0f || step * step < sq))
                {
                    float len = Mathf.Sqrt(sq);
                    moved = new Vector2(d.x / len * step + cur.x, d.y / len * step + cur.y);
                }
                originalLensShift = moved;
            }

            // ---- ③ 自动档：把 `targetLensShift` 朝取景器算出来的位移插值 ----
            //      ⚠️ 先算「这一帧的平滑基础值」，自动/手动两档用的是**同一个变量**（原版 xmm10/xmm11）
            Vector2 smoothed;
            if (!manualCamera)
            {
                Vector2 newSensorSize, desiredLensShift;
                framer.CalculateFraming(currentZoomLevel, out newSensorSize, out desiredLensShift);

                float t = settle ? 1f : Mathf.Clamp01(dt * snapBackSpeed);
                targetLensShift = new Vector2(targetLensShift.x + (desiredLensShift.x - targetLensShift.x) * t,
                                              targetLensShift.y + (desiredLensShift.y - targetLensShift.y) * t);
                // 🔴 「贴近就吸附」—— 吸附的**赋值是 `desiredLensShift`**（指令流 `0x18060D22A/D232`；
                //    Ghidra 的 `.c` 把它读成了「vcam − desired」，见文件头 ③）。
                Vector2 near = virtualCameraLensShift - desiredLensShift;
                if (near.x * near.x + near.y * near.y < LensShiftSnapEpsilon)      // DAT_1834b3174 = 1e-07
                    targetLensShift = desiredLensShift;

                smoothed = targetLensShift;
            }
            else
            {
                // 手动档第一句（原版 `0x18060D23C..0x18060D281`）：**平移量直接叠在当前 vcam 位移上**
                //   —— `targetLensShift = virtualCameraLensShift + drag`（不是 `+=` 到旧 target 上；
                //   因为是「当前值 + 增量」，逐帧调它本身就在累积）。
                targetLensShift = new Vector2(virtualCameraLensShift.x + drag.x,
                                              virtualCameraLensShift.y + drag.y);

                // 再 `Vector2.SmoothDamp(vcam.LensShift, targetLensShift, ref currentLensShiftVelocity,
                //                        Time.deltaTime * zoomSpeed, +∞, Time.deltaTime)`
                smoothed = settle
                    ? targetLensShift
                    : Vector2.SmoothDamp(virtualCameraLensShift, targetLensShift, ref currentLensShiftVelocity,
                                         dt * zoomSpeed, float.PositiveInfinity, dt);
            }

            // ---- ④ 参考点 A（原版 `[rsp+0x118]`）：自动档 = `Vector2.zero`；手动档 = `GetWorldPositionUnderMouse()` ----
            Vector2 referenceA = manualCamera ? GetWorldPositionUnderMouse() : Vector2.zero;

            // ---- ⑤ 落点：先算一次取景（拿 `newSensorSize` 写进相机），再算参考点 B，最后写 vcam 的 LensShift ----
            Vector2 ss2, ignoreShift;
            framer.CalculateFraming(currentZoomLevel, out ss2, out ignoreShift);
            targetCamera.sensorSize = ss2;             // 原版 `Camera.set_sensorSize(newSensorSize)`（`.x` 是曲线值、`.y` 原样）

            Vector2 referenceB = manualCamera ? GetWorldPositionUnderMouse() : Vector2.zero;

            // `(referenceA − referenceB) / targetCamera.sensorSize`（**同一个 sensorSize**，两次都在
            // `set_sensorSize` 之后读；指令流 `0x18060D481/D492`，见文件头 ③）——这一项补偿的是
            // 「传感器尺寸变了 ⇒ 世界尺度变了」那一跳。
            Vector2 ss = targetCamera.sensorSize;
            Vector2 corrected = new Vector2(smoothed.x + (referenceA.x - referenceB.x) / ss.x,
                                            smoothed.y + (referenceA.y - referenceB.y) / ss.y);

            virtualCameraLensShift = ClampShiftLimits(corrected);
            ApplyVirtualCameraLensShift();
            if (settle) FramingApplyCount++;
        }

        /// <summary>= Cinemachine 每帧「把 `vcam.m_Lens` 推给真相机」那一下（我们没有 Cinemachine，见文件头 ⑥）。
        /// 原版 `ApplyZoom` 的最后一跳只是写 `vcam.m_Lens.LensShift`（`0x18060D520/D528`），
        /// 由 Cinemachine 的 pipeline 落到 `Camera.lensShift` —— 我们在这里显式做掉。</summary>
        void ApplyVirtualCameraLensShift()
        {
            if (targetCamera == null) return;
            targetCamera.lensShift = virtualCameraLensShift;
        }

        /// <summary>`DAT_1834b3174` = **1e-07**（「vcam 与目标位移贴近到多少就吸附」的**平方**阈值）。</summary>
        public const float LensShiftSnapEpsilon = 1e-7f;
        /// <summary>`DAT_1834b2ba8` = **1e-10**（边界修正「小到可忽略」的**平方**阈值；同 `CalculateFraming` 里缓存重抓那一格）。</summary>
        public const float ShiftCorrectionEpsilon = 1e-10f;
        /// <summary>`DAT_1834b2db8` = **1e-06**（相机 forward 与 `Vector3.forward` 的夹角判据）。</summary>
        public const float CameraForwardEpsilon = 1e-6f;

        /// <summary>= 原版 `ClampShiftLimits(Vector2 desiredLensShift)`（逐句 + 指令流 `0x18060D720`）：
        /// 把「视野在 `y = 0` 平面上的世界矩形」算出来，与 <see cref="worldBounds"/> 比；
        /// 出界就按 `CorrectLensShift` 那个除法把位移推回去；**修正小到 `1e-10` 以内就原样返回**。
        /// <para>⚠️ 那两条 x 判断**不是 `else if`**（原版两条独立 `if`，后者可以覆盖前者）—— 照抄，
        /// 别「顺手改成 else if」。</para></summary>
        public Vector2 ClampShiftLimits(Vector2 desiredLensShift)
        {
            Rect r = GetCameraFrustumWorldBoundsWithShift(desiredLensShift);

            float nx = 0f, ny = 0f;                                  // 原版初值 = Vector2.zero
            if (r.xMin < worldBounds.xMin) nx = worldBounds.xMin - r.xMin;
            if (worldBounds.xMax < r.xMax) nx = worldBounds.xMax - r.xMax;
            if (r.yMin < worldBounds.yMin) ny = worldBounds.yMin - r.yMin;
            if (worldBounds.yMax < r.yMax) ny = worldBounds.yMax - r.yMax;

            if (nx * nx + ny * ny < ShiftCorrectionEpsilon) return desiredLensShift;
            return CorrectLensShift(desiredLensShift, new Vector2(nx, ny));
        }

        /// <summary>= 原版 `CorrectLensShift(Vector2 desiredLensShift, Vector2 correction)`（指令流 `0x18060D950`）：
        /// <code>
        /// float kX = (targetCamera.sensorSize.x * Mathf.Abs(targetCamera.transform.position.z)) / targetCamera.focalLength;
        /// float kY = (targetCamera.sensorSize.y * Mathf.Abs(targetCamera.transform.position.z)) / targetCamera.focalLength;
        /// return (desired.x + correction.x / kX, desired.y + correction.y / kY);
        /// </code>
        /// （那个 `|position.z|` 就是 `andps [0x1834b2e60]` = `0x7FFFFFFF` 掩码。）</summary>
        public Vector2 CorrectLensShift(Vector2 desiredLensShift, Vector2 correction)
        {
            float z = Mathf.Abs(targetCamera.transform.position.z);
            float f = targetCamera.focalLength;
            float kx = (targetCamera.sensorSize.x * z) / f;
            float ky = (targetCamera.sensorSize.y * z) / f;
            return new Vector2(desiredLensShift.x + correction.x / kx,
                               desiredLensShift.y + correction.y / ky);
        }

        /// <summary>= 原版 `GetCameraFrustumWorldBoundsWithShift(Vector2 desiredShift)`（逐句 + 文件头 ③④）：
        /// 「相机视野在 `z = 0` 平面上」的世界矩形（含透镜位移带来的平移）。
        /// <para>`t` = 相机到那个平面沿 forward 的距离；那个平面判据用的是 `Vector3.forward`
        /// （`Mathf.Abs(forward.z) &lt; 1e-06` ⇒ 打一句警告、返回**空 Rect**）。</para>
        /// <para>半宽 `= sensorSize.x × t / focalLength / 2`；半高 `= (那个半宽 / aspect) / 2`。
        /// 平移项 `= desiredShift × t × sensorSize / focalLength`。</para></summary>
        public Rect GetCameraFrustumWorldBoundsWithShift(Vector2 desiredShift)
        {
            if (targetCamera == null) return new Rect(0f, 0f, 0f, 0f);
            Transform ct = targetCamera.transform;
            Vector3 fwd = ct.forward;
            float along = fwd.z;                       // = Vector3.Dot(forward, Vector3.forward)

            if (Mathf.Abs(along) < CameraForwardEpsilon)
            {
                if (!_cameraPlaneWarned)
                {
                    _cameraPlaneWarned = true;
                    Debug.LogWarning("[CombatCameraZoom] 战场相机的 forward 与 `Vector3.forward` 近乎垂直"
                                   + "（原版这一支会 `Debug.LogWarning` 并返回**空 Rect**）⇒ 世界边界这一档失去意义");
                }
                return new Rect(0f, 0f, 0f, 0f);       // 原版：`param_1[0..3] = 0`
            }

            Vector2 ss = targetCamera.sensorSize;
            float focal = targetCamera.focalLength;
            Vector3 pos = ct.position;
            float t = (0f - pos.z) / along;

            float halfW = (ss.x * t) / focal;                     // 原版 fVar11
            Vector3 right = ct.right;
            float rx = right.x * (halfW * 0.5f);                  // 原版 fVar8
            float ry = right.y * (halfW * 0.5f);                  // 原版 fVar14
            float halfH = (halfW / targetCamera.aspect) * 0.5f;   // 原版 fVar12
            Vector3 up = ct.up;
            float ux = up.x * halfH;                              // 原版 fVar11（第二义）
            float uy = up.y * halfH;                              // 原版 fVar12（第二义）

            float shiftX = (desiredShift.x * t * ss.x) / focal;   // 原版 fVar13
            float shiftY = (desiredShift.y * t * ss.y) / focal;   // 原版 fVar6

            float cx = pos.x + fwd.x * t + up.x * shiftY + right.x * shiftX;
            float cy = pos.y + fwd.y * t + up.y * shiftY + right.y * shiftX;

            float minX = (cx - rx) - ux;
            float minY = (cy - ry) - uy;
            return new Rect(minX, minY, (cx + rx + ux) - minX, (cy + ry + uy) - minY);
        }

        /// <summary>= 原版 `OnCameraShiftChanged(Vector2 newLensShift)`：一句 `targetOriginalLensShift = newLensShift`。
        /// 🔴 **触发源 = `BattleCameraSreenSize`（我们没做）**，见文件头 D。</summary>
        public void OnCameraShiftChanged(Vector2 newLensShift) { targetOriginalLensShift = newLensShift; }

        /// <summary>= 原版 `OnCameraSensorSizeChanged(Vector2 newSensorSize)`：**空方法**
        /// （`RVA 0x4B33B0` 是个共享空桩 ⇒ `dump.cs` 也把它列在那个共享地址上）。照原版留空。</summary>
        public void OnCameraSensorSizeChanged(Vector2 newSensorSize) { }

        /// <summary>🔴 **2026-10-12（A422）：我们加的** —— 无帧循环下的等价落点（文件头 A）。
        /// <para>原版靠 `LateUpdate` 每帧把 `currentZoomLevel` / `targetLensShift` / `originalLensShift`
        /// 收敛过去；**批处理里 `LateUpdate` 不跑**（本仓自检全是 `-batchmode`）⇒ 只写 `TargetZoomLevel`
        /// 画面永远不动。这一条 = 「**把这一帧当成已经收敛完**」：`currentZoomLevel = targetZoomLevel`
        /// （原版 `Initialize` 末尾那一手）+ 两处插值一步到位，再走**同一条** `ApplyZoomAt` 的落点
        /// （⛔ 落点逻辑**没有第二份**）。</para>
        /// <para>返回「真的写了相机没有」（`manualCamera` 为真时不写 —— 那是原版
        /// `if (!combatCameraZoom.manualCamera)` 那道闸的等价物）。</para></summary>
        public bool SettleFraming()
        {
            if (manualCamera) return false;
            if (framer == null || targetCamera == null || framer.boardCamera == null) return false;
            ApplyZoomAt(Vector2.zero, 0f, true);
            return true;
        }

        /// <summary>`OnDrawGizmos`（原版逐句）：先画 <see cref="worldBounds"/> 的线框，再把「当前视野矩形」画出来。
        /// ⚠️ 原版在 `targetCamera == null` 时是空引用；**编辑器里 `OnDrawGizmos` 每帧都会跑** ⇒ 我们早退
        /// （只影响 gizmo，不是语义差异）。</summary>
        void OnDrawGizmos()
        {
            if (targetCamera == null) return;
            Color saved = Gizmos.color;
            // 两个颜色 = `.rdata` 直读（`_DAT_1834b31a0..bc` 八个 float）：
            //   世界边界 = **(1, 0, 0, 0.5)**（红半透）· 当前视野矩形 = **(0, 1, 0, 0.5)**（绿半透）。
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Vector3 c1 = new Vector3(worldBounds.x + worldBounds.width * 0.5f, worldBounds.y + worldBounds.height * 0.5f, 0f);
            Vector3 s1 = new Vector3(worldBounds.width, worldBounds.height, 0f);
            Gizmos.DrawWireCube(c1, s1);

            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Rect r = GetCameraFrustumWorldBoundsWithShift(targetCamera.lensShift);
            Gizmos.DrawWireCube(new Vector3(r.x + r.width * 0.5f, r.y + r.height * 0.5f, 0f),
                                new Vector3(r.width, r.height, 0f));
            Gizmos.color = saved;
        }
    }
}
