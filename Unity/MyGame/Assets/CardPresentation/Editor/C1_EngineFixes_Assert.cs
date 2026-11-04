// C1_EngineFixes_Assert.cs — 第一百零九会话 · C 组 · 执行代理 `C1` 的断言
//
// ============================ 它盯的是什么 ============================
// 本笔在引擎侧落了四件（判据与改动清单见 `资料/完全复刻/报告/C1.md`）：
//   ① **`poisoned` 的通用 `addTrait` 钩子** —— 原版把 `BroadcastUnitPoisoned` 挂在【通用 addEffect】上
//      （`decomp_full/BattleManager__ResolveAddEffect.c:131` 闸 `buffType(+0x38) == 3` ∧ `:180` 闸 `trait(+0x3c) == 200`
//      ⇒ `:268` 调用；🔴 2026-11-05 `B143-W3` 就地订正：原文写「闸 `buffType == 200`」是**误读**，`+0x3c` 是 trait 不是 buffType），
//      我们改前只在 `poisonous` 关键词那一支里播（`RuleCore.cs:16307`）⇒ 效果文本给出的
//      `give … poisoned` 原版会播、我们不会。落点 = `EffectResolver.ApplyOneGain` 的 `addTrait` 正支。
//   ② **0 伤 / 盾挡不派 60**（核实项，**零代码改动**）—— 原版 `CardScript__ResolveDamageDealt.c:31/:34`
//      两道早退（`damageAbsorbed` / `damageValue == 0`）；我们这一侧的等价闸早就在
//      `RuleCore.EmitHit`（`RuleCore.cs:9534` 的 `absorbed || amount == 0`，2026-10-24 `A20`）⇒ 本组把它**钉住**。
//   ③ **`TriggerOpsByNumber` 的数据半已挂**（`Data/CardDatabase.cs:1749`）—— 报告 `B109_R19` 之外的顺手项，
//      也是本笔订正 `CardDefPartial_B96CA.cs` 头注那句话的判据。
//   ④ **`backlash` / `penitence` 的 关键词→trigger 名 映射缺口**（本笔**没改**：落点在 `ActionPump.cs`、白名单外，
//      已写补丁交回）—— 本组只钉「缺口的影响面真的存在」这一层前提。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Run()` 是唯一入口；宿主接一个 `{ C1_EngineFixes_Assert.Run(); }` 即可（本文件**不改**任何宿主）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A6：把 `ApplyOneGain` 新钩子那半句删掉**不会**红（它只影响广播、不影响关键词落地）——
//     这正是本组「只能锁到闸的前一半」的如实边界（见报告 §没查清）。
//     🆕 **2026-11-05（`B143-W3`）就地补上另一半**：A2 组（A14/A15）用「带 290 正文的听众」观测广播 ⇒
//     删钩子 / 把 `DefinedTraitNumber == 200` 改错 ⇒ **A14 红**；A5–A7 仍只锁「关键词落地」这一半（如实记）。
//   · B6：把 `RuleCore.cs:9534` 的 `|| amount == 0` 删掉 ⇒ 广播会被走到；但池内今天 0 张卡带 60 正文
//     ⇒ 事件数仍是 0。本条的判别式在报告里写清（**断言不是万能尺**，⛔ 别拿它当「闸一定在」的证明）。
//   · C1：把 `CardDatabase.cs:1749` 的 `AttachTriggerOpsByNumber` 那一跳删掉 ⇒ `HasTriggerOpsByNumber` 全 false ⇒ 红。
//     🆕 2026-10-31（`B110_X2`）：原来这里的阈值写的是 `known > 500`，**凭空估的、从未成立过**
//     （现读真值 = 163 = 已挂表 44 + 列空 119；判据 → `判据/B110_X1_六条红诊断.md` §4）。
//     已改成 `known > 0`，判别力**一点没减**（删那一跳 ⇒ 0 ⇒ 红），并补 `C1b` 钉住「已挂表 / 列空 两档都非空」。
//   · D1/D2：卡表一改（那 6 / 5 张的关键词或 abilityTriggers 变了）⇒ 数字变 ⇒ 红（提示回来重核影响面）。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>C1 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>；⛔ 不改任何宿主）。</summary>
public static class C1_EngineFixes_Assert
{
    static int _fail, _pass;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _fail = 0; _pass = 0;
        Section("🆕 C1：引擎四件（poisoned 通用钩子 / 0 伤不派 60 / 数据半 / backlash·penitence 映射前提）");

        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[C1] `CardDatabase.Load()` 抛了：" + e.Message); }
        CheckTrue(pool != null && pool.Count > 500, "A0 前提：卡池读得到且规模正常（≥ 500 张）");

        TestPoisonedHook();
        TestPoisonBroadcastFromGive();   // 🆕 2026-11-05（`B143-W3`）：补 A 组缺的那条灭自证 —— `give poisoned` **真的触发了广播**
        TestZeroDamageNoTrigger();
        if (pool != null && pool.Count > 500)
        {
            TestTriggerOpsDataHalf(pool);
            TestBacklashPenitencePremise(pool);
        }
        else
        {
            CheckTrue(false, "C0/D0 前提：卡池可用（不可用 ⇒ C/D 两组**没跑**，别当成通过）");
        }

        Summary();
    }

    // ==================================================================
    //  A 组：`poisoned` 的通用 `addTrait` 钩子（原版 `ResolveAddEffect.c:180/:268`）
    // ==================================================================

    static void TestPoisonedHook()
    {
        Section("A `poisoned` 的通用 `addTrait` 钩子（闸 = 原版 `buffType(+0x38)==3` ∧ `trait(+0x3c)==200`）");

        Check(CardDef.DefinedTraitNumber(KeywordTable.Poisoned), 200,
              "A1 前提：`DefinedTraitNumber(\"poisoned\") == 200` —— 生产代码就是拿它比原版那道 `buffType == 200` 的闸"
            + "｜🧨 改比别的数（或字符串比较写错大小写）⇒ 红");
        Check(KeywordTable.Poisoned, "poisoned",
              "A2 前提：引擎关键词名逐字是 `poisoned`（`DefinedTraitNumberOfName` 是**大小写敏感**的 `Ordinal` 表）");

        UnitState u;
        BattleContext ctx = MakeBattle(out u);
        CheckTrue(ctx != null && u != null, "A3 前提：夹具单位摆上了（0 号玩家格 3）");
        if (ctx == null || u == null) return;
        CheckTrue(!u.Has(KeywordTable.Poisoned), "A4 前提：夹具单位一开始**没有** `poisoned`");

        // 🆕 2026-10-31（`B110_X2` · 判据 `判据/B110_X1_六条红诊断.md` §1）：**直接钉词表**，比走 `ResolveOps` 更近。
        //    这是 A5-A7 三条的**共同前提**；`GiveKw` 里那行一删 ⇒ 本条先红，省得从 A5 的 `unresolved` 倒推。
        var pv = GivePayload.Parse("poisoned");
        CheckTrue(pv != null && pv.Count == 1 && pv[0].Keyword == KeywordTable.Poisoned,
                  "A8 ★ `poisoned` 在载荷词表里（`GivePayload.Parse(\"poisoned\")` ⇒ 恰一条 Keyword=poisoned）"
                + "｜🧨 从 `GiveKw` 删掉那一行 ⇒ null ⇒ 红（这条是 A5-A7 的**前提**，先红它）");

        var ops = new List<EffectOp>
        {
            new EffectOp { Verb = "give", Payload = "poisoned", Source = "C1 夹具" },
        };
        List<string> unresolved;
        RuleCore.ResolveOps(ctx, 0, null, ops, null, out unresolved);

        CheckTrue(unresolved == null || unresolved.Count == 0,
                  "A5 前提：载荷认得（`unresolved` 为空 —— 否则这条 `give poisoned` 根本没生效）");
        CheckTrue(u.Has(KeywordTable.Poisoned),
                  "A6 ★★ 效果层真路（`ResolveOps` 的 `give poisoned`）把 `poisoned` **挂到了目标身上** ——"
                + "这正是新钩子的闸 `appliedTrait != null` 成立的那一格（原版 `local_198 != 0`）");
        CheckTrue(u.KwValue(KeywordTable.Poisoned) > 0,
                  "A7 ★ 而且是**正的**层数（`val > 0` ⇒ 走 `ApplyTraitEffect` 记录原语，不是空给）");
    }

    // ==================================================================
    //  A2 组（🆕 2026-11-05 · `B143-W3`）：`give poisoned` **真的触发了 `BroadcastUnitPoisoned`**
    //  为什么补：A5–A7 只断「关键词挂上了」—— **把 `EffectResolver.cs:12322` 那句钩子整句删掉，它们仍然全绿**
    //    ⇒ 钩子本身没有灭自证。本条用一张**带 290 正文的听众**去观测广播：
    //    原版 `ResolveAddEffect.c:131`（`buffType(+0x38)==3`）+ `:180`（`trait(+0x3c)==200`）⇒ `:268` 调广播。
    //  判别式 = 把 `DefinedTraitNumber(p.Keyword) == 200` 那句改错（或整句删掉）⇒ A14/A15 红。
    // ==================================================================
    static void TestPoisonBroadcastFromGive()
    {
        Section("A2 `give poisoned` ⇒ 真的广播 `UnitPoisoned`（钩子的灭自证）");

        var seg = EffectText.ParseSegment("Draw 1 card");
        CheckTrue(seg.Kind == EffectText.SegKind.Ok && seg.Ops != null && seg.Ops.Count > 0,
                  "A10 前提：`Draw 1 card` 解析得出 op（拿它给 listener 造一条 290 正文）");
        if (seg.Kind != EffectText.SegKind.Ok || seg.Ops == null || seg.Ops.Count == 0) return;

        var givePoison = new List<EffectOp> { new EffectOp { Verb = "give", Payload = "poisoned", Source = "C1 广播夹具" } };

        // ---- ① 负对照：场上**没有 290 听众** ⇒ give 不产生任何 `UnitPoisoned` 触发 ----
        UnitState victimN;
        var ctxN = MakeBattle(out victimN);
        CheckTrue(ctxN != null && victimN != null, "A11 前提：负对照的局面建出来了");
        if (ctxN == null || victimN == null) return;
        List<string> unresolvedN;
        RuleCore.ResolveOps(ctxN, 0, null, givePoison, null, out unresolvedN);
        CheckTrue(victimN.Has(KeywordTable.Poisoned),
                  "A11b 前提：负对照里 `poisoned` 照样挂上了（否则本条测的是别的）");
        Check(CountPoisonTriggers(ctxN), 0,
              "A12 ★ **没有 290 听众 ⇒ 一条 `UnitPoisoned` 触发都不发**"
            + "（`give` 本身只发 `StatusApplied`，不是触发）");

        // ---- ② 正对照：一张**带 290 正文**的听众在场 ⇒ give 之后它必须被触发 ----
        UnitState victimP;
        var ctxP = MakeBattle(out victimP);
        CheckTrue(ctxP != null && victimP != null, "A13 前提：正对照的局面建出来了");
        if (ctxP == null || victimP == null) return;
        var listener = PlaceUnit(ctxP, 1, 6, new CardDef("C1Listen", "C1Listen", "unit", "", null, "Test", 1, 1, 5, 0, null));
        CheckTrue(listener != null, "A13b 前提：听众摆上了（1 号玩家格 6）");
        if (listener == null) return;
        listener.GrantOps("UnitPoisoned", seg.Ops, "Draw 1 card");
        CheckTrue(RuleCore.HasUnitPoisonedBodyForTest(listener),
                  "A13c 前提：听众身上**有** 290 正文（`GrantOps` 之后；= 原版 `OnTrigger(290)` 的接收端）");

        List<string> unresolvedP;
        RuleCore.ResolveOps(ctxP, 0, null, givePoison, null, out unresolvedP);
        CheckTrue(victimP.Has(KeywordTable.Poisoned),
                  "A13d 前提：正对照里 `poisoned` 挂上了（= 钩子的闸 `appliedTrait != null` 成立）");
        int got = CountPoisonTriggers(ctxP);
        CheckTrue(got >= 1,
                  $"A14 ★★ **`give poisoned` ⇒ 带 290 正文的听众被触发**（`EvtKind.Trigger(UnitPoisoned)` +{got}）"
                + "｜🧨 把 `EffectResolver.cs:12322` 那句钩子删掉（或把 `DefinedTraitNumber(p.Keyword) == 200` 改错）"
                + "⇒ 这里 **+0** ⇒ 红（A5–A7 只锁「关键词落地」、锁不到这一跳）");
        Check(got, 1,
              "A15 ★ 这一次 `give` **恰好触发 1 条** —— 不双播（关键词路 `RuleCore.cs:16308` 走 `ApplyTraitEffect`、"
            + "⛔ 不经过 `ApplyOneGain`，所以不会与本钩子各播一遍）"
            + "｜🧨 把钩子挪进关键词那一支 / 两条路都接 ⇒ 这里变 2 ⇒ 红");
    }

    /// <summary>数这个局面里 `EvtKind.Trigger` 且 `Keyword == UnitPoisoned` 的条目
    /// （`ctx.ActionLog` = `Emit` 的留档副本、不会被 `DrainSignals` 搬走）。</summary>
    static int CountPoisonTriggers(BattleContext ctx)
    {
        int n = 0;
        if (ctx == null || ctx.ActionLog == null) return 0;
        for (int i = 0; i < ctx.ActionLog.Count; i++)
        {
            var e = ctx.ActionLog[i];
            if (e != null && e.Kind == EvtKind.Trigger && e.Keyword == "UnitPoisoned") n++;
        }
        return n;
    }

    // ==================================================================
    //  B 组：0 伤 / 盾挡 ⇒ 表现层仍发 Hit(0)，但不派 60（原版 `ResolveDamageDealt.c:31/:34`）
    // ==================================================================

    static void TestZeroDamageNoTrigger()
    {
        Section("B 0 伤 / 盾挡 ⇒ `EvtKind.Hit(Amount=0)` 照发，但**不派** 60/185/187");

        UnitState u;
        BattleContext ctx = MakeBattle(out u);
        CheckTrue(ctx != null && u != null, "B1 前提：夹具单位摆上了（0 号玩家格 3）");
        if (ctx == null || u == null) return;

        u.AddKeyword(KeywordTable.Shield, 1);
        CheckTrue(u.HasShield, "B2 前提：单位带着 `shield`（`HasShield` = `Has(\"shield\")` 的派生属性）");

        int hp0 = u.Health;
        ctx.ClearSignals();
        int dealt = RuleCore.HurtWithSourceForTest(ctx, u, 5, "C1 夹具");

        Check(dealt, 0, "B3 盾挡下实际伤害 = 0（`DamageAfterReduction` 第一句 `HasShield ⇒ return 0`）");
        Check(u.Health, hp0, "B4 生命一点没掉");

        int hits = 0, zeroHits = 0, triggers = 0;
        if (ctx.Signals != null)
        {
            for (int i = 0; i < ctx.Signals.Count; i++)
            {
                var e = ctx.Signals[i];
                if (e == null) continue;
                if (e.Kind == EvtKind.Hit) { hits++; if (e.Amount == 0) zeroHits++; }
                else if (e.Kind == EvtKind.Trigger) triggers++;
            }
        }
        Check(hits, 1, "B5 ★ 表现层仍收到**恰 1 条** `EvtKind.Hit`（盾碎 / 闪过要有反馈 —— 原版 `EvtKind.Hit` 那一格**不受**那两道早退影响）");
        Check(zeroHits, 1, "B6 ★ 而且那条的 `Amount == 0`（= 原版 `damageValue == 0` / `damageAbsorbed` 那一档）");
        Check(triggers, 0,
              "B7 ★★ 盾挡这一下**没有任何** `EvtKind.Trigger` —— 对应原版 `ResolveDamageDealt.c:31/:34` 的"
            + "「absorbed / damageValue==0 ⇒ 直接 return」，等价闸 = `RuleCore.cs:9534` 的 `absorbed || amount == 0`"
            + "（2026-10-24 `A20`）｜⚠️ 池内今天 **0 张**卡带 60 正文 ⇒ 本条同时成立也是必然，判别式见报告 §没查清");
    }

    // ==================================================================
    //  C 组：`TriggerOpsByNumber` 的数据半【已挂】（`Data/CardDatabase.cs:1749`）
    // ==================================================================

    static void TestTriggerOpsDataHalf(List<CardDef> pool)
    {
        Section("C `TriggerOpsByNumber` 的数据半已挂（订正 `CardDefPartial_B96CA.cs` 头注那句「还没落列 ⇒ 休眠」）");

        var probes = new[] { 0, 60, 80, 120, 130, 145, 200, 220, 300, 410, 580, 646, 715, 720 };
        int known = 0, withOps = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            if (c.HasTriggerOpsByNumber) known++;
            for (int k = 0; k < probes.Length; k++)
            {
                var ops = c.TriggerOpsByNumber(probes[k]);
                if (ops != null && ops.Count > 0) { withOps++; break; }
            }
        }
        // 🆕 2026-10-31（`B110_X2` · 判据 `判据/B110_X1_六条红诊断.md` §4）：阈值原来是凭空估的 `> 500`，
        //   **从未成立过** —— 现读真值 = 163（= 已挂表 44 + 列空 119，与同趟 `[triggerOpsByNumber]` 汇总逐位吻合）。
        //   ⛔ 别写死 163 / 别改回 500 附近（卡表一改就假红；覆盖率为 44 是**另一件事**，见 X1 报告「顺手发现 1」）。
        //   判别力靠的是 `Attached > 0`：删掉 `CardDatabase.cs:1749` 那一跳 ⇒ `known = 0` ⇒ 两条一起红。
        CheckTrue(known > 0,
                  "C1 ★★ `HasTriggerOpsByNumber` 为真的卡 = " + known + " / " + pool.Count
                + "（= 已挂表 " + CardDatabase.TriggerOpsByNumberAttached
                + " + 列空 " + CardDatabase.TriggerOpsByNumberEmptyColumn + "；"
                + "`AttachTriggerOpsByNumber` 没挂时**恒 false** ⇒ 本条红）"
                + "｜🧨 删掉 `def.AttachTriggerOpsByNumber(MapTriggerOpsByNumber(def))` ⇒ 0 ⇒ 红");
        CheckTrue(CardDatabase.TriggerOpsByNumberAttached > 0 && CardDatabase.TriggerOpsByNumberEmptyColumn > 0,
                  "C1b ★ 三态里的两档都**真的有卡**（已挂表 " + CardDatabase.TriggerOpsByNumberAttached
                + " · 列空 " + CardDatabase.TriggerOpsByNumberEmptyColumn + "）"
                + "｜🧨 把 `rows.Length == 0` 那档并进「不挂」⇒ 列空 = 0 ⇒ 红；"
                + "把「一条都归不了位」改成挂空表 ⇒ 已挂表 ≥ 119 ⇒ 与列空的口径分家");
        CheckTrue(withOps > 0,
                  "C2 ★ 至少 " + withOps + " 张卡能从 `TriggerOpsByNumber(n)`（n ∈ " + probes.Length + " 个探测值）取到正文 —— 数据半不是空表"
                + "｜🧨 组装口 `MapTriggerOpsByNumber` 全返回空 ⇒ 0 ⇒ 红");
        CheckTrue(pool.Count > 0 && pool[0] != null,
                  "C3 前提：池里第一张卡非空（C1/C2 的分母不是 0）");
    }

    // ==================================================================
    //  D 组：`backlash` / `penitence` 映射缺口的**影响面**（item 3 前提；本笔未落地，补丁已交回）
    // ==================================================================

    static void TestBacklashPenitencePremise(List<CardDef> pool)
    {
        Section("D `backlash`→`ThisUnitDeath(80)` / `penitence`→`Rage(130)` 映射缺口的真实影响面");

        int bl = 0, bl80 = 0, pe = 0, pe130 = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            if (c.Has(KeywordTable.Backlash))
            {
                bl++;
                var rows = c.TriggerAbilityRowsOf(80);
                if (rows != null && rows.Length > 0) bl80++;
            }
            if (c.Has(KeywordTable.Penitence))
            {
                pe++;
                var rows = c.TriggerAbilityRowsOf(130);
                if (rows != null && rows.Length > 0) pe130++;
            }
        }
        CheckTrue(bl > 0 && bl80 == bl,
                  "D1 ★ `backlash` 卡 " + bl + " 张、其中真有 **`trigger 80 (ThisUnitDeath)`** 数据行的 " + bl80 + " 张 ——"
                + "`ActionPump.TriggerNameOfKeyword(\"backlash\")` 返回 null 时，这 " + bl80 + " 行在"
                + " `Enchantments.Matching` / `AbilityContextOfAbilityV14` 里**永远命不中**（原版走 `ThisUnitDeath`："
                + "`BattleManager._ResolveBacklash_d__451__MoveNext.c:94` 的 `CardScript__TriggerUnitDeathActions`）");
        CheckTrue(pe > 0 && pe130 == pe,
                  "D2 ★ `penitence` 卡 " + pe + " 张、其中真有 **`trigger 130 (Rage)`** 数据行的 " + pe130 + " 张 —— 同上"
                + "（原版 penitence 那一刻发的是 `RawCardScript__OnTrigger(… 0x82 = 130 Rage …)`，`CardScript__ResolveDamageDealt.c:98-100`）");
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>一场「督军 + 一张单位」的干净对局；返回时 <paramref name="unit"/> = 已摆到 0 号玩家格 3 的单位。</summary>
    static BattleContext MakeBattle(out UnitState unit)
    {
        var heroA = new CardDef("C1HeroA", "C1HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("C1HeroB", "C1HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var unitCard = new CardDef("C1Unit", "C1Unit", "unit", "", null, "Test", 1, 1, 5, 0, null);
        var foeCard = new CardDef("C1Foe", "C1Foe", "unit", "", null, "Test", 1, 1, 5, 0, null);
        var ctx = RuleCore.NewBattle(new List<CardDef> { heroA, unitCard },
                                     new List<CardDef> { heroB, foeCard },
                                     seed: 0, shuffle: false, cardPool: new List<CardDef>());
        unit = PlaceUnit(ctx, 0, 3, unitCard);
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
        if (_fail == 0) Debug.Log($"C1 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"C1 合计：{_pass} 通过 / {_fail} 失败");
    }

    // 🆕 2026-10-31（B109 · 调度台接线）：宿主聚合行读这两个口（类尾、零行漂）。
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
