// RuleEngineTest_EngineB7.cs — 引擎线 `B7` 的断言（2026-10-24）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_EngineA1` / `_EngineB3` / `_EngineB6` / `_EngineP1` / `_Diag` … 都在），
// 而**同一时刻只有一个写手改一个文件** ⇒ 本批测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一段 `Section(...)` + 一行 `Step(...)`**。
//
// 两件判据（**第一权威 = `d:/2/tools/decomp_full/`，本轮现读**；⛔ 别改回「看着像」的旧口径）：
//   ① `Deal N damage` 的「生命最低」挑法 = 原版 `BattleManager__GetLowestHealthUnit`
//      （`AbilityLogic__GetTargets.c:835-844` 的 `ta = 240` 那一档）：
//      候选闸 `IsInPlay ∧ !EnoughPendingDamageToDie ∧ !isRemnant` · 初始 best = 该侧督军 ⇒ 并列督军优先 ·
//      遍历序 = `allCardsInPlay` 追加序（= `UnitState.PlayOrder`）。
//      🔴 **2026-10-24（`E6`）就地订正**：本行原来还写「**裸 `Deal N damage` 走的正是它**」——
//      **已证伪**：裸 Deal 是 `ta = 30 (target)`（`AbilityLogic__GetTargets.c:192-207`；全池 33/33，
//      判据 → `资料/普查产出_1024/C4_ta240是否含督军.md` §六·2），由施放方选目标；
//      `ta = 240` 只有卡面明写 `with lowest Health` 的卡才走（全池 `SAU70` / `SAU73` 两张）。
//      ⇒ 本文件 ①~⑤ 用手搓 `B7Lowest(...)` 规格（= `ta=240` 的形状）仍在守这条挑法；
//        ⑥ 也已**换成真卡 `SAU73 Annihilation Barge`**，不再拿裸 Deal 当样本。
//   ② `diedsincelastturn` 的**侧**：`enemy` 句式以前被 `since **your** last turn` 里的 `your` 误判成 `own`
//      （独立审查 R1 `P2-10`）；原版这一位 = `multiCountCriteria.friendlyUnits/enemyUnits` 两个布尔位。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量，⛔ 不从实现常量反算（那会自证）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestB7EnginePickAndCount)`）。
    /// 两个子用例是 `B7…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestB7EnginePickAndCount()
    {
        Section("🆕 引擎线 B7：`… with lowest Health` 的挑法（`GetLowestHealthUnit` / `ta=240`）· `diedsincelastturn` 的侧");
        B7LowestHealthPick();
        B7DiedSinceLastTurnSide();
    }

    /// <summary>一个「敌方、生命最低、取一个」的目标规格 —— 与卡面 `… with lowest Health`
    /// 解析出来的**同一个形状**（`EffectText.TryDeal` 里 lowest/highest 那一支，`spec.PickMost = "-health"`；
    /// 原版 = `ta 240`）。
    /// 🔴 **2026-10-24（`E6`）订正**：这里原来写「与**裸 `Deal N damage`** 解析出来的同一个形状」——
    /// 裸 Deal 已按原版改回 `ta = 30 (target)`（不带 `PickMost`）⇒ **不再同形**，⛔ 别再拿它当样本。</summary>
    static EffectTargetSpec B7Lowest(string kind)
    {
        return new EffectTargetSpec
        {
            Raw = "(B7: 生命最低)", Side = "enemy", Kind = kind, Count = 1, Auto = true,
            PickMost = "-health",
        };
    }

    // ==============================================================================================
    //  ① `… with lowest Health` 的「生命最低」= 原版 `BattleManager__GetLowestHealthUnit`（`ta = 240`）
    //     🔴 2026-10-24（`E6`）：原来这里写的是「`Deal N damage` 的『生命最低』」—— 裸 Deal 是 `ta=30`，
    //        不设 `PickMost`；本组用的是手搓的 `B7Lowest(...)`（= `ta=240` 的形状），口径没变。
    // ==============================================================================================

    /// <summary>
    /// 判据（本轮现读 `d:/2/tools/decomp_full/BattleManager__GetLowestHealthUnit.c`）：
    ///   · `:73-88` 收集 + `:122-128` 复核 = 候选闸 `IsInPlay ∧ !EnoughPendingDamageToDie ∧ !isRemnant(+0x65)`；
    ///   · `:100-109` 初始 best = `GetHero(manager, param_2)`（= **该侧督军**）⇒ **并列时督军优先**；
    ///   · `:66` 遍历 `*(manager + 0x470)` = `allCardsInPlay`（**进场追加序** ⇒ 我们的 `PlayOrder`）；
    ///   · `:153` 比 `currentHealth`，**严格小于**才换人 ⇒ 并列留先者。
    /// 分派 = `AbilityLogic__GetTargets.c:835-844`（`iVar6 == 0xf0`）⇒ 取**一个**、不过筛、不 Trim。
    /// </summary>
    static void B7LowestHealthPick()
    {
        // ---- ① 同血「部队 vs 督军」⇒ **挑督军**（原版初始 best 就是它）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            var foeW = ctx.Players[foe].Warlord;
            foeW.Health = 3;                                   // 与下面那支部队**同血**
            var troop = Place(ctx, foe, 3, Unit("B7WlTroop", 1, 1, 3));

            var got = RuleCore.ResolveTargets(ctx, me, B7Lowest("any"), null, null);
            Check(got.Count, 1, "★ ① 「生命最低」只返回**一个**目标（原版 `GetLowestHealthUnit` 只给一个）"
                              + "｜🧨 当成 `allFiltered + 排序` ⇒ 实得 2（部队 + 督军）⇒ 红");
            Check(got.Count > 0 ? got[0].Name : "(null)", "FixtureWarlord",
                  "★★ ① **并列时督军优先**（原版 `:100-109` 的初始 best = 该侧督军，`:153` 严格小于 ⇒ 同血不换人）"
                + "｜🧨 回到改前的 `foreach` + 严格小于（并列取池里先出现的）⇒ 实得 `B7WlTroop`"
                + "（它在我方格号 3，督军在 4 ⇒ 池序里它在前面）⇒ 红");
            CheckTrue(troop != null && foeW != null && troop.Name != foeW.Name, "① （前提）两个候选确实是不同的对象");
        }

        // ---- ② 同血两个**部队** ⇒ 挑 **`PlayOrder` 在先**的那个（原版遍历 `allCardsInPlay` 追加序）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            // 🔴 故意让「进场早」的那个在**更大的格号**上：格号序与进场序相反 ⇒ 两套口径能分开。
            var early = Place(ctx, foe, 5, Unit("B7TieEarly", 1, 1, 2));    // 进场早、格号 5
            var late = Place(ctx, foe, 3, Unit("B7TieLate", 1, 1, 2));      // 进场晚、格号 3
            CheckTrue(early.PlayOrder >= 0 && late.PlayOrder > early.PlayOrder,
                      "② （前提）`Place` 取了进场号，且 `B7TieEarly` 确实早于 `B7TieLate`"
                    + $"（{early.PlayOrder} < {late.PlayOrder}）");

            var got = RuleCore.ResolveTargets(ctx, me, B7Lowest("any"), null, null);
            Check(got.Count > 0 ? got[0].Name : "(null)", "B7TieEarly",
                  "★★ ② 两个部队同血 ⇒ 取**进场序在先**的那个（原版遍历 `allCardsInPlay` = 进场追加序）"
                + "｜🧨 按格号序（= 改前的池序）⇒ 实得 `B7TieLate`（格号 3 在前面）⇒ 红；"
                + "🧨 并列取后者 ⇒ 也实得 `B7TieLate` ⇒ 红");
        }

        // ---- ③ **残骸**不入候选（`Health = 1`、`IsAlive` 真）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            var rem = Place(ctx, foe, 3, Unit("B7RemRemnant", 1, 1, 1));
            rem.IsRemnant = true;                              // 引擎里由 `CleanupDeaths` 那条路置真
            var live = Place(ctx, foe, 5, Unit("B7RemLive", 1, 1, 5));
            CheckTrue(rem.IsAlive && rem.Health == 1, "③ （前提）残骸 `Health = 1` 且 `IsAlive` 为真"
                                                     + "（⇒ 只判 `IsAlive` 会把它当成最低血）");

            var got = RuleCore.ResolveTargets(ctx, me, B7Lowest("any"), null, null);
            Check(got.Count > 0 ? got[0].Name : "(null)", "B7RemLive",
                  "★★ ③ **残骸不进「生命最低」的候选**（原版 `BattleManager__GetLowestHealthUnit.c:75` 的 "
                + "`*(char *)(card + 0x65) == 0` = `!isRemnant`）"
                + "｜🧨 删掉 `if (u.IsRemnant) continue;` ⇒ 实得 `B7RemRemnant`（1 血）⇒ 红");
        }

        // ---- ④ 待死（`waitingToDie`）/ 判死单位不入候选 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            var dying = Place(ctx, foe, 3, Unit("B7PendDying", 1, 1, 1));
            ctx.AddPendingDeath(foe, 3, -1);                   // 已入将死队列、但仍在棋盘上
            var live = Place(ctx, foe, 5, Unit("B7PendLive", 1, 1, 5));
            CheckTrue(ctx.IsPendingDeath(dying) && dying.IsAlive,
                      "④ （前提）那个单位**已进将死队列**、但它 `Health` 仍 > 0（`IsAlive` 真）"
                    + "—— `Destroy` 类效果打死的单位就是这一形状");

            var got = RuleCore.ResolveTargets(ctx, me, B7Lowest("any"), null, null);
            Check(got.Count > 0 ? got[0].Name : "(null)", "B7PendLive",
                  "★★ ④ **待死单位不进候选**（原版 `IsInPlay` 不含 `waitingToDie(5)`，且 "
                + "`!EnoughPendingDamageToDie` 那一道也挡它 —— `CardScript__EnoughPendingDamageToDie.c:30` "
                + "对 `cardState == 5` 直接 `return 1`）"
                + "｜🧨 删掉 `if (ctx.IsPendingDeath(u)) continue;` ⇒ 实得 `B7PendDying`（1 血）⇒ 红");
        }

        // ---- ⑤ 督军**不在池里**时不许预置它（卡面写了 `troop` ⇒ 督军本来就不是候选）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            ctx.Players[foe].Warlord.Health = 1;               // 全场最低，但它是**督军**
            var t = Place(ctx, foe, 3, Unit("B7TroopOnly", 1, 1, 4));

            var got = RuleCore.ResolveTargets(ctx, me, B7Lowest("troop"), null, null);
            Check(got.Count > 0 ? got[0].Name : "(null)", "B7TroopOnly",
                  "★★ ⑤ `Kind = troop` ⇒ 督军**不在候选池**里，所以「初始 best = 督军」那一条**不许**"
                + "把一个被卡面筛掉的对象当目标（本件只在督军自己也在候选表里时才预置）"
                + "｜🧨 无条件把 `best` 预置成该侧督军 ⇒ 实得 `FixtureWarlord`（1 血）⇒ 红");
            CheckTrue(t != null, "⑤ （前提）部队摆在那儿");
        }

        // ---- ⑥ 端到端：真解析链（卡面 `… with lowest Health` → `PickMost = -health`）在**残骸**身上空过 ----
        //    🔴 **2026-10-24（`E6`）换样本**：本格原来拿裸 `Deal 2 damage` 当 `-health` 的样本 ——
        //       那条**已按原版改回 `ta = 30 (target)`**（`AbilityLogic__GetTargets.c:192-207`；全池 33/33
        //       张裸 Deal = `ta=30`）⇒ **不再带 `PickMost`**，守不住「生命最低」这条挑法了（G5 §1·5）。
        //       ⇒ 换成卡面**真写了 `with lowest Health`** 的 `SAU73 Annihilation Barge`
        //       （原版 `ta = 240`；全池只有 `SAU70` / `SAU73` 两张，且 `SAU70` 是 `Attacks … lowest Health`
        //        不是 deal ⇒ 唯一可选的就是这张）。规格从卡池现读，⛔ 不是手抄的字符串。
        //    🔑 **判别力没减**：残骸（1 血、`IsAlive` 真）排在**池序/格号更前**的槽 3，活得那支在槽 5 ⇒
        //       少了 `!isRemnant` 那道闸，3 点伤害会落到残骸身上（1 → 0）、6 血那支不动 ⇒ 下面两条同时红。
        {
            var pool = CardDatabase.Load();
            var barge = CardDatabase.Find(pool, "Annihilation Barge");
            CheckTrue(barge != null, "⑥ （前提）卡池里找得到 `Annihilation Barge`（`SAU73`，全池唯一带 deal 的 `lowest Health` 卡）");
            var ops = barge == null ? null : EffectText.Parse(barge.Desc, out _, out _);
            CheckTrue(ops != null && ops.Count == 1 && ops[0].Target != null
                      && ops[0].Amount == 3 && ops[0].Target.PickMost == "-health",
                      "⑥ （前提）卡面 `" + (barge == null ? "(找不到该卡)" : barge.Desc) + "` ⇒ **一条** `deal 3`、"
                    + "目标规格带 `PickMost = -health`（原版 `ta = 240`，`AbilityLogic__GetTargets.c:835-844`）"
                    + "｜🧨 卡面改成裸 Deal / 解析丢了 `PickMost` ⇒ 红");

            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            var rem = Place(ctx, foe, 3, Unit("B7E2eRemnant", 1, 1, 1));
            rem.IsRemnant = true;
            var live = Place(ctx, foe, 5, Unit("B7E2eLive", 1, 1, 6));
            RuleCore.ResolveOps(ctx, me, null, ops, "B7 自检 端到端");

            Check(live.Health, 3, "★★ ⑥ 端到端：3 点伤害打**活着的那支部队**（6 → 3）"
                                + "｜🧨 候选闸少了 `!isRemnant` ⇒ 伤害落到残骸上 ⇒ 实得 6 ⇒ 红");
            Check(rem.Health, 1, "⑥ 端到端：**残骸一点没挨**｜🧨 同上（残骸 1 血，会被打掉）⇒ 实得 0 ⇒ 红");
        }
    }

    // ==============================================================================================
    //  ② `diedsincelastturn` 的**侧**（own / enemy / any）
    // ==============================================================================================

    /// <summary>
    /// 判据：原版这一位来自 `multiCountCriteria` 的 `friendlyUnits(+0x14)` / `enemyUnits(+0x15)`
    /// 两个布尔位（`AbilityLogic__GetTargets.c:207-219` 那一段同一来源）；窗口 =
    /// `CardScript__HasDiedSinceLastTurn.c:26-47`（本回合 ∨ 上一回合，A1 已钉）。
    ///
    /// **改前的缺陷**（独立审查 R1 `P2-10`）：`ownD = c.Contains("friendly") || c.Contains("your")` ——
    /// 而时间窗短语 `since **your** last turn` **自己就带 `your`** ⇒ 判据恒真 ⇒
    /// 「`for each enemy troop that died since your last turn`」被分成 `own`（少数一半、静默）。
    /// 夹具故意造成 **P1 死 2 个 / P0 死 1 个**，让 `own` 与 `enemy` 的读数**必然不同**（否则分不开）。
    /// </summary>
    static void B7DiedSinceLastTurnSide()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);                                  // Turn = 1（P0 行动）
        Check(ctx.Turn, 1, "② （前提）现在第 1 回合");
        Check(ctx.DeadUnits.Count, 0, "② （前提）开局还没人阵亡（下面那 3 条都是本用例造成的）");

        var e0 = Place(ctx, 1, 3, Unit("B7DeadEnemy0", 1, 0, 1));
        var e1 = Place(ctx, 1, 5, Unit("B7DeadEnemy1", 1, 0, 1));
        var m0 = Place(ctx, 0, 3, Unit("B7DeadOwn0", 1, 0, 1));
        RuleCore.HurtForTest(ctx, e0, 99, "夹具");
        RuleCore.HurtForTest(ctx, e1, 99, "夹具");

        PassTurn(ctx);                                     // → 第 2 回合（P1 行动）
        Check(ctx.Turn, 2, "② （前提）现在第 2 回合（窗口 = 本回合 ∨ 上一回合 ⇒ 两批都在窗口里）");
        RuleCore.HurtForTest(ctx, m0, 99, "夹具");

        Check(ctx.DeadUnits.Count, 3, "② （前提）本用例造成 3 条阵亡记录（P1 两个 / P0 一个）");
        int own1 = 0, enemy1 = 0, own0 = 0, enemy0 = 0;
        for (int i = 0; i < ctx.DeadUnits.Count; i++)
        {
            if (ctx.DeadUnits[i].Owner == 1) own1++; else own0++;
        }
        Check(own1, 2, "② （前提）`DeadUnits` 里 P1 那侧 2 条");
        Check(own0, 1, "② （前提）P0 那侧 1 条");
        enemy1 = own0; enemy0 = own1;                      // 对某一方而言，「敌方死者」= 另一方的死者

        var opOwn = new EffectOp { CountScope = "diedsincelastturn", CountRef = "own|troop" };
        var opEnemy = new EffectOp { CountScope = "diedsincelastturn", CountRef = "enemy|troop" };
        var opAny = new EffectOp { CountScope = "diedsincelastturn", CountRef = "any|troop" };

        // ---- 消费端：三档互不相同（互为灭自证）----
        Check(RuleCore.CountForTest(ctx, 1, null, opOwn), own1,
              "★★ ② `own|troop` 在 owner=1 上 = **自己那侧**的阵亡数（2）"
            + "｜🧨 不看 `d.Owner`（两侧都数）⇒ 实得 3 ⇒ 红");
        Check(RuleCore.CountForTest(ctx, 1, null, opEnemy), enemy1,
              "★★ ② **`enemy|troop` 在 owner=1 上 = 另一侧（P0）的阵亡数（1）** —— 第三档真的分开了"
            + "｜🧨 消费端只认 `own`（`enemy` 落进「不筛」）⇒ 实得 3 ⇒ 红；"
            + "🧨 分类层把 `enemy` 折成 `own`（改前的病）⇒ 这一格也会得到 2 ⇒ 红（与上一条互为灭自证）");
        Check(RuleCore.CountForTest(ctx, 1, null, opAny), 3,
              "② `any|troop` = 两侧合计（3）"
            + "｜🧨 让 `any` 也按 `own` 筛 ⇒ 实得 2 ⇒ 红");

        Check(RuleCore.CountForTest(ctx, 0, null, opEnemy), enemy0,
              "★★ ② 换一方：`enemy|troop` 在 owner=0 上 = P1 那侧的阵亡数（2）"
            + "｜🧨 侧判反（`enemy` 当成「自己」）⇒ 实得 1 ⇒ 红");
        Check(RuleCore.CountForTest(ctx, 0, null, opOwn), own0,
              "② `own|troop` 在 owner=0 上 = 自己那侧（1）"
            + "｜🧨 与上一条互相顶替 ⇒ 两条同时红");

        // ---- 分类层：**时间窗里的 `your` 不许算成侧**（R1 `P2-10` 的正题）----
        {
            var ops = EffectText.Parse("Repeat for each enemy troop that died since your last turn",
                                       out _, out _);
            CheckTrue(ops != null && ops.Count > 0, "② （前提）敌方句式解析出了 op");
            if (ops != null && ops.Count > 0)
                Check(ops[0].CountRef, "enemy|troop",
                      "★★ ② 「`for each **enemy** troop that died since **your** last turn`」⇒ `enemy|troop`"
                    + "｜🧨 改前的 `c.Contains(\"friendly\") || c.Contains(\"your\")`（时间窗自带 `your` ⇒ 恒真）"
                    + "⇒ 实得 `own|troop` ⇒ 红");
        }
        {
            var ops = EffectText.Parse("Repeat for each friendly troop that died since your last turn",
                                       out _, out _);
            CheckTrue(ops != null && ops.Count > 0, "② （前提）己方句式解析出了 op");
            if (ops != null && ops.Count > 0)
                Check(ops[0].CountRef, "own|troop",
                      "★★ ② 己方句式仍是 `own|troop`（`UM77 Chairon` 的卡面就是这一句 —— 今天的现役形状）"
                    + "｜🧨 把侧一律判成 `enemy` / `any` ⇒ 实得 `enemy|troop` / `any|troop` ⇒ 红"
                    + "（与上一条互为灭自证）");
        }
        {
            var ops = EffectText.Parse("Repeat for each troop that died since your last turn",
                                       out _, out _);
            CheckTrue(ops != null && ops.Count > 0, "② （前提）没写侧的句式解析出了 op");
            if (ops != null && ops.Count > 0)
                Check(ops[0].CountRef, "any|troop",
                      "② 没写侧 ⇒ `any|troop`（两侧都数）—— 原版这一位是 `friendlyUnits/enemyUnits` "
                    + "两个布尔位都没置 ⇒ 不筛"
                    + "｜🧨 缺省当成 `own` ⇒ 实得 `own|troop` ⇒ 红");
        }
    }
}
