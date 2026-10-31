// B111_E1_Assert.cs — `CardScript.AddEffect` 的两道 extrinsic 闸（原版 `N29` / 活状态表 `B-06d`）
//
// ============================ 它盯的是什么 ============================
// ① **闸② jam × extrinsic**（原版 `CardScript__AddEffect.c:85-86` ＝ ILSpy `CardScript.cs:17457-17460`）：
//    `if (newEffect.enchantingCard.HasCurrentTrait(DefinedTrait.jam) && newEffect.extrinsic) return false;`
//    我们早就有一份逐字实现（`RuleCore.AddEffectCasterNotJammed`），但**调用点把 `extrinsic` 实参写死成
//    `false`** ⇒ 那一闸**永远不触发**。本批改成读作用域量 `ctx.ResolvingExtrinsic`
//    （唯一写点 = `RuleCore.ResolveHandBuffOps`）。A 段就是钉这件事。
// ② **extrinsic 的去重 / 替换**（原版 `CardScript__AddEffect.c:91-150`）：
//    固定型（`extrinsicVariable == 0`）同源同档已有 ⇒ **拒**；变量型且值变了 ⇒ **先整份摘掉同源的再挂**；
//    变量型且值没变（`IsSameEffect`）⇒ 拒。落点 = `RuleCore.ExtrinsicAddEffectGate`
//    （`EffectResolverPartial_B111E1.cs`），调用点 = `EffectResolver.ApplyOneGain` 第一句。B / C 段钉它。
//
// ============================ 怎么接 ============================
// `RuleEngineTest` 的 `Run()` 里加一句 `B111E1();`（零行数变化：追加在同一物理行、且在第一个 `//` 之前）。
// ⚠️ 入口**故意不叫** `Run` / 不带 `Test` 前缀（同 `B110_C09_Assert` / `B107_W19_Assert`）。
// ⚠️ **本件由 `B111_E1` 交付时【未接线】**（断言宿主 `Editor/RuleEngineTest.cs` 在本批的禁改清单里）
//    ⇒ 报告 `报告/B111_E1.md` §四 已如实写明「几条断言 / 未接线」。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · 把 `AddEffectCasterGates(..., ctx.ResolvingExtrinsic, ...)` 改回写死 `false`
//    ⇒ **A1** 红（A2 仍绿 —— A2 就是「非 extrinsic 照挂」那一半，两条必须同时成立才说明闸门是 extrinsic）。
//  · 把 `ExtrinsicAddEffectGate` 从 `ApplyOneGain` 撤掉、或让它恒 `return true`
//    ⇒ **B2** 红（同源固定型不再被拒）。
//  · 让它恒 `return false` ⇒ **B3** / **B4** / **C4** 红（不同属性 / 非 extrinsic / 没有同源记录都被误伤）。
//  · 把「变量型替换」写成「直接再挂一条」⇒ **C3'** 红（旧的那条必须被摘掉，剩下 0 条）。
//  · 把 `ExtrinsicCasterName` 从 `ApplyOneGain` 的 `source:` 撤掉 ⇒ **B1'** 红（记录带不回施放者，
//    B2 的「找到同源」也就无从谈起）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由宿主 `Run()` 里一句 `B111E1();` 调；⛔ 不叫 `Run` / 不带 `Test` 前缀）。</summary>
    public static void B111E1()
    {
        Section("🆕 B111_E1：`AddEffect` 的 extrinsic 两道 —— 闸② jam×extrinsic（`:85-86`）· 去重/替换（`:91-150`）");
        B111E1JamGate();
        B111E1Dedupe();
        B111E1VariableReplace();
        B111E1OathManaGate();
    }

    // ==================================================================
    //  D · 誓约闸把**卡费**算进去（活状态表 `B-06f`；原版 `.c:142-148` 的 `ManaLeft` 是扣完卡费的量）
    // ==================================================================
    static void B111E1OathManaGate()
    {
        // 原版：`iVar2 = BoardAnalysis.GetManaLeft(...)`（= `PlayerManager.GetUnusedMana`，**未用能量**）
        //       `iVar5 = GetDefaultTraitValue(oath)`；`if (iVar5 <= iVar2) return 1;`
        //   而两个调用点（`_d__447:498/:728`）都排在 `:377 PayCardCost`（→ `UseMana(CurrentCost)`）**之后**
        //   ⇒ 原版判的是「**基础卡费已经扣掉**的余量 ≥ 誓约值」。
        //   我们在 `PlayAsks`（出牌之前）问 ⇒ 等价式 = `Energy − 卡费 ≥ 誓约值`。
        var ctx = B111E1Ctx();
        ctx.Active = 0;

        var card = new CardDef("B111E1OathCard", "B111E1OathCard", "tactic", "", null, "Test",
                               3 /* cost */, 0, 0, 0, null,
                               choicesTargetsAffected: 180 /* = 选池轴，过闸① */);
        card.AttachCardTraits(new[] { "oath 2" });
        CheckTrue(RuleCore.HasOathTrait(card, out _), "D0 （前提）夹具卡带 oath=2（`cardTraits` = `oath 2`）");
        Check(card.GetDefaultTraitValue(KeywordTable.Oath, out _), 2, "D0 （前提）誓约值读出来是 2");
        Check(card.Cost, 3, "D0 （前提）卡费 = 3");

        // D1 · 4 能量 / 卡费 3 / 誓约 2 ⇒ 扣完卡费只剩 1 ⇒ **付不起** ⇒ 不问
        ctx.Players[0].Energy = 4;
        CheckTrue(!RuleCore.NeedsToChooseFromPool(ctx, 0, card),
                  "D1 ★★ `4 能量 · 卡费 3 · 誓约 2` ⇒ **不问**（扣完卡费只剩 1 < 2）"
                + "｜🧨 把闸改回 `Energy >= oath`（不减卡费）⇒ 4 ≥ 2 成立 ⇒ 本条红（这就是原版那个「少减一个卡费」）");

        // D2 · 5 能量 ⇒ 扣完卡费正好 2 ⇒ 付得起 ⇒ 问（证明 D1 不是「恒不问」）
        ctx.Players[0].Energy = 5;
        CheckTrue(RuleCore.NeedsToChooseFromPool(ctx, 0, card),
                  "D2 ★ 5 能量 ⇒ 扣完卡费 2 ≥ 2 ⇒ **问**（D1 不是恒不问）");

        // D3 · 免费卡（卡费 0）⇒ 退化成老口径：2 能量就够
        var free = new CardDef("B111E1OathFree", "B111E1OathFree", "tactic", "", null, "Test",
                               0, 0, 0, 0, null, choicesTargetsAffected: 180);
        free.AttachCardTraits(new[] { "oath 2" });
        ctx.Players[0].Energy = 2;
        CheckTrue(RuleCore.NeedsToChooseFromPool(ctx, 0, free),
                  "D3 ★ 卡费 0 ⇒ `Energy(2) − 0 ≥ 誓约(2)` ⇒ 问（减法不会把免费卡误伤）");

        // D4 · 按份的费用修正也吃得准：把「这一份」降到 1 费 ⇒ 4 能量就该付得起（3 费那一版付不起）
        // ⚠️ D3 把能量改成了 2，这里必须复原成 4（D4/D5 的文案都按 4 算）
        ctx.Players[0].Energy = 4;
        var inst = ctx.NewInstance(card);
        CheckTrue(!RuleCore.NeedsToChooseFromPool(ctx, 0, card, inst: inst),
                  "D4 （前提）还没加减费时，按份查询与模板查询一样判否");
        ctx.CostMods.Add(new CostMod { Player = 0, Key = card.Id, Delta = -2, ExpireTurn = -1 });
        CheckTrue(RuleCore.NeedsToChooseFromPool(ctx, 0, card, inst: inst),
                  "D5 ★ 按份费用修正生效后（现价 1 费）⇒ 4−1=3 ≥ 2 ⇒ 问"
                + "｜🧨 把 `inst` 从闸里摘掉（只按模板算 3 费）⇒ 本条红");
    }

    // ==================================================================
    //  A · 闸②：`源带 jam ∧ 本效果 extrinsic ⇒ 不收`（原版 `AddEffect.c:85-86`）
    // ==================================================================
    static void B111E1JamGate()
    {
        var ctx = B111E1Ctx();
        var jammer = B111E1Place(ctx, 0, 3, B111E1Unit("B111E1Jammer"));
        jammer.AddKeyword(KeywordTable.Jam, 1);
        CheckTrue(jammer.Has(KeywordTable.Jam), "A0 （前提）施放者身上真的带 jam（`DefinedTrait.jam = 130`）");

        var victim = B111E1Place(ctx, 1, 3, B111E1Unit("B111E1Victim"));
        var ops = EffectText.Parse("Give +1 Attack to an enemy troop", out _, out _);
        CheckTrue(ops != null && ops.Count > 0, "A0 （前提）`Give +1 Attack to an enemy troop` 解析出了 op");
        if (ops == null || ops.Count == 0) return;

        // ⚠️ 施放者走 `ResolveOps` 的第 3 个实参（`source`）：`ResolveOne` 会把它写进 `ctx.ActingUnit`
        //    （`EffectResolver.cs:823`）并在这里面**找不到第二个口** —— 所以**别**在调用前手工写 `ctx.ActingUnit`，
        //    那会被 `ResolveOneCore` 之前的保存/赋值覆盖掉（A3 那条就是专门验「`source = null` ⇒ 没有施放者」）。
        ctx.ResolvingExtrinsic = true;

        RuleCore.ResolveOps(ctx, 0, jammer, ops, by: "B111E1-A1", chosen: victim);
        Check(B111E1CountMods(victim, "attack"), 0,
              "A1 ★★ `源带 jam ∧ 本效果 extrinsic` ⇒ **不加**（原版 `CardScript__AddEffect.c:85-86`）"
            + "｜🧨 把 `AddEffectCasterGates(..., ctx.ResolvingExtrinsic, ...)` 改回写死 `false` ⇒ 本条红");

        // ★ 灭自证：**同一条 effect**，只要不是 extrinsic ⇒ 闸② 不管 ⇒ 照挂。
        //    这两条必须**同时**成立才说明闸门真的是 `extrinsic` 这一位（而不是「jam 一律不收」）。
        ctx.ResolvingExtrinsic = false;
        RuleCore.ResolveOps(ctx, 0, jammer, ops, by: "B111E1-A2", chosen: victim);
        Check(B111E1CountMods(victim, "attack"), 1,
              "A2 ★★ **灭自证**：非 extrinsic ⇒ 同一张**带 jam 的施放者**挂的同一句效果**照挂**"
            + "（闸② 的闸门就是 `extrinsic` 这一位，⛔ 不是「jam 一律不收」）");

        // A3：施放者为 null ⇒ 原版那段 `if (enchantingCard != null)` 整段跳过 ⇒ 照挂。
        ctx.ResolvingExtrinsic = true;
        var victim2 = B111E1Place(ctx, 1, 5, B111E1Unit("B111E1VictimA3"));
        RuleCore.ResolveOps(ctx, 0, null, ops, by: "B111E1-A3", chosen: victim2);
        Check(B111E1CountMods(victim2, "attack"), 1,
              "A3 没有施放者对象 ⇒ 原版 `newEffect.enchantingCard != null` 不成立、整段跳过 ⇒ 照挂");
    }

    // ==================================================================
    //  B · 固定型 extrinsic：同源同档已有 ⇒ 拒（原版 `.c:97-101`）
    // ==================================================================
    static void B111E1Dedupe()
    {
        var ctx = B111E1Ctx();
        var caster = B111E1Place(ctx, 0, 3, B111E1Unit("B111E1CasterB"));
        var victim = B111E1Place(ctx, 1, 3, B111E1Unit("B111E1VictimB"));
        var other = B111E1Place(ctx, 1, 5, B111E1Unit("B111E1VictimB2"));
        ctx.ResolvingExtrinsic = true;

        var atk = EffectText.Parse("Give +1 Attack to an enemy troop", out _, out _);
        var hp = EffectText.Parse("Give +1 Health to an enemy troop", out _, out _);
        if (atk == null || atk.Count == 0 || hp == null || hp.Count == 0)
        {
            CheckTrue(false, "B0 （前提）两句夹具效果都解析出了 op —— 否则本节不成立");
            return;
        }

        // ⚠️ `source` = 施放者（`ResolveOne` 会把它写进 `ctx.ActingUnit`，见 A 段那段说明）。
        RuleCore.ResolveOps(ctx, 0, caster, atk, by: "B111E1-B1", chosen: victim);
        Check(B111E1CountMods(victim, "attack"), 1, "B1 第一次挂上（extrinsic +1 攻）");
        CheckTrue(B111E1HasExtrinsicFrom(victim, caster.Name),
                  "B1' ★ 记录带回了**施放者卡名** + `Extrinsic` —— 没有这一格，`:91-150` 的「同源」就无从判起"
                + "｜🧨 把 `ApplyOneGain` 里 `source: ExtrinsicCasterName(ctx) ?? by` 撤掉 ⇒ 本条红");

        RuleCore.ResolveOps(ctx, 0, caster, atk, by: "B111E1-B2", chosen: victim);
        Check(B111E1CountMods(victim, "attack"), 1,
              "B2 ★★ 同一施放者的**固定型** extrinsic 再挂一次 ⇒ **拒**、不叠加（原版 `.c:97-101`：`!extrinsicVariable ⇒ return false`）"
            + "｜🧨 撤掉 `ExtrinsicAddEffectGate` 的调用、或让它恒真 ⇒ 本条红");

        RuleCore.ResolveOps(ctx, 0, caster, hp, by: "B111E1-B3", chosen: victim);
        Check(B111E1CountMods(victim, "health"), 1,
              "B3 ★ 同一施放者、**不同属性** ⇒ 照挂（闸按 `(buffType, 属性名)` 收敛，⛔ 不误伤多增量子句）");

        // ★ 灭自证：非 extrinsic ⇒ 同一句挂两次**就是两条**（证明 B2 不是「反正只会有 1 条」）。
        ctx.ResolvingExtrinsic = false;
        RuleCore.ResolveOps(ctx, 0, caster, atk, by: "B111E1-B4", chosen: other);
        RuleCore.ResolveOps(ctx, 0, caster, atk, by: "B111E1-B5", chosen: other);
        Check(B111E1CountMods(other, "attack"), 2,
              "B4 ★★ **灭自证**：非 extrinsic ⇒ 同一施放者挂两次**就是两条**（闸只对 extrinsic 生效）");
    }

    // ==================================================================
    //  C · 变量型 extrinsic：值没变 ⇒ 拒；值变了 ⇒ 先整份摘同源再挂（原版 `.c:112/144/147`）
    // ==================================================================
    static void B111E1VariableReplace()
    {
        var ctx = B111E1Ctx();
        var caster = B111E1Place(ctx, 0, 3, B111E1Unit("B111E1CasterC"));
        var victim = B111E1Place(ctx, 1, 3, B111E1Unit("B111E1VictimC"));
        ctx.ActingUnit = caster;      // C 段后半是**直调闸**（不过 `ResolveOps`）⇒ 这一格要自己写；
                                      // 走 `ResolveOps` 的那一句仍按第 3 个实参给（它内部 save/restore 回 caster）
        ctx.ResolvingExtrinsic = true;

        var atk = EffectText.Parse("Give +1 Attack to an enemy troop", out _, out _);
        if (atk == null || atk.Count == 0) { CheckTrue(false, "C0 （前提）夹具效果解析出了 op"); return; }
        RuleCore.ResolveOps(ctx, 0, caster, atk, by: "B111E1-C0", chosen: victim);
        Check(B111E1CountMods(victim, "attack"), 1, "C0 （前提）先有一条 `+1 攻` 的 extrinsic 记录");
        if (B111E1CountMods(victim, "attack") != 1) return;

        // C1 · 变量型 + 值没变（`IsSameEffect` 为真）⇒ 拒，**且不摘**
        CheckTrue(!RuleCore.ExtrinsicAddEffectGate(ctx, victim, EffectsToShow.BuffTypeAddStats, "attack", 1, false, true, "B111E1-C1"),
                  "C1 ★ `IsSameEffect`（值一样）⇒ **拒**（原版 `.c:144`）");
        Check(B111E1CountMods(victim, "attack"), 1, "C1' 拒的时候**什么都不摘**（旧记录还在）");

        // C2 · 固定型 + 值不同 ⇒ 也是拒（固定型根本不看值）
        CheckTrue(!RuleCore.ExtrinsicAddEffectGate(ctx, victim, EffectsToShow.BuffTypeAddStats, "attack", 9, false, false, "B111E1-C2"),
                  "C2 ★ 固定型 + 值不同 ⇒ 仍然**拒**（原版 `.c:97`：`!extrinsicVariable` 先于一切值比较）");
        Check(B111E1CountMods(victim, "attack"), 1, "C2' 同上，旧记录还在");

        // C3 · 变量型 + 值变了 ⇒ 允许，并且**先整份摘掉同源旧记录**
        CheckTrue(RuleCore.ExtrinsicAddEffectGate(ctx, victim, EffectsToShow.BuffTypeAddStats, "attack", 5, false, true, "B111E1-C3"),
                  "C3 ★★ 变量型 + 值变了 ⇒ **允许重挂**（原版 `.c:147` 那一路）");
        Check(B111E1CountMods(victim, "attack"), 0,
              "C3' ★★ 同源旧记录被**整份摘掉**（原版 `RemoveExtrinsicEffectsFrom(enchantingCard)`）"
            + "｜🧨 把「替换」写成「直接再挂一条」⇒ 本条红（会剩 1 条旧的）");

        // C4 · 摘干净之后没有同源记录 ⇒ 闸放行
        CheckTrue(RuleCore.ExtrinsicAddEffectGate(ctx, victim, EffectsToShow.BuffTypeAddStats, "attack", 5, false, false, "B111E1-C4"),
                  "C4 ★ 没有同源记录 ⇒ 照挂（闸只在「已有同源」时才动手）");

        // C5 · 不是 extrinsic 的解析期 ⇒ 整个闸不介入（原版整段包在 `if (effect.extrinsic)` 里）
        ctx.ResolvingExtrinsic = false;
        CheckTrue(RuleCore.ExtrinsicAddEffectGate(ctx, victim, EffectsToShow.BuffTypeAddStats, "attack", 5, false, false, "B111E1-C5"),
                  "C5 ★ 非 extrinsic ⇒ 闸恒放行（原版那一段整段包在 `if (newEffect.extrinsic)` 里）");
    }

    // ==================================================================
    //  夹具（✅ 名字全部带 `B111E1` 前缀：本类是全仓共用的 `partial class RuleEngineTest`，
    //        ⛔ 别用 `Unit` / `Place` 这种通用名 —— 会和别的断言件撞签名）
    // ==================================================================
    static BattleContext B111E1Ctx()
    {
        var a = new List<CardDef> { B111E1Warlord("B111E1Hero0") };
        var b = new List<CardDef> { B111E1Warlord("B111E1Hero1") };
        return RuleCore.NewBattle(a, b, seed: 0, shuffle: false);
    }

    static CardDef B111E1Warlord(string id)
        => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0, null, subtype: "Warlord");

    static CardDef B111E1Unit(string id)
        => new CardDef(id, id, "unit", "", null, "Test", 1, 0, 6, 0, null);

    /// <summary>白盒放盘（`RuleEngineTest.Place` 的等价物；⛔ 不走棋盘钩子 —— 本件不验光环重算）。</summary>
    static UnitState B111E1Place(BattleContext ctx, int seat, int slot, CardDef def)
    {
        var u = new UnitState(ctx.NewInstance(def), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[seat].Board[slot] = u;
        return u;
    }

    /// <summary>这个单位身上有几条**这个属性**的修饰记录（`StatMod` 那本账）。</summary>
    static int B111E1CountMods(UnitState u, string attr)
    {
        int n = 0;
        var mods = u.StatMods;
        for (int i = 0; i < mods.Count; i++)
            if (mods[i] != null && mods[i].Attr == attr) n++;
        return n;
    }

    /// <summary>这个单位身上有没有「施放者 = <paramref name="caster"/> ∧ extrinsic」的修饰记录
    /// （= `:91-150` 找同源记录的那个键；B1' 用它证明「施放者真的被存下来了」）。</summary>
    static bool B111E1HasExtrinsicFrom(UnitState u, string caster)
    {
        var mods = u.StatMods;
        for (int i = 0; i < mods.Count; i++)
        {
            var m = mods[i];
            if (m != null && m.Extrinsic && m.Source == caster) return true;
        }
        var traits = u.TraitEffects;
        for (int i = 0; i < traits.Count; i++)
        {
            var r = traits[i];
            if (r != null && r.Extrinsic && r.Source == caster) return true;
        }
        return false;
    }
}
