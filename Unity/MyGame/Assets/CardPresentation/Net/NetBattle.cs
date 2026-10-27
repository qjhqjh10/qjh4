// NetBattle.cs — 联机**对局**那一层：开局交接 · 动作中转 · 座位口径（绝对编号·不翻译） · 状态指纹 · 换牌定序 · 重连重放
//
// 设计判据全文 → `资料/联机P2P_设计与交接.md` §五 / §六（N3~N5）。这里只写**实现要点**：
//
// 🔴 **主机的三件事**（别让别处再判一遍）：
//   ① **开局参数**（种子 / 两副牌的顺序 / 谁先手 / 战场）由主机算完下发；
//   ② **动作定序**（每条动作一个 `seq`，写进权威动作流 —— 重连时靠它重放）；
//   ③ **换牌阶段的定序**（`Mulligan` 会掷 `ctx.Rng`，**两边的调用顺序必须一模一样**）。
//
// 🔴 **座位是【绝对的】：主机 = 0 · 客机 = 1，两端跑同一套编号**（判据 → `NetProtocol.Fingerprint` 的注释、
//   正本 §5·3）。⇒ 对局中**没有任何座位翻译**：`targetP` / `slot` / `handIdx` / `winner` / `active` 全都不用翻，
//   视图方向只由 `BattleDriver._me` 决定（客机被 `SetMySeat(1)` 摆到下面）。
//   ⚠️ **2026-10-19（`A1114`(a)）就地订正（铁律 5）**：本节原来写的是**镜像时代**的话 ——
//     「对局中两端都是『自己 = 0 号位』（`BattleDriver._me` 焊死）」＋「座位翻译只在 `NetProtocol.Seat()`
//      一处做（收到对面动作时翻 `targetP`）」＋「联机局一律 `shuffle:false` + 用主机下发的顺序
//      （见 `NetPendingBattle.DeckOrder`）」。**三句全都不成立**：
//     ① 镜像那套**2026-09-26 已废弃**（引擎层面走不通，第 40 步 `Unstable` 当场分叉）；
//     ② `NetProtocol.Seat()` **不在任何运行路径上**（只剩 `NetSelfTest` I①~I③ 在测它自己）；
//     ③ `NetPendingBattle.DeckOrder` **这个字段根本不存在**，`shuffle` 门也早没了 ——
//        现在是「两端同种子 + 同输入 ⇒ 各洗各的必然一致」（`NetPendingBattle.NoShuffle` 只是保留开关）。
//        🔴 **2026-10-19（T04 复核 · 铁律 5）：那个「主机下发的顺序」现在【存在哪】写清楚** ——
//        它**不是**一个独立字段，而是**跟着两副牌本身走**：`NetPendingBattle.Seat0Deck` / `Seat1Deck`
//        （类型 `PlayerDeck`，牌序 = `PlayerDeck.CardIds` 这个 `List<string>`；来源 = 开局包的
//        `MsgStart.hostDeckJson / clientDeckJson` 反序列化，见 `NetBattle.cs` 的 `FromStart`）。
//        ⛔ 别再去搜 `DeckOrder`（全仓只剩下面这一句历史引用）。
//   ⛔ 别再把这四行改回镜像口径；同一族的第三处 = `BattleDriver.NetReplay` 的 `<summary>`（那份文件不在本批白名单）。
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

        /// <summary>🆕 **2026-10-15（A383）**：本局**真正的模式号**（原版 `MatchData.playMode`）。
        /// 由**主机**定、随开局包下发（原版也是服务端按排到的那场活动给 `playMode` ——
        /// `MatchMakerManager.FindMatch` 取 `IPlayEvent` slot 0 = `EventPlayMode`）。
        /// 字段还是那个 `Mode` 字符串，只是**编码换了**：老编码只有 `"Classic"`/`"Skirmish"`
        /// （= 能不能分辨模式），现在就是 `GameMode` 的枚举名 ⇒ 旧录像/旧开局包照样读得回来。
        /// ⚠️ 认不出的串由 <see cref="PlayModeNames.Parse"/> **出声**（不是静默）并退回 `Classic`。</summary>
        public GameMode PlayMode { get { return PlayModeNames.Parse(Mode); } }

        /// <summary>本局的**规则参数**（经典 / 遭遇那两套）。
        /// 🔴 判据与单机那条路**同源**：**只看这副牌**（`CardDeck.gameMode`，`资料/加时与冲突模式_原版规格.md` §2.7）——
        /// 主机那副先看，没有就看客机那副；两副都不在包里（空卡组那条极端）才退回按 `PlayMode` 推。
        ///
        /// ⚠️ **为什么不直接 `GameplayVariables.For(PlayMode)`**：那样「12 张的牌从练习窗开出去」
        /// 这类局面（原版 UI 不允许、我们这边点得到，见 `BattleDriver.BeginFromDeckLibrary` 的注）
        /// **录像重放会按 30 张重建** —— 判据从牌上取就没有这个缝。</summary>
        public GameplayVariables Vars
        {
            get
            {
                var d = Seat0Deck ?? Seat1Deck;
                bool skirmish = d != null ? d.IsSkirmish : (PlayMode == GameMode.Skirmish);
                return GameplayVariables.For(skirmish ? GameMode.Skirmish : GameMode.Classic);
            }
        }
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
                // 🔴 **A961（2026-10-18，主对话裁定「接着做完」）**：`MsgStart.myName / foeName`
                //   （`NetProtocol.cs:77` 的 `MsgStart.myName/foeName`，2026-10-19 现核）**两个都是对端可控的** ⇒ 与另外三个入口**同一处口径**：先钳
                //   （复用 `NetSession.ClampPeerText`）。
                //   ⚠️ 这两个字段**目前全仓没有消费方**（`Net/NetMatchmaking.cs:549` 那句注释已记这一点）
                //      ⇒ 钳在这里是**给将来接上界面的那道口先装上限**。
                //   🔴 **真正在界面上显示对端名的那条路不是这里** —— 是
                //      `Net/NetMatchmaking.cs:475`（`FoeName = st.myName`）→ `BattleDriver` 的敌方名；
                //      那份文件**不在本笔白名单**⇒ **还没钳**，如实记在交件报告 §9 续（别以为已经封上了）。
                MyName = NetSession.ClampPeerText(isHost ? s.myName : s.foeName, NetProtocol.MaxPeerNameChars),   // 它的值就是**绝对先手座位**
                FoeName = NetSession.ClampPeerText(isHost ? s.foeName : s.myName, NetProtocol.MaxPeerNameChars),
                Raw = s,
            };
        }

        /// <summary>重连重放用的别名（同一个东西，名字不同只是为了调用处读得懂）。</summary>
        public static NetPendingBattle FromReplay(MsgStart s, bool isHost) { return FromStart(s, isHost); }
    }

    /// <summary>一条原生动作 + 它带的面板答案（🔴 **座位是绝对编号** ⇒ 这份线上包**不翻**任何座位）。</summary>
    public class NetLocalAction
    {
        public MsgAction Wire;
    }

    /// <summary>
    /// 联机层要的那几件事（`BattleDriver` 实现它；**联机自检**用一个裸 `BattleContext` 的轻实现）。
    /// 抽这一层是为了让「动作中转 / 座位翻译 / 指纹 / 重放」**能被自检直接验**
    /// —— 不然要往一个场景里塞两个完整对局驱动才验得了。
    /// </summary>
    /// <summary>🆕 **2026-10-25（联机域审查 · D 域）**：**纯表现**类的对手事件（原版三条 RPC）。
    ///
    /// <para>三条都**不参与局面**、**不改指纹**、**不判胜负** —— 局面正确性由「主机定序动作流 + 每回合指纹」
    /// 保证（见 `NetProtocol.Fingerprint`）。⇒ 宿主不实现这个接口**不影响对局**，只会少三处表现
    /// （`Dispatch` 里会**出声**，⛔ 不静默）。</para>
    ///
    /// <para>判据：`BattleCommsManager.cs` 的 `SendCardSeen:654 / PUNReceiveCardSeen:1359` →
    /// `BattleManager.ShowEnemyCardSeen`（`:30` → `CardScript.ShowEnemyCardInHand`）·
    /// `SendCardReorder:708 / PUNReceiveCardReorder:1372` → `BattleManager.ShowEnemyHandReorder`
    /// （`:31` → `PlayerHand.HandCardMovedToNewIndex`）· `SendMatchFinished:701 /
    /// PUNReceiveMatchFinished:1320` → `BattleManager.ReceiveMatchFinished`。</para></summary>
    public interface INetFoePresentationHost
    {
        /// <summary>对面在手里查看了某张牌（`uniqueInGameID` = **局内唯一 id**，⛔ 不是卡 id）。</summary>
        void NetRemoteCardSeen(int uniqueInGameId);
        /// <summary>对面手牌换位（`uniqueInGameID` → 新下标 `newIndex`）。</summary>
        void NetRemoteCardReorder(int uniqueInGameId, int newIndex);
        /// <summary>对面说「我这局打完了」。⚠️ **不判胜负** —— 只用来开「等对手最后几条动作」那个窗口。</summary>
        void NetRemoteMatchFinished();
    }

    public interface INetBattleHost
    {
        BattleContext Ctx { get; }
        /// <summary>把权威流里的一条落到引擎上（引擎那段在 `NetApply.Apply`，**只有那一份**）。
        /// 🔴 座位是**绝对编号** ⇒ **不用翻**（翻座位那套已作废，见 `NetProtocol.Fingerprint` 的注释）。</summary>
        int ApplyLoggedAction(MsgAction m);
        void NetSay(string s);
        /// <summary>对面投降 ⇒ 本机判胜（`RuleCore.Forfeit` + 表现层刷新都在宿主里）。
        /// <para>🆕 **2026-10-18（A913）**：多带一个**理由码**（原版 `DeadHero(bool, BattleResult)`
        /// 的第二个实参）。🔴 **本口服务三档不同的理由**（这就是它必须带码的原因）：
        /// `NetKind.Resign` 那条消息 ⇒ <see cref="BattleResult.Forfeit"/>(2)；
        /// 掉线倒计时到点 ⇒ <see cref="BattleResult.Disconnect"/>(3)；
        /// 对面没投降就把台关了（`bye`）⇒ 也是 3。⛔ **不许写默认值** —— 三档各写各的，
        /// 给了默认值就有一档会**静默说错**（本批反复强调的那个模式）。</para></summary>
        void NetRemoteResign(BattleResult reason);
        /// <summary>🆕 **2026-10-18（A914 第四续）**：**本机自己**判负（对手没输，是**我**这一端掉了、
        /// 重连也失败）。
        ///
        /// <para>判据 = 原版 `BattleManager__FailedToReconnectAfterDisconnect.c:26-27`：
        /// `AddResignAction(param_1, 1)` + `DeadHero(param_1, 1, 3)`（第二个实参 `1` = **本机死**）。
        /// 与 <see cref="NetRemoteResign"/> 的差别**只有一个座位号**（那边判 `1 - _me`）。</para>
        ///
        /// <para>🔴 **落地必须走既有的记账口**（宿主那份 = `RecRaw(RecKindForfeit)`【= 原版 `AddResignAction`
        /// + 本地录像】+ `RuleCore.Forfeit(Ctx, _me, reason)` + 刷新）—— ⛔ **调用方不许自己直接调 `RuleCore.Forfeit`**
        /// （那会绕过录像口 ⇒ 回放从这里开始演成另一局，`CLAUDE.md` §三）。</para>
        ///
        /// <para>🔴 **它同时修掉一个两端不一致**：原来两端都判「对面」负 ⇒ 各判自己赢、`ForfeitedBy` 两家各说各的；
        /// 现在本端判 `_me`、对端判 `1-对面_me` ⇒ **两端报的是同一个座位**。</para>
        ///
        /// <para>🆕 **2026-10-18（A913）**：理由码同 <see cref="NetRemoteResign"/>（今天唯一一档 =
        /// `Disconnect`(3)，判据见上）。</para></summary>
        void NetSelfResign(BattleResult reason);
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
            nb.WireSession();                                  // 客机：收到 `resume` ⇒ 重建 + 全量重放（+ A880 那两条）
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
            nb.WireSession();                                  // 客机：收到 `resume` ⇒ 重建 + 全量重放（+ A880 那两条）
            Debug.Log($"[Net] 对局已接上联机层（本机 = {(pb.IsHost ? "主机" : "客机")}，种子 {pb.Seed}，"
                    + $"战场 {pb.Arena}，本机座位 {pb.MySeat}，先手座位 {pb.FirstSeat}）");
            return nb;
        }

        /// <summary>「这一局打岔了，但**这一跳判不了**」的收尾：只出声、**不判胜负、也不终止网络**。
        /// <para>🆕 **2026-10-26（批次 6 · G15）**：它现在**只剩两个调用点**：
        /// ① `case NetKind.Hash` 的**状态指纹对不上** —— 那是**我们自设**的检测（原版没有这一跳），而 `SendFingerprint`
        /// 两端都发 ⇒ 两端都可能判出来 ⇒ 「谁赢」都可能是错的 ⇒ 保持「不判」；
        /// ② `case NetKind.Reject`（主机拒了我那条动作）—— 只报、不判，判负那一跳由紧跟的
        /// `NetKind.Desync` 走 <see cref="AcceptPeerDesync"/>（理由见那一支）。
        /// 原版能判结果的两条同类路径已经落地：发现方 <see cref="AbortWithVictory"/>
        /// （= `CancelMatchWithVictory` ⇒ 判本机胜）、收方 <see cref="AcceptPeerDesync"/>
        /// （= `ReceiveEnemyDesync` ⇒ 认账判自己负）。⛔ 别把这两条改回 `Abort`（那会把判胜负丢掉）。</para></summary>
        void Abort(string why)
        {
            if (_aborted) return;
            _aborted = true;
            Debug.LogError("[Net] 🔴 联机对局中止：" + why
                         + "（两端状态可能已经不一致 —— 本工程没有回滚，如实说出来，不假装没事）");
            if (_d != null) _d.NetSay("联机对局中止：" + why);
            // 🔴 **红线**：提示行会被后面的消息顶掉、也容易被漏看 ⇒ **再弹一个**
            //    （`项目任务.md` §三 第 14 条 表里的第 6 条）。⚠️ 战场里也弹得出来 ——
            //    `WindowsManager` 挂在 `DontDestroyOnLoad` 的壳上（`ShellRuntime.Awake`）。
            NetRuntime.Notice("联机对局中止：" + why
                            + "\n两端的状态可能已经不一致了，这一局没法继续。"
                            + "本工程**没有回滚**，如实告诉你，不假装没事。");
        }

        // ==================================================================
        //  🆕 2026-10-26（批次 6 · G15）：原版「对手打岔」那一族 —— `OnOpponentDesync` / `CancelMatch`
        // ==================================================================
        //  🔴 **就地订正（铁律 5）**：本节原来写的是「**`CancelMatch` 一族 —— 原版有对应物，但我们不建**」
        //    （理由 =「唯一触发源 = 服务端检测到对手 desync，本地没有这条数据 ⇒ 铁律 11 例外①」）。
        //    那句话**只对了一半**：原版那条链的**检测在客户端**（本机结算对面那条动作失败时），
        //    只有最后的「记账」才在服务端。而我们这边**本机检测早就有了** —— `Abort` 那几个调用点就是它
        //    （对面不是他的回合就发动作 / 本机引擎拒了对面的动作 / 主机拒了我的动作 / 状态指纹对不上）。
        //    ⇒ 本批把原版那条链补上（判据逐句）：
        //
        //   · **发现方** = 原版 `BattleManager.CancelMatchWithVictory(errorLog)`
        //     （`decomp_full/BattleManager__CancelMatchWithVictory.c:11-33`）四步：
        //     ① 置 `matchData.desyncCancelledGame = true` + `alreadyCancelledMatch = true`（**只认一次**）；
        //     ② 记「本站胜」（`PlayerDataManager.RecordMatchWonByCancel` —— **服务器那一笔账，我们不做**）；
        //     ③ **`commsManager.SendDesync()`**（`BattleCommsManager__SendDesync.c:20-27` =
        //        `photonView.RPC("PUNReceiveEnemyDesync", PhotonTargets.All)`，**无参**）；
        //     ④ 触发 `OnOpponentDesync(true)` ⇒ `BattleErrorUIManager.OpponentDesyncCancelMatch(true)`
        //        （`cpp2il/Everguild/BattleErrorUIManager.cs:537-560`）：
        //        `WindowsManager.ShowPopUp(EnemySyncError, localizeTexts:false, closeOnEsc:false, SubmitButton, onSubmit)`，
        //        submit = `BattleManager.CancelMatch(MatchWon)`。
        //   · **`BattleManager.CancelMatch(bm, bool MatchWon)`**（`decomp_full/BattleManager__CancelMatch.c:18-29`）：
        //       `MatchWon == false` ⇒ `BattleNetworkManager.LeaveBattle()`（**不判结果**）；
        //       否则 ⇒ `AddResignAction` + **`DeadHero(bm, 0, 5)`**（那个 `0` = 本机不是负方 ⇒ **判本机胜**；
        //       理由码 `5` = `Cancelled`，与 `RuleEngine/Core/RuleCore.cs` 的 `BattleResult.Cancelled` 逐值相同）。
        //       ⛔ 别读成「取消者输」：`CancelMatch.c` 的 `param_2` 是**负方座位**，写反就是把胜负弄反。
        //   · **收方**（`PUNReceiveEnemyDesync` → `BattleManager.ReceiveEnemyDesync`，
        //     `decomp_full/BattleManager__ReceiveEnemyDesync.c:22-45`）：**只认账**
        //     （`PlayerDataManager.RecordMatchWonByCancelEnemy` = 记「对面靠取消赢了我」），
        //     **不弹窗、不判自己、不动本机局面**（谁赢由服务端记）。
        //
        //  🔴 **我们的落地（两条路，落点都不新造口）**：
        //   · 发现方 = <see cref="AbortWithVictory"/>：发 `NetKind.Desync`（= ③）⇒ 弹窗
        //     `Loc.T("Battle/EnemySyncError")` + 提交钮 `Loc.T("MainMenu/General/OK")`（= ④；两条词条都在
        //     `Core/Loc.cs`、值是从官方表录的）⇒ 点确定的回调 = `INetBattleHost.NetRemoteResign(BattleResult.Cancelled)`
        //     （= `CancelMatch(true)` 的 `DeadHero(对面, 5)`；宿主那份仍走既有记账口 `RecRaw` + `RuleCore.Forfeit`）。
        //   · 收方 = <see cref="AcceptPeerDesync"/>（`case NetKind.Desync`；`case NetKind.Reject` 仍走 `Abort`
        //     只报不打分 —— 判负那一跳由紧跟的 `desync` 那条走，理由见那一支的注释）：
        //     **不弹窗**（原版收方也没有），只**判本机负**、理由码同样 `Cancelled` —— 因为**我们本地没有服务器
        //     那一笔账**，不收尾这一局就永远挂着（⚠️ 如实写：这一句是**本地等价**，原版是「服务端记一笔、本地不动」）。
        //     两端因此报**同一个弃权座位**（同 `A914` 那条口径）。
        //   · **`MatchWon == false`（`CancelMatchWithoutResult` / `LeaveBattle`）今天【没有落地】**：原版它由
        //     `PUNReceiveWrongRoom` / `EventPlayerDisconnected` 触发，我们这两档各自的收尾**已经有了**
        //     （`NetMatchmaking` 的 `ModeMismatch` 那条 + `HandlePeerClosed` 那条）⇒ 再建一个「不判结果直接离开」
        //     的口 = 第三条同类路（判据还落在别人文件里）。**唯一仍走 `Abort`（只报不打分）的是「状态指纹对不上」**
        //     —— 那是**我们自设**的检测（原版没有这一跳），且**两端可能同时判出来** ⇒ 谁赢都可能是错的 ⇒ 保持「不判」。
        //   · **`BattleErrorUIManager.SetupErrors()` 那 5 条订阅，逐条对账**（判据 = `decomp_full/Everguild.BattleErrorUIManager__SetupErrors.c`，
        //     5 处 `System_Delegate__Combine`，挂在 `BattleManager` 的 `+0x1c0/+0x1b0/+0x1b8/+0x1c8` 与
        //     `UIGenericEventCatcher.Source(+0x148)` 上）：
        //     ① `OnConnectionStatusChanged` ⇒ 我们的 `HandlePeerLost` / `HandlePeerClosed` / `Tick` 那条「对面回来了」的边沿（**有等价**）；
        //     ② `UpdateReconnectStatus` ⇒ `ReportReconnectStatus`（**有等价**，每秒一拍）；
        //     ③ `OnOpponentDesync` ⇒ **本批补上**（`NetKind.Desync` + `AbortWithVictory` + `AcceptPeerDesync`）；
        //     ④ `UIGenericEventCatcher.Source`（`GameBlocked` = 重复连接 ⇒ 弹 `CustomErrors/DuplicateConnection` 后 `Application.Quit()`）——
        //        **原版触发源本地不存在**（那是 Photon 房间层的「同一账号重复进房」）⇒ 例外①，⛔ 别自己造一个；
        //     ⑤ `Action<object>`（`SaveMatchFailed` ⇒ 弹 `CustomErrors/ErrorSavingMatch`）—— **原版触发源在服务端**
        //        （局末成绩上传失败）⇒ 例外①，同上。
        //  判据全文 → `资料/普查产出_1019/W_战斗线_T07结算_1019b.md` §一·3（原结论保留，只订正「不建」那句）。

        /// <summary>🆕 **2026-10-26（批次 6 · G15）**：发现方主张胜利（= 原版 `CancelMatchWithVictory`）。
        /// 与 <see cref="Abort"/> 的差别**只有一处**：它发 `NetKind.Desync` 并且**点了确定就判本机胜**
        /// （`BattleResult.Cancelled`）；<see cref="Abort"/> 只报信、不判。</summary>
        /// <param name="why">为什么判「打岔了」（进日志与提示行；⛔ 它不上对端屏幕 —— 原版那条 RPC 不带理由）。</param>
        void AbortWithVictory(string why)
        {
            if (_desyncClaimed) return;      // 原版 `alreadyCancelledMatch`：**只认一次**
            _desyncClaimed = true;
            _aborted = true;                 // 顺带停掉「再发动作」（同 `Abort` 那一条）
            _desyncClaims++;
            Debug.LogError("[Net] 🔴 联机对局打岔（**判本机胜**）：" + why
                         + "（原版 `CancelMatchWithVictory` → `SendDesync` → `OnOpponentDesync(true)` → "
                         + "`CancelMatch(true)` = `DeadHero(对面, 5)`；理由码 `Cancelled`(5)）");
            if (_d != null) _d.NetSay("对面那一手和本机对不上 —— 这一局按打岔结束（判你胜）");
            // ③ 先告诉对面（原版 `commsManager.SendDesync()` 在弹窗**之前**）——
            //    对面收到 `NetKind.Desync` 就走 `AcceptPeerDesync`（认账判自己负）。
            Send(NetKind.Desync, new MsgDesync());
            // ④ 弹窗 + 提交回调（原版 `WindowsManager.ShowPopUp(EnemySyncError, …, SubmitButton, onSubmit)`）。
            NetRuntime.ShowSyncErrorPopup(() =>
            {
                // = `CancelMatch(MatchWon: true)` ⇒ `AddResignAction` + `DeadHero(对面, 5)`。
                // ⚠️ 点确定那一刻这一局可能已经被别的路径收掉了（玩家先投降 / 对面先认账）——
                //    判负入口自己那道闸管着（`RuleCore.Forfeit` 见 `Winner != 0` 一律挡），这里只保证**有宿主**。
                if (_d == null)
                {
                    Debug.LogWarning("[Net] 打岔窗点了确定，但本类**没有挂宿主**（`_d == null`）"
                                   + " ⇒ 判胜这一跳走不了（如实出声，不假装收干净了）");
                    return;
                }
                Debug.Log("[Net] 打岔窗点了确定 ⇒ `NetRemoteResign(Cancelled)`（= 原版 `CancelMatch(true)` 的 "
                        + "`DeadHero(对面, 5)` ⇒ **判本机胜**）");
                _d.NetRemoteResign(BattleResult.Cancelled);
            });
        }

        /// <summary>🆕 **2026-10-26（批次 6 · G15）**：收方认账（= 原版 `BattleManager.ReceiveEnemyDesync`
        /// 的本地等价：`RecordMatchWonByCancelEnemy` 记「对面靠取消赢了我」）。
        /// <para>⚠️ 原版收方**不弹窗、不动本机局面**（谁赢由服务端记）；我们本地没有服务器 ⇒ 必须在这里把这一局
        /// 收掉（判**本机负**、理由码 `Cancelled`），否则对端已经结束、本机永远挂着。</para>
        /// <para>⛔ 别用 `NetRemoteResign`：那是「对面负」；收方这一档是**自己负**
        /// （原版 `ReceiveEnemyDesync` 记的是自己输）。</para></summary>
        void AcceptPeerDesync(string why)
        {
            if (_desyncAccepted) return;     // 只认一次（原版 `alreadyCancelledMatch` 同效）
            _desyncAccepted = true;
            _aborted = true;                 // 顺带停掉「再发动作」（同 `Abort` 那一条）
            _desyncAccepts++;
            Debug.LogWarning("[Net] 对面声称这一局打岔了 ⇒ **认账、判本机负**（原版 `ReceiveEnemyDesync` 的 "
                           + "`RecordMatchWonByCancelEnemy` 的本地等价；理由码 `Cancelled`(5)，不弹窗）：" + why);
            if (_d == null)
            {
                Debug.LogWarning("[Net] 认账那一跳没走成：本类**没有挂宿主**（`_d == null`）—— 如实出声，不假装收干净了");
                return;
            }
            if (_d.Ctx != null && _d.Ctx.IsOver)
            {
                Debug.Log("[Net] 对面声称打岔时，本机这一局**已经结束了**（`Ctx.IsOver`）⇒ 不再判一次（同一个判据管两档）");
                return;
            }
            _d.NetSay("这一局和对面打岔了 —— 按对面的账，这一局判你负");
            _d.NetSelfResign(BattleResult.Cancelled);
        }

        /// <summary>自检读口：本机以「打岔」为由**主张胜利**过几次（原版 `CancelMatchWithVictory` 那一跳）。</summary>
        public int DesyncClaimsForTest { get { return _desyncClaims; } }
        /// <summary>自检读口：本机以「打岔」为由**认账判自己负**过几次（原版 `ReceiveEnemyDesync` 那一跳）。</summary>
        public int DesyncAcceptsForTest { get { return _desyncAccepts; } }
        int _desyncClaims, _desyncAccepts;
        /// <summary>「本机已经以打岔为由主张过胜利」/「本机已经认账判过自己负」—— 各只认一次
        /// （原版 `CancelMatchWithVictory` 的 `alreadyCancelledMatch` 那个闸；⛔ 别拿 `_aborted` 兼职：
        /// 它还会被 `Abort`（指纹那一档）与别的收尾路径立起来，兼职会把判负那一跳静默吞掉）。</summary>
        bool _desyncClaimed, _desyncAccepted;

        // ==================================================================
        //  🆕 2026-10-17（B13·A880 / B17·A900+A901）：对面**掉线 / 主动离开** —— 出厂接线 + 到点判负
        // ==================================================================
        //  账 `A880`：`NetSession.OnClosed` / `OnPeerLost` 原来**全仓零接线**（只有定义 + 三处 `Invoke`）
        //  ⇒ 对面主动退出、或对面掉线，**本机玩家什么都不会发生**（静默 —— 红线）。
        //  判据（全量反编译 `d:/2/tools/decomp_full/`，逐条）：
        //    · **掉线**：`BattleNetworkManager__SetOpponentDisconnected.c:13`（`LogWarning("Opponent disconnected")`）
        //      → `:24` `BattleManager.DisconnectedDuringBattle(false)` →
        //      `BattleManager__ShowDisconnectionPopup.c:14` 把连接状态置成 **2**（枚举
        //      `BattleManager.ConnectionStatus`：0 Connected · 1 Reconnecting · 2 WaitingForOtherPlayerToReconnect
        //      · 3 Disconnected · 4 EnemyForfeit · 5 DisconnectedAfterTryingToReconnect，`dump.cs:27413-27422`）
        //      → `Everguild.BattleErrorUIManager__ConnectionStatusChanged.c:41-43`
        //      ⇒ `ShowDisconnectionPopUp(false)` ⇒ `…__ShowDisconnectionPopUp.c:51` **`WindowsManager.ShowPopUp(文案)`**；
        //      文案的键 = `Battle/HUD/WaitOpponentConnectionMsg`（13 个战场场景各一份，
        //      `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4062.json:26`）。
        //      同一刻还有 `BattleManager__ShowDisconnectionPopup.c:27` 的 **`ClockManager.PauseClock()`**。
        //    · **主动离开房间**：和掉线**同一条链** —— `NetworkCustomManager__OnPhotonPlayerDisconnected.c:24`
        //      → `BattleNetworkManager__EventPlayerDisconnected.c:45-52`（对局中 `battleConnectionStatus == connected(10)`
        //      且名字对得上 ⇒ `SetOpponentDisconnected()`；其余状态 `CustomDebug.LogError`，**不静默**）
        //      ⇒ 也是状态 2、也是那一扇窗（**原版没有「对面离开了」的专属窗**）。
        //  我们这三层与上面**同形**：`WindowsManager.ShowPopUp` = `NetRuntime.Notice`
        //  （`NetRuntime.cs:104-105` 弹的就是它；战场里也弹得出来，同 `Abort` 那条的注）、
        //  界面上的「提示行」= `INetBattleHost.NetSay`、日志 = `Debug.Log`。
        //  ⚠️ **B13 记的那两条「原版有、我们还没有」⇒ 2026-10-17 B17 已经补上**（别再照抄旧结论）：
        //    ① **对局时钟暂停** ⇒ 见下面 `ClockPaused` 那一段（闸只有一处 = `BattleDriver.TickClock`）；
        //    ② **`ForfeitDisconnectedEnemy` 那个倒计时** ⇒ 见下面 A900 那一段。
        //  ✅ **B13 留的那一条「还差一条」—— 2026-10-17（B27·A912）已裁、已落地**（口径全文见
        //    `HandlePeerClosed` 头上那一段）：**按「有没有说再见」分两档** ——
        //      · **心跳超时 / 连接断**（对面**没有**说 `bye`）：保持原样 ⇒ 停表 + 30 秒倒计时 + 到点判弃权
        //        （他可能真回来 —— 对局内重连本来就是支持的）；
        //      · **`bye`**（对面**明确关掉**了这一局）：**直接判他弃权**（对面收尾了、不会回来，
        //        而 `bye` 必定把会话置成 `Closed` ⇒ 接进倒计时就是「数 30 秒然后什么都不做」）。

        /// <summary>这一局挂在会话上的回调**只在这一处接**（两个 `Attach` 都走它 —— 一条规矩一处写）。</summary>
        void WireSession()
        {
            if (_s == null)
            {
                // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：联机层挂上了、
                //   却没有会话 ⇒ 之后 `bye`/掉线/RESUME **一条都收不到**（`Send` 那边也发不出去）。
                //   自检里有故意传 `null` 的夹具（那一档正常）；真 Play 出现这一行就是缺陷。
                Debug.LogWarning("[Net] NET/BYE-R7/no-session 对局联机层挂上了，但**本类没有会话**（`_s == null`）"
                               + " ⇒ 之后 `bye` / 掉线 / `resume` 一条都收不到（自检夹具的正常档；"
                               + "真 Play 看到它就是缺陷）");
                return;
            }
            _s.OnResumed += OnResumed;            // 客机：收到 `resume` ⇒ 重建 + 全量重放
            _s.OnPeerLost += HandlePeerLost;      // 🔴 A880①：对面掉线（心跳超时 / 连接断）
            _s.OnClosed += HandlePeerClosed;      // 🔴 A880②：对面主动离开这一局（收到 `bye`）
            // 🆕 2026-10-17（B23·A902 附带那一格）：**接上来的那一刻对面就已经不在了**。
            //   为什么会有这一格：开局包（`MsgStart`）与「真进战场」之间隔着一次 `LoadScene`
            //   （客户端还可能被 `HoldForPresentation` 多留一口气）—— 对面正好在这一小段里掉线 / 离开的话，
            //   那两条回调**在挂上之前就烧掉了**，本类再也收不到 ⇒ 这一局会**静默地停在那里**
            //   （而大厅那一层那一刻已经交权：`LobbyHandled` 被 `Attach` 置成了 false，它按定义不再开口）。
            //   原版那一刻在对面侧就是「对局中对手掉线 / 离开房间」（`SetOpponentDisconnected` ⇒ 弹窗 +
            //   停表 + 30 秒倒计时，见本节头部那两条）⇒ **这里补的就是接上时补做那一次判断**。
            //   ⚠️ **只看这两档**：`WaitingReconnect`（对面掉线）与 `Closed`（对面捎过 `bye`）。
            //      `Off` 是**本机自己关的**（`NetSession.Close`），`Lobby`/`InBattle` 是正常那一帧 ——
            //      都不报（同族先例：`Editor/BattleScene.cs` 那个 `RecordingTransport` 夹具就是 `Off` 接上来的，
            //      在那里多弹一条会把它的通知计数打乱）。
            if (_s.State == NetState.WaitingReconnect) HandlePeerLost();
            else if (_s.State == NetState.Closed)
            {
                Debug.LogWarning("[Net] 接上对局联机层时会话**已经关上了**（对面在这之前就离开了 / 重连被拒）"
                               + $" ⇒ 补报一次：{_s.StatusText}");
                HandlePeerClosed(_s.StatusText);
            }
        }

        /// <summary>拆掉这份接线（`BattleDriver.AttachNet(null)` 会调）。
        /// 为什么要它：会话（`NetRuntime.Session`）**活得比一局久** —— 不摘的话，上一局那个
        /// `NetBattle` 还挂在同一个会话上，下一局对面一掉线就**多弹一次窗**（说错话）。</summary>
        public void Detach()
        {
            if (_s == null) return;
            _s.OnResumed -= OnResumed;
            _s.OnPeerLost -= HandlePeerLost;
            _s.OnClosed -= HandlePeerClosed;
            // 🆕 2026-10-17（B17）：把这一局的**闸与倒计时**一起收干净 ——
            //   不收回来的话，被换下去的那个 `NetBattle` 还记着「时钟暂停」，而驱动侧的
            //   `NetClockPaused` 读的是**新的**那个 `_net`（所以时钟本身不会真停），
            //   但那个旧对象身上的倒计时会继续跑到点、**判一场已经不在的局的负**（静默、且说错话）。
            //   ⚠️ `hideWindow: false` —— 换手那一刻台面上那扇窗可能是下一局要用的。
            _peerGone = false;
            CancelReconnectCountdown(hideWindow: false);
        }

        /// <summary>对面掉线那一刻起为真；观察到会话回到 `InBattle` 那一刻清掉（只用来说「他回来了」那一句）。</summary>
        bool _peerGone;

        /// <summary>对面**掉线**了（心跳超时 / 连接断了）。判据全文见本节头部那一段。
        ///
        /// <para>🆕 🔴 **2026-10-19（W · 联机线「结算段掉线口径」）：这一局已经结算 ⇒ 静默。**
        /// 原版（全量反编译，逐跳）：</para>
        /// <list type="bullet">
        /// <item>`BattleManager__DisconnectedDuringBattle.c:35-51`：`battleStage == PostBattle(0x3C=60)`
        ///   且 **对手**那一端掉线（`param_2 == 0`）⇒ **直接 return**（不弹窗、不停表、不改结果）。</item>
        /// <item>`Everguild.BattleErrorUIManager__ConnectionStatusChanged.c:31-54`：stage == 60 时
        ///   **只 `CustomDebug.LogWarning` 然后 return** ⇒ 结算段**任何**连接状态变化都不进弹窗分支。</item>
        /// </list>
        /// <para>我们原来在这里**无条件** `SayPeerGone(ReconnectHint(30)…)` ⇒ 结算后爆一句
        /// 「30 秒后判他弃权」，而 `StartReconnectCountdown` 遇 `Ctx.IsOver` 根本不起倒计时
        /// ⇒ **那句话是假的**（说错话 = 另一种静默）。现在按原版改成静默。</para>
        ///
        /// <para>⚠️ **本机掉线那一半（原版 `:52-64`）如实留缺、不猜**：原版对本机掉线会
        /// `ShowDisconnectionPopup(true)` + `shouldLoseIfDisconnectEnd(+0x2D8) = true`，重连失败后
        /// `FailedToReconnectAfterDisconnect.c:26-27` 的 `DeadHero(我, 3)` 由 `DeadHero.c:109-121`
        /// 那道战后闸**放行重判**（`RuleCore.Forfeit` 现在没有同款的放行门 —— 它见 `Winner != 0` 一律挡，
        /// `RuleCore.cs:9478-9481` 已如实记着这一条）。而且我们**分不清「谁掉线」**（见
        /// `PeerLossCauseNote` 与 `ReconnectCountdownExpired` 里 `iAmDisconnectedEnd` 那一段）。
        /// ⇒ 结算段一律按原版【对手】那一档处理：**静默、不改结果**。
        /// 欠的两件（分谁掉 / 战后重判入口）记在交件报告，⛔ 不为对称造一个走不到的后门。</para></summary>
        void HandlePeerLost()
        {
            // 🔴 结算已出 ⇒ 原版这一档 = 静默（判据见上面 summary）。
            if (_d != null && _d.Ctx != null && _d.Ctx.IsOver)
            {
                Debug.Log("[Net] 对手掉线，但这一局**已经结算完了**（`Ctx.IsOver`）⇒ 按原版**静默**："
                        + "不报信、不弹窗、不停表、不起倒计时、不判负"
                        + "（判据 = `BattleManager__DisconnectedDuringBattle.c:35-51` 在 PostBattle(60) 对对手直接 return；"
                        + "`Everguild.BattleErrorUIManager__ConnectionStatusChanged.c:31-54` 在 stage 60 只 LogWarning）"
                        + PeerLossCauseNote());
                return;
            }
            _peerGone = true;
            _peerLossWhy = _s != null ? _s.LastError : null; if (DeferForPeerLastActions()) return;   // 掉线**这一刻**的原因（重连退避会覆盖它，先抓住）；后半句 = 原版 `DisconnectedDuringBattle.c:74-89`
            bool hasCtx = _d != null && _d.Ctx != null;
            StartReconnectCountdown();          // 🔴 A900：先起倒计时（第一拍的数字要进下面那两句文案）
            ClockPaused = true;                 // 🔴 A901：对局时钟停走（原版 `ClockManager.PauseClock()`）
            // 🔴 2026-10-19（W · 结算掉线）：**没有对局状态**时不许再说「N 秒后判他弃权」——
            //   那一支**没有倒计时**（`StartReconnectCountdown` 只报信），说秒数也是一句假话。
            SayPeerGone(hasCtx ? ReconnectHint(IAmDisconnectedEnd, _countdownLeft)
                               : (IAmDisconnectedEnd ? "本机掉线了 —— 正在重连" : "对手掉线了 —— 正在等他回来"),
                        hasCtx ? ReconnectPopup(IAmDisconnectedEnd, _countdownLeft)
                               : (IAmDisconnectedEnd
                                  ? "本机掉线了 —— 正在重连。\n（这一档只报信：本机没有对局状态，没有结果可判。）"
                                  : "对手掉线了 —— 正在等他回来。\n（这一档只报信：本机没有对局状态，没有弃权可判。）"));
        }

        /// <summary>🆕 2026-10-19（W · 结算掉线）：**掉线那一刻**的传输层原因（`_s.LastError`）。
        /// 为什么要单独存一份：客机那条重连退避会 `_t.Connect(...)`，而 `Connect` 会**清空** `_lastError`
        /// ⇒ 30 秒后到点那一刻再读 `_s.LastError`，读到的可能是**最近一次重连失败**、不是「为什么掉线」。
        /// ⚠️ 只用于日志（见 `PeerLossCauseNote`）；⛔ 不参与判负。</summary>
        string _peerLossWhy;

        /// <summary>🆕 **2026-10-19（W · 结算掉线）**：把 `A1267` 那条「传输层掉线：原因 = …」拼进**日志**。
        /// 🔴 **它判不出「谁掉线」** —— 原因描述的是**本机这条 socket 怎么坏的**：
        ///   · `Settings/Online/St/PeerClosed`（「对面关掉了连接」）= 本机读到 EOF ⇒ **看起来**是对面先关的；
        ///   · `St/SilentTimeout`（「N 秒没收到对面的任何消息」）/ `St/ReadAbort` / `St/SendFailed`：
        ///     **本机断网与对面断网在 TCP 上表现一模一样**（都没包、或都读到中断）⇒ **判不了**。
        /// ⚠️ **连 `PeerClosed` 都不能当可靠判据**：本机自己 `ClosePeer()` 拆连接时，本机那个读线程也会以
        /// 同族原因失败并写进 `_lastError`（`NetTransport.Fail` 里 `_closing` 只**压日志**，**不压** `_lastError` /
        /// `_peerLost`）⇒ 「本机主动拆」与「对面关掉」在这一格上**同值**。
        /// ⛔ 因此它**只进日志、不参与判负**（拿它判 = 猜）。原版也不把原因拼进界面
        /// （`NetTransport.Fail` 那条注：玩家侧只有固定词条）。</summary>
        string PeerLossCauseNote(string why = null)
        {
            if (why == null) why = _s != null ? _s.LastError : null;
            if (string.IsNullOrEmpty(why)) return "";
            // ⚠️ `Loc.T` 跟着语档走；`PeerClosed` 那条**不带参数**，所以能直接比相等
            //    （`SilentTimeout` 那条带 `{0}`，格式化后当然不等）。
            bool peerClosed = why == Loc.T("Settings/Online/St/PeerClosed");
            return $"（传输层原因 = 「{why}」—— "
                 + (peerClosed
                    ? "**看起来**是对面先关的连接；⚠️ 本机 `ClosePeer` 拆连接时也可能是同一族原因 ⇒ **不据此判负**"
                    : "**判不出是谁掉线**（本机断网 / 对面断网在 TCP 上同形）")
                 + "；A1267）";
        }

        /// <summary>对面**主动离开了这一局**（收到 `bye`）。判据全文见本节头部那一段。
        ///
        /// <para>🆕 **2026-10-17（B27·A912）：按「有没有说再见」分档判弃权 —— 这一档直接判他弃权。**
        /// 🔴 **口径（主对话裁定，照做）**：对局中对手消失有**两路**，两路都要有人收尾 ——</para>
        /// <list type="bullet">
        /// <item>**A 路（对面点了投降/退出那颗钮）**：`Battle/SettingsPanel.cs` 的 `HitResign` →
        ///   `BattleDriver.Forfeit()` → `NetBattle.OnLocalResign()` 先发 `NetKind.Resign`
        ///   ⇒ 本机收到就走 `case NetKind.Resign` → `NetRemoteResign()` ⇒ **立刻判他负**。
        ///   原版同形：`BattleManager__ClickExitBattle.c:44-49` 先 `BattleCommsManager.SendForfeit()`
        ///   （`PublishRPC("PUNReceiveEnemyForfeit")`）再 `AddResignAction` + `DeadHero(我,2)`，
        ///   而全仓 `ClickExitBattle` **只有这一个调用点** ⇒ 原版对局中也没有第二颗「退出」钮。
        ///   ⇒ 这一路**现在就是对的、本件一个字没动**。</item>
        /// <item>**B 路（没点投降就没了：关应用 / 场景卸载 / 主机重开）**：不先发 `resign`，只走
        ///   `LeaveNetRoom()` → `NetSession.Close(say:true,…)` → `Send(NetKind.Bye)`。
        ///   原来本机收到它**只看到「联机对局结束」、谁也没赢、一直挂着**（B13 报告 §④ 第 2 条）。
        ///   ⇒ 本件补的就是这一档。</item>
        /// </list>
        ///
        /// <para>🔴 **判据（就一格）**：对面侧 **`Ctx.IsOver == false`** = 「**他没投降就没了**」那一档。
        /// 投降那一路本机已经 `RuleCore.Forfeit` 过 ⇒ `IsOver == true` ⇒ 这一格自然放过，
        /// **不需要再加一个标志位**（多一个标志位 = 第二份判据，迟早两边不一致）。</para>
        ///
        /// <para>⚠️ **这是【我们自己的口径】，不是复刻**（铁律 3 —— 如实标出来）：原版那两条（掉线 /
        /// 离开房间）走的是**同一个** Photon 回调 `OnPhotonPlayerDisconnected` ⇒ 原版**没有**这个分档
        /// （`PlayerDataManager__QuitApplication.c:23-35` 走 `LeaveRoom()`、**不发** forfeit，
        /// 对面那一侧也只是 `SetOpponentDisconnected` ⇒ 弹窗 + `PauseClock` + 30 秒 ⇒ `DeadHero(对面,3)`）。
        /// 而 **`bye` 这条协议消息本来就是我们设计的**（原版没有它）⇒ 分档只能由我们定。</para>
        ///
        /// <para>⛔ **别把 `bye` 接到 `StartReconnectCountdown()` 上**（B23 警告过的坑，理由在此）：
        /// `ReconnectCountdownExpired` 头上那道「会话 `Closed`/`Off` ⇒ 不判他弃权」的闸是从原版
        /// `…_d__322__MoveNext.c:146-161`（状态不是 70 就什么都不做）抄来的，而 **`bye` 必定**
        /// 把会话置成 `Closed` ⇒ 接上去的净效果是「**数了 30 秒，然后什么都不做**」。
        /// ⇒ 分档点就落在**这里**（收到 `bye` 那一刻），而不是落在倒计时到点那一跳。</para>
        ///
        /// <para>⚠️ **撤窗那一句的闸**：`wasWaiting` = 「本机先报过掉线、台面上还挂着『等他回来』那扇窗」
        /// （客机重连被拒那一档会先 `OnPeerLost` 再收到 `bye`）⇒ 那时才去撤窗。
        /// 与 `NetMatchmaking._peerGonePopup` 是同一条纪律：**没弹过就不许撤窗** —— 否则撤掉的是
        /// 玩家这一刻**自己开着**的那扇。
        /// ⚠️ **那一档会弹第二条**（前一条是「对手掉线了…正在等他回来」，这一条是「他不可能回来了
        /// ⇒ 判他弃权」）—— 两句话说的是**两个不同的状态**，不是「同一件事弹两次」
        /// （同一件事弹两次 = 本机与大厅那一半各弹一次，那条由 `LobbyHandled` 那道闸挡着）。</para></summary>
        void HandlePeerClosed(string why)
        {
            bool wasWaiting = _peerGone || _countdownOn;
            _peerGone = false;      // 对面把台关了，不可能再「回来」（`NetSession` 收到 `bye` 就 `Close()` 了）
            // 那一刻若台面上还挂着「等他回来」那扇窗 ⇒ 先收掉（下面要换一句话说）。
            // ⚠️ 顺带把闸与倒计时收干净 —— 不收的话，那条倒计时会继续跑、**每秒把弹窗正文改回**
            //    「对手掉线了…（N 秒后判他弃权）」，把我们刚说的话顶掉（说错话 = 另一种静默）。
            CancelReconnectCountdown(hideWindow: wasWaiting);

            string body = string.IsNullOrEmpty(why) ? "对面离开了这一局" : why;
            bool forfeited = ForfeitPeerIfLeftMidGame();
            // ⚠️ 这三句是**给玩家看的**（提示行 + 弹窗）⇒ 不写 `**` 那种标记（`WindowsManager.ShowPopUp`
            //    不做 markdown 转换，星号会原样印出来）。
            SayPeerGone("联机对局结束：" + body + (forfeited ? " —— 判他弃权，这一局你赢了" : ""),
                        "联机对局结束：" + body + "\n"
                      + (forfeited
                          ? "（对面已经退出了这一局的联机房间，不可能再回来 ⇒ 这一局判他弃权，你赢了。）"
                          : "（对面已经退出了这一局的联机房间，这一局没法再继续。）"));
        }

        /// <summary>🆕 2026-10-17（B27·A912）：对面**没说再见就没了**（收到 `bye`）⇒ **直接判他弃权**。
        /// 判据与口径全文 → <see cref="HandlePeerClosed"/> 头上那一段。
        /// <para>返回真 = 真的判了这次弃权（自检据此断言；⛔ 别拿它去驱动别的状态 —— 它只是「这一跳做没做」的回执）。</para>
        /// <para>🔴 判负入口复用现成的 <see cref="INetBattleHost.NetRemoteResign"/>（B17 那两条也用它）
        /// ⇒ **不新造口**（`BattleDriver` 那一份 = `RecRaw(RecKindForfeit)`【= 原版 `AddResignAction`】
        /// + `RuleCore.Forfeit(Ctx, 1-_me)` + 刷新表现层）。</para></summary>
        bool ForfeitPeerIfLeftMidGame()
        {
            if (_d == null || _d.Ctx == null)
            {
                // 没有对局 ⇒ 没有「到点判弃权」可言。**出声**（⛔ 不静默，同 `StartReconnectCountdown` 那两支）。
                Debug.LogWarning("[Net] 对面离开了这一局，但本机**没有对局状态**（`Ctx == null`）"
                               + "⇒ 没有弃权可判，只报信（这一档在自检/重连被拒那类没有 `Ctx` 的宿主里会走到）");
                return false;
            }
            if (_d.Ctx.IsOver)
            {
                Debug.Log("[Net] 对面离开了这一局，但这一局**已经打完了**（`Ctx.IsOver`）"
                        + "⇒ 不再判一次弃权（投降那一路早就判过了 —— 同一个判据管两档）");
                return false;
            }
            Debug.Log("[Net] 对面**没点投降就没了**（收到 `bye`，本局 `Ctx.IsOver == false`）⇒ **直接判他弃权**"
                    + "（原版 `ClickExitBattle` 那条路会先 `SendForfeit`、`QuitApplication` 那条**不发** ⇒"
                    + " 「对面不会回来了」这一档由我们判；落地 = `NetRemoteResign()` → `RuleCore.Forfeit(Ctx, 1-_me)`）"
                    + " —— 🆕 理由码取 **`Disconnect`(3)**：原版「对面不会回来了」本来就走掉线那条链"
                    + "（`NetworkCustomManager.OnPhotonPlayerDisconnected` → … → `DeadHero(对面, 3)`），"
                    + "⛔ 不是 `Forfeit`(2)（那要对面**真的点了**投降 —— 那一条由 `NetKind.Resign` 走）");
            _d.NetRemoteResign(BattleResult.Disconnect);
            return true;
        }

        /// <summary>**出声**那一处（三个口一起说：日志 + 提示行 + 弹窗）。
        /// 🔴 三个都要：日志给自检和下一个会话看；提示行是原位反馈但**会被后面的消息顶掉**
        /// （同 `Abort` 那条的注）；**弹窗才是「玩家一定看得见」的那个口** —— 原版那一刻弹的也是窗。</summary>
        void SayPeerGone(string hint, string popup)
        {
            Debug.Log("[Net] " + hint);
            if (_d != null) _d.NetSay(hint);
            NetRuntime.Notice(popup);
        }

        // ==================================================================
        //  🆕 2026-10-17（B17·A900 / A901）：**掉线之后那两半** —— 时钟停走 + 倒计时判弃权
        // ==================================================================
        //  B13（`资料/普查产出_1017/W_B13_联机静默.md` §⑤ 第 1、2 条）把这条链**查清了但没做**，
        //  这里补的是那两半。原版判据（全量反编译 `d:/2/tools/decomp_full/`，逐跳）：
        //
        //  **A901 · 对局时钟停走**
        //   · 对手掉线那一刻：`BattleManager__ShowDisconnectionPopup.c:27` ⇒ **`ClockManager.PauseClock()`**
        //     （那一刻把 `ClockManager+0x85` 置 1；`ClockManager__Update.c:36` 与
        //      `ClockManager__ClockRunning.c:5` **都拿它当闸** ⇒ 表不走）。
        //   · 对手回来那一刻：`BattleManager__SuccessfulReconnection.c:51` ⇒ **`ClockManager.UnpauseClock()`**
        //     （那条里还跟着 `:19` `StopCoroutine(AttemptReconnect)`、`:27` `StopCoroutine(ForfeitDisconnectedEnemy)`，
        //      就是下面 A900 的「取消倒计时」）。
        //
        //  **A900 · 倒计时判弃权**
        //   · `BattleManager._ForfeitDisconnectedEnemy_d__322__MoveNext.c:61`
        //     ⇒ **`secLeft = Math.Max(15, globalVars.maxSecToReconnect + enemyDisconnects × (-3))`**；
        //       `:63` 算完就 `enemyDisconnects++`（**先取值再自增** ⇒ 第一次掉线用的是 30）。
        //     ⚠️ 那两个字段**都读出来了**，不是猜的：
        //       · `maxSecToReconnect` = `VarsGlobal` 的 `+0x30`（`dump.cs:120083`）—— **值 = 30**
        //         （`资料/VarsGlobal_原版数值.md` 的整表第 29 行；读法见该文件头部：`sharedassets0.assets`
        //          没有 type tree，字段名从 `Warpforge_code/…/VarsGlobal.cs` 的声明顺序取）。
        //       · `enemyDisconnects` = `BattleManager` 的 `+0x45C`（`ObscuredInt`，`dump.cs:30882`）——
        //         本局**对手掉线过几次**，从 0 起。
        //       ⇒ **这一局第一次掉线 = 30 秒**，之后每掉一次少 3 秒，地板 15 秒。
        //   · `:99-117` 每拍：`UpdateReconnectStatus(secLeft)`（`BattleManager+0x1B0`，
        //     `public Action<int>`）→ 接的是 `BattleErrorUIManager__UpdateReconnectWindow.c`
        //     （它拿 `TimeSpan.FromSeconds(sec)` 拼进**弹窗正文**，弹窗还开着就改字、不在就 `ShowPopUp`）
        //     + 等 **1.0 秒**（`:120` `WaitForSecondsRealtime`，常量 `0x1834b2bb8` 从 DLL 里解出来 = **1.0f**
        //       ⇒ **真实流逝时间，不受 `timeScale` 影响**）。
        //     ⚠️ 那一拍数到的数是 **30 → 1**（不是 29 → 0）：第一句在进协程那一帧就报出去了，
        //       减 1 只发生在 `WaitForSecondsRealtime` 之后（`:88`）⇒ 整段恰好 **30 秒整**。
        //   · `:118-145` 到点：状态置 **3 = `Disconnected`**（⇒ `…ConnectionStatusChanged.c:41-43` 的
        //     0/3/4/5 那一支 ⇒ **`CloseAllWindows()` = 把提示窗收掉**，⚠️ 是**关窗不是开窗**）
        //     → `LeaveBattleRoom(false)` + `AddResignAction` + **`DeadHero(对面, 3)` = 判对面弃权**。
        //     ⚠️ `:146-161` 那一刻状态若已不是 70（waitingOpponentReconnection）⇒
        //       `LogError("Avoid leaving battle because connection status is …")` **并且什么都不做**
        //       —— 我们这一道对应「会话已经关了 ⇒ 不判」，见 `ReconnectCountdownExpired`。
        //
        //  🔴 **我们这边的对应物**（一个都不新造）：
        //   · 时钟闸 = `BattleDriver.NetClockPaused`（它读本类的 `ClockPaused`）——
        //     单机局 `_net == null` ⇒ 判据恒 false ⇒ **单机路径一个字节都没变**。
        //   · 判负入口 = **`INetBattleHost.NetRemoteResign(reason)`**（现成的，`BattleDriver` 那一份 =
        //     `RecRaw(RecKindForfeit)`【= 原版 `AddResignAction`】+ `RuleCore.Forfeit(Ctx, 1-_me, reason)`）。
        //     ✅ **2026-10-18（A913）订正**：这里原来写「我们引擎的 `RuleCore.Forfeit` **没有「理由码」
        //     这一个参数**，两档走同一个口（这是差异，如实记在报告里）」—— **那条差异已经消掉**：
        //     `RuleCore.Forfeit` 现在有第三个形参 `BattleResult reason`，本文件三处逐档传码
        //     （倒计时到点的两半 = `Disconnect`(3)，原版那一跳就是 `DeadHero(对面,3)` / `DeadHero(我,3)`；
        //      `case NetKind.Resign` = `Forfeit`(2)）。
        //   · 撤窗 = `NetRuntime.HideNoticePopup()`（`WindowsManager.HidePopUp(false)`）——
        //     ⛔ 我们**不调 `CloseAllWindows`**（理由见 B13 报告 §②：它会把玩家这一刻自己开的窗一起关掉）。
        //   · 每秒那一拍 = 提示行（`NetSay`）+ 弹窗正文（`NetRuntime.UpdateReconnectPopup`）。
        //     ⚠️ 弹窗正文那条**只有真 Play 验得到**（批处理没有窗口系统）。

        /// <summary>原版 `VarsGlobal.maxSecToReconnect` = **30**（`资料/VarsGlobal_原版数值.md` 第 29 行；
        /// 字段偏移 `+0x30`，`dump.cs:120083`）。</summary>
        public const int MaxSecToReconnect = 30;
        /// <summary>原版那个 `Math.Max(15, …)` 的**地板**：掉线次数再多，最少也给 15 秒。</summary>
        public const int ReconnectSecondsFloor = 15;
        /// <summary>原版每多掉一次就减掉的秒数（`enemyDisconnects * -3`）。</summary>
        public const int ReconnectSecondsStep = 3;

        /// <summary>原版那一句 **`Math.Max(15, maxSecToReconnect − 3 × enemyDisconnects)`**
        /// （`…_d__322__MoveNext.c:61`）。`enemyDisconnects` = 本局**在此之前**对手掉线过几次（从 0 起）。</summary>
        public static int ReconnectSecondsFor(int enemyDisconnects)
        {
            if (enemyDisconnects < 0) enemyDisconnects = 0;
            return System.Math.Max(ReconnectSecondsFloor,
                                   MaxSecToReconnect - ReconnectSecondsStep * enemyDisconnects);
        }

        /// <summary>本局对手掉线过几次（原版 `BattleManager.enemyDisconnects`，`+0x45C`）。</summary>
        public int EnemyDisconnects { get { return _enemyDisconnects; } }
        /// <summary>🔴 **A901**：对手掉线期间为真 ⇒ 对局时钟停走（原版 `ClockManager+0x85`）。
        /// 「对手回来」与「本局拆掉联机层」两条路都会把它放回 false。</summary>
        public bool ClockPaused { get; private set; }
        /// <summary>倒计时在不在跑（自检读口）。</summary>
        public bool ReconnectCountdownRunning { get { return _countdownOn; } }
        /// <summary>倒计时上一次报给玩家的秒数（0 = 没在跑）。</summary>
        public int ReconnectCountdownLeft { get { return _countdownLeft; } }

        /// <summary>倒计时读的那个钟。原版那一拍是 `WaitForSecondsRealtime(1.0f)`
        /// （`…_d__322__MoveNext.c:120`，常量 `0x1834b2bb8` 从 `GameAssembly.dll` 解出来 = **1.0f**）
        /// ⇒ **真实流逝时间、不受 `timeScale` 影响**，所以这里用秒表，不用 `Time.deltaTime`。
        /// 🔴 **自检可以把它换掉**（不换就得**真等 30 秒**，那一条会拖垮整条自检）——
        /// `SetClockForTest(f)`，传 `null` 恢复成真实秒表。**生产路径一处都不调它。**</summary>
        public static void SetClockForTest(Func<float> now) { _nowOverride = now; }
        static Func<float> _nowOverride;
        static readonly System.Diagnostics.Stopwatch _wall = System.Diagnostics.Stopwatch.StartNew();
        static float NowSec() { return _nowOverride != null ? _nowOverride() : _wall.ElapsedMilliseconds * 0.001f; }

        int _enemyDisconnects;          // 原版 `BattleManager.enemyDisconnects`（`+0x45C`）
        bool _countdownOn;
        int _countdownLeft;             // 上一次报出去的秒数
        float _countdownNextAt;         // 下一拍该报数的时刻（秒表刻度）

        /// <summary>对手掉线 ⇒ 起倒计时（原版 `ShowDisconnectionPopup(false)` 里那一对
        /// `StopCoroutine` + `StartCoroutine(ForfeitDisconnectedEnemy)` —— **重开**，不是叠加）。
        /// 🔴 **2026-10-19（W · 结算掉线）**：两道闸挪到「算秒数 + 自增 `enemyDisconnects`」**之前**。
        ///   原版那个自增在 `_ForfeitDisconnectedEnemy_d__322__MoveNext.c:63`，而那条协程**只有真去等重连才起**
        ///   ⇒ 已经结算的局**不该**记「对手又掉了一次」（原来那个自增发生在闸之前，是静默的状态污染）。
        ///   ⚠️ 今天 `HandlePeerLost` 已经在 `Ctx.IsOver` 时早退了，这里是**第二道**（防御同族调用点）。</summary>
        void StartReconnectCountdown()
        {
            if (_d == null || _d.Ctx == null)
            {
                // 没有对局 ⇒ 没有「到点判弃权」可言。**出声**（⛔ 不静默）。
                Debug.LogWarning("[Net] 对手掉线，但本机**没有对局状态**（`Ctx == null`）⇒ 只报信、不起倒计时");
                _countdownOn = false;
                return;
            }
            if (_d.Ctx.IsOver)
            {
                Debug.Log("[Net] 对手掉线，但这一局**已经打完了** ⇒ 不起倒计时（没什么可判的）");
                _countdownOn = false;
                return;
            }
            _countdownLeft = ReconnectSecondsFor(_enemyDisconnects);
            _enemyDisconnects++;                       // 原版 `:63`：**算完就自增**（先取值再 ++）
            _countdownOn = true;
            _countdownNextAt = NowSec() + 1f;           // 原版 `:120`：报完数**等 1 秒**再减
            Debug.Log($"[Net] 对手掉线：**{_countdownLeft} 秒**倒计时开始"
                    + $"（原版 `max(15, maxSecToReconnect {MaxSecToReconnect} − {ReconnectSecondsStep} × "
                    + $"{_enemyDisconnects - 1})`；本局对手第 {_enemyDisconnects} 次掉线）"
                    + " ⇒ 到点判他弃权");
        }

        /// <summary>每帧推倒计时（`Tick` 里调）。节奏照原版：**报 N → 等 1 秒 → 报 N−1 → … → 报 1 → 等 1 秒 → 判弃权**
        /// （⇔ 整段恰好 N 秒）。用 `while` 而不是 `if`：自检把假钟一次性推过 30 秒也能一次走到位。</summary>
        void TickReconnectCountdown()
        {
            if (!_countdownOn) { TickNetFallbacks(); return; }   // 没在等重连 = 原版 `waitingWhileDisconnected` 的反面 ⇒ 才数兜底（见 `TickNetFallbacks`）
            while (_countdownOn && NowSec() >= _countdownNextAt)
            {
                _countdownNextAt += 1f;
                _countdownLeft--;
                if (_countdownLeft <= 0) { ReconnectCountdownExpired(); return; }
                ReportReconnectStatus(_countdownLeft);
            }
        }

        /// <summary>原版 `UpdateReconnectStatus(secLeft)` 那一拍（`BattleManager+0x1B0` `Action&lt;int&gt;`）——
        /// 它接的是 `BattleErrorUIManager__UpdateReconnectWindow.c`：**把秒数拼进弹窗正文**。
        /// 我们这边两个口都刷：**提示行**（真 Play 与自检都看得见）+ **弹窗正文**（只有真 Play 验得到）。</summary>
        void ReportReconnectStatus(int sec)
        {
            if (_d != null) _d.NetSay(ReconnectHint(IAmDisconnectedEnd, sec));
            NetRuntime.UpdateReconnectPopup(ReconnectPopup(IAmDisconnectedEnd, sec));
        }

        // ================================================================================
        //  🔴 **2026-10-18（第十二轮 · W6）：本文件那 8 处中文硬串的「原版对照」查清了 —— 全部照旧不动，但记载改写**
        //
        //  背景：本文件这几句是**我们自拟的中文**，落在 HUD 提示行上
        //  （`NetSay` → `BattleDriver.NetSay` → `SetHint`）+ `NetRuntime.Notice`/`UpdateReconnectPopup` 的弹窗正文。
        //  之前的普查只记了「同一块屏，但没给原版对照」。**本批找到了原版那一族的词条键**（载体 = 代码里的
        //  `LocalizedString` 字段，**不是** `Localize.mTerm` —— 所以按 `mTerm` 扫是**无效否定**，
        //  `资料/已知的坑.md` #20 的第二/第三种载体）：
        //    · 类 = `Everguild.BattleErrorUIManager`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/Everguild/BattleErrorUIManager.cs`），
        //      实例在 13 个 `bundle_scenes_scenes_battlearena*` 里（`MonoBehaviour_4062.json` 那一族），
        //      9 个 `LocalizedString` 字段（偏移见 `d:/2/tools/il2cpp_out/dump.cs` 的 `BattleErrorUIManager`）：
        //        `LostConnection`        → **`Battle/HUD/LostConnectionMsg`**
        //        `WaitingForOpponent`    → **`Battle/HUD/WaitOpponentConnectionMsg`**
        //        `WaitForConnection`     → **`Battle/HUD/PleaseWaitConnection`**
        //        `MatchEndedWhileReconnecting` → **`Battle/HUD/MatchAlreadyFinished`**
        //        `EnemySyncError`        → **`Battle/EnemySyncError`**
        //        `SubmitButton` → `MainMenu/General/OK` · `SaveMatchError`/`SaveMatchFail` → `CustomErrors/ErrorSavingMatch`
        //        · `GameBlockedDuplicateConnection` → `CustomErrors/DuplicateConnection`
        //    · **重连弹窗正文的拼法有确凿方法体**：`BattleErrorUIManager.UpdateReconnectWindow(sec)`
        //      （`d:/2/tools/decomp_full/Everguild.BattleErrorUIManager__UpdateReconnectWindow.c:27-30`）=
        //      `LocalizedString.op_Implicit(isPlayer ? LostConnection : WaitingForOpponent)` **换行** +
        //      `TimeSpan.FromSeconds(sec).ToString()`；
        //      而 `isPlayer`（字段 `+0x108`）由 `ShowDisconnectionPopUp(bool localPlayerDisconnected)` 那一支决定
        //      （`ConnectionStatusChanged`：状态 1 ⇒ 本机断线、状态 2 ⇒ 对面断线）。
        //
        //  ⛔ **本批【没有】把这几句接上 `Loc.T`，如实写出两条理由**（不是「影响小所以不做」）：
        //    ① **两个语档的文案本地【都】取不到** —— 那 9 条是 `LocalizedString` 字段，**值**在远端 I2 表里
        //       （同 `Shell/AllianceMemberOptionsPopup` 那三颗西语占位的处境，但更彻底：连出厂占位串都没有）。
        //       接上去 = **中英两列全是我们自己编的**，比现在「只编中文」更糟（会让人以为英文也是原版）。
        //    ② **那一格不是我们这一格**：原版那几条是**通用短句**、落在 `WindowsManager.ShowPopUp(...)`
        //       的**弹窗**里（还带一颗 `SubmitButton` = `MainMenu/General/OK`）；
        //       而我们这几句是**带运行期参数的整句**（秒数 / 掉线原因）、落点是 **HUD 提示行**。
        //       硬套会把「对局已暂停、对面回来就能接着打」这类信息丢掉。
        //    ⇒ 已登记成待办（拿到远端 I2 表 / 或决定自拟两列时再做），⛔ 别当成「原版没有」。
        //
        //  🆕 **2026-10-18（第十三轮 · G2b）逐处点清：本文件「面向玩家」的中文硬串在这里**
        //    （下面那批**仍然不接** `Loc.T`，理由就是上面那两条，⛔ 已由主对话裁定接受）。
        //    ⚠️ 与上面那 8 处的「计数」口径无关：下面是**按落点**数出来的（同一处可能横跨两行），
        //       而且**只列面向玩家的**（`Debug.Log*` 那些中文诊断串不在内，它们不上屏）。
        //    🔴 **2026-10-19（P6d）就地订正（铁律 5）**：原来这一节的标题句写的是
        //       「本文件『面向玩家』的中文硬串**【全部】**在这里，**一条都不接** `Loc.T`」——
        //       **`Wire` 那两条已经不成立**（见下面末两行的订正）；本文件其余各处照旧不接。
        //    ⚠️ 下面的行号是 **2026-10-18 的读数**，**已经漂了**（本轮现读就对不上）⇒
        //       把它们当**定位线索**用，判据一律按**符号名**去认（⛔ 别再往下写死新行号）：
        //      · `HandlePeerLost` 一带   `_d.NetSay("联机对局中止：" + why)` —— **HUD 提示行**，`why` 是运行期串
        //      · `HandlePeerLost` 一带   `NetRuntime.Notice("联机对局中止：" + why + "\n…")` —— **弹窗正文**（整段）
        //      · `HandlePeerClosed`      `"对面离开了这一局"` —— 只是下一行那句的**主语片段**（`body` 的兜底值）
        //      · `HandlePeerClosed`      `SayPeerGone(...)` 的**提示行那句 + 弹窗那句**（各带 `body`/`forfeited`）
        //      · `ReconnectHint(sec)` —— 提示行，**带秒数**
        //      · `ReconnectPopup(sec)` —— 弹窗正文，**带秒数**
        //      · `ReconnectCountdownExpired` 里那两句 `NetSay($"重连没成功（超过 {sec} 秒）—— 这一局判你负")` /
        //        `NetSay($"对手掉线超过 {sec} 秒还没回来 —— 判他弃权，这一局你赢了")` —— 提示行，**带秒数**
        //      · 「对手回来了 —— 联机已恢复（…）」/「…接着打」 —— 提示行（两档措辞）
        //    🆕 **2026-10-19（P6d · A1038）改口的两条**（它们是**走线串**，不在上面那批里）：
        //      · `ReconnectCountdownExpired` 的主机那一支：`_s.Close(true, …)`
        //        —— 原来传的是中文整句「这一局的联机房间已经散了」，现在传**词条键**
        //        `Settings/Online/Wire/RoomGone`（`Close` 的 `reason` 已改成「词条键」语义，见那个方法的 doc）。
        //      · `Dispatch` 的 `case NetKind.Action`：`MsgReject.reason`
        //        —— 原来发的是「不是你的回合」，现在发 `NetWireText.Pack("Settings/Online/Wire/NotYourTurn")`。
        //      🔴 **上面那两条走线串「仍然不接」的判据已经由主对话裁掉了**（2026-10-19，`A1038`/P6d）：
        //        🔴 **2026-10-19（`A1039`）就地订正**：本行原来写的是**本文件的旧行号 `:952`**，
        //        现读早已落空 ⇒ 按**上面两颗符号**认（`ReconnectCountdownExpired` / `Dispatch` 的 `case NetKind.Action`），
        //        ⛔ 别再往这一段写死新行号（本段其余处已全部改指符号名）。
        //        判据 ①（「wire 载荷 ⇒ 接 `Loc.T` 会把发送方语言灌到接收方」）**正是本件要解决的问题**，
        //        解法**不是**「不接」，而是**线上只发语言无关的词条键、收侧按自己的语言取词**
        //        （`NetProtocol.NetWireText`）⇒ 这一条**不再是不接的理由**，反而变成了必须接的理由。
        //        判据 ②（「原版 `Battle/Tips/NotYourTurn` 是 HUD 提示行、不是回给对面的拒绝码」）**照旧成立**
        //        ⇒ 用的是 `Wire/*` 那一族的**自拟**键（原版两张表都搜过、0 命中），**⛔ 没有复用**
        //        `Battle/Tips/NotYourTurn`（⛔ 两条键 ZH 同值但语义不同 —— `Core/Loc.cs` 已声明两族并存）。
        //      ⚠️ **同一支里还有一条 `MsgReject.reason`**（引擎拒动作那条，值是 `RuleCodes.Describe(code)`）：
        //        那是**中文整句、不是键**，本轮**不动**（`RuleCodes.cs` 逐字写着「改成键」是 `A985⑧` 第 ③ 步）；
        //        收侧 `NetWireText.Unpack` 对它 `HasEntry` 为假 ⇒ **原样回显**，行为与接线前逐字相同。
        // ================================================================================

        /// <summary>🆕 **2026-10-26（批次 6 · G15）**：**本机是不是「断线那一端」** —— 全类**只此一处判**。
        ///
        /// <para>判据（自拟，理由见 `ReconnectCountdownExpired` 里那一段长注释）：**我们这套里只有客机**有主动重连
        /// 那条路（`NetSession.Pump` 的「客机：重连退避」支）⇒ 本机是客机 = 断线那一端；主机 = 观察对面没了的那一端。
        /// 原版两端各有自己的 `AttemptReconnect` ⇒ **原版这一维有「本机/对面」两个来源**，我们没有第二个。</para>
        /// <para>它驱动三处**必须分档**的东西：① 提示行 / 弹窗的文案（本机掉线说「你」，对面掉线说「他」）
        /// ② 倒计时到点判谁负（`ReconnectCountdownExpired`）③ 原版 `BattleErrorUIManager.ShowDisconnectionPopUp(bool
        /// localPlayerDisconnected)` 的 `isPlayer` 字段（本机断线 ⇒ `LostConnection` + `PleaseWaitConnection` 两行；
        /// 对面断线 ⇒ `WaitOpponentConnectionMsg` 一行，判据 = `decomp_full/Everguild.BattleErrorUIManager__ShowDisconnectionPopUp.c:30-60`）。</para></summary>
        public bool IAmDisconnectedEnd { get { return _s != null && _s.Role == NetRole.Client; } }

        /// <summary>`mm:ss`（原版 `BattleErrorUIManager__UpdateReconnectWindow.c:27-30` 用
        /// `TimeSpan.FromSeconds(sec).ToString("mm\\:ss")` 拼进弹窗正文）。</summary>
        static string ReconnectClock(int sec)
        {
            if (sec < 0) sec = 0;
            return (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00");
        }

        /// <summary>倒计时那一句**提示行**（HUD 上那一行）。🆕 **2026-10-26（批次 6 · G15）**：按
        /// <paramref name="iAmDisconnectedEnd"/> 分两档 —— 这是 #8/#12 那个缺口的正主：
        /// 原来**无条件**说「对手掉线了」，本机自己是断线那一端时那句话是**反的**（说错话 = 另一种静默）。
        /// ⚠️ 提示行是**我们自设的通道**（原版没有 HUD 提示行），所以本机那一档的措辞是**我们写的**，
        /// 但**语义与原版 `LostConnection` 一致**（本机掉线、正在重连）；弹窗那一档用的是**原版词条**（见 <see cref="ReconnectPopup"/>）。</summary>
        static string ReconnectHint(bool iAmDisconnectedEnd, int sec)
        {
            return iAmDisconnectedEnd
                ? $"本机掉线了 —— 正在重连（{sec} 秒之内连不回来，就判你负）"
                : $"对手掉线了 —— 正在等他回来（{sec} 秒后判他弃权）";
        }

        /// <summary>倒计时那一句**弹窗正文**。🆕 **2026-10-26（批次 6 · G15）**：改走**原版词条**
        /// （`Core/Loc.cs` 里那 4 条 `Battle/HUD/*`，值是从官方表录入的），两档的拼法逐句照原版：
        /// <list type="bullet">
        /// <item>`ShowDisconnectionPopUp(true)`（本机掉线）= `LostConnection` **换行** `WaitForConnection`
        ///   （`decomp_full/…__ShowDisconnectionPopUp.c:30-58`：`param_2 != 0` 走 `+0x30`，再 `AppendLine(+0x60)`）；</item>
        /// <item>`ShowDisconnectionPopUp(false)`（对面掉线）= `WaitingForOpponent` 一行（同文件 `+0x48` 那一支）；</item>
        /// <item>每秒那一拍 `UpdateReconnectWindow(sec)` 两条都**再换行拼 `mm:ss`**（`…__UpdateReconnectWindow.c:27-30`）。</item>
        /// </list>
        /// ⚠️ **与原版的一处措辞差（如实记）**：原版那两句是「与**服务器**断开」，我们是 P2P（没有服务器）——
        /// 但**用的是原版词条本身**（复刻优先，⛔ 别自己改写成「与对手断开」：那会让两列文案与官方表不一致）。</summary>
        static string ReconnectPopup(bool iAmDisconnectedEnd, int sec)
        {
            string head = iAmDisconnectedEnd
                ? Loc.T("Battle/HUD/LostConnectionMsg") + "\n" + Loc.T("Battle/HUD/PleaseWaitConnection")
                : Loc.T("Battle/HUD/WaitOpponentConnectionMsg");
            return head + "\n" + ReconnectClock(sec);
        }

        /// <summary>到点那一跳（原版 `…_d__322__MoveNext.c:118-145`）。</summary>
        void ReconnectCountdownExpired()
        {
            _countdownOn = false;
            int sec = _countdownLeft;
            _countdownLeft = 0;

            // 🔴 **原版那一道闸**（`:146-161`）：到点那一刻连接状态若已**不是** 70
            //    （waitingOpponentReconnection）⇒ `LogError("Avoid leaving battle because connection status is …")`
            //    并且**什么都不做**。我们这一道 = 会话已经关了/停了 ⇒ **不判弃权**。
            if (_s != null && (_s.State == NetState.Closed || _s.State == NetState.Off))
            {
                Debug.LogWarning("[Net] 对手掉线倒计时到点，但**会话已经关闭** ⇒ 不判他弃权"
                               + "（原版同一道闸：`…_d__322__MoveNext.c:146-161`，状态不是 70 就 "
                               + "`LogError(\"Avoid leaving battle because connection status is …\")`）。"
                               + "⛔ 如实说出来，不假装没事。");
                // 🔴 **2026-10-17 收口时补（`A931`）**：这一支原来**直接 return、没放开时钟闸** ——
                //    而闸是在 `HandlePeerLost`（本文件 `:296`）按下的 ⇒ **那一刻起对局时钟永远停走**
                //    （`BattleDriver.TickClock` 那句 `if (NetClockPaused) return;` 从此恒真）。
                //    证据：**同一函数另三支全都放开了** —— `:571`（`_d == null`）· `:574`（`IsOver`）·
                //    `:582`（判负那支）；`:600` 还有一处。**只此一支漏**，是静默真缺陷（不报错、只是不走）。
                //    ⚠️ 语义上也该放开：倒计时**已经结束**了，这一支只是「不判他弃权」，
                //       = 玩家还在这一局里 ⇒ 时钟必须继续走。
                ClockPaused = false;
                return;
            }
            if (_d == null || _d.Ctx == null) { ClockPaused = false; return; }
            if (_d.Ctx.IsOver)
            {
                ClockPaused = false;
                Debug.Log("[Net] 对手掉线倒计时到点，但这一局**已经打完了** ⇒ 不判弃权");
                return;
            }

            // 撤窗在前（原版那一刻状态置 3 ⇒ `ConnectionStatusChanged` 的 3 那一支 ⇒ `CloseAllWindows()`
            // ⇒ **是关窗不是开窗**），再判负。
            NetRuntime.HideNoticePopup();
            ClockPaused = false;     // 这一局就到此为止，闸先放开（下一局是**另一个** `NetBattle`）

            // 🔴 **判负对象按【两端】分**（2026-10-18 **第四续**：`INetBattleHost.NetSelfResign()` 补上之后，
            //    两端连「判谁负」都不一样了）—— 「本机是哪一端」的判据与理由见下面那一段长注释。
            // 🔴 **2026-10-19（W · 联机线「结算段掉线口径」）：这一格是【角色】判据，不是「谁真掉线」—— 如实标出。**
            //   原版本机掉线那一半：`BattleManager._AttemptReconnect_d__320__MoveNext.c`（本机那条重连退避）
            //   → 失败 ⇒ `FailedToReconnectAfterDisconnect.c:26-27` 的 `AddResignAction(1)` + `DeadHero(我, 3)`。
            //   P2P 里「本机断网」与「对面断网」在本地 socket 上**同形**，`A1267` 的「传输层掉线：原因 = …」
            //   只说明**本机这条 socket 怎么坏的**：`St/PeerClosed` 看起来是对面先关，但本机 `ClosePeer()`
            //   拆连接时也可能是同一族原因（`_closing` 只压日志、不压 `_lastError`）⇒ **没有可靠的「谁掉」判据**；
            //   `St/SilentTimeout` / `St/ReadAbort` / `St/SendFailed` 更是两边通用（见 `PeerLossCauseNote`）。
            //   能用的判据只剩「我们这套里**只有客机**有主动重连那条路」
            //   （`NetSession.Pump` 的「客机：重连退避」支）⇒ 本机是客机 = 断线那一端。
            //   **这是我们自拟的**（原版两端各有 `AttemptReconnect`）。**还缺两件**（⛔ 不猜、也不为它造后门）：
            //     ① 一条应用层「谁在连续存活」的握手（重连成功后互换 last-heard 时间戳，由活着那侧判）；
            //     ② 服务端 / 第三方仲裁。在这两样到手之前，⛔ 别按传输层原因去猜「谁掉线」。
            //   ⚠️ 另外：本格**只在对局中走到**（`HandlePeerLost` 在 `Ctx.IsOver` 时已经静默早退）——
            //     原版【结算段本机掉线 ⇒ 重判本机负】那一档（`DisconnectedDuringBattle.c:52-64` +
            //     `shouldLoseIfDisconnectEnd` 放行 `DeadHero` 战后重入）我们**没有**，理由见 `HandlePeerLost` 的 summary。
            bool iAmDisconnectedEnd = IAmDisconnectedEnd;   // 🆕 2026-10-26（G15）：判据收口到属性，⛔ 别在这里再写一份
            Debug.Log("[Net] 掉线倒计时到点的判负对象：" + (iAmDisconnectedEnd
                        ? "本机是**客机** ⇒ 按「断线那一端」处理（判本机负）"
                        : "本机是**主机** ⇒ 按「对面掉线那一端」处理（判对面弃权）")
                    + " —— 判据 = 角色（我们自拟，原版两端各有 AttemptReconnect）" + PeerLossCauseNote(_peerLossWhy));

            // ==================================================================
            //  🔴 **A914（2026-10-18，按独立审查 `REV_W3_联机.md` R3 改成【分端】）：判完这一局怎么收尾**
            // ==================================================================
            //  🔴 **原版【两端行为不同】，这不是同一跳**（第一版把两端当一回事，是**真偏离**）：
            //
            //  · **【对面掉线那一端】**（观察到对面没了、判**对面**弃权）——
            //    `BattleManager._ForfeitDisconnectedEnemy_d__322__MoveNext.c`：
            //      `:142` **`BattleNetworkManager.LeaveBattleRoom(mgr, false)`**
            //        —— `LeaveBattleRoom.c:41-44`：`param_2 == false` ⇒ 额外调
            //           `NetworkCustomManager.RemoveRoomAfterLeaving()`（`RemoveRoomAfterLeaving.c:26`
            //           = `Room.EmptyRoomTtl = 0` ⇒ 人一走房间就散），`:46-49` 才是 `LeaveRoom()`；
            //      `:143` `AddResignAction` + `:144` `DeadHero(对面, 3)`。
            //    ⇒ **这一端才「离开房间」**（我们的等价物 = `NetSession.Close`，与 `BattleDriver.LeaveNetRoom()`
            //      是同一句）。
            //
            //  · **【本机是断线那一端】**（本机自己掉了、重连也失败）——
            //    `BattleManager__FailedToReconnectAfterDisconnect.c`（**现读全函数**）：
            //      `:14` **`StopCoroutine(AttemptReconnect)`** · `:26-27` `AddResignAction(1)` + `DeadHero(我, 3)`
            //      —— **整个函数里没有 `LeaveBattleRoom`**。
            //    ⇒ **这一端【不】离开房间**（原版没有那一跳，多做就是偏离）；动作 = **停掉重连退避**
            //      （等价物 = `NetSession.StopReconnecting()`，它对应的就是上面那句 `StopCoroutine(AttemptReconnect)`）。
            //    ⚠️ **「本机是哪一端」这个判据是我们自拟的**：我们这套里**只有客机**有主动重连那条路
            //      （`NetSession.Pump` 里「客机：重连退避」那一支的判据含 `Role == NetRole.Client`）
            //      ⇒ 「本机正在重连的那一端」在我们这儿**恒等于客机**（原版两端各有自己的 `AttemptReconnect`）。
            //      ⚠️ 因此「**主机自己掉线**」那一端在我们这儿仍走「离开房间」—— **如实记着**（交件报告 §11）。
            //    ✅ **这一端「判本机负」2026-10-18 第四续已补上**：`INetBattleHost.NetSelfResign()`
            //      （`BattleDriver` 那份 = 把 `NetRemoteResign` 的 `1-_me` 换成 `_me`，**仍走既有记账口**
            //      `RecRaw(RecKindForfeit)`）⇒ 这一端现在判的是**自己**（见下面那一支的注释）。
            //      🔴 **它顺带修掉了一个两端不一致**：原来两端都判「对面」负 ⇒ **各判自己赢**；
            //      现在两端**都说是同一个座位弃的权**（本端 `Forfeit(Ctx, _me)`、对端 `Forfeit(Ctx, 1-对面_me)`
            //      ⇒ 两边 `ForfeitedBy` 恰好相同）—— 断言钉在 `NetBattleTest` §10③-8。
            //
            //  🔴 **不做的后果**（`资料/普查产出_1018/R3_联机三档现核.md` §2 现核出来的、账上没写的那半）：
            //    `NetSession` 停在 `WaitingReconnect`，而它的 `_wasInBattle` 仍是 **true**
            //    ⇒ 客机那条退避分支**每 2 秒真去 `Connect` 一次、永不停止**，连上就把状态字改成
            //    「连上了，正在补上这一局的进度…」—— 玩家刚被告知判负，转头字就变了（说错话 = 另一种静默）。
            //    （原版停那个循环靠的正是 `StopCoroutine(AttemptReconnect)`。）
            //
            //  ⚠️ **位置是硬的：必须排在上面那道「会话已关/已停 ⇒ 不判他弃权」的闸【之后】。**
            //      那道闸同形于原版 `…_d__322:146-161`（状态不是 70 就什么都不做）。把这一段挪到它前面
            //      ⇒ 会话先被置成 `Off` ⇒ 下次到点直接走「不判弃权」那一支（**判负整个失效**，而且静默）。
            //
            //  ⚠️ **语句顺序与原版【相反】，如实标出（独立审查 R6）**：原版是
            //      `:142 LeaveBattleRoom` → `:143 AddResignAction` → `:144 DeadHero`；
            //      我们是**上面那句 `_d.NetRemoteResign()`（判负）在前、这一段收尾在后**。
            //      对端**只看得见 `bye` 这一条**（`MsgBye.reason`）⇒ 这个顺序差**不可观测**，
            //      所以**没改**；只是别再把「照原版顺序」当成事实写进注释。
            //
            //  ⚠️ **不写成「在 `Dispatch` 的 `case NetKind.Resign` 里一并离开房间」**：那会让
            //      `Editor/BattleScene.cs` 里「`bye` 恰好 1 条」那条线路上判别式红（主对话已裁：不归本笔）。
            //
            //  📌 **还差一处（交回主对话裁）**：结算段要不要调 `BattleDriver.LeaveNetRoom()`
            //      （`BattleDriver.LeaveNetRoom()`，除关会话外还做表现层收尾；⚠️ 行号会漂，2026-10-19 现核 `:7952-7974`）—— 那份文件在另一个写手手里，
            //      **本笔一个字节都没碰**。建议与理由 → 交件报告 §7。
            if (iAmDisconnectedEnd)
            {
                // ================= 【本机是断线那一端】：判**本机**负 + 停重连，⛔ 不离开房间 =================
                //   判据 = `BattleManager__FailedToReconnectAfterDisconnect.c:26-27`
                //     （`AddResignAction(param_1, 1)` + `DeadHero(param_1, 1, 3)` —— `isPlayerDying = 1` = **我死**）。
                //   `NetSelfResign()` 走的是**同一套既有记账口**（`BattleDriver` 那份 = `RecRaw(RecKindForfeit)` +
                //   `RuleCore.Forfeit(Ctx, _me)` + 刷新）—— ⛔ **不是**这里直接调 `RuleCore.Forfeit`（那会绕过录像口）。
                if (_d != null && _d.Ctx != null && _d.Ctx.MulliganOpen) { LeaveWithoutResult($"还没开打（换牌阶段）就断线、重连也没成功（{sec} 秒）"); _s.StopReconnecting(); return; }   // 原版 `FailedToReconnectAfterDisconnect.c:54-108`：battleStage==0 ⇒ CancelMatchWithoutResult（**不判结果**）
                Debug.Log($"[Net] 本机掉线超过 {sec} 秒、重连没成功 ⇒ **判本机负**（原版 `FailedToReconnectAfterDisconnect.c:26-27`：`AddResignAction(1)` + `DeadHero(我, 3)`）");
                if (_d != null) _d.NetSay($"重连没成功（超过 {sec} 秒）—— 这一局判你负");
                _d.NetSelfResign(BattleResult.Disconnect);   // 🆕 A913：码 3 = 原版那一跳的第三个实参
                // 停重连 = 原版同一支 `:14` 的 `StopCoroutine(AttemptReconnect)`；
                // ⛔ 这一端**不**离开房间（原版那一支里没有 `LeaveBattleRoom`）。
                _s.StopReconnecting();
            }
            else
            {
                // ================= 【对面掉线那一端】：判**对面**弃权 + 离开房间 =================
                Debug.Log($"[Net] 对手掉线超过 {sec} 秒没回来 ⇒ **判对面弃权**"
                        + "（原版 `…_d__322__MoveNext.c:143-144`：`AddResignAction` + `DeadHero(对面, 3)`；"
                        + "我们的对应物 = `NetRemoteResign()` → `RuleCore.Forfeit(Ctx, 1-_me)`）");
                if (_d != null) _d.NetSay($"对手掉线超过 {sec} 秒还没回来 —— 判他弃权，这一局你赢了");
                _d.NetRemoteResign(BattleResult.Disconnect);   // 🆕 A913：码 3（原版第三个实参）

                if (_s == null)
                {
                    // 🔴 **R4（独立审查）**：这一格原来是 `_s.State != NetState.Off` —— **死条件**
                    //    （上面那道闸对 `Closed/Off` 已经返回过了）。换成真正有意义的那一格（`_s == null`），
                    //    并且**出声**：同族原版那道「已经离开过就不再走」的闸本来就是出声的
                    //    （`BattleManager__LeaveBattle.c:40 CustomDebug.LogWarning`，见 `BattleDriver.cs` 自己的注释）。
                    Debug.LogWarning("[Net] 倒计时到点、判完弃权，但**本类没有挂会话**（`_s == null`）"
                                   + "⇒ 「离开房间」这一跳**走不了**（原版那一刻会走 `LeaveBattleRoom(false)`）"
                                   + " —— 如实说出来，别假装收干净了");
                }
                else
                {
                    // ⚠️ 措辞**中性**（**R7**）：这一句会经 `MsgBye.reason` 送到**对面**，由对端的
                    //    `HandlePeerClosed` → `SayPeerGone("联机对局结束：" + body …)` 显示**在对面的界面上**
                    //    ⇒ 写成「对手掉线判负」在对面读起来是**反的**（那句在描述它自己那个座位）。
                    //    ⛔ 不许写成对面视角的「你掉线了」、也不许写成我们视角的「对手掉线」——
                    //      写成**对双方都成立的陈述**。
                    // 🔴 **2026-10-19（P6d · A1038）**：传的是**词条键**（不是那句中文本体）——
                    //    线上只发键、收侧按**它自己**的语言取词（`NetWireText.Unpack`）；
                    //    本机那半（`SetState(Off, …)` 的状态字）也走同一个键 ⇒ ⛔ 两半都不留裸串。
                    _s.Close(true, "Settings/Online/Wire/RoomGone");
                }
            }
        }

        /// <summary>对手回来了 / 这一局拆了 ⇒ **撤窗 + 恢复时钟 + 取消倒计时**（原版
        /// `BattleManager__SuccessfulReconnection.c:19,27,51` 那三句：`StopCoroutine(AttemptReconnect)` /
        /// `StopCoroutine(ForfeitDisconnectedEnemy)` / `ClockManager.UnpauseClock()`）。
        /// ⚠️ `hideWindow` 分开：`Detach()` 那条路**不撤窗** —— 那一刻台面上那扇窗可能正是下一局要用的
        /// （会话活得比一局久，见 `Detach` 的注）。</summary>
        void CancelReconnectCountdown(bool hideWindow)
        {
            bool wasRunning = _countdownOn;
            _countdownOn = false;
            _countdownLeft = 0;
            _peerLossWhy = null;
            ClockPaused = false;
            // 🆕 **2026-10-26（批次 8 · 执行代理 L）**：**第二句 `StopCoroutine` 的等价物** ——
            //   原版 `BattleManager__StopWaitingOpponentReconnection.c:10-11` 停的是**两处**协程
            //   （`ForfeitDisconnectedEnemy` + `WaitOpponentLastActions`，两个 DAT 与 `ShowDisconnectionPopup`
            //   里那两个对照认出）。第一处 = 上面那几行（倒计时闸）；第二处 = 下面这两行
            //   （收尾等待窗口 `_finishWaitLeft`）—— 原来只做了第一处 ⇒ 对手回来之后
            //   `WaitsForPeerLastActions` 还会挂到 12.5 秒上限。⛔ 别把这两行挪走：
            //   「对手回来了」那一刻原版就是**同时**停这两个窗口的。
            _finishWaitLeft = -1f;
            _finishWaitAcc = 0f;
            if (hideWindow) NetRuntime.HideNoticePopup();
            if (wasRunning)
                Debug.Log("[Net] 对手回来了 ⇒ **倒计时取消 + 对局时钟恢复 + 提示窗收掉**"
                        + "（原版 `SuccessfulReconnection.c:27,51` 的 `StopCoroutine` + `UnpauseClock`）");
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

        /// <summary>🆕 2026-09-30：**非 `AiAction` 的本地动作**（进攻卡 / 防御卡那两条伪动作）。
        /// 语义与 `OnLocalAction` 完全一样（本地**已经落地**、乐观执行）—— 单独一个口子只是因为
        /// 那两条在 `AiAction` 里没有对应物。
        ///
        /// ⚠️ 它们跑在**换牌之后、开打之前**，所以主机那边「不是你的回合」那道守卫要放行
        /// （`NetApply.IsEnvPick`）—— 那时候 `Ctx.Active` 还没轮到任何人。</summary>
        public void OnLocalRawAction(MsgAction m)
        {
            if (_aborted || m == null) return;
            m.actor = MySeat;                                   // 🔴 **绝对座位**（别落在默认的 0 上）
            m.picks = null; m.pickIds = null;
            if (IsHost)
            {
                m.seq = Log.Count;
                m.preApplied = false;                           // 客机那边**没落过**这条 ⇒ 要落地
                Log.Add(m);
                Send(NetKind.Applied, m);
                Debug.Log($"[Net] 主机动作 #{m.seq} 已广播（kind={m.kind}，envSlot={m.envSlot}）");
            }
            else
            {
                Send(NetKind.Action, m);                        // 客机：交给主机定序
                Debug.Log($"[Net] 客机动作已发出，等主机确认（kind={m.kind}，envSlot={m.envSlot}）");
            }
        }

        /// <summary>本地换牌**提交**（联机局里 `Mulligan` 由主机定序 ⇒ 本地先不落地）。</summary>
        public void OnLocalMulligan(int[] marks)
        {
            _myMulligan = marks ?? new int[0]; BeginMulliganFallback();   // 原版 `SendMulliganCards()` 之后起那条兜底（ILSpy `:25865`）
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

        /// <summary>🆕 **2026-10-25（执行代理 `CH`）**：**本机督军**说了 6 颗台词钮里的第 `chatId` 句
        /// ⇒ 告诉对面（原版 `BattleCommsManager.SendChatMessage`：单 int RPC）。
        ///
        /// <para>🔴 **只由「本地玩家开口」那一路调**（宿主的 `SpeakChat` 播完之后）。
        /// ⛔ **收到别人的聊天时不许调它** —— 原版的防回环就是「收到那一档根本不进发送支」
        /// （`DisplayWarlordChatMessage.c:53` 那个 `isLocal != 0` 闸），见 `NetChat` 文件头 ③。</para>
        ///
        /// <para>⚠️ **不改引擎状态**（不消耗 `Ctx.Rng`、不进 `Log`、不影响指纹）⇒ 它**不走**
        /// `OnLocalRawAction` 那条动作流。📌 会话重连时聊天**不补发**（它没有录像语义，
        /// 原版重连也只恢复局面；要看聊天得回放——那一档我们由宿主录 `0x37`）。</para></summary>
        public void OnLocalChat(int chatId)
        {
            if (_aborted) return;
            // 只发「可以转发」那一档：0/1 是开幕台词（原版 `:65` 那两道闸），其余越界值一律不发（出声）。
            // 🔴 **2026-10-27（音频线 `B9` 第五批 · A-06）就地改（铁律 5）**：闸从
            // `IsButtonLine` **收窄成只用 `IsRelayable`**（`chatId != 0 && chatId != 1`）。
            //   判据 = 原版 `DisplayWarlordChatMessage.c:65` 的发送闸**只有那两道**（0/1），
            //   并没有「必须落在 5…10」这一条；而 `IsButtonLine(5…10)` 会把 **`4`（Concede）** 一起挡掉
            //   ⇒ 认输那一句**发不出去**（`BattleDriver` 侧 `OnLocalChat(4)` 已经接了，就等这一句放行）。
            //   ⚠️ **收侧仍然保留 `IsButtonLine`**（见下面 `case NetKind.Chat` 那一支）—— 那一道是
            //   防伪造越界，两侧的闸**本来就不是同一条**，⛔ 别把两边改成一样。
            if (!NetChat.IsRelayable(chatId))
            {
                Debug.LogWarning($"[Net] 本机督军那句 `chatId={chatId}` **没有发给对面**："
                               + "它是 `0`/`1`（开幕台词，原版只在本地演）"
                               + " —— 判据 = 原版 `SendChatMessage` 之前那两道闸（`DisplayWarlordChatMessage.c:65`）");
                return;
            }
            Send(NetKind.Chat, new MsgChat { seat = MySeat, chatId = chatId });
            Debug.Log($"[Net] 本机督军说了第 {chatId} 句，已告诉对面（座位 {MySeat}）");
        }

        // ==================================================================
        //  🆕 2026-10-25（联机域审查 · D 域）：原版三条**纯表现** RPC
        //  —— `CardSeen` / `CardReorder` / `MatchFinished`（判据见 `INetFoePresentationHost` 的注释）
        // ==================================================================
        //
        //  🔴 **兼容性（照 `NetKind.Chat` 那条既定口径）**：三条都是**新增 kind、不改任何既有消息格式**
        //     ⇒ **不 +`NetProtocol.Version`**。旧端收到会走 `Dispatch` 末尾「不认识的联机消息」**出声**并丢掉
        //     （不崩、不影响对局）。⚠️ **只有给已有消息加字段才是「改格式」**，那时才必须 +版本。
        //  ⚠️ 三条都**不进权威流、不改指纹** ⇒ 丢一条不会让两端局面分叉（代价只是少一次表现）。

        /// <summary>告诉对面「我在手里看了这张牌」（原版 `SendCardSeen(uniqueInGameID)`）。
        /// ⚠️ 只在对局中发；没有会话时**出声**（不静默）。</summary>
        public void SendCardSeen(int uniqueInGameId)
        {
            if (!CanSendPresentation("CardSeen", uniqueInGameId >= 0)) return;
            Send(NetKind.CardSeen, new MsgCardSeen { cardId = uniqueInGameId });
        }

        /// <summary>告诉对面「我把手牌挪到了新位置」（原版 `SendCardReorder(uniqueInGameID, newIndex)`）。</summary>
        public void SendCardReorder(int uniqueInGameId, int newIndex)
        {
            if (!CanSendPresentation("CardReorder", uniqueInGameId >= 0 && newIndex >= 0)) return;
            Send(NetKind.CardReorder, new MsgCardReorder { cardId = uniqueInGameId, index = newIndex });
        }

        /// <summary>告诉对面「我这局打完了」（原版 `SendMatchFinished()`，**无载荷**）。
        /// <para>🔴 **它不判胜负**：只用来开收尾窗口（对端 `PeerMatchFinished` ⇒ `TickFinishWait` 那个条件）。
        /// 发完本机可以 `BeginFinishWait()` 开始等对手最后几条动作（见那个方法的注释）。</para></summary>
        public void SendMatchFinished()
        {
            if (!CanSendPresentation("MatchFinished", true)) return;
            Send(NetKind.MatchFinished, new MsgMatchFinished());
            Debug.Log("[Net] 已告诉对面「本机打完了」（`match.finished`，原版 `SendMatchFinished`）");
        }

        bool CanSendPresentation(string what, bool argsOk)
        {
            if (_aborted) return false;
            if (!argsOk) { Debug.LogWarning($"[Net] `{what}` 的参数非法 ⇒ 这一条不发（⛔ 不猜一个值发出去）"); return false; }
            if (_s == null || _s.State != NetState.InBattle)
            {
                // ⚠️ 大厅/已关闭阶段发「战斗内表现消息」= 调用点搞错了 ⇒ 出声（不静默）
                Debug.LogWarning($"[Net] `{what}` 只在对局中发；当前会话状态 = {(_s == null ? "<无会话>" : _s.State.ToString())}"
                               + " ⇒ 这一条丢掉");
                return false;
            }
            return true;
        }

        // ---- 收尾「等对手最后几条动作」的窗口（原版 `BattleManager.WaitOpponentLastActions`）----
        //
        //  判据（**逐句读过**，`d:/2/tools/decomp_full/`）：
        //   · `BattleManager__ShouldWaitForOpponentLastActions.c`：
        //        `return matchFinished || ActionsLeftInQueue() > 0;`   ← 就是这两个条件
        //   · `BattleManager._WaitOpponentLastActions_d__316__MoveNext.c`：
        //        `remaining = 0x41480000 (=12.5f)`；
        //        `while (ShouldWait && remaining > 0) { LogWarning(prefix+remaining); remaining -= 2.0f; yield return <1 秒>; }`
        //        常量 `DAT_1834b2bbc` 已从 `GameAssembly.dll` **直读 = 2.0f**（0x1834b2bbc → `00000040`）
        //   · `BattleManager__ReceiveMatchFinished.c`：对面那条 RPC 就是把 `matchFinished` 置 1。
        //  ⚠️ **没读清**：`ActionsLeftInQueue()` 的口径（本机待播 / 待应用 / 两者）—— 这里用
        //     `Session.Inbox.Count`（**本机还没消费的入站帧**）当代理，并在下面标出来。
        //     ⇒ 注释里写死「这一格是代理量，不是原版那个函数」。

        /// <summary>原版那个常量 `0x41480000` = **12.5f**（秒）。</summary>
        public const float FinishWaitCap = 12.5f;

        /// <summary>🆕 2026-10-25：**自检口** —— 直接置那个标志，不走真 socket。
        /// 与同族的 `SetClockForTest` 一个口径（本仓已有先例）；⛔ 生产代码别调。</summary>
        public void SetPeerMatchFinishedForTest(bool v) { PeerMatchFinished = v; }
        /// <summary>原版 `DAT_1834b2bbc` = **2.0f**（每轮从剩余里扣掉的量；已从 DLL 直读）。</summary>
        public const float FinishWaitStep = 2.0f;

        float _finishWaitLeft = -1f;      // <0 = 没有在等
        float _finishWaitAcc;             // 攒到 1 秒扣一次（原版每轮 `yield return` 约 1 秒）
        /// <summary>对面说过「打完了」没有（原版 `BattleManager.matchFinished`，`+0x2cc`）。</summary>
        public bool PeerMatchFinished { get; private set; }
        /// <summary>现在是不是还在等对手最后几条动作（宿主据此延后结算界面）。</summary>
        public bool WaitsForPeerLastActions { get { return _finishWaitLeft >= 0f; } }

        /// <summary>开一个「等对手最后几条动作」的窗口（原版在收尾前调 `WaitOpponentLastActions`）。</summary>
        public void BeginFinishWait()
        {
            _finishWaitLeft = _peerForfeited ? -1f : FinishWaitCap;   // 对面已投降 ⇒ 不开窗（原版 `ReceiveEnemyForfeit` 第 ① 步就把它停了）
            _finishWaitAcc = 0f;
        }

        /// <summary>宿主每帧推一次；**返回 true = 还要等**（此时不要显示结算）。到条件失效或超时自动结束。
        /// <para>🧨 **反向断言点**（待自检恢复时跑）：把本方法改成恒 `false` ⇒ 对手最后几条动作会被截断。</para></summary>
        public bool TickFinishWait(float dt)
        {
            if (_finishWaitLeft < 0f) return false;
            // ⚠️ 代理量：原版是 `ActionsLeftInQueue()`（没读到它的口径）；我们用「本机还没消费的入站帧数」
            int pendingIn = (_s != null && _s.Inbox != null) ? _s.Inbox.Count : 0;
            bool keepGoing = PeerMatchFinished || pendingIn > 0;
            if (!keepGoing || _finishWaitLeft <= 0f)
            {
                _finishWaitLeft = -1f; OnFinishWaitEnded();   // 原版那条协程的收尾支（`_WaitOpponentLastActions_d__316:47-65`）
                return false;
            }
            _finishWaitAcc += dt;
            if (_finishWaitAcc >= 1f)
            {
                _finishWaitAcc -= 1f;
                _finishWaitLeft -= FinishWaitStep;
                Debug.Log($"[Net] 还在等对手最后几条动作（剩 {_finishWaitLeft:F1}；"
                        + $"peerFinished={PeerMatchFinished} pending={pendingIn}）");
            }
            return true;
        }

        /// <summary>每回合末对一次指纹（**两端算的是同一个状态点**：`EndTurn` + `BeginTurn` 之后）。</summary>
        public void SendFingerprint()
        {
            // 🔴 2026-10-18（同族静默失败，本笔顺手补）：原来是一条**无声的 `return`** ——
            //   没有对局状态时这一回合的指纹对账**悄悄没了**（红线：不许静默失败）。
            if (_d == null || _d.Ctx == null)
            {
                Debug.LogWarning("[Net] 该对指纹了，但本机**没有对局状态**（`Ctx == null`）"
                               + "—— 这一回合的指纹对账落空（两端打岔就发现不了了）");
                return;
            }
            int h = NetProtocol.Fingerprint(_d.Ctx);
            _lastHashTurn = _d.Ctx.Turn; _lastHash = h;
            Send(NetKind.Hash, new MsgHash { turn = _d.Ctx.Turn, hash = h });
        }

        /// <summary>发一条（本类所有出站消息都走这里）。🔴 2026-10-18：`_s == null`（压根没接上会话）
        /// 原来是**无声丢掉** —— 同族静默失败，现在出声。真正的「没连上」那一档由
        /// `NetSession.Send` 自己出声（它返回 `false`）。</summary>
        void Send<T>(string kind, T msg)
        {
            if (_s == null)
            {
                Debug.LogWarning("[Net] 这条 `" + kind + "` **没发出去**：本类**没有挂会话**（`_s == null`）"
                               + " —— 话没传到对面，如实说出来");
                return;
            }
            _s.Send(kind, msg);
        }

        // ==================================================================
        //  收：主线程泵（`BattleDriver.Update` 调；自检里显式调）
        // ==================================================================

        public void Tick()
        {
            if (_s == null) return;

            // 对面**回来了**（`WaitingReconnect` → `InBattle`）⇒ 说一声 + 把 A900/A901 那两半收干净。
            // 判据：原版那一刻连接状态回到 `Connected(0)`，走的是 `BattleErrorUIManager__ConnectionStatusChanged.c:33-37,59-63`
            // 的 **`WindowsManager.CloseAllWindows()`**（= 把「等对手」那扇提示窗收掉）；
            // 同一刻 `BattleManager__SuccessfulReconnection.c:19,27,51` 还停掉了两个协程并 `UnpauseClock()`。
            // 🔴 `CloseAllWindows` 我们**不调**（它会把玩家这一刻自己开的窗一起关掉，而原版那一刻台面上只有那一扇提示窗）
            //    ⇒ 只收**弹窗**那一颗（`NetRuntime.HideNoticePopup` → `WindowsManager.HidePopUp(false)`）。
            // 🔴 **只认边沿**（`_peerGone` 是掉线那一下记下的），否则每一帧都会说一遍。
            if (_peerGone && _s.State == NetState.InBattle)
            {
                _peerGone = false;
                CancelReconnectCountdown(hideWindow: true);   // 撤窗 + 恢复时钟 + 取消倒计时
                bool over = _d != null && _d.Ctx != null && _d.Ctx.IsOver;
                string back = over ? "对手回来了 —— 联机已恢复（这一局已经打完了）"
                                   : "对手回来了 —— 联机已恢复，接着打";
                Debug.Log("[Net] " + back);
                if (_d != null) _d.NetSay(back);
            }

            TickReconnectCountdown();     // 🆕 A900：对手还没回来 ⇒ 往下数（到点判弃权）

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
                    // 🔴 **2026-10-18（账：「`Send()` 静默丢」的同族静默失败）**：原来这两句是
                    //   **光秃秃的 `return`** —— 对面发来一条解不出来的 `action`，本机**一点痕迹都没有**
                    //   （对照：同一段 `Dispatch()` 末尾「不认识的联机消息」**有** `LogWarning`；
                    //    `NetSession.cs` 里「没有设密码」那句也有 ⇒ 同文件里原来两种口径）。⇒ 全部改成出声。
                    //   ⚠️ **2026-10-19 订正（原文→实际）**：原写死行号 `:829` / `NetSession.cs:334`，
                    //   两处都已漂（现分别在 `NetBattle.cs` 的 `Dispatch()` 末尾、`NetSession.cs` 的
                    //   「没有设密码」那一支）⇒ 改成**符号锚点**：行号会漂、锚点不会（TOOL-6 报告）。
                    if (m == null)
                    {
                        Debug.LogWarning("[Net] 对面发来的 `action` **解不出来**（payload 坏了 / 版本对不上）"
                                       + $"—— 丢掉这一条，不做任何落地。会话 {f.kind}");
                        return;
                    }
                    if (_d.Ctx == null)
                    {
                        Debug.LogWarning("[Net] 收到对面的 `action`，但本机**没有对局状态**（`Ctx == null`）"
                                       + "—— 丢掉这一条（这一档只在没有 `Ctx` 的宿主里会走到）");
                        return;
                    }
                    // 🆕 2026-09-30：**进攻卡 / 防御卡**那两条发生在开打之前 ⇒ 「不是你的回合」那道
                    //   守卫对它们不适用（那时候还没轮到谁）。其余动作照旧拦。
                    if (!NetApply.IsEnvPick(m) && _d.Ctx.Active != RemoteSeat)
                    {
                        // 🆕 2026-10-26（批次 6 · G15）：这一档 = 原版 `CancelMatchWithVictory`
                        //   （对面那一手本机判定为非法 ⇒ **本机主张胜利**）。判据与该路径的四步 → `AbortWithVictory` 的 doc。
                        AbortWithVictory($"对面在**不是他回合**的时候发了一条动作（现在行动方是本机）—— 拒绝并中止");
                        // 🔴 **2026-10-19（P6d · A1038）**：线上发**词条键**（收侧 `NetWireText.Unpack` 取词）
                        //    —— 原来发的是中文整句「不是你的回合」，等于把**本机**的界面语言灌到对面屏幕上。
                        Send(NetKind.Reject, new MsgReject
                        {
                            seq = m.seq,
                            reason = NetWireText.Pack("Settings/Online/Wire/NotYourTurn"),
                        });
                        return;
                    }
                    int code = _d.ApplyLoggedAction(m);
                    if (code != RuleCodes.OK)
                    {
                        Send(NetKind.Reject, new MsgReject { seq = m.seq, reason = RuleCodes.Describe(code) });
                        // 🆕 2026-10-26（批次 6 · G15）：对面那一手本机引擎拒了 ⇒ 原版 `CancelMatchWithVictory`
                        //   （本机主张胜利；判据 → `AbortWithVictory` 的 doc）。
                        AbortWithVictory($"引擎拒了对面那条动作：{RuleCodes.Describe(code)}");
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
                    // 🔴 2026-10-18（同族静默失败）：主机收到 `applied` 本来是**无声忽略** ——
                    //   这是「消息走反了方向」的征兆（主机才是发 `applied` 的那一端）⇒ 出声。
                    if (IsHost)
                    {
                        Debug.LogWarning("[Net] 主机收到了 `applied`（这条该只有客机收）—— 忽略；"
                                       + "它说明有一端把消息发反了方向");
                        return;
                    }
                    var m = NetProtocol.Unpack<MsgAction>(f.payload);
                    // 🔴 2026-10-18（同族静默失败）：原来也是光秃秃的 `return`（上面 `action` 那两条同理）。
                    if (m == null)
                    {
                        Debug.LogWarning("[Net] 主机广播来的 `applied` **解不出来**（payload 坏了）"
                                       + "—— 丢掉这一条；⚠️ 本机日志会因此**少一格 seq**，指纹迟早会对不上");
                        return;
                    }
                    // 日志按 seq 对齐（自己发出去的那条也会被广播回来 ⇒ 靠 seq 认领）
                    while (Log.Count <= m.seq) Log.Add(null);
                    if (m.seq >= 0 && m.seq < Log.Count) Log[m.seq] = m; NoteApplied(m);   // 换牌兜底那两个退出条件
                    // 🔴 `preApplied` = 主机说「这条你本地已经落过了」（乐观执行那条路）⇒ 只认领 seq，别再落一遍
                    if (m.preApplied) return;
                    // 剩下的都要落地：**主机自己的动作** + **换牌那两条伪动作**（它们是主机定序的）
                    int code = _d.ApplyLoggedAction(m);
                    if (code != RuleCodes.OK)
                        // 🆕 2026-10-26（批次 6 · G15）：对面（主机）那条动作本机引擎拒了 ⇒ 原版 `CancelMatchWithVictory`
                        //   （本机主张胜利；判据 → `AbortWithVictory` 的 doc）。
                        AbortWithVictory($"本机引擎拒了对面的动作（{RuleCodes.Describe(code)}，kind={m.kind}）—— 两端打岔了");
                    return;
                }
                case NetKind.Reject:
                {
                    var m = NetProtocol.Unpack<MsgReject>(f.payload);
                    // 🔴 **A961（2026-10-18，主对话裁定「接着做完」）**：`MsgReject.reason`（`NetProtocol.MsgReject.reason`）
                    //   **是对端可控的**，而它经 `Abort` 进**提示行**（`NetSay`）+ **弹窗**（`NetRuntime.Notice`）
                    //   ⇒ 与另外三个入口（`MsgBye.reason` / `MsgProof.name` / `MsgAck.reason`）**同一处口径**：
                    //   **进界面前先钳**（复用现成的 `NetSession.ClampPeerText`，⛔ 不写第二份）。
                    //   ⚠️ 这是我们自拟的（原版没有对端文本进界面的先例）→ `NetProtocol.MaxPeerTextChars`。
                    // 🔴 **2026-10-19（P6d）**：钳完再 `NetWireText.Unpack`（**次序硬** —— 先钳**对端可控的原串**、
                    //   再取词；反过来的话 40 字钳的是我们自己的文案）。
                    //   ⚠️ 这里还有**第二类发送方**：主机引擎拒动作时发的是 `RuleCodes.Describe(code)`
                    //   （上面 `case NetKind.Action` 里那条 `Send(NetKind.Reject, …)`）—— 那是**中文整句、不是键**
                    //   ⇒ `Unpack` 的 `HasEntry` 为假 ⇒ **原样回显**，行为与接线前逐字相同
                    //   （`RuleCodes.cs` 逐字写着「改成键」是 `A985⑧` 第 ③ 步、本轮不做）。
                    //   🔴 **2026-10-26（批次 6 · G15）**：这一档**仍然是 `Abort`（只报不打分）** —— 判胜负那一跳
                    //   **不在这里**：对面拒了我的动作之后会紧跟一条 `NetKind.Desync`（原版 `CancelMatchWithVictory`
                    //   那四步里的 ③），**判本机负**由那一支走 `<see cref="AcceptPeerDesync"/>`。
                    //   ⚠️ **为什么不在这里直接判**：① 原版收方（`ReceiveEnemyDesync`）也不在这一跳判；
                    //     ② `Editor/NetSelfTest.cs` 的 Q⑪/Q⑫ 钉的正是「`reject.reason` 那串对端可控文本
                    //        经 `Abort` **钳过**才进提示行 + 弹窗」—— 挪走就会把那条断言弄成空转（它会在 0 条通知上红）。
                    //   ⚠️ 两条消息（`reject` / `desync`）**可能都到**：这个 `Abort` 先把 `_aborted` 立起来（停止再发动作），
                    //     而 `AcceptPeerDesync` 用的是**它自己的** `_desyncAccepted` 闸 ⇒ 判负那一跳照样会走到（只走一次）。
                    Abort($"主机拒绝了我的动作：{(m != null ? NetWireText.Unpack(NetSession.ClampPeerText(m.reason)) : "?")}");
                    return;
                }
                case NetKind.Mulligan:                                  // 主机收
                {
                    var m = NetProtocol.Unpack<MsgMulligan>(f.payload);
                    // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：坏包原来被**静默**
                    //   当成「对面换了 0 张」继续定序 ⇒ 两端换牌结果不同、指纹迟早对不上。出声。
                    if (m == null)
                        Debug.LogWarning("[Net] NET/BYE-R1/mulligan-unparsable 对手换牌包 **解不出来** ⇒ 按「换了 0 张」继续定序"
                                       + "（这一条不改行为；真连看到它 = 换牌包坏了，两端牌序可能因此分叉）");
                    _theirMulligan = true; _theirMulliganMarks = m != null ? m.indices : new int[0]; _peerMulliganSeen = true;   // 原版 `+0x29c`
                    Debug.Log($"[Net] 主机收到对手换牌（{(_theirMulliganMarks != null ? _theirMulliganMarks.Length : 0)} 张）");
                    if (_myMulligan != null && !_mulliganResolved) HostResolveMulligan();   // ⛔ 已定序就不再定一次（重复换牌包会让牌被洗两遍）
                    return;
                }
                case NetKind.Hash:
                {
                    var m = NetProtocol.Unpack<MsgHash>(f.payload);
                    // 🔴 2026-10-18（同族静默失败）：原来是一条**合取的 `return`**（两个原因共用一句）——
                    //   拆开各自出声，否则「包坏了」和「本机没有对局状态」在日志里长得一样。
                    if (m == null)
                    {
                        Debug.LogWarning("[Net] 对面发来的 `hash` **解不出来**（payload 坏了）—— 丢掉这一条"
                                       + "；⚠️ 这一回合的指纹对账就此落空（下一次对账还有机会）");
                        return;
                    }
                    if (_d.Ctx == null)
                    {
                        Debug.LogWarning("[Net] 收到对面的 `hash`，但本机**没有对局状态**（`Ctx == null`）"
                                       + "—— 没法对账，丢掉这一条");
                        return;
                    }
                    if (m.turn != _d.Ctx.Turn) { Debug.Log($"[Net] 指纹回合对不上（对面 {m.turn} / 本机 {_d.Ctx.Turn}）—— 可能是交叉到达，先不判"); return; }
                    int mine = NetProtocol.Fingerprint(_d.Ctx);
                    if (mine != m.hash)
                    {
                        Abort($"**状态指纹对不上**（第 {m.turn} 回合：对面 {m.hash} / 本机 {mine}）—— 两端已经打岔");
                        // 🆕 2026-10-26（批次 8 · 执行代理 L）：**这一档原先"挂着不结束"** ——
                        //   原版同一档的收尾是 `CancelMatchWithoutResult` ⇒ `CancelMatch(false)` ⇒ `LeaveBattle`（不判结果）。
                        //   判据与该路径的每一跳 → `LeaveWithoutResult` 的 doc。⛔ **不判负也不判胜**（原版那条链里没有 `DeadHero`）。
                        LeaveWithoutResult($"第 {m.turn} 回合状态指纹对不上（对面 {m.hash} / 本机 {mine}）");
                    }
                    else Debug.Log($"[Net] 第 {m.turn} 回合指纹一致（{mine}）✅");
                    return;
                }
                case NetKind.Resign:
                {
                    // 🆕 2026-10-26（执行代理 W2）：整支挪进类尾的 `AcceptPeerForfeit()` = 原版 `ReceiveEnemyForfeit`
                    //   的四步（`BattleManager__ReceiveEnemyForfeit.c:19-37`）：停两处协程 → 连接状态置 4
                    //   （`EnemyForfeit`）→ `matchFinished(+0x2cc) = true` → `DeadHero(对面, 2)`。
                    //   ⚠️ 原来这一支**只有第 ④ 步**（判对面负）⇒ 账 516 只能记 B；①②③ 本轮补上。
                    //   🔴 理由码 = **`Forfeit`(2)**，那个 `2` 在原版是**写死**的（`SendForfeit` 不带参数）。
                    //   ⚠️ 与 `HandlePeerClosed`（对面说 `bye`）那一档**不是**同一条：那边理由码是 `Disconnect`(3)，
                    //      因为原版「对面不会回来了」走的是掉线那条链（`DeadHero(对面, 3)`）——
                    //      `Forfeit`(2) 只留给「对面**真的点了**投降」这一条（判据同 `NetRemoteResign` 的 doc）。
                    //   ⚠️ 第 ② 步（连接状态 4）我们**没有那一格**，如实记在 `AcceptPeerForfeit` 的 doc 里，⛔ 不造假状态。
                    //   判据逐句 → 那个方法的 doc；无宿主时仍**出声**（⛔ 不静默，T05 探针那条）。
                    AcceptPeerForfeit();
                    return;
                }
                case NetKind.Desync:
                {
                    // 🆕 **2026-10-26（批次 6 · G15）**：对面说「这一局已经打岔了」（原版
                    //   `PUNReceiveEnemyDesync` → `BattleManager.ReceiveEnemyDesync`）。载荷为空（原版那条 RPC 也无参）。
                    Debug.Log("[Net] 对面发来 `desync`（= 原版 `SendDesync` / `PUNReceiveEnemyDesync`）"
                            + " ⇒ 认账、判本机负（理由码 `Cancelled`(5)，**不弹窗** —— 原版收方 `ReceiveEnemyDesync` 也不弹）");
                    AcceptPeerDesync("对面通过 `NetKind.Desync` 声称这一局打岔了");
                    return;
                }
                case NetKind.Chat:
                {
                    // 🆕 **2026-10-25（执行代理 `CH`）**：对面督军说了第 `chatId` 句。
                    //   判据链（逐句）→ `NetChat` 文件头；这里只做**四道闸**，然后交给宿主显示：
                    //     ① 包解不出来 ⇒ 出声丢掉；
                    //     ② **发送方 == 本机座位** ⇒ **什么都不做**（原版 `PUNReceiveChatMessage`
                    //        那条「自己那条回显不显示」的过滤；⛔ **也不回发** —— 回环防护就在这一支，
                    //        原版收到那一档压根不进发送支，`DisplayWarlordChatMessage.c:53`）；
                    //     ③ 座位越界 ⇒ 出声丢掉（协议只有 0/1）；
                    //     ④ `chatId` 不在 5…10 ⇒ 出声丢掉（对面可以发任意 int，⛔ 不许拿它去索引台词表）。
                    var m = NetProtocol.Unpack<MsgChat>(f.payload);
                    if (m == null)
                    {
                        Debug.LogWarning("[Net] 对面发来的 `chat` **解不出来**（payload 坏了 / 版本对不上）"
                                       + "—— 丢掉这一条，不显示也不转发");
                        return;
                    }
                    if (m.seat == MySeat)
                    {
                        // 正常情况走不到这里（对端不会把本机那条回显回来）；走到 = 有人伪造 / 对端
                        // 把消息发反了方向 ⇒ 出声。**但绝不回发**（否则两端会打乒乓）。
                        Debug.LogWarning($"[Net] 收到一条 `chat` 带着**本机座位**（{MySeat}，chatId={m.chatId}）"
                                       + "—— 按原版的发送者过滤**不显示**，也**不回发**（防回环）");
                        return;
                    }
                    if (m.seat != RemoteSeat)
                    {
                        Debug.LogWarning($"[Net] 收到一条 `chat`，座位 `{m.seat}` 既不是本机（{MySeat}）"
                                       + $"也不是对面（{RemoteSeat}）—— 丢掉这一条");
                        return;
                    }
                    if (!NetChat.IsButtonLine(m.chatId))
                    {
                        Debug.LogWarning($"[Net] 对面发来的 `chat` 台词号 `{m.chatId}` 不在"
                                       + $" {NetChat.FirstButtonChatId}…{NetChat.LastButtonChatId} 这一档"
                                       + " —— 丢掉这一条（⛔ 不猜一个最接近的：那会把一句伪造的话说成督军台词）");
                        return;
                    }
                    if (_d is INetChatHost chatHost) chatHost.NetRemoteChat(m.chatId);
                    else
                        // 🔴 不许静默：宿主没接显示面（自检里那些只管动作流的假宿主就是这一档）
                        Debug.LogWarning($"[Net] 对面督军说了第 {m.chatId} 句，但本机宿主**没有接聊天显示**"
                                       + $"（`{_d?.GetType().Name ?? "<null>"}` 没实现 `INetChatHost`）"
                                       + " —— 这一句不会出现在任何气泡上");
                    return;
                }
                // ---- 🆕 2026-10-25（D 域）：原版三条**纯表现** RPC 的接收侧 ----
                //   ⚠️ 三条都**不进权威流、不改指纹**；宿主没实现 `INetFoePresentationHost` ⇒ **出声**
                //      （⛔ 不静默），但**不影响对局**。
                case NetKind.CardSeen:
                {
                    var m = NetProtocol.Unpack<MsgCardSeen>(f.payload);
                    if (m == null || m.cardId < 0)
                    {
                        Debug.LogWarning($"[Net] 对面发来的 `card.seen` 解不出来或 id 非法（`{m?.cardId}`）—— 丢掉");
                        return;
                    }
                    if (_d is INetFoePresentationHost h1) h1.NetRemoteCardSeen(m.cardId);
                    else Debug.LogWarning($"[Net] 对面查看了牌 {m.cardId}，但宿主 `{_d?.GetType().Name ?? "<null>"}`"
                                        + " 没实现 `INetFoePresentationHost` ⇒ 这一次揭示不会出现（不影响对局）");
                    return;
                }
                case NetKind.CardReorder:
                {
                    var m = NetProtocol.Unpack<MsgCardReorder>(f.payload);
                    if (m == null || m.cardId < 0 || m.index < 0)
                    {
                        Debug.LogWarning($"[Net] 对面发来的 `card.reorder` 解不出来或参数非法"
                                       + $"（`{m?.cardId}` → `{m?.index}`）—— 丢掉");
                        return;
                    }
                    if (_d is INetFoePresentationHost h2) h2.NetRemoteCardReorder(m.cardId, m.index);
                    else Debug.LogWarning($"[Net] 对面把牌 {m.cardId} 挪到 {m.index}，但宿主没实现 "
                                        + "`INetFoePresentationHost` ⇒ 对手手牌不会重排（不影响对局）");
                    return;
                }
                case NetKind.MatchFinished:
                {
                    // 原版 `PUNReceiveMatchFinished` → `BattleManager.ReceiveMatchFinished`
                    // （`BattleManager__ReceiveMatchFinished.c:16-19`）：**只把 `matchFinished(+0x2cc)` 置 1**，
                    // 没有别的副作用；重复收到就是再置一次 1（幂等），那个标志正是 `ShouldWaitForOpponentLastActions` 的第一个条件。
                    // 🆕 2026-10-26（执行代理 W2）：整支挪进类尾的 `AcceptPeerMatchFinished()`（幂等闸 +
                    // 表现层通知 `INetFoePresentationHost.NetRemoteMatchFinished`），判据逐行见那个方法的 doc
                    // ⇒ 账 518 因此有了**方法级**落点（原来只落一个 `case` 支 ⇒ 只能记 B）。
                    // ⚠️ 它**不判胜负**：只用来开「等对手最后几条动作」那个窗口（胜负由 `DeadHero` 那条链判）。
                    AcceptPeerMatchFinished();
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
            Debug.Log($"[Net] 换牌已定序（主机 {seat0.Length} 张 / 客机 {seat1.Length} 张）"); NoteMulliganResolved("主机定序完成（seat0 → seat1 → mulliganDone）");
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
            if (IsHost)
            {
                // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：`resume` 只有
                //   主机发得出来（客机收）。主机收到它 = 消息走反了方向 ⇒ 丢掉，但必须说出来。
                Debug.LogWarning("[Net] NET/BYE-R1/resume-wrong-direction 主机收到了 `resume`（它该只有客机收）"
                               + " ⇒ 忽略；这条消息走反了方向，查对面是谁发的");
                return;
            }
            Debug.Log($"[Net] 重连：重建这一局并重放 {actions.Count} 条动作");
            Log.Clear();
            foreach (var m in actions) Log.Add(m);
            if (_d != null) _d.NetReplayFromNet(start, actions);
        }

        /// <summary>开场时把 `MsgStart` 记下来（重连要用）。</summary>
        public void RememberStart(MsgStart start) { _start = start; }

        /// <summary>对局是不是**正在进行中**（会话在且状态 = `InBattle`）。
        /// 🆕 **2026-10-25（N9）**：给「战斗内表现消息」的**调用点**当发前闸 ——
        /// `SendCardSeen` / `SendCardReorder` 自己也会挡（`CanSendPresentation`）并出声，
        /// 但那会**每次**都打一条 warning；调用点先用它挡掉「换牌/大厅阶段顺手触发」的那些噪声。
        /// ⚠️ 判据仍然只此一处（`NetState.InBattle`），⛔ 别在调用点另写一遍状态比较。
        /// 📌 **故意放在类尾**（2026-10-25）：本类被别的账/报告按行号引用得多，插在中间会把
        /// 下游行号整体推移 —— 新增成员一律追加到末尾，别插进已有成员之间。</summary>
        public bool InBattle { get { return _s != null && _s.State == NetState.InBattle; } }

        // ==================================================================
        //  🆕 2026-10-26（批次 8 · 执行代理 L）：**不判结果直接离场** —— `CancelMatch(false)` 那一半
        // ==================================================================
        //  为什么要有它（G15 当时如实留下的缺口）：原版那条链的最后一步是
        //    `BattleErrorUIManager.OpponentDesyncCancelMatch(MatchWon: false)`（`decomp_full/Everguild.BattleErrorUIManager__OpponentDesyncCancelMatch.c:1-30`）
        //    的提交钮 = `BattleManager.CancelMatch(false)`（**逐句**：`decomp_full/BattleManager__CancelMatch.c:16-26`）：
        //      `param_2 == 0` ⇒ `BattleNetworkManager.LeaveBattle()` 然后 **return**（⛔ 没有 `AddResignAction` /
        //      `DeadHero` —— 那两句只在 `MatchWon == true` 那一支 ⇒ **这条路按定义不产生结果**）。
        //    `BattleNetworkManager.LeaveBattle()`（`decomp_full/BattleNetworkManager__LeaveBattle.c:38-47`）里那两跳 =
        //      `NetworkCustomManager.RemoveRoomAfterLeaving()`（房间 `EmptyRoomTtl = 0`，人走房散）
        //      + **`NetworkCustomManager.LeaveRoom()`** —— **不捎任何告别**；
        //    对端因此走的是 `OnPhotonPlayerDisconnected` ⇒ `EventPlayerDisconnected` ⇒ `SetOpponentDisconnected`
        //      （`…__EventPlayerDisconnected.c:45-52`）⇒ 弹窗 + 停表 + **它自己的** `ForfeitDisconnectedEnemy`
        //      倒计时（`max(15, 30 − 3×enemyDisconnects)` 秒）**到点才判我们弃权**。
        //
        //  ⛔ **不能拿 `Close(say: true)` 顶这一跳**：`say:true` 会发 `NetKind.Bye` ⇒ 对端 `HandlePeerClosed`
        //    **当场判我们弃权**（本文件 `ForfeitPeerIfLeftMidGame`），而原版要等对端那个 15/30 秒窗口。
        //    ⇒ 唯一正确的原语 = `NetSession.Close(say: false)`（硬断，`NetSession.cs` 的 `Close`；
        //      `sentBye == false` ⇒ `CloseTransportAfterBye` 走 `HardCloseRequestedForTest` 那一档）。
        //  ⛔ **不加判负/判胜支**：本机这一侧也**不产生结果**（原版这条链里没有 `DeadHero`）——
        //    本机只是「离场」，结果由**对端**在它自己的倒计时到点之后判（`ReconnectCountdownExpired`）。
        //  ⚠️ **与原版的一处差（如实记，⛔ 别当成复刻）**：原版是**玩家点了 `SubmitButton` 之后**才
        //    `CancelMatch(false)`（`…__OpponentDesyncCancelMatch.c` 的 `ShowPopUp(…, SubmitButton, onSubmit)`），
        //    我们这里是**调用即离场**（调用点 `case NetKind.Hash` 的那一档**已经**由 `Abort` 弹过一扇告知窗，
        //    再等一次点击会变成两扇窗叠着等）。⇒ 差异列里写死这一条，别把「立刻」读成「照原版」。
        //
        //  调用点（今天只有一处，⛔ 别在别处再叫）：`Dispatch` 的 `case NetKind.Hash` —— **状态指纹对不上**。
        //    （`case NetKind.Reject` **不调**：那一档紧跟的 `NetKind.Desync` 还要判本机负，
        //      先硬断会把那条在飞的帧丢掉 ⇒ 判负那一跳丢失。理由见 `case NetKind.Reject` 的注。）

        /// <summary>自检读口：本机以「不判结果直接离场」收过几次（原版 `CancelMatch(false)` 那一跳）。
        /// ⚠️ 它**不是**「这一局判了谁」—— 这条路按定义不产生结果。</summary>
        public int NoResultLeavesForTest { get { return _noResultLeaves; } }
        int _noResultLeaves;
        /// <summary>只认一次（对齐原版那条链的语义：离场一次就够；重复调不许把状态字再改一遍）。</summary>
        bool _leftWithoutResult;

        /// <summary>🆕 **2026-10-26（批次 8 · L）**：**不判结果直接离场** —— 原版
        /// `BattleManager.CancelMatch(false)` ⇒ `BattleNetworkManager.LeaveBattle()` ⇒ `NetworkCustomManager.LeaveRoom()`
        /// 的本地等价。判据逐跳见本节头部那一段。
        /// <para>语义三条（缺一条就会走成别的路）：① **不判胜负**（没有 `DeadHero` 那一跳）；
        /// ② **不发 `bye`**（硬断 ⇒ 对端靠自己的倒计时判，原版 `OnPhotonPlayerDisconnected` 同形）；
        /// ③ **本机状态字说「房间已经散了」**（`Settings/Online/Wire/RoomGone`，与 `ReconnectCountdownExpired`
        /// 离开房间那一档同一个键）。</para>
        /// <para>返回真 = 真做了离场；假 = 已经离过场（幂等），或压根没有会话（那一档**出声**）。</para></summary>
        /// <param name="why">为什么离场（只进日志与提示行 —— ⛔ 不上对端屏幕：原版那条 `LeaveRoom` 一个字也带不出去）。</param>
        public bool LeaveWithoutResult(string why)
        {
            if (_leftWithoutResult) return false;      // 幂等（原版那条链只会被点一次）
            _leftWithoutResult = true;
            _noResultLeaves++;
            _aborted = true;                           // 同 `Abort`：之后不再发动作（对面已经不在了）
            Debug.LogError("[Net] 🔴 不判结果直接离场（原版 `CancelMatchWithoutResult` → `CancelMatch(false)` → "
                         + "`BattleNetworkManager.LeaveBattle` → `NetworkCustomManager.LeaveRoom`）：" + why
                         + " —— ⛔ 本机**不判胜负**、也**不发 `bye`**（原版 `LeaveRoom` 也不发）");
            if (_d != null) _d.NetSay("联机对局中止：" + why + "（不判胜负，直接离开）");
            if (_s == null)
            {
                Debug.LogWarning("[Net] 「不判结果直接离场」那一跳走不了：本类**没有挂会话**（`_s == null`）"
                               + " —— 如实说出来（同族先例：`ReconnectCountdownExpired` 的 `_s == null` 那一支）");
                return false;
            }
            // 🔴 `say: false` 是这一跳的**判据本身**（见本节头部那一段）：发了 `bye` 就变成「当场判我们弃权」。
            _s.Close(false, "Settings/Online/Wire/RoomGone");
            Debug.Log("[Net] 已离场（`NetSession.Close(say:false)` ⇒ 硬断、不发 `bye`）—— 对端会用自己的"
                    + " `ForfeitDisconnectedEnemy` 倒计时（`max(15, 30 − 3×n)` 秒）到点才判；"
                    + "本机这一侧**没有结果**（原版同一支里也没有 `DeadHero`）");
            return true;
        }

        // ==================================================================
        //  🆕 2026-10-26（执行代理 W2 · B9 P1）：原版「兜底倒计时」两个协程 + 收方三条 + 收尾窗口的收尾支
        // ==================================================================
        //  🔴 判据一律取自**原版方法体**（`D:/2/tools/decomp_full/**` 与 `cpp2il/out_cpp2il_dev_ilspy/**`），
        //     ⛔ 不用我们自己的注释当判据。逐支出处写在各方法的 doc 里。
        //  ⚠️ 整段**故意追加在类尾**：插在中间会让账里按 `文件:行号` 的落点整片漂移
        //     （`CLAUDE.md` §三「新成员一律追加到类尾」）。

        /// <summary>原版 `scenarioVariables.clockTimeLimit` = **60**（`DefaultScenario.json:19`，
        /// `bundle_duplicateassetisolationso_assets_all/MonoBehaviour/`）。`InBattleFallbackCountdown`
        /// 等的是它 **× 3**（`BattleManager._InBattleFallbackCountdown_d__400__MoveNext.c:43-45`）。</summary>
        public const float FallbackClockTimeLimit = 60f;
        /// <summary>原版换牌兜底每拍等的秒数：`new WaitForSeconds(4f)`
        /// （`out_cpp2il_dev_ilspy/BattleManager.cs:3733`；`.c` 里是 `DAT_1834b2df0`）。</summary>
        public const float MulliganFallbackInterval = 4f;
        /// <summary>换牌兜底的判胜阈值（原版 `attemptsCount > 15`，`.c:84`）。</summary>
        public const int MulliganFallbackAttempts = 15;
        /// <summary>对局中兜底的判胜阈值（原版 `attemptsCount > 10`，`.c:75`）。</summary>
        public const int InBattleFallbackAttempts = 10;

        // ---- 换牌兜底（原版 `MulliganFallbackCountdown`）----
        bool _mulliganFallbackOn;
        float _mulliganFallbackNextAt;
        /// <summary>原版那个 `attemptsCount`（`.c` 的 `param_1+0x30`）。</summary>
        int _mulliganFallbackAttempts;
        /// <summary>原版 `+0x29c`（「我已经收到对面的换牌」）—— 与 `ReceiveMulliganAck` 置的 `+0x2a8`
        /// 一起构成本协程的两个退出条件（`.c:61-72`）。</summary>
        bool _peerMulliganSeen;
        /// <summary>原版 `+0x2a8`（「对面确认了我的换牌」，由 `ReceiveMulliganAck` 置真，`.c:19-28`）。</summary>
        bool _mulliganAcked;
        /// <summary>本局换牌阶段已经定序完成（= 上面两格都成立的那个**本地等价**）。</summary>
        bool _mulliganResolved;

        // ---- 对局中兜底（原版 `InBattleFallbackCountdown`）----
        /// <summary>上一次看到的「行动方 + 回合」；一变就重置计时（原版拿 `+0x3f8` 前后比对，`.c:88/118`）。</summary>
        int _fallbackSig = int.MinValue;
        float _fallbackNextAt;
        int _fallbackAttempts;
        /// <summary>「掉线时先把对手最后几条动作等完」只延一次（原版那两个协程的起停是配对的，
        /// 延完还延就等于永远不判弃权）。</summary>
        bool _peerLastActionsWaited;
        /// <summary>对面已经投降（原版 `ReceiveEnemyForfeit` 那一跳；只为幂等 + `BeginFinishWait` 的闸）。</summary>
        bool _peerForfeited;

        /// <summary>自检读口：换牌兜底现在在不在跑（原版那条协程活着没有）。</summary>
        public bool MulliganFallbackRunning { get { return _mulliganFallbackOn; } }
        /// <summary>自检读口：换牌兜底已经数到第几拍。</summary>
        public int MulliganFallbackAttemptsForTest { get { return _mulliganFallbackAttempts; } }
        /// <summary>自检读口：对局中兜底已经数到第几拍。</summary>
        public int InBattleFallbackAttemptsForTest { get { return _fallbackAttempts; } }
        /// <summary>自检读口：原版 `ReceiveMulliganAck` 置的那一格（`+0x2a8`）。</summary>
        public bool MulliganAcked { get { return _mulliganAcked; } }
        /// <summary>自检读口：本局换牌阶段定序完成没有。</summary>
        public bool MulliganPhaseResolved { get { return _mulliganResolved; } }

        /// <summary>原版 `MulliganFallbackCountdown` 的**启动点**（ILSpy `BattleManager.cs:25865`：联机局
        /// 「本地提交换牌 + `SendMulliganCards()`」之后）。⚠️ **两端都起**（原版两端各跑一份）。
        /// ⚠️ 我们只在会话已经在「对局/等重连」那一档时起 —— 大厅阶段收到换牌提交是调用点搞错了。</summary>
        void BeginMulliganFallback()
        {
            if (_mulliganFallbackOn || _mulliganResolved) return;
            if (_s == null || _s.State == NetState.Off || _s.State == NetState.Closed) return;
            _mulliganFallbackOn = true;
            _mulliganFallbackAttempts = 0;
            _mulliganFallbackNextAt = NowSec() + MulliganFallbackInterval;
            Debug.Log("[Net] 换牌兜底开始（原版 `MulliganFallbackCountdown`：每 "
                    + $"{MulliganFallbackInterval} 秒一拍，`attempts > {MulliganFallbackAttempts}` 且仍没收到对面换牌"
                    + " ⇒ `CancelMatchWithVictory` = **判本机胜**）");
        }

        /// <summary>换牌阶段定序完成 ⇒ 收掉兜底。原版停止点 = ILSpy `BattleManager.cs:3082` 的
        /// `StopCoroutine("MulliganFallbackCountdown")`（那一刻的日志是 `Finished waitingForEnemyMulligan`）。
        /// 🔴 同时把 `+0x29c` / `+0x2a8` 两格置真 —— 那就是原版那两个退出条件。</summary>
        void NoteMulliganResolved(string why)
        {
            if (_mulliganResolved) return;
            _mulliganResolved = true;
            _peerMulliganSeen = true;
            _mulliganAcked = true;      // = 原版 `ReceiveMulliganAck` 置的 `+0x2a8`（我们的 ack 就是定序结果）
            _mulliganFallbackOn = false;
            Debug.Log("[Net] 换牌阶段定序完成 ⇒ 换牌兜底收掉（原版 `StopCoroutine(\"MulliganFallbackCountdown\")`）：" + why);
        }

        /// <summary>每拍推一次。**由 <see cref="TickReconnectCountdown"/> 在「没在跑掉线倒计时」时转调** ——
        /// 那正是原版的 `waitingWhileDisconnected` 闸（`.c:117-160`：掉线期间只 `yield`、不计数）。</summary>
        void TickNetFallbacks()
        {
            float now = NowSec();
            TickMulliganFallback(now);
            TickInBattleFallback(now);
        }

        /// <summary>换牌兜底一拍。逐条对 `.c:79-161`：
        /// ① `attemptsCount++`；② 没收到对面换牌 ⇒ `RequestMulliganResend(attempts)`、收到了 ⇒ `ConfirmMulliganReceived(...)`；
        /// ③ `attempts > 15` 且**仍没收到对面换牌** ⇒ `CancelMatchWithVictory("MulliganFallbackCountdown " + opponentId)`
        /// （⇒ **判本机胜**）。
        /// <para>⚠️ **两处本地等价（如实标，⛔ 不是原版那一句）**：
        /// ① `ConfirmMulliganReceived` 我们**不发帧** —— 我们的「确认」就是定序结果广播（`applied(mulliganDone)`），
        /// 重发它会让对端在 `case NetKind.Applied` 里把换牌**再落地一次**；
        /// ② `RequestMulliganResend`（原版是「请对面重发」）我们落成**自己重发自己的换牌** ——
        /// 净效果同为「让缺的那条换牌包重传」，且不必新增消息种类（`NetKind` 不在本笔白名单里）。</para></summary>
        void TickMulliganFallback(float now)
        {
            if (!_mulliganFallbackOn) return;
            if (_mulliganResolved) { _mulliganFallbackOn = false; return; }
            if (_d == null || _d.Ctx == null) return;
            if (_d.Ctx.IsOver) { _mulliganFallbackOn = false; return; }
            if (now < _mulliganFallbackNextAt) return;
            _mulliganFallbackNextAt = now + MulliganFallbackInterval;
            _mulliganFallbackAttempts++;
            if (!_peerMulliganSeen)
            {
                if (!IsHost && _myMulligan != null)
                    Send(NetKind.Mulligan, new MsgMulligan { indices = _myMulligan });   // = 原版 `RequestMulliganResend`
                if (_mulliganFallbackAttempts > MulliganFallbackAttempts)
                {
                    _mulliganFallbackOn = false;
                    AbortWithVictory($"换牌兜底：对面 {MulliganFallbackAttempts} 拍（每拍 {MulliganFallbackInterval} 秒）"
                                   + "一直没把换牌发过来（原版 `CancelMatchWithVictory(\"MulliganFallbackCountdown \" + opponentId)`"
                                   + " ⇒ 判本机胜）");
                }
                return;
            }
            Debug.Log($"[Net] 换牌兜底第 {_mulliganFallbackAttempts} 拍：已收到对面换牌 ⇒ 原版这一跳是 "
                    + "`ConfirmMulliganReceived`（我们不发帧 —— 确认 = 定序结果广播，理由见本方法 doc）");
        }

        /// <summary>对局中兜底一拍。逐条对 `.c:27-133`：
        /// 记下**那一刻的行动方**（`+0x3f8`）→ 等 `clockTimeLimit × 3`（= **180 秒**）→
        /// 之后每拍「行动方没变」⇒ `attemptsCount++` + `ResendLostActions()` + 等约 1 秒；
        /// `attempts > 10` 且行动方仍没变 ⇒ `LogError("Failed InBattleFallbackCountdown after 10 attempts for player …")`
        /// + `CancelMatchWithVictory("InBattleFallbackCountdown")`（⇒ **判本机胜**）；行动方一变 ⇒ 协程结束。
        /// <para>⚠️ **两处如实记**：① `ResendLostActions()` 在本机**没有对应物** —— 我们走 TCP（有序可靠，
        /// 对局内不会丢动作），重连走「全量重放」⇒ 这一跳是**空操作**，⛔ 别为它造一条假的补发；
        /// ② 原版的**启动点读不到**（`decomp_full` 里除自身外 0 引用，ILSpy 只有 `StopBattleAfterBattleEnd`
        /// 的 `StopCoroutine`；那属于「没方法体」那一族）⇒ 本实现按「每回合重置」对待。</para></summary>
        void TickInBattleFallback(float now)
        {
            if (_d == null || _d.Ctx == null || _d.Ctx.IsOver) { _fallbackSig = int.MinValue; return; }
            if (_s == null || _s.State != NetState.InBattle) { _fallbackSig = int.MinValue; return; }
            if (ClockPaused) return;                                               // 掉线期间由 `ForfeitDisconnectedEnemy` 那条管
            if (_d.Ctx.MulliganOpen) { _fallbackSig = int.MinValue; return; }       // 换牌阶段不算（那是上面那个兜底的事）
            int sig = _d.Ctx.Active * 1000000 + _d.Ctx.Turn;
            if (sig != _fallbackSig)
            {
                _fallbackSig = sig; _fallbackAttempts = 0;
                _fallbackNextAt = now + FallbackClockTimeLimit * 3f;
                return;
            }
            if (now < _fallbackNextAt) return;
            _fallbackNextAt = now + 1f;
            _fallbackAttempts++;
            if (_fallbackAttempts > InBattleFallbackAttempts)
            {
                int who = _d.Ctx.Active;
                _fallbackSig = int.MinValue;
                Debug.LogError($"[Net] Failed InBattleFallbackCountdown after {InBattleFallbackAttempts} attempts "
                             + $"for player {(who == MySeat ? "本机" : "对面")}（原版同一句 `LogError`，`.c:90-91`）");
                AbortWithVictory($"{FallbackClockTimeLimit * 3f:F0} 秒之后行动方（座位 {who}）又 "
                               + $"{InBattleFallbackAttempts} 拍没换过手（原版 "
                               + "`CancelMatchWithVictory(\"InBattleFallbackCountdown\")` ⇒ 判本机胜）");
            }
        }

        /// <summary>对面投降 —— 原版 `BattleManager.ReceiveEnemyForfeit`
        /// （`BattleManager__ReceiveEnemyForfeit.c:19-37`，逐句四步）：
        /// ① `StopCoroutine(DAT_184281920)` + `StopCoroutine(DAT_1842cbea0)` —— 两个 DAT 与
        ///    `ShowDisconnectionPopup.c:27`（`ForfeitDisconnectedEnemy`）/ `DisconnectedDuringBattle.c:74-89`
        ///    （`WaitOpponentLastActions`）交叉认出 ⇒ 停「等对面重连」与「等对手最后动作」两处；
        /// ② 连接状态 ≠ **4 = `EnemyForfeit`** ⇒ 广播 `OnConnectionStatusChanged(prev,4)` 再置 4；
        /// ③ `matchFinished(+0x2cc) = true`；
        /// ④ `DeadHero(this, 0, 2)` —— `0` = **对面死** ⇒ 本机胜，码 2 = `Forfeit`（与 `RuleCore` 的 `BattleResult` 同值）。
        /// <para>⚠️ 第 ② 步在本机**没有对应格**（`NetState` 没有「对面投降」这个状态：投降直接判负 + 收局，
        /// 见 `ReconnectCountdownExpired` 那段长注释）—— 如实记，⛔ 不造一个走不到的状态。</para></summary>
        void AcceptPeerForfeit()
        {
            if (_peerForfeited) return;
            _peerForfeited = true;
            Debug.Log("[Net] 对面投降了（`NetKind.Resign` ⇒ 理由码 `Forfeit`(2) = 原版 `ReceiveEnemyForfeit.c:37`"
                    + " 的 `DeadHero(param_1, 0, 2)`）");
            // ① 停两处协程（= `CancelReconnectCountdown`：掉线倒计时 + 收尾等待窗口）
            CancelReconnectCountdown(hideWindow: _peerGone || _countdownOn);
            // ② 连接状态置 4（EnemyForfeit）—— ⚠️ 本机这一格不存在，如实记在 doc 里
            // ③ matchFinished = true（原版 `+0x2cc`；它正是 `ShouldWaitForOpponentLastActions` 的第一个条件）
            PeerMatchFinished = true;
            // ④ 判对面负（理由码 `Forfeit`(2)）；仍然走既有记账口 `INetBattleHost.NetRemoteResign`
            if (_d != null) _d.NetRemoteResign(BattleResult.Forfeit);
            else
                Debug.LogWarning("[Net] NET/BYE-R7/no-host 收到对面投降，但**本机没有对局宿主**（`_d == null`）"
                               + " ⇒ 判负这一跳走不了，这一局不会结束（真 Play 看到它就是缺陷）");
        }

        /// <summary>对面说「我这局打完了」—— 原版 `BattleManager.ReceiveMatchFinished`
        /// （`BattleManager__ReceiveMatchFinished.c:16-19`）：**只**把 `matchFinished(+0x2cc)` 置 1，
        /// 没有别的副作用；重复收到就是再置一次 1（幂等）。
        /// ⚠️ 它**不判胜负** —— 只用来开「等对手最后几条动作」那个窗口。</summary>
        void AcceptPeerMatchFinished()
        {
            if (PeerMatchFinished) return;                       // 幂等（原版重复收也只是再置一次 1）
            PeerMatchFinished = true;
            Debug.Log("[Net] 对面说「他打完了」（`match.finished`）⇒ 收尾窗口的等待条件已成立");
            if (_d is INetFoePresentationHost h) h.NetRemoteMatchFinished();
            else Debug.Log("[Net] 宿主没实现 `INetFoePresentationHost` ⇒ 不额外做什么"
                         + "（标志已置，`TickFinishWait` 那一跳照常生效）");
        }

        /// <summary>收到主机一条 `applied`（不管是不是本机自己那条）—— 两件事，都在
        /// `MulliganFallbackCountdown` 的**退出条件**上（`.c:61-72`）：
        /// ① `mulligan(actor=对面)` ⇒ 原版 `+0x29c`（「我收到对面换牌了」）置真；
        /// ② `mulliganDone` ⇒ 换牌阶段定序完成 = 原版 `ReceiveMulliganAck` 置的 `+0x2a8`（我们的 ack 就是这条定序结果）。
        /// ⛔ 这里**不改局面**（落地是 `case NetKind.Applied` 自己的事）。</summary>
        void NoteApplied(MsgAction m)
        {
            if (m == null) return;
            if (m.kind == NetActionKind.Mulligan && m.actor == RemoteSeat) _peerMulliganSeen = true;
            if (m.kind == NetActionKind.MulliganDone)
                NoteMulliganResolved("收到主机广播的定序结果（`applied` mulliganDone）");
        }

        /// <summary>原版 `BattleManager.DisconnectedDuringBattle` 的第三支
        /// （`…__DisconnectedDuringBattle.c:74-89`）：**对手掉线**时，若「对手已经 `matchFinished`」或
        /// 「动作队列里还有动作」⇒ `StopCoroutine` + `StartCoroutine(WaitOpponentLastActions)`（= 重启收尾等待窗口）
        /// 然后 **return** —— ⛔ **不弹掉线窗、也不起弃权倒计时**，等窗口收尾那一跳再走（见 <see cref="OnFinishWaitEnded"/>）。
        /// <para>返回真 = 这一跳被延后了（调用方应立刻 `return`）。
        /// 🔴 **只延一次**（`_peerLastActionsWaited`）：原版那两个协程的起停是配对的，延完还延就等于永远不判弃权。
        /// ⚠️ 原版这一支只看 `param_2 == 0`（对手掉线）⇒ 本机掉线那一档不延（用 `IAmDisconnectedEnd` 那个自拟判据）。</para></summary>
        bool DeferForPeerLastActions()
        {
            if (IAmDisconnectedEnd) return false;                 // 原版这一支只管「对手掉线」
            if (_peerLastActionsWaited) return false;             // 只延一次
            if (_d == null || _d.Ctx == null || _d.Ctx.IsOver) return false;
            // ⚠️ 代理量：与 `TickFinishWait` 用**同一个**口径（`Session.Inbox.Count`），⛔ 不另造第二份
            int pending = (_s != null && _s.Inbox != null) ? _s.Inbox.Count : 0;
            if (!PeerMatchFinished && pending <= 0) return false;
            _peerLastActionsWaited = true;
            BeginFinishWait();                                    // 原版 `StopCoroutine` + `StartCoroutine(WaitOpponentLastActions)`
            Debug.Log($"[Net] 对手掉线，但它已经说过打完 / 队列里还有 {pending} 条 ⇒ 按原版**先重启收尾等待窗口**，"
                    + "暂不弹掉线窗、也不起弃权倒计时（判据 = `DisconnectedDuringBattle.c:74-89`）");
            return true;
        }

        /// <summary>原版 `WaitOpponentLastActions` 的**收尾支**
        /// （`BattleManager._WaitOpponentLastActions_d__316__MoveNext.c:47-65`）：窗口结束时若**还在对局中** ⇒
        /// `matchFinished(+0x2cc) = 0` + `ShowDisconnectionPopup(false)`。
        /// <para>我们只在「对手确实不在了」（`_peerGone`）时走这一跳 —— 那条协程在原版就是**只从掉线链启动**的
        /// （`DisconnectedDuringBattle` 那两支）；我们额外从 `SendMatchFinished` 那条开窗，那一路不该按掉线收尾。</para></summary>
        void OnFinishWaitEnded()
        {
            if (!_peerGone) return;
            if (_d == null || _d.Ctx == null || _d.Ctx.IsOver) return;   // 原版那道 `IsDuringBattle()` 闸
            PeerMatchFinished = false;                                   // 原版 `+0x2cc = 0`
            HandlePeerLost();                                            // = 原版 `ShowDisconnectionPopup(false)`
        }

        /// <summary>自检读口：`DeferForPeerLastActions` 那一跳走过没有（原版那两个协程的起停配对）。</summary>
        public bool PeerLastActionsWaitedForTest { get { return _peerLastActionsWaited; } }
    }
}
