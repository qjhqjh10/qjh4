// B109_E6_EnchantDispatch_Assert.cs — 第一百零九会话 · 波 1 · 执行代理 `B109_E6` 的断言
//
// ============================ 它盯的是什么 ============================
// `RuleEngine/Core/EffectResolver.cs` 的 `FireTriggerByNumberWithSink`（`:20995`）—— 引擎里**按 trigger 数值**派发
// 的那一个口（伤害广播 60/185/187、造牌 210、后续结算 410 都走它）。
// 改前它**只跑载体自己那张卡**的 `TriggerOpsByNumber`、**整条不查 `ActiveEnchantments`** ⇒
// 挂在单位身上的附魔卡（`Pray Enchantment` 的 60 / 31）**永远不会被派发** ⇒ `Pray_Exit` 那一拍静默不播。
//
// ============================ 原版口径（判据，B109_R2 已核清）============================
//   · `decomp_full/RawCardScript__TriggerAbilities.c` 的**内联附魔趟** = `:49-197`（不是旧施工单说的 `:47-108`）：
//       `:50-53` 空表闸 · `:66` `IsSpentEnchantment` 整件跳 · `:108-109` `CanTriggerAbility`
//       （第一句 `CardAbility__CanTriggerAbility.c:71`：`ability.trigger(+0x10) != 传入值 ⇒ return 0`，
//        **判的是数据位、不是卡面正文**）· `:115-116` `ReachedQuestCounter` · `:184-186` 派发。
//   · 派发实参：**载体 = thisCard**（`AbilityData+0x10`）、**能力属于附魔卡**（`+0x28`）、能力下标（`+0x4c`）；
//       三道 criteria（`CanTriggerAbility.c:131-138`）读**附魔卡的行**、被筛对象是**载体**。
//   · ⚠️ **「数据位 ability 只发信号、不跑正文」这条分支原版【不存在】** —— 过闸就照跑正文。
//       我们的数据行（`chosenAbility 340`）执行器不在这一口，其**效果**由手写等价物承担
//       （`ApplyDamageCore:9067` / `GrantPrayEnchantmentFaith:1636`）⇒ 本口只发信号 + 跑文本 op。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Run()` 是唯一入口；宿主接一个 `{ B109_E6_EnchantDispatch_Assert.Run(); }` 即可（本文件**不改**任何宿主）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · B/E：把新口里的 `EmitCardTrigger` 那一行删掉 ⇒ 「挂附魔 = 1 条、不挂 = 0 条、差恰为 1」全红。
//   · B/F：把 `EffectResolver.cs:21006` 的 `return _enchFired > 0;` 改回 `return false;` ⇒ F 组红。
//   · C：把 `Enchantments.AllSpent` 那一行删掉 ⇒ 「已花掉的不派」红。
//   · D：把 `EnchantmentHasAbilityForTrigger` 改成恒 `true` ⇒ 「trigger 不匹配不派」红。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B109_E6 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>；⛔ 不改任何宿主）。</summary>
public static class B109_E6_EnchantDispatch_Assert
{
    static int _fail, _pass;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _fail = 0; _pass = 0;
        Section("🆕 B109_E6：附魔派发侧（数值路 `FireTriggerByNumber` 的附魔趟）");

        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B109_E6] `CardDatabase.Load()` 抛了：" + e.Message); }
        CheckTrue(pool != null && pool.Count > 500, "A0 前提：卡池读得到且规模正常（≥ 500 张）");
        if (pool == null || pool.Count <= 500) { Summary(); return; }

        CardDef pray = null;
        for (int i = 0; i < pool.Count && pray == null; i++)
        {
            var c = pool[i];
            if (c != null && c.Id == Enchantments.PrayEnchantmentCardId) pray = c;
        }
        CheckTrue(pray != null, "A0b 前提：池里有 `Pray Enchantment`（`id " + Enchantments.PrayEnchantmentCardId + "`）");
        if (pray == null) { Summary(); return; }

        // ---- A 组：数据形状（= 为什么改前那条路恒不发）--------------------------------------
        Section("A 数据形状：数据位有 60、正文一个都没有");
        CheckTrue(pray.IsEnchantment, "A1 前提：它真的是附魔卡（`IsEnchantment`）");
        CheckTrue(pray.TriggerAbilityRowsOf(60) != null && pray.TriggerAbilityRowsOf(60).Length > 0,
                  "A2 前提：`triggerAbilityIndices` 里有 `trigger 60` 的行（= 原版 `cardAbilities[].trigger`）");
        CheckTrue(pray.TriggerOpsByNumber(60) == null,
                  "A3 **缺口本体**：正文 `TriggerOpsByNumber(60)` 是 null（数据行能力没有文本 op）");
        Check(pray.AbilityTriggerPresence("UnitDamaged"), 1, "A4 前提：`AbilityTriggerPresence(\"UnitDamaged\") == 1`");

        // ---- B 组：挂上附魔 ⇒ 受伤那一刻恰好多 1 条 `EvtKind.Trigger`（本次修的那一格）---------
        Section("B 挂附魔 + 按数值派 60 ⇒ 恰好多 1 条 `EvtKind.Trigger(CardId = 附魔卡名)`");
        int withEnch;
        bool firedWith;
        {
            var ctx = MakeBattle(out var carrier);
            CheckTrue(carrier != null, "B1 前提：载体上场了（格 3）");
            if (carrier == null) { Summary(); return; }
            var eff = Enchantments.Attach(ctx, carrier.Instance, pray, Enchantments.BuffTypeSingleUse);
            CheckTrue(eff != null, "B2 前提：附魔挂上了（`Enchantments.Attach` 是唯一挂载口）");

            ctx.ClearSignals();
            firedWith = RuleCore.FireTriggerByNumberForTest(ctx, carrier, 60, 0, 3, null, null);
            withEnch = CountTriggers(ctx, "UnitDamaged", pray.Name);
            Check(withEnch, 1,
                  "B3 ★★★ **本次修的那一格**：按数值派 `trigger 60` ⇒ 附魔那件恰好多 **1** 条 "
                + "`EvtKind.Trigger(keyword=\"UnitDamaged\", CardId=\"Pray Enchantment\")`"
                + "｜🧨 删掉 `FireEnchantmentAbilitiesByNumber` 里的 `EmitCardTrigger` ⇒ 0 ⇒ 红");
            var evt = FirstTrigger(ctx, "UnitDamaged");
            CheckTrue(evt != null && evt.CardId == pray.Name,
                      "B4 ★★ 信号的 `CardId` = **附魔卡名**（原版 `AbilityLogic.PlayAnim` 的 anim 列表属于附魔卡那条 ability；"
                    + "`BattleEvent.CardId` 的约定是卡名）｜🧨 改回 `u.Name`（载体名）⇒ 红");
            CheckTrue(evt != null && evt.Slot == 3 && evt.Player == 0,
                      "B5 ★ 信号的锚点 = **载体**（`Player/Slot`；原版 `AbilityData+0x10` = thisCard = 载体）");
        }

        // ---- C 组：已花掉的附魔整件跳过（原版 `:66`）------------------------------------------
        Section("C 已花掉（`IsSpentEnchantment`）⇒ 不派");
        {
            var ctx = MakeBattle(out var carrier);
            CheckTrue(carrier != null, "C1 前提：载体上场了（格 3）");
            if (carrier != null)
            {
                Enchantments.Attach(ctx, carrier.Instance, pray, Enchantments.BuffTypeSingleUse);
                bool spent = Enchantments.Spend(ctx, carrier.Instance, pray);
                CheckTrue(spent, "C2 前提：附魔真的被花掉了（`Spend` 返回 true）");
                CheckTrue(Enchantments.AllSpent(carrier.Instance, pray), "C3 前提：`AllSpent` 为真");
                ctx.ClearSignals();
                bool fired = RuleCore.FireTriggerByNumberForTest(ctx, carrier, 60, 0, 3, null, null);
                Check(CountTriggers(ctx, "UnitDamaged", pray.Name), 0,
                      "C4 ★★ 已花掉的附魔**整件跳过**（原版 `RawCardScript__TriggerAbilities.c:66`）｜🧨 删掉 `AllSpent` 那一行 ⇒ 1 ⇒ 红");
                CheckTrue(!fired, "C5 而且返回值仍是 false（没有任何东西被派发）");
            }
        }

        // ---- D 组：trigger 不匹配 ⇒ 不派（原版 `CanTriggerAbility.c:71`）------------------------
        Section("D trigger 不匹配（0 ThisCardPlayed）⇒ 不派");
        {
            var ctx = MakeBattle(out var carrier);
            CheckTrue(carrier != null, "D1 前提：载体上场了（格 3）");
            if (carrier != null)
            {
                Enchantments.Attach(ctx, carrier.Instance, pray, Enchantments.BuffTypeSingleUse);
                ctx.ClearSignals();
                bool fired = RuleCore.FireTriggerByNumberForTest(ctx, carrier, 0, 0, 3, null, null);
                Check(CountTriggers(ctx, "ThisCardPlayed", pray.Name), 0,
                      "D2 ★★ 附魔卡在 `trigger 0` 上**明确没有** ability（`triggerAbilityIndices` 无该行）⇒ 一条都不发"
                    + "｜🧨 把 `EnchantmentHasAbilityForTrigger` 改成恒 true ⇒ 1 ⇒ 红");
                CheckTrue(!fired, "D3 并且返回值 false");
            }
        }

        // ---- E 组：灭自证 —— 「挂」与「不挂」两个**同构**夹具，差恰为 1 --------------------------
        Section("E 灭自证（结构上不可能两边一起改回去）：挂附魔 1 条 / 不挂 0 条 / 差恰为 1");
        {
            var ctx = MakeBattle(out var bare);
            CheckTrue(bare != null, "E1 前提：对照载体上场了（格 3）");
            if (bare != null)
            {
                ctx.ClearSignals();
                bool firedBare = RuleCore.FireTriggerByNumberForTest(ctx, bare, 60, 0, 3, null, null);
                int without = CountTriggers(ctx, "UnitDamaged", pray.Name);
                Check(without, 0, "E2 对照：**没有**附魔的载体 ⇒ 0 条（把「多出来的那一条」归因到附魔本身）");
                CheckTrue(!firedBare, "E3 对照：返回值 false");
                Check(withEnch - without, 1,
                      "E4 ★★★ 灭自证：`挂附魔的条数 − 不挂的条数 == 1`（B 组 " + withEnch + " − E 组 " + without + "）"
                    + "｜🧨 把两条路径一起改成 0（或一起改成 1）⇒ 差不再是 1 ⇒ 红");
            }
        }

        // ---- F 组：返回值 —— 载体自己没有正文、只有附魔时也要返回 true ---------------------------
        Section("F 返回值：载体无文本 op、只有附魔派发成功 ⇒ `FireTriggerByNumber` 返回 true");
        CheckTrue(firedWith,
                  "F1 ★★ `FireTriggerByNumber(60)` 在「载体 ops == null、附魔派发成功」时返回 **true**"
                + "｜🧨 把 `EffectResolver.cs:21006` 的 `return _enchFired > 0;` 改回 `return false;` ⇒ 红");

        Summary();
    }

    // ==================================================================
    //  夹具 / 读数
    // ==================================================================

    /// <summary>一场「督军 + 一张单位」的干净对局；返回时 <paramref name="carrier"/> = 已经摆到 0 号玩家格 3 的载体。</summary>
    static BattleContext MakeBattle(out UnitState carrier)
    {
        var heroA = new CardDef("B109E6HeroA", "B109E6HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B109E6HeroB", "B109E6HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var unitCard = new CardDef("B109E6Carrier", "B109E6Carrier", "unit", "", null, "Test", 1, 1, 5, 0, null);
        var foeCard = new CardDef("B109E6Foe", "B109E6Foe", "unit", "", null, "Test", 1, 1, 5, 0, null);
        var ctx = RuleCore.NewBattle(new List<CardDef> { heroA, unitCard },
                                     new List<CardDef> { heroB, foeCard },
                                     seed: 0, shuffle: false, cardPool: new List<CardDef>());
        carrier = PlaceUnit(ctx, 0, 3, unitCard);
        PlaceUnit(ctx, 1, 3, foeCard);
        return ctx;
    }

    static UnitState PlaceUnit(BattleContext ctx, int p, int slot, CardDef card)
    {
        if (ctx == null || card == null) return null;
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static int CountTriggers(BattleContext ctx, string keyword, string cardId)
    {
        int n = 0;
        if (ctx == null || ctx.Signals == null) return 0;
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e == null || e.Kind != EvtKind.Trigger) continue;
            if (!string.Equals(e.Keyword, keyword, StringComparison.Ordinal)) continue;
            if (cardId != null && !string.Equals(e.CardId, cardId, StringComparison.Ordinal)) continue;
            n++;
        }
        return n;
    }

    static BattleEvent FirstTrigger(BattleContext ctx, string keyword)
    {
        if (ctx == null || ctx.Signals == null) return null;
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e != null && e.Kind == EvtKind.Trigger && string.Equals(e.Keyword, keyword, StringComparison.Ordinal)) return e;
        }
        return null;
    }

    // ==================================================================
    //  最小断言框架（本文件独立类 ⇒ 不借用 `RuleEngineTest` 的 private 助手）
    // ==================================================================

    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B109_E6 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B109_E6 合计：{_pass} 通过 / {_fail} 失败");
    }
    // 🆕 2026-10-31（B109 · 调度台接线）：宿主聚合行读这两个口（追加到类尾、零行漂）。
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
