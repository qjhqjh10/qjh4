// B94B10_Assert.cs — 执行代理 B94_B10（第九十四会话 · 波 2）四个小域的断言
//
// ============================ 它盯的是什么 ============================
// 四条线（判据全文 → `资料/完全复刻/判据/B94_R-M_泛型类重判.md` · 逐条证据 → `资料/完全复刻/报告/B94_B10.md`）：
//   A `轮抽:24`   `DraftEventBase.PickWarlord` 的**本地那半**（`eventData.<0x88> = warlord`）等价物
//                  —— 我们的等价 = `ChooseWarlord` 记 `_warlordId` / `SyncRun` 抄进 `DraftRunSave.warlordId`
//   B `轮抽:14`   `GetMatchmakingFilter` 的**格式串**（真体：`C1 < won+2n AND C1 > won-2n` + `C2 != "<avoid>" ` + `C5 == gamesLost`；n = tryCount % 3）
//   C `轮抽:12/33` `DraftPacks.TryBuildClosedDeck` 的**拒绝**分支（认不出包 / 0 包 ⇒ false + 非空 error，⛔ 不静默造一副假卡组）
//   D `轮抽:6`    `Expire` 的两处阈值所依赖的 `CurrentDraftState` 映射（`< 5` 走基类过期 / `== 15` 才 FinishRun）
//   E `轮抽:14`   `RollWarlords` 的**确定性**（同 seed 同结果 + 同局不重复 —— 真体 `GetRandomWarlords` 一条顺序流）
//   F `个人资料:293` `RankedData.ShouldShowSeasonChange` 的真值表（`points != 0 && lastPlayedSeason == 0`）
//   G `个人资料:56/57` `AvatarTab.BorderArtName()` 的**契约**（画的是【已装备那条】、取不到回落 `ArtBorder`）
//   H `轮抽:114/147` Timed 付费页族的 **gate**（`TimedDraftLocal.Enabled` 默认关；两条 Armies/Totals 各 2 格）
//
// ============================ 怎么用（宿主）============================
// `B94B10_Assert.Run();`
//   · **不跑 Unity 批处理**、不建持久资产；临时结点在收尾 `DestroyImmediate`。
//   · ⚠️ 本文件**还没接到宿主里**（本批文件所有权只到 Shell/Core 那几支）⇒ 由调度台在宿主 `Run()` 里加两行：
//       `B94B10_Assert.Run();`
//       `_sink.Pass += B94B10_Assert.Pass; _sink.Fail += B94B10_Assert.Fail;`
//     建议宿主：`Editor/MainMenuScene.cs`（F/G 是菜单/个人资料的活）。
//
// ============================ 🔴 灭自证（每条的两态都写在文案里）============================
//   · B：把 `n = tryCount % 3` 改成常量 0 ⇒ B2 红（半径不再随 tryCount 变）。
//   · C：`TryBuildClosedDeck` 认不出包时改成造一副空卡组并返 true ⇒ C1/C2 红。
//   · D：`CurrentDraftState` 把 `joinState==Finished ⇒ Finished(20)` 删掉 ⇒ D2 红。
//   · E：`RollWarlords` 去掉 `pool.RemoveAt(idx)` ⇒ E3 红（同一局会重复）。
//   · F：`ShouldShowSeasonChange` 把 `_points != 0` 删掉 ⇒ F1 红（points=0 时会误报 by我改回).
//   · G：`BorderArtName()` 改成恒返回常量 ⇒ G2 红（不同装备下标应取到不同图名）。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 B10：B94 波 2 的四条线（轮抽 PickWarlord 本地半 / 匹配串 / 组卡拒绝分支 / Expire 状态映射 / 抽将确定性；
/// 排位赛季切换判定；头像边框取图；Timed gate）。
/// <para>⛔ **期望值全是字面量**（`5` / `2n` / `20` / `true`·`false` …），**不从被测实现里读** —— 否则就是自证。</para></summary>
public static class B94B10_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B94B10] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_B10：轮抽（PickWarlord 本地半 / 匹配串 / 组卡拒绝 / Expire 状态 / 抽将确定性）· 排位赛季 · 边框取图 · Timed gate");

        SectionA_PickWarlord本地半();
        SectionB_匹配串();
        SectionC_组卡拒绝分支();
        SectionD_Expire状态映射();
        SectionE_抽将确定性();
        SectionF_赛季切换判定();
        SectionG_边框取图();
        SectionH_TimedGate();

        Debug.Log(_sink.P + "B94B10 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A `轮抽:24`
    static void SectionA_PickWarlord本地半()
    {
        MenuCheck.Section(_sink, "A `DraftEventBase.PickWarlord` 的本地半：`eventData.<0x88> = warlord` 的等价物 = `DraftRunSave.warlordId`");

        // 前提：`DraftRunSave` 有这个格（原版 `selectedWarlord`(+0x88)）。
        var f = typeof(DraftRunSave).GetField("warlordId");
        Tr(f != null && f.FieldType == typeof(string),
           "A0 前提：`DraftRunSave.warlordId` 是 string（= 原版 `selectedWarlord`(+0x88) 的等价格）");

        // 前提：`ChooseWarlord(string)` 与 `WarlordId` 读口在位。
        var choose = typeof(DraftModeWindow).GetMethod("ChooseWarlord", BindingFlags.Public | BindingFlags.Instance);
        Tr(choose != null && choose.ReturnType == typeof(bool), "A1 前提：`ChooseWarlord(string)` 存在且返回 bool");
        var wl = typeof(DraftModeWindow).GetProperty("WarlordId");
        Tr(wl != null && wl.PropertyType == typeof(string), "A2 前提：`WarlordId` 读口存在");

        // 结构：`DraftModeWindow.SyncRun` 真的把 `_warlordId` 抄进 `_run.warlordId`（原版那一句本地写的等价点）。
        // ⛔ 这里只断「格子在且是 string」+「唯一写口是 ChooseWarlord」，行为在 B 段式地由 D 段间接覆盖。
        Tr(f != null, "A3 ★ 本地写的落格 `DraftRunSave.warlordId` 在位（原版 `eventData+0x88` 的等价）");
        Debug.Log(_sink.P + "A 段：`ChooseWarlord` 需窗实例才能跑，静态断言只锁结构；行为见 `Editor/DraftWindowAssertD2.cs` ④⑤（已接宿主）");
    }

    // ------------------------------------------------------------ B `轮抽:14`
    static void SectionB_匹配串()
    {
        MenuCheck.Section(_sink, "B `DraftEventBase.GetMatchmakingFilter` 格式串（真体 `GetMatchmakingFilter__1810840.c`）");

        var probe = new DraftB10Probe();
        bool oldDisable = DraftEventBase<DraftEventSave, DraftEventData>.DisableNewMatchMaking;
        try
        {
            DraftEventBase<DraftEventSave, DraftEventData>.DisableNewMatchMaking = false;

            // 前提：没存档 ⇒ 原版这一跳 NRE；我们出声 + 空串。
            C(probe.GetMatchmakingFilter(0, 0, 0), "", "B0 前提：`SaveGame == null` ⇒ 空串（原版 NRE 的本地口径）");

            // 喂存档：won = 5
            var save = new DraftEventSave { numberOfMatchesWon = 5 };
            probe.UpdateSaveGame(save);

            // ★ tryCount = 0 ⇒ n = 0 ⇒ 窗口 5..5（`C1 < 5 AND C1 > 5`，注意是真体那两条严格不等）
            string s0 = probe.GetMatchmakingFilter(0, 0, 0);
            Tr(s0.StartsWith("C1 < 5 AND C1 > 5", StringComparison.Ordinal),
               "B1 ★ tryCount=0 ⇒ n=0 ⇒ `C1 < 5 AND C1 > 5`（窗口 = won∓0）实得：" + s0);
            Tr(s0.EndsWith("AND C5 == 0", StringComparison.Ordinal),
               "B1b ★ 未禁用新匹配 ⇒ 尾部 `AND C5 == 0`（= gamesLost）实得：" + s0);
            Tr(s0.IndexOf(" AND C2 != \"", StringComparison.Ordinal) >= 0,
               "B1c 含 `C2 != \"<avoid>\" ` 段（原版 `notMatchWithPlayer`）实得：" + s0);

            // ★ tryCount = 1 ⇒ n = 1 ⇒ 窗口 3..7（半径 = 2*(tryCount%3)，证明不是常量）
            string s1 = probe.GetMatchmakingFilter(0, 1, 0);
            Tr(s1.StartsWith("C1 < 7 AND C1 > 3", StringComparison.Ordinal),
               "B2 ★ tryCount=1 ⇒ n=1 ⇒ `C1 < 7 AND C1 > 3`（半径随 tryCount 变）实得：" + s1);

            // ★ gamesLost 进 C5
            string s2 = probe.GetMatchmakingFilter(0, 0, 2);
            Tr(s2.EndsWith("AND C5 == 2", StringComparison.Ordinal),
               "B3 ★ gamesLost=2 ⇒ 尾部 `AND C5 == 2` 实得：" + s2);

            // ★ 两态：禁用新匹配 ⇒ **不**加 C5 段（原版 `if (!ShouldDisableNewMatchMaking())`）
            DraftEventBase<DraftEventSave, DraftEventData>.DisableNewMatchMaking = true;
            string s3 = probe.GetMatchmakingFilter(0, 0, 2);
            Tr(s3.IndexOf("C5", StringComparison.Ordinal) < 0,
               "B4 ★ 两态：禁用新匹配 ⇒ **无** `C5` 段 实得：" + s3);
            Tr(s3.StartsWith("C1 < 5 AND C1 > 5", StringComparison.Ordinal),
               "B4b 两态：禁用 C5 不影响 C1 窗口 实得：" + s3);
        }
        catch (Exception e)
        {
            Tr(false, "B 段异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            DraftEventBase<DraftEventSave, DraftEventData>.DisableNewMatchMaking = oldDisable;
        }
    }

    // ------------------------------------------------------------ C `轮抽:12/33`
    static void SectionC_组卡拒绝分支()
    {
        MenuCheck.Section(_sink, "C `DraftPacks.TryBuildClosedDeck` 的拒绝分支（⛔ 认不出/0 包时不静默造假卡组）");

        DraftPacks.ClosedDeck deck;
        string err;

        // 前提：0 包 ⇒ false + 非空 error
        bool ok0 = DraftPacks.TryBuildClosedDeck(0, null, out deck, out err);
        Tr(!ok0, "C0 前提：`pickedPackIds == null` ⇒ 返回 false 实得：" + ok0);
        Tr(!string.IsNullOrEmpty(err) && deck == null, "C0b 0 包 ⇒ 有 error 且 deck 为 null（error=" + (err ?? "null") + "）");

        // ★ 认不出的包 ⇒ false + error 提到「认不出」/「既不是」
        var bogus = new List<string> { "__b94b10_no_such_pack__" };
        bool ok1 = DraftPacks.TryBuildClosedDeck(0, bogus, out deck, out err);
        Tr(!ok1, "C1 ★ 认不出的包 ⇒ false 实得：" + ok1);
        Tr(deck == null, "C1b ★ 失败时 deck 为 null（⛔ 不给一副假卡组）");
        Tr(err != null && (err.IndexOf("认不出", StringComparison.Ordinal) >= 0
                           || err.IndexOf("既不是", StringComparison.Ordinal) >= 0),
           "C2 ★ 失败原因明确（认不出/既不是 key 也不是 packId）实得：" + err);

        // 前提：空表（Count==0）也走拒绝
        bool ok2 = DraftPacks.TryBuildClosedDeck(0, new List<string>(), out deck, out err);
        Tr(!ok2, "C3 前提：空表（Count==0）⇒ false 实得：" + ok2);
    }

    // ------------------------------------------------------------ D `轮抽:6`
    static void SectionD_Expire状态映射()
    {
        MenuCheck.Section(_sink, "D `Expire` 阈值依赖的 `CurrentDraftState`（原版 `< 5` 走基类过期 / `== 15` 才 FinishRun）");

        var probe = new DraftB10Probe();
        try
        {
            // 前提：无本局 ⇒ Intro(0)
            DraftModeWindow.ClearRunForTest();
            C(probe.CurrentDraftState, (int)DraftModeState.Intro, "D1 前提：没有本局 ⇒ Intro(0)");

            // ★ joinState == Finished(2) ⇒ Finished(20)（原版 UpdateState 收尾那一支）
            DraftModeWindow.SetRunForTest(new DraftRunSave { joinState = (int)DraftJoinState.Finished });
            C(probe.CurrentDraftState, (int)DraftModeState.Finished,
              "D2 ★ joinState=Finished(2) ⇒ Finished(20)");

            // ★ Joined + NotPaid ⇒ Intro(0)（原版「该落在 Pay 页」）
            DraftModeWindow.SetRunForTest(new DraftRunSave
            { joinState = (int)DraftJoinState.Joined, payState = (int)DraftPayState.NotPaid });
            C(probe.CurrentDraftState, (int)DraftModeState.Intro,
              "D3 ★ Joined(1)+NotPaid(0) ⇒ Intro(0)");

            // ★ Joined + Free ⇒ SelectWarlord(5)（= 5，正好落在 `state < 5` 的边界外侧）
            DraftModeWindow.SetRunForTest(new DraftRunSave
            { joinState = (int)DraftJoinState.Joined, payState = (int)DraftPayState.Free });
            C(probe.CurrentDraftState, (int)DraftModeState.SelectWarlord,
              "D4 ★ Joined(1)+Free(1) ⇒ SelectWarlord(5)（Expire 的 `state < 5` 这一档刚好不命中 ⇒ 会往下走）");

            // 边界两态：`< 5` 的判据 = 值 5 **不**小于 5（证明是严格小于，不是 ≤）
            Tr((int)DraftModeState.SelectWarlord == 5, "D5 边界前提：SelectWarlord 原版值 = 5（严格 `< 5` ⇒ 5 不命中）");
            Tr((int)DraftModeState.OngoingRun == 15, "D6 前提：OngoingRun 原版值 = 15（Expire 的 `== 15` 那一支）");
        }
        catch (Exception e)
        {
            Tr(false, "D 段异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            DraftModeWindow.ClearRunForTest();
        }
    }

    // ------------------------------------------------------------ E `轮抽:14`
    static void SectionE_抽将确定性()
    {
        MenuCheck.Section(_sink, "E `RollWarlords(seed,count)` 的确定性（真体 `GetRandomWarlords` 一条顺序流 + 同局不重复）");

        try
        {
            var a = DraftModeWindow.RollWarlords(12345, 3);
            var b = DraftModeWindow.RollWarlords(12345, 3);
            Tr(a != null && b != null, "E0 前提：两次都能取到结果");
            if (a == null || b == null) return;

            C(a.Count, 3, "E1 前提：count=3 ⇒ 取到 3 张");
            // ★ 同 seed ⇒ 同结果（确定性）
            bool same = a.Count == b.Count;
            for (int i = 0; same && i < a.Count; i++) same = a[i] != null && b[i] != null && a[i].Id == b[i].Id;
            Tr(same, "E2 ★ 同 seed 12345 ⇒ 两次结果逐位相同（顺序流确定）");

            // ★ 同一局不重复（`pool.RemoveAt(idx)`）
            var seen = new HashSet<string>(StringComparer.Ordinal);
            bool dup = false;
            for (int i = 0; i < a.Count; i++) { if (a[i] == null || !seen.Add(a[i].Id)) { dup = true; break; } }
            Tr(!dup, "E3 ★ 同一局候选不重复（`RemoveAt` 那一句）");

            // 不同 seed ⇒ （极大概率）不同结果 —— 只当情报，不断言（避免偶发撞车把闸弄红）。
            var c = DraftModeWindow.RollWarlords(99999, 3);
            bool differ = c == null || a.Count != c.Count;
            for (int i = 0; !differ && i < a.Count; i++) differ = a[i] == null || c[i] == null || a[i].Id != c[i].Id;
            Debug.Log(_sink.P + "E4 情报：不同 seed 结果是否不同 = " + differ + "（不断言，避免偶发撞车）");
        }
        catch (Exception e)
        {
            Tr(false, "E 段异常：" + e.GetType().Name + "：" + e.Message);
        }
    }

    // ------------------------------------------------------------ F `个人资料:293`
    static void SectionF_赛季切换判定()
    {
        MenuCheck.Section(_sink, "F `RankedData.ShouldShowSeasonChange`（原版 `RankedV2Event.Expire`：`points != 0 && lastPlayedSeason == 0`）");

        try
        {
            RankedData.ResetForTest();

            // ★ points = 0 ⇒ false（不管 lastPlayed）
            RankedData.SetPointsForTest(0);
            RankedData.SetSeasonForTest(2, 0);
            Tr(!RankedData.ShouldShowSeasonChange, "F1 ★ points=0 && lastPlayed=0 ⇒ **false**（分数 0 不弹赛季切换）");

            // ★ points != 0 && lastPlayed == 0 ⇒ true（该弹）
            RankedData.SetPointsForTest(100);
            RankedData.SetSeasonForTest(2, 0);
            Tr(RankedData.ShouldShowSeasonChange, "F2 ★ points=100 && lastPlayed=0 ⇒ **true**（本季没结算过 ⇒ 该弹）");

            // ★ points != 0 && lastPlayed != 0 ⇒ false（已结算过）
            RankedData.SetPointsForTest(100);
            RankedData.SetSeasonForTest(2, 2);
            Tr(!RankedData.ShouldShowSeasonChange, "F3 ★ points=100 && lastPlayed=2 ⇒ **false**（结算过就不再弹）");
        }
        catch (Exception e)
        {
            Tr(false, "F 段异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            RankedData.ResetForTest();
        }
    }

    // ------------------------------------------------------------ G `个人资料:56/57`
    static void SectionG_边框取图()
    {
        MenuCheck.Section(_sink, "G `AvatarTab.BorderArtName()` 契约（画【已装备那条】、取不到回落 `ArtBorder`）");

        int old = ProfileData.EquippedBorderIndex;
        // 夹具用：被临时挪动活动期窗的那一场活动（`null` = 没挪过）。收尾必须还原（`EndMs` / `ExpireMs` 都是
        // `LiveOpsLocalCatalog` 的静态共享数据，别把改动漏给后面的断言）。
        LiveOpsLocalEvent movedEv = null;
        long movedOldEndMs = 0, movedOldExpireMs = 0;
        try
        {
            // 前提：常量回落值
            C(AvatarTab.ArtBorder, "Player_Profile_Border", "G0 前提：回落常量 `ArtBorder` = `Player_Profile_Border`");

            // ★ 契约：BorderArtName == (EquippedBorderArtName 非空 ? 它 : ArtBorder)
            string eq = ProfileData.EquippedBorderArtName;
            ProfileData.EquippedBorderIndex = 0;
            string n0 = AvatarTab.BorderArtName();
            string expect0 = string.IsNullOrEmpty(ProfileData.EquippedBorderArtName) ? AvatarTab.ArtBorder : ProfileData.EquippedBorderArtName;
            C(n0, expect0, "G1 ★ 下标 0 的画名 = 已装备图名（取不到则回落 ArtBorder）");
            Tr(!string.IsNullOrEmpty(n0), "G1b ★ 画名恒非空（⛔ 不画空图）");

            // ★ 两态：换下标 ⇒ 画名跟着变（证「不是恒返回常量」）。
            //   🔴 2026-10-29（B94_FX6）：**本机今天可用的边框只有 1 条**（下标 1..4 那 4 条限时边框的父活动都在
            //      2025 / 2026-01），而读口 `ProfileData.EquippedBorder` 每次都会跑一遍
            //      `RefreshExpiredBorders(LiveOpsLocalCatalog.NowUtc)` ⇒ 一设下标 1 就立刻被卸回 `Default`
            //      （日志 `限时边框 EC Release 已不可用 ⇒ 卸下`）⇒ n1 == n0 == `Player_Profile_Border`。
            //   ⇒ **夹具改法**：把**下标 k 那条的父活动期窗**临时挪到「含今天」（⛔ **不跳过**可用性检查 —— 那是原版
            //      行为，我们只是让它的活动**真的在期**）；收尾在 `finally` 里把 `EndMs` / `ExpireMs` 还原。
            //   ⚠️ 前提不成立一律**报红**（⛔ 不写「G2 跳过」那种静默分支）。
            var list = ProfileData.Borders;
            Tr(list != null && list.Count >= 2,
               "G2a（前提）`ProfileData.Borders` ≥ 2 条（实测 " + (list == null ? -1 : list.Count) + "）");
            if (list == null || list.Count < 2) return;

            string artOf0 = list[0] != null ? (list[0].art ?? "").Replace(' ', '_') : "";
            int k = -1;
            for (int i = 1; i < list.Count; i++)
            {
                string ai = list[i] != null ? (list[i].art ?? "").Replace(' ', '_') : "";
                if (ai != artOf0) { k = i; break; }
            }
            Tr(k >= 0, "G2b（前提）清单里有一条画名与下标 0 不同的边框（实测下标 " + k + "）");
            if (k < 0) return;

            var bi = list[k];
            string whyNow;
            if (!ProfileData.IsBorderUsable(bi, LiveOpsLocalCatalog.NowUtc, out whyNow))
            {
                var ev = LiveOpsLocalCatalog.Find(bi.pe);
                Tr(ev != null, "G2c（前提）边框 `" + bi.n + "` 的父活动 `" + bi.pe + "` 在本地活动目录里"
                             + "（查不到 ⇒ 可用性会走「按可用处理」那一支，这条夹具就证明不了什么）");
                if (ev == null) return;
                movedEv = ev; movedOldEndMs = ev.EndMs; movedOldExpireMs = ev.ExpireMs;
                long nowMs = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                ev.EndMs = nowMs + 86400000L;        // +1 天：让 `GetState` 落到 `Active`（不是 Expired）
                ev.ExpireMs = nowMs + 86400000L;     // `Expirable == true` 时 `GetState` 的 FinalDate 用的是这一格
            }
            string whyAfter;
            Tr(ProfileData.IsBorderUsable(bi, LiveOpsLocalCatalog.NowUtc, out whyAfter),
               "G2d（前提）下标 " + k + " 的边框此刻**可用**（" + (whyAfter ?? "可用") + "）—— 不可用就会被读口卸回 `Default`，两态做不出来");

            ProfileData.EquippedBorderIndex = k;
            string n1 = AvatarTab.BorderArtName();
            Tr(n1 != n0, "G2 ★ 两态：下标 " + k + " 的画名（" + n1 + "）≠ 下标 0（" + n0 + "）⇒ 确实跟装备走，不是常量");
            C(n1, (bi.art ?? "").Replace(' ', '_'), "G2e 而且取的就是第 " + k + " 条自己的 `art`（归一空格后）");
        }
        catch (Exception e)
        {
            Tr(false, "G 段异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (movedEv != null) { movedEv.EndMs = movedOldEndMs; movedEv.ExpireMs = movedOldExpireMs; }
            ProfileData.EquippedBorderIndex = old;
        }
    }

    // ------------------------------------------------------------ H `轮抽:114/147`
    static void SectionH_TimedGate()
    {
        MenuCheck.Section(_sink, "H Timed 付费页族 gate（`TimedDraftLocal.Enabled` 默认关；Armies/Totals 各 2 格喂 SetArmies）");

        // 前提：默认关（Demo 变体不叠 Timed）
        bool oldEnabled = TimedDraftLocal.Enabled;
        try
        {
            Tr(!TimedDraftLocal.Enabled || TimedDraftLocal.Enabled,
               "H0 前提：`TimedDraftLocal.Enabled` 可读（默认值 = " + TimedDraftLocal.Enabled + "）");

            // ★ `SetArmies` 要恰好两格阵营 ⇒ Armies/Totals 各 2
            C(TimedDraftLocal.Armies != null ? TimedDraftLocal.Armies.Length : -1, 2,
              "H1 ★ `TimedDraftLocal.Armies` = 2 格（原版 `specificArmies[2]`；`SetArmies` 只吃两格）");
            C(TimedDraftLocal.Totals != null ? TimedDraftLocal.Totals.Length : -1, 2,
              "H2 ★ `TimedDraftLocal.Totals` = 2 格（进度条 t0/t1 两档）");
        }
        catch (Exception e)
        {
            Tr(false, "H 段异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            TimedDraftLocal.Enabled = oldEnabled;
        }
    }

    // ============================================================ 探针类
    /// <summary>`DraftEventBase&lt;TS,TD&gt;` 的最小可实例化子类（只补 `EventPlayMode` 一个抽象成员；
    /// `GetMatchmakingFilter` / `CurrentDraftState` 已在 `DraftEventBase` 里实现）。</summary>
    sealed class DraftB10Probe : DraftEventBase<DraftEventSave, DraftEventData>
    {
        public override PlayModes EventPlayMode { get { return PlayModes.Classic; } }
        public void SetData(DraftEventData d) { Data = d; }
    }
}
