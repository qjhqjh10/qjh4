// NetTransport.cs — TCP 传输层：**组帧收发 + 后台线程 + 主线程 Pump**
//
// 为什么是 TCP（用户 2026-09-26 拍板「自写最小协议跑 TCP」）：
//   · 回合制卡牌的消息量极小（一条动作几十字节），**延迟不是瓶颈**；
//   · TCP 天然**可靠 + 有序** ⇒ 省掉自己实现「可靠 UDP」（重传/去重/排序）那一整套；
//   · 零第三方依赖 ⇒ 工程里不引入 DLL，自检能直接跑。
//   ⚠️ 将来若要 NAT 穿透/低延迟，**只换这一层**（`INetTransport` 后面），上层协议不动。
//
// 🔴 **批处理下没有帧循环** ⇒ 收包一律靠调用方**显式 `Pump()`**（`NetSelfTest` 就是这么推的）。
//    ⚠️ 真 Play 里若要边走边收：Unity 在窗口失焦时会暂停 `Update` ⇒ 外壳那层要设
//    `Application.runInBackground = true`（`NetSession.EnsureBackground`）。
//
// 线程规矩：**后台线程只碰 socket 与并发队列，绝不碰 Unity API**（`Debug.Log` 由主线程打）。
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CardPresentation.Net
{
    /// <summary>一帧收到的消息（kind + payload JSON）。</summary>
    public struct NetFrame
    {
        public string kind;
        public string payload;
    }

    public interface INetTransport
    {
        /// <summary>主机：监听中。客机：一直为 false。</summary>
        bool IsListening { get; }
        /// <summary>此刻有一条活的连接。</summary>
        bool IsConnected { get; }
        /// <summary>曾经连上过、现在断了（**掉线** —— 触发重连那条路，不是「还没连上」）。</summary>
        bool PeerLost { get; }
        /// <summary>最近一次失败的原因（人话，可直接显示）。</summary>
        string LastError { get; }
        /// <summary>本机监听的端口（客机为 0）。</summary>
        int Port { get; }
        /// <summary>**累计接受过几条连接**（每次 `Setup` +1）。
        /// 🔴 上层用它做**边沿触发**：「有新连接进来」这件事只能看这个数变没变 ——
        /// 光看 `IsConnected` 是**电平**，会让主机在等重连期间**反复重发握手包**（2026-09-26 实测到过）。
        /// 🔴 它与 `IsConnected` **由同一次写发布**（A943）—— 见 `TcpTransport` 里 `_state` 那段。
        /// ✅ **读侧也闭环了**（2026-10-18，同一条账 A943）：接口本身仍是两次读（`NetSession.Pump`
        ///    先读这个数、再读 `IsConnected`），但**要「两个事实一致」的那处已经补了确认**
        ///    —— 落点 `NetSession.cs:247`（`_t.IsConnected && (Role != NetRole.Host || _t.AcceptedCount == _lastAccepted)`；
        ///    那半句只对主机成立 —— 客机的 `_lastAccepted` 没人写）。
        ///    ⚠️ 新代码要用这两个事实判事，**照那一处抄**，别自己分两次读就下结论。</summary>
        int AcceptedCount { get; }

        void Listen(int port);
        bool Connect(string host, int port, int timeoutMs);
        void Send(string kind, string payloadJson);
        /// <summary>主线程调：把收到的帧全部倒进 `into`，返回条数。</summary>
        int Pump(List<NetFrame> into);
        /// <summary>只断连接、**保留监听**（主机等重连用）。</summary>
        void ClosePeer();
        /// <summary>整条收工（监听也停）。</summary>
        void Close();
    }

    public class TcpTransport : INetTransport
    {
        TcpListener _listener;
        TcpClient _client;
        NetworkStream _stream;
        Thread _acceptThread, _readThread;
        readonly ConcurrentQueue<NetFrame> _inbox = new ConcurrentQueue<NetFrame>();
        readonly object _sendLock = new object();
        volatile bool _peerLost;
        string _lastError = "";

        // 🔴 **A943（2026-10-18）：连接位与接受计数【同处一个 32 位字、由一次 CAS 一起发布】**。
        //    原来 `Setup` 里是 `_connected = true;` 紧接着 `AcceptedCount++;` —— **两条独立的写**，
        //    而写它的是**接受线程**（`AcceptLoop` → `Setup`）、读它的是**主线程**
        //    （`NetSession.Pump`，每帧 / 自检里显式调）⇒ 存在一个「已连接、但计数还没加」的**中间态**。
        //    读侧恰好撞上那个中间态时（`Pump:213` 读到旧计数 ⇒ 不刷新静默基线；
        //    `Pump:232` 读到连接位已立 ⇒ 把一条**刚连上、一条包都还没发**的连接判成
        //    「10 秒没收到对面的任何消息」并 `ClosePeer`）——**掐掉一条好连接**
        //    （同一族的日志实证见 `资料/普查产出_1017/D4_联机诊断.md` §②）。
        //    ⇒ 现在两个事实**同处一个字**：要么都看得见、要么都看不见，**不存在半次发布**。
        //    ⚠️ 顺序**不能改成「先加计数、后置连接位」**（那会更糟）：上层是**边沿触发**
        //       （`NetSession.Pump:213-215` 看到计数变了就 `_lastAccepted = …` 并**立刻**发 `Challenge`），
        //       那一刻 `Send`（本文件 `:300` 一带）会看到「还没连上」⇒**握手包发不出去、边沿却被消费掉了**
        //       ⇒ **永久握不上**。所以「连接位先可见」这条顺序是**必须保持**的 —— 现在由同一个字保证。
        //    ✅ **2026-10-18 读侧也闭环了**（原来这一行写的是「还剩半边、那一段在 `NetSession.cs`、
        //       不在本案白名单里」—— 这一轮补上了）：`NetSession.cs:247` 那道静默超时判据补了
        //       `(Role != NetRole.Host || _t.AcceptedCount == _lastAccepted)` ⇒「**计数还是我认过的那个**」
        //       才说明**没有新发布夹在上面那次计数读（`NetSession.cs:213`）与这次连接位读之间**
        //       ⇒ 不会拿「旧基线 + 新连接位」掐掉一条刚连上的连接。
        //       🔴 那个 `Role` 项**不能省**：客机的 `_lastAccepted` 没人写（停在 -1）、而它的计数是 1
        //          （`Setup` 在客机 `Connect()` 里也跑）⇒ `1 == -1` 恒假 ⇒ **客机的静默超时会永久关掉**。
        //       **本账闭环 = 写侧一个字（本文件）+ 读侧一次确认（`NetSession`）**。
        //       判别式见 `NetSelfTest` §P（可注入传输把那个纳秒窗口摆出来；P⑫ 钉客机那半边）。
        //    ⚠️ **别把这个字段声明成 `volatile`** —— 可见性已由 `Volatile.Read` / `Interlocked` 负责，
        //       声明成 volatile 反而会让 `ref` 传参报 CS0420。
        int _state;                        // bit0 = 有活连接；bit1..31 = 累计接受过几条连接
        const int ConnBit = 1;
        const int CountStep = 2;           // 计数左移一位：加它**永远碰不到**连接位（也不用进位）

        public bool IsListening { get { return _listener != null; } }
        public bool IsConnected { get { return (Volatile.Read(ref _state) & ConnBit) != 0; } }
        public bool PeerLost { get { return _peerLost; } }
        public string LastError { get { return _lastError; } }
        public int Port { get; private set; }
        public int AcceptedCount { get { return Volatile.Read(ref _state) >> 1; } }

        /// <summary>A943：**原子地**翻状态字（读-改-写一次完成）。
        /// `connected` = 置/清连接位；`bumpCount` = 计数 +1。两者**在同一次写里落地**。</summary>
        void Publish(bool connected, bool bumpCount)
        {
            while (true)
            {
                int cur = Volatile.Read(ref _state);
                int next = bumpCount ? cur + CountStep : cur;
                next = connected ? (next | ConnBit) : (next & ~ConnBit);
                if (Interlocked.CompareExchange(ref _state, next, cur) == cur) return;
            }
        }

        // ---- 主机 ----

        public void Listen(int port)
        {
            if (_listener != null) return;
            _peerLost = false;                        // 上一次会话留下的「掉过线」不该带到这一局

            // 🔴 **2026-09-26 改成双栈**。原来是「只监 IPv4（`IPAddress.Any`）」，理由写的是
            //    「双栈会带来『客机连的是 ::1 还是 127.0.0.1』这种与本项目无关的麻烦」——
            //    ⚠️ **那个理由站不住**：**有公网 IPv6 的玩家不必做端口映射就能直连**，
            //    而这正是「网友联机」最省事的一条路（见 `资料/联机P2P_设计与交接.md`）。
            //    ⇒ 现在**先试双栈**（一张 socket 同时收 v4/v6），不行再退回 IPv4。
            // 🔴 **但「端口被占」不能退回 IPv4** —— 退了会**在同一端口上起出第二台主机**
            //    （v6 双栈占的是 v6 的那一份，v4 还能绑上）⇒ 自检 B② 当场抓到过。
            //    只有「双栈这条路本身不可用」才该退回。
            if (TryListen(port, true, out bool portBusy)) return;
            if (portBusy) return;                     // 端口被占 ⇒ `_lastError` 已填好，**别再试 IPv4**
            TryListen(port, false, out _);
            // 两条都失败 ⇒ `_lastError` 已由最后一次填好（**不许静默**）
        }

        /// <summary>开监听。`dualStack = true` ⇒ **一张 socket 同时收 IPv4 与 IPv6**。
        /// 失败返回 `false` 并把原因写进 `_lastError`；`portBusy` 报「是不是端口被占了」。</summary>
        bool TryListen(int port, bool dualStack, out bool portBusy)
        {
            portBusy = false;
            TcpListener lis = null;
            try
            {
                if (dualStack)
                {
                    lis = new TcpListener(IPAddress.IPv6Any, port);
                    lis.Server.DualMode = true;       // 关掉「只收 v6」⇒ v4 的客机也连得进来
                }
                else lis = new TcpListener(IPAddress.Any, port);

                lis.Start();
                _listener = lis;
                Port = ((IPEndPoint)lis.LocalEndpoint).Port;
                _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "wf-net-accept" };
                _acceptThread.Start();
                return true;
            }
            catch (SocketException se)
            {
                portBusy = se.SocketErrorCode == SocketError.AddressAlreadyInUse;
                _lastError = (dualStack ? "（双栈）" : "（仅 IPv4）") + Describe(se, port);
                try { if (lis != null) lis.Stop(); } catch { }
                return false;
            }
            catch (Exception e)
            {
                _lastError = (dualStack ? "（双栈）" : "（仅 IPv4）") + Describe(e, port);
                try { if (lis != null) lis.Stop(); } catch { }
                return false;
            }
        }

        void AcceptLoop()
        {
            var lis = _listener;
            while (lis != null)
            {
                TcpClient c = null;
                try { c = lis.AcceptTcpClient(); }
                catch { return; }                      // 监听被关掉 = 正常退出
                if (_client != null && IsConnected)
                {
                    // 已经有**活的**对家了 ⇒ 多出来的直接关掉（v1 只支持 1v1，不排队）
                    try { c.Close(); } catch { }
                    continue;
                }
                Setup(c);
            }
        }

        // ---- 客机 ----

        public bool Connect(string host, int port, int timeoutMs)
        {
            if (string.IsNullOrEmpty(host)) { _lastError = "没有填 IP 地址"; return false; }
            try
            {
                // 🔴 **2026-09-26：必须按地址族建 socket。**
                //    `new TcpClient()` 在 Unity(Mono) 里建出来的是 **IPv4** socket
                //    ⇒ **拿 IPv6 地址去连必定失败**。而「公网 IPv6 直连」正是用户选的那条路
                //    ⇒ 这一条不通，整条路就是死的。
                //    做法：**先把地址解析出来**（字面量优先），再按它的 `AddressFamily` 建客户端。
                var ip = ResolveHost(host);
                var c = ip != null ? new TcpClient(ip.AddressFamily) : new TcpClient();
                c.NoDelay = true;
                var ar = ip != null ? c.BeginConnect(ip, port, null, null)
                                    : c.BeginConnect(host, port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(timeoutMs))
                {
                    try { c.Close(); } catch { }
                    _lastError = $"连接 {host}:{port} 超时（{timeoutMs} 毫秒）—— 对面没开主机，或防火墙挡住了";
                    return false;
                }
                c.EndConnect(ar);                     // 失败会在这里抛
                Setup(c);
                return true;
            }
            catch (Exception e)
            {
                _lastError = Describe(e, port);
                return false;
            }
        }

        /// <summary>把玩家填的那串解析成一个地址：**先当字面量**（`192.168.1.10` · `2001:db8::1` ·
        /// `[::1]`），解析不出来**才**查 DNS。
        /// ⚠️ **别只走 `Dns.GetHostAddresses`** —— 本工程在本机名那条路上踩过
        /// （见 `NetConfig.LocalIPv4` 的历史：`Dns.GetHostAddresses(Dns.GetHostName())` 抛
        /// `Illegal byte sequence`）。而且**字面量走 DNS 是白绕一圈**。
        /// ⚠️ 解析出多个时**优先 IPv4**（局域网那条路最常见），一个 v4 都没有才用 IPv6。</summary>
        public static IPAddress ResolveHost(string host)
        {
            if (string.IsNullOrEmpty(host)) return null;
            string h = host.Trim();
            if (h.Length > 1 && h[0] == '[' && h[h.Length - 1] == ']') h = h.Substring(1, h.Length - 2);
            IPAddress lit;
            if (IPAddress.TryParse(h, out lit)) return lit;          // 字面量（含 IPv6）
            try
            {
                var all = Dns.GetHostAddresses(h);
                foreach (var a in all) if (a.AddressFamily == AddressFamily.InterNetwork) return a;
                if (all.Length > 0) return all[0];
            }
            catch (Exception e) { UnityEngine.Debug.LogWarning($"[Net] 解析「{host}」失败：" + e.Message); }
            return null;
        }

        void Setup(TcpClient c)
        {
            try
            {
                c.NoDelay = true;
                var old = _client;                       // 掉线后重连：旧的那条先关掉，别把 socket 漏在那
                if (old != null && old != c) { try { old.Close(); } catch { } }
                _client = c;
                _stream = c.GetStream();
                _peerLost = false;
                // 🔴 A943：**一次写**发布两个事实（原来这里是 `_connected = true;` + `AcceptedCount++;` 两句）。
                //    顺序仍是「连接位先可见」（见字段区那段：反过来会让上层消费掉握手边沿却发不出包）。
                Publish(connected: true, bumpCount: true);
                _lastError = "";
                _readThread = new Thread(ReadLoop) { IsBackground = true, Name = "wf-net-read" };
                _readThread.Start();
            }
            catch (Exception e)
            {
                _lastError = "连上了但拿不到流：" + e.Message;
                try { c.Close(); } catch { }
            }
        }

        /// <summary>只断**连接**，**保留监听** —— 主机等对面重连时要的就是这个
        /// （整条 `Close()` 会把监听也停掉，那主机就再也等不到人了）。</summary>
        public void ClosePeer()
        {
            try { if (_stream != null) _stream.Close(); } catch { }
            try { if (_client != null) _client.Close(); } catch { }
            _stream = null; _client = null;
            Publish(connected: false, bumpCount: false);   // A943：只清连接位，**计数是「累计」、不动**
        }

        // ---- 收 ----

        void ReadLoop()
        {
            var s = _stream;
            var head = new byte[NetProtocol.HeaderBytes];
            while (s != null)
            {
                int n;
                try { n = ReadFull(s, head, 0, NetProtocol.HeaderBytes); }
                catch (Exception e) { Fail("读取中断：" + e.Message); return; }
                if (n <= 0) { Fail("对面关掉了连接"); return; }

                int len = NetProtocol.FrameLength(head, 0);
                if (len < 0 || len > NetProtocol.MaxFrame) { Fail($"帧长度不合理（{len} 字节）—— 对面发的不是本协议的帧"); return; }

                var body = new byte[len];
                try { n = ReadFull(s, body, 0, len); }
                catch (Exception e) { Fail("读取中断：" + e.Message); return; }
                if (n <= 0) { Fail("对面关掉了连接"); return; }

                var env = NetProtocol.Parse(body, len);
                if (env == null) { Fail("收到的帧解不出信封"); return; }
                _inbox.Enqueue(new NetFrame { kind = env.kind, payload = env.payload });
            }
        }

        /// <summary>读满 `count` 字节才返回；返回 `&lt;=0` = 对面关了。</summary>
        static int ReadFull(NetworkStream s, byte[] buf, int off, int count)
        {
            int got = 0;
            while (got < count)
            {
                int n = s.Read(buf, off + got, count - got);
                if (n <= 0) return got;
                got += n;
            }
            return got;
        }

        void Fail(string why)
        {
            if (_lastError == "") _lastError = why;
            Publish(connected: false, bumpCount: false);   // A943：同上 —— 掉线只清连接位
            _peerLost = true;                          // 「连过又断了」—— 与「还没连上」区分开
        }

        // ---- 发 ----

        public void Send(string kind, string payloadJson)
        {
            var s = _stream;
            if (s == null || !IsConnected) { _lastError = "还没连上，发不出去"; return; }
            byte[] buf = NetProtocol.Frame(kind, payloadJson);
            try
            {
                lock (_sendLock) { s.Write(buf, 0, buf.Length); s.Flush(); }
            }
            catch (Exception e) { Fail("发送失败：" + e.Message); }
        }

        public int Pump(List<NetFrame> into)
        {
            if (into == null) return 0;
            int n = 0;
            NetFrame f;
            while (_inbox.TryDequeue(out f)) { into.Add(f); n++; }
            return n;
        }

        public void Close()
        {
            var lis = _listener; _listener = null;
            try { if (lis != null) lis.Stop(); } catch { }
            ClosePeer();
            _acceptThread = null; _readThread = null;   // 线程都是后台线程，socket 关掉后自己会退
        }

        static string Describe(Exception e, int port)
        {
            var se = e as SocketException;
            if (se != null)
            {
                if (se.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    return $"端口 {port} 已被占用 —— 换一个端口，或先关掉已经在跑的那个实例";
                if (se.SocketErrorCode == SocketError.ConnectionRefused)
                    return $"对面拒绝了连接（{port} 端口没人在听）—— 主机那边要先点「保存」并保持游戏开着";
                if (se.SocketErrorCode == SocketError.HostNotFound || se.SocketErrorCode == SocketError.NoData)
                    return "这个 IP 地址找不到——检查一下有没有抄错";
                return "网络错误：" + se.SocketErrorCode;
            }
            return e.Message;
        }
    }
}
