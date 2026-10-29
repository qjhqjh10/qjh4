// B98G5_Assert.cs — 执行代理 `B98_G5`（第九十八会话 · 波 5b · 战斗线写手）的断言集
//
// ============================ 它盯的是什么 ============================
//   账 `战斗逻辑驱动 :636` ／ `:832②`（**同一处实参**）＋ `:1041`。
//
//   ① `:636 / :832②` —— `RuleEngine/Core/Aura.cs:896` 的
//      `t.RecordGrant(RuleCore.NormalizeAttr(op.Attr), op.Value, GrantTag)` 只传 3 个实参，
//      而 `UnitState.RecordGrant` 的第 5 形参 `caster` 早就在（`UnitState.cs:1358`）⇒ 只是**没传**。
//      本笔补 `caster: src.Name`。
//
//      判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_R10_波5残差归属.md` §二 的 636 / 832 两行
//      ＋ `资料/完全复刻/判据/B98_R4_波5补判据.md`。
//      **为什么 `caster` = 光环来源那张卡**（本代理回源认定的那一行，写进报告 `报告/B98_G5.md` §一）：
//        · `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:1199`
//          = `*(undefined8 *)(local_res8 + 0x18) = *(undefined8 *)(param_3 + 0x10);`
//          （`local_res8` = 新建的 `CardEffect`，`+0x18` = `enchantingCard`（`dump.cs:119066`），
//            `param_3` = `AbilityData`，`+0x10` = `thisCard`（ILSpy `AbilityData.cs:42-43`）；
//            ⚠️ 施工单写的是 `:1198`，**现读是 `:1199`**（差 1 行），以现读为准）
//        · `param_3` 是 `AbilityData` 的**旁证**：同一段里 `param_3 + 0x40` / `+0x41` 读作 `whileInPlay` /
//          `whileInPlayVariable`（`:1194-1196`）、`+0x28` 写给 `CardEffect.baseEnchantmentCard(+0x98)`（`:1205`）、
//          `+0x4c` 写给 `enchantmentCardId(+0xa4)`（`:1207`）—— 只有 `AbilityData` 的字段表对得上。
//        · 那条 `AbilityData.thisCard` 在 while-in-play 链上**就是光环来源卡**：
//          `CardScript__UpdateWhileInPlay.c:14/19-20`（`RawCardScript__UpdateWhileInPlay(rawCard(+0x28), manager(+600), param_1=本卡, actingCard, 0)`）
//          → `RawCardScript__UpdateWhileInPlay.c` 里 `TriggerAbilities(rawCard, 0x14 /*WhileInPlay*/, manager, thisCard, actingCard, …)`
//          → ILSpy `RawCardScript.cs:1768` `pooledAbilityData.SetNewData(thisCard, …)`。
//        ⇒ 落点 = `src.Name`（`Aura.ApplyAura` 的 `src` = 光环来源单位）。
//          ⛔ 别用 `GrantTag` 顶替（那是 `Source` 来源**标签**，原版两格不同字段）；⛔ 也别用「被罩住的单位」。
//
//   ② `:1041` —— `UnitState.AnyEffectGrantsTrait`（`UnitState.cs:3444`）看不见**光环授出的 trait**。
//      口径 = `判据/B98_R4_波5补判据.md` §1041 的 **B 案**：在 `:3455` 的 `return false;` 前加
//      `if (AuraKwValue(trait) > 0) return true;`（只读 `_auraKw`、不写账、不双计）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · A1/A2：把 `Aura.cs:896` 的 `caster: src.Name` 删掉（回到 3 个实参）⇒ 那条 grant 的 `Caster` 是 `null`
//           ⇒ **A1 红**；`ContainsExtrinsicEffectsFrom(srcName)` 也回到 false ⇒ **A2 红**。
//   · A3：把 `caster:` 错传成 `GrantTag`（或传成被罩住的单位名）⇒ **A3 红**（判别式：`Caster != Source` 且 ≠ 目标名）。
//   · A4：把 `caster:` 只传进 grant、不落 `_statMods` —— 不可能（同一句），但若有人把 `RecordGrant` 的
//          `caster` 参数吞掉（不写进 `GrantRecord`）⇒ **A1/A2/A4 同时红**。
//   · B1：把 `RevertGrantsCastBy` 的 `extrinsicOnly` 闸去掉 ⇒ 同施放者的**非** extrinsic 那条也被撤 ⇒ **B1 红**。
//   · C2：把 `AnyEffectGrantsTrait` 新加的那半句删掉 ⇒ 只有光环授出、没有 `addTrait` 记录的那一档回到 false ⇒ **C2 红**。
//   · C3（**反向灭自证**）：把 `AuraKwValue` 换成 `KwValue`（读 `_keywords` 全集）⇒ **C3 红**
//          （`AddKeyword` 给的关键词不该让 `AnyEffectGrantsTrait` 为真 —— 原版那张表只装「效果记录」）。
//   · C5：把新加那句挪到**残骸闸之前** ⇒ **C5 红**（原版 `isRemnant && !isLeavingRemnant` 是整段早退）。
//
// ============================ 怎么用（宿主）============================
//   `B98G5_Assert.Run(); _sink.Pass += B98G5_Assert.Pass; _sink.Fail += B98G5_Assert.Fail;`
//   · 推荐宿主 = `Editor/RuleEngineTest.cs`（`RuleEngineTest.Run`）—— 本件**只碰纯引擎**
//     （`RuleCore.NewBattle` / `UnitState` / `Auras`），不建 GameObject、不建资产、不需要场景。
//   · A 段要一局**真对局上下文**（`Auras.Recompose` 会遍历双方 `Board`）；牌库只放一张督军
//     （照 `B97W38_Assert` 的 `DeckOf` 先例），双方单位用**白盒放盘 + 手动 Recompose**摆上去。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B98_G5`：账 `战斗逻辑驱动:636 / 832②`（光环赠予要带 `caster`）＋ `:1041`
/// （光环授出的 trait 要能被 `HasCurrentTraitActiveEffect` 看见）。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B98G5_Assert
{
    static readonly CheckSink _sink = new CheckSink("[G5] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }
    /// <summary>本趟**实际执行**的断言条数（= Pass + Fail；调用方打印它 = 自证真被跑到）。</summary>
    public static int RunCount { get { return _sink.Pass + _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具

    /// <summary>⚠️ `subtype` **必须给** `"Warlord"` —— 真卡的 56 个督军全是它（见 `RuleEngineTest.HeroOf` 的注释）。</summary>
    static CardDef Warlord(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }

    static List<CardDef> DeckOf(string faction)
    {
        return new List<CardDef> { Warlord("B98G5Hero_" + faction, faction) };
    }

    /// <summary>一局**真对局上下文**（`Auras.Recompose` 要有双方 `Board` 才跑得起来）。</summary>
    static BattleContext NewCtx()
    {
        return RuleCore.NewBattle(DeckOf("SpaceWolves"), DeckOf("Goff"), seed: 0, shuffle: false);
    }

    /// <summary>白盒放盘（`RuleEngineTest.Place` 的等价物；⛔ **不走棋盘钩子** ⇒ 摆完必须自己 `Auras.Recompose`）。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>带一条**数值光环**的源牌（`Your troops have +1 Attack`）。
    /// ⚠️ 这句必须能被 `Auras.TryParse` 收下 —— 收不下 A0 会红（⛔ 不许静默跳过）。</summary>
    static CardDef AuraSource(string name)
    {
        return new CardDef(name, name, "troop", "Your troops have +1 Attack", "common", "Test",
                           1, 1, 3, 0, null, subtype: "troop");
    }

    static CardDef Troop(string name, int attack, int health)
    {
        return new CardDef(name, name, "troop", "", "common", "Test", 1, attack, health, 0, null,
                           subtype: "troop");
    }

    /// <summary>`_grants` 里第一条「属性 = <paramref name="attr"/> 且施放者 = <paramref name="caster"/>」的记录。</summary>
    static UnitState.GrantRecord GrantOf(UnitState u, string attr, string caster)
    {
        var list = u.PendingGrants;
        for (int i = 0; i < list.Count; i++)
        {
            var g = list[i];
            if (g == null || g.Attr != attr) continue;
            if (caster != null && g.Caster != caster) continue;
            return g;
        }
        return null;
    }

    // ============================================================ 入口
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();     // `RunCount` 只算本趟
        MenuCheck.Section(_sink, "B98_G5 账 636 / 832②（光环赠予带 `caster` = 光环来源卡）＋ 账 1041"
                              + "（光环授出的 trait 要能被 `AnyEffectGrantsTrait` 看见）");

        SectionA_光环赠予带Caster();
        SectionB_非Extrinsic不撤();
        SectionC_光环授出的Trait();

        Debug.Log(_sink.P + "B98G5 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 账 636 / 832②
    static void SectionA_光环赠予带Caster()
    {
        MenuCheck.Section(_sink, "A 光环走 `ApplyAura` ⇒ grant 的 `Caster` = **光环来源卡名**（判据 `AbilityLogic__PlayAbility.c:1199`），"
                              + "且 `ContainsExtrinsicEffectsFrom(来源名)` 看得出来、按它摘得掉");

        var srcDef = AuraSource("B98G5AuraSrc");
        // A0 **前提硬断言**（⛔ 不许写成 `if (条件) Check(...)` —— 前提不成立时会静默跳过）
        C(srcDef.AuraSpecs.Count, 1,
          "A0 ★ 前提：源牌的 `desc` 被 `Auras.TryParse` 收成 **1 条** `AuraSpec`（收不下 ⇒ 下面全在测空气，本条必须先红）");
        if (srcDef.AuraSpecs.Count != 1)
        {
            // 前提破了：把失败说清楚（⛔ 不静默），但仍继续跑 B/C 两段（它们不依赖这局夹具）
            MenuCheck.Section(_sink, "A0 前提不成立 —— A 段剩余条目**无法执行**（⚠️ 这是失败，不是跳过）");
            return;
        }
        C(srcDef.AuraSpecs[0].Payload, "+1 Attack", "A0′ 前提：认出来的载荷逐字是 `+1 Attack`");

        var ctx = NewCtx();
        var src = Place(ctx, 0, 0, srcDef);
        var tgt = Place(ctx, 0, 1, Troop("B98G5Tgt", 2, 4));

        Auras.Recompose(ctx);                                   // 白盒放盘不走钩子 ⇒ 手动重算

        // A0″ 前提：光环真的生效了（否则下面的 Caster 断言是空的）
        C(tgt.Attack, 3, "A0″ 前提：光环生效 —— 目标近战 2 ⇒ **3**（`+1 Attack`）");

        var g = GrantOf(tgt, "attack", src.Name);
        Tr(g != null,
           "A1 🔴 有一条「属性 = attack、**施放者 = 光环来源卡名**（" + src.Name + "）」的 grant 记录"
         + "｜🧨 把 `Aura.cs:896` 的 `caster: src.Name` 删掉（回到 3 个实参）⇒ `Caster` 为 null ⇒ 红");
        if (g != null)
        {
            C(g.Source, Auras.GrantTag,
              "A1′ 同一条记录的 `Source` 仍是**来源标签**（`Auras.GrantTag`）—— 原版 `Source` 与 `enchantingCard` 是两格不同字段");
            Tr(g.Extrinsic,
               "A1″ 同一条记录 `Extrinsic == true`（光环 = 原版 `whileInPlay` ⇒ `CardEffect.extrinsic(+0x30)`，"
             + "写点 `AbilityLogic__PlayAbility.c:1194-1197`）");
            Tr(src.Name != Auras.GrantTag && g.Caster != Auras.GrantTag,
               "A1‴ 判别式：`Caster` **不是** `GrantTag`（把 `caster:` 错传成 `GrantTag` ⇒ 红）");
            Tr(g.Caster != tgt.Name,
               "A1⁗ 判别式：`Caster` **不是被罩住的那个单位**（传成目标名 ⇒ 红）");
        }

        Tr(tgt.ContainsExtrinsicEffectsFrom(src.Name),
           "A2 🔴 「先查」看得见这条光环效果（`ContainsExtrinsicEffectsFrom(来源名)` == true）"
         + "｜🧨 不传 `caster` 时只有 `Source == \"光环\"` 可比 ⇒ 永远是 false ⇒ 红。"
         + "⚠️ 这正是 `removeEffectsFrom(47)` 会不会入队的那一格");

        // A3 / A4：按施放者撤得掉，且数值真的回落
        int n = tgt.RemoveExtrinsicEffectsFrom(src.Name);
        Tr(n >= 1, "A3 🔴 按来源名撤得掉（`RemoveExtrinsicEffectsFrom(来源名)` 返回 ≥ 1；实得 " + n + "）");
        C(tgt.Attack, 2, "A4 🔴 撤完近战回到 **2**（base 2，光环那份没了）");
        Tr(!tgt.ContainsExtrinsicEffectsFrom(src.Name), "A4′ 撤完「再查」说没有（查/摘同口径）");
        // A5 灭自证（**范围**）：摘的是「命中施放者的那个单位」，⛔ 不是「全场按这个名字清一遍」
        Tr(src.ContainsExtrinsicEffectsFrom(src.Name),
           "A5 🔴 同一施放者罩着的**另一个单位**（来源自己，`Your troops` 不含 `other` ⇒ 它也被罩着）不受影响 —— "
         + "`RemoveExtrinsicEffectsFrom` 是逐单位调用、⛔ 不是全场广播");
        C(src.Attack, 2, "A5′ 来源自己的 `+1 Attack` 还在（base 1 ⇒ 2）");
    }

    // ============================================================ B 灭自证
    static void SectionB_非Extrinsic不撤()
    {
        MenuCheck.Section(_sink, "B 灭自证：**同一个施放者**给的两条里，只有 extrinsic 的那条被摘，非 extrinsic 的一条都不动");

        var u = new UnitState(Troop("B98G5B", 2, 5), false);
        const string caster = "B98G5CasterCard";

        // ① extrinsic（= 光环那条路：source 是 `GrantTag`）
        u.RecordGrant("attack", 1, Auras.GrantTag, caster: caster);
        // ② 非 extrinsic（source 是别的标签 ⇒ `Extrinsic` 假），**同一个 caster**
        u.RecordGrant("attack", 1, "B98G5OtherSource", caster: caster);
        C(u.Attack, 4, "B0 前提：两条 `+1 attack` 都加上了（2 ⇒ 4）");
        C(u.PendingGrants.Count, 2, "B0′ 前提：`_grants` 两条");

        int n = u.RemoveExtrinsicEffectsFrom(caster);
        C(n, 1,
          "B1 🔴 只摘 **extrinsic** 那条（返回 1）｜🧨 去掉 `extrinsicOnly` 闸 ⇒ 返回 2 ⇒ 红"
        + "（原版 `CardScript__RemoveExtrinsicEffectsFrom.c:32` 第一条合取就是 `extrinsic != 0`）");
        C(u.Attack, 3, "B1′ 值只掉 **1**（2 ⇒ 3）—— 非 extrinsic 那条留着");
        C(u.PendingGrants.Count, 1, "B1″ `_grants` 还剩 1 条（非 extrinsic 那条）");
        Tr(!u.ContainsExtrinsicEffectsFrom(caster),
           "B1‴ 撤完「再查」说没有 —— 剩下那条不是 extrinsic，谓词不该再命中");
    }

    // ============================================================ C 账 1041
    static void SectionC_光环授出的Trait()
    {
        MenuCheck.Section(_sink, "C 账 1041：`AnyEffectGrantsTrait` 要看得见**光环授出**的 trait"
                              + "（原版没有 Aura，光环就是 `activeEffects` 里的 `buffType 3` 记录 ⇒ `EntityScript__HasCurrentTraitActiveEffect.c:15-48` 扫得到）");

        // C0 前提
        {
            var u0 = new UnitState(Troop("B98G5C0", 1, 3), false);
            Tr(!u0.AnyEffectGrantsTrait("stealth"), "C0 前提：什么都没给 ⇒ false");
            C(u0.AuraKwValue("stealth"), 0, "C0′ 前提：`AuraKwValue(stealth) == 0`");
        }

        // C1 既有那一半（`addTrait` 记录）仍然认 —— 回归保护
        {
            var u = new UnitState(Troop("B98G5C1", 1, 3), false);
            u.ApplyTraitEffect("stealth", 1, source: "B98G5Rec", extrinsic: true);
            Tr(u.AnyEffectGrantsTrait("stealth"), "C1 回归：`_traitEffects` 里那条 `addTrait` 记录照旧认（原有行为不许被本笔碰坏）");
        }

        // C2 ★ 本笔新增的那一半：只有光环授出、**没有**任何 addTrait 记录
        {
            var u = new UnitState(Troop("B98G5C2", 1, 3), false);
            u.AddAuraKeyword("stealth", 1);
            C(u.AuraKwValue("stealth"), 1, "C2′ 前提：`AuraKwValue(stealth) == 1`");
            Tr(u.PendingGrants.Count == 0 && u.TraitEffects.Count == 0,
               "C2″ 前提：`_grants` / `_traitEffects` **都是空的**（⇒ 下面那条只可能来自新加的那半句）");
            Tr(u.AnyEffectGrantsTrait("stealth"),
               "C2 🔴 光环授出的 trait ⇒ **判真**｜🧨 把 `UnitState.cs:3455` 新加的 `if (AuraKwValue(trait) > 0) return true;` 删掉 ⇒ 红"
             + "（原版那条记录就在 `activeEffects` 里，本方法扫得到；我们只写 `_auraKw` ⇒ 必须补读）");
            // C3 **反向灭自证**：`AddKeyword` 给的关键词（不走光环那本账）**不该**让本方法为真
            var v = new UnitState(Troop("B98G5C3", 1, 3), false);
            v.AddKeyword("stealth", 1);
            C(v.AuraKwValue("stealth"), 0, "C3′ 前提：`AddKeyword` 不动 `_auraKw`");
            Tr(!v.AnyEffectGrantsTrait("stealth"),
               "C3 🔴 非光环给的关键词 ⇒ **判假**｜🧨 把新加那句的 `AuraKwValue` 换成 `KwValue`（读 `_keywords` 全集）⇒ 红。"
             + "理由：原版那张表只装「效果记录」，卡面/普通效果直接给的关键词**不在** `activeEffects` 里");

            // C4 撤掉光环 ⇒ 回到假（原方法语义：这一路本来就是「还有没有**别的**授予路」）
            u.ClearAuraGrants();
            Tr(!u.AnyEffectGrantsTrait("stealth"), "C4 🔴 光环收回后 ⇒ **判假**（新加那句不是恒真）");

            // C5 残骸闸**优先**：原版 `isRemnant && !isLeavingRemnant` 整段早退，光环不豁免
            var r = new UnitState(Troop("B98G5C5", 1, 3), false) { IsRemnant = true, IsLeavingRemnant = false };
            r.AddAuraKeyword("stealth", 1);
            Tr(!r.AnyEffectGrantsTrait("stealth"),
               "C5 🔴 残骸（且不是正在离开残骸态）⇒ 仍判假｜🧨 把新加那句挪到残骸闸**之前** ⇒ 红"
             + "（判据 `EntityScript__HasCurrentTraitActiveEffect.c:15`：`isRemnant(+0x65) == 0 || isLeavingRemnant(+0x66) != 0` 才往下扫）");
            r.IsLeavingRemnant = true;
            Tr(r.AnyEffectGrantsTrait("stealth"),
               "C5′ 正在离开残骸态 ⇒ 残骸闸放开、光环那半句接管 ⇒ **判真**");
        }
    }
}
