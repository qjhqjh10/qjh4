// RuleEngineTest_EngineB95WBD.cs — 账 324：`BattleManager.GetAvailableActions` 那两档字节闸的手牌段（2026-10-29 · `B95_WBD`）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineF8` /
// `_BoardModel` / `_E8` / `_Diag` … 都在），而 `RuleEngineTest.cs` 本批**不在本代理白名单**
// ⇒ 本件测试体全写在这里，`RuleEngineTest.cs` 只需在 `Run()` 里加一行
//   `Step(TestEngineB95WBDHandGate)`。
//
// ---- 判据链（⛔ 不要重新论证）----
//  · 原版 `BattleManager.GetAvailableActions(bool isPlayer, bool allowAutoActions)`
//    （`decomp_full/BattleManager__GetAvailableActions.c:60-75`）：`allowAutoActions == false` 时，
//    取手牌**之前**先过该方那一个字节闸 —— 玩家侧 `+0x244 allowPlayerActionsFlag`、
//    敌方侧 `+0x246 allowEnemyActionsFlag`；闸为假 ⇒ **直接返回空表**（连手牌都不碰）。
//  · 那两档闸的**写点** = `BattleManager._NextTurn_d__395__MoveNext.c:380-383`
//    （`+0x244 = 本回合是玩家0` · `+0x246 = (本回合不是玩家0)`）⇒ **语义就是「回合归属」**；
//    `BattleManager__ResolveEndTurn.c:336-337` / `StopBattleAfterBattleEnd.c:48-49` 收手清 0。
//  · 我们的等价物 = `SimpleAI.EnumerateActions(BattleContext ctx, int forSide = -1)`
//    （`Data/SimpleAI.cs:316`）：**闸不满足 ⇒ 整张表为空**（`:326 if (me != ctx.Active) return list;`，
//    连 `endTurn` 都没有 —— `.c:62/67` 的 `goto LAB_180964050` 落在 `:69 return *(param_1 + 0x560)`，
//    而 `endTurn` 在 `:339-341` 即返回点**之后**）。🔴 2026-10-29（`B97_TUT`）订正：原来这里写「只挡手牌段」，是**读错 `.c`**。
//
// 🔴 三条硬要求（本项目踩过 3 次「前提不成立 ⇒ 断言空转绿」）：
//    ① **前提断言与结论断言并列写**，⛔ 不写 `if (前提) Check(结论)`；
//    ② 每条都带 `🧨`（什么改动能让它红）—— 没写 `🧨` 的断言一律当没写；
//    ③ 期望值写死字面量，⛔ 不从实现常量反算。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`RuleEngineTest.Run()` 里加一行 `Step(TestEngineB95WBDHandGate)`）。
    /// ⚠️ 子用例用 `B95WBD…` 打头的**辅助方法**（⛔ 别用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestEngineB95WBDHandGate()
    {
        Section("🆕 账 324：`GetAvailableActions` 的两档字节闸 —— 只挡手牌段，`endTurn` 必须仍在");
        B95WBDHandGateCase();
    }

    /// <summary>
    /// 夹具：**双方手里各一张**能打得起的单位卡（费用 1 / 能量给 9）⇒ 手牌候选只可能来自「闸放行的那一方」。
    /// <para>期望（判据见文件头）：</para>
    /// · `EnumerateActions(ctx)`（= 行动方）⇒ 手牌候选 **1** 条（自己那张）；
    /// · `EnumerateActions(ctx, foe)`（**显式给非行动方**）⇒ **整张表为空**（手牌 0 条 ∧ 连 `endTurn` 也没有；判据 `.c:62/67 → :69`）；
    /// · `EnumerateActions(ctx, me)`（显式给行动方）⇒ 手牌候选 **1** 条（与默认档同）。
    /// </summary>
    static void B95WBDHandGateCase()
    {
        // 🔴 2026-10-30（`B100_F18` · G1）：`Battle(...)` → `ProbeBattle(...)`。
        //   原夹具用 `Battle` ⇒ `Deck()` 塞 **20 张 filler**、`NewBattle` 再起手 3/4 张
        //   （`RuleEngineTest.cs:2582` / `RuleCore.cs:285`）⇒ 手上不是「恰好 1 张」（实测 4 / 5），
        //   下面两条前提与两条结论全部塌掉。`ProbeBattle` = 只有督军 + 指定的那几张（`RuleEngineTest.cs:2629`）。
        //   ⛔ 期望值不改（改成 4/5 会把判别式废掉：那样「闸放不放行」不再是唯一变量）。
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active, foe = 1 - me;
        var mine = ctx.Players[me];
        var theirs = ctx.Players[foe];

        // 手上各放一张「费用 1 / 攻 1 / 血 1」的单位卡（名字带前缀，便于红的时候看日志是谁）。
        mine.Hand.Add(ctx.NewInstance(Unit("B95WBDMine", 1, 1, 1)));
        theirs.Hand.Add(ctx.NewInstance(Unit("B95WBDFoe", 1, 1, 1)));
        mine.Energy = 9;                      // 远高于费用 1 ⇒ 不会因为「付不起」被 `EnumerateActions` 提前跳过
        theirs.Energy = 9;

        // ---------- （前提）三条硬检查：都在断言**结论之前**、各自独立成条 ----------
        CheckTrue(ctx != null && !ctx.IsOver,
                  "★（前提）这一局还没结束（`EnumerateActions` 在 `ctx.IsOver` 时直接返回空表 ⇒ 后面的结论会假绿）"
                + "｜🧨 夹具改成「已经判胜」的 ctx ⇒ 本条红");
        Check(mine.Hand.Count, 1,
              "★（前提）行动方手里**恰好 1 张**（空牌库 ⇒ 起手为空，只有我们放的那张）"
            + "｜🧨 `Deal` 改成会发起手牌 / `Battle(...)` 换成分发真牌库 ⇒ 手上张数不是 1 ⇒ 红（那时下面的「手牌候选 = 1」就不再是判别式）");
        Check(theirs.Hand.Count, 1,
              "★（前提）非行动方手里也**恰好 1 张** —— 这是本用例的**判别式支点**："
            + "两边都有一张，闸放不放行是唯一变量"
            + "｜🧨 只给一方发牌 ⇒ 「非行动方手牌候选 = 0」在**没实现闸**的实现里也成立 ⇒ 断言失去判别力（这条红就是在提醒这一点）");

        // ---------- 结论 ----------
        int handMine = CountHandEntries(SimpleAI.EnumerateActions(ctx));
        int handMineExplicit = CountHandEntries(SimpleAI.EnumerateActions(ctx, me));
        var foeList = SimpleAI.EnumerateActions(ctx, foe);
        int handFoe = CountHandEntries(foeList);
        bool foeHasEndTurn = false;
        for (int i = 0; i < foeList.Count; i++) if (foeList[i].Kind == AiActionKind.EndTurn) foeHasEndTurn = true;

        Check(handMine, 1,
              "★★ 默认档（= 行动方自己）⇒ 手牌候选 **1** 条（只有自己那张）"
            + "｜🧨 闸被写成「一律挡手牌」（把 `me == ctx.Active` 写反 / 恒 false）⇒ 实得 0 ⇒ 红；"
            + "🧨 两侧手牌都被枚举（没接闸）⇒ 实得 2 ⇒ 红");
        Check(handMineExplicit, 1,
              "★★ 显式传 `forSide = 行动方` ⇒ 与默认档**同一条**（1 条）"
            + "｜🧨 `forSide` 的取值分支写错（把显式值当成「永远挡」）⇒ 实得 0 ⇒ 红");
        Check(handFoe, 0,
              "★★ 显式传 `forSide = 非行动方` ⇒ 手牌候选 **0** 条（原版 `+0x246/+0x244` 闸 = 回合归属）"
            + "｜🧨 闸没接 / 接在整表外却漏了手牌段 ⇒ 实得 1 ⇒ 红");
        Check(foeList.Count, 0,
              "★★ 非行动方那侧 **整张表为空** —— 原版 `.c:62/67` 的 `goto LAB_180964050` 落在 `:69 return *(param_1 + 0x560)`，"
            + "返回的是已清空的动作表（`endTurn` 在 `:339-341`，在返回点**之后**）"
            + "｜🧨 闸只包手牌段（棋盘 / 攻击 / 主动能力 / 残骸 / `endTurn` 照发）⇒ 实得 >0 ⇒ 红");
        CheckTrue(!foeHasEndTurn,
                  "★★ 同一张表里 **`endTurn` 也不在** —— 同一条判据（`:69` 在 `:339-341` 之前返回）"
                + "｜🧨 把闸包在手牌段外面（`endTurn` 仍追加在末尾）⇒ 本条红 —— 这正是本用例要挡的「改错法」");

        // 收尾：别把夹具留在全局状态里（本用例只读枚举，不落盘；这行是防御性的）
        mine.Energy = 0; theirs.Energy = 0;
    }

    /// <summary>数一张表里「出牌」那几档有几条（`PlayCard` 只会在手牌段产生）。</summary>
    static int CountHandEntries(List<AiAction> list)
    {
        if (list == null) return 0;
        int n = 0;
        for (int i = 0; i < list.Count; i++) if (list[i].Kind == AiActionKind.PlayCard) n++;
        return n;
    }
}
