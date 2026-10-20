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
//   ② **两副牌原样下发**（**不洗**）：两端跑同一套**绝对座位** + 同一个种子 ⇒ 各洗各的必然一致
//      （原版也是主机抽种子下发）。⚠️ 2026-10-19（`A1114`(a) 同族）就地订正：本行原来写「洗两副牌的顺序……
//      两端镜像跑会洗出不同的牌堆，所以顺序必须由一边定好发过去」—— 那是**镜像时代**的做法，**已作废**；
//      现在 `HostStartMatch` 的下发注释逐字写着「不洗牌」。
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

        // 🔴 **A1323（2026-10-19）**：原来只有**一个** `_started`，它同时当【我发过开局包】与
        //    【我收到过开局包】两种意思（`GoToBattle` 那处**两端都置**）⇒ 「谁先发的」这类问题问不出来，
        //    迟早出错。现在拆成两格，**对外只有一句判据** `Started`（两格取或）——
        //    ⛔ 其余各处别再自己拼一次（两处写同一条规则 = 迟早不一致）。
        //    · `_sentStart`     = 本机是**主机**、已经把 `MsgStart` 下发出去了（`HostStartMatch`）；
        //    · `_receivedStart` = 本机是**客机**、已经收到主机的 `MsgStart`（`PumpLobby` 的 `case NetKind.Start`）。
        //    ⚠️ 一台会话里只可能有一端 ⇒ 两格**不会同时为真**。
        static bool _sentStart, _receivedStart;
        /// <summary>这一局在【本机】这一端已经成立（**我发过 / 我收到过**开局包）—— **唯一**的对外判据。</summary>
        static bool Started { get { return _sentStart || _receivedStart; } }

        /// <summary>本机是不是正在等对面交卡组（界面可以据此显示「等待对手…」）。</summary>
        public static bool Waiting { get { return _myDeck != null && !Started && _foeDeck == null; } }

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
            if (s == null || !s.Transport.IsConnected) { why = Loc.T("Settings/Online/Cancel/WhyNoLink"); return false; }
            if (_myDeck == null && !Started) { why = Loc.T("Settings/Online/Cancel/WhyNotMatching"); return false; }
            if (Started)
            {
                // 开局包已经发出/收到 ⇒ 这一局成立了。**不假装取消成功**。
                why = Loc.T("Settings/Online/Cancel/WhyStarted");
                return false;
            }
            ICancelled = true;
            ClearMatch();
            s.Send(NetKind.MatchCancel, new MsgMatchCancel { reason = reason });
            Debug.Log("[Net] 已发出「取消匹配」—— 这一局不打了（对面也会退回大厅）");
            // 🆕 2026-10-10（`A985⑫` · 判据 = `项目任务.md` 第 373 行 + §四·15「走【最小形态】」）：
            //   **理由码**照引擎那套 token 形态记一条。原版那一刻（`CancelMatch.c:29` → `DeadHero(_, BattleResult)`
            //   的第三个实参）`BattleManager` **已经有 `MatchData`**、码进的是 `ctx`；而**我们取消匹配发生在
            //   还没有 `ctx` 的时候** ⇒ ⛔ **不动「`ctx` 何时出生」那条架构**（裁定原话），改在**匹配层自己的日志**里记，
            //   并且**如实标注这处差异**（`ctx.Events` 那边**一个字都没有** —— 这一条只落日志）。
            //   🔴 **那行字只有一处拼** = `ReplayStore.ResultLine`（`Battle/ReplayStore.cs`）—— 它与
            //   `RuleCore.Forfeit` / `CheckWinner` 那两句 `ctx.Log($"[BattleResult] {reason}({(int)reason}) seat={player}")`
            //   **同形**，而录像头尾那两条记录走的也是它 ⇒ 「同一 token 形态」是**用出来的**、不是再抄一份
            //   （⛔ 别在 `Net/` 里另拼一个 `"[BattleResult] Cancelled(5)"` 字面量：两份写法迟早不一致）。
            //   ⚠️ `seat` 传 `-1` = **没有座位可报**（那一刻还没有对局、没有绝对座位）⇒ `ResultLine` 不写 `seat=`
            //      —— 与引擎那两句的区别**只有这一处**，而且是真的（不是省了）。
            Debug.Log(ReplayStore.ResultLine(BattleResult.Cancelled, -1)
                    + " —— 匹配层：取消这一局的联机匹配（原版那一刻 `BattleManager` 已有 `MatchData`、"
                    + "码进 `ctx`；我们这一刻**还没有 `ctx`** ⇒ 这一条只落日志，`ctx.Events` 里没有它）");
            return true;
        }

        /// <summary>把「这一局」的账清掉（保留 `ICancelled` / `FoeCancelled` —— 它们要活到下一次 `Reset`）。</summary>
        static void ClearMatch()
        {
            _myDeck = null; _foeDeck = null;
            _myMode = _foeMode = null; _myFaction = _foeFaction = null; _myName = null;
            FoeName = null;
            _sentStart = false; _receivedStart = false;      // 🔴 A1323：两格都得清（只清一格 = 另一格带着上一局的意思）
            _revokedForLateCancel = false;                   // 🔴 A1322：那一档也是「这一局」的状态
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
        /// <summary>🆕 **A1322（2026-10-19）**：这一局是被**迟到的 `match.cancel`** 撤掉的 ⇒
        /// <see cref="GoNow"/> 据此**不报缺陷**、不切场景。由 `ClearMatch()` 清
        /// （`Reset` / `TryStart` 都会经过它 ⇒ 不会带到下一局）。</summary>
        static bool _revokedForLateCancel;

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
            // 🔴 **2026-10-19（P6d · A1079②）**：这一句原来是**裸中文字面量**（它进
            //   `DeferToBattleLayer` → `Lobby/DeferToBattle` 的 `{0}` → 提示行）⇒ 落键。
            //   形状选择见 `DeferToBattleLayer` 的 doc（**碎片建成独立键**，⛔ 没动父键形状）。
            if (Started) { DeferToBattleLayer(Loc.T("Settings/Online/Lobby/PeerLostFrag")); return; }
            string tail = RevokeMatchLocal();
            // 🔴 **2026-10-18（A933）压缩这一句**。原 =「联机断开了：对面掉线了 —— <tail>（对面回来之后，
            //   两边重新各点一次 `Battle!`）」= **52 / 56 字**（两条 `tail` 分支），超过提示行的**尺子**
            //   （`Shell/SearchingMatchPopup`：`HintLineWidth(句) > HintLineMaxWidth` = **80 个半宽字位**；
            //    其中 `HintLineMaxChars` = 40 个**汉字** = 中文档那一档 —— 下面这些数都是**中文列**的字符数）
            //   ⇒ 真跑到这一跳时每次都会 `LogWarning`。
            //   现压到 **36 / 40 字**（两条都 ≤ `HintLineMaxChars`）。去掉的只是**弹窗已经说过的那半句**（「联机断开了：」
            //   与那对 em-dash）—— 两处本来就是**两个口**（判据 → `SearchingMatchPopup.ShowHint` 上头那段）。
            //   ✅ **信息一个不少**：对面掉线了 · `tail`（这一局撤没撤）· **两边**都 · 回来各点一次 `Battle!`。
            //   ⚠️ 自检读的是子串 `Contains("掉线")`（`Editor/NetSelfTest.cs` M⑧ `:538` 与 N④ `:749`）⇒ **那个词保留**。
            // ⚠️ **2026-10-18 订正（铁律 5）**：`A933` 把「47 字那句」记在 **`:409`（「对面回来了…」）**上
            //   —— **对不上**：`:409` 实测 **39 字**（本来就没越界）。🔑 按长度反查，**47 字那句实测就是
            //   下面的 `DeferToBattleLayer`**（`what` = 14 ⇒ 14 + 33 = 47，逐字吻合）⇒ A933 的**落点漂了**；
            //   真正的越界句是**两句**：下面那一句 + **本句**（52/56），现都压到 ≤ `HintLineMaxChars`(40 个汉字)。
            SayLobby(string.Format(Loc.T("Settings/Online/Lobby/PeerLostHint"), tail),
                     string.Format(Loc.T("Settings/Online/Lobby/PeerLost"), tail));
            _peerGonePopup = true;
        }

        /// <summary>对面**主动离开**（收到 `bye`：他关了台 / 重开主机 / 重连被拒）。判据全文见本节头部。</summary>
        static void HandleLobbyPeerClosed(string why)
        {
            if (!LobbyOwnsSession) return;          // 对局那半边接管了 ⇒ 由它说（互斥闸）
            // 🔴 **A1321（2026-10-19）**：与 `HandleLobbyPeerLost` 那道闸**同形** —— `_peerGone` 是**边沿**。
            //    形状：对面**先掉线**（`OnPeerLost`，第一跳已经弹过窗、提示行也说过 `tail`）⇒ 同一条会话
            //    随后又发来 `bye`（`NetSession` 收到 `bye` 就 `Close()` ⇒ 这条 `OnClosed` 照样会到）。
            //    原来这里**没有**这道闸 ⇒ 同一段「对面不在了」会被**说第二遍**：多弹一次窗，
            //    而且提示行会**改口** —— 第二跳 `RevokeMatchLocal()` 里 `had` 已经是假（第一跳撤过了）
            //    ⇒ 说成「本机本来就没在匹配这一局」，与第一跳刚说的那句相反。
            //    ⚠️ **不是**「把 `bye` 吞掉」：`_peerGone` 只在「这一次对面不在了」期间为真；
            //      对面回来（`PumpLobby` 里 `State == Lobby` 那一支）会把它清掉 ⇒ 下一段掉线照旧会报。
            if (_peerGone)
            {
                // ⛔ 被闸挡掉的那一条**要留痕**（红线：不许静默失败）。
                string already = string.IsNullOrEmpty(why) ? Loc.T("Settings/Online/St/PeerLeft") : why;
                Debug.Log("[Net] 大厅：对面**离开了**（" + already + "），但这一段「对面不在了」"
                        + "**已经报过**（`_peerGone` 是边沿，第一跳 = 掉线）⇒ **不重复弹窗、也不改口提示行**（A1321）");
                return;
            }
            _peerGone = true;                       // `NetSession` 收到 `bye` 就 `Close()` ⇒ 不会再「回来」（同 `NetBattle` 那条注）
            string body = string.IsNullOrEmpty(why) ? Loc.T("Settings/Online/St/PeerLeft") : why;
            // 🔴 **2026-10-19（P6d · A1079②）**：「对面离开了：」这个**前缀碎片**原来是裸中文字面量 ⇒ 落键。
            //   ⚠️ **取词在拼句【之前】**（`body` 上面那一行就已经是取了词的那一句）—— 这条次序是硬的：
            //      反过来的话 `what` 会变成「带着键名的 35 字」⇒ 拼进 `DeferToBattle` 后 59 字
            //      > 提示行的尺子（`Shell/SearchingMatchPopup.HintLineWidth(句) > HintLineMaxWidth`；
            //        中文档那一档 = `HintLineMaxChars` = 40 个汉字），每次都 `LogWarning`。
            if (Started) { DeferToBattleLayer(Loc.T("Settings/Online/Lobby/PeerLeftFrag") + body); return; }
            string tail = RevokeMatchLocal();
            SayLobby(string.Format(Loc.T("Settings/Online/Lobby/PeerLeftHint"), body, tail),
                     string.Format(Loc.T("Settings/Online/Lobby/PeerLeft"), body, tail));
            _peerGonePopup = true;
        }

        /// <summary>「这一局已经开局」（开局包已发/收）那一档 ⇒ **不在这儿弹窗**：
        /// 接下来的出口是进战场 → `NetBattle` 接上来时补报（见 `NetBattle.WireSession` 末尾那段，
        /// 判据 = 接上时会话不在 `Lobby`）。这里只记日志 + 那一行提示（⛔ 不静默）。
        /// <para>⚠️ **参数 `what` 是【已经取过词的碎片】**（调用点自己拼好，例如
        /// `Lobby/PeerLeftFrag` + `body`）—— ⛔ 不要改成「把键传进来在这儿取词」：
        /// 那个戳会把取词推到**拼句之后**，`what` 就会带上 29 字的键名（见下面那条提示行尺子的预算）。</para></summary>
        static void DeferToBattleLayer(string what)
        {
            Debug.LogWarning("[Net] 大厅：" + what + " —— 但这一局**已经开局**（开局包已发/收）⇒ "
                           + "**不在大厅这一半弹窗**，交给对局那一层（`NetBattle` 接上来时会看到会话不在 `Lobby`）");
            // 🔴 **2026-10-18（A933）同时压缩这一句**：原 = `what` + 「 —— 这一局已经开局、正在进战场
            //   （断线那件事由对局那一层接着说）」（字面量 33 字）⇒ `what` 一长就超 `HintLineMaxChars`(40)。
            //   🔑 **`A933` 说的那句「47 字」实测就是这一句**：生产最长那条 `what` = `对面离开了：` +
            //   对方报的 `对面离开了这一局` = **14 字**，14 + 33 = **47** ⇒ 每次都出声（不是「对面回来了」那句）。
            //   现字面量 **24 字** ⇒ 生产路径最长那条 = **38 字**（`what` = 14），全部 ≤ `HintLineMaxChars`
            //   （`what` 只有两个来源：`HandleLobbyPeerLost` 的「对面掉线了」= 5 ·
            //    `HandleLobbyPeerClosed` 的「对面离开了：」+ 对方报的理由）。
            // 🔴 **2026-10-19（P6d）核过一遍，两个数都不变**：那两段碎片各自落成词条
            //   `Lobby/{PeerLostFrag,PeerLeftFrag}`，**ZH 列 = 调用点原话逐字** ⇒ 中文档长度**逐字相同**
            //   （5 字 / 6 字）⇒ 上面那条「最长 38 ≤ `HintLineMaxChars`」照旧成立。
            // ⚠️ **本段所有那些数都是【中文列字符数】**，比的是 `Shell/SearchingMatchPopup.HintLineMaxChars`
            //   （40 个**汉字**）。提示行真正的那把尺子是 `HintLineWidth(句) > HintLineMaxWidth`
            //   （**80 个半宽字位**，中英混排按字算）—— ⛔ 别再把「40 字」当成整条提示行的预算
            //   （那是 `A1083` 换尺子**之前**的口径）。
            //   ⇒ 🔴 **2026-10-11 就地订正（`A1315`，铁律 5）**：本行原写「英文档本来就超（下面是
            //      `DeferToBattle` 的 EN 列，**光模板就 86 字符**）」—— **已不成立**：`A1085` 把那几条
            //      EN 列压短之后，**模板只剩 47 位**（`{0} — match already started, entering the arena`）
            //      ⇒ **本来就不超**。**改后仍会超的只有夹对端 `body` 的两条**（`body` 满 40 个 ASCII
            //      ⇒ 上界 **99 / 81** 个半宽字位）——⚠️ 那两条**与语档无关**（中文档同一格 **136 / 122 位**），
            //      且**不是本次引入的回归**。⛔ 别再拿「86 字符」当现状。
            //   ⚠️ 原话那个「86」与 `Loc.cs` 记的**改前 88 位**对不上（差 2）—— 本条**没有替它裁**，
            //      按铁律 2 如实标出（出处 → `资料/普查产出_第十二会话/P5_文档与脚本小账.md` §⑤·1）。
            //   ⚠️ `body` 现在可能来自**词条键取词**（`Wire/*` 那 7~8 条，最长 ZH 13 字）
            //      ⇒ 最坏 `what` = 6 + 13 = 19 ⇒ 19 + 24 = 43 > `HintLineMaxChars`(40)（**接线前同样是 19**，
            //      因为那时对面直接发中文整句）⇒ **不是本次引入的回归**。
            SayHintOnly(string.Format(Loc.T("Settings/Online/Lobby/DeferToBattle"), what));
        }

        /// <summary>大厅阶段对面不在了 ⇒ **把本地这一局的账撤掉**（= 原版 `MatchMakerManager.CancelSearch`
        /// 那一下，但**不发 `match.cancel`**：对面已经不在那条连接上了，`Cancel()` 那个口会如实报
        /// 「联机没连上（这一局本来就没走联机）」—— 那句话在**这个**场景里是错的（我们确实在匹配，
        /// 只是对面没了）。🔴 **不新增协议消息**：这一下纯本地；对面回来之后两边重新点 `Battle!` 就是新的一局。
        /// ⚠️ `Started` 之后不走这里（调用方已经先把它分派出去了）。返回给玩家看的那半句。</summary>
        static string RevokeMatchLocal()
        {
            bool had = _myDeck != null || _foeDeck != null;
            ClearMatch();
            Debug.Log(had
                ? "[Net] 大厅：对面不在了 ⇒ 本地这一局的账已撤（原版那一刻 `MatchMakerManager.CancelSearch`，"
                + "那一下里还有一句 `BattleNetworkManager.CancelBattleSearch` —— 我们把状态交回 `NetSession` 自己管）"
                : "[Net] 大厅：对面不在了 —— 本机本来就没在匹配这一局（只是那条会话断了）");
            if (had)
            {
                // 🆕 2026-10-11（`A1297②`）：**这一局没了**（对面掉线 / 主动离开，两条路都走这里）⇒ 记一档理由码。
                //   🔴 **如实标注：原版【没有这条路】，码是我们挑的。** 原版那批带码的调用点**全表**
                //      在 `RuleEngine/Core/RuleCore.cs` 的 `BattleResult` 枚举注释里（每条都带原版
                //      `文件:行号`）—— **一张表里没有一条**是「大厅阶段对面不在了」：原版是**服务端匹配**
                //      ⇒ 搜索阶段根本没有「对手」这个实体挂在你这条连接上
                //      （`BattleNetworkManager__EventPlayerDisconnected.c` 对 20/30 两档落的是
                //       `LogError("Unhandled …")`）⇒ 这一跳是**我们自己的 P2P 口径**（见本节头部）。
                //   ⇒ 取**语义最近**的一档 `Disconnect`(3)：本机这一局是因为**对面那条连接没了**才结束的
                //      （不是谁点了取消 —— 那档见 `NetKind.MatchCancel` 那一支的 `A1297①`）。
                //   ⚠️ `seat` 传 `-1` = **没有座位可报**（大厅阶段还没有对局、没有绝对座位），同 `Cancel()` 那条。
                //   ⛔ 那行字**只有一处拼** = `ReplayStore.ResultLine`（别在这儿抄字面量）。
                //   🔴 **只在 `had` 时记**：本机本来就没在匹配这一局（`had == false`）⇒ 没有「局」可没
                //      ⇒ 记码就是静默说错一句话。⚠️ 这个闸**顺带**兼作去重：第一趟 `ClearMatch()`
                //      之后 `_myDeck/_foeDeck` 都空了，同一条连接上再来一次 `OnClosed` 时 `had` 为假
                //      ⇒ 不会为同一局记第二条码。
                Debug.Log(ReplayStore.ResultLine(BattleResult.Disconnect, -1)
                        + " —— 匹配层：大厅阶段对面不在了、本地这一局的账已撤（⚠️ 原版无此路：带码的调用点里"
                        + "没有「大厅阶段对面不在了」这一档 —— 原版服务端匹配、搜索阶段没有对手这个实体 ⇒ **码是我们挑的**，"
                        + "取语义最近的一档）");
            }
            return had ? Loc.T("Settings/Online/Lobby/MatchRevoked") : Loc.T("Settings/Online/Lobby/NotMatchingThisGame");
        }

        // ==================================================================
        //  🆕 **A1322（2026-10-19 · 调度台裁定 = 选项 A）**：**迟到的** `match.cancel`
        // ==================================================================
        //  「迟到」= 本机这一端**已经把开局包发出 / 收到**（`Started`），而对面这时才发取消。
        //
        //  🔴 **口径（裁定原文）**：**收侧认了取消** —— 不再继续进战场，撤本地这一局 + 用现有
        //     `Notice` / 提示行告知玩家；**只在 `PumpLobby` 可达窗口内做**（本机尚未切场景），
        //     窗口外保持现状并 Log；⛔ **不新增协议消息**（所以这一档也**不回包**）。
        //
        //  ⚠️ **为什么「不回包」不等于「只认一半」**：现有消息表里没有任何一条能表达
        //     「你这个取消来晚了」—— 回 `match.cancel` 会让对面当成「对面也撤了」并弹一句**假话**；
        //     重发 `start` 只会让对面再弹一次「已取消 ⇒ 丢掉迟到的开局包」（重复、无新信息）。
        //     ⇒ 判据落在**行为一致**：两边都**不进战场**（对面本来就没打算进），而不是「补一条消息」。

        /// <summary>这一局是不是**还来得及**认下这条迟到的取消 —— 判据 =「本机还没切场景」。
        ///
        /// <para>三个分支（由 `GoToBattle` 的实际次序决定）：</para>
        /// <list type="bullet">
        /// <item>**批处理**：`GoToBattle` 在 `Application.isBatchMode` 那一句就返回（**不切场景**）
        ///   ⇒ 窗口恒开（自检靠这一档验它）。</item>
        /// <item>**界面挂着 <see cref="HoldForPresentation"/>**（「找到对手」那一态还没放行）：
        ///   `GoToBattle` 停在那口气里、**没调 `SceneManager.LoadScene`** ⇒ 窗口开。</item>
        /// <item>**其余**：`GoToBattle` **已经同步调过** `SceneManager.LoadScene` ⇒ 切场景在飞 ⇒ **窗口关**。</item>
        /// </list>
        /// <para>⚠️ 还要求 `NetPendingBattle.Current != null`：那份跨场景槽**被战斗层 `Take()` 走** =
        /// 战斗层已经接管 ⇒ 没有任何可撤的东西了。</para></summary>
        static bool CanStillHonorLateCancel()
        {
            if (NetPendingBattle.Current == null) return false;
            return Application.isBatchMode || HoldForPresentation != null;
        }

        /// <summary>认下这条迟到的取消：撤本地这一局 + 出声（判据与口径 → 上面那一节）。
        /// ⚠️ **不发任何协议消息** —— 对面就是取消方，它没有在等我们回话。</summary>
        static void RevokeForLateCancel(MsgMatchCancel mc)
        {
            bool had = _myDeck != null || _foeDeck != null;
            // 🔴 **跨场景那份必须作废**：不清的话，「找到对手」展示完那一跳（`GoNow`）会照旧把本机
            //    **切进战场** —— 那正是本账要挡的那一下。
            NetPendingBattle.Current = null;
            ClearMatch();                        // 两格 `Started` / 两副牌 / 「谁是对面」全清
            _revokedForLateCancel = true;        // 给 `GoNow` 一个判据（它据此**不报错**、不切场景）
            // ⚠️ `HoldForPresentation` **不在这里清**：它由注册方（`SearchingOpponentWindow.Close/OnDestroy`）
            //    自己摘；真正挡住切场景的是上面那句 `Current = null`。
            //    📌 残留（Shell 那一侧，不在本批白名单）：那扇「找到对手」窗不会自己关 —— 玩家看到的是
            //       一句「对面取消了」的弹窗 + 那扇还开着的窗（可由 Esc/Cancel 关掉）。
            Debug.Log("[Net] 对面取消了这一局（**迟到**：本机已经把开局包发出/收到）⇒ **收侧认了**："
                    + "撤掉本地这一局、**不进战场**（A1322 · 选项 A）。理由：对面明确取消了、它不会进战场；"
                    + "本机单方面进去 = 两端分叉"
                    + (mc != null && !string.IsNullOrEmpty(mc.reason) ? $"（对面给的理由：{mc.reason}）" : ""));
            if (had)
            {
                // 理由码与「大厅阶段收到对面取消」那一支**同一档**（`A1297①` 的现成口径）：
                // 这一局确实是**被取消掉的**（不是掉线 —— 掉线那一档见 `RevokeMatchLocal` 的 `A1297②`）。
                // ⛔ 那行字只有一处拼 = `ReplayStore.ResultLine`（铁律 6，别在这儿抄字面量）。
                // ⚠️ 与那一支的唯一区别 = 「**迟到**」：这一局在本机**曾经成立过**（开局包已发/收）；
                //    而 `seat` 照样传 `-1` —— 那一刻**还没有对局、没有绝对座位**（同 `Cancel()` 那条）。
                Debug.Log(ReplayStore.ResultLine(BattleResult.Cancelled, -1)
                        + " —— 匹配层：**迟到的**对面取消 ⇒ 收侧认了、这一局没开"
                        + "（⚠️ 原版无此路：P2P 自拟；码取语义最近的一档 `Cancelled`）");
            }
            // 玩家那一侧：**沿用现成词条**（⛔ 没新增键）——
            //   弹窗 = `PeerCancelled`（「双方都没有开局，退回大厅」，与这一档逐字相符）；
            //   提示行 = `MatchRevoked`（最短那句，落在 `A933` 那把 80 个半宽字位的尺子里）。
            SayLobby(Loc.T("Settings/Online/Lobby/MatchRevoked"),
                     Loc.T("Settings/Online/Lobby/PeerCancelled"));
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

        /// <summary>界面展示完了 ⇒ 真的切场景（配 <see cref="HoldForPresentation"/> 用）。
        /// 🆕 **A1322（2026-10-19）**：展示期间这一局若被**对面迟到的取消**撤掉（`RevokeForLateCancel`），
        /// 这里**不切场景**、也不把它报成缺陷（跨场景槽空着在这一档是**正常**的）。</summary>
        public static void GoNow()
        {
            var pb = NetPendingBattle.Current;
            if (pb == null)
            {
                // 🆕 **A1322（2026-10-19）**：这一份空着**有可能是正常的** —— 这一局在「找到对手」
                //    展示期间被**对面迟到的取消**撤掉了（`RevokeForLateCancel` 把跨场景槽清空）。
                //    ⇒ 这一档**不报缺陷**、也**不切场景**（判据与口径 → `RevokeForLateCancel` 上头那一节）。
                if (_revokedForLateCancel)
                {
                    Debug.Log("[Net] 「找到对手」展示完毕，但这一局**已经被对面取消** ⇒ 不切场景"
                            + "（窗口内认下的取消，A1322）—— 那扇窗由玩家自己关（Esc/Cancel），回大厅");
                    return;
                }
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
            if (s == null || !s.Transport.IsConnected) { why = Loc.T("Settings/Online/Lobby/BotNoLink"); return false; }
            if (s.State != NetState.Lobby && s.State != NetState.InBattle)
            { why = string.Format(Loc.T("Settings/Online/Lobby/BotSessionNotReady"), s.State); return false; }
            if (deck == null) { why = Loc.T("Settings/Online/Lobby/BotEmptyDeck"); return false; }

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
            NetRuntime.Notice(string.Format(Loc.T("Settings/Online/Lobby/PlayedVsBot"), why,
                            cfg.role == (int)NetRole.Host
                                ? "【" + Loc.T("Settings/Online/Save") + "】"
                                : "【" + Loc.T("Settings/Online/CheckConnection") + "】"));
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
                SayHintOnly(Loc.T("Settings/Online/Lobby/LobbyRestored"));
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
                        if (m == null || string.IsNullOrEmpty(m.deckJson))
                        {
                            // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志）**：唯一前缀，方便真连时
                            //   一眼分清「卡组包坏了」与「本机取消过 ⇒ 主动丢掉」（上面那一支）。
                            Debug.LogWarning("[Net] NET/BYE-R1/deck-unparsable 收到空的卡组包（NET/DECK/empty）"
                                           + " —— 丢掉这一条；主机不会因此开局");
                            break;
                        }
                        _foeDeck = JsonUtility.FromJson<PlayerDeck>(m.deckJson);
                        _foeMode = m.gameMode;
                        _foeFaction = m.faction;
                        // 🆕 主机在这一刻就知道「对面是谁」了（握手包里带着对方的名字）
                        //    ⇒ `SearchingOpponentWindow` 的「找到对手」那一态有东西可填。
                        // ✅ **本路已经钳过**（2026-10-18 现核）：`s.PeerName` 来自 `MsgProof.name`，
                        //    而它在 `NetSession.cs` 的收包段（`case NetKind.Proof`）就套过一次
                        //    `NetSession.ClampPeerText` ⇒ 这里**不用再钳**（⛔ 别写第二份）。
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
                            NetRuntime.Notice(Loc.T("Settings/Online/Lobby/StartAfterCancel"));
                            break;
                        }
                        var st = NetProtocol.Unpack<MsgStart>(f.payload);
                        if (st == null)
                        {
                            // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：原来是一句
                            //   光秃秃的 `break` —— 开局包解不出来时本机会**静默留在原地**（对面已进战场）。
                            Debug.LogWarning("[Net] NET/BYE-R1/start-unparsable 主机发来的开局包 `start` **解不出来**"
                                           + "（载荷坏了 / 版本对不上）⇒ 丢掉这一条、**不切场景**；"
                                           + "对面那一端已经按「已开局」走了 —— 这一局会两端分叉，必须查");
                            break;
                        }
                        // 🆕 客机在这一刻才知道「对面是谁」（大厅阶段拿不到，只有主机收得到 Deck）。
                        //    ⚠️ 这几个字段都是**主机视角**的名字：`myName` 是主机的名字、`hostDeckJson`
                        //    是主机那副牌 —— 对客机来说它们正是「对面」。
                        if (!string.IsNullOrEmpty(st.hostDeckJson))
                            _foeDeck = JsonUtility.FromJson<PlayerDeck>(st.hostDeckJson);
                        _foeFaction = st.hostFaction;
                        // 🔴 **A961（2026-10-18，主对话裁定「封口」）：这一行是同一笔账里【玩家看得见】的那个入口。**
                        //   `st.myName` **是对端可控的**（= 主机那台机器的显示名，`NetProtocol.cs:77` 的 `MsgStart.myName`，2026-10-19 现核），
                        //   而 `FoeName` 往下走两条**真在界面上**的路：
                        //     · `Shell/SearchingOpponentWindow.cs:83`（「找到对手」那扇窗）；
                        //     · `BattleDriver.AlliancePanelName`（2026-10-19 现核 `:4237-4238`）/ `_enemyText` 那一句（现核 `:19628-19629`）。
                        //   ⇒ 与另外五个入口**同一处口径**：进界面前先钳
                        //     （复用现成的 `NetSession.ClampPeerText`，⛔ 不写第二份）。
                        //   ⚠️ 这是我们自拟的（原版那一刻的名字来自**服务器**账号 id、长度由服务端管）
                        //      → 取值与出处见 `NetProtocol.MaxPeerTextChars`。
                        FoeName = NetSession.ClampPeerText(st.myName, NetProtocol.MaxPeerNameChars);
                        Debug.Log($"[Net] 主机开局：种子 {st.seed} · 模式 {st.mode} · 战场 {st.arena} · "
                                + $"先手 = {(st.hostFirst == 0 ? "主机" : "客机")}"
                                + (string.IsNullOrEmpty(FoeName) ? "" : $" · 对面是「{FoeName}」"));
                        // 🔴 **A1323（2026-10-19）**：【我收到过开局包】= 这一跳（客机那一端）。
                        _receivedStart = true;
                        GoToBattle(st);
                        break;
                    }
                    case NetKind.MatchCancel:                           // 🆕 两端都收（大厅阶段）
                    {
                        var mc = NetProtocol.Unpack<MsgMatchCancel>(f.payload);
                        FoeCancelled = true;
                        if (Started)
                        {
                            // ==================================================================
                            //  🔴 **A1322（2026-10-19 · 调度台裁定 = 选项 A）：收侧认了取消。**
                            // ==================================================================
                            //  **原来**：只 `LogWarning` + `Notice(MissedCancel)`，**照旧进战场** ⇒
                            //  对面已经退回大厅、本机单方面进去 = **两端分叉**。
                            //  注释当时自己写着「如实说、不假装两边一致」—— 那不是「双向语义」，
                            //  取消只认了一半。
                            //
                            //  **现在**：取消是一条**双向**语义 —— 对面明确取消了 ⇒ **这一局不往下走**：
                            //    ① 撤掉本地这一局（`ClearMatch` + 作废跨场景那份 `NetPendingBattle`）；
                            //    ② 告诉玩家（**沿用现成词条** `Lobby/PeerCancelled`，⛔ 没新增协议消息、也没新增词条键）。
                            //  ⚠️ **只在窗口内做**：「本机**还没切场景**」那一小段（判据见 `CanStillHonorLateCancel`）。
                            //     窗口外（战斗层已经接管）⇒ **保持现状** + 出声（那是真的来不及了）。
                            if (!CanStillHonorLateCancel())
                            {
                                // 窗口外：这一局已经被战斗层 `Take()` 走（`NetPendingBattle.Current == null`），
                                // 或者 `GoToBattle` 已经调过 `SceneManager.LoadScene`（没有 `HoldForPresentation`
                                // 拦着）⇒ **取消来不及了**。⛔ 不假装撤掉。
                                Debug.LogWarning("[Net] 对面在开局之后才取消 —— 但这一局**已经切进战场了**"
                                               + "（窗口外：本机已无可撤回的待进开局包）⇒ 取消**来不及了**，"
                                               + "这一局照旧（原版那一刻也是「已经开局、取消不了」）。如实说出来");
                                NetRuntime.Notice(Loc.T("Settings/Online/Lobby/MissedCancel"));
                                break;
                            }
                            RevokeForLateCancel(mc);
                            break;
                        }
                        else
                        {
                            ClearMatch();
                            Debug.Log($"[Net] 对面取消了这一局的匹配（理由：{mc?.reason ?? "未说明"}）⇒ 本地也复位，不开局");
                            // 🆕 2026-10-11（`A1297①`）：**这一局没了** ⇒ 照 `Cancel()` 那一条的做法记一档理由码。
                            //   🔴 **如实标注：原版【没有这条路】，码是我们挑的。** 全量反编译里带码的调用点
                            //      **只有一处**是「取消匹配」这一档 —— `CancelMatch.c:29`
                            //      （= **玩家自己**点取消那一下，已在 `Cancel()` 里记过）。「对面撤销了匹配」
                            //      这个**事件**在原版不存在：原版是**服务端匹配**，撤单由服务端做，
                            //      客户端之间根本没有 `match.cancel` 这种东西（同本节头部那段「我们怎么定的 · ⚠️ 不是复刻」）。
                            //   ⇒ 我们这条 P2P 口径取**语义最近**的一档 `Cancelled`(5)（那一局确实是被「取消」掉的，
                            //      不是掉线 —— 掉线那一档见 `RevokeMatchLocal` 里的 `A1297②`）。
                            //   ⚠️ `seat` 传 `-1` = **没有座位可报**（那一刻还没有对局、没有绝对座位），同 `Cancel()` 那条。
                            //   ⛔ 那行字**只有一处拼** = `ReplayStore.ResultLine`（别在这儿抄字面量，铁律「两处写同一条规则」）。
                            //   🔴 **这一支（`Started == false`）记码** —— 这一局**还没开局就没了**。
                            //      ⚠️ **上面 `Started` 那一支现在不是「照旧开」了**（`A1322` 选项 A）：窗口内
                            //      它会 `RevokeForLateCancel`、并由那一支**自己**记一条码；**窗口外**才保持
                            //      「这一局照旧开」且**不记码**（那时没有「没了」，记码就是静默说错一句话）。
                            Debug.Log(ReplayStore.ResultLine(BattleResult.Cancelled, -1)
                                    + " —— 匹配层：对面撤了这局匹配（⚠️ 原版无此路：带码的调用点只有 `CancelMatch.c:29` 一处、"
                                    + "那是玩家自己撤；「对面撤」在服务端匹配的原版里不存在 ⇒ **码是我们挑的**，取语义最近的一档）");
                            NetRuntime.Notice(Loc.T("Settings/Online/Lobby/PeerCancelled"));
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
            if (s == null)
            {
                // 🔴 **2026-10-20（W58-NETPROBE · T05 探针，只加日志、不改行为）**：原来静默返回 ——
                //   两端都交完卡组、而这一刻会话没了（被 `Reset` 换掉 / `NetRuntime` 不在）⇒ 谁都开不了局。
                Debug.LogWarning("[Net] NET/BYE-R7/no-session 主机要开局，但**这一刻没有会话**"
                               + "（`NetRuntime.Instance.Session == null`）⇒ 开不了局；"
                               + "这一局会停在这里（不是「正在等对手」）");
                return;
            }

            // ① 模式必须一致（首版只做经典；两端都带遭遇牌也允许 —— 同一套引擎，但**要出声**）
            string mode = _myMode ?? "Classic";
            if (!string.IsNullOrEmpty(_foeMode) && _foeMode != mode)
            {
                Debug.LogError($"[Net] 🔴 两端模式不一样（我 {mode} / 对面 {_foeMode}）—— 拒绝开局，别打出两端不一致的账");
                // 🔴 **红线**：原来只有这行日志 ⇒ 两边都卡在「正在搜索」上、**谁也不明白为什么开不了**。
                //    判据 → `项目任务.md` §三 第 14 条 表里的第 4 条。
                NetRuntime.Notice(string.Format(Loc.T("Settings/Online/Lobby/ModeMismatch"), mode, _foeMode));
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
            bool sent = s.Send(NetKind.Start, start);
            // 🔴 **A1323（2026-10-19）**：【我发过开局包】就落在这里（原来这句在 `GoToBattle` 里，两端共用）。
            //    ⚠️ 发送失败也记「已发」—— 这一局在**本机这一端**确实已经成立（种子 / 先手 / 战场都定了、
            //    `NetPendingBattle` 也放进静态槽了），只是对面收不到；`Send` 那一刻自己已经喊过一句，
            //    这里再**明确说一次后果**（⛔ 不静默）。
            _sentStart = true;
            if (!sent)
                Debug.LogWarning("[Net] ⚠️ 开局包 **没发出去**（这一刻连接已断）⇒ 本机仍按「这一局已开局」处理"
                               + "（这一端已经定了种子/先手/战场），但**对面收不到** —— 如实说出来，不假装两边一致");
            Debug.Log($"[Net] 主机开局：种子 {seed} · 先手座位 {firstSeat}（{(firstSeat == 0 ? "主机" : "客机")}）· "
                    + $"战场 {arena} · 两副牌 {_myDeck.CardIds.Count}/{_foeDeck.CardIds.Count} 张已下发"
                    + "（**不洗牌**：两端同种子同输入 ⇒ 洗出来的顺序必然一样）");
            GoToBattle(start);
        }

        /// <summary>两端都走这一条：把开局参数放进跨场景的静态槽，然后切到那一场。</summary>
        static void GoToBattle(MsgStart st)
        {
            // 🔴 **A1323（2026-10-19）**：这里原来是一句 `_started = true;` —— **两端共用**这一条
            //    ⇒ 「我发过」与「我收到过」混成一个格。现在两格由**各自的调用点**置：
            //    · 主机 = `HostStartMatch` 里那句 `Send(NetKind.Start, …)` 之后（`_sentStart`）；
            //    · 客机 = `PumpLobby` 的 `case NetKind.Start` 里（`_receivedStart`）。
            //    ⛔ 别挪回这里：这里分不出是谁（`host` 那一句要等 `NetRuntime` 活着才答得出来）。
            bool host = NetRuntime.Instance != null && NetRuntime.Instance.Session != null
                     && NetRuntime.Instance.Session.Role == NetRole.Host;
            var pb = NetPendingBattle.FromStart(st, host);
            if (pb == null)
            {
                Debug.LogError("[Net] 开局包解不出来 —— 不切场景");
                NetRuntime.Notice(Loc.T("Settings/Online/Lobby/StartParseFailed"));
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
