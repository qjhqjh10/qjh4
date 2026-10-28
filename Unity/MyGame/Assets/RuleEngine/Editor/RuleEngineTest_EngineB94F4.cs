// RuleEngineTest_EngineB94F4.cs — 第九十四会话 · 波 1 · 执行代理 `F4`（引擎小文件族）
//
// 为什么另开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// 同一时刻只有一个写手改 `RuleEngineTest.cs`（归 F1）⇒ 本批断言全写在这里、**只交一个入口方法名**，
// 由调度台在 `Run()` 里加一行 `Step(TestB94F4EngineGaps)`（⛔ 本代理不改 F1 的文件）。
//
// 本批覆盖的 4 个原版方法（判据全文 = `D:/2/tools/decomp_full/`，逐句现读 2026-10-29）：
//   ① `BattleManager.AddTriggerCourtesy` / `AddTriggerRelentless`（账 178 / 183）
//      —— 入队那条动作的 `actionType` = **0x41 = 65（triggerCourtesy）/ 0x3e = 62（triggerRelentless）**，
//      其余关键词 = 0。判据：`BattleManager__AddTriggerCourtesy.c:29` `BattleAction___ctor(local_208,0x41,…)`
//      / `BattleManager__AddTriggerRelentless.c:29` 同形只差常数 `0x3e`。
//   ② `BattleManager.CancelEffectsFromDisappearingUnitFromQueue`（账 241 / 242）
//      —— 倒序扫一条泳道；命中条件 = casterCard 非空且同卡 ∧ actionType≠1(attack) ∧ targetCard 为 null 或相同；
//      三个**结束整条泳道**的豁免 = ⓐ 45(destroyUnit)+死亡类型 40(poison) ⓑ 26(recallToHand) ⓒ
//      shouldIgnoreDestroy ∧ 45。判据：`BattleManager__CancelEffectsFromDisappearingUnitFromQueue.c:1-140`。
//   ③ `EntityScript.EnemyReturnsAttack`（账 1025）—— **预测版** = 实际四道 + 第 5 道 shuriken(1120)，
//      第 5 道**不要求远程**（`EntityScript__EnemyReturnsAttack.c:60-73`）。它与实际结算 `NoCounter`（四道）
//      在「星镖能打死守方」时**结论相反**（预测免反击 / 实际照吃）—— 这就是本批的核心灭自证断言。
//   ④ `CardTraitCollection.GetTraitForActiveAbilityRawCard`（账 993）—— 六档按阵营：10→1275 / 80→1185 /
//      100→1220 / 110→1235 / 130→1265 / 其余→400（`CardTraitCollection__GetTraitForActiveAbilityRawCard.c:1-26`），
//      阵营号 ↔ 名 的映射 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardArmy.cs`。
//   ⑤ `MinionManager.GetMinionList`（账 1114）—— 按 `orderInPlay` 升序（`MinionManager__GetMinionList.c` 末段
//      `Enumerable.OrderBy`）⇒ 我们的 `PlayerState.UnitsByPlayOrder`。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（调度台在 `Run()` 里加一行 `Step(TestB94F4EngineGaps)`）。</summary>
    public static void TestB94F4EngineGaps()
    {
        Section("🆕 F4（B94 波 1）：触发入队道/类型 · 撤动作泳道 · 反击预测第 5 道 · 主动能力 trait 六档 · 兵列表进场序");
        B94F4T1GrantedTriggerActionTypes();
        B94F4T2CancelLaneExemptions();
        B94F4T3CounterAttackPredictiveFifthGate();
        B94F4T4ActiveAbilityTraitSixBranches();
        B94F4T5MinionListPlayOrder();
        B94F4T6CemeteryBanishLatch();
        B94F4T7CardChosenInSelectionAndPositionWillBeAtLeft();
    }

    // ==============================================================================================
    //  ① 触发入队那条动作的 actionType（账 178 / 183）
    // ==============================================================================================

    static void B94F4T1GrantedTriggerActionTypes()
    {
        // ── courtesy ⇒ 0x41 = 65 ────────────────────────────────────────────────────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var target = new UnitState(ctx.NewInstance(Unit("B94TargetC", 1, 1, 3)), false);
            var caster = new UnitState(ctx.NewInstance(Unit("B94CasterC", 1, 1, 3)), false);
            bool ok = RuleCore.EnqueueGrantedTrigger(ctx, KeywordTable.Courtesy, target, caster, ActionPriority.Instant);
            CheckTrue(ok, "① （前提）`EnqueueGrantedTrigger(courtesy)` 入队成功");
            var qa = ctx.ActionQueue.At(ActionPriority.Instant, 0);
            CheckTrue(qa != null, "① （前提）队列里拿得到那一条");
            Check(qa.ActionType, 0x41,
                  "★★ ① `courtesy` 入队那条动作的 actionType = **0x41 = 65（triggerCourtesy）**"
                + "｜判据 `BattleManager__AddTriggerCourtesy.c:29` `BattleAction___ctor(…,0x41,…)`"
                + "｜🧨 把 actionType 写成 0 / 写反成 0x3e / 只对照 courtesy 不判 relentless ⇒ 红");
        }
        // ── relentless ⇒ 0x3e = 62 ─────────────────────────────────────────────────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var target = new UnitState(ctx.NewInstance(Unit("B94TargetR", 1, 1, 3)), false);
            var caster = new UnitState(ctx.NewInstance(Unit("B94CasterR", 1, 1, 3)), false);
            bool ok = RuleCore.EnqueueGrantedTrigger(ctx, KeywordTable.Relentless, target, caster, ActionPriority.Instant);
            CheckTrue(ok, "① （前提）`EnqueueGrantedTrigger(relentless)` 入队成功");
            var qa = ctx.ActionQueue.At(ActionPriority.Instant, 0);
            CheckTrue(qa != null, "① （前提）队列里拿得到那一条");
            Check(qa.ActionType, 0x3e,
                  "★★ ① `relentless` 入队那条动作的 actionType = **0x3e = 62（triggerRelentless）**"
                + "｜判据 `BattleManager__AddTriggerRelentless.c:29`（与 Courtesy 逐字节同形，只差常数 0x3e）"
                + "｜🧨 与 courtesy 共用同一个常数（写死 0x41）⇒ 红");
        }
        // ── 其它关键词 ⇒ 0（不冒充致谢/不懈）────────────────────────────────────────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var target = new UnitState(ctx.NewInstance(Unit("B94TargetX", 1, 1, 3)), false);
            var caster = new UnitState(ctx.NewInstance(Unit("B94CasterX", 1, 1, 3)), false);
            RuleCore.EnqueueGrantedTrigger(ctx, "somenotrigger", target, caster, ActionPriority.Instant);
            var qa = ctx.ActionQueue.At(ActionPriority.Instant, 0);
            CheckTrue(qa != null, "① （前提）非 courtesy/relentless 的关键词也入队成功（入队本身不按类型拒）");
            Check(qa.ActionType, 0,
                  "★★ ① 其它关键词的 actionType = **0**（⛔ 不冒充 0x41/0x3e）"
                + "｜🧨 把三目写成 `keyword != Relentless ? 0x41 : …` ⇒ 这一条红");
        }
    }

    // ==============================================================================================
    //  ② 撤一条泳道的命中条件 + 三个「结束整条泳道」的豁免（账 241 / 242）+ 下标越界（账 240 的一半）
    // ==============================================================================================

    static void B94F4T2CancelLaneExemptions()
    {
        // 每格一个独立 ctx（互不影响）。jammed = 被撤动作的「施放者/载体」，other = 别人。
        // ── ① 正常命中：actionType=2（非 attack）、Actor=jammed、Target=null ⇒ 撤掉 ─────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var jammed = new UnitState(ctx.NewInstance(Unit("B94Jammed", 1, 1, 3)), false);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Label = "hit" },
                                    ActionPriority.Instant);
            int n = RuleCore.CancelEffectsFromDisappearingUnitFromQueue(ctx, jammed, null, ActionPriority.Instant);
            Check(n, 1, "★★ ② 命中条件全满足（同卡 · 非 attack · target=null）⇒ 撤掉 **1** 条"
                       + "｜🧨 ① 少判 `Actor==jammed`（撤别人）/ ② 把 `!=1` 写成 `==1`（只撤 attack）⇒ 红");
            Check(ctx.ActionQueue.CountOf(ActionPriority.Instant), 0, "② 撤完泳道清空");
        }
        // ── ② actionType=1（attack）⇒ **不撤** ────────────────────────────────────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var jammed = new UnitState(ctx.NewInstance(Unit("B94Jammed2", 1, 1, 3)), false);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 1, Label = "atk" },
                                    ActionPriority.Instant);
            int n = RuleCore.CancelEffectsFromDisappearingUnitFromQueue(ctx, jammed, null, ActionPriority.Instant);
            Check(n, 0, "★★ ② `actionType == 1`（attack）**不撤**（原版 `while` 条件第二支 `type != 1`）"
                       + "｜🧨 删掉 `if (a.ActionType == 1) continue;` ⇒ 撤了 ⇒ 实得 1 ⇒ 红");
            Check(ctx.ActionQueue.CountOf(ActionPriority.Instant), 1, "② attack 那条还在");
        }
        // ── ③ 载体不像 ⇒ 不撤 ─────────────────────────────────────────────────────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var jammed = new UnitState(ctx.NewInstance(Unit("B94Jammed3", 1, 1, 3)), false);
            var other = new UnitState(ctx.NewInstance(Unit("B94Other3", 1, 1, 3)), false);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = other, ActionType = 2, Label = "other" },
                                    ActionPriority.Instant);
            int n = RuleCore.CancelEffectsFromDisappearingUnitFromQueue(ctx, jammed, null, ActionPriority.Instant);
            Check(n, 0, "★★ ② Actor 是**别人** ⇒ 不撤（命中条件①「casterCard == jammedUnit」）"
                       + "｜🧨 去掉 `ReferenceEquals(a.Actor, jammedUnit)` ⇒ 撤了 ⇒ 实得 1 ⇒ 红");
        }
        // ── ④ 豁免 ⓑ：actionType=26（recallToHand）⇒ 结束整条泳道、不撤 ───────────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var jammed = new UnitState(ctx.NewInstance(Unit("B94Jammed4", 1, 1, 3)), false);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 26, Label = "recall" },
                                    ActionPriority.Instant);
            int n = RuleCore.CancelEffectsFromDisappearingUnitFromQueue(ctx, jammed, null, ActionPriority.Instant);
            Check(n, 0, "★★ ② 豁免 ⓑ：`actionType == 26`（recallToHand）⇒ **结束整条泳道、不撤**"
                       + "｜🧨 删掉 `if (a.ActionType == 0x1a) return n;` ⇒ 撤了 ⇒ 实得 1 ⇒ 红");
        }
        // ── ⑤ 豁免 ⓐ：45(destroyUnit) + DeathType 40(poison) ⇒ 结束整条泳道、不撤 ────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var jammed = new UnitState(ctx.NewInstance(Unit("B94Jammed5", 1, 1, 3)), false);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 45, DeathType = 40, Label = "poison" },
                                    ActionPriority.Instant);
            int n = RuleCore.CancelEffectsFromDisappearingUnitFromQueue(ctx, jammed, null, ActionPriority.Instant);
            Check(n, 0, "★★ ② 豁免 ⓐ：`actionType == 45 ∧ DeathType == 40(poison)` ⇒ **结束整条泳道、不撤**"
                       + "｜🧨 删掉 `if (a.ActionType == 0x2d && a.DeathType == 0x28) return n;` ⇒ 撤了 ⇒ 实得 1 ⇒ 红");
        }
        // ── ⑥ 豁免 ⓒ：45(destroyUnit) 非毒 —— shouldIgnoreDestroy 才豁免；否则照撤 ───────────────
        {
            var ctxA = Battle(new CardDef[0], new CardDef[0]);
            var jammedA = new UnitState(ctxA.NewInstance(Unit("B94Jammed6a", 1, 1, 3)), false);
            ctxA.ActionQueue.Enqueue(new QueuedAction { Actor = jammedA, ActionType = 45, DeathType = 0, Label = "destroy" },
                                     ActionPriority.Instant);
            int nA = RuleCore.CancelEffectsFromDisappearingUnitFromQueue(ctxA, jammedA, null,
                                                                           ActionPriority.Instant, shouldIgnoreDestroy: false);
            Check(nA, 1, "★★ ② `45(destroyUnit)` 非毒 + `shouldIgnoreDestroy=false` ⇒ **照撤**"
                        + "｜🧨 把豁免 ⓒ 写成无条件 `45 ⇒ return` ⇒ 实得 0 ⇒ 红");

            var ctxB = Battle(new CardDef[0], new CardDef[0]);
            var jammedB = new UnitState(ctxB.NewInstance(Unit("B94Jammed6b", 1, 1, 3)), false);
            ctxB.ActionQueue.Enqueue(new QueuedAction { Actor = jammedB, ActionType = 45, DeathType = 0, Label = "destroy" },
                                     ActionPriority.Instant);
            int nB = RuleCore.CancelEffectsFromDisappearingUnitFromQueue(ctxB, jammedB, null,
                                                                           ActionPriority.Instant, shouldIgnoreDestroy: true);
            Check(nB, 0, "★★ ② `45(destroyUnit)` + `shouldIgnoreDestroy=true` ⇒ **豁免、结束泳道、不撤**"
                        + "｜🧨 忽略 shouldIgnoreDestroy 实参 / 把它写反 ⇒ 实得 1 ⇒ 红");
        }
        // ── ⑦ 四道全扫：Instant + DelayedInstant 各一条可撤 ⇒ 合计 2（账 241）────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var jammed = new UnitState(ctx.NewInstance(Unit("B94Jammed7", 1, 1, 3)), false);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Label = "a" }, ActionPriority.Instant);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Label = "b" },
                                    ActionPriority.DelayedInstant);
            int n = RuleCore.CancelEffectsFromDisappearingUnit(ctx, jammed, null, false);
            Check(n, 2, "★★ ② `CancelEffectsFromDisappearingUnit` 四道全扫 ⇒ Instant + DelayedInstant 各撤 1 = **2**"
                       + "｜判据 `BattleManager__CancelEffectsFromDisappearingUnit.c` 对四条队列各调一次"
                       + "｜🧨 只扫一两条道 / 漏 DelayedInstant ⇒ 实得 1 ⇒ 红");
        }
        // ── ⑧ 账 240 的一半：RemoveQueuedActionAt 下标越界 ⇒ false（原版 LogError 后 return）──────
        {
            var q = new ActionQueue();
            q.Enqueue(new QueuedAction { Label = "only" }, ActionPriority.Instant);
            CheckTrue(!q.RemoveQueuedActionAt(ActionPriority.Instant, 5),
                      "★★ ② `RemoveQueuedActionAt` 下标越界 ⇒ **false**（原版 `CancelBattleActionFromQueue.c:20-25` "
                    + "`idx >= count ⇒ LogError+return`）｜🧨 越界改成静默 true / RemoveAt 抛异常 ⇒ 红");
            CheckTrue(q.RemoveQueuedActionAt(ActionPriority.Instant, 0), "② 合法下标 ⇒ 撤成功");
            Check(q.CountOf(ActionPriority.Instant), 0, "② 撤完 Instant 道 = 0");
        }
    }

    // ==============================================================================================
    //  ③ 反击【预测】版第 5 道 shuriken —— 与实际结算 NoCounter（四道）结论相反（账 1025）
    // ==============================================================================================

    static void B94F4T3CounterAttackPredictiveFifthGate()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);

        // 攻方：无 sneak/terror/longrange/sniper（前四道全不命中）；守方 2 血、0 甲。
        // 加一条 shuriken=5（≥ 守方 2 血 ∧ 守方未被保护）⇒ 预测「星镖先打死守方 ⇒ 不吃反击」。
        {
            var atk = new UnitState(ctx.NewInstance(Unit("B94Shuriken", 1, 1, 5)), false);
            atk.AddKeyword("shuriken", 5);
            var tgt = new UnitState(ctx.NewInstance(Unit("B94ShuriTgt", 1, 0, 2)), false);

            bool noCounter = CounterAttack.NoCounter(ctx, 0, atk, 1, tgt, ranged: false, targetWillDie: false);
            CheckTrue(!noCounter,
                      "③ （前提）实际四道全不命中 ⇒ `NoCounter` = **false**（= 不吃免反击 ⇒ 会挨反击）");

            bool returns = CounterAttack.EnemyReturnsAttack(ctx, 0, atk, 1, tgt, ranged: false);
            CheckTrue(!returns,
                      "★★ ③ 预测版第 5 道：`shuriken=5` 能打死 2 血守方 ∧ 未被保护 ⇒ `EnemyReturnsAttack` = **false**"
                    + "（= 敌方**不会**反击，因为星镖先手）"
                    + "｜判据 `EntityScript__EnemyReturnsAttack.c:60-73`（第 5 道 `0x460`）"
                    + "｜🧨 删掉第 5 道 / 把 `KwValue` 当「有没有」而不看数值大小 ⇒ 实得 true ⇒ 红");
        }

        // 反向（灭自证）：同一条 shuriken 但守方 6 血（星镖打不死）⇒ 预测与实际一致 = 会反击。
        {
            var atk = new UnitState(ctx.NewInstance(Unit("B94Shuriken2", 1, 1, 5)), false);
            atk.AddKeyword("shuriken", 5);
            var tgt = new UnitState(ctx.NewInstance(Unit("B94ShuriTgt2", 1, 0, 6)), false);

            bool returns = CounterAttack.EnemyReturnsAttack(ctx, 0, atk, 1, tgt, ranged: false);
            CheckTrue(returns,
                      "★★ ③ `shuriken=5` 打不死 6 血守方 ⇒ `EnemyReturnsAttack` = **true**（会反击）"
                    + "｜🧨 第 5 道不看 `DamageKillsTarget`（只看有没有该 trait）⇒ 实得 false ⇒ 红");
        }

        // 反向（灭自证 2）：shuriken 能打死，但守方带 survivor（protected）⇒ 不豁免 ⇒ 会反击。
        //   判据：`IsProtectedFromDamageOrSurvivor` 为真 ⇒ 第 5 道**不生效**。
        {
            var atk = new UnitState(ctx.NewInstance(Unit("B94Shuriken3", 1, 1, 5)), false);
            atk.AddKeyword("shuriken", 5);
            var tgt = new UnitState(ctx.NewInstance(Unit("B94ShuriTgt3", 1, 0, 2)), false);
            tgt.AddKeyword(KeywordTable.Survivor, 1);

            bool returns = CounterAttack.EnemyReturnsAttack(ctx, 0, atk, 1, tgt, ranged: false);
            CheckTrue(returns,
                      "★★ ③ shuriken 能打死但守方带 `survivor`（受保护）⇒ 第 5 道**不豁免** ⇒ `EnemyReturnsAttack` = **true**"
                    + "（原版 `IsProtectedFromDamageOrSurvivor` 为真 ⇒ 不放行；我们走 `ConditionEval.MeetsUnit(…,120)`）"
                    + "｜🧨 第 5 道漏判「未被保护」⇒ 实得 false ⇒ 红");
        }
    }

    // ==============================================================================================
    //  ④ 主动能力 trait 六档（账 993）
    // ==============================================================================================

    static void B94F4T4ActiveAbilityTraitSixBranches()
    {
        // 阵营名 ↔ CardArmy 数值（`CardArmy.cs`）：10 Ultramarines · 80 Sororitas · 100 AstraMilitarum ·
        //   110 DarkAngels · 130 SpaceWolves。trait 值判据 = `CardTraitCollection__GetTraitForActiveAbilityRawCard.c:1-26`。
        Check(CardDef.ActiveAbilityTraitForFaction("Ultramarines").TraitValue, 1275,
              "★★ ④ Ultramarines(10) → **1275 = 0x4fb (oath)**"
            + "｜🧨 把 10 那档写错值 / 阵营名拼错 ⇒ 红");
        Check(CardDef.ActiveAbilityTraitForFaction("Sororitas").TraitValue, 1185,
              "★★ ④ Sororitas(80) → **1185 = 0x4a1 (pray)**｜🧨 见上");
        Check(CardDef.ActiveAbilityTraitForFaction("AstraMilitarum").TraitValue, 1220,
              "★★ ④ AstraMilitarum(100) → **1220 = 0x4c4 (duty)**｜🧨 见上");
        Check(CardDef.ActiveAbilityTraitForFaction("DarkAngels").TraitValue, 1235,
              "★★ ④ DarkAngels(110=0x6e) → **1235 = 0x4d3 (agenda)**｜🧨 见上");
        Check(CardDef.ActiveAbilityTraitForFaction("SpaceWolves").TraitValue, 1265,
              "★★ ④ SpaceWolves(130=0x82) → **1265 = 0x4f1 (ferocity)**｜🧨 见上");
        Check(CardDef.ActiveAbilityTraitForFaction("BlackLegion").TraitValue, 400,
              "★★ ④ 其余阵营（如 BlackLegion 50）→ **400 = activeAbility**（`.c:26` 的 default 返回）"
            + "｜🧨 把 default 写成 0 或漏写 default ⇒ 红");
    }

    // ==============================================================================================
    //  ⑤ 兵列表按进场序（账 1114 · `MinionManager.GetMinionList`）
    // ==============================================================================================

    static void B94F4T5MinionListPlayOrder()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var ps = ctx.Players[0];
        // 三张单位，进场序故意乱放（格号序 ≠ 进场序）：格 0=5 · 格 1=1 · 格 2=3。
        var u0 = new UnitState(ctx.NewInstance(Unit("B94Order5", 1, 1, 3)), false) { PlayOrder = 5 };
        var u1 = new UnitState(ctx.NewInstance(Unit("B94Order1", 1, 1, 3)), false) { PlayOrder = 1 };
        var u2 = new UnitState(ctx.NewInstance(Unit("B94Order3", 1, 1, 3)), false) { PlayOrder = 3 };
        ps.Board[0] = u0;
        ps.Board[1] = u1;
        ps.Board[2] = u2;

        var list = ps.UnitsByPlayOrder();
        var names = new List<string>();
        foreach (var u in list) names.Add(u.Name);
        Check(string.Join(",", names.ToArray()), "B94Order1,B94Order3,B94Order5",
              "★★ ⑤ `UnitsByPlayOrder` 按 `PlayOrder` **升序**（1→3→5），⛔ 不是格号序（0→1→2）"
            + "｜判据 `MinionManager__GetMinionList.c` 末段 `Enumerable.OrderBy(card => card.orderInPlay)`"
            + "｜🧨 改成按格号/按 InstanceId ⇒ 实得 5,1,3 序 ⇒ 红");

        // 反向（灭自证）：没取号的（PlayOrder=-1）排末尾。
        var u3 = new UnitState(ctx.NewInstance(Unit("B94OrderNeg", 1, 1, 3)), false);   // PlayOrder 缺省 -1
        ps.Board[3] = u3;
        var list2 = ps.UnitsByPlayOrder();
        Check(list2[list2.Count - 1].Name, "B94OrderNeg",
              "★★ ⑤ `PlayOrder < 0`（没取号）排**末尾**（`StableSortByPlayOrder` 口径）"
            + "｜🧨 把 -1 当 0 参与升序（排到最前）⇒ 红");
    }

    // ==============================================================================================
    //  ⑥ 墓地「永久放逐闩」（协调员 R-B1 补 · 账 534 · `CardScript+0x24b = shouldCheckIfIsInCemetery`）
    // ==============================================================================================

    static void B94F4T6CemeteryBanishLatch()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var ps = ctx.Players[0];
        var card = Unit("B94Banish", 1, 1, 3);
        var inst = ctx.NewInstance(card);
        ps.Discard.Add(inst);

        CheckTrue(ps.IsInCemetery(inst),
                  "⑥ （前提）在墓地里的卡 ⇒ `IsInCemetery` = true");

        ps.MarkRemovedFromCemetery(inst);   // ≡ 原版 `RemoveCardFromCemetery` 把 `+0x24b` 清 0

        CheckTrue(!ps.IsInCemetery(inst),
                  "★★ ⑥ 被 `MarkRemovedFromCemetery` 过的卡**仍在 `Discard` 里**，但 `IsInCemetery` = **false**"
                + "（原版 `+0x24b` = `shouldCheckIfIsInCemetery` 一次性清 0、全库无写回 1 ⇒ 永久放逐；"
                + "`GetCardsInCemetery.c:54` 按它过滤）"
                + "｜🧨 删掉 `&& !_banishedFromCemetery.Contains(inst.Id)` ⇒ 实得 true ⇒ 红"
                + "（= 回到「可观测效果为零」那个已被推翻的旧口径）");

        // 另一张没被放逐的卡不受影响（防「闩写成一刀切」）。
        var inst2 = ctx.NewInstance(Unit("B94Banish2", 1, 1, 3));
        ps.Discard.Add(inst2);
        CheckTrue(ps.IsInCemetery(inst2),
                  "⑥ 放逐闩只对**那一张**生效：另一张在墓地里的卡仍 = true"
                + "｜🧨 把过滤写成按卡名/全局开关 ⇒ 实得 false ⇒ 红");
    }

    // ==============================================================================================
    //  ⑦ `BattleContext.CardChosenInSelection`（协调员补 · 账 455）+
    //     `MinionManager.PositionWillBeAtLeft`（协调员补 · 账 1138）
    // ==============================================================================================

    static void B94F4T7CardChosenInSelectionAndPositionWillBeAtLeft()
    {
        // ── CardChosenInSelection：默认 null，可写可读（原版 +0x2E8 RawCardScript）──────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            CheckTrue(ctx.CardChosenInSelection == null,
                      "⑦ （前提）`CardChosenInSelection` 默认 = **null**（= 原版「当前没有选择中的卡」）");
            var c = Unit("B94ChosenCard", 1, 1, 1);
            ctx.CardChosenInSelection = c;
            CheckTrue(ReferenceEquals(ctx.CardChosenInSelection, c),
                      "★★ ⑦ `CardChosenInSelection` 可写可读（承载「选择 UI 里选中的那张卡」，"
                    + "原版 `BattleManager.cardChosenInSelection // +0x2E8`）"
                    + "｜🧨 字段名/类型改错 ⇒ 编译不过（这一条同时是「格真的在」的硬检查）");
            ctx.CardChosenInSelection = null;
            CheckTrue(ctx.CardChosenInSelection == null,
                      "⑦ 消费方用完即清（原版 `AbilityLogic.cs:1612`）⇒ 再读回 null");
        }

        // ── PositionWillBeAtLeft：三档（原版 `MinionManager__PositionWillBeAtLeft.c`）────────────
        //  格号口径 = 原版有符号（`signed = ourAbs - 4`）：左 = -4..-1 · 督军 = 0 · 右 = +1..+4。
        CheckTrue(TutorialPositioning.PositionWillBeAtLeft(-1, 0, true),
                  "★★ ⑦ ref = **督军**(0) ⇒ 只有 slot **-1** 算「左边」"
                + "｜判据 `PositionWillBeAtLeft.c:31` `slot == 0xffffffff`"
                + "｜🧨 改成「任意负格都算左边」（= 旧近似的整半场） ⇒ `-2` 那格也会 true ⇒ 下面那条红");
        CheckTrue(!TutorialPositioning.PositionWillBeAtLeft(-2, 0, true),
                  "★★ ⑦ ref = 督军(0) ⇒ slot **-2** **不**算「紧贴左边那一格」"
                + "（这正是旧近似 `TargetSlot < WarlordSlot` 会把 -2 误判成真的 bug）"
                + "｜🧨 用整半场近似 ⇒ 实得 true ⇒ 红");

        CheckTrue(TutorialPositioning.PositionWillBeAtLeft(-3, -2, false),
                  "★★ ⑦ ref 在**左表** signed -2 ⇒ 「左边那一格」= **-3**（`refSigned - 1`）"
                + "｜判据 `.c:44` `uVar4 = ~i; uVar4 -= 1` ⇒ `-(i+2)` / 左表第 i 个 = `-(i+1)`");
        CheckTrue(!TutorialPositioning.PositionWillBeAtLeft(-1, -2, false),
                  "★★ ⑦ ref 在左表 -2 ⇒ slot **-1**（更靠督军，在 ref 的**右**边）**不**算左边"
                + "｜🧨 把方向写反（`refSigned + 1`）⇒ 实得 true ⇒ 红");

        CheckTrue(TutorialPositioning.PositionWillBeAtLeft(3, 3, false),
                  "★★ ⑦ ref 在**右表** signed +3 ⇒ 在**同一格**插入会把 ref 顶出去 ⇒ slot **+3** 算「ref 左边」"
                + "｜判据 `.c:74` 命中时 `uVar4` 已 `+1`、不再 `-1` ⇒ `slot == j+1 == refSigned`"
                + "｜🧨 把右表也套用 `refSigned-1` ⇒ 实得 false ⇒ 红");

        // ActingSlot 的「未接」语义：默认 null（我们的绝对格 0 合法 ⇒ 不能用 0 当哨兵）。
        var att = new TutorialAttempt();
        CheckTrue(!att.ActingSlot.HasValue,
                  "★★ ⑦ `TutorialAttempt.ActingSlot` 默认 = **null**（未接），⛔ 不是 0"
                + "（我们绝对格 0 是合法格，用 0 当哨兵会把「ref 在 0 号格」误判成未接）");
    }
}
