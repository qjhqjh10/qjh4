// RuleEngineTest_B116G2.cs — 执行写手 `B116_W9`（2026-11-02 · 第一百一十六会话）
//
// ============================ 这件活是什么 ============================
// **ENG-02**：「多 ability 原版 +N」—— 一件任务附魔在这一拍派发了几条同 trigger 的 ability，
// `questCounter` 就加几（⛔ 不是「每件附魔 +1」）。实现早已落地、**断言 0 条**，本文件补上。
//
// ============================ 判据（⛔ 不是我挑的）============================
//   · 实现：`Core/Enchantment.cs:520`
//       `int __qStep = AbilityCountForTrigger(e.Card, keyword); … QuestCounter += __qStep;`
//     条数判定 = 同文件类尾 `AbilityCountForTrigger`（`:2097` 起）：
//       过 `RuleCore.TriggerNameOfKeyword` → `CardDef.AbilityTriggerNumber` → 只数
//       `AbilityTargets[i].Trigger == trigger` 的行；三档保守回退 1
//       （① `!AbilityTargetsVersioned` ② 一条都对不上 ③ 关键词认不出）。
//   · 原版：`d:/2/tools/decomp_full/RawCardScript__TriggerAbilities.c:115`
//     —— 调用点在内层 ability 循环（`:71-190` 逐条 `cardAbilities(+0x290)`）里
//     ⇒ 一件附魔有 N 条同 trigger 的 ability 就累 **N**。
//
// ============================ 🔴 灭自证（本组为什么这样写）============================
//   本组三条**结构上不可能同时**被下面两种错实现满足：
//     · 「恒 +1」（改回 `QuestCounter += 1`）⇒ A 实得 1 ⇒ **A 红**；
//     · 「恒等于 `AbilityTargets.Count`」（把 trigger 那道筛删掉）⇒ C 实得 2 ⇒ **C 红**；
//     · 「恒加一个固定大数」⇒ B 实得 ≥2 ≠ 1 ⇒ **B 红**。
//   ⇒ A（2 条同 trigger ⇒ +2）· B（1 条 ⇒ +1）· C（2 条、只有 1 条对得上 ⇒ +1）三者互为对照。
//
// ============================ 宿主接线 ============================
//   `RuleEngineTest.cs` 的 `Run()` 里追加 `Guard("CheckB116G2MultiAbilityQuest", …)`。
//   ⚠️ 入口名**不以 `Test` / `Report` 打头** —— `VerifyRunCaseCoverage` / `gen_diag_runner` 按那两个前缀认用例。
//
// ============================ 数据前提（本组最容易假红的地方）============================
//   🔴 `AbilityTargetsVersioned` 与 `AbilityTriggersVersioned` **共用一个版本位**（`CardDef.cs:1120`）
//   ⇒ 夹具必须显式给 `abilityTriggersVersioned: true`，否则 `AbilityCountForTrigger` 走回退 1
//      ⇒ A 会**静默**变成 1（看着像实现错、其实是夹具没给数据）。
//   本文件把它写成**硬前提断言**（P1/P2），⛔ 不用 `if (前提) Check(...)` 那种「前提不成立就静默跳过」。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（`Run()` 里一行 `Guard(...)`）。</summary>
    public static void CheckB116G2MultiAbilityQuest()
    {
        Section("🆕 B116_W9 · ENG-02：一件任务附魔在一拍里派发 N 条 ability ⇒ `questCounter += N`");

        // `slay` → `RuleCore.TriggerNameOfKeyword` → `"Slay"` → `CardDef.AbilityTriggerNumber` = 120
        const string kw = KeywordTable.Slay;
        const int trigSlay = 120;
        const int trigCodex = 145;

        // ---- A：2 条同 trigger ----
        var q2 = QuestEnchFixture("B116G2Q2", kw, trigSlay, trigSlay);
        // ---- B：1 条（灭自证：任何「固定 +2」的实现过不了它）----
        var q1 = QuestEnchFixture("B116G2Q1", kw, trigSlay);
        // ---- C：2 条、但只有 1 条对得上（灭自证：删掉 trigger 那道筛的实现过不了它）----
        var qmix = QuestEnchFixture("B116G2QMix", kw, trigSlay, trigCodex);

        // ---- 前提（硬断言，先断前提再断结论）----
        CheckTrue(q2.AbilityTargetsVersioned && q2.AbilityTargets.Count == 2,
                  "P1 前提：A 的 `abilityTargets` 列**已 versioned** 且有 2 行"
                  + "（不给 `abilityTriggersVersioned: true` ⇒ 该列静默是 false ⇒ 本段后面的期望值全部没有意义，`CardDef.cs:1120`）");
        CheckTrue(q2.CardType == CardDef.CardTypeWhispers && q2.SpellType == Enchantments.SpellTypeQuest
                  && q2.QuestTimes == 2,
                  "P2 前提：任务附魔的三道数据闸齐（`cardType == 40 Whispers` ∧ `spellType == 120 Quest` ∧ `questTimes > 1`）"
                  + " —— 少任何一道 `FilterByQuestCounter` 都会原样放行、计数器一步都不动");
        CheckTrue(qmix.AbilityTargetsVersioned && qmix.AbilityTargets.Count == 2,
                  "P3 前提：C 也是 2 行（一行 `Slay`、一行 `Codex`）");

        // ---- A：2 条同 trigger ⇒ 计数 +2 ----
        int stA = RunQuestOnce(q2, kw, out int keptA);
        Check(stA, 2,
              "★ A 一件附魔有 **2 条** 同 trigger 的 ability ⇒ 触发一次 `QuestCounter == 2`（原版 `.c:115` 在逐条循环里）"
              + "｜🧨 改回 `QuestCounter += 1` ⇒ 实得 1 ⇒ 红");
        Check(keptA, 1,
              "★ A2 计数到 2 ≥ `questTimes 2` ⇒ 这一拍**放行**（返回值里留 1 条）"
              + "｜🧨 计数器不累加（恒 0/1）⇒ 被摘掉 ⇒ 实得 0 ⇒ 红");

        // ---- B：1 条 ⇒ 计数 +1（灭自证）----
        int stB = RunQuestOnce(q1, kw, out int keptB);
        Check(stB, 1,
              "★★ B 同卡只有 **1 条** ⇒ `QuestCounter == 1`"
              + "｜🧨 与 A **结构上不可能同时满足**：任何「恒 +2 / 恒等于行数 +1」的实现 ⇒ 这里实得 ≠ 1 ⇒ 红");
        Check(keptB, 0,
              "★ B2 计数 1 < `questTimes 2` ⇒ 这一拍**不放行**（返回值里 0 条；原版 `ReachedQuestCounter.c:11-14` 返回 0）");

        // ---- C：2 条但只有 1 条对得上 ⇒ +1（灭自证）----
        int stC = RunQuestOnce(qmix, kw, out int _);
        Check(stC, 1,
              "★★ C 2 行里只有 1 行 `Trigger == 120`（另一行 145 `Codex`）⇒ `QuestCounter == 1`"
              + "｜🧨 把 `rows[i].Trigger == trigger` 那道筛删掉（改成数行数）⇒ 实得 2 ⇒ 红");
    }

    /// <summary>
    /// 造一张「任务附魔」夹具卡：`cardType = 40 Whispers` ∧ `spellType = 120 Quest` ∧ `questTimes = 2`，
    /// `abilityTargets` 逐行给 `Trigger`（**顺序即语义**，一行一条 ability）。
    /// </summary>
    static CardDef QuestEnchFixture(string id, string kw, params int[] triggers)
    {
        var rows = new List<CardDef.AbilityTargetSpec>();
        foreach (int t in triggers) rows.Add(new CardDef.AbilityTargetSpec { Trigger = t });
        var card = new CardDef(id, id, "enchant", "", null, "Test", 0, 0, 0, 0, null,
                               spellType: Enchantments.SpellTypeQuest,
                               questTimes: 2,
                               abilityTriggers: triggers,             // 与 `abilityTargets` 同进同出（v17 那一列）
                               abilityTriggersVersioned: true,        // 🔴 共用版本位 ⇒ `AbilityTargetsVersioned` 也跟着真
                               abilityTargets: rows);
        card.AttachCardType(CardDef.CardTypeWhispers);
        return card;
    }

    /// <summary>
    /// 一局干净夹具：载体一张上场 → 挂这张任务附魔 → 调 `Enchantments.FilterByQuestCounter` 一次。
    /// 返回**这一拍之后的 `QuestCounter`**，`kept` = 放行后剩下的附魔条数（0/1）。
    /// </summary>
    static int RunQuestOnce(CardDef quest, string kw, out int kept)
    {
        kept = -1;
        var carrierDef = Unit(quest.Id + "_carrier", 1, 1, 5);
        var ctx = Battle(new[] { carrierDef }, new[] { Unit(quest.Id + "_foe", 1, 1, 5) });
        var u = Place(ctx, 0, 1, carrierDef);
        if (u == null || u.Instance == null) return -1;      // 调用方的 P 系列前提已经断过，这里只是防 NRE

        var eff = Enchantments.Attach(ctx, u.Instance, quest, Enchantments.BuffTypeEnchantment,
                                      Enchantments.SpellTypeQuest);
        if (eff == null || eff.Enchantment == null) return -1;   // 同上：挂在前提已经不成立时

        var matched = new List<ActiveEnchantment> { eff.Enchantment };   // `Attach` 回的是 `ActiveCardEffect`，附魔条目在 `.Enchantment`
        var after = Enchantments.FilterByQuestCounter(ctx, u, matched, kw);
        kept = after == null ? 0 : after.Count;
        return u.Instance.QuestCounter;
    }
}
