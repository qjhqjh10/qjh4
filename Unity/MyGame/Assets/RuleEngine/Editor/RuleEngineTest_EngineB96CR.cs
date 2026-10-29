// RuleEngineTest_EngineB96CR.cs — 第九十六会话 · 执行代理 `B96_CR`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆）。
// 本件只放本批新增判据的断言；宿主 `RuleEngineTest.cs` 里只需要加一个
//   `{ CheckB96CRCombatBits(); }` 块（⚠️ 方法名**不用 `Test`/`Report` 打头**，
//   也**不进 8 空格缩进的顶层语句** ⇒ 不会被 `VerifyRunCaseCoverage` / `gen_diag_runner` 认成用例）。
//   🔴 **本代理不动宿主**（简报红线）⇒ 这一个 `{ … }` 块要由调度台补，否则本件**一条都不会跑**。
//
// 本批覆盖（3 条能直接断的；另 4 条只有状态层、无公开入口，见文件末「没覆盖」）：
//   · 账 1053 `EntityScript.RevealedSecret` —— `Enchantments.RevealedSecret` + `ActiveCardEffect.SecretBuff/QuestBuff`
//     判据 = `decomp_full/EntityScript__RevealedSecret.c`（全文 78 行）·
//            `decomp_full/CardScript__AddEffect.c:853-855`（60 ⇒ `+0x90 = 1`）/ `:886-889`（120 ⇒ `+0x91 = 1` + 摘别的任务）
//   · 账 876 `CardScript.ResolveUnitSummoned` 的 550 那一跳 —— `RuleCore.BroadcastSupportOnSummon` 的空转/空实参
//     判据 = `decomp_full/CardScript__ResolveUnitSummoned.c:46-60`
//   · 账 1217 `RawCardScript.GetAbilitiesForTriggerRawcard` —— 逐条闸（`ActingLevelCriteriaAllowsByAbility`）的三态
//     判据 = `decomp_full/CardAbility__CanTriggerAbility.c:127-140`
//
// 🔴 每条都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
// 🔴 **先断前提、再断结论**（`if (x != null) Check(...)` 那种「前提不成立就静默跳过」的写法一律不用）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB96CRCombatBits()
    {
        Section("🆕 B96_CR：账 1053 密令/任务「已揭示」两位 + 账 876 的 550 空转 + 账 1217 逐条闸的三态");
        B96CR1SecretRevealed();
        B96CR2QuestFlagAndDedup();
        B96CR3SupportOnSummonNoBody();
        B96CR4PerAbilityGateUnknown();
    }

    // ==================================================================
    //  T1 · 账 1053：`spellType 60` 的附魔挂上 ⇒ `SecretBuff` 真；`RevealedSecret` 把它置 0
    //  判据 = `CardScript__AddEffect.c:853-855` · `EntityScript__RevealedSecret.c`
    // ==================================================================
    static void B96CR1SecretRevealed()
    {
        var carrier = Unit("B96CRCarrier1", 1, 1, 5);
        var ctx = Battle(new[] { carrier }, new[] { Unit("B96CRFoe1", 1, 1, 5) });
        var u = Place(ctx, 0, 1, carrier);
        var ench = new CardDef("B96CRSecret1", "B96CRSecret1", "enchant", "", null, "Test",
                               0, 0, 0, 0, null, spellType: Enchantments.SpellTypeSecretOrder);

        var eff = Enchantments.Attach(ctx, u.Instance, ench, Enchantments.BuffTypeEnchantment,
                                      Enchantments.SpellTypeSecretOrder);
        Check(eff != null, true, "前提：密令附魔挂上了（不属于三档 buffType 之一会被拒 → 这条会先红）");
        if (eff == null) return;                                   // 前提不成立 ⇒ 后面两条断言的期望没有对象
        Check(eff.SecretBuff, true, "🧨 `spellType == 60(SecretOrder)` ⇒ `secretBuff` **置真**（`AddEffect.c:853-855`）");
        Check(Enchantments.CountSecretOrders(u.Instance), 1, "前提：载体身上数得到 1 条密令（`HasSecretOrders` 判据）");
        Check(Enchantments.RevealedSecret(ctx, u.Instance, ench), true,
              "🧨 揭示那一下 ⇒ **返回真**（`RevealedSecret.c` 找到并清掉那一条）");
        Check(eff.SecretBuff, false, "🧨 揭示后 `secretBuff` **置 0**（原版 `*(char*)(effect+0x90) = 0`）");
        Check(Enchantments.RevealedSecret(ctx, u.Instance, ench), false,
              "🧨 再揭示一次 ⇒ **假**（已经 0；若恒真说明那句 `if (!e.SecretBuff) continue` 被删了）");
        // 反例（灭自证）：换成一张**不是它**的附魔卡 ⇒ 找不到、不误置
        var other = new CardDef("B96CRSecret2", "B96CRSecret2", "enchant", "", null, "Test",
                                0, 0, 0, 0, null, spellType: Enchantments.SpellTypeSecretOrder);
        Check(Enchantments.RevealedSecret(ctx, u.Instance, other), false,
              "🧨 拿**另一张**密令卡去揭示 ⇒ 假（`effect.enchantment == param_2` 那道身份闸）");
    }

    // ==================================================================
    //  T2 · 账 1053 的另一半：`spellType 120(Quest)` ⇒ `QuestBuff` 真，且**同时只留一条**任务
    //  判据 = `CardScript__AddEffect.c:886-889`（倒序摘别的任务 + `+0x91 = 1`）
    // ==================================================================
    static void B96CR2QuestFlagAndDedup()
    {
        var carrier = Unit("B96CRCarrier2", 1, 1, 5);
        var ctx = Battle(new[] { carrier }, new[] { Unit("B96CRFoe2", 1, 1, 5) });
        var u = Place(ctx, 0, 1, carrier);
        var q1 = new CardDef("B96CRQuestA", "B96CRQuestA", "enchant", "", null, "Test",
                             0, 0, 0, 0, null, spellType: Enchantments.SpellTypeQuest);
        var q2 = new CardDef("B96CRQuestB", "B96CRQuestB", "enchant", "", null, "Test",
                             0, 0, 0, 0, null, spellType: Enchantments.SpellTypeQuest);

        var e1 = Enchantments.Attach(ctx, u.Instance, q1, Enchantments.BuffTypeEnchantment, Enchantments.SpellTypeQuest);
        Check(e1 != null, true, "前提：第 1 条任务附魔挂上了");
        if (e1 == null) return;
        Check(e1.QuestBuff, true, "🧨 `spellType == 120(Quest)` ⇒ `questBuff` **置真**（`AddEffect.c:886-889`）");

        var e2 = Enchantments.Attach(ctx, u.Instance, q2, Enchantments.BuffTypeEnchantment, Enchantments.SpellTypeQuest);
        Check(e2 != null, true, "前提：第 2 条任务附魔挂上了");
        if (e2 == null) return;
        Check(Enchantments.CountQuests(u.Instance), 1,
              "🧨 挂第 2 条时**旧的那条被摘掉** ⇒ 只剩 1 条任务（`AddEffect.c:886-889` 的倒序 RemoveEffect）");
        Check(u.Instance.ActiveEffects.Contains(e1), false, "🧨 旧那条的 `CardEffect` 也被摘了（两条 list 一起摘）");
        Check(e2.QuestBuff, true, "🧨 新那条仍是 `questBuff` 真");
    }

    // ==================================================================
    //  T3 · 账 876：550 那一跳的**空转**与**空实参**
    //  判据 = `CardScript__ResolveUnitSummoned.c:46-60`（接收者要「同方 ∧ 不是刚落地那张 ∧ 有 550 正文」）
    // ==================================================================
    static void B96CR3SupportOnSummonNoBody()
    {
        var summon = Unit("B96CRSummoned", 1, 1, 3);
        var ctx = Battle(new[] { Unit("B96CRCarrier3", 1, 1, 5) }, new[] { Unit("B96CRFoe3", 1, 1, 5) });
        var su = Place(ctx, 0, 1, summon);
        Check(RuleCore.BroadcastSupportOnSummon(ctx, 0, su), 0,
              "🧨 同方没有一张带 `Support(550)` 正文的卡 ⇒ **一条都不触发**（原版 `OnTrigger(550)` 空转）");
        Check(RuleCore.BroadcastSupportOnSummon(ctx, 0, null), 0, "🧨 空实参 ⇒ 0（⛔ 不抛异常）");
        Check(RuleCore.BroadcastSupportOnSummon(null, 0, su), 0, "🧨 `ctx == null` ⇒ 0（⛔ 不抛异常）");
    }

    // ==================================================================
    //  T4 · 账 1217：逐条闸的三态 —— **列没落 ⇒ 判不了（null）**，⛔ 不是「不满足」（false）
    //  判据 = `CardAbility__CanTriggerAbility.c:127-140`（三态是调用点契约，不是「真/假」两档）
    // ==================================================================
    static void B96CR4PerAbilityGateUnknown()
    {
        var plain = Unit("B96CRPlain", 1, 1, 5);   // 手写夹具：`abilityTargets` 列没落 ⇒ `AbilityRowsForTrigger` 回 null
        string why;
        bool? r = RuleCore.ActingLevelCriteriaAllowsByAbility(null, 0, null, plain, "UnitAttack", null, null, out why);
        Check(r == null, true, "🧨 `abilityTargets` 列没落 ⇒ **判不了（null）**（⛔ 不许返回 false —— 那是「明确不满足」）");
        Check(string.IsNullOrEmpty(why), false, "🧨 判不了必须带原因（本工程红线：不许静默失败）");

        bool? b = RuleCore.BoardLevelCriteriaAllowsByAbility(null, 0, null, plain, "UnitAttack", out why);
        Check(b == null, true, "🧨 board 闸同一口径：列没落 ⇒ 判不了（null）");
    }
}
