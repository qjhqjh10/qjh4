// B110_E5_Assert.cs — 第一百一十会话 · 执行代理 `B110_E5` 的断言
//
// ============================ 它盯的是什么 ============================
//  🔴5：`CardDatabase.LoadEnchantmentTemplates` 从未调 `AttachTriggerAbilityIndices` ⇒ **池外的附魔模板在引擎里
//  没有任何 ability 行** ⇒ `EnchantmentHasAbilityForTrigger` 只能落「按文本 op 兜底」那一档
//  ⇒ 池外 30 张被可玩卡授予的模板**一条 trigger 都派不出来**（判据 `判据/B110_V1_真缺陷复核A.md` §🔴5）。
//
//  原版判据：每张卡（含附魔模板）恒有 `cardAbilities`；`CardAbility__CanTriggerAbility.c:71` 按
//  `ability.trigger(+0x10) == 传入 trigger` 选 ability ⇒ 我们的对应物 = `CardDef.TriggerAbilityRowsOf(trigger)`。
//  `triggerValue` = `CardAbility.triggerValue // +0x20`（`dump.cs:20340`；`判据/B109_R18_附魔三判据.md` §2）。
//
// ============================ 本件测什么 / 不测什么 ============================
//  ✅ A 组：映射器 `CardDatabase.MapEnchantmentTriggerAbilityIndices` 的**三态**（直调，纯函数）
//  ✅ B 组：真表端到端 —— `LoadEnchantmentTemplates()` 出来的每张模板**都挂了 ability 行**
//  ✅ C 组：打击面 —— 可玩卡 `enchantment` 列授予的**池外模板**每一张都有行（这一条才是「真缺陷已修」）
//  ❌ 不测「派发趟里效果数值对不对」：模板卡的正文 op 为空（模板不进 `cards_engine.json` 的 `cards[]`），
//     所以本次修好的是**「有没有 ability 行」这一层**，不是效果正文。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · B2 ★：删掉 `CardDatabase.cs` 里 `def.AttachTriggerAbilityIndices(MapEnchantmentTriggerAbilityIndices(...));`
//     那一句 ⇒ 全部模板 `HasTriggerAbilityIndices == false` ⇒ 红。
//   · A1/A2 ★：把映射器的 `if (rows == null) return null;` 删掉（`null` 折成空数组）⇒ 「未知 vs 明确没有」
//     分不开 ⇒ A1 红；把 `list.ToArray()` 改成 `length == 0 ? null : …` ⇒ A2 红。
//   · A3 ★：去掉 `if (a.trigger < 0) continue;` ⇒ 坏值行被当成 `trigger 0 (ThisCardPlayed)` 派出去 ⇒ 红。
//   · A8 ★：把 `LoadEnchantmentTemplates` 改成「`rows == null` 时也 `?? new[0]`」⇒ 列未知被折成「明确没有」
//     ⇒ 消费端不再出声兜底 ⇒ A8 红。
//   · C2 ★：把 `MapEnchantmentTriggerAbilityIndices` 恒返回 `null` ⇒ 池外 30 张全部 0 行 ⇒ 红。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B110_E5 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>；⛔ 不改任何宿主 ——
/// 接线 `B110_E5_Assert.Run(); _sink.Pass += B110_E5_Assert.Pass; _sink.Fail += B110_E5_Assert.Fail;` 归调度台）。</summary>
public static class B110_E5_Assert
{
    const string P = "[B110/E5] ";

    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B110/E5：附魔模板挂 `AttachTriggerAbilityIndices`（池外 30 张今天一条 trigger 都派不出来）");

        try { GroupA_MapperThreeState(); } catch (Exception e) { Crash("A 映射器三态", e); }
        try { GroupB_RealTemplateTable(); } catch (Exception e) { Crash("B 真表端到端", e); }
        try { GroupC_ReachableOutOfPool(); } catch (Exception e) { Crash("C 池外打击面", e); }

        Summary();
    }

    // ==================================================================
    //  A 组：映射器三态（直调；纯函数）
    // ==================================================================
    static void GroupA_MapperThreeState()
    {
        Section("A `MapEnchantmentTriggerAbilityIndices` 的三态");

        // A1 · 缺键（入参 null）⇒ **null**（列未知）
        var unknown = CardDatabase.MapEnchantmentTriggerAbilityIndices(null, "E5FixtureUnknown");
        CheckTrue(unknown == null,
                  "A1 ★ 入参 `null`（表里连 `abilities` 键都没有）⇒ 返回 **null**（列未知，⛔ 不是空数组）");

        // A1b · 把「未知」真的接到 `CardDef` 上 ⇒ `TriggerAbilityRowsOf` 也是 null（= 消费端兜底那一档的入口）
        var unknownCard = MakeCard("E5Unknown");
        unknownCard.AttachTriggerAbilityIndices(unknown);
        CheckTrue(unknownCard.TriggerAbilityRowsOf(120) == null,
                  "A1b ★ 列未知 ⇒ `TriggerAbilityRowsOf(120) == null`（消费端才落「按文本 op 兜底」并出声）");

        // A2 · 空表（`abilities: []`）⇒ **空数组**（明确没有）
        var knownEmpty = CardDatabase.MapEnchantmentTriggerAbilityIndices(
            new CardDatabase.EnchantmentTemplateAbilityDto[0], "E5FixtureEmpty");
        CheckTrue(knownEmpty != null && knownEmpty.Length == 0,
                  "A2 ★ 入参 `[]` ⇒ 返回**空数组**（明确没有 ability；⛔ 别与 A1 的 null 折成一档）");

        // A3 · 坏 trigger（< 0）⇒ 出声 + 丢弃该行；结果仍是「明确没有」的空数组
        var badRows = CardDatabase.MapEnchantmentTriggerAbilityIndices(new[]
        {
            new CardDatabase.EnchantmentTemplateAbilityDto
            {
                trigger = -1,
                logic = new[] { new CardDatabase.EnchantmentTemplateLogicDto() },
            },
        }, "E5FixtureBadTrigger");
        CheckTrue(badRows != null && badRows.Length == 0,
                  "A3 ★ `trigger < 0` 的坏行 ⇒ **丢弃**（⛔ 不猜 0 = ThisCardPlayed）⇒ 结果 = 空数组（上方应有一条 LogError 出声）");

        // A4 · 元素是 null ⇒ 丢弃
        var nullElem = CardDatabase.MapEnchantmentTriggerAbilityIndices(
            new CardDatabase.EnchantmentTemplateAbilityDto[] { null }, "E5FixtureNullElem");
        CheckTrue(nullElem != null && nullElem.Length == 0, "A4 `abilities[i] == null` ⇒ 丢弃（结果仍是空数组，不抛）");

        // A5 · 没有 logic ⇒ 仍占**一行**，`Lg = -1`
        var noLogic = CardDatabase.MapEnchantmentTriggerAbilityIndices(new[]
        {
            new CardDatabase.EnchantmentTemplateAbilityDto { trigger = 120, logic = null },
        }, "E5FixtureNoLogic");
        CheckTrue(noLogic != null && noLogic.Length == 1 && noLogic[0].Lg == -1 && noLogic[0].Ab == 0
                  && noLogic[0].Trigger == 120,
                  "A5 一条 `trigger=120` 的 ability 没有 logic ⇒ **仍占一行**（`ab=0, lg=-1, trigger=120`，"
                + "与卡表 `triggerAbilityIndices` 的 `lg=-1` 同口径）");

        // A6 · 一条 ability 的 N 条 logic ⇒ N 行、`Ab` 相同（⛔ 别去重）
        var multi = CardDatabase.MapEnchantmentTriggerAbilityIndices(new[]
        {
            new CardDatabase.EnchantmentTemplateAbilityDto
            {
                trigger = 130, triggerValue = 1,
                logic = new[]
                {
                    new CardDatabase.EnchantmentTemplateLogicDto(),
                    new CardDatabase.EnchantmentTemplateLogicDto(),
                },
            },
            new CardDatabase.EnchantmentTemplateAbilityDto { trigger = 410, logic = new CardDatabase.EnchantmentTemplateLogicDto[0] },
        }, "E5FixtureMulti");
        CheckTrue(multi != null && multi.Length == 3,
                  "A6 ★ 两条 ability（前者 2 条 logic、后者 0 条）⇒ **3 行**（2 + 1；N 条 logic ⇒ N 行，⛔ 不去重）");
        CheckTrue(multi != null && multi.Length == 3 && multi[0].Ab == 0 && multi[0].Lg == 0
                  && multi[1].Ab == 0 && multi[1].Lg == 1 && multi[2].Ab == 1 && multi[2].Lg == -1,
                  "A6b 行顺序 = 表顺序（`(ab,lg)` = `(0,0) (0,1) (1,-1)`）");

        // A7 · `triggerValue` 原样带出来；缺键/坏值 ⇒ -1（⛔ 不是 0）
        CheckTrue(multi != null && multi.Length == 3 && multi[0].TriggerValue == 1 && multi[2].TriggerValue == -1,
                  "A7 ★ `triggerValue` 原样带出（`1` 保住）；缺键 ⇒ **-1**（未知；⛔ 别折成 0 = 「明确 val 0」）"
                + "｜🧨 把 `TriggerValue = a.triggerValue` 删掉 ⇒ 恒 -1 ⇒ 红");

        // A8 · 空 logic 的那条也真的能被 `TriggerAbilityRowsOf` 选中（`Lg=-1` 不能让它被过滤掉）
        var card = MakeCard("E5Multi");
        card.AttachTriggerAbilityIndices(multi);
        var r130 = card.TriggerAbilityRowsOf(130);
        var r410 = card.TriggerAbilityRowsOf(410);
        CheckTrue(r130 != null && r130.Length == 2, "A8 ★ `TriggerAbilityRowsOf(130)` 恰 **2** 行（多 logic 不去重）");
        CheckTrue(r410 != null && r410.Length == 1 && r410[0].Lg == -1,
                  "A8b ★ `TriggerAbilityRowsOf(410)` 恰 **1** 行且 `Lg == -1`（空 logic 那行没被过滤）");
    }

    // ==================================================================
    //  B 组：真表端到端
    // ==================================================================
    static void GroupB_RealTemplateTable()
    {
        Section("B 真表端到端：`LoadEnchantmentTemplates()` 出来的每张模板都有 ability 行");

        List<CardDef> tpl = null;
        try { tpl = CardDatabase.LoadEnchantmentTemplates(); }
        catch (Exception e) { Debug.LogWarning(P + "`LoadEnchantmentTemplates()` 抛了：" + e.Message); }
        CheckTrue(tpl != null && tpl.Count >= 80,
                  "B1 前提：第二张表读得到（现读 86 张 · 2026-11-02 `B116_W28` 并集闸订正 85→86，阈值 `>= 80` 不变；< 80 说明资源没进工程 ⇒ 后面按「前提不成立」处理）");
        if (tpl == null || tpl.Count == 0) return;

        // B2 ★★ 灭自证主断言：**每一张**都必须有「列已知」的正面信号
        int hasCol = 0;
        var noCol = new List<string>();
        var emptyTpl = new List<string>();
        for (int i = 0; i < tpl.Count; i++)
        {
            var t = tpl[i];
            if (t == null) continue;
            if (t.HasTriggerAbilityIndices) hasCol++; else noCol.Add(t.Name ?? t.Id ?? "?");
            if (t.HasTriggerAbilityIndices && t.TriggerAbilityIndices != null && t.TriggerAbilityIndices.Length == 0)
                emptyTpl.Add(t.Name ?? t.Id ?? "?");
        }
        CheckTrue(noCol.Count == 0,
                  "B2 ★★★ **全部** " + tpl.Count + " 张模板都挂了 `triggerAbilityIndices`（列已知 " + hasCol + " 张）"
                + "（未知的 = " + noCol.Count + " 张" + (noCol.Count == 0 ? "" : "：" + string.Join(" / ", noCol.ToArray())) + "）"
                + "｜🧨 删掉 `LoadEnchantmentTemplates` 里 `def.AttachTriggerAbilityIndices(...)` 那一句 ⇒ 全部未知 ⇒ 红");

        // B3 · 空行模板只能是「真值里 `cardAbilities` 就是空」的那两张（现读 `Hive Fleet Leviathan` / `Saviour Protocols`）
        bool onlyKnownEmpty = true;
        for (int i = 0; i < emptyTpl.Count; i++)
            if (emptyTpl[i] != "Hive Fleet Leviathan" && emptyTpl[i] != "Saviour Protocols") onlyKnownEmpty = false;
        CheckTrue(onlyKnownEmpty,
                  "B3 「明确没有 ability」的模板只有真值里就为空的那两张"
                + "（现读 " + emptyTpl.Count + " 张" + (emptyTpl.Count == 0 ? "" : "：" + string.Join(" / ", emptyTpl.ToArray())) + "）");

        // B4 ★ 点名样张：`Ferocious Rage`（判据给的正样例，trigger 120）
        var rage = Find(tpl, "0078441d07f6d604db6c6cfb1c1fa46f");
        CheckTrue(rage != null, "B4 前提：模板表里找得到 `Ferocious Rage`（`0078441d07f6d604db6c6cfb1c1fa46f`）");
        if (rage == null) return;
        var r120 = rage.TriggerAbilityRowsOf(120);
        CheckTrue(r120 != null && r120.Length > 0,
                  "B4 ★★ `Ferocious Rage` 的 `TriggerAbilityRowsOf(120)` **非 null 且非空**（行数 = "
                + (r120 == null ? "null" : r120.Length.ToString()) + "）"
                + "｜🧨 同 B2：删掉那一跳 ⇒ 这里是 null ⇒ 红");

        // B5 · 反例：「明确没有」≠「未知」—— 同一张模板查一个不存在的 trigger 必须是**空数组**
        var r999 = rage.TriggerAbilityRowsOf(999);
        CheckTrue(r999 != null && r999.Length == 0,
                  "B5 ★ 同一张模板 `TriggerAbilityRowsOf(999)` = **空数组**（明确没有），⛔ 不是 null（未知）");

        // B6 · `Hallowed Martyrs`：`[130, 410]` 两条都要在（410 那条是 410 去重闩的唯一数据源）
        var hallowed = Find(tpl, "752141bbf0543314987e3b9afed938af");
        CheckTrue(hallowed != null, "B6 前提：模板表里找得到 `Hallowed Martyrs`");
        if (hallowed != null)
        {
            var r130 = hallowed.TriggerAbilityRowsOf(130);
            var r410 = hallowed.TriggerAbilityRowsOf(410);
            CheckTrue(r130 != null && r130.Length > 0 && r410 != null && r410.Length > 0,
                      "B6 ★ `Hallowed Martyrs` 同时有 `130` 与 `410` 两行（410 那条是 `Enchantments.ApplyFollowUpResolutionDedup` 的"
                    + "唯一数据源；现读 `130=" + (r130 == null ? "null" : r130.Length.ToString())
                    + " / 410=" + (r410 == null ? "null" : r410.Length.ToString()) + "`）");
        }
    }

    // ==================================================================
    //  C 组：打击面 —— 池外被授予的模板每一张都有行
    // ==================================================================
    static void GroupC_ReachableOutOfPool()
    {
        Section("C 打击面：可玩卡 `enchantment` 列授予的**池外**模板，每一张都有 ability 行");

        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning(P + "`CardDatabase.Load()` 抛了：" + e.Message); }
        CheckTrue(pool != null && pool.Count > 0, "C1 前提：卡池读得到（现读 1,129 张）");
        if (pool == null || pool.Count == 0) return;

        var poolIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < pool.Count; i++) if (pool[i] != null && !string.IsNullOrEmpty(pool[i].Id)) poolIds.Add(pool[i].Id);

        // 池外模板 id 集合（= 被可玩卡授予、但自己不是可玩卡的模板）
        var promised = new HashSet<string>(StringComparer.Ordinal);
        int grantRows = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null || string.IsNullOrEmpty(c.Id)) continue;
            List<Enchantments.GrantedEnchantmentRow> rows = null;
            try { rows = CardDatabase.EnchantmentGrantsOf(c.Id); }
            catch (Exception e) { Debug.LogWarning(P + "`EnchantmentGrantsOf(" + c.Id + ")` 抛了：" + e.Message); }
            if (rows == null) continue;
            for (int k = 0; k < rows.Count; k++)
            {
                var r = rows[k];
                if (r == null || string.IsNullOrEmpty(r.TemplateCardId)) continue;
                grantRows++;
                if (!poolIds.Contains(r.TemplateCardId)) promised.Add(r.TemplateCardId);
            }
        }
        CheckTrue(promised.Count >= 20,
                  "C1b 前提：池外被授予的模板 ≥ 20 张（现读 " + promised.Count + " 张；授予行 " + grantRows + " 条）");
        if (promised.Count == 0) return;

        List<CardDef> tpl = null;
        try { tpl = CardDatabase.LoadEnchantmentTemplates(); } catch (Exception) { }
        CheckTrue(tpl != null && tpl.Count > 0, "C1c 前提：模板表读得到");
        if (tpl == null || tpl.Count == 0) return;
        var byId = new Dictionary<string, CardDef>(StringComparer.Ordinal);
        for (int i = 0; i < tpl.Count; i++)
            if (tpl[i] != null && !string.IsNullOrEmpty(tpl[i].Id)) byId[tpl[i].Id] = tpl[i];

        int withRows = 0;
        var unknown = new List<string>();       // 列未知 = 真缺陷
        var zeroRows = new List<string>();      // 列已知但 0 行 = 真值里 `cardAbilities` 本来就空（合法）
        foreach (var id in promised)
        {
            CardDef t;
            if (!byId.TryGetValue(id, out t)) { unknown.Add(id + "（模板表里没有）"); continue; }
            var all = t.TriggerAbilityIndices;
            if (all == null) { unknown.Add((t.Name ?? id) + "（列未知）"); continue; }
            if (all.Length == 0) { zeroRows.Add(t.Name ?? id); continue; }
            withRows++;
        }
        CheckTrue(unknown.Count == 0,
                  "C2 ★★★ **池外被授予的 " + promised.Count + " 张模板全部挂了 ability 列**（列已知；现读 "
                + withRows + " 张有行 + " + zeroRows.Count + " 张「明确没有」/ " + unknown.Count + " 张未知"
                + (unknown.Count == 0 ? "" : "：" + string.Join(" / ", unknown.ToArray())) + "）"
                + "｜🧨 把映射器恒返回 null（或删掉 `AttachTriggerAbilityIndices` 那一跳）⇒ 全部「列未知」⇒ 红");
        bool zeroOk = true;
        for (int i = 0; i < zeroRows.Count; i++)
            if (zeroRows[i] != "Hive Fleet Leviathan" && zeroRows[i] != "Saviour Protocols") zeroOk = false;
        CheckTrue(zeroOk,
                  "C2b 「明确没有 ability」的池外被授予模板只能碰真值里就为空的那两张"
                + "（现读 " + zeroRows.Count + " 张" + (zeroRows.Count == 0 ? "" : "：" + string.Join(" / ", zeroRows.ToArray())) + "）");
        CheckTrue(withRows >= 20,
                  "C2c ★ **真正补上 ability 行的池外模板 ≥ 20 张**（现读 " + withRows + " 张；判据口径的 30 张里有 2 张真值本来就空）");

        // C3 · 对照（防「把两路写成互斥」）：**池内**的附魔模板卡走 `Parse` 那一跳，必须仍然有行
        int inPoolChecked = 0;
        for (int i = 0; i < tpl.Count; i++)
        {
            var t = tpl[i];
            if (t == null || string.IsNullOrEmpty(t.Id) || !poolIds.Contains(t.Id)) continue;
            inPoolChecked++;
            CheckTrue(t.HasTriggerAbilityIndices,
                      "C3 ★ 池内附魔模板「" + (t.Name ?? t.Id) + "」走 `Parse` 主路，`triggerAbilityIndices` **仍然非 null**"
                    + "（两路不该互斥）");
        }
        CheckTrue(inPoolChecked >= 1,
                  "C3b 前提：池内的附魔模板至少 1 张（现读 " + inPoolChecked + " 张；"
                + "0 = 模板表 `id` 与卡池 `id` 对不上，要人工看一眼，⛔ 不是「没有池内模板」）");
        Debug.Log(P + "C3 池内模板核对 " + inPoolChecked + " 张（现读 2 张：`Pray Enchantment` / `Reconnaissance Mission`）");
    }

    // ==================================================================
    //  夹具 / 读数
    // ==================================================================
    static CardDef Find(List<CardDef> list, string id)
    {
        if (list == null) return null;
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].Id == id) return list[i];
        return null;
    }

    static CardDef MakeCard(string id)
    {
        return new CardDef(id, id, "unit", "", null, "Test", 1, 1, 5, 0, null);
    }

    // ==================================================================
    //  最小断言框架（本文件独立类 ⇒ 不借用任何宿主的 private 助手）
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Crash(string seg, Exception e)
    {
        _fail++;
        Debug.LogError(P + "✗ 断言段「" + seg + "」抛异常（后面的条目没跑完）：" + e);
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log("B110_E5 合计：" + _pass + " 通过 / 0 失败");
        else Debug.LogError("B110_E5 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }

    // 宿主聚合行读这两个口（追加到类尾、零行漂）。
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
