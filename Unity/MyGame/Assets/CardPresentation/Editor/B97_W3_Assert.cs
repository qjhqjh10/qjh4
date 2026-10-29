// B97_W3_Assert.cs — 第九十七会话 · 执行代理 `B97_W3`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆，见
// `RuleEngine/Editor/RuleEngineTest_EngineB94F1.cs` 的同一做法）。本件只放本批**新增行为**的断言；
// 宿主 `RuleEngineTest.cs` 里只加一个 `{ CheckB97W3CemeteryDequeueAndTransformTail(); }`
// （⚠️ 方法名**不用 `Test`/`Report` 打头**、且**故意缩进深一层** ⇒ 不会被
// `VerifyRunCaseCoverage` / `gen_diag_runner` 认成用例，与 `B93_G1` / `B94_F1` 既有做法同形）。
//
// 本件覆盖两条**真实行为改动**（判据 = `d:/2/tools/decomp_full/`，2026-10-29 逐句亲读）：
//   ① 账 534 · `BattleContext.ResolveRemoveFromCemetery(CardInstance)` —— **动作 `removeFromCemetery(60)`
//      的出队处理口**。原版 `BattleManager__ResolveRemoveFromCemetery.c:82-134` 只做两件事：
//      卡非空 ⇒ `CardScript.RemoveCardFromCemetery(那张)`（全文一句 `*(card + 0x24b) = 0`）、
//      卡为空 ⇒ `LogWarning`；`:182/:229` 收尾（我们的等价物 = 动作泵 `ActionScope.Dispose`）。
//      ⇒ **放逐后的牌从所有墓地查询里永久消失**（`GetCardsInCemetery.c:54`）。
//   ② 账 548 · `RuleCore.TransformUnitPostResolve` 的两跳 ——
//      `.c:1775-1825` `HasCurrentTrait(0x4B0 = ambush)` ⇒ `SetAmbush(true)`（我们的状态位 = `FaceDown`）；
//      `.c:1829/:1831` 两手 `PlayerHand.UpdateCardEffects(hand,0,0)` ⇒ `RuleCore.ExpireHandBuffs(ctx, false)`；
//      `.c:1931/:1981` 两手 `PlayerHand.CheckEffectsOnNewCard(hand, card)` ⇒ `RuleCore.SetupCardInHand`
//      （两个原版方法的**核心循环逐句同形**，见 `TransformUnit_B94B12.cs` 类尾那个方法的 doc）。
//
// 🔴 每条都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB97W3CemeteryDequeueAndTransformTail()
    {
        Section("🆕 B97_W3：`removeFromCemetery(60)` 的出队处理口（账 534）· `TransformUnit` 末两跳（账 548）");
        B97W3T1CemeteryDequeue();
        B97W3T2TransformPostResolve();
    }

    // ==================================================================
    //  ① 账 534：出队处理口 —— 入队一条 `actionType 60`，泵点出队后那张牌被**永久放逐**
    // ==================================================================

    /// <summary>
    /// 判据链：`AddRemoveFromCemetery.c:31-35`（组 `actionType 0x3c (60)` 入队）→
    /// `ResolveRemoveFromCemetery.c:82-134`（卡非空 ⇒ `RemoveCardFromCemetery`）→
    /// `CardScript__RemoveCardFromCemetery.c:1-8`（全文一句 `*(card + 0x24b) = 0`）→
    /// `GetCardsInCemetery.c:54`（`+0x24b != 0` 才收）。
    /// </summary>
    static void B97W3T1CemeteryDequeue()
    {
        var ctx = Battle(new[] { Unit("RemA", 1, 1, 1), Unit("RemB", 1, 1, 1) },
                         new[] { Unit("RemFoe", 1, 0, 9) });
        ToP1Turn(ctx, 2);

        // ---- 前提（先硬检查，⛔ 别写成 `if (前提) Check(结论)`）----
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "RemA"), 1), RuleCodes.OK, "前提：打出 `RemA`");
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "RemB"), 2), RuleCodes.OK, "前提：打出 `RemB`");
        var a = B97W3UnitByName(ctx, 0, "RemA");
        var b = B97W3UnitByName(ctx, 0, "RemB");
        CheckTrue(a != null && b != null, "前提：两张都在场上");
        if (a == null || b == null) return;                 // 前提不成立就别往下测（会 NRE）

        // 用 `HurtWithSourceForTest`（自带 `ActionScope` + `RemoveIfDead`）把两张打进墓地。
        RuleCore.HurtWithSourceForTest(ctx, a, 9, "自检");
        RuleCore.HurtWithSourceForTest(ctx, b, 9, "自检");

        var cem0 = RuleCore.GetCardsInCemetery(ctx, true, true, true, true, true, true);
        var ia = B97W3CemFind(cem0, "RemA");
        var ib = B97W3CemFind(cem0, "RemB");
        CheckTrue(ia != null && ib != null, "前提：两张都进了墓地、且墓地查询**都查得到**");
        if (ia == null || ib == null) return;

        // ---- 前提：空卡**不入队**（原版 `.c:32` 的 `if (param_2 != 0)`）----
        bool emptyQueued;
        using (var sc = RuleCore.BeginAction(ctx)) { emptyQueued = ctx.AddRemoveFromCemetery(null); }
        CheckTrue(!emptyQueued, "前提：空卡 ⇒ `AddRemoveFromCemetery` 返回 false（**不入队**）");
        CheckTrue(!RuleCore.IsExiledFromCemetery(ctx, ia) && !RuleCore.IsExiledFromCemetery(ctx, ib),
                  "前提：空卡那一次**没有**误放逐任何一张");

        // ---- 被测：入队一条 `removeFromCemetery(60)`，让**泵点**把它出队结算 ----
        bool queued;
        using (var sc = RuleCore.BeginAction(ctx)) { queued = ctx.AddRemoveFromCemetery(ia); }
        // 🧨 把 `AddRemoveFromCemetery` 里的 `ResolveRemoveFromCemetery(card)` 换回「什么都不做」
        //    或把 `ResolveRemoveFromCemetery` 的空卡/非空两支写反 ⇒ 下面第一条红。
        CheckTrue(queued, "被测：`AddRemoveFromCemetery(RemA)` 入队成功");
        CheckTrue(RuleCore.IsExiledFromCemetery(ctx, ia),
                  "🧨 出队处理口跑过了 ⇒ `RemA` 已被放逐（原版 `ResolveRemoveFromCemetery` → `+0x24b = 0`）");

        var cem1 = RuleCore.GetCardsInCemetery(ctx, true, true, true, true, true, true);
        // 🧨 把 `GetCardsInCemetery` 里那句 `|| IsExiledFromCemetery(ctx, inst)` 删掉 ⇒ 本条红。
        CheckTrue(B97W3CemFind(cem1, "RemA") == null,
                  "🧨 放逐后 `RemA` **不再出现**在任何墓地查询里（原版 `GetCardsInCemetery.c:54`）");
        // 灭自证：**只影响那一份**，别把整条墓地一起挡掉。
        CheckTrue(B97W3CemFind(cem1, "RemB") != null, "灭自证：没被放逐的 `RemB` **仍然查得到**");
        Check(cem1.Count, 1, "灭自证：墓地查询 2 → 1（不是 0）");

        // 幂等：同一份再入队一次，结果不变（原版那句 `= 0` 本来就是幂等的）。
        using (var sc = RuleCore.BeginAction(ctx)) { ctx.AddRemoveFromCemetery(ia); }
        Check(RuleCore.GetCardsInCemetery(ctx, true, true, true, true, true, true).Count, 1,
              "放逐是幂等的（再入队一次，墓地查询仍是 1 张）");
    }

    // ==================================================================
    //  ② 账 548：`TransformUnit` 的两跳尾巴
    // ==================================================================

    /// <summary>
    /// 判据：`BattleManager__ResolveTransformUnit.c:1775-1825` ——
    /// `if (local_308 != 0) { if (EntityScript__HasCurrentTrait(local_308, 0x4b0, 0)) CardScript__SetAmbush(local_308, 1, 0); }`。
    /// `0x4B0 = 1200` = `KeywordTable.Ambush`（`RuleCore.PlaysFaceDown` 用的是同一个词条）；
    /// 我们的状态位 = `UnitState.FaceDown`。
    /// <para>⚠️ 本件**只断伏击那一跳**。另两跳（`UpdateCardEffects` / `CheckEffectsOnNewCard`）需要一张能
    /// 「往手牌登记表里放记录」的夹具（生产点是战术卡的 `GrantHandBuff` 一族），本批没有现成可复用的夹具
    /// ⇒ 那两跳在报告里如实标「**未断**」（不是「断了通过」）。</para>
    /// </summary>
    static void B97W3T2TransformPostResolve()
    {
        var plain = Unit("PlainUnit", 1, 1, 1);
        var form = Unit("AmbushForm", 1, 1, 1, "ambush");       // 变身后带 `Ambush`
        var plainForm = Unit("PlainForm", 1, 1, 1);            // 灭自证：变身后**不带** `Ambush`
        CheckTrue(form.Has(KeywordTable.Ambush), "前提：`AmbushForm` 身上有 `ambush`（`CardDef.Has`）");
        CheckTrue(!plain.Has(KeywordTable.Ambush), "前提：`PlainUnit` 身上**没有** `ambush`");

        var ctx = Battle(new[] { plain }, new[] { Unit("T2Foe", 1, 0, 9) });
        ToP1Turn(ctx, 2);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "PlainUnit"), 1), RuleCodes.OK,
                  "前提：打出 `PlainUnit`");
        var u = B97W3UnitByName(ctx, 0, "PlainUnit");
        CheckTrue(u != null, "前提：`PlainUnit` 在场上");
        if (u == null) return;
        int slot = B97W3SlotOf(ctx, 0, u);
        CheckTrue(slot >= 0, "前提：能定位到它的格号");
        if (slot < 0) return;
        CheckTrue(!u.FaceDown, "前提：普通单位不是面朝下（`FaceDown == false`）");

        // ---- 被测：变成一张**带 Ambush** 的卡 ----
        CheckTrue(RuleCore.TransformUnit(ctx, 0, slot, form, keepEffects: false),
                  "被测：`TransformUnit` 变身成功（`PlainUnit` ⇒ `AmbushForm`，keepEffects = false）");
        var nu = ctx.Players[0].Board[slot];
        CheckTrue(nu != null && nu.Card != null && nu.Card.Id == form.Id,
                  "前提：那一格上已经是新卡 `AmbushForm`");
        if (nu == null) return;
        // 🧨 删掉 `TransformUnitPostResolve` 里那段 `if (nu.Has(KeywordTable.Ambush)) nu.FaceDown = true;`
        //    （或把它挪到换卡**之前**、拿旧单位判）⇒ 本条红。
        CheckTrue(nu.FaceDown,
                  "🧨 变身后带 `Ambush` ⇒ **面朝下**（原版 `.c:1825` 的 `SetAmbush(card, 1, 0)`）");

        // ---- 灭自证：同一格再变成**不带 Ambush** 的卡 ⇒ 必须**不是**面朝下 ----
        //   否则「无条件置 FaceDown」也能让上面那条绿 —— 那正是要挡的改法。
        CheckTrue(RuleCore.TransformUnit(ctx, 0, slot, plainForm, keepEffects: false),
                  "灭自证：再变身成 `PlainForm`（不带 Ambush）");
        var nu2 = ctx.Players[0].Board[slot];
        CheckTrue(nu2 != null && nu2.Card != null && nu2.Card.Id == plainForm.Id, "灭自证：格上已是 `PlainForm`");
        if (nu2 != null)
            CheckTrue(!nu2.FaceDown,
                      "灭自证：不带 `Ambush` 的新卡 ⇒ `FaceDown == false`（证明上面那条是**按关键词判**的）");
    }

    // ---------------- 本件私有小工具（与宿主里的同名助手不冲突）----------------

    /// <summary>按卡名在某一侧棋盘上找那个单位（我们的小格号不保证，扫全场最稳）。</summary>
    static UnitState B97W3UnitByName(BattleContext ctx, int p, string name)
    {
        var board = ctx.Players[p].Board;
        for (int s = 0; s < board.Length; s++)
        {
            var u = board[s];
            if (u != null && u.Card != null && u.Card.Name == name) return u;
        }
        return null;
    }

    /// <summary>那个单位在哪一格（`-1` = 没找到）。</summary>
    static int B97W3SlotOf(BattleContext ctx, int p, UnitState u)
    {
        var board = ctx.Players[p].Board;
        for (int s = 0; s < board.Length; s++)
            if (ReferenceEquals(board[s], u)) return s;
        return -1;
    }

    /// <summary>在墓地查询结果里按卡名找那一份实例。</summary>
    static CardInstance B97W3CemFind(List<CardInstance> list, string name)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].Card != null && list[i].Card.Name == name) return list[i];
        return null;
    }
}
