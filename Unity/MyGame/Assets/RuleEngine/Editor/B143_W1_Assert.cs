// B143_W1_Assert.cs — 批次 **B143** · 执行写手 `W1`（引擎域五条里**可离线断言**的三条：
//   `B141-SAN04-TACTIC` / `B141-CHOOSECARD-ROW` / `B141-ABILITY-HAND-COVERAGE`）；
//   另两条（`B142-DEADCEM-THISTURN` / `B142-CEMLOG-NAME`）的判据已由 `B142_E56_Assert` B 组与卡池读数覆盖，本件不重复。
//
// ============================ 它盯的是什么 ============================
//   A 组（SAN04-TACTIC）：战术卡的付费组**切片 + 逐组目标槽**。
//     原版 `RawCardScript__TriggerAbilities.c:59/207/231` 逐条 ability 各跑一次，
//     `AbilityLogic__PlayAbility.c:262` 每条用**自己的** `targetCriteria` 算目标 ⇒ 各组的目标**不共享**。
//     我们没把正文切开（那会清 6 个代词/计数窗口），只把「哪条 op 属于哪一组」交给结算循环，
//     逐 op 把目标记进**那一组**的 scratch ⇒ 本组断的就是这个映射。
//   B 组（CHOOSECARD-ROW）：`ResolveCemeteryTargets` 读不到 `(ab,lg)` 时的**唯一性回落**。
//     回落只在「全卡恰好一条 `targetsAffected = 60`」时才用，多于一条 ⇒ 必须回 null（⛔ 不猜）。
//   C/D 组（ABILITY-HAND-COVERAGE）：`UnitAttacked` 的 300 手牌跳真的接上了（走到了手牌执行口但被区域闸拒）；
//     `WhileInPlay` 手牌那趟**只发 20、⛔ 不碰 240**（调度台 2026-11-05 裁定：240 由 `CostOf` 族承载，再接 = 双计）。
//   E 组（DOGIVE-PRODUCER）：`whileinplay` / `whileinplayvariable` 两条**关键词→trigger 名**映射存在，
//     且**没有任何一张** 20/240 卡带 `_triggerOps` 供给（裁定：不建第二条供给，建了 = 与 Aura 层双倍加成）。
//   F 组（HAND-CTX-GATE 的「过宽」半）：**桥那一路**（`TurnStart` 30 / `UnitDamaged` 60）也必须吃
//     `abilityContext == Hand` 那道严格闸 —— `context = Play(0)` 的卡在手牌里**一条都不许被叫醒**，
//     同时用 attempts 计数证明「真的扫到了」，挡「没扫 = 空转绿」。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1/A5：`SplitPaidGroups` 的组边界若改成「不按 Cost 切」⇒ 组数不再是 3 / 组首不再是 4<7 ⇒ 红。
//   · A6/A7：`GroupScratchOf` 若退化成「永远回 fallback」（= 改前的整条正文一条）⇒ s1/s2 都等于 fallback ⇒ 红。
//   · A8（判别式）：不传分组表时必须**回 fallback** —— 这一条挡「为了过 A6 把回落逻辑整个删掉」（那会改旧调用点行为）。
//   · B4（判别式）：造一张**两条** `ta=60` 的卡 ⇒ 必须回 null。挡「无条件取第一条」（那会按错行筛）。
//   · C2（判别式）：同一张卡直调 `FireHandAbilityTrigger(…"Mob")` ⇒ attempts **+1** ⇒ 证明 C1 的 0 不是空转。
//   · D3（判别式）：手牌放一张带 trigger **20** 的卡 ⇒ 本趟 attempts **+1** ⇒ 证明整趟真的在跑（挡「pass 根本没执行」的假绿）。
//   · D2：直调 240 ⇒ attempts +1 ⇒ 证明 D1 的 0 不是因为夹具不活。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行追加**（落在该行第一个 `//` 之前）：
//     `Guard("B143W1_Assert", B143_W1_Assert.Run); _pass += B143_W1_Assert.Pass; _fail += B143_W1_Assert.Fail;`
//   （接线由调度台统一做；本写手不改宿主文件。）
//
// ⚠️ 本文件**不建 GameObject、不建资产、不写盘**；只用 `RuleCore.NewBattle` 的内存局面。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B143-W1 的断言（独立类：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B143_W1_Assert
{
    static int _pass, _fail;

    /// <summary>离线取证用的卡池注入窄口（同 `B141_W77_Assert.PoolOverride`；Unity 侧恒 `null` ⇒ 走真装载链）。
    /// ⛔ 生产侧别写它。</summary>
    public static List<CardDef> PoolOverride;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B143-W1 A：`B141-SAN04-TACTIC` —— 战术卡按付费组切 + 逐组目标槽");
        GroupA_TacticPaidGroups();
        Section("🆕 B143-W1 B：`B141-CHOOSECARD-ROW` —— 墓地行的唯一性回落");
        GroupB_UniqueCemeteryRow();
        Section("🆕 B143-W1 C/D：`B141-ABILITY-HAND-COVERAGE` —— 300 手牌跳接上了 / WhileInPlay 只发 20");
        GroupC_HandHops();
        Section("🆕 B143-W1 E：`B141-DOGIVE-PRODUCER` —— 两条关键词→trigger 映射存在，且**没有**第二条 op 供给");
        GroupE_WhileInPlayMapping();
        Section("🆕 B143-W1 F：`B142-HAND-CTX-GATE`「过宽」—— 桥那一路（30 / 60）也吃同一道 `abilityContext == Hand` 闸");
        GroupF_HandCtxGateCoverage();
        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static CardDef Unit(string id) => new CardDef(id, id, "unit", "", null, "Test", 1, 1, 5, 0,
                                                 (IEnumerable<string>)null, subtype: "Infantry");

    /// <summary>合成卡：只为把 `abilityTriggers` / `abilityContext` 两列钉成想要的值（正文留空 ⇒ 走行级派发那条路）。</summary>
    static CardDef Synth(string id, IEnumerable<int> triggers, IEnumerable<CardDef.AbilityContextSpec> ctxs)
    {
        return new CardDef(id, id, "tactic", "", null, "Test", 1, 0, 0, 0,
                           abilityTriggers: triggers, abilityTriggersVersioned: true,
                           abilityContext: ctxs, abilityContextsVersioned: true);
    }

    static CardDef.AbilityContextSpec Ctx(int trigger, string name, int context)
    {
        return new CardDef.AbilityContextSpec { Trigger = trigger, Name = name, Context = context };
    }

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B143W1Warlord0") };
        var d1 = new List<CardDef> { Hero("B143W1Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static int FirstIndexOfGroup(int[] map, int group)
    {
        if (map == null) return -1;
        for (int i = 0; i < map.Length; i++) if (map[i] == group) return i;
        return -1;
    }

    // ==================================================================
    //  A 组：战术卡付费组切片 + 逐组目标槽（`SOR56 Beacon of Faith`，池内唯一的双信仰组战术卡）
    // ==================================================================
    static void GroupA_TacticPaidGroups()
    {
        var pool = PoolOverride ?? CardDatabase.Load();
        CheckTrue(pool != null && pool.Count > 0,
                  "A0 前提：卡池可读（`CardDatabase.Load()` / `PoolOverride`；离线探针必须注入 PoolOverride）");
        if (pool == null) return;

        var card = CardDatabase.FindById(pool, "SOR56");
        CheckTrue(card != null, "A0b 前提：`SOR56 Beacon of Faith` 在池内（池内**唯一**的双信仰组战术卡）");
        if (card == null) return;

        var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
        CheckTrue(ops != null && ops.Count >= 3, "A0c 前提：正文解析出 ≥3 条 op（实际 = " + (ops != null ? ops.Count : 0) + "）");
        if (ops == null || ops.Count < 3) return;

        int[] map;
        var groups = RuleCore.SplitPaidGroupsWithMapForTest(ops, out map);

        Check(groups.Count, 3, "A1 ★ 按付费组切出 **3 组**（正文 + `4 ☀` + `7 ☀`）");
        Check(map.Length, ops.Count, "A2 组映射长度 = op 条数（每条 op 都必须落在某一组）");

        bool mono = true;
        for (int i = 1; i < map.Length; i++) if (map[i] < map[i - 1]) mono = false;
        CheckTrue(mono, "A3 组映射**单调不减**（同一组的 op 连在一起，⛔ 不是散着分）");

        if (groups.Count == 3)
        {
            CheckTrue(groups[0][0] != null && groups[0][0].Cost <= 0, "A4 第 0 组 = 正文（组首不是付钱那条）");
            CheckTrue(groups[1][0] != null && groups[2][0] != null
                      && groups[1][0].Cost > 0 && groups[2][0].Cost > 0
                      && groups[1][0].Cost < groups[2][0].Cost,
                      "A5 ★ 后两组的组首各是**一条付费前缀**，门槛递增（`4 ☀` < `7 ☀`）");
        }

        // ---- 逐组目标槽（本件真正的被测物）----
        var fallback = new BattleEvent();
        var scratches = new List<BattleEvent> { new BattleEvent(), new BattleEvent(), new BattleEvent() };
        int i1 = FirstIndexOfGroup(map, 1), i2 = FirstIndexOfGroup(map, 2);
        CheckTrue(i1 >= 0 && i2 >= 0, "A6a 前提：第 1 / 第 2 组都非空");
        if (i1 < 0 || i2 < 0) return;

        var s1 = RuleCore.GroupScratchOfForTest(ops, i1, scratches, map, fallback);
        var s2 = RuleCore.GroupScratchOfForTest(ops, i2, scratches, map, fallback);

        CheckTrue(s1 != null && s2 != null && !ReferenceEquals(s1, s2),
                  "A6 ★★ 两组各拿**各自**的 scratch（不是同一条 —— 改前两条都等于整条正文那一条）");
        CheckTrue(!ReferenceEquals(s1, fallback) && !ReferenceEquals(s2, fallback),
                  "A7 ★ 有分组表时**不**回落到「整条正文那一条」");
        CheckTrue(ReferenceEquals(s1, scratches[1]) && ReferenceEquals(s2, scratches[2]),
                  "A7b 且落的正是**它自己那一组**的下标（1 / 2，⛔ 不是错位）");

        // ---- 判别式：没有分组表时必须原样回落（= 全部既有调用点的行为）----
        var legacy = RuleCore.GroupScratchOfForTest(ops, i1, null, null, fallback);
        CheckTrue(ReferenceEquals(legacy, fallback),
                  "A8 ★★ 判别式：不传分组表（旧调用点形状）⇒ **回 fallback**（挡「为了过 A6 把回落删掉」）");
    }

    // ==================================================================
    //  B 组：`ta = 60` 那一行的唯一性回落
    // ==================================================================
    static void GroupB_UniqueCemeteryRow()
    {
        var pool = PoolOverride ?? CardDatabase.Load();
        if (pool == null) { CheckTrue(false, "B0 前提：卡池可读"); return; }

        var ec39 = CardDatabase.FindById(pool, "EC39");
        CheckTrue(ec39 != null, "B0b 前提：`EC39 Eternal Servitude` 在池内（5 张 `choose … died …` 之一）");
        if (ec39 == null) return;

        var set = RuleCore.UniqueCemeteryCriteriaRowForTest(ec39);
        CheckTrue(set != null, "B1 ★ 回落命中：EC39 的 `abilityTargetCriteria` 里有唯一那条 `ta = 60` 行");
        if (set != null)
        {
            Check(set.TargetsAffected, 60, "B2 那一行确实是 `targetsAffected = 60`（inCemeteryFiltered）");
            Check(set.IncludeSpells, 0, "B3 且 `includeSpells = 0` —— 「按目标短语推」那一档会给 `true`（候选被放宽）");
        }

        // ---- 判别式：两条 `ta = 60` 行 ⇒ 必须回 null（⛔ 不猜）----
        var amb = new CardDef(
            "B143W1_AMB", "B143W1_AMB", "tactic", "", null, "Test", 1, 0, 0, 0,
            abilityTargetCriteria: new[]
            {
                new CardDef.TargetCriteriaRowSpec { Ability = 0, Logic = 0, Criteria = new CardDef.CountSetSpec { TargetsAffected = 60, IncludeMinions = 1, IncludeSpells = 0, IncludeWhispers = 0, FriendlyUnits = 1, EnemyUnits = 0 } },
                new CardDef.TargetCriteriaRowSpec { Ability = 1, Logic = 0, Criteria = new CardDef.CountSetSpec { TargetsAffected = 60, IncludeMinions = 1, IncludeSpells = 0, IncludeWhispers = 0, FriendlyUnits = 0, EnemyUnits = 1 } },
            },
            abilityTargetCriteriaVersioned: true);
        CheckTrue(RuleCore.UniqueCemeteryCriteriaRowForTest(amb) == null,
                  "B4 ★★ 判别式：**两条** `ta = 60` ⇒ 回 null（挡「无条件取第一条」——那会按错行筛）");

        // ---- 0 条同样回 null ----
        var none = new CardDef("B143W1_NONE", "B143W1_NONE", "tactic", "", null, "Test", 1, 0, 0, 0,
                               abilityTargetCriteriaVersioned: true);
        CheckTrue(RuleCore.UniqueCemeteryCriteriaRowForTest(none) == null, "B5 0 条 ⇒ 同样回 null（不猜）");
    }

    // ==================================================================
    //  C/D 组：手牌两跳
    // ==================================================================
    static void GroupC_HandHops()
    {
        // ---------- C：`UnitAttacked` 的 300 手牌跳 ----------
        RuleCore.ResetHandAbilityCountersForTest();
        var ctx = NewCtx();
        // `abilityContext = Play(0)`：原版 `IsRightContext` 对手牌不成立 ⇒ 必须被拒（⛔ 不是「我们没做这一跳」）
        ctx.Players[0].Hand.Add(ctx.NewInstance(
            Synth("B143W1_C300", new[] { 300 }, new[] { Ctx(300, "Mob", 0) })));
        var attacker = Place(ctx, 0, 4, Unit("B143W1_Attacker"));
        var attacked = Place(ctx, 1, 4, Unit("B143W1_Attacked"));

        int a0 = RuleCore.HandAbilityTriggerAttemptsForTest;
        int fired = RuleCore.BroadcastUnitAttackedHandHops(ctx, attacked, attacker, 1);   // attackType = 1 (Melee) ⇒ 300
        int dAtt = RuleCore.HandAbilityTriggerAttemptsForTest - a0;
        int dQ = RuleCore.HandAbilityTriggerQueuedForTest;

        Check(fired, 0, "C1 ★ 300 手牌跳：`context = Play` 的卡被区域闸拒 ⇒ **0 条**（与原版同）");
        Check(dAtt, 1, "C2 ★★ 判别式：但**确实走到了**手牌执行口（attempts +1）—— 证明这一跳真的接上了，不是空转");
        Check(dQ, 0, "C3 没有入队（闸拒），⛔ 别把「接了」当成「会执行」");

        // ---------- D：`WhileInPlay` 手牌那趟只发 20 ----------
        RuleCore.ResetHandAbilityCountersForTest();
        var ctx2 = NewCtx();
        // 一张带 240（且 `context = Hand`，正是池内那 4 张「便宜 N 费」卡的形状）
        ctx2.Players[0].Hand.Add(ctx2.NewInstance(
            Synth("B143W1_C240", new[] { 240 }, new[] { Ctx(240, "WhileInPlayVariable", 10) })));
        // 另一张带 20（`context` 缺行 ⇒ 未知 ⇒ 闸拒，但 attempts 会 +1，用来证明整趟真的在跑）
        ctx2.Players[0].Hand.Add(ctx2.NewInstance(
            Synth("B143W1_C20", new[] { 20 }, null)));

        int b0 = RuleCore.HandAbilityTriggerAttemptsForTest;
        RuleCore.EmitWhileInPlayHandPassForTest(ctx2);
        int dAll = RuleCore.HandAbilityTriggerAttemptsForTest - b0;

        Check(dAll, 1, "D3 ★★ 判别式：本趟**真的跑了** —— 全手牌只走到 **1** 次手牌执行口（= 只有那张 trigger 20 的卡；240 那条**一次都没被碰**）");
        CheckTrue(RuleCore.HandAbilityTriggerQueuedForTest == 0, "D4 两张都没入队（20 那张 context 未知、240 那张本趟不派）");

        // 正面判别式：同一张 240 卡**直接派** ⇒ attempts +1（证明 D3 里的 1 不是「240 恰好不计」）
        var inst240 = ctx2.Players[0].Hand[0];
        int b1 = RuleCore.HandAbilityTriggerAttemptsForTest;
        RuleCore.FireHandAbilityTrigger(ctx2, 0, inst240, "WhileInPlayVariable", by: "b143w1:test");
        int d240 = RuleCore.HandAbilityTriggerAttemptsForTest - b1;
        Check(d240, 1, "D2 ★ 判别式：直调 `WhileInPlayVariable` ⇒ attempts +1 ⇒ 证明 D3 的计数对 240 同样敏感（夹具是活的）");
    }

    // ==================================================================
    //  E 组：`B141-DOGIVE-PRODUCER` —— 两条映射（⛔ 不建第二条 op 供给）
    //  判据：`判据/B143_奖励背包与待裁四项.md` §裁定 2 —— 池内 49 卡 / 53 行 logic 里 **45 行是纯 `buff`**，
    //  再造一份 `_triggerOps["whileinplay"]` 会让 **38 段双倍加成且零报错**；原版也没有「关键词→op 表」，
    //  我们的等价物 = Aura 层。⇒ 只补「关键词 → trigger 名」这一跳（零行为变化）。
    // ==================================================================
    static void GroupE_WhileInPlayMapping()
    {
        Check(RuleCore.TriggerNameOfKeyword("whileinplay"), "WhileInPlay",
              "E1 ★ `whileinplay` → `WhileInPlay`(20)（判据 `CardDef.cs:1918` 的名字表）");
        Check(RuleCore.TriggerNameOfKeyword("whileinplayvariable"), "WhileInPlayVariable",
              "E2 ★ `whileinplayvariable` → `WhileInPlayVariable`(240)（判据 `CardDef.cs:1927`）");
        CheckTrue(RuleCore.TriggerNameOfKeyword("b143w1_no_such_kw") == null,
                  "E3 反例：认不出的关键词仍回 `null`（⛔ 没把 `default` 支改成「什么都认」）");

        var pool = PoolOverride ?? CardDatabase.Load();
        if (pool == null) { CheckTrue(false, "E4 前提：卡池可读"); return; }

        int n = 0, bad = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            if (c.AbilityTriggerPresence(20) != 1 && c.AbilityTriggerPresence(240) != 1) continue;
            n++;
            if (c.TriggerOps("whileinplay") != null) bad++;          // ⛔ 不许有第二条供给
            if (c.TriggerOps("whileinplayvariable") != null) bad++;
        }
        CheckTrue(n > 0, "E4a 前提：池内存在带 20/240 ability 的卡（实际 = " + n + " 张）");
        Check(bad, 0,
              "E4 ★★ 反证：**没有一张** 20/240 卡带 `_triggerOps[\"whileinplay\" / \"whileinplayvariable\"]` 供给"
            + " —— 钉住「我们**没有**建第二条供给」（建了 = 与 Aura 层双倍加成：53 行里 45 行是纯 `buff`）");
    }

    // ==================================================================
    //  F 组：`B142-HAND-CTX-GATE` 的「过宽」半 —— **桥那一路**（`TurnStart` 30 / `UnitDamaged` 60）
    //  背景：`B142_E36` 只把闸加进了**两条文本监听路**（`BroadcastHandTrapWhen` / `BroadcastHandWhen`）的循环里；
    //  另有一族**专用桥**（`BroadcastTurnStartHandHops` / `BroadcastUnitDamagedHandHops` / …）直接逐张调
    //  `FireHandAbilityTrigger` —— 它们靠的是那个方法里的**严格闸**（`:19047-19052`：`actx != Hand ⇒ return false`，
    //  与文本路的「`-1` 也放行」**口径不同**）。本组把这条覆盖钉住：一旦有人把那道严格闸放宽（改成 `!= Hand && != -1`），
    //  `context = Play(0)` 的卡就会在手牌里被叫醒 ⇒ F1/F2 立刻红。
    // ==================================================================
    static void GroupF_HandCtxGateCoverage()
    {
        RuleCore.ResetHandAbilityCountersForTest();
        var ctx = NewCtx();
        ctx.Players[0].Hand.Add(ctx.NewInstance(
            Synth("B143W1_F30", new[] { 30 }, new[] { Ctx(30, "TurnStart", 0) })));
        int a0 = RuleCore.HandAbilityTriggerAttemptsForTest;
        int n30 = RuleCore.BroadcastTurnStartHandHops(ctx);
        Check(n30, 0, "F1 ★ `BroadcastTurnStartHandHops`：`context = Play(0)` 的卡被判拒 ⇒ **0 条**（= 改前「过宽」那一类不许回来）");
        Check(RuleCore.HandAbilityTriggerAttemptsForTest - a0, 1,
              "F1b ★★ 判别式：attempts **+1** ⇒ 这一跳**真的走到了**那道闸（⛔ 不是「压根没扫手牌」的空转绿）");

        RuleCore.ResetHandAbilityCountersForTest();
        var ctx2 = NewCtx();
        ctx2.Players[0].Hand.Add(ctx2.NewInstance(
            Synth("B143W1_F60", new[] { 60 }, new[] { Ctx(60, "UnitDamaged", 0) })));
        var damaged = Place(ctx2, 0, 4, Unit("B143W1_Dmg"));
        var acting = Place(ctx2, 1, 4, Unit("B143W1_Act"));
        int b0 = RuleCore.HandAbilityTriggerAttemptsForTest;
        int n60 = RuleCore.BroadcastUnitDamagedHandHops(ctx2, damaged, acting, 0);
        Check(n60, 0, "F2 ★ `BroadcastUnitDamagedHandHops`：同一判据 ⇒ **0 条**");
        Check(RuleCore.HandAbilityTriggerAttemptsForTest - b0, 1, "F2b ★★ 判别式：attempts **+1**（60 那一族确实走到了闸）");
    }

    // ==================================================================
    //  口令 / 汇总
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("[B143W1] ✓ " + msg); }
        else { _fail++; Debug.LogError("[B143W1] ✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B143_W1 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B143_W1 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
