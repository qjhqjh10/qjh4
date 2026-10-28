// RuleEngineTest_EngineB94B12.cs — 第九十四会话 · 波 3 · 执行代理 `B94_B12`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆）。
// 本件只放本批新增判据的断言；宿主 `RuleEngineTest.cs` 里只需要加一个
//   `{ CheckB94B12TransformUnit(); }` 块（⚠️ 方法名**不用 `Test`/`Report` 打头**，
//   也**不进 8 空格缩进的顶层语句** ⇒ 不会被 `VerifyRunCaseCoverage` / `gen_diag_runner` 认成用例）。
//
// 本批覆盖：**`transformUnit`（账 172 + 548）** —— 原版
//   `BattleManager__AddTransformCardWithBuff.c`（入队）→ `ResolveAction.c:2931`（出队口）→
//   `BattleManager__ResolveTransformUnit.c`（体）的**状态层**。
//   落点 = `Core/TransformUnit_B94B12.cs` 的 `RuleCore.TransformUnit(...)` / `RuleCore.KeepEffectsOf(...)`。
//
// 🔴 每条都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
// 🔴 **先断前提、再断结论**（`if (x != null) Check(...)` 那种「前提不成立就静默跳过」的写法一律不用）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB94B12TransformUnit()
    {
        Section("🆕 B94_B12：transformUnit（账 172/548）—— 变身前后 卡 id / 关键词 / 血量上限 / 召唤病 的保留与重置");
        B12T1KeepEffectsFalseFullReset();
        B12T2KeepEffectsTrueCarries();
        B12T3KeepEffectsTrueHealthClamp();
        B12T4Preconditions();
        B12T5KeepEffectsOfThreeStates();
    }

    // ==================================================================
    //  T1 · `keepEffects == false` ⇒ 整份重置（卡面 / 血量 / 关键词 / 召唤病）
    //  判据 = `BattleManager__ResolveTransformUnit.c:559-655`
    //         （`:605 ResetToValuesInHand(true)` + `:654 CancelEffectsFromDisappearingUnit`）
    // ==================================================================
    static void B12T1KeepEffectsFalseFullReset()
    {
        var src = Unit("B12SrcA", 1, 3, 4, "flank");   // 旧卡：3/4，卡面带 flank
        var dst = Unit("B12DstA", 1, 2, 6);            // 新卡：2/6，卡面**不带** flank
        var ctx = Battle(new[] { src }, new[] { Unit("B12FoeA", 1, 0, 9) });
        ToP1Turn(ctx, 1);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B12SrcA"), 1), RuleCodes.OK,
                  "前提：把 `B12SrcA` 部署到 P0 棋盘");

        int slot = B12SlotByName(ctx, 0, "B12SrcA");
        CheckTrue(slot >= 0, "前提：在 P0 棋盘上找到 `B12SrcA`");
        var before = slot >= 0 ? ctx.Players[0].Board[slot] : null;
        CheckTrue(before != null, "前提：`B12SrcA` 那一格上真有单位");
        if (before == null) return;

        // 前提：落位时它是**能动**的（卡面带 flank ⇒ `HasDeployExemption` 为真）
        CheckTrue(before.Has("flank"), "前提：`B12SrcA` 卡面带 flank");
        CheckTrue(!before.Exhausted, "前提：带 flank 落位 ⇒ 当回合能动（`Exhausted == false`）");

        int oldInstanceId = before.InstanceId;
        int oldSlot = slot;

        bool ok = RuleCore.TransformUnit(ctx, 0, slot, dst, keepEffects: false);
        CheckTrue(ok, "`TransformUnit(keepEffects: false)` 返回 true");
        CheckTrue(ReferenceEquals(ctx.Players[0].Board[oldSlot], before) == false,
                  "前提复核：格位上的**对象换了**（不是原地改 def）");

        var after = ctx.Players[0].Board[oldSlot];
        CheckTrue(after != null, "变身之后那一格上仍有单位");
        if (after == null) return;

        // ---- 卡 id ----
        // 🧨 把 `TransformUnit` 里的 `new UnitState(ni, …)` 改回沿用旧实例 ⇒ 这条会红。
        Check(after.Card != null ? after.Card.Name : "(null)", "B12DstA",
              "🧨 变身之后卡 id = 新卡 `B12DstA`");
        // ---- 实例号沿用（原版是同一个 CardScript，`+0x22c` 不变）----
        Check(after.InstanceId, oldInstanceId,
              "🧨 实例号**沿用**（原版同一个 `CardScript`，`+0x22c` 不变；≠ 新发号）");
        // ---- 血量：ResetToValuesInHand ⇒ 满血 = 新卡血量 ----
        Check(after.Health, 6, "🧨 `keepEffects:false` ⇒ 血量重置成新卡满血 6（`ResetToValuesInHand`）");
        Check(after.MaxHealth, 6, "血量上限 = 新卡 6");
        // ---- 关键词：旧卡的 `flank` 被清掉、新卡面那条也没有 ----
        // 🧨 在 `keepEffects==false` 支里也不跑 `ResetToValuesInHand` ⇒ 这条会红（flank 会留着）。
        CheckTrue(!after.Has("flank"), "🧨 `keepEffects:false` ⇒ 旧卡的 `flank` **没了**（整份重置）");
        // ---- 召唤病：同一个 `keepEffects` 位也控制它（`ResolveTransformUnit.c:1207` 第 5 实参）----
        CheckTrue(after.SummonSickness, "🧨 `keepStatus = keepEffects = false` ⇒ 写召唤病 `+0x58 = 1`");
        CheckTrue(after.Exhausted, "召唤病在身且新卡面无 fast/flank ⇒ 当回合不能动（`Exhausted`）");
        // ---- 落点不变 ----
        Check(B12SlotByName(ctx, 0, "B12DstA"), oldSlot, "变身**不挪格位**（原版同一个对象，位置不动）");
    }

    // ==================================================================
    //  T2 · `keepEffects == true` ⇒ 只摘「来源 = 它自己」的效果，**不**重置
    //  判据 = `BattleManager__ResolveTransformUnit.c:656-703`（`:702 RemoveOwnExtrinsicEffects`）
    // ==================================================================
    static void B12T2KeepEffectsTrueCarries()
    {
        var src = Unit("B12SrcB", 1, 3, 4, "flank");   // 卡面带 flank
        var dst = Unit("B12DstB", 1, 2, 6);            // 新卡面**没有** flank
        var ctx = Battle(new[] { src }, new[] { Unit("B12FoeB", 1, 0, 9) });
        ToP1Turn(ctx, 1);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B12SrcB"), 1), RuleCodes.OK,
                  "前提：把 `B12SrcB` 部署到 P0 棋盘");

        int slot = B12SlotByName(ctx, 0, "B12SrcB");
        CheckTrue(slot >= 0, "前提：在 P0 棋盘上找到 `B12SrcB`");
        if (slot < 0) return;
        var before = ctx.Players[0].Board[slot];
        CheckTrue(before != null, "前提：`B12SrcB` 那一格上真有单位");
        if (before == null) return;

        before.Health = 2;    // 受伤：4 → 2
        Check(before.Health, 2, "前提：`B12SrcB` 现在是 2 血（受伤态）");
        CheckTrue(before.Has("flank"), "前提：`B12SrcB` 身上有 flank");

        bool ok = RuleCore.TransformUnit(ctx, 0, slot, dst, keepEffects: true);
        CheckTrue(ok, "`TransformUnit(keepEffects: true)` 返回 true");

        var after = ctx.Players[0].Board[slot];
        CheckTrue(after != null, "变身之后那一格上仍有单位");
        if (after == null) return;

        Check(after.Card != null ? after.Card.Name : "(null)", "B12DstB",
              "🧨 变身之后卡 id = 新卡 `B12DstB`");
        // ---- 血量：这一支**不**跑 ResetToValuesInHand ⇒ 当前血量保留（不回满）----
        // 🧨 在 `keepEffects==true` 支里也调 `ResetToValuesInHand` ⇒ 这条会变成 6 而且红。
        Check(after.Health, 2, "🧨 `keepEffects:true` ⇒ **保留当前血量 2**（不回满）");
        Check(after.MaxHealth, 6, "血量上限 = **新卡**的 6（卡面换了 ⇒ base 跟着换）");
        // ---- 关键词：旧身上那条 `flank` 属于「覆盖层」⇒ 搬过去 ----
        // 🧨 把 `CarryKeywordOverlay` 去掉 ⇒ 这条会红（新卡面没有 flank ⇒ 变成 false）。
        CheckTrue(after.Has("flank"), "🧨 `keepEffects:true` ⇒ 旧身的关键词覆盖层**跟着走**（flank 还在）");
        // ---- 召唤病：`keepStatus = keepEffects = true` ⇒ **不写** `+0x58` ----
        // 判据 = `_MinionPlayedIntoField_d__314__MoveNext.c:69-71` 的 `if (!keepStatus) card.summonSickness = true;`
        CheckTrue(!after.SummonSickness, "🧨 `keepStatus = true` ⇒ **不写**召唤病 `+0x58`");
        CheckTrue(!after.Exhausted, "没有召唤病 ⇒ 当回合能动（`Exhausted == false`）");
        // ---- 原版这一支**不**撤待结算动作（照抄的 bug）----
        // 🧨 在真支里也调 `CancelEffectsFromDisappearingUnit` ⇒ 行为会与原版分叉（今天没有
        //    「变身时队列里有东西」的可达局面，所以这条只在代码审查上成立 ⇒ 用注释钉死）。
        CheckTrue(after.Has("flank"), "（真支不撤队列 —— 原版 `:656-703` 全文无 `CancelEffectsFromDisappearingUnit`）");
    }

    // ==================================================================
    //  T3 · `keepEffects == true` 时血量被**新卡上限**夹住
    // ==================================================================
    static void B12T3KeepEffectsTrueHealthClamp()
    {
        var big = Unit("B12BigC", 1, 1, 10);   // 10 血
        var small = Unit("B12SmallC", 1, 1, 3); // 新卡只有 3 血
        var ctx = Battle(new[] { big }, new[] { Unit("B12FoeC", 1, 0, 9) });
        ToP1Turn(ctx, 1);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B12BigC"), 1), RuleCodes.OK,
                  "前提：把 `B12BigC`（10 血）部署到 P0 棋盘");

        int slot = B12SlotByName(ctx, 0, "B12BigC");
        CheckTrue(slot >= 0, "前提：在 P0 棋盘上找到 `B12BigC`");
        if (slot < 0) return;
        var before = ctx.Players[0].Board[slot];
        CheckTrue(before != null && before.Health == 10, "前提：`B12BigC` 是满血 10");
        if (before == null) return;

        bool ok = RuleCore.TransformUnit(ctx, 0, slot, small, keepEffects: true);
        CheckTrue(ok, "`TransformUnit` 返回 true");

        var after = ctx.Players[0].Board[slot];
        CheckTrue(after != null, "变身之后那一格上仍有单位");
        if (after == null) return;

        // 🧨 去掉 `if (hp > cap) hp = cap;` 那一句 ⇒ 血量会停在 10 > 上限 3（不报错、静默错）。
        Check(after.MaxHealth, 3, "新卡上限 = 3");
        Check(after.Health, 3, "🧨 血量被**新卡上限**夹到 3（10 → 3）");
    }

    // ==================================================================
    //  T4 · 前置：空格 / 空卡 ⇒ 返回 false 且**不动棋盘**
    //  判据 = `BattleManager__ResolveTransformUnit.c:106-155`（目标为 null ⇒ 只出声、什么都不换）
    // ==================================================================
    static void B12T4Preconditions()
    {
        var src = Unit("B12SrcD", 1, 2, 5);
        var ctx = Battle(new[] { src }, new[] { Unit("B12FoeD", 1, 0, 9) });
        ToP1Turn(ctx, 1);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B12SrcD"), 1), RuleCodes.OK,
                  "前提：把 `B12SrcD` 部署到 P0 棋盘");
        int slot = B12SlotByName(ctx, 0, "B12SrcD");
        CheckTrue(slot >= 0, "前提：在 P0 棋盘上找到 `B12SrcD`");
        if (slot < 0) return;

        // ---- 空卡 ----
        var keep = ctx.Players[0].Board[slot];
        CheckTrue(!RuleCore.TransformUnit(ctx, 0, slot, null, keepEffects: false),
                  "🧨 `newCard == null` ⇒ 返回 false（原版对空目标只记一条错误）");
        CheckTrue(ReferenceEquals(ctx.Players[0].Board[slot], keep),
                  "空卡那一趟**没动棋盘**");

        // ---- 空格（挑一个该侧没有单位的格）----
        int empty = -1;
        for (int s = 0; s < BoardSpec.Size; s++)
            if (s != BoardSpec.WarlordSlot && ctx.Players[1].Board[s] == null) { empty = s; break; }
        CheckTrue(empty >= 0, "前提：找到 P1 侧一个空格");
        if (empty >= 0)
        {
            var dst = Unit("B12DstD", 1, 1, 1);
            CheckTrue(!RuleCore.TransformUnit(ctx, 1, empty, dst, keepEffects: false),
                      "🧨 空格上变身 ⇒ 返回 false（不静默造出一个单位）");
            CheckTrue(ctx.Players[1].Board[empty] == null, "空格仍然是空格（没有被凭空补一个单位）");
        }

        // ---- 越界格 ----
        CheckTrue(!RuleCore.TransformUnit(ctx, 0, BoardSpec.Size, src, keepEffects: false),
                  "越界格号 ⇒ 返回 false");
    }

    // ==================================================================
    //  T5 · `KeepEffectsOf` 三态（真 / 假 / **读不到**）
    //  判据 = `SummonCriteria.keepEffects (+0x2A)`（`AbilityLogic__PlayAbility.c:903/939/977`）
    // ==================================================================
    static void B12T5KeepEffectsOfThreeStates()
    {
        // ---- ① 整列未知（`AbilitySummons == null`，手写夹具 / 旧表）⇒ null ----
        var plain = Unit("B12Plain", 1, 1, 1);
        CheckTrue(!plain.HasAbilitySummon, "前提：手写夹具没有 `abilitySummon` 列");
        CheckTrue(RuleCore.KeepEffectsOf(plain) == null,
                  "🧨 整列未知 ⇒ `KeepEffectsOf` 返回 **null**（⛔ 不许当成 false）");

        // ---- ② `-1` 哨兵（字段读不到）⇒ null ----
        var sentinel = Unit("B12Sentinel", 1, 1, 1);
        sentinel.AttachAbilitySummon(new[] { new CardDef.AbilitySummonSpec { Ab = 0, Lg = 0, KeepEffects = -1 } });
        CheckTrue(RuleCore.KeepEffectsOf(sentinel) == null,
                  "🧨 `keepEffects == -1`（读不到）⇒ 返回 **null**，⛔ 不许落到 false");

        // ---- ③ 明确 0 / 1 ----
        var zero = Unit("B12Zero", 1, 1, 1);
        zero.AttachAbilitySummon(new[] { new CardDef.AbilitySummonSpec { Ab = 0, Lg = 0, KeepEffects = 0 } });
        Check(RuleCore.KeepEffectsOf(zero), (bool?)false, "🧨 `keepEffects == 0` ⇒ false");

        var one = Unit("B12One", 1, 1, 1);
        one.AttachAbilitySummon(new[] { new CardDef.AbilitySummonSpec { Ab = 0, Lg = 0, KeepEffects = 1 } });
        Check(RuleCore.KeepEffectsOf(one), (bool?)true, "🧨 `keepEffects == 1` ⇒ true");

        // ---- ④ null 卡 ----
        CheckTrue(RuleCore.KeepEffectsOf(null) == null, "`card == null` ⇒ null");
    }

    // ---------------- 本件私有小工具（与宿主里的同名助手不冲突）----------------

    /// <summary>按卡名在某一侧棋盘上找格号（找不到返回 -1）。</summary>
    static int B12SlotByName(BattleContext ctx, int p, string name)
    {
        var board = ctx.Players[p].Board;
        for (int s = 0; s < board.Length; s++)
        {
            var u = board[s];
            if (u != null && u.Card != null && u.Card.Name == name) return s;
        }
        return -1;
    }
}
