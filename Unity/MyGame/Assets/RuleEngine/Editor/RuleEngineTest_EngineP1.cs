// RuleEngineTest_EngineP1.cs — 引擎线 `P1` 三条（2026-10-23）的回归断言
//
// 判据（⛔ 不要重新论证）→ `资料/普查产出_1023c/`：
//   · `引擎深审A_规则语义.md` §二 2·6 / §三 P1·1 —— ① 触发优先级在 `WhenEvent` 一族上整层丢失；
//   · `引擎深C_数据吻合.md` §2·2 / §五·3       —— ② `goesSecond` 缺 6 位督军（EC / SW 两族）；
//   · `引擎深C_数据吻合.md` §四·1 / §五·4       —— ③ 拿「动画行在不在」当「ability 在不在」的闸。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 **期望值一律写死字面量**（`2` / `"EC59"` / `30` …）—— ⛔ 不从被测实现反算（那会自证）。
// 🔴 **成对/灭自证**：每条「新写法的观测值」都配一条「旧写法 / 反例」的观测值，
//    两者**结构上不可能同时满足**（例：① 的 `[SAU1, DA14, DA12]` vs `[DA12, SAU1, DA14]`；
//    ③ 的「有动画行 24 张」vs「有 ability 30 张」；② 的「旧表 55 条」vs「现算 64 条」）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批**唯一入口**（`Run()` 里一行 `Step(TestEngineP1)`）。</summary>
    public static void TestEngineP1()
    {
        Section("🆕 引擎线 `P1` 三条：触发优先级（WhenEvent 族）/ `goesSecond` 补齐 / ability 闸");
        W67T1AbilityTriggerPriorityData();
        W67T2AbilityTriggerPriorityLane();
        W67T3AbilityGateDouble();
        W67T4GoesSecondCoverage();
        W67T5GoesSecondEndToEnd();
    }

    // ==================================================================
    //  W67T1 · 数据层：`abilityTriggerPriority`（v18）落进了 `CardDef`
    // ==================================================================
    /// <summary>
    /// 真值（`cards_raw.json` 的 `cardAbilities[].priority`）：池内非默认共 **7 条**，
    /// 其中 **3 条**是 v18 这一列新增的（v13 的关键词那一列覆盖不到）：
    /// `DA12` OnCollectQuestPoints=2 · `BL72` UnitReceivesMarkOfChaos=2 · `DA14` FollowUpResolution=0。
    /// 🧨 生成器不落这一列 / `CardDatabase` 不接这一跳 ⇒ 全池扫描 0 条、逐卡取值回 1 ⇒ 红。
    /// 🧨 把两列合并成一列（键 = 引擎关键词）⇒ `DA12.AbilityTriggerPriority("OnCollectQuestPoints")`
    ///    查不到 ⇒ 红（见下面那条「两列不是一套主键」的灭自证）。
    /// </summary>
    static void W67T1AbilityTriggerPriorityData()
    {
        var pool = CardDatabase.Load();
        CheckTrue(pool.Count >= 1000, $"W67T1 夹具：卡表加载 {pool.Count} 张（应 ≥ 1000）");

        var da12 = CardDatabase.FindById(pool, "DA12");   // Company Veteran
        var bl72 = CardDatabase.FindById(pool, "BL72");   // Accursed Helbrute
        var da14 = CardDatabase.FindById(pool, "DA14");   // Ravenwing Speeder
        var ash53 = CardDatabase.FindById(pool, "ASH53"); // Reclaim the Stars
        CheckTrue(da12 != null && bl72 != null && da14 != null && ash53 != null,
                  "W67T1 夹具：四张样本卡都在池里（DA12 / BL72 / DA14 / ASH53）");
        if (da12 == null || bl72 == null || da14 == null || ash53 == null) return;

        // ---- ① 三张「本列新增」的卡（判据见方法 doc）----
        Check(da12.AbilityTriggerPriority("OnCollectQuestPoints"), 2,
              "★ W67T1 `DA12 Company Veteran` 的 `OnCollectQuestPoints(695)` = **2 Interrupt**（原版真值）");
        Check(bl72.AbilityTriggerPriority("UnitReceivesMarkOfChaos"), 2,
              "★ W67T1 `BL72 Accursed Helbrute` 的 `UnitReceivesMarkOfChaos(510)` = **2 Interrupt**（原版真值）");
        Check(da14.AbilityTriggerPriority("FollowUpResolution"), 0,
              "★ W67T1 `DA14 Ravenwing Speeder` 的 `FollowUpResolution(410)` = **0 Player**（原版真值）");

        // ---- ② 缺省契约：没登记 / 认不出的名字 ⇒ 1（Instant），⛔ 不是 0 ----
        //   🧨 访问器改成「查不到 ⇒ 0（Player）」⇒ 监听器会全部掉进最后才结算的那一道 ⇒ 红。
        Check(da12.AbilityTriggerPriority("NoSuchTriggerName"), 1,
              "★ W67T1 未登记的 trigger 名 ⇒ 默认 **1 = Instant**（⛔ 不是 0 = Player）");
        CheckTrue(da12.AbilityTriggerPrioritiesVersioned,
                  "W67T1 `DA12` 的 `abilityTriggerPriority` 列**来自 v18 卡表**（正面信号）"
                + " —— 🧨 `CardDatabase` 忘记传 `version >= 18` 那一格 ⇒ 红");

        // ---- ③ 两列**不是一套主键**（灭自证的关键）----
        //   `triggerPriority`（v13）的键是**引擎关键词**；698/510/410 这三条**没有关键词**
        //   ⇒ 同一张卡、同一条 ability，关键词那一列查不到、本列查得到。
        //   🧨 「把两列合并」或「让本列也按关键词存」⇒ 下面第一条会变成 2 ⇒ 红。
        Check(da12.TriggerPriority("OnCollectQuestPoints"), 1,
              "🧨 W67T1 灭自证：`DA12` 的 **关键词列**（`triggerPriority`）里没有 `OnCollectQuestPoints` ⇒ 1");
        //   而两列的**确有交集**的地方必须逐值吻合（同一份真值的两个投影）：
        Check(ash53.TriggerPriority("spiritstone"), 3,
              "W67T1 `ASH53` 关键词列 `spiritstone` = 3（v13 现读值，改前就有的那 4 条之一）");
        Check(ash53.AbilityTriggerPriority("UseSpiritStone"), 3,
              "★ W67T1 **同一个真值、两个投影**：`ASH53` 的 `UseSpiritStone(600)` 在本列也是 **3**");

        // ---- ④ 全池扫描：非默认条目必须**恰好**是这 7 条（漏接线 ⇒ 0、多落 ⇒ 8+）----
        var got = new List<string>();
        int cells = 0;
        foreach (var c in pool)
        {
            if (c == null) continue;
            foreach (var kv in c.AbilityTriggerPriorities)
            {
                cells++;
                if (kv.Value != CardDef.DefaultTriggerPriority) got.Add(c.Id + "/" + kv.Key + "=" + kv.Value);
            }
        }
        got.Sort(StringComparer.Ordinal);
        Check(cells, 7, "★ W67T1 全池 `abilityTriggerPriority` 的**条目数** = 7（2026-10-23 现读）");
        Check(string.Join(",", got.ToArray()),
              "ASH53/UseSpiritStone=3,BL72/UnitReceivesMarkOfChaos=2,DA12/OnCollectQuestPoints=2,"
            + "DA14/FollowUpResolution=0,SAU13/Artifice=3,SAU70/Artifice=3,UM37/Codex=3",
              "★ W67T1 全池非默认条目的**逐条清单**（`id/trigger名=值`，Ordinal 排序）");

        // ---- ⑤ 空 logic 的那条**仍然不落**（GOF97 陷阱没被这一列绕过）----
        var gof97 = CardDatabase.FindById(pool, "GOF97");
        CheckTrue(gof97 != null, "W67T1 夹具：`GOF97` 在池里");
        if (gof97 != null)
            Check(gof97.AbilityTriggerPriorities.Count, 0,
                  "★ W67T1 `GOF97`（`trigger=0 / priority=0 / abilityLogic=[]` 的占位能力）⇒ **一条都不落**"
                + " —— 🧨 生成器去掉「空 logic 不落」那道闸 ⇒ 这里变成 1 条 `ThisCardPlayed=0` ⇒ 红");
    }

    // ==================================================================
    //  W67T2 · 道接线：`ListenerLane(card, triggerName)` 读数据，泵序真的变
    // ==================================================================
    /// <summary>
    /// 判别式（报告 §三 P1·1 的验收条件）：
    ///   · **跨道**：按 `[SAU1(默认 Instant), DA14(Player), DA12(Interrupt)]` 的**入队序**排进去，
    ///     泵出来的执行序必须是 `DA12 → SAU1 → DA14`；
    ///     🧨 道不读数据（全落 Instant）⇒ 执行序 = 入队序 `SAU1,DA14,DA12` ⇒ 红（这就是改前的形态）。
    ///   · **Player 道最后结算**（原版消费序 2→1→3→0 的最后一档）。
    /// </summary>
    static void W67T2AbilityTriggerPriorityLane()
    {
        var pool = CardDatabase.Load();
        var da12 = CardDatabase.FindById(pool, "DA12");
        var da14 = CardDatabase.FindById(pool, "DA14");
        var sau1 = CardDatabase.FindById(pool, "SAU1");
        CheckTrue(da12 != null && da14 != null && sau1 != null,
                  "W67T2 夹具：`DA12` / `DA14` / `SAU1` 都在池里");
        if (da12 == null || da14 == null || sau1 == null) return;

        // ---- ① 三个取道口（直读数据）----
        Check(RuleCore.ListenerLane(da12, "OnCollectQuestPoints"), ActionPriority.Interrupt,
              "★ W67T2 `ListenerLane(DA12, \"OnCollectQuestPoints\")` = **Interrupt(2)**");
        Check(RuleCore.ListenerLane(da14, "FollowUpResolution"), ActionPriority.Player,
              "★ W67T2 `ListenerLane(DA14, \"FollowUpResolution\")` = **Player(0)**");
        Check(RuleCore.ListenerLane(sau1, "OnCollectQuestPoints"), ActionPriority.Instant,
              "W67T2 `SAU1` 没有这条 ⇒ **Instant(1)**（缺省契约）");
        // 认不出是哪条 ability（调用点没传名字）⇒ 默认道（⛔ 不许按卡名/正文猜一条）
        Check(RuleCore.ListenerLane(da12, null), ActionPriority.Instant,
              "★ W67T2 不传 trigger 名 ⇒ **Instant(1)**（认不出就不猜）");
        Check(RuleCore.ListenerLane(null, "OnCollectQuestPoints"), ActionPriority.Instant,
              "W67T2 没有载体卡（`null`）⇒ **Instant(1)**");

        // ---- ② 端到端：入队序 vs 泵出序（跨道 priority 压过入队序）----
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var order = new List<string>();
        RuleCore.EnqueueAbility(ctx, RuleCore.ListenerLane(sau1, null), "sau1", () => order.Add("SAU1"));
        RuleCore.EnqueueAbility(ctx, RuleCore.ListenerLane(da14, "FollowUpResolution"), "da14", () => order.Add("DA14"));
        RuleCore.EnqueueAbility(ctx, RuleCore.ListenerLane(da12, "OnCollectQuestPoints"), "da12", () => order.Add("DA12"));
        Check(ctx.ActionQueue.Count, 3, "W67T2 夹具：三条都在队列里（入队 ≠ 结算）");
        RuleCore.PumpActionQueues(ctx);
        Check(string.Join(",", order.ToArray()), "DA12,SAU1,DA14",
              "★ W67T2 泵出序 = `DA12,SAU1,DA14`（Interrupt → Instant → Player）"
            + " —— 🧨 道不读数据（全 Instant，FIFO）⇒ 得到 `SAU1,DA14,DA12` ⇒ 红");
        Check(ctx.ActionQueue.Count, 0, "W67T2 泵完 ⇒ 队列空");

        // ---- ③ 灭自证：同一夹具在**没有数据**时的序（= 改前形态）----
        //   与 ② **结构上不可能同时满足**：同一批动作、只换「道从哪来」。
        var ctx2 = Battle(new CardDef[0], new CardDef[0]);
        var order2 = new List<string>();
        RuleCore.EnqueueAbility(ctx2, ActionPriority.Instant, "sau1", () => order2.Add("SAU1"));
        RuleCore.EnqueueAbility(ctx2, ActionPriority.Instant, "da14", () => order2.Add("DA14"));
        RuleCore.EnqueueAbility(ctx2, ActionPriority.Instant, "da12", () => order2.Add("DA12"));
        RuleCore.PumpActionQueues(ctx2);
        Check(string.Join(",", order2.ToArray()), "SAU1,DA14,DA12",
              "🧨 W67T2 灭自证：**全部 Instant** 时泵出序 = 入队序（`SAU1,DA14,DA12`）"
            + " —— 与上面那条不同 ⇒ 上面那条**只能是数据换来的**");
    }

    // ==================================================================
    //  W67T3 · 闸：`EmitCardTrigger` 的判据从「有动画行」扩成「有动画行 **或** 有 ability」
    // ==================================================================
    /// <summary>
    /// 真值（池内 1,127 张，2026-10-23 现读）：
    ///   · `FollowUpResolution(410)`：有 ability **30** 张 / 有动画行 **24** 张 ⇒ **6 张**只在前者里
    ///     （`EC40`/`EC42`/`GOF31`/`GOF93`/`TL3`/`UM64`）；
    ///   · `TurnStartPlayer(31)`：**8 / 4** ⇒ 缺 **4**；`TurnStart(30)`：5 / 3 ⇒ 缺 2。
    ///     🔴 2026-10-25（`X20`）：31 这一对 7/3 → **8/4**（池内新增的隐藏附魔 `Pray Enchantment`
    ///       自己有 31 这条 ability、`animTimes` 里也真有 `Pray_Success` 那条 31 动画行 ⇒ 两边各 +1，
    ///       差值仍是 4）。归因 → `资料/普查产出_1025/X20_PrayEnchantment入池_执行报告.md`。
    /// 🧨 闸改回只看 `HasAnimTrigger` ⇒ 「只发得出 24 张」那条成立、发射断言红（`TL3` 不发事件）。
    /// 🧨 把闸**整个删掉** ⇒ `DA12`（没有 410、也没有 410 动画行）也会发 ⇒ 下面的反例红。
    /// </summary>
    static void W67T3AbilityGateDouble()
    {
        var pool = CardDatabase.Load();

        // ---- ① 名字 → 数值（③ 依赖这张表；抄漏 = 静默少发）----
        Check(CardDef.AbilityTriggerNumber("FollowUpResolution"), 410, "W67T3 `FollowUpResolution` = 410");
        Check(CardDef.AbilityTriggerNumber("TurnStartPlayer"), 31, "W67T3 `TurnStartPlayer` = 31");
        Check(CardDef.AbilityTriggerNumber("OtherUnitDeath"), 90, "W67T3 `OtherUnitDeath` = 90");
        Check(CardDef.AbilityTriggerNumber("OnCollectQuestPoints"), 695, "W67T3 `OnCollectQuestPoints` = 695");
        Check(CardDef.AbilityTriggerNumber("NoSuchTriggerName"), -1,
              "W67T3 认不出的名字 ⇒ **-1 = 未知**（⛔ 不猜 0 = ThisCardPlayed）");

        // ---- ② 全池：两个集合的**大小**（灭自证：它们必须不同）----
        Check(W67CountByAnim(pool, "FollowUpResolution"), 24,
              "★ W67T3 有 `FollowUpResolution` **动画行**的卡 = 24 张（旧闸只认这一批）");
        Check(W67CountByAbility(pool, "FollowUpResolution"), 30,
              "★ W67T3 有 `FollowUpResolution` **ability**的卡 = 30 张（真值 `abilityTriggers`）"
            + " —— 🧨 两数相等 ⇒ 说明数据/闸没区分开，本组断言失去鉴别力");
        Check(W67CountByAnim(pool, "TurnStartPlayer"), 4,
              "W67T3 有 `TurnStartPlayer(31)` 动画行 = **4** 张（`X20` 新增的附魔卡带 `Pray_Success` 那条）");
        Check(W67CountByAbility(pool, "TurnStartPlayer"), 8,
              "W67T3 有 `TurnStartPlayer(31)` ability = **8** 张（缺 4 张动画行；`X20` +1）");
        Check(W67CountByAnim(pool, "TurnStart"), 3, "W67T3 有 `TurnStart(30)` 动画行 = 3 张");
        Check(W67CountByAbility(pool, "TurnStart"), 5, "W67T3 有 `TurnStart(30)` ability = 5 张（缺 2 张）");

        // ---- ③ 发射口（公开路径）：有 ability、没动画行 ⇒ **照发**（P1·③ 的本体）----
        //   走 `ResolveAtTurn` 这一条真路径（`EmitTurnTrigger` 用的是**同一个** `HasTriggerSource` 闸）。
        //   `GOF86 Unleashed Tramplasquig`：`At the start of each turn …` + `animTimes` **空表**
        //   + `abilityTriggers` 含 30 ⇒ 正是「有身体没动画」那一档。
        var gof86 = CardDatabase.FindById(pool, "GOF86");
        var gunDrone = CardDatabase.FindById(pool, "TAU6");   // 有 TurnStart 动画行的对照卡
        CheckTrue(gof86 != null && gunDrone != null, "W67T3 夹具：`GOF86` / `TAU6` 都在池里");
        if (gof86 == null || gunDrone == null) return;
        CheckTrue(!gof86.HasAnimTrigger("TurnStart") && gof86.AbilityTriggerPresence("TurnStart") == 1,
                  "🧪 W67T3 夹具：`GOF86` **没有** `TurnStart` 动画行、但**有** 30 这条 ability"
                + "（判据报告点名的那一档）");
        CheckTrue(gunDrone.HasAnimTrigger("TurnStart"),
                  "🧪 W67T3 对照夹具：`TAU6 Gun Drone` **有** `TurnStart` 动画行（改前就发得出的那批）");

        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Players[0].Energy = 99;
        Check(ctx.Active, 0, "🧪 W67T3 夹具：P1 是当前行动方（`TAU6` 是 `your turn` 的从句，换边它就不触发）");
        Place(ctx, 0, 2, gof86, exhausted: true);
        Place(ctx, 0, 4, gunDrone, exhausted: true);
        ctx.ClearSignals();
        RuleCore.ResolveAtTurn(ctx, "turn_start");
        int gofN = 0, droneN = 0;
        foreach (var e in ctx.Signals)
        {
            if (e == null || e.Kind != EvtKind.Trigger) continue;
            // ⚠️ `e.CardId` 里装的是**卡名**（`ctx.Emit(kind, player, slot, cardId…)` 的第四参在
            //    `EmitTurnTrigger` 传的是 `card.Name`）—— ⛔ 不是 `CardDef.Id`。
            if (e.CardId == gof86.Name && e.Keyword == "TurnStart") gofN++;
            if (e.CardId == gunDrone.Name && e.Keyword == "TurnStart") droneN++;
        }
        Check(droneN, 1,
              "W67T3 对照：`TAU6`（**有动画行**）照旧发 1 条 `TurnStart`（改前行为逐字不变）");
        Check(gofN, 1,
              "★ W67T3 `GOF86`（**有 ability、没动画行**）**也发了 1 条** `TurnStart`"
            + " —— 🧨 闸只认 `HasAnimTrigger` ⇒ 0 条 ⇒ 红");

        // ---- ④ 灭自证：**两样都没有/未知**的卡 ⇒ 一条都不发（⛔ 不是「把闸删掉」）----
        //   合成夹具（`abilityTriggers` 旧表 = `-1` 未知、`animTimes` 空）＋ 一条 at-turn 正文：
        //   闸若被整个删掉（`TurnTriggerNameOf` 恒取 `names[0]`）⇒ 它也会发一条 `TurnStart` ⇒ 红。
        var noData = new CardDef("W67NoData", "W67NoData", "unit",
                                 "At the start of your turn, gain 1 Health",
                                 "common", "Test", 1, 1, 5, 0, null, subtype: "Infantry");
        Check(noData.AbilityTriggerPresence("TurnStart"), -1,
              "🧪 W67T3 夹具：合成卡的 `abilityTriggers` = **-1 未知**（旧表口径）");
        CheckTrue(!noData.HasAnimTrigger("TurnStart"), "🧪 W67T3 夹具：合成卡没有 `TurnStart` 动画行");
        // ⚠️ **单独一局**：只摆它一个 ⇒ 队列里那一条**只可能**是它的 at-turn 作业
        //   （夹具自检：作业真排上了，否则「0 条信号」是**空断言**）。
        var ctx3 = Battle(new CardDef[0], new CardDef[0]);
        Check(ctx3.Active, 0, "🧪 W67T3 夹具（隔离局）：P1 是当前行动方");
        Place(ctx3, 0, 2, noData, exhausted: true);
        ctx3.ClearSignals();
        RuleCore.ResolveAtTurn(ctx3, "turn_start");
        Check(ctx3.ActionQueue.Count, 1,
              "🧪 W67T3 夹具自检（隔离局）：它的 at-turn 作业**排进了队列**（正文解析得出来）"
            + " —— 🧨 解析不出 ⇒ 下面的「0 条信号」是假通过 ⇒ 本条红");
        int noDataN = 0;
        foreach (var e in ctx3.Signals)
            if (e != null && e.Kind == EvtKind.Trigger && e.CardId == noData.Name) noDataN++;
        Check(noDataN, 0,
              "★ W67T3 **灭自证**：两样都没有 / 未知的卡 ⇒ **0 条** `TurnStart`"
            + " —— 🧨 把闸整个删掉（不做双闸）⇒ 这里冒出 1 条 ⇒ 红");
    }

    // ==================================================================
    //  W67T4 · `goesSecond`：13 个阵营 / 56 位督军全覆盖
    // ==================================================================
    /// <summary>
    /// 真值（`cards_raw.json` 的 `full_raw.goSecondCardInHand` PPtr，2026-10-23 现算）：
    /// **64 条**（56 张真 id + 8 张重复 SO），**13 个阵营各一张**；旧中间表只有 **55 条 / 11 个阵营**
    /// ⇒ EmperorsChildren / SpaceWolves 6 位督军后手拿不到防御卡。
    /// 🧨 不重生成中间表 ⇒ `EC55`/`SW1` 的 `GoesSecondCard` 为 null ⇒ 红。
    /// 🧨 按「11 个阵营」写死一条白名单 ⇒ 13 那一格红。
    /// </summary>
    static void W67T4GoesSecondCoverage()
    {
        var pool = CardDatabase.Load();
        int heroes = 0, withCard = 0;
        var missing = new List<string>();
        var factions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in pool)
        {
            if (c == null || c.Type != "hero") continue;
            heroes++;
            if (c.GoesSecondCard != null) { withCard++; factions.Add(c.Faction); }
            else missing.Add(c.Id + "/" + c.Name);
        }
        Check(heroes, 56, "W67T4 尺子：全池督军 56 张（2026-10-23 现读）");
        Check(withCard, 56,
              "★ W67T4 **每一位督军都有 `goesSecond`**（56/56）"
            + " —— 缺的：" + string.Join("、", missing.ToArray()));
        Check(factions.Count, 13, "★ W67T4 覆盖 **13 个阵营**（改前只有 11 —— 缺 EC / SW）");
        Check(missing.Count, 0, "★ W67T4 空着的督军 = 0 张"
                              + " —— 🧨 中间表退回 2025-07-30 旧快照（55 条）⇒ EC×3 + SW×3 全空 ⇒ 红");

        // ---- 逐张（这 6 位就是审计点名的那 6 张）----
        var expect = new[]
        {
            new[] { "EC55", "EC59" }, new[] { "EC56", "EC59" }, new[] { "EC57", "EC59" },
            new[] { "SW1", "SW59" }, new[] { "SW2", "SW59" }, new[] { "SW3", "SW59" },
        };
        foreach (var pair in expect)
        {
            var w = CardDatabase.FindById(pool, pair[0]);
            CheckTrue(w != null, $"W67T4 夹具：督军 `{pair[0]}` 在池里");
            if (w == null) continue;
            var got = w.GoesSecondCard;
            CheckTrue(got != null && got.Id == pair[1],
                      $"★ W67T4 `{pair[0]} {w.Name}` 的后手防御卡 = `{pair[1]}`"
                    + $" —— 实得「{(got == null ? "**空**" : got.Id + " " + got.Name)}」");
            if (got != null)
            {
                CheckTrue(got.Type == "defence" && DeckRules.SameFaction(got.Faction, w.Faction),
                          $"W67T4 `{pair[0]}` 那张**是同阵营的防御卡**（`{got.Id}` / {got.Faction} / {got.Type}）");
            }
        }
    }

    // ==================================================================
    //  W67T5 · 端到端：电脑（EC55）后手**真的拿到** `EC59 Vial Tanks`
    // ==================================================================
    /// <summary>
    /// 走真实的 `NewBattle`（`StartBattlePhase` → `AddGoesSecondCardToDeck` → 第 ③ 档
    /// 「督军自己的 `goSecondCardInHand`」；`matchType` 默认经典 → 翻转 `RankedBot 20`，
    /// **不在** `{50,80,110,120}` 那四档里 ⇒ 正好走 ③）。
    /// 🧨 数据没接 ⇒ 手里 0 张防御卡（改前就是这个症状）。
    /// </summary>
    static void W67T5GoesSecondEndToEnd()
    {
        var pool = CardDatabase.Load();
        var ec55 = CardDatabase.FindById(pool, "EC55");
        CheckTrue(ec55 != null, "W67T5 夹具：`EC55 Lucius the Eternal` 在池里");
        if (ec55 == null) return;

        // 玩家（P1）阵营要给一个有防御卡的阵营 —— `GoesSecondCard` 第 ③ 档的第二道闸
        //   （`PlayerHasOwnFactionDefence`：玩家收藏里有「自阵营的防御卡」）本项目恒真，但**要先有阵营**。
        var playerDeck = new List<CardDef> { HeroOf("W67Warlord", "Ultramarines", 2, 30) };
        for (int i = 0; i < 20; i++) playerDeck.Add(Unit("w67filler" + i, 1, 0, 1));

        var botDeck = new List<CardDef> { ec55 };
        for (int i = 0; i < 20; i++) botDeck.Add(Unit("w67bfiller" + i, 1, 0, 1));

        var ctx = RuleCore.NewBattle(playerDeck, botDeck, seed: 62, shuffle: false,
                                     firstSeat: 0, cardPool: pool, botSeat: 1);
        Check(ctx.SecondSeat, 1, "W67T5 夹具：玩家先手 ⇒ 电脑（P2）是后手");
        CheckTrue(ctx.IsBotSeat(1), "W67T5 夹具：P2 是电脑（③ 那一支只对电脑开）");

        CardDef got = null;
        int n = 0;
        foreach (var h in ctx.Players[1].Hand)
            if (h != null && h.Card != null && h.Card.Type == "defence") { n++; got = h.Card; }
        Check(n, 1, "★ W67T5 电脑（EC55 当督军）后手手里**正好 1 张**防御卡");
        CheckTrue(got != null && got.Id == "EC59",
                  $"★ W67T5 那张就是 `EC59 Vial Tanks` —— 实得「{(got == null ? "**没有**" : got.Id + " " + got.Name)}」"
                + " —— 🧨 中间表退回旧快照（EC 族整族缺席）⇒ 0 张 ⇒ 红");
    }

    // ---- 本组小工具（`W67` 打头 = 辅助方法，不会被 `RunCoverageCompiledCases` 当成用例）----

    static int W67CountByAnim(IReadOnlyList<CardDef> pool, string triggerName)
    {
        int n = 0;
        foreach (var c in pool) if (c != null && c.HasAnimTrigger(triggerName)) n++;
        return n;
    }

    static int W67CountByAbility(IReadOnlyList<CardDef> pool, string triggerName)
    {
        int n = 0;
        foreach (var c in pool) if (c != null && c.AbilityTriggerPresence(triggerName) == 1) n++;
        return n;
    }

    static CardDef W67FindByAnim(IReadOnlyList<CardDef> pool, string triggerName)
    {
        foreach (var c in pool) if (c != null && c.HasAnimTrigger(triggerName)) return c;
        return null;
    }
}
