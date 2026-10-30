// B104_MT_Assert.cs — 执行代理 `B104_MT`（第一百零四会话 · MT-b）
//
// ============================ 它盯的是什么 ============================
//   施工单 `资料/完全复刻/判据/B104_T21与多目标_时间线_施工单.md` 的 **MT-b**：
//   「多目标生产端的目标表」—— `RuleCore.AbilityTargetsFor` 改前**恒 ≤ 1 个目标、从不读 `ta`**。
//   本批在 `RuleCore` 类尾新增 `AbilityTargetListFromCriteria`（照原版 `AbilityLogic.GetTargets`），
//   并让 `AttachAbilityTargets` 收一份可选的 `countSet`（`CardDef.CountSetSpec`）、
//   由 `UseActiveAbilityCore` 把**这一行**的 `activeAbilityTargets` 递进去。
//
//   判据（🥇）= `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c`（全文 1,427 行，逐句现读）：
//     `ta` 建池 `:184-1079` · 逐候选筛 `:1081-1163` · `maxTargets` 截断 `:1164-1371`；
//     字段表 `d:/2/tools/il2cpp_out/dump.cs:23823-23872`。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   A1 只留「建池 + 白名单」、不看 `friendlyUnits/enemyUnits` ⇒ A2（敌方那一侧）会变 4 ⇒ 红。
//   A3-a 不做 `maxTargets` 截断 ⇒ 3 个候选原样返回 ⇒ 红。
//   A3-b 截断不走 `ctx.GetRandomInt`（比如 `ctx.Rng.Next` / 直接取前 N）⇒ **随机表游标不前进** ⇒ 红。
//   A4 `targetSelectMethod = 7 firstAdded` 被写成随机 ⇒ 抓到的不是槽号 1/2（池序前两个）⇒ 红。
//   A6 `null`（判不了/没接）与**空表**混成一种 ⇒ `ta=30 无目标` / `ta=65` / `ta=0` 三条会互相打架 ⇒ 红。
//   A8 `ta=2/3 valueInPlay`（本版没有 `GetValueInPlay` 等价字段）被拿随机冒充 ⇒ 会返回 1 个而不是 `null` ⇒ 红。
//   B1 **接线那一跳**（`UseActiveAbilityCore` → `AttachAbilityTargets(…, criteria)`）被退回 5 实参
//      ⇒ 事件里的表走 `AbilityTargetsFor`（`chosen == null` ⇒ **空表**）⇒ 数出 0 而不是 4 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   纯引擎侧（**不碰 `Unity` 场景、不碰 `BattleDriver`**）⇒ 接在任意一个跑在 **Editor 程序集**的自检宿主里都行，
//   建议接在 `CardPresentation/Editor/RuleEngineTest.cs`（或它的分部文件）的某个断言段**末尾**，同一物理行末尾追加：
//     ` B104_MT_Assert.Run(); pass += B104_MT_Assert.Pass; fail += B104_MT_Assert.Fail;`
//   ⚠️ `Pass` / `Fail` 是**累计值** ⇒ 只能加一次；⛔ 别在两个宿主里各调一次（同一个静态 sink）。
//   ⚠️ 本文件**不动**任何 `Editor/*Scene.cs`（调度台接线）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B104_MT`：MT-b（多目标生产端目标表）的断言。合成夹具 —— 见文件头。</summary>
public static class B104_MT_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B104_MT] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 夹具

    /// <summary>`subtype` 给 `"Warlord"`（真卡 56 个督军全是），否则会掉进「原版没给兵种」那一类。</summary>
    static CardDef Hero(string id) =>
        new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0, null, subtype: "Warlord");

    static CardDef PlainUnit(string id) =>
        new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 3, 0, (string[])null);

    static BattleContext NewCtx(IEnumerable<CardDef> cardPool)
    {
        var d0 = new List<CardDef> { Hero("B104MT_w0") };
        for (int i = 0; i < 20; i++) d0.Add(PlainUnit("B104MT_f" + i));
        var d1 = new List<CardDef> { Hero("B104MT_w1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>(cardPool));
    }

    /// <summary>⚠️ 督军格要 `warlord: true`：`UnitState.IsWarlord` 是**构造期只读**（`UnitState.cs:51/886`），
    /// 光把一张 `type = hero` 的卡摆到 4 号格**不会**让它变成督军。</summary>
    static UnitState Put(BattleContext ctx, int p, int slot, CardDef card, bool warlord = false)
    {
        var u = new UnitState(ctx.NewInstance(card), warlord);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>一份**14 格都已知**的 `countSet`（`Unknown` 会让 `TargetCriteriaEval.Matches` 回「判不了」，
    /// 那是另一档语义 —— 这里要的是「确定值」）。字段顺序与 `CardDef.CountSetSpec` 逐字对齐。</summary>
    static CardDef.CountSetSpec Set(int ta, int friendly, int enemy, int hero, int minions, int self,
                                    int maxTargets = 0, int method = 0)
    {
        var s = new CardDef.CountSetSpec();
        s.PlayerTurn = 0;
        s.TargetsAffected = ta;
        s.ChosenArmy = 0; s.ArmyFilter = 0; s.KeywordFilter = 0; s.TraitsFilter = 0;
        s.MaxTargets = maxTargets; s.TargetSelectMethod = method;
        s.AttackFilter = 0; s.AttackFilterOption = 0;
        s.CurrentHealthFilter = 0; s.CurrentHealthFilterOption = 0;
        s.ManaCostFilter = 0; s.ManaCostFilterOption = 0;
        s.RarityFilter = 0; s.RarityFilterOption = 0;
        s.SpellType = 0;
        s.FriendlyUnits = friendly; s.EnemyUnits = enemy;
        s.IncludeHero = hero; s.IncludeMinions = minions;
        s.IncludeSpells = 0; s.IncludeWhispers = 0; s.IncludeWeapons = 0;
        s.IncludeRemnants = 0; s.IncludeHeroParts = 0; s.IncludeSelf = self;
        s.ExcludeNotSelf = 0; s.ExcludeSimilarCards = 0; s.ExcludeTargetted = 0;
        s.ExcludeFriendlyHero = 0; s.ExcludeEnemyHero = 0;
        s.ExcludeRace = 0; s.ExcludeTrait = 0; s.SelectFromChooseResolution = 0;
        s.FilterSpecificCardIds = new string[0];
        s.ExcludeEnchantmentIds = new string[0];
        return s;
    }

    static string Slots(List<BattleEventTarget> l)
    {
        if (l == null) return "(null)";
        var b = new System.Text.StringBuilder();
        for (int i = 0; i < l.Count; i++) { if (i > 0) b.Append(','); b.Append(l[i].Player).Append(':').Append(l[i].Slot); }
        return b.ToString();
    }

    // ============================================================ 入口

    public static void Run()
    {
        MenuCheck.Section(_sink, "B104_MT：MT-b 多目标生产端目标表（`AbilityTargetListFromCriteria`）");
        SectionA_Criteria();
        SectionB_EndToEnd();
        Debug.Log(_sink.P + "B104_MT 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · `AbilityTargetListFromCriteria` 逐档

    static void SectionA_Criteria()
    {
        // 棋盘：我方 0 号位：2/4(督军)/5 三张 + 施放者 3 号位（共 4 张）；敌方 1 号位：4(督军)/6 两张。
        BattleContext MakeCtx()
        {
            var ctx = NewCtx(new CardDef[0]);
            ctx.Active = 0;
            Put(ctx, 0, 2, PlainUnit("B104MT_a2"));
            Put(ctx, 0, 5, PlainUnit("B104MT_a5"));
            Put(ctx, 0, 4, Hero("B104MT_h0"), warlord: true);
            Put(ctx, 1, 4, Hero("B104MT_h1"), warlord: true);
            Put(ctx, 1, 6, PlainUnit("B104MT_e6"));
            return ctx;
        }

        // ---- A1：`ta=10 allFiltered` 己方（含督军+部队+自己）⇒ 4 张 ----
        {
            var ctx = MakeCtx();
            var src = Put(ctx, 0, 3, PlainUnit("B104MT_src"));
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(
                ctx, 0, Set(10, friendly: 1, enemy: 0, hero: 1, minions: 1, self: 1), src, null);
            Tr(list != null, "A1 （前提）`ta=10` 这一档本批**接了**（不是 `null`）");
            C(list == null ? -1 : list.Count, 4,
              "A1 **`ta=10 allFiltered` 己方（含督军／部队／自己）⇒ 4 张**"
            + "｜判据 `AbilityLogic__GetTargets.c:972-990` → `GetUnitsInPlayPooledList.c:47-86`"
            + "（`friendlyUnits` +0x14 · `includeHero` +0x16 · `includeMinions` +0x17）"
            + "｜🧨 建池时写死 `troopOnly`（只取部队）⇒ 少掉督军 ⇒ 3 ⇒ 红；只看己方不看 `enemyUnits` 的那一半由 A2 抓");
            Tr(list != null && list.TrueForAll(t => t.Player == 0),
              "A1-b **只收己方**（`enemyUnits = 0`）⇒ 敌方那 2 张一张都不在表里（" + Slots(list) + "）");
        }

        // ---- A2（灭自证）：同一套 criteria 只看敌方那一侧 ⇒ 2 张，且全在 1 号位 ----
        {
            var ctx = MakeCtx();
            var src = Put(ctx, 0, 3, PlainUnit("B104MT_src"));
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(
                ctx, 0, Set(10, friendly: 0, enemy: 1, hero: 1, minions: 1, self: 0), src, null);
            C(list == null ? -1 : list.Count, 2,
              "A2 **`ta=10` 只看 `enemyUnits` ⇒ 2 张（敌方督军 + 敌方部队）**"
            + "｜🧨 建池时把 `friendlyUnits` 写成常数 / 不看 `enemyUnits` ⇒ 变 4 ⇒ 红");
            Tr(list != null && list.TrueForAll(t => t.Player == 1),
              "A2-b 全在敌方（1 号位）：" + Slots(list));
        }

        // ---- A3：`ta=10` + `maxTargets=1` + `targetSelectMethod=0 (random)` ⇒ 1 张，且【推进一格】随机表 ----
        {
            var ctx = MakeCtx();
            var src = Put(ctx, 0, 3, PlainUnit("B104MT_src"));
            int before = ctx.RandomNumbersIndex;
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(
                ctx, 0, Set(10, 1, 0, 1, 1, 1, maxTargets: 1, method: 0), src, null);
            C(list == null ? -1 : list.Count, 1,
              "A3 **`maxTargets = 1` + `random(0)` ⇒ 截到 1 张**"
            + "｜判据 `AbilityLogic__GetTargets.c:1164-1273`（`pool.Count > maxTargets` 才进截断；`default` 支逐抽）"
            + "｜🧨 不截断 ⇒ 变 4 ⇒ 红");
            C(ctx.RandomNumbersIndex - before, 1,
              "A3-b **截断真的走了 `ctx.GetRandomInt`**（随机表游标 **+1**，不是 `ctx.Rng.Next` / 直接取前 N）"
            + "｜判据 `:1258` `BattleManager__GetRandomInt(manager, 0, pool.Count)`（开局预烧 100 个整数 + 按下标取）"
            + "｜🧨 换成池序前 N 或 `ctx.Rng.Next` ⇒ 游标不动 / 动的地方不对 ⇒ 红");
        }

        // ---- A4：`targetSelectMethod = 7 (firstAdded)` ⇒ 取**池序前两个**（槽号 2 / 4），不是随机 ----
        {
            var ctx = MakeCtx();
            var src = Put(ctx, 0, 3, PlainUnit("B104MT_src"));
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(
                ctx, 0, Set(10, 1, 0, 1, 1, 1, maxTargets: 2, method: 7), src, null);
            string got = Slots(list);
            Tr(list != null && list.Count == 2 && list[0].Slot == 2 && list[1].Slot == 3,
              "A4 **`targetSelectMethod = 7 firstAdded` ⇒ 池序前两个（2 号格 / 3 号格），不是随机**"
            + "（实得 " + got + "）"
            + "｜判据 `AbilityLogic__GetTargets.c:1339-1361`（表头取 N）"
            + "｜🧨 把它也丢进随机支 ⇒ 抓到的槽号随机 ⇒ 红");
        }

        // ---- A5：`ta=30 target` ⇒ 就是玩家点的那个；**没有点选目标 ⇒ 空表（不是 null）** ----
        {
            var ctx = MakeCtx();
            var src = Put(ctx, 0, 3, PlainUnit("B104MT_src"));
            var chosen = ctx.Players[1].Board[6];
            var one = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(30, 0, 0, 0, 0, 0), src, chosen);
            Tr(one != null && one.Count == 1 && one[0].Player == 1 && one[0].Slot == 6,
              "A5 **`ta=30 target` ⇒ 只有玩家点的那一个**（实得 " + Slots(one) + "）"
            + "｜判据 `:192-207`（整张函数只有 `30`/`220` 读 `param_5`）"
            + "｜🧨 去做「敌方全体」⇒ 张数变 2 ⇒ 红");
            var none = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(30, 0, 0, 0, 0, 0), src, null);
            Tr(none != null && none.Count == 0,
              "A5-b **`ta=30` 但这次没有点选目标 ⇒ 空表**（⛔ 不是 `null`：`null` 专给「本版判不了／没接」）"
            + "｜判据 `:192-207`（`param_5 == null` ⇒ 一个都不加）"
            + "｜🧨 拿 `null` 冒充空表 ⇒ 调用方会退回单数路、把「原版确实没有目标」翻成别的语义 ⇒ 红");
        }

        // ---- A6：`ta=20 self` ⇒ 施放者自己 ----
        {
            var ctx = MakeCtx();
            var src = Put(ctx, 0, 3, PlainUnit("B104MT_src"));
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(20, 0, 0, 0, 0, 0), src, null);
            Tr(list != null && list.Count == 1 && list[0].Player == 0 && list[0].Slot == 3,
              "A6 **`ta=20 self` ⇒ 就是写这条 logic 的那张卡（施放者）**（实得 " + Slots(list) + "）"
            + "｜判据 `:184-191`｜🧨 换成 `chosen` / 己方全体 ⇒ 红");
        }

        // ---- A7：`ta=110 adjacentToTargetIfMeetsCriteria` ⇒ 目标的左右邻（含督军），锚点自己不算 ----
        {
            var ctx = MakeCtx();
            var src = Put(ctx, 0, 3, PlainUnit("B104MT_src"));
            var anchor = Put(ctx, 1, 5, PlainUnit("B104MT_anchor"));      // 敌方 5 号格：邻居 = 4(督军) / 6(部队)
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(
                ctx, 0, Set(110, 0, 1, 1, 1, 0), src, anchor);
            string got = Slots(list);
            Tr(list != null && list.Count == 2 && list.TrueForAll(t => t.Player == 1) && list.TrueForAll(t => t.Slot == 4 || t.Slot == 6),
              "A7 **`ta=110` ⇒ 目标的**左右邻**（敌方 4 号督军 / 6 号部队），锚点自己不进表**（实得 " + got + "）"
            + "｜判据 `:1046-1060`（`GetAdjacentUnits(targetCard)`）+ `:1081-1163`（邻居也要过整份 36 格）"
            + "｜🧨 把锚点也放进去 ⇒ 3 张 ⇒ 红；把邻居的阵营筛漏掉 ⇒ 红");
            var none = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(110, 0, 1, 1, 1, 0), src, null);
            Tr(none != null && none.Count == 0,
              "A7-b **`ta=110` 但没有目标 / 目标不在棋盘上 ⇒ 空表**（原版 `GetAdjacentUnits(null)` 那一支同样什么都不加）"
            + "｜🧨 退回「整个池子」⇒ 变 2 ⇒ 红");
        }

        // ---- A8：`ta=150 previousAbilityFiltered` ⇒ 上一条 ability 的目标；**不在棋盘的那半跳过并出声** ----
        {
            var ctx = MakeCtx();
            var src = Put(ctx, 0, 3, PlainUnit("B104MT_src"));
            ctx.PreviousAbilityTargets.Clear();
            ctx.PreviousAbilityTargets.Add(ctx.Players[0].Board[2]);
            ctx.PreviousAbilityTargets.Add(ctx.Players[1].Board[6]);
            ctx.PreviousAbilityTargets.Add(new UnitState(ctx.NewInstance(PlainUnit("B104MT_hand")), false));   // 不在棋盘
            var list = RuleCore.AbilityTargetListFromCriteriaForTest(
                ctx, 0, Set(150, 1, 1, 1, 1, 0), src, null);
            string got = Slots(list);
            Tr(list != null && list.Count == 2,
              "A8 **`ta=150 previousAbilityFiltered` ⇒ 上一条 ability 的目标里**仍在棋盘上**的那些（2 张）**"
            + "（实得 " + got + "）"
            + "｜判据 `:867-883`（`AddRange(上一条目标表)` 后照常过 36 格）"
            + "｜裁定 ②：原版这一档的目标**可以在手牌里**，`BattleEventTarget` 只能表达棋盘格 ⇒ 手牌那半**部分做**（出声，⛔ 不发明坐标）"
            + "｜🧨 把不在棋盘的那张也硬塞进去（`Slot` 填 -1）⇒ 变 3 ⇒ 红");
        }

        // ---- A9：**没接的档必须回 `null`**（调用方据此退回改前的单数路）----
        {
            var ctx = MakeCtx();
            var src = Put(ctx, 0, 3, PlainUnit("B104MT_src"));
            Tr(RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(65, 1, 0, 0, 1, 0), src, null) == null,
              "A9-a **`ta=65 inDeckFiltered` 本批没接 ⇒ `null`**（⛔ 不拿空表冒充「原版也没有目标」）");
            Tr(RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(0, 1, 1, 1, 1, 0), src, null) == null,
              "A9-b **`ta=0 any` 保持休眠 ⇒ `null`**"
            + "｜裁定 ④ 2026-10-31（池内 0 张、原版休眠；铁律 11 例外①）"
            + "｜🧨 顺手把它接上 ⇒ 这条红（那会凭「场上全部单位」造出原版休眠档的行为）");
            var unknownFlags = Set(10, 1, 0, 1, 1, 1); unknownFlags.FriendlyUnits = CardDef.CountSetSpec.Unknown;
            Tr(RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, unknownFlags, src, null) == null,
              "A9-c **`ta=10` 但 `friendlyUnits` 没落 ⇒ `null`（判不了，⛔ 不猜己方/敌方）**");
        }

        // ---- A10（灭自证）：`targetSelectMethod = 2/3 valueInPlay` 要截断 ⇒ `null`（本版没有那个等价字段）----
        {
            var ctx = MakeCtx();
            var src = Put(ctx, 0, 3, PlainUnit("B104MT_src"));
            Tr(RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(10, 1, 0, 1, 1, 1, maxTargets: 1, method: 2), src, null) == null,
              "A10-a **`targetSelectMethod = 2 highestValueInPlay` 要截断 ⇒ `null`**"
            + "｜判据 `:1215-1246` 要 `CardScript.GetValueInPlay()`，本版**没有**等价字段（`CountEval.ApplyMaxTargets` 同一结论）"
            + "｜🧨 拿随机 / 池序冒充 ⇒ 会返回 1 张 ⇒ 红");
            Tr(RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(10, 1, 0, 1, 1, 1, maxTargets: 1, method: 3), src, null) == null,
              "A10-b 同理 `3 lowestValueInPlay` ⇒ `null`（`:1247-1261`）");
            // 不截断时（`maxTargets <= 0`）与这两档**无关** ⇒ 照常出表（证明 `null` 只在「真要选人」时发生）
            var noTrunc = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, Set(10, 1, 0, 1, 1, 1, maxTargets: 0, method: 2), src, null);
            C(noTrunc == null ? -1 : noTrunc.Count, 4,
              "A10-c **不截断时 `targetSelectMethod` 不参与 ⇒ 照常出 4 张**（`maxTargets <= 0` 直接跳过 `:1164`）"
            + "｜🧨 把 `null` 判成「这一档永远不行」⇒ 变 0 ⇒ 红");
        }
    }

    // ============================================================ B · 端到端（主动能力 → 事件）

    /// <summary>合成 v25 卡表：`ca = [30 buffOthers, 462 triggerPray]`、`ta = [10, 30]`；
    /// `activeAbilityTargets` 两行 —— `lg=0` 是 `ta=10`（己方全体），`lg=1` 是 `ta=30`。
    /// <para>⚠️ 为什么 `ca` 用 30：`buffOthers` 会经 `CardDef.AttachByAbilityKind` 把正文 op 挂到这一行
    /// （`Rally: Give +2 [Ranged] to a friendly unit this turn`，与真卡 `SOR66` 同形）⇒
    /// `UseActiveAbilityCore` 的 `plan` 才非空、才走到**发事件 + 挂目标表**那一跳。</para></summary>
    static string CardJson()
    {
        return "{\"version\":25,\"count\":1,\"cards\":[{"
             + "\"id\":\"B104MT1\",\"name\":\"B104MT 合成探针\",\"type\":\"unit\","
             + "\"desc\":\"Rally: Give +2 [Ranged] to a friendly unit this turn\","
             + "\"rarity\":\"Common\",\"faction\":\"Ultramarines\",\"cost\":1,\"attack\":1,\"health\":3,\"ranged\":0,"
             + "\"subtype\":\"Infantry\",\"keywords\":[\"Rally\"],\"cardTraits\":[],"
             + "\"hasActiveAbility\":1,\"activeAbilityManaCost\":0,\"activeAbilityPsychicCost\":0,"
             + "\"activeAbilityPlasmaCost\":0,\"activeAbilityTargeted\":0,"
             + "\"activeAbilityLogic\":[{\"ab\":-1,\"gate\":1,\"trigger\":400,\"needsLoading\":0,"
             + "\"ta\":[10,30],\"fm\":[0,0],\"ca\":[30,462],\"pa\":[0,10],"
             + "\"dmg\":[{\"min\":0,\"max\":0,\"opt\":0,\"type\":0},{\"min\":0,\"max\":0,\"opt\":0,\"type\":0}]}],"
             + "\"activeAbilityTargets\":["
             + "{\"ab\":-1,\"lg\":0,\"criteria\":" + CriteriaJson(10, 1, 0, 1, 1, 1) + "},"
             + "{\"ab\":-1,\"lg\":1,\"criteria\":" + CriteriaJson(30, 0, 0, 0, 0, 0) + "}"
             + "]}]}";
    }

    static string CriteriaJson(int ta, int friendly, int enemy, int hero, int minions, int self)
    {
        return "{\"playerTurn\":-1,\"targetsAffected\":" + ta + ",\"chosenArmy\":0,\"armyFilter\":0,"
             + "\"keywordFilter\":0,\"traitsFilter\":0,\"maxTargets\":0,\"targetSelectMethod\":0,"
             + "\"attackFilter\":0,\"attackFilterOption\":0,\"currentHealthFilter\":0,"
             + "\"currentHealthFilterOption\":0,\"manaCostFilter\":0,\"manaCostFilterOption\":0,"
             + "\"rarityFilter\":0,\"rarityFilterOption\":0,\"spellType\":0,"
             + "\"friendlyUnits\":" + friendly + ",\"enemyUnits\":" + enemy + ","
             + "\"includeHero\":" + hero + ",\"includeMinions\":" + minions + ","
             + "\"includeSpells\":0,\"includeWhispers\":0,\"includeWeapons\":0,\"includeRemnants\":0,"
             + "\"includeHeroParts\":0,\"includeSelf\":" + self + ",\"excludeNotSelf\":0,"
             + "\"excludeSimilarCards\":0,\"excludeTargetted\":0,\"excludeFriendlyHero\":0,"
             + "\"excludeEnemyHero\":0,\"excludeRace\":0,\"excludeTrait\":0,"
             + "\"selectFromChooseResolution\":0,\"filterSpecificCards\":[],\"excludeEnchantments\":[]}";
    }

    static int CountAbilitySignals(BattleContext ctx)
    {
        int n = 0;
        var sig = ctx.Signals;
        for (int i = 0; i < sig.Count; i++) if (sig[i] != null && sig[i].Kind == EvtKind.Ability) n++;
        return n;
    }

    static BattleEvent LastAbilitySignal(BattleContext ctx)
    {
        var sig = ctx.Signals;
        for (int i = sig.Count - 1; i >= 0; i--) if (sig[i] != null && sig[i].Kind == EvtKind.Ability) return sig[i];
        return null;
    }

    static void SectionB_EndToEnd()
    {
        var parsed = CardDatabase.Parse(CardJson());
        // 🔴 `CardDatabase.Parse` 末尾那句 `CreatePool.BuildNameIndex(list)` 会把**全局**「卡名 → id」索引
        //    换成**只有这张合成卡**那一份（同 `RuleEngineTest_EngineE1.cs:207-209` 的 finally 那条纪律）
        //    ⇒ 立刻用真池复原，别污染后面的用例。
        CreatePool.BuildNameIndex(CardDatabase.Load());
        Tr(parsed != null && parsed.Count == 1, "B0 （前提）合成 v25 探针卡解析出来了");
        if (parsed == null || parsed.Count != 1) return;
        var card = parsed[0];

        Tr(card.ActiveAbilityMap == "attached",
          "B0-b （前提）`ApplyActiveAbilityLogic` 把 `ca=30` 那一行配到了正文 op（`ActiveAbilityMap = " + card.ActiveAbilityMap + "`）");
        Tr(card.ActiveAbilityTargetCriteriaAt(CardDef.ActiveAbilityObject, 0) != null
           && card.ActiveAbilityTargetCriteriaAt(CardDef.ActiveAbilityObject, 1) != null,
          "B0-c （前提）`activeAbilityTargets` 的 `(ab=-1, lg=0/1)` 两行都解析进来了");

        var ctx = NewCtx(new[] { card });
        ctx.Active = 0;
        var caster = Put(ctx, 0, 3, card);
        // 🔴 **夹具必须显式放行「本回合可行动」**：`UnitState` 构造函数末尾是
        //    `Exhausted = !RuleCore.HasDeployExemption(this)`（`UnitState.cs:916`，= 原版「部署当回合不可行动」），
        //    而 `Put` 手摆的是一张**刚落地**的牌 ⇒ 它默认是**疲劳**的 ⇒ `CanStartActiveAbility` 直接回
        //    **`ErrExhausted(6)`**、`EvtKind.Ability` 压根不发（上一轮 B1/B4 红的根因就是这个，**不是产品语义**）。
        //    真卡在 `activeAbility` 那一回合时早已在场上 ⇒ 这里置假才是等价前提。
        caster.Exhausted = false;
        C(RuleCore.CanStartActiveAbility(ctx, 0, 3), RuleCodes.OK,
          "B0-d （前提）施放者**本回合可行动**、`CanStartActiveAbility` 放行"
        + "｜🧨 忘了清 `Exhausted`（部署当回合 = 疲劳，`UnitState.cs:916`）⇒ 回 `ErrExhausted(6)`、"
        + "下面 B1 拿不到事件（这一条就是为了把该根因单独钉出来，别再靠 B1 的级联去猜）");
        Put(ctx, 0, 2, PlainUnit("B104MT_t2"));
        Put(ctx, 0, 6, PlainUnit("B104MT_t6"));
        Put(ctx, 0, 4, Hero("B104MT_h0b"), warlord: true);
        Put(ctx, 1, 4, Hero("B104MT_h1b"), warlord: true);
        Put(ctx, 1, 6, PlainUnit("B104MT_e6b"));

        int before = CountAbilitySignals(ctx);
        int rc = RuleCore.UseActiveAbility(ctx, 0, 3, -1);
        var e = LastAbilitySignal(ctx);
        Tr(e != null, "B1 （前提）主动能力跑出了 `EvtKind.Ability`（rc=" + rc + "；rc=0/13 都正常 —— `ErrUnimplemented(13)` 只表示后面的结算行没跑完，"
                     + "**事件在它之前就发了**；rc=**6 = `ErrExhausted`** ⇒ 夹具忘了清 `Exhausted`，看 B0-d）");
        if (e != null)
        {
            var list = e.AbilityTargetList;
            Tr(list != null, "B2 （前提）`AttachAbilityTargets` 填了 `AbilityTargetList`（不是 `null`）");
            C(list == null ? -1 : list.Count, 4,
              "B3 **端到端：主动能力事件的目标表 = 4 张（己方督军 + 2 部队 + 施放者自己）**"
            + "（实得 " + Slots(list) + "）"
            + "｜判据：`ta=10` 的建池 + 36 格筛选（`AbilityLogic__GetTargets.c:972-990` / `:1081-1163`）"
            + "｜🧨 **接线那一跳**（`UseActiveAbilityCore` → `AttachAbilityTargets(…, criteria)`）被退回 5 实参 ⇒ "
            + "`AbilityTargetsFor(null spec, chosen=null)` 给**空表** ⇒ 变 0 ⇒ 红");
            Tr(list != null && list.TrueForAll(t => t.Player == 0),
              "B3-b 表里全是己方：" + Slots(list));
        }
        C(CountAbilitySignals(ctx) - before, 1,
          "B4 **一条 ability 仍然只发一条 `EvtKind.Ability`**（目标表是挂在这条事件上的列表，⛔ 不是逐目标各发一条事件）"
        + "｜原版是每行一次 `GetTargets + PlayAnim`（`AbilityLogic__PlayAbility.c:262/273`），而我们的表现层按列表逐目标排拍"
        + "｜🧨 改成「逐目标发事件」⇒ 变 4 ⇒ 红（那会改 `Editor/RuleEngineTest.cs` 按字面量数的结构断言）");

        // ---- B5：`card_anim_order` 里没有这张合成卡 ⇒ 消费端 `ExpandAbilityBeats` 直接 `return false`（退一拍）----
        {
            var beats = new List<CardAnimBeat>();
            int n = CardAnimOrder.TryGetBeats("B104MT1", beats);
            Tr(n <= 0,
              "B5 **合成卡在 `card_anim_order` 里没有行 ⇒ 消费端 `ExpandAbilityBeats` 走 `if (n <= 0) return false`"
            + "（整条事件退成**一拍**、不展开）** —— 目标表再长也不影响这一档（n=" + n + "）");
        }

        // ---- B6：多行挑一行的规则（调度台裁定 ①）----
        {
            var p0 = new List<int[]>
            {
                new[] { 462, 30, 1, CardDef.ActiveAbilityObject },   // 先放「非多目标档」，验证它不是靠「第一行」
                new[] { 30,  10, 0, CardDef.ActiveAbilityObject },
            };
            var chosenSet = RuleCore.ActiveAbilityTargetCriteriaForPlanForTest(caster, p0);
            Tr(chosenSet != null && chosenSet.TargetsAffected == 10,
              "B6 **多行时优先挑 `ta ∈ {10,110,150}` 的那一行**（本例两行 `ta=30/10` ⇒ 挑 `ta=10`）"
            + "｜裁定 ①（2026-10-31）：按行独立、**不合并**；原版每行各一次 `GetTargets + PlayAnim`，"
            + "我们一条事件只有一张表 ⇒ 只挑一行整表"
            + "｜🧨 改成「取第一行」⇒ 这里会拿到 `ta=30` ⇒ 红；改成并集 ⇒ 判据上没有出处");
            var p1 = new List<int[]> { new[] { 30, 30, 1, CardDef.ActiveAbilityObject } };
            var onlySingle = RuleCore.ActiveAbilityTargetCriteriaForPlanForTest(caster, p1);
            Tr(onlySingle != null && onlySingle.TargetsAffected == 30,
              "B6-b 没有多目标档时退回**第一行有 criteria 的**（`ta=30`）");
            Tr(RuleCore.ActiveAbilityTargetCriteriaForPlanForTest(caster, new List<int[]>()) == null,
              "B6-c 空计划表 ⇒ `null`（⛔ 不猜）");
        }
    }
}
