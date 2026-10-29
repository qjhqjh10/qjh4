// B99G2_Assert.cs — 第九十九会话 · 波 3 · 执行代理 `B99_G2`（战役数据 · `Shell/CampaignData.cs` 的领奖状态机）
//
// ============================ 它盯的是什么 ============================
//  `CampaignData` 的**领奖状态机**原来有四处偏离原版（施工判据 → `资料/完全复刻/判据/B99_R4a_近似改原版值_卡牌界面.md` §2）：
//   ① `StateOf` 第一行写成 `_base[i] && _prem[i]` ⇒ **可重复节点**领过之后判 `AllCollected(30)`，
//      而原版 `GetNodeState` 的 **20/30 两支都被 `IsRepeatable` 覆盖成 40**（⇒ 可无限重复领）。
//   ② `Claim` 里有**两条原版没有的逐档拒绝**（`if (_prem[i]) …` / `if (_base[i]) …`）⇒ 把可重复节点的重复领挡死。
//   ③ 高级档顺手把**基础档**也标上（原版两个列表各写各的：基础档写 `persistence+0x30`、高级档写 `+0x38`）。
//   ④ 「战役完成」判成「47 格基础档全真」；原版是 `basicList.Contains(Last(GetCampaignNodes()))`。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · `d:/2/tools/decomp_full/CampaignNode__Collect.c:29-47` —— 闸**只有** `State ∈ {10, 40}`
//     （`if ((*(int *)(param_1 + 0x38) == 10) || (… == 0x28))`）；`uVar2 = 0x14; if (param_2 != 0) uVar2 = 0x1e;`（基础→20、高级→30）；
//     末段 `if (*(int *)(local_50 + 0x38) == 0) { …= 10; SetNodeStyle(); }`（把相邻 `locked` 置 10）。
//   · `d:/2/tools/decomp_full/CampaignTrack__GetNodeState.c:27-58` —— premList 命中 ⇒ 30、basicList 命中 ⇒ 20，
//     **两支都 `goto LAB_1806b9a30`**，那里 `if (*(char *)(lVar4 + 0x40) != '\0') uVar3 = 0x28;`（`IsRepeatable` ⇒ 40）；
//     10/0 两支**直接 return**（不受 repeatable 影响）。
//   · `d:/2/tools/decomp_full/Everguild.LiveOps.Campaign.__c__DisplayClass11_0___CollectRewards_g__OnCollect_2.c:69-90 / :140-165`
//     —— 基础档写 `persistence+0x30` 并**扣点**（`CampaignPointAmountUpdateSignal`）；高级档写 `persistence+0x38`、
//     **不动点数**、**不写基础档列表**。
//   · `d:/2/tools/decomp_full/Everguild.LiveOps.Campaign__get_IsComplete.c:29-31`
//     —— `basicList(=param_1+0x20 → +0x30).Contains(Enumerable.Last(nodes))`。
//   · `d:/2/tools/decomp_full/CampaignWindowTab__OnNodeClicked.c:30-60` —— 三个派生位：
//     `Claimable = (points >= cost) && (State==10 || State==40)` · `BaseCollected = (State==20 || State==30)` · `PremiumCollected = (State==30)`。
//   · 施工判据 → `资料/完全复刻/判据/B99_R4a_近似改原版值_卡牌界面.md` §2；本件报告 → `资料/完全复刻/报告/B99_G2.md`
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · B2：把 `Claim` 高级档那支改回 `if (!_base[i]) _base[i] = true;` ⇒ `ClaimedCount` 变 1 ⇒ B2 红。
//   · B5：把 `Claim` 里的 `MarkSuccessorsUnlocked(i)` 删掉、或 `IsUnlocked` 不读 `_sessionUnlocked`
//     ⇒ 只领高级档后后继**当场**退回 locked ⇒ B5 红（那正是「比原版更严」那条）。
//   · B6：`ResetForTest` 不重置 `_sessionUnlocked` ⇒ 上一局顶开的门漏到下一局 ⇒ B6 红。
//   · C6：把 `AllNodesClaimed` 改回「47 格 `_base` 全真」⇒ 只领了路径上 22 格时判 false ⇒ C6 红。
//   · D5：把 `Claim` 基础档那支改回 `if (_base[i]) { why = …; return false; }` ⇒ 第二次领返回 false ⇒ D5 红。
//   · D8：把 `StateOf` 第一行改回 `if (_base[i] && _prem[i]) return AllCollected;` ⇒ 可重复节点 40 → 30 ⇒ D8 红。
//   · A6：把 `Claimable` 那道闸放宽到 20/30 ⇒ 「先领高级再领基础」会成功 ⇒ A6 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的 `RuleEngineTest.Run()` —— 在**现有**某条 `Step(...)`
//  的**同一物理行**后面追加（零行数变化）：
//      B99G2_Assert.Run(); _pass += B99G2_Assert.Pass; _fail += B99G2_Assert.Fail;
//  ⚠️ 本件**不建 GameObject、不要场景、不跑 Unity 批处理**（`CampaignData` 全是静态字段），任何 Editor 宿主都能跑。
//  ⚠️ 本件会**临时**改写 `ProfileData` 的 `Ach_16`（`X18-9` 把它当「从 0 开始」用），`finally` 里**原值还原**。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B99_G2`：`CampaignData` 的领奖状态机（闸 / 两个列表 / 完成判据 / 可重复节点）。</summary>
public static class B99G2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[G2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B99_G2 `CampaignData` 领奖状态机（闸只有 State∈{10,40} · 高级档不写基础档 · 完成=末节点 · 可重复无限领）");

        int ach0 = ProfileData.AchievementValue(ProfileData.IdAchCompleteCampaign);
        // C 段要看到「第一次写」⇒ 先归 0（`finally` 原值还原，见文件头）。
        ProfileData.SetAchievementValue(ProfileData.IdAchCompleteCampaign, 0);
        CampaignData.ResetForTest();
        try
        {
            SectionA_GateIsOnlyState10Or40();
            SectionB_PremiumDoesNotWriteBase();
            SectionC_CompletionIsLastNode();
            SectionD_Repeatable();
        }
        finally
        {
            CampaignData.ResetForTest();
            ProfileData.SetAchievementValue(ProfileData.IdAchCompleteCampaign, ach0);
        }

        Debug.Log(_sink.P + "B99G2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具：`UM0 → 末节点` 的一条路径（按 `Nodes[i].Next` 做 BFS）
    static List<int> PathTo(int target)
    {
        var path = new List<int>();
        int n = CampaignData.NodeCount;
        if (target < 0 || target >= n) return path;
        var prev = new int[n];
        for (int i = 0; i < n; i++) prev[i] = -1;
        var q = new Queue<int>();
        prev[0] = 0; q.Enqueue(0);
        while (q.Count > 0)
        {
            int cur = q.Dequeue();
            if (cur == target) break;
            int[] nx = CampaignData.At(cur).Next;
            for (int k = 0; k < nx.Length; k++)
            {
                int v = nx[k];
                if (v >= 0 && v < n && prev[v] < 0) { prev[v] = cur; q.Enqueue(v); }
            }
        }
        if (prev[target] < 0) return path;                  // 不可达
        for (int cur = target; ; cur = prev[cur])
        {
            path.Add(cur);
            if (cur == 0) break;
            if (prev[cur] < 0) { path.Clear(); return path; }   // 链断了（不该发生）
        }
        path.Reverse();
        return path;
    }

    // ============================================================ A 闸只有 State ∈ {10,40}（没有逐档拒绝）+ 原版怪癖①
    static void SectionA_GateIsOnlyState10Or40()
    {
        CampaignData.ResetForTest();
        string why;
        int i0 = CampaignData.IndexOf("UM0");
        int last = CampaignData.NodeCount - 1;

        Tr(i0 >= 0, "A0 前提：节点表里有 `UM0`");
        Tr(CampaignData.NodeCount > 1, "A1 前提：节点表多于 1 格");
        if (i0 < 0) { Tr(false, "A2 前提：`UM0` 找不到（A0 已红，本段无法继续）"); return; }

        C(CampaignData.StateOf(i0), CampaignData.Unlocked, "A2 前提：起手根节点 = `Unlocked(10)`");
        Tr(!CampaignData.Claim(last, CampaignData.TierBasic, out why),
           "A3 锁着的末节点领不了 —— 闸要求 `State ∈ {10,40}`（why=[" + why + "]）");

        Tr(CampaignData.Claim(i0, CampaignData.TierPremium, out why), "A4 根节点**先领高级档**成功（State=10 过闸）");
        C(CampaignData.StateOf(i0), CampaignData.AllCollected, "A5 领高级档后 State = `AllCollected(30)`");
        Tr(!CampaignData.Claim(i0, CampaignData.TierBasic, out why),
           "A6 ★ 原版怪癖①：先领高级 ⇒ **基础档再也领不到了**（State=30 不过闸）｜🧨 把闸放宽到 20/30 ⇒ 红；⛔ 这不是 bug、照抄（why=[" + why + "]）");
        C(CampaignData.StateOf(i0), CampaignData.AllCollected, "A7 被拒后 State 不变（仍 30）");

        CampaignData.ResetForTest();
    }

    // ============================================================ B 高级档**不写**基础档列表 + 会话内解锁
    static void SectionB_PremiumDoesNotWriteBase()
    {
        CampaignData.ResetForTest();
        string why;
        int i0 = CampaignData.IndexOf("UM0");
        int i1 = CampaignData.IndexOf("UM1");
        if (i0 < 0 || i1 < 0) { Tr(false, "B0 前提：表里有 `UM0` / `UM1`（A0 已红）"); return; }

        Tr(!CampaignData.IsUnlocked(i1), "B0 前提：起手 `UM1`（`UM0` 的后继）是锁着的");
        Tr(CampaignData.Claim(i0, CampaignData.TierPremium, out why), "B1 前提：领根节点的高级档成功（why=[" + why + "]）");

        C(CampaignData.ClaimedCount, 0,
          "B2 ★ 领高级档**不写基础档列表**（原版：高级档只追加 `persistence+0x38`、不碰 `+0x30`）⇒ `ClaimedCount` 仍 0"
        + "｜🧨 改回 `if (!_base[i]) _base[i] = true;` ⇒ 1 ⇒ 红");
        C(CampaignData.BaseClaimed(i0), true,
          "B3 `BaseCollected` 仍为真 —— 原版 = `State ∈ {20,30}`，由 State 派生、**不看** `_base`");
        C(CampaignData.PremiumClaimed(i0), true, "B4 `PremiumCollected` 为真（原版 = `State == 30`）");
        Tr(CampaignData.IsUnlocked(i1),
          "B5 ★ 后继**当场**解锁（原版 `Collect` 末段把相邻 `locked` 置 10）—— 只领高级档也能往下走"
        + "｜🧨 删掉 `MarkSuccessorsUnlocked` / `IsUnlocked` 不读 `_sessionUnlocked` ⇒ 红");

        CampaignData.ResetForTest();
        Tr(!CampaignData.IsUnlocked(i1),
          "B6 ★ `ResetForTest` 同时清 `_sessionUnlocked`（否则「会话内解锁」会漏到下一局）｜🧨 不清 ⇒ 红");
    }

    // ============================================================ C 「战役完成」= 末节点 ∈ 基础档列表（不是 47 格全真）
    static void SectionC_CompletionIsLastNode()
    {
        CampaignData.ResetForTest();
        int last = CampaignData.NodeCount - 1;
        List<int> path = PathTo(last);
        if (path.Count < 2) { Tr(false, "C0 前提：`UM0 → 末节点` 有路径（BFS 失败，本段无法继续）"); return; }

        C(path[0], 0, "C0′ 前提：路径从根节点（下标 0）起");
        C(path[path.Count - 1], last, "C1 前提：路径终点 = 末节点");
        Tr(CampaignData.NodeCount > path.Count, "C2 前提：路径只有 " + path.Count + " 格，表里却有 " + CampaignData.NodeCount + " 格（**可以**辨识「全真」与「末节点」）");

        int okCount = 0;
        for (int k = 0; k < path.Count; k++)
        {
            string why;
            if (CampaignData.Claim(path[k], CampaignData.TierBasic, out why)) okCount++;
            else Debug.LogWarning(_sink.P + "C 段：领下标 " + path[k] + " 失败：" + why);
        }
        C(okCount, path.Count, "C3 前提：路径上 " + path.Count + " 格基础档全领到");
        C(CampaignData.ClaimedCount, path.Count, "C4 前提：`_base` 只记了路径上这 " + path.Count + " 格");
        Tr(CampaignData.ClaimedCount < CampaignData.NodeCount, "C5 前提：**没有**全领（" + CampaignData.ClaimedCount + " < " + CampaignData.NodeCount + "）");

        Tr(CampaignData.AllNodesClaimed(),
          "C6 ★ 「完成」判据 = **末节点 ∈ 基础档列表**（`Campaign__get_IsComplete.c:29-31`），不是「47 格全真」｜🧨 改回全量循环 ⇒ 红");
        Tr(CampaignData.NotifyCampaignCompletionIfAllClaimed(),
          "C7 完成判定 → `Ach_16` 记 1（`SetAchievementValueAtLeast`，幂等写点）");

        CampaignData.ResetForTest();
    }

    // ============================================================ D 可重复节点：无限重复领 + `GetNodeState` 的 40 覆盖
    static void SectionD_Repeatable()
    {
        CampaignData.ResetForTest();
        int last = CampaignData.NodeCount - 1;
        List<int> path = PathTo(last);
        if (path.Count < 2) { Tr(false, "D0 前提：`UM0 → 末节点` 有路径（BFS 失败，本段无法继续）"); return; }

        Tr(CampaignData.At(last).Repeatable, "D0′ 前提：末节点带 `IsRepeatable`（表里唯一一个）");
        for (int k = 0; k < path.Count - 1; k++) { string w0; CampaignData.Claim(path[k], CampaignData.TierBasic, out w0); }
        C(CampaignData.StateOf(last), CampaignData.Unlocked, "D1 前提：末节点已解锁、还没领");

        string why;
        Tr(CampaignData.Claim(last, CampaignData.TierBasic, out why), "D2 前提：末节点基础档领到（why=[" + why + "]）");
        C(CampaignData.StateOf(last), CampaignData.Repeatable,
          "D3 可重复节点领过之后 State = `Repeatable(40)`（原版 `GetNodeState` 把 20 覆盖成 40）");
        Tr(CampaignData.Claimable(last), "D4 可重复节点领过之后**仍可领**（`Claimable` = `State ∈ {10,40}`）");
        Tr(CampaignData.Claim(last, CampaignData.TierBasic, out why),
          "D5 ★ 重复领**基础档**仍成功（原版闸只看 State=40）｜🧨 恢复 `if (_base[i]) { …; return false; }` ⇒ 红");
        C(CampaignData.StateOf(last), CampaignData.Repeatable, "D6 重复领之后仍是 40");
        Tr(CampaignData.Claim(last, CampaignData.TierPremium, out why),
          "D7 前提：再领**高级档**也成功（`Claim` 不看 `HasPremium`；末节点实际没有高级档奖励，这里测的是状态机；why=[" + why + "]）");
        C(CampaignData.StateOf(last), CampaignData.Repeatable,
          "D8 ★ 高级档命中后仍被 `IsRepeatable` 覆盖成 40 ｜🧨 把 `StateOf` 第一行改回 `_base && _prem` ⇒ 30 ⇒ 红");

        CampaignData.ResetForTest();
    }
}
