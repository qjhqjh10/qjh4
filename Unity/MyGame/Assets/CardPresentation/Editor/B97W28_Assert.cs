// B97W28_Assert.cs — 执行代理 `B97_W28`（第九十七会话 · 波 1 补位）的断言
//
// ============================ 它盯的是什么 ============================
//   `RuleEngine/Core/RuleCore.cs` 本笔的五件：
//     ① 账 `:867` ①  **誓约费 = trait 1275 的数值**（⛔ 不是卡面文本、更不是 `ActiveAbilityManaCost`）。
//     ② 账 `:1288` ①  `FireTriggerAt` 的 `opsOverride` 形参 —— 传表时**按表结算**，不传时逐字不变。
//     ③ 账 `:871` ③  `CardScript.ResolveDeadCard.c:233` 的两道闸（残骸档 + `alreadyTriggeredBacklash`）。
//     ④ 账 `特效动画:503` 猎杀标记数走 `EvtKind.Death` 的 `Amount`（发侧；表现侧归 `B97_W18`）。
//     ⑤ 账 `:517` ② `FireTriggerByNumber`（按 trigger 数值取正文并结算）的执行口。
//
// 判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/报告/B97_W28.md` §一 ＋
//   `decomp_full/BattleManager__PayActiveAbilityCostOath.c:24` ·
//   `decomp_full/EntityScript__GetCurrentTraitValue.c` ·
//   `decomp_full/CardScript__ResolveDeadCard.c:233` ·
//   `decomp_full/CardScript__TriggerOnMinionDeath.c:33-95` ·
//   `decomp_full/CardScript__FollowUpResolutionPlayed.c:7`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A（誓约费）：把 `AbilityCostGate` 的 oath 分支改成读 `CardDef.ActiveAbilityManaCost` ⇒ A3 红
//                （那张卡的 manaCost = 0 ⇒ 能量 2 也放行）。把闸改回读卡面文本 ⇒ A2 红
//                （文本写 1、数据面写 4，能量 3 时会被放行）。两条**结构上不可能同时满足**。
//   · B（opsOverride）：把 `var ops = opsOverride ?? (...)` 里的 `opsOverride ??` 去掉 ⇒ B2/B2' 红
//                （卡上没 `Strike` 正文 ⇒ 仍返 false、护甲仍是 0）；把 `opsOverride` 变成恒 `null` 同理。
//   · C（死亡两闸）：从 `DeathObserverGateAllows` 里删掉 `AlreadyTriggeredBacklash` 那一句 ⇒ C2 红；
//                把它写成 `if (dead.IsRemnant) return false;`（漏掉 `|| cardState == 2`）⇒ C4 红；
//                把 `CleanupDeaths` 里那四处 `deathTriggersAllowed &&` 去掉 ⇒ C2 红。
//   · D（猎杀标记）：把 `amount: _dhunt` 去掉 ⇒ D2 红（实得 0）；把它写成常量 ⇒ D2 也红。
//   · E（按数值取正文）：把 `FireTriggerByNumber` 里的 `TriggerOpsByNumber` 换成读卡面关键词 ⇒ E2/E2' 红。
//
// ============================ 怎么用（宿主）============================
//   `RuleEngineTest.Run()` 里插一行：`Step(TestB97W28);`
//   （本文件的类 = `RuleEngineTest` 的**分部**，这样才复用得上夹具 `Battle` / `ProbeBattle` / `Place` /
//    `ToP1Turn` / `Check` —— ⛔ 另起一个类会把那套夹具抄第二份，违反「一处判据」。）
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

public static partial class RuleEngineTest
{
    /// <summary>B97_W28 的全部断言（宿主：`RuleEngineTest.Run` 里 `Step(TestB97W28);`）。</summary>
    public static void TestB97W28()
    {
        Section("B97_W28 —— 账 867 / 1288 / 871 / 特效动画 503 / 517");

        TestB97W28OathCostByTrait();
        TestB97W28OpsOverride();
        TestB97W28DeathObserverGate();
        TestB97W28HuntMarkOnDeathSignal();
        TestB97W28FireTriggerByNumber();
    }

    /// <summary>夹具：一张带 `Oath N:` 正文的单位卡。
    /// <para><paramref name="cardTraits"/> 非 `null` ⇒ 挂上数据面那条 trait（= 真卡表走过的路）；
    /// `null` ⇒ **不挂**（= `new CardDef(...)` 的老夹具 / 旧卡表，`CardTraitsKnown == false`）。</para></summary>
    static CardDef W28OathCard(string id, string desc, string[] cardTraits, int? abilityManaCost)
    {
        var c = new CardDef(id, id, "unit", desc, "common", "Ultramarines", 1, 2, 5, 0,
                            new[] { KeywordTable.Oath }, subtype: "Infantry");
        if (cardTraits != null) c.AttachCardTraits(cardTraits);
        if (abilityManaCost.HasValue) c.AttachActiveAbilityCost(abilityManaCost.Value, 0, 0, 0);
        return c;
    }

    // ==============================================================================================
    //  ① 账 `:867` —— **誓约费读 trait 1275**（`0x4fb`），⛔ 不是卡面文本、⛔ 不是 `ActiveAbilityManaCost`
    // ==============================================================================================
    static void TestB97W28OathCostByTrait()
    {
        // ---- A：两路读数相同的那种卡（真卡池 28 张里 28 张都是这样）----
        var cA = W28OathCard("W28OathA", "Oath 3: Gain Armour 1", new[] { "oath 3" }, null);
        CheckTrue(cA.CardTraitsKnown, "A0 （前提）`cardTraits` 已挂上 —— 否则走的是文本兜底那一路，A1 会变成空转");
        Check(cA.OathCost, 3, "A0 （前提）卡面文本 `Oath 3:` 解出 `OathCost = 3`");

        var ctxA = Battle(new[] { cA }, new CardDef[0]);
        ToP1Turn(ctxA, 1);
        var uA = Place(ctxA, 0, 3, cA, exhausted: true);
        Check(RuleCore.OathCostTrait1275ForTest(uA), 3,
              "★ A1 **誓约费 = trait 1275 的数值 3**（原版 `PayActiveAbilityCostOath.c:24` 的 "
            + "`EntityScript__GetCurrentTraitValue(card, 0x4fb)`）"
            + "｜🧨 改成读 `ActiveAbilityManaCost`（这张卡没挂 ⇒ `-1`）⇒ 实得 0 ⇒ 红");

        ctxA.Players[0].Energy = 2;
        CheckCode(RuleCore.CanUseOathAbility(ctxA, 0, 3), RuleCodes.ErrCost,
                  "★ A2 能量 2 < 3 ⇒ **拒绝**（`ErrCost`；原版 `HasEnoughMana` 那一跳，⛔ 不是「点得出来」）");
        ctxA.Players[0].Energy = 3;
        CheckCode(RuleCore.CanUseOathAbility(ctxA, 0, 3), RuleCodes.OK,
                  "★ A2' 能量 3 ⇒ 放行（边界取等号）");

        // ---- A3：**灭自证 · 数据面有、文本也有、但 `ActiveAbilityManaCost` 是 0** ----
        //   这张卡的 manaCost = 0（原版 `ActiveAbility.manaCost` 的合法值），若有人把誓约费切成那一格，
        //   `cost <= 0` ⇒ 恒放行 ⇒ 能量 2 也会过。
        var cB = W28OathCard("W28OathB", "Oath 3: Gain Armour 1", new[] { "oath 3" }, abilityManaCost: 0);
        CheckTrue(cB.ActiveAbilityCostKnown, "A3 （前提）`ActiveAbilityManaCost` 那一格已知（= 0）");
        var ctxB = Battle(new[] { cB }, new CardDef[0]);
        ToP1Turn(ctxB, 1);
        Place(ctxB, 0, 3, cB, exhausted: true);
        ctxB.Players[0].Energy = 2;
        CheckCode(RuleCore.CanUseOathAbility(ctxB, 0, 3), RuleCodes.ErrCost,
                  "★★ A3 **灭自证**：`ActiveAbilityManaCost == 0` 时能量 2 **仍然要拒** "
                + "｜🧨 把誓约费切成 `ActiveAbilityManaCost` ⇒ 实得 `OK` ⇒ 红"
                + "（这就是施工单第一版那个「27 张 oath 卡静默免费」缺陷的判别式）");

        // ---- A4：**灭自证 · 数据面 vs 卡面文本不一致时，以数据面为准** ----
        var cC = W28OathCard("W28OathC", "Oath 1: Gain Armour 1", new[] { "oath 4" }, null);
        Check(cC.OathCost, 1, "A4 （前提）卡面文本写的是 `Oath 1`");
        var ctxC = Battle(new[] { cC }, new CardDef[0]);
        ToP1Turn(ctxC, 1);
        var uC = Place(ctxC, 0, 3, cC, exhausted: true);
        Check(RuleCore.OathCostTrait1275ForTest(uC), 4, "A4 （前提）数据面 trait 1275 = **4**");
        ctxC.Players[0].Energy = 3;
        CheckCode(RuleCore.CanUseOathAbility(ctxC, 0, 3), RuleCodes.ErrCost,
                  "★★ A4 文本 1 / 数据面 4 ⇒ **按 4 收**（能量 3 拒）"
                + "｜🧨 退回读卡面文本（1）⇒ 实得 `OK` ⇒ 红 —— 这一条钉的是「换源了没有」");
        ctxC.Players[0].Energy = 4;
        CheckCode(RuleCore.CanUseOathAbility(ctxC, 0, 3), RuleCodes.OK, "★ A4' 能量 4 ⇒ 放行");

        // ---- A5：**数据面缺失 ⇒ 退回文本那一路**（⛔ 不把「不知道」当免费）----
        var cD = W28OathCard("W28OathD", "Oath 3: Gain Armour 1", null, null);
        CheckTrue(!cD.CardTraitsKnown, "A5 （前提）这张**没挂** `cardTraits` ⇒ `CardTraitsKnown == false`");
        var ctxD = Battle(new[] { cD }, new CardDef[0]);
        ToP1Turn(ctxD, 1);
        Place(ctxD, 0, 3, cD, exhausted: true);
        ctxD.Players[0].Energy = 2;
        CheckCode(RuleCore.CanUseOathAbility(ctxD, 0, 3), RuleCodes.ErrCost,
                  "★★ A5 数据面判不了 ⇒ **退回文本那一路**（能量 2 仍拒）"
                + "｜🧨 把「判不了」当成 0 费 ⇒ 实得 `OK` ⇒ 红（那会让现有夹具与旧卡表全部静默免费）");
        ctxD.Players[0].Energy = 3;
        CheckCode(RuleCore.CanUseOathAbility(ctxD, 0, 3), RuleCodes.OK, "★ A5' 能量 3 ⇒ 放行");

        // ---- A6/A7：**psychic / plasma 两道币种闸**（原版 `CanUseActiveAbility.c:60-70`）----
        //   我们**只建模了 normal 那一池**（`PlayerState.Energy`）；卡表两列现读 87 张全 0 ⇒ 今天不可达。
        //   闸的语义 = 「判不了就**拒**并出声」，⛔ 不是静默放行。
        var cPsi = new CardDef("W28Psi", "W28Psi", "unit", "", "common", "Test", 1, 2, 5, 0,
                               null, subtype: "Infantry", hasActiveAbility: 1);
        cPsi.AttachActiveAbilityCost(0, 1, 0, 0);          // manaCost 0 · psychic 1
        CheckTrue(cPsi.ActiveAbilityCostKnown, "A6 （前提）主动能力费用四格已知");
        var ctxPsi = Battle(new[] { cPsi }, new CardDef[0]);
        ToP1Turn(ctxPsi, 1);
        var uPsi = Place(ctxPsi, 0, 3, cPsi);
        CheckTrue(uPsi.ActiveAbilityAvailable, "A6 （前提）`ActiveAbilityAvailable` 真（`hasActiveAbility = 1`）");
        CheckCode(RuleCore.CanStartActiveAbility(ctxPsi, 0, 3), RuleCodes.ErrCost,
                  "★★ A6 标了 **psychic 币种**（我们没建模那个池）⇒ **拒**并出声"
                + "｜🧨 把它当成 normal 池 / 静默放行 ⇒ 实得 `OK` ⇒ 红"
                + "（原版 `PlayerManager__HasEnoughMana.c:14-50` 是按三个 bool 在四个池里选一个）");

        var cNo = new CardDef("W28PsiNo", "W28PsiNo", "unit", "", "common", "Test", 1, 2, 5, 0,
                              null, subtype: "Infantry", hasActiveAbility: 1);
        cNo.AttachActiveAbilityCost(0, 0, 0, 0);           // 两个币种都 0
        var ctxNo = Battle(new[] { cNo }, new CardDef[0]);
        ToP1Turn(ctxNo, 1);
        var uNo = Place(ctxNo, 0, 3, cNo);
        CheckTrue(uNo.ActiveAbilityAvailable, "A7 （前提）`ActiveAbilityAvailable` 真");
        CheckCode(RuleCore.CanStartActiveAbility(ctxNo, 0, 3), RuleCodes.OK,
                  "★★ A7 灭自证：两个币种都是 0 ⇒ **不拦**（闸不是恒拒）");
    }

    // ==============================================================================================
    //  ② 账 `:1288` ① —— `FireTriggerAt(..., opsOverride)`：**传表 ⇒ 按表结算**，不传 ⇒ 逐字不变
    // ==============================================================================================
    static void TestB97W28OpsOverride()
    {
        var plain = new CardDef("W28Plain", "W28Plain", "unit", "", "common", "Test", 1, 1, 5, 0,
                                null, subtype: "Infantry");
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        var u = Place(ctx, 0, 3, plain);

        CheckTrue(!u.HasFx(KeywordTable.Strike),
                  "B0 （前提）这张卡**没有** `Strike` 正文 —— 否则下面「不传就 false」会被卡面正文顶上（假绿）");
        CheckTrue(!RuleCore.FireTriggerAtWithOverrideForTest(ctx, u, KeywordTable.Strike, 0, 3, null),
                  "★ B1 不传 override 且卡上没正文 ⇒ **不触发**（= 改前行为：`FireTriggerAt` 早退返 false）");

        var ops = EffectText.Parse("Gain Armour 2", out _, out _);
        CheckTrue(ops != null && ops.Count > 0, "B2 （前提）`Gain Armour 2` 解得出 ≥1 条 op");
        Check(u.Armor, 0, "B2 （前提）打之前护甲 0");

        CheckTrue(RuleCore.FireTriggerAtWithOverrideForTest(ctx, u, KeywordTable.Strike, 0, 3, ops),
                  "★★ B2 传了 override ⇒ **入队**（返 true）"
                + "｜🧨 把 `var ops = opsOverride ?? (...)` 里的 `opsOverride ??` 去掉 ⇒ 实得 false ⇒ 红");
        RuleCore.PumpActionQueues(ctx);
        Check(u.Armor, 2,
              "★★ B2' 泵完 ⇒ **按调用方给的那张表结算**（护甲 0 → 2）"
            + "｜🧨 同上 ⇒ 仍是 0 ⇒ 红（这一条是「数据半挂进来的表能不能真跑」的判别式，"
            + "`EffectResolver.TriggerOncePass` 那一跳就等这一格）");
    }

    // ==============================================================================================
    //  ③ 账 `:871` ③ —— `ResolveDeadCard.c:233` 两道闸：`(isRemnant || cardState == 2) && !alreadyTriggeredBacklash`
    // ==============================================================================================
    static void TestB97W28DeathObserverGate()
    {
        var watcher = new CardDef("W28Watcher", "W28Watcher", "unit",
                                  "When a friendly troop dies, gain +3 Attack",
                                  "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
        var fodder = new CardDef("W28Fodder", "W28Fodder", "unit", "", "common", "Test",
                                 1, 1, 1, 0, null, subtype: "Infantry");
        Check(watcher.WhenTriggers.Count, 1,
              "C0 （前提）`When a friendly troop dies, gain +3 Attack` 被收成一条监听器");

        // ---- C1 闸放行 ⇒ 观察者响 ----
        var ctx1 = ProbeBattle(new[] { watcher, fodder }, new CardDef[0]);
        ToP1Turn(ctx1, 1);
        var w1 = Place(ctx1, 0, 2, watcher, exhausted: true);
        var f1 = Place(ctx1, 0, 3, fodder, exhausted: true);
        CheckTrue(RuleCore.DeathObserverGateAllowsForTest(ctx1, f1),
                  "C0 （前提）闸放行（`IsRemnant=false` ∧ `AlreadyTriggeredBacklash=false`）");
        int atk1 = w1.Attack;
        RuleCore.ApplyDamage(ctx1, f1, 5, "自检");
        RuleCore.PumpActionQueues(ctx1);
        CheckTrue(!f1.IsAlive, "C0 （前提）炮灰真的死了");
        Check(w1.Attack, atk1 + 3,
              "★★ C1 闸放行 ⇒ 观察者的 `When a friendly troop dies` **响**（攻 +3）");

        // ---- C2 死者的 `+0x222 alreadyTriggeredBacklash` 置位 ⇒ 观察者**不响** ----
        var ctx2 = ProbeBattle(new[] { watcher, fodder }, new CardDef[0]);
        ToP1Turn(ctx2, 1);
        var w2 = Place(ctx2, 0, 2, watcher, exhausted: true);
        var f2 = Place(ctx2, 0, 3, fodder, exhausted: true);
        f2.AlreadyTriggeredBacklash = true;
        CheckTrue(!RuleCore.DeathObserverGateAllowsForTest(ctx2, f2),
                  "C2 （前提）闸拦下（`+0x222` 已置位）");
        int atk2 = w2.Attack;
        RuleCore.ApplyDamage(ctx2, f2, 5, "自检");
        RuleCore.PumpActionQueues(ctx2);
        CheckTrue(!f2.IsAlive, "C2 （前提）炮灰真的死了");
        Check(w2.Attack, atk2,
              "★★ C2 `+0x222` 置位 ⇒ 观察者 **不响**（攻不变）"
            + "｜🧨 删掉 `DeathObserverGateAllows` 里 `if (dead.AlreadyTriggeredBacklash) return false;`"
            + " 或 `CleanupDeaths` 的四处 `deathTriggersAllowed &&` ⇒ 实得 +3 ⇒ 红");

        // ---- C3/C4 残骸那一档（`.c:233` 的 `(isRemnant || cardState == 2)` 两半）----
        var ctx3 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx3, 1);
        var rem = new UnitState(ctx3.NewInstance(fodder), false) { IsRemnant = true };
        rem.MarkCardState(CardStateOptions.inPlayTransforming);
        Check(rem.CardStateOf(ctx3), CardStateOptions.inPlayTransforming,
              "C3 （前提）残骸此刻的 `cardState` = 15（`transforming`）");
        CheckTrue(!RuleCore.DeathObserverGateAllowsForTest(ctx3, rem),
                  "★★ C3 残骸 + `cardState != 2` ⇒ **拦**（`.c:233` 前半的第一支）"
                + "｜🧨 删掉 `if (dead.IsRemnant && dead.CardStateOf(ctx) != CardStateOptions.inPlay)` ⇒ 放行 ⇒ 红");
        rem.ClearCardStateMark();
        Check(rem.CardStateOf(ctx3), CardStateOptions.inPlay, "C4 （前提）清标记后 `cardState` 回 2（`inPlay`）");
        CheckTrue(RuleCore.DeathObserverGateAllowsForTest(ctx3, rem),
                  "★★ C4 残骸 + `cardState == 2` ⇒ **放行**（`.c:233` 的 `|| cardState == 2` 那一半）"
                + "｜🧨 写成 `if (dead.IsRemnant) return false;` ⇒ 实得「拦」⇒ 红 —— C3/C4 两条**结构上不可能同时满足**");
    }

    // ==============================================================================================
    //  ④ 账 `特效动画:503` —— 死讯的 `EvtKind.Death.Amount` 带**猎杀标记数**（发侧；表现侧归 `B97_W18`）
    // ==============================================================================================
    static void TestB97W28HuntMarkOnDeathSignal()
    {
        var fodder = new CardDef("W28HMFodder", "W28HMFodder", "unit", "", "common", "Test",
                                 1, 1, 1, 0, null, subtype: "Infantry");
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        var f = Place(ctx, 0, 3, fodder, exhausted: true);
        f.AddKeyword("huntmark", 2);   // 规范键就是字面量 `huntmark`（引擎读点 `u.KwValue("huntmark")`）
        Check(f.KwValue("huntmark"), 2, "D0 （前提）死者身上挂着 2 层猎杀标记");

        ctx.ClearSignals();
        RuleCore.ApplyDamage(ctx, f, 5, "自检");
        RuleCore.PumpActionQueues(ctx);
        CheckTrue(!f.IsAlive, "D0 （前提）炮灰真的死了");

        int deathCount = 0, deathAmount = -1;
        foreach (var e in ctx.Signals)
            if (e.Kind == EvtKind.Death) { deathCount++; deathAmount = e.Amount; }
        Check(deathCount, 1, "★ D1 恰一条 `EvtKind.Death`（这条信号从 2026-10-22 起是入队的，泵完才有）");
        Check(deathAmount, 2,
              "★★ D2 死讯的 `Amount` = **猎杀标记数 2**（原版 `TriggerOnMinionDeath.c:77` 是直调 "
            + "`ActivateTraitParticlesFromCodeInTarget(huntMark, …)`，闸 = 观察者==死者 ∧ `huntMark > 0`）"
            + "｜🧨 去掉 `amount: _dhunt` ⇒ 实得 0 ⇒ 红；写成常量 ⇒ 也红");

        // 反例（灭自证）：**没有**猎杀标记的死者 ⇒ `Amount` 必须是 0（不许带别的东西）
        var plain = new CardDef("W28HMPlain", "W28HMPlain", "unit", "", "common", "Test",
                                1, 1, 1, 0, null, subtype: "Infantry");
        var ctx2 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx2, 1);
        var f2 = Place(ctx2, 1, 3, plain, exhausted: true);
        ctx2.ClearSignals();
        RuleCore.ApplyDamage(ctx2, f2, 5, "自检");
        RuleCore.PumpActionQueues(ctx2);
        int amount2 = -1, count2 = 0;
        foreach (var e in ctx2.Signals)
            if (e.Kind == EvtKind.Death) { count2++; amount2 = e.Amount; }
        Check(count2, 1, "D3 （前提）反例也恰一条 `EvtKind.Death`");
        Check(amount2, 0, "★★ D3 反例：**没有** `huntmark` 的死者 ⇒ `Amount` = 0（不带任何别的数）");
    }

    // ==============================================================================================
    //  ⑤ 账 `:517` ② —— `FireTriggerByNumber`：按 trigger **数值**取正文并结算（数据半没落列 ⇒ 休眠）
    // ==============================================================================================
    static void TestB97W28FireTriggerByNumber()
    {
        // ---- E1 数据半没挂 ⇒ 今天恒「没有正文」（= 现池状态，铁律 11 的「休眠」）----
        var bare = new CardDef("W28FTBN0", "W28FTBN0", "unit", "", "common", "Test", 1, 1, 5, 0,
                               null, subtype: "Infantry");
        var ctx0 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx0, 1);
        var u0 = Place(ctx0, 0, 3, bare);
        CheckTrue(!bare.HasTriggerOpsByNumber, "E1 （前提）这张卡**没挂**数据半那张表");
        CheckTrue(!RuleCore.FireTriggerByNumberForTest(ctx0, u0, 410, 0, 3, null, null),
                  "★ E1 数据半没落列 ⇒ trigger 410 取不到正文 ⇒ **不结算**（休眠，⛔ 不是「做完了」）");

        // ---- E2 挂了数据半那张表 ⇒ **真结算** ----
        var ops = EffectText.Parse("Gain Armour 2", out _, out _);
        CheckTrue(ops != null && ops.Count > 0, "E2 （前提）`Gain Armour 2` 解得出 ≥1 条 op");
        var withTable = new CardDef("W28FTBN1", "W28FTBN1", "unit", "", "common", "Test", 1, 1, 5, 0,
                                    null, subtype: "Infantry");
        withTable.AttachTriggerOpsByNumber(new Dictionary<int, List<EffectOp>> { { 410, ops } });
        CheckTrue(withTable.HasTriggerOpsByNumber, "E2 （前提）数据半已挂上");
        var ctx1 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx1, 1);
        var u1 = Place(ctx1, 0, 3, withTable);
        Check(u1.Armor, 0, "E2 （前提）打之前护甲 0");
        CheckTrue(RuleCore.FireTriggerByNumberForTest(ctx1, u1, 410, 0, 3, null, null),
                  "★★ E2 trigger 410 **入队了**（返 true）"
                + "｜🧨 执行口没接 / `TriggerOpsByNumber` 改成只读卡面关键词 ⇒ 实得 false ⇒ 红");
        RuleCore.PumpActionQueues(ctx1);
        Check(u1.Armor, 2,
              "★★ E2' 泵完 ⇒ **按 trigger 410 从数据半取到的正文结算**（护甲 0 → 2）"
            + "｜🧨 同上 ⇒ 仍是 0 ⇒ 红 —— 这一条是「数据半挂进来能不能真跑」的判别式（账 `:517` 的前置）");

        // ---- E3 数值不在 `AbilityTriggerNumberOfName` 里 ⇒ ⛔ 不猜 ----
        CheckTrue(!RuleCore.FireTriggerByNumberForTest(ctx1, u1, 99999, 0, 3, null, null),
                  "★ E3 认不出的 trigger 数值 ⇒ 返 false 并出声（⛔ 不猜）");
    }
}
