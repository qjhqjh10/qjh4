// B141_W65_Assert.cs — 批次 **B141** · 执行写手 `W65`（CHK-05 战斗域：账 `战斗逻辑驱动` L336 / L77）
//
// ============================ 它盯的是什么 ============================
//   A 组（账 **L336** · `BattleManager.GetCardsInCemetery` 的**残骸闸**）：
//     「残骸被摧毁」进墓的那张，原版 `BattleManager__GetCardsInCemetery.c:60` **无条件排除**
//     （判据 = `CardScript +0x65 isRemnant` 在进墓时不清 ⇒ 那张带着 `isRemnant=1`；见 `DeadUnit.IsRemnant` 的 doc）。
//     本仓等价物 = `RuleCore.GetCardsInCemetery` 的⑥那一道（`RuleCore.cs:15964` 的 `remnantIds`）。
//     两条断言口：**候选口** `CemeteryDeadUnitsForTest`（= `ReturnFromCemetery` / `DoDeployOnce` 的共用适配器）
//     与**消费端整条路** `ReturnFromCemeteryForTest`。
//     🔴 2026-11-04 `W65` 现读：`ReturnFromCemetery` 已于 2026-10-29 `B98_K4` 换源到墓地那一口 ⇒
//        **账上写的「残骸闸未生效」已过期**（本组就是钉住它的回归）。
//   B 组（账 **L77** · `AbilityLogic.GetValueFromCriteria` 的 `owner` 递参）：
//     80 档（`min + max × multiplierFactor`）**必须拿到 `owner`** 才求得出值；
//     调用点 `EffectResolver.ResolveOneCore:840` 现已传 `ApplyRefCardValueOption(ctx, op, card, owner, chosen)`
//     （2026-11-03 提交 995da8d5）。本组钉「递了 owner ⇒ 值出来 / 没递 ⇒ 退回文本值」这条判别式。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2/A3/A4：把 `RuleCore.GetCardsInCemetery` 的 `remnantIds` 那一段删掉（或 `IsRemnant` 写成 false）
//     ⇒ 残骸那张重新出现在候选里 ⇒ 三条一起红。
//   · A1：把候选换回「手扫 `ctx.DeadUnits`」⇒ 换成残骸那一组仍然 1 张（A2 变 1）⇒ 红。
//   · A0'：**同一份夹具**（A 组 vs A2 组逐字相同、只差 `IsRemnant` 这一位）+ 「它确实在 `Discard` 里」
//     ⇒ 排除**不可能**是「它压根不在墓地 / 兵种被筛掉」造成的。
//   · B2：把 `ApplyRefCardValueOption(..., owner, chosen)` 的 `owner` 改成 `-1`（= 调用点不递参）
//     ⇒ 值退回文本 2 ⇒ B1 红。B3 是把这条判别式本身钉死。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里**同一物理行追加**（落在该行第一个 `//` 之前）：
//     `Guard("B141W65_Assert", B141_W65_Assert.Run); _pass += B141_W65_Assert.Pass; _fail += B141_W65_Assert.Fail;`
//   （写法与 `B141_W22_Assert` / `B141_W15_Assert` 同形；⚠️ 接线由**调度台**做 —— 本批白名单不含宿主。）
//
// ⚠️ 本文件**不建 GameObject、不建资产、不写盘**；只用 `RuleCore.NewBattle` 的内存局面。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B141-W65 的断言（独立类：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B141_W65_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B141-W65：L336 墓地残骸闸（`GetCardsInCemetery` ⑥）/ L77 `GetValueFromCriteria` 的 owner 递参");
        GroupA_RemnantGate();
        GroupB_OwnerWiring();
        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static CardDef Unit(string id) => new CardDef(id, id, "unit", "", null, "Test", 1, 1, 5, 0,
                                                 (IEnumerable<string>)null, subtype: "Infantry");

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B141W65Warlord0") };
        var d1 = new List<CardDef> { Hero("B141W65Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    /// <summary>`friendly troop` 那一条目标短语（与 `B98K4_Assert.K4Spec` 同口径：`Kind = "troop"` = 单位）。</summary>
    static EffectTargetSpec TroopSpec()
        => new EffectTargetSpec { Raw = "your troop", Side = "own", Kind = "troop", Count = 1 };

    static EffectOp ReturnOp(EffectTargetSpec spec)
        => new EffectOp
        {
            Verb = "return", Source = "B141W65Fixture", Dest = "decktop", Amount = 0,
            Target = spec, SubjectTargetsAffected = 60,
        };

    /// <summary>造一份「已进墓地」的单位卡：实例进 `Discard`，并按 <paramref name="isRemnant"/> 写一条阵亡登记。
    /// ⚠️ `Discard`（墓地列表）**两组逐字相同** —— 唯一的差别就是那个 `IsRemnant` 位。</summary>
    static CardInstance CemeteryUnit(BattleContext ctx, string id, bool isRemnant)
    {
        var card = Unit(id);
        var inst = ctx.NewInstance(card);
        ctx.Players[0].Discard.Add(inst);
        ctx.DeadUnits.Add(new DeadUnit
        {
            Card = card, Instance = inst, Owner = 0, DeathTurn = ctx.Turn, IsRemnant = isRemnant,
        });
        return inst;
    }

    // ==================================================================
    //  A 组：L336 —— 残骸闸（原版 `BattleManager__GetCardsInCemetery.c:60`）
    // ==================================================================
    static void GroupA_RemnantGate()
    {
        Section("A（L336）墓地候选的**残骸闸**：`isRemnant` 为真的那张查不出来（原版 `.c:60` 无条件排除）");
        var spec = TroopSpec();

        // ---- A 组：普通阵亡（`IsRemnant = false`）⇒ 三个查询口都读得到、消费端搬得走 ----
        var ctx = NewCtx();
        var normal = CemeteryUnit(ctx, "B141W65_DeadUnit", isRemnant: false);
        Check(ctx.Players[0].Discard.Contains(normal), true,
              "A0 ★ 前提：那张单位卡**在墓地列表（`Discard`）里**");
        Check(ctx.DeadUnits.Count, 1, "A0' ★ 前提：阵亡登记 1 条（`IsRemnant = false`）");
        Check(RuleCore.GetCardsInCemetery(ctx, true, true, true, true, true, false).Count, 1,
              "A1 ★ 查询口：`GetCardsInCemetery(全开、只看我方)` 读到 1 张（普通阵亡 ⇒ 照收）");
        Check(RuleCore.CemeteryDeadUnitsForTest(ctx, 0, spec, null).Count, 1,
              "A2 ★ 候选口：`CemeteryDeadUnits`（`ReturnFromCemetery`/`DoDeployOnce` 的共用适配器）读到 1 张");
        var movedA = new List<CardInstance>();
        int backA = RuleCore.ReturnFromCemeteryForTest(ctx, 0, "W65", ReturnOp(spec), spec, "all", movedA);
        Check(backA, 1,
              "A3 ★ 消费端整条路：`ReturnFromCemetery` 把它搬进牌库顶 1 张"
            + "（🧨 候选换回手扫 `DeadUnits` 这一条仍然绿 —— 真正的判别式在 A2' 那一组）");
        Check(ctx.Players[0].Deck.Contains(normal), true, "A3' 那一份实例真的进了牌库（`decktop`）");

        // ---- A 组'：**同一份夹具、只把 `IsRemnant` 翻成 true** ⇒ 三个查询口都读不到 ----
        var ctx2 = NewCtx();
        var rem = CemeteryUnit(ctx2, "B141W65_RemDeadUnit", isRemnant: true);
        Check(ctx2.Players[0].Discard.Contains(rem), true,
              "A0'' ★★ 灭自证前提：残骸那张**同样躺在墓地列表里**（`Discard.Contains` = true）"
            + " ⇒ 下面读不到**不是**「它压根不在墓地」也不是「兵种被筛掉」");
        Check(ctx2.DeadUnits.Count, 1, "A0''' 前提：阵亡登记 1 条（`IsRemnant = true` —— 与 A 组只差这一位）");
        Check(RuleCore.GetCardsInCemetery(ctx2, true, true, true, true, true, false).Count, 0,
              "A1' ★★ 查询口排除：`GetCardsInCemetery` = **0 张**（原版 `.c:60` 对 `isRemnant != 0` **无条件排除**）"
            + "｜🧨 删掉 `remnantIds` 那一段 ⇒ 实得 1 ⇒ 红");
        Check(RuleCore.CemeteryDeadUnitsForTest(ctx2, 0, spec, null).Count, 0,
              "A2' ★★ 候选口排除：`CemeteryDeadUnits` = **0 张**（残骸进不了「墓地候选」）"
            + "｜🧨 候选换回手扫 `ctx.DeadUnits` ⇒ 实得 1 ⇒ 红");
        var movedB = new List<CardInstance>();
        int backB = RuleCore.ReturnFromCemeteryForTest(ctx2, 0, "W65", ReturnOp(spec), spec, "all", movedB);
        Check(backB, 0,
              "A3'' ★★ 消费端整条路：`ReturnFromCemetery` 搬 **0 张**（残骸不会被当成「本局死过的部队」捞回来）"
            + "｜🧨 闸没落地（账上旧文说的那样）⇒ 实得 1 ⇒ 红");
        Check(movedB.Count, 0, "A3''' 搬走表也是空的（0 张）");
        Check(ctx2.Players[0].Deck.Contains(rem), false, "A3'''' 那一份实例**没有**进牌库（原处未动）");
    }

    // ==================================================================
    //  B 组：L77 —— 80 档求值必须拿到 `owner`（`ResolveOneCore:840` 现在传了）
    // ==================================================================
    static void GroupB_OwnerWiring()
    {
        Section("B（L77）`ApplyRefCardValueOption` 的 80 档：**递了 `owner` 才求得出值**，没递 ⇒ 退回文本值");

        var card = ZeroMultCard("B141W65_Opt80", min: 1, max: 2, opt: 80);
        var spec = card.AbilityDamageAt(0, 0);
        CheckTrue(spec != null && spec.ValueOptions == 80,
                  "B0 ★ 前提：夹具卡的 `abilityDamage` 挂上了（`ValueOptions = 80`、`min = 1`、`max = 2`）");
        if (spec == null) return;

        var ctx = NewCtx();
        var op = new EffectOp { Verb = "deal", Source = card.Name, Amount = 2, AbilityIndex = 0, LogicIndex = 0 };

        // B1 ★★ 递了 owner ⇒ 走 80 档求值（`multiplier.targetsAffected = 0 (any)` ⇒ 倍率 1 ⇒ 1 + 2×1 = 3）
        var got = RuleCore.ApplyRefCardValueOption(ctx, op, card, 0, null);
        Check(got.Amount, 3,
              "B1 ★★ 递 `owner = 0` ⇒ 80 档求出 `min + max × 1 = 3`（`Amount`）"
            + "｜🧨 `ResolveOneCore` 那句不递 `owner` ⇒ 退回文本值 2 ⇒ 红");
        Check(got.AmountMax, 3, "B1' 定值：`AmountMax` 同步 = 3（原版这三档不掷骰 ⇒ 不是区间）");
        Check(op.Amount, 2,
              "B1'' 原件不被改（`EffectOp` 是解析产物、会被复用 ⇒ 只改 `Clone()`）");

        bool said = false;
        foreach (string e in ctx.Events)
            if (e != null && e.Contains("min + max × 目标数")) said = true;
        CheckTrue(said, "B2 ★ 日志说得出「`min + max × 目标数`」（= 值走的是 80 档那一支，不是文本兜底）");

        // B3 ★★ 灭自证：**没递 owner**（`-1`）⇒ 原样返回、值停在文本 2 ⇒ 上面 B1 的「3」只可能来自
        //     调用点真的把 `owner` 递了下来。
        var ctxNoOwner = NewCtx();
        var gotNoOwner = RuleCore.ApplyRefCardValueOption(ctxNoOwner, op, card, -1, null);
        Check(gotNoOwner.Amount, 2,
              "B3 ★★ 灭自证：`owner = -1`（= 调用点没递参）⇒ 值**原样** 2、不求出 3"
            + "（这条钉住的正是账 L77 的①「调用点未传 `owner`」—— 今天它已传）");
        bool gapSaid = false;
        foreach (string e in ctxNoOwner.Events)
            if (e != null && e.Contains("没递 `owner`")) gapSaid = true;
        CheckTrue(gapSaid, "B3' 没递 `owner` 时会**出声**（不许静默退回文本值 —— 工程红线：不许静默失败）");
    }

    /// <summary>一张「Rally + 一条 `mult role` 行」的夹具单位；`opt` = `damageCriteria.valueOptions`。
    /// `multiplier.targetsAffected = 0 (any)` ⇒ 原版 `PlayAbility.c:190` 的倍率 = 1（不依赖手牌/棋盘夹具）。</summary>
    static CardDef ZeroMultCard(string id, int min, int max, int opt)
    {
        var mult = new CardDef.CountSetSpec { TargetsAffected = 0 };
        var conds = new[]
        {
            new CardDef.ConditionSpec { Ability = 0, Logic = 0, Role = "mult", Condition = 0, MultiplierSet = mult },
        };
        var card = new CardDef(id, id, "unit", "Deal 2 damage to an enemy troop", "common", "Test",
                               1, 0, 9, 0, new[] { "Rally" }, abilityConditions: conds);
        card.AttachCardTraits(new string[0]);      // 空表 = 「已挂、只是没有 trait」（`null` 会让卡面级 criteria 判不了）
        card.AttachAbilityDamage(new[]
        {
            new CardDef.AbilityDamageSpec { Ab = 0, Lg = 0, Min = min, Max = max, ValueOptions = opt, Type = 0 },
        });
        return card;
    }

    // ==================================================================
    //  口令 / 汇总
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B141_W65 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B141_W65 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
