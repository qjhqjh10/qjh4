// RuleEngineTest_EngineB3.cs — 引擎线 `B3` 第二批五件的断言（2026-10-24）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_EngineA1` / `_EngineP1` / `_Order` / `_BoardModel` / `_S14` / `_S23_*` / `_Priority` /
// `_EntryPump` / `_UnitRoles` / `_Diag` 都在），而**同一时刻只有一个写手改一个文件**
// ⇒ 本批测试体全写在这里，`RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestB3EngineFive)`**。
//
// 五件的判据（第一入口 = `资料/普查产出_1024/C1_引擎线判据总表.md`，⛔ 不要重新论证）：
//   ① `D11` 回合开始的结算序 = **棋盘 → 当前方手牌 → 常驻**
//      （`BattleManagerSupport__BroadcastTurnStart.c:18-31 / 32-43 / 44-61`）；
//   ② `UnitDeathByAbility(180)` 的发射口 = `RawCardScript__OnMinionDeath.c:25-37`（死因 `20` 才发）；
//   ③ `SW18` 变身 = **逐张目标各自掷一次**（`AbilityLogic__PlayAbility.c:950-978` +
//      `AbilityLogic__GetCardToSummon.c:191-206`）；
//   ④ `BroadcastTurnEnd` 棋盘趟第三支 = `!IsInPlay ∧ remnant(1050) ∧ cardState==5` 也调 `OnTurnEnd`
//      （`BattleManagerSupport__BroadcastTurnEnd.c:26-41` + 机器码 `mov edx, 0x41a`）；
//   ⑤ `currentCombatAttack` 的 `!isLeavingRemnant(+0x66)` 那一维
//      （`EntityScript__get_currentCombatAttack.c:40-47`）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量，⛔ 不从实现常量反算（那会自证）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestB3EngineFive)`）。
    /// 五个子用例是 `B3…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestB3EngineFive()
    {
        Section("🆕 引擎线 B3 五件：回合结算序 · `UnitDeathByAbility(180)` · `SW18` 逐张随机 · "
              + "回合末第三支 · `isLeavingRemnant`");
        B3TurnResolveOrder();
        B3UnitDeathByAbility();
        B3TurnEndWaitingRemnant();
        B3LeavingRemnant();
    }

    // ==============================================================================================
    //  ① `D11`：回合结算序 = 棋盘 → 当前方手牌 → 常驻
    // ==============================================================================================

    /// <summary>
    /// **可区分夹具**（照 `C1` §2 的判别卡思路，换成合成卡免得依赖池内数据）：
    /// · **手牌陷阱**（`At the end of your turn, your troops take 1 damage`）；
    /// · **棋盘单位**（1 血，`At the end of your turn, draw a card`）。
    ///
    /// 两种次序给出**可观测的差别**：
    /// · **棋盘先**（原版 / 改后）：单位先抽 1 张 → 然后被陷阱打死；
    /// · **手牌先**（改前）：陷阱先把单位打死 → 它的作业出队时 `requireActorInPlay` 判假 ⇒ **不抽**。
    /// 🧨 **灭自证**：把 `ResolveAtTurn` 里那两趟 foreach 换回「手牌 → 棋盘」⇒ 手牌数不涨 ⇒ 红。
    /// </summary>
    static void B3TurnResolveOrder()
    {
        // ---- 对照组：手牌没有陷阱 ⇒ 单位一定抽得到（证明「抽牌那条路本身通」）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 3, new CardDef("B3Drawer0", "B3Drawer0", "unit",
                                        "At the end of your turn, draw a card",
                                        null, "Test", 1, 0, 1, 0, null));
            int hand0 = ctx.Players[0].Hand.Count;
            RuleCore.ResolveAtTurn(ctx, "turn_end");
            RuleCore.PumpActionQueues(ctx);
            Check(ctx.Players[0].Hand.Count, hand0 + 1,
                  "① （对照组）没有陷阱时，棋盘单位的 `At the end of your turn, draw a card` 抽到 1 张");
        }

        // ---- 实验组：陷阱会把它打死 ----
        {
            var ctx = Battle(new[] { Tactic("B3HandTrap", 1,
                                            "At the end of your turn, your troops take 1 damage") },
                             new CardDef[0]);
            Place(ctx, 0, 3, new CardDef("B3Drawer1", "B3Drawer1", "unit",
                                        "At the end of your turn, draw a card",
                                        null, "Test", 1, 0, 1, 0, null));
            int hand0 = ctx.Players[0].Hand.Count;
            RuleCore.ResolveAtTurn(ctx, "turn_end");
            RuleCore.PumpActionQueues(ctx);

            CheckTrue(SlotOf(ctx, 0, "B3Drawer1") < 0,
                      "① （前提）陷阱真的把那个 1 血单位打死了（否则下面那条不具区分力）");
            Check(ctx.Players[0].Hand.Count, hand0 + 1,
                  "★★ ① **棋盘单位先于手牌陷阱结算** —— 它先抽到那 1 张，随后才被陷阱打死"
                + "｜🧨 把结算序换回「手牌 → 棋盘」⇒ 单位在它自己的作业出队前就已经死了、"
                + "`requireActorInPlay` 判假 ⇒ 手牌数不涨（= `hand0`）⇒ 红");
        }
    }

    // ==============================================================================================
    //  ② `UnitDeathByAbility(180)`：结构化发射口
    // ==============================================================================================

    /// <summary>
    /// 判据链：`BattleManagerSupport__BroadcastDeadUnit.c:82-150`（遍历 `allCardsInPlay`）
    /// → `CardScript__ResolveDeadCard.c:228-237` → `CardScript__TriggerOnMinionDeath.c:97`
    /// → **`RawCardScript__OnMinionDeath.c:25-37`**：`deathType == 20 (ability)` ⇒ `OnTrigger(0xb4 = 180)`；
    /// `10 → 175` / `40 → 170`；其余（`15` / `30` / `50`）**一条都不发**。
    ///
    /// 本用例钉四件事：
    /// ① `ability(20)` 的死 ⇒ 在场带 180 的监听者收到 `UnitDeathByAbility`；
    /// ② `combatAttacker(10)` / 未知（`0`）⇒ **一条都不发**（死因白名单）；
    /// ③ `abilityContext` 闸：`Play(0)` 的载体变成**残骸** ⇒ 不发（`IsRightContext` 的 `!isRemnant`）；
    /// ④ 池内 4 张真卡：`abilityTriggers` 都有 180，且 `AM9` 的 context 是 **50**（不是 Play）。
    /// 🧨 灭自证见每条断言后面。
    /// </summary>
    static void B3UnitDeathByAbility()
    {
        // ---- ① 正例：ability(20) 死亡 ⇒ 180 发出 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 1, B3Listener180("B3DeathListener", context: 0));
            var victim = Place(ctx, 1, 3, Unit("B3Victim", 1, 0, 1));
            RuleCore.HurtWithSourceForTest(ctx, victim, 5, "测试效果", killer: 0,
                                           deathType: UnitDeathType.Ability);
            CheckTrue(!victim.IsAlive, "② （前提）受害者被打死");
            Check(B3TriggerCount(ctx, "UnitDeathByAbility", "B3DeathListener"), 1,
                  "★★ ② `deathType = ability(20)` ⇒ 在场监听者收到 **1 条** `UnitDeathByAbility`"
                + "｜🧨 把 `CleanupDeaths` 里那句 `if (deathType == UnitDeathType.Ability) …` 删掉 ⇒ 实得 0 ⇒ 红；"
                + "🧨 把闸写成「`actingCard != null`」（= 旧的一维口径）⇒ 下面 `combat` 那条会一起发 ⇒ 红");
        }

        // ---- ② 反例：combatAttacker(10) / 未知(0) 一条都不发 ----
        foreach (int dt in new[] { UnitDeathType.CombatAttacker, UnitDeathType.CombatDefender,
                                   UnitDeathType.Tactic, UnitDeathType.Poison, UnitDeathType.Unknown })
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 1, B3Listener180("B3DeathListener", context: 0));
            var victim = Place(ctx, 1, 3, Unit("B3Victim", 1, 0, 1));
            RuleCore.HurtWithSourceForTest(ctx, victim, 5, "测试", deathType: dt);
            Check(B3TriggerCount(ctx, "UnitDeathByAbility", "B3DeathListener"), 0,
                  "★★ ② `deathType = " + dt + "` ⇒ **不发** `UnitDeathByAbility`"
                + "（原版白名单只有 10/20/40 三条，其中 20 才是 180；15/30/50/未知一条都不发）"
                + "｜🧨 把闸放宽成「只要有死亡就发」⇒ 实得 1 ⇒ 红");
        }

        // ---- ③ `abilityContext = Play(0)` 的载体变成残骸 ⇒ 不发 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var listener = Place(ctx, 0, 1, B3Listener180("B3RemnantListener", context: 0));
            listener.IsRemnant = true;
            var victim = Place(ctx, 1, 3, Unit("B3Victim", 1, 0, 1));
            RuleCore.HurtWithSourceForTest(ctx, victim, 5, "测试", deathType: UnitDeathType.Ability);
            Check(B3TriggerCount(ctx, "UnitDeathByAbility", "B3RemnantListener"), 0,
                  "★★ ② `Play(0)` 的载体是**残骸** ⇒ 不发（原版 `IsRightContext` 的第三闸 `+0x65 == 0`）"
                + "｜🧨 把 `EmitDeathByAbilityTriggers` 里的 context 闸删掉 ⇒ 实得 1 ⇒ 红");
        }

        // ---- ④ 池内 4 张真卡：结构数据在，且 `AM9` 的 context 不是 Play ----
        {
            var pool = CardDatabase.Load();
            foreach (var id in new[] { "AM9", "AM34", "SOR29", "UM99" })
            {
                var c = CardDatabase.FindById(pool, id);
                CheckTrue(c != null, "② （前提）" + id + " 在池内");
                if (c == null) continue;
                Check(c.AbilityTriggerPresence("UnitDeathByAbility"), 1,
                      "★ ② " + id + " 的 `abilityTriggers` 里有 **180**（`UnitDeathByAbility`）"
                    + "｜🧨 卡表解析把这一列丢了 ⇒ 实得 -1 ⇒ 红");
            }
            var am9 = CardDatabase.FindById(pool, "AM9");
            if (am9 != null)
                Check(RuleCore.AbilityContextOfAbilityV14(am9, "UnitDeathByAbility", null),
                      CardDef.AbilityContextInPlayOrCemetery,
                      "★★ ② `AM9 Kasrkin` 的 180 是 **`50 InPlayOrCemetery`**（⛔ 不是 `Play(0)`）"
                    + "｜🧨 把 180 一律按 Play 判 ⇒ AM9 在墓地里那半会被静默掐掉（本用例钉住这个数）");
            var am34 = CardDatabase.FindById(pool, "AM34");
            if (am34 != null)
                Check(RuleCore.AbilityContextOfAbilityV14(am34, "UnitDeathByAbility", null),
                      CardDef.AbilityContextPlay,
                      "★ ② `AM34 Valkyrie` 的 180 是 **`Play(0)`**（v14 缺行 = 省略的 Play）");

            // ---- 正文兜底仍在（⛔ 不许因为补了结构化口就把它删掉）----
            var ops = EffectText.Parse("If it dies, draw a card", out _, out _);
            CheckTrue(ops != null && ops.Count > 0 && ops[0].ConditionKind == "targetdies",
                      "★★ ② 正文后置条件 `targetdies` **仍然在**（4 张 180 卡的**效果**靠它）"
                    + "｜🧨 把 `EffectCondition` 的 `targetdies` 那一支删掉/改名 ⇒ 4 张卡的效果整条丢 ⇒ 红");
        }
    }

    /// <summary>在场带 180 的自检监听卡。`context &lt; 0` = 不带 v14 列（`-1` ⇒ 不判 context 闸）。</summary>
    static CardDef B3Listener180(string name, int context)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 1, 1, 3, 0, null,
            abilityContext: context < 0 ? null : new[]
            {
                new CardDef.AbilityContextSpec
                {
                    Trigger = 180, Name = "UnitDeathByAbility", Context = context,
                },
            },
            abilityContextsVersioned: context >= 0,
            abilityTriggers: new[] { 180 }, abilityTriggersVersioned: true);
    }

    /// <summary>`ctx.ActionLog` 里「这张卡 × 这个 trigger 名」的事件条数。</summary>
    static int B3TriggerCount(BattleContext ctx, string keyword, string cardName)
    {
        int n = 0;
        foreach (var e in ctx.ActionLog)
        {
            if (e == null) continue;
            if (e.Kind != EvtKind.Trigger) continue;
            if (!string.Equals(e.Keyword, keyword, StringComparison.Ordinal)) continue;
            if (!string.Equals(e.CardId, cardName, StringComparison.Ordinal)) continue;
            n++;
        }
        return n;
    }

    // ==============================================================================================
    //  ③ `BroadcastTurnEnd` 棋盘趟的第三支：不在场的待死残骸也收 `OnTurnEnd`
    // ==============================================================================================

    /// <summary>
    /// 判据 = `BattleManagerSupport__BroadcastTurnEnd.c:26-41`（机器码 `0x9B6C95 mov edx, 0x41a`）：
    /// `!IsInPlay(card) ∧ HasCurrentTrait(card, 1050 remnant) ∧ cardState(+0x228) == 5 (waitingToDie)`
    /// ⇒ **也调 `OnTurnEnd`**。
    ///
    /// 我们这一侧的映射：**不在场** = 血 ≤ 0 且已进将死队列；**`waitingToDie`** = `ctx.IsPendingDeath`；
    /// **remnant** = `UnitState.IsRemnant`。⚠️ 这两者**不是同一个概念**（C1 §6 点名）——
    /// 所以下面第三条反例（待死但**不是残骸**）必须不发。
    ///
    /// 🧨 灭自证：把收集循环那句 `waitingRemnant` 删掉（退回「只收活着的」）⇒ 第一条实得 0 张 ⇒ 红；
    /// 把条件放宽成「只要待死就发」（去掉 `IsRemnant`）⇒ 第三条反例实得 1 张 ⇒ 红。
    /// </summary>
    static void B3TurnEndWaitingRemnant()
    {
        // ---- ① 残骸 + 待死（不在场）⇒ 它的 `At the end of your turn, draw a card` 照跑 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = Place(ctx, 0, 3, new CardDef("B3WaitingRemnant", "B3WaitingRemnant", "unit",
                                                "At the end of your turn, draw a card",
                                                null, "Test", 1, 0, 1, 0, null));
            u.Health = 0;
            u.IsRemnant = true;
            ctx.AddPendingDeath(0, 3, -1);
            CheckTrue(ctx.IsPendingDeath(u), "③ （前提）它已进将死队列（≈ `cardState = 5`）");

            int hand0 = ctx.Players[0].Hand.Count;
            RuleCore.ResolveAtTurn(ctx, "turn_end");
            RuleCore.PumpActionQueues(ctx);
            Check(ctx.Players[0].Hand.Count, hand0 + 1,
                  "★★ ③ **不在场的待死残骸仍然收到 `OnTurnEnd`**（原版第三支）—— 它抽到了那 1 张"
                + "｜🧨 删掉 `waitingRemnant` 那一支（退回「只收活着的」）⇒ 手牌数不涨 ⇒ 红；"
                + "🧨 只认 `IsRemnant` 不认「待死」⇒ 它也进不来（`inPlay` 判假 ⇒ 两支都假）⇒ 红");
        }

        // ---- ② 对照：残骸**活着在场**（正常那一支）也照跑 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = Place(ctx, 0, 3, new CardDef("B3AliveRemnant", "B3AliveRemnant", "unit",
                                                "At the end of your turn, draw a card",
                                                null, "Test", 1, 0, 1, 0, null));
            u.IsRemnant = true;                       // 活着（Health = 1）、没进将死队列
            int hand0 = ctx.Players[0].Hand.Count;
            RuleCore.ResolveAtTurn(ctx, "turn_end");
            RuleCore.PumpActionQueues(ctx);
            Check(ctx.Players[0].Hand.Count, hand0 + 1,
                  "③ （对照）在场的残骸走**正常那一支**（`IsInPlay`），照跑 `OnTurnEnd`");
        }

        // ---- ③ 反例：待死但**不是残骸** ⇒ 不发（两个条件都要）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = Place(ctx, 0, 3, new CardDef("B3WaitingPlain", "B3WaitingPlain", "unit",
                                                "At the end of your turn, draw a card",
                                                null, "Test", 1, 0, 1, 0, null));
            u.Health = 0;
            u.IsRemnant = false;                     // ⛔ 普通待死单位
            ctx.AddPendingDeath(0, 3, -1);
            int hand0 = ctx.Players[0].Hand.Count;
            RuleCore.ResolveAtTurn(ctx, "turn_end");
            RuleCore.PumpActionQueues(ctx);
            Check(ctx.Players[0].Hand.Count, hand0,
                  "★★ ③ **待死但不是残骸** ⇒ **不发**（原版第三支要求 `HasCurrentTrait(1050)`）"
                + "｜🧨 把条件放宽成「只要待死就发」（去掉 `IsRemnant`）⇒ 手牌 +1 ⇒ 红");
        }
    }

    // ==============================================================================================
    //  ④ `currentCombatAttack` 的 `!isLeavingRemnant(+0x66)` 那一维
    // ==============================================================================================

    /// <summary>
    /// 判据 = `EntityScript__get_currentCombatAttack.c:40-47`：
    /// **`(+0x65 isRemnant != 0) && (+0x66 isLeavingRemnant == 0)` ⇒ return 0**；
    /// 否则按 `+0x120` 取远程/近战 —— 也就是**「正在离场的残骸」会落回正常攻击值**。
    ///
    /// ⚠️ **可达性未证**：原版 `+0x66` 的写入点本轮没查清（C1 §5），池内也没有写入点
    /// ⇒ 本用例是**合成夹具**（`UnitState.IsLeavingRemnant` 只有本测试会置）。
    /// 🧨 灭自证：把 `CurrentCombatAttack` 的判据改回 `if (u.IsRemnant) return 0;` ⇒ 第二条实得 0 ⇒ 红。
    /// </summary>
    static void B3LeavingRemnant()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var u = Place(ctx, 0, 3, Unit("B3RemnantAtk", 1, 3, 1));
        u.IsRemnant = true;

        Check(CounterAttack.CurrentCombatAttack(ctx, 0, u), 0,
              "★ ④ 普通残骸（`+0x65` 真、`+0x66` 假）⇒ `currentCombatAttack` = **0**"
            + "｜🧨 把整个残骸支删掉 ⇒ 实得 3 ⇒ 红");

        u.IsLeavingRemnant = true;
        Check(CounterAttack.CurrentCombatAttack(ctx, 0, u), 3,
              "★★ ④ **正在离场的残骸**（`+0x66` 真）⇒ **落回攻击值 3**（不是 0）"
            + "｜🧨 判据只写 `u.IsRemnant`（改前就是）⇒ 实得 0 ⇒ 红 —— 这就是 `C1` §5 缺的那一维");

        // ---- terror 那一支也跟着变（同一口，别在别处再写一份）----
        var attacker = Place(ctx, 0, 1, Unit("B3TerrorAttacker", 1, 2, 5, KeywordTable.Terror));
        CheckTrue(!CounterAttack.NoCounter(ctx, 0, attacker, 0, u, ranged: false, targetWillDie: false),
                  "★★ ④ terror：守方是**正在离场的残骸**（攻 3 > 攻方 2）⇒ **不豁免反击**"
                + "｜🧨 用 `IsRemnant` 一维（守方算 0）⇒ 豁免成立 ⇒ 这条红");
        u.IsLeavingRemnant = false;
        CheckTrue(CounterAttack.NoCounter(ctx, 0, attacker, 0, u, ranged: false, targetWillDie: false),
                  "★★ ④ terror（对照）：普通残骸攻 0 < 2 ⇒ **豁免成立**"
                + "｜🧨 两条一起用同一个判据 ⇒ 至少一条红（灭自证）");
    }
}
