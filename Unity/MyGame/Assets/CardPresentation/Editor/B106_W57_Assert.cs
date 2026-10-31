// B106_W57_Assert.cs — 执行代理 `B106_W57`（第一百零六会话 · 特效线剩余两件）的断言
//
// ============================ 它盯的是什么 ============================
// 施工单 → `资料/完全复刻/判据/B106_特效线剩余_施工单.md` 第一节的 T21-7 / T21-2。
//
//  **① `Avenging Zeal`（UM40）的触发链今天一次都不播**（`T21-7`）：
//     改前 `TriggeredCodex`(650) 在 `VfxMap.NoEmitTriggerNames`、不在 `IsRoutedTriggerName` 白名单
//     ⇒ 生成器把该行动画放进 `trigger_unmapped`，而 `TryResolveTriggerPicks` 只读 `trigger`/`ability`
//     两桶 ⇒ `FiredCount == 0`。本件把它放行 + 加进 `MustRouteTriggerNames` + 重跑生成器。
//     判据：引擎发射口 = `EffectResolver.TriggerNameOfWhenKind` 的 `case KeywordTable.Codex:
//     return "TriggeredCodex"`；原版 = `CardScript__TriggerOtherCardCodex.c:7-12`（`OnTrigger(0x28a = 650)`）。
//
//  **② 手牌触发的 `InstanceId` 恒 0**（`T21-2`）：`EmitListenerTrigger → EmitCardTrigger` 对
//     `unit == null` 恒写 `InstanceId = 0` ⇒ 手牌里**同名两张**时表现层只能配对到第一张。
//     三处手牌调用点（降费 / 手牌陷阱 / 手牌监听）改传 `CardInstance.Id`。
//     判据：`AbilityLogic__PlayAnim.c:185-222`（逐目标 `IsInHand` ⇒ 用**那张牌自己**的 2D 卡）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加**一个块**（建议紧接 `{ CheckB99E9(); }` 之后）。
// 🔴 **同一物理行追加即可（零行数变化）**：
//       { B106W57(); }
//   🔴 故意不用 `Step(...)`、入口也不带 `Test` 前缀（理由同 `B97_ER_Assert` / `B99E9_Assert`：
//     `Step(...)` 会进 `_ranCases` 记账，而 `工具/gen_diag_runner.py` 只认 8 空格缩进的顶层语句）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · ①「映射」：把 `TriggerNameOfWhenKind` 的 `case KeywordTable.Codex` 删掉 ⇒ 映射红。
//   · ①「件级」：把 `TriggeredCodex` 留在 `NoEmitTriggerNames` / 生成器没重跑 ⇒ `ResolveAllTimed` 取 0 件 ⇒ 红。
//   · ②「区分」：把三处调用点的 `instanceId:` 实参删掉（`EmitCardTrigger` 回到 `: 0`）⇒ 两条事件的
//     `InstanceId` 都变 0 ⇒ 红。反过来，把 `EmitCardTrigger` 的 `: instanceId` 改成「常量 4242」
//     ⇒ 第二条（`0` 那一档）与「等于对应实例 Id」两条同时红。
//   · ②「单位那一趟没被改坏」：把 `unit != null ? unit.InstanceId : instanceId` 改成
//     「一律 `instanceId`」⇒ 单位那条红。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由宿主 `Run()` 里一个 `{ … }` 块调；⛔ 不叫 `Run` / 不带 `Test` 前缀）。</summary>
    public static void B106W57()
    {
        Section("🆕 B106_W57：① `TriggeredCodex` 归口（UM40）· ② 手牌监听 `InstanceId`");
        B106W57Route();
        B106W57InstanceId();
    }

    // ==================================================================
    //  ① `TriggeredCodex`(650) 的触发链
    // ==================================================================

    /// <summary>
    /// 判据链：卡面 `Lower cost by 1 when a friendly Troop or Stratagem trigger [Codex icon] Codex`
    /// → `CardDef.CostWhen`（`EffectText.TryCostWhen`；`[Codex icon]` 由 `WhenEvents.Clean` 剥）
    /// → `WhenEventKind.TriggersPrefix + "codex"` → `TriggerNameOfWhenKind` → `"TriggeredCodex"`(650)
    /// → `VfxMap.ResolveAllTimed(trigger, …, keyword: "TriggeredCodex")` 取到 `Card_Buff_Hand`。
    /// ⛔ 不断「函数返回了字符串」这种没意义的 —— 最后一条断的是**动画桶里真取到那一件**。
    /// </summary>
    static void B106W57Route()
    {
        CardPresentation.VfxMap.EnsureLoaded();   // `ResolveAllTimed` 自己不触发载表 ⇒ 先载（否则逐卡桶是空的、断言会假红）
        var pool = CardDatabase.Load();
        CheckTrue(pool != null && pool.Count > 0,
                  "① （前提）卡池载到（`CardDatabase.Load()` 非空）—— 拿不到 ⇒ 本段后面的断言全部无对象");
        if (pool == null || pool.Count == 0) return;

        var um40 = CardDatabase.FindById(pool, "UM40");
        CheckTrue(um40 != null, "① （前提）卡池里有 `UM40`");
        if (um40 == null) return;
        Check(um40.Name, "Avenging Zeal", "① （前提）`UM40` 就是 `Avenging Zeal`");

        // ---- 前提：正文真的解析出 `triggers:codex` 那条手牌监听（`CostWhens`，不是 `HandTrapWhens`）----
        int hit = -1;
        for (int i = 0; i < um40.CostWhens.Count; i++)
        {
            var ev = um40.CostWhens[i].Ev;
            string k = ev != null ? ev.Kind : null;
            if (k != null
                && k.StartsWith(WhenEventKind.TriggersPrefix, System.StringComparison.Ordinal)
                && k.ToLowerInvariant().EndsWith("codex", System.StringComparison.Ordinal))
            { hit = i; break; }
        }
        CheckTrue(hit >= 0,
                  "① （前提）`UM40` 的正文解析出 `triggers:codex` 那条手牌监听"
                + "｜🧨 解析器不吃 `[Codex icon]` / `Troop or Stratagem` 那一句 ⇒ 这里红（后面的件级断言也就没有意义）");
        if (hit < 0) return;

        string kind = um40.CostWhens[hit].Ev.Kind;
        Check(RuleCore.TriggerNameOfWhenKindForTest(kind), "TriggeredCodex",
              "① ★ 判据：那个 kind 映射到 `TriggeredCodex`（650）"
            + "｜🧨 映射写错（`Codex` / `OtherCardCodex`）或 `case KeywordTable.Codex` 被删 ⇒ 红"
            + "｜⛔ 不是 `Codex`（145 那一族）：650 = 「**别的卡**触发 Codex 时」那一拍");

        Check(RuleCore.HasTriggerSourceForTest(um40, "TriggeredCodex", null, "B106_W57"), true,
              "① （前提）`HasTriggerSource` 放行（`abilityTriggers` 含 650）—— 不过闸 ⇒ 引擎不发事件，后面全空转");

        // ---- 件级：这一拍真取得到动画（不是「整桶」「0 件」）----
        var got = CardPresentation.VfxMap.ResolveAllTimed(
            CardPresentation.VfxMap.Trigger, um40.Faction, "UM40", um40.AnimTimes,
            null, true, "TriggeredCodex");
        bool hasRow = false;
        for (int i = 0; i < got.Length; i++) if (got[i].Name == "Card_Buff_Hand") hasRow = true;
        Check(got.Length, 1,
              "① ★★ 件级：`TriggeredCodex` 这一拍恰好取到 **1 件**（UM40 只有那 1 行）"
            + "｜🧨 `TriggeredCodex` 留在 `NoEmitTriggerNames` / 生成器没重跑 ⇒ 那行还在 `trigger_unmapped`"
            + " ⇒ 取 0 件 ⇒ 红");
        CheckTrue(hasRow,
              "① ★★ 件级：取到的那件是 `Card_Buff_Hand`（原版 `cardAbilities[1].abilityLogic[0].cardAnimations[0]`）"
            + "｜🧨 播成整桶（`play` 桶那两件 `UM_AvengingSon` / `Basic_army_UM`）⇒ 红");
    }

    // ==================================================================
    //  ② 手牌监听 `InstanceId`（同名两张要能区分）
    // ==================================================================

    /// <summary>`ta = 10 (allFiltered)` 的 36 格 criteria（逐格显式给值 —— 缺省是 `Unknown(-1)`，
    /// `TargetCriteriaEval` 遇 `Unknown` 会回 `null`（判不了），夹具会静默变成「什么都没验到」）。
    /// ⚠️ 与 `RuleEngineTest_EngineE2.E2Criteria10` / `B99E9Criteria10` 同形；⛔ 别改成「少给几格」。</summary>
    static CardDef.CountSetSpec B106W57Criteria10(int friendlyUnits, int enemyUnits)
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = 0,
            TargetsAffected = 10,
            ChosenArmy = 0, ArmyFilter = 0,
            KeywordFilter = 0,
            TraitsFilter = 0,
            MaxTargets = 1,
            TargetSelectMethod = 0,
            AttackFilter = 0, AttackFilterOption = 0,
            CurrentHealthFilter = 0, CurrentHealthFilterOption = 0,
            ManaCostFilter = 0, ManaCostFilterOption = 0,
            RarityFilter = 0, RarityFilterOption = 0,
            SpellType = 0,
            FriendlyUnits = friendlyUnits,
            EnemyUnits = enemyUnits,
            IncludeHero = 1, IncludeMinions = 1,
            IncludeSpells = 0, IncludeWhispers = 0, IncludeWeapons = 0,
            IncludeRemnants = 1, IncludeHeroParts = 0,
            IncludeSelf = 0, ExcludeNotSelf = 0, ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0,
            ExcludeRace = 0, ExcludeTrait = 0,
            SelectFromChooseResolution = 0,
        };
    }

    /// <summary>一张**战术卡**、手里监听「一张友军死亡 ⇒ 降 1 费」，带 `abilityTriggers: {90}`（= `OtherUnitDeath`）。
    /// <para>为什么要 `abilityTriggers`：`HasTriggerSource` 的 presence 闸读它 —— 不给 ⇒ `presence == 0`
    /// ⇒ `EmitCardTrigger` **不发事件**，第 ② 件就无对象可量（`B99E9` 那套夹具只验降费、不验事件，所以没这格）。</para>
    /// <para>criteria 照 `B99E9CostCard` 的正例（`ta 10` + 友方 1 / 敌方 0）—— 保证 ability 级那道闸放行。</para></summary>
    static CardDef B106W57ListenerCard(string id)
    {
        var ta = new List<CardDef.AbilityTargetSpec>
        {
            new CardDef.AbilityTargetSpec { Trigger = 90, TargetsAffected = new[] { 10 } },
        };
        var rows = new List<CardDef.TargetCriteriaRowSpec>
        {
            new CardDef.TargetCriteriaRowSpec
            {
                Ability = 0, Trigger = 90, Logic = -1,
                Criteria = B106W57Criteria10(friendlyUnits: 1, enemyUnits: 0),
            },
        };
        return new CardDef(id, id, "tactic", "Lower cost by 1 when a friendly dies.", "common", "Test",
                           cost: 5, attack: 0, health: 0, rangedAttack: 0, keywords: null,
                           abilityTriggers: new[] { 90 },
                           abilityTriggersVersioned: true,
                           abilityTargets: ta.ToArray(),
                           conditionsVersioned: true,
                           abilityLevelCriteria: rows.ToArray(),
                           abilityLevelCriteriaVersioned: true);
    }

    /// <summary>
    /// 判据：`AbilityLogic__PlayAnim.c:185-222`（逐目标 `IsInHand` ⇒ 用**那张牌自己**的 2D 卡）
    /// ⇒ 事件里的 `InstanceId` 必须是**那一份手牌实例**的 `CardInstance.Id`，不是 0、也不是模板 id。
    /// </summary>
    static void B106W57InstanceId()
    {
        var card = B106W57ListenerCard("B106W57Listener");
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        CheckTrue(ctx != null, "② （前提）夹具开出一局（`NewBattle` 回非 null）");
        if (ctx == null) return;

        var i1 = ctx.NewInstance(card);
        var i2 = ctx.NewInstance(card);
        CheckTrue(i1 != null && i2 != null && i1.Id != i2.Id,
                  "② （前提）两张手牌是**不同的实例**（`CardInstance.Id` 不同）");
        CheckTrue(ReferenceEquals(i1.Card, i2.Card), "② （前提）两张共享同一个 `CardDef`（同名同模板）");
        ctx.Players[0].Hand.Add(i1);
        ctx.Players[0].Hand.Add(i2);

        int sigBefore = ctx.Signals.Count;
        try
        {
            // ---- 端到端：一次「友军死亡」广播，两张同名手牌各结算一次、各发一条 `EvtKind.Trigger` ----
            var dead = Place(ctx, 0, 3, Unit("B106W57Dead", 1, 1, 1));
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Die, 0, dead.Card, subject: dead);

            var got = new List<BattleEvent>();
            for (int i = sigBefore; i < ctx.Signals.Count; i++)
            {
                var e = ctx.Signals[i];
                if (e != null && e.Kind == EvtKind.Trigger && e.CardId == card.Name) got.Add(e);
            }
            Check(got.Count, 2,
                  "② ★ 结论：同名两张手牌**各结算一次** ⇒ 2 条 `EvtKind.Trigger`"
                + "｜🧨 只 1 条 ⇒ 第二张的监听根本没结算（夹具/闸的问题，先查前提）");
            if (got.Count >= 2)
            {
                Check(got[0].Slot, -1, "② 结论：手牌那一趟 `Slot == -1`（不在棋盘上）");
                CheckTrue(got[0].InstanceId != got[1].InstanceId,
                          "② ★★ 结论：两条事件的 `InstanceId` **不同**（同名两张能区分）"
                        + "｜🧨 把三处调用点的 `instanceId:` 删掉 / `EmitCardTrigger` 改回 `: 0` ⇒ 都变 0 ⇒ 红");
                var ids = new HashSet<int> { got[0].InstanceId, got[1].InstanceId };
                CheckTrue(ids.Contains(i1.Id) && ids.Contains(i2.Id),
                          "② ★★ 结论：两个 `InstanceId` **各自等于对应手牌实例的 `Id`**（不是别的卡的号）"
                        + "｜🧨 传成模板 id / 对方座位 / 常量 ⇒ 红");
            }

            // ---- 裸发射口：`unit == null` 用调用方给的号；`unit != null` 仍用 `unit.InstanceId`（语义没改）----
            var evtHand = RuleCore.EmitCardTriggerForTest(ctx, card, 0, null, "OtherUnitDeath", "B106W57", 4242);
            CheckTrue(evtHand != null, "② （前提）裸发射口发得出来（`HasTriggerSource` 放行）");
            if (evtHand != null)
                Check(evtHand.InstanceId, 4242,
                      "② 结论：`unit == null` ⇒ `InstanceId` = 调用方给的 `instanceId`（4242，不是常量）"
                    + "｜🧨 改回 `: 0` ⇒ 红");

            var uInst = ctx.NewInstance(card);
            var unit = new UnitState(uInst, false);
            CheckTrue(uInst.Id != 4242, "② （前提）单位实例号 ≠ 4242（否则下一条没有判别力）");
            var evtUnit = RuleCore.EmitCardTriggerForTest(ctx, card, 0, unit, "OtherUnitDeath", "B106W57", 4242);
            CheckTrue(evtUnit != null, "② （前提）带单位的裸发射口也发得出来");
            if (evtUnit != null)
                Check(evtUnit.InstanceId, uInst.Id,
                      "② 结论：`unit != null` ⇒ **仍用 `unit.InstanceId`**（传入的 4242 被忽略 —— 单位那一趟语义没变）"
                    + "｜🧨 改成「一律用 instanceId」⇒ 红");
        }
        finally
        {
            // 还原：本段塞进 `Signals` 的事件全部摘掉；两张夹具手牌摘掉（⛔ 不给后面的断言留污染）
            while (ctx.Signals.Count > sigBefore) ctx.Signals.RemoveAt(ctx.Signals.Count - 1);
            ctx.Players[0].Hand.Remove(i1);
            ctx.Players[0].Hand.Remove(i2);
        }
    }
}
