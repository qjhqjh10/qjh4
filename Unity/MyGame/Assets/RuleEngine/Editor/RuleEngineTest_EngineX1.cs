// RuleEngineTest_EngineX1.cs — 引擎线 `X3`（2026-10-25 · 执行代理 `X3`）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineE5` /
// `_EngineE7` / `_EngineE9` … 都在），同一时刻只有一个写手改一个文件 ⇒ 本批测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestEngineX1)`**
// （⚠️ 另需重生成 `RuleEngineTest_Diag.cs`：`VerifyRunCaseCoverage` ② 拿「反射到的用例方法」↔
//   「生成物里的 `RunCaseNames`」对账 ⇒ 不重生成会红）。
//
// 本批 = 判据文档 `资料/待办判据_引擎_1025b.md` 里那几件「判据已定、只差照做」的：
//   ① `FollowUpSchedulerOf` 漏 6 张池内卡（`EC40/EC42/GOF31/GOF93/TL3/UM64`）—— 现读 **25 张**全覆盖
//   ② `ward` 的另一半（战术卡 / 通用效果目标）—— 落点 `EffectResolver.AddSide` 的敌方侧池闸
//   ③ `TargetCriteriaEval` 的 `ta=100` 口径（**原版【有】那个尾巴分支**，见 X1T4 的注释）
//   ⑤ `rally → ThisCardPlayed` 映射
//   ⑥ 不可达 trait 名单（`TraitReachability`）
// 判据全文 → `资料/普查产出_1025/X3_引擎判据缺口_执行报告.md`，⛔ 不要重新论证。
//
// 🔴 每条断言都带 `🧨`（什么改动能让它红）。期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestEngineX1)`）。</summary>
    public static void TestEngineX1()
    {
        Section("🆕 引擎线 X1：跟随调度表覆盖 25 张 · ward 管战术/效果目标 · `ta=100` 走常规格"
              + " · `rally→ThisCardPlayed` · 不可达 trait 5 条");
        X1T1FollowUpSchedulerCoverage();
        X1T2TraitReachabilityTable();
        X1T3WardOnEffectTargets();
        X1T4Ta100RunsRegularFilters();
        X1T5RallyMapsToThisCardPlayed();
    }

    // ==================================================================
    //  ① 跟随调度表：池内 25 张全覆盖（改前 19）
    // ==================================================================

    /// <summary>
    /// **判据** = `cards_raw.json` 现读：池内（1127 张）「`chosenAbility ∈ {320, 325}`」= **25 张**
    /// （`资料/普查产出_1025/E9_ActionPump三偏差.md` §四）。
    /// 改前硬编码表只有 **19 条**，漏的正好是下面这 6 张。
    /// </summary>
    static void X1T1FollowUpSchedulerCoverage()
    {
        // ---- ① 漏掉的那 6 张，逐张钉「应 return 哪个 trigger 名」 ----
        // (id, 期望值, 理由)。期望值写死；⛔ 不从 `FollowUpSchedulerOf` 反算。
        var must = new[]
        {
            Tuple.Create("EC40", "ThisCardPlayed"),   // ab0 trigger 0
            Tuple.Create("EC42", "ThisCardPlayed"),   // ab0 trigger 0
            Tuple.Create("GOF31", "ThisCardPlayed"),  // ab0 trigger 0
            Tuple.Create("GOF93", "ThisCardPlayed"),  // ab0 trigger 0
            Tuple.Create("TL3", "Strike"),            // ab0 trigger 580
            Tuple.Create("UM64", "Codex"),            // ab1 trigger 145
        };

        var pool = CardDatabase.Load();
        var byId = new Dictionary<string, CardDef>(StringComparer.Ordinal);
        foreach (var c in pool) if (c != null && c.Id != null) byId[c.Id] = c;

        foreach (var t in must)
        {
            CardDef c;
            if (!byId.TryGetValue(t.Item1, out c))
            {
                CheckTrue(false, "（前提）池里有 `" + t.Item1 + "`｜🧨 卡池换了 id ⇒ 本行红（表要重生成）");
                continue;
            }
            Check(RuleCore.FollowUpSchedulerOf(c), t.Item2,
                  "★★ `" + t.Item1 + "` 的 followAfterResolution 调度 = `" + t.Item2 + "`"
                + "｜🧨 把这 6 个 case 从 `FollowUpSchedulerOf` 删掉 ⇒ 回 `null` ⇒ 红"
                + "（= 2026-10-25 之前的状态）");
        }

        // ---- ② 全池覆盖数 = 25（改前 19）----
        int covered = 0;
        var coveredIds = new List<string>();
        foreach (var c in pool)
        {
            if (c == null) continue;
            if (RuleCore.FollowUpSchedulerOf(c) != null) { covered++; coveredIds.Add(c.Id); }
        }
        Check(covered, 25,
              "★★ 池内「`chosenAbility ∈ {320,325}`」**25 张全覆盖**（现读 " + covered + " 张）"
            + "｜🧨 少一条 case ⇒ 24 或更少 ⇒ 红；🧨 多收一张（例如把 `SW9` 那 2 行也收进来 —— "
            + "原版本身不触发）⇒ 26 ⇒ 也红");
        Check(coveredIds.Count, covered, "（自洽）覆盖数 = 命中 id 数");
    }

    // ==================================================================
    //  ⑥ 不可达 trait 名单（`TraitReachability`）
    // ==================================================================

    /// <summary>
    /// **判据三支**（任一成立即**可达**）—— 判据全文 → `待办判据_引擎_1025b.md` §六。
    /// 正确名单 = `regeneration` · `concussive` · `frenzied` · `large` · `mekaniak`。
    /// 🔴 **`resolution` 不许在这张表里**（旧名单把它算进去 = 错）。
    /// </summary>
    static void X1T2TraitReachabilityTable()
    {
        var tbl = TraitReachability.UnreachableForTriggerDisplay;
        Check(tbl.Length, 5,
              "★★ 不可达 trait 名单 = **5 条**（现读 " + tbl.Length + "）");

        var want = new[] { "regeneration", "concussive", "frenzied", "large", "mekaniak" };
        for (int i = 0; i < want.Length; i++)
        {
            bool found = false;
            for (int j = 0; j < tbl.Length; j++)
                if (tbl[j] != null && string.Equals(tbl[j].Trait, want[i], StringComparison.Ordinal))
                { found = true; break; }
            CheckTrue(found, "★ 名单含 `" + want[i] + "`｜🧨 从表里删掉它 ⇒ 红");
        }

        // 🔴 灭自证（正例那一侧）：这 4 条**可达**，任何一条出现在不可达表里都必须红。
        //    两半结构上不可能同时满足（`resolution` 既在表里又不在表里）。
        foreach (var name in TraitReachability.ReachableCounterExamples)
        {
            CheckTrue(!TraitReachability.IsUnreachableForTriggerDisplay(name),
                      "🧨🧨 **灭自证**：`" + name + "` **不在**不可达表里"
                    + "｜🧨 把 `resolution` 加回表里（= 2026-10-25 之前那份名单的错）⇒ 本行红");
        }

        // 🔴 判据锚点：`resolution` 真的有一条会被发射的 `AbilityTrigger`（45）。
        Check(CardDef.AbilityTriggerNumber("Resolution"), 45,
              "★ 判据锚：`AbilityTrigger.Resolution = 45`（`dump.cs:20249`）"
            + "｜🧨 改 `AbilityTrigger` 名表 ⇒ 本行红（那时 `resolution` 的结论要重新查）");
        Check(CardDef.AbilityTriggerNumber("regeneration"), -1,
              "★ 判据锚：`regeneration` **没有** `AbilityTrigger` 枚举项（(a) 支 ✗）"
            + "｜🧨 哪天原版加了 ⇒ 本行红，那时它可能变可达");

        CheckTrue(TraitReachability.IsUnreachableForTriggerDisplay("regeneration"),
                  "★ 查询口与表一致（`regeneration`）");
        CheckTrue(!TraitReachability.IsUnreachableForTriggerDisplay("stomp"),
                  "★ 查询口：`stomp` **可达**（`BattleManager__ShouldTriggerStompDamage` `0x4d8`）");
    }

    // ==================================================================
    //  ② `ward` 的另一半：战术卡 / 通用效果目标（`EffectResolver.AddSide` 的敌方侧闸）
    // ==================================================================

    /// <summary>
    /// **判据**（第一权威，逐句）：
    /// ① `BattleManager__IsValidSpellTarget.c:41-53` —— 三支同形，全是
    ///    `候选.isPlayer != 施放者.isPlayer` ⇒ 再看 stealth / **warded** / camouflaged，任一成立即 `return 0`；
    /// ② `RawCardScript__IsValidTarget.c:16-40` —— `ward` 那支**不分施放者卡种**、只要异侧就拒。
    /// ⇒ 落点 = `AddSide` 的 `stealthGate`（**只在 `isFoe` 且知座位时**生效 = 原版那个「异侧」）。
    /// ⚠️ **池内 0 张**（`cards_engine.json` v28 的 keywords/desc 现扫 0 命中）⇒ 真卡不可达，
    ///    本条只由合成夹具验（铁律 11：原版有的都要有）。
    /// </summary>
    static void X1T3WardOnEffectTargets()
    {
        // ---- ⓪ 前提：词认得出来（不登记 ⇒ `Normalize` 回 null ⇒ 卡数据会被静默吃掉）----
        Check(KeywordTable.Normalize("Ward"), ConditionEval.Ward,
              "（前提）`ward` 认得出 → `ConditionEval.Ward`｜🧨 从 `Prefixes` 删掉 ⇒ 下面的「挡」变假绿");
        Check(KeywordTable.Normalize("wardWb"), ConditionEval.WardWb,
              "（前提）`wardWb` 认得出 → `WardWb`（145，⛔ 不是被 `ward` 抢吃成 140）");

        // ---- ① 敌方带 ward ⇒ 战术卡选不中（`ErrNoTargetAvailable`）----
        {
            var tac = Tactic("X1_Ward", 1, "Deal 4 damage to an enemy troop");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) },
                             new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("Warded", 1, 1, 5, "Ward"));
            Check(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "X1_Ward"), 3),
                  RuleCodes.ErrNoTargetAvailable,
                  "★★ 敌方带 `Ward` 的单位**不能被战术卡选中**（原版 `IsValidSpellTarget.c:51-53`）"
                + "｜🧨 把 `IsCurrentlyWarded(u)` 从 `AddSide` 的闸里删掉 ⇒ 回 `OK` ⇒ 红"
                + "（= 2026-10-25 之前的状态：只筛 stealth + camouflage）");
        }
        // ---- ② 同一个位置、不带 ward ⇒ 能选中（挡的是关键词、不是这一格）----
        {
            var tac = Tactic("X1_NoWard", 1, "Deal 4 damage to an enemy troop");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) },
                             new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("Plain", 1, 1, 5));
            Check(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "X1_NoWard"), 3),
                  RuleCodes.OK,
                  "★ 对照：同一格**不带** ward ⇒ 能选中｜🧨 把闸改成「敌方一律不可选」⇒ 红");
        }
        // ---- ③ `wardWb`(145) 同样挡（`isCurrentlyWarded` = ward ∨ wardWb）----
        {
            var tac = Tactic("X1_WardWb", 1, "Deal 4 damage to an enemy troop");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) },
                             new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 3, Unit("WardedWb", 1, 1, 5, "WardWb"));
            Check(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "X1_WardWb"), 3),
                  RuleCodes.ErrNoTargetAvailable,
                  "★★ `wardWb`(145) 也挡（原版 `get_isCurrentlyWarded` = 140 ∨ 145）"
                + "｜🧨 只判 `Ward` 不判 `WardWb` ⇒ 红");
        }
        // ---- ④ 「只在异侧」：己方带 ward 的单位照样能被自己的战术选中 ----
        {
            var tac = Tactic("X1_OwnWard", 1, "Deal 1 damage to a friendly troop");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) },
                             new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 3, Unit("MyWarded", 1, 1, 5, "Ward"));
            Check(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "X1_OwnWard"), 3),
                  RuleCodes.OK,
                  "★ 异侧才挡：**己方**带 ward 的单位可以被己方战术选中"
                + "｜🧨 把 `isFoe` 那一格去掉（= 不分侧一律挡）⇒ 回 `ErrNoTargetAvailable` ⇒ 红");
        }

        // ---- ⑤ 如实出声：池内 0 张（今天真卡走不到这条闸）----
        int wardCards = 0;
        foreach (var c in CardDatabase.Load())
        {
            if (c == null || c.Keywords == null) continue;
            foreach (var k in c.Keywords)
                if (string.Equals(k.Key, ConditionEval.Ward, StringComparison.Ordinal)
                    || string.Equals(k.Key, ConditionEval.WardWb, StringComparison.Ordinal))
                { wardCards++; break; }
        }
        Check(wardCards, 0,
              "（如实）池内带 `ward`/`wardWb` 的卡 = **0 张** ⇒ 这条闸今天只由合成夹具验到"
            + "｜🧨 卡池里出现带 ward 的卡 ⇒ 本行红（那时要拿真卡复核一遍）");
    }

    // ==================================================================
    //  ③ `ta=100`：常规 36 格真的跑了（不是「一上来就判不了」）
    // ==================================================================

    /// <summary>
    /// **判据（第一权威，逐句）** —— 🔴 这里**订正**判据文档 `待办判据_引擎_1025b`「顺手发现 2」的说法：
    /// · 早返 switch（`FilterMethods__CheckIfMeetsCriteria.c:56-160`）里**没有** `100` 这一档
    ///   ⇒ 常规格该判（改前我们 `gap=未实现` 早返 ⇒ **一格都没跑**）；
    /// · 但**尾巴** `:407` 明写 `if (*(int *)(param_4 + 0x10) != 100) return 1;`，`:411-425` 再走
    ///   `GetAdjacentUnits(thisCard) + Find` ⇒ **`ta=100` 还多一条「候选必须就在 `thisCard` 的相邻里」**。
    ///   🔴 **2026-10-27（`W20AS` 复核 · `W21A2` 落地 · 铁律 5）：上面那句「那一截本版未实现」已过期。**
    ///   `W6K11`（2026-10-26）已落地 `TargetCriteriaEval.CandidateAdjacentTo`（`TargetCriteriaEval.cs:880`，
    ///   判据 = 原版 `:411-425` + `MinionManager__GetAdjacentUnits.c:18-115`）⇒ 常规格过后**照尾巴判**：
    ///   候选不在相邻里 = **`false`（淘汰）**；只有缺 ctx / 载体 / 棋盘才 `null`（判不了 + 出声）。
    ///   ⛔ 别再按「常规格过后一律回 `null`」写断言 —— 那是 2026-10-26 之前的状态。
    ///   （那份文档说「原版没有 0x64 分支」是因为反编译器把常量印成了**十进制 `100`**、`grep 0x64` 空。）
    /// </summary>
    static void X1T4Ta100RunsRegularFilters()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
        ToP1Turn(ctx, 1);
        var mine = Place(ctx, 0, 3, Unit("Anchor", 1, 2, 4));      // `thisCard`（载体）
        var ally = Place(ctx, 0, 5, Unit("Ally", 1, 2, 4));        // 己方候选（⚠️ 不能拿 `mine` 当候选：
                                                                   //   那样会先被 `includeSelf=0` 淘汰 ⇒ 量不到阵营闸）
        var foe = Place(ctx, 1, 4, Unit("Foe", 1, 2, 4));

        // ta=100 + `enemyUnits=1 / friendlyUnits=0`（其余格显式给值 ⇒ 不留 Unknown）
        var set = new CardDef.CountSetSpec
        {
            PlayerTurn = 0,
            TargetsAffected = 100,
            ChosenArmy = 0, ArmyFilter = 0,
            KeywordFilter = 0,
            TraitsFilter = 0,
            MaxTargets = 1,
            TargetSelectMethod = 0,
            AttackFilter = 0, AttackFilterOption = 0,
            CurrentHealthFilter = 0, CurrentHealthFilterOption = 0,
            ManaCostFilter = 0, ManaCostFilterOption = 0,
            RarityFilter = 0, RarityFilterOption = 0,
            SpellType = 0,
            FriendlyUnits = 0,
            EnemyUnits = 1,
            IncludeHero = 1, IncludeMinions = 1,
            IncludeSpells = 0, IncludeWhispers = 0, IncludeWeapons = 0,
            IncludeRemnants = 1, IncludeHeroParts = 0,
            IncludeSelf = 0, ExcludeNotSelf = 0, ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0,
            ExcludeRace = 0, ExcludeTrait = 0,
            SelectFromChooseResolution = 0,
        };

        // ---- ① 己方候选 ⇒ **false**（`enemyUnits=1` 那一格真的跑了）----
        string gap1;
        bool? v1 = TargetCriteriaEval.Matches(ctx, 0, set, ally, null, mine, null, out gap1);
        CheckTrue(v1 == false,
                  "★★ `ta=100` + `enemyUnits=1` 碰上**己方**候选 ⇒ **false**（常规格的阵营闸真的跑了）"
                + "｜🧨 把 `case 100` 改回「早返 `gap=未实现`」⇒ 这里回 **null** ⇒ 红"
                + "（= 2026-10-25 之前的状态）｜实得 " + (v1.HasValue ? v1.Value.ToString() : "null")
                + (string.IsNullOrEmpty(gap1) ? "" : "（gap=" + gap1 + "）"));

        // ---- ② 敌方候选 ⇒ 常规格全过，但**尾巴**那一截把它淘汰 ⇒ **false**（不是「判不了」）----
        //  🔴 **2026-10-27（`W20AS` 复核 · `W21A2` 落地 · 铁律 5）：期望由 `null` 改成 `false`。**
        //    `W6K11`（2026-10-26）把尾巴实现成了 `CandidateAdjacentTo`（`TargetCriteriaEval.cs:880`）。
        //    夹具 `mine` 在座位 0、`foe` 在座位 1 ⇒ 那一支的 `cp != dp` ⇒ **false（淘汰）**，
        //    不是「判不了」（`null` 只留给缺 ctx / 载体 / 棋盘）。
        //    ⇒ 这一条现在**比旧版更有分辨力**：它同时钉住「尾巴真的在筛」和「不是一律放过」。
        string gap2;
        bool? v2 = TargetCriteriaEval.Matches(ctx, 0, set, foe, null, mine, null, out gap2);
        CheckTrue(v2 == false,
                  "★★ `ta=100` 的敌方候选 ⇒ **false**（`CandidateAdjacentTo` 已实现："
                + "候选必须**就是** `thisCard` 的相邻，原版 `:407-425`；夹具 `mine` 在座位 0 / `foe` 在座位 1 ⇒ 不同盘）"
                + "｜🧨 退回早返 `gap=未实现` ⇒ 这里回 **null** ⇒ 红；或把尾巴写成 `return true` ⇒ 回 true ⇒ 红"
                + "｜实得 " + (v2.HasValue ? v2.Value.ToString() : "null")
                + (string.IsNullOrEmpty(gap2) ? "" : "（gap=" + gap2 + "）"));
        // 🔴 **2026-10-27（`W20AS` 复核）：这条原为「gap 含『相邻』」。已实现 ⇒ 反向钉「不许再报判不了」。**
        //   ⚠️ 它**不是**下面那条的重复：上面断「值对不对」，这条断「**出声口干净**」——
        //     万一哪天有人把尾巴改回「判不了就保留候选」，`v2` 会变 `null`（上面红）、`gap2` 会带「相邻」（这条也红）；
        //     而「静默回 `false` 但 gap 里塞一句判不了」只有这一条能抓到。
        CheckTrue(gap2 == null,
                  "★ 出声口：**已实现 ⇒ 不许再报判不了**（`gap` 必须是 null）"
                + "｜🧨 退回早返那一版 ⇒ gap 里又出现「相邻」⇒ 红｜实得 gap=" + (gap2 ?? "null"));

        // ---- ③ 对照：同样的棋盘、`ta=10` ⇒ 敌方候选 true（说明 ② 的 false 是 `ta=100` 那截尾巴判的）----
        set.TargetsAffected = 10;
        string gap3;
        bool? v3 = TargetCriteriaEval.Matches(ctx, 0, set, foe, null, mine, null, out gap3);
        CheckTrue(v3 == true,
                  "★ 对照：同一份 36 格把 `ta` 换成 10 ⇒ 敌方候选 **true**"
                + "（⇒ ② 的 `false` 是 `ta=100` 那截「相邻」判出来的，**不是**常规 36 格把它筛掉了）"
                + "｜🧨 若这里也是 null/false ⇒ ② 那条量的就不是「尾巴那一截」⇒ 红");
    }

    // ==================================================================
    //  ⑤ `rally → ThisCardPlayed`
    // ==================================================================

    /// <summary>
    /// **判据 = 数据现读**：`cards_engine.json` v28 里引擎解析后带 `rally` 关键词的卡 **105 张**
    /// （`keywords` 列里精确写 `Rally` 的 **98 张** ＋ 写成 `Rally: &lt;效果&gt;` 的 **7 张** ——
    /// `KeywordTable.Parse` 按 `':'` 切前半段，两类都归成 `rally`），
    /// 这 105 张的 `abilityTriggers` 里 **`trigger 0` 全部存在**（105/105）
    /// ⇒ 引擎「部署时触发 rally」与原版「打出这张卡 ⇒ `TriggerAbilities(0)`」是同一件事。
    /// ⚠️ 接上它今天**零行为变化**（`FireTriggerAt` 只拿它查 `abilityContext`，而
    ///    `ThisCardPlayed` 在 skip 表里）—— 它是给「`FireTriggerAt` 补 `abilityTrigger/abilityTarget`」
    ///    那一步铺的路（那一步在 `RuleCore.cs`，本轮不在白名单）。
    /// ⚠️ **2026-10-25 现核订正**：判据文档（与 `ActionPump` 那条注释的第一版）写的是 **98 张**，
    ///    那是「`keywords` 列精确等于 `Rally`」的口径；按引擎解析口径是 **105**（多出的 7 张见上）。
    /// </summary>
    static void X1T5RallyMapsToThisCardPlayed()
    {
        Check(RuleCore.TriggerNameOfKeyword(KeywordTable.Rally), "ThisCardPlayed",
              "★★ `rally` → `ThisCardPlayed`（trigger 0）"
            + "｜🧨 删掉 `TriggerNameOfKeyword` 里那一行 ⇒ 回 `null` ⇒ 红（= 2026-10-25 之前）");

        int rally = 0, bad = 0;
        foreach (var c in CardDatabase.Load())
        {
            if (c == null || c.Keywords == null) continue;
            bool hasRally = false;
            foreach (var k in c.Keywords)
                if (string.Equals(k.Key, KeywordTable.Rally, StringComparison.Ordinal)) { hasRally = true; break; }
            if (!hasRally) continue;
            rally++;
            // 有 `rally` ⇒ **必须**有 trigger 0（`abilityTriggers` 真值列：1 = 有）
            if (c.AbilityTriggerPresence(0) != 1) bad++;
        }
        Check(rally, 105,
              "（如实）引擎解析后带 `rally` 的卡 = **105 张**（现读 " + rally + "；"
            + "`keywords` 精确写 `Rally` 的 98 ＋ 写 `Rally: …` 的 7）"
            + "｜🧨 卡池变了 ⇒ 本行红（那时要重新核这条映射）");
        Check(bad, 0,
              "★★ 每一张带 `rally` 的卡都**有** `trigger 0` 这条 ability（105/105）"
            + "｜🧨 出现一张带 rally 而没有 trigger 0 的卡 ⇒ 本行红 ⇒ 「rally = ThisCardPlayed」这个前提就不成立");
    }
}
