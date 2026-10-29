// B99A1_Assert.cs — 第九十九会话 · 波 3 · 组 A（`BattleDriver.cs`）执行代理 `B99_A1` 的断言
//   施工单 = `资料/完全复刻/判据/B99_R3a_施工单_战斗按文件.md` §一「组 A」那一节（11 条）。
//   报告 = `资料/完全复刻/报告/B99_A1.md`（含逐条结论 + 行号漂移表 + 接线指令）。
//
// ============================ 它盯的是什么（本批真的改了代码的那 4 条）============================
//  ① **AUD8 · Phrase 词条**（施工单 §六 反向清单 #2）：`THIS UNIT CANNOT ACT` 的两处 `NotifyCantDoAction`
//     **必须已经删掉**（原版 `CardScript__TryDraggingFromBoard.c:35-38` 是裸 `return`、完全静默；
//     官方 I2 表 `Battle/*` 156 键里 `Phrase/*` = 0 条）。词条本身**仍然保留**（删表项会牵动 `CardBaseDemo` 的扫描）。
//  ② **AUD8 · 拖空出声**（反向清单 #1）：出声的判据收窄成 **`Targeted == 1 && !IsUnit`**
//     （原版 `BattleManager.cs:26054/26677/26685/26693` 四处全在 `playingTargetedSpell` / `targetedActiveAbility`；
//      `case 3 playingMinion` / `case 5` 松手支 **0 处**）⇒ 判据本体 = `BattleDriver.NeedsTargetOnDrag(CardDef)`。
//  ③ **AUD8 · Any 并集**：`targetsAffected = any` 的候选 = **敌我两侧并集**（原版 `GetUnitsInPlayList(…, both=true)`；
//     il2csharp `AbilityLogic.cs:3824-3827` 的 `inclPlayer=1 ∧ inclEnemy=1`）⇒ 表现层用哨兵 `TargetSideBoth = -2`，
//     消费点两侧都试（**敌先友后**，与 `RuleCore.AbilityPickOwnersOf` 的 `Any` 同序）。
//     ⛔ 表现层**不再有**自己那份 `AbilityPickSide` / `AbilityPickSideOf`（已转发 `RuleCore` 那一份 —— 判据只留一处）。
//  ④ **T09-新账 · tooltip**：多卡展示窗**逐张**都挂 tooltip（原版 `UIMultiCardDisplay__Initialize.c:91`
//     逐卡 `ToggleTooltips(card2d, enableCardTooltips(=1), 0)`）⇒ `TipMultiCards(Vector3)` 的廉价前置门必须存在。
//  ⑤ **#9 `TermResolver`**：`RuleCore.TermResolver` 挂上 `Loc.T` 之后，替代行动的显示名**真的走本地化**
//     （不挂 = 永远中文兜底，而且 `UnresolvedActionNameKeys` 会记账）。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B99A1_Assert.Run -logFile "d:/4/_tmp_view/b99a1.log"
//   🔴 **接线由调度台做**（`Editor/BattleScene.cs` 是共享断言宿主，不在本代理白名单）：在 `BattleScene.Run()`
//      里插一行 `B99A1_Assert.Run();` 即可（本类自带 `Check` / 计数，不依赖宿主的 `Check`）。
//   判绿 = 末行 `合计：N 通过 / 0 失败`；出现 `… 未验到：` 时**别读成通过**（那是夹具缺件，不是实现错）。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 把 `NotifyCantDoAction(CardText.Phrase("THIS UNIT CANNOT ACT"))` 任一处加回来 ⇒ §A 红。
//   · 把 `NeedsTargetOnDrag` 改成 `d.Targeted != 0`、或去掉 `!d.IsUnit`、或改成恒真 ⇒ §B 红（B2/B3/B4 各自红）。
//   · 把 `TargetSideBoth` 改回 `-1` / 删掉 `SideCount`/`SideAt` 的哨兵分支 ⇒ §C 红（C2/C3/C4）。
//   · 在 `BattleDriver` 里**再写一份**本地 `AbilityPickSide` 枚举 ⇒ §D 红（结构断言：嵌套类型必须不存在）。
//   · 删掉 `TipMultiCards` 里的 `_multiCards == null || !Visible` 前置门 ⇒ §E 红（NRE / 返回真）。
//   · 让 `RuleCore.TermResolver` 恒为 null（或把 `Loc.T` 的接线删掉）⇒ §F 红（正控那一半拿不到本地化名）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using CardPresentation;
using RuleEngine;
using UnityEditor;
using UnityEngine;

public static class B99A1_Assert
{
    const string P = "B99A1 ";

    static int _ok, _bad;

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; } else { _bad++; }
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    /// <summary>出声的「**未验到**」—— ⛔ 别读成通过（本仓坑表 #18：断言缺前提时静默跳过）。</summary>
    static void NotVerified(string label) { Debug.LogWarning(P + "… 未验到：" + label); }

    /// <summary>🔴 本仓纪律：断言不许写成 `if (前提) Check(结论)`（前提不成立会**静默跳过**）。
    /// 前提单列一条 `Check`，结论另列一条 —— 两件事都要红。</summary>
    static void CheckWithPremise(bool premise, string premiseLabel, bool conclusion, string conclusionLabel)
    {
        Check(premise, premiseLabel);
        Check(conclusion, conclusionLabel);
    }

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "===== B99_A1（组 A：Phrase 静默 / 拖空判据 / Any 并集 / tooltip 逐张 / TermResolver）开始 =====");

        CheckPhraseSilenced();
        CheckNeedsTargetOnDrag();
        CheckAnySideSentinel();
        CheckNoLocalAbilityPickSide();
        CheckTipMultiCardsGate();
        CheckTermResolverWiring();

        Debug.Log(P + $"===== 合计：{_ok} 通过 / {_bad} 失败 =====");
    }

    // ==================================================================
    //  §A  源码级：`THIS UNIT CANNOT ACT` 的两处调用已删（词条本身仍留着）
    // ==================================================================
    static void CheckPhraseSilenced()
    {
        const string rel = "Assets/CardPresentation/Battle/BattleDriver.cs";
        var abs = Path.Combine(Directory.GetCurrentDirectory(), rel);
        bool exists = File.Exists(abs);
        Check(exists, "§A0 （前提）找得到 `" + rel + "`（Editor 下 `CurrentDirectory` = 工程根）");
        if (!exists) { NotVerified("§A 整段（源码文件不在预期位置）"); return; }

        var raw = File.ReadAllText(abs);
        int calls = 0, allShortCircuits = 0;
        foreach (var line in raw.Split('\n'))
        {
            var s = line.Trim();
            if (s.StartsWith("//")) continue;                       // ⛔ 注释里提到这个函数名不算调用
            if (s.Contains("NotifyCantDoAction(CardText.Phrase(\"THIS UNIT CANNOT ACT\"))")) calls++;
            if (s.Contains("if (!melee && !ranged && !skill)")) allShortCircuits++;
        }
        Check(allShortCircuits == 2,
              "§A1 （前提）「三项全无」那两条闸还在（`if (!melee && !ranged && !skill)` 现读 "
            + allShortCircuits + " 处，期望 2）—— 本段只删**提示**，⛔ 别把 return 一起删掉");
        Check(calls == 0,
              "★ A2 🔴 `NotifyCantDoAction(CardText.Phrase(\"THIS UNIT CANNOT ACT\"))` **0 处**（原版 "
            + "`CardScript__TryDraggingFromBoard.c:35-38` 裸 return、完全静默；官方表也没有这个键）｜"
            + "🧨 任一处加回来 ⇒ 红");

        // 判别式：词条**本身**必须还在（删表项会牵动 `CardBaseDemo` 那组扫描）
        bool phraseStillThere = false;
        try { phraseStillThere = CardText.Phrase("THIS UNIT CANNOT ACT") != null; } catch { }
        Check(phraseStillThere,
              "§A3 词条本身仍在（`CardText.Phrase(\"THIS UNIT CANNOT ACT\")` 不抛、不返回 null）—— 只是**没人调它**了");
    }

    // ==================================================================
    //  §B  `NeedsTargetOnDrag` 判据（= 原版那 4 处 `NotifyCantDoAction` 的覆盖面）
    // ==================================================================
    static void CheckNeedsTargetOnDrag()
    {
        var mi = typeof(BattleDriver).GetMethod("NeedsTargetOnDrag",
                    BindingFlags.NonPublic | BindingFlags.Static);
        Check(mi != null, "§B0 （前提）反射找得到 `BattleDriver.NeedsTargetOnDrag(CardDef)`（private static）");
        if (mi == null) { NotVerified("§B 整段（找不到这个判据 —— 改名了？）"); return; }

        Func<CardDef, bool> f = d => (bool)mi.Invoke(null, new object[] { d });

        Check(f(null) == false,
              "★ B1 `null` ⇒ 假（⛔ 不抛）");
        var unitTargeted = B99A1Card("B99A1UnitT", "troop", targeted: 1);
        var tacticTargeted = B99A1Card("B99A1TacT", "tactic", targeted: 1);
        var tacticPlain = B99A1Card("B99A1TacP", "tactic", targeted: 0);
        var tacticUnknown = B99A1Card("B99A1TacU", "tactic", targeted: -1);

        CheckWithPremise(unitTargeted.IsUnit, "§B2a （前提）夹具 `B99A1UnitT` 确实是**单位卡**（`IsUnit`）",
                         f(unitTargeted) == false,
                         "★ B2 🔴 **单位卡 + targeted** ⇒ **假**（原版 `case 3 playingMinion` 松手支 0 处 ⇒ 拖空不出声）"
                       + "｜🧨 去掉 `!d.IsUnit` ⇒ 红");
        CheckWithPremise(tacticTargeted.IsUnit == false && tacticTargeted.Targeted == 1,
                         "§B3a （前提）夹具 `B99A1TacT` = 非单位 ∧ `Targeted == 1`",
                         f(tacticTargeted) == true,
                         "★ B3 🔴 **目标型非单位卡** ⇒ **真**（原版 `playingTargetedSpell` 那两支才出声）｜🧨 改成恒假 ⇒ 红");
        Check(f(tacticPlain) == false,
              "★ B4 `Targeted == 0` 的战术卡 ⇒ 假（原版 `case 5 playingNonTargetSpell` 松手支 0 处）｜"
            + "🧨 写成 `Targeted != 0` ⇒ 红");
        Check(f(tacticUnknown) == false,
              "★ B5 `Targeted == -1`（三态里的「未知」）⇒ 假（⛔ 别把「未知」当「目标型」）");

        // 判别式：与 `IsDraggingTargetedSpell` 必须**同一条谓词**（那边已改成转发本判据）
        var drag = typeof(BattleDriver).GetMethod("IsDraggingTargetedSpell", BindingFlags.NonPublic | BindingFlags.Instance);
        Check(drag != null,
              "§B6 （前提）`IsDraggingTargetedSpell` 还在（拖法术准星那条路的行为不能丢）");
    }

    // ==================================================================
    //  §C  Any 并集：哨兵 `TargetSideBoth = -2` + 两侧枚举（敌先友后）
    // ==================================================================
    static void CheckAnySideSentinel()
    {
        var fBoth = typeof(BattleDriver).GetField("TargetSideBoth", BindingFlags.NonPublic | BindingFlags.Static);
        Check(fBoth != null, "§C0 （前提）反射找得到 `const int BattleDriver.TargetSideBoth`");
        if (fBoth == null) { NotVerified("§C 整段（哨兵没建/改名了）"); return; }
        int both = (int)fBoth.GetRawConstantValue();
        Check(both == -2,
              "★ C1 `TargetSideBoth == -2`（必须是 `-2`：`-1` 已被「不用点目标」占用、`0/1` 是座位）｜"
            + "🧨 改成 -1 ⇒ 红（会与「无需目标」撞档）");

        var go = new GameObject("B99A1Probe");
        try
        {
            var drv = go.AddComponent<BattleDriver>();
            Check(drv != null, "§C2a （前提）`BattleDriver` 能挂到探针对象上（运行时程序集 + 有脚本资产）");
            if (drv == null) { NotVerified("§C2 整段（挂不上）"); return; }
            // 编辑器里 `AddComponent` **不触发 `Awake/Start`**（本仓坑表）⇒ `_me` 还是默认 0
            var meF = typeof(BattleDriver).GetField("_me", BindingFlags.NonPublic | BindingFlags.Instance);
            int me = meF != null ? (int)meF.GetValue(drv) : 0;
            var mCount = typeof(BattleDriver).GetMethod("SideCount", BindingFlags.NonPublic | BindingFlags.Instance);
            var mAt = typeof(BattleDriver).GetMethod("SideAt", BindingFlags.NonPublic | BindingFlags.Instance);
            Check(mCount != null && mAt != null,
                  "§C2b （前提）反射找得到 `SideCount(int)` / `SideAt(int,int)`");
            if (mCount == null || mAt == null) { NotVerified("§C2 整段（两个 helper 找不到）"); return; }

            Func<int, int> cnt = s => (int)mCount.Invoke(drv, new object[] { s });
            Func<int, int, int> at = (s, i) => (int)mAt.Invoke(drv, new object[] { s, i });

            Check(cnt(-2) == 2, "★ C2 哨兵档 = **2 个候选**（两侧都试）｜🧨 `SideCount` 不认 -2 ⇒ 红");
            Check(at(-2, 0) == 1 - me && at(-2, 1) == me,
                  "★ C3 🔴 次序 = **敌先友后**（`SideAt(-2,0)=" + (1 - me) + "` / `SideAt(-2,1)=" + me + "`，`_me=" + me + "`）"
                + "—— 与引擎 `RuleCore.AbilityPickOwnersOf` 的 `Any` 档同序（联机/回放两端因此必然一致）｜🧨 反过来 ⇒ 红");
            Check(cnt(me) == 1 && at(me, 0) == me,
                  "★ C4 单侧档（`_me`）只试 1 个、且就是它自己（非 both 时行为与改前逐位相同）");
            Check(cnt(-1) == 0,
                  "★ C5 `-1`（这一手不用点目标）⇒ **0 个候选**（⛔ 别把「不用点」也当成一侧）｜᛭ 与 `CommandTargetSideForTest` 的读数一致");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ==================================================================
    //  §D  表现层**不再**有自己那份 `AbilityPickSide` / `AbilityPickSideOf`（判据只留一处）
    // ==================================================================
    static void CheckNoLocalAbilityPickSide()
    {
        var nested = typeof(BattleDriver).GetNestedType("AbilityPickSide", BindingFlags.NonPublic | BindingFlags.Public);
        Check(nested == null,
              "★ D1 🔴 `BattleDriver` 里**没有**嵌套枚举 `AbilityPickSide`（已删 —— 转发 `RuleCore.AbilityPickSide`）｜"
            + "🧨 再写一份本地枚举 ⇒ 红（两处写同一条规则 = 迟早不一致）");

        var m = typeof(BattleDriver).GetMethod("AbilityPickSideOf",
                    BindingFlags.NonPublic | BindingFlags.Instance);
        Check(m != null && m.ReturnType == typeof(RuleCore.AbilityPickSide),
              "★ D2 `BattleDriver.AbilityPickSideOf` 的**返回类型就是** `RuleCore.AbilityPickSide`（= 转发，不是副本）｜"
            + "🧨 返回本地枚举/自己算一遍 ⇒ 红");

        Check((int)RuleCore.AbilityPickSide.Unknown == 0 && (int)RuleCore.AbilityPickSide.Enemy == 1
              && (int)RuleCore.AbilityPickSide.Friendly == 2 && (int)RuleCore.AbilityPickSide.Any == 3,
              "★ D3 `RuleCore.AbilityPickSide` 值域 = `0/1/2/3`（`AbilityPickSideForTest` 的读数口径不变）");
    }

    // ==================================================================
    //  §E  多卡展示窗 tooltip 的**廉价前置门**（没窗 ⇒ 立刻假，⛔ 不 NRE、⛔ 不遍历）
    // ==================================================================
    static void CheckTipMultiCardsGate()
    {
        var mi = typeof(BattleDriver).GetMethod("TipMultiCards", BindingFlags.NonPublic | BindingFlags.Instance);
        Check(mi != null, "§E0 （前提）反射找得到 `TipMultiCards(Vector3)`（private instance）");
        if (mi == null) { NotVerified("§E 整段（找不到 —— 改名了？）"); return; }

        var go = new GameObject("B99A1ProbeTip");
        try
        {
            var drv = go.AddComponent<BattleDriver>();
            var fMult = typeof(BattleDriver).GetField("_multiCards", BindingFlags.NonPublic | BindingFlags.Instance);
            CheckWithPremise(drv != null && fMult != null,
                             "§E1a （前提）拿到 `_multiCards` 字段",
                             drv != null && fMult != null && fMult.GetValue(drv) == null,
                             "§E1b （前提）探针实例上 `_multiCards == null`（编辑器里 `AddComponent` 不跑 `Awake`）");
            if (drv == null) { NotVerified("§E2 整段（挂不上）"); return; }

            bool r;
            try { r = (bool)mi.Invoke(drv, new object[] { Vector3.zero }); }
            catch (Exception e) { Check(false, "★ E2 🔴 `TipMultiCards` 在**没有多卡窗**时不许抛：" + e.GetType().Name); return; }
            Check(r == false,
                  "★ E2 🔴 没有多卡窗 ⇒ **假**（⛔ 不是「遍历空表刚好也假」—— 是**前置门**；见 E3）｜🧨 删掉 `_multiCards == null` 那道门 ⇒ 红");
            Check(drv.TooltipCandidateEvalsForTest == 0,
                  "★ E3 前置门在前面：`_multiCards == null` 时**一次候选评估都不做**（计数器仍 0）｜"
                + "🧨 把门挪到循环之后 ⇒ 红（每帧对空窗白跑）");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ==================================================================
    //  §F  #9 `TermResolver`：挂上 `Loc.T` 之后替代行动的显示名**真的走本地化**
    // ==================================================================
    static void CheckTermResolverWiring()
    {
        var prev = RuleCore.TermResolver;
        try
        {
            string key = RuleCore.AlternativeActionTermKey("duty");
            Check(key != null && key.Length > 0,
                  "§F0 （前提）`AlternativeActionTermKey(\"duty\")` 给出词条键（现读 `" + key + "`）");
            Check(Loc.T(key) != key,
                  "§F1 （前提）`Loc.T(\"" + key + "\")` 在表里有值（不是回键名本身）—— 否则「走本地化」这一问测不出来");
            if (key == null || Loc.T(key) == key) { NotVerified("§F 正控（`Card_Trait/duty` 在 `Loc` 表里查不到）"); }

            RuleCore.TermResolver = Loc.T;
            var viaResolver = RuleCore.AlternativeActionName("duty");
            Check(key != null && viaResolver == Loc.T(key),
                  "★ F2 🔴 挂上 `RuleCore.TermResolver = Loc.T` ⇒ `AlternativeActionName(\"duty\")` **等于** `Loc.T(`" + key + "`)`"
                + "（现读 `" + viaResolver + "`）｜🧨 接线被删/被挪到不走的地方 ⇒ 红");

            // 负控（灭自证）：**不挂**解析器 ⇒ 必须退中文兜底、并把键记进未解析账
            RuleCore.TermResolver = null;
            string fKey = RuleCore.AlternativeActionTermKey("ferocity");
            int before = RuleCore.UnresolvedActionNameKeys != null ? RuleCore.UnresolvedActionNameKeys.Count : 0;
            var fallback = RuleCore.AlternativeActionName("ferocity");
            int after = RuleCore.UnresolvedActionNameKeys != null ? RuleCore.UnresolvedActionNameKeys.Count : 0;
            Check(fKey != null && fallback == RuleCore.AlternativeActionNameFallback("ferocity"),
                  "★ F3 负控：**没挂**解析器 ⇒ 回中文兜底（现读 `" + fallback + "`）—— 证明 F2 那一条真的经过了 `TermResolver`｜"
                + "🧨 把 `TermResolver` 的读点删掉（永远走兜底）⇒ F2 红这条仍假绿，所以 F2 才是主判据");
            bool recorded = false;
            if (RuleCore.UnresolvedActionNameKeys != null)
                for (int i = 0; i < RuleCore.UnresolvedActionNameKeys.Count; i++)
                    if (RuleCore.UnresolvedActionNameKeys[i] == fKey) { recorded = true; break; }
            Check(fKey != null && (recorded || after > before),
                  "★ F4 没挂解析器时**出声**：`UnresolvedActionNameKeys` 里记了这条键（⛔ 不静默）");
        }
        finally
        {
            RuleCore.TermResolver = prev;
        }
    }

    /// <summary>夹具卡：`type` / `targeted` 由调用方给（其余走 `CardDef` 缺省）。
    /// <para>⚠️ `Targeted` 是**三态 int**（`-1` 未知 / `1` 是 / 其余 0）—— ⛔ 别把它当 bool。</para></summary>
    static CardDef B99A1Card(string id, string type, int targeted)
    {
        return new CardDef(id, id, type, "", null, "SpaceWolves", 1, 1, 3, 0, null,
                           subtype: type, targeted: targeted);
    }
}
