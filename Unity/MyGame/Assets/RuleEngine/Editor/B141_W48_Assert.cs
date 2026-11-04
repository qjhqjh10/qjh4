// B141_W48_Assert.cs — 批次 **B141** · 执行写手 `W48`（2026-11-04 · 第一百二十八会话 · `B141-ENG11B-LISTENER410`）
//
// ============================ 它钉的是什么（⛔ 不是「已修」的证明）============================
//   施工单要求：在 `EffectResolver.FireBoardListenerDeferred`（`EffectResolver.cs:10657-10666`）
//   `EmitFollowUpResolution` 之后补 `FireTriggerByNumber(ctx, u, 410, …)`。
//   **本次没有落那一行** —— 回源核完发现它**今天跑不出任何效果**，且会给日后留一个双发地雷：
//
//     · `FireTriggerByNumberWithSink`（`EffectResolver.cs:21078`）先取
//       `u.Card.TriggerOpsByNumber(410)`；两张卡上这一格**恒 null**
//       （`triggerops` 探针：`HasTriggerOpsByNumber = False` ⇒ 退回「卡面关键词 == 触发词名」那一格，
//        `TriggerOps("followupresolution")` 也是 null）。
//     · ⇒ 落进 `FireTriggerRowsByChosenAbility`（`:21385`）按 `ca` 分派；而
//       `ASH81` 的 410 行是 `ca 40`、`GSC14` 的 410 行是 `ca 30` ⇒ `TriggerRowKindOf` 返 **BodyOp**
//       （`:21373`）⇒ `default` 分支里**只有 `ca 50` 有执行器**，其余一律走
//       `NoteTriggerRowKindGap`（`:21405`）＝ **只喊一句 Debug.LogWarning、不结算**。
//     · 也就是说：接上之后 `FireTriggerByNumber(410)` 会 **返回 false、一行效果都不执行**，
//       只多一条「这一版没有它的非正文执行器」的警告。
//     · 而这两张卡的**效果今天已经在跑** —— 跑在监听者的**正文路**：
//       `WhenTrigger.Ops`（`CardDef.CollectWhenTriggers` → `TryParseWhenSentence`）。
//       实测两段正文都解析得出真 op（`seg` 探针）：
//         `give +1 Attack and +1 Health to your troops` ⇒ `verb=give`
//         `gain Sniper and +1 Ranged`                     ⇒ `verb=gain`
//       ⇒ 再给 410 补一个「按 `ca` 执行」的实现 = **同一效果打两遍**（监听者路没有 `AbilityLayerOwnsRow`
//         那种「让路闸」；那张表只在战术/防御路 `:2917` 用过）。
//
//   ⇒ 本文件钉的是**上面这条判断链的每一格**（都是可读的事实），让下一个人不必重查一遍；
//     ⛔ 它不是「410 已派」的证明，也⛔ 不能拿来核销 ENG-11b。
//
// ============================ 判据（全部本轮现读）============================
//   · `CardScript__FollowUpResolutionPlayed.c:5-9`：
//     `RawCardScript__OnTrigger(card+0x28, 0x19a = 410, manager, thisCard = card,
//      actingCard = card, targetCard = param_2, 0)` ⇒ 410 的两槽 = **本卡 / 入参目标**。
//   · `AbilityLogic__PlayAbility.c:2474` → `BattleManager__AddFollowAfterResolution(manager,
//      abilityData+0x10 /* thisCard */, abilityData+0x20 /* targetCard */, priority, false)`
//     （`AbilityData__SetNewData.c` 的偏移表：`+0x10 = thisCard` · `+0x18 = actingCard` · `+0x20 = targetCard`）
//     ⇒ 送进 `FollowUpResolutionPlayed` 的第 1 实参 = **监听者本卡**，第 2 实参 = **监听者这条 ability 的 targetCard**。
//   · `CardScript__TriggerOtherCardSpiritStone.c:21`（455）：`OnTrigger(455, …, this, **default, default**)`
//     ⇒ `ASH81` 的 targetCard **恒 null**；`RawCardScript__OnCardPlayedWithTarget.c:96`（10）传
//     `param_4 = 被打出的那张` / `param_5 = 那张牌的目标` ⇒ `GSC14` 的 targetCard = 被打出那张牌的目标
//     （我们的 `BroadcastWhen(Play)` 两处调用点都传 `target = null` ⇒ 这一格我们拿不到，见报告 §四）。
//
// ============================ 🧨 灭自证（改哪一句会红）============================
//   · 给 `FireTriggerRowsByChosenAbility` 的 `default` 分支补一个「BodyOp 执行器」⇒ **B 组红**
//     （红得有道理：那时 410 会真跑效果，而监听者正文路**仍在跑同一条效果** ⇒ 必须同时补让路闸，
//      否则 `GSC14` 的 `+1/+1` 会打两遍）。
//   · 把 `TriggerRowKindOf(320)` 改成非 `FollowUp` ⇒ A3 红（320 是既有执行器，别动）。
//   · 把 `TriggerOpsByNumber` 的退回档改成「返回空表 = 有正文」⇒ C 组红。
//
// ============================ 怎么接（宿主 · 调度台做）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加（必须落在该行第一个 `//` 之前）：
//     `Guard("B141W48_Assert", B141_W48_Assert.Run); _pass += B141_W48_Assert.Pass; _fail += B141_W48_Assert.Fail;`
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B141-W48 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B141_W48_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B141-W48：监听者路的 410 —— **接线是空操作**（⛔ 不是「已修」）");

        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B141_W48] `CardDatabase.Load()` 抛了：" + e.Message); }
        CheckTrue(pool != null && pool.Count > 500, "A0 前提：卡池读得到且规模正常（≥ 500 张）");
        if (pool == null || pool.Count <= 500) { Summary(); return; }

        GroupA_Rows(pool);
        GroupB_RowKind();
        GroupC_NoTriggerOps(pool);
        GroupD_AbilityLayerNotOwner(pool);
        GroupE_SchedulerNames(pool);
        Summary();
    }

    // ==================================================================
    //  A 两张卡的行形状（监听者行 = 320；410 行 = 真效果种类）
    // ==================================================================
    static void GroupA_Rows(List<CardDef> pool)
    {
        Section("A 行形状 —— `ASH81`/455+410 · `GSC14`/10+410");
        CheckRow(pool, "ASH81", 455, 320, "A1 `ASH81` 的**监听者**行（455）是 `ca 320`");
        CheckRow(pool, "ASH81", 410, 40, "A2 `ASH81` 的 **410** 行是 `ca 40`（ab1/lg0、lg1 各一行）");
        CheckRow(pool, "GSC14", 10, 320, "A3 `GSC14` 的**监听者**行（10）是 `ca 320`");
        CheckRow(pool, "GSC14", 410, 30, "A4 `GSC14` 的 **410** 行是 `ca 30`（ab1/lg0）");
    }

    static void CheckRow(List<CardDef> pool, string id, int trigger, int ca, string msg)
    {
        var c = Find(pool, id);
        CheckTrue(c != null, "A· 前提：池内有 " + id);
        if (c == null) return;
        var rows = c.TriggerAbilityRowsOf(trigger);
        bool hit = false;
        if (rows != null)
            for (int i = 0; i < rows.Length; i++)
                if (rows[i] != null && rows[i].Ca == ca) { hit = true; break; }
        CheckTrue(hit, msg);
    }

    // ==================================================================
    //  B 410 的行种类 = BodyOp ⇒ `FireTriggerRowsByChosenAbility` 只出声、不结算
    // ==================================================================
    static void GroupB_RowKind()
    {
        Section("B `TriggerRowKindOf` —— 410 的 `ca` 落 `BodyOp`（本轮不结算）");
        Check(RuleCore.TriggerRowKindOf(40), RuleCore.TriggerRowKind.BodyOp,
              "B1 `TriggerRowKindOf(40)` = BodyOp（`ASH81` 的 410 行）"
            + "｜🧨 哪天它变成可执行的那一类，本行红 ⇒ 回读报告 §三：410 会与监听者正文路**双发**");
        Check(RuleCore.TriggerRowKindOf(30), RuleCore.TriggerRowKind.BodyOp,
              "B2 `TriggerRowKindOf(30)` = BodyOp（`GSC14` 的 410 行）"
            + "｜🧨 同上");
        Check(RuleCore.TriggerRowKindOf(320), RuleCore.TriggerRowKind.FollowUp,
              "B3 `TriggerRowKindOf(320)` = FollowUp（监听者行那一支由 `RunFollowAfterResolutionRow` 负责）");
        Check(RuleCore.TriggerRowKindOf(50), RuleCore.TriggerRowKind.BodyOp,
              "B4 `TriggerRowKindOf(50)` = BodyOp（唯一带执行器的 BodyOp 种类：`RunHandBuffRowOfEnchantment`）");
    }

    // ==================================================================
    //  C `TriggerOpsByNumber(410)` 恒 null ⇒ `FireTriggerByNumber` 走不到正文那一路
    // ==================================================================
    static void GroupC_NoTriggerOps(List<CardDef> pool)
    {
        Section("C `TriggerOpsByNumber(410)` —— 两张卡都取不到正文");
        foreach (var id in new[] { "ASH81", "GSC14" })
        {
            var c = Find(pool, id);
            CheckTrue(c != null, "C· 前提：池内有 " + id);
            if (c == null) continue;
            var ops = c.TriggerOpsByNumber(410);
            int n = ops == null ? 0 : ops.Count;
            Check(n, 0, "C· " + id + " 的 `TriggerOpsByNumber(410)` = 空"
                      + "（⇒ `FireTriggerByNumber` 落进 `FireTriggerRowsByChosenAbility` 的 `ca` 分派，"
                      + "而那一支对 BodyOp **不结算**）");
            CheckTrue(!c.HasTriggerOpsByNumber,
                      "C· " + id + " 的 `triggerOpsByNumber` 列**没挂表**（`HasTriggerOpsByNumber = false`）"
                    + "⇒ 上面那一格走的是「卡面关键词 == 触发词名」退回档，不是数据");
        }
    }

    // ==================================================================
    //  D `AbilityLayerOwnsRow` 对 410 行恒 false（B6：真效果种类 ⇒ 卡面正文负责）
    // ==================================================================
    static void GroupD_AbilityLayerNotOwner(List<CardDef> pool)
    {
        Section("D `AbilityLayerOwnsRow` —— 410 行**不归**能力层");
        foreach (var pair in new[] { new[] { "ASH81", "410" }, new[] { "GSC14", "410" } })
        {
            var c = Find(pool, pair[0]);
            int trigger = int.Parse(pair[1]);
            CheckTrue(c != null, "D· 前提：池内有 " + pair[0]);
            if (c == null) continue;
            var rows = c.TriggerAbilityRowsOf(trigger);
            CheckTrue(rows != null && rows.Length > 0, "D· " + pair[0] + " 的 trigger " + trigger + " 确有行");
            if (rows == null) continue;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;
                string why;
                bool owns = RuleCore.AbilityLayerOwnsRow(c, rows[i], out why);
                CheckTrue(!owns, "D· " + pair[0] + " 410 行 #" + i + "（ab" + rows[i].Ab + "/lg" + rows[i].Lg
                               + "/ca" + rows[i].Ca + "）**不归**能力层（why = " + (why ?? "null") + "）"
                             + " ⇒ 效果由卡面正文承担；接 410 执行器之前必须先补让路闸");
            }
        }
    }

    // ==================================================================
    //  E 监听者路的命中判据（名字对得上 = 能走到 `:10662`，但 410 那一跳仍是空操作）
    // ==================================================================
    static void GroupE_SchedulerNames(List<CardDef> pool)
    {
        Section("E `FollowUpSchedulerOf` —— 监听者路命中的那两条名字");
        var gsc = Find(pool, "GSC14");
        var ash = Find(pool, "ASH81");
        CheckTrue(gsc != null && RuleCore.FollowUpSchedulerOf(gsc) == "OtherCardPlayed",
                  "E1 `GSC14` 的 320 调度者 = `OtherCardPlayed`（`EffectResolver.cs:7966`）");
        CheckTrue(ash != null && RuleCore.FollowUpSchedulerOf(ash) == "OtherCardSpiritStone",
                  "E2 `ASH81` 的 320 调度者 = `OtherCardSpiritStone`（`EffectResolver.cs:7964`）");
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
        if (_fail == 0) Debug.Log($"B141_W48 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B141_W48 合计：{_pass} 通过 / {_fail} 失败");
    }

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
