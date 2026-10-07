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
            // 🆕 2026-10-17（B23·A902）：大厅那半边**必须**盯着这台会话（对面掉线/离开 ⇒ 出声 + 撤匹配）。
            //    ⛔ 别挪到「第一次用的时候」懒挂：掉线是在 `Update` 的 `Session.Pump()` 里判出来的，
            //      那一帧 `PumpLobby` 还没跑 ⇒ 懒挂会整帧丢掉第一次掉线。判据 → `NetMatchmaking.WireLobby`。
            NetMatchmaking.WireLobby(Session);
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
        /// <summary>🔴 **2026-09-27 加的锁**：`Notice` 现在**也会从后台线程调**
        /// （`UpnpPortMapper` 在它自己的工作线程上报告「路由器那边成没成」），
        /// 而 `DrainNotices` 在主线程取 ⇒ **不加锁就是竞态**（`Queue` 不是线程安全的）。
        /// ⚠️ 锁里只做入队/出队，**不碰 Unity 对象**（`ShowPopUp` 留在锁外）。</summary>
        static readonly object _noticeLock = new object();

        /// <summary>联机层要**当面**告诉玩家一件事（弹窗）。⚠️ **别拿它当日志用** —— 日志另写一份。</summary>
        public static void Notice(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            lock (_noticeLock) _notices.Enqueue(text);
        }

        /// <summary>还没弹出去的通知（自检用：批处理里没有窗口系统）。取走即清空。</summary>
        public static string[] DrainNoticesForTest()
        {
            lock (_noticeLock)
            {
                var a = _notices.ToArray();
                _notices.Clear();
                return a;
            }
        }

        void DrainNotices()
        {
            while (true)
            {
                string t;
                lock (_noticeLock)
                {
                    if (_notices.Count == 0) return;
                    t = _notices.Dequeue();
                }
                var wm = WindowsManager.Instance;
                if (wm != null) wm.ShowPopUp(t, "知道了", null);
                else Debug.LogWarning("[Net] 有件事要告诉玩家，但这一帧**没有 `WindowsManager`**"
                                    + "（自检/批处理？）⇒ 弹不出来，只能落在这里：" + t);
            }
        }

        // ============================================================ 🆕 2026-10-17（B17·A900）
        //  「对手掉线」那扇窗**开了之后**那两件事：**刷新它** 与 **收掉它**。
        //  判据（原版，全量反编译）：
        //   · 刷新 —— `Everguild.BattleErrorUIManager__UpdateReconnectWindow.c`：它把
        //     `TimeSpan.FromSeconds(sec)` 拼进正文，**窗还开着就改同一个窗里的字**、
        //     不在（`activeSelf == false`）才 `WindowsManager.ShowPopUp`。调用它的是
        //     `BattleManager.UpdateReconnectStatus`（`+0x1B0`，`Action<int>`，每秒一拍）。
        //   · 收掉 —— `Everguild.BattleErrorUIManager__ConnectionStatusChanged.c:32-37,59-63`：
        //     状态回到 0/3/4/5 那一支 ⇒ **`WindowsManager.CloseAllWindows()`**。

        /// <summary>🆕 A900：**把「等他回来」那扇提示窗的正文换成新的秒数**（原版每秒那一拍
        /// `UpdateReconnectStatus(secLeft)` 的落点）。
        /// 🔴 走的是 `WindowsManager.ShowPopUp` 的**复用支**（`ShowMessagePopUp` 认「还开着 ⇒
        /// 重配**同一扇**」，不是新建）⇒ 与原版「改同一个窗里的字」同形。
        /// ⛔ **不走 `Notice` 队列** —— 那是「**新弹一条**」的语义，每秒入一次队会把通知队撑爆，
        ///    也会让「一次掉线 = 一条弹窗」那条判据失去意义（`NetBattleTest` §9① 盯的就是那个数）。
        /// ⚠️ 没有窗口系统（批处理 / 自检）时**什么都不做** —— 这一路的数字仍写在**提示行**上
        ///    （`NetBattle.ReportReconnectStatus` 两个口同时写），不会丢。</summary>
        public static void UpdateReconnectPopup(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            var wm = WindowsManager.Instance;
            if (wm == null) return;
            wm.ShowPopUp(text, "知道了", null);
        }

        /// <summary>🆕 A900：**把还开着的那扇消息弹窗收掉**（对手回来了 / 倒计时到点判了弃权）。
        /// 🔴 原版那一跳是 `WindowsManager.CloseAllWindows()`，我们**不调它** —— 它会把玩家这一刻
        /// **自己开的**别的窗一起关掉（B13 报告 §② 已记这条口径）；改用同族里**只关弹窗**那一颗：
        /// `WindowsManager.HidePopUp(force:false)`（原版同名方法，语义 = 「当前最上面那扇**就是**弹窗时才关」
        /// ⇒ 玩家自己开的窗不受影响）。
        /// ⚠️ 批处理 / 自检里没有窗口系统 ⇒ 只记一笔调用次数，那句日志照打（⛔ 不静默）。</summary>
        public static void HideNoticePopup()
        {
            HidePopupCallsForTest++;
            var wm = WindowsManager.Instance;
            if (wm == null)
            {
                Debug.Log("[Net] 撤窗：「等他回来」那扇提示窗 —— 本轮**没有窗口系统**（批处理/自检？）"
                        + "⇒ 不真关（生产路径那一刻关的是它）");
                return;
            }
            wm.HidePopUp();
        }

        /// <summary>自检读：<see cref="HideNoticePopup"/> 被调过几次。
        /// 🔴 它记的是「**那一刻真的去撤窗了没有**」，**不是**「窗真关掉了」——
        /// 后者只有真 Play 验得到（批处理里没有 `WindowsManager`）。</summary>
        public static int HidePopupCallsForTest { get; private set; }

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
            // 🆕 2026-10-17（B23·A902）：换会话 ⇒ 接线跟着换（`WireLobby` 自己会把旧的那台摘掉）
            NetMatchmaking.WireLobby(Session);
            return Session;
        }

        /// <summary>🆕 2026-10-03 **自检用**：把这台会话换成给定的那一台（**与真 Play 走的是同一个字段**）。
        /// 为什么需要它：`NetMatchmaking`（匹配/取消那一条链）只认 `NetRuntime.Instance.Session`，
        /// 而自检里那两台会话是**手工建的**（`NetSelfTest` 的 host/cli 对）。
        /// ⚠️ **用完必须换回去**（`AttachForTest(keep)`）—— 否则后面的用例会接着用这台。
        /// 判据 → `资料/联机P2P_设计与交接.md`（`Cancel` 那一条：大厅阶段可取消、开局后不可）。
        /// 🆕 2026-10-17（B23·A902）：**顺带把大厅掉线那条接线也挪过来** —— 换会话不挪接线的话，
        /// 自检验的就不是生产那条路了（生产上换会话只有 `Reset`，那条也挂了）。</summary>
        public void AttachForTest(NetSession s) { Session = s; NetMatchmaking.WireLobby(s); }
    }
}
