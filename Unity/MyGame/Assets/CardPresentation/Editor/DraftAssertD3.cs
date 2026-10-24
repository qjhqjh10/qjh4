// DraftAssertD3.cs — 执行代理 `E3`（2026-10-24）的断言宿主：轮抽的三条尾巴
//   **(A) 卡包预览弹窗** · **(B) 战斗结果回灌** · **(C) `Finished` 的触发者**
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景、不进任何 `Editor/*Scene.cs` 宿主 —— 宿主不在本批白名单）。
// 调用方式（主对话在某个宿主收尾处加两行，想并进总数就带后一行）：
//
//     DraftAssertD3.Run();
//     pass += DraftAssertD3.Pass; fail += DraftAssertD3.Fail;
//
// 或者单独跑：`-executeMethod DraftAssertD3.Run`
//
// ==================================================================
//  🔴 期望值从哪来（⛔ 不许拿我们自己的常量证明我们自己的实现）
// ------------------------------------------------------------------
//  · **(A) 版面**：`bundle_menus_assets_all` 的 `Generic Multi Card Display`（父链实读到 `Draft Mode Menu Demo`）
//    根 RT `RectTransform_-5481408756880803131.json`：`anchor(0,.5)-(1,.5) · apos(0,−20.475) · size(0,818.04)`
//    ⇒ 设计帧 1920×1080 下 y[151.455, 969.495]；`CardUI Reference` 布局框 2.54×3.37 × `m_LocalScale 223.1`
//    ⇒ 视觉 **567.60×751.67**；`Content` 的 `HorizontalLayoutGroup.m_Spacing = 500` · `m_ChildAlignment = 4`
//    · `apos.x = −8` ⇒ 三个卡心 **449.46 / 952.00 / 1454.54**（间距 502.54 < 卡宽 567.60 ⇒ **相邻重叠 65.06**）。
//    这几条**不是**从 `DraftModeWindow` 的常量抄来的期望值：它们是**prefab 字段**算出来的，
//    🧨 谁把摆位改成「三张平分屏宽」（= 392.4 / 960 / 1527.6）这几条立刻红。
//  · **(A) 行为**：`DraftModeSelectPacksStateDemo__PackPreview.c`（`GetCardLibrary` → `Initialize(cards, **null**)`
//    → `WindowsManager.OpenWindow`）+ `UIMultiCardDisplay__Initialize.c` 末句
//    `headerText.SetActive(!IsNullOrEmpty(headerTitle))` ⇒ **标题为空 ⇒ 关**。
//    `BackgroundCloseButton`（`MonoBehaviour_-1185320953084347707.json`）的 `window = {PathID:0}`
//    + `onClick` 空 ⇒ **点背景不关窗**（所以那颗不是 `ShadeHit`）。
//  · **(B) 计数**：`DraftEventBase.OnMatchFinished` → `PlayerDataManager.RecordEventMatchFinished`
//    →（服务端）`numberOfMatchesWon/Lost`；`DraftRunDisplay__Initialize.c` 第一句
//    `victoryCounterText.text = save+0x48.ToString()` ⇒ **Victories 数字 = 胜场**。
//    平局是**第三格**（`numberOfMatchesDraw +0x50`）⇒ 两边都不算。
//  · **(C) 状态机**：`DraftEventBase.UpdateState`（泛型实例 **VA `0x181814090`**，逐句反汇编）：
//    `lost >= maxTries` / `joinState == Finished(2)` / `won >= matches.Length` ⇒ `rewardsCollected ? 22 : 20`；
//    否则 `selectedPacks.Count < draftRounds ? 10 : 15`；`selectedWarlord == null` 时
//    `(joinState==Joined && payState==notPaid) ? 0 : 5`。
//    ⇒ 🔴 **「4 轮打完」落 `OngoingRun(15)`，⛔ 不是 `Finished(20)`**（第 ⑤ 组那条专门钉它）。
//    第三条（`won >= matches.Length`）的 `matches` 是**服务端值、本地 0 命中** ⇒ 我们**不设胜场上限**
//    （第 ⑯ 条把「没有上限」钉住：谁自己编一个上限就红）。
//  · **`maxTries = 3`**：`StagedEventData.maxTries(+0x90)` 在服务端；本地唯一的旁证 =
//    prefab 的 `Losses container` **恒 3 格**（S3 §2.7「失败 3 场结束」）+ `SetupStageMarks` 把它当 `max`。
//    🔴 **3 是我们挑的**，本条断言钉的是「= `DraftModeWindow.MaxTries` 且 = 败场格数」，不是「原版就是 3」。
// ==================================================================
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class DraftAssertD3
{
    const string P = "[D3] ";
    const int TestSeed = 20261024;

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        DraftModeWindow win = null;

        // 现场保存（本类会动三个静态通道 + 包池缓存，跑完必须原样还回去）
        var prevMode = BattleDriver.PendingPlayMode;
        var prevPre = PrebuiltDecks.PendingSource;
        var prevRun = DraftModeWindow.RunForTest;

        try
        {
            var wm = WindowsManager.Get();
            if (!Ok(wm != null, "前提：拿得到 `WindowsManager`")) return;

            win = DraftModeWindow.Create(wm, TestSeed);
            if (!Ok(win != null, "前提：`DraftModeWindow.Create(wm, seed)` 建出窗来")) return;

            WarlordPageGroup(win);
            PreviewGroup(win);
            BattleResultGroup(win);
            FinishedGroup(win);
            ResumeGroup(wm);
        }
        catch (Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            if (win != null) { try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            DraftModeWindow.ClearRunForTest();                     // ⛔ 别把自检那一局留给下一次真 Play
            WindowsManager.ClearReuseCacheForTest();
            DraftPacks.InvalidateCache();
            PrebuiltDecks.ClearPendingBattleDeck();
            if (prevPre != null) PrebuiltDecks.SetPendingBattleDeck(prevPre);
            BattleDriver.TakePendingPlayMode();                    // 读一次就清（`LaunchBattle` 放进去的那一格）
            if (prevMode.HasValue) BattleDriver.SetPendingPlayMode(prevMode.Value);
            if (prevRun != null) DraftModeWindow.SetRunForTest(prevRun);   // 万一外面本来有一局，还回去
            Debug.Log(P + $"=== D3 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ==================================================================
    //  断言小工具（与 `DraftWindowAssertD2` / `DraftAssertD1` 同形）
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

    static bool Near(float got, float want, float tol, string msg)
    {
        return Ok(Mathf.Abs(got - want) <= tol, msg + $"（实得 {got:F2} / 期望 {want:F2} ± {tol:F2}）");
    }

    static string GoToPacks(DraftModeWindow win)
    {
        if (win.State != DraftModeState.SelectPack) win.ClickFree();
        if (win.State != DraftModeState.SelectWarlord)
        { Ok(false, "前提：`Free` 之后进得了选将页（当前 " + win.State + "）"); return null; }
        if (win.Warlords == null || win.Warlords.Count == 0)
        { Ok(false, "前提：有可选战将"); return null; }
        string id = win.Warlords[0].Id;
        if (!Ok(win.ChooseWarlord(id), "前提：选得中第一张战将 `" + id + "`")) return null;
        if (!Ok(win.ContinueAfterWarlord(), "前提：`Continue` ⇒ 选包页")) return null;
        return id;
    }

    static DraftPacks.PackDto FakePack(string id, int army, string[] cardIds)
    {
        return new DraftPacks.PackDto
        {
            key = id, packId = id, packName = "D3 fake " + id, army = army,
            armyName = ArmyIcons.Faction(army), tier = 0, tierName = "Common", enabled = true,
            cardIds = cardIds, srcFile = "D3_assert", duplicate = false,
        };
    }

    /// <summary>造两个「卡完全不同」的合成包（预览要断「换了包，画出来的卡也跟着换」）。</summary>
    static List<DraftPacks.PackDto> TwoFakePacks(int army, out string[] idsA, out string[] idsB)
    {
        idsA = null; idsB = null;
        var pool = CardDatabase.Load();
        if (pool == null) return null;
        var all = new List<string>();
        for (int i = 0; i < pool.Count && all.Count < 6; i++)
            if (pool[i] != null && !string.IsNullOrEmpty(pool[i].Id) && pool[i].Type == "unit") all.Add(pool[i].Id);
        if (all.Count < 6) return null;
        idsA = new[] { all[0], all[1], all[2] };
        idsB = new[] { all[3], all[4], all[5] };
        return new List<DraftPacks.PackDto> { FakePack("D3_PACK_A", army, idsA), FakePack("D3_PACK_B", army, idsB) };
    }

    // ==================================================================
    //  (A) 卡包预览弹窗
    // ==================================================================
    static void PreviewGroup(DraftModeWindow win)
    {
        Debug.Log(P + "--- (A) 卡包预览弹窗（原版 `Generic Multi Card Display`）---");

        // ---- ① 节点在、默认关（原版 GO `m_IsActive = false`；`UIMultiCardDisplay.Awake` 也关它）----
        win.BeginRun(TestSeed);
        var prev = win.PackPreviewNode;
        Ok(prev != null, "① 预览层节点建出来了（原版 prefab 名 `Generic Multi Card Display`）");
        if (prev != null)
        {
            Ok(!prev.gameObject.activeSelf,
               "① 预览层**默认关着**（原版 `Generic Multi Card Display.json` 的 `m_IsActive = false`）"
             + "🧨 怎么改会红：`Build()` 里忘了 `SetActive(false)`（那样一开窗就糊一层黑 + 三张卡）");
            Ok(prev.Find("Menu Dark Background") != null && prev.Find("Viewport/Content") != null
               && prev.Find("Close button") != null && prev.Find("Header Text") != null,
               "① 四件都在：`Menu Dark Background` / `Viewport/Content` / `Close button` / `Header Text`（名字逐字照 prefab）");
            var backTx = prev.Find("Close button/Text");
            Ok(backTx != null, "① `Close button/Text` 在（原版那颗 'Back' 字）");
            Ok(!win.PackPreviewHeaderVisible,
               "① `Header Text` **不显示**（`PackPreview` 给 `Initialize(cards, null)` ⇒ "
             + "`Initialize` 末句 `SetActive(!IsNullOrEmpty(headerTitle))` = 关）"
             + "🧨 怎么改会红：把 pack 名当标题传进去（原版这一刻**没有**标题）");
        }
        Ok(!win.PackPreviewVisible, "① `PackPreviewVisible` = false");

        // ---- ② 合成两个卡完全不同的包（真包池随机、可能两个包 3 张一样 ⇒ 断不出「跟着换」）----
        // ⚠️ 顺序要紧：**先选定战将**才知道阵营（`RollRound` 按 `packArmy` 筛），包池换完再 `Continue`。
        win.BeginRun(TestSeed);
        win.ClickFree();
        if (!Ok(win.State == DraftModeState.SelectWarlord, "② 前提：`Free` ⇒ 选将页")) { RestoreRealPool(); return; }
        if (!Ok(win.Warlords != null && win.Warlords.Count > 0, "② 前提：有可选战将")) { RestoreRealPool(); return; }
        string wl = win.Warlords[0].Id;
        if (!Ok(win.ChooseWarlord(wl), "② 前提：选得中第一张战将 `" + wl + "`")) { RestoreRealPool(); return; }
        int army = win.WarlordArmyId();
        string[] idsA, idsB;
        var fake = TwoFakePacks(army, out idsA, out idsB);
        if (!Ok(fake != null, "② 前提：卡池里能凑出 6 张 unit 卡（造两个合成包）")) { RestoreRealPool(); return; }
        DraftPacks.SetPacksForTest(fake);
        if (!Ok(win.ContinueAfterWarlord(), "② `Continue` ⇒ 选包页（此刻池里只有 2 个合成包）")) { RestoreRealPool(); return; }
        Eq(win.OfferedPacks.Count, 2, "② 合成池只有 2 个包 ⇒ 这一轮就 2 个（=`RollRound` 如实少给）");

        // ---- ③ 打开预览 ----
        string keyA = win.OfferedPacks[0].key;
        Ok(win.ShowPackPreview(keyA), "③ `ShowPackPreview(\"" + keyA + "\")` 成功");
        Ok(win.PackPreviewVisible, "③ 预览层亮了");
        Eq(win.PackPreviewCardCount, 3, "③ 画了 **3 张**卡（原版 882/882 包恒 3 张 ⇒ `GetCardLibrary` 给 3 张）"
          + "🧨 怎么改会红：只画 1 张 / 把包名当成一张卡");
        for (int i = 0; i < 3; i++)
            Eq(win.PackPreviewCardIdAt(i), idsA[i], "③ 第 " + (i + 1) + " 张 = 包 A 的 `cardIds[" + i + "]`（顺序一致）");

        // ---- ④ 版面（期望值 = prefab 字段算出来的，⛔ 不是抄本文件的常量）----
        PxRect r0, r1, r2;
        // ⚠️ 三条分开调：`out` 参数在 `&&` 短路里可能没被赋值（CS0165）
        bool have0 = win.PackPreviewCardRectAt(0, out r0);
        bool have1 = win.PackPreviewCardRectAt(1, out r1);
        bool have2 = win.PackPreviewCardRectAt(2, out r2);
        bool have = have0 && have1 && have2;
        Ok(have, "④ 三张卡的矩形都记下来了");
        if (have)
        {
            Near(r0.CX, 449.46f, 0.02f, "④ 第 1 张卡心 x = **449.46**（`Content` apos −8 ⇒ 中心 952；"
                 + "总宽 3×2.54+2×500 = 1007.62 ⇒ 左沿 448.19 ⇒ 卡心 449.46）"
                 + "🧨 怎么改会红：三张平分屏宽（那会是 392.4 / 960 / 1527.6）");
            Near(r1.CX, 952.00f, 0.02f, "④ 第 2 张卡心 x = **952.00**（= 960 + apos −8）");
            Near(r2.CX, 1454.54f, 0.02f, "④ 第 3 张卡心 x = **1454.54**");
            Near(r1.CX - r0.CX, 502.54f, 0.02f, "④ 卡心间距 = **502.54**（`CardUI Reference` 布局框 2.54 + HLG 间距 500）");
            Near(r0.W, 567.60f, 0.02f, "④ 卡视觉宽 = **567.60**（2.54 × `m_LocalScale 223.1`）");
            Near(r0.H, 751.67f, 0.02f, "④ 卡视觉高 = **751.67**（3.37 × 223.1）");
            Ok(r1.CX - r0.CX < r0.W,
               "④ 相邻卡**重叠**（间距 502.54 < 卡宽 567.60，重叠 65.06）—— 这是 prefab 那张真实的版面"
             + "🧨 怎么改会红：改成「首尾不重叠、居中铺开」（那要么缩卡、要么把间距写大）");
            Near(r0.CY, 560.48f, 0.02f, "④ 卡心 y = **560.48**（预览根 y[151.46,969.50] 的竖直中心）");
        }

        // ---- ⑤ 换一个包 ⇒ 画出来的卡跟着换（断「不是写死/不是缓存」）----
        string keyB = win.OfferedPacks[1].key;
        Ok(win.ShowPackPreview(keyB), "⑤ 换预览第二个包（`" + keyB + "`）");
        for (int i = 0; i < 3; i++)
            Eq(win.PackPreviewCardIdAt(i), idsB[i], "⑤ 第 " + (i + 1) + " 张换成了包 B 的卡（⛔ 不是上一包的残留）");
        Eq(win.PackPreviewCardCount, 3, "⑤ 还是 3 张（清旧 + 重建，⛔ 不叠成 6 张）");

        // ---- ⑥ 隐藏时**不吃点击**（活性的那一关；判据 = `PointerLayer.HitQuad` 的 `isActiveAndEnabled`）----
        var prevNow = win.PackPreviewNode;
        var backNode = prevNow != null ? prevNow.Find("Close button") : null;
        var backBtn = backNode != null ? backNode.GetComponentInChildren<WindowButton>() : null;
        Ok(backBtn != null, "⑥ `Close button` 上真的有 `WindowButton`（`MenuDraw.Hit` 建的）");
        if (backBtn != null)
        {
            Ok(PointerLayer.HitQuadForTest(backBtn) != null,
               "⑥ 预览**开着**时那颗钮可命中（`HitQuadForTest` 非空）");
            win.ClosePackPreview();
            Ok(!win.PackPreviewVisible, "⑥ `ClosePackPreview()` ⇒ 关上了");
            Ok(PointerLayer.HitQuadForTest(backBtn) == null,
               "⑥ 预览**关着**时那颗钮**不可命中**（`PointerLayer.HitQuad` 的 `isActiveAndEnabled` 那一关）"
             + "🧨 怎么改会红：把预览层改成「只把 quad 的 alpha 置 0 / 移出屏外」而节点仍 active"
             + " —— 那样关着的预览会**偷吃**轮抽窗的点击，而画面上什么都看不到");
        }
        else win.ClosePackPreview();
        Eq(win.PackPreviewCardCount, 0, "⑥ 关掉时把卡也清了（⛔ 不留在树里）");

        // ---- ⑦ 不在这一轮的包里 ⇒ 拒绝并出声（⛔ 不抛异常、不静默开一个空预览）----
        Ok(!win.ShowPackPreview("D3_NOT_OFFERED"), "⑦ `ShowPackPreview(不在本轮包里的 key)` ⇒ **false**");
        Ok(!win.PackPreviewVisible, "⑦ 而且**没有**开预览（空预览 = 玩家看到一片黑，那是骗人）");

        // ---- ⑧ 换页自动收（原版那是另一扇窗，`ChangeTabCO` 不会带着它走）----
        Ok(win.ShowPackPreview(win.OfferedPacks[0].key), "⑧ 再开一次预览");
        Ok(win.ESCPressed() && !win.PackPreviewVisible && win.gameObject.activeSelf,
           "⑧ ESC ⇒ **只关预览**、轮抽窗还在（原版预览是另一扇窗、`closeOnESC = 1`）"
         + "🧨 怎么改会红：不在 `ESCPressed()` 里先吃掉这一层（那 ESC 会连轮抽窗一起关掉）");
        Ok(win.ShowPackPreview(win.OfferedPacks[0].key), "⑧ 再开一次预览（给下一条用）");
        win.GoToState(DraftModeState.OngoingRun);
        Ok(!win.PackPreviewVisible, "⑧ 换页 ⇒ 预览自动收（🧨 怎么改会红：`GoToState` 忘了那句 ClosePackPreview）");

        Ok(RestoreRealPool(), "⑧ 收尾：真包池回来了（count = " + DraftPacks.Count + "）");
        Ok(!win.MissingArt.Contains("40k_UI_bt_back"),
           "⑧ `Close button` 的底图 `40k_UI_bt_back` **没有**进 `MissingArt`（本机有这张图）");
    }

    /// <summary>与 `DraftWindowAssertD2.RestoreRealPool` 同源（两条候选路径）。</summary>
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

    // ==================================================================
    //  (B) 战斗结果回灌
    // ==================================================================
    static BattleLogData.Match LogRow(BattleLogData.Outcome r, string mode)
    {
        return new BattleLogData.Match { Result = r, Mode = mode, OwnHeroName = "D3", EnemyHeroName = "bot" };
    }

    static string DraftModeName() { return PlayModeNames.Name(GameMode.ClosedDeck); }

    static void BattleResultGroup(DraftModeWindow win)
    {
        Debug.Log(P + "--- (B) 战斗结果回灌（原版 `OnMatchFinished` → 战绩两格）---");

        // ---- ① 纯函数：只认 `ClosedDeck`、新→旧、平局单独一档 ----
        string d = DraftModeName();
        Eq(d, "ClosedDeck", "① 轮抽的模式串 = `ClosedDeck`（`PlayModeNames.Name(GameMode.ClosedDeck)`）");
        var log = new List<BattleLogData.Match>
        {
            LogRow(BattleLogData.Outcome.Victory, d),               // 最新：轮抽·胜
            LogRow(BattleLogData.Outcome.Victory, "Classic"),       // 别的模式 ⇒ 必须被跳过
            LogRow(BattleLogData.Outcome.Defeat, d),                // 轮抽·负
            LogRow(BattleLogData.Outcome.Draw, d),                  // 轮抽·平 ⇒ null
        };
        var outs = DraftModeWindow.DraftOutcomes(log);
        Eq(outs.Count, 3, "① 4 条记录里只有 **3 条**是轮抽局（`Mode != ClosedDeck` 的那条被跳过）"
          + "🧨 怎么改会红：不按模式筛（那会把别的高难度局算成轮抽战绩）");
        if (outs.Count == 3)
        {
            Eq(outs[0], (bool?)true, "① 第 1 条 = 胜（**新→旧**：记录表 `Insert(0, …)`）");
            Eq(outs[1], (bool?)false, "① 第 2 条 = 负");
            Eq(outs[2], (bool?)null, "① 第 3 条 = 平 ⇒ `null`（原版平局是**第三格** `numberOfMatchesDraw +0x50`）");
        }
        var rs = new DraftRunSave { matchesWon = 0, matchesLost = 0 };
        int applied = DraftModeWindow.ApplyOutcomes(rs, outs);
        Eq(applied, 2, "① `ApplyOutcomes` 只动了 2 场（平局不算）");
        Eq(rs.matchesWon, 1, "① 胜场 +1");
        Eq(rs.matchesLost, 1, "① 败场 +1（🧨 怎么改会红：把平局也算成负）");

        // ---- ② 先把 4 轮抽完（= 本批 (C) 的核心判据：组卡完成 ⇒ `OngoingRun(15)`，⛔ 不是 `Finished(20)`）----
        win.BeginRun(TestSeed);
        if (GoToPacks(win) == null) return;
        int guard = 0;
        while (win.State == DraftModeState.SelectPack && guard++ < 16)
        {
            if (win.OfferedPacks.Count == 0) { Ok(false, "② 第 " + (win.CurrentDraftRound + 1) + " 轮没包"); return; }
            win.PickPack(win.OfferedPacks[0].key);
        }
        Eq(win.CurrentDraftRound, DraftPacks.RoundsPerDraft, "② 选满 `RoundsPerDraft` 包");
        Eq(win.State, DraftModeState.OngoingRun,
           "② **4 轮（组卡）打完 ⇒ `OngoingRun(15)`，⛔ 不是 `Finished(20)`**"
         + "（原版 `UpdateState`：`selectedPacks.Count < draftRounds ? SelectPack(10) : OngoingRun(15)`）"
         + "🧨 怎么改会红：把「组卡完成」直接当收尾（那样玩家一次都没打就进收尾页）");
        Ok(win.State != DraftModeState.Finished, "② 反证：此刻**不在** Finished 页");

        // ---- ③ 喂 2 场胜 ⇒ 战绩 / 屏幕 / 格数三处一起动 ----
        // 把游标钉到 0：本组喂的是**参数版日志**（与全局 `BattleLogData` 无关）；
        // 真实的那条「基线」由 `BeginRun` / `ResumeRun` 走生产路径（见最后一组）。
        DraftModeWindow.RunForTest.consumedDraftMatches = 0;
        var wins2 = new List<BattleLogData.Match>
        {
            LogRow(BattleLogData.Outcome.Victory, d),
            LogRow(BattleLogData.Outcome.Victory, d),
        };
        Eq(win.Victories, 0, "③ 喂之前胜场 = 0（新一局的基线：⛔ 不把上一次的记录算进来）");
        Eq(win.IngestBattleResults(wins2), 2, "③ 吃进 2 场（`IngestBattleResults(log)` 返回吃了几个）");
        Eq(win.Victories, 2, "③ `Victories` = **2**（原版 `save.numberOfMatchesWon +0x48`）");
        Eq(win.State, DraftModeState.OngoingRun, "③ 2 胜 0 负 ⇒ 还停在进行中页（`lost < maxTries`）");
        Eq(win.VictoriesText, "2", "③ Ongoing 页 `Victories text Number` 印 **2**"
          + "（原版 `DraftRunDisplay.Initialize` 第一句 `victoryCounterText.text = save+0x48`）"
          + "🧨 怎么改会红：把它写回常量 `\"0\"`（就是本批要修的那个「胜负场恒 0」）");
        Eq(win.FilledWinMarks, 2, "③ 胜场格**亮了 2 格**（原版 `SetupStageMarks` 的 `i < current`）");
        Eq(win.FilledLossMarks, 0, "③ 败场格一格没亮");

        // ---- ④ 幂等：同一份日志再吃一次 ⇒ 不吃（游标） ----
        Eq(win.IngestBattleResults(wins2), 0, "④ 同一份日志**再吃一次** = 0 场（`consumedDraftMatches` 游标）"
          + "🧨 怎么改会红：不记游标（那每次回窗都白送一场胜）");
        Eq(win.Victories, 2, "④ 胜场仍是 2");

        // ---- ⑤ 输了不算胜；输满 `MaxTries` ⇒ 收尾（原版 `UpdateState` 的 `lost >= maxTries` 那一条）----
        var mixed = new List<BattleLogData.Match>
        {
            LogRow(BattleLogData.Outcome.Defeat, d),      // 新的一条
            LogRow(BattleLogData.Outcome.Victory, d),     // 旧的（已经吃过）
            LogRow(BattleLogData.Outcome.Victory, d),
        };
        Eq(win.IngestBattleResults(mixed), 1, "⑤ 只吃新的那 1 场（列表整体换了，靠条数差认新旧）");
        Eq(win.Defeats, 1, "⑤ 败场 = 1");
        Eq(win.Victories, 2, "⑤ 胜场还是 2（⛔ 没被重算）");
        Eq(win.State, DraftModeState.OngoingRun, "⑤ 1 负 ⇒ 还能打");
        Eq(win.FilledLossMarks, 1, "⑤ 败场格亮了 1 格");

        var threeLosses = new List<BattleLogData.Match>
        {
            LogRow(BattleLogData.Outcome.Defeat, d),
            LogRow(BattleLogData.Outcome.Defeat, d),
            LogRow(BattleLogData.Outcome.Defeat, d),
            LogRow(BattleLogData.Outcome.Defeat, d),
            LogRow(BattleLogData.Outcome.Defeat, d),      // 游标已到 3 ⇒ 只吃前 2 条，正好补到 3 负
        };
        Eq(win.IngestBattleResults(threeLosses), 2, "⑤ 只吃了 2 场（第 3 条起是「已经吃过」的旧记录）");
        Eq(win.Defeats, 3, "⑤ 输满 **3** 场（`DraftModeWindow.MaxTries`）");
        Eq(win.State, DraftModeState.Finished,
           "⑤ **输满 `maxTries` ⇒ `Finished(20)`**（原版 `UpdateState` 的 `lost >= data.maxTries ⇒ 收尾`）"
         + "🧨 怎么改会红：`UpdateState` 不读败场（那输多少都停在进行中页）");
        Ok(win.RewardPanelNode != null && win.RewardPanelNode.gameObject.activeSelf,
           "⑤ `Finished` ⇒ `Reward Info Panel` **显示**（S3 §2.7：`DraftState == 20` 那一支）");
        Ok(win.ToBattleNode != null && !win.ToBattleNode.gameObject.activeSelf,
           "⑤ `Finished` ⇒ `To Battle Button` **藏起来**（S3 §2.7 同一支）");
        Eq(win.FilledLossMarks, 3, "⑤ 败场格亮满 3 格");
        Ok(!string.IsNullOrEmpty(win.QuoteText), "⑤ `Quote End` 出了一句结束语（按胜场档取，见 `QuoteTermForWins`）");

        // ---- ⑥ 没有 `matches.Length` ⇒ **不设胜场上限**（如实标，⛔ 别自己编一个） ----
        win.BeginRun(TestSeed);
        if (GoToPacks(win) == null) return;
        guard = 0;
        while (win.State == DraftModeState.SelectPack && guard++ < 16)
        {
            if (win.OfferedPacks.Count == 0) { Ok(false, "⑥ 第 " + (win.CurrentDraftRound + 1) + " 轮没包"); return; }
            win.PickPack(win.OfferedPacks[0].key);
        }
        DraftModeWindow.RunForTest.consumedDraftMatches = 0;
        var manyWins = new List<BattleLogData.Match>();
        for (int i = 0; i < 10; i++) manyWins.Add(LogRow(BattleLogData.Outcome.Victory, d));
        win.IngestBattleResults(manyWins);
        Eq(win.Victories, 10, "⑥ 吃进 10 场胜（原版这格就是计数器，没有上限）");
        Eq(win.State, DraftModeState.OngoingRun,
           "⑥ **10 胜 0 负仍是 `OngoingRun`** —— 原版那条「`won >= data.matches.Length` 就收尾」的 "
         + "`matches` 是**服务端值、本地 0 命中**（S3 §五）⇒ 我们**不设胜场上限**"
         + "🧨 怎么改会红：自己编一个「赢够 N 场收尾」（那会提前掐掉玩家的一局）");
        Ok(win.FilledWinMarks >= 10,
           "⑥ 胜场格**至少亮 10 格**（格子数 = `max(RoundsPerDraft, 胜场)`；⛔ 不许把多出来的胜场藏起来）"
         + "（实得 " + win.FilledWinMarks + "）");
    }

    // ==================================================================
    //  (C) `Finished` 的触发者
    // ==================================================================
    static void FinishedGroup(DraftModeWindow win)
    {
        Debug.Log(P + "--- (C) `Finished` 的触发者（原版 `UpdateState` / `FinishRun`）---");

        // ---- ① `FinishRun()` = 显式那一支（`..._FinishRun_g__Callback_0.c`：set_DraftState(0x14) → UpdateState）----
        win.BeginRun(TestSeed);
        if (GoToPacks(win) == null) return;
        Ok(DraftModeWindow.RunForTest != null, "① 有一局存档（`RunForTest`）");
        win.FinishRun();
        Eq(win.State, DraftModeState.Finished, "① `FinishRun()` ⇒ `Finished(20)`");
        Eq(DraftModeWindow.RunForTest != null ? DraftModeWindow.RunForTest.joinState : -1, (int)DraftJoinState.Finished,
           "① 顺手把 `joinState` 钉成 `Finished(2)`（原版 `set_DraftState` 那半边）");

        // ---- ② `rewardsCollected` 一开 ⇒ 同一处落 `Collected(22)` 并关窗（原版 `cmove edx, 0x14 / 0x16`）----
        DraftModeWindow.RunForTest.rewardsCollected = true;
        win.UpdateState();
        Eq(win.State, DraftModeState.Collected,
           "② `rewardsCollected = true` 后 `UpdateState()` ⇒ `Collected(22)`"
         + "（原版 `state = rewardsCollected ? 22 : 20`）🧨 怎么改会红：领奖后还停在 20（那能无限点领奖）");
        Ok(!win.gameObject.activeSelf, "② `Collected` ⇒ 窗被关掉（原版那一档没有独立页）");

        // ---- ③ 生产那条路 `CollectReward()`：发不了奖（奖励表在服务端）**但**要把这一局清掉 ----
        win.BeginRun(TestSeed);       // 回到活的一局（窗对象还活着，只是 inactive —— 批处理里不需要真的开窗）
        // ★ `CollectReward` 的**合法前提**是「此刻已经在 Finished」（生产上 `Collect Button` 只在 Finished 页建，
        //   `DraftModeWindow.cs` 的 `var btn = MenuDraw.Node(info, "Collect Button", CollectR)` 建钮、
        //   `CollectReward` 的 doc 写着「只有 Finished 有」）⇒ 必须先把这一局造到 `Finished(20)`。
        //   不插这句的话新局的 `joinState = NotJoined`、`matchesLost = 0` ⇒ `UpdateState` 落到 `SelectWarlord`，
        //   断言实测值就是 `SelectWarlord(5)`（2026-10-24 第七十三会话 E12 修：夹具前提没造到）。
        //   ⚠️ 走 `FinishRun()`（生产入口）而不是直写 `joinState = 2` —— 直写就绕过了被测的那一跳。
        win.FinishRun();              // ⇒ joinState = Finished(2) ⇒ Finished(20)
        win.CollectReward();
        Eq(win.State, DraftModeState.Collected, "③ `CollectReward()` ⇒ `Collected`");
        Ok(DraftModeWindow.RunForTest == null,
           "③ 收尾后**静态存档被清**（原版 `ResetCollectedDraft`）"
         + "🧨 怎么改会红：留着那一局（下次开窗会接着一局已领奖的收尾页，玩家再也开不了新局）");

        // ---- ④ `UpdateState` 的两条「没战将」分支 = 原版那两行 ----
        win.BeginRun(TestSeed);
        DraftModeWindow.RunForTest.joinState = (int)DraftJoinState.Joined;
        DraftModeWindow.RunForTest.payState = (int)DraftPayState.NotPaid;
        win.UpdateState();
        Eq(win.State, DraftModeState.Intro,
           "④ `joinState == Joined && payState == notPaid(0)` ⇒ `Intro(0)`"
         + "（原版 `UpdateState` ②：`cmove` 到 0；`GetTabFromState(0)` 落 Pay 页）");
        // ★ 这条**必须在此刻**查：下一句把 `payState` 改成 free 再 `UpdateState()` 就会推到 `SelectWarlord`，
        //   而 `GoToState` 每次都 `SetOn(_pagePay, page == _pagePay)` ⇒ 那时 Pay 页已经被关掉，再查恒 False。
        //   （2026-10-24 第七十三会话 E12 修：原断言写在 `SelectWarlord` 之后 = 查错时机 ⇒ 假红。）
        Ok(win.PageNode(DraftModeState.Intro) != null
           && win.PageNode(DraftModeState.Intro) == win.PageNode(DraftModeState.PayScreen)
           && win.PageNode(DraftModeState.PayScreen).gameObject.activeSelf,
           "④ `Intro` 与 `PayScreen` **同页**（S3 §〇·3），且此刻**显示着**");
        DraftModeWindow.RunForTest.payState = (int)DraftPayState.Free;
        win.UpdateState();
        Eq(win.State, DraftModeState.SelectWarlord,
           "④ `payState == free(1)` 且还没选战将 ⇒ `SelectWarlord(5)`");

        // ---- ⑤ 选将之后按「已选包数」分流（原版 ③ 那一行）----
        if (GoToPacks(win) == null) return;
        Eq(win.State, DraftModeState.SelectPack, "⑤ 选了战将、0 包 ⇒ `SelectPack(10)`");
        win.PickPack(win.OfferedPacks[0].key);
        Eq(win.State, DraftModeState.SelectPack, "⑤ 1 包（< `draftRounds`）⇒ 还是 `SelectPack(10)`");
        Eq(win.StageCounterText, "2/" + DraftPacks.RoundsPerDraft, "⑤ `Stage Counter` 跟着到 2/4");
    }

    // ==================================================================
    //  (B-续) 跨窗续打：关窗（= 进对局）之后回壳再开，战绩与进度还在
    // ==================================================================
    static void ResumeGroup(WindowsManager wm)
    {
        Debug.Log(P + "--- (B-续) 关窗再开 ⇒ 接着上一局（原版进度在服务端）---");

        // 前一组的窗可能已经被 `Collected` 关掉了，而且它带着别的状态 ⇒ 干脆起一扇**新的**（钉种子 = 新局）
        var old = DraftModeWindow.LastOpened;
        if (old != null) { try { UnityEngine.Object.DestroyImmediate(old.gameObject); } catch { } }
        var win = DraftModeWindow.Create(wm, TestSeed);
        if (win == null) { Ok(false, "前提：建得出第一扇窗"); return; }
        win.BeginRun(TestSeed);
        if (GoToPacks(win) == null) return;
        win.PickPack(win.OfferedPacks[0].key);
        // ⚠️ 这里**直接写存档那一格**，不往全局 `BattleLogData` 里塞假记录（自检不动玩家的真表）。
        //    「吃日志 ⇒ 计数」那条链已由上一组 ②/③ 用**参数版**逐条验过（同一个 `ApplyOutcomes`）。
        DraftModeWindow.RunForTest.matchesWon = 1;
        int winsBefore = win.Victories, packsBefore = win.CurrentDraftRound;
        string wlBefore = win.WarlordId;
        Eq(winsBefore, 1, "① 记下现场：1 胜 / " + packsBefore + " 包");

        // 「进对局」= 窗被销毁（切场景）。这里显式销毁它，再用**生产那条路**（`Create(wm)` 不带种子）重建。
        UnityEngine.Object.DestroyImmediate(win.gameObject);
        var win2 = DraftModeWindow.Create(wm);            // seed = 0 ⇒ 走 `Open()` 的 resume 分支
        Ok(win2 != null, "② 重开一扇窗（`Create(wm)` ⇒ `Open()` 走 `ResumeRun`）");
        if (win2 == null) return;
        Eq(win2.WarlordId, wlBefore, "② 战将还在");
        Eq(win2.CurrentDraftRound, packsBefore, "② 已选包数还在（⛔ 没被 `BeginRun` 重置）"
          + "🧨 怎么改会红：`Open()` 无条件 `BeginRun`（那就是本批要修的「关一次窗进度全丢」）");
        Eq(win2.Victories, winsBefore, "② 胜场还在");
        Ok(win2.State == DraftModeState.SelectPack || win2.State == DraftModeState.OngoingRun,
           "② 落在「接着打」的那一页（实得 " + win2.State + "）");
        UnityEngine.Object.DestroyImmediate(win2.gameObject);

        // ---- ③ 「还没报名」那一档：原版 `Open` 落页看的是**存着的** `DraftState`（`GetStartingTab`），
        //      不是 `UpdateState` 现算 ⇒ `joinState = NotJoined` 必须回 **Pay 页**（不是选将页）----
        var win3 = DraftModeWindow.Create(wm, TestSeed);          // 钉种子 ⇒ 新局（NotJoined）
        Ok(win3 != null, "③ 前提：建得出第三扇窗");
        if (win3 == null) return;
        win3.BeginRun(TestSeed);
        UnityEngine.Object.DestroyImmediate(win3.gameObject);     // 「关窗」
        var win4 = DraftModeWindow.Create(wm);                    // 生产路 ⇒ `ResumeRun`
        Ok(win4 != null, "③ 再开一扇（`ResumeRun`）");
        if (win4 == null) return;
        Eq(win4.State, DraftModeState.PayScreen,
           "③ `joinState = NotJoined` 的那一局 ⇒ 回 **Pay 页**（原版 `GetStartingTab` → `GetTabFromState(DraftState)`）"
         + "🧨 怎么改会红：拿 `UpdateState` 现算落页（它会给 `SelectWarlord` —— 玩家会跳过报名页）");
        UnityEngine.Object.DestroyImmediate(win4.gameObject);
    }

    // ==================================================================
    //  ★ U1（2026-10-24）：选将页的**版面**与**真命中**（`Draft Mode Select Warlord State`）
    // ------------------------------------------------------------------
    //  🔴 期望值来源 = **prefab 现读**（⛔ 不是抄 `DraftModeWindow` 的常量）：
    //     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
    //      "Generic Select Card Container" --depth 5 --relative`
    //   · 选将格 `Generic Select Card Container`：布局 412.75×617.69 · `m_LocalScale 1.221`
    //     ⇒ 视觉 **503.97×754.20**；同层 `Spacing` 宽 80 ⇒ 步距 **492.75**；首格布局中心 x = **206.375**
    //     ⇒ 第 i 格**视觉框**左 = i×492.75 − 45.61，格视觉框顶 y = 554.42 − 377.10 = **177.32**
    //     （554.42 = 视口顶 101.90 + 锚点偏移 452.52，S3 §2.4）。
    //   · 卡体 `2DCard`：**视觉** 386.60×615.42 @ 格视觉框左上角 +(13.1, −19.6)
    //     （那一行的 `⇲ls=184.7` = `CardUI 151.3 × 容器 1.221`，**已经含**容器缩放
    //      ⇒ S3 §2.4 写的「386.6×615.42 **× 1.221** = 472×751.4」是**重复乘**；2026-10-24 订正）
    //     ⇒ 卡心 = (i×492.75 + **160.79**, **465.42**)。
    //   · `Select Button`：视觉 210.02×81.66 @ 格视觉框左上角 +(101.4, 585.5)→(311.4, 667.1)
    //     ⇒ 第 i 格命中区 x = i×492.75 + **55.79 … 265.79** · y = **762.82 … 844.42**。
    //  🧨 改坏法（每条都对应一次真缺陷）：
    //     · 卡高改回 751.4 / 卡按格心居中 ⇒ ② 红（真机症状：`选择` 按钮压在战将数值行上，四张卡挤成一排）；
    //     · 命中区没建出来 / 建到格心 ⇒ ③④ 红（`HitBoxForTest` 直接 false / 点是点得中但落到别处）；
    //     · 命中区队列降到 ≤ 吸收层 ⇒ ④ 红（真机 symptom = 「点在按钮上，命中的是 `AbsorbHit`」）；
    //     · `Continue` 出厂可点 ⇒ ⑤ 红（原版 `OnOpen` 末句 `interactable = false`）；
    //     · 卡体不建吸收区（或吸收区档 ≥ `QHit`）⇒ ⑥ 红（点卡体穿透到窗内吸收层 / 反过来抢走按钮）。
    // ==================================================================
    static void WarlordPageGroup(DraftModeWindow win)
    {
        Debug.Log(P + "--- (U1) 选将页：版面 + 真命中（原版 `Draft Mode Select Warlord State`）---");

        win.BeginRun(TestSeed);
        win.ClickFree();
        if (!Ok(win.State == DraftModeState.SelectWarlord, "① 前提：`Free` ⇒ 选将页（实得 " + win.State + "）")) return;
        int n = win.Warlords != null ? win.Warlords.Count : 0;
        if (!Ok(n > 0, "① 前提：这一局有可选战将（实得 " + n + "）")) return;
        // 第 5 格在视口外（格视觉框右沿 = 4×492.75+458.36 = 2429.4 > 1920）⇒ 只验**屏上这 4 格**；
        // 「滚出去的那一格点不到」是原版 `RectMask2D` 的射线语义（`MenuDraw.Hit` 的 `clip`），另有一批断言在管。
        int vis = Mathf.Min(4, n);

        // ---- ② 卡体（`2DCard`）的**渲染**矩形：从 `CardView` 自己量（`BaseScale × Width/Height × 108` + 世界位置）----
        for (int i = 0; i < vis; i++)
        {
            PxRect r;
            if (!Ok(win.WarlordCardRectRenderedForTest(i, out r), "② 第 " + (i + 1) + " 格的卡量得到（`CardView` 在）")) continue;
            Near(r.H, 615.42f, 0.6f, "② 第 " + (i + 1) + " 格 **卡体视觉高 = 615.42**（原版 `2DCard`；"
                                   + "🧨 751.4 = 把容器那 1.221 乘了两次）");
            Near(r.W, 386.60f, 0.6f, "② 第 " + (i + 1) + " 格 **卡体视觉宽 = 386.60**（原版 `2DCard`）");
            Near(r.CX, i * 492.75f + 160.79f, 0.6f, "② 第 " + (i + 1) + " 格卡心 x = 格视觉框左 + 206.4"
                                                  + "（原版卡在格子里**偏左**：卡心 = 布局框中心）");
            Near(r.CY, 465.42f, 0.6f, "② 第 " + (i + 1) + " 格卡心 y = **465.42**（原版 `2DCard` 顶部 157.72 + 615.42/2；"
                                    + "🧨 旧写法按格心居中 = 554.42 ⇒ 卡比原版低 89.0px）");
        }

        // ---- ③ `Select Button` 的**命中区**（`PointerLayer.HitBoxForTest` = 生产那一份 `HitBoxPx`）----
        for (int i = 0; i < vis; i++)
        {
            var b = win.WarlordSelectButtonForTest(i);
            Vector2 c = Vector2.zero, h = Vector2.zero; ImageQuad q = null;
            if (!Ok(b != null && PointerLayer.HitBoxForTest(b, out c, out h, out q),
                    "③ 第 " + (i + 1) + " 格 `Select Button` 的命中区**量得到**"
                  + "（`null` = 命中区没建出来 / 没有 `ImageQuad` —— 那正是「真鼠标点不动」，`WindowButton.Click()` 直调看不出来）"))
                continue;
            Near(c.x - h.x, i * 492.75f + 55.79f, 0.6f, "③ 第 " + (i + 1) + " 格命中区左 = 格视觉框左 + 101.4");
            Near(c.x + h.x, i * 492.75f + 265.79f, 0.6f, "③ 第 " + (i + 1) + " 格命中区右 = 格视觉框左 + 311.4");
            Near(c.y - h.y, 762.82f, 0.6f, "③ 第 " + (i + 1) + " 格命中区顶 = 格视觉框顶 + 585.5");
            Near(c.y + h.y, 844.42f, 0.6f, "③ 第 " + (i + 1) + " 格命中区底 = 格视觉框顶 + 667.1（= 原版那颗 `Select Button` 的框）");
        }

        // ---- ④ 真命中：按 `PointerLayer` 那条链（同一个 `AllButtons` + `HitBoxForTest` + **渲染队列高的赢**）
        //      算「点这一点，吃到的是谁」，⛔ 不是直调 `Click()` ----
        int absorbQ = QuadQueue(win.transform.Find("General Red Background/AbsorbHit"));
        Ok(absorbQ != int.MinValue, "④ 前提：窗内吸收层 `AbsorbHit` 在、量得到队列（实得 " + absorbQ + "）");
        for (int i = 0; i < vis; i++)
        {
            float px = i * 492.75f + 160.79f;          // 按钮中心 x（55.79…265.79 的中点）
            float py = (762.82f + 844.42f) * 0.5f;     // 按钮中心 y = 803.62
            int wq; string who;
            var winner = HitTestInWindow(win.transform, px, py, out wq, out who);
            Ok(winner != null && winner == win.WarlordSelectButtonForTest(i),
               "④ 点第 " + (i + 1) + " 格按钮中心 (" + px.ToString("F1") + "," + py.ToString("F1") + ") "
             + "⇒ 吃到的是**这颗 `Select Button`**（实得 `" + who + "`；"
             + "🧨 真机症状：落到 `AbsorbHit` = 用户报的「按钮点不到」）");
            Ok(wq > absorbQ, "④ 而且它的队列 " + wq + " **严格高于**窗内吸收层 `AbsorbHit` 的 " + absorbQ
                           + "（同档时 `ImageQuad` 世界 z 恒 0 ⇒ 谁吃到退化成枚举顺序）");
        }

        // ---- ⑤ `Continue`：出厂灰（原版 `OnOpen` 末句），选中之后才可点（`SelectWarlord` 末句）----
        var cont = win.WarlordContinueButtonForTest;
        if (!Ok(cont != null, "⑤ `Continue` 的 `WindowButton` 在")) return;
        Ok(!cont.Interactable, "⑤ 还没选战将 ⇒ `Continue.interactable = false`（原版 `OnOpen`）");
        if (WindowButton.GrayShaderAvailable)
            Ok(cont.GrayedForTest, "⑤ 而且它**真的灰了**（`Everguild/UI/Greyscale`；原版 `DoStateTransition(Disabled)` 那一支）");

        // 真点一下：走**上面那套命中表**选出来的那颗（= 生产会派发的那一颗），再验三处可见反馈
        int dq; string dwho;
        var clickable = HitTestInWindow(win.transform, 160.79f, (762.82f + 844.42f) * 0.5f, out dq, out dwho);
        if (Ok(clickable != null, "⑤ 前提：第 1 格按钮中心算得出命中件（实得 `" + dwho + "`）"))
            clickable.Click();
        string first = win.Warlords[0].Id;
        Eq(win.WarlordId, first, "⑤ 点一下 ⇒ **真的选中**第 1 位战将 `" + first + "`");
        Ok(cont.Interactable, "⑤ 选中之后 `Continue` 可点（原版 `SelectWarlord` 末句）");
        var cv0 = win.WarlordCardViewForTest(0);
        Ok(cv0 != null && cv0.State == CardHighlightState.Selected,
           "⑤ 选中那张卡**高亮成 `Selected`**（原版 `ToggleSelected` → `Card2DController.ToggleHighlight(true)`）");
        var sb0 = win.WarlordSelectButtonForTest(0);
        Ok(sb0 != null && !sb0.Interactable,
           "⑤ 而且**那一格按钮自己灰掉**（原版 `ToggleSelected` 的 `selectButton.interactable = !selected`）");
        var cv1 = win.WarlordCardViewForTest(1 < n ? 1 : 0);
        Ok(cv1 == null || cv1.State != CardHighlightState.Selected || n < 2,
           "⑤ 没选的那几张**不高亮**（⛔ 别把整排都点亮）");

        // ---- ⑥ 卡体那一下 = 原版 `2DCard/UI Collider`（`UIGenericEventCatcher + NonDrawingGraphic`，无处理器）
        //      ⇒ **吞掉、什么都不做**；⛔ 不是以前那样**穿透到窗内吸收层 `AbsorbHit`**
        //      （真机 click_log 2026-10-24 #37/#39 的观感就是「点了卡，结果命中的是窗户的吸收层」）----
        {
            float px = 160.79f, py = 300f;      // 第 1 格卡体（卡体 y 157.72…773.14，取上半部分，避开 `Select Button`）
            int cq; string cwho;
            var cb = HitTestInWindow(win.transform, px, py, out cq, out cwho);
            Ok(cb != null && cb.name == "CardBodyHit" && MenuDraw.WasAbsorb(cb.transform),
               "⑥ 点卡体 ⇒ 吃到的是**这张卡自己的吸收区**（原版 `2DCard/UI Collider`；实得 `" + cwho + "`）");
            string before = win.WarlordId;
            if (cb != null) cb.Click();
            Eq(win.WarlordId, before, "⑥ 而且它**什么都不做**（原版那张 `UI Collider` 上没有处理器：不选、不换页、不关窗）");
        }

        // 收尾：把这一组留下的选中态清掉（后面的组各自 `BeginRun`，这里只是不让「本组最后的状态」飘到别处）
        win.BeginRun(TestSeed);
    }

    /// <summary>`PointerLayer` 那条命中链的**同口径**复刻（自检用）：候选 = 场上所有 `WindowButton`
    /// （生产 `CollectHits` 用的是同一个 `AllButtons`）· 命中矩形 = `HitBoxForTest`（生产 `HitBoxPx` 的
    /// **唯一实现**，⛔ 不在这里重算）· 排序 = 生产那个比较器的三条键**逐条同序**：
    /// **渲染队列↓ → `z` 小的先 → 后兄弟先**（第三条见 <see cref="AboveInHierarchy"/>）。
    /// ⚠️ 只收**本窗**（`win` 子树）里的钮，并且不过 `PointerReachable`（自检只开这一扇窗）。
    /// </summary>
    static WindowButton HitTestInWindow(Transform win, float px, float py, out int q, out string who)
    {
        WindowButton best = null; ImageQuad bestQ0 = null;
        int bestQ = int.MinValue; float bestZ = float.MaxValue;
        string bestWho = "空点（本窗没有钮盖住这一点）";
        var all = PointerLayer.AllButtonsForTest();
        for (int i = 0; i < all.Length; i++)
        {
            var b = all[i];
            if (b == null || b.transform == null || win == null || !b.transform.IsChildOf(win)) continue;
            Vector2 c, h; ImageQuad quad;
            if (!PointerLayer.HitBoxForTest(b, out c, out h, out quad)) continue;
            if (Mathf.Abs(px - c.x) > h.x || Mathf.Abs(py - c.y) > h.y) continue;
            int qq = quad != null ? quad.RenderQueue : int.MinValue;
            float z = quad != null ? quad.transform.position.z : 0f;
            bool take;
            if (best == null) take = true;
            else if (qq != bestQ) take = qq > bestQ;
            else if (z < bestZ - 1e-4f) take = true;
            else if (z > bestZ + 1e-4f) take = false;
            else take = AboveInHierarchy(quad != null ? quad.transform : null, bestQ0 != null ? bestQ0.transform : null);
            if (!take) continue;
            best = b; bestQ0 = quad; bestQ = qq; bestZ = z;
            bestWho = b.name + "（队列 " + qq + "，框 (" + (c.x - h.x).ToString("F1") + ","
                    + (c.y - h.y).ToString("F1") + ")-(" + (c.x + h.x).ToString("F1") + ","
                    + (c.y + h.y).ToString("F1") + ")）";
        }
        q = bestQ; who = bestWho; return best;
    }

    /// <summary>同队列同 `z` 时的平局判据 = **后兄弟先**。
    /// 🔴 口径**照抄** `PointerLayer.IsAboveInHierarchy`（它 `private static`、本文件转发不到）：
    /// 从根向下逐级比 `GetSiblingIndex()`，同一级下标相同 ⇒ 同一个父 ⇒ 继续往下；
    /// 一条链是另一条的前缀（祖先 vs 后代）⇒ **后代赢**（子件画在父件之后）。
    /// ⚠️ **两份同一条规则**（生产一份、本文件一份）：改生产那份时这份要跟着改 —— 见 CLAUDE.md §三。
    /// </summary>
    static bool AboveInHierarchy(Transform a, Transform b)
    {
        if (a == b || a == null) return false;
        if (b == null) return true;
        var pa = new List<Transform>(); var pb = new List<Transform>();
        for (var t = a; t != null; t = t.parent) pa.Insert(0, t);
        for (var t = b; t != null; t = t.parent) pb.Insert(0, t);
        int n = Mathf.Min(pa.Count, pb.Count);
        for (int i = 0; i < n; i++)
        {
            if (pa[i] == pb[i]) continue;
            return pa[i].GetSiblingIndex() > pb[i].GetSiblingIndex();
        }
        return pa.Count > pb.Count;      // 前缀 ⇒ 后代赢
    }

    /// <summary>一个节点子树里那颗 `ImageQuad` 的渲染队列（量不到 ⇒ `int.MinValue`）。</summary>
    static int QuadQueue(Transform t)
    {
        if (t == null) return int.MinValue;
        var q = t.GetComponentInChildren<ImageQuad>();
        return q != null ? q.RenderQueue : int.MinValue;
    }
}
