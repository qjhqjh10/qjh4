// RuleEngineTest_EngineP3.cs — 引擎线 `E8`：`abilityContext`「缺行 = `Play(0)`」的**两个前提**
// （2026-10-24 第七十三会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineA1` /
// `_EngineB3` / `_Priority` … 都在），而**同一时刻只有一个写手改一个文件** ⇒ 本批测试体写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestEngineP3)`**。
//
// 判据（⛔ 不要重新论证）：
//   ① 闸的位置 =**入队前**：`CardAbility__CanTriggerAbility` 由 `RawCardScript.TriggerAbilities`
//      在广播那一刻同步调（`RawCardScript__TriggerAbilities.c:108,207`）；
//      `CardAbility__IsRightContext` 全库只有它一个调用者 ⇒ 原版**没有出队那一道**。
//      `QueuedAction.RequireActorInPlay` 只是那道闸的**出队旧近似**（`ActionPump.cs:775-776`）。
//   ② 六档取值 = `CardAbility__IsRightContext.c` 逐句；`0 Play` 的三闸见 `:20-32`。
//   ③ **「缺行 = `Play(0)`」的两个前提**（判据 `资料/普查产出_1024c/A2_引擎战斗判据总表.md` Q3 §4）：
//      ① 卡表 `version ≥ 14`（`CardDef.AbilityContextsVersioned`）；
//      ② 该 trigger **出现在这张卡的 `abilityTriggers` 里**（`CardDef.AbilityTriggerPresence == 1`，v17 列）。
//      理由：生成器**只落 `context != 0` 的行**（`gen_cards_engine.py` 的 `ABILITY_CONTEXT_DEFAULT = 0`），
//      而真值快照覆盖全池 ⇒「卡在表里 + trigger 在列 + 无非 0 行」= 原版就是 `0`。
//      ⛔ 少了 ② 就是「给一条**不存在**的 ability 编 `Play(0)` 口径」。
//   ④ skip 表（这几族**整段不读** context，`CardAbility__CanTriggerAbility.c:74-83`）：
//      trigger `0 ThisCardPlayed` / `80 ThisUnitDeath` / `145 Codex` / `220 Sacrifice` /
//      `500 TriggerEnchantment`。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量（`0` / `-1` / `40` / `50`），⛔ 不从实现常量反算（那会自证）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestEngineP3)`）。
    /// 五个子用例是 `P3…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestEngineP3()
    {
        Section("🆕 引擎线 E8：`abilityContext` 缺行 = `Play(0)` 的前提"
              + "（v14 列 ∧ `abilityTriggers` 真含这条 trigger）· skip 表 · 合成夹具回退");
        P3MissingRowIsPlayWhenTriggerPresent();
        P3MissingRowIsNotPlayWhenTriggerAbsent();
        P3ExistingRowUnchanged();
        P3SkipTableSegments();
        P3SyntheticFixtureFallsBack();
    }

    // ==============================================================================================
    //  ① 「缺列 ⇒ 按 `Play(0)`」—— **这条 trigger 真的在这张卡的 `abilityTriggers` 里**
    // ==============================================================================================

    /// <summary>
    /// 原版判据：`Play(0)` = `IsInPlay ∧ !EnoughPendingDamageToDie ∧ !isRemnant`
    /// （`CardAbility__IsRightContext.c:20-32`）。数据侧：v14 只落非 0 行 ⇒ **真有这条 ability
    /// 而查不到行** = 那一行是**省略掉的 `0`**（Q3 §4 前提②成立）。
    /// </summary>
    static void P3MissingRowIsPlayWhenTriggerPresent()
    {
        var pool = CardDatabase.Load();
        CardDef slay = null;
        foreach (var c in pool)
            if (c != null && c.AbilityTriggerPresence("Slay") == 1
                && RuleCore.AbilityContextOfAbility(c, "Slay", "slay") == CardDef.AbilityContextUnknown)
            { slay = c; break; }
        CheckTrue(slay != null,
                  "⑨ （前提）池内至少有一张卡：`abilityTriggers` 里**有** `Slay(120)`，"
                + "而 `abilityContext` 里**没有**它的非 0 行（v14 只落非 0 ⇒ 那一行是省略的 `0`）");
        if (slay == null) return;

        Check(slay.AbilityTriggerPresence("Slay"), 1,
              "⑨ （前提）`" + slay.Name + "` 的 v17 `abilityTriggers` 明确**有** `Slay(120)`");
        Check(RuleCore.AbilityContextOfAbility(slay, "Slay", "slay"), -1,
              "⑨ 原始访问器查不到 `Slay` 行 ⇒ `-1`（= 「这一行没落」这个事实本身）"
            + "｜🧨 把生成器的「只落非 0」改成「0 也落」⇒ 实得有行 ⇒ 本条的 `-1` 不成立");
        Check(RuleCore.AbilityContextOfAbilityV14(slay, "Slay", "slay"), 0,
              "★★ ⑨ **真有这条 ability + 缺行 ⇒ `Play(0)`**（原版 `AbilityContext.Play = 0`）"
            + "｜🧨 删掉 `AbilityContextOfAbilityV14` 的「缺行 = Play」那一跳 ⇒ 实得 `-1` ⇒ 红");
        CheckTrue(RuleCore.ActorMustBeInPlayForTrigger(slay, "Slay", "slay"),
                  "★★ ⑨ 消费端同口径：`ActorMustBeInPlayForTrigger` = **要判在场**（true）"
                + "｜🧨 让它改调**未补 0**的 `AbilityContextOfAbility` ⇒ 落到旧 5 词表"
                + "（`slay` 恰好在表里，实得仍 true —— 所以本条**只钉供值，不单独钉前提②**；"
                + "前提② 由 ⑩ 钉）");
    }

    // ==============================================================================================
    //  ② 「非缺列」的**第二种形状**：这张卡**没有**这条 trigger 的 ability ⇒ ⛔ 不许推 `Play(0)`
    //     （= 本次收口：改前是「versioned ⇒ 一律 Play」，会给不存在的 ability 编口径）
    // ==============================================================================================

    /// <summary>
    /// 灭自证的**主判别式**：同一张卡上，`Slay`（在旧 5 词表）与 `Strike`（不在）**两条都不真**。
    /// 改前两者都被读成 `Play(0)`（= 与「真值行」无关地一律 Play）；改后必须回 `-1`，
    /// 于是只有旧 5 词表那一支还能把 `Slay` 抬成 true —— 而 `Strike` 必须落 false。
    /// </summary>
    static void P3MissingRowIsNotPlayWhenTriggerAbsent()
    {
        var pool = CardDatabase.Load();
        CardDef none = null;
        foreach (var c in pool)
            if (c != null && c.AbilityTriggerPresence("Slay") == 0
                && c.AbilityTriggerPresence("Strike") == 0
                && c.AbilityTriggerPresence("Artifice") == 0
                && RuleCore.AbilityContextOfAbility(c, "Slay", "slay") == CardDef.AbilityContextUnknown
                && RuleCore.AbilityContextOfAbility(c, "Strike", "strike") == CardDef.AbilityContextUnknown)
            { none = c; break; }
        CheckTrue(none != null,
                  "⑩ （前提）池内至少有一张卡：`abilityTriggers` **明确没有** `Slay` / `Strike` / `Artifice`"
                + "（`presence == 0`），且 `abilityContext` 也没有对应行");
        if (none == null) return;

        Check(RuleCore.AbilityContextOfAbility(none, "Strike", "strike"), -1,
              "⑩ （前提）`" + none.Name + "` 的 `Strike` 原始访问器 = `-1`（没有这一行）");
        Check(RuleCore.AbilityContextOfAbilityV14(none, "Strike", "strike"), -1,
              "★★ ⑩ **没有这条 ability ⇒ `-1`（未知）**，⛔ **不是 `Play(0)`**"
            + "（「缺行 = 0」的前提②不成立：那一行本该不存在，不是被省略）"
            + "｜🧨 把前提② 的 `AbilityTriggerPresence(tn) == 1` 删掉 / 写成 `!= 0` / 写成 `!= -1`"
            + " ⇒ 退回「versioned ⇒ 一律 Play」⇒ 实得 `0` ⇒ 红");
        CheckTrue(!RuleCore.ActorMustBeInPlayForTrigger(none, "Strike", "strike"),
                  "★★ ⑩ 消费端：`strike` 既不在 v14 行里、也不在旧 5 词表"
                + "（`rally/mob/slay/artifice/abouttoattack`）⇒ **不判在场**（false）"
                + "｜🧨 同上：把前提② 删掉 ⇒ 实得 true ⇒ 红（= 给一条不存在的 ability 加了出队复核）");

        // 同一张卡上的对照：`Slay` 在旧 5 词表里 ⇒ `-1` 回退后**仍然是 true**
        // —— 这一条不钉前提②（旧表本来就判），钉的是「回退支不许被顺手删掉」。
        CheckTrue(RuleCore.ActorMustBeInPlayForTrigger(none, "Slay", "slay"),
                  "⑩ 对照：同一张卡的 `slay` 走**旧关键词表**回退 ⇒ 仍然 true"
                + "（= 改前口径逐字不变；⛔ 不是 `Play(0)`，因为上面那条已经钉住 V14 = `-1`）"
                + "｜🧨 把 `LegacyActorMustBeInPlayForKeyword` 删掉 ⇒ 旧卡表 / 合成夹具整片放松 ⇒ 本条红");

        // ---- 结构性：全池 `Slay` 的「读成 Play」⇔「真有 Slay ability」两列必须一致 ----
        int nPlay = 0, nPresence = 0, nViolation = 0;
        foreach (var c in pool)
        {
            if (c == null) continue;
            if (RuleCore.AbilityContextOfAbility(c, "Slay", "slay") != CardDef.AbilityContextUnknown)
                continue;                            // 有非 0 真值行 ⇒ 不参与本判别式（另有断言钉它）
            bool play = RuleCore.AbilityContextOfAbilityV14(c, "Slay", "slay") == 0;
            bool pres = c.AbilityTriggerPresence("Slay") == 1;
            if (play) nPlay++;
            if (pres) nPresence++;
            if (play != pres) nViolation++;
        }
        Check(nViolation, 0,
              $"★★ ⑩ 全池 {pool.Count} 张：`Slay` 读成 `Play(0)` **⇔** 这张卡真的有 `Slay` ability"
            + $"（现读 `Play` {nPlay} 张 / `presence == 1` {nPresence} 张；两列是**独立数据**，必须一致）"
            + "｜🧨 退回「versioned ⇒ 一律 Play」（= 改前口径）⇒ `Play` = 全池、`presence` = 十几张 ⇒ 红");
        CheckTrue(nPlay > 0 && nPlay < pool.Count,
                  "⑩ 上一条**非平凡**：`Play` 的既不是 0 张也不是全池"
                + "（🧨 把「缺行 = Play」那一跳删掉 ⇒ 0 张 ⇒ 红；🧨 退回旧口径 ⇒ 全池 ⇒ 红）");
    }

    // ==============================================================================================
    //  ③ 「非缺列」的**第一种形状**：有真值行 ⇒ **照旧按那一行**（40 / 50 都不许被当成 `Play`）
    // ==============================================================================================

    /// <summary>
    /// 判据 = `CardAbility__IsRightContext.c:52-55`（40 `InPlayOrDying`，含待死）与
    /// `:66-72`（50 `InPlayOrCemetery`，**不含待死**）—— 两档都**不是** `Play(0)`。
    /// 这条钉的是：补前提② 时**不许**顺手把「有真值行」的那批也按 Play 判
    /// （`W63 #2` 那一族：`Rage` / `Unstable` / `Regiment` 靠 40 工作）。
    /// </summary>
    static void P3ExistingRowUnchanged()
    {
        var pool = CardDatabase.Load();

        CardDef strike40 = null, stone50 = null;
        foreach (var c in pool)
        {
            if (c == null) continue;
            if (strike40 == null
                && RuleCore.AbilityContextOfAbility(c, "Strike", "strike") == 40)
                strike40 = c;
            if (stone50 == null
                && RuleCore.AbilityContextOfAbility(c, "UseSpiritStone", "spiritstone") == 50)
                stone50 = c;
        }

        CheckTrue(strike40 != null,
                  "⑪ （前提）池内至少有一张 `Strike(580)` 的 `abilityContext = 40`"
                + "（`InPlayOrDying`，含待死 —— 判据 `CardAbility__IsRightContext.c:52-55`）");
        if (strike40 != null)
        {
            Check(strike40.AbilityTriggerPresence("Strike"), 1,
                  "⑪ （前提）`" + strike40.Name + "` 的 v17 `abilityTriggers` 也有 `Strike(580)`"
                + "（两列一致 ⇒ 这一行不是坏数据）");
            Check(RuleCore.AbilityContextOfAbilityV14(strike40, "Strike", "strike"), 40,
                  "★★ ⑪ 有真值行 ⇒ **原样返回 `40`**，⛔ 不改成 `0`"
                + "｜🧨 把前提② 那一段写成「versioned ∧ presence==1 ⇒ 一律 Play」"
                + "（= 先补 0 再查行）⇒ 实得 `0` ⇒ 红");
            CheckTrue(!RuleCore.ActorMustBeInPlayForTrigger(strike40, "Strike", "strike"),
                      "★★ ⑪ 40 **不等于** `Play(0)` ⇒ 出队旧近似**不能**按「必须活着在场」判（false）"
                    + "｜🧨 把 40 读成 Play ⇒ 待死载体的 Strike 正文被掐 ⇒ 实得 true ⇒ 红"
                    + "（同一族回归口 = `TestW61T27…` ⑩ 与 `RuleEngineTest.W63` 那批）");
        }

        CheckTrue(stone50 != null,
                  "⑪ （前提）池内至少有一张 `UseSpiritStone(600)` 的 `abilityContext = 50`"
                + "（`InPlayOrCemetery`，**不含待死** —— `IsRightContext.c:66-72`）");
        if (stone50 != null)
        {
            Check(RuleCore.AbilityContextOfAbilityV14(stone50, "UseSpiritStone", "spiritstone"), 50,
                  "★★ ⑪ `600 UseSpiritStone` 的 50 **照旧**返回 50（原版在**余额判定之前**显式查它："
                + "`CanTriggerAbility.c:114-124`）"
                + "｜🧨 把 50 读成 Play ⇒ 在墓地里的灵魂石能力被掐 ⇒ 红");
            CheckTrue(!RuleCore.ActorMustBeInPlayForTrigger(stone50, "UseSpiritStone", "spiritstone"),
                      "★★ ⑪ 50 也不按「必须活着在场」判 ⇒ false"
                    + "｜🧨 同族：把 `AbilityContextOfAbilityV14` 改成「先补 0」再查行 ⇒ true ⇒ 红");
        }
    }

    // ==============================================================================================
    //  ④ skip 表：这几族**整段不读** context（`CardAbility__CanTriggerAbility.c:74-83`）
    // ==============================================================================================

    /// <summary>
    /// 逐族找一张「**真有**这条 trigger、且没有非 0 行」的卡 —— 那是**最强**的反例：
    /// 少了 skip 表，它们会被读成 `Play(0)` ⇒ 出队复核 = true（把阵亡 / 献祭族的正文掐掉）。
    /// ⚠️ `220 Sacrifice` 现读**池内 0 张**卡有这条 trigger ⇒ 只能验「按名字/关键词放行」这一半，
    /// 验不到「没有 skip 会变红」那一半（报告里如实记着）。
    /// </summary>
    static void P3SkipTableSegments()
    {
        var pool = CardDatabase.Load();

        P3SkipOne(pool, "ThisCardPlayed", null, "0 `ThisCardPlayed`");
        P3SkipOne(pool, "ThisUnitDeath", null, "80 `ThisUnitDeath`（按 trigger 名）");
        P3SkipOne(pool, "ThisUnitDeath", KeywordTable.Backlash, "80 `ThisUnitDeath`（按关键词 `backlash`）");
        P3SkipOne(pool, "Codex", null, "145 `Codex`（按 trigger 名）");
        P3SkipOne(pool, "Codex", KeywordTable.Codex, "145 `Codex`（按关键词 `codex`）");
        P3SkipOne(pool, "TriggerEnchantment", null, "500 `TriggerEnchantment`");
        P3SkipOne(pool, "Sacrifice", KeywordTable.Sacrifice,
                  "220 `Sacrifice`（⚠️ 池内 0 张卡有这条 trigger ⇒ 只用合成卡钉「放行」这一半）");
    }

    /// <summary>skip 表的单族断言。`keyword == null` ⇒ 只按 trigger 名那半张表判。</summary>
    static void P3SkipOne(List<CardDef> pool, string triggerName, string keyword, string label)
    {
        CardDef card = null;
        foreach (var c in pool)
            if (c != null && c.AbilityTriggerPresence(triggerName) == 1
                && RuleCore.AbilityContextOfAbility(c, triggerName, keyword) == CardDef.AbilityContextUnknown)
            { card = c; break; }
        if (card == null)
        {
            // 池内没有真卡 ⇒ 退回合成卡：钉「skip 表按名字/关键词放行」这一半（⛔ 不静默跳过）。
            var fixture = Unit("P3SkipFixture", 1, 1, 3);
            CheckTrue(!RuleCore.ActorMustBeInPlayForTrigger(fixture, triggerName, keyword),
                      "⑫ skip 表 · " + label + " ⇒ **整段不判**（false；合成卡上验「放行」）"
                    + "｜🧨 把 `TriggerSkipsAbilityContext` 里这一条删掉且给合成卡 v14 列 ⇒ 实得 Play");
            return;
        }
        Check(card.AbilityTriggerPresence(triggerName), 1,
              "⑫ （前提）`" + card.Name + "` 真有 " + label + " 这条 ability");
        CheckTrue(!RuleCore.ActorMustBeInPlayForTrigger(card, triggerName, keyword),
                  "★★ ⑫ skip 表 · " + label + " ⇒ **整段不判** `abilityContext`（false）"
                + "｜🧨 把 `TriggerSkipsAbilityContext` 里这一族删掉 ⇒ 这张卡（真有 ability + 缺行）"
                + "被读成 `Play(0)` ⇒ 实得 true ⇒ 红");
    }

    // ==============================================================================================
    //  ⑤ 合成夹具卡（无 v14 / v17 列）⇒ `-1` ⇒ 旧口径回退，**逐字同改前**
    // ==============================================================================================

    /// <summary>
    /// 这是「本次收口**不**改变夹具行为」的钉：合成卡 `AbilityContextsVersioned = false`
    /// （`CardDef.cs:928` 的形参默认 `false`）⇒ 前提① 不成立 ⇒ `-1` ⇒ 旧 5 词表。
    /// ⛔ 别把「`-1` 未知」读成 `Play(0)`（那会让所有夹具卡突然要求「必须活着在场」）。
    /// </summary>
    static void P3SyntheticFixtureFallsBack()
    {
        var fixture = Unit("P3Fixture", 1, 1, 3);
        Check(fixture.AbilityTriggerPresence("Slay"), -1,
              "⑬ （前提）合成夹具卡没有 v17 `abilityTriggers` 列 ⇒ `presence = -1`（未知）");
        Check(RuleCore.AbilityContextOfAbilityV14(fixture, "Slay", "slay"), -1,
              "★★ ⑬ 合成夹具卡 ⇒ `-1`（前提① 不成立：没有 v14 列）"
            + "｜🧨 把前提② 写成 `presence != -1` 之类、或让 `-1` 也走「缺行 = Play」⇒ 实得 `0` ⇒ 红");
        CheckTrue(RuleCore.ActorMustBeInPlayForTrigger(fixture, "Slay", "slay"),
                  "⑬ 合成夹具卡 ⇒ 回退旧关键词表（`slay` 在 5 词表）⇒ 仍判 true（**改前改后逐字相同**）"
                + "｜🧨 把 `ActorMustBeInPlayForTrigger` 的 `-1` 回退支删掉 ⇒ 实得 false ⇒ 红");
        CheckTrue(!RuleCore.ActorMustBeInPlayForTrigger(fixture, "Strike", "strike"),
                  "⑬ 同上：`strike` 不在旧 5 词表 ⇒ false（= **旧口径回退**，不是 `Play(0)`）"
                + "｜🧨 把「`-1` ⇒ Play(0)」写进 V14 ⇒ 实得 true ⇒ 红");
    }
}
