// RuleEngineTest_EngineE3.cs — 引擎数据管线 `E3`（2026-10-25 · 第七十五会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineE2` /
// `_EngineA3` / `_EngineC5` … 都在），同一时刻只有一个写手改一个文件 ⇒ 本批测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestE3AbilityActingCriteria)`**。
//
// 判据（⛔ 不要重新论证；全文 → `资料/普查产出_1025/E2_ability_criteria管线.md` §六 /
// `资料/普查产出_1025/E3_actingCardCriteria.md`）：
//   · ability 级 **第一道闸** = 原版 **`CardAbility.actingCardCriteria`（`+0x28`）**：
//       `CardAbility__CanTriggerAbility.c:127-130`
//       `CheckIfMeetsCriteria(thisCard, actingCard, targetCard, *(ability+0x28), 0)`
//     形参表 `d:/2/tools/il2cpp_out/dump.cs:20360` ⇒ **被筛对象 = `actingCard`**。
//   · 第二道闸 = `+0x30`（筛 `targetCard`，v26 `abilityLevelCriteria`，见 `_EngineE2`）⇒
//     **短路次序 = acting → target → board**。
//   · `targetsAffected == 0 (any)` ⇒ 原版 `return 1` ⇒ 生成器**不落那一行**（零信息）。
//   · 🔴 **`Die` 的 `actingCard` = 凶手，不是死者** —— `RawCardScript.OnMinionDeath(manager,
//     thisCard, actingCard, deadCard, …)`（`dump.cs:23526`）里是**两个形参**。我们这一路
//     = `CleanupDeaths` 的 `actingCard`（`DamageInfo.actingCard` 带下来的来源卡）；
//     **没有来源的死（毒 / 反噬 / 反击）拿不到 ⇒ 那道闸判不了 ⇒ 出声 + 跳过（放行）**。
//
// 🔴 每条断言都带 `🧨`（什么改动能让它红）。期望值一律写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestE3AbilityActingCriteria)`）。</summary>
    public static void TestE3AbilityActingCriteria()
    {
        Section("🆕 引擎线 E3：ability 级 `actingCardCriteria`（`CardAbility.actingCardCriteria` `+0x28`）"
              + " —— 数据落位 · 唯一入口 · 触发前第一道闸（Attack / Die）");
        E3Case1DataPipelineRealPool();
        E3Case2UniqueEntryAndUnknown();
        E3Case3GateUnitLevel();
        E3Case4BroadcastWiring();
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>`ta = 20 self` 的 36 格（其余格全 0 = 原版真值里那 30 行的形状）。
    /// ⚠️ 必须逐格给：`CountSetSpec` 的缺省是 `Unknown(-1)`，`TargetCriteriaEval` 遇 `Unknown`
    /// 返回 `null`（判不了）⇒ 夹具会静默变成「没验到」。直接复用 `_EngineE2` 的满格夹具再改 `ta`。</summary>
    static CardDef.CountSetSpec E3CriteriaSelf()
    {
        var s = E2Criteria10(0, 0);
        s.TargetsAffected = 20;
        return s;
    }

    /// <summary>造一张合成卡：`trigger` 数组 = 每条 ability 的 trigger（下标 = `cardAbilities` 下标）。
    /// `acting` / `level` = 两列**各自的**行（键 = `ab`）—— 🔴 两列**不共用一个字典**（本件要验）。
    /// ⚠️ `abilityTriggersVersioned: true` 是 trigger 定位的版本位（v17），不给 ⇒ 一路 `-1`。</summary>
    static CardDef E3Synthetic(string id, string desc, int[] triggers,
                              Dictionary<int, CardDef.CountSetSpec> acting,
                              Dictionary<int, CardDef.CountSetSpec> level = null,
                              bool actingVersioned = true, bool levelVersioned = true)
    {
        var ta = new List<CardDef.AbilityTargetSpec>();
        for (int i = 0; i < triggers.Length; i++)
            ta.Add(new CardDef.AbilityTargetSpec { Trigger = triggers[i], TargetsAffected = new[] { 10 } });
        var arows = new List<CardDef.TargetCriteriaRowSpec>();
        if (acting != null)
            foreach (var kv in acting)
                arows.Add(new CardDef.TargetCriteriaRowSpec
                {
                    Ability = kv.Key, Trigger = triggers[kv.Key], Logic = -1, Criteria = kv.Value,
                });
        var lrows = new List<CardDef.TargetCriteriaRowSpec>();
        if (level != null)
            foreach (var kv in level)
                lrows.Add(new CardDef.TargetCriteriaRowSpec
                {
                    Ability = kv.Key, Trigger = triggers[kv.Key], Logic = -1, Criteria = kv.Value,
                });
        return new CardDef(id, id, "unit", desc, "common", "Test",
                           1, 1, 5, 0, null,
                           subtype: "Infantry",
                           abilityTriggersVersioned: true,
                           abilityTargets: ta.ToArray(),
                           // 🔴🆕 **2026-10-25（执行代理 `F3` · 铁律 5·b）：`conditionsVersioned: true` 必须给。**
                           //   理由与 `_EngineE2.E2Synthetic` 同一处（那里有完整判据）：
                           //   `E9` 接上第 ⑧ 格之后，缺省 `false` = 「旧表 ⇒ 判不了」哨兵
                           //   （`cond = ConditionUnknown` = -1）⇒ 第一道闸在**正例**上也会
                           //   `verdict == null` ⇒ 出声（`AbilityActingCriteriaWarningsForTest` +1）。
                           //   真卡池 v27 ⇒ `ConditionsVersioned` 恒 true ⇒ 夹具同形。
                           //   ⚠️ 断言一个字没动，改的是夹具。
                           conditionsVersioned: true,
                           abilityLevelCriteria: levelVersioned ? lrows.ToArray() : null,
                           abilityLevelCriteriaVersioned: levelVersioned,
                           abilityActingCriteria: actingVersioned ? arows.ToArray() : null,
                           abilityActingCriteriaVersioned: actingVersioned);
    }

    // ==================================================================
    //  ① 数据管线：真卡池里那 83 行**真的读得到**，且每一格都对得上生成器
    // ==================================================================

    static void E3Case1DataPipelineRealPool()
    {
        var pool = CardDatabase.Load();

        // ---- ①·a `SW16 Long Fang`：`ta = 20 self`（「acting 就是我」）----
        var sw16 = CardDatabase.FindById(pool, "SW16");
        CheckTrue(sw16 != null, "① （前提）卡池里有 `SW16 Long Fang`");
        if (sw16 != null)
        {
            CheckTrue(sw16.AbilityActingCriteriaVersioned,
                      "★★ ① `abilityActingCriteria` 列在（`version >= 27`）"
                    + "｜🧨 生成器不落这一列 / `CardDatabase` 不传版本位 ⇒ 访问器一路 null ⇒ 红");
            CheckTrue(sw16.AbilityActingCriteriaRowCount > 0,
                      "★ ① `SW16` 有第一道闸的行（现读 1 行）");
            string why;
            var set = sw16.ActingLevelCriteriaForTrigger("UnitAttack", out why);
            CheckTrue(set != null,
                      "★★ ① `SW16` 取得到 `UnitAttack` 那条 ability 的第一道闸 36 格"
                    + $"（why = {why ?? "（无）"}）"
                    + "｜🧨 生成器只落第二道闸那一列（`abilityLevelCriteria`）⇒ 这里 null ⇒ 红");
            if (set != null)
            {
                Check(set.TargetsAffected, 20, "★★ ① `ta = 20 self`（原版那一批 30 行的主档）"
                                             + "｜🧨 `ta` 被写成第二道闸那一列的值（`SW16` 是 10）⇒ 红");
                Check(set.EnemyUnits, 0, "★ ① `enemyUnits = 0`（self 档下其余 35 格在真值里全是 0）");
                Check(set.FriendlyUnits, 0, "★ ① `friendlyUnits = 0`");
            }
        }

        // ---- ①·b `UM70 Invictor Warsuit`：`ta = 10 · enemyUnits = 1`（「attacker 必须是敌方」）----
        var um70 = CardDatabase.FindById(pool, "UM70");
        CheckTrue(um70 != null, "① （前提）卡池里有 `UM70 Invictor Warsuit`");
        if (um70 != null)
        {
            string why;
            var set = um70.ActingLevelCriteriaForTrigger("UnitAttack", out why);
            CheckTrue(set != null, "★★ ① `UM70` 取得到 `UnitAttack` 那条 ability 的第一道闸");
            if (set != null)
            {
                Check(set.TargetsAffected, 10, "★ ① `UM70` `ta = 10 allFiltered`");
                Check(set.EnemyUnits, 1, "★★ ① `UM70` `enemyUnits = 1`（acting 必须是**敌方**）"
                                        + "｜🧨 两列串了（读到 `abilityLevelCriteria`）⇒ 值是 `ta=10 / enemyUnits=1` "
                                        + "……两张卡这一格恰好相同 ⇒ 本行**抓不到**，靠 ①·a 的 `ta` 与 ③ 的合成卡抓");
                Check(set.IncludeMinions, 1, "★ ① `UM70` `includeMinions = 1`");
                Check(set.IncludeHero, 1, "★ ① `UM70` `includeHero = 1`");
            }
        }

        // ---- ①·c 覆盖面读数（生成器与卡池必须对得上；数字写死 = 变化时红）----
        int actingCards = 0, actingRows = 0;
        foreach (var c in pool)
        {
            if (c == null) continue;
            if (c.AbilityActingCriteriaRowCount > 0) actingCards++;
            actingRows += c.AbilityActingCriteriaRowCount;
        }
        Check(actingRows, 83, "★★ ① 全池第一道闸落 **83 行**（v27 生成器现读）"
                            + "｜🧨 落行门槛从 `ta != 0` 改成「36 格非平凡」（那会多出 10 行 `ta=0` 的零信息行）⇒ 93 ⇒ 红");
        Check(actingCards, 80, "★★ ① 全池第一道闸落 **80 张卡**"
                             + "｜🧨 同上 ⇒ 86 ⇒ 红");
    }

    // ==================================================================
    //  ② 唯一入口：取不到时**明确是空的哪一档**，⛔ 不是默认值
    // ==================================================================

    static void E3Case2UniqueEntryAndUnknown()
    {
        // ---- ②·a 旧表（列没落）：`null` + why 说「没落」，⛔ 不是「没有条件」 ----
        {
            var c = E3Synthetic("E3Old", "", new[] { 50 },
                                new Dictionary<int, CardDef.CountSetSpec> { { 0, E3CriteriaSelf() } },
                                actingVersioned: false);
            string why;
            CheckTrue(c.AbilityActingCriteriaAt(0) == null,
                      "★ ②a 旧表（版本位 false）⇒ `AbilityActingCriteriaAt(0)` 是 **null**（⛔ 不是「默认 criteria」）");
            var set = c.ActingLevelCriteriaForTrigger("UnitAttack", out why);
            CheckTrue(set == null, "★ ②a 旧表 ⇒ `ActingLevelCriteriaForTrigger` 返回 **null**");
            CheckTrue(why != null && why.Contains("没落"),
                      "★★ ②a 旧表 ⇒ `why` 明确说「列没落」（这一档消费端必须**放行并出声**）"
                    + "｜🧨 版本位判定被吞（恒 true）⇒ why 变 null ⇒ 红");
        }

        // ---- ②·b 新版但这条 ability 没有行 ⇒ `null` + why == null（= **明确没有条件**）----
        {
            var c = E3Synthetic("E3Partial", "", new[] { 50, 90 },
                                new Dictionary<int, CardDef.CountSetSpec> { { 1, E3CriteriaSelf() } });
            string why;
            var set = c.ActingLevelCriteriaForTrigger("UnitAttack", out why);
            CheckTrue(set == null && why == null,
                      "★★ ②b 第 0 条 ability（`UnitAttack`）**没有那一行** ⇒ 返回 null 且 `why == null`"
                    + "（= 明确「没有这一格条件」；这一档消费端**放行、不出声**）"
                    + "｜🧨 把「没落」与「没有这一行」混成一档 ⇒ `why` 非空 ⇒ 红");
            var set90 = c.ActingLevelCriteriaForTrigger("OtherUnitDeath", out why);
            CheckTrue(set90 != null, "★ ②b 第 1 条 ability（`OtherUnitDeath`）**取得到**（两条互不串行）");
        }

        // ---- ②·c 认不出的 trigger 名 ⇒ `null` + why（判不了，⛔ 不是「没有」）----
        {
            var c = E3Synthetic("E3BadName", "", new[] { 50 },
                                new Dictionary<int, CardDef.CountSetSpec> { { 0, E3CriteriaSelf() } });
            string why;
            var set = c.ActingLevelCriteriaForTrigger("NotARealTrigger", out why);
            CheckTrue(set == null && why != null && why.Contains("认不出"),
                      "★★ ②c 认不出的 trigger 名 ⇒ `null` + `why` 说「认不出」"
                    + "｜🧨 退回 `ThisCardPlayed(0)` 之类的默认值 ⇒ 会静默筛错 ⇒ 红");
        }

        // ---- ②·d 取不到不许落成「默认值」：`AbilityActingCriteriaAt(-1)` 恒 null ----
        {
            var c = E3Synthetic("E3Neg", "", new[] { 50 },
                                new Dictionary<int, CardDef.CountSetSpec> { { 0, E3CriteriaSelf() } });
            CheckTrue(c.AbilityActingCriteriaAt(-1) == null,
                      "★ ②d `AbilityActingCriteriaAt(-1)`（active/alternative 那一路）**恒 null** —— "
                    + "那一层是 `ActiveAbility` 自己的 criteria，本列不收");
        }

        // ---- ②·e 🔴 **两列不共用字典**：同一张卡、同一个 `ab` 位、两份不同的 criteria ----
        {
            var actingSelf = E3CriteriaSelf();                       // ta = 20
            var levelEnemy = E2Criteria10(1, 0);                     // ta = 10 / enemyUnits = 1
            var c = E3Synthetic("E3TwoCols", "", new[] { 50 },
                                new Dictionary<int, CardDef.CountSetSpec> { { 0, actingSelf } },
                                new Dictionary<int, CardDef.CountSetSpec> { { 0, levelEnemy } });
            string why;
            var a = c.ActingLevelCriteriaForTrigger("UnitAttack", out why);
            var l = c.AbilityLevelCriteriaForTrigger("UnitAttack", out why);
            CheckTrue(a != null && l != null && !ReferenceEquals(a, l),
                      "★★ ②e 同一 `ab` 位上两列**各是各的**（不是同一个对象）"
                    + "｜🧨 两列塞进同一个 `Dictionary<int, CountSetSpec>` ⇒ 后写的盖掉先写的 ⇒ 其中一个变 null/同一个 ⇒ 红");
            Check(a != null ? a.TargetsAffected : -1, 20,
                  "★★ ②e 第一道闸那一列仍是 `ta = 20`（没被第二道闸的 `ta = 10` 覆盖）"
                + "｜🧨 共用一个字典 ⇒ 这里是 10 ⇒ 红");
        }
    }

    // ==================================================================
    //  ③ 闸（单元级，走**生产入口** `RuleCore.EnqueueAbility`）
    // ==================================================================

    static void E3Case3GateUnitLevel()
    {
        // 合成卡直接当 `Place` 的 `CardDef` 用 ⇒ 场上那个 `UnitState.Card` 就是它。
        var gateCard = E3Synthetic("E3Gate", "", new[] { 50 },
                                   new Dictionary<int, CardDef.CountSetSpec> { { 0, E2Criteria10(1, 0) } });

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var own = Place(ctx, 0, 3, gateCard);
        var foe = Place(ctx, 1, 1, Unit("E3GateFoe", 1, 2, 5));
        var mate = Place(ctx, 0, 4, Unit("E3GateMate", 1, 2, 5));
        CheckTrue(own != null && own.Card == gateCard,
                  "③ （前提）合成卡真的挂到了场上那个 `UnitState` 上");

        int warn0 = RuleCore.AbilityActingCriteriaWarningsForTest;
        int lwarn0 = RuleCore.AbilityLevelCriteriaWarningsForTest;

        // ---- 正例：`actingCard` = 敌方 ⇒ 满足 ⇒ **触发** ----
        bool queued = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "E3·正例",
                                              () => { }, actor: own,
                                              abilityTrigger: "UnitAttack", abilityTarget: null,
                                              abilityActingCard: foe, abilityActingGate: true);
        CheckTrue(queued, "★★ ③ 正例：`actingCard` 是**敌方** ⇒ 第一道闸满足 ⇒ **触发**（入队成功）"
                        + "｜🧨 候选接成 `thisCard`（= 载体自己，己方）⇒ 这一格变 false ⇒ 红");
        Check(RuleCore.AbilityActingCriteriaWarningsForTest, warn0,
              "★ ③ 正例**不出声**（第一道闸计数不变）");

        // ---- 负例：`actingCard` = 己方 ⇒ 不满足 ⇒ **不触发** + 出声 ----
        int warn1 = RuleCore.AbilityActingCriteriaWarningsForTest;
        bool queued2 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "E3·负例",
                                               () => { }, actor: own,
                                               abilityTrigger: "UnitAttack", abilityTarget: null,
                                               abilityActingCard: mate, abilityActingGate: true);
        CheckTrue(!queued2, "★★ ③ 负例：`actingCard` 是**己方** ⇒ 第一道闸不满足 ⇒ **不触发**（入队被拒）"
                          + "｜🧨 闸只写不判（永远放行）⇒ 红");
        CheckTrue(RuleCore.AbilityActingCriteriaWarningsForTest > warn1,
                  "★★ ③ 负例**出声**（第一道闸计数 +1）—— 反静默的唯一判别式"
                + "｜🧨 只 return false 不打日志 ⇒ 红");
        Check(RuleCore.AbilityLevelCriteriaWarningsForTest, lwarn0,
              "★★ ③ 第一道闸拒绝时**根本走不到第二道闸**（原版短路次序 acting → target）"
            + "｜🧨 两道闸的次序写反 / 两道闸跑成两个独立 if（不 return）⇒ 第二道闸的计数也会涨 ⇒ 红");

        // ---- 判不了那一档：拿不到 `actingCard` ⇒ 出声但**放行**（⛔ 不许当「不满足」）----
        int warn2 = RuleCore.AbilityActingCriteriaWarningsForTest;
        bool queued3 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "E3·无 acting",
                                               () => { }, actor: own,
                                               abilityTrigger: "UnitAttack", abilityTarget: null,
                                               abilityActingCard: null, abilityActingGate: true);
        CheckTrue(queued3, "★★ ③ 拿不到 `actingCard` ⇒ **判不了 ⇒ 放行**（⛔ 不许当「不满足」把触发掐死）"
                         + "｜🧨 把 null 候选当 false ⇒ 会**静默掐死**一整族（没有来源的死亡）⇒ 红");
        CheckTrue(RuleCore.AbilityActingCriteriaWarningsForTest > warn2,
                  "★ ③ 「判不了」也要**出声**（第一道闸计数 +1）");

        // ---- 歧义例：2 条同 trigger 的 ability ⇒ **不触发** + 出声 ----
        var ambCard = E3Synthetic("E3Amb", "", new[] { 50, 50 },
                                  new Dictionary<int, CardDef.CountSetSpec>
                                  {
                                      { 0, E2Criteria10(1, 0) },
                                      { 1, E2Criteria10(1, 0) },
                                  });
        var ctx3 = ProbeBattle(new CardDef[0], new CardDef[0]);
        var aown = Place(ctx3, 0, 3, ambCard);
        var afoe = Place(ctx3, 1, 1, Unit("E3AmbFoe", 1, 2, 5));
        string why;
        bool amb;
        int ab = ambCard.UniqueAbilityIndexForTrigger("UnitAttack", out why, out amb);
        Check(ab, -1, "★★ ③ 歧义：`UniqueAbilityIndexForTrigger` 返回 **-1**（⛔ 不猜第几条）");
        CheckTrue(amb, "★★ ③ 歧义被**单独标出来**（`ambiguous = true`）"
                     + "｜🧨 把歧义并进「没落」那一档 ⇒ 这一位 false ⇒ 红");
        int warn3 = RuleCore.AbilityActingCriteriaWarningsForTest;
        bool queued4 = RuleCore.EnqueueAbility(ctx3, ActionPriority.Instant, "E3·歧义",
                                               () => { }, actor: aown,
                                               abilityTrigger: "UnitAttack", abilityTarget: afoe,
                                               abilityActingCard: afoe, abilityActingGate: true);
        CheckTrue(!queued4, "★★ ③ 歧义 ⇒ **不触发**（简报口径：定位不唯一就不猜、不出手）"
                          + "｜🧨 歧义时放行 ⇒ 红（那等于「随便挑一条」）");
        CheckTrue(RuleCore.AbilityActingCriteriaWarningsForTest > warn3,
                  "★★ ③ 歧义**出声**（第一道闸计数 +1）");

        // ---- 灭自证：`abilityActingGate = false` ⇒ 这道闸**不启用**（= 改前行为）----
        int warn4 = RuleCore.AbilityActingCriteriaWarningsForTest;
        bool queued5 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "E3·不启用",
                                               () => { }, actor: own,
                                               abilityTrigger: "UnitAttack", abilityTarget: null,
                                               abilityActingCard: mate, abilityActingGate: false);
        CheckTrue(queued5, "★★ ③ `abilityActingGate = false` ⇒ 第一道闸**不启用**（`actingCard` 是己方也照放行）"
                         + "｜🧨 闸无条件启用（不看那一格）⇒ 会把没查清 `actingCard` 的十几条路全筛掉 ⇒ 红");
        Check(RuleCore.AbilityActingCriteriaWarningsForTest, warn4,
              "★ ③ 不启用时**不出声**");

        // ---- 灭自证 ②：旧表（第一道闸那一列没落）⇒ 不筛 ----
        var oldCard = E3Synthetic("E3OldGate", "", new[] { 50 },
                                  new Dictionary<int, CardDef.CountSetSpec> { { 0, E2Criteria10(1, 0) } },
                                  actingVersioned: false);
        var ctx4 = ProbeBattle(new CardDef[0], new CardDef[0]);
        var oown = Place(ctx4, 0, 3, oldCard);
        var omate = Place(ctx4, 0, 4, Unit("E3OldMate", 1, 2, 5));
        bool queued6 = RuleCore.EnqueueAbility(ctx4, ActionPriority.Instant, "E3·旧表",
                                               () => { }, actor: oown,
                                               abilityTrigger: "UnitAttack", abilityTarget: null,
                                               abilityActingCard: omate, abilityActingGate: true);
        CheckTrue(queued6, "★★ ③ 旧表（列没落）⇒ 这道闸**不筛**（放行）—— 与改前逐字相同"
                         + "｜🧨 旧表也拿「空 criteria」当「不满足」⇒ 会把旧数据静默掐死 ⇒ 红");
    }

    // ==================================================================
    //  ④ 接线：`BroadcastWhen` → `FireBoardWhenListener` → 第一道闸
    //     （两条真广播：`Attack` 取攻击者、`Die` 取**凶手**）
    // ==================================================================

    /// <summary>
    /// 两条端到端（走**真广播**，不是直调 `EnqueueAbility`）：
    /// · `Attack`：监听者 = `When an enemy attacks, deal 2 damage to it`（与 `UM70`/`SAU31` 卡面逐字相同的
    ///   正文 ⇒ 解析路径已被那两张真卡验证过）+ 第一道闸 `ta=10 / enemyUnits=1`。
    ///   敌方攻击 ⇒ acting = 敌方 ⇒ 过闸 ⇒ 监听者真的结算（攻击者掉 2 血）；
    ///   己方攻击 ⇒ acting = 己方 ⇒ 被筛掉 ⇒ 不结算。
    /// · `Die`：监听者 = `When another troop dies, deal 1 damage to a random enemy`（`SAU68` 卡面同文）
    ///   + `ta=20 self`。acting = **监听者自己** ⇒ 过闸 ⇒ 结算；acting = 另一个友方 ⇒ 筛掉 ⇒ 不结算；
    ///   acting = **null** ⇒ **判不了 ⇒ 出声 + 照跑**（🧨 若有人把 `subject`（死者）顶成 acting，
    ///   这一档会变成「筛掉、不跑」⇒ 红）。
    /// </summary>
    static void E3Case4BroadcastWiring()
    {
        // ---------------- ④·a Attack：actingCard = 攻击者（subject）----------------
        // ⚠️ 两张夹具分开摆（负例那张的**正文极性必须能匹配**，否则「没结算」是**正文**筛的、不是闸筛的
        //    ⇒ 负例就变成假绿 —— 2026-10-25 本代理第一版就是这样写错的，就地改掉）：
        //    · listenerA：`When an enemy attacks, …` + `enemyUnits=1` ⇒ **敌方攻击**才进闸；
        //    · listenerB：`When a friendly unit attacks, …` + **同一个** `enemyUnits=1` ⇒
        //      **己方攻击**时正文匹配、**闸**不匹配 ⇒ 只有这一条能证明「闸真的在筛」。
        var listenerA = E3Synthetic("E3ListenA", "When an enemy attacks, deal 2 damage to it.",
                                    new[] { 50 },
                                    new Dictionary<int, CardDef.CountSetSpec> { { 0, E2Criteria10(1, 0) } });
        var listenerB = E3Synthetic("E3ListenB", "When a friendly unit attacks, deal 2 damage to it.",
                                    new[] { 50 },
                                    new Dictionary<int, CardDef.CountSetSpec> { { 0, E2Criteria10(1, 0) } });
        CheckTrue(HasWhenKind(listenerA, WhenEventKind.Attack),
                  "④a （前提）`When an enemy attacks, deal N damage to it` 解析出了 `Attack` 监听器"
                + "（判据 = `SAU31`/`UM70` 卡面同文；解析不出 ⇒ 后面几条全无意义）｜"
                + "🧨 `EffectText` 不再认这句 ⇒ 红");
        CheckTrue(HasWhenKind(listenerB, WhenEventKind.Attack),
                  "④a （前提）`When a friendly unit attacks, deal N damage to it` 解析出了 `Attack` 监听器"
                + "（判据 = `UM83`/`GSC12` 的 `When a friendly unit attacks` 那一族）｜"
                + "🧨 解析不出 ⇒ 负例那两条会**假绿**（正文没匹配 ≠ 闸筛掉了）⇒ 红");

        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            var me = Place(ctx, 0, 3, listenerA);
            var foeAtk = Place(ctx, 1, 1, Unit("E3FoeAtk", 1, 3, 8));
            int hp0 = foeAtk.Health;
            int warn = RuleCore.AbilityActingCriteriaWarningsForTest;
            // 🔴 真广播：who = 攻击者阵营（1）· subject/actor = 攻击者 · target = 监听者
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Attack, 1, foeAtk.Card,
                                         subject: foeAtk, actor: foeAtk, target: me);
            Check(foeAtk.Health, hp0 - 2,
                  "★★ ④a 正例：**敌方**攻击 ⇒ acting = 敌方攻击者 ⇒ 第一道闸通过 ⇒ 监听者真的结算（攻击者 -2 血）"
                + "｜🧨 `AbilityActingGateCard(Attack)` 接成 `target`（被打者 = 监听者自己，己方）⇒ 闸失败 ⇒ 血不变 ⇒ 红");
            Check(RuleCore.AbilityActingCriteriaWarningsForTest, warn,
                  "★ ④a 正例**不出声**");
        }

        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            var meB = Place(ctx, 0, 3, listenerB);
            var mateAtk = Place(ctx, 0, 4, Unit("E3MateAtk", 1, 3, 8));
            var foe = Place(ctx, 1, 1, Unit("E3FoeTgt", 1, 2, 8));
            int hp0 = mateAtk.Health;
            int warn = RuleCore.AbilityActingCriteriaWarningsForTest;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Attack, 0, mateAtk.Card,
                                         subject: mateAtk, actor: mateAtk, target: foe);
            Check(mateAtk.Health, hp0,
                  "★★ ④a 负例：**己方**攻击（**正文匹配** `When a friendly unit attacks`）⇒ acting = 己方 ⇒ 被"
                + " `enemyUnits = 1` 筛掉 ⇒ 监听者**不结算**（攻击者不掉血）"
                + "｜🧨 闸没挂在 Attack 那一跳 / 候选接成 `thisCard` 之外的格子 ⇒ 掉 2 血 ⇒ 红");
            CheckTrue(RuleCore.AbilityActingCriteriaWarningsForTest > warn,
                      "★★ ④a 负例**出声**（反静默；这一条同时证明「没结算」是**闸**造成的，不是正文没匹配）");
        }

        // ---------------- ④·b Die：actingCard = **凶手**（不是死者）----------------
        var listenerD = E3Synthetic("E3ListenD", "When another troop dies, deal 1 damage to a random enemy",
                                    new[] { 90 },
                                    new Dictionary<int, CardDef.CountSetSpec> { { 0, E3CriteriaSelf() } });
        CheckTrue(HasWhenKind(listenerD, WhenEventKind.Die),
                  "④b （前提）合成卡正文解析出了 `Die` 监听器（判据 = `SAU68` 卡面同文）｜"
                + "🧨 解析不出 ⇒ 下面三条全无意义 ⇒ 红");

        {
            // 正例：acting = 监听者自己（「是我杀的」）⇒ ta=20 self 通过 ⇒ 结算
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            var me = Place(ctx, 0, 3, listenerD);
            var mate = Place(ctx, 0, 4, Unit("E3DMate", 1, 2, 8));
            var foeU = Place(ctx, 1, 1, Unit("E3DFoe", 1, 2, 8));
            var dead = new UnitState(ctx.NewInstance(Unit("E3Dead1", 1, 2, 5)), false);
            int sum0 = SumHealth(ctx, 1);
            int warn = RuleCore.AbilityActingCriteriaWarningsForTest;
            // who = 死者阵营（0）· subject = 死者 · actingCard = 凶手（监听者自己）
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Die, 0, dead.Card,
                                         subject: dead, actingCard: me);
            Check(SumHealth(ctx, 1), sum0 - 1,
                  "★★ ④b 正例：acting = **监听者自己** ⇒ `ta=20 self` 通过 ⇒ 监听者真的结算（随机敌人 -1 血）");
            Check(RuleCore.AbilityActingCriteriaWarningsForTest, warn,
                  "★ ④b 正例**不出声**");
        }

        {
            // 负例：acting = 另一个友方 ⇒ ta=20 self 不通过 ⇒ 不结算
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            var me = Place(ctx, 0, 3, listenerD);
            var mate = Place(ctx, 0, 4, Unit("E3DMate2", 1, 2, 8));
            var foeU = Place(ctx, 1, 1, Unit("E3DFoe2", 1, 2, 8));
            var dead = new UnitState(ctx.NewInstance(Unit("E3Dead2", 1, 2, 5)), false);
            int sum0 = SumHealth(ctx, 1);
            int warn = RuleCore.AbilityActingCriteriaWarningsForTest;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Die, 0, dead.Card,
                                         subject: dead, actingCard: mate);
            Check(SumHealth(ctx, 1), sum0,
                  "★★ ④b 负例：acting = 另一个友方 ⇒ `ta=20 self` 不通过 ⇒ 监听者**不结算**"
                + "｜🧨 `Die` 那一格接成 `subject`（死者）⇒ acting ≠ 监听者 ⇒ 与负例同结果，抓不到 —— 见下一条");
            CheckTrue(RuleCore.AbilityActingCriteriaWarningsForTest > warn, "★★ ④b 负例**出声**");
        }

        {
            // 🔴 灭自证：acting = **null**（没有来源的死）⇒ 判不了 ⇒ **出声 + 照跑**
            //    🧨 若把 `subject`（死者）顶成 actingCard ⇒ 这一档会「筛掉、不跑」⇒ 血不变 ⇒ 红。
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            var me = Place(ctx, 0, 3, listenerD);
            var mate = Place(ctx, 0, 4, Unit("E3DMate3", 1, 2, 8));
            var foeU = Place(ctx, 1, 1, Unit("E3DFoe3", 1, 2, 8));
            var dead = new UnitState(ctx.NewInstance(Unit("E3Dead3", 1, 2, 5)), false);
            int sum0 = SumHealth(ctx, 1);
            int warn = RuleCore.AbilityActingCriteriaWarningsForTest;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Die, 0, dead.Card,
                                         subject: dead, actingCard: null);
            Check(SumHealth(ctx, 1), sum0 - 1,
                  "★★ ④b 灭自证：拿不到 `actingCard` ⇒ **判不了 ⇒ 放行**（监听者照跑）"
                + "｜🧨 拿 `subject`（死者）顶 ⇒ `ta=20 self` 判成 false ⇒ 不跑 ⇒ 红");
            CheckTrue(RuleCore.AbilityActingCriteriaWarningsForTest > warn,
                      "★★ ④b 灭自证：这一档**必须出声**（⛔ 静默放行更糟）");
        }
    }

    /// <summary>这张卡的监听器里有没有某一类事件（夹具前提用）。</summary>
    static bool HasWhenKind(CardDef card, string kind)
    {
        if (card == null) return false;
        foreach (var t in card.WhenTriggers)
            if (t.Ev != null && t.Ev.Kind == kind) return true;
        return false;
    }
}
