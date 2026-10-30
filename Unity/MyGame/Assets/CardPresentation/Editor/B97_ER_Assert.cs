// B97_ER_Assert.cs — 第九十七会话 · 执行代理 `B97_ER`
//
// ============================ 它盯的是什么 ============================
// 再裁 **#54 `CardAbility.CanTriggerAbility`**（判据 → `资料/完全复刻/判据/B97_结构差与休眠_再裁.md` 第 54 行；
// 逐条证据 → `资料/完全复刻/报告/B97_ER.md`）：
//
//   · 🔴 **原版六道闸里的第四道 `actingCardCriteria(+0x28)` 对【所有】 trigger 生效**
//     —— `CardAbility__CanTriggerAbility.c:71` 只按 trigger 号筛一次；`:74-124` 那几支
//     （0 / 80 / 0xdc=220 / 500 / 600 / 605 / 0x91=145）**只跳过 `IsRightContext`**，
//     最后都汇到 `:130` 的 `LAB_180922e57` ⇒ `:131-141` 那条 `&&` 链
//     （`+0x28` acting → `+0x30` target → `+0x38` board）**不看 trigger 号**。
//   · 我们改前**只在五族**（Attack / Die / 510 / 570 / 515）启用这一道（`AbilityActingGateApplies`），
//     而 `abilityActingCriteria` 现读 83 行里**有 56 行落在另外 21 个 trigger 上**（一条都没筛）
//     ⇒ **比原版宽 ⇒ 多触发**（可达面约 55 张）。本件验的就是「扩到全部 trigger」这件事。
//   · 另：原版 `ThisUnitDeath(80)`/`Unstable(81)` 只发给**死者自己**（`RawCardScript__OnMinionDeath.c:17-24`
//     的 `thisCard == deadCard` 支，`actingCard = thisCard`），观察者才是 90 —— 我们的 `Die` 广播恒映射 90
//     ⇒ `GOF3` 那条 80 行**查不到自己**。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `Editor/RuleEngineTest.cs` 的 `Run()` 里加**一个块**（位置建议 = 紧接 `{ CheckB97W1Grants(); }` 之后，
// 与 `B97_W1` 同一批）：
//       // 🆕 2026-10-29（调度台代接线）：B97_ER —— 再裁 #54 第一道闸扩到全部 trigger。
//       { CheckB97ERAbilityGates(); }
//   🔴 **故意不用 `Step(...)`、方法名也不带 `Test` 前缀**（理由同 `B97_W1` / `B96_CR`）：
//     `Step(...)` 会让 `_ranCases` 记账、`VerifyRunCaseCoverage()` 拿反射清单对账，
//     而 `工具/gen_diag_runner.py` 只认「8 空格缩进的顶层语句」⇒ 两者都得跟着改。块调用两条都不碰。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」都写在文案里）============================
//   · `AbilityActingGateApplies` 改回「只认那五族」 ⇒ 第 ① 段红（`OtherUnitSummoned` 不是那五族）。
//   · `AbilityActingGateCard` 把 `Deploy` 那一格删掉（回 `null`）⇒ 第 ② 段负例变绿、第 ① 段红。
//   · `EnqueueAbility` 里把 `actingTrig` 改回 `abilityTrigger`（取消解耦）⇒ 第 ③ 段红
//     （第二道闸的计数会跟着涨 —— 那正是「共用一个名字」的后果）。
//   · 第 ④ 段（`actor == null` + 宿主卡/座位那条新来路）删掉 ⇒ 负例放行 ⇒ 红。
//   · 第 ⑤ 段（真卡池 `GOF3` 的 `ThisUnitDeath`）若有人把 `Die` 恒映射成 90 的旧口径套回来 ⇒ 红。
using System;
using System.Collections.Generic;
using System.Reflection;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，见文件头）。</summary>
    public static void CheckB97ERAbilityGates()
    {
        Section("🆕 B97_ER：再裁 #54 —— ability 级 `actingCardCriteria`（`+0x28`）扩到**全部** trigger");
        B97ERGateAppliesAll();
        B97ERGateUnitLevelNonFiveFamily();
        B97ERActingTriggerDecoupledFromTargetGate();
        B97ERHostCardBranch();
        B97ERRealPoolThisUnitDeath();
    }

    // ==================================================================
    //  ① 结构：`AbilityActingGateApplies` 对**所有** kind 都是 `true`
    // ==================================================================

    /// <summary>两个私有静态方法的反射口（⛔ 它们不是 `public`：引擎里没有生产消费者，
    /// 只给本件与 `RuleEngineTest` 内部用）。取不到 ⇒ 断言直接红、文案里说明原因。</summary>
    static MethodInfo B97ERPrivate(string name)
    {
        return typeof(RuleCore).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
    }

    static void B97ERGateAppliesAll()
    {
        var mi = B97ERPrivate("AbilityActingGateApplies");
        CheckTrue(mi != null,
                  "★ ① （前提）`RuleCore.AbilityActingGateApplies` 还在（私有静态）"
                + "｜🧨 改名 / 提成实例方法 ⇒ 本件取不到 ⇒ 红");
        if (mi == null) return;
        // 原版 `+0x28` 那道闸不看 trigger（判据见文件头）⇒ 每一个 kind 都该是 true。
        string[] kinds =
        {
            WhenEventKind.Attack, WhenEventKind.Die, WhenEventKind.Damaged,
            WhenEventKind.GetsDarkPact, WhenEventKind.Reanimated, WhenEventKind.GetsMarkerlight,
            WhenEventKind.GetsHuntMark, WhenEventKind.Play, WhenEventKind.Deploy,
            WhenEventKind.Prays, WhenEventKind.GainFaith, WhenEventKind.GainQuest,
            WhenEventKind.CreatesSecret, WhenEventKind.CreatesSabotage,
            WhenEventKind.Triggers(KeywordTable.Ferocity), WhenEventKind.Triggers(KeywordTable.Swarm),
            WhenEventKind.Triggers(KeywordTable.Codex), WhenEventKind.Triggers(KeywordTable.Duty),
            WhenEventKind.Triggers(KeywordTable.Mob), WhenEventKind.Triggers(KeywordTable.Synapse),
            "losestealth",
        };
        int off = 0;
        var offNames = new List<string>();
        foreach (var k in kinds)
            if (!(bool)mi.Invoke(null, new object[] { k })) { off++; offNames.Add(k); }
        Check(off, 0,
              "★★ ① 21 个「改前没启用」的 kind 现在**全都**过第一道闸（0 个漏网）"
            + "｜🧨 退回「只认 Attack/Die/510/570/515」⇒ 这里非 0 ⇒ 红"
            + (off == 0 ? "" : "（漏：" + string.Join(",", offNames) + "）"));

        var cmd = B97ERPrivate("AbilityActingGateCard");
        CheckTrue(cmd != null, "★ ① （前提）`RuleCore.AbilityActingGateCard` 还在");
        if (cmd == null) return;
        // `Deploy` 那一格 = `subject`（判据 `CardScript__ResolveUnitSummoned.c:31` acting=param_2=被召唤者）。
        // 🔴 2026-10-30（`B100_F18` · G4）：反射目标 `AbilityActingGateCard(string, UnitState, UnitState)`
        //   （`EffectResolver.cs:7773`）两个形参都是 `UnitState`，而 `Unit(...)` 返回的是 `CardDef`
        //   ⇒ 原来那一下直接抛 `Object of type 'RuleEngine.CardDef' cannot be converted to type 'RuleEngine.UnitState'`。
        //   这里造一个真的 `UnitState`（同 `RuleEngineTest_EngineB95WRS.cs` 的写法），
        //   并且把 `actingCard` 传 **null** —— `Deploy` 那一格若被错接成 `actingCard` ⇒ 回 null ⇒ 判据更硬。
        var dummy = new UnitState(CardInstance.Detached(Unit("B97ERDummy", 1, 1, 1)), false);
        var cand = (UnitState)cmd.Invoke(null, new object[] { WhenEventKind.Deploy, dummy, null });
        CheckTrue(ReferenceEquals(cand, dummy),
                  "★★ ① `Deploy` 那一族的 `actingCard` = 被召唤的那个单位（= `subject`）"
                + "｜🧨 删掉这一格（回 null）⇒ 那一族永远「判不了 ⇒ 放行」⇒ 红");
        // 查不清的族仍必须是 null（⛔ 不拿 subject 顶）
        var unk = (UnitState)cmd.Invoke(null, new object[] { WhenEventKind.Draw, dummy, dummy });
        CheckTrue(unk == null,
                  "★ ① `OtherCardDrawn(260)` 这一格**仍返回 null**（⛔ 没查清就拿 subject 顶）"
                + "｜🧨 给所有族一律 return subject ⇒ 这一格非 null ⇒ 红（那等于筛错人）");
    }

    // ==================================================================
    //  ② 单元级：**非五族**（`Deploy` / `OtherUnitSummoned`）也真的被筛
    // ==================================================================

    static void B97ERGateUnitLevelNonFiveFamily()
    {
        // 合成卡：trigger 190（`OtherUnitSummoned`）+ 第一道闸 `ta=10 / enemyUnits=1`
        //   —— 与真卡 `AM7 Vox Operator` / `ASH66` / `AM55` …（现读 11 行 190）同形。
        var card = E3Synthetic("B97ERDeploy", "", new[] { 190 },
                               new Dictionary<int, CardDef.CountSetSpec> { { 0, E2Criteria10(1, 0) } });
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var own = Place(ctx, 0, 3, card);
        var foe = Place(ctx, 1, 1, Unit("B97ERFoe", 1, 2, 5));
        var mate = Place(ctx, 0, 4, Unit("B97ERMate", 1, 2, 5));
        CheckTrue(own != null && own.Card == card, "② （前提）合成卡挂在场上那个 `UnitState` 上");

        int warn0 = RuleCore.AbilityActingCriteriaWarningsForTest;

        bool q1 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "B97ER·正例", () => { }, actor: own,
                                          abilityActingTrigger: "OtherUnitSummoned",
                                          abilityActingCard: foe, abilityActingGate: true);
        CheckTrue(q1, "★★ ② 正例：`actingCard` = **敌方** ⇒ 第一道闸满足 ⇒ 触发（入队成功）");
        Check(RuleCore.AbilityActingCriteriaWarningsForTest, warn0, "★ ② 正例**不出声**");

        int warn1 = RuleCore.AbilityActingCriteriaWarningsForTest;
        bool q2 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "B97ER·负例", () => { }, actor: own,
                                          abilityActingTrigger: "OtherUnitSummoned",
                                          abilityActingCard: mate, abilityActingGate: true);
        CheckTrue(!q2, "★★ ② 负例：`actingCard` = **己方** ⇒ 第一道闸不满足 ⇒ **不触发**"
                     + "｜🧨 这一族没接（闸恒不启用 / 候选恒 null）⇒ 放行 ⇒ 红");
        CheckTrue(RuleCore.AbilityActingCriteriaWarningsForTest > warn1,
                  "★★ ② 负例**出声**（反静默的唯一判别式）");

        int warn2 = RuleCore.AbilityActingCriteriaWarningsForTest;
        bool q3 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "B97ER·无候选", () => { }, actor: own,
                                          abilityActingTrigger: "OtherUnitSummoned",
                                          abilityActingCard: null, abilityActingGate: true);
        CheckTrue(q3, "★★ ② 拿不到 `actingCard` ⇒ **判不了 ⇒ 放行**（⛔ 不许当「不满足」把触发掐死）"
                    + "｜🧨 把 null 候选当 false ⇒ 一整族被静默掐死 ⇒ 红");
        CheckTrue(RuleCore.AbilityActingCriteriaWarningsForTest > warn2, "★ ② 「判不了」也出声");
    }

    // ==================================================================
    //  ③ 解耦：`abilityActingTrigger` 喂第一道闸，**不许**顺带把第二道闸（target）点亮
    // ==================================================================

    static void B97ERActingTriggerDecoupledFromTargetGate()
    {
        var card = E3Synthetic("B97ERDecouple", "", new[] { 190 },
                               new Dictionary<int, CardDef.CountSetSpec> { { 0, E2Criteria10(1, 0) } });
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var own = Place(ctx, 0, 3, card);
        var mate = Place(ctx, 0, 4, Unit("B97ERMate2", 1, 2, 5));

        int lw0 = RuleCore.AbilityLevelCriteriaWarningsForTest;
        bool q = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "B97ER·解耦", () => { }, actor: own,
                                         abilityActingTrigger: "OtherUnitSummoned",
                                         abilityActingCard: mate, abilityActingGate: true);
        CheckTrue(!q, "★ ③ （前提）第一道闸照样拒了（= 解耦没有把闸弄失效）");
        Check(RuleCore.AbilityLevelCriteriaWarningsForTest, lw0,
              "★★ ③ 只给 `abilityActingTrigger` 时**第二道闸（target/board）一次都不跑**"
            + "｜🧨 改回 `actingTrig = abilityTrigger`（共用一个形参）⇒ 第二道闸会跟着跑并出声 ⇒ 计数变 ⇒ 红"
            + "（原版三道闸的覆盖面本就不同：acting 是全部 trigger，target/board 我们只查清了七族）");

        // 老调用点：只给 `abilityTrigger`、不给 `abilityActingTrigger` ⇒ 第一道闸吃 `abilityTrigger`（逐字不变）
        int warn0 = RuleCore.AbilityActingCriteriaWarningsForTest;
        bool q2 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "B97ER·老口径", () => { }, actor: own,
                                          abilityTrigger: "OtherUnitSummoned",
                                          abilityActingCard: mate, abilityActingGate: true);
        CheckTrue(!q2, "★★ ③ 老调用点（只给 `abilityTrigger`）行为**逐字不变**（缺省回退那一格还在）");
        CheckTrue(RuleCore.AbilityActingCriteriaWarningsForTest > warn0, "★ ③ 老口径也照样出声一条");
    }

    // ==================================================================
    //  ④ 没有 `actor` 的那条新来路：宿主卡 + 座位（手牌监听 / 手牌陷阱 / 常驻效果）
    // ==================================================================

    static void B97ERHostCardBranch()
    {
        // 手牌里的监听者**没有 `UnitState`** ⇒ 改前这一路恒「判不了 ⇒ 放行」；
        // 现在按 `abilityHostCard` + `abilityOwnerSeat` 判（判据：原版那几跳的 `thisCard` = 那张牌自己）。
        var card = E3Synthetic("B97ERHand", "", new[] { 190 },
                               new Dictionary<int, CardDef.CountSetSpec> { { 0, E2Criteria10(1, 0) } });
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var foe = Place(ctx, 1, 1, Unit("B97ERHandFoe", 1, 2, 5));
        var mate = Place(ctx, 0, 4, Unit("B97ERHandMate", 1, 2, 5));

        bool q1 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "B97ER·手牌正例", () => { },
                                          abilityActingTrigger: "OtherUnitSummoned",
                                          abilityActingCard: foe, abilityActingGate: true,
                                          abilityHostCard: card, abilityOwnerSeat: 0);
        CheckTrue(q1, "★★ ④ 无 `actor` + 宿主卡/座位 ⇒ 正例（敌方 acting）**触发**");

        bool q2 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "B97ER·手牌负例", () => { },
                                          abilityActingTrigger: "OtherUnitSummoned",
                                          abilityActingCard: mate, abilityActingGate: true,
                                          abilityHostCard: card, abilityOwnerSeat: 0);
        CheckTrue(!q2, "★★ ④ 无 `actor` + 宿主卡/座位 ⇒ 负例（己方 acting）**不触发**"
                     + "｜🧨 删掉 `abilityOwnerSeat` 那条支（回「actor==null ⇒ 一律判不了」）⇒ 放行 ⇒ 红");

        int warn0 = RuleCore.AbilityActingCriteriaWarningsForTest;
        bool q3 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "B97ER·无座位", () => { },
                                          abilityActingTrigger: "OtherUnitSummoned",
                                          abilityActingCard: mate, abilityActingGate: true,
                                          abilityHostCard: card, abilityOwnerSeat: -1);
        CheckTrue(q3, "★ ④ 宿主卡有、座位没有 ⇒ **判不了 ⇒ 放行**（⛔ 不猜 `owner`）");
        CheckTrue(RuleCore.AbilityActingCriteriaWarningsForTest > warn0, "★ ④ 这一档也出声");
    }

    // ==================================================================
    //  ⑤ 真卡池：`ThisUnitDeath(80)` 那一行**查得到**（`GOF3`），观察者仍是 90
    // ==================================================================

    static void B97ERRealPoolThisUnitDeath()
    {
        var pool = CardDatabase.Load();
        var gof3 = CardDatabase.FindById(pool, "GOF3");
        CheckTrue(gof3 != null, "⑤ （前提）卡池里有 `GOF3 Bomb Squig`");
        if (gof3 == null) return;
        string why;
        var set80 = gof3.ActingLevelCriteriaForTrigger("ThisUnitDeath", out why);
        CheckTrue(set80 != null,
                  "★★ ⑤ `GOF3` 的 `ThisUnitDeath(80)` 那一行**查得到**（`ta=20`）"
                + "｜🧨 `UniqueAbilityIndexForTrigger` 的 trigger 名表丢了 80 ⇒ null ⇒ 红");
        if (set80 != null)
            Check(set80.TargetsAffected, 20,
                  "★★ ⑤ 那一行是 `ta = 20 self`（原版真值；acting = 死者自己）");
        var set90 = gof3.ActingLevelCriteriaForTrigger("OtherUnitDeath", out why);
        CheckTrue(set90 == null,
                  "★ ⑤ `GOF3` 在 `OtherUnitDeath(90)` 上**没有**这一格（80 与 90 是两条 ability）"
                + "｜🧨 把 80 那行错落到 90 上 ⇒ 非 null ⇒ 红");

        // 我们的 `Die` 广播对**观察者**恒映射 90（逐字不变）；死者自己那一支要走 80
        //   —— 判据 `RawCardScript__OnMinionDeath.c:17-24`（`thisCard == deadCard` ⇒ `0x50`/`0x51`）。
        Check(TriggerNameOfWhenKindForAssert(WhenEventKind.Die), "OtherUnitDeath",
              "★★ ⑤ 观察者那一跳的 trigger 名仍是 `OtherUnitDeath`（90）"
            + "｜🧨 把 `Die` 整体改成 ThisUnitDeath ⇒ 观察者那 12 行会筛错人 ⇒ 红");
    }

    /// <summary>`EffectResolver.TriggerNameOfWhenKind`（私有静态）的反射口 —— 只为第 ⑤ 段那条。
    /// ⚠️ 反射不到就返回 `"<取不到>"`（断言会红并把原因写在文案里，⛔ 不静默放行）。</summary>
    static string TriggerNameOfWhenKindForAssert(string kind)
    {
        var mi = B97ERPrivate("TriggerNameOfWhenKind");
        if (mi == null) return "<取不到 TriggerNameOfWhenKind>";
        try
        {
            return (string)mi.Invoke(null, new object[] { kind, false });
        }
        catch (Exception e)
        {
            return "<抛 " + e.GetType().Name + ">";
        }
    }
}
