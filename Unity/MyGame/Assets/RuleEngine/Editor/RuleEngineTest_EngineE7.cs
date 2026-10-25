// RuleEngineTest_EngineE7.cs — 引擎线 `E7`（2026-10-25 · 第七十五会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineE5` /
// `_EngineE4` / `_EngineE3` … 都在），同一时刻只有一个写手改一个文件 ⇒ 本批测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestE7FourGates)`**
// （⚠️ 另需重生成 `RuleEngineTest_Diag.cs`：`VerifyRunCaseCoverage` ② 拿「反射到的用例方法」↔
//   「生成物里的 `RunCaseNames`」对账 ⇒ 不重生成会红）。
//
// 本批 = E5 留下的**四道「现池 0 张、判据已查全、但没建」的候选闸**（铁律 11：要建，
// ⛔ 不是「今天走不到就不做」）：
//   ① `ward`(140) / `wardWb`(145) —— 原版 `isCurrentlyWarded`，**只在异侧**挡
//   ② `summonRift`(730) —— **不分侧**，一律不可点
//   ③ `tainted`(160) —— 异侧 ∧ 载体(`thisCard`)卡型 = Hero(10) ∧ 候选 tainted ∧ `!HasVanguardTrait`
// 判据全文（逐句 + 出处 + 为什么这么分侧）→ `资料/普查产出_1025/E7_四道闸.md`，⛔ 不要重新论证。
//
// 🔴 每条断言都带 `🧨`（什么改动能让它红）。期望值一律写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestE7FourGates)`）。</summary>
    public static void TestE7FourGates()
    {
        Section("🆕 引擎线 E7：主动能力候选闸四道 —— ward/wardWb（异侧）· summonRift（不分侧）"
              + " · tainted（异侧 + 载体 Hero + 非隐身-vanguard）");
        E7Case0KeywordsRecognized();
        E7Case1Ward();
        E7Case2SummonRift();
        E7Case3Tainted();
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>施放者用的**单位型**主动能力卡（`Ability: Damage 3 EnemyUnit`）。
    /// <para>🔴 旧表（没有 `activeAbilityPickCriteria` / `unitConditions` 两列）⇒ 最后那层 36 格闸
    /// 「判不了 ⇒ 放行 + 出声」（`ActiveAbilityPickGate` 的既有契约），于是本文件断的是**前面那四道闸**，
    /// 不会被 criteria 一层的「未知」搅进来。</para>
    /// <para>⚠️ `hasAlternativeAbility: 0` 必须给 —— `-1` ⇒ `UsesAlternativeAbility` 回 `null`
    /// ⇒ 整道闸「判不了 ⇒ 放行」，四道闸里那三条 OK 的断言会**假绿**。</para></summary>
    static CardDef E7Caster(string id)
    {
        return new CardDef(id, id, "unit", "", null, "Test", 1, 1, 5, 0,
                           new[] { "Ability: Damage 3 EnemyUnit" },
                           hasAlternativeAbility: 0);
    }

    /// <summary>E7 的棋盘：我在槽 3 摆一个**单位型**施放者、对面槽 3 摆一个普通单位。
    /// <para>⚠️ `Place` 是**直写格子**（跳过部署推人）⇒ 传几号就是几号；
    /// 但读回来仍走 <see cref="E4SlotOf"/>（别假设位置）。</para></summary>
    static BattleContext E7Board(out int me, out int casterSlot, out int foeSlot)
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
        me = ctx.Active;
        var caster = Place(ctx, me, 3, E7Caster("E7Caster"));
        var foe = Place(ctx, 1 - me, 3, Unit("E7Foe", 1, 1, 5));
        casterSlot = E4SlotOf(ctx, me, caster);
        foeSlot = E4SlotOf(ctx, 1 - me, foe);
        return ctx;
    }

    // ==================================================================
    //  ⓪ 前提：四个词认得出来（不被 `Normalize` 静默丢弃）
    // ==================================================================

    static void E7Case0KeywordsRecognized()
    {
        // 为什么要单独一条：**不登记进 `KeywordTable.Prefixes` ⇒ `Normalize` 返回 null ⇒
        // `KeywordTable.Parse` 与 `CardDef` 构造函数双双丢弃**（卡数据 / 卡面正文写它们会被静默吃掉）——
        // 那样下面 ①②③ 的「挡」全是假绿（词根本进不了 `UnitState.Has`）。
        Check(KeywordTable.Normalize("ward"), ConditionEval.Ward,
              "★★ ⓪ `ward` 认得出（→ `ConditionEval.Ward`）"
            + "｜🧨 从 `Prefixes` 删掉那一条 ⇒ 卡数据里的 ward 静默消失、① 的「挡」变绿（假的）⇒ 红");
        Check(KeywordTable.Normalize("wardWb"), ConditionEval.WardWb,
              "★★★ ⓪ **原版拼写** `wardWb` 认得出，且归到 **WardWb(145)** 而不是 Ward(140)"
            + "｜🧨 把 `wardwb` 那条排到 `ward` 后面（`Normalize` = `StartsWith` + 先到先得）"
            + " ⇒ 这里回 `\"ward\"` ⇒ 红（145 被静默并成 140）");
        CheckTrue(KeywordTable.Normalize("wardWb") != ConditionEval.Ward,
                  "★★ ⓪ 反向确认：`wardWb` **不是** `ward`（145 与 140 是两个词，⛔ 别合并）");
        Check(KeywordTable.Normalize("tainted"), ConditionEval.Tainted,
              "★★ ⓪ `tainted` 认得出（→ `ConditionEval.Tainted`）");
        Check(KeywordTable.Normalize("summonRift"), KeywordTable.SummonRift,
              "★★ ⓪ **原版拼写** `summonRift` 认得出（→ `KeywordTable.SummonRift`）");

        // 走**整条卡构造**（`KeywordTable.Parse` → `Normalize`）确认一个都不丢。
        var card = Unit("E7KwHolder", 1, 1, 5, "ward", "wardWb", "tainted", "summonRift");
        Check(card.Keywords.Count, 4,
              "★★ ⓪ 四个词经 `CardDef` 构造函数后**一个不少**（4 条）"
            + "｜🧨 任一条没登记 ⇒ 只剩 3 条 ⇒ 红（这条挡的就是「静默丢弃」）");
        CheckTrue(card.Keywords.ContainsKey(ConditionEval.Ward)
                  && card.Keywords.ContainsKey(ConditionEval.WardWb)
                  && card.Keywords.ContainsKey(ConditionEval.Tainted)
                  && card.Keywords.ContainsKey(KeywordTable.SummonRift),
                  "★★ ⓪ 四条**逐键**都在 `Keywords` 里（`Ward` 与 `WardWb` 是**两个**键）"
                + "｜🧨 前缀表顺序写反（`ward` 抢先吃 `wardWb`）⇒ 这里少一个 `wardwb` ⇒ 红");
    }

    // ==================================================================
    //  ① ward / wardWb —— **只在异侧**挡
    // ==================================================================

    static void E7Case1Ward()
    {
        int me, cs, fs;
        var ctx = E7Board(out me, out cs, out fs);
        var caster = ctx.Players[me].Board[cs];

        var foeWard = Place(ctx, 1 - me, 1, Unit("E7FoeWard", 1, 1, 5, "ward"));
        int foeWardSlot = E4SlotOf(ctx, 1 - me, foeWard);
        var foeWardWb = Place(ctx, 1 - me, 2, Unit("E7FoeWardWb", 1, 1, 5, "wardWb"));
        int foeWardWbSlot = E4SlotOf(ctx, 1 - me, foeWardWb);
        var allyWard = Place(ctx, me, 1, Unit("E7AllyWard", 1, 1, 5, "ward"));
        int allyWardSlot = E4SlotOf(ctx, me, allyWard);
        var foeVanguard = Place(ctx, 1 - me, 5, Unit("E7FoeVanguard", 1, 1, 5, KeywordTable.Vanguard));
        int foeVanguardSlot = E4SlotOf(ctx, 1 - me, foeVanguard);

        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, 1 - me, foeWardSlot), RuleCodes.ErrTarget,
                  "★★★ ① 异侧的 **`ward(140)`** 候选 **不可点**"
                + "（`BattleManager__IsValidActiveAbilityTarget.c:51-54` + "
                + "`RawCardScript__IsValidActiveAbilityTarget.c:94-100`）"
                + "｜🧨 删掉 `IsCurrentlyWarded` 那一道 ⇒ 红");
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, 1 - me, foeWardWbSlot), RuleCodes.ErrTarget,
                  "★★★ ① 异侧的 **`wardWb(145)`** 候选 **同样不可点**（`isCurrentlyWarded` = 140 ∨ 145）"
                + "｜🧨 只判 `ward` 不判 `wardwb` ⇒ 红");
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, 1 - me, fs), RuleCodes.OK,
                  "★★ ① 异侧的**普通**单位仍可点（挡的是那一个词，不是「异侧全挡」）");
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, me, allyWardSlot), RuleCodes.OK,
                  "★★★ ① **同侧的 `ward` 单位可点**（原版只在候选 `isPlayer != thisCard.isPlayer` 时才看它）"
                + "｜🧨 把 ward 那一道写成不分侧（照 `summonRift` 抄）⇒ 红；"
                + " 这一条与 ②「同侧 summonRift 也不可点」**互斥** —— 一个「按侧统一处理」的实现不可能两处都过");
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, 1 - me, foeVanguardSlot), RuleCodes.OK,
                  "★★ ① 对照组：异侧带**别的关键词**（`vanguard`）仍可点"
                + "｜🧨 把闸写成「候选有任何关键词 ⇒ 拒」⇒ 红");

        // ---- 灭自证（甲）：闸读的是**候选**的关键词，不是**施放者**的 ----
        //   同一句判据若误写成 `caster.Has(Ward)`（或两处都判），候选侧那几条仍绿、这一条必红。
        caster.AddKeyword(ConditionEval.Ward, 1);
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, 1 - me, fs), RuleCodes.OK,
                  "★★★ ① 灭自证：**施放者自己带 `ward`** ⇒ 敌方普通候选**照旧可点**"
                + "（原版读的是 `param_3` 候选侧的 `HasCurrentTrait`）"
                + "｜🧨 判据写成 `caster.Has(Ward)` / `caster.Has(Ward) || 候选.Has(Ward)` ⇒ 红");
    }

    // ==================================================================
    //  ② summonRift —— **不分侧**，一律不可点
    // ==================================================================

    static void E7Case2SummonRift()
    {
        int me, cs, fs;
        var ctx = E7Board(out me, out cs, out fs);
        var caster = ctx.Players[me].Board[cs];

        var foeRift = Place(ctx, 1 - me, 1, Unit("E7FoeRift", 1, 1, 5, "summonRift"));
        int foeRiftSlot = E4SlotOf(ctx, 1 - me, foeRift);
        var allyRift = Place(ctx, me, 1, Unit("E7AllyRift", 1, 1, 5, "summonRift"));
        int allyRiftSlot = E4SlotOf(ctx, me, allyRift);

        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, 1 - me, foeRiftSlot), RuleCodes.ErrTarget,
                  "★★★ ② 异侧的 **`summonRift(730)`** 候选不可点"
                + "（`BattleManager__IsValidActiveAbilityTarget.c:55-56`）"
                + "｜🧨 删掉 `SummonRift` 那一道 ⇒ 红");
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, me, allyRiftSlot), RuleCodes.ErrTarget,
                  "★★★ ② **同侧** `summonRift` **也不可点** —— 原版那道闸是**裸的**"
                + "（`HasCurrentTrait(候选, 0x2da) ⇒ return 0`，没有 `isPlayer !=` 前置）"
                + "｜🧨 把它照 ward/stealth 写成「只在异侧挡」⇒ 红；这是本文件里区分"
                + "「不分侧」与「只在异侧」的唯一判别式");
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, caster, 1 - me, fs), RuleCodes.OK,
                  "★★ ② 对照：普通敌方单位仍可点（不是「有这个词以外的原因也拒」）");
    }

    // ==================================================================
    //  ③ tainted —— 异侧 ∧ 载体(`thisCard`)卡型 = Hero(10) ∧ !(非隐身的 vanguard)
    // ==================================================================

    static void E7Case3Tainted()
    {
        int me, cs, fs;
        var ctx = E7Board(out me, out cs, out fs);
        var unitCaster = ctx.Players[me].Board[cs];            // 卡型 `unit`
        var heroCaster = ctx.Players[me].Warlord;              // 卡型 `hero`（坐在督军格）

        CheckTrue(heroCaster != null && heroCaster.Card != null && heroCaster.Card.Type == "hero",
                  "③ （前提）己方督军在场、且卡型 = `hero`（原版 `get_cardType(thisCard) == 10`）");
        CheckTrue(unitCaster != null && unitCaster.Card != null && unitCaster.Card.Type == "unit",
                  "③ （前提）另一个施放者的卡型 = `unit`（≠ 10）");

        var foeTainted = Place(ctx, 1 - me, 1, Unit("E7FoeTainted", 1, 1, 5, "tainted"));
        int foeTaintedSlot = E4SlotOf(ctx, 1 - me, foeTainted);
        var foeTaintedVg = Place(ctx, 1 - me, 2,
                                 Unit("E7FoeTaintedVg", 1, 1, 5, "tainted", KeywordTable.Vanguard));
        int foeTaintedVgSlot = E4SlotOf(ctx, 1 - me, foeTaintedVg);
        var foeTaintedVgSt = Place(ctx, 1 - me, 5,
                                   Unit("E7FoeTaintedVgSt", 1, 1, 5, "tainted", KeywordTable.Vanguard,
                                        KeywordTable.Stealth));
        int foeTaintedVgStSlot = E4SlotOf(ctx, 1 - me, foeTaintedVgSt);
        var allyTainted = Place(ctx, me, 1, Unit("E7AllyTainted", 1, 1, 5, "tainted"));
        int allyTaintedSlot = E4SlotOf(ctx, me, allyTainted);

        // ---- 正例（挡）：载体 Hero + 异侧 + tainted + 非 vanguard ----
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, heroCaster, 1 - me, foeTaintedSlot), RuleCodes.ErrTarget,
                  "★★★ ③ 载体是 Hero + 异侧 `tainted` 非-vanguard 候选 ⇒ **不可点**"
                + "（`RawCardScript__IsValidActiveAbilityTarget.c:94-102`）"
                + "｜🧨 删掉 tainted 那一段 ⇒ 红");

        // ---- 负例（不挡）：候选 tainted **且** vanguard ⇒ 逃生 ----
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, heroCaster, 1 - me, foeTaintedVgSlot), RuleCodes.OK,
                  "★★★ ③ 候选 = `tainted + vanguard`（非隐身）⇒ **可点**"
                + "（`HasVanguardTrait(候选)` 为真 ⇒ 那一闸整串为真）"
                + "｜🧨 写成「tainted ⇒ 一律拒」（忽略 vanguard 逃生）⇒ 红");

        // ---- 负例（不挡）：载体**不是** Hero ⇒ 即使候选 tainted 也不挡 ----
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, unitCaster, 1 - me, foeTaintedSlot), RuleCodes.OK,
                  "★★★ ③ 载体卡型 = `unit`（≠ Hero）⇒ 同一个 tainted 候选 **可点**"
                + "（原版 `get_cardType(param_2 /*thisCard*/) != 10` 时那一闸整串为真）"
                + "｜🧨 漏掉「载体是 Hero」这一格（= 见 tainted 就拒）⇒ 红；"
                + " 也挡「拿**候选**的 cardType 当载体」的写法（候选是 `unit` ⇒ 这里会拒 ⇒ 红）");

        // ---- 侧别（不挡）：同侧的 tainted 候选 ----
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, heroCaster, me, allyTaintedSlot), RuleCodes.OK,
                  "★★★ ③ **同侧** tainted 单位可点（`候选.isPlayer == thisCard.isPlayer` ⇒ 那一闸为真）"
                + "｜🧨 漏掉异侧前置 ⇒ 红");

        // ---- 载体身份的精确判据：**卡型 hero ≠ 坐在督军格** ----
        //   原版读 `get_cardType(thisCard)`（卡模板），不是「它在不在 4 号格」。
        //   把一张 hero 卡摆在**普通格**（`IsWarlord = false`）当施放者：
        //   照卡型的实现仍然挡；写成 `caster.IsWarlord` 的实现会漏。
        var heroAt6 = Place(ctx, me, 6, HeroOf("E7HeroAtSlot6", "Ultramarines", 2, 30));
        CheckTrue(!heroAt6.IsWarlord && heroAt6.Card != null && heroAt6.Card.Type == "hero",
                  "③ （前提）摆在普通格的 hero 卡：`IsWarlord = false` 但 `Card.Type == \"hero\"`");
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, heroAt6, 1 - me, foeTaintedSlot), RuleCodes.ErrTarget,
                  "★★★ ③ 灭自证：载体判据是**卡型 `hero`**（不是「坐在督军格」）——"
                + "普通格上的 hero 卡放技能照样挡 tainted 候选"
                + "｜🧨 把 `caster.Card.Type == \"hero\"` 写成 `caster.IsWarlord` ⇒ 这里漏 ⇒ 红");

        // ---- 对照（不挡）：普通候选 ----
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, heroCaster, 1 - me, fs), RuleCodes.OK,
                  "★★ ③ 对照：普通敌方单位仍可点（挡的是 tainted 那一格）");

        // ---- ⚠️ 如实标注：`HasVanguardTrait` 里的「隐身 ⇒ 不算 vanguard」那半，**在这个闸上观测不到** ----
        //   原版 `HasVanguardTrait(候选)` = `非隐身 ∧ vanguard`。但**异侧 + 隐身**的候选在更早的
        //   `stealth` 那一道（判据 4）就被拒了 ⇒ 到不了 tainted 这一步。下面这条**绿的原因不是**
        //   tainted 那半，而是 stealth 先挡 —— 写在这里是为了**不让下一个人误以为它验到了那半**。
        CheckCode(RuleCore.CanUseAbilityTarget(ctx, me, heroCaster, 1 - me, foeTaintedVgStSlot), RuleCodes.ErrTarget,
                  "③ （如实标注）`tainted + vanguard + stealth` 的异侧候选被拒 —— "
                + "**挡它的是 stealth 闸（判据 4），不是 tainted 里「隐身⇒不算vanguard」那半**"
                + "（那半在本闸上不可观测：异侧隐身更早就被拒了）");
    }
}
