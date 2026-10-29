// B97W4_Assert.cs — 执行代理 B97_W4（第九十七会话 · 波 1）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `:241` / `:242`（`BattleManager.CancelEffectsFromDisappearingUnit(FromQueue)`）
//   账 `:853`（`CardScript.RemoveEffect` 的「按一条记录摘」）
//   账 `:770`（`CardScript.HasCancelAttackEffect` 的「逐条 ability 判、任一真即真」）
//
// 判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_战斗剩余_按文件施工单.md` §1·3 / §1·4 / §1·7
//   ＋ 本轮报告 `资料/完全复刻/报告/B97_W4.md`。
//
// ============================ 怎么用（宿主）============================
// `B97W4Assert.Run();`
//   + 宿主里两行（照 `B94B18Assert` 的先例）：
//     `_sink.Pass += B97W4Assert.Pass; _sink.Fail += B97W4Assert.Fail;`
//   · 推荐宿主 = `Editor/RuleEngineTest.cs`（`RuleEngineTest.Run`）—— 本文件**只碰纯引擎**
//     （`RuleCore` / `UnitState` / `ActionQueue`），不建 GameObject、不需要场景。
//   · 纯内存夹具：每次自建 `BattleContext` / `UnitState`，**不建持久资产**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1/A2：把 `CancelEffectsFromDisappearingUnitFromQueue` 的 `!ReferenceEquals(a.Actor, jammedUnit)` 那行删掉
//            ⇒ `keep-other` 也被撤 ⇒ A2 红。
//   · A3：把那句 `if (a.ActionType == 1) continue;` 删掉 ⇒ `keep-attack` 被撤 ⇒ A3 红（只数条数抓不到这一格）。
//   · A4/A4′：把 `if (shouldIgnoreDestroy && a.ActionType == 0x2d) return n;` 删掉 ⇒ 那条 45 被撤 ⇒ A4 红。
//   · A5：把整个 `actionType` 匹配分支改成「0x2d 一律保留」⇒ A5 红。
//   · B3：把 `UnitState.RemoveEffectRecord` 的 `case 3/90` 退回「无条件 `RemoveAll`」（不给 `TraitRecord` 那条快路）
//        ⇒ B3 红（关键词被整条抹掉）。
//   · B4：把 `RemoveTraitEffect` 里的 `RemoveAll` 删掉 ⇒ B4 红（关键词永远摘不掉）。
//   · B5：把 `AnyEffectGrantsTrait` 的残骸闸（`IsRemnant && !IsLeavingRemnant`）删掉 ⇒ B5 红。
//   · C1/C2/C3：把 `AbilityRowsForTrigger` 退回「只回唯一一条」（或让 `UniqueAbilityIndexForTrigger` 对
//        同 trigger 两条任选一条）⇒ C1/C2 红。
//   · C4/C5：把 `AbilityCriteriaAllowsAt` 的 `null` 档改成 `false` ⇒ C4/C5 红（那也是把一个「判不了」
//        静默变成「不满足」的实现缺陷）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 B97_W4：战斗线三件小件（离场清扫 / 按记录摘 / 逐条 ability 闸）的断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W4Assert
{
    static readonly CheckSink _sink = new CheckSink("[W4] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_W4 战斗线小件（离场清扫 · 按记录摘 · 逐条 ability 闸）");

        SectionA_离场清扫();
        SectionB_按记录摘();
        SectionC_逐条Ability闸();

        Debug.Log(_sink.P + "B97W4 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    static CardDef Troop(string name, string faction = "SpaceWolves")
    {
        return new CardDef(name, name, "troop", "", null, faction, 1, 1, 3, 0, null, subtype: "troop");
    }

    static List<CardDef> DeckOf(string faction)
    {
        return new List<CardDef>
        {
            new CardDef("w4hero_" + faction, "W4Hero", "hero", "", null, faction, 0, 0, 30, 0, null, subtype: "Warlord"),
        };
    }

    static BattleContext NewCtx()
    {
        return RuleCore.NewBattle(DeckOf("SpaceWolves"), DeckOf("Goff"), seed: 0, shuffle: false);
    }

    // ============================================================ A 离场清扫（账 241 / 242）
    static void SectionA_离场清扫()
    {
        MenuCheck.Section(_sink, "A `CancelEffectsFromDisappearingUnit`（账 241/242）：只撤同一 actor · attack 不撤 · ⓒ 豁免");

        var ctx = NewCtx();
        var leaver = new UnitState(Troop("W4Leaver"), false);
        var other = new UnitState(Troop("W4Other"), false);

        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "keep-attack", () => { }, actor: leaver, actionType: 1);
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "drop-1", () => { }, actor: leaver, actionType: 0);
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "keep-other", () => { }, actor: other, actionType: 0);

        var q = ctx.ActionQueue;
        int before = q.CountOf(ActionPriority.Instant);
        Tr(before >= 3, "A0 前提：Instant 道里至少有三条（我入队的那三条；`NewBattle` 可能另有自动动作 ⇒ 只判下界）");

        int n = RuleCore.CancelEffectsFromDisappearingUnit(ctx, leaver, null, false);
        C(n, 1, "A1 只撤「`Actor == 离场那张` ∧ `actionType != 1 attack`」的那一条（= 1 条）");
        C(q.CountOf(ActionPriority.Instant), before - 1, "A2 只少了那一条（attack 不撤 · 别人的 actor 不撤）");

        bool attackKept = false;
        for (int i = 0; i < q.CountOf(ActionPriority.Instant); i++)
            if (q.At(ActionPriority.Instant, i).ActionType == 1) attackKept = true;
        Tr(attackKept, "A3 `attack(1)` 那条**确实还在**（⛔ 只数条数不够：`drop-1` 留下、`keep-attack` 被撤也是 2 条）");

        // A4 / A4′：ⓒ `shouldIgnoreDestroy ∧ 0x2d` ⇒ 结束整条泳道
        var ctx2 = NewCtx();
        var destroyer = new UnitState(Troop("W4Destroy"), false);
        RuleCore.EnqueueAbility(ctx2, ActionPriority.Instant, "destroyUnit", () => { }, actor: destroyer, actionType: 45);
        int before2 = ctx2.ActionQueue.CountOf(ActionPriority.Instant);
        int n2 = RuleCore.CancelEffectsFromDisappearingUnit(ctx2, destroyer, null, true);
        C(n2, 0, "A4 ⓒ `shouldIgnoreDestroy=true ∧ actionType=0x2d(45)` ⇒ **结束整条泳道**、一条都不撤（判据 `.c:88-91`）");
        C(ctx2.ActionQueue.CountOf(ActionPriority.Instant), before2, "A4′ 泳道条数**一条没少**（那条 `0x2d` 还在）");

        // A5：ⓒ 的反面 —— `shouldIgnoreDestroy=false` 时 0x2d 照样撤
        var ctx3 = NewCtx();
        var destroyer2 = new UnitState(Troop("W4Destroy2"), false);
        RuleCore.EnqueueAbility(ctx3, ActionPriority.Instant, "destroyUnit2", () => { }, actor: destroyer2, actionType: 45);
        int before3 = ctx3.ActionQueue.CountOf(ActionPriority.Instant);
        int n3 = RuleCore.CancelEffectsFromDisappearingUnit(ctx3, destroyer2, null, false);
        C(n3, 1, "A5 ⓒ 的反面：`shouldIgnoreDestroy=false` 时 `0x2d` 照样撤（证明 A4 不是「0x2d 一律保留」）");
        C(ctx3.ActionQueue.CountOf(ActionPriority.Instant), before3 - 1, "A5′ 泳道少了一条（同上，反面判别式）");
    }

    // ============================================================ B 按记录摘（账 853）
    static void SectionB_按记录摘()
    {
        MenuCheck.Section(_sink, "B `AnyEffectGrantsTrait` + `RemoveEffectRecord` case 3/90：**还有别的记录在给 ⇒ 关键词留着**");

        var u = new UnitState(Troop("W4Trait"), false);
        Tr(!u.Has("fast"), "B0 前提：这个新单位身上**没有** `fast`（不成立 ⇒ B1..B4 无意义）");

        var r1 = u.ApplyTraitEffect("fast", 1);
        Tr(r1 != null && u.Has("fast") && u.AnyEffectGrantsTrait("fast"),
           "B1 挂第一条记录 ⇒ 关键词在、`AnyEffectGrantsTrait` 真");

        var r2 = u.ApplyTraitEffect("fast", 1, extrinsic: true);   // extrinsic ⇒ 不与既有记录合并 ⇒ 真正第二条
        C(u.TraitEffects.Count, 2, "B2 前提：`_traitEffects` 里是**两条**（extrinsic 不合并）");

        u.RemoveEffectRecord(new UnitState.EffectRecord
        {
            BuffType = UnitState.BuffTypeAddTrait,
            Trait = "fast",
            TraitRecord = r2,
        });
        Tr(u.Has("fast"),
           "B3 摘掉**其中一条**记录后关键词**还在**（原版 `HasCurrentTraitActiveEffect` 第二道闸，判据 `RemoveEffect.c:63-72`）");
        C(u.TraitEffects.Count, 1, "B3′ 记录账只剩一条");

        var last = u.TraitEffects[0];
        u.RemoveEffectRecord(new UnitState.EffectRecord
        {
            BuffType = UnitState.BuffTypeAddTrait,
            Trait = "fast",
            TraitRecord = last,
        });
        Tr(!u.Has("fast"), "B4 最后一条也摘掉 ⇒ 关键词才真掉（说明 B3 不是「永远不摘」）");
        C(u.TraitEffects.Count, 0, "B4′ 记录账清空");

        // B5：残骸闸（原版那个「或」不是「且」）
        var ur = new UnitState(Troop("W4Rem"), false) { IsRemnant = true };
        ur.ApplyTraitEffect("fast", 1);
        Tr(!ur.AnyEffectGrantsTrait("fast"),
           "B5 残骸档（`IsRemnant ∧ !IsLeavingRemnant`）⇒ **整段跳过、恒 false**（判据 `HasCurrentTraitActiveEffect.c`）");
    }

    // ============================================================ C 逐条 ability 闸（账 770）
    static void SectionC_逐条Ability闸()
    {
        MenuCheck.Section(_sink, "C 账 770：同 trigger 两条 ability ⇒ 旧的「唯一定位」口径判不了（这就是 ② 要修的那一格）");

        var card = new CardDef("w4enchant", "W4 Enchant", "enchantment", "", null, "SpaceWolves", 0, 0, 0, 0, null,
                               subtype: "enchantment",
                               abilityTriggers: new[] { 280 }, abilityTriggersVersioned: true,
                               abilityTargets: new[]
                               {
                                   new CardDef.AbilityTargetSpec { Trigger = 280, TargetsAffected = new[] { 0 } },
                                   new CardDef.AbilityTargetSpec { Trigger = 280, TargetsAffected = new[] { 0 } },
                               });

        var rows = card.AbilityRowsForTrigger("AboutToAttack");
        C(rows == null ? -1 : rows.Count, 2, "C1 `AbilityRowsForTrigger(\"AboutToAttack\")` 回**两条**（列表形状，= 原版 `GetAbilitiesForTriggerRawcard`）");

        string why;
        int ab = card.UniqueAbilityIndexForTrigger("AboutToAttack", out why);
        C(ab, -1, "C2 🔴 旧的「唯一定位」口径对这张卡回 `-1`（同 trigger 两条 ⇒ 不猜第几条）—— 这正是 `FindCancelAttack` 改前的行为");
        Tr(!string.IsNullOrEmpty(why), "C3 而且它**说清**了是「不唯一」那一档（不是静默 -1）");

        var ctx = NewCtx();
        var carrier = new UnitState(card, false);
        var acting = new UnitState(Troop("W4Attacker"), false);
        var target = new UnitState(Troop("W4Target", "Goff"), false);

        bool? bad = RuleCore.AbilityCriteriaAllowsAt(ctx, 0, carrier, card, -1, acting, target, out why);
        Tr(bad == null, "C4 `AbilityCriteriaAllowsAt` 的 `abilityIndex = -1` ⇒ **判不了（null）**，⛔ 不是 false");

        bool? none = RuleCore.AbilityCriteriaAllowsAt(ctx, 0, carrier, null, 0, acting, target, out why);
        Tr(none == null, "C5 没有卡 ⇒ null（三态契约：判不了 ≠ 不满足）");

        // C6：这张卡的 criteria 列都没落 ⇒ 三道闸「判不了」而不是「不满足」（= 判不了 ⇒ 放行）
        bool? gap = RuleCore.AbilityCriteriaAllowsAt(ctx, 0, carrier, card, 0, acting, target, out why);
        Tr(gap == null && !string.IsNullOrEmpty(why),
           "C6 criteria 列没落的卡 ⇒ **判不了（null）+ 出声**（⛔ 不许把它当 false 把触发掐死）");

        // C7：没有附魔的载体 ⇒ `FindCancelAttack` 回 null（前提断言：不许因为 null 而静默跳过后续判断）
        Tr(Enchantments.FindCancelAttack(ctx, carrier, acting, target) == null,
           "C7 载体身上没有附魔 ⇒ `FindCancelAttack` 回 `null`（这一条不是「跳过」，是**判它确实为空**）");
    }
}
