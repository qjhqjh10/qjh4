// WindowsManager.cs — 窗口系统：**所有菜单页都从这一条路开**（阶段二「游戏外壳」）
//
// ============================ 出处（唯一正本） ============================
// `资料/阶段二_Shell_原版规格.md` §三「开窗口调用链」。类名 / 枚举值 / 流程**照原版**：
//   · `WindowsPlacement { None=0, Canvas=5, World=10, Popup=15 }`
//     实证：主菜单的 `1 - Below Upper Bar Holder{10}` · `2 - Canvas Holder Above upper bar{5}` · `3 - PopUp Holder{15}`
//   · `WindowType { Fullscreen=0, Popup=1 }` —— `WindowsManager__OpenWindowCO.c` 里 `window.type==0` 关当前主窗、
//     `==1` 把上一个 `ToBackground()`
//   · `WindowHolder.OnEnable` → `WindowsManager.RegisterAnchor(placement, transform)`（`WindowHolder__OnEnable.c`）；
//     `GetWindowAnchor` 取不到时原版走 `CustomDebug.LogError` ⇒ 我们照做（项目红线：不许静默失败）
//   · `GameWindow.TryOpen`：`SetupData` → **`SoundManager.Play2D(openSound, MixerType.FX)`** → `SetActive(true)`
//     → `CurrentState=Open` → `Open()`（`GameWindow__TryOpen.c`）
//   · `GameWindow.Open()`：**小屏 UI 开关开着**且 `extraScaleSmallScreen != 1` 时
//     `TransformScalerBySmallScreenUI.SetScale(extraScaleSmallScreen)`（`GameWindow__Open.c`）
//     —— 🔴 **2026-10-06 就地订正（铁律 5）**：这里原来写「**窗口宽度 < 阈值**时 …」+「实证值：普通窗 1.0 ·
//     商店/活动类 1.2」—— **两句都不对**。判据：`GameWindow__Open.c` 里**没有任何宽度判定**
//     （第一层判据是静态 bool `GameStaticData.smallScreenUI`，第二层是 `Mathf.Approximately(extra,1)`）；
//     `extra` 实测 1.0 有 93 个、其余 48 个是 1.05/1.07/1.075/1.1/1.12/1.15/1.2/1.35 **逐窗实数**。
//     全量复核 → `资料/普查产出_1006/A154_A155_窗口档位与缩放.md` §②（正本 §三 第 5 条也已就地订正）。
//
// 🔴 **不是照抄的部分（原版查不到，如实标 —— 铁律 3）**：
//   ① **弹窗 prefab 与「类 → prefab」字典是自建的**：全 `assets_full` grep
//      `popupWindowOneButton` / `temporaryWindowDictionary` **零命中**（正本 §五 第 2 条）。
//      ⇒ `ShowPopUp` 我们只保留**签名与行为**（文案 + 1~2 个按钮 + 结果回调）。
//      🔴 **2026-09-23 更正（铁律 5）**：上面这条「原版 popup prefab 本地没有」**已经不成立** ——
//      `GenericPromptWindow` 在 `bundle_menus_assets_all` 里参数齐全（`资料/日常_原版规格.md` §七），
//      界面已改由 `PromptPopup` **照原版**搭。
//      🔴 **2026-10-12 二次更正（A416）**：`GenericPromptWindow` 那一条**只说对了一半** ——
//      原版 `ShowPopUp` 的宿主**不是**它，而是 `MessagePopupWindow{,_2Buttons}`（类 `PopUpGameWindow`，
//      在 `bundle_generalgamewindows_assets_all`，**全库只有这 2 个实例**）⇒ 拿 `PromptPopup` 当
//      `ShowPopUp` 的宿主曾经是**一处【已知偏离】**。🔴 **A416（2026-10-12）已收编**：`ShowPopUp` 转调
//      `ShowMessagePopUp`、宿主换成 `PopUpGameWindow`；`PromptPopup` 仍是**它自己那扇窗**（另有直建它的站点）。
//   ② 🔴 **2026-10-06 就地订正（铁律 5 · A154/A165 全量复核）**：这里原来写「**「宽度 < 阈值」的那个阈值查不到**
//      ⇒ 只在 `extraScaleSmallScreen != 1f` 时才放大；普通窗实测就是 1.0 ⇒ 默认空转（不是没实现，是没东西可放大）」
//      —— **阈值不存在**，原版从来不量宽度；真正缺的那一层是**开关**（`GameStaticData.smallScreenUI`，
//      出厂 0）+ **缩放器组件**。**2026-10-06（A165）两样都补上了**：开关 → `Shell/TransformScalerBySmallScreenUI.cs`
//      的 `SmallScreenUI`，缩放器 → 同文件的 `TransformScalerBySmallScreenUI`，挂在 `GameWindow.TryOpen` 上。
//      ⇒ 「默认空转」这个说法**现在不成立**了：普通窗（`extra = 1.0` 且 prefab 没烤 `menuScale`）在小屏开关开着时
//      也**不放大**（原版就是这么设计的），但三条 `extra = 1.0 / 烤 1.35` 的窗会按 **1.35** 放大。
//   ③ `WindowsManager` 实例的序列化值全丢 ⇒ `anchors` 表靠场景里的 `WindowHolder` 注册（照原版机制），
//      **不要**在代码里写死三个锚点的引用。
using System.Collections.Generic;
using UnityEngine;
using WarpforgeVFX;      // `WFSoundPlayer`（特效/音效那条路唯一的播放器，`Assets/WarpforgeVFX/Runtime/`）

namespace CardPresentation
{
    /// <summary>窗口锚点位置。**值照原版**（主菜单三个 Holder = 10 / 5 / 15）。</summary>
    public enum WindowsPlacement { None = 0, Canvas = 5, World = 10, Popup = 15 }

    /// <summary>窗口类型。**值照原版**（`OpenWindowCO` 按它决定关不关当前主窗 / 要不要把上一个压到背景）。</summary>
    public enum WindowType { Fullscreen = 0, Popup = 1 }

    /// <summary>窗口状态。**值照原版**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowState.cs`：
    /// `Closed = 0 / Open = 1 / Background = 2`）。
    /// <para>🔴 **2026-10-07（A77㉑②）就地订正（铁律 5）**：本枚举原来中间多插了一枚 `Opening`
    /// （`Closed = 0, Opening, Open, Background`）⇒ **`Open` 从 1 变成 2**。
    /// 原版 `GameWindow.IsOpen()` 的判据是**字面量** `*(int*)(this + 0x68) == 1`
    /// （`d:/2/tools/decomp_full/GameWindow__IsOpen.c`；`CurrentState` 就在 0x68 —— 字段偏移见
    /// `d:/2/tools/il2cpp_out/dump.cs` 的 `GameWindow` 一节）⇒ 枚举一旦错位，**照常量补那道门槛就会判错**
    /// （`Opening` 会被当成 `Open`）。</para>
    /// <para>`Opening` 全仓**从未被使用过**（`grep -rn "Opening" --include=*.cs` 只命中 `Booster Opening` 之类
    /// 的动画名）⇒ 删掉它是**零行为变化**，且本文件下面新加的 `IsOpen()` 才与那个 `1` 对得上。</para></summary>
    public enum WindowState { Closed = 0, Open = 1, Background = 2 }


    /// <summary>
    /// 窗口管理器单例。**名字照原版（`WindowsManager`，注意有个 s；全工程没有 `WindowManager` 这个类）**。
    /// 挂在 `Shell` 场景里的常驻对象上。
    /// </summary>
    public class WindowsManager : MonoBehaviour
    {
        public static WindowsManager Instance { get; private set; }

        /// <summary>🔴 **统一读口：有就用、没有就现建**（`EnsureHost` 本身幂等）。
        /// <para>**为什么必须统一**（2026-10-11 实测）：`Instance` 只有两个写点 —— `Awake` 置 this、`OnDestroy` 清 null，
        /// 而**壳的那一台会被 `ShellRuntime.Build()` 的拆除循环拆掉**（延迟到帧末）⇒ **切到主菜单之后 `Instance` 就是 null**。
        /// 全仓**十几处直接读 `Instance`、只有两处走 `EnsureHost()`** ⇒ 同一族两套口径 ⇒
        /// 「**有的路能开窗、有的路报「没有 WindowsManager」**」—— 实测就是模式卡的 `OpenMode` 落在坏的那一支
        /// （而同一跑的日志里 `EnsureHost` 神不知鬼不觉地现建了一台、窗也真的开出来了）。
        /// ⇒ **凡是要「用」窗口层的读点一律走这里**；只做 `== null` 判空（例如自检前提）才读 `Instance`。</para></summary>
        public static WindowsManager Get() => Instance != null ? Instance : EnsureHost();

        /// <summary>锚点表。原版是 `static Dictionary&lt;WindowsPlacement, Transform> anchors`。</summary>
        static readonly Dictionary<WindowsPlacement, Transform> _anchors =
            new Dictionary<WindowsPlacement, Transform>();

        public readonly List<GameWindow> openWindows = new List<GameWindow>();
        public GameWindow currentWindow;
        public GameWindow popUpWindow;

        /// <summary>本类**建过的那一扇消息弹窗**（`ShowMessagePopUp` 唯一出口建的；全仓 `PopUpGameWindow.Create`
        /// 只被它调）。为什么要单独记一格：`popUpWindow` 会在关窗时被 `NotifyClosed` 清掉，而我们的
        /// `Close()` **只 `SetActive(false)`、不销毁** ⇒ 光看字段找不到那具已关掉的对象，
        /// 每弹一次就在锚点下留一具尸体。原版 `OBJECT.Destroy` 那一步靠这一格补上
        /// （判据 → `WindowsManager._LoadPopup_d__55__MoveNext.c:20-45`；2026-10-24 `A11`）。</summary>
        PopUpGameWindow _lastMessagePopup;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // 指针层与窗口管理器**同生共死**：任何开窗路径都会经过这里
            //（真鼠标的点击/滚轮全走它；自检也能直调 `PointerLayer.ClickAt/WheelAt` 验「这一处吃不吃得到输入」）。
            // ⚠️ 放在 `Awake` 而不是 `EnsureHost`：自检是**直接 `AddComponent<WindowsManager>()`** 建的
            //    （不走 `EnsureHost`）—— 2026-09-23 就因为放在 `EnsureHost` 里，自检报「`PointerLayer` 不在场景里」。
            PointerLayer.Ensure(transform.parent);
        }

        void OnDestroy()
        {
            // 🩺 2026-10-11 临时诊断 + 出声（`A1491`·②「有但已死」原来与「有且活着」在日志上长得一样 —— 红线：不许静默失败）
            Debug.Log($"[Win] OnDestroy 场景={gameObject.scene.name} 父={(transform.parent != null ? transform.parent.name : "<null>")}");
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// **确保场景里有 `WindowsManager` 与三个锚点**（幂等）。原版这三个 Holder 在主菜单场景里
        /// （`1 - Below Upper Bar Holder{10}` / `2 - Canvas Holder Above upper bar{5}` / `3 - PopUp Holder{15}`，
        /// **缺一不可**；正本 §三 第 7 条）。
        /// 🔴 **判据只留这一份**：壳（`ShellRuntime`）与「单独打开某个界面场景按 Play」都走它 ——
        /// 两处各建一次 = 迟早不一致（CLAUDE.md §三）。名字与 placement 都照原版。
        /// </summary>
        public static WindowsManager EnsureHost(Transform root = null)
        {
            // 指针层与窗口管理器**同生共死**：任何开窗路径都会经过这里
            //（真鼠标的点击/滚轮全走它；自检也能直调 `PointerLayer.ClickAt/WheelAt` 验「这一处吃不吃得到输入」）
            PointerLayer.Ensure(root);

            if (Instance != null) return Instance;

            var holderRoot = new GameObject("Window Anchors").transform;
            if (root != null) holderRoot.SetParent(root, false);
            MakeHolder(holderRoot, "1 - Below Upper Bar Holder", WindowsPlacement.World);
            MakeHolder(holderRoot, "2 - Canvas Holder Above upper bar", WindowsPlacement.Canvas);
            MakeHolder(holderRoot, "3 - PopUp Holder", WindowsPlacement.Popup);

            var go = new GameObject("WindowsManager");
            if (root != null) go.transform.SetParent(root, false);
            var wm = go.AddComponent<WindowsManager>();
            // 🔴 **2026-09-24 修**：这里必须**显式登记 `Instance`** —— 它原来只在 `Awake()` 里赋，
            //    而**编辑模式（自检）不跑 `Awake`** ⇒ 建完 `Instance` 还是 null：
            //      ① 任何用 `WindowsManager.Instance` 的代码**静默失败**（实测：模式卡的 `OpenMode` 报「没有 WindowsManager」）；
            //      ② 再调一次 `EnsureHost` 会**又建一台 + 又一套锚点**（`Instance` 仍是 null ⇒ 判不出「已经有了」）。
            //    同族先例就在本文件：`WindowHolder` 正是因此才有 `RegisterNow()`（见下面 `MakeHolder` 的注释）。
            Instance = wm;
            Debug.Log("[Win] 场景里没有 `WindowsManager` ⇒ 现建了一台 + 三个锚点（单独打开界面场景时走这条路）");
            // 🆕 **2026-10-12（`A1629`）**：主菜单那一棵建得比这一台还早（编辑期建场时这里根本不会跑）
            //   ⇒ 它把自己的窗挂在 `MainMenuRuntime._pendingBaseMenu` 上等这一跳来认领
            //   （判据 / 为什么要分期 → `MainMenuRuntime.RegisterMainMenuWindow` 的 doc）。
            //   ⚠️ **必须在 `Instance = wm` 之后**：认领那一跳会 `OpenWindow`，而那条路上有读 `Instance` 的地方。
            MainMenuRuntime.OnWindowsManagerReady(wm);
            return wm;
        }

        static void MakeHolder(Transform parent, string name, WindowsPlacement p)
        {
            var go = new GameObject(name);
            // 🔴 **2026-10-12（`A1629`）就地修（铁律 5）**：原来这里是
            //   `SetParent → AddComponent<WindowHolder>() → h.placement = p → h.RegisterNow()`，
            //   而 `WindowHolder` 带 **`[ExecuteAlways]`** ⇒ `AddComponent` 那一刻 `OnEnable` **立刻**跑、
            //   拿**默认值 `None`** 去 `RegisterAnchor` ⇒ **每次 `EnsureHost` 都刷三条红色假缺陷**
            //   （真机日志 `_tmp_view/run_shell.log:78-80` 那三条 `placement = None`；顺序也在那儿：
            //    **三条报错排在 `[Win] 场景里没有 WindowsManager ⇒ 现建了一台` 之前** ⇒ 就是这三次 `AddComponent`）。
            //   ⚠️ **那三条不是「场景文件坏了」**：`MainMenu.unity` 里 `WindowHolder`/`WindowsManager`/
            //   `Window Anchors` **逐个都是 0 个**（`grep -c` 现读）⇒ `项目任务.md` 的 `A1494④` 那句
            //   「`MainMenu.unity` 里带着 3 个 `WindowHolder` 且 `placement = None`」**与事实不符**，已写进报告。
            //   修法 = **先关物体再加组件**（物体不活 ⇒ `OnEnable` 不跑），赋完 `placement` 再激活
            //   ⇒ `OnEnable` 那一刻字段已经是对的，**一条假报错都不出**，注册行为逐位不变。
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<WindowHolder>();
            h.placement = p;
            go.SetActive(true);                        // `OnEnable` 在这一刻跑（`placement` 已就位）
            h.RegisterNow();                           // 照旧兜一次（`RegisterAnchor` 对同一节点是幂等的）
        }

        // ---------------------------------------------------------- 锚点

        public static void RegisterAnchor(WindowsPlacement p, Transform t)
        {
            if (p == WindowsPlacement.None)
            {
                Debug.LogError($"[Win] `WindowHolder` 挂在 `{t.name}` 上，但 placement = None —— " +
                               "原版这个字段是必填的（None 只用于「不挂 Holder」的窗口，如 MainMenuWindow）");
                return;
            }
            if (_anchors.TryGetValue(p, out var cur))
            {
                if (cur == t) return;                       // 同一节点重复注册（建场景 + 运行时 OnEnable 各一次）—— 正常
                // 上一次建场景留下的**死引用**（对象已销毁）⇒ 直接顶掉，这不是冲突
                if (cur != null)
                    Debug.LogError($"[Win] 锚点 {p} 被两个**活着的**节点抢：`{cur.name}` 与 `{t.name}` —— 原版是静态表，后注册的会顶掉前一个");
            }
            _anchors[p] = t;
        }

        public static void UnregisterAnchor(WindowsPlacement p, Transform t)
        {
            if (_anchors.TryGetValue(p, out var cur) && cur == t) _anchors.Remove(p);
        }

        /// <summary>🆕 **2026-10-25（`N5`）**：原版那个**静态锚点覆盖**字段
        /// （`WindowsManager.anchorOverride`，静态 `@0x8`；`out_il2csharp/Assembly-CSharp/WindowsManager.cs:19`）。
        /// <para>语义（逐句照原版 `GetWindowAnchor`）：**`placement == None` 先返回 null，然后只要 override 非空就用它、
        /// 最后才查 `anchors` 字典** ⇒ override 是「临时把所有窗都挂到一个节点上」（原版唯一使用者 =
        /// `TransitionUIAnchor.EnableAnchor/DisableAnchor`，服务于场景过渡 `SimpleSceneTransition`）。</para>
        /// <para>⚠️ **本工程今天没有任何写点**（我们还没有场景过渡那一层）⇒ 恒 null ⇒ 行为零变化，
        /// 记在账里的是「方法已对齐、调用方未建」而不是「不可达」。</para></summary>
        static Transform _anchorOverride;

        /// <summary>= 原版 `WindowsManager.RegisterAnchorOverride(Transform anchor)`（方法体 = `anchorOverride = anchor`）。
        /// <para>🔴 **账里这条判 D**（原版那个包装**全程序集 0 引用** —— 真实写点是 `TransitionUIAnchor.EnableAnchor`
        /// **直接写字段**）。这里仍然给一行实现，只是为了让 `_anchorOverride` 有一个**具名写入口**
        /// （否则将来谁要用就得直接摸私有字段）—— **不影响那条 D 的判断**。</para></summary>
        public static void RegisterAnchorOverride(Transform anchor) { _anchorOverride = anchor; }

        /// <summary>= 原版 `WindowsManager.UnregisterAnchorOverride()`（方法体 = `anchorOverride = null`）。
        /// 原版唯一调用点 = `TransitionUIAnchor.DisableAnchor`（`out_il2csharp/.../TransitionUIAnchor.cs:23`）。</summary>
        public static void UnregisterAnchorOverride() { _anchorOverride = null; }

        /// <summary>自检/诊断用：现在有没有临时锚点覆盖（= 原版 `anchorOverride != null`）。</summary>
        public static bool HasAnchorOverride { get { return _anchorOverride != null; } }

        /// <summary>取锚点。**取不到要报出来**（照原版 `CustomDebug.LogError`）—— 静默返回 null 会让窗口建到场景根上。
        /// <para>🔴 **2026-10-12（`A1629`）就地订正（铁律 5）：`None` 不是「缺锚点」，是原版一个【合法档位】。**
        /// 本行原来没有 `None` 那一支 ⇒ 走到下面那句 `找不到 None 的锚点 —— 场景里缺对应的 WindowHolder`
        /// —— **与本文件 `RegisterAnchor` 里那句报错文案自相矛盾**（那句写着「None 只用于『不挂 Holder』的窗口，
        /// 如 MainMenuWindow」，另一半却把它当缺陷）。**判据（实读）**：主菜单那一扇的 MB
        /// `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1927.json` 的
        /// `windowsPlacement = 0`（= `None`）—— 它**本身就是**那一棵树的父级、不归三个锚点管。
        /// ⇒ `None` **早退返回 null、不出声**；调用点 `AttachToAnchor` 据此**不换父**（窗口留在自己原来的位置）。
        /// ⚠️ **别把它和哨兵 `-1` 混成一档**：哨兵是「忘了赋值」（必须出声），`None` 是「显式写了不挂」（合法）。</para>
        /// <para>🆕 **2026-10-25（`N5`）**：补上原版那一道 **`anchorOverride` 覆盖**（判据见 `_anchorOverride` 的 doc）。
        /// ⚠️ 顺序上我们把**哨兵**那道（我们自己的防御，原版没有）留在 override **之前** ——
        /// 哨兵 = `placement` 忘了赋值，那是 bug，不该被一次临时覆盖悄悄掩盖。</para></summary>
        public static Transform GetWindowAnchor(WindowsPlacement p)
        {
            if (p == WindowsPlacement.None) return null;      // 原版合法值 = 不挂 Holder（主力就是 MainMenuWindow）
            if ((int)p < 0)
            {
                // A166：哨兵只该在 `AttachToAnchor` 里被拦下并出声（那里会带上窗口名）。走到这里 = 有人绕过了它。
                Debug.LogError("[Win] `GetWindowAnchor(哨兵)` —— 这是 `GameWindow.placement` **没显式赋值**时那个默认值，" +
                               "不是任何一档锚点。原版这个字段必填（141/141 都有值）。");
                return null;
            }
            if (_anchorOverride != null) return _anchorOverride;   // = 原版 `if (WindowsManager.anchorOverride != null) return it;`
            if (_anchors.TryGetValue(p, out var t) && t != null) return t;
            Debug.LogError($"[Win] 找不到 {p} 的锚点 —— 场景里缺对应的 `WindowHolder`。" +
                           "主菜单那一层需要 10 / 5 / 15 三个都齐（正本 §三 第 7 条）");
            return null;
        }

        public static bool HasAnchor(WindowsPlacement p) => _anchors.TryGetValue(p, out var t) && t != null;

        public static void ClearAnchorsForTest() { _anchors.Clear(); }

        // ---------------------------------------------------------- 开 / 关

        /// <summary>照原版 `OpenWindowCO` 的判定顺序。
        /// <para>🔴 **2026-10-11（A217③）就地改了两处**（判据 = `d:/2/tools/decomp_full/WindowsManager__OpenWindowCO.c`
        /// 逐句 + 调度台裁定 `资料/普查产出_1010/调度台_口径裁定_1011.md` §A217）：
        /// <list type="number">
        /// <item>**全屏支**：原来是 `currentWindow.Close()`（只收掉当前主窗）⇒ 照原版改成
        ///   `if (openWindows.Count > 0) HideAllWindows();` —— 原版这一句**连弹窗一起藏**
        ///   （`HideAllWindows` 的 predicate 只排除 `state == Closed` 的窗）。
        ///   ⚠️ 这是**可观测的行为变化**：原来「开全屏窗 ⇒ 底下那扇被 `Close()` 掉、出表」，
        ///   现在「被藏起来、**仍在表里**」⇒ 关掉上面那扇时 `ShowPreviousWindow` 会把它**带回来**
        ///   （原版就是这么退回去的）。</item>
        /// <item>**弹窗支之外统一写 `currentWindow`**：原版 `set_CurrentWindow(param_1, param_2)` 写在
        ///   if/else **之后**（`WindowsManager__OpenWindowCO.c:50`，两支都过）⇒ 弹窗也写
        ///   ⇒ **`currentWindow` 就等于「最上面那扇」**（与 `TopWindow` 那段的旁证一致：
        ///   `HidePopUp` 第一件事就是比 `currentWindow == popUpWindow`）。
        ///   ⚠️ 这条修掉一个真缺陷：原来「全屏窗盖在弹窗上」时 `currentWindow` 仍是**被盖住的那扇**，
        ///   而 ESC / 键盘选中走的是 `TopWindow`（= 旧式 `popUpWindow ?? currentWindow`）⇒ **打错窗**。</item>
        /// </list>
        /// ⚠️ **一处如实标注**：弹窗支那个 `currentWindow != win` 条件**是我们加的**（原版没有）——
        ///   后果只是「同一扇弹窗再开一次时不会被自己 `ToBackground()` 一下」。
        ///   🔴 **2026-10-12（A447）就地订正（铁律 5）**：本行原来写的理由是「因为我们 `TryOpen` **恒会**
        ///   `Open()`（那条偏离另记），原版那一压随后也会被盖回 `Open`」—— **前半句 A217② 之后不成立**
        ///   （`TryOpen` 已按 `CurrentState` 分三档，`Open` 支只早退）。**结论仍然成立，理由换一条**：
        ///   那一压把 state 写成 `Background` ⇒ 紧接着的 `TryOpen(...)` 走 **`Background` 支**
        ///   （`state = Open` + 提前台）⇒ 同样盖回 `Open`、同样**不重建内容** ⇒ **仍是零可观测差异**
        ///   （🔴 **2026-10-17 就地订正（铁律 5）**：本行原来写「本仓 `ToBackground` **没有覆写**，
        ///   见 `grep "override void ToBackground"` 零命中」—— **已过期**：`Shell/MainMenuWindow.cs` 的 `override void ToBackground`
        ///   现在**有**覆写（原版 `MainMenuWindow.ToBackground` = 首句幂等 + `base.ToBackground()` +
        ///   `gameObject.SetActive(false)`，`A1629` 落地）⇒ 主菜单被压背景时**整棵关掉**；
        ///   基类那一份仍只写状态、不关物体，与「别的窗」一致）。
        /// </para></summary>
        public void OpenWindow(GameWindow win, object data = null, bool closeAll = false)
        {
            if (win == null) { Debug.LogError("[Win] OpenWindow(null)"); return; }

            // 🆕 **A672 第二层出声**：`type` 还是哨兵 ⇒ 有人**绕过了 `AttachToAnchor`**（或挂上之后又被改回哨兵）。
            //   与 `GetWindowAnchor` 那条哨兵报错**同形**（那里写「走到这里 = 有人绕过了它」）。
            // 🔴 **为什么光有 `AttachToAnchor` 那条不够**：紧接着的那句判的是 `== WindowType.Fullscreen`
            //   （**肯定式**）—— 哨兵 `-1` 在它下面判假 ⇒ 会**静默走弹窗支**（把当前主窗 `ToBackground()`），
            //   那是与「静默走全屏支」**不同的另一种坏法**、同样没有断言会红。这里先落定成**旧默认值**
            //   `Fullscreen` ⇒ 两层哨兵路都只有一种兜底、与改前逐位一致。
            // ⚠️ 今天全仓**零个站**会走到这里（35 个生产建窗点 + 17 处夹具全赋了 `type`，见 `UnsetType` 的 doc）；
            //   这条是**给将来那扇忘了赋的新窗**准备的（那扇窗也会先在 `AttachToAnchor` 那一层出声）。
            if (!win.HasType)
            {
                Debug.LogError($"[Win] `OpenWindow` 拿到一扇 `type` **还是哨兵**（{(int)GameWindow.UnsetType}）的窗：`{win.name}` —— " +
                               "只可能来自「没走 `AttachToAnchor`」或「挂上之后又被改回哨兵」（正常建窗两条路都会赋它）。" +
                               "⚠️ 现在照**旧默认值 `Fullscreen`(0)** 兜底（⛔ 不是照「不等于 `Fullscreen` ⇒ 当弹窗」那条隐含判据走），" +
                               "请去那扇窗的建法里照原版那颗 MB 补上（`0=Fullscreen` / `1=Popup`）。见 A672 / `GameWindow.UnsetType`。");
                win.type = WindowType.Fullscreen;
            }

            if (closeAll) CloseAllWindows();

            if (win.type == WindowType.Fullscreen)
            {
                // 原版：全屏窗开时**场上一扇都不留**（`if (openWindows.Count > 0) HideAllWindows()`）
                if (openWindows.Count > 0) HideAllWindows();
                currentWindow = win;
            }
            else
            {
                // 原版：弹窗开时把上一个压到背景
                if (currentWindow != null && currentWindow != win) currentWindow.ToBackground();
                popUpWindow = win;
                // = 原版那一句写在 if/else **之外**的 `set_CurrentWindow(win)`（见上面第 2 条）
                currentWindow = win;
            }

            win.Manager = this;
            if (!openWindows.Contains(win)) openWindows.Add(win);
            win.TryOpen(data);
            // 🆕 A49：新开的窗成了**最上面那一扇** ⇒ 把「选中」挪到它里面第一颗。
            // 🔴 **2026-10-07（A77㉑⑥）就地订正（铁律 5）**：这里原来写「照 `StandaloneInputModule.ActivateModule`」——
            //    **两句都不对**（判据 = 本机 UGUI 源码
            //    `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/InputModules/StandaloneInputModule.cs`）：
            //    ① `ActivateModule`（`:269`）**只在输入模块被激活时跑一次**（由 `EventSystem.cs:542` 在
            //       `ShouldActivateModule()` 由假转真那一下调）—— **不是每次开窗**；
            //    ② 它取的是 `eventSystem.currentSelectedGameObject`（非空时）**否则** `firstSelectedGameObject`
            //       （`:280-288`）—— 两者**都不是**「窗里层级序第一颗按钮」。
            //    ⇒ 「开窗后默认选中谁」这件事，原版的**时刻与对象都与我们这一次调用不同**；本行是**我们挑的**
            //    （原版那台 `EventSystem` 的实例在 `assets_full` 里根本不存在 ⇒ `firstSelectedGameObject`
            //    取不到判据，见 `PointerLayer` 的「键盘导航」一节 ①）。⛔ **别把它写成「原版就是这么做的」。**
            PointerLayer.SelectFirstIn(win.gameObject);

            // 🆕 **2026-10-19（界面线 `N117` · 用户拍板「方案 C」）**：最上面那一扇变了 ⇒
            //   左栏命中区按「模态 / 非模态」升降（判据 = `GameWindow.BlocksLeftNav`）。
            //   ⚠️ 必须在 `TryOpen` **之后**：`TryOpen` 才会写 `CurrentState`，而 `TopWindow` 读的是它。
            MainMenuRuntime.RefreshNavHitForTopWindow();
        }

        // ============================================================ 🆕 2026-10-25（菜单 C）：按类型取/关/刷新窗
        //
        // 判据（全量反编译）：
        //   · `WindowsManager__GetOpenWindow_object_.c` = `openWindows`（字段 0x50）上 `Linq.FirstOrDefault(x => x is T)` 再转型；
        //   · `WindowsManager__CloseWindow_object_.c` = 先 `GetOpenWindow<T>()`，非空才 `CloseWindow(win)`；
        //   · `WindowsManager__RefreshWindow_object_.c` = 只对 **`currentWindow`（字段 0x58）**：是 T 就调它的
        //     「回前台」虚方法（+0x1a8）并把它写回 `currentWindow`。
        // ⚠️ 我们的等价物：`TryOpen()` 在 `Background` 态那一支正是「SetActive + 回 Open + 不重建」，与 +0x1a8 同净效果。

        /// <summary>原版 `GetOpenWindow&lt;T&gt;`：已开窗里第一扇 `is T`（没有 ⇒ null）。</summary>
        public T GetOpenWindow<T>() where T : GameWindow
        {
            if (openWindows == null) return null;
            for (int i = 0; i < openWindows.Count; i++)
            {
                var w = openWindows[i];
                if (w == null) continue;
                var t = w as T;
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>原版 `CloseWindow&lt;T&gt;`：找到就关（没有 ⇒ 什么都不做）。</summary>
        public void CloseWindow<T>() where T : GameWindow
        {
            var w = GetOpenWindow<T>();
            if (w != null) w.Close();
        }

        /// <summary>原版 `RefreshWindow&lt;T&gt;`：**只认当前顶窗**（`currentWindow`）——是 T 才回前台。</summary>
        public void RefreshWindow<T>() where T : GameWindow
        {
            if (currentWindow == null || !(currentWindow is T)) return;
            currentWindow.TryOpen();          // Background 支 = 回前台、不重建（见上面判据）
        }

        /// <summary>🆕 A49：**最上面那一扇窗**。
        /// 🔴 原版 `WindowsManager.Update` 的 ESC 就是打给 `currentWindow`（字段 0x58，
        /// 实证 = `WindowsManager__get_CurrentWindow.c` 读的就是 0x58），而原版 `OpenWindowCO`
        /// **对弹窗也会 `set_CurrentWindow(win)`** —— 那一句写在 type 分支**之外**
        /// （`WindowsManager__OpenWindowCO.c:50`），旁证：`HidePopUp` 第一件事就是比
        /// `currentWindow == popUpWindow`（`WindowsManager__HidePopUp.c` 读 0x58 / 0x60）
        /// ⇒ **原版的 `currentWindow` 就等于「最上面那扇」**。
        /// <para>🔴 **2026-10-11（A217③）就地订正（铁律 5）**：本属性原来是
        /// `popUpWindow ?? currentWindow`（那段注释还写着「⛔ 不要为了这一条去改 `OpenWindow` 的语义」）——
        /// **A217③ 把 `OpenWindow` 按原版改了**（两支都写 `currentWindow`）⇒ 那个折中**不再需要**，
        /// 本属性直接返回 `currentWindow`（= 原版 `get_CurrentWindow` 的读法）。
        /// 🔴 **不改会怎样**：全屏窗开时 `HideAllWindows()` 会把弹窗藏起来，而 `popUpWindow` 字段
        /// **仍指着那扇藏起来的弹窗**（原版这一支也不清 0x60）⇒ 老写法会让「最上面那扇」**指向一扇藏起来的窗**
        /// ⇒ ESC 与键盘默认选中都会打到它身上（A217③ 修的就是这一处）。
        /// ⛔ 别再退回 `??` 那一版。</para>
        /// <para>`popUpWindow` 字段**留着**：它是 `ShowPreviousWindow` / `NotifyClosed` / `Dump()` 那几处的
        /// 记账（原版 0x60 也是这个角色），只是**不再**参与「谁是顶窗」的判定。
        /// 没有窗 ⇒ null（原版那一刻也是「什么都不做」）。</para></summary>
        public GameWindow TopWindow
        {
            get
            {
                return currentWindow;      // Unity 的假 null（已销毁）也会走这里 ⇒ 调用点照旧自己判
            }
        }

        /// <summary>🆕 **A217③**：原版 `WindowsManager.HideAllWindows()`
        /// （`WindowsManager__HideAllWindows.c` + predicate `WindowsManager.__c__.&lt;HideAllWindows>b__49_0.c` 逐句）——
        /// 把 `openWindows` 里**活着且 `state != Closed`** 的每一扇都 `Hide()` 掉
        /// （= `CurrentState = Closed` + 物体 `SetActive(false)`，见 `GameWindow.Hide`）。
        /// <list type="bullet">
        /// <item>predicate 逐句 = `w != null（Unity 假 null 也算 null）` **且** `*(int*)(w + 0x68) != 0`
        ///   ⇒ **只藏「还开着」的**（已经 `Closed` 的不再动）。</item>
        /// <item>🔴 **不摘表、不动 `currentWindow` / `popUpWindow`**（原版就不动）⇒ 藏起来的窗**仍留在
        ///   `openWindows` 里**，等上面那扇关掉时由 `ShowPreviousWindow` 认**列表尾**把它们带回来
        ///   —— 🔴 **2026-10-12（A217②）订正（铁律 5）**：本行原来写 `UnHide()` / `ReopenFromBackground()`，
        ///   现在两条回来路的**入口都是 `TryOpen`**：列表尾是弹窗时先 `UnHide()` 那一扇「最后一个非弹窗」
        ///   （它把 state 写成 `Background`，正是 `TryOpen` 的 `Background` 支），列表尾自己走完整 `TryOpen`
        ///   （`state == Closed` ⇒ **重建内容** —— 这一档在 A217③ 之后很常见，见 `ShowPreviousWindow` ⑥）。
        ///   ⚠️ **A447 起列表尾那一跳调的是【无参】重载 `TryOpen()`**（原版那一跳调的就是它，
        ///   **不重喂 `data`**）—— 本行原来写的 `TryOpen(null)` 会**清空 `Data`**。</item>
        /// <item>谁调它：`OpenWindow` 的**全屏支**唯一一处（= 原版那一句）。</item>
        /// </list>
        /// ⚠️ 与原版的差别如实记：原版关窗会 `Destroy` + 释放 addressable，我们只 `SetActive(false)`（老账，A123 记过）。</summary>
        public void HideAllWindows()
        {
            for (int i = openWindows.Count - 1; i >= 0; i--)
            {
                var w = openWindows[i];
                if (w == null) continue;                               // = predicate 的 Unity 假 null 那一半
                if (w.CurrentState == WindowState.Closed) continue;     // = predicate 的 `*(int*)(w+0x68) != 0`
                w.Hide();
            }
        }

        // ---------------------------------------------------------- 常驻主菜单（原版 `baseMenu`，字段 0x48）

        /// <summary>🆕 **2026-10-12（`A1629`）**：**当前这一场里那一扇「常驻主菜单」** = 原版
        /// `WindowsManager` 的 `mainMenu` 字段（偏移 **0x48**；`SetBaseMenu` 写、`HideMainMenu`/`ShowPreviousWindow` 读；
        /// 字段名以 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs` 的属性表为准
        /// —— `BaseMenu` / `CurrentBaseMenu` / `ShowMainMenu` / `HideMainMenu` 都在那张 API 表里）。
        /// <para>🔴 **它存在的意义（本笔要修的那件事）**：原版主菜单**自己是一扇窗** ——
        /// `MainMenuWindow.Initialize()` 末尾 `SetBaseMenu(this)` + `OpenWindow(this)`
        /// （`d:/2/tools/decomp_full/MainMenuWindow__Initialize.c`）⇒ 开别的窗时它会被
        /// `HideAllWindows()`（全屏支）/ `ToBackground()`（弹窗支）**一起藏掉**、关掉上面那扇时又被
        /// `ShowPreviousWindow()` 开回来。我们原来没有这一档 ⇒ **模式卡永远露在窗口底下**
        /// （用户 2026-10-12 报的正是这一条，实拍 `_tmp_view/s21_04_collection.png` / `s21_05_practice.png`）。</para>
        /// <para>⚠️ 取值时**照 Unity 的假 null 判**（场景卸载会把那一扇连物体一起销毁）——
        /// 直接 `BaseMenu != null` 就够了（`UnityEngine.Object` 的 `==` 已被重载）。</para></summary>
        public GameWindow BaseMenu { get; private set; }

        /// <summary>原版 `WindowsManager.SetBaseMenu(GameWindow menu)` **逐句**（`WindowsManager__SetBaseMenu.c`）：
        /// <list type="number">
        /// <item>`CustomDebug.Log("...")`（原版第一句就是一句日志）；</item>
        /// <item>字段 `0x48 = menu`；</item>
        /// <item>**把 `openWindows`（字段 `0x50`）整个清空**（`_size = 0` + `_version++` + `Array.Clear`）
        ///   —— ⚠️ 这一句是**真的清表**，⛔ 不是「顺手清一下」：原版那一刻场上那些引用都不该留
        ///   （主菜单是**新一局**的起点）。</item>
        /// </list>
        /// <para>**谁调它**：`Shell/MainMenuRuntime.Build()`（= 原版 `MainMenuWindow.Initialize` 那一跳）。</para></summary>
        public void SetBaseMenu(GameWindow menu)
        {
            Debug.Log($"[Win] `SetBaseMenu`：常驻主菜单 = {(menu != null ? "`" + menu.name + "`" : "<null>")}"
                      + "（原版 `WindowsManager.SetBaseMenu`：写字段 0x48 + 清空 `openWindows`）");
            BaseMenu = menu;
            openWindows.Clear();
            currentWindow = null;
            popUpWindow = null;
            // 🆕 **2026-10-19（`N117`）**：换了一局常驻主菜单 ⇒ 顶窗记账清空 ⇒ 左栏回到可点那一档。
            MainMenuRuntime.RefreshNavHitForTopWindow();
        }

        /// <summary>原版 `WindowsManager.ShowMainMenu()` **逐句**（`WindowsManager__ShowMainMenu.c`）：
        /// `<see cref="OpenWindow(GameWindow, object, bool)"/>(BaseMenu, closeAll: false, options: null)`
        /// —— 且**先判 `BaseMenu` 是非 Unity 假 null**（原版那句是 `Object.op_Implicit`，判假就什么都不做）。
        /// <para>⚠️ 原版还有一条 `private void HideMainMenu()`（`WindowsManager__HideMainMenu.c`：
        /// 对 `0x48` 那一扇虚调一次无参方法）—— **全量反编译里一个调用点都没有**
        /// （`grep -rl "HideMainMenu" d:/2/tools/decomp_full/` **只命中它自己的 .c**）⇒ 判为**死代码**，
        /// 本仓**不实现**，⛔ 别照着补一个出来当「照原版」。</para></summary>
        public void ShowMainMenu()
        {
            if (BaseMenu == null) return;      // 原版那句 `op_Implicit` 判假 ⇒ 直接返回
            OpenWindow(BaseMenu);
        }

        /// <summary>把窗口挂到它自己的锚点上（建场景时调一次；窗口是自己建的，锚点由 `WindowHolder` 给）。
        /// <para>🆕 **A166**：`placement` 还是哨兵（= 这扇窗**忘了显式赋值**）时**出声**
        /// （红线：不许静默失败），并照**老默认值 `Popup`** 兜底建出来 —— 画面不变、但日志里明明白白。</para>
        /// <para>🆕 **A672**：`type` 也照同一条办（哨兵 ⇒ 出声 + 照**旧默认值 `Fullscreen`** 兜底）。
        /// ⚠️ 两条出声的**先后固定**（`placement` 在前）—— `Editor/SettingsScene.cs` 的 A166 那条★断言
        /// 取的是 `errs[0]`，顺序动了它就会去比另一条文案。⛔ 别重排。</para></summary>
        public static void AttachToAnchor(GameWindow win)
        {
            var p = win.placement;
            if (!win.HasPlacement)
            {
                Debug.LogError($"[Win] `{win.name}` 的 `placement` **没有显式赋值**（还是哨兵 {(int)GameWindow.UnsetPlacement}）—— " +
                               "原版这个字段是**必填**的（全库 141 个实例逐个有值），我们逐窗在各自的 `Create()` 里赋。" +
                               "⚠️ 现在照**旧默认值 `Popup`(15)** 兜底建出来（画面不变），但请去那扇窗的 `Create()` 里" +
                               "照原版那颗 MB 补上（`5=Canvas` / `10=World` / `15=Popup`）。见 A166 / `GameWindow.UnsetPlacement`。");
                p = WindowsPlacement.Popup;
            }
            // 🆕 **A672**：`type` 也是哨兵（= 忘了显式赋值）时同样出声。**放在 `placement` 那段之后**——
            //   ① 两条一起构成「建窗那一刻的体检」；② 顺序固定（见方法 doc 的 ⚠️，A166 的断言认 `errs[0]`）。
            // 🔴 **这里要把兜底值【写回】字段**（`placement` 那条不写回、只用一个局部量 `p`）—— 为什么必须写回：
            //   哨兵 `-1` 一旦漏给后面的读者就**静默换了一种坏法** —— `OpenWindow` 下一句判的是
            //   `== Fullscreen`（**肯定式**），哨兵在它下面判假 ⇒ 会走**弹窗支**（把上一个压到背景）。
            //   写回 ⇒ 这扇窗此后所有读者看到的都是**旧默认值** `Fullscreen`，与改前逐位一致。
            //   ⚠️ 副作用是**有意**的：同一个对象再调一次本方法不再重复出声 —— `Editor/SettingsScene.cs`
            //   A166 那条反面断言（「显式赋过值的窗**不报警**」，`errs2.Count == 0`）正是靠它保持绿：
            //   那只探针窗只赋了 `placement`、**从没赋过 `type`**（`:517-533`）。
            if (!win.HasType)
            {
                Debug.LogError($"[Win] `{win.name}` 的 `type` **没有显式赋值**（还是哨兵 {(int)GameWindow.UnsetType}）—— " +
                               "原版这个字段在 prefab 里是**必填**的（`WindowsManager__OpenWindowCO.c` 按它决定「把其余全部藏起来」还是「压到背景」），" +
                               "我们逐窗在各自的 `Create()` 里赋。⚠️ 现在照**旧默认值 `Fullscreen`(0)** 兜底（画面不变），" +
                               "但请去那扇窗的 `Create()` 里照原版那颗 MB 补上（`0=Fullscreen` / `1=Popup`）。见 A672 / `GameWindow.UnsetType`。");
                win.type = WindowType.Fullscreen;
            }
            // 🔴 **2026-10-12（`A1629`）**：`placement == None` = 原版那个**合法**档位「不挂 Holder」
            //   （主力 = 主菜单那一扇，判据 → `GetWindowAnchor` 的注释）⇒ **不换父、不出声**。
            //   ⚠️ 必须放在两条哨兵体检**之后**：哨兵 `-1` 判的是 `!(int)p >= 0`，与这一条是两回事；
            //      而且 `Editor/SettingsScene.cs` 的 A166 断言认 `errs[0]`（顺序动了它就去比另一条文案）。
            if (p == WindowsPlacement.None) return;
            var anchor = GetWindowAnchor(p);
            if (anchor == null) return;
            win.transform.SetParent(anchor, false);
            win.transform.localPosition = Vector3.zero;
            win.transform.localRotation = Quaternion.identity;
            win.transform.localScale = Vector3.one;
        }

        internal void NotifyClosed(GameWindow win)
        {
            // 🆕 A77㉑①：原版 `CloseWindowCO` 里那一跳的判据是「关掉的**是不是 `CurrentWindow`**」
            //   —— 而原版 `CurrentWindow` 就等于「最上面那扇」（弹窗也写它，见本类 `TopWindow` 那段的旁证）
            //   ⇒ 我们这一侧取 `TopWindow` **必须**在摘除**之前**算（摘完 `currentWindow`/`popUpWindow`
            //   都已经清空，那一刻恒为 null）。
            bool wasTop = win == TopWindow;
            openWindows.Remove(win);
            if (currentWindow == win) currentWindow = null;
            if (popUpWindow == win) popUpWindow = null;
            if (wasTop) ShowPreviousWindow();      // = 原版 `WindowsManager__CloseWindowCO.c` 的 `ShowPreviousWindow()`
            // 🆕 **2026-10-19（`N117`）**：顶窗变了（可能换成了一扇非模态的底窗）⇒ 左栏命中区重算。
            //   放在 `ShowPreviousWindow()` **之后**：那一跳会把底窗带回来并写 `currentWindow`。
            MainMenuRuntime.RefreshNavHitForTopWindow();
        }

        /// <summary>🆕 A77㉑①：**把「上一扇」带回来** —— 逐句对位原版
        /// `WindowsManager.ShowPreviousWindow`（`d:/2/tools/decomp_full/WindowsManager__ShowPreviousWindow.c`）。
        /// 原版那一串（每一步都标出我们的对应物，**不同的地方单独说**）：
        /// <list type="number">
        /// <item>`openWindows.RemoveAll(w =&gt; w == null)` —— predicate `b__51_0` 就是 Unity 的假 null 判据
        ///   （`WindowsManager.__c___ShowPreviousWindow_b__51_0.c` 体里只有一句 `Object.op_Equality`）。</item>
        /// <item>列表**空了** ⇒ `CurrentWindow = baseMenu`（字段 0x48）+ `if (baseMenu != null) OpenWindow(baseMenu)`。
        ///   🔴 **我们这一支什么都不做** —— 详见下面那段「我们没有 baseMenu」。</item>
        /// <item>取**列表尾**那一扇（原版 `List.get_Item(Count - 1)`）。</item>
        /// <item>它是弹窗（`type == Popup`，字段 **0x20**）⇒ 在列表里 `LastOrDefault(type != Popup)`
        ///   （predicate `b__51_1` = `*(int*)(w + 0x20) != 1`）并 `UnHide()` 它 —— 把它**重新显示出来、置于背景**。</item>
        /// <item>`CurrentWindow = 列表尾那一扇`；`列表尾.TryOpen()`（非 `Closed` 支 ⇒ 只回 `Open` + `ToFocus`）。</item>
        /// </list>
        /// <para>🔴 **常驻 `baseMenu`（第 2 步）—— `2026-10-12`（`A1629`）起我们【有】了。**
        /// 原文（留痕，铁律 5）：「本仓的主菜单**不是** `WindowsManager` 的窗 … ⇒ 没有可以「开回来」的对象，
        /// 这一步**什么都不做**（主菜单本来就一直在）」—— **那句「主菜单本来就一直在」正是缺陷本体**：
        /// 它既不会被 `HideAllWindows` 藏掉、也不会被开回来，于是**开任何一扇窗时模式卡/主界面全露在外面**
        /// （用户 2026-10-12 实拍 `_tmp_view/s21_04_collection.png` · `s21_05_practice.png`）。
        /// 现在 `Shell/MainMenuWindow.cs` 是**真的**一扇 `GameWindow`（原版同名类），
        /// `Shell/MainMenuRuntime.Build()` 按原版 `MainMenuWindow.Initialize()` 那两句
        /// `SetBaseMenu` + `OpenWindow` 把它登记进栈 ⇒ 本步**照原版那一跳执行**。
        /// 判据全文（含原版 .c/.json 逐条）→ `Shell/MainMenuWindow.cs` 的文件头。</para>
        /// <para>🔴 **为什么要它**（㉑ 的裁定：三件必须一起做）：原版 `CloseWindowCO` 关掉**最上面那扇**时会调本方法
        /// ⇒ 关掉弹窗之后，底下那扇从 `Background` **回到 `Open`**。我们原来没有这一跳 ⇒ 底窗**停在 `Background`**；
        /// 又因为 `KeyCancel` 那时也没问 `IsOpen()`，ESC **照样能关它** —— 两处偏离**凑巧抵消**、看着像对的
        /// （这正是那条旧断言 `…它被压到背景态` 在替我们背书的由来）。
        /// ⇒ 本方法 **与 `PointerLayer.KeyCancel` 里那道 `IsOpen()` 门槛必须成对**，⛔ 不许只留一件。</para>
        /// <para>⚠️ **可见性照原版 = `private`**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs` 的 ``可见性照原版 = `private` ``
        /// 那一行就是 `private void ShowPreviousWindow()`）—— 自检**不直调它**、走生产那条路
        /// （`KeyCancel` → `Close` → `NotifyClosed` → 这里），比直调更有鉴别力。</para></summary>
        void ShowPreviousWindow()
        {
            // ① 死条目（原版 predicate = Unity 假 null；`openWindows` 里理论上不留死引用，但照原版清一遍不花钱）
            for (int i = openWindows.Count - 1; i >= 0; i--)
                if (openWindows[i] == null) openWindows.RemoveAt(i);

            if (openWindows.Count == 0)
            {
                // ② 原版：`CurrentWindow = baseMenu` + `if (baseMenu != null) OpenWindow(baseMenu)`
                //    —— 🆕 **2026-10-12（`A1629`）起我们【有】baseMenu 了**（`Shell/MainMenuWindow.cs` +
                //    `MainMenuRuntime.Build()` 的登记那一跳，判据 → `SetBaseMenu` 的 doc）。
                //    ⚠️ 原文（留痕，铁律 5）：「我们没有常驻 `baseMenu` … 这一步**什么都不做**（主菜单本来就一直在）」
                //    —— 那一句**就是本笔要修的那个缺陷本体**：主菜单「一直在」= 它永远盖不住也永远不被盖，
                //    于是开窗时模式卡全露在外面。
                //    🔴 **这一跳是「关掉最上面那扇之后主菜单回来」的唯一入口** ⇒ 删了它，主菜单**再也回不来**
                //    （比一直可见更糟：`HideAllWindows` 把它藏成 `Closed` 之后没人带它回来）。
                currentWindow = BaseMenu;          // = 原版 `set_CurrentWindow(baseMenu)`（写在判空之前）
                popUpWindow = null;
                if (BaseMenu != null) OpenWindow(BaseMenu);   // = 原版那一跳（判 Unity 假 null）
                return;
            }

            var prev = openWindows[openWindows.Count - 1];                   // ③ 列表尾

            GameWindow lastMain = null;                                      // ④ 最后一个「非弹窗」
            for (int i = openWindows.Count - 1; i >= 0; i--)
                if (openWindows[i] != null && openWindows[i].type != WindowType.Popup) { lastMain = openWindows[i]; break; }

            if (prev.type == WindowType.Popup && lastMain != null) lastMain.UnHide();

            // ⑤ 顶窗记账：**照原版** `CurrentWindow = 列表尾那一扇`（原版第 4 步 —— 不论它是不是弹窗）。
            //    🔴 **2026-10-11（A217③）就地订正（铁律 5）**：这里原来写
            //      `if (prev.type == Popup) { popUpWindow = prev; currentWindow = lastMain; }`
            //      —— 那个「列表尾是弹窗 ⇒ `currentWindow` 让给主窗」的分格，**只在旧模型下自洽**
            //      （旧模型里 `currentWindow` = 主窗、`TopWindow = popUpWindow ?? currentWindow`）。
            //      A217③ 之后 `currentWindow` 的语义 = **最上面那扇**（`OpenWindow` 两支都写它）
            //      ⇒ 再把 `currentWindow` 让给 `lastMain`，`TopWindow` 就会指向**被压在下面那扇**
            //      （ESC / 键盘默认选中直接打错窗 —— 与本条要修的是同一个缺陷，只是换了个入口）。
            //    `popUpWindow` 仍按我们这一份记账填（原版只有 `currentWindow` 一个字段）。
            currentWindow = prev;
            popUpWindow = prev.type == WindowType.Popup ? prev : null;

            // ⑥ = 原版 **无参** `TryOpen()` —— 🔴 **2026-10-12（A217②）就地订正（铁律 5）**：
            //    这里原来是 `prev.ReopenFromBackground()`（= 只做「非 `Closed` 支」那两步），
            //    并在注释里自陈「与 `TryOpen` 的差归 A217② 那条待查证」。
            //    A217② 已把 `TryOpen` 按原版改成**按 `CurrentState` 分三档**（`Closed ⇒ 重建内容` /
            //    `Background ⇒ 只提前台` / `Open ⇒ 什么都不做`，判据 → `TryOpen` 那段），
            //    ⇒ **直接调它就逐句等于原版**；那个「只做前两步」的替身（`ReopenFromBackground`）
            //    **在生产路径上已让位给本行**（方法还留着，只为 `Editor/ShellScene.cs` 那个既有调用点；
            //    ⛔ 以后别再从生产代码调它）。
            //    ⚠️ **这条今天是可观测的**：A217③ 之后「列表尾是 `Closed`」很常见
            //    （`HideAllWindows` 藏起来的窗就是 `Closed`）⇒ 原版那一刻会走「`SetActive(true)` +
            //    `state=Open` + **`Open()`（重建内容）**」那一支；原来我们只做前两步，**内容不重建**。
            //
            //    🔴 **2026-10-12（A447）就地订正（铁律 5）：本行原来是 `prev.TryOpen(null)` —— 那个写法是错的。**
            //    原版这一跳调的是**无参**重载 `GameWindow.TryOpen()`
            //    （判据 = 本件亲读指令流：`WindowsManager__ShowPreviousWindow` @ `0x180876560` 的
            //    `0x1808767C1`–`0x1808767D0` = `mov rcx,[rdi+0x58]`（`currentWindow`）→ `xor edx,edx`
            //    → **`call 0x180835fd0`**），而**无参那条一次都不调 `SetupData`**（`0x180835FD0` 序言之后
            //    头一条就是读 `CurrentState`；带参那条 `0x180836120` 头三句才是 `call [rax+0x188]` = `SetupData`）
            //    ⇒ 原版回来之后那扇窗的 `Data` 是**原样保留**的。
            //    ⚠️ 而这里原来那句注释写的「传 `null` 与那次无参调用**同义**」**不成立**：
            //    传 `null` 走的是**带参**重载 ⇒ `SetupData(null)` 把 `Data`（以及三个覆写
            //    `CampaignRewardWindow` / `RewardWindow` / `MissionRerollPopup` 里的 `_ctx` / `Index`）
            //    **清成 null/0**。「同义」只到「都不重建内容」那一层，**写数据这一层正好相反**。
            prev.TryOpen();     // = 原版那一跳：**无参**重载 ⇒ 不重喂 `data`、`Data`/`_ctx` 原样保留（A447）
        }

        /// <summary>🆕 **A1709**：收掉场上**每一扇**窗（原版 `WindowsManager.CloseAllWindows`）。
        /// <para>**原版逐句**（`d:/2/tools/decomp_full/WindowsManager__CloseAllWindows.c`，2026-10-12 现读）：
        /// ① 取 `openWindows.ToArray()` 的**快照**（`:18-20`，`List&lt;T&gt;.ToArray`）；
        /// ② 在快照上**从尾到头**走，对每一颗**非 Unity 假 null** 的调虚表 **Slot 8 = `Close()`**
        /// （`:37` 的 `(**(code **)(*plVar4 + 0x1b8))()`；`0x1b8` = Slot 8，由
        /// `GameWindow__TryOpen.c:47` 的 `0x178` = Slot 4 `Open()` 起、每槽 `0x10` 推得，
        /// 且与 `HideAllWindows.c:63` 的 `0x1c8` = Slot 9 `Hide()` 互证）——
        /// ⚠️ **不是 `Hide()`**（那一跳在 `CloseWindowCO` 里），所以「逐扇 `Close()`」这一步我们本来就是对的；
        /// ③ 收尾 **`List.Clear()`**（`:40-47` = `_version++` + `_size = 0` + `Array.Clear`）——
        /// **原版这里确实是无条件清表**，我们跟它一致（⛔ 别把第 ③ 步读成「只清一部分」）。
        /// 🔴 原版之所以能无条件清表，是因为它第 ② 步里**关到 `baseMenu` 自己**时走的是另一支
        /// （`CloseWindowCO` 的 `param_2 == baseMenu` ⇒ **只 `Hide()` 就 return**，不摘表也不
        /// `ShowPreviousWindow`）⇒ **不会**出现「关它自己却把它重新开回来」那一拍；**我们缺那一支**
        /// （如实标注 → `Close()` 的注释里那一条），所以本方法第 ③ 步**不能照抄无条件清表**，
        /// 否则会把刚被开回来的常驻主菜单**摘成孤儿**（= A1709，后果见下面那句注释）。</para></summary>
        public void CloseAllWindows()
        {
            for (int i = openWindows.Count - 1; i >= 0; i--)
                if (openWindows[i] != null) openWindows[i].Close();
            // 🔴 **2026-10-12（`A1709`）就地改（铁律 5）**：本行原来是**无条件** `openWindows.Clear();`。
            //   上面那一圈的**最后一拍**会把常驻主菜单**重新开回来**：关掉 `baseMenu` 自己（它在表里下标 0、
            //   被下降循环**最后**关到）时 `Close()` → `NotifyClosed` → `ShowPreviousWindow` 的「列表空了」支
            //   （`WindowsManager.cs` 的 `OpenWindow(baseMenu)` = 原版 `OpenWindow(baseMenu)`）⇒ 它又是 `Open` + active、
            //   而且**已经被 `openWindows.Add` 加回表**。无条件清表把这一条摘掉 = **孤儿**（开着的窗不在册）。
            //   🔴 后果**不是记账难看，是真画错**：`OpenWindow` 的全屏支是
            //   `if (openWindows.Count > 0) HideAllWindows();`（本类 `本文件`，逐字照原版 `OpenWindowCO.c:32-37`）
            //   ⇒ 表被清空 ⇒ **主菜单那一棵不会被藏** ⇒ 它一直露在新开的那扇全屏窗底下
            //   （用户 2026-10-12 报的「界面相互重叠」那一族；断言 → `Editor/MainMenuScene.cs`）。
            //   ⇒ 只摘**真的已经关掉**的：`null`（Unity 假 null / 已销毁）与 `CurrentState == Closed`。
            //   ⚠️ **故意不摘**「物体不活、但 `state != Closed`」那一档（绕过 `Close()`/`Hide()` 造出来的脏现场）——
            //   留在册上，`TryOpen` 的早退守卫（本类 `本文件`）才会**出声**，不静默（铁律：不许静默失败）。
            for (int i = openWindows.Count - 1; i >= 0; i--)
            {
                var w = openWindows[i];
                if (w == null || w.CurrentState == WindowState.Closed) openWindows.RemoveAt(i);
            }
            currentWindow = null;
            popUpWindow = null;
            // 🆕 **2026-10-19（`N117`）**：场上收空了 ⇒ 左栏回到「可点」那一档（模态闸不要留残留）。
            MainMenuRuntime.RefreshNavHitForTopWindow();
        }

        // ---------------------------------------------------------- 开窗入口：**按引用复用**（A123 落地 · A177 收编）

        // 🔴 **原版判据（三层，全部本地可复现）** → `资料/普查产出_1007/波6_A123_入口复用.md` ·
        //   `资料/普查产出_1007/波6判据核查.md` §2 · `资料/普查产出_1006/甲5_A97_A99_A104_A105.md` §二 A104：
        //   ① 缓存字段 = **`WindowsManager.automaticallyLoadedWindows`**
        //      （`dump.cs`：`BiDirectionalDictionary<ComponentReference<GameWindow>, GameWindow>` @ **0x68**，
        //      **键 = prefab 引用**）；② `WindowsManager.OpenWindow` **第一件事就是查它**（`TryGetValue`
        //      在 `Instantiate` **之前**，命中 `jne` 直接跳复用块，实测 VA `0x180875990`）
        //      ⇒ **同一扇窗点两次只有一个实例**；③ 🔴 **命中只在「窗还开着」时发生**：
        //      `WindowsManager__CloseWindowCO.c` 关窗时会 `Object.Destroy(gameObject)` + `ComponentReference.Release`
        //      + **把缓存条目删掉** ⇒ 关过之后再点，**原版走的是新建**。
        //
        // 🔴 **2026-10-11（A177）**：这一份**从 `Shell/MainMenuRuntime.cs` 搬到了这里** —— 那个缓存字段
        //   **本来就是 `WindowsManager` 的**（证据见上面 ①），A123 当年因为「`WindowsManager` 是共用件、
        //   本批不动」把它留在了入口那一侧；A177 要收编的正是「**没走这条机制**的那几条入口」。
        //   ✅ **已合并成一份** —— 🔴 **2026-10-15（A633）就地订正（铁律 5）**：本段原来写「**今天仓里仍是两份**
        //   （`MainMenuRuntime._openByRef` 那 8 条主菜单入口还在用它自己那一份）……**要等调度台把那边 8 条
        //   改成转调这里**（改法：整个方法体换成一行 `=> WindowsManager.OpenByRef(prefabRef, create, closeAll);`
        //   并删掉它自己那份 `_openByRef` / `StillOpen`；**已写进报告**）」—— **那件事已经做完了**
        //   （A177 尾巴，2026-10-11 当天；原文留着更正痕迹，⛔ 别再照它排一次活）。实据（现读）：
        //   `Shell/MainMenuRuntime.cs` 里**已经没有** `_openByRef` 字段，它那 8 条入口的 `OpenByRef`
        //   只剩一行转调（`Shell/MainMenuRuntime.cs` 的 `=> WindowsManager.OpenByRef(prefabRef, create, closeAll);`），
        //   全文只剩讲这段历史的注释；全仓 `_openByRef` 的**字段声明只有本文件这一处** ⇒ 跨入口去重是真的生效了。
        //   ⚠️ 守着它的断言 → `Editor/MainMenuScene.cs` ⑥「两份缓存**已合并成一份**」那一节
        //   （★ 两条：两条入口开出来的是**同一扇** · `openWindows` 里聊天窗只有 **1** 扇）。
        //   ⛔ 别在任何入口那一侧再抄第三份。
        //
        // **我们的等价物**（与原版逐条对位）：
        //   · 缓存 = `_openByRef`，键 = **prefab 根名**（与各自 `Create()` 里 `new GameObject(...)` 用的那个名字
        //     同源 ⇒ 真·「按引用」；⚠️ 改名要两处一起改，几条共用名收在下面那几个 `PrefabRef*` 常量里）；
        //   · 「还开着吗」= `WindowsManager.openWindows` 里还有没有它 —— `GameWindow.Close()` →
        //     `NotifyClosed` 会把它摘掉 ⇒ **与判据③同义**；
        //   · 关窗那一刻我们**拿不到回调**（`GameWindow.Close()` 里没有事件）⇒ 条目**惰性**删（下一次开窗时）。
        //     对外可观测的行为与判据③一致：**关过之后这条引用永不复用**。
        //   · **`static`**：原版那份缓存在 **manager** 上（不随主菜单那一层重建而丢），而窗是挂在锚点上的、
        //     主菜单重建也不消失 ⇒ 放成实例字段会在「主菜单重来一次、旧窗还开着」时**又建一扇**（正是要修的）。
        //   ⛔ **别用「实例还在就复用」**（`XxxWindow.Instance == null` 那种写法）—— 我们的 `Close()` 只
        //     `SetActive(false)`、**不销毁**（原版是 `Object.Destroy`）⇒ 「实例还在」在关窗之后**恒为真**、
        //     等于永不新建。断言 → `Editor/MainMenuScene.cs` 的 A123 那一节（最锋利的那条就盯它）。
        static readonly Dictionary<string, GameWindow> _openByRef = new Dictionary<string, GameWindow>();

        /// <summary>几条「prefab 根名」的**唯一出处**（= 各自 `Create()` 里 `new GameObject(...)` 用的那个名字）。
        /// ⚠️ `MainMenuRuntime` 那边也有几个同形的 `Ref*` 常量 —— 🔴 **2026-10-15（A633）就地订正（铁律 5）**：
        /// 本行原文写「另有一份同名的 `Ref*` 常量（**合并时以这里为准**）」—— 那个「合并」**已经发生**
        /// （A177 尾巴，2026-10-11）。现读实况：与这里**重名的只有 2 个**（`RefChat` / `RefProfile`），
        /// 它们现在是**直接取这一份**的别名（`Shell/MainMenuRuntime.cs` 的 `= WindowsManager.PrefabRefChat;`），
        /// 字面量只留一个出处；其余几个（`RefInbox` / `RefSettings` / `RefSocial` / `RefRewards` /
        /// `RefCollection` / `RefShop`）是**那边独有**的入口键，这里没有对应物 ⇒ 不存在「以谁为准」的问题。
        /// ⛔ 别再往这一侧抄一份。
        /// ⚠️ 排行榜那一条对**两棵** prefab（遭遇榜 / 经典榜）⇒ 键由 `LeaderboardWindow.NameOf(kind)` 给，
        /// 那是「kind → prefab 根名」的唯一来源（`Create` 建 GO 用的也是它），别在这再抄一份。</summary>
        public const string PrefabRefChat = "ChatPanel";
        public const string PrefabRefProfile = "Player Profile Window";
        public const string PrefabRefBattleLog = "Battle Log Popup";

        /// <summary>🆕 **A251-L1**：`BaseOfferPopup` 那一族（**21 个 prefab = 1 母版 + 20 变体**）的
        /// **母版** prefab 根名 —— 它就是 `Shell/BaseOfferPopup.cs` 的 `Variants[0].Prefab`，
        /// 也是 `Create()` 里 `new GameObject(...)` 用的那个名字（**两处同源**：那边直接取 `Variant.Prefab`）。
        /// ⚠️ **变体不各占一个键**：原版那 20 份变体是**不同的 prefab**，但对我们来说「同一扇窗换一套几何/
        /// 抽屉表」⇒ 复用键取**母版名**、变体由 `OpenBaseOfferPopup(variant, …)` 的实参选
        /// （`OpenByRef` 那一套缓存认的是「prefab 引用」，两处不同名会被当成两扇窗）。</summary>
        public const string PrefabRefBaseOfferPopup = "Base Offer Popup";

        /// <summary>🆕 **A251-L2**：`GenericOptionsPanel` 那一扇的 prefab 根名（= `Create()` 里 `new GameObject(...)`
        /// 用的那个名字，**两处同源**）。
        ///
        /// <para>🔴 **2026-10-13（A1718）就地更正（铁律 5）**：本行原文写「⚠️ **本地没有任何调用方**
        /// （全量反编译里只命中它自己的 20 个 `.c`）」—— **两句都不成立**：
        /// · **本地有调用方、而且一直有** = `Shell/DeckInfoPopup.cs` 的 `ShareOnChat()`（`Share On Chat` 那颗钮
        ///   ⇒ `WindowsManager.OpenGenericOptionsPanel(ctx, null)`）；原版对应那一跳 =
        ///   `d:/2/tools/decomp_full/DeckInfoPopup__ShareDeckOnChat.c`（本件 `grep -rln GenericOptionsPanel`
        ///   现读命中它）。⚠️ 入口那一段的正文在上面 `Shell/DeckInfoPopup.cs` 与本文件
        ///   `OpenGenericOptionsPanel` 的 doc，⛔ 这里不抄第二份（铁律 6）。
        /// · 那个「20 个 `.c`」是**按【文件名】数**出来的：`ls d:/2/tools/decomp_full/ | grep -c GenericOptionsPanel`
        ///   现读 = **44**；而**调用方的文件名里根本没有这个词** ⇒ 按文件名数，**永远数不到调用方**。
        /// ⇒ **错因**：当年只按【文件名】grep、没按【内容】grep（`grep -rln` 一次就能命中
        /// `DeckInfoPopup__ShareDeckOnChat.c`）。</para></summary>
        public const string PrefabRefGenericOptionsPanel = "Generic Options Panel";

        /// <summary>🆕 **2026-10-17（界面线 T18/T25）**：`ItemDisplayWindow`（物品展示窗）的 prefab 根名
        /// （= `Shell/ItemDisplayWindow.cs` 的 `Create()` 里 `new GameObject(...)` 用的那个名字，**两处同源**）。
        /// <para>🔴 **入口是本地判据、不是猜的**：原版 `CardbackCollectionTab__OnItemSelected.c`
        /// 构造 `ItemDisplayWindowContext(item, 1)` 后 `WindowsManager.OpenWindow&lt;ItemDisplayWindow&gt;`
        /// ⇒ 收藏窗卡背页点一张卡背开的就是这一扇（判据全文 → `Shell/ItemDisplayWindow.cs` 文件头）。</para></summary>
        public const string PrefabRefItemDisplay = "Item Display Window";

        /// <summary>🆕 **2026-10-17（界面线 T18/T27）**：`WhereToGetCardsPopup`（卡牌来源窗 · 原版 `WhereToGetCardsWindow`）
        /// 的 prefab 根名（= `Shell/WhereToGetCardsPopup.cs` 的 `Create()` 里 `new GameObject(...)` 用的名字，**两处同源**）。
        /// <para>🔴 **原版入口是本地判据、不是猜的**：`ExpansionPassEventTab.cs:25` 与 `GachaEventTab.cs:20` 各有一个
        /// `AssetReferenceTyped&lt;WhereToGetCardsWindow&gt; getCardsWindow` ⇒ 原版由**事件页那两个 tab** 开出来。
        /// ⚠️ 我们**没有建承载窗 `Parent Event Window`** ⇒ 这里只提供「怎么开」，⛔ 不伪造调用点
        /// （同 `TutorialMessagePopup` / `T26` 口径；判据全文 → `Shell/WhereToGetCardsPopup.cs` 文件头）。</para></summary>
        public const string PrefabRefWhereToGetCards = "WhereToGetCardsPopup";

        /// <summary>🆕 **2026-10-17（界面线 T28）**：`ParentEventWindow`（活动承载窗 · 原版 `BaseParentEventWindow`
        /// 那一族 / prefab 根名 `Parent Event Window`）的 prefab 根名
        /// （= `Shell/ParentEventWindow.cs` 的 `Create()` 里 `new GameObject(...)` 用的名字，**两处同源**）。
        /// <para>⚠️ **本地没有生产入口**：原版由 `ParentEventHandler` / liveops 事件链打开，我们没有那条链
        /// ⇒ 只提供「怎么开」，⛔ 不伪造调用点（同 `TutorialMessagePopup` / `T26` / `T27` 口径）。</para>
        /// <para>⚠️ 窗参 / `tabs` 取自 **`Two Sides Event Window`** 那份 prefab（`tabs` = 2），
        /// 外壳树取自 `Parent Event Window` —— 两份 prefab 的事实与「为什么这样组合」→
        /// `Shell/ParentEventWindow.cs` 文件头「两个 prefab 的事实」。</para></summary>
        public const string PrefabRefParentEvent = "Parent Event Window";

        /// <summary>🆕 **2026-10-19（活动线 L01）**：`GachaEventWindow`（Gacha 抽卡活动页 · 原版 `GachaEventTab`
        /// 那一页）的注册键。
        /// <para>⚠️ **原版没有独立窗口 prefab**：`GachaEventTab : WindowTabBase&lt;BaseParentEventWindow&gt;`
        /// 是 `Parent Event Window` 里由 liveops 事件列表**动态建出来**的一个 tab
        /// （`BaseParentEventWindow__SetupTabs.c` / `CreateEventTab.c`）。本地没有 liveops 事件列表
        /// ⇒ 我们建的是「**承载窗外壳 + 恰好一个 Gacha 页**」的等价形态，根名沿用原版**页** prefab 的
        /// `Gacha Tab`（如实记录，⛔ 不谎称原版有这扇窗）。判据 → `Shell/GachaEventWindow.cs` 文件头。</para></summary>
        public const string PrefabRefGachaEvent = "Gacha Tab";

        /// <summary>🆕 **2026-10-19（活动线 L02）**：`RaidEventWindow`（Raid 团队活动页 · 原版 `RaidEventTab`
        /// 那一页）的注册键。
        /// <para>⚠️ **原版没有独立窗口 prefab**（与 <see cref="PrefabRefGachaEvent"/> 同一条口径）：
        /// `RaidEventTab : WindowTabBase&lt;BaseParentEventWindow&gt;, ILiveOpsTab`
        /// 是 `Parent Event Window` 里由 liveops 事件列表**动态建出来**的一个 tab
        /// （`BaseParentEventWindow__SetupTabs.c` / `CreateEventTab.c`）。本地没有 liveops 事件列表
        /// ⇒ 我们建的是「**承载窗外壳 + 恰好一个 Raid 页**」的等价形态，根名沿用原版**页** prefab 的
        /// `Raid Progress Tab`（如实记录，⛔ 不谎称原版有这扇窗）。判据 → `Shell/RaidEventWindow.cs` 文件头。</para></summary>
        public const string PrefabRefRaidEvent = "Raid Progress Tab";

        /// <summary>🆕 **2026-10-19（活动线 L03）**：`ExpansionPassEventWindow`（Expansion Pass 战令页 · 原版
        /// `ExpansionPassEventTab` 那一页）的注册键。
        /// <para>⚠️ **原版没有独立窗口 prefab**（与 <see cref="PrefabRefGachaEvent"/> / <see cref="PrefabRefRaidEvent"/>
        /// 同一条口径）：`ExpansionPassEventTab : WindowTabBase&lt;BaseParentEventWindow&gt;, ILiveOpsTab`
        /// 是 `Parent Event Window` 里由 liveops 事件列表**动态建出来**的一个 tab
        /// （`BaseParentEventWindow__SetupTabs.c` / `CreateEventTab.c`）。本地没有 liveops 事件列表
        /// ⇒ 我们建的是「**承载窗外壳 + 恰好一个 Pass 页**」的等价形态，根名沿用原版**页** prefab 的
        /// `Expansion Pass Tab`（如实记录，⛔ 不谎称原版有这扇窗）。判据 → `Shell/ExpansionPassEventWindow.cs` 文件头。</para>
        /// <para>⚠️ 原版还有**第二个 tab** `Expansion Pass Missions Tab`（任务页）；本窗**两页都建**
        /// （页签栏两颗：Pass / Missions），判据与数据缺口 → 同文件头。</para></summary>
        public const string PrefabRefExpansionPassEvent = "Expansion Pass Tab";

        /// <summary>🆕 **2026-10-17（界面线 T18）**：`TutorialMessagePopup`（原版 `TutorialMessageWindowPopUp`）的
        /// prefab 根名（= `Shell/TutorialMessagePopup.cs` 的 `Create()` 里 `new GameObject(...)` 用的名字，**两处同源**）。
        /// <para>⚠️ **本地没有生产入口**：原版由 `TutorialManager.ShowTutorialMessageWindow` ← 3 个教程 stage 的
        /// `DoMenuActions` 开出来，而我们**没有菜单版教程 stage 状态机**（判据 → `Shell/TutorialMessagePopup.cs` 的类 doc
        /// 与 `资料/普查产出_1017/UI/R2_暂缓批复核.md` §1·1）⇒ 这里只提供「怎么开」。</para></summary>
        public const string PrefabRefTutorialMessage = "Tutorial Message Window PopUp";

        /// <summary>🆕 **2026-10-17（界面线 T18）**：`WelcomePopup`（原版 prefab 根名 `WelcomeScreen Alpha`）的
        /// prefab 根名（= `Shell/WelcomePopup.cs` 的 `Create()` 里 `new GameObject(...)` 用的名字，**两处同源**）。
        /// <para>⚠️ **本地没有生产入口**：原版由 FTUE onboarding（`!CheckTag(CollectedFirstDeck)`）开出来，
        /// 我们没有 tag 存档 / FTUE ⇒ 这里只提供「怎么开」，**不自己判显隐**。</para></summary>
        public const string PrefabRefWelcome = "WelcomeScreen Alpha";

        /// <summary>🆕 **2026-10-17（界面线 T26）**：`FTUESelectDeckWindow`（选阵营送卡组窗，prefab
        /// `Choose Army Deck FTUE Window`）的 prefab 根名（= `Shell/FTUESelectDeckWindow.cs` 的 `Create()` 里
        /// `new GameObject(...)` 用的那个名字，**两处同源**）。
        /// <para>⚠️ **本地没有生产入口**：原版是 FTUE onboarding（首启送卡组）的一环，而我们**没有菜单版教程状态机**
        /// （判据 → `Shell/FTUESelectDeckWindow.cs` 的文件头与 `Shell/TutorialMessagePopup.cs` 的类 doc）
        /// ⇒ 这里只提供「怎么开」，⛔ 不伪造调用点。</para></summary>
        public const string PrefabRefChooseArmyDeck = "Choose Army Deck FTUE Window";

        /// <summary>🆕 **2026-10-17（界面线 T18 收尾）**：排位赛季三窗 + 反馈问卷的 prefab 根名
        /// （各自 = 自己 `Create()` 里 `new GameObject(...)` 用的名字，**两处同源**）。
        /// <para>⚠️ 四扇**本地都没有生产入口**：三窗由排位事件结算链开、反馈窗的 `Feedback Button` 在这个
        /// build 里出厂关着（判据 → `Shell/GiveFeedbackPopup.cs` 文件头）⇒ 这里只提供「怎么开」，⛔ 不伪造入口。</para>
        /// <para>⚠️ `Ranked New Season WIndow` 的 **`WIndow`（大写 I）是原版拼写**，⛔ 别「修正」它。</para></summary>
        public const string PrefabRefRankedDivisionChange = "Ranked Division Change Window";
        public const string PrefabRefRankedNewSeason = "Ranked New Season WIndow";
        public const string PrefabRefRankedSeasonEnded = "Ranked Season Ended Window";
        public const string PrefabRefGiveFeedback = "Give Feedback Popup menu";

        /// <summary>🆕 **A251-L2**：`AllianceMemberOptionsPopup` 那一扇的 prefab 根名。
        /// ⚠️ **本地走不到**（八颗钮的显隐全要服务器，见 `Shell/AllianceMemberOptionsPopup.cs` 文件头）。</summary>
        public const string PrefabRefAllianceMemberOptions = "Member Options Panel";

        /// <summary>🆕 **A251-L2**：`PurchasePremiumWindow` 那一扇的 prefab 根名。
        /// 🔴 它属于「**带参数的窗口**」那一族（`TryOpen(data, options)` 覆写，全库 7 个 —— 见上面 `TryOpen`
        /// 那一段的注释），参数由 <see cref="OpenPurchasePremiumWindow"/> 的实参给。</summary>
        public const string PrefabRefPurchasePremium = "Purchase Premium Window";

        /// <summary>🆕 **A251-L4**：`RankedRewardEventWindow` 那一扇的 prefab 根名。
        /// ⚠️ ⛔ **别与 `PrefabRefRankedEvent`（`RankedEventWindowV2`）混** —— 那是另一扇窗。</summary>
        public const string PrefabRefRankedRewardEvent = "Ranked Boost Reward Event Window";

        /// <summary>🆕 **A251-L3**：`ReferralPopupWindow`（推荐人窗）那一扇的 prefab 根名（= `Create()` 里
        /// `new GameObject(...)` 用的那个名字，**两处同源**）。
        /// 🔴 **它的 prefab 不在 `menus` 包里** —— 在 **`bundle_generalgamewindows_assets_all`**
        /// （同批另六扇都在 `bundle_menus_assets_all`；这是最容易踩的一格）。
        /// ⚠️ 原版的入口是 `ReferralContainer.OpenPopup()`（LiveOps 容器链）—— **我们没建那个容器**
        /// ⇒ 这里只提供「怎么开」，入口由调用方自己定。</summary>
        public const string PrefabRefReferralPopup = "Referral Popup";

        /// <summary>🆕 **A103**：`EnergySinglePlayerOnlyEventWindow`（单机能源活动窗）的 prefab 根名
        /// （= `Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `Create()` 里 `new GameObject(...)` 用的那个名字，
        /// **两处同源**）。
        /// <para>⚠️ **A103 的另一半（`Alliance Event Score Panel` / `… Info`）不在这里** —— 那两件
        /// 根组件是 **`MonoBehaviour`**（`AlliancesEventScorePanel` / `AllianceScoreBar`）、**不是 `GameWindow`**
        /// ⇒ **没有 `type` / `placement` / `closeOnESC` 可填、也不注册**（⛔ 别为了「凑一条注册」给它们编窗身份）。
        /// 它们的建法 = `Shell/AllianceEventScorePanel.cs` 的 `Create(parent, x1, y1)`。</para>
        /// <para>⚠️ 原版这一扇走 `LiveOps` 事件链（`SinglePlayerScoringEvent`，远端 CCD）⇒ **全量反编译里没有
        /// 第二个引用它的地方** ⇒ 我们只提供「怎么开」，**入口由调用方自己定**（⛔ 不编一个）。</para></summary>
        public const string PrefabRefEnergyEvent = "EnergySinglePlayerOnlyEventWindow";

        /// <summary>🆕 **A177**：按 **prefab 引用**开窗 —— **原版 `automaticallyLoadedWindows` 命中就复用**的等价物。
        /// 还开着 ⇒ 复用同一扇（同一个实例再走一遍开窗流程，不新建）；关过 / 没建过 ⇒ 新建。两条路都出声（红线）。
        ///
        /// <para>🔴 **2026-10-12（A217②）就地订正（铁律 5）**：本段原来写「复用那一支……`Open()` 重建内容」——
        /// **A217② 之后不再成立**：`GameWindow.TryOpen` 已照原版按 `CurrentState` 分三档，
        /// **还开着那一档什么都不做**（`Open` 支早退）⇒ 复用**不重建内容**。
        /// 这一点**与判据一致**：原版 `automaticallyLoadedWindows` 命中后调的同样是 `TryOpen`，
        /// 而它那一档（`state == Open`）在原版里**一个字段都不写就 return**（判据 → `TryOpen` 那段）。
        /// ⚠️ **由此产生的行为差**（⛔ 别当成缺陷、也别当成已收口）：这 8 条入口的窗
        /// （`InboxWindow` / `SettingsWindow` / `SocialWindow` / `ChatPanel` / `LeaderboardWindow` /
        /// `PlayerProfileWindow` / `RewardsWindow` / `CollectionWindow` / `ShopWindow`）如果**内容需要
        /// 每次重进都刷新**，那它们得像原版那 7 个 `TryOpen` 覆写一样**自己补一条刷新**（本轮没补：
        /// 「原版那 9 条入口各自刷不刷」是**另一半判据、还没查**，见报告与 `资料/待办判据_1012` 那条）。
        /// 今天**唯一的必改点已经改掉**：`CardDetailPopup.ShowCard` 改为**显式 `Build()`**（它靠重建换卡）。</para></summary>
        /// <param name="prefabRef">prefab 根名（见上面那几个 `PrefabRef*`）。看下面「键为什么可能带后缀」那条。</param>
        /// <param name="closeAll">照该入口原版 `OpenWindowButton.closeOtherMenus` 传
        /// （`OpenSocial` / `OpenRewards` / `OpenCollection` / `OpenShop` 四条是 1 ⇒ `true`）。
        /// ⚠️ 这一支会先 `CloseAllWindows()`：若复用的那扇自己也在开着，它会先被关掉再立刻重开
        /// （**同一个实例**）—— 关这一下把 `CurrentState` 写成了 `Closed` ⇒ 重开走 `TryOpen` 的
        /// **重建支**（内容按当前状态重铺、`Open()` 的那些「重置成当前值」照原样发生）⇒ 结束时仍只有一扇，
        /// 结果与原版「复用后照样重走一遍开窗流程」一致。</param>
        public static T OpenByRef<T>(string prefabRef, System.Func<WindowsManager, T> create, bool closeAll = false)
            where T : GameWindow
        {
            var wm = EnsureHost();
            T win = null;
            GameWindow cached;
            if (_openByRef.TryGetValue(prefabRef, out cached))
            {
                if (StillOpen(wm, cached))
                {
                    // = 原版 `TryGetValue` 命中那一跳（A104 §二 第 2 跳）
                    win = cached as T;
                    Debug.Log($"[Win] `{prefabRef}` 已经开着了 ⇒ **复用同一扇**（照原版 `automaticallyLoadedWindows` 命中复用），不新建");
                }
                else
                {
                    // = 原版 `CloseWindowCO` 把缓存条目删掉那一跳（我们惰性做，见上面那段）
                    _openByRef.Remove(prefabRef);
                }
            }
            if (win == null)
            {
                win = create(wm);
                _openByRef[prefabRef] = win;
                Debug.Log($"[Win] `{prefabRef}` 不在（没建过 / 已经关掉）⇒ **新建一扇**（照原版 `Instantiate` 那一支）");
            }
            wm.OpenWindow(win, null, closeAll);   // 复用那一支照原版**照样再走一遍**开窗流程（A104 §二 第 5 跳）
                                                  // ⚠️ 但内容**重建与否按 `CurrentState` 走**（A217②：还开着 ⇒ 不重建，照原版）
            return win;
        }

        /// <summary>「这扇窗还开着吗」—— 判据只有 `WindowsManager.openWindows` 一处
        /// （`GameWindow.Close()` → `NotifyClosed` 会把它从那里摘掉）。
        /// ⚠️ 先过一遍 Unity 的 `== null`：场景卸载 / 对象被销毁时那是**假 null**（那种也要从缓存里剔掉）。</summary>
        public static bool StillOpen(WindowsManager wm, GameWindow win)
        {
            if (win == null) return false;
            if (wm == null) return false;
            return wm.openWindows.Contains(win);
        }

        /// <summary>自检用：清空那一份「按引用复用」的缓存（别让它跨段影响后面的断言）。</summary>
        public static void ClearReuseCacheForTest() { _openByRef.Clear(); }

        /// <summary>🆕 **A251-L1**：开一扇 `BaseOfferPopup`（原版那一族 **21 个 prefab** ——
        /// `Base Offer Popup` + 20 个 `General Basic Offer Popup …` 变体）。
        /// 走 <see cref="OpenByRef"/>（= 原版 `automaticallyLoadedWindows` 命中复用那一套），
        /// 键 = <see cref="PrefabRefBaseOfferPopup"/>（**母版名**，见那边的注释）。
        ///
        /// <para><paramref name="variant"/> = 变体 prefab 根名（逐字，如
        /// `"General Basic Offer Popup Variant Premium_Resource"`）；`null` 或空 ⇒ 用**母版**。
        /// 名字不在 `BaseOfferPopup.Variants` 里 ⇒ `Create` 会**出声并退回母版**（那份表 = 21 条，
        /// 来源 = `menu_dump` 逐份现读，见 `Shell/BaseOfferPopup.cs` 文件头）。</para>
        ///
        /// <para><paramref name="content"/> = 报价内容（原版由服务端给，本地没有）。`null` ⇒ 用
        /// **出厂值**（`BaseOfferPopup.DefContent()`）—— 那一档走的是**原版自己的 no-data 分支**：
        /// `Timer` / `Offer Badge` / `Available Counter` 三件**照原版关掉**，其余用 prefab 出厂文本
        /// （判据逐条 → `Shell/BaseOfferPopup.cs` 文件头那张表）。</para>
        ///
        /// <para>⚠️ **本方法不会自己造入口**：原版开它的是商店/容器那条服务端链
        /// （`ContainerService.OpenOfferContainer` 那一族），我们**没有那个数据源** ⇒
        /// 这里只提供「怎么开」，**调用方自己决定挂在哪**（与 `OfferContainer` 那条口径一致）。</para></summary>
        public static BaseOfferPopup OpenBaseOfferPopup(string variant = null,
                                                        OfferContainer.Content? content = null)
        {
            var win = OpenByRef(PrefabRefBaseOfferPopup,
                                wm => BaseOfferPopup.Create(wm, variant, content));
            // 🔴 复用那一支**也要**把变体/内容喂进去：`TryOpen` 在 `CurrentState == Open` 时**早退**
            //    （照原版，A217②）⇒ 光靠 `Open()` 重建不了内容 ⇒ 显式调 `Show`。
            //    ⚠️ 但 `Show` **自己会判「变体与内容都没变 ⇒ 不重建」**（照原版 `TryOpen` 的 `Open` 支）
            //    ⇒ 新建那一支这里等于空转，**不会多建一遍**（原来那版会：`Create` 建母版 + `Show` 重建变体）。
            win.Show(variant, content);
            return win;
        }

        // ---------------------------------------------------------- 🆕 A251-L2/L4：四扇小窗
        //
        // 🔴 **四扇都走 `OpenByRef`**（= 原版 `automaticallyLoadedWindows` 命中就复用那一套），
        //    键 = 上面那四条 `PrefabRef*`（各自 = 自己 `Create()` 里 `new GameObject(...)` 用的名字，两处同源）。
        // ⚠️ **复用那一支也会把新参数喂进去**：`GameWindow.TryOpen` 在 `CurrentState == Open` 时**早退**
        //    （照原版，A217②）⇒ 光靠 `Open()` 刷不了内容 ⇒ 显式调各自的 `Show(...)` / `OpenEx(...)`。
        //    ⚠️ 新建那一支这里会**多建一遍**（`Create` 的 `Open()` 建一次、`Show` 再建一次）——
        //    四扇的树都很小（8 / 22 / 34 / 17 个节点），且这四扇**没有** `BaseOfferPopup` 那种
        //    「变体与内容都没变 ⇒ 不重建」的守卫（它们的参数是**调用方每次现给**的，判「没变」要逐格比数组）
        //    ⇒ **如实记**这一处多余的重建，⛔ 不假装它不存在。
        // 🔴 **2026-10-13（A1718）更正（铁律 5）**：本行原文写「⚠️ **四扇都没有本地入口**（全量反编译里各自
        //    只命中自己的那几个 `.c`）」—— **对 `GenericOptionsPanel` 不成立**：它的本地入口一直在 =
        //    `Shell/DeckInfoPopup.cs` 的 `ShareOnChat()`（`Share On Chat` 那颗钮 ⇒ `OpenGenericOptionsPanel`；
        //    原版那一跳 = `d:/2/tools/decomp_full/DeckInfoPopup__ShareDeckOnChat.c`）。**错因**（按【文件名】grep，
        //    而那个调用方的文件名里没有这个词）与判据 → 上面 `PrefabRefGenericOptionsPanel` 那条更正，⛔ 不抄第二份。
        //    **另三扇**（`AllianceMemberOptions` / `PurchasePremium` / `RankedRewardEvent`）**仍然没有游戏内入口**
        //    （只有自检宿主 `Editor/ShopScene.cs` 在开它）⇒ 那三扇只提供「怎么开」。

        /// <summary>🆕 **A251-L2**：开一扇 <see cref="GenericOptionsPanel"/>（原版 `GenericOptionsPanel`）。
        /// <paramref name="ctx"/> = `Context`（标题 + 一列按钮；`null` ⇒ 出厂值）；<paramref name="anchor"/> =
        /// 画布像素点（`null` ⇒ 用 prefab 自带的 `anchoredPosition` 那一档）。</summary>
        public static GenericOptionsPanel OpenGenericOptionsPanel(GenericOptionsPanel.Context? ctx = null,
                                                                  Vector2? anchor = null)
        {
            var win = OpenByRef(PrefabRefGenericOptionsPanel,
                                wm => GenericOptionsPanel.Create(wm, ctx, anchor));
            win.Show(ctx, anchor);
            return win;
        }

        /// <summary>🆕 **2026-10-17（界面线 T18/T25）**：开一扇 <see cref="ItemDisplayWindow"/>（原版 `ItemDisplayWindow`）。
        /// <para><paramref name="ctx"/> = 已解析好的内容（名 / 描述 / 类型 / 阵营 / lore / 图）；
        /// 卡背那一档的建法 = <see cref="ItemDisplayWindow.ForCardback"/>（数据全在本地）。</para>
        /// <para>⚠️ **新建那一支会多建一遍**（`Create` 的 `Open()` 建一次、`Show` 再建一次）—— 与本文件
        /// `OpenGenericOptionsPanel` / A251 那四扇**同一个形状**（`GameWindow.TryOpen` 在 `CurrentState == Open`
        /// 时照原版早退 ⇒ 复用那一支光靠 `Open()` 刷不了内容，必须显式 `Show`）。</para></summary>
        public static ItemDisplayWindow OpenItemDisplay(ItemDisplayWindow.Context? ctx = null)
        {
            var win = OpenByRef(PrefabRefItemDisplay,
                                wm => ItemDisplayWindow.Create(wm, ctx));
            win.Show(ctx);
            return win;
        }

        /// <summary>🆕 **2026-10-17（界面线 T18/T27）**：开一扇 <see cref="WhereToGetCardsPopup"/>
        /// （原版 `WhereToGetCardsWindow` · prefab `WhereToGetCardsPopup` —— 卡牌来源窗）。
        /// <para><paramref name="ctx"/> = 大标题词条键 + 来源段列表；`null` ⇒ **出厂那一份（段列表空）**——
        /// 本机**没有来源数据**（`CardsSpecialConfig` 全 `assets_full` 0 命中）⇒ 那只会在「0 段 + 出声」那一档
        /// （判据 → `Shell/WhereToGetCardsPopup.cs` 文件头）。</para>
        /// <para>⚠️ **本地没有生产入口**（原版由事件页两个 tab 的 `getCardsWindow` AssetReference 开；
        /// 承载窗 `Parent Event Window` 已于 **T28（2026-10-17）** 建出（见 <see cref="OpenParentEvent"/>），
        /// 但两个事件 tab 本机**没有 liveops 数据** ⇒ 那条入口仍不接线）⇒ 只提供「怎么开」，⛔ 不伪造调用点。
        /// ⚠️ 新建那一支会**多建一遍**（`Create` 的 `Open()` 建一次、`Show` 再建一次）—— 与同文件
        /// `OpenItemDisplay` 同一个形状（`TryOpen` 在 `CurrentState == Open` 时照原版早退）。</para></summary>
        public static WhereToGetCardsPopup OpenWhereToGetCards(WhereToGetCardsPopup.Context? ctx = null)
        {
            var win = OpenByRef(PrefabRefWhereToGetCards,
                                wm => WhereToGetCardsPopup.Create(wm, ctx));
            win.Show(ctx);
            return win;
        }

        /// <summary>🆕 **2026-10-17（界面线 T28）**：开一扇 <see cref="ParentEventWindow"/>
        /// （活动承载窗 · 原版 `BaseParentEventWindow` 那一族 / prefab 根名 `Parent Event Window`）。
        /// <para><paramref name="page"/> = 开在哪一页（默认 `ChooseSide` —— 原版 `CheckAndOpenTab` 按
        /// 「玩家是否已选阵营」决定，那份数据在远端 LiveOps ⇒ 本地只能挑一个并出声；
        /// 判据 → `Shell/ParentEventWindow.cs` 文件头 ④）。</para>
        /// <para>✅ **2026-10-23 起有生产入口了**：左竖导航第 6 颗 `Event Navigation Button` → `OnNavClick(5)`
        /// → <see cref="OpenEventHub"/>（默认 `Parent`）→ 本方法；另外三扇活动窗左栏的「事件钮」也走它。
        /// ⚠️ 活动数据全在远端 ⇒ 本窗照 prefab 出厂原文画 + 出声一次
        /// （数据缺口清单 → 同文件「数据缺口」那一节）。</para></summary>
        public static ParentEventWindow OpenParentEvent(ParentEventWindow.Page page = ParentEventWindow.Page.ChooseSide)
        {
            return OpenByRef(PrefabRefParentEvent,
                             wm => ParentEventWindow.Create(wm, page));
        }

        /// <summary>🆕 **2026-10-19（活动线 L01）**：开一扇 <see cref="GachaEventWindow"/>
        /// （Gacha 抽卡活动页 · 原版 `GachaEventTab`）。
        /// <para>⚠️ **原版没有独立窗口**：Gacha 是 `BaseParentEventWindow` 里由 liveops 事件列表动态建的
        /// 一个 tab（判据 → <see cref="PrefabRefGachaEvent"/> 的注释与 `Shell/GachaEventWindow.cs` 文件头）。
        /// 本地没有 liveops 事件列表 ⇒ 本方法开的是「**承载窗外壳 + 恰好一个 Gacha 页**」。</para>
        /// <para>✅ **2026-10-23 起有生产入口了**：① 左竖导航第 6 颗 `Event Navigation Button` →
        /// <see cref="OpenEventHub"/>；② **任何一扇活动窗左栏的「事件钮」**（`ParentEventWindow.SetupEventTabs`
        /// 建的四扇窗共用那一份）→ 同一个 `OpenEventHub`。活动数据全在远端 LiveOps
        /// ⇒ 页面建满、数值 `--` + 出声（⛔ 不编；数据缺口清单 → `Shell/GachaEventWindow.cs` 文件头）。</para></summary>
        public static GachaEventWindow OpenGachaEvent()
        {
            return OpenByRef(PrefabRefGachaEvent,
                             wm => GachaEventWindow.Create(wm));
        }

        /// <summary>🆕 **2026-10-19（活动线 L02）**：开一扇 <see cref="RaidEventWindow"/>
        /// （Raid 团队活动页 · 原版 `RaidEventTab`）。
        /// <para>⚠️ **原版没有独立窗口**：Raid 是 `BaseParentEventWindow` 里由 liveops 事件列表动态建的
        /// 一个 tab（判据 → <see cref="PrefabRefRaidEvent"/> 的注释与 `Shell/RaidEventWindow.cs` 文件头）。
        /// 本地没有 liveops 事件列表 ⇒ 本方法开的是「**承载窗外壳 + 恰好一个 Raid 页**」。</para>
        /// <para>✅ **2026-10-23 起有生产入口了**：① 左竖导航第 6 颗 `Event Navigation Button` →
        /// <see cref="OpenEventHub"/>；② **任何一扇活动窗左栏的「事件钮」**（`ParentEventWindow.SetupEventTabs`
        /// 建的四扇窗共用那一份）→ 同一个 `OpenEventHub`。活动数据全在远端 LiveOps
        /// （里程碑 / 社区分 / 个人分 / 奖励 / 起止时刻 / 背景图 / 阵营图）
        /// ⇒ 页面建满、数值 `--` + 出声（⛔ 不编；数据缺口清单 → `Shell/RaidEventWindow.cs` 文件头）。</para></summary>
        public static RaidEventWindow OpenRaidEvent()
        {
            return OpenByRef(PrefabRefRaidEvent,
                             wm => RaidEventWindow.Create(wm));
        }

        /// <summary>🆕 **2026-10-19（活动线 L03）**：开一扇 <see cref="ExpansionPassEventWindow"/>
        /// （Expansion Pass 战令页 · 原版 `ExpansionPassEventTab`）。
        /// <para>⚠️ **原版没有独立窗口**：它是 `BaseParentEventWindow` 里由 liveops 事件列表动态建的
        /// 一个 tab（判据 → <see cref="PrefabRefExpansionPassEvent"/> 的注释与
        /// `Shell/ExpansionPassEventWindow.cs` 文件头）。本地没有 liveops 事件列表
        /// ⇒ 本方法开的是「**承载窗外壳 + Pass / Missions 两页**」。</para>
        /// <para>✅ **2026-10-23 起有生产入口了**：① 左竖导航第 6 颗 `Event Navigation Button` →
        /// <see cref="OpenEventHub"/>；② **任何一扇活动窗左栏的「事件钮」**（`ParentEventWindow.SetupEventTabs`
        /// 建的四扇窗共用那一份）→ 同一个 `OpenEventHub`。活动数据全在远端 LiveOps
        /// （里程碑奖励轨道 / 等级与分数 / `HasPremium` / 付费档报价 / 起止时刻 / 背景图 / 阵营图）
        /// ⇒ 页面建满、数值 `--` + 出声（⛔ 不编；数据缺口清单 → `Shell/ExpansionPassEventWindow.cs` 文件头）。</para></summary>
        public static ExpansionPassEventWindow OpenExpansionPassEvent()
        {
            return OpenByRef(PrefabRefExpansionPassEvent,
                             wm => ExpansionPassEventWindow.Create(wm));
        }

        /// <summary>🆕 **2026-10-23（活动线 L01/L02/L03 收尾 + 界面线 T28）**：**活动窗的唯一开窗入口**
        /// （= 原版 `OpenEventButton__OpenWindow.c` 那一跳）。
        ///
        /// <para>**原版那条链**（`d:/2/tools/decomp_full/`，方法体完整现读）：
        /// `NavigationPanelController.BuildEventButtons`（每个 live 事件 `Instantiate(eventButtonPrefab)` →
        /// `OpenEventButton.Initialize` → `SetButtonInfo`）→ 点那颗钮 = `OpenEventButton__ButtonClick` →
        /// `OpenWindow` → `WindowsManager.OpenWindow(LiveOpsAssetUtility.GetComponentReference(ev, 10), ev)`
        /// （10 = `EventComponentReferenceType.MainWindow`）；那颗钮是 toggle，关回来走
        /// `ToggleValueChange(false)` → 那扇窗的 `Close()`。</para>
        ///
        /// <para>🔴 **`ev → 哪扇窗` 的映射是核过的**（`bundle_menus_assets_all/AssetBundle/AssetBundle_1.json`
        /// 的 `m_Container`（键 = GUID）→ PathID → UnityPy 读名字）：
        /// `Parent` 事件的 `MainWindow` = `9a26697357770d8458298d89467fd459` → PathID `-3290546486736551612`
        /// = **`Parent Event Window`**；`Scoring` 事件的 `Tab` = `e6914cbdd45814f5f9bd40a854c12218` →
        /// PathID `-555883983194387681` = **`Expansion Pass Tab`**。判据全文 →
        /// `Shell/ParentEventWindow.cs` 文件头「页签与入口」那一节。</para>
        ///
        /// <para>⚠️ **本机没有 liveops 事件列表**（远端 PlayFab/CCD）⇒ 这里按 <see cref="ParentEventWindow.EventWindowKind"/>
        /// 直接开对应那扇窗（原版是「事件实例 → 它的 `MainWindow`」；我们没有实例 ⇒ **这一层映射是等价物、如实标**）。
        /// 活动数据仍全在远端：窗照样建满、数值 `--` + 出声（数据缺口 → 各窗文件头）。</para>
        ///
        /// <para>✅ **2026-10-23：最后一跳【已接】** —— 原版那颗「事件钮」在左竖导航（`BuildEventButtons`），
        /// 本仓 `Shell/MainMenuRuntime.BuildNavigationPanel` 末尾建出第 6 颗 **`Event Navigation Button`**
        /// （原版 prefab 真名，`NavigationPanelToggle.toggleType = 5`），`OnNavClick(5)` → **本方法**
        /// （`EventWindowKind.Parent`）；`SetNavToggle(Event)` 现在真的点亮它（值序与
        /// `NavigationPanelToggleType.Event = 5` 逐值相同）。⚠️ 原版是「每个 live 事件一颗钮」，
        /// 本机没有事件列表 ⇒ 本仓建**一颗**总入口钮（如实标）。</para></summary>
        public static GameWindow OpenEventHub(ParentEventWindow.EventWindowKind kind = ParentEventWindow.EventWindowKind.Parent)
        {
            GameWindow w;
            switch (kind)
            {
                case ParentEventWindow.EventWindowKind.Gacha: w = OpenGachaEvent(); break;
                case ParentEventWindow.EventWindowKind.Raid: w = OpenRaidEvent(); break;
                case ParentEventWindow.EventWindowKind.ExpansionPass: w = OpenExpansionPassEvent(); break;
                default: w = OpenParentEvent(); break;
            }
            Debug.Log("[Win] `OpenEventHub(" + kind + ")` ⇒ 开 `" + (w != null ? w.name : "(null)")
                    + "`（原版那一跳 = `OpenEventButton.OpenWindow` → `WindowsManager.OpenWindow(MainWindow)`；"
                    + "入口 = 左竖导航第 6 颗 `Event Navigation Button`，见本方法的 doc）");
            return w;
        }

        /// <summary>🆕 **2026-10-17（界面线 T18）**：开一扇 <see cref="TutorialMessagePopup"/>
        /// （原版 `TutorialMessageWindowPopUp` —— 教程消息窗 / 「点击以继续」整屏简报）。
        /// <para><paramref name="ctx"/> = 标题/正文词条键 + `Event image`（`null` ⇒ prefab 出厂那一份：
        /// `MainMenu/Welcome/{Title,Subtitle}` + 无图）。</para>
        /// <para>⚠️ **本地没有生产入口**（原版那一跳在教程 stage 系统里，我们没建）⇒ 只提供「怎么开」，
        /// ⛔ 不伪造调用点。⚠️ 新建那一支会**多建一遍**（`Create` 的 `Open()` 建一次、`Show` 再建一次）——
        /// 与同文件 `OpenItemDisplay` 同一个形状（`TryOpen` 在 `CurrentState == Open` 时照原版早退）。</para></summary>
        public static TutorialMessagePopup OpenTutorialMessage(TutorialMessagePopup.Context? ctx = null)
        {
            var win = OpenByRef(PrefabRefTutorialMessage,
                                wm => TutorialMessagePopup.Create(wm, ctx));
            win.Show(ctx);
            return win;
        }

        /// <summary>🆕 **2026-10-17（界面线 T18）**：开一扇 <see cref="WelcomePopup"/>（原版 `WelcomePopup` /
        /// prefab `WelcomeScreen Alpha`）。
        /// <para><paramref name="ctx"/> = 标题/正文词条键 + `Event image`（`null` ⇒ prefab 出厂那一份）。
        /// ⚠️ **不判显隐**：原版入口判据 `IsFirstWelcome` 在远端 PlayFab tag 里（判据 → 那个属性）。
        /// 谁要在本地开它，自己先确认该不该开。</para></summary>
        public static WelcomePopup OpenWelcome(WelcomePopup.Context? ctx = null)
        {
            var win = OpenByRef(PrefabRefWelcome,
                                wm => WelcomePopup.Create(wm, ctx));
            win.Show(ctx);
            return win;
        }

        /// <summary>🆕 **2026-10-17（界面线 T26）**：开一扇 <see cref="FTUESelectDeckWindow"/>
        /// （原版 `FTUESelectDeckWindow` / prefab `Choose Army Deck FTUE Window` —— 「SELECT ARMY」选阵营送卡组）。
        /// <para><paramref name="decks"/> = 起始卡组表（`null` ⇒ <see cref="FTUESelectDeckWindow.Starters"/>，
        /// 四副 `UM/ORK/BL/SAU_SK_Starter`，逐列判据 → 那个文件的文件头）。</para>
        /// <para>⚠️ **本地没有生产入口**（原版在 FTUE onboarding 链里，我们没建那条状态机）⇒ 只提供「怎么开」，
        /// ⛔ 不伪造调用点。⚠️ 新建那一支会**多建一遍**（`Create` 的 `Open()` 建一次、这里没有 `Show` 二次
        /// —— 本窗**没有**可换的内容参数，所以**不**走 `Show`；只有 `decks` 变了才需要重建，而那是建窗参数）。</para></summary>
        public static FTUESelectDeckWindow OpenChooseArmyDeck(FTUESelectDeckWindow.StarterDeck[] decks = null)
        {
            var win = OpenByRef(PrefabRefChooseArmyDeck,
                                wm => FTUESelectDeckWindow.Create(wm, decks));
            return win;
        }

        // ---------------------------------------------------------- 🆕 2026-10-17（界面线 T18 收尾）：排位赛季三窗 + 反馈问卷
        //
        // 🔴 **四扇都走 `OpenByRef`**（= 原版 `automaticallyLoadedWindows` 命中就复用那一套），
        //    键 = 上面那四条 `PrefabRef*`（各自 = 自己 `Create()` 里 `new GameObject(...)` 用的名字，两处同源）。
        // ⚠️ **复用那一支也要把新参数喂进去**：`GameWindow.TryOpen` 在 `CurrentState == Open` 时照原版早退
        //    ⇒ 显式调各自的 `Show(ctx)`（同 `OpenItemDisplay` / `OpenTutorialMessage` 的形状）。
        // ⚠️ **本地都没有生产入口**（判据 → 各窗文件头）⇒ 这里只提供「怎么开」，⛔ 不伪造调用点。

        /// <summary>🆕 开一扇 <see cref="RankedDivisionChangeWindow"/>（原版 `RankedEventDivisionChangeWindowNewSystem`）：
        /// 排位升段窗。`ctx` = 段位名 / 评分 / 印玺进度（**全在服务端**；`null` ⇒ 窗壳 + `--`）。</summary>
        public static RankedDivisionChangeWindow OpenRankedDivisionChange(RankedDivisionChangeWindow.Context? ctx = null)
        {
            var win = OpenByRef(PrefabRefRankedDivisionChange, wm => RankedDivisionChangeWindow.Create(wm, ctx));
            win.Show(ctx);
            return win;
        }

        /// <summary>🆕 开一扇 <see cref="RankedNewSeasonWindow"/>（原版 `RankedEventStartSeasonWindow`）：
        /// 新赛季窗。`ctx.EndUtc` = 赛季结束时刻（**服务端数据**；`null` ⇒ 显示 `--`）。</summary>
        public static RankedNewSeasonWindow OpenRankedNewSeason(RankedNewSeasonWindow.Context? ctx = null)
        {
            var win = OpenByRef(PrefabRefRankedNewSeason, wm => RankedNewSeasonWindow.Create(wm, ctx));
            win.Show(ctx);
            return win;
        }

        /// <summary>🆕 开一扇 <see cref="RankedSeasonEndedWindow"/>（原版 `RankedEventSeasonEndedWindow`）：
        /// 赛季结束窗。🔴 它的窗参是 **`type = 0`（Fullscreen）** —— 别照另两扇抄。`ctx` 全在服务端（`null` ⇒ `--`）。</summary>
        public static RankedSeasonEndedWindow OpenRankedSeasonEnded(RankedSeasonEndedWindow.Context? ctx = null)
        {
            var win = OpenByRef(PrefabRefRankedSeasonEnded, wm => RankedSeasonEndedWindow.Create(wm, ctx));
            win.Show(ctx);
            return win;
        }

        /// <summary>🆕 开一扇 <see cref="GiveFeedbackPopup"/>（原版 `GiveFeedbackMenuDemo`）：反馈问卷整窗。
        /// <para>⚠️ **原版这个 build 里没有活入口**（主菜单 `Feedback Button` 出厂 `m_IsActive = false`、
        /// `feedbackMenu` 字段全库 0 命中）⇒ 只提供「怎么开」。</para>
        /// <para>⚠️ 新建那一支会**多建一遍**（`Create` 的 `Open()` 建一次、`Show` 再建一次）—— 同
        /// `OpenItemDisplay` 那条口径；本窗树很大（问卷 16 题），但只在「复用已开的窗」时才多这一遍。</para></summary>
        public static GiveFeedbackPopup OpenGiveFeedback(GiveFeedbackPopup.Context? ctx = null)
        {
            var win = OpenByRef(PrefabRefGiveFeedback, wm => GiveFeedbackPopup.Create(wm, ctx));
            win.Show(ctx);
            return win;
        }

        /// <summary>🆕 **A251-L2**：开一扇 <see cref="AllianceMemberOptionsPopup"/>（原版 `AllianceMemberOptionsPopup`）。
        /// <paramref name="view"/> = `null` ⇒ **没数据**那一档（八颗钮照 prefab 出厂态摆，
        /// 只有 `Debug Add Skulls` 关着 —— `Awake()` 那条是无条件的）。</summary>
        public static AllianceMemberOptionsPopup OpenAllianceMemberOptions(AllianceMemberOptionsPopup.MemberView? view = null)
        {
            var win = OpenByRef(PrefabRefAllianceMemberOptions,
                                wm => AllianceMemberOptionsPopup.Create(wm, view));
            if (view.HasValue) win.SetMember(view.Value);
            return win;
        }

        /// <summary>🆕 **A251-L2**：开一扇 <see cref="PurchasePremiumWindow"/>（原版 `PurchasePremiumWindow`）。
        /// 🔴 它是「**带参数的窗口**」那一族（`TryOpen(data, options)` 覆写，全库 7 个；判据 = 本文件上面
        /// `TryOpen` 那一段的注释）⇒ 参数走 <paramref name="army"/>（= 原版那个 `CardArmy?`，
        /// **`null` ⇒ `Ultramarines(10)`**，逐字照 `PurchasePremiumWindow__TryOpen.c`）。
        /// <paramref name="offers"/> = `null` ⇒ **空列表**（本地恒如此：`premiumStoreReference` 是空引用、
        /// 数据在远端 LiveOps）。</summary>
        public static PurchasePremiumWindow OpenPurchasePremiumWindow(int? army = null,
                                                                      PurchasePremiumWindow.ArmyOffer[] offers = null)
        {
            var win = OpenByRef(PrefabRefPurchasePremium,
                                wm => PurchasePremiumWindow.Create(wm, offers, army));
            win.OpenEx(army, offers);      // 复用那一支也要把新参数喂进去（见上面那一段）
            return win;
        }

        /// <summary>🆕 **A251-L4**：开一扇 <see cref="RankedRewardEventWindow"/>（原版 `RankedRewardEventWindow`；
        /// ⛔ **别与 `RankedEventWindow`（`RankedEventWindowV2`）混** —— 那是另一扇）。
        /// <paramref name="view"/> = `null` ⇒ **没数据**那一档（走的就是原版自己的兜底分支：
        /// `Title`/`Description` 关着、`Bonus points text` 空串、`Timer` 关着、0 张阵营卡）。</summary>
        public static RankedRewardEventWindow OpenRankedRewardEvent(RankedRewardEventWindow.BoostView? view = null)
        {
            var win = OpenByRef(PrefabRefRankedRewardEvent,
                                wm => RankedRewardEventWindow.Create(wm, view));
            if (view.HasValue) win.SetBoost(view.Value);
            return win;
        }

        // ---------------------------------------------------------- 🆕 A251-L3：推荐人窗
        //
        // 🔴 **它不在 `menus` 包里** —— 原版 prefab 在 `bundle_generalgamewindows_assets_all`
        //    （A251 那七扇里只有它与已裁掉不建的 `Debug Reactivate Event Window` 在这个包）。
        // ⚠️ 同上面四扇：走 `OpenByRef`（= 原版 `automaticallyLoadedWindows` 命中复用），
        //    且**复用那一支也要把新参数喂进去**（`GameWindow.TryOpen` 在 `CurrentState == Open` 时照原版早退）。
        // ⚠️ 新建那一支会**多建一遍**（`Create` 的 `Open()` 建一次、`Show` 再建一次）—— 同上面四扇那条口径。
        // ⚠️ **本地没有任何调用方**（原版由 `ReferralContainer.OpenPopup()` 那条 LiveOps 容器链开，
        //    那个容器我们没建）—— 这里只提供「怎么开」。

        /// <summary>🆕 **A251-L3**：开一扇 <see cref="ReferralPopupWindow"/>（原版 `ReferralPopupWindow`）。
        /// <paramref name="view"/> = `null` ⇒ **原版自己的 no-data 那一档**（`GetReferrer() == null`）：
        /// `Input View` 开着、`Referred View` 关着、计数串 = prefab 原文。</summary>
        public static ReferralPopupWindow OpenReferralPopup(ReferralPopupWindow.ReferralView? view = null)
        {
            var win = OpenByRef(PrefabRefReferralPopup,
                                wm => ReferralPopupWindow.Create(wm, view));
            if (view.HasValue) win.SetReferral(view.Value);
            return win;
        }

        // ---------------------------------------------------------- 🆕 A103：单机能源活动窗
        //  原版一扇，在 `bundle_menus_assets_all`（判据 = `menu_dump … --relative` 现读，**70 个节点**）。
        //  ⚠️ **A103 的另两件（`Alliance Event Score Panel` / `Alliance Event Score Info`）不在这一族** ——
        //     它们的根组件是 `MonoBehaviour`（`AlliancesEventScorePanel` / `AllianceScoreBar`）、**不是窗**
        //     ⇒ **没有注册口**（建法 = `Shell/AllianceEventScorePanel.cs` 的 `Create(parent, x1, y1)`）。
        //  ⚠️ 本窗的 `closeOnESC` 是 **0**（MB 实读）· `BackgroundCloseButton.window` 是 **null**
        //     ⇒ **点窗外与 ESC 都不关**，出口只有表头那颗返回钮（照字段办，⛔ 不做成「点窗外关窗」）。

        /// <summary>🆕 **A103**：开一扇 <see cref="EnergySinglePlayerOnlyEventWindow"/>
        /// （原版 `SinglePlayerOnlyEnergyEventWindow`）。
        /// <para>⚠️ **走 `OpenByRef`**（原版 `automaticallyLoadedWindows` 命中复用）；
        /// 新建那一支会**多建一遍**（`Create` 的 `Open()` 建一次、`Show` 再建一次）——
        /// 与本批 A251 那四扇同一个形状（那半边如实记在 `资料/普查产出_1013/WA103_三扇活动窗.md` §五）。</para>
        /// <para>⚠️ 原版的入口在 LiveOps 事件链上（`SinglePlayerScoringEvent` 是**远端**数据）——
        /// 本地没有事件 ⇒ **入口由调用方自己定**（同 `OpenRankedRewardEvent` / `OpenReferralPopup` 的处置）。</para></summary>
        public static EnergySinglePlayerOnlyEventWindow OpenEnergyEvent()
        {
            var win = OpenByRef(PrefabRefEnergyEvent,
                                wm => EnergySinglePlayerOnlyEventWindow.Create(wm));
            return win;
        }

        // ---------------------------------------------------------- 弹窗

        /// <summary>
        /// 通用弹窗（原版 `ShowPopUp(text, localizeTexts, closeOnEsc, …)` 三个重载）。
        /// 🔴 **2026-10-12（A416）已收编**：宿主 = <see cref="PopUpGameWindow"/>（原版那两扇
        /// `popupWindowOneButton` / `popupWindowTwoButtons` = `MessagePopupWindow` /
        /// `MessagePopupWindow2Buttons`）⇒ 本方法**转调** <see cref="ShowMessagePopUp"/>，
        /// 与反编译那条链同形（`ShowPopUp` → `_LoadPopUpAndShow` → `ConfigurePopUp` → `OpenWindow`）。
        ///
        /// <para>🔴 **2026-10-12（A416）就地订正（铁律 5）**：这里原来写着「原版那两个
        /// `popupWindowOneButton/TwoButtons` **仍然本地没有**（正本 §五 第 2 条）」—— **那条前提是错的**：
        /// 两扇 prefab 就在 `bundle_generalgamewindows_assets_all`（`MessagePopupWindow` /
        /// `MessagePopupWindow2Buttons`，类 **`PopUpGameWindow`**，全库只有这 2 个实例），
        /// 本类下面的 <see cref="ShowMessagePopUp"/> 就是照它们接的线（2026-10-12 A364 建的）。
        /// **错因** = 当年按**猜出来的字段名**（`popupWindowOneButton`）去搜资源、搜不到就记成了「本地没有」，
        /// 而 prefab 的真名是那两个 `MessageXxx`（同族教训 → `CLAUDE.md` 铁律 4 的 2026-10-04 更正）。
        /// 「`GenericPromptWindow` 本地有」只解释了**当年为什么挑了它**，**不解释「原版是哪一扇」**
        /// （铁律 5·c：一个值 ≠ 全部情况）。⛔ **别再把 `PromptPopup` 当成 `ShowPopUp` 的宿主** ——
        /// 它自己是另一扇窗，另有**直建**它的站点（`Shell/MainMenuRuntime.cs` 的 `EscPressed` 那扇「退出游戏」弹窗等）。</para>
        ///
        /// <para>🔴 **为什么这里还要包一层 `Close()`**：原版那扇窗**自己不关**（`PopUpGameWindow` 的两颗钮
        /// 只是调调用方给的委托 —— `PopUpGameWindow__PopUpWindowButtonPressed` 就是调
        /// `GameWindowButton.OnPress`；`DeckEditingWindow.TrySaveDeck` 那两个 lambda 各自 `HidePopUp()`）。
        /// 而本方法的 **11 个生产调用点**是按**旧宿主**的行为写的（`PromptPopup.Choose` = **先 `Close()` 再回调**）
        /// ⇒ 这里照旧包一层、**逐字保持那个顺序**（先关窗、再回调 —— 回调里可能再开一扇窗）；
        /// ⛔ 不包的话它们点完钮窗还挂着（`Editor/ShellScene.cs` 那条「弹窗关掉之后只剩 1 个窗」会红）。
        /// ⚠️ **第二颗钮只要给了 `cancelText` 就一定要挂回调**：那颗钮的 `onClick` 若为 null，
        /// 点了**什么都不发生**（窗关不掉）—— 与旧宿主「点 Cancel 就关」不等价。</para>
        ///
        /// <para>⚠️ **调用点传的都是【明文】**（不是 I2 术语键）：`PopUpGameWindow.Term()` 查不到就原样返回
        /// ⇒ 收编**一行调用点都不用改**；将来往 `Terms` 里填真词条表也不会误伤（明文永远不是键）。</para>
        /// </summary>
        public void ShowPopUp(string text, string okText = null, System.Action onOk = null,
                              string cancelText = null, System.Action onCancel = null)
        {
            PopUpGameWindow win = null;
            win = ShowMessagePopUp(text, okText,
                                   () => { win.Close(); if (onOk != null) onOk(); },
                                   cancelText,
                                   cancelText == null ? null
                                       : (System.Action)(() => { win.Close(); if (onCancel != null) onCancel(); }));
        }

        /// <summary>🆕 **2026-10-12（A364）**：原版那一族 `ShowPopUp(text, localizeTexts, closeOnEsc, …)`
        /// 的**真身** —— 宿主是 <see cref="PopUpGameWindow"/>（`popupWindowOneButton` / `popupWindowTwoButtons`
        /// 那两扇 prefab）。🔴 **2026-10-12（A416）起，上面那个 `ShowPopUp` 也转调本方法** ⇒ 全壳只有**这一条**
        /// 消息弹窗的路（那一条不再建 `PromptPopup`；两个方法**并存但不再分叉**）。
        ///
        /// <para>🔴 **它一度与上面那条并存**：上面那一条是 2026-09-23 在「原版那两个 popup prefab
        /// **本地没有**」这个前提下建的，宿主换成了同族的 `GenericPromptWindow`；**2026-10-12 现核推翻了那个前提**
        /// （`bundle_generalgamewindows_assets_all` 里 `MessagePopupWindow{,_2Buttons}` 参数齐全，见
        /// `资料/普查产出_1012/S3_卡组收藏_开账现核.md` §二·2）⇒ A416 把 `ShowPopUp` 收编到本方法。</para>
        ///
        /// <para>⚠️ **2026-10-12 订正一处数字（铁律 5）**：原文写「上面那条的 **8 个**既有调用点」——
        /// 实际是 **11 个生产调用点**（`ShopWindow:730` · `SettingsWindow:1629` · `RankedEventWindow:181` ·
        /// `PracticeModePopup:1476` · `LiveOpsEventWindow:840,847` · `LeaderboardWindow:608` ·
        /// `DeckInfoPopup:1166,1243,1326` · `NetRuntime:105`）+ **4 个自检站点**。
        /// 它们**各自传的都是【明文】**（不是术语键）⇒ 收编时**这些调用点一行都不用改**
        /// （`PopUpGameWindow.Term()` 查不到就原样返回；明文永远不是键 ⇒ 将来往 `Terms` 里填表也不会误伤）。
        /// 🔴 **2026-10-12（A483）就地订正两处（铁律 5）**：① 自检站点的数原写「2 个」，**实际是 4 个**
        /// —— `Editor/ShellScene.cs` 里 `ShowPopUp` 的自检站点共 **4 处**（本件现读 `CardPresentation/Editor/ShellScene.cs` · `CardPresentation/Editor/ShellScene.cs` ·
        /// `:918` · `:930`，⚠️ 行号会漂 ⇒ **按 `grep -n '\.ShowPopUp(' Editor/ShellScene.cs` 数，数是 4**）。
        /// **错因** = 「2」是 **A416 收编【前】** 的数（`git show HEAD:…/Editor/ShellScene.cs` 那一版只有
        /// `:808` · `:829` 两处）；A416（同一天）为「两钮」与「没给 `onCancel`」两档各补了一处 ⇒ 变成 4。
        /// ② 清单里那几处调用点的**行号已漂**（行号是快照、会随别的改动平移）：
        /// `DeckInfoPopup:1142,1219,1302` → **`:1166,1243,1326`** · `LiveOpsEventWindow:828,835` → **`:840,847`**
        /// （本件按**现读**逐个 `grep` 核对，其余 7 处未动）。⚠️ **「11 + 4」这个数是本件当时的读数** ——
        /// 谁再动了调用点，**按 `grep` 重数**，别照抄这里。
        /// ⚠️ 那 11 处里**只有 `ShopWindow:730` 一处分叉**（`okText` + `cancelText` = 2 按钮版），
        /// 其余 10 处都是单钮那一档 ⇒ 两版 prefab 都有生产消费者。</para>
        /// <para>⚠️ **`DeckInfoPopup` 那 3 处按【现读】是 `CardPresentation/Shell/DeckInfoPopup.cs` / `CardPresentation/Shell/DeckInfoPopup.cs` / `CardPresentation/Shell/DeckInfoPopup.cs`**（上一条已订正；
        /// 同族的另两份记录也各漂过一版：`资料/普查产出_1012/H5_DeckScene红与ShowPopUp收编.md:139,248`
        /// 与 `H17_ShowPopUp收编.md:145,149` 里的旧行号**是历史快照、本件不改**，只在此说明「以 `grep` 为准」）。</para>
        ///
        /// <para>🔴 **2026-10-24（`A11` · 铁律 5 就地订正）：原来这里写的是「复用同一扇（照原版）」，【那是错的】。**
        /// 原文的理由是「`_LoadPopUpAndShow` 的 `MoveNext` 第 2 段 = `ConfigurePopUp(*(this+0x60))` +
        /// `OpenWindow(*(this+0x60))` ⇒ 第二次是重配同一实例」—— **漏读了它前面那一整段**：
        /// `WindowsManager._LoadPopup_d__55__MoveNext.c:20-45`（亲读）逐句是
        /// **`if (popUpWindow != null) { CloseWindow(this, popUpWindow, false); Destroy(popUpWindow.gameObject); }`
        /// → 按 `twoButtons` 挑 prefab `Instantiate` → 赋回 `*(this+0x60)`**。
        /// 配套：`WindowsManager._LoadPopUpAndShow_d__59__MoveNext.c` 的 `bVar4 = 1 &lt; buttons.Count` 就是那个挑法。
        /// ⇒ **原版每次都销毁旧的、按按钮数重建**；`ConfigurePopUp` 里「超出按钮数的 `SetActive(false)`」
        /// 在**原版**是一条**防御性死路**（prefab 的按钮数本来就够）。
        /// 改前的「复用 + `RefreshLiveContent`」**不是原版**，而且有一档会**丢钮**：场上开着一扇
        /// **1 按钮版**实例、又要求两颗钮时，那颗 OK **画不出来**（A6 在调用点做过局部兜底并出声）；
        /// 现在照原版销毁重建 ⇒ **从根上没了**。`RefreshLiveContent` 仍留着（原版 `ConfigurePopUp`
        /// 确实会写活组件，只是它写的那一扇**永远是刚建出来的** ⇒ 落在 `Build()` 那一支）。
        /// 错因 = **只读了 `MoveNext` 的后半段**（`ConfigurePopUp` 那一跳），没读它开头那两句销毁。</para>
        ///
        /// <para>⚠️ 如实标注：我们**没有**原版那个 `popUpWindowOneButton`/`TwoButtons` 的
        /// `automaticallyLoadedWindows` 缓存（原版按 **prefab 引用**缓存）—— 现在**也不需要**它：
        /// 原版这条消息弹窗的路**根本不走复用**（每次 Instantiate）。
        /// 同一时刻只可能有一扇消息弹窗在场（旧的那扇在新建**之前**就被销毁了）。</para></summary>
        /// <param name="messageKey">**I2 术语键**（⛔ 不是明文 —— 见 `PopUpGameWindow` 文件头 ③）。</param>
        /// <param name="primaryKey">主按钮的术语键。原版 `ShowPopUp(…, primaryButton, secondButton)` 里
        /// primary 落在 **`LiveButtons[0]` = `ButtonLeft`（左）**。</param>
        /// <param name="closeOnEsc">🆕 **2026-10-25**：= 原版 `ShowPopUp(text, localizeTexts, **closeOnEsc**, …)` 的
        /// 第 3 个形参（`WindowsManager.ShowPopUp*` 三个重载**都是这个位次**，见签名桩
        /// `d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs:327/331/335`）。
        /// 原版运行时**由调用方给**（经 `LoadPopUpAndShow` → `ConfigurePopUp` 写进窗字段）。
        /// ⛔ **默认 `false` = 保持现状**：两扇 prefab 自己的序列化值是 `0`，而**每个原版调用点传什么要逐条核**
        /// （反例：`DeckEditingWindow.TrySaveDeck` 传 **0**、`BattleLogItem.ClickReplayButton` 的失败弹窗传 **1**）。</param>
        public PopUpGameWindow ShowMessagePopUp(string messageKey, string primaryKey, System.Action onPrimary,
                                                string secondaryKey = null, System.Action onSecondary = null,
                                                bool closeOnEsc = false)
        {
            // 🔴 **2026-10-24（`A11` · 铁律 5 就地改实现）：照原版【销毁旧的、按按钮数重建】。**
            //   判据（第一权威 = 反编译方法体，亲读）→ `WindowsManager._LoadPopup_d__55__MoveNext.c:20-45`：
            //     if (op_Inequality(popUpWindow, 0)) {
            //         WindowsManager__CloseWindow(this, popUpWindow, false);      // ① 先关
            //         Object__Destroy(Component__get_gameObject(popUpWindow));    // ② 再销毁
            //     }
            //     prefab = this.<1 按钮版 / 2 按钮版>;                            // ③ 参数 = `_LoadPopup` 的第 2 参
            //     popUpWindow = Object__Instantiate(prefab, …);                  // ④ 每次新建
            //   ⇒ 「复用 + `RefreshLiveContent`」（A416/A454）**不是原版**，而且有一档会丢钮：
            //   场上是 1 按钮版实例、这次要两颗 ⇒ OK 那颗画不出来（A6 在调用点做过局部兜底，留着当第二道闸）。
            //
            // ⚠️ **为什么还要 `_lastMessagePopup`**：我们的 `GameWindow.Close()` 只 `SetActive(false)`、
            //   **不销毁**（原版 `CloseWindowCO` 是 `Object.Destroy`，见本文件 A123 那段），而 `NotifyClosed`
            //   会把 `popUpWindow` 这一位清掉 ⇒ **光看 `popUpWindow` 找不到那具已经关掉的对象**，
            //   每弹一次就在锚点下留一具尸体。⇒ 用本类自己记的一格「本类建的那扇」兜住
            //   （本类是全仓**唯一**调 `PopUpGameWindow.Create` 的地方，见 `grep`）。
            var old = popUpWindow as PopUpGameWindow;
            if (old == null) old = _lastMessagePopup;
            if (old != null)
            {
                bool wasOpen = StillOpen(this, old);
                if (wasOpen) old.Close();                 // = 原版 `CloseWindow(this, popUpWindow, false)`
                DestroyWindowObject(old.gameObject);      // = 原版 `Object.Destroy(Component.get_gameObject())`
                if (popUpWindow == old) popUpWindow = null;   // 兜底（`NotifyClosed` 正常已清过这一位）
                if (_lastMessagePopup == old) _lastMessagePopup = null;
                Debug.Log("[Win] 消息弹窗**已在场** ⇒ 照原版**先关旧的、销毁、再新建**"
                        + $"（旧的 `{(wasOpen ? "还开着" : "已经关掉")}` · 新建按钮数 = {(string.IsNullOrEmpty(secondaryKey) ? 1 : 2)}）");
            }
            var win = PopUpGameWindow.Create(this, messageKey, primaryKey, onPrimary, secondaryKey, onSecondary,
                                             closeOnEsc);
            _lastMessagePopup = win;
            // = 原版 `_LoadPopUpAndShow` 最后那一跳 `OpenWindow(this, *(this+0x60))`。
            // ⚠️ 现在**没有**「复用支」了 ⇒ 上一版那段「复用一扇还开着的窗时 `Open()` 早退、
            //   必须靠 `RefreshLiveContent` 刷新」的坑在**这条路上**不存在（A454 那份实现仍然留着：
            //   原版 `ConfigurePopUp` 本来就会写活组件，只是它写的那一扇永远是刚建出来的 ⇒ 走 `Build()`）。
            OpenWindow(win);
            return win;
        }

        /// <summary>销毁一扇窗的 GameObject —— **批处理（编辑器、非 Play）里 `Object.Destroy` 不生效**，
        /// 必须 `DestroyImmediate`（`CLAUDE.md`「批处理下没有帧循环」那条）。
        /// 形态与 `MainMenuSubmenuWindow.DestroySafe` 逐句相同（⛔ 不跨文件依赖：那个是窗那边的 public
        /// helper，本类是共用件、不该被某个具体窗的 API 牵着走）。</summary>
        static void DestroyWindowObject(GameObject go)
        {
            if (go == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) { Object.DestroyImmediate(go); return; }
#endif
            Object.Destroy(go);
        }

        /// <summary>🆕 **2026-10-12（A364）**：原版 `WindowsManager.HidePopUp(bool force = false)` —— 逐句照
        /// `d:/2/tools/decomp_full/WindowsManager__HidePopUp.c`：
        /// <code>
        /// cur = currentWindow(+0x58); pop = popUpWindow(+0x60);
        /// if (cur != pop) { if (!force) return; if (pop == null) return; }
        /// CloseWindow(popUpWindow);
        /// </code>
        /// <para>🔴 **「离场/保存成功」那一支要调它**：`DeckEditingWindow__TrySaveDeck.c:103` 就是
        /// `DeckUtility.ValidateDeck` 过了之后 `WindowsManager.HidePopUp(...)`（把还开着的错误窗收掉）。</para>
        /// <para>⚠️ 我们的 `currentWindow` 与 `popUpWindow` 都照原版在 `OpenWindow` / `NotifyClosed` 里维护
        /// （见 `TopWindow` 那段），所以这一句 `cur != pop` 的语义与原版逐字对应。</para></summary>
        public void HidePopUp(bool force = false)
        {
            if (currentWindow != popUpWindow && !force) return;    // = 原版 `op_Equality(cur, pop)` 为假那一支
            if (popUpWindow == null) return;                       // = 原版第二道（只在 force 支里）
            popUpWindow.Close();                                   // = 原版 `CloseWindow(this, popUpWindow)`
        }

        public string Dump()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"锚点 {_anchors.Count} 个：");
            foreach (var kv in _anchors) sb.Append($" {kv.Key}→{kv.Value.name}");
            sb.Append($" · 开着的窗 {openWindows.Count} 个");
            if (currentWindow != null) sb.Append($" · 当前主窗 {currentWindow.name}");
            if (popUpWindow != null) sb.Append($" · 弹窗 {popUpWindow.name}");
            return sb.ToString();
        }

        // ---------------------------------------------------- 场景过渡的「临时锚点」那一对（类尾，⛔ 别往中间挪）

        /// <summary>= 原版 `TransitionUIAnchor.DisableAnchor()`（判据 = `d:/2/tools/decomp_full/TransitionUIAnchor__DisableAnchor.c`
        /// **全 118 行逐跳读完**；`out_cpp2il_dev_ilspy/TransitionUIAnchor.cs:14` 只有签名壳，⛔ 别拿它当体）。
        /// <para>**原版逐跳**（`EnableAnchor` 的反向那一半）：
        /// <list type="number">
        /// <item>`WindowsManager.UnregisterAnchorOverride()`（.c 第 46 行那一跳）；</item>
        /// <item>遍历**自己的直接子件**（`Transform.GetEnumerator`）；</item>
        /// <item>每颗 `TryGetComponent&lt;GameWindow&gt;` —— 不是窗就跳过；</item>
        /// <item>是窗 ⇒ `parentInternal = WindowsManager.Instance.GetWindowAnchor(win.placement)`（`:103-117`）；</item>
        /// <item>`localPosition = Vector3.zero`（`:126-131`）· `localRotation = identity`（`:140-145`）·
        ///   `localScale = Vector3.one`（`:148-152`）—— 三样都**复位到单位**，不是「恢复进过渡前的值」
        ///   （原版没存过旧值，它靠「过渡时只改父节点 + 单位 TRS」这一条自洽）。</item></list></para>
        /// <para>⚠️ **与 <see cref="RegisterAnchorOverride"/> 同一档**：本机**还没有场景过渡那一层**
        /// （原版那颗组件的唯一宿主是 `simpletransition` 场景）⇒ 今天没有调用方，行为零变化；
        /// 记在账里的是「方法已对齐、调用方未建」，⛔ 不是「不可达」。
        /// ⚠️ 遍历用**倒序下标**而不是原版的 `IEnumerator`：原版一边遍历一边把子件 `SetParent` 挪走，
        /// 那正是 `Transform` 枚举器最容易踩空/重复的形状；倒序下标在语义上等价且不会跳件。</para>
        /// <para>⛔ **别把「复位三样」改成「什么都不动」** —— 少了它们，过渡结束后窗会留在
        /// `EnableAnchor` 期间的缩放/偏移上（原版每个子窗都是靠这一跳回到常态的）。</para></summary>
        public static void EndTransitionAnchor(Transform anchorRoot)
        {
            UnregisterAnchorOverride();                       // 原版第一跳
            if (anchorRoot == null)
            {
                Debug.LogError("[Win] `EndTransitionAnchor(null)` —— 过渡锚点根为空，没有任何子窗会被复位" +
                               "（原版这一跳会 NRE，我们出声后照旧返回）");
                return;
            }
            for (int i = anchorRoot.childCount - 1; i >= 0; i--)
            {
                var t = anchorRoot.GetChild(i);
                var win = t.GetComponent<GameWindow>();       // 原版 `UnityEngine_Component__TryGetComponent<GameWindow>`
                if (win == null) continue;                    // 不是窗 ⇒ 原版不碰它（父级/TRS 都不动）
                Transform anchor = win.HasPlacement ? GetWindowAnchor(win.placement) : null;
                t.SetParent(anchor, false);                   // 原版 `set_parentInternal`
                t.localPosition = Vector3.zero;               // `DAT_1842da2a8` = Vector3.zero
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
            }
        }

        // ============================================================ 354 `BlockGame`（B89-E10b 追加在类尾）
        // 判据 = `decomp_full/PlayerDataManager__BlockGame.c`（全文现读 2026-10-28）三句：
        //   ① `if (loginManager(+0xa0) != null) loginManager.DuplicateConnection();`
        //   ② `WindowsManager.Instance.ShowPopUp(Loc("CustomErrors/DuplicateConnection"), …, Loc("MainMenu/General/Confirm"), 回调)`
        //      —— 两个词条实读：`0x425CEA8` = `CustomErrors/DuplicateConnection` · `0x42BE318` = `MainMenu/General/Confirm`
        //      （`il2cpp_out/stringliteral.json`，与施工单 `B88_R10` §2.2 #3 一致；**只有一颗按钮**）。
        //   ③ 那颗按钮的回调 = `UIGenericEventCatcher.Source`（`GameBlocked`）⇒ 弹窗确认后 **`Application.Quit()`**
        //      （判据 = `Net/NetBattle.cs` 的 T07 结算注：「`UIGenericEventCatcher.Source`（`GameBlocked` = 重复连接
        //      ⇒ 弹 `CustomErrors/DuplicateConnection` 后 `Application.Quit()`）」）。
        //
        // ⚠️ **第 ① 句在本仓落不了**：`LoginManager` **全仓 0 命中**（施工单 §2.2 #3 已记）——
        //   它的对外作用只有「断开那条重连会话」，我们**没有登录会话**可断 ⇒ **出声记录、不伪造一个类**。
        // ⚠️ 原版对 `WindowsManager.Instance` 为 null **不判**（`.c` 走 `FUN_1803f47a0` = 抛 NRE）；
        //   我们出声 + 返回 false（同本仓一贯口径：不许静默、也不让一次脏状态把壳炸掉）。

        /// <summary>原版 `PlayerDataManager.BlockGame()`（账 354）——「同一账号重复连接」那句拦人：
        /// 弹 `CustomErrors/DuplicateConnection`（只有一颗「确认」），点确认后退出游戏。
        /// <para>⚠️ 原版那颗按钮的文本键是 `MainMenu/General/Confirm`、**没有取消钮**（`.c` 里第二个按钮位是 0）
        /// ⇒ 这就是一扇**只能确认**的窗，⛔ 别顺手加「取消」。</para>
        /// <para>⚠️ 我们**没有生产触发源**：原版它由 Photon 房间层的「同账号重复进房」推（`UIGenericEventCatcher.GameBlocked` 事件），
        /// 本仓的 P2P <b>不是</b>房间模型 ⇒ 没有这条事件。留着是为了把方法落地（铁律 11），接线位见报告。</para></summary>
        public bool BlockGame()
        {
            Debug.LogWarning("[WindowsManager] `BlockGame()`：原版第 ① 句 `loginManager.DuplicateConnection()` "
                           + "**本仓 0 命中 `LoginManager`**（我们没有登录会话可断）⇒ 这一跳省略（⛔ 不伪造一个类）。"
                           + "第 ② 句的弹窗照做。");
            if (Instance == null)
            {
                Debug.LogError("[WindowsManager] `BlockGame()`：场上没有 `WindowsManager` 实例 ⇒ 弹窗没弹"
                             + "（⛔ 原版这里会抛 NRE）。");
                return false;
            }
            Instance.ShowMessagePopUp("CustomErrors/DuplicateConnection", "MainMenu/General/Confirm",
                                      QuitForDuplicateConnection);
            return true;
        }

        /// <summary>`BlockGame` 那颗唯一按钮的回调 = 原版 `UIGenericEventCatcher.Source` 的 `GameBlocked` 支：
        /// **直接退出**（先例 = `Core/PlayerBoot.cs` 的 `Application.Quit(0)`）。
        /// <para>⚠️ 编辑器里 `Application.Quit` 是空操作 ⇒ 出声（同 `MainMenuRuntime` 那条退出链的口径）。</para></summary>
        static void QuitForDuplicateConnection()
        {
            Debug.Log("[WindowsManager] 重复连接已确认 ⇒ 退出游戏（原版 `UIGenericEventCatcher.GameBlocked` 支）。");
            if (Application.isEditor) Debug.LogWarning("[WindowsManager] 编辑器里 `Application.Quit` 是空操作 ⇒ 真机上才退。");
            Application.Quit(0);
        }
    }

    // ============================================================ 原版 `ReferralContainer.Refresh()`（B94_S3 · 2026-10-29）
    //
    // 判据（逐句实读）：
    //   · `d:/2/tools/decomp_full/ReferralContainer__Refresh.c` **全文**：
    //     ```
    //     priceDisplayButton = this(+0x40);
    //     string s = I2_Loc.LocalizationManager.GetTranslation(<键>, fixRTL:1, applyParameters:0, …);
    //     priceDisplay(+0x30).Setup(s, 0, 0);       // 价签那一跳
    //     ```
    //   · 键名 = **字面量直读**：`il2cpp_out/stringliteral.json` 地址 `0x4276890` = `Demo/DraftMode/FreeButton`
    //     ⇒ 官方表 en `Free` / zh-CN `免费`（`数据/游戏数据/官方本地化_en+zh-CN.json`）。
    //     ⚠️ 同一个键在 `Shell/DraftModeWindow.cs` 也画过一次（草稿重抽的免费档）—— **同一个键，两处消费**。
    //   · `cpp2il/out_cpp2il_dev_ilspy/ReferralContainer.cs`（字段名 `priceDisplayButton` / `priceDisplay`）。
    //
    // ⚠️ **两处如实标**：
    //   ① 原版是 `MonoBehaviour` 上的实例方法（`+0x40` 是 `PriceDisplayButton`、`+0x30` 是它下面的 `PriceDisplay`）。
    //      我们**没有 `PriceDisplay` 类**（另一条线还挂着），`ReferralPopupWindow` 也不带价签 ⇒ 这里把
    //      「取译文 → 交给价签那一跳」做成**注入回调**，默认 `null` = 没有价签可刷（照样返回 false + 出声一次）。
    //   ② 原版价签取不到时是 throw（`FUN_1803f47a0`）；我们出声 + 返回 false（全工程口径）。
    //
    // 🔴 **入口**：原版由 `ReferralContainer.OpenPopup()` 那条 LiveOps 容器链开窗（我们没建那个容器，
    //   见 `WindowsManager.OpenReferralPopup` 的注释）⇒ 本类目前**无调用方**，由 `Editor/B93S3Assert.cs` 钉住。
    /// <summary>原版 `ReferralContainer.Refresh()` 的等价实现：把「免费」那一条的**价签文案**刷上去。</summary>
    public static class ReferralContainer
    {
        /// <summary>价签词条键（判据 `stringliteral.json` 地址 `0x4276890`）。</summary>
        public const string PriceKey = "Demo/DraftMode/FreeButton";

        static bool _said;

        /// <summary>= 原版 `I2_Loc.LocalizationManager.GetTranslation(键, …)`（走工程唯一取词口 <see cref="Loc.T"/>）。
        /// <para>⚠️ 缺键时 `Loc.T` 会**回落印键名** ⇒ 这里额外出声一次（⛔ 不静默把键名当文案）。</para></summary>
        public static string PriceText()
        {
            string t = Loc.T(PriceKey);
            if (t == PriceKey)
            {
                Debug.LogWarning("[Referral] 价签词条 `" + PriceKey + "` 在本地化表里**没有译文** ⇒ 界面上会印出键名。"
                               + "（官方表 en `Free` / zh-CN `免费`；缺 = 表没并进来）");
            }
            return t;
        }

        /// <summary>= 原版 `ReferralContainer.Refresh()`。
        /// <paramref name="priceDisplaySetup"/> = `PriceDisplay.Setup(译串, 0, 0)` 那一跳。
        /// <returns>有没有真的刷上去（false = 没有价签可刷）。</returns></summary>
        public static bool Refresh(System.Action<string> priceDisplaySetup)
        {
            string text = PriceText();
            if (priceDisplaySetup == null)
            {
                if (!_said)
                {
                    _said = true;
                    Debug.LogWarning("[Referral] `ReferralContainer.Refresh()`：价签那一跳没接"
                                   + "（原版 `priceDisplayButton.priceDisplay.Setup(译串, 0, 0)`；取不到时原版会 throw）"
                                   + " ⇒ 文案「" + text + "」只算出来、没刷到屏上。");
                }
                return false;
            }
            priceDisplaySetup(text);
            return true;
        }

        /// <summary>自检口（`_said` 去重闩）。</summary>
        public static void ResetForTest() { _said = false; }
    }
}
