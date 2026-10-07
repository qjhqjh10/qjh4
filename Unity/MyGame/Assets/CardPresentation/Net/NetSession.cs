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
            StatusText = "未连接";
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
                SetState(NetState.Closed, "主机没起来：" + LastError);
                return false;
            }
            _lastAccepted = _t.AcceptedCount;
            // 🔴 **2026-10-17（F4·RC1）**：开台这一刻把**静默基线**也设上（与客机 `InternalConnect` 那句对称）。
            //   原来不设 ⇒ `_lastRecvMs` 停在字段默认值 0，而 `NowMs` 是**进程级**秒表 ⇒
            //   进程起来满 `SilentTimeoutMs` 之后，主机拿 0 去比 `now`，会把**刚连进来、一条包都还没发的客机**
            //   判成「10 秒没收到对面的任何消息」（判据链与日志实证 → `资料/普查产出_1017/D4_联机诊断.md` §②）。
            _lastRecvMs = NowMs;
            SessionToken = NewToken();
            SetState(NetState.Listening, $"主机已就绪，在 {_t.Port} 端口等客机（把本机 IP 告诉对方）");
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
            SetState(NetState.Connecting, $"正在连 {cfg.ip}:{cfg.port} …");
            if (_t.Connect(cfg.ip, cfg.port, ConnectTimeoutMs))
            {
                _lastRecvMs = NowMs;
                SetState(NetState.Handshaking, "连上了，正在核对协议版本与密码…");
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
            if (_t.PeerLost && State != NetState.Closed && State != NetState.WaitingReconnect)
            {
                _wasInBattle = State == NetState.InBattle;
                string why = _t.LastError;
                SetState(NetState.WaitingReconnect,
                         (_wasInBattle ? "对手掉线了，正在等他回来…（对局已暂停）" : "连接断了：" + why));
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
                         ? "有连接进来，正在核对是不是刚才那个人…" : "有客机连进来了，正在核对…");
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
                    LastError = $"{SilentTimeoutMs / 1000} 秒没收到对面的任何消息";
                    // 🆕 2026-10-17（B23·A902）：这行字**要按在不在对局里分开说** —— 原来无条件是
                    //   「对局已暂停」，而大厅阶段（还没进对局）根本不是那么回事（**说错话** = 另一种静默）。
                    //   同一个文件上面那条（`PeerLost` 那一支，`:176`）本来就是这么分的 ⇒ 两处对齐。
                    SetState(NetState.WaitingReconnect,
                             _wasInBattle ? "对手掉线了，正在等他回来…（对局已暂停）"
                                          : "连接断了：" + LastError);
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
                SetState(NetState.WaitingReconnect, "正在重连主机…");
                if (_t.Connect(cfg.ip, cfg.port, ConnectTimeoutMs))
                {
                    _lastRecvMs = now;
                    SetState(NetState.Handshaking, "连上了，正在补上这一局的进度…");
                }
                else
                {
                    StatusText = "重连失败，稍后再试：" + _t.LastError;
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
                        SetState(NetState.Handshaking, "正在核对协议版本与密码…");
                    return;
                }
                case NetKind.Proof:                              // 只有主机会收到
                {
                    var m = NetProtocol.Unpack<MsgProof>(f.payload);
                    string why = null;
                    if (m == null) why = "对面发来的握手包解不出来";
                    else if (m.protoVer != NetProtocol.Version)
                        why = $"两边版本不一样（对面协议 v{m.protoVer}，本机 v{NetProtocol.Version}）—— 要用同一份构建";
                    else
                    {
                        var want = Proof(_hostPassword, _nonce);       // ⚠️ 用**开台那一刻**的密码
                        if (!string.IsNullOrEmpty(_hostPassword) && m.proof != want) why = "密码不对";
                        else if (string.IsNullOrEmpty(_hostPassword))
                            Debug.LogWarning("[Net] 本局**没有设密码** —— 局域网自用可以，公网请设一个");
                    }
                    // 🔴 **A961（2026-10-18）**：`MsgProof.name` **是对端可控的**，而它会被拼进 `StatusText`
                    //   （本文件 `:348` · `:352` · `:411`，设置→联机页那一行字）⇒ **进界面前先钳**。
                    //   口径与取值 → `NetProtocol.MaxPeerTextChars`（⚠️ 是**我们自拟的**，原版把玩家名的长度
                    //   校验放在 PlayFab 服务端，客户端一次都不查）。
                    PeerName = ClampPeerText(m != null ? m.name : null);
                    Send(NetKind.Ack, new MsgAck { ok = why == null, reason = why ?? "", sessionToken = SessionToken });
                    if (why != null)
                    {
                        LastError = why;
                        SetState(NetState.Closed, "拒绝了这次连接：" + why);
                        _t.Close();
                        if (OnClosed != null) OnClosed(why);
                    }
                    else if (_wasInBattle)
                    {
                        // 对局中掉线又回来了 ⇒ 停在「等他发 reconnect」，别退成 Lobby
                        SetState(NetState.WaitingReconnect, $"「{PeerName}」连回来了，正在等他报进度…");
                    }
                    else
                    {
                        SetState(NetState.Lobby, $"「{PeerName}」进来了 —— 各自选好卡组就能开战");
                        if (OnPeerReady != null) OnPeerReady();
                    }
                    return;
                }
                case NetKind.Ack:                                // 只有客机会收到
                {
                    var m = NetProtocol.Unpack<MsgAck>(f.payload);
                    if (m == null || !m.ok)
                    {
                        // 🔴 **A961（2026-10-18）**：`MsgAck.reason`（`NetProtocol.cs:54`）**也是对端可控的** ——
                        //   它往下进 `StatusText`（下面那句 `SetState`）与 `OnClosed`（→ `NetMatchmaking` /
                        //   `NetBattle` 的提示行与弹窗）⇒ 与 `PeerName` / `MsgBye.reason` **同一处口径**：先钳。
                        string why = m != null && !string.IsNullOrEmpty(m.reason)
                                   ? ClampPeerText(m.reason) : "对面拒绝了连接";
                        LastError = why;
                        SetState(NetState.Closed, why);
                        _t.Close();
                        if (_checkMode && OnCheckDone != null) OnCheckDone(false, why);
                        if (OnClosed != null) OnClosed(why);
                        return;
                    }
                    SessionToken = m.sessionToken;
                    if (_checkMode && OnCheckDone != null) OnCheckDone(true, "连接成功 —— 可以直接开战了");
                    _checkMode = false;
                    if (_wasInBattle)
                    {
                        // 🔴 **重连的那一步**：报上钥匙 + 打到第几条，等主机灌权威动作流回来
                        Send(NetKind.Reconnect, new MsgReconnect { sessionToken = SessionToken, lastSeq = _lastSeq });
                        SetState(NetState.WaitingReconnect, "已经连上主机，正在等他补这一局的进度…");
                    }
                    else
                    {
                        SetState(NetState.Lobby, "连上主机了 —— 各自选好卡组就能开战");
                        if (OnPeerReady != null) OnPeerReady();
                    }
                    return;
                }
                case NetKind.Reconnect:                          // 主机收到：认 token ⇒ 灌动作流
                {
                    var m = NetProtocol.Unpack<MsgReconnect>(f.payload);
                    if (m == null || (SessionToken != null && m.sessionToken != SessionToken))
                    {
                        Send(NetKind.Bye, new MsgBye { reason = "这把钥匙对不上这一局" });
                        LastError = "重连被拒：钥匙对不上";
                        SetState(NetState.Closed, LastError);
                        _t.Close();
                        return;
                    }
                    if (ResumeProvider == null)
                    {
                        Send(NetKind.Bye, new MsgBye { reason = "主机这边没有这一局的记录" });
                        LastError = "重连被拒：主机没有权威动作流";
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
                    SetState(NetState.InBattle, $"「{PeerName}」回来了 —— 已把这一局的 {actions?.Count ?? 0} 条动作发过去");
                    _wasInBattle = false;                      // 追平了：再有人连进来就是新的一桌
                    return;
                }
                case NetKind.Resume:                             // 客机收到：全量重放
                {
                    var m = NetProtocol.Unpack<MsgResume>(f.payload);
                    if (m == null || m.start == null) { Close(true, "重连包解不出来"); return; }
                    var list = NetProtocol.Unpack<MsgActionList>(m.actionsJson);
                    var actions = list != null ? list.items : new List<MsgAction>();
                    SetState(NetState.InBattle, $"追上了 —— 重放这一局的 {actions.Count} 条动作");
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
                    string why = m != null && !string.IsNullOrEmpty(m.reason)
                               ? ClampPeerText(m.reason) : "对面退出了";
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
            SetState(NetState.InBattle, "对局中" + (Role == NetRole.Host ? "（本机是主机，动作由本机定序）" : "（客机：操作由主机确认）"));
        }

        /// <summary>主动收工（用户退出 / 打完）。`say=true` 时给对面捎一句 `bye`。
        /// 🔴 **2026-10-18（独立审查 R5）**：`say=true` 而**这一刻发不出去**时**要出声** ——
        /// 原来那一句写成 `if (say &amp;&amp; _t != null &amp;&amp; _t.IsConnected &amp;&amp; State != Off) Send(…)`，
        /// 条件不成立就**一声不响**（而 `Send` 新加的那声警告**永远走不到**这一步）⇒
        /// 「丢包一定出声」这条**恰好在「我们最需要对面收到的那句话」上不成立**。现在补上。</summary>
        public void Close(bool say = true, string reason = null)
        {
            if (say)
            {
                if (_t == null || !_t.IsConnected || State == NetState.Off)
                    Debug.LogWarning("[Net] 本来要给对面捎一句 `bye`（" + (reason ?? "对面结束了这一局") + "）"
                                   + $"—— 但**这一刻没有活的连接**（会话 {State}）⇒ 这句话**没发出去**，"
                                   + "对面**不会收到任何通知**（它那边只能靠心跳超时发现）。如实说出来（红线）");
                else
                    Send(NetKind.Bye, new MsgBye { reason = reason ?? "对面结束了这一局" });
            }
            // 🆕 关台时**把要来的那条映射撤掉**（别在玩家路由器上留一条没人用的转发规则）。
            //    端口要在 `Close()` 之前抓 —— 关完就取不到了。
            if (Role == NetRole.Host && _t != null) UpnpPortMapper.UnmapAsync(_t.Port);
            if (_t != null) _t.Close();
            SetState(NetState.Off, reason ?? "未连接");
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
