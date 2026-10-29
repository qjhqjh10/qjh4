// B97W10_Assert.cs — 执行代理 `B97_W10`（第九十七会话 · 波 1 补位）的断言
//
// 本件是 `RuleEngineTest` 的**分册**（`public static partial class RuleEngineTest`，与 `B97_AI_Assert.cs`
// / `B97W5_Assert.cs` 同一种形状）—— `RuleEngine/Editor/RuleEngineTest.cs` 本体**本批不在白名单**，
// 所以测试体全写在这里，宿主只需在 `Run()` 里加一行（见下）。
//
// ============================ 它盯的是哪两件 ============================
//   件 1：`SimpleAI.EnumerateActions` 的**三处目标枚举**改成原版 `unitsInPlay`(`+0x470`) 的**进场序**
//          （战术卡 · 替代行动 · 普通主动技能），外加**施放者外循环**与**收灵魂石**两处同轴。
//   件 2：`SimpleAI.TweakAvailableActions` 的排序改成原版的**稳定** `OrderByDescending`。
//
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_R4_战斗队列三件.md` 件 1 / 件 2
//   ＋ 本轮报告 `资料/完全复刻/报告/B97_W10.md`。
//
// ============================ 宿主接线（调度台做）============================
//   `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加一行（建议紧跟在 `Step(CheckB97AIGaps);` 之后）：
//       Step(CheckB97W10PlayOrderAndStableSort);
//   （本方法名**不以 `Test` / `Report` 打头** ⇒ 不会被 `RunCoverageCompiledCases()` 当成「有 Test* 但没 Step」判红。）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   ① **前提断言与结论断言并列写**，⛔ 不写 `if (前提) Check(结论)`（前提不成立时**红**，不静默跳过）；
//   ② 期望值一律**字面量 / 由构造规则直接算出**，⛔ 不从被测实现反算；
//   ③ 结构断言单独一段（`W10C`）：把实现改回格号序 / `list.Sort` ⇒ 那一段必红，
//      而**不是**靠「实现 + 断言一起改回去」蒙混过去。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由宿主 `Run()` 里那一行调用）。</summary>
    public static void CheckB97W10PlayOrderAndStableSort()
    {
        Section("🆕 B97_W10：AI 动作枚举改「进场序」＋ `TweakAvailableActions` 稳定排序");
        W10A_StableSortKeepsInsertionOrder();
        W10B_TacticTargetsUsePlayOrder();
        W10C_AbilityTargetsUsePlayOrder();
        W10D_AlternativeTargetsUsePlayOrder();
        W10E_StructuralNoRegression();
    }

    // ==========================================================================================
    //  A. 件 2 —— `StableSortByScoreDesc` 必须**稳定**（原版 `AI.TweakAvailableActions` 的 `OrderByDescending`）
    // ==========================================================================================
    //  为什么走反射：`StableSortByScoreDesc` 是 `SimpleAI` 的私有助手，而本文件在**编辑器程序集**
    //  （`Assets/RuleEngine/Data/*.cs` 编进 `Assembly-CSharp`，`Assets/CardPresentation/Editor/*.cs` 编进
    //   `Assembly-CSharp-Editor`）⇒ 直接调用编译不过。反射取值不影响判别力（判的仍是**那一份实现**）。
    //  先例 = `B94B11_Assert.cs` 对 `TutorialScriptPredicatesB94B8.IsScriptedStageAvailable` 用 `GetMethod`。
    static void W10A_StableSortKeepsInsertionOrder()
    {
        var mi = typeof(SimpleAI).GetMethod("StableSortByScoreDesc", BindingFlags.NonPublic | BindingFlags.Static);
        CheckTrue(mi != null,
            "★（前提）`SimpleAI.StableSortByScoreDesc` 在位（私有静态）｜🧨 有人把它删掉 / 改名 / 挪进别的类 ⇒ 红");
        if (mi == null) return;

        var ps = mi.GetParameters();
        CheckTrue(ps.Length == 1 && ps[0].ParameterType == typeof(List<AiAction>),
            "★（前提）签名是「吃 `List<AiAction>` 的 void 方法」｜🧨 改成返回新表 / 换参数类型 ⇒ 红");
        if (ps.Length != 1 || ps[0].ParameterType != typeof(List<AiAction>)) return;

        // ---- A1：20 条**同分** ⇒ 稳定排序必须原样保持（>16 元素才会进 introsort 的分区路径）----
        var eq = new List<AiAction>();
        for (int i = 0; i < 20; i++) eq.Add(new AiAction { Kind = AiActionKind.PlayCard, Slot = i, Score = 0f });
        mi.Invoke(null, new object[] { eq });
        bool eqKept = true;
        for (int i = 0; i < 20 && eqKept; i++) eqKept = eq[i].Slot == i;
        CheckTrue(eqKept,
            "★★ A1：**20 条同分**动作稳定降序后，次序**原样不变**（Slot 0,1,2,…,19）"
          + "｜🧨 换回 `list.Sort(...)`（introsort、不保证稳定）⇒ 20 > 16 会走分区路径打乱等键次序 ⇒ 红");

        // ---- A2：40 条混合分（分 = i%4）+ 同分交错 ⇒ 降序排列，且**同分组内保持原下标序** ----
        //  期望值由**构造规则**直接算出（分组 + 组内下标升序），⛔ 不从实现反算。
        var mix = new List<AiAction>();
        for (int i = 0; i < 40; i++) mix.Add(new AiAction { Kind = AiActionKind.PlayCard, Slot = i, Score = i % 4 });
        mi.Invoke(null, new object[] { mix });

        var expect = new List<int>();
        for (int wantScore = 3; wantScore >= 0; wantScore--)
            for (int i = 0; i < 40; i++)
                if (i % 4 == wantScore) expect.Add(i);

        bool orderOk = mix.Count == expect.Count;
        int firstBad = -1;
        for (int i = 0; orderOk && i < expect.Count; i++)
            if (mix[i].Slot != expect[i]) { orderOk = false; firstBad = i; }
        CheckTrue(orderOk,
            "★★ A2：40 条混合分（分 = `i%4`）降序后 = 「3 分组 → 2 分组 → 1 分组 → 0 分组」，"
          + "**每组内部保持原下标升序**（稳定排序的定义）"
          + (orderOk ? "" : $"｜第一个不符的位置 = {firstBad}（实得 Slot {mix[firstBad].Slot}，"
                         + $"期望 {expect[firstBad]}）")
          + "｜🧨 换回不稳定的 `list.Sort(...)` ⇒ 同分组被打乱 ⇒ 红");

        // ---- A3：首尾钉两个**字面量**（⛔ 不靠 A2 的算式自证）----
        Check(mix.Count > 0 ? mix[0].Slot : -1, 3,
            "★★ A3：首元素 = **3 号**（分 = 3 的那一组里下标最小的那个）｜🧨 降序写反 ⇒ 首元素变成分 = 0 的 0 号 ⇒ 红");
        Check(mix.Count > 0 ? mix[mix.Count - 1].Slot : -1, 36,
            "★★ A3：末元素 = **36 号**（分 = 0 的那一组里下标最大的那个）｜🧨 稳定性丢失（同分组乱序）⇒ 末元素可能变成别的 0 分组元素 ⇒ 红");
    }

    // ==========================================================================================
    //  B. 件 1·② —— 战术卡的目标枚举 = 进场序（原版 `.c:103` `RawCardScript.GetValidTargets`）
    // ==========================================================================================
    static void W10B_TacticTargetsUsePlayOrder()
    {
        // 两张**不同 id** 的战术卡各塞一边手牌（`TargetSideOf` 按卡 id 缓存 ⇒ id 必须不同）：
        // 这样无论谁先手，**行动方的手牌 0 号**都是一张「打敌方」的战术卡。
        var tac0 = Tactic("B97W10TacA", 0, "Deal 1 damage to an enemy");
        var tac1 = Tactic("B97W10TacB", 0, "Deal 1 damage to an enemy");
        var ctx = Battle(new[] { tac0 }, new[] { tac1 });
        CheckTrue(ctx != null && !ctx.IsOver,
            "★（前提）这一局还没结束（`EnumerateActions` 在对局结束时返回空表 ⇒ 下面会假红）｜🧨 夹具改坏 ⇒ 红");
        if (ctx == null || ctx.IsOver) return;

        int me = ctx.Active, foe = 1 - me;
        var e8 = Place(ctx, foe, 8, Unit("B97W10T8", 1, 0, 5));
        var e2 = Place(ctx, foe, 2, Unit("B97W10T2", 1, 0, 5));
        CheckTrue(e8 != null && e2 != null,
            "★（前提）敌方 8 / 2 号格各摆上一个兵｜🧨 摆位失效（格号越界 / 被督军占去）⇒ 红");
        if (e8 == null || e2 == null) return;
        CheckTrue(e8.PlayOrder < e2.PlayOrder,
            "★（前提）**8 号格那个先进场**（`PlayOrder` 更小）⇒ 进场序 `[8, 2]`、格号序 `[2, 8]`，两者**不同**"
          + "｜🧨 把两次 `Place` 的顺序对调 ⇒ 判别式失效 ⇒ 红");

        var all = SimpleAI.EnumerateActions(ctx);
        CheckTrue(all != null && all.Count > 0,
            "★（前提）动作表非空｜🧨 回合归属闸（`me != ctx.Active`）挡掉整张表 ⇒ 红");
        if (all == null || all.Count == 0) return;

        int i8 = -1, i2 = -1;
        for (int i = 0; i < all.Count; i++)
        {
            var a = all[i];
            if (a.Kind != AiActionKind.PlayCard || a.HandIdx != 0) continue;   // 手牌 0 号 = 那张战术卡
            if (a.Slot == 8 && i8 < 0) i8 = i;
            if (a.Slot == 2 && i2 < 0) i2 = i;
        }
        CheckTrue(i8 >= 0,
            "★（前提）`Slot=8` 的战术动作在表里（i8=" + i8 + "）｜🧨 `CanPlayCard` 被别的闸挡掉 ⇒ 红");
        CheckTrue(i2 >= 0,
            "★（前提）`Slot=2` 的战术动作在表里（i2=" + i2 + "）｜🧨 同上 ⇒ 红");
        if (i8 < 0 || i2 < 0) return;

        CheckTrue(i8 < i2,
            "★★ 战术卡：**先进场的 8 号格目标先被枚举**（i8=" + i8 + " < i2=" + i2 + "）"
          + " —— 原版 `.c:103` 走 `RawCardScript.GetValidTargets`，而它自己也按 `+0x470` 下标 0→大正序"
          + "｜🧨 改回「格号升序」的 `for (int s = 0; s < BoardSpec.Size; s++)` ⇒ 2 号格在前 ⇒ 红");
    }

    // ==========================================================================================
    //  C. 件 1·④ —— 普通主动技能（`Ability:` 档）的目标枚举 = 进场序（原版 `.c:185-215`）
    // ==========================================================================================
    static void W10C_AbilityTargetsUsePlayOrder()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        CheckTrue(ctx != null && !ctx.IsOver, "★（前提）这一局还没结束｜🧨 夹具改坏 ⇒ 红");
        if (ctx == null || ctx.IsOver) return;

        int me = ctx.Active, foe = 1 - me;
        var caster = Place(ctx, me, 3, Unit("B97W10Caster", 3, 2, 5, "Ability: Damage 3 EnemyUnit"));
        var e8 = Place(ctx, foe, 8, Unit("B97W10A8", 1, 0, 5));
        var e2 = Place(ctx, foe, 2, Unit("B97W10A2", 1, 0, 5));
        CheckTrue(caster != null && e8 != null && e2 != null,
            "★（前提）施法者（我方 3 号格）与两个目标（敌方 8 / 2 号格）都摆上了｜🧨 摆位失效 ⇒ 红");
        if (caster == null || e8 == null || e2 == null) return;
        CheckTrue(e8.PlayOrder < e2.PlayOrder,
            "★（前提）**8 号格那个先进场** ⇒ 进场序 `[8, 2]`、格号序 `[2, 8]`｜🧨 对调 `Place` 顺序 ⇒ 红");

        var all = SimpleAI.EnumerateActions(ctx);
        CheckTrue(all != null && all.Count > 0, "★（前提）动作表非空｜🧨 回合归属闸挡掉整张表 ⇒ 红");
        if (all == null || all.Count == 0) return;

        int i8 = -1, i2 = -1;
        for (int i = 0; i < all.Count; i++)
        {
            var a = all[i];
            if (a.Kind != AiActionKind.ActiveAbility || a.Slot != 3 || a.AltKeyword != null) continue;
            if (a.TargetSlot == 8 && i8 < 0) i8 = i;
            if (a.TargetSlot == 2 && i2 < 0) i2 = i;
        }
        CheckTrue(i8 >= 0 && i2 >= 0,
            "★（前提）施法者对 8 / 2 号格各有一条主动技能动作（i8=" + i8 + " · i2=" + i2 + "）"
          + "｜🧨 `CanUseAbility` 被别的闸挡掉 ⇒ 红");
        if (i8 < 0 || i2 < 0) return;

        CheckTrue(i8 < i2,
            "★★ 主动技能：**先进场的 8 号格目标先被枚举**（i8=" + i8 + " < i2=" + i2 + "）"
          + " —— 原版 `.c:185-215` 的 `lVar12 = *(manager + 0x470)` 下标 0→大正序"
          + "｜🧨 改回格号升序 ⇒ 红");
    }

    // ==========================================================================================
    //  D. 件 1·③ —— 替代行动的目标枚举 = 进场序（原版**与主动技能同一个分支** `.c:150-236`）
    // ==========================================================================================
    static void W10D_AlternativeTargetsUsePlayOrder()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        CheckTrue(ctx != null && !ctx.IsOver, "★（前提）这一局还没结束｜🧨 夹具改坏 ⇒ 红");
        if (ctx == null || ctx.IsOver) return;

        int me = ctx.Active, foe = 1 - me;
        var duty = new CardDef("B97W10Duty", "B97W10Duty", "unit", "Duty: Deal 3 damage to an enemy",
                               "common", "Test", 1, 1, 9, 0, new[] { "Duty" }, subtype: "Infantry");
        var holder = Place(ctx, me, 3, duty);
        var e8 = Place(ctx, foe, 8, Unit("B97W10D8", 1, 0, 5));
        var e2 = Place(ctx, foe, 2, Unit("B97W10D2", 1, 0, 5));
        CheckTrue(holder != null && e8 != null && e2 != null,
            "★（前提）职责持有人（我方 3 号格）与两个目标（敌方 8 / 2 号格）都摆上了｜🧨 摆位失效 ⇒ 红");
        if (holder == null || e8 == null || e2 == null) return;
        CheckTrue(e8.PlayOrder < e2.PlayOrder,
            "★（前提）**8 号格那个先进场** ⇒ 进场序 `[8, 2]`、格号序 `[2, 8]`｜🧨 对调 `Place` 顺序 ⇒ 红");

        Check(RuleCore.AvailableAlternative(ctx, me, 3), "duty",
            "★（前提）这一格给出来的是**职责**｜🧨 夹具正文写坏 / `Duty:` 没被收成替代行动 ⇒ 红");

        var all = SimpleAI.EnumerateActions(ctx);
        CheckTrue(all != null && all.Count > 0, "★（前提）动作表非空｜🧨 回合归属闸挡掉整张表 ⇒ 红");
        if (all == null || all.Count == 0) return;

        int i8 = -1, i2 = -1;
        for (int i = 0; i < all.Count; i++)
        {
            var a = all[i];
            if (a.Kind != AiActionKind.ActiveAbility || a.Slot != 3 || a.AltKeyword != "duty") continue;
            if (a.TargetSlot == 8 && i8 < 0) i8 = i;
            if (a.TargetSlot == 2 && i2 < 0) i2 = i;
        }
        CheckTrue(i8 >= 0 && i2 >= 0,
            "★（前提）职责对 8 / 2 号格各有一条候选（i8=" + i8 + " · i2=" + i2 + "）"
          + "｜🧨 `CanPickAlternativeTarget` 被别的闸挡掉（目标规格不匹配）⇒ 红 —— 那是夹具问题，不是实现问题");
        if (i8 < 0 || i2 < 0) return;

        CheckTrue(i8 < i2,
            "★★ 替代行动：**先进场的 8 号格目标先被枚举**（i8=" + i8 + " < i2=" + i2 + "）"
          + " —— 原版 `.c:185-215`：替代行动与主动技能走**同一个** `activeAbility` 分支（`.c:150-236`）"
          + "｜🧨 改回格号升序 ⇒ 红");
    }

    // ==========================================================================================
    //  E. 结构断言 —— 灭自证（防止「实现 + 运行时断言一起改回去」）
    // ==========================================================================================
    static void W10E_StructuralNoRegression()
    {
        string src = W10SourceOf("RuleEngine/Data/SimpleAI.cs");
        CheckTrue(src != null,
            "★（前提）读得到 `RuleEngine/Data/SimpleAI.cs` 源码文本（读不到 ⇒ 下面几条红，⛔ 不静默跳过）"
          + "｜🧨 文件被挪走 / 改名 ⇒ 红");
        if (src == null) return;

        // ① 三处目标枚举各自走 `PlayOrderedSlots`（原版 `+0x470` 正序）
        CheckTrue(src.Contains("PlayOrderedSlots(ctx.Players[tp].Board)"),
            "★★ E1 战术卡：目标枚举走 `PlayOrderedSlots(ctx.Players[tp].Board)`"
          + "｜🧨 改回 `for (int s = 0; s < BoardSpec.Size; s++)` 的格号序 ⇒ 红");
        CheckTrue(src.Contains("PlayOrderedSlots(tb)"),
            "★★ E2 替代行动：目标枚举走 `PlayOrderedSlots(tb)`｜🧨 改回格号序 ⇒ 红");
        CheckTrue(src.Contains("PlayOrderedSlots(foeBoard)"),
            "★★ E3 主动技能：目标枚举走 `PlayOrderedSlots(foeBoard)`｜🧨 改回格号序 ⇒ 红");
        // ② 施放者外循环 / 收灵魂石（同一条轴：`+0x570` 由 `+0x470` 正序填）
        CheckTrue(src.Contains("PlayOrderedSlots(mine.Board)"),
            "★★ E4 施放者外循环 / 收灵魂石：走 `PlayOrderedSlots(mine.Board)`｜🧨 改回格号序 ⇒ 红");

        // ③ 件 2：排序换成稳定助手，且**旧的不稳定写法已消失**
        CheckTrue(src.Contains("StableSortByScoreDesc(list)"),
            "★★ E5 `TweakAvailableActions` 调 `StableSortByScoreDesc(list)`｜🧨 删掉这一句 ⇒ 红");
        CheckTrue(!src.Contains("list.Sort((x, y) => y.Score.CompareTo(x.Score))"),
            "★★ E6 旧的 **不稳定** `list.Sort((x, y) => y.Score.CompareTo(x.Score))` 已不在"
          + "｜🧨 有人把它写回来（introsort 不保证稳定）⇒ 红");

        // ④ 分册文件在位（稳定排序的实现本体）
        string src2 = W10SourceOf("RuleEngine/Data/SimpleAI.StableSort.cs");
        CheckTrue(src2 != null,
            "★（前提）`RuleEngine/Data/SimpleAI.StableSort.cs` 在位｜🧨 被删 / 被并回 `SimpleAI.cs` ⇒ 红");

        // ⑤ 第 ④ 档（`SimpleAI.ActiveEntry.cs`）也走进场序
        string src3 = W10SourceOf("RuleEngine/Data/SimpleAI.ActiveEntry.cs");
        CheckTrue(src3 != null && src3.Contains("PlayOrderedSlots(mine)"),
            "★★ E7 第 ④ 档主动能力（`B88_E3` 那一档）目标枚举也走 `PlayOrderedSlots(mine)`"
          + "｜🧨 改回格号序 / 文件读不到 ⇒ 红");
    }

    /// <summary>读 `Assets/` 下的源码文本；读不到 ⇒ `null`（断言会红，但**不抛**）。</summary>
    static string W10SourceOf(string assetsRelativePath)
    {
        try
        {
            string full = Path.Combine(UnityEngine.Application.dataPath,
                                       assetsRelativePath.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(full) ? File.ReadAllText(full) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
