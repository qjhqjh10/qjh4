// RuleEngineTest_EngineE2.cs — 引擎数据管线 `E2`（2026-10-25 · 第七十五会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineA3` /
// `_EngineA2` / `_EngineC5` … 都在），同一时刻只有一个写手改一个文件 ⇒ 本批测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestE2AbilityLevelCriteria)`**。
//
// 判据（⛔ 不要重新论证；全文 → `资料/普查产出_1025/A3_引擎_at-turn与criteria管线.md` §A3-2）：
//   · ability 级 criteria = 原版 **`CardAbility.targetCriteria`（`+0x30`）** —— 🔴 与 v25 已落的
//     `AbilityLogic.targetCriteria`（`+0x28`，logic 级）是**两个同名对象、两层语义**。
//   · 消费点 = `CardAbility__CanTriggerAbility.c:131-135`：
//       `CheckIfMeetsCriteria(thisCard, targetCard, targetCard, *(ability+0x30), 0)`
//     形参表 `d:/2/tools/il2cpp_out/dump.cs:20360` ⇒ **被筛对象 = `targetCard`**
//     （⛔ 不是 `actingCard` —— 那是第一道闸 `+0x28` 的候选）。**这一条与简报不同，见报告 §判据订正**。
//   · `targetsAffected == 0 (any)` ⇒ 原版 `return 1` ⇒ 生成器**不落那一行**（零信息）。
//
// 🔴 每条断言都带 `🧨`（什么改动能让它红）。期望值一律写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestE2AbilityLevelCriteria)`）。</summary>
    public static void TestE2AbilityLevelCriteria()
    {
        Section("🆕 引擎线 E2：ability 级 `targetCriteria`（`CardAbility.targetCriteria` `+0x30`）"
              + " —— 数据落位 · 唯一入口 · 触发前闸");
        E2Case1DataPipelineRealPool();
        E2Case2UniqueEntryAndUnknown();
        E2Case3GatePositiveNegativeAmbiguous();
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>一条**所有格都显式给值**的 `TargetCriteria`（`ta = 10 allFiltered`）。
    /// ⚠️ 必须逐格给：`CountSetSpec` 的缺省是 `Unknown(-1)`，`TargetCriteriaEval` 遇 `Unknown`
    /// 返回 `null`（判不了）⇒ 夹具会静默变成「没验到」（同 `_EngineA3` 的注释）。</summary>
    static CardDef.CountSetSpec E2Criteria10(int enemyUnits, int friendlyUnits)
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = 0,
            TargetsAffected = 10,
            ChosenArmy = 0, ArmyFilter = 0,
            KeywordFilter = 0,
            TraitsFilter = 0,
            MaxTargets = 1,
            TargetSelectMethod = 0,
            AttackFilter = 0, AttackFilterOption = 0,
            CurrentHealthFilter = 0, CurrentHealthFilterOption = 0,
            ManaCostFilter = 0, ManaCostFilterOption = 0,
            RarityFilter = 0, RarityFilterOption = 0,
            SpellType = 0,
            FriendlyUnits = friendlyUnits,
            EnemyUnits = enemyUnits,
            IncludeHero = 1, IncludeMinions = 1,
            IncludeSpells = 0, IncludeWhispers = 0, IncludeWeapons = 0,
            IncludeRemnants = 1, IncludeHeroParts = 0,
            IncludeSelf = 0, ExcludeNotSelf = 0, ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0,
            ExcludeRace = 0, ExcludeTrait = 0,
            SelectFromChooseResolution = 0,
        };
    }

    /// <summary>造一张合成卡：`trigger` 数组 = 每条 ability 的 trigger（下标 = `cardAbilities` 下标）。
    /// `criteriaAt` = 只给**某几条** ability 落 36 格（其余不落 = 明确「没有触发条件」）。
    /// ⚠️ `abilityTriggersVersioned: true` 是**两列共用**的版本位（v17；见 `A3-3`）——
    /// 不给 ⇒ `UniqueAbilityIndexForTrigger` 一路 `-1`（夹具会静默变成「什么都没验到」）。</summary>
    static CardDef E2Synthetic(string id, int[] triggers,
                              Dictionary<int, CardDef.CountSetSpec> criteriaAt,
                              bool levelCriteriaVersioned = true)
    {
        var ta = new List<CardDef.AbilityTargetSpec>();
        for (int i = 0; i < triggers.Length; i++)
            ta.Add(new CardDef.AbilityTargetSpec { Trigger = triggers[i], TargetsAffected = new[] { 10 } });
        var rows = new List<CardDef.TargetCriteriaRowSpec>();
        if (criteriaAt != null)
            foreach (var kv in criteriaAt)
                rows.Add(new CardDef.TargetCriteriaRowSpec
                {
                    Ability = kv.Key, Trigger = triggers[kv.Key], Logic = -1, Criteria = kv.Value,
                });
        return new CardDef(id, id, "unit", "Whenever a friendly unit attacks, gain 1 life.", "common", "Test",
                           1, 1, 4, 0, null,
                           subtype: "Infantry",
                           abilityTriggersVersioned: true,
                           abilityTargets: ta.ToArray(),
                           // 🔴🆕 **2026-10-25（执行代理 `F3` · 铁律 5·b）：`conditionsVersioned: true` 必须给。**
                           //   `E9` 把第 ⑧ 格（`unitConditions` 的 `role="target"/"acting"` 行）接进
                           //   `TargetCriteriaEval.Matches` 之后，`ConditionsVersioned == false`（构造缺省）
                           //   会被 `ActionPump.AbilityLevelConditionRow` 判成「旧表 ⇒ 判不了」的哨兵
                           //   （`Condition = ConditionUnknown` = `-1`）⇒ 每到第 ⑧ 步 `Matches` 回 `null`
                           //   ⇒ `EnqueueAbility` 走「判不了 ⇒ 放行 + 出声」⇒ **正例也涨计数**。
                           //   真卡池（v27）走 `CardDatabase.Parse` 的 `dto.version >= 15` ⇒ 恒 `true`，
                           //   夹具必须同形（同一口径的先例 = `RuleEngineTest.cs:28116` 那句「必须给」）。
                           //   ⚠️ 这不是把断言改松：断言一个字没动，改的是夹具「像不像真卡」。
                           conditionsVersioned: true,
                           abilityLevelCriteria: levelCriteriaVersioned ? rows.ToArray() : null,
                           abilityLevelCriteriaVersioned: levelCriteriaVersioned);
    }

    // ==================================================================
    //  ① 数据管线：真卡池里那 85 行**真的读得到**，且每一格都对得上生成器
    // ==================================================================

    /// <summary>
    /// `SW16 Long Fang`（池内唯一 `When this unit attacks …` 那条 40 context 的卡）现读的
    /// ability 级 criteria = `ta=10 · enemyUnits=1 · includeMinions=1 · traitsFilter=1260`。
    /// ① 列在（`AbilityLevelCriteriaVersioned`）；
    /// ② **按 trigger 名唯一定位**到第 0 条 ability；
    /// ③ 取回来的 36 格逐格对得上；④ 行为：**敌方**候选过、**己方**候选被筛掉。
    /// </summary>
    static void E2Case1DataPipelineRealPool()
    {
        var pool = CardDatabase.Load();
        var card = CardDatabase.FindById(pool, "SW16");
        CheckTrue(card != null, "① （前提）卡池里有 `SW16 Long Fang`");
        if (card == null) return;

        CheckTrue(card.AbilityLevelCriteriaVersioned,
                  "★★ ① `abilityLevelCriteria` 列在（`version >= 26`）"
                + "｜🧨 生成器不落这一列 / `CardDatabase` 不传版本位 ⇒ 访问器一路 null ⇒ 红");
        CheckTrue(card.AbilityLevelCriteriaRowCount > 0,
                  "★ ① `SW16` 有 ability 级 criteria 行（现读 1 行）");

        string why;
        int ab = card.UniqueAbilityIndexForTrigger("UnitAttack", out why);
        Check(ab, 0, "★★ ① `SW16` 的 `UnitAttack` 定位到**第 0 条** ability"
                    + $"（why = {why ?? "（无）"}）"
                    + "｜🧨 `abilityTargets` 的下标语义被改（不再是 `cardAbilities` 下标）⇒ 红");

        var set = card.AbilityLevelCriteriaForTrigger("UnitAttack", out why);
        CheckTrue(set != null, "★★ ① `SW16` 取得到 `UnitAttack` 那条 ability 的 36 格"
                             + $"（why = {why ?? "（无）"}）"
                             + "｜🧨 生成器只落 logic 级那一列（`abilityTargetCriteria`）⇒ 这里 null ⇒ 红");
        if (set == null) return;
        Check(set.TargetsAffected, 10, "★ ① `ta = 10 allFiltered`");
        Check(set.EnemyUnits, 1, "★★ ① `enemyUnits = 1`（只对**敌方**那个被打的目标生效）");
        Check(set.FriendlyUnits, 0, "★ ① `friendlyUnits = 0`");
        Check(set.IncludeMinions, 1, "★ ① `includeMinions = 1`");
        Check(set.TraitsFilter, 1260, "★★ ① `traitsFilter = 1260`（🕷 那个特质）—— **其余 35 格**就在这一格里，"
                                    + "logic 级那一列一个字都没有");

        // ---- 行为探针：**用取回来的那个对象**，不是手搓 set ----
        // 🔴 2026-10-25（执行代理 `F1` · 铁律 5 就地订正）：这里原来摆的是一个**不带猎杀标记**的敌人、
        //   却断言「放行」。而 `SW16` 这张卡的 ability 级 criteria 里有 `traitsFilter = 1260 (huntMark)`
        //   —— 卡面原文 `When this unit attacks an enemy with Hunt Mark, deal 3 damage to adjacent enemies`
        //   ⇒ 这一格要求**被打的那个敌人带猎杀标记**（原版 `FilterMethods__MeetsTraitCriteria.c`：
        //   `excludeTrait == 0 ⇒ 必须 has`）⇒ 原来的夹具与它自己的数据自相矛盾（那条红是**夹具错**）。
        //   ⇒ 现在三档：带标记的敌人**放行** · 不带标记的敌人**淘汰**（后者才是「这一格真有数据」的判别式）
        //   · 己方候选**淘汰**。
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var owner = Place(ctx, 0, 3, Unit("E2Owner", 1, 2, 5));
        var foe = Place(ctx, 1, 1, Unit("E2Foe", 1, 2, 5, "Hunt Mark"));
        var plain = Place(ctx, 1, 5, Unit("E2Plain", 1, 2, 5));
        CheckTrue(foe.Has("huntmark"),
                  "① （前提）夹具那个敌人**真的带猎杀标记**（`Hunt Mark` → `KeywordTable.Parse` = `huntmark`）"
                + "｜🧨 卡面写法改成 `HuntMark`（不带空格）⇒ `Prefixes` 只有 `hunt mark` 那一条 ⇒ 解析不出 ⇒ 本行红");
        string gap;
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, set, foe, null, owner, foe, out gap) == true,
                  "★★ ① 敌方候选（**带**猎杀标记）**放行** —— `ta=10 / enemyUnits=1 / traitsFilter=1260` 都满足"
                + "｜这是「有数据」的判别式");
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, set, plain, null, owner, plain, out gap) == false,
                  "★★ ① 同一个敌方、**不带**猎杀标记 ⇒ **淘汰**（`traitsFilter = 1260`）"
                + "｜🧨 `TraitNameOf(1260)` 给的名字过不了 `KeywordTable.Normalize`（camelCase `huntMark` "
                + "对不上前缀表里那条 `hunt mark`）⇒ 这一格返回 `null`（**判不了**）⇒ 本行红 —— "
                + "而「判不了」在消费端 = **整条 criteria 静默不筛**（`ApplyTargetCriteriaFilter` 的 null 支）");
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, set, owner, null, owner, owner, out gap) == false,
                  "★★ ① 己方候选**被筛掉**（`friendlyUnits = 0`）"
                + "｜🧨 数据没落（`set == null` ⇒ `Matches` 恒 true）⇒ 这一格红（灭自证）");
    }

    // ==================================================================
    //  ② 唯一入口：取不到时**明确是空的哪一档**，⛔ 不是默认值
    // ==================================================================

    static void E2Case2UniqueEntryAndUnknown()
    {
        // ---- ②·a 旧表（列没落）：`null` + why 说「没落」，⛔ 不是「没有条件」 ----
        {
            var c = E2Synthetic("E2Old", new[] { 50 }, null, levelCriteriaVersioned: false);
            string why;
            CheckTrue(c.AbilityLevelCriteriaAt(0) == null,
                      "★ ②a 旧表（版本位 false）⇒ `AbilityLevelCriteriaAt(0)` 是 **null**（⛔ 不是「默认 criteria」）");
            var set = c.AbilityLevelCriteriaForTrigger("UnitAttack", out why);
            CheckTrue(set == null, "★ ②a 旧表 ⇒ `AbilityLevelCriteriaForTrigger` 返回 **null**");
            CheckTrue(why != null && why.Contains("没落"),
                      "★★ ②a 旧表 ⇒ `why` 明确说「列没落」（这一档消费端必须**放行并出声**，"
                    + "⛔ 不许当「没有条件」静默放行）｜🧨 版本位判定被吞（恒 true）⇒ why 变 null ⇒ 红");
        }

        // ---- ②·b 新版但这条 ability 没有行 ⇒ `null` + why == null（= **明确没有条件**）----
        {
            // 两条 ability（50 / 90），只给第 1 条（90）落 36 格
            var c = E2Synthetic("E2Partial", new[] { 50, 90 },
                                new Dictionary<int, CardDef.CountSetSpec> { { 1, E2Criteria10(1, 0) } });
            string why;
            var set = c.AbilityLevelCriteriaForTrigger("UnitAttack", out why);
            CheckTrue(set == null && why == null,
                      "★★ ②b 第 0 条 ability（`UnitAttack`）**没有那一行** ⇒ 返回 null 且 `why == null`"
                    + "（= 明确「没有触发条件」；这一档消费端**放行、不出声**）"
                    + "｜🧨 把「没落」与「没有这一行」混成一档 ⇒ `why` 非空 ⇒ 红");
            var set90 = c.AbilityLevelCriteriaForTrigger("OtherUnitDeath", out why);
            CheckTrue(set90 != null, "★ ②b 第 1 条 ability（`OtherUnitDeath`）**取得到**（同一张卡的两条互不串行）"
                                   + $"｜🧨 按 trigger 名定位时串到第 0 条 ⇒ 红（why = {why ?? "（无）"}）");
        }

        // ---- ②·c 认不出的 trigger 名 ⇒ `null` + why（判不了，⛔ 不是「没有」）----
        {
            var c = E2Synthetic("E2BadName", new[] { 50 },
                                new Dictionary<int, CardDef.CountSetSpec> { { 0, E2Criteria10(1, 0) } });
            string why;
            var set = c.AbilityLevelCriteriaForTrigger("NotARealTrigger", out why);
            CheckTrue(set == null && why != null && why.Contains("认不出"),
                      "★★ ②c 认不出的 trigger 名 ⇒ `null` + `why` 说「认不出」"
                    + "｜🧨 退回 `ThisCardPlayed(0)` 之类的默认值 ⇒ 会**静默筛错** ⇒ 红");
        }

        // ---- ②·d 取不到不许落成「默认值」：`AbilityLevelCriteriaAt(-1)` 恒 null ----
        {
            var c = E2Synthetic("E2Neg", new[] { 50 },
                                new Dictionary<int, CardDef.CountSetSpec> { { 0, E2Criteria10(1, 0) } });
            CheckTrue(c.AbilityLevelCriteriaAt(-1) == null,
                      "★ ②d `AbilityLevelCriteriaAt(-1)`（active/alternative 那一路）**恒 null** —— "
                    + "那一层是 `ActiveAbility.targetCriteria`（另一个类），本列不收");
        }
    }

    // ==================================================================
    //  ③ 触发前闸：正例 / 负例 / 歧义例（⚠️ 被筛对象 = `targetCard`）
    // ==================================================================

    /// <summary>
    /// 三档端到端（走**生产入口** `RuleCore.EnqueueAbility`）：
    /// · 正例 = `targetCard` 是敌方 ⇒ 满足 ⇒ **触发**（`EnqueueAbility` 返回 true）；
    /// · 负例 = `targetCard` 是己方 ⇒ 不满足 ⇒ **不触发** + 出声（计数 +1）；
    /// · 歧义例 = 这张卡有 **2 条** 同 trigger 的 ability ⇒ **不触发** + 出声（计数 +1）。
    /// 另配「没有 targetCard ⇒ 判不了 ⇒ 放行 + 出声」——⛔ 不许把「不知道」当「不满足」。
    /// </summary>
    static void E2Case3GatePositiveNegativeAmbiguous()
    {
        // 夹具：合成卡直接当 `Place` 的 `CardDef` 用 ⇒ 场上那个 `UnitState.Card` 就是它
        //（`RuleCore.EnqueueAbility` 的闸读 `actor.Card`）。
        var gateCard = E2Synthetic("E2Gate", new[] { 50 },
                                   new Dictionary<int, CardDef.CountSetSpec> { { 0, E2Criteria10(1, 0) } });

        var ctx2 = ProbeBattle(new CardDef[0], new CardDef[0]);
        var own = Place(ctx2, 0, 3, gateCard);
        var foe = Place(ctx2, 1, 1, Unit("E2GateFoe2", 1, 2, 5));
        CheckTrue(own != null && own.Card == gateCard,
                  "③ （前提）合成卡真的挂到了场上那个 `UnitState` 上");

        int warn0 = RuleCore.AbilityLevelCriteriaWarningsForTest;

        // ---- 正例：候选 = 敌方 ⇒ 满足 ⇒ **触发** ----
        bool queued = RuleCore.EnqueueAbility(ctx2, ActionPriority.Instant, "E2·正例",
                                              () => { }, actor: own,
                                              abilityTrigger: "UnitAttack", abilityTarget: foe);
        CheckTrue(queued, "★★ ③ 正例：`targetCard` 是**敌方** ⇒ ability 级 criteria 满足 ⇒ **触发**（入队成功）"
                        + "｜🧨 把候选接成 `actingCard`（= 载体自己，己方）⇒ 这一格会**红**（判成不满足）");
        Check(RuleCore.AbilityLevelCriteriaWarningsForTest, warn0,
              "★ ③ 正例**不出声**（计数不变）");

        // ---- 负例：候选 = 己方 ⇒ 不满足 ⇒ **不触发** + 出声 ----
        int warn1 = RuleCore.AbilityLevelCriteriaWarningsForTest;
        bool queued2 = RuleCore.EnqueueAbility(ctx2, ActionPriority.Instant, "E2·负例",
                                               () => { }, actor: own,
                                               abilityTrigger: "UnitAttack", abilityTarget: own);
        CheckTrue(!queued2, "★★ ③ 负例：`targetCard` 是**己方** ⇒ 不满足 ⇒ **不触发**（入队被拒）"
                          + "｜🧨 门槛只写不判（永远放行）⇒ 红");
        CheckTrue(RuleCore.AbilityLevelCriteriaWarningsForTest > warn1,
                  "★★ ③ 负例**出声**（计数 +1）—— 反静默的唯一判别式"
                + "｜🧨 只 return false 不打日志 ⇒ 红");

        // ---- 判不了那一档：没有 targetCard ⇒ 放行 + 出声 ----
        int warn2 = RuleCore.AbilityLevelCriteriaWarningsForTest;
        bool queued3 = RuleCore.EnqueueAbility(ctx2, ActionPriority.Instant, "E2·无目标",
                                               () => { }, actor: own,
                                               abilityTrigger: "UnitAttack", abilityTarget: null);
        CheckTrue(queued3, "★★ ③ 没有 `targetCard` ⇒ **判不了 ⇒ 放行**（⛔ 不许当「不满足」把触发掐死）"
                         + "｜🧨 把 null 候选当 false ⇒ 会**静默掐死**一整个族 ⇒ 红");
        CheckTrue(RuleCore.AbilityLevelCriteriaWarningsForTest > warn2,
                  "★ ③ 「判不了」也要**出声**（计数 +1）");

        // ---- 歧义例：2 条同 trigger 的 ability ⇒ **不触发** + 出声 ----
        var ambCard = E2Synthetic("E2Amb", new[] { 50, 50 },
                                  new Dictionary<int, CardDef.CountSetSpec>
                                  {
                                      { 0, E2Criteria10(1, 0) },
                                      { 1, E2Criteria10(1, 0) },
                                  });
        var ctx3 = ProbeBattle(new CardDef[0], new CardDef[0]);
        var aown = Place(ctx3, 0, 3, ambCard);
        var afoe = Place(ctx3, 1, 1, Unit("E2AmbFoe", 1, 2, 5));
        string why;
        bool amb;
        int ab = ambCard.UniqueAbilityIndexForTrigger("UnitAttack", out why, out amb);
        Check(ab, -1, "★★ ③ 歧义：`UniqueAbilityIndexForTrigger` 返回 **-1**（⛔ 不猜第几条）");
        CheckTrue(amb, "★★ ③ 歧义被**单独标出来**（`ambiguous = true`）"
                     + "｜🧨 把歧义并进「没落」那一档 ⇒ 这一位 false ⇒ 红");
        int warn3 = RuleCore.AbilityLevelCriteriaWarningsForTest;
        bool queued4 = RuleCore.EnqueueAbility(ctx3, ActionPriority.Instant, "E2·歧义",
                                               () => { }, actor: aown,
                                               abilityTrigger: "UnitAttack", abilityTarget: afoe);
        CheckTrue(!queued4, "★★ ③ 歧义 ⇒ **不触发**（简报口径：定位不唯一就不猜、不出手）"
                          + "｜🧨 歧义时放行 ⇒ 红（那等于「随便挑一条」）");
        CheckTrue(RuleCore.AbilityLevelCriteriaWarningsForTest > warn3,
                  "★★ ③ 歧义**出声**（计数 +1）");

        // ---- 灭自证：把版本位关掉 ⇒ 整道闸**不启用**（= 改前行为）----
        var oldCard = E2Synthetic("E2OldGate", new[] { 50 },
                                  new Dictionary<int, CardDef.CountSetSpec> { { 0, E2Criteria10(1, 0) } },
                                  levelCriteriaVersioned: false);
        var ctx4 = ProbeBattle(new CardDef[0], new CardDef[0]);
        var oown = Place(ctx4, 0, 3, oldCard);
        var ofoe = Place(ctx4, 1, 1, Unit("E2OldFoe", 1, 2, 5));
        bool queued5 = RuleCore.EnqueueAbility(ctx4, ActionPriority.Instant, "E2·旧表",
                                               () => { }, actor: oown,
                                               abilityTrigger: "UnitAttack", abilityTarget: oown);
        CheckTrue(queued5, "★★ ③ 旧表（列没落）⇒ 这道闸**不筛**（放行）—— 与改前逐字相同"
                         + "｜🧨 旧表也拿「空 criteria」当「不满足」⇒ 会把整池旧数据静默掐死 ⇒ 红");
    }
}
