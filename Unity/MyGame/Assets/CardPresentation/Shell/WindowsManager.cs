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
//   · `GameWindow.Open()`：宽度 < 阈值时 `TransformScalerBySmallScreenUI.SetScale(extraScaleSmallScreen)`
//     —— 实证值：**普通窗 1.0 · 商店/活动类 1.2**（`GameWindow__Open.c` + `bundle_menus_assets_all` 18 例）
//
// 🔴 **不是照抄的部分（原版查不到，如实标 —— 铁律 3）**：
//   ① **弹窗 prefab 与「类 → prefab」字典是自建的**：全 `assets_full` grep
//      `popupWindowOneButton` / `temporaryWindowDictionary` **零命中**（正本 §五 第 2 条）。
//      ⇒ `ShowPopUp` 我们只保留**签名与行为**（文案 + 1~2 个按钮 + 结果回调），界面由 `PopUpGameWindow` 自己搭。
//   ② **「宽度 < 阈值」的那个阈值查不到** ⇒ 只在 `extraScaleSmallScreen != 1f` 时才放大；
//      普通窗实测就是 1.0 ⇒ 默认空转（不是没实现，是没东西可放大）。
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

    public enum WindowState { Closed = 0, Opening, Open, Background }

    /// <summary>挂在场景里的锚点节点上（主菜单三个）。`OnEnable` 自注册 —— 原版就是这个机制。
    /// ⚠️ `[ExecuteAlways]` 是**我们加的**：建场景是在**编辑模式**下跑的，而普通 MonoBehaviour 的
    /// `OnEnable` 在编辑模式下不触发 ⇒ 不注册就一个锚点都没有（2026-09-22 第一版自检 8 条红里有 5 条是这个根因）。</summary>
    [ExecuteAlways]
    public class WindowHolder : MonoBehaviour
    {
        public WindowsPlacement placement = WindowsPlacement.None;

        void OnEnable() { RegisterNow(); }
        void OnDisable() { WindowsManager.UnregisterAnchor(placement, transform); }

        /// <summary>🔴 **必须由建场景的代码在「赋完 placement 之后」显式调一次** ——
        /// `AddComponent` 的那一刻 `OnEnable` 就跑掉了，而 `placement` 是**下一行**才赋值的
        /// ⇒ 光靠 `OnEnable` 会拿 `None` 去注册（2026-09-22 第一版就这么白跑一轮，日志里那条
        /// 「placement = None」的报错就是它）。同一个节点重复注册是幂等的（`RegisterAnchor` 会直接返回）。</summary>
        public void RegisterNow() { WindowsManager.RegisterAnchor(placement, transform); }
    }

    /// <summary>所有菜单窗口的基类。子类重写 <see cref="Open"/> 做自己的铺数据/播动画。</summary>
    public class GameWindow : MonoBehaviour
    {
        public WindowType type = WindowType.Fullscreen;
        public WindowsPlacement placement = WindowsPlacement.Popup;
        /// <summary>ESC 能不能关。**逐窗不同，照各自实证值填**（正本 §三 第 10 条列了 7 个实例）。</summary>
        public bool closeOnEsc = true;
        /// <summary>小屏 UI 下的额外放大倍数。实证：普通窗 1.0 · 商店/活动类 1.2。</summary>
        public float extraScaleSmallScreen = 1f;
        /// <summary>开窗音效（原版 `TryOpen` 里播，走 FX 组）。没有就不播。</summary>
        public AudioClip openSound;

        public WindowState CurrentState { get; private set; }
        public object Data { get; private set; }
        public WindowsManager Manager { get; internal set; }

        /// <summary>原版叫 `TryOpen`：只有它做「播音 → 激活 → 进 Open 态」这一串。</summary>
        public bool TryOpen(object data)
        {
            SetupData(data);
            if (openSound != null) WFSoundPlayer.Play(openSound, true, 1f, 1f);   // = 原版 `SoundManager.Play2D(..., MixerType.FX)`
            gameObject.SetActive(true);                                           // 建场景时窗口是关着的（照原版）
            CurrentState = WindowState.Open;
            Open();
            return true;
        }

        protected virtual void SetupData(object data) { Data = data; }

        /// <summary>子类在这里铺自己的内容。基类什么都不做（原版 `Open()` 也是虚方法）。</summary>
        public virtual void Open() { }

        /// <summary>被更高优先级的窗压到背景（原版 `ToBackground()`）。</summary>
        public virtual void ToBackground() { CurrentState = WindowState.Background; }

        public virtual void Close()
        {
            CurrentState = WindowState.Closed;
            Data = null;
            if (Manager != null) Manager.NotifyClosed(this);
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 窗口管理器单例。**名字照原版（`WindowsManager`，注意有个 s；全工程没有 `WindowManager` 这个类）**。
    /// 挂在 `Shell` 场景里的常驻对象上。
    /// </summary>
    public class WindowsManager : MonoBehaviour
    {
        public static WindowsManager Instance { get; private set; }

        /// <summary>锚点表。原版是 `static Dictionary<WindowsPlacement, Transform> anchors`。</summary>
        static readonly Dictionary<WindowsPlacement, Transform> _anchors =
            new Dictionary<WindowsPlacement, Transform>();

        public readonly List<GameWindow> openWindows = new List<GameWindow>();
        public GameWindow currentWindow;
        public GameWindow popUpWindow;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>
        /// **确保场景里有 `WindowsManager` 与三个锚点**（幂等）。原版这三个 Holder 在主菜单场景里
        /// （`1 - Below Upper Bar Holder{10}` / `2 - Canvas Holder Above upper bar{5}` / `3 - PopUp Holder{15}`，
        /// **缺一不可**；正本 §三 第 7 条）。
        /// 🔴 **判据只留这一份**：壳（`ShellRuntime`）与「单独打开某个界面场景按 Play」都走它 ——
        /// 两处各建一次 = 迟早不一致（CLAUDE.md §三）。名字与 placement 都照原版。
        /// </summary>
        public static WindowsManager EnsureHost(Transform root = null)
        {
            if (Instance != null) return Instance;

            var holderRoot = new GameObject("Window Anchors").transform;
            if (root != null) holderRoot.SetParent(root, false);
            MakeHolder(holderRoot, "1 - Below Upper Bar Holder", WindowsPlacement.World);
            MakeHolder(holderRoot, "2 - Canvas Holder Above upper bar", WindowsPlacement.Canvas);
            MakeHolder(holderRoot, "3 - PopUp Holder", WindowsPlacement.Popup);

            var go = new GameObject("WindowsManager");
            if (root != null) go.transform.SetParent(root, false);
            var wm = go.AddComponent<WindowsManager>();
            Debug.Log("[Win] 场景里没有 `WindowsManager` ⇒ 现建了一台 + 三个锚点（单独打开界面场景时走这条路）");
            return wm;
        }

        static void MakeHolder(Transform parent, string name, WindowsPlacement p)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            var h = t.gameObject.AddComponent<WindowHolder>();
            h.placement = p;
            // ⚠️ 先赋字段**再**注册（`OnEnable` 在 `AddComponent` 那一刻就跑过了，那时 placement 还是 None）
            h.RegisterNow();
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

        /// <summary>取锚点。**取不到要报出来**（照原版 `CustomDebug.LogError`）—— 静默返回 null 会让窗口建到场景根上。</summary>
        public static Transform GetWindowAnchor(WindowsPlacement p)
        {
            if (_anchors.TryGetValue(p, out var t) && t != null) return t;
            Debug.LogError($"[Win] 找不到 {p} 的锚点 —— 场景里缺对应的 `WindowHolder`。" +
                           "主菜单那一层需要 10 / 5 / 15 三个都齐（正本 §三 第 7 条）");
            return null;
        }

        public static bool HasAnchor(WindowsPlacement p) => _anchors.TryGetValue(p, out var t) && t != null;

        public static void ClearAnchorsForTest() { _anchors.Clear(); }

        // ---------------------------------------------------------- 开 / 关

        /// <summary>照原版 `OpenWindowCO` 的判定顺序。</summary>
        public void OpenWindow(GameWindow win, object data = null, bool closeAll = false)
        {
            if (win == null) { Debug.LogError("[Win] OpenWindow(null)"); return; }

            if (closeAll) CloseAllWindows();

            if (win.type == WindowType.Fullscreen)
            {
                // 原版：全屏窗开时把当前主窗关掉
                if (currentWindow != null && currentWindow != win) currentWindow.Close();
                currentWindow = win;
            }
            else
            {
                // 原版：弹窗开时把上一个压到背景
                if (currentWindow != null && currentWindow != win) currentWindow.ToBackground();
                popUpWindow = win;
            }

            win.Manager = this;
            if (!openWindows.Contains(win)) openWindows.Add(win);
            win.TryOpen(data);
        }

        /// <summary>把窗口挂到它自己的锚点上（建场景时调一次；窗口是自己建的，锚点由 `WindowHolder` 给）。</summary>
        public static void AttachToAnchor(GameWindow win)
        {
            var anchor = GetWindowAnchor(win.placement);
            if (anchor == null) return;
            win.transform.SetParent(anchor, false);
            win.transform.localPosition = Vector3.zero;
            win.transform.localRotation = Quaternion.identity;
            win.transform.localScale = Vector3.one;
        }

        internal void NotifyClosed(GameWindow win)
        {
            openWindows.Remove(win);
            if (currentWindow == win) currentWindow = null;
            if (popUpWindow == win) popUpWindow = null;
        }

        public void CloseAllWindows()
        {
            for (int i = openWindows.Count - 1; i >= 0; i--)
                if (openWindows[i] != null) openWindows[i].Close();
            openWindows.Clear();
            currentWindow = null;
            popUpWindow = null;
        }

        // ---------------------------------------------------------- 弹窗

        /// <summary>
        /// 通用弹窗（原版 `ShowPopUp(text, localizeTexts, closeOnEsc, …)` 三个重载）。
        /// ⚠️ **原版的 popup prefab 本地没有**（正本 §五 第 2 条）⇒ `PopUpGameWindow` 是我们自建的，
        /// 只保证**行为**一致：一段文案 + 1~2 个按钮 + 选完回调。
        /// </summary>
        public void ShowPopUp(string text, string okText = null, System.Action onOk = null,
                              string cancelText = null, System.Action onCancel = null)
        {
            var win = PopUpGameWindow.Create(this, text, okText, onOk, cancelText, onCancel);
            OpenWindow(win);
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
    }
}
