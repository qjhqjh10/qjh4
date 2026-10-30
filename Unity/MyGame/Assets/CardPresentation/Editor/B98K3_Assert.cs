// B98K3_Assert.cs — 执行代理 `B98_K3`（第九十八会话 · 波 9 · 日常线）的断言
//
// ============================ 它盯的是什么 ============================
//   · 账 `日常/奖励:524` `MissionsConfig.CleanUp()`（新建 `Shell/MissionsConfig.cs` + `Shell/ChallengeList.cs`）
//   · 账 `日常/奖励:526` `MissionsTab.InstantiateGridMissions(...)` 的**建树那一半**
//     （`Shell/MissionsTab.cs` 类尾 `InstantiateGridMissionsCore`）
//   · 账 `日常/奖励:772` `TwoSidesEvent_OnGoingTab.InitializeMilestones()` 的三档 state 公式
//     （`Shell/ParentEventWindow.cs` 类尾 `MilestoneState`）
//
// ============================ 怎么用（宿主）============================
// `B98K3Assert.Run();`
//   + 宿主里两行：`_sink.Pass += B98K3Assert.Pass; _sink.Fail += B98K3Assert.Fail;`
//   · **不建 GameObject / 不建资产 / 不开窗**（唯一例外 = C 段建一个裸 `ContainerHolder`，纯数据类）⇒ 任何宿主都能跑。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A3/A4：删掉 `if (!ch.IsInitialized) continue;`（改成全量清理）⇒ A4 红（它专门放一条**未初始化**的进去）。
//   · A5：把 `ch.ReplaceOnProgressUpdate(空 lambda)` 改成 `null` ⇒ A5 红（判「非 null 且只剩一个空方法」）。
//   · A6：把那一句整行删掉（等价于 `-=` 都没做）⇒ A6 红（原订阅者还在）。
//   · A7：把「按运行时类型分发 `OnReset`」改成「调基类某个空体」⇒ A7 红（探针的计数不涨）。
//   · B4：把 `challenges == null` 那一支改成抛异常 ⇒ B4 红。
//   · C1：把内层「逐条 `challengeProgress`」删掉（改成每个 mission 一格）⇒ C1 红（4 → 2）。
//   · C2：把 `globalIndex` 改成**每个 mission 重置** ⇒ C2 红（`F,F,T,T` → `F,F,F,F`）。
//   · C3：把 `WritesSwitch` 的 `<=` 改成 `<` ⇒ C2 红（`F,F,T,T` → `F,F,F,T`）。
//   · C4：把 `ColumnCount` 的 `4 条 ⇒ 0` 改成 `⇒ 2` ⇒ C4 红。
//   · D2/D4：把 state 公式的任一 `<` 改成 `<=` ⇒ D2 / D4 红（两个边界各一条）。
//   · D1/D5：把 `Locked` 与 `Unlocked` 对调 ⇒ D1 红。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B98_K3`：账 `日常/奖励:524` / `:526` / `:772` 的落地断言。
/// <para>⛔ 期望值全是字面量（`4` / `F,F,T,T` / 枚举成员名），**不从被测实现里读回来**。</para></summary>
public static class B98K3Assert
{
    static readonly CheckSink _sink = new CheckSink("[B98K3] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B98_K3 账 `日常:524`（`MissionsConfig.CleanUp`）· `:526`（网格建树）· `:772`（三档 state）");
        SectionA_CleanUp();
        SectionB_CleanUp边界();
        SectionC_网格建树();
        SectionD_三档state();
        SectionE_结构();
    }

    // ============================================================ 探针：一个只会数数的挑战
    /// <summary>测试专用 `MissionChallenge`（原版那一族有 10+ 个子类，本断言只需要「OnReset 被调到」这一个可观察量）。
    /// <para>🔴 它的 `OnReset` 是**普通公有方法**（不是 `override`）—— 与本仓既有约定一致
    /// （`Shell/TroopsKilledCount.cs:70` / `Shell/EnergySpent.cs:139`：基类没有 `OnReset` 抽象位）。</para></summary>
    class ProbeChallenge : MissionChallenge
    {
        public int initCount;
        public int resetCount;

        protected override void OnInitialize() { initCount++; }

        /// <summary>原版 Slot 8。计数 = 这条被调了几次。</summary>
        public void OnReset() { resetCount++; }
    }

    /// <summary>取 field-like event `OnProgressUpdate` 的私有委托字段（判「被换成空 lambda 还是 null」的唯一读口）。</summary>
    static Delegate ProgressDelegate(MissionChallenge ch)
    {
        var f = typeof(MissionChallenge).GetField("OnProgressUpdate",
                                                  BindingFlags.Instance | BindingFlags.NonPublic);
        if (f == null) return null;
        return f.GetValue(ch) as Delegate;
    }

    static MissionsConfig MakeConfig(params MissionChallenge[] list)
    {
        var cfg = new MissionsConfig();
        cfg.SetData(new List<MissionChallenge>(list), null, null);
        return cfg;
    }

    // ============================================================ A `CleanUp()` 的主链
    static void SectionA_CleanUp()
    {
        MenuCheck.Section(_sink, "A `MissionsConfig.CleanUp()`：初始化过的才重置 / 摘光订阅 / 清闩");

        var a = new ProbeChallenge();
        var b = new ProbeChallenge();                 // **故意不初始化**
        int got = 0;
        Action<MissionChallengeUpdatePayload> handler = _ => { got++; };
        a.OnProgressUpdate += handler;
        b.OnProgressUpdate += handler;

        // 前提（硬断言，不是 `if`）
        Tr(!a.IsInitialized, "A0 前提：新挑战 `IsInitialized` = false");
        Tr(ProgressDelegate(a) != null, "A0 前提：订阅挂上去了（私有委托字段非 null）");

        a.Initialize();
        C(a.initCount, 1, "A1 前提：`Initialize()` 走了一次 `OnInitialize`（一次性闩）");

        var cfg = MakeConfig(a, b);
        cfg.CleanUp();

        C(a.resetCount, 1, "A2 已初始化的那条：`CleanUp` 调了一次 Slot 8 `OnReset()`");
        Tr(!a.IsInitialized, "A3 已初始化的那条：`initialized(+0x50)` 被清成 false");
        C(b.resetCount, 0, "A4 未初始化的那条：**一次都没碰**（原版 `.c` 的 `if (initialized == 0) continue;`）");
        Tr(b.IsInitialized == false, "A4b 未初始化的那条：闩仍是 false（没被顺手改）");

        var dA = ProgressDelegate(a);
        Tr(dA != null, "A5 已初始化的那条：`+0x48` 被换成**空 lambda**（原版是赋值空方法，⛔ 不是 null）");
        C(dA != null ? dA.GetInvocationList().Length : -1, 1,
          "A5b 只剩一个目标（原订阅者被摘光；`+=`/`-=` 系做不到「不知道是谁也能全摘」）");
        C(dA != null ? dA.Method.Name : "<null>", "EmptyProgressUpdateBody",
          "A5c 剩下的那一个**就是那个空方法**（原版 `<>c__9__30_0`，RVA 0x4B33B0 = `ret 0`）");

        var dB = ProgressDelegate(b);
        C(dB != null ? dB.GetInvocationList().Length : -1, 1,
          "A6 未初始化的那条：订阅**原封不动**（`CleanUp` 只碰 initialized == true 的那些）");

        // 再跑一遍：闩已经清了 ⇒ 一条都不该再被重置
        cfg.CleanUp();
        C(a.resetCount, 1, "A7 第二次 `CleanUp`：闩已是 false ⇒ **不重复重置**（灭自证：删掉那个 `continue` 这条红）");
    }

    // ============================================================ B `CleanUp()` 的边界
    static void SectionB_CleanUp边界()
    {
        MenuCheck.Section(_sink, "B `MissionsConfig.CleanUp()`：null 表 / null 元素 / 空表");

        var cfgNull = new MissionsConfig();
        Tr(cfgNull.Challenges == null, "B0 前提：新实例的 `challenges` 是 null（原版 `.ctor` 只 new `configs`）");
        bool threw = false;
        try { cfgNull.CleanUp(); } catch (Exception) { threw = true; }
        C(threw, false, "B1 `challenges == null` ⇒ 我们**出声 + 返回**（原版这一跳是 NRE；全工程口径不把崩溃当复刻）");

        var cfgEmpty = MakeConfig();
        threw = false;
        try { cfgEmpty.CleanUp(); } catch (Exception) { threw = true; }
        C(threw, false, "B2 空表 ⇒ 什么都不做、不抛");

        var withNull = MakeConfig(null, new ProbeChallenge());
        threw = false;
        try { withNull.CleanUp(); } catch (Exception) { threw = true; }
        C(threw, false, "B3 表里混一个 null 元素 ⇒ 跳过那一条、不抛（原版 NRE；我们出声）");

        var cfg = new MissionsConfig();
        C(cfg.ConfigKey, "MissionsConfig", "B4 `IConfigData.ConfigKey` = `\"MissionsConfig\"`（ILSpy `get_ConfigKey` 逐字）");
        Tr(cfg.Configs != null && cfg.Configs.Length == 0, "B5 前提：`.ctor` 把 `configs` 建成**空数组**（原版 `.ctor` 唯一一句）");
        C(cfg.TutorialMissionId, null, "B6 `tutorialMissionRef` 为 null ⇒ `TutorialMissionId` 给 null（原版 NRE，我们出声）");
        C(cfg.DailyLogin, null, "B7 `dailyLoginRef` 为 null ⇒ `DailyLogin` 给 null（原版 NRE）");
    }

    // ============================================================ C 网格建树
    /// <summary>假的 mission 源：只提供原版那一支读的两格（`GetBaseSave()` / `UseSmallContainer`）+ 容器提供者。</summary>
    class FakeSource : MissionsTab.IGridMissionSource
    {
        public MissionPersistence save;
        public bool useSmall;
        public readonly List<bool> log = new List<bool>();

        public IUiContainer GetContainer() { return null; }
        public void ReleaseContainer() { }
        public MissionPersistence GetBaseSave() { return save; }
        public bool UseSmallContainer
        {
            get { return useSmall; }
            set { useSmall = value; log.Add(value); }
        }
    }

    static MissionPersistence SaveWith(int challengeCount)
    {
        var s = new MissionPersistence();
        s.challengeProgress = new MissionChallengeProgress[challengeCount];
        for (int i = 0; i < challengeCount; i++) s.challengeProgress[i] = new MissionChallengeProgress();
        return s;
    }

    static FakeSource Miss(int challengeCount)
    {
        return new FakeSource { save = SaveWith(challengeCount) };
    }

    static string Log(FakeSource m)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < m.log.Count; i++) { if (i > 0) sb.Append(','); sb.Append(m.log[i] ? "T" : "F"); }
        return sb.ToString();
    }

    static void SectionC_网格建树()
    {
        MenuCheck.Section(_sink, "C `MissionsTab.InstantiateGridMissionsCore`：按**挑战进度**建格 + 列数开关按全局序号");

        // C1/C2：2 条 mission × 2 条进度 ⇒ columns = 2（`1/2 条 ⇒ 2 列`）
        var h1 = new ContainerHolder(0, null);
        var m1 = Miss(2); var m2 = Miss(2);
        MissionsTab.InstantiateGridMissionsCore(h1, new List<MissionsTab.IGridMissionSource> { m1, m2 });
        C(h1.GetContainers().Count, 4,
          "C1 建格数 = **4**（按 `challengeProgress` 逐条建，不是按 mission 建 ⇒ 改回 2 就是复刻错了）");
        C(Log(m1) + "|" + Log(m2), "F,F|T,T",
          "C2 `UseSmallContainer` 按**全局**序号写：`columns(2) <= 0/1/2/3` ⇒ `F,F,T,T`（⛔ 不是每个 mission 重置）");

        // C3：3 条 mission × 1 条进度 ⇒ columns = 1
        var h2 = new ContainerHolder(0, null);
        var a1 = Miss(1); var a2 = Miss(1); var a3 = Miss(1);
        MissionsTab.InstantiateGridMissionsCore(h2, new List<MissionsTab.IGridMissionSource> { a1, a2, a3 });
        C(Log(a1) + "|" + Log(a2) + "|" + Log(a3), "F|T|T",
          "C3 `3 条 ⇒ 1 列` ⇒ `1 <= 全局序号` ⇒ `F,T,T`（判据 `.c:53-67` 的 `iVar2` 分支）");

        // C4：4 条 ⇒ 0 列 ⇒ 那个开关恒真
        var h3 = new ContainerHolder(0, null);
        var b1 = Miss(1); var b2 = Miss(1); var b3 = Miss(1); var b4 = Miss(1);
        MissionsTab.InstantiateGridMissionsCore(h3, new List<MissionsTab.IGridMissionSource> { b1, b2, b3, b4 });
        C(Log(b1) + "|" + Log(b4), "T|T", "C4 `4 条 ⇒ 0 列`（原版那一支什么都不写 ⇒ `0 <= 序号` 恒真）");

        // C5：`maxAmount` 闸
        var h4 = new ContainerHolder(2, null);
        var c1 = Miss(2); var c2 = Miss(2);
        MissionsTab.InstantiateGridMissionsCore(h4, new List<MissionsTab.IGridMissionSource> { c1, c2 });
        C(h4.GetContainers().Count, 2, "C5 `ContainerHolder.maxAmount = 2` ⇒ 只建 **2** 格（满员后 `TryAdd` 返回 false）");

        // C6：表里混 null mission
        var h5 = new ContainerHolder(0, null);
        var d1 = Miss(1); var d2 = Miss(1);
        bool threw = false;
        try
        {
            MissionsTab.InstantiateGridMissionsCore(h5, new List<MissionsTab.IGridMissionSource> { null, d1, d2 });
        }
        catch (Exception) { threw = true; }
        C(threw, false, "C6 mission 表里有 null ⇒ 跳过那一条、不抛（原版 NRE；我们出声）");
        C(h5.GetContainers().Count, 2, "C6b 剩下两条照建 ⇒ 2 格");

        // C7：`challengeProgress == null` ⇒ 该 mission 一格都不产
        var h6 = new ContainerHolder(0, null);
        var e1 = new FakeSource { save = new MissionPersistence() };     // 没有 challengeProgress
        MissionsTab.InstantiateGridMissionsCore(h6, new List<MissionsTab.IGridMissionSource> { e1 });
        C(h6.GetContainers().Count, 0, "C7 `challengeProgress` 为 null ⇒ 该任务不产格（`.c`：`+0x48` 为 0 就跳）");
        C(e1.log.Count, 0, "C7b 也不写 `UseSmallContainer`（那个开关在**每条进度**的循环里写）");

        // C8：null 参数
        threw = false;
        try { MissionsTab.InstantiateGridMissionsCore(null, new List<MissionsTab.IGridMissionSource>()); }
        catch (Exception) { threw = true; }
        C(threw, false, "C8 `holder == null` ⇒ 出声返回、不抛");
        threw = false;
        try { MissionsTab.InstantiateGridMissionsCore(new ContainerHolder(0, null), null); }
        catch (Exception) { threw = true; }
        C(threw, false, "C9 `missions == null` ⇒ 出声返回、不抛（原版 `ToList()` 会抛）");
    }

    // ============================================================ D 三档 state
    static void SectionD_三档state()
    {
        MenuCheck.Section(_sink, "D `ParentEventWindow.MilestoneState`：`(currentScore < score) ? 0 : ((i < currentMilestone) + 1)`");

        C(ParentEventWindow.MilestoneState(5, 10, 0, 0), GenericScoringMilestoneState.Locked,
          "D1 分数没到 ⇒ `Locked`(0)");
        C(ParentEventWindow.MilestoneState(9, 10, 5, 9), GenericScoringMilestoneState.Locked,
          "D1b 分数没到 ⇒ `Locked`，**不管** `i` 与 `currentMilestone` 的关系（分数优先）");
        C(ParentEventWindow.MilestoneState(10, 10, 0, 0), GenericScoringMilestoneState.Unlocked,
          "D2 **边界**：`currentScore == score` ⇒ 算到了（`<` 不是 `<=`）⇒ `Unlocked`");
        C(ParentEventWindow.MilestoneState(10, 10, 0, 1), GenericScoringMilestoneState.Collected,
          "D3 到了分且 `0 < currentMilestone(1)` ⇒ `Collected`(2)（已领）");
        C(ParentEventWindow.MilestoneState(10, 10, 1, 1), GenericScoringMilestoneState.Unlocked,
          "D4 **边界**：`i == currentMilestone` ⇒ 还没领 ⇒ `Unlocked`(1)（`<` 不是 `<=`）");
        C(ParentEventWindow.MilestoneState(10, 10, 2, 1), GenericScoringMilestoneState.Unlocked,
          "D5 `i > currentMilestone` ⇒ 也是 `Unlocked`");
        C(ParentEventWindow.MilestoneState(0, 0, 0, 0), GenericScoringMilestoneState.Unlocked,
          "D6 全 0 档：门槛 0 分、没领过 ⇒ `Unlocked`（可领）");
    }

    // ============================================================ E 结构
    static void SectionE_结构()
    {
        MenuCheck.Section(_sink, "E 结构：类型与签名（原版逐一对应）");

        var tCfg = typeof(MissionsConfig);
        C(tCfg.Name, "MissionsConfig", "E1 类型名逐字");
        Tr(typeof(IConfigData).IsAssignableFrom(tCfg), "E2 `MissionsConfig : IConfigData`（原版 Slot 6 = `CleanUp`）");

        var nested = tCfg.GetNestedType("MissionConfig", BindingFlags.Public | BindingFlags.NonPublic);
        Tr(nested != null, "E3 嵌套类 `MissionsConfig.MissionConfig` 在（原版 TypeDefIndex 1487）");
        // 🔴 **2026-10-30（`B100_F6`）就地订正（铁律 5）**：`type` 是 **`internal`**（`Shell/MissionsConfig.cs:65`
        //   `internal MissionType type;`）⇒ 默认 BindFlags（只看 public）取不到它。`maxAmount` 是 public ⇒ 两条要分开取。
        const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Tr(nested != null && nested.GetField("type", AnyInstance) != null && nested.GetField("maxAmount", AnyInstance) != null,
           "E3b 它有两格 `type` / `maxAmount`");
        object nc = null;
        try { nc = nested != null ? Activator.CreateInstance(nested) : null; } catch (Exception) { }
        Tr(nc != null && (int)nested.GetField("maxAmount").GetValue(nc) == -1,
           "E3c `maxAmount` 出厂 = **-1**（`.ctor` 的 `.c`：`*(undefined4*)(param_1+0x14) = 0xffffffff`）");

        var tCl = typeof(ChallengeList);
        Tr(tCl.GetField("id") != null && tCl.GetField("challenges") != null,
           "E4 `ChallengeList` 两格 `id` / `challenges` 在（原版 `+0x18` / `+0x20`）");
        Tr(tCl.GetMethod("GetID", Type.EmptyTypes) != null, "E4b `GetID()` 在（原版 Slot 5）");

        C(typeof(MissionsTab.IGridMissionSource).GetInterfaces().Length, 1,
          "E5 `IGridMissionSource` 继承**恰好一个**接口（`IContainerProvider`，原版 `IMission : … IContainerProvider`）");
        Tr(typeof(ContainerHolder).GetMethod("TryAdd", new[] { typeof(IContainerProvider), typeof(IUiContainer).MakeByRefType() }) != null,
           "E5b `ContainerHolder.TryAdd(IContainerProvider, out IUiContainer)` 在（网格建树那一跳）");

        var mi = typeof(ParentEventWindow).GetMethod("InitializeMilestones", BindingFlags.Instance | BindingFlags.Public);
        Tr(mi != null && mi.GetParameters().Length == 3, "E6 `ParentEventWindow.InitializeMilestones` 三参（currentScore / currentMilestone / milestones）");
        var ap = typeof(ParentEventWindow).GetMethod("ApplyOnGoingTeams", BindingFlags.Instance | BindingFlags.Public);
        Tr(ap != null && ap.GetParameters().Length == 5, "E6b `ParentEventWindow.ApplyOnGoingTeams` 五参（nameA / captainA / nameB / captainB / mySide）");
        Tr(typeof(ParentEventWindow).GetProperty("LastMilestoneProgress") != null,
           "E6c 归一化进度的读口 `LastMilestoneProgress` 在（= 原版 `Slider.normalizedValue` 那一格）");
    }
}
