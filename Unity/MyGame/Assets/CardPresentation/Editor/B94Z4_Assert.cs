// B94Z4_Assert.cs — 执行代理 **B94_Z4**（第九十四会话 · 波 2）的断言
//
// ============================ 它盯的是什么 ============================
// `GameModesHandler.PracticeSave.matchStarted` 的**本地落盘**（账 583 的残差①）：
//   写穿（开局 / 结算 / 续局清位）+ 重启读回 + 「那场到底打没打完」的判别。
// 逐条判据 / 做法 / 没落地部分 → `资料/完全复刻/报告/B94_Z4.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
// `B94Z4_Assert.Run();`
//   · 建议宿主（调度台统一接线，本文件不接）：**`Editor/MainMenuScene.cs`**（练习活动就在主菜单链上，
//     `IngestPracticeResults` 的生产入口是 `Shell/MainMenuRuntime.cs:4539`）。
//     接线两行：`B94Z4_Assert.Run();  _sink.Pass += B94Z4_Assert.Pass; _sink.Fail += B94Z4_Assert.Fail;`
//   · **不跑 Unity 批处理中的任何 Unity 编辑器 API、不写玩家真档**：整段把 `SaveCenter.OverrideRoot`
//     指到临时目录（`Path.GetTempPath()` 下），收尾删掉并把 `PracticeSave` 还原。
//
// ============================ 🔴 灭自证（每条都写了「改哪一处会让它红」）============================
//   · A2 = 开局那拍**真落盘**：删掉 `StartPracticeMatchNoCheck` 里的 `SavePracticeMatchStarted()` ⇒ 红。
//   · A6 = 重启后读回 true：删掉 `LoadPracticeMatchStarted` 里那句 `PracticeSave.matchStarted = ongoing;` ⇒ 红。
//   · B1 = 「已经打完」判别：把 `now > practiceCountAtStart` 那段删掉（或改成恒 `ongoing`）⇒ 红。
//   · C1 = 结算口清位也写穿：删掉 `OnPracticeMatchFinished` 里的 `SavePracticeMatchStarted()` ⇒ 红。
//   · D1/D3 = 端到端「重启 ⇒ 判到未收尾 ⇒ 清位写盘」：删掉 `InitializePracticeForMenuStart` 里的
//     `LoadPracticeMatchStarted()` ⇒ D1 红；删掉那句 `SavePracticeMatchStarted()` ⇒ D3 红。
//   · E1 = 内存权威（本进程改过就不许被旧盘档覆盖）：删掉 `_practiceMatchStartedSynced` 闸 ⇒ 红。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 B94_Z4：练习活动 `matchStarted` 本地落盘的断言。
/// <para>⛔ **期望值全是原版实读结论 / 字面量**（原版写 `+0x44` 的唯一处 = `OnMatchFound`，读 = `Initialize`），
/// **不从被测实现里反读**。</para></summary>
public static class B94Z4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B94Z4] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94-Z4：练习活动 `matchStarted` 本地落盘（写穿 / 重启读回 / 已打完判别 / 内存权威）");

        string prevRoot = SaveCenter.OverrideRoot;
        var prevSave = GameModesHandler.PracticeSave;
        bool prevDraft = GameModesHandler.DraftModeUnlocked;
        string dir = Path.Combine(Path.GetTempPath(), "wf_b94z4_" + Guid.NewGuid().ToString("N"));
        var emptyLog = new List<BattleLogData.Match>();
        var oneLog = new List<BattleLogData.Match> { PracticeMatch() };
        try
        {
            GameModesHandler.ResetPracticeForTest();
            SaveCenter.OverrideRoot = dir;
            Directory.CreateDirectory(dir);

            // ------------------------------------------------------------ P 前提（硬检查，⛔ 不许写成 `if (前提) Check(结论)`）
            MenuCheck.Section(_sink, "P 前提：落盘门真的开着 · 键表认这个 key · 起点没有档");
            Tr(SaveCenter.StoreOn,
               "P1 **前提**：设了 `OverrideRoot` ⇒ `SaveCenter.StoreOn` 为真（下面的写盘是真落，不是空转）");
            Tr(GameModesHandler.PracticeMatchStartedKey.StartsWith(SaveKeys.OursPrefix, StringComparison.Ordinal),
               "P2 **前提**：key 带 `_ours_` 前缀（`" + GameModesHandler.PracticeMatchStartedKey + "`）—— 这是自定键的唯一合法形状");
            Tr(SaveKeys.IsKnown(GameModesHandler.PracticeMatchStartedKey),
               "P2b **前提**：`SaveKeys.IsKnown` 认它（否则 `SaveCenter` 会拒写，下面全是假绿）");
            Tr(!SaveCenter.Exists(GameModesHandler.PracticeMatchStartedKey),
               "P3 **前提**：临时目录起点**没有**这份档（否则「读回」那几条测不出东西）");
            C(GameModesHandler.PracticeSave.matchStarted, false, "P4 **前提**：`PracticeSave.matchStarted` 起点 = false（原版默认）");

            // ------------------------------------------------------------ A 开局写穿
            MenuCheck.Section(_sink, "A 开局那拍：内存置 true + **真的落盘**（原版写 `+0x44` 的唯一处 = `OnMatchFound`）");
            new GameModesHandler().StartPracticeMatchNoCheck("b94z4-deck");
            C(GameModesHandler.PracticeSave.matchStarted, true,
              "A1 开局那拍把内存置 true（本仓等价点 = `StartPracticeMatchNoCheck`，练习没有匹配回包）");
            Tr(SaveCenter.Exists(GameModesHandler.PracticeMatchStartedKey),
               "A2 ★ 开局那拍**真的落盘了** —— 把 `StartPracticeMatchNoCheck` 里的 `SavePracticeMatchStarted()` 删掉 ⇒ 这条红");
            var disk1 = SaveCenter.Get<GameModesHandler.PracticeMatchStartedPayload>(GameModesHandler.PracticeMatchStartedKey);
            Tr(disk1 != null, "A3 **前提**：盘上那份能读回来（读不出 ⇒ 下面几条没意义）");
            C(disk1 != null && disk1.matchStarted, true,
              "A3b 盘上那份 `matchStarted = true`（读的是**盘**，不是内存自证）");
            C(disk1 == null ? -1 : disk1.practiceCountAtStart, 0,
              "A4 同时记下「开局那拍对局记录里已有 0 场练习」—— 这一格是「打没打完」的判别输入");

            // ------------------------------------------------------------ A5/A6 「重启」：记录里没有新场次 ⇒ 还在打
            MenuCheck.Section(_sink, "A5/A6 重启（= 内存丢了）：盘上 true + 记录里没多出练习 ⇒ 读回 true（那一场还在打）");
            GameModesHandler.ResetPracticeForTest();
            C(GameModesHandler.PracticeSave.matchStarted, false, "A5 **前提**：`ResetPracticeForTest` 之后内存回到 false（模拟重启）");
            GameModesHandler.LoadPracticeMatchStarted(emptyLog);
            C(GameModesHandler.PracticeSave.matchStarted, true,
              "A6 ★ 重启后读回 **true** —— 本功能的核心（删掉 `LoadPracticeMatchStarted` 里 `PracticeSave.matchStarted = ongoing;` ⇒ 这条红）");

            // ------------------------------------------------------------ B 判别：「那场已经打完」
            MenuCheck.Section(_sink, "B 「开局以来记录里多出了练习场次」⇒ 那一场已打完 ⇒ 读回 false（= 原版服务器回包里清位）");
            GameModesHandler.ResetPracticeForTest();
            SaveCenter.Set(GameModesHandler.PracticeMatchStartedKey,
                           new GameModesHandler.PracticeMatchStartedPayload { matchStarted = true, practiceCountAtStart = 0 });
            Tr(GameModesHandler.LoadPracticeMatchStarted(oneLog),
               "B0 **前提**：盘上有档 ⇒ `Load` 应用了值（返回 true）");
            C(GameModesHandler.PracticeSave.matchStarted, false,
              "B1 ★ 开局时 0 场、现在 1 场 ⇒ 那一场**已经打完** ⇒ 读回 false（把 `now > practiceCountAtStart` 那段删掉 / 改成恒 true ⇒ 这条红）");

            MenuCheck.Section(_sink, "B2 盘上本来就是 false ⇒ 读回 false（与记录里多不多无关）");
            GameModesHandler.ResetPracticeForTest();
            SaveCenter.Set(GameModesHandler.PracticeMatchStartedKey,
                           new GameModesHandler.PracticeMatchStartedPayload { matchStarted = false, practiceCountAtStart = 0 });
            GameModesHandler.LoadPracticeMatchStarted(oneLog);
            C(GameModesHandler.PracticeSave.matchStarted, false, "B2 盘上 false ⇒ 读回 false");

            // ------------------------------------------------------------ C 结算口清位也写穿
            MenuCheck.Section(_sink, "C 结算口：清位 + **写穿**（原版这一步在服务器回包里）");
            GameModesHandler.ResetPracticeForTest();
            SaveCenter.Delete(GameModesHandler.PracticeMatchStartedKey);
            new GameModesHandler().StartPracticeMatchNoCheck("b94z4-deck");
            Tr(SaveCenter.Exists(GameModesHandler.PracticeMatchStartedKey), "C0 **前提**：开局后又有了盘档");
            new GameModesHandler().OnPracticeMatchFinished(BattleWinner.Player, 0, 0, 1f, null);
            C(GameModesHandler.PracticeSave.matchStarted, false, "C1 结算口把内存清位");
            var disk2 = SaveCenter.Get<GameModesHandler.PracticeMatchStartedPayload>(GameModesHandler.PracticeMatchStartedKey);
            C(disk2 != null && disk2.matchStarted, false,
              "C2 ★ 清位**也写穿了**（删掉 `OnPracticeMatchFinished` 里那句 `SavePracticeMatchStarted()` ⇒ 这条红）");

            // ------------------------------------------------------------ D 端到端：重启 ⇒ 判到「未收尾」⇒ 判负 + 清位写盘
            MenuCheck.Section(_sink, "D 端到端：盘上 true（上一场中途退出）⇒ 进主菜单判一次负 + 提示，并清位写盘");
            GameModesHandler.ResetPracticeForTest();
            SaveCenter.Set(GameModesHandler.PracticeMatchStartedKey,
                           new GameModesHandler.PracticeMatchStartedPayload { matchStarted = true, practiceCountAtStart = 0 });
            int lostBefore = GameModesHandler.PracticeSave.numberOfMatchesLost;
            bool hit = new GameModesHandler().InitializePracticeForMenuStart();
            Tr(hit,
               "D1 ★ 重启后（盘上 true、记录里 0 场练习）⇒ 判到「上一场没收尾」（删掉 `InitializePracticeForMenuStart` 里那句 `LoadPracticeMatchStarted()` ⇒ 这条红）");
            C(GameModesHandler.PracticeSave.numberOfMatchesLost, lostBefore + 1, "D2 按原版判一次负（`OnMatchLost` 那一支）");
            C(GameModesHandler.PracticeSave.matchStarted, false, "D3 判完清位");
            var disk3 = SaveCenter.Get<GameModesHandler.PracticeMatchStartedPayload>(GameModesHandler.PracticeMatchStartedKey);
            C(disk3 != null && disk3.matchStarted, false,
              "D3b ★ 清位**写穿了**（删掉那句 `SavePracticeMatchStarted()` ⇒ 这条红；不写穿 ⇒ 每次重启都再判一次负）");
            Tr(!new GameModesHandler().InitializePracticeForMenuStart(),
               "D4 同一份档再跑一次 ⇒ **不再命中**（⛔ 不许重复记负）");

            // ------------------------------------------------------------ E 内存权威
            MenuCheck.Section(_sink, "E 内存权威：本进程改过这一格 ⇒ 不许被盘上旧值覆盖");
            GameModesHandler.ResetPracticeForTest();
            SaveCenter.Set(GameModesHandler.PracticeMatchStartedKey,
                           new GameModesHandler.PracticeMatchStartedPayload { matchStarted = false, practiceCountAtStart = 0 });
            GameModesHandler.PracticeSave.matchStarted = true;    // 本进程「改过」（由上一句 Reset 起 synced=false，故先真跑一次同步）
            GameModesHandler.LoadPracticeMatchStarted(emptyLog);   // 第一次同步：盘上 false ⇒ 会把它覆盖成 false（预期行为）
            C(GameModesHandler.PracticeSave.matchStarted, false, "E0 **前提**：第一次同步确实按盘上那份来（false）");
            GameModesHandler.PracticeSave.matchStarted = true;     // 本进程此刻是权威方
            SaveCenter.Set(GameModesHandler.PracticeMatchStartedKey,
                           new GameModesHandler.PracticeMatchStartedPayload { matchStarted = false, practiceCountAtStart = 0 });
            GameModesHandler.LoadPracticeMatchStarted(emptyLog);   // 第二次：本进程已同步过 ⇒ 不许读盘
            C(GameModesHandler.PracticeSave.matchStarted, true,
              "E1 ★ 本进程改过这一格 ⇒ `Load` **不许**拿盘上旧值把它覆盖回 false（删掉 `_practiceMatchStartedSynced` 闸 ⇒ 这条红）");

            // ------------------------------------------------------------ G 旧场次重放不许清掉「从盘上读回的还在打」
            MenuCheck.Section(_sink, "G 重启后回灌**更旧**的场次：不许把「从盘上读回的还在打」清掉");
            GameModesHandler.ResetPracticeForTest();
            var twoLog = new List<BattleLogData.Match> { PracticeMatch(), PracticeMatch() };
            SaveCenter.Set(GameModesHandler.PracticeMatchStartedKey,
                           new GameModesHandler.PracticeMatchStartedPayload { matchStarted = true, practiceCountAtStart = 2 });
            GameModesHandler.LoadPracticeMatchStarted(twoLog);
            C(GameModesHandler.PracticeSave.matchStarted, true,
              "G0 **前提**：盘上 true 且记录里**没多出**练习 ⇒ 读回 true");
            new GameModesHandler().OnPracticeMatchFinished(BattleWinner.Player, 0, 0, 1f, null);   // = 回灌每一步都会走到的结算口
            C(GameModesHandler.PracticeSave.matchStarted, true,
              "G1 ★ 回灌**更旧的**场次**不许**把这一格清掉（删掉 `_practiceMatchStartedFromDiskOngoing` 闸 ⇒ 这条红）");
            var disk4 = SaveCenter.Get<GameModesHandler.PracticeMatchStartedPayload>(GameModesHandler.PracticeMatchStartedKey);
            Tr(disk4 != null && disk4.matchStarted,
               "G1b 盘上也**没被写回 false**（那一支是整段跳过，不是只跳内存）");

            // ------------------------------------------------------------ F 载荷形状（只镜像这两格，计数器不落盘）
            MenuCheck.Section(_sink, "F 载荷形状：只镜像 `matchStarted` + 判别计数（⛔ 不落计数器 —— 否则回灌会重数）");
            var fields = typeof(GameModesHandler.PracticeMatchStartedPayload).GetFields(BindingFlags.Public | BindingFlags.Instance);
            var names = new List<string>();
            for (int i = 0; i < fields.Length; i++) names.Add(fields[i].Name);
            names.Sort(StringComparer.Ordinal);
            C(string.Join(",", names.ToArray()), "matchStarted,practiceCountAtStart",
              "F1 载荷**恰好**两格：`matchStarted` + `practiceCountAtStart`（把胜/负/平计数器也搬进来 ⇒ 这条红 + 重启后回灌会重数一遍）");
        }
        finally
        {
            GameModesHandler.ResetPracticeForTest();            // 顺带把本文件用的两个 static 同步位清回初值
            GameModesHandler.PracticeSave = prevSave;
            GameModesHandler.DraftModeUnlocked = prevDraft;
            SaveCenter.OverrideRoot = prevRoot;
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
            catch (Exception e) { Debug.LogWarning("[B94Z4] 临时存档目录没删掉（" + dir + "）：" + e.Message); }
        }

        Debug.Log(_sink.P + "B94-Z4 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    /// <summary>造一条「练习」对局记录（模式名取 `GameMode.OfflinePractice` —— `PracticeOutcomes` 认的那两档之一）。</summary>
    static BattleLogData.Match PracticeMatch()
    {
        var m = new BattleLogData.Match();
        m.Mode = RuleEngine.PlayModeNames.Name(RuleEngine.GameMode.OfflinePractice);
        m.Result = BattleLogData.Outcome.Victory;
        return m;
    }
}
