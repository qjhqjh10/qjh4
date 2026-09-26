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
        /// 光看 `IsConnected` 是**电平**，会让主机在等重连期间**反复重发握手包**（2026-09-26 实测到过）。</summary>
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
        volatile bool _connected, _peerLost;
        string _lastError = "";

        public bool IsListening { get { return _listener != null; } }
        public bool IsConnected { get { return _connected; } }
        public bool PeerLost { get { return _peerLost; } }
        public string LastError { get { return _lastError; } }
        public int Port { get; private set; }
        public int AcceptedCount { get; private set; }

        // ---- 主机 ----

        public void Listen(int port)
        {
            if (_listener != null) return;
            _peerLost = false;                        // 上一次会话留下的「掉过线」不该带到这一局
            try
            {
                // ⚠️ 只监 IPv4（`IPAddress.Any`）—— 局域网/端口映射那两条路都走 IPv4；
                //    双栈会带来「客机连的是 ::1 还是 127.0.0.1」这种与本项目无关的麻烦。
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "wf-net-accept" };
                _acceptThread.Start();
            }
            catch (Exception e)
            {
                _lastError = Describe(e, port);
                _listener = null;
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
                if (_client != null && _connected)
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
                var c = new TcpClient();
                c.NoDelay = true;
                var ar = c.BeginConnect(host, port, null, null);
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
                _connected = true;
                AcceptedCount++;                     // 🔴 上层靠它「边沿触发」新连接（见接口注释）
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
            _connected = false;
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

        /// <summary>读满 `count` 字节才返回；返回 `<=0` = 对面关了。</summary>
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
            _connected = false;
            _peerLost = true;                          // 「连过又断了」—— 与「还没连上」区分开
        }

        // ---- 发 ----

        public void Send(string kind, string payloadJson)
        {
            var s = _stream;
            if (s == null || !_connected) { _lastError = "还没连上，发不出去"; return; }
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
