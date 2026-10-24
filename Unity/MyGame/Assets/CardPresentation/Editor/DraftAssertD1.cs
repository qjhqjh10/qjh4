// DraftAssertD1.cs — 执行代理 `D1`（2026-10-24）的断言宿主：**轮抽的「数据 + 组卡」这一层**
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景、不进任何 `Editor/*Scene.cs` 宿主）。
// 调用方式（主对话在某个宿主收尾处加两行，想并进总数就带后一行）：
//
//     DraftAssertD1.Run();
//     pass += DraftAssertD1.Pass; fail += DraftAssertD1.Fail;
//
// 或者单独跑：`-executeMethod DraftAssertD1.Run`
//
// ==================================================================
//  六条（= 本批的验收标准）＋ 一条额外交叉校验
// ------------------------------------------------------------------
//  ① 数据形状：包总数 882 · enabled 872 · 每包恒 3 张 · 阵营 13 个
//  ② 卡 id 全命中：904 个唯一 `cardIds` 全在 `cards_engine.json` 里
//  ③ 抽包确定性：同 seed 同轮次 ⇒ 逐位相同；不同 seed ⇒ 有不同
//  ④ 逐轮不重复：同一轮里不会给出同一个 `packId` 两次（`exclude` 也真的在滤）
//  ⑤ 组卡：4 个包 ⇒ 12 张，且 = 4 个包内容的并集（**按选包顺序、包内原序**）
//  ⑥ 阵营过滤：Goff 的包不会出现在 UM 的候选里（13 个阵营逐一验）
//  ⑦（额外）数据里的 `armyName` 列与 `ArmyIcons.Faction` 逐包一致
//     —— 这一列是**给人和工具读的副本**，运行期唯一权威是 `ArmyIcons`；
//        两处任一处改了而另一处没改 ⇒ 本断言红（防「两处写同一条规则」）。
//
// 🔴 **期望值从哪来**（不许拿我们自己的常量证明我们自己的实现）：
//    · 882 / 872 / 3 / 13 / 904 全部是**原版解包数据的实测数**
//      （`bundle_draftpacks_assets_all` 的 882 个 `PrebuiltPack` SO；判据 → `资料/普查产出_1024b/S2_轮抽模式数据.md`）；
//    · 卡 id 命中 = 拿我们的卡池 `cards_engine.json` 去比原版 id（**外部真值**）；
//    · ⚠️ `RoundsPerDraft=4` / `PacksPerRoundOffered=3` **不是原版真值**（服务端下发、本地 0 命中）
//      ⇒ ⑤ 只验「按 N 个包摊平 = 3N 张、顺序对」，**不验 4 这个数**（它按定义是我们挑的）。
// ==================================================================
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class DraftAssertD1
{
    const string P = "[D1] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进宿主的合计数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    /// <summary>原版实测：882 个 `PrebuiltPack` SO。</summary>
    const int RawPackFiles = 882;
    /// <summary>原版实测：`packEnabled == 1` 的有 872 个。</summary>
    const int RawEnabled = 872;
    /// <summary>原版实测：13 个阵营（`CardArmy` 10…130，不含 `Neutral`）。</summary>
    const int RawArmies = 13;
    /// <summary>原版实测：882 个包共 2646 个卡槽 / 904 个唯一卡 id。</summary>
    const int RawUniqueCardIds = 904;

    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            string path = FindDataFile();
            if (!Ok(path != null, "前提：找得到卡包表 `draft_packs.json`"
                                + "（`数据/游戏数据/` 或 `Resources/`）"))
            {
                Debug.Log(P + "找不到数据文件，后面的条目全部没跑 —— "
                           + "先跑 `python 工具/gen_draft_packs.py --write`");
                return;
            }
            Debug.Log(P + "数据文件 = " + path);

            string json;
            try { json = File.ReadAllText(path); }
            catch (Exception e) { Ok(false, "前提：读得了 `" + path + "`：" + e.Message); return; }

            var packs = DraftPacks.Parse(json);
            DraftPacks.SetPacksForTest(packs);
            if (!Ok(packs.Count > 0, "前提：表解析出包（`DraftPacks.Parse` 非空）")) return;

            A1_Shape(packs);
            A2_CardIds(packs);
            A3_RollDeterminism();
            A4_NoRepeatPerRound();
            A5_ClosedDeck();
            A6_ArmyFilter();
            A7_ArmyNameColumn(packs);
        }
        catch (Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            DraftPacks.InvalidateCache();
        }
        Debug.Log(P + $"=== D1 合计：{Pass} 通过 / {Fail} 失败 ===");
    }

    // ==================================================================
    //  断言小工具（与 `E13` / `E7` 同形：本类自己记一份 Pass/Fail）
    // ==================================================================
    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + $"   ✓ {msg}"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}"); return false;
    }

    static bool Eq<T>(T got, T want, string msg)
    {
        return Ok(EqualityComparer<T>.Default.Equals(got, want), msg + $"（实得 {got} / 期望 {want}）");
    }

    /// <summary>数据文件位置：先看 `数据/游戏数据/`（生成器的正规产物），再看 `Resources/`（运行期那份）。
    /// ⚠️ `Application.dataPath` = `<工程>/Assets` ⇒ 上两级才是 `d:/4/Unity`。</summary>
    static string FindDataFile()
    {
        var cands = new[]
        {
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../数据/游戏数据/draft_packs.json")),
            Path.Combine(Application.dataPath, "CardPresentation/Resources/draft_packs.json"),
        };
        for (int i = 0; i < cands.Length; i++)
            if (File.Exists(cands[i])) return cands[i];
        return null;
    }

    static string Sig(List<DraftPacks.PackDto> roll)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < roll.Count; i++) { if (i > 0) sb.Append('|'); sb.Append(roll[i].key); }
        return sb.ToString();
    }

    // ==================================================================
    //  ① 数据形状
    // ==================================================================
    static void A1_Shape(List<DraftPacks.PackDto> packs)
    {
        Debug.Log(P + "--- ① 数据形状（882 / 872 / 每包 3 张 / 13 阵营）---");

        Eq(packs.Count, RawPackFiles,
           "① 包总数 = 882 🧨 怎么改会红：生成器的 `CARDS_PER_PACK`/读源目录漏文件，"
         + "或有人手删 `draft_packs.json` 里几条，或把 882 个 SO 去重成 855 条");

        int en = 0, badLen = 0, badArmy = 0;
        var armies = new HashSet<int>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        int dupKeys = 0;
        for (int i = 0; i < packs.Count; i++)
        {
            var p = packs[i];
            if (p == null) { badLen++; continue; }
            if (p.enabled) en++;
            if (p.cardIds == null || p.cardIds.Length != DraftPacks.CardsPerPack) badLen++;
            if (p.army == 0) badArmy++; else armies.Add(p.army);
            if (!keys.Add(p.key ?? "")) dupKeys++;
        }

        Eq(en, RawEnabled,
           "① enabled = 872 🧨 怎么改会红：把 `packEnabled` 读成永远 true（漏读 0），"
         + "或生成器把 10 个禁用包过滤掉了");

        Eq(packs.Count - en, 10,
           "① disabled = 10（9 个 Emperor's Children + 1 个 Tau）"
         + " 🧨 怎么改会红：同上（多/少认了禁用包）");

        Eq(badLen, 0,
           "① 每个包恰好 3 张卡（882/882）"
         + " 🧨 怎么改会红：生成器对短包**补齐/截断**（本类明令不补）、或原版数据换了而没人复核");

        Eq(badArmy, 0, "① 没有包的 `army` = 0（`CardArmy.Neutral`）"
                     + " 🧨 怎么改会红：`packArmy` 字段读错位 / 映射表把 0 当合法阵营");

        Eq(armies.Count, RawArmies,
           "① 阵营数 = 13 🧨 怎么改会红：阵营 id 映射表少一项，或生成器按 `armyName` 字符串分组把两族并了");

        Eq(dupKeys, 0,
           "① `key` 882 条两两不同（重复 packId 靠 `#2` 后缀区分）"
         + " 🧨 怎么改会红：生成器去掉 `#n` 后缀 ⇒ `Resolve` / `excludePackIds` 会静默认错包");

        // 用 `CardArmy` 真值 10…130（⛔ 不是 1..13 —— 那会让「按序号遍历」看起来也对）
        int[] armyIds = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130 };
        int sumArmy = 0;
        for (int i = 0; i < armyIds.Length; i++) sumArmy += DraftPacks.PacksForArmy(armyIds[i], false).Count;
        Eq(sumArmy, RawPackFiles,
           "① 13 个阵营的包数加起来 = 882（`PacksForArmy` 一个都不漏、也不重复算）"
         + " 🧨 怎么改会红：`PacksForArmy` 的过滤条件写错（多算/漏算某个 army）");
    }

    // ==================================================================
    //  ② 卡 id 全命中卡池
    // ==================================================================
    static void A2_CardIds(List<DraftPacks.PackDto> packs)
    {
        Debug.Log(P + "--- ② 904 个唯一 cardId 全在 cards_engine.json 里 ---");

        var pool = CardDatabase.Load();
        if (!Ok(pool.Count > 0, "② 前提：卡池 `cards_engine.json` 读进来了")) return;

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < pool.Count; i++) if (pool[i] != null && pool[i].Id != null) ids.Add(pool[i].Id);

        var uniq = new HashSet<string>(StringComparer.Ordinal);
        var missing = new List<string>();
        for (int i = 0; i < packs.Count; i++)
        {
            var c = packs[i] != null ? packs[i].cardIds : null;
            if (c == null) continue;
            for (int k = 0; k < c.Length; k++)
            {
                if (string.IsNullOrEmpty(c[k])) continue;
                uniq.Add(c[k]);
                if (!ids.Contains(c[k])) missing.Add(c[k]);
            }
        }

        Eq(uniq.Count, RawUniqueCardIds,
           "② 包里一共 904 个唯一卡 id 🧨 怎么改会红：生成器把 `cardIds` 顺序/条数改了，"
         + "或原版包数据换了批次而没人复核这个数");

        if (missing.Count > 0)
            Debug.LogError(P + "   卡池里查不到的 id（前 20 个）：" + string.Join(", ",
                missing.GetRange(0, Math.Min(20, missing.Count)).ToArray()));
        Eq(missing.Count, 0,
           "② 904 个 id 全命中卡池（0 缺）🧨 怎么改会红：卡池少一张卡、"
         + "或包数据里出现卡池没有的 id（= 轮抽会抽到不存在的卡）");
    }

    // ==================================================================
    //  ③ 抽包确定性
    // ==================================================================
    static void A3_RollDeterminism()
    {
        Debug.Log(P + "--- ③ 抽包确定性（同 seed 同轮次逐位相同；换 seed 要有不同）---");

        const int um = 10;   // Ultramarines

        var a = DraftPacks.RollRound(12345, 0, um, DraftPacks.PacksPerRoundOffered);
        var b = DraftPacks.RollRound(12345, 0, um, DraftPacks.PacksPerRoundOffered);
        Eq(a.Count, DraftPacks.PacksPerRoundOffered,
           "③ 一轮给出 `PacksPerRoundOffered`(3) 个包 🧨 怎么改会红：候选池不够（阵营过滤写错）、"
         + "或 `RollRound` 的循环上界写错");
        Eq(Sig(a), Sig(b),
           "③ 同 seed 同轮次跑两次 ⇒ 逐位相同 🧨 怎么改会红：抽包改用 `UnityEngine.Random`（工程铁律禁止）、"
         + "或 `Random` 在循环外只建一次（那就不再「逐位确定」）");

        // 逐轮逐位：换轮次 ⇒ 结果不同（同 seed）
        var r1 = DraftPacks.RollRound(12345, 1, um, DraftPacks.PacksPerRoundOffered);
        Ok(Sig(r1) != Sig(a),
           "③ 换轮次（同 seed）⇒ 这一轮给的包不同 🧨 怎么改会红：`round` 没进 `Random` 的种子"
         + $"（实得第 0 轮 {Sig(a)} / 第 1 轮 {Sig(r1)}）");

        // 换 seed：扫 0..15，既验「每个 seed 都可复现」又验「不是所有 seed 都一样」
        var sigs = new HashSet<string>(StringComparer.Ordinal);
        bool allRepro = true;
        for (int s = 0; s < 16; s++)
        {
            var x = DraftPacks.RollRound(s, 0, um, DraftPacks.PacksPerRoundOffered);
            var y = DraftPacks.RollRound(s, 0, um, DraftPacks.PacksPerRoundOffered);
            if (Sig(x) != Sig(y)) allRepro = false;
            sigs.Add(Sig(x));
        }
        Ok(allRepro,
           "③ seed 0…15 每个都跑两遍，16 对全部逐位相同"
         + " 🧨 怎么改会红：抽包里有隐藏状态（静态计数器 / 缓存没清）⇒ 第二次调用结果变了");
        Ok(sigs.Count > 1,
           $"③ 16 个不同 seed 至少给出 >1 种结果（实得 {sigs.Count} 种）"
         + " 🧨 怎么改会红：`seed` 被忽略（硬编码 0 / 传成常量）⇒ 16 个 seed 结果一模一样");

        // 同 seed 同轮次、不同阵营 ⇒ 各自的包必须都属于那个阵营（防「阵营没进种子」那种偷懒实现）
        var goff = DraftPacks.RollRound(12345, 0, 20, DraftPacks.PacksPerRoundOffered);
        Ok(Sig(goff) != Sig(a),
           "③ 同 seed 同轮次、不同阵营 ⇒ 结果不同（Goff vs UM）"
         + " 🧨 怎么改会红：`RollRound` 忘了用 `armyId` 过滤候选池");
    }

    // ==================================================================
    //  ④ 逐轮不重复
    // ==================================================================
    static void A4_NoRepeatPerRound()
    {
        Debug.Log(P + "--- ④ 同一轮里不会给出同一个 packId 两次；exclude 真的在滤 ---");

        int[] armyIds = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130 };
        int badRound = 0, badArmy = 0, checkedRounds = 0;
        for (int ai = 0; ai < armyIds.Length; ai++)
        {
            // 每族只取 8 个 seed × 4 轮，够覆盖「同一轮」的边界又不拖慢批处理
            for (int s = 0; s < 8; s++)
            {
                for (int round = 0; round < DraftPacks.RoundsPerDraft; round++)
                {
                    var roll = DraftPacks.RollRound(1000 + s, round, armyIds[ai],
                                                    DraftPacks.PacksPerRoundOffered);
                    checkedRounds++;
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    for (int i = 0; i < roll.Count; i++)
                    {
                        if (!seen.Add(roll[i].packId)) badRound++;
                        if (roll[i].army != armyIds[ai]) badArmy++;
                    }
                }
            }
        }
        Eq(badRound, 0,
           $"④ 扫了 {checkedRounds} 轮，**同轮内 `packId` 重复 0 次**"
         + " 🧨 怎么改会红：去掉 `RollRound` 里那句 `seenPackId.Add`（那 27 组导出重复会让同一个包"
         + "在一轮里出现两次、或某个包权重翻倍）");

        Eq(badArmy, 0,
           $"④ 扫了 {checkedRounds} 轮，**给出的包 100% 属于请求的阵营**"
         + " 🧨 怎么改会红：候选池忘了按 `packArmy` 过滤");

        // exclude：把**已选中的包**排除掉，后面几轮必须一个都不再出现
        // ⚠️ 只排除「选中」的那一个 —— 同一轮**没被选中**的另外两个包在原版里仍可能再出现
        //    （`ReRollPack` 的存在也说明包会重复出现），所以别把整轮都塞进 exclude。
        var first = DraftPacks.RollRound(777, 0, 10, DraftPacks.PacksPerRoundOffered);
        if (!Ok(first.Count > 0, "④ 前提：第 0 轮抽得出包")) return;

        var pickedKeys = new List<string> { first[0].key };
        var exclude = new HashSet<string>(StringComparer.Ordinal)
        {
            first[0].key, first[0].packId,
        };

        int leaked = 0;
        for (int round = 1; round < DraftPacks.RoundsPerDraft; round++)
        {
            var roll = DraftPacks.RollRound(777, round, 10, DraftPacks.PacksPerRoundOffered, exclude);
            for (int i = 0; i < roll.Count; i++)
                if (exclude.Contains(roll[i].key) || exclude.Contains(roll[i].packId)) leaked++;
            if (roll.Count > 0)
            {
                pickedKeys.Add(roll[0].key);
                exclude.Add(roll[0].key); exclude.Add(roll[0].packId);
            }
        }
        Eq(leaked, 0,
           "④ 整局 4 轮（跨轮传 exclude）没有任何**已选中的**包再被给出"
         + " 🧨 怎么改会红：`RollRound` 不认 `excludePackIds`（那 4 轮会给出同一个包）");
        Eq(new HashSet<string>(pickedKeys, StringComparer.Ordinal).Count, DraftPacks.RoundsPerDraft,
           "④ 4 轮共选出 4 个**不同**的包（供 ⑤ 用）"
         + " 🧨 怎么改会红：同上一句 —— exclude 没生效 ⇒ 4 轮可能选到同一个包");
    }

    // ==================================================================
    //  ⑤ 组卡
    // ==================================================================
    static void A5_ClosedDeck()
    {
        Debug.Log(P + "--- ⑤ 4 个包 ⇒ 12 张，且 = 4 个包内容的并集（按选包顺序）---");

        // 复算一遍 4 轮（与 ④ 同口径，但这里要拿到 4 个 PackDto 本体）
        var pickedKeys = new List<string>();
        var exclude = new HashSet<string>(StringComparer.Ordinal);
        var pickedPacks = new List<DraftPacks.PackDto>();
        for (int round = 0; round < DraftPacks.RoundsPerDraft; round++)
        {
            var roll = DraftPacks.RollRound(4242, round, 20 /* Goff */, DraftPacks.PacksPerRoundOffered, exclude);
            if (roll.Count == 0) break;
            pickedPacks.Add(roll[0]);                 // 每轮选 1 包（原版 `PickPack`）
            pickedKeys.Add(roll[0].key);
            exclude.Add(roll[0].key); exclude.Add(roll[0].packId);
        }
        if (!Ok(pickedPacks.Count == DraftPacks.RoundsPerDraft,
                $"⑤ 前提：4 轮各选 1 包（实得 {pickedPacks.Count} 包）"
              + " 🧨 怎么改会红：Goff 候选池不足 4 个 / `RollRound` 返回空"))
            return;

        int warlordId = 21;    // 随便一个数：`warlordId` **只做标记**，不参与选卡（原版也不参与）
        DraftPacks.ClosedDeck deck;
        string err;
        if (!Ok(DraftPacks.TryBuildClosedDeck(warlordId, pickedKeys, out deck, out err),
                "⑤ `TryBuildClosedDeck` 成功（错误：" + (err ?? "无") + "）"
              + " 🧨 怎么改会红：`Resolve` 不认 `key`（那 4 个包全都认不出）"))
            return;

        Eq(deck.warlordId, warlordId,
           "⑤ `ClosedDeck.warlordId` 原样带过去"
         + " 🧨 怎么改会红：组卡时把 warlordId 丢了（原版一手牌挂哪位战将是存档字段）");

        Eq(deck.cardIds.Length, DraftPacks.RoundsPerDraft * DraftPacks.CardsPerPack,
           "⑤ 12 张（4 包 × 3 张）"
         + " 🧨 怎么改会红：`BuildClosedDeck` 把包内卡去重了（原版允许包内自重）、"
         + "或漏摊平了某一包");

        // 逐位比：必须是「按选包顺序、包内原序」的拼接
        var want = new List<string>();
        for (int i = 0; i < pickedPacks.Count; i++)
            for (int k = 0; k < pickedPacks[i].cardIds.Length; k++) want.Add(pickedPacks[i].cardIds[k]);

        bool same = want.Count == deck.cardIds.Length;
        if (same)
            for (int i = 0; i < want.Count; i++)
                if (want[i] != deck.cardIds[i]) { same = false; break; }
        Ok(same,
           "⑤ 12 张 = 4 个包的 `cardIds` **按选包顺序逐位拼接**"
         + " 🧨 怎么改会红：组卡时对卡排序 / 去重 / 换了包的顺序"
         + $"（实得 {string.Join(",", deck.cardIds)}）");

        Eq(deck.packIds.Length, DraftPacks.RoundsPerDraft,
           "⑤ `ClosedDeck.packIds` 4 条（原版 `GetClosedDeckByPackIds` 的输入）"
         + " 🧨 怎么改会红：组卡时只留下卡、丢了包 id（存档回放要对账原版）");
    }

    // ==================================================================
    //  ⑥ 阵营过滤
    // ==================================================================
    static void A6_ArmyFilter()
    {
        Debug.Log(P + "--- ⑥ 阵营过滤：Goff 的包不会出现在 UM 的候选里 ---");

        var um = DraftPacks.PacksForArmy(10, true);
        var goff = DraftPacks.PacksForArmy(20, true);
        Ok(um.Count > 0 && goff.Count > 0, "⑥ 前提：UM / Goff 两族的候选池都非空");
        Eq(um.Count, 131, "⑥ UM 候选 131 个（全 enabled）"
                        + " 🧨 怎么改会红：`PacksForArmy` 漏了某批包 / 生成器漏了 UM 的一部分");
        Eq(goff.Count, 92, "⑥ Goff 候选 92 个（全 enabled）"
                         + " 🧨 怎么改会红：同上");

        var goffKeys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < goff.Count; i++) { goffKeys.Add(goff[i].key); goffKeys.Add(goff[i].packId); }

        int leaked = 0, wrongArmy = 0;
        for (int i = 0; i < um.Count; i++)
        {
            if (goffKeys.Contains(um[i].key) || goffKeys.Contains(um[i].packId)) leaked++;
            if (um[i].army != 10) wrongArmy++;
        }
        Eq(leaked, 0, "⑥ UM 候选池里 0 个 Goff 的包"
                    + " 🧨 怎么改会红：`PacksForArmy` 过滤条件写错（用了 `packName` 前缀、或漏了 `army`）");
        Eq(wrongArmy, 0, "⑥ UM 候选池里每个包的 `army` 都是 10"
                       + " 🧨 怎么改会红：同上");

        // 抽包层面也验一遍：13 族 × 4 轮，给出的包必须 100% 属于请求的阵营
        int[] armyIds = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130 };
        int bad = 0;
        for (int ai = 0; ai < armyIds.Length; ai++)
            for (int round = 0; round < DraftPacks.RoundsPerDraft; round++)
            {
                var roll = DraftPacks.RollRound(31337, round, armyIds[ai], DraftPacks.PacksPerRoundOffered);
                for (int i = 0; i < roll.Count; i++)
                    if (roll[i].army != armyIds[ai]) bad++;
            }
        Eq(bad, 0, "⑥ 13 族 × 4 轮抽包：给出的包 100% 属于请求的阵营"
                 + " 🧨 怎么改会红：`RollRound` 绕开 `PacksForArmy` 自己筛（很容易漏条件）");
    }

    // ==================================================================
    //  ⑦（额外）armyName 列 × ArmyIcons
    // ==================================================================
    static void A7_ArmyNameColumn(List<DraftPacks.PackDto> packs)
    {
        Debug.Log(P + "--- ⑦ 数据里的 armyName 列 × ArmyIcons（同一件事只许一份权威）---");

        int bad = 0;
        var ex = "";
        for (int i = 0; i < packs.Count; i++)
        {
            var p = packs[i];
            if (p == null) continue;
            var truth = CardPresentation.ArmyIcons.Faction(p.army);
            if (truth == null || truth != p.armyName)
            {
                if (bad == 0) ex = $"（例：packId={p.packId} army={p.army} 数据列=`{p.armyName}` / ArmyIcons=`{truth}`）";
                bad++;
            }
        }
        Eq(bad, 0,
           "⑦ 882 个包的 `armyName` 与 `ArmyIcons.Faction(army)` 逐条一致" + ex
         + " 🧨 怎么改会红：改了 `ArmyIcons` 的阵营名（或改了生成器里的 `ARMIES`）而没同步另一处"
         + " —— 运行期用 `ArmyIcons`、数据列只是副本，所以这条专门抓「两处写同一条规则」");
    }
}
