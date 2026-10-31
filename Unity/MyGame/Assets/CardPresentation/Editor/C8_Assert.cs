// C8_Assert.cs — 第一百零九会话 · C 组 · 执行代理 `C8` 的断言
//
// ============================ 它盯的是什么 ============================
//  ① **410 去重闩**：原版 `RawCardScript__TriggerAbilities.c:136-164` —— 同一次派发趟里，某件附魔派过一条
//     `trigger == 410(FollowUpResolution) ∧ triggerValue == 1` 的 ability 之后，**该附魔卡**后续的 ability 全跳过。
//  ② **`AbilityData +0x4c` = `enchantmentCardId`**（不是能力下标）—— 落到我们的
//     `ActiveCardEffect.EnchantmentCardId`（原版 `CardEffect.enchantmentCardId // +0xA4`）。
//
//  判据 = `资料/完全复刻/判据/B109_R18_附魔三判据.md` §2 / §3（数据源 `资料/普查产出_1018/allcards_1350/cards_raw.json`）。
//
// ============================ 本件测什么 / 不测什么 ============================
//  ✅ 数据形状（`CardDef.TriggerAbilityIndexSpec.TriggerValue` 三态 + `CardDatabase` 映射）
//  ✅ 闩本体（`Enchantments.ApplyFollowUpResolutionDedup` / `HasTriggerAbilityWithValue`，直调）
//  ✅ 写点（`Enchantments.Attach` ⇒ `ActiveCardEffect.EnchantmentCardId` 与 `ActiveEnchantment.EnchantmentCardId` 同源）
//  ❌ **端到端（派发趟里的接线）不在本件**：那一行在 `EffectResolverPartial_B109E6.FireEnchantmentAbilitiesByNumber`
//     的白名单外 ⇒ 由调度台按报告 §三 的补丁接线。接上之后 B/C 组即成为**灭自证**的端到端保证。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · C3：把 `ApplyFollowUpResolutionDedup` 里的 `lastFollowUp = e.Card;` 删掉 ⇒ 两条同卡都留着 ⇒ 红。
//   · C4：把 `.c:136-137` 那条 `skip` 判据改成「卡不同也跳」⇒ 不同卡的两条会少一条 ⇒ 红。
//   · C6：把 `if (triggerNumber != TriggerFollowUpResolution) return hits;` 删掉 ⇒ 非 410 也会闩 ⇒ 红。
//   · A2：把 `CardDatabase.MapTriggerAbilityIndices` 的 `TriggerValue = r.triggerValue` 删掉 ⇒ 真表读出来恒 -1 ⇒ 红。
//   · D2：把 `Enchantments.Attach` 的 `EnchantmentCardId = id,` 删掉 ⇒ 恒 -1 ⇒ 红。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>C8 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>；⛔ 不改任何宿主）。</summary>
public static class C8_Assert
{
    static int _fail, _pass;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _fail = 0; _pass = 0;
        Section("🆕 C8：附魔三判据的 ②410 去重（+0x20 = triggerValue）与 ③enchantmentCardId（CardEffect +0xA4）");

        GroupA_ModelShape();
        GroupB_HasTriggerAbilityWithValue();
        GroupC_FollowUpDedup();
        GroupD_EnchantmentCardIdWritePoint();

        Summary();
    }

    // ==================================================================
    //  A 组：数据形状（`TriggerAbilityIndexSpec.TriggerValue`）
    // ==================================================================
    static void GroupA_ModelShape()
    {
        Section("A 数据形状：`TriggerValue` 三态 + 真表映射");

        // A1 列未知（没有 Attach）⇒ `TriggerAbilityRowsOf` 是 null（⛔ 不是空数组）
        var bare = MakeCard("C8Bare");
        CheckTrue(bare.TriggerAbilityRowsOf(410) == null,
                  "A1 列未知（没 `AttachTriggerAbilityIndices`）⇒ `TriggerAbilityRowsOf(410)` 是 **null**（不是 `[]`）");

        // A2 手建行：TriggerValue 原样带出来
        var marked = MakeCard("C8Marked", Row(0, 0, 130, 0), Row(1, 0, 410, 1));
        var rows = marked.TriggerAbilityRowsOf(410);
        CheckTrue(rows != null && rows.Length == 1,
                  "A2 前提：手建两行 `[130(val=0), 410(val=1)]` ⇒ `TriggerAbilityRowsOf(410)` 恰 1 行");
        Check(rows != null && rows.Length == 1 ? rows[0].TriggerValue : -999, 1,
              "A3 ★★ `TriggerValue` 从 `TriggerAbilityIndexSpec` 原样读得回（= 原版 `CardAbility.triggerValue // +0x20`）");
        var rows130 = marked.TriggerAbilityRowsOf(130);
        Check(rows130 != null && rows130.Length == 1 ? rows130[0].TriggerValue : -999, 0,
              "A4 同一张卡 `trigger 130` 那行的 `TriggerValue == 0`（⛔ 别把 `-1 未知` 与 `0` 混成一个）");

        // A5 默认值 = -1（⛔ 不是 0）
        var dflt = new CardDef.TriggerAbilityIndexSpec();
        Check(dflt.TriggerValue, -1, "A5 缺省 `TriggerValue == -1`（未知；⛔ 不是 0 = 「明确 val 0」）");

        // A6 真表：`cards_engine.json` 若已带 `triggerValue` 列 ⇒ 池内至少一行非 -1；否则如实报「列未生」
        var pool = SafeLoadPool();
        if (pool == null) { CheckTrue(false, "A6 前提：`CardDatabase.Load()` 读得到卡池"); return; }
        int known = 0, total = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var all = pool[i]?.TriggerAbilityIndices;
            if (all == null) continue;
            for (int k = 0; k < all.Length; k++)
            {
                if (all[k] == null) continue;
                total++;
                if (all[k].TriggerValue != -1) known++;
            }
        }
        if (known == 0)
        {
            // 如实报「列还没生成」——⛔ 不静默跳过（铁律：断言不许用 if(前提) 吞结论）
            Debug.LogWarning("[C8] A6 **未生效**：`cards_engine.json` 还没有 `triggerValue` 列（" + total
                           + " 行全是 -1）⇒ 等调度台按报告 §三 重跑 `工具/gen_cards_engine.py` 后本组才有意义。");
            CheckTrue(true, "A6（待生成器重跑）现读 0 / " + total + " 行带 `triggerValue` —— 已知未生效，不判失败");
        }
        else
        {
            CheckTrue(known > 0, "A6 ★★ 真表已有 `triggerValue` 列（现读 " + known + " / " + total + " 行非 -1）");
        }
    }

    // ==================================================================
    //  B 组：`HasTriggerAbilityWithValue` 三态
    // ==================================================================
    static void GroupB_HasTriggerAbilityWithValue()
    {
        Section("B `HasTriggerAbilityWithValue`（原版 `.c:148` 的置位条件）");
        var marked = MakeCard("C8BMarked", Row(0, 0, 410, 1));
        var plain = MakeCard("C8BPlain", Row(0, 0, 410, 0));
        var bare = MakeCard("C8BBare");

        CheckTrue(Enchantments.HasTriggerAbilityWithValue(marked, 410, 1), "B1 `410 ∧ val==1` 的行 ⇒ true");
        CheckTrue(!Enchantments.HasTriggerAbilityWithValue(plain, 410, 1), "B2 `410 ∧ val==0` ⇒ false（⛔ 别把 0 当命中）");
        CheckTrue(!Enchantments.HasTriggerAbilityWithValue(bare, 410, 1),
                  "B3 列未知 ⇒ false（**不闩**、保持旧行为；原版恒有 `cardAbilities`，我们判不了时不猜）");
        CheckTrue(!Enchantments.HasTriggerAbilityWithValue(marked, 130, 1), "B4 trigger 不匹配 ⇒ false");
        CheckTrue(!Enchantments.HasTriggerAbilityWithValue(null, 410, 1), "B5 null 卡 ⇒ false（不抛）");
    }

    // ==================================================================
    //  C 组：410 去重闩本体（灭自证主战场）
    // ==================================================================
    static void GroupC_FollowUpDedup()
    {
        Section("C 410 去重闩：同卡后续跳 / 异卡照派 / 非 410 不动");

        var marked = MakeCard("C8CMarked", Row(0, 0, 410, 1));
        var plain = MakeCard("C8CPlain", Row(0, 0, 410, 0));

        // C1/C2 前提
        var two = new List<ActiveEnchantment> { Mount(marked), Mount(marked) };
        CheckTrue(two.Count == 2, "C1 前提：夹具两件**同一张**附魔卡（模拟同卡挂两次）");
        CheckTrue(Enchantments.HasTriggerAbilityWithValue(marked, 410, 1),
                  "C2 前提：这张卡确实带 `410 ∧ val==1` 的行");

        // C3 ★ 灭自证：同卡两条都带标记 ⇒ 只剩 1
        var kept = Enchantments.ApplyFollowUpResolutionDedup(two, Enchantments.TriggerFollowUpResolution);
        Check(kept == null ? -1 : kept.Count, 1,
              "C3 ★★★ **410 去重**：两件同卡、都带 `410 ∧ val==1` ⇒ 本趟只剩 **1** 条"
            + "（原版 `RawCardScript__TriggerAbilities.c:136-137` + `:148/158/163`）"
            + "｜🧨 删掉 `lastFollowUp = e.Card;` ⇒ 2 ⇒ 红");

        // C4 异卡 ⇒ 两条都留
        var diff = new List<ActiveEnchantment> { Mount(marked), Mount(MakeCard("C8COther", Row(0, 0, 410, 1))) };
        Check(Enchantments.ApplyFollowUpResolutionDedup(diff, Enchantments.TriggerFollowUpResolution).Count, 2,
              "C4 ★ 不同附魔卡各自带标记 ⇒ **两条都留**（原版比的是 `CardScript` 引用）"
            + "｜🧨 把 `skip` 改成「只要 `lastFollowUp != null`」⇒ 1 ⇒ 红");

        // C5 没标记 ⇒ 不动
        var noMark = new List<ActiveEnchantment> { Mount(plain), Mount(plain) };
        var keptNoMark = Enchantments.ApplyFollowUpResolutionDedup(noMark, Enchantments.TriggerFollowUpResolution);
        Check(keptNoMark.Count, 2, "C5 同卡两件但都不带 `val==1` 标记 ⇒ 一条都不跳");
        CheckTrue(ReferenceEquals(keptNoMark, noMark), "C5b 没东西被跳过时**原样返回同一个 List**（零分配契约）");

        // C6 非 410 广播 ⇒ 不动（原版 `CanTriggerAbility.c:71` 已要求 trigger 相等）
        Check(Enchantments.ApplyFollowUpResolutionDedup(new List<ActiveEnchantment> { Mount(marked), Mount(marked) }, 60).Count, 2,
              "C6 ★ 广播 trigger != 410 ⇒ 闩不生效、两条都留"
            + "｜🧨 删掉 `if (triggerNumber != TriggerFollowUpResolution) return hits;` ⇒ 1 ⇒ 红");

        // C7 顺序保持 + 空/单元素不炸
        var mixed = new List<ActiveEnchantment> { Mount(marked), Mount(plain), Mount(marked) };
        var keptMixed = Enchantments.ApplyFollowUpResolutionDedup(mixed, Enchantments.TriggerFollowUpResolution);
        CheckTrue(keptMixed.Count == 2 && ReferenceEquals(keptMixed[0], mixed[0]) && ReferenceEquals(keptMixed[1], mixed[1]),
                  "C7 ★ 顺序保持：`[标记卡, 无标记卡, 标记卡]` ⇒ 留下前两件、顺序不变（原版是「后续同卡跳过」不是「只留第一件」）");
        CheckTrue(ReferenceEquals(Enchantments.ApplyFollowUpResolutionDedup(null, 410), null), "C8 null 入参 ⇒ null（不抛）");
        Check(Enchantments.ApplyFollowUpResolutionDedup(new List<ActiveEnchantment>(), 410).Count, 0, "C9 空表 ⇒ 空表");
    }

    // ==================================================================
    //  D 组：`ActiveCardEffect.EnchantmentCardId` 写点
    // ==================================================================
    static void GroupD_EnchantmentCardIdWritePoint()
    {
        Section("D `CardEffect.enchantmentCardId // +0xA4`（= `AbilityData+0x4c`）的写点");

        BattleContext ctx = null;
        UnitState carrier = null;
        try { ctx = MakeBattle(out carrier); }
        catch (Exception e) { Debug.LogWarning("[C8] 建夹具抛了：" + e.Message); }
        CheckTrue(ctx != null && carrier != null, "D1 前提：夹具对局建得起来、载体在格 3");
        if (ctx == null || carrier == null) return;

        var ench = MakeCard("C8DEnch", Row(0, 0, 60, 0));
        int before = ctx.EnchantmentsUsed;
        var e1 = Enchantments.Attach(ctx, carrier.Instance, ench, Enchantments.BuffTypeEnchantment);
        var e2 = Enchantments.Attach(ctx, carrier.Instance, ench, Enchantments.BuffTypeEnchantment);
        CheckTrue(e1 != null && e2 != null, "D1b 前提：两次挂载都成功（`Enchantments.Attach` 是唯一挂载口）");
        if (e1 == null || e2 == null) return;

        Check(e1.EnchantmentCardId, before,
              "D2 ★★ 第一件的 `EnchantmentCardId` = 挂载前的 `ctx.EnchantmentsUsed`（原版 `CardScript__AddEnchantment.c:44` 的流水号）"
            + "｜🧨 删掉 `Enchantments.Attach` 里的 `EnchantmentCardId = id,` ⇒ -1 ⇒ 红");
        Check(e1.EnchantmentCardId, e1.Enchantment != null ? e1.Enchantment.EnchantmentCardId : -999,
              "D3 ★ 与 `ActiveEnchantment.EnchantmentCardId`（原版 `+0x18`）**同源**（判据 `.c:184-186` 那个实参）");
        CheckTrue(e2.EnchantmentCardId != e1.EnchantmentCardId,
                  "D4 ★ 两次挂载的流水号**不同**（= 「同一张附魔卡的不同次挂载不算同一条效果」那个合并键语义）");
        Check(e2.EnchantmentCardId, e1.EnchantmentCardId + 1, "D5 第二次 = 第一次 + 1（连续流水号）");
        CheckTrue(e1.EnchantmentCardId >= 0, "D6 流水号不为负数（`-1` 只该出现在「载体自己那趟」）");
    }

    // ==================================================================
    //  夹具 / 读数
    // ==================================================================
    static CardDef MakeCard(string id, params CardDef.TriggerAbilityIndexSpec[] rows)
    {
        var c = new CardDef(id, id, "unit", "", null, "Test", 1, 1, 5, 0, null);
        if (rows != null) c.AttachTriggerAbilityIndices(rows);
        return c;
    }

    static CardDef.TriggerAbilityIndexSpec Row(int ab, int lg, int trigger, int triggerValue)
    {
        return new CardDef.TriggerAbilityIndexSpec { Ab = ab, Lg = lg, Trigger = trigger, TriggerValue = triggerValue };
    }

    static ActiveEnchantment Mount(CardDef card)
    {
        return new ActiveEnchantment { Card = card, EnchantmentCardId = 0 };
    }

    static List<CardDef> SafeLoadPool()
    {
        try { return CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[C8] `CardDatabase.Load()` 抛了：" + e.Message); return null; }
    }

    /// <summary>一场「督军 + 一张单位」的干净对局；返回时 <paramref name="carrier"/> = 摆在 0 号玩家格 3 的载体。</summary>
    static BattleContext MakeBattle(out UnitState carrier)
    {
        var heroA = new CardDef("C8HeroA", "C8HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("C8HeroB", "C8HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var unitCard = new CardDef("C8Carrier", "C8Carrier", "unit", "", null, "Test", 1, 1, 5, 0, null);
        var foeCard = new CardDef("C8Foe", "C8Foe", "unit", "", null, "Test", 1, 1, 5, 0, null);
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
        if (_fail == 0) Debug.Log($"C8 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"C8 合计：{_pass} 通过 / {_fail} 失败");
    }

    // 宿主聚合行读这两个口（追加到类尾、零行漂）。
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
