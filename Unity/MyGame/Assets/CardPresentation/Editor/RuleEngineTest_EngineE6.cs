// RuleEngineTest_EngineE6.cs — 执行代理 `E6` 三条（2026-10-24）的回归断言
//
// 判据（⛔ 不要重新论证）：
//   · ① `BattleContext.DiedLastTurn` 生产侧**只写不读** → `资料/普查产出_1024b/E2_结算残值与EvtKind.md` 顺手发现 1。
//        原版「自上一回合以来」是**逐卡**的 `CardScript.turnDied` + `CardScript__HasDiedSinceLastTurn.c:26-47`
//        （`d:/2/tools/decomp_full/` 现读：`turnDied ∈ {turnCounter, turnCounter-1}`），
//        乘数集是 `inCemeteryFiltered(60)` = **过一个墓地筛选** ⇒ 原版**没有**全局计数器；
//        我们真正的消费者是 `RuleCore.cs` 的 `diedsincelastturn` 分支（`DeadUnits` + `DeathTurn`）。
//   · ② 整条 `desc` 那条路（`EffectText.AssignAbilityTargets` 的 **full** 分支）仍可能给「替代行动块」
//        贴 `cardAbilities` 的 `ta` → `资料/普查产出_1024b/E1_chosenAbility与ta来源.md` §十·4
//        （E1 顺手发现；本件补的正是「那条路上的 op 有没有被引擎消费」）。
//   · ③ `CardDatabase.Find(pool,name,faction)` 阵营不匹配/未命中时返回 null **且不出声** ＋
//        `EvtKind.Death` 的 `CardId` 装的是 `Name` 而不是 `Id`
//        → `资料/普查产出_1024b/E4_录像头与对手聊天.md` 顺手发现 1/3。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 期望值一律写死字面量，⛔ 不从被测实现反算。
// ✅ 接线：`RuleEngineTest.Run()` 里加一行 `Step(TestEngineE6);`
//    （⛔ 本文件不碰 `RuleEngineTest.cs` / `BattleContext.cs` / `CardDef.cs` —— 都不在本代理白名单里）
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批**唯一入口**（`Run()` 里一行 `Step(TestEngineE6)`）。
    /// 三个子用例是 `E6…` 打头的辅助方法（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestEngineE6()
    {
        Section("🆕 执行代理 `E6`：`DiedLastTurn` 生产侧无读者 · 整条 desc 路的替代行动块 · "
              + "`CardDatabase.Find` 未命中出声 · `EvtKind.Death` 的 `CardId` 口径");
        E6T1DiedLastTurnNotProductionInput();
        E6T2WholeDescAltBlockHazard();
        E6T3FindMissAndDeathCardId();
    }

    // ==============================================================================================
    //  ① `DiedLastTurn` 不是生产输入 —— 真正的消费者是 `DeadUnits` + `DeathTurn`
    // ==============================================================================================

    /// <summary>
    /// 夹具与 `EngineA1` 的 `A1DiedSinceLastTurn` 同形（P0 砍死 P1 的 1 血兵），但**量的是别的东西**：
    /// A1 断的是「`DiedLastTurn` 滚动到 1」（那一格**生产侧没人读** ⇒ 那条断言接近自证）；
    /// 本用例断的是「**把 `DiedLastTurn` 写脏，读数一个字不变**」＋「**清掉 `DeadUnits`，读数归 0**」——
    /// 两条方向相反，结构上不可能同时由「读那一格」实现。
    /// </summary>
    static void E6T1DiedLastTurnNotProductionInput()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        Place(ctx, 0, 3, Unit("E6DsKiller", 1, 3, 10), exhausted: false);
        Place(ctx, 1, 3, Unit("E6DsVictim", 1, 0, 1), exhausted: true);
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "① 砍死 P1 的 1 血兵");
        Check(ctx.DiedThisTurn, 1, "① （前提）`DiedThisTurn = 1`");
        PassTurn(ctx);                                   // → 第 2 回合（滚动 + 清零都在 BeginTurnCore）
        Check(ctx.Turn, 2, "① （前提）现在第 2 回合");

        var since = new EffectOp { CountScope = "diedsincelastturn", CountRef = "own|troop" };
        int rolled = ctx.DiedLastTurn;
        CheckTrue(rolled == 1,
                  "① 写点还在：`BeginTurnCore` 把上一回合的 1 滚进了 `DiedLastTurn`"
                + "（**这一格只为自检保留**，⛔ 它不是 `UM77` 的判据）"
                + "｜🧨 删掉 `RuleCore.cs` 的 `ctx.DiedLastTurn = ctx.DiedThisTurn;` ⇒ 实得 0 ⇒ 红");

        // ---- 正面：真消费者（`DeadUnits` + `DeathTurn` 窗口）----
        Check(RuleCore.CountForTest(ctx, 1, null, since), 1,
              "★ ① `diedsincelastturn` 数出 1 —— 来源 = `ctx.DeadUnits`（`DeathTurn == Turn-1`）"
            + "｜🧨 把它并进 `died`（本回合计数）⇒ 实得 0 ⇒ 红");

        // ---- 反面 1：把 `DiedLastTurn` 写脏 ⇒ 读数必须一个字不变 ----
        ctx.DiedLastTurn = 999;
        Check(RuleCore.CountForTest(ctx, 1, null, since), 1,
              "★★ ① 把 `DiedLastTurn` 写成 **999** ⇒ 读数**仍是 1**（生产侧没有任何读者）"
            + "｜🧨 谁把 `diedsincelastturn` 改成读 `ctx.DiedLastTurn` ⇒ 实得 999 ⇒ 红");
        ctx.DiedLastTurn = rolled;

        // ---- 反面 2：清掉真来源 ⇒ 归 0（同一时刻 `DiedLastTurn` 仍是 1）----
        ctx.DeadUnits.Clear();
        Check(ctx.DiedLastTurn, 1, "① （前提）清 `DeadUnits` 不影响 `DiedLastTurn`（它只是个数）");
        Check(RuleCore.CountForTest(ctx, 1, null, since), 0,
              "★★ ① 清空 `ctx.DeadUnits` ⇒ 读数 **0** —— 真来源就是那张表"
            + "｜🧨 反过来（读数来自 `DiedLastTurn`）⇒ 仍是 1 ⇒ 与上一条不可能同时成立");
    }

    // ==============================================================================================
    //  ② 整条 `desc` 那条路仍会给「替代行动块」贴 `cardAbilities` 的 `ta`（E1 §十·4 的缺口）
    // ==============================================================================================

    /// <summary>
    /// **合成表探针**（不依赖真卡数据的巧合）：一张 `keywords=["Duty"]`、`desc` 是**裸写正文**
    /// （没有 `Duty:` 前缀）的单位卡 ⇒ 正文走 `CardDef.CollectBareKeywordBody` →
    /// `EffectText.Parse(Desc, card: this)`（**整条 desc**，`IsWholeDesc` 为真）⇒ `AssignAbilityTargets`
    /// 的 **full 分支**（E1 的 `alternativeActionBody` 闸只加在**片段**分支）。
    /// 表里故意让两个来源给出**不同的数**：
    ///   · `abilityTargets[0].ta = 25`（旧来源 = `cardAbilities`）；
    ///   · `activeAbilityLogic[0].ta = 30`（E1 认定的真来源）。
    /// 于是：整条 desc 的产物**不贴 `ta`（-1）**，而**运行期结算的那条 op 是另一个对象、带 30** ——
    /// 这就是「这条路上的 op 今天没被消费」的可执行证据（也是将来接错时的判别式）。
    /// 🔴 **2026-10-24（执行代理 `E1`）核销**：full 分支已加与 `AssignAbilityTargets` 同口径的替代行动闸
    ///   ⇒ 整条 desc 不再把 `cardAbilities` 的 `ta` 贴上（期望从 `25` 改为 **`-1`**；⛔ 不是删掉这条）。
    /// </summary>
    static void E6T2WholeDescAltBlockHazard()
    {
        var realPool = CardDatabase.Load();
        const string json =
            "{\"version\":24,\"count\":1,\"cards\":[{\"id\":\"E6Duty\",\"name\":\"E6 交替行动探针\","
          + "\"type\":\"unit\",\"desc\":\"Deal 3 damage\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
          + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
          + "\"keywords\":[\"Duty\"],\"cardTraits\":[],"
          + "\"abilityTargets\":[{\"trigger\":662,\"ta\":[25]}],"
          + "\"abilityMultipliers\":[{\"trigger\":662,\"fm\":[0]}],"
          + "\"activeAbilityLogic\":[{\"ab\":-1,\"gate\":1,\"trigger\":400,\"needsLoading\":0,"
          + "\"ta\":[30],\"fm\":[0],\"ca\":[10],\"pa\":[0],"
          + "\"dmg\":[{\"min\":3,\"max\":3,\"opt\":0,\"type\":0}]}]}]}";
        CardDef probe = null;
        List<EffectOp> fresh = null;
        IReadOnlyList<EffectOp> resolved = null;
        string map = null;
        try
        {
            var list = CardDatabase.Parse(json);
            Check(list.Count, 1, "② （前提）合成探针卡解析出 1 张");
            if (list.Count == 1) probe = list[0];
            if (probe != null)
            {
                fresh = EffectText.Parse(probe.Desc, out _, out _, card: probe);
                resolved = probe.TriggerOps(KeywordTable.Duty);
                map = probe.ActiveAbilityMap;
            }
        }
        finally
        {
            // 合成卡的名字进了全局名字索引 ⇒ 复原（否则后面的用例按名字找卡会撞上它）
            CreatePool.BuildNameIndex(realPool);
        }
        CheckTrue(probe != null, "② （前提）探针卡拿到了");
        if (probe == null) return;

        Check((map ?? "(null)"), "attached",
              "② （前提）运行期那一路按 `chosenAbility` 种类挂上了（`ta=30`）"
            + "｜🧨 把 `ca=10` 改掉 / 删掉 `ApplyActiveAbilityLogic` 的按种类归位 ⇒ 红");
        CheckTrue(fresh != null && fresh.Count == 1, "② （前提）整条 `desc` 解析出 1 条 op");
        var fop = fresh != null && fresh.Count == 1 ? fresh[0] : null;
        CheckTrue(fop != null && fop.SubjectTargetsAffected == -1,
              "★★ ② **整条 `desc` 那条路不再把 `cardAbilities` 的 `ta` 贴到替代行动块上（已核销）** —— "
            + "现在应为 `-1`（E1 已给 full 分支加同一口径的替代行动闸；合成表里 `abilityTargets[0].ta = 25` 不该再被贴上）"
            + "｜🧨 把 full 分支那道闸删掉 / 放宽 ⇒ 实得 `25` ⇒ 红；"
            + "**这条不再允许改回 `25`**（那等于把已修好的缺陷钉回去）");
        var rop = resolved != null && resolved.Count == 1 ? resolved[0] : null;
        CheckTrue(rop != null, "② （前提）`TriggerOps(\"duty\")` 也收出了 1 条 op");
        CheckTrue(rop != null && rop.SubjectTargetsAffected == 30,
              "★★ ② **运行期结算的那条 op 不是整条 desc 的那条**：它被 `ApplyActiveAbilityLogic`"
            + "（按 `chosenAbility` 种类）覆盖成了 active 行的 **30**"
            + "｜🧨 让结算去吃 `Parse(Desc)` 的整条产物（或删掉那次覆盖）⇒ 实得 25 ⇒ 红");
        CheckTrue(!ReferenceEquals(fop, rop),
              "★ ② 两份是**不同的对象**（两条收集路各 Parse 一次）—— 这条钉住「谁也没把另一份当自己」"
            + "｜🧨 谁把 `_triggerOps` 直接指向 `Parse(Desc)` 的返回值 ⇒ 变同一份 ⇒ 红");
    }

    // ==============================================================================================
    //  ③ `CardDatabase.Find(pool,name,faction)` 未命中出声 ＋ `EvtKind.Death.CardId` 只放 `Name`
    // ==============================================================================================

    static void E6T3FindMissAndDeathCardId()
    {
        var pool = CardDatabase.Load();

        // ---- ① 名字根本不在池里 ----
        int before = CardDatabase.FindMissWarningsForTest;
        var miss = CardDatabase.Find(pool, "E6 NoSuchCard 20261024", "Ultramarines");
        CheckTrue(miss == null, "③ （前提）池里没有这张卡 ⇒ 返回 null（返回值语义**没改**）");
        Check(CardDatabase.FindMissWarningsForTest - before, 1,
              "★ ③ **未命中出声**：喊了 1 次（改前一声不响，调用方静默不生效）"
            + "｜🧨 删掉 `NoteFactionFindMiss` 那一行 ⇒ 0 ⇒ 红");
        CardDatabase.Find(pool, "E6 NoSuchCard 20261024", "Ultramarines");
        Check(CardDatabase.FindMissWarningsForTest - before, 1,
              "★ ③ 同一 `(name, faction)` 再问一次 ⇒ **不再喊**（去重表；卡组校验那种循环里会问很多遍）"
            + "｜🧨 去掉 `_findMissWarned` 去重 ⇒ 2 ⇒ 红");

        // ---- ② 名字在池里，但阵营对不上 ----
        var sor6 = CardDatabase.FindById(pool, "SOR6");
        CheckTrue(sor6 != null, "③ （前提）池里有 `SOR6`");
        if (sor6 != null)
        {
            CheckTrue(ReferenceEquals(CardDatabase.Find(pool, sor6.Name, sor6.Faction), sor6),
                      "③ （正例）阵营对得上 ⇒ 找得到（并且**不出声** —— 下一格数着）");
            int before2 = CardDatabase.FindMissWarningsForTest;
            var cross = CardDatabase.Find(pool, sor6.Name, "Necrons");
            CheckTrue(cross == null, "③ **阵营不匹配 ⇒ null**（语义没改）");
            Check(CardDatabase.FindMissWarningsForTest - before2, 1,
                  "★★ ③ 而且**出声**，消息里区分「有这个名字、但阵营都不是 Necrons」"
                + "｜🧨 只对「名字不存在」出声、漏掉跨阵营这一档 ⇒ 0 ⇒ 红"
                + "（`E4` 那次 C4 断言差点因为这档**静默**而「因为错的原因变绿」）");
        }

        // ---- ③ `EvtKind.Death.CardId` = `Name`（⛔ 不是 `Id`）----
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        Place(ctx, 0, 3, Unit("E6DeathKiller", 1, 5, 10), exhausted: false);
        // 🔴 关键：这张卡的 **id 与 name 故意不同**（`Unit(...)` 那个夹具两者相同 ⇒ 断不出这一格）
        var victimCard = new CardDef("E6_DEATH_ID", "E6 Death Name", "unit", "", null, "Test",
                                     1, 0, 1, 0);
        Place(ctx, 1, 3, victimCard, exhausted: true);
        ctx.ClearSignals();
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "③ 砍死它（1 血）");

        var death = FindSignal(ctx, EvtKind.Death);
        CheckTrue(death != null, "③ （前提）发了一条 `EvtKind.Death`");
        if (death != null)
        {
            Check(death.CardId, "E6 Death Name",
                  "★★ ③ `EvtKind.Death` 的 `CardId` 装的是 **`CardDef.Name`**（`RuleCore.cs` 那三处 `Emit` 都传 `u.Name`）"
                + "｜🧨 把它改成 `u.Card.Id` ⇒ 实得 `E6_DEATH_ID` ⇒ 红");
            CheckTrue(death.CardId != "E6_DEATH_ID",
                  "★★ ③ **不是 `Id`** —— 这一格是 `BattleDriver.SpeakFor` 找阵亡语音的键"
                + "（`CardDatabase.Find(pool, name, faction)` 按**名字**找）"
                + "｜🧨 同上（两条一起变，所以上面那条是主判据、这条是「口径」说明）");
            Check(death.Slot, 3, "③ （附带）格位 = 被打的那一格");
            Check(death.Player, 1, "③ （附带）`Player` = 死者的主人（P1）");
        }
    }
}
