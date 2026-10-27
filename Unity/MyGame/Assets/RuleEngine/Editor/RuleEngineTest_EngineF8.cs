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
        F8ActiveAbilityEntryEnumerated();   // 🆕 2026-10-27（`B88_E3`）：AI 动作表的第 ④ 档
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

    // ==================================================================
    //  🆕 2026-10-27（`B88_E3` · 账 B03 · 第八十八会话波 1）：
    //  **第 ④ 档主动能力（原版通用 `activeAbilityLogic`）必须进 AI 动作表**
    //
    //  判据（⛔ 不要重新论证）：
    //    · 原版 `AI.GetAvailableActions` 对 `activeAbility`(5) 产一条候选；打分只读 `ActiveAbility`
    //      自己的两列（`nonTargetScoringCriteria(+0x58)` → 再 `cardTargetScoringCriteria(+0x50)`）
    //      → `资料/完全复刻/判据/AI打分与动作选择_第2轮.md` §2·1（`activeAbility` 支）。
    //    · 枚举口原来只有 `oath` / 替代行动 / `Ability:` 自定文法三条 ⇒ 池内唯一「五缺」的
    //      第 ④ 档卡 `SOR66 Armorium Cherub` 在 AI 手里永远不发动
    //      （基线读数 `ruleprobe ai actions SOR66` = 0 条 `activeAbility`）。
    //    · 新口 = `SimpleAI.AddActiveAbilityEntry`（兄弟分册 `Data/SimpleAI.ActiveEntry.cs`），
    //      从 `Data/SimpleAI.cs:421` 的「`u.Ability == null`」分叉点调用。
    //
    //  ⚠️ 这里**不新建 `Test*` 方法**：`Run()` 的 `RunCaseNames` ↔ 反射对账要求「每个 `Test*` 各有一条
    //     `Step`」，而 `RuleEngineTest.cs` 不在本批白名单 ⇒ 只能挂在既有用例 `TestF8EnginePlayOrder` 里
    //     （本文件独有的是 AI 侧引擎判据，故挂这里）。
    // ==================================================================

    /// <summary>④ 档进动作表 · 目标在**己方** · 两个负对照（`hasActiveAbility=0` / 已有替代行动入口）。</summary>
    static void F8ActiveAbilityEntryEnumerated()
    {
        // 夹具形状照 `SOR66`：`hasActiveAbility=1` ∧ `Ability:` 正文 / 替代行动 / 誓约三条口都没有，
        // `activeAbilityLogic` = 两行（`chosenAbility` = `buffOthers(30)` / `triggerPray(462)`），
        // 第 0 行 `targetsAffected = 30`（= `target`，要调用方点一个目标）。
        CardDef AaCard(int hasActive)
        {
            return new CardDef("F8AACard" + hasActive, "F8AACard" + hasActive, "unit", "", "common", "Test",
                               1, 1, 3, 0, null,
                               subtype: "Infantry",
                               hasActiveAbility: hasActive,
                               hasAlternativeAbility: 0,
                               activeAbilityLogic: new[]
                               {
                                   new CardDef.ActiveAbilityLogicSpec
                                   {
                                       Ability = CardDef.ActiveAbilityObject,
                                       Gate = 1,
                                       TargetsAffected = new[] { 30, 20 },
                                       ChosenAbility = new[] { 30, 462 },
                                   },
                               },
                               activeAbilityLogicVersioned: true);
        }

        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        var u = Place(ctx, 0, 3, AaCard(1), exhausted: false);

        // ---- 前提（硬检查，⛔ 不写 `if (前提) Check(结论)`）----
        CheckTrue(u != null && ctx.Active == 0,
                  "④ （前提）第 0 号玩家是当前行动方、且载体已摆上 3 号格"
                + "｜🧨 `ToP1Turn` / `Place` 的夹具口径变了 ⇒ 本条红，下面几条就不算证据");
        Check(u.Card.HasActiveAbility, 1,
              "④ （前提）夹具的 `hasActiveAbility = 1`（`CardScript.hasActiveAbility` `+0x220` 那一格）"
            + "｜🧨 构造口把这一格吞掉（`CardDef` 的 `hasActiveAbility` 形参没接上）⇒ 红");
        CheckTrue(!u.HasAbility && RuleCore.AlternativeActionKeyword(u) == null && u.Card.OathOps.Count == 0,
                  "④ （前提）**前三条等价入口在数据上都不存在**（`Ability:` 正文 / 替代行动 / 誓约）"
                + "—— 这正是「第 ④ 档」的定义（判据 = `BattleDriver.IsActiveAbilityEntry` 的 doc）"
                + "｜🧨 夹具里混进 `Duty`/`Oath` 之类的关键词 ⇒ 红；那时下面的「进表」会由别的那一口产出，"
                + "本断言就不再是这个意思");
        CheckCode(RuleCore.CanStartActiveAbility(ctx, 0, 3), RuleCodes.OK,
                  "④ （前提）引擎说这一格**能开始**主动能力（`BattleManager__CanUseActiveAbility` 的六道闸全过）"
                + "｜🧨 费用闸 / `Exhausted` 夹具变了 ⇒ 红");

        // ---- ① 正面：动作表里有一条 `activeAbility` 且带第 ④ 档哨兵 ----
        int nActive = 0, nOnSlot3 = 0;
        bool targetOnSelf = true;      // 只要出现过一条「目标侧不是己方」就翻假
        foreach (var a in SimpleAI.EnumerateActions(ctx))
        {
            if (a.Kind != AiActionKind.ActiveAbility) continue;
            if (a.AltKeyword != SimpleAI.AltActiveAbility) continue;
            nActive++;
            if (a.Slot != 3) continue;
            nOnSlot3++;
            if (a.TargetP != 0) targetOnSelf = false;      // `-1`（不用点目标）与 `0`（己方格）都算己方
        }
        CheckTrue(nActive > 0,
                  "★★ ④ **第 ④ 档主动能力进了 AI 动作表**（`Kind = activeAbility(5)` ∧ `AltKeyword = \"active\"`）"
                + "｜🧨 把 `Data/SimpleAI.cs:421` 那一句改回 `if (spec == null) continue;` ⇒ 动作表里 0 条 ⇒ 红"
                + "（= 改前：`SOR66` 在 AI 手里永远不会发动，且没有任何报错）");
        CheckTrue(nOnSlot3 > 0 && targetOnSelf,
                  "★★ ④ 候选落在**这个载体**上、目标侧 = **己方**（`TargetP == 0`）"
                + "—— 原版 `SOR66` 的 `targetCriteria` 是 `friendlyUnits=1`，而引擎那一口 "
                + "`RuleCore.CanUseActiveAbility` 的 `targetOwner` 写死成施放者那一侧"
                + "｜🧨 照「普通主动技」那一支扫**敌方**棋盘（`foeBoard`）⇒ 一条候选都产不出来 ⇒ 红；"
                + "🧨 把 `TargetP` 填成 `1 - me` ⇒ 红");

        // ---- ② 负对照 A：`hasActiveAbility = 0` ⇒ 不进表（灭自证：谓词那一位真的在起作用）----
        {
            var c2 = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(c2, 1);
            Place(c2, 0, 3, AaCard(0), exhausted: false);
            int cnt = 0;
            foreach (var a in SimpleAI.EnumerateActions(c2))
                if (a.Kind == AiActionKind.ActiveAbility && a.AltKeyword == SimpleAI.AltActiveAbility) cnt++;
            Check(cnt, 0,
                  "★★ ④ **负对照：`hasActiveAbility = 0` ⇒ 动作表里 0 条**"
                + "（原版 `CardScript.SetBaseCardTraits` 是**无条件**从数据灌这一格 ⇒ `0` 就是「这张卡没有」）"
                + "｜🧨 这一条的判别力来自**两道闸的合取**（`AddActiveAbilityEntry` 的谓词 ∧ "
                + "`RuleCore.CanStartActiveAbility` 的 `HasActiveAbility != 1 ⇒ ErrNoAbility`）⇒ "
                + "**只删谓词那一行它仍是 0（不红）**，⛔ 别把它写成「删谓词就红」；"
                + "真能让它红的是「两道闸一起放宽」（删谓词 **且** 把引擎那道闸也去掉）"
                + "—— 那时 AI 会给一张**没有这颗钮**的卡产候选 ⇒ 实得 > 0 ⇒ 红");
        }

        // ---- ③ 负对照 B：已有**替代行动**入口 ⇒ 由那一口产出，⛔ 不许再产一条第 ④ 档 ----
        //    现池 87 张 `hasActiveAbility == 1`，其中 86 张同时带 `duty`/`pray`/`agenda`/`ferocity`/`oath`
        //    ⇒ 少了这道排除，那 86 张会被**多产一条动作**（重复动作 + 打分口径不同）。
        {
            var c3 = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(c3, 1);
            var duty = new CardDef("F8AADuty", "F8AADuty", "unit", "Duty: Deal 3 damage to an enemy troop",
                                   "common", "Test", 2, 2, 3, 0, new[] { KeywordTable.Duty },
                                   subtype: "Infantry",
                                   hasActiveAbility: 1,
                                   hasAlternativeAbility: 0,
                                   activeAbilityLogic: new[]
                                   {
                                       new CardDef.ActiveAbilityLogicSpec
                                       {
                                           Ability = CardDef.ActiveAbilityObject, Gate = 1,
                                           TargetsAffected = new[] { 30, 20 },
                                           ChosenAbility = new[] { 30, 462 },
                                       },
                                   },
                                   activeAbilityLogicVersioned: true);
            Place(c3, 0, 3, duty, exhausted: false);
            Place(c3, 1, 3, Unit("F8AAPrey", 1, 1, 3), exhausted: true);   // 替代行动要有目标
            int alt = 0, act = 0;
            foreach (var a in SimpleAI.EnumerateActions(c3))
            {
                if (a.Kind != AiActionKind.ActiveAbility) continue;
                if (a.AltKeyword == KeywordTable.Duty) alt++;
                else if (a.AltKeyword == SimpleAI.AltActiveAbility) act++;
            }
            CheckTrue(alt > 0,
                      "④ （前提）`duty` 那一口**确实**产出了候选（否则下面那条的 0 可能只是「两条都没产」）"
                    + "｜🧨 `duty` 夹具失效 ⇒ 红");
            Check(act, 0,
                  "★★ ④ **负对照：有替代行动入口的卡不产第 ④ 档的重复候选**"
                + "｜🧨 把 `RuleCore.AlternativeActionKeyword(u) != null` 那一条排除删掉 ⇒ 实得 > 0 ⇒ 红"
                + "（现池 86 张卡会各多一条重复动作）");
        }
    }
}
