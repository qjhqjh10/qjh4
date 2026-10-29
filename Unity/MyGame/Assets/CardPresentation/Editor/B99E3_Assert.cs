// B99E3_Assert.cs — 执行代理 `B99_E3`（第九十九会话 · 波 1 · 战斗线写手）的断言集
//
// ============================ 它盯的是什么 ============================
//   账 `战斗逻辑驱动 :1281` / `:1379`（`RawCardScript.ResolveActiveAbility` 的 4 参 / 3 参两个重载，
//   **同一个落点**）的三处缺口：
//
//   ① **(a) `chosenAbility 320 = followAfterResolution` 的 ca 分派**（`SW9 Fenrisian Wolf` 1 张）
//      判据（第一权威，逐句现读 `d:/2/tools/decomp_full/`）：
//        `AbilityLogic__PlayAbility.c:2244` `if (iVar8 != 0x140) return;`（`0x140 = 320`）
//        → `:2471-2478` `BattleManager__AddFollowAfterResolution(manager, abilityData+0x10, +0x20, +0x44, …, 0)`
//        → `BattleManager__AddFollowAfterResolution.c:26` 造一条 `BattleAction(0x3a = 58)`，末句入行动队列
//        → 出队时 `BattleManager__ResolveFollowAfterResolution.c:371` `CardScript__FollowUpResolutionPlayed`
//        → `CardScript__FollowUpResolutionPlayed.c:5-9` `RawCardScript__OnTrigger(…, 0x19a = 410, …)`
//      我们这一侧 = `RuleCore.RunFollowAfterResolutionRow`（`EmitFollowUpResolution` ＋
//      `RuleCore.FireTriggerByNumber(410)`，与 `RuleCore.cs:12884` 同一对调用）。
//
//   ② **(b) 手牌族 `ta = 50 / 51 / 65`** 的卡池计数（`CountEval.CountCardPool` + `CardPoolCandidatePasses`）
//      判据 = `AbilityLogic__GetTargets.c:918-931`(50 手牌) / `:933-950`(51 手牌+牌库) /
//      `:966-978`(65 牌库) ＋ 逐候选 `FilterMethods.CheckIfRawCardMeetsCriteria`
//      （我们的等价物 = `TargetCriteriaEval.MatchesRawCard`）。
//      **改前的两个真缺陷**（现池就有消费者）：
//        · `GSC75 Roaming Outriders`（`ta=50 · keywordFilter=10 Vehicle`）⇒ 整行 gap ⇒ 「每有一辆载具重复一次」失效；
//        · `GSC37 Patriarch`（`ta=50 · includeWhispers=1`）⇒ 整行 gap ⇒ 「每张（敌方）手牌减 1 费」失效；
//        · 另外改前**防御卡（`defence`）不算 `includeSpells`**（手写 `c.Type == "tactic"`），
//          而原版 `MeetsCardTypeCriteria` 的 `Tactic(20)` 含 `defence`/`enchantment`（`TypeGatePasses` 的同一张表）。
//
//   ③ **(c) 墓地族 `ta = 60`** 的逐候选判据（`CountEval.CemeteryCandidatePasses` 改走同一个
//      `CardPoolCandidatePasses`）—— 改前 `traitsFilter` / 四组 stat / `armyFilter` / `spellType`
//      / 稀有度**整条 gap**，改后都能判（判据 `CheckIfRawCardMeetsCriteria.c:18-103`）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · A1/A2：把 `RunActiveRowOpsOrGrantedTrigger` 里的 `if (ca == 320) return RunFollowAfterResolutionRow(...)`
//           删掉 ⇒ `ca=320` 落到 `RunActiveRowOps`（没有 op）⇒ 返回 0、关键词不挂 ⇒ **A2/A3 红**。
//   · A3：把 `RunFollowAfterResolutionRow` 里的 `RuleCore.FireTriggerByNumber(...410...)` 删掉
//           （只留 `EmitFollowUpResolution` 那一句表现层事件）⇒ 410 正文不跑 ⇒ **A3 红**（A2 仍绿）。
//   · B1：把手牌支改回「只认 `unit` / `tactic`」⇒ 防御卡那 1 张不算 ⇒ **B1 红**（2 ≠ 3）。
//   · B2：把 `CardPoolSetSupported` 的 `keywordFilter` 挡回去 ⇒ 整行 null ⇒ **B2 红**。
//   · B3：把 ta=51 的手牌 + 牌库两半只留一半 ⇒ **B3 红**。
//   · C1：把 `CemeteryCandidatePasses` 改回手写「只看 keyword / filterSpecificCards」⇒
//          费用那一格整条 gap ⇒ **C1 红**。
//
// ============================ 怎么用（宿主）============================
//   `B99E3_Assert.Run(); _sink.Pass += B99E3_Assert.Pass; _sink.Fail += B99E3_Assert.Fail;`
//   · 推荐宿主 = `Editor/RuleEngineTest.cs`（`RuleEngineTest.Run`）—— 本件**只碰纯引擎**，
//     不建 GameObject、不建资产、不需要场景（同 `B98G5_Assert`）。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B99_E3`：账 `1281` / `1379` 的 (a) `ca=320` 分派 · (b) 手牌/牌库卡池 · (c) 墓地卡池。</summary>
public static class B99E3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[E3] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }
    /// <summary>本趟**实际执行**的断言条数（= Pass + Fail；宿主打印它 = 自证真被跑到）。</summary>
    public static int RunCount { get { return _sink.Pass + _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具

    static CardDef Warlord(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }

    static List<CardDef> DeckOf(string faction)
    {
        return new List<CardDef> { Warlord("B99E3Hero_" + faction, faction) };
    }

    /// <summary>一局**真对局上下文**。</summary>
    static BattleContext NewCtx()
    {
        return RuleCore.NewBattle(DeckOf("SpaceWolves"), DeckOf("Goff"), seed: 0, shuffle: false);
    }

    /// <summary>白盒放盘（`RuleEngineTest.Place` 的等价物；⛔ 不走棋盘钩子）。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>手搓一张卡。⚠️ `cardTraits` / `keyword` 两列**必须显式挂**（`CardDatabase.Parse` 才挂它们）——
    /// 不挂的话 `TargetCriteriaEval.MatchesRawCard` 的 traits 那一格会判成「判不了」（`_cardTraits == null`），
    /// 整个卡池计数会静默退化成 `null`（那是夹具的前提，不是被测行为）。</summary>
    static CardDef Card(string name, string type, string faction, int cost, int attack, int health)
    {
        var def = new CardDef(name, name, type, "", "common", faction, cost, attack, health, 0, null,
                              subtype: type == "unit" ? "troop" : null);
        def.AttachCardTraits(new string[0]);     // 「没有任何 trait」—— 与真卡「cardTraits 已挂、只是空」同形
        def.AttachKeywordId(0);                  // 原版 `keyword = 0 (none)`；⛔ 不挂会让 `KeywordHas` 走词表兜底并出声
        return def;
    }

    /// <summary>手搓一条只填了指定几格的 `countSet` —— 其余维度**必须显式填 0**
    /// （`CountSetSpec` 的默认是 `Unknown(-1)`，会被判成「没落 ⇒ 判不了」）。</summary>
    static CardDef.CountSetSpec Set(int ta, int friendly, int enemy, int minions, int spells)
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = 0,
            TargetsAffected = ta,
            ChosenArmy = 0, ArmyFilter = 0, KeywordFilter = 0, TraitsFilter = 0,
            MaxTargets = 0, TargetSelectMethod = 0,
            AttackFilter = 0, AttackFilterOption = 0,
            CurrentHealthFilter = 0, CurrentHealthFilterOption = 0,
            ManaCostFilter = 0, ManaCostFilterOption = 0,
            RarityFilter = 0, RarityFilterOption = 0,
            SpellType = 0,
            FriendlyUnits = friendly, EnemyUnits = enemy,
            IncludeHero = 0, IncludeMinions = minions,
            IncludeSpells = spells, IncludeWhispers = 0, IncludeWeapons = 0,
            IncludeRemnants = 0, IncludeHeroParts = 0,
            IncludeSelf = 0, ExcludeNotSelf = 0,
            ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0,
            ExcludeRace = 0, ExcludeTrait = 0,
            SelectFromChooseResolution = 0,
            FilterSpecificCardIds = null, ExcludeEnchantmentIds = null,
        };
    }

    // ============================================================ 入口

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();

        MenuCheck.Section(_sink, "B99_E3 账 1281 / 1379：(a) `ca=320 followAfterResolution` 分派"
                              + " · (b) 手牌/牌库卡池计数 · (c) 墓地逐候选判据");

        SectionA_FollowAfterResolution();
        SectionB_手牌与牌库卡池();
        SectionC_墓地候选();

        Debug.Log(_sink.P + "B99E3 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 账 1281/1379 (a)

    static void SectionA_FollowAfterResolution()
    {
        MenuCheck.Section(_sink, "A `chosenAbility 320 = followAfterResolution` ⇒ 排一条 `trigger 410` 的正文"
                              + "（判据 `AbilityLogic__PlayAbility.c:2244/2471-2478` → `CardScript__FollowUpResolutionPlayed.c:5-9`）");

        // A0 **前提硬断言**（⛔ 不许写成 `if (条件) Check(...)`）
        C(RuleCore.RunFollowAfterResolutionRowForTest(null, 0, null, null), 0,
          "A0 ★ 前提：`ctx == null` / `u == null` ⇒ 返回 0、不抛（原版 `:2475` 是 `param_3 != 0 && param_2 != 0` 才动手）");

        var ctx = NewCtx();
        var noBody = Place(ctx, 0, 0, Card("B99E3NoFerocity", "unit", "SpaceWolves", 2, 3, 3));
        C(noBody.Card.TriggerOpsByNumber(410) == null ? 1 : 0, 1,
          "A1 ★ 前提：`B99E3NoFerocity` **没有** `trigger 410` 的正文（`TriggerOpsByNumber(410)` 为空）");
        C(RuleCore.RunActiveRowDispatchForTest(ctx, 0, noBody, 0, 320, 20, null), 0,
          "A1 🔴 `ca = 320` 走到 `RunFollowAfterResolutionRow`，但本卡没有 410 正文 ⇒ 返回 0（= 「没有后续」）"
        + "｜🧨 若 `ca == 320` 那一格分派被删掉，本行会落到 `RunActiveRowOps`（无 op）—— 值同样是 0，"
        + "所以真正的判据在 A2/A3（下面那两条用**有正文**的卡咬住）");

        // ---- 有 410 正文的卡 ⇒ 排进队列、正文真的跑 ----
        var withBody = Card("B99E3Ferocious", "unit", "SpaceWolves", 2, 3, 3);
        var bodyOps = new List<EffectOp>
        {
            new EffectOp { Verb = "give", Payload = "flank", Amount = 1, Source = "B99E3 夹具（410 正文）" },
        };
        var table = new Dictionary<int, List<EffectOp>>();
        table[410] = bodyOps;
        withBody.AttachTriggerOpsByNumber(table);            // = 数据半那张表（`CardDatabasePartial_B98H7`）
        var fu = Place(ctx, 0, 1, withBody);

        C(fu.Card.TriggerOpsByNumber(410) != null && fu.Card.TriggerOpsByNumber(410).Count == 1 ? 1 : 0, 1,
          "A2 ★ 前提：`B99E3Ferocious` 挂上了 **1 条** `trigger 410` 正文");
        Tr(!fu.Has("flank"),
           "A2 ★ 前提：它**现在没有** `flank`（否则 A3 测不出东西）");

        int dispatched = RuleCore.RunActiveRowDispatchForTest(ctx, 0, fu, 0, 320, 20, fu);
        C(dispatched, 1,
          "A2 🔴 `ca = 320` 分派到 follow-after-resolution 那一支 ⇒ 返回 **1**（= 已把 410 正文排进行动队列）"
        + "｜🧨 删掉 `RunActiveRowOpsOrGrantedTrigger` 里的 `if (ca == 320)` ⇒ 落到 `RunActiveRowOps`（无 op）⇒ 0 ⇒ 红");

        RuleCore.PumpActionQueues(ctx);
        Tr(fu.Has("flank"),
           "A3 🔴 泵完队之后 `flank` 真的挂上了 ⇒ **410 那条正文确实结算了**"
         + "（不是只发了表现层事件）｜🧨 把 `RunFollowAfterResolutionRow` 里的 `RuleCore.FireTriggerByNumber(…, 410, …)` "
         + "删掉（只留 `EmitFollowUpResolution`）⇒ 本行红、A2 仍绿 —— 这一对就是灭自证");

        // ---- 反例：**没有** 410 正文的卡 ⇒ 一个关键词都不该多出来 ----
        var plain = Place(ctx, 0, 2, Card("B99E3Plain", "unit", "SpaceWolves", 1, 1, 1));
        C(RuleCore.RunActiveRowDispatchForTest(ctx, 0, plain, 0, 320, 20, plain), 0,
          "A4 对照：同一分派、同一时刻，**没有 410 正文**的卡返回 0（⛔ 不是「无差别返回 1」）");
        RuleCore.PumpActionQueues(ctx);
        Tr(!plain.Has("flank"), "A4′ 对照：泵完队它身上没有 `flank`（没有正文就真的什么都不发生）");
    }

    // ============================================================ B 账 1281/1379 (b)

    static void SectionB_手牌与牌库卡池()
    {
        MenuCheck.Section(_sink, "B 手牌 `50` / 手牌+牌库 `51` / 牌库 `65` 三个卡池的计数"
                              + "（判据 `AbilityLogic__GetTargets.c:918-978` ＋ `CheckIfRawCardMeetsCriteria.c:18-103`）");

        var ctx = NewCtx();
        var ps0 = ctx.Players[0];
        var ps1 = ctx.Players[1];
        ps0.Hand.Clear(); ps0.Deck.Clear();
        ps1.Hand.Clear(); ps1.Deck.Clear();

        var ownUnitH = ctx.NewInstance(Card("E3OwnHandUnit", "unit", "SpaceWolves", 1, 2, 2));
        var ownTactic = ctx.NewInstance(Card("E3OwnHandTactic", "tactic", "SpaceWolves", 2, 0, 0));
        ps0.Hand.Add(ownUnitH); ps0.Hand.Add(ownTactic);

        var ownDeckUnit1 = ctx.NewInstance(Card("E3OwnDeckUnit1", "unit", "SpaceWolves", 1, 1, 1));
        var ownDeckUnit2 = ctx.NewInstance(Card("E3OwnDeckUnit2", "unit", "SpaceWolves", 3, 1, 1));
        var ownDeckDefence = ctx.NewInstance(Card("E3OwnDeckDefence", "defence", "SpaceWolves", 0, 0, 0));
        ps0.Deck.Add(ownDeckUnit1); ps0.Deck.Add(ownDeckUnit2); ps0.Deck.Add(ownDeckDefence);

        var foeUnit = ctx.NewInstance(Card("E3FoeHandUnit", "unit", "Goff", 2, 2, 2));
        var foeTactic = ctx.NewInstance(Card("E3FoeHandTactic", "tactic", "Goff", 1, 0, 0));
        var foeDefence = ctx.NewInstance(Card("E3FoeHandDefence", "defence", "Goff", 0, 0, 0));
        var foeVehicle = ctx.NewInstance(Card("E3FoeHandVehicle", "unit", "Goff", 3, 2, 2));
        foeVehicle.Card.AttachKeywordId(10);                 // 原版 `keyword(+0x98) == 10`（Vehicle）
        ps1.Hand.Add(foeUnit); ps1.Hand.Add(foeTactic); ps1.Hand.Add(foeDefence); ps1.Hand.Add(foeVehicle);

        // B0 前提
        C(ps1.Hand.Count, 4, "B0 ★ 前提：敌方手牌 **4** 张（部队 · 战术 · 防御 · 载具）");
        C(ps0.Hand.Count, 2, "B0′ ★ 前提：我方手牌 **2** 张（部队 · 战术）");
        C(ps0.Deck.Count, 3, "B0″ ★ 前提：我方牌库 **3** 张（2 部队 · 1 防御）");

        string gap;

        // ---- B1 ta=50：单位 + 战术 + **防御**（`defence` 归 `Tactic(20)` —— 改前不算）----
        var s50 = Set(50, friendly: 0, enemy: 1, minions: 1, spells: 1);
        C(CountEval.CountTargets(ctx, 0, s50, 0, null, out gap), 4,
          "B1 🔴 `ta=50` 敌方手牌「部队+战术」⇒ **4** 张（含**防御卡**：原版 `MeetsCardTypeCriteria` 的 "
        + "`Tactic(20)` 含 `defence`/`enchantment`）"
        + "｜🧨 改回手写的 `c.Type == \"tactic\"` ⇒ 防御那 1 张不算 ⇒ 3 ⇒ 红");

        var s50min = Set(50, friendly: 0, enemy: 1, minions: 1, spells: 0);
        C(CountEval.CountTargets(ctx, 0, s50min, 0, null, out gap), 2,
          "B1′ `ta=50` 只要部队 ⇒ **2**（`E3FoeHandUnit` + `E3FoeHandVehicle`）"
        + "｜🧨 把卡种白名单忽略 ⇒ 4 ⇒ 红");

        // ---- B2 ta=50 + keywordFilter（改前整行 gap）----
        var s50kw = Set(50, friendly: 0, enemy: 1, minions: 1, spells: 1);
        s50kw.KeywordFilter = 10;                            // Vehicle
        C(CountEval.CountTargets(ctx, 0, s50kw, 0, null, out gap), 1,
          "B2 🔴 `ta=50` + `keywordFilter=10 (Vehicle)` ⇒ **1**（只有那张载具）"
        + "｜🧨 把 `CardPoolSetSupported` 的 `keywordFilter` 挡回去（旧 `HandSetSupported` 的行为）⇒ "
        + "整行 `null` ⇒ 红（正是 `GSC75 Roaming Outriders` 那条 `multiplier` 失效的原因）");
        Tr(!string.IsNullOrEmpty(gap), "B2′ 这一格**判得了**（返回非 null）⇒ 不该再留 `gap`（实得 gap=" + (gap ?? "null") + "）");

        // ---- B3 ta=51：手牌 + 牌库 ----
        var s51 = Set(51, friendly: 1, enemy: 0, minions: 1, spells: 0);
        C(CountEval.CountTargets(ctx, 0, s51, 0, null, out gap), 3,
          "B3 🔴 `ta=51` = 手牌部队（1）**＋** 牌库部队（2）⇒ **3**"
        + "｜🧨 只留手牌（或只留牌库）⇒ 1 / 2 ⇒ 红（`AbilityLogic__GetTargets.c:933-950` 是 `AddRange` 两池拼接）");

        // ---- B4 ta=65：只有牌库 ----
        var s65 = Set(65, friendly: 1, enemy: 0, minions: 0, spells: 1);
        C(CountEval.CountTargets(ctx, 0, s65, 0, null, out gap), 1,
          "B4 🔴 `ta=65` = **牌库**里的「法术」⇒ **1**（只有那张 `E3OwnDeckDefence`；手牌里那张战术**不该被算进牌库池**）"
        + "｜🧨 把 65 也并上手牌 ⇒ 2 ⇒ 红");

        // ---- B5 `maxTargets` 截断（走 `ApplyMaxTargets`，与棋盘那一支同一份）----
        var s50max = Set(50, friendly: 0, enemy: 1, minions: 1, spells: 1);
        s50max.MaxTargets = 2;
        C(CountEval.CountTargets(ctx, 0, s50max, 0, null, out gap), 2,
          "B5 🔴 `maxTargets=2`（命中 4 张）⇒ 截断后 **2**（`AbilityLogic__GetTargets.c:1164-1369` 的 `Take`）；"
        + "纯计数档个数 = `min(count, max)`"
        + "｜🧨 不截断 ⇒ 4 ⇒ 红");

        // ---- B6 `selectFromChooseResolution` 仍如实判不了（账 676 的 (a) 格同一根）----
        var s50sel = Set(50, friendly: 0, enemy: 1, minions: 1, spells: 1);
        s50sel.SelectFromChooseResolution = 1;
        var selRes = CountEval.CountTargets(ctx, 0, s50sel, 0, null, out gap);
        Tr(selRes == null && !string.IsNullOrEmpty(gap),
          "B6 🔴 `selectFromChooseResolution != 0` ⇒ **仍 `null` + 出声**（`ctx.CardChosenInSelection` 装的是 "
        + "`UnitState`，卡片候选接不上）—— ⛔ 不许静默当「不筛」而多数一遍（实得 " + (selRes.HasValue ? selRes.Value.ToString() : "null") + "）");
    }

    // ============================================================ C 账 1281/1379 (c)

    static void SectionC_墓地候选()
    {
        MenuCheck.Section(_sink, "C 墓地 `ta = 60` 的逐候选判据改走 `TargetCriteriaEval.MatchesRawCard`"
                              + "（改前 traits / 四组 stat / army / spellType / 稀有度**整条 gap**）");

        var ctx = NewCtx();
        ctx.DeadUnits.Clear();
        var cheap = Card("E3DeadCheap", "unit", "SpaceWolves", 1, 1, 1);
        var pricey = Card("E3DeadPricey", "unit", "SpaceWolves", 5, 1, 1);
        ctx.DeadUnits.Add(new DeadUnit { Card = cheap, Instance = ctx.NewInstance(cheap), Owner = 0, DeathTurn = 1 });
        ctx.DeadUnits.Add(new DeadUnit { Card = pricey, Instance = ctx.NewInstance(pricey), Owner = 0, DeathTurn = 1 });

        string gap;

        // C0 前提
        C(ctx.DeadUnits.Count, 2, "C0 ★ 前提：墓地里 **2** 张友方部队");

        var s60 = Set(60, friendly: 1, enemy: 0, minions: 1, spells: 0);
        C(CountEval.CountTargets(ctx, 0, s60, 0, null, out gap), 2,
          "C0′ ★ 前提：不加任何额外筛 ⇒ **2**（改前的既有行为，本笔不该动它）");

        // C1 费用组（改前整条 gap）
        var s60cost = Set(60, friendly: 1, enemy: 0, minions: 1, spells: 0);
        s60cost.ManaCostFilterOption = 2;                    // `StatEqualOrHigher`：`filter <= cost`
        s60cost.ManaCostFilter = 3;
        var costRes = CountEval.CountTargets(ctx, 0, s60cost, 0, null, out gap);
        Tr(costRes != null,
          "C1 ★ 前提：`manaCostFilterOption=2 / manaCostFilter=3` 这一格**判得了**（不是 gap）—— 实得 gap=" + (gap ?? "null"));
        C(costRes, 1,
          "C1 🔴 墓地 + 「费用 ≥ 3」⇒ **1**（只有那张 5 费；改前这一格整条 gap ⇒ 返回 null）"
        + "｜🧨 把 `CemeteryCandidatePasses` 改回手写「只看 keyword/白名单」⇒ 整条 gap ⇒ 红");

        // C2 阵营闸（由调用方 `SideMatches` 判，与卡面级无关）
        var s60foe = Set(60, friendly: 0, enemy: 1, minions: 1, spells: 0);
        C(CountEval.CountTargets(ctx, 0, s60foe, 0, null, out gap), 0,
          "C2 🔴 同一墓地、只要**敌方** ⇒ **0**（阵营闸仍在调用方，⛔ 别被卡面级那一层吃掉）");

        // C3 卡种闸（`includeMinions == 0` ⇒ 墓地里一张都不算）
        var s60noMinion = Set(60, friendly: 1, enemy: 0, minions: 0, spells: 1);
        C(CountEval.CountTargets(ctx, 0, s60noMinion, 0, null, out gap), 0,
          "C3 🔴 只要法术（`includeMinions=0`）⇒ 墓地里的部队**不算**（卡种白名单生效）"
        + "｜🧨 忽略卡种白名单 ⇒ 2 ⇒ 红");
    }
}
