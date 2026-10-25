// RuleEngineTest_EngineA3.cs — 引擎线 `A3` 三件（2026-10-25 · 第七十五会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineA2` /
// `_EngineC5` / `_G3` … 都在），同一时刻只有一个写手改一个文件 ⇒ 本批测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestA3EngineAtTurnCriteria)`**。
//
// 判据（⛔ 不要重新论证）：
//   · **A3-1** at-turn 片段正文的 `deal` / `give` 也要贴 logic 级 `targetCriteria`。
//     判据链 = `CardScript__OnTurnStart.c:180-200`（`OnTrigger(0x1e/0x1f …)`）→
//     `RawCardScript__TriggerAbilities.c:207-250`（按 `ability.trigger` 选 ability →
//     `AbilityLogic__GetTargets(criteria, …)`）→ `AbilityLogic__GetTargets.c:1086-1095`
//     （逐候选 `FilterMethods__CheckIfMeetsCriteria`）。**与打出族同一条链**。
//   · **A3-2**（只出判据、本笔**不改数据管线**）ability 级 36 格 = `CardAbility.targetCriteria`
//     （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardAbility.cs` 的序列化字段），
//     消费点是 `CardAbility__CanTriggerAbility.c:131-135` 的**触发前闸**，不是目标池筛。
//     缺的四步与影响面 → `资料/普查产出_1025/A3_引擎_at-turn与criteria管线.md` §A3-2。
//   · **A3-3** `AbilityTargetsVersioned = abilityTriggersVersioned`（`CardDef.cs` 的 ctor）——
//     **不是原版语义**（原版没有「版本位」，那是我们数据管线的形状）⇒ 只给一列时**出声**。
//
// 🔴 每条断言都带 `🧨`（什么改动能让它红）。期望值一律写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestA3EngineAtTurnCriteria)`）。
    /// 子用例用 `A3…` 打头的辅助方法（⛔ 不用 `Test`/`Report` 打头 —— 见 `_EngineA2` 同一条注释）。</summary>
    public static void TestA3EngineAtTurnCriteria()
    {
        Section("🆕 引擎线 A3：at-turn 的 `deal`/`give` criteria 贴片 · 说不清的窄闸 · 两列版本位出声");
        A3Case1AtTurnBodyCriteriaAttached();
        A3Case2AtTurnCriteriaGates();
        A3Case3AtTurnEndToEnd();
        A3Case4AbilityVersionFlagTrap();
    }

    // ==================================================================
    //  夹具小工具
    // ==================================================================

    /// <summary>按卡池里那张真卡的 **at-turn 从句正文**走一遍生产解析路
    /// （`RuleCore.ResolveAtTurn` 用的就是 `Parse(cl.Body)` —— 不带 card ⇒ 不吃
    /// `parse(desc, card:)` 那条贴片），返回 body 的 op 列表。
    /// ⚠️ 拿不到从句 / 相位对不上 ⇒ 返回 `null`（调用方那条断言会红，不会静默当 0 条）。</summary>
    static List<EffectOp> A3AtTurnBody(CardDef card, string phase)
    {
        var clauses = EffectText.AtTurnClauses(card.Desc);
        for (int i = 0; i < clauses.Count; i++)
            if (clauses[i].Phase == phase)
                return EffectText.Parse(clauses[i].Body, out _, out _);
        return null;
    }

    /// <summary>造一张「只有一条 ability、`ta=[10]`」的合成 at-turn 单位卡。
    /// `versioned` = 那条 `abilityTriggersVersioned`（**两列共用的版本位**，见 `A3-3`）。</summary>
    static CardDef A3Synthetic(string id, bool versioned, string desc)
    {
        var ta = new CardDef.AbilityTargetSpec { Trigger = 31, TargetsAffected = new[] { 10 } };
        return new CardDef(id, id, "unit", desc, "common", "Test", 1, 1, 4, 0, null,
                           subtype: "Infantry",
                           abilityTriggersVersioned: versioned,
                           abilityTargets: new[] { ta },
                           abilityTargetCriteria: versioned
                               ? new[]
                                 {
                                     new CardDef.TargetCriteriaRowSpec
                                     {
                                         Ability = 0, Trigger = 31, Logic = 0,
                                         Criteria = A3Criteria10(enemyUnits: 1, friendlyUnits: 0),
                                     },
                                 }
                               : null,
                           abilityTargetCriteriaVersioned: versioned);
    }

    /// <summary>一条**所有格都显式给值**的 `TargetCriteria`（`ta = 10 allFiltered`）。
    /// ⚠️ 必须逐格给：`CountSetSpec` 的缺省是 `Unknown(-1)`，`TargetCriteriaEval` 遇 `Unknown`
    /// 返回 `null`（判不了 ⇒ 整条不筛）⇒ 夹具会静默变成「没验到」（同 `_EngineA2` 的注释）。</summary>
    static CardDef.CountSetSpec A3Criteria10(int enemyUnits, int friendlyUnits)
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

    // ==================================================================
    //  ① `A3-1`：五张真卡 + 一张手牌陷阱 —— 贴片真的贴上了，而且**真的能筛**
    // ==================================================================

    /// <summary>
    /// 逐张（`AM18` / `AM67` / `TAU6` / `TAU7` / `TAU13` 棋盘 + `Poisoned Supplies` 手牌陷阱）：
    /// ① 生产路径解析出来的 op **不跑 helper 时一条 criteria 都没有**（灭自证的判别式）；
    /// ② 跑 `AttachAtTurnBodyCriteria` 之后挂上 `ta=10` 的 36 格 + 同格的 cond/ex；
    /// ③ 拿**挂上去的那个对象**喂 `TargetCriteriaEval`：敌方候选过、己方候选**不过**
    ///    （5 张是 `enemyUnits=1 / friendlyUnits=0`，陷阱反过来）—— 这一条**不贴就必红**
    ///    （`set == null` ⇒ `Matches` 一律 `true`），所以它是灭自证而不是同义反复。
    /// ④ 有 `excludeCondition=220`（= 将死）那三张：把候选 `Health` 打到 0 ⇒ 必须**被筛掉**。
    /// </summary>
    static void A3Case1AtTurnBodyCriteriaAttached()
    {
        var pool = CardDatabase.Load();
        // (id, phase, 期望有 220/`ex=1` 那一行, 目标侧是敌方?)
        A3CheckReal("AM18", "turn_end", true);
        A3CheckReal("AM67", "turn_end", true);
        A3CheckReal("TAU6", "turn_start", true);
        A3CheckReal("TAU7", "turn_start", true);
        A3CheckReal("TAU13", "turn_start", false);

        // ---- 手牌陷阱 `Poisoned Supplies`（走 `SplitAtTurn` 那条更窄的路）----
        {
            var ps = CardDatabase.FindById(pool, "a7bb23355a30d344e9d2decf976d91e1");
            CheckTrue(ps != null, "① （前提）卡池里有 `Poisoned Supplies`");
            if (ps != null)
            {
                var at = EffectText.SplitAtTurn(ps.Desc);
                CheckTrue(at != null && at[0] == "turn_end",
                          "① （前提）`Poisoned Supplies` 是**手牌陷阱**形态（`SplitAtTurn` 认得、相位 turn_end）"
                        + "｜🧨 desc 改成多句/后缀式 ⇒ 它不再进「手牌陷阱」通道 ⇒ 下面就不是生产路了");
                if (at != null)
                {
                    var ops = EffectText.Parse(at[1], out _, out _);
                    CheckTrue(ops != null && ops.Count == 1 && ops[0].Verb == "deal",
                              "① （前提）正文解析成 1 条 `deal`");
                    CheckTrue(ops != null && ops.Count == 1 && ops[0].TargetCriteriaSet == null,
                              "★ ① 不跑 helper ⇒ 手牌陷阱的 at-turn 正文**没有 criteria**（= 改前）");
                    EffectText.AttachAtTurnBodyCriteria(ps, ops, at[0]);
                    CheckTrue(ops[0].TargetCriteriaSet != null,
                              "★★ ① 手牌陷阱那条 `deal` 也贴上了 logic 级 36 格 criteria"
                            + "（`uniqueAtTurnAbilityIndex` 按 trigger 40 选到**唯一**那条）"
                            + "｜🧨 只给棋盘那一趟调 helper ⇒ 红");
                    var set = ops[0].TargetCriteriaSet;
                    Check(set != null ? set.TargetsAffected : -999, 10, "★ ① 陷阱的 criteria `ta = 10`");
                    Check(set != null ? set.FriendlyUnits : -999, 1, "★ ① 陷阱的 `friendlyUnits = 1`（只打自己人）");
                    Check(set != null ? set.IncludeRemnants : -999, 0,
                          "★★ ① 陷阱的 `includeRemnants = 0` —— **文本层没有这一格**（「your troops」不区分残骸），"
                        + "这就是贴片带来的唯一新信息｜🧨 不贴 ⇒ 这一格读不到 ⇒ 红");
                    CheckTrue(ops[0].TargetCondition == null,
                              "★ ① 陷阱这一格**没有** cond/ex 行（`unitConditions` 整列为 null）⇒ 不硬造一条");

                    // 行为探针：己方部队过、敌方不过
                    var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
                    var own = Place(ctx, 0, 1, Unit("A3PSOwn", 1, 1, 4));
                    var foe = Place(ctx, 1, 1, Unit("A3PSFoe", 1, 1, 4));
                    string gap;
                    CheckTrue(TargetCriteriaEval.Matches(ctx, 0, set, own, ops[0].TargetCondition,
                                                         null, null, out gap) == true,
                              "★★ ① 陷阱 criteria：**己方部队**放行");
                    CheckTrue(TargetCriteriaEval.Matches(ctx, 0, set, foe, ops[0].TargetCondition,
                                                         null, null, out gap) == false,
                              "★★ ① 陷阱 criteria：**敌方**被筛掉（`enemyUnits = 0`）"
                            + "｜🧨 不贴（`set == null` ⇒ `Matches` 恒 true）⇒ 这一格红");
                }
            }
        }
    }

    /// <summary>单张真卡的四段检查（见 <see cref="A3Case1AtTurnBodyCriteriaAttached"/> 的 doc）。</summary>
    static void A3CheckReal(string id, string phase, bool expectCond220)
    {
        var pool = CardDatabase.Load();
        var card = CardDatabase.FindById(pool, id);
        CheckTrue(card != null, $"① （前提）卡池里有 `{id}`");
        if (card == null) return;
        var ops = A3AtTurnBody(card, phase);
        CheckTrue(ops != null && ops.Count == 1,
                  $"① （前提）`{id}` 在 `{phase}` 相位上解析出**恰好 1 条** op"
                + "（`ops.Count == 1` 是窄闸之一）");
        if (ops == null || ops.Count != 1) return;
        CheckTrue(ops[0].Verb == "deal" || ops[0].Verb == "give",
                  $"① （前提）`{id}` 那条 op 是 `deal`/`give`（现读 verb = `{ops[0].Verb}`）");

        CheckTrue(ops[0].TargetCriteriaSet == null,
                  $"★ ① `{id}`：**不跑 helper 就没有 criteria**（= 改前；这条是下面灭自证的判别式）"
                + "｜🧨 万一 `Parse(cl.Body)` 自己贴上了 —— 那本笔就是多余的，先查清是谁贴的");

        EffectText.AttachAtTurnBodyCriteria(card, ops, phase);

        CheckTrue(ops[0].TargetCriteriaSet != null,
                  $"★★ ① `{id}` 的 at-turn `{ops[0].Verb}` op **贴上了** 36 格 criteria"
                + "（`ta == 10 allFiltered` 那一档）"
                + "｜🧨 删掉 `ResolveAtTurn` 里那次 `AttachAtTurnBodyCriteria`（或窄闸放宽/收紧）⇒ 红");
        var set = ops[0].TargetCriteriaSet;
        if (set == null) return;
        Check(set.TargetsAffected, 10,
              $"★ ① `{id}` 贴的是 `ta = 10 (allFiltered)`（`cards_engine.json` 现读）"
            + "｜🧨 贴到别的行（例如 AM18 的 `multiCountCriteria` 行）⇒ 数字变别的 ⇒ 红");
        Check(ops[0].TargetCriteriaAbility, 0, $"★ ① `{id}` 的 criteria 坐标 `ab = 0`");
        Check(ops[0].TargetCriteriaLogic, 0, $"★ ① `{id}` 的 criteria 坐标 `lg = 0`");

        bool hasCond = ops[0].TargetCondition != null;
        CheckTrue(hasCond == expectCond220,
                  $"★★ ① `{id}` 的**同一格** cond/ex 行：期望「有 220/ex=1」= {expectCond220}，实测 = {hasCond}"
                + "｜🧨 只贴 36 格、不贴 `unitConditions`（或反之）⇒ 红");
        if (expectCond220 && ops[0].TargetCondition != null)
        {
            Check(ops[0].TargetCondition.Condition, 220,
                  $"★ ① `{id}` 的条件值 = 220（枚举名叫 `dealtKillDmg`，**语义是「将死」**，"
                + "判据 = `ConditionEval` 的 220 那一支 + `CardScript__EnoughPendingDamageToDie.c`）");
            CheckTrue(ops[0].TargetCondition.Exclude,
                      $"★ ① `{id}` 的 220 是 `excludeCondition`（取反）");
        }

        // ---- 行为探针（**用挂上去的那个对象**，不是手搓 set）----
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var mine = Place(ctx, 0, 3, Unit("A3Mine", 1, 1, 5));
        var foe = Place(ctx, 1, 1, Unit("A3Foe", 1, 1, 5));
        string why;
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, set, foe, ops[0].TargetCondition,
                                             mine, null, out why) == true,
                  $"★★ ① `{id}`：**敌方候选过筛**（`enemyUnits = 1`）");
        CheckTrue(TargetCriteriaEval.Matches(ctx, 0, set, mine, ops[0].TargetCondition,
                                             mine, null, out why) == false,
                  $"★★ ① `{id}`：**己方候选被筛掉**（`friendlyUnits = 0`）"
                + "｜🧨 不贴（`set == null` ⇒ `Matches` 恒 true）⇒ 这一格红（这就是灭自证）");
        if (expectCond220)
        {
            int hp = foe.Health;
            foe.Health = 0;                       // 「将死」= `Health <= 0 && Survivor <= 0`
            CheckTrue(TargetCriteriaEval.Matches(ctx, 0, set, foe, ops[0].TargetCondition,
                                                 mine, null, out why) == false,
                      $"★★ ① `{id}`：敌方**已「将死」**（`Health = 0`）⇒ 被 `excludeCondition = 220` 筛掉"
                    + "｜🧨 只贴 36 格、不贴 cond/ex ⇒ 这一格仍是 true ⇒ 红");
            foe.Health = hp;
            CheckTrue(TargetCriteriaEval.Matches(ctx, 0, set, foe, ops[0].TargetCondition,
                                                 mine, null, out why) == true,
                      $"★ ① `{id}`：把血改回去 ⇒ 又放行（与上一条互为灭自证：唯一变量是 `Health`）");
        }
    }

    // ==================================================================
    //  ② `A3-1`：说不清的一律**不贴**（窄闸逐条钉住）
    // ==================================================================

    /// <summary>
    /// 五张「本笔**故意不贴**」的卡 —— 每一条都对应一个「贴上去就是猜」的理由，钉住它，
    /// 免得下一个人顺手把所有 at-turn 卡都贴一遍（那会把 `TL37` 的**反向行序**贴错目标）。
    /// 另加一条合成负例：相位不符 ⇒ 一条都不贴。
    /// </summary>
    static void A3Case2AtTurnCriteriaGates()
    {
        var pool = CardDatabase.Load();

        // ---- ① 相位不符（合成卡：trigger 31 = TurnStartPlayer，喂 turn_end）----
        {
            var card = A3Synthetic("A3Phase", true, "At the start of your turn, deal 1 damage to a random enemy");
            var ops = A3AtTurnBody(card, "turn_start");
            CheckTrue(ops != null && ops.Count == 1, "② （前提）合成卡的 at-turn 正文解析成 1 条 op");
            EffectText.AttachAtTurnBodyCriteria(card, ops, "turn_end");
            CheckTrue(ops != null && ops.Count == 1 && ops[0].TargetCriteriaSet == null,
                      "★★ ② 相位不符（`turn_end` vs trigger 31 = TurnStartPlayer）⇒ **一条都不贴**"
                    + "｜🧨 不看相位 ⇒ 贴错 ability ⇒ 红");
            var ops2 = A3AtTurnBody(card, "turn_start");
            EffectText.AttachAtTurnBodyCriteria(card, ops2, "turn_start");
            CheckTrue(ops2 != null && ops2.Count == 1 && ops2[0].TargetCriteriaSet != null,
                      "★★ ② 换回正确相位 ⇒ 贴上（与上一条互为灭自证：唯一变量是 phase）");
        }

        // ---- ② `GOF82`：`ta = 50 (inHandFiltered)` —— 手牌池不是 `ResolveTargets` 的棋盘池 ----
        A3ExpectNoAttach(pool, "GOF82", "turn_end", 50,
                         "`ta = 50 (inHandFiltered)`：原版那条 criteria 属于 `HandEffect.targetCriteria`，"
                       + "我们的等价物是 `spec.SubtypeFilter = Beast`（`DoGive` 的手牌 buff 支），"
                       + "`ResolveTargets` 对手牌**直接早退**（`spec.HandOnly`）⇒ 贴上去零消费者、只会误导");

        // ---- ③ `TAU10`：`ta = 100 (adjacentToSelfIfMeetsCriteria)` ----
        A3ExpectNoAttach(pool, "TAU10", "turn_end", 100,
                         "`ta = 100` 的「相邻里有满足者」本版**未实现**（`TargetCriteriaEval` 对它明确返回"
                       + " `null` = 判不了）⇒ 贴上去每次只出一次声、零筛。**还差**：实现 `:407-425` 那一档");

        // ---- ④ `TL37`：2 条 op vs 2 行 criteria，而且**行序与文字序相反** ----
        {
            var card = CardDatabase.FindById(pool, "TL37");
            CheckTrue(card != null, "② （前提）卡池里有 `TL37` Spore Mine");
            if (card != null)
            {
                var ops = A3AtTurnBody(card, "turn_start");
                Check(ops != null ? ops.Count : -1, 2,
                      "★ ② `TL37` 的正文是 **2 条 op**（`destroy this unit` + `deal 3 damage`）"
                    + "｜🧨 解析层把它合成一条 ⇒ 下面那条前提不成立");
                CheckTrue(card.AbilityTargets.Count == 1 && card.AbilityTargets[0].TargetsAffected.Length == 2,
                          "★ ② （前提）`TL37` 那条 ability 的 `ta` 也是 2 格");
                EffectText.AttachAtTurnBodyCriteria(card, ops, "turn_start");
                CheckTrue(ops != null && ops[0].TargetCriteriaSet == null && ops[1].TargetCriteriaSet == null,
                          "★★ ② `TL37` **一条都不贴** —— 文字序是 destroy(self)→deal(enemy)，"
                        + "而 criteria 行序是 `lg0 = ta10 enemy` / `lg1 = ta20 self`（**正好相反**）"
                        + "⇒ 按位置贴会把「摧毁自己」贴上「敌人池」的筛子"
                        + "｜🧨 放宽 `ops.Count == 1` ⇒ 这一格红（那才是真偏离）");
            }
        }

        // ---- ⑤ `GSC5`：`Subjectless`（「Takes 1 damage」= 打自己），不是棋盘候选池 ----
        {
            var card = CardDatabase.FindById(pool, "GSC5");
            CheckTrue(card != null, "② （前提）卡池里有 `GSC5` Concealed Explosives");
            if (card != null)
            {
                var ops = A3AtTurnBody(card, "turn_start");
                CheckTrue(ops != null && ops.Count == 1 && ops[0].Target != null && ops[0].Target.Subjectless,
                          "★ ② （前提）`GSC5` 的 op 目标是**无主语**（解析层给 `Subjectless = 1`）");
                EffectText.AttachAtTurnBodyCriteria(card, ops, "turn_start");
                CheckTrue(ops != null && ops[0].TargetCriteriaSet == null,
                          "★★ ② `GSC5` **不贴** —— 无主语那支在 `ResolveTargets` 里**早退**"
                        + "（根本不建棋盘池、也就走不到 `ApplyTargetCriteriaFilter`）⇒ 贴了零生效");
            }
        }

        // ---- ⑥ `GOF90`：2 条 ability、`ta=[10,10]` 2 格 vs 1 条 op ----
        {
            var card = CardDatabase.FindById(pool, "GOF90");
            CheckTrue(card != null, "② （前提）卡池里有 `GOF90` Wurrboy");
            if (card != null)
            {
                var ops = A3AtTurnBody(card, "turn_end");
                Check(ops != null ? ops.Count : -1, 1,
                      "★ ② （前提）`GOF90` 的正文是 1 条 op（`deal 1-2 … for each friendly troop`）");
                EffectText.AttachAtTurnBodyCriteria(card, ops, "turn_end");
                CheckTrue(ops != null && ops[0].TargetCriteriaSet == null,
                          "★★ ② `GOF90` **不贴** —— 它的 at-turn 那条 ability 是 `ab=1`、`ta=[10,10]`（2 格）"
                        + "而正文只有 1 条 op ⇒ 「第 j 条 op = 第 j 条 logic」没有判据"
                        + "｜🧨 只按 `ta.Length == 1` 放宽成「取第 0 格」⇒ 红（那会静默少贴/贴错）");
            }
        }
    }

    /// <summary>「这张真卡**必须不贴**」的通用检查（前提 = 现存的那一格 `ta` 等于 `wantTa`）。</summary>
    static void A3ExpectNoAttach(List<CardDef> pool, string id, string phase, int wantTa, string reason)
    {
        var card = CardDatabase.FindById(pool, id);
        CheckTrue(card != null, $"② （前提）卡池里有 `{id}`");
        if (card == null) return;
        int gotTa = -999;
        for (int i = 0; i < card.AbilityTargets.Count; i++)
            if (card.AbilityTargets[i] != null && card.AbilityTargets[i].TargetsAffected != null
                && card.AbilityTargets[i].TargetsAffected.Length > 0)
            { gotTa = card.AbilityTargets[i].TargetsAffected[0]; break; }
        Check(gotTa, wantTa, $"★ ② （前提）`{id}` 的 `ta` = {wantTa}（`cards_engine.json` 现读）");
        var ops = A3AtTurnBody(card, phase);
        CheckTrue(ops != null && ops.Count == 1, $"② （前提）`{id}` 的 at-turn 正文解析成 1 条 op");
        EffectText.AttachAtTurnBodyCriteria(card, ops, phase);
        CheckTrue(ops != null && ops.Count == 1 && ops[0].TargetCriteriaSet == null,
                  $"★★ ② `{id}` **不贴** —— {reason}"
                + "｜🧨 把窄闸放宽到「只要能取到 ta 就贴」⇒ 这一格红");
    }

    // ==================================================================
    //  ③ `A3-1` 端到端：真卡 `TAU6 Gun Drone`（回合开始）＋ 手牌陷阱 `Poisoned Supplies`（回合末）
    // ==================================================================

    /// <summary>
    /// 两条**真广播**的端到端：
    /// · `Poisoned Supplies`（手牌陷阱通道）—— 它是本笔唯一「真能改行为」的那张
    ///   （criteria 的 `friendlyUnits = 1 / enemyUnits = 0` 是**新信息**，文本层只有「your troops」）
    ///   ⇒ 持有者的部队必须掉 1 点、对方部队一点不掉。
    /// · `TAU6 Gun Drone`（棋盘通道）—— `deal 2 damage to a random enemy` 必须**只打在其中一个**
    ///   敌人身上（`Count = 1`）；这一半**不**用来看 criteria（今天零可观测差异，见 helper 的 doc），
    ///   只钉住「本笔没有把 at-turn 的 `deal` 弄坏」。
    /// </summary>
    static void A3Case3AtTurnEndToEnd()
    {
        // ---- 手牌陷阱 `Poisoned Supplies`：它是本笔唯一「真能改行为」的那张 ⇒ 真结算一拍 ----
        {
            var ps = CardDatabase.FindById(CardDatabase.Load(), "a7bb23355a30d344e9d2decf976d91e1");
            CheckTrue(ps != null, "③ （前提）卡池里有 `Poisoned Supplies`");
            if (ps != null)
            {
                var cx = ProbeBattle(new[] { ps }, new CardDef[0]);
                ToP1Turn(cx, 1);
                var own = Place(cx, 0, 1, Unit("A3PSTroop", 1, 1, 5));
                var foe = Place(cx, 1, 1, Unit("A3PSFoe2", 1, 1, 5));
                RuleCore.EndTurn(cx);           // P0 的回合末 ⇒ 陷阱响（持有者 = P0）
                Check(5 - own.Health, 1,
                      $"★★ ③ 陷阱真结算：持有者的部队掉 1 点（实测掉 {5 - own.Health}）"
                    + "｜🧨 criteria 贴片把持有者自己的池子筛空（`friendlyUnits` 方向写反）⇒ 这里变 0 ⇒ 红");
                Check(5 - foe.Health, 0,
                      $"★★ ③ 陷阱真结算：**对方**部队不掉血（`enemyUnits = 0`）"
                    + "｜🧨 不贴（`set == null` 谁都不筛）或把 owner 传错 ⇒ 对方也掉 ⇒ 红");
            }
        }

        var drone = CardDatabase.FindById(CardDatabase.Load(), "TAU6");
        CheckTrue(drone != null, "③ （前提）卡池里有 `TAU6` Gun Drone");
        if (drone == null) return;

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        Place(ctx, 0, 3, drone, exhausted: false);
        var a = Place(ctx, 1, 1, Unit("A3E2EA", 1, 1, 6), exhausted: true);
        var b = Place(ctx, 1, 5, Unit("A3E2EB", 1, 1, 6), exhausted: true);
        RuleCore.EndTurn(ctx);      // P0 结束
        RuleCore.BeginTurn(ctx);    // P1 开始（不是它的主人 ⇒ `your turn` 那半不发）
        RuleCore.EndTurn(ctx);      // P1 结束
        RuleCore.BeginTurn(ctx);    // P0 开始 ⇒ 触发

        int lostA = 6 - a.Health, lostB = 6 - b.Health;
        Check(lostA + lostB, 2,
              $"★★ ③ 真卡 `TAU6` 的回合开始**只打了一下 2 点**（实测 A 掉 {lostA} / B 掉 {lostB}）"
            + "｜🧨 criteria 贴片把候选池筛空 / 筛成两个（`maxTargets` 那一格被误当筛子）⇒ 红");
        CheckTrue((lostA == 2 && lostB == 0) || (lostA == 0 && lostB == 2),
                  $"★★ ③ 掉血**只落在其中一个**敌人身上（`Count = 1`，不是「all enemies」）"
                + $"（实测 A {lostA} / B {lostB}）");
    }

    // ==================================================================
    //  ④ `A3-3`：`CardDef` 两列共用一个版本位 —— 静默陷阱要出声
    // ==================================================================

    /// <summary>
    /// `AbilityTargetsVersioned = abilityTriggersVersioned`（ctor）**不是原版语义**，是我们数据管线的
    /// 便利默认 ⇒ 只给一列时构造函数**出声**（`Debug.LogWarning`），并用
    /// <see cref="CardDef.AbilityVersionFlagWarningsForTest"/> 当判别式。
    /// 另一半是**行为**：同一个正文，版本位 false ⇒ criteria 贴不上（= `A2` 踩过的静默跳过）；
    /// 给了 true ⇒ 贴上。两组互为灭自证（唯一变量 = 那一位）。
    /// </summary>
    static void A3Case4AbilityVersionFlagTrap()
    {
        const string desc = "At the start of your turn, deal 1 damage to a random enemy";

        // ---- ① 只给 `abilityTargets`、没给版本位 ⇒ 出声 + 静默跳过 ----
        int before = CardDef.AbilityVersionFlagWarningsForTest;
        var bad = A3Synthetic("A3VfBad", versioned: false, desc: desc);
        CheckTrue(!bad.AbilityTargetsVersioned,
                  "★★ ④ 只给 `abilityTargets` 不给 `abilityTriggersVersioned` ⇒ `AbilityTargetsVersioned` = **false**"
                + "（两列共用一个版本位）｜🧨 改成各自一位 ⇒ 这一格红（那时要重写本用例与报告）");
        Check(CardDef.AbilityVersionFlagWarningsForTest - before, 1,
              "★★ ④ 这个组合**必须出声一次**（`Debug.LogWarning`，计数 +1）"
            + "｜🧨 删掉 ctor 里那条 warning（= `A2` 踩的静默陷阱）⇒ 计数不涨 ⇒ 红");
        var opsBad = A3AtTurnBody(bad, "turn_start");
        EffectText.AttachAtTurnBodyCriteria(bad, opsBad, "turn_start");
        CheckTrue(opsBad != null && opsBad.Count == 1 && opsBad[0].TargetCriteriaSet == null,
                  "★★ ④ 行为面：版本位 false ⇒ at-turn 的 criteria **贴不上**（消费端当「旧表整列没落」）"
                + "｜🧨 去掉 `AttachAtTurnDealGiveCriteria` 里那道 `AbilityTargetsVersioned` 闸 ⇒ 红");

        // ---- ② 补上版本位 ⇒ 不出声、贴得上（灭自证）----
        before = CardDef.AbilityVersionFlagWarningsForTest;
        var good = A3Synthetic("A3VfGood", versioned: true, desc: desc);
        CheckTrue(good.AbilityTargetsVersioned,
                  "★ ④ 给了 `abilityTriggersVersioned: true` ⇒ `AbilityTargetsVersioned` = true");
        Check(CardDef.AbilityVersionFlagWarningsForTest - before, 0,
              "★★ ④ 两列**都给**时不喊（唯一变量 = 那一位；上一条的计数涨只能来自缺位）");
        var opsGood = A3AtTurnBody(good, "turn_start");
        EffectText.AttachAtTurnBodyCriteria(good, opsGood, "turn_start");
        CheckTrue(opsGood != null && opsGood.Count == 1 && opsGood[0].TargetCriteriaSet != null,
                  "★★ ④ 版本位 true ⇒ 同一句正文的 criteria **贴上了**（与上一条互为灭自证）");

        // ---- ③ 反方向：版本位给了、trigger 有、targets 整列空 ⇒ 也要喊 ----
        before = CardDef.AbilityVersionFlagWarningsForTest;
        var missing = new CardDef("A3VfMissing", "A3VfMissing", "unit", "", "common", "Test", 1, 1, 1, 0, null,
                                  subtype: "Infantry",
                                  abilityTriggers: new[] { 31 },
                                  abilityTriggersVersioned: true);
        Check(missing.AbilityTargets.Count, 0, "★ ④ （前提）这张卡的 `abilityTargets` 整列为空");
        Check(CardDef.AbilityVersionFlagWarningsForTest - before, 1,
              "★★ ④ 版本位给了、trigger 有、`abilityTargets` 整列空 ⇒ 同样出声（数据缺列，不是「没有 ability」）"
            + "｜🧨 只做前一个方向 ⇒ 红");

        // ---- ④ 真卡池这两列**同进同出**（生产零噪声的判据）----
        {
            var pool = CardDatabase.Load();
            int mismatch = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                if (c == null) continue;
                bool hasTg = c.AbilityTriggerCount > 0;
                bool hasTa = c.AbilityTargets.Count > 0;
                if (hasTg != hasTa) mismatch++;
            }
            Check(mismatch, 0,
                  "★★ ④ 现读卡池：`abilityTriggers` 与 `abilityTargets` **同进同出**（0 张不一致）"
                + "⇒ 上面两条 warning 在正式装载时**零噪声**"
                + "｜🧨 生成器改了某一列的形状（只落一列）⇒ 这里红，说明那声会对 1127 张刷屏");
        }
    }
}
