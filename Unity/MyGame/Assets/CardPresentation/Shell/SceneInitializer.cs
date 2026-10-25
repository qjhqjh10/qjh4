// SceneInitializer.cs — 场景初始化分层（原版 `SceneInitializer` 的复刻）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【为什么要有它】原版每个场景里都有一台 `SceneInitializer`：它把场景里所有实现
//   `IBaseInitializable` 的 MonoBehaviour 收上来，**按 `InitializationPriority` 分四组**
//   依次初始化（Data → Manager → UI → AfterUI），每组「同步件跑完 → 异步件起协程 →
//   **等这一组所有件 `Status == Initialized`** → 才进下一组」，全组走完再触发完成回调。
//
//   我们以前是 `ShellRuntime.Build()` **同步一把建完**（顺序靠源码书写次序）。这一层补上之后：
//     · 需要「我先、你后」的件只要实现接口 + 填 `priority`，**不用再靠调用次序**；
//     · 需要「等所有件就绪」的件用 `RegisterCompletionCallback`（例如窗口层、首开窗）；
//     · 出错时**出声且不中断**（原版语义，见 `OnInitializableError`）。
//
// 【判据（第一权威 = 反编译，逐条）】
//   `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/SceneInitializer.cs`：
//     · `Awake`（:540）＝ 把两个静态钩子指到本实例 → `FetchAllInitializables()` →
//       先跑 `IBeforeInitializationHandler.OnBeforeInitialization()` → 起 `InitializeBehaviours` 协程
//     · `FetchAllInitializables`（:629）＝ `FindObjectsOfType<MonoBehaviour>(fetchInactiveBehaviours)`
//       **∪ 已登记的**；同时收 `IInitializationComplete` / `IBeforeInitializationHandler` 两份名单
//     · `InitializeBehaviours`（:702 / 协程体 :218-470）＝ `status = Initializing` →
//       按 `Enum.GetValues(InitializationPriority)` 升序逐组：
//         `Debug.Log("Initializing Group {p}")` → 同步批 → 异步批 →
//         `Debug.Log("Waiting for group {p} complete Initialization")` → **逐帧等该组全部 `Status == Initialized`**
//       → 组内 `IInitializationComplete` 收进等待名单 →（若 `needToRestart` ⇒ `Restartinitialization`）→
//       全部走完 ⇒ `status = Initialized` + `TriggerCompletionCallbacks()`
//     · `InitializeDefaultBehaviours`（:713）＝ 组内 `IInitializable` 逐个 `Initialize()`
//     · `InitializeAsyncBehaviours`（:757）/`AsyncInitialization`（:1221）＝ 组内 `IInitializableAsync`
//       逐个 `StartCoroutine(Initialize())`（枚举器跑到底）
//     · `OnInitializableError`（:1137）＝ `Debug.LogError("Failed to initialize behaviour: {name}\nError: {message}\n\n{stack}")`
//       + `status = Error`（**只在第一次**）+ 弹 `CustomErrors/FailedToInitializeServices`（`string.Format(词条, name)`）
//     · `OnInitializationComplete`（:1488）＝ 未完成 ⇒ 串到 `OnComplete`；已完成 ⇒ 立即调用
//     · `QueueInitializable`（:1356）/`Restartinitialization`（:1344）＝ 运行期新登记的件：
//       初始化中 ⇒ `needToRestart = true`；已初始化且允许重启 ⇒ 重跑整条
//     · `OnDestroy`（:623）＝ 两个静态钩子置 null
//
// 【与原版**有意不同**的两处（都写清楚，⛔ 别当「原版就这样」）】
//   ① **「这一组等完了吗」把 `Status == Error` 也算「已定」** —— 原版只等 `Initialized`，
//      于是一件抛异常后 `Status` 停在 `Pending/Error` ⇒ 那条协程**永远等下去**（同一函数里
//      `OnInitializableError` 又写着「不中断后续初始化」⇒ 两条语义自相矛盾）。
//      我们按「**出错要出声、且不拖死整条链**」实现：`Error` 也算 settled（见 `IsSettled`）。
//   ② **多一个 `RunSynchronouslyForTest()`** —— 本工程批处理**没有帧循环**（`Update` 不跑），
//      而原版这条链完全跑在协程里 ⇒ 自检里必须有一个「手动把协程与异步枚举器全部推到底」的入口。
//      ⛔ 生产路径不许调它（真机走 Unity 协程）。
//
// 【落点】放 `Shell/`（原判据建议 `Core/`）：本代理白名单里 `Core/**` 归别人，而调用方
//   （`ShellRuntime` / `MainMenuRuntime`）全在 `Shell/` ⇒ 放同一目录，**跨目录依赖为零**。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>初始化分组（原版 `InitializationPriority`，**名字与顺序逐字照抄**）。</summary>
    public enum InitializationPriority { Data = 0, Manager = 1, UI = 2, AfterUI = 3 }

    /// <summary>单件初始化状态（原版 `InitializationStatus`：`Error = -1` 在前）。</summary>
    public enum InitializationStatus { Error = -1, Pending = 0, Initializing = 1, Initialized = 2 }

    public interface IBaseInitializable
    {
        InitializationPriority Priority { get; }
        InitializationStatus Status { get; set; }
    }

    public interface IInitializable : IBaseInitializable { void Initialize(); }

    /// <summary>⚠️ 方法名是 <c>Initialize</c> 且**返回 `IEnumerator`**（原版 `IInitializableAsync.cs`）。</summary>
    public interface IInitializableAsync : IBaseInitializable { IEnumerator Initialize(); }

    /// <summary>「这一组等完」之后回调（原版 `IInitializationComplete`）。</summary>
    public interface IInitializationComplete { void OnInitializationComplete(); }

    /// <summary>初始化**之前**回调（原版 `IBeforeInitializationHandler`）。</summary>
    public interface IBeforeInitializationHandler { void OnBeforeInitialization(); }

    /// <summary>原版 `InitializableBehaviour` 的等价基类：`Start()` 里自己向场景登记。</summary>
    public abstract class InitializableBehaviour : MonoBehaviour, IInitializable, IInitializationComplete, IBeforeInitializationHandler
    {
        [SerializeField] InitializationPriority priority = InitializationPriority.Data;
        [SerializeField] InitializationStatus status = InitializationStatus.Pending;

        public InitializationPriority Priority { get { return priority; } }
        public InitializationStatus Status { get { return status; } set { status = value; } }

        public abstract void Initialize();

        void Start() { SceneInitializer.QueueForInitialization(this); }

        public virtual void OnInitializationComplete() { }
        public virtual void OnBeforeInitialization() { }
    }

    /// <summary>
    /// 场景初始化机（原版 `SceneInitializer`）。**判据全文见文件头**。
    /// ⛔ 一个场景只该有一台；`Awake` 里重复的那台会被 `Destroy`（同原版单例语义）。
    /// </summary>
    public sealed class SceneInitializer : MonoBehaviour
    {
        public static SceneInitializer Instance { get; private set; }

        static Action<IBaseInitializable> s_onQueued;
        static Action<Action> s_onRegister;

        [SerializeField] bool fetchInactiveBehaviours = true;
        [SerializeField] bool allowInitializationRestart = true;

        readonly List<MonoBehaviour> _initializables = new List<MonoBehaviour>();
        readonly List<IInitializationComplete> _waitingForCompletion = new List<IInitializationComplete>();
        readonly List<IBeforeInitializationHandler> _beforeStart = new List<IBeforeInitializationHandler>();

        InitializationStatus _status = InitializationStatus.Pending;
        bool _needToRestart;
        bool _errorPopupShown;
        bool _begun;

        /// <summary>当前整机状态（原版 `Status`）。</summary>
        public InitializationStatus Status { get { return _status; } }

        /// <summary>「全部初始化完成」回调（原版 `OnComplete`；`RegisterCompletionCallback` 串在这里）。</summary>
        public Action OnComplete;

        // ==================================================================
        //  静态入口（原版同名）
        // ==================================================================

        /// <summary>登记一件。（原版 `SceneInitializer.QueueForInitialization`，:498）
        /// <para>⚠️ 早于 `Awake` 调 ⇒ 原版打 `LogWarning`：
        /// 「Scene Initializer is not available - it's either missing or you are trying to queue too early, requester: …」。</para></summary>
        public static void QueueForInitialization(IBaseInitializable initializable)
        {
            if (initializable == null) return;
            if (s_onQueued != null) { s_onQueued(initializable); return; }
            Debug.LogWarning("Scene Initializer is not available - it's either missing or you are trying to queue too early, requester: "
                           + Describe(initializable));
        }

        /// <summary>注册完成回调。（原版 `RegisterCompletionCallback`，:527；早于 `Awake` ⇒ **LogError**）</summary>
        public static void RegisterCompletionCallback(Action callback)
        {
            if (callback == null) return;
            if (s_onRegister != null) { s_onRegister(callback); return; }
            // 原版就是 LogError（不是 Warning）—— 早注册会**永久丢**回调。
            Debug.LogError("Scene Initializer is not available - it's either missing or you are trying to register too early");
        }

        /// <summary>场景里有没有初始化机（自检/调用方判前提用；原版无此口）。</summary>
        public static bool Available { get { return s_onQueued != null; } }

        // ==================================================================
        //  生命周期
        // ==================================================================

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SceneInitializer] 场景里出现了第二台 ⇒ 拆掉这一台（原版单例语义）");
                Destroy(this);
                return;
            }
            if (_begun) return;
            _begun = true;
            Instance = this;
            s_onQueued = QueueInitializable;
            s_onRegister = OnInitializationComplete;

            FetchAllInitializables();

            // 原版：**先**跑 `OnBeforeInitialization`，再起初始化协程。
            RunBeforeInitialization();

            StartCoroutine(InitializeBehaviours());
        }

        void RunBeforeInitialization()
        {
            for (int i = 0; i < _beforeStart.Count; i++)
            {
                var h = _beforeStart[i];
                if (h == null) continue;
                try { h.OnBeforeInitialization(); }
                catch (Exception e)
                {
                    // 原版这一跳没有 try（`Awake` 里的循环）；我们不让它把整台机掀掉 ⇒ 走同一个出声口。
                    OnInitializableError(h as IBaseInitializable, e);
                }
            }
        }

        /// <summary>🆕 **编辑器 / 批处理入口**：`AddComponent` 在编辑器里**不派 `Awake`**（本工程已记过的坑）
        /// ⇒ 由建它的人显式调本方法，走一遍「接静态钩子 → 收集 → before → 同步跑完」。
        /// <para>⚠️ 真机（Play）里 `Awake` 会先跑并把 `_begun` 置真 ⇒ 本方法是**空操作**（不会跑两遍）。</para>
        /// <para>⚠️ 编辑器里建场景时调它，会让场景里**已实现接口的件**当场初始化一次；今天（复刻第 1 轮）**
        /// 没有任何件实现这套接口**，所以是空转。</para></summary>
        public void BeginIfNeeded()
        {
            if (_begun) return;
            _begun = true;
            Instance = this;
            s_onQueued = QueueInitializable;
            s_onRegister = OnInitializationComplete;
            FetchAllInitializables();
            RunBeforeInitialization();
            RunSynchronouslyForTest();
        }

        void OnDestroy()
        {
            // 原版 :623 只清两个静态钩子（不清 Instance —— 那是单例自身的事）。
            if (s_onQueued == QueueInitializable) s_onQueued = null;
            if (s_onRegister == OnInitializationComplete) s_onRegister = null;
            if (Instance == this) Instance = null;
        }

        // ==================================================================
        //  收集
        // ==================================================================

        /// <summary>原版 `FetchAllInitializables`（:629）：场景里全部 MonoBehaviour（含未激活，按开关）
        /// **∪ 已登记的**；同时收 `IInitializationComplete` / `IBeforeInitializationHandler`。</summary>
        public void FetchAllInitializables()
        {
            // ① 先留一份「已登记的」（运行期 `QueueInitializable` 加进来的那些，`FindObjectsOfType` 不一定收得到）
            var registered = new List<MonoBehaviour>(_initializables);

            var found = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(fetchInactiveBehaviours);

            _initializables.Clear();
            var seen = new HashSet<MonoBehaviour>();
            for (int i = 0; i < registered.Count; i++)
            {
                var m = registered[i];
                if (m != null && seen.Add(m)) _initializables.Add(m);
            }
            for (int i = 0; i < found.Length; i++)
            {
                var m = found[i];
                if (m != null && seen.Add(m)) _initializables.Add(m);
            }

            _waitingForCompletion.Clear();
            _beforeStart.Clear();
            for (int i = 0; i < _initializables.Count; i++)
            {
                var m = _initializables[i];
                if (m is IInitializationComplete) _waitingForCompletion.Add((IInitializationComplete)m);
                if (m is IBeforeInitializationHandler) _beforeStart.Add((IBeforeInitializationHandler)m);
            }
        }

        /// <summary>原版 `GetInitializablesWithPriority`（:668）。</summary>
        public List<IBaseInitializable> GetInitializablesWithPriority(InitializationPriority priority)
        {
            var result = new List<IBaseInitializable>();
            for (int i = 0; i < _initializables.Count; i++)
            {
                var b = _initializables[i] as IBaseInitializable;
                if (b != null && b.Priority == priority) result.Add(b);
            }
            return result;
        }

        // ==================================================================
        //  主循环
        // ==================================================================

        IEnumerator InitializeBehaviours()
        {
            _status = InitializationStatus.Initializing;
            _needToRestart = false;

            var priorities = (InitializationPriority[])Enum.GetValues(typeof(InitializationPriority));
            Array.Sort(priorities);                       // 原版按 `Enum.GetValues` 的自然序（= 声明序）

            for (int g = 0; g < priorities.Length; g++)
            {
                var priority = priorities[g];
                var group = GetInitializablesWithPriority(priority);
                Debug.Log("Initializing Group " + priority);

                // ① 同步批
                var sync = new List<IInitializable>();
                for (int i = 0; i < group.Count; i++) if (group[i] is IInitializable) sync.Add((IInitializable)group[i]);
                InitializeDefaultBehaviours(sync);

                // ② 异步批
                var asyncs = new List<IInitializableAsync>();
                for (int i = 0; i < group.Count; i++) if (group[i] is IInitializableAsync) asyncs.Add((IInitializableAsync)group[i]);
                InitializeAsyncBehaviours(asyncs);

                // ③ 等这一组全部定下来
                Debug.Log("Waiting for group " + priority + " complete Initialization");
                while (!AllSettled(group)) yield return null;

                // ④ 组内完成回调进等待名单（原版 `Except(...)` + `AddRange`）
                for (int i = 0; i < group.Count; i++)
                {
                    var c = group[i] as IInitializationComplete;
                    if (c != null && !_waitingForCompletion.Contains(c)) _waitingForCompletion.Add(c);
                }

                if (_needToRestart)
                {
                    _needToRestart = false;
                    Restartinitialization();
                    yield break;                          // 原版：重起一条新协程、本协程结束
                }
            }

            _status = InitializationStatus.Initialized;
            TriggerCompletionCallbacks();
        }

        /// <summary>组内是否都已定下来（见文件头差异 ①：`Error` 也算 settled）。</summary>
        static bool AllSettled(List<IBaseInitializable> group)
        {
            for (int i = 0; i < group.Count; i++) if (!IsSettled(group[i])) return false;
            return true;
        }
        static bool IsSettled(IBaseInitializable i)
        {
            if (i == null) return true;
            // ⚠️ 被 Unity 销毁的件（== null 的 UnityEngine.Object）也算 settled —— 否则永远等。
            var obj = i as UnityEngine.Object;
            if (obj != null && obj == null) return true;
            return i.Status == InitializationStatus.Initialized || i.Status == InitializationStatus.Error;
        }

        /// <summary>原版 `InitializeDefaultBehaviours`（:713）：逐个同步 `Initialize()`；
        /// **单件抛异常不打断同组其它件**（原版同一函数里的 `try/catch → OnInitializableError`）。</summary>
        void InitializeDefaultBehaviours(IEnumerable<IInitializable> group)
        {
            if (group == null) return;
            foreach (var b in group)
            {
                if (b == null) continue;
                try
                {
                    b.Status = InitializationStatus.Initializing;
                    b.Initialize();
                    b.Status = InitializationStatus.Initialized;
                }
                catch (Exception e)
                {
                    b.Status = InitializationStatus.Error;
                    OnInitializableError(b, e);
                }
            }
        }

        /// <summary>原版 `InitializeAsyncBehaviours`（:757）：逐个起协程。</summary>
        void InitializeAsyncBehaviours(IEnumerable<IInitializableAsync> group)
        {
            if (group == null) return;
            foreach (var b in group)
            {
                if (b == null) continue;
                StartCoroutine(AsyncInitialization(b));
            }
        }

        /// <summary>原版 `AsyncInitialization`（:1221）：把 `Initialize()` 那个枚举器跑到底。</summary>
        IEnumerator AsyncInitialization(IInitializableAsync initializable)
        {
            if (initializable == null) yield break;
            IEnumerator e = null;
            try
            {
                initializable.Status = InitializationStatus.Initializing;
                e = initializable.Initialize();
            }
            catch (Exception ex)
            {
                initializable.Status = InitializationStatus.Error;
                OnInitializableError(initializable, ex);
                yield break;
            }
            while (true)
            {
                bool moved;
                object cur = null;
                try { moved = e.MoveNext(); if (moved) cur = e.Current; }
                catch (Exception ex)
                {
                    initializable.Status = InitializationStatus.Error;
                    OnInitializableError(initializable, ex);
                    yield break;
                }
                if (!moved) break;
                yield return cur;
            }
            initializable.Status = InitializationStatus.Initialized;
        }

        // ==================================================================
        //  出错（原版 `OnInitializableError`，:1137）
        // ==================================================================

        /// <summary>固定模板 + **首次**弹 `CustomErrors/FailedToInitializeServices`；**不中断后续初始化**。</summary>
        void OnInitializableError(IBaseInitializable initializable, Exception e)
        {
            string name = Describe(initializable);
            string stack = UnityFormatStacktrace(e);
            Debug.LogError("Failed to initialize behaviour: " + name
                         + "\nError: " + (e != null ? e.Message : "(null exception)")
                         + "\n\n" + stack);

            if (_status == InitializationStatus.Error) return;   // 原版：**只在第一次**做下面这段
            _status = InitializationStatus.Error;

            string term = Loc.T("CustomErrors/FailedToInitializeServices");
            string text;
            try { text = string.Format(term, name); }
            catch (FormatException) { text = term + "\n" + name; }   // 词条里没有 {0} ⇒ 原样 + 名字（不静默）

            var wm = WindowsManager.Instance;
            if (wm == null)
            {
                // 原版直接 `SingletonBehaviour<WindowsManager>.Instance.ShowPopUp(...)`（宿主必在）。
                // 我们这一层可能**早于窗口层**跑（Data/Manager 组）⇒ 宿主可能还没建。
                // ⛔ 不静默：日志里说清「这一声没弹出来」。
                Debug.LogWarning("[SceneInitializer] 初始化失败弹窗**没弹**（WindowsManager 还没建）—— 上面那条 LogError 已出声。"
                               + " 词条 `CustomErrors/FailedToInitializeServices` = " + text);
                return;
            }
            if (!_errorPopupShown)
            {
                _errorPopupShown = true;
                wm.ShowPopUp(text);
            }
        }

        /// <summary>把 Exception 栈整理成 Unity 格式（原版 `GetUnityFormatStacktrace`，:785）：
        /// **去掉首行**（首行是 `ExceptionType: message`，原版模板里已经单独印了 message）+ 统一缩进。</summary>
        static string UnityFormatStacktrace(Exception e)
        {
            if (e == null) return "";
            string st = e.StackTrace;
            if (string.IsNullOrEmpty(st)) return "";
            var lines = st.Replace("\r\n", "\n").Split('\n');
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                string ln = lines[i];
                if (string.IsNullOrEmpty(ln)) continue;
                if (ln.TrimStart().StartsWith("at ")) ln = "  " + ln.TrimStart();
                sb.AppendLine(ln);
            }
            return sb.ToString().TrimEnd();
        }

        // ==================================================================
        //  完成
        // ==================================================================

        /// <summary>原版 `OnInitializationComplete(Action)`（:1488）：未完成 ⇒ 串进 `OnComplete`；已完成 ⇒ **立即调用**。</summary>
        public void OnInitializationComplete(Action callback)
        {
            if (callback == null) return;
            if (_status != InitializationStatus.Initialized) { OnComplete += callback; return; }
            try { callback(); }
            catch (Exception e) { Debug.LogError("[SceneInitializer] 完成回调抛异常：" + e); }
        }

        /// <summary>原版 `TriggerCompletionCallbacks`（:1232）：先对 `IInitializationComplete` 逐个回调，再跑 `OnComplete`（**一次性**）。</summary>
        void TriggerCompletionCallbacks()
        {
            var snapshot = new List<IInitializationComplete>(_waitingForCompletion);
            _waitingForCompletion.Clear();
            for (int i = 0; i < snapshot.Count; i++)
            {
                var c = snapshot[i];
                if (c == null) continue;
                try { c.OnInitializationComplete(); }
                catch (Exception e)
                {
                    Debug.LogError("Failed to initialize behaviour: " + Describe(c as IBaseInitializable)
                                 + "\nError: " + e.Message + "\n\n" + UnityFormatStacktrace(e));
                }
            }
            var done = OnComplete;
            OnComplete = null;
            if (done != null)
                try { done(); }
                catch (Exception e) { Debug.LogError("[SceneInitializer] OnComplete 抛异常：" + e); }
        }

        // ==================================================================
        //  运行期新登记 / 重启（原版 :1356 / :1344）
        // ==================================================================

        void QueueInitializable(IBaseInitializable initializable)
        {
            var mb = initializable as MonoBehaviour;
            if (mb == null)
            {
                // 原版 `QueueInitializable` 只收 MonoBehaviour（`is MonoBehaviour` 判据）⇒ 其它出声丢掉。
                Debug.LogWarning("[SceneInitializer] 只收 MonoBehaviour：丢掉 " + Describe(initializable));
                return;
            }
            if (_initializables.Contains(mb)) { /* 重复登记：幂等（原版 `Union` 去重） */ }
            else _initializables.Add(mb);
            if (mb is IInitializationComplete && !_waitingForCompletion.Contains((IInitializationComplete)mb))
                _waitingForCompletion.Add((IInitializationComplete)mb);

            if (_status == InitializationStatus.Initializing) { _needToRestart = true; return; }
            if (_status == InitializationStatus.Initialized && allowInitializationRestart) Restartinitialization();
        }

        /// <summary>原版 `Restartinitialization`（:1344）：日志 + 重跑整条链。</summary>
        void Restartinitialization()
        {
            Debug.Log("Restarting initialization due to new behaviours added");
            StopAllCoroutines();
            StartCoroutine(InitializeBehaviours());
        }

        // ==================================================================
        //  自检口（⛔ 生产不调；文件头差异 ②）
        // ==================================================================

        /// <summary>批处理没有帧循环 ⇒ 手动把整条初始化链推到底（同步件直接跑、异步枚举器也推到 `MoveNext()==false`）。
        /// <para>返回 `true` = 走到了 `Initialized`（或第一次 `Error` 后仍把后续组跑完）；`false` = 超步数保护触发。</para>
        /// <para>⚠️ 生产路径**不许**调它：真机走 Unity 协程（`Awake` 已起）。</para></summary>
        public bool RunSynchronouslyForTest(int maxStepsPerEnumerator = 100000)
        {
            _status = InitializationStatus.Initializing;
            _needToRestart = false;
            var priorities = (InitializationPriority[])Enum.GetValues(typeof(InitializationPriority));
            Array.Sort(priorities);
            for (int g = 0; g < priorities.Length; g++)
            {
                var group = GetInitializablesWithPriority(priorities[g]);
                var sync = new List<IInitializable>();
                for (int i = 0; i < group.Count; i++) if (group[i] is IInitializable) sync.Add((IInitializable)group[i]);
                InitializeDefaultBehaviours(sync);

                var asyncs = new List<IInitializableAsync>();
                for (int i = 0; i < group.Count; i++) if (group[i] is IInitializableAsync) asyncs.Add((IInitializableAsync)group[i]);
                for (int i = 0; i < asyncs.Count; i++)
                {
                    var b = asyncs[i];
                    if (b == null) continue;
                    IEnumerator e = null;
                    try { b.Status = InitializationStatus.Initializing; e = b.Initialize(); }
                    catch (Exception ex) { b.Status = InitializationStatus.Error; OnInitializableError(b, ex); continue; }
                    int steps = 0;
                    while (e != null)
                    {
                        bool moved;
                        try { moved = e.MoveNext(); }
                        catch (Exception ex) { b.Status = InitializationStatus.Error; OnInitializableError(b, ex); break; }
                        if (!moved) break;
                        if (++steps > maxStepsPerEnumerator)
                        {
                            Debug.LogError("[SceneInitializer] 异步初始化超步数保护（" + Describe(b) + "）—— 该件可能永远不结束");
                            return false;
                        }
                    }
                    if (b.Status != InitializationStatus.Error) b.Status = InitializationStatus.Initialized;
                }

                for (int i = 0; i < group.Count; i++)
                {
                    var c = group[i] as IInitializationComplete;
                    if (c != null && !_waitingForCompletion.Contains(c)) _waitingForCompletion.Add(c);
                }
            }
            // ⚠️ 批处理里 `_status` 被 `OnInitializableError` 置过 `Error` 时不覆盖成 `Initialized`
            //    （那是「出过错」的事实，`OnInitializationComplete` 的 `!= Initialized` 分支要能看见）。
            if (_status != InitializationStatus.Error) _status = InitializationStatus.Initialized;
            TriggerCompletionCallbacks();
            return true;
        }

        /// <summary>自检用：清空静态状态（跨用例隔离；原版无此口）。</summary>
        public static void ResetStaticsForTest()
        {
            s_onQueued = null;
            s_onRegister = null;
            if (Instance != null) { UnityEngine.Object.DestroyImmediate(Instance); Instance = null; }
        }

        static string Describe(IBaseInitializable i)
        {
            var o = i as UnityEngine.Object;
            if (o != null) return o.name;
            return i != null ? i.GetType().Name : "(null)";
        }
    }
}
