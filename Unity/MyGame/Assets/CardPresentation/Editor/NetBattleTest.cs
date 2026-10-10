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
    /// <summary>⚠️ `_warn` = **本宿主里验不了、如实出声**的那几格（不算失败，同 `NetSelfTest` 那位先例）。
    /// 为什么要它：编辑模式**不派生命周期消息**，而本项目有两处要验的正是生命周期那一跳
    /// （`OnDestroy` 捎 `bye`）—— 那几格改断「环境事实」+ `_warn`，**别写成绿**（铁律 11）。</summary>
    static int _pass, _fail, _warn;

    // ==================================================================
    //  一个「裸 context」的联机宿主（真对局走的是 `BattleDriver`，接口同一套）
    // ==================================================================
    class BareHost : INetBattleHost
    {
        public BattleContext Ctx { get; set; }
        public bool Replayed; public string LastSay;
        public int MySeat;      // 绝对座位：本机是几号
        /// <summary>🆕 2026-10-17（B27·A912）：**判负入口被调过几次**。
        /// 🔴 为什么要数它、而不是只看 `Ctx.Winner`：`RuleCore.Forfeit` 自己那条「已经判过就不再判」的早退
        ///    （`RuleCore.cs:3685`）会把「**已经打完的局又判了一次**」这个错**完全吃掉**
        ///    ⇒ 只盯 `Winner` 的话，把 `Ctx.IsOver` 那道闸删掉也一样绿（两边一起变）。</summary>
        /// <summary>🆕 2026-10-17（B27·A912）：**判负入口被调过几次**。
        /// 🔴 为什么要数它、而不是只看 `Ctx.Winner`：`RuleCore.Forfeit` 自己那条「已经判过就不再判」的早退
        ///    （`RuleCore.cs:3685`）会把「**已经打完的局又判了一次**」这个错**完全吃掉**
        ///    ⇒ 只盯 `Winner` 的话，把 `Ctx.IsOver` 那道闸删掉也一样绿（两边一起变）。</summary>
        public int ResignCalls;
        /// <summary>🆕 **2026-10-18（A914 第四续）**：**`NetSelfResign`（判本机负）被调过几次**。
        /// 与 `ResignCalls` 分开数 —— 两端判的**不是同一个人**（本端判 `MySeat`、对面端判 `1-MySeat`），
        /// 只数一个计数器就分不出「谁该判谁」（那正是 R3 那半条要钉的东西）。</summary>
        public int SelfResignCalls;
        /// <summary>🆕 **2026-10-18（A913）**：**两个判负口各自收到的理由码**
        /// （原版 `DeadHero(bool, BattleResult)` 的**第二个实参**）。
        /// 🔴 它与 `RuleCore.Forfeit` 里那句 `ctx.Log`（`Ctx.Events` 上的 `[BattleResult] …` token）是
        /// **两个不同源**的观测口：这一格盯「**调用方传的是哪个码**」，
        /// 那个 token 盯「**引擎有没有照码落地**」——
        /// 把 `Forfeit` 里那句 `reason` 写死 ⇒ **只有 token 那组红**；把调用点的码传错 ⇒ **只有这一格红**。
        /// ⚠️ `HasRemoteReason` / `HasSelfReason`：那两个口被调过没有 —— 没有它，
        /// 「没调过」与「传了 `Undefined`(0)」在读出来的值上**一模一样**（弱断言）。</summary>
        public BattleResult LastResignReason, LastSelfResignReason;
        public bool HasRemoteReason, HasSelfReason;
        public int ApplyLoggedAction(MsgAction m)
        {
            return NetApply.Apply(Ctx, m, MySeat, s => Debug.Log(P + "  · " + s));
        }
        public void NetSay(string s) { LastSay = s; }
        /// <summary>对面投降 ⇒ 判**对面**负。
        /// 🔴 **2026-10-18 订正**：原来这里把座位写死成 `1`（「1 号位 = 对面」）—— 那只对 `MySeat == 0` 成立。
        ///    改成 `1 - MySeat`（与真宿主 `BattleDriver.NetRemoteResign` 的 `1 - _me` **同口径**）。
        ///    ⚠️ 这不只是好看：R3 那条断言要能分辨「判自己 vs 判对面」，**桩要是把座位写死，就分不出来**
        ///    （`MySeat == 1` 时两边的结果会**碰巧一样** ⇒ 断言恒绿）。</summary>
        public void NetRemoteResign(BattleResult reason)
        {
            ResignCalls++; LastResignReason = reason; HasRemoteReason = true;
            RuleCore.Forfeit(Ctx, 1 - MySeat, reason);
        }
        /// <summary>🆕 2026-10-18（A914 第四续）：**本机自己**判负（判 `MySeat`）。
        /// 与上面那个**只差一个座位号** —— 这正是要钉的那一格。
        /// 🆕 **A913**：理由码照收照传（观测口见 `LastResignReason` 那一段）。</summary>
        public void NetSelfResign(BattleResult reason)
        {
            SelfResignCalls++; LastSelfResignReason = reason; HasSelfReason = true;
            RuleCore.Forfeit(Ctx, MySeat, reason);
        }
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
        _pass = 0; _fail = 0; _warn = 0;
        string tmp = Path.Combine(Path.GetTempPath(), "wf_netbattletest.json");
        NetConfig.OverridePath = tmp;
        try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }

        try
        {
            // 🔴 **自检把 `NetBattle` 倒计时读的那个钟换成假的**（`SetClockForTest`）—— **这一句要先做**。
            //   不换的话：§5 / §6b / §9 那三段都是**故意**让对面掉线、再等最多 20~25 秒重连，
            //   而「掉线那一刻」起的是一条**真实 30 秒**的倒计时（A900）⇒ 只要那几段加起来耗过 30 秒，
            //   它就会**判一场已经接回来的局的负**（后面的指纹对账会跟着红，而且**红得没道理**）。
            //   换成假钟（初值 0）⇒ **只有 §10 明确推它时才走**，这条自检就不是计时敏感的了。
            //   ⚠️ `NetSession` 自己那个钟（心跳 / 静默超时 / 重连退避）读的是它自己的 `Stopwatch`，
            //      **不受影响** —— 这里换的只有 `NetBattle` 倒计时那一个。
            _clk = 0f;
            NetBattle.SetClockForTest(FakeClock);

            var pool = CardDatabase.Load();
            Ok(pool != null && pool.Count > 100, $"卡池装起来了（{pool?.Count ?? 0} 张）");

            // ---- 0) 🆕 2026-10-17（F4·RC1）：**主机开台过了 10 秒之后才来的客机，也必须收得下** ----
            //   判据链（逐句可核 → `资料/普查产出_1017/D4_联机诊断.md` §②，日志实证 `netbattle.log:5674-5676`）：
            //     · `SilentTimeoutMs = 10000`；主机侧 `_lastRecvMs` **从没赋过初值**（字段默认 0），
            //       而 `NowMs` 是**进程级**秒表 ⇒ 进程起来满 10 秒之后，**任何一条新连接**都会在
            //       「一条包都没见过」的情况下被判成「10 秒没收到对面的任何消息」；
            //     · 要害是 `Pump()` 里的**次序**：静默超时判在【接受新连接】那一支**之前** ⇒
            //       接受那一刻补的 `_lastRecvMs = now` 来不及救这一次判定（客机的第一个包要等主机发 `Challenge`）。
            //   ⇒ 修法两处（`Net/NetSession.cs`）：① `StartHost` 补初值（与客机 `InternalConnect` 对称）；
            //     ② **把「接受新连接」那一支挪到心跳/超时块之前**。
            //   🔴 **为什么用可注入的钟**：不换就得**真睡 10 秒**（同 `NetBattle.SetClockForTest` 的先例）。
            //      ⚠️ 这一段**只**换 `NetSession` 那个钟（`NetBattle` 倒计时那个 `_clk` 不动）；
            //      段末在 `finally` 里还回真实秒表（进程级静态状态别留给后面的段）。
            //   ⚠️ 假钟是**冻住**的 ⇒ 这段里心跳/退避都不走，握手本身是**事件驱动**的，照样走完（确定性）。
            {
                long nsClock = 0;                                  // 假钟（毫秒）
                NetSession.SetClockForTest(() => nsClock);
                NetSession hs0 = null, cs0 = null;
                try
                {
                    nsClock = 30000;           // 「进程已经起来 30 秒」—— 真机上玩家点「当主机」时就是这样
                    int port0 = FreePort();
                    NetConfig.Current.port = port0; NetConfig.Current.ip = "127.0.0.1";
                    NetConfig.Current.password = "";
                    hs0 = NetSession.NewTcp(); cs0 = NetSession.NewTcp();
                    int hostLost0 = 0;
                    hs0.OnPeerLost = () => hostLost0++;

                    Ok(hs0.StartHost(NetConfig.Current), "（RC1）主机起来监听");
                    Ok(hs0.SinceLastRecvMsForTest <= 1000,
                       $"★（RC1）**开台那一刻静默基线就是当刻**（距上一次收包 {hs0.SinceLastRecvMsForTest} ms）"
                     + " —— 这一格钉的是 `StartHost` 里那句 `_lastRecvMs = NowMs;`"
                     + "；🧨 判别式：删掉那句 ⇒ 基线变成「进程秒表的全程」（这里就是 30000 ms）⇒ 本条红");

                    // 主机**一个人**在台前等着，让钟推过静默阈值 —— 没人在，不许自己判自己掉线
                    nsClock += NetSession.SilentTimeoutMs + 5000;
                    hs0.Pump(); hs0.Pump();
                    Ok(hs0.State == NetState.Listening && hostLost0 == 0,
                       $"（RC1）台开着、**还没人连进来** ⇒ 时钟越过 {NetSession.SilentTimeoutMs / 1000} 秒"
                     + $"也**不许**自己判掉线（实际 {hs0.State} · `OnPeerLost` {hostLost0} 次）");

                    // 客机**这时候才**连进来 —— 真机上「主机早就在等」的那一档
                    cs0.CheckConnection(NetConfig.Current);
                    Ok(PumpUntil(hs0, cs0, () => hs0.State == NetState.Lobby && cs0.State == NetState.Lobby, 8000),
                       $"★（RC1）**主机先开台（钟已越过 {NetSession.SilentTimeoutMs / 1000} 秒）→ 客机才连 ⇒ 照样握上手**"
                     + $"（主机 {hs0.State} / 客机 {cs0.State}）"
                     + " —— 🧨 判别式：把「接受新连接」那一支挪回静默超时判**之后** ⇒ 主机当场判自己掉线"
                     + "（红，且是 `netbattle.log:5674-5676` 那三行的原样重现）");
                    Ok(hostLost0 == 0,
                       $"★（RC1）…而且主机**一次都没判过「对面掉线」**（`OnPeerLost` 触发 {hostLost0} 次）"
                     + " —— 与上一条**不同源**：上一条只看终态，这一条钉住「中途掐过一下再握回来」也算缺陷"
                     + "（那正是日志里 `WaitingReconnect → Handshaking` 那个**倒退**）");

                    // 对照：**真的安静 10 秒**这条路没被堵死（只推主机，让对面一个包都不回）
                    nsClock += NetSession.SilentTimeoutMs + 1000;
                    Ok(PumpOne(hs0, () => hs0.State == NetState.WaitingReconnect, 4000) && hostLost0 >= 1,
                       $"（RC1·对照）**对面真的安静了 {NetSession.SilentTimeoutMs / 1000} 秒 ⇒ 照样判掉线**"
                     + $"（实际 {hs0.State} · `OnPeerLost` {hostLost0} 次）"
                     + " —— 这一条防的是「为了让上面两条过，把静默超时整个关掉/堵死」");
                }
                finally
                {
                    NetSession.SetClockForTest(null);              // 🔴 还回真实秒表（进程级静态状态）
                    if (hs0 != null) hs0.Close(false);
                    if (cs0 != null) cs0.Close(false);
                }
            }

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

            // ---- 4b) 🆕 2026-09-30：**进攻卡 / 防御卡**那两条的同步 ★
            //   我们自己的设计（原版两台各弹各的、**没有这条同步包**）—— 判据 →
            //   `NetActionKind.OffensivePick` 的注释。**不同步的后果是静默的**：两端各切各的环境，
            //   后手那张防御卡只有一端有 ⇒ 指纹迟早不一致。走的是现成的 `Action`/`Applied` 那条流。
            {
                int firstSeat = hostCtx.FirstSeat, secondSeat = hostCtx.SecondSeat;
                var nbFirst = firstSeat == 0 ? hNB : cNB;      // 先手方选**进攻卡**
                var nbSecond = secondSeat == 0 ? hNB : cNB;    // 后手方选**防御卡**
                int envSlot = 1; string envSO = "TestEnv/A";   // 值是任取的：这一节验的是**两端一致**
                CardDef defCard = null;
                for (int i = 0; i < pool.Count; i++)
                    if (pool[i] != null && pool[i].Type == "defence") { defCard = pool[i]; break; }
                string defId = defCard != null ? defCard.Id : null;

                // 两端各自**先落地**（乐观执行，与 `OnLocalAction` 同一条规矩），再发出去。
                // 🔴 本地这一半必须与对面收到后做的是**同一组调用**（`NetApply` 那两条分支）——
                //    否则「一端做了、另一端没做」，那正是这一节要防的事。
                var firstCtx = firstSeat == 0 ? hostCtx : cliCtx;
                var secondCtx = secondSeat == 0 ? hostCtx : cliCtx;
                RuleCore.ChooseOffensiveCard(firstCtx, firstSeat, envSlot, envSO);
                nbFirst.OnLocalRawAction(new MsgAction
                { kind = NetActionKind.OffensivePick, envSlot = envSlot, envSO = envSO });
                RuleCore.ChooseDefensiveCard(secondCtx, secondSeat, 0);
                if (defCard != null) RuleCore.SetDefensiveCard(secondCtx, secondSeat, defCard);
                nbSecond.OnLocalRawAction(new MsgAction
                { kind = NetActionKind.DefensivePick, envSlot = 0, defId = defId });

                Ok(PumpUntil2(hs, cs, hNB, cNB,
                              () => hostCtx.OffensiveChosen && cliCtx.OffensiveChosen
                                 && hostCtx.DefensiveSlotIdx >= 0 && cliCtx.DefensiveSlotIdx >= 0, 4000),
                   "★ 进攻卡 / 防御卡那两条在**两端都落地了**");
                Eq(cliCtx.OffensiveSeat, hostCtx.OffensiveSeat, "…选进攻卡的是**同一方**");
                Eq(cliCtx.OffensiveSlotIdx, hostCtx.OffensiveSlotIdx, "★ 两端的环境**槽号**一致");
                Ok(string.Equals(cliCtx.OffensiveEnvSO, hostCtx.OffensiveEnvSO),
                   $"★ 两端的环境 **SO 逐字一致** —— 这才是「换哪条环境」的判据"
                 + $"（客机 `{cliCtx.OffensiveEnvSO}` / 主机 `{hostCtx.OffensiveEnvSO}`）");
                Eq(cliCtx.DefensiveSlotIdx, hostCtx.DefensiveSlotIdx, "★ 两端的防御卡槽号一致");
                if (defId != null)
                    Eq(cliCtx.Players[secondSeat].Hand.Count, hostCtx.Players[secondSeat].Hand.Count,
                       "★ 防御卡**换进手牌**那一步也两端一致（只同步下标的话这一步会缺）");
                Ok(true, $"（这一节：先手 = 座位 {firstSeat}、后手 = 座位 {secondSeat}；"
                       + $"防御卡 Id = `{defId ?? "<卡池里没有 defence 卡>"}`）");
            }

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
                // 🔴 **2026-09-27 修一处「断言取错了对象」**：重放时 `NetReplayFromNet` 会 **`Ctx = Rebuild()`**
                //    —— 换成了一个**新建的** context。上面那个局部 `cliCtx` 从此指向**掉线前那一个**，
                //    而它当然等于 `cBefore`（掉线前两端本来就一致）⇒ 原来那两条断言**恒真**、
                //    **根本没验到重放的结果**。要问的是**活着的那个**（`cb.Ctx`）。
                Eq(NetProtocol.Fingerprint(cb.Ctx), hBefore,
                   "★ **重放之后客机的状态追平了主机**（这是「掉线能接着打」的唯一判据）");
                Eq(NetProtocol.Fingerprint(cb.Ctx), cBefore, "★ 重放出来的状态**与掉线前一致**（重放是确定性的）");
                Debug.Log($"{P}  重连前后：主机 {hBefore} · 客机(掉线前) {cBefore} · 客机(重放后) "
                        + $"{NetProtocol.Fingerprint(cb.Ctx)}");
            }

            // ---- 6b) **主机掉线**那一支（原来只测了客机那一边）----
            //   🔴 **两边掉的不是同一条路**：客机掉线考的是「客机自己重连」；
            //      主机这一侧掉线时，**发起重连的是客机**，而主机要做的是
            //      **① 停在等待重连（不判负）② 用 `ClosePeer` 而不是 `Close`（监听必须留着）**
            //      —— 后者是这条支路唯一会「静默永远连不回来」的地方，所以必须有断言钉住。
            {
                int hBefore = NetProtocol.Fingerprint(hb.Ctx), cBefore = NetProtocol.Fingerprint(cb.Ctx);
                hs.Transport.ClosePeer();                     // 🔴 **主机**这一侧的连接断了（**监听还在**）
                Ok(PumpUntil(hs, cs, () => hs.State == NetState.WaitingReconnect, 6000),
                   $"主机自己发现连接断了 ⇒ 进等待重连（实际 {hs.State}）");
                Ok(hs.State != NetState.Closed,
                   "🔴 主机**没有退出对局**（停在等待重连 —— 掉线不判负，两边一样）");
                cb.Replayed = false;
                Ok(PumpUntil2(hs, cs, hNB, cNB, () => cb.Replayed, 20000),
                   $"★ 客机自动重连**回到主机**并收到 `resume`（实际 客机 {cs.State}）");
                Ok(cb.Replayed, "★ 走的是同一条「重建 + 全量重放」");
                Eq(NetProtocol.Fingerprint(cb.Ctx), hBefore,
                   "★ **主机掉线这一支**：重连后客机也追平了主机");
                Ok(cs.State == NetState.InBattle, $"两边回到 `InBattle`（能接着打）—— 客机实际 {cs.State}");
                Ok(hs.State == NetState.InBattle, $"主机那一侧也回了 `InBattle` —— 实际 {hs.State}");
                Debug.Log($"{P}  主机掉线前后：主机 {hBefore} · 客机(掉线前) {cBefore} · 客机(重放后) "
                        + $"{NetProtocol.Fingerprint(cb.Ctx)}");
            }

            // ---- 6c) 「本该问玩家」的选择点：**另开一局逼出来**（上面那一局打完时 `ChooseSites` 是 0）----
            //  🔴 判据（`BattleContext` 的两个计数器）：`ChooseSites` = 引擎问了几次，
            //     `ChooseAnswered` = 其中**用了面板答案**的几次；差着的那几次 = **引擎替玩家挑的**
            //     （联机里就是「问不到对面 ⇒ 回落 `ctx.Rng`」，见 `资料/联机P2P_设计与交接.md` §5·5 第 1 条）。
            //  ⚠️ **为什么另开一局**：① 主循环那一局**打到 `IsOver` 才停** ⇒ 收尾时**打不出任何牌**；
            //     ② 塞卡是「手改局面」，塞进主那一局会**当场把 6a/6b 的重放对账打红**（本轮先试过，实证）。
            //     ⇒ 新开一段：新端口 + 新 socket + 新 context，只验这一件事，**不碰主那一局**。
            {
                int savedPort = NetConfig.Current.port;
                int port2 = FreePort();
                NetConfig.Current.port = port2;
                var hs2 = NetSession.NewTcp(); var cs2 = NetSession.NewTcp();
                Ok(hs2.StartHost(NetConfig.Current), "（选择点·另开一局）主机起来监听");
                cs2.CheckConnection(NetConfig.Current);
                Ok(PumpUntil(hs2, cs2, () => hs2.State == NetState.Lobby && cs2.State == NetState.Lobby, 8000),
                   "（选择点·另开一局）握手走完");

                int seed2 = 20260927;
                var da = Shuffle(DeckBuilder.StarterDeck(pool, "Ultramarines", DeckBuilder.ClassicDeckSize,
                                                         new System.Random(11), unitsOnly: false), seed2 ^ 0x33);
                var db = Shuffle(DeckBuilder.StarterDeck(pool, "Goff", DeckBuilder.ClassicDeckSize,
                                                         new System.Random(12), unitsOnly: false), seed2 ^ 0x44);
                var ctxA = RuleCore.NewBattle(da, db, seed2, cardPool: pool, openMulligan: true,
                                              vars: GameplayVariables.Classic, firstSeat: 0);
                var ctxB = RuleCore.NewBattle(da, db, seed2, cardPool: pool, openMulligan: true,
                                              vars: GameplayVariables.Classic, firstSeat: 0);
                var ha2 = new BareHost { Ctx = ctxA, MySeat = 0 };
                var hb2 = new BareHost { Ctx = ctxB, MySeat = 1 };
                var na = NetBattle.Attach(ha2, hs2, isHost: true);
                var nc = NetBattle.Attach(hb2, cs2, isHost: false);
                hs2.EnterBattle(); cs2.EnterBattle();

                na.OnLocalMulligan(new int[0]); nc.OnLocalMulligan(new int[0]);
                PumpBoth(hs2, cs2, na, nc, 60);
                Ok(PumpUntil2(hs2, cs2, na, nc, () => !ctxA.MulliganOpen && !ctxB.MulliganOpen, 4000),
                   "（选择点·另开一局）换牌走完");
                Eq(NetProtocol.Fingerprint(ctxB), NetProtocol.Fingerprint(ctxA),
                   "（选择点·另开一局）开局两端一致");

                CardDef choiceCard = null;
                foreach (var c in pool) if (c != null && c.Name == "Exemplary Warrior") { choiceCard = c; break; }
                Ok(choiceCard != null, "卡池里有那张带**三选一**的战术卡 `Exemplary Warrior`（引擎自检用的同一张）");
                if (choiceCard != null && !ctxA.IsOver)
                {
                    int a = ctxA.Active;                       // 现在轮到哪一方（这一局 firstSeat=0）
                    // 🔴 **两边同序地塞进手牌**（自检的布置，不是产品路径；两边必须一模一样）
                    ctxA.Players[a].Hand.Add(ctxA.NewInstance(choiceCard));
                    ctxB.Players[a].Hand.Add(ctxB.NewInstance(choiceCard));
                    ctxA.Players[a].Energy = 20; ctxB.Players[a].Energy = 20;
                    int hIdx = ctxA.Players[a].Hand.Count - 1;

                    // ⚠️ **别走 `SimpleAI.EnumerateActions`**：它按格位枚举（`CanPlayCard(ctx, me, i, s)`），
                    //    而战术卡在 `-1`（无目标）那一档 ⇒ **枚举里根本不会有它**（实测：找不到那条）。
                    //    ⇒ 照**驱动那条路**直接构造（`BattleDriver.SimulatePlay` 就是把 `Slot` 传下去）。
                    var act = new AiAction { Kind = AiActionKind.PlayCard, HandIdx = hIdx, Slot = -1 };
                    int can = RuleCore.CanPlayCard(ctxA, a, hIdx, -1);
                    Ok(can == RuleCodes.OK, $"那张卡现在打得出去（`CanPlayCard` = {RuleCodes.Describe(can)}）");
                    if (can == RuleCodes.OK)
                    {
                        var nb2 = a == 0 ? na : nc;
                        var wire2 = NetProtocol.ToWire(act, ctxA, 0);
                        wire2.actor = nb2.MySeat;
                        nb2.CaptureLocalAnswers(ctxA, act);   // 本自检**没有表现层面板** ⇒ 队列是空的（要的就是这一支）
                        int code2 = NetApply.Apply(ctxA, wire2, nb2.MySeat);
                        nb2.OnLocalAction(act);
                        PumpBoth(hs2, cs2, na, nc, 12);
                        Debug.Log($"{P}  逼出来的那一手：座位 {a} 打出 `Exemplary Warrior` → {RuleCodes.Describe(code2)}");

                        Ok(ctxA.ChooseSites > 0,
                           $"★ 这一手**真的问了玩家**（`ChooseSites` = {ctxA.ChooseSites}）");
                        Eq(ctxA.ChooseAnswered, 0,
                           "这次**没有面板答案**（本自检没有表现层）⇒ 走的就是**引擎兜底**那一支");
                        Eq(ctxB.ChooseSites, ctxA.ChooseSites, "★ 两端**问了几次**一致");
                        Eq(ctxB.ChooseAnswered, ctxA.ChooseAnswered, "★ 两端**答了几次**一致");
                        Eq(NetProtocol.Fingerprint(ctxB), NetProtocol.Fingerprint(ctxA),
                           "★ **引擎替玩家挑的那一支：两端挑出来的是同一个**（同种子 + 同顺序 ⇒ 指纹仍然一致）");
                    }
                }
                hs2.Close(); cs2.Close();
                NetConfig.Current.port = savedPort;          // 🔴 还原（后面那节负例还用主那套会话）
            }

            // ---- 7) 负例：故意改一边 ⇒ 指纹检查必须**报出来** ----
            hostCtx.Players[0].Energy += 3;
            hNB.SendFingerprint(); cNB.SendFingerprint();
            PumpBoth(hs, cs, hNB, cNB, 120);
            Ok(hNB.Aborted || cNB.Aborted,
               "★ 负例：故意把一边的能量改掉 ⇒ **指纹检查报出打岔并中止**（不静默）");

            // ---- 8) 🆕 2026-10-13（A530）：开局日志的**三档措辞**里，联机 / 重连那两档 ----
            //   **A410** 把 `BattleDriver.BeginFromPendingCore` 里那句**写死**的「联机开局」改成了
            //   **按实情分三档**（判据 = `attachNet` + `_net`）：
            //     档① `attachNet: true`                              ⇒ `[Net] 联机开局`
            //     档② `attachNet: false` + `_net != null`（重连重建）  ⇒ `[Net] 联机重建（重连）`
            //     档③ `attachNet: false` + `_net == null`（放录像）    ⇒ `[Replay] 回放开局`
            //   `Editor/BattleScene.cs` 的 A410 段（它在回放那一节里）**只跑得到档③** —— 跑到那儿时
            //   `driver.AttachNet(null)` 已经把联机层摘了 ⇒ **档①② 的措辞只有本文件能验**（账 = A530）。
            //
            //   ⚠️ **为什么要在这儿自己造一台裸 `BattleDriver`**：那句日志在 `BattleDriver` 里，
            //      而本自检的 `BareHost` 走的是**另一条路**（裸 `BattleContext`，压根碰不到驱动）；
            //      建一台**完整**驱动（`interaction` / 棋盘 / 手牌 / 两台相机…）是 `Editor/BattleScene.cs`
            //      的活（那也是本工程跑得最慢的一条自检）⇒ 不能为了这两句措辞把它拖进来。
            //   🔴 **判据在日志那一句上，而它在 `Begin(...)` 之前就打出来了**
            //      （源码顺序：标签 → `Debug.Log($"{kindNet}：…")` → `Begin(...)`）
            //      ⇒ 这台裸驱动没有 `interaction`（`Begin` 里**紧跟 `BuildHud()` 之后**那几处 `interaction.…`
            //      —— `CanDropAtSlot = …` / `DropLandingSlot = …` / `OnDropPreview -= …` —— **都没有守卫**）
            //      时 `Begin` 会中断在那一处 —— **那个异常不当失败**（要的东西已经拿到），
            //      但**必须如实打出来**（红线：不许静默），见 `NoteProbeThrow`。
            //
            //   🧨 **改坏法（必须红）**：
            //     ① 把标签改回**二值分流**（`attachNet ? "联机开局" : "回放开局"`）⇒ 档② 红
            //        （**重连被说成「放录像」—— 这正是 A410 报告里点名的那条陷阱**：光按 `attachNet`
            //        分不出来，因为 `NetReplay` 走的也是 `attachNet: false`）；
            //     ② 把档② 也写成「联机开局」⇒ 档② 的两条互斥断言一起红；
            //     ③ 把整句日志删掉 ⇒ 档①② 都红（红线：不许静默失败）。
            //   ⚠️ **档③ 不在这里重复验**（`Editor/BattleScene.cs` 的 A410 段已经验了「不是联机开局」
            //      +「是回放开局」，而且它还带着指纹对账）—— 同一条规矩不写第二份（两处迟早不一致）。
            {
                var startA530 = new MsgStart
                {
                    seed = 20261014, mode = "Classic", arena = "Battle", hostFirst = 0,
                    hostFaction = "Ultramarines", clientFaction = "Goff",
                    hostDeckJson = "", clientDeckJson = "",     // 与 §6 同一个取舍：这一趟不靠它解牌
                };
                var goA530 = new GameObject("A530_OpeningLabelProbe");
                var probe = goA530.AddComponent<BattleDriver>();
                try
                {
                    // ---- 8a) 档① **真联机开局** —— 走产品入口 `BeginFromDeckLibrary()` ----
                    //   它的联机那一支在**方法最前面**（`NetPendingBattle.Take()` ≠ null ⇒ 整条走开局包、
                    //   立刻 `return`），够不到后面那条「读玩家卡组」的路 ⇒ 这里摆一份开局包进去是安全的。
                    //   ⚠️ 下面两条是**夹具前提**（不是 A530 的判据）：开局包要是没摆进去 / 解不出来，
                    //      `BeginFromDeckLibrary` 会**落到 `PickSavedDeck()` 那条路去读玩家的真卡组**
                    //      —— 自检不该碰玩家的真卡组 ⇒ 两条一起把这条路钉死。
                    var pbNetA530 = NetPendingBattle.FromStart(startA530, isHost: true);
                    NetPendingBattle.Current = pbNetA530;
                    Ok(pbNetA530 != null, "（A530·档①）夹具：联机开局包造出来了（`FromStart` 非 null）");
                    //   起手这一格 `_net == null` ⇒ 「联机开局」这五个字**只可能**来自 `attachNet` 那一支
                    //   （而档③ 的判据也正好是这一格）⇒ 下面那条断言因此是**能分状态**的，不是恒真。
                    Ok(probe.AiShouldDriveOpponent,
                       "（A530·档①）夹具：起手 `_net == null`（`AiShouldDriveOpponent` = true，它是 `_net == null` 的公开证人）");
                    Exception err1;
                    var log1 = CaptureLogs(() => probe.BeginFromDeckLibrary(), out err1);
                    NoteProbeThrow(err1, "档①");
                    Ok(!HasLog(log1, "[Battle] 本局模式："),
                       "（A530·档①）夹具：**没落到「读玩家卡组」那条路**（那条路的 `[Battle] 本局模式：…` 一条都没出现）");
                    Ok(HasLog(log1, "[Net] 联机开局："),
                       $"★（A530·档①）真联机开局：日志自称「联机开局」（这一趟抓到 {log1.Count} 条日志）");
                    Ok(!HasLog(log1, "[Net] 联机重建") && !HasLog(log1, "[Replay] 回放开局"),
                       "★（A530·档①）…而且**没说成另外两档** —— 三档互斥，不是「联机 / 不是联机」二值");

                    // ---- 8b) 档② **重连重建** —— 走产品入口 `NetReplay()` ----
                    //   生产里它是 `NetBattle` 收到 `resume` 时调的（`NetReplayFromNet`），
                    //   而那一趟**必然**已经挂着联机层 ⇒ 先按同一状态把层挂上：这一步就是档② 与档③ 的**唯一分界**。
                    //   ⚠️ 会话传 `null`（同 `Editor/BattleScene.cs` 那条联机探针
                    //      `NetBattle.Attach((INetBattleHost)driver, null, isHost: true)`）—— 本段不碰网络。
                    var nbA530 = NetBattle.Attach((INetBattleHost)probe, null, isHost: false);
                    probe.AttachNet(nbA530);
                    Ok(!probe.AiShouldDriveOpponent,
                       "（A530·档②）前提：联机层**挂上了**（`_net != null`）—— 与档③ 的分界就在这一格");
                    Exception err2;
                    var log2 = CaptureLogs(() => probe.NetReplay(startA530, new List<MsgAction>()), out err2);
                    NoteProbeThrow(err2, "档②");
                    Ok(HasLog(log2, "[Net] 重连重放："),
                       "（A530·档②）前提：走到的**确实是重连那条入口**（它自己那句「重连重放：重建这一局」在）");
                    Ok(HasLog(log2, "[Net] 联机重建（重连）："),
                       $"★（A530·档②）重连重建：日志自称「**联机重建（重连）**」（这一趟抓到 {log2.Count} 条日志）");
                    Ok(!HasLog(log2, "[Net] 联机开局：") && !HasLog(log2, "[Replay] 回放开局"),
                       "★（A530·档②）…而且**既没说成「联机开局」、也没说成「回放开局」** —— "
                     + "后者就是「只按 `attachNet` 二值分流」会掉进去的那一档（`NetReplay` 传的也是 `attachNet: false`）");
                }
                finally
                {
                    probe.AttachNet(null);                          // 摘掉联机层（免得 `OnDestroy` 走「断开」那条路）
                    UnityEngine.Object.DestroyImmediate(goA530);    // ⚠️ 批处理下 `Destroy` 不生效（本工程记过）
                }
            }

            // ---- 9) 🆕 2026-10-17（B13·A880 / A881）：**对面走了，玩家得看得见** ----
            //   原来 `NetSession.OnClosed` / `OnPeerLost` 在**生产侧零接线**（全仓只有定义 + 三处 `Invoke`）
            //   ⇒ 对面主动离开、或对面掉线，**本机玩家什么都不会发生**（静默 —— 红线）。
            //   判据（原版那一刻弹的是窗）→ `Net/NetBattle.cs` 里那一整节 + 两个 handler 的注释
            //   （`BattleManager.ConnectionStatus = WaitingForOtherPlayerToReconnect(2)` →
            //    `BattleErrorUIManager.ConnectionStatusChanged` → `WindowsManager.ShowPopUp`）。
            //   🔴 批处理没有帧循环 ⇒ 弹窗走 `NetRuntime` 的通知队（和真 Play 同一条：那边 `Update` 取出来弹），
            //      这里用 `DrainNoticesForTest()` 取出来验；提示行验 `BareHost.LastSay`。
            //   🧨 **改坏法（必须红）**：把 `NetBattle.WireSession()` 里那两句
            //      `s.OnPeerLost += …` / `s.OnClosed += …` 删掉 ⇒ 本节的 ★ 全红（那正是接线之前的实况）。
            {
                int savedPort9 = NetConfig.Current.port;
                int port9 = FreePort();
                NetConfig.Current.port = port9; NetConfig.Current.ip = "127.0.0.1";
                var hs9 = NetSession.NewTcp(); var cs9 = NetSession.NewTcp();
                Ok(hs9.StartHost(NetConfig.Current), "（对面走了·另开一局）主机起来监听");
                cs9.CheckConnection(NetConfig.Current);
                Ok(PumpUntil(hs9, cs9, () => hs9.State == NetState.Lobby && cs9.State == NetState.Lobby, 8000),
                   "（对面走了·另开一局）握手走完");

                var dk9a = Shuffle(DeckBuilder.StarterDeck(pool, "Ultramarines", DeckBuilder.ClassicDeckSize,
                                                           new System.Random(31), unitsOnly: false), 0x61);
                var dk9b = Shuffle(DeckBuilder.StarterDeck(pool, "Goff", DeckBuilder.ClassicDeckSize,
                                                           new System.Random(32), unitsOnly: false), 0x62);
                var ctx9h = RuleCore.NewBattle(dk9a, dk9b, 20261017, cardPool: pool, openMulligan: true,
                                               vars: GameplayVariables.Classic, firstSeat: 0);
                var ctx9c = RuleCore.NewBattle(dk9a, dk9b, 20261017, cardPool: pool, openMulligan: true,
                                               vars: GameplayVariables.Classic, firstSeat: 0);
                var hb9 = new BareHost { Ctx = ctx9h, MySeat = 0 };
                var cb9 = new BareHost { Ctx = ctx9c, MySeat = 1 };
                var hNB9 = NetBattle.Attach(hb9, hs9, isHost: true);
                var cNB9 = NetBattle.Attach(cb9, cs9, isHost: false);
                hs9.EnterBattle(); cs9.EnterBattle();                 // 掉线只有「对局中」才走等重连
                hNB9.RememberStart(new MsgStart
                {
                    seed = 20261017, mode = "Classic", arena = "Battle", hostFirst = 0,
                    hostFaction = "Ultramarines", clientFaction = "Goff",
                    hostDeckJson = "", clientDeckJson = "",
                });
                cb9.Rebuild = () => RuleCore.NewBattle(dk9a, dk9b, 20261017, cardPool: pool, openMulligan: true,
                                                       vars: GameplayVariables.Classic, firstSeat: 0);

                NetRuntime.DrainNoticesForTest();      // 先把队清掉（本段只认自己弹的那几条）
                hb9.LastSay = null;
                // ⚠️ **只推一台**会话用这个（本文件的 `PumpUntil` 第二个参数**没有空判** —— 给它 null 会 NPE）：
                //    两边一起推的话**客机自己也会报一次**，通知队里就有两条、数量断言说不清是谁弹的。
                Func<NetSession, Func<bool>, int, bool> pumpOne = (s, cond, ms) =>
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < ms) { s.Pump(); if (cond()) return true; Thread.Sleep(3); }
                    s.Pump(); return cond();
                };

                // ---- ① 对面**掉线**（心跳超时 / 连接断）⇒ 必须出声 ----
                cs9.Transport.ClosePeer();
                Ok(pumpOne(hs9, () => hs9.State == NetState.WaitingReconnect, 6000),
                   $"（对面掉线）主机进「等重连」（实际 {hs9.State}）");
                var n9a = NetRuntime.DrainNoticesForTest();
                Ok(n9a.Length == 1,
                   $"★（对面掉线）**弹了一条给人看的提示**（原来零接线 ⇒ 一条都没有；实得 {n9a.Length} 条）"
                 + " —— 🧨 改坏法：删掉 `WireSession` 里那句 `s.OnPeerLost += …` ⇒ 红");
                Ok(n9a.Length >= 1 && n9a[0].Contains("掉线"),
                   $"★（对面掉线）那条话点明是**对面掉线**（实得「{(n9a.Length > 0 ? n9a[0] : "")}」）");
                Ok(hb9.LastSay != null && hb9.LastSay.Contains("掉线"),
                   $"★（对面掉线）**提示行**也说了（实得「{hb9.LastSay}」）");

                // ---- ② 对面**回来了** ⇒ 提示行说一句（原版那一刻是 `CloseAllWindows`）----
                // 🔴 **2026-10-10（F5）：这一格的等待条件原来只查【主机】一台** —— 而 `PumpUntil2`
                //   的循环体是 `hs.Pump(); cs.Pump(); cond()`（本文件 `:1423`），主机那台在**同一次
                //   `Pump` 里**就把 `Resume` 发出去并自己转 `InBattle`（`NetSession.cs:487-494`）⇒
                //   **客机那一侧还没读到 `Resume` 帧，`cond()` 就已经为真**，循环当轮就退出。
                //   ⇒ 之后第 ③ 步是在「客机仍 `WaitingReconnect`」这个**没验证过的状态**下关会话。
                //   实证（5 跑 5 对，`资料/普查产出_第十一会话/F5_NetBattle夹具.md` §一）：
                //     · 2026-10-10 全套跑（`netbattle.log:5303`）本行印「主机 InBattle / 客机 WaitingReconnect」
                //       ⇒ 第 ③ 步三条 ★ 全红；
                //     · 同日/前日 4 次净跑（`netbattle_rerun1/2` · `netbattle2_1020` · `netbattle2`）本行印
                //       **「两边 InBattle」** ⇒ 第 ③ 步三条 ★ **全绿**。
                //   ⇒ 这一格的**文案本来就写着「两边回到对局」**，判据必须两个都查。
                //   ⛔ **别改回「只查主机」**：客机没追上时第 ③ 步那三条红是**假的**（`D1` 判的 (δ)）。
                bool bothBack9 = PumpUntil2(hs9, cs9, hNB9, cNB9,
                    () => hs9.State == NetState.InBattle && cs9.State == NetState.InBattle, 25000);
                if (!bothBack9)
                    Debug.LogWarning($"{P} ⚠️（对面回来）**等两边都回到对局·超时了**（主机 {hs9.State} / "
                                   + $"客机 {cs9.State}）—— 客机那一侧的 `Resume` 还没被它自己的 `Pump()` 消化。"
                                   + "⛔ **必须出声**：下面第 ③ 步会在**这个状态下**关会话，而它那三条 ★ 要的是"
                                   + "「对面的 `bye` 到了主机」—— 客机没追上时它们**会假红**（2026-10-10 实测："
                                   + "本行印「客机 WaitingReconnect」⇒ 第 ③ 步三条全红）。"
                                   + "⇒ 先看这一条，再看第 ③ 步的红，别去修传输层。");
                Ok(bothBack9,
                   $"（对面回来）客机自动重连、**两边**回到对局（主机 {hs9.State} / 客机 {cs9.State}）"
                 + " —— 🧨 改坏法：把条件退回「只查主机」⇒ 客机没追上时这一格照样绿，而第 ③ 步会假红"
                 + "（`NetSession.cs:487-494`：主机转 `InBattle` 与发 `Resume` 是同一次 `Pump`）");
                Ok(hb9.LastSay != null && hb9.LastSay.Contains("回来"),
                   $"★（对面回来）提示行改成「回来了」（实得「{hb9.LastSay}」）—— 与上面那条**不同源**："
                 + "上面验的是那一下说不说，这条验的是**恢复之后会不会改口**（不说的话玩家一直以为对面还没回来）");

                // ---- ③ 对面**主动离开这一局**（`bye`）⇒ 也必须出声 ----
                // 🔴 **2026-10-10（F5）核过：这里的 `pumpOne`（只推主机）是对的，⛔ 别改成推两台。**
                //   ① **不需要**：上面的 ② 现在保证**客机也 `InBattle`**（= 它的 `Resume` 已被自己
                //      `Pump()` 消化、收发两条都不再积压）⇒ 这一刻 `cs9.Close(true, …)` 把 `bye`
                //      写出去之后，**客机这一侧没有任何事要做**（它在 `Close` 里已经 `Off`）。
                //   ② **有反作用**：客机此刻 `State == Off`，而 `NetSession.Pump()` 里「掉线检测」
                //      那道闸（`NetSession.cs:205`）**只挡 `Closed` / `WaitingReconnect`、不挡 `Off`**
                //      ⇒ 它这一侧只要 `_t.PeerLost` 为真就会**再报一次** `OnPeerLost`
                //      ⇒ 下面 `n9c.Length == 1` 那条就不是 1 了（那是**假的**红，跟本段要验的东西无关）。
                //   ③ 主判据只跟**主机**有关：`bye` 到没到主机、主机弹了几条、主机提示行说了什么。
                NetRuntime.DrainNoticesForTest();
                hb9.LastSay = null;
                cs9.Close(true, "对面离开了这一局");     // = 客机那一侧的 `BattleDriver.LeaveNetRoom()`
                Ok(pumpOne(hs9, () => hs9.State == NetState.Closed, 4000),
                   $"（对面离开）主机收到 `bye` ⇒ 会话关上（实际 {hs9.State}）");
                var n9c = NetRuntime.DrainNoticesForTest();
                Ok(n9c.Length == 1,
                   $"★（对面离开）**弹了一条**（原来零接线 ⇒ 一条都没有；实得 {n9c.Length} 条）"
                 + " —— 🧨 改坏法：删掉 `WireSession` 里那句 `s.OnClosed += …` ⇒ 红");
                Ok(n9c.Length >= 1 && n9c[0].Contains("离开"),
                   $"★（对面离开）那条话里**带着对面报的理由**（实得「{(n9c.Length > 0 ? n9c[0] : "")}」）"
                 + " —— 这条与「掉线」那一条是**两个不同的回调**（`OnClosed` vs `OnPeerLost`），别混成一个");
                Ok(hb9.LastSay != null && hb9.LastSay.Contains("结束"),
                   $"★（对面离开）提示行也说了（实得「{hb9.LastSay}」）");

                hs9.Close(false); cs9.Close(false);
                NetConfig.Current.port = savedPort9;

                // ---- ④ 🆕 A881①：`OnDestroy` 那条路**也要捎一句 `bye`** ----
                //   原来不捎 ⇒ 对面只能等 **10 秒心跳超时**（`NetSession.SilentTimeoutMs`）才发现我们没了。
                //   判据：原版在「销毁 / 退出应用」这条路上也是**主动退房间**（`NetworkCustomManager__OnDestroy.c:13-19`
                //   的 `PhotonNetwork.Disconnect()`；`PlayerDataManager__QuitApplication.c:23-35` 的 `LeaveRoom()`）。
                {
                    int port9d = FreePort();
                    NetConfig.Current.port = port9d; NetConfig.Current.ip = "127.0.0.1";
                    var hs9d = NetSession.NewTcp(); var cs9d = NetSession.NewTcp();
                    Ok(hs9d.StartHost(NetConfig.Current), "（OnDestroy·另开一局）主机起来监听");
                    cs9d.CheckConnection(NetConfig.Current);
                    Ok(PumpUntil(hs9d, cs9d, () => hs9d.State == NetState.Lobby && cs9d.State == NetState.Lobby, 8000),
                       "（OnDestroy·另开一局）握手走完");
                    hs9d.EnterBattle(); cs9d.EnterBattle();

                    var go9d = new GameObject("B13_LeaveOnDestroy");
                    var probe9d = go9d.AddComponent<BattleDriver>();
                    NetBattle.Attach(probe9d, hs9d, NetPendingBattle.FromStart(new MsgStart(), true));
                    Ok(probe9d.Net != null && ReferenceEquals(probe9d.Net.Session, hs9d),
                       "（OnDestroy）夹具：这台驱动**挂着联机层**（`_net != null` 就是「联机局」那个判据）");
                    // 🔴 **2026-10-17（F4·RC2）：这一格按 `Application.isPlaying` 分两档。**
                    //   判据链（→ `资料/普查产出_1017/D4_联机诊断.md` §③）：`BattleDriver` **不带
                    //   `[ExecuteAlways]`**（`Battle/BattleDriver.cs:22`），而本自检跑在**编辑模式** ⇒
                    //   **编辑器不派生命周期消息** ⇒ `DestroyImmediate(组件)` **不会**调 `OnDestroy`
                    //   ⇒ `LeaveNetRoom()` 压根没执行（本轮日志实证：全篇 `离开房间` = 0、`离开战场` = 0，
                    //   本机会话停在 `InBattle`）。项目已实测记过两次：`Editor/BattleScene.cs:12976-12986`
                    //   （A388(b)）+ `Shell/WindowsManager.cs:65-70`。
                    //   ⛔ **不写成绿、也不许删**（铁律 11 + 「弱断言分不出两种状态 = 没断」）：
                    //      编辑模式这半分**改断「环境事实」**（组件真没了 ∧ 两台会话状态**原封不动**，
                    //      两件都实读）+ `_warn++` 出声。
                    //   🔴 **真路径（真卸载 ⇒ 对面收到 `bye`）批处理里验不了** ⇒ 归
                    //      `资料/真Play待验清单.md` **D39**（离开战场场景 → 再进一次）。
                    var stBefore9d = hs9d.State;                    // 卸之前那一刻的会话状态（实读）
                    UnityEngine.Object.DestroyImmediate(go9d);      // ⚠️ 批处理下 `Destroy` 不生效（本工程记过）
                    if (Application.isPlaying)
                    {
                        Ok(pumpOne(cs9d, () => cs9d.State == NetState.Closed, 4000),
                           "★（OnDestroy）拆掉驱动时**对面收到了 `bye`**（不用干等 10 秒心跳超时）"
                         + " —— 🧨 改坏法：删掉 `BattleDriver.OnDestroy` 里那句 `LeaveNetRoom();` ⇒ 红");
                        Ok(hs9d.State == NetState.Off, $"（OnDestroy）本机那一侧也关上了（实际 {hs9d.State}）");
                    }
                    else
                    {
                        // 给对面一点时间 —— 它本来就**不该**收到任何东西（断言要看得出「真没收到」）
                        pumpOne(cs9d, () => cs9d.State == NetState.Closed, 300);
                        _warn++;
                        Debug.LogWarning($"{P} ⚠️（OnDestroy）**本宿主里这一跳验不了**：编辑模式"
                                       + "（`Application.isPlaying == false`）不派生命周期消息 ⇒ "
                                       + "`DestroyImmediate(组件)` 不调 `OnDestroy` ⇒ `LeaveNetRoom()` 没跑过。"
                                       + $"⇒ 这一档改断**环境事实**：对面仍然 **{cs9d.State}**（没收到 `bye`）、"
                                       + $"本机会话仍然是 **{hs9d.State}**（== 卸之前那一刻的 {stBefore9d}）。"
                                       + "真路径 = **离开战场场景 → 再进一次**（联机局那一刻对面该收到 `bye`），"
                                       + "只有真 Play 跑得到 ⇒ `资料/真Play待验清单.md` **D39**。");
                        Ok(probe9d == null && cs9d.State != NetState.Closed && hs9d.State == stBefore9d,
                           $"★（OnDestroy·编辑模式档）拆掉驱动之后：**组件真没了**（`== null`）而"
                         + $"**两台会话的状态原封不动**（本机 {hs9d.State} / 对面 {cs9d.State}）"
                         + " —— 这是「本宿主不派 `OnDestroy`」那条**环境事实**，**不是**「实现了捎 `bye`」"
                         + "（那一跳只有真 Play 能验，D39）"
                         + "；🧨 改坏法：给 `BattleDriver` 加 `[ExecuteAlways]`（或让这一路真的派上 `OnDestroy`）"
                         + " ⇒ 会话当场被关上 ⇒ 本条红 —— 那是「环境变了、好消息」，"
                         + "把上面 Play 那一档提成无条件即可");
                    }
                    hs9d.Close(false); cs9d.Close(false);
                    NetConfig.Current.port = savedPort9;
                }
            }

            // ---- 10) 🆕 2026-10-17（B17·A900 / A901）：**掉线之后那两半** ----
            //   A901 = 对手掉线 ⇒ **对局时钟停走**（原版 `BattleManager__ShowDisconnectionPopup.c:27` 的
            //          `ClockManager.PauseClock()`；回来那一刻 `SuccessfulReconnection.c:51` 的 `UnpauseClock()`）。
            //   A900 = 对手掉线 ⇒ 起 `max(15, maxSecToReconnect 30 − 3 × enemyDisconnects)` 秒的倒计时
            //          （每秒报一拍 `UpdateReconnectStatus(secLeft)`），**到点判对面弃权**。
            //   判据全文（含那两个字段是怎么从 DLL / 资产里读出来的）→ `Net/NetBattle.cs` 的 A900/A901 那一节。
            //
            //   ⚠️ **两半分开验，因为宿主不同**：
            //     ① **时钟闸只长在真 `BattleDriver` 上**（`NetClockPaused`）⇒ 那两条用真驱动；
            //     ② **倒计时 / 判负走的是 `INetBattleHost` 那一层** ⇒ 用裸 host（与 §6/§9 **同一份契约**）。
            //   ⛔ **「用真驱动跑一遍到点判负」这条路故意不走**：`BattleDriver.NetRemoteResign` 末尾的
            //      `RefreshAll()` 要手牌 / 棋盘 / 相机（本自检没有 ⇒ `Begin` 都跑不完，先例见 §A530）——
            //      那条路归 `Editor/BattleScene.cs`（有整套场景那一条）。
            //   🧨 **判别式**（本节的 ★ 里挑一条，删掉对应实现就必红）：
            //      ① 删掉 `BattleDriver.TickClock` 里那句 `if (NetClockPaused) return;` ⇒ 10②「表停走」红；
            //      ② 删掉 `NetBattle.HandlePeerLost` 里那句 `ClockPaused = true;` ⇒ 10②「闸按下了」红；
            //      ③ 删掉 `NetBattle.Tick()` 里那句 `TickReconnectCountdown();` ⇒ 10③ 全红（数字一动不动）；
            //      ④ 删掉 `ReconnectSecondsFor` 里的 `- ReconnectSecondsStep * n` ⇒ 10③「第二次 = 27」红。
            {
                float clk = 0f;
                NetBattle.SetClockForTest(() => clk);      // 本段自己那个假钟（`_clk` 留给前面那几段）
                try
                {
                    var dkB1 = Shuffle(DeckBuilder.StarterDeck(pool, "Ultramarines", DeckBuilder.ClassicDeckSize,
                                                               new System.Random(61), unitsOnly: false), 0x81);
                    var dkB2 = Shuffle(DeckBuilder.StarterDeck(pool, "Goff", DeckBuilder.ClassicDeckSize,
                                                               new System.Random(62), unitsOnly: false), 0x82);

                    // ============ 10① **单机：没有联机会话时时钟照常走**（任务书 ④ 的那条红线）============
                    {
                        var goA = new GameObject("B17_SinglePlayerClock");
                        var drvA = goA.AddComponent<BattleDriver>();
                        drvA.SetCtxForTest(RuleCore.NewBattle(Shuffle(dkB1, 0x91), Shuffle(dkB2, 0x92), 20261020,
                                                              cardPool: pool, openMulligan: false,
                                                              vars: GameplayVariables.Classic, firstSeat: 0));
                        Ok(!drvA.NetClockPaused, "（时钟闸·单机）夹具：这台驱动**没有联机层**（`_net == null`）");
                        Ok(drvA.Ctx.Active == drvA.MyIndex, "（时钟闸·单机）夹具：轮到本机（`TickClock` 头一道闸）");
                        float sp0 = drvA.ClockLeft;
                        Ok(sp0 > 40f, $"（时钟闸·单机）夹具：表是满的（{sp0:F0} s，> 催命阈值 35 ⇒ 这一趟碰不到语音那条路）");
                        drvA.TickClockForTest(1f);
                        Ok(Mathf.Abs(drvA.ClockLeft - (sp0 - 1f)) < 0.01f,
                           $"★（时钟闸·单机）**没有联机会话时时钟照常走**：推 1 s ⇒ {sp0:F0} → {drvA.ClockLeft:F1}"
                         + " —— 🧨 改坏法：把 `NetClockPaused` 写成「没联机也算暂停」"
                         + "（例如 `_net == null || _net.ClockPaused`），或让 `TickClock` 变成无条件 `return` ⇒ 红");
                        UnityEngine.Object.DestroyImmediate(goA);   // ⚠️ 批处理下 `Destroy` 不生效（本工程记过）
                    }

                    // ============ 10② 真驱动：对手掉线 ⇒ **表停**；对手回来 ⇒ **表接着走** ============
                    {
                        int savedPortB = NetConfig.Current.port;
                        int portB = FreePort();
                        NetConfig.Current.port = portB; NetConfig.Current.ip = "127.0.0.1";
                        var hsB = NetSession.NewTcp(); var csB = NetSession.NewTcp();
                        Ok(hsB.StartHost(NetConfig.Current), "（时钟闸）主机起来监听");
                        csB.CheckConnection(NetConfig.Current);
                        Ok(PumpUntil(hsB, csB, () => hsB.State == NetState.Lobby && csB.State == NetState.Lobby, 8000),
                           "（时钟闸）握手走完");

                        var goB = new GameObject("B17_NetClockGate");
                        var drvB = goB.AddComponent<BattleDriver>();
                        drvB.SetCtxForTest(RuleCore.NewBattle(Shuffle(dkB1, 0xA1), Shuffle(dkB2, 0xA2), 20261021,
                                                              cardPool: pool, openMulligan: false,
                                                              vars: GameplayVariables.Classic, firstSeat: 0));
                        var nbB = NetBattle.Attach(drvB, hsB, NetPendingBattle.FromStart(new MsgStart(), isHost: true));
                        nbB.RememberStart(new MsgStart { seed = 20261021, mode = "Classic", arena = "Battle", hostFirst = 0 });
                        hsB.EnterBattle(); csB.EnterBattle();
                        Ok(!drvB.NetClockPaused, "（时钟闸）夹具：刚开局**闸没按下**");

                        csB.Transport.ClosePeer();                 // 🔴 模拟对面掉线（§6/§9 同一个手法）
                        Ok(PumpUntil(hsB, csB, () => hsB.State == NetState.WaitingReconnect, 8000),
                           $"（时钟闸）主机发现对面掉线、进等待重连（实际 {hsB.State}）");
                        Ok(drvB.NetClockPaused,
                           "★ **对手掉线 ⇒ 对局时钟的闸按下了**"
                         + "（原版 `ShowDisconnectionPopup.c:27` = `ClockManager.PauseClock()`）"
                         + " —— 🧨 改坏法：删掉 `HandlePeerLost` 里那句 `ClockPaused = true;` ⇒ 红");
                        Ok(nbB.ClockPaused && nbB.ReconnectCountdownRunning,
                           $"★ 同一刻**倒计时也起了**（{nbB.ReconnectCountdownLeft} 秒 —— 原版另起一条协程，不是同一件事）");
                        float cB = drvB.ClockLeft;
                        drvB.TickClockForTest(2f);
                        Ok(Mathf.Abs(drvB.ClockLeft - cB) < 0.001f,
                           $"★ **表停走**：推 2 s 一点没动（还是 {cB:F1} s）"
                         + " —— 🧨 改坏法：删掉 `BattleDriver.TickClock` 里那句 `if (NetClockPaused) return;` ⇒ 红");

                        // 对手回来（客机自动重连 ⇒ 主机发 `resume` ⇒ 两边回 `InBattle`）
                        Ok(PumpUntil(hsB, csB, () => hsB.State == NetState.InBattle, 25000),
                           $"（时钟闸）对手回来、两边回到对局（主机 {hsB.State} / 客机 {csB.State}）");
                        int hideBefore = NetRuntime.HidePopupCallsForTest;
                        nbB.Tick();                                // 认「回来了」那一条边沿
                        Ok(!drvB.NetClockPaused,
                           "★ **对手回来 ⇒ 闸放开了**（原版 `SuccessfulReconnection.c:51` 的 `ClockManager.UnpauseClock()`）");
                        Ok(!nbB.ReconnectCountdownRunning && !nbB.ClockPaused,
                           "★ …而且**倒计时也取消了**（原版同一句里的 `StopCoroutine(ForfeitDisconnectedEnemy)`）");
                        Ok(NetRuntime.HidePopupCallsForTest > hideBefore,
                           "★ …并且**去撤那扇窗了**（原版那一刻是 `CloseAllWindows()`；我们只关**弹窗**那一颗 —— "
                         + "`NetRuntime.HideNoticePopup` → `WindowsManager.HidePopUp(false)`）"
                         + " ⚠️ 批处理里没有 `WindowsManager`，这一格记的是「**真的去撤了**」，"
                         + "「窗真关掉了」只有真 Play 验得到");
                        float cB2 = drvB.ClockLeft;
                        drvB.TickClockForTest(2f);
                        Ok(Mathf.Abs(drvB.ClockLeft - (cB2 - 2f)) < 0.01f,
                           $"★ **表接着走**：推 2 s ⇒ {cB2:F1} → {drvB.ClockLeft:F1}"
                         + "（这一条与上面「表停走」那条**互斥**，不是同义反复：一条验「停得住」、一条验「停完还能开」）");

                        drvB.AttachNet(null);                      // 摘联机层（免得 `OnDestroy` 走「断开」那条路）
                        hsB.Close(false); csB.Close(false);
                        NetConfig.Current.port = savedPortB;
                        UnityEngine.Object.DestroyImmediate(goB);
                    }

                    // ============ 10③ 倒计时判弃权（裸 host 那一层 —— 与 §6/§9 同一份 `INetBattleHost` 契约）============
                    {
                        int savedPortC = NetConfig.Current.port;
                        int portC = FreePort();
                        NetConfig.Current.port = portC; NetConfig.Current.ip = "127.0.0.1";
                        var hsC = NetSession.NewTcp(); var csC = NetSession.NewTcp();
                        Ok(hsC.StartHost(NetConfig.Current), "（判弃权）主机起来监听");
                        csC.CheckConnection(NetConfig.Current);
                        Ok(PumpUntil(hsC, csC, () => hsC.State == NetState.Lobby && csC.State == NetState.Lobby, 8000),
                           "（判弃权）握手走完");

                        var hbC = new BareHost
                        {
                            MySeat = 0,
                            Ctx = RuleCore.NewBattle(Shuffle(dkB1, 0xB1), Shuffle(dkB2, 0xB2), 20261022,
                                                     cardPool: pool, openMulligan: false,
                                                     vars: GameplayVariables.Classic, firstSeat: 0),
                        };
                        var cbC = new BareHost
                        {
                            MySeat = 1,
                            Ctx = RuleCore.NewBattle(Shuffle(dkB1, 0xC1), Shuffle(dkB2, 0xC2), 20261022,
                                                     cardPool: pool, openMulligan: false,
                                                     vars: GameplayVariables.Classic, firstSeat: 0),
                        };
                        var hNBC = NetBattle.Attach(hbC, hsC, isHost: true);
                        var cNBC = NetBattle.Attach(cbC, csC, isHost: false);
                        hsC.EnterBattle(); csC.EnterBattle();
                        hNBC.RememberStart(new MsgStart
                        {
                            seed = 20261022, mode = "Classic", arena = "Battle", hostFirst = 0,
                            hostFaction = "Ultramarines", clientFaction = "Goff",
                            hostDeckJson = "", clientDeckJson = "",
                        });
                        cbC.Rebuild = () => RuleCore.NewBattle(Shuffle(dkB1, 0xB1), Shuffle(dkB2, 0xB2), 20261022,
                                                               cardPool: pool, openMulligan: false,
                                                               vars: GameplayVariables.Classic, firstSeat: 0);

                        NetRuntime.DrainNoticesForTest();          // 先把队清掉（本段只认自己弹的那几条）
                        hbC.LastSay = null;

                        // ---- ③-1 第一次掉线：**30 秒**（原版 `max(15, 30 − 3×0)`）----
                        csC.Transport.ClosePeer();
                        Ok(PumpUntil(hsC, csC, () => hsC.State == NetState.WaitingReconnect, 8000),
                           $"（判弃权）主机发现对面掉线（实际 {hsC.State}）");
                        Eq(hNBC.ReconnectCountdownLeft, 30,
                           "★ 倒计时 = **30 秒**（原版 `max(15, VarsGlobal.maxSecToReconnect 30 − 3 × enemyDisconnects 0)`）"
                         + " —— 🧨 改坏法：删掉 `Tick()` 里那句 `TickReconnectCountdown();` ⇒ 本节全红");
                        Ok(hbC.LastSay != null && hbC.LastSay.Contains("30"),
                           $"★ 起的那一刻就把秒数告诉了玩家（实得「{hbC.LastSay}」）");

                        // ---- ③-2 每秒报一拍（原版 `UpdateReconnectStatus(secLeft)`）----
                        clk += 1f;
                        hNBC.Tick();
                        Eq(hNBC.ReconnectCountdownLeft, 29,
                           "★ **每秒报一拍**：30 → 29（原版那一拍接的是 `BattleErrorUIManager.UpdateReconnectWindow`，"
                         + "它把 `TimeSpan.FromSeconds(sec)` 拼进弹窗正文）");
                        Ok(hbC.LastSay != null && hbC.LastSay.Contains("29"),
                           $"★ 提示行跟着改口（实得「{hbC.LastSay}」）");

                        // ---- ③-3 对手在倒计时内回来 ⇒ **取消倒计时、不判负** ----
                        Ok(PumpUntil2(hsC, csC, hNBC, cNBC, () => hsC.State == NetState.InBattle, 25000),
                           $"（判弃权）对手回来（主机 {hsC.State} / 客机 {csC.State}）");
                        Ok(!hNBC.ReconnectCountdownRunning,
                           "★ **对手在倒计时内回来 ⇒ 倒计时取消**（原版 `SuccessfulReconnection.c:27` 的 `StopCoroutine`）");
                        Ok(hbC.LastSay != null && hbC.LastSay.Contains("回来"),
                           $"★ 提示行改口说「回来了」（实得「{hbC.LastSay}」）");
                        Eq(hbC.Ctx.Winner, 0,
                           "★ …而且**没有判负**（`Winner` 还是 0 —— 用户 2026-09-26 要的「掉线能接着打」）");

                        // ---- ③-4 第二次掉线：**27 秒** = `max(15, 30 − 3×1)` ★ 判别式 ----
                        //   🔴 这一条盯的是公式里那个 **`− 3 × enemyDisconnects`** 项 ——
                        //      把秒数写死成 30 的实现会在这里红。
                        csC.Transport.ClosePeer();
                        Ok(PumpUntil(hsC, csC, () => hsC.State == NetState.WaitingReconnect, 8000),
                           $"（判弃权）第二次掉线（实际 {hsC.State}）");
                        Eq(hNBC.EnemyDisconnects, 2,
                           "★ 本局对手掉线次数记到 2（原版 `BattleManager.enemyDisconnects`，`+0x45C`）");
                        Eq(hNBC.ReconnectCountdownLeft, 27,
                           "★ **第二次 = 27 秒** = `max(15, 30 − 3×1)`"
                         + " —— 🧨 改坏法：删掉 `ReconnectSecondsFor` 里的 `- ReconnectSecondsStep * n` ⇒ 红");

                        // ---- ③-5 到点 ⇒ **判对面弃权**（原版 `DeadHero(对面, 3)`）----
                        //   ⚠️ 先把「还没有被接回来」摆明（客机那边**可能正在自动重连**，
                        //      那会让 `Tick` 走「回来了」那一支而不是「到点」那一支 ⇒ 先钉住前提）。
                        Ok(hsC.State == NetState.WaitingReconnect,
                           "（判弃权）夹具：到点这一刻**对面还没接回来**（不然下面验的就不是「到点」那一条）");
                        // 🆕 **正例（独立审查 R1 要的那一半）**：到点**之前**，客机自己那一端
                        //   `_wasInBattle` **必须还是真** —— 没有这一条，下面 ③-6 的「变假了」就分不出
                        //   「清掉了」和「本来就是假」（弱断言 / 假绿）。它是那一对的**前半**。
                        Ok(csC.WasInBattleForTest,
                           "★（A914 正例）到点之前：**客机自己那一端**的「掉线前在对局中」标记仍是**真**"
                         + "（`NetSession.WasInBattleForTest`）—— 这一条是下面「到点之后变假」那一条的**对照**，"
                         + "没它那一条就是弱断言");
                        int hideBefore2 = NetRuntime.HidePopupCallsForTest;
                        clk += 27f;
                        hNBC.Tick();
                        Eq(hbC.Ctx.Winner, 1, "★ **到点判对面弃权**（本机是座位 0 ⇒ 判座位 1 负、本机胜）");
                        Eq(hbC.Ctx.ForfeitedBy, 1,
                           "★ …「谁弃的权」记的是**对面**（走的是现成入口 `INetBattleHost.NetRemoteResign` ="
                         + " `RuleCore.Forfeit(Ctx, 1−_me)`，原版那一跳是 `DeadHero(对面, 3)`）");
                        // 🆕 **2026-10-18（A914 第四续）两端判别式 · 主机那一半**
                        //   主机 = 【对面掉线那一端】⇒ 该调 `NetRemoteResign`（判对面）、**不该**调 `NetSelfResign`。
                        //   🧨 把主机那一支改成 `NetSelfResign()` ⇒ 下面第二条红（`Ctx` 也会跟着变 ⇒ 上面那条也红）。
                        Eq(hbC.ResignCalls, 1,
                           "★（A914·两端判别式）**主机这一端**走的是 `NetRemoteResign`（判**对面**）—— 恰一次");
                        Eq(hbC.SelfResignCalls, 0,
                           "★（A914·两端判别式）…而且**一次都没判自己**（主机不是断线那一端）"
                         + " —— 🧨 改坏法：把主机那一支改成调 `NetSelfResign()` ⇒ 红");
                        // ---- ③-5' 🆕 2026-10-18（**A913**）：**理由码**（原版 `DeadHero(_, _, BattleResult)`）----
                        //   这一档「掉线到点」= 原版 `…_d__322__MoveNext.c:144` 的 `DeadHero(对面, 3)` ⇒ **码 3**。
                        //   🔴 两个**不同源**的观测口都断（缺一个，就有一半改坏法测不出来）：
                        //     ① 桩记下的「**调用方传的码**」（`BareHost.LastResignReason`）；
                        //     ② 引擎 `ctx.Events` 上那个 `[BattleResult] …` **token**（码有没有「照着落地」）。
                        //   🧨 判别式：调用点把码传成 2 ⇒ 只红 ①②里的正例；`RuleCore.Forfeit` 里那句
                        //      写死成 `Forfeit` ⇒ 只红 ②（①照样绿）—— 这正是要把两个口分开的原因。
                        Ok(hbC.HasRemoteReason,
                           "（A913）夹具：这一档**真的从判负入口走过**（不然下面读到的 `Disconnect` 可能只是默认值 0/2）");
                        Eq((int)hbC.LastResignReason, (int)BattleResult.Disconnect,
                           "★★（A913）**掉线倒计时到点 ⇒ 理由码 `Disconnect`(3)**"
                         + "（原版 `…_d__322__MoveNext.c:144` `DeadHero(对面, 3)`）"
                         + " —— 🧨 改坏法：把 `ReconnectCountdownExpired` 那一句改成传 `BattleResult.Forfeit` ⇒ 红");
                        Ok(hbC.LastResignReason != BattleResult.Forfeit,
                           "★★（A913）…而且**不是**投降那一档（2）—— 两码在这一格里**互斥**"
                         + "（只断一条的话，「两档都传 3」也照样绿 = 弱断言分不出两态）");
                        Ok(hbC.Ctx.Events.Exists(e => e.Contains("[BattleResult] Disconnect(3) seat=1")),
                           "★★（A913）…而且**引擎照着这个码落了地**（`ctx.Events` 那个 token = 第二个观测口，"
                         + "与上面那个桩**不同源**）—— 🧨 把 `RuleCore.Forfeit` 里那句 token 写死成 `Forfeit` ⇒ 只红这一条");
                        Ok(!hbC.Ctx.Events.Exists(e => e.Contains("[BattleResult] Forfeit(2)")),
                           "★★（A913）…反向：这一局**没有**出现投降那一档的 token");
                        Ok(!hNBC.ReconnectCountdownRunning && !hNBC.ClockPaused,
                           "★ 判完**倒计时收掉、闸也放开**（不留一个还在跑的状态）");
                        Ok(NetRuntime.HidePopupCallsForTest > hideBefore2,
                           "★ 判负那一刻**先把那扇提示窗撤掉**"
                         + "（原版那一刻状态置 3 = `Disconnected` ⇒ `…ConnectionStatusChanged.c:41-43` 的 `CloseAllWindows()`"
                         + " —— **是关窗不是开窗**）");

                        // ---- ③-6 🆕 2026-10-18（`A914`，**按独立审查 R1/R3 重写**）：判负之后**分端**收尾 ----
                        //   🔴 **原版两端行为【不同】**（第一版把两端当一回事 = **真偏离**，`REV_W3_联机.md` R3）：
                        //     · **【对面掉线那一端】**（判**对面**弃权）⇒ `…_d__322__MoveNext.c:142-144`
                        //       的 `LeaveBattleRoom(false)` + `AddResignAction` + `DeadHero(对面,3)`
                        //       ⇒ **离开房间**（`LeaveBattleRoom.c:41-44` 的 `RemoveRoomAfterLeaving()`
                        //       = `Room.EmptyRoomTtl = 0`，`:46-49` `LeaveRoom()`）。
                        //     · **【本机是断线那一端】**（自己掉了、重连也失败）⇒
                        //       `BattleManager__FailedToReconnectAfterDisconnect.c:14` 的
                        //       `StopCoroutine(AttemptReconnect)` + `:26-27 AddResignAction(1)+DeadHero(我,3)`
                        //       —— **全函数里没有 `LeaveBattleRoom`** ⇒ **不离开房间**，只**停重连**。
                        //   ⚠️ 「本机是哪一端」的判据 = `_s.Role == NetRole.Client`（**这是我们自拟的**：
                        //      我们这套里只有客机有主动重连那条路；原版两端各有 `AttemptReconnect`）。
                        //   🧨 **判别式（两条不同源，删任一端的分支必红对的那条）**：
                        //     · 删掉主机那一支的 `Close` ⇒ **`hsC.State == Off` 那条红**；
                        //     · 删掉客机那一支的 `StopReconnecting()` ⇒ **`!csC.WasInBattleForTest` 那条红**。
                        //   ⚠️ **`:886` 上面那条 `WaitingReconnect` 断言【不红】**（求值在 `Tick()` **之前**）。
                        Ok(hsC.State == NetState.Off,
                           "★★（A914·**对面掉线那一端** = 本机是主机）会话**真的离开了房间**"
                         + $"（`Close` ⇒ `SetState(Off)`；实际 {hsC.State}）"
                         + " —— 判据 = 原版 `…_d__322:142 LeaveBattleRoom(false)` 里的 `LeaveRoom()`；"
                         + "🧨 删掉 `ReconnectCountdownExpired` 末尾主机那一支的 `Close(...)` ⇒ 只红这一条");
                        // 🔴 **2026-10-19（P6d）**：这一条原来断的是**写死的中文子串**「这一局的联机房间已经散了」
                        //   —— 那是 `ReconnectCountdownExpired` 传给 `Close` 的**那串字面量**。P6d 把
                        //   `Close` 的 `reason` 改成**词条键**（线上发键、收侧取词），本机状态字 = `Loc.T(键)`
                        //   ⇒ 断死中文在**英文档必红** ⇒ 改成断 `Loc.T(键)`（表跟着语档走）。
                        string wantRoomGone = Loc.T("Settings/Online/Wire/RoomGone");
                        Ok(hsC.StatusText != null && hsC.StatusText.Contains(wantRoomGone),
                           $"★（A914）…而且是**这一跳**把它请出去的（状态字里带着 `Settings/Online/Wire/RoomGone` "
                         + $"那条词条；实得「{hsC.StatusText}」）"
                         + " —— 与上一条**不同源**：上一条只看「走没走」，这一条看「走的是不是这条路」"
                         + "；⚠️ 那句措辞是**中性**的（它会经 `MsgBye.reason` 显示在**对面**界面上 —— 见 R7）"
                         + "；🧨 改坏法：把 `ReconnectCountdownExpired` 那一支的 `Close(true, 键)` 换回别的键 ⇒ 红");

                        // ---- ③-7 🆕 **本机是断线那一端**（客机自己的倒计时到点）⇒ 只停重连、**不离开房间** ----
                        //   `clk` 一次推到 60（**不依赖客机那一端 L0 究竟是 27 还是 30** —— 它取决于
                        //   客机的 `enemyDisconnects` 走到几，那是它自己那条 `OnPeerLost` 的时序，
                        //   本自检不去赌它）。到点那一跳在 `ReconnectCountdownExpired` 里，与主机的同源。
                        clk += 60f;
                        cNBC.Tick();
                        Ok(csC.State == NetState.WaitingReconnect,
                           "★★（A914·**本机是断线那一端** = 本机是客机）会话**【没有】离开房间**"
                         + $"（仍是 `WaitingReconnect`；实际 {csC.State}）"
                         + " —— 判据 = 原版 `FailedToReconnectAfterDisconnect.c` **全函数里没有 `LeaveBattleRoom`**"
                         + " ⇒ 我们**不做那一跳**（多做就是偏离）；🧨 把那一支也改成 `Close(...)` ⇒ 只红这一条"
                         + "（它与上面主机那条**互为反例**：同一方法、两端不同行为）");
                        Ok(!csC.WasInBattleForTest,
                           "★（A914）…但客机那条**每 2 秒重连**的退避**停了**（`StopReconnecting()`）"
                         + " —— 原版那一刻是 `StopCoroutine(AttemptReconnect)`"
                         + "（`FailedToReconnectAfterDisconnect.c:14`）"
                         + "；🧨 删掉客机那一支的 `_s.StopReconnecting()` ⇒ 只红这一条"
                         + "（⚠️ 上面两条钉不到它 —— `State` 那一格在**两支**里都不会是 `WaitingReconnect` 之外的东西）");

                        // ---- ③-8 🆕 2026-10-18（A914 第四续）：**判负对象两端不同** ----
                        //   客机 = 【本机是断线那一端】⇒ 该调 `NetSelfResign`（判**自己**）、**不该**调 `NetRemoteResign`。
                        //   判据 = 原版 `FailedToReconnectAfterDisconnect.c:26-27` 的 `DeadHero(我, 3)`（第二个实参 = 1）。
                        //   🧨 **判别式（两端不同源，删任一端分支只红对的那条）**：
                        //     · 把客机那一支改成 `_d.NetRemoteResign(...)` ⇒ **下面三条全红**（`SelfResignCalls` 停在 0、
                        //       `ResignCalls` 变 1、`cbC.Ctx` 的 `ForfeitedBy` 变成 **0**）；
                        //     · 把主机那一支改成 `NetSelfResign(...)` ⇒ **上面 ③-5 那两条红**（不是这三条）。
                        //     ⚠️ A913 之后这两个口**各多带一个理由码** ⇒ 这里写「改成另一个口」时记得照抄它那一档的码
                        //       （`NetRemoteResign(BattleResult.Disconnect)` / `NetSelfResign(BattleResult.Disconnect)`）。
                        Eq(cbC.SelfResignCalls, 1,
                           "★★（A914·两端判别式）**客机这一端**走的是 `NetSelfResign`（判**自己**）—— 恰一次"
                         + " —— 判据 = 原版 `FailedToReconnectAfterDisconnect.c:26-27`"
                         + "（`AddResignAction(1)` + `DeadHero(param_1, 1, 3)`，第二个实参 `1` = **本机死**）");
                        Eq(cbC.ResignCalls, 0,
                           "★★（A914·两端判别式）…而且**一次都没判对面**"
                         + " —— 🧨 改坏法：把客机那一支改回 `_d.NetRemoteResign()` ⇒ 红"
                         + "（它与主机那两条**不同源**：同一方法、两端判的不是同一个人）");
                        // ---- ③-8' 🆕 2026-10-18（**A913**）：**本机是断线那一端**的理由码 ----
                        //   原版 `FailedToReconnectAfterDisconnect.c:32`：`DeadHero(param_1, 1, 3)`
                        //   —— 第二个实参 `1` = 本机死、**第三个实参 3 = `Disconnect`**。
                        Ok(cbC.HasSelfReason, "（A913）夹具：客机那一端真的走过 `NetSelfResign`（不是没调）");
                        Eq((int)cbC.LastSelfResignReason, (int)BattleResult.Disconnect,
                           "★★（A913）**本机重连失败 ⇒ 理由码也是 `Disconnect`(3)**"
                         + "（原版 `FailedToReconnectAfterDisconnect.c:32` 的 `DeadHero(param_1, 1, 3)`）"
                         + " —— 🧨 改坏法：把那一句传成 `BattleResult.Forfeit` ⇒ 红");
                        Ok(cbC.LastSelfResignReason != BattleResult.Forfeit,
                           "★★（A913）…不是投降那一档"
                         + "（⚠️ 与主机那一格**互为对照**：两边判的都是**座位 1**（A914 要的那个一致性），"
                         + "所以分辨力只能来自**理由码**这两条，⛔ 别把它写成座位断言）");
                        Ok(cbC.Ctx.Events.Exists(e => e.Contains("[BattleResult] Disconnect(3) seat=1")),
                           "★★（A913）…客机那一份 `ctx.Events` 上同样落了地（座位 1 = 本机）");
                        Ok(!cbC.Ctx.Events.Exists(e => e.Contains("[BattleResult] Forfeit(2)")),
                           "★★（A913）…反向：没有投降那一档的 token");
                        Eq(cbC.Ctx.ForfeitedBy, hbC.Ctx.ForfeitedBy,
                           "★★（A914）**两端报的是同一个座位**（本端判 `_me`、对端判 `1−对面_me` ⇒ 两边 `ForfeitedBy` 相同）"
                         + $"（客机 `{cbC.Ctx.ForfeitedBy}` vs 主机 `{hbC.Ctx.ForfeitedBy}`）"
                         + " —— 🔴 **这正是「判本机负」修掉的那件事**：改回旧写法时两端**各判自己赢**、`ForfeitedBy` 一家说 0 一家说 1"
                         + "；🧨 把客机那一支改回 `NetRemoteResign()` ⇒ 红");

                        hNBC.Detach(); cNBC.Detach();
                        hsC.Close(false); csC.Close(false);
                        NetConfig.Current.port = savedPortC;
                    }
                }
                finally
                {
                    NetBattle.SetClockForTest(FakeClock);      // 还回上面那条（`_clk`）
                }
            }

            // ---- 11) 🆕 2026-10-17（B23·A902 附带那一格）：**接上联机层那一刻对面就已经不在了** ----
            //   为什么单开一段：`MsgStart` 与「真进战场」之间隔着一次 `LoadScene`（客户端还可能被
            //   `HoldForPresentation` 多留一口气）—— 对面正好在那一小段里掉线 / 离开的话，
            //   `OnPeerLost` / `OnClosed` **在 `NetBattle` 挂上之前就烧掉了** ⇒ 这一局会**静默地停在那里**
            //   （而大厅那一层那一刻已经交权：`NetRuntime.LobbyHandled` 被 `Attach` 置成了 false，
            //   它按定义不再开口 —— 见 `NetMatchmaking.LobbyOwnsSession`）。
            //   `NetBattle.WireSession` 末尾那两句就是补这一格：接上时若会话已经不在（`WaitingReconnect` /
            //   `Closed`）⇒ **补报一次**，走的是与实时掉线**同一对** handler（不是另写一套口径）。
            //   ⚠️ 只看那两档：`Off` 是**本机自己关的**（`Editor/BattleScene.cs` 那个 `RecordingTransport`
            //      夹具就是 `Off` 接上来的 ⇒ 在那儿多弹一条会把它的通知计数打乱）。
            //   🧨 判别式：删掉 `WireSession` 末尾那两句 ⇒ 本段「该补的补了」那条红；
            //      把补报写成无条件的 ⇒ 反例那条红。
            {
                int savedPort11 = NetConfig.Current.port;
                NetConfig.Current.port = FreePort();
                NetConfig.Current.ip = "127.0.0.1"; NetConfig.Current.password = "";
                var hs11 = NetSession.NewTcp(); var cs11 = NetSession.NewTcp();
                Ok(hs11.StartHost(NetConfig.Current), "（补齐·另一局）主机起来监听");
                cs11.CheckConnection(NetConfig.Current);
                Ok(PumpUntil(hs11, cs11, () => hs11.State == NetState.Lobby && cs11.State == NetState.Lobby, 8000),
                   "（补齐·另一局）握手走完");
                hs11.EnterBattle(); cs11.EnterBattle();

                // 对面掉线 ⇒ 主机进「等重连」；**此刻主机这台会话上还没有任何 `NetBattle`**
                //（模拟的正是「大厅已交权、对局还没接上」那个空档）
                cs11.Transport.ClosePeer();
                Ok(PumpUntil(hs11, cs11, () => hs11.State == NetState.WaitingReconnect, 8000),
                   $"（补齐）主机进「等重连」（实际 {hs11.State}）");
                NetRuntime.DrainNoticesForTest();          // 清一把（本段只认自己那一条）

                var b11 = new BareHost { Ctx = null, MySeat = 0 };
                var nb11 = NetBattle.Attach(b11, hs11, isHost: true);
                var n11 = NetRuntime.DrainNoticesForTest();
                Eq(n11.Length, 1,
                   "★（补齐）**接上联机层时会话已经在 `WaitingReconnect` ⇒ 补报一条**"
                 + "（原来这一档一声不响 —— 玩家会在一个对面早就不在的局里干等）"
                 + " —— 🧨 改坏法：删掉 `NetBattle.WireSession` 末尾那两句 ⇒ 红");
                Ok(n11.Length >= 1 && n11[0].Contains("掉线"),
                   $"★（补齐）那条话是对局那一档的口径（实得「{(n11.Length > 0 ? n11[0] : "")}」）");
                Ok(b11.LastSay != null && b11.LastSay.Contains("掉线"), "★（补齐）提示行也说了");
                Ok(nb11.ClockPaused, "★（补齐）对局时钟的闸**也按下了**（原版那一刻 `ClockManager.PauseClock()`）");
                Ok(!nb11.ReconnectCountdownRunning,
                   "（补齐）这一次的裸 host **没有 `Ctx`** ⇒ 不起倒计时、只出声"
                 + "（`StartReconnectCountdown` 那条如实记录的分支）");
                nb11.Detach(); hs11.Close(false); cs11.Close(false);

                // ---- 反例（**不同源**）：会话在 `Lobby`（正常那一帧）接上 ⇒ 一句都不许补 ----
                {
                    NetConfig.Current.port = FreePort();
                    var hs11b = NetSession.NewTcp(); var cs11b = NetSession.NewTcp();
                    Ok(hs11b.StartHost(NetConfig.Current), "（补齐·反例）主机起来监听");
                    cs11b.CheckConnection(NetConfig.Current);
                    Ok(PumpUntil(hs11b, cs11b, () => hs11b.State == NetState.Lobby && cs11b.State == NetState.Lobby, 8000),
                       "（补齐·反例）握手走完（两边都在 `Lobby`）");
                    NetRuntime.DrainNoticesForTest();
                    var b11b = new BareHost { Ctx = null, MySeat = 0 };
                    var nb11b = NetBattle.Attach(b11b, hs11b, isHost: true);
                    Eq(NetRuntime.DrainNoticesForTest().Length, 0,
                       "★（补齐·反例）会话在 `Lobby` 接上 ⇒ **不许补报**"
                     + " —— 与上面那条**不同源**：上面验「该补的补了」，这条验「不该补的别乱补」"
                     + "（两边一起改坏 = 把补报写成无条件 ⇒ **只有这条红**）");
                    Ok(!nb11b.ClockPaused, "★（补齐·反例）时钟闸也没被按下");
                    nb11b.Detach(); hs11b.Close(false); cs11b.Close(false);
                }

                NetConfig.Current.port = savedPort11;
            }

            // ---- 12) 🆕 2026-10-17（B27·A912）：**按「有没有说再见」分档判弃权** ----
            //   账 `A912`：对局中对手消失有**两路** ——
            //     **A 路**（点了投降/退出那颗钮）**先发 `Resign`** ⇒ 对面立刻判他负
            //       （`NetBattle.Dispatch` 的 `case NetKind.Resign` → `NetRemoteResign()`）—— **这一路本来就是对的**；
            //     **B 路**（没点投降就没了：关应用 / 场景卸载 / 主机重开）只走 `LeaveNetRoom()` →
            //       `NetSession.Close(say: true, …)` → `Send(NetKind.Bye)` ⇒ 原来对面**只看到「联机对局结束」、
            //       谁也没赢、一直挂着**。
            //   本件补的是 **B 路**：**收到 `bye` ⇒ 直接判他弃权**；而**心跳超时（没有 `bye`）保持原样**
            //   （仍走 A900 那 30 秒 —— 他可能真回来，对局内重连本来就支持）。
            //   🔴 **口径全文（含「为什么这么分档」、为什么不能接到 `StartReconnectCountdown`、
            //   以及「这是我们的口径、不是复刻」那一条）→ `Net/NetBattle.cs` 的 `HandlePeerClosed` 头上那一大段。**
            //
            //   🧨 **判别式（删掉对应实现就必红）**：
            //     ① 删掉 `HandlePeerClosed` 里那句 `ForfeitPeerIfLeftMidGame()` ⇒ 12① 红；
            //     ② 把那一格写成**无条件**（去掉 `_d.Ctx.IsOver` 那道闸）⇒ 12③ 红；
            //     ③ 把这一档并回 `HandlePeerLost`（= `bye` 也去起倒计时）⇒ 12④ 红。
            //   ⚠️ **12① 与 12③ 是【不同源】的一对**：① 看 `Ctx.Winner`（该判的判了）；
            //      ③ 看 `ResignCalls`（**已经打完的局不许再判一次**）—— 只盯 `Winner` 是**测不出**的：
            //      `RuleCore.Forfeit` 自己会早退（`RuleCore.cs:3685`）⇒ 两边一起改坏也照样绿。
            {
                int savedPort12 = NetConfig.Current.port;
                var rt12 = NetRuntime.Ensure();
                bool keepLobby12 = rt12.LobbyHandled;
                var keepSess12 = rt12.Session;
                try
                {
                    // ============ 12①-② 对面**没说再见就没了**（`bye`）⇒ 判他弃权 + 一台会话只弹一次 ============
                    {
                        var f = SetupA912(pool, 20261030, "A912·局没打完");
                        // 🔴 大厅那一半**也挂在这一台会话上**（生产上它在会话出生时就挂了 —— `NetRuntime.Init`
                        //    / `Reset` / `AttachForTest` 那三处；`NetBattle.Attach` 随后把闸置 false）。
                        rt12.AttachForTest(f.hs);
                        NetMatchmaking.Reset();
                        Ok(!rt12.LobbyHandled,
                           "（A912①）夹具：接上对局那一半 ⇒ **大厅那一半已经交权**（`NetRuntime.LobbyHandled == false`）");
                        Ok(!f.hb.Ctx.IsOver && !f.cb.Ctx.IsOver,
                           "（A912①）夹具：这一局**还没打完**（两端 `Ctx.IsOver == false` = 「他没投降就没了」那档的判据）");
                        NetRuntime.DrainNoticesForTest();          // 本段只认自己那几条
                        f.hb.LastSay = null;

                        f.cs.Close(true, "自检：对面关掉了这一局");   // = B 路（`BattleDriver.OnDestroy` → `LeaveNetRoom`）
                        Ok(PumpOne(f.hs, () => f.hs.State == NetState.Closed, 4000),
                           $"（A912①）主机收到 `bye` ⇒ 会话关上（实际 {f.hs.State}）");
                        Ok(f.hb.Ctx.Winner == 1 && f.hb.Ctx.ForfeitedBy == 1,
                           "★（A912①）**对面 `bye` + 本局还没结束 ⇒ 直接判他弃权**"
                         + $"（本机座位 0 ⇒ 判座位 1 负、本机胜；`Winner` = {f.hb.Ctx.Winner}、"
                         + $"`ForfeitedBy` = {f.hb.Ctx.ForfeitedBy}）"
                         + " —— 🧨 改坏法：删掉 `HandlePeerClosed` 里那句 `ForfeitPeerIfLeftMidGame()` ⇒ 红");
                        Eq(f.hb.ResignCalls, 1,
                           "★（A912①）…而且走的是**现成那个判负入口**（`INetBattleHost.NetRemoteResign`，**恰一次**）"
                         + "—— 口径：判负入口只有这一处，⛔ 别新造口");
                        var n12 = NetRuntime.DrainNoticesForTest();
                        Eq(n12.Length, 1,
                           "★（A912②·一台会话不许弹两次）这一跳**只弹一条**"
                         + " —— 大厅那一半（`NetMatchmaking`）也挂在同一台会话上，而它一个字都没说："
                         + "判据 = `NetRuntime.LobbyHandled`（对局那一半接上时置 false）"
                         + " —— 🧨 改坏法：把 `NetMatchmaking.LobbyOwnsSession` 那道闸去掉 ⇒ 这一条变成 2 条");
                        Ok(n12.Length >= 1 && n12[0].Contains("弃权"),
                           $"★（A912②）那条话**说出了结局**（判他弃权 / 这一局你赢了；实得「{(n12.Length > 0 ? n12[0] : "")}」）");
                        Ok(f.hb.LastSay != null && f.hb.LastSay.Contains("弃权"),
                           $"★（A912②）**提示行**上也说了（实得「{f.hb.LastSay}」）");
                        Ok(!NetMatchmaking.PeerGone,
                           "★（A912②）…而且大厅那一半**连边沿都没置**（`NetMatchmaking.PeerGone == false`）"
                         + " —— 与上面那条**不同源**：上面数的是「弹了几条」，这条看的是「另一半有没有偷偷记账」");
                        f.hs.Close(false); f.cs.Close(false);
                    }

                    // ============ 12③ 这一局**已经打完了** ⇒ 不许再判一次（判负入口一次都不许调）============
                    {
                        var f = SetupA912(pool, 20261031, "A912·局已打完");
                        // 让这一局先结束掉：本机投降（`ForfeitedBy = 0`、`Winner = 2`）—— 与「对面 bye」无关
                        // 🆕 A913：投的是**投降**那一档 ⇒ 理由码显式给 `Forfeit`(2)
                        RuleCore.Forfeit(f.hb.Ctx, 0, BattleResult.Forfeit);
                        Ok(f.hb.Ctx.IsOver && f.hb.Ctx.ForfeitedBy == 0,
                           "（A912③）夹具：这一局**已经结束**了（本机先投的降；`ForfeitedBy` = 0）");
                        NetRuntime.DrainNoticesForTest();

                        f.cs.Close(true, "自检：对面关掉了这一局");
                        Ok(PumpOne(f.hs, () => f.hs.State == NetState.Closed, 4000),
                           $"（A912③）主机收到 `bye`（实际 {f.hs.State}）");
                        Eq(f.hb.ResignCalls, 0,
                           "★（A912③）**已经打完的局 ⇒ 不许再判一次弃权**（判负入口**一次都没调**）"
                         + " —— 🧨 改坏法：把 `ForfeitPeerIfLeftMidGame` 里 `_d.Ctx.IsOver` 那道闸去掉 ⇒ 红"
                         + "（⚠️ 光看 `Winner` 是**测不出来**的：`RuleCore.Forfeit` 自己会早退，两边一起变）");
                        Eq(f.hb.Ctx.ForfeitedBy, 0,
                           "★（A912③）…「谁弃的权」还是本机自己那一次（没被对面这条 `bye` 改写）");
                        var n13 = NetRuntime.DrainNoticesForTest();
                        Eq(n13.Length, 1,
                           "（A912③）这一路**仍然要出声**（「不判弃权」≠「不说话」）—— 这一台会话上只有"
                         + "对局那一半挂着（大厅那一半的接线在 12① 那台已关掉的会话上）⇒ 恰好 1 条");
                        f.hs.Close(false); f.cs.Close(false);
                    }

                    // ============ 12④ **只有掉线（没有 `bye`）⇒ 保持原样**：不判弃权，照 A900 起 30 秒 ============
                    {
                        var f = SetupA912(pool, 20261032, "A912·只有掉线");
                        Ok(!f.hb.Ctx.IsOver, "（A912④）夹具：局还没打完");
                        NetRuntime.DrainNoticesForTest();

                        f.cs.Transport.ClosePeer();                    // 🔴 **拔网线**那一档：对面没来得及说再见
                        // ⚠️ **只推主机那一台**：拔线是主机自己的 socket 收到 FIN 判出来的（同 §11 那条），
                        //    推对面反而会让它开始自动重连 —— 那与本节要验的东西无关（多余的动静）。
                        Ok(PumpOne(f.hs, () => f.hs.State == NetState.WaitingReconnect, 8000),
                           $"（A912④）主机发现对面掉线、进等待重连（实际 {f.hs.State}）");
                        Eq(f.hb.ResignCalls, 0,
                           "★（A912④）**只有掉线（没有 `bye`）⇒ 不许判弃权**（判负入口一次都没调）"
                         + " —— 🧨 改坏法：把这一档也并进 `HandlePeerClosed`（或反过来把 `bye` 并进 `HandlePeerLost`）⇒ 红");
                        Ok(f.hNB.ReconnectCountdownRunning && f.hNB.ReconnectCountdownLeft == 30,
                           $"★（A912④）…而是照 **A900** 起那 30 秒倒计时（实得 {f.hNB.ReconnectCountdownLeft} 秒 ·"
                         + " 他可能真回来 —— 对局内重连本来就支持）");
                        Eq(f.hb.Ctx.Winner, 0, "★（A912④）…这一局**还没判**（`Winner` 还是 0）"
                                             + " —— 与 12① 是同一格判据的两面（「说了再见」vs「没说再见」）");
                        f.hNB.Detach(); f.cNB.Detach();                 // 收掉倒计时（别留给后面的用例）
                        f.hs.Close(false); f.cs.Close(false);
                    }

                    // ============ 12⑤ **对照（灭自证）**：两个半边都打开 ⇒ 大厅那一半**真的会说话** ============
                    //   🔴 为什么要这一格（`CLAUDE.md` §三「灭自证」那条）：12② 那句「只弹一条」如果
                    //      **大厅那一半压根没挂上这台会话**，也一样是 1 条 ⇒ 那是一条**恒真**的断言。
                    //      这一格把闸**故意**放回 `true`（换一个开关、别的都不动）：能同时证明
                    //      ① 那根接线**真的挂在这一台会话上**；② 12② 那一条是**那道闸在挡**。
                    //   ⛔ **真机上这个组合不该出现**（`NetBattle.Attach` 会把 `LobbyHandled` 置 false）——
                    //      这一格是**对照**，不是「期望的行为」。
                    {
                        var f = SetupA912(pool, 20261033, "A912·对照双方都开");
                        rt12.LobbyHandled = true;      // ← **故意**把闸打开（对照，只有这一格这么做）
                        rt12.AttachForTest(f.hs);
                        NetMatchmaking.Reset();
                        NetRuntime.DrainNoticesForTest();

                        f.cs.Close(true, "自检：对照用的一局");
                        Ok(PumpOne(f.hs, () => f.hs.State == NetState.Closed, 4000),
                           "（A912⑤·对照）对面 `bye` —— 这一格是**对照**，不是正常路径");
                        Eq(NetRuntime.DrainNoticesForTest().Length, 2,
                           "★（A912⑤·对照）两个半边都打开 ⇒ **两条**（大厅一条 + 对局一条）"
                         + " —— 这一格的作用是**证明大厅那一半真的挂在同一台会话上**"
                         + "（不然 12② 那条「只弹一条」可能只是「没有订户」= 恒真断言）");
                        Ok(NetMatchmaking.PeerGone,
                           "★（A912⑤·对照）大厅那一半**置上了它那个边沿**（`NetMatchmaking.PeerGone == true`）"
                         + " —— 同一根接线在 12② 里一个字节都没动，差别只有那道闸");
                        f.hs.Close(false); f.cs.Close(false);
                    }
                }
                finally
                {
                    rt12.AttachForTest(keepSess12);
                    rt12.LobbyHandled = keepLobby12;
                    NetMatchmaking.Reset();
                    NetRuntime.DrainNoticesForTest();
                    NetConfig.Current.port = savedPort12;
                }
            }

            // ---- 13) 🆕 2026-10-18（**A913**）：**对面真的「点了投降」那一档** ⇒ 理由码 `Forfeit`(2) ----
            //   为什么单开一格：A913 这条账的**全部内容**就是「原来**两档走同一个口**」——
            //   上面 ③-5' / ③-8' 钉的是「掉线 ⇒ 3」，这一格钉它的**反面**：「点了投降 ⇒ 2」。
            //   ⛔ 少了这一格，**把两档都传 3 也照样全绿**（`Disconnect` 那一组一条都不会红）——
            //   那正是「弱断言分不出两态」。
            //   走的是**真链**（不是直调）：`NetBattle.OnLocalResign()`（= `BattleDriver.Forfeit()`
            //   末尾那一跳）→ `NetKind.Resign` → 对面 `Dispatch` 的 `case NetKind.Resign`。
            {
                int savedPort13 = NetConfig.Current.port;
                var f13 = SetupA912(pool, 20261040, "A913·对面点投降");
                f13.hb.LastSay = null;
                Ok(!f13.hb.Ctx.IsOver && !f13.cb.Ctx.IsOver, "（A913·投降）夹具：这一局**还没打完**");
                f13.cNB.OnLocalResign();                       // 客机点了投降（真入口）
                Ok(PumpUntil2(f13.hs, f13.cs, f13.hNB, f13.cNB, () => f13.hb.Ctx.IsOver, 8000),
                   $"（A913·投降）主机收到那条 `Resign` ⇒ 立刻判完（`Winner` = {f13.hb.Ctx.Winner}）");
                Ok(f13.hb.HasRemoteReason, "（A913·投降）夹具：走的是判负入口（不是「一次都没调」）");
                Eq((int)f13.hb.LastResignReason, (int)BattleResult.Forfeit,
                   "★★（A913）**对面点投降 ⇒ 理由码 `Forfeit`(2)**"
                 + "（原版 `BattleManager__ReceiveEnemyForfeit.c:37` 就是 `DeadHero(param_1, 0, 2)` —— 那个 2 是**写死**的，"
                 + "因为对面发的那条 RPC 根本不带参数）"
                 + " —— 🧨 改坏法：把 `case NetKind.Resign` 那一句改成传 `BattleResult.Disconnect` ⇒ 红");
                Ok(f13.hb.LastResignReason != BattleResult.Disconnect,
                   "★★（A913）…而且**不是**掉线那一档（3）"
                 + " —— 与 ③-5' 那两条**互为反例**：**同一个方法**（`INetBattleHost.NetRemoteResign`）"
                 + "在两档里必须给出**不同的**码（那正是这条账要钉的东西）");
                Ok(f13.hb.Ctx.Events.Exists(e => e.Contains("[BattleResult] Forfeit(2) seat=1")),
                   "★★（A913）…引擎照着码落了地（`ctx.Events` token = 第二个观测口，与桩不同源）");
                Ok(!f13.hb.Ctx.Events.Exists(e => e.Contains("[BattleResult] Disconnect(3)")),
                   "★★（A913）…反向：这一局**没有**掉线那一档的 token");
                f13.hs.Close(false); f13.cs.Close(false);
                NetConfig.Current.port = savedPort13;
            }
        }
        catch (Exception e)
        {
            Fail("自检本身炸了：" + e);
        }
        finally
        {
            NetBattle.SetClockForTest(null);      // 🔴 把钟还回去（别把静态状态留给下一条自检）
            NetSession.SetClockForTest(null);     // 🔴 同上：`NetSession` 那个钟也是**进程级**的
            NetConfig.OverridePath = null;
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }

        Debug.Log($"{P} ===== 通过 {_pass} · 失败 {_fail} =====");
        // ⚠️ 「验不了、如实出声」那几格**单起一行**：上面那行的格式**保持逐字不变**
        //    （别的地方按 `===== 通过 N · 失败 M =====` 认这一条自检的结果）。
        if (_warn > 0)
            Debug.LogWarning($"{P} ===== 跳过/警告 {_warn}（**验不了的格子，不算失败** —— 逐条理由见上面那几条 ⚠️）=====");
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

    /// <summary>🆕 2026-10-17（B17）：**假钟**（秒）。`NetBattle` 的掉线倒计时读的就是它
    /// （`NetBattle.SetClockForTest`）—— **不推它就不走**。
    /// 为什么要它：原版那条倒计时是**真实 30 秒**（`WaitForSecondsRealtime(1.0f) × N`），
    /// 自检真等 30 秒既慢、又会让「谁先掉线」这类用例变成计时敏感。
    /// ⚠️ 只有 §10 明确 `clk += …` 时才推进；其余各段把它当「冻住」用。</summary>
    static float _clk;
    static float FakeClock() { return _clk; }

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

    /// <summary>🆕 2026-10-17（B27）：**只推一台**会话。
    /// 🔴 为什么要单独一个：只推一边才不会让**对面也报一次** —— 两边一起推的话通知队里就有两条，
    /// 而「一次事件弹几条」正是本段要数的东西（§9 里那个同名局部 lambda 是同一个道理）。
    /// ⚠️ 第二个参数**没有空判**（给它 null 会 NPE）—— 调用方传非 null 的 lambda。</summary>
    static bool PumpOne(NetSession s, Func<bool> cond, int ms)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < ms) { s.Pump(); if (cond()) return true; Thread.Sleep(3); }
        s.Pump(); return cond();
    }

    /// <summary>🆕 2026-10-17（B27·A912）：**搭一局「对局中」的裸 fixture** —— 两台会话 + 两个裸 host
    /// （各一份同输入的 `BattleContext`，**局还没打完**）+ 两端都接上 `NetBattle`。
    /// 与 §6 / §9 / §10③ 走的是**同一份 `INetBattleHost` 契约**（不是另写一套简化实现）。</summary>
    static (NetSession hs, NetSession cs, BareHost hb, BareHost cb, NetBattle hNB, NetBattle cNB)
        SetupA912(List<CardDef> pool, int seed, string tag)
    {
        NetConfig.Current.ip = "127.0.0.1"; NetConfig.Current.password = "";
        NetConfig.Current.port = FreePort();
        var hs = NetSession.NewTcp(); var cs = NetSession.NewTcp();
        Ok(hs.StartHost(NetConfig.Current), $"（{tag}）主机起来监听");
        cs.CheckConnection(NetConfig.Current);
        Ok(PumpUntil(hs, cs, () => hs.State == NetState.Lobby && cs.State == NetState.Lobby, 8000),
           $"（{tag}）握手走完");
        var da = Shuffle(DeckBuilder.StarterDeck(pool, "Ultramarines", DeckBuilder.ClassicDeckSize,
                                                 new System.Random(seed), unitsOnly: false), seed ^ 0x71);
        var db = Shuffle(DeckBuilder.StarterDeck(pool, "Goff", DeckBuilder.ClassicDeckSize,
                                                 new System.Random(seed + 1), unitsOnly: false), seed ^ 0x72);
        // 两端**各建一份**（同输入 ⇒ 同状态）—— 本段只动**主机那一份**的胜负，不做指纹对账
        var hb = new BareHost
        {
            Ctx = RuleCore.NewBattle(da, db, seed, cardPool: pool, openMulligan: false,
                                     vars: GameplayVariables.Classic, firstSeat: 0),
            MySeat = 0,
        };
        var cb = new BareHost
        {
            Ctx = RuleCore.NewBattle(da, db, seed, cardPool: pool, openMulligan: false,
                                     vars: GameplayVariables.Classic, firstSeat: 0),
            MySeat = 1,
        };
        var hNB = NetBattle.Attach(hb, hs, isHost: true);
        var cNB = NetBattle.Attach(cb, cs, isHost: false);
        hs.EnterBattle(); cs.EnterBattle();      // 掉线 / 离开这两条只有「对局中」才走得通
        return (hs, cs, hb, cb, hNB, cNB);
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

    /// <summary>把一段调用期间打进控制台的日志**抓下来**（本仓现成那一套：`Application.logMessageReceived`，
    /// 同 `Editor/BattleScene.cs` 的 A410 段 / `Editor/SettingsScene.cs` 的 `CaptureErrors`）。
    /// **异常不往外抛** —— 走 `err` 交给调用方自己判（本自检有一趟调用**预期**会在中途炸，见 §8 的说明）；
    /// 钩子在 `finally` 里摘 ⇒ 异常路径上也不留一根悬着的钩子。</summary>
    static List<string> CaptureLogs(Action body, out Exception err)
    {
        var lines = new List<string>();
        err = null;
        Application.LogCallback h = (string m, string st, LogType ty) => lines.Add(m);
        Application.logMessageReceived += h;
        try { body(); }
        catch (Exception e) { err = e; }
        finally { Application.logMessageReceived -= h; }
        return lines;
    }

    /// <summary>抓到的日志里有没有**以这一段开头**的那一条。
    /// 🔴 用 `StartsWith` 而**不是** `Contains`：要认的是「**这一句自称是哪一档**」——
    ///    `Contains` 会被别的句子里顺带提到的词命中（那样写，改坏了也可能照样绿）。
    ///    ⚠️ 前缀带全角冒号，认的就是「标签 + `：` + 内容」这整句的开头。</summary>
    static bool HasLog(List<string> lines, string prefix)
    {
        if (lines == null) return false;
        for (int i = 0; i < lines.Count; i++)
            if (lines[i] != null && lines[i].StartsWith(prefix, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>（A530）裸驱动那两趟**预期**会在 `Begin(...)` 里中断（本自检没有 `interaction`/棋盘/相机）——
    /// **如实打出来**，但**不当失败**：本段要的是那句日志，而它在 `Begin(...)` **之前**就已经打出来了。
    /// ⚠️ 反过来说：**这里不许静默** —— 真中断了要说清是哪一趟、中断在什么异常上；
    ///    要是哪天真不炸了（有人给 `Begin` 补了守卫），也**照实说**（那更好，不是问题）。</summary>
    static void NoteProbeThrow(Exception e, string which)
    {
        if (e == null)
        {
            Debug.Log(P + $"   （A530·{which}）裸驱动这一趟**把 `Begin(...)` 跑完了**（没中断）—— 日志照旧已经拿到");
            return;
        }
        Debug.Log(P + $"   （A530·{which}）裸驱动这一趟在 `Begin(...)` 里中断：{e.GetType().Name}：{e.Message}"
                   + " —— **预期之内**：这句日志在 `Begin` 之前就打出来了（本段要的就是它），"
                   + "而裸驱动没有 `interaction` / 棋盘 / 相机（那是 `Editor/BattleScene.cs` 建的东西）。");
    }

    static void Ok(bool cond, string msg)
    {
        if (cond) { _pass++; Debug.Log(P + " ✓ " + msg); return; }
        Fail(msg);
    }
    static void Fail(string msg) { _fail++; Debug.LogError(P + " ✗ " + msg); }
    static void Eq(int got, int want, string msg) { Ok(got == want, $"{msg}（实际 {got}，应为 {want}）"); }
}
