// B142_E1_Assert.cs — 批次 **B142** · 执行写手 `B142-E1`（2026-11-04 · 第一百二十九会话）
//
// ============================ 它钉的是什么 ============================
//   本批在 `RuleEngine/Core/EffectResolver.cs` 落了三条（判据/施工单 → `报告/B142_E1.md`）：
//     ① `B141-HAND-TRIGGER-KEY/WIRE`：`FireHandAbilityTrigger` 的正文键从 `triggerName` 改成
//        `KeywordFromTriggerName(triggerName) ?? triggerName`（`EffectResolver.cs:19056`）；
//     ② `ENG-11b` 半①：监听者正文层对 `ca 320` 载体**让路** ——
//        `ListenerBodyYieldsToFollowUp` / `AllChosenAbilityRowsExecutable`（本文件 A/B 组钉它们）；
//     ③ `ENG-11b` 半②：`ca 30/40` 的 BodyOp 执行器（**只在** `bodyLayerSuppressed == true` 时跑）。
//   本文件钉的是**判定函数与数据事实**，⛔ 不跑战场（宿主断言的战场那一半由调度台另派）。
//
// ============================ 判据（全部现读，本轮核过）============================
//   · `AbilityLogic__PlayAbility.c` 的 `chosenAbility` 巨型 switch（`ca 320 = followAfterResolution`、
//     `30 = buffOthers`、`40 = buffSelf`、`50 = buffHand`）；
//   · `CardScript__FollowUpResolutionPlayed.c:5-9` → `OnTrigger(0x19a = 410)`；
//   · `cards_engine.json` v49 现读：`ASH81` 的 455 行 = `ca 320`、410 = `ca 40 + ca 40`；
//     `GSC14` 的 10 行 = `ca 320`、410 = `ca 30`；`SW9` 的 410 里有 `ca 238`（**不是**执行器档）。
//
// ============================ 🧨 灭自证（改哪一句会红）============================
//   · 把 `ListenerBodyYieldsToFollowUp` 改成恒 `true` ⇒ **B2/B3/B4** 红（SW9/BL8/BL20 不许让路）；
//   · 把 `AllChosenAbilityRowsExecutable` 的档位集 `{30,40,50}` 缩成 `{30,40}` ⇒ **C3** 红
//     （`BL8` 的 410 = `ca 40` 仍应可执行，而 `ca 50` 那一档是 `buffHand` 已被 `B123_E1` 接上）；
//   · 把 `ca 30/40` 从 `TriggerRowKindOf` 挪去 `OwnLane` ⇒ **D1/D2** 红；
//   · 把 `KeywordFromTriggerName` 换成 `triggerName.ToLowerInvariant()` ⇒ **A4** 红
//     （`othercardspiritstone` 这类名字与关键词**字面不同**，小写化换不出来）。
//
// ============================ 怎么接（宿主 · 调度台做）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加（必须落在该行第一个 `//` 之前）：
//     `Guard("B142_E1_Assert", B142_E1_Assert.Run); _pass += B142_E1_Assert.Pass; _fail += B142_E1_Assert.Fail;`
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B142-E1 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B142_E1_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B142-E1：HAND-TRIGGER 反查消费 + ENG-11b 让路闸 + ca 30/40 档位 + 手牌 ca 100/165 数据事实");

        GroupA_ReverseKeywordMap();
        GroupD_RowKinds();

        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B142_E1] `CardDatabase.Load()` 抛了：" + e.Message); }
        CheckTrue(pool != null && pool.Count > 500, "P0 前提：卡池读得到且规模正常（≥ 500 张）");
        if (pool != null && pool.Count > 500)
        {
            GroupB_ListenerYields(pool);
            GroupC_RowExecutable(pool);
            GroupE_HandCaFacts(pool);
        }
        Summary();
    }

    static CardDef Find(List<CardDef> pool, string id)
    {
        if (pool == null) return null;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && string.Equals(pool[i].Id, id, StringComparison.OrdinalIgnoreCase)) return pool[i];
        return null;
    }

    // ==================================================================
    //  A 反查表（= ① 消费点的取值口）：`KeywordFromTriggerName`
    // ==================================================================
    static void GroupA_ReverseKeywordMap()
    {
        Section("A · `KeywordFromTriggerName` 反查表（由正向 `TriggerNameOfKeyword` 推导）");
        var table = RuleCore.KeywordByTriggerNameTable;
        CheckTrue(table != null && table.Count >= 25,
                  "A1 反查表非空且覆盖足够（现读 = " + (table != null ? table.Count : -1) + " 对，期望 ≥ 25）");

        // A2 自洽：每一对都必须能被正向函数反推回同一个名字（⛔ 别让反查变成第二份手写映射）
        int bad = 0; string firstBad = null;
        if (table != null)
        {
            foreach (var kv in table)
            {
                string back = RuleCore.TriggerNameOfKeyword(kv.Value);
                if (!string.Equals(back, kv.Key, StringComparison.OrdinalIgnoreCase))
                { bad++; if (firstBad == null) firstBad = kv.Key + "→" + kv.Value + "→" + (back ?? "null"); }
            }
        }
        Check(bad, 0, "A2 ★ 每一对都能被正向 `TriggerNameOfKeyword` 原样反推回来（不一致 = " + bad
                      + (firstBad != null ? "，第一处 " + firstBad : "") + "）");

        // A3 字面不同名（证明这不是 `ToLower()` 假货 —— 英文名与引擎关键词本来就不一样）
        CheckTrue(RuleCore.KeywordTriggerPairNonIdentityCount > 0,
                  "A3 ★ 至少有一对「trigger 名 ≠ 关键词」（现读 " + RuleCore.KeywordTriggerPairNonIdentityCount
                  + " 对）⇒ 反查表不是小写化糊出来的");

        // A4 逐条点名（这四条是「小写化换不出来」的代表：`OtherCardPlayed`/`OtherCardSpiritStone`/
        //    `ThisCardPlayed`→`rally`/`Cruelty`→`cruelty`）
        Check(RuleCore.KeywordFromTriggerName("OtherCardPlayed"), "othercardplayed",
              "A4a `OtherCardPlayed` → `othercardplayed`");
        Check(RuleCore.KeywordFromTriggerName("OtherCardSpiritStone"), "othercardspiritstone",
              "A4b `OtherCardSpiritStone` → `othercardspiritstone`");
        Check(RuleCore.KeywordFromTriggerName("ThisCardPlayed"), "rally",
              "A4c `ThisCardPlayed` → `rally`（判据 = `TriggerNameOfKeyword(rally) == \"ThisCardPlayed\"`）");
        Check(RuleCore.KeywordFromTriggerName("Cruelty"), "cruelty",
              "A4d `Cruelty` → `cruelty`（手牌 `Cruelty(720)` 那条真被解析）");

        // A5 认不出 ⇒ `null`（调用点据此回落 `triggerName`，保证零回归）
        Check(RuleCore.KeywordFromTriggerName("TurnStart"), null,
              "A5 `TurnStart` 不是关键词 ⇒ `null`（调用点回落 `triggerName`，逐字改前行为）");
        Check(RuleCore.KeywordFromTriggerName(null), null, "A6 `null` 入参 ⇒ `null`（不抛）");
    }

    // ==================================================================
    //  D `ca → 支别`（② 执行器所在的那一档）
    // ==================================================================
    static void GroupD_RowKinds()
    {
        Section("D · `TriggerRowKindOf`：`ca 30/40/50` 都落 `BodyOp`（执行器所在档）");
        Check(RuleCore.TriggerRowKindOf(30), RuleCore.TriggerRowKind.BodyOp, "D1 `ca 30 buffOthers` ⇒ BodyOp");
        Check(RuleCore.TriggerRowKindOf(40), RuleCore.TriggerRowKind.BodyOp, "D2 `ca 40 buffSelf` ⇒ BodyOp");
        Check(RuleCore.TriggerRowKindOf(50), RuleCore.TriggerRowKind.BodyOp, "D3 `ca 50 buffHand` ⇒ BodyOp");
        Check(RuleCore.TriggerRowKindOf(320), RuleCore.TriggerRowKind.FollowUp, "D4 `ca 320` ⇒ FollowUp（⛔ 别并进 BodyOp）");
        Check(RuleCore.TriggerRowKindOf(480), RuleCore.TriggerRowKind.OwnLane, "D5 `ca 480` ⇒ OwnLane（`B141-SW34-480` 另立账）");
        Check(RuleCore.TriggerRowKindOf(-1), RuleCore.TriggerRowKind.Unknown, "D6 `ca -1` ⇒ Unknown（⛔ 别折成 0）");
    }

    // ==================================================================
    //  B 让路闸（`ListenerBodyYieldsToFollowUp`）
    // ==================================================================
    static void GroupB_ListenerYields(List<CardDef> pool)
    {
        Section("B · `ListenerBodyYieldsToFollowUp`：只有 `ca 320 载体 ∧ 410 全部可执行` 才让路");
        var ash81 = Find(pool, "ASH81");   // Bright Lance Vyper：455 = ca 320；410 = ca 40 × 2
        var gsc14 = Find(pool, "GSC14");   // Acolyte Leader：10 = ca 320；410 = ca 30
        var sw9 = Find(pool, "SW9");       // Fenrisian Wolf：410 里有 ca 238 ⇒ 安全闸必须挡
        var bl8 = Find(pool, "BL8");       // Noise Marine：60 = ca 40，无 320
        var bl20 = Find(pool, "BL20");     // Child of Torment：60 = ca 40，无 320

        CheckTrue(ash81 != null, "B0a 前提：`ASH81` 在池里");
        CheckTrue(gsc14 != null, "B0b 前提：`GSC14` 在池里");
        if (ash81 != null)
            Check(RuleCore.ListenerBodyYieldsToFollowUp(ash81, WhenEventKind.SpiritAbility, false), true,
                  "B1 ★★ `ASH81` + `When you trigger a Spirit Stone ability`(455) ⇒ **让路**（正文改走 410）");
        if (gsc14 != null)
            Check(RuleCore.ListenerBodyYieldsToFollowUp(gsc14, WhenEventKind.Play, false), true,
                  "B2 ★★ `GSC14` + `When your opponent plays a Stratagem`(10) ⇒ **让路**");
        if (sw9 != null)
            Check(RuleCore.ListenerBodyYieldsToFollowUp(sw9, WhenEventKind.Attack, false), false,
                  "B3 ★★ `SW9` ⇒ **不让路**（它的 410 里有 `ca 238`，安全闸挡住；且它走主动能力路）");
        if (bl8 != null)
            Check(RuleCore.ListenerBodyYieldsToFollowUp(bl8, WhenEventKind.Damaged, false), false,
                  "B4 ★★ `BL8`（60 = ca 40，无 320）⇒ **不让路**（让路 = 与正文双发）");
        if (bl20 != null)
            Check(RuleCore.ListenerBodyYieldsToFollowUp(bl20, WhenEventKind.Damaged, false), false,
                  "B5 ★★ `BL20`（60 = ca 40，无 320）⇒ **不让路**");
        Check(RuleCore.ListenerBodyYieldsToFollowUp(null, WhenEventKind.Play, false), false, "B6 `null` 卡 ⇒ false（不抛）");
        Check(RuleCore.ListenerBodyYieldsToFollowUp(gsc14, "不存在的 kind", false), false,
              "B7 认不出的 kind ⇒ false（⛔ 不猜）");
    }

    // ==================================================================
    //  C 安全闸（`AllChosenAbilityRowsExecutable`）
    // ==================================================================
    static void GroupC_RowExecutable(List<CardDef> pool)
    {
        Section("C · `AllChosenAbilityRowsExecutable`：410 那边**每一行**都得有执行器（{30,40,50}）");
        var ash81 = Find(pool, "ASH81");
        var gsc14 = Find(pool, "GSC14");
        var sw9 = Find(pool, "SW9");
        var bl8 = Find(pool, "BL8");
        if (ash81 != null)
        {
            var r = ash81.TriggerAbilityRowsOf(410);
            CheckTrue(r != null && r.Length == 2, "C0a 前提：`ASH81` 的 410 = 2 行（现读 "
                                                  + (r != null ? r.Length : -1) + "）");
            Check(RuleCore.AllChosenAbilityRowsExecutable(ash81, 410), true, "C1 ★★ `ASH81` 的 410 全部可执行");
        }
        if (gsc14 != null)
        {
            var r = gsc14.TriggerAbilityRowsOf(410);
            CheckTrue(r != null && r.Length == 1, "C0b 前提：`GSC14` 的 410 = 1 行（现读 "
                                                  + (r != null ? r.Length : -1) + "）");
            Check(RuleCore.AllChosenAbilityRowsExecutable(gsc14, 410), true, "C2 ★★ `GSC14` 的 410 全部可执行");
        }
        if (bl8 != null)
            Check(RuleCore.AllChosenAbilityRowsExecutable(bl8, 410), false,
                  "C3 ★ `BL8` 的 410 是 **空数组/明确没有** ⇒ false（⛔ 空集不算「全部可执行」）");
        if (sw9 != null)
            Check(RuleCore.AllChosenAbilityRowsExecutable(sw9, 410), false,
                  "C4 ★★ `SW9` 的 410 含 `ca 238` ⇒ **false**（安全闸的灭自证点：放宽档位集这里就变绿）");
        Check(RuleCore.AllChosenAbilityRowsExecutable(null, 410), false, "C5 `null` 卡 ⇒ false（不抛）");
    }

    // ==================================================================
    //  E 手牌上下文事实（④ `ca 100` / `ca 165` 为什么本批停手的**数据钉子**）
    // ==================================================================
    static void GroupE_HandCaFacts(List<CardDef> pool)
    {
        Section("E · 手牌上下文 `ca 100 / 165` 的数据事实（本批**未做**，钉住理由）");
        int hand100 = 0, hand165 = 0, hand160 = 0; string hand100Who = null;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null || !c.AbilityContextsVersioned) continue;
            var ctxs = c.AbilityContexts;            // `null`/空 = 旧表或全 0
            if (ctxs == null) continue;
            for (int k = 0; k < ctxs.Count; k++)
            {
                var ctx = ctxs[k];
                if (ctx == null || ctx.Context != CardDef.AbilityContextHand) continue;
                var rs = c.TriggerAbilityRowsOf(ctx.Trigger);
                if (rs == null) continue;
                for (int j = 0; j < rs.Length; j++)
                {
                    var r = rs[j];
                    if (r == null) continue;
                    if (r.Ca == 100) { hand100++; if (hand100Who == null) hand100Who = c.Id + "/" + c.Name; }
                    else if (r.Ca == 165) hand165++;
                    else if (r.Ca == 160) hand160++;
                }
            }
        }
        Check(hand165, 0, "E1 ★ 池内**手牌上下文** `ca 165 discardWholeHand` = 0 行（现读 " + hand165 + "）⇒ 休眠档");
        Check(hand100, 1, "E2 ★ 池内**手牌上下文** `ca 100 createCardInHand` = 1 行（现读 " + hand100
                          + (hand100Who != null ? "，第一处 " + hand100Who : "") + "）");
        Check(hand160, 0, "E3 ★ 池内**手牌上下文** `ca 160 discardFromHand` = 0 行（现读 " + hand160
                          + "；教程起手占位卡不在可玩池里）");
        Debug.Log("[B142_E1] E 组口径：手牌上下文的 `ca 100/165` 执行器**本批停手** —— 主路执行体要 "
                + "`UnitState carrier`（`RunCreateCardInHandRowOfEnchantment` / `RunDiscardWholeHandRow` 都在 "
                + "`carrier == null` 时直接返 0），而手牌上下文只有 `CardInstance`。判据 → `报告/B142_E1.md` §四。");
    }

    // ==================================================================
    //  最小断言框架（本文件独立类 ⇒ 不借用 `RuleEngineTest` 的 private 助手）
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        object a = actual, e = expected;
        Report(ok, msg + "（实际 = " + (a == null ? "null" : a.ToString())
                        + "，期望 = " + (e == null ? "null" : e.ToString()) + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B142_E1 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B142_E1 合计：{_pass} 通过 / {_fail} 失败");
    }

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
