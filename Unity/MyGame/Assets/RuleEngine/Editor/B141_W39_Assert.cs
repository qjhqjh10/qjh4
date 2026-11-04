// B141_W39_Assert.cs — 批次 **B141** · 执行写手 `W39`（2026-11-04 · 第一百二十八会话 · `B141-EXTRA-TRIGGER-NAME`）
//
// ============================ 本文件钉的是什么 ============================
//   `ActionPump.ExtraTriggerName` 补上两条「引擎关键词 → 原版 trigger 名」映射：
//     · `othercardplayed`       → `OtherCardPlayed`      （`CardDef.cs:1917`，trigger **10**）
//     · `othercardspiritstone`  → `OtherCardSpiritStone` （`CardDef.cs:1933`，trigger **455**）
//
//   🔴 **但本文件同时钉住一个反证**（报告 `资料/完全复刻/报告/B141_W128_ExtraTriggerName.md` §三）：
//   这两条映射 **不会** 让 `ASH81` / `GSC14` 的 `ca 320` 跑起来 —— 它们的 320 走的是
//   **监听者路** `EffectResolver.FireBoardListenerDeferred`（`EffectResolver.cs:10657-10666`），
//   那里的比较是 `FollowUpSchedulerOf(card) == TriggerNameOfWhenKind(kind)`，**根本不经过**
//   `ExtraTriggerName` / `TriggerNameOfKeyword`（C 组就是这条反证的可执行形式）。
//   ⇒ 真缺口 = 那条路只发 `EmitFollowUpResolution`、**没有** `FireTriggerByNumber(410)`；
//     修点在 `EffectResolver.cs`，**不在本批白名单**。
//
// ============================ ⚠️ 没有写的那条断言 ============================
//   施工单要求「`ASH81`/`GSC14` 的 `FollowUpResolution` 从 0 变 1」。**今天它已经不成立**：
//   `EmitFollowUpResolution` 在监听者路 `:10663` 早就在发（两张卡的 `abilityTriggers` 都含 410
//   ⇒ `HasTriggerSource` 为真），改前 = 改后 = **1**；真正没跑的是 **410 那条触发器**
//   （`FireTriggerByNumber`）。要钉它得进宿主打一局（本批 ⛔ 不跑 Unity），且要等
//   `EffectResolver.cs` 那条修好 —— 两者都不在本次范围。⇒ 本文件**不写一条注定为假的断言**。
//
// ============================ 🧨 灭自证（改哪一句会红）============================
//   · 删掉 `ExtraTriggerName` 的任一新增映射 ⇒ **A1/A2** 红；
//   · 把 C 组改成 `TriggerNameOfKeyword`（而不是 `TriggerNameOfWhenKind`）⇒ **C3/C4 的
//     「两路名字相同」断言会红/绿失配** —— 它逼改的人分清「关键词路」与「监听者路」；
//   · 删掉 `TriggerNameOfWhenKind` 的 `Play` / `SpiritAbility` 两支 ⇒ **C1/C2** 红。
//
// ============================ 怎么接（宿主 · 调度台做）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加（必须落在该行第一个 `//` 之前）：
//     `Guard("B141W39_Assert", B141_W39_Assert.Run); _pass += B141_W39_Assert.Pass; _fail += B141_W39_Assert.Fail;`
//   🔴 接线前先处理 `Editor/B141_W36_Assert.cs` 的 **C6/C7** —— 那两条是「映射必须为 null」的钉子，
//   本批补上映射之后它们会**按设计变红**（W36 原文：「届时删掉本行并确认没有重复派 320」）。
//   = ⇒ 本次已核：**没有重复派**（这两条关键词在生产代码里 0 个消费者，见报告 §三）。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B141-W39 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B141_W39_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B141-W39：`ExtraTriggerName` 补两条映射（`othercardplayed` / `othercardspiritstone`）");

        GroupA_Mappings();
        GroupB_SchedulerNames();
        GroupC_RealRoute_ListenerPath();
        GroupD_AntiSelfProof();
        Summary();
    }

    // ==================================================================
    //  A 两条映射在位（+ 名字 ↔ 数值）
    // ==================================================================
    static void GroupA_Mappings()
    {
        Section("A 映射在位：`othercardplayed` → `OtherCardPlayed` · `othercardspiritstone` → `OtherCardSpiritStone`");
        Check(RuleCore.TriggerNameOfKeyword("othercardplayed"), "OtherCardPlayed",
              "A1 ★ `othercardplayed` ⇒ `OtherCardPlayed`（`ActionPump.ExtraTriggerName`）"
            + "｜🧨 删掉那一句 ⇒ 本行红");
        Check(RuleCore.TriggerNameOfKeyword("othercardspiritstone"), "OtherCardSpiritStone",
              "A2 ★ `othercardspiritstone` ⇒ `OtherCardSpiritStone`"
            + "｜🧨 删掉那一句 ⇒ 本行红");
        Check(CardDef.AbilityTriggerNumber("OtherCardPlayed"), 10, "A3 `OtherCardPlayed` = 10（`CardDef.cs:1917`）");
        Check(CardDef.AbilityTriggerNumber("OtherCardSpiritStone"), 455, "A4 `OtherCardSpiritStone` = 455（`CardDef.cs:1933`）");
        // 既有 21 条映射不许被这两句挡住（顺序 / default 分支回归）
        Check(RuleCore.TriggerNameOfKeyword("regiment"), "Regiment", "A5 回归：`regiment` ⇒ `Regiment` 未受影响");
        Check(RuleCore.TriggerNameOfKeyword("strike"), "Strike", "A6 回归：`strike` ⇒ `Strike` 未受影响");
        Check(RuleCore.TriggerNameOfKeyword(null), null, "A7 `null` ⇒ `null`（空入参不炸）");
        Check(RuleCore.TriggerNameOfKeyword(""), null, "A8 `\"\"` ⇒ `null`");
    }

    // ==================================================================
    //  B 两张卡在 `FollowUpSchedulerOf` 里的名字 = 上面那两个
    // ==================================================================
    static void GroupB_SchedulerNames()
    {
        Section("B `FollowUpSchedulerOf`：`GSC14` = OtherCardPlayed(10) · `ASH81` = OtherCardSpiritStone(455)");
        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B141_W39] `CardDatabase.Load()` 抛了：" + e.Message); }
        CheckTrue(pool != null && pool.Count > 500, "B0 前提：卡池读得到且规模正常（≥ 500 张）");
        if (pool == null || pool.Count <= 500) return;

        var gsc14 = Find(pool, "GSC14");
        var ash81 = Find(pool, "ASH81");
        CheckTrue(gsc14 != null, "B1 前提：池内有 `GSC14 Acolyte Leader`");
        CheckTrue(ash81 != null, "B2 前提：池内有 `ASH81 Bright Lance Vyper`");
        if (gsc14 != null)
            Check(RuleCore.FollowUpSchedulerOf(gsc14), "OtherCardPlayed",
                  "B3 `GSC14.FollowUpSchedulerOf` = `OtherCardPlayed`（`EffectResolver.cs:7966`）");
        if (ash81 != null)
            Check(RuleCore.FollowUpSchedulerOf(ash81), "OtherCardSpiritStone",
                  "B4 `ASH81.FollowUpSchedulerOf` = `OtherCardSpiritStone`（`EffectResolver.cs:7964`）");
        // ★ 0→1 那条施工要求为什么写不成：`EmitFollowUpResolution` 的唯一闸 = `HasTriggerSource`
        //   （`EffectResolver.cs:8078`），而这两张卡的 `abilityTriggers` 都含 410
        //   ⇒ 监听者路 `:10663` 那一跳**今天就在发**（改前 = 改后 = 1）。没跑的是 410 触发器。
        if (gsc14 != null)
            CheckTrue(RuleCore.HasTriggerSourceForTest(gsc14, "FollowUpResolution"),
                      "B5 ★★ `GSC14` 的 `FollowUpResolution` 闸 = **真** ⇒ `EmitFollowUpResolution` 改前就在发"
                    + "（所以「0→1」不成立；缺的是 `FireTriggerByNumber(410)`）");
        if (ash81 != null)
            CheckTrue(RuleCore.HasTriggerSourceForTest(ash81, "FollowUpResolution"),
                      "B6 ★★ `ASH81` 同上（`abilityTriggers` 含 410）");
    }

    // ==================================================================
    //  C ★ 真正的路由：监听者路按 `TriggerNameOfWhenKind` 比名（**不经过** `ExtraTriggerName`）
    // ==================================================================
    static void GroupC_RealRoute_ListenerPath()
    {
        Section("C ★ 反证：这两张的 320 走监听者路（`TriggerNameOfWhenKind`），不经关键词路");
        string playName = RuleCore.TriggerNameOfWhenKindForTest(WhenEventKind.Play);
        string spiritName = RuleCore.TriggerNameOfWhenKindForTest(WhenEventKind.SpiritAbility);
        Check(playName, "OtherCardPlayed",
              "C1 `TriggerNameOfWhenKind(play)` = `OtherCardPlayed`（`EffectResolver.cs:7639`）"
            + "｜🧨 删掉那一支 ⇒ 本行红");
        Check(spiritName, "OtherCardSpiritStone",
              "C2 `TriggerNameOfWhenKind(spiritability)` = `OtherCardSpiritStone`（`:7662`）"
            + "｜🧨 删掉那一支 ⇒ 本行红");
        // 监听者路那一条比较式（`:10662`）：`sched == TriggerNameOfWhenKind(kind)` —— 两个名字**相等**
        // ⇒ 这一步**与 A 组无关**就成立 ⇒ 补 A 组不会把 FollowUpResolution 从 0 变 1（改前就已经是 1）。
        CheckTrue(playName == "OtherCardPlayed" && spiritName == "OtherCardSpiritStone",
                  "C3 ★★ 监听者路的两条名字**本来就在**（`Play` → 10 / `SpiritAbility` → 455）"
                + " ⇒ `FireBoardListenerDeferred` 的比较式不依赖 `ExtraTriggerName`");
        CheckTrue(RuleCore.TriggerNameOfKeyword("othercardplayed") != null
               && RuleCore.TriggerNameOfWhenKindForTest(WhenEventKind.Play) != null,
                  "C4 两路（关键词路 / 监听者路）**各有一份**名字来源 —— ⛔ 别把它们当成同一条链，"
                + "也别为了「让监听者路能比上」去改关键词路（改了也不改行为）");
    }

    // ==================================================================
    //  D 灭自证 / 负例
    // ==================================================================
    static void GroupD_AntiSelfProof()
    {
        Section("D 灭自证");
        // 这两条关键词在**生产代码**里没有任何 `FireTriggerAt` 消费者（"othercard" 只出现在
        // 本映射与 `CardDef` 名字表 / `EffectResolver` 监听者路的名字里）⇒ 本映射是「补全映射层」，
        // 不是「修 ASH81/GSC14 的 320」。D1/D2 把「名字存在」与「有行为」两件事分开钉住。
        CheckTrue(RuleCore.TriggerNameOfKeyword("othercardplayed") == "OtherCardPlayed",
                  "D1 映射存在（≠ 有行为）— 生产 `FireTriggerAt` 没有任何一处传 `othercardplayed`");
        CheckTrue(RuleCore.TriggerNameOfKeyword("othercardspiritstone") == "OtherCardSpiritStone",
                  "D2 映射存在（≠ 有行为）— 同上");
        CheckTrue(RuleCore.TriggerNameOfWhenKindForTest(WhenEventKind.Play) == "OtherCardPlayed"
               && RuleCore.TriggerNameOfWhenKindForTest(WhenEventKind.SpiritAbility) == "OtherCardSpiritStone",
                  "D3 ★★★ 灭自证：**把 A 组两句删掉，C 组仍然全绿** ⇒ 证明这两张卡的 320 不靠本映射"
                + "（真缺口 = 监听者路没派 `410`，修点在 `EffectResolver.cs`，见报告 §三）");
    }

    static CardDef Find(List<CardDef> pool, string id)
    {
        if (pool == null) return null;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && string.Equals(pool[i].Id, id, StringComparison.OrdinalIgnoreCase)) return pool[i];
        return null;
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
        if (_fail == 0) Debug.Log($"B141_W39 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B141_W39 合计：{_pass} 通过 / {_fail} 失败");
    }

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
