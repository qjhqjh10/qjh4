// RuleEngineTest_EngineE4.cs — 引擎线 `E4`（2026-10-25 · 第七十五会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineE2` /
// `_EngineE3` / `_EngineA3` / `_EngineC5` … 都在），同一时刻只有一个写手改一个文件 ⇒ 本批测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestE4TriggerTargetAndActivePick)`**。
//
// 三件的判据（⛔ 不要重新论证；全文 → `资料/普查产出_1025/E4_trigger目标与activeAbility目标池.md`）：
//  ① **E4-1**：`EvtKind.Trigger` 要**带目标** —— 原版
//     `RawCardScript.TriggerAbility` → `AbilityLogic.PlayAbility.c:257-264`
//     （`abilityLogic.targetCriteria` → `AbilityLogic.GetTargets` → `possibleTargets(+0x78)`）
//     → `AbilityLogic__PlayAnim.c:171-248`（**按目标逐个** `GetPos` + `AddCardAnim`）。
//     我们的事件在**广播那一拍**发、目标要等**结算那一拍**才选出来 ⇒ 事后填同一个事件对象
//     （`BattleContext.TriggerTargetSink` 的 doc 有机制）。
//  ② **E4-2**：`ActiveAbility.targetCriteria`（`+0x38`）= 主动能力**目标闸** ——
//     `RawCardScript__IsValidActiveAbilityTarget.c:103-107` →
//     `FilterMethods__CheckIfMeetsCriteria(thisCard, 候选, 候选, *(activeAbility+0x38), 0)`；
//     调用者 = `BattleManager.IsValidActiveAbilityTarget`（高亮 / 点选 / AI / 出手前复核）。
//  ③ **E4-3**：易伤是**第二拍另一条 `DamageInfo`** —— `CardScript._ReceiveDamage_d__381__MoveNext.c`
//     `:226` 造、`:252-254 DealMultiDamage`；主 1.0 s + 易伤 1.4 s、**串行**
//     （判据全文 → `资料/普查产出_1025/J3_判据_战斗剩余四条.md` §3）。
//
// 🔴 每条断言都带 `🧨`（什么改动能让它红）。期望值一律写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestE4TriggerTargetAndActivePick)`）。</summary>
    public static void TestE4TriggerTargetAndActivePick()
    {
        Section("🆕 引擎线 E4：① trigger 事件带目标（`E4-1`）· ② `ActiveAbility.targetCriteria` 目标闸（`E4-2`）"
              + " · ③ 易伤两条 `Hit`（`E4-3`）");
        E4Case1TriggerEventCarriesTarget();
        E4Case2ActivePickData();
        E4Case3ActivePickGate();
        E4Case4VulnerableTwoHits();
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>真卡池 + 指定手牌造一局（`ProbeBattle` 用的是空池，真卡进不去）。</summary>
    static BattleContext E4RealBattle(List<CardDef> pool, params CardDef[] hand0)
    {
        var d0 = new List<CardDef> { Hero("E4W0", 2, 30) };
        for (int i = hand0.Length - 1; i >= 0; i--) d0.Add(hand0[i]);
        var d1 = new List<CardDef> { Hero("E4W1", 2, 30) };
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool);
        if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
        // 先手座位由督军决定（合成督军现读恒 0，但**不假设**）—— 推到「P0 行动」为止。
        int guard = 0;
        while (ctx.Active != 0 && guard++ < 4) { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }
        ctx.Players[0].Energy = 20;
        return ctx;
    }

    /// <summary>那个单位现在在第几格（`-1` = 不在棋盘上）。⚠️ 别假设格号 —— 督军在槽 4，
    /// `BoardSlots.Insert` 会推人（`ProbeBattle` 的夹具尤其容易撞上）。</summary>
    static int E4SlotOf(BattleContext ctx, int p, UnitState u)
    {
        for (int s = 0; s < BoardSpec.Size; s++) if (ctx.Players[p].Board[s] == u) return s;
        return -1;
    }

    static BattleEvent E4FirstTrigger(BattleContext ctx, string cardName)
    {
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e != null && e.Kind == EvtKind.Trigger && e.CardId == cardName) return e;
        }
        return null;
    }

    /// <summary>真卡 `id`：打出它、再推进到自己下一个回合开始（回合起止那一拍）。</summary>
    static BattleEvent E4PlayAndStartTurn(List<CardDef> pool, string cardId, out BattleContext ctx,
                                          out CardDef card)
    {
        ctx = null; card = CardDatabase.FindById(pool, cardId);
        if (card == null) return null;
        ctx = E4RealBattle(pool, card);
        int hi = -1;
        for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
            if (ctx.Players[0].Hand[i]?.Card?.Id == cardId) { hi = i; break; }
        if (hi < 0) return null;
        int rc = RuleCore.PlayCard(ctx, 0, hi, 4);
        if (rc != RuleCodes.OK) return null;
        ctx.Signals.Clear();                 // 只看「回合开始」这一拍
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);   // → 对手回合
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);   // → 自己回合开始（触发）
        return E4FirstTrigger(ctx, card.Name);
    }

    /// <summary>合成主动能力卡：`Ability: Damage 3 EnemyUnit`（⇒ `Card.Ability.Target == EnemyUnit`，
    /// `EffectTargets.NeedsPick` 为真、`CanStartAbility` 过得去）。
    /// `pick` = `ActiveAbility.targetCriteria` 那一列（`ab = -1` active）。</summary>
    static CardDef E4AbilityCard(string id, CardDef.CountSetSpec pick, bool versioned = true)
    {
        List<CardDef.TargetCriteriaRowSpec> rows = null;
        if (pick != null)
            rows = new List<CardDef.TargetCriteriaRowSpec>
            {
                new CardDef.TargetCriteriaRowSpec
                {
                    Ability = CardDef.ActiveAbilityObject, Trigger = -1, Logic = -1, Criteria = pick,
                },
            };
        return new CardDef(id, id, "unit", "", null, "Test", 1, 1, 5, 0,
                           new[] { "Ability: Damage 3 EnemyUnit" },
                           // 🔴 `hasAlternativeAbility: 0` **必须给**：原版 `RawCardScript.GetActiveAbility`
                           //   先看这一位；`GateUnknown(-1)` ⇒ `UsesAlternativeAbility` 回 `null` ⇒
                           //   目标闸**判不了 ⇒ 放行**（夹具会静默变成「什么都没验到」）。
                           //   真卡表 v21+ 的缺席值是 `0`（生成器只落非 0）⇒ 夹具照抄 `0` 才同形。
                           hasAlternativeAbility: 0,
                           activeAbilityPickCriteria: rows,
                           activeAbilityPickCriteriaVersioned: versioned);
    }

    // ==================================================================
    //  ① E4-1：`EvtKind.Trigger` 带目标（回合起止那一趟）
    // ==================================================================

    static void E4Case1TriggerEventCarriesTarget()
    {
        var pool = CardDatabase.Load();

        // ---- ①·a 真卡 `TL37 Spore Mine`：「回合开始摧毁自己并对随机敌人 3 点」 ----
        //   两条 op：`[0] destroy this unit`（**自指**）、`[1] deal 3 damage to a random enemy`。
        //   原版 `SporeMine_Explosion` 的位移挂在**敌人那条**逻辑上 ⇒ 事件要带**敌人**、⛔ 不是自己。
        BattleContext ctx; CardDef spore;
        var evt = E4PlayAndStartTurn(pool, "TL37", out ctx, out spore);
        CheckTrue(spore != null, "① （前提）卡池里有 `TL37 Spore Mine`");
        CheckTrue(evt != null, "★★ ① `TL37` 的 `TurnStartPlayer` 触发事件发出来了"
                             + "｜🧨 `HasTriggerSource` 的闸被改坏 / 回合起止发射口被摘 ⇒ 红");
        if (evt != null)
        {
            CheckTrue(evt.TargetPlayer >= 0 && evt.TargetSlot >= 0,
                      "★★★ ①【E4-1 的修复点】这条 `EvtKind.Trigger` **带目标**"
                    + "｜🧨 把 `AtTurnUnitJobDeferred` 里那次 arm（`ctx.TriggerTargetSink`）删掉 ⇒ 红"
                    + "（改前就是 `-1` ⇒ 表现层 `GetBoardPosOfCard` 算不出落点 ⇒ 卡本体位移播不出来）");
            CheckTrue(evt.TargetPlayer == 1,
                      "★★ ① 目标是**敌方**（`TargetPlayer == 1`）"
                    + "｜🧨 把 `NoteTriggerTargetSink` 里那条「跳过施放者自己」删掉 ⇒ 会填成自己（`p0s5`）⇒ 红"
                    + "（`TL37` 的第一条 op 是 `destroy this unit`，它的目标是**自己**）");
            Check(evt.TargetCardId, ctx.Players[1].Board[evt.TargetSlot]?.Name ?? "?",
                  "★ ① `TargetCardId` 与那一格上的单位一致");
        }

        // ---- ①·b 负例：`TL8 Genestealer`「回合开始把一张 Genestealer 放进手牌」= 没有棋盘目标 ----
        BattleContext c8; CardDef g8;
        var e8 = E4PlayAndStartTurn(pool, "TL8", out c8, out g8);
        CheckTrue(e8 != null, "①（前提）`TL8` 的触发事件发出来了");
        if (e8 != null)
            CheckTrue(e8.TargetPlayer < 0 && e8.TargetSlot < 0,
                      "★★ ① 没有棋盘目标的触发 ⇒ **保持 `-1`**（⛔ 不拿格号猜）"
                    + "｜🧨 把 `NoteTriggerTargetSink` 改成「随便挑第一个目标」/ 填施放者 ⇒ 红");

        // ---- ①·c 负例：`GOF80 Grot Orderly`「回合开始回到手牌」= **自指**目标 ----
        BattleContext c80; CardDef g80;
        var e80 = E4PlayAndStartTurn(pool, "GOF80", out c80, out g80);
        CheckTrue(e80 != null, "①（前提）`GOF80` 的触发事件发出来了");
        if (e80 != null)
            CheckTrue(e80.TargetPlayer < 0 && e80.TargetSlot < 0,
                      "★★ ① **自指目标 ⇒ 不填**（原版落点 `boardPos == start` ⇒ `normalize(0)`、不位移，"
                    + "与「没有目标」同一结果）"
                    + "｜🧨 去掉那条「跳过施放者」⇒ 会填成自己 ⇒ 红");

        // ---- ①·d 正例（多目标）：`TAU13 DS8 Support Turret`「对全体敌人 2 点」⇒ 取第一个 ----
        BattleContext ct; CardDef gt;
        var et = E4PlayAndStartTurn(pool, "TAU13", out ct, out gt);
        CheckTrue(et != null, "①（前提）`TAU13` 的触发事件发出来了");
        if (et != null)
            CheckTrue(et.TargetPlayer == 1 && et.TargetSlot >= 0,
                      "★★ ① 多目标（`all enemies`）那一档也带目标 —— **我们的事件只能带 1 个**，取第一个"
                    + "（原版按 `possibleTargets` 逐目标各播一份；如实记在报告里）");
    }

    // ==================================================================
    //  ② E4-2 数据：`activeAbilityPickCriteria` 列真的读得到
    // ==================================================================

    static void E4Case2ActivePickData()
    {
        var pool = CardDatabase.Load();

        // ---- ②·a 真卡 `AM68 Tempestus Scion`：`ta = 10 / enemyUnits = 1 / includeRemnants = 1` ----
        var am68 = CardDatabase.FindById(pool, "AM68");
        CheckTrue(am68 != null, "② （前提）卡池里有 `AM68 Tempestus Scion`");
        if (am68 != null)
        {
            CheckTrue(am68.ActiveAbilityPickCriteriaVersioned,
                      "★★ ② `activeAbilityPickCriteria` 列在（`version >= 28`）"
                    + "｜🧨 生成器不落这一列 / `CardDatabase` 不传版本位 ⇒ 访问器一路 null ⇒ 红");
            string why;
            var set = am68.ActiveAbilityPickCriteriaFor(CardDef.ActiveAbilityObject, out why);
            CheckTrue(set != null,
                      "★★ ② `AM68` 取得到 `activeAbility`（`ab = -1`）那一格的目标闸"
                    + $"（why = {why ?? "（无）"}）"
                    + "｜🧨 把 `ab` 的取值写成 `cardAbilities` 下标（`>= 0`）⇒ 这里 null ⇒ 红");
            if (set != null)
            {
                Check(set.EnemyUnits, 1, "★★ ② `enemyUnits = 1`（这一档才是我们今天能忠实判的）"
                                       + "｜🧨 生成器读成 `activeAbilityLogic[j].targetCriteria` ⇒ 值是 `ta=30` 那一批 ⇒ 红");
                Check(set.TargetsAffected, 10, "★ ② `ta = 10 allFiltered`");
                Check(set.IncludeRemnants, 1, "★ ② `includeRemnants = 1`（残骸可以点）");
            }
            // `ab = -2`（alternativeAbility）这张卡没有 ⇒ 明确「没有这一格」
            var alt = am68.ActiveAbilityPickCriteriaFor(CardDef.AlternativeAbilityObject, out why);
            CheckTrue(alt == null && why == null,
                      "★ ② `ab = -2` 没有行 ⇒ `null` 且 `why == null`（= 明确「没有这一格条件」）"
                    + "｜🧨 把「没有行」也塞进 `why` 档 ⇒ 消费端会白出声 ⇒ 红");
        }

        // ---- ②·b 覆盖面读数（写死 ⇒ 生成器变了就红）----
        int cards = 0, rows = 0, ta10 = 0, ta20 = 0, enemySide = 0;
        foreach (var c in pool)
        {
            if (c == null) continue;
            if (c.ActiveAbilityPickCriteriaRowCount > 0) cards++;
            rows += c.ActiveAbilityPickCriteriaRowCount;
            string w;
            var a = c.ActiveAbilityPickCriteriaFor(CardDef.ActiveAbilityObject, out w);
            if (a == null) continue;
            if (a.TargetsAffected == 10) ta10++;
            if (a.TargetsAffected == 20) ta20++;
            if (a.EnemyUnits == 1) enemySide++;
        }
        Check(rows, 107, "★★ ② 全池落 **107 行**（v28 生成器现读；`ta = 10` 73 + `ta = 20` 34）"
                       + "｜🧨 落行门槛从 `ta != 0` 改成「36 格非平凡」⇒ 121 ⇒ 红");
        Check(cards, 107, "★★ ② 全池落 **107 张卡**｜🧨 同上 ⇒ 121 ⇒ 红");
        Check(ta10, 73, "★ ② `ta = 10` 的行 **73**");
        Check(ta20, 34, "★ ② `ta = 20 self` 的行 **34**"
                       + "（🔴 2026-10-25 `E8` 就地订正原注释：这些**现在也能进**主动能力目标闸 —— "
                       + "候选所有者按 criteria 的候选侧解；`ta=20` 由 `TargetCriteriaEval` 的 `self` 支判"
                       + "「候选必须就是载体自己」⇒ 拿别的单位当候选会被拒，与反编译的 "
                       + "`CheckIfMeetsCriteria` 那两格同向）");
        // ---- ②·c 🔴 **可达性（如实）**：这道闸接在 `CanUseAbility` 上，而那个口要求
        //     `Card.Ability != null`（正文里有一条 `Ability:` 关键字）。现读**全池 0 张** ⇒
        //     今天**一张真卡都走不到**这道闸（`canUseAbility` 的主动能力路整条要等「结构化
        //     `ActiveAbility` 执行」那一批）。⛔ 别把这条删掉当作「已经验过真卡」。
        int withAbilityText = 0;
        foreach (var c in pool) if (c != null && c.Ability != null) withAbilityText++;
        Check(withAbilityText, 0,
              "★★ ② **可达性：全池 0 张卡有解析成功的 `Ability:`** ⇒ `CanUseAbility` 的主动能力目标路今天"
            + "**一条都走不到**（`CanStartAbility` 先回 `ErrNoAbility`）"
            + "｜🧨 将来结构化 `ActiveAbility` 落地 / 卡表里出现 `Ability:` 正文 ⇒ 这个数会变 ⇒ 到时候"
            + "必须把这道闸按真卡逐张复核（⛔ 本行不是「闸没用」，是「今天的路还没通」）");
        Check(enemySide, 48, "★★ ② `enemyUnits == 1`（**今天唯一能忠实判的一档**）**48 张**"
                          + "｜🧨 生成器把 `ab = -2` 那一支也落了（`SW21`）⇒ 有行但 `enemyUnits != 1` ⇒ 本行不变、"
                          + "但 ②·c 的读数会变 ⇒ 一起看");
    }

    // ==================================================================
    //  ③ E4-2 消费点：`CanUseAbility` 的目标闸（三态 + 灭自证）
    // ==================================================================

    static void E4Case3ActivePickGate()
    {
        // `E2Criteria10(1, 0)`：`ta = 10 / enemyUnits = 1 / includeHero + includeMinions + includeRemnants = 1`
        var allowAll = E2Criteria10(1, 0);
        // （b）把「残骸」挡在外面：只改这一格 ⇒ 其余逐格相同（= 只差被验的那一格）
        var noRemnant = E2Criteria10(1, 0); noRemnant.IncludeRemnants = 0;
        // （d）候选侧不是敌方：`enemyUnits = 0`（= 只打友方 / 自己）—— 我们的候选集不在原版这一档里
        var friendlyOnly = E2Criteria10(0, 1);

        // ---- ③·a 正例：残骸也算合法（`includeRemnants = 1`）----
        {
            var card = E4AbilityCard("E4PickA", allowAll);
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            int me = ctx.Active;
            var caster = Place(ctx, me, 3, card);
            var foe = Place(ctx, 1 - me, 3, Unit("E4FoeA", 1, 1, 5));
            foe.IsRemnant = true;
            int cs = E4SlotOf(ctx, me, caster), ts = E4SlotOf(ctx, 1 - me, foe);
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, ts), RuleCodes.OK,
                      "★★ ③a 正例：`enemyUnits = 1` + 残骸 `includeRemnants = 1` ⇒ **可点**"
                    + "｜🧨 闸接成 `set.EnemyUnits != 1` 就 return（把整档都放行）⇒ 下面 ③b 红");
        }

        // ---- ③·b 负例：同一张卡改成 `includeRemnants = 0` ⇒ 残骸**不可点** ----
        //   与 ③·a **只差这一格** ⇒ 这就是「闸真的在读 36 格」的灭自证。
        {
            var card = E4AbilityCard("E4PickB", noRemnant);
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            int me = ctx.Active;
            var caster = Place(ctx, me, 3, card);
            var foe = Place(ctx, 1 - me, 3, Unit("E4FoeB", 1, 1, 5));
            foe.IsRemnant = true;
            int cs = E4SlotOf(ctx, me, caster), ts = E4SlotOf(ctx, 1 - me, foe);
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, ts), RuleCodes.ErrTarget,
                      "★★★ ③b 负例：`includeRemnants = 0` ⇒ 残骸 **不可点**（`ErrTarget`）"
                    + "｜🧨 把 `ActiveAbilityPickGate` 整条删掉 / 恒回 OK ⇒ 红（改前就是这样）");
            // 同一张卡打**普通单位**仍然可点（证明挡的是「残骸」这一格，不是「全都挡」）
            var live = Place(ctx, 1 - me, 6, Unit("E4FoeB2", 1, 1, 5));
            CheckCode(RuleCore.CanUseAbility(ctx, me, cs, E4SlotOf(ctx, 1 - me, live)), RuleCodes.OK,
                      "★★ ③b 同一张卡打**普通单位** ⇒ **可点**（挡的只是残骸那一格）"
                    + "｜🧨 判据写成「候选是残骸就 false」（不理 `includeRemnants`）⇒ 本行仍绿、③a 红 ⇒ 两条一起看");
        }

        // ---- ③·c 友方档：`enemyUnits == 0 / friendlyUnits == 1`
        //      🔴 **2026-10-25（执行代理 `E8` · `E8-1`）就地改判**：这一档**不再**是「判不了 ⇒ 放行」——
        //        候选侧已经按原版 criteria 解（`RuleCore.AbilityPickSideOf`）⇒ 候选真的落在**友方棋盘**上，
        //        可以忠实地过 36 格闸。改前那句 `if (set.EnemyUnits != 1) ⇒ 放行` 的立足点
        //        （「我们的候选恒为敌方」）已经消失。
        //      ⇒ 本组钉三件：**解到友方侧** · 友方格**可点** · 敌对那一格**不动**（判别式）。
        {
            var card = E4AbilityCard("E4PickC", friendlyOnly);
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            int me = ctx.Active;
            var caster = Place(ctx, me, 3, card);
            var mate = Place(ctx, me, 6, Unit("E4MateC", 1, 1, 5));
            var foe = Place(ctx, 1 - me, 6, Unit("E4FoeC", 1, 1, 5));
            string sideWhy;
            Check((int)RuleCore.AbilityPickSideOf(ctx, me, caster, out sideWhy), 2,
                  "★★ ③c 友方档 ⇒ 候选侧 = **Friendly(2)**"
                + "｜🧨 两格读反 / 把这一档还当「判不了」⇒ 红");
            CheckCode(RuleCore.CanUseAbility(ctx, me, E4SlotOf(ctx, me, caster), 6), RuleCodes.OK,
                      "★★ ③c 友方档：点**自己人** ⇒ 可点（`enemyUnits=0` 不再让整档失效）");
            CheckCode(RuleCore.UseAbility(ctx, me, E4SlotOf(ctx, me, caster), 6), RuleCodes.OK,
                      "★★ ③c 友方档：放得出来");
            Check(mate.Health, 2, "★★★ ③c 效果落在**自己人**身上（5 → 2，`Damage 3`）"
                               + "｜🧨 候选所有者写回 `1 - p` ⇒ 自己人不动、敌对那格挨打 ⇒ 本行与下一行一起红");
            Check(foe.Health, 5, "★★★ ③c 对面那一格**一滴没掉**（判别式：友方候选不许落到敌方棋盘）");
        }

        // ---- ③·d 旧表（列没落）⇒ 放行 + 出声（与改前逐字相同）----
        {
            var card = E4AbilityCard("E4PickD", allowAll, versioned: false);
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            int me = ctx.Active;
            var caster = Place(ctx, me, 3, card);
            var foe = Place(ctx, 1 - me, 3, Unit("E4FoeD", 1, 1, 5));
            int warn = RuleCore.ActiveAbilityPickCriteriaWarningsForTest;
            CheckCode(RuleCore.CanUseAbility(ctx, me, E4SlotOf(ctx, me, caster), E4SlotOf(ctx, 1 - me, foe)), RuleCodes.OK,
                      "★★ ③d 旧表（列没落）⇒ 这道闸**不筛**（放行）");
            CheckTrue(RuleCore.ActiveAbilityPickCriteriaWarningsForTest > warn,
                      "★ ③d 旧表也**出声**（判不了 ≠ 没有条件）");
        }

        // ---- ③·e 明确「没有这一格条件」⇒ 放行且**不出声** ----
        {
            var card = E4AbilityCard("E4PickE", null);      // 没有行 = `[]`
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            int me = ctx.Active;
            var caster = Place(ctx, me, 3, card);
            var foe = Place(ctx, 1 - me, 3, Unit("E4FoeE", 1, 1, 5));
            int warn = RuleCore.ActiveAbilityPickCriteriaWarningsForTest;
            CheckCode(RuleCore.CanUseAbility(ctx, me, E4SlotOf(ctx, me, caster), E4SlotOf(ctx, 1 - me, foe)), RuleCodes.OK,
                      "★ ③e 没有这一格条件 ⇒ 放行");
            Check(RuleCore.ActiveAbilityPickCriteriaWarningsForTest, warn,
                  "★★ ③e **不出声**（「明确没有条件」与「判不了」必须分开）"
                + "｜🧨 把 `why == null` 那一档也塞进 `NoteActivePickGap` ⇒ 红");
        }
    }

    // ==================================================================
    //  ④ E4-3：易伤两条 `Hit`
    // ==================================================================

    static void E4Case4VulnerableTwoHits()
    {
        // ---- ④·a 非致死档：伤 5 / 甲 1 / 易伤 2 ⇒ 主 4 + 易伤 1、两条 `Hit`、顺序固定 ----
        {
            UnitState a, b;
            var ctx = Duel(Unit("E4Peer1", 1, 1, 9), Unit("E4Def1", 1, 0, 9, "Armour 1"), out a, out b);
            var t = Board(ctx, 1, 3);
            t.AddKeyword("vulnerable", 2);
            ctx.Signals.Clear();
            int ret = RuleCore.ApplyDamage(ctx, t, 5, "自检");
            var hits = new List<BattleEvent>();
            foreach (var e in ctx.Signals) if (e != null && e.Kind == EvtKind.Hit) hits.Add(e);
            Check(hits.Count, 2,
                  "★★★ ④a 易伤目标挨一下 ⇒ **两条 `EvtKind.Hit`**（原版两条 `DamageInfo`）"
                + "｜🧨 退回「折进同一条 `Hit`」⇒ 1 条 ⇒ 红（`E4-3` 就是修这个）");
            if (hits.Count >= 2)
            {
                Check(hits[0].Amount, 4,
                      "★★ ④a 第 1 条 = **主伤害**（`max(1, 5−1)` = 4；⛔ 不含易伤那份）"
                    + "｜🧨 第 1 条仍发合计（5）⇒ 红");
                CheckTrue(!hits[0].IsVulnerableSecondHit, "★★ ④a 第 1 条 **不是**易伤拍");
                Check(hits[1].Amount, 1,
                      "★★ ④a 第 2 条 = **易伤那份** `max(1, 2−1)` = 1");
                CheckTrue(hits[1].IsVulnerableSecondHit,
                          "★★★ ④a 第 2 条带 `IsVulnerableSecondHit`（表现层据此排 1.4 s）"
                        + "｜🧨 不设这一位 ⇒ 表现层只能按「受击方带不带 vulnerable」猜 ⇒ 主拍也被排成 1.4 ⇒ 红");
                CheckTrue(hits[0].Player == hits[1].Player && hits[0].Slot == hits[1].Slot,
                          "★ ④a 两条打的是**同一格**（同一个受击者）");
            }
            Check(ret, 5, "★ ④a 返回值 = 合计 5（**算术没变**，只有事件拆开了）");
            Check(t.Health, 4, "★ ④a 真扣 5 血（9 − 5）");
        }

        // ---- ④·b 无易伤 ⇒ 只有一条 `Hit`（回归）----
        {
            UnitState a, b;
            var ctx = Duel(Unit("E4Peer2", 1, 1, 9), Unit("E4Def2", 1, 0, 9, "Armour 1"), out a, out b);
            var t = Board(ctx, 1, 3);
            ctx.Signals.Clear();
            RuleCore.ApplyDamage(ctx, t, 5, "自检");
            int n = 0;
            foreach (var e in ctx.Signals) if (e != null && e.Kind == EvtKind.Hit) n++;
            Check(n, 1, "★★ ④b 没有易伤 ⇒ **只有一条** `Hit`"
                      + "｜🧨 无条件发两条 ⇒ 红");
        }

        // ---- ④·c 主伤害就能打死 ⇒ **不发**第二拍（原版 `:210-211` 那道闸）----
        {
            UnitState a, b;
            var ctx = Duel(Unit("E4Peer3", 1, 1, 9), Unit("E4Def3", 1, 0, 9), out a, out b);
            var t = Board(ctx, 1, 3);
            t.AddKeyword("vulnerable", 2);
            ctx.Signals.Clear();
            RuleCore.ApplyDamage(ctx, t, 9, "自检");
            int n = 0;
            foreach (var e in ctx.Signals) if (e != null && e.Kind == EvtKind.Hit) n++;
            Check(n, 1, "★★ ④c 主伤害致死 ⇒ **不出第二拍**"
                      + "｜🧨 在 `ApplyDamage` 尾巴按「只要带 vulnerable 就发」⇒ 红");
        }

        // ---- ④·d 残骸 ⇒ 不发第二拍 ----
        {
            UnitState a, b;
            var ctx = Duel(Unit("E4Peer4", 1, 1, 9), Unit("E4Def4", 1, 0, 9), out a, out b);
            var t = Board(ctx, 1, 3);
            t.IsRemnant = true;
            t.AddKeyword("vulnerable", 2);
            ctx.Signals.Clear();
            RuleCore.ApplyDamage(ctx, t, 5, "自检");
            int n = 0;
            foreach (var e in ctx.Signals) if (e != null && e.Kind == EvtKind.Hit) n++;
            Check(n, 1, "★★ ④d 残骸 ⇒ **不出第二拍**（原版 `+0x65 == 0`）｜🧨 去掉 `u.IsRemnant` ⇒ 红");
        }

        // ---- ④·e 盾挡下 ⇒ 一条 `Hit(0)`、没有第二拍 ----
        {
            UnitState a, b;
            var ctx = Duel(Unit("E4Peer5", 1, 1, 9), Unit("E4Def5", 1, 0, 9), out a, out b);
            var t = Board(ctx, 1, 3);
            t.AddKeyword("vulnerable", 2);
            t.AddKeyword(KeywordTable.Shield, 1);
            ctx.Signals.Clear();
            CheckCode(RuleCore.ApplyDamage(ctx, t, 5, "自检"), RuleCodes.OK, "（前提）盾那一支返回 0");
            var hits = new List<BattleEvent>();
            foreach (var e in ctx.Signals) if (e != null && e.Kind == EvtKind.Hit) hits.Add(e);
            Check(hits.Count, 1, "★★ ④e 盾挡下 ⇒ **只有一条**（`Amount = 0`）｜🧨 发第二拍 ⇒ 红");
            if (hits.Count >= 1) Check(hits[0].Amount, 0, "★ ④e 那一条的 `Amount = 0`");
        }

        // ---- ④·f 堡垒被打穿 ⇒ 主 = 溢出、第二拍 = 易伤那份（**走堡垒那一支的拆数**）----
        {
            UnitState a, b;
            var ctx = Duel(Unit("E4Peer6", 1, 1, 9), Unit("E4Def6", 1, 0, 9, "Bastion 3", "Armour 1"),
                           out a, out b);
            var t = Board(ctx, 1, 3);
            t.AddKeyword("vulnerable", 2);
            ctx.Signals.Clear();
            RuleCore.ApplyDamage(ctx, t, 5, "自检");
            var hits = new List<BattleEvent>();
            foreach (var e in ctx.Signals) if (e != null && e.Kind == EvtKind.Hit) hits.Add(e);
            Check(hits.Count, 2, "★★ ④f 堡垒被打穿 + 易伤 ⇒ **两条**");
            if (hits.Count >= 2)
            {
                Check(hits[0].Amount, 2, "★★ ④f 第 1 条 = `打穿后的溢出` 2（5 − 3）"
                                        + "｜🧨 那一支不设 `vulnSecondPart` ⇒ 第 1 条会是 3 ⇒ 红");
                Check(hits[1].Amount, 1, "★★ ④f 第 2 条 = `max(1, 2 − 1)` = 1");
            }
        }
    }
}
