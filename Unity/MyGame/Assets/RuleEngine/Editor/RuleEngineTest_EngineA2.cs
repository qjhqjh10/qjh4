// RuleEngineTest_EngineA2.cs — 引擎线 `A2` 五件（2026-10-25 · 第七十五会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_EngineA1` / `_EngineB6` / `_G3` … 都在），而同一时刻只有一个写手改一个文件
// ⇒ 本批测试体全写在这里，`RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestA2EngineFive)`**。
//
// 判据（⛔ 不要重新论证）：
//   · **A2-1** `Target == null` 的 `forceattack`（本卡自己打 = `chosenAbility 235/238` 族）
//     ⇒ `possibleTargets` 是**被打的那个** ⇒ logic `targetCriteria` 筛**受害者池**
//     （`d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c` 的 `chosenAbility` 分派 +
//       `A1` 报告 §A1 的两栏表）。`SW9` 是例外：它的 huntMark 行是 **ability 级** criteria
//     （`CardAbility__CanTriggerAbility.c:46` 之前的 `CheckIfMeetsCriteria`），不是受害者筛。
//   · **A2-2** `EntityScript__HasRemovedTrait.c`：读 `activeEffects(+0x108)` 里
//     `CardEffect.buffType == removeTrait(4)` 且 `trait(+0x3C) == 查询值` 的条目
//     （`BuffType.cs`）；写入 = `CardScript__AddEffect.c:680-694`（`case 4`）；
//     清空 = 同文件 `case 3` 进门 `CardScript__RemoveActiveEffects(card, 4)`（不带 trait 过滤）。
//   · **A2-3** `PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:104-117`（档位区间）+
//     `CardScript__ClickMulliganSelected.c:27` / `AI__GetAiMulliganCards.c:39`（两个 `==30` 消费点）。
//   · **A2-4** 已由 `B6` / `B7` / `G5` 收口（`ta==0 / 140 / 150 / 240` 与 `SAU73` 的 `lowest Health`）
//     ⇒ 本文件**不重复**那几条断言（同一件事写两处 = 迟早不一致），只在 `A2` 报告里记复核结论。
//   · **A2-5** 天赋那一拍（`AbilityTrigger.Talent = 46`）的表现走 **trait 框**
//     （`trait_effect_bindings.json` 的 `triggerType 3/6`），不走 `animTimes`
//     ⇒ 那条 warning 的文案必须如实这么说（⛔ 仍出声，不许静默）。
//
// 🔴 每条断言都带 `🧨`（什么改动能让它红）。期望值一律写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestA2EngineFive)`）。
    /// 子用例用 `A2…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestA2EngineFive()
    {
        Section("🆕 引擎线 A2：Target 空卡的 criteria 归栏 · at-turn 片段 criteria · 已移除 trait 簿记"
              + " · 换牌档位 · Talent 文案");
        A2Case1CriteriaOwnership();
        A2Case2AtTurnCriteriaApplies();
        A2Case3RemovedTraitBookkeeping();
        A2Case4MulliganOption();
        A2Case5TalentWarningWording();
    }

    // ==================================================================
    //  夹具小工具
    // ==================================================================

    /// <summary>一条**所有格都已知**的 `TargetCriteria`（未指定的格 = 0 = 原版默认放行）。
    /// ⚠️ 必须逐格显式给：`CountSetSpec` 的缺省是 `Unknown(-1)`，`TargetCriteriaEval`
    /// 遇 `Unknown` 会返回 `null`（判不了 ⇒ 整条不筛）⇒ 夹具会静默变成「没验到」。</summary>
    static CardDef.CountSetSpec A2Criteria(bool excludeFlying)
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = 0,
            TargetsAffected = 10,                  // allFiltered
            ChosenArmy = 0, ArmyFilter = 0,
            KeywordFilter = 0,
            TraitsFilter = excludeFlying ? 390 : 0, // DefinedTrait.flying
            MaxTargets = 1,
            TargetSelectMethod = 0,
            AttackFilter = 0, AttackFilterOption = 0,
            CurrentHealthFilter = 0, CurrentHealthFilterOption = 0,
            ManaCostFilter = 0, ManaCostFilterOption = 0,
            RarityFilter = 0, RarityFilterOption = 0,
            SpellType = 0,
            FriendlyUnits = 0, EnemyUnits = 1,
            IncludeHero = 0,                       // 督军不入池（`CardTypePasses` 的 Hero 格）
            IncludeMinions = 1,
            IncludeSpells = 0, IncludeWhispers = 0, IncludeWeapons = 0,
            IncludeRemnants = 0, IncludeHeroParts = 0,
            IncludeSelf = 0, ExcludeNotSelf = 0, ExcludeSimilarCards = 0, ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0, ExcludeEnemyHero = 0,
            ExcludeRace = 0,
            ExcludeTrait = excludeFlying ? 1 : 0,
            SelectFromChooseResolution = 0,
        };
    }

    /// <summary>造一张「回合开始强制攻击」的合成卡。
    /// `withCriteria` ⇒ 带一条 logic 级 criteria（**排除飞行**）；`false` ⇒ 只有 ability、没有 criteria 行。</summary>
    static CardDef A2AtTurnCard(string id, bool withCriteria)
    {
        var ta = new CardDef.AbilityTargetSpec { Trigger = 31, TargetsAffected = new[] { 10 } };
        var rows = withCriteria
            ? new[]
              {
                  new CardDef.TargetCriteriaRowSpec
                  {
                      Ability = 0, Trigger = 31, Logic = 0, Criteria = A2Criteria(excludeFlying: true),
                  },
              }
            : null;
        return new CardDef(id, id, "unit",
                           "At the start of your turn attack the enemy with highest attack",
                           "common", "Test", 1, 1, 9, 6, null,
                           subtype: "Infantry",
                           // 🔴 `AbilityTargetsVersioned` 在 ctor 里 = `abilityTriggersVersioned`
                           //    （v17 一次加的两列同一个版本位）⇒ 这一位不给，`AbilityTargets` 会被当旧表。
                           abilityTriggersVersioned: true,
                           abilityTargets: new[] { ta },
                           abilityTargetCriteria: rows,
                           abilityTargetCriteriaVersioned: withCriteria);
    }

    /// <summary>驱动「攻方（P0）自己的回合开始」这一拍，返回攻方实例（`A1606` 控制组同款）。</summary>
    static UnitState A2RunTurnStart(BattleContext ctx, CardDef atkCard, out UnitState fly, out UnitState foot)
    {
        ToP1Turn(ctx, 1);
        var atk = Place(ctx, 0, 3, atkCard, exhausted: false);
        fly = Place(ctx, 1, 1, Ranged("A2Fly", 1, 9, 2, 0, KeywordTable.Flying), exhausted: true);
        foot = Place(ctx, 1, 5, Unit("A2Foot", 1, 1, 6), exhausted: true);
        RuleCore.EndTurn(ctx);      // P0 回合结束
        RuleCore.BeginTurn(ctx);    // P1 回合开始（攻方不是它的主人 ⇒ `your turn` 这一拍不发）
        RuleCore.EndTurn(ctx);      // P1 回合结束
        RuleCore.BeginTurn(ctx);    // P0 回合开始 ⇒ 触发
        return atk;
    }

    // ==================================================================
    //  ① `A2-1`：五张卡的 criteria 该归哪一栏
    // ==================================================================

    /// <summary>
    /// 静态层：`Target` 为空的 `forceattack` 上，**该卡自己的 logic criteria 有没有贴上来**。
    /// · `EC12 / EC22 / SAU70` ⇒ 必须贴（`chosenAbility = 238`、criteria 描述受害者）；
    /// · `SW9` ⇒ **不许贴**（它的 huntMark 行是 ability 级触发闸；logic 级 `ta=30 target`
    ///   原样收事件目标 ⇒ 与卡面不矛盾）。
    /// </summary>
    static void A2Case1CriteriaOwnership()
    {
        var pool = CardDatabase.Load();
        A2CheckCardCriteria(pool, "EC12", 10, true);
        A2CheckCardCriteria(pool, "EC22", 10, true);
        A2CheckCardCriteria(pool, "SAU70", 240, true);
        A2CheckCardCriteria(pool, "SW9", -1, false);

        // ---- SW9 的另一半：`If it has a Hunt Mark, gain +1 Attack` 的**条件**在解析层已经有人接 ----
        // 判据 = 卡面（成品卡图 `d:/2/Warpforge部队卡片/Space Wolves/3部队/Warpforge_16_Fenrisian-Wolf.png`：
        // `Tide 1. Ferocity: Attack a non Flying enemy troop. **If it has a Hunt Mark**, gain +1 [Melee]`）
        // ＋ `EffectText` 的 `targethaskw` 条件壳（`EffectOp.ConditionKind`）。
        // ⇒ 所以「huntMark 该不该筛受害者」这个问题的答案是**两边都不是**：
        //   受害者 = 文本 `a non Flying enemy troop`（`Target2` 的 `NotKeyword = flying`）；
        //   猎杀标记 = **加 buff 的前置条件**（`targethaskw`），不是目标池筛子。
        {
            var sw9 = CardDatabase.FindById(pool, "SW9");
            if (sw9 != null)
            {
                var ops = EffectText.Parse(sw9.Desc, out _, out _, card: sw9);
                EffectOp gain = null;
                for (int i = 0; i < ops.Count; i++)
                    if (ops[i] != null && ops[i].Verb == "gain") { gain = ops[i]; break; }
                CheckTrue(gain != null, "① SW9 解析出那条 `gain` op");
                if (gain != null)
                {
                    Check(gain.ConditionKind ?? "", "targethaskw",
                          "★★ ① SW9 的 `+1 Attack` 带着**条件** `it has a Hunt Mark`（`targethaskw`）"
                        + "｜🧨 条件壳丢了 ⇒ 变成无条件 +1 ⇒ 红（那才是真偏离）");
                    CheckTrue((gain.Condition ?? "").ToLowerInvariant().Contains("hunt mark"),
                              "★ ① 条件原文里确实是「hunt mark」｜🧨 条件串被换掉 ⇒ 红");
                }
            }
        }
    }

    static void A2CheckCardCriteria(List<CardDef> pool, string id, int wantTa, bool attached)
    {
        var card = CardDatabase.FindById(pool, id);
        CheckTrue(card != null, $"① （前提）卡池里有 `{id}`");
        if (card == null) return;

        var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
        EffectOp fa = null;
        for (int i = 0; i < ops.Count; i++)
            if (ops[i] != null && ops[i].Verb == "forceattack") { fa = ops[i]; break; }

        CheckTrue(fa != null, $"① `{id}`（{card.Name}）解析出一条 `forceattack`");
        if (fa == null) return;
        CheckTrue(fa.Target == null,
                  $"① `{id}` 的攻击者**没点名**（`op.Target == null` = 本卡自己打，"
                + "`chosenAbility 235/238` 那一族）"
                + "｜🧨 解析层把它改成点名攻击者 ⇒ 下面「criteria 归受害者」的前提就不成立 ⇒ 红");

        if (attached)
        {
            CheckTrue(fa.TargetCriteriaSet != null,
                      $"★★ ① `{id}` 的 logic `targetCriteria` **贴到了这条 op 上**"
                    + "（`AssignAbilityTargets` 的 G1/G2 都过了）"
                    + "｜🧨 块数/条数对不上（G1/G2 失配）⇒ 不贴 ⇒ 红");
            int got = fa.TargetCriteriaSet != null ? fa.TargetCriteriaSet.TargetsAffected : -999;
            Check(got, wantTa,
                  $"★ ① `{id}` 的 criteria `ta = {wantTa}`（与 `cards_engine.json` 现读一致）"
                + "｜🧨 贴错行（例如贴到第一条 buff 行）⇒ 数字变 20 或别的 ⇒ 红");
        }
        else
        {
            CheckTrue(fa.TargetCriteriaSet == null,
                      $"★★ ① `{id}` 的 criteria **不贴在攻击 op 上** —— 它的 huntMark 行是"
                    + " **ability 级** criteria（`CardAbility.targetCriteria +0x30`，`CanTriggerAbility` 触发前判；"
                    + "卡面 `Attack a non Flying enemy troop. **If it has a Hunt Mark**, gain +1 Attack`"
                    + " ⇒ 那是「目标带不带猎杀标记 ⇒ 走哪条 ability」的分支闸），logic 级 `ta=30 target` 不筛"
                    + "｜🧨 把 ability 级 criteria 合进 logic 行（或让 SW9 的 op 挂上它）⇒ 红");
        }
    }

    // ==================================================================
    //  ② `A2-1` 端到端：at-turn 片段正文的 criteria **真的筛了受害者池**
    // ==================================================================

    /// <summary>
    /// 对拍两组：**同一张卡、同一局面**，唯一差别 = 有没有那条 criteria（`excludeTrait = flying`）。
    /// · 有 criteria：飞行敌人（攻 9，最高）被**排除** ⇒ 打地面那个；
    /// · 没 criteria：飞行敌人攻击力最高 ⇒ `PickMost = +attack` 挑它（远程 6 打飞行合法）。
    /// ⇒ 两组结果相反 = 这条 criteria 真的在**受害者池**上生效（`DoForceAttack` 的 `critOnDefender`）。
    /// ⚠️ 飞行只挡近战（`RuleCore.IsValidTarget`：`!ranged && 目标会飞 && 攻方不会飞`）⇒
    ///    攻方做成**远程 6 / 近战 1**，两种结果都打得出去。
    /// </summary>
    static void A2Case2AtTurnCriteriaApplies()
    {
        // ---- ⓪ 真卡 `GOF86`：at-turn 片段的两半都要贴上（36 格 criteria + cond/ex 行）----
        {
            var gof = CardDatabase.FindById(CardDatabase.Load(), "GOF86");
            CheckTrue(gof != null, "② （前提）卡池里有 `GOF86`");
            if (gof != null)
            {
                var body = EffectText.Parse("attack a random non Flying enemy", out _, out _);
                EffectText.AttachAtTurnForceAttackCriteria(gof, body, "turn_start");
                CheckTrue(body.Count == 1 && body[0].TargetCriteriaSet != null,
                          "★★ ② `GOF86` 的 at-turn 片段贴上了 36 格 criteria"
                        + "｜🧨 不跑那个 helper（= 改前）⇒ 红");
                Check(body.Count == 1 && body[0].TargetCriteriaSet != null
                      ? body[0].TargetCriteriaSet.TargetsAffected : -999, 10,
                      "★ ② 贴的是 `ta = 10 (allFiltered)`（`cards_engine.json` 现读）");
                CheckTrue(body[0].TargetCondition != null && body[0].TargetCondition.Condition == 220
                          && body[0].TargetCondition.Exclude,
                          "★★ ② 同一格 `(ab=0,lg=0)` 的 **`excludeCondition=220 (dealtKillDmg)`** 也贴上了"
                        + "（原版同一个 `TargetCriteria` 对象的 `conditionsFilter/excludeCondition`；"
                        + "我们拆成 `unitConditions` 一列）"
                        + "｜🧨 只贴 36 格、不贴 cond/ex ⇒ 这一条红（`dealtKillDmg` 那半静默失效）");

                var body2 = EffectText.Parse("attack a random non Flying enemy", out _, out _);
                EffectText.AttachAtTurnForceAttackCriteria(gof, body2, "turn_end");
                CheckTrue(body2[0].TargetCriteriaSet == null,
                          "★ ② 相位不符（`turn_end`）⇒ 一条都不贴（窄闸：`GOF86` 的 trigger 是 30 TurnStart）"
                        + "｜🧨 不看相位 ⇒ 别的 at-turn 卡会被贴错 ability ⇒ 红");
            }
        }

        // ---- 组 A：有 criteria（排除飞行）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            UnitState fly, foot;
            A2RunTurnStart(ctx, A2AtTurnCard("A2WithCrit", withCriteria: true), out fly, out foot);
            Check(fly.Health, 2,
                  "★★ ② 组 A（有 criteria）：**飞行敌人一口血没掉** —— criteria 把它从受害者池里筛掉了"
                + "（`excludeTrait = flying`，原版 `TargetCriteria.excludeTrait`）" + LogTail(ctx));
            CheckTrue(foot.Health < 6,
                      $"★★ ② 组 A：地面敌人挨了这一记（生命 6 → {foot.Health}）⇒ 打的确实是筛剩下的那个"
                    + "｜🧨 `DoForceAttack` 不把 criteria 交给 `DefenderPool`（= 改前）⇒ 飞行那个（攻 9）"
                    + "被 `PickMost=+attack` 挑走、它掉血 ⇒ 上面那一格红");
        }

        // ---- 组 B（控制组）：同卡去掉 criteria ----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            UnitState fly, foot;
            A2RunTurnStart(ctx, A2AtTurnCard("A2NoCrit", withCriteria: false), out fly, out foot);
            CheckTrue(fly.Health < 2,
                      $"★★ ② 组 B（控制组，无 criteria）：飞行敌人（攻 9 = 最高）**被打中了**"
                    + $"（生命 2 → {fly.Health}）"
                    + "｜🧨 这一格红说明「组 A 没打飞行」另有原因（例如远程打不了飞行 / 挑法不是最高攻）"
                    + "⇒ 组 A 就不再是判据");
            Check(foot.Health, 6,
                  "★ ② 组 B：地面敌人**没挨打**（`maxTargets = 1` 只挑最高攻那个）"
                + "｜🧨 两个都打了 ⇒ 挑法/裁剪变了 ⇒ 红（与组 A 互为灭自证：同一局面只换了 criteria）");
        }
    }

    // ==================================================================
    //  ③ `A2-2`：「已移除 trait」簿记（`HasRemovedTrait`）
    // ==================================================================

    /// <summary>
    /// 原版 `EntityScript.HasRemovedTrait` 读的是 **`activeEffects` 里 `buffType = removeTrait` 的记录**，
    /// 不是「当前带不带」。本用例钉四件事：
    /// ① 没摘过 ⇒ false；② `lose X` 摘掉 ⇒ `Has=false` ∧ `HasRemovedTrait=true`；
    /// ③ 对**不在身上**的 trait 调摘除 ⇒ **不写账**（原版 `RemoveTrait` 找不到就 LogWarning 返回）；
    /// ④ 加任意一条 trait ⇒ 整本清掉（原版 `AddEffect case 3` 的 `RemoveActiveEffects(4)`）。
    /// 再加 `280 siege` 的**消费点**：读的必须是这本账。
    /// </summary>
    static void A2Case3RemovedTraitBookkeeping()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var u = Place(ctx, 0, 1, Unit("A2Reloader", 1, 1, 3, KeywordTable.Stealth));

        CheckTrue(u.Has(KeywordTable.Stealth), "③ （前提）夹具单位此刻带 `stealth`");
        CheckTrue(!u.HasRemovedTrait(KeywordTable.Stealth),
                  "★ ③ **没摘过 ⇒ `HasRemovedTrait = false`**（那是「已移除簿记」，不是「当前带不带」）"
                + "｜🧨 把它实现成 `Has()` ⇒ 这里立刻红");

        var ops = EffectText.Parse("Lose Stealth", out _, out _);
        Check(ops != null ? ops.Count : -1, 1, "③ （前提）`Lose Stealth` 解析成 1 条 `lose` op");
        RuleCore.ResolveOps(ctx, 0, u, ops, "A2-2 夹具");

        CheckTrue(!u.Has(KeywordTable.Stealth), "★ ③ `lose stealth` 之后 `Has = false`");
        CheckTrue(u.HasRemovedTrait(KeywordTable.Stealth),
                  "★★ ③ `lose stealth` 之后 `HasRemovedTrait = true`"
                + "（= 原版那个 `CardEffect{buffType = removeTrait(4), trait = stealth}`）"
                + "｜🧨 只在 `RemoveAll` 里写账、但 `ApplyOneGain` 没接 ⇒ 这里红；"
                + "🧨 写成「摘了就记」且不看「原来在不在」⇒ 下面那条负例红");

        // 负例（灭自证）：对**不在身上**的 trait 调摘除 ⇒ 原版 `RemoveTrait` 找不到 ⇒ 不写账
        u.RemoveKeyword(KeywordTable.Flying, 1);
        CheckTrue(!u.HasRemovedTrait(KeywordTable.Flying),
                  "★★ ③ 对**不在身上**的 trait 调摘除 ⇒ **不写账**（原版 `RemoveTrait` 找不到 trait 就"
                + " LogWarning 返回、根本不建那条 effect）"
                + "｜🧨 在 `RemoveAll` 里无条件 `Add` ⇒ 本行红（这条正是「不能只看 `RemoveAll`」的判别式）");

        // 加任意一条 trait ⇒ 原版 `AddEffect case 3` 的 `RemoveActiveEffects(4)` 把整本清掉
        u.AddKeyword("flank", 1);
        CheckTrue(!u.HasRemovedTrait(KeywordTable.Stealth),
                  "★★ ③ 加一条 trait ⇒ 已移除簿记**整本清掉**（原版 `CardScript__AddEffect.c:474` 的"
                + " `RemoveActiveEffects(card, 4)` 不带 trait 过滤）"
                + "｜🧨 改成「只清同名 trait」⇒ 本行红（那是「看着更合理」的偏离）");

        // ---- 消费点：`280 siege` 必须读这本账 ----
        var siegeCard = new CardDef("A2Siege", "A2Siege", "unit", "", "common", "Test", 1, 1, 5, 0, null,
                                    subtype: "Infantry",
                                    hasActiveAbility: 1,
                                    activeNeedsLoading: 1,
                                    alternativeNeedsLoading: 0,
                                    hasAlternativeAbility: 0);
        var s = Place(ctx, 0, 2, siegeCard);
        var r0 = ConditionEval.MeetsUnit(ctx, s, 280);
        CheckTrue(r0 == true,
                  "★ ③ （正例前提）`hasActiveAbility=1` ∧ `needsLoading=1` ∧ 没移除过 `reload` ⇒ 280 = true"
                + "｜🧨 数据面那三格没接上 ⇒ 返回 null/false ⇒ 红");
        s.MarkTraitRemoved(ConditionEval.ReloadTrait);
        var r1 = ConditionEval.MeetsUnit(ctx, s, 280);
        CheckTrue(r1 == false,
                  "★★ ③ 280 的「第二道闸」读的是 `HasRemovedTrait(490 reload)`："
                + "记一条「已移除 reload」⇒ 立刻变假"
                + "｜🧨 改回 `u.Has(\"reload\")`（= 改前）⇒ 这一格仍是 **true** ⇒ 红（灭自证）");

        // 负例：同一张卡把记录清掉（加一条 trait）⇒ 又变 true
        s.AddKeyword("flank", 1);
        CheckTrue(ConditionEval.MeetsUnit(ctx, s, 280) == true,
                  "★ ③ 再 `AddKeyword` 一次（清空簿记）⇒ 280 又变回 true"
                + "｜🧨 「清空点」没接上 ⇒ 一直 false ⇒ 红（与上一条互为灭自证：一真一假两个方向）");
    }

    // ==================================================================
    //  ④ `A2-3`：换牌档位 `MulliganOption`
    // ==================================================================

    /// <summary>
    /// ① 开局指派：先手侧全 **0**；后手侧 `pos=0`、`e=secondExtraCards` ⇒ **下标 0..e-1 = 10**、其余 0；
    ///    今天 1351 张 `initialHandModifier` 全 0 ⇒ 20/30 不可达。
    /// ② 两个消费点：`==30` 的牌**换不掉**（玩家）/ **跳过**（AI），改成 10/0 就能换 ⇒ 灭自证。
    /// </summary>
    static void A2Case4MulliganOption()
    {
        var d0 = new List<CardDef> { HeroOf("A2WL0", "Ultramarines", 2, 30) };
        var d1 = new List<CardDef> { HeroOf("A2WL1", "Goff", 2, 30) };
        // 后手那侧全放 >4 费的（AI 换牌判据 = `manaCost > 4`），先手那侧放 1 费
        for (int i = 0; i < 8; i++)
        {
            d0.Add(Unit("A2Cheap0_" + i, 1, 1, 3));
            d1.Add(Unit("A2Costly1_" + i, 6, 1, 3));
        }
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false,
                                     cardPool: new List<CardDef>(), openMulligan: true);
        CheckTrue(ctx.MulliganOpen, "④ （前提）换牌阶段开着");

        int first = ctx.FirstSeat, second = ctx.SecondSeat;
        int startHand = ctx.Vars.startingHand, extra = ctx.Vars.secondExtraCards;

        // ---- ① 档位指派 ----
        for (int i = 0; i < ctx.Players[first].Hand.Count; i++)
            Check(ctx.Players[first].Hand[i].MulliganOption, 0,
                  $"★ ④ 先手侧下标 {i} 的档位 = **0**（`pos=0`、`e=0`）"
                + "｜🧨 把 `e` 也给先手 ⇒ 下标 0 变 10 ⇒ 红");

        Check(ctx.Players[second].Hand.Count, startHand + extra,
              "④ （前提）后手手牌 = `startingHand + secondExtraCards`（原版 `count` 公式）");
        for (int i = 0; i < ctx.Players[second].Hand.Count; i++)
        {
            int want = i < extra ? 10 : 0;
            Check(ctx.Players[second].Hand[i].MulliganOption, want,
                  $"★ ④ 后手侧下标 {i} 的档位 = **{want}**（`pos=0`、`e={extra}` ⇒ 只有前 `e` 张是 10）"
                + "｜🧨 区间方向写反（把 10 给到末尾）⇒ 红；🧨 把 20/30 也发出来 ⇒ 红");
            CheckTrue(ctx.Players[second].Hand[i].MulliganOption == 0
                      || ctx.Players[second].Hand[i].MulliganOption == 10,
                      "★ ④ 今天 **20 / 30 两档不可达**（`initialHandModifier` 全 0；协程里 `+0x3c` 写死 0）");
        }

        // ---- ①-b `20` 档的区间正例（合成督军 `initialHandModifier = 2`）----
        // 原版 `pos = max(mod, 0)` ⇒ 下标 `0..pos-1 = 20`、`pos..pos+e-1 = 10`、其余 0；
        // `count = startingHand + min(mod,0) + pos + e` ⇒ 后手 3+0+2+1 = 6 张。
        // ⚠️ 现池 1351 张 `initialHandModifier` 全 0 ⇒ **这一档只能拿合成督军验**。
        {
            var e0 = new List<CardDef> { HeroOf("A2MWL0", "Ultramarines", 2, 30) };
            var e1 = new List<CardDef>
            {
                new CardDef("A2MWL1", "A2MWL1", "hero", "", "common", "Test", 0, 2, 30, 0, null,
                            initialHandModifier: 2),
            };
            for (int i = 0; i < 8; i++)
            {
                e0.Add(Unit("A2MCheap0_" + i, 1, 1, 3));
                e1.Add(Unit("A2MCostly1_" + i, 6, 1, 3));
            }
            var c2 = RuleCore.NewBattle(e0, e1, seed: 0, shuffle: false,
                                        cardPool: new List<CardDef>(), openMulligan: true);
            int s2 = c2.SecondSeat;
            int pos = 2, ex = c2.Vars.secondExtraCards;
            Check(c2.Players[s2].Hand.Count, c2.Vars.startingHand + pos + ex,
                  "★ ④ （前提）`initialHandModifier = 2` ⇒ 后手 `count = startingHand + 0 + 2 + e`");
            for (int i = 0; i < c2.Players[s2].Hand.Count; i++)
            {
                int want = i < pos ? 20 : (i < pos + ex ? 10 : 0);
                Check(c2.Players[s2].Hand[i].MulliganOption, want,
                      $"★★ ④ 后手（mod=2）下标 {i} 的档位 = **{want}**"
                    + "（`0..1 = 20` · `2 = 10`（e=1）· 其余 = 0）"
                    + "｜🧨 区间边界错一格（`<=` 写成 `<`）⇒ 红；"
                    + "🧨 把 30 也发出来（`i < 0` 那一支）⇒ 红");
            }
        }

        // ---- ② 玩家闸：`==30` 换不掉 ----
        var ps = ctx.Players[second];
        string keepName = ps.Hand[0].Card.Name;
        ps.Hand[0].MulliganOption = 30;
        int swapped = RuleCore.Mulligan(ctx, second, new List<int> { 0 });
        Check(swapped, 0,
              "★★ ④ `mulliganOption = 30` ⇒ 玩家换牌**被拒**（原版 `CardScript__ClickMulliganSelected.c:27`）"
            + "｜🧨 不判这一格 ⇒ 返回 1 ⇒ 红");
        Check(ps.Hand[0].Card.Name, keepName, "★ ④ 那张 30 档的牌**还在原位**");
        ps.Hand[0].MulliganOption = 10;
        int swapped2 = RuleCore.Mulligan(ctx, second, new List<int> { 0 });
        Check(swapped2, 1,
              "★★ ④ 同一张改成 **10 档** ⇒ 换得掉"
            + "｜🧨 上面那条红的唯一原因是「30 档」这件事 —— 如果 10 档也换不掉，就说明闸写宽了 ⇒ 红");

        // ---- ③ AI 闸：`==30` 跳过 ----
        var ai = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false,
                                    cardPool: new List<CardDef>(), openMulligan: true);
        int aiSeat = ai.SecondSeat;
        var allIdx = SimpleAI.AiMulliganIndices(ai, aiSeat);
        Check(allIdx.Count, ai.Players[aiSeat].Hand.Count,
              "④ （控制组）AI 侧手里全是 `cost > 4` ⇒ 全部进换牌表"
            + "｜🧨 这个前提不成立（抽到的不是 6 费卡）⇒ 下面那条不可信 ⇒ 红");
        ai.Players[aiSeat].Hand[1].MulliganOption = 30;
        var gated = SimpleAI.AiMulliganIndices(ai, aiSeat);
        Check(gated.Count, allIdx.Count - 1,
              "★★ ④ `mulliganOption = 30` ⇒ AI **跳过**那一张（原版 `AI__GetAiMulliganCards.c:39`）"
            + "｜🧨 不判这一格 ⇒ 条数与上面相等 ⇒ 红");
        CheckTrue(!gated.Contains(1), "★ ④ AI 跳过的正是下标 1 那一张");
        ai.Players[aiSeat].Hand[1].MulliganOption = 0;
        Check(SimpleAI.AiMulliganIndices(ai, aiSeat).Count, allIdx.Count,
              "★★ ④ 改回 **0 档** ⇒ 又回到换牌表（灭自证：上面那条红的唯一原因是 30）");
    }

    // ==================================================================
    //  ⑤ `A2-5`：96 张天赋卡的 warning 文案
    // ==================================================================

    /// <summary>
    /// 天赋（`AbilityTrigger.Talent = 46`）的表现走 **trait 框**（`trait_effect_bindings.json`
    /// 的 `triggerType 3/6`，`Core/TraitFrames.DisplayTrigger`），**不走 `animTimes`**
    /// ⇒ 「有 ability、没动画行」那条通用 warning 的说法**误导**（技术上 `animTimes` 确实 0 行）。
    /// 本用例只断**关键短语**：① 必须说「已发出」+「trait_effect_bindings」；② **不许**再说
    /// 「没有对应动画行」那句（那是「数据缺口」的说法）；③ 非 Talent 的 trigger **仍**用通用句。
    /// </summary>
    static void A2Case5TalentWarningWording()
    {
        var pool = CardDatabase.Load();
        var talent = CardDatabase.FindById(pool, "ASH19");   // Witchblade Warlock（`abilityTriggers` 含 46）
        CheckTrue(talent != null, "⑤ （前提）卡池里有 `ASH19`（96 张天赋卡之一）");
        if (talent == null) return;
        CheckTrue(talent.AbilityTriggerPresence("Talent") == 1,
                  "⑤ （前提）`ASH19` 的 `abilityTriggers` 里真有 `Talent(46)`");

        string msg = RuleCore.AbilityNoAnimRowMessageForTest(talent, "Talent");
        CheckTrue(msg.Contains("trait_effect_bindings"),
                  "★★ ⑤ 天赋那条日志**点名了正确的表现来源**（`trait_effect_bindings.json` 的 trait 框）"
                + "｜🧨 用回旧的通用句 ⇒ 不含这个词 ⇒ 红");
        CheckTrue(msg.Contains("不是缺陷"),
                  "★★ ⑤ 明说「`animTimes` 没行**不是缺陷**」（技术上真、但不误导）"
                + "｜🧨 删掉这句 ⇒ 红");
        CheckTrue(!msg.Contains("没有对应动画行"),
                  "★★ ⑤ **不再**用「有 ability 但**没有对应动画行**」那句（它是「数据缺口」的说法，"
                + "而天赋的表现本来就不在 `animTimes` 里）"
                + "｜🧨 改回通用句 ⇒ 红");
        CheckTrue(msg.Contains("已发出"),
                  "★ ⑤ 仍如实说「触发**已发出**」（⛔ 不是静默：本工程红线）");

        string other = RuleCore.AbilityNoAnimRowMessageForTest(talent, "UnitDamaged");
        CheckTrue(other.Contains("没有对应动画行"),
                  "★ ⑤ **非 Talent** 的 trigger 仍走通用句（别把整条 warning 都改没了）"
                + "｜🧨 顺手把通用句也删掉 ⇒ 红（那会让真正的数据缺口不再出声）");
    }
}
