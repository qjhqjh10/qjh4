// RuleEngineTest_EngineF8.cs — 引擎线 `F8`：`AiTargets` 遍历序的**判别式**断言（2026-10-24）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_EngineA1` / `_EngineB3` / `_EngineB6` / `_EngineB7` / `_Order` / `_Diag` … 都在），
// 而**同一时刻只有一个写手改一个文件** ⇒ 本件测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一段 `Section(...)` + 一行 `Step(TestF8EnginePlayOrder)`**。
//
// 来历：`F7`（`资料/普查产出_1024/F7_引擎两处小订正.md`）收口 `AiTargets.AddLowestHealthUnit`
// 的遍历序时发现 —— 既有的「并列取进场更早」断言（`RuleEngineTest_EngineB6.cs` 那一条）
// **分辨不出遍历序**（格号序与进场序恰好同向）；`F8` 把那条夹具改成能判别，并把
// **`PlayOrder == -1`（未取号）不许抢在已取号之前**这条**兜底口径**落成断言（本文件）。
//
// 判据链（⛔ 不要重新论证）：
//   · 原版遍历序 = `BattleManager.allCardsInPlay` 的**追加序**（= 进场序），⛔ 不是棋盘格号序
//     —— `BattleManager__GetLowestHealthUnit.c:150-215`（`F7` §一 已核）；
//   · 我们的落地 = `RuleCore.StableSortByPlayOrder`（`Core/RuleCore.cs:11448`）：
//     **`PlayOrder < 0` 映射成 `int.MaxValue`（排末尾）** + **下标做第二排序键（稳定）**。
//     ⚠️ 「`-1` 排末尾」是**我们自己的兜底口径**，不是原版语义（原版每个进场都有位置，没有「-1」这回事）；
//     它保证「夹具漏取号 / 老数据缺号」时不会**静默**把没号的人当成「最早进场」。
//   · `AiTargets.AddLowestHealthUnit`（`Data/AiTargets.cs:406-441`）：候选闸（`IsAlive` /
//     `!IsPendingDeath` / `!IsRemnant` / 卡型不是 Tactic·Whispers）→ 排序 → **严格小于**才换人
//     ⇒ 并列留先者；初始 best = 该侧督军。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量，⛔ 不从实现常量反算（那会自证）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestF8EnginePlayOrder)`）。
    /// 子用例用 `F8…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestF8EnginePlayOrder()
    {
        Section("🆕 引擎线 F8：`AiTargets` 遍历序 —— 未取号（-1）不许抢在已取号之前");
        F8LowestHealthMinusOneOrder();
    }

    /// <summary>
    /// `aiTargetCriteria.targetsAffected == 240`（`lowestHealth`）走
    /// `AiTargets.AddLowestHealthUnit` ⇒ **`PlayOrder == -1` 的候选排在已取号之后**。
    ///
    /// 夹具布局（🔑 三个格号与进场序**故意错开**，否则判别不了）：
    ///   · 格 6 `F7OrdFiller`（5 血）—— **第 1 个** `Place` ⇒ 号最小，垫格用；
    ///   · 格 7 `F7OrdEarly`（2 血）—— **第 2 个** `Place` ⇒ 号居中；
    ///   · 格 5 `F7OrdLate`（2 血）—— **第 3 个** `Place` ⇒ 号最大，**人为抹成 `-1`**。
    /// ⚠️ 具体号值不写死：开局两个督军已各占一个号（`RuleCore.cs:605`），所以 Place 拿到的
    /// 是**从 2 起的连续号**；本用例只用它们的**相对大小**（且下面用 `lateOrig > early.PlayOrder` 钉住）。
    /// 期望 `F7OrdEarly`（同血并列里**有进场号且更早**的那个）。
    ///
    /// 判别力（三条实现都试过）：
    ///   · ✅ `StableSortByPlayOrder`：filler → early → late（`-1` 映射成 `int.MaxValue`，排末尾）
    ///     ⇒ 5 血先置 best、2 血换 early、late 的 2 **不严格小于** 2 ⇒ `F7OrdEarly`；
    ///   · 🧨 退回 `order.Sort((a,b) => a.PlayOrder.CompareTo(b.PlayOrder))`：`-1` 被当成最小
    ///     ⇒ late 排最前 ⇒ 实得 `F7OrdLate` ⇒ 红（这正是 `F7` 改掉的那一行）；
    ///   · 🧨 换成**格号序**（5 → 6 → 7）：late 仍排最前 ⇒ 实得 `F7OrdLate` ⇒ 红。
    /// </summary>
    static void F8LowestHealthMinusOneOrder()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active;

        var c = new AiTargetCriteriaDto
        {
            targetsAffected = 240, friendlyUnits = true, enemyUnits = false,
            maxTargets = 1, includeMinions = true,
        };

        var filler = Place(ctx, me, 6, Unit("F7OrdFiller", 1, 1, 5));   // 垫格（5/6/7 连续，不碰督军格 4）
        var early = Place(ctx, me, 7, Unit("F7OrdEarly", 1, 1, 2));     // 有号、进场早
        var late = Place(ctx, me, 5, Unit("F7OrdLate", 1, 1, 2));       // 同血、`PlayOrder` 人为抹成 -1
        int lateOrig = late.PlayOrder;                                  // 抹号前的号：用来钉「它确实后进场」
        late.PlayOrder = -1;                                            // 🔑 判别式只在这一行

        // （前提）三张都真的在盘上、号确实被抹成 -1、且 `late` 抹号前**确实后进场** ——
        // 防「夹具没摆上去 / 抹错对象 / 顺序搞反」导致断言在**错误的理由**下变绿
        // （例如池子只剩 early 一个人 ⇒ 无论怎么排都选它）。
        Check(filler != null && early != null && late != null, true,
              "★ （前提）`F7Ord*` 三张都摆上了盘"
            + "｜🧨 `Place` 没写进 `Board[]` ⇒ 池子里缺人 ⇒ 本条红（不然后面的期望值可能假绿）");
        Check(late.PlayOrder, -1,
              "★ （前提）`F7OrdLate.PlayOrder` 确实是 `-1`（未取号）"
            + "｜🧨 `Place` 改成一进门就固定发号且这里写不进去（字段名改了）⇒ 红；"
            + "这条红说明「-1」这个夹具前提没建立，下面的判别式也就不成立");
        Check(lateOrig > early.PlayOrder,
              true,
              "★ （前提）`F7OrdLate` 抹号前**确实后进场**（原号 > `F7OrdEarly`）"
            + "｜🧨 夹具摆放顺序被改成「late 先摆」⇒ 未取号与已取号之间不再构成「并列留先者」的对比 ⇒ 红");

        var got = AiTargets.GetTargets(ctx, c, Unit("F7OrdSelf", 1, 1, 3), null, me, null);
        Check(got.Count > 0 && got[0].Unit != null ? got[0].Unit.Name : "(null)", "F7OrdEarly",
              "★★ 240 的并列留**有进场号**的（`PlayOrder == -1` 排末尾，不是排最前）"
            + "｜🧨 退回 `order.Sort((a,b) => a.PlayOrder.CompareTo(b.PlayOrder))` ⇒ `-1` 排最前 ⇒ "
            + "实得 `F7OrdLate` ⇒ 红；"
            + "🧨 遍历序改成**格号序**（5 < 6 < 7）⇒ 实得 `F7OrdLate` ⇒ 红");
    }
}
