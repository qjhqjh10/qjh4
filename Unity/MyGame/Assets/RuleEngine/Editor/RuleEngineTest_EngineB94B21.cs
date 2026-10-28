// RuleEngineTest_EngineB94B21.cs — 第九十四会话 · 波 6 · 执行代理 `B94_B21`
//
// 覆盖本批新落的两条**休眠机制本体**（生产端池内 0 张 ⇒ 只能靠合成夹具驱动）：
//   ① 账 642 `BroadcastTurnSetup`         —— 原版 `BattleManagerSupport__BroadcastTurnSetup.c` 三跳
//   ② 账 876 的 550 `Support` 那一跳        —— 原版 `CardScript__ResolveUnitSummoned.c:46-60`
//
// ⚠️ 接线只差 `RuleEngineTest.cs` 的 `Run()` 里**一行**：
//     Step(CheckB94B21DormantBroadcasts);
// 那个文件**归调度台**（本代理没碰，⛔ 不动 `Editor/RuleEngineTest.cs`）。
//
// 🔴 每条断言都带 `🧨`（写清什么改动能让它红）。期望值一律写死字面量（⛔ 不从被测实现反算）。
// 🔴 先断「前提」、再断「结论」（⛔ 不写 `if (前提) Check(结论)` —— 那会在前提不成立时静默跳过）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（⚠️ 不以 `Test`/`Report` 打头，避免被 `VerifyRunCaseCoverage` 认成用例）。</summary>
    public static void CheckB94B21DormantBroadcasts()
    {
        Section("🆕 B94_B21 · 休眠档机制本体：`BroadcastTurnSetup`(155 三跳) · 550 `Support` 召唤跳");
        B94B21Case1TurnSetupBroadcast();
        B94B21Case2SupportOnSummon();
    }

    // ==============================================================================================
    //  ① 账 642 —— `BroadcastTurnSetup`：棋盘那一跳（`TurnSetup` 155）
    // ==============================================================================================

    /// <summary>
    /// **棋盘跳**：场上一张带 `TurnSetup(155)` 能力的单位 ⇒ 广播一次、**恰一条** `EvtKind.Trigger`。
    ///
    /// <para>**判据（🥇 `decomp_full/BattleManagerSupport__BroadcastTurnSetup.c:34-51`）**：
    /// 遍历 `unitsInPlay`，先过 `CardScript.IsInPlay()` ⇒ `CardScript__OnTurnSetup` ⇒ 末尾
    /// `OnTrigger(0x9b = 155)`。</para>
    ///
    /// <para>判别式：把 `HasTurnSetupBody` 那道筛子去掉 ⇒ 「没有 155 的卡」也会多出一条事件 ⇒ 负例红；
    /// 把 `BroadcastTurnSetup` 整个空转 ⇒ 正例红。</para>
    /// </summary>
    static void B94B21Case1TurnSetupBroadcast()
    {
        var withSetup = new CardDef("B21TS", "B21TS", "unit", "", null, "Test", 1, 1, 5, 0,
                                    abilityTriggers: new[] { 155 }, abilityTriggersVersioned: true);
        // 🔴 2026-10-29（`B94_FX8` · DG1 δ2）：对照卡必须**显式声明「版本化 + 空表」** —— `CardDef.AbilityTriggerPresence`
        //   第一句是 `if (!AbilityTriggersVersioned) return -1;`（`CardDef.cs:2462-2466`），默认 ctor 不传就是 **-1 = 未知**。
        //   ⛔ 别把期望改成 -1：`-1`（未知）与 `0`（明确没有）在下面的负例里鉴别力完全不同。
        var noSetup = new CardDef("B21NO", "B21NO", "unit", "", null, "Test", 1, 1, 5, 0,
                                  abilityTriggers: new int[0], abilityTriggersVersioned: true);

        // 🧨 前提硬检查：数据侧真的有 155（枚举名 = `TurnSetup`）
        Check(withSetup.AbilityTriggerPresence("TurnSetup"), 1,
              "🧨 前提：夹具卡的 `abilityTriggers` 含 155 ⇒ `AbilityTriggerPresence(\"TurnSetup\") == 1`");
        Check(noSetup.AbilityTriggerPresence("TurnSetup"), 0,
              "🧨 前提：对照卡的 `AbilityTriggerPresence(\"TurnSetup\") == 0`（明确没有）");

        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var uYes = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Left, 1), withSetup);   // 带 155
        var uNo = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 1), noSetup);     // 不带
        var eYes = Place(ctx, 1, BoardSlots.SlotOf(BoardSlots.Left, 1), withSetup);   // 对面也带 ⇒ 也要响
        CheckTrue(uYes != null && uNo != null && eYes != null, "🧨 前提：三张都摆上了场");

        ctx.Signals.Clear();
        int n = RuleCore.BroadcastTurnSetup(ctx);

        int hits = 0;
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e.Kind == EvtKind.Trigger && e.Keyword == "TurnSetup") hits++;
        }
        CheckTrue(n >= 2,
                  "🧨 前提：`BroadcastTurnSetup` 至少命中 2 张（双方各一张带 155 的）—— 实得 " + n);
        Check(hits, 2,
              "★★ ① 棋盘跳：**双方各一张**带 `TurnSetup(155)` 的单位 ⇒ **恰 2 条** `EvtKind.Trigger(TurnSetup)`"
            + "｜判据 `BattleManagerSupport__BroadcastTurnSetup.c:34-51`（遍历 `unitsInPlay` = **跨双方**的单一列表）"
            + "｜🧨 只扫当前行动方那一侧（实得 1）／ 去掉 `HasTurnSetupBody` 筛子（实得 3，那张没 155 的也会发）⇒ 红");
    }

    // ==============================================================================================
    //  ② 账 876 的 550 `Support` —— `BroadcastSupportOnSummon`
    // ==============================================================================================

    /// <summary>
    /// **550 那一跳**：刚落地的部队 ⇒ **同方其它**带 `Support(550)` 的卡各响一次；
    /// **排除**刚落地那张自己；**不跨方**。
    ///
    /// <para>**判据（🥇 `decomp_full/CardScript__ResolveUnitSummoned.c:46-60`）**：
    /// `op_Inequality(cardPlayed, this)` ∧ `IsUnit(cardPlayed)` ∧
    /// `cardPlayed.isPlayer == this.isPlayer` ⇒ `OnTrigger(0x226 = 550)`。</para>
    ///
    /// <para>判别式（三条负例）：① 把 `ReferenceEquals(t, summoned)` 那句删掉 ⇒ 刚落地那张自己也响 ⇒ 红；
    /// ② 把 `side` 换成「两边都扫」⇒ 对方那张也响 ⇒ 红；③ 把 `HasSupportBody` 删掉 ⇒ 没 550 的卡也响 ⇒ 红。</para>
    /// </summary>
    static void B94B21Case2SupportOnSummon()
    {
        var withSupport = new CardDef("B21SUP", "B21SUP", "unit", "", null, "Test", 1, 1, 5, 0,
                                      abilityTriggers: new[] { 550 }, abilityTriggersVersioned: true);
        // 🔴 2026-10-29（`B94_FX8` · DG1 δ2）：同 `noSetup` —— 「明确没有」= 版本化 + 空表 ⇒ `presence == 0`（不是 -1 未知）。
        var plain = new CardDef("B21PL", "B21PL", "unit", "", null, "Test", 1, 1, 5, 0,
                                abilityTriggers: new int[0], abilityTriggersVersioned: true);
        // ⚠️ 刚落地那张必须是**另一张卡**（卡名不同）：否则「监听者」与「刚落地那张」的 `CardId` 撞车，
        //    按 `CardId` 数事件就分不开，那两条断言会**静默变成假绿**。
        var withSupportSummoned = new CardDef("B21SUP2", "B21SUP2", "unit", "", null, "Test", 1, 1, 5, 0,
                                              abilityTriggers: new[] { 550 }, abilityTriggersVersioned: true);
        // 对面那张同理要**独立卡名**（否则「对方被叫醒」会被数进监听者那一格，变成假绿）。
        var withSupportEnemy = new CardDef("B21SUP3", "B21SUP3", "unit", "", null, "Test", 1, 1, 5, 0,
                                           abilityTriggers: new[] { 550 }, abilityTriggersVersioned: true);

        Check(withSupport.AbilityTriggerPresence("Support"), 1,
              "🧨 前提：夹具卡的 `abilityTriggers` 含 550 ⇒ `AbilityTriggerPresence(\"Support\") == 1`");
        Check(plain.AbilityTriggerPresence("Support"), 0,
              "🧨 前提：对照卡的 `AbilityTriggerPresence(\"Support\") == 0`");

        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        // 本方：一张监听者（550）+ 一张普通 + 刚落地那张（**也让它是 550**，用来验「排除自己」）
        var listener = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Left, 1), withSupport);
        var bystander = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Left, 2), plain);
        var summoned = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 1), withSupportSummoned);
        // 对方也放一张 550 —— **不该**被叫醒
        var enemy = Place(ctx, 1, BoardSlots.SlotOf(BoardSlots.Left, 1), withSupportEnemy);
        CheckTrue(listener != null && bystander != null && summoned != null && enemy != null,
                  "🧨 前提：四张都摆上了场");

        ctx.Signals.Clear();
        int n = RuleCore.BroadcastSupportOnSummon(ctx, 0, summoned);

        int onListener = 0, onSummoned = 0, onBystander = 0, onEnemy = 0;
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e.Kind != EvtKind.Trigger || e.Keyword != "Support") continue;
            if (e.CardId == listener.Card.Name) onListener++;
            else if (e.CardId == summoned.Card.Name) onSummoned++;
            else if (e.CardId == bystander.Card.Name) onBystander++;
            else if (e.CardId == enemy.Card.Name) onEnemy++;
        }
        Check(n, 1,
              "★★ ② `BroadcastSupportOnSummon` **只命中 1 张**（同方那张监听者）"
            + "｜判据 `CardScript__ResolveUnitSummoned.c:46-60`"
            + "｜🧨 两边都扫（实得 2）／ 不排除刚落地那张（实得 2）⇒ 红");
        Check(onListener, 1, "★★ ② 同方监听者身上**恰一条** `EvtKind.Trigger(Support)`｜🧨 空转 ⇒ 实得 0 ⇒ 红");
        Check(onSummoned, 0,
              "★★ ② **刚落地那张自己不算**（原版 `op_Inequality(cardPlayed, this)`）"
            + "｜🧨 删掉 `ReferenceEquals(t, summoned)` 那句 ⇒ 实得 1 ⇒ 红");
        Check(onEnemy, 0,
              "★★ ② **不跨方**（原版要求 `cardPlayed.isPlayer == this.isPlayer`）｜🧨 少写 side 闸 ⇒ 实得 1 ⇒ 红");
        Check(onBystander, 0,
              "★★ ② 没有 550 能力的那张**一条都不发**（原版 `OnTrigger` 内部按 `cardAbilities` 空转）"
            + "｜🧨 去掉 `HasSupportBody` 筛子 ⇒ 实得 1 ⇒ 红（且会污染 `play all dump` 指纹）");
    }
}
