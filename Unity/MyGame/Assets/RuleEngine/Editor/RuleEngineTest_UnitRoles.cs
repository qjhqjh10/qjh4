// RuleEngineTest_UnitRoles.cs — 引擎线 `新账-95` #2：`unitConditions` 残余 8 个 role 的断言
// （2026-10-24 · 执行代理 N2）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_Order` / `_BoardModel` / `_S14` / `_S23_*` / `_Priority` / `_EntryPump` / `_Diag` 都在），
// 而**同一时刻只能有一个写手改一个文件** ⇒ 本批测试体全写在这里，`RuleEngineTest.cs` **只加
// `Run()` 里一行 `Step(...)`**。
//
// 判据（⛔ 不要重新论证）→ `资料/普查产出_1023e/A1_引擎四件.md` §2 ＋ 本批报告
// `资料/普查产出_1023e/N2_八个role.md` 的逐 role 表：
//   · `mult`    = `AbilityLogic.multiplier`（`AbilityLogic__PlayAbility.c:181-236`：
//                 `GetTargets(multiplier)` 的**个数** = `local_178`，`any(0)` ⇒ 1，
//                 `210/220` ⇒ 累加 `traitsFilter`；随后 `× furtherMultiplier`）；
//   · `multi`   = `CardAbility.multiCountCriteria`（`+0x40`；`RawCardScript__TriggerAbilities.c:230-250`：
//                 `targetsAffected != 0` ⇒ **整条 ability 触发 N 遍**，`0` ⇒ 只触发一次）；
//   · `avail`   = `RawCardScript.targetsAvailableCounter`（`+0x298`；原版**只服务 UI**：
//                 `BattleCardUI__ShowAvailableTargets.c:42`）；
//   · `choice`  = `RawCardScript.choicesDefinition`（`+0x2A0`；`BattleManager__GetChoiceFullPool.c:28-35`）；
//   · `nontarget` / `targetscore` = `ScoringCriteria`（`AI__ScoreFromPlayingNow.c:32/:54` →
//                 `AI__ScoreFromCriteria.c`），数据在 `Resources/ai_scoring.json`（**不在** `cards_engine.json`）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 **期望值一律写死字面量**（`10` / `60` / `3` / `2` / `6`）—— ⛔ 不从实现常量反算（那会自证）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestN2UnitConditionRoles)`）。
    /// 五个子用例是 `N2…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestN2UnitConditionRoles()
    {
        Section("🆕 N2 / `新账-95` #2：`unitConditions` 残余 8 个 role 的逐 role 判别式");
        N2RoleMult();
        N2RoleMulti();
        N2RoleAvail();
        N2RoleChoice();
        N2RoleScoring();
    }

    // ==============================================================================================
    //  ① `mult` —— 原版 `abilityLogic[].multiplier`
    // ==============================================================================================

    /// <summary>
    /// **数据 + 消费端**两条腿：
    /// · 数据：`GOF74` 的 `mult` 行带**整条 criteria**（`10 allFiltered / enemyUnits / includeHero
    ///   / includeMinions / cond 40 damaged`）—— 只有 `cond` 时只能回答「条件是什么」，
    ///   回答不了「数谁」（`TAU75` 的同一格数的是「受伤的敌人」）；
    /// · 消费端：把正文那条 `for each` 从句**摘掉**（`CountRef` 清空）之后，`repeats` 必须由
    ///   结构化乘数给 —— 2 个受伤敌人 ⇒ 结算 2 遍（`+1 Health` ×2）；3 个敌人**都没受伤** ⇒ 0 遍。
    /// </summary>
    static void N2RoleMult()
    {
        var pool = CardDatabase.Load();
        var gof74 = CardDatabase.FindById(pool, "GOF74");
        CheckTrue(gof74 != null, "(前提) GOF74 Grizzled Skarboy 在池内");
        if (gof74 == null) return;

        CardDef.ConditionSpec mult = null;
        foreach (var r in gof74.Conditions)
            if (r != null && r.Role == "mult") mult = r;
        CheckTrue(mult != null, "★ GOF74 带一条 `role=mult` 的行");
        if (mult == null) return;
        CheckTrue(mult.MultiplierSet != null,
                  "★★ `mult` 行带**整条 criteria**（`multiplierSet`）—— v22 之前只有 `cond`，"
                + "「数谁」读不出来｜🧨 生成器漏落 `multiplierSet` ⇒ 本行红");
        if (mult.MultiplierSet == null) return;
        var ms = mult.MultiplierSet;
        Check(ms.TargetsAffected, 10, "★ `multiplier.targetsAffected = 10 (allFiltered)`（不是 0 = any ⇒ 会数个数）");
        Check(ms.EnemyUnits, 1, "★ 数的是**敌方**（`enemyUnits = 1`）");
        Check(ms.FriendlyUnits, 0, "★ 不是友方（`friendlyUnits = 0`）");
        Check(ms.IncludeHero, 1, "★ 含督军（`includeHero = 1`）");
        Check(ms.IncludeMinions, 1, "★ 含部队（`includeMinions = 1`）");
        Check(mult.Condition, 40, "★ 条件是 `UnitConditions.damaged(40)`");
        CheckTrue(mult.Exclude == false, "★ 不取反（`excludeCondition = 0`）");

        // ---- op 归位：GOF74 **没有** `role=target` 行 ⇒ 走 v22 新加的「只标块下标」那一支 ----
        var ops = EffectText.Parse(gof74.Desc, out _, out _, card: gof74);
        Check(ops.Count, 2, "(前提) GOF74 解出两条 op（`Give Armour 1` / `+1 Health for each damaged enemy`）");
        if (ops.Count != 2) return;
        CheckTrue(ops[1].AbilityIndex == 0 && ops[1].LogicIndex == 1,
                  "★★ 第二条 op 归位到 `(ab=0, lg=1)`（= `mult` 行的位置）"
                + "｜🧨 `AssignAbilityIndices` 在「没有 `target` 行」时直接 return（v22 之前的行为）⇒ 本行红"
                + "，结构化乘数**一条都取不到**");

        // 🔴 夹具：摘掉正文的 `for each` 从句，让结构化乘数**独自**决定遍数。
        var bare = ops[1].Clone();
        bare.CountRef = null;
        bare.CountScope = null;
        bare.PerCount = 0;

        // ---- A：2 个**受伤**敌人 ⇒ 按结构化乘数结算 **2** 遍（`+1 Health` ×2）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            var w = ctx.Players[0].Warlord;
            int before = w.MaxHealth;
            // 🔴 夹具归零（2026-10-24 · 修复代理 P1）：`ProbeBattle` 的牌库**只有督军一张** ⇒
            //   开局抽牌立刻吃疲劳（实测 1+2+3+4 = 10 点）⇒ **敌方督军天然是 20/30 = `damaged(40)`**。
            //   而 `GOF74` 的 `mult` 行 `includeHero = 1` ⇒ 它**会被数进去**。
            //   ⇒ 不补满的话，「2 个受伤的敌人」这片场其实有 **3** 个受伤的敌人（原断言实得 3、红）。
            //   ⛔ 这不是实现错：结构化乘数、`conditionsFilter`、`includeHero` 全都按数据在跑
            //   （读数 → `资料/普查产出_1023e/P1_引擎三条红.md` §2）。
            N2FullEnemyWarlord(ctx);
            var e0 = Place(ctx, 1, 0, Unit("N2_mult_hurt0", 1, 1, 5));
            var e1 = Place(ctx, 1, 1, Unit("N2_mult_hurt1", 1, 1, 5));
            Place(ctx, 1, 2, Unit("N2_mult_full", 1, 1, 5));      // 满血 ⇒ **不该被数**
            e0.Health = 3; e1.Health = 3;
            List<string> un;
            RuleCore.ResolveOps(ctx, 0, null, new List<EffectOp> { bare }, null, out un, sourceCard: gof74);
            Check(w.MaxHealth - before, 2,
                  "★★ 2 个受伤的敌人 ⇒ 乘数 **2**（督军生命上限 +2）"
                + "｜🧨 数的是「全部敌人」(3) ⇒ 实得 3；不看条件 ⇒ 也是 3；结构乘数没接上 ⇒ 1（repeats 初值）");
        }
        // ---- B：3 个敌人**都满血** ⇒ 0 个受伤 ⇒ **一遍都不结算**（该筛的筛掉）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            var w = ctx.Players[0].Warlord;
            int before = w.MaxHealth;
            N2FullEnemyWarlord(ctx);      // 同上：不补满的话敌方督军自己就是「那 1 个受伤的敌人」
            Place(ctx, 1, 0, Unit("N2_mult_a", 1, 1, 5));
            Place(ctx, 1, 1, Unit("N2_mult_b", 1, 1, 5));
            Place(ctx, 1, 2, Unit("N2_mult_c", 1, 1, 5));
            List<string> un;
            RuleCore.ResolveOps(ctx, 0, null, new List<EffectOp> { bare }, null, out un, sourceCard: gof74);
            Check(w.MaxHealth - before, 0,
                  "★★ 敌人**一个都没受伤** ⇒ 乘数 0 ⇒ 一遍都不结算（督军生命上限不变）"
                + "｜🧨 忽略 `conditionsFilter` ⇒ 实得 3（全部敌人都算）");
        }
    }

    /// <summary>
    /// 把**敌方督军补满血**，并断一条前提 —— 两件事：
    /// ① 夹具归零：`ProbeBattle` 的牌库只有督军 ⇒ 开局疲劳 10 点 ⇒ 敌方督军天然 `damaged(40)`，
    ///    而 `mult` 行的 `includeHero = 1` 会把它数进去（本文件 `N2RoleMult` 两条断言因此红过）；
    /// ② **前提断言**：「一个受伤的敌人都不存在」这件事必须**看得见** —— 否则补满这一步被谁去掉，
    ///    两条断言会重新变假红（而不是静默漂移）。
    /// </summary>
    static void N2FullEnemyWarlord(BattleContext ctx)
    {
        var foe = ctx.Players[1].Warlord;
        foe.Health = foe.MaxHealth;
        CheckTrue(foe.Health == foe.MaxHealth,
                  "(前提) 敌方督军已补满 " + foe.MaxHealth + "/" + foe.MaxHealth
                + "（`mult` 行 `includeHero = 1` ⇒ 它若带伤就会被数成一个「受伤的敌人」）");
    }

    // ==============================================================================================
    //  ② `multi` —— 原版 `CardAbility.multiCountCriteria`
    // ==============================================================================================

    /// <summary>
    /// `UM77` 的 `multi` 行 `targetsAffected = 0 (any)` ⇒ 原版 `RawCardScript__TriggerAbilities.c:230-250`
    /// 那一支 `goto` 到「**只触发一次**」⇒ **本卡这一格今天不是重复源**；真正的重复来自
    /// `mult` 行（`targetsAffected = 60 inCemeteryFiltered`）。两条一起断，才分得开
    /// 「哪一格在驱动遍数」。
    /// </summary>
    static void N2RoleMulti()
    {
        var pool = CardDatabase.Load();
        var um77 = CardDatabase.FindById(pool, "UM77");
        CheckTrue(um77 != null, "(前提) UM77 Chairon 在池内");
        if (um77 == null) return;

        CardDef.ConditionSpec multi = null, mult = null;
        foreach (var r in um77.Conditions)
        {
            if (r == null) continue;
            if (r.Role == "multi") multi = r;
            if (r.Role == "mult") mult = r;
        }
        CheckTrue(multi != null && multi.MultiSet != null,
                  "★★ UM77 带 `role=multi` 行且落了整条 criteria（`multiSet`）"
                + "｜🧨 生成器漏落 `multiSet` ⇒ 本行红");
        if (multi != null && multi.MultiSet != null)
        {
            Check(multi.MultiSet.TargetsAffected, 0,
                  "★★ `multiCountCriteria.targetsAffected = 0 (any)` ⇒ 原版**只触发一次**"
                + "（`TriggerAbilities.c:230-236` 那一支 `goto` 到单次触发）"
                + "｜🧨 把这一格当成「数一遍」/ 拿 `10` 顶 ⇒ 本行红");
            Check(multi.MultiSet.FriendlyUnits, 1, "★ 条件集合是**友方**（`friendlyUnits = 1`）");
            Check(multi.MultiSet.IncludeMinions, 1, "★ 只数部队（`includeMinions = 1` / `includeHero = 0`）");
            Check(multi.Condition, 222, "★ 条件是 `diedSinceLastTurn(222)`");
        }
        CheckTrue(mult != null && mult.MultiplierSet != null, "★ 同一张卡另有一条 `mult` 行（带 `multiplierSet`）");
        if (mult != null && mult.MultiplierSet != null)
        {
            Check(mult.MultiplierSet.TargetsAffected, 60,
                  "★★ 真正驱动遍数的是 `mult` 行的 `inCemeteryFiltered(60)`"
                + "（`Deal 1-2 damage … Repeat for each friendly troop that died since your last turn`）"
                + "｜🧨 把两格弄反 ⇒ 本行红");
            Check(mult.MultiplierSet.IncludeHero, 0, "★ 墓地那一支只数部队（`includeHero = 0`）");
        }
    }

    // ==============================================================================================
    //  ③ `avail` —— 原版 `RawCardScript.targetsAvailableCounter`（**只服务 UI**）
    // ==============================================================================================

    /// <summary>
    /// 判据结论 = 「**引擎侧没有消费点**，原版唯一读点是 `BattleCardUI__ShowAvailableTargets.c:42`」
    /// ⇒ 本批**不建**引擎消费点（`CardPresentation` 不归本代理）。
    /// 但**数据必须落全**，而且要能证明「同一批单位已经由 ability 级的 `target` 行筛过」——
    /// 否则「引擎没做」会被误读成「这些单位没被筛」。三张卡逐张断 `availSet` 与同卡 `target` 行的
    /// `cond` 相同。
    /// </summary>
    static void N2RoleAvail()
    {
        var pool = CardDatabase.Load();
        string[] ids = { "EC39", "SAU11", "SOR42" };
        int checkedCards = 0;
        foreach (var id in ids)
        {
            var card = CardDatabase.FindById(pool, id);
            CheckTrue(card != null, "(前提) " + id + " 在池内");
            if (card == null) continue;
            CardDef.ConditionSpec avail = null;
            var targetConds = new List<int>();
            foreach (var r in card.Conditions)
            {
                if (r == null) continue;
                if (r.Role == "avail") avail = r;
                if (r.Role == "target") targetConds.Add(r.Condition);
            }
            CheckTrue(avail != null && avail.AvailSet != null,
                      "★ " + id + " 带 `role=avail` 行且落了整条 criteria（`availSet`）"
                    + "｜🧨 生成器漏落 `availSet` ⇒ 本行红");
            if (avail == null || avail.AvailSet == null) continue;
            checkedCards++;
            Check(avail.AvailSet.MaxTargets, 0,
                  "★★ " + id + " 的 `availSet.maxTargets = 0`（`targetsAvailableCounter` 没有这一格）"
                + "｜🧨 把 `choicesDefinition` 误读成 `targetsAvailableCounter` ⇒ 实得 3（红的）");
            CheckTrue(targetConds.Contains(avail.Condition),
                      "★★ " + id + " 的同一条条件（cond=" + avail.Condition + "）也落在 ability 级 `target` 行上 "
                    + "⇒ 「引擎没做 avail」**不等于**这些单位没被筛（同一个条件、同一批候选）"
                    + "｜🧨 卡表里两处 cond 不一致 ⇒ 本行红（那时的结论就不成立了）");
        }
        Check(checkedCards, 3, "★ 三张 `avail` 卡全部核过（EC39 / SAU11 / SOR42）");
    }

    // ==============================================================================================
    //  ④ `choice` —— 原版 `RawCardScript.choicesDefinition`
    // ==============================================================================================

    /// <summary>
    /// `EC39` / `SOR42`：`choicesDefinition = {60 inCemeteryFiltered, friendly, minions,
    /// cond 222 diedSinceLastTurn, maxTargets 3}`。我们的选牌池走**正文** `choosecard/dead/{CDS}`
    /// ⇒ 两处必须指向**同一批**候选（否则「数据说 A、实际问 B」）。
    /// </summary>
    static void N2RoleChoice()
    {
        var pool = CardDatabase.Load();
        string[] ids = { "EC39", "SOR42" };
        foreach (var id in ids)
        {
            var card = CardDatabase.FindById(pool, id);
            CheckTrue(card != null, "(前提) " + id + " 在池内");
            if (card == null) continue;
            CardDef.ConditionSpec ch = null;
            foreach (var r in card.Conditions)
                if (r != null && r.Role == "choice") ch = r;
            CheckTrue(ch != null && ch.ChoiceSet != null, "★ " + id + " 带 `role=choice` 行且落了 `choiceSet`");
            if (ch != null && ch.ChoiceSet != null)
            {
                Check(ch.ChoiceSet.MaxTargets, 3,
                      "★★ " + id + " 的 `choiceSet.maxTargets = 3`（面板摆 3 张）"
                    + "｜🧨 与 `availSet` 弄反 ⇒ 实得 0（红的）");
                Check(ch.ChoiceSet.TargetsAffected, 60,
                      "★ 候选域 = `inCemeteryFiltered(60)`（墓地）");
                Check(ch.Condition, 222, "★ 条件 = `diedSinceLastTurn(222)`");
                Check(ch.ChoiceSet.FriendlyUnits, 1, "★ 只数友方");
            }
            // ---- 正文那一半：`choosecard` 的候选域必须也是「墓地 + 自上个回合起」----
            var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
            bool deadSinceLast = false;
            foreach (var op in ops)
                if (op != null && op.Verb == "choosecard" && op.ChooseSrc == "dead"
                    && op.ChooseDeadScope == "since_last_turn") deadSinceLast = true;
            CheckTrue(deadSinceLast,
                      "★★ " + id + " 的正文解析出 `choosecard/dead/{CDS:since_last_turn}` ⇒ 与 `choiceSet` "
                    + "（墓地 + `diedSinceLastTurn`）指向**同一批**候选"
                    + "｜🧨 解析层丢掉 `since_last_turn`（候选变成「本局阵亡」）⇒ 本行红");
        }
    }

    // ==============================================================================================
    //  ⑤ `nontarget` / `targetscore` —— 原版 `ScoringCriteria`（AI 评分面）
    // ==============================================================================================

    /// <summary>
    /// 两件事：
    /// · **数据在另一份文件里**：`unitConditions` 的这两格与 `Resources/ai_scoring.json` 的
    ///   `targetCriteria.conditionsFilter / excludeCondition` **逐条相同**（26/26）—— 后者才是消费者
    ///   （`AiScoring.ScoreFromPlayingNow` → `AiTargets.MeetsCondition` → `ConditionEval`）。
    ///   ⛔ 别在 `unitConditions` 上再接一个消费者（两处写同一条规则 = 迟早不一致）。
    /// · **消费者真的按条件筛**：`ASH47` 的 `nonTargetScoringCriteria` = `Count × multiplyByFriend(3)`，
    ///   条件是 `canAttack(10)` ⇒ 把两个友方单位打上 `Exhausted`，分数必须**少 6**。
    /// </summary>
    static void N2RoleScoring()
    {
        var pool = CardDatabase.Load();
        int rows = 0, matched = 0;
        var bad = new List<string>();
        foreach (var card in pool)
        {
            foreach (var r in card.Conditions)
            {
                if (r == null) continue;
                if (r.Role != "nontarget" && r.Role != "targetscore") continue;
                rows++;
                var dto = AiScoringData.Get(card.Id);
                if (dto == null) { bad.Add(card.Id + "：`ai_scoring.json` 里没有这张卡"); continue; }
                var list = r.Role == "nontarget" ? dto.nonTargetScoringCriteria : dto.cardTargetScoringCriteria;
                if (list == null || r.Logic < 0 || r.Logic >= list.Length)
                {
                    bad.Add(card.Id + "：" + r.Role + " 的第 " + r.Logic + " 条不存在");
                    continue;
                }
                var tc = list[r.Logic] != null ? list[r.Logic].targetCriteria : null;
                int cf = tc != null ? tc.conditionsFilter : 0;
                int ex = tc != null && tc.excludeCondition ? 1 : 0;
                if (cf != r.Condition || ex != (r.Exclude ? 1 : 0))
                {
                    bad.Add(card.Id + "：`unitConditions`=(" + r.Condition + "," + (r.Exclude ? 1 : 0)
                          + ") vs `ai_scoring.json`=(" + cf + "," + ex + ")");
                    continue;
                }
                matched++;
            }
        }
        Check(rows, 26, "★ 池内 `nontarget` + `targetscore` 行合计 **26** 条（20 + 6）");
        Check(matched, rows,
              "★★ 这 26 条与 `ai_scoring.json` 的 `conditionsFilter / excludeCondition` **逐条相同**"
            + "（消费者 = `AiScoring` → `AiTargets.MeetsCondition` → `ConditionEval`）"
            + "｜🧨 两份数据源漂移 ⇒ 本行红" + (bad.Count > 0 ? "（首条：" + bad[0] + "）" : ""));

        // ---- 消费者真的在按条件筛：`Exhausted` 的两个友方单位不计分 ----
        // ⚠️ 「友方」= `ctx.Active`（`AiScoring.thisSide`）—— 夹具的先手**不保证是 0**
        //    （两边督军 initiative 相同 ⇒ 先后手由 `FirstSeatRules` 的兜底决定）⇒ 按 `ctx.Active` 摆。
        var ash47 = CardDatabase.FindById(pool, "ASH47");
        CheckTrue(ash47 != null, "(前提) ASH47 Bladestorm 在池内");
        if (ash47 == null) return;
        var c1 = ProbeBattle(new CardDef[0], new CardDef[0]);
        int side = c1.Active;
        var u0 = Place(c1, side, 0, Unit("N2_score_a", 1, 2, 5));
        var u1 = Place(c1, side, 1, Unit("N2_score_b", 1, 2, 5));
        float sFresh = AiScoring.ScoreFromPlayingNow(c1, ash47, null, -1, -1);
        u0.Exhausted = true; u1.Exhausted = true;
        float sTired = AiScoring.ScoreFromPlayingNow(c1, ash47, null, -1, -1);
        CheckFloatDelta(sFresh - sTired, 6f,
                        "★★ `ASH47` 的 `nontarget` 评分 = `Count × multiplyByFriend(3)`、条件 `canAttack` ⇒ "
                      + "把 2 个友方单位打成已行动，分数必须**少 6**"
                      + "｜🧨 评分不看 `conditionsFilter` ⇒ 差值 0（红）；`multiplyByFriend` 没用上 ⇒ 差值 2（红）");
    }

    /// <summary>浮点比较（`float` 的 `==` 在本仓被禁 —— 见其它分册的 `CheckFloat` 口径）。
    /// ⚠️ 这里只给本批那几条**整数级**的评分差值用（期望值全是 0.5 的整数倍）。</summary>
    static void CheckFloatDelta(float actual, float expected, string msg)
    {
        bool ok = System.Math.Abs(actual - expected) < 0.001f;
        CheckTrue(ok, msg + "（实得 " + actual.ToString("F3") + " · 期望 " + expected.ToString("F3") + "）");
    }
}
