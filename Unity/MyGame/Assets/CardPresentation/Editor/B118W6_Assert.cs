// B118W6_Assert.cs — 第一百一十八会话 · 执行代理 `B118-W6` · `B117-T13` 战斗内联机表现 7 条
//
// ============================ 覆盖（本波动过的那几条）============================
//   ③ **停表闸**：原版只有「对手掉线」那一支才 `PauseClock()`（`ShowDisconnectionPopup.c:25-34`）
//   ④ **结算段分档**：对手掉线静默、**本机**掉线要出声 + 记放行门 + 起 27 秒窗口
//      （`DisconnectedDuringBattle.c:35-66`；判负那一半 = `RuleCore.Forfeit(..., postBattleRejudge: true)`，第三轮补完）
//   ⑤ **退出房间不发 `bye`**：原版 `QuitApplication` 第②跳 `LeaveRoom()` 不带告别（`NetRoomExit`）
//   ⑦ **换牌期不是「stage == 0」**：原版换牌 = stage 40 ⇒ 要判负（`FailedToReconnectAfterDisconnect.c:30-53`）
//   ①② （零钮窗 / 就地换字）要**真窗口系统** ⇒ 在 `RunWindowChecks(wm)` 里，等调度台接到 Shell 宿主。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   C3  把 `ClockPaused` 改回**无条件** `true` ⇒ C3 红（C2 仍绿 ⇒ 只有它红）。
//   D3  把结算段改回「两端一律静默」⇒ D3 红。
//   E2  把 `NetRoomExit.SayGoodbyeOnQuit` 改回 `true` ⇒ E2 红（帧数 0 → 1）。
//   E4  E2/E3 是**同一台会话、只差一个形参** ⇒ 想「两边一起改回 true」做不到：E3 的对照就要求 `true` 发 1 帧。
//   F2  把「换牌期 ⇒ 判负」改回 `if (Ctx.MulliganOpen) LeaveWithoutResult(...)` ⇒ F2 的前提（本引擎不存在
//       stage==0 窗口）仍绿，但 `NetBattle` 那一支就会被 F2 的**说明**判为错 —— 见报告 §7，本条是**判据钉**
//       （机器可验的部分 = 「`MulliganOpen` 一建好就有值」）。
//   W2/W3 把掉线窗改回 `ShowPopUp(text, OK, …)` / 每秒走 `ShowMessagePopUp` ⇒ W2（多一颗钮）与 W3（实例变了）各红一条。
//
// ============================ 判据出处（一手）============================
//   · 停表闸：`d:/2/tools/decomp_full/BattleManager__ShowDisconnectionPopup.c:14-36`
//       `iVar3 = (param_2 ^ 1) + 1`（连接状态）· `if (param_2 == 0) { ClockManager.PauseClock(); … }`
//       ⇒ `param_2 == 0` = **对手**掉线；本机掉线那一支**没有** `PauseClock`。
//   · 结算段分档：`d:/2/tools/decomp_full/BattleManager__DisconnectedDuringBattle.c:35-66`
//       `stage == 0x3c(60)` ⇒ `if (param_2 == 0) return;`（对手 = 静默）；
//       本机（`param_2 != 0`）= `ShowDisconnectionPopup(1)` + `shouldLoseIfDisconnectEnd(+0x2D8) = true`。
//   · 退出不发 bye：`d:/2/tools/decomp_full/PlayerDataManager__QuitApplication.c` 第②跳 `photonManager.LeaveRoom()`；
//       对面侧 `NetworkCustomManager__OnPhotonPlayerDisconnected` → `SetOpponentDisconnected`
//       ⇒ 弹窗 + 停表 + 它自己的 30 秒 ⇒ 到点 `DeadHero(对面, 3)`。
//   · 换牌不是 stage 0：`…__FailedToReconnectAfterDisconnect.c:30-53`（`if (battleStage != 0)` ⇒ 判负）
//       ＋ `dump.cs:27347-27355`（`Undefined 0 / Mulligan 40 / MatchProper 50 / PostBattle 60`）
//       ＋ `_SetupMulliganPhase_d__341:27`（一进换牌就置 40）。
//   · 零钮窗 / 就地换字：`Everguild.BattleErrorUIManager__ShowDisconnectionPopUp.c:51`（六个 0 实参）
//       ＋ `…__UpdateReconnectWindow.c:44-75`（`popUpWindow` 还开着 ⇒ `SetText`；不在才 `ShowPopUp`）。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  ① 纯函数那一段（`Run()`）—— 任一宿主的聚合行、`//` 之前同样式追加：
//        B118W6_Assert.Run(); pass += B118W6_Assert.Pass; fail += B118W6_Assert.Fail;
//     ⚠️ **先看那一行第一个 `//` 在哪**（追加到 `//` 之后 = 断言永远不跑，而 typecheck / 账闸 / 退出码全绿）。
//     建议宿主：`Editor/NetSelfTest.cs`（联机那三台的既有宿主）或 `BattleScene`。
//  ② 窗口那一段（`RunWindowChecks(wm)`）**只能**在**有 `WindowsManager` 的宿主**里跑（`ShellScene`）：
//        B118W6_Assert.RunWindowChecks(WindowsManager.Instance); …
//     ⛔ 别在批处理里无参调它 —— 没有窗口系统时那几条**必然是假红**。
//
// ============================ 边界（如实记）============================
//  · ⛔ 不验「重连倒计时 / 判负」那一整链（要一对真 socket；`NetBattleTest` 已有 loopback 宿主）。
//  · ⛔ 这里只钉「结算段分档对不对」。「本机那一端到点真的**重判**了」需要一条会话 + 已结算的 `Ctx`
//    ⇒ 归 `NetBattleTest` 的 loopback 夹具（可粘贴的那段在报告 §第三轮）。那道**放行门**本身由
//    `B118W11_Assert` B3/B4 钉（`postBattleRejudge: true` ⇒ 重判 / 缺省 ⇒ 仍被挡）。
using System.Collections.Generic;
using CardPresentation;
using CardPresentation.Net;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B118-W6`：战斗内联机表现的 ③④⑤⑦ 四条（纯函数）+ ①② 两条（需窗口系统）。</summary>
public static class B118W6_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B118W6] ") { Near = MenuNearStyle.Compact3 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    public static void Run()
    {
        SectionC_ClockGate();
        SectionD_AfterBattleSilence();
        SectionE_RoomExitSaysNoBye();
        SectionF_NoUndefinedStageWindow();
    }

    // ==========================================================================================
    //  C · ③ 停表闸：只有「对手掉线」才停表
    // ==========================================================================================
    static void SectionC_ClockGate()
    {
        MenuCheck.Section(_sink, "C · ③ 停表闸（原版 ShowDisconnectionPopup.c:25-34）");

        bool peer = NetBattle.PausesClockWhileDisconnected(false);    // 对手掉线
        bool self = NetBattle.PausesClockWhileDisconnected(true);     // 本机掉线
        MenuCheck.True(_sink,
            NetBattle.PausesClockWhileDisconnected(false) || NetBattle.PausesClockWhileDisconnected(true),
            "（前提）这一格**不是恒 false**（恒 false 会让 C2 也算绿，等于没查）");

        MenuCheck.Check(_sink, peer, true,
            "★ C2 对手掉线（`localPlayerDisconnected == 0`）⇒ **停表**（原版 `ClockManager.PauseClock()`）");

        MenuCheck.Check(_sink, self, false,
            "★ C3 **本机**掉线 ⇒ **不停表**（原版那一支 `else` 里只 `StopCoroutine(AttemptReconnect)`，没有 `PauseClock`）"
          + "｜🧨 改回无条件 `ClockPaused = true` ⇒ 本行红");

        MenuCheck.True(_sink, peer != self,
            "★ C4 灭自证：两档**必须不同** ⇒ 「无条件停表」怎么改都会有一条红（C2 或 C3）");
    }

    // ==========================================================================================
    //  D · ④ 结算段静默分档
    // ==========================================================================================
    static void SectionD_AfterBattleSilence()
    {
        MenuCheck.Section(_sink, "D · ④ 结算段（PostBattle）掉线：对手静默 / 本机出声");

        bool peer = NetBattle.SilentWhenPeerLostAfterBattle(false);   // 对手掉线
        bool self = NetBattle.SilentWhenPeerLostAfterBattle(true);    // 本机掉线

        MenuCheck.Check(_sink, peer, true,
            "★ D2 **对手**在结算段掉线 ⇒ **静默**（原版 `DisconnectedDuringBattle.c:49-51`：`if (param_2 == 0) return;`）");

        MenuCheck.Check(_sink, self, false,
            "★ D3 **本机**在结算段掉线 ⇒ **不许静默**（原版 `:52-64`：`ShowDisconnectionPopup(true)` + `shouldLoseIfDisconnectEnd = true`）"
          + "｜🧨 改回「两端一律静默」⇒ 本行红");

        MenuCheck.True(_sink, peer != self,
            "★ D4 灭自证：两档必须不同（同一句 `return` 不可能同时满足 D2 与 D3）");
    }

    // ==========================================================================================
    //  E · ⑤ 「退出应用 ⇒ 离开房间」不捎 bye
    // ==========================================================================================
    static void SectionE_RoomExitSaysNoBye()
    {
        MenuCheck.Section(_sink, "E · ⑤ NetRoomExit：离开房间**不带告别**（原版 QuitApplication 第②跳 LeaveRoom）");

        MenuCheck.Check(_sink, NetRoomExit.SayGoodbyeOnQuit, false,
            "★ E1 `NetRoomExit` 的收尾档 = **不发 `bye`**（`Close(say: false)` = 硬断）"
          + "｜🧨 改回 `Close(true, …)` ⇒ E1 与 E2 一起红");

        // E2 / E3 走**同一台**会话，只差 `say` 那一格 ⇒ 结构上不可能两边一起绿。
        int byeWithout = ByeFramesSent(say: NetRoomExit.SayGoodbyeOnQuit);
        int byeWith = ByeFramesSent(say: true);

        MenuCheck.Check(_sink, byeWithout, 0,
            "★ E2 实际发出去：`Close(say: false)` ⇒ **0 帧 `bye`**（对面因此走它自己的 15/30 秒窗口，"
          + "到点才 `DeadHero(对面, 3)` —— 与原版 `OnPhotonPlayerDisconnected` 同形）");

        MenuCheck.Check(_sink, byeWith, 1,
            "E3 对照：`Close(say: true)` ⇒ **恰 1 帧 `bye`**（说明 E2 读的确实是「发不发」这一格，不是夹具坏了）");

        MenuCheck.True(_sink, byeWith > byeWithout,
            "★ E4 灭自证：E2 与 E3 是同一台会话、只差一个形参 ⇒ 「两边一起改」做不到");
    }

    /// <summary>用一台**假传输层**跑一次 `NetSession.Close`，返回它看见的 `bye` 帧数。
    /// ⚠️ 会话必须先进 `InBattle` —— 新会话默认 `State == Off`，而 `Close(say:true)` 在 `Off` 那一档**不发**。</summary>
    static int ByeFramesSent(bool say)
    {
        var t = new ByeRecorder();
        var s = new NetSession(t);
        s.EnterBattle();
        MenuCheck.True(_sink, s.State == NetState.InBattle && t.IsConnected,
            "（前提）假会话已进对局、传输层报「连着」（否则 `Close` 会把那句 `bye` 当成「没有活连接」丢掉）");
        s.Close(say, "Settings/Online/Wire/PeerLeftMatch");
        return t.Count(NetKind.Bye);
    }

    /// <summary>只记「发过哪些 kind」的传输层（`INetTransport` 的最小实现，⛔ 不碰真 socket）。</summary>
    sealed class ByeRecorder : INetTransport
    {
        public readonly List<string> Kinds = new List<string>();
        public int Count(string kind) { int n = 0; foreach (var k in Kinds) if (k == kind) n++; return n; }
        public bool IsListening { get { return false; } }
        public bool IsConnected { get { return true; } }
        public bool PeerLost { get { return false; } }
        public string LastError { get { return null; } }
        public int Port { get { return 0; } }
        public int AcceptedCount { get { return 1; } }
        public void Listen(int port) { }
        public bool Connect(string host, int port, int timeoutMs) { return true; }
        public void Send(string kind, string payloadJson) { Kinds.Add(kind); }
        public int Pump(List<NetFrame> into) { return 0; }
        public void ClosePeer() { }
        public void Close() { }
    }

    // ==========================================================================================
    //  F · ⑦ 本引擎**没有**「`battleStage == Undefined(0)`」那个窗口
    // ==========================================================================================
    static void SectionF_NoUndefinedStageWindow()
    {
        MenuCheck.Section(_sink, "F · ⑦ 换牌期原版 = stage 40（⇒ 判负），不是 stage 0（⇒ 不判结果）");

        // 前提：`NewBattle` 返回的 Ctx **一存在就已经把换牌档位定好**（没有「还没决定」的中间态）。
        // ⚠️ `GameplayVariables.For(...)` 返回的是**共享静态实例** ⇒ 要改字段必须先 `Clone()`（`NewBattle` 自己也 Clone）。
        var varsMull = GameplayVariables.Classic.Clone(); varsMull.showMulligan = true;
        var ctxMull = NewCtx(openMulligan: true, vars: varsMull);
        MenuCheck.True(_sink, ctxMull != null && ctxMull.MulliganOpen && ctxMull.Vars.showMulligan,
            "（前提）`openMulligan: true` + `showMulligan: true` ⇒ `Ctx` **建好那一刻** `MulliganOpen == true`"
          + "（判据 `RuleCore.cs` 的 `mulliganOpenNow = openMulligan && ctx.Vars.showMulligan`，只此一处算式）");

        var varsNo = GameplayVariables.Classic.Clone(); varsNo.showMulligan = false;
        var ctxNo = NewCtx(openMulligan: true, vars: varsNo);
        var ctxOff = NewCtx(openMulligan: false, vars: GameplayVariables.Classic);

        MenuCheck.True(_sink, ctxNo != null && !ctxNo.MulliganOpen && ctxOff != null && !ctxOff.MulliganOpen,
            "★ F1 另外两档也**建好即定**（`showMulligan: false` / `openMulligan: false` ⇒ 立刻 `MulliganOpen == false`）"
          + " ⇒ 本引擎**不存在**原版 `battleStage == Undefined(0)` 那个「换牌还没开始」的窗口");

        MenuCheck.True(_sink, ctxMull != null && ctxMull.MulliganOpen,
            "★ F2 换牌期（原版 stage **40**）⇒ **不是** stage 0 ⇒ 重连失败时原版走 `AddResignAction(1)` + `DeadHero(我,3)`"
          + "（**判负**，`FailedToReconnectAfterDisconnect.c:30-53`）—— 我们原来拿 `Ctx.MulliganOpen` 当「stage == 0」"
          + " 去走 `LeaveWithoutResult`（**不判结果**），方向是反的；已删掉那一支"
          + "｜🧨 把 `if (Ctx.MulliganOpen) LeaveWithoutResult(...)` 加回来 ⇒ 与本条的前提直接矛盾");

        MenuCheck.True(_sink, ctxOff != null && !ctxOff.MulliganOpen,
            "F3 不跑换牌那一档（原版 stage **50** = MatchProper）⇒ 同样 `!= 0` ⇒ 也要判负（不是「不判结果」）");
    }

    /// <summary>与 `RuleEngine/Editor/B102R1_Assert.cs` 的夹具同口径（`shuffle: false`，只有两个督军）。</summary>
    static BattleContext NewCtx(bool openMulligan, GameplayVariables vars)
    {
        var d0 = new List<CardDef> { Hero("B118W6Hero0", "SpaceWolves") };
        var d1 = new List<CardDef> { Hero("B118W6Hero1", "Goff") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, openMulligan: openMulligan, vars: vars);
    }

    static CardDef Hero(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 0, 30, 0, null, subtype: "Warlord");
    }

    // ==========================================================================================
    //  W · ①② 掉线窗：零颗钮 + 每秒就地换字（**必须有窗口系统**，只给 Shell 那种宿主调）
    // ==========================================================================================
    /// <summary>①②的验收（⛔ 批处理里没有 `WindowsManager`，别在无窗宿主里调 —— 那几条必然假红）。
    /// 建议接线点：`Editor/ShellScene.cs` 里已经建好 `wm` 的那一段。</summary>
    public static void RunWindowChecks(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "W · ①② 掉线窗：开窗零颗钮 / 每秒就地换字（需窗口系统）");

        MenuCheck.True(_sink, wm != null, "（前提）这一轮**有** `WindowsManager`");
        if (wm == null) return;

        string text1 = NetBattle.ReconnectPopupInitial(true);
        NetRuntime.ShowReconnectPopup(text1);
        var win = NetRuntime.ReconnectPopupForTest;
        MenuCheck.True(_sink, win != null, "（前提）掉线窗真的建出来了（`NetRuntime.ReconnectPopupForTest != null`）");
        if (win == null) return;

        MenuCheck.True(_sink, win.PrimaryNode != null && win.PrimaryShown != null,
            "（前提）这一扇是**建过树**的（`Build()` 跑过 ⇒ `PrimaryNode` / `PrimaryShown` 非空）"
          + " —— 否则下面 W2「没有钮」会因为「压根没建」而假绿");

        MenuCheck.True(_sink, !win.PrimaryVisible && string.IsNullOrEmpty(win.SecondaryKey),
            "★ W2 开窗那一刻**一颗钮都没有**（原版 `ShowDisconnectionPopUp.c:51` 那六个 0 实参："
          + "钮文案 / 回调 / `closeOnEsc` 全空）｜🧨 改用 `ShowPopUp(text, OK, …)` ⇒ 本行红（会冒出一颗 OK）");

        int idBefore = win.GetInstanceID();
        string text2 = NetBattle.ReconnectPopup(true, 27);
        NetRuntime.UpdateReconnectPopup(text2);
        var win2 = NetRuntime.ReconnectPopupForTest;
        MenuCheck.True(_sink, win2 != null && win2.GetInstanceID() == idBefore,
            "★ W3 每秒那一拍是**就地换字**，⛔ 不是销毁重建（原版 `UpdateReconnectWindow.c:57-65`："
          + "`popUpWindow` 还活着 ⇒ 只对那颗文字组件 `SetText`）｜🧨 改回 `ShowMessagePopUp`（它会 `Destroy` + `Instantiate`）"
          + " ⇒ 实例 id 变了 ⇒ 本行红（真 Play 上就是每秒闪一下）");

        MenuCheck.True(_sink, win2 != null && win2.MessageShown == text2,   // ⚠️ 与上面那一拍同一串（`text2`）
            "★ W4 换的是**正文**：那一拍之后窗里那串 = `ReconnectPopup(true, 27)`（`LostConnectionMsg` + `00:27`）"
          + "｜🧨 只换实例不换字 / 换了另一档的拼法 ⇒ 本行红");

        NetRuntime.HideNoticePopup();
        MenuCheck.True(_sink, NetRuntime.ReconnectPopupForTest == null,
            "W5 撤窗之后引用作废（`HideNoticePopup` 已清 ⇒ 下一拍会重开一扇，而不是往一只尸体上写字）");
    }
}
