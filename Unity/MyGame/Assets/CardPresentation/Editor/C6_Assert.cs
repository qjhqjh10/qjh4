// C6_Assert.cs — 执行代理 `C6`（第一百零九会话 · 2026-10-31 · C 组）
//
// ============================ 它盯的是什么（判据 → `资料/完全复刻/判据/B109_R20_挑战计数.md`）============================
//  P1 **零生产调用者**：`ChallengeEngine.OnWarlordDamaged` 原先全仓只有 Editor 三处调用 ⇒ 真局里
//     `ChallengeData.DamageEnemyWarlord` **永远是 0**。现接在 `BattleDriver.UpdateHud` 的
//     「敌方督军生命变了」那一支（`_foeSkullHpSeen` 比较，与内存里 `BattleEventsController.CheckHealth`
//     那条 `EnemyHealthChangeSignal` 同一条闸）：**先存旧值**再喂 `prevFoeHp − foeHpNow`。
//     口径来源 = 原版 `decomp_full/EnemyHeroDamageScorer__OnHealthChange.c:11-23`（「本局最低血 / 创新低才累加」），
//     **它是评分器、不是挑战消费者**（原版挑战那一格走服务端 `ChallengeVarResult`、本地无判据）⇒ 借来的口径，账里已标。
//  P2 **结算拍喂计数**：`ChallengeEngine.FeedRunCounters(Ctx.Tally, seat)` 读引擎侧 `AchievementTally`
//     的 `TroopsKilled` / `HealDone` 两格（`B88_R5` §4·1/4·2）。⚠️ `EffectsAdded` **没有引擎源**（见该方法的 doc）。
//  P3 **doc 判据写反 + 陷阱方法**：`ChallengeData.ResetRunCounters` 的注释原写「原版的挑战进度按一局累计」——
//     实读**跨局累加**（`MissionChallenge.UpdateProgress(value,false)` + `ChallengeProgress.CurrentValue`）；
//     `ChallengeEngine.Reset()`（0 调用者）名字像「每局复位」、实际**漏清血线基线** ⇒ 改名 `ResetAllForTest()` + 补清基线。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  · A/B/C 三段：`C6_Assert.RunUnit(); pass += C6_Assert.Pass; fail += C6_Assert.Fail;`
//    **必须在开战前调**（它会为「陷阱复现」临时摆弄 `ChallengeEngine` 的低血基线；收尾会还原五格计数与待处理队列）。
//    建议接在 `Editor/BattleScene.cs` 那条「合计」聚合行之前、**同一物理行追加**（⇒ 行数零变化）。
//    🔴 追加前先看清那一行第一个 `//` 在哪：落在它**之后** = 代码进注释，**这条断言一次都不会跑**
//       （`typecheck` / 账闸全绿、退出码 0，而一条都没执行）。
//  · D 段（真局）：`C6_Assert.RunLive(battleDriver);` —— 接在**本宿主把一局真打到「敌方督军掉过血」之后**
//    （例如自动打牌/教程脚本那一段收尾）。⚠️ 前提不成立时它会**红并说明**（⛔ 不写成 `if (前提) Check(...)` 静默跳过）。
//
// ============================ 🧨 灭自证（哪一处改动会让哪一条红）============================
//  · A2：`ChallengeEngine` 上出现回一个公开静态 `Reset` ⇒ 红（那正是会被误接进 `Begin` 的那个陷阱）。
//  · A4：把 `ResetWarlordHealthBaseline()` 从 `ResetAllForTest()` 里删掉 ⇒ 红（同一喂法会静默计 0）。
//  · B2：把 `FeedRunCounters` 的**赋值**改成 `+=` ⇒ 红（预期是覆盖，不是累加）。
//  · B4：拿别的量顶替 `EffectsAdded` ⇒ 红（本件预置 98，喂完必须仍是 98）。
//  · D：删掉 `UpdateHud` 那句、或把减法写反（`foeHpNow − prevFoeHp`）⇒ 红（真局里首击 delta 变负 ⇒ 计 0）。
//
// ============================ 判据 / 报告 ============================
//  报告：`资料/完全复刻/报告/C6.md`（⛔ 别在这里抄第二份）
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `C6`：挑战计数（P1 血线接线 · P2 结算喂计数 · P3 doc/陷阱方法）。</summary>
public static class C6_Assert
{
    const string P = "[C6] ";

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; UnityEngine.Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; UnityEngine.Debug.Log(P + "   ✗ " + msg);
        return false;
    }

    static void Sec(string s) { UnityEngine.Debug.Log(P + s); }

    /// <summary>五格计数的快照 / 还原（本件全程只动表现层静态格，⛔ 不碰引擎）。</summary>
    struct Snapshot
    {
        public int killed, heal, fx, dmg, turn;
        public List<PracticeChallenge> pending;

        public static Snapshot Take()
        {
            var s = new Snapshot();
            s.killed = ChallengeData.TroopsKilled; s.heal = ChallengeData.HealAmount; s.fx = ChallengeData.EffectsAdded;
            s.dmg = ChallengeData.DamageEnemyWarlord; s.turn = ChallengeData.SurviveTurns;
            s.pending = new List<PracticeChallenge>(ChallengeData.PendingPracticeMatches);
            return s;
        }

        public void Restore()
        {
            ChallengeData.TroopsKilled = killed; ChallengeData.HealAmount = heal; ChallengeData.EffectsAdded = fx;
            ChallengeData.DamageEnemyWarlord = dmg; ChallengeData.SurviveTurns = turn;
            ChallengeData.ClearPending();
            for (int i = 0; i < (pending != null ? pending.Count : 0); i++) ChallengeData.AddPending(pending[i]);
            ChallengeEngine.ResetWarlordHealthBaseline();
        }
    }

    /// <summary>A/B/C 三段（不需要真局）。⚠️ **开战前调**（见文件头）。</summary>
    public static void RunUnit()
    {
        var snap = Snapshot.Take();
        try
        {
            SectionA_ResetSplit();
            SectionB_FeedFromTally();
            SectionC_CreatesNewLow();
        }
        finally { snap.Restore(); }
    }

    // ------------------------------------------------------------------------------------------------
    //  A · P3：`ResetRunCounters` / `ResetAllForTest` / 陷阱方法
    // ------------------------------------------------------------------------------------------------
    static void SectionA_ResetSplit()
    {
        Sec("A · P3：每局清五格 vs 整份复位（判据 `B109_R20` §2·1/§2·3/§2·4）");

        // A1 `ResetRunCounters` 只清五格、不动 `_pending`（原版「跨局累加」那条只适用于**我们自建**的这五格）。
        ChallengeData.TroopsKilled = 7; ChallengeData.HealAmount = 8; ChallengeData.EffectsAdded = 9;
        ChallengeData.DamageEnemyWarlord = 10; ChallengeData.SurviveTurns = 11;
        ChallengeData.AddPending(new PracticeChallenge { PlayerName = "C6-Probe", ModeKey = "Demo/C6", PlayerFirst = true });
        int pendingBefore = ChallengeData.PendingCount;
        ChallengeData.ResetRunCounters();
        Ok(ChallengeData.CountOf(ChallengeKind.KillTroops) == 0 && ChallengeData.CountOf(ChallengeKind.HealAmount) == 0
           && ChallengeData.CountOf(ChallengeKind.AddEffects) == 0 && ChallengeData.CountOf(ChallengeKind.DamageEnemyWarlord) == 0
           && ChallengeData.CountOf(ChallengeKind.SurviveTurns) == 0, "A1 `ResetRunCounters()` 清掉五格计数");
        Ok(ChallengeData.PendingCount == pendingBefore,
           "A1' 且**不动**待处理队列（`PendingCount` 前后都是 " + pendingBefore + "）");

        // A2 陷阱方法不允许再叫 `Reset`（名字会被误当成「每局复位」接进 `Begin`）。
        var t = typeof(ChallengeEngine);
        var oldReset = t.GetMethod("Reset", BindingFlags.Public | BindingFlags.Static);
        var newName = t.GetMethod("ResetAllForTest", BindingFlags.Public | BindingFlags.Static);
        Ok(oldReset == null, "A2 `ChallengeEngine.Reset` 已消失（🧨 谁把它加回来 ⇒ 红：名字像每局复位、实际漏清血线基线）");
        Ok(newName != null, "A2' 改名后的 `ChallengeEngine.ResetAllForTest()` 在（自检/换档唯一入口）");

        // A3/A4 陷阱复现：不清基线 ⇒ 新局首击算成「最低血 − 当前血」= 负 ⇒ **静默计 0**。
        ChallengeEngine.ResetAllForTest();
        ChallengeData.DamageEnemyWarlord = 0;

        // 上一局：敌方督军被打到 27（基线 = 27）。
        ChallengeEngine.OnWarlordDamaged(false, 0, 27);      // 首次信号 delta=0 ⇒ 不计（原版 `0 < num` 同一条闸）
        ChallengeEngine.OnWarlordDamaged(false, 3, 24);      // 首次正 delta ⇒ 计 3，基线 = 24
        Ok(ChallengeData.DamageEnemyWarlord == 3, "A3 首次正 delta 计 3（基线落在 24；实得 " + ChallengeData.DamageEnemyWarlord + "）");

        // 「新局」但**没有**清基线：督军满血 30 吃第一击 2 点 ⇒ d = 24 − 30 = −6 ⇒ 计 0（陷阱本身）。
        int before = ChallengeData.DamageEnemyWarlord;
        ChallengeEngine.OnWarlordDamaged(false, 2, 30);
        Ok(ChallengeData.DamageEnemyWarlord == before,
           "A4 🧨 不清基线的「新局」首击**静默计 0**（上局最低血 24 − 当前 30 = 负）—— 这就是旧 `Reset()` 漏清基线会造的 bug");

        // 真清一次（`ResetAllForTest` 里那句 `ResetWarlordHealthBaseline()`）：同一喂法 ⇒ 计 2。
        ChallengeEngine.ResetAllForTest();
        ChallengeData.DamageEnemyWarlord = 0;
        ChallengeEngine.OnWarlordDamaged(false, 2, 30);
        Ok(ChallengeData.DamageEnemyWarlord == 2,
           "A4' `ResetAllForTest()` 清过基线后，同一喂法计 2（🧨 把那句 `ResetWarlordHealthBaseline()` 删掉 ⇒ 这里恒 0 ⇒ 红）");
    }

    // ------------------------------------------------------------------------------------------------
    //  B · P2：结算拍从引擎 tally 喂三格
    // ------------------------------------------------------------------------------------------------
    static void SectionB_FeedFromTally()
    {
        Sec("B · P2：`FeedRunCounters(Ctx.Tally, seat)`（判据 `B88_R5` §4·1/4·2）");

        var tally = new AchievementTally();
        tally.AddTroopKilled(0); tally.AddTroopKilled(0);
        tally.AddHealDone(0, 5);
        tally.AddTroopKilled(1);          // 对端那一格：**不许**落进本机玩家

        // 前提（硬检查）：先证明 fixture 里真有数（否则下面「0 == 0」恒绿 = 假断言）。
        Ok(tally.TroopsKilledOf(0) == 2, "B0 前提：夹具里座位 0 击杀 = 2（实得 " + tally.TroopsKilledOf(0) + "）");
        Ok(tally.HealDoneOf(0) == 5, "B0' 前提：座位 0 累计治疗 = 5（实得 " + tally.HealDoneOf(0) + "）");

        // 预置一个「上一局的旧值」+ 一个**没有引擎源**的 `EffectsAdded` 哨兵值。
        ChallengeData.TroopsKilled = 99; ChallengeData.HealAmount = 99; ChallengeData.EffectsAdded = 98;
        ChallengeEngine.FeedRunCounters(tally, 0);

        Ok(ChallengeData.TroopsKilled == 2,
           "B1 `TroopsKilled` = 2 —— **赋值不是累加**（预置 99；🧨 改成 `+=` ⇒ 101 ⇒ 红）");
        Ok(ChallengeData.HealAmount == 5, "B2 `HealAmount` = 5（预置 99）");
        Ok(ChallengeData.TroopsKilled != tally.TroopsKilledOf(0) + tally.TroopsKilledOf(1),
           "B3 座位隔离：对端那 1 次击杀**没有**混进本机玩家的数（否则会是 3）");
        Ok(ChallengeData.EffectsAdded == 98,
           "B4 `EffectsAdded` **没被顶替**（它今天没有引擎源；🧨 拿别的量冒充 ⇒ 变数 ⇒ 红）");

        // 传 null 不炸（结算路径上 `Ctx.Tally` 理论上恒非空，但别让断言自己变成异常源）。
        bool threw = false;
        try { ChallengeEngine.FeedRunCounters(null, 0); } catch (Exception) { threw = true; }
        Ok(!threw, "B5 `FeedRunCounters(null, …)` 安全返回（不抛）");
    }

    // ------------------------------------------------------------------------------------------------
    //  C · P1 的计数口径（纯函数冒烟；公式的完整覆盖在 `BattleScene` 的 N22-1…5）
    // ------------------------------------------------------------------------------------------------
    static void SectionC_CreatesNewLow()
    {
        Sec("C · P1：口径 = 本局最低血（打 3 → 治回 3 → 再打 2 ⇒ 计 1，不是 5）");

        ChallengeEngine.ResetAllForTest();
        ChallengeData.DamageEnemyWarlord = 0;

        ChallengeEngine.OnWarlordDamaged(false, 30, 27);   // 首次信号：取 DeltaHealth = 30 ⇒ 计 30、基线 27
        Ok(ChallengeData.DamageEnemyWarlord == 30, "C1 首次信号取 `DeltaHealth`（实得 " + ChallengeData.DamageEnemyWarlord + "）");

        ChallengeEngine.OnWarlordDamaged(false, 0, 30);    // 治疗回 30：d = 27 − 30 < 0 ⇒ 不计
        Ok(ChallengeData.DamageEnemyWarlord == 30, "C2 治疗后的信号**不扣**（实得 " + ChallengeData.DamageEnemyWarlord + "）");

        ChallengeEngine.OnWarlordDamaged(false, 0, 26);    // 再打 4：d = 27 − 26 = 1 ⇒ 只计 1（创新低的那一段）
        Ok(ChallengeData.DamageEnemyWarlord == 31,
           "C3 再打 4 点只按「最低血推进量」计 1（实得 " + ChallengeData.DamageEnemyWarlord + "；逐次累加会是 34）");

        // 我方督军吃伤害不计（原版信号只发敌方那一侧）。
        int b = ChallengeData.DamageEnemyWarlord;
        ChallengeEngine.OnWarlordDamaged(true, 9, 1);
        Ok(ChallengeData.DamageEnemyWarlord == b, "C4 我方督军挨打**不计**（只数「对敌方」）");
    }

    // ------------------------------------------------------------------------------------------------
    //  D · P1 接线：真局里必须真的喂到（⚠️ 需要宿主把一局打到「敌方督军掉过血」之后）
    // ------------------------------------------------------------------------------------------------
    /// <summary>真局断言：本局敌方督军掉过血 ⇒ `ChallengeData.DamageEnemyWarlord &gt; 0`。
    /// <para>🧨 灭自证：删掉 `BattleDriver.UpdateHud` 那句 `ChallengeEngine.OnWarlordDamaged(...)` ⇒ 恒 0；
    /// 把减法写反（`foeHpNow − prevFoeHp`）⇒ 首击 delta 变负 ⇒ 也恒 0。</para></summary>
    public static void RunLive(BattleDriver d)
    {
        Sec("D · P1 接线：真局里敌方督军掉血 ⇒ `ChallengeData.DamageEnemyWarlord > 0`");
        if (d == null || d.Ctx == null)
        {
            Ok(false, "D0 前提：传入的 `BattleDriver` 且 `Ctx` 非空（接线点选错 / 这局还没开始）");
            return;
        }
        int foe = 1 - d.MyIndex;
        var w = d.Ctx.Players[foe] != null ? d.Ctx.Players[foe].Warlord : null;
        // 前提两条**硬断**（⛔ 不写成 `if (前提) …` 静默跳过）。
        Ok(w != null, "D0 前提：敌方（座位 " + foe + "）督军在场上");
        if (w == null) return;
        Ok(w.Health < w.MaxHealth,
           "D0' 前提：敌方督军**掉过血**（当前 " + w.Health + " / 最大 " + w.MaxHealth + "）—— 否则 `UpdateHud` 那一支一次都没进过，下面那条不成立，请把接线点挪到真打完一段之后");
        Ok(ChallengeData.DamageEnemyWarlord > 0,
           "D1 ★ 真局喂到了：`ChallengeData.DamageEnemyWarlord = " + ChallengeData.DamageEnemyWarlord + "`（> 0）");
        Ok(ChallengeData.DamageEnemyWarlord <= w.MaxHealth * 100,
           "D2 🧨 量级哨兵：计数没被「逐次累加 + 治疗回补」刷成天文数字（≤ 最大血×100）");
    }
}
