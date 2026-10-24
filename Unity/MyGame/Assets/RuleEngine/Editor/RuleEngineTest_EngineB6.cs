// RuleEngineTest_EngineB6.cs — 引擎线 `B6` 第三批的断言（2026-10-24）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_EngineA1` / `_EngineB3` / `_EngineP1` / `_Order` / `_BoardModel` / `_EntryPump` / `_Diag` … 都在），
// 而**同一时刻只有一个写手改一个文件** ⇒ 本批测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一段 `Section(...)` + 一行 `Step(TestB6EngineThird)`**。
//
// 三件的判据（第一入口 = `资料/普查产出_1024/C1_引擎线判据总表.md`，⛔ 不要重新论证）：
//   ① `AiTargets` 的 `targetsAffected == 0 (any)` = **场上全部单位**（`C1` 顺手发现 1）；
//   ④ `targetsAffected = 140 / 150 / 240` 三档（`C1` §4）；
//   ② `requireActorInPlay` 与 `abilityContext` **两个判点**的差别（`C1` §8）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量，⛔ 不从实现常量反算（那会自证）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestB6EngineThird)`）。
    /// 三个子用例是 `B6…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestB6EngineThird()
    {
        Section("🆕 引擎线 B6 第三批：`AiTargets` 的 `any`/140/150/240 · `abilityContext` 的两个判点");
        B6AnyTargets();
        B6PreviousAndLowestHealth();
        B6AbilityContextPoints();
    }

    // ==============================================================================================
    //  ① `AiTargets` 的 `targetsAffected == 0 (any)` = 场上全部单位
    // ==============================================================================================

    /// <summary>
    /// 判据 = `AbilityLogic__GetTargets.c:884-891`：
    /// `if (targetsAffected == 0) { GetUnitsInPlayList(manager, result, 0, 1,1,1,1,1,1,1,1);
    ///  goto LAB_180818485; }` —— **场上全部单位**（八个筛位全 1 ⇒ 忽略 criteria 其余筛位），
    /// `goto` 到函数出口 ⇒ **不过筛、不 Trim**。而 `AI__ScoreFromCriteria.c:93` 正是拿
    /// `targetCriteria` 调它 ⇒ 改前（返回空表）凡 `ta=0` 的评分判据**一律算 0 个目标**。
    ///
    /// 池内真受影响的两张（`ai_scoring.json` 现读，`ta==0` 且 scoringOption 会遍历目标）：
    /// `DA50 Sacred Standard`（option 1 `Count`）· `UM86 Assault Doctrine`（option 2 `SumValueInPlay`）。
    /// `UM86` 的 `targetCriteria` 在 JSON 里**整个缺键**（`cards_truth.json` 把全默认对象省略）⇒
    /// 本用例还钉住 `c == null` 与 `ta == 0` **同一条路**。
    /// </summary>
    static void B6AnyTargets()
    {
        // ---- ① 直接调 `GetTargets`：双方棋盘全部算上（含督军），且忽略所有筛位 / maxTargets ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            Place(ctx, me, 1, Unit("B6AnyF0", 1, 1, 3));
            Place(ctx, me, 2, Unit("B6AnyF1", 1, 1, 3));
            Place(ctx, foe, 1, Unit("B6AnyE0", 1, 1, 3));
            // 两侧督军都在 Board[4] ⇒ 在场总数 = 2 + 1 + 2 = 5

            // 🔴 故意把**所有**筛位设成「最窄」+ `maxTargets = 1`：`any` 必须一律无视。
            var c = new AiTargetCriteriaDto
            {
                targetsAffected = 0,
                friendlyUnits = false, enemyUnits = false,
                includeHero = false, includeMinions = false,
                maxTargets = 1,
            };
            var got = AiTargets.GetTargets(ctx, c, Unit("B6AnySelf", 1, 1, 3), null, me, null);
            Check(got.Count, 5,
                  "★★ ① `ta=0 (any)` = **场上全部单位**（双方 5 个，含两个督军）—— "
                + "`friendlyUnits/enemyUnits/includeHero/includeMinions` 全 false、`maxTargets=1` **一律无视**"
                + "（原版八个筛位全 1，且 `:891 goto` 直接返回 ⇒ 不过筛、不 Trim）"
                + "｜🧨 退回改前的「返回空表」⇒ 实得 0 ⇒ 红；"
                + "🧨 拿 `case 10 (allFiltered)` 顶替（会走 `ApplyFilters` + `Trim`）⇒ 筛完只剩 0、"
                + "就算只去掉 Trim 也是 0（`includeMinions=false`）⇒ 红");

            int friends = 0, enemies = 0;
            foreach (var t in got) { if (t.Side == me) friends++; else enemies++; }
            Check(friends, 3, "① 其中**友方** = 3（2 个部队 + 自己的督军）"
                            + "｜🧨 漏了督军（只扫 `AddBoard` 的旧口径不吃 Board[4]）⇒ 实得 2 ⇒ 红");
            Check(enemies, 2, "① 其中**敌方** = 2（1 个部队 + 对方督军）"
                            + "｜🧨 只扫自己那一侧 ⇒ 实得 0 ⇒ 红");
        }

        // ---- ② `criteria` 缺键（`c == null`）= 「全默认对象」⇒ 同一条路（`UM86` 正是这一形状）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            Place(ctx, me, 1, Unit("B6NullF0", 1, 1, 3));
            Place(ctx, foe, 1, Unit("B6NullE0", 1, 1, 3));
            var got = AiTargets.GetTargets(ctx, null, Unit("B6NullSelf", 1, 1, 3), null, me, null);
            Check(got.Count, 4,
                  "★★ ① **`targetCriteria` 缺键（= 全默认对象 ⇒ `ta=0`）** 也走同一条路：4 个在场单位"
                + "（`UM86 Assault Doctrine` 的评分行就是这个形状 —— 我们的 JSON 里它**整个缺键**）"
                + "｜🧨 把 `c == null` 当空表 ⇒ 实得 0 ⇒ 红");
        }

        // ---- ③ 待死单位**不算**在场（原版 `GetUnitsInPlayList` 走 `CardScript.IsInPlay`）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            Place(ctx, me, 1, Unit("B6PendF0", 1, 1, 3));
            var dying = Place(ctx, me, 5, Unit("B6PendDying", 1, 1, 1));
            Place(ctx, foe, 1, Unit("B6PendE0", 1, 1, 3));
            dying.Health = 0;
            ctx.AddPendingDeath(me, 5, -1);
            CheckTrue(ctx.IsPendingDeath(dying), "① （前提）那个单位已进将死队列");
            var c = new AiTargetCriteriaDto { targetsAffected = 0 };
            var got = AiTargets.GetTargets(ctx, c, Unit("B6PendSelf", 1, 1, 3), null, me, null);
            Check(got.Count, 4,
                  "★ ① **待死（`waitingToDie`）/ 血已归零的不算在场**（2 友 + 1 敌 + 2 督军 = 4）"
                + "｜🧨 用不过 `IsInPlay` 的 `AddBoard` 顶替 ⇒ 实得 5 ⇒ 红");
        }

        // ---- ④ 端到端：真卡 `DA50 Sacred Standard` 的 AI 评分随「在场友军数」变 ----
        //   `nonTargetScoringCriteria[0]`：`scoringOption = 1 (Count)`、`multiplyByFriend = 1`、
        //   `targetCriteria = { friendlyUnits:1, includeHero:1, includeMinions:1 }`（**ta 缺键 ⇒ 0 any**）。
        //   ⇒ 原版得分 = 场上友方单位数 × 1（敌方那一栏 `multiplyByEnemy = 0` ⇒ 不贡献）。
        {
            var pool = CardDatabase.Load();
            var da50 = CardDatabase.FindById(pool, "DA50");
            CheckTrue(da50 != null, "① （前提）`DA50 Sacred Standard` 在池内");
            if (da50 != null)
            {
                var ctx = Battle(new CardDef[0], new CardDef[0]);
                int me = ctx.Active, foe = 1 - ctx.Active;

                // 空场对照组：只剩自己的督军 ⇒ 1 分
                float sEmpty = AiScoring.ScoreFromPlayingNow(ctx, da50, null, -1, -1);

                Place(ctx, me, 1, Unit("B6Da50F0", 1, 1, 3));
                Place(ctx, me, 2, Unit("B6Da50F1", 1, 1, 3));
                Place(ctx, me, 3, Unit("B6Da50F2", 1, 1, 3));
                Place(ctx, foe, 1, Unit("B6Da50E0", 1, 1, 3));
                Place(ctx, foe, 2, Unit("B6Da50E1", 1, 1, 3));
                float sFull = AiScoring.ScoreFromPlayingNow(ctx, da50, null, -1, -1);

                Check((int)Math.Round(sEmpty), 1,
                      "★ ① （对照）`DA50` 空场（只有自己的督军）⇒ 评 **1**（`Count` × `multiplyByFriend`）"
                    + "｜🧨 若把 `any` 也算成「双方全部」但漏掉督军 ⇒ 实得 0 ⇒ 红");
                Check((int)Math.Round(sFull), 4,
                      "★★ ① 端到端：`DA50` 在「3 个友军 + 督军、2 个敌军 + 督军」时评 **4**"
                    + "（= 友方在场数 × 1；敌方那一列 `multiplyByEnemy = 0` 不贡献）"
                    + "｜🧨 退回改前（`ta=0` 给空表）⇒ 实得 0（两次都 0）⇒ 红 —— "
                    + "这就是「这张卡 AI 永远觉得没价值」那条静默偏差");
            }
        }

        // ---- ⑤ 另一张：`UM86 Assault Doctrine` 的 `cardTargetScoringCriteria` 缺 `targetCriteria` ----
        {
            var pool = CardDatabase.Load();
            var um86 = CardDatabase.FindById(pool, "UM86");
            CheckTrue(um86 != null, "① （前提）`UM86 Assault Doctrine` 在池内");
            if (um86 != null)
            {
                var ctx = Battle(new CardDef[0], new CardDef[0]);
                int me = ctx.Active, foe = 1 - ctx.Active;
                Place(ctx, foe, 1, Unit("B6Um86E0", 1, 1, 3));      // 敌方给个目标，好让 targeted 闸放行
                var foeUnit = ctx.Players[foe].Board[1];
                Place(ctx, me, 1, Unit("B6Um86F0", 1, 1, 4));
                Place(ctx, me, 2, Unit("B6Um86F1", 1, 1, 6));

                // `targeted = 1` 且带目标 ⇒ `cardTargetScoringCriteria` 才会跑
                // （`ScoreFromPlayingNow` 的 `slot`/`targetSide` 指的是**被选中的那个目标**）
                float s = AiScoring.ScoreFromPlayingNow(ctx, um86, null, 1, foe);
                CheckTrue(foeUnit != null, "① （前提）敌方目标在位");
                // option 2 `SumValueInPlay`：友方 3 个在场单位（含督军）的 `GetValueInPlay` 之和
                CheckTrue(s > 0f,
                          "★★ ① `UM86` 的 `targetCriteria` 缺键 ⇒ 候选 = **全场单位** ⇒ `SumValueInPlay` > 0"
                        + "（实得 " + s.ToString("F2") + "）"
                        + "｜🧨 把缺键当空表 ⇒ 只剩 `baseValue` = 0 ⇒ 红");
            }
        }
    }

    // ==============================================================================================
    //  ④ `targetsAffected = 140 / 150 / 240`
    // ==============================================================================================

    /// <summary>
    /// · **140 `previousAbilityTargets`**（`GetTargets.c:849-866`，`iVar6 == 0x8c`）=
    ///   静态池 `pooledPossibleTargets` 整批 `AddRange`，然后 **`:865 goto` 直接返回** ⇒ **不过筛**；
    /// · **150 `previousAbilityFiltered`**（`:867-882`，`0x96`）= 同一个池子，但**不 return**
    ///   ⇒ 落到 `:1083-1105` 的通用筛选尾 ⇒ **要过筛**（两档的唯一差别）；
    /// · **240 `lowestHealth`**（`:835-844`，`0xf0`）= `GetLowestHealthUnit(manager, bVar16)` 取**一个**，
    ///   挑法判据 = `BattleManager__GetLowestHealthUnit.c:150-215`（严格小于 ⇒ 并列留先者；
    ///   初始 best = 该侧督军 ⇒ 空场回落督军）；选哪一侧 = `bVar16`（相对 `thisCard` 的 friendly/enemy，
    ///   `GetTargets.c:207-219`）。
    /// </summary>
    static void B6PreviousAndLowestHealth()
    {
        // ---- ① 140 vs 150：同一个候选池，一个不过筛、一个过筛 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            var f0 = Place(ctx, me, 1, Unit("B6PrevF0", 1, 1, 3));
            var e0 = Place(ctx, foe, 1, Unit("B6PrevE0", 1, 1, 3));
            ctx.PreviousAbilityTargets.Clear();
            ctx.PreviousAbilityTargets.Add(f0);
            ctx.PreviousAbilityTargets.Add(e0);

            var self = Unit("B6PrevSelf", 1, 1, 3);

            var c140 = new AiTargetCriteriaDto
            {
                targetsAffected = 140, friendlyUnits = true, enemyUnits = false,
                includeMinions = true, maxTargets = 1,
            };
            var r140 = AiTargets.GetTargets(ctx, c140, self, null, me, null);
            Check(r140.Count, 2,
                  "★★ ④ `ta=140` ⇒ **不过筛、不 Trim**：`enemyUnits=false` + `maxTargets=1` 都拦不住"
                + "，敌方那个照样在（原版 `:865 goto LAB_180818485` = 直接返回）"
                + "｜🧨 让 140 也走 `ApplyFilters`/`Trim` ⇒ 实得 1 ⇒ 红");

            var c150 = new AiTargetCriteriaDto
            {
                targetsAffected = 150, friendlyUnits = true, enemyUnits = false,
                includeMinions = true, maxTargets = 1,
            };
            var r150 = AiTargets.GetTargets(ctx, c150, self, null, me, null);
            Check(r150.Count, 1,
                  "★★ ④ `ta=150` ⇒ 同一个池子**要过筛**（`enemyUnits=false` ⇒ 只剩友方那 1 个）"
                + "｜🧨 140/150 共用一条「不过筛」的路 ⇒ 实得 2 ⇒ 红；"
                + "🧨 两条都过筛（140 走筛选）⇒ 上一条实得 1 ⇒ 红 —— 两条互为灭自证");

            ctx.PreviousAbilityTargets.Clear();
            Check(AiTargets.GetTargets(ctx, c140, self, null, me, null).Count, 0,
                  "④ `previousAbilityTargets` **空表** ⇒ 0（⛔ 不退化成「全场单位」）"
                + "｜🧨 空表时回退到盘面 ⇒ 实得 2 个督军 ⇒ 红");
        }

        // ---- ② 240：在**友方那一侧**挑血量最低的，一个 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active, foe = 1 - ctx.Active;
            Place(ctx, me, 1, Unit("B6LowBig", 1, 1, 5));
            // 🔴 2026-10-24（`F8`）：**早进场的那张要放【更大的】格号**，否则这条断言分辨不出遍历序。
            //   夹具的 `Place` 顺序 = 进场顺序（每摆一次取一个 `TakePlayOrder`）：
            //   `B6LowSmall`（格 7）先进场 ⇒ `PlayOrder` 更小；`B6LowTie`（格 5）后进场。
            //   改前那版（Small=格 2、Tie=格 3）**格号序与进场序完全一致** ⇒ 按格号 / 按 `PlayOrder` /
            //   按池插入序都得同一个答案 ⇒ 只证明「不是取后者」，证不了「是按进场序」。
            Place(ctx, me, 7, Unit("B6LowSmall", 1, 1, 2));    // 友方最低（并列里**进场更早**，格号却**更大**）
            Place(ctx, me, 5, Unit("B6LowTie", 1, 1, 2));      // 同血、进场更晚（格号更小）⇒ 不该被选
            Place(ctx, foe, 1, Unit("B6LowFoe", 1, 1, 1));     // 全场最低，但**是敌方**

            var c = new AiTargetCriteriaDto
            {
                targetsAffected = 240, friendlyUnits = true, enemyUnits = false,
                maxTargets = 1, includeMinions = true,
            };
            var got = AiTargets.GetTargets(ctx, c, Unit("B6LowSelf", 1, 1, 3), null, me, null);
            Check(got.Count, 1, "★★ ④ `ta=240` ⇒ **恰好一个**目标（原版 `GetLowestHealthUnit` 只返回一个）"
                              + "｜🧨 当成 `allFiltered + 排序` ⇒ 实得 3（友方 3 个）⇒ 红");
            Check(got.Count > 0 && got[0].Unit != null ? got[0].Unit.Name : "(null)", "B6LowSmall",
                  "★★ ④ 240 在**选中那一侧**里挑 `currentHealth` 最低的；**并列取进场更早**的那个"
                + "（🔑 这条判别的是**进场序**，⛔ 不是格号序 —— 早进场的 `B6LowSmall` 摆在**更大的**格 7，"
                + "晚进场的 `B6LowTie` 摆在格 5）"
                + "｜🧨 全场找最低（不分侧）⇒ 实得 `B6LowFoe`（1 血）⇒ 红；"
                + "🧨 并列取后者 ⇒ 实得 `B6LowTie` ⇒ 红；"
                + "🧨 遍历序改回**格号序**（5 < 7 ⇒ 先见到 `B6LowTie`）⇒ 实得 `B6LowTie` ⇒ 红");

            // ---- 另一侧：`enemyUnits=true` ⇒ 选敌方（这一条同时钉住 `bVar16` 的映射不对称）----
            var ce = new AiTargetCriteriaDto
            {
                targetsAffected = 240, friendlyUnits = false, enemyUnits = true,
                maxTargets = 1, includeMinions = true,
            };
            var gotE = AiTargets.GetTargets(ctx, ce, Unit("B6LowSelf", 1, 1, 3), null, me, null);
            Check(gotE.Count > 0 && gotE[0].Unit != null ? gotE[0].Unit.Name : "(null)", "B6LowFoe",
                  "★★ ④ 240 的**选侧**：`enemyUnits=true` ⇒ 敌方那一侧（`bVar16` 相对 `thisCard` 解析，"
                + "`GetTargets.c:207-219`）"
                + "｜🧨 一律选友方 ⇒ 实得 `B6LowSmall` ⇒ 红（与上一条互为灭自证）");
        }

        // ---- ③ 240 空场回落督军 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            int me = ctx.Active;
            var c = new AiTargetCriteriaDto { targetsAffected = 240, friendlyUnits = true, includeMinions = true };
            var got = AiTargets.GetTargets(ctx, c, Unit("B6LowEmptySelf", 1, 1, 3), null, me, null);
            Check(got.Count, 1, "★ ④ 240 在**空场**时 ⇒ 仍然给 1 个（回落到该侧督军，"
                              + "`BattleManager__GetLowestHealthUnit.c` 的初始 best 就是督军）"
                              + "｜🧨 空池返回空表 ⇒ 实得 0 ⇒ 红");
            Check(got.Count > 0 && got[0].Unit != null && got[0].Unit.IsWarlord, true,
                  "★ ④ 回落的那一个**确实是督军**｜🧨 回落到别的单位 ⇒ 红");
        }
    }

    // ==============================================================================================
    //  ② `requireActorInPlay`（出队旧口径）vs `abilityContext`（入队 + 只取消「变残骸」）
    // ==============================================================================================

    /// <summary>
    /// `C1` §8 说的「五处只传 `requireActorInPlay`、不传 `abilityContext`」——
    /// **A1 已经把三处补上实参**（`RuleCore` 的 `UnitAttack`/`attacked`、`Unstable`、`Slay`），
    /// 另两处是 **`ThisUnitDeath` 的 skip 表**（原版整段不判 context）与 **拿不到载体卡的死讯结转**
    /// ⇒ 都不需要补（`B6_引擎第三批.md` §二 有逐处出处）。
    ///
    /// 本用例不重复 A1 的「缺行 ⇒ Play」那条，而是钉**为什么必须补**——两个判点行为不同：
    /// · 传了 `abilityContext` ⇒ 闸在**入队那一刻**（原版 `CanTriggerAbility`），入了队之后
    ///   载体死亡**不取消**（原版 `CancelEffectsFromDisappearingUnit` 的四个调用点里没有死亡）；
    /// · 只传 `requireActorInPlay` ⇒ 闸在**出队**（`UnitInPlay`，改前的近似）⇒ 死了就取消。
    /// 另加一条：`Play(0)` 的**第三闸 `!isRemnant`** 只有 `IsRightContext` 有，`UnitInPlay` 没有。
    /// </summary>
    static void B6AbilityContextPoints()
    {
        // ---- ① 传了 `abilityContext: Play` ⇒ 入队后载体死亡**不取消** ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = Place(ctx, 0, 3, Unit("B6CtxActor", 1, 1, 3));
            bool ran = false;
            bool ok = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "b6-ctx",
                        () => { ran = true; },
                        actor: u, requireActorInPlay: true,
                        abilityContext: CardDef.AbilityContextPlay);
            CheckTrue(ok, "② （前提）入队那一刻载体还活着在场 ⇒ 入队成功（原版闸就在这一刻）");
            u.Health = 0;                        // 入队之后才死（不写 Board ⇒ 不需要 aura 钩子）
            RuleCore.PumpActionQueues(ctx);
            CheckTrue(ran,
                      "★★ ② 传了 `abilityContext` ⇒ **出队时不按死亡取消**（原版只在入队前判，"
                    + "`RawCardScript__TriggerAbilities.c:108,207`）"
                    + "｜🧨 让泵点忽略 `abilityContext`、一律走 `requireActorInPlay && !UnitInPlay` ⇒ "
                    + "载体已死 ⇒ 取消 ⇒ 红");
        }

        // ---- ② 只传 `requireActorInPlay`（旧口径）⇒ 同一场景**会被取消**（两个判点确实不同）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = Place(ctx, 0, 3, Unit("B6CtxLegacy", 1, 1, 3));
            bool ran = false;
            bool ok = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "b6-legacy",
                        () => { ran = true; },
                        actor: u, requireActorInPlay: true);          // ⛔ 不传 abilityContext
            CheckTrue(ok, "② （前提）旧口径入队成功（它的闸在出队，入队时不判）");
            u.Health = 0;
            RuleCore.PumpActionQueues(ctx);
            CheckTrue(!ran,
                      "★★ ② **旧口径（只传 `requireActorInPlay`）在同一场景下会取消** —— "
                    + "它比原版严（原版「入队后被后面动作打死」照跑）"
                    + "｜🧨 泵点改成「一律照跑」（把这一支删掉）⇒ 实得 true ⇒ 红 —— "
                    + "与上一条**互为灭自证**：两条同时绿才说明「两个判点真的并存且分工正确」");
        }

        // ---- ③ `Play(0)` 的第三闸 `!isRemnant`：只有 `IsRightContext` 有，`UnitInPlay` 没有 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = Place(ctx, 0, 3, Unit("B6CtxRemnant", 1, 1, 3));
            u.IsRemnant = true;                  // 残骸：占着格子、还活着（`Health` 不变）

            bool okOld = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "b6-rem-old",
                            () => { }, actor: u, requireActorInPlay: true);
            CheckTrue(okOld,
                      "② （对照）旧口径只看 `UnitInPlay` ⇒ **残骸放行**（这正是它比原版宽的地方）");

            bool okNew = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "b6-rem-new",
                            () => { }, actor: u, requireActorInPlay: true,
                            abilityContext: CardDef.AbilityContextPlay);
            CheckTrue(!okNew,
                      "★★ ② 传 `abilityContext: Play` ⇒ 残骸**被拦在入队口**"
                    + "（`CardAbility__IsRightContext.c:20-30` 第三闸 `+0x65 == 0`；`C1` §8 举的正是这一格）"
                    + "｜🧨 把 `ActionPump.IsRightContext` 的 `!cell.IsRemnant` 删掉 ⇒ 实得 true ⇒ 红");
        }
    }
}
