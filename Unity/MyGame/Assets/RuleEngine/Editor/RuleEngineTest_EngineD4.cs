// RuleEngineTest_EngineD4.cs — 执行代理 `E9`（2026-10-24 · 第七十三会话）的断言分册
//
// 本件钉 `cards_engine.json` **v25 两列** —— logic / op 级 `targetCriteria` 的**其余 36 格**：
//   ① `activeAbilityTargets`  = 原版 `activeAbility` / `alternativeAbility` 的
//      `activeAbilityLogic[j].targetCriteria`；形状 `[{"ab":-1|-2,"lg":j,"criteria":{37 键}}]`；
//   ② `abilityTargetCriteria` = 原版 `cardAbilities[i].abilityLogic[j].targetCriteria`；
//      形状 `[{"ab":i,"trigger":…,"lg":j,"criteria":{37 键}}]`。
//
// 判据（按权威顺序）：
//   ① **原版全量反编译** `d:/2/tools/decomp_full/`：
//      · `AbilityLogic__PlayAbility.c:262` —— **每一行**拿自己的 `targetCriteria` 去 `GetTargets`；
//      · `AbilityLogic__GetTargets.c` —— `targetsAffected`(+0x10) 先选候选池；
//      · `FilterMethods__CheckIfMeetsCriteria.c` —— 其余 35 格是过滤（逐字段）。
//   ② 字段表 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{AbilityLogic,TargetCriteria}.cs`
//      （`AbilityLogic.cs:16-17` 的 `targetCriteria`；`TargetCriteria` 38 个字段，
//        其中 `conditionsFilter` / `excludeCondition` 已由 `unitConditions` 落成 `cond`/`ex`）。
//   ③ 判据总表 → `资料/普查产出_1024c/A2_引擎战斗判据总表.md` 的 **Q7**；
//      本批的报告 → `资料/普查产出_1024c/E9_卡表v25_logic级判据列.md`。
//
// 🔴 **期望值一律写死字面量**（1127 / 193 / 1616 / 132 / 1008 / 13 / 390 …），
//   全部来自**生成器现读的产物**（`gen_cards_engine.py` 的 v25 摘要）与原版真值快照，
//   ⛔ 不从被测实现里反算（那就是自证）。每条都带 `🧨`（什么改动能让它红）。
//
// ⚠️ **本文件还没接线**：`RuleEngineTest.cs` 的 `Run()` 里要由主对话加一行
//   `Step(TestEngineD4);`（那一份不在本代理白名单）；`RuleEngineTest_Diag.cs` 同理（工具重生成）。
//   补之前覆盖闸会红并点名 `TestEngineD4` —— 那是**设计如此**（宁红，也不要「文件在、从没跑过」）。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批**唯一入口**（`Run()` 里一行 `Step(TestEngineD4);`）。
    /// 子用例是 `D4…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestEngineD4()
    {
        Section("🆕 执行代理 E9：卡表 v25 —— logic/op 级 `targetCriteria` 两列（结构 / 条数 / 对齐 / 守卫）");
        var dto = D4LoadRaw();
        if (!CheckTrueD4(dto != null && dto.cards != null, "前提：`cards_engine.json` 能读成 `CardDatabase.FileDto`")) return;
        D4Shape(dto);
        D4Counts(dto);
        D4Alignment(dto);
        D4ConcreteCards(dto);
        D4VersionGuard();
    }

    // ==============================================================================================
    //  §0 读法：**直接读原始 JSON**（不经 `CardDef`）
    // ==============================================================================================
    /// <summary>读 `Resources/cards_engine.json` → `FileDto`。
    /// 🔴 为什么不用 `CardDatabase.Load()`：这两列**本批没有 `CardDef` 消费者**（`Core/` 另批接）⇒
    /// `CardDef` 上看不到它们。读 DTO 是**唯一**能钉「列落上了、`JsonUtility` 绑上了」的路。</summary>
    static CardDatabase.FileDto D4LoadRaw()
    {
        var asset = Resources.Load<TextAsset>(CardDatabase.ResourcePath);
        return asset == null ? null : JsonUtility.FromJson<CardDatabase.FileDto>(asset.text);
    }

    /// <summary>本工程 `CheckTrue` 的薄包装（只为让 `if (!… ) return;` 读起来短，⛔ 不改语义）。</summary>
    static bool CheckTrueD4(bool cond, string msg) { CheckTrue(cond, msg); return cond; }

    static CardDatabase.CardDto D4Card(CardDatabase.FileDto dto, string id)
    {
        foreach (var c in dto.cards) if (c != null && c.id == id) return c;
        return null;
    }

    // ==============================================================================================
    //  §A 结构：两列都在、每一行都成形
    // ==============================================================================================
    static void D4Shape(CardDatabase.FileDto dto)
    {
        // ---- A1 · 表版本 / 张数（加列必须 bump —— `CardDatabase` 的守卫靠它出声）----
        // 🔴 2026-10-25（执行代理 `F1` · 铁律 5 就地订正）：期望值原来是 **25**。
        //   同会话的 `E2`（`资料/普查产出_1025/E2_ability_criteria管线.md`）加了第三列
        //   **`abilityLevelCriteria`（v26）** ⇒ 表已 bump 到 **26**，本行过期。
        //   ⛔ 判据没有放宽：仍然钉**确切版本号**（不是 `>= 25`）——
        //   「加列必须 bump」这条纪律靠的就是这个等式。
        // 🔴 2026-10-25（执行代理 `E3` · 铁律 5 就地订正）：同会话的 `E3` 又加了第四列
        //   **`abilityActingCriteria`（v27，`CardAbility.actingCardCriteria` `+0x28`）** ⇒ 表已 bump 到
        //   **27**。归因 → `资料/普查产出_1025/E3_actingCardCriteria.md`。
        // 🔴 2026-10-25（执行代理 `E4` · 铁律 5 就地订正）：同会话的 `E4` 又加了第五列
        //   **`activeAbilityPickCriteria`（v28，`ActiveAbility.targetCriteria` `+0x38`）** ⇒ 表已 bump 到
        //   **28**。归因 → `资料/普查产出_1025/E4_trigger目标与activeAbility目标池.md`。
        // 🔴 2026-10-25（执行代理 `X6` · 铁律 5 就地订正）：同会话的 `X6` 又给
        //   **`abilityLevelCriteria` 每行加了 `ca`（能力种类族名）** ⇒ 表已 bump 到 **29**。
        //   归因 → `资料/普查产出_1025/X6_口径乙ca列_执行报告.md`。
        // 🔴 2026-10-25（执行代理 `X20` · 铁律 5 就地订正）：同会话的 `X20` **加了一张卡不加列**
        //   （隐藏附魔模板 `Pray Enchantment`，池 1127→1128）⇒ 表 bump 到 **30**。
        //   归因 → `资料/普查产出_1025/X20_PrayEnchantment入池_执行报告.md`。
        // 🔴 2026-10-26（执行代理 `B8_BM` · 铁律 5 就地订正）：同会话的 `B8_AH` 加了 **`keyword` 一列**
        //   （v31）、`B8_BM` 又加了 **`chosenAbility` 一列**（v32）⇒ 表 bump 到 **32**。
        //   归因 → `资料/完全复刻/报告/B8_BM_强制攻击退还数据化.md`。
        Check(dto.version, 32,
              "★ A1 卡表 `version` = **32**（v32 = 加 `chosenAbility` 一列 —— 「强制攻击退不退还行动额度」"
            + "的判据来源（`ForcedAttackRefund`）；v31 = 加 `keyword` 一列；"
            + "v30 = **加卡不加列**：池内新增隐藏附魔模板 `Pray Enchantment`；"
            + "v29 `abilityLevelCriteria` 的 `ca` 一格 / v28 `activeAbilityPickCriteria` / v27 "
            + "`abilityActingCriteria` / v26 / v25 各列照旧；"
            + "⛔ 不 bump 的话卡池/新列的变更没有版本可认）"
            + "｜🧨 生成器 return 里改回 31（或忘了 bump）⇒ 红");

        // ---- A2 · 两列「每张卡都落」：`null` 与空数组必须分得开 ----
        int aatNull = 0, atcNull = 0;
        foreach (var c in dto.cards)
        {
            if (c == null) continue;
            if (c.activeAbilityTargets == null) aatNull++;
            if (c.abilityTargetCriteria == null) atcNull++;
        }
        Check(aatNull, 0,
              "★ A2a `activeAbilityTargets` **每张卡都有**（没行 = `[]`）—— `null`（缺列）必须是 0 张"
            + "｜🧨 生成器改成「只落有行的卡」⇒ 红（那样「缺列」与「没有行」就分不开了）");
        Check(atcNull, 0,
              "★ A2b `abilityTargetCriteria` **每张卡都有**（没 ability = `[]`）—— `null` 必须是 0 张"
            + "｜🧨 同上 ⇒ 红");

        // ---- A3 · `activeAbilityTargets` 每行：ab ∈ {-1,-2} · lg ≥ 0 · criteria 绑上 · playerTurn = -1 ----
        int aatRows = 0, badAb = 0, badLg = 0, nullCrit = 0, badPt = 0, unbound = 0;
        foreach (var c in dto.cards)
        {
            if (c == null) continue;
            foreach (var r in (c.activeAbilityTargets ?? new CardDatabase.TargetCriteriaRowDto[0]))
            {
                aatRows++;
                if (r == null || (r.ab != -1 && r.ab != -2)) badAb++;
                if (r != null && r.lg < 0) badLg++;
                if (r == null || r.criteria == null) { nullCrit++; continue; }
                if (r.criteria.playerTurn != -1) badPt++;
                // 绑定探针：整数格 / 布尔格 / 引用格**各取一个**（`JsonUtility` 绑不上就是 -99 / null）
                if (r.criteria.targetsAffected == CardDatabase.UnitConditionDto.MissingIndex
                    || r.criteria.includeHero == CardDatabase.UnitConditionDto.MissingIndex
                    || r.criteria.filterSpecificCards == null) unbound++;
            }
        }
        Check(aatRows, 193,
              "★ A3a `activeAbilityTargets` 共 **193 行**（= `activeAbilityLogic` 的 193 行，逐行一份 criteria）"
            + "｜🧨 生成器只落 `targetsAffected != 0` 的行 / 漏掉 alternative 支 ⇒ 红");
        Check(badAb, 0,
              "★ A3b `ab` **只取 -1 / -2**（active / alternative，与 `activeAbilityLogic`/`unitConditions` 同词）"
            + "｜🧨 生成器写成 `ab = 0` 或漏键（-99）⇒ 红（漏键会被读成第 0 条 ability = 错对象）");
        Check(badLg, 0,
              "★ A3c `lg` **全部 ≥ 0**（这一列的行都是 logic 级）"
            + "｜🧨 漏写 `lg`（默认 -1）⇒ 红");
        Check(nullCrit, 0, "★ A3d 每行都有 `criteria` 子对象（⛔ 不是 null）｜🧨 生成器漏写 `criteria` ⇒ 红");
        Check(badPt, 0,
              "★ A3e `criteria.playerTurn` **恒 -1**（`AbilityLogic.targetCriteria` 结构上没有这一格 ——"
            + " 那是 `BoardCriteria` 的）⇒ ⛔ 不许出现 `0`（会被读成 Both）或 -99（缺键）"
            + "｜🧨 生成器改传 `has_player_turn=True` ⇒ 红");
        Check(unbound, 0,
              "★ A3f **绑定探针**：每行的 `targetsAffected` / `includeHero` / `filterSpecificCards` 都不是缺键哨兵"
            + "（-99 / null）—— 证明 `JsonUtility` 把子对象 `criteria` 整块绑上了"
            + "｜🧨 C# DTO 的字段名与 JSON 键名不一致（或把 36 格平铺）⇒ 红");

        // ---- A4 · `abilityTargetCriteria` 每行：ab ≥ 0 · lg ≥ 0 · trigger ≥ 0 · criteria 绑上 ----
        int atcRows = 0, nBadAb = 0, nBadLg = 0, nNullCrit = 0, nBadTg = 0, nBadPt = 0, nUnbound = 0;
        foreach (var c in dto.cards)
        {
            if (c == null) continue;
            foreach (var r in (c.abilityTargetCriteria ?? new CardDatabase.TargetCriteriaRowDto[0]))
            {
                atcRows++;
                if (r == null || r.ab < 0) nBadAb++;
                if (r != null && r.lg < 0) nBadLg++;
                if (r != null && r.trigger < 0) nBadTg++;
                if (r == null || r.criteria == null) { nNullCrit++; continue; }
                if (r.criteria.playerTurn != -1) nBadPt++;
                if (r.criteria.targetsAffected == CardDatabase.UnitConditionDto.MissingIndex
                    || r.criteria.traitsFilter == CardDatabase.UnitConditionDto.MissingIndex
                    || r.criteria.excludeEnchantments == null) nUnbound++;
            }
        }
        // 🔴 2026-10-25（`X20`）：1616 → **1619**（池内新增的附魔卡自己带 2 条 ability / 3 条 logic，
        //   它们的 `targetCriteria` 各落 1 行 ⇒ +3 行；`abilityTargets` 的 logic 数同步 1616→1619）。
        Check(atcRows, 1619,
              "★ A4a `abilityTargetCriteria` 共 **1619 行**（= `abilityTargets` 的 1619 个 logic，逐一对应）"
            + "｜🧨 漏落某条 ability 的 logic / 把 ability 级的 `targetCriteria` 也塞进来 ⇒ 红");
        Check(nBadAb, 0, "★ A4b `ab` 全部 ≥ 0（= `cardAbilities` 下标）｜🧨 写成 -1 ⇒ 红");
        Check(nBadLg, 0, "★ A4c `lg` 全部 ≥ 0｜🧨 漏写 ⇒ 红");
        Check(nBadTg, 0, "★ A4d `trigger` 全部 ≥ 0（只作可读性，但坏值必须能被看见）｜🧨 漏写 ⇒ 红");
        Check(nNullCrit, 0, "★ A4e 每行都有 `criteria`｜🧨 漏写 ⇒ 红");
        Check(nBadPt, 0, "★ A4f `playerTurn` 恒 -1（同上）｜🧨 改传 `has_player_turn=True` ⇒ 红");
        Check(nUnbound, 0, "★ A4g 绑定探针（整数 / 枚举 / 引用三格）｜🧨 DTO 字段名不一致 ⇒ 红");
    }

    // ==============================================================================================
    //  §B 条数：覆盖 + 36 格里**真正有内容**的那几格
    // ==============================================================================================
    static void D4Counts(CardDatabase.FileDto dto)
    {
        int aatCards = 0, atcCards = 0;
        int aatTa0 = 0, atcTa0 = 0;
        int aatRefs = 0, atcRefs = 0;
        // 逐格计数（⛔ 不写 36 格全表：只钉「有内容」的那几格，每一格都是一条独立判据）
        int aatKeyword = 0, aatTraits = 0, aatMaxT = 0, aatFriendly = 0, aatHero = 0, aatMinions = 0, aatRemnants = 0;
        int atcKeyword = 0, atcTraits = 0, atcMaxT = 0, atcFriendly = 0, atcHero = 0, atcMinions = 0, atcExclTrait = 0;
        int atcChosenArmy = 0, atcArmyFilter = 0, atcRemnants = 0, atcChooseRes = 0, atcSpellType = 0, atcMana = 0, atcRarity = 0;
        int aatRarity = 0, aatChooseRes = 0;

        Action<CardDatabase.CountSetDto, bool> one = (cr, isAat) =>
        {
            if (cr == null) return;
            if (cr.targetsAffected == 0) { if (isAat) aatTa0++; else atcTa0++; }
            if ((cr.filterSpecificCards != null && cr.filterSpecificCards.Length > 0)
                || (cr.excludeEnchantments != null && cr.excludeEnchantments.Length > 0))
            { if (isAat) aatRefs++; else atcRefs++; }
            if (cr.keywordFilter != 0 && cr.keywordFilter != -1) { if (isAat) aatKeyword++; else atcKeyword++; }
            if (cr.traitsFilter != 0 && cr.traitsFilter != -1) { if (isAat) aatTraits++; else atcTraits++; }
            if (cr.maxTargets > 0) { if (isAat) aatMaxT++; else atcMaxT++; }
            if (cr.friendlyUnits == 1) { if (isAat) aatFriendly++; else atcFriendly++; }
            if (cr.includeHero == 1) { if (isAat) aatHero++; else atcHero++; }
            if (cr.includeMinions == 1) { if (isAat) aatMinions++; else atcMinions++; }
            if (!isAat)
            {
                if (cr.excludeTrait == 1) atcExclTrait++;
                if (cr.chosenArmy != 0 && cr.chosenArmy != -1) atcChosenArmy++;
                if (cr.armyFilter != 0 && cr.armyFilter != -1) atcArmyFilter++;
                if (cr.includeRemnants == 1) atcRemnants++;
                if (cr.selectFromChooseResolution == 1) atcChooseRes++;
                if (cr.spellType != 0 && cr.spellType != -1) atcSpellType++;
                if (cr.manaCostFilter != 0 && cr.manaCostFilter != -1) atcMana++;
                if (cr.rarityFilter != 0 && cr.rarityFilter != -1) atcRarity++;
            }
            else
            {
                if (cr.includeRemnants == 1) aatRemnants++;
                if (cr.selectFromChooseResolution == 1) aatChooseRes++;
                if (cr.rarityFilter != 0 && cr.rarityFilter != -1) aatRarity++;
            }
        };

        foreach (var c in dto.cards)
        {
            if (c == null) continue;
            var a1 = c.activeAbilityTargets;
            if (a1 != null && a1.Length > 0) aatCards++;
            foreach (var r in (a1 ?? new CardDatabase.TargetCriteriaRowDto[0])) one(r == null ? null : r.criteria, true);
            var a2 = c.abilityTargetCriteria;
            if (a2 != null && a2.Length > 0) atcCards++;
            foreach (var r in (a2 ?? new CardDatabase.TargetCriteriaRowDto[0])) one(r == null ? null : r.criteria, false);
        }

        Check(aatCards, 132,
              "★ B1 `activeAbilityTargets` **有行的 132 张**（= `activeAbilityLogic` 有行的 132 张，逐卡同集）"
            + "｜🧨 生成器把这一列写成「只给 `hasActiveAbility=1` 的 87 张」⇒ 红（死数据也要落，见 `gate:0` 口径）");
        // 🔴 2026-10-25（`X20`）：1008 → **1009**（新增的附魔卡自己就有 `abilityLogic.targetCriteria` 行）。
        Check(atcCards, 1009,
              "★ B2 `abilityTargetCriteria` **有行的 1009 张**（其余 119 张没有 `abilityLogic` ⇒ `[]`）"
            + "｜🧨 只给带 `abilityTargets` 非空的卡落 ⇒ 红");
        Check(aatTa0, 17,
              "★ B3 `activeAbilityTargets` 里 `targetsAffected == 0`（`any`）的 **17 行** ——"
            + " 这些行的其余 35 格在原版里**全部失效**（`GetTargets.c:190-192`），但**仍然照落**"
            + "（消费端要能看见「原版说 any」）｜🧨 生成器按 `criteria_carries_targets` 那套把 0 行丢掉 ⇒ 红");
        Check(atcTa0, 286,
              "★ B4 `abilityTargetCriteria` 里 `targetsAffected == 0` 的 **286 行**（同上，照落）"
            + "｜🧨 丢掉 `any` 行 ⇒ 红");

        // ---- B5 · 36 格里「真正有内容」的格（每一格 = 一条独立判据；数量少 ⇒ 说明整块没落对）----
        Check(aatKeyword, 4, "★ B5a `activeAbilityTargets.keywordFilter` 非 0 的 **4 行**｜🧨 该格没落 / 落成 constant ⇒ 红");
        Check(aatTraits, 4, "★ B5b `activeAbilityTargets.traitsFilter` 非 0 的 **4 行**｜🧨 同上");
        Check(aatMaxT, 21, "★ B5c `activeAbilityTargets.maxTargets` > 0 的 **21 行**｜🧨 同上");
        Check(aatFriendly, 31, "★ B5d `activeAbilityTargets.friendlyUnits == 1` 的 **31 行**｜🧨 同上");
        Check(aatHero, 97, "★ B5e `activeAbilityTargets.includeHero == 1` 的 **97 行**｜🧨 同上");
        Check(aatMinions, 119, "★ B5f `activeAbilityTargets.includeMinions == 1` 的 **119 行**｜🧨 同上");
        Check(aatRemnants, 13, "★ B5g `activeAbilityTargets.includeRemnants == 1` 的 **13 行**｜🧨 同上");
        Check(aatChooseRes, 8, "★ B5h `activeAbilityTargets.selectFromChooseResolution == 1` 的 **8 行**｜🧨 同上");
        Check(aatRarity, 1, "★ B5i `activeAbilityTargets.rarityFilter` 非 0 的 **1 行**｜🧨 同上");
        Check(aatRefs, 0,
              "★ B5j `activeAbilityTargets` 的引用列表**全空**（0 行）—— 主动能力这一批的 criteria 里"
            + "没有 `filterSpecificCards`/`excludeEnchantments`；⛔ 不要为了「有值」去编"
            + "｜🧨 把别的 criteria 的引用串进来 ⇒ 红");

        Check(atcKeyword, 117, "★ B6a `abilityTargetCriteria.keywordFilter` 非 0 的 **117 行**｜🧨 该格没落 ⇒ 红");
        Check(atcTraits, 60, "★ B6b `traitsFilter` 非 0 的 **60 行**｜🧨 同上");
        Check(atcMaxT, 391, "★ B6c `maxTargets` > 0 的 **391 行**｜🧨 同上");
        Check(atcFriendly, 556, "★ B6d `friendlyUnits == 1` 的 **556 行**｜🧨 同上");
        Check(atcHero, 336, "★ B6e `includeHero == 1` 的 **336 行**｜🧨 同上");
        Check(atcMinions, 729, "★ B6f `includeMinions == 1` 的 **729 行**｜🧨 同上");
        Check(atcExclTrait, 28, "★ B6g `excludeTrait == 1` 的 **28 行**（布尔「排除」档）｜🧨 同上");
        Check(atcChosenArmy, 51, "★ B6h `chosenArmy` 非 0 的 **51 行**（`CardArmy`）｜🧨 同上");
        Check(atcArmyFilter, 50, "★ B6i `armyFilter` 非 0 的 **50 行**｜🧨 同上");
        Check(atcRemnants, 163, "★ B6j `includeRemnants == 1` 的 **163 行**｜🧨 同上");
        Check(atcChooseRes, 79, "★ B6k `selectFromChooseResolution == 1` 的 **79 行**｜🧨 同上");
        Check(atcSpellType, 10, "★ B6l `spellType` 非 0 的 **10 行**｜🧨 同上");
        Check(atcMana, 21, "★ B6m `manaCostFilter` 非 0 的 **21 行**｜🧨 同上");
        Check(atcRarity, 2, "★ B6n `rarityFilter` 非 0 的 **2 行**｜🧨 同上");
        Check(atcRefs, 13,
              "★ B6o 引用列表非空的 **13 行**（`filterSpecificCards` 的 PPtr 已解成卡 id）；"
            + "现读全部解得出（生成器 `set_unresolved` = 0）"
            + "｜🧨 生成器跳过引用解析 / 拿 path_id 当 id ⇒ 红");
    }

    // ==============================================================================================
    //  §C 对齐：两列与**已有的** `abilityTargets` / `activeAbilityLogic` 逐格同集
    // ==============================================================================================
    /// <summary>🔴 这一节是**跨列对账**，不依赖 `CardDef`：新列的行位置必须与旧列的 logic 位置**完全一致**。
    /// 为什么：消费端要按 `(ab, lg)` 把 criteria 挂回已经挂好的 op 上 —— 位置差一格就是**静默串行**。</summary>
    static void D4Alignment(CardDatabase.FileDto dto)
    {
        int cards = 0, countMismatch = 0, posMismatch = 0, triggerMismatch = 0, aatMismatch = 0, aatPosMismatch = 0;
        foreach (var c in dto.cards)
        {
            if (c == null) continue;
            cards++;
            // ---- C1 · `abilityTargetCriteria` ↔ `abilityTargets` ----
            int want = 0;
            var oldAt = c.abilityTargets ?? new CardDatabase.AbilityTargetDto[0];
            for (int i = 0; i < oldAt.Length; i++)
                want += (oldAt[i] != null && oldAt[i].ta != null) ? oldAt[i].ta.Length : 0;
            var rows = c.abilityTargetCriteria ?? new CardDatabase.TargetCriteriaRowDto[0];
            if (want != rows.Length) countMismatch++;
            var wantPos = new HashSet<string>();
            for (int i = 0; i < oldAt.Length; i++)
                if (oldAt[i] != null && oldAt[i].ta != null)
                    for (int j = 0; j < oldAt[i].ta.Length; j++) wantPos.Add(i + "/" + j);
            var gotPos = new HashSet<string>();
            foreach (var r in rows)
                if (r != null) gotPos.Add(r.ab + "/" + r.lg);
            if (!wantPos.SetEquals(gotPos)) posMismatch++;
            // ---- C2 · `trigger` 与旧列同位置的值一致（新列不是另抄一份 trigger）----
            foreach (var r in rows)
            {
                if (r == null) continue;
                if (oldAt.Length <= r.ab || oldAt[r.ab] == null) { triggerMismatch++; continue; }
                if (oldAt[r.ab].trigger != r.trigger) triggerMismatch++;
            }
            // ---- C3 · `activeAbilityTargets` ↔ `activeAbilityLogic` ----
            int wantA = 0;
            var oldAa = c.activeAbilityLogic ?? new CardDatabase.ActiveAbilityLogicDto[0];
            for (int i = 0; i < oldAa.Length; i++)
                wantA += (oldAa[i] != null && oldAa[i].ta != null) ? oldAa[i].ta.Length : 0;
            var arows = c.activeAbilityTargets ?? new CardDatabase.TargetCriteriaRowDto[0];
            if (wantA != arows.Length) aatMismatch++;
            var wantAPos = new HashSet<string>();
            for (int i = 0; i < oldAa.Length; i++)
                if (oldAa[i] != null && oldAa[i].ta != null)
                    for (int j = 0; j < oldAa[i].ta.Length; j++) wantAPos.Add(oldAa[i].ab + "/" + j);
            var gotAPos = new HashSet<string>();
            foreach (var r in arows)
                if (r != null) gotAPos.Add(r.ab + "/" + r.lg);
            if (!wantAPos.SetEquals(gotAPos)) aatPosMismatch++;
        }
        // 🔴 2026-10-25（`X20`）就地改期望值：1127 → 1128（池内新增隐藏附魔模板 `Pray Enchantment`；
        //    它没有 `abilityTargets`/`activeAbilityLogic` 行 ⇒ 只把「逐卡对账」的卡数 +1，不增任何行）。
        Check(cards, 1128, "★ C0 对账覆盖 1128 张卡｜🧨 池子换了 ⇒ 提醒改这批字面量（不是缺陷，是过期）");
        Check(countMismatch, 0,
              "★ C1a 每张卡 `abilityTargetCriteria` 的**行数** = 该卡 `abilityTargets[].ta` 的长度和"
            + "（1616 个 logic ⇒ 1616 行）｜🧨 漏一条 logic ⇒ 红");
        Check(posMismatch, 0,
              "★ C1b 每张卡的 `(ab,lg)` **位置集合**与 `abilityTargets` 逐格相同"
            + "｜🧨 `ab` 用错（比如按 trigger 排序）⇒ 红 —— 这一条就是「位置对不对」的判据");
        Check(triggerMismatch, 0,
              "★ C2 新列的 `trigger` 与**旧列同位置**的值逐格一致（新列不是另抄一份）"
            + "｜🧨 生成器写 `trigger = -1` / 复制错行 ⇒ 红");
        Check(aatMismatch, 0,
              "★ C3a 每张卡 `activeAbilityTargets` 的**行数** = `activeAbilityLogic[].ta` 的长度和（193）"
            + "｜🧨 只落 active 支、漏 alternative ⇒ 红");
        Check(aatPosMismatch, 0,
              "★ C3b 每张卡的 `(ab,lg)` 位置集合与 `activeAbilityLogic` 逐格相同｜🧨 同上 ⇒ 红");
    }

    // ==============================================================================================
    //  §D 具体卡：抽几张和原版真值逐格对（防「整块偏移一行」这类对账看不出的错）
    // ==============================================================================================
    static void D4ConcreteCards(CardDatabase.FileDto dto)
    {
        // ---- D1 · `ASH1 Anvirr Keltoc`：主动能力 1 行，criteria 全默认、只有 targetsAffected = 30 ----
        var ash1 = D4Card(dto, "ASH1");
        if (!CheckTrueD4(ash1 != null && ash1.activeAbilityTargets != null
                         && ash1.activeAbilityTargets.Length == 1
                         && ash1.activeAbilityTargets[0] != null
                         && ash1.activeAbilityTargets[0].criteria != null,
                         "★ D1a 前提：`ASH1 Anvirr Keltoc` 的 `activeAbilityTargets` 恰好 1 行"))
            return;
        var r1 = ash1.activeAbilityTargets[0];
        Check(r1.ab, -1, "★ D1b `ASH1` 的 `ab` = -1（active 支）｜🧨 写成别的 ⇒ 红");
        Check(r1.lg, 0, "★ D1c `ASH1` 的 `lg` = 0｜🧨 红");
        Check(r1.criteria.targetsAffected, 30,
              "★ D1d `ASH1` 的 `targetsAffected` = **30**（原版真值；= `activeAbilityLogic[0].ta[0]`）"
            + "｜🧨 整块偏移一行 ⇒ 红");
        Check(r1.criteria.includeMinions, 0,
              "★ D1e 同一行 `includeMinions` = 0（这一条 criteria 只有 `targetsAffected`）"
            + "｜🧨 把 `ability` 级那份 criteria 混进来 ⇒ 红");

        // ---- D2 · `ASH31 Winged Autarch`：trait / 布尔三格同时非默认 ----
        var ash31 = D4Card(dto, "ASH31");
        if (CheckTrueD4(ash31 != null && ash31.abilityTargetCriteria != null
                        && ash31.abilityTargetCriteria.Length == 1
                        && ash31.abilityTargetCriteria[0] != null
                        && ash31.abilityTargetCriteria[0].criteria != null,
                        "★ D2a 前提：`ASH31 Winged Autarch` 的 `abilityTargetCriteria` 恰好 1 行"))
        {
            var r2 = ash31.abilityTargetCriteria[0];
            Check(r2.ab, 0, "★ D2b `ASH31` 的 `ab` = 0｜🧨 红");
            Check(r2.trigger, 240, "★ D2c `ASH31` 的 `trigger` = **240**（`UnitDealsDamage` 族的真值）｜🧨 红");
            Check(r2.criteria.targetsAffected, 10, "★ D2d `ASH31.targetsAffected` = 10（`allFiltered`）｜🧨 红");
            Check(r2.criteria.traitsFilter, 390,
                  "★ D2e `ASH31.traitsFilter` = **390**（原版 `DefinedTrait` 真值 —— 这一格是「36 格真的落进来了」的硬证据）"
                + "｜🧨 该格没落（缺键 -99 / 写 0）⇒ 红");
            Check(r2.criteria.friendlyUnits, 1, "★ D2f `friendlyUnits` = 1｜🧨 同上");
            Check(r2.criteria.includeHero, 1, "★ D2g `includeHero` = 1｜🧨 同上");
            Check(r2.criteria.includeMinions, 1, "★ D2h `includeMinions` = 1｜🧨 同上");
        }

        // ---- D3 · `EC33 Alluress`：`filterSpecificCards` 的 PPtr 解引用（`EC32`）----
        var ec33 = D4Card(dto, "EC33");
        if (CheckTrueD4(ec33 != null && ec33.abilityTargetCriteria != null,
                        "★ D3a 前提：`EC33 Alluress` 有 `abilityTargetCriteria`"))
        {
            CardDatabase.TargetCriteriaRowDto hit = null;
            foreach (var r in ec33.abilityTargetCriteria)
                if (r != null && r.ab == 1 && r.lg == 0) hit = r;
            if (CheckTrueD4(hit != null && hit.criteria != null && hit.criteria.filterSpecificCards != null,
                            "★ D3b 前提：`EC33` 的 (ab=1, lg=0) 行有 `filterSpecificCards`"))
            {
                Check(hit.criteria.filterSpecificCards.Length, 1,
                      "★ D3c `EC33` 的引用列表长度 = 1｜🧨 引用解析整段没跑 ⇒ 红");
                Check(hit.criteria.filterSpecificCards[0], "EC32",
                      "★ D3d 引用解出来是 **`EC32`**（原版 `RawCardScript` PPtr → `uniqueId`）"
                    + "｜🧨 拿 `m_PathID` 当 id / 换错解引用表 ⇒ 红");
                Check(hit.criteria.targetsAffected, 10, "★ D3e 同一行 `targetsAffected` = 10｜🧨 整块错行 ⇒ 红");
            }
        }

        // ---- D4 · `ASH46 Aerial Dominion`：同一 ability 的两条 logic，criteria **不同** ----
        var ash46 = D4Card(dto, "ASH46");
        if (CheckTrueD4(ash46 != null && ash46.abilityTargetCriteria != null
                        && ash46.abilityTargetCriteria.Length == 2,
                        "★ D4a 前提：`ASH46` 的 `abilityTargetCriteria` 恰好 2 行（ab=0 的 lg 0/1）"))
        {
            CardDatabase.TargetCriteriaRowDto l0 = null, l1 = null;
            foreach (var r in ash46.abilityTargetCriteria)
            {
                if (r == null) continue;
                if (r.ab == 0 && r.lg == 0) l0 = r;
                if (r.ab == 0 && r.lg == 1) l1 = r;
            }
            if (CheckTrueD4(l0 != null && l1 != null && l0.criteria != null && l1.criteria != null,
                            "★ D4b 前提：两行都读到了"))
            {
                Check(l0.criteria.keywordFilter, 0,
                      "★ D4c `ASH46` 的 lg=0 行 `keywordFilter` = 0（`enemyUnits` 族）｜🧨 两行 criteria 串了 ⇒ 红");
                Check(l0.criteria.enemyUnits, 1, "★ D4d lg=0 行 `enemyUnits` = 1｜🧨 红");
                Check(l1.criteria.keywordFilter, 10,
                      "★ D4e `ASH46` 的 lg=1 行 `keywordFilter` = **10**（两条 logic 的 criteria 不同 ——"
                    + " 这一条钉的是「**逐行**一份 criteria」，⛔ 不是一卡一份）｜🧨 按 ability 只落第一行 ⇒ 红");
                Check(l1.criteria.maxTargets, 0, "★ D4f lg=1 行 `maxTargets` = 0｜🧨 红");
            }
        }

        // ---- D5 · 🔴 **灭自证 / `ab` 必要性**：`(trigger, lg)` 不唯一 ----
        // `Empyric Ambush` 的两条 ability **共用 trigger 0**，criteria 却不同（30 vs 10 / keyword 0 vs 5）。
        var emp = D4Card(dto, "dc7e575c0a328f446be1521c23ec850b");
        if (CheckTrueD4(emp != null && emp.abilityTargetCriteria != null
                        && emp.abilityTargetCriteria.Length >= 2,
                        "★ D5a 前提：`Empyric Ambush`（`dc7e575c…`）有 ≥2 行 criteria"))
        {
            CardDatabase.TargetCriteriaRowDto a0 = null, a1 = null;
            foreach (var r in emp.abilityTargetCriteria)
            {
                if (r == null) continue;
                if (r.ab == 0 && r.lg == 0) a0 = r;
                if (r.ab == 1 && r.lg == 0) a1 = r;
            }
            if (CheckTrueD4(a0 != null && a1 != null && a0.criteria != null && a1.criteria != null,
                            "★ D5b 前提：(ab=0,lg=0) 与 (ab=1,lg=0) 两行都在"))
            {
                Check(a0.trigger, a1.trigger,
                      "★ D5c 两行的 `trigger` **相同**（= 0）—— 这就是 `(trigger, lg)` 不能当主键的证据"
                    + "｜🧨 若某天这张卡的 trigger 变了 ⇒ 说明数据源换了（不是缺陷，是过期）");
                CheckTrueD4(a0.criteria.targetsAffected != a1.criteria.targetsAffected,
                            "★ D5d 🔴 **两行 criteria 的 `targetsAffected` 不同**（30 vs 10）——"
                          + " 只按 `(trigger, lg)` 对齐会把它们并成一条/串行；必须靠 `ab`"
                          + "｜🧨 生成器删掉行里的 `ab`（或消费端改用 `(trigger,lg)` 当键）⇒ 这一对就分不开了");
                Check(a1.criteria.keywordFilter, 5,
                      "★ D5e ab=1 那行 `keywordFilter` = **5**｜🧨 错行 ⇒ 红");
            }
        }
    }

    // ==============================================================================================
    //  §E 版本守卫：v24 表必须**出声**，v25 表**一条都不该有**
    // ==============================================================================================
    static void D4VersionGuard()
    {
        // 最小可用卡（与 G12 那份同形：`CardDef` 构造要的那几格必须在）
        const string oldTable =
            "{\"version\":24,\"count\":1,\"cards\":[{\"id\":\"E9W1\",\"name\":\"E9 版本守卫探针\","
          + "\"type\":\"unit\",\"desc\":\"\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
          + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\"}]}";
        const string curTable =
            "{\"version\":25,\"count\":1,\"cards\":[{\"id\":\"E9W2\",\"name\":\"E9 当前表守卫探针\","
          + "\"type\":\"unit\",\"desc\":\"\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
          + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
          + "\"activeAbilityTargets\":[],\"abilityTargetCriteria\":[]}]}";

        int oldWarns = 0, curWarns = 0;
        string firstOld = null;
        bool countingOld = false, countingCur = false;
        Application.LogCallback cb = (text, stack, type) =>
        {
            if (type != LogType.Warning || text == null) return;
            // 只数**这两列**那条守卫（别把 `< 23`/`< 24` 那些 `activeAbilityLogic` 的算进来）
            if (!text.Contains("activeAbilityTargets")) return;
            if (countingOld) { oldWarns++; if (firstOld == null) firstOld = text; }
            if (countingCur) curWarns++;
        };
        var pool = CardDatabase.Load();          // 先在**真实表**上建一次名索引（下面两份假表会覆盖它）
        Application.logMessageReceived += cb;
        try
        {
            countingOld = true;
            CardDatabase.Parse(oldTable);         // v24：两列整列没有 ⇒ 必须出声一条
            countingOld = false;
            countingCur = true;
            CardDatabase.Parse(curTable);         // v25：当前版本 ⇒ 一条都不该有
            countingCur = false;
        }
        finally
        {
            countingOld = countingCur = false;
            Application.logMessageReceived -= cb;
            CreatePool.BuildNameIndex(pool);      // 还原名索引（同 G12 收尾）
        }

        Check(oldWarns, 1,
              "★ E1 **v24 旧表出声一条**（`< 25` 守卫点名 `activeAbilityTargets` / `abilityTargetCriteria`）——"
            + " ⛔ 缺列不许静默读成「原版说这些 criteria 全是默认值」"
            + "｜🧨 删掉 `CardDatabase.Parse` 里的 `< 25` 守卫 ⇒ 掉成 0");
        CheckTrue(firstOld != null && firstOld.Contains("null") && firstOld.Contains("36"),
                  "★ E2 那条警告把**后果**与**格数**写清了（`null = 未知` / 36 格），⛔ 不是一句「版本低」"
                + "｜🧨 改成没有后果说明的空话 ⇒ 红");
        Check(curWarns, 0,
              "★ E3 **v25 当前表一条都不该有** —— 否则每跑一次 `Load()` 就刷一条假警告（实测耗时里最贵的是噪声）"
            + "｜🧨 把守卫写成 `<= 25` ⇒ 红");
    }
}
