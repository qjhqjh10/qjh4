// RuleEngineTest_EngineB94W3.cs — 第九十四会话 · 波 1.5 · 执行代理 `W3`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// 本件只放本批的断言；宿主 `RuleEngineTest.cs` 里加**一个** `{ CheckB94W3DeathTurnPredicate(); }` 块
// （⚠️ 方法名**不用 `Test`/`Report` 打头** ⇒ 不被 `RunCoverageCompiledCases` / `gen_diag_runner`
// 认成用例，与 `B94_F1` / `B93_G1` 那几件的既有做法同形；**接线由调度台做**，本代理不碰宿主）。
//
// ============================================================================================
//  本件钉的是什么
// ============================================================================================
//  R-H（`判据/B94_R-H_死亡回合谓词VA复核.md`）用**机器码**把两条谓词定死了：
//    · `CardScript.HasDiedThisTurn()`    RVA `0x5EF960` ⇒ `return turnDied == turnCounter;`
//    · `CardScript.HasDiedSinceLastTurn()` RVA `0x5EF850` ⇒
//        `turnDied == turnCounter || (turnDied + 1) == turnCounter`（`lea ecx,[rdi+1]` ⇒ **`+1` 加在 `turnDied` 上**）
//  我们自己 `ConditionEval` 的 `case 221 / 222`（`ConditionEval.cs:324/331`）与它**逐字等价**
//  （`DiedInTurn(..., 0) || DiedInTurn(..., 1)` ⇒ `DeathTurn == ctx.Turn || DeathTurn == ctx.Turn - 1`）。
//
//  🔴 **唯一的行为分歧**（R-H §3.1）：原版**对活卡没有闸** —— 活卡的 `turnDied == 0`
//     （`.ctor` / `CardSetup` 都不写 `+0x250`，唯一生产写点 = 临死时烙 `turnCounter`），
//     `turnCounter` 初值 0 ⇒ 原版在 `turnCounter == 0` 时 221/222 都真、`== 1` 时 222 真；
//     我们 `ConditionEval` 是 `u != null ⇒ false`（= 把原版这个行为「修好了」）。
//     铁律 18·0 要求**照抄原版（含 bug）**，但照抄的前提是**可达**（这里能观测到差别）。
//
//  ⇒ 本件的第一件事就是**可达性探测**（`B94W3T1Reachability`）：把池内所有 221/222 行的
//     **候选域**逐条钉住，证明**没有任何一条会拿活卡当主语**：
//       ① 全池只有 5 张卡用这两个条件、共 14 行（多一张就要重做本结论）；
//       ② 会求值的那 10 格它的候选域全是 **`ta = 60 inCemeteryFiltered`（墓地）**；
//          剩下 3 格的 `ta` 是 `30 / 0 / 0`（`SW44` 两条 + `UM77` 的 `multi`），而
//          **原版对这几档根本不求值 `conditionsFilter`**（`TargetCriteriaEval.EarlyReturnTargetsAffected`，
//          判据 `FilterMethods__CheckIfMeetsCriteria.c:57-157` 早返 ⇒ 那几格是**死字段**）；
//       ③ **全池没有任何 `lg == -1` 的 221/222 `target` 行** ⇒ ability 级那两条闸
//          （`ActionPump.AbilityLevelConditionRow` / `RuleCore.CanUseAbility`，两者都只认 `lg = -1`）
//          结构上取不到它们。
//  ⇒ 结论 = **不可达** ⇒ 本批**零代码改动**（`ConditionEval.cs` 一个字没动）；
//     本件把「不可达」的三个支点 + 三态行为**都钉成断言**，将来真有卡把它变得可达时**这里会红**。
//
//  ⚠️ **将来若变红怎么办**：按铁律 18·0 **照抄原版** —— 去掉 `u != null ⇒ false` 那道闸、
//     改成「活卡按 `turnDied == 0` 参与比较」（并在注释里写死「原版对活卡没有闸，⛔ 别修」）。
//     ⛔ 不许反过来「因为观感更合理」保留我们的写法 —— 那是自造口径。
//
//  🔴 期望值一律写死字面量（5 张 / 14 行 / `10 墓 + 3 死字段 + 1 卡级` / 每张卡的 `role+cond` 串），⛔ 不从被测实现反算。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB94W3DeathTurnPredicate()
    {
        Section("🆕 B94_W3：`HasDiedThisTurn(221)` / `HasDiedSinceLastTurn(222)` —— 活卡可达性探测（VA 已定语义）");
        B94W3T1Reachability();
        B94W3T2ThreeStates();
    }

    // ==================================================================
    //  ① 可达性：221/222 的候选域逐条钉住（结论 = 活卡结构上进不来）
    // ==================================================================
    static void B94W3T1Reachability()
    {
        var pool = CardDatabase.Load();
        CheckTrue(pool != null && pool.Count > 0, "前提：卡池读得到（`CardDatabase.Load()`）");
        if (pool == null || pool.Count == 0) return;

        string[] ids = { "EC39", "EC58", "SOR42", "SW44", "UM77" };

        // ---- ①a 全池扫一遍：带 221/222 的卡**只有这 5 张、共 14 行** ----
        int cards = 0, rows = 0;
        var extra = new List<string>();
        foreach (var c in pool)
        {
            if (c == null || c.Conditions == null) continue;
            int n = 0;
            foreach (var r in c.Conditions)
                if (r != null && (r.Condition == 221 || r.Condition == 222)) n++;
            if (n == 0) continue;
            cards++; rows += n;
            if (System.Array.IndexOf(ids, c.Id) < 0) extra.Add(c.Id + "×" + n);
        }
        // 🧨 卡表里出现第 6 张用 221/222 的卡 ⇒ 本条红（那时「不可达」的结论必须重做，⛔ 别硬留着）
        Check(cards, 5, "★★ 全池带 221/222 的卡**只有 5 张**（`EC39/EC58/SOR42/SW44/UM77`）"
                      + "｜🧨 多一张 ⇒ 可达性结论作废、回源重查");
        Check(rows, 14, "★★ 一共 **14 行**（`EC39`×3 · `EC58`×2 · `SOR42`×5 · `SW44`×2 · `UM77`×2）");
        Check(extra.Count, 0, "★★ 没有第 6 张卡（多出来的：" + string.Join("、", extra.ToArray()) + "）");

        // ---- ①b 逐卡的 (role, cond) 组合（写死字面量）----
        var want = new Dictionary<string, string>
        {
            { "EC39",  "avail222|choice222|target222" },
            { "EC58",  "count221|count221" },
            { "SOR42", "avail222|choice222|nontarget221|count222|target222" },
            { "SW44",  "target221|target221" },
            { "UM77",  "multi222|mult222" },
        };
        foreach (var kv in want)
        {
            var card = CardDatabase.FindById(pool, kv.Key);
            CheckTrue(card != null, "前提：" + kv.Key + " 在池内");
            if (card == null) continue;
            var got = new List<string>();
            foreach (var r in card.Conditions)
                if (r != null && (r.Condition == 221 || r.Condition == 222))
                    got.Add(r.Role + r.Condition);
            Check(string.Join("|", got.ToArray()), kv.Value,
                  "★ " + kv.Key + " 的 221/222 行 = `role+cond` 逐行（顺序照卡表）"
                + "｜🧨 生成器把某个 role 换掉 / 少落一行 ⇒ 红");
        }

        // ---- ①c 每一个**会求值**的位置：候选域必须是墓地（60），或者原版根本不求值这一格 ----
        int checkedRows = 0, cardLevelRows = 0, deadTaRows = 0, cemeteryRows = 0;
        foreach (var id in ids)
        {
            var card = CardDatabase.FindById(pool, id);
            if (card == null) continue;
            foreach (var r in card.Conditions)
            {
                if (r == null || (r.Condition != 221 && r.Condition != 222)) continue;
                string where = id + " · role=" + r.Role + " · cond=" + r.Condition
                             + "（ab=" + r.Ability + ", lg=" + r.Logic + "）";
                checkedRows++;
                switch (r.Role)
                {
                    case "count":
                    {
                        // 被数集合 = `BoardCriteria.countCriteria`；墓地那一档 ⇒ `CountEval` 走 `DeadUnits`
                        CheckTrue(r.CountSet != null, "★ " + where + "：带 `countSet`（没有它「数谁」读不出来）");
                        if (r.CountSet == null) break;
                        Check(r.CountSet.TargetsAffected, 60,
                              "★★ " + where + "：候选域 = `inCemeteryFiltered(60)`（**墓地**，不是棋盘）"
                            + "｜🧨 改成 10/55（在场上）⇒ 活卡就进得来，本件的「不可达」结论当场作废");
                        if (r.CountSet.TargetsAffected == 60) cemeteryRows++;
                        break;
                    }
                    case "multi":
                    {
                        // ⚠️ `UM77` 这一格是 `multiSet.ta = 0 (any)` —— `any` 是**原版早返那一档**
                        // （`EarlyReturnTargetsAffected`）⇒ `conditionsFilter` 是死字段；而
                        // `EffectResolver.cs:13799` 也照这个口径「`ta == 0` ⇒ 不携带信息 ⇒ 不重复」。
                        CheckTrue(r.MultiSet != null, "★ " + where + "：带 `multiSet`");
                        if (r.MultiSet == null) break;
                        int mta = r.MultiSet.TargetsAffected;
                        bool mdead = TargetCriteriaEval.ConditionIsDeadByTargetsAffected(mta);
                        CheckTrue(mta == 60 || mdead,
                                  "★★ " + where + "：`multiSet.ta=" + mta + "` ⇒ "
                                + (mta == 60 ? "墓地(60)" : "原版不求值这一档（死字段）")
                                + "｜🧨 落在 10/55（在场上）且非死字段 ⇒ 活卡可达");
                        if (mta == 60) cemeteryRows++; else if (mdead) deadTaRows++;
                        break;
                    }
                    case "mult":
                    {
                        CheckTrue(r.MultiplierSet != null, "★ " + where + "：带 `multiplierSet`");
                        if (r.MultiplierSet == null) break;
                        Check(r.MultiplierSet.TargetsAffected, 60, "★★ " + where + "：候选域 = 墓地(60)");
                        if (r.MultiplierSet.TargetsAffected == 60) cemeteryRows++;
                        break;
                    }
                    case "avail":
                    case "choice":
                    {
                        // 这两个 role **引擎侧没有规则消费点**：`avail` 原版只服务 UI
                        // （`BattleCardUI__ShowAvailableTargets.c:42`）；`choice` 的候选池我们走
                        // **正文** `choosecard/dead/since_last_turn`（见 `RuleEngineTest_UnitRoles` 的 ⑤）。
                        var set = r.Role == "avail" ? r.AvailSet : r.ChoiceSet;
                        CheckTrue(set != null, "★ " + where + "：带 `" + r.Role + "Set`");
                        if (set == null) break;
                        Check(set.TargetsAffected, 60, "★★ " + where + "：候选域 = 墓地(60)");
                        if (set.TargetsAffected == 60) cemeteryRows++;
                        break;
                    }
                    case "target":
                    {
                        // `role=target` = 原版**同一个** `TargetCriteria` 对象里的 `conditionsFilter`。
                        // 两种情形，**两种都不会把活卡喂进来**：
                        var set = card.AbilityTargetCriteriaAt(r.Ability, r.Logic);
                        CheckTrue(set != null, "★ " + where + "：`AbilityTargetCriteriaAt` 取得到那条 36 格");
                        if (set == null) break;
                        int ta = set.TargetsAffected;
                        bool deadField = TargetCriteriaEval.ConditionIsDeadByTargetsAffected(ta);
                        // 🧨 若某条 `target` 行的 ta 变成 10/55（在场上）且原版会求值 ⇒ 活卡可达 ⇒ 红
                        CheckTrue(ta == 60 || deadField,
                                  "★★ " + where + "：`ta=" + ta + "` ⇒ "
                                + (ta == 60 ? "墓地（同 ①c 的口径）"
                                            : "**原版对这一档根本不求值 `conditionsFilter`**（早返 / 不筛两族 ⇒ 死字段）")
                                + "｜🧨 ta 落在 10/55（在场上）且非死字段 ⇒ 活卡可达、必须按铁律 18·0 照抄原版");
                        if (ta == 60) cemeteryRows++; else if (deadField) deadTaRows++;
                        break;
                    }
                    case "nontarget":
                    case "targetscore":
                    {
                        // 卡级计分（`nonTargetScoringCriteria` / `cardTargetScoringCriteria`）：
                        // 唯一消费者 = `AiScoring` → `AiTargets.MeetsCondition`（见 `RuleEngineTest_UnitRoles` ⑤）
                        // ⇒ **`ab = -3`**，永远进不了 `(ab, lg)` 那两条 ability 级闸。
                        Check(r.Ability, -3, "★★ " + where + "：`ab = -3`（卡级计分 ⇒ 只喂 AI 评分，不进 ability 的 36 格链）");
                        cardLevelRows++;
                        break;
                    }
                    default:
                        CheckTrue(false, "★★ " + where + "：**没见过的 role**（本波只有上面 8 种）"
                                       + "｜🧨 新 role ⇒ 得先查它在原版哪里被求值");
                        break;
                }
            }
        }
        Check(checkedRows, 14, "★ 上面逐行核过 **14 行**（与 ①a 的计数对得上）");
        Check(cemeteryRows, 10, "★★ 其中 **10 行**的候选域是墓地(60)（`EC39` 3 · `EC58` 2 · `SOR42` 4 · `UM77` 1）");
        Check(deadTaRows, 3, "★★ **3 行**的 `ta` 是原版不求值那一档（`SW44` 两条：`lg=0` ⇒ `ta=30 target` · "
                           + "`lg=1` ⇒ `ta=0 any`；`UM77` 的 `multi` 行 ⇒ `multiSet.ta=0 any`）");
        Check(cardLevelRows, 1, "★★ **1 行**是卡级计分（`SOR42` 的 `nontarget`）");
        Check(cemeteryRows + deadTaRows + cardLevelRows, 14, "★★ 三段加起来 = 14 ⇒ 每一行都有归宿（没有漏网的）");

        // ---- ①d 两条「死字段」的判据本身（原版早返 / 不筛那两族）----
        CheckTrue(TargetCriteriaEval.ConditionIsDeadByTargetsAffected(0),
                  "★ `ta=0 (any)` ⇒ 原版不求值 `conditionsFilter`（`EarlyReturnTargetsAffected`）");
        CheckTrue(TargetCriteriaEval.ConditionIsDeadByTargetsAffected(30),
                  "★ `ta=30 (target)` ⇒ 同上（`SW44` 的 `lg=0` 那一行）");
        CheckTrue(!TargetCriteriaEval.ConditionIsDeadByTargetsAffected(60),
                  "★ `ta=60 (inCemeteryFiltered)` ⇒ **不是**死字段（所以 `EC39`/`SOR42` 那一行真的会求值 —— "
                + "只是它的候选域是墓地）");
        CheckTrue(!TargetCriteriaEval.ConditionIsDeadByTargetsAffected(10)
                  && !TargetCriteriaEval.ConditionIsDeadByTargetsAffected(55),
                  "★ `ta=10/55`（在场上）**也不是**死字段 ⇒ 「候选域必须不是棋盘」这条才是真正的防线");

        // ---- ①e ability 级那两条闸**结构上取不到** 221/222（它们只认 `lg = -1`）----
        int lgMinusOne = 0;
        foreach (var c in pool)
        {
            if (c == null || c.Conditions == null) continue;
            foreach (var r in c.Conditions)
            {
                if (r == null || (r.Condition != 221 && r.Condition != 222)) continue;
                if (r.Logic == -1 && r.Role == "target") lgMinusOne++;
            }
        }
        Check(lgMinusOne, 0,
              "★★ **全池没有一条 `lg = -1` 的 221/222 `target` 行** ⇒ `ActionPump.AbilityLevelConditionRow` / "
            + "`RuleCore.CanUseAbility`（两者都只 `TryGetCondition(ab, -1, \"target\")`）取不到它们"
            + "｜🧨 出现这种行 ⇒ 触发闸/主动技能闸会拿**活候选**去判 221/222 ⇒ 红");
        foreach (var id in ids)
        {
            var card = CardDatabase.FindById(pool, id);
            if (card == null) continue;
            CardDef.ConditionSpec spec;
            bool has = card.TryGetCondition(0, -1, "target", out spec) && spec != null;
            CheckTrue(!has, "★ " + id + " 的 `(ab=0, lg=-1, target)` **取不到**条件（同上，ability 级闸看不到 221/222）");
        }
    }

    // ==================================================================
    //  ② 三态：活卡 / 真死本回合 / 真死上回合（+ 更早）
    // ==================================================================
    static void B94W3T2ThreeStates()
    {
        var early = Unit("W3Early", 1, 1, 1);   // 第 1 回合死的（两回合前的）
        var prev  = Unit("W3Prev",  1, 1, 1);   // 第 2 回合死的（上一个回合）
        var now   = Unit("W3Now",   1, 1, 1);   // 第 3 回合死的（本回合）

        var ctx = Battle(new[] { early, prev, now }, new[] { Unit("W3Foe", 1, 0, 9) });
        CheckTrue(ctx != null, "前提：对局建得起来");
        if (ctx == null) return;
        ToP1Turn(ctx, 1);
        Check(ctx.Turn, 1, "前提：`ctx.Turn = 1`（`NewBattle` 置 0；`ToP1Turn(1)` 走一次 `BeginTurn` ⇒ +1）");
        Check(ctx.Active, 0, "前提：现在是 P1 的回合");

        // ---- 三态之一：**活卡**（原版这里 `turnDied == 0`，`turnCounter == 0` 时判真 —— 我们 false）----
        var uEarly = Place(ctx, 0, 3, early);
        CheckTrue(uEarly != null && uEarly.IsAlive, "前提：`W3Early` 在场上且活着（`IsAlive`）");
        CheckTrue(!B94W3InDead(ctx, early),
                  "🔴 前提：活卡**不在** `DeadUnits`（= 原版 `turnDied == 0` —— 它从没死过；"
                + "这与「原版把活卡判真」是同一件事的两面）");
        // 🧨 把 `case 221` 的 `if (u != null) return false;` 删掉 ⇒ 本条当场红（那时会走到 `DiedInTurn`）
        CheckTrue(ConditionEval.MeetsUnit(ctx, uEarly, 221) == false,
                  "★ 活卡 + 221 = **false**（我们：`u != null ⇒ false`）");
        CheckTrue(ConditionEval.MeetsUnit(ctx, uEarly, 222) == false, "★ 活卡 + 222 = **false**");
        CheckTrue(ConditionEval.MeetsUnit(ctx, uEarly, 221, exclude: true) == true,
                  "★ `exclude = true` 在 `Meets` 出口取反一次 ⇒ 活卡 + 221 的取反 = true");
        // 按**卡模板**问（`MeetsCard`，主语 = 不在场上的那张）：它不在「本局阵亡流水」里 ⇒ 同样 false
        CheckTrue(ConditionEval.MeetsCard(ctx, early, 0, 221) == false,
                  "★ 活卡按卡模板问 221 = false（`DeadUnits` 里没有它）");
        CheckTrue(ConditionEval.MeetsCard(ctx, early, 0, 222) == false, "★ 活卡按卡模板问 222 = false");

        // ---- 第 1 回合：真死一次 ----
        RuleCore.HurtWithSourceForTest(ctx, uEarly, 9, "自检");   // B94_FX8（DG1 δ1）：改用 `HurtWithSourceForTest` —— 裸 `ApplyDamage` 明文**不跑离场结算**（`RuleCore.cs:8446-8447`）⇒ `DeathTurn` 会被下一回合的清尸趟记成 2
        CheckTrue(!uEarly.IsAlive, "前提：`W3Early` 真的死了（9 点打在 1 血上）");
        Check(B94W3DeathTurn(ctx, early), 1, "前提：它的 `DeathTurn = 1`（= `ctx.Turn`，`RuleCore.cs:9794/10260`）");

        // ---- 推进到第 2 回合（P2 的回合），在那里再死一个 ----
        PassTurn(ctx);
        Check(ctx.Turn, 2, "前提：推进到第 2 回合");
        var uPrev = Place(ctx, 0, 3, prev);
        CheckTrue(uPrev != null && uPrev.IsAlive, "前提：`W3Prev` 摆上场");
        RuleCore.HurtWithSourceForTest(ctx, uPrev, 9, "自检");   // B94_FX8（DG1 δ1）：同上，走真死亡入口（自带 `ActionScope` + `RemoveIfDead`）
        Check(B94W3DeathTurn(ctx, prev), 2, "前提：`W3Prev` 的 `DeathTurn = 2`");

        // ---- 推进到第 3 回合（P1 的第二个回合），本回合再死一个 ----
        PassTurn(ctx);
        Check(ctx.Turn, 3, "前提：推进到第 3 回合");
        Check(ctx.Active, 0, "前提：又轮到 P1");
        var uNow = Place(ctx, 0, 3, now);
        CheckTrue(uNow != null && uNow.IsAlive, "前提：`W3Now` 摆上场");
        RuleCore.HurtWithSourceForTest(ctx, uNow, 9, "自检");   // B94_FX8（DG1 δ1）：同上 —— 裸 `ApplyDamage` 下第三笔死亡**一次清尸都没扫到**（`DeathTurn` 记成 -1）
        Check(B94W3DeathTurn(ctx, now), 3, "前提：`W3Now` 的 `DeathTurn = 3`");

        // 三条前提再复核一次（⛔ 别只信上面那三次 Check 之后局面没被回合推进改掉）
        Check(ctx.DeadUnits.Count, 3, "前提：`DeadUnits` 里恰好 3 条（三张都在，没有被回合推进清掉）");
        Check(B94W3DeathTurn(ctx, early), 1, "前提（复核）：`W3Early.DeathTurn` 仍是 1");
        Check(B94W3DeathTurn(ctx, prev), 2, "前提（复核）：`W3Prev.DeathTurn` 仍是 2");
        Check(B94W3DeathTurn(ctx, now), 3, "前提（复核）：`W3Now.DeathTurn` 仍是 3");

        // ---- 三态之二 / 之三：真死本回合 vs 上一个回合 ----
        // 本回合死的（`turnDied == turnCounter`）
        CheckTrue(ConditionEval.MeetsCard(ctx, now, 0, 221) == true,
                  "★★ 真死**本回合** ⇒ 221 = **true**（`turnDied == turnCounter`）");
        CheckTrue(ConditionEval.MeetsCard(ctx, now, 0, 222) == true,
                  "★★ 真死**本回合** ⇒ 222 = **true**（析取项①）");
        // 上一个回合死的（`turnDied + 1 == turnCounter`）
        CheckTrue(ConditionEval.MeetsCard(ctx, prev, 0, 221) == false,
                  "★★ 真死**上一个回合** ⇒ 221 = **false**（等号不含 ±1）");
        CheckTrue(ConditionEval.MeetsCard(ctx, prev, 0, 222) == true,
                  "★★ 真死**上一个回合** ⇒ 222 = **true**（`turnDied + 1 == turnCounter`，"
                + "`+1` 加在 `turnDied` 上 —— `lea ecx,[rdi+1]`）");
        // 更早死的：两条都假（灭自证：别把 222 做成「死过就真」）
        CheckTrue(ConditionEval.MeetsCard(ctx, early, 0, 221) == false, "★★ 两回合前死的 ⇒ 221 = false");
        CheckTrue(ConditionEval.MeetsCard(ctx, early, 0, 222) == false,
                  "★★ 两回合前死的 ⇒ 222 = **false**｜🧨 把 222 做成「死过就真」⇒ 本条红");
        // 阵营那一维：同一张卡按**对面**问 ⇒ `DeadUnits` 按 `Owner` 筛掉 ⇒ 假
        CheckTrue(ConditionEval.MeetsCard(ctx, now, 1, 221) == false,
                  "★ 按**对面**阵营问同一张卡 ⇒ false（`DiedInTurn` 的 `Owner` 闸）");
    }

    // ---------------- 本件私有小工具（与宿主里的同名助手不冲突）----------------

    /// <summary>这张卡（模板）在「本局阵亡流水」里有没有一条（不区分回合）。</summary>
    static bool B94W3InDead(BattleContext ctx, CardDef card)
    {
        if (ctx == null || card == null) return false;
        for (int i = 0; i < ctx.DeadUnits.Count; i++)
        {
            var d = ctx.DeadUnits[i];
            if (d != null && ReferenceEquals(d.Card, card)) return true;
        }
        return false;
    }

    /// <summary>这张卡（模板）的死亡回合号；找不到 ⇒ `-1`。</summary>
    static int B94W3DeathTurn(BattleContext ctx, CardDef card)
    {
        if (ctx == null || card == null) return -1;
        for (int i = 0; i < ctx.DeadUnits.Count; i++)
        {
            var d = ctx.DeadUnits[i];
            if (d != null && ReferenceEquals(d.Card, card)) return d.DeathTurn;
        }
        return -1;
    }
}
