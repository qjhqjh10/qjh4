// B116W27_Assert.cs — 执行代理 `B116_W27`（2026-11-02 · 第一百一十六会话 · 引擎线）
//   文件所有权 = `RuleEngine/Core/CardAbilityDispatch_B116.cs`（新建）· `Core/RuleCore.cs`（**只加同一物理行**）·
//                `Core/EffectResolver.cs`（**只加同一物理行 / 只改条件**）· 本文件（新建）· `Editor/RuleEngineTest.cs`（**同一物理行**）
//
// ============================ 它盯的是什么 ============================
//  活状态表 `B-211`：「`cardAbilities` 逐 ability 派发」这一层 —— 原版 `trigger 0` 的派发形状 =
//  **遍历 `cardAbilities(+0x290)` → 逐条 `CanTriggerAbility` → 逐条 `PlayAbility`**；我们原来只有
//  「把 `desc` 解析成一条扁平 op 流」这一条路 ⇒ 缺的是「一行 ability 一份结算」。
//  本批接两支：`ca 380 setReferenceCard`（3 行）+ 战术路的 `ca 320 followAfterResolution`（15 行）；
//  并落 `B-255` 的**排他闸**（硬编码的 `FollowUpResolution` 那一拍让路，条件必须**数据驱动**）。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//  · `资料/完全复刻/判据/B116_R6_B211逐ability派发施工面.md`（B 表 B1..B9 · D 表 D1..D7）
//  · `资料/完全复刻/判据/B116_R9_B211三个判据空白.md`（320/325/360 三支）
//  · `decomp_full/AbilityLogic__PlayAbility.c:2128-2160`（380 写 `actingCard.referencedCards`）
//  · `decomp_full/AbilityLogic__PlayAbility.c:2244` / `:2471-2478`（320 = `AddFollowAfterResolution`）
//  · `decomp_full/AbilityLogic__GetTargets.c:192-207`（只有 `ta = 30/220` 读调用方点定的那一个）
//  · `decomp_full/RawCardScript__TriggerAbilities.c:201-215`（逐条 `CanTriggerAbility` → 逐条 `TriggerAbility`）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A 段：把 `ca < 0` 折成 `0` ⇒ A3 红；把 `null`（列未知）当「明确没有」⇒ A1 红；把 320 的战术限定去掉 ⇒ A6 红。
//  · B 段：`AbilityLayerOwns320Row` 恒 `true` ⇒ B2（`ASH73`）红；恒 `false` ⇒ B1（15 张）红。**两侧不可能同时绿**。
//  · C 段：不写引用表 ⇒ C2 红；`ta=30` 那一跳改成拿「上一条能力的目标表」⇒ C2/C3 红。
//  · D 段：`ta = 140/150` 那一跳改成无条件拿 `chosen` ⇒ D2 实得 1（期望 3）⇒ 红。
//  · E 段：把 320 也派到单位路上 ⇒ E 实得 2 ⇒ 红（`GOF93` 那种「单位 + 320」就是靠这一条挡住双发）。
//  · F 段：`AbilityLayerOwns320Row` 判据没接上（`ASH73` 也发）⇒ F2 红。
//  · G 段：删掉 B7 双保险 ⇒ G2 实得 1 ⇒ 红。
//  · H 段：把「判不了 ⇒ 放行」改成「判不了 ⇒ 不跑」⇒ H3 红；把「明确不满足 ⇒ 跳过」改成恒跑 ⇒ H1 红。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B116_W27`：`B-211`（`cardAbilities` 逐 ability 派发）＋ `B-255`（排他闸）。</summary>
public static class B116W27_Assert
{
    const string P = "[B116W27] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    static bool Eq(int got, int want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + $"   ✓ {msg}（实际 {got}）"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}｜实际 {got}，期望 {want}"); return false;
    }

    // ============================================================ 夹具
    static CardDef FindCard(List<CardDef> pool, string id)
    {
        if (pool == null) return null;
        for (int i = 0; i < pool.Count; i++) if (pool[i] != null && pool[i].Id == id) return pool[i];
        return null;
    }

    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B116W27Warlord0") };
        var d1 = new List<CardDef> { Hero("B116W27Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        // ⚠️ `Exhausted = false` 必须显式写：`UnitState` 的 ctor 恒 `true`（同 `B102R1_Assert.Place`）。
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static CardDef Body(string id) => new CardDef(id, id, "unit", "", null, "Test", 1, 2, 5, 0);

    /// <summary>「这张卡 trigger 0 / ab / lg」那一行（读不到 ⇒ null）。</summary>
    static CardDef.TriggerAbilityIndexSpec RowOf(CardDef card, int ab, int lg)
    {
        var rows = card != null ? card.TriggerAbilityRowsOf(0) : null;
        if (rows == null) return null;
        for (int i = 0; i < rows.Length; i++)
            if (rows[i] != null && rows[i].Ab == ab && rows[i].Lg == lg) return rows[i];
        return null;
    }

    static int CountFollowUp(BattleContext ctx)
    {
        int n = 0;
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e != null && e.Kind == EvtKind.Trigger && e.Keyword == "FollowUpResolution") n++;
        }
        return n;
    }

    /// <summary>造一张「`trigger 0` 上只有一行 `ca=380`、目标集 `ta=30`」的合成卡。
    /// <para><paramref name="criteriaKind"/>：`0` = 不挂任何 criteria闸；`1` = 挂 **ability 级 `targetCriteria`**
    /// （`CardAbility.targetCriteria +0x30`，`ta=20 self`）；`2` = 挂 **`actingCardCriteria`**
    /// （`+0x28`，同样 `ta=20 self`）。H 段用它们把闸的三种状态（`false` / `true` / `null`）一个个做出来。</para></summary>
    static CardDef GateCard(string id, int criteriaKind)
    {
        var rows = criteriaKind == 0 ? null
                 : new[] { new CardDef.TargetCriteriaRowSpec { Ability = 0, Trigger = 0, Logic = -1,
                            Criteria = new CardDef.CountSetSpec { TargetsAffected = 20 } } };
        var c = new CardDef(id, id, "tactic", "", "common", "Test", 1, 0, 0, 0, null,
            conditionsVersioned: true,
            abilityTargets: new[] { new CardDef.AbilityTargetSpec { Trigger = 0, TargetsAffected = new[] { 30 } } },
            abilityLevelCriteria: criteriaKind == 1 ? rows : null,
            abilityLevelCriteriaVersioned: criteriaKind == 1,
            abilityActingCriteria: criteriaKind == 2 ? rows : null,
            abilityActingCriteriaVersioned: criteriaKind == 2);
        c.AttachTriggerAbilityIndices(new[] { new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = 0, Ca = 380 } });
        return c;
    }

    // ============================================================ 入口
    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            var pool = CardDatabase.Load();
            Ok(pool != null && pool.Count > 0, "前提：卡池读到了（`CardDatabase.Load`）");
            if (pool == null || pool.Count == 0) return;

            SectionA_RowClassifier(pool);
            SectionB_Owns320(pool);
            SectionC_Gof104(pool);
            SectionD_Am54(pool);
            SectionE_UnitPathSkips320(pool);
            SectionF_TacticPathEmits320(pool);
            SectionG_TextCoverage(pool);
            SectionH_GateThreeStates();
        }
        catch (System.Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== B116W27 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ============================================================
    //  A · 行级判定（B 表 B1..B6）—— 纯函数，六组夹具
    // ============================================================
    static void SectionA_RowClassifier(List<CardDef> pool)
    {
        Debug.Log(P + "--- A · `AbilityLayerOwnsRow` 的 B1..B6 ---");
        string why;

        // B1：`triggerAbilityIndices` 列**未知**（夹具卡不挂这一列 ⇒ `_triggerAbilityIndices == null`）
        var noCol = Body("B116W27NoCol");
        Ok(!RuleCore.AbilityLayerOwnsRow(noCol, new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = 0, Ca = 380 }, out why),
           "A1 B1：列**未知**（`null`）⇒ 不进本层（三态之一，⛔ 别与「明确没有」合并）");

        // B2：列在、但这条 trigger 明确没有
        var empty = Body("B116W27Empty");
        empty.AttachTriggerAbilityIndices(new CardDef.TriggerAbilityIndexSpec[0]);
        Ok(!RuleCore.AbilityLayerOwnsRow(empty, new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = 0, Ca = 380 }, out why),
           "A2 B2：明确没有这条 trigger（`[]`）⇒ 不进本层");

        // B3：`ca = -1` = 未知（现池唯一一行 = `GOF97 ab0/lg-1`）
        var gof97 = FindCard(pool, "GOF97");
        var r97 = gof97 != null ? RowOf(gof97, 0, -1) : null;
        Ok(r97 != null && r97.Ca == -1, "A3 **前提**：`GOF97 Gargantuan Squiggoth` 的 `ab0/lg-1` 那一行 `ca = -1`");
        Ok(r97 != null && !RuleCore.AbilityLayerOwnsRow(gof97, r97, out why),
           "A3′ B3：`ca = -1`（未知）⇒ 不进本层（⛔ 别折成 `0 drawCard`）｜🧨 折成 0 ⇒ 本行变 `BodyOp` ⇒ 红");

        // B4：`325/360/380/480` 都是 meta-op ⇒ 本层拥有
        var am54 = FindCard(pool, "AM54");
        var r380 = am54 != null ? RowOf(am54, 0, 1) : null;
        Ok(r380 != null && r380.Ca == 380, "A4 **前提**：`AM54 At All Costs` 的 `ab0/lg1` 那一行 `ca = 380`");
        Ok(r380 != null && RuleCore.AbilityLayerOwnsRow(am54, r380, out why),
           "A4′ B4：`ca = 380` ⇒ 本层拥有（卡面文本结构上表达不出来）");
        var ash73 = FindCard(pool, "ASH73");
        var r325 = ash73 != null ? RowOf(ash73, 0, 1) : null;
        Ok(r325 != null && r325.Ca == 325 && RuleCore.AbilityLayerOwnsRow(ash73, r325, out why),
           "A4″ B4：`ca = 325`（`ASH73`）⇒ 也归本层（本批出声不跑）");

        // B6：真效果种类 ⇒ 归文本层
        var rBody = am54 != null ? RowOf(am54, 0, 0) : null;
        Ok(rBody != null && rBody.Ca == 0 && !RuleCore.AbilityLayerOwnsRow(am54, rBody, out why),
           "A5 B6：`ca = 0 drawCard`（真效果种类）⇒ **不进**本层（卡面正文就在跑它）");

        // B5：320 只归战术/防御路
        var r320 = am54 != null ? RowOf(am54, 0, 2) : null;
        Ok(r320 != null && r320.Ca == 320 && RuleCore.AbilityLayerOwnsRow(am54, r320, out why),
           "A6 B5：`320` 且载体是战术卡（`FollowUpSchedulerOf == ThisCardPlayed`）⇒ 归本层");
        var fake320 = Body("B116W27Fake320");
        Ok(!RuleCore.AbilityLayerOwnsRow(fake320, new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = 0, Ca = 320 }, out why),
           "A6′ B5：`320` 但载体**不是**战术/防御 ⇒ 不进本层（单位卡的 320 由关键词路负责）"
         + "｜🧨 去掉这一条战术限定 ⇒ `GOF93` 那类「单位 + 320」会被本层多派一次（E 段红）");
    }

    // ============================================================
    //  B · `AbilityLayerOwns320Row`（`B-255` 排他闸的唯一判据）—— 数据驱动
    // ============================================================
    static void SectionB_Owns320(List<CardDef> pool)
    {
        Debug.Log(P + "--- B · `AbilityLayerOwns320Row`：18 张硬编码表里谁该让路 ---");
        string[] ids = { "ASH73", "AM54", "AM58", "EC47", "EC49", "SAU61", "GOF104", "GOF102",
                         "EC40", "EC42", "GOF31", "GOF93", "3ebb159036778d14ebd1c48451aa6ca0", "SW56", "TAU40", "UM39" };
        int owned = 0, notOwned = 0;
        for (int i = 0; i < ids.Length; i++)
        {
            var c = FindCard(pool, ids[i]);
            if (c == null) { Ok(false, "B0 **前提**：池里找不到 `" + ids[i] + "`（卡池变了就该红）"); continue; }
            bool o = RuleCore.AbilityLayerOwns320Row(c);
            var rows = c.TriggerAbilityRowsOf(0);
            bool dataHas320 = false;
            if (rows != null) for (int k = 0; k < rows.Length; k++) if (rows[k] != null && rows[k].Ca == 320) dataHas320 = true;
            Ok(o == dataHas320, "B1 `" + ids[i] + "`：让路 ⇔ 数据里确有 `ca 320` 行（本层接管则硬编码让路，净「1 拍」）");
            if (o) owned++; else notOwned++;
        }
        Eq(owned, 15, "B1′ 16 张 `ThisCardPlayed` 表里 **15 张**数据上确有 `ca 320` ⇒ 本层接管（= 现池 320 行 15 行）");
        Eq(notOwned, 1, "B2 **`ASH73` 只有 `ca 325`** ⇒ **不让路**（硬编码那一拍照旧发）"
                      + "｜🧨 `AbilityLayerOwns320Row` 恒 `true` ⇒ 这里变 0、B1 那 15 张也会错 ⇒ 红");
        var ash73 = FindCard(pool, "ASH73");
        Ok(ash73 != null && !RuleCore.AbilityLayerOwns320Row(ash73),
           "B2′ `ASH73 Whirling Death` 明确**不**让路（`FollowUpSchedulerOf` 命中但数据里没有 320 行）"
         + "｜🧨 让路条件写成「表命中就让路」⇒ 它从 1 拍变 0 拍 ⇒ 红");
    }

    // ============================================================
    //  C · `GOF104`（唯一 `ta=30` 的 380 行）—— 最小验证格
    // ============================================================
    static void SectionC_Gof104(List<CardDef> pool)
    {
        Debug.Log(P + "--- C · `GOF104 Drag it Down`：`380 ta=30` 真的写引用表（恰 1 张）---");
        var card = FindCard(pool, "GOF104");
        Ok(card != null, "C0 **前提**：池里有 `GOF104`");
        if (card == null) return;
        var row = RowOf(card, 0, 0);
        Ok(row != null && row.Ca == 380, "C0′ **前提**：它的 `ab0/lg0` 那一行 `ca = 380`");
        Eq(RuleCore.TargetsAffectedOfRow(card, row), 30, "C0″ **前提**：该行 `targetsAffected = 30 target`（`abilityTargets` 列）");

        var ctx = NewCtx();
        int me = ctx.Active;
        var target = Place(ctx, 1 - me, 3, Body("B116W27C_fixture"));
        Ok(target != null, "C0‴ **前提**：敌方摆好一张可点的目标");
        var inst = ctx.NewInstance(card);

        int n = RuleCore.DispatchCardAbilityRowsForTest(ctx, card, inst, 0, me, -1,
                                                       actingCard: target, targetCard: target,
                                                       tacticPath: true, tacticSide: "enemy");
        Eq(n, 2, "C1 派发器认了 **2 行**：`ca 380`（`ta=30`）＋ 同一 trigger 上的 `ca 320`（`chosenAbility` 现读 "
               + "`[30, 260, 320, 380]` ⇒ `ca 320` 也归本层战术路，`AbilityLayerOwnsRow` B5）"
             + "｜⚠️ `B116_W45` 读码推断（改前写 1）⇒ **重跑后待确认**");
        Eq(inst.ReferencedCards.Count, 1, "C2 ★★ 引用表 = **恰 1 条**（`AbilityLogic__PlayAbility.c:2128-2160`）"
                                        + "｜🧨 不写表 ⇒ 0 ⇒ 红");
        Ok(inst.ReferencedCards.Count == 1 && object.ReferenceEquals(inst.ReferencedCards[0].Unit, target),
           "C3 ★★ 那一条就是**调用方点定的那个单位**（`ta=30` 只有这一档读 `param_5`，`GetTargets.c:192-207`）"
         + "｜🧨 拿「上一条能力的目标表」⇒ 表空/条数错 ⇒ 红");

        // 第三次派发：同一份实例再来一次 ⇒ 仍是 1 条（覆盖语义，不是追加）
        RuleCore.DispatchCardAbilityRowsForTest(ctx, card, inst, 0, me, -1,
                                                actingCard: target, targetCard: target,
                                                tacticPath: true, tacticSide: "enemy");
        Eq(inst.ReferencedCards.Count, 1, "C4 「再派一次」仍是 1 条（原版 `:2135-2143` 换新 List = **覆盖**，⛔ 不是 `Add`）");
    }

    // ============================================================
    //  D · `AM54`（`ta=150`）—— 引用表 == 上一条能力的目标表（抽到的 3 张）
    // ============================================================
    static void SectionD_Am54(List<CardDef> pool)
    {
        Debug.Log(P + "--- D · `AM54 At All Costs`：`380 ta=150` ⇒ 引用表 = 上一条能力的目标（恰 3 张）---");
        var card = FindCard(pool, "AM54");
        Ok(card != null, "D0 **前提**：池里有 `AM54`");
        if (card == null) return;
        var row = RowOf(card, 0, 1);
        Ok(row != null && row.Ca == 380, "D0′ **前提**：它的 `ab0/lg1` 那一行 `ca = 380`");
        Eq(RuleCore.TargetsAffectedOfRow(card, row), 150, "D0″ **前提**：该行 `targetsAffected = 150 previousAbilityFiltered`");

        var ctx = NewCtx();
        int me = ctx.Active;
        var c1 = ctx.NewInstance(Body("B116W27D1"));
        var c2 = ctx.NewInstance(Body("B116W27D2"));
        var c3 = ctx.NewInstance(Body("B116W27D3"));
        ctx.PreviousAbilityTargets.Clear();
        ctx.PreviousAbilityTargets.Add(CardRef.Of(c1));
        ctx.PreviousAbilityTargets.Add(CardRef.Of(c2));
        ctx.PreviousAbilityTargets.Add(CardRef.Of(c3));
        var inst = ctx.NewInstance(card);

        int n = RuleCore.DispatchCardAbilityRowsForTest(ctx, card, inst, 0, me, -1,
                                                       actingCard: null, targetCard: null,
                                                       tacticPath: true, tacticSide: "");
        Eq(n, 2, "D1 派发器认了 **2 行**：`ca 380`（`ta=150`）＋ 同一 trigger 上的 `ca 320`（战术路归本层）"
               + "｜⚠️ `B116_W45` 读码推断（改前写 1）⇒ **重跑后待确认**");
        Eq(inst.ReferencedCards.Count, 3, "D2 ★★ 引用表 = **恰 3 条**（= 抽到的那 3 张，`GetTargets.c:867-883`）"
                                        + "｜🧨 不判 `ta`、一律拿 `chosen` ⇒ 实得 0/1 ⇒ 红");
        Ok(inst.ReferencedCards.Count == 3
           && object.ReferenceEquals(inst.ReferencedCards[0].Inst, c1)
           && object.ReferenceEquals(inst.ReferencedCards[1].Inst, c2)
           && object.ReferenceEquals(inst.ReferencedCards[2].Inst, c3),
           "D3 ★★ 逐值同序 = 那 3 份**实例**（按引用认，⛔ 不按卡名）");

        // 表空 ⇒ `if (0 < count)` 不成立 ⇒ **不动引用表**（⛔ 别 `Clear` 掉上一次的结果）
        inst.ReferencedCards.Clear();
        inst.ReferencedCards.Add(CardRef.Of(c1));
        ctx.PreviousAbilityTargets.Clear();
        int n2 = RuleCore.DispatchCardAbilityRowsForTest(ctx, card, inst, 0, me, -1,
                                                         actingCard: null, targetCard: null,
                                                         tacticPath: true, tacticSide: "");
        Eq(n2, 1, "D4 上一张能力的目标表**为空** ⇒ `ca 380` 那一行不结算，但同 trigger 的 `ca 320` 照派 ⇒ 返回 **1**"
                + "（引用表不动那半见 D5）｜⚠️ `B116_W45` 读码推断（改前写 0）⇒ **重跑后待确认**");
        Eq(inst.ReferencedCards.Count, 1, "D5 ★★ 表空时**不动引用表**（原版 `:2131-2132` 的 `if (0 < count)`；⛔ 不是无条件 `Clear`）");
    }

    // ============================================================
    //  E · 单位路**不派** 320（双保险：`GOF93` 那类「单位 + 320」靠这一条挡住双发）
    // ============================================================
    static void SectionE_UnitPathSkips320(List<CardDef> pool)
    {
        Debug.Log(P + "--- E · 单位路（`tacticPath:false`）不派 `320` ---");
        var card = FindCard(pool, "GOF93");
        Ok(card != null, "E0 **前提**：池里有 `GOF93 Hunta Rig`（**单位** 且数据里有 `ca 320` 行）");
        if (card == null) return;
        var r320 = RowOf(card, 0, 2);
        Ok(r320 != null && r320.Ca == 320, "E0′ **前提**：它的 `ab0/lg2` 那一行 `ca = 320`");

        var ctx = NewCtx();
        int me = ctx.Active;
        var self = Place(ctx, me, 3, card);
        Ok(self != null, "E0″ **前提**：把它摆上场（单位路要给 `actingCard`）");
        int before = CountFollowUp(ctx);
        RuleCore.DispatchCardAbilityRowsForTest(ctx, card, self.Instance, 0, me, 3,
                                                actingCard: self, targetCard: null,
                                                tacticPath: false, tacticSide: null);
        Eq(CountFollowUp(ctx) - before, 0,
           "E1 ★★ 单位路派发**一条 `FollowUpResolution` 都不发**（它的 320 由 `FireTriggerDeferred` 的关键词路负责）"
         + "｜🧨 去掉 `AbilityLayerOwnsRow` 的战术限定（或去掉 `tacticPath` 那道闸）⇒ 实得 1 ⇒ 与关键词路**双发** ⇒ 红");
    }

    // ============================================================
    //  F · 战术路**恰发 1 拍**；`ASH73`（无 320 行）**0 拍**（= 硬编码那一拍没被误让路）
    // ============================================================
    static void SectionF_TacticPathEmits320(List<CardDef> pool)
    {
        Debug.Log(P + "--- F · 战术路 `320`：AM54 恰 1 拍 · ASH73 0 拍（让路判据数据驱动）---");
        var am54 = FindCard(pool, "AM54");
        var ash73 = FindCard(pool, "ASH73");
        Ok(am54 != null && ash73 != null, "F0 **前提**：池里有 `AM54` / `ASH73`");
        if (am54 == null || ash73 == null) return;

        var ctx = NewCtx();
        int me = ctx.Active;
        var tgt = Place(ctx, 1 - me, 3, Body("B116W27F_fixture"));
        var i54 = ctx.NewInstance(am54);
        int before = CountFollowUp(ctx);
        RuleCore.DispatchCardAbilityRowsForTest(ctx, am54, i54, 0, me, 3,
                                                actingCard: tgt, targetCard: tgt,
                                                tacticPath: true, tacticSide: "enemy");
        Eq(CountFollowUp(ctx) - before, 1, "F1 ★★ `AM54` 战术路恰 **1 拍** `FollowUpResolution`（`AbilityLogic__PlayAbility.c:2244`）");

        var i73 = ctx.NewInstance(ash73);
        before = CountFollowUp(ctx);
        RuleCore.DispatchCardAbilityRowsForTest(ctx, ash73, i73, 0, me, 3,
                                                actingCard: tgt, targetCard: tgt,
                                                tacticPath: true, tacticSide: "enemy");
        Eq(CountFollowUp(ctx) - before, 0, "F2 ★★ `ASH73` 本层发 **0 拍**（它只有 `ca 325`）⇒ 硬编码那一拍**必须留着**"
                                         + "（`AbilityLayerOwns320Row` 为假）｜🧨 恒让路 ⇒ 全卡 0 拍 ⇒ 红");
        Ok(!RuleCore.AbilityLayerOwns320Row(ash73) && RuleCore.AbilityLayerOwns320Row(am54),
           "F3 ★★ 两条判据**方向相反**（一个让路、一个不让路）⇒ 任何恒 `true` / 恒 `false` 的实现都过不了");
    }

    // ============================================================
    //  G · B7 双保险：正文 op 已覆盖该 ability ⇒ 本层跳过（+ 出声）
    // ============================================================
    static void SectionG_TextCoverage(List<CardDef> pool)
    {
        Debug.Log(P + "--- G · B7 双保险：`TriggerOpsByNumber` 已覆盖该 `ab` ⇒ 本层不跑 ---");
        // ⚠️ 用**合成卡**而不是池内卡：`AttachTriggerOpsByNumber` 会覆盖 `CardDatabase` 挂上去的那张表，
        //    在池内卡上演练会把后续用例（以及别的断言文件）看到的正文桶悄悄换掉（静默）。
        var card = GateCard("B116W27Cover", criteriaKind: 0);
        var row = RowOf(card, 0, 0);
        Ok(row != null && row.Ca == 380, "G0 **前提**：合成卡有 `ab0/lg0 / ca 380 / ta=30` 那一行");

        var ctx = NewCtx();
        int me = ctx.Active;
        var target = Place(ctx, 1 - me, 3, Body("B116W27G_fixture"));
        var inst = ctx.NewInstance(card);

        // 造一份「正文 op 已覆盖 ab 0」的桶（`MapTriggerOpsByNumber` 的等价物，按 `trigger` 分桶）
        var map = new Dictionary<int, List<EffectOp>>();
        map[0] = new List<EffectOp> { new EffectOp { Verb = "damage", Source = "B116W27 G 夹具", AbilityIndex = 0 } };
        card.AttachTriggerOpsByNumber(map);

        int n = RuleCore.DispatchCardAbilityRowsForTest(ctx, card, inst, 0, me, -1,
                                                       actingCard: target, targetCard: target,
                                                       tacticPath: true, tacticSide: "enemy");
        Eq(n, 0, "G1 ★★ 正文已覆盖 `ability#0` ⇒ 本层**一行都不跑**（B7 双保险）");
        Eq(inst.ReferencedCards.Count, 0, "G2 ★★ 引用表**一条都没写**"
                                        + "｜🧨 删掉 B7 那一句 ⇒ 实得 1 ⇒ 与正文双发 ⇒ 红");

        card.AttachTriggerOpsByNumber(null);   // 复原，免得影响别的断言（同一张池内卡被后面的用例复用）
        int n2 = RuleCore.DispatchCardAbilityRowsForTest(ctx, card, inst, 0, me, -1,
                                                         actingCard: target, targetCard: target,
                                                         tacticPath: true, tacticSide: "enemy");
        Eq(n2, 1, "G3 ★★ 同一张卡、只把正文桶摘掉 ⇒ 又跑了 1 行（**同一夹具两个方向**，⛔ 不是「恒不跑」）");
        Eq(inst.ReferencedCards.Count, 1, "G4 表里 1 条（摘桶后恢复正常写入）");
    }

    // ============================================================
    //  H · 逐行 criteria 闸的三态（`false` / `true` / `null`）
    // ============================================================
    static void SectionH_GateThreeStates()
    {
        Debug.Log(P + "--- H · `AbilityCriteriaAllowsAt` 三态：`false` 跳过 / `true` 放行 / `null` **放行**（⛔ 不当作不满足）---");
        var card = GateCard("B116W27Gate", criteriaKind: 1);
        var row = RowOf(card, 0, 0);
        Ok(row != null && row.Ca == 380, "H0 **前提**：合成卡有 `ab0/lg0 / ca 380 / ta=30` 那一行");

        // H1：`ta=20 self` ∧ candidate ≠ thisCard ⇒ **false** ⇒ 跳过
        var ctx1 = NewCtx();
        int me1 = ctx1.Active;
        var a = Place(ctx1, me1, 3, Body("B116W27H_self"));
        var b = Place(ctx1, 1 - me1, 3, Body("B116W27H_other"));
        var i1 = ctx1.NewInstance(card);
        string why1;
        bool? g1 = RuleCore.AbilityCriteriaAllowsAt(ctx1, me1, a, card, 0, a, b, out why1);
        Ok(g1 == false, "H1 **前提**：`ta=20 self` 且候选 ≠ 自己 ⇒ 闸 = **false**");
        Eq(RuleCore.DispatchCardAbilityRowsForTest(ctx1, card, i1, 0, me1, -1, actingCard: a, targetCard: b,
                                                   tacticPath: true, tacticSide: "enemy"), 0,
           "H1′ ★★ 闸 `false` ⇒ 这一行**不跑**（返回 0）");
        Eq(i1.ReferencedCards.Count, 0, "H1″ 引用表没被动（⛔ 「明确不满足」不许还跑）"
                                      + "｜🧨 把它当 `true` ⇒ 实得 1 ⇒ 红");

        // H2：candidate == thisCard ⇒ **true** ⇒ 放行
        var ctx2 = NewCtx();
        int me2 = ctx2.Active;
        var a2 = Place(ctx2, me2, 3, Body("B116W27H_self2"));
        var i2 = ctx2.NewInstance(card);
        string why2;
        bool? g2 = RuleCore.AbilityCriteriaAllowsAt(ctx2, me2, a2, card, 0, a2, a2, out why2);
        Ok(g2 == true, "H2 **前提**：候选 == `thisCard` ⇒ 闸 = **true**");
        Eq(RuleCore.DispatchCardAbilityRowsForTest(ctx2, card, i2, 0, me2, -1, actingCard: a2, targetCard: a2,
                                                   tacticPath: true, tacticSide: ""), 1,
           "H2′ ★★ 闸 `true` ⇒ 跑（返回 1）");
        Eq(i2.ReferencedCards.Count, 1, "H2″ 引用表 1 条");

        // H3：`actingCardCriteria(+0x28)` 在 ∧ `actingCard == null`（战术卡没有 `UnitState`）⇒ **null** ⇒ 放行
        var actingGateCard = GateCard("B116W27Gate2", criteriaKind: 2);
        var ctx3 = NewCtx();
        int me3 = ctx3.Active;
        var c = Place(ctx3, 1 - me3, 3, Body("B116W27H_tgt"));
        var i3 = ctx3.NewInstance(actingGateCard);
        string why3;
        bool? g3 = RuleCore.AbilityCriteriaAllowsAt(ctx3, me3, null, actingGateCard, 0, null, c, out why3);
        Ok(g3 == null, "H3 **前提**：挂了 `actingCardCriteria` 而 `actingCard == null` ⇒ 闸 = **null**（判不了）");
        Eq(RuleCore.DispatchCardAbilityRowsForTest(ctx3, actingGateCard, i3, 0, me3, -1, actingCard: null, targetCard: c,
                                                   tacticPath: true, tacticSide: "enemy"), 1,
           "H3′ ★★ 闸 `null` ⇒ **放行 + 出声**（⛔ 不把「判不了」当「不满足」，与 `CostWhenAbilityCriteriaAllows` 同一口径）"
         + "｜🧨 改成「`null` ⇒ 不跑」⇒ 实得 0 ⇒ 红");
        Eq(i3.ReferencedCards.Count, 1, "H3″ 引用表 1 条（`ta=30` 拿点定的那个目标）");
    }
}
