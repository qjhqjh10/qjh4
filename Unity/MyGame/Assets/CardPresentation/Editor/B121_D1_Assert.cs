// B121_D1_Assert.cs — 第一百二十一会话 · 执行写手 `B121-D1`（附魔模板 `ca` 执行器补齐）
//
// ============================ 它盯的是什么 ============================
//   活状态表条目 `B121-01` / `B121-02`：**附魔模板的 `ca`（`AbilityLogic.chosenAbility`）执行器**
//   原来只接 `30 / 40 / 50 / 340`（`Core/EnchantmentExecutor_B115.cs`），模板 138 行 logic 里 **75 行无执行器**。
//   本波补上 15 档的分派骨架：其中 **3 档能真执行**（140 / 320 / 410），其余 12 档给出**精确的数据缺口出声**。
//
// ============================ 原版口径（判据，现读 `d:/2/tools/decomp_full/`）============================
//   · `AbilityLogic__PlayAbility.c:262` = `AbilityLogic.GetTargets(targetCriteria(+0x28), …)` → 目标表（每支共用）；
//   · `:285-286` `iVar21 = 当前 trigger` / `iVar8 = chosenAbility`；`:288` 起巨型 if/switch；每支末尾 `:2743-2745`；
//   · `ca 140 recallToOwnerHand` = `:541-566`（`case 0x8c`）逐目标 `AddRecallToHand`；
//   · `ca 320 followAfterResolution` = `:2244`（`if (iVar8 != 0x140) return;`）→ `:2472-2478` `AddFollowAfterResolution`；
//   · `ca 410 triggerCodex` = `:1685-1699`（`case 0x19a`）逐目标 `AddTriggerCodex`。
//   · 目标集各档 → `AbilityLogic__GetTargets.c`：`ta 0` = `:884-891`（双方全场）· `ta 20` = `:184-194` ·
//     `ta 30/220` = `:192-207` · `ta 120/130` = `:721-750` · `ta 140/150` = `:849-883`。
//
// ============================ 我们怎么接的（⛔ 别推翻）============================
//   · 三个执行体**全部复用仓里已有的唯一实现**：`DoReturn`（回手）· `RunFollowAfterResolutionRow`（后续结算）·
//     `ActionPump.EnqueueGrantedTrigger`（codex 触发登记）。⛔ 别在成器里再写第二份「挪棋盘 / 报名触发」。
//   · 其余 12 档走 `NoteEnchantRowDataGap`（与「本版没有这个执行器」的 `NoteEnchantRowUnsupported` **区分开**），
//     逐档写清缺的是哪一列（`playersAffected` / `damageCriteria` / `healCriteria` / `manaBuff` /
//     `summonCriteria` / `targetCriteria` 其余格 / 执行体）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · B1：删掉 `RunEnchantmentLogicRows` 里 `ca 320` 那一跳分派 ⇒ 护甲停在 0 ⇒ 红；
//   · C1：删掉 `ca 140` 那一跳 ⇒ 那张单位还在棋盘上、手牌不涨 ⇒ 红；
//   · D1：删掉 `ca 410` 那一跳（或把 `ta=20` 也判成不可解）⇒ 件数 1 → 0 ⇒ 红；
//   · E1 **反证**：`ca 10`（载荷缺）必须**出声且不改状态** —— 把 `NoteEnchantRowDataGap` 换成静默
//     `return 0` ⇒ 计数不涨 ⇒ E1 红（红线：不许静默失败）；
//   · F1 **反证**：本版不认识的 `ca 999` 仍走 `NoteEnchantRowUnsupported`（不 crash、不结算）。
//   · C2 是结构上的灭自证：**点定的那张**回手、旁边那张不动 —— 「取第一个己方单位」的近似同时满足不了两者。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加：
//     `Guard("B121D1_Assert", B121_D1_Assert.Run); _pass += B121_D1_Assert.Pass; _fail += B121_D1_Assert.Fail;`
//   （⚠️ 追加的文本必须落在该行第一个 `//` **之前**。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B121-D1 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B121_D1_Assert
{
    const int Trig = 640;                 // 模板行挂一个存在的 trigger（本组只测 `ca` 分派，不测派发闸）
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B121-D1：附魔模板 `ca` 执行器（140 / 320 / 410 真执行 · 其余 12 档精确出声）");

        // ---- A 组：前提 ----------------------------------------------------------------
        Section("A 前提：夹具（模板 `TriggerAbilityIndexSpec` / 附魔挂载）");
        var tpl320 = Template("B121D1Tpl320", Trig, 320, 20);
        var tpl140 = Template("B121D1Tpl140", Trig, 140, 30);
        var tpl410 = Template("B121D1Tpl410", Trig, 410, 20);
        var tpl410ta10 = Template("B121D1Tpl410ta10", Trig, 410, 10);
        var tpl10 = Template("B121D1Tpl10", Trig, 10, 10);

        CheckTrue(tpl320.TriggerAbilityRowsOf(Trig) != null && tpl320.TriggerAbilityRowsOf(Trig).Length == 1,
                  "A0 前提：模板的 `triggerAbilityIndices` 挂得上（1 行）");
        CheckTrue(tpl140.TriggerAbilityRowsOf(Trig)[0].Ca == 140 && tpl140.TriggerAbilityRowsOf(Trig)[0].Ta == 30,
                  "A1 前提：`ca` / `ta` 两格真的落进运行时 spec（`Ca == 140` ∧ `Ta == 30`）");

        // ---- B 组：ca 320 followAfterResolution（真执行）--------------------------------
        Section("B `ca 320 followAfterResolution`：载体自己那张卡的 `trigger 410` 真被跑起来");
        {
            var carrierCard = UnitWithTriggerOps("B121D1Carrier320", 410, "Gain Armour 2");
            var ctx = MakeBattle(carrierCard, out var carrier);
            CheckTrue(carrier != null, "B0 前提：载体上场了（0 号玩家格 3）");
            if (carrier != null)
            {
                var e = Attach(ctx, carrier, tpl320);
                CheckTrue(e != null, "B0b 前提：附魔挂上了");
                Check(carrier.Armor, 0, "B0c 前提：跑之前护甲 0");
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, null);
                RuleCore.PumpActionQueues(ctx);
                Check(n, 1, "B1 ★★★ **结论**：`ca 320` 执行器返回 1（登记了 1 条后续结算）"
                          + "｜🧨 删掉 `RunEnchantmentLogicRows` 里 `ca 320` 那一跳 ⇒ 返回 0 ⇒ 红");
                Check(carrier.Armor, 2, "B2 ★★★ **结论**：`trigger 410` 的正文 `Gain Armour 2` 真跑了（护甲 = 2）"
                                       + "｜判据 `AbilityLogic__PlayAbility.c:2472-2478` → `CardScript__FollowUpResolutionPlayed.c:5-9`");
            }
        }

        // ---- C 组：ca 140 recallToOwnerHand（真执行）------------------------------------
        Section("C `ca 140 recallToOwnerHand`：**点定的那一张**回其所有者手牌，旁边那张不动");
        {
            var carrierCard = PlainUnit("B121D1Carrier140");
            var buddyCard = PlainUnit("B121D1Buddy140");
            var ctx = MakeBattle(carrierCard, out var carrier);
            if (ctx == null || carrier == null)
            {
                CheckTrue(false, "C0 前提不成立：对局 / 载体没建起来");
            }
            else
            {
                var buddy = PlaceUnit(ctx, 0, 4, buddyCard);
                CheckTrue(buddy != null, "C0 前提：旁边那张友方单位在格 4");
                var e = Attach(ctx, carrier, tpl140);
                int handBefore = ctx.Players[0].Hand.Count;
                CheckTrue(handBefore < 9, "C0b 前提：手牌没满（否则回手会走「进弃牌堆」那一支，读数会误判）");
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "UnitDeath", null, buddy);
                Check(n, 1, "C1 ★★★ **结论**：`ca 140` 对点定的那 1 张执行成功"
                          + "｜🧨 删掉 `ca 140` 那一跳 ⇒ 0 ⇒ 红");
                Check(ctx.Players[0].Hand.Count, handBefore + 1, "C2 ★★★ **灭自证**：**点定的那张**进了手牌（手牌 +1）");
                CheckTrue(!BoardHas(ctx, 0, buddy), "C3 ★★★ **灭自证**：它已离开棋盘（不是「只给手牌加一张」）");
                CheckTrue(BoardHas(ctx, 0, carrier), "C4 ★★★ **灭自证**：旁边那张（载体自己）**没被动**"
                                                   + "｜🧨 把目标集换成「取第一个己方单位」⇒ C2/C4 至少一条红");
            }
        }

        // ---- D 组：ca 410 triggerCodex（真执行 / 目标集判不了）-------------------------
        Section("D `ca 410 triggerCodex`：`ta 20` 能解出目标 ⇒ 登记；`ta 10` 判不出 ⇒ 出声、不结算");
        {
            var ctx = MakeBattle(PlainUnit("B121D1Carrier410"), out var carrier);
            if (ctx == null || carrier == null) CheckTrue(false, "D0 前提不成立：对局 / 载体没建起来");
            else
            {
                var e = Attach(ctx, carrier, tpl410);
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "CardPlayed", null, null);
                Check(n, 1, "D1 ★★ **结论**：`ca 410`（`ta 20` = 载体自己）⇒ 登记 1 条 `codex` 触发"
                          + "｜🧨 删掉 `ca 410` 那一跳 ⇒ 0 ⇒ 红");
            }

            var ctx2 = MakeBattle(PlainUnit("B121D1Carrier410b"), out var carrier2);
            if (ctx2 == null || carrier2 == null) CheckTrue(false, "D2 前提不成立：第二场没建起来");
            else
            {
                int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                var e2 = Attach(ctx2, carrier2, tpl410ta10);
                int n2 = RuleCore.RunEnchantmentLogicRowsForTest(ctx2, e2, carrier2, 0, 3, Trig, "CardPlayed", null, null);
                Check(n2, 0, "D2 ★★ `ta 10`（allFiltered）**判不出哪一方** ⇒ 不结算（返回 0，⛔ 不猜「全体」）");
                CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore,
                          "D3 ★★ **不静默**：这一档真的走了「数据缺口出声」那条路（计数涨了）");
            }
        }

        // ---- E 组：反证 —— 载荷缺的 ca 10 必须出声且不改状态 -------------------------
        Section("E 反证：`ca 10 doDamage`（缺 `damageCriteria`）⇒ 出声 + 不改任何状态");
        {
            var ctx = MakeBattle(PlainUnit("B121D1Carrier10"), out var carrier);
            if (ctx == null || carrier == null) CheckTrue(false, "E0 前提不成立：对局 / 载体没建起来");
            else
            {
                var foe = PlaceUnit(ctx, 1, 4, PlainUnit("B121D1Foe10"));
                int hpBefore = foe != null ? foe.Health : -1;
                int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                var e = Attach(ctx, carrier, tpl10);
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, foe);
                Check(n, 0, "E1 ★★★ **反证**：载荷缺 ⇒ 返回 0（**本行不结算**）");
                Check(foe != null ? foe.Health : -1, hpBefore, "E2 ★★★ **反证**：目标生命值**一点没动**（⛔ 不拿猜的数值打折）");
                CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore,
                          "E3 ★★★ **不静默**：走了「已知 ca、数据缺」的出声口（计数涨了）"
                        + "｜🧨 把它换成静默 `return 0` ⇒ 本行红（红线：不许静默失败）");
            }
        }

        // ---- F 组：反证 —— 不认识的 ca 仍走「没有执行器」----------------------------
        Section("F 反证：不认识的 `ca 999` 仍走「本版没有这个执行器」");
        {
            var ctx = MakeBattle(PlainUnit("B121D1Carrier999"), out var carrier);
            if (ctx == null || carrier == null) CheckTrue(false, "F0 前提不成立：对局 / 载体没建起来");
            else
            {
                var e = Attach(ctx, carrier, Template("B121D1Tpl999", Trig, 999, 20));
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n, 0, "F1 ★★ 不认识的 `ca` ⇒ 返回 0、不 crash（走 `NoteEnchantRowUnsupported`，⛔ 不静默）");
            }
        }

        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>一张附魔模板卡：1 条 ability / 1 行 logic（`ca` / `ta` 按实参）。</summary>
    static CardDef Template(string id, int trigger, int ca, int ta)
    {
        var c = new CardDef(id, id, "enchantment", "", "", "", 0, 0, 0, 0, null);
        c.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = trigger, TriggerValue = 0, Ca = ca, Ta = ta }
        });
        c.AttachAbilityTriggers(new[] { trigger });
        return c;
    }

    /// <summary>载体卡（不带任何 ability / 正文）。</summary>
    static CardDef PlainUnit(string id)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 20, 0, null, subtype: "Infantry");
    }

    /// <summary>带一条「触发数值 → 正文 op」的载体卡（ca 320 要靠这张卡的 `trigger 410` 观察）。</summary>
    static CardDef UnitWithTriggerOps(string id, int triggerNumber, string text)
    {
        var c = new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 20, 0, null, subtype: "Infantry");
        var ops = EffectText.Parse(text, out _, out _);
        if (ops != null)
            for (int i = 0; i < ops.Count; i++)
                if (ops[i] != null) { ops[i].AbilityIndex = 0; ops[i].LogicIndex = 0; }
        c.AttachTriggerOpsByNumber(new Dictionary<int, List<EffectOp>> { { triggerNumber, ops ?? new List<EffectOp>() } });
        return c;
    }

    static BattleContext MakeBattle(CardDef carrierCard, out UnitState carrier)
    {
        var heroA = new CardDef("B121D1HeroA", "B121D1HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B121D1HeroB", "B121D1HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var ctx = RuleCore.NewBattle(new List<CardDef> { heroA, carrierCard },
                                     new List<CardDef> { heroB },
                                     seed: 0, shuffle: false, cardPool: new List<CardDef>());
        carrier = PlaceUnit(ctx, 0, 3, carrierCard);
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

    /// <summary>挂一件附魔（唯一写口 = `Enchantments.Attach`）。</summary>
    static ActiveEnchantment Attach(BattleContext ctx, UnitState carrier, CardDef tpl)
    {
        if (ctx == null || carrier == null || carrier.Instance == null || tpl == null) return null;
        var eff = Enchantments.Attach(ctx, carrier.Instance, tpl, Enchantments.BuffTypeSingleUse);
        return eff != null ? eff.Enchantment : null;
    }

    static bool BoardHas(BattleContext ctx, int p, UnitState u)
    {
        if (ctx == null || u == null) return false;
        var board = ctx.Players[p].Board;
        for (int s = 0; s < board.Length; s++) if (ReferenceEquals(board[s], u)) return true;
        return false;
    }

    // ==================================================================
    //  最小断言框架
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
        if (_fail == 0) Debug.Log($"B121_D1 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B121_D1 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
