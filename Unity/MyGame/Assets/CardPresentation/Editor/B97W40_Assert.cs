// B97W40_Assert.cs — 执行代理 W40（第九十七会话 · 波 4 · 日常线）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `日常_奖励:681` `RewardService.CollectSingleReward(RewardInfo)` 的 **⑥ 物品层**：
//     原版末尾统一跳 `reward.Item.<+0x1b8>(quantity)` = `ObtainableItem.HandleOnCollect`（Slot 8）。
//     基类 = **空实现**；**全汇编唯一 override** = `CosmeticItemAvatarBorder`：
//       `base(...) + if (AutoAssign) PlayerAvatarDataManager.Instance.SetCurrentItem(this)`。
//     我们的等价物 = `Shell/Inventory.cs` 的 `RewardService.HandleOnCollectEquivalent(string)`
//       （id 命中 `ProfileData.Borders` 且 `aa != 0` ⇒ `ProfileData.SetEquippedBorderById`）。
//
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/报告/B97_W40.md` §二；
//   原始二进制 = `d:/2/tools/decomp_full/CosmeticItemAvatarBorder__HandleOnCollect.c:12-23`
//     ＋ `d:/2/tools/il2cpp_out/dump.cs:18154`（override，RVA 0x6EDF40，Slot 8）
//     ＋ `dump.cs:18791`（基类，RVA 0x4B33B0 = 全局空桩 `ret 0`）。
//
// ============================ 怎么用（宿主）============================
//   `B97W40Assert.Run(); _sink.Pass += B97W40Assert.Pass; _sink.Fail += B97W40Assert.Fail;`
//   · 推荐宿主 = `Editor/ShellScene.cs`（`ShellScene.Run`）—— 插在**合计行之前**。
//   · 本文件**不建 GameObject、不建资产**；只改两个全局静态量（`ProfileData.EquippedBorderIndex`
//     与 `InventoryManager` 的「已报过」记忆），两条都在 `finally` 里还原。
//   · ⚠️ 必须在 **`-batchmode`** 下跑（`ProfileData.EnsureSave` 在批处理里不读、不写玩家的真存档；
//     非批处理跑这一份会真的装备并落盘玩家档案）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A 段：把 `CollectSingleReward` 里那句 `HandleOnCollectEquivalent(reward.Id)` 删掉
//           （= ⑤/⑥ 又变成「物品层缺口」）⇒ A1/A2 红（`EquippedBorderIndex` 停在起始值）。
//   · B 段：把 `HandleOnCollectEquivalent` 里的 `if (b.aa == 0) return;` 删掉
//           （= 「反正拿到了就装」那种错法）⇒ B1 红（`Default` 那条 `aa=0` 被装上）。
//   · C 段：把 `CollectItemRewardById` 的 `rs-item:unresolved` 那条 `Note` 删掉 ⇒ C2 红（静默失败 = 红线）。
//   · C3：把 `HandleOnCollectEquivalent` 的空 id 闸拆了、改成直接 `list[0].id == itemId` ⇒ 仍不崩，
//           但 C3 的前置断言（空 id 不改 index）会红（被 `Default` 顺手装上）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 W40：`CollectSingleReward` 的 ⑥ 物品层（`HandleOnCollect` ⇒ 自动装备头像边框）。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W40Assert
{
    static readonly CheckSink _sink = new CheckSink("[W40] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>一个「肯定不是边框」的 id —— 用来说明「其余类型 = 原版空基类 ⇒ 什么都不做」。</summary>
    const string BogusId = "B97W40_NotABorder_7f3a";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_W40 `RewardService.CollectSingleReward` 的 ⑥ 物品层："
                              + "`HandleOnCollect` ⇒ 按 `AutoAssign` 自动装备头像边框（判据 `CosmeticItemAvatarBorder__HandleOnCollect.c:12-23`）");
        int prevIndex = ProfileData.EquippedBorderIndex;      // 别把宿主/别人留下的状态带出这一份
        try
        {
            SectionA_自动装备();
            SectionB_aa为0不装备();
            SectionC_非边框不装备也不崩();
        }
        finally
        {
            ProfileData.EquippedBorderIndex = prevIndex;
            InventoryManager.ResetNotesForTest();
        }
        Debug.Log(_sink.P + "B97W40 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    static System.Collections.Generic.List<ProfileData.BorderItem> Borders()
    {
        return ProfileData.Borders;
    }

    /// <summary>最后一条 `aa != 0` 的边框下标（-1 = 数据里没有）。⛔ 不写死下标 —— 数据换了也要跟着走。</summary>
    static int LastAutoBorder(System.Collections.Generic.List<ProfileData.BorderItem> list)
    {
        int idx = -1;
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].aa != 0) idx = i;
        return idx;
    }

    /// <summary>第一条 `aa == 0` 的边框下标（-1 = 数据里没有）。</summary>
    static int FirstManualBorder(System.Collections.Generic.List<ProfileData.BorderItem> list)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].aa == 0) return i;
        return -1;
    }

    // ============================================================ A `AutoAssign=true` ⇒ 领取后装上
    static void SectionA_自动装备()
    {
        MenuCheck.Section(_sink, "A 喂一条「id = 某条 `aa != 0` 的边框」的奖励 ⇒ 领取后**那一条被装上**"
                              + "（原版 `if (AutoAssign) SetCurrentItem(this)`）");

        var list = Borders();
        Tr(list != null && list.Count >= 2,
           "A0 **前提断言**：`ProfileData.Borders` 至少 2 条（`Resources/profile_cosmetics.json` 现读 5 条；"
         + "清单空 ⇒ A1..A2 **没跑**，本文件不静默跳过）");
        if (list == null || list.Count < 2) return;

        int autoIdx = LastAutoBorder(list);
        int baseIdx = FirstManualBorder(list);
        Tr(autoIdx >= 0, "A0b **前提断言**：数据里至少有一条 `aa != 0`（原版 `autoAssign=1`）的边框");
        Tr(baseIdx >= 0, "A0c **前提断言**：数据里至少有一条 `aa == 0` 的边框（原版 `Default`，用来当起始值）");
        if (autoIdx < 0 || baseIdx < 0) return;

        ProfileData.EquippedBorderIndex = baseIdx;             // 起点故意**不是**要装的那条
        RewardService.CollectSingleReward(new RewardInfo(list[autoIdx].id, 1));

        C(ProfileData.EquippedBorderIndex, autoIdx,
          "A1 ★ 领到 `aa != 0` 的边框 ⇒ `EquippedBorderIndex` = 那一条（#" + autoIdx + " `" + list[autoIdx].n + "`）"
        + "｜🧨 把 `CollectSingleReward` 里那句 `HandleOnCollectEquivalent(reward.Id)` 删掉 ⇒ 停在 #" + baseIdx + " ⇒ 红");
        Tr(ProfileData.EquippedBorderIndex >= 0 && ProfileData.EquippedBorderIndex < list.Count
            && list[ProfileData.EquippedBorderIndex].id == list[autoIdx].id,
           "A2 装上的**就是那一条**（按 id 回读，⛔ 不是「随便装了清单里的某一条」）");

        int again = baseIdx;
        ProfileData.EquippedBorderIndex = again;
        RewardService.CollectSingleReward(new RewardInfo(list[autoIdx].id, 4));   // 再领一次（quantity 不进这件事）
        C(ProfileData.EquippedBorderIndex, autoIdx,
          "A3 同一 id 再领一次（quantity=4）⇒ 仍是那一条（重复领取不炸、也不乱挑）");
    }

    // ============================================================ B `AutoAssign=false` ⇒ 不装（灭自证）
    static void SectionB_aa为0不装备()
    {
        MenuCheck.Section(_sink, "B 喂一条「`aa == 0`」的边框奖励 ⇒ **装上的不变**"
                              + "（原版 `AutoAssign` 为假时 `SetCurrentItem` 那半边整段不执行）");

        var list = Borders();
        Tr(list != null && list.Count >= 2, "B0 **前提断言**：`Borders` 至少 2 条（不成立 ⇒ B1..B2 **没跑**）");
        if (list == null || list.Count < 2) return;

        int autoIdx = LastAutoBorder(list);
        int zeroIdx = FirstManualBorder(list);
        Tr(autoIdx >= 0 && zeroIdx >= 0, "B0b **前提断言**：两类边框都取到了（一条 `aa != 0`、一条 `aa == 0`）");
        if (autoIdx < 0 || zeroIdx < 0) return;

        ProfileData.EquippedBorderIndex = autoIdx;             // 当前装备的是**另一条**（且 aa != 0）
        RewardService.CollectSingleReward(new RewardInfo(list[zeroIdx].id, 1));

        C(ProfileData.EquippedBorderIndex, autoIdx,
          "B1 ★ 灭自证：领到 `aa == 0` 的边框（`" + list[zeroIdx].n + "`）⇒ 当前装备**不动**（仍是 #" + autoIdx + "）"
        + "｜🧨 删掉 `HandleOnCollectEquivalent` 里的 `if (b.aa == 0) return;`（=「拿到就装」）⇒ 变成 #" + zeroIdx + " ⇒ 红");
        Tr(ProfileData.EquippedBorderIndex != zeroIdx,
           "B2 逐条再核一遍：`aa == 0` 那条**没有**抢走当前装备");
    }

    // ============================================================ C 非边框 ⇒ 不装、不崩、出声
    static void SectionC_非边框不装备也不崩()
    {
        MenuCheck.Section(_sink, "C 不是边框的奖励 ⇒ 原版走**空基类**（什么都不做）＋ 我们那条 `rs-item:unresolved` 出声"
                              + "（⛔ 不静默失败，也 ⛔ 不假装装上）");

        var list = Borders();
        Tr(list != null && list.Count >= 1, "C0 **前提断言**：`Borders` 非空（不成立 ⇒ C1..C3 **没跑**）");
        if (list == null || list.Count < 1) return;

        int keepBefore = list.Count - 1;                      // 随便一个「当前装备」，用来验「没被动过」

        InventoryManager.ResetNotesForTest();                  // `Note` 是「同一个 key 只喊一次」⇒ 先清记忆
        string captured = null;
        Application.LogCallback cb = (m, stack, type) =>
        {
            if (type == LogType.Warning || type == LogType.Error)
                if (m != null && m.Contains(BogusId)) captured = m;
        };

        ProfileData.EquippedBorderIndex = keepBefore;
        Application.logMessageReceived += cb;
        try { RewardService.CollectSingleReward(new RewardInfo(BogusId, 3)); }   // 不许抛
        finally { Application.logMessageReceived -= cb; }

        C(ProfileData.EquippedBorderIndex, keepBefore,
          "C1 ★ 不是边框 ⇒ 当前装备**一格不动**（原版基类空实现；⛔ 别把「没命中」做成「装清单第 0 条」）");
        Tr(captured != null && captured.Contains(BogusId),
           "C2 ★ **真的出声了**（`CollectItemRewardById` 的 `rs-item:unresolved`，报文带那个 id；实得："
         + (captured == null ? "<没捕获到任何 warning>" : "「" + captured + "」") + "）"
         + "｜🧨 删掉那条 `Note` ⇒ 本条红（静默失败 = 红线）");

        // 空 id：同样不崩、不动
        int before = ProfileData.EquippedBorderIndex;
        RewardService.CollectSingleReward(new RewardInfo("", 1));
        C(ProfileData.EquippedBorderIndex, before,
          "C3 空 id ⇒ 不崩、当前装备也不动（`HandleOnCollectEquivalent` 的空 id 闸 + `rs-item:nullid` 出声）");
    }
}
