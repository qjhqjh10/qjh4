// CombatAutoZoom.cs — 「Auto Zoom」那一格的**消费者**（原版组件 `CombatAutoZoom`，`dump.cs` 的 `TypeDefIndex 669`）
//
// ============================ 这是 A175 的主体 ============================
// 在那之前：`Shell/SettingsWindow.cs` 的 `AutoZoom`（= 原版 `GameStaticData.useCombatAutoZoom` +0x125）
//   **只存值、点了不产** —— `ToggleAutoZoom` 里挂的是一句 `LogWarning`「我们没做那个组件」。
// 现在补齐：**开关真起作用**（关着 = 不缩放；开着 = 按原版 `unitsZoomCurve` 的条件缩放战场相机取景）。
//
// ============================ 判据（全是实读，没有一个是推的） ============================
// ① **方法体（第一权威）** = `d:/2/tools/decomp_full/CombatAutoZoom__{ctor, Initialize, OnEnable, OnDisable,
//    OnMinionNumberChanged, SetZoomLevel, ResetCameraZoomUIAction, ForceRefresh}.c`
//    （8 个方法体；`ResetCameraZoomUIAction` 与 `ForceRefresh` **同一个 RVA 0x60CA80** ⇒ 原版这两条**逐字节同体**，
//     `dump.cs` 也把两条都列在同一个 `VA` 上）；
// ② **字段名/偏移** = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CombatAutoZoom.cs`（签名桩）+
//    `d:/2/tools/il2cpp_out/dump.cs`（`// 0x20` 那几行）；
// ③ **`SetZoomLevel` 里那两个被反编译器吃掉的操作数**（`Evaluate` 的实参 / 给 setter 的值）—— **读指令流**读出来的：
//    `工具/disasm_va.py <dll> 0x18060ce40 90`（`CombatAutoZoom$$SetZoomLevel` = VA `0x18060ce40`）：
//      `cmp byte [rax+0x125],0` → `useCombatAutoZoom`；真 ⇒ `cvtdq2ps xmm1, esi`（**人数转 float**）→ `Evaluate`；
//      假 ⇒ `movss xmm6,[rip+…]`（**常量 = `DAT_1834b2bb8` = 1.0**）；
//      然后 `Clamp(zoom, 0, DAT_1834b2bb8)`（`comiss` 两条）→ `set_TargetZoomLevel`；
// ④ **`unitsZoomCurve` 的 5 个键** = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena*/MonoBehaviour/`
//    里 13 个战场各一份（`MonoBehaviour_5231.json` 等），🔴 **13/13 逐字节相同**（md5 `ca085afd`，
//    `useTestMode 0` / `testNumberOfUnits 4` / `m_PreInfinity = m_PostInfinity = 2`(Clamp) 也全同）
//    ⇒ **不存在「按场/按状态另一套曲线」**（铁律 5·c 要的「状态 → 参数」表在这件事上是退化的一格）；
// ⑤ **`smallScreenUI` 那一层夹取**（`CombatCameraZoom.set_TargetZoomLevel` 的自变量）=
//    `maxZoomForSmallScreenDevicesByAspectRatio`，同样 **13/13 逐字节相同**（`MonoBehaviour_4404.json`）；
// ⑥ **调用时机** = `BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c:232` 调 `CombatAutoZoom.Initialize()`
//    （换牌结束、真开打那一刻；教学那条 `_TutorialStartSequence` 也是同一个位置）；
// ⑦ **谁抬 `OnMinionAddOrRemoved`** = `MinionManager__RefreshOccupationSlots.c`（该静态委托在**全反编译里只有这一处 Invoke**）；
// ⑧ **取景落点** = `CameraVerticalFramer__CalculateFraming.c` 的 `lensShift.y` 一跳 —— 这一条**不是新写的**：
//    它就是 `Editor/BattleScene.cs` 里那个 `BoardFramer`（2026-09-20 接上、有断言盯着），本文件只是把它
//    **从「只认 zoom = 1」推广成「认 zoom」**，并把判据**收成一处**（`BoardFramer` 现在转调这里，见那边的注释）。
// ⑨ 🔴 **2026-10-12（A444）订正**：取景那三条曲线（`viewShiftModifier` / `verticalPaddingByZoom` /
//    `verticalPaddingModifierByAspectRatio`）原来**抄错 + 截断**了 —— 四段切线整批是错的、`PadMod` 那两个
//    切线**连符号都反了**、末键 `time` 少了几位、若干 `value` 被截到 5~7 位小数。
//    判据 = `MonoBehaviour_4697.json`（13/13 场逐字节相同）；**新旧两份解包逐值相同 ⇒ 是抄错、不是解包差异**。
//    错值 → 应有值逐条写在下面「曲线数据」那一节的头注里。⚠️ **改完必须重烘 `Battle.unity`**（见那边）。
//    （顺带把 `maxZoomForSmallScreenDevicesByAspectRatio` 同类的截断也换成原版字面量。）
//
// ============================ 🔴 我们没做的那几件（如实，⛔ 不冒充原版） ============================
// A. ✅ **原版 `[SerializeField] CombatCameraZoom combatCameraZoom`(0x30) 那一整件 —— 2026-10-12（A422）做了**，
//    新建 `Battle/CombatCameraZoom.cs`（手动缩放（滚轮/右键拖拽）· 世界边界 · `SmoothDamp` 平滑 · 回弹 ·
//    `sensorSize` 落点 · `Initialize` 反解开局 zoom，26 个方法体逐句复刻）。
//    **本文件现在只做两件事**：① 当它的**取景器**（= 原版 `CameraVerticalFramer` 那件，见 `CalculateFraming`）；
//    ② 把 `manualCamera` / `targetZoomLevel` 两格**转发**给它（原版这两格**本来就是那件组件的字段**）。
//    ⛔ **别再在本类里另存一份状态** —— 两份 = 「两处写同一条规则」（工程红线）。
//    ⚠️ `boardCamera` 这个字段仍然保留：它是**两件共用的那台相机**（原版 `CombatCameraZoom.targetCamera`）。
// B. ✅ **`sensorSize.x`（`cameraSizeXTable` 那半）** —— **2026-10-12（A421）接上了**，见本文件
//    「A421」那一节。原来那句老结论「zoom = 1 时该式恒等 ⇒ 16:9 下不生效，只有窄屏才要它」**是错的**
//    （A175 起 `zoom` 会真的走到 0）⇒ 那一半**会**影响画面，判据与实现都在下面。
//    判据（全是实读）：曲线 `cameraSizeXTable` = `framer+0x38`（`MonoBehaviour_4697.json`，**6 键**，
//    13/13 场逐字节相同）· `maxVerticalSizeInViewPort` = `framer+0x78` = **7.0**（13/13 相同）·
//    那一段的指令流 = `CameraVerticalFramer$$CalculateFraming` VA `0x1806085D0` 的
//    `0x180608B46`(abs)→`0x180608B69`(Min)→`0x180608B7C`(写 `+0x7c`)→`0x180608BB0`(取 `+0x38`)
//    →`0x180608BD7`(`get_sensorSize`)→`0x180608C0E`(写回 `*param_3`)。
// C. ✅ **HUD 那颗「重置自动镜头」钮 —— 2026-10-12（A423）做了**（原版 `BattleHud.resetCameraZoomButton` `+0xa8`，
//    节点 `CenterCameraButton`）：图与位置**本来就摆对了**，接上的是**行为**（显隐 + 点击 → 这条 `Action`）。
//    ⚠️ **2026-10-12 更正**：这里原来写「全仓 `ResetCamera` 0 命中 ⇒ **我们 HUD 里没有那颗钮**」——
//    那半句说的是**链**不在（确实：驱动里既没引用也没点击判定），**不是「图不在」**
//    （`Editor`→`Battle/BattleDriver.cs` 的 `BuildHud` 里 `HudAbs(…, "CenterCameraButton")` 一直都在）。
//    落点现在 = `BattleDriver.ToggleCameraResetButton`（钩子在 `BattleDriver.SetupAutoZoom` 里接上）；
//    点击 = `BattleDriver.ResetCameraZoomClick` → `RaiseResetCameraZoom()`。
// D. ✅ **平滑做了**（A422 起）：`CombatCameraZoom.ApplyZoom` 每帧 `SmoothDamp`（`smoothTime = Time.deltaTime × 12.5`）
//    —— 逐句原版。⚠️ **批处理里没有帧循环** ⇒ 那条路验不了；自检/开局走的是
//    `CombatCameraZoom.SettleFraming()`（= 「把这一帧当成已经收敛完」，出处见那件组件的文件头 A）。
// E. **`useTestMode` / `testNumberOfUnits` 是死字段** —— 原版 8 个方法体里**一个都没读**它们
//    （全反编译里 `+0x20` / `+0x24` 没有任何读取点）⇒ 我们**照抄字段与出厂值、不发明用途**（自检只钉住「抄对了」）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 战场相机的**自动缩放**（原版 `CombatAutoZoom`）。
    /// <para>一句话：**场上人多 ⇒ 拉回全景；人少 ⇒ 收进取景**。人数 = `min(左半, 右半)` 传来的那个「较忙那半」，
    /// 曲线 `unitsZoomCurve` 把人数映成 zoom（`≤2 → 0` · `3 → 0.1053` · `≥4 → 1`），
    /// zoom = 1 就是「不缩放」（= 我们烘进场景那套取景）。</para>
    /// </summary>
    public class CombatAutoZoom : MonoBehaviour
    {
        // ==================================================================
        //  原版序列化字段（偏移 = `dump.cs` 的 `// 0x..`）
        // ==================================================================

        /// <summary>原版 `+0x20`。🔴 **死字段**（见文件头 E）—— 照抄，不发明用途。</summary>
        [SerializeField] bool useTestMode;

        /// <summary>原版 `+0x24`，`[Range(0, 4)] int`。出厂值 **4** = `CombatAutoZoom__ctor.c` 的第一句
        /// （`*(undefined4 *)(param_1 + 0x24) = 4;`）。🔴 **死字段**（见文件头 E）。</summary>
        [SerializeField, Range(0f, 4f)] int testNumberOfUnits = 4;

        /// <summary>原版 `+0x28`。13 个战场各序列化了一份、**逐字节相同**（文件头 ④）⇒ 我们这份在
        /// <see cref="Curve"/> 里按同一组键建出来（本组件是**运行时 `AddComponent`**，没有序列化来源）。</summary>
        [SerializeField] AnimationCurve unitsZoomCurve;

        /// <summary>3D 战场那台相机（= `BattleDriver.boardCam`）。**两件共用的一台**：
        /// 本类拿它当取景器的 `boardCamera`，`CombatCameraZoom` 拿它当 `targetCamera`（原版那一格；
        /// `MonoBehaviour_4404.json` 的 `targetCamera` = 场景里那台 `BoardCamera`）。
        /// ⚠️ 原版本类这一格是 `[SerializeField] CombatCameraZoom combatCameraZoom`(+0x30) —— 那个引用现在就是
        /// <see cref="_cameraZoom"/>（见文件头 A）。</summary>
        public Camera boardCamera;

        // ==================================================================
        //  原版私有状态
        // ==================================================================

        /// <summary>原版 `currentEnemySize`（`+0x38`）。`isPLayer == false` 时被写。</summary>
        int currentEnemySize;
        /// <summary>原版 `currentPlayerSize`（`+0x3c`）。`isPLayer == true` 时被写。</summary>
        int currentPlayerSize;

        /// <summary>= 原版 `CombatCameraZoom`（TypeDefIndex 670）那一整件 —— **A422（2026-10-12）起它真的在**。
        /// <para>🔴 **为什么 `targetZoomLevel` / `manualCamera` 不再住在本类**：原版它们是**那件组件的字段**
        /// （`+0x84` / `+0x30`），本类只是通过 `TargetZoomLevel` 的 setter 与直接赋值去改它们
        /// （`CombatAutoZoom__SetZoomLevel.c` 逐句）。两份各存一份 = 「两处写同一条规则」（工程红线）
        /// ⇒ 本类只留转发口（<see cref="ZoomLevel"/> / <see cref="manualCamera"/>）。</para>
        /// <para>⭐ 两件**互相引用**：本类要它落点，它要本类当取景器（原版那一格是 `CameraVerticalFramer`，
        /// 而那条数学链的判据收在本类里，见 <see cref="CalculateFraming"/>）。</para></summary>
        CombatCameraZoom _cameraZoom;

        /// <summary>那件组件（惰性建）。⚠️ **必须先设好 <see cref="boardCamera"/> 再取它** ——
        /// `BattleDriver.SetupAutoZoom` 就是这个次序（先赋相机、再 `ResetForBattle`）。</summary>
        CombatCameraZoom CameraZoom
        {
            get
            {
                if (_cameraZoom == null)
                {
                    _cameraZoom = gameObject.AddComponent<CombatCameraZoom>();
                    _cameraZoom.framer = this;                       // = 原版 `cameraVerticalFramer`(+0x50)
                    _cameraZoom.targetCamera = boardCamera;          // = 原版 `targetCamera`(+0x20)
                    _cameraZoom.ToggleResetCameraZoomUi = RaiseToggleResetCameraZoomUi;   // = 原版 `BattleHud.ToggleResetAutoCameraZoom`
                    // 开局那一格：原版是场景里序列化的 `virtualCamera.m_Lens.LensShift = (0.0, −0.205)`
                    //   （`MonoBehaviour_4328.json`）；我们烘进场景的是 `(0, BoardLensShiftY())`
                    //   ⇒ 取「不缩放那一档」，见 `ResetForBattle`（`Initialize` 的反解依赖它）。
                }
                return _cameraZoom;
            }
        }

        /// <summary>那件组件（自检/驱动用；⛔ 只读，别拿它当第二条写入口）。</summary>
        public CombatCameraZoom CameraZoomForTest { get { return _cameraZoom; } }

        /// <summary>= 原版 `BattleHud.Instance.ToggleResetAutoCameraZoom(bool active)` 在我们这一档的落点
        /// （`CombatCameraZoom` 的三处调用点转发过来：`LateUpdate` / `SetZoomLevel(force:true)` / `ToggleManualCameraControl`）。
        /// ✅ **2026-10-12（A423）起真的接上了**（`BattleDriver.SetupAutoZoom` 把 <see cref="ToggleResetCameraZoomUi"/>
        /// 接成 `BattleDriver.ToggleCameraResetButton`）—— 那颗钮在 `BuildHud` 里建出来、开局关着、
        /// 玩家一动镜头就出现。⚠️ 钩子**没人接**时才出声一次（那是「不在战斗里」的档，⛔ 不是正常路）。</summary>
        void RaiseToggleResetCameraZoomUi(bool active)
        {
            if (ToggleResetCameraZoomUi != null) { ToggleResetCameraZoomUi(active); return; }
            if (!_resetUiMissingNoted)
            {
                _resetUiMissingNoted = true;
                Debug.Log("[AutoZoom] 原版 `BattleHud.ToggleResetAutoCameraZoom(" + active + ")"
                        + "（显隐「重置自动镜头」那颗钮 + `DOPunchScale`）—— ⚠️ **本局没人接这条钩子** ⇒ 这一句只出声、"
                        + "不产生画面效果（正常战斗里 `BattleDriver.SetupAutoZoom` 会把它接成 `ToggleCameraResetButton`；"
                        + "走到这里 = 那颗钮没建出来，多半是 HUD 那棵树没建）");
            }
        }
        bool _resetUiMissingNoted;

        // ==================================================================
        //  自检口（⛔ 只读，不给生产用）
        // ==================================================================

        /// <summary>原版 `currentEnemySize`（自检用）。</summary>
        public int CurrentEnemySize { get { return currentEnemySize; } }
        /// <summary>原版 `currentPlayerSize`（自检用）。</summary>
        public int CurrentPlayerSize { get { return currentPlayerSize; } }
        /// <summary>现在的 zoom 目标值（自检用；= 原版 `CombatCameraZoom.targetZoomLevel` `+0x84`）。
        /// 关着那一档**恒为 1**（= 不缩放）。组件还没建起来时返回**不缩放档** —— 那是我们烘进场景的那一档
        /// （原版零初始化是 0，但 `Initialize` 的反解在我们这一档给的就是 1，见文件头 A）。</summary>
        public float ZoomLevel { get { return _cameraZoom != null ? _cameraZoom.TargetZoomLevel : OffZoomLevel; } }

        /// <summary>= 原版 `CombatCameraZoom.manualCamera`（`+0x30`）的**转发口**（A422 起状态住在
        /// `Battle/CombatCameraZoom.cs`，见 <see cref="_cameraZoom"/>）。真 = 玩家手动控着镜头，自动缩放**不碰**它
        /// （那道 `if (!manualCamera)` 在 `CombatCameraZoom` 里）。
        /// ⚠️ 组件还没建时**读**给 `false`（= 原版零初始化）；**写**会把它建出来（要先设好 `boardCamera`）。</summary>
        public bool manualCamera
        {
            get { return _cameraZoom != null && _cameraZoom.ManualCamera; }
            set { CameraZoom.ManualCamera = value; }
        }
        /// <summary>`OnMinionNumberChanged` 被抬了几次（自检用：钉「触发条件真的接上了」）。</summary>
        public int ZoomEventCount { get; private set; }
        /// <summary>取景真的写进相机几次（自检用：钉「写了」而不只是「算了」）。</summary>
        public int FramingApplyCount { get; private set; }
        /// <summary>`useTestMode` 的读口（自检钉「照抄了」；原版没有任何行为读它）。</summary>
        public bool UseTestMode { get { return useTestMode; } }
        /// <summary>`testNumberOfUnits` 的读口（同上）。</summary>
        public int TestNumberOfUnits { get { return testNumberOfUnits; } }

        // ==================================================================
        //  事件
        // ==================================================================

        /// <summary>= 原版 `MinionManager.OnMinionAddedOrRemoved`（`public static Action&lt;int, bool&gt;`，
        /// **静态字段偏移 `0x0`** —— `dump.cs` 的 `MinionManager` 第一格）。
        /// <para>原版的**唯一 Invoke 点** = `MinionManager__RefreshOccupationSlots.c`
        /// （`InsertMinion` / `RemoveMinion` 都调它）⇒ 载荷 = **`Mathf.Max(左半 Count, 右半 Count)`**
        /// （那一段逐句：先把两半占位表刷完，再 `iVar6 = 右半.Count; iVar7 = 左半.Count; if (iVar6 &lt;= iVar7) iVar6 = iVar7;`
        /// 然后 `Invoke(iVar6, isPLayer)`）+ `isPLayer`（= 「这个 `MinionManager` 是不是本地玩家那一个」）。</para>
        /// <para>⚠️ 那个 max 是**原版的行为**，不是我们简化：`MinionManager` 里 `leftMinions`(0x60) / `rightMinions`(0x68)
        /// 是**同一个玩家督军左右两半**（`InsertMinion.c`：`slot &lt; 1` 进左表、`≥ 1` 进右表 ⇒ 督军格 0 进左表会算出 −1 下标
        /// ⇒ **督军本来就不在这两条表里**）。我们抬的时候用的是同一个口径：
        /// `BoardSlots.CountOnSide(ps, Left/Right)`（那是全工程唯一的「这一侧几个人」判据，督军格不算）。</para>
        /// <para>抬的人 = `BattleDriver.TickAutoZoom`（原版是 `MinionManager.RefreshOccupationSlots`）。</para></summary>
        public static event System.Action<int, bool> OnMinionAddedOrRemoved;

        /// <summary>抬一次事件（只给 `BattleDriver` 用；⛔ 别在别处再抬 —— 那会变成第二条人数来源）。</summary>
        public static void RaiseMinionAddedOrRemoved(int numberOfMinionsInSide, bool isPlayer)
        {
            var h = OnMinionAddedOrRemoved;
            if (h != null) h(numberOfMinionsInSide, isPlayer);
        }

        /// <summary>= 原版 `BattleHud.ResetCameraZoom`（`public Action`，`+0xc0`）：玩家点 HUD 那颗
        /// 「重置自动镜头」钮时该跑的东西。✅ **2026-10-12（A423）起那条链真的通了** ——
        /// 钮（`BattleDriver._cameraResetBtn`）的点击走 `BattleDriver.ResetCameraZoomClick` →
        /// <see cref="RaiseResetCameraZoom"/>（= 原版 `BattleHud.DoResetCameraZoom()`）。</summary>
        public System.Action ResetCameraZoom;

        /// <summary>= 原版 `BattleHud.DoResetCameraZoom()`（那颗钮的点击处理器：把上面那条 `Action` Invoke 出来）。</summary>
        public void RaiseResetCameraZoom()
        {
            var a = ResetCameraZoom;
            if (a != null) a();
        }

        /// <summary>原版 `SetZoomLevel(force: true)` 里那句 `BattleHud.Instance.ToggleResetAutoCameraZoom(false)`
        /// 的落点（显隐那颗「重置」钮 + `DOPunchScale`）。✅ **2026-10-12（A423）起挂上了**
        /// （`BattleDriver.SetupAutoZoom` → `BattleDriver.ToggleCameraResetButton`）；
        /// 钩子为空时**第一次走到时出声一次**（不许静默失败）。</summary>
        public System.Action<bool> ToggleResetCameraZoomUi;

        bool _noCameraNoted;
        bool _initialized;
        bool _minionHooked;

        // ==================================================================
        //  原版的 8 个方法（逐句）
        // ==================================================================

        /// <summary>= 原版 `CombatAutoZoom.Initialize()`：把自己挂到 HUD 那条「重置镜头」`Action` 上。
        /// <para>原版链路（`CombatAutoZoom__Initialize.c` 逐句）：`BattleHud.Instance`(`get_Instance`) →
        /// `UIGenericEventCatcher.SourceDelegate` 的 `.ctor(this, ResetCameraZoomUIAction)` →
        /// `Delegate.Combine(实例那条 Action, 新的)` → 写回 `BattleHud` 的 `+0xc0` 字段。</para>
        /// <para>⚠️ **加了一条原版没有的幂等守卫**：`Delegate.Combine` **不幂等**，原版靠「一局只叫一次」
        /// （换牌结束 / 教学开始，两条互斥的路）保证不重复订阅；我们的 `BattleDriver.Begin` 会被调很多次
        /// （重开一局、自检里十几趟）⇒ 这里显式守卫。</para></summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            ResetCameraZoom -= ResetCameraZoomUIAction;   // 守卫的第一半（见上面 ⚠️）
            ResetCameraZoom += ResetCameraZoomUIAction;
            // 🆕 A422：原版这两句是**挨着的** —— `BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c:229`
            //   先 `CombatCameraZoom.Initialize(matchType != 100, …)`、`:232` 再 `CombatAutoZoom.Initialize()`。
            //   我们那件只是被建出来（`Awake` 里 `enabled = false`）⇒ 这里打开它 + 做那句「反解开局 zoom」。
            //   ⚠️ 实参 = `true`：原版传的是 `matchType != 100`（100 = 教学那一档，教学里不许玩家手动控镜头）；
            //   我们**没有教学模式**（游戏模式只有战役/遭遇/排位那几档）⇒ 恒允许手动。
            if (boardCamera != null) CameraZoom.Initialize(true);
        }

        /// <summary>= 原版 `CombatAutoZoom.ForceRefresh()` —— **与 `ResetCameraZoomUIAction` 同一个 RVA**
        /// （原版这两条逐字节同体）：按「两侧里多的那个」重算一次，`force: true`。</summary>
        public void ForceRefresh() { ResetCameraZoomUIAction(); }

        /// <summary>🆕 **我们加的**（原版没有这个方法 —— 它靠「一局一份场景 = 一局一个新组件」拿到同样的效果）：
        /// 新开一局时把状态放回出厂那一档。
        /// <para>出厂档的三条都有出处：`currentEnemySize/currentPlayerSize` = 零初始化（`dump.cs` 里没有初值、
        /// `CombatAutoZoom__ctor.c` 也只写 `+0x24 = 4`）；`manualCamera` 同理（= `CombatCameraZoom` 那一格）；
        /// `targetZoomLevel` 走**那件的 setter**（它的 `GetMaxZoomLevel` 含 `smallScreenUI` 夹取）。</para>
        /// <para>⚠️ 顺带把取景**写回不缩放那一档** —— 原版 `CombatCameraZoom.Initialize` 末尾也是当场写一次
        /// （它反解出来的 `t` 直接进 `currentZoomLevel` 与 `targetZoomLevel`，画面不动）。</para>
        /// <para>🆕 **A422 起它还负责把 `CombatCameraZoom` 那件建出来并接上相机**（原版是场景里就摆好的
        /// —— 我们没有战场场景里的那件组件，只能 `AddComponent`）。</para></summary>
        public void ResetForBattle()
        {
            currentEnemySize = 0;
            currentPlayerSize = 0;

            // 🆕 A422：把状态放回出厂那一档，并**打开那件组件**（原版 `CombatCameraZoom.Awake` 是 `enabled = false`，
            //   `Initialize()` 才打开；它的 `Awake` 里那两句 `+=` 只在**第一次** `AddComponent` 时跑）。
            var cz = CameraZoom;
            cz.targetCamera = boardCamera;
            cz.ManualCamera = false;                    // 原版 `+0x30 manualCamera` 零初始化
            cz.ToggleAllowManualControl(true);          // 原版 `Initialize(matchType != 100)`（100 = 教学档；我们没有教学）

            // 「开局不跳画面」那一格：原版是 `CombatCameraZoom.Initialize` 拿相机**当前**的传感器尺寸**反解** zoom
            //   （判据 → `CombatCameraZoom.Initialize`）。反解的另一半自变量是 `virtualCameraLensShift`
            //   （= 原版 `vcam.m_Lens.LensShift`，场景序列化 `(0.0, −0.205)`）⇒ 先把它放回**我们烘进场景的那一档**
            //   = `(0, LensShiftY(aspect, 1))`（`Editor/BattleScene.cs` 的 `BoardFramer` 写进去的就是它）。
            float aspect = boardCamera != null ? boardCamera.aspect : (16f / 9f);
            cz.virtualCameraLensShift = new Vector2(0f, LensShiftY(aspect, OffZoomLevel));

            // 走 **setter**（不是直接写字段）—— 原版 `CombatCameraZoom.Initialize` 末尾那句就是 `set_TargetZoomLevel(t)`，
            //   ⇒ **`smallScreenUI` 那道夹取在开局这一档也照样生效**（开着的话上界 ~0.305，取景当场收到那儿）。
            //   ⚠️ 顺带：setter 里就带 `ApplyFraming()`，所以不必再单独调一次。
            TargetZoomLevel = OffZoomLevel;
        }

        /// <summary>= 原版 `CombatAutoZoom.ResetCameraZoomUIAction()`（私有；`BattleHud` 那颗钮的回调）。</summary>
        void ResetCameraZoomUIAction()
        {
            SetZoomLevel(Mathf.Max(currentEnemySize, currentPlayerSize), true);
        }

        /// <summary>= 原版 `CombatAutoZoom.OnEnable()`：`MinionManager.OnMinionAddedOrRemoved += OnMinionNumberChanged;`
        /// （`CombatAutoZoom__OnEnable.c`：取静态字段 → `Action&lt;int,bool&gt;.ctor(this, OnMinionNumberChanged)` → `Combine` → 写回）。</summary>
        void OnEnable() { AttachMinionEvent(); }

        /// <summary>= 原版 `OnDisable()`：同一句 `Delegate.Remove`（逐句与 `OnEnable` 对称）。</summary>
        void OnDisable() { DetachMinionEvent(); }

        /// <summary>把 <see cref="OnMinionNumberChanged"/> 挂到 <see cref="OnMinionAddedOrRemoved"/> 上（**幂等**）。
        /// <para>⚠️ **为什么把 `OnEnable` 里那句拆出来、还加了「挂过没有」这一格**：批处理下 `AddComponent`
        /// 到底跑不跑 `OnEnable` **本工程没有定论**（`Editor/BattleScene.cs` 里那条 A321 订正记着「不跑」
        /// 与「会跑」两档，分界是「父链激活」还是「类型带 `[ExecuteAlways]`」**未坐实**，所以那三处
        /// 一律**显式补一次** `Build()`）。「人数事件接上没有」是 A175 的命门 ⇒ **不能赌生命周期**：
        /// `BattleDriver.SetupAutoZoom` 会显式调这里一次，`OnEnable` 跑到也只是重复调一次（幂等）。
        /// ⛔ 别把这一格删了改成裸 `+=` —— 那样两条路都跑时**会挂两份**，每来一次人数变化就重算两遍。</para></summary>
        public void AttachMinionEvent()
        {
            if (_minionHooked) return;
            _minionHooked = true;
            OnMinionAddedOrRemoved += OnMinionNumberChanged;
        }

        /// <summary>与 <see cref="AttachMinionEvent"/> 对称（幂等；`OnDisable` 走这里）。</summary>
        public void DetachMinionEvent()
        {
            if (!_minionHooked) return;
            _minionHooked = false;
            OnMinionAddedOrRemoved -= OnMinionNumberChanged;
        }

        /// <summary>= 原版 `CombatAutoZoom.OnMinionNumberChanged(int numberOfMinionsInSide, bool isPLayer)`（逐句）：
        /// `isPLayer == false` ⇒ 写 `currentEnemySize`；否则写 `currentPlayerSize`；
        /// 然后 `SetZoomLevel(Mathf.Max(currentEnemySize, currentPlayerSize), force: false)`。
        /// <para>⚠️ 注意那个 `Mathf.Max` 是**两次**取的：事件载荷本身已经是「较忙那半」（见
        /// <see cref="OnMinionAddedOrRemoved"/>），这里再对**两侧**取一次 max。</para></summary>
        void OnMinionNumberChanged(int numberOfMinionsInSide, bool isPLayer)
        {
            ZoomEventCount++;
            if (!isPLayer) currentEnemySize = numberOfMinionsInSide;
            else currentPlayerSize = numberOfMinionsInSide;
            SetZoomLevel(Mathf.Max(currentEnemySize, currentPlayerSize), false);
        }

        /// <summary>= 原版 `CombatAutoZoom.SetZoomLevel(int numberOfMinionsInSide, bool force = false)`（逐句；
        /// 那两个被反编译器吃掉的操作数是**读指令流**拿回来的，见文件头 ③）：
        /// <code>
        /// float zoom = GameStaticData.useCombatAutoZoom ? unitsZoomCurve.Evaluate(n) : 1.0f;   // 常量 DAT_1834b2bb8
        /// zoom = Mathf.Clamp(zoom, 0f, 1.0f);                                                 // 同一个常量当上界
        /// if (force) { BattleHud.Instance.ToggleResetAutoCameraZoom(false); combatCameraZoom.manualCamera = false; }
        /// if (!combatCameraZoom.manualCamera) combatCameraZoom.TargetZoomLevel = zoom;
        /// </code>
        /// <para>🔴 **`GameStaticData.useCombatAutoZoom`(+0x125) 就是我们那颗开关** —— 判据：`GraphicsTab__AutoZoomClick.c`
        /// 写 `+0x125` 与 `+0x12f`（= `autoCombatChosenManually`），`BattleSettingsWindow__OnAutoZoomChanged` 也写 `+0x125`；
        /// `dump.cs` 的 `GameStaticData` 把 `+0x125` 落在 `useCombatAutoZoom`。</para></summary>
        void SetZoomLevel(int numberOfMinionsInSide, bool force = false)
        {
            // ⚠️ 原版这一句读的是**静态字段**（当场读，不是开窗时读一次）⇒ 开关在战斗中途翻也照样即时生效。
            float zoom = AutoZoom.Enabled ? Curve.Evaluate(numberOfMinionsInSide) : OffZoomLevel;
            zoom = Mathf.Clamp(zoom, 0f, OffZoomLevel);       // 上界就是那个常量 1.0（`DAT_1834b2bb8`）

            if (force)
            {
                RaiseToggleResetCameraZoomUi(false);           // 原版：`BattleHud.Instance.ToggleResetAutoCameraZoom(false)`
                manualCamera = false;                          // 原版：`*(combatCameraZoom + 0x30) = 0`
            }
            if (!manualCamera) TargetZoomLevel = zoom;         // 原版：`if (*(lVar1 + 0x30) == '\0') set_TargetZoomLevel(...)`
            // 🆕 A422：无帧循环那一档的落点（原版靠 `CombatCameraZoom.LateUpdate` 每帧追，见文件头 D）。
            //   ⚠️ `manualCamera` 为真时 `SettleFraming()` 直接返回 false（原版那道闸）⇒ 这里什么都不写。
            ApplyFraming();
        }

        /// <summary>= 原版 `CombatCameraZoom.set_TargetZoomLevel(float value)`（`…__set_TargetZoomLevel.c` 逐句）：
        /// <code>
        /// float max = 1.0f;                                            // DAT_1834b2bb8
        /// if (GameStaticData.smallScreenUI /* +0x11c */)
        ///     max = maxZoomForSmallScreenDevicesByAspectRatio.Evaluate(camera.aspect);
        /// targetZoomLevel = Mathf.Min(value, max);
        /// </code>
        /// <para>🔴 **那一层 `smallScreenUI` 是真会生效的另一档**（铁律 5·c）：开着「Small Screen UI」时
        /// 上界掉到 **~0.305（16:9）/ ~0.535（16:10）/ ~0.574（4:3）**，而 `unitsZoomCurve` 在人多时给的是 1.0
        /// ⇒ 两条一起才定得下最终 zoom。判据 = `MonoBehaviour_4404.json`（13 场逐字节相同，文件头 ⑤）。</para>
        /// <para>🔴 **2026-10-12（A422）起它不再是本类的一个私有属性**：状态住在 `CombatCameraZoom`（原版
        /// 那一格就是**它的**字段 `+0x84`）—— 本类这一格只转发，夹取在
        /// `CombatCameraZoom.GetMaxZoomLevel` / `TargetZoomLevel` 里（判据只此一处）。
        /// 原版那两格的可见性是 private，这里照抄 private —— 自检读的是 <see cref="ZoomLevel"/>。</para></summary>
        float TargetZoomLevel
        {
            get { return _cameraZoom != null ? _cameraZoom.TargetZoomLevel : OffZoomLevel; }
            set
            {
                CameraZoom.TargetZoomLevel = value;            // setter 内含 `Mathf.Min(value, GetMaxZoomLevel(value))`
                ApplyFraming();                                // 🆕 无帧循环那一档的落点（见文件头 D）
            }
        }

        /// <summary>「不缩放」那一档 = 原版常量 `DAT_1834b2bb8` = **1.0**（`BoardFramer.Zoom` 也是它，
        /// `.rdata` 直读；同一个常量在原版里同时当 `SetZoomLevel` 的默认值与上夹取）。</summary>
        public const float OffZoomLevel = 1f;

        // ==================================================================
        //  取景：原版 `CameraVerticalFramer.CalculateFraming`（**两半都在这里**）
        //          （判据只此一处 —— `Editor/BattleScene.cs` 的 `BoardFramer` 转调 `LensShiftY`；
        //            `CombatCameraZoom.ApplyZoom` 转调 `CalculateFraming`）
        // ==================================================================

        /// <summary>= 原版 `CameraVerticalFramer.CalculateFraming(float zoomLevel, out Vector2 newSensorSize,
        /// out Vector2 desiredLensShift)`（**我们这一档**：原版那件组件我们没做，它那条链的判据就收在本类里 ——
        /// A422 起由 `CombatCameraZoom.ApplyZoom` 转调它，和原版那一跳一一对应）。
        /// <para>两半都在：<br/>
        /// ① `desiredLensShift.y` = <see cref="LensShiftY"/>（`viewShiftModifier(−) × verticalPadding…` 那条链，
        /// 2026-09-20 就接着的那一半）；<br/>
        /// ② `newSensorSize.x` = <see cref="FrameSensorSizeX"/>（**A421**，`cameraSizeXTable` 那一半）
        /// —— 自变量是**现量**的 <see cref="MeasureCardAreaGapWorld"/> 过 <see cref="BoundsVerticalSizeInViewport"/>，
        /// 与 ① 用的是**同一套 helper 几何**（`PlayerHelperTopPx` / `EnemyHelperBottomPx`）。</para>
        /// <para>逐项出处（**全是实读**，`CameraVerticalFramer__CalculateFraming.c`）：
        /// <list type="bullet">
        /// <item>`newSensorSize` 先原样拿相机**当前**的 `sensorSize`（`:204-206` `get_sensorSize → *param_3`），
        ///   **再只写 `.x`**（`:213-214` `*param_3 = (framer+0x98 − curve) × zoom + curve`）⇒ `.y` 一个字节都不碰；</item>
        /// <item>`desiredLensShift` 先原样拿相机**当前**的 `lensShift`（`:216-218`），
        ///   **再只写 `.y`**（`:226` `*(float*)((longlong)param_4 + 4) = viewShiftModifier.Evaluate(…)`）；</item>
        /// <item>`zoom` 的夹取 = `:207-212` 那两条 `comiss`（0 与 `DAT_1834b2bb8` = 1.0，与
        ///   <see cref="FrameSensorSizeX"/> 里同一个 <see cref="OffZoomLevel"/>）；</item>
        /// <item>自变量 = `(minUIViewportPosition.y − maxUIViewportPosition.y) × 0.5 + maxUIViewportPosition.y`
        ///   （`:222-225`；`+0x90` 是 min 的 y、`+0x84` 是 max 的 y —— 我们的两个 px 常量就是它）。</item>
        /// </list></para>
        /// <para>⛔ **原版还会把 `framer.originalSensorSize`/`originalLensShift` 写回相机**（`:141-147`），
        /// 并在结尾把进函数时那个 `sensorSize` 还原（`:231-234`）；**我们只做「量之前换、量完还原」**
        /// （见 <see cref="MeasureCardAreaGapWorld"/>），**不**照抄「把缓存值写进相机」那一半 —— 它的消费者是
        /// `CombatCameraZoom` 对 vcam 的回写，而我们那份（`virtualCameraLensShift`）从不读相机的 `lensShift`
        /// ⇒ 那一半在我们这一档**没有可观测效果**（如实记，不是「做了」）。</para>
        /// <para>返回 `false` = 没有 3D 战场相机（已出声一次）；调用方**别落点**。</para></summary>
        public bool CalculateFraming(float zoom, out Vector2 newSensorSize, out Vector2 desiredLensShift)
        {
            newSensorSize = Vector2.zero;
            desiredLensShift = Vector2.zero;
            if (boardCamera == null)
            {
                if (!_noCameraNoted)
                {
                    _noCameraNoted = true;
                    Debug.LogWarning("[AutoZoom] 🔴 `boardCamera` 是 null ⇒ 取景算不出来也落不下去（这一局没有 3D 战场相机）"
                                   + " —— 原版这台相机是场景里的 `BoardCamera`，我们是 `BattleDriver.boardCam`");
                }
                return false;
            }
            // ② 先现量（`z` / `sensorSize.x` 的换入换出都在 `MeasureCardAreaGapWorld` 里）
            MeasuredWorldDeltaY = MeasureCardAreaGapWorld();
            BoundsVerticalSize  = BoundsVerticalSizeInViewport(MeasuredWorldDeltaY);

            var ssCur = boardCamera.sensorSize;
            newSensorSize = new Vector2(FrameSensorSizeX(OriginalSensorSizeX(), BoundsVerticalSize, zoom), ssCur.y);

            var lsCur = boardCamera.lensShift;
            desiredLensShift = new Vector2(lsCur.x, LensShiftY(boardCamera.aspect, zoom));
            return true;
        }

        /// <summary>无帧循环那一档的落点：让 `CombatCameraZoom` 把当前 `targetZoomLevel` **当成一帧收敛完**
        /// 写进相机（出处 → `CombatCameraZoom.SettleFraming` 的注释与那份文件头 A）。
        /// <para>⚠️ `manualCamera` 为真 ⇒ 原版那道 `if (!manualCamera)` 闸 ⇒ `SettleFraming()` 返回 false，
        /// 这里**什么都不写**（`FramingApplyCount` 也不涨 —— 那个计数钉的是「真的写进相机几次」）。</para></summary>
        void ApplyFraming()
        {
            if (_cameraZoom == null) return;                    // 还没建（`ResetForBattle` 会建）
            if (_cameraZoom.SettleFraming()) FramingApplyCount++;
        }

        // ==================================================================
        //  曲线数据（原版逐值；两条都是 13/13 场逐字节相同 —— 见文件头 ④⑤）
        // ==================================================================

        /// <summary>原版 `unitsZoomCurve`（`MonoBehaviour_5231.json` 等 13 份）。
        /// 键：`(0,0) (1,0) (2,0) (3,0.10525775700807571) (4,1)`，切线逐值照抄，
        /// `weightedMode = None`、`m_PreInfinity = m_PostInfinity = 2`(Clamp)。</summary>
        static AnimationCurve OriginalUnitsZoomCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f,            0f,                0f),
                new Keyframe(1f, 0f,           -0f,                0f),
                new Keyframe(2f, 0f,           -0f,                0.10525775700807571f),
                new Keyframe(3f, 0.10525775700807571f, 0.10525775700807571f, 0.8947422504425049f),
                new Keyframe(4f, 1f,            0.8947422504425049f, 0.5f));
        }

        /// <summary>原版 `CombatCameraZoom.maxZoomForSmallScreenDevicesByAspectRatio`（`MonoBehaviour_4404.json` 等 13 份）。
        /// <para>🔴 **2026-10-12（A444 同一处）订正**：原来 5 个键的 `time`/`inSlope`/`outSlope` 都是 4~7 位小数的
        /// **截断**（`1.333f` / `0.027326f` / `-0.144899f` …，value 还多截了 3 个：`0.573931f` vs 原版 `0.5739306211471558f`）
        /// ⇒ 现在**逐值换成原版字面量**（判据同下面那三条曲线；`weightedMode = 0` ⇒ `inWeight/outWeight` 不参与）。
        /// 影响量级 ~4e-7（这一档只夹 `smallScreenUI` 开时的 zoom 上界），但既然判据是逐值实读，就不留截断。</para></summary>
        static AnimationCurve OriginalMaxZoomByAspect()
        {
            return new AnimationCurve(
                new Keyframe(1.3329999446868896f, 0.5739306211471558f,  0.027326153591275215f, -0.14489907026290894f),
                new Keyframe(1.600000023841858f,  0.5352425575256348f, -0.14489907026290894f, -1.3539530038833618f),
                new Keyframe(1.7699999809265137f, 0.3050706088542938f, -1.3539530038833618f,   0.005991908255964518f),
                new Keyframe(2.3329999446868896f, 0.3084440529346466f,  0.005991908255964518f, -0.00967209693044424f),
                new Keyframe(2.440000057220459f,  0.3074091374874115f, -0.00967209693044424f,  -0.00967209693044424f));
        }

        AnimationCurve _unitsZoomFallback;
        AnimationCurve _maxZoomFallback;

        /// <summary>`unitsZoomCurve`，取不到（运行时 `AddComponent` ⇒ 没有序列化来源）就用原版那一份。</summary>
        AnimationCurve Curve
        {
            get
            {
                if (unitsZoomCurve != null && unitsZoomCurve.length > 0) return unitsZoomCurve;
                if (_unitsZoomFallback == null) _unitsZoomFallback = OriginalUnitsZoomCurve();
                return _unitsZoomFallback;
            }
        }

        /// <summary>这一档（`smallScreenUI` 开）在某个宽高比下的 zoom 上界。</summary>
        public static float MaxZoomForSmallScreen(float aspect)
        {
            // ⚠️ 曲线每次现建（`AnimationCurve` 不是线程安全的共享件、也不进静态缓存）—— 这一跳一次战斗才走几回。
            return OriginalMaxZoomByAspect().Evaluate(aspect);
        }

        /// <summary>把人数映成 zoom（原版 `unitsZoomCurve`）：`≤2 → 0` · `3 → 0.1052578` · `≥4 → 1`。
        /// ⛔ **自检的期望值不许读这里**（那是拿实现当期望值）—— 断言里写死原版那三档。</summary>
        public static float EvaluateUnitsZoom(float numberOfMinions)
        {
            return OriginalUnitsZoomCurve().Evaluate(numberOfMinions);
        }

        // ---- 取景算法本身（原版 `CameraVerticalFramer.CalculateFraming` 的 lensShift.y 那一跳）----
        //
        // 逐项出处（**全是实读**）：
        //  · 公式 = `D:/2/tools/decomp_full/CameraVerticalFramer__CalculateFraming.c`
        //  · 三条曲线与 `maxVerticalSizeInViewPort` = `07_场景/battlearena1/MonoBehaviour/MonoBehaviour_4697.json`
        //  · `k = 0.5` = `DAT_1834b2bb4`（VA→RVA→`.rdata` 直读）
        //  · `zoom` 那一档由调用方给（原版是 `CombatCameraZoom.currentZoomLevel`）
        //  · 两个 helper 的几何 = 运行时 UI dump（`runtime_ui_dump_drive_0912.tsv:340,348`）
        //  · 角点：max 读 corner[0]=左下、min 读 corner[2]=右上（`+0x20` / `+0x38`，每点 12 字节）
        // 🔴 **这一段原来在 `Editor/BattleScene.cs` 的 `BoardFramer` 里**（只看 zoom = 1）。
        //    自动缩放要按 zoom 现算 ⇒ 判据**收成一处**（工程规矩：两处写同一条规则 = 迟早不一致）：
        //    `BoardFramer.LensShiftY(aspect)` 现在就是 `CombatAutoZoom.LensShiftY(aspect, 1f)`。

        const float FramingK = 0.5f;                   // DAT_1834b2bb4 —— 取 [minY,maxY] 的**中点**
        const float FramingRefH = 1080f;
        const float PlayerHelperTopPx = 249.9f;        // 我方 helper 上沿（pivot 在下 ⇒ 往上长）
        const float EnemyHelperBottomPx = 1080f - 96.7f;   // 敌方 helper 下沿（pivot 在上 ⇒ 往下长）

        static AnimationCurve MakeCurve(float[] t, float[] v, float[] ins, float[] outs)
        {
            var keys = new Keyframe[t.Length];
            for (int i = 0; i < t.Length; i++)
                keys[i] = new Keyframe(t[i], v[i], ins[i], outs[i]);
            return new AnimationCurve(keys);
        }

        // ---- 🆕 2026-10-12（A444）：下面三条取景曲线**逐键逐切线**照抄原版（原来是「抄错 + 截断」）----
        //
        // 判据 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4697.json`
        //   （= 原版 `CameraVerticalFramer` 那一份；**13 个战场逐字节相同**，2026-10-12 逐份复读，13/13）。
        // 🔴 **这不是「新旧解包不一致」，是当年照抄时抄错了**：旧解包
        //   （`d:/2/解包整理/07_场景/battlearena1/MonoBehaviour/` 那一份）**这四条曲线逐值也相同**。
        // **错 → 应有值**（每一条都已改成原版字面量）：
        //   · `viewShiftModifier` 四段切线 `−0.972000 / −0.593500 / −0.997100 / −1.129900`
        //     → **`−0.6743468046188354` / `−0.5585059523582458` / `−0.9103565812110901` / `−1.6017104387283325`**
        //     （原版 `outSlope[2]=inSlope[3]` · `outSlope[3]=inSlope[4]` · `outSlope[4]=inSlope[5]` · `outSlope[5]=inSlope[6]`
        //      ⇒ **一段一个切线**）；末键 `time` `0.7067` → **`0.7067446112632751`**
        //   · `verticalPaddingModifierByAspectRatio` 第 3 键的 `outSlope` 与第 4 键的 `inSlope`：
        //     `+0.051700` → **`−0.6183817982673645`**（🔴 **符号也反了**）—— 这一档**只在宽高比 > 1.77 时才被采**
        //     ⇒ 16:9 上只差一点点、肉眼看不出来，正是它躲过了前面几轮
        //   · 三条里若干 `value` / 切线原来是**截断**（`−0.09650f` / `0.6519f` / `−0.1394944f` 这种 5~7 位小数）
        //     ⇒ 全部换成**原版字面量**；`inWeight/outWeight` **不抄**（`weightedMode = 0` ⇒ 权重不参与求值）
        // ⚠️ **连带要重烘场景**：`BoardLensShiftY()` 会变 ⇒ `Battle.unity` 里烘着的 `lensShift.y` 与它对不上
        //   （`Editor/BattleScene.cs` 里那条比较会红）⇒ 必须跑一次 `BattleScene.BuildAndSaveScene`。
        //   `Editor/BattleScene.cs` 的 A175 那两个期望值（`lensShift.y` 的 zoom = 1 / zoom = 0 两档）**已按这三条重算**。
        // ⚠️ **wrap 模式没动、也别动**：这三条走的就是 `new AnimationCurve(keys)` 的**默认** wrap 模式 ——
        //   原版序列化的是 `m_PreInfinity/m_PostInfinity = 2`，**与我们代码建的曲线序列化出来的那个值是同一个**
        //   （2026-10-12 实测：我们建的曲线在资产里写成 2；而显式写 `(WrapMode)2` 的会写成 **1**，
        //   见 `WarpforgeArena1/Editor/SunFlareData.gen.cs` → `flares/Sun Flare 1.asset`）⇒ **默认 = 原版那一档**。
        //   ⚠️ `zoom = 0` 那一档要靠 `PadByZoom` 在**第一键（0.0030534）左边**取端点值（不是回绕）—— A175 的两个
        //   期望值就建立在这上面（那条断言会把它断出来；红了先查这一格）。

        // viewShiftModifier —— **输出就是 `lensShift.y`**（7 键，实读）
        static readonly AnimationCurve ViewShift = MakeCurve(
            new[] { 0.3449999988079071f, 0.38874998688697815f, 0.4169999957084656f, 0.5f,
                    0.5709999799728394f, 0.6514000296592712f, 0.7067446112632751f },
            new[] { -0.09650024026632309f, -0.15226143598556519f, -0.16414929926395416f, -0.22012008726596832f,
                    -0.25977399945259094f, -0.33296671509742737f, -0.4216127097606659f },
            new[] { 0f, -1.274541974067688f, -0.42080917954444885f, -0.6743468046188354f,
                    -0.5585059523582458f, -0.9103565812110901f, -1.6017104387283325f },
            new[] { -1.274541974067688f, -0.42080917954444885f, -0.6743468046188354f, -0.5585059523582458f,
                    -0.9103565812110901f, -1.6017104387283325f, 0f });

        // verticalPaddingByZoom（2 键）：`0.0030534351244568825 → −0.032013118267059326` · `1.0 → −0.17108154296875`
        //   两个键的四条切线**同一个值** `−0.13949435949325562`（原版就这样）
        static readonly AnimationCurve PadByZoom = MakeCurve(
            new[] { 0.0030534351244568825f, 1.0f },
            new[] { -0.032013118267059326f, -0.17108154296875f },
            new[] { -0.13949435949325562f, -0.13949435949325562f },
            new[] { -0.13949435949325562f, -0.13949435949325562f });

        // verticalPaddingModifierByAspectRatio（5 键）：≤1.77 恒 1.0；`2.333 → 0.6518510580062866`；`2.44 → 0.6508162021636963`
        //   🔴 第 3 键 `outSlope` 与第 4 键 `inSlope` 都是 **`−0.6183817982673645`**（原来写成 `+0.051700` —— 连符号都错）；
        //   第 1、2 键的 `inSlope` 原版是 **`-0.0`**（负零，照抄 `-0f`）
        static readonly AnimationCurve PadModByAspect = MakeCurve(
            new[] { 1.3329999446868896f, 1.600000023841858f, 1.7699999809265137f,
                    2.3329999446868896f, 2.440000057220459f },
            new[] { 1.0f, 1.0f, 1.0f, 0.6518510580062866f, 0.6508162021636963f },
            new[] { 0.027326153591275215f, -0f, -0f, -0.6183817982673645f, -0.00967153999954462f },
            new[] { 0f, 0f, -0.6183817982673645f, -0.00967153999954462f, -0.00967209693044424f });

        /// <summary>这个宽高比 + 这个 zoom 下原版会用的 `lensShift.y`。
        /// `zoom = 1` ⇒ 我们烘进场景的那个值（`BoardFramer` 转调的就是它）。</summary>
        public static float LensShiftY(float aspect, float zoom)
        {
            float minY = PlayerHelperTopPx / FramingRefH;
            float maxY = EnemyHelperBottomPx / FramingRefH + PadModByAspect.Evaluate(aspect) * PadByZoom.Evaluate(zoom);
            return ViewShift.Evaluate((maxY + minY) * FramingK);
        }

        // ==================================================================
        //  🆕 2026-10-12（A421）：`CameraVerticalFramer.CalculateFraming` 的**另一半** —— `sensorSize.x`
        //
        //  原版那一段（`CameraVerticalFramer$$CalculateFraming`，RVA 0x6085D0 / VA 0x1806085D0，
        //  **读指令流**逐条核过；Ghidra 那个 `.c` 在这里把两个操作数认错了，别照它抄）：
        //    · `0x180608B46` `subss xmm8,xmm10` + `0x180608B4F` `andps xmm8,[0x1834b2e60]`
        //       ⇒ `|y(player 手牌区) − y(敌方区)|`（那个常量就是 **float 的绝对值掩码 `0x7FFFFFFF`**）
        //    · `0x180608B31` `movss xmm11,[rbx+0x78]` / `0x180608B69` `call Math.Min`
        //       ⇒ `Mathf.Min(|Δ|, framer+0x78)`（`+0x78` = `maxVerticalSizeInViewPort`）
        //    · `0x180608B86` `movss [rbx+0x7c],xmm1` ⇒ 存进 `boundsVerticalSizeInViewport`
        //    · `0x180608BB0` `mov rcx,[rbx+0x38]` → `AnimationCurve.Evaluate(bounds)`（`+0x38` = `cameraSizeXTable`）
        //    · `0x180608BD7` `get_sensorSize(boardCamera)`（`+0x20`）⇒ `*out = 当前 sensorSize`
        //    · `0x180608BE3` `movss xmm2,[rbx+0x98]` = `originalSensorSize.x`（`+0x98`，缓存值）
        //    · `0x180608BDC/BFA` 把 `zoom` 夹到 [0, 1]（`xmm9` = 0 · `[0x1834b2bb8]` = 1.0）
        //    · `0x180608C02-C0A` `(origX − curve) × zoom + curve` → `*out.x`
        //  两个 helper 的世界 Y：
        //    · 敌方区**下沿**  = `enemyCardAreaSizeHelper.GetWorldCorners()[0]`（左下）→ `UICamera.WorldToViewportPoint`
        //      → 存 `minUIViewportPosition`（`framer+0x8c`）
        //    · 我方手牌区**上沿** = `playerCardAreaSizeHelper.GetWorldCorners()[2]`（右上）→ 同上 → 存
        //      `maxUIViewportPosition`（`framer+0x80`）
        //      ⚠️ 名字与屏幕高低**是反的**（敌方的下沿在屏幕上更高）—— 原版就长这样，别「顺手改对称」。
        //    · 那两条 viewport 点各自过 **`boardCamera.ViewportToWorldPoint(vp, z)`**，
        //      `z` = `boardCamera.transform.InverseTransformPoint(new Vector3(DAT_1834b2e08 = **100**, 0, 0)).z`
        //      （`0x180608A0E` 读的那格字面量 = 100.0；100 = **原版场地根的 x**。我们整体平移了 −100
        //       （`Editor/BattleScene.cs` 的 `ArenaOriginX`）⇒ 等价点 = `Vector3.zero`；相机朝向不变 ⇒ z 恒等）
        //    · `y` 的差**与 z 无关地**只由 `Δvp.y` 决定（`lensShift` 在差分里抵消）。
        //  谁还会碰这一格：`CombatCameraZoom.set_TargetZoomLevel` 的 `smallScreenUI` 夹取**只夹 zoom**，
        //  不碰 `sensorSize`；`ApplyZoom` 每帧把这里的 `out` 原样 `set_sensorSize` 回去。
        // ==================================================================

        /// <summary>原版 `CameraVerticalFramer.maxVerticalSizeInViewPort`（`framer+0x78`）。
        /// 13 个战场**逐字节相同**（= **7.0**；2026-10-12 逐份亲读 `MonoBehaviour_4697.json` 等 13 份，
        /// 与 `cameraSizeXTable` / `viewShiftModifier` / 两条 padding 曲线一起算 md5，13/13 同一个值）。</summary>
        public const float MaxVerticalSizeInViewPort = 7f;

        /// <summary>原版 `CameraVerticalFramer.cameraSizeXTable`（`framer+0x38`）—— **6 个键**，
        /// 逐值（含切线）照抄 `MonoBehaviour_4697.json`，13/13 场逐字节相同，
        /// `m_PreInfinity = m_PostInfinity = 2`（Clamp）。
        /// <para>🔴 **连切线一起抄，别只抄 value**：自变量被 `Mathf.Min(…, 7.0)` 夹过 ⇒
        /// 常见档下它**恰好是 7.0**，落在第 3～4 键之间、**这一段是切线说了算**。</para></summary>
        static AnimationCurve OriginalCameraSizeXCurve()
        {
            var c = new AnimationCurve(
                new Keyframe( 4.930829048156738f, 41.084930419921875f, -0.08417845517396927f, -8.180681228637695f),
                new Keyframe( 5.591578006744385f, 35.6795539855957f,   -8.180681228637695f, -8.84035587310791f),
                new Keyframe( 5.856215000152588f, 33.34006881713867f,  -8.84035587310791f,  -4.210846900939941f),
                new Keyframe( 7.6655964851379395f,25.721040725708008f, -4.210846900939941f,  -2.655146598815918f),
                new Keyframe( 8.940309524536133f, 22.336490631103516f, -2.655146598815918f,  -1.93594229221344f),
                new Keyframe(11.45475959777832f,  17.468660354614258f, -1.93594229221344f,   -2.3610286712646484f));
            c.preWrapMode = WrapMode.Clamp;      // = 原版序列化的 m_PreInfinity = 2
            c.postWrapMode = WrapMode.Clamp;     // = 原版序列化的 m_PostInfinity = 2
            return c;
        }

        /// <summary>原版 `cameraSizeXTable.Evaluate(boundsVerticalSizeInViewport)`（自检口）。
        /// ⛔ **自检的期望值不许读这里** —— 那 6 个键是原版字面量，断言里自己算一遍 Hermite。</summary>
        public static float EvaluateCameraSizeX(float boundsVerticalSizeInViewport)
        {
            return OriginalCameraSizeXCurve().Evaluate(boundsVerticalSizeInViewport);
        }

        /// <summary>= 原版那一段 `Mathf.Min(Mathf.Abs(Δ世界 Y), maxVerticalSizeInViewPort)`（指令流见本节头）。
        /// 参数 = 「敌方区下沿」与「我方手牌区上沿」在**战场相机世界系**里的 Y 之差（**带符号**，函数自己取绝对值）。</summary>
        public static float BoundsVerticalSizeInViewport(float worldDeltaY)
        {
            return Mathf.Min(Mathf.Abs(worldDeltaY), MaxVerticalSizeInViewPort);
        }

        /// <summary>= 原版 `CalculateFraming` 出来的 `newSensorSize.x`：
        /// <code>
        /// float curve = cameraSizeXTable.Evaluate(bounds);
        /// zoom = Mathf.Clamp01(zoom);                              // 原版那两条 comiss（0 与 1.0）
        /// sensorSizeX = (originalSensorSize.x − curve) * zoom + curve;
        /// </code>
        /// <para>`zoom = 1` ⇒ **恒等**（= 我们烘进场景那一档）；`zoom = 0` ⇒ 曲线值。</para></summary>
        public static float FrameSensorSizeX(float originalSensorX, float boundsVerticalSizeInViewport, float zoom)
        {
            float curve = EvaluateCameraSizeX(boundsVerticalSizeInViewport);
            return (originalSensorX - curve) * Mathf.Clamp01(zoom) + curve;
        }

        // ---- 运行时那两格（现量的世界高差 / 缓存的原版 sensorSize.x） ----

        /// <summary>`originalSensorSize` 那一格是从**哪台相机**抓的（原版一局一份场景 ⇒ 没有这一格；
        /// 我们 `BattleDriver.Begin` 会被调很多次、相机可能换 ⇒ 加它只为「换了相机就重抓」）。</summary>
        Camera _originalSensorFrom;
        /// <summary>= 原版 `CameraVerticalFramer.originalSensorSize`（`framer+0x98`）的 `.x`（见 <see cref="OriginalSensorSizeX"/>）。</summary>
        float _originalSensorX;
        /// <summary>= 原版 `CameraVerticalFramer.originalSensorSize`（`framer+0x98`）的 `.x`。
        /// 原版**只在「缓存 ≈ 零」时**从相机重抓（`|cached|² &lt; DAT_1834b2ba8 = 1e-10`，指令流
        /// `0x1806080…`/`0x1806088BE` 那一段）—— 那条规则照抄；额外那一条「换相机就重抓」是**我们加的**（见上）。</summary>
        float OriginalSensorSizeX()
        {
            if (boardCamera == null) return 0f;
            float x = _originalSensorX;
            bool stale = _originalSensorFrom != boardCamera;
            if (stale || x * x < 1e-10f)          // 1e-10 = DAT_1834b2ba8（原版那一格是 Vector2 的模方）
            {
                _originalSensorX   = boardCamera.sensorSize.x;
                _originalSensorFrom = boardCamera;
            }
            return _originalSensorX;
        }

        /// <summary>= 原版 `PlayerHand.get_CurrentSizeMultiplier()`（`PlayerHand__get_CurrentSizeMultiplier.c` 逐句）：
        /// `GameStaticData.smallScreenUI`(+0x11c) 假 ⇒ 常量 `DAT_1834b2bb8` = **1.0**；
        /// 真 ⇒ `PlayerHand+0x3c`（那一档把手牌区放大 M 倍）。
        /// <para>🔴 **我们这一档恒 1.0，如实记（不是「照抄了那一档」）**：原版那一位的作用是把
        /// `playerCardAreaSizeHelper.sizeDelta.y` 设成 `playerCardAreaSizeHelperOriginal.rect.height × M`
        /// ⇒ 手牌区**上沿抬高 M 倍**、`bounds` 跟着变小。而**我们的战斗 HUD 没有那个放大档**
        /// （`Shell/TransformScalerBySmallScreenUI.cs` 的 `AddComponent` 生产调用点只在菜单窗口那两处）
        /// ⇒ 两个分支都取 1.0。</para>
        /// <para>⛔ **别把 `SmallScreenUI.Enabled` 直接当 M 乘进来** —— 那会凭空放大手牌区、`bounds` 与
        /// `sensorSize.x` 跟着错，而 M 的真值（`PlayerHand` 那个字段谁写、写多少）**还没查到**。
        /// 它进「没查清」那一栏，等查到再补。</para></summary>
        public static float CurrentSizeMultiplier { get { return 1f; } }

        /// <summary>最近一次量到的「世界系高差」（自检用）。</summary>
        public float MeasuredWorldDeltaY { get; private set; }
        /// <summary>最近一次用的 `boundsVerticalSizeInViewport`（= `Min(|高差|, 7)`，自检用）。</summary>
        public float BoundsVerticalSize { get; private set; }

        /// <summary>量「敌方区下沿 ↔ 我方手牌区上沿」在**战场相机世界系**里的高差（原版那一段）。
        /// <para>链条逐项与本节头那张表一一对应：两个 helper 的世界 Y（我们**没有**那两个 `RectTransform`
        /// —— 全线是世界空间 mesh ⇒ 用**原版运行时 dump 的那两条 helper 几何**，与 <see cref="LensShiftY"/>
        /// 用的是**同一份**常量）→ `UICamera.WorldToViewportPoint`（我们的 UI 相机 = `LayoutSpace.Cam`，
        /// 正交、中心在原点、可见高 10 ⇒ 与原版 `UICamera` 同一语义）→ `boardCamera.ViewportToWorldPoint(vp, z)`
        /// （`z` 的取法见本节头）→ 两个 `.y` 相减。</para>
        /// <para>⚠️ 拿不到 UI 相机时**返回 0 并出声一次**（不许静默）—— `0` 会让 `bounds` 落到曲线左端
        /// （sensorSize.x = 41.08），那是**错的**，所以只在「这台机上根本没有 HUD 相机」时才会发生。</para></summary>
        public float MeasureCardAreaGapWorld()
        {
            var ui = LayoutSpace.Cam;                 // 原版那台 `UICamera`
            if (boardCamera == null || ui == null)
            {
                if (!_noUiCameraNoted)
                {
                    _noUiCameraNoted = true;
                    Debug.LogWarning("[AutoZoom] 🔴 拿不到 UI 相机（`LayoutSpace.Cam`）⇒ 量不出"
                                   + " `boundsVerticalSizeInViewport`，`sensorSize.x` 这一半**没生效**"
                                   + "（原版那台是场景里的 `UICamera`）");
                }
                return 0f;
            }

            // 🔴 **原版先把相机换成「originalSensorSize」再量那两条 viewport 点**：
            //    指令流 `0x1806089DB/0x180608A05` 是 `set_sensorSize(+0x98)` / `set_lensShift(+0xa0)`，
            //    量完还原（`0x180608C71/C7A` 把 vCam 那两格写回、结尾 `set_sensorSize(uVar3)` 把**进函数时**
            //    那个 sensorSize 写回相机）。
            //    ⚠️⚠️ **这一还原是必需的，不是洁癖**：`ViewportToWorldPoint` 的世界尺度 = `z · sensorSize.x / (aspect · focal)`
            //    ⇒ **不还原就自激**：`sensorSize.x` 改尺度 → `bounds` 变 → `sensorSize.x` 又变……
            //    实测那一圈**不收敛**（zoom = 0 那档会来回跳几度）。
            //    ⚠️ `lensShift` 那半**不必**还原：它在两条 `ViewportToWorldPoint` 的**差**里正好抵消
            //    （`worldY = camPos.y + z·T·(2·vp.y − 1 + 2·lensShift.y)` ⇒ 相减时没了）。
            float origSx = OriginalSensorSizeX();
            var ssSaved = boardCamera.sensorSize;
            boardCamera.sensorSize = new Vector2(origSx, ssSaved.y);
            try
            {
                // z = 相机到「场地根所在那个平面」的**相机局部 z**（原版 `InverseTransformPoint(new Vector3(100,0,0))`；
                //     100 = 原版场地根的 x —— 我们整体平移了 −100 ⇒ 等价点 = `Vector3.zero`，两者 z 恒等）。
                float z = boardCamera.transform.InverseTransformPoint(Vector3.zero).z;

                // 两条 helper 边（原版 `enemyCardAreaSizeHelper.corner[0]` 下沿 / 手牌 helper `.corner[2]` 上沿），
                // 换到我们的 HUD 世界系（`LayoutSpace.Px`：1080 设计 px ↔ 10 世界单位、屏幕中心 = y 0）。
                float half = LayoutSpace.DesignPxH * 0.5f;
                float enemyBottomY = LayoutSpace.Px(EnemyHelperBottomPx - half);
                float playerTopY   = LayoutSpace.Px(PlayerHelperTopPx * CurrentSizeMultiplier - half);

                var vpEnemy  = ui.WorldToViewportPoint(new Vector3(0f, enemyBottomY, 0f));
                var vpPlayer = ui.WorldToViewportPoint(new Vector3(0f, playerTopY,   0f));
                float yEnemy  = boardCamera.ViewportToWorldPoint(new Vector3(vpEnemy.x,  vpEnemy.y,  z)).y;
                float yPlayer = boardCamera.ViewportToWorldPoint(new Vector3(vpPlayer.x, vpPlayer.y, z)).y;
                return yPlayer - yEnemy;
            }
            finally
            {
                boardCamera.sensorSize = ssSaved;         // 原版结尾那句 `set_sensorSize(uVar3)` 的等价物
            }
        }

        bool _noUiCameraNoted;
    }
}
