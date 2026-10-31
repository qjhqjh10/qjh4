// B109_E7_EnchantVfx_Assert.cs — 第一百零九会话 · 波 1 · 执行代理 `B109_E7` 的断言
//
// ============================ 它盯的是什么 ============================
//  `BattleDriver` 的**表现侧**：**附魔卡自己的 `UnitDamaged(60)` 能力动画**
//  （`Pray Enchantment` → `Pray_Exit`）。原版链路（第一权威，逐句）：
//    `CardScript__ResolveDamageDealt.c:160` → `RawCardScript__OnTrigger(raw, 0x3c = UnitDamaged(60), …)`
//    → `RawCardScript__TriggerAbilities.c:49-197` 的**内联附魔趟**（`:108-109` `CanTriggerAbility` 比数据位；
//       `:184-186` `TriggerAbility`）→ `TriggerAbility.c:73-106` `PlayAbility`
//    → `AbilityLogic.PlayAnim`：**动画资产取自附魔卡自己**的 `cardAnimations[]`，锚点 = `AbilityData+0x10` = 载体。
//
// ============================ 本笔对施工单的【两处订正】（都实测过，B 组 / D 组分别钉住）============================
//  `报告/B108_A16.md` §六·2 给的那一版是「在 `PlaySignal` 的 `EvtKind.Hit` 段遍历
//  `Ctx.Players[p].Board[slot].Instance.ActiveEnchantments`，播 `VfxMap.Hit`」。**今天走不通**：
//   ① **桶已经搬走了**（B 组）：`VfxMap` 载表时把所有逐卡 `hit` 行【改归】`unit_damaged_anim`
//      （`Core/VfxMap.cs:1263-1271`，2026-10-23 `T1`）⇒ `ResolveAllTimed("hit", …, perCardOnly:true)`
//      对**任何**卡都恒回空数组。`Pray_Exit` 今天在 `VfxMap.UnitDamaged` 桶里。
//   ② **播放时刻 `ActiveEnchantments` 已经空了**（D 组）：`ApplyDamageCore` 那一格是**排队摘**
//      （`Enchantments.EnqueueRemovePray`；泵点 = 外层 `ApplyDamage` 的 `ActionScope` 退出，
//      `RuleCore.cs:8623`）⇒ 引擎调用返回时附魔已经摘掉，而 `PlaySignals` 是引擎返回**之后**才 drain。
//  ⇒ 本笔的锚点 = **派发侧那条 `EvtKind.Trigger`**（`CardId` = 附魔卡名 · `Keyword` = `UnitDamaged` ·
//     `Player/Slot` = 载体），它正是原版 `TriggerAbility` 那一跳的等价物，而且**发生在摘除之前**。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  唯一入口 = `B109_E7_EnchantVfx_Assert.Run()`；宿主接一个 `{ B109_E7_EnchantVfx_Assert.Run(); }` 即可。
//
// ============================ 🔴 灭自证 ============================
//  · B2：把 `VfxMap.cs` 的「`hit` → `unit_damaged_anim` 搬桶」删掉 ⇒ `hit` 桶回非空 ⇒ B2 红。
//  · C1/C3/C5：删 `BattleDriver.IsEnchantUnitDamagedTrigger` 里的 `IsEnchantment` 或
//    `AbilityTriggerPresence == 1`、或不再剥 `#i` 后缀 ⇒ 对应那条红。
//  · D3/D4/D5：把 `EnqueueRemovePray` 改回**同步摘**、或把 `EmitCardTrigger` 那一行删掉 ⇒ 红。
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>B109_E7 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>；⛔ 不改任何宿主）。</summary>
public static class B109_E7_EnchantVfx_Assert
{
    static int _fail, _pass;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _fail = 0; _pass = 0;
        Section("🆕 B109_E7：附魔卡的 `UnitDamaged(60)` 演出（`Pray Enchantment` → `Pray_Exit`）");

        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B109_E7] `CardDatabase.Load()` 抛了：" + e.Message); }
        CheckTrue(pool != null && pool.Count > 500, "A0 前提：卡池读得到且规模正常（≥ 500 张）");
        if (pool == null || pool.Count <= 500) { Summary(); return; }

        CardDef pray = null;
        for (int i = 0; i < pool.Count && pray == null; i++)
            if (pool[i] != null && pool[i].Id == Enchantments.PrayEnchantmentCardId) pray = pool[i];
        CheckTrue(pray != null, "A0b 前提：池里有 `Pray Enchantment`（`id " + Enchantments.PrayEnchantmentCardId + "`）");
        if (pray == null) { Summary(); return; }

        SectionA_Data(pray);
        SectionB_Bucket(pray);
        SectionC_Judge(pool, pray);
        SectionD_RealDamage(pray);

        Summary();
    }

    // ==================================================================
    //  A —— 数据形状（前提：这四样不成立，下面几条测的就是别的东西）
    // ==================================================================
    static void SectionA_Data(CardDef pray)
    {
        Section("A 数据形状：附魔卡 + 数据位有 60/31、没有 50；桶名常量没被改名");

        CheckTrue(pray.IsEnchantment, "A1 前提：`Pray Enchantment` 真的是附魔卡（`IsEnchantment`）");
        Check(pray.AbilityTriggerPresence("UnitDamaged"), 1,
              "A2 前提：数据位 `UnitDamaged(60)` 在（`AbilityTriggerPresence == 1`；判据 `CardAbility__CanTriggerAbility.c:71`）");
        Check(pray.AbilityTriggerPresence("TurnStartPlayer"), 1,
              "A3 前提：`TurnStartPlayer(31)` 也在（`Pray_Success` 那一行 —— ⛔ 它**不归**本笔，本笔只做 60）");
        Check(pray.AbilityTriggerPresence("UnitAttack"), 0,
              "A4 反面：`UnitAttack(50)` **明确没有**（= 0 不是 -1）");
        CheckTrue(string.Equals(VfxMap.UnitDamaged, "unit_damaged_anim", StringComparison.Ordinal),
                  "A5a 桶名常量：`VfxMap.UnitDamaged == \"unit_damaged_anim\"`（本笔消费的那个桶）");
        CheckTrue(string.Equals(VfxMap.UnitDamagedTriggerName, "UnitDamaged", StringComparison.Ordinal),
                  "A5b 判据名常量：`VfxMap.UnitDamagedTriggerName == \"UnitDamaged\"`（`CardAnimTime.tn` 的那个拼写）");

        int rows = 0;
        if (pray.AnimTimes != null)
            for (int i = 0; i < pray.AnimTimes.Count; i++)
            {
                var t = pray.AnimTimes[i];
                if (t != null && t.Anim == "Pray_Exit" && string.Equals(t.TriggerName, "UnitDamaged", StringComparison.Ordinal))
                    rows++;
            }
        Check(rows, 1,
              "A6 数据：`animTimes` 里恰好一行 `a:\"Pray_Exit\", tn:\"UnitDamaged\"`"
            + "（`cards_engine.json` 的 `tg:60`；这条是「动画来自**附魔卡自己**的 `cardAnimations[]`」的判据）");
    }

    // ==================================================================
    //  B —— 桶：`unit_damaged_anim` 有、`hit` **恒空**（= 施工单那一版为什么走不通）
    // ==================================================================
    static void SectionB_Bucket(CardDef pray)
    {
        Section("B 桶：`Pray_Exit` 在 `VfxMap.UnitDamaged` 里；`VfxMap.Hit` 那一路**恒空**（施工单那一版不可用）");

        var got = VfxMap.ResolveAllTimed(VfxMap.UnitDamaged, pray.Faction, pray.Name, pray.AnimTimes,
                                         null, perCardOnly: true);
        Check(got.Length, 1,
              "B1 ★★ 生产解释路径（`perCardOnly:true`）在 `unit_damaged_anim` 桶里给 **1** 件");
        bool named = got.Length == 1 && got[0].Name == "Pray_Exit" && got[0].Anim == "Pray_Exit";
        CheckTrue(named, "B1b ★★★ 而且就是 `Pray_Exit`（prefab 名与 `CardAnim` 名都对）"
                       + "｜🧨 删掉 `card_vfx_by_card.json` 里 `9218576f…` 的那一行 ⇒ 0 件 ⇒ 红");

        var oldBucket = VfxMap.ResolveAllTimed(VfxMap.Hit, pray.Faction, pray.Name, pray.AnimTimes,
                                               null, perCardOnly: true);
        Check(oldBucket.Length, 0,
              "B2 ★★★ **本笔对施工单的订正**：`VfxMap.Hit`（\"hit\"）这一路对附魔卡**恒回 0 件** —— "
            + "`VfxMap.cs:1263-1271` 把所有逐卡 `hit` 行【改归】`unit_damaged_anim`（2026-10-23 `T1`）"
            + "｜🧨 把那次「搬桶」改回「抄桶」（旧桶也留一份）⇒ 这里变 1 ⇒ 红");

        var clips = CardAnimClips.Resolve(VfxMap.UnitDamaged, pray.Faction, pray.Name, pray.AnimTimes);
        Check(clips.Length, 1, "B3 拍数：`CardAnimClips.Resolve(...)` 给 1 拍（`PlayUnitSkillAnim` 消费的那个口）");
        if (clips.Length == 1)
        {
            CheckTrue(clips[0].Prefab == "Pray_Exit", "B3b 这一拍的 prefab = `Pray_Exit`");
            var p = clips[0].Place;
            CheckTrue(p.StartPos == 0 && p.EndPos == 0 && p.PointTowards == 0,
                      "B3c ★★ 锚点 = **起始位自己**（`sp/ep/pt` 全 0；原版 `AbilityLogic.GetsPos` 的 `self` 那一档）"
                    + " ⇒ 所以 `ViewAt(载体)` 就是对的位置，⛔ 别按「朝目标飞」去改");
            CheckTrue(!p.Move, "B3d 且 `shouldMoveVFX == false`（不是位移件）");
        }
    }

    // ==================================================================
    //  C —— 判据纯函数（`BattleDriver.IsEnchantUnitDamagedTrigger`）
    // ==================================================================
    static void SectionC_Judge(List<CardDef> pool, CardDef pray)
    {
        Section("C 判据纯函数：`IsEnchantUnitDamagedTrigger` 的四道闸逐条灭自证");

        CheckTrue(BattleDriver.IsEnchantUnitDamagedTrigger(Trig(pray.Name, "UnitDamaged"), pool),
                  "C1 ★★★ 真信号形状（`Kind=Trigger` · `CardId=附魔卡名` · `Keyword=UnitDamaged`）⇒ **真**"
                + "｜🧨 删掉 `IsEnchantment` / `AbilityTriggerPresence == 1` 任一条 ⇒ 红");

        CheckTrue(!BattleDriver.IsEnchantUnitDamagedTrigger(Trig(pray.Name, "TurnStartPlayer"), pool),
                  "C2 ★★ `Pray_Success` 那一拍（`TurnStartPlayer(31)`）**不归这里**"
                + "｜🧨 去掉关键词那一道闸 ⇒ 红（会在回合起也播 `Pray_Exit`）");

        CheckTrue(BattleDriver.IsEnchantUnitDamagedTrigger(Trig(pray.Name, "UnitDamaged#1"), pool),
                  "C3 ★★ 带能力序号后缀（`UnitDamaged#1`）⇒ 仍然**真**（先剥后缀再归一）"
                + "｜🧨 把 `SplitKeywordAbility` 那一步删掉 ⇒ 红");

        var asHit = Trig(pray.Name, "UnitDamaged"); asHit.Kind = EvtKind.Hit;
        CheckTrue(!BattleDriver.IsEnchantUnitDamagedTrigger(asHit, pool),
                  "C4 ★ `Kind=Hit` 的事件**不**走这一跳（本笔的锚点是派发侧那条 `Trigger`）");

        // 单位自己的 `UnitDamaged` 归 `PlayDefenderDamagedAnim`（攻击那条路）⇒ ⛔ 这里不许再播一次。
        CardDef unit = null;
        for (int i = 0; i < pool.Count && unit == null; i++)
        {
            var c = pool[i];
            if (c != null && !c.IsEnchantment && c.AbilityTriggerPresence("UnitDamaged") == 1) unit = c;
        }
        CheckTrue(unit != null, "C5a 前提：池里找得到一张「**非**附魔、且数据位有 60」的卡（用来测反面）");
        if (unit != null)
            CheckTrue(!BattleDriver.IsEnchantUnitDamagedTrigger(Trig(unit.Name, "UnitDamaged"), pool),
                      "C5b ★★★ 单位的 `UnitDamaged` **不归本跳**（`" + unit.Name + "`）—— 那一路归 `PlayDefenderDamagedAnim`"
                    + "，这里再播一次就是**原版没有的第二下**｜🧨 删掉 `IsEnchantment` 闸 ⇒ 红");

        CheckTrue(!BattleDriver.IsEnchantUnitDamagedTrigger(Trig(pray.Name, "UnitDamaged"), new List<CardDef>()),
                  "C6 ★ 卡在池里查不到（空池）⇒ **假**（⛔ 不猜、不静默当成有）");
    }

    // ==================================================================
    //  D —— 真伤害链：事件次序 + `ActiveEnchantments` 已空（本笔锚点选择的硬证据）
    // ==================================================================
    static void SectionD_RealDamage(CardDef pray)
    {
        Section("D 真伤害：受伤那一刻引擎发了什么、以及为什么不能读 `ActiveEnchantments`");

        UnitState carrier;
        var ctx = MakeBattle(out carrier);
        CheckTrue(carrier != null, "D1 前提：载体上场了（0 号玩家 · 格 3）");
        if (carrier == null) { Summary(); return; }

        var eff = Enchantments.Attach(ctx, carrier.Instance, pray, Enchantments.BuffTypeSingleUse);
        CheckTrue(eff != null, "D2 前提：附魔挂上了（`Enchantments.Attach` 是唯一挂载口）");
        CheckTrue(Enchantments.FindPray(carrier) != null, "D2b 前提：`FindPray` 认得它（排队摘靠它找）");

        ctx.ClearSignals();
        int dealt = RuleCore.HurtForTest(ctx, carrier, 1, "B109E7");
        CheckTrue(dealt >= 1, "D3 前提：真扣到了血（`HurtForTest` 返回 " + dealt + "）");

        Check(Count(ctx, EvtKind.Hit, e => e.Amount > 0 && string.Equals(e.CardId, carrier.Name, StringComparison.Ordinal)), 1,
              "D4 ★ 表现侧挨了 1 条 `EvtKind.Hit`（`Amount > 0`、`CardId` = 载体名）");

        // 🔴 本笔最关键的实证：**引擎调用返回时，附魔已经不在 `ActiveEnchantments` 里了**。
        int left = carrier.Instance != null && carrier.Instance.ActiveEnchantments != null
                 ? carrier.Instance.ActiveEnchantments.Count : -1;
        Check(left, 0,
              "D5 ★★★ **施工单那一版为什么不能用**：`HurtForTest` 返回时载体身上的 `ActiveEnchantments` 已经是 **0** 条"
            + "（`EnqueueRemovePray` 排队摘，泵点 = 外层 `ApplyDamage` 的 `ActionScope` 退出，`RuleCore.cs:8623`）"
            + " ⇒ 「在 `PlaySignal` 那一刻遍历 `ActiveEnchantments`」**恒读空** ｜🧨 把排队摘改回同步/改掉泵点 ⇒ 这里变 1 ⇒ 红（并且说明可以改回施工单那版）");

        Check(Count(ctx, EvtKind.EnchantmentRemoved, e => string.Equals(e.Effect, pray.Name, StringComparison.Ordinal)), 1,
              "D6 ★ 摘除信号在（`EvtKind.EnchantmentRemoved` · `Effect` = 附魔卡名 —— 它是「摘了」唯一的窄口，"
            + "但**不区分**是哪条 trigger 摘的，所以本笔没有拿它当锚点）");

        int trig = Count(ctx, EvtKind.Trigger, e => string.Equals(e.CardId, pray.Name, StringComparison.Ordinal));
        Check(trig, 1,
              "D7 ★★★ **本笔的锚点**：这一下恰好来了 1 条 `EvtKind.Trigger(CardId = 附魔卡名)`"
            + "（`B109_E6` 的派发侧；`Keyword` 里带 `UnitDamaged`）｜🧨 删掉 `FireEnchantmentAbilitiesByNumber` 里的 "
            + "`EmitCardTrigger` ⇒ 0 条 ⇒ 红（本笔的表现侧会**静默不播**）");

        BattleEvent anchor = null;
        if (ctx.Signals != null)
            for (int i = 0; i < ctx.Signals.Count; i++)
            {
                var e = ctx.Signals[i];
                if (e != null && e.Kind == EvtKind.Trigger && string.Equals(e.CardId, pray.Name, StringComparison.Ordinal))
                { anchor = e; break; }
            }
        CheckTrue(anchor != null, "D8 前提：取到了那条锚点事件");
        if (anchor != null)
        {
            CheckTrue(anchor.Player == 0 && anchor.Slot == 3,
                      "D8b ★ 锚点 = **载体**（`Player/Slot` = 0/3；原版 `AbilityData+0x10` = thisCard = 载体）");
            var loadedPool = CardDatabase.Load();
            CheckTrue(BattleDriver.IsEnchantUnitDamagedTrigger(anchor, loadedPool),
                      "D8c ★★★ 生产判据对**真事件**判真 ⇒ `PlayEnchantUnitDamagedAnim` 会走进去"
                    + "（C 组是合成事件、这一条是引擎真发出来的那条）");
        }
    }

    // ==================================================================
    //  夹具 / 读数
    // ==================================================================

    /// <summary>合成一条 `EvtKind.Trigger`（只填判据要看的四位）。</summary>
    static BattleEvent Trig(string cardId, string keyword)
    {
        return new BattleEvent { Kind = EvtKind.Trigger, Player = 0, Slot = 3, CardId = cardId, Keyword = keyword };
    }

    /// <summary>一场「督军 + 一张单位」的干净对局；返回时 <paramref name="carrier"/> = 已摆到 0 号玩家格 3 的载体。</summary>
    static BattleContext MakeBattle(out UnitState carrier)
    {
        var heroA = new CardDef("B109E7HeroA", "B109E7HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B109E7HeroB", "B109E7HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var unitCard = new CardDef("B109E7Carrier", "B109E7Carrier", "unit", "", null, "Test", 1, 1, 5, 0, null);
        var foeCard = new CardDef("B109E7Foe", "B109E7Foe", "unit", "", null, "Test", 1, 1, 5, 0, null);
        var ctx = RuleCore.NewBattle(new List<CardDef> { heroA, unitCard },
                                     new List<CardDef> { heroB, foeCard },
                                     seed: 0, shuffle: false, cardPool: new List<CardDef>());
        carrier = PlaceUnit(ctx, 0, 3, unitCard);
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

    static int Count(BattleContext ctx, EvtKind kind, Func<BattleEvent, bool> pred)
    {
        int n = 0;
        if (ctx == null || ctx.Signals == null || pred == null) return 0;
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e == null || e.Kind != kind) continue;
            if (pred(e)) n++;
        }
        return n;
    }

    // ==================================================================
    //  计数 / 打印（本件自带，⛔ 不依赖任何宿主）
    // ==================================================================
    static void Section(string title) { Debug.Log("[B109_E7] ── " + title); }

    static void Check(int actual, int expected, string msg)
    {
        if (actual == expected) { _pass++; Debug.Log("[B109_E7]   ✓ " + msg + "（实际 " + actual + "）"); }
        else { _fail++; Debug.LogError("[B109_E7]   ✗ " + msg + " —— 期望 " + expected + "，实际 " + actual); }
    }

    static void CheckTrue(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("[B109_E7]   ✓ " + msg); }
        else { _fail++; Debug.LogError("[B109_E7]   ✗ " + msg); }
    }

    static void Summary()
    {
        Debug.Log("[B109_E7] === 合计：通过 " + _pass + " / 失败 " + _fail + " ===");
    }
    // 🆕 2026-10-31（B109 · 调度台接线）：宿主聚合行读这两个口（追加到类尾、零行漂）。
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
