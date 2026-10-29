// B98H4_Assert.cs — 第九十八会话 · 波 7 · 执行代理 `B98_H4`
//   账 `战斗逻辑驱动:1360`（`TroopsKilledCount`）/ `:1373`（`EnergySpent`）的 **B4 喂入点**
//   + `MissionChallenge` 三个模式闸注入口（`报告/B98_A11.md` §4·1 ~ §4·3）。
//
// ============================ 它盯的是什么（三段，缺一段都留不住）============================
//  ① **喂入点真的接上了**：`BattleDriver` 结算拍那句 `FeedMissionChallengeSignals(_me)`。
//     断言走**真信号总线**：先 `LiveOpsSignal.Register<T>`，再反射调那个私有方法，数收到的信号。
//  ② **按类型分档**：`EnergySpentOf(seat,1)` / `(seat,2)` 各发一条，⛔ 不是 `EnergySpentTotalOf` 一条。
//  ③ **三个模式闸装上 / 摘下**：装之前 `ValidateMatchConditions()` 恒真（= 原来的缺口），
//     装之后按 playMode / 回放 / 教程三档真判；`OnDestroy` 之后复原成 `null`（= 原版「没在打局 ⇒ 放行」）。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity，本节留给人 / 调度台）=============
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B98H4_Assert.Run -logFile "d:/4/_tmp_view/b98h4.log"
//   🔴 **接线由调度台做**：本文件没有挂到任何自检宿主（`Editor/*.cs` 是别人的文件）。判绿 = 小结那行 `✗ 0`。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 删掉 B4 那句 `FeedMissionChallengeSignals(_me)` ⇒ 第 5 段三条进度全 0 ⇒ 红。
//   · 把 `EnergySpentOf(seat, 类型)` 换成 `EnergySpentTotalOf(seat)` 塞进一条信号
//     ⇒ 第 3 / 5 段「CardCost 只拿到 5（不是 8）」红。
//   · 删掉 `if (_replaySession) return;` ⇒ 第 4 段「回放局一条都不发」红。
//   · 三个闸不装（或只装两个）⇒ 第 2 段「装之后 `ValidateMatchConditions()` 不再恒真」红。
//   · 把 `MatchIsReplay` 写成只认 160（漏 140 TutorialReplay）⇒ 第 2 段「TutorialReplay ⇒ 回放闸真」红。
//   · 删掉 `OnDestroy` 那三个 `= null` ⇒ 第 6 段红。
//   · 喂入写死座位 0 ⇒ 第 3 段 3·c「换座位（seat 1）也照发」红。
using System;
using System.Reflection;
using CardPresentation;
using UnityEngine;

public static class B98H4_Assert
{
    const string P = "B98H4 ";

    static int _ok, _bad;

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; } else { _bad++; }
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    const BindingFlags AnyInst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static MethodInfo _feed, _install;
    static PropertyInfo _ctxProp;
    static FieldInfo _replayField, _ctxBacking;

    // ── 信号计数（监听器都是静态方法组 ⇒ 好摘）────────────────────────────────
    static int _killTrue, _killFalse, _killNull;
    static int _cardSum, _cardN, _abilitySum, _abilityN, _energyNotPlayer, _energyOther;

    static void OnKill(TroopKilledSignal s)
    {
        if (s == null) { _killNull++; return; }
        if (s.killerIsPlayer) _killTrue++; else _killFalse++;
    }

    static void OnEnergy(EnergySpentSignal s)
    {
        if (s == null) { _energyOther++; return; }
        if (!s.IsPlayer) { _energyNotPlayer++; return; }
        if (s.Type == EnergyCostType.CardCost) { _cardSum += s.Cost; _cardN++; }
        else if (s.Type == EnergyCostType.AbilityCost) { _abilitySum += s.Cost; _abilityN++; }
        else { _energyOther++; }
    }

    static void ResetCounters()
    {
        _killTrue = _killFalse = _killNull = 0;
        _cardSum = _cardN = _abilitySum = _abilityN = _energyNotPlayer = _energyOther = 0;
    }

    // ── 夹具：走**原版装载口** `MissionEvent.UpdateSaveGame`（它会 `EnableChallenge` = 订阅 + `Initialize`）──
    sealed class Fixture
    {
        public MissionEvent ev;
        public MissionChallengeProgress progress;
        public int Value { get { return progress == null ? -1 : progress.currentValue; } }
    }

    static Fixture Build(MissionChallenge c)
    {
        var persist = new MissionPersistence();
        var p = new MissionChallengeProgress();
        p.entryId = "b98h4";
        p.challengeId = c.GetID();
        p.currentValue = 0;
        p.Challenge = c;
        persist.challengeProgress = new MissionChallengeProgress[] { p };

        var ev = new MissionEvent();
        ev.UpdateSaveGame(persist);

        var f = new Fixture();
        f.ev = ev;
        f.progress = p;
        return f;
    }

    // ── 反射口 ────────────────────────────────────────────────────────────
    static void ResolveReflection()
    {
        _feed = typeof(BattleDriver).GetMethod("FeedMissionChallengeSignals", AnyInst);
        _install = typeof(BattleDriver).GetMethod("InstallMissionChallengeMatchGates", AnyInst);
        _ctxProp = typeof(BattleDriver).GetProperty("Ctx", AnyInst);
        _ctxBacking = typeof(BattleDriver).GetField("<Ctx>k__BackingField", AnyInst);
        _replayField = typeof(BattleDriver).GetField("_replaySession", AnyInst);
    }

    static void AttachCtx(BattleDriver d, RuleEngine.BattleContext ctx)
    {
        try { if (_ctxProp != null) _ctxProp.SetValue(d, ctx); }
        catch (Exception e) { Debug.LogWarning(P + "Ctx 走 property 写失败（改走 backing field）：" + e.Message); }
        if (d.Ctx != ctx && _ctxBacking != null) _ctxBacking.SetValue(d, ctx);
    }

    static void InvokeFeed(BattleDriver d, int seat) { _feed.Invoke(d, new object[] { seat }); }
    static void InvokeInstall(BattleDriver d) { _install.Invoke(d, null); }
    static void SetPlayMode(BattleDriver d, RuleEngine.GameMode mode) { d.Ctx.PlayMode = mode; }
    static void SetReplaySession(BattleDriver d, bool v) { _replayField.SetValue(d, v); }

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "=== B98_H4 喂入点 / 模式闸自检 开始（账 战斗逻辑驱动:1360 / :1373）===");

        var savedFirstTwo = MissionChallenge.FirstTwoTutorialCompleted;
        var savedPlayMode = MissionChallenge.MatchPlayMode;
        var savedReplay = MissionChallenge.MatchIsReplay;
        var savedTutorial = MissionChallenge.MatchIsTutorial;

        // 教程门：原版「前两段教程没完成 ⇒ 只放行 DailyLoginChallenge」⇒ 自检里摆成「已完成」，
        // 否则本机存档状态会决定第 3~5 段的结果（**不确定的断言等于没断**）。
        MissionChallenge.FirstTwoTutorialCompleted = () => true;
        MissionChallenge.MatchPlayMode = null;
        MissionChallenge.MatchIsReplay = null;
        MissionChallenge.MatchIsTutorial = null;

        GameObject go = null;
        MissionChallenge[] cleanup = new MissionChallenge[0];
        try
        {
            ResolveReflection();

            var ctx = new RuleEngine.BattleContext(20261029);
            go = new GameObject("b98h4-driver");
            var d = go.AddComponent<BattleDriver>();
            AttachCtx(d, ctx);

            Section1_Prereq(d, ctx);
            if (_bad == 0)
            {
                Section2_Gates(d);
                Section3_Feed(d, ctx);
                Section4_ReplayGate(d, ctx);
                cleanup = Section5_EndToEnd(d, ctx);
                Section6_Teardown(ctx, go);
                go = null;                               // 第 6 段已销毁
            }
            else
            {
                Debug.LogError(P + "第 1 段前提不成立 ⇒ 后面几段跳过（这不是绿）");
            }
        }
        finally
        {
            LiveOpsSignal.Unregister<TroopKilledSignal>(OnKill);
            LiveOpsSignal.Unregister<EnergySpentSignal>(OnEnergy);
            for (int i = 0; i < cleanup.Length; i++)
            {
                var k = cleanup[i];
                if (k == null) continue;
                var tk = k as TroopsKilledCount;
                if (tk != null) { tk.OnReset(); continue; }
                var es = k as EnergySpent;
                if (es != null) es.OnReset();
            }
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            MissionChallenge.FirstTwoTutorialCompleted = savedFirstTwo;
            MissionChallenge.MatchPlayMode = savedPlayMode;
            MissionChallenge.MatchIsReplay = savedReplay;
            MissionChallenge.MatchIsTutorial = savedTutorial;
        }

        Debug.Log(P + "=== 小结：✓ " + _ok + " · ✗ " + _bad + " ===");
        if (_bad > 0) Debug.LogError(P + "有 " + _bad + " 条失败");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  1 · 前提（**硬断言先跑**：这一条红了，后面所有段都在对一份不成立的前提上做判断）
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section1_Prereq(BattleDriver d, RuleEngine.BattleContext ctx)
    {
        Debug.Log(P + "--- 1 前提 / 方法签名 ---");

        Check(_feed != null, "1 前提：`BattleDriver.FeedMissionChallengeSignals` 存在（私有实例方法）");
        Check(_install != null, "1 前提：`BattleDriver.InstallMissionChallengeMatchGates` 存在");
        Check(_replayField != null, "1 前提：`BattleDriver._replaySession` 字段找得到");
        if (_feed == null || _install == null) return;

        var fp = _feed.GetParameters();
        Check(_feed.ReturnType == typeof(void) && fp.Length == 1 && fp[0].ParameterType == typeof(int),
              "1 签名：`FeedMissionChallengeSignals(int seat)` —— **收座位号**，⛔ 不是 `PlayerState`（`报告/B98_A11.md` §4·2 里的 `me` 是笔误：`UpdateHud` 里 `me` 是 `PlayerState`）");
        Check(_install.ReturnType == typeof(void) && _install.GetParameters().Length == 0,
              "1 签名：`InstallMissionChallengeMatchGates()` 无参无返回");

        Check(d.Ctx == ctx, "1 前提：`Ctx` 已注入（反射写私有 setter）");
        Check(ctx.Tally != null, "1 前提：`Ctx.Tally` 有值");
        Check(ctx.MatchType == RuleEngine.MatchType.Ranked,
              "1 前提：新 `BattleContext` 默认 `PlayMode = Classic(0)` ⇒ `MatchType = Ranked(10)`");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  2 · 三个模式闸（装之前恒真 · 装之后三档真判 · 140 两头都真）
    //     判据：`decomp_full/MissionChallenge__ValidateMatchConditions.c:31-33` 起那棵树
    //           `decomp_full/BattleManager__IsReplayMatch.c:20-24`（= {0x8c, 0xa0}）
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section2_Gates(BattleDriver d)
    {
        Debug.Log(P + "--- 2 模式闸（装之前 / 装之后）---");

        // 探针：一条带 `playModeBlocklist = {0}` 的挑战（Classic 的 playMode 就是 0）
        var probe = new TroopsKilledCount();
        probe.id = "b98h4-gate-probe";
        probe.playModeBlocklist = new[] { 0 };

        Check(MissionChallenge.MatchPlayMode == null, "2 装之前：`MatchPlayMode == null`");
        Check(probe.ValidateMatchConditions(),
              "2 装之前：`ValidateMatchConditions()` **恒真** —— 这就是原缺口（= 原版「没有 `BattleManager`」那一支）");

        InvokeInstall(d);

        Check(MissionChallenge.MatchPlayMode != null && MissionChallenge.MatchIsReplay != null
              && MissionChallenge.MatchIsTutorial != null, "2 装之后：三个闸都非 null");
        Check(MissionChallenge.MatchPlayMode() == 0, "2 装之后：Classic ⇒ playMode 0（`GameMode` 与原版 `PlayModes` 同编号）");
        Check(!MissionChallenge.MatchIsReplay(), "2 装之后：Classic 不是回放");
        Check(!MissionChallenge.MatchIsTutorial(), "2 装之后：Classic 不是教程");
        Check(!probe.ValidateMatchConditions(),
              "2 **真闸**：`playModeBlocklist = {0}` ∧ Classic ⇒ **不放行**（装之前它是放行的 ⇒ 这就是缺口被补上的证据）");

        // 决斗：原版第二跳 `playMode == 1 ⇒ allowOnDuels`
        SetPlayMode(d, RuleEngine.GameMode.Duel);
        Check(MissionChallenge.MatchPlayMode() == 1, "2 决斗：playMode 1");
        Check(!probe.ValidateMatchConditions(), "2 决斗：`allowOnDuels` 默认 false ⇒ 不放行");
        probe.allowOnDuels = true;
        Check(probe.ValidateMatchConditions(), "2 决斗：`allowOnDuels = true` ⇒ 放行（`playMode == 1` 那一支真的走到了）");

        // 教程：原版第三跳 `IsTutorialMatch() ⇒ allowOnTutorials`
        SetPlayMode(d, RuleEngine.GameMode.Tutorial);
        probe.allowOnDuels = false;
        probe.allowOnTutorials = false;
        Check(MissionChallenge.MatchIsTutorial(), "2 教程：`Tutorial(100)` ⇒ 教程闸真");
        Check(!probe.ValidateMatchConditions(), "2 教程：`allowOnTutorials` 默认 false ⇒ 不放行");
        probe.allowOnTutorials = true;
        Check(probe.ValidateMatchConditions(), "2 教程：`allowOnTutorials = true` ⇒ 放行");

        // 🔴 140 TutorialReplay：**回放闸与教程闸必须同时为真**（原版两处判据都含 140）
        SetPlayMode(d, RuleEngine.GameMode.TutorialReplay);
        Check(MissionChallenge.MatchIsReplay(),
              "2 回放：`TutorialReplay(140)` ⇒ **回放闸真**（`MatchTypes.IsReplayMatch` = {140,160}；写成只认 160 这条会红）");
        Check(MissionChallenge.MatchIsTutorial(), "2 回放：`TutorialReplay(140)` 同时是教程（原版 `IsTutorialMatch` 也含 140）");

        SetPlayMode(d, RuleEngine.GameMode.Replay);
        Check(MissionChallenge.MatchIsReplay(), "2 回放：`Replay(160)` ⇒ 回放闸真");

        SetPlayMode(d, RuleEngine.GameMode.Classic);
        Check(!MissionChallenge.MatchIsReplay() && !MissionChallenge.MatchIsTutorial(), "2 复原：Classic ⇒ 两闸都假");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  3 · 喂入（空账不发声 · 按座位 · 按类型分档 · 累计值原样补发）
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section3_Feed(BattleDriver d, RuleEngine.BattleContext ctx)
    {
        Debug.Log(P + "--- 3 喂入（`FeedMissionChallengeSignals`）---");

        LiveOpsSignal.Register<TroopKilledSignal>(OnKill);
        LiveOpsSignal.Register<EnergySpentSignal>(OnEnergy);

        // 3·a 空账：一条都不许发（0 值不补发 —— 否则每局都往总线里灌空信号）
        ResetCounters();
        InvokeFeed(d, 0);
        Check(_killTrue == 0 && _killFalse == 0 && _cardN == 0 && _abilityN == 0,
              "3 空账：`Tally` 全是 0 ⇒ **一条信号都不发**");

        // 3·b 真账：座位 0 / 座位 1 各喂一半，分开数
        ctx.Tally.AddTroopKilled(0);
        ctx.Tally.AddTroopKilled(0);
        ctx.Tally.AddTroopKilled(0);                                                 // 座位 0 击杀 3
        ctx.Tally.AddTroopKilled(1);
        ctx.Tally.AddTroopKilled(1);                                                 // 座位 1 击杀 2（不许混进来）
        ctx.Tally.AddEnergySpent(0, 5, RuleEngine.AchievementTally.EnergyCostCard);
        ctx.Tally.AddEnergySpent(0, 3, RuleEngine.AchievementTally.EnergyCostAbility);
        ctx.Tally.AddEnergySpent(1, 9, RuleEngine.AchievementTally.EnergyCostCard);    // 座位 1 的花费（不许混进来）

        ResetCounters();
        InvokeFeed(d, 0);

        Check(_killTrue == 3, "3 击杀：座位 0 的 3 条 ⇒ 补发 3 条 `TroopKilledSignal(killerIsPlayer: true)`");
        Check(_killFalse == 0 && _killNull == 0, "3 击杀：没混进假信号 / 空信号");
        Check(_cardSum == 5 && _cardN == 1,
              "3 分档：`CardCost` 只一条、金额 **5**（若用 `EnergySpentTotalOf` 塞一条 ⇒ 这里是 8 ⇒ 红；座位错了 ⇒ 14）");
        Check(_abilitySum == 3 && _abilityN == 1, "3 分档：`AbilityCost` 只一条、金额 **3**（两类**各发一条**，⛔ 不是合计一条）");
        Check(_energyNotPlayer == 0, "3 座位：发出去的都是 `IsPlayer = true`（原版消费者第一道闸看这一位）");
        Check(_energyOther == 0, "3 其他：没有未知类型的能量信号");

        // 3·c 座位 1 自己的账也照发（证明「按传进来的座位取数」，不是写死 0）
        ResetCounters();
        InvokeFeed(d, 1);
        Check(_killTrue == 2, "3 换座位：`FeedMissionChallengeSignals(1)` ⇒ 补发座位 1 的 2 条击杀");
        Check(_cardSum == 9 && _abilityN == 0, "3 换座位：座位 1 只有 `CardCost 9`（`AbilityCost` 一条都没有）");

        LiveOpsSignal.Unregister<TroopKilledSignal>(OnKill);
        LiveOpsSignal.Unregister<EnergySpentSignal>(OnEnergy);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  4 · 回放闸（`_replaySession` ⇒ 一条都不发；翻回去必须立刻恢复）
    //     口径与 `AwardAchievements` 里那句 `bool replay = _replaySession;` 同一处判据
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section4_ReplayGate(BattleDriver d, RuleEngine.BattleContext ctx)
    {
        Debug.Log(P + "--- 4 回放闸 ---");

        LiveOpsSignal.Register<TroopKilledSignal>(OnKill);
        LiveOpsSignal.Register<EnergySpentSignal>(OnEnergy);

        SetReplaySession(d, true);
        ResetCounters();
        InvokeFeed(d, 0);
        Check(_killTrue == 0 && _cardN == 0 && _abilityN == 0,
              "4 回放局：`_replaySession = true` ⇒ **一条都不发**（回放不是「真打完」）");

        // 灭自证：只把那一位翻假 ⇒ 立刻按同一份账补发（证明第 1 条不是「账空了」挡住的）
        SetReplaySession(d, false);
        ResetCounters();
        InvokeFeed(d, 0);
        Check(_killTrue == 3 && _cardSum == 5 && _abilitySum == 3,
              "4 灭自证：只把 `_replaySession` 翻假 ⇒ 立刻补发 3 杀 / 5 / 3（同一条链，同一份账）");

        LiveOpsSignal.Unregister<TroopKilledSignal>(OnKill);
        LiveOpsSignal.Unregister<EnergySpentSignal>(OnEnergy);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  5 · 端到端（喂入 → 信号 → 消费者 → `UpdateProgress` → 存档行里的 `currentValue`）
    //     🔴 「灯是不是真的亮了」的判据在这里：只断「信号发出去了」等于没断（进度可能根本没落地）。
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static MissionChallenge[] Section5_EndToEnd(BattleDriver d, RuleEngine.BattleContext ctx)
    {
        Debug.Log(P + "--- 5 端到端（喂入 → 消费者 → 存档行）---");

        var kid = new TroopsKilledCount();
        kid.id = "b98h4-kill";
        // 里程碑必须 ≥1 条：`IsComplete()` 的算式是「`AvailableRewards()` 条数 ≤ `collectedRewards`」
        // ⇒ rewards 为空的挑战天生「已完成」，进度一律推不动（原版同）。
        kid.rewards = new[] { new ChallengeMilestone { difficulty = 1, targetValue = 99 } };
        var fk = Build(kid);

        var ec = new EnergySpent();
        ec.id = "b98h4-card-cost";
        ec.rewards = new[] { new ChallengeMilestone { difficulty = 1, targetValue = 99 } };
        ec.costCondition = new CostCondition { ComparisonMethod = ChallengeComparison.HigherOrEqual, Cost = 1 };
        ec.costType = EnergyCostType.CardCost;
        var fe = Build(ec);

        var ea = new EnergySpent();
        ea.id = "b98h4-ability-cost";
        ea.rewards = new[] { new ChallengeMilestone { difficulty = 1, targetValue = 99 } };
        ea.costCondition = new CostCondition { ComparisonMethod = ChallengeComparison.HigherOrEqual, Cost = 1 };
        ea.costType = EnergyCostType.AbilityCost;
        var fa = Build(ea);

        var all = new MissionChallenge[] { kid, ec, ea };

        Check(fk.progress != null && fe.progress != null && fa.progress != null,
              "5 前提：三条进度行都取到了（走 `MissionEvent.UpdateSaveGame` 真装载口）");
        if (fk.progress == null || fe.progress == null || fa.progress == null) return new MissionChallenge[0];
        Check(fk.Value == 0 && fe.Value == 0 && fa.Value == 0, "5 前提：三条进度都从 0 起");
        Check(MissionChallenge.MatchPlayMode != null, "5 前提：三个模式闸装着（本段走真闸，不是 `null` 放行）");

        InvokeFeed(d, 0);                                  // 账：座位 0 = 3 杀 / CardCost 5 / AbilityCost 3

        Check(fk.Value == 3, "5 端到端：`TroopsKilledCount` 进度 **3**（信号 → 消费者 → 存档行）");
        Check(fe.Value == 5, "5 端到端：`EnergySpent(CardCost)` 进度 **5**（金额；若喂了合计 8 这里就是 8 ⇒ 红）");
        Check(fa.Value == 3, "5 端到端：`EnergySpent(AbilityCost)` 进度 **3**（⛔ 没吃到 CardCost 那 5）");

        return all;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  6 · 拆机复原（`OnDestroy` 里那三个 `= null` ⇒ 原版「没有 `BattleManager` ⇒ 放行」）
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section6_Teardown(RuleEngine.BattleContext ctx, GameObject go)
    {
        Debug.Log(P + "--- 6 拆机复原（OnDestroy）---");

        Check(MissionChallenge.MatchPlayMode != null, "6 前提：销毁之前三个闸是装着的");

        var go2 = new GameObject("b98h4-teardown");
        var d2 = go2.AddComponent<BattleDriver>();
        AttachCtx(d2, ctx);
        InvokeInstall(d2);
        Check(MissionChallenge.MatchPlayMode != null, "6 前提：第二个 driver 也装上了");

        UnityEngine.Object.DestroyImmediate(go2);

        Check(MissionChallenge.MatchPlayMode == null && MissionChallenge.MatchIsReplay == null
              && MissionChallenge.MatchIsTutorial == null,
              "6 复原：销毁 driver ⇒ `OnDestroy` 把三个闸置回 `null`（= 原版「没在打局 ⇒ `ValidateMatchConditions()` 放行」；删掉那三个 `= null` 这条会红）");

        UnityEngine.Object.DestroyImmediate(go);           // 第 5 段那位也收掉（`Run` 里把 `go` 置 null，不再销毁）
    }
}
