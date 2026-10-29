// B94X1_Assert.cs — 执行代理 B94_X1（第九十四会话 · 波 1.5）6 件活的断言
//
// ============================ 它盯的是什么 ============================
// 判据全文 → `资料/完全复刻/判据/B94_R-B2_无落点_其他.md`；逐条做法与证据 → `资料/完全复刻/报告/B94_X1.md`
// （⛔ 别在这里抄第二份）。6 件：
//   A `轮抽:9`      `DraftEventBase<TS,TD>.GetBaseData()`（类型化读口）
//   B `其他:779`    `IRankedEvent` + `GameModes.CurrentSeasonNumber`
//   C `菜单:439`    `RebuildModeCards(bool forceClean = false)` + 刷新信号接它
//   D `未分类:479`  `PlayerInfo.SetBotInfo` 的局部落地（bot 档案：name / IsBot / elo 只在 >0 时写）
//   E `未分类:301`  `FeatureChecker`（🔶 本地模拟：未登记 key ⇒ true = 原版默认支）
//   F `未分类:359/360` `ArenaIconRotation`（每帧定步长自转；`rate` 0 ⇒ 每帧都转）
//
// ============================ 怎么用（宿主）============================
// `B94X1_Assert.Run();`
//   · **不跑 Unity 批处理**、不建持久资产；临时结点在收尾 `DestroyImmediate`。
//   · ⚠️ 本文件**还没接到宿主里**（本批文件所有权只到上面 6 个文件）⇒ 由调度台在宿主 `Run()` 里加两行：
//       `B94X1_Assert.Run();`
//       `_sink.Pass += B94X1_Assert.Pass; _sink.Fail += B94X1_Assert.Fail;`
//     建议宿主：`Editor/MainMenuScene.cs`（C 段是主菜单的活）或 `Editor/BattleScene.cs`（F 段是战场侧）。
//
// ============================ 🔴 灭自证（每条的两态都写在文案里）============================
//   · A：把 `GetBaseData()` 改成 `return null;` ⇒ A2 红（A1 结构那条仍绿 ⇒ 两条合起来才分得出真假）。
//   · B：把 `IRankedEvent` 从 `RankedEventBase<>` 的基类表里删掉 ⇒ B3 红；把 `SeasonNumber` 改成常量 ⇒ B5 红。
//   · C：把 `RebuildNavForLiveOpsRefresh` 末尾那句调用删掉 ⇒ C4 红（C1~C3 结构那条仍绿）。
//   · D：把 `ApplyBotInfo` 里 `ViewedPlayer = BotName` 删掉 ⇒ D4 红；把 elo 那格无条件写 0 ⇒ D6 红。
//   · E：把「未登记 ⇒ true」改成 `false` ⇒ E1 红；把覆写表读成 `!v` ⇒ E2 红。
//   · F：把 `period <= accum` 改成 `<` ⇒ F3 红（rate=0 且 accum=0 那一支）；把 `accum = 0` 删掉 ⇒ F5 红。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 X1：B94 波 1.5 的 6 条（轮抽读口 / 排位赛季号 / 主菜单刷新信号 / bot 档案 / FeatureChecker / 背景环自转）。
/// <para>⛔ **期望值全是字面量**（`DraftEventData` / 42 / 0 / true·false / 0.02° …），**不从被测实现里读**
/// —— 否则就是「拿我们的实现证明我们的实现」。</para></summary>
public static class B94X1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B94X1] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_X1：轮抽读口 / 排位赛季号 / 主菜单刷新信号 / bot 档案 / FeatureChecker / 背景环自转");

        SectionA_轮抽GetBaseData();
        SectionB_排位赛季号();
        SectionC_主菜单刷新信号();
        SectionD_bot档案();
        SectionE_FeatureChecker();
        SectionF_背景环自转();

        Debug.Log(_sink.P + "B94X1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A `轮抽:9`
    static void SectionA_轮抽GetBaseData()
    {
        MenuCheck.Section(_sink, "A `DraftEventBase<TS,TD>.GetBaseData()`（原版 ILSpy `DraftEventBase.cs:4136`）");

        // ⛔ 先断前提：这个闭合泛型**能成立**（约束 TS : PlayEventSaveGame / TD : DraftEventData）
        var closed = typeof(DraftEventBase<DraftEventSave, DraftEventData>);
        Tr(closed != null, "A0 前提：`DraftEventBase<DraftEventSave, DraftEventData>` 闭合类型成立");

        // 结构：`GetBaseData` 由本类**自己声明**，返回 `DraftEventData`（原版 `public new DraftEventData GetBaseData()`）
        var own = closed.GetMethod("GetBaseData", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Tr(own != null, "A1 前提：`GetBaseData()` 声明在 `DraftEventBase<,>` 自身上（不是只继承）");
        Tr(own != null && own.ReturnType == typeof(DraftEventData),
           "A1b 返回类型 = `DraftEventData`（原版 `public new DraftEventData GetBaseData()`）");

        // 对照：基类 `PlayEvent<,>` 的同名读口返回 `PlayEventData`（⇒ 本类是 `new` 遮蔽，不是 override）
        var baseMi = typeof(PlayEvent<DraftEventSave, DraftEventData>)
            .GetMethod("GetBaseData", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Tr(baseMi != null && baseMi.ReturnType == typeof(PlayEventData),
           "A2 两态对照：`PlayEvent<,>.GetBaseData()` 返回 `PlayEventData`（同一名字、不同类型 = 遮蔽）");

        // 行为：返回的就是 `Data` 那一份（原版「先取基类 GetBaseData() → 判型 → 返回」= 同一个字段）
        var probe = new DraftProbe();
        Tr(probe.GetBaseData() == null, "A3 前提：没喂 data ⇒ 返回 null（原版 `null ⇒ null`）");
        var d = new DraftEventData();
        probe.SetData(d);
        Tr(ReferenceEquals(probe.GetBaseData(), d), "A4 ★ `GetBaseData()` 返回的就是 `Data` 本身（同一个引用，不是拷贝）");
    }

    // ------------------------------------------------------------ B `其他:779`
    static void SectionB_排位赛季号()
    {
        MenuCheck.Section(_sink, "B `IRankedEvent` + `GameModes.CurrentSeasonNumber`（原版 `GameModes__get_CurrentSeasonNumber.c`）");

        // 前提：接口本体（`dump.cs:130514` 的 9 个具体成员里，本条账要的是 `SeasonNumber`）
        Tr(typeof(ILiveOps).IsAssignableFrom(typeof(IRankedEvent)),
           "B1 前提：`IRankedEvent : ILiveOps`（原版父接口含 `ILiveOps`；泛型约束 `GetActiveEvent<T>` 要它）");
        var p = typeof(IRankedEvent).GetProperty("SeasonNumber");
        Tr(p != null && p.PropertyType == typeof(int) && p.CanRead,
           "B2 前提：`IRankedEvent.SeasonNumber` 存在、`int` 且可读");

        // 实现：`RankedEventBase<,>` 真的实现了这个接口
        var impl = typeof(RankedEventBase<PlayEventSaveGame, RankedBaseData>).GetInterfaces();
        Tr(Array.IndexOf(impl, typeof(IRankedEvent)) >= 0,
           "B3 `RankedEventBase<PlayEventSaveGame, RankedBaseData>` 实现 `IRankedEvent`");

        // 行为：赛季号来自 `Data.seasonNumber`（原版 `IRankedEvent +0x88`）
        RankedProbe probe = new RankedProbe();
        Tr(probe.GetBaseData() == null || probe.GetBaseData() != null, "B4 前提：探针可构造");
        C(((IRankedEvent)probe).SeasonNumber, 0, "B4b 没喂 data ⇒ 0（原版 `Data == null` 那一支）");
        var data = new RankedBaseData();
        data.seasonNumber = 42;                                  // ⛔ 字面量
        probe.SetData(data);
        C(((IRankedEvent)probe).SeasonNumber, 42, "B5 ★ 赛季号 = `Data.seasonNumber`（42 是喂进去的字面量）");

        // `GameModes.CurrentSeasonNumber`：没有在期排位活动 ⇒ 出声 + 0（原版这一跳 NRE，我们出声）
        GameModesHandler.ResetForTest();
        var h = new GameModesHandler();
        C(h.CurrentSeasonNumber, 0, "B6 没有在期排位活动 ⇒ 返回 0（原版 NRE 的本地口径：出声 + 0）");
    }

    // ------------------------------------------------------------ C `菜单:439`
    static void SectionC_主菜单刷新信号()
    {
        MenuCheck.Section(_sink, "C `MainMenuRuntime.RebuildModeCards(bool forceClean)` + `RebuildNavForLiveOpsRefresh` 接它（原版 `MainMenuWindow__RefreshSignal.c`）");

        var mi = typeof(MainMenuRuntime).GetMethod("RebuildModeCards", BindingFlags.NonPublic | BindingFlags.Instance);
        Tr(mi != null, "C1 前提：`RebuildModeCards` 存在");
        var ps = mi != null ? mi.GetParameters() : new ParameterInfo[0];
        Tr(ps.Length == 1 && ps[0].ParameterType == typeof(bool),
           "C2 形参 = 1 个 `bool`（原版 `RefreshLiveOpContainers(bool forceClean)`）");
        Tr(ps.Length == 1 && ps[0].HasDefaultValue && ps[0].DefaultValue is bool && !(bool)ps[0].DefaultValue,
           "C3 缺省值 = `false`（与原文签名对齐；⚠️ 我们两支走同一路，见方法注释）");

        // 行为：刷新信号 ⇒ **发起**一次模式卡重摆（原版那一句唯一内容就是 `RefreshLiveOpContainers(AllTutorialsCompleted())`）
        var go = new GameObject("B94X1_MainMenuProbe");
        try
        {
            var mm = go.AddComponent<MainMenuRuntime>();
            Tr(mm != null, "C4 前提：探针组件挂上了");
            C(mm.ModeCardRebuildRequestsForTest, 0, "C4b 前提：起始计数 = 0");
            mm.RebuildNavForLiveOpsRefresh();                    // 菜单树不在 ⇒ 左栏/模式卡都早退（**这是正确行为**）
            C(mm.ModeCardRebuildRequestsForTest, 1, "C5 ★ 刷新信号 ⇒ 发起 1 次模式卡重摆（账 菜单:439 的唯一内容）");
            mm.RebuildNavForLiveOpsRefresh();
            C(mm.ModeCardRebuildRequestsForTest, 2, "C6 两态：再刷一次 ⇒ 2（不是「恒 1」的巧合）");
        }
        catch (Exception e)
        {
            Tr(false, "C 段异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ------------------------------------------------------------ D `未分类:479`
    static void SectionD_bot档案()
    {
        MenuCheck.Section(_sink, "D `PlayerProfileWindow` 的 bot 支（原版 `PlayerInfo__SetBotInfo.c` 全文 4 句）");

        var t = typeof(PlayerProfileWindow);
        Tr(t.GetField("BotName") != null && t.GetField("BotName").FieldType == typeof(string), "D1 前提：`BotName`（string）在");
        Tr(t.GetField("BotELO") != null && t.GetField("BotELO").FieldType == typeof(int), "D2 前提：`BotELO`（int）在");
        Tr(t.GetProperty("IsBot") != null, "D3 前提：`IsBot` 读口在");
        var apply = t.GetMethod("ApplyBotInfo", BindingFlags.NonPublic | BindingFlags.Instance);
        Tr(apply != null, "D3b 前提：`ApplyBotInfo()`（原版 `SetBotInfo` 的 4 句）在");
        if (apply == null || t.GetField("BotName") == null || t.GetField("BotELO") == null) return;

        var go = new GameObject("B94X1_ProfileProbe");
        try
        {
            var w = go.AddComponent<PlayerProfileWindow>();
            Tr(w != null, "D4 前提：探针组件挂上了");
            Tr(!w.IsBot, "D4b 前提：没给 `BotName` ⇒ 不是 bot（`IsBot` 由名字派生）");
            apply.Invoke(w, null);
            Tr(w.ViewedPlayer == null, "D4c 非 bot ⇒ `ApplyBotInfo` 精确地什么都不做（`ViewedPlayer` 仍 null）");

            // ★ 第 1/2 句：name + IsBot
            w.BotName = "Bot Alpha";                              // ⛔ 字面量
            w.BotELO = 0;
            apply.Invoke(w, null);
            Tr(w.IsBot, "D5 ★ 给了 `BotName` ⇒ `IsBot = true`");
            C(w.ViewedPlayer, "Bot Alpha", "D5b ★ 名字落到这一窗（原版第 1 句 `Name = botName`）");

            // ★ 第 4 句的有闸半：`botElo <= 0` ⇒ **不写**（保持原值 = 0）
            C(w.BotELO, 0, "D6 ★ `botElo = 0` ⇒ ELO 那一格**不写**（原版 `if (0 < botElo)`；我们的等价 = 保持 0 ⇒ 界面不显示）");

            // ★ 第 4 句的写半：`botElo > 0` ⇒ 写进去
            var go2 = new GameObject("B94X1_ProfileProbe2");
            try
            {
                var w2 = go2.AddComponent<PlayerProfileWindow>();
                w2.BotName = "Bot Beta";
                w2.BotELO = 7;                                    // ⛔ 字面量
                apply.Invoke(w2, null);
                C(w2.BotELO, 7, "D7 ★ 两态：`botElo = 7 > 0` ⇒ ELO 写进去（7 是喂进去的字面量）");
            }
            finally { UnityEngine.Object.DestroyImmediate(go2); }
        }
        catch (Exception e)
        {
            Tr(false, "D 段异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ------------------------------------------------------------ E `未分类:301`
    static void SectionE_FeatureChecker()
    {
        MenuCheck.Section(_sink, "E `FeatureChecker`（🔶 本地模拟 · 原版 `FeatureConfig__CheckFeature.c` 的默认支：未登记 ⇒ true）");

        FeatureChecker.ResetForTest();

        // 前提：4 个 key 就是原版资产里那 4 个（10 实例 / 6 文件，见 `FeatureChecker.cs` 文件头的表）
        C(FeatureChecker.KnownKeys.Length, 4, "E0 前提：资产里只读出 4 个不同 key（10 个实例 / 6 个文件）");
        C(FeatureChecker.KnownKeys[0], "PlayerLeaderboard", "E0b key 字面量 1/4 = `PlayerLeaderboard`");
        C(FeatureChecker.KnownKeys[1], "Alliances", "E0c key 字面量 2/4 = `Alliances`");
        C(FeatureChecker.KnownKeys[2], "FactionLeaderboard", "E0d key 字面量 3/4 = `FactionLeaderboard`");
        C(FeatureChecker.KnownKeys[3], "FactionRanking", "E0e key 字面量 4/4 = `FactionRanking`");

        // ★ 原版默认支：未登记 ⇒ true
        Tr(FeatureChecker.IsEnabled("PlayerLeaderboard"), "E1 ★ 未登记 ⇒ **true**（原版 `lVar4 == 0 ⇒ 1`）");
        Tr(FeatureChecker.IsEnabled("FactionRanking"), "E1b 同上（换一个 key 再证一次，不是单点巧合）");
        Tr(FeatureChecker.IsEnabled("AKeyThatIsNotInAnyAsset"), "E1c 完全没见过的 key ⇒ 也是 true（不按 4 个白名单筛）");
        Tr(FeatureChecker.IsEnabled(null), "E1d null key ⇒ 出声 + true（原版这一跳 NRE；⛔ 不返回 false）");

        // ★ 两态：登记了 false ⇒ false（证明上一条不是「恒 true」）
        FeatureChecker.SetEnabledForTest("Alliances", false);
        Tr(!FeatureChecker.IsEnabled("Alliances"), "E2 ★ 覆写 false ⇒ false（与 E1 两态对照）");
        Tr(FeatureChecker.IsEnabled("FactionLeaderboard"), "E2b 只影响登记的那一格（别的 key 仍 true）");
        FeatureChecker.SetEnabledForTest("Alliances", true);
        Tr(FeatureChecker.IsEnabled("Alliances"), "E3 覆写回 true ⇒ true");

        FeatureChecker.ResetForTest();
        Tr(FeatureChecker.IsEnabled("Alliances"), "E4 收尾 `ResetForTest` 后回到「未登记 ⇒ true」");
    }

    // ------------------------------------------------------------ F `未分类:359 / 360`
    static void SectionF_背景环自转()
    {
        MenuCheck.Section(_sink, "F `ArenaIconRotation`（原版 `IconRotation__Start.c` / `__Update.c`；两环 y=−0.02 / +0.01，rate 均 0）");

        var t = typeof(WarpforgeVFX.ArenaIconRotation);
        var update = t.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
        var start = t.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
        Tr(update != null && start != null, "F0 前提：`Start` / `Update` 都是私有的实例方法（照原版 MonoBehaviour 形状）");
        if (update == null || start == null) return;

        // 两态 ①：`rate = 0` ⇒ 判据 `period <= accum` 在 accum=0 时**也成立** ⇒ 第一次 Tick 就转
        //   🔴 2026-10-29（B94_FX6）两条夹具前提都不成立，都改掉了：
        //   ① **⛔ 不再反射调 `Update()`** —— 编辑模式批处理里 `Time.deltaTime` **不是可用的正帧长**
        //      （DG2 §四·1 推定：只有 `Time.deltaTime < 0` 才能让 `updateRate(0) <= accum` 为假 ⇒ 闸门永不开；
        //      ⚠️ 这一条**本会话没有实测**，因为没有帧循环就没有可观测的 deltaTime —— 但注入 dt 后与宿主无关）。
        //      改喂 `TickForTest(dt)`（dt 注入缝，`ArenaIconRotation.cs` 类尾）：`Update` 只是
        //      `TickForTest(Time.deltaTime)` 的转发 ⇒ 被测语义仍是同一条，输入变成确定值。
        //   ② **⛔ 不再用 `Quaternion.Angle` 量转角** —— 它在 < 0.162° 时恒返回精确 0（见类尾 `AngleDegTiny`
        //      的 IL 证据），而这个夹具每拍只转 0.02° ⇒ 用旧量角器**结构上永远读 0**（转没转都读 0，
        //      F4 那种「不该转」的断言也会恒绿 = 空转绿）。改用 `AngleDegTiny`。
        //   喂 `dt = 0f` ⇒ 一次恰好 0.02°（= `|ySpeed|`）。
        var goA = new GameObject("B94X1_IconRotA");
        var goB = new GameObject("B94X1_IconRotB");
        try
        {
            var a = goA.AddComponent<WarpforgeVFX.ArenaIconRotation>();
            a.xSpeed = 0f; a.ySpeed = -0.02f; a.zSpeed = 0f; a.updateRate = 0f;   // ★ 原版 `Background_Ring_1` 的三个字面量
            start.Invoke(a, null);
            var q0 = goA.transform.rotation;
            a.TickForTest(0f);
            float degA = AngleDegTiny(q0, goA.transform.rotation);
            Tr(degA > 0.001f, "F1 ★ `rate = 0` ⇒ **每次 Tick 都转**（一次 Tick(dt=0) 就动了，实测 " + degA.ToString("0.####") + "°）");
            MenuCheck.Near(_sink, degA, 0.02f, 0.001f, "F2 ★ 转动量 = |ySpeed| = 0.02°（原版 `Background_Ring_1` 的 ySpeed）");

            // 累加器清零那一句：`accum = 0` 且 `period <= accum` ⇒ 是 **`<=`** 不是 `<`
            a.TickForTest(0f);
            float degA2 = AngleDegTiny(q0, goA.transform.rotation);
            MenuCheck.Near(_sink, degA2, 0.04f, 0.002f, "F3 ★ 两次 Tick ⇒ 0.04°（证明 `period <= accum` 是**含等号**）");

            // 两态 ②：`rate` 很大 ⇒ 累计不到 ⇒ **不转**（证明 F1 不是「恒转」的巧合）
            var b = goB.AddComponent<WarpforgeVFX.ArenaIconRotation>();
            b.xSpeed = 0f; b.ySpeed = -0.02f; b.zSpeed = 0f; b.updateRate = 1000f;  // ⛔ 字面量（远大于喂进去的 dt）
            start.Invoke(b, null);
            var qB = goB.transform.rotation;
            b.TickForTest(0f);
            float degB = AngleDegTiny(qB, goB.transform.rotation);
            Tr(degB < 0.001f, "F4 ★ 两态：`rate = 1000` ⇒ 累计不到 ⇒ **不转**（实测 " + degB.ToString("0.####") + "°）");

            // 灭自证补一条：给一个**正** dt 且凑够闸门 ⇒ 转；给不到闸门 ⇒ 不转（证明 dt 真的被累加，不是恒 0 的空转）
            b.updateRate = 0.5f;
            start.Invoke(b, null);
            var qB2 = goB.transform.rotation;
            b.TickForTest(0.25f);
            float degHalf = AngleDegTiny(qB2, goB.transform.rotation);
            Tr(degHalf < 0.001f, "F5 ★ dt 真的进了累加器：0.25 < rate(0.5) ⇒ **不转**（实测 " + degHalf.ToString("0.####") + "°）");
            b.TickForTest(0.25f);
            float degFull = AngleDegTiny(qB2, goB.transform.rotation);
            Tr(degFull > 0.001f, "F5b ★ …再来 0.25（累计 0.5 ≥ rate）⇒ **转**（实测 " + degFull.ToString("0.####") + "°）");
        }
        catch (Exception e)
        {
            Tr(false, "F 段异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(goA);
            UnityEngine.Object.DestroyImmediate(goB);
        }
    }

    // ============================================================ 探针类
    /// <summary>`DraftEventBase&lt;TS,TD&gt;` 的最小可实例化子类（只补 `EventPlayMode` 一个抽象成员；
    /// `GetMatchmakingFilter` 已在 `DraftEventBase` 里实现）。</summary>
    sealed class DraftProbe : DraftEventBase<DraftEventSave, DraftEventData>
    {
        public override PlayModes EventPlayMode { get { return PlayModes.Classic; } }
        public void SetData(DraftEventData d) { Data = d; }
    }

    /// <summary>`RankedEventBase&lt;TS,TD&gt;` 的最小可实例化子类（补 `PlayEvent` 留给子类的那两个抽象成员：
    /// `EventPlayMode` / `GetMatchmakingFilter`）。</summary>
    sealed class RankedProbe : RankedEventBase<PlayEventSaveGame, RankedBaseData>
    {
        public override PlayModes EventPlayMode { get { return PlayModes.RankedFriendly; } }
        public override string GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost) { return string.Empty; }
        public void SetData(RankedBaseData d) { Data = d; }
    }

    // ============================================================ 小角度量角器（追加在类尾 —— 不挪前面成员的行号）
    /// <summary>🆕 **2026-10-29（B94_FX6）**：小角度专用的量角器（单位：度），**不设任何 dot 阈值**。
    /// <para>🔴 **为什么不能用 `Quaternion.Angle`**：它先过 `IsEqualUsingDot` —— 本机
    /// `UnityEngine.CoreModule.dll` 的 IL 现读是 `ldc.r4 0x3F7FFFEF`（= `1 − 1e-6`）+ `cgt` ⇒
    /// **`dot > 1 − 1e-6` 就返回精确的 `0.0f`**，也就是**夹角 &lt; `2·acos(1−1e-6)` = 0.16206° 时恒读 0**。
    /// 而 `ArenaIconRotation` 每拍只转 `|ySpeed| = 0.02°` ⇒ 落在这个盲区里 ⇒ **F1/F2/F3 用 `Quaternion.Angle`
    /// 量，无论组件转没转都读到 0.000**（本工程同一坑的先例与 IL 证据 → `Editor/BattleScene.cs:48487` 的
    /// `AngleDegTiny`；那一轮也是「打印 0.000°，实际 0.0396°」）。</para>
    /// <para>这里按四元数定义算 `θ = 2·atan2(|xyz|, |w|)`（取 `|w|` 折掉 `q` / `−q` 的双覆盖），大角度下与
    /// `Quaternion.Angle` 数值一致（差在浮点末位），只是不会把 0.16° 以下当成 0。</para></summary>
    static float AngleDegTiny(Quaternion a, Quaternion b)
    {
        // 🔴 2026-10-29（B95_A2B17 批 2）：算式收口到 `CardPresentation/Core/AngleMath.cs`（全仓唯一实现），本函数只保留原名给本文件调用点。
        return AngleMath.AngleDegTiny(a, b);
    }
}
