// B113W2_Assert.cs — 执行代理 `B113W2`（第一百一十三会话 · 第 1 波 · 引擎 criteria 组）
//
// ============================ 它盯的是什么（三条账）============================
//   `B-210` `eachunitdeal` 的**出伤单位池**没过本 op 的 36 格 criteria
//           （`EffectResolver.DoEachUnitDeal` 的 `for (s…) { var u = mine.Board[s]; … }` 循环）。
//           原版 260 `triggerEffectInCard` 的 criteria 描述的就是**出伤单位池**
//           （判据 → `资料/完全复刻/判据/B112W_B193_criteria静默丢弃裁定.md` §2·2；
//            决定性样本 `ASH46 Aerial Dominion`：lg0 = 敌方池、lg1 = 「己方 Vehicle」）。
//   `B-208` `EffectResolver.AddSide` 自己不过滤残骸 ⇒ `includeRemnants` 默认 0（原版默认）
//           在「拿不到 criteria」的降级路上失效。判据 `decomp_full/FilterMethods__CheckIfMeetsCriteria.c:273-278`
//           ⇒ `TargetCriteriaEval.cs:289`；`IncludeRemnantsOf` = 我们这一侧的唯一判法。
//   `B-181` 多 ability 卡在 `blocks.Count != abil.Count` 时 `EffectText.AssignAbilityTargets`
//           **循环之前就 `return`** ⇒ op 既没有 criteria、也没有 gap ⇒ **静默不筛**。
//           现在那一支记 `block-mismatch`，由 `ApplyTargetCriteriaFilter` 出声。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · B-210 A1/A1'：把出伤循环里新加的那段 `TargetCriteriaEval.Matches` 删掉 ⇒ 残骸也开火 ⇒ 实得 −4 ⇒ 红。
//   · B-210 A2（**同一次改动不可能同时弄绿**）：criteria 改成 `includeRemnants = 1` ⇒ 必须 −4。
//     若有人「一刀切剔残骸」（不读 criteria）⇒ A2 变 −2 ⇒ 红；若有人把闸写成恒放行 ⇒ A1 变 −4 ⇒ 红。
//   · B-208 B1/B1'：把 `AddSide` 里 `if (!includeRemnants && u.IsRemnant) continue;` 删掉 ⇒ B1 池里又有残骸 ⇒ 红；
//     把它写成无条件的 `if (u.IsRemnant) continue;` ⇒ B2（`includeRemnants=1` 要保留）⇒ 红。
//   · B-181 C2：把 `EffectText` 新加的 `block-mismatch` 标记删回裸 `return` ⇒ C2 红；
//     C3：把 `ApplyTargetCriteriaFilter` 的 `block-mismatch` 出声支删掉 ⇒ C3 红。
//
// ============================ 怎么用（宿主）============================
//   `RuleEngineTest.Run()` 里那一行聚合追加 `Step(TestB113W2);`
//   （本文件是 `RuleEngineTest` 的**分部** ⇒ 复用夹具 `Battle` / `ProbeBattle` / `Place` /
//    `HandIdx` / `Check` —— ⛔ 另起一个类会把那套夹具抄第二份，违反「一处判据」）。
using System;
using System.Collections.Generic;
using System.Reflection;
using RuleEngine;
using UnityEngine;

public static partial class RuleEngineTest
{
    /// <summary>B113W2 的全部断言（宿主：`RuleEngineTest.Run` 里 `Step(TestB113W2);`）。</summary>
    public static void B113W2Case()
    {
        Section("🆕 B113W2 —— B-210（eachunitdeal 出伤池 criteria）/ B-208（AddSide 残骸闸）/ B-181（多 ability 卡出声）");
        B113W2EachUnitDealRemnant();
        B113W2AddSideRemnantGate();
        B113W2MultiAbilityCriteriaNotice();
    }

    // ==============================================================================================
    //  夹具
    // ==============================================================================================

    /// <summary>一份**形状完整**的 36 格 criteria（⛔ 别省字段：`Unknown` 会让 `TargetCriteriaEval` 直接判「判不了」）。
    /// 形状照 `B106W57Criteria10`（同一条路已经验过），只把 `includeRemnants` / `friendlyUnits` / `enemyUnits` 露成参数。</summary>
    static CardDef.CountSetSpec W2Criteria(int friendlyUnits, int enemyUnits, int includeRemnants,
                                           int includeHero = 1)
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = 0,
            TargetsAffected = 10,               // allFiltered
            ChosenArmy = 0, ArmyFilter = 0,
            KeywordFilter = 0, TraitsFilter = 0,
            MaxTargets = 0, TargetSelectMethod = 0,
            AttackFilter = 0, AttackFilterOption = 0,
            CurrentHealthFilter = 0, CurrentHealthFilterOption = 0,
            ManaCostFilter = 0, ManaCostFilterOption = 0,
            RarityFilter = 0, RarityFilterOption = 0,
            SpellType = 0,
            FriendlyUnits = friendlyUnits,
            EnemyUnits = enemyUnits,
            IncludeHero = includeHero, IncludeMinions = 1,
            IncludeSpells = 0, IncludeWhispers = 0, IncludeWeapons = 0,
            IncludeRemnants = includeRemnants, IncludeHeroParts = 0,
            IncludeSelf = 0, ExcludeNotSelf = 0, ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0,
            ExcludeRace = 0, ExcludeTrait = 0,
            SelectFromChooseResolution = 0,
        };
    }

    /// <summary>`UM82 Sergeant Gadriel` 那一族的最小夹具：**定值伤害的 `eachunitdeal`**
    /// ＋ 一条「己方全体」的 36 格 criteria（`includeRemnants` 可切）。
    /// <para>为什么用定值（不是 `equal to its Shuriken`）：定值不依赖关键词值 ⇒ 残骸（`ClearAllKeywords` 过）
    /// 也会照样开火 ⇒ 「剔没剔残骸」**在数值上看得见**；换成 Shuriken 就永远是 0、验不出东西。</para></summary>
    static CardDef W2EachUnitDealCard(string id, int dmg, int includeRemnants)
    {
        var ta = new[]
        {
            new CardDef.AbilityTargetSpec { Trigger = 0, TargetsAffected = new[] { 10 } },
        };
        var rows = new[]
        {
            new CardDef.TargetCriteriaRowSpec
            {
                Ability = 0, Trigger = 0, Logic = 0,
                // ⚠️ `includeHero = 0`：**督军也是己方棋盘上的单位**（`IncludeHero = 1` 时它会一起开火）⇒
                //    留着它，「几个出伤单位」就不可数了（本组断言全靠那个数）。
                Criteria = W2Criteria(friendlyUnits: 1, enemyUnits: 0,
                                      includeRemnants: includeRemnants, includeHero: 0),
            },
        };
        return new CardDef(id, id, "tactic", $"Each of your units deals {dmg} damage to a random enemy",
                           "common", "Test", 1, 0, 0, 0, keywords: null,
                           abilityTriggers: new[] { 0 },
                           abilityTriggersVersioned: true,
                           abilityTargets: ta,
                           abilityTargetCriteria: rows,
                           abilityTargetCriteriaVersioned: true);
    }

    /// <summary>敌方棋盘上**还活着**的单位血量之和（靶子都摆大血 ⇒ 不会死人 ⇒ 差就是打出的总伤害）。</summary>
    static int W2EnemyHp(BattleContext ctx)
    {
        var ps = ctx.Players[1];
        int t = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ps.Board[s];
            if (u != null && u.IsAlive) t += u.Health;
        }
        return t;
    }

    /// <summary>摆一局「己方 1 个真单位 + 1 个残骸 / 敌方大血靶子」，并把那张 `eachunitdeal` 战术卡打进手牌。
    /// <para>⚠️ **残骸的造法**：`Place` 只建普通单位（它走 `ctx.NewInstance`，实例号段要与真部署一致），
    /// 所以**摆完再把 `IsRemnant` 置真** —— 这正是 `RuleCore.CleanupDeaths` 翻面支的形状
    /// （同一格、同一实例、`Health=1 / 攻 0 / 关键词清空`）。⚠️ 这里**不**去清关键词（夹具单位本来就没有）。</para></summary>
    static BattleContext W2EachUnitDealBattle(CardDef card, out UnitState remnant, out bool remnantOk)
    {
        var ctx = RuleCore.NewBattle(DeckOf("Test", card),
                                     DeckOf("Test", Unit("W2FoeA", 1, 0, 20), Unit("W2FoeB", 1, 0, 20)),
                                     seed: 0, shuffle: false);
        RuleCore.BeginTurn(ctx);
        ctx.Players[0].Energy = 20;
        // 敌方两只大血靶子（加上督军 = 3 个候选）⇒ 「随机一个敌人」不会打死谁，血量差就是总伤害
        Place(ctx, 1, 3, Unit("W2TgtA", 1, 0, 20));
        Place(ctx, 1, 5, Unit("W2TgtB", 1, 0, 20));
        // 己方：1 个真单位 + 1 个残骸
        Place(ctx, 0, 3, Unit("W2Ally", 1, 1, 5));
        var rem = Place(ctx, 0, 2, Unit("W2Rem", 1, 0, 1));
        rem.IsRemnant = true;
        remnant = rem;
        remnantOk = rem.IsRemnant && rem.IsAlive;
        return ctx;
    }

    // ==============================================================================================
    //  ① `B-210` —— 出伤单位池要过 criteria（`includeRemnants` 默认 0 ⇒ 残骸不开火）
    // ==============================================================================================
    static void B113W2EachUnitDealRemnant()
    {
        // ---- A0：夹具自检（硬前提 —— ⛔ 不写 `if (x != null) Check(...)` 那种会静默跳过的形状）----
        var cardOff = W2EachUnitDealCard("W2EachOff", 2, includeRemnants: 0);
        var probe = EffectText.Parse(cardOff.Desc, out _, out _, card: cardOff);
        CheckTrue(probe != null && probe.Count == 1 && probe[0] != null && probe[0].Verb == "eachunitdeal",
                  "A0 （前提）`Each of your units deals 2 damage to a random enemy` 解出**唯一一条** `eachunitdeal`"
                + "｜解不出 ⇒ 后面三条全部无对象");
        CheckTrue(probe != null && probe.Count == 1 && probe[0] != null && probe[0].TargetCriteriaSet != null
                  && probe[0].TargetCriteriaSet.IncludeRemnants == 0,
                  "A0 （前提）那条 op **真挂上了** 36 格 criteria，且 `includeRemnants = 0`"
                + "（没挂上 ⇒ A1 的「−2」是空转，不是闸在起作用）");

        // ---- A1：场上有残骸 + `includeRemnants = 0` ⇒ **残骸不开火** ⇒ 只打 2 点 ----
        UnitState rem1; bool remOk1;
        var ctx1 = W2EachUnitDealBattle(cardOff, out rem1, out remOk1);
        CheckTrue(remOk1, "A1 （前提）己方第 2 格那个是**活着的残骸**（`IsRemnant ∧ IsAlive`）");
        CheckTrue(ctx1.Players[0].Board[3] != null && ctx1.Players[0].Board[3].IsAlive
                  && !ctx1.Players[0].Board[3].IsRemnant,
                  "A1 （前提）己方第 3 格那个是**真单位**（不是残骸）⇒ 出伤单位池恰好 1 个合法 + 1 个残骸");
        int hi1 = HandIdx(ctx1, 0, "W2EachOff");
        CheckTrue(hi1 >= 0, "A1 （前提）那张战术卡在手里");
        // ---- 隔离层：先证明「criteria 本身判得出 true/false」，再谈循环有没有用它 ----
        //   ⚠️ 不加这一层的话，`Matches` 返回 `null`（= 判不了 ⇒ 整条不筛）与「闸没接」在数字上**长得一样**。
        var set1 = probe != null && probe.Count == 1 && probe[0] != null ? probe[0].TargetCriteriaSet : null;
        CheckTrue(set1 != null, "A1 （前提）拿得到那条 op 的 36 格 criteria（`TargetCriteriaSet != null`）");
        if (set1 != null)
        {
            string whyAlly, whyRem;
            CheckTrue(TargetCriteriaEval.Matches(ctx1, 0, set1, ctx1.Players[0].Board[3], null, null, null, out whyAlly) == true,
                      "A1 （前提）criteria 对**真单位**判 `true`（不是「判不了」）");
            CheckTrue(TargetCriteriaEval.Matches(ctx1, 0, set1, ctx1.Players[0].Board[2], null, null, null, out whyRem) == false,
                      "A1 （前提）criteria 对**残骸**判 `false`（`includeRemnants = 0` ⇒ `TargetCriteriaEval.cs:289`）");
        }
        int before1 = W2EnemyHp(ctx1);
        if (hi1 >= 0)
        {
            CheckCode(RuleCore.PlayCard(ctx1, 0, hi1, 0), RuleCodes.OK, "A1 （前提）这张卡打得出去");
            Check(W2EnemyHp(ctx1), before1 - 2,
                  "★★ A1 **残骸不算出伤单位**：己方「1 真单位 + 1 残骸」、criteria `includeRemnants=0` "
                + "⇒ 敌方总伤害 = **2**（只有真单位那一下）"
                + "｜🧨 删掉 `DoEachUnitDeal` 出伤循环里新加的那段 `TargetCriteriaEval.Matches` ⇒ 实得 −4 ⇒ 红"
                + "（这就是 `UM82 Sergeant Gadriel` 在原版里不该多打的那 1-2 点）");
        }

        // ---- A2：灭自证 —— 同一个场、只把 criteria 换成 `includeRemnants = 1` ⇒ 必须 −4 ----
        var cardOn = W2EachUnitDealCard("W2EachOn", 2, includeRemnants: 1);
        var probeOn = EffectText.Parse(cardOn.Desc, out _, out _, card: cardOn);
        CheckTrue(probeOn != null && probeOn.Count == 1 && probeOn[0] != null && probeOn[0].TargetCriteriaSet != null
                  && probeOn[0].TargetCriteriaSet.IncludeRemnants == 1,
                  "A2 （前提）第二条夹具的 criteria `includeRemnants = 1`（闸的**另一侧**）");
        UnitState rem2; bool remOk2;
        var ctx2 = W2EachUnitDealBattle(cardOn, out rem2, out remOk2);
        CheckTrue(remOk2, "A2 （前提）同样摆着一个活残骸");
        int hi2 = HandIdx(ctx2, 0, "W2EachOn");
        CheckTrue(hi2 >= 0, "A2 （前提）那张卡在手里");
        var set2 = probeOn != null && probeOn.Count == 1 && probeOn[0] != null ? probeOn[0].TargetCriteriaSet : null;
        if (set2 != null)
        {
            string whyRem2;
            CheckTrue(TargetCriteriaEval.Matches(ctx2, 0, set2, ctx2.Players[0].Board[2], null, null, null, out whyRem2) == true,
                      "A2 （前提）`includeRemnants = 1` ⇒ criteria 对**残骸**判 `true`（闸的另一侧确实是「要它」）");
        }
        int before2 = W2EnemyHp(ctx2);
        if (hi2 >= 0)
        {
            CheckCode(RuleCore.PlayCard(ctx2, 0, hi2, 0), RuleCodes.OK, "A2 （前提）这张卡打得出去");
            Check(W2EnemyHp(ctx2), before2 - 4,
                  "★★ A2 **灭自证 · `includeRemnants = 1` ⇒ 残骸照样开火** ⇒ 总伤害 = **4**"
                + "｜🧨 把闸写成「一刀切剔残骸」（不读 criteria）⇒ 实得 −2 ⇒ 红；"
                + "与 A1 是**同一次改动不可能同时弄绿**的一对（一刀切弄绿 A1 就弄红 A2，反过来也一样）");
        }

        // ---- A3：对照 —— 场上**没有**残骸 ⇒ 与 A1 同为 −2（证明 A1 的 −2 不是「少了谁」）----
        var ctx3 = RuleCore.NewBattle(DeckOf("Test", cardOff),
                                      DeckOf("Test", Unit("W2FoeC", 1, 0, 20)),
                                      seed: 0, shuffle: false);
        RuleCore.BeginTurn(ctx3);
        ctx3.Players[0].Energy = 20;
        Place(ctx3, 1, 3, Unit("W2TgtC", 1, 0, 20));
        Place(ctx3, 0, 3, Unit("W2AllyC", 1, 1, 5));
        int hi3 = HandIdx(ctx3, 0, "W2EachOff");
        CheckTrue(hi3 >= 0, "A3 （前提）那张卡在手里");
        int before3 = W2EnemyHp(ctx3);
        if (hi3 >= 0)
        {
            CheckCode(RuleCore.PlayCard(ctx3, 0, hi3, 0), RuleCodes.OK, "A3 （前提）这张卡打得出去");
            Check(W2EnemyHp(ctx3), before3 - 2,
                  "★ A3 对照：**场上没有残骸**时同样是 −2 ⇒ A1 的读数不是「残骸被别处吃掉了」，"
                + "而是「残骸本来就不该开火」");
        }
    }

    // ==============================================================================================
    //  ② `B-208` —— `AddSide` 的残骸闸：无 criteria（降级路）⇒ 剔；`includeRemnants=1` ⇒ 留
    // ==============================================================================================
    static void B113W2AddSideRemnantGate()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        CheckTrue(ctx != null, "B0 （前提）夹具开出一局");
        if (ctx == null) return;

        var troop = Place(ctx, 0, 3, Unit("W2B208Troop", 1, 1, 5));
        var rem = Place(ctx, 0, 2, Unit("W2B208Rem", 1, 0, 1));
        rem.IsRemnant = true;
        CheckTrue(rem.IsRemnant && rem.IsAlive && troop.IsAlive && !troop.IsRemnant,
                  "B0 （前提）己方：1 个真单位 + 1 个活残骸");

        var spec = new EffectTargetSpec
        { Raw = "(夹具：己方全体部队)", Side = "own", Kind = "troop", Count = 0, Auto = true };

        // ---- B1：`op == null`（= 这条 effect 没有 36 格数据 / 降级路）⇒ **剔残骸** ----
        var pNull = RuleCore.ResolveTargets(ctx, 0, spec, null, null, null, /*op*/ null);
        CheckTrue(pNull != null && pNull.Contains(troop) && !pNull.Contains(rem),
                  "★★ B1 **没有 criteria 的 op ⇒ `AddSide` 按原版默认（`includeRemnants = 0`）剔掉残骸**"
                + "（池 = 只有那个真单位）"
                + "｜🧨 删掉 `AddSide` 里那句 `if (!includeRemnants && u.IsRemnant) continue;` ⇒ 池里又有残骸 ⇒ 红"
                + "（这正是 `B-208` 说的降级路：`logic-gate` / `no-row` / `block-mismatch` 拿不到 criteria 时，"
                + "`all units` 那类正文会把残骸算进去）");

        // ---- B1'：显式 criteria `includeRemnants = 0` ⇒ 同样剔（两道闸同向）----
        var opOff = new EffectOp { Source = "W2B208Off", TargetCriteriaSet = W2Criteria(1, 0, includeRemnants: 0) };
        var pOff = RuleCore.ResolveTargets(ctx, 0, spec, null, null, null, opOff);
        CheckTrue(pOff != null && pOff.Contains(troop) && !pOff.Contains(rem),
                  "★ B1' criteria `includeRemnants = 0` ⇒ 同样剔残骸（与 `TargetCriteriaEval.cs:289` 同向，不是两套口径）");

        // ---- B2：灭自证 —— `includeRemnants = 1` ⇒ **必须保留** ----
        var opOn = new EffectOp { Source = "W2B208On", TargetCriteriaSet = W2Criteria(1, 0, includeRemnants: 1) };
        var pOn = RuleCore.ResolveTargets(ctx, 0, spec, null, null, null, opOn);
        CheckTrue(pOn != null && pOn.Contains(troop) && pOn.Contains(rem),
                  "★★ B2 **灭自证：criteria 明写 `includeRemnants = 1` ⇒ 残骸留在池里**"
                + "｜🧨 把闸写成无条件的 `if (u.IsRemnant) continue;`（不读 criteria）⇒ 本行红；"
                + "与 B1 是**结构上不可能同时满足**的一对");
    }

    // ==============================================================================================
    //  ③ `B-181` —— 多 ability 卡的 criteria 挂载失败**要出声**（⛔ 不许静默不筛）
    // ==============================================================================================
    static void B113W2MultiAbilityCriteriaNotice()
    {
        // 夹具 = `SW49 Fenrisian Blizzard` 的形状：`desc` **一句话**、`cardAbilities` **两条**。
        var ta = new[]
        {
            new CardDef.AbilityTargetSpec { Trigger = 0, TargetsAffected = new[] { 10 } },
            new CardDef.AbilityTargetSpec { Trigger = 0, TargetsAffected = new[] { 10 } },
        };
        var rows = new[]
        {
            new CardDef.TargetCriteriaRowSpec { Ability = 0, Trigger = 0, Logic = 0, Criteria = W2Criteria(1, 1, 1) },
            new CardDef.TargetCriteriaRowSpec { Ability = 1, Trigger = 0, Logic = 0, Criteria = W2Criteria(1, 1, 1) },
        };
        var multi = new CardDef("W2MultiAbil", "W2MultiAbil", "tactic",
                                "Deal 2 damage to all units", "common", "Test", 1, 0, 0, 0, keywords: null,
                                abilityTriggers: new[] { 0, 0 },
                                abilityTriggersVersioned: true,
                                abilityTargets: ta,
                                abilityTargetCriteria: rows,
                                abilityTargetCriteriaVersioned: true);

        Check(multi.AbilityTargets.Count, 2,
              "C0 （前提）这张夹具卡的数据面确实是 **2 条 ability**（= `blocks.Count(1) != abil.Count(2)` 的必要条件）"
            + "（真卡样例：`SW49 Fenrisian Blizzard`）");

        var ops = EffectText.Parse(multi.Desc, out _, out _, card: multi);
        CheckTrue(ops != null && ops.Count >= 1, "C0' （前提）`desc` 解出了 op（否则 C1/C2 无对象）");
        if (ops == null || ops.Count == 0) return;

        bool allNoSet = true, allGap = true, allVerbOk = true;
        for (int i = 0; i < ops.Count; i++)
        {
            var o = ops[i];
            if (o == null) { allNoSet = false; allGap = false; continue; }
            if (o.Verb != "deal") allVerbOk = false;
            if (o.TargetCriteriaSet != null) allNoSet = false;
            if (o.TargetCriteriaGap != "block-mismatch") allGap = false;
        }
        CheckTrue(allVerbOk, "C1 （前提）解出来的是 `deal`（`Deal 2 damage to all units`）");
        CheckTrue(allNoSet, "★ C1 块数对不上 ⇒ 这一条 op **没挂上** criteria（`TargetCriteriaSet == null`，"
                          + "与原版「宁可不筛也不猜」一致：⛔ 不按位置硬贴）");
        CheckTrue(allGap, "★★ C2 **原因码 `block-mismatch` 记上了**（改前这里既没 criteria 也没 gap ⇒ 下游静默不筛）"
                        + "｜🧨 把 `EffectText.AssignAbilityTargets` 新加的那段标原因码删回裸 `return` ⇒ 本行红"
                        + "（症状卡 = `SW49 Fenrisian Blizzard`）");

        // ---- C3：那个原因码在**消费端真的出声**（不是只有字段值）----
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        Place(ctx, 0, 3, Unit("W2B181A", 1, 1, 5));
        Place(ctx, 1, 3, Unit("W2B181B", 1, 1, 5));
        var spec = new EffectTargetSpec
        { Raw = "(夹具：两个单位)", Side = "any", Kind = "unit", Count = 0, Auto = true };

        int before = W2NoticeCount("targetcriteria|block-mismatch|");
        // 先跑一条**没有** gap 的 op（对照）：它不该产生 block-mismatch 的出声
        var clean = new EffectOp { Source = "W2B181Clean", TargetCriteriaSet = null, TargetCriteriaGap = "" };
        RuleCore.ResolveTargets(ctx, 0, spec, null, null, null, clean);
        int afterClean = W2NoticeCount("targetcriteria|block-mismatch|");
        // 再跑带原因码的那一条
        RuleCore.ResolveTargets(ctx, 0, spec, null, null, null, ops[0]);
        int afterGap = W2NoticeCount("targetcriteria|block-mismatch|");

        CheckTrue(before >= 0, "C3 （前提）能读到 `_structuredConditionNotices`（返回 -1 = 字段改名/搬走 ⇒ 下面必红，⛔ 不假绿）");
        Check(afterClean, before, "C3' 对照：**没有**原因码的 op ⇒ 不产生 `block-mismatch` 出声（区分度）");
        CheckTrue(afterGap > afterClean,
                  "★★ C3 **`block-mismatch` 在消费端真的出声了**（`StructuredConditionNotice` 的去重表新增一条）"
                + "｜🧨 把 `ApplyTargetCriteriaFilter` 里新加的 `else if (… == \"block-mismatch\")` 支删掉 ⇒ 本行红"
                + "（= 回到「静默不筛」那个旧形状）");
    }

    /// <summary>读 `RuleCore._structuredConditionNotices`（私有静态去重表）里以某前缀开头的键数。
    /// <para>⚠️ 与 `RuleEngineTest` 里既有的那条同形（同一份反射口径）；返回 `-1` = 字段改名/搬走 ⇒ 断言应当**红**，⛔ 不假绿。</para></summary>
    static int W2NoticeCount(string prefix)
    {
        var f = typeof(RuleCore).GetField("_structuredConditionNotices",
                                          BindingFlags.NonPublic | BindingFlags.Static);
        var set = f != null ? f.GetValue(null) as HashSet<string> : null;
        if (set == null) return -1;
        int n = 0;
        foreach (var k in set) if (k.StartsWith(prefix, StringComparison.Ordinal)) n++;
        return n;
    }
}
