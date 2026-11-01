// B112W_B127_Assert.cs — 执行代理 `B112W`（2026-11-01 · 活状态表 `B-127` 的 S1 / S2 验收）
//
// ============================ 它钉的是哪两件事 ============================
//  **S1**（`B-127` S1 · `UnitState.TempBuff.TargetOwner`）
//    原版判据：`d:/2/tools/decomp_full/CardScript__OnTurnSetup.c:40` —— `param_1` = 调 `OnTurnSetup` 的**那张卡**
//    （= 持效果的那张卡），`+0x40` 是**它自己**的 `isPlayer` ⇒ `+0x33`（`until your next turn`）的到期点
//    **按持卡人判**，⛔ 不是按施放者。改前我们比的是 `Owner`（= 施放者）⇒ 跨阵营限时减益会**多撑整整一个回合**。
//    · **A 组**：跨阵营（A 给 B 的单位挂 `until your next turn`）⇒ 在**持卡人（B）**的回合开始撤。
//    · **B 组**（灭自证反向 · 对照）：自增益（目标是自己人）⇒ `TargetOwner == Owner`，行为**与改前一致**。
//  **S2**（`B-127` S2 · bit2 `untilEnemyTurnStart`）
//    原版判据：`CardScript__OnTurnSetup.c:71` → `:76` → `:83`（`+0x34` 在**施放者对面**的回合开始撤）。
//    池内唯一数据行 = `Worst Temper`：`armour` 那一格是 `untilEnemyTurnStart`，而卡面写 `this turn`
//    （`cards_raw.json` 的 `armourChange 1 + untilEnemyTurnStart`）⇒ **照数据不照卡面**，且**只换 armour 那一项**。
//    · **C 组**：解析层前置（`B-186`：`[Shield] Armour 1` 现在解成 `armour`，⛔ 不是 `shield` 关键词）。
//    · **D 组**：`armour` 是 `UntilOpponentNextTurn`、`attack` / `flying` **不是**；再接回合边界**三档**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · **A 组**：把 `TempBuff.IsDueAtTurnStart` 改回 `UntilMyNextTurn && Owner == turnPlayer`（= 不看 `TargetOwner`）
//    ⇒ 「B 的回合开始撤」变假、「A 的回合开始撤」变真 ⇒ **A-6 两半一起翻**；`TargetOwner` 恒 -1 ⇒ **A-5 红**。
//  · **B 组**：把自增益那一侧也改成「按对面判」（或 `TargetOwner` 恒写 `1 - TargetSeatOf`）⇒ **B-5 / B-6 红**；
//    而 A 组仍绿 —— 两条**结构上不可能同时满足**。
//  · **C 组**：把 `GivePayload.IconWordsArePayloads`（连同 `IconNames` 里的 `shield`）去掉 / 把守卫改回
//    「无条件剥 / 不剥」两档，`shield armour 1` 就回到「被 `shield` 前缀抢走、`Armour 1` 静默丢掉」
//    ⇒ **C-3（armour 缺席）与 C-4（多出一项 `shield` 关键词）一起红**。
//  · **D 组**：删掉 `RuleCore.ItemDurationOutlier` 的 `Worst Temper` 支（或把 `untilEnemyTurnStart` 写回 `turn`）
//    ⇒ `armour` 那条的 `UntilOpponentNextTurn` 变假 ⇒ **D-1 红、D-5(b) 红**；
//    把 outlier 写成「**整条 op** 换时长」（不是逐属性）⇒ `attack` / `flying` 也变 `+0x34`
//    ⇒ **D-2 / D-3 红、D-5(a) 红**（施放者回合就开始撤）。
//
// ============================ 判据从哪来（⛔ 别在代码里反算期望值）============================
//  · 值一律**字面量**，来源是**卡面印刷文字 + 原版数据**，⛔ 不是从我们的实现推：
//      `Tempest's Wrath` 卡面 `Give -2 [Ranged] to an enemy troop until your next turn`（⇒ -2 · nextturn）
//      `Steadfast Warriors` 卡面 `Give Armour 2 to all friendly units until your next turn`（⇒ armour 2 · nextturn）
//      `Worst Temper` 卡面 `Your Warlord gains +1 [Attack], [Shield] Armour 1 and [Wings] Flying this turn`
//        ＋ 原版数据 `armourChange 1 + untilEnemyTurnStart`（判据 → `判据/B113_G2_回合起止语义.md` §3·4）
//  · **卡面字符串是【现读】的**（`CardDatabase.Load()` → `CardDef.Desc`），⛔ 不硬编码 ——
//    所以卡表改了就跟着改，不会出现「断言里抄了一份旧卡面」。
//  · `Worst Temper` 的解析那一段**必须真解析一次**（`EffectText.Parse` → `GivePayload.Parse`）：
//    `B-186` 修的是**解析层**，绕过它去断言就等于没有验。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  本件**自包含**（不建窗、不依赖 UI、不读盘）。建议宿主 = `RuleEngine/Editor/RuleEngineTest.cs`
//  的 `Run()` 里、**第 1389 行那一整行块的末尾**（`B112_T2C_Assert.Run(); …` 之后，
//  **同一物理行、且该行目前没有行尾 `//`**）追加：
//      B112W_B127_Assert.Run(); _pass += B112W_B127_Assert.Pass; _fail += B112W_B127_Assert.Fail;
//  ⚠️ **追加前先看那一行有没有行尾 `//`** —— 有的话要追加在它**之前**（否则整段代码被注释吞掉，
//    `typecheck` 照样 0/0、断言一条都不跑；判据 → `CLAUDE.md` §三 那条「同一物理行追加」）。
//
// ============================ 副作用 / 恢复 ============================
//  · 每次 `RuleCore.NewBattle(...)` 建**全新** `BattleContext`（`NewBattle` 自己会清 `_keywordsPact` 静态旁表）；
//    ⛔ 不碰 `CardProgress` / 背包 / 存档 / `Resources` 写口。
//  · `CardDatabase.Load()` 只读；结果缓存在 `_pool`（同一次自检里反复用）。
//  · 入队类动作一律 `using (RuleCore.BeginAction(ctx)) { }` 泵干净再读局面（与 `B98G1_Assert` 同一口径）。
//  · 本件**不**调 `EditorApplication.Exit`（那是宿主的活）。
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>`B-127` S1（`TargetOwner`：`+0x33` 按**持卡人**判到期）+ S2（`untilEnemyTurnStart` ⇒ `UntilOpponentNextTurn`）。</summary>
public static class B112W_B127_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B127] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ════════════════════════════════════════════════════════════════════════════════════════
    //  ① 编排
    // ════════════════════════════════════════════════════════════════════════════════════════
    public static void Run()
    {
        MenuCheck.Section(_sink,
            "🆕 B112W · `B-127` S1（`+0x33` 按**持卡人**判到期 · `TargetOwner`）+ S2（`Worst Temper` 的 `armour` ⇒ `+0x34`）");

        TrySection("A S1 跨阵营：持卡人回合开始撤（不是施放者回合）", SectionA_CrossSideExpiry);
        TrySection("B S1 灭自证反向：自增益 `TargetOwner == Owner`，行为与改前一致", SectionB_SelfBuffControl);
        TrySection("C S2 解析层前置：`[Shield] Armour 1` 解成 `armour`（`B-186`）", SectionC_PayloadParse);
        TrySection("D S2 时长与回合边界三档：只有 `armour` 是 `+0x34`", SectionD_DurationAndTurnBoundary);

        Debug.Log(_sink.P + "B112W_B127 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void TrySection(string name, Action body)
    {
        try { body(); }
        catch (Exception e) { Tr(false, "「" + name + "」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    //  ② A 组 · S1：跨阵营（0 号打 1 号的单位）⇒ 到期点在**持卡人（1 号）**的回合开始
    //     判据 = `CardScript__OnTurnSetup.c:40`（`param_1 + 0x40` = **持效果那张卡**的 `isPlayer`）
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void SectionA_CrossSideExpiry()
    {
        MenuCheck.Section(_sink, "A · S1 跨阵营：`Tempest's Wrath`（0 号 → 1 号的部队）⇒ **持卡人**回合开始撤");

        var card = CardByName("Tempest's Wrath");
        Tr(card != null, "A-0 **前提**：卡池按名取得到 `Tempest's Wrath`（`CardDatabase.Load()`）"
                       + "｜🧨 卡名改了 / 卡池读不到 ⇒ 红（⛔ 不静默跳过）");
        if (card == null) return;
        Tr(!string.IsNullOrEmpty(card.Desc) && card.Desc.IndexOf("until your next turn", StringComparison.OrdinalIgnoreCase) >= 0,
           "A-0b **前提**：卡面写着 `until your next turn`（实得 `" + (card.Desc ?? "<null>") + "`）—— 本条测的就是 `+0x33` 那一档");

        var ctx = Battle();
        Tr(ctx.Active == 0, "A-0c **前提**：0 号玩家先手（实得 `Active=" + ctx.Active + "`）");
        if (ctx.Active != 0) return;
        var victim = Place(ctx, 1, 2, TroopDef("B127Victim", 2, 2, 3));
        Tr(victim != null && ctx.Players[1].Board[2] == victim, "A-0d **前提**：1 号玩家的 2 号格上摆着那个目标");

        ctx.Players[0].Energy = 10;
        ctx.Players[0].Hand.Add(ctx.NewInstance(card));
        int handIdx = ctx.Players[0].Hand.Count - 1;
        int rc = RuleCore.PlayTactic(ctx, 0, handIdx, 2, victim);   // `chosen` = 唯一带侧信息的目标入口（同 `CanPlayTactic` 的 doc）
        C(rc, RuleCodes.OK, "A-1 **前提**：这一手**打出去了**（实得 " + RuleCodes.Describe(rc) + "）"
                          + "｜🧨 卡表 / 解析层回归导致打不出去 ⇒ 红（⛔ 不是跳过）");
        if (rc != RuleCodes.OK) return;

        var buff = Buff(victim, "ranged");
        Tr(buff != null, "A-2 **前提**：目标身上挂上了 `ranged` 限时增益（否则下面全无从谈起）");
        if (buff == null) return;
        C(buff.Value, -2, "A-2b 值是 **-2**（卡面 `Give -2 [Ranged]`）");
        Tr(buff.UntilMyNextTurn && !buff.UntilOpponentNextTurn,
           "A-3 这是 `+0x33` 那一档（`UntilMyNextTurn` 真、`UntilOpponentNextTurn` 假）—— 与 `Worst Temper` 的 `+0x34` 分开测");

        C(buff.Owner, 0, "A-4 `Owner` = **施放者**（0 号）—— 它仍然是 `+0x34` 那一档要用的量，⛔ 别把它也改成持卡人");
        C(buff.TargetOwner, 1,
          "A-5 ★★ `TargetOwner` = **持卡人**（1 号，`CardScript__OnTurnSetup.c:40` 的 `param_1 + 0x40`）"
        + "｜🧨 `TargetSeatOf` 恒回 -1（或写死施放者）⇒ 红");

        Tr(buff.IsDueAtTurnStart(1) && !buff.IsDueAtTurnStart(0),
           "A-6 ★★ 到期点 = **持卡人（1 号）**的回合开始；施放者（0 号）的回合开始**不撤**"
         + "｜🧨 `IsDueAtTurnStart` 改回 `Owner == turnPlayer` ⇒ 两个都翻 ⇒ 红（这就是改前的错，多撑一整个回合）");

        // 行为面（与 A-6 是两个独立读数：一个是谓词、一个是真入队 + 真泵）
        int qCaster = RuleCore.EnqueueTurnSetupEffectRemovals(ctx, 0);
        Drain(ctx);
        Tr(HasBuff(victim, buff),
           "A-7 ★★ **行为**：施放者（0 号）回合开始那一趟 ⇒ 这条**还在**（本趟入队 " + qCaster + " 条，与本条无关的另有其数）"
         + "｜🧨 同上，改回按施放者判 ⇒ 这里就被撤掉了 ⇒ 红");

        int qHolder = RuleCore.EnqueueTurnSetupEffectRemovals(ctx, 1);
        Drain(ctx);
        Tr(!HasBuff(victim, buff),
           "A-8 ★★ **行为**：持卡人（1 号）回合开始那一趟 ⇒ 这条**被撤掉了**（本趟入队 " + qHolder + " 条）"
         + "｜🧨 谓词只改一侧 / 入队口另抄一份 ⇒ 与 A-6 分叉 ⇒ 红");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    //  ③ B 组 · S1 的灭自证反向：**自增益**（目标是自己人）⇒ `TargetOwner == Owner`，行为与改前一致
    //     为什么必须有这一组：只断「跨阵营按持卡人撤」的话，把 `TargetOwner` 写成「恒对面」也能绿。
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void SectionB_SelfBuffControl()
    {
        MenuCheck.Section(_sink, "B · S1 灭自证反向：`Steadfast Warriors`（0 号 → 自己的部队）⇒ `TargetOwner == Owner`，行为与改前一致");

        var card = CardByName("Steadfast Warriors");
        Tr(card != null, "B-0 **前提**：卡池按名取得到 `Steadfast Warriors` ｜🧨 卡名改了 ⇒ 红");
        if (card == null) return;
        Tr(!string.IsNullOrEmpty(card.Desc) && card.Desc.IndexOf("until your next turn", StringComparison.OrdinalIgnoreCase) >= 0,
           "B-0b **前提**：卡面写着 `until your next turn`（实得 `" + (card.Desc ?? "<null>") + "`）");

        var ctx = Battle();
        Tr(ctx.Active == 0, "B-0c **前提**：0 号玩家先手（实得 `Active=" + ctx.Active + "`）");
        if (ctx.Active != 0) return;
        var ally = Place(ctx, 0, 2, TroopDef("B127Ally", 2, 2, 3));
        Tr(ally != null && ctx.Players[0].Board[2] == ally, "B-0d **前提**：0 号玩家的 2 号格上摆着自己人");

        ctx.Players[0].Energy = 10;
        ctx.Players[0].Hand.Add(ctx.NewInstance(card));
        int rc = RuleCore.PlayCard(ctx, 0, ctx.Players[0].Hand.Count - 1, -1);
        C(rc, RuleCodes.OK, "B-1 **前提**：这一手**打出去了**（实得 " + RuleCodes.Describe(rc) + "）");

        var buff = Buff(ally, "armour");
        Tr(buff != null, "B-2 **前提**：自己人身上挂上了 `armour` 限时增益");
        if (buff == null) return;
        C(buff.Value, 2, "B-2b 值是 **2**（卡面 `Give Armour 2`）");
        Tr(buff.UntilMyNextTurn && !buff.UntilOpponentNextTurn, "B-3 这是 `+0x33` 那一档");

        C(buff.Owner, 0, "B-4 `Owner` = 施放者（0 号）");
        C(buff.TargetOwner, 0,
          "B-5 ★★ **自增益 ⇒ `TargetOwner` 与 `Owner` 相等**（持卡人就是施放者自己）"
        + "｜🧨 把 `TargetOwner` 写成「恒 `1 - 持卡人`」那种反向 ⇒ 红（A 组那时仍绿 ⇒ 两条不可能同时满足）");

        Tr(buff.IsDueAtTurnStart(0) && !buff.IsDueAtTurnStart(1),
           "B-6 ★ **行为与改前完全一致**：自己的回合开始撤、对手的回合开始不撤"
         + "｜🧨 把判据改成「按对面」⇒ 两个都翻 ⇒ 红");

        int qOther = RuleCore.EnqueueTurnSetupEffectRemovals(ctx, 1);
        Drain(ctx);
        Tr(HasBuff(ally, buff),
           "B-7 ★ **行为**：对手（1 号）回合开始那一趟 ⇒ 这条**还在**（本趟入队 " + qOther + " 条，与本条无关的另有其数）"
         + "｜🧨 同上 ⇒ 红");

        int qSelf = RuleCore.EnqueueTurnSetupEffectRemovals(ctx, 0);
        Drain(ctx);
        Tr(!HasBuff(ally, buff),
           "B-8 ★ **行为**：自己（0 号）回合开始那一趟 ⇒ 这条**被撤掉了**（本趟入队 " + qSelf + " 条）");

        // ——— B-200（`B112W_B200`）：`all friendly units` **含督军** ———
        // 判据 = 原版 SO `cards_raw.json` 的 `DA53.abilityLogic[0].targetCriteria`
        //        = targetsAffected 10 / friendlyUnits 1 / includeHero 1 / includeMinions 1
        //        ＋ 反编译 `AbilityLogic__GetTargets.c:972-989`（ta=10 把 `+0x16 includeHero` 传进建池器）
        //        ＋ `BattleManager__GetUnitsInPlayPooledList.c:54-57`（`includeHero == 0` 才跳过 cardType 10 = 督军）。
        //   ⚠️ 前提是**同一份 `ctx`**（B-8 已经把 ally 的 armour 撤了）⇒ 这里**新开一局**，⛔ 别复用上面那份。
        var ctxB2 = Battle();
        Tr(ctxB2.Active == 0, "B-200-0 **前提**：0 号玩家先手（实得 `Active=" + ctxB2.Active + "`）");
        if (ctxB2.Active != 0) return;
        var allyB2 = Place(ctxB2, 0, 2, TroopDef("B200Ally", 2, 2, 3));
        var myWarlord = ctxB2.Players[0].Warlord;
        var foeWarlord = ctxB2.Players[1].Warlord;
        Tr(allyB2 != null && ctxB2.Players[0].Board[2] == allyB2,
           "B-200-0b **前提**：0 号槽 2 摆着自己人");
        Tr(myWarlord != null && myWarlord.IsWarlord && foeWarlord != null && foeWarlord.IsWarlord,
           "B-200-0c **前提**：双方督军都在场（`Players[*].Warlord`）");
        if (allyB2 == null || myWarlord == null || foeWarlord == null) return;

        ctxB2.Players[0].Energy = 10;
        ctxB2.Players[0].Hand.Add(ctxB2.NewInstance(card));
        int rcB2 = RuleCore.PlayCard(ctxB2, 0, ctxB2.Players[0].Hand.Count - 1, -1);
        C(rcB2, RuleCodes.OK, "B-200-1 **前提**：`Steadfast Warriors` 打出去了");

        var allyBuff = Buff(allyB2, "armour");
        var myBuff   = Buff(myWarlord, "armour");
        var foeBuff  = Buff(foeWarlord, "armour");
        Tr(allyBuff != null, "B-200-2 自己人（槽 2）挂上了 `armour`");
        Tr(myBuff != null,
           "B-200-3 ★★ **己方督军也挂上了 `armour`**（原版 `includeHero = 1`；卡面写 `units` 不是 `troops`）"
         + "｜🧨 解析层 `Kind` 归成 `troop`，或 `AddSide` 的 `troopOnly` 判错 ⇒ 红");
        if (myBuff != null) C(myBuff.Value, 2, "B-200-3b 督军身上同样是 **2**（卡面 `Armour 2`）");
        Tr(foeBuff == null,
           "B-200-4 ★ **敌方督军没有**（`friendlyUnits = 1`）｜🧨 `Side` 写成 `any` ⇒ 红");

        int qB2 = RuleCore.EnqueueTurnSetupEffectRemovals(ctxB2, 0);
        C(qB2, 2,
          "B-200-5 ★★ 这一趟**恰好 2 条**到期项（自己人 + 己方督军）"
        + "｜🧨 `all friendly units` 少收/多收一个对象 ⇒ 红（与 B-200-3 是一对：只改计数不断对象 = 不知道是谁）");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    //  ④ C 组 · S2 的**真前置**（`B-186`）：`Worst Temper` 卡面的 `[Shield] Armour 1` 解成 `armour`
    //     ⛔ 这一段必须**真解析一次卡面字符串** —— 绕过去断「时长对了」就等于没验那个修复。
    //     期望值来源 = 卡面印刷文字（`+1 [Attack], [Shield] Armour 1 and [Wings] Flying`）
    //                ＋ 原版数据 `armourChange 1`（`cards_raw.json`）⇒ **不是**从我们的实现反算。
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void SectionC_PayloadParse()
    {
        MenuCheck.Section(_sink, "C · S2 前置（`B-186`）：`Worst Temper` 的 `[Shield] Armour 1` **真解析**成 `armour`，且不出现 `shield` 关键词");

        var card = CardByName("Worst Temper");
        Tr(card != null, "C-0 **前提**：卡池按名取得到 `Worst Temper` ｜🧨 卡名改了 ⇒ 红");
        if (card == null) return;
        Tr(!string.IsNullOrEmpty(card.Desc), "C-0b **前提**：它的卡面文字非空");

        List<string> unparsed, partial;
        var efOps = EffectText.Parse(card.Desc, out unparsed, out partial, card: card);
        Tr(unparsed == null || unparsed.Count == 0,
           "C-0c **前提**：卡面**整条**都解析得出（`unparsed` 空，实得 " + (unparsed == null ? 0 : unparsed.Count) + " 段）");

        EffectOp give = null;
        if (efOps != null)
            for (int i = 0; i < efOps.Count; i++)
                if (efOps[i] != null && !string.IsNullOrEmpty(efOps[i].Payload)) { give = efOps[i]; break; }
        Tr(give != null, "C-0d **前提**：解析出一个带载荷的 `give` / `gain` op");
        if (give == null) return;
        C(give.Duration, "turn",
          "C-1 卡面写的是 `this turn` ⇒ op 的 `Duration` = `turn`（这一格**没被改** —— 原版数据只在 armour 那一项上是 `+0x34`）");

        var ops = GivePayload.Parse(give.Payload);
        Tr(ops != null, "C-2 **前提**：载荷 `" + give.Payload + "` 解得开（`GivePayload.Parse`）");
        if (ops == null) return;

        int nArmour = 0, nAttack = 0, nFlying = 0, nShield = 0, nArmourVal = -1;
        for (int i = 0; i < ops.Count; i++)
        {
            var o = ops[i];
            if (o == null) continue;
            string attr = o.IsKeyword ? null : o.Attr;
            if (attr == "attack") nAttack++;
            if (o.Keyword == "armour") { nArmour++; nArmourVal = o.Value; }
            if (o.Keyword == "flying") nFlying++;
            if (o.Keyword == "shield") nShield++;
        }

        C(ops.Count, 3, "C-2b 卡面上**恰好三项**（`+1 Attack` / `Armour 1` / `Flying`）—— " + DumpOps(ops)
                      + "｜🧨 多出 `Unresolved` 一项 ⇒ 红（那说明又有半句被吞了）");
        C(nArmour, 1,
          "C-3 ★★ `[Shield] Armour 1` 解成**一项 `armour`**（卡面 `Armour 1` ＋ 原版数据 `armourChange 1`）"
        + "｜🧨 `B-186` 的守卫没了（`shield` 前缀把 `armour 1` 吃掉）⇒ 这一项**缺席** ⇒ 红");
        C(nArmourVal, 1, "C-3b `armour` 的值是 **1**（卡面 `Armour 1`）");
        C(nShield, 0,
          "C-4 ★★ 灭自证：**一项 `shield` 关键词都没有** —— 改前被前缀抢走时会恰好解出「`shield` 关键词 1」"
        + "（`Give Shield to a friendly unit` 那 33 张卡走的是另一条路，不受影响）｜🧨 把 `IconNames` 里的 `shield` 去掉 ⇒ 红");
        C(nAttack, 1, "C-5 `+1 [Attack]` 解成一项 `attack`（非关键词 · 值 1）");
        C(nFlying, 1, "C-6 `[Wings] Flying` 解成一项 `flying` 关键词");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    //  ⑤ D 组 · S2 的时长 + 回合边界三档
    //     判据 = `CardScript__OnTurnSetup.c:71/:76/:83`（`+0x34` 在**施放者对面**的回合开始撤）
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void SectionD_DurationAndTurnBoundary()
    {
        MenuCheck.Section(_sink, "D · S2：`Worst Temper` 的 `armour` 是 `+0x34`（`UntilOpponentNextTurn`）、`attack`/`flying` **不是**；再接回合边界三档");

        BattleContext ctx0; UnitState wl0; string why0;
        if (!PlayWorstTemper(out ctx0, out wl0, out why0)) { Tr(false, "D-0 **前提**：" + why0); return; }

        var atk = Buff(wl0, "attack");
        var arm = Buff(wl0, "armour");
        var fly = Buff(wl0, "flying");
        Tr(atk != null && arm != null && fly != null,
           "D-0b **前提**：督军身上 `attack` / `armour` / `flying` 三条**都在**"
        + "（实得 attack=" + (atk != null) + " armour=" + (arm != null) + " flying=" + (fly != null) + "）");
        if (atk == null || arm == null || fly == null) return;

        C(arm.Value, 1, "D-0c `armour` 的值是 **1**（卡面 `Armour 1` ＋ 原版 `armourChange 1`）");
        C(atk.Value, 1, "D-0d `attack` 的值是 **1**（卡面 `+1 [Attack]`）");

        Tr(arm.UntilOpponentNextTurn && !arm.UntilMyNextTurn,
           "D-1 ★★ **`armour` 那一条是 `untilEnemyTurnStart`（`+0x34`）** —— 原版数据 `armourChange 1 + untilEnemyTurnStart`，卡面却写 `this turn`"
         + "｜🧨 删掉 `ItemDurationOutlier` 的 `Worst Temper` 支 / 把 `untilEnemyTurnStart` 写回 `turn` ⇒ 这一格变假 ⇒ 红");
        Tr(!atk.UntilOpponentNextTurn && !atk.UntilMyNextTurn,
           "D-2 ★★ **`attack` 那一条仍是 `this turn`**（两个标志都为假 = 回合末到期）"
         + "｜🧨 把 outlier 写成「整条 op 换时长」（不是逐属性）⇒ 它也变 `+0x34` ⇒ 红");
        Tr(!fly.UntilOpponentNextTurn && !fly.UntilMyNextTurn,
           "D-3 ★★ **`flying` 那一条仍是 `this turn`** ｜🧨 同 D-2 ⇒ 红");
        Tr(atk.Owner == 0 && arm.Owner == 0 && fly.Owner == 0,
           "D-4 三条的 `Owner` 都是**施放者**（0 号）—— `+0x34` 那一档比的就是它（`originalIsPlayer`）"
         + "｜🧨 把 `Owner` 也改成持卡人 ⇒ 红（那会让 `+0x34` 的判据变成 `+0x33`）");
        Tr(arm.TargetOwner == 0 && atk.TargetOwner == 0 && fly.TargetOwner == 0,
           "D-4b 三条的 `TargetOwner` 都是**持卡人**（这里 = 自己的督军 = 0 号；与 B 组同一条口径）");

        // ---- 三档：每档**各建一份独立夹具**（⛔ 不共用状态 —— 5c 的教训：共用会让 (c) 读到的 armour 早已被 (b) 摘掉）
        // (a) **施放者自己的**回合开始 ⇒ `+0x34` 不到期（`Owner != turnPlayer` 为假）⇒ 三条都还在
        {
            BattleContext ctx; UnitState wl; string why;
            if (!PlayWorstTemper(out ctx, out wl, out why)) { Tr(false, "D-5a **前提**：" + why); }
            else
            {
                var a1 = Buff(wl, "attack"); var r1 = Buff(wl, "armour"); var f1 = Buff(wl, "flying");
                int q = RuleCore.EnqueueTurnSetupEffectRemovals(ctx, 0);
                Drain(ctx);
                Tr(a1 != null && r1 != null && f1 != null && HasBuff(wl, a1) && HasBuff(wl, r1) && HasBuff(wl, f1),
                   "D-5a ★ **档 (a) 施放者（0 号）回合开始 ⇒ 三条都还在**（本趟入队 " + q + " 条，与本组无关的另有其数）"
                 + "｜🧨 outlier 写成整条 op 换时长 ⇒ `armour` 在这里就被撤 ⇒ 红");
            }
        }
        // (b) **对手**回合开始 ⇒ `+0x34` 到期 ⇒ 只有 `armour` 撤
        {
            BattleContext ctx; UnitState wl; string why;
            if (!PlayWorstTemper(out ctx, out wl, out why)) { Tr(false, "D-5b **前提**：" + why); }
            else
            {
                var a1 = Buff(wl, "attack"); var r1 = Buff(wl, "armour"); var f1 = Buff(wl, "flying");
                int q = RuleCore.EnqueueTurnSetupEffectRemovals(ctx, 1);
                Drain(ctx);
                Tr(r1 != null && !HasBuff(wl, r1),
                   "D-5b ★★ **档 (b) 对手（1 号）回合开始 ⇒ `armour` 撤掉**（本趟入队 " + q + " 条）"
                 + "｜🧨 删掉 `ItemDurationOutlier` ⇒ 它还留着 ⇒ 红");
                Tr(a1 != null && f1 != null && HasBuff(wl, a1) && HasBuff(wl, f1),
                   "D-5c ★★ **同一趟里 `attack` / `flying` 还在**（它们是 `this turn`，回合末才撤）"
                 + "｜🧨 整条 op 换时长 ⇒ 它们一起没了 ⇒ 红");
            }
        }
        // (c) 回合末 ⇒ `this turn` 那两条撤，`+0x34` 那条**不撤**
        {
            BattleContext ctx; UnitState wl; string why;
            if (!PlayWorstTemper(out ctx, out wl, out why)) { Tr(false, "D-5d **前提**：" + why); }
            else
            {
                var a1 = Buff(wl, "attack"); var r1 = Buff(wl, "armour"); var f1 = Buff(wl, "flying");
                int reverted = wl.RevertBuffs(true, 0);      // `atTurnEnd:true` = 「本回合结束撤」那第三档
                Tr(a1 != null && f1 != null && !HasBuff(wl, a1) && !HasBuff(wl, f1),
                   "D-5d ★★ **档 (c) 回合末 ⇒ `attack` / `flying` 撤掉**（实撤 " + reverted + " 条）"
                 + "｜🧨 把 `+0x34` 那档并进「回合末」⇒ 也会被撤（症状在这里看不出，D-5e 抓）");
                Tr(r1 != null && HasBuff(wl, r1),
                   "D-5e ★★ **同一趟里 `armour` 不撤** —— 它是 `until enemy turn start`，⛔ 不是「本回合结束」"
                 + "｜🧨 把 `UntilOpponentNextTurn` 并进回合末那一档 ⇒ 红");
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    //  ⑥ 夹具
    // ════════════════════════════════════════════════════════════════════════════════════════
    static List<CardDef> _pool;

    /// <summary>卡池（只读；`Resources/cards_engine.json` 那一份）。读不到就返回空表 —— 调用方一律**先断前提**。</summary>
    static List<CardDef> Pool()
    {
        if (_pool != null && _pool.Count > 0) return _pool;
        try { _pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B127] `CardDatabase.Load()` 抛了：" + e.Message); _pool = new List<CardDef>(); }
        return _pool;
    }

    static CardDef CardByName(string name)
    {
        var pool = Pool();
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && string.Equals(pool[i].Name, name, StringComparison.Ordinal)) return pool[i];
        return null;
    }

    static CardDef HeroDef(string n, string faction)
    { return new CardDef(n, n, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord"); }

    static CardDef TroopDef(string n, int cost, int atk, int hp)
    { return new CardDef(n, n, "unit", "", null, "Test", cost, atk, hp, 0, null); }

    /// <summary>督军 + 20 张垫牌的牌库（与 `RuleEngineTest.Deck` 同一口径）。</summary>
    static List<CardDef> Deck()
    {
        var d = new List<CardDef> { HeroDef("B127Warlord", "Test") };
        for (int i = 0; i < 20; i++) d.Add(TroopDef("B127filler" + i, 1, 0, 1));
        return d;
    }

    /// <summary>建一局（`shuffle:false` ⇒ 可复现；`NewBattle` 自己会清 `_keywordsPact` 那类局级静态旁表）。</summary>
    static BattleContext Battle()
    { return RuleCore.NewBattle(Deck(), Deck(), seed: 0, shuffle: false); }

    /// <summary>把一个单位直接摆到场上（跳过部署流程，只为构造目标场景）。口径同 `RuleEngineTest.Place`。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        u.SetOwnerSeat(p);
        return u;
    }

    /// <summary>「把泵点跑干净」—— 入队类动作只有在 `BeginAction` 的作用域结束时才真执行。</summary>
    static void Drain(BattleContext ctx) { using (RuleCore.BeginAction(ctx)) { } }

    /// <summary>名字命中的第一条限时增益（找不到 = null）。</summary>
    static UnitState.TempBuff Buff(UnitState u, string name)
    {
        if (u == null) return null;
        for (int i = 0; i < u.TempBuffs.Count; i++)
            if (u.TempBuffs[i] != null && u.TempBuffs[i].Name == name) return u.TempBuffs[i];
        return null;
    }

    /// <summary>**同一个对象**还在不在表里（⛔ 不按名字判 —— 同名可能有多条）。</summary>
    static bool HasBuff(UnitState u, UnitState.TempBuff b)
    {
        if (u == null || b == null) return false;
        for (int i = 0; i < u.TempBuffs.Count; i++) if (ReferenceEquals(u.TempBuffs[i], b)) return true;
        return false;
    }

    /// <summary>打出一张 `Worst Temper` 的完整夹具：新局 → 0 号把它打进手牌 → 打给自己督军。</summary>
    /// <returns>false = 前提不成立（`why` 写清楚是哪一步）。</returns>
    static bool PlayWorstTemper(out BattleContext ctx, out UnitState warlord, out string why)
    {
        ctx = null; warlord = null; why = null;
        var card = CardByName("Worst Temper");
        if (card == null) { why = "卡池里按名取不到 `Worst Temper`"; return false; }
        ctx = Battle();
        if (ctx.Active != 0) { why = "夹具要求 0 号先手，实得 `Active=" + ctx.Active + "`"; return false; }
        warlord = ctx.Players[0].Warlord;
        if (warlord == null) { why = "0 号玩家没有督军（`NewBattle` 建的）"; return false; }
        ctx.Players[0].Energy = 10;
        ctx.Players[0].Hand.Add(ctx.NewInstance(card));
        int rc = RuleCore.PlayCard(ctx, 0, ctx.Players[0].Hand.Count - 1, -1);
        if (rc != RuleCodes.OK) { why = "`Worst Temper` 打不出去，码 " + rc + "（" + RuleCodes.Describe(rc) + "）"; return false; }
        return true;
    }

    static string DumpOps(List<PayloadOp> ops)
    {
        if (ops == null) return "null";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < ops.Count; i++)
        {
            if (i > 0) sb.Append(" · ");
            var o = ops[i];
            if (o == null) { sb.Append("<null>"); continue; }
            if (o.IsKeyword) sb.Append("kw:").Append(o.Keyword).Append(' ').Append(o.Value);
            else sb.Append(o.Attr).Append(' ').Append(o.Value);
            if (o.Unresolved) sb.Append("(未解)");
        }
        return sb.ToString();
    }
}
