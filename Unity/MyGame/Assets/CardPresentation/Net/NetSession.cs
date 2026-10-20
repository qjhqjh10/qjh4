// NetSession.cs — 联机会话：**握手 · 心跳 · 掉线检测 · 重连**（不含对局逻辑，那在 `NetBattle`）
//
// 状态机（`NetState`）：
//   Host  ：Off → Listening →（有人连进来）Handshaking → Lobby → InBattle
//   Client：Off → Connecting →（拿到 challenge）Handshaking → Lobby → InBattle
//   任一边掉线且在对局中 ⇒ WaitingReconnect（**不判负** —— 用户 2026-09-26 要重连）→ 追平后回 InBattle
//
// 🔴 **重连的口径**（正本 §5·6）：**主机持有权威动作流**，客机重连 = **从种子重建 + 全量重放**。
//    本文件只负责「把 `resume` 送到、把上层回调叫起来」；重放本身由 `NetBattle` 做。
//
// 🔴 **所有时间都走 `Clock`（单调毫秒），不累加 `Time.deltaTime`** ——
//    批处理下没有帧循环，自检里靠 `Pump()` 显式推进（见 `NetSelfTest`）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation.Net
{
    public class NetSession
    {
        // ---- 常量（改这里 = 改行为，正本 §二 有对应说明）----
        public const int HeartbeatMs = 1000;        // 心跳间隔
        public const int SilentTimeoutMs = 10000;   // 多久没收到对面的任何包 = 掉线
        public const int ReconnectEveryMs = 2000;   // 客机重连尝试间隔
        public const int ConnectTimeoutMs = 4000;   // 单次连接超时
        /// <summary>🆕 🔴 **2026-10-20（W58-S6FIX）**：**待替换候选**从被接受到凭据齐了的最长等待。
        /// 过期 ⇒ 关候选、**旧连接原样保留**（`NET/BYE-S6/replacement-timeout`）。
        /// 取值理由：正常凭据往返是**毫秒级**（同机/局域网），6 秒足够容忍一次丢包重传；
        /// 又不至于让「候选位被占着」把客机的下一次重试（`ReconnectEveryMs` = 2 秒）拖太久。
        /// ⚠️ 这是**我们自拟的**边界值，原版没有对应参数（原版走 Photon 的重连，判据不在本地）。</summary>
        public const int ReplacementHandshakeTimeoutMs = 6000;

        public NetRole Role { get; private set; }
        public NetState State { get; private set; }
        /// <summary>给界面看的一句话（**任何状态变化都要更新它** —— 红线：不许静默）。</summary>
        public string StatusText { get; private set; }
        public string LastError { get; private set; }
        public string PeerName { get; private set; }
        /// <summary>主机发给客机的对局钥匙（重连凭它认人）。⚠️ **不进日志**。</summary>
        public string SessionToken { get; private set; }

        public INetTransport Transport { get { return _t; } }
        /// <summary>握手过了以后，上层从这取消息（`Pump` 会把非会话类的帧放进来）。</summary>
        public readonly List<NetFrame> Inbox = new List<NetFrame>();

        // ---- 界面/上层接的回调 ----
        /// <summary>客机【检查连接】的结果：成功？+ 人话理由。</summary>
        public Action<bool, string> OnCheckDone;
        /// <summary>握手通过（主机：客机来了；客机：连上了）。</summary>
        public Action OnPeerReady;
        /// <summary>对局中掉线（已进入等待重连）。</summary>
        public Action OnPeerLost;
        /// <summary>重连成功（`resume` 已到）。参数 = 上层要做的那次「全量重放」。
        /// 🔴 记着：**先丢本地状态再重放**，别增量补（正本 §5·6 第 4 条）。</summary>
        public Action<MsgStart, List<MsgAction>> OnResumed;
        /// <summary>对面主动退出 / 会话结束。理由是人话。</summary>
        public Action<string> OnClosed;

        INetTransport _t;
        readonly List<NetFrame> _buf = new List<NetFrame>();
        long _lastRecvMs, _lastPingMs, _lastReconnMs;
        string _nonce;
        bool _checkMode;            // true = 这次连接是「检查连接」发起的（结果要回报）
        bool _wasInBattle;          // 掉线前是否在对局中（决定要不要重连）
        int _lastSeq;
        /// <summary>开台那一刻的密码（**不在核对时读全局设置**，见 `StartHost` 的注释）。</summary>
        string _hostPassword = "";
        /// <summary>已经发过握手包的那条连接的序号（边沿触发用，见 `INetTransport.AcceptedCount`）。</summary>
        int _lastAccepted = -1;

        // ---- 🆕 🔴 **2026-10-20（W58-S6FIX · 缺陷 S6）：待替换候选的状态**（主机侧）----
        // 只在 `IPendingReplacementHost` 上跑（真 TCP）。候选的帧走**独立队列**，凭据没过之前
        // 它说的话**不能**被当成当前对家说的话 —— 这正是「陌生连接踢不掉人」的实现边界。
        readonly List<NetFrame> _pendBuf = new List<NetFrame>();
        int _pendingGen = -1;          // 已经发过 Challenge 的那条候选代次
        string _pendingNonce;          // 专门给候选的那次 nonce（与当前连接的 `_nonce` 分开）
        string _pendingPeerName;       // 候选在 `Proof` 里报的名字（钳过，`Proof` 通过才用）
        bool _pendingProofOk;          // 候选已经过了密码/版本那一关，等 `Reconnect{sessionToken}`

        /// <summary>🔴 **自检可以把它换掉**（不换就得**真睡 10 秒**才验得到静默超时）——
        /// `SetClockForTest(f)`，传 `null` 恢复成真实秒表。**生产路径一处都不调它**
        /// （先例 = `NetBattle.SetClockForTest`）。⚠️ 它是**进程级**的：换完记得还回来。</summary>
        public static void SetClockForTest(Func<long> now) { _nowOverride = now; }
        static Func<long> _nowOverride;
        static long NowMs { get { return _nowOverride != null ? _nowOverride() : _sw.ElapsedMilliseconds; } }
        static readonly System.Diagnostics.Stopwatch _sw = System.Diagnostics.Stopwatch.StartNew();

        /// <summary>自检读口：**距上一次「收到对面的包 / 接受一条新连接」过去了多少毫秒**。
        /// 🔴 主机开台那一刻这条基线**必须已经设好**（`StartHost` 里那句 `_lastRecvMs = NowMs;`）——
        ///    否则进程起来满 `SilentTimeoutMs` 之后，主机会拿**陈旧基线**（字段默认 0）去比 `now`，
        ///    在**一条包都没见过**的情况下判自己「10 秒没收到对面的任何消息」
        ///    （判据链 → `Pump()` 里「主机：有新连接进来就握手」那一段）。</summary>
        public long SinceLastRecvMsForTest { get { return NowMs - _lastRecvMs; } }

        /// <summary>🆕 自检读口：**掉线之前是不是在对局中**（== 要不要走客机那条 2 秒重连退避）。
        /// 🔴 账 `A914` 钉的正是它：判负倒计时**到点之后**它必须已经是 `false` ——
        /// 否则客机会**每 2 秒真去 `Connect` 一次、永不停止**（`Pump()` 里那一支的判据就是这个字段），
        /// 连上还会把状态字改成「连上了，正在补上这一局的进度…」（玩家刚被告知判负，转头字就变了）。</summary>
        public bool WasInBattleForTest { get { return _wasInBattle; } }

        /// <summary>🆕 🔴 **A961（2026-10-18）**：把**对端可控**的文本钳到
        /// <see cref="NetProtocol.MaxPeerTextChars"/>（超出 = 截断 + 一个 `…`）。
        /// 口径、取值判据与「这是我们自拟的、原版没有先例」那条**全文** →
        /// `NetProtocol.MaxPeerTextChars` 的注释。⛔ 只钳**对面发来的**字符串，
        /// **别拿它去钳我们自己写的文案**（自己写的那些本来就有出处、也没上限问题）。</summary>
        public static string ClampPeerText(string s, int maxChars = NetProtocol.MaxPeerTextChars)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= maxChars) return s;
            return s.Substring(0, maxChars) + "…";
        }

        public NetSession(INetTransport transport)
        {
            _t = transport;
            Role = NetRole.Off;
            State = NetState.Off;
            StatusText = Loc.T("Settings/Online/St/Off");
        }

        public static NetSession NewTcp() { return new NetSession(new TcpTransport()); }

        /// <summary>真 Play 时窗口失焦会暂停 `Update` ⇒ 收包也会停。外壳那边调一次这个。
        /// 🔴 **批处理下直接返回** —— 在编辑器里设 `Application.runInBackground` 会**把
        /// `ProjectSettings.asset` 写脏**（2026-09-26 实测：跑一次自检 `runInBackground` 就从 0 变成 1），
        /// 而**自检不该改工程设置**。</summary>
        public static void EnsureBackground()
        {
            if (Application.isBatchMode) return;
            Application.runInBackground = true;
        }

        // ==================================================================
        //  主机
        // ==================================================================

        /// <summary>按【保存】：开始监听（**端口占用之类的错会写进 `LastError`**，别静默）。</summary>
        public bool StartHost(NetConfigData cfg)
        {
            Close(false);
            Role = NetRole.Host;
            // 🔴 **密码在开台这一刻定格** —— 别在核对时去读 `NetConfig.Current`：
            //    那是个全局单例，中途改设置（或像自检那样同进程开两桌）会让**已经开打的那一桌换密码**。
            _hostPassword = cfg.password ?? "";
            _t.Listen(cfg.port);
            if (!_t.IsListening)
            {
                LastError = _t.LastError;
                SetState(NetState.Closed, string.Format(Loc.T("Settings/Online/St/HostFailed"), LastError));
                return false;
            }
            _lastAccepted = _t.AcceptedCount;
            // 🔴 **2026-10-17（F4·RC1）**：开台这一刻把**静默基线**也设上（与客机 `InternalConnect` 那句对称）。
            //   原来不设 ⇒ `_lastRecvMs` 停在字段默认值 0，而 `NowMs` 是**进程级**秒表 ⇒
            //   进程起来满 `SilentTimeoutMs` 之后，主机拿 0 去比 `now`，会把**刚连进来、一条包都还没发的客机**
            //   判成「10 秒没收到对面的任何消息」（判据链与日志实证 → `资料/普查产出_1017/D4_联机诊断.md` §②）。
            _lastRecvMs = NowMs;
            SessionToken = NewToken();
            SetState(NetState.Listening, string.Format(Loc.T("Settings/Online/St/Listening"), _t.Port));
            // 🆕 2026-09-27（用户拍板做 B 档）：**顺手向路由器要一条入站映射**。
            //    为什么放在这儿：这是**唯一**一个「本机确定要当主机、端口已经定下来」的时刻。
            //    ⚠️ 它**全在后台线程**跑（SSDP 要等 2.5 秒），**不挡这一句返回**；
            //    成没成、为什么没成，由 `UpnpPortMapper` 自己走 `NetRuntime.Notice` 告诉玩家（红线）。
            //    ⚠️ **批处理里不跑**（自检会反复开台 ⇒ 会反复改玩家路由器的映射表，见那个方法的注释）。
            UpnpPortMapper.MapAsync(_t.Port);
            return true;
        }

        // ==================================================================
        //  客机
        // ==================================================================

        /// <summary>【检查连接】：连 + 握手，**成功后保持连接**（接着就能开战）。结果走 `OnCheckDone`。</summary>
        public void CheckConnection(NetConfigData cfg)
        {
            Close(false);
            Role = NetRole.Client;
            _checkMode = true;
            InternalConnect(cfg);
        }

        /// <summary>直接连（不做检查那一步，用于「已经配好了，开打」）。</summary>
        public void Connect(NetConfigData cfg)
        {
            Close(false);
            Role = NetRole.Client;
            _checkMode = false;
            InternalConnect(cfg);
        }

        void InternalConnect(NetConfigData cfg)
        {
            SetState(NetState.Connecting, string.Format(Loc.T("Settings/Online/St/Connecting"), cfg.ip, cfg.port));
            if (_t.Connect(cfg.ip, cfg.port, ConnectTimeoutMs))
            {
                _lastRecvMs = NowMs;
                SetState(NetState.Handshaking, Loc.T("Settings/Online/St/Handshaking"));
            }
            else
            {
                LastError = _t.LastError;
                SetState(NetState.Closed, LastError);
                if (_checkMode && OnCheckDone != null) OnCheckDone(false, LastError);
            }
        }

        // ==================================================================
        //  跑（**主线程每帧 / 自检里显式调**）
        // ==================================================================

        public void Pump()
        {
            _buf.Clear();
            _t.Pump(_buf);
            for (int i = 0; i < _buf.Count; i++)
            {
                try { Handle(_buf[i]); }
                catch (Exception e)
                {
                    // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：`Handle` 抛异常时，
                    //   原来这**一帧和队列里剩下的帧**都随异常一起没了（`Pump` 当场中断），而且只有
                    //   Unity 的通用异常日志、不说是哪条消息。⛔ 这里**照旧 rethrow**（不改行为），
                    //   只把「丢了几帧、是哪一条」补进日志。
                    Debug.LogError("[Net] NET/BYE-R1/handle-threw 处理 `" + _buf[i].kind + "` 时抛异常 ⇒ 本帧**和队列里剩下的 "
                                 + (_buf.Count - i - 1) + " 帧**都不会被处理（`Pump` 在本帧中断）；异常原文：" + e);
                    throw;
                }
            }
            _buf.Clear();

            long now = NowMs;

            // ---- 掉线检测（对局中才进等待重连；没开打就直接结束）----
            // 🔴 **A1269（2026-10-19）**：这一道闸原来**只挡 `Closed` / `WaitingReconnect`、不挡 `Off`**
            //    ⇒ `Close()` 之后再被 `Pump` 一次（而 `_t.PeerLost` **还**是真 —— `ClosePeer` 只清连接位、
            //    不清 `_peerLost`）就会**再报一次** `OnPeerLost`，而且状态从 `Off` **倒回** `WaitingReconnect`。
            //    `Off` = 「还没开台 / 已经收工」（`NetSession.Close` 之后就是它）——
            //    从一个**终态**倒回「等重连」**逻辑上就是错的**，原版也没有这一跳。
            //    ⚠️ 生产路径现在只有一处 `Pump`（`NetRuntime.Update`），所以它平时不现形；
            //      但这是一条「**谁再 `Pump` 一次就现形**」的边（`F5` §七·3 就是这么记的），
            //      而且 `NetBattleTest` 里本来就有一处**手工再推一次**的写法。
            //    ⛔ 别再把这半句删回去 —— `NetSelfTest` §R 的 R⑨~R⑫ 钉住它。
            if (_t.PeerLost && State != NetState.Closed && State != NetState.WaitingReconnect
                && State != NetState.Off)
            {
                _wasInBattle = State == NetState.InBattle;
                string why = _t.LastError;
                SetState(NetState.WaitingReconnect,
                         (_wasInBattle ? Loc.T("Settings/Online/St/PeerLostInBattle")
                                       : string.Format(Loc.T("Settings/Online/St/Disconnected"), why)));
                LastError = why;
                if (OnPeerLost != null) OnPeerLost();
            }

            if (State == NetState.Closed || State == NetState.Off) return;

            // ---- 🆕 🔴 **2026-10-20（W58-S6FIX · S6）：主机已有活连接时进来的新连接** ----
            //   旧行为：传输层直接关掉（客机重连只看到「连上了又断」）。现在先收作**候选**，
            //   在这里走凭据（`Challenge → Proof → Reconnect{sessionToken}`）；凭据齐了才提升。
            //   ⚠️ 必须跑在下面「静默超时」判据**之前**吗？不需要 —— 候选提升会刷新 `_lastRecvMs`，
            //      而旧连接该超时照旧超时（它本来就可能已经死了）。放在最前面只是为了**尽早**处理。
            PumpReplacement();

            // ---- 主机：有新连接进来就握手（**边沿触发**：看 `AcceptedCount` 变没变，
            //      别看 `IsConnected` —— 那是电平，会在等重连期间**反复重发握手包**）----
            // 🔴 **2026-10-17（F4·RC1）：这一支必须跑在下面那道【静默超时】判【之前】**（原来是反的）。
            //   它干的事里有一句「刷新静默基线」：`_lastRecvMs = now` —— 而下面那道判据正是拿
            //   `now - _lastRecvMs` 去判「对面是不是没了」。接受线程（`NetTransport.Setup`，它把
            //   `_connected = true` 与 `AcceptedCount++` 一起置上）与主线程 `Pump()` 是**并发**的：
            //   只要这一步落在「超时判完 / 这一支还没跑」的窗口里，主机就会拿**旧基线**去判一条
            //   **刚连上、一条包都还没发**的连接（客机的第一个包要等主机发的 `Challenge`）。
            //   日志实证（`netbattle.log:5674-5676`）：`Handshaking：连上了…` →
            //   `WaitingReconnect：连接断了：10 秒没收到对面的任何消息` → **`Handshaking：有客机连进来了`**
            //   —— 这个**倒退**只可能出自同一个 `Pump()`（掐完又握手，而 `Challenge` 已经发不出去 ⇒ 永远握不上）。
            //   ⚠️ 挪到这儿**不会**把「真的对面掉线」那条路堵死：真掉了就是**没有包来刷新** `_lastRecvMs`，
            //      超时照旧成立（自检里那条对照就是钉这一点的）。
            if (Role == NetRole.Host && _t.AcceptedCount != _lastAccepted)
            {
                // ⛔ **别把下面这一句挪到 `Send` 成功之后**（R3 报告 §4 建议过；2026-10-18 主对话已裁「不挪」）：
                //    ① 实测会变成**每帧重试** —— 连接断着而 `AcceptedCount` 已经变过时，这一支**每帧都进**
                //       （判据一直是 `不等于`）⇒ 每帧一次 `NewToken()`（加密随机数 + 十六进制串）
                //       与一次 `SetState()`（每次都 `Debug.Log`），**一直刷到下一次连接进来为止**；
                //    ② 这一句正是 `A943` 读侧确认（下面 `if (_t.IsConnected && (Role != NetRole.Host ||`
                //       `_t.AcceptedCount == _lastAccepted) …)` 那半句）依赖的那一处 —— 动它要重走整段 §P。
                //    ⇒ 要「边沿不被吃掉」请走**别的**路（例如给这一支加 `&& _t.IsConnected`），**别挪这一句**。
                _lastAccepted = _t.AcceptedCount;
                _lastRecvMs = now;
                bool wasReconnect = _wasInBattle;
                _nonce = NewToken();
                // 🔴 **2026-10-18（账：「`Send()` 静默丢」）**：这一句原来是**放空**的。
                //   它丢掉的后果比别处重 —— 这一支的**边沿在上一句就已经消费掉了**（它按 `AcceptedCount`
                //   变没变触发）⇒ 这条连接**这一辈子不会再发第二个 `Challenge`**，对面永远握不上手。
                //   ⚠️ 真发生的那一刻连接**已经断了**（`Send` 只在 `!_t.IsConnected` 时丢，而 A943 里
                //      连接位与计数由同一次 CAS 发布 ⇒ 计数动了就说明那一下是活的）⇒ 它不是「永久停摆」，
                //      R3 报告 §5 已逐帧推演过。**本笔只补「出声」**；「把 `_lastAccepted` 挪到发送成功之后」
                //      那条建议**没做**，理由写在下面 `Send()` 的注释里（会变成每帧重试）。
                if (!Send(NetKind.Challenge, new MsgChallenge { nonce = _nonce }))
                    Debug.LogWarning("[Net] ⚠️ 握手 `Challenge` **没发出去**，而这一支的边沿（上面那句 "
                                   + "`_lastAccepted = …`）**已经消费掉了** ⇒ 这条连接不会再收到第二个 `Challenge`"
                                   + "（要等下一次连接进来）。那一刻连接确实已经断了，所以它**不是**「永久停摆」"
                                   + "（`资料/普查产出_1018/R3_联机三档现核.md` §5 逐帧推演过）—— 但**必须说出来**（红线）");
                SetState(NetState.Handshaking, wasReconnect
                         ? Loc.T("Settings/Online/St/PeerBack") : Loc.T("Settings/Online/St/PeerJoined"));
            }

            // ---- 心跳 ----
            if (State != NetState.WaitingReconnect && State != NetState.Connecting)
            {
                if (now - _lastPingMs >= HeartbeatMs)
                {
                    _lastPingMs = now;
                    Send(NetKind.Ping, new MsgPing { t = now });
                }
                // 🔴 **A943 读侧闭环（2026-10-18）**：`_t.AcceptedCount == _lastAccepted` 这半句**不能删**。
                //   两个连接事实（连接位 / 接受计数）在 `TcpTransport` 里**已经同处一个字、由一次 CAS 发布**
                //   （`_state`，见那边的注释），但**读侧是两次读**：上面那一支（`Pump` 的**握手边沿**：
                //   `Role == NetRole.Host && _t.AcceptedCount != _lastAccepted`）读计数、这里读连接位。
                //   一次新发布**夹在这两次读之间**时，这里就会拿「旧计数（⇒ 上面那支没走、`_lastRecvMs`
                //   没跟着刷新）+ 新连接位（⇒ 真）」去判静默 ⇒ **掐掉一条刚连上、一条包都还没发的好连接**
                //   （同族的日志实证见 `资料/普查产出_1017/D4_联机诊断.md` §②）。
                //   ⇒ 「计数还是我认过的那个」才说明**没有夹进来的新连接**（计数只增，且上面那支认的就是它）。
                //   🔴 `Role != NetRole.Host ||` 这半句**也不能删**：这道确认**只对主机成立** ——
                //      只有主机的计数是**后台的接受线程**加起来的（`TcpTransport.Setup` 那条路）；
                //      客机的计数由**主线程自己**在 `Connect()` 里加（`Setup` 是两边共用的一支），
                //      而 `_lastAccepted` 在客机侧**没人写**（停在 `-1`）⇒ 少了这半句，客机的静默超时
                //      会被**永久关掉**（`1 == -1` 恒假 ⇒ 对面拔网线再也判不出来）。自检 P⑫ 钉这一点。
                //   ⚠️ 不是死循环、不会饿死：`Pump()` 每帧重来，**下一帧 `Pump` 的握手边沿那一支就把边沿补上并刷新基线**
                //      （自检 §P 的 P⑤/P⑥ 钉这一点）；真有连接不停进来的话，基线本来也在被不停刷新。
                //   ⚠️ 判据仍然只此一处：`_lastAccepted` 只在 `StartHost` 与上面那支里被写。
                if (_t.IsConnected && (Role != NetRole.Host || _t.AcceptedCount == _lastAccepted)
                    && now - _lastRecvMs >= SilentTimeoutMs)
                {
                    // 静默太久 = 对面没了（拔网线这种不会触发 TCP 的 FIN）⇒ 当作掉线
                    // ⚠️ 用 `ClosePeer` 而**不是** `Close` —— 主机要把监听留着，否则等不到重连
                    _t.ClosePeer();
                    _wasInBattle = State == NetState.InBattle;
                    LastError = string.Format(Loc.T("Settings/Online/St/SilentTimeout"), SilentTimeoutMs / 1000);
                    // 🆕 2026-10-17（B23·A902）：这行字**要按在不在对局里分开说** —— 原来无条件是
                    //   「对局已暂停」，而大厅阶段（还没进对局）根本不是那么回事（**说错话** = 另一种静默）。
                    //   同一个文件上面那条（`Pump` 里 `_t.PeerLost` 那一支）本来就是这么分的 ⇒ 两处对齐。
                    SetState(NetState.WaitingReconnect,
                             _wasInBattle ? Loc.T("Settings/Online/St/PeerLostInBattle")
                                          : string.Format(Loc.T("Settings/Online/St/Disconnected"), LastError));
                    if (OnPeerLost != null) OnPeerLost();
                }
            }

            // ---- 主机「接受新连接就握手」那一支**已挪到上面**（跑在下面那道静默超时判**之前**）----
            //   🔴 2026-10-17（F4·RC1）：理由见那一段的注释 —— 它靠**刷新 `_lastRecvMs`** 让
            //      「刚连上、还没发过包」的连接不被误判成「10 秒没消息」。⛔ 别挪回这儿。

            // ---- 客机：重连退避 ----
            if (Role == NetRole.Client && State == NetState.WaitingReconnect && _wasInBattle &&
                !string.IsNullOrEmpty(SessionToken) && now - _lastReconnMs >= ReconnectEveryMs)
            {
                _lastReconnMs = now;
                var cfg = NetConfig.Current;
                _t.ClosePeer();
                SetState(NetState.WaitingReconnect, Loc.T("Settings/Online/St/Reconnecting"));
                if (_t.Connect(cfg.ip, cfg.port, ConnectTimeoutMs))
                {
                    _lastRecvMs = now;
                    SetState(NetState.Handshaking, Loc.T("Settings/Online/St/CaughtUp"));
                }
                else
                {
                    StatusText = string.Format(Loc.T("Settings/Online/St/ReconnectFailed"), _t.LastError);
                }
            }
        }

        // ==================================================================
        //  🆕 🔴 **2026-10-20（W58-S6FIX · 缺陷 S6）：主机侧「待替换候选」的凭据流程**
        //  旧行为：`TcpTransport.AcceptLoop` 看到 `ConnBit` 立着就把新连接**直接关掉** ——
        //  而「旧连接已经死了、只是主机还没判出来」正是客机重连最常见的时序 ⇒ 客机看到「连上又断」。
        //  新行为（**安全边界写在函数里**）：
        //    ① 候选先拿一次 `Challenge`（新 nonce，与当前连接分开）；
        //    ② `Proof` = HMAC(房间密码, nonce) 必须对上；对不上 ⇒ 关候选，**旧连接原样保留**；
        //    ③ 对局中的替换**还必须**再过 `Reconnect{sessionToken}`（这一局的钥匙）——
        //       「认不出 = 不是这局的人 ⇒ 拒绝」（正本 §5·6 第 3 条）；
        //    ④ 凭据齐了才 `PromotePendingReplacement()`：候选成为当前连接、旧连接走有界优雅收尾。
        //  ⇒ 任意第三方（只要连得上端口）**踢不掉人**：它拿不到密码 proof、也拿不到 sessionToken。
        // ==================================================================
        void PumpReplacement()
        {
            var rh = _t as IPendingReplacementHost;
            if (rh == null) return;           // 夹具 / 非真 TCP：行为与加这一段之前逐字相同

            bool armed = Role == NetRole.Host
                      && (State == NetState.InBattle
                          || (State == NetState.WaitingReconnect && _wasInBattle));
            rh.ReplacementArmed = armed;

            if (!rh.HasPendingReplacement)
            {
                if (_pendingGen != -1) ResetPendingReplacementState();
                return;
            }
            if (!armed)
            {
                rh.RejectPendingReplacement("not-in-battle");
                Debug.LogWarning("[Net] NET/BYE-S6/reject-no-proof 主机此刻**不在对局 / 等重连**（会话 " + State
                               + "）⇒ 候选连接不参与替换（大厅/握手阶段没有「客机重连」这回事）—— 关候选，当前连接不动");
                ResetPendingReplacementState();
                return;
            }
            if (rh.PendingReplacementAgeMs >= ReplacementHandshakeTimeoutMs)
            {
                rh.RejectPendingReplacement("timeout");
                Debug.LogWarning("[Net] NET/BYE-S6/replacement-timeout 候选连接（第 " + rh.PendingReplacementGeneration
                               + " 代 · " + rh.PendingReplacementPeer + "）在 " + ReplacementHandshakeTimeoutMs
                               + " ms 内没交齐凭据 ⇒ 关掉候选，**旧连接原样保留**（客机下一次退避重试会再连）");
                ResetPendingReplacementState();
                return;
            }

            int gen = rh.PendingReplacementGeneration;
            if (gen != _pendingGen)
            {
                // 新候选（或被拒之后又来了一条）：发一次**专门给它**的 Challenge
                _pendingGen = gen;
                _pendingProofOk = false;
                _pendingPeerName = null;
                _pendingNonce = NewToken();
                if (!rh.SendToPendingReplacement(NetKind.Challenge,
                        NetProtocol.Pack(new MsgChallenge { nonce = _pendingNonce })))
                    Debug.LogWarning("[Net] NET/BYE-S6/pending-challenge-failed 候选（第 " + gen
                                   + " 代）的 `Challenge` **没发出去** ⇒ 它交不出 proof，会走到超时（旧连接不受影响）");
            }

            _pendBuf.Clear();
            rh.PumpPendingReplacement(_pendBuf);
            for (int i = 0; i < _pendBuf.Count; i++)
            {
                if (!rh.HasPendingReplacement) break;      // 中途被提升/被拒 ⇒ 后面的帧属于已作废的候选
                try { HandlePendingFrame(rh, _pendBuf[i]); }
                catch (Exception e)
                {
                    Debug.LogError("[Net] NET/BYE-S6/pending-handler-threw 处理候选帧 `" + _pendBuf[i].kind
                                 + "` 时抛异常 ⇒ 立即作废候选（旧连接不受影响）；异常原文：" + e);
                    rh.RejectPendingReplacement("handler-threw");
                    ResetPendingReplacementState();
                    break;
                }
            }
            _pendBuf.Clear();
        }

        void ResetPendingReplacementState()
        {
            _pendingGen = -1;
            _pendingProofOk = false;
            _pendingNonce = null;
            _pendingPeerName = null;
        }

        void HandlePendingFrame(IPendingReplacementHost rh, NetFrame f)
        {
            switch (f.kind)
            {
                case NetKind.Ping:
                {
                    var m = NetProtocol.Unpack<MsgPing>(f.payload);
                    rh.SendToPendingReplacement(NetKind.Pong, NetProtocol.Pack(new MsgPing { t = m != null ? m.t : 0 }));
                    return;
                }
                case NetKind.Pong: return;

                case NetKind.Proof:
                {
                    var m = NetProtocol.Unpack<MsgProof>(f.payload);
                    object[] args;
                    string key = CheckProofKey(m, _pendingNonce, out args);
                    if (key != null)
                    {
                        string why = args == null ? Loc.T(key) : string.Format(Loc.T(key), args);
                        rh.SendToPendingReplacement(NetKind.Ack, NetProtocol.Pack(new MsgAck
                        {
                            ok = false,
                            reason = NetWireText.Pack(key, args),
                            sessionToken = null,
                        }));
                        rh.RejectPendingReplacement("bad-proof");
                        Debug.LogWarning("[Net] NET/BYE-S6/reject-no-proof 候选（第 " + _pendingGen + " 代 · "
                                       + rh.PendingReplacementPeer + "）的 `Proof` **没过**（" + why
                                       + "）⇒ 关候选，**旧连接原样保留**（凭据是替换的唯一入口）");
                        ResetPendingReplacementState();
                        return;
                    }
                    _pendingPeerName = ClampPeerText(m != null ? m.name : null, NetProtocol.MaxPeerNameChars);
                    if (!rh.SendToPendingReplacement(NetKind.Ack, NetProtocol.Pack(new MsgAck
                    {
                        ok = true,
                        reason = "",
                        // 🔴 **2026-10-20（W58-S6FIX · 安全边界）**：候选**永远不**在 `Ack` 里拿到 `sessionToken`。
                        //   重连的客机本来就有它；下发只会让「知道密码 / 空密码房间」的候选白拿这把钥匙 ⇒
                        //   等于把替换门槛降到零。候选必须**自己**在 `Reconnect` 里出示它。
                        sessionToken = null,
                    })))
                    {
                        rh.RejectPendingReplacement("ack-write-failed");
                        Debug.LogWarning("[Net] NET/BYE-S6/reject-no-proof 候选（第 " + _pendingGen
                                       + " 代）的 `Ack` 写不出去（连接已坏）⇒ 作废候选，旧连接不动");
                        ResetPendingReplacementState();
                        return;
                    }
                    _pendingProofOk = true;
                    Debug.Log("[Net] NET/BYE-S6/proof-ok 候选（第 " + _pendingGen + " 代 · " + rh.PendingReplacementPeer
                            + "）密码/版本通过 ⇒ 等它的 `Reconnect{sessionToken}` 才提升"
                            + "（对局中的替换必须有这一道：钥匙对不上就不是这局的人）");
                    return;
                }

                case NetKind.Reconnect:
                {
                    if (!_pendingProofOk)
                    {
                        rh.SendToPendingReplacement(NetKind.Bye,
                            NetProtocol.Pack(new MsgBye { reason = NetWireText.Pack("Settings/Online/Wire/BadKey") }));
                        rh.RejectPendingReplacement("reconnect-before-proof");
                        Debug.LogWarning("[Net] NET/BYE-S6/reject-no-proof 候选（第 " + _pendingGen
                                       + " 代）**没交 proof 就发 `Reconnect`** ⇒ 作废候选，旧连接不动");
                        ResetPendingReplacementState();
                        return;
                    }
                    var m = NetProtocol.Unpack<MsgReconnect>(f.payload);
                    if (m == null || (SessionToken != null && m.sessionToken != SessionToken))
                    {
                        rh.SendToPendingReplacement(NetKind.Bye,
                            NetProtocol.Pack(new MsgBye { reason = NetWireText.Pack("Settings/Online/Wire/BadKey") }));
                        rh.RejectPendingReplacement("bad-token");
                        Debug.LogWarning("[Net] NET/BYE-S6/reject-no-token 候选（第 " + _pendingGen + " 代 · "
                                       + rh.PendingReplacementPeer + "）的 `Reconnect` **钥匙对不上这一局**"
                                       + " ⇒ 关候选，**旧连接原样保留**（正本 §5·6：认不出 = 不是这局的人 ⇒ 拒绝）");
                        ResetPendingReplacementState();
                        return;
                    }
                    if (ResumeProvider == null)
                    {
                        rh.SendToPendingReplacement(NetKind.Bye,
                            NetProtocol.Pack(new MsgBye { reason = NetWireText.Pack("Settings/Online/Wire/NoRecord") }));
                        rh.RejectPendingReplacement("no-record");
                        Debug.LogWarning("[Net] NET/BYE-S6/reject-no-proof 候选（第 " + _pendingGen
                                       + " 代）钥匙对、但本机**没有重放记录**（`ResumeProvider == null`）⇒ 作废候选，旧连接不动");
                        ResetPendingReplacementState();
                        return;
                    }

                    // 凭据齐了：**先**取重放数据（失败不影响旧连接），**再**提升
                    var (start, actions) = ResumeProvider();
                    if (!rh.PromotePendingReplacement())
                    {
                        Debug.LogWarning("[Net] NET/BYE-S6/promote-failed 候选（第 " + _pendingGen
                                       + " 代）凭据已通过、但提升那一步失败（候选已被别的线程作废？）⇒ 保持旧连接");
                        ResetPendingReplacementState();
                        return;
                    }
                    PeerName = _pendingPeerName;
                    long nowMs = NowMs;
                    _lastRecvMs = nowMs;
                    _lastPingMs = nowMs;
                    // 从这里起 `Send` 走的是**新**连接（`_stream` 已被提升那一步换掉）
                    Send(NetKind.Resume, new MsgResume
                    {
                        start = start,
                        actionsJson = NetProtocol.Pack(new MsgActionList { items = actions ?? new List<MsgAction>() }),
                    });
                    SetState(NetState.InBattle, string.Format(Loc.T("Settings/Online/St/ResumeSent"),
                                                              PeerName, actions?.Count ?? 0));
                    _wasInBattle = false;
                    Debug.Log("[Net] NET/BYE-S6/replaced 候选（第 " + _pendingGen + " 代 · " + PeerName
                            + "）凭据齐全 ⇒ **接管当前连接**（旧连接已在传输层走有界优雅收尾）；"
                            + "已灌 `resume`（权威动作流 " + (actions?.Count ?? 0) + " 条）");
                    ResetPendingReplacementState();
                    return;
                }

                case NetKind.Bye:
                    rh.RejectPendingReplacement("peer-bye");
                    Debug.Log("[Net] NET/BYE-S6/candidate-bye 候选（第 " + _pendingGen + " 代 · "
                            + rh.PendingReplacementPeer + "）自己说了 `bye` ⇒ 作废候选，旧连接不动");
                    ResetPendingReplacementState();
                    return;

                default:
                    // 凭据阶段只认 Proof / Reconnect / Ping / Pong / bye；别的都不足以提升候选，也**不能**
                    // 交给上层（它不是当前对家说的话）。不静默。
                    Debug.LogWarning("[Net] NET/BYE-S6/pending-unexpected 候选（第 " + _pendingGen + " 代）发来 `"
                                   + f.kind + "`（凭据阶段不认）⇒ 忽略这一帧；候选**不会**因此被提升");
                    return;
            }
        }

        /// <summary>握手口令核对（**唯一一份规则**：当前连接的 `case Proof` 与候选路径都走它）。
        /// 返回 `null` = 通过；否则返回**词条键**（`args` 是它的格式化参数）。
        /// ⚠️ 空密码 = 不校验（局域网自用；`Settings/Online/St/WrongPassword` 只在设了密码时成立）。</summary>
        string CheckProofKey(MsgProof m, string nonce, out object[] args)
        {
            args = null;
            if (m == null) return "Settings/Online/St/BadHello";
            if (m.protoVer != NetProtocol.Version)
            {
                args = new object[] { m.protoVer, NetProtocol.Version };
                return "Settings/Online/St/VersionMismatch";
            }
            var want = Proof(_hostPassword, nonce);       // ⚠️ 用**开台那一刻**的密码
            if (!string.IsNullOrEmpty(_hostPassword) && m.proof != want)
                return "Settings/Online/St/WrongPassword";
            if (string.IsNullOrEmpty(_hostPassword))
                Debug.LogWarning("[Net] 本局**没有设密码** —— 局域网自用可以，公网请设一个");
            return null;
        }

        // ==================================================================
        //  收包
        // ==================================================================

        void Handle(NetFrame f)
        {
            _lastRecvMs = NowMs;
            switch (f.kind)
            {
                case NetKind.Ping:
                {
                    var m = NetProtocol.Unpack<MsgPing>(f.payload);
                    Send(NetKind.Pong, new MsgPing { t = m != null ? m.t : 0 });
                    return;
                }
                case NetKind.Pong: return;                       // 心跳往返，记时间就够了

                case NetKind.Challenge:                          // 只有客机会收到
                {
                    var m = NetProtocol.Unpack<MsgChallenge>(f.payload);
                    // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志）**：解不出来原来会**静默**按空 nonce
                    //   继续（`MsgChallenge` 只有一个字段，坏包多半是版本/编码问题）⇒ 握手会拿错 nonce 算口令。
                    if (m == null)
                        Debug.LogWarning("[Net] NET/BYE-R1/challenge-unparsable 对面发来的 `hello.challenge` **解不出来**"
                                       + " ⇒ 按**空 nonce** 继续握手（这一条不改行为；真连时看到它 = 握手包坏了）");
                    _nonce = m != null ? m.nonce : "";
                    var cfg = NetConfig.Current;
                    Send(NetKind.Proof, new MsgProof
                    {
                        protoVer = NetProtocol.Version,
                        gameVer = Application.version,
                        name = PlayerName(),
                        proof = Proof(cfg.password, _nonce),
                    });
                    if (State != NetState.WaitingReconnect)
                        // ⚠️ **2026-10-19（P6 · 双语）**：这一句与 `:181` 共用
                        //    `Settings/Online/St/Handshaking` **一条键**（两处原文只差「连上了，」一个前缀）
                        //    ⇒ 落键后这一句**也跟着带上了那个前缀**，如实标出这处可见变化。
                        SetState(NetState.Handshaking, Loc.T("Settings/Online/St/Handshaking"));
                    return;
                }
                case NetKind.Proof:                              // 只有主机会收到
                {
                    var m = NetProtocol.Unpack<MsgProof>(f.payload);
                    // ⚠️ **2026-10-19（P6 · 双语）**：下面这三句 `why` 有**两个**去处 ——
                    //    ① 本机的 `SetState` / `LastError`（玩家看得见 ⇒ 走词条）；
                    //    ② `Send(NetKind.Ack, … reason = why)` —— **会穿过 TCP 到对端屏幕上**
                    //       （那是 `A1038` / P6d 的「走线文案」问题）。P6 简报把 `bye` 那 4 处圈成
                    //       「本次一个字不许碰」，这三处**不在**那个圈里（施工单 §② 判它 ①）⇒ 落键。
                    // 🔴 **2026-10-19（P6d · A1081）走线那一半也接完了**：`reason` 现在发的是
                    //     **`NetWireText.Pack(键, 参数)`**，⛔ 不再发**已渲染好的**那句（那等于把本机的
                    //     语言灌到对端屏幕上）。**这三条键与「本机显示」共用 `St/*` 一族** —— 判据：
                    //     两个角色下**要印的那句话逐字相同**（本机印 「密码不对」，对面读到的也该是
                    //     「密码不对」的**它自己语言**版），而**走线上的是键、键是语言无关的**
                    //     ⇒ 另开 `Wire/*` 只会多出一份迟早会漂的副本（铁律 6）。
                    //     ⛔ 与那 7 条 `Wire/*` 的分别：那一族是**本机那句与发给对面的那句措辞不同**
                    //     （例 `St/RejectBadKey`「重连被拒：钥匙对不上」vs `Wire/BadKey`「这把钥匙对不上这一局」）。
                    // 🔴 **2026-10-20（W58-S6FIX）**：口令/版本那三条规则抽到 `CheckProofKey` ——
                    //   候选路径（`HandlePendingFrame`）走**同一份**规则。⛔ 别在两处各写一份（迟早漂）。
                    object[] whyArgs;
                    string whyKey = CheckProofKey(m, _nonce, out whyArgs);
                    // 本机那一份（`SetState` 里那句「拒绝了这次连接：{0}」要的是**已渲染**的整句）
                    string why = whyKey == null ? null
                               : (whyArgs == null ? Loc.T(whyKey) : string.Format(Loc.T(whyKey), whyArgs));
                    // 🔴 **A961（2026-10-18）**：`MsgProof.name` **是对端可控的**，而它会被拼进 `StatusText`
                    //   （本文件 `SetState` 那几处，设置→联机页那一行字）⇒ **进界面前先钳**。
                    //   口径与取值 → `NetProtocol.MaxPeerTextChars`（⚠️ 是**我们自拟的**，原版把玩家名的长度
                    //   校验放在 PlayFab 服务端，客户端一次都不查）。
                    PeerName = ClampPeerText(m != null ? m.name : null, NetProtocol.MaxPeerNameChars);
                    // 🔴 **P6d：线上发【键 + 参数】，⛔ 不发已渲染的文本**（收侧 `NetWireText.Unpack` 取词）
                    // 🔴 **2026-10-20（W58-NETFIX · NETPROBE ④）**：接住返回值 —— 下面那条拒绝支要靠它判
                    //   「这句理由到底有没有写进传输层」，从而决定收尾要不要排空（见那个分支的注释）。
                    bool sentAck = Send(NetKind.Ack, new MsgAck
                    {
                        ok = whyKey == null,
                        reason = whyKey == null ? "" : NetWireText.Pack(whyKey, whyArgs),
                        // 🆕 🔴 **2026-10-20（W58-S6FIX · 安全边界）**：**对局中（含等重连）不再下发钥匙**。
                        //   原来这里无条件下发 `SessionToken` —— 而重连的客机**本来就已经有**它（重连退避的
                        //   前提就是 `SessionToken` 非空）。无条件下发只有一个效果：**任何**知道/不需要密码
                        //   的连接都能在握手时白拿这把钥匙 —— 配上候选替换就等于「空密码房间谁都能踢人」。
                        //   ⇒ `InBattle` / `_wasInBattle` 这两档发空串；入局前（Lobby 之前）照旧下发。
                        sessionToken = (State == NetState.InBattle || _wasInBattle) ? null : SessionToken,
                    });
                    if (why != null)
                    {
                        LastError = why;
                        SetState(NetState.Closed, string.Format(Loc.T("Settings/Online/St/Refused"), why));
                        // 🔴 **2026-10-20（W58-NETFIX · NETPROBE ④）**：原来是 `Send(Ack ok=false, 理由)` 之后
                        //   **立刻** `_t.Close()` —— 与 `bye` 旧形状同构，那句**拒绝理由**可能到不了对面
                        //   （对面只看到「连上了又断了」）。改走唯一收口点：
                        //   `sentAck == true` ⇒ 真 TCP 上 `Shutdown(Send)` + 有界排空，让 `ack` 先出去；
                        //   `sentAck == false` ⇒ 没有要排空的字节，照旧直接关（别白等 200 ms）。
                        CloseTransportAfterBye(sentAck);
                        if (OnClosed != null) OnClosed(why);
                    }
                    else if (_wasInBattle)
                    {
                        // 对局中掉线又回来了 ⇒ 停在「等他发 reconnect」，别退成 Lobby
                        SetState(NetState.WaitingReconnect, string.Format(Loc.T("Settings/Online/St/PeerBackWaitReport"), PeerName));
                    }
                    else
                    {
                        SetState(NetState.Lobby, string.Format(Loc.T("Settings/Online/St/PeerInLobby"), PeerName));
                        if (OnPeerReady != null) OnPeerReady();
                    }
                    return;
                }
                case NetKind.Ack:                                // 只有客机会收到
                {
                    var m = NetProtocol.Unpack<MsgAck>(f.payload);
                    if (m == null || !m.ok)
                    {
                        // 🔴 **A961（2026-10-18）**：`MsgAck.reason`（`NetProtocol.MsgAck.reason`）**也是对端可控的** ——
                        //   它往下进 `StatusText`（下面那句 `SetState`）与 `OnClosed`（→ `NetMatchmaking` /
                        //   `NetBattle` 的提示行与弹窗）⇒ 与 `PeerName` / `MsgBye.reason` **同一处口径**：先钳。
                        // 🔴 **P6d**：钳完再 `NetWireText.Unpack`（**次序硬** —— 先钳对端可控的原串、
                        //   再取词；反过来的话 40 字钳的是我们自己的文案，理由见 `NetWireText` 类注释）。
                        string why = m != null && !string.IsNullOrEmpty(m.reason)
                                   ? NetWireText.Unpack(ClampPeerText(m.reason))
                                   : Loc.T("Settings/Online/St/PeerRefused");
                        LastError = why;
                        SetState(NetState.Closed, why);
                        // 🔴 **2026-10-20（W58-NETFIX · NETPROBE ③ 的同族顺手）**：这一支原来也是
                        //   `_t.Close()` **硬拆**。对端（主机）在 `case Proof` 拒绝支里刚发完这句
                        //   `ack ok=false`、正按 ④ 走优雅收尾 ⇒ 本机一个 RST 打过去会把它的排空打成
                        //   `NET/BYE-S5/drain-error`，而**那句拒绝理由**正是它要送给我们的。
                        //   ⇒ 与收 `bye` 同一档：`graceWithoutBye:true`（本机没 `bye` 要发，只给对端礼节）。
                        CloseTransportAfterBye(false, true);
                        if (_checkMode && OnCheckDone != null) OnCheckDone(false, why);
                        if (OnClosed != null) OnClosed(why);
                        return;
                    }
                    // 🆕 🔴 **2026-10-20（W58-S6FIX · 安全边界）**：**重连这一趟不要用 `Ack` 覆盖钥匙** ——
                    //   主机在对局中/等重连时**故意**发空 `sessionToken`（见 `case Proof` 的注释；
                    //   理由：不能让候选白拿钥匙）。重连的客机**本来就持有**这把钥匙
                    //   （它的退避闸 `!string.IsNullOrEmpty(SessionToken)` 就是前提）。
                    //   ⛔ 别改回无条件赋值：那会把客机自己的钥匙**抹成空** ⇒ 下一次 `Reconnect`
                    //   必然钥匙对不上（重连永远失败，而且日志看起来像「主机拒绝」）。
                    if (!_wasInBattle && !string.IsNullOrEmpty(m.sessionToken))
                        SessionToken = m.sessionToken;
                    // 🆕 **2026-10-19（P6d · A1079①）**：这一句原来是一段**裸中文字面量**，而它会经
                    //     `OnCheckDone` 进设置窗（`Shell/SettingsWindow.cs` 的 `SetFlash(() => (ok ? "✅ " : "❌ ") + why)`）
                    //     ⇒ **是①类玩家可见文案**，落键。键名/两列**两张原版表都搜过、0 命中**（原版联机走 PlayFab、
                    //     没有「检查连接」这套流程）⇒ **自拟**，出处 = 调用点原话逐字（`Core/Loc.cs` 的 `St/CheckOk`）。
                    if (_checkMode && OnCheckDone != null) OnCheckDone(true, Loc.T("Settings/Online/St/CheckOk"));
                    _checkMode = false;
                    if (_wasInBattle)
                    {
                        // 🔴 **重连的那一步**：报上钥匙 + 打到第几条，等主机灌权威动作流回来
                        Send(NetKind.Reconnect, new MsgReconnect { sessionToken = SessionToken, lastSeq = _lastSeq });
                        SetState(NetState.WaitingReconnect, Loc.T("Settings/Online/St/ResumedWaitProgress"));
                    }
                    else
                    {
                        SetState(NetState.Lobby, Loc.T("Settings/Online/St/ClientLobby"));
                        if (OnPeerReady != null) OnPeerReady();
                    }
                    return;
                }
                case NetKind.Reconnect:                          // 主机收到：认 token ⇒ 灌动作流
                {
                    var m = NetProtocol.Unpack<MsgReconnect>(f.payload);
                    if (m == null || (SessionToken != null && m.sessionToken != SessionToken))
                    {
                        // 🔴 **P6d（A1038）**：线上发**词条键**，⛔ 不发中文整句（那会把本机语言灌到对面屏幕上）。
                        //   收侧 `NetWireText.Unpack` 按**它自己**的语言取词；旧端收到键名则原样印（≤40 字、不被钳）。
                        // 🔴 **2026-10-19（`A1264` X2 · T05）**：这一支原来也走「发完 `bye` 立刻 `_t.Close()`」
                        //   —— 那是 X2 里被点名的旧形状（同一族两处都改成优雅收尾）⇒ 走唯一收口点。
                        bool sentBye = Send(NetKind.Bye, new MsgBye { reason = NetWireText.Pack("Settings/Online/Wire/BadKey") });
                        LastError = Loc.T("Settings/Online/St/RejectBadKey");
                        SetState(NetState.Closed, LastError);
                        CloseTransportAfterBye(sentBye);
                        return;
                    }
                    if (ResumeProvider == null)
                    {
                        // 🔴 **2026-10-19（`A1264` X2 · T05）**：同上（`Wire/NoRecord` 那一支）。
                        bool sentBye = Send(NetKind.Bye, new MsgBye { reason = NetWireText.Pack("Settings/Online/Wire/NoRecord") });
                        LastError = Loc.T("Settings/Online/St/RejectNoLog");
                        SetState(NetState.Closed, LastError);
                        CloseTransportAfterBye(sentBye);
                        return;
                    }
                    var (start, actions) = ResumeProvider();
                    Send(NetKind.Resume, new MsgResume
                    {
                        start = start,
                        actionsJson = NetProtocol.Pack(new MsgActionList { items = actions ?? new List<MsgAction>() }),
                    });
                    SetState(NetState.InBattle, string.Format(Loc.T("Settings/Online/St/ResumeSent"), PeerName, actions?.Count ?? 0));
                    _wasInBattle = false;                      // 追平了：再有人连进来就是新的一桌
                    return;
                }
                case NetKind.Resume:                             // 客机收到：全量重放
                {
                    var m = NetProtocol.Unpack<MsgResume>(f.payload);
                    if (m == null || m.start == null)
                    {
                        // 🔴 **P6d（A1038）**：`Close` 的 `reason` 现在是**词条键**（见那个方法的 doc）
                        //    —— 这一串是**唯一客机→主机**的那一条（上面两条只有主机发得出来）。
                        Close(true, "Settings/Online/Wire/BadResume");
                        return;
                    }
                    var list = NetProtocol.Unpack<MsgActionList>(m.actionsJson);
                    var actions = list != null ? list.items : new List<MsgAction>();
                    SetState(NetState.InBattle, string.Format(Loc.T("Settings/Online/St/ResumeCaughtUp"), actions.Count));
                    _wasInBattle = false;
                    if (OnResumed != null) OnResumed(m.start, actions);
                    return;
                }
                case NetKind.Bye:
                {
                    var m = NetProtocol.Unpack<MsgBye>(f.payload);
                    // 🔴 **A961（2026-10-18）**：`MsgBye.reason` **是对端可控的**，上界原来只有
                    //   `NetProtocol.MaxFrame = 4 MiB` ⇒ 对面一句话能把提示行 / 弹窗 / 状态字**全顶爆**。
                    //   这里是**收口点**（三个入口都在本文件）：钳完再往下走，`StatusText` 与 `OnClosed`
                    //   的每一个消费方（`NetMatchmaking` 的提示行+弹窗、`NetBattle.HandlePeerClosed`）
                    //   **自动都拿到钳过的那一段**。取值与「这是我们的口径、原版没有先例」→ `NetProtocol.MaxPeerTextChars`。
                    // 🔴 **P6d**：钳完再 `NetWireText.Unpack`（**次序硬** —— 理由同 `case NetKind.Ack`）。
                    //   对面发来**词条键**时这里取的是**本机语言**那一句；发来的是**旧端的自然语言**
                    //   （或自检那种自定义串）时 `HasEntry` 为假 ⇒ **原样回显**（白赚的兼容）。
                    string why = m != null && !string.IsNullOrEmpty(m.reason)
                               ? NetWireText.Unpack(ClampPeerText(m.reason)) : Loc.T("Settings/Online/St/PeerLeft");
                    // 🔴 **2026-10-19（T05 判别器）**：收到这一帧**留一条**（与发送侧那条成对）。
                    //   排查「`bye` 丢在哪一跳」时：这一行出现 = **对端的 `bye` 真的到了本机应用层**
                    //   （与下面 `SetState(Closed, …)` 那条自动打的 `[Net] Closed：…` **不是同一个观测口** ——
                    //    `Closed` 也可能是 `Ack` 被拒 / 重连被拒，理由不同）。
                    // ⚠️ 打的是**钳过**的原串（`MsgBye.reason` 是对端可控的）—— ⛔ 别把钳去掉。
                    // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：本机**先关**过
                    //   （`Closed`/`Off`）之后队列里仍可能剩下一条迟到的 `bye`（`Close` 不清 transport 的
                    //   `_inbox`，W57 顺手发现 #1）⇒ 这一档会说**第二遍告别**。记一笔，避免真连时把它
                    //   当成「对面刚刚才离开」而与 `OnPeerLost` 的时序对不上。
                    if (State == NetState.Closed || State == NetState.Off)
                        Debug.Log("[Net] NET/BYE-R6/bye-after-local-close 收到 `bye` 时会话**已经是 " + State
                                + "**（本机先关的）⇒ 这是队列里迟到的告别，不是对面**新的**离开"
                                + "（`Close` 不清传输层收件箱 ⇒ 旧 `bye` 仍会被处理；见 W57 顺手发现 #1）"
                                + "【⚠️ 2026-10-20 W58-NETFIX ⑤ 之后：`TcpTransport.Close()` 已清收件箱 + 入队口加了 `_closed` 闸，"
                                + "这一档只剩「本机没走 Close 的换会话/夹具路径」，正常真连应看不到它】");
                    Debug.Log("[Net] 收到 `bye`（reason = "
                            + (m != null && m.reason != null ? ClampPeerText(m.reason) : "<null>") + "）");
                    SetState(NetState.Closed, why);
                    // 🔴 **2026-10-20（W58-NETFIX · NETPROBE ③）**：原来这里 `_t.Close()` **硬拆** ——
                    //   本机确实没东西要发，但对端可能还在它那 200 ms 排空里（`CloseGracefully` 等 FIN），
                    //   一个 RST 打过去会让它看到 `NET/BYE-S5/drain-error`。改成走**同一个收口点**的
                    //   优雅档（真 TCP：`Shutdown(Send)` + 有界排空 + `Close()`；夹具：照旧 `Close()`）。
                    //   ⚠️ `sentBye:false` —— 本机没有 `bye` 要发，不需要为「发出 `bye`」额外等；`graceWithoutBye:true`
                    //   表达的是「对端在收尾，我给足它礼节」。两段等待共用一个 `GracefulCloseDrainMs` 预算。
                    CloseTransportAfterBye(false, true);
                    if (OnClosed != null) OnClosed(why);
                    return;
                }
            }

            // 其余（对局类消息）交给上层
            Inbox.Add(f);
        }

        /// <summary>主机：重连时给客机灌的那份「开局参数 + 权威动作流」。由 `NetBattle` 接。</summary>
        public Func<(MsgStart start, List<MsgAction> actions)> ResumeProvider;

        // ==================================================================
        //  发
        // ==================================================================

        /// <summary>发一条。**返回「这条到底发出去了没有」** —— 没连上时返回 `false` 并且**一定出声**
        /// （`Debug.LogWarning`）。调用方只有需要据此做别的事时才接这个返回值（原来返回 `void`）。
        ///
        /// <para>🔴 **2026-10-18（账：「`Send()` 静默丢」）**：原来这一支是 `if (…) return;` —— **连日志都没有**。
        /// 而握手包 / 开局包 / 指纹 / 投降**全都走这里** ⇒ 一条包丢了，日志里一个字都没有，
        /// 只剩玩家那边「什么都没发生」（红线：不许静默失败）。</para>
        ///
        /// <para>🔴 **2026-10-18（第五轮 · 账 ①）**：**过了守卫、底层仍可能失败** —— 那种情况的消费点
        /// 就在本方法里（调传输层**前后各读一次 `_t.LastError`**，变了就出声 + 返回 `false`）。
        /// ⛔ 别把它改成「判 `LastError` 非空」：那个字段是**粘的**（只在 `Setup`/`Listen` 里清零）。</para>
        ///
        /// <para>🔴 **为什么不顺手把 `_lastAccepted` 挪到「发送成功之后」**（R3 报告 §4 建议过这一步）：
        /// 实测会变成**每帧重试** —— 连接断着而 `AcceptedCount` 已经变过的话，`Pump()` 里那一支
        /// **每帧都会进**（判据是 `AcceptedCount != _lastAccepted`，而它一直不相等），于是每帧一次
        /// `NewToken()`（加密随机数 + 十六进制串）与一次 `SetState()`（每次都 `Debug.Log`），
        /// 一直刷到下一次连接进来为止 —— 比「丢一条握手包」更糟（而且会刷日志）。
        /// 何况那一支正是 `A943` 读侧确认（`Pump()` 里 `_t.AcceptedCount == _lastAccepted` 那半句）依赖的地方，
        /// 动它要重走整段 §P。⇒ **本笔按账上要求只补「出声」**；次序那条**如实挂给主对话裁**
        /// （理由与建议见 `资料/普查产出_1018/W3_联机收尾交件.md`）。</para></summary>
        public bool Send(string kind, string payloadJson)
        {
            if (_t == null || !_t.IsConnected)
            {
                // 没连上就丢：调用方看 StatusText/LastError —— **但要出声**（红线）。
                Debug.LogWarning("[Net] NET/BYE-S2/no-live-link 这条 `" + kind + "` **没发出去、被丢掉了**：这一刻没有活的连接"
                               + $"（会话 {State} · 本机角色 {Role}）—— 如实说出来，别让玩家只看到「什么都没发生」");
                return false;
            }
            string errBefore = _t.LastError;
            _t.Send(kind, payloadJson);
            // 🔴 **2026-10-18（第五轮 · 账 ①）：`_lastError` 的【消费点】。**
            //   传输层「过了守卫、但底层真写失败」（`TcpTransport.Send` 的 `catch`）只把原因写进
            //   `_lastError` —— 那个字段**原来没有任何生产者路径读它** ⇒ 真失败被吞。
            //   这里**前后各读一次**：**变了**才算这次失败（`_lastError` 是**粘的** —— 只在 `Setup`/`Listen`
            //   里清零，直接判非空会把一次旧失败**永久**报成新失败）。
            //   ⛔ 别改成「判 `!string.IsNullOrEmpty`」——那是上面那个粘性坑。
            string errAfter = _t.LastError;
            if (!string.IsNullOrEmpty(errAfter) && !string.Equals(errAfter, errBefore, StringComparison.Ordinal))
            {
                Debug.LogWarning("[Net] NET/BYE-S4/net-session-send-failed 这条 `" + kind + "` 在**传输层报失败了**（写失败 / 链路刚断）："
                               + errAfter + " —— 对面的那半边收不到它（红线：不许静默失败）"
                               + "。⚠️ 本判据是「调用前后 `_lastError` 变了」，同一个原因**第二次**出现时它认不出来"
                               + "（W57 顺手发现 #3）—— 那种情况去看传输层 `NET/BYE-S4/write-failed` 那一行");
                return false;
            }
            return true;
        }

        public bool Send<T>(string kind, T msg) { return Send(kind, NetProtocol.Pack(msg)); }

        /// <summary>进对局（上层在 `Begin` 之后调）。之后掉线才走「等重连」。</summary>
        public void EnterBattle(int lastSeq = 0)
        {
            _lastSeq = lastSeq;
            _wasInBattle = false;                      // 新的一局：掉线前的「他是我对手」标记清掉
            SetState(NetState.InBattle, Loc.T("Settings/Online/St/InBattle")
                                      + (Role == NetRole.Host ? Loc.T("Settings/Online/St/HostSide")
                                                              : Loc.T("Settings/Online/St/ClientSide")));
        }

        /// <summary>主动收工（用户退出 / 打完）。`say=true` 时给对面捎一句 `bye`。
        /// 🔴 **2026-10-18（独立审查 R5）**：`say=true` 而**这一刻发不出去**时**要出声** ——
        /// 原来那一句写成 `if (say &amp;&amp; _t != null &amp;&amp; _t.IsConnected &amp;&amp; State != Off) Send(…)`，
        /// 条件不成立就**一声不响**（而 `Send` 新加的那声警告**永远走不到**这一步）⇒
        /// 「丢包一定出声」这条**恰好在「我们最需要对面收到的那句话」上不成立**。现在补上。
        ///
        /// <para>🔴 **2026-10-19（P6d · A1038 裁定 ③）：`reason` 是【词条键】，不是一句现成的文案。**
        /// 它是**双重载荷** —— 既上 wire（`MsgBye.reason`，**到对端屏幕上**）、又进本机
        /// `StatusText`（下面那句 `SetState(Off, …)`）⇒ 两半都走**同一个键**，⛔ 不留裸串：
        ///   · 线上发 <c>reason</c> 本身（键是 ASCII、语言无关，≤ 40 字 ⇒ `ClampPeerText` 不会截）；
        ///   · 本机印 `NetWireText.Unpack(reason)`（按**本机**语言取词）。
        /// 传 `null` = **线上**走兜底 `Settings/Online/Wire/PeerDone`（「对面结束了这一局」），
        /// 而**本机状态字仍是** `St/Off`（「未连接」，与接线前逐字一致 —— 见下面那一句的注释）。
        /// ⚠️ **传进来的若不是词条键**（自检里那几个「自检：…」串、以及旧调用点遗留的整句）
        /// ⇒ `Unpack` 原样回显、**不出声**（`HasEntry` 假就走回显那一支，不碰缺键记账）
        /// ⇒ 行为与接线前**逐字相同**，那几条既有断言照旧绿。</para></summary>
        public void Close(bool say = true, string reason = null)
        {
            string key = reason ?? "Settings/Online/Wire/PeerDone";
            bool sentBye = false;
            if (say)
            {
                if (_t == null || !_t.IsConnected || State == NetState.Off)
                    Debug.LogWarning("[Net] NET/BYE-S1/no-live-link 本来要给对面捎一句 `bye`（" + NetWireText.Unpack(key) + "）"
                                   + $"—— 但**这一刻没有活的连接**（会话 {State}）⇒ 这句话**没发出去**，"
                                   + "对面**不会收到任何通知**（它那边只能靠心跳超时发现）。如实说出来（红线）");
                else
                {
                    sentBye = Send(NetKind.Bye, new MsgBye { reason = key });
                    // 🔴 **2026-10-19（T05 判别器）**：**成功那一刻也留一条** —— 排查「`bye` 丢在哪一跳」
                    //   时，这一行 = 「**发送侧真把它交给传输层了**」（`Send` 已按「调用前后各读一次
                    //   `_lastError`、变了才算失败」判过写失败）；它**不**代表对面收到（那要看对端的
                    //   「收到 `bye`」那一行）。
                    //   ⛔ 别把它并进下面那条失败告警里 —— 两条是不同的观测口。
                    if (sentBye)
                        Debug.Log("[Net] 已给对面捎一句 `bye`（" + NetWireText.Unpack(key) + "）"
                                + " ⇒ 走有界优雅关闭（发送侧：**已交给传输层**）[NET/BYE-S1/sent]"
                                + " —— ⚠️ 这一行只证明「本机写进了传输层」，**不代表对面收到**；"
                                + "对面那一侧要有 `收到 `bye`` 才算闭环");
                }
            }
            // 🆕 关台时**把要来的那条映射撤掉**（别在玩家路由器上留一条没人用的转发规则）。
            //    端口要在 `Close()` 之前抓 —— 关完就取不到了。
            if (Role == NetRole.Host && _t != null) UpnpPortMapper.UnmapAsync(_t.Port);
            // 🔴 说完 `bye` 之后的收尾 = **有界优雅关闭**（理由 / 次序 / 边界 → `CloseTransportAfterBye` 的注释；
            //    ⛔ 别在这里再写一份「`Shutdown` + 排空」的规则 —— 「两处写同一条规则迟早不一致」）。
            CloseTransportAfterBye(sentBye);
            // ⚠️ **本机那一句与线上那句【兜底不同】**：`reason == null` 时线上捎 `Wire/PeerDone`，
            //    而**本机状态字仍是** `St/Off`（「未连接」）—— 与接线前逐字一致。
            //    ⛔ 别把这两半并成一个 `key`（那会让 `Close(false)` 这条常用路上状态字变成「对面结束了这一局」）。
            SetState(NetState.Off, reason == null ? Loc.T("Settings/Online/St/Off") : NetWireText.Unpack(reason));
            _checkMode = false;
            _wasInBattle = false;
            // 🔴 **为什么这里【故意不清】`SessionToken`**（2026-10-18 第五轮 · 账 ③，现核过全仓读者）：
            //   ① 它的**两个写点都在「新连接的开头」** —— `StartHost`（新一把 `NewToken()`）与
            //      收 `Ack` 时（`Handle` 的 `NetKind.Ack` 那一支：`SessionToken = m.sessionToken`）⇒ **不存在「把陈旧钥匙带到下一局」的路径**
            //      （`NetRuntime.Reset()` 更是直接**换一台新会话对象**）。自检 §Q⑦ 把这条**钉住**了。
            //   ② 反过来，**清掉它是有风险的**：重连鉴权那一句写成
            //      `if (m == null || (SessionToken != null && m.sessionToken != SessionToken))` ——
            //      把它清成 `null` 会让**任何** `reconnect` 都通过（鉴权被短路）。虽然那一刻会话通常是
            //      `Off`（收不到包），但**拿一个「现在没事、改天有事」的短路去换一个不存在的收益**不值得。
            //   ③ `Pump()` 里那条客机重连门（`!string.IsNullOrEmpty(SessionToken)`）也不靠 `Close` 来关：
            //      它另有两道闸（`State == WaitingReconnect`、`_wasInBattle`），`Close` 把 `State` 置 `Off`
            //      ⇒ 那一条**本来就已经死了**。
            //   ⇒ **结论：不该清**（不是漏做）。⛔ 下一个人别把它当漏项补上。
        }

        /// <summary>🆕 🔴 **2026-10-19（`A1264` X2「`bye` 丢在哪一跳」· T05）**：**说完 `bye` 之后的收尾** ——
        /// 真 TCP 走**有界优雅关闭**（`Shutdown(Send)` + ≤ `TcpTransport.GracefulCloseDrainMs` 排空），
        /// 其余照旧 `Close()`。
        ///
        /// <para>为什么要有它：原来 `Send`（同步 `Write + Flush`）之后**立刻** `_t.Close()` —— 没有
        /// `Shutdown(Send)`、没有排空、没有 linger ⇒ 刚写出的 `bye` 可能在拆连接时被丢掉
        /// （带了未读数据的拆除在 Windows 上还可能走 abortive 路径，把发送队列一起丢）。
        /// 这与 `NetSession.Close` 头一次被修的那条**是同一个危险形状**（`A1264` 候选 ②）。</para>
        ///
        /// <para>⚠️ 只有「这一趟**真的把 `bye` 写出去了**」才需要排空（`sentBye`）；没发出去 /
        /// `Close(false)` 都照旧直接关（自检里大量 `Close(false)`，别让每条都白等 200ms）。</para>
        ///
        /// <para>⚠️ 只有真 TCP 有这一档：`INetTransport` 接口**没加**这个方法（那要改
        /// `Editor/BattleScene.cs` 的 `RecordingTransport`，不在本笔白名单）⇒ 按类型分派，
        /// 自检里的假 transport 照旧 `Close()`。⛔ 别把接口改成「必须实现」。</para>
        ///
        /// <para>🔴 **唯一收口点**：`Close()` 与 `Handle` 的 `case NetKind.Reconnect` 两条拒绝支
        /// （`Wire/BadKey` / `Wire/NoRecord` —— 那里也是「发完 `bye` 立刻 `_t.Close()`」的旧形状）
        /// **都走这里**。⛔ 别再在别处手写 `Shutdown` / `CloseGracefully` / `_t.Close()` 三件套。</para>
        ///
        /// <para>🆕 🔴 **2026-10-20（W58-NETFIX · NETPROBE ③④）：新增 `graceWithoutBye` 一档，收口点扩到两处新调用方。**
        ///   · `case NetKind.Bye`（**收**到对面的告别，③）：本机没有 `bye` 要发，但**同样不该硬拆** ——
        ///     对面可能还在它那 200 ms 排空里，一个 RST 打过去会让它看到 `drain-error`。
        ///     ⇒ 传 `(sentBye: false, graceWithoutBye: true)` ⇒ 真 TCP 上照样
        ///     `Shutdown(Send)` + 有界排空 + `Close()`（半关闭礼节）。
        ///   · `case NetKind.Proof` 的拒绝支（④）：`Send(Ack ok=false, 理由)` 之后原来**立刻** `_t.Close()`
        ///     ⇒ 那句拒绝理由可能到不了对面。现在把 `Send` 的返回值接进来，**真写出去了才走优雅档**
        ///     （没写出去就没有要排空的字节，照旧直接关 —— 别白等 200 ms）。</para>
        ///
        /// <para>⚠️ **`graceWithoutBye` 只影响「真 TCP 且本次没有 `bye`」这一档**；`Close(false)` 这类
        /// 常用路仍然**直接 `Close()`**（自检里大量 `Close(false)`，别让每条都白等 200 ms）。
        /// 夹具 transport（非 `TcpTransport`）在 `graceWithoutBye` 下仍是 `Close()`，只多一行说明日志。</para>
        /// </summary>
        /// <param name="sentBye">本次**真的把 `bye` 写进了传输层**（`Send` 的返回值）。</param>
        /// <param name="graceWithoutBye">本次没发 `bye`，但**对端已经在收尾**，仍要走优雅档（③ 收 `bye` / ④ 拒绝支）。</param>
        void CloseTransportAfterBye(bool sentBye, bool graceWithoutBye = false)
        {
            if (_t == null) return;
            bool graceful = sentBye || graceWithoutBye;
            if (graceful) GracefulCloseRequestedForTest++;
            else HardCloseRequestedForTest++;
            var tcp = _t as TcpTransport;
            if (tcp != null && graceful) tcp.CloseGracefully();
            else
            {
                // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：假 transport（自检夹具）
                //   没有 `Shutdown`/排空的语义 ⇒ 走这一档是**设计内的**，但说出来，免得真连日志里
                //   把「没走优雅关闭」当成缺陷。
                if (sentBye)
                    Debug.Log("[Net] NET/BYE-S5/not-tcp 这台传输层不是真 TCP（" + _t.GetType().Name
                            + "）⇒ 没法 `Shutdown`/排空，直接 `Close()`（自检夹具走这一档，正常）");
                else if (graceWithoutBye)
                    Debug.Log("[Net] NET/BYE-S5/not-tcp 收尾要求优雅档（收 `bye` / 拒绝支），但这台传输层不是真 TCP（"
                            + _t.GetType().Name + "）⇒ 直接 `Close()`（夹具没有排空语义，正常）");
                _t.Close();
            }
        }

        // ==================================================================
        //  🆕 🔴 **2026-10-20（W58-NETFIX）静态可验证的读口**（生产路径一处都不读）
        //  说明：`NetSession` 不持有 socket 的快照，读口只记**收尾意向**（该不该走优雅档），
        //  与传输层真的怎么做（`TcpTransport.CloseGracefully`）分开 —— 夹具（`ScriptedTransport` /
        //  `RecordingTransport`）走不到真 socket，但**收尾语义照样能被断言**。
        //  ⚠️ 可粘贴的自检块见交件报告（`Editor/NetSelfTest.cs` 不在本轮白名单）。
        // ==================================================================

        /// <summary>③④ 本轮「**该走优雅收尾**」的次数（`sentBye` 或 `graceWithoutBye`）。</summary>
        public int GracefulCloseRequestedForTest { get; private set; }
        /// <summary>「没有告别、也不需要对端礼节」⇒ 直接硬关的次数（`Close(false)` 那一档，行为未变）。</summary>
        public int HardCloseRequestedForTest { get; private set; }

        /// <summary>🆕 🔴 **A914 / 独立审查 R3（2026-10-18）**：**停掉客机那条「每 2 秒重连一次」的退避**，
        /// 但**不关台、不离开房间**。
        ///
        /// <para>对应原版 **`BattleManager__FailedToReconnectAfterDisconnect.c:14`** 的
        /// **`StopCoroutine(AttemptReconnect)`** —— 「**本机**是断线那一端、重连也失败了」那一支
        /// （`:26-27` 只有 `AddResignAction(1) + DeadHero(我,3)`，**整个函数里没有 `LeaveBattleRoom`**，
        /// 现读 `d:/2/tools/decomp_full/BattleManager__FailedToReconnectAfterDisconnect.c` 确认）
        /// ⇒ 对到我们这儿 = **别调 `Close`**（那是原版**另一端**那一跳才有的），只把这条循环停掉。</para>
        ///
        /// <para>落地 = 清 `_wasInBattle`：那正是退避支判据里的一项（`Pump()` 里「客机：重连退避」）。
        /// 而 `_t.PeerLost` 那一支**不会**再把它置回来 —— 它要求状态**不是** `WaitingReconnect`，
        /// 而这里**故意不改状态**。</para>
        ///
        /// <para>⚠️ **状态仍然是 `WaitingReconnect`**（我们**不新造状态**）⇒ 心跳块与静默超时块在这一端
        /// 仍被跳过（它们都要求状态不是它）。**如实记着**，别以为这一端已经收干净。</para>
        ///
        /// <para>🔴 **2026-10-19（铁律 5）就地订正：本段原来写着「这一端**还欠『判本机负』**」—— 已过期。**
        /// 「判本机负」早在 **2026-10-18 第四续（`A914`）** 就补上了：`INetBattleHost.NetSelfResign()`
        /// （`BattleDriver` 那一份 = `RecRaw(RecKindForfeit)` + `RuleCore.Forfeit(Ctx, _me)`，**仍走既有记账口**），
        /// 调用点 = `NetBattle.ReconnectCountdownExpired` 的本机那一支：`_d.NetSelfResign(BattleResult.Disconnect)`
        /// 那一句，紧接着才是本方法 `_s.StopReconnecting()`）⇒ **本方法只负责停循环**，「判谁负」在**调用方**。
        /// ⛔ 别在这里再判一次（那会绕过调用方那一支的 `_d == null` 闸，也会让两端判负各写一份）。</para></summary>
        public void StopReconnecting()
        {
            if (!_wasInBattle) return;                 // 幂等：本来就没在重连 ⇒ 不重复出声
            _wasInBattle = false;
            Debug.Log("[Net] 本机这一端的重连退避**停了**（原版 `FailedToReconnectAfterDisconnect.c:14` 的 "
                    + "`StopCoroutine(AttemptReconnect)`）—— ⚠️ **不关台、不离开房间**"
                    + "（原版那一支里没有 `LeaveBattleRoom`）");
        }

        void SetState(NetState s, string text)
        {
            State = s;
            StatusText = text;
            Debug.Log($"[Net] {s}：{text}");
        }

        // ==================================================================
        //  小工具
        // ==================================================================

        /// <summary>`HMACSHA256(密码, 主机给的一次性 nonce)` 的十六进制 —— **不加密流量**，只做握手口令（正本 §二·1）。</summary>
        public static string Proof(string password, string nonce)
        {
            if (string.IsNullOrEmpty(password)) return "";
            try
            {
                using (var h = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(password)))
                {
                    byte[] mac = h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(nonce ?? ""));
                    var sb = new System.Text.StringBuilder(mac.Length * 2);
                    foreach (var b in mac) sb.Append(b.ToString("x2"));
                    return sb.ToString();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Net] 算握手口令失败（当成空密码）：" + e.Message);
                return "";
            }
        }

        static string NewToken()
        {
            var b = new byte[16];
            using (var r = System.Security.Cryptography.RandomNumberGenerator.Create()) r.GetBytes(b);
            var sb = new System.Text.StringBuilder(b.Length * 2);
            foreach (var x in b) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }

        static string PlayerName()
        {
            // ⚠️ 我们没有「玩家名」那套数据源（原版在服务器）⇒ 用机器名当显示名，**并在界面/日志里说明**。
            // 🔴 2026-09-27：**收口到 `ProfileData.PlayerName`** —— 档案窗的 `ChooseNameWindow` 改的也是它，
            //    两处各取一次机器名就是「两处写同一条规则」（改了名、联机那边还报旧名）。
            return ProfileData.PlayerName;
        }
    }
}
