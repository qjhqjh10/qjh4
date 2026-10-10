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
        ///    —— 落点 `NetSession.cs:247`（`_t.IsConnected &amp;&amp; (Role != NetRole.Host || _t.AcceptedCount == _lastAccepted)`；
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

        // 🔴 **A1268（2026-10-19）**：连接的【代】—— `Setup` 每开一条新连接就 +1。
        //    为什么要有它：`Setup` 关掉旧 socket 会**唤醒旧的那个读线程**，而它到底什么时候醒来
        //    **谁也说不准**（OS 调度）—— 它可能落在新连接的 `Publish(connected: true)` **之后**，
        //    于是**旧连接的收尾**（`Fail` → `Publish(false)` + `_peerLost = true`）
        //    会把**活着的新连接**标成「掉了」。有了这一代号，旧读线程就认得出自己已经过期
        //    （闸在 `Fail(int, string)` 里）。
        //    ⚠️ 与 `_state` 同理、**别声明成 `volatile`**（可见性由 `Volatile.Read` / `Interlocked` 负责）。
        int _gen;

        // 🔴 **A1268（2026-10-19）**：本机**正在主动拆**这条连接（`ClosePeer` / `Close` 之间）。
        //    为什么要有它：主动拆 socket 会让**我们自己的**读线程也失败一次 —— 那不是「掉线」，是收工。
        //    不区分的话，每一次正常收台（`NetSession.Close` / `Reset` / 自检里的清理）
        //    都会在日志里留下一条「掉线」告警 —— 那正是排查「为什么掉线」时最不该有的**假信号**。
        //    `Setup` 开新连接时清掉它。
        volatile bool _closing;

        // 🆕 🔴 **A1267（2026-10-19）**：后台读线程要说的「**为什么掉线**」攒在这儿，由**主线程** `Pump()` 取走打印。
        //    ⛔ **不能在 `Fail` 里直接 `Debug.Log`** —— `Fail` 的调用点里有一半在**读线程**上，
        //    而文件头那条规矩是「后台线程只碰 socket 与并发队列，**绝不碰 Unity API**」。
        //    （形状照抄 `_inbox`：后台入队、主线程出队。）
        //    `_noteWarn` = 真掉线（`Debug.LogWarning`）；`_noteLog` = 正常收尾，例如旧读线程随重连退出（`Debug.Log`）。
        readonly ConcurrentQueue<string> _noteWarn = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<string> _noteLog = new ConcurrentQueue<string>();

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

        /// <summary>🆕 **A1268 自检读口**：此刻这条连接是【第几代】（`Setup` 每开一条 +1）。
        /// ⚠️ 与 `NetSession.SinceLastRecvMsForTest` / `WasInBattleForTest` 同族：
        /// **生产路径一处都不读它**，只服务自检。</summary>
        public int GenerationForTest { get { return Volatile.Read(ref _gen); } }

        /// <summary>🆕 **A1268 自检口**：从**指定的那一代连接**报一次失败（`generation` 不等于当前代 ⇒ 旧连接迟到那一档）。
        ///
        /// <para>🔴 **为什么需要它**：「旧读线程迟到的 `Fail` 打在新连接上」那个交错
        /// （`资料/普查产出_第十一会话/F5_NetBattle夹具.md` §三 C · 候选 (iii)）**微秒级、而且不可控**
        /// —— 唤醒旧线程的是 `old.Close()` 那一下，醒来时机由 OS 调度决定 ⇒ 拿真 socket 跑一万次也撞不稳。
        /// 写成行为断言只会是「**永远绿（或永远红）的假断言**」，比没有更糟
        /// （同 `Editor/NetSelfTest.cs` §O 的 `_state`、§P 的 `ScriptedTransport` 那两条的理由）。
        /// 把「哪一代」当参数摆出来，那个交错就**每次都必现**。</para>
        /// ⚠️ **生产路径一处都不调它。**</summary>
        public void FailForTest(int generation, string why) { Fail(generation, why); }

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
            if (_listener != null)
            {
                // 🔴 **2026-10-18（第五轮 · 账 ②）**：重复 `Listen` **照旧被忽略**（行为一字不改 ——
                //    语义上那是对的：已经在听同一个端口了），但原来**一声不响** ⇒ 现在**说出来**。
                UnityEngine.Debug.LogWarning($"[Net] 已经在监听（端口 {Port}）⇒ 这一次 `Listen({port})` "
                                           + "**被忽略**（行为照旧：不重开、不叠加监听）—— 只是说出来，不静默");
                return;
            }
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
                _lastError = (dualStack ? Loc.T("Settings/Online/St/StackDual") : Loc.T("Settings/Online/St/StackV4Only")) + Describe(se, port);
                try { if (lis != null) lis.Stop(); } catch { }
                return false;
            }
            catch (Exception e)
            {
                _lastError = (dualStack ? Loc.T("Settings/Online/St/StackDual") : Loc.T("Settings/Online/St/StackV4Only")) + Describe(e, port);
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
            if (string.IsNullOrEmpty(host)) { _lastError = Loc.T("Settings/Online/St/NoIp"); return false; }
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
                    _lastError = string.Format(Loc.T("Settings/Online/St/ConnectTimeout"), host, port, timeoutMs);
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
                // 🔴 **A1268（2026-10-19）：先把【旧连接】作废，再关它。**
                //    ⛔ 这两句的**次序不能反**：下面那句 `old.Close()` **正是唤醒旧读线程的那一下**
                //       —— 先关再 +1 的话，旧线程那次小 `Fail` 会挤在「还没作废」的窗口里，
                //       照旧打到共享状态（连接位 / `_peerLost` / `_lastError`）上。
                int gen = Interlocked.Increment(ref _gen);
                var old = _client;                       // 掉线后重连：旧的那条先关掉，别把 socket 漏在那
                if (old != null && old != c) { try { old.Close(); } catch { } }
                _client = c;
                var st = c.GetStream();
                _stream = st;                            // ⚠️ 读线程**只认这一份**（下面按值传进 `ReadLoop`）
                _peerLost = false;
                _closing = false;                        // A1268：新开的这条是「活着」的，不是「正在拆」
                // 🔴 A943：**一次写**发布两个事实（原来这里是 `_connected = true;` + `AcceptedCount++;` 两句）。
                //    顺序仍是「连接位先可见」（见字段区那段：反过来会让上层消费掉握手边沿却发不出包）。
                Publish(connected: true, bumpCount: true);
                _lastError = "";
                // 🔴 **A1268**：读线程**按值**拿到「它属于哪一代 / 哪一条流」。
                //    原来是线程自己读 `var s = _stream;` —— 线程晚一点起来就会读到**新**的那条流
                //    （那时它就该退出了，却会去读一条不属于它的连接）。
                _readThread = new Thread(() => ReadLoop(gen, st)) { IsBackground = true, Name = "wf-net-read" };
                _readThread.Start();
            }
            catch (Exception e)
            {
                _lastError = string.Format(Loc.T("Settings/Online/St/NoStream"), e.Message);
                try { c.Close(); } catch { }
            }
        }

        /// <summary>只断**连接**，**保留监听** —— 主机等对面重连时要的就是这个
        /// （整条 `Close()` 会把监听也停掉，那主机就再也等不到人了）。</summary>
        public void ClosePeer()
        {
            // 🆕 🔴 **A1268**：这是**本机主动拆** ⇒ 下面我们自己那个读线程失败的那一次
            //   **不算「掉线」**（是收工），别在日志里留假信号。`Setup` 开新连接时清掉。
            _closing = true;
            try { if (_stream != null) _stream.Close(); } catch { }
            try { if (_client != null) _client.Close(); } catch { }
            _stream = null; _client = null;
            Publish(connected: false, bumpCount: false);   // A943：只清连接位，**计数是「累计」、不动**
        }

        // ---- 收 ----

        void ReadLoop(int gen, NetworkStream s)
        {
            var head = new byte[NetProtocol.HeaderBytes];
            // 🔴 **A1268**：`gen == _gen` = 「我这条流还是**当前**那条」—— `Setup` 换了新连接就**立刻退出**，
            //    连 `Fail` 都不叫（`Fail(int, string)` 里还有第二道闸，两道都要有：
            //    这一道管「别再读旧流」，那一道管「别改共享状态」）。
            while (s != null && gen == Volatile.Read(ref _gen))
            {
                int n;
                try { n = ReadFull(s, head, 0, NetProtocol.HeaderBytes); }
                catch (Exception e) { Fail(gen, string.Format(Loc.T("Settings/Online/St/ReadAbort"), e.Message)); return; }
                if (n <= 0) { Fail(gen, Loc.T("Settings/Online/St/PeerClosed")); return; }

                int len = NetProtocol.FrameLength(head, 0);
                if (len < 0 || len > NetProtocol.MaxFrame) { Fail(gen, string.Format(Loc.T("Settings/Online/St/BadFrame"), len)); return; }

                var body = new byte[len];
                try { n = ReadFull(s, body, 0, len); }
                catch (Exception e) { Fail(gen, string.Format(Loc.T("Settings/Online/St/ReadAbort"), e.Message)); return; }
                if (n <= 0) { Fail(gen, Loc.T("Settings/Online/St/PeerClosed")); return; }

                var env = NetProtocol.Parse(body, len);
                if (env == null) { Fail(gen, Loc.T("Settings/Online/St/BadEnvelope")); return; }
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

        /// <summary>🆕 🔴 **A1268（2026-10-19）**：`gen` = **这一次失败是从哪一代连接的上下文里报上来的**。
        /// 只有「还是当前那一代」才允许改共享状态（连接位 / `_peerLost` / `_lastError`）。</summary>
        void Fail(int gen, string why)
        {
            // 🔴 **A1268：旧连接迟到的失败 —— 共享状态一个字都不许碰。**
            //    为什么：`Publish(connected: false)` 会把**活着的新连接**标成断的、
            //    `_peerLost = true` 会让 `NetSession.Pump` 把新连接判成「对面掉线」、
            //    `_lastError` 会被写上一个**不属于当前连接**的原因（而 `NetSession` 拿它当掉线理由）。
            //    ✅ 但它**要出声** —— 「旧读线程什么时候退、为什么」正是排查重连/丢帧时缺的那一格
            //       （`资料/普查产出_第十一会话/F5_NetBattle夹具.md` §三 C · 候选 (iii)）。
            //    ⚠️ 级别是 `Debug.Log`（不是警告）：**每一次重连**都会有一个旧读线程这样退出，是正常收尾。
            if (gen != Volatile.Read(ref _gen))
            {
                _noteLog.Enqueue("[Net] 旧连接的读线程退出了（原因：" + why + "）—— 它属于第 " + gen
                               + " 代连接、当前是第 " + Volatile.Read(ref _gen)
                               + " 代 ⇒ **与现在这条活着的连接无关**，一律不改共享状态（A1268）");
                return;
            }

            if (_lastError == "") _lastError = why;
            Publish(connected: false, bumpCount: false);   // A943：同上 —— 掉线只清连接位
            _peerLost = true;                          // 「连过又断了」—— 与「还没连上」区分开

            // 🔴 **A1267（2026-10-19）：这一行原来【没有】。** `Fail` 只写 `_lastError`、一行日志都不打
            //    ⇒ **「为什么掉线」永远不进日志**（违反「不许静默失败」；`F5` 的 X2「`bye` 丢在哪一跳」
            //    就卡在这一格上）。
            //    ⚠️ **`_lastError` 的消费面如实说清**（`F5` §三 B 那句「全仓唯一消费点是 `NetSession.Send`」
            //      **不准确**：`StartHost` / `InternalConnect` / 重连失败那三处也会把它拼进状态字）——
            //      但**在掉线这条路上**它确实等于没人看：`NetSession.Pump` 把它读进 `why` 之后，
            //      「对局中」那一支印的是**固定词条** `St/PeerLostInBattle`（**不含原因**），
            //      而 `NetSession.LastError` 在被掉线覆盖的那几条路上**没有任何界面读它**
            //      （`Shell/SettingsWindow` 只在「主机没起来」那一支读它）⇒ 原因等于石沉大海。
            if (_closing)
            {
                // 本机**自己刚拆的**（`ClosePeer` / `Close`）⇒ 这不是「掉线」，是收工 —— **不出声**。
                // ⚠️ 这**不是**静默失败：那三条路**各自**已经出过声了 ——
                //    · 静默超时支 → `SetState(WaitingReconnect, 「N 秒没收到对面的任何消息」)`；
                //    · 客机重连支 → `SetState(WaitingReconnect, 「正在重连主机…」)`；
                //    · `NetSession.Close` → `SetState(Off, …)`。
                //    在这里再喊一句「掉线」只会给排查制造**假信号**。
            }
            else
            {
                // 🔑 **这一行是给【排查】看的**（自检 / `Player.log`）——
                //    ⛔ **别把原因拼进界面文案**：玩家那一侧看到的是 `St/PeerLostInBattle` 这种**固定词条**，
                //      而原版那一刻也是固定词条（`Battle/HUD/WaitOpponentConnectionMsg，13 个战场各一份`）
                //      ⇒ 这属于铁律 11 的第一类「**原版本身就没有**」，不要在 UI 上自造第二份。
                _noteWarn.Enqueue("[Net] 传输层掉线：原因 = " + why
                                + " —— 这一行是给**排查**看的（自检 / `Player.log`）；玩家那一侧只有固定词条的状态字"
                                + "（原版同：`Battle/HUD/WaitOpponentConnectionMsg`），**不含原因**（A1267）");
            }
        }

        /// <summary>当前那一条连接上的失败（`Send` 的 `catch` 走它 —— 那个调用点本来就在当前连接上）。</summary>
        void Fail(string why) { Fail(Volatile.Read(ref _gen), why); }

        // ---- 发 ----

        public void Send(string kind, string payloadJson)
        {
            var s = _stream;
            if (s == null || !IsConnected)
            {
                // 🔴 **2026-10-18（账：「`Send()` 静默丢」的同族，第 2 处）**：原来这里**只写 `_lastError`
                //    就返回** —— 一条包在这里没了，**日志里一个字都没有**（红线：不许静默失败）。现在出声。
                // ✅ **2026-10-18（第五轮 · 账 ①）**：`_lastError` 也**有消费点了** —— `NetSession.Send`
                //    在调用前后各读一次，变了就出声 + 返回 `false`（不再有「写了没人看」的字段）。
                //
                // 🔴 **口径对齐（第五轮 · 账 ④ 的判据，现读全量反编译）**：原版「发不出去那一刻」
                //    **零 UI**，只有一条日志 —— `PhotonNetwork.VerifyCanUseNetwork()`
                //    （`d:/2/tools/decomp_full/PhotonNetwork__VerifyCanUseNetwork.c`）：
                //      `if (!PhotonNetwork.connected) { UnityEngine.Debug.LogError(DAT_1842c4e58); return 0; }`
                //    字面量 `DAT_1842c4e58` 按「VA − ImageBase(0x180000000) → 查
                //    `d:/2/tools/il2cpp_out/stringliteral.json`」解出来 =
                //    **`"Cannot send messages when not connected. Either connect to Photon OR use offline mode!"`**
                //    （同一映射用两个**已知答案**验证过：`DAT_18425d4a8` → `CustomErrors/InternetUnreachable`、
                //      `DAT_1842be718` → `MainMenu/General/OK`）。
                //    `PhotonNetwork.RPC` 里另外两条同族也全是日志、**没有一处 `WindowsManager`**：
                //      `:43 LogError`（"RPC can't be sent to target PhotonPlayer being null! …"）、
                //      `:67 LogWarning`（"RPCs can only be sent in rooms. …"）；`NetworkingPeer.RPC` 同。
                //    ⇒ **我们这里是 `LogWarning`（级别与原版的 `LogError` 差一档，如实标出）**，
                //      **不弹窗** —— 因为原版本身就没有那一扇窗（铁律 11 的「原版本身没有」那一类）。
                //    ⚠️ 别把这一档与「**对手掉线**弹固定词条 `Battle/HUD/WaitOpponentConnectionMsg`」混起来：
                //      那是**另一件事**（B17 已接，走 `NetRuntime.Notice`），与本档无关。
                _lastError = Loc.T("Settings/Online/St/NotConnected");
                // ⚠️ 本文件**没有 `using UnityEngine`**（线程规矩见文件头：后台线程不许碰 Unity API）——
                //    这里跟同文件的 `ResolveHost` 一样**全限定**写。`Send` 只由主线程调
                //    （唯一入口 = `NetSession.Send`，它跑在 `Pump()` / 各 handler / `Close()` 里）。
                UnityEngine.Debug.LogWarning("[Net] 这条 `" + kind + "` **没发出去**（传输层：" + _lastError + "）"
                                           + " —— 这一刻连接位是断的（`IsConnected == false`）");
                return;
            }
            byte[] buf = NetProtocol.Frame(kind, payloadJson);
            try
            {
                lock (_sendLock) { s.Write(buf, 0, buf.Length); s.Flush(); }
            }
            catch (Exception e)
            {
                // 🔴 **2026-10-18（第五轮 · 账 ①）**：这一支是「**过了守卫、但底层真写失败**」——
                //    `Fail` 只把原因写进 `_lastError`。**加上了消费点**：`NetSession.Send` 会在调用前后
                //    各读一次 `LastError`，一旦**变了**就出声 + 返回 `false`（见那边的注释）。
                //    ⇒ 这一档**不再被吞**（原来是「生产路径没人读 `_lastError`」—— 那句已经不成立了）。
                // ⚠️ 为什么走「加消费点」而不是「把 `INetTransport.Send` 改成 `bool`」：见交件报告 §13·①
                //    （改动面：`RecordingTransport` / `ScriptedTransport` / `TcpTransport` 三个实现 + 全部调用点）。
                Fail(string.Format(Loc.T("Settings/Online/St/SendFailed"), e.Message));
            }
        }

        public int Pump(List<NetFrame> into)
        {
            // 🆕 🔴 **A1267（2026-10-19）**：**先**把后台读线程攒下的话说完 —— 必须在 `into == null`
            //    那道早退**之前**（`NetSession` 永远传非 null，但这是公开口，别让这一支吞话）。
            //    ⚠️ **打印只在这里**：本方法只由**主线程**调（`NetSession.Pump` ← `NetRuntime.Update` / 自检显式推）
            //       —— `Fail` 那四个调用点里有一半在读线程上，所以「说」和「打印」必须分开。
            string note;
            while (_noteWarn.TryDequeue(out note)) UnityEngine.Debug.LogWarning(note);
            while (_noteLog.TryDequeue(out note)) UnityEngine.Debug.Log(note);

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
                    return string.Format(Loc.T("Settings/Online/St/PortBusy"), port);
                if (se.SocketErrorCode == SocketError.ConnectionRefused)
                    return string.Format(Loc.T("Settings/Online/St/ConnRefused"), port);
                if (se.SocketErrorCode == SocketError.HostNotFound || se.SocketErrorCode == SocketError.NoData)
                    return Loc.T("Settings/Online/St/HostNotFound");
                return string.Format(Loc.T("Settings/Online/St/SocketError"), se.SocketErrorCode);
            }
            return e.Message;
        }
    }
}
