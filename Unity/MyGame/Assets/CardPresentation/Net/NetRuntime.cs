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
                // 🔴 **2026-10-19（P6 · 双语）**：钮上的字走词条 `MainMenu/General/OK`（原版那颗钮的键，
                //    表里那条）。⚠️ **中文档从「知道了」变成「确定」** —— 那是词条 ZH 列的措辞
                //    （原版键的英文列 = `OK`），如实标出这处**玩家可见的变化**。
                if (wm != null) wm.ShowPopUp(t, Loc.T("MainMenu/General/OK"), null);
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
            wm.ShowPopUp(text, Loc.T("MainMenu/General/OK"), null);
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

        // ============================================================ 🆕 2026-10-26（批次 6 · G15）
        /// <summary>原版「对手打岔」那扇窗（`BattleErrorUIManager.OpponentDesyncCancelMatch`：
        /// `WindowsManager.ShowPopUp(EnemySyncError, localizeTexts:false, closeOnEsc:false, SubmitButton, onSubmit)`）。
        ///
        /// <para>🔴 **必须走带回调的那个 `ShowPopUp` 重载**（`Shell/WindowsManager.cs` 的
        /// `ShowPopUp(text, okText, onOk, …)`）—— 原版那一下**点了确定才** `BattleManager.CancelMatch(MatchWon)`；
        /// 用 `Notice`（队列那条）会把「判胜负」这一跳丢掉（那正是 G7 §⑥·② 第 7 条点名的缺口）。</para>
        ///
        /// <para>⚠️ **批处理 / 自检没有窗口系统**（`WindowsManager.Instance == null`）⇒ **不能等人点**：
        /// 记一条 `LogWarning` 之后**立刻按「点了确定」处理**（否则自检里这一局永远挂着、断言也读不到结果）。
        /// ⚠️ 真 Play 有窗口 ⇒ 走原版那条路（等玩家点）。</para>
        /// <para>两条文案都用**原版词条**：正文 `Battle/EnemySyncError`、钮 `MainMenu/General/OK`
        /// （= 原版的 `EnemySyncError` / `SubmitButton` 两个 `LocalizedString` 字段，见 `Core/Loc.cs` 那两行）。</para></summary>
        public static void ShowSyncErrorPopup(System.Action onSubmit)
        {
            SyncErrorPopupsShownForTest++;
            var wm = WindowsManager.Instance;
            if (wm != null)
            {
                wm.ShowPopUp(Loc.T("Battle/EnemySyncError"), Loc.T("MainMenu/General/OK"), onSubmit);
                return;
            }
            Debug.LogWarning("[Net] 要弹「对手打岔」那扇窗，但这一帧**没有 `WindowsManager`**（批处理 / 自检）"
                           + " ⇒ 不等点击，**立刻**按「点了确定」处理（生产路径上这一步是玩家点 `OK`）");
            if (onSubmit != null) onSubmit();
        }

        /// <summary>自检读：<see cref="ShowSyncErrorPopup"/> 被调过几次。</summary>
        public static int SyncErrorPopupsShownForTest { get; private set; }

        /// <summary>自检用：把上面那个计数归零（几个用例之间互相串数会看错）。</summary>
        public static void ResetSyncErrorPopupCountForTest() { SyncErrorPopupsShownForTest = 0; }

        void Update()
        {
            // 先弹通知：**它不该被 `Session == null` 那道闸挡住**（会话断了才更需要说话）
            DrainNotices();
            TickConnectingBanner();          // 🆕 2026-10-28（B89-R4b · 账 HUD 805/806）：常驻「连接中」横幅
            if (Session == null) return;
            Session.Pump();
            // 对局外（大厅/匹配阶段）的消息在这儿处理；进了对局就交给 `NetBattle.Tick`
            if (!LobbyHandled) return;
            NetMatchmaking.PumpLobby();
        }

        /// <summary>会话重建（换角色/换端口时用）。旧的那条**先收工**（会捎一句 `bye` 给对面）。</summary>
        public NetSession Reset()
        {
            // 🔴 **2026-10-19（P6d · A1038）**：传的是**词条键**（`Close` 的 `reason` 已改成「词条键」语义，
            //   见那个方法的 doc）—— 线上只发键、收侧按**它自己**的语言取词；本机状态字也走同一个键。
            // 🔴 **同一轮把措辞改成【中性】（`A1038` 顺手项，同 `NetBattle` 那条 R7 纪律）**：
            //   这一句**两端都会发** —— `Reset()` 被 `Shell/SettingsWindow` 的【保存】（主机那条）与
            //   【检查连接】（**客机**那条）都调过，而原来那一串写的是「主机重开了」
            //   ⇒ **客机这一路捎给主机的是一句语义颠倒的话**（它自己没有「主机」这个概念）。
            //   ⇒ 词条 ZH 列改成「对面重开了联机」（对**收方**恒成立：它看到的确实是「对面」重开了这一条）。
            if (Session != null)
            {
                // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：旧会话对象被换掉时，
                //   它 transport 收件箱里**还没被 `Pump()` 取走的帧**（最坏是一句迟到的 `bye`）随对象一起丢。
                //   这一档原来是静的（W57 §三 R7）⇒ 记一笔，真连时能分清「bye 没到」还是「到了但会话被换了」。
                Debug.Log("[Net] NET/BYE-R7/session-reset 换会话：旧会话（" + Session.State
                        + "）的收件箱里还没被读的帧（最坏是一句迟到的 `bye`）随旧对象一起丢弃"
                        + " —— 新会话从干净状态开始（`Reset` 的正常收尾，不是缺陷）");
                Session.Close(true, "Settings/Online/Wire/HostRestarted");
            }
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

        // ============================================================ 🆕 2026-10-28（B89-R4b · 账 HUD 805 / 806）
        //  原版 `ShowStatusWhenConnecting`：一块**常驻**的「连接中」横幅，`OnGUI` 每帧自绘
        //  （`decomp_full/ShowStatusWhenConnecting__OnGUI.c:1-80`），正文 = 基串 + `dots` 个 "."
        //  （`__GetConnectingDots.c:1-38`）。⚠️ 原版是 IMGUI 自绘 —— **我们走自己的 uGUI 叠加画布不算偏差**
        //  （账里已经这么判：差的是「常驻」与「节奏」，不是「用不用 OnGUI」）。
        //
        //  🔴 **⛔ 不走 `Notice` 队列** —— 那条是「**新弹一条**」的语义，每秒入一次队会把通知队撑爆
        //     （同一条理由见上面 `UpdateReconnectPopup` 的 doc）。
        //
        //  ── 数值（本轮 `工具/read_literal.py` 4B float 档实读；⚠️ 跑它要先 `export PYTHONIOENCODING=utf-8`）──
        //   · `0x1834b3270 = 400`  · `0x1834b2e08 = 100`  → 横幅盒子居中：`((W−400)/2, (H−100)/2, 400, 100)`
        //   · `0x1834b2e8c = 3`    · `0x1834b2df0 = 4`    → `dots = floor(Mathf.Repeat(t × 3, 4))`
        //     ⇒ **0/1/2/3 循环、每 1/3 秒加一个点**（⚠️ 账里原来写的「每秒加一个点」**不准**，本轮订正）。
        //
        //  🔶 自己设计（非原版）：**字号 / 颜色 / 文字在盒子里的对齐** —— 原版是 IMGUI 默认 skin ⇒ 无像素判据
        //   · 基串**不是**自设计：原版硬编码英文 `"Connecting"`（`cpp2il/out_cpp2il_dev_ilspy/ShowStatusWhenConnecting.cs:48`
        //     `string text3 = "Connecting" + text;`；⚠️ 2026-10-28 `B89_RXb` 订正：R4b 曾取
        //     `Loc.T("MenuLogin/Intro/Connecting")` 是**猜的**，原版不本地化这一串）。
        //   · 字号/颜色/对齐：原版是 IMGUI 默认 skin ⇒ 无像素判据 ⇒ 我们挑（28 px / 白 / 居中）。
        //
        //  🔶 自己设计（非原版）：**显示条件与第二行** —— 原版 `OnGUI` 尾部 `if (PhotonNetwork.inRoom) enabled = false;`
        //     （`…ShowStatusWhenConnecting.cs:60-63`）⇒ 语义 = **未进房就一直亮、进房即灭**；我们无 Photon，按 `NetState`
        //     映射（`ShouldShowConnectingBanner`）；第二行 `Status: <ClientState>`（`:56-58`）改印我们的 `NetState`。

        /// <summary>原版 `0x1834b3270`（4B float 实读）= **400**。</summary>
        public const float ConnectingBannerW = 400f;
        /// <summary>原版 `0x1834b2e08`（4B float 实读）= **100**。</summary>
        public const float ConnectingBannerH = 100f;
        /// <summary>原版 `0x1834b2e8c`（4B float 实读）= **3**（每秒加 3 个点的倍率）。</summary>
        public const float ConnectingDotsRate = 3f;
        /// <summary>原版 `0x1834b2df0`（4B float 实读）= **4**（点数的循环长度）。</summary>
        public const float ConnectingDotsPeriod = 4f;

        /// <summary>原版 `GetConnectingDots` 的**点数**：`floor(Mathf.Repeat(t × 3, 4))` ∈ {0,1,2,3}。
        /// <para>⚠️ 只算「加几个点」，不含基串 —— 基串是**复刻**（`"Connecting"`，见上面那段）。</para></summary>
        public static int ConnectingDotsForTest(float secondsSinceLoad)
        {
            return Mathf.FloorToInt(Mathf.Repeat(secondsSinceLoad * ConnectingDotsRate, ConnectingDotsPeriod));
        }

        /// <summary>横幅正文 = 基串 + `dots` 个 "."（原版 `String.Concat(base, ".", …)` 逐次拼）。</summary>
        public static string ConnectingBannerTextForTest(float secondsSinceLoad)
        {
            int n = ConnectingDotsForTest(secondsSinceLoad);
            if (n <= 0) return ConnectingBaseString;
            var sb = new System.Text.StringBuilder(ConnectingBaseString);
            for (int i = 0; i < n; i++) sb.Append(" .");   // 原版每次拼 `" ."`（`DAT_1842c63a8`，ILSpy `ShowStatusWhenConnecting.cs:22`）；⚠️ 2026-10-28 B89-E11e 订正：原来是 `'.'`（少一个空格）
            return sb.ToString();
        }

        /// <summary>基串 = 原版**硬编码英文** `"Connecting"`（`cpp2il/out_cpp2il_dev_ilspy/ShowStatusWhenConnecting.cs:48`）。⚠️ 2026-10-28 B89-E11e 订正：R4b 原来取 `Loc.T("MenuLogin/Intro/Connecting")`（猜的）—— **原版不本地化这一串**（`B89_RXb` 把出处由 `:32` 改成实读的 `:48`）。</summary>
        const string ConnectingBaseString = "Connecting";

        /// <summary>🔶 自己设计（非原版）：字号 28 px（原版 IMGUI 默认 skin，无像素判据）。</summary>
        const int ConnectingBannerFontScale = 28;

        /// <summary>原版 `OnGUI` 的 `GUILayout.BeginArea` 矩形（**GUI 口径：y 向下**）：
        /// `((Screen.width − 400) × 0.5, (Screen.height − 100) × 0.5, 400, 100)`。</summary>
        public static Rect ConnectingBannerRectForTest(int screenW, int screenH)
        {
            return new Rect((screenW - ConnectingBannerW) * 0.5f,
                            (screenH - ConnectingBannerH) * 0.5f,
                            ConnectingBannerW, ConnectingBannerH);
        }

        /// <summary>🔶 自己设计（非原版）：显示条件的 `NetState` 映射 —— 原版 `OnGUI` 尾 `if (PhotonNetwork.inRoom) enabled = false;`（`…ShowStatusWhenConnecting.cs:60-63`）⇒ 未进房亮、进房灭。</summary>
        public static bool ShouldShowConnectingBanner(NetState st)
        {
            return st == NetState.Connecting || st == NetState.Handshaking || st == NetState.Lobby || st == NetState.Listening;
        }

        Canvas _bannerCanvas;
        Label _bannerLabel;
        Label _statusLabel;      // 原版第二行 `Status: <ClientState>`（`…ShowStatusWhenConnecting.cs:56-58`）—— 两条标签，⛔ 不用 `\n`（点阵后端不认换行）
        bool _bannerWarned;

        /// <summary>自检读：横幅**这一刻**是不是挂着（批处理里没有帧循环 ⇒ 由用例显式调
        /// <see cref="TickConnectingBannerForTest"/> 推一次再读）。</summary>
        public bool ConnectingBannerVisibleForTest { get { return _bannerLabel != null && _bannerLabel.gameObject.activeSelf; } }

        /// <summary>自检用：把「连接中横幅」这一拍按给定状态推一次（**与 `Update` 走同一份代码**）。</summary>
        public void TickConnectingBannerForTest(NetState st, float secondsSinceLoad)
        {
            ApplyConnectingBanner(st, secondsSinceLoad);
        }

        /// <summary>`Update` 每帧调一次的那一跳（⛔ 别在别处再调一份）。</summary>
        void TickConnectingBanner()
        {
            var st = Session != null ? Session.State : NetState.Off;
            ApplyConnectingBanner(st, Time.timeSinceLevelLoad);
        }

        void ApplyConnectingBanner(NetState st, float secondsSinceLoad)
        {
            bool on = ShouldShowConnectingBanner(st);
            if (!on)
            {
                if (_bannerLabel != null && _bannerLabel.gameObject.activeSelf) _bannerLabel.gameObject.SetActive(false);
                if (_statusLabel != null && _statusLabel.gameObject.activeSelf) _statusLabel.gameObject.SetActive(false);
                return;
            }
            if (!EnsureBanner()) return;
            _bannerLabel.gameObject.SetActive(true);
            _bannerLabel.SetText(ConnectingBannerTextForTest(secondsSinceLoad));
            // 第二行 = 原版 `Status: <ClientState>`；枚举无 Photon ⇒ 印我们的 `NetState`（🔶 映射见上方长注）
            if (_statusLabel != null)
            {
                _statusLabel.gameObject.SetActive(true);
                _statusLabel.SetText("Status: " + st);
            }
        }

        /// <summary>懒建那块**独立叠加画布**（`ScreenSpaceOverlay`，⛔ 不挂进壳的画布 —— 这个宿主
        /// `DontDestroyOnLoad`，跟着场景走会一起没掉）。
        /// <para>⚠️ `Label.CreateUgui` 需要画布祖先才画得出来 ⇒ 画布必须建在**它自己的 GameObject 上**。</para></summary>
        bool EnsureBanner()
        {
            if (_bannerLabel != null) return true;
            try
            {
                var go = new GameObject("Net Connecting Banner", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                _bannerCanvas = go.AddComponent<Canvas>();
                _bannerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _bannerCanvas.sortingOrder = 30000;      // 🔶 自己设计（非原版）：压在所有窗口之上（原版 IMGUI 恒在最上层）
                var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
                // 盒子居中 ⇒ 文字锚在画布中心、偏移 0（🔶 自己设计（非原版）：原版是 GUILayout 区内的默认左上对齐）
                _bannerLabel = Label.CreateUgui(go.transform, "", Vector3.zero,
                                                ConnectingBannerFontScale, Color.white,
                                                new Vector2(0.5f, 0.5f), "connecting_label");
                _bannerLabel.gameObject.SetActive(false);
                // 第二行（原版两条 `GUILayout.Label`）：摆在正文下方 24 设计 px
                _statusLabel = Label.CreateUgui(go.transform, "", new Vector3(0f, -24f, 0f),
                                                ConnectingBannerFontScale, Color.white,
                                                new Vector2(0.5f, 0.5f), "connecting_status");
                _statusLabel.gameObject.SetActive(false);
                return true;
            }
            catch (System.Exception ex)
            {
                if (!_bannerWarned)
                {
                    _bannerWarned = true;
                    Debug.LogWarning("[Net] 「连接中」横幅建不出来（" + ex.GetType().Name + "：" + ex.Message
                                   + "）⇒ 这一档只剩状态字（`Settings/Online/St/Connecting`），⛔ 不静默");
                }
                return false;
            }
        }

        // ============================================================ 🆕 2026-10-29（B94 · Y6 · 账 轮抽:30 / 补读:259）
        //  原版 `DraftEventBase<TS,TD>.SetCustomMatchOptions(RoomOptions options, int currentELO)` 的**本地等价**。
        //
        //  真体逐句（`D:/2/tools/decomp_generic_trial/out_b94/DraftEventBase_object__object_.SetCustomMatchOptions__1813cb0.c`
        //  + ILSpy `DraftEventBase.cs:4432`）：
        //    base.SetCustomMatchOptions(options, currentELO);              // 基类 = 空体（0x4B33B0）
        //    int won  = SaveGame.numberOfMatchesWon(+0x48);               // SaveGame = this+0x20；+0x48 = 胜场
        //    int ls   = LiveOpsManager.GetHandler<GameModes>().LossStreak;
        //    if (currentELO <= GameStaticData.newbieELO && won < 3) currentELO = 0;   // ★ 新手档清零
        //    options.CustomRoomProperties["C1"] = won;                    // ★ 胜场
        //    options.CustomRoomProperties["C4"] = currentELO;             // ★ （可能清零后的）ELO
        //    options.CustomRoomProperties["C5"] = ls;                     // ★ 连败
        //    options.CustomRoomPropertiesForLobby = new[]{"C1","C2","C3","C4","C5"};  // ★ 恰好 5 个键
        //
        //  3 个键的字符串（C1/C4/C5）与 5 键 lobby 表 = ILSpy `DraftEventBase.cs:4456/4465` 字面量逐字。
        //  ⚠️ **我们 Net 层没有 Photon `RoomOptions` / `Hashtable`**（自建 TCP，房参数是另一套）⇒
        //     这一跳落成**纯函数**：算出「该写哪三格 + 值 + lobby 键表」，由调用方套到自己的房间参数上
        //     （⛔ 不发明一个假的 RoomOptions）。判据见上。

        /// <summary>原版 `GameStaticData.newbieELO`（ILSpy `GameStaticData.cs:2424` 的 `.cctor` 写死 **800**；
        /// 真体里是 `GameStaticData +0x264` 那个静态 int）。✅ 2026-10-29（B97_W42）：`GameStaticData.newbieELO`（`Shell/GameStaticData.cs:272`）已落地 ⇒ **改指过去**，
        /// 编译期折叠、行为零变化；⛔ 别在两处各存一份不同值（结构上已不可能分叉，`B97W39_Assert` D2 仍钉值相等）。</summary>
        public const int DraftNewbieElo = global::CardPresentation.GameStaticData.newbieELO;

        /// <summary>原版 `DraftEventBase.SetCustomMatchOptions` 算出的「房间属性」三格 + lobby 键表。</summary>
        public struct DraftMatchOptions
        {
            /// <summary>"C1" = 胜场（`SaveGame.numberOfMatchesWon`）。</summary>
            public int Won;
            /// <summary>"C4" = ELO（新手档 `<= 800 && won < 3` 时被清成 0）。</summary>
            public int Elo;
            /// <summary>"C5" = 连败（`GameModes.LossStreak`）。</summary>
            public int LossStreak;
            /// <summary>`CustomRoomPropertiesForLobby` = `{ "C1", "C2", "C3", "C4", "C5" }`（原版恰好 5 个键）。</summary>
            public string[] LobbyKeys;
        }

        /// <summary><see cref="DraftMatchOptions"/> 的纯函数版（原版 `options.CustomRoomProperties[...] = ...` 的等价）。
        /// `won` 传 `SaveGame.numberOfMatchesWon`、`lossStreak` 传 `GameModes.LossStreak`、`currentELO` 传匹配 ELO。</summary>
        public static DraftMatchOptions ComputeDraftMatchOptions(int currentELO, int won, int lossStreak)
        {
            int elo = currentELO;
            if (elo <= DraftNewbieElo && won < 3) elo = 0;   // ★ 原版：新手档（ELO ≤ 800）且胜场 < 3 ⇒ ELO 置 0
            return new DraftMatchOptions
            {
                Won = won,
                Elo = elo,
                LossStreak = lossStreak,
                LobbyKeys = new[] { "C1", "C2", "C3", "C4", "C5" },
            };
        }
    }
}
