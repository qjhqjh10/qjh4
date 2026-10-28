// B93S4Assert.cs — 执行代理 **B94-S4**（第九十四会话 · 波 1 · 其它小域）的断言
//
// ============================ 它盯的是什么 ============================
// `判据/B94_派活.tsv` 里 `写手组 == "S4_其它小域"` 那 35 条中**能落到可判行为的**那些。
// 逐条的判据、做法、没落地的部分 → `资料/完全复刻/报告/B94_S4.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
// `B93S4Assert.Run();`
//   · **不跑 Unity 批处理**，不写玩家存档（唯一动 PlayerPrefs 的 B 节自己备份/还原）。
//   · 建议宿主（调度台统一接线，本文件不接）：
//       - **`Editor/MainMenuScene.cs`**（顶栏资源计数 / 教程完成谓词 / 退出链 / 活动钮 都在这条线上）
//       - 也可放 `Editor/ProfileDataAssertE13` 那一族所在的宿主（战将精通 / 头像边框层）
//     接线两行：`B93S4Assert.Run();  _sink.Pass += B93S4Assert.Pass; _sink.Fail += B93S4Assert.Fail;`
//
// ============================ 🔴 灭自证（每条都写了「改哪一处会让它红」）============================
//   · A 组 = 原版字面量（0.5s / 110·120 / "UM47" / "Player_Profile_Border"）。改常量 ⇒ 立刻红。
//   · B 组 = 文案两态（`max > 0` 带 `/max`，`max <= 0` **不带**）。把判据从 `> 0` 改成 `!= 0` ⇒ B1 红。
//   · C 组 = 三个谓词的两态（全做 / 缺一位）。把 `AllTutorialsCompleted` 改成 `done > 0` ⇒ C4 红；
//            把 `FirstTwoTutorialCompleted` 改成 `||` ⇒ C6 红。
//   · D 组 = 原版那条 **NRE**（照抄）。把它改回 `return null` ⇒ D1 红（这就是灭自证的两态）。
//   · E 组 = 战将精通的 ②③④ 三句。把「日志优先」改成「一律 HighestCardId」⇒ E3 红；
//            把 `Math.Max(total,1)` 去掉 ⇒ E2 红；把 `UM47` 兜底删掉 ⇒ E5 红。
//   · F 组 = `ArmyProgressBar.SetArmies` 的两态（不画 / 画两张）。把 `Neutral ⇒ 不画` 删掉 ⇒ F3 红。
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 B94-S4：其它小域（轮抽/菜单/个人资料/社交/教程/锻造/基础设施/泛型）35 条的断言。
/// <para>⛔ **期望值全是字面量或原版实读值**（0.5f / 110 / 120 / "UM47" …），**不从被测实现里读**
/// —— 否则就是「拿我们的实现证明我们的实现」。</para></summary>
public static class B93S4Assert
{
    static readonly CheckSink _sink = new CheckSink("[B94S4] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94-S4 其它小域：计数文案 / 教程谓词 / 排位接口 NRE / 战将精通 / 阵营进度条 / 头像边框");

        SectionA_判据字面量();
        SectionB_资源计数文案();
        SectionC_教程完成谓词();
        SectionD_排位接口支的NRE();
        SectionE_战将精通纯逻辑();
        SectionF_阵营进度条();
        SectionG_头像边框层();

        Debug.Log(_sink.P + "B94-S4 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A 判据字面量（⛔ 硬检查，先跑）
    static void SectionA_判据字面量()
    {
        MenuCheck.Section(_sink, "A 判据字面量（原版实读值 —— 被改掉就要红）");

        // `UIResourceCounter.SetQuantity` 的 DOTween 时长（`报告/D_未分类_第1轮.md:105` 实读 0.5 s）
        C(TopBar.CounterAnimTime, 0.5f, "A1 原版 `UIResourceCounter.SetQuantity` 的补间时长 = **0.5 s**");

        // `BattleManager__IsClosedDeckGame.c:1-10`：matchType ∈ {0x6e, 0x78}
        C((int)MatchType.ClosedDeck, 110, "A2 原版 `MatchType.ClosedDeck` = **0x6e = 110**");
        C((int)MatchType.ClosedDeckBot, 120, "A3 原版 `MatchType.ClosedDeckBot` = **0x78 = 120**");

        // `WarlordMasteryContainer__OnInitialize.c:85` 的字面量兜底
        C(ProfileTab.WarlordMasteryFallbackCardId, "UM47",
          "A4 原版 `AssetLocator.GetAsset<RawCardScript>(\"UM47\")` 的**字面量兜底** = `UM47`");

        // `AvatarTab.ArtBorder`（`AvatarBorderController.Initialize` 取不到图时的回落常量）
        C(AvatarTab.ArtBorder, "Player_Profile_Border", "A5 头像边框层的回落常量 = `Player_Profile_Border`");
    }

    // ------------------------------------------------------------ B 资源计数文案（`UIResourceCounter.SetQuantity` 的非动画那一半）
    static void SectionB_资源计数文案()
    {
        MenuCheck.Section(_sink, "B `UIResourceCounter.SetQuantity`：`qty` / `qty/max` 两态（+ 计数数据口）");

        // `.c:64-70`：`if (maxAmount > 0) 后缀 = "/max" else 后缀 = ""` —— ⛔ 判据是 `> 0`，不是 `!= 0`
        C(MainMenuRuntime.CounterText(5, 0), "5", "B1 `max = 0` ⇒ **不带后缀**（判据 `max > 0`）");
        C(MainMenuRuntime.CounterText(5, -1), "5", "B2 `max = -1`（原版基类 `ICurrency.MaxAmount` 的默认）⇒ 仍不带后缀");
        C(MainMenuRuntime.CounterText(5, 2000), "5/2000", "B3 `max = 2000` ⇒ `qty/max`");

        // 数据口两态（`ForceCounterForTest` 是自检拨盘；段尾必须还原）· 🔴 R5 订正（2026-10-29 FX5 · 判据 `B94_DG2_battle_menu.md` §二·#9）：段首先记**生产上限** `savedMax`（`campaignPoints` 的 `CounterSpec.MaxAmount` 本来就是 2000，`ClearCounterForTest()` 只清拨盘 ⇒ 回落到 2000 ⇒ 期望 0 是假红），段尾跟它比 —— ⛔ 别写死 0。
        var saved = MainMenuRuntime.CurCampaignPoints; int savedMax = MainMenuRuntime.MaxOf(saved);
        try
        {
            MainMenuRuntime.ForceCounterForTest(saved, 5, 500);
            C(MainMenuRuntime.OwnedOf(saved), 5, "B4 自检拨盘：持有数口读得到（= 原版 `CurrencyExtensions.GetOwnedCount`）");
            C(MainMenuRuntime.MaxOf(saved), 500, "B5 上限口读得到（= 原版 `ICurrency.MaxAmount`）");
            C(MainMenuRuntime.CounterText(MainMenuRuntime.OwnedOf(saved), MainMenuRuntime.MaxOf(saved)), "5/500",
              "B6 ★ 上限是**通用机制**（换一颗币种，文案跟着变 ⇒ ⛔ 不是把 `/2000` 写死在活动点上）");
            C(MainMenuRuntime.ShouldShow(saved), true, "B7 `DefaultCurrencies` 里那三颗恒显（campaignPoints 只在 >0 时显，这里 5 > 0）");
        }
        finally { MainMenuRuntime.ClearCounterForTest(); }
        C(MainMenuRuntime.MaxOf(saved), savedMax, "B8 拨盘已还原（收尾不许漏到别的段）—— 与段首记下的生产上限比对（现读 " + savedMax + "；⛔ 别写死 0，那不是「还原」的判据）");
    }

    // ------------------------------------------------------------ C 教程完成谓词（`PlayerDataManager` 那三条）
    static void SectionC_教程完成谓词()
    {
        MenuCheck.Section(_sink, "C `AllTutorialsCompleted` / `FirstTwoTutorialCompleted` / `IsTutorialIndexCompleted`");

        int n = MenuTutorialStages.Count;
        Tr(n >= 2, "C1 前提（硬检查）：阶段表至少 2 条（现读 " + n + " 条）—— 少于 2 条时 C5/C6 无意义");
        if (n < 2) return;

        // 备份 → 清空 → 断言 → 还原（⛔ 不能把玩家真进度留下改动）
        var keys = new List<string>();
        var saved = new List<int>();
        for (int i = 0; i < n; i++)
        {
            var d = MenuTutorialStages.At(i);
            string k = DoneKeyOf(d);
            keys.Add(k);
            saved.Add(k == null ? 0 : PlayerPrefs.GetInt(k, 0));
        }
        try
        {
            for (int i = 0; i < n; i++) if (keys[i] != null) PlayerPrefs.DeleteKey(keys[i]);
            C(MenuTutorialFlow.AllTutorialsCompleted(), false, "C2 ★ 一条都没完成 ⇒ `AllTutorialsCompleted` = 假");
            C(MenuTutorialFlow.FirstTwoTutorialCompleted(), false, "C3 一条都没完成 ⇒ `FirstTwoTutorialCompleted` = 假");

            for (int i = 0; i < n; i++) if (keys[i] != null) PlayerPrefs.SetInt(keys[i], 1);
            C(MenuTutorialFlow.AllTutorialsCompleted(), true,
              "C4 ★ 全部完成 ⇒ 真（原版 `数 key == dict.Count`；把判据改成 `done > 0` ⇒ 这条仍绿、C2 红 ⇒ 两态合起来才分得出真假）");
            C(MenuTutorialFlow.FirstTwoTutorialCompleted(), true, "C5 ★ 前两段都完成 ⇒ `IsTutorialIndexCompleted(0) && (1)` = 真");

            if (keys[1] != null) PlayerPrefs.DeleteKey(keys[1]);        // 只留第 0 段完成
            C(MenuTutorialFlow.IsTutorialIndexCompleted(0), true, "C6 第 0 段完成位读得出来");
            C(MenuTutorialFlow.IsTutorialIndexCompleted(1), false, "C7 第 1 段被清掉 ⇒ 读得到「没完成」");
            C(MenuTutorialFlow.FirstTwoTutorialCompleted(), false,
              "C8 ★ **`&&` 不是 `||`**（把实现改成 `||` ⇒ 这条红、C5 仍绿）");
            C(MenuTutorialFlow.AllTutorialsCompleted(), false, "C9 缺一位 ⇒ 全体完成 = 假");
        }
        finally
        {
            for (int i = 0; i < n; i++)
            {
                if (keys[i] == null) continue;
                if (saved[i] == 0) PlayerPrefs.DeleteKey(keys[i]); else PlayerPrefs.SetInt(keys[i], saved[i]);
            }
            PlayerPrefs.Save();
        }
        C(MenuTutorialFlow.AllTutorialsCompleted(), AllOf(saved),
          "C10 玩家原进度已还原（收尾比对：还原后与备份逐个相等）");
    }

    /// <summary>= `MenuTutorialFlow.DoneKey`（`PrefDonePrefix + (StageId 或 "#"+ClassName)`）。⛔ 不调私有方法。</summary>
    static string DoneKeyOf(MenuTutorialStageDef d)
    {
        if (d == null) return null;
        string tail = string.IsNullOrEmpty(d.StageId) ? "#" + d.ClassName : d.StageId;
        return MenuTutorialFlow.PrefDonePrefix + tail;
    }

    static bool AllOf(List<int> flags)
    {
        for (int i = 0; i < flags.Count; i++) if (flags[i] == 0) return false;
        return flags.Count > 0;
    }

    // ------------------------------------------------------------ D 排位接口支 = 原版 NRE（照抄 §18·0）
    static void SectionD_排位接口支的NRE()
    {
        MenuCheck.Section(_sink, "D `GameModes.GetActiveEvent(Type)` 的排位接口支（原版这一支是 **NRE**，照抄）");

        var savedIface = GameModesHandler.RankedEventInterfaceType;
        var savedMain = GameModesConfigSource.MainRankedType;
        var handler = new GameModesHandler();
        try
        {
            // ① 问的是「排位接口」，而远端 `GameModesConfig.mainRankedType` 本地拿不到 ⇒ 原版 `goto LAB_18068658b`
            //    → `/* Subroutine does not return */ FUN_1803f47a0();` = **throw**
            GameModesHandler.RankedEventInterfaceType = typeof(GameModesHandler);
            GameModesConfigSource.MainRankedType = null;
            bool threw = false;
            try { handler.GetActiveEvent(typeof(GameModesHandler)); }
            catch (NullReferenceException) { threw = true; }
            catch (Exception) { /* 别的异常也算「不是 return null」，但不合判据 */ }
            Tr(threw, "D1 ★ 原版这一支 **throw NRE**（改回 `return null` ⇒ 这条红 = 灭自证）");

            // ② 同一入口、`mainRankedType == 排位接口本身` ⇒ 不进递归、落到 LINQ（不抛）
            GameModesConfigSource.MainRankedType = typeof(GameModesHandler);
            bool threw2 = false; object r = "未调用";
            try { r = handler.GetActiveEvent(typeof(GameModesHandler)); }
            catch (Exception) { threw2 = true; }
            C(threw2, false, "D2 ★ 配了 `mainRankedType` ⇒ **不抛**（与 D1 两态对照）");
            C(r == null, true, "D3 …且事件表里没有这个类型 ⇒ `FirstOrDefault()` = null（不是抛）");

            // ③ 没注入排位接口类型（默认态）⇒ 整支进不去，直接 LINQ
            GameModesHandler.RankedEventInterfaceType = null;
            GameModesConfigSource.MainRankedType = null;
            bool threw3 = false;
            try { handler.GetActiveEvent(typeof(GameModesHandler)); } catch (Exception) { threw3 = true; }
            C(threw3, false, "D4 ★ 默认态（`RankedEventInterfaceType = null`）⇒ 那支根本不进，⛔ 不抛");
        }
        finally
        {
            GameModesHandler.RankedEventInterfaceType = savedIface;
            GameModesConfigSource.MainRankedType = savedMain;
        }
    }

    // ------------------------------------------------------------ E 战将精通（`WarlordMasteryContainer.OnInitialize` 的 ②③④）
    static void SectionE_战将精通纯逻辑()
    {
        MenuCheck.Section(_sink, "E `WarlordMasteryContainer.OnInitialize` 的 ②③④（表选取 / 日志优先 / UM47 兜底）");

        Func<string, bool> hasNothing = id => false;
        Func<string, bool> hasUm47 = id => id == ProfileTab.WarlordMasteryFallbackCardId;
        Func<string, bool> hasAll = id => id == "UM47" || id == "UM82" || id == "DA1" || id == "SW5";

        // ② 空表 ⇒ 什么都不写（原版 `FirstOrDefault` 给 default，后段会 NRE；我们不崩宿主）
        var empty = ProfileTab.ResolveWarlordMastery(null, null, hasAll);
        C(empty.Has, false, "E1 表是 null ⇒ `Has = false`（⛔ 不编一个 0 胜）");

        // ② 按 `totalVictories` 取最大那一组（两态：次大那组先塞进去）· 🔴 R6 订正（2026-10-29 FX5 · 判据 `B94_DG2_battle_menu.md` §二·#10）：原版 ③④ **只查最高那一组**的 `victoriesById`（`WarlordMasteryContainer__OnInitialize.c:70-84`）⇒ `DA1` 必须放进 **UM82**（9 胜那组）；原来放在次大组 `SW5`（3 胜）⇒ E6 无论实现对不对都取不到它（假红）。⚠️ E4 仍取 `UM47`（该组里计数最大者）—— 两态照样可分。
        var map = new Dictionary<string, VictoryCounter>
        {
            { "SW5", new VictoryCounter { totalVictories = 3, victoriesById = new Dictionary<string, int>() } },
            { "UM82", new VictoryCounter { totalVictories = 9, victoriesById = new Dictionary<string, int> { { "UM47", 4 }, { "DA1", 2 } } } },
        };
        var v = ProfileTab.ResolveWarlordMastery(map, null, hasAll);
        C(v.Has, true, "E2 有表 ⇒ `Has = true`");
        C(v.Victories, 9, "E3 ★ 取 `OrderByDescending(totalVictories).First()` ⇒ **9**（不是 3）");
        C(v.CardId, "UM47", "E4 ★ 没有战报 ⇒ 走 `HighestCardId`（该组 `victoriesById` 里计数最大的 key）");

        // ② `Math.Max(total, 1)`：0 胜 ⇒ 1（原版 ⑥ 的实参）
        var zero = new Dictionary<string, VictoryCounter>
        { { "SW5", new VictoryCounter { totalVictories = 0, victoriesById = new Dictionary<string, int>() } } };
        C(ProfileTab.ResolveWarlordMastery(zero, null, hasAll).Victories, 1,
          "E5 ★ `Math.Max(totalVictories, 1)` ⇒ 0 胜时印 **1**（删掉 Max ⇒ 这条红）");

        // ③ 日志优先：`ownHeroName` 在该组 `victoriesById` 里 ⇒ 用它（⛔ 不再看 HighestCardId）
        var logs = new List<string> { "ZZ9", "DA1" };
        var v3 = ProfileTab.ResolveWarlordMastery(map, logs, hasAll);
        C(v3.CardId, "DA1", "E6 ★ **战报优先**：`ZZ9` 不在桶里、`DA1` 在 ⇒ 取 `DA1`（改成一律 HighestCardId ⇒ 这条红 = E4 绿）");

        // ④ 两支都取不到 ⇒ 兜底 `UM47`
        var v4 = ProfileTab.ResolveWarlordMastery(map, null, hasUm47);
        C(v4.CardId, "UM47", "E7 ★ `GetAsset(id)` 取不到 ⇒ `GetAsset(\"UM47\")` 兜底");
        C(v4.CardResolved, true, "E8 …兜底取到了 ⇒ `CardResolved = true`（原版 ⑤ 的 `bVar10`）");

        var v5 = ProfileTab.ResolveWarlordMastery(map, null, hasNothing);
        C(v5.CardResolved, false, "E9 ★ 连 `UM47` 都没有 ⇒ `CardResolved = false`（原版那一支只写 ⑥ 的胜利数）");
        C(v5.Victories, 9, "E10 …但胜利数照算（原版 ⑥ 无条件写 —— 两件事别混）");
    }

    // ------------------------------------------------------------ F 阵营进度条（`ArmyProgressBar.SetArmies`）
    static void SectionF_阵营进度条()
    {
        MenuCheck.Section(_sink, "F `ArmyProgressBar.SetArmies` / 轮抽那两个阵营");

        // 两态 ①：Demo 变体（`TimedDraftLocal.Enabled` 默认 false）⇒ 没有 specificArmies ⇒ -1,-1
        bool savedEnabled = TimedDraftLocal.Enabled;
        var savedArmies = TimedDraftLocal.Armies;
        int l, r;
        try
        {
            TimedDraftLocal.Enabled = false;
            DraftModeWindow.DraftArmyPair(out l, out r);
            C(l, -1, "F1 ★ Demo 变体（无 `specificArmies`）⇒ 左阵营 = **-1**（= 不画，⛔ 不拿默认阵营顶上）");
            C(r, -1, "F2 …右阵营也是 -1");

            // 两态 ②：Timed ⇒ 用 `TimedDraftLocal.Armies`
            TimedDraftLocal.Enabled = true;
            TimedDraftLocal.Armies = new[] { "Ultramarines", "Goff" };
            DraftModeWindow.DraftArmyPair(out l, out r);
            C(l, ArmyIcons.Id("Ultramarines"), "F3 ★ Timed ⇒ 左阵营 = `ArmyIcons.Id(\"Ultramarines\")`");
            C(r, ArmyIcons.Id("Goff"), "F4 …右阵营 = `ArmyIcons.Id(\"Goff\")`");
            Tr(ArmyIcons.Texture(l) != null, "F5 前提：这两张阵营图本地取得到（`ArmyUtilities.GetArmyIcon` 的等价物）");

            // 真建两格（临时父件，收尾销毁）—— ⛔ 不依赖 `DraftModeWindow.Build`
            var go = new GameObject("B94S4_ArmyBar");
            try
            {
                int q = DraftModeWindow.SetArmies(go.transform, -1, -1,
                    new PxRect(0f, 0f, 74f, 74f), new PxRect(274f, 0f, 348f, 74f), 3000);
                C(q, 0, "F6 ★ 两张图都没有（army=-1）⇒ **一张都不画**（原版那两支 NRE，我们出声不崩）");
                C(DraftModeWindow.LastArmyIconsDrawn, 0, "F7 读口与返回值一致");

                q = DraftModeWindow.SetArmies(go.transform, l, r,
                    new PxRect(0f, 0f, 74f, 74f), new PxRect(274f, 0f, 348f, 74f), 3000);
                C(q, 2, "F8 ★ 两个真阵营 ⇒ **画两张**（与 F6 两态对照 —— 删掉「按阵营取图」这句 ⇒ 这条红）");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        finally
        {
            TimedDraftLocal.Enabled = savedEnabled;
            TimedDraftLocal.Armies = savedArmies;
        }
    }

    // ------------------------------------------------------------ G 头像边框层（`AvatarBorderController.Initialize`）
    static void SectionG_头像边框层()
    {
        MenuCheck.Section(_sink, "G `AvatarBorderController.Initialize()` / `Initialize(PlayerInfo)` 的等价物");

        string equipped = ProfileData.EquippedBorderArtName;
        string got = AvatarTab.BorderArtName();
        Tr(!string.IsNullOrEmpty(got), "G1 边框图名**永远非空**（原版取不到图时 `Image.sprite` 留空，我们回落常量）");
        C(got, string.IsNullOrEmpty(equipped) ? AvatarTab.ArtBorder : equipped,
          "G2 ★ 有已装备边框 ⇒ 画**已装备那一条**；没有 ⇒ 回落 `ArtBorder`（两态：把这条逻辑删掉 ⇒ 没装备时红）");

        // `FindActiveAvatarBorder()` 与 `BorderArtName()` 是**两件事**（一个是「有没有可用边框」，一个是「画哪张」）
        var active = ProfileData.FindActiveAvatarBorder();
        C(active == null, string.IsNullOrEmpty(equipped),
          "G3 ★ 「那颗钮显不显示」读的是 `FindActiveAvatarBorder() != null`，⛔ 不是读图名 —— 两者在空态下要一致");
    }
}
