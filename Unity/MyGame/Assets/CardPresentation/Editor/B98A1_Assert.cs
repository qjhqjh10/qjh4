// B98A1_Assert.cs — 执行代理 A1（第九十八会话 · 波 5a · 日常/奖励线）的断言集
//
// ============================ 它盯的是什么 ============================
//   账 `日常_奖励 :681` `RewardService.CollectSingleReward(RewardInfo)` 的 **⑤/⑥ 物品层**：
//     原版末尾**统一跳** `reward.Item.<+0x1b8>(quantity)`（`decomp_full/RewardService__CollectSingleReward.c:182-186`）
//     = `ObtainableItem.HandleOnCollect`（**Slot 8**）。
//     · 基类 = **空实现**（`dump.cs:18791` `RVA 0x4B33B0` = 全局空桩 `ret 0`；
//       `out_il2csharp/ObtainableItem.cs:35` 空体；`decomp_full/ObtainableItem__HandleOnCollect.c` 体只有 `return;`
//       —— ⚠️ 那份**首行是 `NetworkingPeer__OnMessage`**，属已记的「文件名与内容不符」，**体照读、名不认**）。
//     · **全汇编唯一 override** = `CosmeticItemAvatarBorder.HandleOnCollect`（`dump.cs:18154`，RVA 0x6EDF40，Slot 8）
//       = `base(...)` + `if (AutoAssign) PlayerAvatarDataManager.Instance.SetCurrentItem(this)`
//       （`decomp_full/CosmeticItemAvatarBorder__HandleOnCollect.c:12-23`；`AutoAssign` = `PlayerItem.autoAssign` `+0x34`）。
//   我们的等价物 = `Shell/Inventory.cs` 的 `RewardService.HandleOnCollectEquivalent(string)`
//     （id 命中 `ProfileData.Borders` 且 `aa != 0` ⇒ `ProfileData.SetEquippedBorderById`）；
//     三条出口都调它：货币支 `:2083`（同行追加，零行数变化）· 末尾统一跳 `:2102`。
//
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_R9_日常20条施工单.md` §四·2
//                            ＋ `资料/完全复刻/报告/B97_W40.md` §二
//
// ============================ 与 B97W40_Assert.cs 的关系 ============================
//   调度台要求本波做**独立复验**（同一三档：aa != 0 自动装备 / aa == 0 不装备＝灭自证 / 非边框不动且出声）。
//   本文件的**独有部分**：
//     · D0 **批处理前提硬断言** —— `ProfileData.SetEquippedBorder` 末尾的
//       `SaveIfAuto()` = `if (!Application.isBatchMode) SaveNow();`（`ProfileData.cs:1151`）
//       ⇒ 非 `-batchmode` 跑本文件会**真装备并落盘玩家档案**。D0 在动任何状态**之前**判，红了就整份退出。
//     · A5 id 必须**逐字相等**（`HandleOnCollectEquivalent` 里是 `b.id != itemId`）——
//       只给 id 的**前缀** ⇒ 一律不装（＝灭「用 `Contains` / `StartsWith` 糊过去」那种写法）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1：删掉 `CollectSingleReward` 末尾那句 `HandleOnCollectEquivalent(reward.Id)` ⇒ A1/A2 红
//         （`EquippedBorderIndex` 停在起始的 `aa == 0` 那条）。
//   · B1：删掉 `HandleOnCollectEquivalent` 里的 `if (b.aa == 0) return;`（=「拿到就装」）⇒ B1/B2 红。
//   · A5：把 `b.id != itemId` 改成 `!b.id.Contains(itemId)` / `b.id.StartsWith(itemId)` ⇒ A5 红。
//   · C2：删掉 `CollectItemRewardById` 的 `rs-item:unresolved` 那条 `Note` ⇒ C2 红（静默失败 = 红线）。
//
// ============================ 怎么用（宿主）============================
//   `B98A1_Assert.Run(); _sink.Pass += B98A1_Assert.Pass; _sink.Fail += B98A1_Assert.Fail;`
//   · 推荐宿主 = `Editor/ShellScene.cs`（`ShellScene.Run`，**`-batchmode`**）—— 插在合计行之前。
//   · 本文件**不建 GameObject、不建资产**；只改两个全局静态量（`ProfileData.EquippedBorderIndex`
//     与 `InventoryManager` 的「已报过」记忆），两条都在 `finally` 里还原。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>A1：`CollectSingleReward` 的 ⑤/⑥ 物品层（`HandleOnCollect` ⇒ 按 `AutoAssign` 自动装备头像边框）。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B98A1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[A1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>本趟通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>本趟失败的条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }
    /// <summary>本趟**实际执行**的断言条数（= Pass + Fail；调用方打印它 = 自证真被跑到）。</summary>
    public static int RunCount { get { return _sink.Pass + _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>一个「肯定不是边框」的 id —— 用来说明「其余类型 = 原版空基类 ⇒ 什么都不做」。</summary>
    const string NotABorderId = "B98A1_NotABorder_5c91";

    // ============================================================ 入口
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();     // `RunCount` 只算本趟
        MenuCheck.Section(_sink, "B98_A1 `RewardService.CollectSingleReward` 的 ⑥ 物品层复验："
                              + "`HandleOnCollect` ⇒ 按 `AutoAssign` 自动装备头像边框（判据 `CosmeticItemAvatarBorder__HandleOnCollect.c:12-23`）");

        // ---- D0：批处理前提**硬断言**（⛔ 不许省：非批处理下 SetEquippedBorder 会真写玩家档案）
        Tr(Application.isBatchMode,
            "D0 ★ 前提：**必须在 `-batchmode` 下跑**（`ProfileData.SaveIfAuto()` = `if (!Application.isBatchMode) SaveNow();` "
          + "⇒ 非批处理会真装备并落盘玩家 `profile.json`）—— 本条红时下面一条都不执行（⛔ 不静默跳过）");
        if (!Application.isBatchMode)
        {
            Debug.LogError(_sink.P + "B98_A1 合计（**提前中止** · 非批处理）：" + RunCount + " 条，失败 " + _sink.Fail);
            return;
        }

        int prevIndex = ProfileData.EquippedBorderIndex;            // 别把宿主/别人留下的状态带出这一份
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
        Debug.Log(_sink.P + "B98_A1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败（共 " + RunCount + " 条）");
    }

    // ============================================================ 夹具
    /// <summary>第一条 `aa != 0` 的边框下标（-1 = 数据里没有）。⛔ 不写死下标 —— 数据换了也要跟着走。</summary>
    static int FirstAutoBorder(List<ProfileData.BorderItem> list)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].aa != 0) return i;
        return -1;
    }

    /// <summary>第一条 `aa == 0` 的边框下标（原版 `Default` 那条，用来当起始值 / 反例）。</summary>
    static int FirstManualBorder(List<ProfileData.BorderItem> list)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].aa == 0) return i;
        return -1;
    }

    // ============================================================ A `AutoAssign != 0` ⇒ 领取后装上
    static void SectionA_自动装备()
    {
        MenuCheck.Section(_sink, "A 喂一条「`Id` = 某条 `aa != 0` 的边框」的奖励 ⇒ 领取后**那一条被装上**"
                              + "（原版 `if (AutoAssign) SetCurrentItem(this)`）");

        var list = ProfileData.Borders;
        Tr(list != null && list.Count >= 2,
           "A0 ★ 前提：`ProfileData.Borders` 至少 2 条（`Resources/profile_cosmetics.json` 现读 5 条；"
         + "清单空 ⇒ A1..A5 无法判别，本条红，⛔ 不静默跳过）");
        if (list == null || list.Count < 2) return;

        int autoIdx = FirstAutoBorder(list);
        int manualIdx = FirstManualBorder(list);
        Tr(autoIdx >= 0 && manualIdx >= 0,
           "A0b ★ 前提：数据里两类边框都在（`aa != 0` 与 `aa == 0` 各至少一条；现读 5 条 = 1×aa0 + 4×aa1）");
        if (autoIdx < 0 || manualIdx < 0) return;

        ProfileData.EquippedBorderIndex = manualIdx;               // 起点故意**不是**要装的那条
        RewardService.CollectSingleReward(new RewardInfo(list[autoIdx].id, 1));

        C(ProfileData.EquippedBorderIndex, autoIdx,
          "A1 ★ 领到 `aa != 0` 的边框 ⇒ `EquippedBorderIndex` = 那一条（#" + autoIdx + " `" + list[autoIdx].n + "`）"
        + "｜🧨 删掉 `CollectSingleReward` 末尾那句 `HandleOnCollectEquivalent(reward.Id)` ⇒ 停在 #" + manualIdx + " ⇒ 红");
        Tr(ProfileData.EquippedBorderIndex >= 0 && ProfileData.EquippedBorderIndex < list.Count
            && list[ProfileData.EquippedBorderIndex].id == list[autoIdx].id,
           "A2 装上的**就是那一条**（按 id 回读，⛔ 不是「清单里随便装了某一条」）");

        int before = ProfileData.EquippedBorderIndex;              // = autoIdx
        string prefix = list[autoIdx].id != null && list[autoIdx].id.Length > 8 ? list[autoIdx].id.Substring(0, 8) : null;
        Tr(prefix != null && prefix != list[autoIdx].id,
           "A4 ★ 前提：能取到该 id 的**真前缀**（短于整条 —— A5 试的就是「前缀不是 id」）");
        if (prefix != null && prefix != list[autoIdx].id)
        {
            RewardService.CollectSingleReward(new RewardInfo(prefix, 1));
            C(ProfileData.EquippedBorderIndex, before,
              "A5 ★ 只给 id 的**前缀** ⇒ 当前装备**一格不动**（`HandleOnCollectEquivalent` 是 `b.id != itemId` 逐字相等；"
            + "🧨 把比较写成 `Contains` / `StartsWith` ⇒ 这条抢走当前装备 ⇒ 红）");
        }
    }

    // ============================================================ B `AutoAssign == 0` ⇒ 不装（灭自证）
    static void SectionB_aa为0不装备()
    {
        MenuCheck.Section(_sink, "B 喂一条「`aa == 0`」的边框奖励 ⇒ **装上的不变**"
                              + "（原版 `AutoAssign` 为假时 `SetCurrentItem` 那半边整段不执行）");

        var list = ProfileData.Borders;
        Tr(list != null && list.Count >= 2, "B0 ★ 前提：`Borders` 至少 2 条（不成立 ⇒ B1..B2 无法判别，本条红）");
        if (list == null || list.Count < 2) return;

        int autoIdx = FirstAutoBorder(list);
        int zeroIdx = FirstManualBorder(list);
        Tr(autoIdx >= 0 && zeroIdx >= 0 && autoIdx != zeroIdx,
           "B0b ★ 前提：两类边框都取到了（一条 `aa != 0`、一条 `aa == 0`，且不是同一条）");
        if (autoIdx < 0 || zeroIdx < 0 || autoIdx == zeroIdx) return;

        ProfileData.EquippedBorderIndex = autoIdx;                 // 当前装备的是**另一条**（且 aa != 0）
        int before = ProfileData.EquippedBorderIndex;
        RewardService.CollectSingleReward(new RewardInfo(list[zeroIdx].id, 1));

        C(ProfileData.EquippedBorderIndex, before,
          "B1 ★ 灭自证：领到 `aa == 0` 的边框（`" + list[zeroIdx].n + "`）⇒ 当前装备**不动**（仍是 #" + before + "）"
        + "｜🧨 删掉 `HandleOnCollectEquivalent` 里的 `if (b.aa == 0) return;`（=「拿到就装」）⇒ 变成 #" + zeroIdx + " ⇒ 红");
        Tr(ProfileData.EquippedBorderIndex != zeroIdx,
           "B2 逐条再核一遍：`aa == 0` 那条**没有**抢走当前装备");
    }

    // ============================================================ C 非边框 ⇒ 不装、不崩、出声
    static void SectionC_非边框不装备也不崩()
    {
        MenuCheck.Section(_sink, "C 不是边框的奖励 ⇒ 原版走**空基类**（什么都不做）＋ 我们那条 `rs-item:unresolved` 出声"
                              + "（⛔ 不静默失败，也 ⛔ 不假装装上）");

        var list = ProfileData.Borders;
        Tr(list != null && list.Count >= 1, "C0 ★ 前提：`Borders` 非空（不成立 ⇒ C1..C3 无法判别，本条红）");
        if (list == null || list.Count < 1) return;

        int keep = list.Count - 1;                                 // 随便一个「当前装备」，用来验「没被动过」
        ProfileData.EquippedBorderIndex = keep;

        InventoryManager.ResetNotesForTest();                      // `Note` 是「同一个 key 只喊一次」⇒ 先清记忆
        string captured = null;
        Application.LogCallback cb = (m, stack, type) =>
        {
            if ((type == LogType.Warning || type == LogType.Error) && m != null && m.Contains(NotABorderId)) captured = m;
        };
        Application.logMessageReceived += cb;
        try { RewardService.CollectSingleReward(new RewardInfo(NotABorderId, 3)); }   // 不许抛
        finally { Application.logMessageReceived -= cb; }

        C(ProfileData.EquippedBorderIndex, keep,
          "C1 ★ 不是边框 ⇒ 当前装备**一格不动**（原版基类空实现；⛔ 别把「没命中」做成「装清单第 0 条」）");
        Tr(captured != null && captured.Contains(NotABorderId),
           "C2 ★ **真的出声了**（`CollectItemRewardById` 的 `rs-item:unresolved`，报文带那个 id；实得："
         + (captured == null ? "<没捕获到任何 warning>" : "「" + captured + "」") + "）"
         + "｜🧨 删掉那条 `Note` ⇒ 本条红（静默失败 = 红线）");

        int before = ProfileData.EquippedBorderIndex;
        RewardService.CollectSingleReward(new RewardInfo("", 1));
        C(ProfileData.EquippedBorderIndex, before,
          "C3 空 id ⇒ 不崩、当前装备也不动（`HandleOnCollectEquivalent` 的空 id 闸 + `rs-item:nullid` 出声）");
    }
}
