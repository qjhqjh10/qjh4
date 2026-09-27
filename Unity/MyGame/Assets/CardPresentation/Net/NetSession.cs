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

        static long NowMs { get { return _sw.ElapsedMilliseconds; } }
        static readonly System.Diagnostics.Stopwatch _sw = System.Diagnostics.Stopwatch.StartNew();

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

            // ---- 心跳 ----
            if (State != NetState.WaitingReconnect && State != NetState.Connecting)
            {
                if (now - _lastPingMs >= HeartbeatMs)
                {
                    _lastPingMs = now;
                    Send(NetKind.Ping, new MsgPing { t = now });
                }
                if (_t.IsConnected && now - _lastRecvMs >= SilentTimeoutMs)
                {
                    // 静默太久 = 对面没了（拔网线这种不会触发 TCP 的 FIN）⇒ 当作掉线
                    // ⚠️ 用 `ClosePeer` 而**不是** `Close` —— 主机要把监听留着，否则等不到重连
                    _t.ClosePeer();
                    _wasInBattle = State == NetState.InBattle;
                    SetState(NetState.WaitingReconnect, "对手掉线了，正在等他回来…（对局已暂停）");
                    LastError = $"{SilentTimeoutMs / 1000} 秒没收到对面的任何消息";
                    if (OnPeerLost != null) OnPeerLost();
                }
            }

            // ---- 主机：有新连接进来就握手（**边沿触发**：看 `AcceptedCount` 变没变，
            //      别看 `IsConnected` —— 那是电平，会在等重连期间**反复重发握手包**）----
            if (Role == NetRole.Host && _t.AcceptedCount != _lastAccepted)
            {
                _lastAccepted = _t.AcceptedCount;
                _lastRecvMs = now;
                bool wasReconnect = _wasInBattle;
                _nonce = NewToken();
                Send(NetKind.Challenge, new MsgChallenge { nonce = _nonce });
                SetState(NetState.Handshaking, wasReconnect
                         ? "有连接进来，正在核对是不是刚才那个人…" : "有客机连进来了，正在核对…");
            }

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
                    PeerName = m != null ? m.name : null;
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
                        string why = m != null && !string.IsNullOrEmpty(m.reason) ? m.reason : "对面拒绝了连接";
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
                    string why = m != null && !string.IsNullOrEmpty(m.reason) ? m.reason : "对面退出了";
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

        public void Send(string kind, string payloadJson)
        {
            if (_t == null || !_t.IsConnected) return;         // 没连上就丢：调用方看 StatusText/LastError
            _t.Send(kind, payloadJson);
        }

        public void Send<T>(string kind, T msg) { Send(kind, NetProtocol.Pack(msg)); }

        /// <summary>进对局（上层在 `Begin` 之后调）。之后掉线才走「等重连」。</summary>
        public void EnterBattle(int lastSeq = 0)
        {
            _lastSeq = lastSeq;
            _wasInBattle = false;                      // 新的一局：掉线前的「他是我对手」标记清掉
            SetState(NetState.InBattle, "对局中" + (Role == NetRole.Host ? "（本机是主机，动作由本机定序）" : "（客机：操作由主机确认）"));
        }

        /// <summary>主动收工（用户退出 / 打完）。`say=true` 时给对面捎一句 `bye`。</summary>
        public void Close(bool say = true, string reason = null)
        {
            if (say && _t != null && _t.IsConnected && State != NetState.Off)
                Send(NetKind.Bye, new MsgBye { reason = reason ?? "对面结束了这一局" });
            // 🆕 关台时**把要来的那条映射撤掉**（别在玩家路由器上留一条没人用的转发规则）。
            //    端口要在 `Close()` 之前抓 —— 关完就取不到了。
            if (Role == NetRole.Host && _t != null) UpnpPortMapper.UnmapAsync(_t.Port);
            if (_t != null) _t.Close();
            SetState(NetState.Off, reason ?? "未连接");
            _checkMode = false;
            _wasInBattle = false;
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
