// B102MISS_Assert.cs — 执行代理 `B102_MISS`（2026-10-30 · 日常线）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `日常/奖励:526` · `MissionsTab.InstantiateGridMissions(IEnumerable<IMission>)` 的**列数分母**。
//   判据（施工单 = `资料/完全复刻/判据/B102_R2_日常奖励B类五条.md` §二）：
//     · 原版 `.c:53-54`（`decomp_full/MissionsTab__InstantiateGridMissions.c`）
//       `n = list.SelectMany(x => x.GetBaseSave().challengeProgress).Count()` ——
//       🔴 那是 **`SelectMany` 选择器**（ILSpy `MissionsTab.cs:825` 直接印出 `Enumerable.SelectMany`），⛔ 不是 `Where` 谓词。
//     · ⇒ `n` = **所有 mission 的挑战进度条数之和 = 网格格数**，⛔ 不是 mission 个数。
//     · 列数（`.c:56-67`）：`n ∈ {1,2} ⇒ 2` · `n == 3 ⇒ 1` · 其它（含 0 与 ≥4）⇒ 0。
//   被测落点：`CardPresentation/Shell/MissionsTab.cs`
//     · `GridMissionCellCount.Count(IReadOnlyList<IGridMissionSource>)`（文件尾新增的纯函数）
//     · `GridMissionLayoutLogic.ColumnCount(int)` / `WritesSwitch(int,int)`（既有，未改）
//     · `MissionsTab.InstantiateGridMissionsCore(...)`（接线后的端到端形状）
//
// ============================ 怎么用（宿主接线一行）============================
//   宿主 = `Editor/ShellScene.cs` 的 `ShellScene.Run`（与 `B98K3Assert` 同一个 `_sink`）。
//   在**合计行之前**、**同一行追加**（⇒ 行数零变化，符合既有代接线的做法）：
//     B102MISSAssert.Run(); _sink.Pass += B102MISSAssert.Pass; _sink.Fail += B102MISSAssert.Fail;
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · B1b / B2b / B3b / B4b：把 `MissionsTab.cs` 的 `ColumnCount(GridMissionCellCount.Count(list))`
//     退回成 `ColumnCount(list.Count)` ⇒ **必红**（这些夹具的 mission 数与格数刻意不同）。
//   · D1：把 `GridMissionCellCount.Count` 改成 `return missions.Count;` ⇒ 红。
//   · C1/C2/C3/C4：给 Count 的任何一跳去掉 null 判断（照原版直接 NRE）⇒ 抛异常 ⇒ 红。
//   · C6b：把 `ColumnCount` 的「其它 ⇒ 0」改成「⇒ 2」⇒ 红。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B102_MISS`：账 `日常/奖励:526`「列数分母 = 总进度条数」的落地断言。
/// <para>⛔ 期望值全是**字面量**（`2` / `1` / `0` / `F,T,T`），**不从被测实现里读回来**；
/// `B2` / `D2` 那两条「旧口径会得多少」也是字面量 —— 它们是**灭自证锁**，不是被测值。</para></summary>
public static class B102MISSAssert
{
    static readonly CheckSink _sink = new CheckSink("[B102MISS] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B102_MISS 账 `日常:526` 列数分母：n = Σ challengeProgress.Length（SelectMany），⛔ 不是 mission 个数");
        SectionA_前提();
        SectionB_判别式();
        SectionC_边界();
        SectionD_灭自证();
    }

    // ============================================================ 夹具
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

    static FakeSource Miss(int challengeCount) { return new FakeSource { save = SaveWith(challengeCount) }; }

    /// <summary>三个实参以下都够用；⛔ 名字避开 `List`（那是 `System.Collections.Generic.List&lt;T&gt;`）。</summary>
    static List<MissionsTab.IGridMissionSource> SrcList(params MissionsTab.IGridMissionSource[] items)
    {
        return new List<MissionsTab.IGridMissionSource>(items);
    }

    static string Log(FakeSource m)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < m.log.Count; i++) { if (i > 0) sb.Append(','); sb.Append(m.log[i] ? "T" : "F"); }
        return sb.ToString();
    }

    // ============================================================ A 前提（硬断言，⛔ 不用 `if (x != null) Check(...)`）
    static void SectionA_前提()
    {
        MenuCheck.Section(_sink, "A 前提：夹具 + 纯函数本身（列数三档）");

        var twoByOne = SrcList(Miss(1), Miss(1));              // 2 个 mission，各 1 条进度
        C(GridMissionCellCount.Count(twoByOne), 2, "A0a 前提：2 mission × 1 条进度 ⇒ 格数 = **2**（字面量）");

        var oneByTwo = SrcList(Miss(2));                       // 1 个 mission，2 条进度
        C(GridMissionCellCount.Count(oneByTwo), 2, "A0b 前提：1 mission × 2 条进度 ⇒ 格数 = **2**");

        // 前提：`ColumnCount` 三档（既有纯函数，本批未改）
        C(MissionsTab.GridMissionLayoutLogic.ColumnCount(1), 2, "A0c 前提：`ColumnCount(1)` = 2");
        C(MissionsTab.GridMissionLayoutLogic.ColumnCount(2), 2, "A0d 前提：`ColumnCount(2)` = 2");
        C(MissionsTab.GridMissionLayoutLogic.ColumnCount(3), 1, "A0e 前提：`ColumnCount(3)` = 1");
        C(MissionsTab.GridMissionLayoutLogic.ColumnCount(4), 0, "A0f 前提：`ColumnCount(4)` = 0（其它 ⇒ 0）");

        // 端到端前提：2 格 / 2 列 ⇒ 全局序号 0/1 都 < 2 ⇒ 开关 F,F
        var h = new ContainerHolder(0, null);
        var m1 = Miss(1); var m2 = Miss(1);
        MissionsTab.InstantiateGridMissionsCore(h, SrcList(m1, m2));
        C(h.GetContainers().Count, 2, "A0g 前提：该夹具真的建了 **2** 格");
        C(Log(m1) + "|" + Log(m2), "F|F", "A0h 前提：2 格 / 2 列 ⇒ `UseSmallContainer` = `F|F`");

        // ⚠️ 这一格**不是**判别式（口径相同的场合）：2 个 1 条 vs 1 个 2 条，两种数法都得 2
        C(MissionsTab.GridMissionLayoutLogic.ColumnCount(twoByOne.Count), 2,
          "A0i 提示：`ColumnCount(mission 个数 = 2)` 也是 2 ⇒ 这组夹具**分不开**两种口径（见 B 段）");
    }

    // ============================================================ B ★ 判别式
    static void SectionB_判别式()
    {
        MenuCheck.Section(_sink, "B ★ 判别式：1 个 mission 带 3 条进度 ⇒ n=3 ⇒ **1 列**（按 mission 数会得 2 列）");

        var oneByThree = SrcList(Miss(3));
        C(GridMissionCellCount.Count(oneByThree), 3, "B1 格数 = **3**（1 个 mission 的 3 条进度全算）");
        C(MissionsTab.GridMissionLayoutLogic.ColumnCount(GridMissionCellCount.Count(oneByThree)), 1,
          "B1b ★ **1 列**（n=3 ⇒ `.c:56-67` 的第三档；退回 `ColumnCount(list.Count=1)` 会得 2 ⇒ 本条红）");
        C(MissionsTab.GridMissionLayoutLogic.ColumnCount(oneByThree.Count), 2,
          "B2 灭自证锁：**旧口径** `ColumnCount(mission 个数 = 1)` = 2 —— 与 B1b 的 1 不同 ⇒ 两种口径确实分开");
        Tr(MissionsTab.GridMissionLayoutLogic.ColumnCount(oneByThree.Count)
           != MissionsTab.GridMissionLayoutLogic.ColumnCount(GridMissionCellCount.Count(oneByThree)),
           "B2b ★ 判别不等式：两个口径在这组夹具上**不相等**（相等则本条与本段失效，说明夹具挑错了）");

        // 端到端：1 mission × 3 条 ⇒ 1 列 ⇒ `1 <= 0/1/2` ⇒ F,T,T（旧实现 2 列 ⇒ F,F,T）
        var h1 = new ContainerHolder(0, null);
        var m = Miss(3);
        MissionsTab.InstantiateGridMissionsCore(h1, SrcList(m));
        C(h1.GetContainers().Count, 3, "B3a 端到端：建了 **3** 格");
        C(Log(m), "F,T,T", "B3b ★ 端到端：`UseSmallContainer` = `F,T,T`（1 列；旧实现会给 `F,F,T`）");

        // 规格 C8 的夹具：2 个 mission，第一个 3 条、第二个 0 条 ⇒ n=3 ⇒ 1 列
        var h2 = new ContainerHolder(0, null);
        var a = Miss(3); var b = Miss(0);
        MissionsTab.InstantiateGridMissionsCore(h2, SrcList(a, b));
        C(h2.GetContainers().Count, 3, "B4a 端到端：2 mission（3+0 条）⇒ 建 **3** 格");
        C(Log(a) + "|" + Log(b), "F,T,T|", "B4b ★ 端到端：`F,T,T` 且第二个 mission 一格不产（旧实现 2 列 ⇒ `F,F,T|`）");
    }

    // ============================================================ C 边界
    static void SectionC_边界()
    {
        MenuCheck.Section(_sink, "C 边界：null / 空表 / 4 条（其它 ⇒ 0 列）");

        C(GridMissionCellCount.Count(null), 0,
          "C1 `missions == null` ⇒ 0（原版 `ToList()` 这一跳会抛；我们在 Count 里返回 0、调用方另行出声）");

        bool threw = false;
        try { GridMissionCellCount.Count(SrcList(null, Miss(2), null)); } catch (Exception) { threw = true; }
        C(threw, false, "C2 表里混 null mission ⇒ 跳过、不抛（原版选择器 `throw`）");
        C(GridMissionCellCount.Count(SrcList(null, Miss(2), null)), 2, "C2b 剩下那条的 2 条进度照算 ⇒ 2");

        var noSave = new FakeSource { save = null };
        threw = false;
        try { GridMissionCellCount.Count(SrcList(noSave, Miss(1))); } catch (Exception) { threw = true; }
        C(threw, false, "C3 `GetBaseSave()` 为 null ⇒ 跳过、不抛（原版选择器 `throw`）");
        C(GridMissionCellCount.Count(SrcList(noSave, Miss(1))), 1, "C3b 只剩那条的 1 条进度 ⇒ 1");

        var noProgress = new FakeSource { save = new MissionPersistence() };   // challengeProgress 为 null
        threw = false;
        try { GridMissionCellCount.Count(SrcList(noProgress)); } catch (Exception) { threw = true; }
        C(threw, false, "C4 `challengeProgress` 为 null ⇒ 不计、不抛（原版 `SelectMany` 收 null 序列会抛）");
        C(GridMissionCellCount.Count(SrcList(noProgress)), 0, "C4b 格数 = **0**");

        C(GridMissionCellCount.Count(SrcList()), 0, "C5 空表 ⇒ 0");
        C(MissionsTab.GridMissionLayoutLogic.ColumnCount(GridMissionCellCount.Count(SrcList())), 0,
          "C5b 0 格 ⇒ `ColumnCount(0)` = 0（原版那一支什么都不写）");

        // 4 条 ⇒ 其它 ⇒ 0 列 ⇒ 开关恒真
        var h = new ContainerHolder(0, null);
        var q = Miss(4);
        MissionsTab.InstantiateGridMissionsCore(h, SrcList(q));
        C(h.GetContainers().Count, 4, "C6a 4 条进度 ⇒ 建 **4** 格");
        C(Log(q), "T,T,T,T", "C6b 4 ⇒ **0 列** ⇒ `0 <= 序号` 恒真 ⇒ `T,T,T,T`（改成「⇒ 2」这条红）");

        // 满员闸不受列数口径影响（原版 `TryAdd` 满员跳过）
        var h2 = new ContainerHolder(2, null);
        var r1 = Miss(2); var r2 = Miss(2);                    // 合计 4 条 ⇒ 0 列
        MissionsTab.InstantiateGridMissionsCore(h2, SrcList(r1, r2));
        C(h2.GetContainers().Count, 2, "C7 `maxAmount = 2` ⇒ 只建 **2** 格（与该批列数口径无关）");
    }

    // ============================================================ D 灭自证
    static void SectionD_灭自证()
    {
        MenuCheck.Section(_sink, "D 灭自证：把实现退回「数 mission 个数」时，哪几条会红");

        // D1：`Count` 若写成 `return missions.Count;`（= 旧口径换了个名字）⇒ 这组夹具上 3 ≠ 1
        var oneByThree = SrcList(Miss(3));
        Tr(GridMissionCellCount.Count(oneByThree) != oneByThree.Count,
           "D1 ★ `Count(1 mission × 3 条) = 3` ≠ `missions.Count = 1`（实现被改成数 mission ⇒ 本条红）");

        // D2：2 个 mission、**0 条进度** —— 旧口径 2 列，新口径 0 格（什么都不画）
        var zero = SrcList(Miss(0), Miss(0));
        C(GridMissionCellCount.Count(zero), 0, "D2 2 mission × 0 条 ⇒ 格数 **0**（旧口径 `ColumnCount(2)` = 2）");
        C(MissionsTab.GridMissionLayoutLogic.ColumnCount(GridMissionCellCount.Count(zero)), 0,
          "D2b 0 格 ⇒ **0 列**（旧口径会得 2 列 —— 这一格就是「分母错了」的可观察后果）");

        // D3：格数 ≥ 3 的夹具一律把两种口径拉开（列数只能是 1 或 0，mission 数却可能 1/2）
        var twoMissionsThreeCells = SrcList(Miss(1), Miss(2));  // 格 3 / mission 2
        C(GridMissionCellCount.Count(twoMissionsThreeCells), 3, "D3 1+2 条 ⇒ 格数 **3**");
        C(MissionsTab.GridMissionLayoutLogic.ColumnCount(GridMissionCellCount.Count(twoMissionsThreeCells)), 1,
          "D3b 格数 3 ⇒ **1 列**（旧口径 `ColumnCount(2)` = 2 ⇒ 本条红）");

        // D4：口径相同的场合要**明说分不开**（防止后人拿它当判别式）
        var twoByOne = SrcList(Miss(1), Miss(1));
        Tr(MissionsTab.GridMissionLayoutLogic.ColumnCount(GridMissionCellCount.Count(twoByOne))
           == MissionsTab.GridMissionLayoutLogic.ColumnCount(twoByOne.Count),
           "D4 提示：2 mission × 1 条时两种口径都得 2 列 ⇒ ⛔ 别用这组当判别式（判别式见 B1b / B4b）");
    }
}
