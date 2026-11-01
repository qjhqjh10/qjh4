// B114_W34_Assert.cs — 第一百一十四会话 · 写手 `W-34` · 账 `B-270` + `B-271`
//   `PlayTargetPool` 的 `asIfOnBoard` 预览单位（= 待落地那张自己）那一路的两件：
//     ① `B-270`（真缺陷）：预览**不看 `spec.Side`** —— 侧筛只在 `AddSide` 里做，预览直接进 `probe`
//        ⇒ 35 张「敌方目标」的单位卡会把自己（友军）算进「要不要问」的判定。今天被 `includeSelf == 0`
//        **顺带**挡住，但那是**巧合不是判据**（两格是不同的维度）。
//     ② `B-271`：预览应过**整道** criteria 闸（原版是**落位之后**问：
//        `_ResolvePlayCardFromHand_d__447__MoveNext.c:849-860` → `IsThereValidUnitTarget`
//        → `RawCardScript__GetValidTargets.c:31-45` 扫落位后的棋盘、逐候选 `IsValidTarget`）。
//        改前只有「不依赖棋盘」的 `includeSelf` 一格；其余格因为 `SideOf` 恒 -1 全变「判不了」。
//
// ============================ 🔴 灭自证（什么改动能让它红）============================
//  · A1 ：把 probe 那一句的 `ownSideInPool &&` 摘掉 ⇒ 敌方目标的卡又把「友军的自己」并进池 ⇒ A1 红。
//         A2 是**同一形状、只把句子的侧换成 `friendly`** 的对照（说明 A1 不是「池恒空」）。
//  · B3 ：把 probe 那一趟新加的 `ApplyCardTargetCriteriaFilter(..., thisCard: asIfOnBoard, sideOverride: owner)`
//         摘掉 ⇒ 整道闸只剩 `includeSelf` ⇒ 预览留在池里 ⇒ B3 红。
//         B4 是**同一张牌、只把 `keywordFilter` 换成预览自己那个兵种**的对照。
//  · C1 ：把 `!PreviewSelfExcludedByCardCriteria(card)` 摘掉（= 删掉那条老判据）⇒ C1 红。
//         C2 是同一张牌只翻 `includeSelf` 一位的对照。
//
// ============================ 怎么接（宿主 = `RuleEngine/Editor/RuleEngineTest.cs`）============================
//  在 `Run()` 的聚合行（**同一物理行**、行尾 `//` **之前**）追加：
//      Guard("CheckB114W34PreviewSideAndFullGate", CheckB114W34PreviewSideAndFullGate);
//  ⚠️ 本仓已 6 次踩「追加落到行尾 `//` 后面 ⇒ 代码被注释吞掉、断言一条没跑」。
//  ⚠️ 入口名**不以 `Test` / `Report` 打头**（`VerifyRunCaseCoverage` / `gen_diag_runner` 按那两个前缀认用例）。
//
// ============================ 副作用 ============================
//  · 只读卡表 + 只在**自建的两局**里造实例；不写存档、不动全局状态、不跑回合。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>`B-270` / `B-271`：预览单位的**侧筛**与**整道 criteria 闸**。</summary>
    public static void CheckB114W34PreviewSideAndFullGate()
    {
        // ======================================================================================
        //  0 · 数据面前提（这两件的可达面各靠一条不变量）
        // ======================================================================================
        var db = CardDatabase.Load();
        CheckTrue(db != null && db.Count > 1000, "前提 0：卡表装上了（" + (db != null ? db.Count : -1) + " 张）");
        int foeN = 0, foeNotIncludeSelf = 0, ownN = 0, ownIncludeSelf0 = 0, ownIncludeSelf1 = 0;
        for (int i = 0; i < db.Count; i++)
        {
            var c = db[i];
            if (c == null || !c.IsUnit || c.Targeted != 1) continue;
            var set = c.CardTargetCriteria;
            if (!c.CardTargetCriteriaVersioned || set == null || set.TargetsAffected != 10) continue;
            if (set.FriendlyUnits == 0)
            {
                foeN++;
                if (set.IncludeSelf == 0) foeNotIncludeSelf++;
            }
            else if (set.FriendlyUnits == 1)
            {
                ownN++;
                if (set.IncludeSelf == 0) ownIncludeSelf0++;
                else if (set.IncludeSelf == 1) ownIncludeSelf1++;
            }
        }
        CheckTrue(foeN >= 1, "★ 前提 0a：`targeted==1 · ta==10 · friendlyUnits==0` 的**单位卡**有 " + foeN + " 张"
                           + "（这条侧筛的可达面；`B114_W30` 报告 §六·1 现读 35 张）");
        Check(foeNotIncludeSelf, foeN, "★ 前提 0b：这批敌方目标的卡**全部** `includeSelf == 0`（实得 " + foeNotIncludeSelf + "/" + foeN + "）"
                                      + " —— 所以 `B-270` 那个缺陷今天被**顺带**挡住；⛔ 那是巧合不是判据");
        CheckTrue(ownIncludeSelf0 >= 1 && ownIncludeSelf1 >= 1,
                  "★ 前提 0c：友方目标那批（" + ownN + " 张）里 `includeSelf == 0` 的 " + ownIncludeSelf0
                + " 张、`== 1` 的 " + ownIncludeSelf1 + " 张 —— C 段那个对照要这两档都在");

        // ======================================================================================
        //  A · `B-270`：预览也过 `spec.Side`（敌方目标的卡不该把「友军的自己」算进来）
        // ======================================================================================
        {
            var foeCard = W34Unit("W34SideFoe", "Rally: Deal 2 damage to an enemy unit", null, keywordId: 5);
            var ctxA = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctxA, 2);
            var instA = ctxA.NewInstance(foeCard);
            var prevA = new UnitState(instA, false);

            var opsA = EffectText.Parse(foeCard.Desc, out _, out _, card: foeCard);
            var specA = EffectText.PickTarget(opsA);
            CheckTrue(specA != null && specA.Side == "enemy",
                      "前提 A0：夹具的句形解析出 `Side == \"enemy\"` 的选目标规格（实得 "
                    + (specA == null ? "null" : specA.Side) + "）—— 解析不出来本段测的就不是侧筛");
            CheckTrue(prevA != null && prevA.IsAlive && !prevA.IsWarlord,
                      "前提 A0b：预览单位活着、且不是督军（否则 `PlayTargetPool` 的 `troopOnly && IsWarlord` 闸先挡）");

            var poolA = RuleCore.PlayTargetPool(ctxA, 0, foeCard, prevA);
            CheckTrue(poolA != null, "前提 A0c：`PlayTargetPool` 有返回");
            CheckTrue(!poolA.Contains(prevA),
                      "★ A1【结论 · `B-270`】：**敌方目标**的卡 ⇒ 预览（友军那一侧的自己）**不进池**"
                    + "（判据 = 池子本来就是按 `spec.Side` 建的：`AddSide` 只在 own/any 时才加出牌者那一侧）"
                    + "｜🧨 把 probe 那一句的 `ownSideInPool &&` 摘掉 ⇒ 这一格变成「在池里」、A1 红");
        }
        {
            // 对照：**同一形状**，只把句子的侧换成 `friendly`
            var ownCard = W34Unit("W34SideOwn", "Rally: Heal 2 to a friendly unit", null, keywordId: 5);
            var ctxB = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctxB, 2);
            var instB = ctxB.NewInstance(ownCard);
            var prevB = new UnitState(instB, false);
            var opsB = EffectText.Parse(ownCard.Desc, out _, out _, card: ownCard);
            var specB = EffectText.PickTarget(opsB);
            CheckTrue(specB != null && specB.Side == "own",
                      "前提 A2a：对照那句解析出 `Side == \"own\"`（实得 " + (specB == null ? "null" : specB.Side) + "）");
            var poolB = RuleCore.PlayTargetPool(ctxB, 0, ownCard, prevB);
            CheckTrue(poolB != null && poolB.Contains(prevB),
                      "★ A2【灭自证 · 与 A1 并排】：只把侧换成 `friendly` ⇒ 预览**必须进池**"
                    + "（说明 A1 不是「池恒空/预览永远进不去」）"
                    + "｜🧨 侧判据写成恒剔 ⇒ A2 红；不接 ⇒ A1 红");
        }

        // ======================================================================================
        //  B · `B-271`：预览过**整道** criteria 闸（不只是 `includeSelf` 那一格）
        // ======================================================================================
        {
            // 预览自己 = Infantry(5)，criteria 要 Vehicle(10) ⇒ 整道闸应把它淘汰
            var keyFoe = W34Unit("W34GateMiss", "Rally: Heal 2 to a friendly unit",
                                 W34Spec(friendly: 1, enemy: 0, includeSelf: 1, keyword: 10), keywordId: 5);
            var ctxC = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctxC, 2);
            var instC = ctxC.NewInstance(keyFoe);
            var prevC = new UnitState(instC, false);

            CheckTrue(keyFoe.CardTargetCriteria != null && keyFoe.CardTargetCriteria.KeywordFilter == 10,
                      "前提 B0：夹具挂上了卡级 criteria（`ta=10 · friendlyUnits=1 · includeSelf=1 · keywordFilter=Vehicle(10)`）");
            CheckTrue(prevC.Card != null && prevC.Card.KeywordId == 5,
                      "前提 B0b：预览自己的 `KeywordId == Infantry(5)`（实得 " + (prevC.Card != null ? prevC.Card.KeywordId : -1) + "）");

            // 机制前提：老口径（不传 thisCard / sideOverride）**判不了** ⇒ 整条不筛 ⇒ 预览会被并进去
            string whyOld;
            bool? vOld = RuleCore.CardTargetCriteriaAccepts(ctxC, 0, keyFoe, prevC, out whyOld);
            CheckTrue(vOld == null,
                      "★ 前提 B1：不传 `thisCard`/`sideOverride` ⇒ 逐候选谓词给 `null`（判不了）—— 这就是改前"
                    + "「其余 35 格接不上」的机制；实得 " + (vOld == null ? "null" : vOld.ToString())
                    + "（" + (whyOld ?? "ok") + "）");
            // 新口径：thisCard = 它自己、侧 = 出牌者 ⇒ 判得出（keyword 那一格把它淘汰）
            string whyNew;
            bool? vNew = RuleCore.CardTargetCriteriaAccepts(ctxC, 0, keyFoe, prevC, out whyNew, prevC, 0);
            CheckTrue(vNew == false,
                      "★ 前提 B2：传 `thisCard = 它自己` + `sideOverride = 0` ⇒ 判得出 `false`（`keywordFilter` 不收 Infantry）"
                    + "；实得 " + (vNew == null ? "null" : vNew.ToString()) + "（" + (whyNew ?? "ok") + "）");

            var poolC = RuleCore.PlayTargetPool(ctxC, 0, keyFoe, prevC);
            CheckTrue(poolC != null && !poolC.Contains(prevC),
                      "★ B3【结论 · `B-271`】：预览过**整道** criteria ⇒ 被判「关键词不符」淘汰、**不进池**"
                    + "｜🧨 把 probe 那一趟新加的 `ApplyCardTargetCriteriaFilter(..., thisCard, sideOverride)` 摘掉 ⇒"
                    + " 整道闸只剩 `includeSelf` ⇒ 预览留在池里、B3 红");
        }
        {
            // 对照：**同一张牌**，只把 `keywordFilter` 换成预览自己那个兵种
            var keyHit = W34Unit("W34GateHit", "Rally: Heal 2 to a friendly unit",
                                 W34Spec(friendly: 1, enemy: 0, includeSelf: 1, keyword: 5), keywordId: 5);
            var ctxD = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctxD, 2);
            var instD = ctxD.NewInstance(keyHit);
            var prevD = new UnitState(instD, false);
            string whyD;
            bool? vD = RuleCore.CardTargetCriteriaAccepts(ctxD, 0, keyHit, prevD, out whyD, prevD, 0);
            CheckTrue(vD == true,
                      "★ 前提 B4a：对照那张（`keywordFilter = Infantry(5)`）判得出 `true`；实得 "
                    + (vD == null ? "null" : vD.ToString()) + "（" + (whyD ?? "ok") + "）");
            var poolD = RuleCore.PlayTargetPool(ctxD, 0, keyHit, prevD);
            CheckTrue(poolD != null && poolD.Contains(prevD),
                      "★ B4【灭自证 · 与 B3 并排】：只把 `keywordFilter` 换成预览自己那个兵种 ⇒ 预览**进池**"
                    + "（说明 B3 的差异只可能来自那道 criteria，不是别的闸）"
                    + "｜🧨 把整道闸写成恒剔 ⇒ B4 红");
        }

        // ======================================================================================
        //  C · 老判据仍在：`includeSelf == 0` 那条窄口（⛔ 别在接整闸时把它删了）
        //   选一个**整道闸判不了、只有窄口能判**的形状：`friendlyUnits` 未知 ⇒ `Matches` 返 `null`
        //   ⇒ 整条不筛 ⇒ 只有 `PreviewSelfExcludedByCardCriteria` 能把预览剔掉。
        // ======================================================================================
        {
            var narrowNo = W34Unit("W34NarrowNo", "Rally: Heal 2 to a friendly unit",
                                   W34Spec(friendly: CardDef.CountSetSpec.Unknown, enemy: 0, includeSelf: 0), keywordId: 5);
            var ctxE = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctxE, 2);
            var instE = ctxE.NewInstance(narrowNo);
            var prevE = new UnitState(instE, false);
            string whyE;
            bool? vE = RuleCore.CardTargetCriteriaAccepts(ctxE, 0, narrowNo, prevE, out whyE, prevE, 0);
            CheckTrue(vE == null,
                      "★ 前提 C0：这张的 `friendlyUnits` 未知 ⇒ 整道闸判不了（`null`）⇒ 那次筛**整条不生效**"
                    + "；实得 " + (vE == null ? "null" : vE.ToString()) + "（" + (whyE ?? "ok") + "）");
            var poolE = RuleCore.PlayTargetPool(ctxE, 0, narrowNo, prevE);
            CheckTrue(poolE != null && !poolE.Contains(prevE),
                      "★ C1【结论】：整道闸判不了时，**老判据 `includeSelf == 0` 仍然把预览剔掉**"
                    + "（⛔ 别把它当成冗余删掉）"
                    + "｜🧨 把 probe 那一句的 `!PreviewSelfExcludedByCardCriteria(card)` 摘掉 ⇒ 预览留在池里、C1 红");
        }
        {
            // 对照：同一张牌只翻 `includeSelf` 一位
            var narrowYes = W34Unit("W34NarrowYes", "Rally: Heal 2 to a friendly unit",
                                    W34Spec(friendly: CardDef.CountSetSpec.Unknown, enemy: 0, includeSelf: 1), keywordId: 5);
            var ctxF = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctxF, 2);
            var instF = ctxF.NewInstance(narrowYes);
            var prevF = new UnitState(instF, false);
            var poolF = RuleCore.PlayTargetPool(ctxF, 0, narrowYes, prevF);
            CheckTrue(poolF != null && poolF.Contains(prevF),
                      "★ C2【灭自证 · 与 C1 并排】：只把 `includeSelf` 翻成 1 ⇒ 预览**进池**（说明 C1 不是「池恒空」）"
                    + "｜🧨 窄口写成恒剔 ⇒ C2 红；不接窄口 ⇒ C1 红");
        }
    }

    // ==========================================================================================
    //  夹具
    // ==========================================================================================
    /// <summary>落地后带一个选目标正文的单位卡（`targeted = 1`）。`criteria = null` ⇒ 不挂卡级 criteria。</summary>
    static CardDef W34Unit(string name, string desc, CardDef.CountSetSpec criteria, int keywordId)
    {
        var c = new CardDef(name, name, "unit", desc, "common", "Test",
                            0, 1, 5, 0, null, subtype: "Infantry", targeted: 1);
        c.AttachKeywordId(keywordId);
        if (criteria != null) c.AttachCardTargetCriteria(criteria, true);
        return c;
    }

    /// <summary>37 格填「全已知」的卡级 criteria（形状与 `B114_W30` 的 `W30Spec` 一致；`
    /// ⚠️ 本夹具与 `W30Spec` 分开写，是为了让**本批**的对照只改一个维度、不动 W30 的夹具
    /// （改它会连带影响 W30 输出读数）。</summary>
    static CardDef.CountSetSpec W34Spec(int friendly, int enemy, int includeSelf, int keyword = 0)
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = CardDef.CountSetSpec.Unknown,
            TargetsAffected = 10,
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
            IncludeHero = 1,
            IncludeMinions = 1,
            IncludeSpells = 0,
            IncludeWhispers = 0,
            IncludeWeapons = 0,
            IncludeRemnants = 1,
            IncludeHeroParts = 0,
            IncludeSelf = includeSelf,
            ExcludeNotSelf = 0,
            ExcludeSimilarCards = 0,
            ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0,
            ExcludeEnemyHero = 0,
            SelectFromChooseResolution = 0,
            FilterSpecificCardIds = new string[0],
            ExcludeEnchantmentIds = new string[0],
        };
    }
}
