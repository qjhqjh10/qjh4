// B98K4_Assert.cs — 执行代理 `B98_K4`（第九十八会话 · 波 9 · 战斗线）的断言集
//
// ============================ 它盯的是什么（三条）============================
//   账 `战斗逻辑驱动` 的三条：
//     · `:1287` `RawCardScript.UpdateTriggerOnce` —— `FireTriggerOnce` 把数据半那张表
//                （`CardDef.TriggerOpsByNumber(200)`）**真喂进正文口**（`FireTriggerAt(..., opsOverride:)`）
//     · `:533`  `BattleManager.ResolveRemoveFromCemetery` 的**消费端** —— `ReturnFromCemetery` /
//                `DoDeployOnce`(graveyard) 的候选从「本局死亡流水 `DeadUnits`」换成**墓地那一口**
//                （`CemeteryDeadUnits` → `ResolveCemeteryTargets` → `RuleCore.GetCardsInCemetery`）
//     · `:646`  `BattleManagerSupport.BroadcastUnitDamaged` 的**棋盘侧按 trigger 数值**发 60 / 185 / 187
//                （`FireBoardDamagedByNumber`，原版 `CardScript__ResolveDamageDealt.c:160 / :191`）
//
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B98_J0_战斗剩余终局.md`（三条的「剩」那一行）
//                            ＋ `资料/完全复刻/报告/B98_K4.md`
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A3：把 `FireTriggerOnce` 最后一句改回 `FireTriggerAt(...)`（**不传** `opsOverride`）⇒ A3/A3' 红
//         （卡面没有 `TriggerOnceIfBoardCriteria` 这个词 ⇒ 正文恒空 ⇒ 护甲仍是 0）。
//   · B2：把 `CemeteryDeadUnits` 换回「扫 `ctx.DeadUnits`」⇒ B2 从 1 变 0 ⇒ 红
//         （那张牌**只进过弃牌堆**、没有死亡记录 = 原版墓地里有它、死亡流水里没有）。
//   · B3：把候选换成「手扫 `Discard`」的实现 ⇒ B3 红（被放逐/残骸的那张会重新冒出来）。
//   · C2：把 `BroadcastWhenCore` 那两条 `FireBoardDamagedByNumber` 调用删掉 ⇒ C2' 红（护甲仍 0）。
//   · C4/C5：把 `damageType` 的分流写反 / 都发 ⇒ C4' 或 C6 红。
//   · C7：把 `130 Rage` 也塞进 `FireBoardDamagedByNumber` ⇒ 忏悔那条**双响**（护甲 2 变 4）⇒ 红。
//
// ============================ 怎么用（宿主）============================
//   `RuleEngineTest.Run()` 里插一行：`Step(TestB98K4);`
//   （本文件与 `B97W28_Assert.cs` 同形 —— `partial class RuleEngineTest`，复用夹具 `Battle` /
//    `ProbeBattle` / `Place` / `ToP1Turn` / `Check`，⛔ 不另抄一套。）
//   ⚠️ **本批白名单不含宿主** ⇒ 这一行要**调度台**加；加之前本文件一条都不跑（如实标）。
//   ⚠️ 本文件**不建 GameObject、不建资产、不写盘**；只用 `new BattleContext` 的内存局面。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

public static partial class RuleEngineTest
{
    /// <summary>B98_K4 的全部断言（宿主：`RuleEngineTest.Run` 里 `Step(TestB98K4);`）。</summary>
    public static void TestB98K4()
    {
        Section("B98_K4 —— 账 1287（trigger 200 正文）/ 533（墓地消费端）/ 646（棋盘按数值发 60·185·187）");

        TestB98K4Trigger200Body();
        TestB98K4CemeteryConsumer();
        TestB98K4BoardDamagedByNumber();
    }

    // ==============================================================================================
    //  夹具
    // ==============================================================================================

    /// <summary>一张挂了指定 `abilityTriggers` 的单位卡（数据列 = v17 真值形状）。</summary>
    static CardDef K4TriggerCard(string id, string desc, int[] triggers, string[] keywords = null)
    {
        return new CardDef(id, id, "unit", desc, "common", "Test", 1, 1, 6, 0,
                           keywords: keywords, subtype: "Infantry",
                           abilityTriggers: triggers, abilityTriggersVersioned: true);
    }

    static EffectTargetSpec K4Spec(string side, string kind)
    {
        return new EffectTargetSpec { Raw = side + " " + kind, Side = side, Kind = kind, Count = 1 };
    }

    /// <summary>`Gain Armour 2` 那一份 op（与 `B97W28` 的 E2 同一份夹具口径）。</summary>
    static List<EffectOp> K4ArmourOps(int n)
    {
        var ops = EffectText.Parse("Gain Armour " + n, out _, out _);
        CheckTrue(ops != null && ops.Count > 0, "（前提）`Gain Armour " + n + "` 解得出 ≥1 条 op");
        return ops;
    }

    // ==============================================================================================
    //  甲 · 账 1287 —— `FireTriggerOnce` 把 `TriggerOpsByNumber(200)` 喂进正文口
    // ==============================================================================================
    static void TestB98K4Trigger200Body()
    {
        Section("甲 · 账 1287 `RawCardScript.UpdateTriggerOnce`（trigger 200 的正文）");

        // A1/A2 前提：夹具的形状真的走到「有 200 这条 ability」那一道闸
        var card = K4TriggerCard("K4T200", "", new[] { 200 });
        Check(card.AbilityTriggerPresence("TriggerOnceIfBoardCriteria"), 1,
              "A1 ★ 前提：夹具卡的 `AbilityTriggerPresence(\"TriggerOnceIfBoardCriteria\") == 1`"
            + "（否则 `FireTriggerOnce` 在第一道闸就早退，下面测的会是别的闸）");
        CheckTrue(!card.HasTriggerOpsByNumber, "A2 ★ 前提：先**不挂**数据半那张表（对照组）");

        var ctx0 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx0, 1);
        var u0 = Place(ctx0, 0, 3, card);
        CheckTrue(!RuleCore.FireTriggerOnce(ctx0, u0, 0, 3),
                  "A2' ★ 灭自证（对照组）：**没挂表** ⇒ 闸那一句 `onceOps == null` 早退 ⇒ **不触发**"
                + "（卡面文本也取不到正文 —— 全池 0 张卡面写 `TriggerOnceIfBoardCriteria`）");

        // A3/A3'：挂了表 ⇒ **真的按表结算**
        var card2 = K4TriggerCard("K4T200b", "", new[] { 200 });
        card2.AttachTriggerOpsByNumber(new Dictionary<int, List<EffectOp>> { { 200, K4ArmourOps(2) } });
        CheckTrue(card2.HasTriggerOpsByNumber, "A3 ★ 前提：数据半已挂上");
        Check(card2.TriggerOpsByNumber(200) != null ? card2.TriggerOpsByNumber(200).Count : -1, 1,
              "A3（同族）`TriggerOpsByNumber(200)` 读到 1 条 op");

        var ctx1 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx1, 1);
        var u1 = Place(ctx1, 0, 3, card2);
        Check(u1.Armor, 0, "A3 ★ 前提：结算前护甲 0");
        CheckTrue(RuleCore.FireTriggerOnce(ctx1, u1, 0, 3),
                  "★★ A3 `FireTriggerOnce` **入队了**（返 true）—— 正文来自 `TriggerOpsByNumber(200)`"
                + "｜🧨 把最后一句改回 `FireTriggerAt(...)`（不传 `opsOverride`）⇒ 实得 false ⇒ 红");
        RuleCore.PumpActionQueues(ctx1);
        Check(u1.Armor, 2,
              "★★ A3' 泵完 ⇒ **按 200 那张表结算**（护甲 0 → 2）"
            + "｜🧨 同上 ⇒ 仍是 0 ⇒ 红 —— 这一条就是「数据半挂进来能不能真跑」的判别式（账 1287 的正文半）");

        // A4 已触发过一次就不再触发（`UnitState.TriggerOnceFired` 那一半的口径前提）
        CheckTrue(!RuleCore.FireTriggerOnce(ctx1, u1, 0, 3),
                  "A4 `TriggerOnceFired` 置位后同一张卡**不再触发**（once 语义；原版 `triggeredAbility = false`）");

        // A5 空值安全
        CheckTrue(!RuleCore.FireTriggerOnce(null, u1, 0, 3), "A5 `ctx == null` ⇒ false（不抛）");
        CheckTrue(!RuleCore.FireTriggerOnce(ctx1, null, 0, 3), "A5（同族）`u == null` ⇒ false（不抛）");
    }

    // ==============================================================================================
    //  乙 · 账 533 —— 墓地消费端（`ReturnFromCemetery` / `DoDeployOnce`）改用墓地那一口
    // ==============================================================================================
    static void TestB98K4CemeteryConsumer()
    {
        Section("乙 · 账 533 `ResolveRemoveFromCemetery` 的消费端（候选 = 墓地，不是死亡流水）");

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);

        // B1 前提：一张**只进过弃牌堆**的单位卡（= 手牌超上限被弃掉的形状：原版墓地里查得到、
        //          我们的 `DeadUnits` 里没有）
        var handed = K4TriggerCard("K4CemUnit", "", null);
        var inst = ctx.NewInstance(handed);
        ctx.Players[0].Discard.Add(inst);
        Check(RuleCore.GetCardsInCemetery(ctx, true, true, true, true, true, false).Count, 1,
              "B1 ★ 前提：`GetCardsInCemetery` 里读到 1 张（我方墓地的那张单位卡）");
        Check(ctx.DeadUnits.Count, 0,
              "B1（同族）前提：`ctx.DeadUnits` **是空的**（这张牌从没死过）—— 这就是「换源了没有」的判别位");

        // B2 ★ 候选来自墓地那一口 ⇒ 它在
        var spec = K4Spec("own", "troop");
        var cem = RuleCore.CemeteryDeadUnitsForTest(ctx, 0, spec, null);
        Check(cem.Count, 1,
              "★★ B2 候选 = **墓地那一口** ⇒ 读到 1 张（原版 `BattleManager.GetCardsInCemetery` 的等价物）"
            + "｜🧨 换回「扫 `ctx.DeadUnits`」⇒ 实得 0 ⇒ 红");
        Check(cem.Count == 1 && cem[0].Instance != null && ReferenceEquals(cem[0].Instance, inst), true,
              "B2（同族）命中的就是**那一份实例**（同名两份不会拿错）");
        Check(cem.Count == 1 ? cem[0].Owner : -1, 0, "B2（同族）`Owner` = 它落在谁的弃牌堆里（0）");
        Check(cem.Count == 1 ? cem[0].DeathTurn : 99, -1,
              "B2（同族）`DeathTurn = -1` —— 没有死亡记录（手牌超上限弃掉的单位卡）⇒ 死亡窗口**判不了**");

        // B3 ★ 灭自证：墓地放逐之后它**必须**消失（证明走的是 `GetCardsInCemetery`，不是手扫 `Discard`）
        ctx.Players[0].MarkRemovedFromCemetery(inst);
        Check(RuleCore.CemeteryDeadUnitsForTest(ctx, 0, spec, null).Count, 0,
              "★★ B3 灭自证：这张牌被「从墓地放逐」（原版 `+0x24b`）⇒ 候选变 0"
            + "｜🧨 换成手扫 `Discard` 的实现 ⇒ 它又会冒出来 ⇒ 红");
        ctx.Players[0].Discard.Remove(inst);

        // B4 ★ 消费端整条路：`deadScope = "all"` ⇒ 真的把它搬进牌库
        var ctx2 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx2, 1);
        var handed2 = K4TriggerCard("K4CemUnit2", "", null);
        var inst2 = ctx2.NewInstance(handed2);
        ctx2.Players[0].Discard.Add(inst2);
        var opReturn = new EffectOp { Verb = "return", Source = "K4Fixture", Dest = "decktop", Amount = 0, Target = spec, SubjectTargetsAffected = 60 };
        var moved = new List<CardInstance>();
        int back = RuleCore.ReturnFromCemeteryForTest(ctx2, 0, "K4", opReturn, spec, "all", moved);
        Check(back, 1,
              "★★ B4 消费端：`friendly troop that died this game` ⇒ 把那张**只进过弃牌堆**的单位卡捞回来 1 张"
            + "｜🧨 换回扫 `DeadUnits` ⇒ 实得 0 ⇒ 红（改前就是 0 —— 这正是本笔换源的可观测差别）");
        Check(ctx2.Players[0].Deck.Contains(inst2), true, "B4（同族）那一份实例进了牌库");
        Check(!ctx2.Players[0].Discard.Contains(inst2), true, "B4（同族）弃牌堆里那一份被摘走（`TakeFromGraveyard` 的唯一口）");

        // B5 ★ 窗口：`died since your last turn` ⇒ 没有死亡记录 ⇒ **排除**（判不了就不猜）
        var ctx3 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx3, 1);
        var handed3 = K4TriggerCard("K4CemUnit3", "", null);
        var inst3 = ctx3.NewInstance(handed3);
        ctx3.Players[0].Discard.Add(inst3);
        var moved3 = new List<CardInstance>();
        Check(RuleCore.ReturnFromCemeteryForTest(ctx3, 0, "K4", opReturn, spec, "since_last_turn", moved3), 0,
              "★★ B5 窗口分支：`DeathTurn = -1`（判不了）⇒ **排除** ⇒ 0 张"
            + "｜🧨 把「判不了」当成「在窗口内」⇒ 实得 1 ⇒ 红（那会把死了一整局的老牌也捞回来）");
    }

    // ==============================================================================================
    //  丙 · 账 646 —— 棋盘侧按 trigger 数值发 60 / 185 / 187
    // ==============================================================================================
    static void TestB98K4BoardDamagedByNumber()
    {
        Section("丙 · 账 646 `BroadcastUnitDamaged` 的棋盘侧（按 trigger 数值 60 / 185 / 187）");

        // C1 前提：60 的数值名在表里（`AbilityTriggerNumberOfName`）
        Check(CardDef.AbilityTriggerNumber("UnitDamaged"), 60, "C1 ★ 前提：60 = `UnitDamaged`");
        Check(CardDef.AbilityTriggerNumber("UnitDamagedByAbility"), 185, "C1（同族）：185 = `UnitDamagedByAbility`");
        Check(CardDef.AbilityTriggerNumber("UnitDamagedByAttack"), 187, "C1（同族）：187 = `UnitDamagedByAttack`");

        // C2/C2'：trigger 60 的卡挂了表 ⇒ `Hurt` 之后按表结算（**只能**从数值那一路来 —— 这张卡没有卡面监听文本）
        var c60 = K4TriggerCard("K4Dmg60", "", new[] { 60 });
        c60.AttachTriggerOpsByNumber(new Dictionary<int, List<EffectOp>> { { 60, K4ArmourOps(2) } });
        Check(c60.WhenTriggers.Count, 0,
              "C2 ★ 前提：这张卡**没有** `When … receives damage` 的卡面监听器（文本那一路不会响）"
            + " ⇒ 下面读到的变化**只能**来自「按 trigger 数值」那一条");
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        var u = Place(ctx, 0, 3, c60);
        Check(u.Armor, 0, "C2（同族）前提：挨打前护甲 0");
        RuleCore.HurtWithSourceForTest(ctx, u, 1, "K4");
        RuleCore.PumpActionQueues(ctx);
        Check(u.Armor, 2,
              "★★ C2' 挨了 1 点伤害 ⇒ **60 那条正文结算**（护甲 0 → 2）"
            + "｜🧨 删掉 `BroadcastWhenCore` 里那两条 `FireBoardDamagedByNumber` 调用 ⇒ 仍是 0 ⇒ 红");

        // C3 ★ 灭自证：同样的卡**不挂表** ⇒ 什么都不发生（证明确实是「数值 → 正文」那一路）
        var c60b = K4TriggerCard("K4Dmg60b", "", new[] { 60 });
        var ctxB = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctxB, 1);
        var uB = Place(ctxB, 0, 3, c60b);
        RuleCore.HurtWithSourceForTest(ctxB, uB, 1, "K4");
        RuleCore.PumpActionQueues(ctxB);
        Check(uB.Armor, 0,
              "★★ C3 灭自证：挂了 `abilityTriggers = [60]` 但**没挂正文表** ⇒ 护甲仍 0"
            + "（`FireTriggerByNumber` 取不到 op ⇒ 不结算，⛔ 不是「有 trigger 就照跑」）");

        // C4 ★ 185：`damageType ∈ {0, 3}`（Spell ⇒ 3）才发
        var c185 = K4TriggerCard("K4Dmg185", "", new[] { 185 });
        c185.AttachTriggerOpsByNumber(new Dictionary<int, List<EffectOp>> { { 185, K4ArmourOps(2) } });
        var ctx185 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx185, 1);
        var u185 = Place(ctx185, 0, 3, c185);
        RuleCore.HurtWithSourceForTest(ctx185, u185, 1, "K4", kind: DamageKind.Spell);
        RuleCore.PumpActionQueues(ctx185);
        Check(u185.Armor, 2,
              "★★ C4 `DamageKind.Spell` ⇒ `damageType = 3 (Ability)` ⇒ **185 发**（护甲 0 → 2）"
            + "｜🧨 把分流条件写反 / 不发 185 ⇒ 仍是 0 ⇒ 红");

        // C5 ★ 187：`damageType ∈ {4, 6}`（Melee ⇒ 4）才发
        var c187 = K4TriggerCard("K4Dmg187", "", new[] { 187 });
        c187.AttachTriggerOpsByNumber(new Dictionary<int, List<EffectOp>> { { 187, K4ArmourOps(3) } });
        var ctx187 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx187, 1);
        var u187 = Place(ctx187, 0, 3, c187);
        RuleCore.HurtWithSourceForTest(ctx187, u187, 1, "K4", kind: DamageKind.Melee);
        RuleCore.PumpActionQueues(ctx187);
        Check(u187.Armor, 3,
              "★★ C5 `DamageKind.Melee` ⇒ `damageType = 4 (Attack)` ⇒ **187 发**（护甲 0 → 3）");

        // C6 ★ 灭自证 · 分流：185 那张卡挨**近战**（187 那一档）⇒ **不发**
        var ctx6 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx6, 1);
        var c185b = K4TriggerCard("K4Dmg185b", "", new[] { 185 });
        c185b.AttachTriggerOpsByNumber(new Dictionary<int, List<EffectOp>> { { 185, K4ArmourOps(2) } });
        var u6 = Place(ctx6, 0, 3, c185b);
        RuleCore.HurtWithSourceForTest(ctx6, u6, 1, "K4", kind: DamageKind.Melee);
        RuleCore.PumpActionQueues(ctx6);
        Check(u6.Armor, 0,
              "★★ C6 灭自证：185 那张卡挨 `Melee` ⇒ **不发**（护甲仍 0）—— 说明 C4/C5 读的是 `damageType` 分流，不是「随便发一条」");

        // C7 ★ 灭自证 · `130 Rage` 不在这里再发一次（否则忏悔双响）
        var pen = K4TriggerCard("K4Penitence", "Penitence: Gain Armour 2", new[] { 130 }, new[] { KeywordTable.Penitence });
        CheckTrue(pen.TriggerOps(KeywordTable.Penitence) != null && pen.TriggerOps(KeywordTable.Penitence).Count > 0,
                  "C7 ★ 前提：`Penitence:` 的正文真的收进了 `_triggerOps`（= 关键词文本那一路会结算）");
        // 同时把 130 的**数值表**也挂上（真卡池里那 5 张就是非空的：`penitence` ∈ `RoutableTriggers` + `ab=0` 行）
        //   ⇒ 若有人把 130 塞进 `FireBoardDamagedByNumber`，下面那条会读到 +4（双响）⇒ 红。
        pen.AttachTriggerOpsByNumber(new Dictionary<int, List<EffectOp>> { { 130, EffectText.Parse("Gain Armour 2", out _, out _) } });
        var ctx7 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx7, 1);
        var u7 = Place(ctx7, 0, 3, pen);
        RuleCore.HurtWithSourceForTest(ctx7, u7, 1, "K4");
        RuleCore.PumpActionQueues(ctx7);
        Check(u7.Armor, 2,
              "★★ C7 灭自证：带 `Penitence` 的单位挨打 ⇒ 护甲 **+2（只一次）**"
            + "｜🧨 把 `130 Rage` 也塞进 `FireBoardDamagedByNumber` ⇒ 双响、实得 4 ⇒ 红"
            + "（130 的等价实现已经在 `HurtCore` 的 `FireTriggerOnBoard(Penitence)` 里）");
    }
}
