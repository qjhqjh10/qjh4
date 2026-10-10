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
        public static string ClampPeerText(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= NetProtocol.MaxPeerTextChars) return s;
            return s.Substring(0, NetProtocol.MaxPeerTextChars) + "…";
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
            for (int i = 0; i < _buf.Count; i++) Handle(_buf[i]);
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
                //   （`_state`，见那边的注释），但**读侧是两次读**：上面那一支（`:213`）读计数、这里读连接位。
                //   一次新发布**夹在这两次读之间**时，这里就会拿「旧计数（⇒ 上面那支没走、`_lastRecvMs`
                //   没跟着刷新）+ 新连接位（⇒ 真）」去判静默 ⇒ **掐掉一条刚连上、一条包都还没发的好连接**
                //   （同族的日志实证见 `资料/普查产出_1017/D4_联机诊断.md` §②）。
                //   ⇒ 「计数还是我认过的那个」才说明**没有夹进来的新连接**（计数只增，且上面那支认的就是它）。
                //   🔴 `Role != NetRole.Host ||` 这半句**也不能删**：这道确认**只对主机成立** ——
                //      只有主机的计数是**后台的接受线程**加起来的（`TcpTransport.Setup` 那条路）；
                //      客机的计数由**主线程自己**在 `Connect()` 里加（`Setup` 是两边共用的一支），
                //      而 `_lastAccepted` 在客机侧**没人写**（停在 `-1`）⇒ 少了这半句，客机的静默超时
                //      会被**永久关掉**（`1 == -1` 恒假 ⇒ 对面拔网线再也判不出来）。自检 P⑫ 钉这一点。
                //   ⚠️ 不是死循环、不会饿死：`Pump()` 每帧重来，**下一帧 `:213` 那支就把边沿补上并刷新基线**
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
                    //   同一个文件上面那条（`PeerLost` 那一支，`:176`）本来就是这么分的 ⇒ 两处对齐。
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
                    string whyKey = null; object[] whyArgs = null;
                    if (m == null) whyKey = "Settings/Online/St/BadHello";
                    else if (m.protoVer != NetProtocol.Version)
                    {
                        whyKey = "Settings/Online/St/VersionMismatch";
                        whyArgs = new object[] { m.protoVer, NetProtocol.Version };
                    }
                    else
                    {
                        var want = Proof(_hostPassword, _nonce);       // ⚠️ 用**开台那一刻**的密码
                        if (!string.IsNullOrEmpty(_hostPassword) && m.proof != want)
                            whyKey = "Settings/Online/St/WrongPassword";
                        else if (string.IsNullOrEmpty(_hostPassword))
                            Debug.LogWarning("[Net] 本局**没有设密码** —— 局域网自用可以，公网请设一个");
                    }
                    // 本机那一份（`SetState` 里那句「拒绝了这次连接：{0}」要的是**已渲染**的整句）
                    string why = whyKey == null ? null
                               : (whyArgs == null ? Loc.T(whyKey) : string.Format(Loc.T(whyKey), whyArgs));
                    // 🔴 **A961（2026-10-18）**：`MsgProof.name` **是对端可控的**，而它会被拼进 `StatusText`
                    //   （本文件 `:348` · `:352` · `:411`，设置→联机页那一行字）⇒ **进界面前先钳**。
                    //   口径与取值 → `NetProtocol.MaxPeerTextChars`（⚠️ 是**我们自拟的**，原版把玩家名的长度
                    //   校验放在 PlayFab 服务端，客户端一次都不查）。
                    PeerName = ClampPeerText(m != null ? m.name : null);
                    // 🔴 **P6d：线上发【键 + 参数】，⛔ 不发已渲染的文本**（收侧 `NetWireText.Unpack` 取词）
                    Send(NetKind.Ack, new MsgAck
                    {
                        ok = whyKey == null,
                        reason = whyKey == null ? "" : NetWireText.Pack(whyKey, whyArgs),
                        sessionToken = SessionToken,
                    });
                    if (why != null)
                    {
                        LastError = why;
                        SetState(NetState.Closed, string.Format(Loc.T("Settings/Online/St/Refused"), why));
                        _t.Close();
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
                        _t.Close();
                        if (_checkMode && OnCheckDone != null) OnCheckDone(false, why);
                        if (OnClosed != null) OnClosed(why);
                        return;
                    }
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
                        Send(NetKind.Bye, new MsgBye { reason = NetWireText.Pack("Settings/Online/Wire/BadKey") });
                        LastError = Loc.T("Settings/Online/St/RejectBadKey");
                        SetState(NetState.Closed, LastError);
                        _t.Close();
                        return;
                    }
                    if (ResumeProvider == null)
                    {
                        Send(NetKind.Bye, new MsgBye { reason = NetWireText.Pack("Settings/Online/Wire/NoRecord") });
                        LastError = Loc.T("Settings/Online/St/RejectNoLog");
                        SetState(NetState.Closed, LastError);
                        _t.Close();
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
                    SetState(NetState.Closed, why);
                    _t.Close();
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
                Debug.LogWarning("[Net] 这条 `" + kind + "` **没发出去、被丢掉了**：这一刻没有活的连接"
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
                Debug.LogWarning("[Net] 这条 `" + kind + "` 在**传输层报失败了**（写失败 / 链路刚断）："
                               + errAfter + " —— 对面的那半边收不到它（红线：不许静默失败）");
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
            if (say)
            {
                if (_t == null || !_t.IsConnected || State == NetState.Off)
                    Debug.LogWarning("[Net] 本来要给对面捎一句 `bye`（" + NetWireText.Unpack(key) + "）"
                                   + $"—— 但**这一刻没有活的连接**（会话 {State}）⇒ 这句话**没发出去**，"
                                   + "对面**不会收到任何通知**（它那边只能靠心跳超时发现）。如实说出来（红线）");
                else
                    Send(NetKind.Bye, new MsgBye { reason = key });
            }
            // 🆕 关台时**把要来的那条映射撤掉**（别在玩家路由器上留一条没人用的转发规则）。
            //    端口要在 `Close()` 之前抓 —— 关完就取不到了。
            if (Role == NetRole.Host && _t != null) UpnpPortMapper.UnmapAsync(_t.Port);
            if (_t != null) _t.Close();
            // ⚠️ **本机那一句与线上那句【兜底不同】**：`reason == null` 时线上捎 `Wire/PeerDone`，
            //    而**本机状态字仍是** `St/Off`（「未连接」）—— 与接线前逐字一致。
            //    ⛔ 别把这两半并成一个 `key`（那会让 `Close(false)` 这条常用路上状态字变成「对面结束了这一局」）。
            SetState(NetState.Off, reason == null ? Loc.T("Settings/Online/St/Off") : NetWireText.Unpack(reason));
            _checkMode = false;
            _wasInBattle = false;
            // 🔴 **为什么这里【故意不清】`SessionToken`**（2026-10-18 第五轮 · 账 ③，现核过全仓读者）：
            //   ① 它的**两个写点都在「新连接的开头」** —— `StartHost`（新一把 `NewToken()`）与
            //      收 `Ack` 时（`:413 SessionToken = m.sessionToken`）⇒ **不存在「把陈旧钥匙带到下一局」的路径**
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
        /// 仍被跳过（它们都要求状态不是它）。**如实记着**，别以为这一端已经收干净。
        /// ⚠️ 这一端**还欠「判本机负」**（原版 `DeadHero(我,3)`）—— 那要给 `INetBattleHost` 加一个口
        /// （`BattleDriver` 实现），**不在本笔白名单**，见交件报告 §11。</para></summary>
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
