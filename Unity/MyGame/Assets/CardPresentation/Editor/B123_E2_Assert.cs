// B123_E2_Assert.cs — 批次 `B123` · 执行代理 **E2**（附魔手牌路 / `EnchantmentExecutor` + 手牌效果）
//
// ============================ 它盯的是什么 ============================
//   ① `ca 30 buffOthers` + `ta = 50 inHandFiltered` 的**手牌目标路**（本波新接，改前完全不结算）：
//      判据 = `AbilityLogic__PlayAbility.c:1176-1250`（`case 0x1e`，逐目标 `AddEffect`）
//           + `AbilityLogic__GetTargets.c:893-901`（`ta == 0x32` ⇒ `GetCardsInHandList(friendly, enemy)`）
//           + `:1086-1095` 逐候选 `CheckIfMeetsCriteria` + `:1164-1365` `maxTargets` 裁剪。
//      载荷 = `abilityBuffs` 的 **`buffEffect` 行**（`bf 2 / costChange -1`）；`handBuff` 行属 `ca 50`，⛔ 不混。
//      ⛔ **不登记** `ps.HandEffectRecords`（那是 `ca 50 buffHand` 的语义 —— 登记了会加宽到后进手牌的牌）。
//   ② `ca 238 scriptedSelfCardAttackDontCount` 改成**转调** `DeclareAttackRefundingForced(…, forceRefund: true)`：
//      行为必须与改前**逐字相同**（预扣 + `wasFree` 才退还 ⇒ 净 0）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · E2-1.2：删掉 `maxTargets` 裁剪 ⇒ 目标数 2 ⇒ 红（原版 `GetTargets.c:1164`）；
//   · E2-1.5：把「钉实例」改成「按卡 id 改」⇒ 两张**同名**手牌一起吃 ⇒ 红；
//   · E2-1.6：忽略 `friendlyUnits` / `enemyUnits`（两边手牌全给）⇒ 敌方那张也有记录 ⇒ 红；
//   · E2-1.9/E2-1.10：与 `ca 50` 合流（`ps.HandEffectRecords.Add`）⇒ 后进手牌的同名牌也吃上 ⇒ 红；
//   · E2-3.2：没有 36 格时不许「猜一个」⇒ 必须出声 + 不结算；
//   · E2-4.3：`ca 238` 去掉退还 ⇒ `AttacksThisTurn` 净 −1（2 → 1）⇒ 红；去掉预扣 ⇒ 净 +? ⇒ 红。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs:1400` 那条聚合行**同一物理行**追加
//   （⛔ 追加的文本必须落在该行第一个 `//` **之前**）：
//     `B123_E2_Assert.Run(); _pass += B123_E2_Assert.Pass; _fail += B123_E2_Assert.Fail;`
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B123-E2 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B123_E2_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B123-E2：`ca 30 buffOthers + ta 50` 手牌目标路 ＋ `ca 238` 的 `forceRefund` 转调");
        LivingIcon();
        SolarEclipse();
        NoCriteria();
        ScriptedForceAttack238();
        Summary();
    }

    // ==============================================================================================
    //  E2-1 `Living Icon`（真数据）：我方手 · 兵 · `manaCostFilter` · `maxTargets 1`
    // ==============================================================================================
    static void LivingIcon()
    {
        Section("E2-1 `Living Icon`（`enchantment_templates.json` 真行）：只挑**1 张**、只在我方手、只改**那一份**");
        CardDef tpl = FindTemplate("Living Icon");
        CheckTrue(tpl != null, "E2-1.0 前提：表里有 `Living Icon`");
        if (tpl == null) return;
        var rows = tpl.TriggerAbilityIndices;
        CheckTrue(rows != null && rows.Length == 1 && rows[0].Ca == 30 && rows[0].Ta == 50,
                  "E2-1.0b 前提：这一行 = `ca 30 / ta 50`（改前在 `ResolveBuffOthersTargets` 里 `ta==50` 早退）");
        var buffs = CardDatabase.AbilityBuffsOf(tpl);
        CheckTrue(buffs != null && CountBuffRows(buffs, false) >= 1,
                  "E2-1.0c 前提：有 `buffEffect` 载荷行（`bt 2 / costChange -1`）");
        if (rows == null || rows.Length == 0 || buffs == null) return;

        var ctx = MakeBattle(out var carrier, out var opp);
        if (ctx == null || carrier == null) { CheckTrue(false, "E2-1.0d 前提不成立：对局没建起来"); return; }

        // 两张**同名**手牌（同一个 `CardDef`、两个不同实例）+ 敌方手里一张同一种卡
        var same = Troop("B123E2SameTroop", 5);
        var a1 = ctx.NewInstance(same);
        var a2 = ctx.NewInstance(same);
        var enemyCard = ctx.NewInstance(same);
        ctx.Players[0].Hand.Add(a1);
        ctx.Players[0].Hand.Add(a2);
        ctx.Players[1].Hand.Add(enemyCard);

        var w = Attach(ctx, carrier, tpl);
        CheckTrue(w != null, "E2-1.0e 前提：附魔挂上了");
        if (w == null) return;
        int recBefore = ctx.Players[0].HandEffectRecords.Count;
        int modsBefore = ctx.CostMods.Count;

        var got = RuleCore.ResolveBuffOthersHandTargetsForTest(ctx, 0, carrier, null, tpl, rows[0]);
        CheckTrue(got != null, "E2-1.1 ★★★ **结论**：`ta = 50` 的手牌目标集能解出来（改前恒 `null`）"
                             + "｜🧨 删掉手牌路 ⇒ null ⇒ 红");
        Check(got != null ? got.Count : -1, 1,
              "E2-1.2 ★★★ **结论**：`maxTargets = 1` ⇒ **只挑 1 张**（原版 `GetTargets.c:1164-1365`）"
            + "｜🧨 删掉裁剪 ⇒ 2 ⇒ 红");
        CheckTrue(got != null && got.Count == 1 && !ReferenceEquals(got[0], enemyCard),
                  "E2-1.3 ★★★ **灭自证**：挑中的**不是敌方手牌那张**（`friendlyUnits=1 / enemyUnits=0`）"
                + "｜🧨 忽略这两格 ⇒ 敌方那张进候选 ⇒ 本行可能红");
        CheckTrue(got != null && got.Count == 1 && ReferenceEquals(got[0], a1) || got != null && got.Count == 1 && ReferenceEquals(got[0], a2),
                  "E2-1.3b 挑中的是**我方两张同名手牌之一**");

        int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, w, carrier, 0, 3, rows[0].Trigger,
                                                        "EarnedSkull", null, null);
        CheckTrue(n >= 1, "E2-1.4 ★★★ **结论**：这一行真的结算了（返回 ≥ 1）"
                        + "｜🧨 把新分派删掉 ⇒ 回到 `ta==50` 早退 ⇒ 0 ⇒ 红");

        int withRecord = 0;
        for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
            if (HasSpecRecord(ctx.Players[0].Hand[i])) withRecord++;
        Check(withRecord, 1,
              "E2-1.5 ★★★ **结论**：我方手里**只有 1 张**带记录（两张**同名**手牌不一起改）"
            + "｜🧨 `CostMod.Key` 写成卡 id / 遍历时按卡名比 ⇒ 2 ⇒ 红");
        CheckTrue(!HasSpecRecord(enemyCard), "E2-1.6 ★★★ **结论**：敌方那张**没有**记录（`enemyUnits = 0`）");

        int added = 0, pinned = -1;
        for (int i = modsBefore; i < ctx.CostMods.Count; i++)
        {
            var m = ctx.CostMods[i];
            if (m == null) continue;
            added++;
            if (m.Key == "*" && m.Delta == -1) pinned = m.HandInstanceId;
        }
        Check(added, 1, "E2-1.7 ★★★ **结论**：只登记了 **1** 条费用修正（`costChange = -1`）");
        CheckTrue(pinned == a1.Id || pinned == a2.Id,
                  "E2-1.8 ★★★ **结论**：修正**钉在一个具体实例**上（`HandInstanceId = inst.Id`，⛔ 不是卡 id）");

        Check(ctx.Players[0].HandEffectRecords.Count, recBefore,
              "E2-1.9 ★★★ **灭自证**：**不登记** `ps.HandEffectRecords`（原版 `ca 30` 是逐目标 `AddEffect`，"
            + "不是 `AddHandEffect`）｜🧨 与 `ca 50` 合流 ⇒ +1 ⇒ 红");

        var later = ctx.NewInstance(same);
        ctx.Players[0].Hand.Add(later);
        CheckTrue(!HasSpecRecord(later),
                  "E2-1.10 ★★★ **灭自证**：之后再进手牌的同名牌**没吃上**"
                + "｜🧨 登记了常驻记录 ⇒ 它会被 `SetupCardInHand` 补挂 ⇒ 红");
    }

    // ==============================================================================================
    //  E2-2 `Solar Eclipse`（真数据，两行）：`ab0/lg0` = 我方手 · `ab0/lg1` = 敌方手
    // ==============================================================================================
    static void SolarEclipse()
    {
        Section("E2-2 `Solar Eclipse`（两行真数据）：我方手 1 张 + 敌方手 1 张，各落 1 条；我方部队卡不吃");
        CardDef tpl = FindTemplate("Solar Eclipse");
        CheckTrue(tpl != null, "E2-2.0 前提：表里有 `Solar Eclipse`");
        if (tpl == null) return;
        var rows = tpl.TriggerAbilityIndices;
        CheckTrue(rows != null && rows.Length == 2 && rows[0].Ca == 30 && rows[1].Ca == 30
                  && rows[0].Ta == 50 && rows[1].Ta == 50,
                  "E2-2.0b 前提：两行都是 `ca 30 / ta 50`（`ab0/lg0` 与 `ab0/lg1`）");
        if (rows == null || rows.Length < 2) return;

        var ctx = MakeBattle(out var carrier, out var opp);
        if (ctx == null || carrier == null) { CheckTrue(false, "E2-2.0c 前提不成立"); return; }

        var ownSpell = Spell("B123E2OwnSpell", 3);
        var enemySpell = Spell("B123E2EnemySpell", 3);
        var ownTroop = Troop("B123E2OwnTroop", 3);
        var o = ctx.NewInstance(ownSpell);
        var x = ctx.NewInstance(enemySpell);
        var t = ctx.NewInstance(ownTroop);
        ctx.Players[0].Hand.Add(o);
        ctx.Players[0].Hand.Add(t);          // 我方**部队**卡：`includeSpells = 1` ⇒ 不该吃
        ctx.Players[1].Hand.Add(x);

        var w = Attach(ctx, carrier, tpl);
        CheckTrue(w != null, "E2-2.0d 前提：附魔挂上了");
        if (w == null) return;

        int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, w, carrier, 0, 3, rows[0].Trigger,
                                                        "EarnedSkull", null, null);
        Check(n, 2, "E2-2.1 ★★★ **结论**：两行各落 1 张 ⇒ 返回 2"
                  + "｜🧨 只认 `friendlyUnits` 那一侧（或只认一侧）⇒ 1 ⇒ 红");
        CheckTrue(HasSpecRecord(o), "E2-2.2 ★★★ **结论**：**我方手牌里的法术**吃上了（`friendlyUnits = 1`）");
        CheckTrue(HasSpecRecord(x), "E2-2.3 ★★★ **结论**：**敌方手牌里的法术**也吃上了（`ab0/lg1` 的 `enemyUnits = 1`）"
                                  + "｜🧨 `HandSideIncluded` 恒取 `side == owner` ⇒ 这一行红");
        CheckTrue(!HasSpecRecord(t), "E2-2.4 ★★★ **反证**：同在我方手里的**部队卡**没吃（`includeSpells = 1` 真的在筛）"
                                   + "｜🧨 卡种白名单失效 ⇒ 它也被挂 ⇒ 红");
    }

    // ==============================================================================================
    //  E2-3 反证：没有 36 格 ⇒ **不结算 + 出声**（⛔ 不许猜「两边手牌全给」）
    // ==============================================================================================
    static void NoCriteria()
    {
        Section("E2-3 反证：`ca 30 / ta 50` 但**没有 36 格** ⇒ 不结算 + 精确出声");
        var ctx = MakeBattle(out var carrier, out var opp);
        if (ctx == null || carrier == null) { CheckTrue(false, "E2-3.0 前提不成立"); return; }
        var same = Troop("B123E2NoCritTroop", 2);
        ctx.Players[0].Hand.Add(ctx.NewInstance(same));
        ctx.Players[1].Hand.Add(ctx.NewInstance(same));   // 两边都有 ⇒ 若「猜两边全给」会挂 2 张

        var tpl = Template("B123E2TplNoCriteria", 0, ca: 30, ta: 50, crit: null);
        var row = tpl.TriggerAbilityIndices[0];
        var w = Attach(ctx, carrier, tpl);
        if (w == null) { CheckTrue(false, "E2-3.0b 前提不成立：附魔没挂上"); return; }
        int gapBefore = RuleCore.EnchantBuffGapForTest;
        int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, w, carrier, 0, 3, 0, "EarnedSkull", null, null);
        Check(n, 0, "E2-3.1 ★★★ **反证**：没有 36 格时**不结算**（返回 0）"
                  + "｜🧨 拿「两边手牌全给」顶 ⇒ 2 ⇒ 红");
        CheckTrue(RuleCore.EnchantBuffGapForTest > gapBefore,
                  "E2-3.2 ★★★ **不静默**：走了 `NoteEnchantBuffGap` 那个出声口（计数涨了）");
        CheckTrue(!HasSpecRecord(ctx.Players[0].Hand[0]) && !HasSpecRecord(ctx.Players[1].Hand[0]),
                  "E2-3.3 ★★★ **反证**：两边手牌里一张都没被改");
    }

    // ==============================================================================================
    //  E2-4 `ca 238`：`forceRefund: true` 转调后行为逐字不变（预扣 + 退还 ⇒ 净 0）
    // ==============================================================================================
    static void ScriptedForceAttack238()
    {
        Section("E2-4 `ca 238` 转调 `DeclareAttackRefundingForced(forceRefund: true)`：净 0（预扣 + 退还）");
        var ctx = MakeBattle(out var carrier, out var opp);
        if (ctx == null || carrier == null || opp == null) { CheckTrue(false, "E2-4.0 前提不成立"); return; }

        var crit = Criteria(10);
        crit.EnemyUnits = 1; crit.FriendlyUnits = 0; crit.IncludeMinions = 1;
        var tpl = Template("B123E2Tpl238", 0, ca: 238, ta: 10, crit: crit);
        var w = Attach(ctx, carrier, tpl);
        if (w == null) { CheckTrue(false, "E2-4.0b 前提不成立：附魔没挂上"); return; }

        // ⚠️ **不要**把 `AttacksThisTurn` 预置成非 0：原版那一档的退还前提是 `wasFree`（= 入队时 `AttacksThisTurn == 0`），
        //    预置成 2 时**预扣 + 不退**才是对的（2 → 1），断言「净不变」会假红（改前那份复制版也是同一行为）。
        //    从 0 起时「攻击本身」那一句 `attacker.AttacksThisTurn++`（`RuleCore.cs:6599` 无条件）把计数顶到 1，
        //    退还再把它减回 0 ⇒ **净 0 是可观测的**：去掉退还 ⇒ 停在 1 ⇒ 红。
        int before = carrier.AttacksThisTurn, hp = opp.Health;
        int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, w, carrier, 0, 3, 0, "EarnedSkull", null, null);
        CheckTrue(n >= 1, "E2-4.1 ★★★ **结论**：`ca 238` 打出去了（返回 ≥ 1）｜🧨 删掉那一跳 ⇒ 0 ⇒ 红");
        CheckTrue(opp.Health < hp, "E2-4.2 ★★★ **结论**：被打的那个真掉血");
        Check(before, 0, "E2-4.2b 前提：入队前 `AttacksThisTurn` = 0（`wasFree` 才成立 ⇒ 退还那一跳可达）");
        Check(carrier.AttacksThisTurn, before,
              "E2-4.3 ★★★ **灭自证**：攻方的攻击次数**净不变**（原版 `dontCountAttack = 1`：入队预扣 + `wasFree` 退还）"
            + "｜🧨 去掉 `forceRefund: true` ⇒ 附魔模板卡让 `ForcedAttackRefunds` 判 ⇒ 不退还 ⇒ 停在 1 ⇒ 红");
    }

    // ==============================================================================================
    //  夹具
    // ==============================================================================================

    static CardDef FindTemplate(string name)
    {
        var list = CardDatabase.LoadEnchantmentTemplates();
        if (list == null) return null;
        for (int i = 0; i < list.Count; i++)
        {
            var c = list[i];
            if (c != null && string.Equals(c.Name, name, StringComparison.Ordinal)) return c;
        }
        return null;
    }

    static int CountBuffRows(IReadOnlyList<CardDatabase.AbilityBuffSpec> rows, bool handBuff)
    {
        int n = 0;
        if (rows == null) return 0;
        for (int i = 0; i < rows.Count; i++)
            if (rows[i] != null && rows[i].IsHandBuff == handBuff) n++;
        return n;
    }

    static bool HasSpecRecord(CardInstance inst)
    {
        if (inst == null || inst.HandEffects == null) return false;
        for (int i = 0; i < inst.HandEffects.Count; i++)
        {
            var e = inst.HandEffects[i];
            if (e != null && e.Spec != null) return true;
        }
        return false;
    }

    /// <summary>一张手写模板：1 条 ability / 1 行 logic；`crit` 非 null 时一起挂 36 格。</summary>
    static CardDef Template(string id, int trigger, int ca, int ta, CardDef.CountSetSpec crit)
    {
        var c = new CardDef(id, id, "enchantment", "", "", "", 0, 0, 0, 0, null);
        c.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec
            { Ab = 0, Lg = 0, Trigger = trigger, TriggerValue = 0, Ca = ca, Ta = ta, Pa = 0, Fm = 0, Mult = 0 },
        });
        c.AttachAbilityTriggers(new[] { trigger });
        if (crit != null)
            c.AttachAbilityTargetCriteria(new[]
            {
                new CardDef.TargetCriteriaRowSpec { Ability = 0, Trigger = trigger, Logic = 0, Criteria = crit },
            });
        return c;
    }

    /// <summary>一份**全格都已知**的 36 格 criteria（`-1` 会让 `TargetCriteriaEval` 返回「判不了」）。</summary>
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

    static CardDef PlainUnit(string id)
        => new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 20, 0, null, subtype: "Infantry");

    static CardDef Troop(string id, int cost)
        => new CardDef(id, id, "unit", "", "common", "Goff", cost, 2, 3, 0, null, subtype: "Infantry");

    static CardDef Spell(string id, int cost)
        => new CardDef(id, id, "tactic", "", "common", "Goff", cost, 0, 0, 0, null);

    /// <summary>建一局：双方各 6 张填充卡 + 督军；0 侧格 3 = 载体、1 侧格 3 = 敌兵；**手牌清空**（由用例自己摆）。</summary>
    static BattleContext MakeBattle(out UnitState carrier, out UnitState opp)
    {
        carrier = null; opp = null;
        var heroA = new CardDef("B123E2HeroA", "B123E2HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B123E2HeroB", "B123E2HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var carrierCard = PlainUnit("B123E2Carrier");
        var deckA = new List<CardDef> { heroA, carrierCard };
        var deckB = new List<CardDef> { heroB };
        for (int i = 0; i < 6; i++) { deckA.Add(PlainUnit("B123E2FillA" + i)); deckB.Add(PlainUnit("B123E2FillB" + i)); }
        var ctx = RuleCore.NewBattle(deckA, deckB, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        if (ctx == null || ctx.Players == null || ctx.Players.Length < 2) return ctx;
        ctx.Players[0].Hand.Clear();
        ctx.Players[1].Hand.Clear();
        carrier = PlaceUnit(ctx, 0, 3, carrierCard);
        opp = PlaceUnit(ctx, 1, 3, PlainUnit("B123E2Opp"));
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
        if (_fail == 0) Debug.Log($"B123_E2 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B123_E2 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
