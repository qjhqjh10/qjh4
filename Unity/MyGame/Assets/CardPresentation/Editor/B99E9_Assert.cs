// B99E9_Assert.cs — 第九十九会话 · 执行代理 `B99_E9`
//
// ============================ 它盯的是什么 ============================
// 两条**接线缺口**（判据全文 → `资料/完全复刻/判据/B99_R4b_引擎与工具.md` §一 `#19` / §二 `#21`）：
//
//   · **#19 降费监听不过闸**：`EffectResolver.BroadcastCostWhen` 原来**完全不看** ability 级 criteria
//     （直接 `EmitListenerTrigger` + 登记 `CostMod`），而且调点只传了 `CardDef`、把
//     `subject`（= 死掉的单位）/ `actingCard`（= 凶手）丢了。本批：调点补传 + 手牌循环里、
//     `EmitListenerTrigger` **之前**插 `AbilityLevelCriteriaAllows`（`false ⇒ continue`，连
//     `EmitListenerTrigger` 都不发 —— 原版那条 ability 没触发就连动画都不播）。
//   · **#21 Rally / 战术正文的 ability 级 criteria 未求值**：`FireTriggerAt` 的
//     `EnqueueAbility` 原来**不传** `abilityTrigger`/`abilityTarget` ⇒ 路径 A 一条都没跑；
//     `PlayTacticBodyDeferred` 那条只走「族闸」，而族闸只在「定位不唯一」时启用。
//     本批：① `FireTriggerAt` 补 `abilityTrigger: _kwTriggerName` + `abilityTarget: chosen`
//     + `abilityHostCard: _carrierCard`；② `ApplyAbilityFamilyGate` 的窄闸③ 改成
//     「唯一定位 ⇒ `AbilityLevelCriteriaAllows`；不唯一 ⇒ 族闸」（两条互斥）。
//
// ============================ 🔴 判据订正（铁律 5 / 纪律 4）============================
// 判据 §2·2 说「`ASH70` 的目标已带 Vanguard ⇒ 这条 ability 根本不触发（`excludeTrait=1`）」——
// **机器码不支持这句**：`ASH70` 那份 criteria 的 `targetsAffected = 30 (target)`，而
// `FilterMethods__CheckIfMeetsCriteria.c:73-91` 在 `ta == 0x1e` 时**只做一次身份比较就 return**
// （`op_Equality(被筛, targetCard)`；`CanTriggerAbility` 的实参是
// `CheckIfMeetsCriteria(thisCard, targetCard, targetCard, …)`，见 `CardAbility__CanTriggerAbility.c:130`）
// ⇒ 身份恒等 ⇒ `excludeTrait` 那一格**永不求值**。
// ⇒ 本件**不写**「ASH70 不叠 Vanguard」那条断言（它会红，而且红得对）；改用**同一形状的合成卡**
//   （`ta=10` + `friendlyUnits=0 / enemyUnits=1`）验闸，`ASH70` 只做**数据前提**断言 + 一条
//   「`ta=30` 是身份短路档」的结构断言。详细反证 → `报告/B99_E9.md` §判据订正。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加**一个块**（位置建议 = 紧接
// `B99E8_Assert.Run(); …` 那一串之后）。🔴 **同一物理行追加即可（零行数变化）**：
//       { CheckB99E9(); }
//   🔴 **故意不用 `Step(...)`、入口也不带 `Test` 前缀**（理由同 `B97_ER_Assert`）：
//     `Step(...)` 会让 `_ranCases` 记账、`VerifyRunCaseCoverage()` 拿反射清单对账，
//     而 `工具/gen_diag_runner.py` 只认「8 空格缩进的顶层语句」⇒ 两者都得跟着改。块调用两条都不碰。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」都写在文案里）============================
//   · `CostWhenAbilityCriteriaAllows` 改成恒 `return true` ⇒ 第 ① 段负例红。
//   · `BroadcastWhenCore` 的调点退回 `BroadcastCostWhen(ctx, kind, who, card)` ⇒ 第 ① 段负例红
//     （`subject` 变 `null` ⇒ 闸判不了 ⇒ 放行 ⇒ 降费照扣）。
//   · `FireTriggerAt` 的 `EnqueueAbility` 去掉 `abilityTrigger:` ⇒ 第 ② 段「负例被拒」红。
//   · `ApplyAbilityFamilyGate` 的 `uab >= 0` 支改回「直接 return」⇒ 第 ③ 段负例红。
using System.Collections.Generic;
using System.Reflection;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里一个 `{ … }` 块调；⛔ 不叫 `Run`/不带 `Test` 前缀，见文件头）。</summary>
    public static void CheckB99E9()
    {
        Section("🆕 B99_E9：#19 降费监听过闸 · #21 Rally/战术正文的 ability 级 criteria");
        B99E9CostWhenGate();
        B99E9RallyGate();
        B99E9TacticBodyUniqueGate();
        B99E9StructureWiring();
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>`ta = 10 (allFiltered)` 的 36 格 criteria（逐格显式给值 —— 缺省是 `Unknown(-1)`，
    /// `TargetCriteriaEval` 遇 `Unknown` 会回 `null`（判不了），夹具会静默变成「什么都没验到」）。
    /// ⚠️ 与 `RuleEngineTest_EngineE2.E2Criteria10` 同形；⛔ 别改成「少给几格」。</summary>
    static CardDef.CountSetSpec B99E9Criteria10(int friendlyUnits, int enemyUnits)
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = 0,
            TargetsAffected = 10,
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
            FriendlyUnits = friendlyUnits,
            EnemyUnits = enemyUnits,
            IncludeHero = 1, IncludeMinions = 1,
            IncludeSpells = 0, IncludeWhispers = 0, IncludeWeapons = 0,
            IncludeRemnants = 1, IncludeHeroParts = 0,
            IncludeSelf = 0, ExcludeNotSelf = 0, ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0,
            ExcludeRace = 0, ExcludeTrait = 0,
            SelectFromChooseResolution = 0,
        };
    }

    /// <summary>一张**战术卡**、手里监听「一张友军死亡 ⇒ 降 1 费」，并带 ability 级 criteria
    /// （`trigger = 90 OtherUnitDeath`，`ta = 10`，阵营闸由参数给）。
    /// <para>⚠️ 卡面文本刻意写 `a friendly dies`（`Clean` 会吃掉冠词 ⇒ `OwnerIs = RelFriendly`、**没有别的筛**）——
    /// 这样 `WhenEvents.Matches` 会**放行**，剩下的交给 ability 级闸：本件验的就是**那道闸**。
    /// 若改写成 `a friendly troop dies`，`Subjects` 会再加一层兵种筛，会把「闸没跑」和「Matches 没命中」混在一起。</para></summary>
    static CardDef B99E9CostCard(string id, int friendlyUnits, int enemyUnits)
    {
        var ta = new List<CardDef.AbilityTargetSpec>
        {
            new CardDef.AbilityTargetSpec { Trigger = 90, TargetsAffected = new[] { 10 } },
        };
        var rows = new List<CardDef.TargetCriteriaRowSpec>
        {
            new CardDef.TargetCriteriaRowSpec
            {
                Ability = 0, Trigger = 90, Logic = -1,
                Criteria = B99E9Criteria10(friendlyUnits, enemyUnits),
            },
        };
        return new CardDef(id, id, "tactic", "Lower cost by 1 when a friendly dies.", "common", "Test",
                           cost: 5, attack: 0, health: 0, rangedAttack: 0, keywords: null,
                           abilityTriggersVersioned: true,
                           abilityTargets: ta.ToArray(),
                           conditionsVersioned: true,
                           abilityLevelCriteria: rows.ToArray(),
                           abilityLevelCriteriaVersioned: true);
    }

    /// <summary>一张带 **Rally** 正文的合成单位，`abilityLevelCriteria` = `trigger 0 / ta 10 / 阵营闸`。
    /// 卡面文本抄 `ASH70` 原文（`Rally: Give Vanguard to a unit until your next turn`），
    /// 确保 ops 解析走的是与真卡**同一条**路。</summary>
    static CardDef B99E9RallyCard(string id, int friendlyUnits, int enemyUnits)
    {
        var ta = new List<CardDef.AbilityTargetSpec>
        {
            new CardDef.AbilityTargetSpec { Trigger = 0, TargetsAffected = new[] { 10 } },
        };
        var rows = new List<CardDef.TargetCriteriaRowSpec>
        {
            new CardDef.TargetCriteriaRowSpec
            {
                Ability = 0, Trigger = 0, Logic = -1,
                Criteria = B99E9Criteria10(friendlyUnits, enemyUnits),
            },
        };
        return new CardDef(id, id, "unit", "Rally: Give Vanguard to a unit until your next turn", "common", "Test",
                           cost: 6, attack: 2, health: 3, rangedAttack: 0, keywords: null,
                           subtype: "Infantry",
                           abilityTriggersVersioned: true,
                           abilityTargets: ta.ToArray(),
                           conditionsVersioned: true,
                           abilityLevelCriteria: rows.ToArray(),
                           abilityLevelCriteriaVersioned: true,
                           targeted: 1);
    }

    /// <summary>读某个关键词的**层数**（`UnitState.Has` 只给布尔，验「没叠」需要层数）。
    /// ⚠️ 拿不到层数口时回退 `Has ? 1 : 0`（至少能验「有没有」）。</summary>
    static int B99E9KeywordLayers(UnitState u, string keyword)
    {
        if (u == null) return -1;
        var mi = typeof(UnitState).GetMethod("GetKeywordCount", BindingFlags.Public | BindingFlags.Instance)
              ?? typeof(UnitState).GetMethod("KeywordCount", BindingFlags.Public | BindingFlags.Instance);
        if (mi != null)
        {
            var v = mi.Invoke(u, new object[] { keyword });
            if (v is int) return (int)v;
        }
        return u.Has(keyword) ? 1 : 0;
    }

    // ==================================================================
    //  ① #19：降费监听过 ability 级闸（前提 + 负例 + 正例 + 镜像 + 灭自证）
    // ==================================================================

    /// <summary>
    /// 判据 = 原版 `RawCardScript__OnMinionDeath.c:14-24`（`OnTrigger(90 OtherUnitDeath, …,
    /// targetCard = 死掉的单位)`）→ `RawCardScript__TriggerAbilities.c:207`（`CanTriggerAbility`）
    /// → `CardAbility__CanTriggerAbility.c:130-135`（`CheckIfMeetsCriteria(thisCard, targetCard, targetCard, +0x30)`）。
    /// ⇒ **降费监听也是那条 ability**：目标不满足 ⇒ 不触发（连 `EmitListenerTrigger` 都不发）。
    /// </summary>
    static void B99E9CostWhenGate()
    {
        // ---- 前提：文本真的解析出「手里监听降费」的 CostWhen，且它落了 ability 级 criteria 行 ----
        var denyFriend = B99E9CostCard("B99E9CostDenyFriend", friendlyUnits: 0, enemyUnits: 1);
        CheckTrue(denyFriend.CostWhens.Count > 0,
                  "① （前提）合成卡的 `Lower cost by 1 when a friendly dies` **解析出了 `CostWhens`**"
                + "｜🧨 改卡面文本词序 ⇒ 解析不出来 ⇒ 本件其余断言会**全部假绿**（本行就是那个前提闸）");
        Check(denyFriend.AbilityLevelCriteriaRowCount, 1,
              "① （前提）它落了 1 行 ability 级 criteria（`trigger = 90`）");
        CheckTrue(denyFriend.CostWhens.Count > 0 && denyFriend.CostWhens[0].Ev != null
                  && denyFriend.CostWhens[0].Ev.Kind == WhenEventKind.Die,
                  "① （前提）那条 `CostWhen` 的事件是 **Die**（`OtherUnitDeath` 那一族）");
        if (denyFriend.CostWhens.Count == 0 || denyFriend.AbilityLevelCriteriaRowCount != 1)
        {
            CheckTrue(false, "① 前提不成立 ⇒ 本段后续**不执行**（⛔ 不许 `if (前提) Check(结论)` 静默跳过 —— 这一行就是显式红）");
            return;
        }

        // ---- 负例：卡面说「友军死」，但 ability 级 criteria 要**敌方**单位 ⇒ 闸拒 ⇒ 不降费 ----
        {
            var ctx = Battle(new[] { denyFriend }, new CardDef[0]);
            int hi = HandIdx(ctx, 0, "B99E9CostDenyFriend");
            CheckTrue(hi >= 0, "① （前提）那张战术卡在 P0 手里");
            if (hi < 0) return;
            var inst = ctx.Players[0].Hand[hi];
            int before = RuleCore.CostOf(ctx, 0, inst);
            Check(before, 5, "① （前提）原始费用 = 5");

            var deadFriend = Place(ctx, 0, 3, Unit("B99E9DeadFriend", 1, 1, 1));
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Die, 0, deadFriend.Card, subject: deadFriend);

            Check(RuleCore.CostOf(ctx, 0, inst), before,
                  "★★ ① 负例：卡面（`friendly`）命中，但 ability 级 criteria 要 `enemyUnits = 1` ⇒ "
                + "**不降费**（原版 `CanTriggerAbility` 拒 ⇒ 连 `EmitListenerTrigger` 都不发）"
                + "｜🧨 把 `BroadcastCostWhen` 里的闸删掉 / 恒 `return true` ⇒ 这里会掉到 4 ⇒ 红"
                + "｜🧨 调点不传 `subject` ⇒ 闸判不了 ⇒ 放行 ⇒ 这里也红");
        }

        // ---- 正例：同一句文本 + criteria 要**友方** ⇒ 降费 ----
        {
            var allowFriend = B99E9CostCard("B99E9CostAllowFriend", friendlyUnits: 1, enemyUnits: 0);
            var ctx = Battle(new[] { allowFriend }, new CardDef[0]);
            int hi = HandIdx(ctx, 0, "B99E9CostAllowFriend");
            CheckTrue(hi >= 0, "① （前提）正例卡在手里");
            if (hi < 0) return;
            var inst = ctx.Players[0].Hand[hi];
            int before = RuleCore.CostOf(ctx, 0, inst);

            var deadFriend = Place(ctx, 0, 3, Unit("B99E9DeadFriend2", 1, 1, 1));
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Die, 0, deadFriend.Card, subject: deadFriend);

            Check(RuleCore.CostOf(ctx, 0, inst), before - 1,
                  "★★ ① 正例：criteria 要 `friendlyUnits = 1`、死的是友军 ⇒ **降 1 费**"
                + "｜🧨 把闸接成「恒拒」/ 把 `AbilityLevelCriteriaAllows` 的三态当二值（`null` 也当 `false`）⇒ 红");
        }

        // ---- 镜像：友军死 ⇒ criteria 要敌方的卡**不**降费；敌方死 ⇒ 降费 ----
        {
            var allowEnemy = B99E9CostCard("B99E9CostAllowEnemy", friendlyUnits: 0, enemyUnits: 1);
            var ctx = Battle(new CardDef[0], new[] { allowEnemy });
            int hi = HandIdx(ctx, 1, "B99E9CostAllowEnemy");     // 卡在 P1 手里
            CheckTrue(hi >= 0, "① （前提）镜像卡在 P1 手里");
            if (hi < 0) return;
            var inst = ctx.Players[1].Hand[hi];
            int before = RuleCore.CostOf(ctx, 1, inst);

            var deadSelf = Place(ctx, 1, 3, Unit("B99E9DeadSelf", 1, 1, 1));
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Die, 1, deadSelf.Card, subject: deadSelf);
            Check(RuleCore.CostOf(ctx, 1, inst), before,
                  "★ ① 镜像负例：友军（对 P1 是己方）死、criteria 要敌方 ⇒ 不降费");

            var deadFoe = Place(ctx, 0, 3, Unit("B99E9DeadFoe", 1, 1, 1));
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Die, 0, deadFoe.Card, subject: deadFoe);
            Check(RuleCore.CostOf(ctx, 1, inst), before - 1,
                  "★ ① 镜像正例：敌方（对 P1 是敌方）死、criteria 要敌方 ⇒ 降 1 费");
        }
    }

    // ==================================================================
    //  ② #21①：`FireTriggerAt` 那一路现在真的带 `abilityTrigger`/`abilityTarget`
    // ==================================================================

    static void B99E9RallyGate()
    {
        // ---- 数据前提：真卡 `ASH70` 确实落了那一行（它自己 `ta = 30` ⇒ 见文件头的判据订正）----
        {
            var pool = CardDatabase.Load();
            var ash = CardDatabase.FindById(pool, "ASH70");
            CheckTrue(ash != null, "② （前提）卡池里有 `ASH70 Swooping Hawk Exarch`");
            if (ash == null) return;
            Check(ash.AbilityLevelCriteriaRowCount, 1,
                  "② （前提）`ASH70` 的 `abilityLevelCriteria` 有 1 行");
            string why; bool amb;
            int ab = ash.UniqueAbilityIndexForTrigger("ThisCardPlayed", out why, out amb);
            Check(ab, 0, "② （前提）`ThisCardPlayed` 唯一定位到第 0 条 ability（Rally 那一跳喂的就是它）");
            var set = ash.AbilityLevelCriteriaForTrigger("ThisCardPlayed", out why);
            CheckTrue(set != null, "② （前提）按 trigger 取得到那份 criteria");
            if (set != null)
                Check(set.TargetsAffected, 30,
                      "② `ASH70` 的 `targetsAffected = 30 (target)` ⇒ `CheckIfMeetsCriteria.c:73-91` 只做身份比较就 return"
                    + " ⇒ 它的 `excludeTrait/traitsFilter` **在原版永不求值**（判据订正，见文件头）");
        }

        // ---- 行为：合成 Rally 卡（`ta = 10`）—— 目标阵营不满足 ⇒ 不触发；满足 ⇒ 触发 ----
        var rally = B99E9RallyCard("B99E9Rally", friendlyUnits: 0, enemyUnits: 1);
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var carrier = Place(ctx, 0, 3, rally);
        var foe = Place(ctx, 1, 1, Unit("B99E9RallyFoe", 1, 1, 3));
        var friend = Place(ctx, 0, 4, Unit("B99E9RallyFriend", 1, 1, 3));
        CheckTrue(carrier != null && carrier.Card == rally, "② （前提）合成 Rally 卡真的挂到了场上那个 `UnitState` 上");

        // 正例：目标是敌方 ⇒ criteria（enemyUnits = 1）满足 ⇒ 入队
        int w0 = RuleCore.AbilityLevelCriteriaWarningsForTest;
        bool q1;
        using (RuleCore.BeginAction(ctx)) { q1 = RuleCore.FireTriggerAtForTest(ctx, carrier, KeywordTable.Rally, 0, 3, chosen: foe); }
        CheckTrue(q1, "★★ ② 正例：Rally 的目标是**敌方** ⇒ ability 级 criteria 满足 ⇒ **触发**（入队成功）"
                    + "｜🧨 `FireTriggerAt` 不传 `abilityTrigger` ⇒ 这道闸不跑，本行仍绿 —— 但下面那条负例会红");
        Check(RuleCore.AbilityLevelCriteriaWarningsForTest, w0, "★ ② 正例**不出声**（计数不变）");
        CheckTrue(foe.Has(KeywordTable.Vanguard), "★★ ② 正例真的结算了（目标拿到 Vanguard）"
                                               + "｜🧨 队列体没泵（漏 `BeginAction`）⇒ 本行红");

        // 负例：目标是己方 ⇒ criteria（friendlyUnits = 0）不满足 ⇒ 不触发 + 出声
        int w1 = RuleCore.AbilityLevelCriteriaWarningsForTest;
        int vangLayersBefore = B99E9KeywordLayers(friend, KeywordTable.Vanguard);
        bool q2;
        using (RuleCore.BeginAction(ctx)) { q2 = RuleCore.FireTriggerAtForTest(ctx, carrier, KeywordTable.Rally, 0, 3, chosen: friend); }
        CheckTrue(!q2, "★★ ② 负例：Rally 的目标是**己方**（criteria 要敌方）⇒ **不触发**（入队被拒）"
                     + "｜🧨 `EnqueueAbility` 的 `abilityTrigger` 那一格被删掉 ⇒ 闸不跑 ⇒ 这一格会变绿（`q2 == true`）⇒ 红");
        CheckTrue(RuleCore.AbilityLevelCriteriaWarningsForTest > w1,
                  "★★ ② 负例**出声**（`AbilityLevelCriteriaWarningsForTest` +1）—— 反静默的唯一判别式");
        Check(B99E9KeywordLayers(friend, KeywordTable.Vanguard), vangLayersBefore,
              "★★ ② 负例里目标**没有拿到 Vanguard**（正文没结算）");
    }

    // ==================================================================
    //  ③ #21②：战术正文的「唯一定位」支真的会判（`ApplyAbilityFamilyGateUnique`）
    // ==================================================================

    /// <summary>
    /// `ApplyAbilityFamilyGate` 的窄闸③ 改前是「唯一定位 ⇒ 不启用」（因为那条路本来有
    /// `EnqueueAbility` 兜着）；但**战术 / 防御正文**（`PlayTacticBodyDeferred`）**不进**
    /// `EnqueueAbility` 的 `abilityTrigger` 闸 ⇒ 那 19 行原来**一格都没判**。
    /// 本段直接走「生产入口的下半截」`RuleCore.ResolveOps(..., abilityFamilyCard, abilityFamilyTrigger)`
    /// （= `PlayTacticBodyDeferred` 唯一喂那两个实参的地方）验「唯一定位 + criteria 不满足 ⇒ 整段不执行」。
    /// </summary>
    static void B99E9TacticBodyUniqueGate()
    {
        var tactic = B99E9TacticCard("B99E9TacGate", friendlyUnits: 0, enemyUnits: 1);
        CheckTrue(tactic.AbilityLevelCriteriaRowCount > 0, "③ （前提）战术卡落了 ability 级 criteria 行");
        string lwhy; bool lamb;
        CheckTrue(tactic.UniqueAbilityIndexForTrigger("ThisCardPlayed", out lwhy, out lamb) >= 0,
                  "③ （前提）`ThisCardPlayed` 在它上面**唯一定位**得到（= 走新加的那一支，⛔ 不是族闸）");
        if (tactic.AbilityLevelCriteriaRowCount == 0) return;

        // ---- 负例：点选目标 = 己方，但 criteria 要敌方 ⇒ 闸拒 ⇒ 一条 op 都不执行 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var friend = Place(ctx, 0, 3, Unit("B99E9TacFriend", 1, 1, 3));
            var ops = new List<EffectOp> { new EffectOp { Verb = "draw", Amount = 1, Source = "B99E9 fixture" } };
            var unresolved = new List<string>();
            int n = RuleCore.ResolveOps(ctx, 0, null, ops, chosen: friend, out unresolved,
                                        sourceCard: tactic, abilityFamilyCard: tactic,
                                        abilityFamilyTrigger: "ThisCardPlayed");
            Check(n, 0, "★★ ③ 负例：战术正文的 ability 级 criteria（ta=10 / 要敌方）不满足 ⇒ "
                      + "**整段正文一条 op 都不结算**（原版 `CanTriggerAbility` 不过 ⇒ `PlayAbility` 不入队）"
                      + "｜🧨 把窄闸③ 改回「唯一定位 ⇒ 直接 return」⇒ 这一格变 1 ⇒ 红");
        }

        // ---- 正例：点选目标 = 敌方 ⇒ 闸放行 ⇒ 那条 op 真的执行 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var foe = Place(ctx, 1, 1, Unit("B99E9TacFoe", 1, 1, 3));
            var ops = new List<EffectOp> { new EffectOp { Verb = "draw", Amount = 1, Source = "B99E9 fixture" } };
            var unresolved = new List<string>();
            int n = RuleCore.ResolveOps(ctx, 0, null, ops, chosen: foe, out unresolved,
                                        sourceCard: tactic, abilityFamilyCard: tactic,
                                        abilityFamilyTrigger: "ThisCardPlayed");
            Check(n, 1, "★★ ③ 正例：点选目标 = 敌方 ⇒ 闸放行 ⇒ **正文照常结算**"
                      + "｜🧨 把唯一定位支接成「恒拒」⇒ 这一格变 0 ⇒ 红");
        }
    }

    static CardDef B99E9TacticCard(string id, int friendlyUnits, int enemyUnits)
    {
        var ta = new List<CardDef.AbilityTargetSpec>
        {
            new CardDef.AbilityTargetSpec { Trigger = 0, TargetsAffected = new[] { 10 } },
        };
        var rows = new List<CardDef.TargetCriteriaRowSpec>
        {
            new CardDef.TargetCriteriaRowSpec
            {
                Ability = 0, Trigger = 0, Logic = -1,
                Criteria = B99E9Criteria10(friendlyUnits, enemyUnits),
            },
        };
        return new CardDef(id, id, "tactic", "Draw 1 card.", "common", "Test",
                           cost: 1, attack: 0, health: 0, rangedAttack: 0, keywords: null,
                           abilityTriggersVersioned: true,
                           abilityTargets: ta.ToArray(),
                           conditionsVersioned: true,
                           abilityLevelCriteria: rows.ToArray(),
                           abilityLevelCriteriaVersioned: true,
                           targeted: 1);
    }

    // ==================================================================
    //  ④ 结构：两条新口 / 补的形参都还在（防后人删掉）
    // ==================================================================

    static void B99E9StructureWiring()
    {
        var mCost = typeof(RuleCore).GetMethod("CostWhenAbilityCriteriaAllows", BindingFlags.NonPublic | BindingFlags.Static);
        CheckTrue(mCost != null,
                  "★★ ④ `RuleCore.CostWhenAbilityCriteriaAllows` 还在（私有静态）"
                + "｜🧨 删掉 / 改名 ⇒ 取不到 ⇒ 红（它是 #19 那道闸的唯一实现）");
        if (mCost != null)
            Check(mCost.GetParameters().Length, 6,
                  "★ ④ 它的形参是 6 个（`ctx / p / c / kind / subject / actingCard`）—— 末两格就是本批补进来的路");

        var mFam = typeof(RuleCore).GetMethod("ApplyAbilityFamilyGateUnique", BindingFlags.NonPublic | BindingFlags.Static);
        CheckTrue(mFam != null,
                  "★★ ④ `RuleCore.ApplyAbilityFamilyGateUnique` 还在（私有静态）"
                + "｜🧨 把窄闸③ 改回「唯一定位 ⇒ 直接 return」⇒ 它变死代码被删 ⇒ 本段红");

        var mBcw = typeof(RuleCore).GetMethod("BroadcastCostWhen", BindingFlags.NonPublic | BindingFlags.Static);
        CheckTrue(mBcw != null, "★ ④ `BroadcastCostWhen` 还在");
        if (mBcw != null)
        {
            var ps = mBcw.GetParameters();
            bool hasSubject = false, hasActing = false;
            foreach (var p in ps)
            {
                if (p.Name == "subject") hasSubject = true;
                if (p.Name == "actingCard") hasActing = true;
            }
            Check(ps.Length, 6, "★★ ④ `BroadcastCostWhen` 的形参从 4 个变成 6 个（补了 `subject` / `actingCard`）"
                              + "｜🧨 退回 4 个 ⇒ 调点那两格传不进去 ⇒ 闸判不了 ⇒ #19 白做");
            CheckTrue(hasSubject && hasActing, "★★ ④ 那两个新形参名字就是 `subject` / `actingCard`（判据 §1·4 落点 1 的字面形状）");
        }
    }
}
