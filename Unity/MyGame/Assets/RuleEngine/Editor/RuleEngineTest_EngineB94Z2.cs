// RuleEngineTest_EngineB94Z2.cs — 第九十四会话 · 波 2 · 执行代理 `B94_Z2`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// 本件只放本批**新增结构**的断言；宿主 `RuleEngineTest.cs` 里由调度台加一个
// `{ CheckB94Z2HandAbilityChannel(); }` 块（⚠️ 方法名**不用 `Test`/`Report` 打头**，
// 与 `B93_G1` / `B94_F1` 那几段同形）。
//
// 本批覆盖的东西（判据 = `d:/2/tools/decomp_full/` ＋ ILSpy `BattleManagerSupport.cs`，2026-10-29 现读）：
//   ① `RuleCore.FireHandAbilityTrigger` —— 手牌卡自己那条 `abilityTriggers` 的**执行口**
//      （原版 `CardScript.OnTrigger` 在手牌那一侧；两道闸 = `AbilityTriggerPresence == 1` ＋
//       `IsRightContext == 10 Hand`）。
//   ② 四类广播的「手牌两跳」：`BroadcastTurnStartHandHops` / `BroadcastTurnStartedHandHops` /
//      `BroadcastUnitPoisonedHandHops` / `BroadcastUnitDamagedHandHops` / `BroadcastUnitRegimentHandHops`。
//
// 🔴 **必须一起读的结论（别只看「断言全绿」）**：现读 `cards_engine.json` v39（池内 1,129 张）——
//    `abilityTriggers` 含 30/31/46/60/230/290/646/160/185/187/720 的卡，其 `abilityContext`
//    **全部是 `0 Play`**（唯一例外 `715 Ecstasy` 有 8 条 40）⇒ 按原版那道 `IsRightContext` 闸，
//    这四类广播的手牌跳**今天全部空转**（不是「我们没做」，是原版也拒）。本件因此**断的是结构**
//    （两跳真的扫了、闸真的按原版拦、闸放行时正文真的跑），⛔ 不是「这几张真卡的正文跑过」。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB94Z2HandAbilityChannel()
    {
        Section("🆕 B94_Z2：手牌 `abilityTriggers` 执行口（`FireHandAbilityTrigger`）＋ 四类广播的手牌两跳");
        B94Z2T1ChannelGates();
        B94Z2T2BroadcastHandHops();
    }

    // ==========================================================================================
    //  T1 —— 执行口的两道闸：正例（`Hand(10)`）真的结算、负例（`Play(0)` / 无 ability / 旧表）一次都不跑
    // ==========================================================================================

    static void B94Z2T1ChannelGates()
    {
        // ---------------- 正例：`abilityContext == 10 Hand` + 正文 `Strike: Draw 1 card` ----------------
        var posCard = B94Z2Fixture("Z2Pos", 10, new[] { 580 }, new[] { "Strike" }, body: "Strike: Draw 1 card");
        // ---- 前提（先硬检查，⛔ 别写成 `if (前提) Check(结论)`）----
        Check(posCard.AbilityTriggerPresence("Strike"), 1, "前提：正例夹具的 `abilityTriggers` 里**真有** `Strike(580)`");
        Check(RuleCore.AbilityContextOfAbilityV14(posCard, "Strike", null), CardDef.AbilityContextHand,
              "前提：正例夹具的 `abilityContext` = `10 Hand`（原版 `IsRightContext` 那一档）");
        CheckTrue(posCard.TriggerOps(KeywordTable.Strike) != null,
                  "前提：`Strike: Draw 1 card` 解析得出 op（`CardDef.TriggerOps(strike)` 非 null）");
        if (posCard.AbilityTriggerPresence("Strike") != 1
            || RuleCore.AbilityContextOfAbilityV14(posCard, "Strike", null) != CardDef.AbilityContextHand
            || posCard.TriggerOps(KeywordTable.Strike) == null) return;

        RuleCore.ResetHandAbilityCountersForTest();
        var ctxPos = B94Z2Ctx(0xB94A);
        ctxPos.Players[0].Deck.Add(ctxPos.NewInstance(B94Z2Filler("Z2Deck1")));   // 抽牌要有牌库
        var posInst = ctxPos.NewInstance(posCard);
        ctxPos.Players[0].Hand.Add(posInst);
        int hand0 = ctxPos.Players[0].Hand.Count;
        int deck0 = ctxPos.Players[0].Deck.Count;

        bool fired = RuleCore.FireHandAbilityTrigger(ctxPos, 0, posInst, "Strike", KeywordTable.Strike, by: "自检:正例");
        CheckTrue(fired, "正例：`Hand(10)` 的手牌卡 ⇒ 执行口**入队**（`FireHandAbilityTrigger` 返回 true）"
                       + "｜🧨 把第 ② 道闸改成恒 false ⇒ 本行红");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 1, "正例：入队计数 = 1（只入队一次，不双入）");
        RuleCore.PumpActionQueues(ctxPos);
        // 🧨 正文真的跑了：删掉 `HandAbilityDeferred` 里的 `ResolveOps` ⇒ 手牌不涨、牌库不减 ⇒ 本条红。
        Check(ctxPos.Players[0].Hand.Count, hand0 + 1, "🧨 正例：正文**真的结算了**（`Draw 1 card` ⇒ 手牌 +1）");
        Check(ctxPos.Players[0].Deck.Count, deck0 - 1, "🧨 正例：牌库 −1（同一件事的另一面，防「凭空加一张」）");
        // 正例也该发一条 `EvtKind.Trigger`（原版次序 = 闸 → `TriggerAbility` → `PlayTriggerTraitAnim`）
        int trig = 0;
        for (int i = 0; i < ctxPos.Signals.Count; i++)          // ⚠️ `Events` 是**日志字符串**表，表现层事件在 `Signals`
            if (ctxPos.Signals[i].Kind == EvtKind.Trigger && ctxPos.Signals[i].Keyword == "Strike") trig++;
        Check(trig, 1, "正例：发过 **1 条** `EvtKind.Trigger`（关键词 = `Strike`）"
                     + "｜🧨 把 `EmitHandAbilityTrigger` 删掉 ⇒ 本行红");

        // ---------------- 负例 ①：`abilityContext == 0 Play`（原版 `IsRightContext` 在手牌里拒）----------------
        var playCard = B94Z2Fixture("Z2Play", 0, new[] { 580 }, new[] { "Strike" }, body: "Strike: Draw 1 card");
        Check(playCard.AbilityTriggerPresence("Strike"), 1, "前提：负例①夹具**也有** `Strike(580)`（不是「没能力」那一档）");
        Check(RuleCore.AbilityContextOfAbilityV14(playCard, "Strike", null), CardDef.AbilityContextPlay,
              "前提：负例①的 `abilityContext` = `0 Play`（必须活着在场）");
        if (playCard.AbilityTriggerPresence("Strike") != 1) return;

        RuleCore.ResetHandAbilityCountersForTest();
        var ctxNeg = B94Z2Ctx(0xB94B);
        ctxNeg.Players[0].Deck.Add(ctxNeg.NewInstance(B94Z2Filler("Z2Deck2")));
        var negInst = ctxNeg.NewInstance(playCard);
        ctxNeg.Players[0].Hand.Add(negInst);
        int negHand0 = ctxNeg.Players[0].Hand.Count;
        bool negFired = RuleCore.FireHandAbilityTrigger(ctxNeg, 0, negInst, "Strike", KeywordTable.Strike, by: "自检:负例0");
        CheckTrue(!negFired, "★ 负例①（`Play(0)` 在手）：**不入队**"
                           + "｜🧨 把第 ② 道闸改成 `true` ⇒ 本行红（判别式：正例响 + 负例不响）");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 0, "★ 负例①：入队计数 = 0");
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 1, "★ 负例①：attempts = 1（**走到了**闸前，只是被闸拒 —— 不是「没调用」）");
        RuleCore.PumpActionQueues(ctxNeg);
        Check(ctxNeg.Players[0].Hand.Count, negHand0, "★ 负例①：正文一次都没跑（手牌不变）");

        // ---------------- 负例 ②：`abilityTriggers` 里**没有**这条（presence == 0）----------------
        var noTrig = B94Z2Fixture("Z2NoTrig", 10, new int[0], new[] { "Strike" }, body: "Strike: Draw 1 card");
        Check(noTrig.AbilityTriggerPresence("Strike"), 0, "前提：负例②夹具的 `abilityTriggers` **明确没有** 580（packed 空表）");
        RuleCore.ResetHandAbilityCountersForTest();
        var ctxNo = B94Z2Ctx(0xB94C);
        var noInst = ctxNo.NewInstance(noTrig);
        ctxNo.Players[0].Hand.Add(noInst);
        CheckTrue(!RuleCore.FireHandAbilityTrigger(ctxNo, 0, noInst, "Strike", KeywordTable.Strike, by: "自检:负例无"),
                  "★ 负例②：数据里没有这条 ability ⇒ 不触发（原版 `TriggerAbilities` 第一句就 return）");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 0, "★ 负例②：入队计数 = 0");

        // ---------------- 负例 ③：**旧卡表**（`abilityTriggersVersioned == false`）⇒ 未知 ⇒ 不猜 ----------------
        var legacy = B94Z2Fixture("Z2Legacy", 10, new[] { 580 }, new[] { "Strike" }, body: "Strike: Draw 1 card",
                                  triggersVersioned: false);
        Check(legacy.AbilityTriggerPresence("Strike"), -1, "前提：负例③是**旧表**（`presence == -1` = 未知，⛔ 不是 0）");
        RuleCore.ResetHandAbilityCountersForTest();
        var ctxLeg = B94Z2Ctx(0xB94D);
        var legInst = ctxLeg.NewInstance(legacy);
        ctxLeg.Players[0].Hand.Add(legInst);
        CheckTrue(!RuleCore.FireHandAbilityTrigger(ctxLeg, 0, legInst, "Strike", KeywordTable.Strike, by: "自检:负例旧表"),
                  "★ 负例③：`-1`（未知）⇒ **不猜成 Hand**、不入队（那条命交给既有文本路）");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 0, "★ 负例③：入队计数 = 0");

        // ---------------- 对照：**同一张卡的棋盘那条路一个字没变**（`FireTriggerAt` 仍按原样结算）----------------
        RuleCore.ResetHandAbilityCountersForTest();
        var ctxB = B94Z2Ctx(0xB94E);
        ctxB.Players[0].Deck.Add(ctxB.NewInstance(B94Z2Filler("Z2Deck3")));
        var boardCard = B94Z2Fixture("Z2Board", 0, new[] { 580 }, new[] { "Strike" }, body: "Strike: Draw 1 card");
        var boardUnit = new UnitState(boardCard, false);
        ctxB.Players[0].Board[0] = boardUnit;
        int bHand0 = ctxB.Players[0].Hand.Count;
        CheckTrue(RuleCore.FireTriggerAtForTest(ctxB, boardUnit, KeywordTable.Strike, 0, 0),
                  "对照前提：棋盘单位走既有 `FireTriggerAt` 仍能入队（⛔ 本笔没动在场那半）");
        RuleCore.PumpActionQueues(ctxB);
        Check(ctxB.Players[0].Hand.Count, bHand0 + 1, "对照：棋盘那条路照样结算（`Draw 1 card` ⇒ 手牌 +1）");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 0,
              "对照：棋盘那条路**不经过**手牌执行口（计数仍 0 —— 两条路结构上分开）");
    }

    // ==========================================================================================
    //  T2 —— 四类广播的手牌两跳：**真的扫了双方手牌**（attempts），且现池数据下被闸拒（queued == 0）
    // ==========================================================================================

    static void B94Z2T2BroadcastHandHops()
    {
        // 夹具：一张「`abilityTriggers` 里有这四类广播要的 trigger、但 `abilityContext` 全是缺行(=Play 0)」的卡。
        int[] trigs = { 30, 31, 46, 60, 160, 187, 230, 290, 646 };
        var hopCard = B94Z2Fixture("Z2Hops", 0, trigs, null, body: "", contextRow: false);
        // ---- 前提 ----
        Check(hopCard.AbilityTriggerPresence("TurnStart"), 1, "前提：夹具 `abilityTriggers` 含 `TurnStart(30)`");
        Check(hopCard.AbilityTriggerPresence("TurnStarted"), 1, "前提：夹具含 `TurnStarted(160)`");
        Check(hopCard.AbilityTriggerPresence("UnitPoisoned"), 1, "前提：夹具含 `UnitPoisoned(290)`");
        Check(hopCard.AbilityTriggerPresence("OtherCardRegiment"), 1, "前提：夹具含 `OtherCardRegiment(646)`");
        Check(hopCard.AbilityTriggerPresence("UnitDamaged"), 1, "前提：夹具含 `UnitDamaged(60)`");
        Check(hopCard.AbilityTriggerPresence("UnitDamagedByAttack"), 1, "前提：夹具含 `UnitDamagedByAttack(187)`");
        Check(RuleCore.AbilityContextOfAbilityV14(hopCard, "TurnStart", null), CardDef.AbilityContextPlay,
              "前提：这条 trigger **没有 context 行** ⇒ v14「缺行 = Play(0)」（原版 `IsRightContext` 要求在场）");
        if (hopCard.AbilityTriggerPresence("TurnStart") != 1) return;

        var ctx = B94Z2Ctx(0xB94F);
        ctx.Active = 0;
        // 双方手牌各一张（**顺序**：P0 是当前行动方）
        ctx.Players[0].Hand.Add(ctx.NewInstance(hopCard));
        ctx.Players[1].Hand.Add(ctx.NewInstance(hopCard));
        // 棋盘上放一个被打者（P1 侧）与一个「触发 Regiment 的那张」（P1 侧）
        var victim = new UnitState(B94Z2Filler("Z2Victim"), false);
        ctx.Players[1].Board[0] = victim;
        var regiment = new UnitState(B94Z2Filler("Z2Regiment"), false);
        ctx.Players[1].Board[1] = regiment;

        // ---- ① `BroadcastTurnStart`（30 / 31 / 230 / 46）：行动方手牌 3 条 + 对手手牌 1 条 ----
        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.BroadcastTurnStartHandHops(ctx);
        // P0（行动方）：30 · 31 · 230 各一次（46 要先过 `HandTalentGateOpen`：夹具无天赋 ⇒ 不计）；
        // P1（对手）：**只** 30（`CardScript__OnTurnStart.c:140` 的早退）。
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 4,
              "★ `BroadcastTurnStart` 手牌两跳：扫描次数 = **4**（行动方 30/31/230 + 对手 30）"
            + "｜🧨 把两跳写成「只扫行动方」⇒ 3；写成「两方都发 31/230」⇒ 6／8 ⇒ 本行红");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 0,
              "★ 同上：**入队 0**（现池这些 trigger 的 `abilityContext` 全是 `Play(0)` ⇒ 原版在手牌里也拒）"
            + "｜🧨 把 `== AbilityContextHand` 那道闸删掉 ⇒ 本行变 4 ⇒ 红");

        // ---- ② `BroadcastTurnStarted`（160）：两方各一次 ----
        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.BroadcastTurnStartedHandHops(ctx);
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 2,
              "★ `BroadcastTurnStarted` 手牌两跳：扫描次数 = **2**（双方手牌各 1 张，不分侧别）");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 0, "★ 同上：入队 0（池内 `TurnStarted(160)` 的卡 = **0 张**）");

        // ---- ③ `BroadcastUnitPoisoned`（290）：两方各一次 ----
        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.BroadcastUnitPoisonedHandHops(ctx, victim, null);
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 2,
              "★ `BroadcastUnitPoisoned` 手牌两跳：扫描次数 = **2**（原版 `:33-56` 不排除中毒者 / 下毒者）");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 0, "★ 同上：入队 0（池内 `UnitPoisoned(290)` 的卡 = **0 张**）");

        // ---- ④ `BroadcastUnitRegiment`（646）：两方各一次 ----
        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.BroadcastUnitRegimentHandHops(ctx, regiment);
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 2,
              "★ `BroadcastUnitRegiment` 手牌两跳：扫描次数 = **2**（`TriggerOtherCardRegiment` 的自排除对手牌恒不成立）");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 0, "★ 同上：入队 0（池内 `OtherCardRegiment(646)` 的卡 = **0 张**）");

        // ---- ⑤ `BroadcastUnitDamaged`：**只扫当前行动方手牌** + `damageType` 决定 185／187 ----
        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.BroadcastUnitDamagedHandHops(ctx, victim, null, 4);          // 4 = Attack
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 2,
              "★ `BroadcastUnitDamaged`（`damageType = 4 Attack`）：扫描 = **2**（`UnitDamaged(60)` + `UnitDamagedByAttack(187)`，**只有行动方手牌**）"
            + "｜🧨 把 `owner != active` 那道侧别闸删掉 ⇒ 两方各 2 ⇒ 4 ⇒ 本行红");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 0, "★ 同上：入队 0（`60` 池内 3 张全 `Play(0)`、`187` 池内 0 张）");

        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.BroadcastUnitDamagedHandHops(ctx, victim, null, -1);         // -1 = 不知道类型
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 1,
              "★ `BroadcastUnitDamaged`（`damageType = -1` 未知）：扫描 = **1**（**只发 60**）"
            + "｜🧨 把 `-1` 当成 `0 Normal` 处理 ⇒ 变 2（错发 185）⇒ 本行红");

        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.BroadcastUnitDamagedHandHops(ctx, victim, null, 3);          // 3 = Ability
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 1,
              "★ `BroadcastUnitDamaged`（`damageType = 3 Ability`）：扫描 = **1**（60 + 185；夹具没有 185 ⇒ 只有 60）");

        // ---- ⑥ 空实参 / 对局结束：空转，不抛 ----
        RuleCore.ResetHandAbilityCountersForTest();
        Check(RuleCore.BroadcastUnitDamagedHandHops(ctx, null, null, 4), 0, "空实参：`damagedUnit == null` ⇒ 空转（返回 0，不抛）");
        Check(RuleCore.BroadcastUnitPoisonedHandHops(ctx, null, null), 0, "空实参：`poisonedUnit == null` ⇒ 空转（返回 0，不抛）");
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 0, "空实参：一次都没走到闸前（attempts 不变）");
    }

    // ==========================================================================================
    //  本件私有小工具（都带 `Z2` 前缀，⛔ 不与宿主 / 兄弟分册的同名助手冲突）
    // ==========================================================================================

    /// <summary>一个**手搭的**最小对局：两个玩家 + 两个督军（不跑开局的抽牌/洗牌）。
    /// 目的 = 把「棋盘 + 手牌 + 牌库」摆成**测试要的那一个截面**（同 `CheckB93F1UnitPoisonedBroadcast` 的做法）。</summary>
    static BattleContext B94Z2Ctx(int seed)
    {
        var ctx = new BattleContext(seed);
        ctx.Players[0] = new PlayerState();
        ctx.Players[1] = new PlayerState();
        var h0 = new UnitState(B94Z2Hero("Z2HeroP0"), true);
        ctx.Players[0].Warlord = h0; ctx.Players[0].Board[BoardSpec.WarlordSlot] = h0;
        var h1 = new UnitState(B94Z2Hero("Z2HeroP1"), true);
        ctx.Players[1].Warlord = h1; ctx.Players[1].Board[BoardSpec.WarlordSlot] = h1;
        ctx.Active = 0;
        return ctx;
    }

    static CardDef B94Z2Hero(string name)
    {
        return new CardDef(name, name, "hero", "", null, "Test", 0, 2, 30, 0, null, subtype: "Warlord");
    }

    static CardDef B94Z2Filler(string name)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 1, 0, 1, 0, null);
    }

    /// <summary>
    /// 造一张「有 `abilityTriggers` / `abilityContext`」的手牌卡夹具。
    /// 正文走 `desc = "Strike: Draw 1 card"` ⇒ `CardDef.TriggerOps(strike)` 非空
    /// （`EffectText` 的正文关键词头，判据同 `CollectTriggerOps`）。
    /// </summary>
    /// <param name="context">这条 `Strike(580)` 的 `abilityContext`（10 = Hand / 0 = Play）。</param>
    /// <param name="contextRow">`false` = **不落 context 行**（= v14「缺行 = Play(0)」那一档；
    /// ⚠️ 前提是 `abilityContextsVersioned = true`，见 `AbilityContextOfAbilityV14` 的那两条前提）。</param>
    static CardDef B94Z2Fixture(string id, int context, int[] triggers, string[] keywords, string body,
                                bool triggersVersioned = true, bool contextRow = true)
    {
        var ctorCtx = new List<CardDef.AbilityContextSpec>();
        if (contextRow)
            ctorCtx.Add(new CardDef.AbilityContextSpec
            {
                Trigger = 580, Name = "Strike", Keyword = KeywordTable.Strike, Context = context,
            });
        var ta = new CardDef.AbilityTargetSpec[triggers != null ? triggers.Length : 0];
        return new CardDef(id, id, "unit", body ?? "", "common", "Test", 1, 1, 1, 0,
                           keywords,
                           abilityContext: ctorCtx,
                           // 🔴 **恒 true**：这是「表来自 v14」的正面信号。「缺行 = Play(0)」那条口径
                           //   要求**同时**满足它 + `presence == 1`（判据 → `AbilityContextOfAbilityV14` 的 doc）。
                           abilityContextsVersioned: true,
                           abilityTriggers: triggers,
                           abilityTriggersVersioned: triggersVersioned,
                           abilityTargets: ta);
    }
}
