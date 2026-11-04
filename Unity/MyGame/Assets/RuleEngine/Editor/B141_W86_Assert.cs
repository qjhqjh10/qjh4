// B141_W86_Assert.cs — 批次 **B141** · 执行写手 `B141-W86`（2026-11-04 · 第一百二十八会话 · `B141-HAND-TRIGGER-KEY`）
//
// ============================ 本文件钉的是什么 ============================
//   `RuleCore.KeywordFromTriggerName(string triggerName)` —— **trigger 名 → 引擎关键词** 的反查
//   （`TriggerNameOfKeyword` 的逆；实现 = `RuleEngine/Core/ActionPump.cs` 类尾 §十二）。
//
//   🔴 为什么要有它（判据 = `资料/完全复刻/报告/B141_W128_CaRowDispatch.md` §六·❓2）：
//   手牌触发路 `EffectResolver.FireHandAbilityTrigger`（`EffectResolver.cs:19056`）的兜底是
//   `string kw = keyword ?? triggerName;`，再喂 `TriggerCarrier.FxOps(kw)` →
//   `CardDef.TriggerOps(kw)` / `CardDef.Effect(kw)`。而那两张表的键是**引擎关键词**
//   （小写 `mob` / `strike` / `rally` …），**不是** `AbilityTrigger` 枚举名（`TurnStart` / `UnitPoisoned` …）
//   ⇒ 12 个生产调用点全部传 `keyword: null` ⇒ `TriggerOps("TurnStart")` **恒 null**
//   ⇒ 手牌能力从来跑不了正文。
//
//   ⛔ **本文件不断「接线之后行为变了」** —— 接线那一句落在 `EffectResolver.cs`（本轮被 `W77` 占用，
//      本笔**不改**）。本文件钉的是**反查表本身**：覆盖数 · 双向往返 · 无重名歧义 · 不是 `ToLower()` 假货
//      · 以及「`TriggerOps` 真的只认推出的这个关键词」这一条**真实消费口**。
//
// ============================ 🧨 灭自证（改哪一句会红）============================
//   · 把 `KeywordFromTriggerName` 换成 `triggerName.ToLowerInvariant()`（或任何「小写兜底」写法）
//     ⇒ **A2 变 0** · **C1/C2/C3/C4 全红** · **E3/E4/E5 从 null 变成非 null**；
//   · 删掉正向表任一行（例如 `ExtraTriggerName` 的 `backlash → ThisUnitDeath`）
//     ⇒ **A1 / B1 的对数掉下来** + **C3 红**；
//   · 把反查表改成「手抄一份正向表的反向」（第二份映射）⇒ 本文件**仍然全绿**
//     —— 那一半只能靠**结构**判（`KeywordByTriggerNameTable` 是运行时从 `TriggerNameOfKeyword`
//     推导的：`§十二` 头注 + `KeywordCandidatesOutsidePrefixes` 里**只有候选词、没有任何名字**），
//     ⛔ 别指望断言能抓住「两份表」这种形状。
//   · D3 是一条**强**负例：`TriggerOps("Mob")` 必须为 null ⇒ 证明 D2 不是「反正都能取到」。
//
// ============================ 怎么接（宿主 · 调度台做）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合块语句（`{ Guard(...) … }` 那些行）**同一物理行**追加
//   （必须落在该行第一个 `//` 之前；⛔ 别新增行 —— 该文件按行号被账引用）：
//     `Guard("B141W86_Assert", B141_W86_Assert.Run); _pass += B141_W86_Assert.Pass; _fail += B141_W86_Assert.Fail;`
//   ⚠️ 本类在**全局命名空间**（类名 == 文件名 ⇒ Unity 建得出脚本资产；⛔ 别加 namespace）。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B141-W86 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B141_W86_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B141-W86：`KeywordFromTriggerName` —— trigger 名 → 关键词 的反查（`TriggerNameOfKeyword` 的逆）");

        GroupA_TableShape();
        GroupB_RoundTrip();
        GroupC_NonIdentity();
        GroupD_RealConsumer();
        GroupE_Negatives();
        GroupF_HandBroadcastNames();
        Summary();
    }

    // ==================================================================
    //  A 表的形状：覆盖数 / 非同名条数
    // ==================================================================
    static void GroupA_TableShape()
    {
        Section("A 反查表：31 对 · 4 对「字面不同」");
        var table = RuleCore.KeywordByTriggerNameTable;
        CheckTrue(table != null, "A0 **前提**：反查表建得起来（懒建，第一次调用时从正向函数推导）");
        if (table == null) return;
        Check(table.Count, 31,
              "A1 ★★ 覆盖数 = **31** —— 即正向表 `TriggerNameOfKeyword` 全部非 null 输出"
            + "（22 条主 switch + 9 条 `ExtraTriggerName`）；🧨 正向表增删一行 ⇒ 这里必须同步，否则红");
        Check(RuleCore.KeywordTriggerPairNonIdentityCount, 4,
              "A2 ★★ 「trigger 名」与「关键词」**字面不同**的条目 = **4** 条"
            + "（`UseSpiritStone`/`ThisCardPlayed`/`ThisUnitDeath`/`Rage`）；"
            + "🧨 把实现换成 `triggerName.ToLowerInvariant()` ⇒ 本行变 **0**");
    }

    // ==================================================================
    //  B 双向往返 + 无重名歧义（候选词 = `Prefixes` 的 target 列 + 表内合成词）
    // ==================================================================
    static void GroupB_RoundTrip()
    {
        Section("B 双向往返：每一对 (关键词 → trigger 名) 都反查得回来 · 且无重名歧义");
        var table = RuleCore.KeywordByTriggerNameTable;
        CheckTrue(table != null, "B0 **前提**：反查表存在");
        if (table == null) return;

        var cands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prefixes = KeywordTable.Prefixes;
        CheckTrue(prefixes != null && prefixes.Length > 50,
                  "B1 **前提**：`KeywordTable.Prefixes` 读得到（= `Normalize` 的唯一关键词注册表；现读 119 行）");
        if (prefixes != null)
            for (int i = 0; i < prefixes.Length; i++)
                if (prefixes[i] != null && prefixes[i].Length >= 2 && !string.IsNullOrEmpty(prefixes[i][1]))
                    cands.Add(prefixes[i][1]);
        foreach (var kv in table) cands.Add(kv.Value);   // 补上不在 `Prefixes` 里的两个合成词

        int pairs = 0, bad = 0, dup = 0;
        string firstBad = null, firstDup = null;
        var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kw in cands)
        {
            string name = RuleCore.TriggerNameOfKeyword(kw);
            if (string.IsNullOrEmpty(name)) continue;       // 这个关键词本来就不映射（多数）
            pairs++;
            string back = RuleCore.KeywordFromTriggerName(name);
            if (!string.Equals(back, kw, StringComparison.Ordinal))
            {
                bad++;
                if (firstBad == null) firstBad = name + " → " + (back ?? "null") + "（应为 " + kw + "）";
            }
            if (byName.ContainsKey(name))
            {
                dup++;
                if (firstDup == null) firstDup = name + "（" + byName[name] + " / " + kw + "）";
            }
            else byName[name] = kw;
        }

        Check(pairs, 31, "B2 ★★ 正向表的非 null 对数 = **31**（候选词 = `Prefixes` 119 行的 target 列 + 2 个合成词）");
        Check(bad, 0, "B3 ★★ 全部可反查：`KeywordFromTriggerName(TriggerNameOfKeyword(k)) == k`"
                    + (firstBad == null ? "" : "；❌ 首个不符 = " + firstBad));
        Check(dup, 0, "B4 ★★ 无重名歧义：**没有任何两个关键词落到同一个 trigger 名**"
                    + (firstDup == null ? "" : "；❌ 首个重名 = " + firstDup));
        Check(byName.Count, table.Count, "B5 表内对数 = 正向表对数（不多不少：反查表没漏、也没多出候选之外的条目）");

        // 反向再走一遍（从表这一侧）：表里的每一对都必须是正向表里真实存在的那一对
        int tableBad = 0; string tableFirstBad = null;
        foreach (var kv in table)
        {
            if (!string.Equals(RuleCore.TriggerNameOfKeyword(kv.Value), kv.Key, StringComparison.Ordinal))
            {
                tableBad++;
                if (tableFirstBad == null) tableFirstBad = kv.Key + " ← " + kv.Value;
            }
        }
        Check(tableBad, 0, "B6 ★ 表内每一对都经得起正向复核（`TriggerNameOfKeyword(值) == 键`）"
                         + (tableFirstBad == null ? "" : "；❌ 首个不符 = " + tableFirstBad));
    }

    // ==================================================================
    //  C 四条「不是小写同名」的硬映射（灭自证 ①）
    // ==================================================================
    static void GroupC_NonIdentity()
    {
        Section("C 四条**字面不同**的映射（照抄正向表，⛔ 不是 `ToLower()`）");
        Check(RuleCore.KeywordFromTriggerName("UseSpiritStone"), "spiritstone",
              "C1 ★ `UseSpiritStone` ⇒ `spiritstone`（`TriggerOps` 的键是 `spiritstone`，"
            + "`ActionPump.cs` 正向 `KeywordTable.SpiritStone` 那一格）｜🧨 换成 `ToLower()` ⇒ 得 `usespiritstone`");
        Check(RuleCore.KeywordFromTriggerName("ThisCardPlayed"), "rally",
              "C2 ★ `ThisCardPlayed` ⇒ `rally`（trigger **0** = 部署时 rally；不是 `thiscardplayed`）");
        Check(RuleCore.KeywordFromTriggerName("ThisUnitDeath"), "backlash",
              "C3 ★ `ThisUnitDeath` ⇒ `backlash`（trigger **80**；不是 `thisunitdeath`）");
        Check(RuleCore.KeywordFromTriggerName("Rage"), "penitence",
              "C4 ★ `Rage` ⇒ `penitence`（trigger **130**；卡面/数据里的词是 `penitence`，不是 `rage`）");
        Check(RuleCore.KeywordFromTriggerName("thisunitdeath"), "backlash",
              "C5 键匹配**忽略大小写**（调用点传的是驼峰枚举名，写法可能不同）");
    }

    // ==================================================================
    //  D ★ 真实消费口：`TriggerOps` 只认「推出的这个关键词」
    // ==================================================================
    static void GroupD_RealConsumer()
    {
        Section("D ★ 真实消费口：`TriggerOps(KeywordFromTriggerName(\"Mob\"))` 非空，而 `TriggerOps(\"Mob\")` = null");
        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B141_W86] `CardDatabase.Load()` 抛了：" + e.Message); }
        bool premise = pool != null && pool.Count > 500;
        CheckTrue(premise, "D0 **前提**：卡池读到且规模正常（≥ 500 张）");
        if (!premise) return;

        CardDef mobCard = null;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            var ops = c.TriggerOps("mob");
            if (ops != null && ops.Count > 0) { mobCard = c; break; }
        }
        CheckTrue(mobCard != null, "D1 **前提**：池内存在 `TriggerOps(\"mob\")` 非空的卡（⇒ `mob` 是真键）");
        if (mobCard == null) return;

        string kw = RuleCore.KeywordFromTriggerName("Mob");
        Check(kw, "mob", "D2 `KeywordFromTriggerName(\"Mob\")` = `mob`");
        var viaKw = mobCard.TriggerOps(kw);
        CheckTrue(viaKw != null && viaKw.Count > 0,
                  "D3 ★★ `TriggerOps(\"mob\")` 走得通 —— 这就是接线后手牌路能拿到的正文（样例卡 = "
                + mobCard.Name + "，op 数 = " + (viaKw == null ? 0 : viaKw.Count) + "）");
        var viaLiteral = mobCard.TriggerOps("Mob");
        Check(viaLiteral, null,
              "D4 ★★★ 灭自证：**旧兜底** `TriggerOps(triggerName)` = **null**"
            + "（`CardDef._triggerOps` 是大小写敏感的精确字典，`Mob` 不是键）⇒ 证明 D3 不是「反正都能取到」");
    }

    // ==================================================================
    //  E 负例：没有关键词的 trigger 名 ⇒ null（⛔ 不是 ToLower）
    // ==================================================================
    static void GroupE_Negatives()
    {
        Section("E 负例：没有对应关键词的 trigger 名 ⇒ `null`");
        Check(RuleCore.KeywordFromTriggerName(null), null, "E1 `null` ⇒ `null`（空入参不炸）");
        Check(RuleCore.KeywordFromTriggerName(""), null, "E2 `\"\"` ⇒ `null`");
        Check(RuleCore.KeywordFromTriggerName("TurnStart"), null,
              "E3 ★★ `TurnStart` ⇒ `null`｜🧨 换成「认不出就 `ToLower()`」兜底 ⇒ 本行变 `turnstart`（假关键词）");
        Check(RuleCore.KeywordFromTriggerName("UnitPoisoned"), null, "E4 ★ `UnitPoisoned` ⇒ `null`（同上）");
        Check(RuleCore.KeywordFromTriggerName("CardCreatedInHand"), null, "E5 ★ `CardCreatedInHand` ⇒ `null`（同上）");
        Check(RuleCore.KeywordFromTriggerName("NoSuchTrigger"), null, "E6 未知名 ⇒ `null`");
    }

    // ==================================================================
    //  F 手牌广播实际发的那批名字：现状对照表（本笔只钉「有没有关键词」）
    // ==================================================================
    static void GroupF_HandBroadcastNames()
    {
        Section("F 手牌广播的 9 个 trigger 名 → 关键词（现状：2 个有、7 个没有）");
        // 名字来源 = `FireHandAbilityTrigger` 的调用点（`EffectResolver.cs:19204/19206/19207/19209/19300/
        //   19303/19306/19311` + `EffectResolverPartial_B97W2.cs:129`）。⚠️ 本组**不断言行为**，
        //   只是把「接线之后哪些名字真的能拿到正文」摆成一张可读的表（改正向表会让它变）。
        Check(RuleCore.KeywordFromTriggerName("Cruelty"), "cruelty",
              "F1 `Cruelty` ⇒ `cruelty`（接线后**能**拿到正文）");
        Check(RuleCore.KeywordFromTriggerName("Relentless"), "relentless",
              "F2 `Relentless` ⇒ `relentless`（接线后**能**拿到正文）");
        Check(RuleCore.KeywordFromTriggerName("Talent"), null,
              "F3 `Talent`(46) ⇒ `null` —— 原版这条没有「关键词正文」通道（⛔ 别造一个 `talent` 关键词）");
        Check(RuleCore.KeywordFromTriggerName("TurnStart"), null, "F4 `TurnStart`(30) ⇒ `null`");
        Check(RuleCore.KeywordFromTriggerName("TurnStartPlayer"), null, "F5 `TurnStartPlayer`(31) ⇒ `null`");
        Check(RuleCore.KeywordFromTriggerName("UnitDamaged"), null, "F6 `UnitDamaged`(60) ⇒ `null`");
        Check(RuleCore.KeywordFromTriggerName("UnitDamagedByAbility"), null, "F7 `UnitDamagedByAbility`(185) ⇒ `null`");
        Check(RuleCore.KeywordFromTriggerName("UnitDamagedByAttack"), null, "F8 `UnitDamagedByAttack`(187) ⇒ `null`");
        Check(RuleCore.KeywordFromTriggerName("CardCreatedInHand"), null, "F9 `CardCreatedInHand`(210) ⇒ `null`");
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
        if (_fail == 0) Debug.Log($"B141_W86 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B141_W86 合计：{_pass} 通过 / {_fail} 失败");
    }

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
