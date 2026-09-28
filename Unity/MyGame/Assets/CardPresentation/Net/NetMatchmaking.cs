// NetMatchmaking.cs — 联机的**开局那条链**（N3）：`Battle!` → 两边交卡组 → 主机开局 → 两端进同一场
//
// 判据 → `资料/联机P2P_设计与交接.md` §六 N3。
//
// 🔴 **只在「联机已连上」时才接管**：`NetRuntime.Session` 活着且握手过了（`Lobby`/`InBattle`），
//    否则 `TryStart` 返回 `false`，调用方照旧跑原来那条「12 秒等 bot」的链
//    （单机行为**一字不改** —— 这条是底线）。
//
// 🔴 **主机干三件事**（别处别再算一遍）：
//   ① 抽**种子**（`DateTime.Now.Ticks`，与原版「建房那一下抽一枚硬币」同性质）；
//   ② 洗**两副牌的顺序**（`RuleCore.NewBattle` 按座位顺序抽随机数洗牌 ⇒ 两端镜像跑会洗出不同的牌堆，
//      所以顺序必须由一边定好发过去，见 `NetBattle` 文件头）；
//   ③ 定**先手**与**战场**（先手 = `BattleDriver.FirstSeatForSeed`，战场 = 主机督军阵营那一场）。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RuleEngine;

namespace CardPresentation.Net
{
    public static class NetMatchmaking
    {
        // 本机提交的那份
        static PlayerDeck _myDeck; static string _myMode, _myFaction, _myName;
        // 对面交上来的（主机收）
        static PlayerDeck _foeDeck; static string _foeMode, _foeFaction;
        static bool _started;

        /// <summary>本机是不是正在等对面交卡组（界面可以据此显示「等待对手…」）。</summary>
        public static bool Waiting { get { return _myDeck != null && !_started && _foeDeck == null; } }

        // ==================================================================
        //  「配到的是谁」—— 给界面用（`SearchingOpponentWindow` 的「找到对手」那一态）
        //  判据 → `资料/阶段二_多人界面_原版规格.md` **§6·4**
        //  🔴 原版那扇窗的「找到对手」这一态**在本 build 里是死代码**（`OpponentFound` 零调用点）
        //     ⇒ **「什么时候显示」没有原版可抄，是我们定的**；下面这些字段只是把「对面是谁」摆出来。
        // ==================================================================

        /// <summary>配到人了没有（`= 对面那副牌到手了`）。</summary>
        public static bool HasOpponent { get { return _foeDeck != null; } }

        /// <summary>对面那副牌（**主机侧** = 客机交上来的；**客机侧** = `MsgStart.hostDeckJson`）。</summary>
        public static PlayerDeck FoeDeck { get { return _foeDeck; } }

        /// <summary>对面那副牌的阵营（主机侧 = `MsgDeck.faction`；客机侧 = `MsgStart.hostFaction`）。</summary>
        public static string FoeFaction { get { return _foeFaction; } }

        /// <summary>本机那副牌（自己那副；`SearchingOpponentWindow` 的「我方格」本来就从收藏里取，
        /// 这里给出来是为了**两端口径对称**、也给自检一个读口）。</summary>
        public static PlayerDeck MyDeck { get { return _myDeck; } }

        /// <summary>本机的名字（= 机器名，见 `TryStart`）。</summary>
        public static string MyName { get { return _myName; } }

        /// <summary>🔴 **对手的名字**。⚠️ **我们显示的是【机器名】** —— 原版那行显示的是服务端账号 id
        /// （`playFabId`），**本地没有对等物**（判据 → 正本 §6·4 第 4 条）⇒ **这是我们挑的，不是复刻**。
        /// 主机侧来自握手包（`NetSession.PeerName`）；客机侧来自 `MsgStart.myName`。</summary>
        public static string FoeName { get; private set; }

        /// <summary>
        /// 🔴 **切战场之前的那一口气** —— 给界面把「找到对手」展示出来用的。
        /// 界面注册它：返回 `true` = **界面接管这一次切场景**，它自己展示完再调 <see cref="GoNow"/>。
        /// **没注册就照旧立刻切**（单机、以及没开那扇窗的入口，行为一字不改）。
        ///
        /// ⚠️ **这是我们挑的**：「什么时候显示、显示多久」原版没有可抄的（见上面那张判据）。
        /// 时长照原版 `MatchMakerManager.StartBattleWithDelay` 的 `waitLoadTime`（默认 **1s**）取，
        /// 但**「拿它当展示时间」是我们加的**。
        ///
        /// ⚠️ 注册方**必须在关窗/销毁时清掉它**，否则这一局会卡在「切不了场景」上。
        /// </summary>
        public static Func<MsgStart, bool> HoldForPresentation;

        /// <summary>界面展示完了 ⇒ 真的切场景（配 <see cref="HoldForPresentation"/> 用）。</summary>
        public static void GoNow()
        {
            var pb = NetPendingBattle.Current;
            if (pb == null)
            {
                Debug.LogError("[Net] 没有待进的开局包 —— 不切场景（这是缺陷，不是正常路径）");
                return;
            }
            // ⚠️ 批处理里**不能真切场景**（自检会手工调它来验「展示完就放行」这条链）
            if (Application.isBatchMode) { Debug.Log("[Net] （批处理：不切场景，只记账）"); return; }
            Debug.Log($"[Net] 展示完毕 ⇒ 切战场 `{pb.Arena}`");
            SceneManager.LoadScene(pb.Arena);
        }

        /// <summary>清干净（换角色 / 断开 / 打完一局都要调 —— 不然下一局会带着上一局那副牌）。</summary>
        public static void Reset()
        {
            _myDeck = null; _foeDeck = null;
            _myMode = _foeMode = null; _myFaction = _foeFaction = null; _myName = null;
            FoeName = null;
            _started = false;
            // ⚠️ 那句「切场景交给界面」也一起清掉：**清在这儿是安全的** —— `Reset()` 只在这两处调：
            //    `TryStart`（点 `Battle!` 那一刻，界面**还没开**）与 `BattleDriver.OnDestroy`。
            //    留着它而界面又没开 ⇒ 这一局会卡在「切不了场景」（静默失败）。
            HoldForPresentation = null;
        }

        /// <summary>
        /// 点 `Battle!` 时调。**返回 true = 这一局走联机**（调用方别再跑 12 秒 bot 链）。
        /// 返回 false 时 `why` 说明为什么没接管（没连上 / 不在大厅状态），**照旧打 bot**。
        /// </summary>
        public static bool TryStart(PlayerDeck deck, string mode, string faction, out string why)
        {
            why = null;
            var rt = NetRuntime.Instance;
            var s = rt != null ? rt.Session : null;
            if (s == null || !s.Transport.IsConnected) { why = "联机没连上"; return false; }
            if (s.State != NetState.Lobby && s.State != NetState.InBattle)
            { why = $"联机会话现在是 `{s.State}`（还没握手完）"; return false; }
            if (deck == null) { why = "这副牌是空的"; return false; }

            Reset();
            _myDeck = deck; _myMode = mode ?? "Classic"; _myFaction = faction;
            // 🔴 **2026-09-28 收口**：原来这里自己取 `Environment.MachineName`（**第二份名字**）——
            //    用户当天把显示名定成「玩家123」这类占位名，联机层报的必须是**同一个名字**
            //    （判据 = `ProfileData.PlayerName` 的注释：「别再在别处写第二份」）。
            //    ⚠️ **两边都没改过名时会同名** —— 玩家档案窗的改名窗可以改。
            _myName = ProfileData.PlayerName;
            bool host = s.Role == NetRole.Host;

            // 🔴 两边都要**显式说清自己在等什么**（红线：不许静默）
            Debug.Log($"[Net] 已进入联机匹配：本机是{(host ? "**主机**" : "客机")}，交了卡组「{deck.Name}」"
                    + $"（{mode} · {faction}）—— 等对面也点 `Battle!`");
            if (host)
            {
                // 主机：对面可能已经交了（它先点的），那就立刻开局
                if (_foeDeck != null) HostStartMatch();
            }
            else
            {
                s.Send(NetKind.Deck, new MsgDeck
                {
                    deckJson = JsonUtility.ToJson(deck),
                    gameMode = mode,
                    faction = faction,          // 主机要用它定战场（`PlayerDeck` 自己不带阵营字段）
                });
            }
            return true;
        }

        /// <summary>「联机**没**接管这一局」时该不该跟玩家说一声（**红线**）。
        /// ⚠️ **只在「配过联机」时才说** —— 单机玩家不该被打扰：
        /// 「没连上就照旧打 bot」是**设计好的**行为，不是错误（`项目任务.md` §三 第 14 条 表 第 5 条）。
        /// 但**配了联机却没连上**时不说，玩家会**无声无息地打了个 bot 还以为是真人**。</summary>
        public static void ExplainNotTakingOver(string why)
        {
            var cfg = NetConfig.Current;
            if (cfg == null || cfg.role == (int)NetRole.Off) return;   // 没配过联机 ⇒ 单机，不打扰
            NetRuntime.Notice("这一局**打的是电脑，不是联机**。\n"
                            + "原因：" + why + "。\n"
                            + "你在设置里配过联机了 —— 请到「设置 → 联机」点一次"
                            + (cfg.role == (int)NetRole.Host ? "【保存】" : "【检查连接】")
                            + "，再回来点 `Battle!`。");
        }

        /// <summary>`NetRuntime.Update` 调（**对局外的**联机消息都在这儿处理）。</summary>
        public static void PumpLobby()
        {
            var rt = NetRuntime.Instance;
            var s = rt != null ? rt.Session : null;
            if (s == null) return;
            for (int i = 0; i < s.Inbox.Count; i++)
            {
                var f = s.Inbox[i];
                switch (f.kind)
                {
                    case NetKind.Deck:                                  // 只有主机收
                    {
                        var m = NetProtocol.Unpack<MsgDeck>(f.payload);
                        if (m == null || string.IsNullOrEmpty(m.deckJson)) { Debug.LogWarning("[Net] 收到空的卡组包"); break; }
                        _foeDeck = JsonUtility.FromJson<PlayerDeck>(m.deckJson);
                        _foeMode = m.gameMode;
                        _foeFaction = m.faction;
                        // 🆕 主机在这一刻就知道「对面是谁」了（握手包里带着对方的名字）
                        //    ⇒ `SearchingOpponentWindow` 的「找到对手」那一态有东西可填。
                        FoeName = s.PeerName;
                        Debug.Log($"[Net] 对手交来卡组「{_foeDeck.Name}」（{_foeMode} · {_foeFaction}）"
                                + (string.IsNullOrEmpty(FoeName) ? "" : $"· 对面是「{FoeName}」"));
                        if (_myDeck != null) HostStartMatch();
                        else Debug.Log("[Net] 主机还没点 `Battle!` ⇒ 先记着，等我方也交");
                        break;
                    }
                    case NetKind.Start:                                 // 只有客机收
                    {
                        var st = NetProtocol.Unpack<MsgStart>(f.payload);
                        if (st == null) break;
                        // 🆕 客机在这一刻才知道「对面是谁」（大厅阶段拿不到，只有主机收得到 Deck）。
                        //    ⚠️ 这几个字段都是**主机视角**的名字：`myName` 是主机的名字、`hostDeckJson`
                        //    是主机那副牌 —— 对客机来说它们正是「对面」。
                        if (!string.IsNullOrEmpty(st.hostDeckJson))
                            _foeDeck = JsonUtility.FromJson<PlayerDeck>(st.hostDeckJson);
                        _foeFaction = st.hostFaction;
                        FoeName = st.myName;
                        Debug.Log($"[Net] 主机开局：种子 {st.seed} · 模式 {st.mode} · 战场 {st.arena} · "
                                + $"先手 = {(st.hostFirst == 0 ? "主机" : "客机")}"
                                + (string.IsNullOrEmpty(FoeName) ? "" : $" · 对面是「{FoeName}」"));
                        GoToBattle(st);
                        break;
                    }
                    default:
                        // ⚠️ 走到这里说明「这条消息不该在大厅阶段出现」—— 出声（红线：不许静默）
                        Debug.LogWarning("[Net] 大厅阶段收到不认识的消息：" + f.kind);
                        break;
                }
            }
            s.Inbox.Clear();
        }

        // ==================================================================
        //  主机：开局
        // ==================================================================

        static void HostStartMatch()
        {
            var s = NetRuntime.Instance != null ? NetRuntime.Instance.Session : null;
            if (s == null) return;

            // ① 模式必须一致（首版只做经典；两端都带遭遇牌也允许 —— 同一套引擎，但**要出声**）
            string mode = _myMode ?? "Classic";
            if (!string.IsNullOrEmpty(_foeMode) && _foeMode != mode)
            {
                Debug.LogError($"[Net] 🔴 两端模式不一样（我 {mode} / 对面 {_foeMode}）—— 拒绝开局，别打出两端不一致的账");
                // 🔴 **红线**：原来只有这行日志 ⇒ 两边都卡在「正在搜索」上、**谁也不明白为什么开不了**。
                //    判据 → `项目任务.md` §三 第 14 条 表里的第 4 条。
                NetRuntime.Notice("两边选的模式不一样：本机是「" + mode + "」，对面是「" + _foeMode + "」。\n"
                                + "这一局没有开成 —— 请两位换成**同一个模式**，再各自点一次 `Battle!`。");
                return;
            }

            // ② 种子（**主机抽、下发给客机** —— 原版也是这么做的，见正本 §四·4）
            int seed = unchecked((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
            // ③ 先手（**我们一律投硬币** —— 用户 2026-09-26 拍板）；⚠️ 这是**绝对座位号**（0 = 主机）
            int firstSeat = BattleDriver.FirstSeatForSeed(seed);
            // ④ 战场 = **主机的督军阵营那一场**（正本 §二·5：这是我们的选择，原版取谁的 army 本地判不出）
            string arena = ArenaByArmy.SceneFor(_myFaction);

            var start = new MsgStart
            {
                seed = seed,
                mode = mode,
                arena = arena,
                hostFirst = firstSeat,
                hostFaction = _myFaction,
                clientFaction = _foeFaction,
                myName = _myName,
                // 🆕 主机的「对面」= 握手包里的机器名（原来写死 `"Opponent"`）。
                //    ⚠️ 这不只是给那扇窗看的：`NetPendingBattle.FoeName` 也吃它（目前全仓没人读，
                //    但留着假名字迟早有人当真名用）。原版这里是**服务端账号 id** —— 我们只有机器名。
                foeName = string.IsNullOrEmpty(FoeName) ? "Opponent" : FoeName,
                // ⚠️ **两副牌原样发**（不洗）—— 两端跑同一套绝对座位 + 同一个种子 ⇒ 各洗各的也必然一致。
                //    第一版是「主机洗好顺序再发」（因为那时两端是**镜像**的）；镜像那套已作废，见
                //    `NetProtocol.Fingerprint` 的注释。
                hostDeckJson = JsonUtility.ToJson(_myDeck),
                clientDeckJson = JsonUtility.ToJson(_foeDeck),
            };
            s.Send(NetKind.Start, start);
            Debug.Log($"[Net] 主机开局：种子 {seed} · 先手座位 {firstSeat}（{(firstSeat == 0 ? "主机" : "客机")}）· "
                    + $"战场 {arena} · 两副牌 {_myDeck.CardIds.Count}/{_foeDeck.CardIds.Count} 张已下发"
                    + "（**不洗牌**：两端同种子同输入 ⇒ 洗出来的顺序必然一样）");
            GoToBattle(start);
        }

        /// <summary>两端都走这一条：把开局参数放进跨场景的静态槽，然后切到那一场。</summary>
        static void GoToBattle(MsgStart st)
        {
            _started = true;
            bool host = NetRuntime.Instance != null && NetRuntime.Instance.Session != null
                     && NetRuntime.Instance.Session.Role == NetRole.Host;
            var pb = NetPendingBattle.FromStart(st, host);
            if (pb == null)
            {
                Debug.LogError("[Net] 开局包解不出来 —— 不切场景");
                NetRuntime.Notice("开局参数没能解析出来，这一局开不了。\n请两边都退回主菜单，重新点一次 `Battle!`。");
                return;
            }
            NetPendingBattle.Current = pb;
            Debug.Log($"[Net] 进战场 `{pb.Arena}`（本机座位 {pb.MySeat}，先手座位 {pb.FirstSeat}"
                    + $"（{(pb.FirstSeat == pb.MySeat ? "我" : "对面")}））");
            if (Application.isBatchMode) { Debug.Log("[Net] （批处理：不切场景，只把开局参数放进静态槽）"); return; }
            // 🆕 切场景之前给界面一口气：把「找到对手」那一态显示出来（**只有注册过的界面才拦**）。
            //    未注册 / 批处理 ⇒ 照旧立刻切（**单机与三个战斗入口窗的行为一字不改**）。
            if (HoldForPresentation != null && HoldForPresentation(st))
            {
                Debug.Log("[Net] 切场景交给界面 —— 先把「找到对手」显示出来，展示完由它调 `NetMatchmaking.GoNow()`");
                return;
            }
            SceneManager.LoadScene(pb.Arena);
        }

        // ==================================================================
        //  小工具
        // ==================================================================

        // ⚠️ 这里原来有个 `Shuffled(PlayerDeck, seed)`（主机洗好两副牌的顺序再下发）—— **删掉了**：
        //    那套是给**镜像**端点的补丁（两端各洗各的会洗出不同牌堆）。现在两端跑**同一套绝对座位**
        //    ⇒ 同种子同输入，各洗各的必然一致。教训 → `NetProtocol.Fingerprint` 的注释。
    }
}
