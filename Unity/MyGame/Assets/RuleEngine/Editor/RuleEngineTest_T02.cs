// RuleEngineTest_T02.cs — 引擎线 `T02`「同族未补」那一批的断言（2026-10-21）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_BoardModel` / `_S14` / `_S23_*` / `_Order` / `_Diag` 都在），而**同一时刻只能有一个写手改一个文件**
// ⇒ 本批一律写在这里，`RuleEngineTest.cs` **只加 `Run()` 里那几行 `Step(...)`**。
//
// 本批补 **T02** 名下「同族还没补」的三族：
//   ① **`or` 族 · 条件里的 `or`**：`If the target dies **or** is a friendly unit`（`SOR31 Dogmata`）。
//      改前 `EffectCondition.Normalize` 只认前半句 ⇒ 归成 `targetdies`，后半句**静默消失**
//      （卡面不打 `*`、探针也报「干净」）。
//   ② **`Sacrifice:` 族 · 正文的落点**：全池 **0 张可达**（见下面那一格的说明），
//      机制用**卡面文法造出来的夹具卡**端到端钉住 —— 「`Gain +1 Attack` 的 +1 落在被献祭那张自己身上」。
//      改前 `DoGive` 的闸是 `!IsAlive`，而触发那一刻 `Health <= 0` ⇒ **+1 静默丢掉**。
//   ③ **落位锚点族 · `A1692` 的另外两个写入点**：`A1692` 当年只给 `RuleCore.DeployFree` 加了
//      「目标格被占 ⇒ 不许静默覆盖」的闸，而 `EffectResolver.DoTakeControl`（抢单位）与
//      `RuleCore.EndTurn` 的「本回合控制到期、还回去」**走的是同一个 `NextWithoutDisplacing`**、
//      **没有**那道闸 ⇒ 有洞棋盘上会**静默盖掉一个人**。判据收进 `BoardSlots.NextFreeWithoutDisplacing` 一处。
//
// 🔴 每条都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// ⚠️ 本文件里凡**改棋盘 / 直接调 `RuleCore.*`** 的，都是**夹具直调**，
//    不走 `BattleDriver.LocalAct` / `SimpleAI.ExecuteAction` 两个记账口 ⇒ 不会被录进本地录像
//    （这是断言不是对局，同 `RuleEngineTest_S14.cs` 的口径）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==================================================================
    //  ① `or` 族 · **条件里的 `or`**（`SOR31 Dogmata`）
    // ==================================================================

    /// <summary>
    /// 判据：卡面中文（`descZh`，卡面印的就是它）「如果目标**死亡或**为友方单位，获得 1 ☀」
    /// ⇒ **两个析取项都要判**；英文 `If the target dies **or** is a friendly unit`。
    /// 原版把「友方」表达成 `targetCriteria.friendlyUnits`
    /// （`cards_1195_2022时代.json` 的 `SOR31.cardAbilities[1].targetCriteria` =
    ///  `targetsAffected 10 / friendlyUnits 1 / includeHero 1 / includeMinions 1 / includeRemnants 1`）。
    /// </summary>
    static void TestT02OrConditionTargetDiesOrFriendly()
    {
        // ---- ① 解析层：条件种类必须**认得两个析取项** ----
        var probe = Tactic("T02_OrCondParse", 0,
            "Deal 1 damage to a friendly troop. If the target dies or is a friendly unit, gain 1 ☀.");
        var ops = EffectText.Parse(probe.Desc, out _, out _);
        EffectOp condOp = null;
        foreach (var o in ops)
            if (o != null && o.Verb == "gainfaith") condOp = o;
        CheckTrue(condOp != null, "（前提）「gain 1 ☀」解成一条 `gainfaith`：" + probe.Desc);
        Check(condOp == null ? "(none)" : condOp.ConditionKind, "targetdiesorfriendly",
              "★★ `T02 · or 族`：`If the target dies **or** is a friendly unit` ⇒ 条件种类 "
            + "`targetdiesorfriendly`（两个析取项都留下来了）"
            + "｜🧨 把 `EffectCondition.Normalize` 里那条新分支删掉 ⇒ 实得 `targetdies`（后半句静默丢失）");

        // ---- ② 行为层（甲）：目标是**友方且活着** ⇒ 条件成立（第一项不成立、第二项成立） ----
        {
            var t = Tactic("T02_OrCondFriend", 1,
                "Deal 1 damage to a friendly troop. If the target dies or is a friendly unit, gain 1 ☀.");
            var ctx = BattlePool(new[] { t }, new CardDef[0], new List<CardDef>());
            ToP1Turn(ctx, 1);
            ctx.Players[0].Energy = 20;
            var ally = Place(ctx, 0, 5, Unit("T02_Ally", 1, 1, 5), exhausted: true);
            int f0 = ctx.Players[0].Faith;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T02_OrCondFriend"), 5),
                      RuleCodes.OK, "打出条件夹具（目标 = 我方活着的部队）");
            CheckTrue(ally.IsAlive, "（前提）这一下没把友方打死（走「友方」那一支，不是「死了」）");
            Check(ctx.Players[0].Faith - f0, 1,
                  "★★ `T02`：目标是**友方、且活着** ⇒ 条件成立 ⇒ 信仰 **+1**"
                + "｜🧨 把 `ConditionHolds` 的 `targetdiesorfriendly` 支改回只判生死 ⇒ 本行实得 0" + LogTail(ctx));
        }

        // ---- ③ 行为层（乙）：目标是**敌方且活着** ⇒ 条件不成立（两个析取项都不成立） ----
        {
            var t = Tactic("T02_OrCondFoeAlive", 1,
                "Deal 1 damage to an enemy troop. If the target dies or is a friendly unit, gain 1 ☀.");
            var ctx = BattlePool(new[] { t }, new CardDef[0], new List<CardDef>());
            ToP1Turn(ctx, 1);
            ctx.Players[0].Energy = 20;
            var foe = Place(ctx, 1, 5, Unit("T02_FoeAlive", 1, 1, 5), exhausted: true);
            int f0 = ctx.Players[0].Faith;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T02_OrCondFoeAlive"), 5),
                      RuleCodes.OK, "打出条件夹具（目标 = 敌方活着的部队）");
            CheckTrue(foe.IsAlive, "（前提）敌方那个还活着（「死了」那一支不成立）");
            Check(ctx.Players[0].Faith - f0, 0,
                  "★★ `T02`：目标是**敌方、且活着** ⇒ 两个析取项都不成立 ⇒ 信仰 **+0**"
                + "｜🧨 把 `ConditionHolds` 那一支改成「恒 true」（或只判 `FindSlot` 成功）"
                + " ⇒ 本行实得 1" + LogTail(ctx));
        }

        // ---- ④ 行为层（丙）：目标是**敌方但被打死** ⇒ 条件成立（「死了」那一支） ----
        {
            var t = Tactic("T02_OrCondFoeDead", 1,
                "Deal 9 damage to an enemy troop. If the target dies or is a friendly unit, gain 1 ☀.");
            var ctx = BattlePool(new[] { t }, new CardDef[0], new List<CardDef>());
            ToP1Turn(ctx, 1);
            ctx.Players[0].Energy = 20;
            Place(ctx, 1, 5, Unit("T02_FoeDead", 1, 1, 5), exhausted: true);
            int f0 = ctx.Players[0].Faith;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T02_OrCondFoeDead"), 5),
                      RuleCodes.OK, "打出条件夹具（目标 = 敌方 5 血、挨 9 点）");
            Check(ctx.Players[0].Faith - f0, 1,
                  "★★ `T02`：目标**死了** ⇒ 条件成立 ⇒ 信仰 **+1**（这一半是改前就有的口径，别一起改坏）"
                + "｜🧨 把 `targetdiesorfriendly` 支里 `!t.IsAlive` 那一行删掉 ⇒ 本行实得 0" + LogTail(ctx));
        }
    }

    // ==================================================================
    //  ② `Sacrifice:` 族 —— **全池 0 张**，机制用夹具钉住语义（`T02` §一「+1 被静默丢掉」那一格）
    // ==================================================================

    /// <summary>
    /// 🔴 **可达性（先读这句）**：`cards_engine.json` 1127 张里带 `Sacrifice:` / `survivor` 的
    /// **一张都没有**（`desc` / `keywords` / `cardTraits` 三个字段扫 `sacrific` 零命中；
    /// 唯一含这个词的是**卡名** `TAU56 Valued Sacrifice`）⇒ **无可达卡**。
    /// 但 T02 点名的那条症状（「有 `Sacrifice:` 的 `+1` 被静默丢掉」）是**机制层**的问题，
    /// 而机制可以用卡面文法造出来的夹具卡端到端量（`keywords` 数组里印
    /// `"Sacrifice: Gain +1 Attack"` —— 与真卡同一套文法）。
    ///
    /// 判据：`Gain +1 Attack` **没写主语** ⇒ 拿到它的是**施放者自己**
    /// （`EffectTargetSpec.Subjectless` 的口径；原版 `AddEffect` 那一跳判的是 `cardState`
    ///  = 在不在场，`CardScript__AddEffect.c:67-75`）。触发那一刻单位 `Health <= 0`、
    /// 但**还在棋盘上**（`CheckIfDead.c:108-110` 用 `{2,3,15,17}` 判在场，触发排在状态变更之前）
    /// ⇒ `DoGive` 的闸必须是「活着 ∨ 还在棋盘上」（`A1426` 于 2026-10-19 这么改的），
    /// ⛔ 不是「删闸」（`Give +N Health` 会复活尸体）。
    /// </summary>
    static void TestT02SacrificeBodyLandsOnSelf()
    {
        var sac = new CardDef("T02_Sac", "T02_Sac", "unit", "",
                              "common", "Test", 1, 2, 5, 0,
                              new[] { "survivor 3", "Sacrifice: Gain +1 Attack" }, subtype: "Infantry");
        var sacOps = sac.TriggerOps(KeywordTable.Sacrifice);
        CheckTrue(sacOps != null && sacOps.Count == 1,
                  "（前提）`Sacrifice:` 正文收得下来（1 条 op）—— 夹具走的是与真卡同一套文法");

        var hitOwn5 = Tactic("T02_HitOwn5", 0, "Deal 5 damage to a friendly unit");
        var ctx = ProbeBattle(new[] { Unit("T02_Dummy", 1, 0, 9) }, new[] { hitOwn5 });
        ToP1Turn(ctx, 2);
        PassTurn(ctx);                                        // 推到 **P1 的回合**（牺牲只在**本方回合**触发）
        Check(ctx.Active, 1, "（前提）现在是 P1（= 这个单位的主人）的回合");

        var u = Place(ctx, 1, 3, sac, exhausted: true);
        u.Health = 2;
        var bystander = Place(ctx, 1, 2, Unit("T02_Bystander", 1, 0, 9), exhausted: true);
        int atk0 = u.Attack;
        int by0 = bystander.Attack;
        Check(atk0, 2, "（前提）被献祭那张初始攻 2");

        CheckCode(RuleCore.PlayTactic(ctx, 1, HandIdx(ctx, 1, "T02_HitOwn5"), 3), RuleCodes.OK,
                  "本方打自己 5 伤 ⇒ 救回 + `Sacrifice:` 那一跳");
        Check(u.Health, 3, "（前提）救回：生命 = 3（`survivor 3`）");
        Check(u.Attack - atk0, 1,
              "★★ `T02`：`Sacrifice: Gain +1 Attack` 的 +1 **落在被献祭那张自己**身上（攻 2 → 3）"
            + "｜🧨 把 `DoGive` 的逐目标闸改回 `!t.IsAlive` ⇒ 那一刻它 `Health <= 0` ⇒ 本行实得 0"
            + "（**这就是 T02 点名的「+1 被静默丢掉」**）" + LogTail(ctx));
        Check(bystander.Attack - by0, 0,
              "★ 灭自证：旁边那个友方单位**一点都没涨**（改前那一版会退回「己方全体」兜底）"
            + "｜🧨 把 `ResolveTargets` 的 `Subjectless` 分支改成「施放者已死 ⇒ 退回己方全体」⇒ 本行红"
            + LogTail(ctx));
    }

    // ==================================================================
    //  ③ 落位锚点族 · `A1692` 的另外两个写入点（抢单位 / 控制到期还回去）
    // ==================================================================

    /// <summary>
    /// **落位锚点族 · `A1692` 的另外两个写入点**：`A1692` 当年只给 `RuleCore.DeployFree` 加了
    /// 「目标格被占 ⇒ 不许静默覆盖」，而 `EffectResolver.DoTakeControl`（抢单位）与
    /// `RuleCore.EndTurn`（本回合控制到期、还回去）**走同一个 `NextWithoutDisplacing`**、
    /// 却没有那道闸 ⇒ 有洞棋盘上会**静默盖掉一个人**。判据现收在
    /// `BoardSlots.NextFreeWithoutDisplacing` 一处（三处共用）。
    ///
    /// **夹具共用的有洞局面**：右 index 0/2 有人（5、7 号格）、index 1（6 号）空着；
    /// 左侧摆 2 个（3、2 号格）⇒ 两侧人数打平 ⇒ `NextWithoutDisplacing` 选右（平手走右）
    /// 并返回 **7（被占）**，该侧最外的空格 = **8**。
    /// ⚠️ 有洞只可能来自夹具直接写 `Board[]`（引擎自己的增删都维持连续无洞，见 `BoardSlots.WarnHoley`）；
    ///    原版两侧是连续 `List`，**根本没有「目标格被占」这一档** ⇒ 这道闸是补我们表示法的洞。
    /// </summary>
    static void TestT02PlacementNoOverwriteFamily()
    {
        // ---- ① `BoardSlots.NextFreeWithoutDisplacing` 本体的读数（判据唯一出处） ----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_HL", 1, 1, 5));
            Place(ctx, 1, 2, Unit("T02_HL2", 1, 1, 5));
            Place(ctx, 1, 5, Unit("T02_HR1", 1, 1, 5));
            var outer = Place(ctx, 1, 7, Unit("T02_HR2", 1, 1, 5));

            Check(BoardSlots.NextWithoutDisplacing(ctx.Players[1]), 7,
                  "（前提）`NextWithoutDisplacing` 在这张有洞棋盘上**指向被占的 7 号格**"
                + "（右侧 index 0/2 有人、index 1 空着）");
            int got = BoardSlots.NextFreeWithoutDisplacing(ctx.Players[1], out int blocked);
            Check(got, 8,
                  "★★ `T02`：`NextFreeWithoutDisplacing` **不覆盖** —— 改到该侧最外的空格 **8**"
                + "｜🧨 把那个 `ps.Board[dst] == null` 早退删掉（退回裸 `NextWithoutDisplacing`）⇒ 实得 7");
            Check(blocked, 7, "★ 而且**报出被挡的那一格**（调用方据此出声）");
            CheckTrue(Board(ctx, 1, 7) == outer,
                      "★★ 灭自证：原来那一格的人**还是原来那个对象**（没被换掉）");
        }

        // ---- ② 抢单位（`EffectResolver.DoTakeControl`）:同一个闸 ----
        {
            var t = Tactic("T02_StealHole", 1, "Take control of an enemy troop this turn");
            var ctx = BattlePool(new[] { t }, new CardDef[0], new List<CardDef>());
            ToP1Turn(ctx, 1);
            ctx.Players[0].Energy = 20;
            // 抢的人（P0）棋盘：左 2 个（3、2 号格）、右 index 0/2 有人（5、7 号格）、6 号空着
            Place(ctx, 0, 3, Unit("T02_SH_L", 1, 1, 5));
            Place(ctx, 0, 2, Unit("T02_SH_L2", 1, 1, 5));
            Place(ctx, 0, 5, Unit("T02_SH_R1", 1, 1, 5));
            var outer = Place(ctx, 0, 7, Unit("T02_SH_R2", 1, 1, 5));
            // 被抢的那个在对面 5 号格
            Place(ctx, 1, 5, Unit("T02_SH_Victim", 1, 3, 5), exhausted: true);

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T02_StealHole"), 5),
                      RuleCodes.OK, "打出「抢单位」夹具（对面 5 号格）");
            Check(SlotOf(ctx, 0, "T02_SH_Victim"), 8,
                  "★★ `T02`：抢来的单位落在**该侧最外的空格 8**（目标格 7 被占 ⇒ **不覆盖**）"
                + "｜🧨 把 `DoTakeControl` 那一行改回裸 `NextWithoutDisplacing` ⇒ 实得 7" + LogTail(ctx));
            CheckTrue(SlotOf(ctx, 0, "T02_SH_R2") == 7 && Board(ctx, 0, 7) == outer,
                      "★★ 灭自证：7 号格原来那个**还在**（没被抢来的单位盖掉）");
            CheckTrue(HasLog(ctx, "不覆盖"),
                      "★ 而且**出声**（本仓红线：不许静默失败）" + LogTail(ctx));
        }

        // ---- ③ 「本回合控制到期、还回去」（`RuleCore.EndTurn`）:同一个闸 ----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            // 被抢的那个此刻在 **P0** 的 5 号格（= 「抢它的人」那一侧）
            var held = Place(ctx, 0, 5, Unit("T02_RB_Unit", 1, 3, 5));
            ctx.TempControls.Add(new BattleContext.TempControl
            {
                Unit = held, Owner = 1, Turn = ctx.Turn, Slot = 5,
            });
            // 原主（P1）棋盘：左 2 个（3、2 号格）、右 index 0/2 有人（5、7 号格）、6 号空着
            Place(ctx, 1, 3, Unit("T02_RB_L", 1, 1, 5));
            Place(ctx, 1, 2, Unit("T02_RB_L2", 1, 1, 5));
            Place(ctx, 1, 5, Unit("T02_RB_R1", 1, 1, 5));
            var outer = Place(ctx, 1, 7, Unit("T02_RB_R2", 1, 1, 5));

            int logFrom = ctx.Events.Count;
            RuleCore.EndTurn(ctx);

            Check(SlotOf(ctx, 1, "T02_RB_Unit"), 8,
                  "★★ `T02`：还回去的单位落在**原主该侧最外的空格 8**（目标格 7 被占 ⇒ **不覆盖**）"
                + "｜🧨 把 `RuleCore.EndTurn` 归还那一段改回裸 `NextWithoutDisplacing` ⇒ 实得 7"
                + "（并且 7 号格上的人被静默抹掉）" + S23RTail(ctx, logFrom));
            CheckTrue(Board(ctx, 1, 7) == outer,
                      "★★ 灭自证：7 号格原来那个**还在、还是同一个对象**" + S23RTail(ctx, logFrom));
            CheckTrue(HasLog(ctx, "不覆盖"),
                      "★ 而且**出声**（不许静默失败）" + S23RTail(ctx, logFrom));
        }
    }
}
