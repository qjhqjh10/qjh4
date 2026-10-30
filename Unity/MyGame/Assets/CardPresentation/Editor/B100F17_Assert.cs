// B100F17_Assert.cs — 第一百会话 · 执行代理 `B100_F17`（`报告/B100_X5.md` 的 **C 组 / G 组**）
//
// 为什么另开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// 同一时刻只有调度台能改 `RuleEngineTest.cs` ⇒ 本批断言全写在这里、**只交一个入口方法名**
// `CheckB100F17Gaps`，由调度台在 `Run()` 里加一行（⛔ 本代理不改调度台的文件）。
//
// 本件钉的是 2026-10-30 `B100_F17` 落的两处**实现侧**改动（⛔ 不是改断言迁就产品）：
//   ① **C 组**（`报告/B100_X5.md` #21–26）：`RuleCore.FireTriggerAt` 在「这张卡的这条 trigger
//      **定位不唯一**」时，**不再**把 trigger 名喂给 `EnqueueAbility` 的 ability 级闸 ——
//      那种卡本来就由 `AbilityFamilyGateOpsForTrigger`（X6 口径乙）按**族**逐 op 近似；
//      两道闸一起判 = 整份正文被掐死（实测 `BL33`/`DA38`/`ASH30`/`DA33`/`SOR31` 的 `Rally` 一律返 false）。
//      判据 / 口径全文 → `RuleCore.AbilityFamilyGateIsActive` 的 doc。
//   ② **G 组**（#64/#65）：`EffectResolver.BroadcastWhenCore` 那一行末尾的 `//` 注释
//      **吞掉了它后面的三句代码** ⇒ `BroadcastUnitDamaged` 的手牌两跳（60/185/187）与
//      `BroadcastUnitRegiment`(646) 一直是**死代码**。已把三句挪到 `//` 之前（零行数变化）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）；期望值写死字面量，⛔ 不从被测实现反算。
using System;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（调度台在 `Run()` 里加一行）。</summary>
    public static void CheckB100F17Gaps()
    {
        Section("🆕 B100_F17：族闸 vs ability 级闸**二选一**（定位不唯一只走族闸）· `Damaged` 手牌两跳不再是死代码");
        B100F17T1FamilyGateExclusive();
        B100F17T2DamagedHandHopsAlive();
    }

    // ==============================================================================================
    //  ① C 组：定位不唯一的卡 ⇒ 只走族闸；唯一定位的卡 ⇒ 照旧走 `EnqueueAbility` 那道闸
    //     判据 = `AbilityFamilyGateOpsForTrigger` 自己的窄闸（`UniqueAbilityIndexForTrigger == -1 ∧ ambiguous`）
    //          ＋ `报告/B100_X5.md` #21–26 的实测（族闸 skip 计数对、入队值却是 false）
    // ==============================================================================================

    static void B100F17T1FamilyGateExclusive()
    {
        var pool = CardDatabase.Load();
        if (pool == null) { CheckTrue(false, "①（前提）卡池加载得到"); return; }

        // ---- 正例：池内这 5 张 `ThisCardPlayed` 各有 2 条 ability（X6b 的夹具，实测）----
        string[] amb = { "BL33", "DA38", "ASH30", "DA33", "SOR31" };
        foreach (string id in amb)
        {
            var c = CardDatabase.FindById(pool, id);
            CheckTrue(c != null, $"①（前提）池内有 `{id}`");
            if (c == null) continue;
            string why; bool a;
            int ab = c.UniqueAbilityIndexForTrigger("ThisCardPlayed", out why, out a);
            CheckTrue(ab < 0 && a, $"①（前提）`{id}` 的 `ThisCardPlayed` 确实**定位不唯一**（`{ab}` / amb=`{a}`）");
            CheckTrue(RuleCore.AbilityFamilyGateIsActiveForTest(c, "ThisCardPlayed"),
                      $"★★ ① `{id}` ⇒ **走族闸**（⇒ `FireTriggerAt` 不再把 trigger 名喂给 `EnqueueAbility`）"
                    + "｜🧨 把谓词写成恒 false（= 改回改前）⇒ 红 —— 那正是 #21–26 的六条红");
        }

        // ---- 灭自证：**唯一定位**得到的卡不许走族闸（否则等于把那 1,000+ 张卡的 ability 级 criteria 闸整族关掉）----
        var ash70 = CardDatabase.FindById(pool, "ASH70");
        CheckTrue(ash70 != null, "①（前提）池内有 `ASH70 Swooping Hawk Exarch`");
        if (ash70 != null)
        {
            string why; bool a;
            int ab = ash70.UniqueAbilityIndexForTrigger("ThisCardPlayed", out why, out a);
            Check(ab, 0, "①（前提）`ASH70` 的 `ThisCardPlayed` **唯一定位**到第 0 条 ability");
            CheckTrue(!RuleCore.AbilityFamilyGateIsActiveForTest(ash70, "ThisCardPlayed"),
                      "★★ ① 灭自证：唯一定位 ⇒ **不**走族闸（`EnqueueAbility` 那道 ability 级闸照旧判）"
                    + "｜🧨 把谓词改成恒 true ⇒ 这一条红（而 `B99E9` 的 Rally 负例也会一起废掉）");
        }

        // ---- 行为：族闸那一路**真的入队了**（`DA38` Rally + 友方目标 —— X6b 只断了 skip 计数，没断返回值）----
        var da38 = CardDatabase.FindById(pool, "DA38");
        if (da38 == null) { CheckTrue(false, "①（前提）池内有 `DA38`"); return; }
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var carrier = Place(ctx, 0, 3, da38);
        var mate = Place(ctx, 0, 5, Unit("B100F17Mate", 1, 1, 8));
        RuleCore.ResetAbilityFamilyGateForTest();
        int w0 = RuleCore.AbilityLevelCriteriaWarningsForTest;
        bool q;
        using (RuleCore.BeginAction(ctx))
            q = RuleCore.FireTriggerAtForTest(ctx, carrier, KeywordTable.Rally, 0, 3, chosen: mate);
        CheckTrue(carrier != null, "①（前提）`DA38` 挂到了场上那个 `UnitState` 上");
        CheckTrue(q, "★★ ① `DA38` 的 `Rally` **入队成功**（族闸剔了 op，但整条不该被另一道闸拒）"
                   + "｜🧨 把 `abilityTrigger:` 那格的 `AbilityFamilyGateIsActive(...) ? null :` 去掉 ⇒ 实得 false ⇒ 红");
        Check(RuleCore.AbilityLevelCriteriaWarningsForTest, w0,
              "★★ ① 两道闸**只留一道**：族闸那一路**不再**让 ability 级闸多喊一声"
            + "｜🧨 恢复「两处都判」⇒ 计数 +1 ⇒ 红");
    }

    // ==============================================================================================
    //  ② G 组：`Damaged` 的手牌两跳（`BroadcastUnitDamagedHandHops`）不再是死代码
    //     判据 = `BattleManagerSupport__BroadcastUnitDamaged.c:62-86` → `CardScript__ResolveDamageDealt.c:191`
    //            `switch(damageInfo.damageType)`：`0/3 ⇒ 185 UnitDamagedByAbility` · `4/6 ⇒ 187 UnitDamagedByAttack`
    //     实测（改前）= attempts 恒 0（那三句被行尾 `//` 注释吞了）
    // ==============================================================================================

    static void B100F17T2DamagedHandHopsAlive()
    {
        var card185 = B100F17HandFixture("B100F17Dmg185", 185, "UnitDamagedByAbility");
        Check(card185.AbilityTriggerPresence("UnitDamagedByAbility"), 1,
              "②（前提）夹具 `abilityTriggers` 含 `UnitDamagedByAbility(185)`");
        Check(RuleCore.AbilityContextOfAbilityV14(card185, "UnitDamagedByAbility", null), CardDef.AbilityContextHand,
              "②（前提）夹具这条 ability 的 `abilityContext` = `10 Hand`（`IsRightContext` 放行那一档）");

        // ---- 正例：`Spell` ⇒ `DamageType.Ability = 3` ⇒ 185 那一跳被扫到 ----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ctx.Active = 0;
            var victim = Place(ctx, 1, 3, Unit("B100F17BodyA", 1, 0, 10));
            ctx.Players[0].Hand.Add(ctx.NewInstance(card185));
            RuleCore.ResetHandAbilityCountersForTest();
            RuleCore.ApplyDamage(ctx, victim, 1, "自检", kind: DamageKind.Spell);
            Check(RuleCore.HandAbilityTriggerAttemptsForTest, 1,
                  "★★ ② `EmitHit`（`Spell` ⇒ `damageType = 3`）⇒ 手牌 `185` 那一跳**真的被扫到**（attempts = 1）"
                + "｜🧨 把 `BroadcastWhenCore` 里那三句又塞回行尾 `//` 之后 ⇒ 实得 0 ⇒ 红（改前就是这样）");
        }

        // ---- 灭自证：同一条手牌 + `Melee`（`4`）⇒ 185 **不该**响（`4` 只发 187）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ctx.Active = 0;
            var victim = Place(ctx, 1, 3, Unit("B100F17BodyB", 1, 0, 10));
            ctx.Players[0].Hand.Add(ctx.NewInstance(card185));
            RuleCore.ResetHandAbilityCountersForTest();
            RuleCore.ApplyDamage(ctx, victim, 1, "自检", kind: DamageKind.Melee);
            Check(RuleCore.HandAbilityTriggerAttemptsForTest, 0,
                  "★★ ② 灭自证：`Melee`（`4`）**不会**叫醒 `185 UnitDamagedByAbility`（原版 `switch` 的 `4` 只发 187）"
                + "｜🧨 把 185 那道的闸写成「凡有伤害就发」⇒ 实得 1 ⇒ 红 —— 这一条与上面那条**结构上不可能同时满足**");
        }
    }

    /// <summary>手牌夹具：`abilityTriggers` 只含给定 trigger、`abilityContext = 10 Hand`、**正文留空**
    /// （这几族 trigger 在 `_triggerOps` 里没有键 ⇒ 本件断的是「**扫到了**」，不是「正文跑了」）。</summary>
    static CardDef B100F17HandFixture(string id, int trigger, string triggerName)
    {
        var ctorCtx = new System.Collections.Generic.List<CardDef.AbilityContextSpec>
        {
            new CardDef.AbilityContextSpec
            {
                Trigger = trigger, Name = triggerName, Keyword = null, Context = CardDef.AbilityContextHand,
            },
        };
        var ta = new CardDef.AbilityTargetSpec[1];
        ta[0] = new CardDef.AbilityTargetSpec { Trigger = trigger };
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 1, 0, null,
                           abilityContext: ctorCtx,
                           abilityContextsVersioned: true,
                           abilityTriggers: new[] { trigger },
                           abilityTriggersVersioned: true,
                           abilityTargets: ta);
    }
}
