// B99B1_Assert.cs — 第九十九会话 · 组 B（`RuleCore.cs`）+ 组 F（`CardInteraction.cs`）执行代理 `B99_B1` 的断言
//   施工单 = `资料/完全复刻/判据/B99_R3a_施工单_战斗按文件.md` §一「组 B（5 条）」与「组 F（1 条）」。
//   报告   = `资料/完全复刻/报告/B99_B1.md`（逐条结论 + 行号漂移段 + 接线指令）。
//
// ============================ 它盯的是什么（本批真的改了代码的 6 条）============================
//  ① **AUD8 · 先锋豁免**（`RuleCore.IsValidTarget` 的 Flying 闸）：原判据 `.c:177-181` 的四个析取项里
//     **有 `∨ HasCurrentTrait(目标, 0x3d4 /*vanguard*/)`** —— 先锋**豁免**飞行闸。改前我们漏了这一档。
//  ② **AUD6 · precognition**：目标带 `precognition(640)` ⇒ 不合法（`.c:136-141`，原版只有 debug 日志、
//     **没有** tip 词条 ⇒ 我们复用 `ErrTarget`）。
//  ③ **AUD6 · tainted（攻击侧）**：攻方是督军 ∧ 目标带 `tainted(160)` ∧ 目标非先锋 ⇒ 不合法（`.c:185-191`）。
//  ④ **AUD6 · coward**：攻方带 `coward(190)` ∧ 目标是督军 ⇒ 不合法（`.c:203-211`）。
//  ⑤ **RE1·R6**：死讯信号（`death-signal:`）必须排在**反噬(80)之后、不稳定(81)之前**（原版同一队列体
//     `RawCardScript__OnMinionDeath.c:19/22/23` = 80 → 81 → 175/180/170）。
//  ⑥ **AUD8 · 让位预览**（`CardInteraction.Release`）：`DropAccepted` 必须在 `ClearDropIndicator()` **之前**
//     摆好（否则 `EndPreviewReturn` 读到**上一手**的残留值 ⇒ 被推开的单位停在假格位）。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B99B1_Assert.Run -logFile "d:/4/_tmp_view/b99b1.log"
//   🔴 **接线由调度台做**（`Editor/BattleScene.cs` / `RuleEngine/Editor/RuleEngineTest.cs` 都不在本代理白名单）：
//      在任一宿主的 `Run()` 里插一行 `B99B1_Assert.Run();` 即可 —— 本类自带 `Check` 与计数，不依赖宿主的 `Check`。
//   判绿 = 末行 `合计：N 通过 / 0 失败`；出现 `… 未验到：` 时**别读成通过**（那是夹具缺件，不是实现错）。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · ① A1b：把 `&& !target.Has(KeywordTable.Vanguard)` 从 Flying 闸删掉 ⇒ A1 红。
//   · ② P2：删掉 `if (target.Has(Precognition))` 那一段 ⇒ P1 红。
//   · ③ T2：把 `!HasVanguardTrait(target)` 删掉 ⇒ T2 红（带 vanguard 的目标也被拒）。
//   · ④ C2：把 `Coward` 常量值改成别的词 / 删闸 ⇒ C1 红。
//   · ⑤ R6：把 `death-signal:` 那个块挪回反噬**之前** ⇒ R1 红。
//   · ⑥ F1/F2/F3：把 `DropAccepted = ok && …` 挪到 `ClearDropIndicator()` **之后**、或把
//     `DropAccepted = !tactic;` 加回来 ⇒ 红。
using System;
using System.Collections.Generic;
using System.IO;
using RuleEngine;
using UnityEngine;

public static class B99B1_Assert
{
    const string P = "B99B1 ";

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
        Debug.Log(P + "===== B99_B1（组 B：先锋豁免 / precognition / tainted / coward / RE1-R6 + 组 F：让位预览）开始 =====");

        SectionVanguardFlyingExemption();
        SectionPrecognition();
        SectionTainted();
        SectionCoward();
        SectionDefenderIgnoresPrecognition();
        SectionDeathSignalOrder();
        SectionPreviewDropAcceptedOrder();

        Debug.Log(P + $"===== 合计：{_ok} 通过 / {_bad} 失败 =====");
    }

    // ==================================================================
    //  夹具（⛔ 不依赖 `RuleEngineTest` 的私有助手）
    // ==================================================================
    static CardDef UnitCard(string name, int atk, int hp)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 1, atk, hp, 0, null);
    }

    static CardDef HeroCard(string name, string faction, int atk, int hp)
    {
        // ⚠️ `subtype` 必须给 `"Warlord"`（真卡的 56 个督军全是它；见 `RuleEngineTest.HeroOf` 的 doc）
        return new CardDef(name, name, "hero", "", null, faction, 0, atk, hp, 0, null,
                           subtype: "Warlord");
    }

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { HeroCard("B99B1HeroA", "Ultramarines", 2, 30) };
        var d1 = new List<CardDef> { HeroCard("B99B1HeroB", "Goff", 2, 30) };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef c)
    {
        var u = new UnitState(ctx.NewInstance(c), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>某个座位督军所在的那一格（⛔ 不写死 4，按对象反查）。</summary>
    static int WarlordSlotOf(BattleContext ctx, int p)
    {
        var w = ctx.Players[p].Warlord;
        if (w == null) return -1;
        var b = ctx.Players[p].Board;
        for (int s = 0; s < b.Length; s++) if (ReferenceEquals(b[s], w)) return s;
        return -1;
    }

    static string CodeName(int code)
    {
        // 只给日志用（⛔ 不参与任何判定）
        if (code == RuleCodes.OK) return "OK";
        if (code == RuleCodes.ErrTarget) return "ErrTarget";
        if (code == RuleCodes.ErrAttackBlockFlying) return "ErrAttackBlockFlying";
        if (code == RuleCodes.ErrAttackBlock) return "ErrAttackBlock";
        if (code == RuleCodes.ErrSelf) return "ErrSelf";
        if (code == RuleCodes.ErrNotUnit) return "ErrNotUnit";
        return "code=" + code;
    }

    // ==================================================================
    //  ① AUD8 · 先锋豁免（Flying 闸漏了 `!vanguard`）
    // ==================================================================
    static void SectionVanguardFlyingExemption()
    {
        Debug.Log(P + "---- ① AUD8·先锋豁免（`BattleManager__IsValidAttackTarget.c:177-181`）----");
        var ctx = NewCtx();
        var atk = Place(ctx, 0, 2, UnitCard("B99B1AtkM", 3, 9));
        var tgt = Place(ctx, 1, 3, UnitCard("B99B1FlyVan", 1, 9));
        tgt.AddKeyword(KeywordTable.Flying, 1);
        tgt.AddKeyword(KeywordTable.Vanguard, 1);

        Check(tgt.Has(KeywordTable.Flying) && tgt.Has(KeywordTable.Vanguard),
              "①0 （前提）目标确实是 flying ∧ vanguard 两个都在（夹具没失真）");
        Check(atk.Has(KeywordTable.Flying) == false, "①0b （前提）攻方**不会飞**、也不带 vanguard");

        int got = RuleCore.IsValidTarget(ctx, 0, 2, 1, 3, ranged: false);
        Check(got == RuleCodes.OK,
              "★ ①1 🔴 近战打「会飞 ∧ 是先锋」的目标 ⇒ **合法**（原版 `.c:179` 的 `∨ vanguard(目标)` 让它豁免飞行闸）"
            + "｜实际=" + CodeName(got) + "｜🧨 删掉 Flying 闸里那句 `&& !target.Has(Vanguard)` ⇒ 红");

        // 灭自证：把 vanguard 摘掉 ⇒ 必须恢复成「被飞行闸挡住」
        tgt.RemoveAll(KeywordTable.Vanguard);
        int got2 = RuleCore.IsValidTarget(ctx, 0, 2, 1, 3, ranged: false);
        Check(got2 == RuleCodes.ErrAttackBlockFlying,
              "★ ①1b **灭自证**：同一个目标摘掉 vanguard 之后必须**变回** ErrAttackBlockFlying"
            + "（证明上面那次放行确实来自这条豁免）｜实际=" + CodeName(got2));
    }

    // ==================================================================
    //  ② AUD6 · precognition（目标带它 ⇒ 不合法）
    // ==================================================================
    static void SectionPrecognition()
    {
        Debug.Log(P + "---- ② AUD6·precognition（`BattleManager__IsValidAttackTarget.c:136-141`）----");
        var ctx = NewCtx();
        Place(ctx, 0, 2, UnitCard("B99B1AtkP", 3, 9));
        var tgt = Place(ctx, 1, 3, UnitCard("B99B1Precog", 1, 9));
        tgt.AddKeyword(RuleCore.Precognition, 1);

        Check(tgt.Has(RuleCore.Precognition), "②0 （前提）目标是 `precognition`（常量值 = " + RuleCore.Precognition + "）");

        int got = RuleCore.IsValidTarget(ctx, 0, 2, 1, 3, ranged: false);
        Check(got == RuleCodes.ErrTarget,
              "★ ②1 打带 precognition 的目标 ⇒ 不合法（原版 `CustomDebug.Log(\"Can't attack … due to Precognition\") + return 0`；"
            + "官方 13 语表**没有** `Battle/Tips/CantAttackPrecognition` ⇒ 复用 ErrTarget）"
            + "｜实际=" + CodeName(got) + "｜🧨 删掉那一段 ⇒ 红");

        tgt.RemoveAll(RuleCore.Precognition);
        int got2 = RuleCore.IsValidTarget(ctx, 0, 2, 1, 3, ranged: false);
        Check(got2 == RuleCodes.OK,
              "★ ②1b **灭自证**：摘掉 precognition 之后必须恢复成合法｜实际=" + CodeName(got2));
    }

    // ==================================================================
    //  ③ AUD6 · tainted（攻击侧：攻方督军 ∧ 目标 tainted ∧ 目标非先锋）
    // ==================================================================
    static void SectionTainted()
    {
        Debug.Log(P + "---- ③ AUD6·tainted 攻击侧（`BattleManager__IsValidAttackTarget.c:185-191`）----");
        var ctx = NewCtx();
        int wSlot = WarlordSlotOf(ctx, 0);
        Check(wSlot >= 0, "③0a （前提）找得到己方督军所在格（现读 " + wSlot + "）");
        var tgt = Place(ctx, 1, 3, UnitCard("B99B1Tainted", 1, 9));
        tgt.AddKeyword(ConditionEval.Tainted, 1);
        Check(ctx.Players[0].Board[wSlot] != null && ctx.Players[0].Board[wSlot].IsWarlord,
              "③0b （前提）攻方那一格确实是**督军**（`IsWarlord`）");
        Check(tgt.Has(ConditionEval.Tainted), "③0c （前提）目标是 `tainted`");

        if (wSlot >= 0)
        {
            int got = RuleCore.IsValidTarget(ctx, 0, wSlot, 1, 3, ranged: false);
            Check(got == RuleCodes.ErrTarget,
                  "★ ③1 督军攻击带 tainted 的非先锋目标 ⇒ 不合法（`.c:187-191`；官方表没有 `CantAttackTainted` ⇒ 复用 ErrTarget）"
                + "｜实际=" + CodeName(got));
        }
        else NotVerified("③1（找不到督军格）");

        // 灭自证：给目标挂上 vanguard ⇒ `HasVanguardTrait(目标)` 为真 ⇒ 这条闸**不再**拒
        tgt.AddKeyword(KeywordTable.Vanguard, 1);
        if (wSlot >= 0)
        {
            int got2 = RuleCore.IsValidTarget(ctx, 0, wSlot, 1, 3, ranged: false);
            Check(got2 == RuleCodes.OK,
                  "★ ③1b **灭自证**：目标成了**先锋**（`HasVanguardTrait` 真）⇒ tainted 闸不成立 ⇒ 合法"
                + "（原版 `.c:188` 的 `&& !HasVanguardTrait(目标)`）｜实际=" + CodeName(got2));
        }

        // 对照：攻方**不是**督军 ⇒ 这条闸不成立
        var ctx2 = NewCtx();
        Place(ctx2, 0, 2, UnitCard("B99B1AtkT", 3, 9));
        var tgt2 = Place(ctx2, 1, 3, UnitCard("B99B1Tainted2", 1, 9));
        tgt2.AddKeyword(ConditionEval.Tainted, 1);
        int got3 = RuleCore.IsValidTarget(ctx2, 0, 2, 1, 3, ranged: false);
        Check(got3 == RuleCodes.OK,
              "★ ③1c 对照：**非督军**攻方打同一个 tainted 目标 ⇒ 合法（原版那一支开头就是 `cardType(攻方) == Hero`）"
            + "｜实际=" + CodeName(got3));
    }

    // ==================================================================
    //  ④ AUD6 · coward（攻方带它 ∧ 目标督军）
    // ==================================================================
    static void SectionCoward()
    {
        Debug.Log(P + "---- ④ AUD6·coward（`BattleManager__IsValidAttackTarget.c:203-211`）----");
        var ctx = NewCtx();
        int wSlot = WarlordSlotOf(ctx, 1);
        Check(wSlot >= 0, "④0a （前提）找得到**敌方**督军所在格（现读 " + wSlot + "）");
        var atk = Place(ctx, 0, 2, UnitCard("B99B1Coward", 3, 9));
        atk.AddKeyword(RuleCore.Coward, 1);
        Check(atk.Has(RuleCore.Coward), "④0b （前提）攻方是 `coward`（常量值 = " + RuleCore.Coward + "）");
        Check(atk.SummonSickness == false, "④0c （前提）攻方**不是**召唤病（否则先被 `CantAttackFlank` 那道闸拦下）");

        if (wSlot >= 0)
        {
            int got = RuleCore.IsValidTarget(ctx, 0, 2, 1, wSlot, ranged: false);
            Check(got == RuleCodes.ErrTarget,
                  "★ ④1 带 coward 的单位攻击**督军** ⇒ 不合法（`.c:203-211`；官方表没有 `CantAttackCoward` ⇒ 复用 ErrTarget）"
                + "｜实际=" + CodeName(got) + "｜🧨 删掉那道闸 ⇒ 红");
        }
        else NotVerified("④1（找不到敌方督军格）");

        // 灭自证：打**非督军**目标 ⇒ 这条闸不成立
        var tgt = Place(ctx, 1, 3, UnitCard("B99B1PlainT", 1, 9));
        int got2 = RuleCore.IsValidTarget(ctx, 0, 2, 1, 3, ranged: false);
        Check(got2 == RuleCodes.OK,
              "★ ④1b **灭自证**：同一个 coward 攻方打**非督军** ⇒ 合法（原版那一支要求 `cardType(目标) == Hero`）"
            + "｜实际=" + CodeName(got2) + (tgt != null ? "" : "（夹具异常）"));
    }

    // ==================================================================
    //  ⑤ 连带：`CardScript.IsDefender` 体内也漏了 `!precognition`
    // ==================================================================
    static void SectionDefenderIgnoresPrecognition()
    {
        Debug.Log(P + "---- ⑤ `CardScript.IsDefender` = `!stealth ∧ !precognition ∧ HasVanguardTrait`（同源对齐）----");
        var ctx = NewCtx();
        Place(ctx, 0, 2, UnitCard("B99B1AtkD", 3, 9));
        // 敌方场上的「先锋」带 precognition ⇒ 它**不算**拦路的先锋
        var van = Place(ctx, 1, 2, UnitCard("B99B1VanPrecog", 1, 9));
        van.AddKeyword(KeywordTable.Vanguard, 1);
        van.AddKeyword(RuleCore.Precognition, 1);
        Place(ctx, 1, 3, UnitCard("B99B1OtherD", 1, 9));

        int got = RuleCore.IsValidTarget(ctx, 0, 2, 1, 3, ranged: false);
        Check(got == RuleCodes.OK,
              "★ ⑤1 敌方唯一的先锋**带 precognition** ⇒ 它不算先锋 ⇒ 可以打别的单位"
            + "（判据 `CardScript__IsDefender.c` 体内的 `!precognition`）｜实际=" + CodeName(got));

        // 灭自证：摘掉 precognition ⇒ 必须恢复成「挡住」
        van.RemoveAll(RuleCore.Precognition);
        int got2 = RuleCore.IsValidTarget(ctx, 0, 2, 1, 3, ranged: false);
        Check(got2 == RuleCodes.ErrAttackBlock,
              "★ ⑤1b **灭自证**：同一个先锋摘掉 precognition 之后必须**变回挡住**（ErrAttackBlock）"
            + "｜实际=" + CodeName(got2));
    }

    // ==================================================================
    //  ⑥ RE1·R6 —— 死讯信号必须排在 反噬(80) 之后、不稳定(81) 之前
    //     （源码级：这三条都是 `RuleCore.cs` 里的**字面量串**，顺序就是队列入队序）
    // ==================================================================
    static void SectionDeathSignalOrder()
    {
        Debug.Log(P + "---- ⑥ RE1·R6 死讯信号次序（`RawCardScript__OnMinionDeath.c:19/22/23` = 80 → 81 → 175/180/170）----");
        const string rel = "Assets/RuleEngine/Core/RuleCore.cs";
        var abs = Path.Combine(Directory.GetCurrentDirectory(), rel);
        bool exists = File.Exists(abs);
        Check(exists, "⑥0 （前提）找得到 `" + rel + "`（Editor 下 `CurrentDirectory` = 工程根）");
        if (!exists) { NotVerified("⑥ 整段（源码文件不在预期位置）"); return; }

        var lines = File.ReadAllText(abs).Split('\n');
        int iBacklash = -1, iSignal = -1, iUnstable = -1, nBacklash = 0, nSignal = 0, nUnstable = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            var s = lines[i].Trim();
            if (s.StartsWith("//")) continue;                       // ⛔ 注释里提到这个名字不算入队
            if (lines[i].Contains("FireTriggerAt(ctx, u, KeywordTable.Backlash")) { iBacklash = i; nBacklash++; }
            if (lines[i].Contains("\"death-signal:\" + (_du.Name")) { iSignal = i; nSignal++; }
            if (lines[i].Contains("\"Unstable:\" + u.Name")) { iUnstable = i; nUnstable++; }
        }
        CheckWithPremise(nBacklash == 1 && nSignal == 1 && nUnstable == 1,
                         "⑥1 （前提）三处入队各现读 **1 次**（反噬 " + nBacklash + " / 死讯 " + nSignal + " / 不稳定 " + nUnstable + "）",
                         iBacklash >= 0 && iSignal > iBacklash && iUnstable > iSignal,
                         "★ ⑥2 🔴 源码次序 = **反噬(" + iBacklash + ") < 死讯(" + iSignal + ") < 不稳定(" + iUnstable + ")`"
                       + "｜🧨 把 `death-signal:` 那个块挪回反噬**之前** ⇒ 红");
    }

    // ==================================================================
    //  ⑦ AUD8 · 让位预览 —— `CardInteraction.Release` 的 `DropAccepted` 次序
    // ==================================================================
    static void SectionPreviewDropAcceptedOrder()
    {
        Debug.Log(P + "---- ⑦ AUD8·让位预览（`CardInteraction.Release` 先摆 DropAccepted 再收指示器）----");
        const string rel = "Assets/CardPresentation/Hand/CardInteraction.cs";
        var abs = Path.Combine(Directory.GetCurrentDirectory(), rel);
        bool exists = File.Exists(abs);
        Check(exists, "⑦0 （前提）找得到 `" + rel + "`");
        if (!exists) { NotVerified("⑦ 整段（源码文件不在预期位置）"); return; }

        var lines = File.ReadAllText(abs).Split('\n');
        int iRelease = -1, iAbort = -1, iPickUp = -1;
        int iSet = -1, iClear = -1, iReset = -1;
        int nAssignFalse = 0, nTacticAssign = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            var s = lines[i].Trim();
            bool comment = s.StartsWith("//");
            if (!comment)
            {
                if (lines[i].Contains("void Release(Vector3 world)")) iRelease = i;
                if (lines[i].Contains("void AbortDrag(string why)")) iAbort = i;
                if (lines[i].Contains("void PickUp(CardView card, Vector3 world, bool freshPress = true)")) iPickUp = i;
                if (lines[i].Contains("DropAccepted = false;")) nAssignFalse++;
                if (lines[i].Contains("DropAccepted = !tactic;")) nTacticAssign++;
            }
            if (iRelease >= 0 && !comment)
            {
                if (iSet < 0 && lines[i].Contains("DropAccepted = ok &&")) iSet = i;
                if (iClear < 0 && lines[i].Contains("ClearDropIndicator();")) iClear = i;
                if (iReset < 0 && iSet >= 0 && lines[i].Contains("DropAccepted = false;")) iReset = i;
            }
        }

        CheckWithPremise(iRelease >= 0 && iSet >= 0 && iClear >= 0,
                         "⑦1 （前提）`Release` 里三处都在（方法 " + iRelease + " / 摆值 " + iSet + " / 收指示器 " + iClear + "）",
                         iSet >= 0 && iClear >= 0 && iSet < iClear,
                         "★ ⑦2 🔴 `DropAccepted = ok && …` 必须排在 `ClearDropIndicator()` **之前**"
                       + "（后者内部 `OnDropPreview(null,-1)` → `EndPreviewReturn()` 要读这个值）"
                       + "｜实际 " + iSet + " vs " + iClear + "｜🧨 调换两者 ⇒ 红");

        CheckWithPremise(iReset >= 0, "⑦3a （前提）紧跟着有一句「用完即清」的 `DropAccepted = false;`",
                         iSet >= 0 && iReset > iClear,
                         "★ ⑦3b 那一句必须排在**收指示器之后**（" + iReset + " > " + iClear + "）—— 早清 = 收指示器读到的是残留值");

        Check(nTacticAssign == 0,
              "★ ⑦4 🔴 全文件**不再有** `DropAccepted = !tactic;`（那个值现在在方法开头就摆好并被消费掉了；"
            + "留着它 = 下一手继承上一手 = 本条 bug 的病根）｜现读 " + nTacticAssign + " 处（注释不算）");

        Check(nAssignFalse >= 3,
              "★ ⑦5 🔴 「清残留」至少三处：`Release` / `AbortDrag` / `PickUp` 各一（现读 " + nAssignFalse + " 处）"
            + "｜🧨 删掉 `AbortDrag` 或 `PickUp` 那一句 ⇒ 取消/起拖会继承上一手的 `true` ⇒ 红");
        Check(iAbort >= 0 && iPickUp >= 0, "⑦5b （前提）`AbortDrag` 与 `PickUp` 两个方法都在（" + iAbort + " / " + iPickUp + "）");
    }
}
