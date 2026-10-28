// ProfileDataAssertE13.cs — 执行代理 `E13`（2026-10-24）的断言宿主
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景、不进 `Editor/BattleScene.cs`）。
// 调用方式（主对话在宿主收尾处加两行，想并进总数就带后一行）：
//
//     ProfileDataAssertE13.Run();
//     pass += ProfileDataAssertE13.Pass; fail += ProfileDataAssertE13.Fail;
//
// ==================================================================
//  三件覆盖的内容（判据逐条写在每条断言的消息里）
// ------------------------------------------------------------------
//  A. **成就进度真写点**（`BattleDriver.AwardBattleAchievements`）：
//     · 按**原版 challenge 原文**找到对应的 `Ach_N`（数据 = `Resources/profile_cosmetics.json` 资产真数）；
//     · 打督军伤害 / 拿骷髅 / 涨信仰 ⇒ 各自那条成就**累加**（不是覆盖）；
//     · 0 值不写；**单机局不算「Multiplayer」那两条**（灭自证：那条闸真的在）；
//     · 真联机排位局才 +1（播放数 / 胜场）。
//  B. **对局历史落盘 / 重载往返**：写 → 清内存 → 从盘读回 ⇒ 条数、字段、录像名一致；
//     超过 `ReplayStore.MaxKept`（50）从最旧删。
//  C. **旧录像可见（兜底行）**：本表空、盘上有录像 ⇒ `BattleLogData.DisplayRows()` 仍列得出
//     且 `ReplayStore.Load()` 读得回来（= 行上那颗回放钮走的就是这一条）；同名的真记录不会重复列。
//
//  D. 🆕 **成就跨档提示**（2026-10-28 `B89-E16` 追加，账 `个人资料_成就_排行:29`）：
//     跨一档 ⇒ `AchievementMilestoneToast` 弹一条；文案 = 官方格式串 + 称号 + 档；
//     **勋章图 = `defaultTierIcon[tier-1]`**（档 1..5 ↔ `medal1..5`，证据 → `AchievementsManager__GetIconByTier.c`
//     + `assetripper_out/…/Intro.unity:7141`）；通道 = `ToastNotificationController`（⛔ 不是 `MessageToast`/错误横幅）。
//
// ⚠️ **不碰玩家真档**：B/C 都把 `OverrideDir` 指到临时目录；A 把 `ProfileData.SavePathOverrideForTest`
//    指到临时文件（虽然批处理本来就不落盘 —— 双保险）。
// 🔴 **删档类调用的顺序（2026-10-24 热修 `E11`）**：`ReplayStore.ResetForTest()` 是**删文件**的 ——
//    调它的时候 `OverrideDir` **必须还指着临时目录**，调完才还原 `OverrideDir`（见 `finally` 里那段注）。
//    ⛔ 顺序反了 = 删掉玩家的真录像（10-24 事故，证据 → `资料/普查产出_1024c/RE1_战斗回放完整审查.md` B1）。
// ==================================================================
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using CardPresentation.Net;      // `MsgStart`（录像头）
using UnityEngine;

public static class ProfileDataAssertE13
{
    const string P = "[E13] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进宿主的合计数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        string tmp = null;
        try
        {
            tmp = Path.Combine(Path.GetTempPath(), "wf_e13_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);

            // A 节：成就写点（临时存档路径 —— 批处理本来就不写盘，这里是双保险）
            ProfileData.SavePathOverrideForTest = Path.Combine(tmp, "profile.json");
            RunAchievementWritePoints();

            // D 节：成就**跨档提示**（B89-E16 追加；在同一份临时存档上跑，阈值真跨得过）
            RunAchievementMilestoneToast();

            // B 节：对局历史落盘
            RunBattleLogPersistence(Path.Combine(tmp, "battlelog"));

            // C 节：录像兜底
            RunReplayFallback(Path.Combine(tmp, "battlelog_empty"), Path.Combine(tmp, "replays"));
        }
        catch (System.Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            // ---------------- 收尾：把改写口全部还回去、删临时目录 ----------------
            // 🔴 **顺序照 `Editor/NetSelfTest.cs:925-929`**：两个 `ResetForTest()` 必须在
            //    **`OverrideDir` 还指着临时目录**的时候调，**然后**才还原 `OverrideDir`。
            //    ⛔ 顺序反了 = 删掉玩家的真录像 —— 2026-10-24 就是这么丢的：原来这里是
            //    `ReplayStore.OverrideDir = null;` 在前、`ReplayStore.ResetForTest();` 在后 ⇒
            //    `ReplayStore.Dir` 落回 `persistentDataPath/WarpforgeReplays` ⇒ 整目录被清空（而且当时一声不响）。
            //    证据与善后 → `资料/普查产出_1024c/RE1_战斗回放完整审查.md` B1；
            //    `ReplayStore.ResetForTest` 现在另有「没设 `OverrideDir` 就拒绝」的硬挡兜底（B2）。
            //    ⚠️ `BattleLogData.ResetForTest()` 是「清内存 + 从盘重读」，同样要**还在临时目录上**时读 ——
            //       否则读进来的是玩家的真对战记录（不是删，但自检不该碰它；`BattleLogData.StoreOn` 那道门只管写）。
            if (!string.IsNullOrEmpty(ReplayStore.OverrideDir)) ReplayStore.ResetForTest();
            if (!string.IsNullOrEmpty(BattleLogData.OverrideDir)) BattleLogData.ResetForTest();
            ReplayStore.OverrideDir = null;
            BattleLogData.OverrideDir = null;
            ProfileData.SavePathOverrideForTest = null;
            try { if (tmp != null && Directory.Exists(tmp)) Directory.Delete(tmp, true); }
            catch (System.Exception e) { Debug.LogWarning(P + "临时目录没删干净（不影响断言）：" + e.Message); }
        }
        Debug.Log(P + $"=== E13 合计：{Pass} 通过 / {Fail} 失败 ===");
    }

    // ==================================================================
    //  断言小工具（与 `E4` / `E7` 同形：本类自己记一份 Pass/Fail）
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

    static ProfileData.Achievement AchById(string id)
    {
        var all = ProfileData.Achievements;
        for (int i = 0; i < all.Count; i++) if (all[i].Id == id) return all[i];
        return null;
    }

    // ==================================================================
    //  A · 成就写点
    // ==================================================================
    static void RunAchievementWritePoints()
    {
        Debug.Log(P + "--- A · 成就进度真写点 ---");

        // ---- A0 键绑对了没有（期望值来自**资产真数**，不是我们自己的常量）----
        if (!Ok(ProfileData.Loaded && ProfileData.Achievements.Count > 0, "A0（前提）成就清单读进来了（profile_cosmetics.json）"))
            return;

        Eq(ProfileData.FindIdByChallenge(ProfileData.ChallengeDamageToWarlord), "Ach_1",
           "A0 `Damage To Warlord` 绑到 Ach_1");
        Eq(ProfileData.FindIdByChallenge(ProfileData.ChallengeCollectSkulls), "Ach_19",
           "A0 `Collect Skulls` 绑到 Ach_19");
        Eq(ProfileData.FindIdByChallenge(ProfileData.ChallengeGainFaith), "Ach_63",
           "A0 `Gain Faith` 绑到 Ach_63");
        Eq(ProfileData.FindIdByChallenge(ProfileData.ChallengeRankedPlayed), "Ach_37",
           "A0 `Play Ranked Multiplayer Matches` 绑到 Ach_37");
        Eq(ProfileData.FindIdByChallenge(ProfileData.ChallengeRankedWon), "Ach_38",
           "A0 `Win Ranked Multiplayer Matches` 绑到 Ach_38");

        var ach1 = AchById("Ach_1");
        Ok(ach1 != null && ach1.Thresholds != null && ach1.Thresholds.Length == 5 && ach1.Thresholds[0] == 1000,
           "A0 Ach_1 的阈值 = 资产真数（5 档、首档 1000）—— 证明绑到的是「Slay the Warlord」那条，不是同名别的");

        // ---- A1 打督军伤害 → Ach_1 累加 ----
        int b1 = ProfileData.AchievementValue("Ach_1");
        int b19 = ProfileData.AchievementValue("Ach_19");
        int b63 = ProfileData.AchievementValue("Ach_63");
        // 🆕 2026-10-25（`X2`）：形参换新版（模式 + 阵营），`rankedNetMatch` 那个布尔**已删**（判据 §F2）。
        //   这里用 `OfflinePractice`（**非**排位/轮抽）⇒ 只验「督军伤害 / 骷髅 / 信仰」这三条。
        BattleDriver.AwardBattleAchievements(new RuleEngine.AchievementTally(), 0,
                                             damageToEnemyWarlord: 120, skullsCollected: 3,
                                             faithGained: 7, mode: RuleEngine.GameMode.OfflinePractice,
                                             faction: "Ultramarines", won: true, replay: false);
        Eq(ProfileData.AchievementValue("Ach_1"), b1 + 120, "A1 打督军 120 伤害 ⇒ `Damage To Warlord` +120");
        Eq(ProfileData.AchievementValue("Ach_19"), b19 + 3, "A1 拿了 3 颗骷髅 ⇒ `Collect Skulls` +3");
        Eq(ProfileData.AchievementValue("Ach_63"), b63 + 7, "A1 涨了 7 点信仰 ⇒ `Gain Faith` +7");

        // ---- A2 第二次结算 = **累加**（灭自证：写成覆盖的话这条会红）----
        BattleDriver.AwardBattleAchievements(new RuleEngine.AchievementTally(), 0, 30, 1, 2,
                                             RuleEngine.GameMode.OfflinePractice, "Ultramarines", false, false);
        Eq(ProfileData.AchievementValue("Ach_1"), b1 + 150, "A2 再打 30 ⇒ **累加**成 +150（⛔ 不是覆盖成 30）");
        Eq(ProfileData.AchievementValue("Ach_19"), b19 + 4, "A2 再拿 1 颗 ⇒ 累计 +4");
        Eq(ProfileData.AchievementValue("Ach_63"), b63 + 9, "A2 再涨 2 点信仰 ⇒ 累计 +9");

        // ---- A3 0 值不写（也不落盘）----
        int c1 = ProfileData.AchievementValue("Ach_1");
        BattleDriver.AwardBattleAchievements(new RuleEngine.AchievementTally(), 0, 0, 0, 0,
                                             RuleEngine.GameMode.OfflinePractice, "Ultramarines", false, false);
        Eq(ProfileData.AchievementValue("Ach_1"), c1, "A3 全 0 的一局 ⇒ 进度一动不动");

        // ---- A4 🔴 **2026-10-25（`X2`）口径变更（判据 §F2，主对话裁断）** ----
        //    原版 `GamesPlayed__OnTrigger.c` 判的是**模式**（`IRankedEvent` / `IDraftEvent` 的运行时类型），
        //    **没有「对手是不是真人」这一条** ⇒ 单机排位（`Classic`）也算。
        //    （改前 E13 这里断的是「单机局**不**涨」—— 那条口径已按原版推翻，见判据 §F2。）
        int r37 = ProfileData.AchievementValue("Ach_37");
        int r38 = ProfileData.AchievementValue("Ach_38");
        BattleDriver.AwardBattleAchievements(new RuleEngine.AchievementTally(), 0, 0, 0, 0,
                                             RuleEngine.GameMode.Classic, "Ultramarines", true, false);
        Eq(ProfileData.AchievementValue("Ach_37"), r37 + 1, "A4 单机**排位**（`Classic`）⇒ `Play Ranked…` +1（★ 原版判模式、不判对手是不是人）");
        Eq(ProfileData.AchievementValue("Ach_38"), r38 + 1, "A4 那一局赢了 ⇒ `Win Ranked…` +1");

        // ---- A5 ★ 灭自证：**非**排位/轮抽的模式 ⇒ 两条都不涨（证明涨的不是「什么局都加」）----
        BattleDriver.AwardBattleAchievements(new RuleEngine.AchievementTally(), 0, 0, 0, 0,
                                             RuleEngine.GameMode.OfflinePractice, "Ultramarines", true, false);
        Eq(ProfileData.AchievementValue("Ach_37"), r37 + 1, "A5 ★ `OfflinePractice` ⇒ `Play Ranked…` **不动**");
        Eq(ProfileData.AchievementValue("Ach_38"), r38 + 1, "A5 ★ 同上 ⇒ `Win Ranked…` **不动**");

        // ---- A5b 输了 ⇒ `Win Ranked…` 不动（灭自证：不是「玩了就加」）----
        BattleDriver.AwardBattleAchievements(new RuleEngine.AchievementTally(), 0, 0, 0, 0,
                                             RuleEngine.GameMode.Classic, "Ultramarines", false, false);
        Eq(ProfileData.AchievementValue("Ach_37"), r37 + 2, "A5b 再打一局排位（输了）⇒ `Play Ranked…` 仍 +1");
        Eq(ProfileData.AchievementValue("Ach_38"), r38 + 1, "A5b 输了 ⇒ `Win Ranked…` 不动（灭自证）");

        // ---- A6 写进去的值在「档位 / 积分」那一侧算得出来（接口贯通）----
        int tier = ProfileData.AchievementTierOf(ach1);
        Ok(tier >= 1 && tier <= 5, "A6 写点之后 `AchievementTierOf` 落在 1..5（实得 " + tier + "）");
    }

    // ==================================================================
    //  B · 对局历史落盘 / 重载往返
    // ==================================================================
    static BattleLogData.Match Mk(string hero, string foe, BattleLogData.Outcome r, string replayFile)
    {
        return new BattleLogData.Match
        {
            Result = r, OwnHeroName = hero, EnemyHeroName = foe,
            OwnName = "E13 Me", EnemyName = "E13 Foe",
            OwnSkulls = 2, EnemySkulls = 1, Mode = "OfflinePractice",
            ReplayFile = replayFile ?? "",
        };
    }

    static void RunBattleLogPersistence(string dir)
    {
        Debug.Log(P + "--- B · 对局历史落盘 / 重载往返 ---");

        BattleLogData.OverrideDir = dir;
        BattleLogData.DeleteStoreForTest();          // 先清掉上一趟残留（必须已设 OverrideDir，否则它拒绝执行）
        BattleLogData.ReloadForTest();
        Eq(BattleLogData.Count, 0, "B0（前提）临时目录里原来是空的");

        BattleLogData.Add(Mk("E13 Hero A", "E13 Hero B", BattleLogData.Outcome.Victory, ""));
        BattleLogData.Add(Mk("E13 Hero C", "E13 Hero D", BattleLogData.Outcome.Defeat, ""));
        BattleLogData.Add(Mk("E13 Hero E", "E13 Hero F", BattleLogData.Outcome.Draw, "20260101_000000_E13HeroE_vs_E13HeroF.json"));
        Eq(BattleLogData.Count, 3, "B1 写进 3 条");
        Ok(File.Exists(BattleLogData.StorePath), "B1 落盘文件真的出现了（" + BattleLogData.StorePath + "）");

        // 🔴 **清内存再读盘** —— 不清的话下面那条「读回 3 条」可能只是内存里那 3 条（自证）
        BattleLogData.ResetForTest();
        Eq(BattleLogData.Count, 0, "B2 清空内存后条数 = 0（⇒ 下面读回的值只可能来自盘）");
        BattleLogData.ReloadForTest();
        Eq(BattleLogData.Count, 3, "B2 从盘读回 3 条");
        if (BattleLogData.Count == 3)
        {
            Eq(BattleLogData.All[0].OwnHeroName, "E13 Hero E", "B2 读回的顺序不变（新的在前，第 0 条 = 最后写的那一局）");
            Eq(BattleLogData.All[0].Result, BattleLogData.Outcome.Draw, "B2 结果（枚举）往返一致");
            Eq(BattleLogData.All[0].ReplayFile, "20260101_000000_E13HeroE_vs_E13HeroF.json", "B2 录像文件名往返一致");
            Eq(BattleLogData.All[0].OwnSkulls, 2, "B2 骷髅数往返一致");
            Eq(BattleLogData.All[0].Mode, "OfflinePractice", "B2 模式字符串往返一致");
        }

        // ---- B3 「留 50 局」口径（与 ReplayStore.MaxKept 同一格数，⛔ 不另立数）----
        for (int i = 0; i < ReplayStore.MaxKept; i++)
            BattleLogData.Add(Mk("E13 Bulk " + i, "E13 Foe", BattleLogData.Outcome.Victory, ""));
        Eq(BattleLogData.Count, ReplayStore.MaxKept, "B3 超过上限 ⇒ 只留 " + ReplayStore.MaxKept + " 条（从最旧的删）");
        if (BattleLogData.Count > 0)
            Eq(BattleLogData.All[0].OwnHeroName, "E13 Bulk " + (ReplayStore.MaxKept - 1), "B3 留下的是最新那一批（第 0 条 = 最后写的）");
        BattleLogData.ReloadForTest();
        Eq(BattleLogData.Count, ReplayStore.MaxKept, "B3 修剪结果**也落了盘**（重读还是 " + ReplayStore.MaxKept + " 条）");
    }

    // ==================================================================
    //  C · 旧录像可见（历史项丢了，录像文件还在）
    // ==================================================================
    static void RunReplayFallback(string emptyLogDir, string replayDir)
    {
        Debug.Log(P + "--- C · 盘上录像兜底行 ---");

        // ---- C1 造一份真录像（走正式写口）----
        ReplayStore.OverrideDir = replayDir;     // 🔴 **先指到临时目录**（下一行是删文件的；⛔ 顺序反了 = 删真录像）
        ReplayStore.ResetForTest();
        var rec = new ReplayRecord
        {
            savedAt = "2026-10-24 00:00:00", mySeat = 0, result = 0,
            myHero = "E13 Hero A", foeHero = "E13 Hero B",
            start = new MsgStart { mode = "Classic", myName = "E13 Me", foeName = "E13 Foe", arena = "Battle" },
        };
        string fn = ReplayStore.Save(rec);
        Ok(!string.IsNullOrEmpty(fn) && File.Exists(Path.Combine(replayDir, fn)), "C1 录像文件存下来了（" + fn + "）");
        Eq(ReplayStore.List().Count, 1, "C1 `ReplayStore.List()` 列得出这一份");
        var load = ReplayStore.Load(fn);
        Ok(load != null, "C1 `ReplayStore.Load()` 读得回来（= 行上那颗回放钮走的就是这一条）");

        // ---- C2 本表**空**（模拟「重启后历史项丢失」）----
        BattleLogData.OverrideDir = emptyLogDir;
        BattleLogData.DeleteStoreForTest();
        BattleLogData.ReloadForTest();
        Eq(BattleLogData.Count, 0, "C2（前提）本表是空的");

        var rows = BattleLogData.DisplayRows();
        Eq(rows.Count, 1, "C2 本表空、盘上有 1 份录像 ⇒ 页面那份仍列出 1 行");
        if (rows.Count == 1)
        {
            Eq(rows[0].ReplayFile, fn, "C2 兜底行的 `ReplayFile` = 盘上那份文件名（点回放就是放它）");
            Ok(rows[0].FromDisk, "C2 那一行标着 `FromDisk`（⇒ 不是真打出来的记录）");
            Eq(rows[0].OwnHeroName, "E13 Hero A", "C2 督军名从录像头读出来");
            Eq(rows[0].EnemyHeroName, "E13 Hero B", "C2 对手督军名从录像头读出来");
            Eq(rows[0].Result, BattleLogData.Outcome.Victory, "C2 结果从录像头读出来（result=0 ⇒ 胜）");
            Eq(rows[0].Mode, "Classic", "C2 模式从录像头读出来");
        }

        // ---- C3 本表里有同名记录 ⇒ **不重复列**（灭自证：去重是活的）----
        BattleLogData.Add(Mk("E13 Hero A", "E13 Hero B", BattleLogData.Outcome.Victory, fn));
        var rows2 = BattleLogData.DisplayRows();
        int same = 0; bool fromDisk = false;
        for (int i = 0; i < rows2.Count; i++)
            if (rows2[i].ReplayFile == fn) { same++; fromDisk = rows2[i].FromDisk; }
        Eq(same, 1, "C3 本表已有这一份录像 ⇒ 只列 1 行（不重复）");
        Ok(!fromDisk, "C3 剩下的那一行是**本表记录**（不是兜底行）");
    }

    // ==================================================================
    //  D · 成就**跨档提示**（原版 `Achievement.OnReachMilestone` 的 ⑤）
    //     期望值全部来自**资产真数 / 官方词条 / 反编译口径**，⛔ 不回读 `AchievementMilestoneToast` 的常量。
    // ==================================================================
    static void RunAchievementMilestoneToast()
    {
        Debug.Log(P + "--- D · 成就跨档提示（原版 OnReachMilestone ⑤）---");

        var a = AchById("Ach_1");
        if (!Ok(a != null && a.Thresholds != null && a.Thresholds.Length >= 2,
                "D0（前提）Ach_1 至少 2 档阈值（否则跨不满一档，下面的断言会静默空转）"))
            return;

        int cur = ProfileData.AchievementValue("Ach_1");
        int thr1 = a.Thresholds[0], thr2 = a.Thresholds[1];
        if (!Ok(cur < thr1, $"D0（前提）Ach_1 当前进度 {cur} **还没到首档** {thr1}（否则本节「跨一档」不成立）"))
            return;

        // 词条是**数据**（官方表），不是我们的常量 —— 缺了就先红，别让文案那条断言测到「空串档」
        string title = Loc.T("Achievements/Title/Ach_1");
        if (!Ok(!string.IsNullOrEmpty(title) && title != "Achievements/Title/Ach_1",
                "D0（前提）`Achievements/Title/Ach_1` 在官方词条表里（否则文案那条测的是称号取空那一档）"))
            return;
        string fmt = Loc.T("Achievements/NewAchievementMessage");

        AchievementMilestoneToast.ResetForTest();
        ToastNotifications.ClearLive();

        // ---- D1 跨第 1 档：弹 1 条 · 文案 = 官方格式串 · 勋章 = medal1 ----
        ProfileData.AddAchievementValue("Ach_1", thr1 - cur);
        Eq(ProfileData.AchievementValue("Ach_1"), thr1, $"D1 进度正好顶到首档阈值（资产真数 {thr1}）");
        Eq(AchievementMilestoneToast.FiredCount, 1, "D1 跨 1 档 ⇒ 弹 1 条（原版「每命中一个 milestone 调一次」）");
        Eq(AchievementMilestoneToast.LastText, string.Format(fmt, title + " 1"),
           "D1 文案 = `Format(I2(NewAchievementMessage), I2(称号) + \" \" + 1)`（原版 ②③④ 逐句）");
        Eq(AchievementMilestoneToast.LastIconArt, "40k_Achievements_icon_medal1",
           "D1 勋章 = `defaultTierIcon[0]` = medal1（`AchievementsManager__GetIconByTier.c` + Intro.unity:7141 的 5 条 PPtr）");

        // ---- D2 跨第 2 档：再多 1 条 · 勋章换 medal2（**档↔图**那条映射真的在走） ----
        ProfileData.AddAchievementValue("Ach_1", thr2 - thr1);
        Eq(AchievementMilestoneToast.FiredCount, 2, "D2 再跨一档 ⇒ 累计 2 条");
        Eq(AchievementMilestoneToast.LastIconArt, "40k_Achievements_icon_medal2", "D2 勋章换到 medal2");

        // ---- D3 真弹到屏上了吗：通道 = ToastNotification（⛔ 不是 MessageToast/错误横幅）----
        var tn = ToastNotifications.Instance;
        if (!Ok(tn != null, "D3（前提）壳里有 `ToastNotificationController`（`ScheduleNotification` 才有落点）"))
            return;
        tn.Step(0.02f);      // 批处理没有帧循环 ⇒ 自己泵一拍，把 `Schedule` 的队列排空
        Eq(tn.LastShownType, ToastNotifications.ToastNotificationType.Achievement,
           "D3 真正走的通道 = `ToastNotification`（原版 `ToastNotificationType.Achievement = 0` → `Toast` 模板）");
        Ok(tn.LiveCount >= 2, $"D3 两条跨档提示都实例化了（实得 {tn.LiveCount} 条活飘窗）");
        // ⚠️ 取**最后**一颗：`Holder` 的子件顺序 = 实例化顺序（先 tier1 后 tier2），`Find("Toast")` 会拿回 tier1 那条。
        var live = tn.Holder != null && tn.Holder.childCount > 0
                 ? tn.Holder.GetChild(tn.Holder.childCount - 1) : null;
        Ok(live != null && (live.name == "Toast" || live.name == "Toast(Clone)"), "D3 飘窗节点名 = `Toast`（原版模板名，⛔ 别改名；🔴 2026-10-28 B89_RD：`Instantiate` 会给克隆体加 `(Clone)` 后缀 —— 本仓既有旁证 `MainMenuRuntime.cs:3394` / `ErrorMessageBanner.cs:70`，所以两种写法都算「没改名」）");
        var iconTr = live != null ? live.Find("Icon") : null;
        var q = iconTr != null ? iconTr.GetComponent<ImageQuad>() : null;
        Eq(q != null && q.Texture != null ? q.Texture.name : null, "40k_Achievements_icon_medal2",
           "D3 最后那条的 `Icon` quad 上贴的**就是** medal2（不是模板默认的 medal1 —— 这条挡「只传了图名、没真换上」）");
        ToastNotifications.ClearLive();
    }
}
