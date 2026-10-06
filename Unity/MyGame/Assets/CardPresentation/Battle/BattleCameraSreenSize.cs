// BattleCameraSreenSize.cs — 原版 `BattleCameraSreenSize`（`dump.cs` TypeDefIndex 493）**整件**（A463，2026-10-13）
//
// ============================ 这件是干什么的 ============================
// 原版战场相机那一组是**三件**（`CombatCameraZoom.cs` 文件头那张表）：
//   · `CameraVerticalFramer`（668）—— 按 zoom 算「传感器尺寸 + 透镜位移」（数学）
//   · `CombatAutoZoom`（669）       —— 按场上人数定 zoom 目标（自动）
//   · `CombatCameraZoom`（670）     —— 每帧把 zoom/位移/边界落到相机上，并吃手动输入
//   · **本件（493）= 「屏幕尺寸变了 ⇒ 重新取景」那条触发器**（外加它自己那条 `DOTween` 补间）
// 它的**唯一生产路径**（全反编译里 `Initialize` 只有一个调用点、`DoLensShift` **零调用点**）：
//   `Start()` 把 `ResolutionHasChanged` 注册到 `Scaffold.Core.Events.Signal` 那条
//   **`ScreenResolutionChangeSignal`** 上 ⇒ 屏幕分辨率一变 ⇒ `ResolutionHasChanged()` =
//   **`Initialize(instant: true)`** ⇒ 重算取景、把 `sensorSize` 与 `lensShift.y` **一步到位**写下去
//   （并抬那两个 `Action<Vector2>`，`CombatCameraZoom.Awake` 早在它上面订过）。
//
// ============================ 判据（全是实读） ============================
// ① **方法体** = `d:/2/tools/decomp_full/BattleCameraSreenSize__{Start, ResolutionHasChanged, Initialize,
//    DoLensShift, ctor}.c` + 四个闭包（`<Initialize>b__11_0/1` · `<DoLensShift>b__12_0/1`）。
// ② **字段名 / 偏移 / 出厂值** = `d:/2/tools/il2cpp_out/dump.cs` 的 `BattleCameraSreenSize`：
//    `boardCamera` `+0x20` · `vcam` `+0x28`(CinemachineVirtualCamera) · `sensorSizeXSmall` `+0x30` ·
//    `sensorSizeXBigScreen` `+0x34` · `animTime` `+0x38` · `cameraVerticalFramer` `+0x40` ·
//    `combatCameraZoom` `+0x48` · `OnCameraShiftChanged` `+0x50` · `OnCameraSensorSizeChanged` `+0x58`。
// ③ **ctor 出厂值**（`BattleCameraSreenSize___ctor.c` 三句，十六进制直读）：
//    `+0x30 = 0x42140000` = **37.0** · `+0x34 = 0x42260000` = **41.5** · `+0x38 = 0x40400000` = **3.0**。
// ④ 🔴 **场景序列化值**（`assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4208.json`，
//    挂在 GO `539` 上）：`sensorSizeXSmall 37.0` · **`sensorSizeXBigScreen 41.0`** · `animTime 3.0`；
//    引用 = `boardCamera`→`Camera_1461` · `vcam`→`4328` · `cameraVerticalFramer`→`4697` · `combatCameraZoom`→`4404`。
//    ⇒ 🔴 **又是一处「ctor ≠ 场景」（铁律 5·c）**：`sensorSizeXBigScreen` **ctor 41.5 / 场景 41.0**
//      —— 我们取**场景那一档**（同 `CombatCameraZoom` 那三处，文件头 ⑤在那边）。
// ⑤ 🔴 **那两个 `DAT_` 是「方法指针」不是常量**（`资料/战斗规则与数值_出处.md` §三 的三类 `DAT_`）——
//    拿 `script.json` 的 `ScriptMetadataMethod` 段解出来：
//    · `DAT_18423d100` = `Method$BattleCameraSreenSize.ResolutionHasChanged()`（`Start` 注册的那个处理器）
//    · `DAT_184271f50` = `Method$Scaffold.Core.Events.Signal.Register<ScreenResolutionChangeSignal>()`
//    · `DAT_18423cf00/…d000/…cd00/…ce00` = 四个闭包（`<Initialize>b__11_0/1` · `<DoLensShift>b__12_0/1`）
//    · `DAT_1842bce88` = `DG.Tweening.Core.DOGetter<Vector2>_TypeInfo` · `DAT_1842bd888` = `…DOSetter<Vector2>_TypeInfo`
//    · `DAT_1842d1320` = `Method$DG.Tweening.TweenSettingsExtensions.SetEase<TweenerCore<Vector2, Vector2, VectorOptions>>()`
//    ⇒ 那两句 `DG_Tweening_DOTween__To(getter, setter, 值, animTime)` **就是** `DOTween.To` 的
//      `DOGetter/ DOSetter` 重载；`SetEase(x, 10, MethodInfo)` 里 **`10` = `DG.Tweening.Ease.InOutCubic`**
//      （`DG.Tweening.Ease`：`Unset 0` `Linear 1` `InSine 2` … `InCubic 8` `OutCubic 9` **`InOutCubic 10`**，
//       `dump.cs` 的 TypeDefIndex 19111 实读 —— ⚠️ **别拿 `UnityEngine.UI.Extensions.EasingCore.Ease`
//       （TypeDefIndex 13768，那一份 `10 = InSine`）来对**，两份枚举值不同序）。
// ⑥ **`.rdata` 常量**（`工具/read_literal.py` 直读，4 字节 float；与 `CombatCameraZoom` 那批同源）：
//    `0x1834b2bb8` = **1.0f**（`GetMaxZoomLevel` 的实参）；`0x1834b2c80` = **−0.0f**（在 `MoveButtons` 那侧用到，
//    本件不用）。（本件**只**读得出这一个 —— `sensorSizeXSmall/BigScreen` 在**本 build 里一个方法都不读**，
//    见下面「没做的」。）
//
// ============================ 🔴 `vcam` 那一格的等价物（简报点名要写清） ============================
// 原版 `vcam`（`+0x28`）是 **`CinemachineVirtualCamera`**；**本工程没有 Cinemachine**
//（`Packages/manifest.json` 的 16 个依赖里没有 `com.unity.cinemachine`）。
// 而本件碰它的**只有一格**（两个闭包 `b__12_0/b__12_1` 读写 `+0xD0`）：
//   · `CinemachineVirtualCamera.m_Lens`（对象偏移 `+0xB8`）里的 `LensSettings.LensShift`（struct 内 `+0x18`）
//     ⇒ 对象偏移 **`+0xD0`/`+0xD4`**（`dump.cs` 的 `CinemachineVirtualCamera` / `LensSettings` 实读；
//       与 `CombatCameraZoom.cs` 文件头 ⑥ 是**同一条判据、同一个偏移**）。
// ⇒ **等价物 = `CombatCameraZoom.virtualCameraLensShift`**，理由三条：
//   ① **同一个存储**：那件组件把它当 `vcam.m_Lens.LensShift` 的替身（它的文件头 ⑥ 就是这么定的），
//      量纲/语义/初值全部一致（场景序列化 `MonoBehaviour_4328.json` 的 `m_Lens.LensShift = (0.0, −0.205)`）；
//   ② **同一批读者**：`ApplyZoom` 从头到尾读它/写它；本件写它 ⇒ 下一帧 `ApplyZoom` 就把它推给真相机
//      （原版那一下是 Cinemachine 的 pipeline 干的 —— 我们显式调 `ApplyVirtualCameraLensShift()`，
//       也就是那件组件已经为「虚拟镜头 → 真相机」写好的那一跳）；
//   ③ **本件从别处拿不到第二个候选**：我们这一档没有第二个「虚拟镜头」概念。
//   ⇒ 于是 `CanLensShift`（见下）那对 get/set **就是** `b__12_0` / `b__12_1`。
//
// ============================ 🔴 我们做的三处「等价物」（⛔ 不当原版） ============================
// A. **「谁在抬 `ScreenResolutionChangeSignal`」那一格**：原版那条信号由 `Scaffold.Core.Events.Signal`
//    那套总线上别处发出来（本件只**注册**）。**我们没有那套总线**，而「是谁在发」在**全反编译里查不到**
//    （`grep -rl ScreenResolutionChangeSignal decomp_full` = **0** —— 那个类只有 `TypeDefIndex 2714` 与一个
//    共享空 ctor，没有任何 `.c` 按名字提到它；发动它的人只经 `TypeInfo` 引用，Ghidra 没跟到）。
//    ⇒ 等价物 = **本类自己比 `Screen.width/height`**：`Update()` 里存一份上一帧的屏宽高，变了就
//    `ResolutionHasChanged()`（= 原版那条信号的**效果**）。另留 `NotifyScreenResolutionChanged()` 显式抬法
//    （给将来接上真信号源 / 给自检）。**如实标：这一格是等价物，⛔ 不是「原版就是这样」。**
// B. **`vcam` 那一格** —— 见上面那一整段。
// C. **自检口**：`CompleteTweensForTest()`（批处理没有帧循环 ⇒ `DOTween` 不推进，「补间跑完」那档验不了）
//    · `InitializeCount` / `LastNewSensorSize` / `LastShiftY`（记「`Initialize` 真的算过」）
//    · 三个序列化字段的只读口。⛔ 生产路径一律不碰。
//
// ============================ 🔴 我们没做 / 做不到的（如实，⛔ 不冒充原版） ============================
// D. **`sensorSizeXSmall`（37.0）/ `sensorSizeXBigScreen`（41.0）在本 build 里【一个方法都不读】** ——
//    我把本件 9 个 `.c` 逐份看过：`+0x30`/`+0x34` **只在 `__.ctor.c` 里被写**，四个方法体、
//    四个闭包里**一次都没出现过**。⇒ 我们**保留这两个字段 + 保留它们的场景值**（这样字段表与原版逐格对上、
//    重烘场景时也不丢），但**不假装它们在起作用**：本件的**行为里没有它们**（原版也没有）。
// ⛔ 别「顺手」拿它俩去写个「小屏/大屏取 sensorSize」的分支 —— 那是**发明**，不是复刻。
// E. **`DoLensShift(float, bool)` 在本 build 里零调用点**（它的体被内联进了 `Initialize` 的后半段）。
//    照原版**留成 public 方法**（⛔ 不删：删了就对不上原版，且它是那半段的「同一个体」）。
// F. **`DOTween.Kill(...)` 那两句在原版是【空操作】** —— 如实记一条：`DOTween.To(getter, setter, …)` 造出来的
//    补间**没有 target**（`TweenerCore.target` 是 `null`），而 `DOTween.Kill(target)` 杀的是「**target 等于它**」
//    的补间 ⇒ `Kill(boardCamera)` / `Kill(vcam)` **杀不掉任何东西**。我们**照抄这两句**（原版行为 = 不杀），
//    ⛔ 不「顺手修好」（那会变成第二次 `Initialize` 不会补间打架 —— 与实况不符）。
//    ⇒ 副作用如实说：**连着调两次 `Initialize(instant: false)` 会留下两条并行的补间**，它们会互相抢那一格。
using UnityEngine;
using DG.Tweening;      // ⚠️ 必须 `using`：`SetEase` / `IsActive` / `Complete` / `Kill` 都是**扩展方法**
                        //（`TweenSettingsExtensions` / `TweenExtensions`），全限定名写法找不到它们。
                        // 原版那两句就是 `DG.Tweening.TweenSettingsExtensions.SetEase<TweenerCore<Vector2,Vector2,VectorOptions>>`
                        // （`script.json` 的 `ScriptMetadataMethod` 解出来的，见文件头 ⑤）。

namespace CardPresentation
{
    /// <summary>
    /// 「屏幕分辨率变了 ⇒ 重新取景」那条触发器（原版 `BattleCameraSreenSize`，TypeDefIndex 493）。
    /// <para>它把 `CameraVerticalFramer` 算出来的 <c>sensorSize</c>（写进 <c>boardCamera</c>）与
    /// <c>desiredLensShift.y</c>（写进虚拟镜头，见文件头「`vcam` 的等价物」）一步到位/补间地落下去，
    /// 并抬 <see cref="OnCameraSensorSizeChanged"/> / <see cref="OnCameraShiftChanged"/> ——
    /// 那两个的订阅方是 `CombatCameraZoom.Awake`（它 `+0x38` 那格就是本件）。</para>
    /// </summary>
    public class BattleCameraSreenSize : MonoBehaviour
    {
        // ==================================================================
        //  原版序列化字段（偏移 / ctor 值 / 场景值都有出处，见文件头 ②③④）
        // ==================================================================

        /// <summary>原版 `+0x20`。场景引用 = `Camera_1461`（= 那台透视的全屏 `BoardCamera`）。
        /// 本件的**唯一落点**：`boardCamera.sensorSize`（`b__11_1`）。</summary>
        public Camera boardCamera;

        /// <summary>= 原版 `+0x28 [SerializeField] CinemachineVirtualCamera vcam`（本工程没有 Cinemachine，
        /// 见文件头「`vcam` 的等价物」）。**这一格不存第二个副本** —— 读写都转发到
        /// <see cref="CombatCameraZoom.virtualCameraLensShift"/>（那就是它的替身）。</summary>
        [System.NonSerialized] public CombatCameraZoom vcamAsCombatCameraZoom;

        /// <summary>原版 `+0x30`。ctor `0x42140000` = **37.0**；场景 `37.0`（两边同）。
        /// 🔴 **本 build 里没有任何方法读它**（文件头 D）—— 保留只为字段表对齐。</summary>
        [SerializeField] float sensorSizeXSmall = 37f;

        /// <summary>原版 `+0x34`。🔴 **ctor `0x42260000` = 41.5，而场景里是 41.0**
        /// ⇒ 我们取**场景那一档**（文件头 ④，铁律 5·c）。同样**本 build 里没人读它**（文件头 D）。</summary>
        [SerializeField] float sensorSizeXBigScreen = 41f;

        /// <summary>原版 `+0x38`。ctor `0x40400000` = **3.0**；场景 `3.0`（两边同）。
        /// 用途 = 那两条 `DOTween.To` 的 **duration**（`b__11_*` / `b__12_*` 那条路；
        /// 实况走的是 `instant: true`，所以它平时不下场）。</summary>
        [SerializeField] float animTime = 3f;

        /// <summary>= 原版 `+0x40 [SerializeField] CameraVerticalFramer cameraVerticalFramer`。
        /// 🔴 我们**没有**那件组件：它的数学全在 <see cref="CombatAutoZoom"/>（那件公开
        /// <see cref="CombatAutoZoom.CalculateFraming"/>，就是原版 `CameraVerticalFramer.CalculateFraming`
        /// 的等价物 —— 同 `CombatCameraZoom.framer` 那一格的口径）。</summary>
        [System.NonSerialized] public CombatAutoZoom cameraVerticalFramer;

        /// <summary>原版 `+0x48 [SerializeField] CombatCameraZoom combatCameraZoom`。
        /// 本件只读它一格：`GetMaxZoomLevel(1.0f)`（`Initialize` 的第一句）。</summary>
        [System.NonSerialized] public CombatCameraZoom combatCameraZoom;

        /// <summary>原版 `+0x50 public Action&lt;Vector2&gt; OnCameraShiftChanged`。
        /// 抬的时机：`DoLensShift(..., instant: true)`（= 实况那条路）。
        /// 订阅方 = `CombatCameraZoom.OnCameraShiftChanged`（写它的 `targetOriginalLensShift` `+0x98`）。</summary>
        public System.Action<Vector2> OnCameraShiftChanged;

        /// <summary>原版 `+0x58 public Action&lt;Vector2&gt; OnCameraSensorSizeChanged`。
        /// ⚠️ 订阅方 `CombatCameraZoom.OnCameraSensorSizeChanged` 是个**空方法**（`RVA 0x4B33B0` 共享空桩）
        /// ⇒ 订了也不做事。**照原版保留、照原版抬**（它仍然是原版接口的一部分）。</summary>
        public System.Action<Vector2> OnCameraSensorSizeChanged;

        // ==================================================================
        //  原版的方法（逐句；出处见文件头 ①）
        // ==================================================================

        /// <summary>原版 `+0x38`/`+0x50` 那条 `DOTween` 的两个把手（自检要能把它们「跑完」，见文件头 C）。</summary>
        DG.Tweening.Tween _sensorTween, _lensTween;

        /// <summary>= 原版 `Start()`（`BattleCameraSreenSize__Start.c` 逐句）：
        /// <code>
        /// UIGenericEventCatcher.SourceDelegate d = new SourceDelegate(this, ResolutionHasChanged);
        /// Signal.Register&lt;ScreenResolutionChangeSignal&gt;(d);
        /// </code>
        /// 🔴 那套 `Scaffold.Core.Events.Signal` 总线我们没有（见文件头 A）⇒ 等价物 = 订到本类的静态事件上。
        /// 抬它的是 <see cref="NotifyScreenResolutionChanged"/> / `Update()` 那条屏宽高比对。
        /// <para>⚠️ 加了一条原版没有的幂等守卫（`Delegate.Combine` 不幂等，而批处理下
        /// `AddComponent` 跑不跑 `Start`/`Awake` 本工程**没有定论**；同 `CombatAutoZoom.AttachMinionEvent` 的理由）。</para></summary>
        void Start() { RegisterResolutionSignal(); }

        /// <summary>🆕 **我们加的**（原版那一句在 `Start()` 里，见它的注释）：
        /// 把 `ResolutionHasChanged` 订到分辨率信号上（**幂等**）。由 <see cref="CombatCameraZoom"/> 建本件时显式调一次
        /// —— 因为批处理下 `Start` 到底跑不跑没有定论，⛔ 不赌生命周期。</summary>
        public void RegisterResolutionSignal()
        {
            ScreenResolutionChanged -= ResolutionHasChanged;
            ScreenResolutionChanged += ResolutionHasChanged;
            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;
        }

        /// <summary>= 原版 `ResolutionHasChanged()`：**一句 `Initialize(instant: 1)`**
        /// （`BattleCameraSreenSize__ResolutionHasChanged.c` 就两行：`BattleCameraSreenSize__Initialize(param_1, 1, 0)`）。
        /// <para>⇒ **实况那条路是 `instant: true`**：一步到位、不补间、并且**会抬那两个 Action**；
        /// `animTime = 3.0` 那条补间路在本 build 里**没有调用点**（见 <see cref="Initialize"/>）。</para></summary>
        void ResolutionHasChanged() { Initialize(true); }

        /// <summary>= 原版 `Initialize(bool instant = false)`（`BattleCameraSreenSize__Initialize.c` 逐句）：
        /// <code>
        /// float zoom = combatCameraZoom.GetMaxZoomLevel(1.0f);                          // DAT_1834b2bb8 = 1.0
        /// cameraVerticalFramer.CalculateFraming(zoom, out newSensorSize, out desiredLensShift);
        /// // 前半：sensorSize
        /// if (!instant) { DOTween.Kill(boardCamera);
        ///                 DOTween.To(() =&gt; boardCamera.sensorSize, x =&gt; boardCamera.sensorSize = x,
        ///                            newSensorSize, animTime).SetEase(Ease.InOutCubic); }
        /// else          { OnCameraSensorSizeChanged?.Invoke(newSensorSize); }
        /// // 后半：透镜位移的 **.y**（x 被强制成 0 —— `(ulonglong)y &lt;&lt; 0x20`）
        /// var shift = new Vector2(0f, desiredLensShift.y);
        /// if (!instant) { DOTween.Kill(vcam);
        ///                 DOTween.To(() =&gt; vcam.m_Lens.LensShift, x =&gt; vcam.m_Lens.LensShift = x,
        ///                            shift, animTime).SetEase(Ease.InOutCubic); }
        /// else          { OnCameraShiftChanged?.Invoke(shift); }
        /// </code>
        /// <para>🔴 **x 归零不是我们简化**：两处 `DOTween.To` 的终值都是 `(ulonglong)y << 0x20`
        /// （低 32 位 = 0）—— 原版就长这样；`DoLensShift(float desiredLensShiftY, …)` 这个**参数名**
        /// 也印证它只传 y。</para>
        /// <para>⚠️ 原版在两个 `if` 之前各有一条空引用抛点（`FUN_1803f47a0`）：`combatCameraZoom == null` 或
        /// `cameraVerticalFramer == null` 时直接抛 `NullReferenceException`。我们改成**出声一次 + 早退**
        /// （每帧抛会刷屏；而这条只有「引用没接上」才走得到）。⛔ 不是语义差异：原版走到那儿已经崩了。</para></summary>
        public void Initialize(bool instant = false)
        {
            InitializeCount++;
            if (combatCameraZoom == null || cameraVerticalFramer == null)
            {
                if (!_refsMissingNoted)
                {
                    _refsMissingNoted = true;
                    string miss = combatCameraZoom == null && cameraVerticalFramer == null
                                ? "`combatCameraZoom` 与 `cameraVerticalFramer` 两个都"
                                : (combatCameraZoom == null ? "`combatCameraZoom`" : "`cameraVerticalFramer`");
                    Debug.LogError("[BattleCameraSreenSize] `Initialize` 的 " + miss + " 没接上 ⇒ **取景没重算、"
                                 + "一个字节也没落下去**（原版这里是空引用抛点，`FUN_1803f47a0`）。"
                                 + "本件由 `CombatCameraZoom.Initialize` 建出来并接引用，见那里的注释。");
                }
                return;
            }

            // 第一句：`GetMaxZoomLevel(1.0f)`（实参 = `DAT_1834b2bb8` = 1.0f —— 就是那个「不缩放」档；
            // 它含 `smallScreenUI` 上夹，所以小屏档下这里拿到的可能 < 1）。
            float zoom = combatCameraZoom.GetMaxZoomLevel(1f);

            Vector2 newSensorSize, desiredLensShift;
            if (!cameraVerticalFramer.CalculateFraming(zoom, out newSensorSize, out desiredLensShift))
            {
                // 那件取景器没有 3D 相机（它自己已经出声一次）⇒ 落点没有意义，如实早退。
                Debug.LogWarning("[BattleCameraSreenSize] 取景器算不出来（`CalculateFraming` 回了 false）"
                               + " ⇒ 本次分辨率变化**没有落到相机上**（原因见它的那一条日志）");
                return;
            }
            LastNewSensorSize = newSensorSize;
            LastShiftY = desiredLensShift.y;

            // ---- 前半：sensorSize（终值 = `local_res8`）----
            if (!instant)
            {
                // 🔴 这一句在原版是**空操作**（见文件头 F）—— 照抄，⛔ 别修。
                if (boardCamera != null) DG.Tweening.DOTween.Kill(boardCamera);
                if (boardCamera != null)
                {
                    _sensorTween = DG.Tweening.DOTween
                        .To(() => boardCamera.sensorSize,
                            x => boardCamera.sensorSize = x,
                            newSensorSize, animTime)
                        .SetEase(DG.Tweening.Ease.InOutCubic);
                }
            }
            else
            {
                var h = OnCameraSensorSizeChanged;
                if (h != null) h(newSensorSize);
            }

            // ---- 后半：透镜位移的 **.y**（x 强制 0）；终值 = `(ulonglong)y << 0x20` ----
            var shift = new Vector2(0f, desiredLensShift.y);
            if (!instant)
            {
                if (vcamAsCombatCameraZoom != null) DG.Tweening.DOTween.Kill(vcamAsCombatCameraZoom);  // 同「空操作」，见文件头 F
                _lensTween = DG.Tweening.DOTween
                    .To(() => VcamLensShift,
                        x => VcamLensShift = x,
                        shift, animTime)
                    .SetEase(DG.Tweening.Ease.InOutCubic);
            }
            else
            {
                var h = OnCameraShiftChanged;
                if (h != null) h(shift);
            }
        }

        /// <summary>= 原版 `DoLensShift(float desiredLensShiftY, bool instant)`（逐句）。
        /// <para>🔴 **本 build 零调用点**（文件头 E）：它的体与 <see cref="Initialize"/> 的**后半段逐句相同**
        /// （同样的闭包 `b__12_0/b__12_1`、同样的 `DOTween.Kill(vcam)`、同样的 `(ulonglong)y << 0x20`）
        /// ⇒ 是编译器把 `Initialize` 末尾那段**内联**了一份。照原版**留成 public 方法**，⛔ 不删。
        /// <see cref="Initialize"/> 后半段**不**转调它 —— 那会让「谁是哪一份」变成两处判据（原版是两份独立代码）。</para></summary>
        public void DoLensShift(float desiredLensShiftY, bool instant)
        {
            var shift = new Vector2(0f, desiredLensShiftY);
            if (!instant)
            {
                if (vcamAsCombatCameraZoom != null) DG.Tweening.DOTween.Kill(vcamAsCombatCameraZoom);
                _lensTween = DG.Tweening.DOTween
                    .To(() => VcamLensShift,
                        x => VcamLensShift = x,
                        shift, animTime)
                    .SetEase(DG.Tweening.Ease.InOutCubic);
            }
            else
            {
                var h = OnCameraShiftChanged;
                if (h != null) h(shift);
            }
        }

        // ==================================================================
        //  等价物 A：那条分辨率信号（我们没有 `Scaffold.Core.Events.Signal` 总线，见文件头 A）
        // ==================================================================

        /// <summary>= 原版 `ScreenResolutionChangeSignal` 的等价物（静态事件）。
        /// 原版那套总线的抬法**在全反编译里查不到**（文件头 A）⇒ 抬它的两条路：
        /// <see cref="NotifyScreenResolutionChanged"/>（显式，给自检 / 给将来接上真信号源）与
        /// `Update()` 里的屏宽高比对（等价物，见那处的注释）。</summary>
        public static event System.Action ScreenResolutionChanged;

        int _lastScreenW = -1, _lastScreenH = -1;

        /// <summary>抬一次「屏幕分辨率变了」（= 原版那条 signal 被发出来）。
        /// ⛔ 生产路径**别手动调** —— 生产那条是 `Update()` 的屏宽高比对。</summary>
        public static void NotifyScreenResolutionChanged()
        {
            var h = ScreenResolutionChanged;
            if (h != null) h();
        }

        /// <summary>🆕 **等价物 A 的「谁在抬」那一半**（原版由信号总线抬，我们查不到那个发动者 ⇒ 自己比屏宽高）。
        /// <para>判据只有一句：`Screen.width/height` 与上一帧不同。⚠️ 原版**没有**这一段代码
        /// —— 它是「原版那条我们查不到的信号源」的等价物，⛔ 别读成「原版就是这么做的」。</para>
        /// <para>⚠️ **另加一条原版没有的守卫**：`_lastScreenW &lt; 0`（还没立基线）时**只立基线、不抬**
        /// —— 不然第一次 `Tick` 就会把「基线还没立」当成「分辨率变了」，白抬一次。</para></summary>
        void Update() { Tick(); }

        /// <summary>框架那一步（= 上面那个 `Update` 的体；`deltaTime` 无关，提出来只为自检能直调 ——
        /// 同 `CombatCameraZoom.Tick` / `Shell/PointerLayer.TickAt` 的先例）。</summary>
        public void Tick()
        {
            if (_lastScreenW < 0) { _lastScreenW = Screen.width; _lastScreenH = Screen.height; return; }
            if (Screen.width == _lastScreenW && Screen.height == _lastScreenH) return;
            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;
            NotifyScreenResolutionChanged();
        }

        // ==================================================================
        //  `vcam` 的等价物（见文件头那一整段；那两个 get/set 就是 `b__12_0` / `b__12_1`）
        // ==================================================================

        /// <summary>= 原版闭包 `&lt;DoLensShift&gt;b__12_0`（读 `vcam.m_Lens.LensShift`，对象偏移 `+0xD0`）。
        /// 我们的替身 = <see cref="CombatCameraZoom.virtualCameraLensShift"/>（文件头那一整段给了三条理由）。</summary>
        public Vector2 VcamLensShift
        {
            get { return vcamAsCombatCameraZoom != null ? vcamAsCombatCameraZoom.virtualCameraLensShift : Vector2.zero; }
            set
            {
                if (vcamAsCombatCameraZoom == null) return;
                vcamAsCombatCameraZoom.virtualCameraLensShift = value;
                // 原版那一下「虚拟镜头 → 真相机」在 Cinemachine 的 pipeline 里（每帧一次）；
                // 我们显式做掉 —— 就是那件组件已经为这一跳写好的 `ApplyVirtualCameraLensShift`。
                vcamAsCombatCameraZoom.ApplyVirtualCameraLensShift();
            }
        }

        // ==================================================================
        //  等价物 C：自检口（⛔ 生产路径一律不碰）
        // ==================================================================

        /// <summary>`Initialize` 跑了几次（自检用：钉「信号真的抬到了」）。</summary>
        public int InitializeCount { get; private set; }
        /// <summary>上一次 `Initialize` 从取景器拿到的 `newSensorSize`（自检用）。</summary>
        public Vector2 LastNewSensorSize { get; private set; }
        /// <summary>上一次 `Initialize` 拿到的 `desiredLensShift.y`（自检用：钉「x 被强制成 0」的另一半）。</summary>
        public float LastShiftY { get; private set; }

        /// <summary>`sensorSizeXSmall`（原版 `+0x30`，场景 37.0；⛔ 本 build 无人读，见文件头 D）。</summary>
        public float SensorSizeXSmallTest { get { return sensorSizeXSmall; } }
        /// <summary>`sensorSizeXBigScreen`（原版 `+0x34`）—— 🔴 取**场景那一档 41.0**，ctor 是 41.5。</summary>
        public float SensorSizeXBigScreenTest { get { return sensorSizeXBigScreen; } }
        /// <summary>`animTime`（原版 `+0x38`，ctor 与场景都是 3.0）。</summary>
        public float AnimTimeTest { get { return animTime; } }
        /// <summary>「补间**对象**建出来了没有」（自检用）。
        /// 🔴 特意与 <see cref="HasLiveTweenForTest"/> 分开：这一条只看「`DOTween.To` 真被调到了」
        /// （`!= null`），**不**依赖 DOTween 的运行时状态 —— 批处理里 DOTween 有没有「活着」是另一件事
        /// （没有帧循环 ⇒ 默认 `UpdateType.Normal` 不推进）。⛔ 别把两者混成一条判据。
        /// <para>⚠️ 判据用 `!= null` 而**不是** Unity 的「假 null」那一套：`DG.Tweening.Tween` 不是
        /// `UnityEngine.Object` ⇒ 它就是普通的引用判空。</para></summary>
        public bool TweenCreatedForTest { get { return _sensorTween != null && _lensTween != null; } }

        /// <summary>「补间那条路上有没有**活着**的东西」（自检用；批处理下 DOTween 不推进 ⇒ 只当参考量）。</summary>
        public bool HasLiveTweenForTest
        {
            get
            {
                return (_sensorTween != null && _sensorTween.IsActive())
                    || (_lensTween != null && _lensTween.IsActive());
            }
        }

        /// <summary>🆕 自检口：把那两条补间**推到终点**（批处理没有帧循环 ⇒ DOTween 默认的
        /// `UpdateType.Normal` 不会自己推进）。
        /// <para>手法照本仓既有的那一套（判据 = `Editor/DOTweenSmokeTest.cs` 的文件头）：
        /// 先 `SetUpdate(UpdateType.Manual)`、再 `DOTween.ManualUpdate(dt)`；末尾再补一个 `Complete()`
        /// 兜底（两路都推到**同一个终值** —— 补间的终值只有一个，⛔ 不是我们另算一遍）。</para>
        /// <para>⚠️ 生产路径**不调**它：原版那两条补间就是默认更新模式。</para></summary>
        public void CompleteTweensForTest()
        {
            if (_sensorTween != null && _sensorTween.IsActive()) _sensorTween.SetUpdate(DG.Tweening.UpdateType.Manual);
            if (_lensTween != null && _lensTween.IsActive()) _lensTween.SetUpdate(DG.Tweening.UpdateType.Manual);
            float step = animTime + 0.1f;
            DG.Tweening.DOTween.ManualUpdate(step, step);
            if (_sensorTween != null && _sensorTween.IsActive()) _sensorTween.Complete();
            if (_lensTween != null && _lensTween.IsActive()) _lensTween.Complete();
        }

        /// <summary>🆕 自检口：把自己那两条补间杀掉（探针收工时用；原版那句 `DOTween.Kill` 是空操作，见文件头 F）。</summary>
        public void KillTweensForTest()
        {
            if (_sensorTween != null) { _sensorTween.Kill(); _sensorTween = null; }
            if (_lensTween != null) { _lensTween.Kill(); _lensTween = null; }
        }

        /// <summary>🆕 自检口：把「上一帧屏宽高」钉成给定值（不钉住的话 `Update` 那条比对跟着真屏走）。</summary>
        public void SetScreenSizeBaselineForTest(int w, int h) { _lastScreenW = w; _lastScreenH = h; }

        /// <summary>🆕 自检口：那条静态信号上现在**挂着几个订阅者**（探针收工要自证「一个不剩」——
        /// 不然会留一个指向已销毁组件的委托，下一次抬信号就 NRE）。</summary>
        public static int ResolutionSignalSubscriberCountForTest
        {
            get
            {
                var h = ScreenResolutionChanged;
                return h == null ? 0 : h.GetInvocationList().Length;
            }
        }

        bool _refsMissingNoted;

        /// <summary>🆕 **我们加的**（⛔ 原版**没有** `OnDestroy` —— 它那 4 个方法就是 `Start` / `ResolutionHasChanged` /
        /// `Initialize` / `DoLensShift`）：原版那条静态信号的生命周期由 `Scaffold.Core.Events.Signal` 总线自己管，
        /// **我们没有那套**（见文件头 A）⇒ 必须自己把订阅摘掉。
        /// <para>不摘的后果是**静默的**：`ScreenResolutionChanged` 是个静态事件，会一直攥着一个已销毁的组件；
        /// 下一次别处抬信号时进那个组件的 `Initialize` ⇒ 只会在日志里冒一条「引用没接上」。
        /// ⛔ 这不是新增语义，是**等价物 A 的收尾**（`RegisterResolutionSignal` 的对称句）。</para></summary>
        void OnDestroy() { UnregisterResolutionSignal(); }

        /// <summary>🆕 把 `ResolutionHasChanged` 从信号上摘下来（自检收工时也调；`OnDestroy` 里同一条）。</summary>
        public void UnregisterResolutionSignal() { ScreenResolutionChanged -= ResolutionHasChanged; }
    }
}
