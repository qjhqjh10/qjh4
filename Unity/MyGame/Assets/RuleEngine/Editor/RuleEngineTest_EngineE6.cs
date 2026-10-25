// RuleEngineTest_EngineE6.cs — 引擎线 `E6`（2026-10-25 · 第七十五会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineE2` /
// `_EngineE3` / `_EngineE4` / `_EngineE5` … 都在），同一时刻只有一个写手改一个文件 ⇒
// 本批测试体全写在这里，`RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestE6AbilityLevelGateFamilies)`**。
//
// 本件接的是 `E5-5` 评估表里「判据齐、只差接闸」的 **13 行**（全文 →
// `资料/普查产出_1025/E5_引擎剩余小缺口.md` §五；本批报告 → `资料/普查产出_1025/E6_十三行接闸.md`）：
//   · `510 UnitReceivesMarkOfChaos` **4 行**（`BL12` `BL3` `BL69` `BL28`）·
//     `570 Reanimated` **5 行**（`SAU3` `SAU4` `SAU71` `SAU22` `SAU37`）·
//     `515 UnitReceivesMarkerlight` **1 行**（`TAU15`）·
//     `60 UnitDamaged` **1 行**（`BL8`，**只接 target 闸**）·
//     `291 UnitStunned` **2 行**（`ASH75` `ASH74`，**只接 target 闸**）。
//
// 判据（第一权威 = 全量反编译，逐句；⛔ 不要重新论证）：
//   · 第二道闸（`+0x30`，筛 `targetCard`）：`CardAbility__CanTriggerAbility.c:131-135`
//     `CheckIfMeetsCriteria(thisCard, targetCard, targetCard, *(ability+0x30), 0)`；
//     形参表 `dump.cs:23487` = `OnTrigger(trigger, manager, thisCard, actingCard, targetCard, …)`。
//   · `ReactToUnitMarked.c:7`（510）· `ReactToUnitReanimated.c:7`（570）·
//     `ReactToUnitMarkerlight.c:7`（515）三处都是 `(…, actingCard=param_2, targetCard=param_2)`
//     且广播器传 `(主体, 主体)` ⇒ **acting = target = 被标记 / 被复活者 = 我们的 `subject`**。
//   · `ReactToUnitStunned.c:7`（291）= `(…, actingCard=**param_3**, targetCard=**param_2**)`
//     ⇒ **只有 targetCard（被晕者）能接**；`actingCard`（施害者）我们那两跳广播里**没带**。
//   · `ResolveDamageDealt` 的 `0x3c`（60）同理：`targetCard = param_2 = 被打者 = subject`；
//     `actingCard = *(DamageInfo+0x10) = 伤害来源`，`EmitHit` 没有这一格 ⇒ 不接。
//
// 🔴 每条断言都带 `🧨`（什么改动能让它红）。期望值一律写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestE6AbilityLevelGateFamilies)`）。</summary>
    public static void TestE6AbilityLevelGateFamilies()
    {
        Section("🆕 引擎线 E6：`abilityLevelCriteria` 再补五族（510 / 570 / 515 / 60 / 291）"
              + " —— 13 行接闸 ＋ 两条 trigger 名映射 ＋ `When your Warlord …` 的主语解析");
        E6Case1DataRows();
        E6Case2TriggerNameMappings();
        E6Case3Gates();
        E6Case4WarlordSubjectParse();
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>满格 36 格（缺一维 = `Unknown` ⇒ `Matches` 回「判不了」⇒ 断言会静默变成没验到）。
    /// 形状照 `_EngineE2.E2Criteria10`（唯一判据），只把 `enemy/friendly` 参数化。</summary>
    static CardDef.CountSetSpec E6Criteria(int enemyUnits, int friendlyUnits)
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = 0, TargetsAffected = 10,
            ChosenArmy = 0, ArmyFilter = 0, KeywordFilter = 0, TraitsFilter = 0,
            MaxTargets = 1, TargetSelectMethod = 0,
            AttackFilter = 0, AttackFilterOption = 0,
            CurrentHealthFilter = 0, CurrentHealthFilterOption = 0,
            ManaCostFilter = 0, ManaCostFilterOption = 0,
            RarityFilter = 0, RarityFilterOption = 0, SpellType = 0,
            FriendlyUnits = friendlyUnits, EnemyUnits = enemyUnits,
            IncludeHero = 1, IncludeMinions = 1,
            IncludeSpells = 0, IncludeWhispers = 0, IncludeWeapons = 0,
            IncludeRemnants = 1, IncludeHeroParts = 0,
            IncludeSelf = 0, ExcludeNotSelf = 0, ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0, ExcludeRace = 0, ExcludeTrait = 0,
            SelectFromChooseResolution = 0,
        };
    }

    /// <summary>合成监听者：`desc` 决定 `WhenTriggers`（正文解析），`trigger` 是**原版 trigger 号**。
    /// <para>🔴 `abilityTriggers` 与 `abilityTargets` **两列都要给**：前者是 `EmitCardTrigger` 那道
    /// 「有没有这条 ability」闸的真值（`AbilityTriggerPresence`），后者是 ability 定位的索引 ——
    /// 缺前者 ⇒ `presence = 0` ⇒ `EvtKind.Trigger` **不发**（映射那几条断言会假红）。</para>
    /// <para><paramref name="levelVersioned"/> `false` = 模拟「旧表 / 这一列没落」⇒ 闸**判不了** ⇒
    /// 放行 + 出声（本件每一族的**灭自证**都用它）。</para></summary>
    static CardDef E6Listener(string id, string desc, int trigger,
                              CardDef.CountSetSpec level, CardDef.CountSetSpec acting = null,
                              bool levelVersioned = true)
    {
        var ta = new List<CardDef.AbilityTargetSpec>
        {
            new CardDef.AbilityTargetSpec { Trigger = trigger, TargetsAffected = new[] { 10 } },
        };
        var lrows = new List<CardDef.TargetCriteriaRowSpec>();
        if (level != null)
            lrows.Add(new CardDef.TargetCriteriaRowSpec
            { Ability = 0, Trigger = trigger, Logic = -1, Criteria = level });
        var arows = new List<CardDef.TargetCriteriaRowSpec>();
        if (acting != null)
            arows.Add(new CardDef.TargetCriteriaRowSpec
            { Ability = 0, Trigger = trigger, Logic = -1, Criteria = acting });
        return new CardDef(id, id, "unit", desc, "common", "Test", 1, 1, 5, 0, null,
                           subtype: "Infantry",
                           abilityTriggers: new[] { trigger },
                           abilityTriggersVersioned: true,
                           abilityTargets: ta.ToArray(),
                           // 🔴🆕 **2026-10-25（执行代理 `F3` · 铁律 5·b）：`conditionsVersioned: true` 必须给。**
                           //   理由与 `_EngineE2.E2Synthetic` 同一处（那里有完整判据）：
                           //   `E9` 接上第 ⑧ 格之后，缺省 `false` = 「旧表 ⇒ 判不了」哨兵
                           //   （`cond = ConditionUnknown` = -1）⇒ **正例**在 `verdict == null` 那一支
                           //   出声（`AbilityLevelCriteriaWarningsForTest` / `...Acting...` 各 +1）
                           //   ⇒ 本件 ③a～③e 的「正例不出声」六条全红。
                           //   真卡池 v27 ⇒ `ConditionsVersioned` 恒 true ⇒ 夹具同形（断言一个字没动）。
                           conditionsVersioned: true,
                           abilityLevelCriteria: lrows.ToArray(),
                           abilityLevelCriteriaVersioned: levelVersioned,
                           abilityActingCriteria: arows.ToArray(),
                           abilityActingCriteriaVersioned: true);
    }

    static int E6CountTriggerEvents(BattleContext ctx, string keyword)
    {
        int n = 0;
        foreach (var e in ctx.Signals)
            if (e.Kind == EvtKind.Trigger && e.Keyword == keyword) n++;
        return n;
    }

    // ==================================================================
    //  ① 数据层：13 行逐条读得到 + 覆盖面读数
    // ==================================================================

    static void E6Case1DataRows()
    {
        var pool = CardDatabase.Load();

        // ①·a 两张 `ta=20 self` 的（「这件事必须发生在我自己身上」）—— 510 与 570 各一张
        var bl12 = CardDatabase.FindById(pool, "BL12");
        CheckTrue(bl12 != null, "① （前提）卡池里有 `BL12 Aspiring Champion`");
        if (bl12 != null)
        {
            string why; bool amb;
            var set = bl12.AbilityLevelCriteriaForTrigger("UnitReceivesMarkOfChaos", out why, out amb);
            CheckTrue(set != null,
                      "★★ ① `BL12` 取得到 `UnitReceivesMarkOfChaos(510)` 那条 ability 的 36 格"
                    + $"（why = {why ?? "（无）"}）"
                    + "｜🧨 `TriggerNameOfWhenKind` 的 `GetsDarkPact` 支被删 / 数据列不落 ⇒ null ⇒ 红");
            if (set != null)
                Check(set.TargetsAffected, 20,
                      "★★ ① `BL12` 是 `ta = 20 self`（「我自己被标记」才触发）"
                    + "｜🧨 把候选接成 `target`（本族恒 null）⇒ 判不了 ⇒ 负例放行 ⇒ 红");
        }

        var sau3 = CardDatabase.FindById(pool, "SAU3");
        CheckTrue(sau3 != null, "① （前提）卡池里有 `SAU3 Flayed One`");
        if (sau3 != null)
        {
            string why; bool amb;
            var set = sau3.AbilityLevelCriteriaForTrigger("Reanimated", out why, out amb);
            CheckTrue(set != null, "★★ ① `SAU3` 取得到 `Reanimated(570)` 的 36 格");
            if (set != null) Check(set.TargetsAffected, 20, "★★ ① `SAU3` 是 `ta = 20 self`");
        }

        // ①·b 每族一张 `ta = 10 allFiltered` 的真卡，逐格写死（生成器/列位置一变就红）
        E6CheckRow(pool, "BL28", "UnitReceivesMarkOfChaos", 0, 1, 0, 1, 1);
        E6CheckRow(pool, "SAU37", "Reanimated", 0, 1, 0, 1, -1);
        E6CheckRow(pool, "TAU15", "UnitReceivesMarkerlight", 1, 0, 1, 1, 0);
        E6CheckRow(pool, "BL8", "UnitDamaged", 0, 1, 1, 0, 0);
        E6CheckRow(pool, "ASH75", "UnitStunned", 1, 0, 1, 1, 0);

        // ①·c `BL63 Havoc` 的 **actingCardCriteria**（第一道闸）也读得到 —— 510 那 2 行会被它筛
        var bl63 = CardDatabase.FindById(pool, "BL63");
        CheckTrue(bl63 != null, "① （前提）卡池里有 `BL63 Havoc`");
        if (bl63 != null)
        {
            string why; bool amb;
            var set = bl63.ActingLevelCriteriaForTrigger("UnitReceivesMarkOfChaos", out why, out amb);
            CheckTrue(set != null,
                      "★★ ① `BL63` 的 `actingCardCriteria`（`+0x28`）也读得到"
                    + "｜🧨 `AbilityActingGateApplies` 不认 `GetsDarkPact` ⇒ 这 2 行永远不判 ⇒ 红");
            if (set != null)
            {
                Check(set.TargetsAffected, 10, "★ ① `BL63` acting `ta = 10 allFiltered`");
                Check(set.FriendlyUnits, 1, "★★ ① `BL63` acting `friendlyUnits = 1`（acting 必须是友方）");
                Check(set.IncludeMinions, 1, "★ ① `BL63` acting `includeMinions = 1`");
                Check(set.IncludeSelf, 1, "★ ① `BL63` acting `includeSelf = 1`（可以是自己）");
            }
        }

        // ①·d 覆盖面：13 行 = 510:4 · 570:5 · 515:1 · 60:1 · 291:2（现读 v28）
        string[] names = { "UnitReceivesMarkOfChaos", "Reanimated", "UnitReceivesMarkerlight",
                           "UnitDamaged", "UnitStunned" };
        int[] expect = { 4, 5, 1, 1, 2 };
        int total = 0;
        for (int i = 0; i < names.Length; i++)
        {
            int n = 0;
            foreach (var c in pool)
            {
                if (c == null) continue;
                string why; bool amb;
                if (c.AbilityLevelCriteriaForTrigger(names[i], out why, out amb) != null) n++;
            }
            total += n;
            Check(n, expect[i],
                  $"★★ ① 全池 `abilityLevelCriteria` 里 `{names[i]}` 的行数"
                + "｜🧨 生成器那边少落一族 / 本件接错族 ⇒ 数字变 ⇒ 红");
        }
        Check(total, 13, "★★ ① 本批 13 行（判据 = `E5_引擎剩余小缺口.md` §五 第 2 档）");
    }

    /// <summary>一张真卡的 level 行：`enemy / friendly / hero / minion / self` 逐格写死。
    /// `self = -1` ⇒ 这一格不验（`SAU37` 的 `includeSelf=0` 就是显式 0，这里区分「0」与「不验」）。</summary>
    static void E6CheckRow(List<CardDef> pool, string id, string trigger,
                           int enemyUnits, int friendlyUnits, int includeHero, int includeMinions, int includeSelf)
    {
        var c = CardDatabase.FindById(pool, id);
        CheckTrue(c != null, $"① （前提）卡池里有 `{id}`");
        if (c == null) return;
        string why; bool amb;
        var set = c.AbilityLevelCriteriaForTrigger(trigger, out why, out amb);
        CheckTrue(set != null,
                  $"★★ ① `{id}` 取得到 `{trigger}` 的 ability 级 criteria"
                + "｜🧨 这一族没接（`TriggerNameOfWhenKind` 缺分支 / 列没落）⇒ null ⇒ 红");
        if (set == null) return;
        Check(set.TargetsAffected, 10, $"★★ ① `{id}` `ta = 10 allFiltered`");
        Check(set.EnemyUnits, enemyUnits, $"★★ ① `{id}` `enemyUnits = {enemyUnits}`（原本这条是**死的**）");
        Check(set.FriendlyUnits, friendlyUnits, $"★★ ① `{id}` `friendlyUnits = {friendlyUnits}`");
        Check(set.IncludeHero, includeHero, $"★ ① `{id}` `includeHero = {includeHero}`");
        Check(set.IncludeMinions, includeMinions, $"★ ① `{id}` `includeMinions = {includeMinions}`");
        if (includeSelf >= 0) Check(set.IncludeSelf, includeSelf, $"★ ① `{id}` `includeSelf = {includeSelf}`");
    }

    // ==================================================================
    //  ② 两条新映射：`GetsStun ⇒ UnitStunned(291)` · `GetsMarkerlight ⇒ UnitReceivesMarkerlight(515)`
    // ==================================================================

    static void E6Case2TriggerNameMappings()
    {
        // ②·a 291：监听者结算 ⇒ 发一条 `EvtKind.Trigger(keyword = UnitStunned)`
        var stunL = E6Listener("E6L291map", "When an enemy receives a Stun, deal 2 damage to it",
                               291, E6Criteria(1, 0));
        CheckTrue(HasWhenKind(stunL, WhenEventKind.GetsStun),
                  "②a （前提）合成卡正文解析出了 `GetsStun` 监听器｜🧨 `WhenEvent` 词表变了 ⇒ 红");
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 2, stunL);
            var foe = Place(ctx, 1, 1, Unit("E6Map291", 1, 2, 9));
            RuleCore.BroadcastWhen(ctx, WhenEventKind.GetsStun, 1, foe.Card, subject: foe);
            Check(E6CountTriggerEvents(ctx, "UnitStunned"), 1,
                  "★★ ②a 监听者结算 ⇒ **恰好一条** `EvtKind.Trigger(UnitStunned)`"
                + "｜🧨 删掉 `TriggerNameOfWhenKind` 的 `case GetsStun` ⇒ 0 条 ⇒ 红"
                + "（改前就是 0：`tn == null` ⇒ `EmitListenerTrigger` 直接 return）");
        }

        // ②·b 515：同一形状
        var markL = E6Listener("E6L515map", "When an enemy gains Markerlight, deal 2 damage to it",
                               515, E6Criteria(1, 0));
        CheckTrue(HasWhenKind(markL, WhenEventKind.GetsMarkerlight),
                  "②b （前提）合成卡正文解析出了 `GetsMarkerlight` 监听器");
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 2, markL);
            var foe = Place(ctx, 1, 1, Unit("E6Map515", 1, 2, 9));
            RuleCore.BroadcastWhen(ctx, WhenEventKind.GetsMarkerlight, 1, foe.Card, subject: foe);
            Check(E6CountTriggerEvents(ctx, "UnitReceivesMarkerlight"), 1,
                  "★★ ②b 监听者结算 ⇒ **恰好一条** `EvtKind.Trigger(UnitReceivesMarkerlight)`"
                + "｜🧨 删掉 `case GetsMarkerlight` ⇒ 0 条 ⇒ 红");
        }

        // ②·c 反例（灭自证）：这张卡**没有** 291 这条 ability（`abilityTriggers` 里没有）⇒
        //      `HasTriggerSource` 的 presence 闸 ⇒ **不发**（映射不是「见事件就发」）
        {
            var noAbility = E6Listener("E6L291no", "When an enemy receives a Stun, deal 2 damage to it",
                                       291, null);
            // 把 abilityTriggers 改成「只有 46」需要另一张卡：这里用 `presence` 直接判
            Check(noAbility.AbilityTriggerPresence("UnitStunned"), 1,
                  "②c （前提）合成卡声明了 291 这条 ability（presence = 1）");
            var other = new CardDef("E6No291", "E6No291", "unit",
                                    "When an enemy receives a Stun, deal 2 damage to it",
                                    "common", "Test", 1, 1, 5, 0, null, subtype: "Infantry",
                                    abilityTriggers: new[] { 46 }, abilityTriggersVersioned: true);
            Check(other.AbilityTriggerPresence("UnitStunned"), 0,
                  "★★ ②c 反例：`abilityTriggers` 只有 46 的卡 ⇒ `presence(UnitStunned) = 0`"
                + "｜🧨 把 presence 闸删掉（只看动画行）⇒ 这张卡会凭空发 291 事件 ⇒ 红");
        }
    }

    // ==================================================================
    //  ③ 五族的闸：正例 / 负例 / 灭自证（旧表 = 判不了 ⇒ 放行 + 出声）
    // ==================================================================

    static void E6Case3Gates()
    {
        // ---------------- 510 `GetsDarkPact` ----------------
        {
            const string desc = "When a friendly troop receives a Dark Pact, deal 2 damage to it";
            var lvl = E6Criteria(0, 1); lvl.IncludeHero = 0; lvl.IncludeMinions = 1; lvl.IncludeSelf = 1;
            var act = E6Criteria(0, 1); act.IncludeHero = 0; act.IncludeMinions = 1; act.IncludeSelf = 1;
            var both = E6Listener("E6L510a", desc, 510, lvl, act);

            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 2, both);
            var mate = Place(ctx, 0, 3, Unit("E6DpMate", 1, 1, 9));
            int l0 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int a0 = RuleCore.AbilityActingCriteriaWarningsForTest;
            int hp0 = mate.Health;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.GetsDarkPact, 0, mate.Card, subject: mate);
            Check(mate.Health - hp0, -2,
                  "★★ ③a 510 正例：友方部队收到黑暗契约 ⇒ 两道闸都过 ⇒ 监听者结算（-2）");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - l0, 0, "★ ③a 510 正例 target 闸不出声");
            Check(RuleCore.AbilityActingCriteriaWarningsForTest - a0, 0, "★ ③a 510 正例 acting 闸不出声");

            // acting 闸负例：**同一张卡只翻 acting 那一格**（friendlyUnits=1 → enemyUnits=1）
            var act2 = E6Criteria(1, 0); act2.IncludeHero = 0; act2.IncludeMinions = 1; act2.IncludeSelf = 1;
            var la = E6Listener("E6L510b", desc, 510, lvl, act2);
            var ctx2 = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx2, 0, 2, la);
            var mate2 = Place(ctx2, 0, 3, Unit("E6DpMate2", 1, 1, 9));
            int l1 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int a1 = RuleCore.AbilityActingCriteriaWarningsForTest;
            int hp1 = mate2.Health;
            RuleCore.BroadcastWhen(ctx2, WhenEventKind.GetsDarkPact, 0, mate2.Card, subject: mate2);
            Check(mate2.Health - hp1, 0,
                  "★★ ③a 510 负例·acting：acting criteria 翻成 `enemyUnits=1` ⇒ 不结算"
                + "｜🧨 `AbilityActingGateCard(GetsDarkPact)` 回 `actingCard`（本族恒 null）⇒ 判不了 ⇒ 放行 ⇒ 掉血 ⇒ 红");
            Check(RuleCore.AbilityActingCriteriaWarningsForTest - a1, 1,
                  "★★ ③a 510 负例·acting **出声**（反静默）");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - l1, 0,
                  "★ ③a 510 acting 挡住后 target 闸**不再判**（原版短路次序 = acting → target，"
                + "`CardAbility__CanTriggerAbility.c:127-135`）");

            // target 闸负例：只挂 level 闸，同一张卡只翻 level 那一格
            var lvl2 = E6Criteria(1, 0); lvl2.IncludeHero = 0; lvl2.IncludeMinions = 1; lvl2.IncludeSelf = 1;
            var lb = E6Listener("E6L510c", desc, 510, lvl2);
            var ctx3 = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx3, 0, 2, lb);
            var mate3 = Place(ctx3, 0, 3, Unit("E6DpMate3", 1, 1, 9));
            int l2 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp2 = mate3.Health;
            RuleCore.BroadcastWhen(ctx3, WhenEventKind.GetsDarkPact, 0, mate3.Card, subject: mate3);
            Check(mate3.Health - hp2, 0, "★★ ③a 510 负例·target：level criteria `enemyUnits=1` ⇒ 不结算");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - l2, 1, "★★ ③a 510 负例·target 出声");

            // 灭自证：列没落 ⇒ 判不了 ⇒ **放行 + 出声**（⛔ 不把「不知道」当「不满足」）
            var lv = E6Listener("E6L510d", desc, 510, lvl, null, levelVersioned: false);
            var ctx4 = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx4, 0, 2, lv);
            var mate4 = Place(ctx4, 0, 3, Unit("E6DpMate4", 1, 1, 9));
            int l3 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp3 = mate4.Health;
            RuleCore.BroadcastWhen(ctx4, WhenEventKind.GetsDarkPact, 0, mate4.Card, subject: mate4);
            Check(mate4.Health - hp3, -2,
                  "★★ ③a 510 灭自证：`abilityLevelCriteriaVersioned = false` ⇒ 判不了 ⇒ **照跑**"
                + "｜🧨 把 `null` 当「不满足」⇒ 不跑 ⇒ 红");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - l3, 1,
                  "★★ ③a 510 灭自证：这一档**必须出声**（⛔ 静默放行更糟）");
        }

        // ---------------- 570 `Reanimated` ----------------
        {
            const string desc = "When you reanimate a Remnant, deal 2 damage to it";
            var lvl = E6Criteria(0, 1); lvl.IncludeHero = 0; lvl.IncludeMinions = 1;
            var L = E6Listener("E6L570a", desc, 570, lvl);

            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 2, L);
            var mate = Place(ctx, 0, 3, Unit("E6ReMate", 1, 1, 9));
            int w0 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp0 = mate.Health;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Reanimated, 0, mate.Card, subject: mate);
            Check(mate.Health - hp0, -2, "★★ ③b 570 正例：友方部队被复活 ⇒ 结算（-2）");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - w0, 0, "★ ③b 570 正例不出声");

            // 负例：**正文仍匹配**（「you reanimate」只看谁做的），candidate 换成友方**督军** ⇒ `includeHero=0`
            var ctx2 = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx2, 0, 2, L);
            var wl = ctx2.Players[0].Warlord;
            int w1 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp1 = wl.Health;
            RuleCore.BroadcastWhen(ctx2, WhenEventKind.Reanimated, 0, wl.Card, subject: wl);
            Check(wl.Health - hp1, 0,
                  "★★ ③b 570 负例：友方督军（`includeHero = 0`）⇒ 不结算"
                + "｜🧨 候选接成 `target`（本族恒 null）⇒ 判不了 ⇒ 放行 ⇒ 掉血 ⇒ 红");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - w1, 1, "★★ ③b 570 负例出声");

            var lv = E6Listener("E6L570b", desc, 570, lvl, null, levelVersioned: false);
            var ctx3 = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx3, 0, 2, lv);
            var mate3 = Place(ctx3, 0, 3, Unit("E6ReMate3", 1, 1, 9));
            int w2 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp2 = mate3.Health;
            RuleCore.BroadcastWhen(ctx3, WhenEventKind.Reanimated, 0, mate3.Card, subject: mate3);
            Check(mate3.Health - hp2, -2, "★★ ③b 570 灭自证：列没落 ⇒ 判不了 ⇒ 照跑");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - w2, 1, "★★ ③b 570 灭自证出声");
        }

        // ---------------- 515 `GetsMarkerlight` ----------------
        {
            const string desc = "When an enemy gains Markerlight, deal 2 damage to it";
            var lvl = E6Criteria(1, 0);
            var L = E6Listener("E6L515a", desc, 515, lvl);

            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 2, L);
            var foe = Place(ctx, 1, 1, Unit("E6MlFoe", 1, 2, 9));
            int w0 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp0 = foe.Health;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.GetsMarkerlight, 1, foe.Card, subject: foe);
            Check(foe.Health - hp0, -2, "★★ ③c 515 正例：敌方部队获得 Markerlight ⇒ 结算（-2）");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - w0, 0, "★ ③c 515 正例不出声");

            var lv2 = E6Criteria(1, 0); lv2.IncludeHero = 0;
            var L2 = E6Listener("E6L515b", desc, 515, lv2);
            var ctx2 = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx2, 0, 2, L2);
            var foeWl = ctx2.Players[1].Warlord;
            int w1 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp1 = foeWl.Health;
            RuleCore.BroadcastWhen(ctx2, WhenEventKind.GetsMarkerlight, 1, foeWl.Card, subject: foeWl);
            Check(foeWl.Health - hp1, 0,
                  "★★ ③c 515 负例：敌方**督军**（`includeHero = 0`）⇒ 不结算"
                + "｜🧨 候选接成 `target` ⇒ 放行 ⇒ 掉血 ⇒ 红");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - w1, 1, "★★ ③c 515 负例出声");

            var lv3 = E6Listener("E6L515c", desc, 515, lvl, null, levelVersioned: false);
            var ctx3 = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx3, 0, 2, lv3);
            var foe3 = Place(ctx3, 1, 1, Unit("E6MlFoe3", 1, 2, 9));
            int w2 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp2 = foe3.Health;
            RuleCore.BroadcastWhen(ctx3, WhenEventKind.GetsMarkerlight, 1, foe3.Card, subject: foe3);
            Check(foe3.Health - hp2, -2, "★★ ③c 515 灭自证：列没落 ⇒ 判不了 ⇒ 照跑");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - w2, 1, "★★ ③c 515 灭自证出声");
        }

        // ---------------- 60 `Damaged`（BL8 的真判据形状：`When your Warlord receives damage`）----------------
        {
            // ⚠️ 效果**不能打承受者自己**：那样「受伤 ⇒ 再受伤」会连锁到 `EffectChain` 上限
            //    （实测 -24 血 / 13 条告警）⇒ 断言全乱。打敌方督军（确定性、可观测）。
            const string desc = "When your Warlord receives damage, deal 2 damage to the enemy Warlord";
            var lvl = E6Criteria(0, 1); lvl.IncludeHero = 1; lvl.IncludeMinions = 0;
            var L = E6Listener("E6L60a", desc, 60, lvl);

            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 2, L);
            var wl = ctx.Players[0].Warlord;
            var foeWl = ctx.Players[1].Warlord;
            int w0 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp0 = foeWl.Health;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Damaged, 0, wl.Card, subject: wl);
            Check(foeWl.Health - hp0, -2,
                  "★★ ③d 60 正例：**我方督军**受伤 ⇒ 结算（敌方督军 -2）"
                + "｜🧨 `AbilityLevelGateTarget(Damaged)` 不给 `subject` ⇒ 判不了 ⇒ 仍会跑（不红）**但**"
                + " 🧨 `WhenEvents.Subjects.Parse` 把 `warlord` 当卡名 ⇒ 监听器压根不响 ⇒ 红");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - w0, 0, "★ ③d 60 正例不出声");

            // 负例：**同一张卡只翻 criteria 那一格**（friendlyUnits=1 → enemyUnits=1），正文仍匹配
            var lv2 = E6Criteria(1, 0); lv2.IncludeHero = 1; lv2.IncludeMinions = 0;
            var L2 = E6Listener("E6L60b", desc, 60, lv2);
            var ctx2 = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx2, 0, 2, L2);
            var wl2 = ctx2.Players[0].Warlord;
            var foeWl2 = ctx2.Players[1].Warlord;
            int w1 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp1 = foeWl2.Health;
            RuleCore.BroadcastWhen(ctx2, WhenEventKind.Damaged, 0, wl2.Card, subject: wl2);
            Check(foeWl2.Health - hp1, 0,
                  "★★ ③d 60 负例：criteria 翻成 `enemyUnits=1` ⇒ 不结算（正文匹配、闸挡住）"
                + "｜🧨 候选接成 `target`（本族恒 null）⇒ 判不了 ⇒ 放行 ⇒ 掉血 ⇒ 红");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - w1, 1, "★★ ③d 60 负例出声");

            var lv3 = E6Listener("E6L60c", desc, 60, lvl, null, levelVersioned: false);
            var ctx3 = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx3, 0, 2, lv3);
            var wl3 = ctx3.Players[0].Warlord;
            var foeWl3 = ctx3.Players[1].Warlord;
            int w2 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp2 = foeWl3.Health;
            RuleCore.BroadcastWhen(ctx3, WhenEventKind.Damaged, 0, wl3.Card, subject: wl3);
            Check(foeWl3.Health - hp2, -2, "★★ ③d 60 灭自证：列没落 ⇒ 判不了 ⇒ 照跑");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - w2, 1, "★★ ③d 60 灭自证出声");
        }

        // ---------------- 291 `GetsStun` ----------------
        {
            const string desc = "When an enemy receives a Stun, deal 2 damage to it";
            var lvl = E6Criteria(1, 0);
            var L = E6Listener("E6L291a", desc, 291, lvl);

            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 2, L);
            var foe = Place(ctx, 1, 1, Unit("E6StFoe", 1, 2, 9));
            int w0 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp0 = foe.Health;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.GetsStun, 1, foe.Card, subject: foe);
            Check(foe.Health - hp0, -2,
                  "★★ ③e 291 正例：敌方部队被晕 ⇒ 结算（`it` = subject = 被晕者，-2）");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - w0, 0, "★ ③e 291 正例不出声");

            var lv2 = E6Criteria(1, 0); lv2.IncludeHero = 0;
            var L2 = E6Listener("E6L291b", desc, 291, lv2);
            var ctx2 = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx2, 0, 2, L2);
            var foeWl = ctx2.Players[1].Warlord;
            int w1 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp1 = foeWl.Health;
            RuleCore.BroadcastWhen(ctx2, WhenEventKind.GetsStun, 1, foeWl.Card, subject: foeWl);
            Check(foeWl.Health - hp1, 0,
                  "★★ ③e 291 负例：敌方**督军**（`includeHero = 0`）⇒ 不结算"
                + "｜🧨 候选接成 `actingCard`（`ReactToUnitStunned` 的 `param_3` = 施害者，我们没带）⇒ 判不了 ⇒ 放行 ⇒ 掉血 ⇒ 红");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - w1, 1, "★★ ③e 291 负例出声");

            var lv3 = E6Listener("E6L291c", desc, 291, lvl, null, levelVersioned: false);
            var ctx3 = ProbeBattle(new CardDef[0], new CardDef[0]);
            Place(ctx3, 0, 2, lv3);
            var foe3 = Place(ctx3, 1, 1, Unit("E6StFoe3", 1, 2, 9));
            int w2 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            int hp2 = foe3.Health;
            RuleCore.BroadcastWhen(ctx3, WhenEventKind.GetsStun, 1, foe3.Card, subject: foe3);
            Check(foe3.Health - hp2, -2, "★★ ③e 291 灭自证：列没落 ⇒ 判不了 ⇒ 照跑");
            Check(RuleCore.AbilityLevelCriteriaWarningsForTest - w2, 1, "★★ ③e 291 灭自证出声");
        }

        // ③·f `ListenerLane`：新映射不会把这批卡的队列道改掉（这 5 张都没有 `triggerPriority` 行）
        {
            var pool = CardDatabase.Load();
            foreach (var p in new[]
                     {
                         new[] { "ASH75", "UnitStunned" }, new[] { "ASH74", "UnitStunned" },
                         new[] { "TAU15", "UnitReceivesMarkerlight" },
                         new[] { "BL28", "UnitReceivesMarkOfChaos" }, new[] { "SAU37", "Reanimated" },
                     })
            {
                var c = CardDatabase.FindById(pool, p[0]);
                if (c == null) continue;
                Check((int)RuleCore.ListenerLane(c, p[1]), CardDef.DefaultTriggerPriority,
                      $"★ ③f `{p[0]}` 配上 `{p[1]}` 之后仍在默认道（`triggerPriority` 没有这一行）"
                    + "｜🧨 卡表多加了一条 priority 行 ⇒ 道变了 ⇒ 排队次序变 ⇒ 红");
            }
        }
    }

    // ==================================================================
    //  ④ `When your Warlord …` 的主语解析（BL8 的正文原本**永远不匹配**）
    // ==================================================================

    static void E6Case4WarlordSubjectParse()
    {
        var pool = CardDatabase.Load();
        var bl8 = CardDatabase.FindById(pool, "BL8");
        CheckTrue(bl8 != null, "④ （前提）卡池里有 `BL8 Noise Marine`");
        if (bl8 == null) return;

        Check(bl8.WhenTriggers.Count, 1,
              "★ ④ `BL8` 只有一条监听（`When your Warlord receives damage, …`）");
        var ev = bl8.WhenTriggers.Count > 0 ? bl8.WhenTriggers[0].Ev : null;
        CheckTrue(ev != null && ev.Kind == WhenEventKind.Damaged,
                  "★★ ④ `BL8` 的监听事件 = `Damaged`｜🧨 正文解析不再认这句话 ⇒ 红");
        if (ev == null) return;

        CheckTrue(ev.Criteria != null,
                  "★★ ④ `BL8` 的主语带筛选（「your **Warlord**」）");
        if (ev.Criteria == null) return;
        Check(ev.Criteria.WarlordOnly ? 1 : 0, 1,
              "★★★ ④ 主语的是**督军**维度（`WarlordOnly`）"
            + "｜🧨 `WhenEvents.Subjects.Parse` 把 `warlord` 当卡名（`Name = \"warlord\"`）⇒ 这一格 0 ⇒ 红"
            + "（改前就是这样：**没有任何一张卡叫 `warlord`** ⇒ 这条监听器一次都不响、且不报错）");
        CheckTrue(string.IsNullOrEmpty(ev.Criteria.Name),
                  "★★★ ④ 且**没有**落到「按卡名全等匹配」那一支（`Name` 必须为空）"
                + "｜🧨 退回 `c.Name = s` ⇒ 恒不匹配 ⇒ 红");

        // 行为：我方督军 = 匹配；我方**部队** = 不匹配（正文那一层就挡掉，与闸是两件事）
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var listener = Place(ctx, 0, 2, Unit("E6WlListener", 1, 1, 5));
        var wl = ctx.Players[0].Warlord;
        var mate = Place(ctx, 0, 3, Unit("E6WlMate", 1, 1, 9));
        CheckTrue(WhenEvents.Matches(ev, 0, 0, wl.Card, listener, wl),
                  "★★★ ④ **我方督军受伤 ⇒ 正文层匹配**（改前恒 `false`）"
                + "｜🧨 主语解析退回「卡名 = warlord」⇒ 红");
        CheckTrue(!WhenEvents.Matches(ev, 0, 0, mate.Card, listener, mate),
                  "★★ ④ 反向：**我方部队**受伤 ⇒ 正文层不匹配（「Warlord」是真筛选，不是摆设）"
                + "｜🧨 把 `WarlordOnly` 改成恒真 / 把主语丢了 ⇒ 这条会变 true ⇒ 红");
    }
}
