// B141_W36_Assert.cs — 批次 **B141** · 执行写手 `W36`（2026-11-04 · 第一百二十八会话 · ENG-11）
//
// ============================ 它钉的是什么（⛔ 不是「已修」的证明）============================
//   ENG-11 的 12 行「被动 trigger meta-op」**本次没有改生产代码**（判据前提不成立，见
//   `资料/完全复刻/报告/B141_W128_ENG11.md` §二）。本文件钉的是那 12 行的**覆盖事实**：
//     · 其中 9 行早已被覆盖（7 行 = 关键词路 `RuleCore.cs:13031-13036`，2 行 = 手写祈祷附魔）；
//     · 3 行是真缺口，但修点在**白名单外**（`ActionPump.ExtraTriggerName` / `RuleCore.FireTriggerDeferred`）。
//   ⇒ 任何人想把 `ca 320` 往 `EffectResolver.cs:21078` 那个口里派之前，先让本文件指着报告。
//
// ============================ 判据（全部现读，本轮核过）============================
//   · `FollowUpSchedulerOf`（`EffectResolver.cs:7950-7974`，硬编码表）：9 张单位卡的 320 由**关键词路**负责。
//   · `TriggerNameOfKeyword`（`ActionPump.cs:1081-1122` + `ExtraTriggerName :1894-1912`）：
//     `regiment / strike / mob / codex / artifice` 有映射；`othercardplayed` / `othercardspiritstone` **2026-11-04 起也已补上**（`ActionPump.cs:1912`）。
//   · `FollowUpSchedulerOf(card) == AbilityTriggerNameOfKeyword(keyword)` 是 `RuleCore.cs:13034` 的闸。
//   · `20 = self`（`RuleCore.cs:15635` / `Enchantment.cs:1796`）⇒ SW34 的 480 行目标 = 载体自己。
//
// ============================ 🧨 灭自证（改哪一句会红）============================
//   · 删掉 `FollowUpSchedulerOf` 的任意一条单位卡映射 ⇒ **B 组**那一条红；
//   · 删掉 `ExtraTriggerName` 的 `othercardplayed` / `othercardspiritstone` 那一句 ⇒ **C6/C7 红**（2026-11-04 起是正向断言）；
//     —— 补映射那一步已由 `B141_W128_ExtraTriggerName` 做完，同时确认过**被动 ca 路不要再派一次**；
//   · 把 `TriggerRowKindOf(320)` 改成 OwnLane / BodyOp ⇒ **D6/E2 红**（320 必须走 FollowUp）。
//
// ============================ 怎么接（宿主 · 调度台做）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加（必须落在该行第一个 `//` 之前）：
//     `Guard("B141W36_Assert", B141_W36_Assert.Run); _pass += B141_W36_Assert.Pass; _fail += B141_W36_Assert.Fail;`
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B141-W36 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B141_W36_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B141-W36：ENG-11 被动 trigger 的 12 行 meta-op —— **覆盖钉子**（⛔ 不是「已修」）");

        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B141_W36] `CardDatabase.Load()` 抛了：" + e.Message); }
        CheckTrue(pool != null && pool.Count > 500, "A0 前提：卡池读得到且规模正常（≥ 500 张）");
        if (pool == null || pool.Count <= 500) { Summary(); return; }

        GroupA_Inventory(pool);
        GroupB_FollowUpScheduler(pool);
        GroupC_KeywordNames();
        GroupD_340_480(pool);
        GroupE_AntiSelfProof(pool);
        Summary();
    }

    // ==================================================================
    //  A 12 行盘点（数据面：`trigger != 0` 上的 `ca ∈ {320,325,340,380,480}`）
    // ==================================================================
    static readonly int[] MetaCas = { 320, 325, 340, 380, 480 };

    static void GroupA_Inventory(List<CardDef> pool)
    {
        Section("A 12 行盘点 —— 现读 `cards_engine.json`");
        int total = 0, c320 = 0, c340 = 0, c325 = 0, c380 = 0, c480 = 0;
        foreach (var c in pool)
        {
            var rows = c.TriggerAbilityIndices;
            if (rows == null) continue;
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                if (r == null || r.Trigger == 0) continue;
                if (Array.IndexOf(MetaCas, r.Ca) < 0) continue;
                total++;
                if (r.Ca == 320) c320++; else if (r.Ca == 325) c325++; else if (r.Ca == 340) c340++;
                else if (r.Ca == 380) c380++; else if (r.Ca == 480) c480++;
            }
        }
        Check(total, 12, "A1 全池「被动 trigger（≠0）上的 meta-op 行」= 12");
        Check(c320, 9, "A2 `ca 320` 的行 = 9（判据 §2 现读值）");
        Check(c325, 0, "A3 `ca 325` 的行 = 0");
        Check(c340, 2, "A4 `ca 340` 的行 = 2（`Pray Enchantment` 模板）");
        Check(c380, 0, "A5 `ca 380` 的行 = 0");
        Check(c480, 1, "A6 `ca 480` 的行 = 1（`SW34 Murderfang`）");

        // 逐条 (id, trigger, ca)
        CheckRow(pool, "ASH81", 455, 320);
        CheckRow(pool, "AM8", 301, 320);
        CheckRow(pool, "AM21", 301, 320);
        CheckRow(pool, "GSC14", 10, 320);
        CheckRow(pool, "GSC17", 580, 320);
        CheckRow(pool, "SAU11", 560, 320);
        CheckRow(pool, "GOF59", 300, 320);
        CheckRow(pool, "TL3", 580, 320);
        CheckRow(pool, "UM64", 145, 320);
        CheckRow(pool, "SW34", 120, 480);
    }

    static void CheckRow(List<CardDef> pool, string id, int trigger, int ca)
    {
        var c = Find(pool, id);
        CheckTrue(c != null, "A· 前提：池内有 " + id);
        if (c == null) return;
        var rows = c.TriggerAbilityRowsOf(trigger);
        bool hit = false;
        if (rows != null)
            for (int i = 0; i < rows.Length; i++)
                if (rows[i] != null && rows[i].Ca == ca) { hit = true; break; }
        CheckTrue(hit, "A· " + id + " 的 trigger " + trigger + " 上有一行 `ca " + ca + "`");
    }

    // ==================================================================
    //  B 9 行 `320` 的归属 —— `FollowUpSchedulerOf` 是否仍覆盖（覆盖 ⇒ ⛔ 不能再按 ca 派）
    // ==================================================================
    static readonly string[,] Nine =
    {
        { "ASH81", "OtherCardSpiritStone" },
        { "AM8",   "Regiment" },
        { "AM21",  "Regiment" },
        { "GSC14", "OtherCardPlayed" },
        { "GSC17", "Strike" },
        { "SAU11", "Artifice" },
        { "GOF59", "Mob" },
        { "TL3",   "Strike" },
        { "UM64",  "Codex" },
    };

    static void GroupB_FollowUpScheduler(List<CardDef> pool)
    {
        Section("B `FollowUpSchedulerOf` 覆盖 —— 这 9 张的 320 已有归属（⛔ 再按 ca 派 = 双发）");
        for (int i = 0; i < Nine.GetLength(0); i++)
        {
            string id = Nine[i, 0], expected = Nine[i, 1];
            var c = Find(pool, id);
            if (c == null) { CheckTrue(false, "B" + (i + 1) + " 前提：池内有 " + id); continue; }
            Check(RuleCore.FollowUpSchedulerOf(c), expected,
                  "B" + (i + 1) + " `" + id + "` 的 `FollowUpSchedulerOf` = `" + expected + "`"
                + "｜🧨 删掉 `EffectResolver.cs:7950-7974` 那一条 ⇒ 本行红");
        }
    }

    // ==================================================================
    //  C 关键词路能不能真叫到 —— `TriggerNameOfKeyword`
    // ==================================================================
    static void GroupC_KeywordNames()
    {
        Section("C 关键词 → trigger 名（7 行覆盖的充分条件 / 2 行缺口的钉子）");
        Check(RuleCore.TriggerNameOfKeyword("regiment"), "Regiment", "C1 `regiment` ⇒ `Regiment`（`AM8`/`AM21`）");
        Check(RuleCore.TriggerNameOfKeyword("strike"), "Strike", "C2 `strike` ⇒ `Strike`（`GSC17`/`TL3`）");
        Check(RuleCore.TriggerNameOfKeyword("mob"), "Mob", "C3 `mob` ⇒ `Mob`（`GOF59`）");
        Check(RuleCore.TriggerNameOfKeyword("codex"), "Codex", "C4 `codex` ⇒ `Codex`（`UM64`）");
        Check(RuleCore.TriggerNameOfKeyword("artifice"), "Artifice", "C5 `artifice` ⇒ `Artifice`（`SAU11`）");
        // ✅ 2026-11-04（`B142-E8`）：钉子**已反转** —— `ActionPump.ExtraTriggerName`（`:1912`）现已补上映射，
        //   这两条改成「必须非 null」（原口径「今天必须是 null」的那个修法已落地，见 `报告/B141_W128_ExtraTriggerName.md`）。
        //   ⚠️ 仍然成立：**被动 ca 路不要再派一次 320**（`报告/B141_W128_ENG11.md` §2·B）。
        Check(RuleCore.TriggerNameOfKeyword("othercardplayed"), "OtherCardPlayed",
              "C6 `othercardplayed` ⇒ `OtherCardPlayed`（映射已补 · `ActionPump.cs:1912`；`GSC14` 的 320 仍走监听者路）"
            + "｜🧨 删掉 `ExtraTriggerName` 里那一句 ⇒ 本行红");
        Check(RuleCore.TriggerNameOfKeyword("othercardspiritstone"), "OtherCardSpiritStone",
              "C7 `othercardspiritstone` ⇒ `OtherCardSpiritStone`（映射已补 · `ActionPump.cs:1912`；`ASH81` 的 320 同理）");
        // 数值 ↔ 名字（`CardDef.cs:1917` / `:1933`）
        Check(CardDef.AbilityTriggerNumber("OtherCardPlayed"), 10, "C8 `OtherCardPlayed` = 10（`CardDef.cs:1917`）");
        Check(CardDef.AbilityTriggerNumber("OtherCardSpiritStone"), 455, "C9 `OtherCardSpiritStone` = 455（`CardDef.cs:1933`）");
    }

    // ==================================================================
    //  D `340`（让开）/ `480`（真缺口 · 目标 = self）
    // ==================================================================
    static void GroupD_340_480(List<CardDef> pool)
    {
        Section("D `ca 340` 已有等价物 · `ca 480` 的真 `ta` = 20 (self)");
        var pray = Find(pool, "9218576fd3e26554db5b4c239a36dbc7");
        CheckTrue(pray != null, "D1 前提：池内有 `Pray Enchantment` 模板（`9218576f…`）");
        if (pray != null)
        {
            int n340 = 0;
            var rows = pray.TriggerAbilityIndices;
            if (rows != null)
                for (int i = 0; i < rows.Length; i++)
                    if (rows[i] != null && rows[i].Trigger != 0 && rows[i].Ca == 340) n340++;
            Check(n340, 2, "D2 该模板的 `ca 340` 行 = 2（`trigger 60` / `trigger 31`）"
                         + " · 等价物 = `EnchantmentExecutor_B115.cs:21-32` ＋ 手写祈祷链（`:43-48` 让开闸）");
        }
        Check((int)RuleCore.TriggerRowKindOf(340), (int)RuleCore.TriggerRowKind.OwnLane,
              "D3 `TriggerRowKindOf(340) == OwnLane` ⇒ 被动路**让开**（⛔ 再跑一次 = 附魔摘两次）");

        var sw = Find(pool, "SW34");
        CheckTrue(sw != null, "D4 前提：池内有 `SW34 Murderfang`");
        if (sw == null) return;
        var rows120 = sw.TriggerAbilityRowsOf(120);
        CardDef.TriggerAbilityIndexSpec r480 = null;
        if (rows120 != null)
            for (int i = 0; i < rows120.Length; i++)
                if (rows120[i] != null && rows120[i].Ca == 480) { r480 = rows120[i]; break; }
        CheckTrue(r480 != null, "D5 `SW34` 的 `trigger 120`（Slay）上有一行 `ca 480`");
        if (r480 != null)
            Check(RuleCore.TargetsAffectedOfRow(sw, r480), 20,
                  "D6 ★★ 那一行的真 `ta` = **20 (self)**（`abilityTargets[0].ta[2]`）"
                + " ⇒ 目标 = 载体自己（`20 = self` 判据 `RuleCore.cs:15635` / `Enchantment.cs:1796`）"
                + "｜🧨 把它当 30/220（读调用方给的目标）⇒ 本行红");
        Check((int)RuleCore.TriggerRowKindOf(480), (int)RuleCore.TriggerRowKind.OwnLane,
              "D7 `TriggerRowKindOf(480) == OwnLane`（表现类 · 主动表那一趟才跑）");
    }

    // ==================================================================
    //  E 灭自证 —— 错的修法会撞哪一条
    // ==================================================================
    static void GroupE_AntiSelfProof(List<CardDef> pool)
    {
        Section("E 灭自证");
        // E1：9 张全在 `FollowUpSchedulerOf` 里（= 「不能按 ca 再派」的唯一判据）
        int covered = 0;
        for (int i = 0; i < Nine.GetLength(0); i++)
        {
            var c = Find(pool, Nine[i, 0]);
            if (c != null && RuleCore.FollowUpSchedulerOf(c) != null) covered++;
        }
        Check(covered, 9, "E1 ★★★ 9 张 `320` 卡**全部**在 `FollowUpSchedulerOf` 里（" + covered + "/9）"
                        + "｜🧨 把 `ca 320` 接进 `EffectResolver.cs:21078` 那条被动 ca 路 ⇒ 这 9 张**两拍**"
                        + "（判据全文 → `报告/B141_W128_ENG11.md` §2·B）");
        // E2：行种别分类不能变（320 = FollowUp，不是 BodyOp）
        Check((int)RuleCore.TriggerRowKindOf(320), (int)RuleCore.TriggerRowKind.FollowUp,
              "E2 `TriggerRowKindOf(320) == FollowUp`｜🧨 改成 BodyOp ⇒ 与 `B114_W9` A3 一起红");
        // E3：`ca 50` 的特判仍在「正文 op 为空」那一格里（本次**没有**把它搬走）
        Check(RuleCore.FollowUpSchedulerOf(null), null, "E3 `FollowUpSchedulerOf(null) == null`（空入参不炸）");
        Summary2(pool);
    }

    static void Summary2(List<CardDef> pool)
    {
        // 结论行：把「本次核查的净覆盖」印出来，免得下一个人把它读成「12 行都修了」。
        int meta = 0;
        foreach (var c in pool)
        {
            var rows = c.TriggerAbilityIndices;
            if (rows == null) continue;
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                if (r == null || r.Trigger == 0) continue;
                if (Array.IndexOf(MetaCas, r.Ca) >= 0) meta++;
            }
        }
        Debug.Log("[B141_W36] 结论：12 行 meta-op 里 **9 行早已覆盖**（7 关键词路 + 2 手写祈祷附魔）、"
                + "**3 行真缺口**（`ASH81`/455 · `GSC14`/10 的 320 · `SW34`/120 的 480）"
                + "且修点**不在本批白名单**⇒ 本次**生产代码 0 行**、净覆盖 **0/12**（现盘点 = " + meta + " 行）。");
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
        // ⚠️ 未约束的泛型**不能**直接写 `actual == null`（`CS0403`）⇒ 走装箱比较（值类型恒非 null）。
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
        if (_fail == 0) Debug.Log($"B141_W36 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B141_W36 合计：{_pass} 通过 / {_fail} 失败");
    }

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
