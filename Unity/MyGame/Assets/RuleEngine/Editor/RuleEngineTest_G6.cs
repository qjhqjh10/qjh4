// RuleEngineTest_G6.cs — 执行代理 `G6`：`AM18` / `GOF4` 的共同根因（2026-10-24）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_UnitRoles` / `_Order` / `_BoardModel` / `_S23_*` … 都在），而**同一时刻只能有一个写手改一个文件**
// ⇒ 本批断言全写在这里，`RuleEngineTest.cs` **只加 `Run()` 里一行
// `Step(TestG6MultiCountRepeats)`**（由主对话落 —— 本文件**不碰**那个文件）。
//
// 判据（⛔ 不要重新论证）→ `资料/普查产出_1024/C6_最后三条判据.md` §1：
//   · **A**（数据）`gen_cards_engine.py`：`multi` / `mult` / `avail` / `choice` 四格
//     `conditionsFilter == 0` 时**照落行**（`cond = 0` / `name = null`），只落 `targetsAffected != 0` 的；
//   · **B**（计数）`EffectResolver.CountFor` 的盘面分支**按兵种筛**（改前只筛 `troop`）；
//   · **C**（引擎）触发层补 `repeats`：`multiCountCriteria`（整条 ability 触发 N 遍）
//     ＋ 出牌路那套 `TryComputeRepeats`（正文 `for each` / `mult` 结构化乘数 / `RepeatTimes` / `fM`）；
//   · **D**（收口）`CardDatabase` 那条「本版没实现」的告警改对。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 **期望值一律写死字面量**（`0` / `3` / `9` / `2` …）—— ⛔ 不从实现常量反算（那会自证）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestG6MultiCountRepeats)`）。
    /// 四个子用例是 `G6…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestG6MultiCountRepeats()
    {
        Section("🆕 G6 / `C6` §1：`multiCountCriteria` 触发 N 遍 ＋ 兵种筛（`AM18` / `GOF4`）");
        G6Am18TurnEndByVehicleCount();
        G6Gof4MultiplierExcludesSelf();
        G6TriggerRepeatsSynthetic();
        G6CountForKindFilter();
    }

    // ==============================================================================================
    //  ① `AM18`：回合末按「友方载具数」触发 N 遍（0 / 1 / 3 三档）
    // ==============================================================================================

    /// <summary>
    /// **判据**（`C6` §1·2 ③ / §1·3 C）：`AM18 Master of Ordnance` 的
    /// `multiCountCriteria = {ta 10 / friendly / minions / keywordFilter 10(Vehicle)}`，
    /// 卡面 `At the end of your turn, deal 3 damage to a random enemy **for each friendly Vehicle in play**`
    /// ⇒ 回合末打 **N × 3**，`N = 友方载具数`（**0 台 ⇒ 一次都不打**）。
    ///
    /// <para>三档都断（0 / 1 / 3）：只断一档时「恒 1 遍」和「恒 N 遍」会混过去。
    /// 夹具里 `AM18` 自己（`Infantry`）与督军（`hero`）**都在场** ⇒ 同时钉住**兵种筛**：
    /// 改前盘面分支只筛 `troop`，会把「AM18 + 督军 + 载具」一起数进去。</para>
    ///
    /// <para>🧨 触发层没有 `repeats` ⇒ 三档恒 `3`；🧨 盘面分支不筛兵种 ⇒ 三档恒 `(N + 2) × 3`。</para>
    /// </summary>
    static void G6Am18TurnEndByVehicleCount()
    {
        var pool = CardDatabase.Load();
        var am18 = CardDatabase.FindById(pool, "AM18");
        CheckTrue(am18 != null, "(前提) `AM18 Master of Ordnance` 在池内");
        if (am18 == null) return;
        CheckTrue(am18.Subtype == "Infantry",
                  "★ (前提) `AM18` 自己是 `Infantry`（不是载具 ⇒ 不会被数进「友方载具」）"
                + "｜🧨 卡表 subtype 变了 ⇒ 本用例的「自己不算」那一半失效");

        foreach (int vehicles in new[] { 0, 1, 3 })
        {
            var ctx = ProbeBattle(new CardDef[0], new[] { Unit("G6_Foe", 1, 0, 9) });
            Place(ctx, 0, 5, am18);                                   // AM18 自己（Infantry）
            for (int i = 0; i < vehicles; i++)
                Place(ctx, 0, i, G6Unit("G6_Veh" + i, "Vehicle"));    // 友方载具
            var foe = ctx.Players[1].Warlord;
            int before = foe.Health;
            RuleCore.EndTurn(ctx);
            int dealt = before - foe.Health;
            Check(dealt, vehicles * 3,
                  $"★ `AM18`：友方载具 **{vehicles}** 台 ⇒ 回合末打 **{vehicles * 3}** 点（3 × N）"
                + "（`RawCardScript__TriggerAbilities.c:226-259`；`C6` §1·2 ③）"
                + "｜🧨 触发层没有 `repeats` ⇒ 实得恒 3"
                + "｜🧨 盘面计数不筛兵种（只筛 troop）⇒ 把 AM18 自己 + 督军数进去、实得 "
                + ((vehicles + 2) * 3) + " 附近");
        }
    }

    // ==============================================================================================
    //  ② `GOF4`：`multiplier` 的 `includeSelf = 0` ⇒ 只有自己时乘 **0**
    // ==============================================================================================

    /// <summary>
    /// **判据**（`C6` §1·4）：`GOF4 Slugga Boy` 的
    /// `abilityLogic[0].multiplier = {ta 10 / friendly / minions / includeSelf 0 /
    /// filterSpecificCards = [GOF4 自己]}`，卡面 `Rally: Gain +1 [Attack] for each **other**
    /// friendly Slugga Boy` ⇒ 场上只有它自己时 **N = 0** ⇒ **+0**（`FilterMethods__CheckIfMeetsCriteria.c:233`
    /// / `:241`：`includeSelf == 0` ⇒ 候选 == thisCard 就淘汰）。
    ///
    /// <para>两档：① 单独 ⇒ 攻击力 = 卡面基础（2）；② 场上另有 1 张 Slugga Boy ⇒ 基础 +1。</para>
    ///
    /// <para>🧨 `gen_cards_engine.py` 的 early-return 没改（`mult` 行不落）⇒ 退回正文计数
    /// （`own|any|all` 含自己）⇒ ① 实得 基础 +1；🧨 `filterSpecificCards` 那一档没补 ⇒ 同样 +1；
    /// 🧨 `includeSelf = 0` 没生效（`actor` 没传触发的那张单位）⇒ 同样 +1。</para>
    /// </summary>
    static void G6Gof4MultiplierExcludesSelf()
    {
        var pool = CardDatabase.Load();
        var gof4 = CardDatabase.FindById(pool, "GOF4");
        CheckTrue(gof4 != null, "(前提) `GOF4 Slugga Boy` 在池内");
        if (gof4 == null) return;
        CheckTrue(gof4.Attack == 2, "★ (前提) 卡面基础近战 = **2**"
                  + "｜🧨 卡表数值变了 ⇒ 下面两条期望值要一起改（不是引擎错）");

        // ---- ① 场上只有它自己 ⇒ Rally 乘 0 ⇒ 攻击力不涨 ----
        {
            var ctx = ProbeBattle(new[] { gof4 }, new[] { Unit("G6_Foe", 1, 0, 9) });
            ctx.Players[0].Energy = 20;
            int slot = G6FreeSlot(ctx, 0);
            // 🔴 **按【身份】取刚上场那张，⛔ 不按请求格取** —— `PlayCard` 的 `slot` 是**请求格**，
            //    落点由 `BoardSlots.Insert` 算（空盘请求 0/1/2 一律落 3；见 `RuleCore.PlayCard` 的 doc
            //    与 `A1597`）。按请求格取 ⇒ 拿到 `null` ⇒ 前提假红（2026-10-24 `E9` 修的就是这一条）。
            var before = G6Snapshot(ctx, 0);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Slugga Boy"), slot), RuleCodes.OK,
                      "(前提) 打出 GOF4");
            var u = G6JustPlayed(ctx, 0, "GOF4", before);
            CheckTrue(u != null, "★ (前提) GOF4 上场了（按 `Card.Id` 扫盘找【打之前不在盘上】的那张）");
            if (u != null)
                Check(u.Attack, 2,
                      "★ **只有它自己** ⇒ `multiplier` 的 `includeSelf = 0` ⇒ 乘 **0** ⇒ 攻击力 = 卡面 2"
                    + "（⛔ 不是 +1）｜🧨 `mult` 行没落 / `filterSpecificCards` 没补 / `actor` 没传 ⇒ 实得 3");
        }

        // ---- ② 场上另有一张 Slugga Boy ⇒ 数到 1 ⇒ 基础 +1 ----
        {
            var ctx = ProbeBattle(new[] { gof4 }, new[] { Unit("G6_Foe", 1, 0, 9) });
            Place(ctx, 0, 3, gof4);                    // 「另一个」—— 直接摆场（不走 PlayCard ⇒ 不触发它的 Rally）
            ctx.Players[0].Energy = 20;
            int slot = G6FreeSlot(ctx, 0);
            // 同 ①：按身份取。这一档盘上会有**两张** `GOF4`（`Place` 摆的那张 ＋ 刚打出的那张）
            // ⇒ 靠「打之前不在盘上」的差集区分；再叠一道 `PlayOrder` 最大做兜底
            // （`Place` 先取号、`PlayCard` 后取号 ⇒ 刚打出的那张号更大）。
            var before = G6Snapshot(ctx, 0);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Slugga Boy"), slot), RuleCodes.OK,
                      "(前提) 打出第二张 GOF4");
            var u = G6JustPlayed(ctx, 0, "GOF4", before);
            CheckTrue(u != null, "★ (前提) 第二张 GOF4 上场了（差集 ＋ `PlayOrder` 最大那两张 GOF4 里挑）");
            if (u != null)
                Check(u.Attack, 3,
                      "★ 场上另有 **1** 张 Slugga Boy ⇒ 乘 1 ⇒ 攻击力 = 2 + 1 = **3**"
                    + "｜🧨 计数退了（`filterSpecificCards` / 兵种筛坏掉）⇒ 实得 2 或 4+");
        }
    }

    // ==============================================================================================
    //  ③ 触发层 `repeats`（`multiCountCriteria`）：合成夹具 3 个目标 ⇒ 3 遍（＋灭自证）
    // ==============================================================================================

    /// <summary>
    /// **判据**（`C6` §1·2 ① / §1·3 C）：`multiCountCriteria.targetsAffected != 0` ⇒
    /// **整条 ability 触发 N 遍**（`for (i &lt; 表长) TriggerAbility(...)`）。
    ///
    /// <para>**为什么用合成夹具**：现池 8 条 `multi` 行里，能真正走结构化计数的只有 `ta = 10`
    /// 的两条（`AM18` 带 `keywordFilter = 10` ⇒ `CountEval` 判不了、退回正文；`ASH52` 是 `ta = 161`
    /// ⇒ 也判不了）⇒ 不合成一张「结构上可判」的卡，「整条 ability 触发 N 遍」这一层**没有任何
    /// 现池卡能验到**。</para>
    ///
    /// <para>夹具：一张 `Deal 1 damage to an enemy` 的卡 ＋ 一条 `multi` 行（`ta 10 / friendly /
    /// includeMinions / includeSelf 0`）＋ 场上 3 个**别的**友方部队 ⇒ 打 3 点（3 遍 × 1 点）。</para>
    ///
    /// <para>**灭自证**（同一条断言的反面）：把那张卡的 `multi` 行**去掉** ⇒ 正文没有 `for each`
    /// ⇒ 只打 **1** 点。🧨 去掉触发层的 `repeats` ⇒ 两档都会变成 1 遍（正档红）；
    /// 🧨 把 `ta` 改成 `0 (any)` ⇒ 正档也变成 1 遍（`any` 不携带信息，见 `TryStructuredMultiTrigger` 的 doc）。</para>
    /// </summary>
    static void G6TriggerRepeatsSynthetic()
    {
        // ---- 正档：带 `multi` 行 ⇒ 3 遍 ----
        {
            var card = G6MultiProbeCard(withMultiRow: true, ta: 10);
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("G6_Foe", 1, 0, 20) });
            var me = Place(ctx, 0, 5, card);
            for (int i = 0; i < 3; i++) Place(ctx, 0, i, G6Unit("G6_Troop" + i, "Infantry"));
            var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
            CheckTrue(ops != null && ops.Count == 1, "★ (前提) 合成卡解出 1 条 op");
            if (ops == null || ops.Count != 1) return;
            // 合成夹具：`multi` 行挂在 **ability 级**（`lg = -1`）⇒ 显式把 op 归到 `(0, -1)`
            // （`AssignAbilityIndices` 归的是 `(0, 0)`，与本行的位置语义不同 —— 这是**数据形状**，不是猜）。
            ops[0].AbilityIndex = 0;
            ops[0].LogicIndex = -1;
            int before = ctx.Players[1].Warlord.Health;
            RuleCore.ResolveOps(ctx, 0, me, ops, "G6 合成");
            Check(before - ctx.Players[1].Warlord.Health, 3,
                  "★ 合成卡（`multiCountCriteria.ta = 10` / 3 个友方部队）⇒ **整条 ability 触发 3 遍** ⇒ 3 点"
                + "（`RawCardScript__TriggerAbilities.c:226-259`）"
                + "｜🧨 触发层没有 `repeats` ⇒ 实得 1"
                + "｜🧨 `includeSelf = 0` 没生效 ⇒ 实得 4（把它自己也算进去）");
        }

        // ---- 灭自证：同一张卡**去掉** `multi` 行 ⇒ 1 遍 ----
        {
            var card = G6MultiProbeCard(withMultiRow: false, ta: 10);
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("G6_Foe", 1, 0, 20) });
            var me = Place(ctx, 0, 5, card);
            for (int i = 0; i < 3; i++) Place(ctx, 0, i, G6Unit("G6_Troop" + i, "Infantry"));
            var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
            if (ops == null || ops.Count != 1) { CheckTrue(false, "★ (前提·灭自证) 合成卡解出 1 条 op"); return; }
            ops[0].AbilityIndex = 0; ops[0].LogicIndex = -1;
            int before = ctx.Players[1].Warlord.Health;
            RuleCore.ResolveOps(ctx, 0, me, ops, "G6 合成·灭自证");
            Check(before - ctx.Players[1].Warlord.Health, 1,
                  "★ **灭自证**：同一张卡去掉 `multi` 行 ⇒ 只打 **1** 点"
                + "（证明正档那 3 点是**这一行数据**带来的，不是夹具自己的差异）"
                + "｜🧨 把 `multi` 行的存在性判错（有没有都当 3 遍）⇒ 本条红");
        }

        // ---- 第二档灭自证：`ta = 0 (any)` ⇒ 按「不携带信息」处理，仍是 1 遍 ----
        {
            var card = G6MultiProbeCard(withMultiRow: true, ta: 0);
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("G6_Foe", 1, 0, 20) });
            var me = Place(ctx, 0, 5, card);
            for (int i = 0; i < 3; i++) Place(ctx, 0, i, G6Unit("G6_Troop" + i, "Infantry"));
            var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
            if (ops == null || ops.Count != 1) { CheckTrue(false, "★ (前提·any) 合成卡解出 1 条 op"); return; }
            ops[0].AbilityIndex = 0; ops[0].LogicIndex = -1;
            int before = ctx.Players[1].Warlord.Health;
            RuleCore.ResolveOps(ctx, 0, me, ops, "G6 合成·any");
            Check(before - ctx.Players[1].Warlord.Health, 1,
                  "★ `targetsAffected = 0 (any)` ⇒ 原版让**其余 35 格全部失效**（`Any disregards all other filters`）"
                + " ⇒ 这一行**不携带「数谁」** ⇒ 按正文（没有 `for each`）打 **1** 点"
                + "（理由 → `TryStructuredMultiTrigger` 的 doc ①：`GOF90` 的真乘数在附魔上，"
                + "按数据返 1 会把正文 `for each` 压成 1 遍）"
                + "｜🧨 把 `any` 当成「N = 1 的可判数据」去覆盖正文 ⇒ 本档仍绿（**所以它只防回退、不当主判据**）");
        }
    }

    // ==============================================================================================
    //  ④ 兵种筛：`CountFor` 的盘面分支对 `vehicle` / `infantry` 各自筛对
    // ==============================================================================================

    /// <summary>
    /// **判据**（`C6` §1·3 B / §四·2）：`for each friendly Vehicle in play` 数的是**载具**
    /// （`AM18` 的卡面原文），改前盘面分支**只筛 `troop`** ⇒ 载具/步兵/兽/机蜂全都被数进去。
    ///
    /// <para>夹具：我方 2 台载具（`FIX_V1` / `FIX_V2`）· 1 个步兵 · 1 只兽 · 督军（`hero`）都在场
    /// ⇒ 我方棋盘 **5** 个活单位（4 个单位 ＋ 1 个督军）；下面五条读数与这个数**逐项对得上**：
    /// `own|vehicle|all` = **2** · `own|infantry|all` = **1** · `own|beast|all` = **1** ·
    /// `own|troop|all` = **4**（4 个单位，**不含督军**）· `own|any|all` = **5**（4 个单位 ＋ 督军）。</para>
    ///
    /// <para>🧨 退回「只筛 troop」（不筛兵种）⇒ `vehicle` 读成 **5**（步兵/兽/督军全被数进来，红）；
    /// 🧨 把 `unit`/`any` 也排除督军 ⇒ `any` 读成 **4**（红）；
    /// 🧨 把 `troop` 与 `unit` 混成一个映射（督军不再排除）⇒ `troop` 读成 **5**（红）。</para>
    /// </summary>
    static void G6CountForKindFilter()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        Place(ctx, 0, 0, G6Unit("FIX_V1", "Vehicle"));
        Place(ctx, 0, 1, G6Unit("FIX_V2", "Vehicle"));
        Place(ctx, 0, 2, G6Unit("FIX_I1", "Infantry"));
        Place(ctx, 0, 3, G6Unit("FIX_B1", "Beast"));
        var w = ctx.Players[0].Warlord;
        CheckTrue(w != null, "★ (前提) 我方督军在棋盘上");
        if (w == null) return;

        Check(G6Count(ctx, w, "own|vehicle|all"), 2,
              "★ `own|vehicle|all` = **2**（只有那两台载具）"
            + "｜🧨 盘面分支只筛 `troop`（不筛兵种）⇒ 实得 5（1 步兵 + 1 兽 + 督军全被数进来）");
        Check(G6Count(ctx, w, "own|infantry|all"), 1,
              "★ `own|infantry|all` = **1**（只有 `FIX_I1`）"
            + "｜🧨 只筛 `troop` ⇒ 实得 5（2 载具 + 1 兽 + 督军全被数进来）");
        Check(G6Count(ctx, w, "own|beast|all"), 1,
              "★ `own|beast|all` = **1**（只有 `FIX_B1`）"
            + "｜🧨 只筛 `troop` ⇒ 实得 5（2 载具 + 1 步兵 + 督军全被数进来）");
        Check(G6Count(ctx, w, "own|troop|all"), 4,
              "★ `own|troop|all` = **4**（盘上 4 个单位全是部队，**不含督军**）"
            + "｜🧨 把 `troop` 与 `unit` 并成一个映射（督军不再排除）⇒ 实得 5（4 单位 + 督军）");
        Check(G6Count(ctx, w, "own|any|all"), 5,
              "★ `own|any|all` = **5**（4 个单位 ＋ 督军 —— **含督军**：卡面写 `units` 时督军要算）"
            + "｜🧨 顺手把 `any` 也排除督军 ⇒ 实得 4");
    }

    // ==============================================================================================
    //  夹具
    // ==============================================================================================

    /// <summary>`subtype` 明确的合成单位（`Unit()` 只给空 subtype ⇒ 兵种筛类断言会失真）。</summary>
    static CardDef G6Unit(string name, string subtype)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 1, 1, 5, 0, null, subtype: subtype);
    }

    /// <summary>棋盘上第一个空格（夹具摆场用；也可当 `PlayCard` 的**请求格**）。
    /// ⚠️ 它**不是落点** —— `PlayCard` 的落点由 `BoardSlots.Insert` 算（连续无洞模型：插入推人），
    /// 要拿落点请用下面的 <see cref="G6JustPlayed"/> 按身份找，⛔ 别拿请求格去 `Board(ctx, p, slot)`。</summary>
    static int G6FreeSlot(BattleContext ctx, int p)
    {
        var b = ctx.Players[p].Board;
        for (int s = 0; s < b.Length; s++) if (b[s] == null) return s;
        return -1;
    }

    /// <summary>棋盘快照（**按引用**拷一份）—— 给 <see cref="G6JustPlayed"/> 求「打之前就在盘上」的差集。</summary>
    static UnitState[] G6Snapshot(BattleContext ctx, int p)
    {
        var b = ctx.Players[p].Board;
        var copy = new UnitState[b.Length];
        System.Array.Copy(b, copy, b.Length);
        return copy;
    }

    /// <summary>
    /// **取「刚打出 / 刚上场」的那张单位**（按身份，⛔ 不按请求格）。
    ///
    /// <para>为什么不能按请求格取：`RuleCore.PlayCard(..., slot)` 的 `slot` 是**请求格**，
    /// 落点由 `BoardSlots.Insert` 算 —— **连续无洞部署模型**下放牌是「插入推人」，
    /// 空盘请求 `0/1/2` 一律落 **3**（`AdjustedSlot` 的夹取；见 `RuleCore.PlayCard` 的 doc 与 `A1597`）。
    /// 拿请求格去 `Board(ctx, p, slot)` ⇒ 在**空位**上取到 `null` ⇒ 前提假红。</para>
    ///
    /// <para>判据三条：① 打之前**不在** <paramref name="before"/> 快照里（引用差集）；
    /// ② `Card.Id` == <paramref name="cardId"/>（⛔ 不用 `Name` —— 卡名会重）；③ 多张命中取
    /// `PlayOrder` **最大**那张（进场号单调递增 ⇒ 后上场的更大）。找不到返回 `null`。</para>
    /// </summary>
    static UnitState G6JustPlayed(BattleContext ctx, int p, string cardId, UnitState[] before)
    {
        UnitState found = null;
        var b = ctx.Players[p].Board;
        for (int s = 0; s < b.Length; s++)
        {
            var u = b[s];
            if (u == null || u.Card == null || u.Card.Id != cardId) continue;
            if (System.Array.IndexOf(before, u) >= 0) continue;      // 打之前就在场（另一张同 id 的卡）
            if (found == null || u.PlayOrder > found.PlayOrder) found = u;
        }
        return found;
    }

    /// <summary>`CountFor` 的公开自检门（`EffectResolver.CountForTest`）。</summary>
    static int G6Count(BattleContext ctx, UnitState src, string reference)
    {
        var op = new EffectOp { Verb = "deal", Amount = 1, CountRef = reference, CountScope = "board" };
        return RuleCore.CountForTest(ctx, 0, src, op);
    }

    /// <summary>
    /// 合成卡：`Deal 1 damage to an enemy`（正文**没有** `for each`）＋（可选）一条 `multi` 行。
    /// `multi` 行的形状照 `C6` §1·2 ③ 的 `AM18` 那一份（`ta 10 / friendly / includeMinions /
    /// includeSelf 0`），只把 `targetsAffected` 做成可判的那一档。
    /// ⚠️ `CountSetSpec` 的「本版未实现」那几格**必须显式写 0**（缺省 `-1` 会被 `CountEval.SetGap`
    /// 判成「没落」⇒ 整条判不了）。
    /// </summary>
    static CardDef G6MultiProbeCard(bool withMultiRow, int ta)
    {
        var conds = new List<CardDef.ConditionSpec>();
        if (withMultiRow)
        {
            conds.Add(new CardDef.ConditionSpec
            {
                Ability = 0, Logic = -1, Role = "multi", Condition = 0, Name = null, Exclude = false,
                MultiSet = new CardDef.CountSetSpec
                {
                    PlayerTurn = 0,
                    TargetsAffected = ta,
                    FriendlyUnits = 1, EnemyUnits = 0,
                    IncludeHero = 0, IncludeMinions = 1, IncludeRemnants = 0,
                    IncludeSelf = 0, ExcludeNotSelf = 0,
                    // ↓ `CountEval.SetGap` 会检查的那几格：本版未实现的维度一律显式 0（= 不适用）
                    ArmyFilter = 0, KeywordFilter = 0, TraitsFilter = 0, SpellType = 0,
                    AttackFilterOption = 0, CurrentHealthFilterOption = 0,
                    ManaCostFilterOption = 0, RarityFilterOption = 0,
                    ExcludeSimilarCards = 0, ExcludeTargetted = 0,
                    ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0,
                    ExcludeRace = 0, ExcludeTrait = 0, SelectFromChooseResolution = 0,
                    FilterSpecificCardIds = new string[0], ExcludeEnchantmentIds = new string[0],
                },
            });
        }
        return new CardDef("G6_MultiProbe", "G6 Multi Probe", "unit", "Deal 1 damage to an enemy",
                           null, "Test", 1, 1, 5, 0, null,
                           abilityConditions: conds, conditionsVersioned: true);
    }
}
