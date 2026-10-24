// RuleEngineTest_E8.cs — 执行代理 `E8`：`ta=150` 空表回归的断言（2026-10-24）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_EngineA1` / `_G6` / `_Order` / `_BoardModel` … 都在），而**同一时刻只有一个写手改一个文件**
// ⇒ 本批断言全写在这里，`RuleEngineTest.cs` **只加 `Run()` 里一行
// `Step(TestE8PreviousAbilityDrawnCards)`**（由主对话落 —— 本文件**不碰**那个文件）。
//
// 钉的是什么（回归 = 本批 `G6` 落下的 5 条 `mult` 行把正文算对的数顶成 0）：
//   `targetsAffected = 150 (previousAbilityFiltered)` 的**上一跳是抽牌**时，候选 = **抽到的那几张牌**。
//   判据（`d:/2/tools/decomp_full/`，逐句现读）：
//     · `AbilityLogic__AddDrawCard.c`：抽牌那一支把 `BattleManager__GetNextCardsInDeck(...)`
//       的结果 `AddRange` 进 `abilityTargetsToSave`（`param_1 + 0x80`）；
//     · `AbilityLogic__SetPreviousAbilityTargets.c:19-28`（`Array.Clear` + `AddRange`，整表覆盖）；
//     · `AbilityLogic__GetTargets.c` 的 `iVar6 == 0x96 (150)` 支把静态表 AddRange 成候选池。
//   ⇒ 抽到的是**手牌里的牌**，而本版 `PreviousAbilityTargets` 是 `List<UnitState>`（只装场上单位）
//     ⇒ 修法 = `CountEval.CountSet` ④ 在「本跳抽过牌」时改读 `ctx.DrawnThisResolve`。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量（`2` / `1` / `0` / `3` …）—— ⛔ 不从实现常量反算（那会自证）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestE8PreviousAbilityDrawnCards)`）。
    /// 两个子用例是 `E8…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestE8PreviousAbilityDrawnCards()
    {
        Section("🆕 执行代理 E8：`ta=150 previousAbilityFiltered` 的上一跳是抽牌 ⇒ 候选 = 抽到的牌"
              + "（修 `G6` 5 条 `mult` 行把正文的 2 顶成 0 的回归）");
        E8DrawnCountOnRealMultRows();
        E8RealCardEndToEnd();
    }

    // ==============================================================================================
    //  ① 结构化乘数本身：**真卡**的 `mult` 行 + 手摆的「本跳抽到 2 张部队」
    // ==============================================================================================

    /// <summary>
    /// 拿 `BL51 Hosts of Chaos` / `DA49 Hunt the Fallen` 的**真 `mult` 行**（`(ab 0, lg 1)`、
    /// `targetsAffected = 150`、`friendlyUnits 1` / `includeMinions 1`），把「本跳抽到 2 张部队」
    /// 摆进 `DrawnThisResolve` ⇒ **数出 2**（改前是 0）。
    ///
    /// <para>三条反向（逐条灭一种自证）：
    /// ① 抽 **1 部队 + 1 战术** ⇒ **1** —— 灭「恒返回 2」与「卡种白名单不生效」；
    /// ② **真空表**（没抽牌、也没上一跳目标）⇒ **0**（⛔ 不是 `null`）—— 灭「空表一律判不了」；
    /// ③ 上一跳是**场上单位**时走老路（3 个友方部队 ⇒ 3），且「场上 3 + 抽牌 2」⇒ **2**
    ///    （抽牌那一跳优先 —— 见 `CountEval.CountSet` ④ 那段「必须先判抽牌」的理由）。</para>
    /// </summary>
    static void E8DrawnCountOnRealMultRows()
    {
        var pool = CardDatabase.Load();
        foreach (var id in new[] { "BL51", "DA49" })
        {
            var card = CardDatabase.FindById(pool, id);
            CheckTrue(card != null, "(前提) `" + id + "` 在池内");
            if (card == null) continue;

            CardDef.ConditionSpec spec;
            bool hasRow = card.TryGetCondition(0, 1, "mult", out spec)
                          && spec != null && spec.MultiplierSet != null;
            CheckTrue(hasRow, "(前提) `" + id + "` 有 `(ab 0, lg 1)` 的 `mult` 行 + `multiplierSet`"
                  + "｜🧨 生成器不再落这一行（`G6` 之前 `cond == 0` 被丢）⇒ 乘数退回正文口径、"
                  + "本条回归**不可观测**（改前它正是被丢掉的）");
            if (!hasRow) continue;

            Check(spec.MultiplierSet.TargetsAffected, 150,
                  "(前提) `" + id + "` 的 `mult` 行指向 `previousAbilityFiltered(150)`"
                + "｜🧨 数据换成别的集合族 ⇒ 下面断的就不是这条回归");

            var ctx = Battle(new CardDef[0], new CardDef[0]);
            // 两张**部队**（`Unit(...)` = `type == "unit"`）: 手牌 + 本跳抽牌账 各一份
            var t1 = ctx.NewInstance(Unit("E8DrawnTroopA", 1, 1, 1));
            var t2 = ctx.NewInstance(Unit("E8DrawnTroopB", 1, 1, 1));
            ctx.Players[0].Hand.Add(t1);
            ctx.Players[0].Hand.Add(t2);
            ctx.DrawnThisResolve.Add(t1);
            ctx.DrawnThisResolve.Add(t2);

            string gap;
            Check(CountEval.CountTargets(ctx, 0, spec.MultiplierSet, spec.Condition, null, out gap), 2,
                  "★★ `" + id + "`：**抽到 2 张部队** ⇒ `previousAbilityFiltered` 数出 **2**（⛔ 不是 0）"
                + "｜🧨 删掉 `CountSet` ④ 的 `DrawnThisResolve` 分支（照改前只看空表）⇒ 实得 0"
                + "（正文算对的 2 被「结构化乘数优先」顶掉 = 本件要修的那条回归）；"
                + "🧨 只 `return CountDrawnAsPreviousTargets(...)` 而**不写回 `out count`** ⇒ 实得 0"
                + "（本次真撞过这个坑）；🧨 恒返回 2 ⇒ 下面两条反向红");

            // ---- 灭自证 ①：1 部队 + 1 战术 ⇒ 1（`includeMinions 1` / `includeSpells 0`）----
            var spell = ctx.NewInstance(Tactic("E8DrawnSpell", 1, "Draw 1 card"));
            ctx.Players[0].Hand.Add(spell);
            ctx.DrawnThisResolve.Clear();
            ctx.DrawnThisResolve.Add(t1);
            ctx.DrawnThisResolve.Add(spell);
            Check(CountEval.CountTargets(ctx, 0, spec.MultiplierSet, spec.Condition, null, out gap), 1,
                  "★ `" + id + "` 反向：**抽 1 部队 + 1 战术** ⇒ **1**（卡种白名单照 `FilterMethods__"
                + "CheckIfMeetsCriteria.c:286-300` 生效）"
                + "｜🧨 卡种白名单不生效（抽到的都算）⇒ 实得 2；🧨 恒返回 2 ⇒ 红");

            // ---- 灭自证 ②：真空表 ⇒ 0（⛔ 不是 null / 判不了）----
            ctx.DrawnThisResolve.Clear();
            ctx.PreviousAbilityTargets.Clear();
            var none = CountEval.CountTargets(ctx, 0, spec.MultiplierSet, spec.Condition, null, out gap);
            CheckTrue(none == 0,
                  "★ `" + id + "` 反向：**真空表**（没抽牌、也没上一跳目标）⇒ **0**（⛔ 不是 `null`）"
                + "｜🧨 空表时改判 `null`（「查不到就说查不到」的直觉）⇒ 红 —— 原版这一跳**没有候选就是 0**，"
                + "判 `null` 会让乘数退回正文口径、与结构化数据打架");

            // ---- 灭自证 ③：上一跳是**场上单位**时走老路；且抽牌那一跳优先 ----
            var f0 = Place(ctx, 0, 1, Unit("E8PrevF0", 1, 1, 3));
            var f1 = Place(ctx, 0, 2, Unit("E8PrevF1", 1, 1, 3));
            var f2 = Place(ctx, 0, 3, Unit("E8PrevF2", 1, 1, 3));
            ctx.PreviousAbilityTargets.Clear();
            ctx.PreviousAbilityTargets.Add(f0);
            ctx.PreviousAbilityTargets.Add(f1);
            ctx.PreviousAbilityTargets.Add(f2);
            Check(CountEval.CountTargets(ctx, 0, spec.MultiplierSet, spec.Condition, null, out gap), 3,
                  "★ `" + id + "` 对照：上一跳是**场上** 3 个友方部队 ⇒ **3**（老路一字不动）"
                + "｜🧨 抽牌分支把场上这条路整个顶掉 ⇒ 实得 0 ⇒ 红");

            ctx.DrawnThisResolve.Add(t1);
            ctx.DrawnThisResolve.Add(t2);
            Check(CountEval.CountTargets(ctx, 0, spec.MultiplierSet, spec.Condition, null, out gap), 2,
                  "★★ `" + id + "`：**本跳抽过牌**时以抽到的牌为准（2），**不是**上一张卡留下的场上目标（3）"
                + " —— `PreviousAbilityTargets` 只在 `ResolveOps` **出口**同步，结算中读到的是上一张卡的残值"
                + "｜🧨 改回「先看场上表、空了才看抽牌」⇒ 实战里先打一张指定目标的卡再打这 5 张之一，"
                + "会把上一张卡的目标当成候选（静默多数）⇒ 实得 3");

            ctx.DrawnThisResolve.Clear();
            ctx.PreviousAbilityTargets.Clear();
        }
    }

    // ==============================================================================================
    //  ② 真卡端到端：抽到的部队数 = 效果实际发生次数
    // ==============================================================================================

    /// <summary>
    /// 三张真卡、三种可观测结果（都**不靠日志**）：
    /// · `BL51 Hosts of Chaos`：抽 2 张部队 ⇒ 手里多 **2** 张 `Abaddon's Chosen`（`BL37`）；
    /// · `DA49 Hunt the Fallen`：抽 2 张部队 ⇒ **任务点 +2**；
    /// · `SOR49 Shrineworld`：抽 **3** 张部队 ⇒ **信仰 +3**（同时钉「不是恒 2」）。
    ///
    /// <para>夹具的牌库垫牌全是 `type == "unit"`（`Deck` / `DeckOf` 的 `filler`）⇒ 抽到的每一张都是部队。
    /// `BL51` 的「造一张具名卡」需要卡池 ⇒ 走 `BattlePool` + 真池。</para>
    /// </summary>
    static void E8RealCardEndToEnd()
    {
        var pool = CardDatabase.Load();

        // ---- BL51：`Draw 2 cards. For each troop drawn, create a copy of Abaddon's Chosen in your hand` ----
        {
            var bl51 = CardDatabase.FindById(pool, "BL51");
            CheckTrue(bl51 != null, "(前提) `BL51 Hosts of Chaos` 在池内");
            if (bl51 != null)
            {
                var ctx = BattlePool(new[] { bl51, Unit("E8Pad1", 1, 0, 1), Unit("E8Pad2", 1, 0, 1) },
                                     new[] { Unit("E8Foe", 1, 1, 5) }, pool, warlordFaction: "BlackLegion");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 30;                 // 能量不是本件要测的东西
                int hi = HandIdx(ctx, 0, "Hosts of Chaos");
                CheckTrue(hi >= 0, "(前提) `BL51` 在起手（抽牌不动它：它不是临时卡）");
                if (hi >= 0)
                {
                    CheckCode(RuleCore.PlayCard(ctx, 0, hi, -1), RuleCodes.OK, "(前提) 打出真卡 `BL51`");
                    int copies = 0;
                    foreach (var h in ctx.Players[0].Hand)
                        if (h != null && h.Card != null && h.Card.Name == "Abaddon's Chosen") copies++;
                    Check(copies, 2,
                          "★★ `BL51` 端到端：**抽到 2 张部队** ⇒ 手里多 **2** 张 `Abaddon's Chosen`"
                        + "（卡面 `for each troop drawn`）"
                        + "｜🧨 修前：结构化乘数读到空表 = 0 ⇒ 一张都不造（本件实测复现过：实得 0）");
                }
            }
        }

        // ---- DA49：`Draw two cards. For each troop drawn, gain 1 Quest Point` ----
        {
            var da49 = CardDatabase.FindById(pool, "DA49");
            CheckTrue(da49 != null, "(前提) `DA49 Hunt the Fallen` 在池内");
            if (da49 != null)
            {
                var ctx = Battle(new[] { da49 }, new[] { Unit("E8Foe", 1, 1, 5) });
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 30;
                Check(ctx.Players[0].QuestPoints, 0, "(前提) 开局任务点 = 0");
                int hi = HandIdx(ctx, 0, "Hunt the Fallen");
                CheckTrue(hi >= 0, "(前提) `DA49` 在起手");
                if (hi >= 0)
                {
                    CheckCode(RuleCore.PlayCard(ctx, 0, hi, -1), RuleCodes.OK, "(前提) 打出真卡 `DA49`");
                    Check(ctx.Players[0].QuestPoints, 2,
                          "★★ `DA49` 端到端：**抽到 2 张部队** ⇒ 任务点 **+2**（`for each troop drawn`）"
                        + "｜🧨 修前按结构化数据 0 遍 ⇒ 实得 0");
                }
            }
        }

        // ---- SOR49：`Draw 3 cards. For each Troop drawn, gain 1 ☀` ----
        {
            var sor49 = CardDatabase.FindById(pool, "SOR49");
            CheckTrue(sor49 != null, "(前提) `SOR49 Shrineworld` 在池内");
            if (sor49 != null)
            {
                var ctx = Battle(new[] { sor49 }, new[] { Unit("E8Foe", 1, 1, 5) });
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 30;
                int faithBefore = ctx.Players[0].Faith;
                int hi = HandIdx(ctx, 0, "Shrineworld");
                CheckTrue(hi >= 0, "(前提) `SOR49` 在起手");
                if (hi >= 0)
                {
                    CheckCode(RuleCore.PlayCard(ctx, 0, hi, -1), RuleCodes.OK, "(前提) 打出真卡 `SOR49`");
                    Check(ctx.Players[0].Faith - faithBefore, 3,
                          "★★ `SOR49` 端到端：**抽到 3 张部队** ⇒ 信仰 **+3**（这条同时钉「不是恒 2」）"
                        + "｜🧨 乘数写死成 2 ⇒ 实得 +2；🧨 修前按结构化数据 0 遍 ⇒ 实得 0");
                }
            }
        }
    }
}
