// B114_W30_Assert.cs — 第一百一十四会话 · 写手 `W-30` · 账 `B-245`
//   `B-177` 刻意没接的**三处**，回源判定之后：哪几处原版**也复核 criteria**（接上）· 哪几处判据不支持（不改）。
//
// ============================ 它盯的是哪三格 ============================
//  ① **`Synapse`（`RepeatTacticOnAdjacent`）** —— 原版 `CardScript__TargetedSpellPlayed.c:51-69`：
//     对每个相邻重跑 vtable `+0x318` = `RawCardScript.IsValidTarget(rawOf这张战术, 这张战术, 相邻)`
//     ⇒ **同一个谓词**（玩家点目标那一支 = `BattleManager__IsValidSpellTarget.c:59-62`）。
//     我们改前 `IsLegalPick(ctx, p, spec, nb)` **没传 `casterCard`** ⇒ 卡级 criteria 与伪装闸都没判。
//  ② **`excludeNotSelf`** —— `FilterMethods__CheckIfMeetsCriteria.c:239-243`（`候选 != thisCard ∧ 该位 != 0 ⇒ 0`）。
//     点选路的 `thisCard` = 打出的那张牌，**永远不是**棋盘上的候选 ⇒ 原版**整池淘汰**。
//     （`includeSelf` 是反向的 `:236-238`，同一个前提让它**恒不命中** ⇒ 跳过即等价，⛔ 别照着补。）
//  ③ **`PlayTargetPool` 的预览单位（待落地那张自己）** —— 原版
//     `_ResolvePlayCardFromHand_d__447__MoveNext.c:849-860` → `BattleManager__IsThereValidUnitTarget`
//     → `RawCardScript__GetValidTargets.c:31-45` 扫的是**落位之后**的棋盘 ⇒ 它自己也在候选里、
//     也要过 `IsValidTarget`。我们只补了**不依赖棋盘**的 `includeSelf`（`PreviewSelfExcludedByCardCriteria`）；
//     其余 35 格要 `SideOf`（预览不在棋盘 ⇒ -1）⇒ 补丁见 `报告/B114_W30_criteria三处.md` §四。
//
// ============================ 🔴 灭自证（什么改动能让它红）============================
//  · A6 ：把 `RepeatTacticOnAdjacent` 的 `casterCard: card` 摘掉 ⇒ 两个相邻**都**被重复 ⇒ A6 红
//         （A5 仍绿 —— 这一对正好卡住「接没接上」）。A7/A7b 是**同一块棋盘、只把 criteria 的 keyword 档换掉**的对照。
//  · B1 ：把 `CardCriteriaExcludeNotSelfEmptiesPool` 那一支摘掉 ⇒ 池子不再被清 ⇒ B1 红；
//         写成「恒剔」⇒ B2 红（B2 是**同一块棋盘、只翻这一位**的对照）。
//  · C1 ：把 `PlayTargetPool` 的 `!PreviewSelfExcludedByCardCriteria(card)` 摘掉 ⇒ 预览留在池里 ⇒ C1 红；
//         C0b 是**同一张牌、只翻 `includeSelf` 一位**的对照（说明 C1 不是「池恒空」）。
//
// ============================ 怎么接（宿主 = `RuleEngine/Editor/RuleEngineTest.cs`）============================
//  在 `Run()` 的聚合行（**同一物理行**、行尾 `//` **之前**）追加：
//      Guard("CheckB114W30CriteriaThreePlaces", CheckB114W30CriteriaThreePlaces);
//  ⚠️ 本仓已 6 次踩「追加落到行尾 `//` 后面 ⇒ 代码被注释吞掉、断言一条没跑」。
//  ⚠️ 入口名**不以 `Test` / `Report` 打头**（`VerifyRunCaseCoverage` / `gen_diag_runner` 按那两个前缀认用例）。
//
// ============================ 副作用 ============================
//  · 只读卡表 + 只在**自建的两局**里摆牌；不写存档、不动全局状态、不跑回合。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>`B-245`：那三处（Synapse 邻居 / `excludeNotSelf` / 预览单位）在原版到底复不复核 criteria。</summary>
    public static void CheckB114W30CriteriaThreePlaces()
    {
        // ======================================================================================
        //  0 · 数据面前提（本文件那两条补齐判据各靠一条不变量）
        // ======================================================================================
        var db = CardDatabase.Load();
        CheckTrue(db != null && db.Count > 1000, "前提 0：卡表装上了（" + (db != null ? db.Count : -1) + " 张）");
        int targetedN = 0, taNot10 = 0, exNotSelfN = 0, incSelfOne = 0;
        for (int i = 0; i < db.Count; i++)
        {
            var c = db[i];
            if (c == null || c.CardTargetCriteria == null) continue;
            if (c.Targeted == 1) { targetedN++; if (c.CardTargetCriteria.TargetsAffected != 10) taNot10++; }
            if (c.CardTargetCriteria.ExcludeNotSelf != 0) exNotSelfN++;
            if (c.CardTargetCriteria.IncludeSelf == 1) incSelfOne++;
        }
        Check(taNot10, 0, "★ 前提 0a：`targeted == 1` 的卡（" + targetedN + " 张）`targetsAffected` 恒 10 —— "
                        + "下面两条补齐都靠它（原版别的 ta 档走更早的 switch 短路，不看那两格）；实得例外 " + taNot10);
        Check(exNotSelfN, 0, "★ 前提 0b：全池 `cardTargetCriteria.excludeNotSelf` 全 0（实得非 0 = " + exNotSelfN + " 张）"
                          + " ⇒ `CardCriteriaExcludeNotSelfEmptiesPool` 今天**零行为变化**（补的是「将来写得出」这一层）");
        CheckTrue(incSelfOne >= 1, "★ 前提 0c：全池有 " + incSelfOne + " 张卡 `includeSelf == 1`（现读 `SOR66 Armorium Cherub`）"
                                 + " —— C 段那个对照要这一档");

        // ======================================================================================
        //  A · 突触（Synapse）：相邻的重跑也要过卡级 criteria
        // ======================================================================================
        {
            var synVeh = W30SynapseUnit("W30SynVeh", 10);                 // 锚点：Vehicle（`Keyword.Vehicle = 10`）
            var healVeh = Tactic("T_W30SynHealVeh", 0, "Heal 3 to a friendly unit");
            healVeh.AttachCardTargetCriteria(W30Spec(10, friendly: 1, enemy: 0, hero: 1, minions: 1, remnants: 1, keyword: 10), true);
            var ctx = ProbeBattle(new[] { synVeh, healVeh }, new CardDef[0]);
            ToP1Turn(ctx, 2);
            var mid = Place(ctx, 0, 2, synVeh, exhausted: true);
            var left = Place(ctx, 0, 1, W30PlainUnit("W30FriendVeh", 10), exhausted: true);   // Vehicle
            var right = Place(ctx, 0, 3, W30PlainUnit("W30FriendInf", 5), exhausted: true);   // Infantry
            mid.Health = 10; left.Health = 4; right.Health = 4;

            CheckTrue(healVeh.CardTargetCriteria != null && healVeh.CardTargetCriteria.KeywordFilter == 10,
                      "前提 A0：战术夹具挂上了**卡级** criteria（`keywordFilter = Vehicle(10)`）");
            CheckTrue(mid.Has(KeywordTable.Synapse), "前提 A1：锚点带 `Synapse`（原版 `TargetedSpellPlayed.c:31` 的 `HasCurrentTrait(0x47e)`）");
            CheckTrue(left != null && right != null && left.Card.KeywordId == 10 && right.Card.KeywordId == 5,
                      "前提 A2：两个相邻分别是 Vehicle(10) / Infantry(5)，且都活着");
            int hiVeh = HandIdx(ctx, 0, "T_W30SynHealVeh");
            CheckTrue(hiVeh >= 0, "前提 A3：战术在手里（" + hiVeh + "）" + LogTail(ctx));

            CheckCode(RuleCore.PlayTactic(ctx, 0, hiVeh, 2), RuleCodes.OK, "A4：友方战术选中带突触的 Vehicle 锚点");
            Check(mid.Health, 13, "A5：锚点自己被治（4 → 13）");
            Check(left.Health, 7, "★ A5b【结论】：相邻的 **Vehicle** 被重复了一次（4 → 7）");
            Check(right.Health, 4, "★ A6【灭自证】：相邻的 **Infantry** **没有**被重复（还是 4）"
                                 + "｜🧨 突触那条 `IsLegalPick` 不传 `casterCard` ⇒ 这一格会变 7、A6 红"
                                 + "（判据 = `CardScript__TargetedSpellPlayed.c:51-69`：每个相邻都跑 `IsValidTarget`）");
        }
        {
            // 对照：**同一块棋盘**，只把 criteria 的 `keywordFilter` 从 Vehicle 改成 0（不筛兵种）
            var synVeh2 = W30SynapseUnit("W30SynVeh2", 10);
            var healAll = Tactic("T_W30SynHealAll", 0, "Heal 3 to a friendly unit");
            healAll.AttachCardTargetCriteria(W30Spec(10, friendly: 1, enemy: 0, hero: 1, minions: 1, remnants: 1, keyword: 0), true);
            var ctx2 = ProbeBattle(new[] { synVeh2, healAll }, new CardDef[0]);
            ToP1Turn(ctx2, 2);
            var mid2 = Place(ctx2, 0, 2, synVeh2, exhausted: true);
            var left2 = Place(ctx2, 0, 1, W30PlainUnit("W30FriendVeh2", 10), exhausted: true);
            var right2 = Place(ctx2, 0, 3, W30PlainUnit("W30FriendInf2", 5), exhausted: true);
            mid2.Health = 10; left2.Health = 4; right2.Health = 4;
            int hi2 = HandIdx(ctx2, 0, "T_W30SynHealAll");
            CheckTrue(hi2 >= 0, "前提 A6b：对照那一份战术在手里（" + hi2 + "）" + LogTail(ctx2));
            CheckCode(RuleCore.PlayTactic(ctx2, 0, hi2, 2), RuleCodes.OK, "A7：同一块棋盘，criteria 不筛兵种 ⇒ 这一手能打");
            Check(left2.Health, 7, "★ A7b【与 A6 并排的对照】：`keywordFilter = 0` ⇒ 相邻的 Vehicle 被重复（4 → 7）");
            Check(right2.Health, 7, "★ A7c【与 A6 并排的对照】：`keywordFilter = 0` ⇒ 相邻的 Infantry **也**被重复（4 → 7）"
                                  + "｜🧨 两条对照合起来说明：A6 的差异**只可能**来自卡级 criteria，不是棋盘或别的闸");
        }

        // ======================================================================================
        //  B · `excludeNotSelf != 0` ⇒ 点选池整池淘汰（`FilterMethods__CheckIfMeetsCriteria.c:239-243`）
        // ======================================================================================
        {
            var onlySelf = Tactic("T_W30ExcludeNotSelf", 0, "Heal 3 to a friendly unit");
            onlySelf.AttachCardTargetCriteria(W30Spec(10, friendly: 1, enemy: 0, hero: 1, minions: 1, remnants: 1,
                                                      keyword: 0, excludeNotSelf: 1), true);
            var ctxB = Battle(new[] { onlySelf }, new CardDef[0]);
            ToP1Turn(ctxB, 2);
            var fB = Place(ctxB, 0, 2, Unit("W30BFriend", 1, 0, 10), exhausted: true);
            CheckTrue(fB != null && fB.IsAlive, "前提 B0：友军部队在场上（否则 B1 测的是「池本来就空」）");
            var poolB = RuleCore.PlayTargetPool(ctxB, 0, onlySelf);
            CheckTrue(poolB != null, "前提 B0b：`PlayTargetPool` 有返回");
            Check(poolB != null ? poolB.Count : -1, 0, "★ B1【结论】：`excludeNotSelf = 1` ⇒ 池子**整池淘汰**"
                                                     + "（原版：施放者自己永远不是棋盘候选 ⇒ `候选 != thisCard` 恒真 ⇒ 全 0）"
                                                     + "｜🧨 那一支摘掉 ⇒ 这一格变 1、B1 红");
        }
        {
            // 对照：**同一张牌、同一块棋盘**，只把 `excludeNotSelf` 从 1 改成 0
            var notOnlySelf = Tactic("T_W30IncludeNotSelf", 0, "Heal 3 to a friendly unit");
            notOnlySelf.AttachCardTargetCriteria(W30Spec(10, friendly: 1, enemy: 0, hero: 1, minions: 1, remnants: 1,
                                                         keyword: 0, excludeNotSelf: 0), true);
            var ctxB2 = Battle(new[] { notOnlySelf }, new CardDef[0]);
            ToP1Turn(ctxB2, 2);
            var fB2 = Place(ctxB2, 0, 2, Unit("W30B2Friend", 1, 0, 10), exhausted: true);
            CheckTrue(fB2 != null && fB2.IsAlive, "前提 B2a：对照局的友军部队在场上");
            var poolB2 = RuleCore.PlayTargetPool(ctxB2, 0, notOnlySelf);
            CheckTrue(poolB2 != null && poolB2.Contains(fB2),
                      "★ B2【灭自证 · 与 B1 并排】：只把 `excludeNotSelf` 翻成 0 ⇒ 同一个候选必须**留在池里**"
                    + "｜🧨 写成「恒剔」⇒ B2 红；不接 ⇒ B1 红");
        }

        // ======================================================================================
        //  C · `PlayTargetPool` 的预览单位（待落地那张自己）：`includeSelf` 那一格
        // ======================================================================================
        {
            var uNoSelf = W30RallyUnit("W30NoSelf", includeSelf: 0);
            var ctxC = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctxC, 2);
            var instC = ctxC.NewInstance(uNoSelf);
            var poolC = RuleCore.PlayTargetPoolAsIfDeployed(ctxC, 0, instC);
            CheckTrue(poolC != null, "前提 C0：`PlayTargetPoolAsIfDeployed` 有返回");
            // 机制前提：预览不在棋盘 ⇒ `SideOf` 返 -1 ⇒ 逐候选谓词只能给 `null`（判不了）
            string whyC;
            var probe = new UnitState(instC, false);
            var verdict = RuleCore.CardTargetCriteriaAccepts(ctxC, 0, uNoSelf, probe, out whyC);
            CheckTrue(verdict == null, "★ 前提 C1：预览不在棋盘 ⇒ 卡级 criteria 的逐候选谓词给 `null`（判不了）—— "
                                     + "这就是「其余 35 格接不上、要 `TargetCriteriaEval` 开 side 钩子」的原因；实得 "
                                     + (verdict == null ? "null" : verdict.ToString()) + "（" + (whyC ?? "ok") + "）");
            Check(poolC != null ? poolC.Count : -1, 0, "★ C2【结论】：`includeSelf = 0` ⇒ 预览的「它自己」**不进池**"
                                                     + "（原版落位后它也在候选里、`CheckIfMeetsCriteria.c:236-238` 会剔掉它）"
                                                     + "｜🧨 `!PreviewSelfExcludedByCardCriteria(card)` 那一句摘掉 ⇒ 这一格变 1、C2 红");
        }
        {
            // 对照：**同一张牌**、只把 `includeSelf` 从 0 翻成 1
            var uSelf = W30RallyUnit("W30Self", includeSelf: 1);
            var ctxC2 = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctxC2, 2);
            var instC2 = ctxC2.NewInstance(uSelf);
            var poolC2 = RuleCore.PlayTargetPoolAsIfDeployed(ctxC2, 0, instC2);
            CheckTrue(poolC2 != null && poolC2.Count > 0,
                      "★ C3【灭自证 · 与 C2 并排】：只把 `includeSelf` 翻成 1 ⇒ 预览**进池**（说明 C2 不是「池恒空」）"
                    + "｜🧨 这条闸写成恒剔 ⇒ C3 红；不接 ⇒ C2 红");
        }
    }

    // ==========================================================================================
    //  夹具
    // ==========================================================================================
    /// <summary>37 格填满「全已知」的卡级 criteria（形状与生成器 `card_target_criteria_set_of` 一致）。
    /// ⚠️ `playerTurn` 恒 `Unknown(-1)` —— 原版 `TargetCriteria` 结构上**没有**那一格。</summary>
    static CardDef.CountSetSpec W30Spec(int ta, int friendly, int enemy, int hero, int minions, int remnants,
                                        int keyword = 0, int includeSelf = 0, int excludeNotSelf = 0)
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = CardDef.CountSetSpec.Unknown,
            TargetsAffected = ta,
            ChosenArmy = 0,
            ArmyFilter = 0,
            KeywordFilter = keyword,
            ExcludeRace = 0,
            TraitsFilter = 0,
            ExcludeTrait = 0,
            MaxTargets = 0,
            TargetSelectMethod = 0,
            AttackFilter = 0,
            AttackFilterOption = 0,
            CurrentHealthFilter = 0,
            CurrentHealthFilterOption = 0,
            ManaCostFilter = 0,
            ManaCostFilterOption = 0,
            RarityFilter = 0,
            RarityFilterOption = 0,
            SpellType = 0,
            FriendlyUnits = friendly,
            EnemyUnits = enemy,
            IncludeHero = hero,
            IncludeMinions = minions,
            IncludeSpells = 0,
            IncludeWhispers = 0,
            IncludeWeapons = 0,
            IncludeRemnants = remnants,
            IncludeHeroParts = 0,
            IncludeSelf = includeSelf,
            ExcludeNotSelf = excludeNotSelf,
            ExcludeSimilarCards = 0,
            ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0,
            ExcludeEnemyHero = 0,
            SelectFromChooseResolution = 0,
            FilterSpecificCardIds = new string[0],
            ExcludeEnchantmentIds = new string[0],
        };
    }

    /// <summary>`KeywordId` 显式给值的普通部队（`Keyword.Vehicle = 10` / `Keyword.Infantry = 5`）。</summary>
    static CardDef W30PlainUnit(string name, int keywordId)
    {
        var c = new CardDef(name, name, "unit", "", "common", "Test", 1, 0, 20, 0, null, subtype: "Infantry");
        c.AttachKeywordId(keywordId);
        return c;
    }

    /// <summary>带突触的部队（锚点）。`keywordId` 同时给 `KeywordId` —— 它自己也要过 criteria（它是被选中的那个目标）。</summary>
    static CardDef W30SynapseUnit(string name, int keywordId)
    {
        var c = new CardDef(name, name, "unit", "", "common", "Test", 1, 0, 20, 0,
                            new[] { KeywordTable.Synapse }, subtype: "Vehicle");
        c.AttachKeywordId(keywordId);
        return c;
    }

    /// <summary>落地后带 Rally 目标的单位卡（`targeted = 1`）—— 用来验「待落地那张自己」那一格。</summary>
    static CardDef W30RallyUnit(string name, int includeSelf)
    {
        var c = new CardDef(name, name, "unit", "Rally: Heal 2 to a friendly unit", "common", "Test",
                            0, 1, 5, 0, null, subtype: "Infantry", targeted: 1);
        c.AttachCardTargetCriteria(W30Spec(10, friendly: 1, enemy: 0, hero: 0, minions: 1, remnants: 1,
                                           keyword: 0, includeSelf: includeSelf), true);
        return c;
    }
}
