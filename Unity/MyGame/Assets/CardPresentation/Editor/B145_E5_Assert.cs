// B145_E5_Assert.cs — 执行写手 `B145-E5`（第一百三十会话 · 2026-11-05）
//   对象 = 施工单 `判据/B145_施工单汇总_离线.md` §二 的 **A-20 / A-21**（A-19 另见报告 §三；A-18 只查判据）。
//
// ============================ 它盯的是什么 ============================
//  · **A-20**：原版 `BattleManager.GetChoiceFullPool` 在交给 `GetChoiceOptions` **之前**先做
//    `Where(非空且 uniqueId 非空).OrderBy(uniqueId)`（判据 = `out_il2csharp/Assembly-CSharp/BattleManager.cs:9142-9159`
//    ＋ `dump.cs:18804` 基类 `PlayerItem.uniqueId // 0x38`）；`GetChoiceOptions.c:46-70` 随后按**下标**
//    从这份池里抽 ⇒ **池的顺序决定「同一个随机数抽到哪一张」**。我们改前全链没有 `OrderBy`。
//    → 落点 = `RuleCore.SortedChoicePool`（`EffectResolver.cs` 类尾）+ `BuildChoiceOptions` 的 `left`/`backup`。
//  · **A-21**：`FilterMethods.CheckIfRawCardMeetsCriteria` 的血组 altValue 读的是
//    `GetDefaultTraitValue(card, 0x352)` = `DefinedTrait.bastion`(850)，**不是** `armour`(1020)
//    （判据 = `decomp_full/FilterMethods__CheckIfRawCardMeetsCriteria.c:49`；
//      棋盘侧同格 = `FilterMethods__CheckIfMeetsCriteria.c:349-350` 的 `GetCurrentTraitValueWithModifiers(…,0x352)`）。
//    我们改前两处都拿 `Armour` 顶。→ 落点 = `TargetCriteriaEval.MatchesRawCard` / `StatGroupsPass`。
//
// ============================ 分组 ============================
//  A · A-20 排序本身：顺序 / 稳定性 / 剔除 / 灭自证 / 结构（`BuildChoiceOptions` 真的走它）
//  B · A-21 取数：真实卡（`BL27 Venomcrawler`，`Armour 1` 且 `bastion=0`）在 option 6 上的判定
//      + 棋盘侧 `Matches` 同一格 + 源码结构（`KeywordTable.Bastion` 在、`KeywordTable.Armour` 不在）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · A-20 把 `SortedChoicePool` 退回 `new List<CardDef>(pool)`（不排序）⇒ **A1/A4 红**。
//  · A-20 改成**不稳定**排序（`List.Sort`）⇒ 同 Id 两条的相对次序会翻 ⇒ **A2 红**（用两条同 Id、
//    不同 `Name` 的合成卡，排序后比 `Name` 次序）。
//  · A-20 再把 `Where` 删掉（保留 null / 空 Id）⇒ **A3 红**。
//  · A-20 只改 `left` 不改 `backup`（或反过来）⇒ **A5 红**（两条调用点逐条查）。
//  · A-21 把 `bastion` 换回 `armour` ⇒ **B2/B5 红**（这两条的判别式就是「+1 之后超界」）。
//  · A-21 顺手把「血组不筛」也跳过（`CurrentHealthFilterOption` 读到 0 就 return）⇒ **B3/B5-控制条 红**
//    （它们要求 `filter = health-1` 时必须 **false**）。
//  · A-21 退回 `candidate.Armor` ⇒ **B4/B6 红**（源码结构两条）。
//
// ============================ 怎么跑 / 怎么接（⚠️ 本代理不动宿主）============================
//   单独跑：
//     unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//       -projectPath "D:\4\Unity\MyGame" -executeMethod B145_E5_Assert.Run -logFile -
//   接宿主（**零前提**：只读两个源文件 + 读卡表 + 建 3 个合成 `CardDef` / 1 个 `UnitState`，不建场景、不碰存档）：
//     在 `CardPresentation/Editor/BattleScene.cs` 的方法尾**聚合行**（合计那句 `Debug.Log` 之前）
//     同一物理行末尾追加（行数零变化）：
//       B145_E5_Assert.Run(); pass += B145_E5_Assert.Pass; fail += B145_E5_Assert.Fail;
//     ⚠️ 追加前先看那一行已有的 `//` 在哪（本仓踩过 6 次「代码落进行末注释」）。
//   判绿红看 `B145E5 === 合计` 那一行，⛔ 不看退出码。

using System;
using System.Collections.Generic;
using System.IO;
using RuleEngine;
using UnityEngine;

/// <summary>写手 `B145-E5`：A-20（选牌池先按 `uniqueId` 稳定排序）＋ A-21（血组 altValue = `bastion`）。</summary>
public static class B145_E5_Assert
{
    const string Tag = "B145E5";
    static int _pass, _fail;

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Eq<T>(T got, T want, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(got, want);
        Check(ok, ok ? msg : msg + $" —— 期望 [{want}]，实得 [{got}]");
    }

    static void Section(string t) { Debug.Log(Tag + " --- " + t + " ---"); }

    // ============================================================ 源文件
    static string SrcPath(string rel) { return Path.Combine(Application.dataPath, rel); }

    static string ReadSrc(string rel)
    {
        try
        {
            string p = SrcPath(rel);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        catch (Exception e) { return null; }
    }

    /// <summary>从 <paramref name="from"/>（含）到 <paramref name="to"/>（不含）之间那一段；找不到 ⇒ `null`。</summary>
    static string Slice(string text, string from, string to)
    {
        if (text == null) return null;
        int a = text.IndexOf(from, StringComparison.Ordinal);
        if (a < 0) return null;
        int b = text.IndexOf(to, a + from.Length, StringComparison.Ordinal);
        return b < 0 ? null : text.Substring(a, b - a);
    }

    /// <summary>合成卡（只有 Id/Name 有意义 —— 排序断言用）。</summary>
    static CardDef Mk(string id, string name)
    {
        return new CardDef(id, name, "unit", "", "common", "Test", 1, 1, 1, 0);
    }

    // ============================================================ 入口
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("B145-E5 · A-20（选牌池先按 `uniqueId` 排）+ A-21（血组 altValue = `bastion`）");

        GroupA20();
        GroupA21();

        Debug.Log(Tag + $" === 合计：{_pass} 通过 / {_fail} 失败 ===");
    }

    // ------------------------------------------------------------ A · A-20
    static void GroupA20()
    {
        Section("A · A-20 `SortedChoicePool`（判据 = out_il2csharp/BattleManager.cs:9142-9159 · dump.cs:18804 `uniqueId // 0x38`）");

        // A1 · 顺序：C/A/B ⇒ A/B/C
        var pool = new List<CardDef>();
        pool.Add(Mk("C", "c")); pool.Add(Mk("A", "a")); pool.Add(Mk("B", "b"));
        var sorted = RuleCore.SortedChoicePool(pool);
        Check(sorted.Count == 3, "A1a 前提：3 条输入全部保留");
        Eq(sorted[0].Id, "A", "A1b 第 1 位 = `A`（按 `uniqueId` 升序；改前无排序 ⇒ 这里会是 `C`）");
        Eq(sorted[1].Id, "B", "A1c 第 2 位 = `B`");
        Eq(sorted[2].Id, "C", "A1d 第 3 位 = `C`");

        // A2 · 稳定性：两条同 Id 的卡保持原相对次序（`OrderBy` 是稳定排序）
        var dup = new List<CardDef>();
        dup.Add(Mk("X", "first")); dup.Add(Mk("A", "a")); dup.Add(Mk("X", "second"));
        var dupSorted = RuleCore.SortedChoicePool(dup);
        Check(dupSorted.Count == 3, "A2a 前提：同 Id 两条不合并（原版 `Where` 不去重）");
        Eq(dupSorted[0].Id, "A", "A2b 同 Id 组落在 `A` 之后");
        Eq(dupSorted[1].Name, "first", "A2c 稳定排序：同 key 保持原相对序（第 1 条仍是 `first`）");
        Eq(dupSorted[2].Name, "second", "A2d 稳定排序：同 key 保持原相对序（第 2 条仍是 `second`）");

        // A3 · `null` / `Id == null` 照原版 `Where` 剔掉
        var dirty = new List<CardDef>();
        dirty.Add(null); dirty.Add(Mk("B", "b")); dirty.Add(Mk(null, "noid")); dirty.Add(Mk("A", "a"));
        var cleaned = RuleCore.SortedChoicePool(dirty);
        Eq(cleaned.Count, 2, "A3a `null` 与 `Id == null` 的元素被剔掉（原版 `Where(c => c != null && c.uniqueId != null)`）");
        Eq(cleaned[0].Id, "A", "A3b 剩下两条仍按 Id 升序（第 1 位 `A`）");
        Eq(RuleCore.SortedChoicePool(null).Count, 0, "A3c 传 null ⇒ 空表（⛔ 不抛）");

        // A4 · 灭自证：输入本身无序 ⇒ 输出**不等于**输入（否则说明它只是拷了一份）
        bool sameOrder = sorted.Count == 3 && sorted[0].Id == "C" && sorted[1].Id == "A";
        Check(!sameOrder, "A4 灭自证：输出次序**不等于**输入次序（把实现退回 `new List<CardDef>(pool)` ⇒ 本行红）");

        // A5 · 结构：`BuildChoiceOptions` 的 `left` / `backup` 两处都走 `SortedChoicePool`
        string er = ReadSrc("RuleEngine/Core/EffectResolver.cs");
        Check(!string.IsNullOrEmpty(er), "A5a 前提：`RuleEngine/Core/EffectResolver.cs` 读得到");
        if (!string.IsNullOrEmpty(er))
        {
            string body = Slice(er, "public static List<CardDef> BuildChoiceOptions", "/// <summary>\n        /// **账 482 ⓑ");
            Check(body != null && body.Length > 200, "A5b 前提：`BuildChoiceOptions` 的方法体切得出来");
            if (body != null)
            {
                Check(body.Contains("SortedChoicePool(cands)"),
                      "A5c `BuildChoiceOptions` 的候选池走 `SortedChoicePool`（A-20 的唯一落点）");
                Check(!body.Contains("new List<CardDef>(cands)"),
                      "A5d `BuildChoiceOptions` 里**不再**有 `new List<CardDef>(cands)`（`left`/`backup` 两条都要换，漏一条就红）");
                Eq(CountOf(body, "SortedChoicePool(cands)"), 2,
                   "A5e `left` 与 `backup` **两条**都走 `SortedChoicePool`（原版 `lVar5` 与 `lVar6 = fullPool.ToList()` 是同一份排好的池）");
            }
        }
    }

    static int CountOf(string s, string needle)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    // ------------------------------------------------------------ B · A-21
    static void GroupA21()
    {
        Section("B · A-21 血组 altValue = `bastion`(850)（判据 = CheckIfRawCardMeetsCriteria.c:49 · CheckIfMeetsCriteria.c:349-350）");

        // B0/B1 · 真实卡前提：`Armour 1` 是 trait 1020、`bastion` 是 trait 850，两者在本池里不是同一件事
        var db = CardDatabase.Load();
        Check(db != null && db.Count > 100, $"B0 前提：卡表读得到且非空（现读 {db.Count} 张）");
        if (db == null || db.Count == 0) return;
        CardDef card = null;
        for (int i = 0; i < db.Count; i++)
            if (db[i] != null && db[i].Id == "BL27") { card = db[i]; break; }
        Check(card != null, "B1a 前提：池里有 `BL27 Venomcrawler`（带 `Armour 1` 的真实卡）");
        if (card == null) return;
        string why1, why2;
        int armourTrait = card.GetDefaultTraitValue(KeywordTable.Armour, out why1);
        int bastionTrait = card.GetDefaultTraitValue(KeywordTable.Bastion, out why2);
        Check(why1 == null && why2 == null, "B1b 前提：`cardTraits` 已挂（两次 `GetDefaultTraitValue` 都不报「没挂」）");
        Eq(armourTrait, 1, "B1c 前提：这条真实卡的 `armour` trait = 1（= 改前会被当 altValue 的那个数）");
        Eq(bastionTrait, 0, "B1d 前提：同一条卡的 `bastion`(850) = 0（原版读的就是它 ⇒ altValue 恒 0）");
        Check(card.Health > 0, "B1e 前提：`Health` 有值（下面两条的判别式要用它）");
        if (card.Health <= 0) return;

        // B2/B3 · 卡面级：`option 6 (EqualOrLowerIncludingBastion)`，`filter = Health`
        //   · 正确（bastion 0）：Health + 0 <= Health ⇒ **true**
        //   · 改前（armour 1）：Health + 1 <= Health ⇒ **false**  ← 判别式
        var set = RawCardSpec(card.Health);
        string gap;
        Eq(TargetCriteriaEval.MatchesRawCard(card, set, out gap), (bool?)true,
           "B2 `MatchesRawCard`：血组 option 6、filter = Health ⇒ **true**（altValue 必须是 `bastion` = 0；拿 `armour`(1) 顶会变 false）" + (gap != null ? " · gap=" + gap : ""));
        var set2 = RawCardSpec(card.Health - 1);
        Eq(TargetCriteriaEval.MatchesRawCard(card, set2, out gap), (bool?)false,
           "B3 控制条：同一格 `filter = Health - 1` ⇒ **false**（证明这一组真的被求值了，不是被短路放行）" + (gap != null ? " · gap=" + gap : ""));

        // B4/B6 · 源码结构（挡住「把两条落点改回去」这种反向修法）
        string tce = ReadSrc("RuleEngine/Core/TargetCriteriaEval.cs");
        Check(!string.IsNullOrEmpty(tce), "B4a 前提：`RuleEngine/Core/TargetCriteriaEval.cs` 读得到");
        if (!string.IsNullOrEmpty(tce))
        {
            string raw = Slice(tce, "public static bool? MatchesRawCard", "/// <summary>卡面级一组 stat");
            Check(raw != null && raw.Length > 200, "B4b 前提：`MatchesRawCard` 的方法体切得出来");
            if (raw != null)
            {
                Check(raw.Contains("KeywordTable.Bastion"),
                      "B4c `MatchesRawCard` 血组 altValue 读 `KeywordTable.Bastion`");
                Check(!raw.Contains("card.GetDefaultTraitValue(KeywordTable.Armour"),
                      "B4d `MatchesRawCard` **不再**读 `KeywordTable.Armour`（改回去 ⇒ 本行红）");
            }
            string stat = Slice(tce, "static bool? StatGroupsPass", "/// <summary>\n        /// 我们 `CardDef.Rarity`");
            Check(stat != null && stat.Length > 200, "B4e 前提：`StatGroupsPass` 的方法体切得出来");
            if (stat != null)
            {
                Check(stat.Contains("candidate.Bastion"),
                      "B4f 棋盘侧 `StatGroupsPass` 血组 altValue 读 `candidate.Bastion`（= `GetCurrentTraitValueWithModifiers(0x352)` 的等价物）");
                Check(!stat.Contains("candidate.Armor"),
                      "B4g 棋盘侧 `StatGroupsPass` **不再**读 `candidate.Armor`");
            }
        }

        // B5 · 棋盘侧实跑：同一个判别式（`UnitState.Bastion` = 0 / `Armor` = 1）
        var u = new UnitState(card, false);
        var other = new UnitState(Mk("ZZZ_OTHER", "other"), false);
        Eq(u.Bastion, 0, "B5a 前提：`UnitState.Bastion` = 0（`bastion` 关键词的当前值）");
        Eq(u.Armor, 1, "B5b 前提：`UnitState.Armor` = 1（`Armour 1` 关键词 —— 改前被误当 altValue 的那个数）");
        string mgap;
        var boardSet = BoardSpec(card.Health);
        Eq(TargetCriteriaEval.Matches(null, 0, boardSet, u, null, other, null, out mgap, 0), (bool?)true,
           "B5c 棋盘侧 `Matches`：血组 option 6、filter = Health ⇒ **true**（altValue = `candidate.Bastion` = 0）" + (mgap != null ? " · gap=" + mgap : ""));
        var boardSet2 = BoardSpec(card.Health - 1);
        Eq(TargetCriteriaEval.Matches(null, 0, boardSet2, u, null, other, null, out mgap, 0), (bool?)false,
           "B5d 控制条：`filter = Health - 1` ⇒ **false**（这一组确实在筛）" + (mgap != null ? " · gap=" + mgap : ""));
    }

    /// <summary>卡面级 criteria：只留「能判且不筛」的格子 + 血组 `option 6 = filter`。</summary>
    static CardDef.CountSetSpec RawCardSpec(int healthFilter)
    {
        var s = new CardDef.CountSetSpec();
        s.TargetsAffected = 10;              // allFiltered（≠0 ⇒ 不早退；≠235/100 等特例档）
        s.IncludeHero = 0;
        s.IncludeMinions = 1;                // `BL27` 是 unit ⇒ Minion(0)
        s.IncludeSpells = 0;
        s.IncludeWhispers = 0;
        s.IncludeWeapons = 0;
        s.IncludeHeroParts = 0;
        s.SpellType = 0;
        s.TraitsFilter = 0;                  // 不筛 trait
        s.ExcludeTrait = 0;
        s.AttackFilterOption = 0;
        s.ManaCostFilterOption = 0;
        s.RarityFilterOption = 0;
        s.CurrentHealthFilterOption = 6;     // EqualOrLowerIncludingBastion
        s.CurrentHealthFilter = healthFilter;
        s.ArmyFilter = 0;
        s.ChosenArmy = 0;
        s.KeywordFilter = 0;
        s.ExcludeRace = 0;
        s.FilterSpecificCardIds = null;      // 未知 ⇒ 这一格不参与（`MatchesRawCard` 只在非 null 且非空时看它）
        s.ExcludeEnchantmentIds = null;
        return s;
    }

    /// <summary>棋盘级 criteria：在 <see cref="RawCardSpec"/> 之上补齐 `Matches` 会读的那几格。</summary>
    static CardDef.CountSetSpec BoardSpec(int healthFilter)
    {
        var s = RawCardSpec(healthFilter);
        s.FriendlyUnits = 1;                 // 候选（`BL27`）与施放方同侧
        s.EnemyUnits = 0;
        s.IncludeSelf = 0;                   // `thisCard` 是另一只 ⇒ 这两格不影响
        s.ExcludeNotSelf = 0;
        s.ExcludeSimilarCards = 0;
        s.ExcludeFriendlyHero = 0;
        s.ExcludeEnemyHero = 0;
        s.IncludeRemnants = 1;               // 候选不是残骸；这一格不参与（`Matches` ⑤ 只看 0 且候选是残骸）
        return s;
    }
}
