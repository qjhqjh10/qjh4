// B148_W2_Assert.cs — 批次 **B148** · 执行写手 `W2`（引擎域）
//
// ============================ 它盯的是什么（五组，逐组 ↔ 一条施工单）============================
//   A 组 · `B147-DUTYUSED-2ND`：`RuleCore.CanStartActiveAbility` 的 `duty` 闸读 **`ActiveAbilityUsed`**
//         （原版 `+0x4C`），⛔ 不是 `DutyUsed` —— 与 `CanUseAlternative`（`:12183`）同一份判据。
//   B 组 · `B123-CA50SIX`/`SAU55`：`EffectText` 先把「`For the rest of this battle, `」这个**时长从句**剥掉
//         （否则载荷 = `for the rest of this battle, your warlords' talent`，下游全认不出）；
//         且 `SAU55` 的第二句**今天归数据路**（`abilityBuffs` 的 `handBuff ∧ ta=235 warlordsTalent`）。
//   C 组 · `B146-S1-G5-STOMP-FIX`：践踏闸 = `HasCurrentTrait(0x4d8)` ∧ **目标裸血 < 0** ∧ 相邻 > 0、
//         量 = `-target.Health`；★ 判别式 = **星镖杀之后照样践踏**（改前 `dealt = 0` ⇒ 不践踏）。
//   D 组 · `B146-S1-G4-KWVALUE`：三处「有这个词」→「值 > 0」（`shuriken` / `sentry` / `blast`）。
//         ⚠️ **今天零可观测**（池内没有 0 值）⇒ 结构断言（源码层钉住）+ 一条行为等价断言。
//   E 组 · `B146-STEALTH-CANCEL`：现身之后**取消这一记、压一条 `Instant` 续攻**（原版 `:429-489`）。
//         ★ 判别式 = 「失去潜行」的监听器（`ASH79` 那类）**先**结算、这一记**后**打。
//
// ============================ 原版判据（`d:/2/tools/decomp_full/`，逐条现读）============================
//   · `BattleManager__CanUseActiveAbility.c:92-97`：`HasCurrentTrait(param_2, 0x4c4) && *(char*)(param_2 + 0x4c)`；
//     `+0x4C` 的写点 = `CardScript__UsedActiveAbility.c:29/34/41/50`（四族共用 ⇒ 我们 `ActiveAbilityUsed`）。
//   · `FilterMethods__CheckIfMeetsCriteria.c:126-157`（`ta=235 warlordsTalent` = `GetHero` → `GetTalentCard` → 按卡名比）。
//   · `BattleManager__ShouldTriggerStompDamage.c` 全文：`HasCurrentTrait(0x4d8)` ∧ `*(int*)(target+0x68) < 0` ∧ `0 < param_4`；
//     量 = `_ResolveAttack_d__438__MoveNext.c:1089` 的 `(uVar13 ^ uVar13>>31) - (uVar13>>31)` = `abs(health)`。
//   · `_ResolveAttack_d__438__MoveNext.c:618-620`（`0 < CurrentShuriken`）· `:721-722`（`0 < CurrentSentry`）·
//     `BattleManager__ShouldTriggerAdjacentDamage.c`（`0 < CurrentBlast`）。
//   · `_ResolveAttack_d__438__MoveNext.c:429-489`：`RemoveTraitAndEffects(0x46)` → `BroadcastUnitLoseStealth` →
//     `CancelAttack` → 两次 `ClearPendingDamage` → `AddFollowResolutionAttackContinuationAction`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2：闸改回 `u.DutyUsed` ⇒ 红；A3：闸写成 `DutyUsed || ActiveAbilityUsed` ⇒ 红。
//   · B1：`SubjectCostOp` 去掉 `CostDurationPrefixes` 那一段 ⇒ 载荷带回从句 ⇒ 红。
//   · C2：践踏闸改回 `dealt > hpBefore` ⇒ 星镖杀那一档不再践踏 ⇒ 红；C4（反例）不带 `stomp` ⇒ 相邻一点不掉。
//   · D1：三处任一改回 `Has(...)` ⇒ 红（源码层）。
//   · E5：把续攻改回「就地继续往下打」⇒ 监听器的 `Hit` 排到这一记**之后** ⇒ 下标比较红；
//     E6（反例）攻击者不带 `stealth` ⇒ 监听器一条都不响（证明 E5 那条 `Hit` 的成因就是「失去潜行」）。
//
// ============================ 怎么接（宿主，调度台统一做）============================
//   ① `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行、第一个 `//` 之前**追加：
//      `Guard("B148W2_Assert", B148_W2_Assert.Run); _pass += B148_W2_Assert.Pass; _fail += B148_W2_Assert.Fail;`
//   ② 引擎域之外**不需要**接线（本批没有表现层断言）。
//   ⚠️ 本类在**全局命名空间**、类名 == 文件名 ⇒ 也可直接 `-executeMethod B148_W2_Assert.Run`。
using System;
using System.Collections.Generic;
using System.IO;
using RuleEngine;
using UnityEngine;

/// <summary>B148-W2：duty 闸 / SAU55 降费 / 践踏 / 关键词值闸 / 现身续攻。</summary>
public static class B148_W2_Assert
{
    const string P = "[B148-W2] ";
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int RunCount { get { return _pass + _fail; } }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log(P + "--- 🆕 B148-W2：duty 闸 / SAU55 / 践踏 / 关键词值闸 / 现身续攻 ---");
        GroupA_DutyGateReadsActiveAbilityUsed();
        GroupB_Sau55CostSentence();
        GroupC_StompAfterShurikenKill();
        GroupD_KeywordValueGates();
        GroupE_StealthCancelContinuation();
        GroupF_SpecialAttackClassification();
        GroupG_PopupAmountAndDamageKind();
        Debug.Log(P + "--- B148-W2 合计：通过 " + _pass + " / 失败 " + _fail + " ---");
    }

    // ==================================================================
    //  夹具（口径与 `B140_WA_Assert` / `RuleEngineTest.Place` 一致）
    // ==================================================================
    static CardDef Hero(string id)
        => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static CardDef Unit(string id, int cost, int atk, int hp, string desc = "", string[] keywords = null)
        => new CardDef(id, id, "unit", desc, null, "Test", cost, atk, hp, 0,
                       keywords, subtype: "Infantry");

    static CardDef ActiveUnit(string id, string desc)
    {
        return new CardDef(id, id, "unit", desc, "common", "Test", 1, 1, 6, 0, (IEnumerable<string>)null,
                           subtype: "Infantry",
                           hasActiveAbility: 1,
                           hasAlternativeAbility: 0,
                           activeAbilityLogic: new[]
                           {
                               new CardDef.ActiveAbilityLogicSpec
                               {
                                   Ability = CardDef.ActiveAbilityObject,
                                   Gate = 1,
                                   TargetsAffected = new[] { 30 },
                                   ChosenAbility = new[] { 30 },
                               },
                           },
                           activeAbilityLogicVersioned: true);
    }

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B148W2Warlord0") };
        var d1 = new List<CardDef> { Hero("B148W2Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card, bool exhausted = false)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = exhausted };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static void Ok(bool cond, string what) { if (cond) { _pass++; Debug.Log(P + "✓ " + what); } else { _fail++; Debug.LogError(P + "✗ " + what); } }
    static void Eq(int got, int want, string what) { Ok(got == want, what + "（实得 " + got + "，期望 " + want + "）"); }
    static void EqS(string got, string want, string what) { Ok(string.Equals(got, want, StringComparison.Ordinal), what + "（实得「" + got + "」，期望「" + want + "」）"); }
    static void EqC(int got, int want, string what) { Ok(got == want, what + "（实得 " + got + "，期望 " + want + "）"); }

    static int SigIndex(BattleContext ctx, EvtKind kind, int player, int slot)
    {
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e == null || e.Kind != kind) continue;
            if (player >= 0 && e.Player != player) continue;
            if (slot >= 0 && e.Slot != slot) continue;
            return i;
        }
        return -1;
    }

    // ==================================================================
    //  A 组：`CanStartActiveAbility` 的 `duty` 闸 = `ActiveAbilityUsed`（`B147-DUTYUSED-2ND`）
    // ==================================================================
    static void GroupA_DutyGateReadsActiveAbilityUsed()
    {
        Debug.Log(P + "--- A · `CanStartActiveAbility` 的 duty 闸读 `ActiveAbilityUsed`（原版 `+0x4C`）---");
        var card = ActiveUnit("B148W2_DutyActive", "Duty: Gain +1 Health");
        var ctx = NewCtx();
        int p = ctx.Active;
        var u = Place(ctx, p, 3, card);

        // 前提（硬检查 —— ⛔ 不写 `if (前提) Check(结论)`）
        Ok(u.Card.HasActiveAbility == 1, "A0 前提：夹具 `hasActiveAbility = 1`");
        Ok(u.ActiveAbilityAvailable, "A0b 前提：`ActiveAbilityAvailable` 真（卡在、没被教程藏、没被 jam）");
        Ok(u.HasFx(KeywordTable.Duty), "A0c 前提：`HasFx(duty)` 真（卡面 `Duty: …` 被收进 TriggerOps）");
        Ok(!u.ActiveAbilityUsed && !u.DutyUsed, "A0d 前提：两个标志开局都是假");
        EqC(RuleCore.CanStartActiveAbility(ctx, p, 3), RuleCodes.OK, "A1 两个都没用过 ⇒ 放行");

        u.ActiveAbilityUsed = true; u.DutyUsed = false;
        EqC(RuleCore.CanStartActiveAbility(ctx, p, 3), RuleCodes.ErrDutyUsed,
            "A2 ★ 本局用过**别的**主动能力族（`ActiveAbilityUsed` 真 / `DutyUsed` 假）⇒ **也必须拒**"
          + "（改前读 `DutyUsed` ⇒ 这里是 OK ⇒ 红；判据 `BattleManager__CanUseActiveAbility.c:92-97`）");

        u.ActiveAbilityUsed = false; u.DutyUsed = true;
        EqC(RuleCore.CanStartActiveAbility(ctx, p, 3), RuleCodes.OK,
            "A3 ★ 反方向：只有 `DutyUsed` 真 ⇒ **放行** —— 原版读的是 `+0x4C` 那一位，"
          + "⛔ 别写成 `DutyUsed || ActiveAbilityUsed`（那样这行红）");
    }

    // ==================================================================
    //  B 组：`SAU55` 那句的解析与归口（`B123-CA50SIX`）
    // ==================================================================
    static void GroupB_Sau55CostSentence()
    {
        Debug.Log(P + "--- B · `For the rest of this battle, your Warlords' Talent costs 1 less` 的解析与归口 ---");
        const string sau55 = "For the rest of this battle, your Warlords' Talent costs 1 less";
        string payload, dur;
        int ops;
        bool parsed = ParseLowerCost(sau55, out payload, out dur, out ops);
        Ok(parsed, "B0 前提：这句真的解出了一条 `lowercost` op（op 数 = " + ops + "）");
        EqS(payload, "warlords' talent",
            "B1 ★ 载荷 = `warlords' talent`（**时长从句已被剥掉**）"
          + "｜🧨 删掉 `SubjectCostOp` 里 `CostDurationPrefixes` 那一段 ⇒ 载荷 = "
          + "`for the rest of this battle, your warlords' talent` ⇒ 红（下游 `FindByName` 认不出 ⇒ 静默不生效）");
        EqS(dur, "", "B2 时长档 = `\"\"`（= 永久 / 本场，卡面写的就是「For the rest of this battle」）");

        // 反例：带**后缀**写法的那一族不许受影响（`TAU51` / `TL47` 那种）
        parsed = ParseLowerCost("Your Drones cost 1 less for the rest of this battle.", out payload, out dur, out ops);
        Ok(parsed, "B3 前提：`Your Drones cost 1 less for the rest of this battle.` 解得出 lowercost");
        EqS(payload, "drones", "B3a 反例① 后缀写法：载荷仍是 `drones`（前缀那一段只认「从句在最前面」）");
        EqS(dur, "", "B3b 反例① 时长档 = `\"\"`（`for the rest of this battle` 后缀写法）");
        parsed = ParseLowerCost("Your troops cost 1 less this turn", out payload, out dur, out ops);
        Ok(parsed, "B4 前提：`Your troops cost 1 less this turn` 解得出 lowercost");
        EqS(payload, "troops", "B4a 反例② 后缀写法：载荷 `troops`");
        EqS(dur, "turn", "B4b 反例② 时长档 = `turn`");

        // 数据面：池里 `SAU55` 的 `abilityBuffs` 真有那条 `handBuff ∧ ta=235` 行（⇒ 结算归数据路，正文只认不登记）
        var rows = CardDatabase.AbilityBuffsOf("SAU55");
        Ok(rows != null && rows.Count > 0, "B5 前提：池卡 `SAU55` 的 `abilityBuffs` 读到了（宿主得先装池）");
        int n235 = 0;
        if (rows != null)
            for (int i = 0; i < rows.Count; i++)
            {
                var b = rows[i];
                if (b != null && b.IsHandBuff && b.Criteria != null && b.Criteria.TargetsAffected == 235) n235++;
            }
        Eq(n235, 1,
            "B6 ★ `SAU55` 的 `abilityBuffs` 里 `at=handBuff ∧ ta=235 warlordsTalent` 恰好 **1** 行"
          + "｜🧨 卡表把 `ta` 改掉 / 删掉那一行 ⇒ 正文那支会退回自己登记（或反过来登记两次）⇒ 红");
    }

    /// <summary>从一句话里取出唯一那条 `lowercost` 的载荷 / 时长 / 整句 op 数。
    /// ⚠️ `EffectText` 的解析入口：与 `B140_WA_Assert.ParseAndFindLowerCost` 同一个口。</summary>
    static bool ParseLowerCost(string sentence, out string payload, out string dur, out int ops)
    {
        payload = null; dur = null; ops = 0;
        List<string> unparsed, partial;
        var list = EffectText.Parse(sentence, out unparsed, out partial);
        if (list == null) return false;
        ops = list.Count;
        for (int i = 0; i < list.Count; i++)
        {
            var op = list[i];
            if (op == null || op.Verb != "lowercost") continue;
            payload = op.Payload;
            dur = op.Duration;
            return true;
        }
        return false;
    }

    // ==================================================================
    //  C 组：践踏闸（`B146-S1-G5-STOMP-FIX`）
    // ==================================================================
    static void GroupC_StompAfterShurikenKill()
    {
        Debug.Log(P + "--- C · 践踏 = `stomp ∧ 目标裸血 < 0 ∧ 相邻 > 0`、量 = `-health`（星镖杀也践踏）---");
        // 布局：我方 3 号 = 攻方（`stomp` + `shuriken 5`）；敌方 2/3/4（连续）= 左邻 / 目标(2 血) / 右邻
        BattleContext Mk(out UnitState atk, out UnitState tgt, out UnitState l, out UnitState r,
                         out int me, out int foe, bool withStomp)
        {
            var ctx = NewCtx();
            me = ctx.Active; foe = 1 - ctx.Active;
            var akw = withStomp ? new[] { "stomp" } : null;
            atk = Place(ctx, me, 3, Unit("B148W2_Atk" + withStomp, 1, 1, 9, "", akw));
            atk.AddKeyword("shuriken", 5);
            l = Place(ctx, foe, 2, Unit("B148W2_L", 1, 1, 10));
            tgt = Place(ctx, foe, 3, Unit("B148W2_T", 1, 1, 2));
            r = Place(ctx, foe, 4, Unit("B148W2_R", 1, 1, 10));
            ctx.ClearSignals();
            return ctx;
        }

        UnitState atk1, tgt1, l1, r1; int me1, foe1;
        var c1 = Mk(out atk1, out tgt1, out l1, out r1, out me1, out foe1, withStomp: true);
        // 前提（硬检查）
        Ok(atk1.Has("stomp"), "C0 前提：攻方带 `stomp`");
        Eq(atk1.KwValue("shuriken"), 5, "C0b 前提：攻方 `shuriken` 值 = 5");
        Eq(tgt1.Health, 2, "C0c 前提：目标只有 2 血（星镖 5 点必然打死）");
        EqC(RuleCore.DeclareAttack(c1, me1, 3, foe1, 3), RuleCodes.OK, "C1 前提：这一记打得出去");
        Eq(tgt1.Health, -3,
            "C1b 前提：目标被星镖打死 ⇒ 裸血 = **-3**（原版 `+0x68 < 0` 那一档）");
        int lost = (10 - l1.Health) + (10 - r1.Health);
        Eq(lost, 3,
            "C2 ★★ **星镖杀之后照样践踏**：相邻两格合计掉 **3** = `abs(-3)`（原版 `ShouldTriggerStompDamage` 只读血量符号）"
          + "｜🧨 闸改回 `dealt > hpBefore`（星镖杀时 `dealt = 0`）⇒ 合计 0 ⇒ 红");

        // 反例（灭自证）：同一夹具、攻方**不带 `stomp`** ⇒ 相邻一点不掉（证明 C2 那 3 点真是践踏发的）
        UnitState atk2, tgt2, l2, r2; int me2, foe2;
        var c2 = Mk(out atk2, out tgt2, out l2, out r2, out me2, out foe2, withStomp: false);
        Ok(!atk2.Has("stomp"), "C3 前提（反例）：攻方不带 `stomp`");
        EqC(RuleCore.DeclareAttack(c2, me2, 3, foe2, 3), RuleCodes.OK, "C3b 前提（反例）：这一记照样打得出去");
        Eq(tgt2.Health, -3, "C3c 前提（反例）：目标同样被星镖打死（裸血 -3）");
        Eq((10 - l2.Health) + (10 - r2.Health), 0,
            "C4 ★ 反例：不带 `stomp` ⇒ 相邻合计掉 **0**（灭自证：C2 那 3 点确实来自践踏）");
    }

    // ==================================================================
    //  D 组：三处「有这个词」→「值 > 0」（`B146-S1-G4-KWVALUE`）
    // ==================================================================
    static void GroupD_KeywordValueGates()
    {
        Debug.Log(P + "--- D · `shuriken` / `sentry` / `blast` 的闸 = 值 > 0（今天零可观测，纯稳健性）---");

        // D0 · 前提：`AddKeyword(x, 0)` 之后「有这个词」为真、而 `KwValue` 为 0 —— 这正是旧闸会进段的那一档
        var ctx0 = NewCtx();
        var u0 = Place(ctx0, ctx0.Active, 3, Unit("B148W2_ZeroKw", 1, 1, 5));
        u0.AddKeyword("shuriken", 0);
        Ok(u0.Has("shuriken"), "D0 前提：`Has(\"shuriken\")` 为真（旧闸 `Has(...)` 会进段）");
        Eq(u0.KwValue("shuriken"), 0, "D0b 前提：`KwValue(\"shuriken\")` = 0（新闸 `> 0` 不进段）");

        // D0c · 行为等价：带 0 值 `shuriken` 的一记攻击 = 不带这个词的一记攻击（都是普通攻击、不多出任何东西）
        int DamageOf(bool withZeroShuriken)
        {
            var ctx = NewCtx();
            int me = ctx.Active, foe = 1 - ctx.Active;
            var atk = Place(ctx, me, 3, Unit("B148W2_DA" + withZeroShuriken, 1, 3, 9));
            if (withZeroShuriken) atk.AddKeyword("shuriken", 0);
            var tgt = Place(ctx, foe, 3, Unit("B148W2_DT" + withZeroShuriken, 1, 0, 20));
            ctx.ClearSignals();
            int rc = RuleCore.DeclareAttack(ctx, me, 3, foe, 3);
            if (rc != RuleCodes.OK) return -999;
            return 20 - tgt.Health;
        }
        int dWith = DamageOf(true), dWithout = DamageOf(false);
        Ok(dWith == dWithout && dWith > 0,
            "D0c 带 0 值 `shuriken` 的一记 = 不带它的一记（" + dWith + " vs " + dWithout + "）"
          + "—— 0 值不该改变任何伤害");

        // D1 · 结构（源码层钉住；今天三项都**没有**可观测的卡池夹具，见报告 §D）
        string src = ReadSource("RuleEngine/Core/RuleCore.cs");
        Ok(src != null, "D1 前提：读得到 `Assets/RuleEngine/Core/RuleCore.cs`");
        if (src == null) return;
        Ok(src.Contains("attacker.KwValue(\"shuriken\") > 0"), "D1a ★ 星镖闸 = `KwValue(\"shuriken\") > 0`");
        Ok(src.Contains("target.KwValue(\"sentry\") > 0"), "D1b ★ 哨戒闸 = `KwValue(\"sentry\") > 0`");
        Ok(src.Contains("attacker.KwValue(\"blast\") > 0"), "D1c ★ 爆裂闸 = `KwValue(\"blast\") > 0`");
        Ok(!src.Contains("attacker.Has(\"shuriken\")"), "D1d ⛔ 源码里不再有 `attacker.Has(\"shuriken\")`（旧闸）");
        Ok(!src.Contains("target.Has(\"sentry\")"), "D1e ⛔ 源码里不再有 `target.Has(\"sentry\")`（旧闸）");
        Ok(!src.Contains("attacker.Has(\"blast\")"), "D1f ⛔ 源码里不再有 `attacker.Has(\"blast\")`（旧闸）");
    }

    static string ReadSource(string rel)
    {
        try
        {
            string path = Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception e) { Debug.LogError(P + "读源码失败：" + e.Message); return null; }
    }

    // ==================================================================
    //  E 组：现身续攻（`B146-STEALTH-CANCEL`）
    // ==================================================================
    static void GroupE_StealthCancelContinuation()
    {
        Debug.Log(P + "--- E · 带 `stealth` 出手 ⇒ 摘潜行 + 广播 + **取消这一记、压一条 Instant 续攻** ---");
        // 监听器卡面照 `A1564` 那条（同一句、同一条登记路）：失去潜行 ⇒ 打敌方督军 1 点。
        const string obsDesc = "When a friendly unit loses Stealth, deal 1 damage to the enemy Warlord";

        BattleContext Mk(bool withStealth, out UnitState atk, out UnitState tgt, out UnitState obs,
                         out int me, out int foe)
        {
            var ctx = NewCtx();
            me = ctx.Active; foe = 1 - ctx.Active;
            var obsCard = new CardDef("B148W2_Obs" + withStealth, "B148W2_Obs", "unit", obsDesc, "common", "Test",
                                      1, 1, 6, 0, (IEnumerable<string>)null, subtype: "Infantry");
            obs = Place(ctx, me, 2, obsCard, exhausted: true);
            var kw = withStealth ? new[] { "Stealth" } : null;
            atk = Place(ctx, me, 3, Unit("B148W2_SAtk" + withStealth, 1, 4, 9, "", kw));
            tgt = Place(ctx, foe, 3, Unit("B148W2_STgt" + withStealth, 1, 0, 20));
            ctx.ClearSignals();
            return ctx;
        }

        UnitState atk1, tgt1, obs1; int me1, foe1;
        var c1 = Mk(true, out atk1, out tgt1, out obs1, out me1, out foe1);
        // 前提（硬检查）
        Eq(obs1.Card.WhenTriggers.Count, 1, "E0 前提：监听器卡登记了 1 条 `When …`");
        Ok(atk1.Has(KeywordTable.Stealth), "E0b 前提：攻方出手前带 `stealth`");
        EqC(RuleCore.DeclareAttack(c1, me1, 3, foe1, 3), RuleCodes.OK, "E1 前提：这一记打得出去");
        Ok(!atk1.Has(KeywordTable.Stealth), "E2 出手后 `stealth` 已摘（原版 `:433 RemoveTraitAndEffects(0x46)`）");
        Eq(20 - tgt1.Health, 4,
            "E3 ★ 这一记**照样打出去一次**（目标掉 4 = 攻方攻击力）—— 续攻是「补打」不是「取消伤害」"
          + "｜🧨 把续攻那条 `EnqueueAbility` 删掉（只取消不补打）⇒ 实得 0 ⇒ 红");
        int wlHit = 30 - c1.Players[foe1].Warlord.Health;
        Eq(wlHit, 1,
            "E4 前提：失去潜行的监听器**真的响了**（敌方督军掉 1）"
          + "｜🧨 删掉 `BroadcastKeywordEvent(LosesStealth, …)` ⇒ 实得 0 ⇒ 红");
        int iLose = SigIndex(c1, EvtKind.Hit, foe1, BoardSpec.WarlordSlot);
        int iAtk = SigIndex(c1, EvtKind.Hit, foe1, 3);
        Ok(iLose >= 0 && iAtk >= 0 && iLose < iAtk,
            "E5 ★★【次序】失去潜行那一跳的下标（" + iLose + "）**必须小于**这一记的下标（" + iAtk + "）"
          + " —— 原版 `:478 AddFollowResolutionAttackContinuationAction` 把这一记压回队列，"
          + "`ASH79` 那类「失去 Stealth ⇒ give it Shuriken 1」必须先落地"
          + "｜🧨 改回「就地继续往下打」（= 改前的形状）⇒ 监听器要等动作作用域退出才跑 ⇒ 两个下标互换 ⇒ 红");

        // 反例（灭自证）：同一夹具、攻方**不带 `stealth`** ⇒ 监听器一条都不响，这一记照常打
        UnitState atk2, tgt2, obs2; int me2, foe2;
        var c2 = Mk(false, out atk2, out tgt2, out obs2, out me2, out foe2);
        EqC(RuleCore.DeclareAttack(c2, me2, 3, foe2, 3), RuleCodes.OK, "E6 前提（反例）：这一记打得出去");
        Eq(30 - c2.Players[foe2].Warlord.Health, 0,
            "E6a ★ 反例：不带 `stealth` ⇒ 敌方督军 **0** 伤（灭自证：E4/E5 那条 `Hit` 的成因就是「失去潜行」）");
        Eq(20 - tgt2.Health, 4, "E6b 反例：这一记照常打出去（4 点）");
    }

    // ==================================================================
    //  F 组：四支特殊打法的**分类次序**（`B146-S1-G6-SPECIAL-SIGNAL`）
    // ==================================================================
    static void GroupF_SpecialAttackClassification()
    {
        Debug.Log(P + "--- F · `CounterAttack.Classify`：else-if 次序 = sneak → terror → sniper → longRange ---");
        BattleContext Mk(out UnitState atk, out UnitState tgt, string[] kw, bool targetDies)
        {
            var ctx = NewCtx();
            int me = ctx.Active, foe = 1 - ctx.Active;
            atk = Place(ctx, me, 3, Unit("B148W2_F_Atk", 1, 5, 9));
            tgt = Place(ctx, foe, 3, Unit("B148W2_F_Tgt", 1, 1, targetDies ? 2 : 20));
            if (kw != null) foreach (var k in kw) atk.AddKeyword(k, 1);
            return ctx;
        }

        // F0 前提：null 安全
        Eq((int)CounterAttack.Classify(null, 0, null, 1, null, false, false), (int)CounterAttack.CounterExempt.None,
            "F0 前提：攻方/守方任一为 null ⇒ `None`（不抛）");

        UnitState a1, t1;
        var c1 = Mk(out a1, out t1, new[] { KeywordTable.SneakAttack, KeywordTable.Sniper, KeywordTable.LongRange }, true);
        Eq((int)CounterAttack.Classify(c1, c1.Active, a1, 1 - c1.Active, t1, true, true), (int)CounterAttack.CounterExempt.SneakAttack,
            "F1 ★ `sneakAttack` 最优先（同带 sniper/longRange 也归它）—— 判据 `_ResolveAttack…:871` 是链首");

        UnitState a2, t2;
        var c2 = Mk(out a2, out t2, new[] { KeywordTable.Terror }, false);
        Eq((int)CounterAttack.Classify(c2, c2.Active, a2, 1 - c2.Active, t2, false, false), (int)CounterAttack.CounterExempt.Terror,
            "F2 ★ terror：守方是部队且当前战斗攻击力 < 攻方（我 5 / 它 1）⇒ Terror（近战也成立）");

        // ★★ 次序判别式：**同时带 sniper 与 longRange** ⇒ 必须是 Sniper（原版 `:869-880` 在前）。
        //    若把 LongRange 判在 Sniper 之前 ⇒ 这里实得 LongRange ⇒ 红。
        UnitState a3, t3;
        var c3 = Mk(out a3, out t3, new[] { KeywordTable.Sniper, KeywordTable.LongRange }, true);
        Eq((int)CounterAttack.Classify(c3, c3.Active, a3, 1 - c3.Active, t3, true, true), (int)CounterAttack.CounterExempt.Sniper,
            "F3 ★★ **Sniper 在 LongRange 之前**（原版次序；`CounterAttack.NoCounter` 的书写次序相反，但两档都为真 ⇒ 反击结论不变）"
          + "｜🧨 把两支对调 ⇒ 实得 LongRange ⇒ 红");

        UnitState a4, t4;
        var c4 = Mk(out a4, out t4, new[] { KeywordTable.LongRange }, false);
        Eq((int)CounterAttack.Classify(c4, c4.Active, a4, 1 - c4.Active, t4, true, false), (int)CounterAttack.CounterExempt.LongRange,
            "F4 只带 longRange + 远程 ⇒ LongRange（不要求击杀）");
        Eq((int)CounterAttack.Classify(c4, c4.Active, a4, 1 - c4.Active, t4, false, false), (int)CounterAttack.CounterExempt.None,
            "F5 ★ 反例：**近战**打法 ⇒ 后两支一句话都不说 ⇒ None（判据 `:874 if (!ranged) return` 那一段之前的两支之外都不成立）");

        // F6 结构：`NoCounter` 只是 `Classify(...) != None` 的 OR（四档逐个对拍）
        UnitState a5, t5;
        var c5 = Mk(out a5, out t5, new[] { KeywordTable.Sniper, KeywordTable.LongRange }, true);
        Ok(CounterAttack.NoCounter(c5, c5.Active, a5, 1 - c5.Active, t5, true, true)
           == (CounterAttack.Classify(c5, c5.Active, a5, 1 - c5.Active, t5, true, true) != CounterAttack.CounterExempt.None),
            "F6 ★ `NoCounter` 与 `Classify != None` 逐档一致（判据只此一处 —— 两处写同一条规则迟早不一致）");
    }

    // ==================================================================
    //  G 组：`PopupAmount` / `DamageKind` 透传（`B146-S1-G8-POPUP-AMOUNT` + 践踏粒子）
    // ==================================================================
    static void GroupG_PopupAmountAndDamageKind()
    {
        Debug.Log(P + "--- G · `Hit.PopupAmount`（空投舱/堡垒整份吃下仍弹原值）· `Hit.DamageKind`（践踏）---");

        BattleEvent LastHit(BattleContext ctx)
        {
            for (int i = ctx.Signals.Count - 1; i >= 0; i--)
                if (ctx.Signals[i] != null && ctx.Signals[i].Kind == EvtKind.Hit) return ctx.Signals[i];
            return null;
        }

        // G0 · 空投舱整份吃下 ⇒ `Amount = 0` 但飘字弹**原值**（原版 `_ReceiveDamage…:313`）
        {
            var ctx = NewCtx();
            var u = Place(ctx, ctx.Active, 3, Unit("B148W2_G_Pod", 1, 1, 9));
            u.AddKeyword(KeywordTable.DropPod, 2);
            ctx.ClearSignals();
            int dealt = RuleCore.ApplyDamage(ctx, u, 5, "B148W2-G0");
            var hit = LastHit(ctx);
            Ok(hit != null, "G0 前提：挨打真的发了一条 `Hit`");
            if (hit != null)
            {
                Eq(dealt, 0, "G0b 前提：池子吃光 ⇒ 实际掉血 0");
                Eq(hit.Amount, 0, "G0c 前提：`Amount` 必须是 **0**（`NotePendingHp` 靠它反推扣血前血量，⛔ 不许改）");
                Eq(hit.PopupAmount, 5, "G0d ★ 飘字仍弹 **原值 5**（`PopupAmount`）"
                    + "｜🧨 不传 `popupAmount` ⇒ 实得 -1、表现层回落 `Amount` = 0 ⇒ 不弹数字 ⇒ 红");
            }
        }

        // G1 · 堡垒整份吃下 ⇒ 同上
        {
            var ctx = NewCtx();
            var u = Place(ctx, ctx.Active, 3, Unit("B148W2_G_Bastion", 1, 1, 9));
            u.AddKeyword(KeywordTable.Bastion, 3);
            ctx.ClearSignals();
            int dealt = RuleCore.ApplyDamage(ctx, u, 2, "B148W2-G1");
            var hit = LastHit(ctx);
            Ok(hit != null, "G1 前提：挨打真的发了一条 `Hit`");
            if (hit != null)
            {
                Eq(dealt, 0, "G1b 前提：堡垒整份吃下 ⇒ 实际掉血 0");
                Eq(hit.Amount, 0, "G1c 前提：`Amount` = 0");
                Eq(hit.PopupAmount, 2, "G1d ★ 堡垒整份吸收仍弹 **原值 2**");
            }
        }

        // G2 · 普通伤害 ⇒ `PopupAmount` 保持哨兵 -1（消费端回落 `Amount`，老行为逐字不变）
        {
            var ctx = NewCtx();
            var u = Place(ctx, ctx.Active, 3, Unit("B148W2_G_Plain", 1, 1, 9));
            ctx.ClearSignals();
            int dealt = RuleCore.ApplyDamage(ctx, u, 3, "B148W2-G2");
            var hit = LastHit(ctx);
            Ok(hit != null, "G2 前提：挨打真的发了一条 `Hit`");
            if (hit != null)
            {
                Eq(dealt, 3, "G2b 前提：掉 3 血");
                Eq(hit.Amount, 3, "G2c 前提：`Amount` = 3");
                Eq(hit.PopupAmount, -1, "G2d ★ 普通伤害 **不写** `PopupAmount`（哨兵 -1 ⇒ 回落 `Amount`）");
            }
        }

        // G3 · 践踏那一下的 `Hit` 带 `DamageKind.Stomp`（表现层靠它把粒子播在**被打者**身上，原版 `:1093`）
        {
            var ctx = NewCtx();
            int me = ctx.Active, foe = 1 - ctx.Active;
            var atk = Place(ctx, me, 3, Unit("B148W2_G_StompAtk", 1, 1, 9));
            atk.AddKeyword("stomp", 1);
            atk.AddKeyword("shuriken", 5);
            Place(ctx, foe, 2, Unit("B148W2_G_L", 1, 1, 10));
            var tgt = Place(ctx, foe, 3, Unit("B148W2_G_T", 1, 1, 2));
            Place(ctx, foe, 4, Unit("B148W2_G_R", 1, 1, 10));
            ctx.ClearSignals();
            EqC(RuleCore.DeclareAttack(ctx, me, 3, foe, 3), RuleCodes.OK, "G3 前提：这一记打得出去");
            BattleEvent stompHit = null;
            for (int i = 0; i < ctx.Signals.Count; i++)
            {
                var e = ctx.Signals[i];
                if (e != null && e.Kind == EvtKind.Hit && e.DamageKind == DamageKind.Stomp) { stompHit = e; break; }
            }
            Ok(stompHit != null, "G3b ★★ 践踏那一下的 `Hit` 带 **`DamageKind.Stomp`**"
                + "｜🧨 `EmitHit` 不传 `damageKind` ⇒ 表现层无从分辨「这一下是践踏」⇒ 红");
            if (stompHit != null)
            {
                Ok(stompHit.Player == foe, "G3c 这一条落在**被打者**那一边（原版 `:1093` 粒子挂被打者）");
                Eq(stompHit.Amount, 3, "G3d 践踏量 = 3（= `abs(目标裸血)`，与 C2 同一条判据）");
            }
        }
    }
}
