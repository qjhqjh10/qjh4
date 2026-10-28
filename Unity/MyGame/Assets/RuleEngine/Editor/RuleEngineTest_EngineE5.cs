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
        E5Case3PremiseChecks(); E5Case4B93F4Primitives(); E5Case4bLogPlayTactic();
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

        // ---- ③·c `ta = 200 / 201` 的 2 张卡：**数据行在**（原写「缺的是『谁写 referencedCards』」↓ 已订正）----
        //   现读 `AM54 At All Costs` / `GOF93 Hunta Rig` 的 `abilityTargetCriteria` 各有一行
        //   `ab=1 / lg=0 / ta=200|201`（卡表里**没有别的** ta=200/201 行）。
        //   ⚠️ `referencedCards` **不是卡数据**：原版是运行期 `CardScript +0x278`（`CardScript.cs:1522`），
        //   唯一写入点 = `AbilityLogic__PlayAbility.c:2128-2160`（`chosenAbility == 0x17c = 380`）。
        //   ⚠️ 2026-10-28 订正（B93_Q8/E8 现核）：原写「缺 `chosenAbility` 那一列 ＋ 写侧调用点 ＋ 触发放行 ⇒
        //   消费链缺三件」**不成立** —— 写侧 `ca=380` 结构上不可达（三张卡 `activeAbilityLogic = []`）· 读侧
        //   `subjTa` 恒 -1（`EffectResolver.cs:1501` 死路）· 无读者（`GOF104` 无 200/201）⇒ 行为由指代槽等价覆盖，见 ③·d。
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
        Check(ta200Rows, 2, "★★ ③ `ta=200/201` 的行 = **2**（`AM54` / `GOF93` 各 1；原写「数据在、消费链缺三件」⚠️ 2026-10-28 订正："
                          + "写侧不可达＋读侧死路＋无读者 ⇒ **不是缺口**，行为由指代槽等价覆盖，见本方法 ③·d）｜🧨 生成器把门槛改了 ⇒ 红");
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

        // ==================================================================
        //  ③·d `AM54 At All Costs` 的【行为等价性】—— `ca=380 setReferenceCard` 那一支
        //       在本卡池里**整体不可达**，但行为早就对了（只落断言、⛔ 零源码改动）
        // ==================================================================
        // **判据** → `资料/完全复刻/判据/B93_Q6_甲组施工单.md` 甲-1（三条硬结论，逐条有出处）：
        //   · 原版**写侧** = `AbilityLogic__PlayAbility.c:2128-2159`（`chosenAbility == 0x17c = 380`）
        //     —— 把本条 ability 的 `GetTargets` 结果**整表替换**进 `CardScript.referencedCards`
        //     （`thisCard + 0x278`；四条边界：覆盖不是追加 · 目标数 0 ⇒ 一个字节不动 ·
        //      `AbilityData == null` / `thisCard == null` ⇒ 什么都不做）。
        //   · 原版**读侧**只有两档：`ta=200 referencedCardInThisCard` / `ta=201 referencedInPlayCardInThisCard`
        //     （`AbilityLogic__GetTargets.c:770-806`；枚举全表 = `TargetsAffected.cs`，**只此两处**）。
        //   · 这三张卡（`AM54` / `GOF93` / `GOF104`）现读 `activeAbilityLogic = []` ⇒
        //     `RuleCore.UseActiveAbilityCore` 的 `ca=380` 那一段（`RuleCore.cs:15424/15493`）**永远进不去**；
        //     而每一条 op 的 `subjTa = -1` ⇒ `EffectResolver.cs:1501` 的 200/201 分派支**同样是死路**。
        //     ⇒ **补写侧不会有任何可观测变化**（原版对 `GOF104` 更彻底：全卡没有 200/201 读者）。
        //   · 我们走的是**另一套指代槽**：`draw` 写 `ctx.DrawnThisResolve`（`EffectResolver.DoDraw`），
        //     紧随其后的 `lowercost(payload=(指代上一张))` 读它、并按**实例**钉住（`DoLowerCost` 的 `pins`）。
        // ⇒ 所以这里断的是**行为**（刚抽到的那 3 份各降 4），**不是**那条分支；
        //   ⛔ 也**不断** `ReferencedCards`：那是结构件、今天 0 生产调用点，断它 = 断我们自己写的空壳（自证）。
        //
        // 四条断言 =「三条正向 + 三条灭自证」，期望值全部写死字面量：
        //   ① 抽到 3 份、手牌 3 → 5；② 那 3 份**各降 4**（4→0 · 5→1 · 6→2，按**实例**）；
        //   ③ 手里**原有的** 2 份不降（≠「整手牌一起降」）；
        //   ④ 牌库里 **6 份同 id 副本**不降（≠「按卡 id 匹配全部副本」）；
        //   ⑤ 🔴 同一切，只把督军血从 10 拨到 11 ⇒ **一份都不降**（与 ② **结构上不可能同时满足**）。
        if (am54 == null)
        {
            // 🔴 前提用**硬检查**（不是 `if (x != null) Check(…)` —— 那种写法在前提不成立时**静默跳过**，
            //    日志里一片绿、其实一条都没跑；本工程一天内撞到过 3 次）。
            CheckTrue(false, "★★ ③·d（前提）`AM54` 必须在池里 —— 否则本段一条断言都跑不出来");
        }
        else
        {
            Check(am54.Cost, 4, "★★ ③·d（前提）`AM54` 的卡面费 = **4**"
                              + "（`Draw 3 cards` + `lower their cost by 4`，`cards_engine.json` 现读）");

            // ---- 正向：督军血 = **10**（卡面阈值 `10 or less`；`warlordlowhp` 读 `Warlord.Health`）----
            var ctx10 = E5AtAllCostsBoard(am54, 10);
            CheckTrue(ctx10.Players[0].Warlord != null,
                      "★★ ③·d（前提）我方督军在棋盘上（`warlordlowhp` 读的就是它的 `Health`）");
            var before10 = new List<CardInstance>(ctx10.Players[0].Hand);
            Check(before10.Count, 3, "★★ ③·d（前提）经典起手 = **3** 张（`GameplayVariables.Classic.startingHand`）");
            int idx10 = HandIdx(ctx10, 0, "At All Costs");
            CheckTrue(idx10 >= 0, "★★ ③·d（前提）`AM54` 在起手（放在牌库顶那一份）");
            CheckCode(RuleCore.PlayTactic(ctx10, 0, idx10, -1), RuleCodes.OK,
                      "★★ ③·d `AM54` 打得出去（`Draw 3` 那一句不需要选目标）");
            var hand10 = ctx10.Players[0].Hand;
            Check(hand10.Count, 5, "★★ ③·d 手牌 **3 → 5**（少掉打出的那张、`draw 3` 补 3 张）"
                                 + "｜🧨 `draw` 的数目改了 ⇒ 红");
            var drawn10 = E5DrawnAfterPlay(ctx10, 0, before10);
            Check(drawn10.Count, 3, "★★ ③·d 新增到手牌的正好 **3 份**（按**实例**识别，不按卡名）");
            int printedSum = 0;
            foreach (var d in drawn10) printedSum += d.Card.Cost;
            Check(printedSum, 15, "★★ ③·d（前提）抽到那 3 张的印刷费 = 4 + 5 + 6 = **15**"
                                + "（三张**各不相同** ⇒ 「各降 4」与「降成某个常数」才分得开）");
            int lowered = 0;
            foreach (var d in drawn10)
                if (RuleCore.CostOf(ctx10, 0, d) == d.Card.Cost - 4) lowered++;
            Check(lowered, 3, "★★★ ③·d 刚抽到的那 3 份**各降 4**（4→0 · 5→1 · 6→2，按**实例**钉住）"
                            + "｜🧨 `DoDraw` 不填 `ctx.DrawnThisResolve`、或 `ResolveOps` 入口不清这个槽"
                            + "（读到上一张卡的陈旧指代）⇒ 红");

            // ---- 灭自证 A：手里**原有的**那 2 张（同一次结算里就在手牌，但不是「刚抽到的」）一份不降 ----
            int keptN = 0, keptBad = 0;
            foreach (var b in before10)
            {
                if (b == null || b.Card == null || b.Card.Id == am54.Id) continue;
                keptN++;
                if (RuleCore.CostOf(ctx10, 0, b) != b.Card.Cost) keptBad++;
            }
            Check(keptN, 2, "★★ ③·d（前提）打出前手里有 **2 张**非 `AM54` 的牌");
            Check(keptBad, 0, "★★ ③·d 手里原有的那 2 份**一分钱都不降**（8 / 7 原样）"
                            + "｜🧨 把降费写成「整手牌一起降」（不走 `DrawnThisResolve`）⇒ 红");

            // ---- 灭自证 B：牌库里**同 id 的 6 份副本**一份都不许被降（降费钉的是**实例**，不是卡 id）----
            int copyN = 0, copyBad = 0;
            foreach (var dc in ctx10.Players[0].Deck)
            {
                if (dc == null || dc.Card == null) continue;
                bool sameId = false;
                foreach (var d in drawn10) if (d.Card.Id == dc.Card.Id) sameId = true;
                if (!sameId) continue;
                copyN++;
                if (RuleCore.CostOf(ctx10, 0, dc) != dc.Card.Cost) copyBad++;
            }
            Check(copyN, 6, "★★ ③·d（前提）牌库里还留着 **6 份**同 id 的副本（灭自证 B 要用它们）");
            Check(copyBad, 0, "★★★ ③·d 牌库里那 6 份**同 id** 副本一份都没被降"
                            + "（降费钉在**那 3 份实例**上，与原版「`referencedCards` 只装本条 ability 的目标」同形）"
                            + "｜🧨 `DoLowerCost` 退回 `pin == null` 那条老路（`Key = 卡 id`）⇒ 这 6 份跟着降 ⇒ 红");

            // ---- 灭自证 C（结构性）：其他一概不变，只把督军血从 **10 → 11** ⇒ **一份都不降** ----
            //   与上面「血 10 ⇒ 各降 4」**结构上不可能同时满足**：任何实现只要不是真的读
            //   `Warlord.Health <= 10`（无条件降 / 恒不降 / 阈值读成别的数），这两条必有一条红。
            var ctx11 = E5AtAllCostsBoard(am54, 11);
            var before11 = new List<CardInstance>(ctx11.Players[0].Hand);
            int idx11 = HandIdx(ctx11, 0, "At All Costs");
            CheckTrue(idx11 >= 0, "★★ ③·d（前提·血 11）`AM54` 也在起手");
            CheckCode(RuleCore.PlayTactic(ctx11, 0, idx11, -1), RuleCodes.OK,
                      "★★ ③·d（血 11）同一张卡照样打得出去（条件只管降费、不管能不能打）");
            var drawn11 = E5DrawnAfterPlay(ctx11, 0, before11);
            Check(drawn11.Count, 3, "★★ ③·d（血 11）一样抽到 **3 份**");
            int unchanged = 0;
            foreach (var d in drawn11)
                if (RuleCore.CostOf(ctx11, 0, d) == d.Card.Cost) unchanged++;
            Check(unchanged, 3, "★★★ ③·d **灭自证**：督军血 = **11** ⇒ 刚抽到的 3 份**原价不动**"
                              + "｜🧨 摘掉条件（无条件降）/ 恒不降 / 阈值读错 ⇒ 本行与上面「血 10 各降 4」"
                              + "必有一条红（两条**结构上不可能同时满足**）");
        }
    }

    /// <summary>`③·d` 的夹具：`AM54 At All Costs` 在**牌库顶**的那一副牌（其余全是垫牌）。
    ///
    /// <para>**布局**（抽牌取【列表末尾】= 我们的「牌库顶」，判据 → `RuleCore.Draw` 那段注释）：
    /// 督军 · 6 张**同 id 副本**（最深处）· 3 张将被 `AM54` 抽到的垫牌（费用 4 / 5 / 6）·
    /// 2 张**起手**垫牌（费用 8 / 7）· `AM54`（顶）。⇒ 经典起手 3 张 = `AM54` + 那 2 张；
    /// 打出之后 `Draw 3 cards` 抽到的正是中间那 3 张，牌库里还留着 6 份同 id 副本。</para>
    ///
    /// <para>⚠️ 垫牌费用**必须 ≥ 4**：`CostOf` 末尾是 `clamp0`（`Math.Max(0, …)`），
    /// 印 1 费的牌降 4 之后读出来还是 0，差值被吃掉 ⇒ 断言读不出「降了 4」。
    /// ⚠️ 那 3 张**各不相同**（4/5/6）⇒ 「各降 4」与「降成某个常数」才分得开。</para>
    /// </summary>
    static BattleContext E5AtAllCostsBoard(CardDef am54, int warlordHp)
    {
        var draw3 = new List<CardDef>
        {
            Tactic("E5D1", 4, ""), Tactic("E5D2", 5, ""), Tactic("E5D3", 6, ""),
        };
        var deck = new List<CardDef> { HeroOf("FixtureWarlord", "Test", 2, 30) };
        // 同 id 的副本（每张 2 份）—— 判别「降费按【实例】钉住」还是「按卡 id 匹配全部副本」。
        foreach (var d in draw3)
            for (int k = 0; k < 2; k++)
                deck.Add(new CardDef(d.Id, d.Name + "Copy" + k, "tactic", "", "common", "Test",
                                     d.Cost, 0, 0, 0, null));
        deck.AddRange(draw3);
        deck.Add(Tactic("E5H1", 8, ""));      // 起手垫牌（不是「刚抽到的」⇒ 一份都不许降）
        deck.Add(Tactic("E5H2", 7, ""));
        deck.Add(am54);                       // 牌库顶

        var ctx = RuleCore.NewBattle(deck, Deck(Unit("E5Foe", 1, 1, 5)), seed: 0, shuffle: false);
        // `warlordlowhp` 判的是 `ctx.Players[owner].Warlord.Health <= 阈值`（阈值从条件原文解析，见
        // `EffectResolver.ConditionHolds` 那一支）⇒ 这里直接拨血。
        // ⚠️ 加 null 守卫：夹具若哪天不再产督军，**红一条断言**比在这里抛 NRE 好 ——
        //    后者会把整个 `RuleEngineTest.Run` 打断（同宿主几百条断言一起没跑）。
        if (ctx.Players[0].Warlord != null) ctx.Players[0].Warlord.Health = warlordHp;
        ctx.Players[0].Energy = 9;            // `AM54` 4 费；给足、别让费用闸挡住
        ctx.Players[0].MaxEnergy = 9;
        return ctx;
    }

    /// <summary>`③·d`：打出 `AM54` 之后**新增到手牌**的那几份 —— 按**实例**判（`ReferenceEquals`），
    /// 不按卡名（同名副本会让卡名匹配静默多算）。</summary>
    static List<CardInstance> E5DrawnAfterPlay(BattleContext ctx, int p, List<CardInstance> before)
    {
        var r = new List<CardInstance>();
        foreach (var h in ctx.Players[p].Hand)
        {
            bool was = false;
            foreach (var b in before) if (ReferenceEquals(b, h)) was = true;
            if (!was) r.Add(h);
        }
        return r;
    }

    // ==================================================================
    //  🆕 2026-10-28（执行代理 `B93_F4`）—— 引擎小文件族的三条判据落地
    //   · `BoardSlots.TryGetCardInPos`      = 原版 `MinionManager.GetCardInPos`（账 L1106）
    //   · `UnitState.AnyEffectGrantsTrait`  = 原版 `EntityScript.HasCurrentTraitActiveEffect`（账 L1041）
    //   · `BoardSlots.CountOnSide` 不含督军 = 原版 `AddMinionOccupation` 的 `cardType==10 ⇒ return`（账 L1094）
    //  ⚠️ 追加在文件尾（`TestE5SmallEngineGaps` 里那行调用是**就地接在** `E5Case3PremiseChecks();` 同一行，
    //     零行数变化 —— 账 `战斗逻辑驱动:80` 的备注引用了本文件 `:356-463`，⛔ 别在中段插行）。
    // ==================================================================

    static void E5Case4B93F4Primitives()
    {
        Section("🆕 B93_F4：`GetCardInPos`（L1106）· `HasCurrentTraitActiveEffect`（L1041）· 占位表不含督军（L1094）");

        // ---------- ① `BoardSlots.TryGetCardInPos` + 占位表不含督军 ----------
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var ps = ctx.Players[0];
            // 🧨 前提硬检查：督军在 4 号格（此后 `CountOnSide` 若把督军数进去，会先在这里红）
            CheckTrue(ps.Warlord != null && ReferenceEquals(ps.Board[BoardSpec.WarlordSlot], ps.Warlord),
                      "🧨 前提：督军占 `BoardSpec.WarlordSlot`");
            var l0 = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Left, 0), Unit("B93L0", 1, 1, 1));
            var l1 = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Left, 1), Unit("B93L1", 1, 1, 1));
            var r0 = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 0), Unit("B93R0", 1, 1, 1));

            UnitState got;
            // 🧨 有符号格号 → 卡：左 idx0 = -1、左 idx1 = -2、右 idx0 = +1（原版 `left[-index-1]` / `right[index-1]`）
            CheckTrue(BoardSlots.TryGetCardInPos(ps, -1, out got) && ReferenceEquals(got, l0),
                      "🧨 `GetCardInPos(-1)` = 左侧贴督军那格");
            CheckTrue(BoardSlots.TryGetCardInPos(ps, -2, out got) && ReferenceEquals(got, l1),
                      "🧨 `GetCardInPos(-2)` = 左侧第二格");
            CheckTrue(BoardSlots.TryGetCardInPos(ps, +1, out got) && ReferenceEquals(got, r0),
                      "🧨 `GetCardInPos(+1)` = 右侧贴督军那格");
            // 🧨 0 / 越界 ⇒ false + 出声（原版 LogWarning + null；改前直索引 `Board[]` 会静默越界）
            CheckTrue(!BoardSlots.TryGetCardInPos(ps, 0, out got) && got == null,
                      "🧨 `GetCardInPos(0)` = 督军格 ⇒ 原版 LogWarning + null");
            CheckTrue(!BoardSlots.TryGetCardInPos(ps, -3, out got) && got == null,
                      "🧨 左侧只有 2 个 ⇒ `-3` 越界（原版 `-index > Count` ⇒ LogWarning + null）");
            // 🧨 与 `OriginalIndexOfCard` 互为逆（灭自证：任一侧改错都红）
            Check(BoardSlots.OriginalIndexOfCard(ps, l0), -1, "🧨 往返：l0 的有符号格号 = -1");
            Check(BoardSlots.OriginalIndexOfCard(ps, r0), +1, "🧨 往返：r0 的有符号格号 = +1");
            Check(BoardSlots.OriginalIndexOfCard(ps, ps.Warlord), 0, "🧨 督军不在那两条表里 ⇒ 0");
            // 🧨 L1094：人数/占位表**不含督军**（原版 `AddMinionOccupation` 的 `cardType==10 ⇒ return`）
            Check(BoardSlots.CountOnSide(ps, BoardSlots.Left), 2, "🧨 左侧人数 = 2（督军不计）");
            Check(BoardSlots.CountOnSide(ps, BoardSlots.Right), 1, "🧨 右侧人数 = 1（督军不计）");
        }

        // ---------- ② `UnitState.AnyEffectGrantsTrait` / `RemoveTraitEffect` 的第二道闸 ----------
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 2), Unit("B93T", 1, 1, 3));
            // 两条**独立记录**（`extrinsic` ⇒ 不合并 —— 原版 `SameTimingAsEffect` 那条 extrinsic 闸）
            var r1 = u.ApplyTraitEffect("e5trait", 1, source: "A", extrinsic: true);
            var r2 = u.ApplyTraitEffect("e5trait", 1, source: "B", extrinsic: true);
            // 🧨 前提硬检查：真的是两条记录（若被合并成一条，下面「留着」那条会假绿）
            Check(u.TraitEffects.Count, 2, "🧨 前提：两条独立 `addTrait` 记录");
            CheckTrue(u.Has("e5trait"), "🧨 前提：关键词已挂上");
            CheckTrue(u.AnyEffectGrantsTrait("e5trait"), "🧨 有记录在给 ⇒ 真");
            // 摘第一条 ⇒ 第二条还在给 ⇒ **关键词留着**（原版 `!HasCurrentTraitActiveEffect` 那道闸）
            CheckTrue(u.RemoveTraitEffect(r1), "🧨 摘掉 r1（返回 true）");
            CheckTrue(u.Has("e5trait"), "🧨 还有一条记录在给 ⇒ 关键词**不许**被摘");
            CheckTrue(u.AnyEffectGrantsTrait("e5trait"), "🧨 仍有一条在给 ⇒ 真");
            // 摘第二条 ⇒ 没人给了 ⇒ 关键词摘掉
            CheckTrue(u.RemoveTraitEffect(r2), "🧨 摘掉 r2（返回 true）");
            CheckTrue(!u.Has("e5trait"), "🧨 没有记录再给 ⇒ `RemoveTraitSilently` 摘掉关键词");
            CheckTrue(!u.AnyEffectGrantsTrait("e5trait"), "🧨 没人给了 ⇒ 假");
            // 🧨 灭自证：**单条记录**那一档（若把闸写成「永远留着」，这条红）
            var r3 = u.ApplyTraitEffect("e5trait2", 1, source: "C", extrinsic: true);
            CheckTrue(u.Has("e5trait2"), "🧨 前提：单条记录已挂");
            CheckTrue(u.RemoveTraitEffect(r3), "🧨 摘掉唯一一条（返回 true）");
            CheckTrue(!u.Has("e5trait2"), "🧨 单条那条：一摘关键词就没了（防「永远留着」）");
            // 🧨 残骸闸：`isRemnant && !isLeavingRemnant` ⇒ 原版整段跳过（`+0x65 && !+0x66`）
            var u2 = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 3), Unit("B93RM", 1, 1, 1));
            u2.ApplyTraitEffect("e5trait3", 1, source: "D", extrinsic: true);
            u2.IsRemnant = true; u2.IsLeavingRemnant = false;
            CheckTrue(!u2.AnyEffectGrantsTrait("e5trait3"), "🧨 残骸（未离开）⇒ 原版整段跳过 ⇒ 假");
            u2.IsLeavingRemnant = true;
            CheckTrue(u2.AnyEffectGrantsTrait("e5trait3"), "🧨 正在离开残骸 ⇒ 门放行 ⇒ 真");
        }
    }

    // ==================================================================
    //  🆕 2026-10-28（`B93_F4b` · 账 `日常_奖励:39` = `ChallengeLogMgr.LogPlayTactic`）
    //  判据（三源一致）：原版只做两件 —— 发一次 `CardPlayedSignal`（带 cost）＋ 给**打出的那一方**
    //  的 `PlayerBattleStats.tacticsPlayed(+0x14)` **+1**。**没有**「7 类战术分别累加」那一维
    //  （`il2cpp_out/dump.cs:37205-37210` 的 `PlayerBattleStats` 只有 3 个 int；
    //   `decomp_full/ChallengeLogMgr__LogPlayTactic.c:401-408` 只有一处自增）。
    //  ⇒ 我们已有的「每方战术张数」= `CardsPlayedByTag[seat*TagCount + CardPlayedTag.Tactic]`
    //     与它**同值**；本轮只补了一个同名读数口 `AchievementTally.TacticsPlayedOf`。
    // ==================================================================

    static void E5Case4bLogPlayTactic()
    {
        Section("🆕 B93_F4b：`LogPlayTactic` = 每方 `tacticsPlayed+1`（原版【无】「7 类」维度）");

        // ---------- ① 战术卡：打出的那一方 +1，另一方不变 ----------
        var ctx = Battle(new CardDef[] { Tactic("B93Tac", 1, "") }, new CardDef[0]);
        // 🧨 前提硬检查：开局两边都是 0（否则下面的 +1 无法判别）
        Check(ctx.Tally.TacticsPlayedOf(0), 0, "🧨 前提：座位0 开局 0");
        Check(ctx.Tally.TacticsPlayedOf(1), 0, "🧨 前提：座位1 开局 0");
        ctx.Players[0].Energy = 9; ctx.Players[0].MaxEnergy = 9;
        int idx = HandIdx(ctx, 0, "B93Tac");
        CheckTrue(idx >= 0, "🧨 前提：战术卡在手里");
        CheckCode(RuleCore.PlayTactic(ctx, 0, idx, -1), RuleCodes.OK, "🧨 打出战术卡（`PlayTactic` 应 OK）");
        // 🧨 原版 `isPlayer ? PlayerBattleStats : EnemyBattleStats` 那一支 ⇒ 只有打出的那方 +1
        Check(ctx.Tally.TacticsPlayedOf(0), 1, "🧨 座位0 战术张数 +1");
        Check(ctx.Tally.TacticsPlayedOf(1), 0, "🧨 座位1 不变（记给打出的那一方）");

        // ---------- ② 灭自证：**单位**卡不落战术格 ----------
        //   若有人把「所有出牌」都当成战术（或把 TacticsPlayedOf 接错到总出牌数），这一条会红。
        var ctx2 = Battle(new CardDef[] { Unit("B93UnitCard", 1, 1, 1) }, new CardDef[0]);
        ctx2.Players[0].Energy = 9; ctx2.Players[0].MaxEnergy = 9;
        int uidx = HandIdx(ctx2, 0, "B93UnitCard");
        CheckTrue(uidx >= 0, "🧨 前提2：单位卡在手里");
        if (uidx >= 0)
            CheckCode(RuleCore.PlayCard(ctx2, 0, uidx, 5), RuleCodes.OK, "🧨 打出单位卡（`PlayCard` 应 OK）");
        Check(ctx2.Tally.TacticsPlayedOf(0), 0, "🧨 单位卡**不**落战术格（防「出牌即战术」）");
        CheckTrue(ctx2.Tally.CardsPlayedOf(0, CardPlayedTag.Tactic) == ctx2.Tally.TacticsPlayedOf(0),
                  "🧨 读数口 = `CardsPlayedByTag` 的 Tactic 格（同一本账，⛔ 不是第二本）");
    }
}
