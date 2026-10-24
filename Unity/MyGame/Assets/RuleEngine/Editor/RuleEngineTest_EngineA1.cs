// RuleEngineTest_EngineA1.cs — 引擎线 `A1` 五件的断言（2026-10-24）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_Order` / `_BoardModel` / `_S14` / `_S23_*` / `_Priority` / `_EntryPump` / `_UnitRoles` /
// `_Diag` 都在），而**同一时刻只有一个写手改一个文件** ⇒ 本批测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestA1EngineFive)`**。
//
// 判据（⛔ 不要重新论证）：
//   ① `TargetsAffected.previousAbilityFiltered = 150`
//      = `AbilityLogic__SetPreviousAbilityTargets.c:19-28`（整表覆盖）
//      + `AbilityLogic__GetTargets.c:867-882`（候选池 = 静态表）
//      + `FilterMethods__CheckIfMeetsCriteria.c:172-183`（候选必须在表里）；
//   ② `UM77` 的「died since last turn」= `CardScript__HasDiedSinceLastTurn.c:26-47`
//      （`turnDied ∈ {turnCounter, turnCounter-1}`）—— 与 `cond 222` 同一口径；
//   ③ 8 张 `multiCountCriteria.targetsAffected != 0` 的逐张判据 →
//      `RawCardScript__TriggerAbilities.c:230-250`（`!= 0` ⇒ 整条 ability 触发 N 遍，
//      `0(any)` ⇒ 只触发一次）；卡面与结构化数据见 `资料/普查产出_1023e/N2_八个role.md` §五·④；
//   ④ 回放补开回合 → `资料/普查产出_1023e/A29_回放展开.md` §6.4；
//   ⑤ 缺列 `abilityContext` → `CardAbility__IsRightContext.c:20-30`
//      （`Play(0)` 三闸；`+0x65 = EntityScript.isRemnant`，`dump.cs:22001`）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量，⛔ 不从实现常量反算（那会自证）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestA1EngineFive)`）。
    /// 五个子用例是 `A1…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestA1EngineFive()
    {
        Section("🆕 引擎线 A1 五件：`previousAbilityFiltered(150)` · 自上一回合阵亡 · 8 张 `multi` 逐张 · "
              + "回放补开回合 · 缺列 `abilityContext`");
        A1PreviousAbilityTargets();
        A1DiedSinceLastTurn();
        A1MultiCountEightCards();
        A1ReplayBeginTurn();
        A1MissingAbilityContext();
    }

    // ==============================================================================================
    //  ① `targetsAffected = 150 (previousAbilityFiltered)` —— 候选 = 上一条能力的目标集合
    // ==============================================================================================

    /// <summary>
    /// 三条腿：
    /// · **正例**：上一跳打过 2 个友方部队 + 1 个敌方部队 ⇒「友方部队」数出 **2**、「敌方部队」数出 **1**；
    /// · **反例（不在表里）**：场上还有第 3 个友方部队，但它**不是**上一跳的目标 ⇒ **不进计数**；
    /// · **判不了**：表里有**不在场上**的候选（原版表可以装手牌 / 牌库的卡）⇒ `null` + `gap` 出声
    ///   （⛔ 不静默少数）。
    /// </summary>
    static void A1PreviousAbilityTargets()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var f0 = Place(ctx, 0, 1, Unit("A1PrevF0", 1, 1, 3));
        var f1 = Place(ctx, 0, 2, Unit("A1PrevF1", 1, 1, 3));
        var f2 = Place(ctx, 0, 3, Unit("A1PrevF2_not_target", 1, 1, 3));   // **不在**上一跳目标里
        var e0 = Place(ctx, 1, 1, Unit("A1PrevE0", 1, 1, 3));

        ctx.PreviousAbilityTargets.Clear();
        ctx.PreviousAbilityTargets.Add(f0);
        ctx.PreviousAbilityTargets.Add(f1);
        ctx.PreviousAbilityTargets.Add(e0);

        // ---- 正例：友方部队 ----
        var own = A1CountSet(150, friendly: 1, enemy: 0, hero: 0, minions: 1);
        string gap;
        Check(CountEval.CountTargets(ctx, 0, own, 0, null, out gap), 2,
              "★ ① 上一跳打过 2 个友方部队 ⇒ `previousAbilityFiltered` 数出 **2**"
            + "｜🧨 把 `ta == 150` 的分支删掉（落到 `targetsAffected=150 本版未实现` 的 null）⇒ 红；"
            + "把 `PreviousAbilityTargets` 当成「全场友军」⇒ 实得 3（`A1PrevF2_not_target` 被错算）⇒ 红");

        // ---- 正例：敌方部队 ----
        var foe = A1CountSet(150, friendly: 0, enemy: 1, hero: 0, minions: 1);
        Check(CountEval.CountTargets(ctx, 0, foe, 0, null, out gap), 1,
              "★ ① 同一张表里**敌方**只数出 **1**（阵营筛走的是 `CheckIfMeetsCriteria` 那一条）"
            + "｜🧨 忽略 `friendlyUnits`/`enemyUnits` ⇒ 实得 3 ⇒ 红");

        // ---- 反例：表为空 ⇒ 0 ----
        ctx.PreviousAbilityTargets.Clear();
        Check(CountEval.CountTargets(ctx, 0, own, 0, null, out gap), 0,
              "★ ① 表空 ⇒ **0**（⛔ 不是「判不了」也不是「全场友军」）"
            + "｜🧨 空表时回退到盘面 ⇒ 实得 3 ⇒ 红");

        // ---- 判不了：候选不在场上（原版可以是抽到的牌）----
        var offBoard = new UnitState(ctx.NewInstance(Unit("A1PrevHandCard", 1, 1, 1)), false);
        ctx.PreviousAbilityTargets.Add(offBoard);
        var r = CountEval.CountTargets(ctx, 0, own, 0, null, out gap);
        CheckTrue(r == null && !string.IsNullOrEmpty(gap),
                  "★ ① 表里有**不在场上**的候选 ⇒ `null` + `gap` 出声"
                + "（原版 `previousAbilityTargets` 装的是 `CardScript`，**可以是手牌/牌库的牌** —— "
                + "`SW51` 的上一跳正是 `Draw 2 cards`）"
                + "｜🧨 把这一支静默跳过（当 0 个数）⇒ 返回 0 ⇒ 红；🧨 拿盘面顶替 ⇒ 返回 2（假绿）");
    }

    /// <summary>手搓一条只填了「150 + 阵营/卡种」的 `countSet` —— 其余筛维度必须显式填 `0`
    /// （`CountSetSpec` 的默认是 `Unknown(-1)`，而 `SetGap` 会把任何非 0 当「本版没实现的筛」）。
    /// </summary>
    static CardDef.CountSetSpec A1CountSet(int ta, int friendly, int enemy, int hero, int minions)
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = 0,
            TargetsAffected = ta,
            ChosenArmy = 0, ArmyFilter = 0, KeywordFilter = 0, TraitsFilter = 0,
            MaxTargets = 0, TargetSelectMethod = 0,
            AttackFilter = 0, AttackFilterOption = 0,
            CurrentHealthFilter = 0, CurrentHealthFilterOption = 0,
            ManaCostFilter = 0, ManaCostFilterOption = 0,
            RarityFilter = 0, RarityFilterOption = 0,
            SpellType = 0,
            FriendlyUnits = friendly, EnemyUnits = enemy,
            IncludeHero = hero, IncludeMinions = minions,
            IncludeSpells = 0, IncludeWhispers = 0, IncludeWeapons = 0,
            IncludeRemnants = 0, IncludeHeroParts = 0,
            IncludeSelf = 0, ExcludeNotSelf = 0,
            ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0,
            ExcludeRace = 0, ExcludeTrait = 0,
            SelectFromChooseResolution = 0,
            FilterSpecificCardIds = null, ExcludeEnchantmentIds = null,
        };
    }

    // ==============================================================================================
    //  ② 「自上一回合以来的阵亡」 vs 「本回合阵亡」
    // ==============================================================================================

    /// <summary>
    /// **窗口 = 本回合 + 上一回合**（`CardScript__HasDiedSinceLastTurn.c:26-47`），
    /// 而 `ctx.DiedThisTurn` 的语义**一个字不动**（还是「本回合」）。
    /// 夹具：第 1 回合（P0 行动）让 P0 砍死 P1 的一只 1 血兵 ⇒
    ///   · 本回合（`Turn = 1`）：`diedsincelastturn` 数出 1 · `died` 也 1；
    ///   · 过一个回合（`Turn = 2`）：`diedsincelastturn` **仍 1**（`Turn-1` 那格）· `died` **0**；
    ///   · 再过一回合（`Turn = 3`）：两个都 0（窗口关上）。
    /// 侧筛也断一半（`own|troop` 对死者那一方才算）。
    /// </summary>
    static void A1DiedSinceLastTurn()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        Check(ctx.Turn, 1, "② （前提）现在第 1 回合");
        var killer = Place(ctx, 0, 3, Unit("A1DsKiller", 1, 3, 10), exhausted: false);
        var victim = Place(ctx, 1, 3, Unit("A1DsVictim", 1, 0, 1), exhausted: true);
        Check(ctx.DiedThisTurn, 0, "② （前提）开局没人阵亡");

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "② 砍死 P1 的 1 血兵");
        Check(ctx.DiedThisTurn, 1, "② （前提）`DiedThisTurn = 1`（这一格**语义未改**）");
        CheckTrue(killer.Health > 0 && victim.Health <= 0, "② （前提）只死了受害者");

        var since = new EffectOp { CountScope = "diedsincelastturn", CountRef = "own|troop" };
        var thisTurn = new EffectOp { CountScope = "died", CountRef = "unit" };
        Check(RuleCore.CountForTest(ctx, 1, null, since), 1,
              "★ ② 本回合死的友军（owner=1）⇒ 「自上一回合以来」**1**");
        Check(RuleCore.CountForTest(ctx, 0, null, since), 0,
              "★ ② **侧筛**：同一批死者对 owner=0 **不算**（`own|troop`）"
            + "｜🧨 忽略 `reference` 的 `own` ⇒ 实得 1 ⇒ 红");
        Check(RuleCore.CountForTest(ctx, 1, null, thisTurn), 1, "② 同回合 `died` 也是 1（对照档）");

        PassTurn(ctx);   // → 第 2 回合
        Check(ctx.Turn, 2, "② （前提）现在第 2 回合");
        Check(ctx.DiedThisTurn, 0, "② `DiedThisTurn` 按回合清零（**语义未动**）");
        // 🔴 **2026-10-24 删掉一条「自证型」断言（铁律 5 · 执行代理 E6 现核）**：
        //   原来这里断 `Check(ctx.DiedLastTurn, 1, …)` —— 但 `DiedLastTurn` **生产侧没有任何读者**
        //   （唯一读点是这条自己）⇒ 它验的只是「我刚写进去的那个数」，形状上接近同义反复。
        //   原版「自上一次回合以来」是**逐卡** `CardScript.turnDied`（`HasDiedSinceLastTurn.c:19-48`）
        //   + `inCemeteryFiltered(60)`，**没有全局计数器**；我们真正的消费者是 `DeadUnits` + `DeathTurn`
        //   （`EffectResolver.cs` 的 `diedsincelastturn` 分支）。⇒ **下面那条 `CountForTest` 才是真判据**。
        Check(RuleCore.CountForTest(ctx, 1, null, since), 1,
              "★★ ② 过了**一个**回合 ⇒ 「自上一回合以来」**仍数得到**（`Turn-1` 那格）"
            + "｜🧨 把它并成 `ctx.DiedThisTurn`（= 原来的实现）⇒ 实得 0 ⇒ 红；"
            + "🧨 窗口写成「永远累计」⇒ 下面 `Turn = 3` 那一格红");
        Check(RuleCore.CountForTest(ctx, 1, null, thisTurn), 0,
              "② （对照）`died`（本回合）已经 0 —— 两条 scope **必须分开**"
            + "｜🧨 把 `diedsincelastturn` 并进 `died` 的读数 ⇒ 两条一起变 ⇒ 至少一条红");

        PassTurn(ctx);   // → 第 3 回合
        Check(ctx.Turn, 3, "② （前提）现在第 3 回合");
        Check(RuleCore.CountForTest(ctx, 1, null, since), 0,
              "★ ② 又过一个回合 ⇒ 窗口关上（**0**）｜🧨 窗口写成「本局累计」⇒ 实得 1 ⇒ 红");

        // ---- 文本分类：`since your last turn` 必须落到**独立**的 scope ----
        var ops = EffectText.Parse("Repeat for each friendly troop that died since your last turn",
                                   out _, out _);
        CheckTrue(ops != null && ops.Count > 0, "② （前提）这句解析出了 op");
        if (ops != null && ops.Count > 0)
            Check(ops[0].CountScope, "diedsincelastturn",
                  "★★ ② 文本分类 = `diedsincelastturn`（**不是** `died`）"
                + "｜🧨 删掉 `ClassifyCount` 里那一段（`that died` 会先被 ② 吃掉）⇒ 实得 `died` ⇒ 红；"
                + "🧨 把它归成 `died` 而不出声 ⇒ `UM77` 静默回到「本回合」⇒ 红");
        var ops2 = EffectText.Parse("For each one that dies, deploy a Shock Trooper", out _, out _);
        CheckTrue(ops2 != null && ops2.Count > 0, "② （前提）`For each one that dies` 解析出了 op");
        if (ops2 != null && ops2.Count > 0)
            Check(ops2[0].CountScope, "died",
                  "② 对照：`that dies` 仍是 `died`（本回合）—— 新 scope 只吃 `since … last turn`"
                + "｜🧨 把新判据写成 `Contains(\"died\")` ⇒ 这一条变 `diedsincelastturn` ⇒ 红");
    }

    // ==============================================================================================
    //  ③ 8 张 `multiCountCriteria.targetsAffected != 0` 逐张核
    // ==============================================================================================

    /// <summary>
    /// 逐张判「我们等价吗」。**判据 = 原版 `RawCardScript__TriggerAbilities.c:230-250`**
    /// （`ta != 0` ⇒ 整条 ability 触发 N 遍；`0(any)` ⇒ 只触发一次），
    /// 卡面 + 结构化数据见 `N2_八个role.md` §五·④。
    ///
    /// 本用例只钉**能判、且今天成立**的那几格（数据/解析形状），判不了的（`ASH52`）写在注释里。
    /// </summary>
    static void A1MultiCountEightCards()
    {
        var pool = CardDatabase.Load();

        // ---- DA57 / SOR45 / SW58：正文「Deploy … for each enemy <troop|unit>」→ 部署 N 个 ----
        //  原版：`ta = 10 (allFiltered)` + `enemyUnits = 1` + `includeMinions = 1`
        //  （`DA57` 的 `includeHero = 0` ⇒ troop；`SOR45`/`SW58` 的 `includeHero = 1` ⇒ unit）
        //  ⇒ 触发 N 遍 × 部署 1 = **N 个**；我们的正文是「一条 `deploy` 带 `countRef`」⇒ **等价**。
        foreach (var id in new[] { "DA57", "SOR45", "SW58" })
        {
            var c = CardDatabase.FindById(pool, id);
            CheckTrue(c != null, "(前提) " + id + " 在池内");
            if (c == null) continue;
            var ops = EffectText.Parse(c.Desc, out _, out _, card: c);
            EffectOp dep = null;
            foreach (var o in ops) if (o.Verb == "deploy") { dep = o; break; }
            CheckTrue(dep != null && dep.CountScope == "board",
                      "★ ③ " + id + "：`deploy` 带**盘面计数**（`countScope = board`）—— "
                    + "N = 敌方兵种数，与原版 `ta 10 / enemyUnits` 同源 ⇒ **等价**"
                    + "｜🧨 把 `countRef` 弄丢（部署变成一次）⇒ 红");
            if (dep != null)
                CheckTrue(dep.CountRef != null && dep.CountRef.StartsWith("enemy|"),
                          "★ ③ " + id + "：数的是**敌方**（`countRef = " + dep.CountRef + "`）"
                        + "｜🧨 数成友方 ⇒ 红");
        }

        // ---- GOF46：「Deploy one Stormboy. Repeat for each enemy unit」----
        //  原版有**两条** `trigger = 0` 的 ability：ab0（`ta = 0` ⇒ 触发一次）+ ab1（`ta = 10` ⇒ N 遍）
        //  ⇒ 总数 = **1 + N**。我们的正文 = `deploy` 一次 + `repeat[deploy]` N 遍 ⇒ **等价**
        //  （实测 `ruleprobe play GOF46`：夹具 4 个敌方单位 ⇒ 5 只 Stormboy = 1 + 4）。
        {
            var c = CardDatabase.FindById(pool, "GOF46");
            CheckTrue(c != null, "(前提) GOF46 在池内");
            if (c != null)
            {
                var ops = EffectText.Parse(c.Desc, out _, out _, card: c);
                int nDeploy = 0, nRepeat = 0;
                foreach (var o in ops) { if (o.Verb == "deploy") nDeploy++; if (o.Verb == "repeat") nRepeat++; }
                Check(nDeploy, 1, "★ ③ GOF46：正文里**一条**独立 `deploy`（= 原版 ab0 的「一次」）");
                Check(nRepeat, 1, "★ ③ GOF46：再**一条** `repeat`（= 原版 ab1 的「N 遍」）");
                CheckTrue(nDeploy == 1 && nRepeat == 1,
                          "★★ ③ GOF46 总数 = **1 + N**，与原版「两条 trigger=0 的 ability（`ta 0` 一次 + `ta 10` N 遍）」"
                        + "**等价**（实测 4 个敌人 ⇒ 5 只）"
                        + "｜🧨 把两条合成「N 遍」（总 N）或「1 + 0」⇒ 红");
            }
        }

        // ---- GSC56：「Deal 3 … Repeat this effect for each Sabotage in the opponent's hand」----
        //  原版：ab0（`ta 0`）一次 + ab1（`ta 50 inHandFiltered / enemyUnits 1 / includeSpells 1 /
        //  spellType 230`）N 遍 ⇒ 总数 **1 + N**，`N = 对手手牌里的 Sabotage 张数`。
        //  🔴 **本批改了计数来源**：改前 `countRef` 被归成 `own|any|board`（数**我方场上单位**）⇒
        //    `ruleprobe play GSC56` 打出 **15** 点伤害（1 + 4 个我方单位 = 5 次 × 3）；
        //    改后 0 张 Sabotage ⇒ **3** 点（只有 base 那一次）。
        {
            var c = CardDatabase.FindById(pool, "GSC56");
            CheckTrue(c != null, "(前提) GSC56 在池内");
            if (c != null)
            {
                var ops = EffectText.Parse(c.Desc, out _, out _, card: c);
                EffectOp rep = null;
                foreach (var o in ops) if (o.Verb == "repeat") { rep = o; break; }
                CheckTrue(rep != null, "(前提) GSC56 有一条 `repeat`");
                if (rep != null)
                {
                    Check(rep.CountScope, "enemyhand",
                          "★★ ③ GSC56 的 `repeat` 计数来源 = `enemyhand`（**对手手牌**）"
                        + "｜🧨 退回 `board`（数我方场上单位）⇒ 红 —— 改前就是这样，打出 15 伤（应 3）");
                    Check(rep.CountRef, "sabotage",
                          "★★ ③ GSC56 数的是 **Sabotage**（`spellType = 230`）"
                        + "｜🧨 认不出筛选词 ⇒ 红");
                }
            }

            // 行为：对手手里 2 张 Sabotage ⇒ 2 遍
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var sab = new CardDef("A1_Sabotage", "Sabotage", "tactic", "", "common", "Test",
                                  1, 0, 0, 0, null, spellType: 230);
            var plain = Tactic("A1_Plain", 1, "Deal 1 damage");
            ctx.Players[1].Hand.Add(ctx.NewInstance(sab));
            ctx.Players[1].Hand.Add(ctx.NewInstance(plain));
            ctx.Players[1].Hand.Add(ctx.NewInstance(sab));
            Check(RuleCore.CountForTest(ctx, 0, null,
                      new EffectOp { CountScope = "enemyhand", CountRef = "sabotage" }), 2,
                  "★★ ③ 对手手里 2 张 Sabotage + 1 张普通战术 ⇒ 数出 **2**"
                + "｜🧨 按 subtype/卡名以外的口径数（比如数全部手牌）⇒ 实得 3 ⇒ 红；"
                + "🧨 数成**我方**手牌（0 张）⇒ 实得 0 ⇒ 红");
        }

        // ---- SW51：「Draw 2 cards. For each troop drawn, give Hunt Mark …」----
        //  原版 `ta = 150 (previousAbilityFiltered)` —— 上一跳 = `Draw 2 cards`，它把抽到的牌
        //  记进 `abilityTargetsToSave` ⇒ N = **抽到的军队卡张数**。我们的正文 `countScope = draw`
        //  数的正是这件事 ⇒ **等价**（这条也是 `ta 150`「手牌候选」那一档**由正文覆盖**的证据）。
        {
            var c = CardDatabase.FindById(pool, "SW51");
            CheckTrue(c != null, "(前提) SW51 在池内");
            if (c != null)
            {
                var ops = EffectText.Parse(c.Desc, out _, out _, card: c);
                EffectOp gv = null;
                foreach (var o in ops) if (o.Verb == "give") { gv = o; break; }
                CheckTrue(gv != null && gv.CountScope == "draw" && gv.CountRef == "any",
                          "★★ ③ SW51 的正文计数 = `draw/any`（= 原版 `previousAbilityFiltered` 在**这条链**上的等价物）"
                        + "｜🧨 改成盘面计数 ⇒ 红（那会把「抽到的军队卡」数成「场上的单位」）");
            }
        }

        // ---- AM18 / ASH52：**判不了的那一半**（如实写，⛔ 不猜、不改行为）----
        //  AM18：「At the end of your turn, deal 3 … for each friendly Vehicle in play」
        //   原版 `ta = 10 / friendlyUnits 1 / includeMinions 1 / keywordFilter 10(Vehicle)` ⇒ **触发 N 遍**。
        //   🔴 我们的 **触发层**（`ResolveOps` 的 `IReadOnlyList<EffectOp>` 重载）**没有 `repeats` 循环**
        //      （只有出牌 / 战术卡那条重载有）⇒ `countRef` 在触发层**被忽略**，恒按 1 遍结算。
        //      实测同族的 `GOF4`（`Rally: Gain +1 Attack for each other friendly Slugga Boy`）：
        //      夹具里 0 个其他 Slugga Boy，原版应 **0** 点加成，实际了 **+1**（`ruleprobe play GOF4`）。
        //   ⇒ 这一格 **不等价**，补法 = 把出牌路那套 `repeats`（`CountFor` + `struct mult`）搬进触发路；
        //      而原版那一边对 `multiplier` 是**把数值乘 N**（`PlayAbility.c:708`
        //      `local_178 * damage`），对 `multiCountCriteria` 才是**触发 N 遍** ——
        //      两者对「随机选靶 / 护甲」的结果不同 ⇒ ⛔ 不能机械照搬，**留待单独立账**（本批只记）。
        {
            var c = CardDatabase.FindById(pool, "AM18");
            CheckTrue(c != null, "③ AM18 在池内（它的正文形状只做记录，见下方注释/报告）");
            if (c != null)
            {
                var ops = EffectText.Parse(c.Desc, out _, out _, card: c);
                EffectOp deal = null;
                foreach (var o in ops) if (o.Verb == "deal" || o.Verb == "atturn") { deal = o; break; }
                CheckTrue(deal != null && deal.CountRef != null && deal.CountRef.Contains("vehicle"),
                          "③ AM18 正文把「每个友方载具」解析成 `countRef = " + (deal != null ? deal.CountRef : "null") + "` —— "
                        + "**形状对**；但它走**触发层**（回合末），那一层今天不读计数 ⇒ ⚠️ **不等价**（见本函数尾部注释）");
            }
        }
        //  ASH52：「Deploy a Wraithguard. Spend all your Spirit Stones. For each one, deploy a Wraithguard.」
        //   原版 ab0（`ta 0`）部署 1；ab1（`trigger = 600 UseSpiritStone`，`ta = 161 currentSpiritStone`）
        //   每触发一次再部署 N 个 —— **但 `UseSpiritStone` 的发射点没定位到**
        //   （`PlayerManager__UseSpiritStoneEnergy.c` 只扣数、**不发触发**），而且 `ta = 161` 是
        //   「取哪一刻的剩余石头数」也没定 ⇒ ⛔ 不猜、不改行为（见报告「判不准」一节）。
    }

    // ==============================================================================================
    //  ④ 换牌期投降的老录像：回放补开回合必须能越过 `IsOver`
    // ==============================================================================================

    /// <summary>
    /// **两条腿**：
    /// · 普通 `BeginTurn` 在 `IsOver` 的局上是**空操作**（这是活局语义，⛔ 不许删闸）；
    /// · `BeginTurnForReplayExpansion` **越过那道闸**，把「录制时真的跑过」的那一拍补上
    ///   （回合 +1 / 能量摆好 / 抽 1 张）—— 否则「换牌期投降」的老录像终局指纹差一格
    ///   （`A29_回放展开.md` §6.4）。
    /// 🧨 **灭自证**：把 `BeginTurnCore` 的 `if (ctx.IsOver && !evenIfOver) return;` 改成
    /// 「只看 `evenIfOver`」或整句删掉 ⇒ 第一条（普通 `BeginTurn` 必须空操作）**必红**。
    /// </summary>
    static void A1ReplayBeginTurn()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        // ⚠️ `Forfeit` 返回 **`ctx.Winner`**（不是理由码）—— 判过之后 = 1（对面胜）
        int forfeitRc = RuleCore.Forfeit(ctx, 0);
        CheckTrue(ctx.IsOver && forfeitRc != 0,
                  "④ （前提）P0 投降 ⇒ 局已结束（`Forfeit` 返回 `Winner = " + forfeitRc + "`）");

        int turn0 = ctx.Turn;
        int hand0 = TotalHand(ctx);
        int energyActive0 = ctx.Players[ctx.Active].Energy;
        RuleCore.BeginTurn(ctx);
        Check(ctx.Turn, turn0, "★ ④ **普通 `BeginTurn` 在已结束的局上不动回合**（活局语义）"
                             + "｜🧨 删掉 `BeginTurnCore` 的 `IsOver` 闸 ⇒ 实得 " + (turn0 + 1) + " ⇒ 红");
        Check(TotalHand(ctx), hand0, "★ ④ 也不抽牌｜🧨 同上 ⇒ 多一张 ⇒ 红");
        Check(ctx.Players[ctx.Active].Energy, energyActive0, "★ ④ 也不重算能量｜🧨 同上 ⇒ 红");
        CheckTrue(ctx.IsOver, "④ 投降状态没被改掉");

        RuleCore.BeginTurnForReplayExpansion(ctx);
        Check(ctx.Turn, turn0 + 1,
              "★★ ④ 回放展开入口**越过 `IsOver`**：回合 +1（= 录制时真的跑过的那一拍）"
            + "｜🧨 把它实现成转调普通 `BeginTurn` ⇒ 实得 " + turn0 + " ⇒ 红（这正是 `A29` §6.4 那一格）");
        Check(TotalHand(ctx), hand0 + 1,
              "★★ ④ 也因此**补抽了那张**（原版 `BeginTurn` 的抽牌）"
            + "｜🧨 只改 `ctx.Turn` 不跑余下步骤（下面能量那条也断）⇒ 红");
        CheckTrue(ctx.Players[ctx.Active].MaxEnergy > 0,
                  "★★ ④ 能量按回合公式摆好（`MaxEnergy > 0`）"
                + "｜🧨 半截实现（只 `Turn++`）⇒ 能量仍是 0 ⇒ 红");
        CheckTrue(ctx.IsOver, "④ 越过闸**不等于**把这一局复活 —— `IsOver` 仍是 true");
    }

    static int TotalHand(BattleContext ctx)
    {
        int n = 0;
        for (int p = 0; p < 2; p++) n += ctx.Players[p].Hand.Count;
        return n;
    }

    // ==============================================================================================
    //  ⑤ 缺列的 `abilityContext` 按哪一档（`Play(0)`）· 以及 `Play(0)` 的第三闸 `!isRemnant`
    // ==============================================================================================

    /// <summary>
    /// **结论（判据 = 全量反编译，已在代码里落成唯一取值口）**：
    /// 生成器 `ability_contexts_of()` **只落 `context != 0` 的行** ⇒ 在有 v14 真值快照的卡上
    /// 「这一条 ability 查不到行」**不是未知，而是 `Play(0)` 被省略了**
    /// （`RuleCore.AbilityContextOfAbilityV14` 的 doc）。`-1` 只剩「旧卡表 / 第二解析路没接列」，
    /// 那时回退旧关键词表 + 泵点 `UnitInPlay`（= 出队时的 `Play(0)` 近似）。
    /// 原版对**认不出的 context 值**是 `LogWarning` 后 `return 0`（`IsRightContext.c:63-68`）——
    /// 但 `-1` 不是「认不出的值」，是「我们没有那一列」，所以走回退而不是判假。
    ///
    /// 本用例钉两件事：① 「缺行 ⇒ Play(0)」这条跳真的在供值；② `Play(0)` 的**第三闸 `!isRemnant`**
    /// （`IsRightContext.c:20-30` 的 `+0x65 == 0`，`dump.cs:22001` 的 `isRemnant`）。
    /// </summary>
    static void A1MissingAbilityContext()
    {
        var pool = CardDatabase.Load();
        CardDef versioned = null;
        foreach (var c in pool)
            if (c != null && c.AbilityContextsVersioned
                && c.AbilityTriggerPresence("Slay") == 1
                && RuleCore.AbilityContextOfAbility(c, "Slay", "slay") == CardDef.AbilityContextUnknown
                && RuleCore.AbilityContextOfAbility(c, "Strike", "strike") == CardDef.AbilityContextUnknown)
            { versioned = c; break; }
        // 🔴 **2026-10-24（执行代理 E8）**：「缺行 = `Play(0)`」现在有**前提②** —— 这张卡必须
        //   确实有这条 trigger 的 ability（`AbilityTriggerPresence == 1`）。原选的那张 `versioned`
        //   可能根本没有 `Strike` ability（`presence == 0` ⇒ 正确结果是「未知」而不是 `Play(0)`）
        //   ⇒ 下面这两条要**另找**一张 `Strike` presence==1 的卡，并补一张 presence==0 的负对照。
        CardDef strikeCard = null, noStrikeCard = null;
        foreach (var c in pool)
        {
            if (c == null || !c.AbilityContextsVersioned) continue;
            int pres = c.AbilityTriggerPresence("Strike");
            if (pres == 1 && RuleCore.AbilityContextOfAbility(c, "Strike", "strike") == CardDef.AbilityContextUnknown)
            { if (strikeCard == null) strikeCard = c; }
            else if (pres == 0 && noStrikeCard == null) noStrikeCard = c;
        }
        CheckTrue(versioned != null,
                  "⑤ （前提）池内至少有一张 `AbilityContextsVersioned = true`、且 **`Slay` / `Strike` 两行都没落** 的卡"
                + "（v14 只落非 0 行 ⇒ 这两条 ability 的 `Play(0)` 是被省略的）");
        if (versioned != null)
        {
            Check(RuleCore.AbilityContextOfAbility(versioned, "Slay", "slay"), CardDef.AbilityContextUnknown,
                  "★ ⑤ 原始访问器查不到 `Slay` 行 ⇒ `-1`（= 「没落这一行」这个事实本身）");
            Check(RuleCore.AbilityContextOfAbilityV14(versioned, "Slay", "slay"), CardDef.AbilityContextPlay,
                  "★★ ⑤ **缺行 ⇒ `Play(0)`**（v14 的省略语义）"
                + "｜🧨 把 `AbilityContextOfAbilityV14` 的「versioned ⇒ Play(0)」那一跳删掉 ⇒ 实得 -1 ⇒ 红"
                + "（= 一条真卡的能力突然按旧关键词表判，比原版更严/更松都可能）");
            // 🔴🆕 2026-10-24（判据由 C1 闭合）：`ActorMustBeInPlayForTrigger` 也必须走**补过 0 的**口
            //   —— 否则「缺列(=Play)」被当成「未知」落到旧关键词表（5 个词之外一律 `false`）
            //   ⇒ 那些路径**比原版宽**（原版 `Play(0)` 是要判的）。
            CheckTrue(strikeCard != null && noStrikeCard != null,
                      "★ ⑤ （前提）找到「`Strike` presence==1 且无非 0 行」与「presence==0」两张对照卡"
                    + "｜🧨 数据侧 `abilityTriggers` 列坏了 ⇒ 这两张找不到 ⇒ 下面两条断言没验到");
            CheckTrue(strikeCard != null && RuleCore.ActorMustBeInPlayForTrigger(strikeCard, "Strike", "strike"),
                      "★★ ⑤ `ActorMustBeInPlayForTrigger`：**有这条 ability + 缺行 ⇒ `Play(0)` ⇒ 要判**（true）"
                    + "｜🧨 把前提② 删掉 / 改回调**未补 0** 的 `AbilityContextOfAbility` ⇒ 实得 false ⇒ 红");
            CheckTrue(noStrikeCard != null && !RuleCore.ActorMustBeInPlayForTrigger(noStrikeCard, "Strike", "strike"),
                      "★★ ⑤ **负对照**：这张卡**根本没有 `Strike` ability**（presence==0）⇒ **不判**（false）"
                    + "｜🧨 把前提② 删掉（缺行一律当 `Play(0)`）⇒ 这一条变 true ⇒ 红（会把没有的能力按原版闸拦掉）");
            CheckTrue(!RuleCore.ActorMustBeInPlayForTrigger(versioned, "ThisUnitDeath", KeywordTable.Backlash),
                      "⑤ 对照：`ThisUnitDeath(80)` 在原版 skip 表里 ⇒ **整段不判**（false）"
                    + "｜🧨 把 skip 表删掉 ⇒ 这一条变 true ⇒ 阵亡信号会被「载体已离场」拦掉 ⇒ 红");
        }

        // ---- `Play(0)` 的第三闸：残骸不算合法载体 ----
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var u = Place(ctx, 0, 3, Unit("A1CtxUnit", 1, 2, 3));
        CheckTrue(RuleCore.IsRightContext(ctx, 0, 3, CardDef.AbilityContextPlay, u.Instance),
                  "⑤ （前提）普通在场单位 = `Play(0)` **合法**载体");
        ctx.Players[0].Board[3] = new UnitState(u.Instance, false) { IsRemnant = true };
        CheckTrue(!RuleCore.IsRightContext(ctx, 0, 3, CardDef.AbilityContextPlay, u.Instance),
                  "★★ ⑤ **残骸不是 `Play(0)` 的合法载体**（原版 `IsRightContext.c:20-30` 第三闸"
                + " `*(char *)(card + 0x65) == 0`；`+0x65 = EntityScript.isRemnant`）"
                + "｜🧨 把 `!cell.IsRemnant` 去掉 ⇒ 本条红（而今天零可达 ⇒ 静默、无其它断言能看见）");
        // 40 那一档仍然「含待死 / 含残骸」——确认没被顺手改严
        CheckTrue(RuleCore.IsRightContext(ctx, 0, 3, CardDef.AbilityContextInPlayOrDying, u.Instance),
                  "⑤ 对照：`InPlayOrDying(40)` 仍然认残骸（原版那一支只看 `IsInPlayOrDying`）"
                + "｜🧨 把 `!IsRemnant` 写进 40 那一档 ⇒ 红");

        // ---- 回归护栏：**活着的** Strike 载体照常结算（残骸闸不许误伤正常路径）----
        //  形状取自 `ASH13 Warp Spider`（`Strike: Return this troop to your hand`）。
        //  `ruleprobe play check` 里 `E|ASH13` 那一行的读数变了（46→44 条 `changes`）——
        //  本断言钉住「**活着**的那一半没变」；读数变化请主对话复核后重钉结算基线。
        {
            var c2 = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(c2, 1);
            Place(c2, 0, 3, Unit("A1WarpSpider", 3, 4, 3, "Strike: Return this troop to your hand"),
                  exhausted: false);
            Place(c2, 1, 3, Unit("A1PunchBag", 1, 0, 10), exhausted: true);
            CheckCode(RuleCore.DeclareAttack(c2, 0, 3, 1, 3), RuleCodes.OK, "⑤ （前提）Warp Spider 打一下");
            RuleCore.PumpActionQueues(c2);
            CheckTrue(SlotOf(c2, 0, "A1WarpSpider") < 0 && HandIdx(c2, 0, "A1WarpSpider") >= 0,
                      "★★ ⑤ **活着的 `Strike` 载体照常回手** —— 残骸闸只挡「已翻成残骸」那一档"
                    + "｜🧨 把闸写成「只要场上找不到同实例就取消」/把 `IsAlive` 那半也改严 ⇒ 红");
        }
    }
}
