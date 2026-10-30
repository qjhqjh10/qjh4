// B100F9_Assert.cs — 第一百会话 · 执行代理 `B100_F9`（卡表 `abilityEffectTexts` 的 `handBuff` 整列读串格）
//
// ============================ 它盯的是什么 ============================
//  报告 `B100_X3.md` #32/#33 那条 β 真缺陷：`cards_engine.json` 的 38 行 `abilityEffectTexts`
//  **全都没有 `isHand` 键**（生成器 `ability_effect_texts_of` 的形状只有 `ab/lg/at/src`，
//  见 `工具/gen_cards_engine.py:7242`），而 `CardDatabase.MapAbilityEffectTexts` 原来读的是 `r.isHand`
//  ⇒ 缺键恒 `false` ⇒ 5 行 `handBuff` 全被当成 `buffEffect` ⇒ 同 `(ab,lg)` 撞键、**后一条被丢**
//  （现场日志 `b100_battle.log:103443/103459`）。
//
//  本件落地 = `RuleEngine/Data/CardDatabase.cs` 的 `MapAbilityEffectTexts` 改走 `ResolveIsHand(r, c)`
//  （判据 = 挂点名 `at`；`isHand` 键只在 `at` 缺/空时作兼容兜底）+ 类尾新增 `ResolveIsHand`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1：把 `ResolveIsHand` 改回直接读 `r.isHand` ⇒ `SAU55` 的 `(0,2,handBuff)` 那格取不到
//        ⇒ A1 的前半红（同时 A1 的后半 `(0,2,false)==""` 仍绿）—— 两半必须**分开断**。
//  · A1-b：把 `IsHand` 一律涂成 `true`（「反正 handBuff 没进来」的假修法）⇒ `(0,2,false)` 变成
//        `EffectImmortalPride` ⇒ A1 的后半红。**A1 前半与 A1-b 结构上不可能同时满足**
//        （一个要「hand 格有值」、一个要「非 hand 格没值」）。
//  · A2：为了消掉撞键而**删掉 `buffEffect` 行** ⇒ `TAU51` 行数 ≠ 5 / `(0,0,false)` 空 ⇒ A2 红。
//        （这条挡的是「撞键了就丢一条了事」的另一种假修法。）
//  · A4：`at` 判反（`buffEffect` 也成了 hand）⇒ `UM44` 的 `(0,0,false)` 空 ⇒ A4 红。
//  · B1：把 `ResolveIsHand` 的 `at == "handBuff"` 那一行删掉 ⇒ 合成夹具 5 行只剩 3 行 ⇒ B1-b 红。
//  · B2：把兼容兜底 `return r.isHand;` 改成 `return false;` ⇒ 无 `at` 的旧写法行丢格 ⇒ B2 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/BattleScene.cs:46649` 那条超长追加行里，`B99W3_Assert.Run(); …` 之后
//  **同一物理行**追加（行数零变化）：
//      B100F9_Assert.Run(); pass += B100F9_Assert.Pass; fail += B100F9_Assert.Fail;
//
// ============================ 判据 / 报告 ============================
//  · 数据形状 → `工具/gen_cards_engine.py:7242-7257`（现读 38 行 / `handBuff` 5 行：`GSC45` 1 · `SAU55` 1 · `TAU51` 3）
//  · 缺陷 → `资料/完全复刻/报告/B100_X3.md` #32 / #33
//  · 本件报告 → `资料/完全复刻/报告/B100_F9.md`
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B100_F9`：`abilityEffectTexts` 的 `handBuff` 行按 `at` 落 `IsHand`（不是 JSON 的 `isHand` 键）。</summary>
public static class B100F9_Assert
{
    static readonly CheckSink _sink = new CheckSink("[F9] ") { Near = MenuNearStyle.Compact3 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B100_F9 `abilityEffectTexts` 的 `handBuff` 整列（`at` 才是判据）");
        SectionA_RealCards();
        SectionB_MappingSynthetic();
        Debug.Log(_sink.P + "B100F9 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 段：真卡（卡表现读）
    static void SectionA_RealCards()
    {
        MenuCheck.Section(_sink, "A 真卡：`handBuff` 那 3 张（`SAU55` / `TAU51` / `GSC45`）");

        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B100F9] `CardDatabase.Load()` 抛了：" + e.Message); }
        Tr(pool != null && pool.Count > 0, "A0 **前提**：卡池读得到（读不到这一节全部无意义）");
        if (pool == null || pool.Count == 0) return;

        var sau55 = CardDatabase.FindById(pool, "SAU55");
        Tr(sau55 != null, "A0-a **前提**：池里有 `SAU55`");
        Tr(sau55 != null && sau55.HasAbilityEffectTexts, "A0-b **前提**：`SAU55` 已带 `abilityEffectTexts` 列");
        C(sau55 == null ? "" : sau55.EffectTextOf(0, 2, true), "EffectImmortalPride",
          "A1 `SAU55`（Immortal Pride）标题落在 **`handBuff`** 的 `lg=2` 那格"
        + "｜🧨 `ResolveIsHand` 改回读 `r.isHand`（缺键恒 false）⇒ 本行红（日志 103443 那个形状）");
        C(sau55 == null ? "?" : sau55.EffectTextOf(0, 2, false), "",
          "A1-b 同一格**不该**出现在 `buffEffect` 那一侧"
        + "｜🧨 把 `IsHand` 一律涂 `true`（假修）⇒ 本行红；与 A1 前半**结构上不可能同时满足**");

        var tau51 = CardDatabase.FindById(pool, "TAU51");
        Tr(tau51 != null, "A0-c **前提**：池里有 `TAU51`");
        C(tau51 == null || tau51.AbilityEffectTexts == null ? -1 : tau51.AbilityEffectTexts.Length, 5,
          "A2 `TAU51`（Saviour Protocols）**5 行全在**（lg0 buff/hand · lg1 buff/hand · lg2 hand）"
        + "｜🧨 撞键就丢一条（或删 buffEffect 行）⇒ 行数变 3/4 ⇒ 本行红");
        C(tau51 == null ? "" : tau51.EffectTextOf(0, 0, false), "SaviorProtocolsEffect",
          "A2-a `(0,0,buffEffect)` 仍在 —— `handBuff` 修好**没有连带吞掉**同键的 `buffEffect`");
        C(tau51 == null ? "" : tau51.EffectTextOf(0, 0, true), "SaviorProtocolsEffect",
          "A2-b `(0,0,handBuff)` 是**另一格**（同 `(ab,lg)` 两行，靠 `IsHand` 分开）");
        C(tau51 == null ? "" : tau51.EffectTextOf(0, 1, true), "SaviorProtocolsEffect",
          "A2-c `(0,1,handBuff)` 也在");
        C(tau51 == null ? "" : tau51.EffectTextOf(0, 2, true), "SaviorProtocolsEffect",
          "A2-d `(0,2,handBuff)` 也在");
        C(tau51 == null ? "?" : tau51.EffectTextOf(0, 2, false), "",
          "A2-e `(0,2,handBuff)` **不是** buffEffect 那一格（挡「全部涂 hand」）");

        var gsc45 = CardDatabase.FindById(pool, "GSC45");
        Tr(gsc45 != null, "A0-d **前提**：池里有 `GSC45`");
        C(gsc45 == null ? "" : gsc45.EffectTextOf(0, 1, true), "EffectUndergroudndNetwork",
          "A3 `GSC45`（Underground Network）标题在 `(0,1,handBuff)`（原文拼写就是 `Undergroudnd`）");
        C(gsc45 == null ? "?" : gsc45.EffectTextOf(0, 1, false), "",
          "A3-b `GSC45` 没有 buffEffect 那一行");

        var um44 = CardDatabase.FindById(pool, "UM44");
        Tr(um44 != null, "A0-e **前提**：池里有 `UM44`");
        C(um44 == null ? "" : um44.EffectTextOf(0, 0, false), "EffectArmouredSupport",
          "A4 纯 `buffEffect` 的卡仍取得到自己的标题（`UM44`/Armoured Support）"
        + "｜🧨 `at` 判反（buffEffect 也当 hand）⇒ 本行红");
        C(um44 == null ? "?" : um44.EffectTextOf(0, 0, true), "",
          "A4-b 它**没有** handBuff 那一格");
    }

    // ============================================================ B 段：映射层合成夹具（不依赖卡表版本）
    //  `MapAbilityEffectTexts` 是 private static ⇒ 用反射调（⛔ 不改它的可见性：那会动账按行号引用的形状）。
    static void SectionB_MappingSynthetic()
    {
        MenuCheck.Section(_sink, "B 映射层：`at` 判 handBuff · `at` 缺时兼容 `isHand` 键");

        var mi = typeof(RuleEngine.CardDatabase).GetMethod("MapAbilityEffectTexts",
                     BindingFlags.NonPublic | BindingFlags.Static);
        Tr(mi != null, "B0 **前提**：`CardDatabase.MapAbilityEffectTexts` 存在（私有静态，反射取）");
        if (mi == null) return;

        // ① TAU51 的真形状：5 行、**只有 `at`、没有 `isHand`**
        var dto = new RuleEngine.CardDatabase.CardDto
        {
            id = "B100F9Synthetic", name = "B100F9Synthetic",
            abilityEffectTexts = new[]
            {
                Row(0, 0, "buffEffect", "SaviorProtocolsEffect"),
                Row(0, 0, "handBuff",  "SaviorProtocolsEffect"),
                Row(0, 1, "buffEffect", "SaviorProtocolsEffect"),
                Row(0, 1, "handBuff",  "SaviorProtocolsEffect"),
                Row(0, 2, "handBuff",  "SaviorProtocolsEffect"),
            },
        };
        CardDef.AbilityEffectTextSpec[] mapped = null;
        try { mapped = (CardDef.AbilityEffectTextSpec[])mi.Invoke(null, new object[] { dto }); }
        catch (Exception e) { Debug.LogWarning("[B100F9] 反射调用抛了：" + e.Message); }
        Tr(mapped != null, "B1-a 映射返回非 null");
        C(mapped == null ? -1 : mapped.Length, 5,
          "B1-b 5 行**一条不丢**｜🧨 删掉 `at == \"handBuff\"` 那一行（还读 `r.isHand`）⇒ 撞键丢 2 条 ⇒ 本行红");
        int handRows = 0, buffRows = 0;
        if (mapped != null)
            foreach (var s in mapped) { if (s.IsHand) handRows++; else buffRows++; }
        C(handRows, 3, "B1-c `IsHand=true` 3 行（lg0/lg1/lg2）");
        C(buffRows, 2, "B1-d `IsHand=false` 2 行（lg0/lg1）—— 与 B1-c **同时**成立才说明没涂反");

        // ② 兼容路：旧写法只有 `isHand`、没有 `at`
        var dto2 = new RuleEngine.CardDatabase.CardDto
        {
            id = "B100F9Compat", name = "B100F9Compat",
            abilityEffectTexts = new[]
            {
                new RuleEngine.CardDatabase.AbilityEffectTextRowDto { ab = 0, lg = 0, isHand = true, at = null, src = "CompatHand" },
                new RuleEngine.CardDatabase.AbilityEffectTextRowDto { ab = 0, lg = 0, isHand = false, at = null, src = "CompatBuff" },
            },
        };
        CardDef.AbilityEffectTextSpec[] mapped2 = null;
        try { mapped2 = (CardDef.AbilityEffectTextSpec[])mi.Invoke(null, new object[] { dto2 }); }
        catch (Exception e) { Debug.LogWarning("[B100F9] 反射调用（兼容路）抛了：" + e.Message); }
        C(mapped2 == null ? -1 : mapped2.Length, 2,
          "B2 无 `at` 的旧写法仍按 `isHand` 分开两行｜🧨 兼容兜底改成 `return false;` ⇒ 撞键丢一条 ⇒ 本行红");
        bool compatHand = false, compatBuff = false;
        if (mapped2 != null)
            foreach (var s in mapped2) { if (s.IsHand && s.Src == "CompatHand") compatHand = true; if (!s.IsHand && s.Src == "CompatBuff") compatBuff = true; }
        Tr(compatHand && compatBuff, "B2-a 两行的 `IsHand` 各自回对（`isHand=true` 那行没被丢）");
    }

    static RuleEngine.CardDatabase.AbilityEffectTextRowDto Row(int ab, int lg, string at, string src)
    {
        return new RuleEngine.CardDatabase.AbilityEffectTextRowDto { ab = ab, lg = lg, at = at, src = src };
    }
}
