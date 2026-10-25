// RuleEngineTest_EngineE9.cs — 引擎线 `E9`（2026-10-25 · 执行代理 `E9`）
//
// 为什么另开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineE5` /
// `_EngineE6` / `_EngineE7` … 都在），同一时刻只有一个写手改一个文件 ⇒ 本批测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestEngineE9)`**
// （⚠️ 另需重生成 `RuleEngineTest_Diag.cs`：`VerifyRunCaseCoverage` ② 拿「反射到的用例方法」↔
//   「生成物里的 `RunCaseNames`」对账 ⇒ 不重生成会红）。
//
// 判据全文（⛔ 不要重新论证）→ `资料/普查产出_1025/J5_判据_48行与DA22.md` §二 F5 / F5·b / §七·2。
// 本批三件事（第四件 `FollowUpSchedulerOf` 补 6 张落在 `EffectResolver.cs` = 别人在改的文件 ⇒ 本文件不碰）：
//   ① **第 ⑧ 格 `cond` 恒传 `null`** —— `RuleCore.ActingLevelCriteriaAllows` /
//      `AbilityLevelCriteriaAllows` 现在把 `unitConditions` 的 `role="acting"` / `role="target"`
//      行拼回那 36 格（原版 `CheckIfMeetsCriteria.c:298-303`）。
//   ② **`ta = 90` 的短路** —— 原版 `:193-201` **只挡身份、继续走常规格**；我们原来与 `ta=30` 合并早返。
//   ③ **战术卡 `actor == null` 静默跳过** —— 现在：`targetCriteria` 闸给 `abilityOwnerSeat` +
//      `abilityHostCard` 通道就能跑；`actingCardCriteria` 闸的候选是**那张牌自己**
//      （没有 `UnitState`）⇒ **判不了 ⇒ 放行 + 出声**（⛔ 不再静默）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）。期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestEngineE9)`）。</summary>
    public static void TestEngineE9()
    {
        Section("🆕 引擎线 E9：第 ⑧ 格 `cond` 拼回 criteria · `ta=90` 不再早返 · "
              + "战术卡（`actor == null`）的两道 ability 级闸");
        E9T1CondJoinCoverage();
        E9T2Ta90VersusTa30();
        E9T3CondGateThroughActionPump();
        E9T4ActingGateNoActorSpeaks();
        E9T5Da22ActingCond();
    }

    // ==================================================================
    //  ① 拼接口的覆盖面（数据侧读数，写死）
    // ==================================================================

    /// <summary>
    /// **拼接口今天真正有几个消费者** —— 全池读 `unitConditions` 里挂在 ability 级
    /// （`lg == -1`）且所属 ability **真有** criteria 行的那两档。
    /// <para>现算口径（`cards_engine.json` v28 / 池 1127 张，2026-10-25 现读）：
    /// `role="target"` **24 行**、`role="acting"` **1 行**（那 1 行就是 `DA22 ab=1`）。</para>
    /// </summary>
    static void E9T1CondJoinCoverage()
    {
        var pool = CardDatabase.Load();
        int joinedTarget = 0, joinedActing = 0;
        foreach (var c in pool)
        {
            if (c == null || c.Conditions == null) continue;
            for (int i = 0; i < c.Conditions.Count; i++)
            {
                var r = c.Conditions[i];
                if (r == null || r.Ability < 0 || r.Logic != -1) continue;   // 只数 ability 级那一档
                if (string.Equals(r.Role, "target", StringComparison.Ordinal)
                    && c.AbilityLevelCriteriaAt(r.Ability) != null) joinedTarget++;
                else if (string.Equals(r.Role, "acting", StringComparison.Ordinal)
                         && c.AbilityActingCriteriaAt(r.Ability) != null) joinedActing++;
            }
        }
        Check(joinedTarget, 24,
              "★ ① 全池「`role=\"target\"` + `lg=-1` + 所属 ability 真有 `abilityLevelCriteria` 行」= **24** 行"
            + "（= 第 ⑧ 格拼接口今天真正的消费者）"
            + "｜🧨 卡表 v28 改行 / 生成器换了 `CONDITION_ROLES` 的落法 ⇒ 读数变，那时按新数据重钉");
        Check(joinedActing, 1,
              "★ ① 同口径的 `role=\"acting\"` = **1** 行（`DA22 ab=1`：`cond=220 ex=1`）"
            + "｜🧨 同上；或谁把 `acting` 并进 `target` 一格 ⇒ 变 0 ⇒ 红");

        // ---- 四张「触发器能唯一定位 + 两级数据都齐」的卡（⑧ 格今天真的走得到的那一档） ----
        //   ⚠️ 另外 20 行的 ability 定位不唯一（trigger 0 一族）⇒ 闸在 ⑧ 之前就按
        //   「定位不唯一 ⇒ 不触发」走掉了（`J5` §二 F5 的歧义账），⛔ 别拿它们当「⑧ 没接」。
        E9CheckJoinRow(pool, "EC40", "FollowUpResolution", 1, 220, true);
        E9CheckJoinRow(pool, "GOF104", "FollowUpResolution", 2, 220, false);
        E9CheckJoinRow(pool, "SW22", "Strike", 0, 220, true);
        E9CheckJoinRow(pool, "TAU18", "Strike", 0, 220, true);
    }

    /// <summary>一张卡同时满足：① 按 trigger 名**唯一定位**到 `ab`；② `abilityLevelCriteria` 有那 36 格；
    /// ③ `unitConditions` 有 `(ab, lg=-1, role="target")` 的 `cond` 行。三条各断一条。</summary>
    static void E9CheckJoinRow(IList<CardDef> pool, string id, string triggerName,
                               int wantAbility, int wantCond, bool wantExclude)
    {
        var c = CardDatabase.FindById(pool, id);
        CheckTrue(c != null, $"① （前提）池里有 `{id}`");
        if (c == null) return;

        string why;
        var set = c.AbilityLevelCriteriaForTrigger(triggerName, out why);
        CheckTrue(set != null,
                  $"★ ① `{id}` 的 `{triggerName}` 能**唯一定位**到一条 ability（否则这道闸在 ⑧ 之前就停了）"
                + (set == null ? $"｜实得 null（why={why}）｜🧨 卡池 id / trigger 变了 ⇒ 红" : ""));
        if (set == null) return;

        CardDef.ConditionSpec cond;
        bool has = c.TryGetCondition(wantAbility, -1, "target", out cond) && cond != null;
        CheckTrue(has, $"★ ① `{id}` 的 `ab={wantAbility}` 有 `role=\"target\"` 的 `cond` 行（⑧ 格的数据在）"
                     + "｜🧨 生成器丢了 `lg=-1` 那一档 ⇒ 红");
        if (!has) return;
        Check(cond.Condition, wantCond, $"★ ① `{id}` `ab={wantAbility}` 的 `cond` = **{wantCond}**");
        CheckTrue(cond.Exclude == wantExclude,
                  $"★ ① `{id}` `ab={wantAbility}` 的 `ex` = **{(wantExclude ? 1 : 0)}**"
                + "｜🧨 取反位读错 ⇒ 「若存活」会变成「若将死」（DA22 那条就是这么翻的车）");
    }

    // ==================================================================
    //  ② `ta = 90` vs `ta = 30`（**同一份 criteria，只差 targetsAffected**）
    // ==================================================================

    /// <summary>
    /// 原版 `FilterMethods__CheckIfMeetsCriteria.c`：
    /// · `0x1e = 30`（`:76-94`）比完身份**直接 `return`**；
    /// · `0x5a = 90`（`:193-201`）只在「候选 ≠ targetCard」时 `return 0`，**然后继续走常规格**。
    /// <para>判别式：同一张「敌人候选 + 友方可点」的棋盘上，**ta=30 放行、ta=90 淘汰** ——
    /// 两条断言用的输入逐字相同，结构上不可能由同一个实现同时满足。</para>
    /// </summary>
    static void E9T2Ta90VersusTa30()
    {
        var probes = E9ParseProbes();
        if (probes == null) return;
        var p90 = probes[0];
        var p30 = probes[1];

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var mine = Place(ctx, 0, 3, E9Vehicle("E9Mine"));
        var foe = Place(ctx, 1, 3, E9Vehicle("E9Foe"));

        // ---- 前提：两张探针卡只差 targetsAffected ----
        var s90 = p90.AbilityLevelCriteriaAt(0);
        var s30 = p30.AbilityLevelCriteriaAt(0);
        CheckTrue(s90 != null && s30 != null, "② （前提）两张探针卡都有 `abilityLevelCriteria` 行");
        if (s90 == null || s30 == null) return;
        Check(s90.TargetsAffected, 90, "② （前提）第一张 = `ta 90`");
        Check(s30.TargetsAffected, 30, "② （前提）第二张 = `ta 30`");

        // ---- ta = 30：身份相符 ⇒ 早返 true（常规格一格都不看）----
        string gap;
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, s30, mine, null, null, mine, out gap) == true,
                  "★ ② `ta=30` 候选 = 友方 + 身份相符 ⇒ **true**（`enemyUnits=1` 被早返跳过）"
                + "｜🧨 把 30 也改成「继续走常规格」⇒ 实得 false ⇒ 红（那是 90 的语义）");

        // ---- ta = 90：身份相符**之后仍要走常规格** ⇒ 友方候选被 `enemyUnits` 淘汰 ----
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, s90, mine, null, null, mine, out gap) == false,
                  "★★ ② `ta=90` 同样的输入 ⇒ **false**（`friendlyUnits=0 & enemyUnits=1` 那一格真的跑了）"
                + "｜🧨 改回「与 30 合并早返」⇒ 实得 true ⇒ 红（**这就是改前的实现**）");
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, s90, foe, null, null, foe, out gap) == true,
                  "★ ② `ta=90` 敌方候选 + 身份相符 ⇒ **true**（常规格全过）"
                + "｜🧨 90 支写成无条件 false / 无条件 true 都红（上面那条与这条夹住它）");
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, s90, foe, null, null, mine, out gap) == false,
                  "★ ② `ta=90` 候选 ≠ targetCard ⇒ **false**（身份闸本身还在）"
                + "｜🧨 把 `:193-201` 的身份判断删掉 ⇒ 实得 true ⇒ 红");
    }

    // ==================================================================
    //  ③ 第 ⑧ 格：`cond` 真的被送进 `Matches` 了（经 `RuleCore.EnqueueAbility` 的整条闸）
    // ==================================================================

    /// <summary>
    /// 走**生产的那个入口**（`RuleCore.EnqueueAbility`），而不是直接调 `Matches`：
    /// 探针是**战术卡** ⇒ `actor == null` ⇒ 改前整道 `targetCriteria` 闸被**静默跳过**
    /// （不管 `cond` 是什么都放行）。这条同时钉住 ③ 的座位通道与 ① 的 `cond` 拼接。
    /// </summary>
    static void E9T3CondGateThroughActionPump()
    {
        var probes = E9ParseProbes();
        if (probes == null) return;
        var p90 = probes[0];

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var foeHealthy = Place(ctx, 1, 3, E9Vehicle("E9FoeHealthy"));
        var foeDying = Place(ctx, 1, 2, E9Vehicle("E9FoeDying"));
        foeDying.Health = 0;                                     // `cond 220 (dealtKillDmg)` = 将死

        int before = RuleCore.AbilityLevelCriteriaWarningsForTest;

        // ---- 负例：敌方候选**活着** ⇒ `cond=220 ex=0` 不满足 ⇒ 不触发 ----
        bool enqHealthy = E9EnqueueTactic(ctx, p90, foeHealthy, seat: 0);
        CheckTrue(!enqHealthy,
                  "★★ ③ 战术卡（`actor == null`）· 候选**活着** ⇒ `EnqueueAbility` **false**"
                + "（`cond=220 ex=0` 那一格真的判了）"
                + "｜🧨 ①把 `cond` 换回 `null` ⇒ 实得 true ⇒ 红；②把 `abilityOwnerSeat` 通道去掉 ⇒ 判不了 ⇒ 放行 ⇒ 红");
        Check(RuleCore.AbilityLevelCriteriaWarningsForTest - before, 1,
              "★ ③ 而且**出声**（不静默丢）｜🧨 去掉 `return false` 前那条 `LogWarning` ⇒ 0 ⇒ 红");

        // ---- 正例：同一张卡、候选**将死** ⇒ 条件满足 ⇒ 入队 ----
        int before2 = RuleCore.AbilityLevelCriteriaWarningsForTest;
        bool enqDying = E9EnqueueTactic(ctx, p90, foeDying, seat: 0);
        CheckTrue(enqDying,
                  "★★ ③ 同一张卡 · 候选**将死** ⇒ `EnqueueAbility` **true**（判据被真正求值了）"
                + "｜🧨 `ConditionEval` 的 220 实现反了 / `ex` 读反 ⇒ 实得 false ⇒ 红");
        Check(RuleCore.AbilityLevelCriteriaWarningsForTest - before2, 0,
              "★ ③ 这一档**不出声**（闸判过了、不是「判不了」）｜🧨 把它也走成「判不了」⇒ 1 ⇒ 红");

        // ---- 灭自证：把 `cond` 换回 `null` ⇒ 同一个「活着」的候选**过** ----
        string gap;
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, p90.AbilityLevelCriteriaAt(0), foeHealthy,
                                             null, null, foeHealthy, out gap) == true,
                  "★★ ③ **灭自证**：同一个候选、**`cond` 传 `null`** ⇒ `Matches` = **true**"
                + " ⇒ 上面那条 `false` 的差别**只能**来自 `cond`"
                + "｜🧨 生产侧又传 `null` 的那一天，这两条会同时为 true ⇒ 第一条红");

        // ---- 座位拿不到 ⇒ 判不了 ⇒ **放行 + 出声**（⛔ 不静默、也不当「不满足」）----
        int before3 = RuleCore.AbilityLevelCriteriaWarningsForTest;
        bool enqNoSeat = E9EnqueueTactic(ctx, p90, foeHealthy, seat: -1);
        CheckTrue(enqNoSeat,
                  "★★ ③ `abilityOwnerSeat = -1`（求不出 `owner`）⇒ **放行**"
                + "（⛔ 不把「不知道」当「不满足」把触发掐死）"
                + "｜🧨 判不了那一档改成 `return false` ⇒ 实得 false ⇒ 红");
        Check(RuleCore.AbilityLevelCriteriaWarningsForTest - before3, 1,
              "★ ③ 而且**出声**（改前这一档是**完全静默**的 —— 连一句 warning 都没有）"
            + "｜🧨 删掉「求不出 `owner`」那条 LogWarning ⇒ 0 ⇒ 红");
    }

    // ==================================================================
    //  ④ `actor == null` 时第一道闸（`actingCardCriteria`）**出声**、不静默
    // ==================================================================

    /// <summary>
    /// 原版这一闸的被筛对象是 `actingCard`；战术卡那条路上它就是**那张牌自己**
    /// （`RawCardScript__OnCardPlayedWithTarget.c:40` → `TriggerAbilities(…, thisCard, cardPlayed, targetCard)`；
    /// `RawCardScript__TriggerAbilities.c:207` → `CanTriggerAbility(…, actingCard, …)`）。
    /// 我们**没有**「已打出的那张战术牌」的 `UnitState` ⇒ 判不了 ⇒ **放行 + 出声**。
    /// </summary>
    static void E9T4ActingGateNoActorSpeaks()
    {
        var probes = E9ParseProbes();
        if (probes == null) return;
        var p90 = probes[0];

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var foe = Place(ctx, 1, 3, E9Vehicle("E9FoeAct"));
        foe.Health = 0;      // 让后面那道 `targetCriteria` 闸也过（否则会被它拦掉，测不到本闸）

        int before = RuleCore.AbilityActingCriteriaWarningsForTest;
        bool enq = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "E9 acting/no-actor", () => { },
                                             actor: null,
                                             abilityTrigger: "ThisCardPlayed",
                                             abilityTarget: foe,
                                             abilityActingGate: true,
                                             abilityHostCard: p90,
                                             abilityOwnerSeat: 0);
        CheckTrue(enq, "★ ④ `actor == null` + `abilityActingGate` ⇒ **放行**（判不了，⛔ 不掐死）");
        Check(RuleCore.AbilityActingCriteriaWarningsForTest - before, 1,
              "★★ ④ **出声**：改前这一档**完全静默**（`if` 没有 `else`）"
            + "｜🧨 删掉那个 `else if` 分支 ⇒ 0 ⇒ 红（而「静默放行」正是这条要挡的）");

        // ---- 灭自证：同一调用但**不开**这道闸 ⇒ 计数不动（证明那 +1 来自本闸，不是别处顺带喊的）----
        int before2 = RuleCore.AbilityActingCriteriaWarningsForTest;
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "E9 acting/gate-off", () => { },
                                  actor: null,
                                  abilityTrigger: "ThisCardPlayed",
                                  abilityTarget: foe,
                                  abilityActingGate: false,
                                  abilityHostCard: p90,
                                  abilityOwnerSeat: 0);
        Check(RuleCore.AbilityActingCriteriaWarningsForTest - before2, 0,
              "★ ④ `abilityActingGate = false`（调用点声明「本路径不跑第一道闸」）⇒ 计数**不动**"
            + "｜🧨 把闸的开位判断去掉 ⇒ 1 ⇒ 红");
    }

    // ==================================================================
    //  ⑤ 真卡 `DA22` 的 `role="acting"` 第 ⑧ 格（`cond=220 ex=1`）
    // ==================================================================

    /// <summary>
    /// `DA22 Techmarine` `ab=1`：原版 `cardAbilities[1].actingCardCriteria` =
    /// `ta=10` + `conditionsFilter=220 / excludeCondition=1` ⇒ 卡面「**若其存活**，为其治疗 3 点」。
    /// <para>⚠️ 走 `Matches` 而不走 `EnqueueAbility`：`DA22` 的两条 ability **都是 `trigger 50`**
    /// ⇒ `UniqueAbilityIndexForTrigger` 判「定位不唯一」⇒ 闸在 ⑧ 之前就停了（`J5` §二 F4 的歧义账，
    /// 不在本批）。这里测的是**拼接口本身**。</para>
    /// </summary>
    static void E9T5Da22ActingCond()
    {
        var pool = CardDatabase.Load();
        var da22 = CardDatabase.FindById(pool, "DA22");
        CheckTrue(da22 != null, "⑤ （前提）池里有 `DA22`（`Techmarine`）");
        if (da22 == null) return;

        var set = da22.AbilityActingCriteriaAt(1);
        CheckTrue(set != null, "⑤ （前提）`DA22 ab=1` 有 `actingCardCriteria` 那 36 格");
        if (set == null) return;
        Check(set.TargetsAffected, 10, "⑤ （前提）`ta=10`（常规档 ⇒ ⑧ 可达）");

        CardDef.ConditionSpec cond;
        bool has = da22.TryGetCondition(1, -1, "acting", out cond) && cond != null;
        CheckTrue(has, "⑤ （前提）`ab=1` 有 `role=\"acting\"` 的 `cond` 行");
        if (!has) return;
        Check(cond.Condition, 220, "⑤ （前提）`cond = 220 (dealtKillDmg)`");
        CheckTrue(cond.Exclude, "⑤ （前提）`ex = 1`（**取反** ⇒ 「不许将死」= 卡面「若其存活」（**卡面口径**）");

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        // `keywordFilter=10 (vehicle)` ⇒ 候选必须真的是 Vehicle（卡面「若其存活」那张是载具）
        var other = Place(ctx, 0, 2, E9Vehicle("E9DaOther"));   // `thisCard`（同侧、不是候选）
        var alive = Place(ctx, 0, 3, E9Vehicle("E9DaAlive"));
        var dying = Place(ctx, 0, 5, E9Vehicle("E9DaDying"));
        dying.Health = 0;

        string gap;
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, set, alive, cond, other, other, out gap) == true,
                  "★★ ⑤ `DA22 ab=1` 候选**存活** ⇒ **true**（`ex=1` 且 220 不成立 ⇒ 条件被排除 ⇒ 放行）"
                + "｜🧨 `ex` 读成「必须将死」/ 220 实现反了 ⇒ 实得 false ⇒ 红");
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, set, dying, cond, other, other, out gap) == false,
                  "★★ ⑤ 候选**将死** ⇒ **false**（卡面「若其存活」不成立）"
                + "｜🧨 同上（两条方向相反 ⇒ 结构上不可能同时满足）");
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, set, dying, null, other, other, out gap) == true,
                  "★★ ⑤ **灭自证**：同一候选、`cond` 传 `null` ⇒ **true** ⇒ 上面那条的差别只能来自 `cond`"
                + "（= 改前两道闸恒传 `null` 的状态）｜🧨 生产侧又传 `null` ⇒ 第一条红");
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>一张**载具型**单位卡（`subtype = Vehicle`）—— `keywordFilter=10` / `armyFilter` 那几格
    /// 会拿 subtype 比，普通 `Unit(...)` 夹具（subtype 为 null）过不了 `vehicle` 那一格。</summary>
    static CardDef E9Vehicle(string name)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 1, 1, 3, 0, null,
                           subtype: "Vehicle");
    }

    /// <summary>经生产的那个入口入队：**战术卡形态**（`actor = null`）⇒ 座位与宿主卡只能由
    /// `abilityOwnerSeat` / `abilityHostCard` 给。</summary>
    static bool E9EnqueueTactic(BattleContext ctx, CardDef host, UnitState target, int seat)
    {
        return RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "E9 tactic-body", () => { },
                                         actor: null,
                                         abilityTrigger: "ThisCardPlayed",
                                         abilityTarget: target,
                                         abilityHostCard: host,
                                         abilityOwnerSeat: seat);
    }

    /// <summary>
    /// 两张**合成探针**战术卡：除了 `targetsAffected`（90 / 30）**逐字相同**，其余 =
    /// `AM75` 的真实 `abilityLevelCriteria` 那一行（`enemyUnits=1 & includeHero & includeMinions`）+
    /// `cond=220 ex=0`（「若其死亡」）。
    /// <para>⚠️ 合成卡的名字会进全局名字索引 ⇒ 收尾复原（否则后面的用例按名字找卡会撞上它）。</para>
    /// </summary>
    static List<CardDef> E9ParseProbes()
    {
        var realPool = CardDatabase.Load();
        List<CardDef> list = null;
        try
        {
            list = CardDatabase.Parse(E9ProbeJson());
            Check(list.Count, 2, "（前提）两张探针卡解析出来了");
        }
        finally
        {
            CreatePool.BuildNameIndex(realPool);
        }
        if (list == null || list.Count != 2) return null;
        return list;
    }

    static string E9ProbeJson()
    {
        // 与 `AM75 ab=1` 的 criteria 逐字一致，只有 `targetsAffected` 不同。
        const string crit =
            "\"criteria\":{\"playerTurn\":-1,\"targetsAffected\":%TA%,\"chosenArmy\":0,\"armyFilter\":0,"
          + "\"keywordFilter\":0,\"traitsFilter\":0,\"maxTargets\":0,\"targetSelectMethod\":0,"
          + "\"attackFilter\":0,\"attackFilterOption\":0,\"currentHealthFilter\":0,"
          + "\"currentHealthFilterOption\":0,\"manaCostFilter\":0,\"manaCostFilterOption\":0,"
          + "\"rarityFilter\":0,\"rarityFilterOption\":0,\"spellType\":0,\"friendlyUnits\":0,"
          + "\"enemyUnits\":1,\"includeHero\":1,\"includeMinions\":1,\"includeSpells\":0,"
          + "\"includeWhispers\":0,\"includeWeapons\":0,\"includeRemnants\":0,\"includeHeroParts\":0,"
          + "\"includeSelf\":0,\"excludeNotSelf\":0,\"excludeSimilarCards\":0,\"excludeTargetted\":0,"
          + "\"excludeFriendlyHero\":0,\"excludeEnemyHero\":0,\"excludeRace\":0,\"excludeTrait\":0,"
          + "\"selectFromChooseResolution\":0,\"filterSpecificCards\":[],\"excludeEnchantments\":[]}";
        string one = "{\"id\":\"%ID%\",\"name\":\"%NAME%\",\"type\":\"tactic\",\"desc\":\"Deal 1 damage\","
                   + "\"rarity\":\"Common\",\"faction\":\"Ultramarines\",\"cost\":1,\"attack\":0,"
                   + "\"health\":0,\"ranged\":0,\"subtype\":\"\",\"keywords\":[],\"cardTraits\":[],"
                   + "\"abilityTargets\":[{\"trigger\":0,\"ta\":[%TA%]}],"
                   + "\"abilityLevelCriteria\":[{\"ab\":0,\"trigger\":0," + crit + "}],"
                   + "\"unitConditions\":[{\"ab\":0,\"lg\":-1,\"role\":\"target\",\"cond\":220,"
                   + "\"name\":\"dealtKillDmg\",\"ex\":0}]}";
        string c90 = one.Replace("%ID%", "E9ProbeTa90").Replace("%NAME%", "E9 probe ta90")
                        .Replace("%TA%", "90");
        string c30 = one.Replace("%ID%", "E9ProbeTa30").Replace("%NAME%", "E9 probe ta30")
                        .Replace("%TA%", "30");
        return "{\"version\":28,\"count\":2,\"cards\":[" + c90 + "," + c30 + "]}";
    }
}
