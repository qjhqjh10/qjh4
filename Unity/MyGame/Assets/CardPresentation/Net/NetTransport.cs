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
        ///    —— 落点 `NetSession.Pump()` 里那道静默超时判据（`_t.IsConnected &amp;&amp; (Role != NetRole.Host || _t.AcceptedCount == _lastAccepted)`；
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

    /// <summary>🆕 🔴 **2026-10-20（W58-NETFIX · NETPROBE ⑤）：能丢弃「还没被上层取走」的收件帧的传输层**。
    ///
    /// <para>为什么是**单独的可选接口**、而不是往 `INetTransport` 加一个成员：那个接口还有
    /// `Editor/BattleScene.cs` 的 `RecordingTransport` 与 `Editor/NetSelfTest.cs` 的 `ScriptedTransport`
    /// 两个实现，而本轮白名单**不含 `Editor/`** ⇒ 往接口里加**抽象成员**会让编辑期程序集编不过；
    /// 用**默认接口实现（DIM）**又踩 Unity Mono 的运行时支持面（⛔ 不拿「大概支持」下注）。
    /// ⇒ 能力接口 + 类型探测：真 TCP 走它，夹具不实现就照旧（夹具没有跨帧积压的语义）。
    /// ⛔ 别把它扩成 `INetTransport` 的必选成员。</para></summary>
    public interface IPendingFrameDropper
    {
        /// <summary>丢掉收件箱里尚未被上层取走的帧，返回丢弃条数（0 = 本来就空）。
        /// `cause` = 谁要求清的（进日志，便于真连时分辨 `Close` / 自检显式调用）。</summary>
        int DropPendingFrames(string cause);
    }

    /// <summary>🆕 🔴 **2026-10-20（W58-S6FIX · 缺陷 S6）：主机已有活连接时，能不能把新连接先收作「待替换候选」**。
    ///
    /// <para><b>为什么要有它</b>：`AcceptLoop` 原来的行为是「`ConnBit` 立着 ⇒ 把新连接**直接关掉**」。
    /// 而「旧连接已经死了、只是本机还没判出来」正是**客机重连**最常见的那一档 —— 客机的
    /// `Connect()` 会成功、主机这边却先把新 socket 关了，客机只看到「连上又断」。这就是 S6。</para>
    ///
    /// <para><b>为什么不是「谁连上来就顶替谁」</b>：那等于**任意第三方**（只要连得上端口）就能
    /// 一脚把正在打的对手踢掉。⇒ 替换必须**先出凭据**：主机在新连接上发一次
    /// `Challenge` → 对方回 `Proof`（HMAC(房间密码, nonce)）→ 对局中的替换还**必须**再出
    /// `Reconnect{sessionToken}`（这一局的钥匙，认不出 = 不是这局的人 ⇒ 拒绝）。**凭据齐了才提升**；
    /// 任何一步失败 ⇒ 候选关掉、**旧连接原样保留**。</para>
    ///
    /// <para>⚠️ 与 `IPendingFrameDropper` 同族：**能力接口 + 类型探测**，⛔ 别把成员加进
    /// `INetTransport`（那会让 `Editor/` 的两个夹具实现编不过；本轮白名单不含它们）。
    /// 夹具不实现它 ⇒ 行为与加这一段之前**逐字相同**。</para></summary>
    public interface IPendingReplacementHost
    {
        /// <summary>上层说「此刻允许把新连接收作候选」（主机、且在对局中 / 等重连）。
        /// 由**主线程**写、**接受线程**读 ⇒ 实现里必须是 `volatile`。⚠️ 没 armed 时照旧拒收（S6 的旧行为，
        /// 大厅/握手阶段没有「重连」这回事，不让陌生连接占候选位）。</summary>
        bool ReplacementArmed { get; set; }

        /// <summary>此刻手里有一条**还没被凭据确认**的候选连接。</summary>
        bool HasPendingReplacement { get; }
        /// <summary>候选连接的代次（每收一条新的 +1；换了一条 ⇒ 上层要重发 `Challenge`）。</summary>
        int PendingReplacementGeneration { get; }
        /// <summary>候选已经挂了多久（毫秒；上层据此判「凭据迟迟不来」⇒ 超时关掉）。</summary>
        long PendingReplacementAgeMs { get; }
        /// <summary>候选的对端地址（只进日志，便于同机两进程排查）。</summary>
        string PendingReplacementPeer { get; }
        /// <summary>自检读口：被提升成「当前连接」的候选数。</summary>
        int ReplacementPromotedCountForTest { get; }
        /// <summary>自检读口：被拒掉的候选数（凭据不对 / 超时 / 上层喊停）。</summary>
        int ReplacementRejectedCountForTest { get; }

        /// <summary>把一帧写到**候选**连接上（`Challenge` / `Ack` / `Pong` / `bye` 走它）。
        /// 返回「写进去了没有」—— 写失败会入队日志，**不静默**。</summary>
        bool SendToPendingReplacement(string kind, string payloadJson);
        /// <summary>主线程调：把候选收到的帧倒进 `into`，返回条数。⚠️ 候选帧与当前连接的帧**分两个队列**
        /// —— 凭据没确认之前，候选说的一切都**不能**被当成当前对家说的话。</summary>
        int PumpPendingReplacement(List<NetFrame> into);
        /// <summary>凭据齐了：候选成为当前连接，**旧连接走有界优雅收尾**（`Shutdown(Send)` + 有界排空 + `Close`）。
        /// 旧代还没被上层取走的帧会被丢弃（它们属于已被顶替的那条连接）。返回 `false` = 没有候选。</summary>
        bool PromotePendingReplacement();
        /// <summary>候选作废：关掉候选 socket、**旧连接一个字都不动**。`reason` 进日志（必出声）。</summary>
        void RejectPendingReplacement(string reason);
    }

    public class TcpTransport : INetTransport, IPendingFrameDropper, IPendingReplacementHost
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

        // 🆕 🔴 **2026-10-20（W58-S6FIX）**：连接代次的**分配器**（`_gen` 是「当前活着的代次」，这个是「下一个号」）。
        //   为什么必须分开：待替换候选也要一个**唯一**代次（它要能区分自己的帧、提升后又要接替读线程），
        //   但候选**不能**把 `_gen` 推走 —— `_gen` 一推进，**旧连接的读线程下一轮就退出**（`while gen == _gen`），
        //   而候选存在的全部前提就是「旧连接可能还活着」。
        //   ⇒ `Setup` / `SetupPending` 都从这里领号；只有「成为当前连接」时才把号写进 `_gen`。
        int _genSeq;

        // 🔴 **A1268（2026-10-19）**：本机**正在主动拆**这条连接（`ClosePeer` / `Close` 之间）。
        //    为什么要有它：主动拆 socket 会让**我们自己的**读线程也失败一次 —— 那不是「掉线」，是收工。
        //    不区分的话，每一次正常收台（`NetSession.Close` / `Reset` / 自检里的清理）
        //    都会在日志里留下一条「掉线」告警 —— 那正是排查「为什么掉线」时最不该有的**假信号**。
        //    `Setup` 开新连接时清掉它。
        volatile bool _closing;

        // 🆕 🔴 **2026-10-20（W58-NETFIX · NETPROBE ⑤）：整条传输层已经【收工】（`Close()` 之后）。
        //    为什么单开一个、不复用 `_closing`：`_closing` 也覆盖**只断连接、保留监听**的 `ClosePeer()`
        //    （掉线 / 换连接那一档，读线程手里最后一帧仍可能要）；这个字段只由 `Close()` 置上、
        //    由下一次 `Setup()` 清掉 —— 它表达的是「**这个传输层对象已经彻底关了，之后任何帧都不该进队**」。
        //    没有它的话，「读线程刚读完一帧 → 主线程 `Close()` 清空收件箱 → 读线程 `Enqueue`」这个
        //    极窄窗口里，那句迟到的 `bye` 仍会躺进 `_inbox`（R6 的根就还在）。
        volatile bool _closed;

        // 🆕 🔴 **A1267（2026-10-19）**：后台读线程要说的「**为什么掉线**」攒在这儿，由**主线程** `Pump()` 取走打印。
        //    ⛔ **不能在 `Fail` 里直接 `Debug.Log`** —— `Fail` 的调用点里有一半在**读线程**上，
        //    而文件头那条规矩是「后台线程只碰 socket 与并发队列，**绝不碰 Unity API**」。
        //    （形状照抄 `_inbox`：后台入队、主线程出队。）
        //    `_noteWarn` = 真掉线（`Debug.LogWarning`）；`_noteLog` = 正常收尾，例如旧读线程随重连退出（`Debug.Log`）。
        readonly ConcurrentQueue<string> _noteWarn = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<string> _noteLog = new ConcurrentQueue<string>();

        // ==================================================================
        //  🆕 🔴 **2026-10-20（W58-S6FIX · 缺陷 S6）：待替换候选连接**
        //  触发条件：`AcceptLoop` 收到新连接时 `ConnBit` 还立着（旧连接「活着」，**但可能已经死了
        //  而本机还没判出来** —— 客机重连最常见的时序）。
        //  旧行为：直接 `c.Close()`（新连接一字节不读）⇒ 客机那边「连上了又断」。
        //  新行为：把它收成**候选**（独立 socket / 独立读线程 / 独立收件箱），由上层
        //  `NetSession` 在它上面走一遍 `Challenge → Proof → (对局中还要) Reconnect{sessionToken}`；
        //  **凭据齐了才 `PromotePendingReplacement()`**（旧连接走优雅收尾），任何一步不对就
        //  `RejectPendingReplacement()`（旧连接原样保留）⇒ 不会出现「谁连上来谁就能踢人」。
        //  ⚠️ 候选**不** `Publish` 连接位 / 计数：`IsConnected` / `AcceptedCount` 只描述**当前**连接。
        //  ⚠️ 候选代次从**同一个** `_gen` 计数器领号，但**不写回** `_gen`（写回就等于宣布旧连接过期）；
        //     提升时才 `_gen = 候选代次` ⇒ 旧读线程下一轮自己退。
        // ==================================================================
        readonly ConcurrentQueue<NetFrame> _pendInbox = new ConcurrentQueue<NetFrame>();
        readonly object _connLock = new object();      // 候选字段的读-改-写（接受线程 / 读线程 / 主线程）
        TcpClient _pendClient;
        NetworkStream _pendStream;
        Thread _pendReadThread;
        int _pendGen;
        volatile bool _pendActive;
        volatile bool _replacementArmed;               // 上层说「此刻允许收候选」
        string _pendPeer = "";
        System.Diagnostics.Stopwatch _pendSince;       // 候选挂了多久（null = 没有候选）
        public int ReplacementPromotedCountForTest { get; private set; }
        public int ReplacementRejectedCountForTest { get; private set; }

        // 🔴 **A943（2026-10-18）：连接位与接受计数【同处一个 32 位字、由一次 CAS 一起发布】**。
        //    原来 `Setup` 里是 `_connected = true;` 紧接着 `AcceptedCount++;` —— **两条独立的写**，
        //    而写它的是**接受线程**（`AcceptLoop` → `Setup`）、读它的是**主线程**
        //    （`NetSession.Pump`，每帧 / 自检里显式调）⇒ 存在一个「已连接、但计数还没加」的**中间态**。
        //    读侧恰好撞上那个中间态时（`NetSession.Pump` 的**握手边沿**那一支读到旧计数 ⇒ 不刷新静默基线；
        //    同一轮 `Pump` 的**静默超时**判据读到连接位已立 ⇒ 把一条**刚连上、一条包都还没发**的连接判成
        //    「10 秒没收到对面的任何消息」并 `ClosePeer`）——**掐掉一条好连接**
        //    （同一族的日志实证见 `资料/普查产出_1017/D4_联机诊断.md` §②）。
        //    ⇒ 现在两个事实**同处一个字**：要么都看得见、要么都看不见，**不存在半次发布**。
        //    ⚠️ 顺序**不能改成「先加计数、后置连接位」**（那会更糟）：上层是**边沿触发**
        //       （`NetSession.Pump` 的**握手边沿**那一支看到计数变了就 `_lastAccepted = …` 并**立刻**发 `Challenge`），
        //       那一刻 `Send`（本文件 `Send()` 开头 `s == null || !IsConnected` 那一判）会看到「还没连上」⇒**握手包发不出去、边沿却被消费掉了**
        //       ⇒ **永久握不上**。所以「连接位先可见」这条顺序是**必须保持**的 —— 现在由同一个字保证。
        //    ✅ **2026-10-18 读侧也闭环了**（原来这一行写的是「还剩半边、那一段在 `NetSession.cs`、
        //       不在本案白名单里」—— 这一轮补上了）：`NetSession.Pump()` 那道静默超时判据补了
        //       `(Role != NetRole.Host || _t.AcceptedCount == _lastAccepted)` ⇒「**计数还是我认过的那个**」
        //       才说明**没有新发布夹在上面那次计数读（`Pump` 的握手边沿那一支）与这次连接位读之间**
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

        // ==================================================================
        //  🆕 🔴 **2026-10-20（W58-NETFIX）静态可验证的读口**（生产路径一处都不读）
        //  对应 NETPROBE ②（输出侧确认）与 ⑤（Close 清收件箱）。
        //  ⚠️ 与上面 `GenerationForTest` / `FailForTest` 同族：这些字段**只服务自检**
        //     （`Editor/NetSelfTest.cs` 那种宿主；本轮白名单不含 `Editor/`，可粘贴块见交件报告）。
        // ==================================================================

        /// <summary>② 本机 `CloseGracefully` 里「发送缓冲已确认不再阻塞」成功的次数。</summary>
        public int SendBufferConfirmedCountForTest { get; private set; }
        /// <summary>② 有界等待内**没能**确认发送缓冲清空的次数（那一档会 `LogWarning` 后照旧拆）。</summary>
        public int SendBufferTimeoutCountForTest { get; private set; }
        /// <summary>⑤ `DropPendingFrames` 累计丢掉的帧数（`Close()` 那条路 + 显式调用）。</summary>
        public int DroppedPendingFrameCountForTest { get; private set; }
        /// <summary>⑤ 此刻收件箱里还没被上层 `Pump()` 取走的帧数（O(n) 快照，只给自检用）。</summary>
        public int PendingFrameCountForTest { get { return _inbox.Count; } }

        // ---- 🆕 W58-S6FIX：`IPendingReplacementHost` 的读口（生产路径只读前两个）----

        /// <summary>⚠️ 由主线程（`NetSession.Pump`）写、接受线程读 ⇒ `volatile`。</summary>
        public bool ReplacementArmed
        {
            get { return _replacementArmed; }
            set { _replacementArmed = value; }
        }
        public bool HasPendingReplacement { get { return _pendActive; } }
        public int PendingReplacementGeneration { get { return Volatile.Read(ref _pendGen); } }
        public string PendingReplacementPeer { get { return _pendPeer; } }
        public long PendingReplacementAgeMs
        {
            get
            {
                var sw = _pendSince;               // 引用读一次（它由 `_connLock` 保护；null 表示没有候选）
                return sw == null ? 0 : sw.ElapsedMilliseconds;
            }
        }

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
                // 🔴 **A1277（2026-10-19）**：**听成了就把上一次的失败话清掉** —— 照 `Setup` 里那句
                //    `_lastError = "";` 的现成写法（口径只留这一套，⛔ 别在 `Send` / `Listen` 里再各写一份）。
                //    为什么必须清：`Listen` 会先试双栈、失败再退 IPv4（`Listen(port)` 那两句）——
                //    **第一次的失败话会挂在那儿**，而监听其实已经起来了 ⇒ 「听成了」和「听败了」在错误态上混着，
                //    以后查「为什么连不上」时会被这句**旧话**指错方向。
                //    ⚠️ 唯一的消费面 `NetSession.Send` 是**前后各读一次、变了才算**
                //       （`_lastError` 是**粘的**，见那边的注）⇒ 在这里把它清空不会伪造出一次失败。
                _lastError = "";
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
                catch (Exception e)
                {
                    // 监听被关掉 = 正常退出。🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志）**：
                    //   原来这一支连「为什么退出」都不留 —— 分不清「本机 `Close()` 停的」还是
                    //   「监听还在、接受线程自己炸了」（后者 = 之后**再也接不到新连接**，而玩家只看到没反应）。
                    if (_listener != null)
                        _noteWarn.Enqueue("[Net] NET/BYE-S6/accept-loop-error 接受线程在**监听还开着**的时候退出了："
                                        + e.GetType().Name + "：" + e.Message
                                        + " ⇒ 之后**再也接不到新连接**（这条必须查；本机没有主动 `Close()`）");
                    else
                        _noteLog.Enqueue("[Net] NET/BYE-S6/accept-loop-exit 接受线程正常退出（监听已由 `Close()` 停掉）—— 不是缺陷");
                    return;
                }
                // 🔴 **A1276（2026-10-19）**：整段判断**只看一次读的同一个快照** ——
                //    原来这里是 `if (_client != null && IsConnected)`：**两次独立的读**、而且读的是
                //    **两个不同的字段**（`_client` 由 `Setup` / `ClosePeer` 写，`IsConnected` 是 `_state`
                //    那一个字），中间可以变 ⇒ 读到中间那个量变了也照样放行，**静默**（不抛、不报）。
                //
                //    判据取**状态字**（`IsConnected` 的同一个来源）：
                //      · 它是「此刻有一条**活**连接」的**唯一权威**（`Setup` / `Fail` / `ClosePeer` 都经
                //        同一个 `Publish` 写它，且与 `AcceptedCount` 同处一个字、一次 CAS 发布 —— 见字段区）；
                //      · `_client != null` **不是**活连接判据：掉线之后 `Fail` **不清** `_client`
                //        （只清连接位），那一刻 `_client` 仍非 null、而我们要的正是**接受重连** ⇒ 那一半本来就多余。
                //    ⚠️ **已知无害的边**：`ClosePeer` 是「先 `_client = null`、后 `Publish(false)`」，
                //       若一条重连恰好落在这两句之间，它会看到「连接位仍真」而被关掉 —— 对面 2 秒后重试
                //       （`NetSession.ReconnectEveryMs`），**自愈**；比「两个字段各读一次、静默放行」好。
                int stateNow = Volatile.Read(ref _state);
                if ((stateNow & ConnBit) != 0)
                {
                    // 🆕 🔴 **2026-10-20（W58-S6FIX · 缺陷 S6）：不再「一律关掉」，先看能不能收作候选。**
                    //   条件：上层说此刻允许替换（主机且在对局中 / 等重连）**且**候选位空着。
                    //   ⚠️ 收作候选 ≠ 替换：候选要过 `Challenge → Proof → Reconnect{sessionToken}`
                    //      三道凭据（判据在 `NetSession.PumpReplacement`）⇒ **陌生连接踢不掉人**。
                    //   ⚠️ 候选位只有一条（v1 1v1）：占着时新连接仍按旧行为拒收（下面那行日志会说清楚）。
                    if (_replacementArmed && !_pendActive)
                    {
                        SetupPending(c);
                        continue;
                    }
                    // 已经有**活的**对家了 ⇒ 多出来的直接关掉（v1 只支持 1v1，不排队）
                    // 🔴 **2026-10-19（T05「`bye` 丢在哪一跳」）**：这一支原来**一声不响** —— 而它正是
                    //   「对面刚重连上来、本机还没发现旧连接已经死了」时的那一跳：新连接上如果捎着
                    //   `bye`（客机退出时正好走重连分支），它就在这儿被关掉、**对端只看到连接断了**
                    //   （对端那侧只会出现 `对面关掉了连接` 的 EOF，看起来像传输层丢包）。
                    //   ⇒ 入队出声。⚠️ 本方法跑在**接受线程**上，⛔ 不能直接 `Debug.Log`（同 `Fail` 的规矩：
                    //   后台线程只碰 socket 与并发队列，打印由主线程 `Pump()` 做）。
                    //   判别器：日志里出现这一行 = 「本条连接被本机拒收」⇒ 别再去查读线程 / 对端发送。
                    // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：加唯一前缀
                    //   `NET/BYE-S6/rejected` + 那一刻的**连接位/计数快照** + 对端地址 —— 同机两进程真连时，
                    //   这一行能直接回答「是谁、在哪一代、为什么被拒」，不用再靠周边日志倒推。
                    string rejectedPeer = "<取不到>";
                    try { if (c.Client != null && c.Client.RemoteEndPoint != null) rejectedPeer = c.Client.RemoteEndPoint.ToString(); }
                    catch { }
                    _noteWarn.Enqueue("[Net] NET/BYE-S6/rejected 已经有一条活连接 ⇒ **拒收了新进来的那条连接**"
                                    + "（v1 只支持 1v1；这一条上如果捎着 `bye` 等消息，它们**不会**被读到）"
                                    + (_replacementArmed ? " · 但上层**允许**替换，只是候选位已被另一条新连接占着" : " · 此刻上层**不允许**替换（不在对局/等重连）")
                                    + " —— 判别器：被拒的对端 = " + rejectedPeer
                                    + " · 状态字 `_state` = " + stateNow + "（连接位=真 · 累计接受 = " + (stateNow >> 1) + "）"
                                    + " ⇒ 来源是**接受线程的拒收**，别去查读线程 / 对端发送");
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
            // 🆕 🔴 **2026-10-20（W58-S6FIX）**：一条新连接要成为**当前**连接 ⇒ 之前那条候选就没意义了
            //   （候选存在的唯一理由是「当前连接可能已经死了」，而现在当前连接要被换掉了）。
            //   ⚠️ 这条只在「旧连接已经失败 / 本来就没有」时才走得到（`ConnBit` 清掉之后才会 `Setup`）；
            //      正常替换走的是 `PromotePendingReplacement`。
            if (_pendActive) RejectPendingReplacement("superseded-by-new-current");
            try
            {
                c.NoDelay = true;
                // 🔴 **A1268（2026-10-19）：先把【旧连接】作废，再关它。**
                //    ⛔ 这两句的**次序不能反**：下面那句 `old.Close()` **正是唤醒旧读线程的那一下**
                //       —— 先关再 +1 的话，旧线程那次小 `Fail` 会挤在「还没作废」的窗口里，
                //       照旧打到共享状态（连接位 / `_peerLost` / `_lastError`）上。
                int gen = Interlocked.Increment(ref _genSeq);
                int oldGen = Volatile.Read(ref _gen);
                Volatile.Write(ref _gen, gen);           // 作废旧代 —— 次序硬：**先作废、后关旧 socket**（见下两句）
                // 🔴 **2026-10-19（T05「`bye` 丢在哪一跳」的同族：**旧代帧串台**）**：换连接前把
                //   `_inbox` 里**剩下的旧代帧**丢掉。为什么：这些帧属于上一条连接（已经死了），
                //   而 `NetSession.StartHost / Connect / CheckConnection` 都会在**同一个 transport 对象**上
                //   复用（`Close(false)` → `_t.Listen/Connect`）—— 不清的话，旧对家的 `bye` 会在
                //   **新对家/新一局**被当成真的收下（`case Bye` → `SetState(Closed)` + `_t.Close()`
                //   ⇒ 连刚起来的监听都会被关掉）。
                //   ⚠️ 读线程那一侧另有一道闸（`ReadLoop` 入队前再核一次 `_gen`）；两道都要：
                //      那一道管「旧线程晚到的帧」，这一道管「**已经进了队**的帧」。
                int stale = 0;
                NetFrame junk;
                while (_inbox.TryDequeue(out junk)) stale++;
                if (stale > 0)
                    _noteWarn.Enqueue("[Net] NET/BYE-R2/stale-inbox 换连接时丢弃了上一条连接留下的 " + stale + " 帧"
                                    + "（它们属于已经被顶替的那一代；最坏的一帧是 `bye` —— 不能拿它当新对家的告别）"
                                    + " —— 判别器：这一行说明「告别/动作」死在**队列里**，不是死在 socket 上");
                var old = _client;                       // 掉线后重连：旧的那条先关掉，别把 socket 漏在那
                if (old != null && old != c)
                {
                    // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志）**：旧连接被顶替时，它上面
                    //   **还没被读线程取走的字节**（最坏就是一句 `bye`）随 socket 一起没了 —— 这一步
                    //   原来是静的。记下代次，真连时能分清「bye 是死在旧代上还是新代上」。
                    _noteLog.Enqueue("[Net] NET/BYE-S6/old-connection-replaced 第 " + oldGen + " 代连接被第 " + gen
                                   + " 代顶替并关闭 —— 旧连接上还没被读线程取走的帧（若有，最坏是一句 `bye`）就此丢弃"
                                   + "（不是本代的问题，别把这句当新连接的缺陷）");
                    try { old.Close(); } catch { }
                }
                _client = c;
                var st = c.GetStream();
                _stream = st;                            // ⚠️ 读线程**只认这一份**（下面按值传进 `ReadLoop`）
                _peerLost = false;
                _closing = false;                        // A1268：新开的这条是「活着」的，不是「正在拆」
                _closed = false;                         // 🆕 ⑤：上一次 `Close()` 的闸在这里放开（同一个对象可以再开台）
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
                // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志）**：一条**已经 accept 成功**的
                //   连接在 `Setup` 里失败 ⇒ 对端只会看到「连上了又断了」。这一支原来只写 `_lastError`。
                _noteWarn.Enqueue("[Net] NET/BYE-S6/setup-failed 新连接（第 " + Volatile.Read(ref _gen)
                                + " 代）在 `Setup` 里失败：" + e.GetType().Name + "：" + e.Message
                                + " ⇒ 这条连接会原地关掉，它上面若捎着 `bye` / 握手包，**一个字节都不会被读到**");
                try { c.Close(); } catch { }
            }
        }

        // ==================================================================
        //  🆕 🔴 **2026-10-20（W58-S6FIX · 缺陷 S6）：待替换候选连接**（`IPendingReplacementHost`）
        //  一条候选的生命周期：`SetupPending` →（上层 `SendToPendingReplacement(Challenge)`）
        //  → 候选 `Proof` / 对局中再 `Reconnect{sessionToken}` → `PromotePendingReplacement`
        //  或任何一步不对 → `RejectPendingReplacement`（旧连接原样保留）。
        // ==================================================================

        /// <summary>候选收件箱的硬上限：陌生连接连上来狂发帧时**不许把内存吃光**；超了直接拒候选
        /// （`NET/BYE-S6/pending-flood`）。正常候选在凭据阶段只会收到 `Proof` / `Reconnect` 一两帧。</summary>
        const int PendingInboxMax = 256;

        /// <summary>清空候选收件箱（只在 `_connLock` 里调：候选读线程入队也拿同一把锁）。
        /// 🔴 为什么每次换候选/作废候选都要清：候选帧与「当前连接」的帧**共用一个候选队列**，
        /// 上一条候选没被上层取走的迟到帧（最坏是一条 `Reconnect`）若留着，会被**下一条候选**
        /// 的凭据流程当成自己的话 —— 那就是把「陌生连接踢不掉人」这条边界又开了一个口子。</summary>
        int DrainPendingInbox()
        {
            int n = 0;
            NetFrame junk;
            while (_pendInbox.TryDequeue(out junk)) n++;
            return n;
        }

        /// <summary>把一条**已经在 TCP 层 accept 成功**的新连接收作候选。⛔ 不碰当前连接的
        /// 连接位 / 计数 / `_peerLost` / `_lastError` —— 凭据没过之前它「不算数」。</summary>
        void SetupPending(TcpClient c)
        {
            int gen = 0;
            int stalePend = 0;
            try
            {
                c.NoDelay = true;
                gen = Interlocked.Increment(ref _genSeq);  // 只领号；**不**写 `_gen`（旧连接此刻仍是当前）
                var st = c.GetStream();
                string peer = "<取不到>";
                try { if (c.Client != null && c.Client.RemoteEndPoint != null) peer = c.Client.RemoteEndPoint.ToString(); }
                catch { }
                lock (_connLock)
                {
                    stalePend = DrainPendingInbox();       // 上一条候选的迟到帧（若有）不能带进这一条
                    _pendGen = gen;
                    _pendClient = c;
                    _pendStream = st;
                    _pendPeer = peer;
                    _pendSince = System.Diagnostics.Stopwatch.StartNew();
                    _pendActive = true;
                }
                if (stalePend > 0)
                    _noteLog.Enqueue("[Net] NET/BYE-S6/pending-inbox-cleared 新候选接手时丢弃了上一条候选残留的 "
                                   + stalePend + " 帧（它们属于**另一条**候选，不能参与这一条的凭据判定）");
                _pendReadThread = new Thread(() => PendingReadLoop(gen, st))
                { IsBackground = true, Name = "wf-net-pending" };
                _pendReadThread.Start();
                _noteLog.Enqueue("[Net] NET/BYE-S6/pending-accepted 已有一条活连接，但**新连接不再直接关掉**"
                               + "（S6 修复）⇒ 先收作**待替换候选**（第 " + gen + " 代 · 对端 " + peer
                               + "）；它必须过 `Challenge → Proof` 与（对局中）`Reconnect{sessionToken}` 才会顶替旧连接"
                               + " —— 凭据不过就关候选，**旧连接原样保留**");
            }
            catch (Exception e)
            {
                _noteWarn.Enqueue("[Net] NET/BYE-S6/pending-setup-failed 候选连接（第 " + gen + " 代）在 `SetupPending` 里失败："
                                + e.GetType().Name + "：" + e.Message + " ⇒ 关掉候选，旧连接不受影响");
                lock (_connLock)
                {
                    if (_pendGen == gen) { _pendActive = false; _pendClient = null; _pendStream = null; _pendSince = null; }
                }
                try { c.Close(); } catch { }
            }
        }

        /// <summary>候选自己的读线程。分帧逻辑与 <see cref="ReadLoop"/> 同源（帧头 + 帧体 + 信封），
        /// 区别只在**入哪个队列**：候选阶段进 `_pendInbox`（上层明确要求才取），提升之后进 `_inbox`。
        /// 提升那一刻 `_gen` 被写成候选代次 ⇒ 这个循环**不需要换线程**就接着当「当前连接的读线程」用。</summary>
        void PendingReadLoop(int gen, NetworkStream s)
        {
            var head = new byte[NetProtocol.HeaderBytes];
            while (true)
            {
                bool mine;
                lock (_connLock) mine = (_pendActive && _pendGen == gen) || Volatile.Read(ref _gen) == gen;
                if (!mine) return;                         // 被拒 / 被另一条候选顶掉 / 已被提升以外的换代

                int n;
                try { n = ReadFull(s, head, 0, NetProtocol.HeaderBytes); }
                catch (Exception e) { PendingEnded(gen, string.Format(Loc.T("Settings/Online/St/ReadAbort"), e.Message)); return; }
                if (n <= 0) { PendingEnded(gen, Loc.T("Settings/Online/St/PeerClosed")); return; }
                if (n < NetProtocol.HeaderBytes)
                {
                    PendingEnded(gen, "候选连接只发了半截帧头（" + n + "/" + NetProtocol.HeaderBytes + " 字节）");
                    return;
                }
                int len = NetProtocol.FrameLength(head, 0);
                if (len < 0 || len > NetProtocol.MaxFrame)
                {
                    PendingEnded(gen, string.Format(Loc.T("Settings/Online/St/BadFrame"), len));
                    return;
                }
                var body = new byte[len];
                try { n = ReadFull(s, body, 0, len); }
                catch (Exception e) { PendingEnded(gen, string.Format(Loc.T("Settings/Online/St/ReadAbort"), e.Message)); return; }
                if (n <= 0) { PendingEnded(gen, Loc.T("Settings/Online/St/PeerClosed")); return; }
                if (n < len)
                {
                    PendingEnded(gen, "候选连接的帧体被截断（" + n + "/" + len + " 字节）");
                    return;
                }
                var env = NetProtocol.Parse(body, len);
                if (env == null) { PendingEnded(gen, Loc.T("Settings/Online/St/BadEnvelope")); return; }

                bool flood = false;
                bool retired = false;
                lock (_connLock)
                {
                    if (_pendActive && _pendGen == gen)
                    {
                        if (_closed) return;
                        if (_pendInbox.Count >= PendingInboxMax) flood = true;
                        else _pendInbox.Enqueue(new NetFrame { kind = env.kind, payload = env.payload });
                    }
                    else if (Volatile.Read(ref _gen) == gen)
                    {
                        // 已提升：从此它读到的帧就是**当前连接**的帧（A1268 的旧代闸由 `_gen` 比较保证）
                        if (_closed) return;
                        _inbox.Enqueue(new NetFrame { kind = env.kind, payload = env.payload });
                    }
                    else
                    {
                        retired = true;
                        _noteLog.Enqueue("[Net] NET/BYE-R2/frame-dropped 第 " + gen + " 代（候选/已被顶替）读完一帧时发现"
                                       + "自己已不是当前代（当前第 " + Volatile.Read(ref _gen) + " 代）⇒ 这一帧丢弃"
                                       + "（与 `ReadLoop` 同一条旧代闸；最坏是一句 `bye`）");
                    }
                }
                if (retired) return;
                if (flood) { RejectPendingReplacement("候选收件箱超过 " + PendingInboxMax + " 帧（疑似灌包）"); return; }
            }
        }

        /// <summary>候选读线程失败/EOF：**绝不碰共享状态**（连接位、`_peerLost`、`_lastError` 都还是
        /// 旧连接的）。若此时它已经是**当前**连接（提升之后），才按正常掉线走 <see cref="Fail(int,string)"/>。</summary>
        void PendingEnded(int gen, string why)
        {
            TcpClient c = null;
            bool wasCurrent = false;
            int stalePend = 0;
            lock (_connLock)
            {
                if (_pendActive && _pendGen == gen)
                {
                    _pendActive = false;
                    c = _pendClient;
                    stalePend = DrainPendingInbox();
                    _pendClient = null; _pendStream = null; _pendReadThread = null; _pendSince = null;
                    _noteWarn.Enqueue("[Net] NET/BYE-S6/pending-lost 候选连接（第 " + gen + " 代 · " + _pendPeer
                                    + "）自己断了/坏了（原因：" + why + "）⇒ 放弃这条候选，**旧连接原样保留**");
                }
                else wasCurrent = Volatile.Read(ref _gen) == gen;
            }
            if (stalePend > 0)
                _noteLog.Enqueue("[Net] NET/BYE-S6/pending-inbox-cleared 候选（第 " + gen
                               + " 代）自己断掉时还留着 " + stalePend + " 帧没被取走 ⇒ 一并丢弃（不能带进下一条候选）");
            try { if (c != null) c.Close(); } catch { }
            if (wasCurrent) Fail(gen, why);
        }

        public bool SendToPendingReplacement(string kind, string payloadJson)
        {
            NetworkStream s;
            int gen;
            lock (_connLock)
            {
                if (!_pendActive) return false;
                s = _pendStream; gen = _pendGen;
            }
            if (s == null) return false;
            byte[] buf = NetProtocol.Frame(kind, payloadJson);
            try
            {
                lock (_sendLock) { s.Write(buf, 0, buf.Length); s.Flush(); }
                return true;
            }
            catch (Exception e)
            {
                _noteWarn.Enqueue("[Net] NET/BYE-S6/pending-write-failed 往候选连接（第 " + gen + " 代）写 `" + kind
                                + "` 失败：" + e.GetType().Name + "：" + e.Message + " ⇒ 这一帧**没到候选**");
                return false;
            }
        }

        public int PumpPendingReplacement(List<NetFrame> into)
        {
            if (into == null) return 0;
            int n = 0;
            NetFrame f;
            while (_pendInbox.TryDequeue(out f)) { into.Add(f); n++; }
            return n;
        }

        public bool PromotePendingReplacement()
        {
            TcpClient oldClient, pend;
            int pendGen;
            int stale = 0, carried = 0;
            string peer;
            lock (_connLock)
            {
                if (!_pendActive) return false;
                pendGen = _pendGen;
                pend = _pendClient;
                peer = _pendPeer;
                oldClient = _client;
                // 次序与 `Setup` 一致：**先**让旧代过期（`_gen` 指向候选），**再**关旧 socket。
                Volatile.Write(ref _gen, pendGen);
                _client = _pendClient;
                _stream = _pendStream;
                _readThread = _pendReadThread;
                _pendClient = null; _pendStream = null; _pendReadThread = null;
                _pendActive = false; _pendSince = null;
                _peerLost = false; _closing = false; _closed = false; _lastError = "";
                Publish(connected: true, bumpCount: false);   // 候选早已在 TCP 层接受过 ⇒ 计数不 +1（它是握手边沿）
                // 旧代还没被上层取走的帧：此刻 `_pendInbox` 里全是**新**连接的帧，所以 `_inbox` 里的一定是旧代 —— 丢。
                NetFrame junk;
                while (_inbox.TryDequeue(out junk)) stale++;
                // 候选在提升之前已经读出来的帧：属于**新**连接 ⇒ 按序搬进主收件箱，不能丢
                while (_pendInbox.TryDequeue(out junk)) { _inbox.Enqueue(junk); carried++; }
            }
            ReplacementPromotedCountForTest++;
            if (oldClient != null && !ReferenceEquals(oldClient, pend)) CloseSocketGracefullyBounded(oldClient);
            _noteLog.Enqueue("[Net] NET/BYE-S6/replaced 候选（第 " + pendGen + " 代 · " + peer
                           + "）凭据通过 ⇒ **提升为当前连接**；旧连接已走有界优雅收尾"
                           + "（`Shutdown(Send)` + ≤" + GracefulCloseDrainMs + "ms 排空 + `Close`）"
                           + (stale > 0 ? "；丢弃旧代残留 " + stale + " 帧" : "")
                           + (carried > 0 ? "；候选先读到的 " + carried + " 帧已并入收件箱" : ""));
            return true;
        }

        public void RejectPendingReplacement(string reason)
        {
            TcpClient c;
            int gen;
            string peer;
            int stalePend;
            lock (_connLock)
            {
                if (!_pendActive) return;
                c = _pendClient; gen = _pendGen; peer = _pendPeer;
                stalePend = DrainPendingInbox();           // 这条候选没被取走的帧不能留到下一条头上
                _pendActive = false;
                _pendClient = null; _pendStream = null; _pendReadThread = null; _pendSince = null;
            }
            ReplacementRejectedCountForTest++;
            try { if (c != null) c.Close(); } catch { }
            string why = reason ?? "?";
            string tag = why == "timeout" ? "NET/BYE-S6/replacement-timeout"
                       : why == "peer-bye" ? "NET/BYE-S6/candidate-bye"
                       : (why == "transport-close" || why == "superseded-by-new-current")
                             ? "NET/BYE-S6/reject-superseded"
                       : why.IndexOf("not-in-battle", StringComparison.OrdinalIgnoreCase) >= 0
                             ? "NET/BYE-S6/reject-not-in-battle"
                       : why.IndexOf("token", StringComparison.OrdinalIgnoreCase) >= 0 ? "NET/BYE-S6/reject-no-token"
                       : "NET/BYE-S6/reject-no-proof";
            _noteWarn.Enqueue("[Net] " + tag + " 候选连接（第 " + gen + " 代 · " + peer + "）**没有通过凭据**（" + why
                            + "）⇒ 关掉候选，**旧连接原样保留**（S6 的安全边界：没有 proof/sessionToken 的连接踢不掉人）");
            if (stalePend > 0)
                _noteLog.Enqueue("[Net] NET/BYE-S6/pending-inbox-cleared 作废候选（第 " + gen + " 代）时丢弃了它还没被取走的 "
                               + stalePend + " 帧（防止下一条候选把别人的话当成自己的凭据）");
        }

        /// <summary>替换掉的那条旧连接也走**有界优雅收尾**（同 `CloseGracefully` 的礼节，但**只关这一条 socket**，
        /// 不碰监听、不碰候选）。最大 `GracefulCloseDrainMs`，⛔ 不无限等。</summary>
        void CloseSocketGracefullyBounded(TcpClient c)
        {
            if (c == null) return;
            try { c.Client.Shutdown(SocketShutdown.Send); } catch { }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                while (sw.ElapsedMilliseconds < GracefulCloseDrainMs)
                {
                    int slice = (int)Math.Min(20L, GracefulCloseDrainMs - sw.ElapsedMilliseconds);
                    if (slice <= 0) break;
                    // 可读 + `Available == 0` = 对面也关了（FIN）。有数据时让旧读线程去消费（它已过期，会丢）。
                    if (c.Client.Poll(slice * 1000, SelectMode.SelectRead) && c.Client.Available == 0) break;
                }
            }
            catch { }
            try { c.Close(); } catch { }
        }

        /// <summary>只断**连接**，**保留监听** —— 主机等对面重连时要的就是这个
        /// （整条 `Close()` 会把监听也停掉，那主机就再也等不到人了）。
        /// ⚠️ 它**不碰候选**：`ClosePeer` 的语义是「当前这条断了，继续等重连」—— 而候选可能正是
        /// 那次重连的新连接（主机静默超时会走它）。</summary>
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
                // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：**半截帧头**。
                //   `ReadFull` 在「对面只发了一半就关连接」时返回 `got`（>0）—— 原来的 `n <= 0` 拦不住它，
                //   下面会拿**半个帧头**去算长度。出声，让真连日志能分清「截断」和「帧头本来就坏」。
                if (n < NetProtocol.HeaderBytes)
                    _noteWarn.Enqueue("[Net] NET/BYE-R1/truncated-header 帧头只读到 " + n + "/"
                                    + NetProtocol.HeaderBytes + " 字节对面就断了 ⇒ 下面会把**半截帧头**当长度解"
                                    + "（多半报「帧长度不合理」，最坏是半帧被当整帧 —— 见 `ReadFull` 那条已知形状）");

                int len = NetProtocol.FrameLength(head, 0);
                if (len < 0 || len > NetProtocol.MaxFrame) { Fail(gen, string.Format(Loc.T("Settings/Online/St/BadFrame"), len)); return; }

                var body = new byte[len];
                try { n = ReadFull(s, body, 0, len); }
                catch (Exception e) { Fail(gen, string.Format(Loc.T("Settings/Online/St/ReadAbort"), e.Message)); return; }
                if (n <= 0) { Fail(gen, Loc.T("Settings/Online/St/PeerClosed")); return; }
                // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：**半截帧体**。
                //   下面仍按原样 `Parse(body, len)`（不改行为）；这一行让日志能分清「截断」与「信封坏了」。
                if (n < len)
                    _noteWarn.Enqueue("[Net] NET/BYE-R1/truncated-body 帧体只读到 " + n + "/" + len
                                    + " 字节对面就断了 ⇒ 接下来会解**半截缓冲**（信封多半解不出 ⇒ `BadEnvelope`）；"
                                    + "若本帧本该是 `bye`，它不会到达应用层");

                var env = NetProtocol.Parse(body, len);
                if (env == null) { Fail(gen, Loc.T("Settings/Online/St/BadEnvelope")); return; }
                // 🔴 **2026-10-19（T05「`bye` 丢在哪一跳」的同族：**旧代帧**串台**）**：上面那个
                //   `while` 只在**每轮开头**看代 —— 一次读帧的动作可能横跨换代（`Setup` 是接受线程 /
                //   主线程写的），于是**旧连接的最后一帧**（最坏就是 `bye`）会被塞进**新连接**的 `_inbox`
                //   ⇒ 新对家一进来就被当成「刚说了再见」，`case Bye` 还会 `_t.Close()` **把新连接关掉**。
                //   ⇒ 入队前**再核一次代**（与 `Fail` 里那道闸同源，两道都要：那一道管共享状态，这道管队列）。
                //   ⚠️ 丢掉是**对的**：这一帧属于已经被顶替的连接，新连接的握手/重放会覆盖它要表达的状态。
                if (gen != Volatile.Read(ref _gen))
                {
                    _noteLog.Enqueue("[Net] NET/BYE-R2/frame-dropped 旧连接（第 " + gen + " 代）的最后一帧被丢弃：它已被第 "
                                   + Volatile.Read(ref _gen) + " 代连接顶替（A1268 / T05）"
                                   + " —— 若那一帧正是 `bye`，它不会到达应用层（这一行是它的墓志铭）");
                    return;
                }
                // 🆕 🔴 **2026-10-20（W58-NETFIX · NETPROBE ⑤）**：整条传输层已经 `Close()` 了 ⇒
                //   这一帧**不许再进队**（没有它的话，「刚读完一帧 → `Close()` 清空收件箱 → 本线程
                //   `Enqueue`」这个窗口里迟到的 `bye` 仍会躺进 `_inbox`，R6 的根就还在）。
                //   ⚠️ 只认 `_closed`（`Close()` 置的），⛔ 别用 `_closing` —— 那也覆盖
                //   `ClosePeer()`（掉线/换连接），那一档读线程手里的最后一帧仍可能要。
                if (_closed)
                {
                    string k = env.kind == null ? "<null>" : (env.kind.Length > 32 ? env.kind.Substring(0, 32) : env.kind);
                    _noteLog.Enqueue("[Net] NET/BYE-R6/inbox-cleared 整条传输层已 `Close()` ⇒ 第 " + gen
                                   + " 代读线程刚读到的最后一帧（kind=`" + k + "`，最长 32 字已截）**不再入队**"
                                   + "（这就是 R6 那条迟到 `bye` 的根治点之一；不是丢包缺陷）");
                    return;
                }
                _inbox.Enqueue(new NetFrame { kind = env.kind, payload = env.payload });
            }
            // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：从 `while` **正常退出** =
            //   本代被 `Setup` 顶替了（不是读失败）—— 旧 socket 上**还没读到的字节**随它一起没了。
            //   与上面那条「最后一帧」不同：那条是**已经读出来的一帧**被丢，这条是**压根没读**。
            if (s != null && gen != Volatile.Read(ref _gen))
                _noteLog.Enqueue("[Net] NET/BYE-S6/read-loop-superseded 第 " + gen + " 代读线程退出（当前第 "
                               + Volatile.Read(ref _gen) + " 代）—— 旧连接上**还没读到的字节**"
                               + "（最坏是一句 `bye`）随旧 socket 一起丢弃（正常收尾，不是缺陷）");
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
                _noteLog.Enqueue("[Net] NET/BYE-R1/stale-reader-exit 旧连接的读线程退出了（原因：" + why + "）—— 它属于第 " + gen
                               + " 代连接、当前是第 " + Volatile.Read(ref _gen)
                               + " 代 ⇒ **与现在这条活着的连接无关**，一律不改共享状态（A1268）");
                return;
            }

            // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：`_lastError` 是**粘的**
            //   —— 第二次失败（哪怕原因不同）会被静默吞掉。这一行把「被吞掉的那个原因」留进日志。
            if (_lastError != "" && !string.Equals(_lastError, why, StringComparison.Ordinal))
                _noteLog.Enqueue("[Net] NET/BYE-R1/cause-superseded 又一次传输层失败，但 `_lastError` 已被占着"
                               + "（旧 = 「" + _lastError + "」/ 新 = 「" + why + "」· 第 " + gen + " 代）"
                               + " ⇒ 新原因只进这条日志（字段是粘的，`NetSession.Send` 靠前后比对认不出来）");

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
                // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：上面的「不出声」保留
                //   （级别仍是 `Debug.Log`、文案里**不带**「传输层掉线」那个标记 —— `NetSelfTest` R⑨/R⑭ 盯的就是这两点），
                //   但把**被按下的那个原因**记一笔：真连时若看到它，说明「拆的时候对面其实还发过东西 / 或
                //   读线程以这个原因退出」，那是 `bye` 可能死在本地收尾这一跳的直接证据。
                _noteLog.Enqueue("[Net] NET/BYE-R1/local-close-failure 本机正在主动拆（`_closing`）⇒ 读线程这次失败被按下不表"
                               + "（原因：「" + why + "」· 第 " + gen + " 代）—— 收工不是掉线，但留一笔，"
                               + "免得把「拆的时候对面其实还发过东西」看丢");
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
                UnityEngine.Debug.LogWarning("[Net] NET/BYE-S3/no-connection 这条 `" + kind + "` **没发出去**（传输层：" + _lastError + "）"
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
                // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：`Fail` 只把原因写进
                //   `_lastError`，而 `NetSession.Send` 的判据是「调用前后**变了**」—— **同一个原因字符串第二次
                //   出现时它认不出来**（W57 顺手发现 #3）⇒ 那条包会被当作「发成功」。这一行是该缝的兜底：
                //   它把**本方法自己**的写失败记下来（含代次 + kind），不必依赖上层比对。
                _noteWarn.Enqueue("[Net] NET/BYE-S4/write-failed 这条 `" + kind + "` 在**传输层的写/刷**上失败了（第 "
                                + Volatile.Read(ref _gen) + " 代）：" + e.GetType().Name + "：" + e.Message
                                + " ⇒ 它**没到对面**；⚠️ 上层靠 `_lastError` 前后比对判失败，"
                                + "同一个原因第二次出现时认不出来 —— 这一行是那条缝的兜底");
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
            // 🆕 🔴 **2026-10-20（W58-S6FIX）**：整条收工 ⇒ 候选也没有存在的意义（它属于「当前连接可能还活着」
            //   那个判断；现在连监听都停了）。先关候选，再停监听（顺序无所谓，但先关能保证候选读线程不再入队）。
            if (_pendActive) RejectPendingReplacement("transport-close");
            // 🆕 🔴 **2026-10-20（W58-NETFIX · NETPROBE ⑤）**：先树旗 —— 之后读线程再读到/再想入队
            //   的任何帧都进不来（见 `ReadLoop` 入队前那道闸）。`Setup` 开新连接时清掉它。
            _closed = true;
            var lis = _listener; _listener = null;
            try { if (lis != null) lis.Stop(); } catch { }
            ClosePeer();
            _acceptThread = null; _readThread = null;   // 线程都是后台线程，socket 关掉后自己会退
            // ⑤ 清收件箱：本机先 `Close()` 之后，队列里若还躺着对端的 `bye` / 对局帧，
            //    `NetSession.Pump` 仍会把它们交给 `Handle`（`Handle` 之后才判状态）⇒ 「第二遍告别」（R6）。
            //    清掉之后，`Pump` 再也看不到这条连接上的旧帧。
            DropPendingFrames("Close");
            // 🆕 W58-S6FIX：候选收件箱也在收工这一刻清掉（它们属于一条**没被确认**的连接，绝不能带进下一局）
            int pendStale = 0;
            NetFrame pj;
            while (_pendInbox.TryDequeue(out pj)) pendStale++;
            if (pendStale > 0)
            {
                DroppedPendingFrameCountForTest += pendStale;
                UnityEngine.Debug.Log("[Net] NET/BYE-S6/pending-inbox-cleared `Close()`：丢弃候选收件箱里 "
                                    + pendStale + " 帧（候选从未被提升 ⇒ 这些帧不属于任何当前对家）");
            }
        }

        /// <summary>🆕 🔴 **2026-10-20（W58-NETFIX · NETPROBE ⑤）**：清空收件箱里**还没被上层取走**的帧。
        /// <para>为什么要有它：`NetSession.Close()` 之后，`NetRuntime.Update` 仍会每帧 `Session.Pump()`，
        /// 而 `Pump` 是**先把帧交给 `Handle`、之后才判状态** ⇒ 队列里一句迟到的 `bye` 会再演一遍
        /// 「对面离开了」（`NET/BYE-R6/bye-after-local-close` 那个观测口说的就是它）。</para>
        /// <para>✅ **有界**：只是把 `ConcurrentQueue` 取空，不等待、不联网。没有可丢时**不出声**
        /// （正常的空收件箱不该刷屏）；丢了就 `Debug.Log`（`Close()` 只从主线程调，可以直接打）。</para>
        /// <para>⚠️ 与 `_closed` 那道闸**成对**：这里管「已经进了队的」，那道管「正要进队的」——
        /// 两道都要，缺哪一道都还剩一个窗口。</para></summary>
        public int DropPendingFrames(string cause)
        {
            int n = 0;
            NetFrame junk;
            while (_inbox.TryDequeue(out junk)) n++;
            if (n > 0)
            {
                DroppedPendingFrameCountForTest += n;
                UnityEngine.Debug.Log("[Net] NET/BYE-R6/inbox-cleared " + (cause ?? "?")
                    + "：丢弃了收件箱里**还没被上层取走**的 " + n + " 帧"
                    + "（最坏是一句迟到的 `bye` —— 不清的话 `Pump` 会把它当成对面**又一次**离开；"
                    + "这是 R6 的收尾清场，不是丢包缺陷）");
            }
            return n;
        }

        /// <summary>有界优雅关闭的排空上限（毫秒）。**必须有界** —— 对面不关时不能把主线程卡住。</summary>
        public const int GracefulCloseDrainMs = 200;

        /// <summary>🆕 2026-10-19（`A1264` X2「`bye` 丢在哪一跳」）：**有界优雅关闭**。
        /// <para>为什么要有它：`NetSession.Close` 原来是「同步 `Write + Flush` 之后**立刻** `Close()`」——
        /// 没有 `Shutdown(Send)`、没有排空、没有 linger ⇒ 刚写出的 `bye` 可能在拆连接时被丢掉
        /// （带了未读数据的拆除在 Windows 上还可能走 abortive 路径，把发送队列一起丢）。</para>
        /// <para>次序（**顺序硬**）：① `Shutdown(SocketShutdown.Send)`（FIN 排在已写出的字节之后）
        /// → **①·b 输出侧确认**（🆕 2026-10-20 W58-NETFIX · NETPROBE ②：有界等
        /// `Poll(SelectWrite)` = 本机内核**不再阻塞发送** —— 发送缓冲里没有待发/被窗口卡住的数据；
        /// 见下面那段注释的**语义边界**）
        /// → ② 有界排空（≤ <see cref="GracefulCloseDrainMs"/>；只 `Poll` / 看 `Available`，
        /// **不自己 `Receive`** —— 读线程正在消费入站帧，两个线程抢同一个 socket 会把帧拆坏）
        /// → ③ `Close()`（原有的收尾：停监听 + `ClosePeer` + 清收件箱）。</para>
        /// <para>🔴 **①·b 解决的是什么**：`Shutdown(Send)` 只保证「FIN 排在**已写出的**字节之后」，
        /// 不保证那些字节已经出了**内核发送缓冲**。若发送队列非空时去 `Close()`，Windows 可能走
        /// abortive（RST）把挂起的发送数据一起丢掉（NETPROBE 候选 ①/建议 ②）。
        /// ⚠️ **它的语义边界（如实写死，别当送达确认）**：`Poll(SelectWrite)` 为真只说明
        /// 「**本机内核的发送缓冲已腾空、不再阻塞发送**」—— **不证明对端 ACK、更不证明应用层收到**。
        /// 端到端确认仍然只有下面那条「等到对面 FIN（`drain-eof`）」。</para>
        /// <para>⛔ **不无限等**：两段等待共享**同一个** <see cref="GracefulCloseDrainMs"/> 预算；
        /// 超时只 `LogWarning` 然后照旧关；异常也只出声（⛔ 不吞掉原异常文本）。
        /// ⚠️ **假设**：`_client` 只由 `TcpListener.AcceptTcpClient()` / `new TcpClient(...)` 建出来
        /// （本文件全部创建点就是这两种），`TcpClient.Client` 拿到的就是那条 socket；本类从不替换 `Client`。</para>
        /// </summary>
        public void CloseGracefully(int drainMs = GracefulCloseDrainMs)
        {
            // 本机自己拆 ⇒ 读线程随后那次「失败」不算掉线（同 `ClosePeer` 的理由；这里要**提前**置，
            // 否则排空期间对面先关，`Fail` 会先打一条假的「传输层掉线」）。
            _closing = true;
            var c = _client;
            var s = c != null ? c.Client : null;
            if (s != null)
            {
                try { s.Shutdown(SocketShutdown.Send); }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning("[Net] 优雅关闭：`Shutdown(Send)` 没成功（" + e.Message
                        + "）—— 照旧往下关（⛔ 不吞掉，也不无限等）[NET/BYE-S5/shutdown-failed]");
                }
                var sw = System.Diagnostics.Stopwatch.StartNew();

                // ---- ①·b 输出侧确认（🆕 W58-NETFIX · NETPROBE ②）----------------------------
                // 与下面 ② **共享同一个 `sw` 预算** ⇒ 总等待仍然 ≤ drainMs（⛔ 不是两段各 200ms）。
                // 正常情况下 `Poll(SelectWrite)` 立即为真（内核发送缓冲有空位）⇒ 这一段几乎 0ms；
                // 只有发送队列真的被堵住（对端不读、窗口为 0）时才会真等 —— 那正是该等的时候。
                bool outClear = false;
                while (sw.ElapsedMilliseconds < drainMs)
                {
                    try
                    {
                        int slice = (int)Math.Min(20L, drainMs - sw.ElapsedMilliseconds);
                        if (slice <= 0) break;
                        // `Poll` 只「看」不「取」；`SelectWrite` = 内核此刻还能收下我们的字节。
                        if (s.Poll(slice * 1000, SelectMode.SelectWrite)) { outClear = true; break; }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning("[Net] 优雅关闭：确认发送缓冲时 socket 报错（" + e.Message
                            + "）—— 照旧往下等对面 FIN（⛔ 不无限等）[NET/BYE-S5/send-buffer-error]");
                        break;
                    }
                }
                if (outClear)
                {
                    SendBufferConfirmedCountForTest++;
                    UnityEngine.Debug.Log("[Net] NET/BYE-S5/send-buffer-drained 优雅关闭：发送缓冲已确认不再阻塞（"
                                        + sw.ElapsedMilliseconds + " ms）⇒ 现在等对面的 FIN"
                                        + "（⚠️ 这一步只证明**本机内核**把字节收了，不证明对面收到；"
                                        + "端到端确认看下面的 drain-eof）");
                }
                else if (sw.ElapsedMilliseconds >= drainMs)
                {
                    SendBufferTimeoutCountForTest++;
                    UnityEngine.Debug.LogWarning("[Net] 优雅关闭：**发送缓冲确认超时**（" + drainMs
                        + " ms 内内核一直报告还不能发）⇒ 发送队列可能仍未腾空，照旧继续收尾"
                        + "（有界，⛔ 不无限等）[NET/BYE-S5/send-buffer-timeout]"
                        + " —— ⚠️ 这一行出现 = 对面的接收窗口可能被堵住（不读/不 ACK）；"
                        + "若随后它报「没收到 bye」，先看这里");
                }

                // ---- ② 等对面 FIN（原有阶段；预算已被 ①·b 用掉一部分）----------------------
                bool sawEof = false;
                while (sw.ElapsedMilliseconds < drainMs)
                {
                    try
                    {
                        int slice = (int)Math.Min(20L, drainMs - sw.ElapsedMilliseconds);
                        if (slice <= 0) break;
                        // `Poll` 只「看」不「取」：可读 + `Available == 0` = 对面也关了（EOF）。
                        // ⚠️ 有数据时让**读线程**去消费，我们只等它变空 —— 两个线程同时 Receive 会拆坏帧。
                        if (s.Poll(slice * 1000, SelectMode.SelectRead) && s.Available == 0) { sawEof = true; break; }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning("[Net] 优雅关闭：排空时 socket 报错（" + e.Message
                            + "）—— 直接拆（⛔ 不无限等）[NET/BYE-S5/drain-error]");
                        break;
                    }
                }
                if (sawEof)
                    UnityEngine.Debug.Log("[Net] NET/BYE-S5/drain-eof 优雅关闭：对面也关了（≤" + drainMs
                                        + " ms）⇒ 收尾正常（那句 `bye` 已在本机写完、FIN 排在它之后）");
                if (!sawEof && sw.ElapsedMilliseconds >= drainMs)
                    UnityEngine.Debug.LogWarning("[Net] 优雅关闭：排空**超时**（" + drainMs
                        + " ms 内对面没关）⇒ 直接拆（有界，⛔ 不无限等）[NET/BYE-S5/drain-timeout]"
                        + " —— ⚠️ 这一行出现 = 本机收尾没等到对面的 FIN；若对面随后报「没收到 bye」，"
                        + "先看这里（Windows 对「接收缓冲还有未读数据」的 socket 可能走 abortive/RST）");
            }
            else
            {
                // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：被要求「优雅关闭」，
                //   可这台传输层**根本没有 socket**（`_client == null`）—— 说明 `Send(bye)` 那一刻的
                //   `_stream` 与这里读到的 `_client` 不是同一份（或被别的线程清掉了）。
                UnityEngine.Debug.LogWarning("[Net] NET/BYE-S5/no-socket 优雅关闭请求到了一台**没有 socket** 的传输层"
                    + "（`_client == null`）⇒ 直接 `Close()`；那句 `bye` 到底写到哪去了，看上一行 `Send` 的记录");
            }
            Close();
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
