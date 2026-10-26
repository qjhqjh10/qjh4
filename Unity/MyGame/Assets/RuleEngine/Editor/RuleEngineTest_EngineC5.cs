// RuleEngineTest_EngineC5.cs — 引擎线 `W2`：`C5` 剩余三件（Q2 / Q3 / Q4）
// （2026-10-24 · 执行代理 `W2`）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineP3` /
// `_EngineB6` / `_Priority` … 都在），而**同一时刻只有一个写手改一个文件** ⇒ 本批测试体写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestEngineC5)`**（+ 重跑 diag 生成器）。
//
// 判据（⛔ 不要重新论证 —— 全部已由只读代理闭合）：
//   · **Q2** → `资料/普查产出_1024c/C5_引擎战斗剩余判据.md` §Q2（+ `CardAbility__CanTriggerAbility.c:46-48`）
//   · **Q3** → 同上 §Q3（+ `CardAbility__IsRightContext.c` 六档逐句）
//   · **Q4** → 同上 §Q4（+ `FilterMethods__CheckIfMeetsCriteria.c` 逐句 / `E9_卡表v25_logic级判据列.md`）
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量（`0` / `-1` / `10` / `580` …），⛔ 不从实现常量反算（那会自证）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestEngineC5)`）。
    /// 三个子用例是 `C5…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestEngineC5()
    {
        Section("🆕 引擎线 W2·C5：Q2 trigger 闸次序（presence 优先）· Q3 区域档入队闸 · Q4 v25 36 格 criteria 消费端");
        C5Q2TriggerGateOrder();
        C5Q3ZoneContextGate();
        C5Q4TargetCriteriaConsumer();
        // 🆕 2026-10-25（引擎线 `A3`）：Q1 —— `talent`（Trigger 46）回合开始那一拍的事件
        C5Q1TalentTurnStartTrigger();
        // 🆕 2026-10-25（引擎线 `A1`）：36 格 criteria **该筛哪一侧**（`forceattack` 族）
        C5Q4bForceAttackCriteriaSide();
    }

    /// <summary>🔴🆕 **2026-10-25（引擎线 `A1`）：`forceattack` 族的 `targetCriteria` **只筛受害者池**。
    ///
    /// <para>**现场**（`ruleprobe play EC46`，改前）：`（按原版 targetCriteria 36 格筛（(ab=0, lg=0)）：4 → 0）`
    /// → `一个能去攻击的单位都没有，空过` ⇒ 整张卡静默什么都不做。</para>
    ///
    /// <para>**判据**（第一权威，逐句 → `资料/普查产出_1024d/W25_引擎8红修复_执行报告.md` §六·1）：
    /// `AbilityLogic__PlayAbility.c:2640-2690` 的 `case 0xed`（= `chosenAbility 237
    /// scriptedTargetCardAttackDontCount`）里，攻击者是 `abilityData + 0x20`（**点选的那张牌**、由能力定死），
    /// `possibleTargets[i]` 一律落 `AddScriptedForceAttack(actingCard, targetCard, …)` 的 **targetCard**
    /// ⇒ `targetCriteria` 建出来的那批 **= 被打的受害者**。
    /// 🔴 **但 230/231/233 那三档相反**（`case 0xe6/e7/e9` 把 `possibleTargets[i]` 传给
    /// `AddForceAttack(targetCard)`，而 `_ResolveForceAttack_d__504__MoveNext.c:33-66` 拿那张卡去
    /// `AI__SelectBestAttackAction` ⇒ 它是**被逼着去攻击的那一个**）⇒ ⛔ **不能一律挪到受害者池**
    /// （那会把 `BL64` 的 `conditionsFilter = damaged`（描述友方攻击者）筛到敌人身上）。
    /// 落点 = `RuleCore.DoForceAttack` 的 `critOnDefender` 判据。</para>
    /// </summary>
    static void C5Q4bForceAttackCriteriaSide()
    {
        var pool = CardDatabase.Load();
        CardDef ec = null;
        foreach (var c in pool) if (c != null && c.Id == "EC46") { ec = c; break; }
        CheckTrue(ec != null, "（前提）池里有 `EC46`（`Peerless Bladesmen`，按 **id** 找）");
        if (ec == null) return;

        // ---- 前提（灭自证的第一半）：criteria **真的挂上了**，而且**描述的是敌人** ----
        // 少了这一条，下面那个判别式在「v25 两列没接上 ⇒ 根本不筛」时也会绿（= 因为错的原因通过）。
        var ops = EffectText.Parse(ec.Desc, out _, out _, false, card: ec);
        EffectOp fa = null;
        if (ops != null) foreach (var o in ops) if (o != null && o.Verb == "forceattack") fa = o;
        CheckTrue(fa != null, "（前提）`EC46` 解析出 `forceattack` op");
        if (fa == null) return;
        CheckTrue(fa.TargetCriteriaSet != null,
                  "★★ **前提成立**：这条 op 真的挂着 36 格 criteria（`TargetCriteriaSet`）"
                + " —— 没有它，「筛错池子」这个 bug 根本不会发生、下面那条也就测不到东西"
                + "｜🧨 `abilityTargetCriteria` 那一列没接上 ⇒ 本行红（那时先修数据管线，"
                + "⛔ 不是把本行改成「预期 null」）");
        if (fa.TargetCriteriaSet == null) return;
        Check(fa.TargetCriteriaSet.EnemyUnits, 1,
              "（前提）criteria 的 `enemyUnits` = 1 —— 它**描述的是被打的敌人**（`cards_engine.json` 现读）");
        Check(fa.TargetCriteriaSet.FriendlyUnits, 0, "（前提）criteria 的 `friendlyUnits` = 0");

        // ---- 行为：真打一发 ----
        var ctx = BattlePool(new[] { ec }, new CardDef[0], pool, "Test", 11);
        ToP1Turn(ctx, 3);
        ctx.Players[0].Energy = 20;
        var atk = Place(ctx, 0, 3, Unit("C5FAtk", 1, 3, 5));     // 友方攻击者（3 攻）
        // 🔴 2026-10-25（执行代理 `F1` · 铁律 5 就地订正）：这个敌人原来是 **0 攻**。
        //   而 `Target2` 是 `the enemy with highest attack`，候选池**含敌方督军**
        //   （`ruleprobe play EC46` 现读：`（按「+attack」挑：FixtureWarlord、… → …）`）——
        //   夹具督军 **2 攻** ⇒ 0 攻的 foe **根本不会被挑中**（强制攻击打的是督军，foe 一滴血不掉）。
        //   ⇒ 那条断言当时红的是**夹具**（不是实现）：`DoForceAttack` 的行为本身是对的。
        //   现在给 foe **4 攻**（> 督军 2 攻）⇒ 挑中的确实是它；它反击 4 点 ⇒ 攻击者 5 血剩 1（不死亡）。
        var foe = Place(ctx, 1, 5, Unit("C5EFoe", 1, 4, 9));     // 敌人（4 攻 / 9 血 ⇒ 是「攻击力最高」那个）
        var enemyWarlord = ctx.Players[1].Warlord;
        CheckTrue(enemyWarlord != null && foe.Attack > enemyWarlord.Attack,
                  "（前提）`C5EFoe` 的攻击力**高于敌方督军**（" + foe.Attack + " > "
                + (enemyWarlord != null ? enemyWarlord.Attack.ToString() : "?") + "）"
                + " ⇒ `+attack` 挑中的一定是它（候选池含敌方督军 —— `ruleprobe play EC46` 现读）"
                + "｜🧨 把 foe 的攻击力降到督军以下 ⇒ 下面那条量的就不是 foe（= 2026-10-25 那次红）");
        int wlHpBefore = enemyWarlord != null ? enemyWarlord.Health : -1;
        int hpBefore = foe.Health;
        int hi = HandIdx(ctx, 0, ec.Name);
        CheckTrue(hi >= 0, "（前提）`EC46` 在手上");
        if (hi < 0) return;
        CheckCode(RuleCore.PlayTactic(ctx, 0, hi, 3), RuleCodes.OK,
                  "打出 `EC46`（点我方那个攻击者）");
        Check(foe.Health, hpBefore - 3,
              "★★ **criteria 只筛受害者池**：`Target friendly unit attacks the enemy with highest attack`"
            + " 打出去 ⇒ 友方那个 3 攻打了一发（敌人 9 → 6）"
            + "｜🧨 把 criteria 套回**攻击者池**（= 2026-10-24 那个真缺陷）⇒ 友方被 `enemyUnits = 1` 全部淘汰 ⇒ "
            + "一个能去攻击的单位都没有、敌人一滴血不掉 ⇒ 本行红");
        CheckTrue(enemyWarlord == null || enemyWarlord.Health == wlHpBefore,
                  "★ 敌方督军**一点没挨**（" + wlHpBefore + " → "
                + (enemyWarlord != null ? enemyWarlord.Health.ToString() : "?") + "）"
                + " —— 打的是卡面点名的「攻击力最高的敌人」= `C5EFoe`，不是随便一个敌方单位"
                + "｜🧨 挑错人（挑到督军）⇒ 本行红 —— 它也挡住「上面那条因为打错了人而假绿」");
        // 🔴 2026-10-26（`B8_BM` · 铁律 5 就地订正）：`EC46` 的 `chosenAbility = **237**`
        //    （`cards_truth.json` 现读）⇒ 原版 `AbilityLogic__PlayAbility.c:2752-2766` 的 `0xed` 支
        //    传 `AddScriptedForceAttack(…, 1, 0)`（第 8 参 = `dontCountAttack`）⇒ `BattleManager__
        //    AddScriptedForceAttack.c:42-46` 预扣 + 排 `AddResetActions` ⇒ **不消耗行动**
        //    （本行原来断的是「疲劳」，那是改前「一律不退还」的产物）。
        //    ⚠️ 只断 `!Exhausted` 在「这段根本没执行」时也会绿 ⇒ 补一条数值灭自证。
        CheckTrue(!atk.Exhausted,
                  "★ 攻击算「本回合已行动」……**但原版对 237 这一族退还** ⇒ 那个友方**不该**疲劳"
                + "（判据：`PlayAbility.c` 的 `0xed` 支传 `dontCountAttack = 1`）");
        Check(atk.AttacksThisTurn, 0,
              "★ （灭自证）它的本回合攻击次数**回到 0** —— 退还真的发生了"
            + "｜🧨 把退还那段删掉 ⇒ 上一行与本行一起红");
    }

    /// <summary>🔴🆕 **2026-10-25（引擎线 `A3` · `C5` Q1）：`talent`（`AbilityTrigger.Talent = 46`）
    /// 回合开始那一拍**从「完全没有事件」变成「发一条 `EvtKind.Trigger("Talent")`」**。
    ///
    /// <para>**判据**（五跳全有出处，逐句 → `资料/普查产出_1024c/C5_引擎战斗剩余判据.md` §Q1）：
    /// `CardScript.OnTurnStart.c:160`（`HasCurrentTrait(0x34d)`) → `:167 OnTrigger(0x2e = 46)` →
    /// `RawCardScript.TriggerAbility.c:60 PlayTriggerTraitAnim` → `BattleAction 0x44` →
    /// `CardScript.ActivateTriggerTraitAnim` → `GetTraitFromAbilityTrigger(46)` = `DefinedTrait.talent(845)`
    /// → `BattleCardUI.DisplayTriggerAnim(845)`。落点 = `RuleCore.SpawnTalents`（造卡**之前**发，
    /// 手牌满造不成时动画照样演）。</para>
    ///
    /// <para>🔴 **`regeneration` 不在这条路上**（**原版本身就没有**，铁律 11 例外①）：
    /// `AbilityTrigger` 枚举 97 条里**没有** `Regeneration` ⇒ `GetTraitFromAbilityTrigger` 走
    /// name-parse 永远返回 0；`CardScript.OnTurnEnd.c:84-107` 对它是**直接 `HealOneCharacter`**、
    /// 没有 `OnTrigger`；`DisplayTriggerAnim` 全 build 只有 9 个调用点、**没有一处传 1030**。
    /// 同族「没有 AbilityTrigger」的 trait 名（**只记账、不造触发**）：
    /// 🔴 **2026-10-25（执行代理 `X3`）就地订正**：原来这里写的是
    /// `concussive` · `frenzied` · `huntMark` · `large` · `mekaniak` · `stomp` —— **多了两个**。
    /// 正确名单（三支全 ✗）见 `RuleEngine/Core/CardDef.cs` 的 `TraitReachability`：
    /// `regeneration` · `concussive` · `frenzied` · `large` · `mekaniak`。
    /// · `huntMark` **可达**：`traitEffect.triggerType ∈ {0,2}` ⇒
    ///   `ActivateTraitsOnSummonOrEnchantment.c:67` 走 `DisplayStatusAnim`（判据 → `待办判据_引擎_1025b` §六）；
    /// · `stomp` **机制可达**：`BattleManager__ShouldTriggerStompDamage`（`0x4d8`）。
    /// ⚠️ `bloodThirst` **不在**这一组（`CardScript__HighlightBloodThirst.c:8` 直接传 220 ⇒ 可播）；
    /// `resolution` 也**不在**（`AbilityTrigger.Resolution = 45` 存在，而且 `CardScript__OnTurnEnd.c:81`
    /// **真的会发** `OnTrigger(0x2d)`）—— 这两条是本轮现核 `AbilityTrigger.cs` 时对 `C5` §Q1
    /// 那个「9 个」名单的**就地订正**（铁律 5）。</para>
    /// </summary>
    static void C5Q1TalentTurnStartTrigger()
    {
        // ============ ① 前提：契约面的三条（先证「仪器是好的」，再证「行为」） ============
        Check(CardDef.AbilityTriggerNumber("Talent"), 46,
              "（前提）`Talent` 在原版 `AbilityTrigger` 里 = **46**｜🧨 改 `AbilityTriggerNumberOfName` 会让本行红");
        Check(CardDef.AbilityTriggerNumber("regeneration"), -1,
              "（前提）`regeneration` **没有** `AbilityTrigger`（⇒ `GetTraitFromAbilityTrigger` 永远解不出 1030）"
            + "｜🧨 哪天给 `regeneration` 加了一条 AbilityTrigger（= 原版有触发），这条会红 ⇒ 那时才该造它的事件");

        var pool = CardDatabase.Load();
        CardDef tal = null, noTal = null;
        foreach (var c in pool)
        {
            if (c == null) continue;
            if (c.Id == "SOR9") tal = c;        // `Preacher`：`Talent: Hymn of Battle`，`abilityTriggers = [46, 605]`
            if (c.Id == "DA25") noTal = c;      // `Deathwing Knight`：`abilityTriggers = [690]`（**没有** 46）
        }
        CheckTrue(tal != null, "（前提）池里有 `SOR9`（带天赋的单位；按 **id** 找，⛔ 不按卡名）");
        CheckTrue(noTal != null, "（前提）池里有 `DA25`（不带天赋的对照单位）");
        if (tal == null || noTal == null) return;

        CheckTrue(tal.TalentName != null,
                  "（前提）`SOR9` 的 `TalentName` 非空（= 引擎那条「这张卡有天赋」的判据认得它）"
                + "｜🧨 这是 `SpawnTalents` 那道闸的一半");
        CheckTrue(noTal.TalentName == null, "（前提）`DA25` 没有天赋名");

        // 🔴 **灭自证（第一半）**：语义闸 = 真、**表现闸 = 假** —— 两者必须相反，
        //    否则「引擎只是照着 `animTimes` 有行才发」这条解释就成立，本条的因果就说不清。
        CheckTrue(RuleCore.HasTriggerSourceForTest(tal, "Talent"),
                  "★ 语义闸：`SOR9` 的 `abilityTriggers` 含 46 ⇒ `HasTriggerSource(\"Talent\")` = **真**"
                + "｜🧨 把 presence 闸改回「只看动画行」⇒ 本行红");
        CheckTrue(!RuleCore.HasAnimRowOnlyForTest(tal, "Talent"),
                  "🧨🧨 **灭自证**：`SOR9` 的 `animTimes` 里**没有** `Talent` 行 ⇒ 表现闸 = **假**。"
                + "上面那条真、这条假，结构上排除了「有动画行才发」那一版实现");

        // ============ ② 行为：真跑一局，P0 的下一个回合开始发**恰好一条** ============
        var ctx = BattlePool(new CardDef[0], new CardDef[0], pool, "Test", 11);
        var uTal = Place(ctx, 0, 3, tal);
        var uNo = Place(ctx, 0, 5, noTal);
        ToP1Turn(ctx, 1);                 // 推到 P0 行动（`ctx.Active == 0`）
        ctx.ClearSignals();               // ⛔ 不清会把「摆上去之前那一拍」混进来
        PassTurn(ctx); PassTurn(ctx);     // → P1 回合开始 → 再回到 P0 的回合开始（= 被测那一拍）

        int nTal = 0, nNo = 0;
        foreach (var e in ctx.Signals)
        {
            if (e == null || e.Kind != EvtKind.Trigger || e.Keyword != "Talent") continue;
            if (e.InstanceId == uTal.InstanceId) nTal++;
            if (e.InstanceId == uNo.InstanceId) nNo++;
        }
        Check(nTal, 1,
              "★★ `talent` 那一拍：P0 回合开始 ⇒ `SOR9` 发**恰好一条** `EvtKind.Trigger(\"Talent\")`"
            + "（原版 `AbilityTrigger.Talent = 46`）"
            + "｜🧨 删掉 `SpawnTalents` 里那句 `EmitTalentTrigger` ⇒ 本行实得 0 ⇒ 红；"
            + "把它挪到「造卡成功之后」⇒ 手牌满那一路会漏（见下一条的前提）");
        Check(nNo, 0,
              "🧨🧨 **灭自证（第二半）**：同一局、同一拍，**不带天赋**的 `DA25` 一条都不发"
            + "（`abilityTriggers = [690]`，presence 对 `Talent` = 0 ⇒ 原版那一格**全静默**）"
            + "｜这一对结构上不可能同时满足「谁都发 / 谁都不发」两种错法");

        // ============ ③ 次序：手牌满 ⇒ 卡造不成，**事件照样发** ============
        // 原版 `OnTrigger(46)` 在 `_AddCardNotDrawnToHand` **之前**（判据同上五跳）。
        var ctx2 = BattlePool(new CardDef[0], new CardDef[0], pool, "Test", 11);
        var u2 = Place(ctx2, 0, 3, tal);
        ctx2.Players[0].Hand.Clear();                                  // 先清空，再塞到上限（10）
        for (int i = 0; i < 10; i++)        {
            var filler = Unit("C5HandFull" + i, 1, 0, 1);
            ctx2.Players[0].Hand.Add(ctx2.NewInstance(filler));
        }
        int handBefore = ctx2.Players[0].Hand.Count;
        ToP1Turn(ctx2, 1);
        ctx2.ClearSignals();
        PassTurn(ctx2); PassTurn(ctx2);
        int n2 = 0;
        foreach (var e in ctx2.Signals)
            if (e != null && e.Kind == EvtKind.Trigger && e.Keyword == "Talent"
                && e.InstanceId == u2.InstanceId) n2++;
        Check(n2, 1,
              "★ **次序**：手牌已满（造不出那张天赋卡）⇒ 触发事件**照样发**"
            + "（原版 `OnTrigger(46)` 排在 `_AddCardNotDrawnToHand` 之前）"
            + "｜🧨 把 `EmitTalentTrigger` 挪到「造卡成功之后」/ `continue` 之后 ⇒ 本行实得 0 ⇒ 红");
        CheckTrue(ctx2.Players[0].Hand.Count <= handBefore,
                  $"（前提）手牌真的满了 / 没有变多（{handBefore} → {ctx2.Players[0].Hand.Count}）"
                + " —— 否则上一条测的就不是「满手那一支」");
    }

    /// <summary>🆕 Q4 的合成 criteria 夹具：**每一格都显式给值**（`CountSetSpec` 的默认值是 `-1 = 未知`，
    /// 而求值器对「未知」一律 `null` —— 只给几格会让整条 criteria 判不了，测不到想测的那一维）。
    /// 默认 = 一条「己方全部部队/督军都能过」的 criteria；各用例按需覆盖。</summary>
    static CardDef.CountSetSpec C5Set(Action<CardDef.CountSetSpec> init)
    {
        var s = new CardDef.CountSetSpec
        {
            PlayerTurn = CardDef.CountSetSpec.Unknown,   // 这两列结构上没有 `playerTurn`（数据恒 -1）
            TargetsAffected = 10,                        // allFiltered
            ChosenArmy = 0, ArmyFilter = 0, KeywordFilter = 0, TraitsFilter = 0,
            MaxTargets = 0, TargetSelectMethod = 0,
            AttackFilter = 0, AttackFilterOption = 0,
            CurrentHealthFilter = 0, CurrentHealthFilterOption = 0,
            ManaCostFilter = 0, ManaCostFilterOption = 0,
            RarityFilter = 0, RarityFilterOption = 0,
            SpellType = 0,                               // 0 = Normal = 不筛
            FriendlyUnits = 1, EnemyUnits = 1,
            IncludeHero = 1, IncludeMinions = 1, IncludeSpells = 1, IncludeWhispers = 1,
            IncludeWeapons = 0, IncludeRemnants = 1, IncludeHeroParts = 0,
            IncludeSelf = 1, ExcludeNotSelf = 0,
            ExcludeSimilarCards = 0, ExcludeTargetted = 0, ExcludeFriendlyHero = 0,
            ExcludeEnemyHero = 0, ExcludeRace = 0, ExcludeTrait = 0,
            SelectFromChooseResolution = 0,
            FilterSpecificCardIds = new string[0], ExcludeEnchantmentIds = new string[0],
        };
        if (init != null) init(s);
        return s;
    }

    // ==============================================================================================
    //  Q2 —— `HasTriggerSource` 的闸次序：**ability presence 优先**（原版 `CanTriggerAbility` 第一句）
    // ==============================================================================================

    static void C5Q2TriggerGateOrder()
    {
        // ---- (a) 全池不变式：凡有动画行 `tn = T` 的卡 ⇒ 这张卡**真有**这条 ability --------------
        //  判据 = `CardAbility__CanTriggerAbility.c:46-48`：动画只在 `CanTriggerAbility` 返回 true
        //  之后才播（`TriggerAbility.c:60`）⇒「有动画行、没有 ability」在原版是**绝对静默**。
        //  今天 1,127 张逐卡逐行都成立（`C5` Q2 的 41 名总表 `aOnly` 列全 0）。
        {
            var pool = CardDatabase.Load();
            int animRows = 0, violations = 0;
            string firstBad = null;
            foreach (var c in pool)
            {
                if (c == null) continue;
                var anims = c.AnimTimes;
                for (int i = 0; i < anims.Count; i++)
                {
                    var a = anims[i];
                    if (a == null || string.IsNullOrEmpty(a.TriggerName)) continue;
                    animRows++;
                    if (c.AbilityTriggerPresence(a.TriggerName) != 1)
                    {
                        violations++;
                        if (firstBad == null) firstBad = c.Id + "/" + a.TriggerName;
                    }
                }
            }
            CheckTrue(animRows > 0, "Q2(a) 前提：全池至少有一行动画（否则本不变式是空转）");
            Check(violations, 0,
                  $"★ Q2(a) **全池不变式**：凡 `animTimes[].tn = T` 的卡 ⇒ `AbilityTriggerPresence(T) == 1`"
                + $"（现读 {animRows} 行动画；违规 {violations}"
                + (firstBad != null ? $"，首个 {firstBad}" : "") + "）"
                + "｜🧨 数据里出现「有动画行、没 ability」的卡 ⇒ 本行红"
                + "（那正是 Q2 要修的差集；今天 = 0）");
        }

        // ---- (b) 合成夹具：**有 ability、没动画行** ⇒ 语义闸 `true` / 表现闸 `false` --------------
        {
            var abilityOnly = new CardDef("C5AbilityOnly", "C5AbilityOnly", "unit", "", null, "Test", 0, 1, 1, 0, null,
                                          abilityTriggers: new[] { 580 },          // Strike
                                          abilityTriggersVersioned: true);
            CheckTrue(RuleCore.HasTriggerSourceForTest(abilityOnly, "Strike"),
                      "★★ Q2(b) **有 ability（Strike 580）、没有动画行** ⇒ 语义闸 **true**（照发事件）"
                    + " —— 判据 `CardAbility__CanTriggerAbility.c:46-48`（presence 是第一道闸）"
                    + "｜🧨 把 `HasTriggerSource` 写成「必须同时有动画行」⇒ 实得 false ⇒ 红");
            CheckTrue(!RuleCore.HasAnimRowOnlyForTest(abilityOnly, "Strike"),
                      "★★ Q2(b) 同一张卡 **表现闸 = false**（没动画行 ⇒ 不发 `EvtKind.Trigger`）"
                    + "—— 与上一条**互为灭自证**（同一输入，一真一假才说明两个闸真的分开了）"
                    + "｜🧨 让 `HasAnimRowOnly` 改读 presence ⇒ 实得 true ⇒ 红");
        }

        // ---- (c) 合成夹具：**有动画行、没有 ability** ⇒ 两条闸 = 语义 false / 表现 true -----------
        {
            var animOnly = new CardDef("C5AnimOnly", "C5AnimOnly", "unit", "", null, "Test", 0, 1, 1, 0, null,
                                       abilityTriggers: new int[0],             // 明确没有 ability
                                       abilityTriggersVersioned: true,
                                       animTimes: new[]
                                       {
                                           new CardDef.CardAnimTime { TriggerName = "Strike", Trigger = 580, Anim = "x" },
                                       });
            CheckTrue(!RuleCore.HasTriggerSourceForTest(animOnly, "Strike"),
                      "★★ Q2(c) **有动画行、没有 ability** ⇒ 语义闸 **false**（原版绝对静默）"
                    + "｜🧨 把次序改回「先看动画行」⇒ 实得 true ⇒ 红（**这就是 Q2 修的那个差集**）");
            CheckTrue(RuleCore.HasAnimRowOnlyForTest(animOnly, "Strike"),
                      "Q2(c) 表现闸照旧 = true（它只问有没有动画行）—— 与上一条互为灭自证");
        }

        // ---- (d) 旧表 / 合成夹具（`abilityTriggersVersioned = false` ⇒ presence = -1）⇒ 回退旧口径 ----
        {
            var legacyAnim = new CardDef("C5LegacyAnim", "C5LegacyAnim", "unit", "", null, "Test", 0, 1, 1, 0, null,
                                         animTimes: new[]
                                         {
                                             new CardDef.CardAnimTime { TriggerName = "Strike", Trigger = 580, Anim = "x" },
                                         });
            Check(legacyAnim.AbilityTriggerPresence("Strike"), -1,
                  "Q2(d) （前提）没标 versioned ⇒ `AbilityTriggerPresence` = **-1（未知）**，⛔ 不是 0");
            CheckTrue(RuleCore.HasTriggerSourceForTest(legacyAnim, "Strike"),
                      "Q2(d) `-1` + 有动画行 ⇒ 回退旧口径 **true**（改前行为逐字不变）"
                    + "｜🧨 把 `-1` 读成 0 ⇒ 实得 false ⇒ 红（那会让**所有合成夹具**不再发 Trigger）");
            var legacyNoAnim = new CardDef("C5LegacyNoAnim", "C5LegacyNoAnim", "unit", "", null, "Test", 0, 1, 1, 0, null);
            CheckTrue(!RuleCore.HasTriggerSourceForTest(legacyNoAnim, "Strike"),
                      "Q2(d) `-1` + 没有动画行 ⇒ 旧口径 **false**（与改前一致）");
        }
    }

    // ==============================================================================================
    //  Q3 —— `EnqueueAbility` 的入队闸覆盖 `Hand(10) / Deck(20) / Cemetery(30)`
    // ==============================================================================================

    static void C5Q3ZoneContextGate()
    {
        // ---- ① `Hand(10)`：**在手里** ⇒ 入队；**不在手里** ⇒ 入队被拒（改前是「静默放行」）------
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var card = Unit("C5HandListener", 1, 1, 3);
            var inst = ctx.NewInstance(card);
            ctx.Players[0].Hand.Add(inst);
            var carrier = new UnitState(inst, false);            // 载体：不在棋盘上（按定义）
            int ran = 0;

            bool inHand = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "q3-hand-in", () => ran++,
                                                  actor: carrier,
                                                  abilityContext: CardDef.AbilityContextHand);
            CheckTrue(inHand,
                      "★★ Q3① `abilityContext=Hand(10)` + 载体**在手里** ⇒ 入队成功"
                    + "｜🧨 闸写成「载体不在棋盘 ⇒ 一律拒」⇒ 实得 false ⇒ 红");
            RuleCore.PumpActionQueues(ctx);
            Check(ran, 1, "Q3① 在手里的那一条**正常结算**（防「区域档恒拒」的假绿）");

            ctx.Players[0].Hand.Remove(inst);                    // 现在不在任何区域里
            bool notInHand = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "q3-hand-out", () => ran++,
                                                     actor: carrier,
                                                     abilityContext: CardDef.AbilityContextHand);
            CheckTrue(!notInHand,
                      "★★ Q3① `Hand(10)` + 载体**不在手里** ⇒ **入队被拒**（`IsRightContext` 的 `InZone` 判假）"
                    + "｜🧨 恢复成改前的「`co < 0` 就跳过判」⇒ 实得 true ⇒ 红"
                    + "（`C5` Q3 点名的 17 行：`Hand` 16 · `Cemetery` 1，改前**永远不过闸**）");

            // 反向（灭自证）：`-1`（未知）⇒ **一个字都不判**，照旧入队。
            bool unknown = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "q3-unknown", () => ran++,
                                                   actor: carrier,
                                                   abilityContext: CardDef.AbilityContextUnknown);
            CheckTrue(unknown,
                      "★★ Q3① `abilityContext = -1`（未知）⇒ **不判**、照旧入队"
                    + "｜🧨 把 `-1` 也拿去 `IsRightContext` 判（它返回 true，看不出问题）"
                    + "——本条真正防的是「顺手把所有入队都拦一遍」的写法；与上一条互为灭自证");
        }

        // ---- ② `Cemetery(30)`：在弃牌堆里 ⇒ 入队；不在 ⇒ 拒 --------------------------------
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var card = Unit("C5GraveListener", 1, 1, 3);
            var inst = ctx.NewInstance(card);
            ctx.Players[1].Discard.Add(inst);                    // 对手的弃牌堆（`InZone(-1,-1)` 两边都扫）
            var carrier = new UnitState(inst, false);
            bool ok = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "q3-grave-in", () => { },
                                              actor: carrier,
                                              abilityContext: CardDef.AbilityContextCemetery);
            CheckTrue(ok, "★★ Q3② `Cemetery(30)` + 载体在**对手的弃牌堆**里 ⇒ 入队成功"
                        + "（`InZone` 的 `owner < 0` 会两边扫 —— 判据 `ActionPump.IsRightContext` 的 doc）"
                        + "｜🧨 只查 `Players[0]` ⇒ 实得 false ⇒ 红");
            ctx.Players[1].Discard.Remove(inst);
            bool no = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "q3-grave-out", () => { },
                                              actor: carrier,
                                              abilityContext: CardDef.AbilityContextCemetery);
            CheckTrue(!no, "★★ Q3② `Cemetery(30)` + 载体不在任何弃牌堆 ⇒ **入队被拒**"
                         + "｜🧨 跳过判 ⇒ 实得 true ⇒ 红");
        }

        // ---- ③ `Play(0)`：载体**不在棋盘上**（活着、也没在任何区域）⇒ 拒（改前静默放行）--------
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var inst = ctx.NewInstance(Unit("C5Ghost", 1, 1, 3));
            var ghost = new UnitState(inst, false);              // 谁都不认识它
            bool ok = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "q3-play-offboard", () => { },
                                              actor: ghost,
                                              abilityContext: CardDef.AbilityContextPlay);
            CheckTrue(!ok,
                      "★★ Q3③ `Play(0)` + 载体**不在任何一方棋盘上** ⇒ **入队被拒**"
                    + "（原版 `IsInPlay` 判假；`C5` Q3 的可观测表里这一格写的是「理论上可观测、今天哪几个调用点会这样没查」）"
                    + "｜🧨 跳过 `co < 0`（改前口径）⇒ 实得 true ⇒ 红");
        }

        // ---- ④ W63 口径的灭自证：入队**之后**才离场 ⇒ 泵点**照跑**（原版不取消）---------------
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var card = Unit("C5LateLeave", 1, 1, 3);
            var inst = ctx.NewInstance(card);
            ctx.Players[0].Hand.Add(inst);
            var carrier = new UnitState(inst, false);
            int ran = 0;
            bool ok = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "q3-late", () => ran++,
                                              actor: carrier,
                                              abilityContext: CardDef.AbilityContextHand);
            CheckTrue(ok, "Q3④ （前提）入队那一刻**还在手里** ⇒ 入队成功");
            ctx.Players[0].Hand.Remove(inst);                    // 入队之后才移走
            RuleCore.PumpActionQueues(ctx);
            Check(ran, 1,
                  "★★ Q3④ 入队**之后**才离手 ⇒ 泵点**照跑**（与原版一致：`CancelEffectsFromDisappearingUnit` "
                + "的调用点里**没有** `recallToHand` / 死亡）"
                + "｜🧨 在泵点补一道 `IsRightContext` 复核 ⇒ 实得 0 ⇒ 红 —— 那正是 `W63 #2`（Slay 正文丢失）的成因");
        }
    }

    // ==============================================================================================
    //  Q4 —— v25 `targetCriteria` 36 格的消费端（join / 求值 / 端到端）
    // ==============================================================================================

    /// <summary>Q4 子用例的**结果断言**：把 `bool?` 的三种取值分开断（`true` / `false` / `null` 是**三件事**）。</summary>
    static void C5Want(BattleContext ctx, int owner, CardDef.CountSetSpec set, UnitState cand,
                       CardDef.ConditionSpec cond, UnitState thisCard, UnitState targetCard,
                       bool? want, string msg)
    {
        string gap;
        bool? got = TargetCriteriaEval.Matches(ctx, owner, set, cand, cond, thisCard, targetCard, out gap);
        string gotText = msg + "（现读 " + (got.HasValue ? got.Value.ToString() : "null") + "）";
        if (want == null) CheckTrue(got == null, gotText);
        else if (want.Value) CheckTrue(got == true, gotText);
        else CheckTrue(got == false, gotText);
        if (want == null)
            CheckTrue(!string.IsNullOrEmpty(gap), "（上一条要求 `null` ⇒ 必须给出 gap 文案，⛔ 不许空着）");
    }

    static void C5Q4TargetCriteriaConsumer()
    {
        var pool = CardDatabase.Load();

        // ---- (a) 数据面：两列真的进 `CardDef` 了（行数 = `E9` 的现读口径）---------------------
        {
            int atcCards = 0, atcRows = 0, aatCards = 0, aatRows = 0;
            foreach (var c in pool)
            {
                if (c == null) continue;
                int rows = 0;
                for (int ab = 0; ab < 16; ab++)
                    for (int lg = 0; lg < 32; lg++)
                        if (c.AbilityTargetCriteriaAt(ab, lg) != null) rows++;
                if (rows > 0) { atcCards++; atcRows += rows; }

                int arows = 0;
                for (int ab = -2; ab <= -1; ab++)
                    for (int lg = 0; lg < 32; lg++)
                        if (c.ActiveAbilityTargetCriteriaAt(ab, lg) != null) arows++;
                if (arows > 0) { aatCards++; aatRows += arows; }
            }
            CheckTrue(pool.Count > 0 && pool[0].AbilityTargetCriteriaVersioned,
                      "Q4(a) 前提：卡表 ≥ v25（`AbilityTargetCriteriaVersioned` 为真）");
            // 🔴 2026-10-25（`X20`）：1616/1008 → **1619/1009**（池内新增的附魔卡自带 2 条 ability / 3 条
            //   logic，`targetCriteria` 各落 1 行 ⇒ +3 行 / +1 张）。
            Check(atcRows, 1619,
                  $"★ Q4(a) `abilityTargetCriteria` 进 `CardDef` 的行数 = **1619**（现读 {atcRows}；"
                + "`C5`/`E9` 口径：1009 张 / 1619 行）｜🧨 少一行 = 某个 `(ab,lg)` 键被合并/丢了");
            Check(atcCards, 1009,
                  $"★ Q4(a) 带行的卡数 = **1009**（现读 {atcCards}）"
                + "｜🧨 `MapTargetCriteriaRows` 把某批行整批丢掉（比如 `lg` 校验写反）⇒ 红");
            Check(aatRows, 193,
                  $"★ Q4(a) `activeAbilityTargets` 行数 = **193**（现读 {aatRows}；132 张）"
                + "｜🧨 主动那一路的 `ab = -1/-2` 被当成非法值丢掉 ⇒ 红");
            Check(aatCards, 132, $"★ Q4(a) 主动那一路带行的卡数 = **132**（现读 {aatCards}）");
            CheckTrue(pool[0].AbilityTargetCriteriaAt(-1, 0) == null,
                      "Q4(a) `AbilityTargetCriteriaAt(-1, 0)` = **null**（active/alternative 在**另一列**）"
                    + "｜🧨 两列混存 ⇒ 这里会拿到东西 ⇒ 红");
        }

        // ---- (a2) 覆盖读数：真的有多少 op 挂上了（⛔ 不许「以为 1008 张全接上了」）---------------
        //  覆盖受 G1/G2 两道闸限制（块数 == ability 数、块内 op 数 == logic 数）⇒ **一定 < 1008 张**。
        //  这里只断言「> 0」并把**确切数**打进日志：数字要让下一个人看得见（本工程「不许静默」）。
        {
            int withOps = 0, opsAttached = 0;
            string sample = null;
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Desc)) continue;
                var ops = EffectText.Parse(c.Desc, out _, out _, card: c);
                int n = 0;
                for (int i = 0; i < ops.Count; i++)
                    if (ops[i] != null && ops[i].TargetCriteriaSet != null) n++;
                if (n > 0)
                {
                    withOps++;
                    opsAttached += n;
                    if (sample == null) sample = c.Id + "×" + n;
                }
            }
            UnityEngine.Debug.Log(P + $"[Q4 覆盖] 挂上 36 格 criteria 的 op = **{opsAttached} 条 / {withOps} 张卡**"
                                     + $"（首个 {sample ?? "(无)"}；分母 = 1008 张带行的卡 —— "
                                     + "差集是 G1/G2 闸没过的那些，不是缺陷，⛔ 但也别当成「全接上了」）");
            CheckTrue(opsAttached > 0,
                      "★★ Q4(a2) **至少有一个 op 真的挂上了 36 格 criteria**"
                    + "｜🧨 `AssignAbilityTargets` 那一跳删掉 / 闸写反 ⇒ 实得 0 ⇒ 红");
        }

        // ---- (b) 求值器逐维（合成夹具；判据 = `FilterMethods__CheckIfMeetsCriteria.c`）----------
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var ownVeh = Place(ctx, 0, 1, new CardDef("C5OwnVeh", "C5OwnVeh", "unit", "", null, "Ultramarines",
                                                      0, 1, 3, 0, null, subtype: "Vehicle"));
            var ownInf = Place(ctx, 0, 2, new CardDef("C5OwnInf", "C5OwnInf", "unit", "", null, "Ultramarines",
                                                      0, 1, 3, 0, null, subtype: "Infantry"));
            var foeVeh = Place(ctx, 1, 1, new CardDef("C5FoeVeh", "C5FoeVeh", "unit", "", null, "Goff",
                                                      0, 1, 3, 0, null, subtype: "Vehicle"));
            var ownHero = ctx.Players[0].Warlord;

            // ta = any(0) ⇒ 「其余 35 格全失效」（`:57-59`）
            var anySet = C5Set(s =>
            {
                s.TargetsAffected = 0; s.FriendlyUnits = 0; s.EnemyUnits = 0;
                s.IncludeHero = 0; s.IncludeMinions = 0; s.KeywordFilter = 10;
            });
            C5Want(ctx, 0, anySet, ownInf, null, null, null, true,
                   "★ Q4(b) `targetsAffected = 0 (any)` ⇒ **恒真**（原版「Any disregards all other filters」）"
                 + "｜🧨 先跑 36 格再判 ta ⇒ 这张 Infantry 会被 `keywordFilter=Vehicle` 筛掉 ⇒ 红");

            // 阵营闸：friendlyUnits / enemyUnits（`:210-232`）
            var sideSet = C5Set(s => { s.TargetsAffected = 10; s.FriendlyUnits = 1; s.EnemyUnits = 0; });
            C5Want(ctx, 0, sideSet, ownVeh, null, null, null, true, "Q4(b) `friendlyUnits=1` ⇒ 己方单位过");
            C5Want(ctx, 0, sideSet, foeVeh, null, null, null, false,
                   "★★ Q4(b) `enemyUnits=0` ⇒ **敌方单位被淘汰**"
                 + "｜🧨 不看 friendly/enemy 两格 ⇒ 实得 true ⇒ 红");

            // 卡种白名单：includeMinions / includeHero（`:280-293`）
            var noHero = C5Set(s => { s.TargetsAffected = 10; s.IncludeHero = 0; s.IncludeMinions = 1; });
            C5Want(ctx, 0, noHero, ownHero, null, null, null, false,
                   "★★ Q4(b) `includeHero = 0` ⇒ **督军被淘汰**"
                 + "｜🧨 卡种白名单不判 ⇒ 实得 true ⇒ 红（`includeMinions=1` 的卡会误伤督军）");
            C5Want(ctx, 0, noHero, ownVeh, null, null, null, true,
                   "Q4(b) 同一份 criteria 对**部队**放行（与上一条互为灭自证）");

            // keywordFilter + excludeRace（`:304-313`）
            var vehSet = C5Set(s => { s.TargetsAffected = 10; s.KeywordFilter = 10; });   // Keyword.Vehicle
            C5Want(ctx, 0, vehSet, ownVeh, null, null, null, true,
                   "Q4(b) `keywordFilter=10 (Vehicle)` + 候选是 Vehicle ⇒ 过");
            C5Want(ctx, 0, vehSet, ownInf, null, null, null, false,
                   "★★ Q4(b) 同一份 criteria + 候选是 Infantry ⇒ **淘汰**"
                 + "｜🧨 认不出 `Keyword.Vehicle` 就静默放行 ⇒ 实得 true ⇒ 红");
            var notVehSet = C5Set(s =>
            {
                s.TargetsAffected = 10; s.KeywordFilter = 10; s.ExcludeRace = 1;   // 有这个词就淘汰
            });
            C5Want(ctx, 0, notVehSet, ownVeh, null, null, null, false,
                   "★★ Q4(b) `excludeRace = 1` ⇒ **有** Vehicle 的候选被淘汰（方向取反）"
                 + "｜🧨 不取反 ⇒ 实得 true ⇒ 红");

            // includeRemnants（`:255-261`）
            var remnant = Place(ctx, 0, 3, new CardDef("C5Remnant", "C5Remnant", "unit", "", null, "Ultramarines",
                                                      0, 1, 1, 0, null, subtype: "Infantry"));
            remnant.IsRemnant = true;
            var noRem = C5Set(s => { s.TargetsAffected = 10; s.IncludeRemnants = 0; });
            C5Want(ctx, 0, noRem, remnant, null, null, null, false,
                   "★★ Q4(b) `includeRemnants = 0` + 候选是残骸 ⇒ **淘汰**"
                 + "｜🧨 不判 `IsRemnant` ⇒ 实得 true ⇒ 红");
            C5Want(ctx, 0, noRem, ownInf, null, null, null, true,
                   "Q4(b) 同一份 criteria 对**非残骸**放行（灭自证）");

            // filterSpecificCards（`:465-483` 白名单，按卡 id）
            var onlyInf = C5Set(s =>
            {
                s.TargetsAffected = 10; s.FilterSpecificCardIds = new[] { ownInf.Card.Id };
            });
            C5Want(ctx, 0, onlyInf, ownInf, null, null, null, true,
                   "Q4(b) `filterSpecificCards` 白名单命中 ⇒ 过");
            C5Want(ctx, 0, onlyInf, ownVeh, null, null, null, false,
                   "★★ Q4(b) 同一份白名单 + 不在表里的卡 ⇒ **淘汰**"
                 + "｜🧨 把 `filterSpecificCards` 当「排除」或当空 ⇒ 实得 true ⇒ 红");

            // armyFilter（`FilterMethods__MeetsArmyCriteria.c` 逐值）
            var umOnly = C5Set(s => { s.TargetsAffected = 10; s.ChosenArmy = 10; s.ArmyFilter = 1; });
            C5Want(ctx, 0, umOnly, ownVeh, null, null, null, true,
                   "Q4(b) `armyFilter=1 (OnlyChosenArmy)` + 候选阵营 = Ultramarines ⇒ 过");
            C5Want(ctx, 0, umOnly, foeVeh, null, null, null, false,
                   "★★ Q4(b) 同一份 + Goff 的候选 ⇒ **淘汰**"
                 + "｜🧨 不判阵营 ⇒ 实得 true ⇒ 红");

            // conditionsFilter（`unitConditions` 的 cond/ex）—— 由调用方拼进来
            var damagedCond = new CardDef.ConditionSpec { Condition = 40, Exclude = false };   // `damaged`
            var condSet = C5Set(null);
            C5Want(ctx, 0, condSet, ownInf, damagedCond, null, null, false,
                   "★★ Q4(b) 拼进来的 `cond=40 (damaged)` + 满血候选 ⇒ **淘汰**"
                 + "｜🧨 求值器不看 `cond` ⇒ 实得 true ⇒ 红（那正是「36 格与 cond/ex 拼回一个对象」那一跳）");
            ownInf.Health = 1;
            C5Want(ctx, 0, condSet, ownInf, damagedCond, null, null, true,
                   "Q4(b) 同一份 + **已受伤**的候选 ⇒ 过（灭自证）");

            // ---- ⑨ 四组 `MeetsStatCriteria`（`:318-375`；🆕 2026-10-24 `W19` 落地）-----------------
            //  判据 = `FilterMethods__MeetsStatCriteria.c` 逐句 + 签名桩
            //  `MeetsStatCriteria(int cardStat, int thisCardStat, int statFilter, StatFilterOptions, int altValue = 0)`。
            //  夹具卡的数**显式给死**：`cost=3 · 近战 2 · 生命 4 · 远程 5 · rarity=rare(=2)`。
            //  （槽 4 是督军位 `BoardSpec.WarlordSlot`，⛔ 别占。）
            var statHost = Place(ctx, 0, 5,
                new CardDef("C5StatHost", "C5StatHost", "unit", "", "rare", "Ultramarines",
                            3, 2, 4, 5, null, subtype: "Infantry"));

            // ── 攻 · `EqualOrHigher(2)` = `statFilter <= cardStat` ──
            C5Want(ctx, 0, C5Set(s => { s.AttackFilter = 2; s.AttackFilterOption = 2; }), statHost,
                   null, null, null, true,
                   "★ Q4⑨攻 `attackFilterOption=2 (EqualOrHigher)` + `attackFilter=2` + 近战 2 ⇒ 过"
                 + "（**边界：等号算过** —— `MeetsStatCriteria.c:13` 是 `param_3 <= param_1`）");
            C5Want(ctx, 0, C5Set(s => { s.AttackFilter = 3; s.AttackFilterOption = 2; }), statHost,
                   null, null, null, false,
                   "★★ Q4⑨攻 同一档 + `attackFilter=3` + 近战 2 ⇒ **淘汰**（2 >= 3 不成立）"
                 + "｜🧨 比较符写反（`cardStat <= statFilter`）⇒ 实得 true ⇒ 红");
            C5Want(ctx, 0, C5Set(s => { s.AttackFilter = 2; s.AttackFilterOption = 1; }), statHost,
                   null, null, null, true,
                   "Q4⑨攻 `EqualTo(1)` + 2 == 2 ⇒ 过（等号边界）");
            C5Want(ctx, 0, C5Set(s => { s.AttackFilter = 1; s.AttackFilterOption = 1; }), statHost,
                   null, null, null, false,
                   "★★ Q4⑨攻 `EqualTo(1)` + 2 != 1 ⇒ **淘汰**｜🧨 `EqualTo` 写成 `<=` ⇒ 实得 true ⇒ 红");

            // ── 血 · `EqualOrLower(3)` = `cardStat <= statFilter` ──
            C5Want(ctx, 0, C5Set(s => { s.CurrentHealthFilter = 4; s.CurrentHealthFilterOption = 3; }), statHost,
                   null, null, null, true,
                   "★ Q4⑨血 `EqualOrLower(3)` + 生命 4 <= 4 ⇒ 过（等号边界）");
            C5Want(ctx, 0, C5Set(s => { s.CurrentHealthFilter = 3; s.CurrentHealthFilterOption = 3; }), statHost,
                   null, null, null, false,
                   "★★ Q4⑨血 同一档 + `currentHealthFilter=3` + 生命 4 ⇒ **淘汰**"
                 + "｜🧨 拿 `MaxHealth`/卡面基础值顶 `Health` ⇒ 下一条会红");
            int statHostMax = statHost.MaxHealth;
            statHost.Health = 2;                                  // 打掉 2 点（上限仍是 4）
            C5Want(ctx, 0, C5Set(s => { s.CurrentHealthFilter = 3; s.CurrentHealthFilterOption = 3; }), statHost,
                   null, null, null, true,
                   "★★ Q4⑨血 **读数取「当前值」**：生命打到 2（`MaxHealth` 仍 4）后 `<=3` 成立"
                 + "｜🧨 读 `MaxHealth`/卡面基础值 ⇒ 实得 false ⇒ 红（原版读 `+0x68` 的 `ObscuredInt`，"
                 + "`EntityScript__get_currentHealth.c:5-10`）");
            statHost.Health = statHostMax;

            // ── 费 · 池内现读 21 行，只用到 `EqualTo(1)` / `EqualOrHigher(2)` / `EqualOrLower(3)` ──
            C5Want(ctx, 0, C5Set(s => { s.ManaCostFilter = 3; s.ManaCostFilterOption = 1; }), statHost,
                   null, null, null, true,
                   "★ Q4⑨费 `EqualTo(1)` + 费用 3 == 3 ⇒ 过（等号边界）");
            C5Want(ctx, 0, C5Set(s => { s.ManaCostFilter = 2; s.ManaCostFilterOption = 1; }), statHost,
                   null, null, null, false,
                   "★★ Q4⑨费 `EqualTo(1)` + 费用 3 != 2 ⇒ **淘汰**｜🧨 不判费用 ⇒ 实得 true ⇒ 红");
            C5Want(ctx, 0, C5Set(s => { s.ManaCostFilter = 3; s.ManaCostFilterOption = 2; }), statHost,
                   null, null, null, true,
                   "Q4⑨费 `EqualOrHigher(2)` + 3 >= 3 ⇒ 过");

            // ── 稀有度 · 池内现读 3 行只用 `EqualOrLower(3)` + `rarityFilter=3` ──
            C5Want(ctx, 0, C5Set(s => { s.RarityFilter = 3; s.RarityFilterOption = 3; }), statHost,
                   null, null, null, true,
                   "★ Q4⑨稀有度 `EqualOrLower(3)` + `rarityFilter=3` + 候选 `rare`(=2) ⇒ 过");
            var epicCard = Place(ctx, 0, 6,
                new CardDef("C5Epic", "C5Epic", "unit", "", "epic", "Ultramarines", 0, 1, 1, 0,
                            null, subtype: "Infantry"));
            C5Want(ctx, 0, C5Set(s => { s.RarityFilter = 3; s.RarityFilterOption = 3; }), epicCard,
                   null, null, null, true,
                   "Q4⑨稀有度 同一档 + 候选 `epic`(=3) ⇒ 过（等号边界）");
            var legCard = Place(ctx, 0, 7,
                new CardDef("C5Leg", "C5Leg", "unit", "", "legendary", "Ultramarines", 0, 1, 1, 0,
                            null, subtype: "Infantry"));
            C5Want(ctx, 0, C5Set(s => { s.RarityFilter = 3; s.RarityFilterOption = 3; }), legCard,
                   null, null, null, false,
                   "★★ Q4⑨稀有度 同一档 + 候选 `legendary`(=4) ⇒ **淘汰**"
                 + "｜🧨 稀有度映射错一档（`epic`/`legendary` 对调）⇒ 实得 true ⇒ 红"
                 + "（映射 = `CardRarity`: common 1 / rare 2 / epic 3 / legendary 4 / special 5）");

            // `NotApplicable(0)` ⇒ 本组**不参与**（原版 `if (param_4 != 0)` 看的是 **option**，不是 filter）
            C5Want(ctx, 0, C5Set(s => { s.AttackFilter = 99; s.AttackFilterOption = 0; }), statHost,
                   null, null, null, true,
                   "★★ Q4⑨ `attackFilterOption = 0 (NotApplicable)` + `attackFilter=99` ⇒ **本组不参与、放行**"
                 + "｜🧨 拿 `filter != 0` 当「这一组要用」的判据 ⇒ 实得 false ⇒ 红");

            // `RandomChance(8)` ⇒ 原版 switch 里**没有这一支** ⇒ 落到末尾 `return true`（**不掷骰**）
            C5Want(ctx, 0, C5Set(s => { s.AttackFilter = 99; s.AttackFilterOption = 8; }), statHost,
                   null, null, null, true,
                   "★★ Q4⑨ `attackFilterOption = 8 (RandomChance)` ⇒ **恒真**"
                 + "（`MeetsStatCriteria.c:10-30` 的 switch 无 `case 8` ⇒ `return bVar1 /* true */`）"
                 + "｜🧨 按枚举名补一次 RNG ⇒ 同一 seed 会出现两种结果（对局不可复现）⇒ 红");

            // `option = -1`（没落）⇒ **判不了**（`null` + 出声）。⛔ 不拿「原版对越界值 return true」顶。
            C5Want(ctx, 0,
                   C5Set(s => { s.AttackFilter = 1; s.AttackFilterOption = CardDef.CountSetSpec.Unknown; }),
                   statHost, null, null, null, null,
                   "★★ Q4⑨ `attackFilterOption = -1`（没落）⇒ **null（判不了）**"
                 + "｜🧨 拿原版「越界值 return true」顶 ⇒ 实得 true ⇒ 红（数据缺失不许冒充「不筛」）");

            // ---- ⑩ `excludeFriendlyHero` / `excludeEnemyHero`（`:244-272`）--------------------------
            //  原版判的是**候选**：`IsWarlordEquivalent(candidate)` = 候选 `cardType == 10 (Hero)`；
            //  再按「候选与载体是否同阵营」分别落到 friendly / enemy 那一格。
            var heroUnit = Place(ctx, 0, 0, Hero("C5Hero", 1, 3));
            var foeHero = Place(ctx, 1, 7, Hero("C5FoeHero", 1, 3));
            var exFriend = C5Set(s => { s.ExcludeFriendlyHero = 1; });
            C5Want(ctx, 0, exFriend, heroUnit, null, null, null, false,
                   "★★ Q4⑩ `excludeFriendlyHero=1` + 候选是**己方 Hero 类卡** ⇒ **淘汰**"
                 + "｜🧨 不判这两格（W2 的旧口径 = 未实现 ⇒ `null`）⇒ 实得 null ⇒ 红");
            C5Want(ctx, 0, exFriend, ownInf, null, null, null, true,
                   "Q4⑩ 同一份 + 候选是**己方普通部队** ⇒ 过（灭自证：判的是候选的卡种）");
            C5Want(ctx, 0, exFriend, foeHero, null, null, null, true,
                   "Q4⑩ `excludeFriendlyHero=1` + 候选是**敌方** Hero ⇒ 过（只管「友好」那一半）");
            var exEnemy = C5Set(s => { s.ExcludeEnemyHero = 1; });
            C5Want(ctx, 0, exEnemy, foeHero, null, null, null, false,
                   "★★ Q4⑩ `excludeEnemyHero=1` + 候选是**敌方 Hero** ⇒ **淘汰**"
                 + "｜🧨 两格写反（friendly/enemy 对调）⇒ 实得 true ⇒ 红");
            C5Want(ctx, 0, exEnemy, heroUnit, null, null, null, true,
                   "Q4⑩ 同一份 + 候选是**己方** Hero ⇒ 过（灭自证）");

            // ---- ⑪ `includeWeapons` / `includeHeroParts`：**原版一次都不读**（非 0 也不许改变结果）--
            var wpn = C5Set(s => { s.IncludeWeapons = 1; s.IncludeHeroParts = 1; });
            C5Want(ctx, 0, wpn, statHost, null, null, null, true,
                   "★★ Q4⑪ `includeWeapons=1` / `includeHeroParts=1` ⇒ **结果不变**（仍过）"
                 + "｜🧨 谁给这两格补一条筛 ⇒ 这里会红 —— 判据：原版两处卡种白名单只读 "
                 + "`+0x16/+0x17/+0x18/+0x19`（`CheckIfMeetsCriteria.c:280-293` / "
                 + "`CheckIfRawCardMeetsCriteria.c:30-34`），且 `CardTypeOptions` 只有 "
                 + "Minion/Hero/Tactic/Whispers 四值 ⇒ 没有任何卡种落得到这两格");

            // ---- ⑫ `traitsFilter = 680 (frenzied)`：卡面字是 `Destroyer` -----------------------------
            var destroyer = Place(ctx, 0, 8,
                new CardDef("C5Destroyer", "C5Destroyer", "unit", "", "common", "Ultramarines",
                            0, 1, 1, 0, new[] { "Destroyer" }, subtype: "Infantry"));
            var frenziedSet = C5Set(s => { s.TraitsFilter = 680; });
            C5Want(ctx, 0, frenziedSet, destroyer, null, null, null, true,
                   "★★ Q4⑫ `traitsFilter = 680 (frenzied)` + 候选带 `Destroyer` ⇒ 过"
                 + "（`KeywordTable.Prefixes` 有 `{ \"frenzied\", \"destroyer\" }` ⇒ `Normalize` 认得出；"
                 + "`DefinedTrait.frenzied = 680` 而卡面字是 `Destroyer`）"
                 + "｜🧨 旧口径（以为 `frenzied` 不在 `Prefixes` ⇒ 判不了）⇒ 实得 null ⇒ 红");
            C5Want(ctx, 0, frenziedSet, statHost, null, null, null, false,
                   "★★ Q4⑫ 同一份 + 候选**不带** `Destroyer` ⇒ **淘汰**"
                 + "｜🧨 把 `frenzied` 走成「判不了」⇒ null ⇒ 红（全池 5 行 `traitsFilter=680` 会白扔）");

            // `ta = 30 (target)` 要走「候选 == 玩家点选的目标」
            var targetSet = C5Set(s => { s.TargetsAffected = 30; });
            C5Want(ctx, 0, targetSet, ownVeh, null, null, null, null,
                   "Q4(b) `ta=30` + **没有点选目标** ⇒ `null`（判不了；原版那一格会抛异常）");
            C5Want(ctx, 0, targetSet, ownVeh, null, null, ownVeh, true,
                   "Q4(b) `ta=30` + 点选的正是它 ⇒ 过");
            C5Want(ctx, 0, targetSet, ownInf, null, null, ownVeh, false,
                   "★★ Q4(b) `ta=30` + 点选的是别人 ⇒ **淘汰**"
                 + "｜🧨 忽略 `targetCard` ⇒ 实得 true ⇒ 红");
        }

        // ---- (c) 端到端：真实卡 `ASH42 Dance of Death`（`Give Shuriken 2 to a friendly unit`）---------
        //  它的 v25 行 = `(ab=0, lg=0, ta=30 target)` ⇒ 原版语义 = 「目标就是**玩家点选的那一个**」
        //  （`CheckIfMeetsCriteria.c:30 target` 那一支：`候选 == targetCard`）。
        //  ⇒ 带 criteria + 点选 u2 ⇒ 池被筛成 `{u2}`；不带 criteria ⇒ 池的第一个（u1）。
        //  🔴 **这是 36 格消费端唯一能一眼看出差别的一档**（其余 36 格大多与卡面文字同义）。
        {
            CardDef ash42 = null;
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null && pool[i].Id == "ASH42") { ash42 = pool[i]; break; }
            CheckTrue(ash42 != null, "Q4(c) 前提：池里有 `ASH42 Dance of Death`");
            if (ash42 != null)
            {
                var ops = EffectText.Parse(ash42.Desc, out _, out _, card: ash42);
                EffectOp hit = null;
                for (int i = 0; i < ops.Count; i++)
                    if (ops[i] != null && ops[i].TargetCriteriaSet != null) { hit = ops[i]; break; }
                CheckTrue(hit != null,
                          "★★ Q4(c) 真卡解析出来的 op **挂上了 36 格 criteria**（`EffectOp.TargetCriteriaSet`）"
                        + "｜🧨 `AssignAbilityTargets` 那一跳删掉 ⇒ 实得 null ⇒ 红"
                        + "（覆盖口径：只在 G1/G2 成立、块内 op 数 == logic 数时挂）");
                if (hit != null)
                {
                    Check(hit.TargetCriteriaSet.TargetsAffected, 30,
                          "Q4(c) 挂上的那一份 `targetsAffected = 30 (target)`（`ASH42` 的数据）");

                    var ctx = Battle(new CardDef[0], new CardDef[0]);
                    var u1 = Place(ctx, 0, 1, new CardDef("C5E2eA", "C5E2eA", "unit", "", null, "Test",
                                                         0, 1, 3, 0, null, subtype: "Infantry"));
                    var u2 = Place(ctx, 0, 2, new CardDef("C5E2eB", "C5E2eB", "unit", "", null, "Test",
                                                         0, 1, 3, 0, null, subtype: "Infantry"));

                    var plain = RuleCore.ResolveTargets(ctx, 0, hit.Target, null, null);
                    Check(plain.Count, 1, "Q4(c) （对照）**不带 criteria** ⇒ 己方第一个（`a friendly unit` 只要 1 个）");
                    CheckTrue(plain.Count == 1 && object.ReferenceEquals(plain[0], u1),
                              "Q4(c) （对照）不带 criteria 时拿到的就是**池里第一个**（槽 1 的 `C5E2eA`）"
                            + "｜🧨 池序/裁剪规则改了 ⇒ 这一条先红（那是别的活，别混）");

                    var filtered = RuleCore.ResolveTargets(ctx, 0, hit.Target, null, u2,
                                                           hit.TargetCondition, hit);
                    Check(filtered.Count, 1,
                          "★★ Q4(c) 带 criteria（`ta=30`）+ **点选 u2** ⇒ 恰好 1 个");
                    CheckTrue(filtered.Count == 1 && object.ReferenceEquals(filtered[0], u2),
                              "★★ Q4(c) 带 criteria 时拿到的是**点选的那个 `C5E2eB`**（不是池里第一个）"
                            + "｜🧨 `ResolveTargets` 不调 `ApplyTargetCriteriaFilter` ⇒ 实得 `C5E2eA` ⇒ 红"
                            + "｜🧨 求值器忽略 `targetCard` ⇒ 实得 `C5E2eA` ⇒ 红（两条互为灭自证）");
                }
            }
        }

        // ---- (d) ability 级 criteria（`CardAbility.targetCriteria` / `actingCardCriteria`）----------
        //  🔴 **本批【不接】**，而且原因不是「难」，是**数据没落**（2026-10-24 `W19` 现核）：
        //    · `unitConditions` 里 `lg = -1` 的行 = `role="target"` **27** + `role="acting"` **1**；
        //    · 它们**只带 `cond`/`ex`**（= `conditionsFilter`/`excludeCondition`）——
        //      原版那**一个** `TargetCriteria` 对象的其余 36 格**生成器没落**
        //      （v25 只落 logic 级两列：`cardAbilities[].abilityLogic[].targetCriteria`）；
        //    · 真值快照 `资料/普查产出_1018/allcards_1350/cards_raw.json` 现扫：
        //      这 28 个对象（26 target + 1 acting + 1 activeAbility target）**全部**还带
        //      非默认的其它格（`friendlyUnits` / `enemyUnits` / `includeHero` / `includeMinions` /
        //      `includeRemnants` / `targetsAffected` …）⇒ **只按 `cond`/`ex` 判 = 静默放宽**
        //      （例：`Drag it Down` 的 `targetCriteria` = `cond=220` **且** `enemyUnits + includeHero
        //       + includeMinions + includeRemnants`）。
        //    ⇒ 宁可不接，也不接一半（红线：不许静默失败）。补齐条件 = 生成器落 ability 级 36 格。
        //    判据（语义侧已闭合）：`CardAbility__CanTriggerAbility.c:134-136`
        //      = `CheckIfMeetsCriteria(thisCard, actingCard, targetCard, ability.actingCardCriteria(+0x28))`
        //      **且** `CheckIfMeetsCriteria(thisCard, targetCard, targetCard, ability.targetCriteria(+0x30))`
        //      **且** `BoardAnalysis.MeetsCriteria( … boardCriteria(+0x38) … )`。
        {
            var pool2 = CardDatabase.Load();
            int targetRows = 0, actingRows = 0;
            string firstTarget = null;
            foreach (var c in pool2)
            {
                if (c == null) continue;
                // `ab` 取 `-3 … 15`：`-1` active / `-2` alternative / `-3` 卡级（生成器 `scan_ability` 的口径）。
                for (int ab = -3; ab < 16; ab++)
                {
                    if (c.ConditionOf(ab, -1, "target") != CardDef.ConditionNone)
                    { targetRows++; if (firstTarget == null) firstTarget = c.Id + "/ab=" + ab; }
                    if (c.ConditionOf(ab, -1, "acting") != CardDef.ConditionNone) actingRows++;
                }
            }
            Check(targetRows, 27,
                  $"★ Q4(d) `unitConditions` 里 `lg=-1` + `role=\"target\"` 的行 = **27**（现读 {targetRows}"
                + (firstTarget != null ? $"，首个 {firstTarget}" : "") + "）"
                + "｜🧨 生成器把 ability 级 criteria 的落法改了 ⇒ 这一行会变，必须回来重核消费端");
            Check(actingRows, 1,
                  $"★ Q4(d) `role=\"acting\"` + `lg=-1` 的行 = **1**（现读 {actingRows}）");

            // 绊线：**ability 级 36 格一格都没落**。今天这条是结构上的恒真
            // （`AbilityTargetCriteriaAt` 对 `logic < 0` 直接返回 null），它的价值 = **将来会红**：
            // 一旦生成器补上 ability 级 36 格，这里不再是 0 ⇒ 逼人回来接 `CanTriggerAbility` 那两跳。
            int abilityLevel36 = 0;
            foreach (var c in pool2)
            {
                if (c == null) continue;
                for (int ab = -2; ab < 16; ab++)
                    if (c.AbilityTargetCriteriaAt(ab, -1) != null) abilityLevel36++;
            }
            Check(abilityLevel36, 0,
                  "★★ Q4(d) 绊线：`abilityTargetCriteria` 里 `lg = -1` 的行 = **0**"
                + "（= ability 级 36 格**没落**，所以 (§三·5) 本批不接）"
                + "｜🧨 生成器补上之后这里会红 —— 那时必须回来接 `CardAbility.targetCriteria` 的消费端，"
                + "⛔ 别把这条改成「预期非 0」了事（那等于把「没做」写成「做完了」）");
        }
    }
}
