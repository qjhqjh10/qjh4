// PlayerDataMathCheck.cs — `Core/PlayerDataMath.cs` + `Shell/PlayerProfileState.cs` 那 10 条的
// **批处理自检**（账 `其他/基础设施` :344 · :351 · :374 · :390 · :401 · :413 · :421 · :474 · :475 · :490）。
//
// ============================ 为什么单开一份 ============================
//  · 那 10 条的落点是**本轮新建的两个文件**（`Core/PlayerDataMath.cs` / `Shell/PlayerProfileState.cs`），
//    现有 15 个自检宿主**没有一个碰过它们**（新建的东西此前不存在）⇒ 不给它们配断言 = 一条都没人盯。
//  · ⚠️ 本批（B89 第 1 波）红线**禁止跑 Unity** ⇒ 本文件写好后**没人跑过**，
//    「断言对不对」要等调度台在同步点把它接进 `_run_8_checks.sh` 才见分晓（见报告 §5 交接）。
//
// ============================ 判据 ============================
//  主判据 = `d:/2/tools/decomp_full/PlayerDataManager__*.c`（真方法体，逐句读过；
//  每条在本文件里都对应一条断言，注释写着「钉的是哪一句」）。
//  交叉 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs`。
//
// ============================ 三条「钉形状」的断言（别删） ============================
//  ① `GetMD5Hash("") == ""` —— 钉的是「**空串，不是 null**」（账上原来写的就是错的）。
//  ② `AreDecksEqual` **不对称** 的两条（多一张 → true / 少一张 → false）——
//     只钉「相等」的话，把实现写成「两边张数相等」依然全绿，等于没测。
//  ③ `IsBattleLogCreated` 的**全 19 档**逐档表 —— 只举两三个例子测不出位掩码解错一档
//     （施工单那张白名单就漏了 `ClosedDeck(110)`）。
//
// ============================ 用法 ============================
//   bash d:/4/Unity/工具/_run_8_checks.sh 里加一条（串行）：
//     unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//       -executeMethod PlayerDataMathCheck.Run -logFile -
//   ⚠️ 判绿红看「PDMATH 合计」，不看退出码（同仓其它宿主的约定）。

using System;
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;
using CardPresentation.Net;
using RuleEngine;

public static class PlayerDataMathCheck
{
    static int _pass, _fail;

    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log("PDMATH ✓ " + what); }
        else { _fail++; Debug.LogError("PDMATH ✗ " + what); }
    }

    /// <summary>批处理入口。</summary>
    public static void Run()
    {
        _pass = _fail = 0;

        CheckMD5();
        CheckAreDecksEqual();
        CheckGetXPFromMatch();
        CheckEnvironment();
        CheckIsBattleLogCreated();
        CheckExtractDeckInfo();
        CheckProfileState();
        CheckB4PureAndBranches();

        Debug.Log("PDMATH 合计: 通过 " + _pass + " / 失败 " + _fail);
    }

    // ============================================================ 401 GetMD5Hash

    static void CheckMD5()
    {
        // 判据 = `__GetMD5Hash.c:16,30`：`if (s == null || s.Length == 0) return String.Empty;`
        string empty = PlayerDataMath.GetMD5Hash("");
        Check(empty != null, "（前提）`GetMD5Hash(\"\")` **不是 null**（原版返 `String.Empty`）");
        Check(empty == "", "`GetMD5Hash(\"\")` == 空串");

        string nul = PlayerDataMath.GetMD5Hash(null);
        Check(nul != null && nul == "", "`GetMD5Hash(null)` == 空串（不是 null）");

        // "abc" 的 MD5 = 900150983CD24FB0D6963F7D28E17F72（UTF-8 / ANSI 在 ASCII 输入下同值）
        const string AbcMd5 = "900150983CD24FB0D6963F7D28E17F72";
        string abc = PlayerDataMath.GetMD5Hash("abc");
        Check(abc == AbcMd5, "`GetMD5Hash(\"abc\")` == " + AbcMd5 + "（实读 = " + abc + "）");
        Check(abc.IndexOf('-') < 0, "`GetMD5Hash(\"abc\")` **没有 `-` 分隔符**（原版 `.Replace(\"-\", \"\")`）");
        Check(abc == abc.ToUpperInvariant(), "`GetMD5Hash` 返回**大写**十六进制（`BitConverter.ToString` 的形状）");
        Check(PlayerDataMath.GetMD5Hash("ABC") != abc, "`GetMD5Hash` **区分大小写**（\"ABC\" ≠ \"abc\"）");
    }

    // ============================================================ 351 AreDecksEqual

    /// <summary>夹具：#i 张卡的 id 直接由数组给（`PlayerDataMath` 那个选择器重载就是为了这个 ——
    /// 不必造 `RuleEngine.CardDef`，也就不会因卡表校验在批处理里炸掉）。</summary>
    static bool DecksEqual(string[] library, string[] cardIds)
    {
        return PlayerDataMath.AreDecksEqual(library, cardIds.Length, i => cardIds[i]);
    }

    static void CheckAreDecksEqual()
    {
        // 判据 = `__AreDecksEqual.c`：rest = new List(library); foreach(c in cards) rest.Remove(c.uniqueId);
        //        return rest.Count == 0;
        Check(DecksEqual(new[] { "A" }, new[] { "A" }), "`AreDecksEqual({\"A\"}, {A})` == true");
        Check(DecksEqual(new[] { "A", "B" }, new[] { "B", "A" }), "`AreDecksEqual` **不看顺序**（{A,B} vs {B,A}）");
        Check(DecksEqual(new string[0], new string[0]), "`AreDecksEqual` 两边都空 == true");
        Check(!DecksEqual(new[] { "A" }, new[] { "B" }), "`AreDecksEqual({\"A\"}, {B})` == false");

        // 🔴 不对称的三条（只钉「相等」的话，把实现写成「张数相等」也全绿 ⇒ 等于没测）
        Check(!DecksEqual(new[] { "A", "A" }, new[] { "A" }),
              "🔴 **少一张 → false**：`AreDecksEqual({A,A}, {A})` == false（表里还剩一张没销掉）");
        Check(DecksEqual(new[] { "A" }, new[] { "A", "A" }),
              "🔴 **多一张 → true**：`AreDecksEqual({A}, {A,A})` == true（原版**不判两边张数相等**，照抄）");
        Check(DecksEqual(new[] { "A" }, new[] { "A", "B" }),
              "🔴 `AreDecksEqual({A}, {A,B})` == true（B 销不掉但表已空）—— 同上那条不对称的第二种形状");

        // 原版：cards 里的 null 元素 → `c.uniqueId` 抛 NRE；我们当 id = null 参与销账（不抛）
        Check(!DecksEqual(new[] { "A" }, new string[] { null }),
              "`AreDecksEqual({\"A\"}, {null})` == false（null 销不掉 A；原版此处抛 NRE，我们出声不抛）");

        // 原版：library == null → `new List<string>(null)` 抛 ANE；我们出声 + false
        Check(!PlayerDataMath.AreDecksEqual((IList<string>)null, 0, i => null),
              "`AreDecksEqual(null, …)` == false（原版抛 ANE，我们出声不抛、也⛔ 不返 true）");
    }

    // ============================================================ 413 GetXPFromMatch（P0）

    static void CheckGetXPFromMatch()
    {
        // 判据 = `__GetXPFromMatch.c` 全文。`DAT_1834b2bb8` = 1.0f ⇒ 赢 = **精确等于 1.0f**。
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Ranked, 1f) == 100, "`GetXPFromMatch(Ranked, 赢)` == 100");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Ranked, 0f) == 30, "`GetXPFromMatch(Ranked, 输)` == 30");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Ranked, 0.5f) == 30,
              "`matchWon` 用的是**精确 `== 1.0f`**（0.5f ⇒ 走输档 = 30）");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.RankedBot, 1f) == 100, "`(RankedBot, 赢)` == 100");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Unranked, 1f) == 100, "`(Unranked, 赢)` == 100");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.UnrankedBot, 0f) == 30, "`(UnrankedBot, 输)` == 30");

        // 🔴 原版 bug 照抄：Campaign 赢的那一档返回 130（= MatchType 值本身）
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Campaign, 1f) == 130,
              "🔴 `(Campaign, 赢)` == **130**（原版 bug 照抄，⛔ 别修）");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Campaign, 0f) == 0, "`(Campaign, 输)` == 0");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Campaign, 0.5f) == 0, "`(Campaign, 0.5f)` == 0（非精确 1.0）");

        // 显式 return 0 的五档
        Check(PlayerDataMath.GetXPFromMatch(MatchType.EventAI, 1f) == 0, "`(EventAI, 赢)` == 0");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Tutorial, 1f) == 0, "`(Tutorial, 赢)` == 0");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.ClosedDeck, 1f) == 0, "`(ClosedDeck, 赢)` == 0");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.ClosedDeckBot, 1f) == 0, "`(ClosedDeckBot, 赢)` == 0");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.TutorialReplay, 1f) == 0, "`(TutorialReplay, 赢)` == 0");

        // ⚠️ 走 default（告警 + 0）那几档 —— 值都是 0，但**不能**把它们塞进上面那组
        //    （那会把「原版没给它们档位」这件事抹掉；`PracticeOffline` 尤其容易被误当成显式 0 档）
        Check(PlayerDataMath.GetXPFromMatch(MatchType.PracticeOffline, 1f) == 0,
              "`(PracticeOffline(50), 赢)` == 0（⚠️ 原版走 **default**、会告警 —— `.c` 里没有 50 这个档）");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Practice, 1f) == 0,
              "`(Practice(90), 赢)` == 0（走 default；⛔ 别顺手给它 XP）");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Duel, 1f) == 0, "`(Duel(150), 赢)` == 0（default）");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Replay, 1f) == 0, "`(Replay(160), 赢)` == 0（default）");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Scene, 1f) == 0, "`(Scene(190), 赢)` == 0（default）");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.FastMode, 1f) == 0, "`(FastMode(200), 赢)` == 0（default）");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.FastModeBot, 1f) == 0, "`(FastModeBot(210), 赢)` == 0（default）");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.FastModeRanked, 1f) == 0,
              "`(FastModeRanked(211), 赢)` == 0（default；⚠️ 它在 `IsBattleLogCreated` 的白名单里，与此处无关）");
        Check(PlayerDataMath.GetXPFromMatch(MatchType.Undefined, 1f) == 0, "`(Undefined(0), 赢)` == 0（default）");
    }

    // ============================================================ 390 GetEnviromentString

    static void CheckEnvironment()
    {
        // 判据 = `__GetEnviromentString.c` 全文 `return DAT_1842a2a30;`；字面量实解 = "LIVE"
        Check(PlayerDataMath.GetEnviromentString() == "LIVE",
              "`GetEnviromentString()` == \"LIVE\"（`stringliteral.json` 里 0x42A2A30 = LIVE）");
    }

    // ============================================================ 421 IsBattleLogCreated

    /// <summary>白名单 = 三段位掩码 + 特例 0xd3，逐位解出（判据 = `__IsBattleLogCreated.c`）。
    /// ⚠️ 这张表是**断言的期望值**，不是实现的抄本 —— 实现里那四行掩码是原样照抄的。</summary>
    static readonly MatchType[] Whitelist =
    {
        MatchType.Ranked, MatchType.RankedBot, MatchType.PracticeOffline,
        MatchType.Practice, MatchType.ClosedDeck, MatchType.ClosedDeckBot,
        MatchType.Duel, MatchType.Unranked, MatchType.UnrankedBot,
        MatchType.FastMode, MatchType.FastModeBot, MatchType.FastModeRanked,
    };

    static void CheckIsBattleLogCreated()
    {
        Check(Whitelist.Length == 12, "（前提）白名单表是 12 档（现读 = " + Whitelist.Length + "）");

        var nonEmpty = new List<int> { 1 };
        var empty = new List<int>();

        var all = (MatchType[])Enum.GetValues(typeof(MatchType));
        int inList = 0;
        foreach (var mt in all)
        {
            bool expected = Array.IndexOf(Whitelist, mt) >= 0;
            if (expected) inList++;
            Check(PlayerDataMath.IsBattleLogCreated(nonEmpty, mt) == expected,
                  "`IsBattleLogCreated(非空表, " + mt + "(" + (int)mt + "))` == " + expected
                + (expected ? "（在白名单里）" : "（不在白名单 ⇒ 一律 false，与表里有没有记录无关）"));
        }
        Check(inList == 12, "（前提）`RuleEngine.MatchType` 19 档里正好 12 档命中白名单（现数 = " + inList + "）");

        // 白名单档 + 空表 ⇒ false；null ⇒ false（两条都是 `.c` 里的 `Count > 0` / `!= null`）
        Check(!PlayerDataMath.IsBattleLogCreated(empty, MatchType.Ranked), "`IsBattleLogCreated(空表, Ranked)` == false");
        Check(!PlayerDataMath.IsBattleLogCreated((List<int>)null, MatchType.Ranked),
              "`IsBattleLogCreated(null, Ranked)` == false");

        // 🔴 单独钉一次 `ClosedDeck(110)` —— 施工单那张表说它「不在白名单」，实际**.c 里在**
        Check(PlayerDataMath.IsBattleLogCreated(nonEmpty, MatchType.ClosedDeck),
              "🔴 `ClosedDeck(110)` **在白名单里**（`0x40100001` 的位 20；施工单那张表写错了）");
        // 反向：`Tutorial(100)` 不在（最容易被顺手加进去的一档）
        Check(!PlayerDataMath.IsBattleLogCreated(nonEmpty, MatchType.Tutorial),
              "`Tutorial(100)` **不在**白名单（表里有记录也返回 false）");
    }

    // ============================================================ 374 ExtractDeckInfo

    static void CheckExtractDeckInfo()
    {
        Check(!PlayerDataMath.ExtractDeckInfo(null).HasValue,
              "`ExtractDeckInfo(null)` == null（原版抛 NRE，我们出声 + null）");

        var noHero = new PlayerDeck("T", null, null, new[] { "A" });
        Check(!PlayerDataMath.ExtractDeckInfo(noHero).HasValue,
              "`ExtractDeckInfo(没有战将的卡组)` == null（原版 `deck.deckHero == null` 抛 NRE）");

        var deck = new PlayerDeck("Deck Name", "UM47", "UMDEF", new[] { "A", "B", "C" })
        {
            CardbackId = "Cardback_AM_Cold Blood",
        };
        var data = PlayerDataMath.ExtractDeckInfo(deck);
        Check(data.HasValue, "（前提）`ExtractDeckInfo(有战将的卡组)` 有值");
        if (!data.HasValue) return;

        var d = data.Value;
        Check(d.deckName == "Deck Name", "`CustomDeckData.deckName` == 卡组名（原版 `deck+0x10`）");
        Check(d.heroID == "UM47", "`CustomDeckData.heroID` == 战将 id（原版 `deckHero.uniqueId`）");
        Check(d.cardID != null && d.cardID.Length == 3, "`CustomDeckData.cardID` 张数 == 卡组张数（原版 `cardLibrary` 逐张）");
        Check(d.cardID != null && d.cardID.Length == 3 && d.cardID[0] == "A" && d.cardID[2] == "C",
              "`CustomDeckData.cardID` **保序**（A,B,C —— 原版 `Select().ToArray()`）");
        Check(d.cardbackID == "Cardback_AM_Cold Blood", "`CustomDeckData.cardbackID` == `PlayerDeck.CardbackId`（原版 `deck+0x28`）");

        // 🔶 我们自己挑的那三格（我们的 `PlayerDeck` 没有这些列）—— 断言钉住「是空的、不是编的」
        Check(d.uniqueId == null, "🔶 `uniqueId`（原版 `deckId`）== null —— 我们的卡组没有 id 这一列");
        Check(d.warcryID == null, "🔶 `warcryID` == null —— 原版取 `deck+0x30`，我们离线没有来源");
        Check(d.lastEditTime == default(DateTime), "🔶 `lastEditTime` == default（原版取 `deck+0x68`，我们没有这一列）");
    }

    // ============================================================ 344 / 474 / 475 / 490 状态

    static void CheckProfileState()
    {
        PlayerProfileState.ResetForTest();
        Check(PlayerProfileState.PracticeMatches.Count == 0, "`ResetForTest` 之后 practiceMatches 为空");
        Check(PlayerProfileState.LocalStartTime == 0, "`ResetForTest` 之后 localStartTime == 0");
        Check(PlayerProfileState.Faction0Points == 0, "`ResetForTest` 之后 faction0Points == 0");

        // 344 AddPracticeMatch = 纯 List.Add（不查重）
        var m1 = new SocialData.ChatMessage { PlayerId = "P1", Sender = "S1" };
        var m2 = new SocialData.ChatMessage { PlayerId = "P1", Sender = "S2" };
        var m3 = new SocialData.ChatMessage { PlayerId = "P2", Sender = "S3" };
        PlayerProfileState.AddPracticeMatch(m1);
        PlayerProfileState.AddPracticeMatch(m2);
        PlayerProfileState.AddPracticeMatch(m3);
        Check(PlayerProfileState.PracticeMatches.Count == 3,
              "`AddPracticeMatch` **不去重**（同一个 PlayerId 约两次就有两条，照原版 `List.Add`）");

        // 474 RemovePracticeMatch = 只删第一条命中
        PlayerProfileState.RemovePracticeMatch("P1");
        Check(PlayerProfileState.PracticeMatches.Count == 2,
              "🔴 `RemovePracticeMatch(\"P1\")` **只删第一条命中**（3 → 2，原始的那条 m2 还在）");
        Check(PlayerProfileState.PracticeMatches.Contains(m2) && !PlayerProfileState.PracticeMatches.Contains(m1),
              "🔴 删掉的是**第一条**（m1 走、m2 留）—— 原版 `RemoveAt(i); return;` 的形状");
        Check(PlayerProfileState.PracticeMatches.Contains(m3), "`RemovePracticeMatch` 不动别的条目（m3 还在）");

        PlayerProfileState.RemovePracticeMatch("不存在的人");
        Check(PlayerProfileState.PracticeMatches.Count == 2, "`RemovePracticeMatch(查不到)` == 什么都不删（也不抛）");

        // 475 ResetFactionPoints = 一个字段清零
        PlayerProfileState.Faction0Points = 42;
        Check(PlayerProfileState.Faction0Points == 42, "（前提）`Faction0Points` 是可写状态（原版 `public int`）");
        PlayerProfileState.ResetFactionPoints();
        Check(PlayerProfileState.Faction0Points == 0, "`ResetFactionPoints()` 把 faction0Points 清零");

        // 490 SetLocalStartTime = Unix 毫秒
        long before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        PlayerProfileState.SetLocalStartTime();
        long after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Check(PlayerProfileState.LocalStartTime >= before - 5 && PlayerProfileState.LocalStartTime <= after + 5,
              "`SetLocalStartTime()` 记的是**当前 Unix 毫秒**（原版 `CurrentMillis()` 的原点 = 1970-01-01，"
            + "现读 = " + PlayerProfileState.LocalStartTime + "）");
        Check(PlayerProfileState.LocalStartTime > 1600000000000L,
              "`SetLocalStartTime()` 不是「开机以来毫秒」那种小数字（> 2020 年的 Unix 毫秒）");

        PlayerProfileState.ResetForTest();
        Check(PlayerProfileState.PracticeMatches.Count == 0 && PlayerProfileState.LocalStartTime == 0,
              "`ResetForTest` 能把三份状态都清回去（自检之间不串味）");
    }

    // ============================================================ B89-E10b：B4 那几条新落的纯函数/开关

    static void CheckB4PureAndBranches()
    {
        // 507 ShouldShowRatePopup —— 判据 `PlayerDataManager__ShouldShowRatePopup.c` 三句
        Check(PlayerDataMath.ShouldShowRatePopup(false, 1), "`ShouldShowRatePopup(未通关, 第1关)` == true（原版唯一会弹的那一档）");
        Check(!PlayerDataMath.ShouldShowRatePopup(false, 0), "`ShouldShowRatePopup(未通关, 第0关)` == false");
        Check(!PlayerDataMath.ShouldShowRatePopup(false, 2), "`ShouldShowRatePopup(未通关, 第2关)` == false（只有第 **1** 关弹）");
        Check(!PlayerDataMath.ShouldShowRatePopup(true, 1), "`ShouldShowRatePopup(已通关, 第1关)` == false（通关过一律不弹）");

        // 423 IsTherePendingMatch · 键 = ((PlayerDataKey)18).ToString() = "LastOpponentInfo"
        Check(PlayerDataMath.PendingMatchKey == "LastOpponentInfo",
              "（前提）待打局的键名 == `LastOpponentInfo`（`PlayerDataKey` 里 18 号就是它）");
        Check(!PlayerDataMath.IsTherePendingMatch((IReadOnlyDictionary<string, string>)null),
              "`IsTherePendingMatch(null)` == false（原版抛 NRE，我们出声不抛、也⛔ 不返 true）");
        Check(!PlayerDataMath.IsTherePendingMatch(new Dictionary<string, string>()),
              "`IsTherePendingMatch(空表)` == false（键不在 ⇒ false）");
        Check(!PlayerDataMath.IsTherePendingMatch(new Dictionary<string, string> { { "LastOpponentInfo", "" } }),
              "`IsTherePendingMatch({LastOpponentInfo:\"\"})` == false（在但值是空串 ⇒ false）");
        Check(PlayerDataMath.IsTherePendingMatch(new Dictionary<string, string> { { "LastOpponentInfo", "{\"a\":1}" } }),
              "`IsTherePendingMatch({LastOpponentInfo:非空})` == true");

        // 555 showDevTools —— 出厂 = 关；可写；ResetForTest 清回关
        PlayerProfileState.ResetForTest();
        Check(!PlayerProfileState.DevToolsEnabled, "`DevToolsEnabled` 出厂 == false（原版 obscurity 未解锁时的值）");
        PlayerProfileState.DevToolsEnabled = true;
        Check(PlayerProfileState.DevToolsEnabled, "`DevToolsEnabled` 是可写开关（账 555 的 `showDevTools()` 读的就是它）");
        PlayerProfileState.ResetForTest();
        Check(!PlayerProfileState.DevToolsEnabled, "`ResetForTest` 把 `DevToolsEnabled` 清回 false");

        // 486 SetAltPorts —— 判据 `PlayerDataManager__SetAltPorts.c` 三句
        //  ⚠️ 它会**落盘** ⇒ 必须先把 `OverridePath` 指到临时文件，否则会改玩家的真联机设置。
        string saved = NetConfig.OverridePath;
        string tmp = System.IO.Path.Combine(Application.temporaryCachePath, "wf_pdmath_netcfg_check.json");
        try
        {
            NetConfig.OverridePath = tmp;
            if (System.IO.File.Exists(tmp)) System.IO.File.Delete(tmp);
            NetConfig.Load();                              // 出厂值（文件不存在 ⇒ 不抛）

            bool before = GameStaticData.DontUseAltPortSetting;   // 🆕 B89-E10c：这一格已归位到 `GameStaticData`
            NetConfig.SetAltPorts(true);
            Check(NetConfig.Current.altPorts, "`SetAltPorts(true)` 把 `altPorts` 写成 true（原版第 ② 句）");
            Check(GameStaticData.DontUseAltPortSetting == !before,
                  "🔴 `SetAltPorts` 第 ① 句是**把全局开关取反**，不是赋值（调一次翻一次）");
            Check(System.IO.File.Exists(tmp), "`SetAltPorts` 第 ③ 句**落了盘**（原版 `SavePlayerData()`）");
            string json = System.IO.File.Exists(tmp) ? System.IO.File.ReadAllText(tmp) : "";
            Check(json.IndexOf("altPorts", StringComparison.Ordinal) >= 0,
                  "落盘的那份 JSON 里**有 `altPorts` 这一格**（否则重启就丢，等于没存）");

            NetConfig.SetAltPorts(false);
            Check(!NetConfig.Current.altPorts, "`SetAltPorts(false)` 又把 `altPorts` 写回 false");
            Check(GameStaticData.DontUseAltPortSetting == before,
                  "再调一次 ⇒ 全局开关翻回原值（两次 = 恒等，正是「取反」的形状）");
        }
        finally
        {
            NetConfig.OverridePath = saved;
            try { if (System.IO.File.Exists(tmp)) System.IO.File.Delete(tmp); } catch { }
            NetConfig.Load();                              // 把缓存换回玩家那份
        }

        CheckCollectorXp();
    }

    // ============================================================ B89-E10c：340 `AddCollectorXp` 增量口 + 本地模拟那一跳

    static void CheckCollectorXp()
    {
        PlayerProfileState.ResetForTest();
        // 🔴 B89-E10e：升级 toast 的**正文词条**必须是 `{0}`（原版 `String.Format(GetTranslation(...), level)`
        //    ⇒ 屏幕上就是等级数字）。这条钉住「文案口径」，防止有人再拿我们自己的实现当标准。
        Check(Loc.T("MenuCollection/Toast/CollectorLevel") == "{0}",
              "🔴 升级 toast 正文词条 `MenuCollection/Toast/CollectorLevel` == `{0}`（原版印的就是等级数字）");
        int baseXp = CardTierConfig.Instance.GetTotalPlayerXPFromInventory();
        Check(PlayerProfileState.CollectorXpGranted == 0, "`ResetForTest` 把 `CollectorXpGranted` 清回 0");
        Check(PlayerProfileState.TotalCollectorXp == baseXp,
              "空增量时 `TotalCollectorXp` == 库存反推那份（`CardTierConfig.GetTotalPlayerXPFromInventory` = " + baseXp + "）");

        // 原版 AddCollectorXp 第 2 句：`CurrentXp += grantedXp`
        int lv = PlayerProfileState.AddCollectorXp(10, false);
        Check(PlayerProfileState.CollectorXpGranted == 10, "`AddCollectorXp(10, false)` ⇒ 增量 == 10");
        Check(PlayerProfileState.TotalCollectorXp == baseXp + 10, "`TotalCollectorXp` == 库存那份 + 10");
        Check(lv == CardTierConfig.Instance.GetLevelForXP(baseXp + 10),
              "`AddCollectorXp` 返回的等级 == `GetLevelForXP(总 XP)`（原版返回 `CurrentLevel`）");

        PlayerProfileState.AddCollectorXp(-10, false);
        Check(PlayerProfileState.CollectorXpGranted == 0, "`AddCollectorXp(-10, false)` 减回去（原版不判正负，照抄）");

        // 🔴 与 `GetXPFromMatch` **没有调用关系**（原版 0 调用点）—— 这里只钉「GrantMatchXp 用的是它的值」
        //  ⚠️ 全部传 `showToast: false`：批处理里没有 toast 控制器，弹那条只会刷一串 LogError（不是断言失败）。
        PlayerProfileState.ResetForTest();
        Check(PlayerProfileState.GrantMatchXp(MatchType.Practice, true, false) == 0,
              "`GrantMatchXp(Practice(90), 赢)` == 0（走 default 那一档 ⇒ 一分不发，也不弹）");
        Check(PlayerProfileState.CollectorXpGranted == 0, "`GrantMatchXp` 给 0 时**不动**增量层");

        PlayerProfileState.ResetForTest();
        Check(PlayerProfileState.GrantMatchXp(MatchType.Ranked, true, false) == 100,
              "🔶 `GrantMatchXp(Ranked, 赢)` == 100（值来自 `GetXPFromMatch` —— 本地模拟，原版这条值来自服务器回包）");
        Check(PlayerProfileState.CollectorXpGranted == 100, "🔶 `GrantMatchXp` 把那 100 记进了增量层");

        PlayerProfileState.ResetForTest();
        Check(PlayerProfileState.GrantMatchXp(MatchType.Ranked, false, false) == 30, "🔶 `GrantMatchXp(Ranked, 输)` == 30");
        PlayerProfileState.ResetForTest();
        Check(PlayerProfileState.GrantMatchXp(MatchType.Campaign, true, false) == 130,
              "🔶 `GrantMatchXp(Campaign, 赢)` == 130（原版 bug 值照抄，见 `GetXPFromMatch`）");
        PlayerProfileState.ResetForTest();
        Check(PlayerProfileState.CollectorXpGranted == 0, "`ResetForTest` 之后增量层回 0（自检之间不串味）");

        // ---- 🆕 B89-E10d：① 等级看得到增量 ② 落盘往返（走 `SaveCenter`，`OverrideRoot` 指临时目录）----
        string savedRoot = SaveCenter.OverrideRoot;
        string tmpRoot = System.IO.Path.Combine(Application.temporaryCachePath, "wf_pdmath_save");
        try
        {
            SaveCenter.OverrideRoot = tmpRoot;
            System.IO.Directory.CreateDirectory(tmpRoot);
            PlayerProfileState.ResetForTest();
            PlayerProfileState.AddCollectorXp(42, false);

            Check(PlayerProfileState.CollectorXpGranted == 42, "（前提）加 42 之后增量 == 42");
            // 🔴 任务 ①：`CardTierConfig.PlayerLevel` 必须把这一层算进去（否则界面上等级不动）
            Check(CardTierConfig.PlayerLevel == CardTierConfig.Instance.GetLevelForXP(baseXp + 42),
                  "🔴 `CardTierConfig.PlayerLevel` **看得到增量层**（== `GetLevelForXP(库存那份 + 42)`）");

            // 🔴 任务 ②：落盘往返
            Check(System.IO.File.Exists(SaveCenter.PathOf(PlayerProfileState.CollectorXpSaveKey)),
                  "`AddCollectorXp` 落了盘（`SaveCenter.PathOf(CollectorXpSaveKey)` 那个文件在）");
            PlayerProfileState.ReloadCollectorXpFromSave();
            Check(PlayerProfileState.CollectorXpGranted == 42,
                  "🔴 **落盘 → 重新读盘**之后增量仍是 42（真往返，不是内存里的残值）");
        }
        finally
        {
            SaveCenter.OverrideRoot = savedRoot;
            try
            {
                string f = System.IO.Path.Combine(tmpRoot, PlayerProfileState.CollectorXpSaveKey + ".json");
                if (System.IO.File.Exists(f)) System.IO.File.Delete(f);
                if (System.IO.Directory.Exists(tmpRoot)) System.IO.Directory.Delete(tmpRoot, true);
            }
            catch { }
            PlayerProfileState.ResetForTest();
        }
    }
}
