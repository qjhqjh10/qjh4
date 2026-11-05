// B146_E4_Assert.cs — 批次 **B146** · 执行代理 `E4`（`B145-AITARGETS-STAT-ALT`：AI 评分路 stat 四组的 altValue 收口）
//
// ============================ 它盯的是什么 ============================
//   A 组 · 攻组 altValue = **候选 `CurrentRangeAttack`**（不是恒 0）；
//   H 组 · 血组 altValue = **候选 bastion**（`GetCurrentTraitValueWithModifiers(候选, 0x352)`）；
//         ＋ 池内唯一非零档（health option 2 = EqualOrHigher）行为不变；
//   R 组 · 第 4 组（稀有度）现在**真的筛**（原来整组是空操作）；
//   S 组 · **结构**：`AiTargets` 里不再有第二份 `MeetsStatCriteria` 求值器，且四组都转调
//         `TargetCriteriaEval.MeetsStatCriteria`（alt 实参逐组不同 —— 源码层钉住）；
//   D 组 · **为什么今天 0 可观测**：`ai_scoring.json` 里 option 6/9/10/11 **一条都没有**
//         ⇒ altValue 那一格无人读（数据面机检，不靠散文）。
//
// ============================ 原版判据 ============================
//   · `decomp_full/FilterMethods__CheckIfMeetsCriteria.c:321-332`（`:326-328` = 攻组，第 5 实参 =
//     `EntityScript__get_CurrentRangeAttack(候选)`）；
//   · 同文件 `:333-355`（`:349-352` = 血组，第 5 实参 = `EntityScript__GetCurrentTraitValueWithModifiers(候选, 0x352)`，
//     `0x352` = 850 = `DefinedTrait.bastion`；⚠️ **不是** `armour`(1020)）；
//   · 同文件 `:356-377`（费组 / 稀有度组，altValue = 0）；
//   · 求值表本体 = `FilterMethods__MeetsStatCriteria.c`（唯一实现 = `Core/TargetCriteriaEval.MeetsStatCriteria`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1/A2：把攻组 alt 改回 `0` ⇒ option 9 变成 `0 == 3` ⇒ A1 红（A2 的「对照」也就永远为真）。
//   · H2：把血组 alt 改回 `0` ⇒ `5 <= 6` 真 ⇒ H2 红。
//   · H3/H4：同上（改回 0 ⇒ `0 == 2` 恒假 ⇒ H3 红）。
//   · R2：把稀有度组改回「空操作」⇒ 恒 true ⇒ R2 红。
//   · S1：在 `AiTargets` 里重新加一份 `switch` 并出现 `MeetsStat(` 调用 ⇒ 红。
//   · S2：把四组改回调别处（或删掉转调）⇒ 红。
//   · D1：数据里出现 option 6/9/10/11 的任一条 ⇒ 红（= 那时本修复就有可观测面了，要回来重测）。
//
// ============================ 怎么接（宿主，调度台统一做）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行、第一个 `//` 之前**追加：
//     `Guard("B146E4_Assert", B146_E4_Assert.Run); _pass += B146_E4_Assert.Pass; _fail += B146_E4_Assert.Fail;`
//   （本类在**全局命名空间**、类名 == 文件名 ⇒ 也可直接 `-executeMethod B146_E4_Assert.Run`。）
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using RuleEngine;
using UnityEngine;

/// <summary>B146-E4：AI 评分路 stat 四组 altValue 收口（攻 = 远程 / 血 = bastion · 唯一求值器）。</summary>
public static class B146_E4_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int RunCount { get { return _pass + _fail; } }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log("--- 🆕 B146-E4：AI 评分 stat 四组 altValue 收口（攻=远程 / 血=bastion / 稀有度真筛）---");
        GroupS_Structure();
        GroupA_AttackAlt();
        GroupH_HealthBastionAlt();
        GroupR_RarityGroup();
        GroupD_ZeroObservable();
        Debug.Log("--- B146-E4 合计：通过 " + _pass + " / 失败 " + _fail + " ---");
    }

    // ============================================================ 夹具
    static CardDef Hero(string id)
    {
        var c = new CardDef(id, id, "hero", "", "common", "Test", 0, 2, 30, 0, (IEnumerable<string>)null);
        c.AttachCardType(10);      // 原版 `cardType` (Hero)；不喂会走 `AiTargets.CardTypeOf` 的喊话兜底
        return c;
    }

    static CardDef Unit(string id, int cost, int atk, int hp, int ranged = 0, string rarity = "common")
    {
        var c = new CardDef(id, id, "unit", "", rarity, "Test", cost, atk, hp, ranged, (IEnumerable<string>)null);
        c.AttachCardType(0);       // 原版 `cardType` (Minion)
        return c;
    }

    static BattleContext NewCtx()
    {
        // 同 `B145_E4_Assert.NewCtx`：督军 + 垫牌（避免起手抽空牌库的噪声前提）。
        var d0 = new List<CardDef> { Hero("B146E4Warlord0") };
        var d1 = new List<CardDef> { Hero("B146E4Warlord1") };
        for (int i = 0; i < 10; i++)
        {
            d0.Add(Unit("B146E4Filler0_" + i, 1, 0, 1));
            d1.Add(Unit("B146E4Filler1_" + i, 1, 0, 1));
        }
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>敌对候选的公共 criteria（`ta=10` 不是特例档 ⇒ 走全部常规筛；side 0 视角看 P1 的兵 = enemy）。</summary>
    static AiTargetCriteriaDto Criteria()
        => new AiTargetCriteriaDto { targetsAffected = 10, enemyUnits = true, includeMinions = true };

    // ============================================================ S：结构（源码层）
    static void GroupS_Structure()
    {
        Section("S 结构：唯一求值器 = `TargetCriteriaEval.MeetsStatCriteria`");

        string path = Path.Combine(Application.dataPath, "RuleEngine/Data/AiTargets.cs");
        bool ok = File.Exists(path);
        CheckTrue(ok, "S0 前提：`AiTargets.cs` 读得到（" + path + "）");
        if (!ok) return;
        string src = File.ReadAllText(path);

        CheckTrue(!src.Contains("MeetsStat("),
            "S1 ★★ `AiTargets` 里**不再有** `MeetsStat(` 的裸调用（= 第二份 switch 已删；⛔ 别再加回来）");
        CheckTrue(src.Contains("TargetCriteriaEval.MeetsStatCriteria("),
            "S2 ★★ 四组转调的是 `TargetCriteriaEval.MeetsStatCriteria`（唯一求值器）");
        CheckTrue(src.Contains("c.attackFilterOption, t.RangedAttack"),
            "S3 ★★ 攻组 alt 实参 = 候选**远程攻击**（`CheckIfMeetsCriteria.c:326-328`）");
        CheckTrue(src.Contains("c.currentHealthFilterOption, thisBastion"),
            "S4 ★★ 血组 alt 实参 = 候选 **bastion**（`:349-352`）");
        CheckTrue(src.Contains("card.KwValue(KeywordTable.Bastion)"),
            "S5 手牌/牌库候选的 bastion 取卡面值（在场候选走 `UnitState.Bastion`）");
    }

    // ============================================================ A：攻组 altValue = 远程攻击
    static void GroupA_AttackAlt()
    {
        Section("A 攻组 altValue = 候选 `CurrentRangeAttack`（option 9 EqualToAlt）");

        var ctx = NewCtx();
        var foe = Place(ctx, 1, 3, Unit("B146E4_RangedFoe", 1, 1, 5, ranged: 3));
        Check(foe.RangedAttack, 3, "A0 前提：候选远程 = 3（近战 = " + foe.Attack + "，两者**刻意不同**）");

        var self = Hero("B146E4_ThisCard");
        var c = Criteria();
        c.attackFilterOption = 9;          // EqualToAlt ⇒ 只比第 5 实参（原版 = 候选远程）
        c.attackFilter = 3;

        CheckTrue(AiTargets.MatchesTarget(ctx, c, self, null, 0, foe),
            "A1 ★★ option 9 + filter 3 ⇒ 过（alt = 远程 3）｜🧨 把 alt 改回 0 ⇒ `0 == 3` 假 ⇒ 红");

        c.attackFilter = 1;
        CheckTrue(!AiTargets.MatchesTarget(ctx, c, self, null, 0, foe),
            "A2 灭自证对照：filter 1（= 近战值，≠ 远程）⇒ 不过｜🧨 「一律放行」的实现 ⇒ 红");
    }

    // ============================================================ H：血组 altValue = bastion
    static void GroupH_HealthBastionAlt()
    {
        Section("H 血组 altValue = 候选 bastion（0x352）；option 6/9 与池内唯一非零档 option 2");

        var ctx = NewCtx();
        var foe = Place(ctx, 1, 3, Unit("B146E4_BastionFoe", 1, 1, 5));
        foe.AddKeyword(KeywordTable.Bastion, 2);
        Check(foe.Bastion, 2, "H0 前提：候选 bastion = 2（血 = " + foe.Health + "）");

        var self = Hero("B146E4_ThisCard");
        var c = Criteria();
        c.currentHealthFilterOption = 6;   // EqualOrLowerIncludingBastion ⇒ 血 + bastion <= filter
        c.currentHealthFilter = 7;

        CheckTrue(AiTargets.MatchesTarget(ctx, c, self, null, 0, foe),
            "H1 ★ option 6 + filter 7 ⇒ 过（5 + 2 = 7 <= 7）");
        c.currentHealthFilter = 6;
        CheckTrue(!AiTargets.MatchesTarget(ctx, c, self, null, 0, foe),
            "H2 ★★ 灭自证：filter 6 ⇒ 7 <= 6 假｜🧨 把 alt 改回 0 ⇒ `5 <= 6` 真 ⇒ 红");

        c.currentHealthFilterOption = 9;   // EqualToAlt ⇒ alt == filter
        c.currentHealthFilter = 2;
        CheckTrue(AiTargets.MatchesTarget(ctx, c, self, null, 0, foe),
            "H3 ★★ option 9 + filter 2 ⇒ 过（alt == bastion）｜🧨 alt 改回 0 ⇒ `0 == 2` 假 ⇒ 红");
        c.currentHealthFilter = 1;
        CheckTrue(!AiTargets.MatchesTarget(ctx, c, self, null, 0, foe),
            "H4 对照：filter 1 ⇒ 不过（bastion 是 2）");

        // 池内唯一非零档（`ai_scoring.json` 现读 4 条 `currentHealthFilterOption = 2`）—— 不能被这次改动碰坏
        c.currentHealthFilterOption = 2;   // EqualOrHigher ⇒ filter <= 血
        c.currentHealthFilter = 5;
        CheckTrue(AiTargets.MatchesTarget(ctx, c, self, null, 0, foe),
            "H5 ★ 线上档 option 2 + filter 5 ⇒ 过（5 >= 5，等号算过）");
        c.currentHealthFilter = 6;
        CheckTrue(!AiTargets.MatchesTarget(ctx, c, self, null, 0, foe),
            "H6 线上档对照：filter 6 ⇒ 不过（5 >= 6 假）");
    }

    // ============================================================ R：第 4 组（稀有度）现在真的筛
    static void GroupR_RarityGroup()
    {
        Section("R 第 4 组稀有度：转调同一求值器（原来整组是空操作）");

        var ctx = NewCtx();
        var rare = Place(ctx, 1, 4, Unit("B146E4_RareFoe", 1, 1, 5, 0, "rare"));
        var self = Hero("B146E4_ThisCard");
        var c = Criteria();
        c.rarityFilterOption = 1;          // EqualTo
        c.rarityFilter = 2;                // CardRarity: rare = 2

        CheckTrue(AiTargets.MatchesTarget(ctx, c, self, null, 0, rare),
            "R1 ★ option 1 + filter 2 ⇒ 过（候选 rarity = rare = 2）");
        c.rarityFilter = 3;
        CheckTrue(!AiTargets.MatchesTarget(ctx, c, self, null, 0, rare),
            "R2 ★★ 灭自证：filter 3（epic）⇒ 不过｜🧨 稀有度组退回「空操作」⇒ 恒 true ⇒ 红");
    }

    // ============================================================ D：为什么今天 0 可观测（数据面机检）
    static void GroupD_ZeroObservable()
    {
        Section("D 可达性：`ai_scoring.json` 里 altValue 那几档一次都没被用到");

        var ta = Resources.Load<TextAsset>("ai_scoring");
        CheckTrue(ta != null, "D0 前提：`Resources/ai_scoring.json` 可读");
        if (ta == null) return;
        string s = ta.text;

        int altAttack = Regex.Matches(s, "\"attackFilterOption\"\\s*:\\s*(?:9|10|11)(?![0-9])").Count;
        int altHealth = Regex.Matches(s, "\"currentHealthFilterOption\"\\s*:\\s*(?:6|9|10|11)(?![0-9])").Count;
        int liveHealth = Regex.Matches(s, "\"currentHealthFilterOption\"\\s*:\\s*2(?![0-9])").Count;
        int rarityAny = Regex.Matches(s, "\"rarityFilterOption\"\\s*:\\s*[1-9](?![0-9])").Count;

        Check(altAttack, 0, "D1 ★ 攻组 alt 档（option 9/10/11）在数据里 = 0 条 ⇒ 这一修复今天**无可观测面**（判据变了、结论不变）");
        Check(altHealth, 0, "D2 ★ 血组 alt 档（option 6/9/10/11）= 0 条（同上）");
        CheckTrue(liveHealth > 0, "D3 对照：血组线上档 option 2 有 " + liveHealth + " 条 ⇒ H5/H6 动的是**真会被执行的**那一档");
        Check(rarityAny, 0, "D4 ★ 稀有度组非零 option = 0 条 ⇒ R 组同样今天休眠（但代码已按原版筛）");
        Debug.Log("[B146E4] （数据面）alt 档出现 0 条时：本修复**行为零变化**，但把「第二份求值器 + alt 丢失」"
                + "这两个隐患收掉了；将来数据出现 6/9/10/11 时立即按原版生效（D1/D2 会先变红提醒重测）。");
    }

    // ============================================================ 小工具
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("[B146E4] ✓ " + msg); }
        else { _fail++; Debug.LogError("[B146E4] ✗ " + msg); }
    }
}
