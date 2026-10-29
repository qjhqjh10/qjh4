// B97W25_Assert.cs — 执行代理 B97_W25（第九十七会话 · 波 1 补位）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `轮抽:30` `DraftEventBase.SetCustomMatchOptions(RoomOptions, int)` —— 本轮把它补成**原版形状**
//   （`Shell/DraftEventBase.cs` 类尾的那个 override），并给纯函数 `NetRuntime.ComputeDraftMatchOptions` 接上**第一个生产消费者**。
//
// 判据（⛔ 别在这里抄第二份）：
//   · 真体 = `d:/2/tools/decomp_generic_trial/out_b94/DraftEventBase_object__object_.SetCustomMatchOptions__1813cb0.c`（RVA 0x1813CB0）
//   · ILSpy `out_cpp2il_dev_ilspy/Everguild.LiveOps/DraftEventBase.cs:4432`（键字面量 :4456/:4459/:4462 · lobby 键表 :4463-4473）
//   · 施工单 = `资料/完全复刻/判据/B97_R6_轮抽P2P房施工单.md` §一 + §1·3
//
// ============================ 值计算的唯一真源 ============================
//   `NetRuntime.ComputeDraftMatchOptions(currentELO, won, lossStreak)`（`Net/NetRuntime.cs:441`）
//   ⇒ 本文件**不抄** 800 / 键名 / lobby 表，全部拿它的返回值当期望（单源断言）。那一份的字面量已由
//   `Editor/B94Y6_Assert.cs` C1–C8 钉死（本文件不重复钉）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2：把 `won` 的来源改成 ILSpy 说的 `eventDeck.defensiveCard`（指针）或任何别的 int 格 ⇒ `C1` 不再是 7 ⇒ 红。
//   · B1/B3：把清零条件改成 `<=` ↔ `<`、或把 `won < 3` 写反 / 去掉 ⇒ 四态里必有一条红。
//   · B4：把 `C1` 也写成「清零后的值」⇒ B5 红（C1 必须留清零前的 won）。
//   · C1/C2/C3：把 `ComputeDraftMatchOptions` 的某格或 lobby 表改掉 ⇒ 三选一（或全）红。
//   · D0：把 `SaveGame == null` 的守卫删掉（直接读 ⇒ NRE / 或照写）⇒ D0 至少一条红（options 不再是空的）。
//
// ============================ 怎么用（宿主）============================
// `B97W25Assert.Run();`
// `_sink.Pass += B97W25Assert.Pass; _sink.Fail += B97W25Assert.Fail;`
//   · 建议宿主 = `Editor/ShellScene.cs`（`ShellScene.Run`，与 `B94Y6_Assert` 同一条线；追加到它合计行**之前**那一行 ⇒ 行数零变化）
//   · 不跑 Unity 批处理、不建持久资产；建的 `GameObject` 都在 `finally` 里 `DestroyImmediate`（本文件其实不建 GameObject）。
//   · ⚠️ `GameModesHandler.RankedSharedData` 在本仓**没跑过 `SetupEvents` 时是 null** ⇒ 被测方法里那句 `handler.LossStreak`
//     会打一行 `[GameModes] LossStreak：RankedSharedData 是 null …` 的 **LogError**（那是**生产守卫**、不是断言失败）。
//     本文件**故意**不调 `SetupEvents`（那会改写共享 handler 的事件表，污染同宿主其它断言）⇒ 本文件自己读
//     `RankedSharedData.lossStreak` 字段（同一格、不同访问径，避开那条 LogError 噪声）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 B97_W25：`DraftEventBase.SetCustomMatchOptions`（原版形状）的断言。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W25Assert
{
    static readonly CheckSink _sink = new CheckSink("[B97W25] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_W25 · DraftEventBase.SetCustomMatchOptions（原版形状 · 账 轮抽:30）");

        EnsureGameModesHandler();
        SectionA_Won来自胜场();
        SectionB_新手档清零();
        SectionC_单一真源();
        SectionD_守卫();

        Debug.Log(_sink.P + "B97_W25 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A `C1` = 胜场（灭自证：⛔ 不是 ILSpy 说的 `defensiveCard`）
    static void SectionA_Won来自胜场()
    {
        MenuCheck.Section(_sink, "A `C1` = `SaveGame.numberOfMatchesWon`（真体 `this+0x20 → +0x48`；⛔ 不是 ILSpy 的 `eventDeck.defensiveCard`）");

        var probe = new DraftW25Probe();
        var save = new DraftEventSave { numberOfMatchesWon = 7 };   // ★ 字面量 7（不是被测实现算出来的）
        probe.UpdateSaveGame(save);
        Tr(probe.GetBaseSave() != null, "A0 **前提断言**：探针手里有存档（`SaveGame != null`）—— 不成立 ⇒ A1..A4 没跑，本条会红");
        Tr(LiveOpsManager.GetHandler<GameModesHandler>() != null,
           "A0b **前提断言**：`GetHandler<GameModesHandler>()` 非 null（否则被测方法按原版 `.c` 的 `if (lVar3 != 0)` 守卫整段不写 ⇒ 下面全红）");

        var options = new RoomOptions();
        options.Set("C2", "sentinel2");     // 原版本方法**不写** C2/C3（由 `GetPvpRoomOptions` 先写）⇒ 必须原样保留
        options.Set("C3", "sentinel3");
        probe.SetCustomMatchOptions(options, 1200);                // 1200 > 800 ⇒ 不清零，C4 走原值那一档

        Tr(options.Get("C1") is int, "A1 `C1` 写进去了且是 `int`（原版 `CustomRoomProperties` 里那一格）");
        C(IntOrZero(options, "C1"), 7,
          "A2 ★ `C1` == 7（= `SaveGame.numberOfMatchesWon` 的字面量）—— 灭自证：照 ILSpy 那版读 `eventDeck.defensiveCard`（指针）⇒ 几乎不可能等于 7");
        C(IntOrZero(options, "C1"), save.numberOfMatchesWon,
          "A3 `C1` == `save.numberOfMatchesWon`（结构式同源；A2 是字面量那一半，两条合起来挡「碰巧相等」）");
        Tr((options.Get("C2") as string) == "sentinel2", "A4 `C2` **没被本方法覆盖**（原版这一格由 `GetPvpRoomOptions` 写，本方法不碰）");
        Tr((options.Get("C3") as string) == "sentinel3", "A4b `C3` 同上，原样保留");
    }

    // ------------------------------------------------------------ B 新手档清零：`elo <= 800 && won < 3 ⇒ elo = 0`
    static void SectionB_新手档清零()
    {
        MenuCheck.Section(_sink, "B 新手档清零：`elo <= 800 && won < 3 ⇒ C4 = 0`（两条件**同时**成立才清）");

        // B1 阈值边界（`<=`）＋ won 2（< 3）⇒ 清零
        var o1 = Build(800, 2);
        Tr(o1.Get("C4") is int, "B0 `C4` 写进去了且是 `int`");
        C(IntOrZero(o1, "C4"), 0, "B1 ★ elo == 800（阈值，`<=`）且 won == 2（< 3）⇒ `C4` = 0");
        C(IntOrZero(o1, "C1"), 2, "B1b ★ 同一次里 `C1` 仍 == 2（= 清零**前**的 won —— 清零只动 ELO 那一格）");

        // B2 elo 刚过阈值 ⇒ 不清零
        C(IntOrZero(Build(801, 2), "C4"), 801, "B2 elo == 801（> 800）且 won == 2 ⇒ `C4` **不清零**（= 801）");

        // B3 won 到 3（不 < 3）⇒ 不清零
        C(IntOrZero(Build(500, 3), "C4"), 500, "B3 elo == 500 但 won == 3（不 < 3）⇒ `C4` **不清零**（= 500）");

        // B4 两条件都刚好不满足 ⇒ 不清零（把「`<=` 写成 `<`」或「`< 3` 写成 `<= 3`」的改动逼红）
        C(IntOrZero(Build(800, 3), "C4"), 800, "B4 elo == 800 && won == 3 ⇒ `C4` **不清零**（两个条件必须同时成立；只满足一个 ⇒ 保留原值）");
    }

    // ------------------------------------------------------------ C 单一真源：值 + lobby 表都跟 `ComputeDraftMatchOptions` 一致
    static void SectionC_单一真源()
    {
        MenuCheck.Section(_sink, "C 与 `NetRuntime.ComputeDraftMatchOptions` 逐格一致（⛔ 本文件不抄 800 / 键名 / lobby 表）");

        const int elo = 640, won = 4;
        int ls = HandlerLossStreakNoNoise();                         // 同一格（`RankedSharedData.lossStreak`）的另一条访问径
        var want = NetRuntime.ComputeDraftMatchOptions(elo, won, ls); // ★ 期望**全部**来自这一个返回值

        var got = Build(elo, won);

        C(IntOrZero(got, "C1"), want.Won, "C1 `C1` == `ComputeDraftMatchOptions(...).Won`");
        C(IntOrZero(got, "C4"), want.Elo, "C2 `C4` == `ComputeDraftMatchOptions(...).Elo`（含新手档清零后的值）");
        C(IntOrZero(got, "C5"), want.LossStreak, "C3 `C5` == `ComputeDraftMatchOptions(...).LossStreak`（= `GameModes.LossStreak`）");

        var lobby = got.CustomRoomPropertiesForLobby;
        Tr(lobby != null, "C4 前提：`CustomRoomPropertiesForLobby` 被写了（原版 `.c` 末尾一定赋这个数组）");
        C(lobby != null ? lobby.Length : -1, want.LobbyKeys.Length,
          "C5 lobby 表**长度** == `ComputeDraftMatchOptions(...).LobbyKeys.Length`（⛔ 不是这里写死的 5）");
        Tr(lobby != null && SameKeys(lobby, want.LobbyKeys),
           "C6 ★ lobby 表**逐字逐序**等于 `ComputeDraftMatchOptions(...).LobbyKeys`（单源；那一份的字面量由 `B94Y6_Assert` C7/C8 钉着）");
    }

    // ------------------------------------------------------------ D 守卫（原版 `.c` 的两个早退支）
    static void SectionD_守卫()
    {
        MenuCheck.Section(_sink, "D 守卫：没存档 ⇒ 一个键都不写（原版 `.c` 的 `if (*(this + 0x20) != 0)`）");

        var probe = new DraftW25Probe();                             // 故意不 `UpdateSaveGame` ⇒ `SaveGame == null`
        var options = new RoomOptions();
        probe.SetCustomMatchOptions(options, 900);
        C(options.CustomRoomProperties.Count, 0, "D0 ★ `SaveGame == null` ⇒ `CustomRoomProperties` **一个键都没写**（原版 `.c` 的守卫；删掉守卫 ⇒ 红）");
        Tr(options.CustomRoomPropertiesForLobby == null, "D1 `SaveGame == null` ⇒ `CustomRoomPropertiesForLobby` 也没被赋（同上那一跳之前就 return 了）");

        // D2 options == null：原版第 2 句就解引用 ⇒ NRE；我们出声/直接返回，**不抛**（⛔ 静默失败也算红）
        bool threw = false;
        try { Build(900, 0, null); } catch (System.Exception) { threw = true; }
        Tr(!threw, "D2 `options == null` ⇒ **不抛异常**（本地口径；原版这里 NRE）");

        Debug.Log(_sink.P + "D3 （未断言）handler == null 那一支需要 `LiveOpsManager.ResetHandlers()`（会清掉全表 handler 的事件，"
                        + "污染同宿主其它断言）⇒ 本文件**不做**；该支行为 = 出声 + 不写任何键（照 `.c` 的 `if (lVar3 != 0)` 守卫），"
                        + "判据见 `Shell/DraftEventBase.cs` 那个 override 的注释。");
    }

    // ============================================================ 工具
    /// <summary>建一个探针、喂存档、跑一次 `SetCustomMatchOptions`，返回它写过的 `RoomOptions`。</summary>
    static RoomOptions Build(int elo, int won, RoomOptions reuse = null)
    {
        var probe = new DraftW25Probe();
        probe.UpdateSaveGame(new DraftEventSave { numberOfMatchesWon = won });
        var options = reuse ?? new RoomOptions();
        probe.SetCustomMatchOptions(options, elo);
        return options;
    }

    /// <summary>取 int 值；键不存在或不是 `int` ⇒ 0（配合前置的 `is int` 断言一起用，⛔ 不拿它单独判「有没有」）。</summary>
    static int IntOrZero(RoomOptions o, string key)
    {
        object v = o.Get(key);
        return (v is int) ? (int)v : 0;
    }

    /// <summary>两张键表**逐字逐序**相等（长度不等/任一 null ⇒ false）。</summary>
    static bool SameKeys(string[] a, string[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    /// <summary>读 `GameModesHandler` 的连败数（**直接读字段**、绕开生产那个 `LogError` 守卫；null ⇒ 0，与生产兜底同值）。</summary>
    static int HandlerLossStreakNoNoise()
    {
        var h = LiveOpsManager.GetHandler<GameModesHandler>();
        if (h == null || h.RankedSharedData == null) return 0;
        return h.RankedSharedData.lossStreak;
    }

    /// <summary>保证 `GameModesHandler` 在注册表里（`Instance` 建单例时登记；被别的断言 `ResetHandlers` 过后再补登记，幂等）。</summary>
    static void EnsureGameModesHandler()
    {
        if (LiveOpsManager.InstanceOrNull == null) LiveOpsManager.EnsureStarted();
        if (LiveOpsManager.GetHandler<GameModesHandler>() == null)
            LiveOpsManager.Register(new GameModesHandler());
    }

    // ============================================================ 探针（最小可实例化子类）
    /// <summary>`DraftEventBase&lt;TS,TD&gt;` 的最小具体子类（只补 `EventPlayMode` 一个抽象成员；照 `B94B10_Assert.DraftB10Probe`）。</summary>
    sealed class DraftW25Probe : DraftEventBase<DraftEventSave, DraftEventData>
    {
        public override PlayModes EventPlayMode { get { return PlayModes.Classic; } }
    }
}
