// B143_R3_Assert.cs — 批次 **B143** · 执行写手 `B143-R3`
//   文件所有权 = `RuleEngine/Core/CardAbilityDispatch_B116.cs`（手牌上下文 `ca` 派发那一段）· 本文件（新建）
//
// ============================ 它盯的是什么 ============================
//  活状态表 `B141-HAND-CA100`：`ca 100 createCardInHand` 落在**手牌上下文**（`abilityContext = 10 Hand`）
//  时，我们这一层没有结构化执行器。查证结论（判据 → `资料/完全复刻/判据/B143_手牌施放者模型.md` §4）：
//  池内唯一那一行（`Cult Propaganda`，trigger 40 TurnEnd）**已经由 at-turn 文本路结算**
//  （`RuleCore.ResolveAtTurn → AtTurnHandJobDeferred → ResolveOps → DoCreate`）⇒ 再写结构化执行器
//  会**每回合双造一张**。所以本笔把它判成 `HandRowKind.CoveredByText`（**Zero behavioural change**）：
//  不入队、不派发、不出声，但也不再误报成「缺口」。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · **A3**：把 `HandAbilityRowKindOf` 里 `ca 100` 那一支删掉（回落 Unsupported）⇒ A3 红。
//  · **A4**：把 `CoveredByText` 并进 `Owned`（或删掉 `HasRows` 那处 `continue`）⇒ A4 变 true ⇒ 红。
//  · **A5**：⚠️ **它单靠自己恒真**（`Unsupported` 档也返回 0、也不动手牌）⇒ 判别力来自
//    **A5b 的出声计数**（`CoveredByText` 必须一声不吭）+ **A6 正控**（`ca 160` 必须真弃 1 张）。
//    删 `DispatchHandAbilityRows` 里 `CoveredByText` 那个 `continue` ⇒ 静默落到 `NoteHandAbilityRowUnsupported` ⇒ A5b 红。
//  · **B1**：把「没有正文」也判成 CoveredByText（无条件返回 true）⇒ B1 红。
//  · **B2**：忽略相位/trigger 匹配 ⇒ B2 红。
//  · **B3**：把 `ca 160 ∧ ta 20` 的 Owned 也改坏 ⇒ B3 红（防误伤同族）。
//  · **B4**：把「卡面任何一句都算覆盖」⇒ B4（非 at-turn 正文）红。
//  · **B5**：只判「解析得出 op」而不判 `create` ⇒ at-turn 的「受伤」正文会被误认成覆盖 ⇒ B5 红。
//  · **B6**：不数「同一 trigger 下几行 `ca 100`」⇒ 两行卡被判整批覆盖 ⇒ 少造一张 ⇒ B6 红。
//  · **D2–D5**：`pa` 没接（DTO 漏字段 / `MapTriggerAbilityIndices` 没填 / `version` 闸写错）⇒ 全部落 `-1`
//    ⇒ 四档直方图同时红；D7–D10 再各钉一个原值（`Cult Propaganda 20 / Patriarch 10 / ASH85 0 / BL52 0`）。
//  · **E1a**：删掉 `DispatchHandAbilityRows` 里 `Ca == 165` 那行 ⇒ fired 0 ⇒ 红；
//    **E3a**：把 `Pa = -1` 当 `Both` 兜底 ⇒ 手牌被弃 ⇒ 红；**E4**：池内出现 `ca 165` 行 ⇒ 红（届时「休眠件」要重评）。
//  ⚠️ 区分 `CoveredByText` 与 `Unsupported` 靠**三条一起**：A3（枚举值）· A5b（**不出声**）· A6（**正控**：本派发口确实会动东西）。
//     ⛔ A3 单留不够 —— 只删 `Dispatch` 的 `continue` 时枚举值不变、手牌不变，它仍绿。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加（落在该行第一个 `//` **之前**）：
//     `Guard("B143R3_Assert", B143_R3_Assert.Run); _pass += B143_R3_Assert.Pass; _fail += B143_R3_Assert.Fail;`
//   （接线由调度台统一做；本写手白名单不含那个文件。）
//
// ⚠️ 本文件不跑 Unity / 不跑宿主（写手纪律）：只保证「编得过 + 判据自洽」。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B143_R3`：手牌上下文 `ca 100` → `HandRowKind.CoveredByText`（文本路已覆盖）。</summary>
public static class B143_R3_Assert
{
    const string P = "[B143R3] ";

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

    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B143R3Warlord0") };
        var d1 = new List<CardDef> { Hero("B143R3Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    /// <summary>合成卡：只给 `Desc` + 一行 `(ab0, lg0)` 的 `triggerAbilityIndices`（分类器只读这两样）。</summary>
    static CardDef RowCard(string id, string desc, int trigger, int ca)
    {
        var c = new CardDef(id, id, "tactic", desc, "common", "Test", 1, 0, 0, 0);
        c.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = trigger, Ca = ca },
        });
        return c;
    }

    /// <summary>**正控**夹具：`trigger 30 / ab0 / lg0 / ca 160 / ta = 20 self` 的手牌卡
    /// （与 `B141_W67_Assert.RowCard` 同形）。`abilityTargets` 必须挂 —— `TargetsAffectedOfRow` 读的是那一列。</summary>
    static CardDef Row160Card(string id)
    {
        var c = new CardDef(id, id, "tactic", "", "common", "Test", 1, 0, 0, 0,
            abilityContext: new[]
            {
                new CardDef.AbilityContextSpec { Name = "TurnStart", Context = CardDef.AbilityContextHand },
            },
            abilityContextsVersioned: true,
            abilityTriggers: new[] { 30 },
            abilityTriggersVersioned: true,
            abilityTargets: new[] { new CardDef.AbilityTargetSpec { Trigger = 30, TargetsAffected = new[] { 20 } } });
        c.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = 30, Ca = 160 },
        });
        return c;
    }

    /// <summary>**`ca 165 discardWholeHand`** 夹具：`trigger 30 / ab0 / lg0 / ca 165 / Pa = 传入值`。
    /// <para>`Pa` 现在由 v50 数据列填（`TriggerAbilityIndexSpec.Pa`）；执行器只读它决定弃哪一侧。</para></summary>
    static CardDef Row165Card(string id, int pa)
    {
        var c = new CardDef(id, id, "tactic", "", "common", "Test", 1, 0, 0, 0);
        c.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = 30, Ca = 165, Pa = pa },
        });
        return c;
    }

    /// <summary>这张卡某条 trigger 上 `Ab == 0 ∧ Lg == 0` 的那一行（读不到 ⇒ null）。</summary>
    static CardDef.TriggerAbilityIndexSpec RowOf(CardDef card, int trigger)
    {
        var rows = card != null ? card.TriggerAbilityRowsOf(trigger) : null;
        if (rows == null) return null;
        for (int i = 0; i < rows.Length; i++)
            if (rows[i] != null && rows[i].Ab == 0 && rows[i].Lg == 0) return rows[i];
        return null;
    }

    /// <summary>`Cult Propaganda`（池内唯一一条手牌 `ca 100`）—— 按 id 精确取，取不到再按名。</summary>
    static CardDef FindCultPropaganda(List<CardDef> pool)
    {
        if (pool == null) return null;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            if (!string.IsNullOrEmpty(c.Id) && c.Id.StartsWith("7c666a04")) return c;
        }
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c != null && c.Name == "Cult Propaganda") return c;
        }
        return null;
    }

    // ============================================================ 入口
    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            var pool = CardDatabase.Load();
            Ok(pool != null && pool.Count > 0, "前提：卡池读到了（`CardDatabase.Load`）");

            SectionA_RealCard(pool);
            SectionB_Negatives();
            SectionC_NoRegressionOnCa160();
            SectionD_PaColumn(pool);
            SectionE_Ca165(pool);
        }
        catch (System.Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== B143R3 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ============================================================
    //  A · 真卡：池内唯一那行手牌 `ca 100` ⇒ CoveredByText，且不派发
    // ============================================================
    static void SectionA_RealCard(List<CardDef> pool)
    {
        Debug.Log(P + "--- A · Cult Propaganda（池内唯一 `ctx 10 Hand` 的 `ca 100`）---");
        var card = FindCultPropaganda(pool);
        Ok(card != null, "A1 **前提**：池里能取到 Cult Propaganda（`7c666a04…`）");
        if (card == null) return;

        var row = RowOf(card, 40);                                   // 40 = TurnEnd
        Ok(row != null && row.Ca == 100,
           "A2 **前提**：它有 `trigger 40 / ab0 / lg0 / ca 100` 那一行（`cards_engine.json` v49）");
        if (row == null || row.Ca != 100) return;

        string why;
        var kind = RuleCore.HandAbilityRowKindOf(card, row, out why);
        Ok(kind == RuleCore.HandRowKind.CoveredByText,
           "A3 ★ `ca 100` ∧ 卡面 at-turn 正文覆盖本行 ⇒ **CoveredByText**"
         + "（判据：`EffectText.SplitAtTurn(desc)` 相位 = turn_end(40) ∧ `Parse(body)` 产出 create op；"
         + "结算者 = `ResolveAtTurn → AtTurnHandJobDeferred → DoCreate`）"
         + "｜🧨 删掉 `ca 100` 那一支 ⇒ 回落 Unsupported ⇒ 红｜why=" + (why ?? "?"));

        Ok(!RuleCore.HandAbilityTriggerHasRowsForTest(card, "TurnEnd"),
           "A4 `HandAbilityTriggerHasRows(card, \"TurnEnd\")` = **false**（CoveredByText **不入队**）"
         + "｜🧨 把 CoveredByText 并进 Owned（或删掉 HasRows 那处 continue）⇒ 变 true ⇒ 红"
         + "（⚠️ 「不出声」不由本条证明，单钉在 A5d）");

        var ctx = NewCtx();
        RuleCore.PumpActionQueues(ctx);                              // 清掉开局留下的队列动作
        int me = 0;
        var inst = ctx.NewInstance(card);
        ctx.Players[me].Hand.Add(inst);
        int handBefore = ctx.Players[me].Hand.Count;
        int oppBefore = ctx.Players[1 - me].Hand.Count;
        RuleCore.ResetHandRowKindNotesForTest();                     // ★ A5d 的基线（只清计数，不动去重闩）
        int fired = RuleCore.DispatchHandAbilityRowsForTest(ctx, me, inst, "TurnEnd");
        Eq(fired, 0, "A5 `DispatchHandAbilityRows` 对它返回 **0**（本层不派发 ⇒ 不会与文本路双造）"
         + "｜⚠️ **本条单靠自己恒真**（Unsupported 档也返回 0）⇒ 判别力在 A5d + A6");
        Eq(ctx.Players[me].Hand.Count, handBefore,
           "A5b 派发后**自己手牌张数不变**（文本路才是结算者；结构路一张都不该动）");
        Eq(ctx.Players[1 - me].Hand.Count, oppBefore, "A5c 对手手牌张数也不变");
        Eq(RuleCore.HandRowKindNotesForTest, 0,
           "A5d ★★ `CoveredByText` **一声不吭**（`NoteHandAbilityRowUnsupported` 计数 = 0）"
         + "｜🧨 删掉 `DispatchHandAbilityRows` 里 `CoveredByText` 那个 `continue` ⇒ 静默落到出声口 ⇒ 计数 1 ⇒ 红"
         + "（这是唯一能区分 `CoveredByText` 与 `Unsupported` 的**行为**断言）");

        // ★ 正控：证明本派发口**确实会动东西**（否则 A5/A5b/A5c 的「0 / 不变」恒真 = 自证）
        var ctl = Row160Card("B143R3A_ctl160");
        var ctx2 = NewCtx();
        RuleCore.PumpActionQueues(ctx2);
        var inst2 = ctx2.NewInstance(ctl);
        ctx2.Players[me].Hand.Add(inst2);
        int ctlHand0 = ctx2.Players[me].Hand.Count;
        int ctlFired = RuleCore.DispatchHandAbilityRowsForTest(ctx2, me, inst2, "TurnStart");
        Eq(ctlFired, 1, "A6 ★ 正控：`ca 160 ∧ ta = 20 self` 的手牌卡 ⇒ fired == 1（本派发口**确实会结算**）");
        Eq(ctx2.Players[me].Hand.Count, ctlHand0 - 1, "A6b …且手牌 **−1**（它自己那张真的被弃掉）");
    }

    // ============================================================
    //  B · 负例：只有「卡面 at-turn 正文真的覆盖本行」才准判 CoveredByText
    // ============================================================
    static void SectionB_Negatives()
    {
        Debug.Log(P + "--- B · 负例（防「无条件放过」）---");
        string why;

        var noDesc = RowCard("B143R3B_noDesc", "", 40, 100);
        Ok(RuleCore.HandAbilityRowKindOf(noDesc, RowOf(noDesc, 40), out why) == RuleCore.HandRowKind.Unsupported,
           "B1 `ca 100` 但**卡面没有正文** ⇒ **Unsupported**（真缺口，照旧出声）"
         + "｜🧨 无条件返回 CoveredByText ⇒ 红");

        var wrongTrigger = RowCard("B143R3B_t30",
            "At the end of your turn, create a Neophyte Initiate in your opponent's hand", 30, 100);
        Ok(RuleCore.HandAbilityRowKindOf(wrongTrigger, RowOf(wrongTrigger, 30), out why) == RuleCore.HandRowKind.Unsupported,
           "B2 同一段 turn_end 正文，但**本行 trigger = 30**（相位对不上）⇒ **Unsupported**"
         + "｜🧨 忽略相位/trigger 匹配 ⇒ 红");

        var notAtTurn = RowCard("B143R3B_dmg", "Deal 1 damage to an enemy troop", 40, 100);
        Ok(RuleCore.HandAbilityRowKindOf(notAtTurn, RowOf(notAtTurn, 40), out why) == RuleCore.HandRowKind.Unsupported,
           "B3 `ca 100` + **非 at-turn 正文**（`Deal 1 damage…`）⇒ **Unsupported**"
         + "｜🧨 把「有正文」当覆盖 ⇒ 红");

        var covered = RowCard("B143R3B_ok",
            "At the end of your turn, create a Neophyte Initiate in your opponent's hand", 40, 100);
        Ok(RuleCore.HandAbilityRowKindOf(covered, RowOf(covered, 40), out why) == RuleCore.HandRowKind.CoveredByText,
           "B4 正例（与 A3 同判据的合成卡）⇒ **CoveredByText**（证明 A3 不是靠 `Cult Propaganda` 的名字）");

        // B5：at-turn、也解析得出 op，但**不是 create** —— 本行是 `ca 100 createCardInHand`，
        //     文本路没在造牌 ⇒ 不算覆盖（否则「敌军全体受 1 伤」这种 at-turn 正文会被误认）。
        var atTurnDamage = RowCard("B143R3B_atDmg",
            "At the end of your turn, your troops take 1 damage", 40, 100);
        Ok(RuleCore.HandAbilityRowKindOf(atTurnDamage, RowOf(atTurnDamage, 40), out why) == RuleCore.HandRowKind.Unsupported,
           "B5 `ca 100` + **at-turn 但不是 create**（`your troops take 1 damage`）⇒ **Unsupported**"
         + "｜🧨 只判「解析得出 op」而不判 `create` ⇒ 误判 CoveredByText ⇒ 红");

        // B6：同一 trigger 下**两行** `ca 100` —— 文本路整段只结算一次 ⇒ 不能让两行都让路。
        var multi = new CardDef("B143R3B_multi", "B143R3B_multi", "tactic",
            "At the end of your turn, create a Neophyte Initiate in your opponent's hand",
            "common", "Test", 1, 0, 0, 0);
        multi.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = 40, Ca = 100 },
            new CardDef.TriggerAbilityIndexSpec { Ab = 1, Lg = 0, Trigger = 40, Ca = 100 },
        });
        Ok(RuleCore.HandAbilityRowKindOf(multi, RowOf(multi, 40), out why) == RuleCore.HandRowKind.Unsupported,
           "B6 同一 trigger 下有 **2 行 `ca 100`** ⇒ **Unsupported**（文本路只结算一次，不能让整批都让路）"
         + "｜🧨 不数行数 ⇒ 误判 CoveredByText ⇒ 少造一张 ⇒ 红");
    }

    // ============================================================
    //  C · 同族不误伤：`ca 160 ∧ ta 20` 仍然是 Owned
    // ============================================================
    static void SectionC_NoRegressionOnCa160()
    {
        Debug.Log(P + "--- C · `ca 160 discardFromHand` 的既有分类不变 ---");
        var c = new CardDef("B143R3C_place", "B143R3C_place", "tactic", "", "common", "Test", 1, 0, 0, 0,
            abilityContext: new[]
            {
                new CardDef.AbilityContextSpec { Name = "TurnStart", Context = CardDef.AbilityContextHand },
            },
            abilityContextsVersioned: true,
            abilityTriggers: new[] { 30 },
            abilityTriggersVersioned: true,
            abilityTargets: new[] { new CardDef.AbilityTargetSpec { Trigger = 30, TargetsAffected = new[] { 20 } } });
        c.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = 30, Ca = 160 },
        });
        string why;
        Ok(RuleCore.HandAbilityRowKindOf(c, RowOf(c, 30), out why) == RuleCore.HandRowKind.Owned,
           "C1 `ca 160` + `ta = 20 self` 仍是 **Owned**（本笔不动它）"
         + "｜🧨 改动误伤 160 那支 ⇒ 红");
    }

    // ============================================================
    //  D · 数据列 `pa`（`B141-HAND-CA165` 的前置）
    //     判据 → `报告/B143_R3_CA165_施工单.md` §1：`pa` = 原版 `AbilityLogic.playersAffected(+0x20)`，
    //     逐 `(ab, lg)` 行；0 Both / 10 Self / 20 Enemy / -1 未知。
    // ============================================================
    static void SectionD_PaColumn(List<CardDef> pool)
    {
        Debug.Log(P + "--- D · `triggerAbilityIndices[].pa`（v50 新列）---");
        if (pool == null) { Ok(false, "D0 **前提**：卡池为空 ⇒ D 组判不了"); return; }

        int rows = 0, n0 = 0, n10 = 0, n20 = 0, nx = 0, nBad = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            var rs = c != null ? c.TriggerAbilityIndices : null;
            if (rs == null) continue;
            for (int j = 0; j < rs.Length; j++)
            {
                var r = rs[j];
                if (r == null) continue;
                rows++;
                switch (r.Pa) { case 0: n0++; break; case 10: n10++; break; case 20: n20++; break; case -1: nx++; break; default: nBad++; break; }
            }
        }
        Eq(rows, 1622, "D1 **前提**：池内 `triggerAbilityIndices` 总行数 = 1,622（v49/v50 同；只加列不加行）");
        Eq(n0, 1254, "D2 `pa == 0 (Both)` 的行数（🧨 缺列/没解析 `Pa` ⇒ 全部落 -1 ⇒ D2/D3/D4 同时红）");
        Eq(n10, 331, "D3 `pa == 10 (Self)` 的行数");
        Eq(n20, 36, "D4 `pa == 20 (Enemy)` 的行数");
        Eq(nx, 1, "D5 `pa == -1 (未知)` 的行数（唯一那条 = `lg == -1` 的空 logic 行）");
        Eq(nBad, 0, "D6 没有非法值（只允许 0/10/20/-1）");

        // 抽样：值必须与 `cards_raw.json` 的 `abilityLogic[].playersAffected` 逐个对得上
        Eq(PaOf(pool, "7c666a04", 40, 100), 20, "D7 抽样：`Cult Propaganda` 的 TurnEnd(40) `ca 100` 行 ⇒ pa = 20 (Enemy)");
        Eq(PaOf(pool, "GSC37", 240, 40), 10, "D8 抽样：`Patriarch` 的 240 行 ⇒ pa = 10 (Self)");
        Eq(PaOf(pool, "ASH85", 240, 40), 0, "D9 抽样：`Fate Inescapable` 的 240 行 ⇒ pa = 0 (Both)");
        Eq(PaOf(pool, "BL52", 240, 30), 0, "D10 抽样：`Unholy Smite` 的 240 行 ⇒ pa = 0 (Both)");
    }

    /// <summary>按 `id` 前缀 + `(trigger, ca)` 取那一行的 `Pa`（读不到 ⇒ -999）。</summary>
    static int PaOf(List<CardDef> pool, string idPrefix, int trigger, int ca)
    {
        if (pool == null) return -999;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null || string.IsNullOrEmpty(c.Id) || !c.Id.StartsWith(idPrefix)) continue;
            var rs = c.TriggerAbilityIndices;
            if (rs == null) return -999;
            for (int j = 0; j < rs.Length; j++)
                if (rs[j] != null && rs[j].Trigger == trigger && rs[j].Ca == ca) return rs[j].Pa;
            return -999;
        }
        return -999;
    }

    // ============================================================
    //  E · `ca 165 discardWholeHand` 的手牌执行器（**休眠件**）
    //     判据 → `AbilityLogic__PlayAbility.c:293-345`（`playersAffected` 分侧 + 逐张 `AddDiscardFromHand`）。
    //     ⚠️ **全 1,351 张 SO 里 `ca 165` = 0 行**（递归扫含 `full_raw`）⇒ 本组只能靠**合成行**验；
    //        真实对局里**不会有任何卡**走到这个执行器 —— ⛔ 别把它当活件。
    // ============================================================
    static void SectionE_Ca165(List<CardDef> pool)
    {
        Debug.Log(P + "--- E · `ca 165 discardWholeHand`（休眠件：全池 0 行）---");
        const int me = 0;

        // E1 Pa=10 (Self) ⇒ 弃**持有者自己**那侧整手牌
        var c1 = Row165Card("B143R3E_self", 10);
        var ctx1 = NewCtx();
        RuleCore.PumpActionQueues(ctx1);
        var inst1 = ctx1.NewInstance(c1);
        ctx1.Players[me].Hand.Add(inst1);
        ctx1.Players[me].Hand.Add(ctx1.NewInstance(RowCard("B143R3E_filler", "", 30, 3)));
        Ok(ctx1.Players[me].Hand.Count >= 2, "E1 **前提**：派发前手里至少 2 张（能力卡 + 填充卡）");
        int fired1 = RuleCore.DispatchHandAbilityRowsForTest(ctx1, me, inst1, "TurnStart");
        Eq(fired1, 1, "E1a ★★ 合成 `ca 165` 行（Pa=10 Self）⇒ 派发分支**命中**（fired == 1）"
         + "｜🧨 删掉 `DispatchHandAbilityRows` 里 `Ca == 165` 那行 ⇒ 0 ⇒ 红");
        Eq(ctx1.Players[me].Hand.Count, 0, "E1b …且**己方整手牌被弃光**（原版 `GetCardsInHandList` 逐张 `AddDiscardFromHand`）");

        // E2 Pa=20 (Enemy) ⇒ 只弃**对面**
        var c2 = Row165Card("B143R3E_enemy", 20);
        var ctx2 = NewCtx();
        RuleCore.PumpActionQueues(ctx2);
        var inst2 = ctx2.NewInstance(c2);
        ctx2.Players[me].Hand.Add(inst2);
        ctx2.Players[1 - me].Hand.Add(ctx2.NewInstance(RowCard("B143R3E_foe", "", 30, 3)));
        int myBefore = ctx2.Players[me].Hand.Count;
        Eq(RuleCore.DispatchHandAbilityRowsForTest(ctx2, me, inst2, "TurnStart"), 1,
           "E2a 合成 `ca 165`（Pa=20 Enemy）⇒ fired == 1");
        Eq(ctx2.Players[me].Hand.Count, myBefore, "E2b …**己方手牌不变**（`IsPlayerAffected(true,\"Enemy\")` = false）");
        Eq(ctx2.Players[1 - me].Hand.Count, 0, "E2c …**对面手牌被弃光**");

        // E3 Pa=-1 ⇒ 判不出侧 ⇒ 不结算（⛔ 不猜成 Both）
        var c3 = Row165Card("B143R3E_unknown", -1);
        var ctx3 = NewCtx();
        RuleCore.PumpActionQueues(ctx3);
        var inst3 = ctx3.NewInstance(c3);
        ctx3.Players[me].Hand.Add(inst3);
        int h3 = ctx3.Players[me].Hand.Count;
        Eq(RuleCore.DispatchHandAbilityRowsForTest(ctx3, me, inst3, "TurnStart"), 0,
           "E3a `Pa = -1` ⇒ **不结算**（⛔ 不猜成 Both）");
        Eq(ctx3.Players[me].Hand.Count, h3, "E3b …手牌一张不动");

        // E4 数据事实：池内（1,142 张）`triggerAbilityIndices` 里 `ca 165` = 0 行
        int n165 = 0;
        if (pool != null)
            for (int i = 0; i < pool.Count; i++)
            {
                var rs = pool[i] != null ? pool[i].TriggerAbilityIndices : null;
                if (rs == null) continue;
                for (int j = 0; j < rs.Length; j++) if (rs[j] != null && rs[j].Ca == 165) n165++;
            }
        Eq(n165, 0, "E4 **不可达**：池内 `triggerAbilityIndices` 里 `chosenAbility == 165` = **0 行**"
         + "（全 1,351 张 SO 递归扫也是 0 ⇒ 休眠件；⛔ 别当成活件）");
    }
}
