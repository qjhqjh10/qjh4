// B97_AI_Assert.cs — 执行代理 `B97_AI`（2026-10-29 · 第九十六会话）
//
// 本件是 `RuleEngineTest` 的**分册**（`public static partial class RuleEngineTest`，与
// `B97_RC_Assert.cs` / `RuleEngineTest_EngineB95WBD.cs` 同一种形状）—— `Editor/RuleEngineTest.cs`
// 本批**不在白名单** ⇒ 测试体全写在这里，`RuleEngineTest.cs` 只需在 `Run()` 里加一行
//      Step(CheckB97AIGaps);
// 建议插在 `Step(CheckB97RCGaps);`（`RuleEngineTest.cs` 里那一行）**之后**。
// ⚠️ 本方法名**不以 `Test` / `Report` 打头** ⇒ 不会被 `RunCoverageCompiledCases()` 当成「有 Test* 但没 Step」判红。
//
// ============================ 它盯的是哪一条 ============================
//   AI 动作表的**攻击者 / 目标枚举次序**（`SimpleAI.EnumerateActions` 的「场上单位」那一段）。
//
//   🔴 **判据订正（本代理回源查到的事，⛔ 别按旧说法做）**：
//     任务简报与 `报告/B97_RC.md` §七·2 说「原版 AI 选靶走 `BattleManager.GetAvailableAttackTargets`（**倒序**）」——
//     **不成立**。三源（`decomp_full` / `out_cpp2il_dev_ilspy` / `out_il2csharp`）现扫，那个函数只有两个调用方：
//       · `BattleManager__EndTurnClick.c:78`（**狂暴提示的 UI 检查**，不是 AI 决策）；
//       · `BattleManager__GetRandomAttackTarget.c:15`（而 `GetRandomAttackTarget` **三源 0 调用方** = 死代码）。
//     原版 AI 的真实路径是 `AI.PlayTurn` → `BattleManager.GetAvailableActions` →
//       `GetAvailableAttackActions`（**正序**）：
//       · `BattleManager__GetAvailableActions.c:132` → `GetUnitsInPlayList(manager, 0x570, …)`
//         → `BattleManager__GetUnitsInPlayPooledList.c`：`0x470`（`unitsInPlay`）**从下标 0 往大扫**；
//       · `BattleManager__GetAvailableAttackActions.c`：目标表 `0x580` 同样是 `0x470` **正序**过滤出来。
//     ⇒ AI 的攻击者与目标**都按 `unitsInPlay` 的进场追加序**（本引擎 = `UnitState.PlayOrder` 升序）枚举，
//        **不是格号序**、**更不是倒序**。（倒序口 `RuleCore.AvailableAttackTargetsDesc` 仍保留，但服务的是别的轴。）
//
//   落点 = `RuleEngine/Data/SimpleAI.cs` 的 `EnumerateActions`（「场上单位：近战 / 远程各一条」那一段）
//          ＋ 类尾新助手 `SimpleAI.PlayOrderedSlots`。
//
// ============================ 🔴 灭自证（每条带 🧨）============================
//    ① **前提断言与结论断言并列写**，⛔ 不写 `if (前提) Check(结论)`；
//    ② 期望值一律**字面量**（`7/3/8/2`），⛔ 不从被测实现反算；
//    ③ 补一条**结构断言**：源码里必须是 `PlayOrderedSlots(mine.Board)` 且**没有** `AvailableAttackTargetsDesc(`
//       —— 免得有人「把实现和这三条断言一起改回格号序 / 改成倒序口」仍然全绿。
using System.Collections.Generic;
using System.IO;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口。</summary>
    public static void CheckB97AIGaps()
    {
        Section("🆕 B97_AI：AI 动作表的攻击者/目标枚举次序（原版 `unitsInPlay` 进场序，⛔ 不是格号序・不是倒序口）");
        B97AIAttackEnumerationOrder();
    }

    // ------------------------------------------------------------ A：攻击者 / 目标的枚举次序
    /// <summary>
    /// 判别式 = **格号序 ≠ 进场序**，两侧都造：
    ///   · 攻击者：先摆 7 号格、后摆 3 号格 ⇒ 进场序 `[7, 3]`、格号序 `[3, 7]`；
    ///   · 目标：先摆 8 号格、后摆 2 号格 ⇒ 进场序 `[8, 2]`、格号序 `[2, 8]`。
    /// 原版按进场序（正序）枚举 ⇒ 动作表里**先出 7 号格攻击者、再出 3 号格**；对同一攻击者**先出 8 号格目标**。
    /// </summary>
    static void B97AIAttackEnumerationOrder()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        CheckTrue(ctx != null && !ctx.IsOver,
            "★（前提）这一局还没结束（`EnumerateActions` 在对局结束/非行动方时返回空表 ⇒ 结论会假红）"
          + "｜🧨 夹具建局方式改了 ⇒ 红");
        if (ctx == null || ctx.IsOver) return;

        int me = ctx.Active, foe = 1 - ctx.Active;

        var a7 = Place(ctx, me, 7, Unit("B97AIA7", 1, 2, 5));
        var a3 = Place(ctx, me, 3, Unit("B97AIA3", 1, 2, 5));
        var t8 = Place(ctx, foe, 8, Unit("B97AIT8", 1, 0, 5));
        var t2 = Place(ctx, foe, 2, Unit("B97AIT2", 1, 0, 5));

        CheckTrue(a7 != null && a3 != null && t8 != null && t2 != null,
            "★（前提）四个单位都摆上了｜🧨 摆位失效（格号越界 / 被督军占去）⇒ 红");
        if (a7 == null || a3 == null || t8 == null || t2 == null) return;

        // 前提：两个判别式都真的「格号序 ≠ 进场序」（⛔ 别让夹具自己退化成同一个序）
        CheckTrue(a7.PlayOrder < a3.PlayOrder,
            "★（前提）**7 号格上那个攻击者先进场**（`PlayOrder` 更小）⇒ 进场序 `[7, 3]`、格号序 `[3, 7]`，两者**不同**"
          + "｜🧨 把两次 `Place` 的顺序对调 ⇒ 判别式失效 ⇒ 红");
        CheckTrue(t8.PlayOrder < t2.PlayOrder,
            "★（前提）**8 号格上那个目标先进场** ⇒ 进场序 `[8, 2]`、格号序 `[2, 8]`，两者**不同**"
          + "｜🧨 同上 ⇒ 红");

        // 🔴 **敌人 4 号格上永远是督军**（`PlayerState.cs:68`）⇒ 不处理的话它也会进目标表，次序断言随之失真。
        //    原版现成的一道闸：「攻击者带召唤病 ∧ 不带 `fast` ⇒ 打不了敌方督军」
        //    （`RuleCore.IsValidTarget`，判据 `BattleManager__IsValidAttackTarget.c:199-202`）。
        a7.SummonSickness = true; a3.SummonSickness = true;
        CheckTrue(ctx.Players[foe].Board[BoardSpec.WarlordSlot] != null,
            "★（前提）敌人 4 号格上**确实是督军**（所以上面那句 `SummonSickness = true` 是必须的，不是装饰）"
          + "｜🧨 开局摆法改了（督军不上棋盘）⇒ 红");

        // 前提：正序口（`RawCardScript.GetValidTargets`）确认夹具的目标集合与判别式
        var asc = RuleCore.ValidTargetSlots(ctx, me, 7, ranged: false);
        Check(asc.Count, 2,
            "★（前提）7 号格攻击者的合法目标**只有那两个兵**（督军已被召唤病闸挡掉）｜🧨 出了别的禁攻关键词 / 督军闸失效 ⇒ 红（那时次序无从比较）");
        if (asc.Count == 2)
        {
            Check(asc[0].slot, 8,
                "★（前提）`ValidTargetSlots` 首项是 **8 号格**（先进场的那个）｜🧨 改回「格号升序」⇒ 首项是 2 ⇒ 红");
            Check(asc[1].slot, 2,
                "★（前提）次项是 **2 号格**（后进场的那个）｜🧨 同上 ⇒ 红");
        }

        var all = SimpleAI.EnumerateActions(ctx);
        CheckTrue(all != null && all.Count > 0,
            "★（前提）动作表非空｜🧨 两档闸（`me != ctx.Active`）挡掉了整张表 ⇒ 红");
        if (all == null || all.Count == 0) return;

        // ---------------- 结论 1：攻击者的枚举次序 = 进场序 ----------------
        int i7 = -1, i3 = -1;
        for (int i = 0; i < all.Count; i++)
        {
            var a = all[i];
            if (a.Kind != AiActionKind.AttackMelee) continue;
            if (a.Slot == 7 && a.TargetSlot == 8 && i7 < 0) i7 = i;
            if (a.Slot == 3 && a.TargetSlot == 8 && i3 < 0) i3 = i;
        }
        CheckTrue(i7 >= 0 && i3 >= 0,
            "★（前提）两名攻击者打 **8 号格**的近战动作都在表里（i7=" + i7 + " · i3=" + i3 + "）"
          + "｜🧨 攻击被别的闸挡掉 ⇒ 红（那时下面那条无从比较）");
        CheckTrue(i7 >= 0 && i3 >= 0 && i7 < i3,
            "★★ **先进场的攻击者（7 号格）先被枚举** —— 原版 `GetAvailableActions.c:132` → `GetUnitsInPlayPooledList`"
          + " 从 `0x470` 下标 0 **正序**扫（⛔ 不是格号序、⛔ 更不是倒序）"
          + "｜🧨 改回格号升序（3 号格在前）或改调倒序口 ⇒ 红");

        // ---------------- 结论 2：目标的枚举次序 = 进场序 ----------------
        int t8Idx = -1, t2Idx = -1;
        for (int i = 0; i < all.Count; i++)
        {
            var a = all[i];
            if (a.Kind != AiActionKind.AttackMelee || a.Slot != 7) continue;
            if (a.TargetSlot == 8 && t8Idx < 0) t8Idx = i;
            if (a.TargetSlot == 2 && t2Idx < 0) t2Idx = i;
        }
        CheckTrue(t8Idx >= 0 && t2Idx >= 0 && t8Idx < t2Idx,
            "★★ 对**同一个攻击者**，**先进场的目标（8 号格）先被枚举** —— 原版 `GetAvailableAttackActions.c` 的目标表"
          + " `0x580` 也是 `0x470` 正序过滤（t8=" + t8Idx + " · t2=" + t2Idx + "）"
          + "｜🧨 改成格号升序 / 倒序 ⇒ 红");

        // ---------------- 结论 3：同分时「先枚举的赢」（严格 `>`），于是选中先进场的那个 ----------------
        // 只取「两名攻击者 × 两个兵」这 4 条近战动作、**分数全给 1** ⇒ 排掉打分差异，只看次序。
        var sub = new List<AiAction>();
        foreach (var a in all)
        {
            if (a.Kind != AiActionKind.AttackMelee) continue;
            if ((a.Slot == 7 || a.Slot == 3) && (a.TargetSlot == 8 || a.TargetSlot == 2))
            {
                a.Score = 1f;
                sub.Add(a);
            }
        }
        Check(sub.Count, 4,
            "★（前提）挑出「2 攻击者 × 2 目标」共 4 条近战动作（每人每目标各一条）｜🧨 少一条 / 多一条 ⇒ 红");
        var best = SimpleAI.SelectBestAction(ctx, sub);
        CheckTrue(best != null,
            "★（前提）`SelectBestAction` 有返回值（空表会返回 null ⇒ 结论会假绿）｜🧨 ⇒ 红");
        if (best != null)
        {
            Check(best.Slot, 7,
                "★★ 同分时选中**先进场的攻击者（7 号格）** —— `SelectBestAction` 严格 `>`（平手留先）∧ 枚举序 = 进场序"
              + "｜🧨 枚举改回格号序 ⇒ 选中 3 号格 ⇒ 红");
            Check(best.TargetSlot, 8,
                "★★ 且选中**先进场的目标（8 号格）**｜🧨 目标枚举改成倒序（先进场的排后面）⇒ 选中 2 ⇒ 红（那正是把 `AvailableAttackTargetsDesc` 接进 AI 会造成的形状）");
        }

        // ---------------- 结构断言（灭自证：防止「实现 + 断言一起改回去」）----------------
        string src = B97AISourceOf("RuleEngine/Data/SimpleAI.cs");
        CheckTrue(src != null,
            "★（前提）读得到 `RuleEngine/Data/SimpleAI.cs` 的源码文本（读不到 ⇒ 下两条红，⛔ 不静默跳过）"
          + "｜🧨 文件被挪走 / 改了名 ⇒ 红");
        CheckTrue(src != null && src.Contains("PlayOrderedSlots(mine.Board)"),
            "★★ 攻击者枚举**仍走** `PlayOrderedSlots(mine.Board)`（= 进场序助手）"
          + "｜🧨 有人把那一行改回 `for (int s = 0; s < BoardSpec.Size; s++)` 的格号序 ⇒ 红");
        CheckTrue(src != null && !src.Contains("AvailableAttackTargetsDesc("),
            "★★ `SimpleAI` **没有**转调 `AvailableAttackTargetsDesc(…)` —— 那个口**是倒序**（`.c:23-33`），"
          + "而原版 AI 走的不是它（它的调用方只有 `EndTurnClick` 与死代码 `GetRandomAttackTarget`）"
          + "｜🧨 有人「按旧简报」把它接进 AI ⇒ 红");
    }

    // ------------------------------------------------------------ 共用：读源码（结构断言用）
    /// <summary>读 `Assets/` 下的源码文本；读不到 ⇒ `null`（断言会红，但**不抛**）。</summary>
    static string B97AISourceOf(string assetsRelativePath)
    {
        try
        {
            string full = Path.Combine(UnityEngine.Application.dataPath,
                                       assetsRelativePath.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(full) ? File.ReadAllText(full) : null;
        }
        catch (System.Exception)
        {
            return null;
        }
    }
}
