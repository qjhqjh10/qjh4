// B141_W37_Assert.cs — 批次 **B141** · 执行写手 `B141-W37`（`ROWABILITY-EARLYRET` + `SAN27-UNION`）
//
// ============================ 它盯的是什么 ============================
//   A 组：`RuleCore.PublishRowAbilityTargetTable` 的**早退闸** —— 改前是
//         `if (step[2] < 0 || step[3] < 0) return;`，而 `step[3] = row.Ability` 对**主动能力恒为 -1**
//         （`CardDef.ActiveAbilityObject`）⇒ 效果层那张 `ctx.AbilityTargetTable` 对真实主动能力**从未按行挂过**。
//         本件盯的就一件事：**`step[3] = -1`（或 -2）的行现在真的会重挂那张表**，而 `logic < 0` 的行仍然不挂。
//
// ============================ 原版判据（现读）============================
//   · `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:262/273`（第二处 `:1524-1531`）：
//     **每一行**各调一次 `GetTargets(criteria)` 再 `PlayAnim(bm, data, abilityTargets, 0)`
//     ⇒ 效果层的目标集 = **那一行**自己的表（不是「整条 ability 一张」）。
//   · `CardDef.cs:2621-2627` `ActiveAbilityTargetCriteriaAt(abilityObject, logic)`：
//     `abilityObject` **本来就接受** `-1` / `-2`（`ActiveAbilityObject` / `AlternativeAbilityObject`），只有 `logic < 0` 才是「没有这一行」。
//   · 数据面（`cards_engine.json` v49 现读，1142 张）：`activeAbilityLogic[].ab` = `-1` ×132 / `-2` ×1
//     ⇒ `step[3] < 0` 那条闸对 132 张卡**恒为真**（早退），这就是本件修的 bug。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1 / A1c / A1d / A2 / A5b：把闸改回 `if (step[2] < 0 || step[3] < 0) return;` ⇒ **全红**（表恒 null）。
//   · A1b：把 `ctx.AbilityTargetTableOwner = source;` 删掉 ⇒ A1b 红（消费端 `EffectResolver.cs:1927` 靠它认主人）。
//   · A4：把 `ta` 白名单（`ta ∈ {0,10,110,140,150}` ∧ 不限张数）放宽 ⇒ A4 红。
//   · ⚠️ **A3 不是灭自证**（如实标注）：`ca=462` 那行（`logic = -1`）在 `ActiveAbilityTargetCriteriaAt`
//     自己的 `logic < 0` 闸就已经返回 `null`（`CardDef.cs:2623`）⇒ 删掉 `if (step[2] < 0) return;` 它**不会红**。
//     A3/A3b/A3c 的作用是**把这条「正当用途」钉住**（⛔ 修 bug 时别把它当同类一起删）。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加：
//     `Guard("B141W37_Assert", B141_W37_Assert.Run); _pass += B141_W37_Assert.Pass; _fail += B141_W37_Assert.Fail;`
//   （⚠️ 追加的文本必须落在该行第一个 `//` **之前**；接线由调度台统一做，本写手不改那个文件。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B141-W37 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。
/// <para>⚠️ 本件只验**效果层那张表**（`ctx.AbilityTargetTable`）的「按行重挂」；
/// 事件层（`BattleEvent.AbilityTargetList` 的并集）由 `B141_W22_Assert` 负责，⛔ 别在这里重复。</para></summary>
public static class B141_W37_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B141-W37：`PublishRowAbilityTargetTable` 早退闸（`step[3] = -1/-2` 必须放行，`logic < 0` 必须挡）");
        GroupA_RowPublishGate();
        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static CardDef Unit(string id, int cost)
        => new CardDef(id, id, "unit", "", null, "Test", cost, 1, 5, 0, (IEnumerable<string>)null, subtype: "Infantry");

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B141W37Warlord0") };
        var d1 = new List<CardDef> { Hero("B141W37Warlord1") };
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

    /// <summary>合成一张有三行主动能力 criteria 的卡（全部 `ab` 用 `-1` / `-2` —— 真数据就是这样）。</summary>
    static CardDef AbilityRowCard(string id)
    {
        var rows = new List<CardDef.TargetCriteriaRowSpec>();

        // 行 0（`logic = 0`，`ab = -1`）：`ta = 10 allFiltered`，**己方非督军部队**（零随机消费档）
        rows.Add(new CardDef.TargetCriteriaRowSpec
        {
            Ability = CardDef.ActiveAbilityObject, Logic = 0,
            Criteria = new CardDef.CountSetSpec
            {
                TargetsAffected = 10, FriendlyUnits = 1, EnemyUnits = 0,
                IncludeHero = 0, IncludeMinions = 1, IncludeRemnants = 0,
                MaxTargets = 0, TargetSelectMethod = 0,
            },
        });

        // 行 1（`logic = 1`，`ab = -1`）：`ta = 0 any` ⇒ **双方全场**（也走零随机档，用来验「逐行重挂」）
        rows.Add(new CardDef.TargetCriteriaRowSpec
        {
            Ability = CardDef.ActiveAbilityObject, Logic = 1,
            Criteria = new CardDef.CountSetSpec { TargetsAffected = 0, MaxTargets = 0, TargetSelectMethod = 0 },
        });

        // 行 2（`logic = 2`，`ab = -1`）：`ta = 20 self` ⇒ **不在**白名单（验 A4 的「不许顶掉上一张表」）
        //                                —— 也是 A3b 的对照组（证明这份 criteria **本身**是能产表的）
        rows.Add(new CardDef.TargetCriteriaRowSpec
        {
            Ability = CardDef.ActiveAbilityObject, Logic = 2,
            Criteria = new CardDef.CountSetSpec { TargetsAffected = 20, MaxTargets = 0, TargetSelectMethod = 0 },
        });

        // 行 0'（`logic = 0`，`ab = -2` = 替代行动能力对象）：`ta = 10` 同上 —— 验 `-2` 也被接受
        rows.Add(new CardDef.TargetCriteriaRowSpec
        {
            Ability = CardDef.AlternativeAbilityObject, Logic = 0,
            Criteria = new CardDef.CountSetSpec
            {
                TargetsAffected = 10, FriendlyUnits = 1, EnemyUnits = 0,
                IncludeHero = 0, IncludeMinions = 1, IncludeRemnants = 0,
                MaxTargets = 0, TargetSelectMethod = 0,
            },
        });

        return new CardDef(id, id, "unit", "", null, "Test", 3, 1, 5, 0,
                           (IEnumerable<string>)null, subtype: "Infantry",
                           activeAbilityTargetCriteria: rows,
                           activeAbilityTargetCriteriaVersioned: true);
    }

    // ==================================================================
    //  A 组：早退闸（`step = { ca, ta, logic, abilityObject }`）
    // ==================================================================
    static void GroupA_RowPublishGate()
    {
        Section("A `PublishRowAbilityTargetTable`：`abilityObject = -1/-2` 放行 · `logic < 0` 挡 · `ta` 白名单照旧");

        // 前提（⛔ 别写成 `if (…) Check(…)`：前提不成立必须红）
        Check(CardDef.ActiveAbilityObject, -1, "A0 前提：`CardDef.ActiveAbilityObject = -1`（这条就是改前早退的根因）");
        Check(CardDef.AlternativeAbilityObject, -2, "A0b 前提：`CardDef.AlternativeAbilityObject = -2`");

        var card = AbilityRowCard("B141W37_RowCard");
        var ctx = NewCtx();
        int p = ctx.Active;
        var carrier = Place(ctx, p, 5, card);
        var mate1 = Place(ctx, p, 3, Unit("B141W37_Mate1", 1));
        var mate2 = Place(ctx, p, 7, Unit("B141W37_Mate2", 2));
        var foe = Place(ctx, 1 - p, 4, Unit("B141W37_Foe", 2));

        int friendlyMinions = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var q = ctx.Players[p].Board[s];
            if (q != null && q.IsAlive && !q.IsWarlord) friendlyMinions++;
        }
        CheckTrue(friendlyMinions == 3, "A0c 前提：己方棋盘上恰有 3 个**非督军**单位（实际 = " + friendlyMinions + "）");

        var chosenNull = (UnitState)null;
        ctx.AbilityTargetTable = null; ctx.AbilityTargetTableOwner = null;

        // ---- A1：真主动能力那一行（`ab = -1`）—— **改前在这里恒早退 ⇒ 表恒 null** ----
        RuleCore.PublishRowAbilityTargetTableForTest(ctx, p, carrier, chosenNull,
                                                     new[] { 30, 10, 0, CardDef.ActiveAbilityObject });
        var t1 = ctx.AbilityTargetTable;
        CheckTrue(t1 != null && t1.Count == friendlyMinions,
                  "A1 ★ 行 `{ca=30, ta=10, logic=0, ab=-1}` 挂上了**本行**的目标表（= 己方非督军 " + friendlyMinions
                + "；实际 = " + (t1 != null ? t1.Count : -1) + "）—— 闸改回 `|| step[3] < 0` ⇒ 这里恒 null ⇒ 红"
                + "（判据 `AbilityLogic__PlayAbility.c:262/273`）");
        CheckTrue(ReferenceEquals(ctx.AbilityTargetTableOwner, carrier), "A1b 表主人 = 这一行的施放者");
        CheckTrue(t1 != null && !t1.Contains(foe), "A1c ★ `ta=10` 只收**己方**（敌方那一张不在表里）");
        CheckTrue(t1 != null && t1.Contains(mate1) && t1.Contains(mate2), "A1d 两个己方陪练都在表里");

        // ---- A2：替代行动能力对象（`ab = -2`）—— 同样必须放行 ----
        ctx.AbilityTargetTable = null; ctx.AbilityTargetTableOwner = null;
        RuleCore.PublishRowAbilityTargetTableForTest(ctx, p, carrier, chosenNull,
                                                     new[] { 10, 10, 0, CardDef.AlternativeAbilityObject });
        var t2 = ctx.AbilityTargetTable;
        CheckTrue(t2 != null && t2.Count == friendlyMinions,
                  "A2 ★ 行 `ab = -2`（`AlternativeAbilityObject`）也接受（实际 = " + (t2 != null ? t2.Count : -1) + "）"
                + "—— 数据面现读正好有 1 行 `ab=-2`（`SW21`）⇒ 别把闸写成 `step[3] != -1`");

        // ---- A3：`ca == 462` 那类行（`logic = -1`）⇒ **不许动表** ----
        //   ⚠️ 如实标：这一条的**行为**由 `ActiveAbilityTargetCriteriaAt` 自己的 `logic < 0` 闸兜住
        //      （`CardDef.cs:2623`）⇒ 就算把 `PublishRowAbilityTargetTable` 里那句 `if (step[2] < 0) return;`
        //      删掉，A3 也**不会红**（那是 defense-in-depth，不是唯一防线）。所以 A3 拆成两半：
        //      · A3  = 契约断言（`At(-1, -1) == null`）—— 证明「462 行取不到 criteria」这条**前提**成立；
        //      · A3c = 行为断言（表的**对象身份**不变）—— 证明早退闸确实没走「重挂」那条路。
        CheckTrue(card.ActiveAbilityTargetCriteriaAt(CardDef.ActiveAbilityObject, -1) == null,
                  "A3 ★ 契约：`ActiveAbilityTargetCriteriaAt(-1, -1)` = `null`（`ca=462` 那类行没有 logic 二元组；"
                + "这一格是早退闸 `step[2] < 0` 的**正当用途**，⛔ 别把它当成 bug 一起删掉）");
        CheckTrue(card.ActiveAbilityTargetCriteriaAt(CardDef.ActiveAbilityObject, 0) != null,
                  "A3b 契约对照：同一张卡 `logic = 0` 取得到（证明 A3 的 null 不是「这张卡没数据」）");

        RuleCore.PublishRowAbilityTargetTableForTest(ctx, p, carrier, chosenNull,
                                                     new[] { 30, 10, 0, CardDef.ActiveAbilityObject });
        var beforeList = ctx.AbilityTargetTable;
        RuleCore.PublishRowAbilityTargetTableForTest(ctx, p, carrier, chosenNull,
                                                     new[] { 462, 10, -1, CardDef.ActiveAbilityObject });
        CheckTrue(ReferenceEquals(ctx.AbilityTargetTable, beforeList),
                  "A3c ★ `logic = -1`（`ca=462` 那类行）**不重挂** —— 表的**对象身份**不变"
                + "（不是「项数一样」：重挂会 new 一份新表）");

        // ---- A4：白名单（`ta=20` 行**不许**顶掉上一张表）—— 与 `B140_WA` C4 同型，但这一条 `step[3] = -1` ----
        //   🔴 这一条**不是**本件那个 bug 的灭自证（改前改后都绿）—— 它盯的是**修 bug 时最容易顺手犯的错**：
        //      把 `|| step[3] < 0` 删掉的同时把 `ta` 白名单也放宽 ⇒ `ta=20 self` 会把整张表换成「只有施放者自己」。
        beforeList = ctx.AbilityTargetTable;
        RuleCore.PublishRowAbilityTargetTableForTest(ctx, p, carrier, chosenNull,
                                                     new[] { 30, 20, 2, CardDef.ActiveAbilityObject });
        CheckTrue(ReferenceEquals(ctx.AbilityTargetTable, beforeList),
                  "A4 ★ `ta=20 self` **不顶掉**上一张表（白名单 = `{0,10,110,140,150}` ∧ 不限张数；"
                + "去掉白名单 ⇒ 这里变成「只有施放者自己」的新表 ⇒ 红）—— 注意本条 `ab = -1`，验的是闸放宽后白名单仍然生效");

        // ---- A5：**逐行重挂**（不是「整条 ability 只挂一次」）----
        RuleCore.PublishRowAbilityTargetTableForTest(ctx, p, carrier, chosenNull,
                                                     new[] { 30, 10, 0, CardDef.ActiveAbilityObject });
        var row0 = ctx.AbilityTargetTable;
        RuleCore.PublishRowAbilityTargetTableForTest(ctx, p, carrier, chosenNull,
                                                     new[] { 10, 0, 1, CardDef.ActiveAbilityObject });
        var row1 = ctx.AbilityTargetTable;
        CheckTrue(row0 != null && row0.Count == friendlyMinions, "A5 前提：行 0 先挂了己方 " + friendlyMinions + " 个");
        CheckTrue(row1 != null && !ReferenceEquals(row1, row0) && row1.Contains(foe),
                  "A5b ★ 行 1（`ta=0 any` = 双方全场）**换掉了**行 0 的表，且敌方那一张进了新表（实际 = "
                + (row1 != null ? row1.Count : -1) + " 项，含敌方 = " + (row1 != null && row1.Contains(foe)) + "）"
                + " —— 原版是**每一行**各一次 `GetTargets`（`AbilityLogic__PlayAbility.c:262/273`）");
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
        if (_fail == 0) Debug.Log($"B141_W37 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B141_W37 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
