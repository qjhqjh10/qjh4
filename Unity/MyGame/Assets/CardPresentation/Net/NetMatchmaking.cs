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
        //  🆕 2026-10-03：**取消这一局的匹配**（`项目任务.md` §三 第 29 条 **A1**）
        //
        //  🔴 **原来是什么样**：点 `Cancel` **只关窗、不拆局** —— 配对已经成了，对面照样开局，
        //     `MsgStart` 一到还是会被拉进战场（`RankedEventWindow.CancelSearch` 里只是如实出声）。
        //
        //  🔴 **我们怎么定的**（⚠️ **不是复刻**）：原版那是**服务端撤单**（`MatchMakerManager.CancelSearch`），
        //     P2P 没有服务端 ⇒ 自己发一条 `match.cancel`。
        //     语义 = **「大厅阶段」内有效**：
        //       · 还没交换完卡组 / 还没发 `MsgStart` ⇒ 两边一起退回大厅，**不开局**；
        //       · 已经发/收到 `MsgStart` ⇒ **取消不了**（那一局已经成立），如实告诉玩家
        //         「要退出只能在对局里投降」—— **不假装取消成功**（红线：不许静默失败）。
        // ==================================================================

        /// <summary>本机在这一局里点过「取消」吗（主机据此丢掉迟到的 `MsgDeck`、客机据此丢掉迟到的 `MsgStart`）。</summary>
        public static bool ICancelled { get; private set; }
        /// <summary>对面取消过吗（收过 `match.cancel`）。</summary>
        public static bool FoeCancelled { get; private set; }

        /// <summary>
        /// 取消这一局的联机匹配。**返回 true = 真的取消了**（已经发出通知、本地状态已复位）；
        /// 返回 false 时 <paramref name="why"/> 说明**为什么取消不了**（调用方**必须**把它说出来）。
        /// ⚠️ 单机路径（`_myDeck == null`）也会返回 false —— 那一支本来就没有「联机匹配」可取消。
        /// </summary>
        public static bool Cancel(string reason, out string why)
        {
            why = null;
            var rt = NetRuntime.Instance;
            var s = rt != null ? rt.Session : null;
            if (s == null || !s.Transport.IsConnected) { why = "联机没连上（这一局本来就没走联机）"; return false; }
            if (_myDeck == null && !_started) { why = "这一局还没进入联机匹配"; return false; }
            if (_started)
            {
                // 开局包已经发出/收到 ⇒ 这一局成立了。**不假装取消成功**。
                why = "这一局**已经开局了**（开局包已经发出/收到）—— 取消不了；要退出请在对局里投降。";
                return false;
            }
            ICancelled = true;
            ClearMatch();
            s.Send(NetKind.MatchCancel, new MsgMatchCancel { reason = reason });
            Debug.Log("[Net] 已发出「取消匹配」—— 这一局不打了（对面也会退回大厅）");
            return true;
        }

        /// <summary>把「这一局」的账清掉（保留 `ICancelled` / `FoeCancelled` —— 它们要活到下一次 `Reset`）。</summary>
        static void ClearMatch()
        {
            _myDeck = null; _foeDeck = null;
            _myMode = _foeMode = null; _myFaction = _foeFaction = null; _myName = null;
            FoeName = null;
            _started = false;
        }

        // ==================================================================
        //  🆕 2026-10-17（B23·A902）：**大厅阶段（还没进对局）对面掉线 / 离开** —— 出声 + 撤这一局的匹配
        // ==================================================================
        //  账 `A902`：`NetMatchmaking` 原来**根本不看会话状态** ⇒ 大厅阶段对面掉了，本机**什么反应都没有**
        //  （只有设置窗那一行 `StatusText` 会变）—— 玩家会一直干等对面点 `Battle!`（**静默**，红线）。
        //
        //  原版判据（全量反编译 `d:/2/tools/decomp_full/`，2026-10-17 逐句现读）：
        //   · **触发点** = `BattleNetworkManager__EventDisconnected.c`：先
        //     `LogWarning("EventDisconnected, with battleConnectionStatus " + 状态)`（**不静默**），
        //     再按状态分派 —— **搜索/匹配那一档**（第 20 号 `searchingRandomOponent`，枚举
        //     `dump.cs:33222-33236`；它与第 80 号同落一处）走：
        //     `searchManager(0x30) != null ⇒ SearchOpponentManager.CancelSearchForDisconnect()`。
        //     其余状态各有各的落点（10 → `DisconnectedDuringBattle` · 30/50 → `LeaveBattle` ·
        //     60 → `TryReconnecting` · 100 → `FinishedLeavingBattle`），认不出的走
        //     `LogError("Unhandled battleConnectionStatus in EventDisconnected: …")`。
        //   · **那一跳干什么** = `SearchOpponentManager__CancelSearchForDisconnect.c`：
        //     ① `WindowsManager.ShowPopUp(text, localizeTexts: true, closeOnEsc: true, submitButtonText, onSubmit: null)`
        //        —— 文案键 = **`CustomErrors/InternetUnreachable`**、钮上的字 = **`MainMenu/General/OK`**
        //        （`DAT_18425d4a8` / `DAT_1842be718` 逐地址查 `d:/2/tools/il2cpp_out/stringliteral.json` 得到；
        //         读法 → `资料/全量反编译_入口与用法.md` 第 ⑤ 条。⚠️ 这两个键在本地**没有词条表**
        //         （表在远端 CCD）⇒ 我们用自己的措辞 —— 与 B13 那条先例同一处理）。
        //     ② `Everguild.MatchMakerManager__CancelSearch`（`MatchMakerManager__CancelSearch.c`）＝
        //        `SearchOpponentManager.CancelBattleSearch()`（→ `BattleNetworkManager.CancelBattleSearch`
        //         把状态置回 0 + `LeaveCurrentRoom`）＋ 叫一次 `OnSearchCancelled`。
        //     ⇒ 原版的反应 = **弹窗 + 把这一局撤掉回大厅**，两件事一起做。
        //   · ⚠️ **「对手自己断开」那一路原版没有专属处理**：`BattleNetworkManager__EventPlayerDisconnected.c`
        //     对 20/30 这两个状态落 `LogError("Unhandled battleConnectionStatus in EventPlayerDisconnected: …")`
        //     （只有 10 = 对局中才 `SetOpponentDisconnected`）—— 因为原版是**服务端匹配**：搜索阶段
        //     根本没有「对手」这个实体挂在你这条连接上。我们是 P2P、对面就是那条连接 ⇒ **这一档
        //     是本工程自己的口径**（与 `Cancel` 那条同性质），照「弹窗 + 提示行 + 日志 + 撤匹配」落地。
        //
        //  🔴 **闸：对局中不抢话**（本件 ③ 那条要求）。判据 = `NetRuntime.LobbyHandled` —— 它**本来就是**
        //     「这一台会话这一帧归谁读」那个开关（`NetRuntime.Update` 用它决定跑不跑 `PumpLobby`；
        //     `NetBattle.Attach` 置 false、`BattleDriver.OnDestroy` 置回 true）⇒ 大厅这一半与
        //     `NetBattle`（对局那一半）**天然互斥**，一台会话上只会有一边说话。
        //  ⚠️ **「已经开局」（开局包已发/收）那一档不由这里弹**：那一局已经成立，接下来的出口是
        //     进战场 → `NetBattle` 接上来 —— 它会在接上那一刻补报（见 `NetBattle.WireSession` 末尾那段）。
        //     两边都弹的话，玩家一局里会被弹两次（那正是 ③ 要挡的）。

        /// <summary>把「大厅阶段对面掉线 / 离开」这条接线挂到会话上（幂等；换会话时自动摘掉旧的那台）。
        /// 调用点 = `NetRuntime.Init` / `Reset` / `AttachForTest`（**会话每一次出生都经过那三处**）。
        /// ⛔ 别改成「第一次 `PumpLobby` 时懒挂」：掉线是在 `NetRuntime.Update` 的 `Session.Pump()`
        /// 里判出来的，那一帧 `PumpLobby` 还没跑 ⇒ 懒挂会**整帧丢掉第一次掉线**（而第一次往往就是唯一一次）。</summary>
        public static void WireLobby(NetSession s)
        {
            if (ReferenceEquals(_wired, s)) return;
            if (_wired != null) { _wired.OnPeerLost -= HandleLobbyPeerLost; _wired.OnClosed -= HandleLobbyPeerClosed; }
            _wired = s;
            if (s != null) { s.OnPeerLost += HandleLobbyPeerLost; s.OnClosed += HandleLobbyPeerClosed; }
            // 换会话 = 换了一台对端 ⇒ 那个「对面不在」的边沿**作废**。
            // 🔴 不在这儿清会**静默地错**：`_peerGone` 一直挂着的话，下一台会话一握手到 `Lobby`，
            //    `PumpLobby` 那条「回来了 ⇒ 撤销」就会**对着一个从来没掉过线的对端**说「回来了」，
            //    还会顺手 `HideNoticePopup()` 把玩家这一刻自己开着的那扇窗关掉。
            _peerGone = false; _peerGonePopup = false;
            Debug.Log(s == null
                ? "[Net] 大厅掉线接线：已摘掉（会话换掉了）"
                : "[Net] 大厅掉线接线：已挂在当前会话上（对面掉线/离开 ⇒ 出声 + 撤这一局的匹配）");
        }

        static NetSession _wired;

        /// <summary>大厅阶段「对面不在了」这个状态的**边沿**：掉线 / 离开那一刻置真，
        /// 观察到他回到 `Lobby`（`PumpLobby` 里）那一刻清掉。自检读它。</summary>
        public static bool PeerGone { get { return _peerGone; } }
        static bool _peerGone;
        /// <summary>那一次「对面不在了」**真的弹过窗**（撤销时据此决定要不要去撤窗 ——
        /// 没弹过就不许撤，否则会把玩家这一刻自己开着的那扇窗关掉）。</summary>
        static bool _peerGonePopup;

        /// <summary>联机层最后一次要对玩家说的那句话（提示行）。自检读它；
        /// 🆕 **2026-10-17（B27·A925）：界面的消费方已接上** = `Shell/SearchingMatchPopup.ShowHint`
        /// （四扇战斗入口窗共用的那扇 `Searching Oponent Popup` 的 `Main Search message` 那一行）。</summary>
        public static string LastHint { get; private set; }
        /// <summary>大厅那一刻那行提示的钩子（**推**，不是拉）：`Shell/SearchingMatchPopup` 在 `OnEnable` 订、
        /// `OnDisable` / `OnDestroy` 摘。⛔ 别在这儿弹窗（弹窗是另一件事，走 `NetRuntime.Notice`）。
        /// ⚠️ 传 `null` = **收回那行字**（`Reset()` 会这么叫一次）—— 订方要当成「把这行清掉」，不是「多说了一句空话」。</summary>
        public static Action<string> OnHint;

        /// <summary>🔴 **闸**：这一帧这台会话归谁说话（大厅这一半 / 对局那一半）。
        /// 判据 = `NetRuntime.LobbyHandled`（理由见本节头部）。
        /// ⚠️ 没有 `NetRuntime`（批处理里那些裸会话夹具）⇒ **不说** —— 生产路径上大厅这一半本来就
        /// 只在 `NetRuntime` 活着时才挂得上来（`Init`/`Reset`/`AttachForTest` 都是它调的）。</summary>
        static bool LobbyOwnsSession
        {
            get { var rt = NetRuntime.Instance; return rt != null && rt.LobbyHandled; }
        }

        /// <summary>对面**掉线**了（心跳超时 / 连接断）。判据全文见本节头部。</summary>
        static void HandleLobbyPeerLost()
        {
            if (!LobbyOwnsSession) return;          // 对局那半边接管了 ⇒ 由它说（互斥闸）
            if (_peerGone)
            {
                // 同一段掉线里再来一次（对局那一半有同样的写法：`_peerGone` 是边沿）—— 不重复弹窗。
                Debug.Log("[Net] 大厅：对面还是没回来（又断了一次）—— 不重复弹窗");
                return;
            }
            _peerGone = true;
            if (_started) { DeferToBattleLayer("对面掉线了"); return; }
            string tail = RevokeMatchLocal();
            // 🔴 **2026-10-18（A933）压缩这一句**。原 =「联机断开了：对面掉线了 —— <tail>（对面回来之后，
            //   两边重新各点一次 `Battle!`）」= **52 / 56 字**（两条 `tail` 分支），超过提示行按框算的 40 字
            //   （`Shell/SearchingMatchPopup.HintLineMaxChars = 40`）⇒ 真跑到这一跳时每次都会 `LogWarning`。
            //   现压到 **36 / 40 字**（两条都 ≤ 40）。去掉的只是**弹窗已经说过的那半句**（「联机断开了：」
            //   与那对 em-dash）—— 两处本来就是**两个口**（判据 → `SearchingMatchPopup.ShowHint` 上头那段）。
            //   ✅ **信息一个不少**：对面掉线了 · `tail`（这一局撤没撤）· **两边**都 · 回来各点一次 `Battle!`。
            //   ⚠️ 自检读的是子串 `Contains("掉线")`（`Editor/NetSelfTest.cs` M⑧ `:538` 与 N④ `:749`）⇒ **那个词保留**。
            // ⚠️ **2026-10-18 订正（铁律 5）**：`A933` 把「47 字那句」记在 **`:409`（「对面回来了…」）**上
            //   —— **对不上**：`:409` 实测 **39 字**（本来就没越界）。🔑 按长度反查，**47 字那句实测就是
            //   下面的 `DeferToBattleLayer`**（`what` = 14 ⇒ 14 + 33 = 47，逐字吻合）⇒ A933 的**落点漂了**；
            //   真正的越界句是**两句**：下面那一句 + **本句**（52/56），现都压到 ≤ 40。
            SayLobby("对面掉线了，" + tail + "（两边回来各点一次 `Battle!`）",
                     "对面掉线了 —— 联机断开。\n" + tail + "，回到大厅。\n"
                   + "（对面回来之后，两边各自重新点一次 `Battle!`。原版那一刻走的是 "
                   + "`SearchOpponentManager.CancelSearchForDisconnect`：弹窗 + 取消搜索。）");
            _peerGonePopup = true;
        }

        /// <summary>对面**主动离开**（收到 `bye`：他关了台 / 重开主机 / 重连被拒）。判据全文见本节头部。</summary>
        static void HandleLobbyPeerClosed(string why)
        {
            if (!LobbyOwnsSession) return;          // 对局那半边接管了 ⇒ 由它说（互斥闸）
            _peerGone = true;                       // `NetSession` 收到 `bye` 就 `Close()` ⇒ 不会再「回来」（同 `NetBattle` 那条注）
            string body = string.IsNullOrEmpty(why) ? "对面退出了" : why;
            if (_started) { DeferToBattleLayer("对面离开了：" + body); return; }
            string tail = RevokeMatchLocal();
            SayLobby("联机结束：" + body + " —— " + tail,
                     "联机结束：" + body + "\n" + tail + "，回到大厅。\n"
                   + "（要再打一局：两边重新各点一次 `Battle!`。原版那一刻走的是 "
                   + "`SearchOpponentManager.CancelSearchForDisconnect`：弹窗 + 取消搜索。）");
            _peerGonePopup = true;
        }

        /// <summary>「这一局已经开局」（开局包已发/收）那一档 ⇒ **不在这儿弹窗**：
        /// 接下来的出口是进战场 → `NetBattle` 接上来时补报（见 `NetBattle.WireSession` 末尾那段，
        /// 判据 = 接上时会话不在 `Lobby`）。这里只记日志 + 那一行提示（⛔ 不静默）。</summary>
        static void DeferToBattleLayer(string what)
        {
            Debug.LogWarning("[Net] 大厅：" + what + " —— 但这一局**已经开局**（开局包已发/收）⇒ "
                           + "**不在大厅这一半弹窗**，交给对局那一层（`NetBattle` 接上来时会看到会话不在 `Lobby`）");
            // 🔴 **2026-10-18（A933）同时压缩这一句**：原 = `what` + 「 —— 这一局已经开局、正在进战场
            //   （断线那件事由对局那一层接着说）」（字面量 33 字）⇒ `what` 一长就超 40。
            //   🔑 **`A933` 说的那句「47 字」实测就是这一句**：生产最长那条 `what` = `对面离开了：` +
            //   对方报的 `对面离开了这一局` = **14 字**，14 + 33 = **47** ⇒ 每次都出声（不是 `:409` 那句）。
            //   现字面量 **24 字** ⇒ 生产路径最长那条 = **38 字**（`what` = 14），全部 ≤ 40
            //   （`what` 只有两个来源：`:184` 的「对面掉线了」= 5 · `:199` 的「对面离开了：」+ 对方报的理由）。
            //   ⚠️ 详细那半句（为什么不在大厅这一半说）仍然**逐字留在上面那行 `Debug.LogWarning` 里**。
            SayHintOnly(what + " —— 已开局、正在进战场（后面由对局那一层说）");
        }

        /// <summary>大厅阶段对面不在了 ⇒ **把本地这一局的账撤掉**（= 原版 `MatchMakerManager.CancelSearch`
        /// 那一下，但**不发 `match.cancel`**：对面已经不在那条连接上了，`Cancel()` 那个口会如实报
        /// 「联机没连上（这一局本来就没走联机）」—— 那句话在**这个**场景里是错的（我们确实在匹配，
        /// 只是对面没了）。🔴 **不新增协议消息**：这一下纯本地；对面回来之后两边重新点 `Battle!` 就是新的一局。
        /// ⚠️ `_started` 之后不走这里（调用方已经先把它分派出去了）。返回给玩家看的那半句。</summary>
        static string RevokeMatchLocal()
        {
            bool had = _myDeck != null || _foeDeck != null;
            ClearMatch();
            Debug.Log(had
                ? "[Net] 大厅：对面不在了 ⇒ 本地这一局的账已撤（原版那一刻 `MatchMakerManager.CancelSearch`，"
                + "那一下里还有一句 `BattleNetworkManager.CancelBattleSearch` —— 我们把状态交回 `NetSession` 自己管）"
                : "[Net] 大厅：对面不在了 —— 本机本来就没在匹配这一局（只是那条会话断了）");
            return had ? "这一局的匹配已经撤销" : "（本机本来就没在匹配这一局）";
        }

        /// <summary>**出声**那一处（提示行 + 日志）。⛔ 不弹窗 —— 弹窗只在真的「对面不在了」那两跳里发。
        /// ⚠️ `hint` 空 / null = **把那行字收掉**（`Reset()` 走这一支）。</summary>
        static void SayHintOnly(string hint)
        {
            if (string.IsNullOrEmpty(hint)) { SetHint(null); return; }
            Debug.Log("[Net] " + hint);
            SetHint(hint);
        }

        /// <summary>那行提示的**唯一落点**（`LastHint` 与 `OnHint` 一个口写出去 —— 别在别处各写一份）。</summary>
        static void SetHint(string hint)
        {
            LastHint = hint;
            if (OnHint != null) OnHint(hint);
        }

        /// <summary>提示行 + 日志 + **弹窗**（三层一起说，与对局那一半的 `NetBattle.SayPeerGone` 同形）。</summary>
        static void SayLobby(string hint, string popup)
        {
            SayHintOnly(hint);
            NetRuntime.Notice(popup);
        }

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
            ClearMatch();
            ICancelled = false; FoeCancelled = false;
            // 🆕 2026-10-17（B23·A902）：那个「对面不在」的边沿也一起清 —— 它的生命周期**只到这一局为止**
            //   （`BattleDriver.OnDestroy` 收工时也走本方法 ⇒ 打完一局不留着它）。
            _peerGone = false; _peerGonePopup = false;
            // 🆕 2026-10-17（B27·A925）：**那行提示也一起收掉**（生命周期与这一局一样长 —— `Reset()` 只在
            //   「点 `Battle!`」与「新一局收工」两处调）。不收的话：下一局一开局，台面上那行字还挂着
            //   上一局那句「对面掉线了…」（**说错话 = 另一种静默**）。
            //   ⚠️ 只在这儿清、**不放进 `ClearMatch()`**：`Cancel()` 也调 `ClearMatch()`，而取消那一刻
            //      那行字该说什么由 `Cancel` 自己的弹窗管（两处各写一份判据 = 迟早不一致）。
            SetHint(null);
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

            // 🆕 2026-10-17（B23·A902）：**对面回来了 ⇒ 撤销**（撤窗 + 提示行改口）。
            //   判据（原版那一刻）：连接状态回到 `connected(0)` ⇒ `BattleErrorUIManager__ConnectionStatusChanged`
            //   的 0/3/4/5 那一支 ⇒ `WindowsManager.CloseAllWindows()`（B13 报告 §① 的 D 条）。
            //   ⚠️ 我们**不调 `CloseAllWindows`** —— 它会把玩家这一刻自己开的窗一起关掉（B13 已记这条口径）
            //      ⇒ 只收**弹窗**那一颗，而且**只有真的弹过**才去收（`_peerGonePopup`：没弹过就撤，
            //      撤掉的会是玩家自己开着的那扇窗）。
            //   🔴 **只认边沿**（`_peerGone` 是掉线/离开那一下置的），否则每一帧都会说一遍。
            if (_peerGone && s.State == NetState.Lobby)
            {
                _peerGone = false;
                if (_peerGonePopup) { _peerGonePopup = false; NetRuntime.HideNoticePopup(); }
                SayHintOnly("对面回来了 —— 联机已恢复。要开这一局，两边重新各点一次 `Battle!`");
            }

            for (int i = 0; i < s.Inbox.Count; i++)
            {
                var f = s.Inbox[i];
                switch (f.kind)
                {
                    case NetKind.Deck:                                  // 只有主机收
                    {
                        if (ICancelled)
                        {
                            // 本机取消过这一局 ⇒ **丢掉迟到的卡组**（不然对面一点 Battle 就被拉回去开局）
                            Debug.Log("[Net] 已取消过这一局的匹配 ⇒ 丢掉对面迟到的卡组包");
                            break;
                        }
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
                        if (ICancelled)
                        {
                            // 本机取消过 ⇒ **丢掉迟到的开局包**（否则对面一开局就把本机拉进战场）
                            Debug.LogWarning("[Net] 已取消过这一局的匹配 ⇒ **丢掉迟到的开局包**（不进战场）");
                            NetRuntime.Notice("对面在你取消之后开局了 —— 这一局**没有进**。\n"
                                            + "对面那边会停在等待界面上，请重新约一次。");
                            break;
                        }
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
                    case NetKind.MatchCancel:                           // 🆕 两端都收（大厅阶段）
                    {
                        var mc = NetProtocol.Unpack<MsgMatchCancel>(f.payload);
                        FoeCancelled = true;
                        if (_started)
                        {
                            // 开局包已经发出去了 ⇒ 对面这条取消**晚了**。如实说，不假装两边一致。
                            Debug.LogWarning("[Net] 对面在开局之后才取消 —— 这一局照旧开（对面会收到开局包）");
                            NetRuntime.Notice("对面在你开局之后才点了取消 —— 这一局**照旧开始**。\n"
                                            + "对面那边会看到「已经开局、取消不了」，要退出只能在对局里投降。");
                        }
                        else
                        {
                            ClearMatch();
                            Debug.Log($"[Net] 对面取消了这一局的匹配（理由：{mc?.reason ?? "未说明"}）⇒ 本地也复位，不开局");
                            NetRuntime.Notice("对面取消了这一局的匹配 —— **双方都没有开局**，退回大厅。\n"
                                            + "可以各自重新点一次 `Battle!`。");
                        }
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
