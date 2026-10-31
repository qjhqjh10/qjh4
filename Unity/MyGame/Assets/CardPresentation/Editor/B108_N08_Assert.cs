// B108_N08_Assert.cs — 第一百零八会话 · 执行代理 `B108_R3`（N08：原版 80 档 = `damageCriteria.valueOptions == 80`）
//
// ============================ 它盯的是什么 ============================
// `ChooseValueOptions.MinPlusMaxTimesMultiplier(80)` 的两个半句都必须**按数据位**走，而不是按形状猜：
//   ① **折成 1 遍**：`TryStructuredMultiplier` 的 `IsMinPlusMaxTimesMultiplier`（`B108_A1` 改的 `EffectResolver.cs`）
//      —— 原版 `AbilityLogic__PlayAbility.c:1466-1508` 的伤害支只调一次 `DealMultiDamage`（`:1504`）。
//   ② **值 = `min + max × multiplierFactor`**：`RuleCore.ApplyMinPlusMaxTimesMultiplier`
//      （`EffectResolverPartial_B88E2.cs`，本代理）—— 判据 `AbilityLogic__GetValueFromCriteria.c:113-116` + VA 0x18081b71f/728/731。
//
// 旧写法（已撤）是**形状等价**：`ta == 50 ∧ op.AmountRef == "enemyhand"` ⇒ 18·0 违规（`B108_N08`）。
// 判据全文 → `资料/完全复刻/判据/B108_N08_valueOptions80.md` · `判据/B88_R2_80档_VA判定.md`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · **A（数据说 80、形状不是）**：夹具正文 `Deal 2 damage …`（⇒ `Amount = 2`、`AmountRef` 为空 ——
//     旧形状判据**不会**折）+ `damageCriteria = {min 1, max 2, opt 80}` + 敌方手牌 3 张合规卡。
//     期望 = `1 + 2×3 = 7` 点、**一次**。
//       · 把值那一半删掉（退回文本 2）⇒ 实得 2 ⇒ 红；
//       · 把折 1 遍删掉（退回 repeats = 3）⇒ 实得 21 ⇒ 红；
//       · 把判据退回形状（`AmountRef` 为空 ⇒ 不折）⇒ 21 ⇒ 红。
//   · **B（形状像 80、数据不是）**：正文 `… equal to the number of cards in the enemy hand`（⇒ `AmountRef = "enemyhand"`）
//     + `damageCriteria = {opt 0}` + 同一条 `mult` 行（`ta 50`）。期望 = repeats 3 × 每下 3 点 = **9**。
//       · 谁把形状判据写回来（`ta==50 ∧ AmountRef=="enemyhand"` ⇒ 折成 1 遍）⇒ 实得 3 ⇒ 红。
//   ⚠️ B 是**判别式**、不是原版语义（原版不会有「opt 0 + 这段文本」的组合）；它的唯一职责是
//      「证明折/不折看的是数据位」。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// 宿主 `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里，紧接下面这一行之后（**同一物理行追加**，插在该行第一个 `//` 之前）：
//      Step(TestB88PerTargetRoll);
// 追加：
//      { CheckB108N08ValueOptions80(); }
// （参考 `B108_A1_TriggerEmit_Assert.cs`：⛔ 不用 `Step(...)`、入口不带 `Test` 前缀 —— `Step` 会进 `_ranCases` 记账。）
//
// 🔴 **依赖（两条必须同批落地）**：本件 A 同时依赖
//   ① `B108_A1` 的折 1 遍（`EffectResolver.cs`）· ② 宿主调用点 `EffectResolver.cs:840` 补传 `owner, chosen`
//   （`ApplyRefCardValueOption(ctx, op, card, owner, chosen)`）。
//   缺 ① ⇒ A 实得 21；缺 ② ⇒ A 实得 2（日志会写「调用链没递 `owner`」）。两条断言都会红。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件入口（宿主 `Run()` 里一个 `{ … }` 块调；⛔ 不叫 `Run` / 不带 `Test` 前缀）。</summary>
    public static void CheckB108N08ValueOptions80()
    {
        Section("🆕 B108_N08：原版 80 档（`damageCriteria.valueOptions == 80`）—— 数据位判据 + `min + max × 目标数`");
        B108N08Case("B108V80A", "Deal 2 damage to an enemy troop", 1, 2, 80, 7,
                    "★ A（数据说 80、形状不是）：`1 + 2 × 3 = 7` 点、**一次**"
                  + "｜🧨 值没接 ⇒ 2；折 1 遍没接 ⇒ 21；判据退回形状 ⇒ 21");
        B108N08Case("B108V80B", "Deal damage to an enemy troop equal to the number of cards in the enemy hand",
                    1, 2, 0, 9,
                    "★ B（形状像 80、数据不是）：**不折** ⇒ 3 遍 × 每下 3 点 = 9"
                  + "｜🧨 把形状判据写回来（`ta==50 ∧ AmountRef==\"enemyhand\"` ⇒ 折成 1 遍）⇒ 3");
    }

    /// <summary>一张「Rally + 一条 `mult ta=50`」的夹具单位。`opt` = 数据位 `damageCriteria.valueOptions`
    /// （`80` = 原版 `MinPlusMaxTimesMultiplier` / `0` = `Normal`）。</summary>
    static CardDef B108N08Card(string name, string desc, int min, int max, int opt)
    {
        // 敌手（异方）手牌、单位 + 战术（`includeMinions=1 · includeSpells=1`），其余一律显式 0
        // （`CountSetSpec` 的默认是 `Unknown(-1)` ⇒ 不显式落 0 会被判「未知 ⇒ 数不出来」，见 `TargetCriteriaEval.MatchesRawCard`）。
        var mult = new CardDef.CountSetSpec
        {
            TargetsAffected = 50,
            FriendlyUnits = 0, EnemyUnits = 1,
            IncludeHero = 0, IncludeMinions = 1, IncludeSpells = 1,
            IncludeWhispers = 0, IncludeWeapons = 0, IncludeRemnants = 0, IncludeHeroParts = 0,
            IncludeSelf = 0, ExcludeNotSelf = 0, ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0,
            ChosenArmy = 0, ArmyFilter = 0, KeywordFilter = 0, ExcludeRace = 0,
            TraitsFilter = 0, ExcludeTrait = 0,
            MaxTargets = 0, TargetSelectMethod = 0,
            AttackFilter = 0, AttackFilterOption = 0,
            CurrentHealthFilter = 0, CurrentHealthFilterOption = 0,
            ManaCostFilter = 0, ManaCostFilterOption = 0,
            RarityFilter = 0, RarityFilterOption = 0,
            SpellType = 0, SelectFromChooseResolution = 0,
        };
        var conds = new[]
        {
            new CardDef.ConditionSpec { Ability = 0, Logic = 0, Role = "mult", Condition = 0, MultiplierSet = mult },
        };
        var card = new CardDef(name, name, "unit", desc, "common", "Test", 1, 0, 9, 0,
                               new[] { "Rally" }, abilityConditions: conds);
        card.AttachCardTraits(new string[0]);      // 空表 = 「已挂、只是没有 trait」（`null` 会让卡面级 criteria 判不了）
        card.AttachAbilityDamage(new[]
        {
            new CardDef.AbilityDamageSpec { Ab = 0, Lg = 0, Min = min, Max = max, ValueOptions = opt, Type = 0 },
        });
        return card;
    }

    /// <summary>组局 + 打出 + 断伤害。敌方手牌固定 = 2 单位 + 1 战术 + 1 督军（督军被 `includeHero=0` 排除 ⇒ 计数 3）。</summary>
    static void B108N08Case(string name, string desc, int min, int max, int opt, int expectDrop, string msg)
    {
        var pool = CardDatabase.Load();
        var card = B108N08Card(name, desc, min, max, opt);

        // 前提 ①：数据位真的挂上了（附错误值 ⇒ 后面全无意义）。
        var spec = card.AbilityDamageAt(0, 0);
        CheckTrue(spec != null && spec.ValueOptions == opt,
                  $"（前提）夹具「{name}」的 `abilityDamage` 挂上了：`ValueOptions = {opt}`（实得 {(spec == null ? "null" : spec.ValueOptions.ToString())}）");
        // 前提 ②：正文真的解析成了触发 op（否则「0 点伤害」会被误读成机制生效）。
        var tops = card.TriggerOps(KeywordTable.Rally);
        CheckTrue(tops != null && tops.Count > 0,
                  $"（前提）夹具「{name}」的 Rally 正文解析出了 op（实得 {(tops == null ? 0 : tops.Count)} 条）");
        if (spec == null || tops == null || tops.Count == 0) return;

        var ctx = B101R2_B88Battle(card, pool);      // 敌手起手 = 2 单位 + 1 战术 + 1 督军（见 `RuleEngineTest_B101R2.cs`）
        ToP1Turn(ctx, 1);
        ctx.Players[0].Energy = 20;

        int qual = 0;                                 // 现算「敌手该数几张」——⛔ 不写死，夹具一改就红
        foreach (var inst in ctx.Players[1].Hand)
            if (inst != null && inst.Card != null
                && (inst.Card.Type == "unit" || inst.Card.Type == "tactic")) qual++;
        Check(qual, 3, $"（前提）敌方手牌里「单位 + 战术」= 3 张（实得 {qual}）");

        var foe = Place(ctx, 1, 3, Unit("B108Foe", 1, 0, 20));   // 唯一敌方部队 ⇒ 「an enemy troop」目标确定
        int hp0 = foe.Health;
        int slot = SimpleAI.FirstFreeSlot(ctx.Players[0]);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, name), slot), RuleCodes.OK,
                  $"（前提）打出夹具「{name}」（Rally 立刻结算）");

        Check(hp0 - foe.Health, expectDrop, msg + LogTail(ctx));

        // 值那一半的出声（80 档专用）：A 必须有；B（数据说不是 80）**不许**有。
        bool valueSaid = false;
        foreach (string e in ctx.Events)
            if (e != null && e.Contains("min + max × 目标数") && e.Contains("80 档")) valueSaid = true;
        CheckTrue(opt == 80 ? valueSaid : !valueSaid,
                  opt == 80
                    ? "★ 日志里说得出「`min + max × 目标数`」（= 80 档的值走数据位；缺 ⇒ 说明调用链没递 `owner`）"
                    : "★ 反例：数据位不是 80 ⇒ **不许**走 `min + max × 目标数` 那条路（走回来 = 形状判据复活）");
    }
}
