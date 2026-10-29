// B97W7_Assert.cs — 执行代理 W7（第九十七会话 · 波 1 补位）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `日常_奖励:680` `RewardService.CollectSingleReward(RewardInfo)` 的 **④ 联盟奖杯那一支**：
//     `Shell/AllianceTrophyModel.MarkCollected`（三格硬赋）＋ `Shell/Inventory.cs` 里那支分派（④ 与原版一样是**互斥两支**）。
//
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/报告/B97_W7.md` §二 / §四；
//   原始二进制 = `d:/2/tools/decomp_full/RewardService__CollectSingleReward.c:110-127`
//   （`FirstOrDefault(trophies, x => x.Data.GetID() == 奖励.GetID())` 命中 ⇒ `save+0x38=1 · save+0x3c=1 · save+0x40=quantity`）。
//
// ============================ 怎么用（宿主）============================
//   `B97W7Assert.Run(); _sink.Pass += B97W7Assert.Pass; _sink.Fail += B97W7Assert.Fail;`
//   · 推荐宿主 = `Editor/ShellScene.cs`（`ShellScene.Run`）—— 插在**合计行之前**、可以直接追加到
//     `MenuCheck.RaycastFilterAsserts(_sink); … B97ProfileAssert.Run(_sink);` 那一行末尾（行数 0 变化）。
//   · 本文件**不建 GameObject、不建资产**；改的全局静态表只有 `AllianceTrophyModel` 的 `_defs` / `_states`，
//     两条都在 `finally` 里 `ResetForTest()` 还原。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A 段：把 `MarkCollected` 里 `st.GroupScore = 1` 改成 `+= 1` ⇒ A2 红（预置脏值与 0 值各跑一次）。
//   · A/B 段：把 `st.SavedTier = quantity` 改成 `st.SavedTier = 1`（「三格都写 1」那种错法）⇒ A4 红。
//   · B 段：把三格中任意一格写成 `Math.Max(旧值, 新值)` ⇒ B1/B2 红（预置值比原版写的大 ⇒ 停在旧值）。
//   · C 段：把 `MarkCollected` 的「未命中返回 false、不写」改成「未命中就现建一条」⇒ C1/C2 红。
//   · D 段：把 `CollectSingleReward` 的 ④ 支删掉（只留 `CollectItemRewardById`）⇒ D1..D3 红（三格恒 0）。
//     把 `IsAllianceTrophyId` 改成恒 `false` ⇒ 同样 D1..D3 红。
//   · D4：把 ④ 支改成「不管什么 id 都调 `MarkCollected`」⇒ D4 红（非奖杯 id 也把三格改了）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 W7：联盟奖杯领取（三格硬赋）＋它在 `CollectSingleReward` 里的接线。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W7Assert
{
    static readonly CheckSink _sink = new CheckSink("[W7] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>夹具奖杯 id（只在本文件里用；`SetDefsForTest` 注入、`ResetForTest` 撤掉）。</summary>
    const string TrophyId = "B97W7_Trophy";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_W7 联盟奖杯领取：`MarkCollected` 三格硬赋 + `CollectSingleReward` ④ 支接线"
                              + "（判据 `RewardService__CollectSingleReward.c:110-127`）");
        try
        {
            SectionA_三格硬赋();
            SectionB_硬赋不是累加();
            SectionC_未命中不写();
            SectionD_CollectSingleReward接线();
        }
        finally
        {
            AllianceTrophyModel.ResetForTest();
        }
        Debug.Log(_sink.P + "B97W7 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    static AllianceTrophyDef MakeDef()
    {
        return new AllianceTrophyDef
        {
            Id = TrophyId,
            MilestoneTargets = new[] { 10, 20 },
            HideIfNotAchieved = false,
        };
    }

    static AllianceTrophyState StateNow()
    {
        return AllianceTrophyModel.StateOf(AllianceTrophyModel.Find(TrophyId));
    }

    // ============================================================ A 三格硬赋（原版 `.c:120-126`）
    static void SectionA_三格硬赋()
    {
        MenuCheck.Section(_sink, "A `MarkCollected(id, 3)` ⇒ `GroupScore=1` · `PlayerScore=1` · `SavedTier=3`"
                              + "（原版 `save+0x38=1` / `+0x3c=1` / `+0x40=quantity`）");

        AllianceTrophyModel.ResetForTest();
        AllianceTrophyModel.SetDefsForTest(new[] { MakeDef() });

        // 🔴 前提断言写在前面（⛔ 不用 `if (前提) Check(结论)` —— 前提不成立时静默跳过）
        Tr(AllianceTrophyModel.Find(TrophyId) != null,
           "A0 **前提断言**：注入定义后 `Find` 取回它（不成立 ⇒ A1..A4 **没跑**，本文件不静默跳过）");
        if (AllianceTrophyModel.Find(TrophyId) == null) return;

        var st0 = StateNow();
        C(st0.GroupScore, 0, "A0b 前提：注入定义后三格起点都是 0（`StateOf` 没有记录 ⇒ 默认值）");
        C(st0.PlayerScore, 0, "A0c 前提：`PlayerScore` 起点 0（新字段，别拿别的字段顶）");

        bool wrote = AllianceTrophyModel.MarkCollected(TrophyId, 3);
        var st = StateNow();
        Tr(wrote, "A1 命中定义 ⇒ `MarkCollected` 返回 true（原版 `FirstOrDefault` 命中那一支）");
        C(st.GroupScore, 1, "A2 ★ `GroupScore` = **1**（原版 `.c:120` 写死 1；⛔ 不是 quantity、不是 +=）");
        C(st.PlayerScore, 1, "A3 ★ `PlayerScore` = **1**（原版 `.c:122` 写死 1 —— 新字段真的被写了）");
        C(st.SavedTier, 3, "A4 ★ `SavedTier` = **quantity(3)**（原版 `.c:126` = 奖励数量，⛔ 不是 `CurrentTier`、不是写死 1）");
    }

    // ============================================================ B 硬赋 ≠ 累加 / 取 max
    static void SectionB_硬赋不是累加()
    {
        MenuCheck.Section(_sink, "B 先把三格塞脏再领一次：必须被**覆盖**（挡 `+=` / `Math.Max` / 「三格都写 1」三种错法）");

        AllianceTrophyModel.ResetForTest();
        AllianceTrophyModel.SetDefsForTest(new[] { MakeDef() });
        Tr(AllianceTrophyModel.Find(TrophyId) != null,
           "B0 **前提断言**：定义表已注入（不成立 ⇒ B1..B3 **没跑**）");
        if (AllianceTrophyModel.Find(TrophyId) == null) return;

        AllianceTrophyModel.SetScoreForTest(TrophyId, 9, 7);        // 脏值：GroupScore=9 · SavedTier=7
        var dirty = StateNow();
        C(dirty.GroupScore, 9, "B0b 前提：脏值确实落进去了（`SetScoreForTest` 没被改坏）");

        AllianceTrophyModel.MarkCollected(TrophyId, 3);
        var st = StateNow();
        C(st.GroupScore, 1, "B1 ★ 灭自证：预置 9 ⇒ 领取后变 **1**（`+=` 实现得 10、`Math.Max` 实现停在 9 ⇒ 红）");
        C(st.SavedTier, 3, "B2 ★ 灭自证：预置 7、quantity=3 ⇒ 变 **3**（`Math.Max` 实现停在 7、`+=` 实现得 10 ⇒ 红）");
        C(st.PlayerScore, 1, "B3 `PlayerScore` 同样被覆盖成 1");
    }

    // ============================================================ C 未命中 ⇒ 什么都不写
    static void SectionC_未命中不写()
    {
        MenuCheck.Section(_sink, "C 未命中定义表 ⇒ 返回 false、三格**一格都不动**（原版 `FirstOrDefault` 未命中那一支）");

        AllianceTrophyModel.ResetForTest();
        AllianceTrophyModel.SetDefsForTest(new[] { MakeDef() });
        Tr(AllianceTrophyModel.Find(TrophyId) != null,
           "C0 **前提断言**：定义表已注入（不成立 ⇒ C1..C4 **没跑**）");
        if (AllianceTrophyModel.Find(TrophyId) == null) return;

        AllianceTrophyModel.MarkCollected(TrophyId, 4);             // 先写一次，建立「已领」基线
        var before = StateNow();
        C(before.SavedTier, 4, "C0b 前提：基线已建立（SavedTier=4）");

        bool wroteUnknown = AllianceTrophyModel.MarkCollected("B97W7_NoSuchTrophy", 9);
        Tr(!wroteUnknown, "C1 ★ 未命中 ⇒ 返回 **false**（原版找不到那件奖杯：不写、也不报错）");

        var after = StateNow();
        C(after.GroupScore, before.GroupScore, "C2 未命中 ⇒ 已有奖杯的 `GroupScore` **不变**");
        C(after.PlayerScore, before.PlayerScore, "C3 未命中 ⇒ `PlayerScore` **不变**");
        C(after.SavedTier, before.SavedTier, "C4 未命中 ⇒ `SavedTier` **不变**（⛔ 没被空 id 顺手写一遍）");
    }

    // ============================================================ D 真接线（走 `CollectSingleReward` 的 ④ 支）
    static void SectionD_CollectSingleReward接线()
    {
        MenuCheck.Section(_sink, "D `RewardService.CollectSingleReward(RewardInfo)` 的 ④ 支："
                              + "联盟奖杯 id ⇒ 写三格；非联盟奖杯 id ⇒ 一格不动（原版这两支互斥）");

        AllianceTrophyModel.ResetForTest();
        AllianceTrophyModel.SetDefsForTest(new[] { MakeDef() });
        Tr(AllianceTrophyModel.Find(TrophyId) != null,
           "D0 **前提断言**：定义表已注入（不成立 ⇒ D1..D5 **没跑**）");
        if (AllianceTrophyModel.Find(TrophyId) == null) return;

        RewardService.CollectSingleReward(new RewardInfo(TrophyId, 4));
        var st = StateNow();
        C(st.GroupScore, 1, "D1 ★ 工厂口 `CollectSingleReward` 走了 ④ 联盟奖杯支：`GroupScore` = 1"
                          + "（分支没命中 ⇒ 停在 0 ⇒ 红；挡「只建了 `MarkCollected` 却没人调」）");
        C(st.PlayerScore, 1, "D2 ★ `PlayerScore` = 1（同一支写进去的）");
        C(st.SavedTier, 4, "D3 ★ `SavedTier` = reward.Quantity(4) —— 数量是从奖励里**穿过来的**，⛔ 不是常量");

        RewardService.CollectSingleReward(new RewardInfo("B97W7_NotATrophy", 5));
        var st2 = StateNow();
        C(st2.SavedTier, 4, "D4 ★ 灭自证：非联盟奖杯 id ⇒ 那件奖杯的三格**不变**（④ 支被误改成「什么 id 都写」⇒ 红）");
        C(st2.GroupScore, 1, "D5 非联盟奖杯 id 也不改 `GroupScore`");
    }
}
