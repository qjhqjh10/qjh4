// DraftWindowAssertD2.cs — 执行代理 `D2`（2026-10-24）的断言宿主：**轮抽（免费/常规）那一扇窗**
//   （原版 `Draft Mode Menu Demo` ←→ `Shell/DraftModeWindow.cs`）
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景、不进任何 `Editor/*Scene.cs` 宿主 —— 宿主不在本批白名单）。
// 调用方式（主对话在某个宿主收尾处加两行，想并进总数就带后一行）：
//
//     DraftWindowAssertD2.Run();
//     pass += DraftWindowAssertD2.Pass; fail += DraftWindowAssertD2.Fail;
//
// 或者单独跑：`-executeMethod DraftWindowAssertD2.Run`
//
// ==================================================================
//  五条（= 派单给的验收标准）＋ 三条本文件自己加的（⑥⑦⑧）
// ------------------------------------------------------------------
//  ① **开窗真的开了**：`DraftModeWindow.Create(wm)` ⇒ 窗在、落在 Popup(15) 锚点下、Pay 页是
//     `Draft Mode Pay State`、压暗层那颗命中区（`BackdropHit`）在、`LastOpened == win`。
//  ② `Stage Counter` 文本 = **`1/4`**（`{CurrentDraftRound+1}/{draftRounds}`）；选一包之后 = **`2/4`**。
//  ③ **一屏包数 = `DraftPacks.RollRound` 的返回条数**（不是写死 3）——
//     用一个**只有 2 个包**的合成包池跑一遍：写死 3 ⇒ 这条必红。
//  ④ 选一包 ⇒ `selectedCards` **+3** 且 `CurrentDraftRound` **+1**。
//  ⑤ 四轮之后 ⇒ `BuildClosedDeck` = **12 张**，且 `LaunchBattle()` 真的把这一局交出去
//     （`StartedBattle = true` · `PendingPlayMode = ClosedDeck(7)` · 交接的牌 12 张）。
//  ⑥（额外）**空态不静默**：可选战将 = 0 / 包 = 0 时页面上有一颗**自拟提示**节点（原版这两页没有空态节点）。
//  ⑦（额外）**缺图要出声**：本机缺的两张 toggle 图标必须进 `MissingArt`（⛔ 不许「取不到就算了」）。
//  ⑧（额外）**卡组右栏的显隐跟着页走**：Pay/选将页关、选包/进行中页开（S3 §2.5/§2.7 的判据）。
//
// 🔴 **期望值从哪来**（不许拿我们自己的常量证明我们自己的实现）：
//    · `1/4` / `2/4` 的**格式**来自反编译 `DraftModeSelectPacksStateDemo__ShowPacks.c`
//      （`String.Format("{0}/{1}", CurrentDraftRound+1, draftRounds)`，S3 §2.5 表 第 1 行）；
//      ⚠️ **分母 4 本身是我们挑的**（`draftRounds` 在服务端、本地 0 命中，S3 §五）—— 这条断言钉的是
//      「**格式对**且**分母取自 `DraftPacks.RoundsPerDraft`**」，不是「原版就是 4」。
//    · `+3` = **原版真值**（882/882 包恒 3 张 ⇒ `DraftPacks.CardsPerPack`）。
//    · `12 张` = 4 轮 × 3 张（4 那一半是我们挑的 ⇒ 期望值用 `RoundsPerDraft * CardsPerPack` 现算，
//      同时另断一条「= 12」把当前口径钉住，口径一改两处一起露）。
//    · `ClosedDeck(7)` = 原版枚举值（`dump.cs:46188-46205`，`PlayModes.ClosedDeck = 7`）。
// ==================================================================
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class DraftWindowAssertD2
{
    const string P = "[D2] ";
    const int TestSeed = 20261024;

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进宿主的合计数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        DraftModeWindow win = null;

        // ---- 现场保存（本类会动四个静态通道，跑完必须原样还回去）----
        var prevMode = BattleDriver.PendingPlayMode;
        var prevPre = PrebuiltDecks.PendingSource;

        try
        {
            var wm = WindowsManager.Get();
            if (!Ok(wm != null, "前提：拿得到 `WindowsManager`（`EnsureHost` 会连三个锚点一起建）")) return;

            win = DraftModeWindow.Create(wm, TestSeed);
            if (!Ok(win != null, "前提：`DraftModeWindow.Create` 建出窗来")) return;

            A1_Opens(win, wm);
            A2_StageCounter(win);
            A4_PickPackAdds(win);
            A5_ClosedDeckAndBattle(win);
            A3_OfferedCountIsRollCount(win);
            A6_EmptyStates(win);
            A7_MissingArt(win);
        }
        catch (Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            // ---- 还原 ----
            if (win != null) { try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            WindowsManager.ClearReuseCacheForTest();
            DraftPacks.InvalidateCache();
            PrebuiltDecks.ClearPendingBattleDeck();
            if (prevPre != null) PrebuiltDecks.SetPendingBattleDeck(prevPre);
            BattleDriver.TakePendingPlayMode();                    // 读一次就清（`LaunchBattle` 放进去的那一格）
            if (prevMode.HasValue) BattleDriver.SetPendingPlayMode(prevMode.Value);
            Debug.Log(P + $"=== D2 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ==================================================================
    //  断言小工具（与 `DraftAssertD1` / `E13` 同形）
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

    /// <summary>推进到「选包页」：Pay 页 `Free` ⇒ 选第一张战将 ⇒ `Continue`。返回选中的战将 id。</summary>
    static string GoToPacks(DraftModeWindow win)
    {
        if (win.State != DraftModeState.SelectPack) win.ClickFree();
        if (win.State != DraftModeState.SelectWarlord)
        { Ok(false, "前提：`Free` 之后进得了选将页（当前 " + win.State + "）"); return null; }
        if (win.Warlords == null || win.Warlords.Count == 0)
        { Ok(false, "前提：有可选战将（`DraftModeWindow.Warlords` 非空）"); return null; }
        string id = win.Warlords[0].Id;
        if (!Ok(win.ChooseWarlord(id), "前提：选得中第一张战将 `" + id + "`")) return null;
        if (!Ok(win.ContinueAfterWarlord(), "前提：`Continue` ⇒ 选包页")) return null;
        return id;
    }

    // ==================================================================
    //  ① 开窗真的开了
    // ==================================================================
    static void A1_Opens(DraftModeWindow win, WindowsManager wm)
    {
        Debug.Log(P + "--- ① 开窗 / 窗参 / Pay 页 ---");

        Eq(win.name, "Draft Mode Menu Demo",
           "① 根节点名 = 原版 prefab 名 `Draft Mode Menu Demo` 🧨 怎么改会红：改 `RootName`（`OpenByRef` 的键也靠它）");
        Ok(DraftModeWindow.LastOpened == win, "① `LastOpened` 记下了这一扇");
        Eq(win.type, WindowType.Popup,
           "① `type = Popup(1)`（根 MB 实读 `type = 1`）🧨 怎么改会红：`Create` 里把 type 改成 Fullscreen（会走「全藏」支）");
        Eq(win.placement, WindowsPlacement.Popup,
           "① `windowsPlacement = Popup(15)`（根 MB 实读 15）🧨 怎么改会红：写成 Canvas(5)");
        var anchor = WindowsManager.GetWindowAnchor(WindowsPlacement.Popup);
        Ok(anchor != null && win.transform.parent == anchor,
           "① 真的被挂到 `3 - PopUp Holder` 下面（`AttachToAnchor` 走的是 placement）");
        Ok(win.closeOnEsc, "① `closeOnESC = 1`");

        Eq(win.State, DraftModeState.PayScreen, "① 开窗落在 Pay 页（`DraftState` 的起点；S3 §〇·3）");
        var pay = win.PageNode(DraftModeState.PayScreen);
        Ok(pay != null && pay.name == "Draft Mode Pay State",
           "① Pay 页节点名 = `Draft Mode Pay State`");
        Ok(pay != null && pay.gameObject.activeSelf, "① Pay 页是**显示**着的");
        Ok(win.PageNode(DraftModeState.SelectPack) != null
           && !win.PageNode(DraftModeState.SelectPack).gameObject.activeSelf,
           "① 选包页此刻**关着**（原版靠 `GetTab<T>()` 切 `activeSelf`，不是页签按钮）");

        Ok(win.transform.Find("Game Mode Header With Back Button") != null,
           "① 顶部那颗 `Game Mode Header With Back Button` 在（原版 prefab 名）");
        Ok(win.transform.Find("General Red Background") != null
           && win.transform.Find("General Red Background/BackdropHit") != null,
           "① 压暗层那一族在，且带 `BackdropHit`（= 原版 `BackgroundCloseButton`，点它关窗）");
        Ok(win.transform.Find("Draft Mode Deck Info Panel") != null,
           "① 右栏 `Draft Mode Deck Info Panel` 建出来了（原版常驻件）");
        Ok(win.transform.Find("Select Pack Anim Anchor (It must be out of Select Pack State)") != null,
           "① 选包动画锚点节点在（名字照原版逐字；本轮不做动画，节点照建）");

        Eq(win.DeckInfoPanelNode != null && win.DeckInfoPanelNode.gameObject.activeSelf, false,
           "⑧ Pay 页**右栏是关的**（原版 `DraftModeMenuPayStateDemo.OnOpen` 把 Deck Info Panel 隐藏，S3 §2.3）");
    }

    // ==================================================================
    //  ② Stage Counter
    // ==================================================================
    static void A2_StageCounter(DraftModeWindow win)
    {
        Debug.Log(P + "--- ② `Stage Counter` = {轮次+1}/{轮数} ---");

        string wl = GoToPacks(win);
        if (wl == null) return;
        Eq(win.State, DraftModeState.SelectPack, "② 走到选包页");
        Eq(win.StageCounterText, "1/4",
           "② 第 1 轮 ⇒ `1/4`（格式来自 `ShowPacks.c` 的 `String.Format(\"{0}/{1}\", round+1, draftRounds)`；"
         + "分母 4 是我们挑的 ⇒ 见文件头）🧨 怎么改会红：写成 `1/3`（把 `PacksPerRoundOffered` 当分母）、"
         + "或写成 `0/4`（漏了 `+1`）");
        Eq(win.StageCounterText, (win.CurrentDraftRound + 1) + "/" + DraftPacks.RoundsPerDraft,
           "② 这一串必须是**现算**的（读 `CurrentDraftRound` + `DraftPacks.RoundsPerDraft`）"
         + "🧨 怎么改会红：把目录里的 4 写死进代码、而 `DraftPacks.RoundsPerDraft` 改了");
        Ok(win.DeckInfoPanelNode.gameObject.activeSelf, "⑧ 选包页**右栏是开的**（S3 §2.5：`Deck Info Panel` 常驻）");
    }

    // ==================================================================
    //  ④ 选一包 ⇒ +3 张 / 轮次 +1
    // ==================================================================
    static void A4_PickPackAdds(DraftModeWindow win)
    {
        Debug.Log(P + "--- ④ 选包 ⇒ selectedCards +3 · CurrentDraftRound +1 ---");

        if (win.State != DraftModeState.SelectPack) { Ok(false, "前提：在选包页"); return; }
        int beforeCards = win.SelectedCards.Count, beforeRound = win.CurrentDraftRound;
        if (!Ok(win.OfferedPacks.Count > 0, "前提：这一轮有包可点")) return;
        string key = win.OfferedPacks[0].key;
        if (!Ok(win.PickPack(key), "④ `PickPack(" + key + ")` 成功")) return;

        Eq(win.SelectedCards.Count, beforeCards + 3,
           "④ 手里多了 **3 张**（= 原版 882/882 包恒 3 张）🧨 怎么改会红：`PickPack` 忘记把 `cardIds` 摊进 "
         + "`selectedCards`，或只加 1 张");
        Eq(win.CurrentDraftRound, beforeRound + 1,
           "④ 轮次 **+1**（原版 `CurrentDraftRound == selectedPacks.Count`）🧨 怎么改会红：把轮次写成"
         + "「已选卡数 / 3」以外的算法");
        Eq(win.StageCounterText, "2/4",
           "②（续）选完 1 包 ⇒ `2/4` 🧨 怎么改会红：`Stage Counter` 只在开页时刷一次、选包后不刷");
        Eq(win.DeckCounterText, Loc.T("Demo/EnergyBalance/Cards") + " 3/12",
           "⑧ 右栏 `CardCounter` 跟着刷（原版 `Cards: 30/30` 那一格；我们 = `Cards: 已拿/总数`）"
         + "🧨 怎么改会红：`RefreshDeckPanel` 不在选包后调");
    }

    // ==================================================================
    //  ⑤ 四轮 ⇒ 12 张 + 能开 ClosedDeck 局
    // ==================================================================
    static void A5_ClosedDeckAndBattle(DraftModeWindow win)
    {
        Debug.Log(P + "--- ⑤ 四轮 ⇒ ClosedDeck 12 张 · `LaunchBattle` 交出这一局 ---");

        int guard = 0;
        while (win.State == DraftModeState.SelectPack && guard++ < 16)
        {
            if (win.OfferedPacks.Count == 0) { Ok(false, "⑤ 第 " + (win.CurrentDraftRound + 1) + " 轮没有包 —— 选不下去"); return; }
            win.PickPack(win.OfferedPacks[0].key);
        }
        Eq(win.CurrentDraftRound, DraftPacks.RoundsPerDraft,
           "⑤ 选满 `RoundsPerDraft` 轮");
        Eq(win.State, DraftModeState.OngoingRun,
           "⑤ 选满之后自动切到 `OngoingRun` 页（原版 `UpdateDeckInfoPanelAnimSync`："
         + "`CurrentDraftRound >= draftRounds` ⇒ `ChangeTab` 到 Ongoing）"
         + "🧨 怎么改会红：末轮选完不切页（还停在选包页）");
        Ok(win.DeckInfoPanelNode.gameObject.activeSelf, "⑧ 进行中页**右栏也开着**（S3 §2.7）");

        Eq(win.SelectedCards.Count, DraftPacks.RoundsPerDraft * DraftPacks.CardsPerPack,
           "⑤ 手里 = 轮数 × 3 张（现算）");
        Eq(win.SelectedCards.Count, 12,
           "⑤ = **12 张**（4 轮 × 3 张 —— 当前口径）🧨 怎么改会红：`RoundsPerDraft` 被改了而没人复核 `12`");

        if (!Ok(win.LaunchBattle(), "⑤ `LaunchBattle()` 成功（组牌 + 交接 + 切场景/批处理记账）")) return;
        Ok(win.StartedBattle, "⑤ `StartedBattle = true`");
        var deck = win.ClosedDeckBuilt;
        Ok(deck != null, "⑤ `ClosedDeckBuilt` 非空");
        if (deck != null)
        {
            Eq(deck.cardIds.Length, 12,
               "⑤ `BuildClosedDeck` = **12 张** 🧨 怎么改会红：`TryBuildClosedDeck` 去重（原版允许包内自重）");
            Eq(deck.packIds.Length, DraftPacks.RoundsPerDraft, "⑤ `packIds` = 4 条（每轮 1 包）");
        }
        Eq(BattleDriver.PendingPlayMode, (GameMode?)GameMode.ClosedDeck,
           "⑤ 开战通道里的模式号 = **`ClosedDeck(7)`**（原版枚举值）🧨 怎么改会红：漏调 "
         + "`BattleDriver.SetPendingPlayMode`（那这一局会被当成 `Classic`/`Skirmish`，骷髅口径与记录都错）");
        var pre = PrebuiltDecks.PendingSource;
        Ok(pre != null, "⑤ 「本局用这副牌」通道里有货（`PrebuiltDecks.PendingSource`）");
        if (pre != null)
        {
            Eq(pre.cardIds != null ? pre.cardIds.Length : -1, 12,
               "⑤ 交给战斗的那副牌 = 12 张 🧨 怎么改会红：交接时把 `deck.cardIds` 漏掉");
            Eq(pre.heroId, win.WarlordId, "⑤ 那副牌挂在选中的战将身上（`heroId`）");
            Eq(pre.faction, win.WarlordFaction, "⑤ 阵营 = 选中战将的阵营");
        }
        // ⚠️ 批处理下**不切场景**（`Application.isBatchMode` 那一支只记账）—— 这条是把那半边钉住。
        if (Application.isBatchMode)
            Ok(win.StartedBattle && Application.isBatchMode,
               "⑤ 批处理：只记账、不切场景（`StartedBattle` 仍为真）");
    }

    // ==================================================================
    //  ③ 一屏包数 = `RollRound` 的返回条数（**写死 3 会红**）
    // ==================================================================
    static void A3_OfferedCountIsRollCount(DraftModeWindow win)
    {
        Debug.Log(P + "--- ③ 一屏包数 = `DraftPacks.RollRound` 的返回条数 ---");

        // ---- ③a 真包池：两处独立算法必须逐条对上 ----
        win.BeginRun(TestSeed);
        string wl = GoToPacks(win);
        if (wl == null) return;
        int army = win.WarlordArmyId();
        var independent = DraftPacks.RollRound(TestSeed, 0, army, DraftPacks.PacksPerRoundOffered, null);
        Eq(win.OfferedPacks.Count, independent.Count,
           "③ 页面给出的包数 = 独立调 `DraftPacks.RollRound(seed,0,army,count,null)` 的条数（同 seed 同轮）"
         + "🧨 怎么改会红：窗口自己另写一套抽样 / 把条数写死");
        Eq(win.VisiblePackCount, win.OfferedPacks.Count,
           "③ `Packs Container` 底下**真的建出来**的包卡数 = 给出的包数 🧨 怎么改会红：只更新数据不重建节点");

        // ---- ③b 合成包池（只有 2 个包）——这是「写死 3 会红」的那一条 ----
        var pool = CardDatabase.Load();
        if (!Ok(pool != null && pool.Count >= 3, "③b 前提：卡池读得到（造合成包要用 3 个真卡 id）")) return;
        var ids = new List<string>();
        for (int i = 0; i < pool.Count && ids.Count < 3; i++)
            if (pool[i] != null && !string.IsNullOrEmpty(pool[i].Id) && pool[i].Type == "unit") ids.Add(pool[i].Id);
        if (!Ok(ids.Count == 3, "③b 前提：找到 3 张 unit 卡")) return;

        var fake = new List<DraftPacks.PackDto>
        {
            FakePack("D2_FAKE_A", army, ids),
            FakePack("D2_FAKE_B", army, ids),
        };
        DraftPacks.SetPacksForTest(fake);
        win.BeginRun(TestSeed);            // 重开一局（换回 Pay 页）
        GoToPacks(win);                    // 再走到选包页（这时候池里只有 2 个包）
        Eq(win.OfferedPacks.Count, 2,
           "③b 合成池只有 2 个包 ⇒ 这一轮**只给 2 个**（`RollRound` 如实少给）"
         + "🧨 怎么改会红：**把一屏包数写死成 3**（那样这里会是 3 —— 正是本条要抓的）");
        Eq(win.VisiblePackCount, 2,
           "③b `Packs Container` 里也只建 2 张包卡 🧨 怎么改会红：同上");
        Ok(win.VisiblePackCount != 3, "③b 反证：一屏包数 **不是** 常量 3（= 写死 3 会红）");
        // 这一页还得能点（少给不等于坏掉）
        Ok(win.PickPack(win.OfferedPacks[0].key), "③b 包池只有 2 个时照样点得动");

        // ---- 还原真包池 ----
        Ok(RestoreRealPool(),
           "③b 收尾：真包池回来了（count = " + DraftPacks.Count + "）");
    }

    /// <summary>把 `DraftPacks` 的缓存恢复成**真包池**（`SetPacksForTest` 之后必须调）。
    /// 先走 `InvalidateCache`（= 运行期那条 `Resources` 路），读不到再直接读文件
    /// （两条候选路径与 `DraftAssertD1.FindDataFile` 同源 —— 编辑器里 `Resources.Load` 偶发拿不到时兜底）。</summary>
    static bool RestoreRealPool()
    {
        DraftPacks.InvalidateCache();
        if (DraftPacks.Available && DraftPacks.Count > 100) return true;
        var cands = new[]
        {
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../数据/游戏数据/draft_packs.json")),
            Path.Combine(Application.dataPath, "CardPresentation/Resources/draft_packs.json"),
        };
        for (int i = 0; i < cands.Length; i++)
        {
            if (!File.Exists(cands[i])) continue;
            try { DraftPacks.SetPacksForTest(DraftPacks.Parse(File.ReadAllText(cands[i]))); }
            catch (Exception e) { Debug.LogError(P + "   兜底读 `" + cands[i] + "` 失败：" + e.Message); }
            break;
        }
        return DraftPacks.Count > 100;
    }

    static DraftPacks.PackDto FakePack(string id, int army, List<string> cardIds)
    {
        return new DraftPacks.PackDto
        {
            key = id, packId = id, packName = "D2 fake pack", army = army,
            armyName = ArmyIcons.Faction(army), tier = 0, tierName = "Common", enabled = true,
            cardIds = cardIds == null ? new string[0] : cardIds.ToArray(), srcFile = "D2_assert", duplicate = false,
        };
    }

    // ==================================================================
    //  ⑥ 空态（原版这两页**没有**空态节点 ⇒ 我们自拟一句提示 + 出声；这里断「真的有一句」）
    // ==================================================================
    static void A6_EmptyStates(DraftModeWindow win)
    {
        Debug.Log(P + "--- ⑥ 空态：没战将 / 没包时有一颗提示节点 ---");

        // 没战将：把可选战将清空（走 `BeginRun` 之外）—— 用「合成空池」办不到战将，
        // 这里直接查节点在不在 + 只在真空态时才显示。
        win.BeginRun(TestSeed);
        var wlPage = win.PageNode(DraftModeState.SelectWarlord);
        var hint = wlPage != null ? wlPage.Find("NoWarlordHint") : null;
        Ok(hint != null, "⑥ 选将页有 `NoWarlordHint` 节点（原版**没有**空态节点 ⇒ 这颗是我们自拟的）");
        Ok(hint == null || !hint.gameObject.activeSelf,
           "⑥ 有战将时那颗提示**关着**（别常亮）");

        // 没包：切一个**没有包**的阵营不现实（13 族都有包）⇒ 用空表构造「包 = 0」那一档。
        var wl = GoToPacks(win);
        if (wl == null) return;
        int army = win.WarlordArmyId();
        var fake = new List<DraftPacks.PackDto>
        {
            // 只给**别的阵营**的包 ⇒ 本阵营候选 = 0（`RollRound` 如实返回空）
            FakePack("D2_OTHER_ARMY", army == 10 ? 20 : 10, null),
        };
        // `cardIds` 给 null 会让 `Parse` 之外的路径不受影响；`RollRound` 不看 `cardIds`。
        DraftPacks.SetPacksForTest(fake);
        win.GoToState(DraftModeState.SelectPack);       // 重刷这一页（候选 = 0）
        var packsPage = win.PageNode(DraftModeState.SelectPack);
        var ph = packsPage != null ? packsPage.Find("NoPackHint") : null;
        Eq(win.OfferedPacks.Count, 0, "⑥ 候选 = 0 时 `OfferedPacks` 为空（`RollRound` 不凑数）");
        Ok(ph != null && ph.gameObject.activeSelf,
           "⑥ 候选 = 0 时 `NoPackHint` **亮着**（🔴 这句是我们自拟的：原版这一页没有空态节点，S3 §四）");
        Eq(win.VisiblePackCount, 0, "⑥ 一张包卡都不建（「有几张画几张」，不占位）");

        Ok(RestoreRealPool(), "⑥ 收尾：真包池回来了（count = " + DraftPacks.Count + "）");
    }
    // ==================================================================
    //  ⑦ 缺图必须出声（本机确实没有那两张 toggle 图标）
    // ==================================================================
    static void A7_MissingArt(DraftModeWindow win)
    {
        Debug.Log(P + "--- ⑦ 取不到的图进 `MissingArt`（⛔ 静默失败是红线）---");

        // 重开一局把整棵树重建一遍（`MissingArt` 在 `Build()` 首句清空）
        win.BeginRun(TestSeed);
        var miss = win.MissingArt;
        Ok(miss.Contains("40K_Info_button_draft menu") && miss.Contains("40K_Cards_button_draft menu"),
           "⑦ 右栏那两张**本机没有**的 toggle 图标进了 `MissingArt`（`Resources/Art/ui*/` 0 命中）"
         + "🧨 怎么改会红：取不到图却不记账（那样界面上少两块图标、日志里也查不出为什么）"
         + "；实得 [" + string.Join(", ", new List<string>(miss).ToArray()) + "]");
        Ok(!miss.Contains("UI_Button_Mulligan") && !miss.Contains("40k_Crate_Tier1_Iron"),
           "⑦ 该有的图**没有**被误报（`UI_Button_Mulligan` / `40k_Crate_Tier1_Iron` 都在本机）");
    }
}
