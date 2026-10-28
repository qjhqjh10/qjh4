// RuleEngineTest_EngineB94B13.cs — 第九十四会话 · 波 3 · 执行代理 `B94_B13`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// 本件只放本批 **`RuleCore.cs` 三处接线** 的断言；宿主 `RuleEngineTest.cs` 里由调度台加一个
// `{ CheckB94B13RuleCoreWiring(); }` 块（⚠️ 方法名**不用 `Test`/`Report` 打头**，与 `B94_Z2` /
// `B94_F1` 那几段同形 —— 否则会被 `VerifyRunCaseCoverage` / `gen_diag_runner` 认成用例）。
//
// 本批覆盖的东西（判据 = `d:/2/tools/decomp_full/` 2026-10-29 现读）：
//   ① `RuleCore.DamageTypeOfKind` —— `DamageKind`（我们的轴）→ 原版 `DamageType`（`DamageInfo.damageType`）
//      的**显式映射**。⛔ 两个轴数值撞车（`DamageKind.Shuriken == 4` 而 `DamageType.Attack == 4`），
//      所以一个强转都不许有。
//   ② `RuleCore.BroadcastTurnStartedToHands` —— 改成**转发** `EffectResolver.BroadcastTurnStartedHandHops`
//      （原版 `BattleManagerSupport__BroadcastTurnStarted.c:32-55` 的手牌两跳 / `OnTrigger(0xa0 = 160)`）。
//   ③ `RuleCore.BroadcastUnitPoisoned` 的 ②③ 两跳 —— 从「只出声」改成 `BroadcastUnitPoisonedHandHops`
//      （原版 `BroadcastUnitPoisoned.c:33-56` / `OnTrigger(0x122 = 290)`）。
//   ④ `RuleCore.EmitHit` —— 把 `DamageKind` 映射成 `damageType` 后带进 `damaged` 广播
//      （原版 `CardScript__ResolveDamageDealt.c:191` 的 `switch`：`0/3` ⇒ 185、`4/6` ⇒ 187）。
//
// 🔴 **必须一起读的结论（别只看「断言全绿」）**：现读池内这些 trigger 的 `abilityContext` **全是 `0 Play`**
//    （判据 → `报告/B94_Z2.md` §三）⇒ 手牌两跳**今天全部空转**（原版自己在手牌里也拒）。
//    本件断的是**接线本身**（扫描次数 / 映射值 / 闸放行时真的走到闸前），⛔ 不是「这几张真卡的正文跑过」。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB94B13RuleCoreWiring()
    {
        Section("🆕 B94_B13：`RuleCore` 三处接线（`EmitHit` 带 `damageType` / TurnStarted 两跳 / UnitPoisoned 两跳）");
        B94B13T1DamageTypeMap();
        B94B13T2TurnStartedWiring();
        B94B13T3PoisonedWiring();
        B94B13T4EmitHitCarriesDamageType();
    }

    // ==========================================================================================
    //  T1 —— `DamageKind → 原版 DamageType` 的显式映射（逐档查 `.c` 的生产点）
    // ==========================================================================================

    static void B94B13T1DamageTypeMap()
    {
        Check(RuleCore.DamageTypeOfKind(DamageKind.Melee), 4,
              "映射：`Melee` ⇒ `4 Attack`（`BattleManager__ResolveAttackDamage.c:28` 字面量 `4`；"
            + "`RecordAttackPendingDamage.c:81` 主攻击同值）");
        Check(RuleCore.DamageTypeOfKind(DamageKind.Ranged), 4,
              "映射：`Ranged` ⇒ `4 Attack`（同上 —— 近战 / 远程走**同一条** `ResolveAttackDamage`）");
        Check(RuleCore.DamageTypeOfKind(DamageKind.Blast), 6,
              "映射：`Blast` ⇒ `6 Blast`（`BattleManager__ResolveTraitAdjacentDamage.c:20` 字面量 `6`）");
        Check(RuleCore.DamageTypeOfKind(DamageKind.Spell), 3,
              "映射：`Spell` ⇒ `3 Ability`（`AbilityLogic__PlayAbility.c:1561` 那一支显式写 `+0x1c = 3`）");
        Check(RuleCore.DamageTypeOfKind(DamageKind.Shuriken), 0,
              "映射：`Shuriken` ⇒ `0 Normal`（`_ResolveAttack_d__438__MoveNext.c:679` / "
            + "`RecordAttackPendingDamage.c:71` 的第 4 实参都是 `0`）");
        Check(RuleCore.DamageTypeOfKind(DamageKind.Sentry), 0,
              "映射：`Sentry` ⇒ `0 Normal`（`_ResolveAttack_d__438__MoveNext.c:783` 第 4 实参 `0`）");
        Check(RuleCore.DamageTypeOfKind(DamageKind.Markerlight), 0,
              "映射：`Markerlight` ⇒ `0 Normal`（`_ResolveAttack_d__438__MoveNext.c:1280` / "
            + "`RecordAttackPendingDamage.c:77` 第 4 实参 `0`）");
        Check(RuleCore.DamageTypeOfKind(DamageKind.Stomp), 11,
              "映射：`Stomp` ⇒ `11 Stomp`（`BattleManager__ResolveTraitStompDamage.c:18` 字面量 `0xb`）");
        Check(RuleCore.DamageTypeOfKind(DamageKind.Unknown), -1,
              "映射：`Unknown` ⇒ `-1`（= 本调用点不知道 ⇒ 只发 60；⛔ 别拿 `0 Normal` 顶 —— `0` 是合法值、会误发 185）"
            + "｜🧨 把 `default:` 改成 `return 0` ⇒ 本行红");

        // 判别式：两个轴**数值撞车** ⇒ 任何一次强转会静默错（Shuriken 4 vs Attack 4）
        CheckTrue(RuleCore.DamageTypeOfKind(DamageKind.Shuriken) != (int)DamageKind.Shuriken,
                  "★ ⛔ 别强转：`DamageKind.Shuriken == 4`、`DamageType.Attack == 4` ⇒ 映射结果必须 ≠ 4"
                + "｜🧨 把映射写成 `(int)kind` ⇒ 本行红");
    }

    // ==========================================================================================
    //  T2 —— ① `BroadcastTurnStartedToHands` 真的走到手牌执行口（改前一个计数都不涨）
    // ==========================================================================================

    static void B94B13T2TurnStartedWiring()
    {
        var card = B94B13HandFixture("B13Turn", 160, "TurnStarted", new[] { 160 });
        // ---- 前提（先硬检查，⛔ 别写成 `if (前提) Check(结论)`）----
        Check(card.AbilityTriggerPresence("TurnStarted"), 1, "前提：夹具 `abilityTriggers` 含 `TurnStarted(160)`");
        Check(RuleCore.AbilityContextOfAbilityV14(card, "TurnStarted", null), CardDef.AbilityContextHand,
              "前提：夹具这条 ability 的 `abilityContext` = `10 Hand`（原版 `IsRightContext` 放行那一档）");
        if (card.AbilityTriggerPresence("TurnStarted") != 1
            || RuleCore.AbilityContextOfAbilityV14(card, "TurnStarted", null) != CardDef.AbilityContextHand) return;

        var ctx = B94B13Ctx(0xB13A);
        ctx.Active = 0;
        ctx.Players[0].Hand.Add(ctx.NewInstance(card));
        ctx.Players[1].Hand.Add(ctx.NewInstance(card));

        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.BroadcastTurnStartedToHandsForTest(ctx);
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 2,
              "★ `BroadcastTurnStartedToHands` **真的扫了双方手牌**（attempts = 2：行动方 1 张 + 对手 1 张）"
            + "｜🧨 把它改回改前的「只出声」（`NoteHandTurnStartedNoChannel`）⇒ 0 ⇒ 本行红");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 0,
              "★ 同上：入队 0 —— 正文取不到（`TriggerOps(\"TurnStarted\")` 恒 null：`_triggerOps` 的键"
            + "只可能是**卡面正文关键词**）。⛔ 这不是「接线没生效」，是 `报告/B94_Z2.md` §五·1 的已知缺口");
    }

    // ==========================================================================================
    //  T3 —— ② `BroadcastUnitPoisoned` 的 ②③ 两跳真的走到手牌执行口
    // ==========================================================================================

    static void B94B13T3PoisonedWiring()
    {
        var card = B94B13HandFixture("B13Pois", 290, "UnitPoisoned", new[] { 290 });
        Check(card.AbilityTriggerPresence("UnitPoisoned"), 1, "前提：夹具 `abilityTriggers` 含 `UnitPoisoned(290)`");
        Check(RuleCore.AbilityContextOfAbilityV14(card, "UnitPoisoned", null), CardDef.AbilityContextHand,
              "前提：夹具这条 ability 的 `abilityContext` = `10 Hand`");
        if (card.AbilityTriggerPresence("UnitPoisoned") != 1
            || RuleCore.AbilityContextOfAbilityV14(card, "UnitPoisoned", null) != CardDef.AbilityContextHand) return;

        var ctx = B94B13Ctx(0xB13B);
        ctx.Active = 0;
        ctx.Players[0].Hand.Add(ctx.NewInstance(card));
        ctx.Players[1].Hand.Add(ctx.NewInstance(card));
        var poisoned = new UnitState(B94B13Filler("B13Victim"), false);
        ctx.Players[1].Board[0] = poisoned;

        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.BroadcastUnitPoisonedForTest(ctx, poisoned, null);
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 2,
              "★ `BroadcastUnitPoisoned` 的 ②③ 两跳**真的扫了双方手牌**（attempts = 2；原版 "
            + "`BattleManagerSupport__BroadcastUnitPoisoned.c:33-56` **不排除**中毒者 / 下毒者）"
            + "｜🧨 把它改回 `NoteHandPoisonedNoChannel`（只出声）⇒ 0 ⇒ 本行红");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 0,
              "★ 同上：入队 0（正文取不到 —— 同 `报告/B94_Z2.md` §五·1）");
    }

    // ==========================================================================================
    //  T4 —— ③ `EmitHit` 把 `DamageKind` 映射成 `damageType` 带进 `damaged` 广播（185／187）
    // ==========================================================================================

    static void B94B13T4EmitHitCarriesDamageType()
    {
        // 夹具 A：`abilityTriggers` 只含 187（`UnitDamagedByAttack`）、`abilityContext = 10 Hand`
        var atkCard = B94B13HandFixture("B13Dmg187", 187, "UnitDamagedByAttack", new[] { 187 });
        var abiCard = B94B13HandFixture("B13Dmg185", 185, "UnitDamagedByAbility", new[] { 185 });
        // ---- 前提 ----
        Check(atkCard.AbilityTriggerPresence("UnitDamagedByAttack"), 1, "前提：夹具 A 含 `UnitDamagedByAttack(187)`");
        Check(abiCard.AbilityTriggerPresence("UnitDamagedByAbility"), 1, "前提：夹具 B 含 `UnitDamagedByAbility(185)`");
        Check(RuleCore.AbilityContextOfAbilityV14(atkCard, "UnitDamagedByAttack", null), CardDef.AbilityContextHand,
              "前提：夹具 A 的 `abilityContext` = `10 Hand`");
        Check(RuleCore.AbilityContextOfAbilityV14(abiCard, "UnitDamagedByAbility", null), CardDef.AbilityContextHand,
              "前提：夹具 B 的 `abilityContext` = `10 Hand`");
        if (atkCard.AbilityTriggerPresence("UnitDamagedByAttack") != 1
            || abiCard.AbilityTriggerPresence("UnitDamagedByAbility") != 1
            || RuleCore.AbilityContextOfAbilityV14(atkCard, "UnitDamagedByAttack", null) != CardDef.AbilityContextHand
            || RuleCore.AbilityContextOfAbilityV14(abiCard, "UnitDamagedByAbility", null) != CardDef.AbilityContextHand) return;

        // ---- ③-1 `Melee` ⇒ `4 Attack` ⇒ 187 那一跳走到闸前（夹具 A 没有 60 ⇒ 只可能是 187 这一条）----
        var ctxA = B94B13Ctx(0xB13C);
        ctxA.Active = 0;
        var victimA = B94B13Victim(ctxA, 1);
        ctxA.Players[0].Hand.Add(ctxA.NewInstance(atkCard));
        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.ApplyDamage(ctxA, victimA, 1, "自检", kind: DamageKind.Melee);
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 1,
              "★ `EmitHit`（主伤害 `Melee` ⇒ `DamageType.Attack = 4`）⇒ 手牌 `187` 那一跳**真的被扫到**（attempts = 1）"
            + "｜🧨 把 `EmitHit` 里那个 `damageType:` 实参删掉（缺省 `-1` ⇒ 只发 60）⇒ 0 ⇒ 本行红");

        // ---- ③-2 `Unknown` ⇒ `-1` ⇒ 只发 60（夹具 A 没有 60 ⇒ 一次都不扫）----
        var ctxB = B94B13Ctx(0xB13D);
        ctxB.Active = 0;
        var victimB = B94B13Victim(ctxB, 1);
        ctxB.Players[0].Hand.Add(ctxB.NewInstance(atkCard));
        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.ApplyDamage(ctxB, victimB, 1, "自检", kind: DamageKind.Unknown);
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 0,
              "★ `kind = Unknown` ⇒ `damageType = -1` ⇒ **不碰** 187（夹具没有 60 ⇒ attempts = 0）"
            + "｜🧨 把 `Unknown` 映射成 `0 Normal` ⇒ 会走 185／187 两道判 ⇒ 本行红");

        // ---- ③-3 `Spell` ⇒ `3 Ability` ⇒ **185** 那一跳（夹具 B 没有 187 ⇒ 只可能是 185 这一条）----
        var ctxC = B94B13Ctx(0xB13E);
        ctxC.Active = 0;
        var victimC = B94B13Victim(ctxC, 1);
        ctxC.Players[0].Hand.Add(ctxC.NewInstance(abiCard));
        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.ApplyDamage(ctxC, victimC, 1, "自检", kind: DamageKind.Spell);
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 1,
              "★ `Spell` ⇒ `DamageType.Ability = 3` ⇒ 手牌 `185` 那一跳真的被扫到（attempts = 1）"
            + "｜🧨 把 `Spell` 映射成 `4` ⇒ 走 187、不是 185 ⇒ 本行红（夹具 B 没有 187）");

        // ---- ③-4 同一张 185 夹具 + `Melee` ⇒ 185 **不**该响（4 不在 `{0,3}` 里）----
        var ctxD = B94B13Ctx(0xB13F);
        ctxD.Active = 0;
        var victimD = B94B13Victim(ctxD, 1);
        ctxD.Players[0].Hand.Add(ctxD.NewInstance(abiCard));
        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.ApplyDamage(ctxD, victimD, 1, "自检", kind: DamageKind.Melee);
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 0,
              "★ 判别式的另一半：`Melee`（`4`）**不会**叫醒 `185 UnitDamagedByAbility`（原版 `switch` 的 `4` 只发 187）"
            + "｜🧨 把 185 那道的闸写成 `damageType != 6` ⇒ 本行变 1 ⇒ 红");
    }

    // ==========================================================================================
    //  本件私有小工具（都带 `B13` 前缀，⛔ 不与宿主 / 兄弟分册的同名助手冲突）
    // ==========================================================================================

    /// <summary>一个**手搭的**最小对局：两个玩家 + 两个督军（不跑开局的抽牌/洗牌）。
    /// 目的 = 把「棋盘 + 手牌 + 牌库」摆成**测试要的那一个截面**（同 `B94Z2Ctx` 的做法）。</summary>
    static BattleContext B94B13Ctx(int seed)
    {
        var ctx = new BattleContext(seed);
        ctx.Players[0] = new PlayerState();
        ctx.Players[1] = new PlayerState();
        var h0 = new UnitState(B94B13Hero("B13HeroP0"), true);
        ctx.Players[0].Warlord = h0; ctx.Players[0].Board[BoardSpec.WarlordSlot] = h0;
        var h1 = new UnitState(B94B13Hero("B13HeroP1"), true);
        ctx.Players[1].Warlord = h1; ctx.Players[1].Board[BoardSpec.WarlordSlot] = h1;
        ctx.Active = 0;
        return ctx;
    }

    static CardDef B94B13Hero(string name)
    {
        return new CardDef(name, name, "hero", "", null, "Test", 0, 2, 30, 0, null, subtype: "Warlord");
    }

    static CardDef B94B13Filler(string name)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 1, 0, 1, 0, null);
    }

    /// <summary>摆一个「10 血、无甲无盾」的被打者到 `owner` 的 0 号槽（`ApplyDamage` 要走完管线
    /// 就必须真扣得动血 ⇒ 不能是 1 血 / 带盾 / 带堡垒的夹具）。</summary>
    static UnitState B94B13Victim(BattleContext ctx, int owner)
    {
        var v = new UnitState(new CardDef("B13Body", "B13Body", "unit", "", null, "Test", 1, 0, 10, 0, null), false);
        ctx.Players[owner].Board[0] = v;
        return v;
    }

    /// <summary>
    /// 造一张「手牌里、`abilityContext = 10 Hand`、`abilityTriggers` 含指定 trigger」的夹具卡。
    /// 正文**故意留空** —— 这四族 trigger 在 `_triggerOps` 里没有键（`报告/B94_Z2.md` §五·1），
    /// 所以本件断的是「**扫到了**」（`HandAbilityTriggerAttemptsForTest`），不是「正文跑了」。
    /// </summary>
    static CardDef B94B13HandFixture(string id, int trigger, string triggerName, int[] triggers)
    {
        var ctorCtx = new List<CardDef.AbilityContextSpec>
        {
            new CardDef.AbilityContextSpec
            {
                Trigger = trigger, Name = triggerName, Keyword = null, Context = CardDef.AbilityContextHand,
            },
        };
        var ta = new CardDef.AbilityTargetSpec[triggers.Length];
        for (int i = 0; i < ta.Length; i++) ta[i] = new CardDef.AbilityTargetSpec { Trigger = triggers[i] };
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 1, 0, null,
                           abilityContext: ctorCtx,
                           // 🔴 恒 true：这是「表来自 v14」的正面信号（判据 → `AbilityContextOfAbilityV14` 的 doc）
                           abilityContextsVersioned: true,
                           abilityTriggers: triggers,
                           abilityTriggersVersioned: true,
                           abilityTargets: ta);
    }
}
