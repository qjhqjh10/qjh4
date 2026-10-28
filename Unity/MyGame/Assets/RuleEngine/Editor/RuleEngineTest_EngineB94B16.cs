// RuleEngineTest_EngineB94B16.cs — 第九十四会话 · 波 3 · 执行代理 `B94_B16`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆）。
// 本件只放本批三件的断言；宿主 `RuleEngineTest.cs` 里只加一个
//   `{ CheckB94B16Engine(); }` 块（⚠️ 入口名**不用 `Test`/`Report` 打头**，也**不进 8 空格缩进的
//   顶层语句** ⇒ 不会被 `VerifyRunCaseCoverage` / `gen_diag_runner` 认成用例）。
//
// 本批覆盖（判据 = `d:/2/tools/decomp_full/`，逐句亲读 2026-10-29）：
//   ① `CardDef.GetTraitFromAbilityTrigger` —— 账 `战斗逻辑驱动:994`
//      （`CardTraitCollection__GetTraitFromAbilityTrigger.c:1-58`：3 个硬编码 + `Enum.TryParse` 同名字）
//   ② `CardDef.AbilitiesForTrigger` —— 账 `:1217`
//      （`RawCardScript__GetAbilitiesForTriggerRawcard.c`：按 trigger 过滤出**列表**）
//   ③ `CardDef.AbilityMutationSpec` / `IsCumulativeMutation` —— 账 `:1254`
//      （`RawCardScript__IsCumulativeMutation.c:17-47`；数据列今天缺 ⇒ 休眠实现）
//
// 🔴 每条都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB94B16Engine()
    {
        Section("🆕 B94_B16：GetTraitFromAbilityTrigger（账 994）· AbilitiesForTrigger（账 1217）· mutationType（账 1254）");
        B94B16T1DefinedTraitTable();
        B94B16T2TraitFromTrigger();
        B94B16T3AbilitiesForTrigger();
        B94B16T4MutationType();
    }

    // ---------------------------------------------------------------------------------------------
    //  T1：`DefinedTrait` 名字表（134 条，转写自 `DefinedTrait.cs`）
    // ---------------------------------------------------------------------------------------------
    static void B94B16T1DefinedTraitTable()
    {
        // ---- 前提：整表规模（抄漏/抄重最容易被这一条抓住）----
        Check(CardDef.DefinedTraitCount, 134,
              "前提：`DefinedTrait` 表 134 条（= `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs` 现读成员数）");

        // ---- 抽样覆盖：「名字解析」那条路的三个端点（最小 / 中间 / 最大）----
        Check(CardDef.DefinedTraitNumber("ephemeral"), 5, "`ephemeral` → 5（最小非 none）");
        Check(CardDef.DefinedTraitNumber("rally"), 10, "`rally` → 10（硬编码那一档的数值）");
        Check(CardDef.DefinedTraitNumber("backlash"), 120, "`backlash` → 120");
        Check(CardDef.DefinedTraitNumber("mutation"), 780, "`mutation` → 780（账 1254 的 traitsFilter 目标值）");
        Check(CardDef.DefinedTraitNumber("unstable"), 1010, "`unstable` → 1010");
        Check(CardDef.DefinedTraitNumber("oath"), 1275, "`oath` → 1275");
        Check(CardDef.DefinedTraitNumber("sentry"), 1280, "`sentry` → 1280（最大）");
        Check(CardDef.DefinedTraitNumber("none"), 0, "`none` → 0");

        // ---- 负例 ----
        // 🧨 把比较器改成 OrdinalIgnoreCase ⇒ 这条会变 10（原版 `Enum.TryParse` 默认**大小写敏感**）。
        Check(CardDef.DefinedTraitNumber("Rally"), 0, "🧨 大小写敏感：`Rally` 认不出 ⇒ 0（⛔ 不是 10）");
        Check(CardDef.DefinedTraitNumber("noSuchTrait"), 0, "认不出的名字 ⇒ 0（= none）");
        Check(CardDef.DefinedTraitNumber(""), 0, "空名字 ⇒ 0");
        Check(CardDef.DefinedTraitNumber(null), 0, "null 名字 ⇒ 0");

        // ---- 反查 ----
        Check(CardDef.DefinedTraitName(1010), "unstable", "反查：1010 → `unstable`");
        Check(CardDef.DefinedTraitName(0), "none", "反查：0 → `none`");
        Check(CardDef.DefinedTraitName(9999), null, "反查：9999 不在枚举里 ⇒ null");
    }

    // ---------------------------------------------------------------------------------------------
    //  T2：`GetTraitFromAbilityTrigger` —— 3 个硬编码 + 名字对名字
    // ---------------------------------------------------------------------------------------------
    static void B94B16T2TraitFromTrigger()
    {
        // ---- 三个硬编码（判据 = `.c` 的三个 switch 支；**必须在名字解析之前**）----
        // 🧨 删掉 0 那支的硬编码 ⇒ 会落到 `thisCardPlayed`（不在 DefinedTrait 里）⇒ 变 0（本该 10）。
        Check(CardDef.GetTraitFromAbilityTrigger(0), 10, "🧨 trigger 0（ThisCardPlayed）⇒ 10 rally（硬编码）");
        // 🧨 删掉 80 那支 ⇒ `thisUnitDeath` 不在枚举里 ⇒ 变 0（本该 120）。
        Check(CardDef.GetTraitFromAbilityTrigger(80), 120, "🧨 trigger 80（ThisUnitDeath）⇒ 120 backlash（硬编码）");
        Check(CardDef.GetTraitFromAbilityTrigger(81), 1010, "trigger 81（Unstable）⇒ 1010");

        // ---- 名字对名字那一路（29 个同名里的抽样）----
        Check(CardDef.GetTraitFromAbilityTrigger(120), 260, "trigger 120（Slay）⇒ 260 slay");
        Check(CardDef.GetTraitFromAbilityTrigger(430), 760, "trigger 430（Requiem）⇒ 760 requiem");
        Check(CardDef.GetTraitFromAbilityTrigger(655), 1185, "trigger 655（Pray）⇒ 1185 pray");
        Check(CardDef.GetTraitFromAbilityTrigger(1225), 1225, "trigger 1225（Regiment）⇒ 1225 regiment");

        // ---- 不该命中的（原版对大多数 trigger 也回 none）----
        Check(CardDef.GetTraitFromAbilityTrigger(20), 0, "trigger 20（WhileInPlay）⇒ 0 none（原版同样折不出）");
        Check(CardDef.GetTraitFromAbilityTrigger(30), 0, "trigger 30（TurnStart）⇒ 0 none");
        // 🔴 2026-10-29（`B94_FX8` · DG1 β1 · 铁律 5 就地改期望值 0 → 9999）：原版 `.c:36-45` 是
        //   `Enum.ToString(boxed)` → lowerFirst → `Enum.TryParse<DefinedTrait>`，而 **`Enum.ToString` 对不在枚举里的
        //   数值返回十进制串**（`"9999"`），`Enum.TryParse` **照样接受数值串**（不校验成员身份）⇒ 原版回 **9999**。
        //   ⛔ 旧期望 `0` 与旧实现（`AbilityTriggerNameOf` 返回 null ⇒ `return 0`）是**两边一起错**（灭自证的反面）。
        Check(CardDef.GetTraitFromAbilityTrigger(9999), 9999,
              "不在 AbilityTrigger 枚举里的数值 ⇒ **9999**（`Enum.TryParse` 接受数值串，原版不校验成员身份）");

        // ---- 名字版（先 name → 数值 → 同一判据；⛔ 不许另写一套 switch）----
        Check(CardDef.GetTraitFromAbilityTrigger("ThisCardPlayed"), 10, "名字版：ThisCardPlayed ⇒ 10");
        Check(CardDef.GetTraitFromAbilityTrigger("ThisUnitDeath"), 120, "名字版：ThisUnitDeath ⇒ 120");
        Check(CardDef.GetTraitFromAbilityTrigger("Unstable"), 1010, "名字版：Unstable ⇒ 1010");
        Check(CardDef.GetTraitFromAbilityTrigger("Slay"), 260, "名字版：Slay ⇒ 260");
        Check(CardDef.GetTraitFromAbilityTrigger("UnitAttack"), 0, "名字版：UnitAttack ⇒ 0 none");
        Check(CardDef.GetTraitFromAbilityTrigger("NoSuchTrigger"), 0, "名字版：认不出的 trigger 名 ⇒ 0");
        Check(CardDef.GetTraitFromAbilityTrigger((string)null), 0, "名字版：null ⇒ 0");

        // ---- 两个版必须同一答案（数值版不许绕过名字表另写一份）----
        Check(CardDef.GetTraitFromAbilityTrigger("Pray"), CardDef.GetTraitFromAbilityTrigger(655),
              "两版一致：`Pray` 名字版 == 655 数值版");
    }

    // ---------------------------------------------------------------------------------------------
    //  T3：`AbilitiesForTrigger` —— 原版返回**列表**（同一 trigger 可多条）
    // ---------------------------------------------------------------------------------------------
    static void B94B16T3AbilitiesForTrigger()
    {
        // 三条 ability：0(ThisCardPlayed) / 655(Pray) / 655(Pray) —— 故意让 Pray **两次**
        // （实测池内 89 张卡有两条 ability 共用同一个 trigger ⇒ 这才是该测的形状）。
        var specs = new[]
        {
            new CardDef.AbilityTargetSpec { Trigger = 0 },
            new CardDef.AbilityTargetSpec { Trigger = 655 },
            new CardDef.AbilityTargetSpec { Trigger = 655 },
        };
        var card = new CardDef("B94B16_A", "B94B16_A", "unit", "", null, "Test", 1, 1, 1, 0,
                               keywords: null,
                               abilityTriggers: new[] { 0, 655 },
                               abilityTriggersVersioned: true,
                               abilityTargets: specs);

        // ---- 前提：列真有 3 条、且两条 Pray（⛔ 别假定夹具落进去了）----
        Check(card.AbilityTargets.Count, 3, "前提：`abilityTargets` 落进 3 条");
        int pray = 0;
        for (int i = 0; i < card.AbilityTargets.Count; i++)
            if (card.AbilityTargets[i] != null && card.AbilityTargets[i].Trigger == 655) pray++;
        Check(pray, 2, "前提：其中 2 条 `trigger == 655`");
        if (pray != 2) return;

        var one = card.AbilitiesForTrigger("ThisCardPlayed");
        CheckTrue(one != null, "前提：`ThisCardPlayed` 判得了（列已 versioned）");
        Check(one != null ? one.Count : -1, 1, "`ThisCardPlayed` 命中 1 条");

        var two = card.AbilitiesForTrigger("Pray");
        // 🧨 若把它实现成「归一到唯一一条」（如 `UniqueAbilityIndexForTrigger` 那样）⇒ 这里会变 1 或 null。
        Check(two != null ? two.Count : -1, 2, "🧨 `Pray` 命中 **2** 条（原版返回列表；归一会变 1）");

        var none = card.AbilitiesForTrigger("TurnStart");
        CheckTrue(none != null && none.Count == 0, "`TurnStart`：明确没有 ⇒ **空表**（不是 null）");

        // ---- 认不出名字 ⇒ 判不了（null），⛔ 不是「没有」----
        // 🧨 把 `AbilityTriggerNumber < 0` 那一支改成返回空表 ⇒ 这条会红。
        CheckTrue(card.AbilitiesForTrigger("NotATrigger") == null,
                  "🧨 认不出的 trigger 名 ⇒ **null**（判不了，⛔ 不是空表）");

        // ---- 数值重载与名字重载同一答案 ----
        var byNum = card.AbilitiesForTrigger(655);
        Check(byNum != null ? byNum.Count : -1, 2, "数值重载：655 ⇒ 2 条（与名字重载一致）");

        // ---- 旧表（列没落）⇒ 一律 null（未知），⛔ 不许当「没有」----
        var oldCard = new CardDef("B94B16_Old", "B94B16_Old", "unit", "", null, "Test", 1, 1, 1, 0);
        CheckTrue(oldCard.AbilitiesForTrigger("ThisCardPlayed") == null,
                  "旧表（`abilityTargets` 没落）⇒ null（未知）");
    }

    // ---------------------------------------------------------------------------------------------
    //  T4：`mutationType`（休眠实现）—— 只认 20 Cumulative
    // ---------------------------------------------------------------------------------------------
    static void B94B16T4MutationType()
    {
        // ---- 未知档（今天全池就是这个形态：卡表没有这一列）----
        var unknown = new CardDef("B94B16_M0", "B94B16_M0", "unit", "", null, "Test", 1, 1, 1, 0);
        CheckTrue(!unknown.HasAbilityMutations, "前提：没挂数据时 `HasAbilityMutations == false`（未知）");
        CheckTrue(!unknown.IsCumulativeMutation(), "未知列 ⇒ `IsCumulativeMutation() == false`（原版签名是 bool）");

        // ---- 明确有 20 ⇒ 真 ----
        var cum = new CardDef("B94B16_M1", "B94B16_M1", "unit", "", null, "Test", 1, 1, 1, 0);
        cum.AttachAbilityMutations(new[]
        {
            new CardDef.AbilityMutationSpec { Ab = 0, Lg = 0, MutationType = CardDef.MutationCumulative },
            new CardDef.AbilityMutationSpec { Ab = 0, Lg = 1, MutationType = CardDef.MutationNone },
        });
        CheckTrue(cum.HasAbilityMutations, "挂上数据后 `HasAbilityMutations == true`");
        CheckTrue(cum.IsCumulativeMutation(), "有一条 `20 Cumulative` ⇒ 真");
        Check(cum.MutationTypeAt(0, 1), 0, "`MutationTypeAt(0,1)` ⇒ 0 none");
        Check(cum.MutationTypeAt(0, 2), -1, "没有这一行 ⇒ -1（⛔ 不是 0）");

        // ---- 只有 10 Normal / 0 none ⇒ 假（原版只认 == 20）----
        // 🧨 把判据从 `== 20` 改成 `!= 0`（「有 buffEffect 就是变异」）⇒ 这条会红。
        var normal = new CardDef("B94B16_M2", "B94B16_M2", "unit", "", null, "Test", 1, 1, 1, 0);
        normal.AttachAbilityMutations(new[]
        {
            new CardDef.AbilityMutationSpec { Ab = 0, Lg = 0, MutationType = CardDef.MutationNormal },
        });
        CheckTrue(!normal.IsCumulativeMutation(), "🧨 只有 `10 Normal` ⇒ **假**（原版只认 20）");

        var noneOnly = new CardDef("B94B16_M3", "B94B16_M3", "unit", "", null, "Test", 1, 1, 1, 0);
        noneOnly.AttachAbilityMutations(new[]
        {
            new CardDef.AbilityMutationSpec { Ab = 0, Lg = 0, MutationType = CardDef.MutationNone },
        });
        CheckTrue(!noneOnly.IsCumulativeMutation(), "只有 `0 none` ⇒ 假");
    }
}
