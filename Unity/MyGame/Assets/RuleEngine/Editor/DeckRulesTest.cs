// DeckRulesTest.cs — 卡组构筑规则 / 校验 / 存档的自检（并进 RuleEngineTest.Run）
//
// 单独一个文件是因为 RuleEngineTest.cs 已经 1300 行了；两半都是
// `public static partial class RuleEngineTest`，共用同一套 `Check` 和计数。
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static partial class RuleEngineTest
{
    // ---------------------------------------------------------------- 规则常量

    static void TestDeckRules()
    {
        // 组卡限制（规则书 :53「普通/稀有/史诗卡最多 2 张；传说卡最多 1 张」）
        Check(RuleEngine.DeckRules.CopyLimit("legendary"), 1, "传说卡同名上限 1（规则书:53）");
        Check(RuleEngine.DeckRules.CopyLimit("epic"), 2, "史诗卡同名上限 2（规则书:53）");
        Check(RuleEngine.DeckRules.CopyLimit("rare"), 2, "稀有卡同名上限 2（规则书:53）");
        Check(RuleEngine.DeckRules.CopyLimit("common"), 2, "普通卡同名上限 2（规则书:53）");
        Check(RuleEngine.DeckRules.CopyLimit("special"), 2, "special（防御/特殊卡）也按 2 —— 规则书只把传说单列");
        Check(RuleEngine.DeckRules.CopyLimit(""), 2, "稀有度是空串时按 2（22 张没定稀有度的卡不能因此被排掉）");

        // 卡组张数与手牌（规则书 :47-48 / :57-58）
        Check(RuleEngine.DeckRules.CardCount(false), 30, "经典模式 30 张阵营卡（规则书:47）");
        Check(RuleEngine.DeckRules.CardCount(true), 12, "遭遇模式 12 张（规则书:57）");
        Check(RuleEngine.DeckRules.ClassicHandStart, 3, "经典起手 3 张（规则书:48）");
        Check(RuleEngine.DeckRules.ClassicHandLimit, 10, "经典手牌上限 10（规则书:48）");
        Check(RuleEngine.DeckRules.SkirmishHandStart, 4, "遭遇起手 4 张（规则书:58）");
        Check(RuleEngine.DeckRules.SkirmishHandLimit, 8, "遭遇手牌上限 8（规则书:58）");
        Check(RuleEngine.DeckRules.OvertimeEnergy, 10, "后手最大能量到 10 进加时（规则书:51）");
        Check(RuleEngine.DeckRules.OvertimeDrawPerTurn, 2, "加时每回合抽 2（规则书:51）");
        Check(RuleEngine.DeckRules.SkirmishWarlordHealthPenalty, 10, "遭遇模式督军生命 -10（规则书:62）");
        Check(RuleEngine.DeckRules.LegendaryTotalLimit(true), 4, "遭遇模式传说卡总数上限 4（规则书:64）");
        CheckTrue(RuleEngine.DeckRules.LegendaryTotalLimit(false) == int.MaxValue,
                  "经典模式没有传说卡总数上限");

        // 骷髅头阈值（规则书 :36「削减至 20 / 10 / 0 时各获得 1 个骷髅头」）
        var sk = RuleEngine.DeckRules.SkullThresholds;
        Check(sk.Length, 3, "骷髅头 3 档（结算界面那三个 skull 就是它）");
        Check(sk[0], 20, "第一档 20"); Check(sk[1], 10, "第二档 10"); Check(sk[2], 0, "第三档 0");

        // ⚠️ 「一条规则只有一处」—— DeckBuilder.DeckLimit 必须转发到 DeckRules.CopyLimit
        foreach (var r in new[] { "legendary", "epic", "rare", "common", "special", "" })
            Check(RuleEngine.DeckBuilder.DeckLimit(r), RuleEngine.DeckRules.CopyLimit(r),
                  $"DeckBuilder.DeckLimit(\"{r}\") 与 DeckRules.CopyLimit 一致（转发，不另写一套）");

        Check(RuleEngine.DeckBuilder.ClassicDeckSize, RuleEngine.DeckRules.ClassicCards,
              "DeckBuilder.ClassicDeckSize 与 DeckRules.ClassicCards 一致");
    }

    // ---------------------------------------------------------------- 校验

    /// <summary>从真卡池里凑一副合法卡组：1 督军 + 1 防御卡 + 30 张同阵营卡（守住同名上限）。</summary>
    static RuleEngine.PlayerDeck BuildLegalDeck(List<RuleEngine.CardDef> pool, out RuleEngine.CardDef warlord)
    {
        warlord = null;
        foreach (var c in pool) if (c.Type == "hero") { warlord = c; break; }
        if (warlord == null) return null;

        RuleEngine.CardDef def = null;
        foreach (var c in pool)
            if (c.Type == "defence" && RuleEngine.DeckRules.SameFaction(c.Faction, warlord.Faction)) { def = c; break; }

        var ids = new List<string>();
        foreach (var c in pool)
        {
            if (c.Type != "unit" || !RuleEngine.DeckRules.SameFaction(c.Faction, warlord.Faction)) continue;
            for (int i = 0; i < RuleEngine.DeckRules.CopyLimit(c.Rarity) && ids.Count < 30; i++)
                ids.Add(c.Id);
            if (ids.Count >= 30) break;
        }
        return new RuleEngine.PlayerDeck("自检卡组", warlord.Id, def == null ? null : def.Id, ids);
    }

    static void TestDeckValidation()
    {
        var pool = RuleEngine.CardDatabase.Load();
        CheckTrue(pool.Count > 0, "卡池加载得到卡（cards_engine.json）");
        if (pool.Count == 0) return;

        var byId = new Dictionary<string, RuleEngine.CardDef>();
        foreach (var c in pool) byId[c.Id] = c;
        System.Func<string, RuleEngine.CardDef> lookup = id =>
        {
            RuleEngine.CardDef c;
            return (id != null && byId.TryGetValue(id, out c)) ? c : null;
        };

        RuleEngine.CardDef warlord;
        var legal = BuildLegalDeck(pool, out warlord);
        CheckTrue(legal != null, "能从真卡池里凑出一副卡组（督军 + 防御卡 + 30 张同阵营）");
        if (legal == null) return;
        CheckTrue(legal.CardIds.Count == 30, $"凑出来的卡组正好 30 张（实际 {legal.CardIds.Count}）");

        Check(RuleEngine.DeckRules.Validate(legal, lookup), RuleEngine.DeckError.None,
              "这副卡组合法 —— 卡池能凑出合法卡组这件事本身也要成立");

        // 空 id（list 里塞 null）→ 认成 UnknownCard
        var bad = legal.Clone(); bad.CardIds[0] = "不存在的卡";
        Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.UnknownCard, "卡不在池子里");

        bad = legal.Clone(); bad.WarlordId = null;
        Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.NoWarlord, "没选督军");

        bad = legal.Clone(); bad.WarlordId = legal.CardIds[0];
        Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.WarlordNotHero, "督军位放了普通卡");

        bad = legal.Clone(); bad.DefensiveId = null;
        Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.DefensiveMissing, "缺防御卡");

        bad = legal.Clone(); bad.DefensiveId = legal.CardIds[0];
        Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.DefensiveNotDefence, "防御卡位放错了");

        bad = legal.Clone(); bad.CardIds.Add(bad.CardIds[0]);
        Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.TooManyCards, "31 张 → 超了");

        bad = legal.Clone(); bad.CardIds.RemoveAt(0);
        Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.TooFewCards, "29 张 → 不够");

        // 阵营：换一张别的阵营的卡进来
        RuleEngine.CardDef other = null;
        foreach (var c in pool)
            if (c.Type == "unit" && !RuleEngine.DeckRules.SameFaction(c.Faction, warlord.Faction)) { other = c; break; }
        if (other != null)
        {
            bad = legal.Clone(); bad.CardIds[0] = other.Id;
            Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.WrongFaction, "混进了别阵营的卡");
        }

        // 督军/防御卡混进普通卡位
        bad = legal.Clone(); bad.CardIds[0] = legal.WarlordId;
        Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.WarlordInCards, "督军又混进了普通卡位");

        // ---- 同名上限（构造一副「30 张全是同一张卡」的，必然越界）----
        RuleEngine.CardDef legendary = null, nonLegendary = null;
        foreach (var c in pool)
        {
            if (c.Type != "unit" || !RuleEngine.DeckRules.SameFaction(c.Faction, warlord.Faction)) continue;
            if (legendary == null && RuleEngine.DeckRules.IsLegendary(c.Rarity)) legendary = c;
            if (nonLegendary == null && !RuleEngine.DeckRules.IsLegendary(c.Rarity)) nonLegendary = c;
        }
        if (legendary != null)
        {
            bad = legal.Clone(); bad.CardIds[0] = legendary.Id; bad.CardIds[1] = legendary.Id;
            Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.CopyLimitExceeded,
                  $"传说卡带 2 张 → 超上限（{legendary.Name}）");
        }
        if (nonLegendary != null)
        {
            // ⚠️ 不能只把 legal 的 0/1 位换掉 —— 那张卡在别处可能已经有 2 张了，
            //    换完变成 4 张，测的就不是「刚好 2 张合法」而是「超限」（第一版就这么写错了）。
            // 这里显式造一副「该卡正好 2 张」的。
            var ok = new RuleEngine.PlayerDeck(legal.Name, legal.WarlordId, legal.DefensiveId,
                                               new List<string>());
            ok.CardIds.Add(nonLegendary.Id);
            ok.CardIds.Add(nonLegendary.Id);
            foreach (var c in pool)
            {
                if (ok.CardIds.Count >= 30) break;
                if (c.Type != "unit" || !RuleEngine.DeckRules.SameFaction(c.Faction, warlord.Faction)) continue;
                if (c.Id == nonLegendary.Id) continue;              // 跳过它，避免超过 2 张
                int room = System.Math.Min(RuleEngine.DeckRules.CopyLimit(c.Rarity),
                                           30 - ok.CardIds.Count);
                for (int i = 0; i < room; i++) ok.CardIds.Add(c.Id);
            }
            Check(ok.CardIds.Count, 30, "构造出的对照卡组是 30 张");
            Check(ok.CountOf(nonLegendary.Id), 2, $"{nonLegendary.Name} 在对照组里正好 2 张");
            Check(RuleEngine.DeckRules.Validate(ok, lookup), RuleEngine.DeckError.None,
                  $"非传说卡带 2 张 → 合法（{nonLegendary.Name}）");
        }

        // 遭遇模式：30 张对 12 张来说太多了
        Check(RuleEngine.DeckRules.Validate(legal, lookup, skirmish: true), RuleEngine.DeckError.TooManyCards,
              "经典卡组拿去做遭遇模式校验 → 张数超了");

        // 错误码有人话
        CheckTrue(RuleEngine.DeckRules.Describe(RuleEngine.DeckError.None) == "", "None 的人话是空串");
        CheckTrue(RuleEngine.DeckRules.Describe(RuleEngine.DeckError.NoWarlord).Length > 0, "其它错误码有人话");
    }

    // ---------------------------------------------------------------- 存档

    static void TestDeckStoreRoundTrip()
    {
        var path = System.IO.Path.Combine(Application.dataPath, "../Temp/_deckstore_selftest.json");
        path = System.IO.Path.GetFullPath(path);
        RuleEngine.DeckStore.OverridePath = path;
        try
        {
            RuleEngine.DeckStore.DeleteFile();
            string note;
            var empty = RuleEngine.DeckStore.LoadAll(out note);
            Check(empty.Count, 0, "存档不存在时返回空表（不抛异常）");
            CheckTrue(!string.IsNullOrEmpty(note), $"并给出一条说明（实际：{note}）");

            var decks = new List<RuleEngine.PlayerDeck>
            {
                new RuleEngine.PlayerDeck("第一套", "Anvirr Keltoc", "Aspect Shrine",
                                          new List<string> { "Autarch", "Autarch", "Rangers" }),
                new RuleEngine.PlayerDeck("第二套", "Ghazghkull Thraka", null, new List<string>()),
            };
            string err;
            CheckTrue(RuleEngine.DeckStore.SaveAll(decks, out err), $"写存档成功（err={err}）");

            var back = RuleEngine.DeckStore.LoadAll();
            Check(back.Count, 2, "读回来还是 2 套");
            Check(back[0].Name, "第一套", "卡组名保住了");
            Check(back[0].WarlordId, "Anvirr Keltoc", "督军 id 保住了");
            Check(back[0].CardIds.Count, 3, "卡表条数保住了");
            Check(back[0].CardIds[1], "Autarch", "**重复的卡按张存**（2 张 Autarch 读回来还是 2 条）");
            Check(back[1].DefensiveId, "", "null 的防御卡读回来是空串（JsonUtility 的行为，已归一化）");
            Check(back[1].CardIds.Count, 0, "空卡表读回来是空表不是 null");

            // 坏文件不许炸
            File.WriteAllText(path, "{ 这不是 json ");
            var broken = RuleEngine.DeckStore.LoadAll(out note);
            Check(broken.Count, 0, "存档损坏时返回空表");
            CheckTrue(note != null && note.Contains("失败"), $"并说明失败（实际：{note}）");

            RuleEngine.DeckStore.DeleteFile();
            CheckTrue(!File.Exists(path), "DeleteFile 能删掉");
        }
        finally
        {
            RuleEngine.DeckStore.OverridePath = null;
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        CheckTrue(!RuleEngine.DeckStore.Path.Contains("_deckstore_selftest"),
                  "自检结束后 Path 恢复成玩家目录（OverridePath 清干净了）");
    }
}
