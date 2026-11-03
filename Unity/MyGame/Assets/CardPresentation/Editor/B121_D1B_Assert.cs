// B121_D1B_Assert.cs — 第一百二十一~二十二会话 · 执行写手 `B121-D1B`（附魔模板**载荷导出/装载**）
//
// ============================ 它盯的是什么 ============================
//   `B121-D1` 查明：附魔模板的 `ca` 执行器缺的不是代码，是**数据** —— `enchantment_templates.json`
//   一行 logic 只导出 `ca/pa/ta/fm` 四格标量，`targetCriteria` 36 格 / `damageCriteria` / `healCriteria` /
//   `manaBuff` / `summonCriteria` 一个都没导出；运行时的 `CardDef.TriggerAbilityIndexSpec` 只带 `Ca`/`Ta`。
//   本件验的是这条链的**两端**：① 生成器真的把载荷写进了第二张表并挂到运行时（`Pa`/`Fm`/`Mult` 不再是 `-1`）；
//   ② 载荷落地后 `ca 0 / 10 / 20 / 60` **真执行**（不是「已知但不结算」）。
//
// ============================ 原版口径（判据，现读 `d:/2/tools/decomp_full/`）============================
//   · `ca 0 drawCard`     `AbilityLogic__PlayAbility.c:1439-1454`（`pa ∈ {0,10}` ⇒ 己方抽；`pa ∈ {0,20}` ⇒ 敌方抽）；
//   · `ca 10 doDamage`    `:1461-1508`（`damageCriteria` → `GetValueFromCriteria` → `DealMultiDamage` 打整张目标表）；
//   · `ca 20 doHeal`      `:1262-1306`（`targets.Count < 2` ⇒ 只治 `targets[0]`）；
//   · `ca 60 buffMana`    `:685-721` → `ResolveManaBuff.c:94-104` → `ProcessManaBuff.c`（createMana = 永久抬上限）；
//   · 目标集 = `AbilityLogic__GetTargets.c`（`ta 10` 建池 + `FilterMethods__CheckIfMeetsCriteria` 逐候选筛）；
//   · `ca 185 / 236 / 238` = 执行体缺（`PlayAbility.c:406-413` / `:2716-2747` / `:2764-2779`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · B2：删掉 `RunEnchantmentLogicRows` 里 `ca 0` 那一跳 ⇒ 手牌不涨 ⇒ 红；
//   · B3 是结构上的灭自证：`pa = 20` 必须让**对手**手牌 +1 而**自己**不动 —— 「两边都抽」同时满足不了；
//   · C2：删掉 `ca 10` 那一跳 / 把目标集换成「双方全场」⇒ 载体也掉血 ⇒ 红；
//   · D1：删掉 `ca 20` 那一跳 ⇒ 生命不涨 ⇒ 红；
//   · E1：删掉 `ca 60` 那一跳 ⇒ `MaxEnergy` 不涨 ⇒ 红；
//   · F1/F2 **反证**：`ca 185 / 236 / 238` **必须仍然**返回 0、目标生命不动、且「数据缺口」计数涨 ——
//     把 `IsKnownChosenAbilityWithoutPayload` 改成恒 `false`（或改成静默 `return 0`）⇒ 计数不涨 ⇒ 红。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加：
//     `Guard("B121D1B_Assert", B121_D1B_Assert.Run); _pass += B121_D1B_Assert.Pass; _fail += B121_D1B_Assert.Fail;`
//   （⚠️ 追加的文本必须落在该行第一个 `//` **之前**。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEditor;
using UnityEngine;

/// <summary>B121-D1B 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B121_D1B_Assert
{
    const int Trig = 640;          // 模板行挂一个存在的 trigger（本组只测 `ca` 分派 / 载荷，不测派发闸）
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B121-D1B：附魔模板载荷（导出 → 装载 → 真执行）");

        // ---- A 组：数据列真的挂到运行时（Pa / Fm / Mult）-----------------------------
        Section("A 数据列：`Pa` / `Fm` / `Mult` 从第二张表挂进 `TriggerAbilityIndexSpec`");
        {
            AssetDatabase.Refresh();                       // 盘上的 JSON 刚被生成器重写过 ⇒ 先让资源库看见它
            var tpls = CardDatabase.LoadEnchantmentTemplates();
            CheckTrue(tpls != null && tpls.Count == 86,
                      $"A0 前提：第二张表读到 86 张模板（实际 = {(tpls != null ? tpls.Count : -1)}）");
            int rows = 0, paKnown = 0, fmKnown = 0, multKnown = 0, ca0Rows = 0, ca0PaOk = 0;
            if (tpls != null)
            {
                for (int i = 0; i < tpls.Count; i++)
                {
                    var def = tpls[i];
                    var idx = def != null ? def.TriggerAbilityIndices : null;
                    if (idx == null) continue;
                    for (int k = 0; k < idx.Length; k++)
                    {
                        var r = idx[k];
                        if (r == null || r.Lg < 0) continue;
                        rows++;
                        if (r.Pa != -1) paKnown++;
                        if (r.Fm != -1) fmKnown++;
                        if (r.Mult != -1) multKnown++;
                        if (r.Ca == 0)
                        {
                            ca0Rows++;
                            if (r.Pa == 10 || r.Pa == 20) ca0PaOk++;
                        }
                    }
                }
            }
            CheckTrue(rows > 100, $"A1 前提：模板的 logic 行数 > 100（实际 {rows}）");
            Check(paKnown, rows, "A2 ★★★ **结论**：每一行都带 `Pa`（`playersAffected`，不再是 `-1`）"
                               + "｜🧨 删掉 `FillEnchantmentIndexPayload` 的 `r.Pa = lg.pa` ⇒ 本行红");
            Check(fmKnown, rows, "A3 ★★★ **结论**：每一行都带 `Fm`（`furtherMultiplier`，不再是 `-1`）");
            Check(multKnown, rows, "A4 ★★ 每一行都带 `Mult`（`multiplier.targetsAffected`）");
            Check(ca0Rows, 12, $"A5 ★★ `ca 0` 的行数 = 12（实际 {ca0Rows}）");
            Check(ca0PaOk, ca0Rows, "A6 ★★★ **结论**：`ca 0` 每一行的 `Pa` 都是 10/20（决定抽牌给哪一侧）");
        }

        // ---- B 组：ca 0 drawCard（真执行）--------------------------------------------
        Section("B `ca 0 drawCard`：`pa 10` ⇒ 自己抽 1 张；`pa 20` ⇒ 对手抽 1 张（自己不动）");
        {
            var ctx = MakeBattle(out var carrier, out var opp);
            if (ctx == null || carrier == null) { CheckTrue(false, "B0 前提不成立：对局 / 载体没建起来"); }
            else
            {
                var tpl = Template("B121D1BTplDrawSelf", Trig, ca: 0, pa: 10, ta: 0, fm: 0, mult: 0, crit: null);
                var e = Attach(ctx, carrier, tpl);
                int mine = ctx.Players[0].Hand.Count, theirs = ctx.Players[1].Hand.Count;
                CheckTrue(ctx.Players[0].Deck.Count > 0, "B0b 前提：自己牌库非空");
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n, 1, "B1 ★★★ **结论**：`ca 0`（pa 10）执行器返回 1"
                          + "｜🧨 删掉 `ca 0` 那一跳 ⇒ 0 ⇒ 红");
                Check(ctx.Players[0].Hand.Count, mine + 1, "B2 ★★★ **结论**：**自己**手牌 +1（抽牌真跑了）");
                Check(ctx.Players[1].Hand.Count, theirs, "B3 ★★★ **反证**：`pa 10` **没有**让对手抽牌");
            }

            var ctx2 = MakeBattle(out var carrier2, out var opp2);
            if (ctx2 == null || carrier2 == null) { CheckTrue(false, "B4 前提不成立：第二场没建起来"); }
            else
            {
                var tpl = Template("B121D1BTplDrawFoe", Trig, ca: 0, pa: 20, ta: 0, fm: 0, mult: 0, crit: null);
                var e = Attach(ctx2, carrier2, tpl);
                int mine = ctx2.Players[0].Hand.Count, theirs = ctx2.Players[1].Hand.Count;
                CheckTrue(ctx2.Players[1].Deck.Count > 0, "B4b 前提：对手牌库非空");
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx2, e, carrier2, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n, 1, "B5 ★★ `ca 0`（pa 20）执行器返回 1");
                Check(ctx2.Players[1].Hand.Count, theirs + 1, "B6 ★★★ **灭自证**：抽牌落在**对手**身上（+1）");
                Check(ctx2.Players[0].Hand.Count, mine, "B7 ★★★ **灭自证**：自己一张没多（`pa 20` 不是「两边都抽」）");
            }
        }

        // ---- C 组：ca 10 doDamage（真执行 + 36 格目标筛）------------------------------
        Section("C `ca 10 doDamage`：`damageCriteria` 2 点 → 只打**敌方**（36 格 criteria 生效）");
        {
            var ctx = MakeBattle(out var carrier, out var opp);
            if (ctx == null || carrier == null || opp == null) { CheckTrue(false, "C0 前提不成立：对局 / 单位没建起来"); }
            else
            {
                var foe = PlaceUnit(ctx, 1, 4, PlainUnit("B121D1BFoe10"));
                var crit = Criteria(ta: 10);
                crit.EnemyUnits = 1; crit.FriendlyUnits = 0; crit.IncludeMinions = 1;
                var tpl = Template("B121D1BTplDmg", Trig, ca: 10, pa: 0, ta: 10, fm: 0, mult: 0, crit: crit);
                tpl.AttachAbilityDamage(new[]
                {
                    new CardDef.AbilityDamageSpec { Ab = 0, Lg = 0, Min = 2, Max = 2, ValueOptions = 0, Type = 0 },
                });
                var e = Attach(ctx, carrier, tpl);
                int foeHp = foe != null ? foe.Health : -1, myHp = carrier.Health;
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, foe);
                CheckTrue(n >= 1, "C1 ★★★ **结论**：`ca 10` 执行器返回 ≥1（返回值 = **实际结算的目标数**，夹具里敌方 3 个 ⇒ 现读 " + n + "）"
                          + "｜🧨 删掉 `ca 10` 那一跳 ⇒ 0 ⇒ 红");
                Check(foe != null ? foe.Health : -1, foeHp - 2, "C2 ★★★ **结论**：目标掉了 2 点血（`damageCriteria` 真用上了）");
                Check(carrier.Health, myHp, "C3 ★★★ **灭自证**：**友方载体没掉血**"
                                          + "（`friendlyUnits = 0` 那一格真在筛）"
                                          + "｜🧨 把目标集换成「双方全场」⇒ 本行红");
            }
        }

        // ---- D 组：ca 20 doHeal（真执行）--------------------------------------------
        Section("D `ca 20 doHeal`：`healCriteria` 3 点 → 载体回 3 点血");
        {
            var ctx = MakeBattle(out var carrier, out var opp);
            if (ctx == null || carrier == null) { CheckTrue(false, "D0 前提不成立：对局 / 载体没建起来"); }
            else
            {
                carrier.Health = Math.Max(1, carrier.MaxHealth - 5);
                int before = carrier.Health;
                var tpl = Template("B121D1BTplHeal", Trig, ca: 20, pa: 0, ta: 20, fm: 0, mult: 0, crit: Criteria(ta: 20));
                tpl.AttachAbilityHeal(new[]
                {
                    new CardDef.AbilityHealSpec { Ab = 0, Lg = 0, Min = 3, Max = 3, ValueOptions = 0 },
                });
                var e = Attach(ctx, carrier, tpl);
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n, 1, "D1 ★★★ **结论**：`ca 20` 执行器返回 1｜🧨 删掉 `ca 20` 那一跳 ⇒ 0 ⇒ 红");
                Check(carrier.Health, before + 3, "D2 ★★★ **结论**：生命 +3（`healCriteria` 真用上了，且不超上限）");
            }
        }

        // ---- E 组：ca 60 buffMana（真执行）------------------------------------------
        Section("E `ca 60 buffMana`：`createMana` 1 → 载体属主的**最大能量** +1（对手不动）");
        {
            var ctx = MakeBattle(out var carrier, out var opp);
            if (ctx == null || carrier == null) { CheckTrue(false, "E0 前提不成立：对局 / 载体没建起来"); }
            else
            {
                var tpl = Template("B121D1BTplMana", Trig, ca: 60, pa: 0, ta: 0, fm: 0, mult: 0, crit: null);
                tpl.AttachAbilityManaBuff(new[]
                {
                    new CardDef.AbilityManaBuffSpec { Ab = 0, Lg = 0, ManaAmount = 1, FriendlyPlayer = 1,
                                                     CreateMana = 1, EnemyPlayer = 0, RefillMana = 0 },
                });
                var e = Attach(ctx, carrier, tpl);
                int maxBefore = ctx.Players[0].MaxEnergy, foeMaxBefore = ctx.Players[1].MaxEnergy;
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n, 1, "E1 ★★★ **结论**：`ca 60` 执行器返回 1｜🧨 删掉 `ca 60` 那一跳 ⇒ 0 ⇒ 红");
                Check(ctx.Players[0].MaxEnergy, maxBefore + 1, "E2 ★★★ **结论**：最大能量 +1（`createMana` = 永久抬上限）");
                Check(ctx.Players[1].MaxEnergy, foeMaxBefore, "E3 ★★ **反证**：对手的最大能量没动（`enemyPlayer = 0`）");
            }
        }

        // ---- F 组：反证 —— 执行体缺的三档仍然精确出声、不改状态 ------------------------
        Section("F 反证：`ca 185 / 236 / 238`（执行体缺）⇒ 返回 0 + 出声 + 不改状态");
        {
            int[] cas = { 185, 236, 238 };
            for (int i = 0; i < cas.Length; i++)
            {
                int ca = cas[i];
                var ctx = MakeBattle(out var carrier, out var opp);
                if (ctx == null || carrier == null) { CheckTrue(false, $"F{ca}a 前提不成立"); continue; }
                var foe = PlaceUnit(ctx, 1, 5, PlainUnit("B121D1BFoeGap" + ca));   // 🔴 2026-11-03（B124-W7 · δ）：原来摆**格 4 = `BoardSpec.WarlordSlot`**（`RuleEngine/Core/BoardSpec.cs:30`）—— 那是**覆盖对手督军**、不是本组要测的东西（`BoardSlots.RemoveAt` 遇督军格 no-op，`EffectResolver.cs:6844-6850` 照样加手牌）⇒ 挪到右贴督军格 5
                var tpl = Template("B121D1BTplGap" + ca, Trig, ca: ca, pa: 0, ta: 10, fm: 0, mult: 0, crit: null);   // 🔴 2026-11-03（B124-W7 · δ）：第二参 = **trigger**，A–E 组都已传 `Trig`，本组原来传 `0` ⇒ `TriggerAbilityRowsOf(640)` 空 ⇒ 执行器 `EnchantmentExecutor_B115.cs:86-87` 直接 `return 0`，`.1/.2` 空转绿、`.3` 恒红
                var e = Attach(ctx, carrier, tpl);
                int hp = foe != null ? foe.Health : -1;
                int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, foe);
                Check(n, 0, $"F{ca}.1 ★★★ **反证**：`ca {ca}` 仍然不结算（返回 0）");
                Check(foe != null ? foe.Health : -1, hp, $"F{ca}.2 ★★★ **反证**：目标生命一点没动");
                CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore,
                          $"F{ca}.3 ★★★ **不静默**：走了「已知 ca、载荷/执行体缺」的出声口（计数涨了）"
                        + "｜🧨 改成静默 `return 0` ⇒ 本行红");
            }
        }

        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>一张附魔模板卡：1 条 ability / 1 行 logic；`crit` 非 null 时一起挂 36 格。</summary>
    static CardDef Template(string id, int trigger, int ca, int pa, int ta, int fm, int mult,
                            CardDef.CountSetSpec crit)
    {
        var c = new CardDef(id, id, "enchantment", "", "", "", 0, 0, 0, 0, null);
        c.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec
            { Ab = 0, Lg = 0, Trigger = trigger, TriggerValue = 0, Ca = ca, Ta = ta, Pa = pa, Fm = fm, Mult = mult },
        });
        c.AttachAbilityTriggers(new[] { trigger });
        if (crit != null)
            c.AttachAbilityTargetCriteria(new[]
            {
                new CardDef.TargetCriteriaRowSpec { Ability = 0, Trigger = trigger, Logic = 0, Criteria = crit },
            });
        return c;
    }

    /// <summary>一份**全格都已知**的 36 格 criteria（`-1` 会让 `TargetCriteriaEval` 返回「判不了」）。
    /// 缺省 = 原版「明确不筛」的全 0；调用方按需改那几格。</summary>
    static CardDef.CountSetSpec Criteria(int ta)
    {
        var s = new CardDef.CountSetSpec();
        s.PlayerTurn = -1;                     // 结构上不适用（同生成器：`has_player_turn=False`）
        s.TargetsAffected = ta;
        s.ChosenArmy = 0; s.ArmyFilter = 0; s.KeywordFilter = 0; s.TraitsFilter = 0;
        s.MaxTargets = 0; s.TargetSelectMethod = 0;
        s.AttackFilter = 0; s.AttackFilterOption = 0;
        s.CurrentHealthFilter = 0; s.CurrentHealthFilterOption = 0;
        s.ManaCostFilter = 0; s.ManaCostFilterOption = 0;
        s.RarityFilter = 0; s.RarityFilterOption = 0;
        s.SpellType = 0;
        s.FriendlyUnits = 0; s.EnemyUnits = 0;
        s.IncludeHero = 0; s.IncludeMinions = 0; s.IncludeSpells = 0; s.IncludeWhispers = 0;
        s.IncludeWeapons = 0; s.IncludeRemnants = 0; s.IncludeHeroParts = 0;
        s.IncludeSelf = 0; s.ExcludeNotSelf = 0; s.ExcludeSimilarCards = 0; s.ExcludeTargetted = 0;
        s.ExcludeFriendlyHero = 0; s.ExcludeEnemyHero = 0; s.ExcludeRace = 0; s.ExcludeTrait = 0;
        s.SelectFromChooseResolution = 0;
        s.FilterSpecificCardIds = new string[0];
        s.ExcludeEnchantmentIds = new string[0];
        return s;
    }

    /// <summary>载体卡（不带 ability / 正文）。</summary>
    static CardDef PlainUnit(string id)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 20, 0, null, subtype: "Infantry");
    }

    /// <summary>建一局：双方各 6 张「填充卡」+ 一张督军；0 号玩家格 3 放载体、1 号玩家格 3 放一个敌兵。</summary>
    static BattleContext MakeBattle(out UnitState carrier, out UnitState opp)
    {
        var heroA = new CardDef("B121D1BHeroA", "B121D1BHeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B121D1BHeroB", "B121D1BHeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var carrierCard = PlainUnit("B121D1BCarrier");
        var deckA = new List<CardDef> { heroA, carrierCard };
        var deckB = new List<CardDef> { heroB };
        for (int i = 0; i < 6; i++) { deckA.Add(PlainUnit("B121D1BFillA" + i)); deckB.Add(PlainUnit("B121D1BFillB" + i)); }
        var ctx = RuleCore.NewBattle(deckA, deckB, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        carrier = PlaceUnit(ctx, 0, 3, carrierCard);
        opp = PlaceUnit(ctx, 1, 3, PlainUnit("B121D1BOpp"));
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
        if (_fail == 0) Debug.Log($"B121_D1B 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B121_D1B 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
