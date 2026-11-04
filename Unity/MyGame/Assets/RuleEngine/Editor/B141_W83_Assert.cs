// B141_W83_Assert.cs — 批次 **B141** · 执行写手 `W83`（W128 · CHK-05 战斗域 **L543** ＋ **L986**）
//
// ============================ 它盯的是什么 ============================
//   A 组（账 `战斗逻辑驱动:543` = `BattleManager.ResolveSummonUnit`）：**召唤那一拍占多久**
//       —— 现口径 = `EventTiming.DeploySummonHold` **0.45 s**（`_MinionPlayedIntoField` 四个 `WaitForSeconds` 之和）。
//       ⚠️ 本组**只锁「我们现在的口径」**：那两条「原版还差什么」的结论在报告里
//       （`资料/完全复刻/报告/B141_W128_CHK05_543_986.md` §1）—— 那里证明「带 `cardAnim` 那一支」
//       在本 build **零生产者**（死分支）⇒ 0.45 就是**完整**寿命，⛔ 不必再加按卡的 clip。
//
//   B 组（账 `战斗逻辑驱动:986` = `CardTraitCollection.GetAbilityToTraitDict`）：**派生 trait 的集合本身**
//       —— 原版 `GetTraitsFromAbilitiesRawCard` 的 **25 对 (trigger→trait)**、
//       Goff(20)/Sautekh(40) **空表闸**、以及「`hasActiveAbility` 补的那一条**不属于**图标路径」这三件。
//
//   C 组：**消费端缺口是可观测的**（不是"判据不足"）＋ **⛔ 不许用「写进引擎关键词」的方式去补**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A4：把 `DeployHoldOf` 的 `default` 也改成返回 `DeploySummonHold` ⇒ A4 红（证明 A3 的值不是"全都一样"）。
//   · A5：把 `DurationOf(EvtKind.Deploy)` 改成 0.45 ⇒ A5 红（证明召唤那一档真的走 `DeployHoldOf` 分支）。
//   · B3/B4：把 `CardDefExtraTraits` 里 `army != 20 && army != 40` 那道闸删掉 ⇒ B3/B4 红。
//   · B5：把 `ExtraAbilityTraits` 的 step② 并进 step①（删 `TriggerName == null` 的判法）⇒ B5 红。
//   · C1：给 `Badges.For` 传入派生的关键词之后**多出来的那一枚图标**，改前必须为 0 ⇒ 删「缺口」这条先红。
//   · C2：把派生 trait **写进** `UnitState._keywords`（= 错误修法）⇒ C2 红。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 聚合行**同一物理行**追加（接线由调度台统一做）：
//     `Guard("B141W83_Assert", B141_W83_Assert.Run); _pass += B141_W83_Assert.Pass; _fail += B141_W83_Assert.Fail;`
//   （⚠️ 追加文本必须落在该行第一个 `//` **之前**。）
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>B141-W83 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B141_W83_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B141-W83：召唤占时（L543）＋ 派生 trait 集合与消费端缺口（L986）");
        GroupA_SummonHold();
        GroupB_DerivedTraits();
        GroupC_ConsumerGap();
        Summary();
    }

    // ==================================================================
    //  A 组 · L543：召唤那一拍 = 0.45 s
    // ==================================================================
    static void GroupA_SummonHold()
    {
        Section("A · L543 `ResolveSummonUnit` 占时 = `_MinionPlayedIntoField` 四个 WaitForSeconds 之和");

        // A1 常量本身
        Check(EventTiming.DeploySummonHold, 0.45f, "A1 `DeploySummonHold` = 0.45");

        // A2 四段相加恒等式（0.1 = delay/`timeToScaleIntoMoving` · 0.05 = timeBeforeLand
        //    · 0.2 = timeToLand · 0.1 = AttackStepTime）—— 改任何一段都必须回来改这条
        float four = 0.1f + 0.05f + 0.2f + EventTiming.AttackStepMelee;
        CheckTrue(Mathf.Abs(four - EventTiming.DeploySummonHold) < 1e-4f,
                  $"A2 四段相加 = `DeploySummonHold`（实际 0.1+0.05+0.2+{EventTiming.AttackStepMelee} = {four}）");

        // A3 召唤 / 复活两档都取这个常量（`Reanimate` 共用本常量 —— 账里记着「未单独核再造那条协程」，
        //    所以这里锁的是**我们的口径**，不是「原版复核过」）
        Check(EventTiming.DeployHoldOf(DeploySource.Summon), EventTiming.DeploySummonHold,
              "A3a `DeployHoldOf(Summon)` = 常量");
        Check(EventTiming.DeployHoldOf(DeploySource.Reanimate), EventTiming.DeploySummonHold,
              "A3b `DeployHoldOf(Reanimate)` = 常量（共用，未单独核原版再造协程）");

        // A4（灭自证）兜底档必须**不等于**召唤档 —— 否则 A3 那两个等式是"全都一样"而不是"这一档特判"
        float unknown = EventTiming.DeployHoldOf(DeploySource.Unknown);
        CheckTrue(Mathf.Abs(unknown - EventTiming.DeploySummonHold) > 1e-4f,
                  $"A4 兜底档 ≠ 召唤档（实际 {unknown} vs {EventTiming.DeploySummonHold}）");
        Check(unknown, EventTiming.DeploySummonHold + EventTiming.DeployLandHold,
              "A4b 兜底档 = 召唤 + 落位（0.75）");

        // A5（灭自证）`DurationOf(Deploy)` 是**不再分档的兜底** ⇒ 它必须 ≠ 0.45，
        //    证明真实路径走的是 `DeployHoldOf` 而不是 `DurationOf`
        float dur = EventTiming.DurationOf(EvtKind.Deploy);
        CheckTrue(Mathf.Abs(dur - EventTiming.DeploySummonHold) > 1e-4f,
                  $"A5 `DurationOf(Deploy)` = 兜底档（实际 {dur}；≠ 召唤档 {EventTiming.DeploySummonHold}）");
    }

    // ==================================================================
    //  B 组 · L986：派生 trait 集合（原版 `GetTraitsFromAbilitiesRawCard` 的 25 对）
    // ==================================================================
    static void GroupB_DerivedTraits()
    {
        Section("B · L986 派生 trait：25 对表 / 阵营空表闸 / step② 与图标路径分离");

        // 25 个 trigger 数值（照 `CardDefExtraTraits.ExtraTraitTriggers`，判据 = `GetTraitsFromAbilitiesRawCard.c`）
        int[] allTriggers =
        {
            0, 80, 81, 120, 430, 440, 145, 130, 230, 45, 46, 220, 300, 301, 550, 560, 580, 590, 655, 670, 690, 680, 710, 715, 720,
        };

        // B1 单条：trigger 81（Unstable）⇒ trait 1010 / 关键词 `unstable`
        var c1 = Unit("B141W83_81", "SaimHann", all: false, triggers: new[] { 81 });
        var e1 = Step1(c1);
        Check(e1.Count, 1, "B1a trigger 81 ⇒ step① 1 条");
        Check(e1.Count > 0 ? e1[0].Keyword : null, "unstable", "B1b trigger 81 ⇒ `unstable`");
        Check(e1.Count > 0 ? e1[0].TraitValue : -1, 1010, "B1c trigger 81 ⇒ TraitValue 1010");

        // B2 没有 trigger ⇒ step① 空（版本位已置、集合为空）
        var c2 = Unit("B141W83_none", "SaimHann", all: false, triggers: new int[0]);
        Check(Step1(c2).Count, 0, "B2 无 trigger ⇒ step① 空");

        // B3/B4 阵营空表闸（Goff = 20 / Sautekh = 40）
        var c3 = Unit("B141W83_goff", "Goff", all: false, triggers: new[] { 81, 120, 715 });
        Check(Step1(c3).Count, 0, "B3 Goff(20) ⇒ 25 对表整张为空");
        var c4 = Unit("B141W83_sautekh", "Sautekh", all: false, triggers: new[] { 81, 120, 715 });
        Check(Step1(c4).Count, 0, "B4 Sautekh(40) ⇒ 25 对表整张为空");

        // B5 step②（`hasActiveAbility` 补的那一条）**不属于**图标路径 —— 单独拷出来看
        var c5 = Unit("B141W83_active", "Sororitas", all: false, triggers: new[] { 81 }, hasActiveAbility: 1);
        int step1Count = Step1(c5).Count;
        int step2Count = c5.ExtraAbilityTraits.Count - step1Count;
        Check(step1Count, 1, "B5a 有主动能力时 step① 仍是 1 条（trigger 81）");
        Check(step2Count, 1, "B5b step②（`hasActiveAbility`）恰好 1 条、且被排除在图标路径之外");
        bool anyNullTriggerNameInStep1 = false;
        for (int i = 0; i < Step1(c5).Count; i++) if (Step1(c5)[i].TriggerName == null) anyNullTriggerNameInStep1 = true;
        CheckTrue(!anyNullTriggerNameInStep1, "B5c step① 里不存在 `TriggerName == null` 的条目");

        // B6 全表：一张带齐 25 个 trigger 的卡 ⇒ 25 条（抽查 6 个键，⛔ 不在这里抄第三份表）
        var c6 = Unit("B141W83_all", "SaimHann", all: true, triggers: allTriggers);
        var e6 = Step1(c6);
        Check(e6.Count, 25, "B6a 25 个 trigger 全带 ⇒ step① 25 条");
        var keys = new HashSet<string>();
        for (int i = 0; i < e6.Count; i++) if (e6[i].Keyword != null) keys.Add(e6[i].Keyword);
        CheckTrue(keys.Contains("rally") && keys.Contains("backlash") && keys.Contains("penitence")
                  && keys.Contains("regiment") && keys.Contains("cruelty") && keys.Contains("unstable"),
                  "B6b 抽查 `rally`/`backlash`/`penitence`(Rage 档)/`regiment`/`cruelty`/`unstable` 都在");
    }

    // ==================================================================
    //  C 组 · L986：消费端缺口可观测 ＋ 灭自证（不许写进引擎关键词）
    // ==================================================================
    static void GroupC_ConsumerGap()
    {
        Section("C · L986 消费端：`Badges.For` 只吃 `u.Keywords` ⇒ 派生来源今天没有落点");

        // 一张「有 trigger 715（Ecstasy）、卡面不带 ecstasy」的卡
        var card = Unit("B141W83_ecstasy", "SaimHann", all: false, triggers: new[] { 715 }, keywords: null);
        var u = new UnitState(card, false);

        var derived = Step1(card);
        Check(derived.Count, 1, "C1a 派生集合里有 1 条（trigger 715 ⇒ ecstasy）");
        Check(derived.Count > 0 ? derived[0].Keyword : null, "ecstasy", "C1b 那一条 = `ecstasy`");

        // C1（缺口）· 今天 `Badges.For(u.Keywords)` **一枚都不画**；把派生键补进去才画得出那一枚。
        //    ⇒ 「数据在、消费端不在」是可复现的，不是判据不足。
        int before = CountKey(Badges.For(u.Keywords), "ecstasy");
        Check(before, 0, "C1c 改前 `Badges.For(u.Keywords)` 不含 ecstasy（缺口可观测）");
        var merged = Merge(u.Keywords, derived);
        int after = CountKey(Badges.For(merged), "ecstasy");
        Check(after, 1, "C1d 把派生的关键词并进去之后 ⇒ 画得出 1 枚（补法已验，只差接线）");

        // C2（灭自证）· ⛔ 派生 trait **不许**写进引擎关键词：原版 `HasCurrentTrait` 读的 `CurrentTraits`
        //    只由 `CheckCurrentTraits` 追加，而那个方法 **全树 0 调用点**（`RawCardScript__CheckCurrentTraits.c`
        //    三源确认）⇒ 写进 `_keywords` 会让 `Has(…)` 多出原版没有的行为。
        CheckTrue(!u.Has("ecstasy"), "C2 派生 trait 不进引擎关键词（`Has(\"ecstasy\")` 必须为 false）");

        // C3 · 原版这一枚**不带角标**：`UpdateTraitIcons` 的 counter 走
        //      `GetCurrentTraitValueWithModifiers(card, traitId)`，而派生 trait 不在 `CurrentTraits` 里
        //      ⇒ `List.Find` 命中不了 ⇒ counter = 0 ⇒ `BoardTraitIcon` 的 `Without counter` 那一支。
        //      ⚠️ 这条**只记录读法**：我们的 `Badges.For` 对 `ecstasy` 会因为 ③ 表给 1 枚角标
        //      ⇒ 接线补丁必须显式压制角标（报告 §2.4），⛔ 别把本行当成"已经对了"。
        CheckTrue(Badges.CarriesValue("ecstasy", 0, null),
                  "C3 `CarriesValue(\"ecstasy\", 0)` = true ⇒ 朴素并表会多印一个 1（接线补丁必须压制角标）");
    }

    // -- C 组用的小工具 --------------------------------------------------
    static List<KeyValuePair<string, int>> Merge(IReadOnlyDictionary<string, int> own,
                                                List<CardDef.ExtraAbilityTrait> derived)
    {
        var acc = new List<KeyValuePair<string, int>>();
        if (own != null) foreach (var kv in own) acc.Add(kv);
        for (int i = 0; i < derived.Count; i++)
        {
            string k = derived[i].Keyword;
            if (string.IsNullOrEmpty(k)) continue;
            bool has = false;
            for (int j = 0; j < acc.Count; j++) if (string.Equals(acc[j].Key, k, System.StringComparison.Ordinal)) { has = true; break; }
            if (!has) acc.Add(new KeyValuePair<string, int>(k, 0));
        }
        return acc;
    }

    static int CountKey(List<Badge> list, string key)
    {
        int n = 0;
        for (int i = 0; i < list.Count; i++) if (string.Equals(list[i].key, key, System.StringComparison.Ordinal)) n++;
        return n;
    }

    /// <summary>原版 `GetTraitInPlayListForIcons` **第二支**用的那一份 = `ExtraAbilityTraits` 里
    /// `TriggerName != null` 的那些（step② 的 `hasActiveAbility` 补条 `TriggerName` 恒 `null`）。
    /// ⚠️ 这就是报告 §2.3 要给 `CardDef`/`RuleCore` 补的那个派生口 —— 本方法只是**断言里的等价写法**，
    /// 产品侧落地由调度台按补丁派给对应文件的写手。</summary>
    static List<CardDef.ExtraAbilityTrait> Step1(CardDef card)
    {
        var acc = new List<CardDef.ExtraAbilityTrait>();
        var all = card.ExtraAbilityTraits;
        for (int i = 0; i < all.Count; i++)
            if (all[i].TriggerName != null && !string.IsNullOrEmpty(all[i].Keyword)) acc.Add(all[i]);
        return acc;
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    static CardDef Unit(string id, string faction, bool all, int[] triggers,
                        IEnumerable<string> keywords = null, int hasActiveAbility = 0)
    {
        var c = new CardDef(id, id, "unit", "", null, faction, 2, 1, 5, 0, keywords, subtype: "Infantry",
                            hasActiveAbility: hasActiveAbility);
        c.AttachAbilityTriggers(triggers);
        return c;
    }

    // ==================================================================
    //  输出
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B141_W83 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B141_W83 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
