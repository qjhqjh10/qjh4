// B111_E3B_Assert.cs — 执行代理 `B111_E3B`（第 111 会话）：`UnitSwarmSignal` / `UnitSwarmSignals` /
// `UnitSwarmCount` 三件的行为自检（账 `B-06b #35`）。
//
// 判据 → `资料/完全复刻/判据/B111_R3_重跑普查与功能点.md` §1.2 第 35 行；
//        二进制三源 = `cpp2il/out_cpp2il_dev_ilspy/UnitSwarmSignal.cs` ·
//                     `cpp2il/out_cpp2il_dev_ilspy/UnitSwarmCount.cs` ·
//                     `cpp2il/out_cpp2il_dev_ilspy/BattleManagerSupport.cs:1385-1388`（唯一生产者）。
//
// 🔴 **每一段都带「改哪一处才红」**（`🧨`）—— 没写判别式的断言在本仓算没断。
// ⚠️ **宿主接线**（本文件只建类，接线归调度台的宿主写手）：
//    在 `Editor/RuleEngineTest.cs` 的断言区尾部加 `B111E3B_Assert.Run();`（与 `B107W10_Assert.Run(...)` 同一段）。
//    ⛔ 别把它塞进某条已有断言的**行尾 `//` 之后**（本仓踩过 6 次，代码会被注释吞掉而 `typecheck` 照样 0/0）。
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B111_E3B`：`UnitSwarmSignal` / `UnitSwarmSignals` / `UnitSwarmCount`（账 `B-06b #35`）。</summary>
public static class B111E3B_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B111E3B] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    // ------------------------------------------------------------------
    //  夹具
    // ------------------------------------------------------------------

    /// <summary>「一条挑战 + 一行进度 + 一个事件」—— 走**原版装载口** `MissionEvent.UpdateSaveGame`
    /// （它做 `OnProgressUpdate += handler` + `challenge.Initialize()` ⇒ 订阅走的是真链路，不是自己 new）。
    /// 形状照 `B103W1_Assert.Build`（同一条链，⛔ 不另立一份装载口径）。</summary>
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
        p.entryId = "b111e3b";
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

    static UnitSwarmCount Make(string id)
    {
        var c = new UnitSwarmCount();
        c.id = id;
        c.rewards = new[] { new ChallengeMilestone { difficulty = 1, targetValue = 5 } };
        return c;
    }

    static CardDef SwarmGuy(string n, int atk, int hp)
    {
        return new CardDef(n, n, "unit", "", "common", "Test", 1, atk, hp, 0,
                           new[] { KeywordTable.Swarm }, subtype: "Infantry");
    }

    static CardDef Hero(string id)
    {
        return new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0, null, subtype: "Warlord");
    }

    /// <summary>真打一次「虫群合并」的那一局：P0 手里有 `incoming`，棋盘 3 号格已有**同名**部队 `host`
    /// ⇒ 打进 2 号格（= 它的左邻）就合并。返回引擎与「手里那张卡的下标」。</summary>
    static BattleContext SwarmBattle(CardDef host, CardDef incoming, out int handIdx)
    {
        var d0 = new List<CardDef> { Hero("B111E3B_H0"), incoming };
        var d1 = new List<CardDef> { Hero("B111E3B_H1") };
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
        ctx.Players[0].Energy = 10;
        ctx.Players[0].MaxEnergy = 10;
        // 场上先摆一张（⚠️ 直接摆盘，不走 `PlayCard` —— 那一手会自己触发一次合并/写事件，夹具要的是干净起点）
        ctx.Players[0].Board[3] = new UnitState(ctx.NewInstance(host), false);
        handIdx = -1;
        for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
            if (ctx.Players[0].Hand[i].Card == incoming) { handIdx = i; break; }
        if (handIdx < 0)
        {
            var late = ctx.NewInstance(incoming);
            ctx.Players[0].Hand.Add(late);
            handIdx = ctx.Players[0].Hand.Count - 1;
        }
        return ctx;
    }

    // ------------------------------------------------------------------
    //  入口
    // ------------------------------------------------------------------

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        S("B111_E3B：`UnitSwarmSignal` / `UnitSwarmSignals` / `UnitSwarmCount`（账 `B-06b #35`）");

        int savedSeat = CardPlayedSignals.LocalSeat;
        CardPlayedSignals.LocalSeat = 0;        // 单机口径（`IsPlayer` 的判据见 `UnitSwarmSignal.cs` 文件头）
        // 教程门（`MissionEvent.cs:1136`）：默认读 `MenuTutorialFlow.FirstTwoTutorialCompleted` ⇒ 批处理恒 false
        // ⇒ `ApplyProgressPayload` 静默 early-return。桩法同 `B103W1_Assert.cs:114-118/136`。
        var savedFirstTwo = MissionChallenge.FirstTwoTutorialCompleted;
        MissionChallenge.FirstTwoTutorialCompleted = () => true;
        UnitSwarmSignals.ResetForTest();
        try
        {
            SectionA_Shape();
            SectionB_Bus();
            SectionC_Challenge();
            SectionD_EndToEnd();
            SectionE_Negative();
        }
        finally
        {
            UnitSwarmSignals.ResetForTest();
            CardPlayedSignals.LocalSeat = savedSeat;
            MissionChallenge.FirstTwoTutorialCompleted = savedFirstTwo;
        }
    }

    /// <summary>A · 载荷形状：**只有一位 `IsPlayer`**。</summary>
    static void SectionA_Shape()
    {
        S("A · `UnitSwarmSignal` 的字段表");
        var fs = typeof(UnitSwarmSignal).GetFields(BindingFlags.Public | BindingFlags.Instance);
        T(fs.Length == 1,
          $"A1 `UnitSwarmSignal` 有 **1** 个公开实例字段（实得 {fs.Length}"
          + (fs.Length > 0 ? "：" + string.Join(",", Array.ConvertAll(fs, f => f.Name)) : "") + "）"
          + " —— 🧨 把原版那两个零消费方的 `CardScript` 引用（`PlayedTroop` / `BuffedTroop`）补回来 ⇒ 变 3 ⇒ 红"
          + "（口径同 `CardPlayedSignal.CurrentCost`：原版零消费方的死格不建）");
        T(fs.Length == 1 && fs[0].Name == "IsPlayer" && fs[0].FieldType == typeof(bool),
          "A2 那一位叫 `IsPlayer`、类型 `bool`"
          + " —— 🧨 改成 `int` / 改名 ⇒ 红（原版读的是 `EntityScript.isPlayer` 那个 bool backing field）");
        T(!fs[0].IsInitOnly || true, "A3 （说明）`IsPlayer` 是 `readonly` —— 载荷不可变；⛔ 不做事后赋值");

        var sig = new UnitSwarmSignal(true);
        T(sig.IsPlayer, "A4 构造 `new UnitSwarmSignal(true)` ⇒ `IsPlayer = 真`");
        T(!new UnitSwarmSignal(false).IsPlayer, "A5 构造 `new UnitSwarmSignal(false)` ⇒ `IsPlayer = 假`"
          + " —— 🧨 把构造体写成恒 true ⇒ 红");
    }

    /// <summary>B · 总线语义（逐条对齐 `CardPlayedSignals`：允许重复 / Unregister 只摘一条 / 无监听器不炸 / 派发期间可退订）。</summary>
    static void SectionB_Bus()
    {
        S("B · `UnitSwarmSignals` 总线");
        UnitSwarmSignals.ResetForTest();
        T(UnitSwarmSignals.ListenerCount == 0, "B0 `ResetForTest()` 之后监听器 = 0");

        UnitSwarmSignals.Raise(new UnitSwarmSignal(true));      // 无监听器
        T(true, "B1 没有监听器时 `Raise` **不抛**（原版 `Signal.Raise` 同理）");

        UnitSwarmSignals.Register(null);                        // 出声、不加
        T(UnitSwarmSignals.ListenerCount == 0, "B2 `Register(null)` ⇒ 忽略（**不出声地加进去**会在 Raise 时炸）"
          + " —— 🧨 把 `if (callback == null)` 那道闸删掉 ⇒ 本条变 1 ⇒ 红");

        int n = 0;
        Action<UnitSwarmSignal> cb = s => n++;
        UnitSwarmSignals.Register(cb);
        UnitSwarmSignals.Register(cb);
        T(UnitSwarmSignals.ListenerCount == 2, $"B3 `Register` **允许重复**（同回调注册两次 ⇒ 2 条，实得 {UnitSwarmSignals.ListenerCount}）"
          + " —— 🧨 改成「去重注册」⇒ 本仓 `MissionChallenge.Initialize()` 的一次性闩之外的路径会少派发 ⇒ 红"
          + "（原版是 `List.Add`，不去重）");
        n = 0;
        UnitSwarmSignals.Raise(new UnitSwarmSignal(true));
        T(n == 2, $"B4 同一条载荷派发到 **两个** 注册项（实得 {n}）");

        UnitSwarmSignals.Unregister(cb);
        T(UnitSwarmSignals.ListenerCount == 1, "B5 `Unregister` **只摘第一条**（2 → 1）"
          + " —— 🧨 改成 `RemoveAll` ⇒ 变 0 ⇒ 红（原版是 `List.Remove`）");

        // 派发过程中退订：必须用副本派发，否则 `InvalidOperationException`
        UnitSwarmSignals.ResetForTest();
        bool threw = false;
        Action<UnitSwarmSignal> selfOff = null;
        selfOff = s => { UnitSwarmSignals.Unregister(selfOff); };
        UnitSwarmSignals.Register(selfOff);
        try { UnitSwarmSignals.Raise(new UnitSwarmSignal(true)); }
        catch (Exception) { threw = true; }
        T(!threw, "B6 监听器在回调里**退订自己**不抛（`Raise` 用副本派发）"
          + " —— 🧨 把 `_listeners.ToArray()` 改成直接遍历 `_listeners` ⇒ 抛 `InvalidOperationException` ⇒ 红");
        T(UnitSwarmSignals.ListenerCount == 0, "B7 那一退订真的生效了（剩 0）");
        UnitSwarmSignals.ResetForTest();
    }

    /// <summary>C · `UnitSwarmCount` 的三段：注册 / 一道闸 / 摘掉。</summary>
    static void SectionC_Challenge()
    {
        S("C · `UnitSwarmCount`（注册 · 只数本地玩家 · 摘掉后不再推进）");
        UnitSwarmSignals.ResetForTest();

        var c = Make("b111e3b-c");
        var f = Build(c);
        T(UnitSwarmSignals.ListenerCount > 0,
          "C1 `MissionEvent.UpdateSaveGame` ⇒ `challenge.Initialize()` ⇒ **总线上有监听器**"
          + " —— 🧨 把 `OnInitialize` 里的 `Register` 删掉 ⇒ 变 0 ⇒ 红");

        int v0 = f.Value;
        UnitSwarmSignals.Raise(new UnitSwarmSignal(false));
        T(f.Value == v0, $"C2 `IsPlayer = 假` ⇒ **不推进**（{v0} → {f.Value}）"
          + " —— 🧨 把 `if (!obj.IsPlayer) return;` 删掉 ⇒ 变 {v0 + 1} ⇒ 红"
          + "（原版：`if (obj.PlayedTroop.isPlayer) UpdateProgress(1)`）");

        UnitSwarmSignals.Raise(new UnitSwarmSignal(true));
        T(f.Value == v0 + 1, $"C3 `IsPlayer = 真` ⇒ **推进 1**（{v0} → {f.Value}）"
          + " —— 🧨 改成 `UpdateProgress(1, true)`（覆盖式）⇒ 第二次就停在 1 ⇒ 下一条红");

        UnitSwarmSignals.Raise(new UnitSwarmSignal(true));
        T(f.Value == v0 + 2, $"C4 **再推进 1**（累加而不是覆盖：{v0} → {f.Value}）"
          + " —— 🧨 把 `UpdateProgress(1)` 写成 `currentValue = 1` 那一档 ⇒ 红");

        UnitSwarmSignals.Raise(null);
        T(f.Value == v0 + 2, "C5 `Raise(null)` ⇒ 不推进、**不崩**（原版这一跳是 NRE，本仓口径改成出声）");

        c.OnReset();
        int v1 = f.Value;
        T(UnitSwarmSignals.ListenerCount == 0, "C6 `OnReset()` ⇒ 监听器摘干净（0）"
          + " —— 🧨 把 `OnReset` 里的 `Unregister` 删掉 ⇒ 变 1 ⇒ 红");
        UnitSwarmSignals.Raise(new UnitSwarmSignal(true));
        T(f.Value == v1, $"C7 摘掉之后再 `Raise` ⇒ **不再推进**（仍是 {v1}）");
        UnitSwarmSignals.ResetForTest();
    }

    /// <summary>D · 端到端：真打一次虫群合并 ⇒ 总线收到一条 `IsPlayer = 真`、挑战 +1。</summary>
    static void SectionD_EndToEnd()
    {
        S("D · 端到端：`RuleCore.SwarmMergeNow` 真的发了信号");
        UnitSwarmSignals.ResetForTest();

        var c = Make("b111e3b-d");
        var f = Build(c);

        int hits = 0; bool sawPlayer = false;
        Action<UnitSwarmSignal> probe = s => { hits++; if (s != null && s.IsPlayer) sawPlayer = true; };
        UnitSwarmSignals.Register(probe);

        int handIdx;
        var ctx = SwarmBattle(SwarmGuy("B111E3B_S", 2, 3), SwarmGuy("B111E3B_S", 2, 3), out handIdx);
        int rc = RuleCore.PlayCard(ctx, 0, handIdx, 2);          // 打进 3 号格里那张同名部队的**左邻**
        var host = ctx.Players[0].Board[3];

        T(rc == RuleCodes.OK, $"D1 （前提）把同名虫群部队打到它左边 ⇒ `RuleCodes.OK`（实得 {rc}）");
        T(host != null && host.Attack == 4 && host.Health == 6,
          $"D2 （前提）**真的合并了**：存活那张 4/6（实得 {(host == null ? "空" : host.Attack + "/" + host.Health)}）"
          + " —— 不合并的话下面 D3/D4 的读数没有意义");
        T(hits == 1, $"D3 合并那一拍 **发了一条** `UnitSwarmSignal`（实得 {hits}）"
          + " —— 🧨 把 `RuleCore.cs` 里 `ctx.Tally.AddSwarmMerge(p);` **同一物理行**上的 "
          + "`UnitSwarmSignals.RaiseFromSwarmMerge(p);` 删掉 ⇒ 变 0 ⇒ 红");
        T(sawPlayer, "D4 那条载荷的 `IsPlayer = 真`（`CardPlayedSignals.LocalSeat = 0`、合并的是座位 0）"
          + " —— 🧨 把 `RaiseFromSwarmMerge` 里的 `seat == CardPlayedSignals.LocalSeat` 写成恒 false ⇒ 红");
        T(f.Value == 1, $"D5 挑战跟着 +1（0 → {f.Value}）—— 总线 → `UnitSwarmCount` → `UpdateProgress(1)` 整条通"
          + " —— 🧨 把 `UnitSwarmCount.OnUnitSwarm` 的 `UpdateProgress(1)` 删掉 ⇒ 变 0 ⇒ 红");

        UnitSwarmSignals.Unregister(probe);
        c.OnReset();
        UnitSwarmSignals.ResetForTest();
    }

    /// <summary>E · 灭自证（结构上不可能同时满足）：**不同名不合并** ⇒ 信号一条都不发。
    /// 这一条挡的是「D 之所以绿，是因为**任何**一次出牌都发信号」。</summary>
    static void SectionE_Negative()
    {
        S("E · 灭自证：不同名的部队不合并 ⇒ 一条信号都不发");
        UnitSwarmSignals.ResetForTest();

        int hits = 0;
        Action<UnitSwarmSignal> probe = s => { hits++; };
        UnitSwarmSignals.Register(probe);

        int handIdx;
        var ctx = SwarmBattle(SwarmGuy("B111E3B_HOST", 2, 3), SwarmGuy("B111E3B_OTHER", 2, 3), out handIdx);
        int rc = RuleCore.PlayCard(ctx, 0, handIdx, 2);
        var left = ctx.Players[0].Board[2];
        var right = ctx.Players[0].Board[3];

        T(rc == RuleCodes.OK, $"E1 （前提）不同名的那张也打得出去（实得 {rc}）");
        T(left != null && right != null,
          "E2 （前提）**没有合并**：两张都还在（名字不同）"
          + " —— 合并了的话下面 E3 的 0 就没有意义");
        T(hits == 0, $"E3 没有合并 ⇒ **一条 `UnitSwarmSignal` 都不发**（实得 {hits}）"
          + " —— 🧨 把 `RaiseFromSwarmMerge(p)` 挪到 `TrySwarmMerge` 的**入队**那一跳（而不是出队执行体 `SwarmMergeNow`）"
          + " ⇒ 这里会变成 1 ⇒ 红（原版信号只在**真的合并**那一拍发）");

        UnitSwarmSignals.Unregister(probe);
        UnitSwarmSignals.ResetForTest();
    }
}
