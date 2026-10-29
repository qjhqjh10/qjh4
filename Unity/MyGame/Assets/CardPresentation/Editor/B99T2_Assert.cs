// B99T2_Assert.cs — 执行代理 `B99_T2`（第九十九会话 · 引擎 · `TargetsAffected` 档位补齐）的断言集
//
// ============================ 它盯的是什么 ============================
//   施工判据 = `资料/完全复刻/判据/B99_R4b_引擎与工具.md` §三·3·1 的 ① ③ ④（② 的结算路接线不在本件白名单内）：
//
//   ① **`CountEval.CountSet` 补 `ta 20 / 30 / 90`**（`20 self` · `30 target` · `90 targetIfMeetsCriteria`）
//      判据（🥇 `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c`）：
//        · `20`（`:184-191`，`iVar6 == 0x14`）：池 = `[thisCard]`、**不筛不裁**，建完就 `goto LAB_180818485`（return）
//          ⇒ 个数恒 1，**且不看 `friendlyUnits`/`enemyUnits`**（A2 咬这一条）；
//        · `30`（`:192-206`，`iVar6 == 0x1e`）：池 = `[targetCard]`（**null ⇒ 空池**）、不筛不裁
//          ⇒ 个数 = `targetCard != null ? 1 : 0`，**同样不看阵营闸**（B4 咬这一条）；
//        · `90`（`:747-759`，`iVar6 == 0x5a`）：池 = `[targetCard]`，**然后不 return**，落 `:1086-1095` 的
//          逐候选筛（`FilterMethods.CheckIfMeetsCriteria`）⇒ 阵营闸**要判**（C2 咬这一条）。
//
//   ③ **修 `ApplyMaxTargets` 两处**（`AbilityLogic__GetTargets.c:1164-1365` + 🥈
//      `out_il2csharp/Assembly-CSharp/AbilityLogic.cs:4021-4112`）：
//        · **method 1（highestAttack）的键链** = `OrderBy(isRemnant).ThenByDescending(GetHighestAttackValue)`
//          （il2csharp `:4059`）—— 改前**漏了 `isRemnant` 主键**，注释还把它写成 `CurrentIntrinsicHealth`
//          （错：那是 `6 lowestHealth` 的键 `b__26_2`）。E1/E2 用「残骸 vs 非残骸」把主键咬住。
//        · **method 4（cardTarget）**：原版 `targetSelectMethod - 1` 的 switch 里 `case 3: goto L_180819895`
//          （il2csharp `:4091-4093`）⇒ **它就是随机抽**，不是「判据未取到」。F1 断言报错文案说清了这件事。
//
//   ④ **补 `ta 52`（inHandAndPlayFiltered）**：池 = 手牌 ∪ 场上（`GetTargets.c:920-948`，后者
//      `GetUnitsInPlayList(…, clearList = 0, …)` 是**追加**）⇒ D1 断言「两半都算了」（少任何一半都不等于 3）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · A1/A2：把 `20` 改成「走常规 36 格筛」⇒ A2（两阵营闸都关）会从 1 变 0 ⇒ **A2 红**。
//   · B1/B2：把 `30` 的 `target == null` 改成「以 thisCard 兜底」⇒ B2 从 0 变 1 ⇒ **B2 红**。
//   · B4：把 `30` 做成「落常规筛」⇒ 敌方目标被阵营闸淘汰 ⇒ 从 1 变 0 ⇒ **B4 红**。
//   · C2：把 `90` 合并回 `30`（`return ReferenceEquals`）⇒ 敌方目标也过 ⇒ 从 0 变 1 ⇒ **C2 红**。
//   · D1：把 `52` 只数手牌（= 退回 `50`）⇒ 2 ≠ 3 ⇒ **D1 红**；只数场上 ⇒ 1 ≠ 3 ⇒ **D1 红**。
//   · E1：把 `isRemnant` 主键删掉（回到「只按攻降序」）⇒ 选中的是敌方高攻那一张 ⇒ `sum = +1 ≠ -1`
//          ⇒ `MeetsBoardCriteria` 从 true 变 false ⇒ **E1 红**；E2 是它的因果对照（去掉残骸标记后**必须**变假）。
//   · F1：把 `cardTarget(4)` 改回旧文案「判据未取到」/ 或把它当成别的支 ⇒ 文案不再同时含
//          `cardTarget` 与「随机」⇒ **F1 红**。
//
// ============================ 怎么用（宿主）============================
//   `B99T2_Assert.Run(); _sink.Pass += B99T2_Assert.Pass; _sink.Fail += B99T2_Assert.Fail;`
//   · 推荐宿主 = `RuleEngine/Editor/RuleEngineTest.cs`（`RuleEngineTest.Run`）—— 本件**只碰纯引擎**，
//     不建 GameObject、不建资产、不需要场景。
//   · ⚠️ 计数字段口径与同族一致：`Pass` / `Fail` / `RunCount`。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B99_T2`：`CountSet` 的 `ta = 20 / 30 / 90 / 52` 四档 ＋ `ApplyMaxTargets` 的两处修正。</summary>
public static class B99T2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[T2] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }
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
        return new List<CardDef> { Warlord("B99T2Hero_" + faction, faction) };
    }

    static BattleContext NewCtx()
    {
        return RuleCore.NewBattle(DeckOf("SpaceWolves"), DeckOf("Goff"), seed: 0, shuffle: false);
    }

    /// <summary>白盒放盘（同 `B99E3_Assert.Place`；⛔ 不走棋盘钩子）。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>手搓一张卡。⚠️ `cardTraits` / `keyword` 两列**必须显式挂**（同 `B99E3_Assert.Card` 的理由：
    /// 不挂会让 `TargetCriteriaEval.MatchesRawCard` 的 traits 那一格判成「判不了」）。</summary>
    static CardDef Card(string name, string type, string faction, int cost, int attack, int health)
    {
        var def = new CardDef(name, name, type, "", "common", faction, cost, attack, health, 0, null,
                              subtype: type == "unit" ? "troop" : null);
        def.AttachCardTraits(new string[0]);
        def.AttachKeywordId(0);
        return def;
    }

    /// <summary>手搓 `countSet` —— 其余维度**必须显式填 0**（`CountSetSpec` 的默认是 `Unknown(-1)`）。</summary>
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

        MenuCheck.Section(_sink, "B99_T2 `CountSet` 的 ta=20/30/90/52 ＋ `ApplyMaxTargets` 的 isRemnant 主键 / cardTarget 口径"
                              + "（判据 `判据/B99_R4b_引擎与工具.md` §三·3·1 ①③④）");

        SectionA_Self20();
        SectionB_Target30();
        SectionC_TargetIfMeets90();
        SectionD_InHandAndPlay52();
        SectionE_MaxTargetsHighestAttack();
        SectionF_MaxTargetsCardTarget();

        Debug.Log(_sink.P + "B99T2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A `ta = 20 self`

    static void SectionA_Self20()
    {
        MenuCheck.Section(_sink, "A `ta=20 self`：池 = `[thisCard]`、不筛不裁（`AbilityLogic__GetTargets.c:184-191`）");

        var ctx = NewCtx();
        var self = Place(ctx, 0, 0, Card("B99T2Self", "unit", "SpaceWolves", 1, 2, 3));

        // A0 前提硬断言：actor 真的是盘上那一个
        C(ReferenceEquals(ctx.Players[0].Board[0], self), true,
          "A0 ★ 前提：`self` 就在 0 号位 0 号槽上（后面两条都拿它当 thisCard）");

        var s20 = Set(20, 1, 0, 1, 0);
        C(CountEval.CountTargets(ctx, 0, s20, 0, self, out string g20), 1,
          "A1 🔴 `ta=20 self` 的个数 = **1**（原版池 = `[thisCard]` 建完就 return）｜🧨 若改走常规 36 格筛，"
        + "`SideEmpty` / 卡种位会把这一格判成 0 ⇒ 红");

        // A2 灭自证：两个阵营闸都关 —— 原版这一档**根本不看**它们（建完池直接 return）
        var s20empty = Set(20, 0, 0, 0, 0);
        C(CountEval.CountTargets(ctx, 0, s20empty, 0, self, out string g20b), 1,
          "A2 🔴 `ta=20` 在 `friendlyUnits = enemyUnits = 0`（两个阵营闸都关）时**仍是 1** —— 判据是"
        + "「原版 `:184-191` 建完池就 `goto LAB_180818485`」，⛔ 不是 `:210-232` 的阵营闸｜"
        + "🧨 若把 20 改成落常规筛 ⇒ 这里变 0 ⇒ 红（A1 仍绿 = 只测到「有载体」，A2 才咬住「不筛」）");
    }

    // ============================================================ B `ta = 30 target`

    static void SectionB_Target30()
    {
        MenuCheck.Section(_sink, "B `ta=30 target`：池 = `[targetCard]`（null ⇒ **空池**、不以自己兜底）、不筛不裁"
                              + "（`AbilityLogic__GetTargets.c:192-206`）");

        var ctx = NewCtx();
        var self = Place(ctx, 0, 0, Card("B99T2Self30", "unit", "SpaceWolves", 1, 2, 3));
        var foe = Place(ctx, 1, 0, Card("B99T2Foe30", "unit", "Goff", 1, 2, 3));

        C(ReferenceEquals(ctx.Players[1].Board[0], foe), true,
          "B0 ★ 前提：`foe` 在 1 号玩家 0 号槽上（后面用它当 targetCard）");

        var s30 = Set(30, 1, 0, 1, 0);   // 只有己方阵营闸开着 —— 用来咬住「30 不筛」
        C(CountEval.CountTargets(ctx, 0, s30, 0, self, out string g1, foe, true), 1,
          "B1 🔴 `ta=30` 有 targetCard ⇒ 个数 **1**");

        C(CountEval.CountTargets(ctx, 0, s30, 0, self, out string g2, null, true), 0,
          "B2 🔴 `ta=30` 且 `targetKnown` 但 target == **null** ⇒ 个数 **0**（原版 `:192-206`：null ⇒ 空池）｜"
        + "🧨 若改成「null ⇒ 以 thisCard 兜底」⇒ 变 1 ⇒ 红");

        var r3 = CountEval.CountTargets(ctx, 0, s30, 0, self, out string g3);
        Tr(r3 == null && !string.IsNullOrEmpty(g3) && g3.Contains("targetCard"),
          "B3 🔴 没喂 `targetCard`（`targetKnown == false`）⇒ **null ＋ 出声**，文案点名 `targetCard` —— ⛔ 不拿 "
        + "`ctx.LastTarget` / `LastTargets[0]` 顶（那是代词槽）。实际 gap = 「" + (g3 ?? "<null>") + "」");

        C(CountEval.CountTargets(ctx, 0, s30, 0, self, out string g4, foe, true), 1,
          "B4 🔴 `ta=30` 的 targetCard 是**敌方**单位、而 criteria 只开 `friendlyUnits = 1` ⇒ **仍是 1** —— "
        + "判据是「原版 30 建完池就 return，不看 `:210-232` 的阵营闸」｜🧨 若把 30 做成落常规筛 ⇒ 变 0 ⇒ 红");
    }

    // ============================================================ C `ta = 90 targetIfMeetsCriteria`

    static void SectionC_TargetIfMeets90()
    {
        MenuCheck.Section(_sink, "C `ta=90 targetIfMeetsCriteria`：池 = `[targetCard]` 后**落常规筛**"
                              + "（`AbilityLogic__GetTargets.c:747-759` → `:1086-1095`）");

        var ctx = NewCtx();
        var self = Place(ctx, 0, 0, Card("B99T2Self90", "unit", "SpaceWolves", 1, 2, 3));
        var mine = Place(ctx, 0, 1, Card("B99T2Mine90", "unit", "SpaceWolves", 1, 2, 3));
        var foe = Place(ctx, 1, 0, Card("B99T2Foe90", "unit", "Goff", 1, 2, 3));

        C(ReferenceEquals(ctx.Players[0].Board[1], mine) && ReferenceEquals(ctx.Players[1].Board[0], foe), true,
          "C0 ★ 前提：己方候选与敌方候选**各在盘上**（C1/C2 是同一条 criteria 只换 target）");

        var s90 = Set(90, 1, 0, 1, 0);   // 只开己方阵营闸
        C(CountEval.CountTargets(ctx, 0, s90, 0, self, out string gc1, mine, true), 1,
          "C1 🔴 `ta=90`、target = **己方**单位、`friendlyUnits = 1` ⇒ 个数 **1**（身份相符后过阵营/卡种筛）");

        C(CountEval.CountTargets(ctx, 0, s90, 0, self, out string gc2, foe, true), 0,
          "C2 🔴 同一条 criteria、target 换成**敌方**单位 ⇒ 个数 **0**（90 档的候选**要过** `:210-232` 阵营闸）｜"
        + "🧨 若把 90 合并回 30（只比身份就 return）⇒ 变 1 ⇒ 红（C1 仍绿，只有 C2 咬得住）");

        var r3 = CountEval.CountTargets(ctx, 0, s90, 0, self, out string gc3);
        Tr(r3 == null && !string.IsNullOrEmpty(gc3) && gc3.Contains("targetCard"),
          "C3 🔴 `ta=90` 没喂 targetCard ⇒ null ＋ 出声（同 B3）。实际 gap = 「" + (gc3 ?? "<null>") + "」");
    }

    // ============================================================ D `ta = 52 inHandAndPlayFiltered`

    static void SectionD_InHandAndPlay52()
    {
        MenuCheck.Section(_sink, "D `ta=52 inHandAndPlayFiltered`：池 = 手牌 ∪ **场上**（两半都要过同一份 criteria）"
                              + "（`AbilityLogic__GetTargets.c:920-948`）");

        var ctx = NewCtx();
        // 先清掉开局抓到的牌，夹具才可数（⛔ 不改引擎，只清这一局的容器）
        ctx.Players[0].Hand.Clear();
        ctx.Players[1].Hand.Clear();

        var handA = ctx.NewInstance(Card("B99T2HandA", "unit", "SpaceWolves", 1, 2, 3));
        var handB = ctx.NewInstance(Card("B99T2HandB", "unit", "SpaceWolves", 1, 2, 3));
        var foeHand = ctx.NewInstance(Card("B99T2FoeHand", "unit", "Goff", 1, 2, 3));
        ctx.Players[0].Hand.Add(handA);
        ctx.Players[0].Hand.Add(handB);
        ctx.Players[1].Hand.Add(foeHand);
        var onBoard = Place(ctx, 0, 2, Card("B99T2Board52", "unit", "SpaceWolves", 1, 2, 3));

        // D0 前提：手牌 / 场上确实各有几件（⛔ 不写成 `if (前提) Check(...)`）
        C(ctx.Players[0].Hand.Count, 2, "D0 ★ 前提：己方手牌 **2** 张（另 1 张在敌方手里）");
        C(ReferenceEquals(ctx.Players[0].Board[2], onBoard), true, "D0 ★ 前提：己方 2 号槽上有 1 个场上单位");

        var s52 = Set(52, 1, 0, 1, 0);   // 手牌 2 + 场上 1 = 3（敌方手牌那张不算）
        C(CountEval.CountTargets(ctx, 0, s52, 0, null, out string g52), 3,
          "D1 🔴 `ta=52` 的个数 = **3** = 手牌 2 ＋ 场上 1（同一个 criteria 两半都过）｜"
        + "🧨 只数手牌（= 退回 `50`）⇒ 2 ⇒ 红；只数场上 ⇒ 1 ⇒ 红");

        var s52empty = Set(52, 0, 0, 1, 0);
        C(CountEval.CountTargets(ctx, 0, s52empty, 0, null, out string g52b), 0,
          "D2 🔴 `ta=52` 两个阵营闸都关 ⇒ **0**（原版两半的取池都按这一对 bool，都关就是空池）");
    }

    // ============================================================ E `ApplyMaxTargets` method 1 的 isRemnant 主键

    static void SectionE_MaxTargetsHighestAttack()
    {
        MenuCheck.Section(_sink, "E `maxTargets` 的 `targetSelectMethod = 1 (highestAttack)`："
                              + "键链 = `OrderBy(isRemnant).ThenByDescending(GetHighestAttackValue)`（il2csharp `AbilityLogic.cs:4059`）");

        // 局面：己方 A 攻 5（非残骸）· 敌方 B 攻 9（**残骸**）
        //  · 带 isRemnant 主键：A(非残骸=0) 排在 B(残骸=1) 前 ⇒ Take(1) 选中 **A**（己方）⇒ 同方 −1 ⇒ 净和 −1
        //  · 漏掉主键（改前）：按攻降序 ⇒ 选中 **B**（敌方）⇒ 异方 +1 ⇒ 净和 +1
        // ⇒ 只有「主键真的参与了排序」时 `EqualTo(-1)` 才成立。
        var ctx = NewCtx();
        var a = Place(ctx, 0, 0, Card("B99T2A_att5", "unit", "SpaceWolves", 1, 5, 5));
        var b = Place(ctx, 1, 0, Card("B99T2B_att9", "unit", "Goff", 1, 9, 9));
        b.IsRemnant = true;

        var set = Set(10, 1, 1, 1, 0);
        set.IncludeRemnants = 1;         // 残骸闸（`CheckIfMeetsCriteria.c:255-261`）：不开的话 B 在筛阶段就被淘汰
        set.MaxTargets = 1;
        set.TargetSelectMethod = 1;      // highestAttack

        var gate = new CardDef.BoardGateSpec
        {
            CountOption = 10,            // EnemiesMinusAllies：同方 −1 / 异方 +1
            StatOption = 1,              // EqualTo
            ReferenceValue = -1,
            CountSet = set,
        };

        // E0 前提：两个候选真的都在盘上、且 B 真的是残骸
        C(ReferenceEquals(ctx.Players[0].Board[0], a) && ReferenceEquals(ctx.Players[1].Board[0], b), true,
          "E0 ★ 前提：A（己方/攻 5）与 B（敌方/攻 9/残骸）各在盘上");
        C(b.IsRemnant, true, "E0 ★ 前提：B 的 `IsRemnant` 已置位");

        bool? r1 = CountEval.MeetsBoardCriteria(ctx, 0, gate, null, out string ge1);
        Tr(r1 == true,
          "E1 🔴 净和 == **−1** ⇒ `EqualTo(−1)` 成立（= 选中了**非残骸**的 A）｜"
        + "🧨 把 `isRemnant` 主键删掉（回到「只按攻降序」）⇒ 选中攻 9 的敌方 B ⇒ 净和 +1 ⇒ 变 false ⇒ 红"
        + "（gap = 「" + (ge1 ?? "") + "」）");

        // E2 灭自证（因果对照）：同一局面、只把 B 的残骸标记撤掉 ⇒ 排序退回「按攻降序」⇒ 必选 B ⇒ 净和 +1 ⇒ 假
        b.IsRemnant = false;
        bool? r2 = CountEval.MeetsBoardCriteria(ctx, 0, gate, null, out string ge2);
        Tr(r2 == false,
          "E2 🔴 只把 B 的 `IsRemnant` 撤掉（其余一字不改）⇒ 净和变 **+1** ⇒ `EqualTo(−1)` **不成立** —— "
        + "这条证明 E1 的差**就是** `isRemnant` 主键带来的，不是别的格（gap = 「" + (ge2 ?? "") + "」）");
    }

    // ============================================================ F `targetSelectMethod = 4 (cardTarget)`

    static void SectionF_MaxTargetsCardTarget()
    {
        MenuCheck.Section(_sink, "F `targetSelectMethod = 4 (cardTarget)`：原版**就落随机支**"
                              + "（il2csharp `AbilityLogic.cs:4021` 的 `method-1` switch → `:4091-4093 case 3: goto L_180819895`）");

        var ctx = NewCtx();
        Place(ctx, 0, 0, Card("B99T2F_a", "unit", "SpaceWolves", 1, 5, 5));
        Place(ctx, 0, 1, Card("B99T2F_b", "unit", "SpaceWolves", 1, 6, 6));

        var set = Set(10, 1, 0, 1, 0);
        set.MaxTargets = 1;
        set.TargetSelectMethod = 4;      // cardTarget —— 原版就是随机抽（不是「口径未取到」）
        var gate = new CardDef.BoardGateSpec
        {
            CountOption = 10, StatOption = 1, ReferenceValue = -1, CountSet = set,
        };

        bool? r = CountEval.MeetsBoardCriteria(ctx, 0, gate, null, out string gf);
        Tr(r == null && !string.IsNullOrEmpty(gf) && gf.Contains("cardTarget") && gf.Contains("随机"),
          "F1 🔴 `method=4` 仍如实判不了（本版随机**选择链**未建模），但**文案必须写清「原版就是随机抽」** —— "
        + "旧文案「`cardTarget` 的真实口径判据未取到」已过期（判据 = il2csharp `:4091-4093` 的 `case 3: goto` 随机支）｜"
        + "🧨 把文案改回「判据未取到」或把它当成非随机支 ⇒ 不再同时含 `cardTarget` 与「随机」⇒ 红"
        + "（实际 gap = 「" + (gf ?? "<null>") + "」）");
    }
}
