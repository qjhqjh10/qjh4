// B97W22_Assert.cs — 执行代理 B97_W22（第九十七会话 · 波 1 补位）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `:853` 的**前置拆分** —— `UnitState.RemoveTempBuffCore` 拆成
//     `DetachTempBuff`（记录账那半）＋ `ApplyTempBuffRemoval`（逐 case 那半）后，
//     **仍是「减量」语义**、`TraitRec == null` 的旧记录**仍会被撤**。
//   账 `:707 CardScript.CardSetup` 缺的三块 ——
//     ③ 主动能力「装填」机制（`+0x224 activeAbilityLoaded` 的 3 写 3 读收在一处）
//        ＋ `+0x59 triggeredAbility`（一次性闩，`ResetToValuesInHand` 时要复位）
//     ④ drop pod 两格（`+0xcc/+0xe0`）
//     ② FTUE 低血无敌位（`+0x2e4`，值来自远端 ⇒ 🔶 由调用方给）
//
// 判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_战斗剩余_按文件施工单.md` §1·4
//   ＋ 本轮报告 `资料/完全复刻/报告/B97_W22.md`；原版方法体全在 `d:/2/tools/decomp_full/`。
//
// ============================ 怎么用（宿主）============================
// `B97W22Assert.Run();`
//   + 宿主里两行（照 `B94B18Assert` 的先例）：
//     `_sink.Pass += B97W22Assert.Pass; _sink.Fail += B97W22Assert.Fail;`
//   · 推荐宿主 = `Editor/RuleEngineTest.cs`（`RuleEngineTest.Run`）—— 本文件**只碰纯引擎**
//     （`UnitState`），不建 GameObject、不需要场景。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1：把 `UnitState.StepActiveAbilityLoading` 里那两句 `ActiveAbilityLoaded` 都删掉
//        ⇒ A1 红（「只装填」这一次不再留下状态）。
//   · A2：把 `ActiveAbilityLoaded ? { ActiveAbilityLoaded = false; return false; }` 那支删掉
//        （改成永远 `return true`）⇒ A2 红（第二次不再「真发动」）。
//   · A3：把 `ResetToValuesInHand` 那一行追加的 `ActiveAbilityLoaded = false;` 删掉 ⇒ A3 红。
//   · B1/B2：把 `ResetToValuesInHand` 那一行追加的 `TriggerOnceFired = false;` 删掉 ⇒ B2 红
//        （一次性闩永远闩着 —— 这正是账 `:707③` 说的「缺『触发过就清 0』」）。
//   · C1/C2：把 `ApplyTempBuffRemoval` 的关键词那一支换成 `RemoveTraitEffect(rec)`（整条摘）
//        ⇒ C1 红（关键字被整条抹掉、记录被删）。
//   · D1：在 `ApplyTempBuffRemoval` 顶部加 `if (b.TraitRec == null) return;`
//        ⇒ D1 红（旧记录静默不撤 —— 账 853 坑②）。
//   · E1/E2：把 `SetupDropPodStats` 的两句删掉（或把常量改成别的数）⇒ E1 红。
//   · F1/F2：把 `SetupFTUELowLifeInvulnerability` 的三个合取项删任一项 ⇒ F1/F2 红。
//   · G1：把 `RemoveEffectRecord` 里 `rec.TempBuffRecord` 那一支删掉 ⇒ G1 红
//        （按记录摘那条路走不到限时增益的逐 case）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 B97_W22：账 853 前置拆分 ＋ 账 707 CardSetup 三块的断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W22Assert
{
    static readonly CheckSink _sink = new CheckSink("[W22] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_W22 账 853 前置拆分（减量语义）· 账 707 CardSetup 三块（装填/闩/drop pod/FTUE）");

        SectionA_装填机制();
        SectionB_一次性闩复位();
        SectionC_限时增益仍是减量语义();
        SectionD_旧记录没有TraitRec也要撤();
        SectionE_DropPod两格();
        SectionF_FTUE低血无敌位();
        SectionG_按记录摘走同一份逐case();

        Debug.Log(_sink.P + "B97W22 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    static CardDef Troop(string name, string faction = "SpaceWolves")
    {
        return new CardDef(name, name, "troop", "", null, faction, 1, 1, 3, 0, null, subtype: "troop");
    }

    /// <summary>带主动能力 `hasActiveAbility=1` 的卡；`needsLoading` 由调用方给（1 = 要装填）。</summary>
    static CardDef AbilityCard(string name, int needsLoading)
    {
        return new CardDef(name, name, "troop", "", null, "SpaceWolves", 1, 1, 3, 0, null,
                           subtype: "troop",
                           hasActiveAbility: 1, activeNeedsLoading: needsLoading);
    }

    /// <summary>督军卡（`CardType == 10`）＋可选关键词。</summary>
    static CardDef Hero(string name, params string[] keywords)
    {
        var c = new CardDef(name, name, "hero", "", null, "SpaceWolves", 0, 0, 30, 0, keywords,
                            subtype: "Warlord");
        c.AttachCardType(CardDef.CardTypeHero);
        return c;
    }

    // ============================================================ A 装填机制（账 707③）
    static void SectionA_装填机制()
    {
        MenuCheck.Section(_sink, "A 账 :707③ 装填机制：写点 3 / 读点 3（`activeAbilityLoaded` 收在一处）");

        // A0：**不需要装填**的卡 ⇒ 判据恒假、推进不动状态（前提断言）
        var u0 = new UnitState(AbilityCard("W22NoLoad", 0), false);
        Tr(!u0.RequiresAbilityLoading, "A0 前提：`needsLoading == 0` 的卡 ⇒ `RequiresAbilityLoading` 假");
        Tr(!u0.ActiveAbilityLoaded && !u0.StepActiveAbilityLoading(),
           "A0′ 不需要装填 ⇒ `StepActiveAbilityLoading` 回 false（这次直接发动）且状态位不动");

        // A1：**需要装填**的卡 ⇒ 第一次「只装填」（写点 2：`+0x224 = 1`）
        var u1 = new UnitState(AbilityCard("W22Load", 1), false);
        Tr(u1.RequiresAbilityLoading, "A1-前 前提：`needsLoading == 1` ∧ `hasActiveAbility == 1` ⇒ 需要装填");
        Tr(!u1.ActiveAbilityLoaded, "A1-前′ 初值 = 0（`SetBaseCardTraits.c:38` 写点 1）");
        Tr(u1.StepActiveAbilityLoading(), "A1 🔴 第一次打出 ⇒ **只装填**（这一步返回 true）");
        Tr(u1.ActiveAbilityLoaded, "A1′ 装填位真（写点 2 = `ResolveActiveAbilityPlayed.c:128`）");

        // A2：已装填 ⇒ 这一次**真发动**、发动后清 0（写点 3：`+0x224 = 0`）
        Tr(!u1.StepActiveAbilityLoading(), "A2 🔴 第二次打出 ⇒ **真发动**（返回 false）");
        Tr(!u1.ActiveAbilityLoaded, "A2′ 发动后清 0（写点 3 = `ResolveActiveAbilityPlayed.c:122`）");
        Tr(u1.RequiresAbilityLoading, "A2″ 读点：清 0 之后仍然「需要装填」（下一次又要先装一次）");

        // A3：写点 1 的另一条路 —— `ResetToValuesInHand` 的 `SetBaseCardTraits`
        u1.StepActiveAbilityLoading();                                  // 再装填一次
        Tr(u1.ActiveAbilityLoaded, "A3-前 前提：又装填上了");
        u1.ResetToValuesInHand(clearBacklash: true);
        Tr(!u1.ActiveAbilityLoaded, "A3 🔴 `ResetToValuesInHand` ⇒ 装填位复位成 0（`SetBaseCardTraits.c:38`）");

        // A4：读点三态 —— 未知（-1）不算「要装填」
        var unknown = new CardDef("W22Unknown", "W22Unknown", "troop", "", null, "SpaceWolves", 1, 1, 3, 0, null,
                                  subtype: "troop", hasActiveAbility: 1);   // activeNeedsLoading 缺省 = -1
        var u4 = new UnitState(unknown, false);
        C(u4.Card.ActiveAbilityNeedsLoading, -1, "A4-前 前提：这一列的缺省是 **-1（未知）**，⛔ 不是 0");
        Tr(!u4.RequiresAbilityLoading, "A4 🔴 未知**不**当成「要装填」（三态口径，与 `BattleDriver.ShowSkillPanel` 一致）");
    }

    // ============================================================ B 一次性闩（账 707③）
    static void SectionB_一次性闩复位()
    {
        MenuCheck.Section(_sink, "B 账 :707③ `triggeredAbility`（我们的反向位 `TriggerOnceFired`）：复位点");

        var u = new UnitState(Troop("W22Latch"), false);
        Tr(!u.TriggerOnceFired, "B0 初值 = 「还没触发过」（原版初值 `HasTriggerOnceAbility()`，我们是**反向位**）");

        u.TriggerOnceFired = true;                    // 模拟 `EffectResolver.TriggerOncePass` 触发成功那一下
        Tr(u.TriggerOnceFired, "B1-前 前提：闩已闩上");
        u.ResetToValuesInHand(clearBacklash: false);
        Tr(!u.TriggerOnceFired,
           "B2 🔴 `ResetToValuesInHand`（= 原版 `SetBaseCardTraits` 那一跳）⇒ 闩复位（`CardScript__SetBaseCardTraits.c` 末句 `+0x59 = HasTriggerOnceAbility()`）");

        // B3：是**每个实例一位**（⛔ 不是静态位）
        var ua = new UnitState(Troop("W22LatchA"), false);
        var ub = new UnitState(Troop("W22LatchB"), false);
        ua.TriggerOnceFired = true;
        Tr(!ub.TriggerOnceFired, "B3 两位互不影响（每个 `UnitState` 一份，⛔ 不是静态位）");
    }

    // ============================================================ C 限时增益仍是「减量」（账 853 坑①）
    static void SectionC_限时增益仍是减量语义()
    {
        MenuCheck.Section(_sink, "C 账 :853 坑①：**同一 trait 两条来源**时，撤一条只能减它自己那份");

        var u = new UnitState(Troop("W22TwoSrc"), false);
        var r1 = u.ApplyTraitEffect("fast", 1);       // 来源 1
        var r2 = u.ApplyTraitEffect("fast", 1);       // 来源 2 —— 同 trait/同时长 ⇒ 合并进**同一条**记录
        Tr(r1 != null && ReferenceEquals(r1, r2), "C0 前提：两条来源合并成**同一条** `TraitEffectRecord`（`AddEffect.c:544`）");
        C(u.KwValue("fast"), 2, "C0′ 前提：关键词值 = 2（两份）");
        C(u.TraitEffects.Count, 1, "C0″ 前提：记录账里只有一条");

        var b1 = new UnitState.TempBuff { IsKeyword = true, Name = "fast", Value = 1, TraitRec = r1 };
        var b2 = new UnitState.TempBuff { IsKeyword = true, Name = "fast", Value = 1, TraitRec = r1 };
        u.AddTempBuff(b1);
        u.AddTempBuff(b2);

        u.RemoveTempBuff(b1);
        C(u.KwValue("fast"), 1, "C1 🔴 撤掉**其中一条**来源 ⇒ 关键词还剩 1（**减量**，不是整条摘）");
        Tr(u.Has("fast"), "C1′ 关键词**还在**（换成 `RemoveTraitEffect` 整条摘 ⇒ 这里就没了 ⇒ 本条红）");
        C(u.TraitEffects.Count, 1, "C1″ 记录**还在**（整条摘会把这条也删掉 ⇒ 红）");

        u.RemoveTempBuff(b2);
        Tr(!u.Has("fast"), "C2 最后一条也撤 ⇒ 关键词才真掉（说明 C1 不是「永远不撤」）");
        C(u.TraitEffects.Count, 0, "C2′ 记录账清空（`ExpireTraitEffect` 归零即摘）");
    }

    // ============================================================ D 旧记录没有 TraitRec（账 853 坑②）
    static void SectionD_旧记录没有TraitRec也要撤()
    {
        MenuCheck.Section(_sink, "D 账 :853 坑②：`TraitRec == null` 的旧记录 → 关键词**照样**要减掉");

        var u = new UnitState(Troop("W22NullRec"), false);
        Tr(u.AddKeyword("fast", 1), "D0 前提：关键词挂上了");
        var old = new UnitState.TempBuff { IsKeyword = true, Name = "fast", Value = 1, TraitRec = null };   // 旧记录：没有反指
        u.AddTempBuff(old);
        Tr(u.Has("fast"), "D0′ 前提：撤之前关键词在");

        Tr(u.RemoveTempBuff(old), "D1-前 前提：这一条真被撤掉（`RemoveTempBuff` 返回真）");
        Tr(!u.Has("fast"),
           "D1 🔴 `TraitRec == null` 也要撤 —— 关键词减到 0 就掉（在 `ApplyTempBuffRemoval` 顶部加 `if (TraitRec == null) return;` ⇒ 本条红）");
    }

    // ============================================================ E drop pod 两格（账 707④）
    static void SectionE_DropPod两格()
    {
        MenuCheck.Section(_sink, "E 账 :707④ drop pod 两格 `+0xcc/+0xe0`（`GameStaticData` 的 2 / 0）");

        // E0：前提 —— 常量就是原版那两个值
        C(UnitState.DropPodMaxHealthDefault, 2, "E0 前提：`dropPodMaxHealth = 2`（`GameStaticData.cs:2405`）");
        C(UnitState.DropPodAttackDefault, 0, "E0′ 前提：`dropPodAttack = 0`（`GameStaticData.cs:2404`）");

        // E1：督军 ∧ 带 dropPod ⇒ 构造时两格 = 2 / 0（= `CardSetup.c:160-200`）
        var hero = new UnitState(Hero("W22Hero", KeywordTable.DropPod), true);
        Tr(hero.Has(KeywordTable.DropPod), "E1-前 前提：这张督军身上**真的**带着 `droppod`");
        C(hero.CurrentDropPodHealth, 2, "E1 🔴 督军 + `droppod` ⇒ `currentDropPodHealth = 2`");
        C(hero.CurrentDropPodAttack, 0, "E1′ 督军 + `droppod` ⇒ `currentDropPodAttack = 0`");

        // E2：不带 dropPod 的督军 ⇒ 两格不动（仍 0 / 0）—— 证明 E1 不是「无条件赋值」
        var hero2 = new UnitState(Hero("W22HeroNoPod"), true);
        Tr(!hero2.Has(KeywordTable.DropPod), "E2-前 前提：这张没有 `droppod`");
        C(hero2.CurrentDropPodHealth, 0, "E2 没有 `droppod` ⇒ 不赋值（仍是 0；改成无条件赋值 ⇒ 本条红）");

        // E3：带 dropPod 的**部队**（非督军）⇒ 也不赋值（原版闸是 `cardType == 10`）
        var troop = new UnitState(new CardDef("W22TroopPod", "W22TroopPod", "troop", "", null, "SpaceWolves", 1, 1, 3, 0,
                                             new[] { KeywordTable.DropPod }, subtype: "troop"), false);
        Tr(troop.Has(KeywordTable.DropPod), "E3-前 前提：这张部队确实带 `droppod`");
        C(troop.CurrentDropPodHealth, 0, "E3 非督军 ⇒ 不赋值（闸 = `cardType == 10`；把闸删掉 ⇒ 本条红）");

        // E4：`ResetToValuesInHand` 那两条是**无条件**的（原版 `.c:110-133` 没有 cardType/trait 闸）
        var plain = new UnitState(Troop("W22Plain"), false);
        plain.CurrentDropPodHealth = 7; plain.CurrentDropPodAttack = 3;
        plain.ResetToValuesInHand(clearBacklash: true);
        C(plain.CurrentDropPodHealth, 2, "E4 🔴 `ResetToValuesInHand` 无条件复位成 2（原版 `.c:110-133`）");
        C(plain.CurrentDropPodAttack, 0, "E4′ …和 0");
    }

    // ============================================================ F FTUE 低血无敌位（账 707②）
    static void SectionF_FTUE低血无敌位()
    {
        MenuCheck.Section(_sink, "F 账 :707② FTUE `+0x2e4`：`isPlayer ∧ cardType==10 ∧ 配置` 三条全真才置");

        var hero = new UnitState(Hero("W22Ftue"), true);
        Tr(!hero.ShouldBecomeInvulnerableWithLowLife, "F0 默认 false（不复用上一次的状态）");

        hero.SetupFTUELowLifeInvulnerability(isPlayer: true, ftueBotConfig: true);
        Tr(hero.ShouldBecomeInvulnerableWithLowLife, "F1 三条全真 ⇒ 置真（判据 `CardSetup.c:88-114`）");

        hero.SetupFTUELowLifeInvulnerability(isPlayer: true, ftueBotConfig: true);   // 幂等
        Tr(hero.ShouldBecomeInvulnerableWithLowLife, "F1′ 幂等（再置一次仍是真）");

        hero.SetupFTUELowLifeInvulnerability(isPlayer: false, ftueBotConfig: true);
        Tr(!hero.ShouldBecomeInvulnerableWithLowLife, "F2 🔴 非玩家侧 ⇒ 假（把 `isPlayer` 那一项删掉 ⇒ 本条红）");

        hero.SetupFTUELowLifeInvulnerability(isPlayer: true, ftueBotConfig: false);
        Tr(!hero.ShouldBecomeInvulnerableWithLowLife, "F2′ 配置为假 ⇒ 假（🔶 远端拿不到时我们一律传 false）");

        var troopU = new UnitState(Troop("W22FtueTroop"), false);
        troopU.SetupFTUELowLifeInvulnerability(isPlayer: true, ftueBotConfig: true);
        Tr(!troopU.ShouldBecomeInvulnerableWithLowLife, "F3 🔴 非督军（`cardType != 10`）⇒ 假（把 `CardType` 那一项删掉 ⇒ 本条红）");
    }

    // ============================================================ G 按记录摘走同一份逐 case（账 853 接线形状）
    static void SectionG_按记录摘走同一份逐case()
    {
        MenuCheck.Section(_sink, "G 账 :853：`EffectRecord.TempBuffRecord` ⇒ `RemoveEffectRecord` 走**同一个**逐 case");

        var u = new UnitState(Troop("W22ByRecord"), false);
        var rec = u.ApplyTraitEffect("fast", 1);
        Tr(rec != null && u.Has("fast"), "G0 前提：记录与关键词都在");
        var buff = new UnitState.TempBuff { IsKeyword = true, Name = "fast", Value = 1, TraitRec = rec };
        u.AddTempBuff(buff);
        C(u.TempBuffs.Count, 1, "G0′ 前提：限时增益账里一条");

        u.RemoveEffectRecord(new UnitState.EffectRecord
        {
            BuffType = buff.BuffType,          // = 派生属性（`UnitStateEffectDisplay.cs:92`）—— 调用点手里就有
            Trait = buff.Name,
            TempBuffRecord = buff,
        });

        C(u.TempBuffs.Count, 0, "G1 🔴 按记录摘 ⇒ 限时增益账里那条**真的没了**（删掉 `rec.TempBuffRecord` 那一支 ⇒ 本条红）");
        Tr(!u.Has("fast"), "G1′ 而且走的是**同一份逐 case**（关键词减量到 0 ⇒ 掉）");

        // G2：幂等 —— 同一个记录再摘一次什么都不做（对应原版「已经不在 activeEffects 里」）
        u.RemoveEffectRecord(new UnitState.EffectRecord
        {
            BuffType = buff.BuffType,
            Trait = buff.Name,
            TempBuffRecord = buff,
        });
        C(u.TempBuffs.Count, 0, "G2 再摘一次 ⇒ 幂等、不报错、条数仍 0");

        // G3：`permanentBuff` 早退**在** TempBuffRecord 之前（原版 `.c:35-38` 是最前一句）
        var u3 = new UnitState(Troop("W22Perm"), false);
        var rec3 = u3.ApplyTraitEffect("fast", 1);
        var buff3 = new UnitState.TempBuff { IsKeyword = true, Name = "fast", Value = 1, TraitRec = rec3 };
        u3.AddTempBuff(buff3);
        u3.RemoveEffectRecord(new UnitState.EffectRecord
        {
            BuffType = buff3.BuffType,
            Trait = buff3.Name,
            TempBuffRecord = buff3,
            Permanent = true,
        });
        C(u3.TempBuffs.Count, 1, "G3 🔴 `permanentBuff` ⇒ 摘不掉（位置照原版：早退在最前；把早退挪到 TempBuffRecord 之后 ⇒ 本条红）");
        Tr(u3.Has("fast"), "G3′ 关键词也没被动");
    }
}
