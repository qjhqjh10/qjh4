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
//   ④ 🆕 **2026-10-22（`W63` 普查）：`SimpleAI.FirstFreeSlot` 是落位判据的第四处写入点**，
//      `T02` 当时没收口 ⇒ 有洞棋盘上它会返回**被占格**（同上后果）。判据同样是
//      `BoardSlots.NextFreeWithoutDisplacing`（⛔ 不另写一份「空格 = 没单位」）。
//   ⑤ 🆕 **2026-10-22（`W63` 普查第二批）：`SimpleAI.NextDeploySlot` 的第三支（平衡）是第五处**
//      —— 它原来写裸算式 `WarlordSlot ± (CountSide+1)`（∝ 原版 `GetNextSlotWithoutDisplacing`），
//      而 `CountSide` 只数「活着且连续」的那一段 ⇒ 段首那具**非空但已死**的单位会让它提前停、
//      指向那一格；再喂给 `RuleCore.PlayCard` 就会**静默抹掉**外侧单位（`A1692` 的原始症状）。
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

    // ==================================================================
    //  ④ 落位锚点族 · `SimpleAI.FirstFreeSlot`（`T02` 普查点名的**最后一处残留**，2026-10-22）
    // ==================================================================

    /// <summary>
    /// **落位判据的第四个写入点 = `SimpleAI.FirstFreeSlot`**。`T02`（2026-10-21）把
    /// `RuleCore.DeployFree` / `EffectResolver.DoTakeControl` / `RuleCore.EndTurn` 三处收口到
    /// `BoardSlots.NextFreeWithoutDisplacing`；2026-10-22 的 `W63` 普查查出**这一处还是裸的
    /// `NextWithoutDisplacing`**（`Data/SimpleAI.cs:1486`）⇒ 有洞棋盘上会返回**被占格**，
    /// 调用方（演示 / 自检 / `BattleDriver` 的拖放兜底）再把它喂给 `RuleCore.PlayCard`，
    /// `BoardSlots.Insert` 的「有洞降级」分支就会把外侧那个单位**静默抹掉**。
    ///
    /// 格号（`ourSlot = 4 + 有符号下标`）：左 `3 2 1 0`、督军 `4`、右 `5 6 7 8`。
    /// 判据出处 = `MinionManager__GetNextSlotWithoutDisplacing.c:14-30`（人少那侧的最外一格、
    /// 平手走右）＋ `BoardSlots.NextFreeWithoutDisplacing`（**唯一**的「真的可占」判据）。
    /// ⚠️ 原版两侧是连续 `List`、**没有「目标格被占」这一档** ⇒ 换格是补**我们表示法**的洞，
    ///    ⛔ 不是「照原版」（同 `A1692` 的措辞口径）。
    /// </summary>
    static void TestT02FirstFreeSlotHonoursAvailability()
    {
        // ---- ① 连续棋盘：**逐字不变**（人少那侧的最外一格）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_FF_OK1", 1, 1, 5));
            Place(ctx, 1, 2, Unit("T02_FF_OK2", 1, 1, 5));
            Place(ctx, 1, 5, Unit("T02_FF_OK3", 1, 1, 5));
            Place(ctx, 1, 6, Unit("T02_FF_OK4", 1, 1, 5));
            Place(ctx, 1, 7, Unit("T02_FF_OK5", 1, 1, 5));
            CheckTrue(BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）① 这块棋盘**连续无洞**（左 2 个 / 右 3 个）");
            Check(BoardSlots.NextWithoutDisplacing(ctx.Players[1]), 1,
                  "（前提）人少侧 = 左 ⇒ 裸算法给 1 号格（本来就空着）");
            Check(SimpleAI.FirstFreeSlot(ctx.Players[1]), 1,
                  "★ 连续棋盘：`FirstFreeSlot` **逐字不变**（还是 1 号格）"
                + "｜🧨 把实现换成「从 0 号格起找第一个空格」⇒ 实得 0");
        }

        // ---- ② 有洞（两侧平手 ⇒ 走右）：裸算法指向**被占**的 7 号格，而 6 号才是那个洞 ----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_FF_H1", 1, 1, 5));
            Place(ctx, 1, 2, Unit("T02_FF_H2", 1, 1, 5));               // 左 2 个（连续）
            Place(ctx, 1, 5, Unit("T02_FF_H3", 1, 1, 5));               // 右 index 0
            var outer = Place(ctx, 1, 7, Unit("T02_FF_H4", 1, 1, 5));   // 右 index 2 —— **6 号格是洞**

            CheckTrue(!BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）② 这是一块**有洞**棋盘（夹具直接写 `Board[]`：右 6 号格空着、7 号格有人）");
            Check(BoardSlots.NextWithoutDisplacing(ctx.Players[1]), 7,
                  "（前提）两侧平手 ⇒ 走右；裸算法指向**已经被占**的 7 号格");
            int got = SimpleAI.FirstFreeSlot(ctx.Players[1]);
            Check(got, 8,
                  "★★ `T02`：`FirstFreeSlot` **绝不返回被占格** —— 改到该侧最外的空格 **8**（裸算法给 7 ≠ 8）"
                + "｜🧨 改回裸 `NextWithoutDisplacing` ⇒ 实得 7（被占，随后 `Insert` 会抹掉 7 号格上的人）");
            CheckTrue(Board(ctx, 1, got) == null,
                      "★★ 灭自证：返回的那一格**确实是空的**（不是碰巧落在一个本来就空的地方）");
            CheckTrue(got != 6,
                      "★★ 灭自证：**不是**「找第一个洞」（6 号格正是那个洞）—— 判据是 `NextFreeWithoutDisplacing` 的"
                    + "「该侧**最外**的空格」，⛔ 别在这条路上另发明一套扫描顺序"
                    + "｜🧨 把实现写成「从督军往外找第一个空格」⇒ 实得 6");
            CheckTrue(Board(ctx, 1, 7) == outer,
                      "★★ 灭自证：7 号格原来那个**还是原来那个对象**（查询不许动棋盘）");
            Check(SimpleAI.FirstFreeSlot(ctx.Players[1]), got,
                  "★ 纯查询：连调两次给同一个答案（幂等、不写盘）");
        }

        // ---- ③ 有洞（左少 ⇒ 走左）：换一侧再钉（防「硬编码 8 / 只处理右侧」）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_FF_L1", 1, 1, 5));               // 左 index 0
            Place(ctx, 1, 1, Unit("T02_FF_L2", 1, 1, 5));               // 左 index 2 —— **2 号格是洞**
            Place(ctx, 1, 5, Unit("T02_FF_L3", 1, 1, 5));
            Place(ctx, 1, 6, Unit("T02_FF_L4", 1, 1, 5));
            Place(ctx, 1, 7, Unit("T02_FF_L5", 1, 1, 5));               // 右 3 个（连续）

            CheckTrue(!BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）③ 有洞棋盘（左 2 号格空着）");
            Check(BoardSlots.NextWithoutDisplacing(ctx.Players[1]), 1,
                  "（前提）左（2）< 右（3）⇒ 走左；裸算法指向**已经被占**的 1 号格");
            Check(SimpleAI.FirstFreeSlot(ctx.Players[1]), 0,
                  "★★ `T02`：换到左侧 ⇒ 该侧最外的空格 **0**"
                + "｜🧨 改回裸 `NextWithoutDisplacing` ⇒ 实得 1（被占）；"
                + "🧨 若写成「从督军往外找第一个空格」⇒ 实得 2（不是共用判据的口径）");
        }

        // ---- ④ 端到端：把这个落点喂给 `PlayCard`，**谁都不许被抹掉**（`A1692` 的原始症状）----
        {
            // ⚠️ 用 `BattlePool`（带 20 张 filler）而不是 `ProbeBattle`：本块要推到**第 4 回合**，
            //    空牌库会在抽牌时进疲劳分支 —— 那是与本断言无关的噪声。
            var ctx = BattlePool(new CardDef[0], new[] { Unit("T02_FF_Play", 1, 1, 5) }, new List<CardDef>());
            ToP1Turn(ctx, 2);
            PassTurn(ctx);                                   // 推到 **P1 的回合**
            Check(ctx.Active, 1, "（前提）现在是 P1 的回合");
            ctx.Players[1].Energy = 20;
            Place(ctx, 1, 3, Unit("T02_FF_E1", 1, 1, 5));
            Place(ctx, 1, 2, Unit("T02_FF_E2", 1, 1, 5));
            Place(ctx, 1, 5, Unit("T02_FF_E3", 1, 1, 5));
            var outer = Place(ctx, 1, 7, Unit("T02_FF_E4", 1, 1, 5));
            int idx = HandIdx(ctx, 1, "T02_FF_Play");
            CheckTrue(idx >= 0, "（前提）那张单位卡在 P1 手里");

            int dst = SimpleAI.FirstFreeSlot(ctx.Players[1]);
            Check(dst, 8, "（前提）`FirstFreeSlot` 给 8 号格（有洞 + 平手走右）");
            int landed;
            int code = RuleCore.PlayCard(ctx, 1, idx, dst, out landed);
            CheckCode(code, RuleCodes.OK, "打出单位卡（落点 = `FirstFreeSlot` 给的 " + dst + "）");
            Check(landed, 8, "★★ 端到端：真落在 **8** 号格");
            CheckTrue(Board(ctx, 1, 7) == outer,
                      "★★ 灭自证（`A1692` 的原始症状）：7 号格上那个单位**还在、还是同一个对象** —— "
                    + "改回裸 `NextWithoutDisplacing` ⇒ `FirstFreeSlot` 给 7（被占）⇒ `Insert` 的"
                    + "有洞降级路径落到插推、把 7 号格的人**静默抹掉** ⇒ 本行红");
            Check(SlotOf(ctx, 1, "T02_FF_Play"), 8, "★ 新单位落在 8 号格（没顶掉任何人）");
        }
    }

    // ==================================================================
    //  ⑤ 落位锚点族 · `SimpleAI.NextDeploySlot` 的**第三支（平衡）**（2026-10-22 `W63`）
    // ==================================================================

    /// <summary>
    /// **落位判据的第五处写入点 = `SimpleAI.NextDeploySlot` 的第三支「平衡」**
    /// （`Data/SimpleAI.cs` 的 `// ---- 3. 平衡` 那一段）。
    ///
    /// 原版那一步就是 `GetNextSlotCheckingAdjacents.c:44` 的那一跳 `GetNextSlotWithoutDisplacing`
    /// （`MinionManager__GetNextSlotWithoutDisplacing.c:14-30`：**只比两条 `List.Count`**，
    /// 左 &lt; 右 ⇒ 左、否则右，落点 = 该侧最外那一格）—— **原版没有「目标格被占」这一档**，
    /// 因为原版两侧是 `List`、结构上不可能有洞。
    ///
    /// 我们的差异（**只可能出现在非法局面上**）：
    ///   · `CountSide` 数的是「贴督军起**连续且 `IsAlive`**」的段 ⇒ 段首那具**非空但已死**的
    ///     单位会让它**提前停** ⇒ 裸算式 `WarlordSlot ± (count+1)` 指到**那一格（被占）**；
    ///   · `Board[]` 是定长数组，**自检夹具**可以直接写出洞。
    /// ⇒ 后果：`RuleCore.PlayCard` 把这个落点喂给 `BoardSlots.Insert`，有洞时降级路径落到插推
    ///   ⇒ **外侧单位被静默抹掉**（与 `A1692` / `FirstFreeSlot` 同一族）。
    ///
    /// 判据 = `BoardSlots.NextFreeWithoutDisplacing`（⛔ 不另写一份「空格 = 没单位」）。
    /// ⚠️ **原版行为【不可比】** —— 换格是补**我们表示法**的洞，⛔ 不是「照原版」。
    /// </summary>
    static void TestT02NextDeploySlotHonoursAvailability()
    {
        // ---- ① 连续棋盘：**逐字不变**（左 1 < 右 2 ⇒ 左二格）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_ND_OK1", 1, 1, 5));
            Place(ctx, 1, 5, Unit("T02_ND_OK2", 1, 1, 5));
            Place(ctx, 1, 6, Unit("T02_ND_OK3", 1, 1, 5));
            CheckTrue(BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）① 这块棋盘**连续无洞**（左 1 / 右 2）");
            Check(SimpleAI.NextDeploySlot(ctx, 1), 2,
                  "★ 连续棋盘：平衡支**逐字不变**（左 1 < 右 2 ⇒ 左二格 2）"
                + "｜🧨 把第三支改成「恒走右」⇒ 实得 7");
        }

        // ---- ② 缺陷本体：段首那具「非空但已死」的单位（`CountSide` 只数活人 ⇒ 提前停）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var corpse = Place(ctx, 1, 3, Unit("T02_ND_Corpse", 1, 1, 5));
            corpse.Health = 0;          // 死了但**还留在 `Board[]` 上**（引擎自己的死亡路径当场 `RemoveAt`）
            Place(ctx, 1, 5, Unit("T02_ND_R1", 1, 1, 5));
            Place(ctx, 1, 6, Unit("T02_ND_R2", 1, 1, 5));
            Place(ctx, 1, 7, Unit("T02_ND_R3", 1, 1, 5));

            CheckTrue(!corpse.IsAlive,
                      "（前提）② 3 号格上是一具**非空但 `IsAlive == false`** 的单位（夹具写的非法局面）");
            CheckTrue(Board(ctx, 1, 3) == corpse,
                      "（前提）它**还在** `Board[3]` 上（所以 `CountSide` 会因 `!IsAlive` 提前停、左侧活人数 = 0）");
            int bare = BoardSpec.WarlordSlot - (0 + 1);   // 裸算式：左连续活人 0 ⇒ 3 号格
            Check(bare, 3, "（前提）裸算式 `WarlordSlot − (左连续活人数 + 1)` 这次给 **3**（= 尸体那格）");

            int got = SimpleAI.NextDeploySlot(ctx, 1);
            Check(got, 2,
                  "★★ `T02`：`NextDeploySlot` 的**平衡支绝不返回被占格** —— 改给左侧最外的空格 **2**"
                + "｜🧨 改回裸 `WarlordSlot ± (CountSide+1)` ⇒ 实得 3（那格上是尸体）");
            CheckTrue(got != bare,
                      "★★ 灭自证：判据**不是**那套裸算式（裸算式给 " + bare + "、实得 " + got
                    + "）—— 走的是 `BoardSlots.NextFreeWithoutDisplacing`"
                    + "｜🧨 把它换回 `IsDeployable(WarlordSlot ± (count+1))` ⇒ 本行红（3 == 3）");
            CheckTrue(Board(ctx, 1, got) == null,
                      "★★ 灭自证：返回的那一格**确实是空的**（不是碰巧落在一个本来就空的地方）");
            CheckTrue(Board(ctx, 1, 3) == corpse,
                      "★★ 灭自证：查询**不许动棋盘**（尸体还在 3 号格原处、还是同一个对象）");
            Check(SimpleAI.NextDeploySlot(ctx, 1), got,
                  "★ 纯查询：连调两次给同一个答案（幂等、不写盘）");
        }

        // ---- ③ 端到端：把这个落点喂给 `PlayCard`，**谁都不许被抹掉**（`A1692` 的原始症状）----
        {
            // ⚠️ 用 `BattlePool`（带 20 张 filler）：本块要推到**第 2 个回合**并真打一张牌，
            //    空牌库会在抽牌时进疲劳分支 —— 那是与本断言无关的噪声（同 `TestT02FirstFreeSlotHonoursAvailability` ④）。
            var ctx = BattlePool(new CardDef[0], new[] { Unit("T02_ND_Play", 1, 1, 5) }, new List<CardDef>());
            ToP1Turn(ctx, 2);
            PassTurn(ctx);                                   // 推到 P1 的回合
            Check(ctx.Active, 1, "（前提）现在是 P1 的回合");
            ctx.Players[1].Energy = 20;
            var corpse = Place(ctx, 1, 3, Unit("T02_ND_E0", 1, 1, 5));
            corpse.Health = 0;                               // 段首那具尸体（2 号格是洞）
            var inner = Place(ctx, 1, 1, Unit("T02_ND_E1", 1, 1, 5));
            var outer = Place(ctx, 1, 0, Unit("T02_ND_E2", 1, 1, 5));
            Place(ctx, 1, 5, Unit("T02_ND_E3", 1, 1, 5));
            int idx = HandIdx(ctx, 1, "T02_ND_Play");
            CheckTrue(idx >= 0, "（前提）那张单位卡在 P1 手里");

            int dst = SimpleAI.NextDeploySlot(ctx, 1);
            Check(dst, 6,
                  "（前提）`NextDeploySlot` 给 **6** 号格（左侧**非空数** 3 ⇒ 平衡走右，右侧只有 1 个）"
                + "｜🧨 改回裸 `CountSide` 算式 ⇒ 实得 3（尸体那格）");
            int landed;
            int code = RuleCore.PlayCard(ctx, 1, idx, dst, out landed);
            CheckCode(code, RuleCodes.OK, "打出单位卡（落点 = `NextDeploySlot` 给的 " + dst + "）");
            CheckTrue(Board(ctx, 1, 0) == outer,
                      "★★ 灭自证（`A1692` 的原始症状）：0 号格上那个单位**还在、还是同一个对象** —— "
                    + "改回裸算式 ⇒ 落点 3（尸体）⇒ `Insert` 的有洞降级路径落到插推、"
                    + "把 0 号格的人**静默抹掉** ⇒ 本行红");
            CheckTrue(Board(ctx, 1, 1) == inner,
                      "★★ 1 号格上那个也没被挪走（插推会把整段外移一格）");
            Check(SlotOf(ctx, 1, "T02_ND_Play"), 6, "★ 新单位落在 6 号格（没顶掉任何人）");
        }
    }

    // ==================================================================
    //  ⑦ 落位锚点族 · `BoardSlots.Resolve` 的**督军格退化路**（`W63` 点名的最后一笔，2026-10-22）
    // ==================================================================

    /// <summary>
    /// **`BoardSlots.Resolve` 的督军格分支**（请求格 = 4 ⇒ 「人少那侧的最外一格」，平手走右）。
    ///
    /// 原版判据：`MinionManager__AdjustedSlot.c:45-61` —— `slot == 0` 时 `LogWarning`，
    /// 左 `List.Count` &lt; 右 ⇒ 左、否则右（**平手也走右**），落点 = 该侧 `List.Count` 那个下标
    /// （左 `:72` 返回 `~leftMinions.Count`、右 `:107` 返回 `rightOccupation.Count + 1`）。
    /// 同一个选侧规则也在 `GetNextSlotWithoutDisplacing.c:14-30`。
    ///
    /// 我们的差异（**只可能出现在非法局面上**）：原版两侧是**连续 `List`**，人数 = 占了哪几格；
    /// 我们的 `Board[]` 是定长数组、**自检夹具**可以直接写出洞（`BoardSlots.WarnHoley`）⇒
    /// `CountOnSide(side)` 算出的下标**可能已经有人**，而 `Insert` 的插推路径会**静默抹掉**那一格的人
    /// （`A1692` 的原始症状）。判据收口到 `BoardSlots.NextFreeWithoutDisplacing`（同族六处共用）。
    ///
    /// ⚠️ **原版行为【不可比】**：原版根本没有「目标格被占」这一档
    ///    （`GetNextSlotWithoutDisplacing.c:14-30` 全文只比两个 `List.Count`）⇒
    ///    换格是补**我们表示法**的洞，⛔ 不是「照原版」。
    /// ⚠️ 有洞时那一句提示是 `UnityEngine.Debug.LogWarning`（`BoardSlots` 里没有 `ctx`）——
    ///    引擎自检捕不到 Unity 日志，本文件**不断言**它（同 `SimpleAI.FirstFreeSlot` 那两处）。
    /// </summary>
    static void TestT02ResolveWarlordSlotHonoursAvailability()
    {
        // ---- ① 连续棋盘：**逐字不变**（钉老行为：左 < 右 ⇒ 左；平手走右；落点 = 该侧最外那一格）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_RW_C1", 1, 1, 5));               // 左 1
            Place(ctx, 1, 5, Unit("T02_RW_C2", 1, 1, 5));
            Place(ctx, 1, 6, Unit("T02_RW_C3", 1, 1, 5));               // 右 2
            CheckTrue(BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）① 连续无洞（左 1 / 右 2）");
            bool ok = BoardSlots.Resolve(ctx.Players[1], BoardSpec.WarlordSlot, out int side, out int index);
            CheckTrue(ok, "（前提）① 连续棋盘上督军格能落");
            Check(side, BoardSlots.Left, "★ 连续棋盘：左（1）< 右（2）⇒ 选**左**（逐字不变）");
            Check(index, 1, "★ 落点下标 = 该侧人数 **1**（逐字不变）"
                          + "｜🧨 把选侧规则改成「恒走右」或把下标改成别的算法 ⇒ 本行红");
            Check(BoardSlots.SlotOf(side, index), 2,
                  "★ 格号 = **2**（左侧第 2 格，空着）");
            Check(Board(ctx, 1, 2), null, "★ 而且那一格**确实是空的**");

            // 平手 ⇒ 走右（原版 `AdjustedSlot.c:53-61`：`左 < 右` 为假就取右侧）
            Place(ctx, 1, 2, Unit("T02_RW_C4", 1, 1, 5));              // 现在左 2 / 右 2（平手）
            CheckTrue(BoardSlots.IsContiguous(ctx.Players[1], out _), "（前提）① 仍然是连续无洞");
            ok = BoardSlots.Resolve(ctx.Players[1], BoardSpec.WarlordSlot, out side, out index);
            CheckTrue(ok, "（前提）平手时督军格也能落");
            Check(side, BoardSlots.Right, "★ 平手 ⇒ **走右**（原版 `左 < 右 ? 左 : 右`，平手落到 else）"
                                        + "｜🧨 把 `l < r ? Left : Right` 改成 `<=` ⇒ 实得 Left");
            Check(BoardSlots.SlotOf(side, index), 7, "★ 格号 = **7**（右侧第 3 格 = 该侧最外那一格）");
            Check(Board(ctx, 1, 7), null, "★ 那一格也确实空着");
        }

        // ---- ② 有洞（左少 ⇒ 走左）：洞**两侧都放人**；裸算法指向被占的 1 号格 ----
        {
            var ctx = BattlePool(new CardDef[0], new[] { Unit("T02_RW_Play", 1, 1, 5) }, new List<CardDef>());
            ToP1Turn(ctx, 2);
            PassTurn(ctx);                                        // 推到 **P1 的回合**
            Check(ctx.Active, 1, "（前提）现在是 P1 的回合");
            ctx.Players[1].Energy = 20;
            Place(ctx, 1, 3, Unit("T02_RW_L0", 1, 1, 5));         // 左 index 0
            var inner = Place(ctx, 1, 1, Unit("T02_RW_L2", 1, 1, 5));  // 左 index 2 —— **2 号格是洞**
            Place(ctx, 1, 5, Unit("T02_RW_R0", 1, 1, 5));
            Place(ctx, 1, 6, Unit("T02_RW_R1", 1, 1, 5));
            Place(ctx, 1, 7, Unit("T02_RW_R2", 1, 1, 5));         // 右 3（连续）

            CheckTrue(!BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）② 有洞棋盘（夹具直接写 `Board[]`：左 2 号格空着）");
            // 裸算法（改前那句 `index = CountOnSide(ps, side)`）在这块棋盘上给什么：
            Check(BoardSlots.SlotOf(BoardSlots.Left, BoardSlots.CountOnSide(ctx.Players[1], BoardSlots.Left)), 1,
                  "（前提）左 2 < 右 3 ⇒ 走左；**裸算法**（下标 = 人数）指向**已经被占**的 1 号格");
            bool ok = BoardSlots.Resolve(ctx.Players[1], BoardSpec.WarlordSlot, out int side, out int index);
            CheckTrue(ok, "（前提）② 有洞时督军格仍然能落");
            Check(side, BoardSlots.Left, "（前提）② 选侧照旧：左 2 < 右 3 ⇒ 左");
            int slot = BoardSlots.SlotOf(side, index);
            Check(slot, 0,
                  "★★ `T02`：`Resolve`（督军格退化路）**绝不返回被占格** —— 改到该侧最外的空格 **0**"
                + "｜🧨 把这一支改回裸 `index = CountOnSide(ps, side)` ⇒ 实得 1（被占）");
            CheckTrue(Board(ctx, 1, slot) == null,
                      "★★ 有洞棋盘上 `Resolve` 返回的格**一定是空的**（不是碰巧）");
            CheckTrue(slot != 2,
                      "★★ 灭自证：**不是**「从督军往外找第一个空格」（2 号格正是那个洞）—— 判据是"
                    + " `NextFreeWithoutDisplacing` 的「该侧**最外**的空格」"
                    + "｜🧨 把实现写成从内往外扫 ⇒ 实得 2");

            // ---- 端到端：把这个落点喂给 `PlayCard` ⇒ **谁都不许被抹掉**（`A1692` 的原始症状）----
            int idx = HandIdx(ctx, 1, "T02_RW_Play");
            CheckTrue(idx >= 0, "（前提）那张单位卡在 P1 手里");
            int landed;
            int code = RuleCore.PlayCard(ctx, 1, idx, BoardSpec.WarlordSlot, out landed);
            CheckCode(code, RuleCodes.OK, "督军格请求真打出去（落点由 `Resolve` 算）");
            Check(landed, 0, "★★ 真正落在 **0** 号格（= `Resolve` 给的落点）");
            CheckTrue(Board(ctx, 1, 1) == inner,
                      "★★ 灭自证（洞外侧那个人还活着）：1 号格上那个单位**还在、还是同一个对象** —— "
                    + "改回裸算法 ⇒ 落点 1（被占）⇒ `Insert` 抹掉它 ⇒ 本行红");
            CheckTrue(Board(ctx, 1, 3) != null, "★★ 灭自证（洞内侧那个人也还在）：3 号格上没人被抹掉");
            Check(SlotOf(ctx, 1, "T02_RW_Play"), 0, "★ 新单位落在 0 号格（没顶掉任何人）");
        }

        // ---- ③ 有洞（平手走右）：换一侧再钉（防「硬编码左侧 / 只处理一侧」）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_RW_S1", 1, 1, 5));
            Place(ctx, 1, 2, Unit("T02_RW_S2", 1, 1, 5));         // 左 2（连续）
            Place(ctx, 1, 5, Unit("T02_RW_S3", 1, 1, 5));         // 右 index 0
            var outer = Place(ctx, 1, 7, Unit("T02_RW_S4", 1, 1, 5));  // 右 index 2 —— **6 号格是洞**

            CheckTrue(!BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）③ 有洞棋盘（右 6 号格空着）");
            Check(BoardSlots.SlotOf(BoardSlots.Right, BoardSlots.CountOnSide(ctx.Players[1], BoardSlots.Right)), 7,
                  "（前提）两侧平手 ⇒ 走右；**裸算法**指向**已经被占**的 7 号格");
            bool ok = BoardSlots.Resolve(ctx.Players[1], BoardSpec.WarlordSlot, out int side, out int index);
            CheckTrue(ok, "（前提）③ 有洞时督军格仍然能落");
            Check(side, BoardSlots.Right, "（前提）③ 平手 ⇒ 走右");
            Check(BoardSlots.SlotOf(side, index), 8,
                  "★★ `T02`：换到右侧 ⇒ 该侧最外的空格 **8**"
                + "｜🧨 把实现写成「硬编码左侧」或裸算法 ⇒ 本行红（裸算法给 7，被占）");
            CheckTrue(Board(ctx, 1, 7) == outer,
                      "★★ 灭自证：7 号格原来那个**还是原来那个对象**（查询不动棋盘）");
        }

        // ---- ④ 内洞（最贴督军那一格空着）：`Resolve` 给的空格下标 **小于人数** ----
        //   ⇒ 这条钉的是 `Insert` 的**插推循环不许在有洞棋盘上跑**（否则它把最外那一格的人覆盖掉）。
        {
            var ctx = BattlePool(new CardDef[0], new[] { Unit("T02_RW_Deep", 1, 1, 5) }, new List<CardDef>());
            ToP1Turn(ctx, 2);
            PassTurn(ctx);
            Check(ctx.Active, 1, "（前提）现在是 P1 的回合");
            ctx.Players[1].Energy = 20;
            // 左：3 号格**空着（内洞）**、2/1/0 号格都有人 ⇒ 人数 3、裸算法指向被占的 0 号格
            Place(ctx, 1, 2, Unit("T02_RW_D1", 1, 1, 5));
            var d2 = Place(ctx, 1, 1, Unit("T02_RW_D2", 1, 1, 5));
            var d3 = Place(ctx, 1, 0, Unit("T02_RW_D3", 1, 1, 5));
            // 右：5/6/7/8 全满（人数 4）⇒ 左 3 < 右 4 ⇒ 走左
            Place(ctx, 1, 5, Unit("T02_RW_D4", 1, 1, 5));
            Place(ctx, 1, 6, Unit("T02_RW_D5", 1, 1, 5));
            Place(ctx, 1, 7, Unit("T02_RW_D6", 1, 1, 5));
            Place(ctx, 1, 8, Unit("T02_RW_D7", 1, 1, 5));

            CheckTrue(!BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）④ 有洞棋盘（左 3 号格空着 = 内洞）");
            Check(BoardSlots.SlotOf(BoardSlots.Left, BoardSlots.CountOnSide(ctx.Players[1], BoardSlots.Left)), 0,
                  "（前提）左 3 < 右 4 ⇒ 走左；**裸算法**指向**已经被占**的 0 号格");
            bool ok = BoardSlots.Resolve(ctx.Players[1], BoardSpec.WarlordSlot, out int side, out int index);
            CheckTrue(ok, "（前提）④ 有洞时督军格仍然能落");
            Check(BoardSlots.SlotOf(side, index), 3,
                  "★★ `T02`：内洞形状下改到**3 号格**（左侧唯一的空格）"
                + "｜🧨 改回裸算法 ⇒ 实得 0（被占）");
            CheckTrue(index < BoardSlots.CountOnSide(ctx.Players[1], side),
                  "★ 这个尺寸值得单钉：空格的下标（" + index + "）**小于**该侧人数（"
                + BoardSlots.CountOnSide(ctx.Players[1], side) + "）—— 插推循环在这形状下会覆盖外侧的人");

            int idx = HandIdx(ctx, 1, "T02_RW_Deep");
            CheckTrue(idx >= 0, "（前提）那张单位卡在 P1 手里");
            int landed;
            int code = RuleCore.PlayCard(ctx, 1, idx, BoardSpec.WarlordSlot, out landed);
            CheckCode(code, RuleCodes.OK, "督军格请求真打出去（内洞形状）");
            Check(landed, 3, "★★ 真正落在 **3** 号格");
            CheckTrue(Board(ctx, 1, 2) != null && Board(ctx, 1, 1) == d2 && Board(ctx, 1, 0) == d3,
                      "★★ 灭自证（这一条钉的是 `Insert` 的**插推循环**）：2/1/0 号格那三个人"
                    + "**一个都没被抹掉、也没被挪走** —— 有洞时一律直写、**不推人**"
                    + "｜🧨 把那道「有洞 ⇒ 督军格也直写」的闸删掉 ⇒ 插推循环从 `count-1` 搬到 `count`"
                    + " ⇒ 0 号格那个被 1 号格的人覆盖 ⇒ 本行红");
            Check(SlotOf(ctx, 1, "T02_RW_Deep"), 3, "★ 新单位落在 3 号格");
        }
    }

    // ==================================================================
    //  ⑥ 归还侧 · 原版 `MinionManager.IsAvailableSlot` 收成唯一判据（`T02·P4`，2026-10-22）
    // ==================================================================

    /// <summary>
    /// **`T02·P4`：归还侧的「还接得住吗」判据 = 原版 `MinionManager.IsAvailableSlot(mm, n)`**
    /// （`MinionManager__IsAvailableSlot.c:19-29`）——
    /// `(左容量 − 左人数) + (右容量 − 右人数) >= n`：**两侧相加**、**只看人数、不看具体格号**。
    ///
    /// 这一笔的**判据结论**（`资料/普查产出_1022/W63_T02_or与落位_普查.md` §3 把它记成「判据未定」）：
    /// 🔴 **原版根本没有「归还」这一跳** —— 全库翻转归属（`CardScript +0x40` isPlayer）的只有
    /// `CardScript__ChangeOwner.c`（`+0x40` 取反）与 `CardScript__SetAsPlayer.c`，调用点 5 处：
    /// `_ResolveStealMinion_d__551:59` · `_ResolveExchangeMinion_d__552:154/:159`（场上单位）·
    /// `_ResolveStealFromDeck_d__547:87` / `_ResolveStealFromHand_d__546:209`（手牌，不是棋盘单位）；
    /// **没有任何路径把它翻回去**（`BuffType` 无「控制权」项、`CardEffect.untilEndOfTurn` 只挂在增益上）。
    /// ⇒ 「归还侧的 `IsAvailableSlot`」在原版**不存在（没有这一侧可判）**；我们的闸用的是
    /// **原版自己那条入口判据**：`NextFreeWithoutDisplacing < 0` ⇔ `IsAvailableSlot(ps, 1) == false`。
    ///
    /// ⚠️ **原版行为不可比**的措辞照旧：原版两侧是无洞 `List`，**没有「目标格被占」这一档**。
    /// </summary>
    static void TestT02ReturnSideIsAvailableSlot()
    {
        // ---- ① `IsAvailableSlot` 本体：判据是**两侧相加**，不是「某一侧放得下」 ----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_AS_L0", 1, 1, 5));
            Place(ctx, 1, 2, Unit("T02_AS_L1", 1, 1, 5));
            Place(ctx, 1, 1, Unit("T02_AS_L2", 1, 1, 5));      // 左 3 个（index 0/1/2）⇒ 0 号格空着
            Place(ctx, 1, 5, Unit("T02_AS_R0", 1, 1, 5));
            Place(ctx, 1, 6, Unit("T02_AS_R1", 1, 1, 5));
            Place(ctx, 1, 7, Unit("T02_AS_R2", 1, 1, 5));      // 右 3 个 ⇒ 8 号格空着
            CheckTrue(BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）① 连续无洞：左 3 / 右 3 ⇒ 每侧各剩 1 格");
            Check(BoardSlots.RoomOnSide(ctx.Players[1], BoardSlots.Left), 1,
                  "（前提）左侧余量 1");
            Check(BoardSlots.RoomOnSide(ctx.Players[1], BoardSlots.Right), 1,
                  "（前提）右侧余量 1");
            CheckTrue(BoardSlots.IsAvailableSlot(ctx.Players[1], 1),
                      "★ `IsAvailableSlot(1)` = 真（两边都还有 1 格）");
            CheckTrue(BoardSlots.IsAvailableSlot(ctx.Players[1], 2),
                      "★★ 判据是**两侧相加**：1 + 1 = 2 ⇒ `IsAvailableSlot(2)` = **真**"
                    + "｜🧨 把实现写成「某一侧放得下 n 个」（`IsAvailableSlotLeft/Right` 那一档，"
                    + "`MinionManager__IsAvailableSlotLeft.c:19`）⇒ 每侧只有 1 ⇒ 实得**假** ⇒ 红");
            CheckTrue(!BoardSlots.IsAvailableSlot(ctx.Players[1], 3),
                      "★★ 上限：1 + 1 = 2 < 3 ⇒ `IsAvailableSlot(3)` = 假"
                    + "｜🧨 把 `>= n` 写成 `> n` 或漏掉一侧 ⇒ 本行红");
        }

        // ---- ② 「满场」那一判 = `IsAvailableSlot(1)`：**一侧满不算满**（选侧规则的等价性）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_AS_F0", 1, 1, 5));
            Place(ctx, 1, 2, Unit("T02_AS_F1", 1, 1, 5));
            Place(ctx, 1, 1, Unit("T02_AS_F2", 1, 1, 5));
            Place(ctx, 1, 0, Unit("T02_AS_F3", 1, 1, 5));      // **左满 4**
            Place(ctx, 1, 5, Unit("T02_AS_F4", 1, 1, 5));
            Place(ctx, 1, 6, Unit("T02_AS_F5", 1, 1, 5));      // 右 2 ⇒ 7、8 都空着
            CheckTrue(BoardSlots.IsAvailableSlot(ctx.Players[1], 1),
                      "（前提）左满 4 / 右 2 ⇒ 全盘还放得下 1 个");
            Check(BoardSlots.NextWithoutDisplacing(ctx.Players[1]), 7,
                  "★★ **一侧满不算满**：左 4 > 右 2 ⇒ 选右 ⇒ 落 7（右 index 2）"
                + "｜🧨 把 `NextWithoutDisplacing` 的闸写成「任意一侧满 ⇒ 返回 -1」⇒ 实得 -1 ⇒ 红"
                + "（原版选侧是 `l < r ? 左 : 右`，`GetNextSlotWithoutDisplacing.c:14-30`，"
                + "选中那侧满 ⇔ 两侧都满 —— 证明见 `BoardSlots.NextWithoutDisplacing` 的 remarks）");

            // 全盘满 ⇒ 两条判据同时说「不行」（这就是归还侧那个 `-1` 分支的判据来源）
            Place(ctx, 1, 7, Unit("T02_AS_F6", 1, 1, 5));
            Place(ctx, 1, 8, Unit("T02_AS_F7", 1, 1, 5));
            CheckTrue(!BoardSlots.IsAvailableSlot(ctx.Players[1], 1),
                      "★★ 全盘满 ⇒ `IsAvailableSlot(1)` = **假**");
            Check(BoardSlots.NextWithoutDisplacing(ctx.Players[1]), -1,
                  "★★ 而且 `NextWithoutDisplacing` = -1 —— 两句**同一条判据**"
                + "｜🧨 把 `NextWithoutDisplacing` 的闸改回裸 `CountOnSide(side) >= 每侧格数`"
                + " 以外的第三套写法（例如只看左侧）⇒ 本行或上一行红");
            int dst = BoardSlots.NextFreeWithoutDisplacing(ctx.Players[1], out int blockedFull);
            Check(dst, -1, "★ 全盘满：`NextFreeWithoutDisplacing` = -1");
            Check(blockedFull, -1, "★ 而且 `blockedSlot = -1` = **「两侧都满」**（不是「目标格被占」）—— 调用方据此说不同的话");
        }

        // ---- ③ 归还侧端到端：目标格被占 + **正确答案是一个夹在两人中间的内洞**（灭自证）----
        //   夹具（原主 = P1）：左 3 号格有人、**2 号格空着（洞）**、1 号格有人、0 号格有人 ⇒ 左 3 个；
        //   右 5/6/7/8 全满 ⇒ 右 4 个。
        //   ⇒ 选侧：左 3 < 右 4 ⇒ 走左；裸算法给 index 3 = **0 号格（被占）**；
        //     正确答案 = 从最外往里找到的第一个空格 = **2 号格**（它的两侧 3 与 1 **都有人**）。
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var held = Place(ctx, 0, 5, Unit("T02_RS_Unit", 1, 3, 5));
            ctx.TempControls.Add(new BattleContext.TempControl
            {
                Unit = held, Owner = 1, Turn = ctx.Turn, Slot = 5,
            });
            var l0 = Place(ctx, 1, 3, Unit("T02_RS_L0", 1, 1, 5));
            var l2 = Place(ctx, 1, 1, Unit("T02_RS_L2", 1, 1, 5));
            var l3 = Place(ctx, 1, 0, Unit("T02_RS_L3", 1, 1, 5));
            Place(ctx, 1, 5, Unit("T02_RS_R0", 1, 1, 5));
            Place(ctx, 1, 6, Unit("T02_RS_R1", 1, 1, 5));
            Place(ctx, 1, 7, Unit("T02_RS_R2", 1, 1, 5));
            Place(ctx, 1, 8, Unit("T02_RS_R3", 1, 1, 5));

            CheckTrue(!BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）③ 有洞棋盘（夹具直接写 `Board[]`：左 2 号格空着）");
            Check(BoardSlots.NextWithoutDisplacing(ctx.Players[1]), 0,
                  "（前提）左 3 < 右 4 ⇒ 走左；裸算法指向**已经被占**的 0 号格");
            int probeTo = BoardSlots.NextFreeWithoutDisplacing(ctx.Players[1], out int probeBlocked);
            Check(probeBlocked, 0, "（前提）被挡住的那一格报成 0");
            Check(probeTo, 2,
                  "★★ `T02·P4`：`NextFreeWithoutDisplacing` 给 **2 号格**（左 2 号格 = 唯一的洞）"
                + "｜🧨 改回裸 `NextWithoutDisplacing` ⇒ 实得 0；"
                + "🧨 写成「被占就取相邻那格」（`dst ± 1`）⇒ 实得 1（也被人占着）⇒ 红");

            int logFrom = ctx.Events.Count;
            RuleCore.EndTurn(ctx);

            Check(SlotOf(ctx, 1, "T02_RS_Unit"), 2,
                  "★★ `T02·P4`：还回去的单位落在 **2 号格**（夹在 3 与 1 之间的内洞）—— 不是被占的 0、也不是 1"
                + "｜🧨 把 `RuleCore.EndTurn` 归还那一段改回裸 `NextWithoutDisplacing` ⇒ 实得 0"
                + "（并且 0 号格上的人被静默抹掉）" + S23RTail(ctx, logFrom));
            CheckTrue(Board(ctx, 1, 3) == l0 && Board(ctx, 1, 1) == l2 && Board(ctx, 1, 0) == l3,
                      "★★ 灭自证：2 号格**两侧**那三个人（3 / 1 / 0 号格）**都还在、还是原来那个对象** —— "
                    + "这条断言不允许「碰巧落在一个本来就空着的边角」：正确答案是被两个人夹住的格"
                    + S23RTail(ctx, logFrom));
            CheckTrue(HasLog(ctx, "不覆盖"),
                      "★ 而且**出声**（不许静默失败）" + S23RTail(ctx, logFrom));
            CheckTrue(BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "★ 还回去之后这块棋盘**重新连续无洞**（左 4 / 右 4 全满）");
            Check(SlotOf(ctx, 0, "T02_RS_Unit"), -1,
                  "★ 它已经从「抢它的人」那边摘掉了（`RemoveMinion` ⇒ 补位）" + S23RTail(ctx, logFrom));
        }

        // ---- ④ 归还侧的另一支：**两侧都满** ⇒ 还不回去、人留在抢它的人那儿（`IsAvailableSlot` 假）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var held = Place(ctx, 0, 5, Unit("T02_RF_Unit", 1, 3, 5));
            ctx.TempControls.Add(new BattleContext.TempControl
            {
                Unit = held, Owner = 1, Turn = ctx.Turn, Slot = 5,
            });
            var keep = new List<UnitState>();
            keep.Add(Place(ctx, 1, 3, Unit("T02_RF_L0", 1, 1, 5)));
            keep.Add(Place(ctx, 1, 2, Unit("T02_RF_L1", 1, 1, 5)));
            keep.Add(Place(ctx, 1, 1, Unit("T02_RF_L2", 1, 1, 5)));
            keep.Add(Place(ctx, 1, 0, Unit("T02_RF_L3", 1, 1, 5)));
            keep.Add(Place(ctx, 1, 5, Unit("T02_RF_R0", 1, 1, 5)));
            keep.Add(Place(ctx, 1, 6, Unit("T02_RF_R1", 1, 1, 5)));
            keep.Add(Place(ctx, 1, 7, Unit("T02_RF_R2", 1, 1, 5)));
            keep.Add(Place(ctx, 1, 8, Unit("T02_RF_R3", 1, 1, 5)));
            CheckTrue(!BoardSlots.IsAvailableSlot(ctx.Players[1], 1),
                      "（前提）④ 原主的棋盘**两侧都满**（`IsAvailableSlot(1)` = 假）");

            int logFrom = ctx.Events.Count;
            RuleCore.EndTurn(ctx);

            Check(SlotOf(ctx, 0, "T02_RF_Unit"), 5,
                  "★★ `T02·P4`：还接不住 ⇒ **人留在抢它的人那儿**（还在 P0 的 5 号格）"
                + "｜🧨 把 `RuleCore.EndTurn` 归还段那个 `if (to < 0) continue;` 删掉 ⇒ "
                + "会往 -1 号格写 ⇒ 越界/静默异常" + S23RTail(ctx, logFrom));
            Check(SlotOf(ctx, 1, "T02_RF_Unit"), -1, "★ 原主那边**没有**这个人");
            bool intact = true;
            for (int i = 0; i < keep.Count; i++)
                if (Board(ctx, 1, BoardSlots.SlotOf(i < 4 ? BoardSlots.Left : BoardSlots.Right,
                                                    i < 4 ? i : i - 4)) != keep[i]) intact = false;
            CheckTrue(intact,
                      "★★ 灭自证：原主棋盘上那 8 个**一个都没被顶掉/挪走**（还是原来那些对象）"
                    + S23RTail(ctx, logFrom));
            CheckTrue(HasLog(ctx, "还不回去"),
                      "★ 而且**出声**（「部署位也满了」那一支，判据 = `IsAvailableSlot(1)` = 假）"
                    + S23RTail(ctx, logFrom));
        }
    }

    // ==================================================================
    //  ⑦ `BoardSlots.Resolve` 的【侧向分支】（请求侧格被占）—— 插推族最后一段（2026-10-23）
    // ==================================================================

    /// <summary>
    /// **`BoardSlots.Resolve` 的侧向分支（`requestedSlot` 落在某一侧）** —— 2026-10-22 那一批只修了
    /// 「督军格退化路」，<see cref="BoardSlots.Resolve"/> 的**侧向分支**仍是「人数当插入下标」那套
    /// （`T02` 普查的原文：「`Resolve` 的侧向分支仍属插推族未修」）。
    ///
    /// 判据（第一权威 = 全量反编译 `MinionManager__AdjustedSlot.c`，逐句）：
    ///   · 请求在某侧、**那一侧没满** ⇒ 用请求的下标（`:81-96` 那一段：「`index &lt;= 该侧人数`
    ///     就原样返回请求值」，否则取该侧最外 —— 我们那句 `index &gt; count ⇒ index = count` 就是它）；
    ///   · 请求在某一侧、**那一侧满了** ⇒ 换到对侧最外（`:64-72` 右满 ⇒ `~leftMinions.Count`；
    ///     `:104-107` 左满 ⇒ `rightOccupation.Count + 1`）。
    ///   ⇒ **连续棋盘上「请求的格有人」是正常的插入点**（`List.Insert` 把后面的整体外移一格）——
    ///     这一档**一个字都不许改**（见 ①②）。
    ///
    /// 我们的差异（**只可能出现在非法局面上**）：原版两侧是**连续 `List`**、结构上不可能有洞；
    /// 我们的 `Board[]` 是定长数组、**自检夹具**可以直接写出洞 ⇒ `index` 可能指到**已经有人的格**，
    /// 而 `Insert` 的插推循环会把**外侧那一格的人覆盖掉**（`A1692` 的原始症状）。
    /// ⇒ 处置 = 有洞时改成「该侧最外的空格」（判据 = `BoardSlots.OutermostFreeOnSide`，
    /// 与 `NextFreeWithoutDisplacing` 同一个方向），并出声（见 ③④⑤⑥）。
    ///
    /// ⚠️ **原版行为【不可比】**：`AdjustedSlot.c` 全文只比两条 `List.Count`、**没有任何占用检查**
    ///    ⇒ 换格是补**我们表示法**的洞，⛔ 不是「照原版」。
    /// ⚠️ 有洞时那一句提示是 `UnityEngine.Debug.LogWarning`（`BoardSlots` 里没有 `ctx`）——
    ///    引擎自检捕不到 Unity 日志，本文件**不断言**它（同 `TestT02ResolveWarlordSlotHonoursAvailability`）。
    /// </summary>
    static void TestT02ResolveSideBranchHonoursAvailability()
    {
        // ---- ① 连续棋盘：请求「两人之间」= 正常插入点，`Resolve` **逐字返回请求的下标** ----
        //   🔴 这条是**灭自证**：把「落点被占就换最外空格」那道闸写成**无条件**的（不先判连续），
        //      这里就会实得 8（该侧最外空格）而不是 5 ⇒ 所有「插在两人之间」的落点全变。
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_RS_C1", 1, 1, 5));
            Place(ctx, 1, 2, Unit("T02_RS_C2", 1, 1, 5));            // 左 2
            Place(ctx, 1, 5, Unit("T02_RS_C3", 1, 1, 5));
            Place(ctx, 1, 6, Unit("T02_RS_C4", 1, 1, 5));            // 右 2（index 0/1）
            CheckTrue(BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）① 连续无洞（左 2 / 右 2）");
            bool ok = BoardSlots.Resolve(ctx.Players[1], 5, out int side, out int index);
            CheckTrue(ok, "（前提）① 连续棋盘上「请求 5 号格」能落");
            Check(side, BoardSlots.Right, "★ 请求 5 号格 ⇒ 右侧");
            Check(index, 0,
                  "★★ 连续棋盘：**请求的下标原样返回 0**（5 号格就是右侧 index 0）"
                + "｜🧨 把「落点被占 ⇒ 换该侧最外空格」那道闸改成**无条件**（不先判 `IsContiguous`）"
                + " ⇒ 实得 3（= 8 号格）—— 那样所有「插在两人之间」全变成「追加到最外」");
            Check(BoardSlots.SlotOf(side, index), 5, "★ 格号 = **5**（= 请求那一格）");
        }

        // ---- ② 连续棋盘 + 端到端：插在两人之间 ⇒ 后面的整体外移一格，**谁都不被抹掉** ----
        {
            var ctx = BattlePool(new CardDef[0], new[] { Unit("T02_RS_Play1", 1, 1, 5) }, new List<CardDef>());
            ToP1Turn(ctx, 2);
            PassTurn(ctx);
            Check(ctx.Active, 1, "（前提）现在是 P1 的回合");
            ctx.Players[1].Energy = 20;
            var r0 = Place(ctx, 1, 5, Unit("T02_RS_E1", 1, 1, 5));
            var r1 = Place(ctx, 1, 6, Unit("T02_RS_E2", 1, 1, 5));
            int idx = HandIdx(ctx, 1, "T02_RS_Play1");
            CheckTrue(idx >= 0, "（前提）那张单位卡在 P1 手里");
            int landed;
            int code = RuleCore.PlayCard(ctx, 1, idx, 5, out landed);
            CheckCode(code, RuleCodes.OK, "在 5 号格打出单位卡（插入语义）");
            Check(landed, 5, "★★ 落在 **5** —— 请求哪一格就插哪一格（连续棋盘，逐字不变）");
            CheckTrue(Board(ctx, 1, 6) == r0 && Board(ctx, 1, 7) == r1,
                      "★★ 灭自证：原来那两位**整体外移一格**（5→6、6→7）且**还是原来那些对象** —— "
                    + "插推语义（原版 `List.Insert`）。🧨 把侧向分支改成「恒追加到最外」⇒ 本行红");
            Check(Board(ctx, 1, 8), null, "★ 8 号格仍然空着（没被顺手填掉）");
        }

        // ---- ③ 有洞：请求右侧最外那一格（**被占**）—— 洞在中段 ----
        //   右：index0 = 5 有人、index1 = 6 **空着（洞）**、index2 = 7 有人 ⇒ 人数 2。
        //   裸算式 `index = IndexOf(7) = 2`，夹到 `<= count(2)` 仍是 2 ⇒ 指向**已被占的 7 号格**。
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_RS_H1", 1, 1, 5));
            Place(ctx, 1, 2, Unit("T02_RS_H2", 1, 1, 5));            // 左 2（连续）
            Place(ctx, 1, 5, Unit("T02_RS_H3", 1, 1, 5));            // 右 index 0
            var outer = Place(ctx, 1, 7, Unit("T02_RS_H4", 1, 1, 5)); // 右 index 2 —— **6 号格是洞**
            CheckTrue(!BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）③ 有洞棋盘（右 6 号格空着 = 中段洞）");
            Check(BoardSlots.SlotOf(BoardSlots.Right, BoardSlots.CountOnSide(ctx.Players[1], BoardSlots.Right)), 7,
                  "（前提）右 2 人 ⇒ **裸算法**（下标 = 人数）指向**已经被占**的 7 号格");
            bool ok = BoardSlots.Resolve(ctx.Players[1], 7, out int side, out int index);
            CheckTrue(ok, "（前提）③ 有洞时「请求 7 号格」仍然能落");
            Check(side, BoardSlots.Right, "（前提）③ 请求 7 号格 ⇒ 仍是右侧（不换边）");
            Check(BoardSlots.SlotOf(side, index), 8,
                  "★★ `Resolve`（侧向分支）**绝不返回被占格** —— 改到该侧最外的空格 **8**"
                + "｜🧨 把那段「落点被占 ⇒ 换该侧最外空格」的闸删掉 ⇒ 实得 7（被占）"
                + "⇒ `Insert` 的插推循环把 7 号格那个**静默抹掉**");
            CheckTrue(Board(ctx, 1, 7) == outer,
                      "★★ 灭自证：7 号格原来那个**还是原来那个对象**（查询不动棋盘）");
            Check(Board(ctx, 1, 8), null, "★ 而且 8 号格**确实是空的**（不是碰巧）");
            CheckTrue(BoardSlots.SlotOf(side, index) != 6,
                      "★★ 灭自证：**不是**「找第一个洞」（6 号格正是那个洞）—— 判据是「该侧**最外**的空格」"
                    + "｜🧨 把实现写成「从督军往外找第一个空格」⇒ 实得 6");
        }

        // ---- ④ 有洞 + 请求的**正是那个洞** ⇒ **不换格**（钉住「不是恒取最外空格」）----
        //   与 ③ 同一块棋盘，只把请求从 7 号格换成 6 号格（那一格空着）。
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_RS_D1", 1, 1, 5));
            Place(ctx, 1, 2, Unit("T02_RS_D2", 1, 1, 5));
            Place(ctx, 1, 5, Unit("T02_RS_D3", 1, 1, 5));
            var outer = Place(ctx, 1, 7, Unit("T02_RS_D4", 1, 1, 5)); // 右 index 2，**6 号格是洞**
            CheckTrue(!BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）④ 有洞棋盘（右 6 号格空着）");
            bool ok = BoardSlots.Resolve(ctx.Players[1], 6, out int side, out int index);
            CheckTrue(ok, "（前提）④ 请求那个洞（6 号格）能落");
            Check(BoardSlots.SlotOf(side, index), 6,
                  "★★ 请求的格**空着** ⇒ **原样用它**（差一格的洞就是要填它）"
                + "｜🧨 把闸写成「只要有洞就恒取该侧最外空格」⇒ 实得 8");
            CheckTrue(index < BoardSlots.CountOnSide(ctx.Players[1], side),
                      "★ 这个尺寸值得单钉：填洞时下标（" + index + "）**小于**该侧人数（"
                    + BoardSlots.CountOnSide(ctx.Players[1], side) + "）—— 连续模型里不存在这一档");
            CheckTrue(Board(ctx, 1, 7) == outer,
                      "★ 灭自证：7 号格那个**没被碰过**");
        }

        // ---- ⑤ 有洞：**请求侧满** ⇒ 换对侧；对侧也有洞 ⇒ 取对侧最外的空格 ----
        //   右 5/6/7/8 满（人数 4）；左：index0 = 3 有人、index1 = 2 **空着（洞）**、index2 = 1 有人
        //   ⇒ 人数 2 ⇒ 原版公式给「对侧最外」= 下标 2 = **1 号格（被占）**。
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("T02_RS_F1", 1, 1, 5));            // 左 index 0
            Place(ctx, 1, 1, Unit("T02_RS_F2", 1, 1, 5));            // 左 index 2 —— **2 号格是洞**
            Place(ctx, 1, 5, Unit("T02_RS_F3", 1, 1, 5));
            Place(ctx, 1, 6, Unit("T02_RS_F4", 1, 1, 5));
            Place(ctx, 1, 7, Unit("T02_RS_F5", 1, 1, 5));
            Place(ctx, 1, 8, Unit("T02_RS_F6", 1, 1, 5));            // 右满 4
            CheckTrue(!BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）⑤ 有洞棋盘（左 2 号格空着）");
            Check(BoardSlots.SlotOf(BoardSlots.Left, BoardSlots.CountOnSide(ctx.Players[1], BoardSlots.Left)), 1,
                  "（前提）左 2 人 ⇒ 原版公式的「对侧最外」= 下标 2 = **1 号格（被占）**");
            bool ok = BoardSlots.Resolve(ctx.Players[1], 5, out int side, out int index);
            CheckTrue(ok, "（前提）⑤ 请求右侧（已满）仍然能落");
            Check(side, BoardSlots.Left,
                  "★ 请求侧满 ⇒ **换到对侧**（原版 `AdjustedSlot.c:64-72` 那一支，逐字不变）");
            Check(BoardSlots.SlotOf(side, index), 0,
                  "★★ 对侧有洞 ⇒ 取对侧最外的空格 **0**（不是被占的 1）"
                + "｜🧨 把闸删掉 ⇒ 实得 1（被占）");
            CheckTrue(Board(ctx, 1, 1) != null,
                      "★★ 灭自证：1 号格那个人**还在**（查询不动棋盘）");
        }

        // ---- ⑥ 有洞 + 端到端：把这个落点喂给 `PlayCard`，**谁都不许被抹掉** ----
        //   ⚠️ 用 `BattlePool`（带 20 张 filler）而不是 `ProbeBattle`：本块要推到**第 4 回合**，
        //      空牌库会在抽牌时进疲劳分支 —— 那是与本断言无关的噪声。
        {
            var ctx = BattlePool(new CardDef[0], new[] { Unit("T02_RS_Play2", 1, 1, 5) }, new List<CardDef>());
            ToP1Turn(ctx, 2);
            PassTurn(ctx);
            Check(ctx.Active, 1, "（前提）现在是 P1 的回合");
            ctx.Players[1].Energy = 20;
            Place(ctx, 1, 3, Unit("T02_RS_G1", 1, 1, 5));
            Place(ctx, 1, 2, Unit("T02_RS_G2", 1, 1, 5));            // 左 2（连续）
            var r0 = Place(ctx, 1, 5, Unit("T02_RS_G3", 1, 1, 5));   // 右 index 0
            var r2 = Place(ctx, 1, 7, Unit("T02_RS_G4", 1, 1, 5));   // 右 index 2 —— **6 号格是洞**
            CheckTrue(!BoardSlots.IsContiguous(ctx.Players[1], out _),
                      "（前提）⑥ 有洞棋盘（右 6 号格空着）");
            int idx = HandIdx(ctx, 1, "T02_RS_Play2");
            CheckTrue(idx >= 0, "（前提）那张单位卡在 P1 手里");
            int landed;
            int code = RuleCore.PlayCard(ctx, 1, idx, 7, out landed);
            CheckCode(code, RuleCodes.OK, "在 7 号格（被占）打出单位卡");
            Check(landed, 8, "★★ 真正落在 **8** 号格（= `Resolve` 给的落点）");
            CheckTrue(Board(ctx, 1, 7) == r2,
                      "★★ 灭自证（`A1692` 的原始症状）：7 号格上那个单位**还在、还是同一个对象** —— "
                    + "改回裸算式 ⇒ 落点 7（被占）⇒ `Insert` 的插推循环把它**静默抹掉** ⇒ 本行红");
            CheckTrue(Board(ctx, 1, 5) == r0 && Board(ctx, 1, 3) != null && Board(ctx, 1, 2) != null,
                      "★★ 灭自证：5 / 3 / 2 号格**一个都没被抹掉、也没被挪走**（有洞时一律直写、不推人）");
            Check(SlotOf(ctx, 1, "T02_RS_Play2"), 8, "★ 新单位落在 8 号格（没顶掉任何人）");
        }
    }
}
