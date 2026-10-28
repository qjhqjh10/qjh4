// RuleEngineTest_EngineB94B14.cs — 引擎线 `B94_B14`（2026-10-29 · 第九十四会话 · 波 3）
// 文件所有者：`RuleEngine/Core/EffectResolver.cs`（账 B14 那 15 行 B/C 的 `EffectResolver` 半边）。
//
// ⚠️ 接线只差 `RuleEngineTest.cs` 的 `Run()` 里**一行**：
//     Step(CheckB94B14EffectResolverGaps);
// 那个文件**归调度台**（本代理没碰，`⛔ 不动 Editor/RuleEngineTest.cs`）。
//
// 🔴 每条断言都带 `🧨`（写清什么改动能让它红）。期望值一律写死字面量。
// 🔴 先断「前提」、再断「结论」（⛔ 不写 `if (前提) Check(结论)`——那会在前提不成立时静默跳过）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（⚠️ 不以 `Test`/`Report` 打头，避免被 `VerifyRunCaseCoverage` / `gen_diag_runner` 认成用例）。</summary>
    public static void CheckB94B14EffectResolverGaps()
    {
        Section("🆕 引擎线 B14 · B94：`Reanimated` 广播次序（546）· jam 清 `CardInstance` 两本账（521）· 破坏卡按 `spellType`（506）");
        B94B14Case1ReanimatedBroadcastOrder();
        B94B14Case2JamClearsInstanceBooks();
        B94B14Case3SabotageBySpellType();
    }

    /// <summary>
    /// **账 546 ① —— `ResolveTransformFromRemnant` 的 `BroadcastUnitReanimated` 排在
    /// `ResetToValuesInHand` 之后【第二句】**。
    ///
    /// <para>**判据（🥇 `decomp_full/BattleManager.__c__DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:7-31`）**：
    /// `ResetToValuesInHand(true)`（`:7`）→ **`BroadcastUnitReanimated`** → `SetAsUnitInPlay(false)` →
    /// `ResetSummonSickness` → `ActivateTraitsOnSummonOrEnchantment` → `UpdateWhileInPlay(true)` →
    /// `CheckStun` → `UpdateFigures` → `BroadcastWhileInPlay(false)` → 双手 `UpdateCardEffects`。</para>
    ///
    /// <para>⇒ 可观察判别式 = **`EvtKind.Trigger(Reanimated)` 必须排在 `EvtKind.Deploy` 之前** ——
    /// 我们那条 `EvtKind.Deploy` 是在收尾（`ApplyDeployTurnState` 之后）才发的；
    /// 把广播挪回它后面（= 改前的写法）这条立刻红。</para>
    /// </summary>
    static void B94B14Case1ReanimatedBroadcastOrder()
    {
        var remCard = new CardDef("B14R1", "B14R1", "unit", "When reanimated, gain +1 Attack",
                                  null, "Test", 1, 1, 5, 0,
                                  abilityTriggers: new[] { 570 }, abilityTriggersVersioned: true);
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var u = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 1), remCard);
        var ops = EffectText.Parse("Reanimate a friendly Remnant", out _, out _, card: null);

        // 🧨 前提硬检查
        CheckTrue(u != null && u.Card == remCard, "🧨 前提：那张卡真的摆上了场");
        Check(remCard.AbilityTriggerPresence("Reanimated"), 1,
              "🧨 前提：`abilityTriggers` 含 570（`Reanimated`）—— 否则 `EmitListenerTrigger` 的闸不过");
        CheckTrue(remCard.WhenTriggers.Count > 0,
                  "🧨 前提：`When reanimated, …` 收成了事件层监听（`CardDef.WhenTriggers` 非空）");
        CheckTrue(ops != null && ops.Count > 0 && ops[0].Verb == "reanimate",
                  "🧨 前提：`Reanimate a friendly Remnant` 解析出一条 `reanimate` op");
        u.IsRemnant = true;
        CheckTrue(u.IsRemnant, "🧨 前提：夹具把这张标成了残骸（`DoReanimate` 候选 ① 的唯一来源）");

        ctx.Signals.Clear();
        int before = u.CurrentMeleeAttack;
        RuleCore.ResolveOps(ctx, 0, null, ops, "B94B14");
        RuleCore.PumpActionQueues(ctx);

        int iTrig = -1, iDep = -1;
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e.Kind == EvtKind.Trigger && e.Keyword == "Reanimated" && iTrig < 0) iTrig = i;
            if (e.Kind == EvtKind.Deploy && e.CardId == remCard.Name && iDep < 0) iDep = i;
        }
        CheckTrue(iTrig >= 0, "🧨 前提：真的发了 `EvtKind.Trigger(Reanimated)`（广播找得到监听者）");
        CheckTrue(iDep >= 0, "🧨 前提：真的发了 `EvtKind.Deploy`（表现层那条落地事件仍在发）");
        // 🧨 次序判别式：改前 `BroadcastWhen(Reanimated)` 排在 `Emit(Deploy)` **之后** ⇒ 这条红
        CheckTrue(iTrig < iDep,
                  "🧨 原版次序 = `ResetToValuesInHand` → **`BroadcastUnitReanimated`** → `SetAsUnitInPlay` → …（`…b__0.c:7-31`）"
                + "⇒ `Reanimated` 那一拍必须**先于**落地事件；把广播挪回收尾处这条立刻红");
        // 前提 ②：监听器真的结算（`When reanimated, gain +1 Attack`）—— 保住「广播不是空响」
        Check(u.CurrentMeleeAttack, before + 1,
              "🧨 残骸自己的 `When reanimated, …` 必须响一次（原版 `BroadcastUnitReanimated` 的棋盘那一跳含自己）");
    }

    /// <summary>
    /// **账 521 ② —— `ReactToUnitJammed` 的「自己」那一支要清 `CardInstance` 上那两本账**
    /// （`activeEffects` / `activeEnchantments`）。
    ///
    /// <para>**判据（🥇 `decomp_full/CardScript__ReactToUnitJammed.c:53-202`）**：自己被 jam ⇒
    /// 倒序 `RemoveEffect(fromJam:true)` → 摘全部 trait → **清三格 / 两本账** → 挂 jam →
    /// 非 Hero 压主动技能 → `CheckIfDead`。</para>
    ///
    /// <para>`UnitState.ApplyJam()` 够不到 `CardInstance.ActiveEffects/ActiveEnchantments`
    /// （那两本挂在**实例**上）⇒ 由 `EffectResolver.ResolveJamUnit` 补清（与 `ReanimateRemnantInPlace`
    /// 同一条分工）。删掉那一句 ⇒ 本用例两条结论断红。</para>
    /// </summary>
    static void B94B14Case2JamClearsInstanceBooks()
    {
        var enemyCard = new CardDef("B14J1", "B14J1", "unit", "", null, "Test", 1, 1, 5, 0);
        var ownCard = new CardDef("B14J0", "B14J0", "unit", "", null, "Test", 1, 1, 5, 0);
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var own = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 1), ownCard);
        var enemy = Place(ctx, 1, BoardSlots.SlotOf(BoardSlots.Right, 1), enemyCard);
        var ops = EffectText.Parse("Jam an enemy troop", out _, out _, card: null);

        // 🧨 前提硬检查
        CheckTrue(own != null && enemy != null, "🧨 前提：双方各摆了一张");
        CheckTrue(ops != null && ops.Count > 0 && ops[0].Verb == "jam",
                  "🧨 前提：`Jam an enemy troop` 解析出一条 `jam` op（解析层 `TryJam`）");
        // 前提 ②：先往那两本账里各放一条（⛔ 不放的话下面两条就是**空转绿**）
        var act = new ActiveEnchantment { Card = enemyCard, EnchantmentCardId = 1 };
        enemy.Instance.ActiveEnchantments.Add(act);
        enemy.Instance.ActiveEffects.Add(new ActiveCardEffect
        {
            EnchantmentCard = enemyCard, Enchantment = act, BuffType = 12, Source = "B94B14 夹具",
        });
        Check(enemy.Instance.ActiveEffects.Count, 1, "🧨 前提：夹具里 `ActiveEffects` 确实非空");
        Check(enemy.Instance.ActiveEnchantments.Count, 1, "🧨 前提：夹具里 `ActiveEnchantments` 确实非空");

        ctx.Signals.Clear();
        RuleCore.ResolveOps(ctx, 0, own, ops, "B94B14");
        RuleCore.PumpActionQueues(ctx);

        CheckTrue(enemy.Has("jam"), "🧨 前提：jam 真的挂上了（`ApplyJam` 跑到）");
        Check(enemy.Instance.ActiveEffects.Count, 0,
              "🧨 原版 `ReactToUnitJammed` 的「自己」支清 `activeEffects`；删掉 `ResolveJamUnit` 里那一句立刻红");
        Check(enemy.Instance.ActiveEnchantments.Count, 0,
              "🧨 同上，`activeEnchantments` 那本");
    }

    /// <summary>
    /// **账 506 ③ —— 「造出来的卡是不是破坏卡」按原版读【数据面 `spellType == 0xE6(230)`】**，
    /// 不按 subtype 词。
    ///
    /// <para>**判据（🥇 `decomp_full/BattleManager._ResolveCreateHandCard_d__512__MoveNext.c:90`）**：
    /// `if (*(int *)(rawCardData + 0x90) == 0xe6) { Signal.Raise(SabotageCreated) ;
    /// BroadcastSabotageCreated(bm, GetHero(那张的主人)) ; DisplayEnemyCardInCenter(…) }`。</para>
    ///
    /// <para>判别式：正例 = `subtype="Sabotage"` **且** `spellType=230`（计数 +1）；
    /// 负例 = 只带 `subtype="Sabotage"`、`spellType=0`（**按原版不该计数**）。
    /// 把判据改回 `CreatePool.MatchesKind(c,"sabotage")`（按词）⇒ 负例那条立刻红。</para>
    /// </summary>
    static void B94B14Case3SabotageBySpellType()
    {
        var realSab = new CardDef("B14S230", "B14S230", "tactic", "", null, "Test", 1, 0, 0, 0,
                                  subtype: "Sabotage", spellType: 230);
        var fakeSab = new CardDef("B14S0", "B14S0", "tactic", "", null, "Test", 1, 0, 0, 0,
                                  subtype: "Sabotage", spellType: 0);
        var ops230 = EffectText.Parse("Create a Sabotage in the enemy hand", out _, out _, card: null);
        var ops0 = EffectText.Parse("Create a Sabotage in the enemy hand", out _, out _, card: null);

        // 🧨 前提硬检查（两条都断，⛔ 不写 `if (前提)`）
        CheckTrue(ops230 != null && ops230.Count > 0 && ops230[0].Verb == "create",
                  "🧨 前提：`Create a Sabotage in the enemy hand` 解析出一条 `create` op");
        Check(realSab.SpellType, 230, "🧨 前提：正例夹具的 `spellType` = 230");
        Check(fakeSab.SpellType, 0, "🧨 前提：负例夹具的 `spellType` = 0（只带 subtype 词）");

        // ---- 正例：spellType==230 ⇒ 破坏计数 +1 ----
        var ctxA = BattlePool(new CardDef[0], new CardDef[0], new List<CardDef> { realSab });
        ctxA.Active = 0;
        int beforeA = ctxA.Tally.SabotageOf(0);
        RuleCore.ResolveOps(ctxA, 0, null, ops230, "B94B14");
        RuleCore.PumpActionQueues(ctxA);
        Check(ctxA.Tally.SabotageOf(0), beforeA + 1,
              "🧨 `spellType==230` 的破坏卡造进手牌 ⇒ `AddSabotage` 计数（原版 `_d__512:90` 那三条）");

        // ---- 负例：只带 subtype 词、spellType=0 ⇒ 一条都不该计 ----
        var ctxB = BattlePool(new CardDef[0], new CardDef[0], new List<CardDef> { fakeSab });
        ctxB.Active = 0;
        int beforeB = ctxB.Tally.SabotageOf(0);
        RuleCore.ResolveOps(ctxB, 0, null, ops0, "B94B14");
        RuleCore.PumpActionQueues(ctxB);
        Check(ctxB.Tally.SabotageOf(0), beforeB,
              "🧨 原版判据是 `spellType`（`_d__512:90`）—— 只带 `subtype=\"Sabotage\"` 不算；"
            + "改回按 subtype 词判（`CreatePool.MatchesKind`）时这条立刻红");
    }
}
