// B140_WA_Assert.cs — 批次 **B140** · 执行写手 `WA`（`RuleEngine/Core` 四件离线活）
//
// ============================ 它盯的是什么 ============================
//   A 组：`战-U3·T1` —— `CanUseAlternative` 的 `duty` 闸读的是 **`ActiveAbilityUsed`**（原版 `+0x4C`
//                        那个「本局用过任一主动能力族」的布尔位），**不是** `DutyUsed`。
//   B 组：`引擎-N4④` —— `EffectText` 对 `GSC75` 的 `lower their cost by 1` 解出 payload =
//                        `vehicle in your hand`（手牌载具）；其它「指代上一张」的句子**一个都不许变**。
//   C 组：`战-多目标`①②③ —— `targetSelectMethod 2/3 valueInPlay` 的排序键、按行重挂
//                        `ctx.AbilityTargetTable`、两个发点递 `targetCriteria`。
//   D 组：`战-T21`    —— `TriggerTargetSink` 的**手牌通道**（`Slot = -1` + `InstanceId`）。
//
// ============================ 原版判据（现读 `d:/2/tools/decomp_full/` / `out_il2csharp`）============
//   · `CanUseActiveAbility.c:92-97`：`HasCurrentTrait(param_2, 0x4c4 /*duty*/) && *(char*)(param_2 + 0x4C) != 0`
//     ⇒ `return 0`；`+0x4C` 的四个写点 = `CardScript__UsedActiveAbility.c:29/34/41/50`（四族都置）。
//   · `AbilityLogic.cs`（`out_il2csharp`）的 `switch (targetSelectMethod - 1)`：
//     `case 2` ⇒ `OrderByDescending<CardScript,float>(pool, __9__26_3)`；`case 3` ⇒ `OrderBy<CardScript,float>(pool, __9__26_4)`；
//     两个闭包体都是 `return unit.GetValueInPlay();`。
//   · `AI.cs`（ILSpy）`GetCardValueInPlay`：`2×manaCost + 1 + ValueOfEnchantments`，受伤 `×(0.5×cur/max+0.5)`、
//     眩晕/压制 `×0.8`。
//   · `AbilityLogic__PlayAbility.c:262/273`：**每一行**各一次 `GetTargets + PlayAnim`。
//   · `dump.cs:35370`：手牌与场上同为 `CardScript` ⇒ 「这条 ability 落到的目标」可以在手牌里。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2：把 `RuleCore.cs:12145` 那一句改回 `u.DutyUsed` ⇒ A2 红（A3 仍绿 ⇒ 两条必须同时成立）。
//   · A3：把闸写成 `u.DutyUsed || u.ActiveAbilityUsed` ⇒ A3 红。
//   · B1：把 `TryLowerCost` 的 `handKindHint ?? "(指代上一张)"` 改回 `"(指代上一张)"` ⇒ B1 红。
//   · B3/B4：把 `TryForEach` 传 hint 的条件放宽成「任何 `for each` 尾句都传」⇒ B3/B4 红。
//   · C2/C3：把 `KeepByValueInPlay` 的 2/3 两档调换（或都写成升序）⇒ 必有一红。
//   · C4：把 `PublishRowAbilityTargetTable` 的 `ta` 白名单去掉 ⇒ C4b 红（`ta=20 self` 会顶掉整张表）。
//   · D2：把 `NoteTriggerTargetSinkHand` 的 `Slot` 写成 `0`、或不填 `TargetInstanceId` ⇒ D2 红。
//   · D3：把棋盘的 `TriggerTargetSinkDone` 闸删掉 ⇒ D3 红。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加：
//     `Guard("B140WA_Assert", B140_WA_Assert.Run); _pass += B140_WA_Assert.Pass; _fail += B140_WA_Assert.Fail;`
//   （⚠️ 追加的文本必须落在该行第一个 `//` **之前**；接线由调度台统一做，本写手不改那个文件。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B140-WA 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B140_WA_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B140-WA：duty 闸 / GSC75 手牌载具降费 / 多目标三小项 / 触发事件手牌通道");
        GroupA_DutyGate();
        GroupB_Gsc75HandVehicleCost();
        GroupC_MultiTarget();
        GroupD_TriggerSinkHand();
        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static CardDef Unit(string id, int cost, string desc = "")
        => new CardDef(id, id, "unit", desc, null, "Test", cost, 1, 5, 0, (IEnumerable<string>)null, subtype: "Infantry");

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B140WAWarlord0") };
        var d1 = new List<CardDef> { Hero("B140WAWarlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        // ⚠️ `Exhausted = false` 必须显式写：`UnitState` 的 ctor 恒 `true`。
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    // ==================================================================
    //  A 组：`duty` 闸读 `ActiveAbilityUsed`（`战-U3·T1`）
    // ==================================================================
    static void GroupA_DutyGate()
    {
        Section("A `CanUseAlternative(…, \"duty\")` 的闸 = `ActiveAbilityUsed`（原版 `+0x4C`）");
        var card = Unit("B140WA_DutyUnit", 2, "Duty: Gain +1 Health");
        var ctx = NewCtx();
        int p = ctx.Active;
        var u = Place(ctx, p, 3, card);

        // 前提（⛔ 不改写成 `if (…) Check(…)`：前提不成立必须红）
        CheckTrue(RuleCore.AlternativeActionKeyword(u) == "duty", "A0 前提：这一张的替代行动关键词 = `duty`");
        CheckTrue(!u.ActiveAbilityUsed && !u.DutyUsed, "A0b 前提：`ActiveAbilityUsed` / `DutyUsed` 开局都是假");
        Check(RuleCore.CanUseAlternative(ctx, p, 3, "duty"), RuleCodes.OK, "A1 两个都没用过 ⇒ 放行");

        u.ActiveAbilityUsed = true; u.DutyUsed = false;
        Check(RuleCore.CanUseAlternative(ctx, p, 3, "duty"), RuleCodes.ErrDutyUsed,
              "A2 ★ 本局用过**别的**主动能力族（`ActiveAbilityUsed` 真、`DutyUsed` 假）⇒ **也必须拒**"
            + "（改前读 `DutyUsed` ⇒ 这里是 OK ⇒ 红；判据 `CanUseActiveAbility.c:92-97`）");

        u.ActiveAbilityUsed = false; u.DutyUsed = true;
        Check(RuleCore.CanUseAlternative(ctx, p, 3, "duty"), RuleCodes.OK,
              "A3 ★ 反方向：只有 `DutyUsed` 真（`ActiveAbilityUsed` 假）⇒ **放行** —— 判据是 `+0x4C` 那一位，"
            + "⛔ 别写成 `DutyUsed || ActiveAbilityUsed`（那样 A3 红）");
    }

    // ==================================================================
    //  B 组：`GSC75` 的 `lower their cost` 指手牌载具（`引擎-N4④`）
    // ==================================================================
    static void GroupB_Gsc75HandVehicleCost()
    {
        Section("B `EffectText`：`GSC75` 的 `lower their cost by 1` ⇒ payload = `vehicle in your hand`");
        // 卡面原文（`cards_engine.json` 的 `GSC75.desc`，逐字抄）
        const string gsc75 = "Deal 1 damage to a random enemy and Stun it. "
                           + "Repeat for each Vehicle in your hand and lower their cost by 1";
        string lower;
        int ops;
        var parsed = ParseAndFindLowerCost(gsc75, out lower, out ops);

        CheckTrue(parsed, "B0 前提：`GSC75` 那句真的解出了一条 `lowercost` op");
        Check(ops, 4, "B0b 前提：整段解出 4 条 op（deal / stun / repeat / lowercost）");
        Check(lower, "vehicle in your hand",
              "B1 ★ 载荷 = 手牌载具（改前 = `(指代上一张)` ⇒ 结算层会去降「刚造出来/刚抽到」的那批 ⇒ 红；"
            + "判据 = 卡面原文 + `AbilityLogic__GetTargets.c:884-892` 的 `inHandFiltered(50)`）");

        // 反例：通用支**一个都不许动**（`AM54` / `GSC29` / `GSC55` 那一族）
        string hit;
        ParseAndFindLowerCost("Draw 3 cards and lower their cost by 3", out hit, out ops);
        Check(hit, "(指代上一张)", "B2 反例①（`Draw N cards and lower their cost by N`）⇒ 仍走 `(指代上一张)`");
        ParseAndFindLowerCost("Draw it and lower its cost by 3", out hit, out ops);
        Check(hit, "(指代上一张)", "B3 反例②（`Draw it and lower its cost by N`）⇒ 仍走 `(指代上一张)`");
        ParseAndFindLowerCost("Return up to 5 friendly troops from play to your deck and lower their cost by 3", out hit, out ops);
        Check(hit, "(指代上一张)", "B4 反例③（`Return … and lower their cost by N`）⇒ 仍走 `(指代上一张)`（hint 只由 `repeat … in your hand` 那一支传）");

        // 反例：`for each … in your hand` 但**不是** `repeat`（`GSC56` 那一类计数句）⇒ 也不许带 hint
        ParseAndFindLowerCost("Deal 1 damage for each card in the opponent's hand and lower their cost by 1", out hit, out ops);
        CheckTrue(hit == "(指代上一张)" || hit == null,
                  "B5 反例④（`for each … in <谁的> hand` 但不是 `repeat`）⇒ 不许带 hint（实际 = " + hit + "）");
    }

    /// <summary>解一条 desc，找出里面**唯一**那条 `lowercost` op 的 `Payload`（顺带报总 op 数）。</summary>
    static bool ParseAndFindLowerCost(string desc, out string payload, out int opCount)
    {
        payload = null; opCount = 0;
        List<string> unparsed, partial;
        var ops = EffectText.Parse(desc, out unparsed, out partial);
        if (ops == null) return false;
        opCount = ops.Count;
        for (int i = 0; i < ops.Count; i++)
            if (ops[i] != null && ops[i].Verb == "lowercost") { payload = ops[i].Payload; return true; }
        return false;
    }

    // ==================================================================
    //  C 组：`战-多目标` ① ② ③
    // ==================================================================
    static void GroupC_MultiTarget()
    {
        Section("C① `targetSelectMethod 2/3 = valueInPlay`（`OrderByDescending` / `OrderBy`）");
        var ctx = NewCtx();
        int p = ctx.Active;
        var cheap = Place(ctx, p, 3, Unit("B140WA_Cheap", 1));
        var pricey = Place(ctx, p, 5, Unit("B140WA_Pricey", 6));
        CheckTrue(SimpleAI.ValueInPlay(pricey) > SimpleAI.ValueInPlay(cheap),
                  "C0 前提：贵的那张卡价值更高（`SimpleAI.ValueInPlay` = `2×费+1 + …`）");

        var hi = PickOne(ctx, p, cheap, 2);
        CheckTrue(hi.HasValue && hi.Value.CardId == pricey.Name, "C1 ★ `targetSelectMethod=2` 留**价值最高**的那个"
            + "（实际 = " + (hi.HasValue ? hi.Value.CardId : "null") + "；判据 `out_il2csharp AbilityLogic.cs` `case 2` ⇒ `OrderByDescending(GetValueInPlay)`）");
        var lo = PickOne(ctx, p, cheap, 3);
        CheckTrue(lo.HasValue && lo.Value.CardId == cheap.Name, "C2 ★ `targetSelectMethod=3` 留**价值最低**的那个"
            + "（实际 = " + (lo.HasValue ? lo.Value.CardId : "null") + "；同处 `case 3` ⇒ `OrderBy(GetValueInPlay)`）");

        Section("C② 按行重挂 `ctx.AbilityTargetTable`（`每行 ability 各一次`）");
        var rowCard = AbilityRowCard("B140WA_RowCard", taRow0: 10);
        var ctx2 = NewCtx();
        int p2 = ctx2.Active;
        var carrier = Place(ctx2, p2, 5, rowCard);
        Place(ctx2, p2, 3, Unit("B140WA_RowA", 1));
        Place(ctx2, p2, 7, Unit("B140WA_RowB", 2));
        var chosenNull = (UnitState)null;
        int expectFriendlyMinions = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var q = ctx2.Players[p2].Board[s];
            if (q != null && q.IsAlive && !q.IsWarlord) expectFriendlyMinions++;
        }
        CheckTrue(expectFriendlyMinions == 3, "C0b 前提：己方棋盘上恰有 3 个**非督军**单位（实际 = " + expectFriendlyMinions + "）");

        // 第一行 `ta = 10 allFiltered`（零随机消费档）⇒ 应当被挂上
        RuleCore.PublishRowAbilityTargetTableForTest(ctx2, p2, carrier, chosenNull, new[] { 30, 10, 0, CardDef.ActiveAbilityObject });
        CheckTrue(ctx2.AbilityTargetTable != null && ctx2.AbilityTargetTable.Count == expectFriendlyMinions,
                  "C3 ★ 行 `ta=10` 挂上了**本行**的目标表（= 己方非督军 3 个；实际 = "
                + (ctx2.AbilityTargetTable != null ? ctx2.AbilityTargetTable.Count : -1) + "）");
        CheckTrue(ReferenceEquals(ctx2.AbilityTargetTableOwner, carrier), "C3b 表主人 = 这一行的施放者");

        // 第二行 `ta = 20 self`（**不在**零随机白名单里）⇒ 不许动上一张表
        RuleCore.PublishRowAbilityTargetTableForTest(ctx2, p2, carrier, chosenNull, new[] { 30, 20, 1, CardDef.ActiveAbilityObject });
        CheckTrue(ctx2.AbilityTargetTable != null && ctx2.AbilityTargetTable.Count == expectFriendlyMinions,
                  "C4 ★ 行 `ta=20 self` **不顶掉**上一张表（白名单 = `{0,10,110,140,150}` ∧ 不限张数；"
                + "去掉白名单 ⇒ 这里变 1 ⇒ 红）");

        Section("C③ 两个发点递 `targetCriteria`（`AttachAbilityTargetsWithCriteria` 的零回归面）");
        // 认不出 trigger 名 ⇒ 与改前逐字等价（`AbilityTargetList` 仍是 `AbilityTargetsFor` 那一份）
        CheckTrue(RuleCore.TriggerAbilityTargetCriteriaForTest(rowCard, null) == null,
                  "C5 前提：`triggerName == null` ⇒ criteria 取不到（这条保证 C6 走的是「原样不动」那一支）");
        CheckTrue(RuleCore.TriggerAbilityTargetCriteriaForTest(rowCard, "NoSuchTrigger") == null,
                  "C6 ★ 认不出的 trigger 名 ⇒ `null`（调用方的 `AttachAbilityTargetsWithCriteria` 因此**原样不动**，"
                + "⛔ 不许抛、也不许退回别的口径）");
    }

    /// <summary>`ta = 10 allFiltered` + `maxTargets = 1` + `targetSelectMethod = <paramref name="method"/>` 的判据表。</summary>
    static BattleEventTarget? PickOne(BattleContext ctx, int owner, UnitState source, int method)
    {
        var set = new CardDef.CountSetSpec
        {
            TargetsAffected = 10, FriendlyUnits = 1, EnemyUnits = 0,
            IncludeHero = 0, IncludeMinions = 1, IncludeRemnants = 0,
            MaxTargets = 1, TargetSelectMethod = method,
        };
        var list = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, owner, set, source, null);
        if (list == null || list.Count != 1) return null;
        return list[0];
    }

    /// <summary>合成一张「主动能力 `logic` 第 0 行 `ta = <paramref name="taRow0"/>`」的卡（`logic` 第 1 行 `ta = 20 self`）。</summary>
    static CardDef AbilityRowCard(string id, int taRow0)
    {
        var rows = new List<CardDef.TargetCriteriaRowSpec>();
        rows.Add(new CardDef.TargetCriteriaRowSpec
        {
            Ability = CardDef.ActiveAbilityObject, Logic = 0,
            Criteria = new CardDef.CountSetSpec
            {
                TargetsAffected = taRow0, FriendlyUnits = 1, EnemyUnits = 0,
                IncludeHero = 0, IncludeMinions = 1, IncludeRemnants = 0,
                MaxTargets = 0, TargetSelectMethod = 0,
            },
        });
        rows.Add(new CardDef.TargetCriteriaRowSpec
        {
            Ability = CardDef.ActiveAbilityObject, Logic = 1,
            Criteria = new CardDef.CountSetSpec { TargetsAffected = 20, MaxTargets = 0, TargetSelectMethod = 0 },
        });
        return new CardDef(id, id, "unit", "", null, "Test", 3, 1, 5, 0,
                           (IEnumerable<string>)null, subtype: "Infantry",
                           activeAbilityTargetCriteria: rows,
                           activeAbilityTargetCriteriaVersioned: true);
    }

    // ==================================================================
    //  D 组：`TriggerTargetSink` 的手牌通道（`战-T21`）
    // ==================================================================
    static void GroupD_TriggerSinkHand()
    {
        Section("D `NoteTriggerTargetSinkHand`：`Slot = -1` + `InstanceId`（与 `BattleEventTarget` 同形）");
        var ctx = NewCtx();
        int p = ctx.Active;
        var handCard = Unit("B140WA_HandCard", 2);
        var inst = ctx.NewInstance(handCard);
        ctx.Players[p].Hand.Add(inst);

        var evt = new BattleEvent { Kind = EvtKind.Trigger };
        ctx.TriggerTargetSink = evt; ctx.TriggerTargetSinkDone = false;
        RuleCore.NoteTriggerTargetSinkHandForTest(ctx, new List<CardRef> { CardRef.Of(inst) });

        Check(ctx.TriggerTargetSinkDone, true, "D1 前提：手牌通道真的落了目标（`TriggerTargetSinkDone`）");
        Check(evt.TargetPlayer, p, "D2a ★ 座位 = 手牌那一方");
        Check(evt.TargetSlot, -1, "D2b ★ `Slot = -1`（不在棋盘上 —— `BattleEventTarget.Slot = -1` 同一约定）");
        Check(evt.TargetCardId, handCard.Name, "D2c 卡名填了");
        Check(evt.TargetInstanceId, inst.Id, "D2d ★ `InstanceId` 填了（`BattleDriver.AbilityTargetView` 的 `Slot = -1` 手牌回落靠它；不填 ⇒ 红）");

        // 灭自证①：棋盘那一路已经填过 ⇒ 手牌通道**不许覆盖**
        var evt2 = new BattleEvent { Kind = EvtKind.Trigger, TargetPlayer = 1 - p, TargetSlot = 3, TargetCardId = "BoardTarget" };
        ctx.TriggerTargetSink = evt2; ctx.TriggerTargetSinkDone = true;
        RuleCore.NoteTriggerTargetSinkHandForTest(ctx, new List<CardRef> { CardRef.Of(inst) });
        Check(evt2.TargetPlayer, 1 - p, "D3 ★ 棋盘通道已填 ⇒ 手牌**不覆盖**（去掉 `TriggerTargetSinkDone` 闸 ⇒ 这里变成 "
            + p + " ⇒ 红）");
        Check(evt2.TargetSlot, 3, "D3b 同上：`Slot` 保持棋盘那一格");

        // 灭自证②：棋盘载体那一项（`CardRef.Unit != null`）不归本通道
        var boardUnit = Place(ctx, p, 3, Unit("B140WA_BoardUnit", 1));
        var evt3 = new BattleEvent { Kind = EvtKind.Trigger };
        ctx.TriggerTargetSink = evt3; ctx.TriggerTargetSinkDone = false;
        RuleCore.NoteTriggerTargetSinkHandForTest(ctx, new List<CardRef> { CardRef.Of(boardUnit) });
        Check(ctx.TriggerTargetSinkDone, false, "D4 ★ 棋盘载体那一项**不进**手牌通道（`CardRef.Unit != null` ⇒ 跳过）");
        Check(evt3.TargetSlot, -1, "D4b 同上：什么字段都没被写");

        // 灭自证③：模板降级项（没有实例号）⇒ 不拿 0 顶
        var tpl = Unit("B140WA_TemplateOnly", 1);
        var evt4 = new BattleEvent { Kind = EvtKind.Trigger };
        ctx.TriggerTargetSink = evt4; ctx.TriggerTargetSinkDone = false;
        RuleCore.NoteTriggerTargetSinkHandForTest(ctx, new List<CardRef> { CardRef.Of(tpl) });
        Check(ctx.TriggerTargetSinkDone, false, "D5 ★ 没有实例的模板项 ⇒ 跳过（⛔ 不拿 `InstanceId = 0` 顶）");
    }

    // ==================================================================
    //  口令 / 汇总
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B140_WA 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B140_WA 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
