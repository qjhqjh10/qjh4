// RuleEngineTest_G5.cs — 引擎线 `G5` 的断言（2026-10-24）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_EngineB7` / `_EngineG2` / `_EngineG3` / `_Diag` … 都在），而**同一时刻只有一个写手改一个文件**
// ⇒ 本批测试体全写在这里，`RuleEngineTest.cs` **只加 `Section(...)` + 一行 `Step(...)`**。
//
// 两件（判据来源 → `资料/普查产出_1024/{C4_ta240是否含督军,C5_剩余判据普查}.md`，
// 第一权威 = `d:/2/tools/decomp_full/`，本轮现读）：
//   件1 裸 `Deal <n> damage`（无目标短语）= 原版 **`targetsAffected = 30 (target)`**
//       —— **由施放方选一个目标**，**不是**「自动挑生命最低」。
//       改动落在 `EffectText.TryDeal` 的 `tok.Length == 0` 支（`Auto = true / PickMost = "-health"`
//       ⇒ `Side = enemy · Kind = any · Count = 1` 的**普通可点目标**）。
//   件2 `ta = 200 / 201` 的 `referencedCards`（原版 `CardScript.referencedCards // +0x278`）：
//       写点 = 能力 `380 setReferenceCard`（`AbilityLogic__PlayAbility.c:2128-2160`）；
//       读点 = `AbilityLogic__GetTargets.c:770-781`（200 整表）/ `:784-806`（201 只留 `IsInPlay`）。
//       落在 `UnitState.ReferencedCards` + `RuleCore.SetReferenceCards` + `ResolveTargets` 的最前面。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量，⛔ 不从实现常量反算（那会自证）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestG5BareDealAndReferencedCards)`）。
    /// 五个子用例是 `G5…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestG5BareDealAndReferencedCards()
    {
        G5BareDealSpec();
        G5BareDealWhoGetsHit();
        G5LowestHealthStillHitsLowest();
        G5BareDealRealCards();
        G5ReferencedCards();
    }

    // ==============================================================================================
    //  件1 · 解析层：裸 `Deal N damage` 现在是一条「要玩家选一个」的目标规格（`ta = 30`）
    // ==============================================================================================

    /// <summary>
    /// 判据（现读 `AbilityLogic__GetTargets.c:192-207`）：**只有** `ta = 30 (target)` /
    /// `0xdc (220)` 两支会读调用方给的 `targetCard` 并原样放进结果表，其余每一档都忽略它。
    /// 数据面（`资料/普查产出_1024/C4_ta240是否含督军.md` §六·2）：当前 build 全池 **33 张**
    /// 裸 `Deal <n> damage` 卡的逻辑行 **33/33 = `ta = 30`**；`ta = 240` 全池只有 2 张、
    /// **都不是裸 Deal**。
    /// </summary>
    static void G5BareDealSpec()
    {
        Section("G5·件1：裸 `Deal N damage` = 原版 `ta=30 target`（由施放方选一个，不是「生命最低」）");

        var ops = EffectText.Parse("Deal 2 damage", out var un, out var pa);
        CheckTrue(ops != null && ops.Count == 1,
                  "（前提）`Deal 2 damage` 解析出**一条** op（unparsed/partial 都空：" + un.Count + "/" + pa.Count + "）");
        if (ops == null || ops.Count == 0) return;
        var t = ops[0].Target;
        CheckTrue(t != null, "（前提）这条 op 有目标规格");
        if (t == null) return;

        Check(t.Auto, false,
              "★★ 裸 Deal 的规格**不再**是 `Auto`（= 不再「不问玩家」）"
            + "｜🧨 改回 `Auto = true` ⇒ 实得 True ⇒ 红（那一档会让 `PickTarget` 跳过它、玩家无从选）");
        Check(t.PickMost ?? "", "",
              "★★ 裸 Deal **不带挑法**（原版 `ta=30`，不是 `ta=240 lowestHealth`）"
            + "｜🧨 改回 `PickMost = \"-health\"` ⇒ 实得 -health ⇒ 红（这正是 `B7` 当时那条错口径）");
        Check(t.Side, "enemy",
              "裸 Deal 打敌方那一侧（与 33 张卡的数据一致：`enemyUnits = 1`）"
            + "｜🧨 判成 own/any ⇒ 实得别的值 ⇒ 红");
        Check(t.Kind, "any",
              "裸 Deal 不限定兵种（卡面没写 troop ⇒ 督军也在候选里；判据 `ParseTarget` 的 enemy 支）"
            + "｜🧨 收窄成 troop ⇒ 实得 troop ⇒ 红（督军会被静默排除）");
        Check(t.Count, 1,
              "只选**一个**目标（`PickTarget` 用 `Count == 1` 判「这张卡要不要玩家点目标」）"
            + "｜🧨 改成 0 = 全部 ⇒ 实得 0 ⇒ 红");
        CheckTrue(EffectText.PickTarget(ops) != null,
                  "★★ 裸 Deal 现在**要求玩家选一个目标**（`EffectText.PickTarget` 非空 ⇒ `CanPlayTactic`"
                + "会要一个合法 `targetSlot`；单位卡的 Duty/Pray/Agenda/Oath 走 `AlternativeTargetSide`）"
                + "｜🧨 设回 `Auto = true` ⇒ `PickTarget` 返回 null ⇒ 红");
        Check(EffectText.PickSide(ops), "enemy",
              "表现层据此高亮**敌方**半场（`PlayTactic` 也用同一句决定点的格子属于谁）"
            + "｜🧨 与 `Side` 分叉 ⇒ 实得空串/own ⇒ 红");
    }

    // ==============================================================================================
    //  件1 · 结算层：玩家点了谁就打谁；没人点（触发式正文）也**不再**固定打生命最低的那个
    // ==============================================================================================

    static void G5BareDealWhoGetsHit()
    {
        // ---- ① 玩家点了「槽号更大、血更多」的那个 ⇒ 就打它 ----
        //    🔴 **夹具必须让三种口径指向不同的人**（否则是自证）：
        //      · 「池序第一」 = 槽 3 的 `G5PickFirst`；
        //      · 「生命最低」 = 槽 3 的 `G5PickFirst`（1 血）；
        //      · 「玩家点的」 = 槽 5 的 `G5PickChosen`（6 血）。
        //    ⇒ 只有真的读 `chosen` 才会打到槽 5 那个。
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            var first = Place(ctx, foe, 3, Unit("G5PickFirst", 1, 0, 1));    // 池序第一 **且** 血最少
            var chosen = Place(ctx, foe, 5, Unit("G5PickChosen", 1, 0, 6));  // 玩家点它（血多、池序在后）
            CheckTrue(first.Health == 1 && chosen.Health == 6,
                      "① （前提）两个合法目标：槽 3 = 1 血（池序第一 ∩ 生命最低）、槽 5 = 6 血");

            var ops = EffectText.Parse("Deal 2 damage", out _, out _);
            var un = new List<string>();
            RuleCore.ResolveOps(ctx, me, null, ops, chosen, out un);        // 玩家点了槽 5
            Check(chosen.Health, 4,
                  "★★ ① 玩家点的那个目标挨 2 点（6 → 4）"
                + "｜🧨 结算层忽略 `chosen`（只看池序 / 只挑生命最低）⇒ 挨打的是槽 3 那个 ⇒ 实得 6 ⇒ 红");
            Check(first.Health, 1,
                  "★★ ① 池序第一 / 生命最低的那个**一点没挨**（1 血原样）"
                + "｜🧨 回到 `PickMost = -health`（改前）或忽略 `chosen` ⇒ 它会掉到 -1 ⇒ 红");
        }

        // ---- ② 没人选（`chosen == null`，= 触发式正文那一路）⇒ 取目标池前 N 个，**不是**生命最低 ----
        //    原版 `ta=30` 在 `targetCard == null` 时返回空表；我们这一侧没有「为触发式能力临时问玩家」
        //    的通道 ⇒ 落到 `ResolveTargets` 末段那条**对所有「明写了目标却没人点」的正文共用的**既有近似
        //    （= 池序前 N = 格号序）。本断言钉的是「**至少不再是生命最低**」，⛔ 别把它读成「等价于原版」。
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            var first = Place(ctx, foe, 3, Unit("G5FirstInPool", 1, 0, 6));  // 池序第一（格号 3）、血多
            var lowest = Place(ctx, foe, 5, Unit("G5LowestHp", 1, 0, 1));    // 血最少、但在池序后面
            CheckTrue(first.PlayOrder < lowest.PlayOrder,
                      "② （前提）`G5FirstInPool` 先进场（池序第一）");

            var ops = EffectText.Parse("Deal 2 damage", out _, out _);
            var un = new List<string>();
            RuleCore.ResolveOps(ctx, me, null, ops, null, out un);
            Check(first.Health, 4,
                  "★★ ② 没人选时打的是**目标池里第一个**（格号 3 / 先进场），6 → 4"
                + "｜🧨 回到 `PickMost = -health`（= 改前的行为）⇒ 挨打的是 1 血的 `G5LowestHp` ⇒ 实得 6 ⇒ 红"
                + "（这就是与 ③ 互为灭自证的那条腿：**裸 Deal 不该看血量**）");
            Check(lowest.Health, 1,
                  "② ……那个 1 血的反面**没挨打**"
                + "｜🧨 同上 ⇒ 它会掉到 -1 ⇒ 红");
        }
    }

    // ==============================================================================================
    //  件1 · 灭自证的另一半：卡面**明写** `with lowest Health` 的仍然打生命最低
    // ==============================================================================================

    /// <summary>
    /// 判据：`ta = 240 lowestHealth`（`AbilityLogic__GetTargets.c:835-844` →
    /// `BattleManager__GetLowestHealthUnit.c`），**与裸 Deal 的 `ta=30` 是两条路**。
    /// 全池真卡 = `SAU73 Annihilation Barge`（卡面 `Artifice: Deal 3 damage to the enemy with lowest Health.`）。
    /// </summary>
    static void G5LowestHealthStillHitsLowest()
    {
        var ops = EffectText.Parse("Deal 1 damage to the enemy with lowest Health.", out _, out _);
        CheckTrue(ops != null && ops.Count > 0 && ops[0].Target != null,
                  "③ （前提）卡面明写 `lowest Health` 的那句解析出目标规格");
        if (ops == null || ops.Count == 0 || ops[0].Target == null) return;
        Check(ops[0].Target.PickMost ?? "", "-health",
              "★★ ③ 明写 `lowest Health` ⇒ **仍然**带 `PickMost = -health`（`ta=240` 那一族没被这次改动碰到）"
            + "｜🧨 顺手把 `ParseTarget` 的 lowest/highest 支也删了 ⇒ 实得空串 ⇒ 红（与 ①② 互为灭自证）");

        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active, foe = 1 - ctx.Active;
        var big = Place(ctx, foe, 3, Unit("G5LhBig", 1, 0, 9));        // 池序在前、血多
        var small = Place(ctx, foe, 5, Unit("G5LhSmall", 1, 0, 3));    // 池序在后、血少
        var un = new List<string>();
        RuleCore.ResolveOps(ctx, me, null, ops, null, out un);         // 没人点（自动挑）
        Check(small.Health, 2,
              "★★ ③ 结算时打的是**生命最低**的那个（3 → 2）"
            + "｜🧨 删掉 `ResolveTargets` 的 `PickMost` 支 ⇒ 退回池序 ⇒ 挨打的是 `G5LhBig`（9）⇒ 红");
        Check(big.Health, 9,
              "③ ……血多的那个没挨打｜🧨 同上 ⇒ 实得 8 ⇒ 红");
    }

    // ==============================================================================================
    //  件1 · 真卡：三张裸 `Deal` 卡 + 一张 `lowest Health` 卡
    // ==============================================================================================

    static void G5BareDealRealCards()
    {
        var pool = CardDatabase.Load();
        CheckTrue(pool != null && pool.Count > 0, "（前提）卡池读得到（" + (pool == null ? 0 : pool.Count) + " 张）");
        if (pool == null) return;

        // 三张裸 Deal（卡面逐字：`Deal 1 damage` / `Deal 3 damage` / `Deal 2 damage`）
        // —— `AM5 Shock Trooper`（单位）· `TAU44 Pulse Onslaught`（战术）· `SAU38 Extermination Protocol`（战术）
        string[] ids = { "AM5", "TAU44", "SAU38" };
        for (int k = 0; k < ids.Length; k++)
        {
            var card = CardDatabase.FindById(pool, ids[k]);
            CheckTrue(card != null, $"（前提）池里有 `{ids[k]}`");
            if (card == null) continue;
            var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
            int deals = 0, ok = 0;
            if (ops != null)
            {
                for (int i = 0; i < ops.Count; i++)
                {
                    var o = ops[i];
                    if (o == null || o.Verb != "deal" || o.Target == null) continue;
                    deals++;
                    if (!o.Target.Auto && string.IsNullOrEmpty(o.Target.PickMost)
                        && o.Target.Side == "enemy" && o.Target.Count == 1) ok++;
                }
            }
            CheckTrue(deals > 0, $"（前提）`{ids[k]}`（{card.Name}）解析出了 deal op");
            Check(ok, deals,
                  $"★★ 真卡 `{ids[k]} {card.Name}`（「{card.Desc}」）：每一条 deal 都是"
                + "「敌方 · 一个 · 由施放方选」（`ta=30`）"
                + "｜🧨 任一条回到 `Auto`/`PickMost = -health` ⇒ 这一格少 1 ⇒ 红");
        }

        // 反向的真卡：`SAU73 Annihilation Barge` 的 `Artifice:` —— 卡面明写 `lowest Health`（`ta=240`）
        var barge = CardDatabase.FindById(pool, "SAU73");
        CheckTrue(barge != null, "（前提）池里有 `SAU73`（Annihilation Barge）");
        if (barge != null)
        {
            var ops = EffectText.Parse(barge.Desc, out _, out _, card: barge);
            string pm = "";
            if (ops != null)
            {
                for (int i = 0; i < ops.Count; i++)
                    if (ops[i] != null && ops[i].Verb == "deal" && ops[i].Target != null)
                        pm = ops[i].Target.PickMost ?? "";
            }
            Check(pm, "-health",
                  "★★ 真卡 `SAU73` 的 `Deal 3 damage to the enemy with lowest Health` ⇒ **仍带** `PickMost = -health`"
                + "（`ta=240` 与裸 Deal 的 `ta=30` 是两条路，⛔ 别一起改）"
                + "｜🧨 把 `lowest Health` 也并进裸 Deal 那一支 ⇒ 实得空串 ⇒ 红");
        }
    }

    // ==============================================================================================
    //  件2 · `ta = 200 / 201` 的 `referencedCards`（380 `setReferenceCard` 写 → 200/201 读）
    // ==============================================================================================

    static void G5ReferencedCards()
    {
        Section("G5·件2：`ta=200/201` 的 `referencedCards`（380 `setReferenceCard` 写入 → 读表）");

        var enemyAll = new EffectTargetSpec
        {
            Raw = "(G5 夹具：ta=150 全表，取敌方部队)", Side = "enemy", Kind = "troop", Count = 0,
        };
        // 🔴 读侧故意给一条 **`Side = own`** 的规格：原版 200/201 **完全绕开** criteria
        //    ⇒ 若读侧没接上、落回文本 spec，得到的会是**自己那侧**的单位（与引用表结构上不同名）。
        var readSpec = new EffectTargetSpec
        {
            Raw = "(G5 夹具：读 200/201 时的文本规格 —— 原版不看它)", Side = "own", Kind = "any", Count = 0,
        };

        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int me = ctx.Active, foe = 1 - ctx.Active;
        var acting = Place(ctx, me, 3, Unit("G5Acting", 1, 0, 5));
        var e1 = Place(ctx, foe, 3, Unit("G5RefA", 1, 0, 5));
        var e2 = Place(ctx, foe, 5, Unit("G5RefB", 1, 0, 5));

        // ---- ① 380 写入：引用表 = 那条 logic 自己的目标表（这里是敌方两家部队）----
        int n = RuleCore.SetReferenceCards(ctx, me, acting, enemyAll);
        Check(n, 2,
              "★★ ① 能力 380 `setReferenceCard` 把「这条 logic 自己的目标表」写进引用表（2 张）"
            + "（判据 `AbilityLogic__PlayAbility.c:2128-2160`：先换新 List、再逐个 Add）"
            + "｜🧨 消费函数不写（只留结构）⇒ 实得 0 ⇒ 红");
        CheckTrue(acting.ReferencedCards.Contains(e1) && acting.ReferencedCards.Contains(e2),
                  "① 引用表里就是那两张（按**引用**认，不按卡名）"
                + "｜🧨 写成「按卡名/卡 id 存」⇒ Contains 为假 ⇒ 红");

        // ---- ② 读 `ta=200`：整表（**不看**文本 spec 的 `Side=own`）----
        var op200 = new EffectOp { Verb = "deal", Amount = 1, SubjectTargetsAffected = 200, Target = readSpec };
        var got200 = RuleCore.ResolveTargets(ctx, me, readSpec, acting, null, null, op200);
        Check(got200.Count, 2,
              "★★ ② `ta=200` = **整表 AddRange**（`AbilityLogic__GetTargets.c:770-781`，不过筛不 Trim）"
            + "｜🧨 读侧不接 200 ⇒ 落回文本 spec（`Side=own`）⇒ 实得 2 张**自己那侧**（含督军）⇒ 名字对不上 ⇒ 红");
        CheckTrue(got200.Contains(e1) && got200.Contains(e2),
                  "② ……就是引用表里那两张"
                + "｜🧨 同上（落回 own 池 ⇒ 两张敌方都不在）⇒ 红");
        CheckTrue(!got200.Contains(ctx.Players[me].Warlord) && !got200.Contains(acting),
                  "★★ ② **不是**照文本 spec 现筛的池（那会把己方督军/施放者收进来）"
                + "｜🧨 读侧忽略 `ReferencedCards` ⇒ 实得己方督军 + 施放者 ⇒ 红");

        // ---- ③ `ta=201`：只留 `IsInPlay`；`ta=200` 仍然整表（两者必须分开）----
        ctx.Players[foe].Board[5] = null;                  // `G5RefB` 离场（不动引用表本身）
        var op201 = new EffectOp { Verb = "deal", Amount = 1, SubjectTargetsAffected = 201, Target = readSpec };
        var got201 = RuleCore.ResolveTargets(ctx, me, readSpec, acting, null, null, op201);
        var got200b = RuleCore.ResolveTargets(ctx, me, readSpec, acting, null, null, op200);
        Check(got201.Count, 1,
              "★★ ③ `ta=201` = 只留 `IsInPlay` 的（`AbilityLogic__GetTargets.c:784-806`）"
            + "｜🧨 201 不筛在场（照 200 处理）⇒ 实得 2 ⇒ 红");
        CheckTrue(got201.Count == 1 && ReferenceEquals(got201[0], e1),
                  "③ ……留下的是还在场上的 `G5RefA`"
                + "｜🧨 筛错方向（留离场的那个）⇒ 实得另一个对象 ⇒ 红");
        Check(got200b.Count, 2,
              "★★ ③ 同一张引用表：`ta=200` **整表**（离场的那个也在）"
            + "｜🧨 把 200 也按 201 筛 ⇒ 实得 1 ⇒ 红（与上一条互为灭自证）");

        // ---- ④ 灭自证：**不消费 380** ⇒ 引用表空 ⇒ 200/201 都得空表（⛔ 不许「回落到目标池」）----
        var acting2 = Place(ctx, me, 5, Unit("G5ActingNoRef", 1, 0, 5));
        var empty = RuleCore.ResolveTargets(ctx, me, readSpec, acting2, null, null, op200);
        Check(empty.Count, 0,
              "★★ ④ **没被 380 写过**的那张牌：`ta=200` 得**空表**（原版是读它自己的 `+0x278`）"
            + "｜🧨 读侧在引用表为空时「回落到现筛目标池」⇒ 实得 己方督军/施放者 若干 ⇒ 红"
            + "（与 ①② 互为灭自证：**200/201 的语义完全由 380 的写入决定**）");

        // ---- ⑤ 380 是**覆盖**不是追加（原版 `:2139-2143` 先换一张新 List）----
        var onlyA = new EffectTargetSpec
        {
            Raw = "(G5 夹具：ta=150 只取第一个敌方部队)", Side = "enemy", Kind = "troop", Count = 1,
        };
        int n2 = RuleCore.SetReferenceCards(ctx, me, acting, onlyA);
        Check(n2, 1,
              "★★ ⑤ 再写一次 ⇒ 引用表被**覆盖**成 1 张（原版先换新 List 再 Add）"
            + "｜🧨 写成 `AddRange` 追加 ⇒ 实得 3（2 + 1）⇒ 红");
        CheckTrue(acting.ReferencedCards.Count == 1 && ReferenceEquals(acting.ReferencedCards[0], e1),
                  "⑤ ……留下的正是这次那条 logic 的目标（`G5RefA`）"
                + "｜🧨 追加而不是覆盖 ⇒ Count == 3 ⇒ 红");

        // ---- ⑥ 真卡数据钉 + 「今天不改变这两张的行为」----
        //    判据 → `资料/普查产出_1024/C5_剩余判据普查.md` §3：池内只有 2 张用 200/201。
        var pool = CardDatabase.Load();
        var am54 = CardDatabase.FindById(pool, "AM54");     // At All Costs（战术）
        var gof93 = CardDatabase.FindById(pool, "GOF93");   // Hunta Rig（单位）
        CheckTrue(am54 != null && gof93 != null, "（前提）池里有 `AM54` / `GOF93`");
        if (am54 != null && gof93 != null)
        {
            CheckTrue(HasTa(am54, 200),
                      "★★ ⑥ 数据钉：`AM54 At All Costs` 的 `abilityTargets` 里有 **`ta=200`** 那一行"
                    + "（`trigger 410 FollowUpResolution`；判据 C5 §3）"
                    + "｜🧨 卡表重生成时丢了 200 ⇒ 红（那时 `ta=200` 在本作里失去唯一的数据入口）");
            CheckTrue(HasTa(gof93, 201),
                      "★★ ⑥ 数据钉：`GOF93 Hunta Rig` 的 `abilityTargets` 里有 **`ta=201`** 那一行"
                    + "｜🧨 同上 ⇒ 红");
            CheckTrue(!HasTa(am54, 201) && !HasTa(gof93, 200),
                      "⑥ 两张卡各自只占一档（200 / 201 不会互相顶替）"
                    + "｜🧨 数据串档 ⇒ 红");

            Check(CountTaOps(am54, 200, 201), 0,
                  "★★ ⑥ **今天的行为不变**：`AM54` 的**卡面文字管线**没有一条 op 带 200/201"
                + "（⇒ 本件补的结构对它的现有行为**零影响**；它的效果仍由文字管线覆盖）"
                + "｜🧨 谁给文字管线硬塞一条 200/201 ⇒ 这一格 > 0 ⇒ 红（那必须同时改报告 §件2 的结论）");
            Check(CountTaOps(gof93, 200, 201), 0,
                  "★★ ⑥ 同上：`GOF93` 的文字管线也没有 200/201 的 op（行为不变）"
                + "｜🧨 同上 ⇒ 红");
        }
    }

    /// <summary>`card` 的 `abilityTargets` 里有没有某一条 logic 的 `targetsAffected == ta`。</summary>
    static bool HasTa(CardDef card, int ta)
    {
        if (card == null || card.AbilityTargets == null) return false;
        for (int a = 0; a < card.AbilityTargets.Count; a++)
        {
            var spec = card.AbilityTargets[a];
            if (spec == null || spec.TargetsAffected == null) continue;
            for (int j = 0; j < spec.TargetsAffected.Length; j++)
                if (spec.TargetsAffected[j] == ta) return true;
        }
        return false;
    }

    /// <summary>`card` 的**卡面文字管线**解析出来的 op 里，`SubjectTargetsAffected` 落在
    /// `[ta1, ta2]` 这一族的有几条（0 = 这两档今天在文字路线上**不可达**）。</summary>
    static int CountTaOps(CardDef card, int ta1, int ta2)
    {
        if (card == null) return 0;
        var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
        int n = 0;
        if (ops == null) return 0;
        for (int i = 0; i < ops.Count; i++)
            if (ops[i] != null && (ops[i].SubjectTargetsAffected == ta1 || ops[i].SubjectTargetsAffected == ta2)) n++;
        return n;
    }
}
