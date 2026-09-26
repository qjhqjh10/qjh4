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

        /// <summary>清干净（换角色 / 断开 / 打完一局都要调 —— 不然下一局会带着上一局那副牌）。</summary>
        public static void Reset()
        {
            _myDeck = null; _foeDeck = null;
            _myMode = _foeMode = null; _myFaction = _foeFaction = null; _myName = null;
            _started = false;
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
            _myName = Environment.MachineName;
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
                        Debug.Log($"[Net] 对手交来卡组「{_foeDeck.Name}」（{_foeMode} · {_foeFaction}）");
                        if (_myDeck != null) HostStartMatch();
                        else Debug.Log("[Net] 主机还没点 `Battle!` ⇒ 先记着，等我方也交");
                        break;
                    }
                    case NetKind.Start:                                 // 只有客机收
                    {
                        var st = NetProtocol.Unpack<MsgStart>(f.payload);
                        if (st == null) break;
                        Debug.Log($"[Net] 主机开局：种子 {st.seed} · 模式 {st.mode} · 战场 {st.arena} · "
                                + $"先手 = {(st.hostFirst == 0 ? "主机" : "客机")}");
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
                foeName = "Opponent",
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
            if (pb == null) { Debug.LogError("[Net] 开局包解不出来 —— 不切场景"); return; }
            NetPendingBattle.Current = pb;
            Debug.Log($"[Net] 进战场 `{pb.Arena}`（本机座位 {pb.MySeat}，先手座位 {pb.FirstSeat}"
                    + $"（{(pb.FirstSeat == pb.MySeat ? "我" : "对面")}））");
            if (Application.isBatchMode) { Debug.Log("[Net] （批处理：不切场景，只把开局参数放进静态槽）"); return; }
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
