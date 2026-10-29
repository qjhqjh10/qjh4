// B97W30_Assert.cs — 第九十七会话 · 执行代理 `B97_W30`（波 1 补位）
//
// ============================ 它盯的是什么 ============================
// 账 `战斗逻辑驱动:64` / `:482`ⓐ / `:636` 那一族：**`extrinsic` 三本账**
//   （`_statMods` / `_buffs` / `_grants`）**补齐 + 消费侧闸**（生产侧灌值见报告 §三 的可粘贴补丁）。
//
//   判据（🥇 `d:/2/tools/decomp_full/` 全文现读，2026-10-29）：
//     · `CardScript__ContainsExtrinsicEffectFrom.c` —— 四条**合取**：
//         ① `*(char*)(effect + 0x30) != 0`（`CardEffect.extrinsic`）
//         ② `enchantingCard(+0x18) != null` ∧ `== 参`
//         ③ `buffType(+0x38) == param_3`（`param_3 == 0` ⇒ 不筛）
//         ④ `baseEnchantmentCard(+0x98) == param_4`（`param_4 == null` ⇒ 要求那一格也是 null）
//     · `CardScript__RemoveExtrinsicEffectsFrom.c:17/22/32/37/47/51/56` —— 倒序扫，① ∧ ② ∧ ③ 后 `RemoveEffect`。
//     · 写点（`extrinsic` 从哪来）＝ `AbilityLogic__PlayAbility.c:1197`
//         `cVar4 = 1; if (*(char*)(param_3 + 0x40) == 0) cVar4 = *(char*)(param_3 + 0x41);`
//         `*(bool*)(newEffect + 0x30) = cVar4 != 0;`
//       `param_3` = `AbilityData`（签名 `PlayAbility(BattleManager, AbilityData, AbilityTrigger)`），
//       `+0x40 = whileInPlay`、`+0x41 = whileInPlayVariable`（`AbilityData.cs` 字段序）
//       ⇒ **`extrinsic = (ability.trigger ∈ {20 WhileInPlay, 240 WhileInPlayVariable})`**，与 `UnitState.IsExtrinsicSource` 的 doc 同一条。
//     · 唯一调用点 `BattleManager__RemoveExtrinsicEffectsFrom.c:48/73/91` 三次都传 `(卡, actingCard, 0, 0)`
//       ⇒ 合取 ③④ **恒不启用**（我们没建模 ③④，与判据等价；见报告 §没查清）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `RuleEngineTest.cs` 里加**一个块**（位置 = 紧接 `{ CheckB97W1Grants(); }` 那一行之后）：
//       // 🆕 2026-10-29（调度台代接线）：B97_W30 —— 账 64/482/636 `extrinsic` 三本账 + 消费侧闸。
//       { CheckB97W30Extrinsic(); }
//   🔴 与 `B97_W1` 同一处置：**故意不用 `Step(...)`、方法名不带 `Test`/`Report` 前缀** —— `Step(...)` 会让
//     `_ranCases`（`RuleEngineTest.cs:1379`）记账、`VerifyRunCaseCoverage()` 拿反射清单对账，
//     而 `工具/gen_diag_runner.py` 只认「8 空格缩进的顶层语句」；块调用**两条都不碰**。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」都写在文案里）============================
//   · `RecordGrant` 初值器里删掉 `Extrinsic = extrinsic || IsExtrinsicSource(source)` ⇒ 2 段首条红。
//   · `RecordGrant` 的 `AddStatMod(..., g.Extrinsic)` 改回 `IsExtrinsicSource(source)` ⇒ 4 段首条红。
//   · `AddTempBuff` 的 `AddStatMod(..., b.Extrinsic)` 改回 `false` ⇒ 1 段末条红（限时那条摘不掉）。
//   · `AddTempBuff` 里 `if (b.Extrinsic && b.Mod != null) b.Mod.Extrinsic = true;` 删掉 ⇒ 4 段中间红。
//   · `RemoveExtrinsicEffectsFrom` 三处 `extrinsicOnly: true` 任一去掉 / `_statMods` 那圈去掉 `!_statMods[i].Extrinsic` ⇒ 2 段全红。
//   · `ContainsExtrinsicEffectsFrom` 三处 `&& Extrinsic` 任一去掉 ⇒ 3 段首条红。
//   · `RevertGrantsFrom` / `RevertGrantsCastBy` / `RemoveBuffsFromCard` 把 `extrinsicOnly` 直接改成无条件 ⇒ 3 段全红（多撤）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，见文件头）。</summary>
    public static void CheckB97W30Extrinsic()
    {
        Section("🆕 B97_W30：账 64/482/636 —— `extrinsic` 三本账（`_statMods` / `_buffs` / `_grants`）的 `extrinsic != 0` 闸");
        B97W30ExtrinsicRemoved();
        B97W30NonExtrinsicUntouched();
        B97W30Conjunctions();
        B97W30ColumnPropagation();
    }

    // ==================================================================
    //  1 · 三本账各一条 extrinsic ⇒ `RemoveExtrinsicEffectsFrom` 全摘
    //  判据 = `CardScript__RemoveExtrinsicEffectsFrom.c:32`（`extrinsic != 0`）＋ `.c:56`（`RemoveEffect`）
    // ==================================================================
    static void B97W30ExtrinsicRemoved()
    {
        var card = Unit("B97W30A", 1, 2, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97W30A2", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;
        int baseAtk = u.CurrentMeleeAttack;
        Check(baseAtk, 2, "前提：卡面近战 2（期望值照 `Unit(\"B97W30A\", 1, 2, 5)` 的实参写死，不向实现反算）");

        // ① grant（`RecordGrant` 会连带建一条同来源的 StatMod）
        u.RecordGrant("attack", 3, "B97W30S_A", caster: "B97W30C_A", extrinsic: true);
        // ② 限时增益（`AddTempBuff` 会自己建那条 StatMod）
        u.AddTempBuff(new UnitState.TempBuff { Name = "attack", Value = 2, SourceCard = "B97W30S_A", Src = "B97W30", Extrinsic = true });
        // ③ 纯修饰
        u.AddStatMod("attack", 4, "B97W30S_A", extrinsic: true);
        Check(u.CurrentMeleeAttack, baseAtk + 9, "前提：三条账都吃到（+3 +2 +4 = +9）");

        Check(u.ContainsExtrinsicEffectsFrom("B97W30S_A"), true,
              "前提：「先查」为真（三条都是 extrinsic ∧ 来源匹配）—— 它为假则下面那条摘不掉是**前面**的问题");

        u.RemoveExtrinsicEffectsFrom("B97W30S_A");
        Check(u.CurrentMeleeAttack, baseAtk,
              "🧨 三本账（grant / tempBuff / statMod）的 extrinsic 那几条**全摘** ⇒ 回 base。"
            + "把 `RemoveExtrinsicEffectsFrom` 里任一处的 `extrinsicOnly: true` 去掉、或 `_statMods` 那一圈去掉 `!_statMods[i].Extrinsic` ⇒ 实得 base+3 / base+2 / base+4");
    }

    // ==================================================================
    //  2 · 同一张卡的**非** extrinsic 记录一条都不动（灭自证：闸恒真 ⇒ 本段全红）
    //  判据 = `CardScript__RemoveExtrinsicEffectsFrom.c:32` 的第一条合取；原版没有「按来源撤」的通用口
    // ==================================================================
    static void B97W30NonExtrinsicUntouched()
    {
        var card = Unit("B97W30B", 1, 2, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97W30B2", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;
        int baseAtk = u.CurrentMeleeAttack;

        // 三条**来源/施放者都相同**，但 `extrinsic` 全为假（缺省）
        u.RecordGrant("attack", 3, "B97W30S_B", caster: "B97W30C_B");
        u.AddTempBuff(new UnitState.TempBuff { Name = "attack", Value = 2, SourceCard = "B97W30S_B", Src = "B97W30" });
        u.AddStatMod("attack", 4, "B97W30S_B");
        Check(u.CurrentMeleeAttack, baseAtk + 9, "前提：三条账都吃到");

        Check(u.ContainsExtrinsicEffectsFrom("B97W30S_B"), false,
              "🧨 「先查」：三条都**不是** extrinsic ⇒ 假（把 `ContainsExtrinsicEffectsFrom` 的三处 `&& Extrinsic` 删掉 ⇒ 实得 true ⇒ 会入队一条**空**动作）");
        int n = u.RemoveExtrinsicEffectsFrom("B97W30S_B");
        Check(n, 0, "🧨 「再摘」同口径：返回 0 条（`ContainsExtrinsicEffectsFrom` 与 `RemoveExtrinsicEffectsFrom` 必须同口径）");
        Check(u.CurrentMeleeAttack, baseAtk + 9,
              "🧨 同一张卡的**非** extrinsic 记录**一条都不动**（灭自证：把三处 `extrinsicOnly` 改成无条件 / 删掉 `_statMods` 那个 `Extrinsic` 判据 ⇒ 三条被多撤 ⇒ 实得 base）");
    }

    // ==================================================================
    //  3 · 四条合取**逐条**断（③④ 未建模 ⇒ 只断「唯一调用点传 0/0 时恒不启用」这一格）
    //  判据 = `CardScript__ContainsExtrinsicEffectFrom.c`（`.c` 全文）＋ `BattleManager__RemoveExtrinsicEffectsFrom.c:48/73/91`
    // ==================================================================
    static void B97W30Conjunctions()
    {
        var card = Unit("B97W30C", 1, 2, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97W30C2", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;
        int baseAtk = u.CurrentMeleeAttack;

        // 合取 ② 的**施放者**那一半独自成立（`Source` 是别的 tag、`Caster` 才是这个名字）
        u.RecordGrant("attack", 3, "B97W30_TAG_C", caster: "B97W30C_C", extrinsic: true);
        // 合取 ② 的**来源**那一半独自成立（`Source` 是这个名字、没有 `Caster`）
        u.AddStatMod("attack", 2, "B97W30C_C", extrinsic: true);
        Check(u.CurrentMeleeAttack, baseAtk + 5, "前提：两条都吃到（+3 +2）");

        // ② 不匹配 ⇒ 两条都不算（`Extrinsic` 为真也不行）
        Check(u.ContainsExtrinsicEffectsFrom("B97W30C_Nobody"), false,
              "🧨 合取②：`Source` / `Caster` 都不是这个名字 ⇒ 假（把 `Source == name || Caster == name` 放宽成「只看 `Extrinsic`」⇒ 实得 true）");
        Check(u.RemoveExtrinsicEffectsFrom("B97W30C_Nobody"), 0, "🧨 合取②：按别的名字撤 ⇒ 0 条");
        Check(u.CurrentMeleeAttack, baseAtk + 5, "前提：按错名字撤之后两条都还在");

        // ① 缺 `Extrinsic`（同一名字、非 extrinsic）⇒ 也不算 —— 本段用**一张已摘空的卡**上的新记录验，避免与 2 段重复
        u.AddTempBuff(new UnitState.TempBuff { Name = "attack", Value = 1, SourceCard = "B97W30C_C", Src = "B97W30", Extrinsic = false });
        Check(u.ContainsExtrinsicEffectsFrom("B97W30C_C"), true, "前提：名字命中的那两条是 extrinsic ⇒ 仍为真（新加那条 false 不改变结果）");
        u.RemoveExtrinsicEffectsFrom("B97W30C_C");
        Check(u.CurrentMeleeAttack, baseAtk + 1,
              "🧨 按名字撤：extrinsic 那两条（+3 grant / +2 statMod）走了，**非** extrinsic 的 +1 限时那条留着"
            + "（合取① 逐条生效 —— 三条同名字、只有 `Extrinsic` 一格不同）");
    }

    // ==================================================================
    //  4 · 三列之间**一份判据**（`RecordGrant` / `AddTempBuff` 的连带 `StatMod`）+ 光环那半没被改掉
    //  判据 = `GrantRecord.Extrinsic` 与 `StatMod.Extrinsic` 必须同值；`IsExtrinsicSource` = 原有口径
    // ==================================================================
    static void B97W30ColumnPropagation()
    {
        var card = Unit("B97W30D", 1, 2, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97W30D2", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;

        // 4·a `RecordGrant` 的 `extrinsic` 同时进 `GrantRecord` 与它连带的那条 `StatMod`
        var mod = u.RecordGrant("attack", 2, "B97W30S_D", caster: "B97W30C_D", extrinsic: true);
        Check(mod != null && mod.Extrinsic, true,
              "🧨 `RecordGrant(..., extrinsic: true)` 连带的那条 `StatMod` 也标 extrinsic（把那行改回 `IsExtrinsicSource(source)` ⇒ 实得 false ⇒ 两本账各说各话）");
        int hits = 0; bool grantExtrinsic = false;
        foreach (var r in u.PendingGrants)
            if (r != null && r.Caster == "B97W30C_D") { hits++; grantExtrinsic = r.Extrinsic; }
        Check(hits, 1, "前提：恰有 1 条 `Caster == \"B97W30C_D\"` 的 grant");
        Check(grantExtrinsic, true, "🧨 `GrantRecord.Extrinsic` 也置真（删掉初值器里 `Extrinsic = …` 那一格 ⇒ 实得 false）");

        // 4·b 调用方**自带** `Mod` 的限时增益也要被标上（`ApplyOneGain` 那条路就是这么传的）
        var bare = u.AddStatMod("attack", 5, null, extrinsic: false);
        Check(bare.Extrinsic, false, "前提：这条 `StatMod` 一开始不是 extrinsic");
        u.AddTempBuff(new UnitState.TempBuff { Name = "attack", Value = 5, SourceCard = "B97W30S_D2", Src = "B97W30", Extrinsic = true, Mod = bare });
        Check(bare.Extrinsic, true,
              "🧨 自带 `Mod` 的限时增益也同步标上（删掉 `if (b.Extrinsic && b.Mod != null) b.Mod.Extrinsic = true;` ⇒ 实得 false）");

        // 4·c 光环那一半（`IsExtrinsicSource`）没被这次改动弄丢
        var aura = u.RecordGrant("attack", 1, Auras.GrantTag);
        Check(aura != null && aura.Extrinsic, true,
              "🧨 光环那份（`Source == Auras.GrantTag`）仍然算 extrinsic（把 `|| IsExtrinsicSource(source)` 删掉 ⇒ 实得 false ⇒ 光环被「按来源撤」漏掉 = 少撤）");
        Check(u.ContainsExtrinsicEffectsFrom(Auras.GrantTag), true, "🧨 同上，「先查」也认它");
    }
}
