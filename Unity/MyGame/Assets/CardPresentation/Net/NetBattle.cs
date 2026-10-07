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
            if (_s == null) return;
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

        /// <summary>对面**掉线**了（心跳超时 / 连接断了）。判据全文见本节头部那一段。</summary>
        void HandlePeerLost()
        {
            _peerGone = true;
            StartReconnectCountdown();          // 🔴 A900：先起倒计时（第一拍的数字要进下面那两句文案）
            ClockPaused = true;                 // 🔴 A901：对局时钟停走（原版 `ClockManager.PauseClock()`）
            SayPeerGone(ReconnectHint(_countdownLeft), ReconnectPopup(_countdownLeft));
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
                    + " 「对面不会回来了」这一档由我们判；落地 = `NetRemoteResign()` → `RuleCore.Forfeit(Ctx, 1-_me)`）");
            _d.NetRemoteResign();
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
        //   · 判负入口 = **`INetBattleHost.NetRemoteResign()`**（现成的，`BattleDriver` 那一份 =
        //     `RecRaw(RecKindForfeit)`【= 原版 `AddResignAction`】+ `RuleCore.Forfeit(Ctx, 1-_me)`）。
        //     ⚠️ 原版到点是 `DeadHero(对面, 3)`、对面自己点投降是 `DeadHero(对面, 2)` ——
        //     我们引擎的 `RuleCore.Forfeit` **没有「理由码」这一个参数**，两档走同一个口
        //     （**这是差异，如实记在报告里**；效果相同 = 立刻判对面负、不看督血）。
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
        /// `StopCoroutine` + `StartCoroutine(ForfeitDisconnectedEnemy)` —— **重开**，不是叠加）。</summary>
        void StartReconnectCountdown()
        {
            _countdownLeft = ReconnectSecondsFor(_enemyDisconnects);
            _enemyDisconnects++;                       // 原版 `:63`：**算完就自增**（先取值再 ++）
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
            if (!_countdownOn) return;
            while (_countdownOn && NowSec() >= _countdownNextAt)
            {
                _countdownNextAt += 1f;
                _countdownLeft--;
                if (_countdownLeft <= 0) { ReconnectCountdownExpired(); return; }
                ReportReconnectStatus(_countdownLeft);
            }
        }

        /// <summary>原版 `UpdateReconnectStatus(secLeft)` 那一拍（`BattleManager+0x1B0` `Action<int>`）——
        /// 它接的是 `BattleErrorUIManager__UpdateReconnectWindow.c`：**把秒数拼进弹窗正文**。
        /// 我们这边两个口都刷：**提示行**（真 Play 与自检都看得见）+ **弹窗正文**（只有真 Play 验得到）。</summary>
        void ReportReconnectStatus(int sec)
        {
            if (_d != null) _d.NetSay(ReconnectHint(sec));
            NetRuntime.UpdateReconnectPopup(ReconnectPopup(sec));
        }

        static string ReconnectHint(int sec)
        {
            return $"对手掉线了 —— 正在等他回来（{sec} 秒后判他弃权）";
        }
        static string ReconnectPopup(int sec)
        {
            return "对手掉线了 —— 正在等他回来。\n"
                 + $"（对局已暂停：对面回来就能接着打。{sec} 秒之内没回来，就判他弃权。）";
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
            Debug.Log($"[Net] 对手掉线超过 {sec} 秒没回来 ⇒ **判对面弃权**"
                    + "（原版 `…_d__322__MoveNext.c:143-144`：`AddResignAction` + `DeadHero(对面, 3)`；"
                    + "我们的对应物 = `NetRemoteResign()` → `RuleCore.Forfeit(Ctx, 1-_me)`）");
            if (_d != null) _d.NetSay($"对手掉线超过 {sec} 秒还没回来 —— 判他弃权，这一局你赢了");
            _d.NetRemoteResign();
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
            ClockPaused = false;
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
                    if (m == null) return;
                    if (_d.Ctx == null) return;
                    // 🆕 2026-09-30：**进攻卡 / 防御卡**那两条发生在开打之前 ⇒ 「不是你的回合」那道
                    //   守卫对它们不适用（那时候还没轮到谁）。其余动作照旧拦。
                    if (!NetApply.IsEnvPick(m) && _d.Ctx.Active != RemoteSeat)
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
