// NetRuntime.cs — 联机会话的**常驻宿主**（一个进程只有一份）
//
// 为什么要有它：`NetSession` 是纯逻辑、不是 MonoBehaviour，而它需要有人**每帧 `Pump()`**
//   （收包 + 心跳 + 掉线检测 + 重连退避）。而且**会话不能挂在设置窗上** ——
//   主机点完【保存】就去翻别的界面了，窗口一关监听就没了。
//
// ⚠️ 批处理下没有帧循环 ⇒ 自检里由 `NetSelfTest` **显式 `Pump()`**（它不走这个 MonoBehaviour）。
// ⚠️ 真 Play 里窗口失焦会暂停 `Update` ⇒ 这里设 `Application.runInBackground`（`NetSession.EnsureBackground`）。
using UnityEngine;
using CardPresentation.Net;

namespace CardPresentation
{
    public class NetRuntime : MonoBehaviour
    {
        public static NetRuntime Instance { get; private set; }

        /// <summary>全局唯一的那个会话（没开主机/没连客机时它也在，只是状态是 `Off`）。</summary>
        public NetSession Session { get; private set; }

        /// <summary>大厅消息由本组件处理（`true`）。**进了对局要置 `false`** ——
        /// 那时候消息归 `NetBattle.Tick`（挂在对局驱动上）处理，两边抢着读会丢消息。</summary>
        public bool LobbyHandled = true;

        public static NetRuntime Ensure(Transform root = null)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("Net Runtime");
            if (root != null) go.transform.SetParent(root, false);
            // 🔴 **跨场景必须活着**：`Battle!` 之后是 `SceneManager.LoadScene`（单场景加载）——
            //    不 `DontDestroyOnLoad` 的话，切进战场时**整个壳连同这个会话一起没了**
            //    ⇒ 对局里没人 `Pump()` ⇒ **联机对局静默变成单机**（2026-09-26 收尾时发现的，
            //    自检是手工 pump 的所以掩盖了这一点）。⚠️ 编辑模式不能调它（会告警），自检不受影响。
            if (Application.isPlaying && go.transform.parent == null) Object.DontDestroyOnLoad(go);
            var rt = go.AddComponent<NetRuntime>();
            // 🔴 **编辑模式（自检）不跑 `Awake`** ⇒ 这里显式初始化 + 登记 `Instance`
            //    （同族先例：`WindowsManager.EnsureHost` 里那段「建完 Instance 还是 null」的更正）
            rt.Init();
            Instance = rt;
            return rt;
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Init();
            Instance = this;
        }

        /// <summary>幂等初始化（`Awake` 与 `Ensure` 都会调）。</summary>
        void Init()
        {
            if (Session == null) Session = NetSession.NewTcp();
            NetSession.EnsureBackground();
        }

        void OnDestroy() { if (Instance == this) { Instance = null; } }

        // ============================================================ 告诉玩家一件事

        // 🔴 **红线**（`项目任务.md` §三 第 14 条 表里那三条「只有日志、玩家什么都看不到」）：
        //    **联机层出的事不许只写 `Debug.LogError`**。但 `Net/` 是纯逻辑、够不到窗口系统
        //    ⇒ 排在这儿，由 `Update` 取出来交给 `WindowsManager` 弹。
        //    ⚠️ **壳是 `DontDestroyOnLoad` 的**（`ShellRuntime.Awake`），而 `WindowsManager` 挂在壳上
        //       （`ShellRuntime.cs` 里 `EnsureHost(root)`）⇒ **进了战场也弹得出来**，不只大厅。
        //    ⚠️ 批处理/自检**没有帧循环**，`Update` 不跑 ⇒ 通知会攒着，用 `DrainNoticesForTest()` 取
        //       （这样这三条才有**断言**盯着，不然又变成「只有日志、验不了」）。
        static readonly System.Collections.Generic.Queue<string> _notices =
            new System.Collections.Generic.Queue<string>();

        /// <summary>联机层要**当面**告诉玩家一件事（弹窗）。⚠️ **别拿它当日志用** —— 日志另写一份。</summary>
        public static void Notice(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            _notices.Enqueue(text);
        }

        /// <summary>还没弹出去的通知（自检用：批处理里没有窗口系统）。取走即清空。</summary>
        public static string[] DrainNoticesForTest()
        {
            var a = _notices.ToArray();
            _notices.Clear();
            return a;
        }

        void DrainNotices()
        {
            while (_notices.Count > 0)
            {
                string t = _notices.Dequeue();
                var wm = WindowsManager.Instance;
                if (wm != null) wm.ShowPopUp(t, "知道了", null);
                else Debug.LogWarning("[Net] 有件事要告诉玩家，但这一帧**没有 `WindowsManager`**"
                                    + "（自检/批处理？）⇒ 弹不出来，只能落在这里：" + t);
            }
        }

        void Update()
        {
            // 先弹通知：**它不该被 `Session == null` 那道闸挡住**（会话断了才更需要说话）
            DrainNotices();
            if (Session == null) return;
            Session.Pump();
            // 对局外（大厅/匹配阶段）的消息在这儿处理；进了对局就交给 `NetBattle.Tick`
            if (!LobbyHandled) return;
            NetMatchmaking.PumpLobby();
        }

        /// <summary>会话重建（换角色/换端口时用）。旧的那条**先收工**（会捎一句 `bye` 给对面）。</summary>
        public NetSession Reset()
        {
            if (Session != null) Session.Close(true, "主机重开了");
            Session = NetSession.NewTcp();
            return Session;
        }
    }
}
