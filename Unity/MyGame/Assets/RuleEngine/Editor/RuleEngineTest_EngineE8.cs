// RuleEngineTest_EngineE8.cs — 引擎线 `E8`（2026-10-25 · 第七十六会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineE5` /
// `_EngineE6` / `_EngineE7` / `_E8` … 都在），同一时刻只有一个写手改一个文件 ⇒
// 本批断言全写在这里，`RuleEngineTest.cs` **只加 `Run()` 里一行
// `Step(TestE8AbilityOwnerAndListenerOrder)`**。
//
// ⚠️ 文件名带 `Engine` 前缀是为了和已有的 `_E8.cs`（那是另一个批次的 `ta=150` 空表回归）分开 ——
//    两者都在同一个 partial 类里，**辅助方法名不许撞车**（本件一律用 `E8b…` 前缀）。
//
// 本件钉两件（判据与改动清单 → `资料/普查产出_1025/E8_友方目标与监听器次序.md`）：
//   · **E8-1**：主动能力的**候选所有者**不再写死 `1 - p` —— 按原版 `ActiveAbility.targetCriteria`
//     的候选侧（`friendlyUnits` / `enemyUnits` 两格）解；`EffectTargets` 新增 `FriendlyUnit`
//     + `ResolveTarget` 对应支。
//   · **E8-2**：`FireBoardWhenListener` 里 `EmitListenerTrigger`（那一拍动画事件）**排在
//     `EnqueueAbility` 的两道能力级闸之后**，被闸拒时**连它一起跳过**（原版次序 =
//     `RawCardScript__TriggerAbilities.c:108/:207` 先 `CanTriggerAbility`，`:60` 才 `PlayTriggerTraitAnim`）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量 —— ⛔ 不从实现常量反算（那会自证）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestE8AbilityOwnerAndListenerOrder)`）。</summary>
    public static void TestE8AbilityOwnerAndListenerOrder()
    {
        Section("🆕 引擎线 E8：主动能力的**候选侧**（敌/友/任意/判不出）＋ 监听器事件排在能力级闸**之后**");
        E8bSideReadings();
        E8bFriendlyTargetLandsOnFriendlySide();
        E8bEnemySideUnchanged();
        E8bAnySideTriesBoth();
        E8bUnknownFallsBackToEnemy();
        E8bSelfTargets();
        E8bListenerEmitAfterGates();
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>我方主动能力卡：`keywords` 里带一条 `Ability: …`（**我们自己设计的封闭文法** ——
    /// 池内 `Ability:` 0 张，见 `CardDef.HasActiveAbility` 的注释），criteria 挂在 `ab = -1` 上。
    /// <para>⚠️ `hasAlternativeAbility: 0` **必须给**：`GateUnknown(-1)` ⇒ `UsesAlternativeAbility`
    /// 回 `null` ⇒ 候选侧**判不了** ⇒ 夹具会静默变成「只验到退回敌方」那一档（E4 同款陷阱）。</para></summary>
    static CardDef E8bAbilityCard(string id, string abilityText, CardDef.CountSetSpec pick, bool versioned = true)
    {
        var rows = new List<CardDef.TargetCriteriaRowSpec>();
        if (pick != null)
            rows.Add(new CardDef.TargetCriteriaRowSpec
            { Ability = CardDef.ActiveAbilityObject, Trigger = -1, Logic = -1, Criteria = pick });
        return new CardDef(id, id, "unit", "", null, "Test", 1, 2, 6, 0,
                           new[] { "Ability: " + abilityText },
                           hasAlternativeAbility: 0,
                           activeAbilityPickCriteria: rows,
                           activeAbilityPickCriteriaVersioned: versioned);
    }

    /// <summary>36 格（缺一维 = `Unknown` ⇒ `Matches` 回「判不了」⇒ 断言会静默变成没验到）。
    /// 形状照 `_EngineE2.E2Criteria10`（唯一判据），只加 `includeSelf` 一格。</summary>
    static CardDef.CountSetSpec E8bCrit(int enemy, int friendly, int includeSelf = 0)
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
            FriendlyUnits = friendly, EnemyUnits = enemy,
            IncludeHero = 1, IncludeMinions = 1,
            IncludeSpells = 0, IncludeWhispers = 0, IncludeWeapons = 0,
            IncludeRemnants = 1, IncludeHeroParts = 0,
            IncludeSelf = includeSelf, ExcludeNotSelf = 0, ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0, ExcludeRace = 0, ExcludeTrait = 0,
            SelectFromChooseResolution = 0,
        };
    }

    /// <summary>合成监听者：`desc` 决定 `WhenTriggers`，`trigger` 是**原版 trigger 号**。
    /// <para>🔴 `abilityTriggers` 与 `abilityTargets` **两列都要给**：前者是 `EmitCardTrigger` 那道
    /// 「有没有这条 ability」闸的真值，后者是 ability 定位的索引 —— 缺前者 ⇒ `EvtKind.Trigger` 不发。</para></summary>
    static CardDef E8bListener(string id, string desc, int trigger,
                               CardDef.CountSetSpec level, bool versioned = true)
    {
        var ta = new List<CardDef.AbilityTargetSpec>
        { new CardDef.AbilityTargetSpec { Trigger = trigger, TargetsAffected = new[] { 10 } } };
        var rows = new List<CardDef.TargetCriteriaRowSpec>();
        if (level != null)
            rows.Add(new CardDef.TargetCriteriaRowSpec
            { Ability = 0, Trigger = trigger, Logic = -1, Criteria = level });
        return new CardDef(id, id, "unit", desc, null, "Test", 1, 1, 9, 0, null,
                           subtype: "Infantry",
                           abilityTriggers: new[] { trigger },
                           abilityTriggersVersioned: true,
                           abilityTargets: ta.ToArray(),
                           abilityLevelCriteria: rows.ToArray(),
                           abilityLevelCriteriaVersioned: versioned);
    }

    static int E8bCountTrigger(BattleContext ctx, string keyword)
    {
        int n = 0;
        foreach (var e in ctx.Signals)
            if (e.Kind == EvtKind.Trigger && e.Keyword == keyword) n++;
        return n;
    }

    // ==================================================================
    //  ① 判据本体：`AbilityPickSideOf` 四档
    // ==================================================================

    static void E8bSideReadings()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
        int me = ctx.Active;
        var cEnemy = Place(ctx, me, 2, E8bAbilityCard("E8bEnemySide", "Damage 2 EnemyUnit", E8bCrit(1, 0)));
        var cFriend = Place(ctx, me, 3, E8bAbilityCard("E8bFriendSide", "Heal 2 FriendlyUnit", E8bCrit(0, 1)));
        var cAny = Place(ctx, me, 6, E8bAbilityCard("E8bAnySide", "Damage 2 EnemyUnit", E8bCrit(1, 1)));
        var cUnknown = Place(ctx, me, 5, E8bAbilityCard("E8bUnknownSide", "Damage 2 EnemyUnit", E8bCrit(1, 0), versioned: false));
        string why;
        Check((int)RuleCore.AbilityPickSideOf(ctx, me, cEnemy, out why), 1,
              "★★ ① `enemyUnits=1 / friendlyUnits=0` ⇒ **Enemy(1)**");
        Check((int)RuleCore.AbilityPickSideOf(ctx, me, cFriend, out why), 2,
              "★★ ① `friendlyUnits=1 / enemyUnits=0` ⇒ **Friendly(2)**"
            + "｜🧨 两格的读法写反 ⇒ 红");
        Check((int)RuleCore.AbilityPickSideOf(ctx, me, cAny, out why), 3,
              "★★ ① 两格都 =1 ⇒ **Any(3)**（两侧都进候选；⛔ 别静默归成敌方）"
            + "｜🧨 `(1,1)` 当成 Enemy ⇒ 红");
        Check((int)RuleCore.AbilityPickSideOf(ctx, me, cUnknown, out why), 0,
              "★★ ① criteria 列没落 ⇒ **Unknown(0)**（⛔ 不是 Any：那是原版明确说「任意」）"
            + "｜🧨 把「判不了」当「任意」⇒ 红");
    }

    // ==================================================================
    //  ② 友方档：效果**真的落在友方棋盘**，敌方血一滴不掉（判别式）
    // ==================================================================

    static void E8bFriendlyTargetLandsOnFriendlySide()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
        int me = ctx.Active;
        var caster = Place(ctx, me, 3, E8bAbilityCard("E8bFriendly", "Heal 2 FriendlyUnit", E8bCrit(0, 1)));
        var ally = Place(ctx, me, 6, Unit("E8bAlly", 1, 1, 5));
        var foe = Place(ctx, 1 - me, 3, Unit("E8bFoe", 1, 1, 5));
        ally.Health = 2; foe.Health = 5;

        int owner;
        CheckCode(RuleCore.CanUseAbility(ctx, me, 3, 6, out owner), RuleCodes.OK,
                  "★★ ② 友方档：点**自己棋盘**第 6 格 ⇒ 可点");
        Check(owner, me, "★★ ② 候选所有者解到**施法者自己那一侧**"
                       + "｜🧨 解回 `1 - p` ⇒ 红（下面「敌对血」两条也会红）");
        CheckCode(RuleCore.CanUseAbility(ctx, me, 3, 8), RuleCodes.ErrTarget,
                  "★★ ② 友方档：点自己棋盘**空格** ⇒ `ErrTarget`（不是「随便找一个友方」）");
        CheckCode(RuleCore.UseAbility(ctx, me, 3, 6), RuleCodes.OK, "★★ ② 友方档：放得出来");
        Check(ally.Health, 4, "★★★ ② 自己人 2 → 4（治疗 2）｜🧨 owner 写回 `1 - p` ⇒ 2 不变 ⇒ 红");
        Check(foe.Health, 5, "★★★ ② **敌方血一滴没掉**（判别式：友方目标技能不许打到对面）");
        CheckTrue(caster.Exhausted, "★★ ② 施法者已耗尽（技能真的发动了，不是空过）");
    }

    // ==================================================================
    //  ③ 敌方档：与改前逐位相同（回归）
    // ==================================================================

    static void E8bEnemySideUnchanged()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
        int me = ctx.Active;
        Place(ctx, me, 3, E8bAbilityCard("E8bEnemy", "Damage 2 EnemyUnit", E8bCrit(1, 0)));
        var mate = Place(ctx, me, 6, Unit("E8bMate", 1, 1, 5));
        var foe = Place(ctx, 1 - me, 5, Unit("E8bFoe2", 1, 1, 5));

        int owner;
        CheckCode(RuleCore.CanUseAbility(ctx, me, 3, 5, out owner), RuleCodes.OK, "★★ ③ 敌方档：点敌人 ⇒ 可点");
        Check(owner, 1 - me, "★★ ③ 候选所有者 = 对面（= 改前的唯一一档）");
        CheckCode(RuleCore.UseAbility(ctx, me, 3, 5), RuleCodes.OK, "★★ ③ 放得出来");
        Check(foe.Health, 3, "★★ ③ 敌人 5 → 3");
        Check(mate.Health, 5, "★★ ③ 自己人不动");
    }

    // ==================================================================
    //  ④ 任意档：**两侧都进候选**（先敌后友的确定性次序）
    // ==================================================================

    static void E8bAnySideTriesBoth()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
        int me = ctx.Active;
        Place(ctx, me, 3, E8bAbilityCard("E8bAny", "Damage 2 EnemyUnit", E8bCrit(1, 1)));
        var mate = Place(ctx, me, 7, Unit("E8bMateAny", 1, 1, 5));
        var foe = Place(ctx, 1 - me, 7, Unit("E8bFoeAny", 1, 1, 5));

        int owner;
        CheckCode(RuleCore.CanUseAbility(ctx, me, 3, 7, out owner), RuleCodes.OK,
                  "★★ ④ 任意档：两侧同号格都有人 ⇒ 可点");
        Check(owner, 1 - me, "★★ ④ 两侧都合法 ⇒ **先敌后友**（确定性次序，联机/回放两端一致）"
                           + "｜🧨 次序反了 ⇒ 红（这条钉的是「两端一致」，不是「原版次序」——原版没有这一格）");
        CheckCode(RuleCore.UseAbility(ctx, me, 3, 7), RuleCodes.OK, "★★ ④ 放得出来");
        Check(foe.Health, 3, "★★ ④ 对面 5 → 3");
        Check(mate.Health, 5, "★★ ④ 自己人不动");

        // 第二侧：对面同号格空 ⇒ 落到**自己那一侧**（证明「两侧都真的在候选里」）
        var ctx2 = ProbeBattle(new CardDef[0], new CardDef[0]);
        if (ctx2.Turn == 0) RuleCore.BeginTurn(ctx2);
        int me2 = ctx2.Active;
        Place(ctx2, me2, 3, E8bAbilityCard("E8bAny2", "Heal 2 FriendlyUnit", E8bCrit(1, 1)));
        var mate2 = Place(ctx2, me2, 7, Unit("E8bMateAny2", 1, 1, 5));
        mate2.Health = 2;
        int owner2;
        CheckCode(RuleCore.CanUseAbility(ctx2, me2, 3, 7, out owner2), RuleCodes.OK,
                  "★★ ④ 任意档：对面同号格空、自己有人 ⇒ 仍可点（落到第二侧）");
        Check(owner2, me2, "★★★ ④ 落到**第二个候选**（自己那一侧）｜🧨 只试第一侧 ⇒ 红");
        CheckCode(RuleCore.UseAbility(ctx2, me2, 3, 7), RuleCodes.OK, "★★ ④ 放得出来（第二侧）");
        Check(mate2.Health, 4, "★★ ④ 自己人 2 → 4");
    }

    // ==================================================================
    //  ⑤ 判不出 ⇒ **退回敌方** + 出声（⛔ 不猜一个侧）
    // ==================================================================

    static void E8bUnknownFallsBackToEnemy()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
        int me = ctx.Active;
        Place(ctx, me, 3, E8bAbilityCard("E8bUnknown", "Damage 2 EnemyUnit", E8bCrit(1, 0), versioned: false));
        var foe = Place(ctx, 1 - me, 5, Unit("E8bFoe3", 1, 1, 5));

        int before = RuleCore.AbilityPickOwnerWarningsForTest;
        int owner;
        CheckCode(RuleCore.CanUseAbility(ctx, me, 3, 5, out owner), RuleCodes.OK,
                  "★★ ⑤ criteria 列没落 ⇒ **退回敌方**（= 改前行为，老夹具零影响）");
        Check(owner, 1 - me, "★★ ⑤ 解到对面");
        CheckTrue(RuleCore.AbilityPickOwnerWarningsForTest > before,
                  "★★ ⑤ 这一档**必须出声**（⛔ 不许静默猜侧）｜🧨 删掉 `NoteAbilityPickOwnerGap` ⇒ 红");
        CheckCode(RuleCore.UseAbility(ctx, me, 3, 5), RuleCodes.OK, "★★ ⑤ 放得出来");
        Check(foe.Health, 3, "★★ ⑤ 敌人 5 → 3");
    }

    // ==================================================================
    //  ⑥ 自己档：`Self`（不用点）＋ 友方含自己（`includeSelf = 1`）
    // ==================================================================

    static void E8bSelfTargets()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
        int me = ctx.Active;
        var caster = Place(ctx, me, 3, E8bAbilityCard("E8bSelf", "Heal 3 Self", null));
        caster.Health = 3;
        CheckCode(RuleCore.CanUseAbility(ctx, me, 3), RuleCodes.OK, "★★ ⑥ `Self` 目标 ⇒ 不用点格，直接可放");
        CheckCode(RuleCore.UseAbility(ctx, me, 3), RuleCodes.OK, "★★ ⑥ 放得出来");
        Check(caster.Health, 6, "★★ ⑥ 自己 3 → 6");

        var ctx2 = ProbeBattle(new CardDef[0], new CardDef[0]);
        if (ctx2.Turn == 0) RuleCore.BeginTurn(ctx2);
        int me2 = ctx2.Active;
        var caster2 = Place(ctx2, me2, 4, E8bAbilityCard("E8bSelfAlly", "Heal 2 FriendlyUnit", E8bCrit(0, 1, includeSelf: 1)));
        caster2.Health = 2;
        int owner;
        CheckCode(RuleCore.CanUseAbility(ctx2, me2, 4, 4, out owner), RuleCodes.OK,
                  "★★ ⑥ 友方含自己：点**施法者自己那一格** ⇒ 可点（`includeSelf = 1`）");
        Check(owner, me2, "★★ ⑥ 解到自己那一侧");
        CheckCode(RuleCore.UseAbility(ctx2, me2, 4, 4), RuleCodes.OK, "★★ ⑥ 放得出来");
        Check(caster2.Health, 4, "★★ ⑥ 自己 2 → 4");
    }

    // ==================================================================
    //  ⑦ E8-2：监听器事件排在能力级闸**之后**
    // ==================================================================

    static void E8bListenerEmitAfterGates()
    {
        // ⑦·a 被闸拒：subject = 监听者眼中的**敌人**，而它的 abilityLevelCriteria 说候选只能是**自己人**
        //      ⇒ 侧闸确定不满足（`TargetCriteriaEval.Matches` ②）⇒ `EnqueueAbility` 回 `false`
        //      ⇒ 连 `EvtKind.Trigger` 一起跳过。
        {
            var rej = E8bListener("E8bLRej", "When an enemy receives a Stun, deal 2 damage to it", 291, E8bCrit(0, 1));
            CheckTrue(HasWhenKind(rej, WhenEventKind.GetsStun),
                      "⑦a （前提）合成卡正文解析出了 `GetsStun` 监听器");
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            int me = ctx.Active;
            Place(ctx, me, 2, rej);
            var foe = Place(ctx, 1 - me, 1, Unit("E8bStunFoe", 1, 1, 9));
            int warn0 = RuleCore.AbilityLevelCriteriaWarningsForTest;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.GetsStun, 1 - me, foe.Card, subject: foe);
            Check(E8bCountTrigger(ctx, "UnitStunned"), 0,
                  "★★★ ⑦a 被闸拒 ⇒ `EvtKind.Trigger(UnitStunned)` **0 条**"
                + "（原版次序 = `RawCardScript__TriggerAbilities.c:108/207` 先 `CanTriggerAbility`、"
                + "`:60` 才 `PlayTriggerTraitAnim`）"
                + "｜🧨 把 `EmitListenerTrigger` 挪回 `EnqueueAbility` 之前 ⇒ 1 条 ⇒ 红");
            CheckTrue(RuleCore.AbilityLevelCriteriaWarningsForTest > warn0,
                      "★★ ⑦a 被闸拒 ⇒ **出声**（⛔ 不许静默丢）");
        }

        // ⑦·b 放行（对照）：同一张卡只把候选侧改成「敌人」⇒ 恰好一条
        {
            var ok = E8bListener("E8bLOk", "When an enemy receives a Stun, deal 2 damage to it", 291, E8bCrit(1, 0));
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            int me = ctx.Active;
            Place(ctx, me, 2, ok);
            var foe = Place(ctx, 1 - me, 1, Unit("E8bStunFoe2", 1, 1, 9));
            RuleCore.BroadcastWhen(ctx, WhenEventKind.GetsStun, 1 - me, foe.Card, subject: foe);
            Check(E8bCountTrigger(ctx, "UnitStunned"), 1,
                  "★★ ⑦b 放行 ⇒ **恰好 1 条** `EvtKind.Trigger(UnitStunned)`"
                + "｜🧨 把 `if (queued)` 写成恒假 ⇒ 0 条 ⇒ 红（两条一起看才排除「恒不发」）");
        }

        // ⑦·c 闸判不了（旧表 / 这一列没落）⇒ **仍然发**（⛔ 不把「不知道」当「不满足」）
        {
            var unk = E8bListener("E8bLUnk", "When an enemy receives a Stun, deal 2 damage to it", 291, E8bCrit(0, 1), versioned: false);
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            int me = ctx.Active;
            Place(ctx, me, 2, unk);
            var foe = Place(ctx, 1 - me, 1, Unit("E8bStunFoe3", 1, 1, 9));
            RuleCore.BroadcastWhen(ctx, WhenEventKind.GetsStun, 1 - me, foe.Card, subject: foe);
            Check(E8bCountTrigger(ctx, "UnitStunned"), 1,
                  "★★ ⑦c 闸判不了 ⇒ **仍然发** 1 条（放行 + 出声）"
                + "｜🧨 把「判不了」当成「不满足」⇒ 0 条 ⇒ 红");
        }
    }
}
