// B114_A_Assert.cs — 第一百一十四会话 · 写手 `W-A` · 账 `B-177`
//   「点选/轻判路**真的**消费卡级 `cardTargetCriteria`」（原版 `RawCardScript.cardTargetCriteria +0x280`）
//
// ============================ 它盯的是哪一格 ============================
//  判据（第一权威）：`d:/2/tools/decomp_full/RawCardScript__IsValidTarget.c:8-44`
//    · `.c:13-15`  `cardTargetCriteria == null` ⇒ `LogError`（原版防御性死支：池内 1129/1129 全有，`B-178` 已闭合）
//    · `.c:22-42`  目标必须 `IsInPlay`；ward / camouflage(`施放者 cardType == 0x14`) / stealth(70) 三条「异侧不可指」
//    · `.c:43-44`  末句 = `FilterMethods__CheckIfMeetsCriteria(thisCard, 目标, 目标, *(raw + 0x280))`
//  ⇒ 语义 = 「**这张卡打出时能点谁**」。求值器 = `TargetCriteriaEval.Matches`（唯一实现）。
//  我们的消费点（本轮接的）：
//    · `EffectResolver.cs:2122`（`IsLegalPick` 的池，`CanPlayTactic:2088` 进）
//    · `EffectResolver.cs:2189`（`PlayTargetPool` 的池，`IsLegalPlayTarget` 进）
//  载体 = `CardDef.CardTargetCriteria` / `CardTargetCriteriaVersioned`（`CardDef.cs:9484/9488`）。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//  · A1/A2 ：把卡级列摘掉（`AttachCardTargetCriteria(null, false)`）⇒ `Accepts` 回 `null` ⇒ A1 红（变成「不筛」，
//             而不是「按原版筛」）；把 `targeted` 改成 0 ⇒ 走「原版根本不调 IsValidTarget」那一支 ⇒ A2 红。
//  · B1/B2 ：这一对**结构上不可能同时满足**：`pAny`（无 keyword 闸）收 Infantry、`pVeh`（keyword=Vehicle）
//             不收 —— 闸被写成「恒放行」⇒ B2 红；写成「恒剔」/「把 keyword 方向写反」⇒ B1 红。
//  · C1/C2 ：`CanPlayTactic` 上同一张卡、同一块棋盘、只换候选：Vehicle ⇒ `OK`、Infantry ⇒ `ErrNoTargetAvailable`。
//             **这一对只可能由卡级 criteria 产生**（两个候选同侧、同兵种类「unit」、同 `includeMinions`）
//             ⇒ 「点选路没接上」时两条都变 `OK` ⇒ C2 红；「接成了全剔」⇒ C1 红。
//  · D1    ：EC46 的数据面 —— 若有人把消费源换成 **logic 级** `AbilityLogic.targetCriteria`
//             （那对 EC46 是 `enemyUnits=1`，即**描述被打的那个敌人**），D1 红。
//             🔴 这正是 `B113_G4` §3·5 那次推翻的错法（实测把 EC46 打成 `ErrNoTargetAvailable`）。
//  · E1/E2 ：`includeRemnants` 一位：同一对象只翻 `IsRemnant` ⇒ 结论必须跟着翻（写成恒值必红）。
//  · F1/F2 ：`includeHero` 一位：敌侧 «督军» 在 `includeHero=0` 下必须被剔、在 `1` 下必须进
//             （把卡种白名单写成「只看 minions」或「恒不看卡种」都只能对一半）。
//
// ============================ 怎么接（宿主 = `RuleEngine/Editor/RuleEngineTest.cs`）============================
//  在 `Run()` 的聚合行（**同一物理行**、行尾 `//` **之前**）追加：
//      Guard("CheckB114ACardTargetCriteriaPick", CheckB114ACardTargetCriteriaPick);
//  ⚠️ 本仓已 6 次踩「追加落到行尾 `//` 后面 ⇒ 代码被注释吞掉、断言一条没跑」。
//  ⚠️ 入口名**不以 `Test` / `Report` 打头**（`VerifyRunCaseCoverage` / `gen_diag_runner` 按那两个前缀认用例）。
//
// ============================ 副作用 ============================
//  · 只读卡表 + 只在**自建的两局**里摆牌；不写存档、不动全局状态、不跑回合。
//  · E 段会临时把一只夹具单位置 `IsRemnant = true`，**立刻翻回**（同一函数内，异常路径也走 `finally`）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>`B-177`：卡级 `cardTargetCriteria` 必须在**点选/轻判路**被真正消费（不是「只落数据」）。
    /// <para>第一验收条 = **`EC46 Peerless Bladesmen` 仍能打出**（旧施工单被推翻正是因为它把 EC46 打成打不出去）。</para></summary>
    public static void CheckB114ACardTargetCriteriaPick()
    {
        var db = CardDatabase.Load();
        CheckTrue(db != null && db.Count > 1000, "前提 0：卡表装上了（" + (db != null ? db.Count : -1) + " 张）");

        // ======================================================================================
        //  A · 数据面前提（把「我们读的是卡级那一份、且只有 ta=10 可达」钉死）
        // ======================================================================================
        int total = 0, noCrit = 0, notVersioned = 0, targetedTaNot10 = 0, targetedN = 0;
        int taOther = 0;
        for (int i = 0; i < db.Count; i++)
        {
            var c = db[i];
            if (c == null) continue;
            total++;
            if (c.CardTargetCriteria == null) noCrit++;
            if (!c.CardTargetCriteriaVersioned) notVersioned++;
            if (c.CardTargetCriteria != null)
            {
                int ta = c.CardTargetCriteria.TargetsAffected;
                if (ta != 0 && ta != 10 && ta != 180) taOther++;
                if (c.Targeted == 1)
                {
                    targetedN++;
                    if (ta != 10) targetedTaNot10++;
                }
            }
        }
        Check(total - noCrit, total, "★ 前提 A0：**每一张卡**都有卡级 `cardTargetCriteria`（`null` 张数 = " + noCrit + "）");
        Check(total - notVersioned, total, "★ 前提 A0b：`CardTargetCriteriaVersioned` 全为真（旧表 < v46 才会是假；假 = " + notVersioned + " 张）");
        Check(taOther, 0, "★ 前提 A0c：`targetsAffected` 只出现 {0, 10, 180} —— 其他档走 `TargetCriteriaEval` 的特例分支，"
                        + "点选路今天一格都不命中（实得越界 " + taOther + " 张）。⛔ 越界 ⇒ 回来看那一份 switch");
        Check(targetedTaNot10, 0, "★ 前提 A0d：**`targeted == 1` 的卡**（" + targetedN + " 张）`targetsAffected` 恒 10 —— "
                                + "「点选体只为 ta=10 跑」这条前提一破，就必须先回来看 `TargetCriteriaEval`（实得例外 " + targetedTaNot10 + " 张）");

        // 逐候选谓词的夹具：一块只有「我方 Vehicle / 我方 Infantry / 敌方督军」的干净棋盘
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        var vehCard = CardDatabase.FindById(db, "ASH61");     // Weapons Platform：`keyword = 10 (Vehicle)`
        var infCard = CardDatabase.FindById(db, "ASH77");     // Death Spinner Warp Spider：`keyword = 5 (Infantry)`
        CheckTrue(vehCard != null && infCard != null && vehCard.KeywordId == 10 && infCard.KeywordId == 5,
                  "前提 A1：两张真卡的 `keyword` 列读得到（Vehicle=10 / Infantry=5）");
        var ownVeh = Place(ctx, 0, 2, vehCard);
        var ownInf = Place(ctx, 0, 3, infCard);
        var foeWarlord = ctx.Players[1].Warlord;
        CheckTrue(ownVeh != null && ownInf != null && foeWarlord != null,
                  "前提 A2：我方 Vehicle / 我方 Infantry / 敌方督军都在场");

        // ======================================================================================
        //  B · 逐候选结论（`CardTargetCriteriaAccepts` = 点选路用的**同一个**谓词）
        // ======================================================================================
        var pAny = B114AProbe("B114AAny", B114ASpec(10, 1, 0, 1, 1, 0, keyword: 0));
        var pVeh = B114AProbe("B114AVeh", B114ASpec(10, 1, 0, 1, 1, 0, keyword: 10));
        CheckTrue(pAny.CardTargetCriteriaVersioned && pAny.CardTargetCriteria != null
                  && pVeh.CardTargetCriteriaVersioned && pVeh.CardTargetCriteria != null,
                  "前提 A3：两张夹具卡挂上了卡级 criteria（`AttachCardTargetCriteria(set, true)`）");

        string why;
        CheckTrue(RuleCore.CardTargetCriteriaAccepts(ctx, 0, pAny, ownInf, out why) == true,
                  "★ B1：`keywordFilter = 0`（不筛兵种）⇒ 我方 Infantry **收**（" + (why ?? "ok") + "）");
        CheckTrue(RuleCore.CardTargetCriteriaAccepts(ctx, 0, pVeh, ownVeh, out why) == true,
                  "★ B0：`keywordFilter = Vehicle` ⇒ 我方 Vehicle **收**（" + (why ?? "ok") + "）");
        CheckTrue(RuleCore.CardTargetCriteriaAccepts(ctx, 0, pVeh, ownInf, out why) == false,
                  "★ B2【灭自证 · 与 B1 并排】：同侧同兵种类、只差 `keywordFilter` ⇒ 我方 Infantry **剔**"
                + "｜🧨 闸写成「恒放行」⇒ 本行红；写成「恒剔」⇒ B0/B1 红（" + (why ?? "") + "）");

        // C · `includeHero` 一位（两侧并排）
        var pFoeNoHero = B114AProbe("B114AFoeNoHero", B114ASpec(10, 0, 1, 0, 1, 0));
        var pFoeHero = B114AProbe("B114AFoeHero", B114ASpec(10, 0, 1, 1, 1, 0));
        CheckTrue(RuleCore.CardTargetCriteriaAccepts(ctx, 0, pFoeNoHero, foeWarlord, out why) == false,
                  "★ F1：`includeHero = 0` ⇒ 敌方**督军**被剔（原版 `:282`：候选 `cardType == 10` 而那一格为假）");
        CheckTrue(RuleCore.CardTargetCriteriaAccepts(ctx, 0, pFoeHero, foeWarlord, out why) == true,
                  "★ F2【灭自证 · 与 F1 并排】：只把 `includeHero` 置 1 ⇒ 同一个候选必须**收**"
                + "｜🧨 卡种白名单写成恒真/恒假都只能对一半（" + (why ?? "ok") + "）");

        // E · `includeRemnants` 一位（同一对象只翻一位）
        var pNoRem = B114AProbe("B114ANoRem", B114ASpec(10, 1, 0, 1, 1, 0));
        bool wasRemnant = ownInf.IsRemnant;
        bool? e1, e2;
        try
        {
            ownInf.IsRemnant = true;
            e1 = RuleCore.CardTargetCriteriaAccepts(ctx, 0, pNoRem, ownInf, out why);
            ownInf.IsRemnant = false;
            e2 = RuleCore.CardTargetCriteriaAccepts(ctx, 0, pNoRem, ownInf, out why);
        }
        finally { ownInf.IsRemnant = wasRemnant; }
        CheckTrue(e1 == false, "★ E1：`includeRemnants = 0` ⇒ 残骸**剔**（原版 `:273-278`）");
        CheckTrue(e2 == true, "★ E2【灭自证 · 与 E1 并排】：同一个对象只把 `IsRemnant` 翻回假 ⇒ 必须**收**"
                            + "｜🧨 这条闸写成恒值（恒剔/恒放）只能对一半");

        // A1 · 列未知 ⇒ 「判不了」（不是「不成立」）：手写夹具没有这一列
        var plain = new CardDef("B114APlain", "B114APlain", "tactic", "Give Shuriken 2 to a friendly unit",
                                "common", "Test", 0, 0, 0, 0, null, targeted: 1);
        CheckTrue(!plain.CardTargetCriteriaVersioned && plain.CardTargetCriteria == null,
                  "前提 A4：手写夹具卡**没有**卡级 criteria（= 旧表 / 夹具的形状）");
        CheckTrue(RuleCore.CardTargetCriteriaAccepts(ctx, 0, plain, ownInf, out why) == null,
                  "★ A5：列未知 ⇒ 返回 `null`（**判不了 ⇒ 不筛**）。⛔ 写成 `false` 会把所有旧表/夹具的卡判死");

        // ======================================================================================
        //  C · 集成：**`CanPlayTactic` 那条链**上真的在筛（第一验收条 + 铁证）
        // ======================================================================================
        var pAny2 = B114AProbe("B114APlayAny", B114ASpec(10, 1, 0, 1, 1, 0, keyword: 0));
        var pVeh2 = B114AProbe("B114APlayVeh", B114ASpec(10, 1, 0, 1, 1, 0, keyword: 10));
        var ctx2 = Battle(new[] { pAny2, pVeh2 }, new CardDef[0]);
        ToP1Turn(ctx2, 3);
        ctx2.Players[0].Energy = 20;
        var cVeh = Place(ctx2, 0, 2, vehCard);
        var cInf = Place(ctx2, 0, 3, infCard);
        int hAny = HandIdx(ctx2, 0, "B114APlayAny"), hVeh = HandIdx(ctx2, 0, "B114APlayVeh");
        CheckTrue(hAny >= 0 && hVeh >= 0, "前提 C0：两张夹具卡都在手里（" + hAny + " / " + hVeh + "）"
                                       + LogTail(ctx2));
        CheckTrue(cVeh != null && cInf != null && cVeh.IsAlive && cInf.IsAlive,
                  "前提 C0b：我方 Vehicle / Infantry 在场且活着");

        CheckCode(RuleCore.CanPlayTactic(ctx2, 0, hAny, -1, cInf), RuleCodes.OK,
                  "★ C0c：`keywordFilter = 0` 的那一份 ⇒ 指 Infantry 打出 `OK`（尺子：同一块棋盘、同一张正文）"
                + LogTail(ctx2));
        CheckCode(RuleCore.CanPlayTactic(ctx2, 0, hVeh, -1, cVeh), RuleCodes.OK,
                  "★ C1：`keywordFilter = Vehicle` ⇒ 指我方 Vehicle 打出 `OK`");
        CheckCode(RuleCore.CanPlayTactic(ctx2, 0, hVeh, -1, cInf), RuleCodes.ErrNoTargetAvailable,
                  "★ C2【灭自证 · 与 C1 并排】：同卡同棋盘、只把候选换成 Infantry ⇒ `ErrNoTargetAvailable`"
                + "｜🧨 这一对**只可能**由卡级 criteria 产生（两个候选同侧、同 `includeMinions`、同 `includeHero`）"
                + "⇒ 点选路没接上时两条都变 `OK`（C2 红）；接成「全剔」⇒ C1 红" + LogTail(ctx2));

        // ======================================================================================
        //  D · `EC46` —— **第一验收条**：仍能打出；且消费的是**卡级**那一份
        // ======================================================================================
        var ec46 = CardDatabase.FindById(db, "EC46");
        CheckTrue(ec46 != null, "前提 D0：`EC46 Peerless Bladesmen` 在卡表里");
        if (ec46 != null)
        {
            CheckTrue(ec46.CardTargetCriteria != null && ec46.CardTargetCriteriaVersioned,
                      "前提 D0b：EC46 的卡级 criteria 有值（与 `B-178` 的读数一致：池内 1,351/1,351 全有）");
            if (ec46.CardTargetCriteria != null)
                CheckTrue(ec46.CardTargetCriteria.FriendlyUnits == 1 && ec46.CardTargetCriteria.EnemyUnits == 0
                          && ec46.CardTargetCriteria.TargetsAffected == 10,
                          "★ D1：EC46 的**卡级**那一份 = `friendlyUnits=1 / enemyUnits=0 / ta=10`"
                        + "（卡面「Target **friendly** unit …」自洽）｜🧨 若有人把消费源换成 **logic 级**"
                        + "`AbilityLogic.targetCriteria`（那对 EC46 是 `enemyUnits=1`，描述的是**被打的那个敌人**）"
                        + "⇒ `PlayTargetPool` 会把友军全剔、D2 红 —— 这正是 `B113_G4` §3·5 推翻的错法");

            var ctx3 = Battle(new[] { ec46 }, new CardDef[0]);
            ToP1Turn(ctx3, 3);
            ctx3.Players[0].Energy = 20;
            var fTroop = Place(ctx3, 0, 2, infCard);
            var eTroop = Place(ctx3, 1, 2, infCard);
            int hEc = HandIdx(ctx3, 0, ec46.Name);
            CheckTrue(hEc >= 0, "前提 D0c：EC46 在手里（" + hEc + "）" + LogTail(ctx3));
            CheckTrue(fTroop != null && eTroop != null, "前提 D0d：一友一敌两个部队在场");

            var poolEc = RuleCore.PlayTargetPool(ctx3, 0, ec46);
            CheckTrue(poolEc.Count > 0, "前提 D0e：`PlayTargetPool` **非空**（空的话测的是「池空」那一条支，"
                                      + "不是 criteria 闸）—— 实得 " + poolEc.Count);
            CheckTrue(poolEc.Contains(fTroop), "★ D2：**EC46 仍能打出** —— 友军部队在点选池里"
                                             + "｜🧨 第一验收条；把卡级 criteria 换成 logic 级（或把闸写成「全剔」）⇒ 本行红");
            CheckTrue(!poolEc.Contains(eTroop), "★ D3：敌军部队**不在**池里（`enemyUnits = 0`）");
            CheckCode(RuleCore.CanPlayTactic(ctx3, 0, hEc, -1, fTroop), RuleCodes.OK,
                      "★ D4：`CanPlayTactic` 指友军 = `OK`（**同一个点、同一条链**，不只是高亮池）" + LogTail(ctx3));
        }
    }

    // ==========================================================================================
    //  夹具：37 格填满「全已知」，只改需要的那几格
    //  ⚠️ 形状与生成器 `card_target_criteria_set_of` 一致；`playerTurn` 恒 `Unknown(-1)`
    //     —— 原版 `TargetCriteria` 结构上**没有**那一格（`PlayerTurn` 属于 `BoardCriteria`）。
    // ==========================================================================================
    static CardDef.CountSetSpec B114ASpec(int ta, int friendly, int enemy, int hero, int minions,
                                          int remnants, int keyword = 0)
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
            IncludeSelf = 0,
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

    /// <summary>造一张**带卡级 criteria** 的战术夹具卡（`targeted = 1` + 真卡原文 ⇒ 正文一定能解析）。</summary>
    static CardDef B114AProbe(string name, CardDef.CountSetSpec spec)
    {
        var c = new CardDef(name, name, "tactic", "Give Shuriken 2 to a friendly unit",
                            "common", "Test", 0, 0, 0, 0, null, targeted: 1);
        c.AttachCardTargetCriteria(spec, true);
        return c;
    }
}
