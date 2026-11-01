// B114_W9_Assert.cs — 执行代理 `B114_W9`（第一百一十四会话 · 第 2 波）的断言
//
// ============================ 它盯的是什么 ============================
//   `RuleEngine/Core/EffectResolver.cs` 本笔新加的那一个**派发口**：
//     · `FireTriggerByNumberWithSink` 里 `ops == null || ops.Count == 0` 那一格 ⇒ 转
//       `RuleCore.FireTriggerRowsByChosenAbility(...)`（同一物理行，零行数变化）；
//     · 判法唯一出口 = `RuleCore.TriggerRowKindOf(int ca)`（四档：`Unknown / FollowUp / OwnLane / BodyOp`）。
//   数据面 = `B114_WB` 落好的 `cards_engine.json` v47 `triggerAbilityIndices[].ca`
//   （= 原版 `AbilityLogic.chosenAbility`；`CardDef.TriggerAbilityIndexSpec.Ca`）。
//
// ============================ 原版口径（判据，现读 `d:/2/tools/decomp_full/`）============================
//   · 🔴 `chosenAbility` **不是**触发侧的派发键 —— 选 ability 只看 `CardAbility.trigger(+0x10)`：
//     `CardAbility__CanTriggerAbility.c:71`（全 143 行**一次都没读 `chosenAbility`**）。
//   · 它真正的作用 = `AbilityLogic.PlayAbility` 内部的巨型 switch：`AbilityLogic__PlayAbility.c:286`
//     `iVar8 = *(int *)(param_1 + 0x10);`（`+0x10` = `AbilityLogic.chosenAbility`）。
//   · ⇒ 所以我们只在**正文 op 为空**时才按它兜底（正文 op 非空时再跑一遍 = 打两次，
//     `报告/B113X7_能力380生产口.md` §四 / `报告/B114_WB_B211逐行ca数据面.md` §四）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2：把 `TriggerRowKindOf` 写成 `if (ca <= 0) return Unknown;`（把 `0 = drawCard` 折成未知）
//         ⇒ A2 红（A1 仍绿）—— 两条**结构上不可能同时满足**。
//   · C2/C3：删掉 `TriggerRowKindOf` 里的 `if (ca == 320) return FollowUp;` ⇒ C2 红（fired 0）、C3 红（护甲仍 0）。
//   · D2：把 `if (ca < 0) return Unknown;` 删掉（让 `-1` 落到 `BodyOp`）⇒ D2 的 fired 仍 0 但 E 组差不是 1；
//         E 组用**同构夹具只改 `Ca`**，把两条路一起改回去 ⇒ 差不再是「有/无」⇒ 红。
//
// ============================ 怎么接（宿主）============================
//   `RuleEngineTest.Run()` 里那行聚合调用追加：
//     `Guard("B114W9_Assert", B114_W9_Assert.Run); _pass += B114_W9_Assert.Pass; _fail += B114_W9_Assert.Fail;`
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B114_W9 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B114_W9_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B114_W9：`B-211` 派发口 —— `chosenAbility`（`ca`）的第一个消费者");

        // ---- A 组：行种别分类（纯函数，不需要战场）-----------------------------------------
        Section("A 行种别分类 `RuleCore.TriggerRowKindOf`（⛔ 全仓只有这一份判法）");
        Check(RuleCore.TriggerRowKindOf(-1), RuleCore.TriggerRowKind.Unknown,
              "A1 `ca == -1`（未知）⇒ Unknown（= 改前行为）");
        Check(RuleCore.TriggerRowKindOf(0), RuleCore.TriggerRowKind.BodyOp,
              "A2 ★★ `ca == 0` 是**合法真值**（`drawCard`，现池 81 行）⇒ **BodyOp**，⛔ 不是 Unknown"
            + "｜🧨 把判法写成 `ca <= 0 ⇒ Unknown` ⇒ 本行红（A1 仍绿）");
        Check(RuleCore.TriggerRowKindOf(320), RuleCore.TriggerRowKind.FollowUp,
              "A3 ★★ `320 followAfterResolution` ⇒ FollowUp（走已有 `RunFollowAfterResolutionRow`）"
            + "｜🧨 删掉这一支 ⇒ 本行与 C 组全红");
        Check(RuleCore.TriggerRowKindOf(340), RuleCore.TriggerRowKind.OwnLane,
              "A4 `340 removeThisEnchantment` ⇒ OwnLane（效果已由手写等价物承担，⛔ 别在本趟再跑一遍）");
        Check(RuleCore.TriggerRowKindOf(380), RuleCore.TriggerRowKind.OwnLane,
              "A5 `380 setReferenceCard` ⇒ OwnLane（主动能力那一趟的标记类）");
        Check(RuleCore.TriggerRowKindOf(480), RuleCore.TriggerRowKind.OwnLane,
              "A6 `480 highlightBloodThirst` ⇒ OwnLane（表现类）");
        Check(RuleCore.TriggerRowKindOf(30), RuleCore.TriggerRowKind.BodyOp,
              "A7 `30 buffOthers`（现池 517 行，最大一块）⇒ BodyOp（它的效果由正文 op 承担）");

        // ---- B 组：真实卡数据 —— 前提（`Ca` 真的从 `cards_engine.json` 读得回）+ 结论（分对了支）----
        Section("B 真实卡数据：`Ca` 读得回，且按 `ca` 分到了对的支");
        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B114_W9] `CardDatabase.Load()` 抛了：" + e.Message); }
        CheckTrue(pool != null && pool.Count > 500, "B0 前提：卡池读得到且规模正常（≥ 500 张）");
        if (pool == null || pool.Count <= 500) { Summary(); return; }

        CardDef am54 = Find(pool, "AM54");
        CheckTrue(am54 != null, "B1 前提：池里有 `AM54 At All Costs`");
        if (am54 != null)
        {
            var r410 = am54.TriggerAbilityRowsOf(410);
            CheckTrue(r410 != null && r410.Length == 1, "B2 前提：它的 `trigger 410` 恰好 1 行（`abilityTriggers` 里 410 只有一条 ability）");
            if (r410 != null && r410.Length == 1)
            {
                Check(r410[0].Ca, 30,
                      "B3 ★★ **前提（`Ca` 读得回）**：`AM54` 的 `(ab1,lg0,trigger 410)` 行 `Ca == 30`"
                    + "（`buffOthers`）—— 这是 `cards_engine.json` v47 的真值，⛔ 不是默认值 `-1`");
                Check((int)RuleCore.TriggerRowKindOf(r410[0].Ca), (int)RuleCore.TriggerRowKind.BodyOp,
                      "B4 ★★ **结论（按 `ca` 走对了支）**：这一行 ⇒ BodyOp（正文 op 承担），⛔ 不是 FollowUp/OwnLane");
            }
            var r0 = am54.TriggerAbilityRowsOf(0);
            bool found380 = false;
            if (r0 != null) for (int i = 0; i < r0.Length; i++) if (r0[i] != null && r0[i].Ca == 380) found380 = true;
            CheckTrue(found380,
                      "B5 ★★ `AM54` 的 `trigger 0` 三行里有一行 `Ca == 380`（`setReferenceCard`）"
                    + " —— 反证旧账「380 在 trigger 410」不成立（`报告/B113X7` §二）");
        }

        CardDef gof97 = Find(pool, "GOF97");
        CheckTrue(gof97 != null, "B6 前提：池里有 `GOF97 Gargantuan Squiggoth`（现池**唯一** `ca == -1` 的一张）");
        if (gof97 != null)
        {
            var g0 = gof97.TriggerAbilityRowsOf(0);
            bool foundUnknown = false;
            if (g0 != null) for (int i = 0; i < g0.Length; i++) if (g0[i] != null && g0[i].Ca == -1) foundUnknown = true;
            CheckTrue(foundUnknown,
                      "B7 ★★ `GOF97` 有一行 `Ca == -1`（`lg == -1`：那条 ability 没有 `abilityLogic` 实例 ⇒ 真值本身）"
                    + "｜🧨 生成器/映射把 `-1` 折成 `0` ⇒ 本行红");
            Check((int)RuleCore.TriggerRowKindOf(-1), (int)RuleCore.TriggerRowKind.Unknown,
                  "B8 ★★ 那一行 ⇒ Unknown ⇒ **改前行为**（⛔ 不猜、⛔ 不结算）");
        }

        // ---- C 组：端到端 —— `ca == 320` 那一支真的走 `RunFollowAfterResolutionRow` --------------------
        Section("C 端到端：`ca 320` ⇒ 派发到 `RunFollowAfterResolutionRow` ⇒ `410` 正文真的结算");
        {
            var ops = EffectText.Parse("Gain Armour 2", out _, out _);
            CheckTrue(ops != null && ops.Count > 0, "C0 前提：`Gain Armour 2` 解得出 ≥1 条 op");
            var card = Fixture("B114W9Ca320", 320, ops);
            Check(card.TriggerAbilityRowsOf(0)[0].Ca, 320, "C1 前提：夹具那一行的 `Ca` 读得回 320");
            var ctx = MakeBattle(card, out var carrier);
            CheckTrue(carrier != null, "C1b 前提：载体上场了（0 号玩家格 3）");
            if (carrier != null)
            {
                Check(carrier.Armor, 0, "C1c 前提：打之前护甲 0");
                int fired = RuleCore.FireTriggerRowsByChosenAbilityForTest(ctx, carrier, 0, 0, 3, null, null);
                CheckTrue(fired > 0,
                          "C2 ★★★ **`ca 320` 那一行被派发了**（`RunFollowAfterResolutionRow` ⇒ `trigger 410` 正文已入队）"
                        + "｜🧨 删掉 `TriggerRowKindOf` 的 `ca == 320` 支 ⇒ 实得 0 ⇒ 红");
                RuleCore.PumpActionQueues(ctx);
                Check(carrier.Armor, 2,
                      "C3 ★★★ 泵完 ⇒ 护甲 0 → 2（= `410` 那条正文「Gain Armour 2」真的结算了）"
                    + "｜🧨 同上 ⇒ 仍是 0 ⇒ 红 —— 这一条是「派发口接没接上」的判别式");
            }
        }

        // ---- D/E 组：灭自证 —— 同构夹具只改 `Ca` ------------------------------------------------
        Section("D/E 灭自证：同构夹具只改 `Ca`（320 / 30 / -1），差值必须来自 `ca` 本身");
        {
            var ops = EffectText.Parse("Gain Armour 2", out _, out _);
            int fired320 = RunFixtureOnce(320, ops);
            int fired30 = RunFixtureOnce(30, ops);
            int firedNeg = RunFixtureOnce(-1, ops);
            CheckTrue(fired320 > 0, "D1 `Ca == 320` ⇒ 派发（fired = " + fired320 + "）");
            Check(fired30, 0,
                  "D2 ★★ `Ca == 30`（`buffOthers`，BodyOp）⇒ **本趟不派**（正文 op 为空 ⇒ 出声、不结算），"
                + "⛔ 不会与正文重复 —— 这是「按 ca 走对了支」的负例");
            Check(firedNeg, 0,
                  "D3 ★★ `Ca == -1`（未知）⇒ **改前行为**（三支都不进）");
            Check(fired320 - firedNeg, 1,
                  "D4 ★★★ 灭自证：`fired(320) − fired(-1)` 恰为 **1**（" + fired320 + " − " + firedNeg + "）"
                + "｜🧨 把 `TriggerRowKindOf` 改回「一律 Unknown」⇒ 两个都 0、差 0 ⇒ 红；"
                + "改回「一律 FollowUp」⇒ 两个都 1、差 0 ⇒ 红");
        }

        Summary();
    }

    // ==================================================================
    //  夹具 / 读数
    // ==================================================================

    /// <summary>造一张「一行 `trigger 0` 的 ability，`ca` 指定，正文只有 `trigger 410`」的卡 —— 正是
    /// 原版 `followAfterResolution` 那个形状（这一行自己没有正文、后续结算在 410）。</summary>
    static CardDef Fixture(string id, int ca, List<EffectOp> opsFor410)
    {
        var c = new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 5, 0, null, subtype: "Infantry");
        c.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = 0, TriggerValue = 0, Ca = ca }
        });
        if (opsFor410 != null && opsFor410.Count > 0)
            c.AttachTriggerOpsByNumber(new Dictionary<int, List<EffectOp>> { { 410, opsFor410 } });
        return c;
    }

    /// <summary>同一形状跑一次，返回 `FireTriggerRowsByChosenAbility` 的入队条数（不泵队）。</summary>
    static int RunFixtureOnce(int ca, List<EffectOp> ops)
    {
        var card = Fixture("B114W9Ca" + (ca < 0 ? "Neg" : ca.ToString()), ca, ops);
        var ctx = MakeBattle(card, out var carrier);
        if (carrier == null) return -1;
        return RuleCore.FireTriggerRowsByChosenAbilityForTest(ctx, carrier, 0, 0, 3, null, null);
    }

    static CardDef Find(List<CardDef> pool, string id)
    {
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && pool[i].Id == id) return pool[i];
        return null;
    }

    /// <summary>一场「督军 + 一张载体 + 一张对手单位」的干净对局；返回时 <paramref name="carrier"/> = 已摆到 0 号玩家格 3 的载体。</summary>
    static BattleContext MakeBattle(CardDef carrierCard, out UnitState carrier)
    {
        var heroA = new CardDef("B114W9HeroA", "B114W9HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B114W9HeroB", "B114W9HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var foeCard = new CardDef("B114W9Foe", "B114W9Foe", "unit", "", null, "Test", 1, 1, 5, 0, null);
        var ctx = RuleCore.NewBattle(new List<CardDef> { heroA, carrierCard },
                                     new List<CardDef> { heroB, foeCard },
                                     seed: 0, shuffle: false, cardPool: new List<CardDef>());
        carrier = PlaceUnit(ctx, 0, 3, carrierCard);
        PlaceUnit(ctx, 1, 3, foeCard);
        return ctx;
    }

    static UnitState PlaceUnit(BattleContext ctx, int p, int slot, CardDef card)
    {
        if (ctx == null || card == null) return null;
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    // ==================================================================
    //  最小断言框架（本文件独立类 ⇒ 不借用 `RuleEngineTest` 的 private 助手）
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
        if (_fail == 0) Debug.Log($"B114_W9 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B114_W9 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口（追加到类尾、零行漂）。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
