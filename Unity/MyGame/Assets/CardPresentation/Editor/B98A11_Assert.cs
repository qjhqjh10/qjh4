// B98A11_Assert.cs — 第九十八会话 · 波 5e · 执行代理 `B98_A11`
//   账 `战斗逻辑驱动:1360`（`TroopsKilledCount.OnTroopKilled`）· 账 `战斗逻辑驱动:1373`（`EnergySpentSignal.Raise`）
//
// ============================ 它盯的是什么（三段，缺一段都留不住）============================
//  ① **A1 真缺口**：`Shell/MissionEvent.cs` 的 `OnChallengeProgressChanged`（原版虚槽 `+0x5d0`）
//     原来是**空实现** ⇒ 任何 `MissionChallenge` 子类都只广播「怎么改进度」、**没人真去改**。
//     判据 = `decomp_generic_trial/out/MissionEvent_object__object___UpdateChallengeProgress__102f660.c`。
//     本文件第 6 段**直接调** `ApplyProgressPayload` 把四跳（教程门 / 取进度行 / `IsComplete` 早退 / 真落地）逐条摆出来。
//  ② **A2 两个消费者**：`TroopsKilledCount`（只数我方击杀 ⇒ +1）· `EnergySpent`（四道闸 ⇒ 累加金额）。
//     第 2~5 段走**完整的真实链路**：`LiveOpsSignal.Raise(信号)` → 消费者 handler → `UpdateProgress`
//     → 广播 → `OnChallengeProgressChanged` → `ApplyProgressPayload` → 存档里的 `currentValue`。
//     🔴 **这就是「灯是不是真的亮了」的判据**：只断「handler 被调过」等于没断（进度可能根本没落地）。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity，本节留给人/调度台）=============
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B98A11_Assert.Run -logFile "d:/4/_tmp_view/b98a11.log"
//   🔴 **接线由调度台做**：本文件**没有**挂到任何自检宿主（`Editor/*.cs` 是别人的文件）。
//   判绿 = 小结那行 `✗ 0`。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 把 `Shell/MissionEvent.cs:224` 那句 `ApplyProgressPayload(payload);` 改回空体（或改成 `return;`）
//     ⇒ 第 2/3/4/5/6 段**一起红**（存档里的 `currentValue` 一个都不会动）。这是**本批最该防的那一改**。
//   · `ApplyProgressPayload` 里删掉「`progress.IsComplete()` ⇒ 早退」⇒ 第 6 段「已完成的挑战不再推进」红。
//   · `ApplyProgressPayload` 里删掉教程门 ⇒ 第 6 段「教程门挡住非 DailyLogin」红。
//   · `TroopsKilledCount.OnTroopKilled` 把 `if (!obj.killerIsPlayer) return;` 删掉 ⇒ 第 2 段「敌方击杀不算」红。
//   · `TroopsKilledCount.OnTroopKilled` 把 `UpdateProgress(1,false)` 改成 `UpdateProgress(1,true)` ⇒ 第 2 段
//     「三次 ⇒ 3」会变成「1」（`shouldOverride` 覆盖而非累加）⇒ 红。
//   · `EnergySpent.OnEnergySpent` 删任一闸 ⇒ 对应的那一条红；把 ④ 的 `obj.Cost` 改成 `1`（数次数而非金额）⇒ 第 5 段红。
//   · `EnergySpent` 的 `costType` 默认值从 `CardCost` 改成 `0` ⇒ 第 5 段「默认值」红。
//   · `EnergySpent.ContainsFlag` 写成 `(a & b) == b` ⇒ 第 5 段的真值表在 `flag == 0` 那一格红
//     （本批的信号只发 1/2，所以**只会由真值表那一条抓到** —— 这就是它存在的理由）。
//   · `ChallengeComparisonExtensions.Compare` 把 `HigherOrEqual` 当成 1（而不是 10）⇒ 第 5 段「枚举值」红。
//   · `TroopsKilledCount.OnReset` 不 Unregister ⇒ 第 4 段「摘订阅之后不再动」红。
using UnityEditor;
using UnityEngine;
using CardPresentation;

public static class B98A11_Assert
{
    const string P = "B98A11 ";

    static int _ok, _bad;

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; } else { _bad++; }
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    /// <summary>一个「存档行 + 事件」的小夹具（真链路要的就是这两件）。</summary>
    sealed class Fixture
    {
        public MissionEvent ev;
        public MissionChallengeProgress progress;
        public int Value { get { return progress == null ? -1 : progress.currentValue; } }
    }

    /// <summary>造「一个挑战 + 一条进度行 + 一个事件」—— 走**原版的装载口** `MissionEvent.UpdateSaveGame`
    /// （它会 `Subscribe + challenge.Initialize()`，所以信号订阅是真链路，⛔ 不是自检手工挂的）。</summary>
    static Fixture Build(MissionChallenge c)
    {
        var persist = new MissionPersistence();
        var p = new MissionChallengeProgress();
        p.entryId = "b98a11";
        p.challengeId = c.GetID();          // 原版按 `challengeId` 字符串找行（见 `GetProgressFor` 的判据）
        p.currentValue = 0;
        p.Challenge = c;                    // 原版由 `MissionChallengeProvider` 注入；本仓是显式口（见 `MissionPersistence.Bind`）
        persist.challengeProgress = new MissionChallengeProgress[] { p };

        var ev = new MissionEvent();
        ev.UpdateSaveGame(persist);

        var f = new Fixture();
        f.ev = ev;
        f.progress = p;
        return f;
    }

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "=== 挑战消费方两条自检 开始（账 战斗逻辑驱动:1360 / :1373）===");

        var savedFirstTwo = MissionChallenge.FirstTwoTutorialCompleted;
        var savedPlayMode = MissionChallenge.MatchPlayMode;
        var savedReplay = MissionChallenge.MatchIsReplay;
        var savedTutorial = MissionChallenge.MatchIsTutorial;

        // 教程门：原版「前两段教程没完成 ⇒ 只放行 DailyLoginChallenge」。自检里把它摆成「已完成」，
        // 否则本机存档状态会让第 2~5 段的结果取决于玩家进度（**不确定的断言等于没断**）。
        MissionChallenge.FirstTwoTutorialCompleted = () => true;
        MissionChallenge.MatchPlayMode = null;             // `null` ⇒ `ValidateMatchConditions` 直接放行（原版「没在打局」那一支）
        MissionChallenge.MatchIsReplay = null;
        MissionChallenge.MatchIsTutorial = null;

        try
        {
            Section1_Prereq();
            Section2_TroopsKilled();
            Section3_ReplayGate();
            Section4_OnReset();
            Section5_EnergySpent();
            Section6_ApplyProgressPayload();
        }
        finally
        {
            MissionChallenge.FirstTwoTutorialCompleted = savedFirstTwo;
            MissionChallenge.MatchPlayMode = savedPlayMode;
            MissionChallenge.MatchIsReplay = savedReplay;
            MissionChallenge.MatchIsTutorial = savedTutorial;
        }

        Debug.Log(P + "=== 小结：✓ " + _ok + " · ✗ " + _bad + " ===");
        if (_bad > 0) Debug.LogError(P + "有 " + _bad + " 条失败");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  1 · 前提（**硬断言先跑**：这一条红了，后面所有段都在对一份不成立的前提做判断）
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section1_Prereq()
    {
        Debug.Log(P + "--- 1 前提 ---");

        var c = new TroopsKilledCount();
        c.id = "b98a11-prereq";
        // 🔴 里程碑必须 ≥1 条：`MissionChallengeProgress.IsComplete()` 的算式是「`AvailableRewards()` 的条数
        //    ≤ `collectedRewards`」⇒ **rewards 为空的挑战天生「已完成」**，进度一律推不动（原版同）。
        c.rewards = new[] { new ChallengeMilestone { difficulty = 1, targetValue = 3 } };

        var f = Build(c);

        Check(c.GetID() == c.id, "前提：`GetID()` 读的是 id 字段（`MissionChallenge__GetID` = `return this.id`）");
        Check(f.ev != null, "前提：`MissionEvent` 实例建出来了");
        Check(f.ev.SaveGame != null, "前提：`UpdateSaveGame` 之后 `SaveGame` 有值");
        Check(f.progress != null, "前提：`GetProgressFor` 能按 `challengeId` 找到进度行");
        if (f.progress == null) { Debug.LogError(P + "前提不成立 ⇒ 后面几段跳过（这不是绿）"); return; }
        Check(f.progress.Challenge == c, "前提：进度行上的 `Challenge` 已注入（否则 `IsComplete()` 走「取不到挑战」那一支）");
        Check(!f.progress.IsComplete(), "前提：这条挑战**还没完成**（`1 <= collectedRewards(0)` 为假）");
        Check(c.IsInitialized, "前提：`UpdateSaveGame` 已把它 `Initialize()`（一次性闩置真）");

        c.OnReset();                                     // 这一段只验前提，别把订阅留给后面
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  2 · `TroopsKilledCount`（真 ×3 ⇒ 3 · 假 ⇒ 不变）
    //     判据：`decomp_full/TroopsKilledCount__OnTroopKilled.c`（全文 11 行）—— `killer.isPlayer` ⇒ +1
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static Fixture _killFx;

    static void Section2_TroopsKilled()
    {
        Debug.Log(P + "--- 2 `TroopsKilledCount`（我方击杀 +1）---");

        var c = new TroopsKilledCount();
        c.id = "b98a11-kill";
        c.rewards = new[] { new ChallengeMilestone { difficulty = 1, targetValue = 3 } };
        _killFx = Build(c);
        if (_killFx.progress == null) { Check(false, "2 前提：进度行取不到（本段无法判断）"); return; }

        Check(_killFx.Value == 0, "2 起点：`currentValue == 0`");

        LiveOpsSignal.Raise(new TroopKilledSignal(false));
        Check(_killFx.Value == 0, "2 灭自证：`killerIsPlayer = false`（敌方击杀）⇒ 不变（`.c:5-8` 第一道闸）");

        LiveOpsSignal.Raise(new TroopKilledSignal(true));
        Check(_killFx.Value == 1, "2 真×1：我方击杀 ⇒ 进度 1（且是**真的写进了存档行**，不是只调了 handler）");

        LiveOpsSignal.Raise(new TroopKilledSignal(true));
        LiveOpsSignal.Raise(new TroopKilledSignal(true));
        Check(_killFx.Value == 3, "2 真×3 累计 ⇒ 进度 3（`UpdateProgress(1,false)` = 累加；若写成 `true` 这里会是 1）");

        LiveOpsSignal.Raise(new TroopKilledSignal(false));
        Check(_killFx.Value == 3, "2 真后再假：`killerIsPlayer = false` ⇒ 仍是 3");

        LiveOpsSignal.Raise<TroopKilledSignal>(null);
        Check(_killFx.Value == 3, "2 空信号：`null` ⇒ 不变（原版这一跳 NRE ⇒ 同样不计数）");

        // 一次性桥（🔶 自己设计）也要断：它按条数补发，效果必须与逐杀一致
        _killFx.progress.currentValue = 0;               // 直接摆回 0（`UpdateProgress` 是 `protected`，自检调不到）
        Check(_killFx.Value == 0, "2 桥前归零：夹具直接摆回 `currentValue = 0`");
        int raised = TroopsKilledCount.RaiseKilledForPlayer(2);
        Check(raised == 2, "2 🔶 桥：`RaiseKilledForPlayer(2)` 返回 2");
        Check(_killFx.Value == 2, "2 🔶 桥：补发 2 条 ⇒ 进度 2（与「逐杀发 2 条」等价）");
        TroopsKilledCount.RaiseKilledForPlayer(0);
        Check(_killFx.Value == 2, "2 🔶 桥：`RaiseKilledForPlayer(0)` 什么都不发");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  3 · 回放闸（`MissionChallenge.ValidateMatchConditions` —— 回放局一律不算）
    //     判据：`decomp_full/MissionChallenge__ValidateMatchConditions.c:31-33` 起那棵树
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section3_ReplayGate()
    {
        Debug.Log(P + "--- 3 回放闸 ---");

        if (_killFx == null || _killFx.progress == null) { Check(false, "3 前提：没有第 2 段的夹具（本段无法判断）"); return; }
        int before = _killFx.Value;

        MissionChallenge.MatchPlayMode = () => 0;        // 0 = 非决斗模式
        MissionChallenge.MatchIsTutorial = () => false;
        MissionChallenge.MatchIsReplay = () => true;     // ★ 回放局

        LiveOpsSignal.Raise(new TroopKilledSignal(true));
        Check(_killFx.Value == before, "3 回放局：`MatchIsReplay = true` ⇒ 进度不变（`.c` 第二跳 `return false`）");

        MissionChallenge.MatchIsReplay = () => false;    // 同一局，只把「回放」翻掉 ⇒ 必须重新开始动
        LiveOpsSignal.Raise(new TroopKilledSignal(true));
        Check(_killFx.Value == before + 1, "3 灭自证：只把 `MatchIsReplay` 翻成假 ⇒ 立刻 +1（证明第 1 条不是别的原因挡住的）");

        MissionChallenge.MatchPlayMode = null;
        MissionChallenge.MatchIsReplay = null;
        MissionChallenge.MatchIsTutorial = null;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  4 · `OnReset`（原版 Slot 8 = `Signal.Unregister`）
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section4_OnReset()
    {
        Debug.Log(P + "--- 4 `OnReset` 摘订阅 ---");

        if (_killFx == null || _killFx.progress == null) { Check(false, "4 前提：没有第 2 段的夹具（本段无法判断）"); return; }
        int beforeA = _killFx.Value;

        // 🔴 **必须给第二个实例配一条自己的进度行**（`Build` 会走真装载口 ⇒ 真订阅）。
        //   只 `new` 一个再 `Initialize()` 是**测不出东西的**：它没有 `OnProgressUpdate` 订阅者，
        //   `UpdateProgress` 广播出去也没人接 ⇒ 「摘不摘订阅」结果一样（那就是自证断言）。
        var c2 = new TroopsKilledCount();
        c2.id = "b98a11-kill-2";
        c2.rewards = new[] { new ChallengeMilestone { difficulty = 1, targetValue = 3 } };
        var f2 = Build(c2);
        Check(f2.progress != null, "4 前提：第二个夹具的进度行取到了");
        if (f2.progress == null) return;
        Check(f2.Value == 0, "4 前提：第二条挑战进度从 0 起");

        LiveOpsSignal.Raise(new TroopKilledSignal(true));
        Check(_killFx.Value == beforeA + 1, "4 两条挑战都挂着 ⇒ A 的进度 +1");
        Check(f2.Value == 1, "4 两条挑战都挂着 ⇒ B 的进度 +1");

        c2.OnReset();                                    // 只摘 B
        LiveOpsSignal.Raise(new TroopKilledSignal(true));
        Check(_killFx.Value == beforeA + 2, "4 摘掉 B 之后 ⇒ A 照样 +1");
        Check(f2.Value == 1, "4 摘掉 B 之后 ⇒ **B 一点没动**（`OnReset` 真的 Unregister 了 —— 把 `Unregister` 删掉这条会红）");

        // 收尾：把第 2 段那个也摘掉，别把订阅漏给后面的段
        var keep = _killFx.progress.Challenge as TroopsKilledCount;
        if (keep != null) keep.OnReset();
        LiveOpsSignal.Raise(new TroopKilledSignal(true));
        Check(_killFx.Value == beforeA + 2, "4 收尾：两条都摘掉 ⇒ A 也不再动（订阅干净了）");
        Check(f2.Value == 1, "4 收尾：两条都摘掉 ⇒ B 也不动");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  5 · `EnergySpent`（四道闸 + 累加**金额**）
    //     判据：`decomp_full/EnergySpent__OnEnergySpent.c`（全文 33 行）
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section5_EnergySpent()
    {
        Debug.Log(P + "--- 5 `EnergySpent`（四道闸）---");

        // 5·a 两个配套件的真值表（照原版枚举值 / 机器码，⛔ 不按语义猜）
        Check((int)ChallengeComparison.EqualTo == 0, "5 枚举：`EqualTo == 0`（`dump.cs:59865` 一带）");
        Check((int)ChallengeComparison.HigherOrEqual == 10, "5 枚举：`HigherOrEqual == 10`（⛔ 不是 1）");
        Check((int)ChallengeComparison.LowerOrEqual == 20, "5 枚举：`LowerOrEqual == 20`（⛔ 不是 2）");
        Check((int)EnergyCostType.CardCost == 1 && (int)EnergyCostType.AbilityCost == 2, "5 枚举：`CardCost=1 / AbilityCost=2`");

        Check(ChallengeComparisonExtensions.Compare(ChallengeComparison.EqualTo, 5, 5), "5 Compare：EqualTo 5,5 ⇒ 真");
        Check(!ChallengeComparisonExtensions.Compare(ChallengeComparison.EqualTo, 5, 6), "5 Compare：EqualTo 5,6 ⇒ 假");
        Check(ChallengeComparisonExtensions.Compare(ChallengeComparison.HigherOrEqual, 7, 5), "5 Compare：HigherOrEqual 7,5 ⇒ 真");
        Check(!ChallengeComparisonExtensions.Compare(ChallengeComparison.HigherOrEqual, 4, 5), "5 Compare：HigherOrEqual 4,5 ⇒ 假");
        Check(ChallengeComparisonExtensions.Compare(ChallengeComparison.LowerOrEqual, 4, 5), "5 Compare：LowerOrEqual 4,5 ⇒ 真");
        Check(!ChallengeComparisonExtensions.Compare((ChallengeComparison)99, 5, 5), "5 Compare：未知比较符 ⇒ 假（原版兜底 false，⛔ 不抛、不当成 EqualTo）");

        // `ContainsFlag` 的真语义（VA `0x180C8A1F0` = `test edx,ecx; setne al` ⇒ `(a & b) != 0`）
        Check(EnergySpent.ContainsFlag(EnergyCostType.CardCost, EnergyCostType.CardCost), "5 ContainsFlag：Card⊆Card ⇒ 真");
        Check(!EnergySpent.ContainsFlag(EnergyCostType.CardCost, EnergyCostType.AbilityCost), "5 ContainsFlag：Ability⊄Card ⇒ 假");
        Check(EnergySpent.ContainsFlag(EnergyCostType.CardCost | EnergyCostType.AbilityCost, EnergyCostType.AbilityCost),
              "5 ContainsFlag：两类都算 ⊆ Ability ⇒ 真");
        Check(!EnergySpent.ContainsFlag((EnergyCostType)0, EnergyCostType.CardCost), "5 ContainsFlag：空掩码 ⊄ Card ⇒ 假");
        Check(!EnergySpent.ContainsFlag((EnergyCostType)0, (EnergyCostType)0),
              "5 ContainsFlag：空掩码 ⊄ 空标记 ⇒ **假**（这条专抓 `(a&b)==b` 那种写法：它会在这里返回真）");

        // 5·b 消费者本体
        var c = new EnergySpent();
        c.id = "b98a11-energy";
        c.rewards = new[] { new ChallengeMilestone { difficulty = 1, targetValue = 5 } };
        c.costCondition = new CostCondition { ComparisonMethod = ChallengeComparison.HigherOrEqual, Cost = 5 };
        c.costType = EnergyCostType.CardCost;
        var f = Build(c);
        if (f.progress == null) { Check(false, "5 前提：进度行取不到（本段无法判断）"); return; }

        Check(new EnergySpent().costType == EnergyCostType.CardCost, "5 默认值：`costType` 缺省 = `CardCost`（原版 `.ctor` 逐字）");
        Check(new EnergySpent().costCondition != null, "5 默认值：`costCondition` 缺省已 new（原版 `.ctor` 逐字）");
        Check(f.Value == 0, "5 起点：`currentValue == 0`");

        LiveOpsSignal.Raise(new EnergySpentSignal(false, 7, EnergyCostType.CardCost));
        Check(f.Value == 0, "5 闸①：`IsPlayer = false` ⇒ 不变（`.c:13`）");

        LiveOpsSignal.Raise(new EnergySpentSignal(true, 2, EnergyCostType.CardCost));
        Check(f.Value == 0, "5 闸②：`Compare(HigherOrEqual, 2, 5)` 为假 ⇒ 不变（`.c:16-18`）");

        LiveOpsSignal.Raise(new EnergySpentSignal(true, 7, EnergyCostType.AbilityCost));
        Check(f.Value == 0, "5 闸③：`costType` 只要 CardCost，来了 AbilityCost ⇒ 不变（`.c:20-23`）");

        LiveOpsSignal.Raise(new EnergySpentSignal(true, 7, EnergyCostType.CardCost));
        Check(f.Value == 7, "5 四闸全过：累加的是**金额 7**（不是次数 1 —— 若按次数算这里会是 1）");

        LiveOpsSignal.Raise(new EnergySpentSignal(true, 6, EnergyCostType.CardCost));
        Check(f.Value == 13, "5 再一笔 6 ⇒ 13（累加口径，⛔ 不是覆盖）");

        LiveOpsSignal.Raise(new EnergySpentSignal(true, 5, EnergyCostType.CardCost));
        Check(f.Value == 18, "5 `HigherOrEqual` 的边界值 5 也算（`5 >= 5`）");

        // 掩码两类的挑战：两类都该收
        var c2 = new EnergySpent();
        c2.id = "b98a11-energy-both";
        c2.rewards = new[] { new ChallengeMilestone { difficulty = 1, targetValue = 5 } };
        c2.costCondition = new CostCondition { ComparisonMethod = ChallengeComparison.HigherOrEqual, Cost = 1 };
        c2.costType = EnergyCostType.CardCost | EnergyCostType.AbilityCost;
        var f2 = Build(c2);
        Check(f2.progress != null, "5 前提：第二个 Fixture 的进度行取到了");
        if (f2.progress != null)
        {
            LiveOpsSignal.Raise(new EnergySpentSignal(true, 3, EnergyCostType.AbilityCost));
            Check(f2.Value == 3, "5 掩码两类：AbilityCost 也进（`ContainsFlag` 命中）");
            c2.OnReset();
        }

        // 空 `costCondition`：原版 NRE ⇒ 我们出声 + 不推进
        var c3 = new EnergySpent();
        c3.id = "b98a11-energy-null";
        c3.rewards = new[] { new ChallengeMilestone { difficulty = 1, targetValue = 5 } };
        c3.costCondition = null;
        var f3 = Build(c3);
        Check(f3.progress != null, "5 前提：第三个 Fixture 的进度行取到了");
        if (f3.progress != null)
        {
            LiveOpsSignal.Raise(new EnergySpentSignal(true, 9, EnergyCostType.CardCost));
            Check(f3.Value == 0, "5 空 `costCondition`：不推进（原版 NRE ⇒ 同样不计数），⛔ 不崩");
            c3.OnReset();
        }

        c.OnReset();
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  6 · A1 真缺口本体：`ApplyProgressPayload`（原版 `UpdateChallengeProgress` 的四跳）
    //     判据：`decomp_generic_trial/out/MissionEvent_object__object___UpdateChallengeProgress__102f660.c`
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void Section6_ApplyProgressPayload()
    {
        Debug.Log(P + "--- 6 `ApplyProgressPayload`（A1 真体）---");

        var c = new TroopsKilledCount();
        c.id = "b98a11-a1";
        c.rewards = new[] { new ChallengeMilestone { difficulty = 1, targetValue = 10 } };
        var f = Build(c);
        if (f.progress == null || f.ev == null) { Check(false, "6 前提：夹具没建好（本段无法判断）"); return; }

        // 6·a 正常一跳
        bool changed = f.ev.ApplyProgressPayload(new MissionChallengeUpdatePayload(c, p => { p.currentValue += 4; return true; }));
        Check(changed, "6 正常：`UpdateFunction` 返回真 ⇒ `ApplyProgressPayload` 返回真");
        Check(f.Value == 4, "6 正常：进度行**真的**变成 4（这一条就是「空实现」与「真体」的分水岭）");

        // 6·b `UpdateFunction` 返回假 ⇒ 不标记待上传
        bool changed2 = f.ev.ApplyProgressPayload(new MissionChallengeUpdatePayload(c, p => false));
        Check(!changed2, "6 返回假：推进函数说「没变」⇒ `ApplyProgressPayload` 返回假");
        Check(f.Value == 4, "6 返回假：进度不动");

        // 6·c payload 空 / Challenge 空
        Check(!f.ev.ApplyProgressPayload(null), "6 payload = null ⇒ 假（原版 NRE，我们出声）");
        Check(!f.ev.ApplyProgressPayload(new MissionChallengeUpdatePayload(null, p => true)), "6 `payload.Challenge = null` ⇒ 假（`.c:51-54` 静默 return）");

        // 6·d 教程门：前两段教程没完成 ⇒ 非 `DailyLoginChallenge` 一律不放行
        MissionChallenge.FirstTwoTutorialCompleted = () => false;
        bool gated = f.ev.ApplyProgressPayload(new MissionChallengeUpdatePayload(c, p => { p.currentValue += 100; return true; }));
        Check(!gated, "6 教程门：`FirstTwoTutorialCompleted = false` 且不是 `DailyLoginChallenge` ⇒ 不放行");
        Check(f.Value == 4, "6 教程门：进度一点没动（`.c:59-77`）");
        MissionChallenge.FirstTwoTutorialCompleted = () => true;

        // 6·e 已完成 ⇒ 告警早退（`IsComplete()` = 里程碑条数 ≤ 已领条数）
        f.progress.collectedRewards = 1;                 // 唯一一条里程碑「已领」⇒ `IsComplete()` 为真
        Check(f.progress.IsComplete(), "6 前提：把 `collectedRewards` 摆成 1 ⇒ `IsComplete()` 为真");
        bool done = f.ev.ApplyProgressPayload(new MissionChallengeUpdatePayload(c, p => { p.currentValue += 1; return true; }));
        Check(!done, "6 已完成：`IsComplete()` ⇒ 告警早退、返回假（`.c:86-98`）");
        Check(f.Value == 4, "6 已完成：进度不动");
        f.progress.collectedRewards = 0;

        // 6·f `resetProgress` 那格镜像本仓没有 `MainMenuMission` ⇒ 必须出声一次（⛔ 不许静默少一件事）
        f.progress.resetProgress = true;
        f.ev.ApplyProgressPayload(new MissionChallengeUpdatePayload(c, p => { p.currentValue += 1; return true; }));
        Check(f.Value == 5, "6 `resetProgress`：进度照样落地（那格镜像只影响主菜单弹窗）");
        f.progress.resetProgress = false;

        c.OnReset();
    }
}
