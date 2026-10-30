// B102C506_Assert.cs — 执行代理 `B102_506`（第一百零二会话 · 波 2）
//
// ============================ 它盯的是什么 ============================
//   账 `战斗逻辑驱动:506`（`BattleManager.ResolveCreateHandCard`）的 **R1** 残差：
//   「造进手牌的**破坏卡** → 中央展示」这一跳。
//   原版 `decomp_full/BattleManager._ResolveCreateHandCard_d__512__MoveNext.c:100-102`：
//   对 `rawCardData.spellType == 0xe6(230)` 的造牌**直接调**
//   `CardScript.DisplayEnemyCardInCenter(targetCard, timeToDraw: 0.3f, stepTime: 0.3f, changeVisibility: true)`。
//   我们跨引擎/表现两层，只能发一条事件 ⇒ `EvtKind.SabotageCreated`（发出点
//   `RuleEngine/Core/EffectResolver.cs` 的 `DoCreate` 破坏支 · 消费点
//   `CardPresentation/Battle/BattleDriver.cs` 的 `PlaySignal` → `MaybeDisplayCreatedSabotageCardInCenter`）。
//
// 判据与施工单（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B102_R1_战斗B类三条.md` §一 · ④ R1。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   A1 把 `DoCreate` 里那句 `ctx.Emit(EvtKind.SabotageCreated, …)` 删掉 ⇒ **A1 红**。
//   A2 把闸从 `c.SpellType == SpellTypeSabotage`（=230，原版 `rawCardData.spellType == 0xe6`）
//      退回收 `CreatePool.MatchesKind(c, "sabotage")`（= 按 **subtype 词**）⇒ 夹具里那张
//      `subtype = Sabotage ∧ spellType = -1` 的卡也会命中 ⇒ **A2 红**。
//   A3 把 `foreach (var c in picked)` 改成只发一次（`picked[0]`）⇒ 造两张那条只记 1 条 ⇒ **A3 红**。
//   A4-a 把 `BattleDriver.PlaySignal` 里 `else if (e.Kind == EvtKind.SabotageCreated && animateFeel)` 删掉 ⇒ **A4-a 红**。
//   A4-b 把那条闸写成「不看 `animateFeel`」⇒ 关着 `animateFeel` 也建卡 ⇒ **A4-b 红**。
//   A4-c 把 `owner` 传成造牌方（`e.Player`）而不是收牌方 ⇒ `LastPlayedCenterOwnerForTest` 对不上 ⇒ **A4-c 红**。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `CardPresentation/Editor/BattleScene.cs` 的 `BattleScene.Run` 里，**接在最后一个用 `driver` 的断言段之后**
//   （本节会 `driver.Begin(...)` 另开一局、覆盖宿主的当前对局），**同一物理行末尾**追加这三句：
//     ` B102C506_Assert.Run(); B102C506_Assert.RunDriver(driver); pass += B102C506_Assert.Pass; fail += B102C506_Assert.Fail;`
//   ⚠️ `Pass` / `Fail` 是**累计值** ⇒ 只能在**调完两句之后**加一次（⛔ 别拆成两次 `+=`，那会重复计数）；
//     也 ⛔ 别在两个宿主里各调一次（同一个静态 sink）。
//   ⚠️ `RunDriver` 会 `driver.Begin("Genestealers", "Ultramarines", 20261030)` —— 破坏卡全是 Genestealers
//     （`cards_engine.json` 现读 4 张），阵营不对 `VfxFactionOf` 就查不到那张卡。
//   ⚠️ 必须接在**同一个 `BattleScene.Run` 方法体内**（`driver` 是那里面的局部变量）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B102_506`：账 `战斗逻辑驱动:506` R1（破坏卡「create → 中央展示」挂钩）的断言。</summary>
public static class B102C506_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102C506] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 夹具

    /// <summary>`subtype` 必须给 `"Warlord"`（真卡 56 个督军全是），否则会掉进「原版没给兵种」那一类。</summary>
    static CardDef Hero(string name) =>
        new CardDef(name, name, "hero", "", null, "Test", 0, 2, 30, 0, null, subtype: "Warlord");

    /// <summary>一张普通单位（`source` 位要用它；`ResolveOps` 收 `UnitState source`，⛔ 别传 null 试运气）。</summary>
    static CardDef PlainUnit(string id) =>
        new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 1, 0, (string[])null);

    /// <summary>
    /// 一张**破坏卡**：`subtype = "Sabotage"`（造牌池按兵种词认得它）＋ 可选的 `spellType`。
    /// <para>🔴 `spellType` 这一格就是判据本身：原版闸是 **`rawCardData.spellType == 0xe6(230)`**
    /// （`_ResolveCreateHandCard_d__512__MoveNext.c:90`），⛔ 不是 `subtype == "Sabotage"`。
    /// 现池两口径**恰好等价**（4 张破坏卡两列都对得上），所以只有**夹具**能把它们分开 ——
    /// A2 就靠这一点做灭自证。</para>
    /// </summary>
    static CardDef SabotageCard(string id, int spellType) =>
        new CardDef(id, id, "tactic", "", "common", "Test", 1, 0, 0, 0, (string[])null,
                    subtype: "Sabotage", spellType: spellType);

    static BattleContext NewCtx(IEnumerable<CardDef> cardPool)
    {
        var d0 = new List<CardDef> { Hero("B102C506_W0") };
        for (int i = 0; i < 20; i++) d0.Add(PlainUnit("B102C506_filler" + i));
        var d1 = new List<CardDef> { Hero("B102C506_W1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>(cardPool));
    }

    static UnitState Put(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>`EvtKind.SabotageCreated` 在 `ctx.Signals` 里出现了几条（⛔ 别数 `ctx.Events` —— 那是日志文本）。</summary>
    static int CountSabotageCreated(BattleContext ctx)
    {
        int n = 0;
        var sig = ctx.Signals;
        for (int i = 0; i < sig.Count; i++)
            if (sig[i] != null && sig[i].Kind == EvtKind.SabotageCreated) n++;
        return n;
    }

    /// <summary>造 `amount` 张破坏卡进**敌方**手牌（= 原版 `spellType 0xe6` 的那条路）。</summary>
    static bool CreateIntoHand(BattleContext ctx, string payload, int amount)
    {
        var src = Put(ctx, 0, 3, PlainUnit("B102C506_src"));
        var ops = new List<EffectOp>
        {
            new EffectOp { Verb = "create", Source = "B102C506", Dest = "enemyhand", Amount = amount, Payload = payload },
        };
        return RuleCore.ResolveOps(ctx, 0, src, ops, by: "B102C506 夹具");
    }

    // ============================================================ 入口

    /// <summary>引擎侧（不需要 `BattleDriver`）：A1 / A2 / A3。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B102C506：账 `战斗逻辑驱动:506` R1 —— 破坏卡「create → 中央展示」的**引擎半**");
        SectionA_Engine();
        Debug.Log(_sink.P + "B102C506 引擎半合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    /// <summary>表现侧（要一个已初始化的 `BattleDriver`，它会 `Begin` 一局新对局）：A4。</summary>
    public static void RunDriver(BattleDriver driver)
    {
        MenuCheck.Section(_sink, "B102C506：账 `战斗逻辑驱动:506` R1 —— 破坏卡「create → 中央展示」的**表现半**");
        SectionD_DriverHook(driver);
        Debug.Log(_sink.P + "B102C506 表现半合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 引擎半

    static void SectionA_Engine()
    {
        // ---- A1：造一张破坏卡进敌方手牌 ⇒ **恰好 1 条** `SabotageCreated` ----
        {
            var ctx = NewCtx(new[] { SabotageCard("B102C506_S1", 230) });
            Tr(CreateIntoHand(ctx, "sabotage", 1), "A1 （前提）`create … enemyhand` 这条 op 结算成功");
            C(CountSabotageCreated(ctx), 1,
              "A1 **造一张破坏卡进手牌 ⇒ 恰好 1 条 `EvtKind.SabotageCreated`**"
            + "｜判据 `decomp_full/BattleManager._ResolveCreateHandCard_d__512__MoveNext.c:100-102`"
            + "（每张一次 `DisplayEnemyCardInCenter(…, 0.3f, 0.3f, changeVisibility: true)`）"
            + "｜🧨 把 `DoCreate` 里那句 `ctx.Emit(EvtKind.SabotageCreated, …)` 删掉 ⇒ 变 0 ⇒ 红");
        }

        // ---- A2：**灭自证** —— `subtype = Sabotage` 但 `spellType != 230` ⇒ **0 条** ----
        //   这条把「闸开在 `spellType`（原版 `+0x90 == 0xe6`）上」与「闸开在 subtype 词上」分开：
        //   现池两口径恰好等价 ⇒ 只有夹具能判别。
        {
            var ctx = NewCtx(new[] { SabotageCard("B102C506_S2", -1) });
            Tr(CreateIntoHand(ctx, "sabotage", 1), "A2 （前提）同样造得出来（`subtype` 仍是 `Sabotage`）");
            C(CountSabotageCreated(ctx), 0,
              "A2 **闸开在 `spellType == 0xe6(230)` 上，不是 `subtype == Sabotage`** —— "
            + "`subtype = Sabotage ∧ spellType = -1` 那张 ⇒ **0 条**"
            + "｜判据同 A1（原版读的是 `rawCardData.spellType(+0x90) == 0xe6`）"
            + "｜🧨 把闸退回收 `CreatePool.MatchesKind(c, \"sabotage\")`（按 subtype 词）⇒ 这里变 1 ⇒ 红");
        }

        // ---- A3：一次造两张 ⇒ **2 条**（逐张发，不是「每个 op 发一条」） ----
        {
            var ctx = NewCtx(new[] { SabotageCard("B102C506_S3", 230), SabotageCard("B102C506_S4", 230) });
            Tr(CreateIntoHand(ctx, "sabotage", 2), "A3 （前提）`Amount = 2` 的造牌结算成功");
            C(CountSabotageCreated(ctx), 2,
              "A3 **一次造两张 ⇒ 2 条**（原版是逐张 `targetCard` 各调一次展示，不是每条动作一条）"
            + "｜🧨 把 `foreach (var c in picked)` 改成只发一次（`picked[0]`）⇒ 这里变 1 ⇒ 红");
        }
    }

    // ============================================================ D · 表现半

    static void StepThrough(BattleDriver d, float maxSec = 9f)
    {
        // 批处理没有帧循环 ⇒ 事件排在时间线上但不会自己播（同 `BattleScene.StepThrough` 的口径）。
        const float dt = 1f / 30f;
        float t = 0f;
        while (d != null && d.TimelinePending > 0 && t < maxSec)
        {
            d.AdvanceTimeline(dt);
            CardTween.Advance(dt);
            t += dt;
        }
        if (d == null) return;
        d.AdvanceTimeline(0.1f);
        CardTween.Advance(0.1f);
    }

    static void SectionD_DriverHook(BattleDriver driver)
    {
        Tr(driver != null, "A4-0 （前提）宿主给了一个 `BattleDriver`");
        if (driver == null) return;

        // 🔴 全部破坏卡都是 `Genestealers`（`cards_engine.json` 现读：`Cult Propaganda` /
        //    `Improvised Barricade` / `Jammed Communications` / `Poisoned Supplies`）⇒
        //    我这一侧的督军必须同阵营，`VfxFactionOf` 才查得到那张卡。
        driver.Begin("Genestealers", "Ultramarines", 20261030);
        if (driver.InMulligan) driver.SimulateMulliganDone();
        driver.RefreshAll();
        StepThrough(driver);               // 先把开局那一批排空 —— 不然它的中央展示会算进下面那条前后差
        Tr(driver.Ctx != null, "A4-1 （前提）`Begin` 之后 `driver.Ctx != null`");
        if (driver.Ctx == null) return;

        const string Sabotage = "Cult Propaganda";
        int recv = 1 - driver.MySeat;      // **收牌方**（造牌方 = 我）—— 见 `MaybeDisplayCreatedSabotageCardInCenter` 的方法头

        bool feelWas = driver.animateFeel;
        try
        {
            // ---- A4-a：`animateFeel` 开 ⇒ 收牌方那一侧建 **1** 张，且 `LastPlayedCenterOwnerForTest` = 收牌方 ----
            driver.animateFeel = true;
            int beforeA = driver.PlayedCenterDisplaysForTest;
            driver.Ctx.Signals.Add(new BattleEvent
            {
                Kind = EvtKind.SabotageCreated, Player = driver.MySeat, Slot = -1,
                CardId = Sabotage, TargetPlayer = recv,
            });
            driver.RefreshAll();
            StepThrough(driver);
            int gotA = driver.PlayedCenterDisplaysForTest - beforeA;
            C(gotA, 1,
              "A4-a **`EvtKind.SabotageCreated` ⇒ 真的建了一张中央展示卡**"
            + "｜判据 `_ResolveCreateHandCard_d__512__MoveNext.c:100-102`（`DisplayEnemyCardInCenter(…, 0.3f, 0.3f, true)`）"
            + "｜🧨 把 `BattleDriver.PlaySignal` 的 `else if (e.Kind == EvtKind.SabotageCreated && animateFeel)` 那一支删掉 ⇒ 变 0 ⇒ 红");
            C(driver.LastPlayedCenterOwnerForTest, recv,
              "A4-c 中央展示的 `owner` = **收牌方**（原版搬的是那张卡本体，它在收牌方手牌里 ⇒ 起飞点是那一侧手牌末位）"
            + "｜🧨 把 `owner` 传成造牌方（`e.Player` = 我）⇒ 这里不等 ⇒ 红");

            // ---- A4-b：`animateFeel` 关 ⇒ **一张都不建**（同一道闸） ----
            driver.animateFeel = false;
            int beforeB = driver.PlayedCenterDisplaysForTest;
            driver.Ctx.Signals.Add(new BattleEvent
            {
                Kind = EvtKind.SabotageCreated, Player = driver.MySeat, Slot = -1,
                CardId = Sabotage, TargetPlayer = recv,
            });
            driver.RefreshAll();
            StepThrough(driver);
            C(driver.PlayedCenterDisplaysForTest - beforeB, 0,
              "A4-b **`animateFeel` 关着 ⇒ 不建卡**（与 `EvtKind.Play` 那一条同口径：批处理自检要「当场精确」）"
            + "｜🧨 把闸写成「不看 `animateFeel`」⇒ 这里变 1 ⇒ 红");
        }
        finally
        {
            driver.animateFeel = feelWas;
        }
    }
}
