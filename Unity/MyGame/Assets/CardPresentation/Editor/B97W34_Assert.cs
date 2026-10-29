// B97W34_Assert.cs — 执行代理 `B97_W34`（第九十七会话 · 波 4-A · `RuleCore*`）的断言
//
// ============================ 它盯的是什么 ============================
//   `RuleEngine/Core/RuleCore.cs` ＋ `RuleCorePartial_B97W34.cs` 本笔的两件（第三件是纯状态口，见 C 段）：
//     ① 账 `:325` `BattleManager.GetAvailableAttackActions` —— **两条「动作表闸」**
//        目标已带致死待结算伤害 ⇒ 不排；攻方这一档攻击力 ≤ 0 ⇒ 不排；近战被 `pindown` 压制 ⇒ 不排近战。
//        落点 = `RuleCore.IsValidTarget` 的那一句（＋ `CanAttackNow` 与它共用 `AttackActionPowerGate`）。
//     ② 账 `:607` `BattleManager.StartBattlePhase` 的「开局上手」取值口 —— **SO 真值优先于卡面文本**。
//     ③ 账 `:573` `BattleManager.SetNextCardAsDeckDrawing` 的状态口（纯呈现态，**未接进 `Draw`**，理由见实现注释）。
//
// 判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/报告/B97_W34.md` §一 ＋
//   `decomp_full/BattleManager__GetAvailableAttackActions.c:126 / :152`（目标将死不排）
//   `decomp_full/BattleManager__GetAvailableAttackActions.c:128-140 / :154-166`（攻击力 > 0 · 近战 pindown）
//   `decomp_full/BattleManager._StartBattlePhase_d__334__MoveNext.c:213-247`（`startingCardInHand`）
//   `decomp_full/BattleManager__SetNextCardAsDeckDrawing.c:1-42` ＋ `CardScript__SetAsDeckDrawing.c`
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1（0 攻近战）：把 `IsValidTarget` 末尾那个 `AttackActionPowerGate` 调用删掉 ⇒ A1 实得 OK ⇒ 红。
//   · A2（灭自证）：A1 同一张卡把近战改成 1 ⇒ 必须 OK。A1/A2 **结构上不可能同时满足**
//                   （同一句判据不可能同时「按攻击力 0 拒」和「不论攻击力都放行」）。
//   · A3（远程档）：`Ranged` 夹具近战 0 / 远程 2 ⇒ 近战档必须 `ErrNoMeleeAttack`、远程档必须 OK。
//                   把 `AttackActionPowerGate` 里的 `ranged` 参数忽略掉（一律按近战判）⇒ 两行必有一行红。
//   · A4（pindown）：近战 3 + `pindown` ⇒ 近战 `ErrPindown`、远程（远程 2）OK。
//                   把 pindown 那一条挪到 `AttackActionPowerGate` 外/删掉 ⇒ 第一个 CheckCode 红。
//   · A5（顺序）：近战 0 **且** `pindown` ⇒ 必须是 `ErrPindown`（原版先 trait 后攻击力）。
//                   把两句调换 ⇒ 实得 `ErrNoMeleeAttack` ⇒ 红。
//   · A6（将死目标）：目标 `Health = 0` ⇒ `ErrNotUnit`；A7 救回来 ⇒ OK。
//                   把 `EnoughPendingDamageToDie(target)` 那半删掉 ⇒ A6 实得 OK ⇒ 红；
//                   把它写成常量 `true` ⇒ A7 红。两条**结构上不可能同时满足**。
//   · A8（回归·共用没被改坏）：`CanAttackNow` 对同一张 0 攻卡仍返 `ErrNoMeleeAttack`。
//   · B1/B2（开局上手）：同一张卡，`StartingCardInHandRaw == null`（旧表）⇒ 走卡面文本那份；
//                   置成 `""`（SO 明确没有）⇒ **必须一条都不发**、且原文本那条要出声。
//                   把 `StartWithNames` 的 `raw == null` 分支删掉 ⇒ B1 红；把「真值优先」写成「两者都发」⇒ B2 红。
//   · C1/C2/C3（牌库顶标记）：两次调用必须标记**不同**的两张（第一张、第二张）。
//                   把 `marks.Contains` 那句删掉 ⇒ 两次都返回 `Deck[0]` ⇒ C2 红。
//                   `ClearDeckDrawingMark` 之后 `IsDeckDrawing` 必须转假 ⇒ 把它写成空实现 ⇒ C3 红。
//
// ============================ 怎么用（宿主）============================
//   `RuleEngineTest.Run()` 里插一行：`Step(TestB97W34);`
//   （本文件 = `RuleEngineTest` 的**分部**，复用它的夹具 `Battle` / `Place` / `Unit` / `Ranged` /
//    `Check` / `CheckCode` / `ToP1Turn` / `Section` —— ⛔ 另起一个类会把那套夹具抄第二份。）
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>B97_W34 的全部断言（宿主：`RuleEngineTest.Run` 里 `Step(TestB97W34);`）。</summary>
    public static void TestB97W34()
    {
        Section("B97_W34 —— 账 325 两条动作表闸 / 607 开局上手取值口 / 573 牌库顶标记");

        TestB97W34AttackActionGates();
        TestB97W34StartWithNames();
        TestB97W34DeckDrawingMark();
    }

    // ==========================================================================================
    //  A 段 —— 账 325：`GetAvailableAttackActions` 的两条动作表闸
    // ==========================================================================================
    static void TestB97W34AttackActionGates()
    {
        // ---- A1/A2：0 攻近战 vs 近战 1（灭自证）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 3, Unit("W34NoAtk", 1, 0, 5), exhausted: false);       // 近战 0 / 远程 0
            Place(ctx, 1, 3, Unit("W34Prey", 1, 0, 9), exhausted: true);
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 3, 1, 3, false), RuleCodes.ErrNoMeleeAttack,
                      "★ A1 近战攻击力 0 ⇒ 这一档**排不出攻击动作**（原版 `GetAvailableAttackActions.c:128-140`）"
                    + "｜🧨 把 `IsValidTarget` 末尾那个 `AttackActionPowerGate` 调用删掉 ⇒ 实得 OK ⇒ 红");

            Place(ctx, 0, 2, Unit("W34OneAtk", 1, 1, 5), exhausted: false);      // 近战 1
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 2, 1, 3, false), RuleCodes.OK,
                      "★ A2 **灭自证**：同一棋盘、同一刻，只把近战改成 1 就必须放行 —— 证明 A1 那次拒绝"
                    + "确实是「攻击力 ≤ 0」这一条在起作用，而不是别处的连带改动");
        }

        // ---- A3：远程档（近战 0 / 远程 2）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 3, Ranged("W34RangedOnly", 1, 0, 5, 2), exhausted: false);
            Place(ctx, 1, 3, Unit("W34Prey3", 1, 0, 9), exhausted: true);
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 3, 1, 3, false), RuleCodes.ErrNoMeleeAttack,
                      "★ A3-a 近战 0 ⇒ 近战那一档排不出（原版 `.c:128-140` 与 `.c:154-166` 是**两支**）");
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 3, 1, 3, true), RuleCodes.OK,
                      "★ A3-b 同一张卡远程 2 ⇒ 远程那一档照常排得出"
                    + "｜🧨 若把 `AttackActionPowerGate` 的 `ranged` 形参忽略掉（一律按近战算）⇒ 本行或上一行必有一行红");
        }

        // ---- A4/A5：`pindown`（只禁近战）＋ 与攻击力的先后 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 3, Ranged("W34Pindown", 1, 3, 5, 2, "pindown"), exhausted: false);
            Place(ctx, 1, 3, Unit("W34Prey4", 1, 0, 9), exhausted: true);
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 3, 1, 3, false), RuleCodes.ErrPindown,
                      "★ A4-a 近战被 `pindown`(0x3ca) 压制 ⇒ 近战那一档排不出（原版 `.c:128-140` 的第二个合取项）"
                    + "｜🧨 把 `AttackActionPowerGate` 里 `!ranged && attacker.Has(\"pindown\")` 那半删掉 ⇒ 实得 OK ⇒ 红");
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 3, 1, 3, true), RuleCodes.OK,
                      "★ A4-b **远程不受压制**（原版 `.c:154-166` 远程那一支**没有** `pindown` 这个 trait）"
                    + "｜🧨 把 pindown 那道闸放到 `ranged` 判断之外（远近都禁）⇒ 本行红");

            Place(ctx, 0, 2, Ranged("W34PindownZero", 1, 0, 5, 2, "pindown"), exhausted: false);
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 2, 1, 3, false), RuleCodes.ErrPindown,
                      "★ A5 **顺序照原版**：近战 0 **且** 带 `pindown` ⇒ 先命中 `pindown`"
                    + "（`CanAttackNow` 改前就是「先 trait 后攻击力」，`RuleEngineTest` 有一批断言盯这个次序）"
                    + "｜🧨 把两句调换 ⇒ 实得 `ErrNoMeleeAttack` ⇒ 红");
        }

        // ---- A6/A7：目标已带致死待结算伤害 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 3, Unit("W34Atk6", 1, 3, 9), exhausted: false);
            var prey = Place(ctx, 1, 3, Unit("W34Dying", 1, 0, 9), exhausted: true);
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 3, 1, 3, false), RuleCodes.OK,
                      "★ A6 （前提）目标还活着时打它是合法的 —— 没这条前提，下面那次拒绝分不清是谁在起作用");
            prey.Health = 0;
            CheckTrue(RuleCore.EnoughPendingDamageToDie(prey),
                      "★ A6 （前提）夹具真的把它摆成「已带致死待结算伤害」了");
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 3, 1, 3, false), RuleCodes.ErrNotUnit,
                      "★ A6 **目标已经要死了 ⇒ 这一档不再排任何攻击动作**"
                    + "（原版 `GetAvailableAttackActions.c:126 / :152` 的 `CardScript__EnoughPendingDamageToDie(目标)`）"
                    + "｜🧨 把 `IsValidTarget` 里 `EnoughPendingDamageToDie(target)` 那半删掉 ⇒ 实得 OK ⇒ 红");
            prey.Health = 9;
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 3, 1, 3, false), RuleCodes.OK,
                      "★ A7 **灭自证**：把它救回来之后必须重新合法"
                    + "｜🧨 把那一句写成常量 `true`（一律拒）⇒ 本行红 —— 与 A6 **结构上不可能同时满足**");
        }

        // ---- A8：同上那两条闸在 `CanAttackNow` 里仍由**同一份判据**提供（回归）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 3, Unit("W34NoAtk8", 1, 0, 5), exhausted: false);
            Place(ctx, 1, 3, Unit("W34Prey8", 1, 0, 9), exhausted: true);
            CheckCode(RuleCore.CanAttackNow(ctx, 0, 3, false), RuleCodes.ErrNoMeleeAttack,
                      "★ A8 回归：`CanAttackNow` 对 0 攻近战仍返 `ErrNoMeleeAttack`"
                    + "（改前是行内两句、改后走共用的 `AttackActionPowerGate` —— 返回码与顺序必须逐字不变）");
        }
    }

    // ==========================================================================================
    //  B 段 —— 账 607：「开局上手」的取值口（SO 真值优先）
    // ==========================================================================================
    static void TestB97W34StartWithNames()
    {
        // ---- B1：整列未知（旧表 / 夹具）⇒ 原样走卡面文本抽出来的那一份（= 改前行为）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var wl = new CardDef("W34WarlordText", "W34WarlordText", "hero",
                                 "Start the game with W34Bonus in hand.", "common", "Test", 0, 3, 30, 0, null);
            CheckTrue(wl.StartWithInHand.Count > 0,
                      "★ B1 （前提）卡面文本真的抽出了「开局上手」那一张（否则下面两行都是空转）");
            var ps = ctx.Players[0];
            ps.Warlord = new UnitState(ctx.NewInstance(wl), true);

            var names = RuleCore.StartWithNamesLogged(ctx, ps);
            Check(names.Count, wl.StartWithInHand.Count,
                  "★ B1 `startingCardInHand` 整列**未知**（`CardDef` 没 attach 过）⇒ 原样退回卡面文本那份"
                + "（= 改前 `SetupStartWith` 读 `StartWithInHand` 的行为，逐字不变）"
                + "｜🧨 把 `StartWithNames` 的 `raw == null` 分支删掉 ⇒ 本行实得 0 ⇒ 红");

            // ---- B2：真值已知且为空（SO 明确没有）⇒ **一条都不发** ----
            wl.AttachStartingCardInHand("");
            var truth = RuleCore.StartWithNamesLogged(ctx, ps);
            Check(truth.Count, 0,
                  "★ B2 SO 真值明确为空（`startingCardInHand` 是空串）⇒ **一张都不发**"
                + "（原版 `_StartBattlePhase_d__334…MoveNext.c:213-247` 只看 SO 那一格，不看卡面文字）"
                + "｜🧨 把「真值优先」改成「两者都发」⇒ 本行实得 1 ⇒ 红");
        }

        // ---- B3：真值非空 ⇒ 以真值为准（哪怕卡面文本写的是别的名字）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var wl = new CardDef("W34WarlordTruth", "W34WarlordTruth", "hero",
                                 "Start the game with W34FromText in hand.", "common", "Test", 0, 3, 30, 0, null);
            wl.AttachStartingCardInHand("W34FromData");
            var ps = ctx.Players[0];
            ps.Warlord = new UnitState(ctx.NewInstance(wl), true);

            var names = RuleCore.StartWithNamesLogged(ctx, ps);
            Check(names.Count, 1, "★ B3 真值非空 ⇒ 只发**真值那一张**（卡面文本那张不发）");
            CheckTrue(names.Count == 1 && names[0] == "W34FromData",
                      "★ B3 发的是 SO 那一格的名字 `W34FromData`，不是卡面文本的 `W34FromText`"
                    + "｜🧨 把 `raw.Length > 0` 那一支去掉、退回文本 ⇒ 本行红");
        }
    }

    // ==========================================================================================
    //  C 段 —— 账 573：牌库顶「正在抽」标记
    // ==========================================================================================
    static void TestB97W34DeckDrawingMark()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var deck = ctx.Players[0].Deck;
        CheckTrue(deck.Count >= 2,
                  "★ C0 （前提）牌库至少两张 —— 否则 C1/C2「两张不同」这条结构上验不了");

        var first = RuleCore.SetNextCardAsDeckDrawing(ctx, 0);
        CheckTrue(ReferenceEquals(first, deck[0]),
                  "★ C1 第一次调用标记的是**牌库顶**那一张（原版 `SetNextCardAsDeckDrawing.c:19-40` 从下标 0 起扫）");
        CheckTrue(RuleCore.IsDeckDrawing(ctx, deck[0]), "★ C1 牌库顶那一张确实被标记了");
        CheckTrue(!RuleCore.IsDeckDrawing(ctx, deck[1]),
                  "★ C1 第二张**不许**被顺带标记（原版找到第一张就 break）");

        var second = RuleCore.SetNextCardAsDeckDrawing(ctx, 0);
        CheckTrue(ReferenceEquals(second, deck[1]),
                  "★ C2 第二次数到**下一张还没标记的**（原版判据是 `cardState == 0 (inDeck)` —— 已标 16 的要跳过）"
                + "｜🧨 把 `marks.Contains(inst.Id)` 那句删掉 ⇒ 两次都返回 `Deck[0]` ⇒ 红");

        CheckTrue(RuleCore.ClearDeckDrawingMark(ctx, deck[0]), "★ C3 标记可以撤掉（原版没有显式清零点，这是我们那一层的等价时机）");
        CheckTrue(!RuleCore.IsDeckDrawing(ctx, deck[0]),
                  "★ C3 撤掉之后 `IsDeckDrawing` 必须转假｜🧨 把 `ClearDeckDrawingMark` 写成空实现 ⇒ 红");
        CheckTrue(RuleCore.IsDeckDrawing(ctx, deck[1]),
                  "★ C3 撤掉第一张**不影响**第二张（不是「清空整张表」）");
    }
}
