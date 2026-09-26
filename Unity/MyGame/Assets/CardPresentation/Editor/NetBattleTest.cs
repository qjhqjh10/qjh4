// NetBattleTest.cs — 联机**对局**的自检：两个裸 `BattleContext` 走 loopback **真打一局**。
//
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod NetBattleTest.Run -logFile -
//
// 为什么这么验（而不是开两个完整 `BattleDriver`）：**同一工程不能同时跑两个 Unity 实例**
//   （`CLAUDE.md` 铁律 1·b），而两个完整驱动挤在一个场景里会撞静态状态（美术缓存/特效解析器…）。
//   `NetBattle` 要的那几件事收在 `INetBattleHost` 里，**自检实现的就是真对局那条路的同一份**
//   （引擎那段在 `NetApply.Apply`，两边共用）⇒ 验的不是「另一套简化实现」。
//
// 🔴 **它盯的四件事**（都是这次真做的时候会踩的）：
//   ① 端点镜像时**牌堆顺序**必须由主机下发（本地各洗各的 ⇒ 立刻分叉）；
//   ② 座位翻译（`targetP`）只翻一次；
//   ③ **换牌的定序**（`RuleCore.Mulligan` 掷 `ctx.Rng` ⇒ 两端调用顺序必须一样）；
//   ④ 每回合的状态指纹 —— 故意改一边要**报出来**（负例）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CardPresentation;
using CardPresentation.Net;
using RuleEngine;
using UnityEditor;
using UnityEngine;

public static class NetBattleTest
{
    const string P = "[NetBattle] ";
    static int _pass, _fail;

    // ==================================================================
    //  一个「裸 context」的联机宿主（真对局走的是 `BattleDriver`，接口同一套）
    // ==================================================================
    class BareHost : INetBattleHost
    {
        public BattleContext Ctx { get; set; }
        public bool Replayed; public string LastSay;
        public int MySeat;      // 绝对座位：本机是几号
        public int ApplyLoggedAction(MsgAction m)
        {
            return NetApply.Apply(Ctx, m, MySeat, s => Debug.Log(P + "  · " + s));
        }
        public void NetSay(string s) { LastSay = s; }
        public void NetRemoteResign() { RuleCore.Forfeit(Ctx, 1); }   // 对面（本机视角的 1 号位）投降
        /// <summary>重连重放用：按**本自检当初建局的那份输入**重建（真驱动走的是
        /// `BeginFromPendingCore` —— 两边都是「同输入 ⇒ 同状态」）。</summary>
        public Func<BattleContext> Rebuild;
        public void NetReplayFromNet(MsgStart start, List<MsgAction> actions)
        {
            Replayed = true;
            Ctx = Rebuild != null ? Rebuild() : Ctx;
            int ok = 0, bad = 0;
            foreach (var m in actions)
            {
                if (m == null) continue;
                if (NetApply.Apply(Ctx, m, MySeat, s => Debug.Log(P + "  · 重放：" + s)) == RuleCodes.OK) ok++;
                else bad++;
            }
            Debug.Log($"{P}  【客机】重放完毕：{ok} 条落地 / {bad} 条被拒");
        }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        string tmp = Path.Combine(Path.GetTempPath(), "wf_netbattletest.json");
        NetConfig.OverridePath = tmp;
        try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }

        try
        {
            var pool = CardDatabase.Load();
            Ok(pool != null && pool.Count > 100, $"卡池装起来了（{pool?.Count ?? 0} 张）");

            // ---- 1) 两张 socket + 握手 ----
            int port = FreePort();
            var hs = NetSession.NewTcp();      // 主机
            var cs = NetSession.NewTcp();      // 客机
            NetConfig.Current.port = port; NetConfig.Current.ip = "127.0.0.1"; NetConfig.Current.password = "";
            Ok(hs.StartHost(NetConfig.Current), "主机起来监听");
            cs.CheckConnection(NetConfig.Current);
            Ok(PumpUntil(hs, cs, () => hs.State == NetState.Lobby && cs.State == NetState.Lobby, 8000),
               $"握手走完（主机 {hs.State} / 客机 {cs.State}）");

            // ---- 2) 两副牌 + 主机定「种子 / 顺序 / 先手」----
            int seed = 20260926;
            int hostFirst = BattleDriver.FirstSeatForSeed(seed);        // 🔴 主机抽、下发
            var hostDeck0 = DeckBuilder.StarterDeck(pool, "Ultramarines", DeckBuilder.ClassicDeckSize,
                                                    new System.Random(1), unitsOnly: false);
            var cliDeck0 = DeckBuilder.StarterDeck(pool, "Goff", DeckBuilder.ClassicDeckSize,
                                                   new System.Random(2), unitsOnly: false);
            var hostDeck = Shuffle(hostDeck0, seed ^ 0x1111);           // 🔴 顺序由主机洗
            var cliDeck = Shuffle(cliDeck0, seed ^ 0x2222);
            Ok(hostDeck.Count > 20 && cliDeck.Count > 20,
               $"两副牌凑好了（主机 {hostDeck.Count} 张 / 客机 {cliDeck.Count} 张）");

            // ---- 3) 两个 context：**两端构造完全相同**（同一套绝对座位 0/1）★
            //        镜像那套（各把自己当 0 号位）已作废 —— 见 `NetProtocol.Fingerprint` 的注释。
            var hostCtx = RuleCore.NewBattle(hostDeck, cliDeck, seed, cardPool: pool, openMulligan: true,
                                             vars: GameplayVariables.Classic, firstSeat: hostFirst);
            var cliCtx = RuleCore.NewBattle(hostDeck, cliDeck, seed, cardPool: pool, openMulligan: true,
                                            vars: GameplayVariables.Classic, firstSeat: hostFirst);
            var hb = new BareHost { Ctx = hostCtx, MySeat = 0 };
            var cb = new BareHost { Ctx = cliCtx, MySeat = 1 };
            var hNB = NetBattle.Attach(hb, hs, isHost: true);
            var cNB = NetBattle.Attach(cb, cs, isHost: false);
            // 🔴 **必须进 `InBattle`**：`NetSession` 只在「对局中」才走「等重连」那条路
            //    （没开打就断了 = 直接结束）。真驱动里是 `BeginFromPendingCore` 调的。
            hs.EnterBattle(); cs.EnterBattle();

            // 🔴 两端的引擎状态**开局就逐张一致**（座位 0 那副牌在两边是同一条序列）
            var a0 = DeckIds(hostCtx.Players[0]); var a1 = DeckIds(cliCtx.Players[0]);
            int cmp = 0, firstDiff = -1;
            for (int i = 0; i < a0.Count && i < a1.Count; i++) { if (a0[i] != a1[i]) { firstDiff = i; break; } cmp++; }
            Ok(a0.Count == a1.Count && firstDiff < 0,
               $"★ 座位 0 那副牌在两端是**同一条序列**（比了 {cmp} 张 / 共 {a0.Count}，第一处不同 = {firstDiff}）");

            // ---- 4) 换牌：两边各自提交，**主机定序** ----
            Ok(hostCtx.MulliganOpen && cliCtx.MulliganOpen, "两边都进了换牌阶段");
            hNB.OnLocalMulligan(new[] { 0 });                 // 主机换第 1 张
            cNB.OnLocalMulligan(new[] { 0, 1 });              // 客机换前 2 张
            PumpBoth(hs, cs, hNB, cNB, 60);
            Ok(PumpUntil2(hs, cs, hNB, cNB, () => !hostCtx.MulliganOpen && !cliCtx.MulliganOpen, 4000),
               "换牌阶段在两端都结束了（主机定序 → 客机照落）");
            Eq(NetProtocol.Fingerprint(cliCtx), NetProtocol.Fingerprint(hostCtx), "★ 换牌之后**两端指纹一致**");
            for (int p = 0; p < 2; p++)
                Eq(cliCtx.Players[p].Hand.Count, hostCtx.Players[p].Hand.Count, $"座位 {p} 的手牌数两端一致");

            // ---- 5) 脚本对打：谁的行动方谁就用引擎自己枚举出来的合法动作 ----
            int steps = 0, turnsSeen = 0;
            for (int guard = 0; guard < 400 && !hostCtx.IsOver; guard++)
            {
                bool hostActs = hostCtx.Active == 0;      // 绝对座位：0 = 主机那一手
                var ctx = hostActs ? hostCtx : cliCtx;
                var nb = hostActs ? hNB : cNB;
                var act = PickAction(ctx);
                // 🔴 **先按「面板答案已经在队列里」把线上包装配好**（与驱动 `LocalAct` 同一个顺序：
                //    抓答案 → 落地 → 上报）。第一版这里给 `ToWire` 传了 `null` ⇒ 本地那条没有答案、
                //    引擎回落 `ctx.Rng`，而对面收到的是带答案的那份 ⇒ **第 40 步就分叉了**。
                var wire = NetProtocol.ToWire(act, ctx, 0);
                wire.actor = nb.MySeat;      // 🔴 绝对座位（与驱动 `NetBattle.OnLocalAction` 同一件事）
                nb.CaptureLocalAnswers(ctx, act);
                int code = NetApply.Apply(ctx, wire, nb.MySeat);
                if (code != RuleCodes.OK) { Debug.LogWarning(P + " 本机这条被拒了：" + RuleCodes.Describe(code) + " " + act); }
                nb.OnLocalAction(act);
                PumpBoth(hs, cs, hNB, cNB, 12);
                steps++;
                if (ctx.Turn != turnsSeen) turnsSeen = ctx.Turn;
                Debug.Log($"{P} 步 {steps}：{(hostActs ? "主机" : "客机")} 出手 {act} → {RuleCodes.Describe(code)}");

                // **每一步**都抽一次账（别等最后才发现分叉，那样查不出是哪一步坏的）
                {
                    int fh = NetProtocol.Fingerprint(hostCtx), fc = NetProtocol.Fingerprint(cliCtx);
                    if (fh != fc)
                    {
                        DumpTail(hostCtx, "主机");
                        DumpTail(cliCtx, "客机");
                        Fail($"第 {steps} 步两端分叉（主机 {fh} / 客机 {fc}）—— 上一步的动作见上面几行");
                        break;
                    }
                }
            }
            Ok(steps > 5, $"脚本对打跑了 {steps} 步（回合 {hostCtx.Turn}）");
            Eq(NetProtocol.Fingerprint(cliCtx), NetProtocol.Fingerprint(hostCtx),
               "★ 打完**两端状态指纹一致**（这是联机对局唯一真正的正确性判据）");
            Eq(cliCtx.Turn, hostCtx.Turn, "两端回合数一致");
            Eq(cliCtx.Active, hostCtx.Active, "两端「现在轮到谁」一致（**绝对座位**，不再是一正一反）");
            Eq(cliCtx.Events.Count, hostCtx.Events.Count, "两端事件条数一致（引擎记账条数）");
            Eq(hNB.Log.Count, cNB.Log.Count, "★ 权威动作流的条数两端一样（客机的日志靠主机广播补出来）");

            // ---- 6) 重连：**掐断 → 自动重连 → 主机灌权威动作流 → 客机重建 + 全量重放** ----
            {
                var start = new MsgStart
                {
                    seed = seed, mode = "Classic", arena = "Battle", hostFirst = hostFirst,
                    hostFaction = "Ultramarines", clientFaction = "Goff",
                    hostDeckJson = "", clientDeckJson = "",       // 本自检不用它解牌（`Rebuild` 直接重建）
                };
                hNB.RememberStart(start);                         // 主机要留着开局包（`resume` 用）
                cb.Rebuild = () => RuleCore.NewBattle(hostDeck, cliDeck, seed, cardPool: pool,
                                                      openMulligan: true,
                                                      vars: GameplayVariables.Classic, firstSeat: hostFirst);

                int hBefore = NetProtocol.Fingerprint(hostCtx), cBefore = NetProtocol.Fingerprint(cliCtx);
                cs.Transport.ClosePeer();                         // 🔴 模拟客机掉线
                Ok(PumpUntil(hs, cs, () => hs.State == NetState.WaitingReconnect, 6000),
                   $"主机发现客机掉线、进等待重连（实际 {hs.State}）");
                Ok(hs.State != NetState.Closed, "🔴 **没有判负** —— 停在等待重连（用户 2026-09-26 要的就是这个）");
                Ok(PumpUntil2(hs, cs, hNB, cNB, () => cb.Replayed, 20000),
                   $"客机自动重连上并收到了 `resume`（实际 {cs.State}）");
                Ok(cb.Replayed, "★ 客机**走了重建 + 全量重放**那条路（`NetReplayFromNet`）");
                Eq(NetProtocol.Fingerprint(cliCtx), hBefore,
                   "★ **重放之后客机的状态追平了主机**（这是「掉线能接着打」的唯一判据）");
                Eq(NetProtocol.Fingerprint(cliCtx), cBefore, "（顺带：重放出来的状态与掉线前一致）");
                Debug.Log($"{P}  重连前后：主机 {hBefore} · 客机(掉线前) {cBefore} · 客机(重放后) "
                        + $"{NetProtocol.Fingerprint(cliCtx)}");
            }

            // ---- 7) 负例：故意改一边 ⇒ 指纹检查必须**报出来** ----
            hostCtx.Players[0].Energy += 3;
            hNB.SendFingerprint(); cNB.SendFingerprint();
            PumpBoth(hs, cs, hNB, cNB, 120);
            Ok(hNB.Aborted || cNB.Aborted,
               "★ 负例：故意把一边的能量改掉 ⇒ **指纹检查报出打岔并中止**（不静默）");
        }
        catch (Exception e)
        {
            Fail("自检本身炸了：" + e);
        }
        finally
        {
            NetConfig.OverridePath = null;
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }

        Debug.Log($"{P} ===== 通过 {_pass} · 失败 {_fail} =====");
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    // ==================================================================
    //  工具
    // ==================================================================

    /// <summary>挑一条**合法**动作（引擎自己枚举 —— 不手写「什么能出」，那样测的是我而不是引擎）。</summary>
    static AiAction PickAction(BattleContext ctx)
    {
        var list = SimpleAI.EnumerateActions(ctx);
        if (list != null)
            foreach (var a in list)
                if (a.Kind != AiActionKind.EndTurn) return a;
        return new AiAction { Kind = AiActionKind.EndTurn };
    }

    static List<CardDef> Shuffle(List<CardDef> src, int seed)
    {
        var l = new List<CardDef>(src);
        var r = new System.Random(seed);
        for (int i = l.Count - 1; i > 0; i--) { int j = r.Next(i + 1); var t = l[i]; l[i] = l[j]; l[j] = t; }
        return l;
    }

    static List<string> DeckIds(PlayerState p)
    {
        var l = new List<string>();
        foreach (var c in p.Deck) l.Add(c != null && c.Card != null ? c.Card.Id : null);
        return l;
    }

    static int FreePort()
    {
        var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        l.Start(); int p = ((System.Net.IPEndPoint)l.LocalEndpoint).Port; l.Stop(); return p;
    }

    static bool PumpUntil(NetSession a, NetSession b, Func<bool> cond, int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            a.Pump(); b.Pump();
            if (cond()) return true;
            Thread.Sleep(5);
        }
        a.Pump(); b.Pump();
        return cond();
    }

    static void PumpBoth(NetSession hs, NetSession cs, NetBattle hNB, NetBattle cNB, int ms)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < ms)
        {
            hs.Pump(); cs.Pump();
            hNB.Tick(); cNB.Tick();
            Thread.Sleep(3);
        }
        hs.Pump(); cs.Pump(); hNB.Tick(); cNB.Tick();
    }

    static bool PumpUntil2(NetSession hs, NetSession cs, NetBattle hNB, NetBattle cNB,
                           Func<bool> cond, int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            hs.Pump(); cs.Pump(); hNB.Tick(); cNB.Tick();
            if (cond()) return true;
            Thread.Sleep(3);
        }
        return cond();
    }

    /// <summary>分叉时把两边的**事件尾巴**打出来 —— 「多了一条什么」比两个哈希值有用得多。</summary>
    static void DumpTail(BattleContext ctx, string who)
    {
        int n = ctx.Events.Count;
        int from = Math.Max(0, n - 8);
        var sb = new System.Text.StringBuilder();
        for (int i = from; i < n; i++) sb.Append("\n    " + (i + 1) + ". " + ctx.Events[i]);
        Debug.LogWarning($"{P} [{who}] 共 {n} 条事件，最后 8 条：{sb}");
        for (int p = 0; p < 2; p++)
        {
            var ps = ctx.Players[p];
            Debug.LogWarning($"{P} [{who}] P{p}：血 {ps?.Warlord?.Health} · 能量 {ps?.Energy}/{ps?.MaxEnergy}"
                           + $" · 手 {ps?.Hand.Count} · 库 {ps?.Deck.Count} · 弃 {ps?.Discard.Count}");
        }
    }

    static void Ok(bool cond, string msg)
    {
        if (cond) { _pass++; Debug.Log(P + " ✓ " + msg); return; }
        Fail(msg);
    }
    static void Fail(string msg) { _fail++; Debug.LogError(P + " ✗ " + msg); }
    static void Eq(int got, int want, string msg) { Ok(got == want, $"{msg}（实际 {got}，应为 {want}）"); }
}
