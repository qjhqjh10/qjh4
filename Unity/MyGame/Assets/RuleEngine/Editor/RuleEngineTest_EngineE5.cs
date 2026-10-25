// RuleEngineTest_EngineE5.cs — 引擎线 `E5`（2026-10-25 · 第七十五会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineE2` /
// `_EngineE3` / `_EngineE4` / `_EngineA3` / `_EngineC5` … 都在），同一时刻只有一个写手改一个文件 ⇒
// 本批测试体全写在这里，`RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestE5SmallEngineGaps)`**。
//
// 两件的判据（⛔ 不要重新论证；全文 → `资料/普查产出_1025/E5_引擎剩余小缺口.md`）：
//  ① **E5-1**：`activeAbility.targetCriteria`（对象级，`ActiveAbility +0x38`）的
//     `conditionsFilter` / `excludeCondition` 两格 —— 原版 `FilterMethods__CheckIfMeetsCriteria.c:298-303`
//     是**同一条 36 格链**里的第 ⑧ 步；数据早在 v15 的 `unitConditions` 里
//     （`{ab:-1|-2, lg:-1, role:"target"}`），缺的是消费（`Matches` 的 `cond` 形参原来恒传 `null`）。
//  ② **E5-2**：主动能力的候选合法性 = 原版 `BattleManager.IsValidActiveAbilityTarget`
//     （65 行逐句），两条旧闸与它不符：
//       · **督军不是「一律不可点」** —— 原版根本没有这一条；能不能点由 criteria 的 `includeHero` 决定；
//       · **潜行 / ward 只在【异侧】才挡**（改前我们不分侧、一律挡）。
//
// 🔴 每条断言都带 `🧨`（什么改动能让它红）。期望值一律写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestE5SmallEngineGaps)`）。</summary>
    public static void TestE5SmallEngineGaps()
    {
        Section("🆕 引擎线 E5：① 主动能力对象级 criteria 的 `cond`/`ex`（`E5-1`）"
              + " · ② 主动能力候选合法性 = 原版 `IsValidActiveAbilityTarget`（`E5-2`）");
        E5Case1PickCriteriaCondition();
        E5Case2AbilityTargetLegality();
        E5Case3PremiseChecks();
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>合成主动能力卡：`Ability: Damage 3 EnemyUnit`（⇒ `Card.Ability.Target == EnemyUnit`、
    /// `EffectTargets.NeedsPick` 为真、`CanStartAbility` 过得去）。
    /// <para><paramref name="pick"/> = `ActiveAbility.targetCriteria` 那一列（`ab = -1`）。</para>
    /// <para><paramref name="cond"/> = `unitConditions` 里的**那一行**（`{ab:-1, lg:-1, role:"target"}`）
    /// —— 就是 `conditionsFilter` / `excludeCondition` 两格（见 `E5-1` 的判据）。</para></summary>
    static CardDef E5AbilityCard(string id, CardDef.CountSetSpec pick, CardDef.ConditionSpec cond,
                                 bool condVersioned, bool pickVersioned = true)
    {
        List<CardDef.TargetCriteriaRowSpec> rows = null;
        if (pick != null)
            rows = new List<CardDef.TargetCriteriaRowSpec>
            {
                new CardDef.TargetCriteriaRowSpec
                {
                    Ability = CardDef.ActiveAbilityObject, Trigger = -1, Logic = -1, Criteria = pick,
                },
            };
        List<CardDef.ConditionSpec> conds = null;
        if (cond != null) conds = new List<CardDef.ConditionSpec> { cond };
        return new CardDef(id, id, "unit", "", null, "Test", 1, 1, 5, 0,
                           new[] { "Ability: Damage 3 EnemyUnit" },
                           // 🔴 `hasAlternativeAbility: 0` **必须给**：闸先看这一位，
                           //    `-1`（未知）⇒ `UsesAlternativeAbility` 回 `null` ⇒ 整道闸判不了 ⇒ 放行。
                           hasAlternativeAbility: 0,
                           abilityConditions: conds,
                           conditionsVersioned: condVersioned,
                           activeAbilityPickCriteria: rows,
                           activeAbilityPickCriteriaVersioned: pickVersioned);
    }

    /// <summary>`unitConditions` 的一行（`role="target"` + `lg=-1` = **对象级** `activeAbility.targetCriteria`）。</summary>
    static CardDef.ConditionSpec E5Cond(int cond, bool exclude, string name = "?")
    {
        return new CardDef.ConditionSpec
        {
            Ability = CardDef.ActiveAbilityObject,   // -1
            Logic = -1,                              // 直接挂在能力对象上（≠ abilityLogic 下标）
            Role = "target",
            Condition = cond,
            Name = name,
            Exclude = exclude,
        };
    }

    /// <summary>摆一张主动能力卡 + 一个敌方单位，返回 `(ctx, me, casterSlot, foeSlot)`。`me` = 当前回合方。</summary>
    static BattleContext E5Board(CardDef card, out int me, out int casterSlot, out int foeSlot)
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
        me = ctx.Active;
        var caster = Place(ctx, me, 3, card);
        var foe = Place(ctx, 1 - me, 3, Unit("E5Foe", 1, 1, 5));
        casterSlot = E4SlotOf(ctx, me, caster);
        foeSlot = E4SlotOf(ctx, 1 - me, foe);
        return ctx;
    }

    // ==================================================================
    //  ① E5-1：`conditionsFilter` / `excludeCondition`（对象级 activeAbility criteria）
    // ==================================================================

    static void E5Case1PickCriteriaCondition()
    {
        // ---- ①·a 真卡 `SW9 Fenrisian Wolf`：`cond = 50 (flying)` + `ex = 1`（排除潜行以外的飞行）----
        //   现读它的 `unitConditions` = `[{ab:-1, lg:-1, role:"target", cond:50, name:"flying", ex:1}]`
        //   —— 正是**对象级** `activeAbility.targetCriteria` 的两格（`E4` 报告 §五·3 说「是 logic 级、
        //   带 `lg`」**不成立**，`lg = -1` 就是「直接挂在能力对象上」）。
        var pool = CardDatabase.Load();
        var sw9 = CardDatabase.FindById(pool, "SW9");
        CheckTrue(sw9 != null, "① （前提）卡池里有 `SW9 Fenrisian Wolf`");
        if (sw9 != null)
        {
            CardDef.ConditionSpec spec;
            bool has = sw9.TryGetCondition(CardDef.ActiveAbilityObject, -1, "target", out spec);
            CheckTrue(has, "★★ ① `SW9` 的对象级 criteria 有 `cond` 行（`ab=-1 / lg=-1 / role=target`）"
                         + "｜🧨 生成器 `scan_ability(-1, …)` 不再落对象级 `targetCriteria` 的 cond 行 ⇒ 红");
            if (has)
            {
                Check(spec.Condition, 50, "★★ ① `cond = 50`（`UnitConditions.flying`）");
                Check(spec.Exclude ? 1 : 0, 1, "★★ ① `excludeCondition = 1`（取反）");
                Check(spec.Name, "flying", "★ ① 枚举名 = `flying`");
                Check(spec.Logic, -1, "★★ ① `lg = -1`（直接挂在能力对象上，**不是** `abilityLogic[j]`）"
                                    + "｜🧨 把它当成 `lg=0` 那一批 ⇒ 取到的是 `ta=20 self` 的那条 logic ⇒ 红");
            }
            string why;
            var set = sw9.ActiveAbilityPickCriteriaFor(CardDef.ActiveAbilityObject, out why);
            CheckTrue(set != null, "① （前提）`SW9` 的 36 格那一行在");
            if (set != null)
            {
                Check(set.EnemyUnits, 1, "★ ① `enemyUnits = 1`（我们的候选是它的子集 ⇒ 这一档能忠实判）");
                Check(set.IncludeMinions, 1, "★ ① `includeMinions = 1`");
            }
        }

        // ---- ①·b 正例 / 负例：**同一张卡只差 `ex` 这一格**（灭自证）----
        //   候选 = 敌方单位；`cond=50(flying)`。
        //     · `ex = true`  ⇒ 飞行候选**不可点**（原版 `:384-389` 取反）
        //     · `ex = false` ⇒ 飞行候选**可点**（同一格、只翻 `ex`）
        {
            var cardEx = E5AbilityCard("E5CondEx", E2Criteria10(1, 0), E5Cond(50, true, "flying"), true);
            int me, cs, fs;
            var ctx = E5Board(cardEx, out me, out cs, out fs);
            var fly = Place(ctx, 1 - me, 1, Unit("E5Fly", 1, 1, 5, KeywordTable.Flying));
            int flySlot = E4SlotOf(ctx, 1 - me, fly);
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, fs), RuleCodes.OK,
                      "★ ① 负例对照：同一张卡打**地面**敌人 ⇒ 可点");
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, flySlot), RuleCodes.ErrTarget,
                      "★★★ ① `cond=50(flying)` + `ex=1` ⇒ **飞行候选不可点**"
                    + "｜🧨 `TargetCriteriaEval.Matches` 的 `cond` 形参退回 `null`（改前就是）⇒ 红（这就是 E5-1 修的那一格）");
        }
        {
            var cardNoEx = E5AbilityCard("E5CondNoEx", E2Criteria10(1, 0), E5Cond(50, false, "flying"), true);
            int me, cs, fs;
            var ctx = E5Board(cardNoEx, out me, out cs, out fs);
            var fly = Place(ctx, 1 - me, 1, Unit("E5Fly2", 1, 1, 5, KeywordTable.Flying));
            int flySlot = E4SlotOf(ctx, 1 - me, fly);
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, flySlot), RuleCodes.OK,
                      "★★ ① 同一格 `cond=50` 但 `ex=0` ⇒ 飞行候选**可点**（`cond` 是**要求**、`ex` 才取反）"
                    + "｜🧨 把 `ex` 忽略（恒当排除）⇒ 红；把 `cond` 忽略（恒放行）⇒ 上面那条红");
        }

        // ---- ①·c 没有 `cond` 行（列在、这一格没条件）⇒ 放行且**不出声** ----
        {
            var card = E5AbilityCard("E5CondNone", E2Criteria10(1, 0), null, true);
            int me, cs, fs;
            var ctx = E5Board(card, out me, out cs, out fs);
            var fly = Place(ctx, 1 - me, 1, Unit("E5Fly3", 1, 1, 5, KeywordTable.Flying));
            int flySlot = E4SlotOf(ctx, 1 - me, fly);
            int warn = RuleCore.ActiveAbilityPickCriteriaWarningsForTest;
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, flySlot), RuleCodes.OK,
                      "★ ① 没有 `cond` 行 = 这一格**没有条件** ⇒ 放行");
            Check(RuleCore.ActiveAbilityPickCriteriaWarningsForTest, warn,
                  "★★ ① 这一档**不出声**（「明确没有条件」≠「判不了」）");
        }

        // ---- ①·d 旧表（`unitConditions` 列没落）⇒ 判不了 ⇒ 放行 + 出声 ----
        {
            var card = E5AbilityCard("E5CondOld", E2Criteria10(1, 0), null, false);
            int me, cs, fs;
            var ctx = E5Board(card, out me, out cs, out fs);
            int warn = RuleCore.ActiveAbilityPickCriteriaWarningsForTest;
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, fs), RuleCodes.OK,
                      "★★ ① 旧表（`conditionsVersioned = false`）⇒ `cond` 未知 ⇒ **放行**"
                    + "（⛔ 不许把「不知道」当「没有条件」，也不许当「不满足」）");
            CheckTrue(RuleCore.ActiveAbilityPickCriteriaWarningsForTest > warn,
                      "★★ ① 这一档**必须出声**｜🧨 去掉 `conditionsVersioned` 那一支 ⇒ 静默放行 ⇒ 红");
        }
    }

    // ==================================================================
    //  ② E5-2：候选合法性（督军 / 潜行分侧）
    // ==================================================================

    static void E5Case2AbilityTargetLegality()
    {
        const int wl = BoardSpec.WarlordSlot;      // 4

        // ---- ②·a criteria 的 `includeHero = 1` ⇒ **督军可点**（原版没有「督军一律不可点」这条）----
        {
            var allow = E2Criteria10(1, 0);        // IncludeHero = 1 / IncludeMinions = 1
            var card = E5AbilityCard("E5WlOk", allow, null, true);
            int me, cs, fs;
            var ctx = E5Board(card, out me, out cs, out fs);
            CheckTrue(ctx.Players[1 - me].Warlord != null, "② （前提）敌方督军在棋盘上");
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, wl), RuleCodes.OK,
                      "★★★ ② `includeHero = 1` ⇒ **敌方督军可被点为主动能力目标**"
                    + "｜🧨 把 `t.IsWarlord ⇒ ErrTarget` 那句加回来（改前就是）⇒ 红"
                    + "（现读池内 62 行 `includeHero = 1` 的目标判据全被它挡住）");
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, fs), RuleCodes.OK,
                      "★ ② 同一张卡打普通单位也照旧可点");
        }

        // ---- ②·b `includeHero = 0` ⇒ 督军**不可点**、普通单位可点（同一张卡，只差候选）----
        {
            var minionsOnly = E2Criteria10(1, 0);
            minionsOnly.IncludeHero = 0;
            var card = E5AbilityCard("E5WlNo", minionsOnly, null, true);
            int me, cs, fs;
            var ctx = E5Board(card, out me, out cs, out fs);
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, wl), RuleCodes.ErrTarget,
                      "★★★ ② `includeHero = 0` ⇒ 督军**不可点**（真正挡它的那一格 = 卡种白名单 `includeHero`）"
                    + "｜🧨 把「督军不可点」做成**无条件**（不看 criteria）⇒ ②·a 红、本条仍绿 ⇒ 两条一起看");
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, fs), RuleCodes.OK,
                      "★★ ② 同一张卡打普通单位 ⇒ 可点（挡的是「督军」这一格，不是「全都挡」）");
        }

        // ---- ②·c 潜行**只在异侧**挡（同侧 / 异侧各一对，`CanUseAbilityTarget` 的候选侧显式版）----
        {
            var card = E5AbilityCard("E5Side", null, null, false);   // 旧表：36 格闸放行，只剩分侧那两道
            int me, cs, fs;
            var ctx = E5Board(card, out me, out cs, out fs);
            var caster = ctx.Players[me].Board[cs];

            var allyStealth = Place(ctx, me, 1, Unit("E5AllyStealth", 1, 1, 5, KeywordTable.Stealth));
            int allySlot = E4SlotOf(ctx, me, allyStealth);
            var foeStealth = Place(ctx, 1 - me, 1, Unit("E5FoeStealth", 1, 1, 5, KeywordTable.Stealth));
            int foeStealthSlot = E4SlotOf(ctx, 1 - me, foeStealth);

            CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, me, allySlot), RuleCodes.OK,
                      "★★★ ② **同侧的潜行单位可点**（原版 `:47-50` 只在 `isPlayer !=` 时才看 stealth）"
                    + "｜🧨 把 stealth 那道闸写成不分侧（改前就是）⇒ 红");
            CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, 1 - me, foeStealthSlot), RuleCodes.ErrTarget,
                      "★★★ ② **异侧的潜行单位不可点**｜🧨 整条 stealth 闸删掉 ⇒ 红");
            CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, 1 - me, fs), RuleCodes.OK,
                      "★ ② 异侧的**普通**单位仍可点（挡的是潜行那一格，不是「异侧全挡」）");
            CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, me, cs), RuleCodes.OK,
                      "★ ② 同侧候选**不是**一律允许的旁证：这里靠旧表（36 格闸判不了 ⇒ 放行）——"
                    + "换成带 criteria 的卡就按 criteria 判（②·b 那两条）");
        }

        // ---- ②·d 空格 / 越界 / 座位越界仍拒（老行为回归）----
        {
            var card = E5AbilityCard("E5Empty", null, null, false);
            int me, cs, fs;
            var ctx = E5Board(card, out me, out cs, out fs);
            var caster = ctx.Players[me].Board[cs];
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, 8), RuleCodes.ErrTarget, "★ ② 空格 ⇒ 目标非法");
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, 99), RuleCodes.ErrTarget, "★ ② 越界 ⇒ 目标非法");
            CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, 2, fs), RuleCodes.ErrTarget,
                      "★ ② 座位越界 ⇒ 拒（别让 `Players[2]` 抛出去）");
        }
    }

    // ==================================================================
    //  ③ 前提校正（E5-3 / E5-4 的读数 —— 只落断言，不改行为）
    // ==================================================================

    static void E5Case3PremiseChecks()
    {
        var pool = CardDatabase.Load();

        // ---- ③·a `AM69` 的主动能力目标判据**不是**「只打督军」----
        //   `E4` 报告说「`AM69` 的 criteria 正是 `includeHero=1 / includeMinions=0` = 只能打督军」——
        //   现读真值与卡表**都不成立**：它是 `friendlyUnits=1 / enemyUnits=0 / includeMinions=1 /
        //   includeHero=0 / keywordFilter=5`（=「一个友方 Regiment 部队」），卡面文字是
        //   `Regiment: Give +1 [Ranged] to a random friendly troop.`
        var am69 = CardDatabase.FindById(pool, "AM69");
        CheckTrue(am69 != null, "③ （前提）卡池里有 `AM69 Tempestor Sergeant`");
        if (am69 != null)
        {
            string why;
            var set = am69.ActiveAbilityPickCriteriaFor(CardDef.ActiveAbilityObject, out why);
            CheckTrue(set != null, "③ `AM69` 的主动能力目标判据在（why = " + (why ?? "（无）") + "）");
            if (set != null)
            {
                Check(set.FriendlyUnits, 1, "★★ ③ `AM69` 的 `friendlyUnits = 1`（**友方**，不是敌方）");
                Check(set.EnemyUnits, 0, "★★ ③ `AM69` 的 `enemyUnits = 0`");
                Check(set.IncludeMinions, 1, "★★ ③ `includeMinions = 1`");
                Check(set.IncludeHero, 0, "★★★ ③ `includeHero = 0` —— **不是「只能打督军」**"
                                        + "｜🧨 谁把 `includeHero` / `includeMinions` 读反 ⇒ 红");
                Check(set.KeywordFilter, 5, "★ ③ `keywordFilter = 5`（卡面 `friendly troop` 那一族）");
            }
            // ⚠️ 可达性（如实）：`AM69` 的正文是 `Regiment: … / Duty: …`，**没有 `Ability:` 段**
            //   ⇒ `CanStartAbility` 先回 `ErrNoAbility`；即使将来结构化执行落地，我们的候选侧
            //   **恒为敌方**（`CommandTargetSide`），而它的目标是**友方** ⇒ 真前置件是「友方目标」那一层
            //   （落在黑名单文件 `CardPresentation/Battle/BattleDriver.cs`）。
            CheckTrue(am69.Ability == null,
                      "★★ ③ `AM69` 今天**没有**解析成功的 `Ability:` ⇒ 它的主动能力路整条不可达"
                    + "（与 `E4` 报告 §二·① 的读数一致）");
        }

        // ---- ③·b 全池：`includeHero = 1 ∧ includeMinions = 0`（「只能打督军」）的行 = **0** ----
        int heroOnly = 0, heroRows = 0, pickRows = 0;
        foreach (var c in pool)
        {
            if (c == null) continue;
            for (int k = 0; k < 2; k++)
            {
                int ab = k == 0 ? CardDef.ActiveAbilityObject : CardDef.AlternativeAbilityObject;
                string w;
                var s = c.ActiveAbilityPickCriteriaFor(ab, out w);
                if (s == null) continue;
                pickRows++;
                if (s.IncludeHero == 1) heroRows++;
                if (s.IncludeHero == 1 && s.IncludeMinions == 0) heroOnly++;
            }
        }
        Check(pickRows, 107, "★★ ③ 全池 `activeAbilityPickCriteria` 行数 = **107**（v28 现读，与 E4 那条一致）");
        Check(heroRows, 62, "★★ ③ 其中 `includeHero = 1` 的 **62** 行（这些卡的主动能力**能点督军**）");
        Check(heroOnly, 0, "★★★ ③ `includeHero = 1 ∧ includeMinions = 0`（「只能打督军」）= **0** 行"
                         + "｜🧨 真出现这种行 ⇒ E5-3 的前提（`AM69` 只打督军）要重新核 ⇒ 先看这条红没红");

        // ---- ③·c `ta = 200 / 201` 的 2 张卡：**数据行在**，缺的是「谁写 referencedCards」----
        //   现读 `AM54 At All Costs` / `GOF93 Hunta Rig` 的 `abilityTargetCriteria` 各有一行
        //   `ab=1 / lg=0 / ta=200|201`（卡表里**没有别的** ta=200/201 行）。
        //   ⚠️ `referencedCards` **不是卡数据**：原版是运行期 `CardScript +0x278`
        //   （`CardScript.cs:1522` 的 `List<CardScript> referencedCards`），唯一写入点 =
        //   `AbilityLogic__PlayAbility.c:2128-2160`（`chosenAbility == 0x17c = 380`）⇒ 我们缺的是
        //   **`cardAbilities[].abilityLogic[].chosenAbility` 这一列**（今天只落了 `activeAbility` 的 `ca`）
        //   ＋ 写侧调用点（`SetReferenceCards` 今天 0 生产调用点）+ 触发放行（410 那条入队点）。
        int ta200Rows = 0;
        foreach (var c in pool)
        {
            if (c == null) continue;
            for (int i = 0; i < c.AbilityTargets.Count; i++)
            {
                var arr = c.AbilityTargets[i] != null ? c.AbilityTargets[i].TargetsAffected : null;
                if (arr == null) continue;
                for (int j = 0; j < arr.Length; j++)
                    if (arr[j] == 200 || arr[j] == 201) ta200Rows++;
            }
        }
        Check(ta200Rows, 2, "★★ ③ `ta=200/201` 的行 = **2**（`AM54` / `GOF93` 各 1；数据在、消费链缺三件）"
                          + "｜🧨 生成器把 `ta=200/201` 门槛改了 ⇒ 红");
        var am54 = CardDatabase.FindById(pool, "AM54");
        var gof93 = CardDatabase.FindById(pool, "GOF93");
        CheckTrue(am54 != null && gof93 != null, "③ （前提）`AM54` / `GOF93` 都在池里");
        if (am54 != null)
        {
            var s200 = am54.AbilityTargetCriteriaAt(1, 0);
            CheckTrue(s200 != null && s200.TargetsAffected == 200,
                      "★★ ③ `AM54` 的 `ab=1 / lg=0` 那一行 `ta = 200`（`referencedCardInThisCard`）");
        }
        if (gof93 != null)
        {
            var s201 = gof93.AbilityTargetCriteriaAt(1, 0);
            CheckTrue(s201 != null && s201.TargetsAffected == 201,
                      "★★ ③ `GOF93` 的 `ab=1 / lg=0` 那一行 `ta = 201`（`referencedInPlayCardInThisCard`）");
        }
    }
}
