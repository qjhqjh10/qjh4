// RuleEngineTest_EngineB95WRE.cs — 执行代理 `B95_WRE` 批 2（2026-10-29 · 文件所有者 = `Core/EffectResolver.cs` ＋ `Core/RuleCore.cs`）
//
// 为什么单开一个文件：`Editor/RuleEngineTest.cs` **归调度台**（它来接线）⇒ 本代理**一个字节都没碰**。
// 接线只差 `RuleEngineTest.cs` 的 `Run()` 里**一行**：
//     Step(CheckB95WREAttackerPlumbing);
//
// 本件覆盖 = 账 `战斗逻辑驱动:647`（原版 `BattleManagerSupport.BroadcastUnitDamaged.c:31-45` 的
// **「外加那一支」**）在**新增 `attacker` 形参之后的两条零回归性质**：
//   ① 不传 `attacker`（`null`）⇒ 与改动之前**逐字相同**（这一支根本不启用）；
//   ② 施害卡**在场且未待死** ⇒ 本支早退（原版是同一条棋盘循环，⛔ 不许双响）。
//
// 🔴 **本件没覆盖（如实写，别当成已验收）**：`damageType == 4` 且施害卡**不在场**时「真的会响」那一条
//   需要一张带 `When a friendly unit is damaged, …` 监听器的夹具卡；本代理**跑不了自检宿主**
//   （⛔ 简报红线 4），所以不能确认那段卡面短语在解析层真的收下了 ⇒ 不写一条可能假红的断言。
//   要补的话，夹具形状在报告 §3 里给了。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（接线 = `RuleEngineTest.cs` 的 `Run()` 里一行 `Step(CheckB95WREAttackerPlumbing)`）。
    /// ⚠️ 方法名**不以 `Test` 打头**（照同族 `CheckB94B20…` 的惯例）。</summary>
    public static void CheckB95WREAttackerPlumbing()
    {
        Section("🆕 B95_WRE 批 2/3：`attacker` 形参的两条零回归性质 ＋ 批 3 的次序重排（源码行序）");
        B95WRECaseDefaultIsUnchanged();
        B95WRECaseInPlayAttackerDoesNotDoubleFire();
        B95WRECaseRallyBeforeDeployBroadcast();
    }

    /// <summary>③ **批 3 次序重排的结构断言**：`FireTriggerOnBoard(… Rally …)` 必须**早于**
    /// `BroadcastWhen(ctx, WhenEventKind.Deploy, …)`。
    ///
    /// <para>判据（`判据/B95_Rally闸_核查.md` §八·1）：`Rally`（原版 `trigger 0`）是 `_ResolvePlayCardFromHand…:1081`
    /// 那一跳的**第一件事**；`190 OtherUnitSummoned` 在**最后的广播轮**（`:1426` → `BroadcastCardPlayed`
    /// → `ResolveUnitSummoned.c:32`）⇒ 源码里 Rally 必须排在 Deploy 广播**之前**。</para>
    ///
    /// <para>⚠️ **扫的是【完整调用串】不是关键词**：`RuleCore.cs` 里两处都还留着「已上移／已下移」的**注释存根**
    /// （写的是 `FireTriggerOnBoard(Rally)` / `BroadcastWhen(Deploy)`，不带实参）⇒ 按关键词找会找到注释。
    /// 完整调用串在改后**各只出现一次**（代码处）。</para>
    ///
    /// <para>🧨 把 `:4104` 那行前三句恢复成「Rally/600 排在 590 之后」的老次序（或把 Deploy 广播挪回 Rally 之前）⇒ 红。</para>
    /// </summary>
    static void B95WRECaseRallyBeforeDeployBroadcast()
    {
        string path = System.IO.Path.Combine(UnityEngine.Application.dataPath, "RuleEngine/Core/RuleCore.cs");
        CheckTrue(System.IO.File.Exists(path), "🧨 前提：找得到 `RuleEngine/Core/RuleCore.cs`（路径 = `Application.dataPath` + 相对路径）");
        string src = System.IO.File.ReadAllText(path);
        CheckTrue(!string.IsNullOrEmpty(src), "🧨 前提：源码读得出来（非空）");

        const string rallyAnchor = "FireTriggerOnBoard(ctx, unit, KeywordTable.Rally, chosen)";
        const string deployAnchor = "BroadcastWhen(ctx, WhenEventKind.Deploy, p, unit.Card, unit, preCalled: unit)";
        int iRally = src.IndexOf(rallyAnchor, System.StringComparison.Ordinal);
        int iDeploy = src.IndexOf(deployAnchor, System.StringComparison.Ordinal);
        // 🧨 前提硬检查（先断前提、再断结论；⛔ 不写 `if (找到) Check(…)`）
        CheckTrue(iRally >= 0, "🧨 前提：Rally 锚点（完整调用串）在源码里找得到");
        CheckTrue(iDeploy >= 0, "🧨 前提：Deploy 广播锚点（完整调用串）在源码里找得到");
        CheckTrue(iRally != iDeploy, "🧨 前提：两个锚点不是同一处");
        CheckTrue(src.IndexOf(rallyAnchor, iRally + 1, System.StringComparison.Ordinal) < 0,
                  "🧨 前提：Rally 锚点全文**只出现一次**（注释存根不许带实参）");
        CheckTrue(src.IndexOf(deployAnchor, iDeploy + 1, System.StringComparison.Ordinal) < 0,
                  "🧨 前提：Deploy 广播锚点全文**只出现一次**（注释存根不许带实参）");

        CheckTrue(iRally < iDeploy,
                  "🧨 原版 `trigger 0 (Rally)` 必须排在 `190` 的 Deploy 广播**之前**（判据 `判据/B95_Rally闸_核查.md` §八·1）");
    }

    /// <summary>① **不传 `attacker`（缺省 `null`）⇒ 行为与改动之前逐字相同。**
    /// <para>判据：`BroadcastUnitDamaged.c:31-45` 那条例外的闸第一句就是 `damageInfo.actingCard(+0x10) != null`
    /// ⇒ `actingCard` 为 `null` 时**整支不启用**（`EffectResolver.BroadcastUnitDamagedExtraAttacker` 的头两句）。</para>
    /// <para>🧨 把 `BroadcastUnitDamagedExtraAttacker` 里那句 `actingCard == null ⇒ return` 删掉（或改成
    /// 「`null` 就拿被打者顶」）⇒ 这一条红（会多叫醒一次监听/多记一次伤害归属）。</para>
    /// </summary>
    static void B95WRECaseDefaultIsUnchanged()
    {
        var victimCard = B95WREUnit("B95WRE_V0");
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var victim = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 0), victimCard);

        // 🧨 前提硬检查（先断前提、再断结论；⛔ 不写 `if (前提) Check(结论)`）
        CheckTrue(victim != null && victim.Card == victimCard, "🧨 前提：被打那个真的上了场");
        CheckTrue(victim.Health == 5, "🧨 前提：夹具血量 5（期望值一律写死字面量）");
        int faith0 = ctx.Players[0].Faith;

        RuleCore.ApplyDamage(ctx, victim, 2, "B95WRE", kind: DamageKind.Melee);

        Check(victim.Health, 3, "🧨 缺省 `attacker` ⇒ 2 点近战照常落账（5 → 3）");
        Check(ctx.Players[0].Faith, faith0, "🧨 缺省 `attacker` ⇒ 信仰一分不动（外加那一支不该被叫醒）");
    }

    /// <summary>② **施害卡在场且未待死 ⇒ 本支早退**（原版是同一条棋盘循环发一次，⛔ 不许双响）。
    /// <para>判据：`BroadcastUnitDamaged.c:24-53` —— `IsInPlay(card) ⇒ 只要 card != damagedUnit 就发` **一次**；
    /// `:31-45` 那条例外只在 `IsInPlay == false` 时才走到 ⇒ 在场的施害卡**不能**再发第二遍。</para>
    /// <para>🧨 把 `BroadcastUnitDamagedExtraAttacker` 里 `if (owner >= 0 &amp;&amp; !pending) return;` 删掉 ⇒
    /// 在场景合下会重复叫醒监听（本仓有现成的「受伤双响」族断言会一起红）。</para>
    /// </summary>
    static void B95WRECaseInPlayAttackerDoesNotDoubleFire()
    {
        var victimCard = B95WREUnit("B95WRE_V1");
        var attackerCard = B95WREUnit("B95WRE_A1");
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var victim = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 0), victimCard);
        var attacker = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 1), attackerCard);

        // 🧨 前提硬检查
        CheckTrue(victim != null && attacker != null, "🧨 前提：被打者与施害卡都上了场");
        CheckTrue(!attacker.IsWarlord && !victim.IsWarlord, "🧨 前提：两个都不是督军");
        CheckTrue(ctx.IsPendingDeath(attacker) == false, "🧨 前提：施害卡**不在**待死队列（在场 + 未待死才是本条要测的那一格）");
        CheckTrue(ReferenceEquals(attacker, victim) == false, "🧨 前提：施害卡 ≠ 被打者（原版 `:35` 那半句）");
        int faith0 = ctx.Players[0].Faith;

        RuleCore.ApplyDamage(ctx, victim, 2, "B95WRE", kind: DamageKind.Melee, attacker: attacker);

        Check(victim.Health, 3, "🧨 带在场的 `attacker` ⇒ 伤害照常落账（5 → 3）");
        Check(ctx.Players[0].Faith, faith0,
              "🧨 在场的施害卡**不许**被「外加那一支」再叫醒一次（双响 = 红）");
    }

    /// <summary>夹具卡：一张没有监听器、没有能力的纯数值单位（`CardDef(id, name, type, desc, keywords, faction, cost, melee, health, x)`）。</summary>
    static CardDef B95WREUnit(string id)
    {
        return new CardDef(id, id, "unit", "A plain test unit", null, "Test", 1, 1, 5, 0);
    }
}
