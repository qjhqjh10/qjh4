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
        // 🔴 **2026-09-26 改了判据对象**：原来断言的是 `DeckRules.OvertimeEnergy`，而那个常量
        //    已经**退出运行路径**（运行读 `ctx.Vars.overtimeTurn`）⇒ 那是**自证**。
        //    现在改成盯**运行时的真源头**（`GameplayVariables.Classic.overtimeTurn`）。
        CheckTrue(RuleEngine.GameplayVariables.Classic.overtimeTurn == 10,
                  "经典加时阈值 = 10（**运行路径读的就是它**；规则书:51 + 用户 2026-09-17 判据）");
        CheckTrue(RuleEngine.GameplayVariables.Skirmish.overtimeTurn == 10,
                  "遭遇**也有**加时、且用**同一个阈值 10**（用户 2026-09-26；「更早」靠能量涨得快，"
                + "不是另发明一个常数）");
        Check(RuleEngine.DeckRules.OvertimeExtraDraw, 1,
              "加时**只多抽 1 张**（原版代码只做这一件事；规则书那个「抽 2 张」= 常规 1 + 加时 1，不是独立常数）");
        Check(RuleEngine.DeckRules.SkirmishWarlordHealthPenalty, 10, "遭遇模式督军生命 -10（规则书:62）");
        Check(RuleEngine.DeckRules.LegendaryTotalLimit(true), 4, "遭遇模式传说卡总数上限 4（规则书:64）");
        CheckTrue(RuleEngine.DeckRules.LegendaryTotalLimit(false) == int.MaxValue,
                  "经典模式没有传说卡总数上限");

        // 骷髅头阈值（规则书 :36「削减至 20 / 10 / 0 时各获得 1 个骷髅头」）
        var sk = RuleEngine.DeckRules.SkullThresholds;
        Check(sk.Length, 3, "骷髅头 3 档（结算界面那三个 skull 就是它）");
        Check(sk[0], 20, "第一档 20"); Check(sk[1], 10, "第二档 10"); Check(sk[2], 0, "第三档 0");

        // 骷髅头：把敌方督军削到 20 / 10 / 0 各得 1 个（规则书:36）。
        // 用「降到过的最低生命」算 —— 生命只会往下走，「首次得到」不回退。
        Check(RuleEngine.DeckRules.SkullsFor(30), 0, "督军满血没动过 → 0 个骷髅");
        Check(RuleEngine.DeckRules.SkullsFor(21), 0, "只掉到 21 → 还不到第一档");
        Check(RuleEngine.DeckRules.SkullsFor(20), 1, "掉到 20 → 1 个");
        Check(RuleEngine.DeckRules.SkullsFor(15), 1, "掉到 15 → 还是 1 个");
        Check(RuleEngine.DeckRules.SkullsFor(10), 2, "掉到 10 → 2 个");
        Check(RuleEngine.DeckRules.SkullsFor(1), 2, "掉到 1 → 2 个");
        Check(RuleEngine.DeckRules.SkullsFor(0), 3, "掉到 0（打死）→ 3 个");
        Check(RuleEngine.DeckRules.SkullsFor(-5), 3, "负数（超杀）也封顶 3 个");

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

        // 🔴 **2026-09-26 改口径**：缺防御卡**不再算不合法** —— 原版 `DeckUtility.ValidateDeck`
        //    及其三个子校验**都不读防御卡字段**，0 张合法；兜底在开局（`AddGoesSecondCardToDeck`
        //    随机补一张）。判据 → `资料/加时与冲突模式_原版规格.md` §2.7c。
        //    ⚠️ 这条**原来断的是 `DefensiveMissing`** —— 那是我们比原版严，不是原版的要求。
        bad = legal.Clone(); bad.DefensiveId = null;
        Check(RuleEngine.DeckRules.Validate(bad, lookup), RuleEngine.DeckError.None,
              "★ **不带防御卡也是合法卡组**（照原版：那一格可以空着，开局会补一张）");

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

    // ---------------------------------------------------------------- 卡组库

    static void TestDeckLibrary()
    {
        var path = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "../Temp/_decklib_selftest.json"));
        RuleEngine.DeckStore.OverridePath = path;
        try
        {
            RuleEngine.DeckStore.DeleteFile();

            var lib = RuleEngine.DeckLibrary.Load();
            Check(lib.Count, 0, "空目录 → 空卡组库（不抛异常）");
            Check(lib.CurrentIndex, -1, "一套都没有时 CurrentIndex = -1");
            CheckTrue(lib.Current == null, "Current 是 null");

            var a = lib.Create("复仇者之刃");
            Check(lib.Count, 1, "新建一套");
            Check(lib.CurrentIndex, 0, "新建的自动被选中");
            Check(lib.Current.Name, "复仇者之刃", "名字对");

            var b = lib.Create("复仇者之刃");
            Check(lib.Count, 2, "再建一套");
            Check(b.Name, "复仇者之刃 2", "重名自动加序号（库内不重名）");

            CheckTrue(lib.Rename(0, "改名了"), "改名成功");
            Check(lib.Decks[0].Name, "改名了", "名字真的改了");
            CheckTrue(!lib.Rename(0, ""), "空名字不接受（免得 UI 上出现看不见的一行）");
            CheckTrue(!lib.Rename(99, "越界"), "越界索引不炸、返回 false");

            var dup = lib.Duplicate(0);
            Check(lib.Count, 3, "复制出一套");
            CheckTrue(dup != null && dup.Name.StartsWith("改名了"), $"复制件的名字基于原名（{dup?.Name}）");
            CheckTrue(!ReferenceEquals(dup, lib.Decks[0]), "复制是**新的对象**，不是同一个引用");
            dup.CardIds.Add("X");
            Check(lib.Decks[0].CardIds.Count, 0, "改复制件不影响原件");

            lib.Select(0);
            Check(lib.CurrentIndex, 0, "选第 0 套");
            var edited = lib.Current.Clone();
            edited.CardIds.Add("Autarch");
            edited.WarlordId = "Anvirr Keltoc";
            CheckTrue(lib.CommitCurrent(edited), "把编辑结果提交回去");
            Check(lib.Decks[0].CardIds.Count, 1, "提交是真的写进去了");
            Check(lib.Decks[0].WarlordId, "Anvirr Keltoc", "督军也写了");

            // 落盘 + 重新载入（**这是「编辑完关掉再打开还在」的那条路**）
            CheckTrue(lib.Save(), $"落盘成功（{lib.LastError}）");
            var lib2 = RuleEngine.DeckLibrary.Load();
            Check(lib2.Count, 3, "重新载入还是 3 套");
            Check(lib2.CurrentIndex, 0, "选中项也保住了（第 0 套）");
            Check(lib2.Decks[0].CardIds.Count, 1, "卡组内容跨进程保住了");
            Check(lib2.Decks[0].WarlordId, "Anvirr Keltoc", "督军跨进程保住了");

            // 删
            lib2.Select(2);
            CheckTrue(lib2.Delete(2), "删掉选中的那套");
            Check(lib2.Count, 2, "剩 2 套");
            CheckTrue(lib2.CurrentIndex < lib2.Count, $"选中项被夹回合法范围（{lib2.CurrentIndex}）");
            lib2.Select(1);
            lib2.Delete(0);
            Check(lib2.CurrentIndex, 0, "删的是前面的 → 选中项往前挪一位，仍指着同一套");
            // ⚠️ 别写成 `Delete(0) && Delete(0)` —— 第二次返回 false 会让整条断言失败（短路），
            //    看起来像「删不掉」，其实是测试写错（第一版就这么写的）
            CheckTrue(lib2.Delete(0), "删掉最后一套");
            Check(lib2.CurrentIndex, -1, "删空之后 CurrentIndex 回到 -1");
            CheckTrue(!lib2.Delete(0), "空了再删 → false，不炸");

            // 卡组串 —— 🔴 **2026-09-19 改成原版格式**（出处 `decomp_full/CardDeck__Serialize.c` /
            // `__DeserializeDeckString.c`）。判据不只是「能来回」，还要证「**它就是原版那个格式**」⇒
            // 下面 ③ 手写一条**原版玩家会分享出来的**串喂进去，读不出来才算失败。
            var pool = RuleEngine.CardDatabase.Load();
            System.Func<string, RuleEngine.CardDef> look = RuleEngine.CardDatabase.DeckLookup(pool);

            RuleEngine.CardDef w = null, d0 = null, u0 = null;
            foreach (var c in pool)
            {
                if (w == null && c.Type == "hero") w = c;
                if (d0 == null && c.Type == "defence") d0 = c;
                if (u0 == null && c.Type == "unit") u0 = c;
            }
            CheckTrue(w != null && d0 != null && u0 != null, "池子里找得到督军 / 防御卡 / 部队卡各一张");

            System.Func<string, string> b64 = t =>
                System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(t));

            var src = new RuleEngine.PlayerDeck("导出测试", w.Id, d0.Id, new List<string> { u0.Id, u0.Id });
            var str = RuleEngine.DeckLibrary.ExportString(src);

            // ① 串本身是 Base64，且解开就是原版那个形状
            string plain = null;
            try { plain = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(str)); }
            catch { }
            CheckTrue(plain != null, "导出的是合法 Base64");
            // 卡序照原版 `GetLibraryInFull()`：**防御卡 → 督军 → 其余**（函数体两次 `List.Insert(0,…)`）
            // 格式 = `名字:防御卡:督军:其余…;gameMode`
            Check(plain, "导出测试:" + d0.Id + ":" + w.Id + ":" + u0.Id + ":" + u0.Id + ";0",
                  "★ 解开就是原版形状（名字:防御卡:督军:其余，尾巴 `;0` = gameMode）");

            // ② 来回
            var back = RuleEngine.DeckLibrary.ImportString(str, look);
            CheckTrue(back != null, "导出串能原样导回来");
            if (back != null)
            {
                Check(back.Name, "导出测试", "名字对");
                Check(back.WarlordId, w.Id, "督军对");
                Check(back.DefensiveId, d0.Id, "防御卡对");
                Check(back.CardIds.Count, 2, "**重复的卡按张导入**（2 张同 id 还是 2 条）");
            }

            // ③ ★ **手写一条原版玩家会分享出来的串** —— 这一条过了才叫「读得了原版的串」
            var ob = RuleEngine.DeckLibrary.ImportString(
                b64("Original Deck:" + d0.Id + ":" + w.Id + ":" + u0.Id + ";0"), look);
            CheckTrue(ob != null, "★ 手写的**原版格式**串读得出来");
            if (ob != null)
            {
                Check(ob.Name, "Original Deck", "名字读对");
                Check(ob.WarlordId, w.Id, "★ 督军是**按类型**摘出来的（它在第 2 位，不是第 1 位）");
                Check(ob.DefensiveId, d0.Id, "防御卡也对");
                Check(ob.CardIds.Count, 1, "其余卡进了 `CardIds`");
            }

            // ④ 名字里的 `:` 转义成 `%3A`，读回来要还原
            var eb = RuleEngine.DeckLibrary.ImportString(b64("A%3AB:" + w.Id + ";0"), look);
            CheckTrue(eb != null && eb.Name == "A:B", "★ 名字里的 `%3A` 还原成 `:`");

            // ⑤ 池子里没有的卡：**丢掉、但记下来**（原版是静默丢；项目红线不许静默 ⇒ 丢得看得见）
            var bb = RuleEngine.DeckLibrary.ImportString(b64("X:" + w.Id + ":NoSuchCard999;0"), look);
            CheckTrue(bb != null, "有池子外的卡时**仍然导得进来**（**这是原版行为**，不是 bug）");
            Check(RuleEngine.DeckLibrary.LastDroppedIds.Count, 1,
                  "★ 但丢掉的 id **记在 `LastDroppedIds` 里**（不静默）");

            // ⑥ 不是这个格式的 ⇒ null
            CheckTrue(RuleEngine.DeckLibrary.ImportString("", look) == null, "空串 → null");
            CheckTrue(RuleEngine.DeckLibrary.ImportString("只有一段", look) == null, "不是 Base64 → null");
            CheckTrue(RuleEngine.DeckLibrary.ImportString(b64("没有分号"), look) == null, "少了 `;gameMode` 尾巴 → null");
            CheckTrue(RuleEngine.DeckLibrary.ImportString("名字|督军||卡", look) == null,
                      "旧的**自定格式不再认**（`|` 分隔那套已废弃）");

            lib2.Add(back);
            Check(lib2.Count, 1, "导入的进了库");
            CheckTrue(lib2.Decks[0].Name.Contains("导出测试"), $"名字保住了（{lib2.Decks[0].Name}）");
        }
        finally
        {
            RuleEngine.DeckStore.OverridePath = null;
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }

    // ---------------------------------------------------------------- 卡组的「模式」

    /// <summary>🆕 2026-09-26：**模式是卡组的固有属性**（原版 `CardDeck.gameMode` @0x70）——
    /// 建组那一刻打上、**落盘**、导出/导入跟着走、建好之后没有改的路径。
    /// 判据全文 → `资料/加时与冲突模式_原版规格.md` §2.7（这一节只验我们这边的行为）。</summary>
    static void TestDeckGameMode()
    {
        var path = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "../Temp/_deckmode_selftest.json"));
        RuleEngine.DeckStore.OverridePath = path;
        try
        {
            RuleEngine.DeckStore.DeleteFile();

            // ① 默认 = 经典（0）；给 13 就是遭遇
            var classic = new RuleEngine.PlayerDeck("经典牌", "W", null, new List<string> { "A" });
            Check(classic.GameMode, 0, "不传模式的卡组 = **经典（0）**（旧存档读回来也是这个值，向后兼容）");
            CheckTrue(!classic.IsSkirmish, "经典牌 `IsSkirmish` = false");

            var sk = new RuleEngine.PlayerDeck("遭遇牌", "W", "D", new List<string> { "A", "B" },
                                               (int)RuleEngine.GameMode.Skirmish);
            Check(sk.GameMode, 13, "遭遇牌的 `GameMode` = 13（照原版 `PlayModes.Skirmish`）");
            CheckTrue(sk.IsSkirmish, "遭遇牌 `IsSkirmish` = true");

            // ★ 2026-09-26 加：**两条「是不是遭遇」的判据必须一致** ——
            //   `PlayerDeck.IsSkirmish`（按 `GameMode` **枚举**）vs
            //   `GameplayVariables.IsSkirmish`（按 `deckSize` **派生**）。
            //   两条在**不同类型**上、判法不同，真实路径靠 `GameplayVariables.For()` 保证一致；
            //   这里把它钉住（**两处写同一条规则 = 迟早不一致**，本工程的老账）。
            CheckTrue(classic.IsSkirmish == RuleEngine.GameplayVariables.Classic.IsSkirmish,
                      "★ 经典：两条判据一致（卡组那边与参数那边都判「不是遭遇」）");
            CheckTrue(sk.IsSkirmish == RuleEngine.GameplayVariables.Skirmish.IsSkirmish,
                      "★ 遭遇：两条判据一致（卡组那边与参数那边都判「是遭遇」）");

            // ② 克隆 / 复制带上模式（`DeckLibrary.Duplicate` 走的就是它）
            Check(sk.Clone().GameMode, 13, "`Clone()` **带上模式**（复制一副遭遇牌还是遭遇牌）");

            // ③ 落盘 → 读回
            RuleEngine.DeckStore.SaveAll(new List<RuleEngine.PlayerDeck> { classic, sk }, 1, out _);
            var back = RuleEngine.DeckStore.LoadAll();
            Check(back.Count, 2, "两套都写进去了");
            Check(back[0].GameMode, 0, "经典那套读回来还是 0");
            Check(back[1].GameMode, 13, "★ **遭遇那套读回来还是 13**（模式真的落盘了，不是只活在内存里）");
            CheckTrue(back[1].IsSkirmish, "读回来 `IsSkirmish` 也对");

            // ④ ★ **旧存档**（JSON 里根本没有这个键）⇒ 反序列化成 0 = 经典，**不炸、不丢套**
            File.WriteAllText(path,
                "{\"version\":1,\"current\":0,\"decks\":[{\"Name\":\"老存档的牌\",\"WarlordId\":\"W\"," +
                "\"DefensiveId\":\"\",\"CardIds\":[\"A\"],\"CardbackId\":\"\"}]}");
            var old = RuleEngine.DeckStore.LoadAll(out string note);
            Check(old.Count, 1, "★ 旧存档（没有 `GameMode` 这个键）照样读得出来");
            Check(old[0].GameMode, 0, "★ 而且**默认成经典** —— 和原版卡组串「null 写 0」同义");
            CheckTrue(note == null || !note.Contains("失败"), $"读旧存档不算失败（note={note}）");

            // ⑤ 卡组串：导出写真实模式、导入读回来
            var pool = RuleEngine.CardDatabase.Load();
            System.Func<string, RuleEngine.CardDef> look = RuleEngine.CardDatabase.DeckLookup(pool);
            RuleEngine.CardDef w = null, d0 = null, u0 = null;
            foreach (var c in pool)
            {
                if (w == null && c.Type == "hero") w = c;
                if (d0 == null && c.Type == "defence") d0 = c;
                if (u0 == null && c.Type == "unit") u0 = c;
            }
            var skSrc = new RuleEngine.PlayerDeck("遭遇导出", w.Id, d0.Id, new List<string> { u0.Id },
                                                  (int)RuleEngine.GameMode.Skirmish);
            var s = RuleEngine.DeckLibrary.ExportString(skSrc);
            string plain = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(s));
            CheckTrue(plain.EndsWith(";13"), $"★ 卡组串尾巴写的是**真实的模式 13**（原来写死 0）：{plain}");
            var skIn = RuleEngine.DeckLibrary.ImportString(s, look);
            CheckTrue(skIn != null, "导得回来");
            Check(skIn.GameMode, 13, "★ **导入的串把模式带进来了**（原来读了就丢）—— 原版玩家分享的遭遇串我们能读对");

            // ⑥ 卡组库：新建时定模式；提交编辑结果不改模式
            var lib = RuleEngine.DeckLibrary.Load();
            var made = lib.Create("新的遭遇牌", (int)RuleEngine.GameMode.Skirmish);
            Check(made.GameMode, 13, "★ `DeckLibrary.Create(name, 13)` ⇒ **建组那一刻就打上模式**（照原版 `SelectDecksTab.CreateDeck`）");
            var edit = lib.Current.Clone();
            edit.CardIds.Add("X");
            lib.CommitCurrent(edit);
            Check(lib.Current.GameMode, 13, "★ 提交编辑结果之后模式**还在**（`CommitCurrent` 逐字段拷那一段没漏掉它）");
            var lib2 = RuleEngine.DeckLibrary.Load();
            Check(lib2.Decks[lib2.Count - 1].GameMode, 13, "★ 跨进程也还在（这一条挡住「加字段忘了补落盘」那类静默丢数据）");
        }
        finally
        {
            RuleEngine.DeckStore.OverridePath = null;
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }

    // ---------------------------------------------------------------- 防御卡的兜底

    /// <summary>🆕 2026-09-26：**卡组没带防御卡 ⇒ 展开时补一张**。
    /// 判据 → `资料/加时与冲突模式_原版规格.md` §2.7c：
    /// 原版 `DeckUtility.ValidateDeck` **不校验防御卡**（0 张合法），兜底在开局
    /// —— `BattleManager.AddGoesSecondCardToDeck` **从防御卡池随机抽一张**进手牌，
    /// 而 **`:154-166` 写着「卡组里有就用卡组那张」** ⇒ 所以我们这条**只在 `DefensiveId` 为空时**走。</summary>
    static void TestDefenceFallback()
    {
        var pool = RuleEngine.CardDatabase.Load();
        if (pool.Count == 0) { CheckTrue(false, "卡池是空的，这条验不了"); return; }

        // 夹具：找一个**同时有督军和同阵营防御卡**的阵营（不靠卡池顺序撞运气）
        RuleEngine.CardDef hero = null, def = null;
        foreach (var c in pool)
        {
            if (c == null || c.Type != "hero") continue;
            foreach (var d in pool)
                if (d != null && d.Type == "defence" && RuleEngine.DeckRules.SameFaction(d.Faction, c.Faction))
                { hero = c; def = d; break; }
            if (hero != null) break;
        }
        CheckTrue(hero != null && def != null, "池子里找得到一个「督军 + 同阵营防御卡」的阵营");
        if (hero == null || def == null) return;

        var ids = new List<string>();
        foreach (var c in pool)
        {
            if (ids.Count >= 12 || c == null || c.Type != "unit"
                || !RuleEngine.DeckRules.SameFaction(c.Faction, hero.Faction)) continue;
            for (int i = 0; i < RuleEngine.DeckRules.CopyLimit(c.Rarity) && ids.Count < 12; i++) ids.Add(c.Id);
        }
        var deck = new RuleEngine.PlayerDeck("兜底测试", hero.Id, def.Id, ids,
                                             (int)RuleEngine.GameMode.Skirmish);

        // ① **带了** ⇒ 展开出来就是他带的那张，**不多补**
        var withDef = RuleEngine.DeckBuilder.FromDeck(pool, deck, null, hero.Faction);
        int defN = 0; RuleEngine.CardDef got = null;
        foreach (var c in withDef) if (c != null && c.Type == "defence") { defN++; got = c; }
        Check(defN, 1, "① 卡组**带了**防御卡 ⇒ 展开出来只有那一张（不会再多补一张）");
        CheckTrue(got != null && got.Id == deck.DefensiveId, "① ……而且就是他带的那张");

        // ② **没带** ⇒ 补一张**本阵营**的
        var noDef = deck.Clone(); noDef.DefensiveId = null;
        var filled = RuleEngine.DeckBuilder.FromDeck(pool, noDef, null, hero.Faction, new System.Random(7));
        defN = 0; got = null;
        foreach (var c in filled) if (c != null && c.Type == "defence") { defN++; got = c; }
        Check(defN, 1, "★ ② 卡组**没带**防御卡 ⇒ 展开时**补了一张**（原版 `AddGoesSecondCardToDeck` 的兜底）");
        CheckTrue(got != null && RuleEngine.DeckRules.SameFaction(got.Faction, hero.Faction),
                  $"★ ② 补的那张是**本阵营**的（{(got == null ? "?" : got.Faction)} vs {hero.Faction}）"
                + "—— 跨阵营的牌在我们引擎里上不了场（这条是我们的选择，原版那个池子按不按阵营筛没查实）");

        // ③ **可复现**：同一个种子 ⇒ 同一张（对局可复现是项目红线）
        var again = RuleEngine.DeckBuilder.FromDeck(pool, noDef.Clone(), null, hero.Faction, new System.Random(7));
        RuleEngine.CardDef got2 = null;
        foreach (var c in again) if (c != null && c.Type == "defence") got2 = c;
        CheckTrue(got2 != null && got != null && got2.Id == got.Id,
                  "★ ③ 同一个种子 ⇒ **补到同一张**（不许同一副牌两局不一样）");

        // ④ 补的那张**确实来自卡池**（不是凭空造的）
        CheckTrue(got != null && RuleEngine.CardDatabase.FindById(pool, got.Id) != null,
                  "④ 补的那张确实在卡池里");

        // ⑤ 卡池里**没有**防御卡的阵营 ⇒ 不补、也不炸（出声）
        var none = RuleEngine.DeckBuilder.FromDeck(pool, noDef.Clone(), null, "不存在的阵营",
                                                   new System.Random(7));
        defN = 0;
        foreach (var c in none) if (c != null && c.Type == "defence") defN++;
        Check(defN, 0, "⑤ 阵营里没有防御卡（拿不存在的阵营当例子）⇒ **不补、不炸**（只出声）");
    }
}
