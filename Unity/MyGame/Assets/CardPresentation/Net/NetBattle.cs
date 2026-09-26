// NetBattle.cs — 联机**对局**那一层：开局交接 · 动作中转 · 座位翻译 · 状态指纹 · 换牌定序 · 重连重放
//
// 设计判据全文 → `资料/联机P2P_设计与交接.md` §五 / §六（N3~N5）。这里只写**实现要点**：
//
// 🔴 **主机的三件事**（别让别处再判一遍）：
//   ① **开局参数**（种子 / 两副牌的顺序 / 谁先手 / 战场）由主机算完下发；
//   ② **动作定序**（每条动作一个 `seq`，写进权威动作流 —— 重连时靠它重放）；
//   ③ **换牌阶段的定序**（`Mulligan` 会掷 `ctx.Rng`，**两边的调用顺序必须一模一样**）。
//
// 🔴 **对局中两端都是「自己 = 0 号位」**（`BattleDriver._me` 焊死）⇒
//   · 座位翻译只在 `NetProtocol.Seat()` 一处做（收到对面动作时翻 `targetP`；发送方视角的原样发）；
//   · **牌堆顺序必须由主机定**：`RuleCore.NewBattle` 的洗牌按座位顺序抽随机数
//     （`BuildPlayer(P0)` 先洗、`BuildPlayer(P1)` 后洗）⇒ 两端镜像跑会**洗出不同的牌堆**。
//     所以联机局一律 `shuffle:false` + 用主机下发的顺序（见 `NetPendingBattle.DeckOrder`）。
//
// 🔴 **不做回滚**（引擎没有快照）：本地动作**乐观落地**（自己那一回合只有自己在动 ⇒
//   「一个行动方 + TCP 有序」天然保证两端顺序一致），主机收到后照样过一遍引擎 ——
//   **引擎拒了就出声中止**（红本：不许静默失败）。每回合末对一次**状态指纹**。
using System;
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

namespace CardPresentation.Net
{
    /// <summary>开局参数（**主机算好的那份**）。跨场景交接用静态 `Current`。
    /// 🔴 **座位是绝对的**：0 = 主机 · 1 = 客机（两端同一套编号）—— 见 `NetProtocol.Fingerprint` 的注释。</summary>
    public class NetPendingBattle
    {
        public int Seed;
        /// <summary>本局模式名（`GameMode` 枚举名：`Classic` / `Skirmish`）。</summary>
        public string Mode = "Classic";
        /// <summary>战场场景名（**主机定** —— 正本 §二·5）。</summary>
        public string Arena;
        public bool IsHost;
        /// <summary>**本机是几号座位**：主机 0 / 客机 1。它决定 `BattleDriver._me` 与视图方向。</summary>
        public int MySeat;
        /// <summary>座位 0 那副（主机那副）—— ⚠️ **不是「我」那副**（客机看它是 1 号位那副）。</summary>
        public PlayerDeck Seat0Deck;
        /// <summary>座位 1 那副（客机那副）。</summary>
        public PlayerDeck Seat1Deck;
        public string Seat0Faction, Seat1Faction;
        /// <summary>先手是几号座位（**绝对**，主机抽的）。</summary>
        public int FirstSeat;
        public string MyName, FoeName;
        /// <summary>保留开关：置真 ⇒ 两端都**不洗牌**（默认假 —— 现在两端输入完全一样，
        /// 各洗各的也必然一致，见 `NetProtocol.Fingerprint` 的注释）。</summary>
        public bool NoShuffle = false;
        /// <summary>主机下发的那份原始开局包（**主机留着给重连用**）。</summary>
        public MsgStart Raw;

        public string ModeStr { get { return string.IsNullOrEmpty(Mode) ? "Classic" : Mode; } }
        /// <summary>本机那副（＝`MySeat` 那一副）。</summary>
        public PlayerDeck MyDeck { get { return MySeat == 0 ? Seat0Deck : Seat1Deck; } }
        /// <summary>对面那副。</summary>
        public PlayerDeck FoeDeck { get { return MySeat == 0 ? Seat1Deck : Seat0Deck; } }

        /// <summary>跨场景那一份（`LoadScene` 会清掉普通静态字段之外的一切，这里只放数据）。</summary>
        public static NetPendingBattle Current;

        public static NetPendingBattle Take()
        {
            var p = Current; Current = null; return p;
        }

        /// <summary>从主机下发的那份开局包造出「本机视角」的开局参数。
        /// 🔴 **两端都走这一条**（开局与重连重放**同一条路** —— 抽成独立方法就是为了这个）。</summary>
        public static NetPendingBattle FromStart(MsgStart s, bool isHost)
        {
            if (s == null) return null;
            var d0 = string.IsNullOrEmpty(s.hostDeckJson) ? null : JsonUtility.FromJson<PlayerDeck>(s.hostDeckJson);
            var d1 = string.IsNullOrEmpty(s.clientDeckJson) ? null : JsonUtility.FromJson<PlayerDeck>(s.clientDeckJson);
            return new NetPendingBattle
            {
                Seed = s.seed,
                Mode = s.mode,
                Arena = s.arena,
                IsHost = isHost,
                MySeat = isHost ? 0 : 1,
                Seat0Deck = d0,
                Seat1Deck = d1,
                Seat0Faction = s.hostFaction,
                Seat1Faction = s.clientFaction,
                FirstSeat = s.hostFirst,                  // 字段名照协议（host = 座位 0）；
                MyName = isHost ? s.myName : s.foeName,   // 它的值就是**绝对先手座位**
                FoeName = isHost ? s.foeName : s.myName,
                Raw = s,
            };
        }

        /// <summary>重连重放用的别名（同一个东西，名字不同只是为了调用处读得懂）。</summary>
        public static NetPendingBattle FromReplay(MsgStart s, bool isHost) { return FromStart(s, isHost); }
    }

    /// <summary>一条原生动作 + 它带的面板答案（**发送方视角**）。</summary>
    public class NetLocalAction
    {
        public MsgAction Wire;
    }

    /// <summary>
    /// 联机层要的那几件事（`BattleDriver` 实现它；**联机自检**用一个裸 `BattleContext` 的轻实现）。
    /// 抽这一层是为了让「动作中转 / 座位翻译 / 指纹 / 重放」**能被自检直接验**
    /// —— 不然要往一个场景里塞两个完整对局驱动才验得了。
    /// </summary>
    public interface INetBattleHost
    {
        BattleContext Ctx { get; }
        /// <summary>把权威流里的一条落到引擎上（引擎那段在 `NetApply.Apply`，**只有那一份**）。
        /// 🔴 座位是**绝对编号** ⇒ **不用翻**（翻座位那套已作废，见 `NetProtocol.Fingerprint` 的注释）。</summary>
        int ApplyLoggedAction(MsgAction m);
        void NetSay(string s);
        /// <summary>对面投降 ⇒ 本机判胜（`RuleCore.Forfeit` + 表现层刷新都在宿主里）。</summary>
        void NetRemoteResign();
        /// <summary>重连：**丢掉本地状态、从种子重建、按序全量重放**。</summary>
        void NetReplayFromNet(MsgStart start, List<MsgAction> actions);
    }

    public class NetBattle
    {
        public bool IsHost { get; private set; }
        /// <summary>本机是几号座位：主机 0 / 客机 1（**绝对编号**，两端同一套）。</summary>
        public int MySeat { get; private set; }
        public NetSession Session { get { return _s; } }
        /// <summary>对面是几号座位。</summary>
        public int RemoteSeat { get { return 1 - MySeat; } }
        /// <summary>这一局的权威动作流（主机：全量；客机：从 `applied` 收到的那份）。</summary>
        public readonly List<MsgAction> Log = new List<MsgAction>();
        public int LastSeq { get { return Log.Count == 0 ? -1 : Log[Log.Count - 1].seq; } }
        /// <summary>这一局是不是已经因为「两端打岔」中止了（自检要读它）。</summary>
        public bool Aborted { get { return _aborted; } }

        INetBattleHost _d;
        NetSession _s;
        int[] _pendingPicks; string[] _pendingPickIds;
        MsgStart _start;                                   // 主机的开局包（重连要用）
        int[] _myMulligan; bool _theirMulligan; int[] _theirMulliganMarks;
        bool _aborted;
        int _lastHashTurn = -1, _lastHash = 0;

        // ==================================================================
        //  接上 / 拆下
        // ==================================================================

        public static NetBattle Attach(INetBattleHost d, NetSession s, bool isHost)
        {
            var nb = new NetBattle { _d = d, _s = s, IsHost = isHost, MySeat = isHost ? 0 : 1 };
            // 进了对局：大厅那一层**别再读** `Session.Inbox`（两边抢着读会丢消息）
            if (NetRuntime.Instance != null) NetRuntime.Instance.LobbyHandled = false;
            if (s != null && isHost)
                s.ResumeProvider = nb.ResumeData;              // 主机：重连时把这一局的权威动作流灌回去
            if (s != null) s.OnResumed += nb.OnResumed;         // 客机：收到 `resume` ⇒ 重建 + 全量重放
            Debug.Log($"[Net] 对局已接上联机层（本机 = {(isHost ? "主机" : "客机")}）");
            return nb;
        }

        /// <summary>`BattleDriver` 那条路：接上并把「本局是不是主机」写进驱动（它要用它判很多事）。</summary>
        public static NetBattle Attach(BattleDriver d, NetSession s, NetPendingBattle pb)
        {
            var nb = new NetBattle { _d = d, _s = s, IsHost = pb.IsHost, MySeat = pb.MySeat };
            d.AttachNet(nb);
            if (NetRuntime.Instance != null) NetRuntime.Instance.LobbyHandled = false;
            if (s != null && pb.IsHost) s.ResumeProvider = nb.ResumeData;
            if (s != null) s.OnResumed += nb.OnResumed;
            Debug.Log($"[Net] 对局已接上联机层（本机 = {(pb.IsHost ? "主机" : "客机")}，种子 {pb.Seed}，"
                    + $"战场 {pb.Arena}，本机座位 {pb.MySeat}，先手座位 {pb.FirstSeat}）");
            return nb;
        }

        void Abort(string why)
        {
            if (_aborted) return;
            _aborted = true;
            Debug.LogError("[Net] 🔴 联机对局中止：" + why
                         + "（两端状态可能已经不一致 —— 本工程没有回滚，如实说出来，不假装没事）");
            if (_d != null) _d.NetSay("联机对局中止：" + why);
        }

        // ==================================================================
        //  本地：动作 / 换牌 / 投降
        // ==================================================================

        /// <summary>落地**之前**抓面板答案（引擎一结算就把 `ChoosePicks` / `ChooseCardIds` 吃空了）。</summary>
        public void CaptureLocalAnswers(BattleContext ctx, AiAction a)
        {
            _pendingPicks = ctx != null && ctx.ChoosePicks.Count > 0 ? ctx.ChoosePicks.ToArray() : null;
            _pendingPickIds = ctx != null && ctx.ChooseCardIds.Count > 0 ? ctx.ChooseCardIds.ToArray() : null;
        }

        /// <summary>本地动作**已经落地**（乐观执行）⇒ 发给对面 / 记进权威流并广播。</summary>
        public void OnLocalAction(AiAction a)
        {
            if (_aborted) return;
            var m = NetProtocol.ToWire(a, null, 0);
            m.actor = MySeat;                                   // 🔴 **绝对座位**（客机是 1，别落在默认的 0 上）
            m.picks = _pendingPicks; m.pickIds = _pendingPickIds;
            _pendingPicks = null; _pendingPickIds = null;

            if (IsHost)
            {
                m.seq = Log.Count;
                m.actor = MySeat;                               // **绝对座位**：主机 = 0
                m.preApplied = false;                           // 客机那边**没落过**这条 ⇒ 要落地
                Log.Add(m);
                Send(NetKind.Applied, m);
                Debug.Log($"[Net] 主机动作 #{m.seq} 已广播（{a}）");
            }
            else
            {
                Send(NetKind.Action, m);                        // 客机：交给主机定序
                Debug.Log($"[Net] 客机动作已发出，等主机确认（{a}）");
            }
        }

        /// <summary>本地换牌**提交**（联机局里 `Mulligan` 由主机定序 ⇒ 本地先不落地）。</summary>
        public void OnLocalMulligan(int[] marks)
        {
            _myMulligan = marks ?? new int[0];
            if (IsHost)
            {
                Debug.Log($"[Net] 主机换牌已提交（换 {_myMulligan.Length} 张）；"
                        + (_theirMulligan ? "对手也交了 ⇒ 立即定序" : "等对手提交…"));
                if (_theirMulligan) HostResolveMulligan();
            }
            else
            {
                Send(NetKind.Mulligan, new MsgMulligan { indices = _myMulligan });
                Debug.Log($"[Net] 客机换牌已提交（换 {_myMulligan.Length} 张），等主机定序…");
            }
        }

        public void OnLocalResign()
        {
            Send(NetKind.Resign, new MsgResign());
            Debug.Log("[Net] 已把「投降」告诉对面");
        }

        /// <summary>每回合末对一次指纹（**两端算的是同一个状态点**：`EndTurn` + `BeginTurn` 之后）。</summary>
        public void SendFingerprint()
        {
            if (_d == null || _d.Ctx == null) return;
            int h = NetProtocol.Fingerprint(_d.Ctx);
            _lastHashTurn = _d.Ctx.Turn; _lastHash = h;
            Send(NetKind.Hash, new MsgHash { turn = _d.Ctx.Turn, hash = h });
        }

        void Send<T>(string kind, T msg) { if (_s != null) _s.Send(kind, msg); }

        // ==================================================================
        //  收：主线程泵（`BattleDriver.Update` 调；自检里显式调）
        // ==================================================================

        public void Tick()
        {
            if (_s == null) return;
            for (int i = 0; i < _s.Inbox.Count; i++)
            {
                var f = _s.Inbox[i];
                try { Dispatch(f); }
                catch (Exception e) { Debug.LogError("[Net] 处理 " + f.kind + " 时炸了：" + e); }
            }
            _s.Inbox.Clear();
        }

        void Dispatch(NetFrame f)
        {
            switch (f.kind)
            {
                case NetKind.Action:                                   // 只有主机会收到
                {
                    if (!IsHost) { Debug.LogWarning("[Net] 客机收到了 `action`（该由主机收）—— 忽略"); return; }
                    var m = NetProtocol.Unpack<MsgAction>(f.payload);
                    if (m == null) return;
                    if (_d.Ctx == null) return;
                    if (_d.Ctx.Active != RemoteSeat)
                    {
                        Abort($"对面在**不是他回合**的时候发了一条动作（现在行动方是本机）—— 拒绝并中止");
                        Send(NetKind.Reject, new MsgReject { seq = m.seq, reason = "不是你的回合" });
                        return;
                    }
                    int code = _d.ApplyLoggedAction(m);
                    if (code != RuleCodes.OK)
                    {
                        Send(NetKind.Reject, new MsgReject { seq = m.seq, reason = RuleCodes.Describe(code) });
                        Abort($"引擎拒了对面那条动作：{RuleCodes.Describe(code)}");
                        return;
                    }
                    m.seq = Log.Count;                                  // 🔴 主机定序
                    m.actor = RemoteSeat;                               // **绝对座位**：客机 = 1
                    m.preApplied = true;   // 🔴 客机**本地已经落过这条**（乐观执行）⇒ 它收到后别再落一遍
                    Log.Add(m);
                    Send(NetKind.Applied, m);                           // 广播（客机靠它建自己的日志）
                    Debug.Log($"[Net] 主机已应用并广播对面动作 #{m.seq}（kind={m.kind}）");
                    return;
                }
                case NetKind.Applied:                                   // 只有客机会收到
                {
                    if (IsHost) return;
                    var m = NetProtocol.Unpack<MsgAction>(f.payload);
                    if (m == null) return;
                    // 日志按 seq 对齐（自己发出去的那条也会被广播回来 ⇒ 靠 seq 认领）
                    while (Log.Count <= m.seq) Log.Add(null);
                    if (m.seq >= 0 && m.seq < Log.Count) Log[m.seq] = m;
                    // 🔴 `preApplied` = 主机说「这条你本地已经落过了」（乐观执行那条路）⇒ 只认领 seq，别再落一遍
                    if (m.preApplied) return;
                    // 剩下的都要落地：**主机自己的动作** + **换牌那两条伪动作**（它们是主机定序的）
                    int code = _d.ApplyLoggedAction(m);
                    if (code != RuleCodes.OK)
                        Abort($"本机引擎拒了对面的动作（{RuleCodes.Describe(code)}，kind={m.kind}）—— 两端打岔了");
                    return;
                }
                case NetKind.Reject:
                {
                    var m = NetProtocol.Unpack<MsgReject>(f.payload);
                    Abort($"主机拒绝了我的动作：{(m != null ? m.reason : "?")}");
                    return;
                }
                case NetKind.Mulligan:                                  // 主机收
                {
                    var m = NetProtocol.Unpack<MsgMulligan>(f.payload);
                    _theirMulligan = true; _theirMulliganMarks = m != null ? m.indices : new int[0];
                    Debug.Log($"[Net] 主机收到对手换牌（{(_theirMulliganMarks != null ? _theirMulliganMarks.Length : 0)} 张）");
                    if (_myMulligan != null) HostResolveMulligan();
                    return;
                }
                case NetKind.Hash:
                {
                    var m = NetProtocol.Unpack<MsgHash>(f.payload);
                    if (m == null || _d.Ctx == null) return;
                    if (m.turn != _d.Ctx.Turn) { Debug.Log($"[Net] 指纹回合对不上（对面 {m.turn} / 本机 {_d.Ctx.Turn}）—— 可能是交叉到达，先不判"); return; }
                    int mine = NetProtocol.Fingerprint(_d.Ctx);
                    if (mine != m.hash)
                        Abort($"**状态指纹对不上**（第 {m.turn} 回合：对面 {m.hash} / 本机 {mine}）—— 两端已经打岔");
                    else Debug.Log($"[Net] 第 {m.turn} 回合指纹一致（{mine}）✅");
                    return;
                }
                case NetKind.Resign:
                {
                    Debug.Log("[Net] 对面投降了");
                    if (_d != null) _d.NetRemoteResign();
                    return;
                }
            }
            Debug.LogWarning("[Net] 不认识的联机消息：" + f.kind);
        }

        // ==================================================================
        //  换牌阶段定序（**只有主机能调**）
        // ==================================================================

        void HostResolveMulligan()
        {
            var seat0 = _myMulligan ?? new int[0];                       // 主机视角：seat0 = 主机自己
            var seat1 = _theirMulliganMarks ?? new int[0];
            // 🔴 **顺序固定**（先主机、后客机）：`RuleCore.Mulligan` 会掷 `ctx.Rng`
            //    ⇒ 两端必须按同一个顺序调，不然两边洗出来的牌不一样。
            AppendLogAndApply(NetActionKind.Mulligan, actor: 0, marks: seat0);
            AppendLogAndApply(NetActionKind.Mulligan, actor: 1, marks: seat1);
            AppendLogAndApply(NetActionKind.MulliganDone, actor: 0, marks: null);
            Debug.Log($"[Net] 换牌已定序（主机 {seat0.Length} 张 / 客机 {seat1.Length} 张）");
        }

        /// <summary>主机：把一条「定序过的」消息记进权威流 → **本机先落地** → 广播给客机。
        /// 换牌那两条伪动作走的就是这条路（所以它们**能被重放**，重连才追得回来）。</summary>
        void AppendLogAndApply(int kind, int actor, int[] marks)
        {
            var m = new MsgAction { kind = kind, actor = actor, marks = marks, seq = Log.Count, preApplied = false };
            Log.Add(m);
            _d.ApplyLoggedAction(m);                        // 主机自己：座位是绝对的，不翻
            Send(NetKind.Applied, m);                       // 客机：收到后按同一顺序落地
        }

        // ==================================================================
        //  重连（正本 §5·6：**主机持权威动作流，客机重建 + 全量重放**）
        // ==================================================================

        /// <summary>主机：把「开局参数 + 到目前为止的全部动作」交出来（`NetSession.ResumeProvider`）。</summary>
        public (MsgStart start, List<MsgAction> actions) ResumeData()
        {
            var list = new List<MsgAction>();
            foreach (var m in Log) if (m != null) list.Add(m);
            return (_start, list);
        }

        /// <summary>客机：收到 `resume` ⇒ **丢掉本地状态、从种子重建、按序全量重放**（不许增量补）。</summary>
        void OnResumed(MsgStart start, List<MsgAction> actions)
        {
            if (IsHost) return;
            Debug.Log($"[Net] 重连：重建这一局并重放 {actions.Count} 条动作");
            Log.Clear();
            foreach (var m in actions) Log.Add(m);
            if (_d != null) _d.NetReplayFromNet(start, actions);
        }

        /// <summary>开场时把 `MsgStart` 记下来（重连要用）。</summary>
        public void RememberStart(MsgStart start) { _start = start; }
    }
}
